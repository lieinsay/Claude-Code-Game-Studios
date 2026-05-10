# Story 006: Edge Cases, Desktop Recovery & Accessibility

> **Epic**: UI / HUD / 航图界面
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/ui-hud-chart-interface.md`
**Requirement**: `TR-ui-001`, `TR-ui-002`, `TR-ui-003`, `TR-ui-004`

**ADR Governing Implementation**: ADR-0012 (§6 desktop window freeze 恢复, §5 dual-focus 同步, §9 空状态视图, C.9 WCAG AA, Edge Cases 全部 13 项)
**ADR Decision Summary**: 本 Story 覆盖 GDD 全部 13 个边缘情况 + 桌面窗口生命周期特殊性 + WCAG AA 无障碍验证。核心边缘情况：桌面窗口失焦/恢复 恢复（NOTIFICATION_APPLICATION_RESUMED + delta > 1.0s → _request_full_ui_refresh() 绕过脏标记全量刷新）；多模态同时请求（S7 覆盖 / S10 排队 / 其余丢弃→Toast）；面板打开期间底层数据变更（面板使用打开时快照——不自动刷新；提交动作通过领域 API 写回）；零物品面板空状态视图（S11/S12/S4）；departure_locked 期间面板请求静默拒绝；命名模态到达序列时序冲突（4 路合取 + skip_count >= 3 不弹出）；船体归零返回 Hub（船体红波段 + 闪烁扳手图标 + can_depart()=false）；货舱模块未安装（S1 货舱区域置灰 + "无货舱"文本）；战斗威胁覆盖容量取舍面板竞态防御；Tab 导航时面板内无可聚焦元素（焦点不穿透）；S7 战斗面板中 Esc 无效；桌面后台恢复 UI 不同步（delta > 1.0s 全量刷新）。WCAG AA 硬性要求：所有 <24px 颜色编码元素必须形状+颜色+文字三重编码；船体波段=色条+分段数+形状(✓/⚡/○)；材料满足/不足=颜色+✓/✗图标+文字；危险红 #D4644B 在帆布米 #E4D2B3 上对比度 ≥ 4.52:1。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: NOTIFICATION_APPLICATION_RESUMED 时 delta > 1.0s → 全量 UI 刷新；所有 <24px 颜色编码元素三重编码（颜色+形状+文字）；面板空状态视图必须存在——不显示空白面板
- Forbidden: 在 freeze 恢复后信任脏标记——必须强制全量刷新；仅用颜色区分状态——24px 以下元素必须附加形状/图标/文字
- Guardrail: 墨水扩散 Shader 在 desktop Compatibility renderer 上精度不足→回退 create_tween() 控制 ColorRect 片段沿航线路径依次显示

---

## Acceptance Criteria

### Desktop Window Focus Recovery

- [ ] **AC-1**: GIVEN 玩家在 Hub S1 HUD 正常 + S12 仓库面板打开，WHEN 桌面窗口失焦→切回（NOTIFICATION_APPLICATION_RESUMED），THEN S1 HUD 与窗口失焦前一致 + S12 面板仍在打开状态 + 焦点位置不变 + WASD 移动恢复
- [ ] **AC-2**: GIVEN 桌面窗口恢复 恢复 + _process delta > 1.0s，WHEN 检测到异常大 delta，THEN _request_full_ui_refresh() 被调用。所有活跃 HUD 元素从领域系统强制拉取最新值——绕过脏标记。面板状态从内存恢复
- [ ] **AC-3**: GIVEN 桌面窗口恢复 恢复 + delta ≤ 1.0s（正常窗口失焦），WHEN 检测，THEN 不触发 full_ui_refresh。依赖正常脏标记更新

### Data Race — Panel Open Snapshot

- [ ] **AC-4**: GIVEN S8 修复面板已打开（显示材料 A 需要 3 个），WHEN 面板打开期间其他系统修改了材料 A 的数量（通过非 UI 路径），THEN S8 面板内数据不变——保持打开时快照。面板关闭后 HUD 在下一帧通过脏标记更新
- [ ] **AC-5**: GIVEN S6a 容量取舍面板已打开，WHEN 面板打开期间新物品被捡起（探索暂停中——已由 #11 EC-11-04 保护），THEN S6a 面板内列表不变——竞态已在上游预防

### Empty State Views

- [ ] **AC-6**: GIVEN S11 嗅辨面板打开 + get_sniff_items() 返回 []，WHEN 渲染，THEN 显示空状态文本——不显示空白面板
- [ ] **AC-7**: GIVEN S12 仓库面板打开 + 仓库为空，WHEN 渲染，THEN 显示空状态文本 + 引导提示
- [ ] **AC-8**: GIVEN S4 航图打开 + 无可见路线，WHEN 渲染，THEN 显示空状态文本

### Naming Modal Timing

- [ ] **AC-9**: GIVEN 到达序列 + naming_prompt_eligibility()=true，WHEN arrival_complete 触发，THEN S10 在到达序列最后 0.3s 弹出。S10 阻断所有 UI 含出航控件
- [ ] **AC-10**: GIVEN naming_skip_count=3，WHEN 到达序列触发，THEN naming_prompt_eligibility()=false。S10 不弹出——窗口已关闭

### Hull Zero Return to Hub

- [ ] **AC-11**: GIVEN hull=0 返回 Hub + can_depart()=false，WHEN S1 HUD 渲染，THEN 船体条显示红波段（hull=1 显示为红波段阈值）+ 船体条旁闪烁扳手图标（"需要维修"）
- [ ] **AC-12**: GIVEN 货舱模块未安装，WHEN S1 HUD 渲染，THEN 货舱装载区域置灰 + 文本显示"无货舱"。S12 货舱整理面板不可打开——Use 货舱锚点时显示 Toast"需要先安装货舱模块"

### Combat Override Race Defense

- [ ] **AC-13**: GIVEN S6a 容量取舍面板已打开 + 战斗威胁触发（违反 #11 EC-11-04 的竞态），WHEN S7 覆盖 S6a，THEN S6a 状态保存到 _combat_override_stack。应急/硬扛后恢复 S6a——全部物品决策保留。撤退后丢弃 S6a

### Tab Navigation Edge Cases

- [ ] **AC-14**: GIVEN 模态面板内唯一可聚焦元素是灰显按钮，WHEN Tab 键，THEN 灰显按钮仍可聚焦——但 Enter 无响应 + tooltip 显示为何不可用
- [ ] **AC-15**: GIVEN 面板内无可聚焦元素（全部 FOCUS_NONE），WHEN 面板打开，THEN 焦点停留在面板容器本身——不穿透到下层 UI
- [ ] **AC-16**: GIVEN 面板关闭时打开前的焦点元素已被销毁（场景切换），WHEN 恢复焦点，THEN 焦点移到当前屏幕第一个可聚焦元素

### S7 Esc Block

- [ ] **AC-17**: GIVEN S7 战斗面板打开，WHEN 按 Esc，THEN Esc 被消费但不触发关闭。视觉提示"选择一个响应以继续"显示

### WCAG AA — Triple Encoding

- [ ] **AC-18**: GIVEN S1 船体波段指示灯渲染，WHEN 检查，THEN 颜色（绿/黄/红）+ 形状（✓/⚡/○）+ 分段数（3/2/1）三重编码同时存在。不可仅依赖颜色
- [ ] **AC-19**: GIVEN S8 修复面板材料满足/不足指示，WHEN 检查，THEN 满足=绿色+✓图标+文字"满足"；不足=红色+✗图标+文字"不足"。仅颜色的元素不存在
- [ ] **AC-20**: GIVEN 任何 <24px 的状态指示元素，WHEN 审计，THEN 全部使用颜色+形状+边缘特征三重编码

### Color Contrast — WCAG AA

- [ ] **AC-21**: GIVEN 危险红色文本 #D4644B 在帆布米底色 #E4D2B3 上，WHEN 测量对比度，THEN ≥ 4.52:1（AA 达标）
- [ ] **AC-22**: GIVEN 航标青文本 #4FB7B2 在帆布米底色上，WHEN 测量对比度，THEN ≥ 3:1（≥18px 文本 AA 达标）

### Highlightable Markers for Onboarding (#18)

- [ ] **AC-23**: GIVEN 修复站点锚点（S8），WHEN 检查，THEN highlightable=true + highlight_priority 字段存在。供 #18 新手引导系统高亮定位
- [ ] **AC-24**: GIVEN departure_locked 期间 + 引导高亮请求，WHEN 检查，THEN 引导高亮不可覆盖 departure_locked 状态——引导请求被静默拒绝

---

## Implementation Notes

### Desktop Focus Recovery

```text
var _last_delta: float = 0.0

func _process(delta: float) -> void:
    _last_delta = delta
    # ... dirty flag batch update ...

func _notification(what: int) -> void:
    if what == NOTIFICATION_APPLICATION_RESUMED:
        if _last_delta > 1.0:
            _request_full_ui_refresh()

func _request_full_ui_refresh() -> void:
    # 绕过脏标记——从所有领域系统强制拉取最新值
    for element_id in _get_all_active_hud_elements():
        var payload := _query_domain_data_for_element(element_id)
        _update_hud_element(element_id, payload)
    _state["_dirty_flags"].clear()
```

### Triple Encoding Helper

```text
func _set_hull_indicator(band: int) -> void:
    var color: Color
    var shape: String
    var segments: int
    match band:
        BAND_GREEN:
            color = Color("#5FAF5F")
            shape = "✓"
            segments = 3
        BAND_YELLOW:
            color = Color("#E8A840")
            shape = "⚡"
            segments = 2
        BAND_RED:
            color = Color("#D4644B")
            shape = "○"
            segments = 1
    # 应用: color + shape icon + segment_count text ——三者同时设置
    _hull_bar.self_modulate = color
    _hull_indicator.text = shape
    _hull_segments.text = str(segments)
```

### Highlightable Anchor

```text
func _register_highlightable(anchor_node: Node, panel_id: StringName, priority: int) -> void:
    anchor_node.set_meta(&"highlightable", true)
    anchor_node.set_meta(&"highlight_priority", priority)
    anchor_node.set_meta(&"highlight_panel_id", panel_id)
```

---

## Out of Scope

- 引导高亮的视觉实现（脉冲光、提示文字）——属于 #18 Onboarding
- #11 EC-11-04 的具体保护逻辑——属于 exploration-scavenge Epic
- 墨水扩散 Shader desktop Compatibility renderer 回退方案的 GLSL → Tween 迁移——属于 godot-shader-specialist
- 桌面 audio device readiness 恢复——属于 ADR-0019 / platform-session-shell Epic
- can_depart()=false 的业务逻辑——属于 #8 Module/Hull Epic

---

## QA Test Cases

- **AC-1-3**: desktop window freeze → resume (delta > 1.0s vs normal)
- **AC-4/5**: Panel snapshot isolation during data changes
- **AC-6-8**: Empty state views (S4/S11/S12)
- **AC-9/10**: Naming modal timing + skip lockout
- **AC-11/12**: Hull zero + cargo module missing states
- **AC-13**: Combat override race defense
- **AC-14-16**: Tab navigation edge cases
- **AC-17**: S7 Esc blocked
- **AC-18-20**: WCAG AA triple encoding verified
- **AC-21/22**: Color contrast AA compliance
- **AC-23/24**: Highlightable markers for onboarding

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/ui-hud-interface/EdgeCasesDesktopA11yTest.csproj` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (screen states), Story 002 (modal stack + input routing), Story 003 (HUD dirty flags + full_ui_refresh), Story 004 (upstream data contracts — empty states), Story 005 (animation interruption)
- Unlocks: N/A — 这是 UI / HUD / 航图界面 Epic 的最后一个 Story
