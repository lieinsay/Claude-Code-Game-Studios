# Story 008: Edge Cases, Error Recovery & Keyboard Navigation

> **Epic**: Chart / Route Planning
> **Status**: Done
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-001`, `TR-chart-003`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — all sections covering edge cases EC-1 through EC-16, keyboard navigation contract)
**ADR Decision Summary**: 航图必须优雅处理 16 个边界情况，涵盖：加载/初始化（EC-1/2）、玩家操作（EC-3/4）、数据不一致（EC-5/6/7/8）、存档/恢复（EC-9/10/11）、空状态/边界（EC-12/13/14）、跨系统合约（EC-15/16）。键盘导航完整流程：Tab/Shift+Tab 在航线间移动焦点（按 route_display_order 顺序）、Enter 选中/确认、Esc 取消选择/关闭浮层。DEPARTURE_CONFIRMED 锁定期间所有键盘输入禁用。Chart 侧提供的数据接口支持键盘焦点顺序：get_visible_routes() 已排序，UIManager 按此顺序分配 Tab 焦点。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 空航图是合法状态（非 ERROR）——显示上下文消息区分三种空状态；数据完整性违规（EC-15）检测并记录警告日志——但不崩溃；优雅降级——部分查询失败时进入 BROWSING 而非 ERROR
- Forbidden: 空航图进入 ERROR 状态；在 DEPARTURE_CONFIRMED 或 LOCKED 状态响应键盘/鼠标输入；对 unknown 航线执行 traversable 查询（EC-15 防御）
- Guardrail: 上下文消息区分三种空状态——"从未有过航线" vs "全传闻+筛选隐藏" vs "当前港口无出发航线"；_internal_warning_counter 记录部分查询失败但不阻断航图进入 BROWSING

---

## Acceptance Criteria

### EC-1: Partial Domain Failure

- [ ] **AC-1**: GIVEN routes/world/intel=COMPLETE 但 threats=FAILED，WHEN open_chart()，THEN LOADING→ERROR。ERROR 明确显示失败域 threats 和状态 FAILED。RETRY 按钮可用。玩家修复 threats 后重试→BROWSING

### EC-2: Partial Query Failure (Graceful Degradation)

- [ ] **AC-2**: GIVEN 5 条航线中 2 条 query_route_knowledge() 失败，WHEN open_chart()，THEN 失败航线视为 unknown（不渲染）。航图以 3 条有效航线进入 BROWSING。_internal_warning_counter=2。非阻断通知可用。不进入 ERROR

### EC-3/4: Double-Click / Rapid Click Protection

- [ ] **AC-3**: GIVEN 出航确认已点击 + DEPARTURE_CONFIRMED 已进入 + 锁定 2.0s 期间，WHEN 快速连点（10+次 <100ms 间隔）航图区域和确认按钮，THEN 所有输入被拦截——route_selectability 分支 2：所有航线→locked。chart_state_transition 终端守卫拒绝所有转换。route_committed 仅发射一次
- [ ] **AC-4**: GIVEN 同一帧内两次 CONFIRM 触发，WHEN 第一次处理完毕→DEPARTURE_CONFIRMED + 第二次到达，THEN 第二次命中终端守卫→{allowed: false}。不产生第二个 route_committed

### EC-5: Knowledge Revoked Mid-Session

- [ ] **AC-5**: GIVEN ROUTE_SELECTED + 已选航线 knowledge=identified，WHEN 外部将其 knowledge→unknown，THEN route_visibility→false→航线从航图移除→强制取消选择→BROWSING。通知："航线 [名称] 的情报已失效——该航线的知识来源不再可信。"

### EC-6: Dock Changed Mid-Session

- [ ] **AC-6**: GIVEN BROWSING + docked=glass-harbor，WHEN docked→other-port，THEN 原起点 glass-harbor 航线→UNAVAILABLE。若已选中→强制取消选择。通知："当前停靠地已变更为 [港口B 名称]——航线选择已更新。"

### EC-7: Ability Unlocked Mid-Session

- [ ] **AC-7**: GIVEN 航线 X UNAVAILABLE (traversable=false)，WHEN 对应能力解锁，THEN route_selectability→BROWSABLE。视觉恢复（UNAVAILABLE→BROWSABLE 的子状态变更）。不强制通知——正向变化通过视觉变化传达

### EC-8: Route Deleted from Registry Mid-Session

- [ ] **AC-8**: GIVEN BROWSING + 5 条航线缓存，WHEN 注册表中 route R_003 被删除，THEN 下次刷新时检测到 R_003 不在 registry→从 _visible_routes 移除。若 R_003 为已选→强制取消选择。不崩溃

### EC-12: First Open — All Routes Unknown

- [ ] **AC-9**: GIVEN 所有航线的 knowledge_state=unknown + 四大域 COMPLETE，WHEN open_chart()，THEN get_visible_routes()→[]。chart_state→BROWSING（非 ERROR）。上下文消息："航图上尚无已知航线。在世界中收集情报以揭示航线。" hide_rumored 切换无可见变化。不崩溃、不黑屏

### EC-13: Zero Departable Routes at This Port

- [ ] **AC-10**: GIVEN 3 条 verified 航线，起点均非当前港口，WHEN open_chart()，THEN 全部航线→UNAVAILABLE。chart_state→BROWSING。上下文消息："当前港口 [X 名称] 无可用出发航线。前往其他港口以选择航线。" 玩家可浏览航线详情——只是无法选择

### EC-14: All Routes Rumored + Filter Hides Rumored

- [ ] **AC-11**: GIVEN 3 条航线全部 rumored + hide_rumored=true，WHEN get_visible_routes()，THEN →[]。上下文消息："所有航线均为传闻级别——关闭'隐藏传闻航线'以查看。" hide_rumored 切回 false 后恢复渲染。切换到 true 时无航线"永久丢失"

### EC-15: Data Consistency Violation

- [ ] **AC-12**: GIVEN traversable=true 但 knowledge_state=unknown，WHEN route_selectability()，THEN route_visibility 优先——hidden 第一分支返回。不执行 traversable 查询。警告日志："route [X]: traversable=true but knowledge=unknown —— data consistency violation in intel system." 不崩溃

### EC-16: Risk Change Between Two Steps

- [ ] **AC-13**: GIVEN 选中航线 risk=safe，WHEN 点击"确认出航"（第一步）时 hazard_tags 新增 pirate_activity，THEN 确认浮层展示当前数据（红色，pirate_activity）而非过时数据。若 traversable 变为 false→确认阻止

### Keyboard Navigation

- [ ] **AC-14**: GIVEN BROWSING + 2 条可见航线，WHEN Tab 键在航线间移动焦点，THEN 焦点顺序与 route_display_order 一致（sky-reef-arc-01:201 先于 storm-cut-01:302）
- [ ] **AC-15**: GIVEN 焦点在可选航线上，WHEN Enter，THEN SELECT（与鼠标点击行为一致）。chart_state: BROWSING→ROUTE_SELECTED
- [ ] **AC-16**: GIVEN ROUTE_SELECTED，WHEN Esc，THEN DESELECT→BROWSING（与点击空白区域一致）
- [ ] **AC-17**: GIVEN ROUTE_SELECTED + 焦点在"确认出航"按钮，WHEN Enter→第一步浮层→Tab→焦点在"出航"按钮→Enter→第二步确认→DEPARTURE_CONFIRMED
- [ ] **AC-18**: GIVEN DEPARTURE_CONFIRMED 锁定中，WHEN Tab/Enter/Esc，THEN 所有键盘输入被禁用——不响应任何按键

### Snapshot Edge Cases

- [ ] **AC-19**: GIVEN 快照 last_committed_route_id 在注册表中不存在（EC-9），WHEN snapshot_package_validity()，THEN violation → valid=false → 存档系统拒绝写入
- [ ] **AC-20**: GIVEN 快照 timestamp 在未来（EC-10），WHEN snapshot_package_validity()，THEN violation → valid=false
- [ ] **AC-21**: GIVEN 快照部分写入损坏（EC-11），WHEN 加载，THEN 缺失必需字段 → validation 失败 → 回退至干净状态。下次海图桌交互从 LOADING 开始

---

## Implementation Notes

### Empty State Context Messages

```text
enum ChartEmptyReason {
    NO_KNOWN_ROUTES,          # EC-12: 从未有过航线
    ALL_RUMORED_HIDDEN,       # EC-14: 全部传闻+筛选隐藏
    NO_DEPARTABLE_AT_PORT,    # EC-13: 当前港口无出发航线
}

func get_empty_chart_reason() -> int:
    if _state["_chart_state"] != &"BROWSING":
        return -1

    var all_routes: Array = Registry.list_by_kind(&"route")
    var has_any_known: bool = false
    var has_any_visible: bool = false
    var has_any_browsable: bool = false

    for route_id in all_routes:
        var knowledge: int = _query_knowledge_state(route_id)
        if knowledge != KNOWLEDGE_UNKNOWN:
            has_any_known = true
        if route_visibility(route_id, _state["_hide_rumored"]):
            has_any_visible = true
        if route_selectability(route_id) == &"browsable":
            has_any_browsable = true

    if not has_any_known:
        return ChartEmptyReason.NO_KNOWN_ROUTES
    if not has_any_visible and _state["_hide_rumored"]:
        return ChartEmptyReason.ALL_RUMORED_HIDDEN
    if not has_any_browsable and has_any_visible:
        return ChartEmptyReason.NO_DEPARTABLE_AT_PORT

    return -1  # 非空状态
```

### EC-15 — Data Consistency Violation Detection

```text
func _detect_consistency_violation(route_id: StringName) -> void:
    var knowledge: int = _query_knowledge_state(route_id)
    var accessibility: Dictionary = _query_route_accessibility(route_id)

    # 检测 traversable=true 但 knowledge=unknown 的矛盾
    if accessibility.get("traversable", false) and knowledge == KNOWLEDGE_UNKNOWN:
        push_warning(
            "route [%s]: traversable=true but knowledge=unknown —— data consistency violation in intel system."
            % route_id
        )
    # route_visibility 优先——unknown 永不渲染，traversable 查询不执行
```

### Partial Query Failure Handling

```text
func _batch_query_routes(all_routes: Array) -> Dictionary:
    var visible: Array[StringName] = []
    var failed_count: int = 0

    for route_id in all_routes:
        var knowledge: int = _safe_query_knowledge(route_id)
        if knowledge < 0:
            failed_count += 1
            continue
        if knowledge == KNOWLEDGE_UNKNOWN:
            continue

        visible.append(route_id)
        if not _state["_route_states"].has(route_id):
            _state["_route_states"][route_id] = &"BROWSABLE"

    return {"visible": visible, "failed_count": failed_count}
```

### Keyboard Navigation Data (For UIManager)

```text
func get_keyboard_nav_order() -> Array[StringName]:
    """返回键盘 Tab 导航的顺序——UIManager 据此分配焦点"""
    return get_visible_routes()  # 已按 display_order 排序

func is_interaction_allowed() -> bool:
    """UIManager 在每次输入处理前查询——锁定期间返回 false"""
    if _state["_chart_state"] == &"DEPARTURE_CONFIRMED":
        return false
    if _state["_departure_lock_remaining"] > 0.0:
        return false
    return _state["_chart_state"] != &"ERROR"
```

### Cache-Consistency Check

```text
func _validate_cache_consistency() -> void:
    """检查 _visible_routes 与 Registry 的一致性——EC-8 防御"""
    var registry_routes: Array = Registry.list_by_kind(&"route")
    var registry_set: Dictionary = {}
    for route_id in registry_routes:
        registry_set[route_id] = true

    var stale_routes: Array[StringName] = []
    for route_id in _state["_visible_routes"]:
        if not registry_set.has(route_id):
            stale_routes.append(route_id)

    if stale_routes.size() > 0:
        for route_id in stale_routes:
            push_warning("Chart: route %s removed from registry — purging from cache" % route_id)
            _state["_visible_routes"].erase(route_id)
            _state["_route_states"].erase(route_id)

        # 若已选航线被删除
        if _state["_selected_route_id"] in stale_routes:
            _force_deselect(&"route_deleted_from_registry")
```

---

## Out of Scope

- 上下文消息的具体 UI 文案和字体渲染——属于 UI 系统 #16（Chart 提供 empty_reason 枚举，UI 选择文案）
- 键盘焦点切换的视觉指示器——属于 UI 系统 #16
- Tooltip 的具体实现和定位——属于 UI 系统 #16
- 确认浮层的 UI 布局——属于 UI 系统 #16
- 存档系统的原子写入和回退——属于存档系统 #3
- IntelManager 的数据一致性修复——属于 IntelManager #6

---

## QA Test Cases

- **AC-1**: threats=FAILED → ERROR → 修复→RETRY → BROWSING
- **AC-2**: 2/5 query failures → graceful degradation → BROWSING + warning
- **AC-3/4**: 连点/double-click → route_committed 仅一次
- **AC-5**: knowledge revoked → forced deselection
- **AC-6**: dock changed → re-evaluation
- **AC-7**: ability unlocked → UNAVAILABLE→BROWSABLE
- **AC-8**: route deleted from registry → cache purge
- **AC-9**: All unknown → empty BROWSING (not ERROR)
- **AC-10**: Zero departable → all UNAVAILABLE + context message
- **AC-11**: All rumored + hide → empty + context message → toggle back
- **AC-12**: traversable+unknown → warning log + hidden
- **AC-13**: Risk change between steps → fresh data in overlay
- **AC-14 through AC-18**: Keyboard navigation full flow

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/chart/edgecases/EdgeCasesTest.csproj` — must exist and pass
**Status**: [x] 43/43 PASS — 2026-05-13；Epic #9 复审通过 — 2026-05-13

---

## Dependencies

- Depends on: Story 001-007 (all chart state machine, formulas, signals, external response, persistence), IntelManager #6, WorldRepair #13, AirshipHub #7, Registry #1, Persistence #3
- Unlocks: — (final chart story; all chart edge cases and keyboard navigation contract defined for UIManager)
