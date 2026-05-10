# Story 003: Repair-Driven Unlock & NPC State

> **Epic**: Settlement Market & Port Village Economy
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/port-village-market.md`
**Requirement**: `TR-settlement-002`, `TR-settlement-003`

**ADR Governing Implementation**: ADR-0014 (§4b 修复驱动, §5c F.2 摊位解锁判定, §5d F.3 定居点活跃度, §4a 查询接口)
**ADR Decision Summary**: 修复驱动是集市系统的核心推进力。on_repair_completed(node_id) 消费 WorldRepair (#13) 的 repair_completed 信号：通过 #13 查询 node_id 的 linked_location_id → 匹配所属 settlement → 将 node_id 加入 completed_node_ids（集合去重）→ 遍历该 settlement 所有摊位执行 F.2 解锁判定。F.2: is_stall_unlocked() 检查 stall.required_node_ids ∩ completed_node_ids 是否 ≥ unlock_threshold_basic (1)。F.3: recalculate_settlement_activity() 计算 active_stall_count = COUNT({stall.state ≥ OPEN_BASIC})，判定 settlement 状态：active=1 → DORMANT，1 < active < total → RECOVERING，active = total → ACTIVE。NPC 状态随摊位联动：摊位 closed→open_basic 时对应 NPC absent→idle。completed_node_ids 集合天然去重——重复 repair_completed 不产生重复条目。摊位 required_node_ids 为空集时永不自动解锁（EC-11）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: completed_node_ids 必须集合去重——重复 repair_completed 不产生重复解锁；F.3 聚合判定在每次摊位状态变更后执行；NPC 状态变更必须与摊位状态变更原子绑定
- Forbidden: 对已 open_basic 的摊位重复触发解锁；在不验证 linked_location_id 匹配的情况下解锁摊位
- Guardrail: 摊位 required_node_ids 为空 → 永不自动解锁（配置错误应 warning）；修复节点不属于任何摊位 → 静默忽略（不产生错误日志）

---

## Acceptance Criteria

### F.2 Stall Unlock Check

- [ ] **AC-1**: GIVEN stall.gh-sail-shop required_node_ids=[repair_node.starlight_dock] + completed_node_ids={repair_node.starlight_dock}，WHEN is_stall_unlocked("stall.gh-sail-shop", completed_node_ids)，THEN 返回 true。1 个匹配 ≥ unlock_threshold_basic(1)
- [ ] **AC-2**: GIVEN stall.gh-lens-workshop required_node_ids=[repair_node.old_lighthouse] + completed_node_ids={repair_node.starlight_dock}，WHEN is_stall_unlocked("stall.gh-lens-workshop", completed_node_ids)，THEN 返回 false。无交集
- [ ] **AC-3**: GIVEN stall.gh-chart-studio required_node_ids=[] + completed_node_ids={repair_node.starlight_dock}，WHEN is_stall_unlocked()，THEN 返回 false。空 required_node_ids → 永不自动解锁
- [ ] **AC-4**: GIVEN stall required_node_ids=[repair_node.starlight_dock, repair_node.old_lighthouse] + completed_node_ids={repair_node.starlight_dock}（仅 1 个匹配），WHEN is_stall_unlocked()，THEN 返回 true。unlock_threshold_basic=1 → 满足 1 个即解锁

### Completed Node IDs Management

- [ ] **AC-5**: GIVEN completed_node_ids=[]，WHEN on_repair_completed("repair_node.starlight_dock") 且 linked_location_id 匹配 glass-harbor，THEN completed_node_ids 变为 ["repair_node.starlight_dock"]
- [ ] **AC-6**: GIVEN completed_node_ids 已包含 "repair_node.starlight_dock"，WHEN 再次收到相同 repair_completed("repair_node.starlight_dock")，THEN completed_node_ids 保持不变。集合去重——重复信号不产生重复条目
- [ ] **AC-7**: GIVEN completed_node_ids=["repair_node.starlight_dock"]，WHEN 收到不匹配该 settlement 的 repair_completed("repair_node.far_away_beacon")，THEN completed_node_ids 保持不变。不相关的修复不影响

### Repair-Driven Stall Unlock

- [ ] **AC-8**: GIVEN stall.gh-sail-shop=CLOSED + npc.yun=ABSENT + completed_node_ids 新加入 repair_node.starlight_dock（匹配 required_node_ids），WHEN on_repair_completed() 处理后，THEN stall_state→OPEN_BASIC + npc_state→IDLE。摊位解锁 + NPC 恢复原子绑定
- [ ] **AC-9**: GIVEN stall.gh-sail-shop 已是 OPEN_BASIC + 重复 repair_completed 到达，WHEN 处理，THEN stall_state 保持 OPEN_BASIC。不重复触发转换
- [ ] **AC-10**: GIVEN 单个 repair_completed(node_id) 的 node_id 同时匹配 2 个摊位的 required_node_ids（如 repair_node.grand_bazaar），WHEN 处理，THEN 两个摊位各自独立 CLOSED→OPEN_BASIC + 对应 NPC ABSENT→IDLE

### F.3 Settlement Activity Aggregation

- [ ] **AC-11**: GIVEN active_stall_count=1（仅杂货摊），WHEN recalculate_settlement_activity("settlement.glass-harbor")，THEN settlement_state=DORMANT。active_stall_count=1 ≤ dormant_max_stalls(1)
- [ ] **AC-12**: GIVEN active_stall_count=2（杂货摊 + 1 个修复解锁），WHEN recalculate_settlement_activity()，THEN settlement_state→RECOVERING。1 < 2 < total(4)
- [ ] **AC-13**: GIVEN active_stall_count=3，WHEN recalculate_settlement_activity()，THEN 保持 RECOVERING。未达到 total_stall_count(4)
- [ ] **AC-14**: GIVEN active_stall_count=4（全部摊位 open_basic），WHEN recalculate_settlement_activity()，THEN settlement_state→ACTIVE。全部摊位开启
- [ ] **AC-15**: GIVEN active_stall_count=4 → settlement_state=ACTIVE 后，WHEN settlement_activity_changed 信号发射，THEN 参数 (settlement_id="settlement.glass-harbor", active_stall_count=4)

### NPC-Stall Coupling

- [ ] **AC-16**: GIVEN stall_id="stall.gh-lens-workshop" + npc.wei stall_id="stall.gh-lens-workshop"，WHEN 摊位 CLOSED→OPEN_BASIC 触发，THEN npc.wei ABSENT→IDLE。NPC 通过 stall_id 字段关联摊位
- [ ] **AC-17**: GIVEN NPC stall_id 字段引用了不存在的摊位，WHEN 初始化验证，THEN 记录 warning。不崩溃——该 NPC 保持在初始状态

### Edge Conditions — Unlock & Activity

- [ ] **AC-18**: GIVEN 全部 4 个摊位均已 OPEN_BASIC（MVP 终态），WHEN 新的 repair_completed 到达，THEN F.2 判定无 unlocked stall → 信号被安全忽略。无错误
- [ ] **AC-19**: GIVEN repair_completed(node_id) 的 node_id 不属于任何摊位的 required_node_ids，WHEN 处理，THEN 静默忽略。不产生错误——某些修复只影响其他系统

---

## Implementation Notes

### F.2 Stall Unlock Check

```text
func is_stall_unlocked(stall_id: StringName, completed_node_ids: Array) -> bool:
    var stall_def := _get_stall_def(stall_id)
    if stall_def.is_empty():
        return false

    var required: Array = stall_def.get("required_node_ids", [])
    if required.size() == 0:
        return false  # EC-11: 空 required_node_ids → 永不自动解锁

    # unlock_threshold_basic = 1 — 至少 1 个匹配
    for node_id in required:
        if node_id in completed_node_ids:
            return true

    return false
```

### Repair Completed Handler

```text
func on_repair_completed(node_id: StringName) -> void:
    # 1. 查询 node_id 的 linked_location_id
    var repair_def := Registry.query_entity(node_id)
    if repair_def.is_empty():
        return  # 未知修复节点——静默忽略

    var linked_location_id: StringName = repair_def.get("linked_location_id", &"")
    if linked_location_id == &"":
        return  # 无关联位置——静默忽略

    # 2. 匹配 linked_location_id 所属 settlement
    var target_settlement_id: StringName = &""
    for sid in settlements:
        var s_def := Registry.query_entity(sid)
        var linked_ids: Array = s_def.get("linked_location_ids", [])
        if linked_location_id in linked_ids:
            target_settlement_id = sid
            break

    if target_settlement_id == &"":
        return  # linked_location_id 不属于任何定居点

    var s := settlements[target_settlement_id]

    # 3. 将 node_id 加入 completed_node_ids（集合去重）
    if node_id not in s["completed_node_ids"]:
        s["completed_node_ids"].append(node_id)
    else:
        return  # 重复信号——已处理

    # 4. 遍历该 settlement 所有摊位，检查解锁
    var any_unlocked := false
    for stall_id in _get_settlement_stalls(target_settlement_id):
        if get_stall_state(stall_id) == STALL_CLOSED:
            if is_stall_unlocked(stall_id, s["completed_node_ids"]):
                _transition_stall_state(stall_id, STALL_OPEN_BASIC)
                _unlock_npc_for_stall(stall_id)
                stall_opened.emit(stall_id, target_settlement_id)
                any_unlocked = true

    # 5. 重算 settlement 活跃度（即使无摊位解锁——completed_node_ids 已变更）
    recalculate_settlement_activity(target_settlement_id)

    # 6. 触发持久化快照
    _trigger_snapshot()
```

### F.3 Activity Aggregation

```text
func recalculate_settlement_activity(settlement_id: StringName) -> void:
    var active_count := 0
    var total_stalls := 0
    for stall_id in _get_settlement_stalls(settlement_id):
        total_stalls += 1
        if stalls[stall_id]["stall_state"] >= STALL_OPEN_BASIC:
            active_count += 1

    var new_state: int
    if active_count == 0:
        new_state = SETTLEMENT_DORMANT
    elif active_count < total_stalls:
        new_state = SETTLEMENT_RECOVERING
    else:
        new_state = SETTLEMENT_ACTIVE

    var current: int = settlements[settlement_id]["settlement_state"]
    if new_state != current:
        settlements[settlement_id]["settlement_state"] = new_state
        settlement_activity_changed.emit(settlement_id, active_count)
```

### NPC Unlock Coupled to Stall

```text
func _unlock_npc_for_stall(stall_id: StringName) -> void:
    for npc_id in npcs:
        if npcs[npc_id]["stall_id"] == stall_id:
            if npcs[npc_id]["npc_state"] == NPC_ABSENT:
                _transition_npc_state(npc_id, NPC_IDLE)
            return  # 一个摊位只有一个 NPC
```

### Settlement Stalls Query

```text
func _get_settlement_stalls(settlement_id: StringName) -> Array:
    var result: Array = []
    for stall_id in stalls:
        if stalls[stall_id]["settlement_id"] == settlement_id:
            result.append(stall_id)
    return result

func _get_stall_def(stall_id: StringName) -> Dictionary:
    return Registry.query_entity(stall_id)
```

---

## Out of Scope

- repair_completed 信号发射——属于 world-repair Epic (#13)
- Registry.query_entity() 实现——属于 content-registry Epic
- linked_location_id 在修复节点定义中的配置——属于 world-repair GDD
- repair_completed 信号的实际连接（signal.connect）——属于 Story 004
- ADR-0003 快照触发 _trigger_snapshot()——属于 Story 005
- 定居点视觉反馈（dormant 冷色调 → active 暖色调）——属于 #17 Feedback 系统

---

## QA Test Cases

- **AC-1**: Exact required_node match → unlocked
- **AC-2**: No intersection → not unlocked
- **AC-3**: Empty required_node_ids → never auto-unlock
- **AC-4**: 1 of 2 required → unlocked (threshold=1)
- **AC-5**: New repair → completed_node_ids updated
- **AC-6**: Duplicate repair signal → idempotent
- **AC-7**: Unrelated repair → ignored
- **AC-8**: Repair match → stall OPEN_BASIC + NPC IDLE (atomic)
- **AC-9**: Already open stall + repair → no change
- **AC-10**: Single repair matching 2 stalls → both unlock
- **AC-11**: 1 active → DORMANT
- **AC-12**: 2 active → RECOVERING
- **AC-13**: 3 active → RECOVERING
- **AC-14**: 4 active → ACTIVE
- **AC-15**: Activity signal emitted correctly
- **AC-16**: NPC coupled to stall
- **AC-18**: All stalls at MVP max → new repair ignored
- **AC-19**: Unrelated repair node → silently ignored

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/settlement-market/UnlockActivityTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine, _transition_stall_state, _transition_npc_state), Story 002 (F.1 formula not needed here, but conceptual continuity), content-registry Epic (query_entity for repair_node, settlement, stall)
- Unlocks: Story 004 (signal wiring with actual repair_completed), Story 005 (persistence of completed_node_ids)
