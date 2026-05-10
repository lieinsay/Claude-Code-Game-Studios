# Story 005: Animation Timing & Downstream Semantic Events

> **Epic**: UI / HUD / 航图界面
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/ui-hud-chart-interface.md`
**Requirement**: `TR-ui-004`

**ADR Governing Implementation**: ADR-0012 (§8 动画与过渡时序, §10 下游语义事件, C.8/C.13)
**ADR Decision Summary**: 所有 12 个 UI 动画使用 create_tween()（SceneTreeTween）统一管理——禁止手动 _process() 插值。动画时长范围 0.15s（面板合上）到 3.0s（进度 Toast）。墨水扩散动画为 GPU 侧 ShaderMaterial + uniform progress（0→1），禁止 Canvas draw_*() 逐帧绘制。全屏羊皮纸纹理使用 NinePatchRect 9-slice 展开。动画性能约束：所有 Tween 使用 Tween.EASE_OUT / Tween.EASE_IN 预设，不自定义缓动曲线。10 个下游语义事件信号（typed params, sync emit）供 #17 Feedback 系统消费：ui_route_selected / ui_departure_confirmed / ui_threat_response_chosen / ui_repair_submitted / ui_purchase_confirmed / ui_item_transferred / ui_naming_confirmed / ui_settlement_closed / ui_panel_opened / ui_panel_closed。所有语义事件在 UI 动作执行后发射（emit-after-action），遵循 ADR-0002 emit-after-mutation 规则。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: 所有动画使用 create_tween()——禁止 _process() 手动插值；墨水扩散使用 ShaderMaterial GPU 侧——禁止 Canvas draw_*()；语义事件在动作完成后发射——emit-after-action
- Forbidden: Tween 中使用自定义缓动曲线——仅使用 EASE_OUT / EASE_IN / EASE_IN_OUT / LINEAR 预设；墨水扩散在 CPU 侧逐帧绘制
- Guardrail: Tween 未完成时面板被强制关闭（departure_locked）→ kill_existing_tween() + 立即设置最终状态

---

## Acceptance Criteria

### Animation Contracts

- [ ] **AC-1**: GIVEN 非模态面板打开（S11），WHEN open_non_modal()，THEN create_tween() 执行 0.25s ease-out 羊皮纸翻开动画（scale 0.9→1.0 + modulate.a 0→1）
- [ ] **AC-2**: GIVEN 非模态面板关闭，WHEN close_non_modal()，THEN create_tween() 执行 0.15s ease-in 羊皮纸合上动画（scale 1.0→0.9 + modulate.a 1→0）
- [ ] **AC-3**: GIVEN 航图选中路线，WHEN route_selected，THEN 选中路线暖金描边宽度从 1px→3px→1px，0.3s ease-in-out 脉冲完成
- [ ] **AC-4**: GIVEN 航图确认出航，WHEN departure_confirmed，THEN 墨水扩散 ShaderMaterial uniform progress 0→1，0.6s ease-out。Shader 在 GPU 侧执行——CPU 仅 set_shader_parameter("progress", t) 每帧 1 次
- [ ] **AC-5**: GIVEN 墨水扩散完成，WHEN lock_complete 触发，THEN 出发口封闭动画 1.2s linear
- [ ] **AC-6**: GIVEN 撤离读条，WHEN S6b 显示，THEN progress bar 2.5s linear 0%→100%。进度文本实时更新
- [ ] **AC-7**: GIVEN Toast 触发（修复提交），WHEN toast.show()，THEN 入场动画 0.2s ease-out（从下方滑入+淡入），停留 2.8s，退场自动移除
- [ ] **AC-8**: GIVEN 结算摘要触发，WHEN S6c 打开，THEN 入场动画 0.5s ease-out
- [ ] **AC-9**: GIVEN 命名模态触发，WHEN S10 打开，THEN 弹出动画 0.3s ease-out

### Animation Interruption

- [ ] **AC-10**: GIVEN 非模态面板 S11 的翻开 tween 正在播放（0.1s 处），WHEN departure_locked 触发 force_close_all_panels()，THEN S11 的 tween 被 kill()。面板立即设置为关闭状态（modulate.a=0, visible=false）
- [ ] **AC-11**: GIVEN S6b 撤离读条 tween 正在播放（1.5s 处），WHEN threat_triggered 打断撤离，THEN 读条 tween 被 kill()。进度条红色闪烁 0.5s×3 次后关闭

### NinePatchRect Texture

- [ ] **AC-12**: GIVEN 全屏羊皮纸面板（S4 航图背景 + 非模态面板背景），WHEN 渲染，THEN 使用 NinePatchRect + 256×256 源纹理 9-slice 展开。不使用整张 2048×2048 纹理

### Downstream Semantic Events

- [ ] **AC-13**: GIVEN 航图中玩家选中路线 "storm-cut-01"，WHEN route_selected 处理完成，THEN ui_route_selected("storm-cut-01", "风暴走廊") 信号发射。信号在屏幕状态变更后发射
- [ ] **AC-14**: GIVEN 出航确认完成（departure_confirmed），WHEN 状态变更完成，THEN ui_departure_confirmed(route_id, departure_mode) 信号发射
- [ ] **AC-15**: GIVEN 战斗面板中玩家选择"应急处理"，WHEN threat_resolved 完成，THEN ui_threat_response_chosen(threat_id, "suppressed") 信号发射
- [ ] **AC-16**: GIVEN 修复面板中玩家确认提交材料，WHEN submit_repair 返回成功，THEN ui_repair_submitted(node_id, materials_submitted) 信号发射
- [ ] **AC-17**: GIVEN 摊位界面中玩家确认购买，WHEN execute_purchase 返回成功，THEN ui_purchase_confirmed(stall_id, good_id, qty, total_cost) 信号发射
- [ ] **AC-18**: GIVEN 仓库整理中玩家转移物品，WHEN transfer 完成，THEN ui_item_transferred(item_id, from_pool, to_pool, qty) 信号发射
- [ ] **AC-19**: GIVEN 命名模态中玩家提交名字"小云"，WHEN submit_partner_name 返回成功，THEN ui_naming_confirmed("partner.sky-cat", "小云") 信号发射
- [ ] **AC-20**: GIVEN 结算摘要关闭，WHEN S6c close_modal()，THEN ui_settlement_closed(voyage_id, items_brought, intel_gained) 信号发射
- [ ] **AC-21**: GIVEN 任何面板打开，WHEN 面板 ACTIVE，THEN ui_panel_opened(panel_id) 信号发射。GIVEN 任何面板关闭，WHEN 面板 CLOSED，THEN ui_panel_closed(panel_id) 信号发射

### Signal Contract Compliance (ADR-0002)

- [ ] **AC-22**: GIVEN 所有 10 个 ui_* 信号，WHEN 检查声明，THEN 全部使用 typed params + sync emit。不包含 Dictionary payload
- [ ] **AC-23**: GIVEN ui_panel_opened 信号发射，WHEN 检查 cascade depth，THEN cascade depth ≤ 2（#16→#17→消费方结束）。不触发 ≥3 级级联

---

## Implementation Notes

### Tween Wrapper

```text
func _tween_panel_open(panel: Control) -> void:
    panel.modulate.a = 0.0
    panel.scale = Vector2(0.9, 0.9)
    panel.visible = true
    var tw := create_tween()
    tw.set_ease(Tween.EASE_OUT)
    tw.tween_property(panel, "modulate:a", 1.0, 0.25)
    tw.parallel().tween_property(panel, "scale", Vector2(1.0, 1.0), 0.25)

func _tween_panel_close(panel: Control) -> void:
    var tw := create_tween()
    tw.set_ease(Tween.EASE_IN)
    tw.tween_property(panel, "modulate:a", 0.0, 0.15)
    tw.parallel().tween_property(panel, "scale", Vector2(0.9, 0.9), 0.15)
    tw.finished.connect(panel.queue_free)
```

### Ink Spread ShaderMaterial

```text
func _play_ink_spread() -> void:
    var mat := _ink_spread_material as ShaderMaterial
    var tw := create_tween()
    tw.set_ease(Tween.EASE_OUT)
    tw.tween_method(_set_ink_progress.bind(mat), 0.0, 1.0, 0.6)

func _set_ink_progress(value: float, mat: ShaderMaterial) -> void:
    mat.set_shader_parameter(&"progress", value)
```

### Semantic Event Emission

```text
func _emit_ui_event(signal_name: StringName, args: Array = []) -> void:
    # emit-after-action — domain state already committed
    match signal_name:
        &"ui_route_selected":
            ui_route_selected.emit(args[0], args[1])
        &"ui_departure_confirmed":
            ui_departure_confirmed.emit(args[0], args[1])
        &"ui_panel_opened":
            ui_panel_opened.emit(args[0])
        # ... 其余同理
```

---

## Out of Scope

- 墨水扩散 Shader 的 GLSL 代码编写——属于 godot-shader-specialist
- #17 Feedback 系统对 ui_* 信号的消费（音频/VFX 触发）——属于 feedback-fx-audio Epic
- 面板打开/关闭的触发逻辑——属于 Story 001/002/003
- 领域系统 API 调用的返回成功/失败——属于各自系统

---

## QA Test Cases

- **AC-1-9**: All 12 animation contracts verified (duration, easing, property)
- **AC-10/11**: Animation interruption on departure_locked and threat
- **AC-12**: NinePatchRect texture usage
- **AC-13-21**: All 10 semantic events emitted at correct timing
- **AC-22**: Typed params + sync emit compliance
- **AC-23**: Cascade depth ≤ 2

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/ui-hud-interface/AnimationEventsTest.csproj` — must exist and pass, OR documented playtest
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (screen transitions trigger animations), Story 002 (modal open/close triggers events), Story 004 (domain API calls trigger events on success)
- External: #17 Feedback (consumes ui_* signals)
- Unlocks: Story 006 (animation edge cases, interruption recovery)
