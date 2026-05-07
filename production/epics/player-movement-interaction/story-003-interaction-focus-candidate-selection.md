# Story 003: Interaction Focus & Candidate Selection

> **Epic**: Player Movement & Interaction
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-003`

**ADR Governing Implementation**: ADR-0004: InteractionHandler @abstract
**ADR Decision Summary**: 焦点评分公式 `focus_score = 0.45×pointer_score + 0.25×proximity_score + 0.15×priority_score + 0.15×stickiness_score`，两级滞回机制（sorting stage stickiness SS 0.15 + selection stage bonus SB 0.08 = 0.23 总粘性），tie-breaking 按 priority > distance > stable ID。同一时刻只有一个世界焦点。键盘 Tab 循环：按 focus_score 降序循环候选目标。`path_clear` 使用 `PhysicsRayQueryParameters2D` 单次射线检测。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `Area2D.overlapping_areas` 用于候选查询；`mouse_entered`/`mouse_exited` 信号设置 pointer_score；`PhysicsRayQueryParameters2D` 射线检测排除玩家自身 RID。

**Control Manifest Rules (Foundation layer)**:
- Required: 单一世界焦点；滞回机制（`acquire_margin < retain_margin` 强制约束）；焦点权重总和 = 1.0
- Forbidden: 不得每帧无界全场景扫描——候选查询限制 ≤ `max_focus_candidates_per_query`（8）
- Guardrail: `min_focus_score=0.35`；`focus_stickiness_bonus=0.08`

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 玩家进入交互目标的 `reach_limit` 范围且目标可用、未被遮挡，WHEN 候选评分完成，THEN 得分最高且超过 `min_focus_score` 的候选成为世界焦点
- [ ] **AC-2**: GIVEN 存在多个候选目标，WHEN 焦点评选完成，THEN `world_focus_id` 最多为一个有效 ID（单一焦点）
- [ ] **AC-3**: GIVEN 当前焦点目标仍在 `retain_margin` 范围内，WHEN 有稍近的新目标进入 `acquire_margin`，THEN 当前焦点保持（滞回——不被稍近的新候选抢走）
- [ ] **AC-4**: GIVEN 当前焦点目标超出 `retain_margin` 范围，WHEN 候选刷新，THEN 焦点清除或切换到下一个有效候选
- [ ] **AC-5**: GIVEN 当前焦点目标的 `target_enabled` 变为 false，WHEN 候选刷新，THEN 焦点立即清除并报告 `target_disabled`
- [ ] **AC-6**: GIVEN 当前焦点目标被几何体遮挡（`path_clear=false`），WHEN 候选刷新，THEN 焦点清除
- [ ] **AC-7**: GIVEN 鼠标明确指向一个可交互目标，WHEN 计算 `focus_score`，THEN 该目标 `pointer_score=1`，在得分中占最高权重（0.45）
- [ ] **AC-8**: GIVEN 鼠标点击空世界空间且无目标可交互，WHEN 处理点击，THEN 不发出 `interaction_used`
- [ ] **AC-9**: GIVEN 两个候选目标 `focus_score` 相等，WHEN 执行 tie-breaking，THEN 按作者优先级 > 距离 > ID 字典序决定唯一焦点
- [ ] **AC-10**: GIVEN `distance_to_anchor == reach_limit`，WHEN 判定可达性，THEN 目标视为可达，允许焦点
- [ ] **AC-11**: GIVEN 纯键盘玩家按 Tab 键，WHEN 候选池非空，THEN 焦点按 `focus_score` 降序在候选目标间循环（最后到最高）
- [ ] **AC-12**: GIVEN Tab 循环选中第 N 个候选后鼠标指向另一目标，WHEN 焦点刷新，THEN 循环位置重置为鼠标指向目标

---

## Implementation Notes

- Focus 状态机（5 态）:
  - `NoFocus`: 无有效候选
  - `Candidate`: 有候选但尚未稳定
  - `Focused`: 当前目标已稳定为唯一焦点
  - `UsePending`: 玩家按下 Use，正在验证
  - `UseLocked`: Use 已发出，等待领域系统完成
- `focus_score` 公式:
  ```
  proximity_score = 1 - clamp(distance_to_anchor / reach_limit, 0, 1)  (reach_limit=0 → 0)
  focus_score = clamp(0.45×pointer_score + 0.25×proximity_score + 0.15×priority_score + 0.15×stickiness_score, 0, 1)
  ```
- `focus_selection` 公式:
  ```
  candidate_selection_score_i = focus_score_i + current_focus_bonus_i
  current_focus_bonus_i = focus_stickiness_bonus(0.08) if i == current_focus_target AND valid, else 0
  focus_selection = argmax(candidate_selection_score_i, tie_break = priority > distance > stable_id)
  ```
- `interaction_reachability` 公式:
  ```
  hysteresis_margin = retain_margin(0.20) if is_current_focus_target else acquire_margin(0.05)
  reach_limit = anchor_radius + player_interaction_radius(0.25) + hysteresis_margin
  interaction_reachability = input_gate_open AND target_available AND target_enabled AND path_clear AND distance <= reach_limit
  ```
- `path_clear` 检测: `PhysicsRayQueryParameters2D` 从玩家质心到 `target.get_anchor_position()`，mask Layer 4，exclude 玩家 RID
- `pointer_score`: 通过 `Area2D.mouse_entered`(→1)/`mouse_exited`(→0) 设置；`mouse_filter = MOUSE_FILTER_PASS`
- Keyboard Tab 循环: 在 `focus_score` 降序列表中按序移动；wrap-around 从末尾到最高分
- 强制约束: `acquire_margin(0.05) < retain_margin(0.20)` — 违反时 fallback `acquire_margin = retain_margin × 0.5`
- 焦点刷新在 `_physics_process` 中执行；≤8 候选排序，O(N) 复杂度

---

## Out of Scope

- Story 001: 玩家移动（焦点依赖玩家位置但不拥有移动逻辑）
- Story 002: Input gate 状态机
- Story 004: Use gate 和 Use dispatch
- Story 005: Interactable 基类定义和注册/注销机制
- Story 006: `interaction_focus_changed` 语义事件的具体发送（本 Story 只判定焦点切换，事件发送由 006 统一管理）

---

## QA Test Cases

- **AC-3**: Hysteresis prevents focus flicker
  - Given: 当前焦点 A（distance=0.80, retain_margin=0.20），新候选 B（distance=0.75, acquire_margin=0.05），anchor_radius=0.45, player_interaction_radius=0.25
  - When: 每帧计算焦点
  - Then: A 的 reach_limit=0.90（可保留），B 的 reach_limit=0.75（虽更近但未突破 A 的保留边距），A 保持焦点
  - Edge cases: B 移动到 distance=0.69 → reach_limit=0.75，B 可达，但 A 仍在保留边距内 → A 保持

- **AC-9**: Tie-breaking
  - Given: 两个候选 A(priority=0.6, distance=0.5) 和 B(priority=0.6, distance=0.7)，其他分项相等
  - When: argmax tie-break
  - Then: A 胜出（距离更近）；若距离也相等 → 按 stable_id 字典序
  - Edge cases: priority 不同 → 先按 priority 打破平局

- **AC-11**: Keyboard Tab cycling
  - Given: 候选池 [C1(0.8), C2(0.6), C3(0.4)]，`world_focus_id=C1`
  - When: 按 Tab → Tab → Tab
  - Then: C1→C2→C3→C1（wrap-around）；按 Tab 中途鼠标悬停另一目标 → 循环位置重置

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/movement/focus_selection_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (player position → distance_to_anchor)；Story 002 (input_gate_open → 焦点状态机前置条件)；Story 005 (Interactable 注册 → 候选池数据来源)
- Unlocks: Story 004 (use_gate — 依赖 focus_selection ≠ NoFocus)；Story 006 (interaction_focus_changed 事件)
