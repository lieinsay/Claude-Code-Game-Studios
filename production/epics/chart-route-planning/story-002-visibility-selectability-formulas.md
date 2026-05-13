# Story 002: Route Visibility & Selectability Formulas

> **Epic**: Chart / Route Planning
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-001`, `TR-chart-002`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — Formula 1 route_visibility + Formula 2 route_selectability)
**ADR Decision Summary**: Formula 1 (route_visibility) 为纯函数——输入 route_id + hide_rumored，返回 boolean。判定逻辑：unknown→false, hide_rumored+rumored→false, 其余→true。Formula 2 (route_selectability) 为纯函数——短路求值顺序：hidden → locked → unavailable(traversable) → unavailable(origin) → selected → browsable。hidden 在第一分支返回避免对不可见航线进行后续跨系统查询。Chart 通过 IntelManager.query_route_knowledge() 和 query_route_accessibility() 读取知识状态——只读，永不写入。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: route_visibility() 和 route_selectability() 为纯函数——不修改 _state，只返回计算结果；短路求值顺序严格按 hidden→locked→unavailable(2种)→selected→browsable
- Forbidden: 在 route_selectability 中直接调用 AirshipHub 方法而不使用 _get_current_docked_location_safe()；在 visibility 判定中返回 true 给 unknown 航线
- Guardrail: route_selectability 仅在 BROWSING/ROUTE_SELECTED 状态下被调用时 origin 检查才有意义——其他状态下 _get_current_docked_location_safe() 防御性返回空值

---

## Acceptance Criteria

### Formula 1 — route_visibility

- [ ] **AC-1**: GIVEN knowledge_state=unknown，WHEN route_visibility(route_id, false)，THEN return false——unknown 航线永不渲染（硬性边界）
- [ ] **AC-2**: GIVEN knowledge_state=rumored + hide_rumored=true，WHEN route_visibility(route_id, true)，THEN return false——筛选器隐藏传闻航线
- [ ] **AC-3**: GIVEN knowledge_state=rumored + hide_rumored=false，WHEN route_visibility(route_id, false)，THEN return true
- [ ] **AC-4**: GIVEN knowledge_state=identified 或 verified + hide_rumored=true/false，WHEN route_visibility()，THEN 始终返回 true
- [ ] **AC-5**: GIVEN query_route_knowledge() 返回空 Dictionary 或 null knowledge_state，WHEN route_visibility()，THEN 视为 unknown → return false（防御性空值处理）

### Formula 2 — route_selectability 短路求值

- [ ] **AC-6**: GIVEN route_visibility=false（unknown 或筛选隐藏），WHEN route_selectability(route_id)，THEN return "hidden"——分支 1 短路，不执行后续查询
- [ ] **AC-7**: GIVEN chart_state=DEPARTURE_CONFIRMED，WHEN route_selectability(any_route)，THEN return "locked"——分支 2 短路，终端状态所有航线不可交互
- [ ] **AC-8**: GIVEN traversable=false（需要能力未解锁），WHEN route_selectability(route_id)，THEN return "unavailable"——分支 3
- [ ] **AC-9**: GIVEN origin_id ≠ docked_location（起点非当前港口），WHEN route_selectability(route_id)，THEN return "unavailable"——分支 4
- [ ] **AC-10**: GIVEN route_id == selected_route_id + chart_state=ROUTE_SELECTED，WHEN route_selectability()，THEN return "selected"——分支 5
- [ ] **AC-11**: GIVEN chart_state=ROUTE_SELECTED + 非已选航线，WHEN route_selectability()，THEN return "browsable"——分支 6（在选中其他航线时，未选航线降级）
- [ ] **AC-12**: GIVEN chart_state=BROWSING + 航线满足所有条件，WHEN route_selectability()，THEN return "browsable"——分支 7（默认）

### Full Short-Circuit Chain Verification

- [ ] **AC-13**: GIVEN 构造 7 条测试航线，每条命中不同短路分支，WHEN 逐一调用 route_selectability()，THEN 返回值与预期完全一致：

| 条件 | chart_state | hide_rumored | docked | traversable | selected_id | 预期 |
|------|-------------|-------------|--------|-------------|-------------|------|
| unknown | BROWSING | false | glass-harbor | — | — | hidden |
| rumored | BROWSING | true | glass-harbor | — | — | hidden |
| any | DEPARTURE_CONFIRMED | — | — | — | — | locked |
| identified | BROWSING | false | glass-harbor | false | — | unavailable |
| identified | BROWSING | false | glass-harbor | true | other | unavailable(origin≠) |
| identified | ROUTE_SELECTED | false | glass-harbor | true | self | selected |
| identified | ROUTE_SELECTED | false | glass-harbor | true | other | browsable |
| identified | BROWSING | false | glass-harbor | true | null | browsable |

### Defensive Error Handling

- [ ] **AC-14**: GIVEN query_route_accessibility() 返回空 Dictionary 或缺少 traversable 字段，WHEN route_selectability()，THEN traversable 默认视为 false → "unavailable"。不崩溃
- [ ] **AC-15**: GIVEN AirshipHub 未就绪（null 或无 get_current_docked_location 方法），WHEN _get_current_docked_location_safe()，THEN 返回空 StringName &""——所有航线 origin≠&"" → UNAVAILABLE。不崩溃

---

## Implementation Notes

### Formula 1 — route_visibility

```text
func route_visibility(route_id: StringName, hide_rumored: bool) -> bool:
    var knowledge_state: int = _query_knowledge_state(route_id)

    # 防御性空值处理——查询失败或 unknown → false
    if knowledge_state < 0 or knowledge_state == KNOWLEDGE_UNKNOWN:
        return false

    # hide_rumored 筛选器——隐藏仅传闻航线
    if hide_rumored and knowledge_state == KNOWLEDGE_RUMORED:
        return false

    return true


func _query_knowledge_state(route_id: StringName) -> int:
    if not IntelManager.has_method("query_route_knowledge"):
        return KNOWLEDGE_UNKNOWN

    var result: Dictionary = IntelManager.query_route_knowledge(route_id)
    if result.is_empty():
        return KNOWLEDGE_UNKNOWN

    var state = result.get("state", KNOWLEDGE_UNKNOWN)
    if typeof(state) != TYPE_INT:
        return KNOWLEDGE_UNKNOWN
    return state
```

### Formula 2 — route_selectability（短路求值）

```text
func route_selectability(route_id: StringName) -> StringName:
    # 分支 1: 不可见 → hidden（短路——避免对隐藏航线进行后续查询）
    if not route_visibility(route_id, _state["_hide_rumored"]):
        return &"hidden"

    # 分支 2: 出航已确认 → locked
    if _state["_chart_state"] == &"DEPARTURE_CONFIRMED":
        return &"locked"

    # 分支 3: 不可通行 → unavailable
    var accessibility: Dictionary = _query_route_accessibility(route_id)
    if not accessibility.get("traversable", false):
        return &"unavailable"

    # 分支 4: 起点不匹配当前停靠地 → unavailable
    var origin_id: StringName = Registry.get_route_origin(route_id)
    var docked: StringName = _get_current_docked_location_safe()
    if origin_id != docked:
        return &"unavailable"

    # 分支 5: 已选中 → selected
    if route_id == _state["_selected_route_id"]:
        return &"selected"

    # 分支 6: 在 ROUTE_SELECTED 状态下非已选航线 → browsable（降级）
    # 分支 7: BROWSING 状态下的正常可选航线 → browsable
    return &"browsable"
```

### AirshipHub Timing Safety Guard

```text
func _get_current_docked_location_safe() -> StringName:
    if not is_instance_valid(AirshipHub) or not AirshipHub.has_method("get_current_docked_location"):
        return &""
    return AirshipHub.get_current_docked_location()
```

### Query Route Accessibility (Safe)

```text
func _query_route_accessibility(route_id: StringName) -> Dictionary:
    if not IntelManager.has_method("query_route_accessibility"):
        return {"traversable": false}

    var result: Dictionary = IntelManager.query_route_accessibility(route_id)
    if result.is_empty():
        return {"traversable": false}

    return result
```

### Re-evaluate All Routes

```text
func _reevaluate_all_routes() -> void:
    for route_id in _state["_visible_routes"]:
        var selectability: StringName = route_selectability(route_id)
        _set_route_sub_state(route_id, selectability)


func _reevaluate_routes_for_location(location_id: StringName) -> void:
    for route_id in _state["_visible_routes"]:
        if Registry.get_route_origin(route_id) == location_id or \
           Registry.get_route_destination(route_id) == location_id:
            var selectability: StringName = route_selectability(route_id)
            _set_route_sub_state(route_id, selectability)
```

---

## Out of Scope

- query_route_knowledge() 和 query_route_accessibility() 的具体实现——属于 IntelManager #6
- 航线被选中后的详情面板填充——属于 UI 系统 #16
- 筛选器 hide_rumored 的 UI 切换控件——属于 UI 系统 #16
- 阻塞航线的 tooltip 具体文案和视觉——属于 UI 系统 #16

---

## QA Test Cases

- **AC-1 through AC-5**: Formula 1 五条分支
  - Given: unknown → false; rumored+hidden → false; rumored+shown → true; identified → true; verified → true
  - Given: null/empty query result → false

- **AC-6 through AC-12**: Formula 2 七条短路分支
  - Given: 构造对应条件 → 验证返回值和短路行为
  - Verify: hidden 分支不调用 query_route_accessibility（性能验证）

- **AC-13**: 完整短路链
  - Given: 8 条测试航线覆盖所有分支 → 逐一验证

- **AC-15**: AirshipHub 未就绪
  - Given: AirshipHub=null → _get_current_docked_location_safe() → &"" → 所有航线 unavailable

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/chart/visibility/VisibilitySelectabilityTest.csproj` — must exist and pass
**Status**: [x] 32/32 PASS — 2026-05-13；Epic #9 复审通过 — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (chart_state 枚举值, _hide_rumored 状态), intel-knowledge Epic (query_route_knowledge/query_route_accessibility interface), airship-hub Epic (get_current_docked_location)
- Unlocks: Story 003 (CONFIRM 触发使用 route_selectability 验证), Story 004 (display_order 依赖 knowledge_state), Story 007 (外部状态变化后的 _reevaluate 调用)
