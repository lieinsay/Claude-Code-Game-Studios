# Story 007: External State Change Response

> **Epic**: Chart / Route Planning
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-001`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — Section 8 External State Change Response), ADR-0007 (IntelManager — knowledge_advanced/ability_unlocked signals), ADR-0011 (WorldRepair — repair_completed signal)
**ADR Decision Summary**: Chart 在 Phase 3b core_data_ready 中连接上游系统的信号以响应航线相关状态的外部变化。IntelManager.knowledge_advanced → _on_knowledge_changed()：重新评估所有涉及该地点的航线的可选择性。IntelManager.ability_unlocked → _on_ability_changed()：重新评估所有航线的 traversable 条件。WorldRepair.repair_completed → _on_repair_completed()：评估哪些航线因修复完成而受益，发射 route_enhanced 信号。响应顺序：先重新计算路由状态，再响应 UNAVAILABLE→BROWSABLE 的视觉变化。若知识状态被撤销（knowledge→unknown），航线必须强制取消选择（ROUTE_SELECTED→BROWSING）并从可见列表中移除。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: signal 连接在 Phase 3b core_data_ready 中执行（ADR-0001 要求）；知识状态变化时立即重新评估——不等待下一帧或轮询间隔；强制取消选择时发射 chart_state_changed 信号
- Forbidden: 在 _on_knowledge_changed/_on_ability_changed 中直接修改 IntelManager 状态（只读合约）；在评估期间阻塞帧——MVP 2 条航线 O(N)=2 评估 < 0.01ms
- Guardrail: 信号回调中的空值防御——始终通过 _safe_query_knowledge() 和 _query_route_accessibility() 查询

---

## Acceptance Criteria

### Signal Connection Setup

- [ ] **AC-1**: GIVEN Chart Phase 3b core_data_ready，WHEN 初始化，THEN 连接 IntelManager.knowledge_advanced → _on_knowledge_changed，IntelManager.ability_unlocked → _on_ability_changed，WorldRepair.repair_completed → _on_repair_completed

### Knowledge State Change

- [ ] **AC-2**: GIVEN 航图 BROWSING + 航线 A (knowledge=identified) 涉及地点 L1，WHEN IntelManager 发射 knowledge_advanced(L1, previous, new_state)，THEN _on_knowledge_changed() 重新评估所有 origin/destination 为 L1 的航线的 route_selectability。若 new_state=verified → 航线 A 仍 BROWSABLE。若 new_state=unknown → 航线 A 的 route_visibility → false → _visible_routes 移除 → 若为已选则强制取消选择
- [ ] **AC-3**: GIVEN 航图 ROUTE_SELECTED + 已选航线 knowledge=identified，WHEN IntelManager 将 knowledge 更新为 unknown，THEN 强制取消选择：chart_state ROUTE_SELECTED→BROWSING。航线从航图消失。chart_state_changed 发射。通知："航线 [名称] 的情报已失效——该航线的知识来源不再可信。"

### Ability Unlock

- [ ] **AC-4**: GIVEN 航线 X 因 ability "deep_navigation" 未解锁 → traversable=false → UNAVAILABLE，WHEN IntelManager 发射 ability_unlocked("deep_navigation", ...)，THEN _on_ability_changed() → _reevaluate_all_routes_accessibility()。航线 X traversable→true → UNAVAILABLE→BROWSABLE。颜色从灰恢复为正常——过渡动画应由 UI 系统根据 selectability 变化触发
- [ ] **AC-5**: GIVEN 已选航线 + 其 traversable 因能力解锁而保持 true，WHEN ability_unlocked 触发，THEN _reevaluate_all_routes_accessibility() 不影响已选航线——其 selectability 仍为 "selected"。不强制取消选择

### World Repair Completed

- [ ] **AC-6**: GIVEN 航线 Y 受益于某个世界节点的修复（如灯塔修复），WHEN WorldRepair 发射 repair_completed(node_id)，THEN _on_repair_completed() 评估 _evaluate_route_enhancements(node_id)。若 node_id 影响航线 Y 的 hazard_tags（降低风险）或 traversable（恢复通行），则发射 route_enhanced(route_id, node_id)
- [ ] **AC-7**: GIVEN 修复完成的 node_id 不对任何航线产生影响，WHEN _on_repair_completed()，THEN _evaluate_route_enhancements() 返回空数组。不发射任何 route_enhanced 信号。不重新评估所有航线（性能——仅评估受影响的节点）

### Docked Location Change

- [ ] **AC-8**: GIVEN 航图 BROWSING + 当前停靠 glass-harbor，WHEN AirshipHub 报告停靠地点变更为 other-port，THEN _reevaluate_all_routes_accessibility()。原起点为 glass-harbor 的航线 → UNAVAILABLE（origin≠docked）。起点为 other-port 的航线 → BROWSABLE（若满足条件）。若已选中航线变为 UNAVAILABLE → 强制取消选择
- [ ] **AC-9**: GIVEN 航图关闭（非 BROWSING/ROUTE_SELECTED），WHEN 外部状态变化，THEN 不执行重新评估——航图未渲染时无需响应。状态变化仅标记为待下次 open_chart() 时处理

### Re-evaluate Performance

- [ ] **AC-10**: GIVEN MVP 2 条航线，WHEN _reevaluate_all_routes()/_reevaluate_all_routes_accessibility()，THEN 对每条航线执行最多 1 次 route_selectability() 调用。MVP 下评估完成 < 0.01ms

---

## Implementation Notes

### Signal Connections in core_data_ready

```gdscript
func _on_core_data_ready() -> void:
    # 注册 domain serializer
    register_serializer()

    # 连接上游信号——外部状态变化响应
    if IntelManager.has_signal("knowledge_advanced"):
        IntelManager.knowledge_advanced.connect(_on_knowledge_changed)

    if IntelManager.has_signal("ability_unlocked"):
        IntelManager.ability_unlocked.connect(_on_ability_changed)

    if is_instance_valid(WorldRepair) and WorldRepair.has_signal("repair_completed"):
        WorldRepair.repair_completed.connect(_on_repair_completed)
```

### Knowledge Change Handler

```gdscript
func _on_knowledge_changed(location_id: StringName, _prev: int, _new: int) -> void:
    # 仅在航图打开期间响应
    if _state["_chart_state"] not in [&"BROWSING", &"ROUTE_SELECTED"]:
        return

    var affected_routes: Array[StringName] = []
    for route_id in _state["_visible_routes"]:
        var origin: StringName = Registry.get_route_origin(route_id)
        var destination: StringName = Registry.get_route_destination(route_id)
        if origin == location_id or destination == location_id:
            affected_routes.append(route_id)

    var selected_id: StringName = _state["_selected_route_id"] if _state["_selected_route_id"] != null else &""

    for route_id in affected_routes:
        # 检查知识状态是否导致航线变为不可见
        var knowledge: int = _query_knowledge_state(route_id)
        if knowledge == KNOWLEDGE_UNKNOWN:
            # 航线从可见列表移除
            _state["_visible_routes"].erase(route_id)
            _state["_route_states"].erase(route_id)

            # 若为已选航线——强制取消选择
            if route_id == selected_id:
                _force_deselect(&"knowledge_revoked")
                selected_id = &""  # 防止后续循环再次取消选择
            continue

        # 知识状态变更但航线仍可见——更新可选择性
        var selectability: StringName = route_selectability(route_id)
        _set_route_sub_state(route_id, selectability)

    # 若取消选择后有剩余可选航线——保持 BROWSING；否则显示空航图消息
```

### Ability Unlock Handler

```gdscript
func _on_ability_changed(_ability_id: StringName, _unlock_path: StringName) -> void:
    if _state["_chart_state"] not in [&"BROWSING", &"ROUTE_SELECTED"]:
        return

    _reevaluate_all_routes_accessibility()


func _reevaluate_all_routes_accessibility() -> void:
    for route_id in _state["_visible_routes"]:
        var selectability: StringName = route_selectability(route_id)
        _set_route_sub_state(route_id, selectability)
```

### World Repair Handler

```gdscript
func _on_repair_completed(node_id: StringName) -> void:
    if _state["_chart_state"] not in [&"BROWSING", &"ROUTE_SELECTED"]:
        return

    var enhanced_routes: Array[StringName] = _evaluate_route_enhancements(node_id)
    for route_id in enhanced_routes:
        route_enhanced.emit(route_id, node_id)

    # 增强可能改变了可选择性——重新评估
    if enhanced_routes.size() > 0:
        _reevaluate_all_routes_accessibility()


func _evaluate_route_enhancements(node_id: StringName) -> Array[StringName]:
    var enhanced: Array[StringName] = []
    for route_id in _state["_visible_routes"]:
        # 查询该修复节点是否为该航线 risk_tag 相关的障碍
        var accessibility: Dictionary = _query_route_accessibility(route_id)
        var related_nodes: Array = accessibility.get("related_world_nodes", [])
        if node_id in related_nodes:
            enhanced.append(route_id)
    return enhanced
```

### Docked Location Change Detection

```gdscript
# Chart 缓存上次已知停靠地点，在每次 get_visible_routes/route_selectability 前检查
var _cached_docked_location: StringName = &""

func _check_docked_location_changed() -> bool:
    var current: StringName = _get_current_docked_location_safe()
    if current != _cached_docked_location:
        _cached_docked_location = current
        if _state["_chart_state"] in [&"BROWSING", &"ROUTE_SELECTED"]:
            _reevaluate_all_routes_accessibility()
            # 若已选航线变为 UNAVAILABLE
            if _state["_selected_route_id"] != null:
                var selected_id: StringName = _state["_selected_route_id"]
                if route_selectability(selected_id) == &"unavailable":
                    _force_deselect(&"dock_changed")
        return true
    return false
```

---

## Out of Scope

- IntelManager 的 knowledge_advanced/ability_unlocked 信号发射逻辑——属于 IntelManager #6
- WorldRepair 的 repair_completed 信号发射逻辑——属于 WorldRepair #13
- AirshipHub 的 docked_location 变更检测——属于 AirshipHub #7
- UNAVAILABLE→BROWSABLE 的颜色过渡动画（0.3s）——属于 UI 系统 #16
- 通知/消息的具体 UI 文案和显示方式——属于 UI 系统 #16

---

## QA Test Cases

- **AC-2**: Knowledge change
  - Given: BROWSING + route A identified via L1 → knowledge_advanced(L1, *, unknown) → route A 从可见列表移除

- **AC-3**: Select + knowledge revoked
  - Given: ROUTE_SELECTED + selected route knowledge→unknown → forced deselect → BROWSING → chart_state_changed 发射

- **AC-4**: Ability unlock
  - Given: route X unavailable (traversable=false) → ability_unlocked → route X→BROWSABLE

- **AC-6**: World repair
  - Given: node_id 影响 route Y → repair_completed → route_enhanced(route_Y, node_id) 发射

- **AC-8**: Dock change
  - Given: BROWSING + docked=glass-harbor → dock→other-port → origin=glass-harbor 航线→UNAVAILABLE

- **AC-9**: Chart closed
  - Given: chart_state not BROWSING/ROUTE_SELECTED → external change → no-op（不重新评估）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/chart/external_state_response_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (chart_state, _visible_routes), Story 002 (route_selectability, _reevaluate), Story 006 (route_enhanced signal), IntelManager #6 (knowledge_advanced/ability_unlocked signals), WorldRepair #13 (repair_completed signal), AirshipHub #7 (docked_location)
- Unlocks: Story 008 (EC-5/6/7/8 external state edge cases)
