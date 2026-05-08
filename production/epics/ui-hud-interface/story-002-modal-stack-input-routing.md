# Story 002: Modal Stack, Combat Override & Input Routing

> **Epic**: UI / HUD / 航图界面
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/ui-hud-chart-interface.md`
**Requirement**: `TR-ui-002`

**ADR Governing Implementation**: ADR-0012 (§3 模态栈, §4 4 层输入路由, §5 Godot 4.6 Dual-Focus 同步策略, C.3/C.4/C.5/C.11)
**ADR Decision Summary**: UIManager 维护单槽模态栈 `_modal_panel` + `_combat_override_stack`。规则 1：同时最多一个模态面板。规则 2：S7（战斗威胁）是唯一可覆盖当前模态的面板——覆盖流程保存被覆盖面板完整上下文（panel_id, data_context, scroll_offset, selected_index）→ 被覆盖面板 process_mode=DISABLED + modulate.a=0.2 → S7 渲染在 CanvasLayer=100。恢复规则：应急处理/硬扛后完整恢复被覆盖面板；撤退后丢弃被覆盖面板。规则 3 排队策略：S7 覆盖、S10（命名）排队（当前模态关闭后自动打开）、其余模态丢弃→Toast"当前无法操作"。规则 4：多个非模态面板可同时打开，z-index 递增。4 层输入路由（Layer 0 模态→Layer 1 半模态→Layer 2 非模态→Layer 3 HUD→Layer 4 世界交互），由 `_get_active_input_layer()` 判定。全局按键（Esc/M/Tab/Enter/WASD/E）在不同 Layer 有不同行为。Godot 4.6 dual-focus 显式同步：所有可交互控件在 MOUSE_BUTTON_PRESSED 时显式 grab_focus()；Theme focus 样式（航标青 #4FB7B2 1.5px 边框）与 hover 样式（10% 亮度叠加无边框）视觉分离；只读元素 focus_mode=FOCUS_NONE；模态打开时自动焦点移到第一个可交互元素；模态关闭时恢复焦点到打开前元素。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: 同时最多一个模态面板——_modal_panel 单槽；S7 是唯一 CombatOverride 例外；所有可交互 Control 的 MOUSE_BUTTON_PRESSED 必须显式 grab_focus()
- Forbidden: 模态面板使用 process_mode=DISABLED 阻断而非 UIManager 仲裁——动画/信号被一并阻断；键盘 focus 样式与鼠标 hover 样式相同——视觉不可混淆
- Guardrail: CombatOverride 恢复时 is_instance_valid() 检查——若被覆盖面板已失效则从数据源重新 bind

---

## Acceptance Criteria

### Modal Stack — Single Slot

- [ ] **AC-1**: GIVEN 无模态面板打开，WHEN open_modal(S8)，THEN _modal_panel=S8 实例 + _modal_id=S8。模态遮罩渲染。WASD 移动被阻断
- [ ] **AC-2**: GIVEN S8 已打开，WHEN open_modal(S9)，THEN 返回 ERR_ANOTHER_MODAL_OPEN。S9 被丢弃→Toast "当前无法操作"。S8 保持打开。_modal_panel 不变
- [ ] **AC-3**: GIVEN S8 已打开 + 玩家按 Esc，WHEN close_modal()，THEN S8 关闭。_modal_panel=null + _modal_id=""。焦点恢复到打开 S8 之前的元素。WASD 移动恢复

### Combat Override (S7)

- [ ] **AC-4**: GIVEN S6a（容量取舍）已打开 + 战斗威胁触发，WHEN open_modal(S7)，THEN S6a 状态保存到 _combat_override_stack {panel_id, data_context, scroll_offset, selected_index}。S6a process_mode=DISABLED + modulate.a=0.2。S7 渲染在 CanvasLayer=100
- [ ] **AC-5**: GIVEN S7 覆盖 S6a + 玩家选择"应急处理"或"硬扛"，WHEN threat_resolved，THEN S7 关闭。从 _combat_override_stack 恢复 S6a——滚动位置、选中索引、物品决策上下文不丢失。_combat_override_stack=null
- [ ] **AC-6**: GIVEN S7 覆盖 S6a + 玩家选择"撤退"，WHEN threat_resolved，THEN S7 关闭。S6a 被丢弃（不恢复）——撤退意味着"放弃当前探索动作"。_combat_override_stack=null
- [ ] **AC-7**: GIVEN 无模态面板 + S7 触发，WHEN open_modal(S7)，THEN 正常打开——无覆盖保存。关闭后恢复正常屏幕状态

### Queue Strategy

- [ ] **AC-8**: GIVEN 无模态面板 + naming_eligible=true，WHEN open_modal(S10)，THEN S10 命名模态正常打开
- [ ] **AC-9**: GIVEN S6c（结算摘要）已打开 + 同时 naming_eligible=true，WHEN open_modal(S10)，THEN S10 排队——pending_modal=S10。S6c 关闭后 S10 自动打开

### Non-Modal Panel Coexistence

- [ ] **AC-10**: GIVEN S11（嗅辨）已打开，WHEN open_non_modal(S12 仓库)，THEN S11 和 S12 同时可见。S12 z-index > S11。WASD 移动仍可用
- [ ] **AC-11**: GIVEN S11+S12 同时打开 + 玩家按 Esc，WHEN esc_pressed，THEN 最后打开的面板（S12）先关闭。再次 Esc→S11 关闭。LIFO 顺序

### 4-Layer Input Routing

- [ ] **AC-12**: GIVEN Layer 0（模态 S3/S6a/S7/S8/S9/S10 任一打开），WHEN 按 WASD，THEN 玩家不移动。按键被模态层消费
- [ ] **AC-13**: GIVEN Layer 1（S6b 撤离读条），WHEN 按 WASD，THEN 玩家不移动。读条期间移动冻结
- [ ] **AC-14**: GIVEN Layer 2（S2 非模态/S11/S12 任一打开），WHEN 按 WASD，THEN 玩家正常移动。非模态面板不阻断世界交互
- [ ] **AC-15**: GIVEN Layer 3（S1/S5 HUD 覆盖层），WHEN 鼠标点击 HUD 区域，THEN 点击穿透到世界层（mouse_filter=IGNORE）。物品栏格除外——物品栏格 mouse_filter=STOP
- [ ] **AC-16**: GIVEN Layer 4（世界交互），WHEN 按 WASD/E，THEN 玩家正常移动/交互

### Global Keys

- [ ] **AC-17**: GIVEN Layer 0 模态面板打开（非 S7/S10），WHEN 按 Esc，THEN 关闭当前模态面板
- [ ] **AC-18**: GIVEN S7 战斗面板打开，WHEN 按 Esc，THEN 无效。Esc 被消费但不触发关闭。视觉提示"选择一个响应以继续"
- [ ] **AC-19**: GIVEN S10 命名模态打开，WHEN 按 Esc，THEN 等价于 skip_naming()——跳过命名
- [ ] **AC-20**: GIVEN Layer 4 + Hub 中，WHEN 按 M 键，THEN S4 航图打开。Layer 0-2 时按 M 键无效
- [ ] **AC-21**: GIVEN 模态面板打开，WHEN 按 Tab，THEN 焦点在模态面板内循环——不穿透到下层 UI 或世界交互层

### Godot 4.6 Dual-Focus Sync

- [ ] **AC-22**: GIVEN 修复面板 S8 中的"+1"按钮，WHEN 鼠标点击该按钮，THEN grab_focus() 被调用。键盘焦点移到该按钮——后续 Tab 键从该位置继续
- [ ] **AC-23**: GIVEN 键盘焦点在"确认提交"按钮上，WHEN 鼠标悬浮在"取消"按钮上，THEN "确认提交"有航标青 focus 边框，"取消"有 10% 亮度 hover 底色。两套样式同时可见但不混淆
- [ ] **AC-24**: GIVEN 修复面板中"节点名称"标签（只读），WHEN Tab 键遍历焦点，THEN 该标签被跳过（focus_mode=FOCUS_NONE）
- [ ] **AC-25**: GIVEN 模态面板 S8 打开，WHEN 面板打开完成，THEN 焦点自动移到面板内第一个可交互元素（grab_focus()）

---

## Implementation Notes

### Combat Override

```gdscript
func _open_modal_combat_override(panel_id: StringName, data: Dictionary) -> void:
    if _state["_modal_panel"] != null:
        _state["_combat_override_stack"] = {
            "panel_id": _state["_modal_id"],
            "scroll_offset": _get_scroll_offset(_state["_modal_panel"]),
            "selected_index": _get_selected_index(_state["_modal_panel"]),
        }
        _state["_modal_panel"].process_mode = Node.PROCESS_MODE_DISABLED
        _state["_modal_panel"].modulate.a = 0.2

    var s7 := _instantiate_panel(&"S7")
    s7.canvas_layer = COMBAT_OVERRIDE_LAYER  # 100
    _state["_modal_panel"] = s7
    _state["_modal_id"] = &"S7"

func _restore_from_combat_override() -> void:
    var saved := _state["_combat_override_stack"]
    if saved == null:
        return
    var panel := _state["_modal_panel"]
    panel.process_mode = Node.PROCESS_MODE_INHERIT
    panel.modulate.a = 1.0
    _restore_scroll_offset(panel, saved["scroll_offset"])
    _restore_selected_index(panel, saved["selected_index"])
    _state["_combat_override_stack"] = null
```

### Input Layer Determination

```gdscript
func _get_active_input_layer() -> int:
    if _state["_modal_panel"] != null:
        return 0
    if _is_semi_modal_open():
        return 1
    if _state["_non_modal_panels"].size() > 0:
        return 2
    if _is_hud_overlay_active():
        return 3
    return 4
```

### Global Key Dispatch

```gdscript
func _unhandled_input(event: InputEvent) -> void:
    var layer := _get_active_input_layer()

    if event.is_action_pressed(&"ui_cancel"):  # Esc
        match _state["_modal_id"]:
            &"S7": return  # 消费但不关闭——必须选择响应
            &"S10":
                _skip_naming()
                return
        if layer == 0:
            close_modal()
            return
        if layer == 2:
            _close_top_non_modal()
            return
        if _state["_active_screen"] in [&"CHART", &"CHART_ROUTE_SELECTED"]:
            _transition_screen(&"HUB")
            return

    if event.is_action_pressed(&"ui_map"):  # M key
        if layer == 4 and _state["_active_screen"] == &"HUB":
            _transition_screen(&"CHART")
            return
```

---

## Out of Scope

- 屏幕状态机的状态转换——属于 Story 001
- departure_locked 期间的模态拒绝——属于 Story 001 (departure_locked guard)
- HUD 脏标记更新——属于 Story 003
- 面板动画（0.25s 羊皮纸翻开）——属于 Story 005
- S7 战斗面板的威胁数据渲染——属于 #12 Combat 系统
- S10 命名模态的命名逻辑——属于 #15 Partner 系统

---

## QA Test Cases

- **AC-1-3**: Single-slot modal open/close/reject
- **AC-4-7**: Combat override save→restore/discard
- **AC-8-9**: Queue strategy S10
- **AC-10-11**: Non-modal coexistence + LIFO close
- **AC-12-16**: All 5 input layers verified (WASD block/pass matrix)
- **AC-17-21**: Global key dispatch (Esc/M/Tab) by layer/modal
- **AC-22-25**: Dual-focus sync (mouse→grab_focus, focus≠hover)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/ui-hud-interface/modal_stack_input_routing_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (screen state machine — modal opens within screen context)
- Unlocks: Story 004 (modal data binding), Story 006 (edge cases in modal stack behavior)
