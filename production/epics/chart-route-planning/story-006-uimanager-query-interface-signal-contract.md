# Story 006: UIManager Query Interface & Signal Contract

> **Epic**: Chart / Route Planning
> **Status**: Done
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-002`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — Section 7 Read-Only Query Interfaces + Section 5 Signal Signatures), ADR-0002 (Signal Communication Protocol)
**ADR Decision Summary**: Chart 提供 4 个只读查询接口给 UIManager (#16)：get_chart_state() 返回 StringName 状态枚举、get_visible_routes() 返回已排序 StringName 航线 ID 数组、get_selected_route() 返回当前选中航线 ID 或空 StringName、get_filter_state() 返回 {hide_rumored: bool}。合约约定：所有接口只返回数据和状态枚举——不返回颜色值、位置坐标、透明度、动画关键帧。UI 系统完全拥有视觉层的实现自由。Chart 定义 3 个向外信号：route_committed（3 typed params）、route_selection_failed（2 typed params）、route_enhanced（2 typed params）+ 1 个状态变更信号 chart_state_changed（2 typed params）+ 1 个筛选器信号 filter_changed（1 typed param）。AirshipHub 时序安全守卫：route_selectability 仅在航图打开后（Phase 5+ AirshipHub 已存在）被调用，_get_current_docked_location_safe() 防御性空值检查。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 4 个只读查询接口只返回数据——不返回视觉属性；所有信号 typed params, sync emit, {noun}_{verb_past} 命名；emit-after-mutation ——信号在状态变更后发射
- Forbidden: 查询接口返回颜色/RGBA 值、像素坐标、透明度、动画曲线；信号 payload 包含 Node/Resource/Callable 引用；在 UIManager 调用中直接修改 Chart 状态
- Guardrail: _get_current_docked_location_safe() 在 AirshipHub 未就绪时返回空 StringName——UIManager 据此显示不可用而非崩溃

---

## Acceptance Criteria

### Read-Only Query Interfaces

- [ ] **AC-1**: GIVEN chart_state=BROWSING，WHEN UIManager 调用 get_chart_state()，THEN 返回 &"BROWSING"——StringName 类型
- [ ] **AC-2**: GIVEN 2 条可见航线（sky-reef-arc-01:201, storm-cut-01:302），WHEN UIManager 调用 get_visible_routes()，THEN 返回 [&"sky-reef-arc-01", &"storm-cut-01"]——已按 display_order 排序
- [ ] **AC-3**: GIVEN 已选中 sky-reef-arc-01，WHEN UIManager 调用 get_selected_route()，THEN 返回 &"sky-reef-arc-01"。无选中时返回 &""（空 StringName，非 null）
- [ ] **AC-4**: GIVEN hide_rumored=true，WHEN UIManager 调用 get_filter_state()，THEN 返回 {&"hide_rumored": true}

### Data/UI Separation Contract

- [ ] **AC-5**: GIVEN 所有 4 个查询接口，WHEN 审查返回值的所有字段类型，THEN 不包含：颜色值（Color/int/RGBA）、像素坐标（Vector2/float）、透明度（0-1 float 的 opacity）、动画时长或关键帧引用
- [ ] **AC-6**: GIVEN UIManager 需要航线渲染数据（视觉样式），WHEN UIManager 调用 get_route_display_data(route_id)，THEN 返回 knowledge_state（枚举，UIManager 自行映射到视觉编码）、selectability（枚举，UI 自行映射到灰/亮/锁定）、hazard_tags（字符串数组，UI 自行选择图标）。UIManager 持有视觉编码查找表（knowledge_state→线型/透明度/颜色）

### Signal Signatures (ADR-0002 Compliance)

- [ ] **AC-7**: GIVEN route_committed 信号定义，WHEN 审查签名，THEN 3 个 typed 参数——(route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName])。命名 {noun}_{verb_past}
- [ ] **AC-8**: GIVEN route_selection_failed 信号定义，WHEN 审查签名，THEN 2 个 typed 参数——(route_id: StringName, reason: StringName)
- [ ] **AC-9**: GIVEN route_enhanced 信号定义，WHEN 审查签名，THEN 2 个 typed 参数——(route_id: StringName, enhancement_id: StringName)
- [ ] **AC-10**: GIVEN chart_state_changed 信号定义，WHEN 审查签名，THEN 2 个 typed 参数——(old_state: StringName, new_state: StringName)
- [ ] **AC-11**: GIVEN filter_changed 信号定义，WHEN 审查签名，THEN 1 个 typed 参数——(hide_rumored: bool)

### Signal Emit Ordering

- [ ] **AC-12**: GIVEN 出航确认成功，WHEN 信号发射，THEN 发射顺序：chart_state_changed（状态变更先发）→ route_committed（出航承诺后发）。route_committed 在状态变更之后、快照写入请求之前
- [ ] **AC-13**: GIVEN 出航确认失败（traversable=false），WHEN 信号发射，THEN 发射顺序：route_selection_failed → chart_state_changed（ROUTE_SELECTED→BROWSING）。fail 信号在状态回退之前发射

### UIManager Integration Contract

- [ ] **AC-14**: GIVEN UIManager 订阅 chart_state_changed 信号，WHEN 状态变更，THEN UIManager 收到新旧状态枚举——自行决定 UI 过渡（如切换面板、播放动画）。Chart 不知道 UI 如何响应状态变更
- [ ] **AC-15**: GIVEN UIManager 需要航线详情展示，WHEN 调用 get_route_display_data(route_id)，THEN 返回的 Dictionary 包含 route_id/display_order/knowledge_state/selectability/traversable/hazard_tags/distance_band/origin_id/destination_id/name/block_reason。所有字段为纯数据——UI 自行渲染

### AirshipHub Timing Safety

- [ ] **AC-16**: GIVEN Chart Phase 3b 初始化，AirshipHub Phase 5 实例化，WHEN Chart 在 Phase 3b-4 期间被调用 route_selectability（异常时序），THEN _get_current_docked_location_safe() 返回 &""。origin_id != &"" → 所有航线返回 "unavailable"。不崩溃

---

## Implementation Notes

### Read-Only Query Interfaces

```text
func get_chart_state() -> StringName:
    return _state["_chart_state"]

func get_visible_routes() -> Array:
    # 实时计算——每次调用反映当前 _hide_rumored 和知识状态
    var visible: Array[StringName] = []
    for route_id in _state["_visible_routes"]:
        if not route_visibility(route_id, _state["_hide_rumored"]):
            continue
        visible.append(route_id)

    visible.sort_custom(func(a: StringName, b: StringName) -> bool:
        var order_a: int = route_display_order(a)
        var order_b: int = route_display_order(b)
        if order_a != order_b:
            return order_a < order_b
        return str(a) < str(b)
    )

    return visible

func get_selected_route() -> StringName:
    if _state["_selected_route_id"] != null:
        return _state["_selected_route_id"]
    return &""

func get_filter_state() -> Dictionary:
    return {"hide_rumored": _state["_hide_rumored"]}
```

### Signal Declarations

```text
## 出航承诺 -- 3 typed params, sync emit
signal route_committed(route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName])

## 出航确认失败 -- 配对的 fail 信号（ADR-0002）
signal route_selection_failed(route_id: StringName, reason: StringName)

## 航线增强（WorldRepair 完成连锁）
signal route_enhanced(route_id: StringName, enhancement_id: StringName)

## 航图状态变更
signal chart_state_changed(old_state: StringName, new_state: StringName)

## 筛选器变更
signal filter_changed(hide_rumored: bool)
```

### UIManager Data Contract

```text
func get_route_display_data(route_id: StringName) -> Dictionary:
    """返回航线在 UI 中展示所需的所有数据。
    注意：不含视觉属性。UIManager 持有 knowledge_state→视觉编码 的映射表。"""
    var knowledge: int = _query_knowledge_state(route_id)
    var accessibility: Dictionary = _query_route_accessibility(route_id)
    var route_data: Dictionary = Registry.get_route_data(route_id)

    return {
        "route_id": route_id,
        "display_order": route_display_order(route_id),
        "knowledge_state": knowledge,
        "selectability": route_selectability(route_id),
        "traversable": accessibility.get("traversable", false),
        "hazard_tags": accessibility.get("hazard_tags", []),
        "distance_band": route_data.get("distance_band", &"medium"),
        "origin_id": route_data.get("origin_location_id", &""),
        "destination_id": route_data.get("destination_location_id", &""),
        "origin_name": _get_location_name(route_data.get("origin_location_id", &"")),
        "destination_name": _get_location_name(route_data.get("destination_location_id", &"")),
        "name": route_data.get("name", ""),
        "block_reason": accessibility.get("block_reason", ""),
        "knowledge_source": knowledge if knowledge >= 0 else KNOWLEDGE_UNKNOWN,
    }
```

### UIManager Visual Encoding Lookup (Documentation)

```text
# UIManager 持有此映射表——Chart 不定义视觉属性
# const KNOWLEDGE_VISUAL: Dictionary = {
#     KNOWLEDGE_RUMORED:    {"line": "dashed", "opacity": 0.6, "endpoint": "dashed_circle", "width": 2},
#     KNOWLEDGE_IDENTIFIED: {"line": "solid",  "opacity": 1.0, "endpoint": "hollow_circle"},
#     KNOWLEDGE_VERIFIED:   {"line": "solid",  "opacity": 1.0, "endpoint": "solid_circle", "glow": "warm_gold"},
# }
#
# const SELECTABILITY_VISUAL: Dictionary = {
#     "browsable":   {"dimmed": false, "interactable": true},
#     "selected":    {"highlighted": true, "pulse": 0.3},
#     "unavailable": {"dimmed": true, "greyed": true, "show_tooltip": true},
#     "locked":      {"dimmed": false, "interactable": false},
#     "hidden":      {"render": false},
# }
```

---

## Out of Scope

- UIManager 中航线渲染的具体实现（Control 节点、线条绘制、颜色映射、动画播放）——属于 UI 系统 #16
- 确认浮层的 UI 布局和控件——属于 UI 系统 #16
- 侧边详情面板的视觉设计——属于 UI 系统 #16
- 墨迹扩散动画的技术实现——属于 UI 系统 #16
- UI 输入处理（鼠标/键盘事件 → Chart 方法调用）——属于 UI 系统 #16

---

## QA Test Cases

- **AC-1 through AC-4**: Query interfaces
  - Given: BROWSING → get_chart_state() → &"BROWSING"
  - Given: 2 visible routes → get_visible_routes() → sorted array
  - Given: selected route → get_selected_route() → route_id; none → &""
  - Given: hide_rumored=true → get_filter_state() → {hide_rumored: true}

- **AC-7 through AC-11**: Signal signatures
  - Verify: route_committed 3 typed params (StringName, StringName, Array[StringName])
  - Verify: route_selection_failed 2 typed params
  - Verify: route_enhanced 2 typed params
  - Verify: chart_state_changed 2 typed params
  - Verify: filter_changed 1 typed param

- **AC-16**: AirshipHub timing safety
  - Given: AirshipHub=null → _get_current_docked_location_safe() → &""
  - Given: origin_id=&"glass-harbor" + docked=&"" → "unavailable"（不崩溃）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/chart/uicontract/UimanagerContractTest.csproj` — must exist and pass
**Status**: [x] 56/56 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (get_chart_state), Story 002 (get_visible_routes, route_selectability), Story 003 (signal signatures), Story 004 (route_display_order, get_filter_state), airship-hub Epic (get_current_docked_location), ADR-0002 (signal protocol)
- Unlocks: UI system #16 implementation (航图渲染、控件交互、动画播放), Story 007 (signal connections for external state)
