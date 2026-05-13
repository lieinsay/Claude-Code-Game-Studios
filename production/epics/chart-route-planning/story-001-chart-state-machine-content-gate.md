# Story 001: Chart State Machine & Content Domain Gate

> **Epic**: Chart / Route Planning
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-001`, `TR-chart-003`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — 5-state hierarchy + 4-state sub-state machine + content domain gate)
**ADR Decision Summary**: ChartManager Autoload #9 管理 Dictionary 状态存储。5 状态航图层级状态机：LOADING → BROWSING → ROUTE_SELECTED → DEPARTURE_CONFIRMED（终端）+ ERROR。6 条合法转换 + 4 条明确禁止转换。4 状态航线子状态机：BROWSABLE / SELECTED / UNAVAILABLE / LOCKED。四大内容域（routes/world/intel/threats）全部 COMPLETE 才能从 LOADING 进入 BROWSING。RETRY cooldown 2.0s 防止 ERROR↔LOADING 紧循环。DEPARTURE_CONFIRMED 是终端状态——任何触发返回 `allowed: false`。未列出的 (state, trigger) 组合默认拒绝。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: Dictionary 状态存储（_chart_state, _route_states, _visible_routes, _selected_route_id）；chart_state_transition() 纯函数——不修改状态，只返回 {new_state, allowed}；四大内容域门控——任一域非 COMPLETE → ERROR；RETRY cooldown 2.0s 由 Registry 常量 base_retry_cooldown 控制
- Forbidden: DEPARTURE_CONFIRMED → 任何其他状态（终端状态不可逆）；ERROR → BROWSING 直达（必须通过 LOADING 重试验证）；UNAVAILABLE → SELECTED（必须先变为 BROWSABLE）；在 chart_state_transition() 中修改 _state（纯函数约束）
- Guardrail: RETRY cooldown 期间重试按钮禁用——防止紧循环；open_chart() 调用入口必须是唯一的状态变更入口——外部不得直接修改 _chart_state

---

## Acceptance Criteria

### Hierarchical State Machine

- [ ] **AC-1**: GIVEN 玩家与海图桌交互锚点交互，WHEN 四大内容域全部 COMPLETE + 航线数据加载成功，THEN chart_state: LOADING → BROWSING。get_chart_state() 返回 &"BROWSING"。航线子状态机初始化——所有可见航线子状态为 BROWSABLE 或 UNAVAILABLE
- [ ] **AC-2**: GIVEN 任一内容域非 COMPLETE（如 threats=FAILED），WHEN 玩家与海图桌交互，THEN chart_state: LOADING → ERROR。ERROR 状态必须包含失败域名称和当前域状态（如 "threats: FAILED"）
- [ ] **AC-3**: GIVEN chart_state=BROWSING，WHEN 玩家选择一条 selectable 航线，THEN chart_state: BROWSING → ROUTE_SELECTED。被选航线的子状态: BROWSABLE → SELECTED
- [ ] **AC-4**: GIVEN chart_state=ROUTE_SELECTED，WHEN 玩家按 Esc 或点击空白区域，THEN chart_state: ROUTE_SELECTED → BROWSING。被选航线子状态: SELECTED → BROWSABLE
- [ ] **AC-5**: GIVEN chart_state=ERROR，WHEN 玩家点击 RETRY 且 cooldown ≤ 0，THEN chart_state: ERROR → LOADING

### Terminal State Guard

- [ ] **AC-6**: GIVEN chart_state=DEPARTURE_CONFIRMED，WHEN 任何触发（SELECT/DESELECT/CONFIRM/RETRY）到达，THEN chart_state_transition() 返回 {new_state: &"DEPARTURE_CONFIRMED", allowed: false}。状态不变——不可逆
- [ ] **AC-7**: GIVEN chart_state=DEPARTURE_CONFIRMED，WHEN 同一帧内两次 CONFIRM 触发，THEN 第一次触发成功转换→DEPARTURE_CONFIRMED，第二次命中终端守卫返回 allowed:false。不产生两次状态转换

### Invalid Transitions Rejected

- [ ] **AC-8**: GIVEN chart_state=ERROR，WHEN COMPLETE 触发到达，THEN 转换被拒绝——ERROR 只能通过 RETRY → LOADING 离开。不允许 ERROR → BROWSING 直达
- [ ] **AC-9**: GIVEN chart_state=LOADING，WHEN SELECT 触发到达，THEN 转换被拒绝——LOADING 只能响应 COMPLETE 或 FAIL。（state, trigger）未在转换表中定义 → 默认返回 {allowed: false}

### Route Sub-State Machine

- [ ] **AC-10**: GIVEN 航线子状态=BROWSABLE + chart_state=BROWSING，WHEN player_select，THEN 子状态 → SELECTED
- [ ] **AC-11**: GIVEN 航线子状态=SELECTED，WHEN player_deselect（Esc 或选另一条航线），THEN 子状态 → BROWSABLE
- [ ] **AC-12**: GIVEN 航线子状态=BROWSABLE，WHEN condition_change（traversable→false 或 origin≠docked_location 或 knowledge→unknown），THEN 子状态 → UNAVAILABLE
- [ ] **AC-13**: GIVEN 航线子状态=UNAVAILABLE，WHEN condition_change（traversable 恢复 + origin==docked_location + knowledge≥rumored），THEN 子状态 → BROWSABLE
- [ ] **AC-14**: GIVEN 任意航线子状态，WHEN chart_state→DEPARTURE_CONFIRMED，THEN 所有航线子状态 → LOCKED（terminal）

### Content Domain Gate

- [ ] **AC-15**: GIVEN open_chart() 调用，WHEN 检查四个内容域，THEN 逐一查询 routes/world/intel/threats 的 domain_state。全部 COMPLETE → 触发 COMPLETE；任一非 COMPLETE → 触发 FAIL。FAIL 时 _failed_domain_states 记录各域当前状态
- [ ] **AC-16**: GIVEN 部分航线 query_route_knowledge() 失败（如 2/5 失败），WHEN 域检查通过但数据查询部分失败，THEN 失败航线视为 knowledge=unknown（不渲染），航图仍进入 BROWSING。_internal_warning_counter 记录失败计数。非阻断通知："部分航线情报读取失败 (2/5)——未知航线未显示。重试？"

### RETRY Cooldown

- [ ] **AC-17**: GIVEN chart_state=ERROR + retry_cooldown_remaining=2.0，WHEN 玩家点击 RETRY，THEN 成功触发 ERROR → LOADING。retry_cooldown_remaining 重置为 Registry 常量 base_retry_cooldown（默认 2.0s）
- [ ] **AC-18**: GIVEN chart_state=ERROR + retry_cooldown_remaining=1.5（冷却中），WHEN 玩家点击 RETRY，THEN chart_state_transition() 返回 {allowed: false}。状态不变。重试按钮在冷却期间禁用

---

## Implementation Notes

### ChartManager Autoload #9 — State Storage

```text
# Chart Autoload #9 — 内部状态 Dictionary
var _state: Dictionary = {
    "_chart_state": &"LOADING",
    "_route_states": {},                   # Dictionary[StringName, StringName]
    "_visible_routes": [],                 # Array[StringName]
    "_selected_route_id": null,            # StringName | null
    "_hide_rumored": false,
    "_last_departure_timestamp": 0.0,
    "_last_committed_route_id": &"",
    "_departure_lock_remaining": 0.0,
    "_retry_cooldown_remaining": 0.0,
    "_failed_domain_states": {},
    "_internal_warning_counter": 0,
}

const CHART_STATES: Array[StringName] = [&"LOADING", &"BROWSING", &"ROUTE_SELECTED", &"DEPARTURE_CONFIRMED", &"ERROR"]
const ROUTE_SUB_STATES: Array[StringName] = [&"BROWSABLE", &"SELECTED", &"UNAVAILABLE", &"LOCKED"]
```

### chart_state_transition() — Pure Function

```text
func chart_state_transition(trigger: StringName, payload: Dictionary = {}) -> Dictionary:
    var current: StringName = _state["_chart_state"]

    # 终端状态守卫 — DEPARTURE_CONFIRMED 不可逆
    if current == &"DEPARTURE_CONFIRMED":
        return {"new_state": current, "allowed": false}

    match current:
        &"LOADING":
            if trigger == &"COMPLETE":
                return {"new_state": &"BROWSING", "allowed": true}
            if trigger == &"FAIL":
                return {"new_state": &"ERROR", "allowed": true}

        &"BROWSING":
            if trigger == &"SELECT":
                var route_id: StringName = payload.get("route_id", &"")
                if route_selectability(route_id) == &"browsable":
                    return {"new_state": &"ROUTE_SELECTED", "allowed": true}

        &"ROUTE_SELECTED":
            if trigger == &"DESELECT":
                return {"new_state": &"BROWSING", "allowed": true}
            if trigger == &"CONFIRM":
                return {"new_state": &"DEPARTURE_CONFIRMED", "allowed": true}

        &"ERROR":
            if trigger == &"RETRY":
                if _state["_retry_cooldown_remaining"] <= 0.0:
                    return {"new_state": &"LOADING", "allowed": true}

    # 默认拒绝 — 未列出的 (state, trigger) 组合
    return {"new_state": current, "allowed": false}
```

### Content Domain Gate

```text
const REQUIRED_DOMAINS: Array[StringName] = [&"routes", &"world", &"intel", &"threats"]

func _check_content_domains() -> bool:
    var all_complete: bool = true
    _state["_failed_domain_states"] = {}

    for domain_id in REQUIRED_DOMAINS:
        var domain_state: StringName = Registry.get_domain_state(domain_id)
        _state["_failed_domain_states"][domain_id] = domain_state
        if domain_state != &"COMPLETE":
            all_complete = false

    return all_complete
```

### open_chart() — Entry Point

```text
func open_chart() -> void:
    _state["_chart_state"] = &"LOADING"
    _state["_internal_warning_counter"] = 0

    # 域门控检查
    if not _check_content_domains():
        _apply_transition(chart_state_transition(&"FAIL"))
        return

    # 加载航线数据 + 批量查询知识状态
    var all_routes: Array = Registry.list_by_kind(&"route")
    var visible: Array[StringName] = []
    var failed_count: int = 0

    for route_id in all_routes:
        var knowledge: int = _safe_query_knowledge(route_id)
        if knowledge < 0:  # 查询失败
            failed_count += 1
            continue
        if knowledge == KNOWLEDGE_UNKNOWN:
            continue
        visible.append(route_id)
        _state["_route_states"][route_id] = &"BROWSABLE"

    _state["_internal_warning_counter"] = failed_count
    _state["_visible_routes"] = visible

    _apply_transition(chart_state_transition(&"COMPLETE"))


func _safe_query_knowledge(route_id: StringName) -> int:
    if not IntelManager.has_method("query_route_knowledge"):
        return -1
    var result: Dictionary = IntelManager.query_route_knowledge(route_id)
    if result.is_empty():
        return -1
    return result.get("state", KNOWLEDGE_UNKNOWN)
```

### State Transition Application

```text
func _apply_transition(result: Dictionary) -> void:
    if not result["allowed"]:
        return

    var old_state: StringName = _state["_chart_state"]
    _state["_chart_state"] = result["new_state"]

    match _state["_chart_state"]:
        &"BROWSING":
            _on_entered_browsing()
        &"ROUTE_SELECTED":
            _on_entered_route_selected()
        &"DEPARTURE_CONFIRMED":
            _on_entered_departure_confirmed()
        &"ERROR":
            _on_entered_error()

    chart_state_changed.emit(old_state, _state["_chart_state"])


func _on_entered_error() -> void:
    _state["_retry_cooldown_remaining"] = Registry.get_constant(&"base_retry_cooldown", 2.0)
```

### Route Sub-State Machine

```text
func _set_route_sub_state(route_id: StringName, new_state: StringName) -> void:
    var old_state: StringName = _state["_route_states"].get(route_id, &"BROWSABLE")
    if old_state == new_state:
        return

    # UNAVAILABLE → SELECTED 禁止
    if old_state == &"UNAVAILABLE" and new_state == &"SELECTED":
        push_warning("Invalid sub-state transition: UNAVAILABLE → SELECTED for %s" % route_id)
        return

    # LOCKED → 任意 禁止
    if old_state == &"LOCKED":
        return

    _state["_route_states"][route_id] = new_state


func _set_all_routes_locked() -> void:
    for route_id in _state["_visible_routes"]:
        _state["_route_states"][route_id] = &"LOCKED"
```

### RETRY Flow

```text
func retry_open_chart() -> void:
    if _state["_retry_cooldown_remaining"] > 0.0:
        return
    var result: Dictionary = chart_state_transition(&"RETRY")
    if result["allowed"]:
        _apply_transition(result)
        open_chart()  # 重新执行加载
```

---

## Out of Scope

- open_chart() 的视觉过渡动画（羊皮纸渐变 0.8s）——属于 UI 系统 #16
- 航线可见性和可选择性公式的具体实现——属于 Story 002
- route_committed 信号的发射逻辑——属于 Story 003
- 知识状态查询的详细错误处理——属于 IntelManager #6 的合约
- 海图桌交互锚点的具体检测——属于 Hub #7

---

## QA Test Cases

- **AC-1 through AC-5**: 状态机完整路径
  - Given: 四大域 COMPLETE → open_chart() → BROWSING
  - Given: threats=FAILED → open_chart() → ERROR
  - Given: BROWSING + select browsable route → ROUTE_SELECTED
  - Given: ROUTE_SELECTED + Esc → BROWSING
  - Given: ERROR + cooldown=0 → RETRY → LOADING

- **AC-6 and AC-7**: Terminal guard
  - Given: DEPARTURE_CONFIRMED → any trigger → {allowed: false}
  - Given: 同帧两次 CONFIRM → 仅第一次有效

- **AC-8 and AC-9**: Invalid transitions
  - Given: ERROR + COMPLETE → rejected
  - Given: LOADING + SELECT → rejected（默认拒绝）

- **AC-15**: Content domain gate
  - Given: routes=COMPLETE, world=COMPLETE, intel=COMPLETE, threats=FAILED → FAIL → ERROR
  - Verify: _failed_domain_states = {routes: COMPLETE, world: COMPLETE, intel: COMPLETE, threats: FAILED}

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/chart/statemachine/ChartStateMachineTest.csproj` — must exist and pass
**Status**: [x] 43/43 PASS — 2026-05-13；Epic #9 复审通过 — 2026-05-13

---

## Dependencies

- Depends on: content-registry Epic (Registry.list_by_kind, domain_state query), intel-knowledge Epic (query_route_knowledge interface contract)
- Unlocks: Story 002 (route_selectability depends on chart_state), Story 003 (departure confirmation depends on transition table), all subsequent chart stories
