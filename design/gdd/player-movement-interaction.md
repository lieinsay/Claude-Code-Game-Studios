# 玩家移动与交互

> **Status**: In Design
> **Author**: User + Codex
> **Last Updated**: 2026-04-28
> **Implements Pillar**: 飞艇是家，不只是载具; 规划先于冒险; 未知带来温和压力
> **System Index**: `design/gdd/systems-index.md`

## Overview

`玩家移动与交互` 是《云海织航》的基础玩家动作层，负责把壳层开放后的键鼠输入转化为清楚、可预期、可被阻断的角色移动、可达性判断、交互焦点和 `Use` 入口。玩家通过它在横版飞艇、起始空港、集市摊位和探索点中行走、靠近、确认目标并发起交互；但具体后果，例如购买、采集、修复、安装模块、触发探索结果或打开领域 UI，都由对应系统拥有。本系统的设计目标不是制造复杂操作技巧，而是让玩家感觉自己真的站在飞艇和世界里：能顺畅移动，能读懂什么可接近、什么可使用、当前会操作哪个对象，也能在后台恢复、壳层 overlay、距离不足或状态锁定时安全地不误触。它支撑“飞艇是家，不只是载具”的身体感，也保护“规划先于冒险”的可读操作节奏。

## Player Fantasy

玩家感受到的不是一套“移动系统”，而是自己真的能在飞艇、空港和未知地点中落脚、靠近、确认并伸手使用。走动不是赶路，而是在把自己的家一次次接回世界：从熟悉的舱室走到甲板，从停靠点进入集市，从陌生探索点找到可达路径，每一步都应该让玩家读懂“这里能通、这里能靠近、这里可以搭手”。

在飞艇内部，移动与交互应该像日常照料：玩家凭身体记忆穿过舱室，靠近工作台、货架、舱门或伙伴驻点时，交互焦点像手自然搭上去，而不是 UI 抢走注意力。回到船上时，节奏应立刻变得熟悉、安稳，强化“飞艇是家，不只是载具”。

在空港、集市和探索点中，本系统承担温和压力的第一层表达。玩家面对未知空间时，可以先观察路径、接近对象、看清焦点，再决定是否使用；距离不足、状态锁定、壳层恢复或下游系统不可用时，系统应可靠地阻断误触。理想体验是：玩家相信自己的每次移动和每次 `Use` 都有明确对象、明确边界和可理解反馈，不会因为焦点混乱或误输入破坏规划节奏。

## Detailed Design

### Core Rules

1. `玩家移动与交互` 只在 `平台与会话壳` 明确开放玩法输入时工作。若壳层处于加载、恢复、后台挂起、错误、overlay 或第一下恢复输入消费状态，本系统不得接收移动或 `Use`。
2. 本系统负责三件事：解释移动输入、判断玩家能否抵达或触达目标、把 `Use` 作为标准化请求分发给当前焦点目标。
3. 移动只改变玩家的位置、速度、朝向和移动状态；移动本身不得触发购买、采集、修复、安装模块、打开商店或推进世界状态。
4. `Use` 是意图请求，不是结果。领域系统可以接受、拒绝、锁定、耗时处理或返回阻断原因；本系统不得自行执行领域后果。
5. 每个可交互对象必须提供稳定 ID、交互锚点、交互半径、可用状态、优先级和阻断原因。显示名、节点路径或临时引用不得作为交互身份来源。
6. 交互必须通过可达性检查：玩家在范围内、目标未被遮挡或阻断、目标当前可用、玩法输入门打开，才允许发出 `use_requested`。
7. 同一时刻只有一个世界交互焦点。UI `Control` 焦点和世界交互焦点必须分离；壳层或 HUD overlay 可见时，世界交互焦点冻结或清空。
8. 焦点选择优先级为：明确鼠标指向或点击目标、最近的可达目标、上一个仍有效焦点。多个目标同时可达时，用优先级、距离和稳定滞回决定唯一焦点。
9. 焦点切换必须稳定，不得因鼠标轻微抖动、玩家站在两个锚点边缘或候选短暂进出范围而快速闪烁。
10. 失败必须可解释。不可交互时，系统应输出明确阻断原因，例如 `input_closed`、`too_far`、`blocked`、`target_disabled`、`target_busy`、`ui_modal_blocked`。
11. MVP 不做自动寻路、跨房间交互、靠近后自动执行、拖拽式复杂操作、连续长按式操作、gamepad、touch、指针锁依赖或战斗专用输入链。
12. 本系统不能读取或改写货币、库存、资源数量、修复状态、市场库存、模块安装结果、探索奖励、剧情进度或存档内容。

### States and Transitions

本系统使用三个正交状态组，避免把壳层门禁、玩家移动和交互焦点揉成一个大状态机。

**Input Gate State**

| State | Meaning | Allowed Input |
|---|---|---|
| `InputClosed` | 壳层未放行、页面后台、overlay 可见或会话未激活 | None |
| `InputReacquire` | 恢复后等待第一下可信输入被壳层消费 | None for gameplay |
| `InputOpen` | 会话激活且无壳层阻断 | Movement, focus update, Use |

Transitions:

- `InputClosed -> InputReacquire`: 壳层从恢复流程返回并要求重新激活。
- `InputReacquire -> InputOpen`: 壳层确认第一下输入已消费，并开放玩法输入。
- `InputClosed -> InputOpen`: 正常进入 `SessionActive` 且无需恢复消费。
- `InputOpen -> InputClosed`: 页面隐藏、失焦、暂停、壳层 overlay、错误态或切场景锁定。

**Movement State**

| State | Meaning |
|---|---|
| `Idle` | 无移动意图，玩家可接收焦点和 Use |
| `Moving` | 有有效移动输入并成功位移 |
| `Blocked` | 有移动意图但被碰撞、边界或临时阻挡拦下 |
| `Rooted` | 交互、领域动作或场景规则要求短暂站定 |

Transitions:

- `Idle / Blocked -> Moving`: 有有效移动输入且输入门打开。
- `Moving -> Idle`: 移动输入结束。
- `Moving -> Blocked`: 移动输入仍存在，但碰撞或边界阻止位移。
- `Any -> Rooted`: 本系统或领域系统收到需要站定的锁定请求。
- `Rooted -> Idle`: 锁定释放，且输入门仍打开。

**Interaction Focus State**

| State | Meaning |
|---|---|
| `NoFocus` | 没有有效候选目标 |
| `Candidate` | 有候选目标，但尚未稳定 |
| `Focused` | 当前目标已稳定为唯一焦点 |
| `UsePending` | 玩家按下 `Use`，正在验证并发出请求 |
| `UseLocked` | `Use` 请求已发出，等待领域系统完成或释放 |

Transitions:

- `NoFocus -> Candidate`: 鼠标指向、玩家进入范围或朝向附近交互目标。
- `Candidate -> Focused`: 候选通过优先级、距离、可达性和滞回稳定检查。
- `Focused -> NoFocus`: 目标离开范围、被遮挡、禁用、销毁或输入门关闭。
- `Focused -> UsePending`: 玩家按下 `Use` 且输入门打开。
- `UsePending -> UseLocked`: `use_requested` 成功发给领域系统。
- `UsePending -> Focused`: 请求被本系统可达性检查拒绝，并输出阻断原因。
- `UseLocked -> Focused / NoFocus`: 领域系统返回完成、拒绝、取消、超时或释放锁定。

### Interactions with Other Systems

| System | This System Receives | This System Sends | Boundary |
|---|---|---|---|
| `平台与会话壳` | `input_gate_open` / `input_gate_reacquire` / `input_gate_closed`, overlay and resume gate state | none required | 壳层决定玩法输入是否可进来；本系统不判断浏览器生命周期 |
| `飞艇家园 Hub` | walkable areas, room bounds, interaction anchors, station availability | `use_requested`, focus events, movement state | Hub 拥有舱室与站点后果；本系统只负责抵达和使用入口 |
| `探索 / 搜撤场景` | walkable areas, extraction anchors, loot/search anchors, threat blockers | `use_requested`, blocked reasons, movement state | 探索系统拥有搜撤、奖励、撤离和危险后果 |
| `空港 / 村镇状态与集市交易` | stall anchors, NPC / stall availability, market blockers | `use_requested` for stall or NPC focus | 市集系统拥有购买、货品、价格和库存变化 |
| `世界修复与解锁` | repair node anchors and repair availability | `use_requested` for repair nodes | 修复系统拥有材料消耗、解锁和世界状态变化 |
| `UI / HUD / 航图界面` | modal / overlay blocking state, optional tooltip presentation policy | focus target, blocked reason, prompt hint, movement/focus state | UI 只显示焦点和原因，不判定可达性 |
| `反馈、特效与音频语义` | none required for MVP | semantic events such as focus changed, use blocked, use requested, movement blocked | 反馈系统表现语义，不拥有规则 |
| `内容数据与状态注册表` | stable interaction target IDs and content definitions through domain systems | none direct in MVP | 本系统不直接解析内容经济或修复规则 |
| `本地存档与世界状态持久化` | none direct in MVP | none direct in MVP | 移动瞬时状态是否保存由具体场景/存档策略决定，不由本系统直接写档 |

## Formulas

The `movement_velocity` formula is defined as:

`movement_velocity = clamp(base_move_speed * input_magnitude * gate_multiplier * root_multiplier * collision_multiplier, 0, max_move_speed)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `base_move_speed` | `B` | float | `> 0` | 玩家基础移动速度。 |
| `input_magnitude` | `I` | float | `0-1` | 归一化后的移动输入强度；键盘满输入为 `1`，无输入为 `0`。 |
| `gate_multiplier` | `G` | int | `0-1` | 输入门倍率；`InputOpen = 1`，否则为 `0`。 |
| `root_multiplier` | `R` | int | `0-1` | 玩家被 `Rooted` 时为 `0`，否则为 `1`。 |
| `collision_multiplier` | `C` | int | `0-1` | 当前位移可走为 `1`，被完全阻断为 `0`。 |
| `max_move_speed` | `M` | float | `> 0` | 速度上限。 |
| `movement_velocity` | `V` | float | `0-M` | 本帧最终移动速度。 |

**Output Range:** `0` to `max_move_speed`。输入门关闭、Rooted 或完全碰撞阻断时输出必须为 `0`。
**Example:** `base_move_speed=4.2`, `input_magnitude=1`, `gate_multiplier=1`, `root_multiplier=1`, `collision_multiplier=1`, `max_move_speed=4.2` 时，`movement_velocity=4.2`。若 `root_multiplier=0`，则 `movement_velocity=0`。

The `interaction_reachability` formula is defined as:

`hysteresis_margin = retain_margin if is_current_focus_target else acquire_margin`

`reach_limit = anchor_radius + player_interaction_radius + hysteresis_margin`

`interaction_reachability = input_gate_open AND target_available AND target_enabled AND path_clear AND distance_to_anchor <= reach_limit`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `input_gate_open` | `G` | bool | true/false | 玩法输入门是否开放。 |
| `target_available` | `A` | bool | true/false | 目标是否仍存在且可被查询。 |
| `target_enabled` | `E` | bool | true/false | 目标是否处于可交互状态。 |
| `path_clear` | `P` | bool | true/false | 简单无遮挡检查结果。 |
| `distance_to_anchor` | `D` | float | `>= 0` | 玩家到目标交互锚点的距离。 |
| `anchor_radius` | `AR` | float | `>= 0` | 目标交互锚点半径。 |
| `player_interaction_radius` | `PR` | float | `>= 0` | 玩家交互触达半径。 |
| `is_current_focus_target` | `F` | bool | true/false | 该目标是否为当前世界焦点。 |
| `acquire_margin` | `AM` | float | `>= 0` | 获取新焦点时的较小滞回边距。 |
| `retain_margin` | `RM` | float | `>= 0` | 保持当前焦点时的较大滞回边距。 |
| `reach_limit` | `L` | float | `>= 0` | 最终可达距离阈值。 |

**Output Range:** true/false。当前焦点使用 `retain_margin`，新候选使用 `acquire_margin`，用来减少边缘抖动。
**Example:** `distance_to_anchor=0.84`, `anchor_radius=0.45`, `player_interaction_radius=0.25`，当前焦点 `retain_margin=0.20` 时，`reach_limit=0.90`，可达；若不是当前焦点且 `acquire_margin=0.05`，`reach_limit=0.75`，不可达。

The `focus_score` formula is defined as:

`proximity_score = 1 - clamp(distance_to_anchor / reach_limit, 0, 1)`

`focus_score = clamp(0.45 * pointer_score + 0.25 * proximity_score + 0.15 * priority_score + 0.15 * stickiness_score, 0, 1)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `pointer_score` | `PS` | float | `0-1` | 鼠标明确指向该目标为 `1`，否则为 `0`。 |
| `distance_to_anchor` | `D` | float | `>= 0` | 玩家到目标交互锚点的距离。 |
| `reach_limit` | `L` | float | `> 0` | 该目标的可达距离阈值。 |
| `proximity_score` | `NS` | float | `0-1` | 距离越近分越高。 |
| `priority_score` | `RS` | float | `0-1` | 作者配置的目标优先级归一化结果。 |
| `stickiness_score` | `SS` | float | `0-1` | 当前有效焦点为 `1`，否则为 `0`。 |
| `focus_score` | `FS` | float | `0-1` | 候选目标的最终焦点分数。 |

**Output Range:** `0` to `1`。分数只负责排序，不直接执行交互。权重表达 MVP 优先级：明确指向 > 近距离可达 > 作者优先级 > 焦点黏性。
**Example:** 鼠标指向目标且 `proximity_score=0.38`, `priority_score=0.60`, `stickiness_score=0` 时，`focus_score=0.45 + 0.095 + 0.09 = 0.635`。

The `focus_selection` formula is defined as:

`candidate_selection_score_i = focus_score_i + current_focus_bonus_i`

`current_focus_bonus_i = focus_stickiness_bonus if i = current_focus_target_id AND current_focus_valid else 0`

`focus_selection = NoFocus if candidate_pool_empty OR max(candidate_selection_score_i) < min_focus_score else argmax(candidate_selection_score_i)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `candidate_pool_empty` | `E` | bool | true/false | 当前是否没有任何候选目标。 |
| `focus_score_i` | `FS_i` | float | `0-1` | 第 `i` 个候选目标的焦点分数。 |
| `current_focus_bonus_i` | `CB_i` | float | `0-1` | 当前焦点目标的滞回加成。 |
| `current_focus_target_id` | `ID` | stable id / null | id/null | 当前世界焦点目标 ID。 |
| `current_focus_valid` | `V` | bool | true/false | 当前焦点是否仍然可用。 |
| `focus_stickiness_bonus` | `SB` | float | `0-1` | 当前焦点保留加成。 |
| `min_focus_score` | `MIN` | float | `0-1` | 焦点启用最低门槛。 |
| `focus_selection` | `SEL` | stable id / `NoFocus` | id/NoFocus | 最终选中的世界焦点。 |

**Output Range:** `NoFocus` 或一个稳定目标 ID。任何时候只允许一个世界焦点。
**Example:** 当前焦点 A 的 `focus_score=0.57`，`focus_stickiness_bonus=0.08`，最终 `0.65`；新候选 B 的 `focus_score=0.62`。A 仍有效时保留 A；A 失效时 B 胜出。

The `use_gate` formula is defined as:

`distance_ok = distance_to_anchor <= reach_limit`

`use_gate = Allowed if input_gate_open AND NOT ui_modal_blocked AND focus_selection != NoFocus AND target_enabled AND path_clear AND distance_ok AND NOT target_busy else Blocked(block_reason)`

`block_reason = input_closed if NOT input_gate_open else ui_modal_blocked if ui_modal_blocked else no_focus if focus_selection = NoFocus else target_disabled if NOT target_enabled else blocked if NOT path_clear else too_far if NOT distance_ok else target_busy if target_busy else blocked`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `input_gate_open` | `G` | bool | true/false | 玩法输入门是否开放。 |
| `ui_modal_blocked` | `U` | bool | true/false | 是否被 UI / HUD 模态层阻断。 |
| `focus_selection` | `SEL` | stable id / `NoFocus` | id/NoFocus | 当前世界焦点。 |
| `target_enabled` | `E` | bool | true/false | 当前焦点目标是否可用。 |
| `path_clear` | `P` | bool | true/false | 简单无遮挡结果。 |
| `distance_to_anchor` | `D` | float | `>= 0` | 玩家到当前焦点目标交互锚点的距离。 |
| `reach_limit` | `L` | float | `>= 0` | 当前目标的可达阈值。 |
| `distance_ok` | `DO` | bool | true/false | 距离是否在阈值内。 |
| `target_busy` | `B` | bool | true/false | 目标是否处于忙碌或占用状态。 |
| `block_reason` | `BR` | enum | `input_closed` / `ui_modal_blocked` / `no_focus` / `target_disabled` / `blocked` / `too_far` / `target_busy` | 阻断原因。 |
| `use_gate` | `UG` | enum | `Allowed` / `Blocked` | `Use` 是否放行。 |

**Output Range:** `Allowed` 或 `Blocked(reason)`。此门只决定是否分发 `use_requested`，不决定领域后果。
**Example:** `input_gate_open=true`, `focus_selection=A`, `target_enabled=true`, `path_clear=true`, `distance_to_anchor=1.30`, `reach_limit=1.05`, `target_busy=false` 时，`use_gate=Blocked(too_far)`。若 `input_gate_open=false`，则优先返回 `Blocked(input_closed)`。

## Edge Cases

- **If the shell is loading, background suspended, in an error state, showing an overlay, or reporting `InputReacquire`**: treat this system as `InputClosed`; discard movement and `Use` inputs immediately, with no queueing and no replay.
- **If the first keyboard or mouse input arrives after browser focus / visibility recovery**: consume it only for shell reactivation; do not produce movement, focus confirmation, or `Use` until a later valid gameplay input edge.
- **If the player is holding movement or `Use` during `InputReacquire`**: do not backfill any action when input opens; the player must release and press again.
- **If a `Control` UI element has focus and the player presses `Use`**: route the input only to UI; do not emit `use_requested`, and keep or freeze the world focus.
- **If a HUD or shell modal is visible even though `Control` focus has not changed**: freeze world focus and block all `Use` attempts with `ui_modal_blocked`.
- **If the player clicks empty world space**: keep the current world focus if it remains valid; otherwise stay in `NoFocus`; do not emit `use_requested`.
- **If small mouse jitter causes multiple targets to enter the candidate pool**: retain the current valid focus first; otherwise choose only the highest `focus_score` target.
- **If two candidates have equal `focus_score` or tie at the focus threshold**: keep the current focus if valid; otherwise break the tie by higher author priority, then shorter distance, then stable ID order.
- **If multiple interactable targets are valid and the mouse does not clearly point at one**: select exactly one world focus by `focus_score`; never highlight multiple world targets as active focus.
- **If `distance_to_anchor == reach_limit`**: treat the target as reachable and allow focus or `Use` if all other gates pass.
- **If a target is just beyond `reach_limit`**: block new `Use` as `too_far`; retain current focus only if the target remains inside the larger `retain_margin`.
- **If the current focus flickers around the reach boundary**: keep focus while it remains inside `retain_margin`; clear or switch only after it exits that margin.
- **If a target is blocked by geometry, building pieces, scene boundaries, or another blocking entity**: remove it from interactable eligibility; return `Blocked(blocked)` for `use_gate`; if it was focused, clear focus or choose the next valid candidate.
- **If the current focus is blocked, disabled, destroyed, or moved out of range on the same frame as `Use`**: fail `Use`, emit no domain consequence, and report the latest valid block reason.
- **If `target_busy = true` while the target remains visible and reachable**: keep focus if appropriate, but block `Use` with `target_busy` and do not enter `UseLocked`.
- **If `use_requested` has been emitted and the domain system accepts a lock**: enter `UseLocked`; optionally place movement in `Rooted`; reject repeated `Use` until the domain system releases the lock.
- **If the domain system times out or fails to release a `UseLocked` interaction**: cancel the pending `Use`, release movement lock, and re-evaluate focus; do not automatically resubmit `Use`.
- **If the player presses movement or repeatedly presses `Use` during `UseLocked`**: ignore those inputs; do not queue, repeat, or execute them after unlock.
- **If a scene transition begins while the player is in `Focused`, `UsePending`, or `UseLocked`**: close input immediately, clear world focus, and cancel or let the old domain system safely finish the current `Use`; the new scene must not inherit old focus or replay old `Use`.
- **If an interactable node is rebuilt but keeps the same stable ID**: treat it as the same target for focus and `Use` routing, subject to fresh reachability and availability checks.
- **If an interactable is replaced, hot-reloaded, or moved to a new scene with a different stable ID**: invalidate the old focus immediately and do not map it onto the new object.
- **If a target changes `target_enabled` from true to false while focused**: clear focus and cancel any pending `Use`; report `target_disabled`.
- **If the browser restores while the mouse is already over a target**: allow focus to refresh only on the next valid gameplay focus update; never auto-execute `Use` on the restore frame.

## Dependencies

硬依赖：

- `平台与会话壳`：提供 `input_gate_open` / `input_gate_reacquire` / `input_gate_closed`，并负责加载、恢复、后台挂起、overlay 和第一下恢复输入消费。本系统不得自行判断浏览器生命周期或绕过壳层门禁。
- 场景可行走区域与碰撞边界：每个使用本系统的场景必须提供可行走区域、阻挡体、边界和临时锁定区域。没有这些数据时，本系统只能关闭移动或进入安全阻断态。
- 交互目标契约：每个可交互对象必须提供稳定 ID、交互锚点、交互半径、可用状态、优先级、忙碌状态和阻断原因。本系统只消费这些数据，不推断领域后果。
- 输入映射：MVP 依赖键盘移动和单一 `Use` 输入动作。gamepad、touch、指针锁、拖拽和长按不是 launch 范围。

软依赖：

- `UI / HUD / 航图界面`：显示当前焦点、可用提示、阻断原因和交互反馈；若 UI 尚未完整，本系统仍可通过调试提示或最小提示运行。
- `反馈、特效与音频语义`：表现 `focus_changed`、`use_requested`、`use_blocked`、`movement_blocked` 等语义事件；MVP 可先用轻量视觉提示替代完整音画反馈。
- `内容数据与状态注册表`：长期应提供稳定目标 ID 和交互类型定义；MVP 可以由场景作者临时配置，但不得使用显示名或节点路径作为最终交互身份。
- `本地存档与世界状态持久化`：本系统不直接写档；若某些场景需要保存玩家位置或交互状态，应由场景或领域系统把可保存状态交给存档系统。

下游系统契约：

| System | Depends on This System For | Must Provide Back |
|---|---|---|
| `飞艇家园 Hub` | 舱室内移动、站点焦点、工作台/货架/舱门/伙伴驻点 `Use` 入口 | 舱室边界、站点锚点、站点可用状态、站点领域处理结果 |
| `探索 / 搜撤场景` | 探索点移动、搜索点/撤离点/风险点焦点和 `Use` 入口 | 探索区域、可搜目标、撤离锚点、威胁阻断、领域处理结果 |
| `空港 / 村镇状态与集市交易` | 摊位、NPC、公告点和市场入口的焦点与 `Use` 请求 | 摊位锚点、NPC 可用状态、市场忙碌/关闭原因、交易 UI 入口 |
| `世界修复与解锁` | 修复节点的触达、焦点和 `Use` 请求 | 修复节点锚点、材料/情报可用性摘要、修复忙碌/锁定原因 |
| `UI / HUD / 航图界面` | 当前焦点、提示文案、阻断原因和输入阻断状态 | 模态 UI 是否阻断世界输入、提示展示策略 |
| `反馈、特效与音频语义` | 移动阻断、焦点变化、Use 请求、Use 阻断等语义事件 | 只返回表现完成或忽略；不得改变规则结果 |

边界声明：

- 本系统不拥有货币、库存、资源、修复、市场、模块安装、探索奖励、战斗、剧情或存档结果。
- 本系统不打开领域 UI；它只发送 `use_requested`，由领域系统决定是否打开 UI。
- 本系统不做自动寻路、不跨场景保持焦点、不跨房间远程交互。
- 本系统不直接订阅浏览器 `visibilitychange`、`pagehide`、`focus` 等事件；这些都由 `平台与会话壳` 归一化后传入。
- 下游系统可以拒绝 `Use`，但必须返回可解释的原因，不能让交互静默失败。

## Tuning Knobs

| Knob | Default / MVP Intent | Safe Range | Too Low / Too Strict | Too High / Too Loose |
|---|---|---|---|---|
| `base_move_speed` | 原型起点 `4.0 units/s`；需用飞艇 Hub 灰盒手感校准 | `3.2-5.2 units/s` | 走动拖沓，飞艇内部像菜单等待 | 跑动感太强，穿过交互点太快 |
| `player_interaction_radius` | `0.25 units`，要求玩家明确靠近目标 | `0.15-0.45 units` | 明明站近却触不到 | 远距离误触，站位意义下降 |
| `default_anchor_radius` | `0.45 units`，常规工作台、舱门、摊位锚点 | `0.25-0.80 units` | 小物件难对准 | 多目标范围重叠，焦点不清 |
| `acquire_margin` | `0.05 units`，获取新焦点需要明确进入范围 | `0.00-0.12 units` | 新焦点太难出现 | 新焦点过早抢走当前焦点 |
| `retain_margin` | `0.20 units`，当前焦点在边界附近保持稳定 | `0.08-0.35 units` | 焦点闪烁 | 焦点粘得太久 |
| `min_focus_score` | `0.35`，低置信候选不进焦点 | `0.20-0.50` | 弱候选也被高亮 | 玩家需要过度精确 |
| `focus_stickiness_bonus` | `0.08`，轻微保留当前焦点 | `0.00-0.15` | 多目标边缘频繁切换 | 当前焦点过度固执 |
| `focus_weight_pointer` | `0.45`，鼠标明确指向优先 | `0.35-0.55` | 鼠标意图不够有力 | 鼠标扫过就抢焦点 |
| `focus_weight_proximity` | `0.25`，近距离是次级依据 | `0.15-0.35` | 靠近行为不够重要 | 最近目标过度抢焦点 |
| `focus_weight_priority` | `0.15`，作者优先级只做辅助 | `0.05-0.25` | 关键站点难以优先 | 作者配置压过玩家意图 |
| `focus_weight_stickiness` | `0.15`，当前焦点有适度稳定性 | `0.05-0.25` | 焦点不稳 | 焦点切换迟钝 |
| `use_lock_timeout_seconds` | `2.0s`，领域系统未释放时自动恢复 | `1.0-5.0s` | 慢交互被误判超时 | 玩家卡住太久 |
| `max_focus_candidates_per_query` | `8`，只限制局部候选排序量 | `4-16` | 密集场景漏掉合理目标 | Web 性能不可控 |
| `movement_block_event_delay` | `0.15s`，短暂撞墙不持续刷阻断事件 | `0.00-0.30s` | 反馈事件噪声太多 | 玩家不知道为什么走不动 |

固定设计值：

- `first_resume_input_consumed = true`，不可调。
- `single_world_focus = true`，不可调。
- `use_is_request_not_result = true`，不可调。
- `gamepad_support = false` for launch。
- `touch_support = false` for launch。
- `auto_path_to_interaction = false` for MVP。
- `cross_room_interaction = false` for MVP。
- `pointer_lock_required = false` for MVP。
- 焦点权重必须归一化，总和保持 `1.0`。

实现边界：

- 阻断原因显示时长归 `UI / HUD / 航图界面`，本系统只输出 `block_reason`。
- 焦点刷新应使用局部范围查询，并在物理帧或交互目标变更事件中更新；不得每帧无界扫描全场景交互对象。
- 所有数值都是原型起点，不是最终手感承诺。

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

[To be designed]

## Open Questions

[To be designed]
