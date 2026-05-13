# Story 004: Route Display Ordering & Filtering

> **Epic**: Chart / Route Planning
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-001`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — Formula 5 route_display_order)
**ADR Decision Summary**: route_display_order 为纯函数——输入 route_id，返回 integer ∈ [101, 303]。排序公式：rank_by_knowledge×100 + rank_by_distance。知识置信度：verified=1, identified=2, rumored=3。距离带：short=1, medium=2, long=3。值越小排越前。知识权重（×100）压倒距离权重（+1~3），确保 verified 航线始终排在 rumored 航线之前。MVP 两条航线验证：sky-reef-arc-01 (identified+short) = 201 < storm-cut-01 (rumored+medium) = 302 → sky-reef-arc-01 排前。未知知识状态返回 999（不应出现在可见列表，防御性）。hide_rumored 筛选器是航图本地 UI 状态，默认值为 false（显示所有可见航线）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: route_display_order() 为纯函数——不修改 _state，只返回 int；排序稳定——相同 display_order 值按 route_id 字典序打破平局；hide_rumored 筛选器即时生效——切换后立即重新计算 _visible_routes
- Forbidden: 排序逻辑包含 UI 关注点（如"收藏航线优先""最近使用优先"）
- Guardrail: 未知 knowledge_state 或 distance_band 返回安全默认值（999 或 medium=2），不因枚举值缺失而崩溃

---

## Acceptance Criteria

### Formula 5 — route_display_order

- [ ] **AC-1**: GIVEN knowledge_state=verified + distance_band=short，WHEN route_display_order(route_id)，THEN return 1×100+1 = 101
- [ ] **AC-2**: GIVEN knowledge_state=identified + distance_band=medium，WHEN route_display_order(route_id)，THEN return 2×100+2 = 202
- [ ] **AC-3**: GIVEN knowledge_state=rumored + distance_band=long，WHEN route_display_order(route_id)，THEN return 3×100+3 = 303
- [ ] **AC-4**: GIVEN knowledge_state=unknown（防御性），WHEN route_display_order(route_id)，THEN return 999——不应出现在可见列表

### Full Output Range Verification

- [ ] **AC-5**: GIVEN 全部 9 种 knowledge×distance 组合，WHEN route_display_order()，THEN 输出覆盖 101-303 全部值：

| 知识 × 距离 | short (1) | medium (2) | long (3) |
|------------|-----------|------------|----------|
| verified (1) | 101 | 102 | 103 |
| identified (2) | 201 | 202 | 203 |
| rumored (3) | 301 | 302 | 303 |

### MVP Route Sorting

- [ ] **AC-6**: GIVEN sky-reef-arc-01 (identified+short) + storm-cut-01 (rumored+medium) + hide_rumored=false，WHEN 计算 display_order 并排序，THEN sky-reef-arc-01 (201) 排在 storm-cut-01 (302) 之前
- [ ] **AC-7**: GIVEN 两条相同 knowledge 层级但不同距离带的航线，WHEN 排序，THEN short (rank=1) 排在 medium (rank=2) 之前，medium 排在 long (rank=3) 之前——同层级内距离优先

### Stable Sort — Tie-Breaking

- [ ] **AC-8**: GIVEN 两条航线 display_order 相同（如同为 identified+short = 201），WHEN 排序，THEN 按 route_id 字典序打破平局。排序结果稳定——多次查询返回相同顺序

### hide_rumored Filter

- [ ] **AC-9**: GIVEN 3 条航线（2 identified + 1 rumored）+ hide_rumored=false，WHEN get_visible_routes()，THEN 返回全部 3 条，按 display_order 排序
- [ ] **AC-10**: GIVEN 同上 + hide_rumored=true，WHEN get_visible_routes()，THEN rumored 航线被过滤。返回 2 条 identified 航线。rumored 航线不在可见列表中
- [ ] **AC-11**: GIVEN 所有航线均为 rumored + hide_rumored=true，WHEN get_visible_routes()，THEN 返回空数组。航图显示空状态消息："所有航线均为传闻级别——关闭'隐藏传闻航线'以查看。"

### Filter State Persistence

- [ ] **AC-12**: GIVEN hide_rumored=true，WHEN 出航确认（DEPARTURE_CONFIRMED），THEN hide_rumored 值被写入 progress.routes 快照包 active_filter 字段。恢复后筛选器状态保持一致

### Defensive Defaults

- [ ] **AC-13**: GIVEN query_route_knowledge() 返回空或无效值，WHEN route_display_order()，THEN knowledge_state 视为 unknown → return 999
- [ ] **AC-14**: GIVEN distance_band 为未知值（非 short/medium/long），WHEN route_display_order()，THEN distance_rank=2（medium 默认值）——不崩溃

---

## Implementation Notes

### Formula 5 — route_display_order

```text
func route_display_order(route_id: StringName) -> int:
    var knowledge_state: int = _query_knowledge_state(route_id)
    var distance_band: StringName = Registry.get_route_distance_band(route_id)

    var rank_by_knowledge: int
    match knowledge_state:
        KNOWLEDGE_VERIFIED:
            rank_by_knowledge = 1
        KNOWLEDGE_IDENTIFIED:
            rank_by_knowledge = 2
        KNOWLEDGE_RUMORED:
            rank_by_knowledge = 3
        _:
            return 999  # unknown — 不应出现在可见列表（防御性）

    var rank_by_distance: int
    match distance_band:
        &"short":
            rank_by_distance = 1
        &"medium":
            rank_by_distance = 2
        &"long":
            rank_by_distance = 3
        _:
            rank_by_distance = 2  # 未知距离带视为 medium（防御性）

    return rank_by_knowledge * 100 + rank_by_distance
```

### get_visible_routes() — Sorted with Filter

```text
func get_visible_routes() -> Array:
    var visible: Array[StringName] = []

    for route_id in _state["_visible_routes"]:
        if not route_visibility(route_id, _state["_hide_rumored"]):
            continue
        visible.append(route_id)

    # 按 display_order 排序，相同值按 route_id 字典序打破平局
    visible.sort_custom(func(a: StringName, b: StringName) -> bool:
        var order_a: int = route_display_order(a)
        var order_b: int = route_display_order(b)
        if order_a != order_b:
            return order_a < order_b
        return str(a) < str(b)  # tie-break: 字典序
    )

    return visible
```

### hide_rumored Toggle

```text
func set_hide_rumored(hide: bool) -> void:
    if _state["_hide_rumored"] == hide:
        return

    _state["_hide_rumored"] = hide

    # 重新计算可见航线列表
    _state["_visible_routes"] = _build_visible_routes_list()

    # 若当前已选航线因筛选器变更而不可见，强制取消选择
    if _state["_selected_route_id"] != null:
        var selected_id: StringName = _state["_selected_route_id"]
        if not route_visibility(selected_id, _state["_hide_rumored"]):
            _force_deselect(&"filter_hidden")

    filter_changed.emit(hide)


func _build_visible_routes_list() -> Array[StringName]:
    var all_routes: Array = Registry.list_by_kind(&"route")
    var visible: Array[StringName] = []

    for route_id in all_routes:
        var knowledge: int = _safe_query_knowledge(route_id)
        if knowledge < 0 or knowledge == KNOWLEDGE_UNKNOWN:
            continue
        visible.append(route_id)
        if not _state["_route_states"].has(route_id):
            _state["_route_states"][route_id] = &"BROWSABLE"

    return visible
```

### Filter State Signal

```text
signal filter_changed(hide_rumored: bool)

func get_filter_state() -> Dictionary:
    return {"hide_rumored": _state["_hide_rumored"]}
```

### get_route_display_data() — For UI Panel

```text
func get_route_display_data(route_id: StringName) -> Dictionary:
    """返回航线在 UI 中展示所需的所有数据——不返回视觉属性"""
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
        "name": route_data.get("name", ""),
        "block_reason": accessibility.get("block_reason", ""),
    }
```

---

## Out of Scope

- 航线在 UI 中的视觉渲染（颜色、线型、透明度、布局位置）——属于 UI 系统 #16
- 侧边详情面板的具体内容和控件——属于 UI 系统 #16
- 筛选器切换控件的 UI 实现——属于 UI 系统 #16
- 空状态上下文消息的具体 UI 文案（由 UI 系统根据航图返回的 empty context code 渲染）

---

## QA Test Cases

- **AC-1 through AC-5**: Formula 5 全部输出
  - Given: verified+short → 101; rumored+long → 303; unknown → 999
  - Given: 9 种组合全部验证

- **AC-6 and AC-7**: MVP sorting
  - Given: sky-reef (201) + storm-cut (302) → sky-reef 排前
  - Given: 同 knowledge 不同 distance → short 优先

- **AC-8**: Tie-breaking
  - Given: 两条航线同为 201 → route_id 字典序稳定

- **AC-9 through AC-11**: Filter toggle
  - Given: 3 routes (2 identified + 1 rumored) → toggle hide_rumored → rumored 隐藏/显示
  - Given: 全 rumored + hide=true → empty → empty state message

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/chart/display/DisplayOrderTest.csproj` — must exist and pass
**Status**: [x] 31/31 PASS — 2026-05-13；Epic #9 复审通过 — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (chart_state, _visible_routes storage), Story 002 (route_visibility, _query_knowledge_state), IntelManager #6 (knowledge state enum values), content-registry Epic (get_route_distance_band, get_route_data)
- Unlocks: Story 006 (get_visible_routes for UIManager), Story 008 (EC-14 全传闻+hide_rumored)
