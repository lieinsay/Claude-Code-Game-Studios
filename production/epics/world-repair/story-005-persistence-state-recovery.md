# Story 005: Persistence & State Recovery

> **Epic**: World Repair & Unlock
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/world-repair-unlock.md`
**Requirement**: `TR-repair-001`, `TR-repair-002`

**ADR Governing Implementation**: ADR-0011 (§6 ADR-0003 序列化, §5 submit_deposit 与 commit_deposit 协作)
**ADR Decision Summary**: WorldRepair 通过 ADR-0003 Canonical JSON 快照包持久化修复节点状态为 `progress.world-repair`。序列化包含所有节点的 repair_state 和 deposited 计数器（repair_progress 由 deposited 重新计算——不持久化衍生值）。反序列化在启动时从 snapshot 恢复全部状态。存档检查点由 repair_completed 信号驱动（每完成一个修复节点 → 立即持久化）。关键恢复策略：若 commit_deposit 成功但 deposited 更新前崩溃，存档恢复时从 Pool 6 的终态记录重建 deposited 计数器（而非仅信任 snapshot 中的 deposited 字段）。分批提交中途存档→读档后 deposited 计数器一致，repair_progress 保持不变，可继续提交。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 序列化仅存储 repair_state + deposited——repair_progress 由 deposited 重新计算（值去重）；存档恢复时交叉验证 deposited 与 Pool 6 终态记录——不一致时以 Pool 6 为准重建；repair_completed 触发后立即 persist
- Forbidden: 序列化 repair_progress 浮点值（精度漂移风险）；仅信任 snapshot deposited 而不与 Pool 6 交叉验证
- Guardrail: snapshot 大小 < 1KB (MVP 1 节点)；反序列化 < 0.5ms

---

## Acceptance Criteria

### Serialization

- [ ] **AC-1**: GIVEN repair_nodes = {starlight_dock: {repair_state: KNOWN, deposited: {repair_kit: 2, basic_supply: 0}, repair_progress: 0.25}}，WHEN _serialize_world_repair()，THEN 输出包含 repair_state: 1 和 deposited: {repair_kit: 2, basic_supply: 0}。不包含 repair_progress（值去重——由 deposited 重新计算）
- [ ] **AC-2**: GIVEN 多个修复节点（未来扩展），WHEN 序列化，THEN 所有节点均包含在 snapshot.nodes 中。不遗漏

### Deserialization & State Recovery

- [ ] **AC-3**: GIVEN snapshot 含 repair_state=KNOWN + deposited={repair_kit: 3}，WHEN _deserialize_world_repair(snapshot)，THEN repair_nodes[starlight_dock] 状态恢复。repair_progress 由 deposited 重新计算为 min(3/4, 1.0)/2 = 0.375（假设还缺 basic_supply）
- [ ] **AC-4**: GIVEN snapshot 中节点 repair_state=REPAIRED + deposited={repair_kit: 4, basic_supply: 4}，WHEN 恢复，THEN repair_state=REPAIRED。已修复节点不可逆

### Mid-Batch Save/Load Integrity

- [ ] **AC-5**: GIVEN 分批提交：第一次提交 repair_kit×3 → deposited[repair_kit]=3, repair_progress=0.375 → 存档，WHEN 读档，THEN deposited[repair_kit]=3, progress=0.375, repair_state=KNOWN。可继续提交剩余 repair_kit×1 + basic_supply×4
- [ ] **AC-6**: GIVEN 分批提交中途存档 + 读档后继续提交，WHEN 提交 {repair_kit: 1, basic_supply: 4}，THEN deposited 更新为 {repair_kit: 4, basic_supply: 4} + repair_completion=true + 状态→REPAIRED。读档不阻止修复完成

### Crash Recovery — Pool 6 Cross-Validation

- [ ] **AC-7**: GIVEN commit_deposit 成功（材料已进入 Pool 6）但 deposited 计数器未更新（崩溃情景），WHEN 恢复存档，THEN 检测 deposited 与 Pool 6 终态记录不一致 → 从 Pool 6 重建 deposited。不丢失已提交材料
- [ ] **AC-8**: GIVEN Pool 6 中 starlight_dock 有 repair_kit×3 + basic_supply×2（前次正常提交），与 deposited 一致，WHEN 恢复时交叉验证，THEN 一致 → 无变更

### Repair Completion Checkpoint

- [ ] **AC-9**: GIVEN repair_completed 信号发射后，WHEN #3 Persistence 消费并写入存档，THEN progress.world-repair snapshot 包含 repair_state=REPAIRED。已修复状态写入持久化

### Cross-Version Compatibility

- [ ] **AC-10**: GIVEN 旧版本存档中 deposited 包含当前版本已移除的材料类型，WHEN 加载，THEN 未知材料键保留在 deposited 中（不报错、不丢弃）。新版本 required_resources 决定验证行为
- [ ] **AC-11**: GIVEN snapshot 中节点 ID 不在当前 Registry 中，WHEN 加载，THEN 保留该节点状态（不丢弃），但标记为 orphan——submit_deposit 对其返回 invalid_node

### ADR-0003 Integration

- [ ] **AC-12**: GIVEN WorldRepair 在 feature_ready 阶段，WHEN 注册 domain serializer，THEN Persistence.register_domain_serializer("world-repair", _serialize_world_repair) 被调用。"world-repair" domain ID 正确注册

---

## Implementation Notes

### Serializer

```gdscript
func _serialize_world_repair() -> Dictionary:
    var serialized_nodes: Dictionary = {}
    for node_id in repair_nodes:
        var node: Dictionary = repair_nodes[node_id]
        serialized_nodes[node_id] = {
            "repair_state": node["repair_state"],
            "deposited": node["deposited"].duplicate(true),
            # repair_progress 不序列化——由 deposited 重新计算（值去重）
        }
    return {
        "domain_id": "world-repair",
        "nodes": serialized_nodes,
    }


func _deserialize_world_repair(snapshot: Dictionary) -> void:
    var nodes_data: Dictionary = snapshot.get("nodes", {})
    for node_id in nodes_data:
        var data: Dictionary = nodes_data[node_id]
        var deposited: Dictionary = data.get("deposited", {})

        repair_nodes[node_id] = {
            "repair_state": data.get("repair_state", REPAIR_STATE_UNREVEALED),
            "deposited": deposited,
            "repair_progress": _compute_repair_progress_from_deposited(node_id, deposited),
        }

    # 交叉验证 Pool 6 终态
    _cross_validate_with_pool_6()


func _compute_repair_progress_from_deposited(node_id: StringName, deposited: Dictionary) -> float:
    # 临时使用 deposited 计算 progress（不依赖完整 repair_nodes）
    var required: Dictionary = _get_required_resources(node_id)
    if required.is_empty():
        return 0.0

    var total: float = 0.0
    var count: int = 0
    for rid in required:
        count += 1
        var req: int = required[rid]
        if req <= 0:
            total += 1.0
        else:
            total += minf(float(deposited.get(rid, 0)) / float(req), 1.0)

    if count == 0:
        return 0.0
    return clampf(total / float(count), 0.0, 1.0)
```

### Pool 6 Cross-Validation

```gdscript
func _cross_validate_with_pool_6() -> void:
    # 对每个已修复或部分提交的节点，交叉验证 deposited 计数器
    for node_id in repair_nodes:
        var node: Dictionary = repair_nodes[node_id]
        var deposited: Dictionary = node["deposited"]

        # 从 Pool 6 查询终态记录（#5 ResourcesManager）
        var pool_6_deposits: Dictionary = ResourcesManager.get_pool_6_deposits(node_id)

        # 若 Pool 6 有更完整的记录，以 Pool 6 为准
        for rid in pool_6_deposits:
            var pool_qty: int = pool_6_deposits[rid]
            var local_qty: int = deposited.get(rid, 0)
            if pool_qty > local_qty:
                push_warning("WorldRepair: deposited[%s] mismatch — pool_6=%d, local=%d. Rebuilding." %
                    [rid, pool_qty, local_qty])
                deposited[rid] = pool_qty

        # 重算 progress
        node["repair_progress"] = _compute_repair_progress_from_deposited(node_id, deposited)

        # 若 Pool 6 显示全部满足但本地状态非 REPAIRED
        if _check_repair_completion_from_deposited(node_id, deposited):
            if node["repair_state"] != REPAIR_STATE_REPAIRED:
                push_warning("WorldRepair: %s completion detected on recovery — transitioning to REPAIRED" % node_id)
                node["repair_state"] = REPAIR_STATE_REPAIRED
```

### Domain Registration

```gdscript
func _on_feature_ready() -> void:
    # 注册 domain serializer（遵循 ADR-0003）
    Persistence.register_domain_serializer("world-repair", _serialize_world_repair)

    # 从存档恢复或初始化新游戏
    if Persistence.has_saved_game():
        var snapshot: Dictionary = Persistence.load_domain_snapshot("world-repair")
        _deserialize_world_repair(snapshot)
    else:
        _init_new_game_state()
```

---

## Out of Scope

- Persistence.register_domain_serializer 的具体实现——属于 local-save-persistence Epic
- ResourcesManager.get_pool_6_deposits 的具体实现——属于 resources-goods-capacity Epic
- Pool 6 终态的完整 schema——属于 resources-goods-capacity Epic Story 007
- capture_snapshot 在 repair_completed 信号回调中的调用——属于 local-save-persistence Epic

---

## QA Test Cases

- **AC-1/2**: Serialization — excludes repair_progress, includes all nodes
- **AC-3/4**: Deserialization — state + progress recomputed, repaired preserved
- **AC-5/6**: Mid-batch save→load→continue→complete
- **AC-7/8**: Pool 6 cross-validation — mismatch detected and rebuilt; match confirmed
- **AC-9**: repair_completed → checkpoint with REPAIRED state
- **AC-10/11**: Cross-version — unknown material preserved; orphan node handled
- **AC-12**: Domain serializer registered with correct domain_id

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/world-repair/persistence_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (repair_nodes structure), Story 002 (deposited counters), resources-goods-capacity Epic (get_pool_6_deposits, Pool 6 schema), local-save-persistence Epic (register_domain_serializer, load_domain_snapshot, has_saved_game)
- Unlocks: Story 006 (persistence edge cases — crashed mid-commit recovery, cross-version orphan nodes)
