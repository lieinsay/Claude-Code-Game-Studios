# Story 003: Two-Step Departure Confirmation & route_committed Signal

> **Epic**: Chart / Route Planning
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-003`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — Formula 3 chart_state_transition + Section 5 route_committed 不可逆出航承诺)
**ADR Decision Summary**: 出航采用两步确认流程——第一步刷新风险数据并展示最终摘要浮层，第二步承诺出航。_commit_departure() 执行顺序：刷新 accessibility → 构建+校验快照包 → 状态转换为 DEPARTURE_CONFIRMED（终端）→ 发射 route_committed 信号 → 触发快照写入。route_committed 信号签名为 (route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName])。单次发射保证由状态机终端守卫提供——第一个 CONFIRM 触发后状态变为 DEPARTURE_CONFIRMED，第二个命中终端守卫返回 allowed:false。配套 fail 信号 route_selection_failed(route_id, reason) 在 traversable 检查和快照校验失败时发射。出航锁定时长 base_lock_duration 默认 2.0s。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 两步确认——第一步必须刷新 query_route_accessibility() 获取最新风险数据；第二步 CONFIRM 触发状态转换；route_committed 信号 typed params (StringName, StringName, Array[StringName]) sync emit；快照校验失败时不发射 route_committed、不转换状态、回退至 ERROR
- Forbidden: 跳过第一步直接 CONFIRM；同帧两次 CONFIRM 产生两次 route_committed 发射（终端守卫防止）；route_committed 携带过时 hazard_tags（第一步已刷新）
- Guardrail: 确认浮层内容由 UI 系统 #16 渲染——Chart 只提供数据；墨水扩散动画由 UIManager 执行——Chart 只提供动画触发信号

---

## Acceptance Criteria

### Two-Step Confirmation Flow

- [ ] **AC-1**: GIVEN chart_state=ROUTE_SELECTED + 已选航线 traversable=true，WHEN 玩家点击"确认出航"（第一步），THEN 重新查询 query_route_accessibility() 和 query_route_knowledge() 获取最新数据。确认浮层弹出——展示航线名称、刷新后风险摘要、预估距离带、"出航"/"取消"按钮
- [ ] **AC-2**: GIVEN 确认浮层显示中 + 玩家点击"取消"，WHEN 点击"取消"，THEN 浮层关闭。chart_state 保持 ROUTE_SELECTED。不发射 route_committed
- [ ] **AC-3**: GIVEN 确认浮层显示中 + 玩家点击"出航"（第二步），WHEN 第二步确认，THEN chart_state: ROUTE_SELECTED → DEPARTURE_CONFIRMED。route_committed 信号发射恰好一次——携带 route_id, destination_id, hazard_tags。所有航线子状态 → LOCKED。出航锁定开始（2.0s）。墨迹扩散动画触发（1.5s）

### Single-Emit Guarantee

- [ ] **AC-4**: GIVEN DEPARTURE_CONFIRMED 已进入，WHEN 同一帧内第二个 CONFIRM 触发到达，THEN chart_state_transition() 终端守卫返回 {allowed: false}。不产生第二个 route_committed 信号。不创建第二份快照
- [ ] **AC-5**: GIVEN DEPARTURE_CONFIRMED 已进入 + 锁定 2.0s 期间，WHEN 快速连点（10+次 <100ms 间隔），THEN 所有输入被 route_selectability 分支 2 拦截（locked）。状态保持 DEPARTURE_CONFIRMED。route_committed 仅发射一次

### Step 1 Refresh Guarantee

- [ ] **AC-6**: GIVEN 选中航线时 risk=safe（绿色），WHEN 点击"确认出航"（第一步）时 query_route_accessibility 返回 hazard_tags 新增 "pirate_activity"，THEN 确认浮层展示当前风险（红色，含 pirate_activity）。而非选中时的过时数据（绿色 safe）
- [ ] **AC-7**: GIVEN 选中航线时 traversable=true，WHEN 点击"确认出航"（第一步）时 traversable 变为 false，THEN 确认被阻止。route_selection_failed(route_id, "route_not_traversable") 发射。chart_state 强制取消选择（ROUTE_SELECTED → BROWSING）。通知："航线 [名称] 状态已变更——无法出航。"

### route_committed Signal

- [ ] **AC-8**: GIVEN 出航确认成功，WHEN route_committed 发射，THEN 信号签名包含 3 个 typed 参数: route_id (StringName), destination_id (StringName), hazard_tags (Array[StringName])。hazard_tags 为刷新后的最新值
- [ ] **AC-9**: GIVEN route_committed 发射后，WHEN 检查信号消费者，THEN fan-out 在同步 emit 调用栈内完成。不依赖 call_deferred

### route_selection_failed Signal

- [ ] **AC-10**: GIVEN 快照校验失败，WHEN _commit_departure() 中 snapshot_package_validity() 返回 valid=false，THEN route_selection_failed(route_id, "snapshot_invalid") 发射。chart_state → ERROR。不发射 route_committed
- [ ] **AC-11**: GIVEN 第一步刷新时 traversable=false，WHEN _commit_departure() 检测到，THEN route_selection_failed(route_id, "route_not_traversable") 发射。chart_state → BROWSING（强制取消选择）

### Departure Lock

- [ ] **AC-12**: GIVEN DEPARTURE_CONFIRMED 进入，WHEN 锁定开始，THEN _departure_lock_remaining = base_lock_duration（默认 2.0s）。锁定期间所有航图交互禁用——click/Tab/Enter/Esc 无效
- [ ] **AC-13**: GIVEN 锁定 2.0s 结束，WHEN 锁定结束，THEN 控制权移交 Navigation #10。航图关闭（chart_state 保持在 DEPARTURE_CONFIRMED——航图不再可用）

---

## Implementation Notes

### signal Declarations

```text
## 出航承诺信号 — 单次发射，不可逆
signal route_committed(route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName])

## 出航确认失败信号 — 配对的 fail 信号（ADR-0002 要求）
signal route_selection_failed(route_id: StringName, reason: StringName)

## 航图状态变更信号
signal chart_state_changed(old_state: StringName, new_state: StringName)
```

### _commit_departure() — Full Flow

```text
func _commit_departure(route_id: StringName) -> void:
    # Step 1: 刷新风险数据 — 两步确认第一步必须展示最新状态
    var accessibility: Dictionary = _query_route_accessibility(route_id)
    if not accessibility.get("traversable", false):
        _force_deselect(&"route_not_traversable")
        route_selection_failed.emit(route_id, &"route_not_traversable")
        return

    # Step 2: 构建并校验快照包
    var pkg: SnapshotPackage = _build_snapshot_package(route_id)
    var timestamp_tolerance: float = Registry.get_constant(&"base_timestamp_tolerance", 300.0)
    var route_registry: Array = Registry.list_by_kind(&"route")
    var current_time: float = Time.get_unix_time_from_system()

    var validation: Dictionary = snapshot_package_validity(
        pkg, current_time, timestamp_tolerance, route_registry
    )
    if not validation["valid"]:
        _enter_error_state(validation["violations"])
        route_selection_failed.emit(route_id, &"snapshot_invalid")
        return

    # Step 3: 状态转换 — 终端状态，不可逆
    var old_state: StringName = _state["_chart_state"]
    _state["_chart_state"] = &"DEPARTURE_CONFIRMED"
    _state["_last_committed_route_id"] = route_id
    _state["_last_departure_timestamp"] = current_time
    _state["_departure_lock_remaining"] = Registry.get_constant(&"base_lock_duration", 2.0)
    _set_all_routes_locked()

    chart_state_changed.emit(old_state, &"DEPARTURE_CONFIRMED")

    # Step 4: 发射 route_committed — 单次 emit，同步 fan-out
    var route_data: Dictionary = Registry.get_route_data(route_id)
    route_committed.emit(
        route_id,
        route_data.get("destination_location_id", &""),
        accessibility.get("hazard_tags", [])
    )

    # Step 5: 触发快照写入
    Persistence.request_save(SAVE_TRIGGER_DEPARTURE)
```

### Two-Step Confirmation Entry Points

```text
func request_confirm_departure(route_id: StringName) -> Dictionary:
    """Step 1: 玩家点击'确认出航'按钮 — 刷新数据并返回最终摘要"""
    if _state["_chart_state"] != &"ROUTE_SELECTED":
        return {"confirmed": false, "reason": "not in ROUTE_SELECTED state"}

    if _state["_selected_route_id"] != route_id:
        return {"confirmed": false, "reason": "route not selected"}

    # 刷新最新数据
    var accessibility: Dictionary = _query_route_accessibility(route_id)
    var knowledge: Dictionary = IntelManager.query_route_knowledge(route_id)
    var route_data: Dictionary = Registry.get_route_data(route_id)

    return {
        "confirmed": false,  # 第一步不确认——让 UI 展示浮层
        "step": 1,
        "route_id": route_id,
        "route_name": route_data.get("name", ""),
        "hazard_tags": accessibility.get("hazard_tags", []),
        "traversable": accessibility.get("traversable", false),
        "distance_band": route_data.get("distance_band", &"medium"),
    }


func confirm_departure(route_id: StringName) -> void:
    """Step 2: 玩家在确认浮层中点击'出航'"""
    # 终端守卫由 _commit_departure 内部 + chart_state_transition 双重保证
    _commit_departure(route_id)
```

### Forced Deselect

```text
func _force_deselect(reason: StringName) -> void:
    if _state["_chart_state"] != &"ROUTE_SELECTED":
        return

    var deselected_id: StringName = _state["_selected_route_id"]
    _state["_selected_route_id"] = null
    _state["_chart_state"] = &"BROWSING"

    if not deselected_id.is_empty():
        _set_route_sub_state(deselected_id, &"BROWSABLE")

    chart_state_changed.emit(&"ROUTE_SELECTED", &"BROWSING")
```

### _enter_error_state()

```text
func _enter_error_state(violations: Array) -> void:
    _state["_chart_state"] = &"ERROR"
    _state["_retry_cooldown_remaining"] = Registry.get_constant(&"base_retry_cooldown", 2.0)
    _state["_failed_domain_states"]["snapshot_violations"] = violations
    chart_state_changed.emit(&"ROUTE_SELECTED", &"ERROR")
```

---

## Out of Scope

- 确认浮层的 UI 渲染（布局、按钮位置、文本样式）——属于 UI 系统 #16
- 墨迹扩散动画的具体实现（shader/Line2D/AnimatedSprite2D）——属于 UI 系统 #16
- Persistence.request_save() 的原子写入和回退——属于存档系统 #3
- Navigation #10 消费 route_committed 后的航行上下文构建——属于 Navigation #10 + ADR-0010
- 锁定期满后控制权移交的具体场景过渡——属于 Navigation #10 + UI #16

---

## QA Test Cases

- **AC-1 through AC-3**: 两步确认完整流程
  - Given: ROUTE_SELECTED → Step 1 刷新数据 → 浮层数据为最新
  - Given: Step 1 → 取消 → 状态保持 ROUTE_SELECTED
  - Given: Step 2 确认 → DEPARTURE_CONFIRMED → route_committed 发射 1 次

- **AC-4 and AC-5**: 单次发射保证
  - Given: 同帧双 CONFIRM → 第一次成功，第二次终端守卫拒绝
  - Given: 锁定期间连点 10+次 → 全部拦截

- **AC-6 and AC-7**: Step 1 刷新保证
  - Given: 风险变更（safe→pirate_activity）→ 浮层反映新风险
  - Given: traversable 变更（true→false）→ 确认阻止 → route_selection_failed 发射

- **AC-8**: Signal signature
  - Verify: route_committed 3 typed params; route_selection_failed 2 typed params

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/chart/DepartureConfirmationTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (chart_state_transition, terminal guard, state storage), Story 002 (route_selectability 用于验证), Story 005 (snapshot_package_validity, _build_snapshot_package), local-save-persistence Epic (SnapshotPackage, Persistence.request_save)
- Unlocks: Story 006 (route_committed signal contract), Story 007 (departure lock blocks external state changes), Story 008 (EC-3/4/16), Navigation #10 implementation
