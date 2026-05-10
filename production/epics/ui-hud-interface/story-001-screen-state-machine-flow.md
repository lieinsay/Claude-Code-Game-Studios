# Story 001: Screen State Machine & Screen Flow

> **Epic**: UI / HUD / 航图界面
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/ui-hud-chart-interface.md`
**Requirement**: `TR-ui-001`

**ADR Governing Implementation**: ADR-0012 (§2 屏幕状态机, §1 UIManager 内部状态, C.10 屏幕状态机)
**ADR Decision Summary**: UIManager 维护 11 态屏幕状态机（HUB → CHART → CHART_ROUTE_SELECTED → CHART_DEPARTURE_CONFIRMED → DEPARTURE_LOCKED → VOYAGE → EXPLORATION → EXTRACTING → SETTLEMENT → HUB_ARRIVING → HUB），16 个事件驱动转换。3 条强制过渡保护：departure_locked=true 期间 open_screen()/open_modal() 静默拒绝 + force_close_all_panels()；CHART_DEPARTURE_CONFIRMED 不可逆；EXTRACTING 阶段除 S7 外不可取消。12 屏清单（S1–S12）按类型分为 HUD 覆盖层（S1/S5）、全屏（S4）、模态（S3/S6a/S6c/S7/S8/S9/S10）、半模态（S6b）、非模态（S2 非模态实例/S11/S12）。UIManager._active_screen 为唯一活跃全屏状态源。全屏面板（S4）使用 visible 切换而非 change_scene。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: departure_locked 期间所有面板请求被静默拒绝——force_close_all_panels() 先执行；CHART_DEPARTURE_CONFIRMED 后不可逆返回 BROWSING；EXTRACTING 阶段除 threat_triggered 外不可取消撤离
- Forbidden: 两个全屏同时 active——_active_screen 单槽；_process() 轮询 screen 状态——所有转换为事件驱动
- Guardrail: 无效状态转换 → push_warning(dev) + no-op

---

## Acceptance Criteria

### Screen State Machine — States & Transitions

- [ ] **AC-1**: GIVEN 新游戏启动 + Hub 场景激活，WHEN UIManager 初始化完成，THEN _active_screen=HUB。S1 Hub HUD 渲染
- [ ] **AC-2**: GIVEN _active_screen=HUB + 玩家 Use 舱门或舵轮，WHEN use_gangway()/use_helm() 触发，THEN S3 出航确认模态打开。_active_screen 保持 HUB
- [ ] **AC-3**: GIVEN _active_screen=HUB + S3 模态打开 + 玩家确认出航，WHEN departure_confirmed，THEN _active_screen→DEPARTURE_LOCKED。_departure_locked=true + 2.0s timer 启动。所有面板 force_close_all_panels()
- [ ] **AC-4**: GIVEN _active_screen=DEPARTURE_LOCKED + 2.0s timer 到期，WHEN lock_timer_complete，THEN _active_screen→CHART。S4 航图屏幕打开（visible=true）
- [ ] **AC-5**: GIVEN _active_screen=HUB + 玩家按 M 键，WHEN 无模态面板打开 且 非 departure_locked，THEN _active_screen→CHART。S4 visible=true
- [ ] **AC-6**: GIVEN _active_screen=CHART + 玩家选中路线，WHEN route_selected，THEN _active_screen→CHART_ROUTE_SELECTED。侧边面板展开，"确认出航"按钮自动获得焦点
- [ ] **AC-7**: GIVEN _active_screen=CHART_ROUTE_SELECTED + 玩家确认出航，WHEN departure_confirmed，THEN _active_screen→CHART_DEPARTURE_CONFIRMED。墨水扩散 0.6s → 出发口封闭+锁定 1.2s
- [ ] **AC-8**: GIVEN _active_screen=CHART_DEPARTURE_CONFIRMED + 锁定完成，WHEN lock_complete，THEN _active_screen→VOYAGE。S4 visible=false，黑屏过渡开始
- [ ] **AC-9**: GIVEN _active_screen=CHART 或 CHART_ROUTE_SELECTED + 玩家按 Esc，WHEN esc_pressed，THEN _active_screen→HUB。S4 visible=false，Hub 恢复
- [ ] **AC-10**: GIVEN _active_screen=VOYAGE + EncounterContext 就绪，WHEN encounter_context_ready，THEN _active_screen→EXPLORATION。S5 探索 HUD 激活
- [ ] **AC-11**: GIVEN _active_screen=EXPLORATION + 玩家抵达撤离锚点确认，WHEN extraction_started，THEN _active_screen→EXTRACTING。S6b 撤离读条显示
- [ ] **AC-12**: GIVEN _active_screen=EXTRACTING + 撤离完成，WHEN extraction_complete，THEN _active_screen→SETTLEMENT。S6c 结算摘要模态打开
- [ ] **AC-13**: GIVEN _active_screen=SETTLEMENT + S6c + 玩家确认结算，WHEN settlement_confirmed，THEN _active_screen→HUB_ARRIVING。到达序列启动
- [ ] **AC-14**: GIVEN _active_screen=HUB_ARRIVING + 到达完成 + naming_eligible=true，WHEN arrival_complete，THEN _active_screen→HUB + S10 命名模态排队打开
- [ ] **AC-15**: GIVEN _active_screen=HUB_ARRIVING + 到达完成 + naming_eligible=false，WHEN arrival_complete，THEN _active_screen→HUB。S1 HUD 恢复

### Transition Guards

- [ ] **AC-16**: GIVEN _departure_locked=true，WHEN open_screen(CHART)/open_modal(S8)/press M key，THEN 全部被静默拒绝。返回 ERR_DEPARTURE_LOCKED。无任何面板打开
- [ ] **AC-17**: GIVEN _active_screen=CHART_DEPARTURE_CONFIRMED，WHEN esc_pressed，THEN 无效。状态不可逆——出航已确认
- [ ] **AC-18**: GIVEN _active_screen=EXTRACTING + S6b 进行中，WHEN 玩家按 Esc，THEN 无效。撤离读条不可手动取消（除 S7 威胁打断外）

### 12-Screen Inventory

- [ ] **AC-19**: GIVEN UIManager 初始化完成，WHEN 查询 12 屏注册表，THEN S1–S12 全部注册。每个 screen_id 对应正确的类型（HUD_OVERLAY/FULLSCREEN/MODAL/SEMI_MODAL/NON_MODAL）和所属系统
- [ ] **AC-20**: GIVEN _active_screen=HUB + S2 非模态面板打开 + WASD 按下，WHEN 检查，THEN 玩家可移动。非模态面板不阻断移动

---

## Implementation Notes

### Screen State Enum

```text
const SCREEN_HUB: StringName = &"HUB"
const SCREEN_CHART: StringName = &"CHART"
const SCREEN_CHART_ROUTE_SELECTED: StringName = &"CHART_ROUTE_SELECTED"
const SCREEN_CHART_DEPARTURE_CONFIRMED: StringName = &"CHART_DEPARTURE_CONFIRMED"
const SCREEN_DEPARTURE_LOCKED: StringName = &"DEPARTURE_LOCKED"
const SCREEN_VOYAGE: StringName = &"VOYAGE"
const SCREEN_EXPLORATION: StringName = &"EXPLORATION"
const SCREEN_EXTRACTING: StringName = &"EXTRACTING"
const SCREEN_SETTLEMENT: StringName = &"SETTLEMENT"
const SCREEN_HUB_ARRIVING: StringName = &"HUB_ARRIVING"
```

### Screen Type Classification

```text
enum ScreenType {
    HUD_OVERLAY,   # S1, S5 — 常驻 HUD，mouse_filter=IGNORE
    FULLSCREEN,    # S4 — 全屏，visible 切换
    MODAL,         # S3, S6a, S6c, S7, S8, S9, S10 — 阻断移动
    SEMI_MODAL,    # S6b — 半模态，阻断部分输入
    NON_MODAL,     # S2(非模态实例), S11, S12 — 不阻断移动
}
```

### Screen Transition

```text
func _transition_screen(new_screen: StringName) -> int:
    if _state["_departure_locked"] and new_screen != &"CHART":
        return ScreenResult.ERR_DEPARTURE_LOCKED

    var current := _state["_active_screen"]
    if not _is_valid_transition(current, new_screen):
        push_warning("UIManager: invalid transition %s → %s" % [current, new_screen])
        return ScreenResult.ERR_INVALID_SCREEN

    _state["_active_screen"] = new_screen
    _apply_screen_visibility(current, new_screen)
    return ScreenResult.SUCCESS
```

### Departure Lock

```text
func _enter_departure_locked() -> void:
    _state["_departure_locked"] = true
    _force_close_all_panels()
    _state["_active_screen"] = &"DEPARTURE_LOCKED"
    # 2.0s timer → _on_lock_timer_complete()

func _on_lock_timer_complete() -> void:
    _state["_departure_locked"] = false
    _transition_screen(&"CHART")
```

---

## Out of Scope

- 模态栈的 S7 战斗覆盖逻辑——属于 Story 002
- 4 层输入路由的 WASD 阻断——属于 Story 002
- HUD 脏标记批量更新——属于 Story 003
- 面板生命周期与缓存池——属于 Story 003
- S4 航图的数据渲染（路线列表、风险标签）——属于 #9 Chart 系统
- departure_locked 2.0s 常量的来源——属于 #7 Hub 系统（`base_lock_duration`）

---

## QA Test Cases

- **AC-1-15**: All 11 states + 16 transitions verified
- **AC-16**: departure_locked guards all open_screen/open_modal/M-key
- **AC-17**: CHART_DEPARTURE_CONFIRMED irreversible
- **AC-18**: EXTRACTING Esc blocked
- **AC-19**: 12-screen registry complete
- **AC-20**: Non-modal panels allow WASD movement

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/ui-hud-interface/ScreenStateMachineTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: ADR-0012 (UIManager architecture), ADR-0001 (Autoload #16 Phase 8), ADR-0002 (signal protocol)
- Unlocks: Story 002 (modal stack atop screen FSM), Story 004 (data contracts per screen)
