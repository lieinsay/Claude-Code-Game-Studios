# Story 004: Use Gate & Dispatch

> **Epic**: Player Movement & Interaction
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-004`

**ADR Governing Implementation**: ADR-0004: InteractionHandler @abstract
**ADR Decision Summary**: Use 分发采用双通道模式——`interaction_used` signal（fire-and-forget，反馈系统消费）+ `handle_use(player_id)` method call（request-response，领域系统消费）。领域系统必须返回 `UseResult` 枚举（ACCEPTED/REJECTED/BUSY）。Use Gate 检查顺序: input_gate_open → ui_modal_blocked → focus_selection ≠ NoFocus → target_enabled → path_clear → distance_ok → target_busy。UseLocked 状态下重复按 Use 被忽略；超时（默认 2s）后自动释放。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `Input.is_action_just_pressed("interact")` 检测 Use 键（非 `is_action_pressed`）；`UseResult` 枚举定义在 `Interactable` 基类中。

**Control Manifest Rules (Foundation layer)**:
- Required: Use 分发走 signal + method call 双通道；UseResult 必须返回枚举
- Forbidden: 不得在 UseLocked 期间接受新的 Use；不得用 `is_action_pressed()` 代替 `is_action_just_pressed()`
- Guardrail: `use_lock_timeout_seconds = 2.0s`；`input_buffer_window = 0.10s`

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN `use_gate = Allowed` 且玩家按下 E/Space，WHEN 分发执行，THEN `interaction_used` signal 发出（含正确 `target_id`），`handle_use(player_id)` 被调用，焦点进入 `UsePending → UseLocked`
- [ ] **AC-2**: GIVEN `distance_to_anchor > reach_limit`，WHEN 玩家按 Use，THEN `use_gate = Blocked(too_far)`，不发送 `interaction_used`
- [ ] **AC-3**: GIVEN 玩家和目标之间有 Layer 4 遮挡体且 `path_clear=false`，WHEN 玩家按 Use，THEN `use_gate = Blocked(blocked)`
- [ ] **AC-4**: GIVEN `target_busy = true`，WHEN 玩家按 Use，THEN `use_gate = Blocked(target_busy)`
- [ ] **AC-5**: GIVEN 无世界焦点（`NoFocus`），WHEN 玩家按 Use，THEN `use_gate = Blocked(no_focus)`，不显示 Toast，玩家角色上出现 ~0.1s 微闪脉冲
- [ ] **AC-6**: GIVEN UI 模态面板打开（`ui_modal_blocked=true`），WHEN 玩家按 Use，THEN `use_gate = Blocked(ui_modal_blocked)`
- [ ] **AC-7**: GIVEN `world_focus_state = UseLocked`，WHEN 玩家重复按 Use，THEN 不发出新的 `interaction_used`，`handle_use()` 不被重复调用
- [ ] **AC-8**: GIVEN 领域系统在 `use_lock_timeout_seconds`（默认 2.0s）内未释放 UseLocked，WHEN 超时，THEN 自动取消 UseLocked，恢复焦点和移动
- [ ] **AC-9**: GIVEN 焦点目标在 `UsePending` 帧和 Use 执行帧之间被禁用/销毁，WHEN Use 执行，THEN `use_gate = Blocked(target_disabled)`，不崩溃
- [ ] **AC-10**: GIVEN 玩家在 `input_buffer_window`（默认 0.10s）内提前按下 Use（如 UseLocked 释放前 2 帧），WHEN 窗口结束，THEN 缓冲输入被重新评估；若仍有效则执行 Use
- [ ] **AC-11**: GIVEN `UseLocked` 且领域系统将 movement 设为 `Rooted`，WHEN 玩家按移动键，THEN 移动键无效

---

## Implementation Notes

- `use_gate` 公式:
  ```
  distance_ok = distance_to_anchor <= reach_limit
  use_gate = Allowed if G AND NOT U AND SEL≠NoFocus AND E AND P AND DO AND NOT B
             else Blocked(block_reason)
  ```
  block_reason 优先级: `input_closed > ui_modal_blocked > no_focus > target_disabled > blocked > too_far > target_busy`
- Use Request Dispatch:
  1. Signal（fire-and-forget）: `interaction_used.emit(target_id: StringName, interaction_type: StringName)` → FeedbackManager
  2. Method call（request-response）: `result = target.handle_use(player_id)` → UseResult
     - `ACCEPTED`: 进入 UseLocked，领域系统负责释放
     - `REJECTED`: 恢复焦点，输出 block_reason
     - `BUSY`: 保持焦点，输出 target_busy
- `Input.is_action_just_pressed("interact")` 检测 — 非 `is_action_pressed`
- UseLocked 行为:
  - `movement_state → Rooted`（可选——由领域系统决定）
  - 重复 Use 键被忽略，不排队
  - 超时: 2s 后 `release_use_lock(target_id)` 自动调用
- Input buffer:
  - `input_buffer_timer` 记录 Use 按下时间
  - buffer window 内不立即执行，记录意向
  - window 结束时重新评估 use_gate → 有效则执行，无效则丢弃

---

## Out of Scope

- Story 003: 焦点评分和 selection（use_gate 依赖 focus_selection 但不拥有焦点逻辑）
- Story 005: `handle_use()` 的具体子类实现（本 Story 只实现分发框架）
- Story 006: `use_blocked` 和 `interaction_used` 语义事件的消费端

---

## QA Test Cases

- **AC-7**: UseLocked prevents repeat
  - Given: 玩家对目标按 E → `handle_use()` 返回 ACCEPTED → `UseLocked`
  - When: 玩家在锁未释放时连续按 E 5 次
  - Then: `handle_use()` 只被调用 1 次；后续按 E 被忽略
  - Edge cases: UseLocked 期间切换到不同目标按 E → 同样忽略

- **AC-8**: UseLock timeout
  - Given: 领域系统接受 Use 但 2.5s 内未调用 `release_use_lock()`
  - When: 超时触发
  - Then: `UseLocked → Focused`；movement 恢复；记录 `ERR_USE_LOCK_TIMEOUT` 诊断
  - Edge cases: 领域系统在超时后 0.1s 才释放 → 释放被忽略（锁已不存在）

- **AC-10**: Input buffer
  - Given: UseLocked 将在 0.05s 后释放，玩家在释放前 0.03s 按 E
  - When: buffer window（0.10s）结束
  - Then: 重新评估 use_gate → 有效 → 自动触发 Use
  - Edge cases: buffer 内焦点目标变化 → 评估新目标的 use_gate；buffer 内 input_gate 关闭 → 丢弃缓冲

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/UseGateTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 003 (focus_selection — use_gate 依赖当前焦点)；Story 005 (Interactable 基类 — UseResult 枚举 + handle_use() 签名)
- Unlocks: Story 006 (interaction_used/use_blocked 语义事件——由 use_gate 触发)
