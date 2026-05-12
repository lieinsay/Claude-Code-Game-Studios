# Story 002: Input Gate & Shell Integration

> **Epic**: Player Movement & Interaction
> **Status**: Done
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-002`

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order; ADR-0002: Signal Communication Protocol; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: `InteractionRegistry`（Autoload #3）消费 `平台与会话壳` 的 `input_gate_open`/`input_gate_closed`/`input_gate_reacquire` 信号来管理输入门状态。输入门三态：`InputClosed`（壳层未放行/overlay/后台）→ `InputReacquire`（恢复后等待第一下输入被消费）→ `InputOpen`（正常玩法输入）。桌面窗口生命周期事件由壳层归一化后传入——本系统不直接订阅 `window_focus_changed`/`suspend_requested`。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: 信号连接使用 `sender.signal_name.connect(receiver.method)`；`InputReacquire` 期间第一下输入消费使用 `Input.is_anything_pressed()` 检测。

**Control Manifest Rules (Foundation layer)**:
- Required: 信号使用 typed params；input gate 三态机不可绕过
- Forbidden: 不得直接订阅桌面窗口生命周期事件（`window_focus_changed`、`suspend_requested`、`focus`）；不得在 `_process()` 中动态 connect/disconnect
- Guardrail: `first_resume_input_consumed = true`（固定不可调）

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN `input_gate_state` 为 `InputClosed`，WHEN 壳层发出 `input_gate_open` 信号，THEN 状态切换为 `InputOpen`；WHEN 壳层发出 `input_gate_closed`，THEN 状态切换为 `InputClosed`
- [ ] **AC-2**: GIVEN `input_gate_state` 为 `InputReacquire`，WHEN 玩家按下任意键或点击鼠标，THEN 该输入被消费（不产生移动或 Use），状态切换为 `InputOpen`
- [ ] **AC-3**: GIVEN `InputReacquire` 期间玩家按住移动键不放，WHEN 切换到 `InputOpen`，THEN 角色不自动开始移动（需松开再按才走）
- [ ] **AC-4**: GIVEN 壳层显示 overlay（设置/存档菜单），WHEN `input_gate_closed` 发出，THEN 移动和 Use 立即阻断；按移动键和 E/Space 均无效果
- [ ] **AC-5**: GIVEN 桌面窗口 `suspend_requested`/`window_focus_changed=hidden` 通过壳层转发为 `input_gate_closed`，WHEN 窗口失焦或暂停，THEN 输入门关闭，移动和使用全部阻断
- [ ] **AC-6**: GIVEN `input_gate_state` 变化，WHEN 新状态生效，THEN 发出 `input_gate_changed` 语义事件（包含 `previous_gate` 和 `new_gate`）

---

## Implementation Notes

- Input Gate 状态机（3 态）:
  - `InputClosed`: 壳层未放行、页面后台、overlay 可见、会话未激活
  - `InputReacquire`: 恢复后等待第一下可信输入被壳层消费
  - `InputOpen`: 会话激活且无壳层阻断
- Transitions:
  - `InputClosed → InputReacquire`: 壳层从恢复流程返回并发出 `input_gate_reacquire`
  - `InputReacquire → InputOpen`: 壳层确认第一下输入已消费（`first_resume_input_consumed` flag）
  - `InputClosed → InputOpen`: 正常进入 `SessionActive` 且无需恢复消费
  - `InputOpen → InputClosed`: 页面隐藏、失焦、暂停、壳层 overlay、错误态或切场景锁定
- 信号消费:
  - `SessionShell.input_gate_open` → `_on_input_gate_open()`
  - `SessionShell.input_gate_closed` → `_on_input_gate_closed()`
  - `SessionShell.input_gate_reacquire` → `_on_input_gate_reacquire()`
- `InputReacquire` 第一下消费: `_physics_process` 中检测 `Input.is_anything_pressed()` 或 mouse click → 设置 `first_resume_input_consumed=true` → 发信号给壳层 → 壳层发 `input_gate_open`
- 门关闭时的副作用: `movement_state → Idle`（速度归零）；`focus_state → NoFocus`（清空焦点）；`use_gate → Blocked(input_closed)`

---

## Out of Scope

- Story 001: 具体移动计算和碰撞
- Story 003: 焦点状态机（门关闭时清空焦点是副作用，但焦点逻辑由 Story 003 拥有）
- Story 004: Use gate 的具体阻断检查
- 壳层如何检测桌面窗口生命周期（由 platform-session-shell Epic 拥有）

---

## QA Test Cases

- **AC-2**: Reacquire consumes first input
  - Given: 从 desktop resume 恢复，`input_gate_state = InputReacquire`
  - When: 玩家按 W 键
  - Then: 角色不移动；`input_gate_state → InputOpen`；下一帧按 W 才走
  - Edge cases: 鼠标点击也消费 → 不触发 Use；任意键（包括 Tab）都消费

- **AC-3**: No backfill on reacquire
  - Given: `InputReacquire` 期间玩家一直按住 W
  - When: 切换到 `InputOpen`
  - Then: 角色保持 Idle；需松开 W 再按才移动
  - Edge cases: 门关闭前在移动 → 门关闭时速度归零 → 门重开后不需要"抵消"旧输入

- **AC-4**: Overlay blocks input
  - Given: 玩家正在场景中移动，壳层发出 `input_gate_closed`（因设置菜单 overlay）
  - When: 玩家按 WASD + E
  - Then: 所有移动和 Use 无效；`input_gate_state = InputClosed`
  - Edge cases: overlay 关闭 → `input_gate_open` → 恢复输入但不自动移动

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/movement/MovementInputGateTest.csproj` — must exist and pass
**Status**: [x] Implemented and passing (2026-05-12)

---

## Dependencies

- Depends on: platform-session-shell Story 001 (input_gate_open/closed/reacquire 信号定义)
- Unlocks: Story 001 (movement — 依赖 input gate 控制移动开关)；Story 003 (focus — 依赖 input gate 清空焦点)；Story 004 (use gate — input_closed 是 use_gate 的第一道检查)
