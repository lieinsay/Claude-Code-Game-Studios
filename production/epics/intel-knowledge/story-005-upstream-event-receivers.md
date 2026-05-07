# Story 005: Upstream Event Receivers

> **Epic**: Intel / Knowledge System
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-001`, `TR-intel-002`, `TR-intel-003`

**ADR Governing Implementation**: ADR-0007 (IntelManager Autoload #6)
**ADR Decision Summary**: IntelManager 接收来自 6 个上游系统的 8 种事件方法。所有方法为同步调用（request-response 语义——非 fire-and-forget 信号）。每个事件方法完成后调用 `_reevaluate_ability_unlocks()` 遍历所有 locked 能力。方法防御性校验输入——无效输入记录 warning/error 日志但不崩溃。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 所有事件接收方法同步执行；完成后调用 _reevaluate_ability_unlocks()
- Forbidden: 事件接收方法返回前未调用 _reevaluate_ability_unlocks()
- Guardrail: consume_intel() 由 ResourcesManager 调用——ResourcesManager 拥有消耗 UI，IntelManager 拥有算法

---

## Acceptance Criteria

### report_observation_event()

- [ ] **AC-1**: GIVEN 有效 pattern_id + event_id，WHEN `report_observation_event()`，THEN observation_score 正确累加（按 event_type 权重），triggered_events 追加 event_id
- [ ] **AC-2**: GIVEN 无效 pattern_id（非 pattern.* 命名空间或不在注册表），WHEN `report_observation_event()`，THEN 记录 warning："unregistered observation event"，不崩溃
- [ ] **AC-3**: GIVEN 事件触发后，WHEN 方法返回，THEN `_reevaluate_ability_unlocks()` 已被调用（可能满足 Path C/D 观测条件的能力被解锁）

### report_pattern_usage_success()

- [ ] **AC-4**: GIVEN pattern 状态为 confirmed, pattern_usage_success 刚被设为 true，WHEN `report_pattern_usage_success()`，THEN is_confirmed_plus 变为 true，pattern_usage_confirmed signal emit
- [ ] **AC-5**: GIVEN pattern 状态为 partially_observed (score=7)，WHEN `report_pattern_usage_success()`，THEN pattern_usage_success 持久化标记为 true，但 is_confirmed_plus 仍为 false（confirmed 前置条件未满足）

### report_navigation_event()

- [ ] **AC-6**: GIVEN event_type="fog_traversal_completed"，WHEN `report_navigation_event()`，THEN fog_traversal_count += 1。若 count 达到 3，Path C 雾中穿行能力解锁
- [ ] **AC-7**: GIVEN event_type 为其他已知类型（如 "route_travel_completed"），WHEN `report_navigation_event()`，THEN 不执行特殊逻辑——为 GDD Part 7 中预留的航行事件扩展点

### on_partner_joined() / on_partner_left()

- [ ] **AC-8**: GIVEN partner_id="partner.old-sailor" 不在 active_crew 中，WHEN `on_partner_joined(&"partner.old-sailor")`，THEN active_crew 追加该 ID，随后 _reevaluate_ability_unlocks() 检查可能因伙伴在场而满足的能力路径
- [ ] **AC-9**: GIVEN partner_id 已在 active_crew 中，WHEN `on_partner_joined()` 重复调用，THEN active_crew 不重复追加
- [ ] **AC-10**: GIVEN partner_id 在 active_crew 中，WHEN `on_partner_left(partner_id)`，THEN 从 active_crew 移除该 ID。注意：已因该伙伴解锁的能力保持 unlocked——能力不可退化

### on_repair_completed()

- [ ] **AC-11**: GIVEN repair_node_id="repair_lighthouse_01"，WHEN `on_repair_completed()`，THEN _completed_repairs 追加该 ID，_reevaluate_ability_unlocks() 检查灯塔信号解读 Path C 是否满足
- [ ] **AC-12**: GIVEN repair_node_id 已在 _completed_repairs 中，WHEN `on_repair_completed()` 重复调用，THEN 不重复追加

### player_arrived_at() — Full Integration

- [ ] **AC-13**: GIVEN player_arrived_at() 将地点推进至 verified，WHEN 方法返回，THEN _reevaluate_ability_unlocks() 检查可能因地点访问而满足的能力路径（如灯塔 Path D）
- [ ] **AC-14**: GIVEN player_arrived_at() 的 location_id 不在注册表中（动态生成的地点），WHEN 调用，THEN 记录 warning："location_id [X] not found in Registry"，但仍推进至 verified（不阻塞——可能是探索产生的动态地点）

---

## Implementation Notes

### Method Signatures (Full Set)

```gdscript
# 8 个上游事件接收方法:

# 1. 情报消耗 — 由 ResourcesManager 调用
func consume_intel(intel_id: StringName) -> Dictionary:
    # [Story 004 已实现] → 返回 IntelConsumeResult
    # 完成后调用 _reevaluate_ability_unlocks()

# 2. 传闻接收 — 由伙伴系统调用
func reveal_rumor(location_id: StringName, source_tag: StringName, hazard_tags: Array, confidence: int) -> void:
    # [Story 002 已实现] → 推进地点知识状态
    # 完成后调用 _reevaluate_ability_unlocks()

# 3. 玩家到达 — 由移动系统调用
func player_arrived_at(location_id: StringName) -> void:
    # [Story 002 已实现] → 推进至 verified
    # 完成后调用 _reevaluate_ability_unlocks()

# 4. 观测事件报告 — 由探索/航行/伙伴/交互系统调用
func report_observation_event(pattern_id: StringName, event_id: StringName) -> void:
    # [Story 001 已实现] → 累加 observation_score
    # 完成后调用 _reevaluate_ability_unlocks()

# 5. 规律使用成功 — 由探索/航行系统调用
func report_pattern_usage_success(pattern_id: StringName) -> void:
    # [Story 001 已实现] → 设置 pattern_usage_success
    # 完成后调用 _reevaluate_ability_unlocks()

# 6. 航行事件 — 由航行系统调用
func report_navigation_event(event_type: StringName, payload: Dictionary) -> void:
    match event_type:
        &"fog_traversal_completed":
            fog_traversal_count += 1
        &"route_travel_completed":
            pass  # 预留扩展点
        &"player_entered_zone":
            pass
        &"player_hit_obstacle":
            pass
        _:
            push_warning("unhandled navigation event type: %s" % event_type)
    _reevaluate_ability_unlocks()

# 7. 伙伴加入 — 由伙伴系统调用
func on_partner_joined(partner_id: StringName) -> void:
    if partner_id not in active_crew:
        active_crew.append(partner_id)
    _reevaluate_ability_unlocks()

# 8. 伙伴离开 — 由伙伴系统调用
func on_partner_left(partner_id: StringName) -> void:
    var idx: int = active_crew.find(partner_id)
    if idx != -1:
        active_crew.remove_at(idx)
    # 注意：不调用 _reevaluate_ability_unlocks()
    # 已解锁能力不可退化——伙伴离开不能锁回能力

# 9. 修复完成 — 由修复系统调用
func on_repair_completed(repair_node_id: StringName) -> void:
    if repair_node_id not in _completed_repairs:
        _completed_repairs.append(repair_node_id)
    _reevaluate_ability_unlocks()
```

### Re-evaluation Entry Point

```gdscript
# 在每个事件方法末尾统一调用
func _reevaluate_ability_unlocks() -> void:
    for ability_id in ability_unlock_paths:
        if ability_state.get(ability_id, ABILITY_LOCKED) == ABILITY_LOCKED:
            check_unlock_conditions(ability_id)
```

### Additional State Variables

```gdscript
# 伙伴相关
var active_crew: Array = []  # Array[StringName]

# 修复记录
var _completed_repairs: Array = []  # Array[StringName]

# 雾气穿越计数
var fog_traversal_count: int = 0
```

### Defensive Validation

```gdscript
func _validate_pattern_id(pattern_id: StringName) -> bool:
    # pattern_id 必须在 event_weight_table 中有定义
    return _event_weight_table.has(pattern_id)

func _validate_location_id(location_id: StringName) -> void:
    if not _registry.has_location(location_id):
        push_warning("location_id %s not found in Registry — treating as dynamic location" % location_id)
```

---

## Out of Scope

- 各上游系统的事件触发判定（探索系统拥有"何时触发 bird-passive-island"的判定逻辑）
- 航行系统的 fog_traversal 判定（"成功穿越"的定义由航行系统拥有）
- 伙伴系统的伙伴管理（伙伴系统拥有伙伴数据，仅通过 on_partner_joined/left 通知）
- 修复系统的修复管理（修复系统拥有修复状态，仅通过 on_repair_completed 通知）

---

## QA Test Cases

- **AC-6**: Fog traversal counting
  - Given: fog_traversal_count = 2
  - When: report_navigation_event("fog_traversal_completed", {})
  - Then: fog_traversal_count = 3, _reevaluate_ability_unlocks() called, ability.fog-navigation unlocked via Path C
  - Edge case: fog_traversal_count 已达 5 → 继续累加（无上限），能力保持 unlocked

- **AC-8 and AC-10**: Partner join/leave
  - Given: active_crew = []
  - When: on_partner_joined("partner.old-sailor") → active_crew = ["partner.old-sailor"], re-evaluation checks bird-flight Path C
  - When: on_partner_left("partner.old-sailor") → active_crew = []
  - Then: 能力保持解锁（若此前因老水手 Path C 解锁）

- **AC-5**: Early pattern_usage_success
  - Given: pattern.bird-flight-direction score=7 (partially_observed)
  - When: report_pattern_usage_success("pattern.bird-flight-direction")
  - Then: pattern_usage_success=true, is_confirmed_plus=false
  - Later: observation_score reaches 11 → state=confirmed, is_confirmed_plus automatically becomes true

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/intel/event_receivers_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-004 (all core logic), content-registry Epic (Registry validation)
- Unlocks: Story 007 (signals emitted by event receivers), external system integration (Exploration/Navigation/Partner/Repair systems can call these methods)
