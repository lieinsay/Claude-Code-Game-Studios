# Story 006: Semantic Events & UI Data Contract

> **Epic**: Player Movement & Interaction
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-006`

**ADR Governing Implementation**: ADR-0002: Signal Communication Protocol; ADR-0004: InteractionHandler @abstract
**ADR Decision Summary**: 5 个语义事件通过 typed signal 发出（fire-and-forget），供 FeedbackManager 和 UIManager 消费。同一帧内多个事件同时触发时只发优先级最高的事件（interaction_used > use_blocked > interaction_focus_changed > movement_blocked）。UI 数据通过 `query_focus_state()` 方法拉取（读查询=直接调用），不动过 signal 推送。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Signal typed params: `signal interaction_focus_changed(focused_id: StringName)` 等；`Dictionary` 只在 `query_focus_state()` 返回时使用——不通过 signal 传递。

**Control Manifest Rules (Foundation layer)**:
- Required: signal 使用 typed params（禁止 Dictionary payload）；读查询使用直接方法调用
- Forbidden: `dictionary_signal_payload`；`untyped_signal_param`；signal cascade depth ≤2
- Guardrail: 并发音源 ≤3；音频在用户手势激活前静默

---

## Acceptance Criteria

### Semantic Events

- [ ] **AC-1**: GIVEN 焦点在目标间切换，WHEN 新焦点确立，THEN 发出 `interaction_focus_changed(focused_id: StringName)` signal，payload 含 `previous_focus_id`、`new_focus_id`、`transition_reason`
- [ ] **AC-2**: GIVEN `interaction_used` 成功发送给领域系统，WHEN 分发完成，THEN 发出 `interaction_used(target_id: StringName, interaction_type: StringName)` signal
- [ ] **AC-3**: GIVEN `use_gate = Blocked(reason)`，WHEN 阻断判定完成，THEN 发出 `use_blocked(target_id: StringName, block_reason: StringName)` signal
- [ ] **AC-4**: GIVEN 持续移动被碰撞阻挡，WHEN 限流条件满足，THEN 发出 `movement_blocked` 语义事件（含 `block_direction`、`block_type`）
- [ ] **AC-5**: GIVEN `input_gate_state` 变化，WHEN 新状态生效，THEN 发出 `input_gate_changed` 语义事件（含 `previous_gate`、`new_gate`）
- [ ] **AC-6**: GIVEN 同一帧内同时触发 `use_blocked` 和 `interaction_focus_changed`，WHEN 事件优先级规则应用，THEN 只发出 `use_blocked`（优先级更高）

### UI Data Contract

- [ ] **AC-7**: GIVEN `world_focus_id` 变化，WHEN UI 调用 `query_focus_state()`，THEN 返回 Dictionary 含正确的 `world_focus_id`、`display_hint`、`focus_state`
- [ ] **AC-8**: GIVEN `use_blocked` 发生，WHEN UI 调用 `query_focus_state()`，THEN `last_block_reason` 和 `last_block_target_id` 反映最近阻断
- [ ] **AC-9**: GIVEN `input_gate_state` 变化，WHEN UI 调用 `query_focus_state()`，THEN `is_input_open` 反映当前门状态
- [ ] **AC-10**: GIVEN UI 模态打开，WHEN UI 调用 `query_focus_state()`，THEN `world_focus_id` 保持模态打开前的值（冻结），但 `use_gate` 对任何 Use 请求返回 `Blocked(ui_modal_blocked)`

---

## Implementation Notes

### Semantic Event Contract

每个语义事件包含: `event_type`、`timestamp`、`source_system="player_movement_interaction"`、`payload` (dict——仅内部使用，不通过 signal 传递)

**Signal 定义**（typed）:
```gdscript
signal interaction_focus_changed(focused_id: StringName)
signal interaction_used(target_id: StringName, interaction_type: StringName)
signal use_blocked(target_id: StringName, block_reason: StringName)
signal movement_blocked(block_direction_x: float, block_direction_y: float, block_type: StringName)
signal input_gate_changed(previous_gate: StringName, new_gate: StringName)
```

**事件优先级**: `interaction_used(1) > use_blocked(2) > interaction_focus_changed(3) > movement_blocked(4)`。同帧冲突时只 emit 最高优先级。

### UI Data Contract

`query_focus_state() -> Dictionary` 返回:
```
{
  "world_focus_id": StringName,       # 当前焦点 ID，NoFocus 时为空
  "display_hint": String,             # ≤12 字符显示名
  "focus_state": StringName,          # "NoFocus"/"Candidate"/"Focused"/"UsePending"/"UseLocked"
  "last_block_reason": StringName,    # 最近阻断原因，无阻断时为空
  "last_block_target_id": StringName, # 最近被阻断的目标 ID
  "input_gate_state": StringName,     # "InputOpen"/"InputClosed"/"InputReacquire"
  "movement_state": StringName,       # "Idle"/"Moving"/"Blocked"/"Rooted"
  "candidate_count": int,             # 当前候选池目标数
  "is_input_open": bool,              # 输入门快捷查询
}
```

### Cross-cutting rules:
- 表现优先级: `interaction_used > use_blocked > interaction_focus_changed > movement_blocked`（同帧冲突时）
- 音频预算: 并发音源 ≤3，超出时丢弃最早播放中的非循环音
- Web 约束: 音频只在壳层 AudioContext 激活（用户手势）后播放；激活前的语义事件保留但不播放历史音频
- 色盲安全: 所有焦点/阻断视觉反馈同时含非色相维度（轮廓样式、亮度、形状变化）

---

## Out of Scope

- Story 001-004: 各自产生事件的条件逻辑（本 Story 只负责事件的标准化发射和 UI 数据聚合）
- FeedbackManager 的具体视觉/音频实现（属于 Presentation 层）
- UIManager 的具体 UI 渲染（属于 UI/HUD Epic）
- Block Reason Toast / Focus Indicator 的具体 UI 控件（属于 Story 007 或 UI Epic）

---

## QA Test Cases

- **AC-6**: Event priority on same frame
  - Given: 按 Use 键的同帧焦点因目标被禁用而清空
  - When: 帧内事件评估
  - Then: 发出 `use_blocked(target_disabled)`，不发出 `interaction_focus_changed`
  - Edge cases: `interaction_used` 和 `interaction_focus_changed` 同帧 → 只发 `interaction_used`

- **AC-10**: UI modal freezes focus data
  - Given: `world_focus_id="hub.helm"`、`focus_state="Focused"`
  - When: UI 模态打开（`ui_modal_blocked=true`）
  - Then: `query_focus_state()` 仍返回 `world_focus_id="hub.helm"`；按 E → `use_gate=Blocked(ui_modal_blocked)`
  - Edge cases: 模态期间目标被外部禁用 → `world_focus_id` 仍需冻结到模态关闭后再清除

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/movement/semantic_events_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (movement_blocked)；Story 002 (input_gate_changed)；Story 003 (interaction_focus_changed)；Story 004 (interaction_used, use_blocked)
- Unlocks: Feedback 系统（消费语义事件）；UIManager（消费 `query_focus_state()` 和信号）
