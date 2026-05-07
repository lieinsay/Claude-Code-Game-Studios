# Story 001: Movement System

> **Epic**: Player Movement & Interaction
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-001`

**ADR Governing Implementation**: ADR-0004: InteractionHandler @abstract
**ADR Decision Summary**: Player 为 `CharacterBody2D`，使用 `move_and_slide()` 处理移动和碰撞。速度公式分两阶段：intended_velocity（输入方向 × 速度标量）→ move_and_slide() → actual_velocity。`collision_multiplier` 从 move_and_slide() 结果派生（二值：0 或 1）。移动在 `_physics_process` 中运行，帧率无关。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `CharacterBody2D.move_and_slide()` 内部处理 delta；`Input.get_vector()` 归一化斜向输入并内置死区；physics layers 通过 Project Settings 配置。

**Control Manifest Rules (Foundation layer)**:
- Required: `move_and_slide()` 作为唯一移动碰撞解决方式；`Input.get_vector()` 归一化输入
- Forbidden: 不得在 `_process()` 中执行移动（必须 `_physics_process`）
- Guardrail: `base_move_speed` 可配置；`max_move_speed` 硬上限；`movement_block_event_delay` 限流

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN `InputOpen` 且玩家按下移动键，WHEN `_physics_process` 运行，THEN 角色以 `base_move_speed × input_magnitude` 的速度沿输入方向移动；1 秒位移量误差在 ±5% 内
- [ ] **AC-2**: GIVEN 玩家释放所有移动键，WHEN 下一物理帧，THEN `movement_velocity = 0`（无惯性滑行）
- [ ] **AC-3**: GIVEN 玩家同时按下水平和垂直移动键，WHEN 计算速度，THEN `movement_velocity` 不超过 `max_move_speed`（斜向归一化）
- [ ] **AC-4**: GIVEN `input_gate_state = InputClosed`，WHEN 玩家按住移动键，THEN 角色位置不变
- [ ] **AC-5**: GIVEN `movement_state = Rooted`，WHEN 玩家按住移动键，THEN `movement_velocity = 0`
- [ ] **AC-6**: GIVEN 角色被碰撞体或场景边界阻挡，WHEN `move_and_slide()` 返回的实际速度为零而意图速度非零，THEN `movement_state = Blocked`、`collision_multiplier = 0`、角色不穿越阻挡体
- [ ] **AC-7**: GIVEN 持续撞墙，WHEN 发送 `movement_blocked` 语义事件，THEN 按 `movement_block_event_delay`（默认 0.15s）限流，1 秒内事件计数 ≤ `1/delay + 1`

---

## Implementation Notes

- Player 节点类型: `CharacterBody2D`（场景 `player.tscn`）
- 速度流程:
  1. `input_direction = Input.get_vector("move_left", "move_right", "move_up", "move_down")`
  2. `movement_velocity_scalar = clamp(base_move_speed × input_magnitude × gate_multiplier × root_multiplier, 0, max_move_speed)`
  3. `intended_velocity = input_direction × movement_velocity_scalar`
  4. `actual_velocity = move_and_slide(intended_velocity)`
  5. `collision_multiplier = 0.0 if (AV.length()==0 AND IV.length()>0) else 1.0`
  6. `movement_velocity = actual_velocity.length()`
- Physics Layers:
  | Layer | Bit | Name | Purpose |
  |-------|-----|------|---------|
  | 1 | 0 | player | 玩家角色 |
  | 2 | 1 | world_geometry | 世界几何体（墙壁、地板、场景边界） |
  | 3 | 2 | interactable | 交互目标 |
  | 4 | 3 | interaction_occlusion | path_clear 射线遮挡 |
- Player collision mask: Layer 2 (仅世界几何体)
- Movement states: `Idle` / `Moving` / `Blocked` / `Rooted`
- Transitions: 门关闭/Rooted 时速度归零；碰撞阻断时 `Moving → Blocked`
- 所有移动计算在 `_physics_process` 中执行，帧率无关（物理帧步默认 60Hz）
- `Input.get_vector()` 已内置死区并归一化斜向输入（死区默认 0.15 用于手柄，键盘为 0）

---

## Out of Scope

- Story 002: Input gate 的具体状态机和壳层信号集成
- Story 003: 交互焦点评分和候选选择
- Story 004: Use gate 和 Use dispatch
- Story 007: 场景切换时角色位置管理
- 动画（角色 Sprite/AnimationPlayer）— 属于 Presentation 层

---

## QA Test Cases

- **AC-3**: Diagonal speed normalization
  - Given: `base_move_speed=4.0`、`max_move_speed=4.0`、玩家同时按住 W+D
  - When: 测量一帧的 `movement_velocity`
  - Then: `movement_velocity ≈ 4.0`（非 ~5.66）
  - Edge cases: 三键同时（如 W+A+D 在 gamepad 上不可能但键盘上可能）→ 仍归一化

- **AC-6**: Wall collision
  - Given: 玩家在可行走区域，前方 1 unit 处有 StaticBody2D 墙壁
  - When: 玩家按住向右移动键
  - Then: 角色停在墙壁碰撞边界；`movement_state = Blocked`；位置不再向右推进
  - Edge cases: 斜向靠近墙壁 → `move_and_slide()` 沿墙滑动，实际速度可能非零（不算 Blocked）

- **AC-4**: Input closed during movement
  - Given: 玩家正在以 `base_move_speed=4.0` 向右移动
  - When: `input_gate_state` 变为 `InputClosed`
  - Then: 角色在第 1 帧内速度归零，`movement_state = Idle`
  - Edge cases: 门关闭后第 2 帧再打开 → 角色不自动恢复移动

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/movement_system_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: platform-session-shell Story 001 (Input gate signals) — 需要 input_gate_open/closed 信号定义
- Unlocks: Story 003 (Focus — 焦点候选查询依赖玩家位置)；Story 004 (Use — use_gate 依赖 distance_to_anchor)；Story 007 (Scene transition — 角色位置管理)
