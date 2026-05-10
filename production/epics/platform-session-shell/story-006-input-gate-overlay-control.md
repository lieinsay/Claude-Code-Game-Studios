# Story 006: Input Gate & Shell Overlay Control

> **Epic**: Platform Session Shell
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-001`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: 壳层只做输入门禁——SessionActive 下普通玩法输入下放给玩家移动与交互系统，壳层仅保留 Esc 暂停和生命周期级拦截。壳层 overlay 可见时焦点不传入 HUD 或玩法层。input_gate 三态：Open（正常玩法输入）、Reacquire（恢复后需重新激活）、Blocked（壳层拦截）。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Godot `_input(event)` 在 Autoload 中优先级高于场景节点；`accept_event()` 或 `SceneTree.set_input_as_handled()` 阻止事件冒泡。

**Control Manifest Rules (Foundation layer)**:
- Required: overlay 可见时壳层独占输入；input_gate 必须在状态变更后立即更新
- Forbidden: 不得在 Blocked/Reacquire 期间下放输入给玩法层
- Guardrail: input_gate 判定 <0.01ms

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 会话状态为 Active、页面在前台、输入焦点正常且无模态阻挡，WHEN 计算 input_gate，THEN 结果为 Open
- [ ] **AC-2**: GIVEN 会话状态为 BackgroundSuspended 且页面已回前台，WHEN 计算 input_gate，THEN 结果为 Reacquire
- [ ] **AC-3**: GIVEN 壳层 overlay 可见，WHEN 玩家使用鼠标/键盘/快捷键操作，THEN 焦点和输入必须停留在壳层 overlay——不得传入 HUD 或玩法层
- [ ] **AC-4**: GIVEN SessionActive 且 input_gate=Open，WHEN 玩家按 Esc，THEN 壳层拦截并打开暂停菜单（或触发威胁决策面板优先于暂停——由 ADR-0018 定义）
- [ ] **AC-5**: GIVEN ResumePending 且 input_gate=Reacquire，WHEN 玩家按下键盘/鼠标，THEN 该输入只用于重新激活——`input_gate` 变为 Open——不触发任何玩法动作
- [ ] **AC-6**: GIVEN 壳层 overlay 关闭且 input_gate=Open，WHEN 玩家按下 W/A/S/D/E/Tab/I/M，THEN 输入路由至 #4 (InteractionRegistry) 或 #16 (UIManager)

---

## Implementation Notes

- `input_gate` 枚举: `enum InputGate { OPEN, REACQUIRE, BLOCKED }`
- `_input(event)` 在 SessionShell Autoload 中实现——优先级最高
- 输入路由逻辑:
  ```
  if input_gate == BLOCKED: accept_event(); return
  if input_gate == REACQUIRE: _handle_reactivation(event); accept_event(); return
  if _overlay_visible: _route_to_overlay(event); accept_event(); return
  if event.is_action_pressed("ui_cancel"): _open_pause_or_threat_panel()
  # else: let event propagate to gameplay
  ```
- overlay 可见性由 `_overlay_visible: bool` 控制——在任何非 SessionActive 壳层状态为 true；SessionActive 时为 false
- `_handle_reactivation(event)`：设置 `input_gate = OPEN`，emit `session_reactivated`，尝试音频恢复（如果 audio_gate=Muted），标记 `_resume_activation_consumed = true`

---

## Out of Scope

- InteractionRegistry (#4): 玩法输入的具体处理（WASD/Click-to-Move/E 交互）
- ADR-0012 (UIManager): 暂停菜单 UI 和威胁决策面板的渲染
- Story 003: ResumePending 状态和重新激活流程

---

## QA Test Cases

- **AC-3**: Overlay captures all input
  - Given: Loading overlay 可见
  - When: 玩家按 W/A/S/D/E/Tab
  - Then: 所有按键被壳层捕获——角色不移动、背包不打开
  - Edge cases: 系统快捷键（如操作系统窗口快捷键）不受影响（由操作系统处理）

- **AC-5**: Reacquire gate → single input only
  - Given: input_gate=Reacquire，恢复面板显示
  - When: 玩家按 W（移动意图）→ input_gate 变为 Open → 玩家再次按 W
  - Then: 第一次 W 被消耗为重新激活（不移动）；第二次 W 正常移动角色
  - Edge cases: 鼠标移动不触发重新激活——仅按键和点击触发

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/session/InputGateTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (State Machine), Story 003 (Suspend/Resume)
- Depends on: InteractionRegistry (#4) — 玩法输入路由目标
- Unlocks: Story 007 (Shell UI 需要 input_gate 保护)
