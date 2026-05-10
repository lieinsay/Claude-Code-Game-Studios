# Story 005: Persistence & State Recovery

> **Epic**: Settlement Market & Port Village Economy
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/port-village-market.md`
**Requirement**: `TR-settlement-001`, `TR-settlement-003`

**ADR Governing Implementation**: ADR-0014 (§6 ADR-0003 序列化, §2 Dictionary 后端存储); ADR-0003 (Canonical JSON 快照包)
**ADR Decision Summary**: 所有定居点/摊位/NPC 状态通过 ADR-0003 Canonical JSON 快照包持久化为 progress.settlement-market。快照时机：(1) 每次购买完成后 (2) repair_completed 处理后 (3) 任何状态变更后。快照格式三层嵌套：settlements { settlement_state, completed_node_ids }, stalls { stall_state, settlement_id }, npcs { npc_state, stall_id }。domain_id="settlement-market"。反序列化时验证所有 stall_id 和 npc_id 在当前 Registry 中存在——跳过未知实体并记录 warning。新游戏初始化时 settlements/stalls/npcs 从 Registry 静态定义构建，completed_node_ids 为空。恢复时快照覆盖初始状态——快照权威。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 快照必须在状态变更后立即写入——不可延迟至帧末；序列化前 .duplicate(true) 所有 Array 字段——防止外部修改；反序列化时验证实体在 Registry 中存在
- Forbidden: 快照中持久化 UI 状态（界面打开/关闭）——只记录数据层状态；对不存在的 stall_id/npc_id 创建幽灵实体
- Guardrail: 快照中 settlement_state 与 active_stall_count 不一致时——读档后立即执行 recalculate_settlement_activity() 修正

---

## Acceptance Criteria

### New Game Initialization

- [ ] **AC-1**: GIVEN 新游戏启动 + Persistence 中无 progress.settlement-market 快照，WHEN SettlementManager 初始化，THEN settlements.stall-state = DORMANT + completed_node_ids=[] + stall.gh-general=OPEN_BASIC + 其余 3 摊位 CLOSED + npc.atu=IDLE + 其余 3 NPC ABSENT
- [ ] **AC-2**: GIVEN 新游戏，WHEN 初始化完成，THEN 触发首次快照 (progress.settlement-market) ——即使初始状态也需持久化

### Serialization Roundtrip

- [ ] **AC-3**: GIVEN settlement_state=RECOVERING + completed_node_ids=[&"repair_node.starlight_dock"] + 2 stalls OPEN_BASIC + 2 CLOSED + 2 NPCs IDLE + 2 ABSENT，WHEN _serialize_settlement() → JSON.stringify() → JSON.parse() → _deserialize_settlement()，THEN 所有字段一致无丢失
- [ ] **AC-4**: GIVEN settlement_state=ACTIVE + completed_node_ids 含 3 个节点 + 全部 4 stalls OPEN_BASIC + 全部 4 NPCs IDLE，WHEN 序列化往返，THEN 所有 3 层状态完整恢复
- [ ] **AC-5**: GIVEN completed_node_ids 包含重复项（如存档损坏），WHEN _deserialize_settlement()，THEN 反序列化时去重——仅保留唯一值

### Snapshot Triggers

- [ ] **AC-6**: GIVEN execute_purchase() 成功后，WHEN 返回前，THEN _trigger_snapshot() 被调用。购买是状态变更——必须立即持久化
- [ ] **AC-7**: GIVEN on_repair_completed() 处理后 + completed_node_ids 或 stall/NPC 状态变更，WHEN 返回前，THEN _trigger_snapshot() 被调用
- [ ] **AC-8**: GIVEN 无状态变更的操作（如重复 repair 信号、use_requested on closed stall），WHEN 处理，THEN 不触发快照。避免无意义写入

### Session Recovery

- [ ] **AC-9**: GIVEN 存档中有 progress.settlement-market 快照 + settlement_state=RECOVERING + 2 stalls OPEN_BASIC + 2 NPCs IDLE，WHEN 读档后 SettlementManager._restore_from_snapshot()，THEN 状态与存档前完全一致 + completed_node_ids 完整恢复。后续 repair_completed 的 F.2 判定不受影响
- [ ] **AC-10**: GIVEN 快照中 stall_state 与 NPC state 不一致（如 stall OPEN_BASIC 但 NPC 仍是 ABSENT——数据损坏），WHEN _restore_from_snapshot()，THEN 自动修正：以 stall_state 为准，强制对应 NPC→IDLE。记录 warning

### Defensive Deserialization

- [ ] **AC-11**: GIVEN 快照中包含未在 Registry 中注册的 stall_id（数据迁移残留），WHEN _deserialize_settlement()，THEN 跳过该条目 + 记录 warning。不崩溃
- [ ] **AC-12**: GIVEN 快照中包含未在 Registry 中注册的 npc_id，WHEN _deserialize_settlement()，THEN 跳过该条目 + 记录 warning
- [ ] **AC-13**: GIVEN 快照中 settlement 的 completed_node_ids 包含未在 Registry 中注册的 node_id，WHEN 反序列化，THEN 保留该 node_id（可能是未来内容的前向兼容）但记录 warning
- [ ] **AC-14**: GIVEN 快照中 settlement_state 与根据 active_stall_count 计算的状态不一致（存档损坏），WHEN _restore_from_snapshot()，THEN 读档后立即执行 recalculate_settlement_activity() 修正 settlement_state。静默修复，不通知玩家

---

## Implementation Notes

### Serialization

```text
func _serialize_settlement() -> Dictionary:
    var serialized_settlements := {}
    for sid in settlements:
        var s := settlements[sid]
        serialized_settlements[sid] = {
            "settlement_state": s["settlement_state"],
            "completed_node_ids": s["completed_node_ids"].duplicate(true),
        }

    var serialized_stalls := {}
    for sid in stalls:
        serialized_stalls[sid] = {
            "stall_state": stalls[sid]["stall_state"],
            "settlement_id": stalls[sid]["settlement_id"],
        }

    var serialized_npcs := {}
    for nid in npcs:
        serialized_npcs[nid] = {
            "npc_state": npcs[nid]["npc_state"],
            "stall_id": npcs[nid]["stall_id"],
        }

    return {
        "domain_id": "settlement-market",
        "settlements": serialized_settlements,
        "stalls": serialized_stalls,
        "npcs": serialized_npcs,
    }
```

### Deserialization

```text
func _deserialize_settlement(snapshot: Dictionary) -> void:
    # 恢复 settlements
    var settlements_data: Dictionary = snapshot.get("settlements", {})
    for sid in settlements_data:
        if not Registry.has_entity(sid):
            push_warning("Settlement: skipping unknown settlement '%s' in snapshot" % sid)
            continue
        var s_data := settlements_data[sid]
        var completed: Array = s_data.get("completed_node_ids", [])

        # 去重
        var deduped: Array = []
        for nid in completed:
            if nid not in deduped:
                deduped.append(nid)

        settlements[sid] = {
            "settlement_state": s_data.get("settlement_state", SETTLEMENT_DORMANT),
            "completed_node_ids": deduped,
        }

    # 恢复 stalls
    var stalls_data: Dictionary = snapshot.get("stalls", {})
    for sid in stalls_data:
        if not Registry.has_entity(sid):
            push_warning("Settlement: skipping unknown stall '%s' in snapshot" % sid)
            continue
        stalls[sid] = {
            "stall_state": stalls_data[sid].get("stall_state", STALL_CLOSED),
            "settlement_id": stalls_data[sid].get("settlement_id", &""),
        }

    # 恢复 npcs
    var npcs_data: Dictionary = snapshot.get("npcs", {})
    for nid in npcs_data:
        if not Registry.has_entity(nid):
            push_warning("Settlement: skipping unknown NPC '%s' in snapshot" % nid)
            continue
        npcs[nid] = {
            "npc_state": npcs_data[nid].get("npc_state", NPC_ABSENT),
            "stall_id": npcs_data[nid].get("stall_id", &""),
        }

    # 一致性修正 (EC-11-09 类比)
    _reconcile_settlement_state()
```

### Snapshot Trigger

```text
func _trigger_snapshot() -> void:
    var snapshot := _serialize_settlement()
    var success := Persistence.capture_snapshot("progress.settlement-market", snapshot)
    if not success:
        push_error("Settlement: failed to persist snapshot")
```

### Consistency Reconciliation

```text
func _reconcile_settlement_state() -> void:
    # 修正 settlement_state 与 stall 状态不一致
    for sid in settlements:
        recalculate_settlement_activity(sid)

    # 修正 NPC 状态与 stall 状态不一致
    for nid in npcs:
        var stall_id: StringName = npcs[nid]["stall_id"]
        if not stalls.has(stall_id):
            continue
        var stall_state: int = stalls[stall_id]["stall_state"]
        var npc_state: int = npcs[nid]["npc_state"]
        if stall_state >= STALL_OPEN_BASIC and npc_state < NPC_IDLE:
            push_warning("Settlement: NPC '%s' inconsistent with stall '%s' — auto-correcting" % [nid, stall_id])
            npcs[nid]["npc_state"] = NPC_IDLE
        elif stall_state < STALL_OPEN_BASIC and npc_state >= NPC_IDLE:
            push_warning("Settlement: NPC '%s' inconsistent with stall '%s' — auto-correcting" % [nid, stall_id])
            npcs[nid]["npc_state"] = NPC_ABSENT
```

---

## Out of Scope

- Persistence.capture_snapshot() / restore_snapshot() 实现——属于 local-save-persistence Epic
- JSON.stringify() / JSON.parse() 与 Canonical JSON 格式——属于 ADR-0003
- Registry.has_entity() 实现——属于 content-registry Epic
- 快照失败 UI 警告（如 user:// storage 配额满）——属于 #16 UIManager

---

## QA Test Cases

- **AC-1**: New game → correct defaults
- **AC-2**: Initial snapshot written
- **AC-3**: Roundtrip RECOVERING state — all fields match
- **AC-4**: Roundtrip ACTIVE state — all 3 tiers correct
- **AC-5**: Duplicate completed_node_ids → deduped on restore
- **AC-6**: Purchase triggers snapshot
- **AC-7**: Repair triggers snapshot
- **AC-8**: No-op does not trigger snapshot
- **AC-9**: Full session recovery fidelity
- **AC-10**: Auto-correct stall/NPC inconsistency
- **AC-11/12**: Unknown stall/NPC in snapshot → skipped + warning
- **AC-13**: Unknown node_id in completed_node_ids → retained + warning
- **AC-14**: settlement_state corrected on restore

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/settlement-market/PersistenceTest.csproj` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (purchase triggers snapshot), Story 003 (repair triggers snapshot), Story 004 (signal wiring for snapshot timing), local-save-persistence Epic (capture_snapshot, restore_snapshot, ADR-0003)
- Unlocks: Story 006 (edge cases may add snapshot guards)
