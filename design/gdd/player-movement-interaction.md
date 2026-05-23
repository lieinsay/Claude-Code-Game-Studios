# 玩家移动与交互

> **Status**: Approved
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-09
> **Review Verdict**: APPROVED (re-review 2026-04-29)
> **Implements Pillar**: 飞艇是家，不只是载具; 规划先于冒险; 未知带来温和压力
> **System Index**: `design/gdd/systems-index.md`
> **Platform Pivot Note**: ADR-0019 supersedes browser lifecycle assumptions. Active input implementation targets desktop Godot .NET/C#; lifecycle gates should be interpreted as shell-normalized desktop focus/pause/quit signals.
> **Scene Physics Note**: `design/gdd/scene-physics-unit-system.md` now owns scene unit physics contracts. This system consumes those contracts when interpreting movement, collision, reachability and blocked feedback.

## Overview

`玩家移动与交互` 是《云海织航》的基础玩家动作层，负责把壳层开放后的键鼠输入转化为清楚、可预期、可被阻断的角色移动、可达性判断、交互焦点和 `Use` 入口。玩家通过它在横版飞艇、起始空港、集市摊位和探索点中行走、靠近、确认目标并发起交互；但具体后果，例如购买、采集、修复、安装模块、触发探索结果或打开领域 UI，都由对应系统拥有。本系统的设计目标不是制造复杂操作技巧，而是让玩家感觉自己真的站在有物理规则的飞艇和世界里：能顺畅移动，能被碰撞和阻挡正确限制，能读懂什么可接近、什么可使用、当前会操作哪个对象，也能在后台恢复、壳层 overlay、距离不足或状态锁定时安全地不误触。它支撑“飞艇是家，不只是载具”的身体感，也保护“规划先于冒险”的可读操作节奏。

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
6. 交互必须通过可达性检查：玩家在范围内、目标未被遮挡或阻断、目标当前可用、玩法输入门打开，才允许发出 `interaction_used`。
7. 同一时刻只有一个世界交互焦点。UI `Control` 焦点和世界交互焦点必须分离；壳层或 HUD overlay 可见时，世界交互焦点冻结或清空。
8. 焦点选择优先级为：明确鼠标指向或点击目标、最近的可达目标、上一个仍有效焦点。多个目标同时可达时，用优先级、距离和稳定滞回决定唯一焦点。
9. 焦点切换必须稳定，不得因鼠标轻微抖动、玩家站在两个锚点边缘或候选短暂进出范围而快速闪烁。
10. 失败必须可解释。不可交互时，系统应输出明确阻断原因，例如 `input_closed`、`too_far`、`blocked`、`target_disabled`、`target_busy`、`ui_modal_blocked`。
11. MVP 不做自动寻路、跨房间交互、靠近后自动执行、拖拽式复杂操作、连续长按式操作、gamepad、touch、指针锁依赖或战斗专用输入链。
12. 本系统不能读取或改写货币、库存、资源数量、修复状态、市场库存、模块安装结果、探索奖励、剧情进度或存档内容。
13. 本系统不定义场景单位物理。水平/垂直场景类型、碰撞语义、前后遮挡、单位尺度、特殊表面、弹性/滑动/可破坏等动态行为由 `场景单位物理设计` (#20) 拥有。本系统只消费这些契约并输出移动/阻断/焦点结果。

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
- `Moving / Blocked -> Idle`: 输入门变为 `InputClosed`（覆盖当前移动状态，速度归零）。
- `Rooted -> Idle`: 输入门变为 `InputClosed` 时强制释放 Rooted 锁定。

**Interaction Focus State**

| State | Meaning |
|---|---|
| `NoFocus` | 没有有效候选目标 |
| `Candidate` | 有候选目标，但尚未稳定 |
| `Focused` | 当前目标已稳定为唯一焦点 |
| `UsePending` | 玩家按下 `Use`，正在验证并发出请求 |
| `UseLocked` | `Use` 请求已发出，等待领域系统完成或释放 |

Transitions:

- `NoFocus -> Candidate`: 鼠标指向、玩家进入范围、键盘焦点循环或朝向附近交互目标。
- `Candidate -> Focused`: 候选通过优先级、距离、可达性和滞回稳定检查。
- `Focused -> NoFocus`: 目标离开范围、被遮挡、禁用、销毁或输入门关闭。
- `Focused -> UsePending`: 玩家按下 `Use` 且输入门打开。
- `UsePending -> UseLocked`: `interaction_used` 成功发给领域系统。
- `UsePending -> Focused`: 请求被本系统可达性检查拒绝，并输出阻断原因。
- `UsePending -> NoFocus`: 输入门关闭或焦点目标在帧间消失/禁用（绕过中间 Focused 状态，避免同帧双事件抖动）。
- `UseLocked -> Focused / NoFocus`: 领域系统返回完成、拒绝、取消、超时或释放锁定。
- `Candidate / Focused / UsePending / UseLocked -> NoFocus`: 输入门变为 `InputClosed`（强制清空焦点）。
- `Candidate / Focused -> NoFocus`: 候选池为空且无有效焦点。

### Interactions with Other Systems

| System | This System Receives | This System Sends | Boundary |
|---|---|---|---|
| `平台与会话壳` | `input_gate_open` / `input_gate_reacquire` / `input_gate_closed`, overlay and resume gate state | none required | 壳层决定玩法输入是否可进来；本系统不判断浏览器生命周期 |
| `场景单位物理设计` | Scene Physics Contract: scene type, movement plane, collision, occlusion, scale, special surfaces, physical behaviors | movement state, blocked reasons, use reachability results | #20 拥有物理契约；本系统消费契约并执行移动/可达性判断 |
| `飞艇家园 Hub` | walkable areas, room bounds, interaction anchors, station availability | `interaction_used`, focus events, movement state | Hub 拥有舱室与站点后果；本系统只负责抵达和使用入口 |
| `探索 / 搜撤场景` | walkable areas, extraction anchors, loot/search anchors, threat blockers | `interaction_used`, blocked reasons, movement state | 探索系统拥有搜撤、奖励、撤离和危险后果 |
| `空港 / 村镇状态与集市交易` | stall anchors, NPC / stall availability, market blockers | `interaction_used` for stall or NPC focus | 市集系统拥有购买、货品、价格和库存变化 |
| `世界修复与解锁` | repair node anchors and repair availability | `interaction_used` for repair nodes | 修复系统拥有材料消耗、解锁和世界状态变化 |
| `玩家知识与情报` | location boundary triggers from scene/domain systems | `player_arrived_at(location_id)` when player enters a location zone | 情报系统拥有知识状态变更；本系统只负责检测到达并发送事件 |
| `UI / HUD / 航图界面` | modal / overlay blocking state, optional tooltip presentation policy | focus target, blocked reason, prompt hint, movement/focus state | UI 只显示焦点和原因，不判定可达性 |
| `反馈、特效与音频语义` | none required for MVP | semantic events such as focus changed, use blocked, use requested, movement blocked | 反馈系统表现语义，不拥有规则 |
| `内容数据与状态注册表` | stable interaction target IDs and content definitions through domain systems | none direct in MVP | 本系统不直接解析内容经济或修复规则 |
| `本地存档与世界状态持久化` | none direct in MVP | none direct in MVP | 移动瞬时状态是否保存由具体场景/存档策略决定，不由本系统直接写档 |

### Implementation Architecture

本节定义 Godot 4.6 实现所需的基础架构约定，供程序员在编写第一行代码前参考。

#### Player Node Type

玩家为 `CharacterBody2D`，使用 `move_and_slide()` 处理移动和碰撞。速度流程为：

```
intended_velocity = input_direction * movement_velocity_scalar
actual_velocity = move_and_slide(intended_velocity)
collision_multiplier = 0.0 if actual_velocity.length() == 0 and intended_velocity.length() > 0 else 1.0
```

`collision_multiplier` 从 `move_and_slide()` 的**结果**派生，不是预乘输入。MVP 使用二值（0 或 1）；不计算渐进值。

#### Physics Layers

| Layer | Bit | Name | Purpose |
|-------|-----|------|---------|
| 1 | 0 | `player` | 玩家角色所在层 |
| 2 | 1 | `world_geometry` | 世界几何体（墙壁、地板、场景边界） |
| 3 | 2 | `interactable` | 交互目标（鼠标检测和候选查询） |
| 4 | 3 | `interaction_occlusion` | 阻挡 `path_clear` 射线检测的物体 |

**Collision Masks:**
- 玩家 mask：Layer 2（仅世界几何体用于移动碰撞）
- 交互检测 mask：Layer 2 + Layer 3（目标 + 墙壁用于距离判断）
- `path_clear` 射线 mask：Layer 4（仅交互遮挡物）

#### Units

`1 unit = 1 Godot 2D 米`（默认坐标空间）。所有距离、速度、半径值均以 Godot 米为单位。实际屏幕像素取决于 `Camera2D.zoom`。

#### Camera

每个场景提供一个带平滑定位的 `Camera2D`（`position_smoothing_enabled = true`，默认 `speed = 5.0`）。相机边界通过 `Camera2D.limit_*` 属性设置，与场景可行走区域一致。鼠标世界坐标通过 `camera.get_global_mouse_position()` 获取。

#### Multi-Scene Architecture

- `PlayerMovementInteraction`：`Node` 自动加载（Autoload 单例）。拥有输入门状态、移动状态机、焦点状态机和 Use 流程。
- `Player`：`CharacterBody2D` 场景（`player.tscn`），由场景实例化或通过场景过渡系统放置。
- `InteractionRegistry`：独立的 `Node` 自动加载，接受交互目标的注册/注销。使用 `Area2D.overlapping_areas` 做候选查询。
- 场景过渡系统将玩家放置在新场景入口生成点后，通知 `PlayerMovementInteraction` 初始化。

#### Interactable Contract

所有可交互对象必须继承 `Interactable` 基类。以下为历史 IDL 草案（GDScript 语法），C# 实现使用 `abstract partial class` + `[Export]` 属性：

```gdscript
# 历史 IDL 草案 — C# 实现参考: abstract partial class Interactable : Node2D
class_name Interactable
extends Node2D

@export var interaction_id: StringName        # 稳定 ID（由内容注册表提供）
@export var anchor_radius: float = 0.45       # 交互锚点半径
@export var priority: float = 0.5             # 作者优先级（0-1）
@export var interaction_type: StringName      # 交互类型："talk"/"use"/"trade"/"repair"/"open"

func is_enabled() -> bool: return true        # 目标是否可交互
func get_anchor_position() -> Vector2: return global_position
func is_busy() -> bool: return false          # 目标是否忙碌
func get_display_hint() -> String: return ""  # 简短显示名（≤12 字符）
```

稳定 ID 类型为 `StringName`，由 `内容数据与状态注册表` 提供。ID 比较使用 `StringName` 的 `==` 操作符。

#### Input Map

所有玩法输入必须使用 Godot Input Map 动作名称，不使用原始按键常量：

| Action Name | Type | Default Keys | Purpose |
|-------------|------|-------------|---------|
| `move_up` | 轴 | W, Up Arrow, D-pad Up | 向上移动 |
| `move_down` | 轴 | S, Down Arrow, D-pad Down | 向下移动 |
| `move_left` | 轴 | A, Left Arrow, D-pad Left | 向左移动 |
| `move_right` | 轴 | D, Right Arrow, D-pad Right | 向右移动 |
| `interact` | 动作 | E, Space | 使用/交互 |
| `focus_cycle_next` | 动作 | Tab | 键盘焦点循环（下一个候选） |

移动输入使用 `Input.get_vector(&amp;"move_left&quot;, &amp;"move_right&quot;, &amp;"move_up&quot;, &amp;"move_down&quot;)` 获取归一化方向向量。此方法内置死区并归一化斜向输入，直接实现 Q1 结论。

#### Pointer Score Method

每个 `Interactable` 节点挂载一个用于鼠标检测的 `Area2D`（碰撞层为 Layer 3）。使用 `mouse_entered` / `mouse_exited` 信号设置 `pointer_score`：

- `mouse_entered` → `pointer_score = 1`
- `mouse_exited` → `pointer_score = 0`
- 若多个目标的 `mouse_entered` 同时激活：`pointer_score = 1` 给所有重叠目标，`focus_score` 公式通过优先级和距离打破平局

`Area2D` 的 `mouse_filter` 设置为 `MOUSE_FILTER_PASS`，不拦截点击。

#### Path Clear Definition

`path_clear` 使用 `PhysicsRayQueryParameters2D` 实现：

- 从玩家碰撞原点（或质心）到目标 `get_anchor_position()` 的单次 2D 射线检测
- 碰撞遮罩：Layer 4（`interaction_occlusion`）仅
- 若射线在到达目标前命中 Layer 4 上的任何物体：`path_clear = false`
- 射线不检测玩家自身（`exclude` 参数包含玩家 `RID`）

#### Keyboard Focus Cycling

纯键盘玩家（无鼠标）可以通过 `focus_cycle_next`（Tab 键）循环选择候选目标：

- 按 Tab：跳转到当前候选列表中 `focus_score` 次高的目标
- 再次按 Tab：继续循环，到达末尾时回到最高分候选
- 仅在 `InputOpen` 且候选池非空时可用
- Tab 循环在 `focus_selection` 公式求值**之前**设置焦点意向，公式确认其有效性
- 若有鼠标交互：鼠标选中目标后重置循环位置

此机制独立于 `focus_score` 权重公式——它不重新平衡鼠标权重，而是为键盘玩家提供替代性焦点选择路径。

#### Perspective-Agnostic Movement

移动系统接收归一化 `Vector2` 输入方向，不对视角做假设：

- 横版飞艇：碰撞体和 Camera2D 布局定义侧面视角。Y 轴输入映射为场景中的垂直移动（楼梯、甲板层次）
- 俯视探索点：碰撞体和 Camera2D 布局定义俯视视角。X/Y 轴输入映射为地面平面移动
- `CharacterBody2D.move_and_slide()` 在两种视角下行为一致
- 移动系统不切换行为——视角差异完全由场景的 Camera2D 和碰撞布局决定

#### Frame Rate Independence

移动计算在 `_physics_process` 中运行（固定物理帧步，默认 60Hz）。`move_and_slide()` 内部处理 delta 时间。`movement_velocity_scalar` 输出单位为 units/second。焦点评估在同一物理帧步中执行，鼠标位置每物理帧从 `Input.get_mouse_position()` 新鲜读取。

---

## Formulas

The `movement_velocity` formula is defined in two stages:

**Stage 1 — Intended Velocity:**

`movement_velocity_scalar = clamp(base_move_speed * input_magnitude * gate_multiplier * root_multiplier, 0, max_move_speed)`

`intended_velocity = input_direction * movement_velocity_scalar`

**Stage 2 — Engine-Resolved Actual Velocity:**

`actual_velocity = move_and_slide(intended_velocity)`

`collision_multiplier = 0.0 if (actual_velocity.length() == 0 AND intended_velocity.length() > 0) else 1.0`

`movement_velocity = actual_velocity.length()`

> **Note:** `collision_multiplier` 从 `move_and_slide()` 的**结果**派生，不是预乘输入。`move_and_slide()` 内部处理碰撞滑动；当引擎报告实际速度为零但意图速度非零时，判定为碰撞阻断。

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `input_direction` | `dir` | Vector2 | 归一化 | `Input.get_vector()` 返回的归一化移动方向。 |
| `input_magnitude` | `I` | float | `0-1` | 归一化后的移动输入强度；键盘满输入为 `1`，无输入为 `0`。 |
| `base_move_speed` | `B` | float | `> 0`，须 `<= max_move_speed` | 玩家基础移动速度（units/s）。 |
| `gate_multiplier` | `G` | int | `0-1` | 输入门倍率；`InputOpen = 1`，否则为 `0`。 |
| `root_multiplier` | `R` | int | `0-1` | 玩家被 `Rooted` 时为 `0`，否则为 `1`。 |
| `max_move_speed` | `M` | float | `> 0`，须 `>= base_move_speed` | 速度上限（units/s）。 |
| `movement_velocity_scalar` | `VS` | float | `0-M` | 本帧意图速度标量（units/s）。 |
| `intended_velocity` | `IV` | Vector2 | `(0,0)` 到 `M` 长度 | 传递给 `move_and_slide()` 的速度向量。 |
| `actual_velocity` | `AV` | Vector2 | `(0,0)` 到 `M` 长度 | `move_and_slide()` 返回的实际速度（碰撞处理后）。 |
| `collision_multiplier` | `C` | float | `0-1` | 实际速度归零但意图非零时为 `0`；否则为 `1`。MVP 二值。 |
| `movement_velocity` | `V` | float | `0-M` | 本帧最终实际移动速度（units/s），`actual_velocity.length()`。 |

**Output Range:** `0` to `max_move_speed`。输入门关闭、Rooted 时输出必须为 `0`。碰撞阻断时 `movement_velocity = 0`，但 `move_and_slide()` 的沿墙滑动可能产生非零实际速度。
**Example:** `base_move_speed=4.2`, `input_magnitude=1`, `gate_multiplier=1`, `root_multiplier=1`, `max_move_speed=4.2` 时，`movement_velocity_scalar=4.2`，`intended_velocity=(0, 4.2)`，若无障碍则 `actual_velocity=(0, 4.2)`，`collision_multiplier=1`，`movement_velocity=4.2`。若 `root_multiplier=0`，则 `movement_velocity_scalar=0`，`intended_velocity=(0, 0)`，`movement_velocity=0`。

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
| `reach_limit` | `L` | float | `> 0` | 最终可达距离阈值。若所有组件均为 0 导致 `L=0`，目标不可达。 |

**Output Range:** true/false。当前焦点使用 `retain_margin`，新候选使用 `acquire_margin`，用来减少边缘抖动。
**Constraint:** `acquire_margin < retain_margin` 为强制配置约束。若违反，滞回机制崩溃，焦点在边界处高频切换。
**Example:** `distance_to_anchor=0.84`, `anchor_radius=0.45`, `player_interaction_radius=0.25`，当前焦点 `retain_margin=0.20` 时，`reach_limit=0.90`，可达；若不是当前焦点且 `acquire_margin=0.05`，`reach_limit=0.75`，不可达。

The `focus_score` formula is defined as:

`proximity_score = 0 if reach_limit == 0 else 1 - clamp(distance_to_anchor / reach_limit, 0, 1)`

`focus_score = clamp(0.45 * pointer_score + 0.25 * proximity_score + 0.15 * priority_score + 0.15 * stickiness_score, 0, 1)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| `pointer_score` | `PS` | float | `0-1` | 鼠标明确指向该目标为 `1`，否则为 `0`。 |
| `distance_to_anchor` | `D` | float | `>= 0` | 玩家到目标交互锚点的距离。 |
| `reach_limit` | `L` | float | `> 0` | 该目标的可达距离阈值。 |
| `proximity_score` | `NS` | float | `0-1` | 距离越近分越高。 |
| `priority_score` | `RS` | float | `0-1` | 作者配置的目标优先级归一化结果。 |
| `stickiness_score` | `SS` | float | `0-1` | **Stage-1 stickiness（焦点评分粘性）**：当前有效焦点为 `1`，否则为 `0`。权重 0.15，影响候选排序。 |
| `focus_score` | `FS` | float | `0-1` | 候选目标的最终焦点分数。 |

> **Naming clarification:** `stickiness_score`（SS，权重 0.15）作用于 `focus_score` 排序阶段——让当前焦点在候选排序中占优。`focus_stickiness_bonus`（SB，默认 0.08）作用于 `focus_selection` 选择阶段——在最终选中时额外加成。两者共同构成两级滞回机制（总计粘性加成 = SS + SB = 0.15 + 0.08 = 0.23 默认值），但作用于不同阶段，避免混淆。

**Output Range:** `0` to `1`。分数只负责排序，不直接执行交互。权重表达 MVP 优先级：明确指向 > 近距离可达 > 作者优先级 > 焦点黏性。
**Example:** 鼠标指向目标且 `proximity_score=0.38`, `priority_score=0.60`, `stickiness_score=0` 时，`focus_score=0.45 + 0.095 + 0.09 = 0.635`。

The `focus_selection` formula is defined as:

`candidate_selection_score_i = focus_score_i + current_focus_bonus_i`

`current_focus_bonus_i = focus_stickiness_bonus if i = current_focus_target_id AND current_focus_valid else 0`

`focus_selection = keyboard_cycle_target if focus_cycle_input_active AND valid_candidates_exist else NoFocus if candidate_pool_empty OR max(candidate_selection_score_i) < min_focus_score else argmax(candidate_selection_score_i, tie_break = higher_priority_score > shorter_distance > stable_id_order)`

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
| `focus_cycle_input_active` | `FC` | bool | true/false | 本帧是否有键盘焦点循环输入（Tab 键）。 |
| `keyboard_cycle_target` | `KCT` | stable id / null | id/null | 键盘循环选中的目标，由 Tab 键在候选列表中按 `focus_score` 降序循环产生。 |

**Output Range:** `NoFocus` 或一个稳定目标 ID。任何时候只允许一个世界焦点。
**Tie-Breaking:** `argmax` 遇到等分候选时，依次按更高作者优先级 (`priority_score`)、更短距离 (`distance_to_anchor`)、稳定 ID 字典序决定胜出者。
**Keyboard Cycling:** Tab 键在候选列表中按 `focus_score` 降序循环，到达末尾时回到最高分候选。有鼠标交互时循环位置重置。此机制为纯键盘玩家提供替代性焦点选择路径，不重新平衡 `focus_score` 的鼠标权重。
**Example:** 当前焦点 A 的 `focus_score=0.57`，`focus_stickiness_bonus=0.08`，最终 `0.65`；新候选 B 的 `focus_score=0.62`。A 仍有效时保留 A；A 失效时 B 胜出。两个候选等分且等优先级时，距离更近者胜出。

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

**Output Range:** `Allowed` 或 `Blocked(reason)`。此门只决定是否分发 `Use` 请求，不决定领域后果。
**Example:** `input_gate_open=true`, `focus_selection=A`, `target_enabled=true`, `path_clear=true`, `distance_to_anchor=1.30`, `reach_limit=1.05`, `target_busy=false` 时，`use_gate=Blocked(too_far)`。若 `input_gate_open=false`，则优先返回 `Blocked(input_closed)`。

**Use Request Dispatch Pattern:** `use_gate = Allowed` 时，系统执行两种不同的通信：

1. **Signal（即发即忘，供反馈系统消费）：**
   ```
   signal interaction_used(target_id: StringName, interaction_type: StringName)
   ```
   用于 `反馈、特效与音频语义` 系统播放确认视觉/音频。不返回结果。

2. **Method Call（请求-响应，供领域系统消费）：**
   ```
   func handle_use(player_id: StringName) -> UseResult
   ```
   调用领域系统的 `handle_use(player_id)` 方法。领域系统返回 `UseResult`（accepted / rejected / busy / timeout）。若为 accepted，进入 `UseLocked`；若为 rejected，输出 `block_reason`。

领域系统在接受锁定后负责通过回调或信号释放锁（`release_use_lock(target_id)`），本系统不自行判定领域后果。

## Edge Cases

- **If the shell is loading, background suspended, in an error state, showing an overlay, or reporting `InputReacquire`**: treat this system as `InputClosed`; discard movement and `Use` inputs immediately, with no queueing and no replay.
- **If the first keyboard or mouse input arrives after browser focus / visibility recovery**: consume it only for shell reactivation; do not produce movement, focus confirmation, or `Use` until a later valid gameplay input edge.
- **If the player is holding movement or `Use` during `InputReacquire`**: do not backfill any action when input opens; the player must release and press again.
- **If a `Control` UI element has focus and the player presses `Use`**: route the input only to UI; do not emit `interaction_used`, and keep or freeze the world focus.
- **If a HUD or shell modal is visible even though `Control` focus has not changed**: freeze world focus and block all `Use` attempts with `ui_modal_blocked`.
- **If the player clicks empty world space**: keep the current world focus if it remains valid; otherwise stay in `NoFocus`; do not emit `interaction_used`.
- **If small mouse jitter causes multiple targets to enter the candidate pool**: retain the current valid focus first; otherwise choose only the highest `focus_score` target.
- **If two candidates have equal `focus_score` or tie at the focus threshold**: keep the current focus if valid; otherwise break the tie by higher author priority, then shorter distance, then stable ID order.
- **If multiple interactable targets are valid and the mouse does not clearly point at one**: select exactly one world focus by `focus_score`; never highlight multiple world targets as active focus.
- **If `distance_to_anchor == reach_limit`**: treat the target as reachable and allow focus or `Use` if all other gates pass.
- **If a target is just beyond `reach_limit`**: block new `Use` as `too_far`; retain current focus only if the target remains inside the larger `retain_margin`.
- **If the current focus flickers around the reach boundary**: keep focus while it remains inside `retain_margin`; clear or switch only after it exits that margin.
- **If a target is blocked by geometry, building pieces, scene boundaries, or another blocking entity**: remove it from interactable eligibility; return `Blocked(blocked)` for `use_gate`; if it was focused, clear focus or choose the next valid candidate.
- **If the current focus is blocked, disabled, destroyed, or moved out of range on the same frame as `Use`**: fail `Use`, emit no domain consequence, and report the latest valid block reason.
- **If `target_busy = true` while the target remains visible and reachable**: keep focus if appropriate, but block `Use` with `target_busy` and do not enter `UseLocked`.
- **If `interaction_used` has been emitted and the domain system accepts a lock**: enter `UseLocked`; optionally place movement in `Rooted`; reject repeated `Use` until the domain system releases the lock.
- **If the domain system times out or fails to release a `UseLocked` interaction**: cancel the pending `Use`, release movement lock, and re-evaluate focus; do not automatically resubmit `Use`.
- **If the player presses movement or repeatedly presses `Use` during `UseLocked`**: ignore those inputs; do not queue, repeat, or execute them after unlock.
- **If a scene transition begins while the player is in `Focused`, `UsePending`, or `UseLocked`**: close input immediately, clear world focus, and cancel or let the old domain system safely finish the current `Use`; the new scene must not inherit old focus or replay old `Use`.
- **If an interactable node is rebuilt but keeps the same stable ID**: treat it as the same target for focus and `Use` routing, subject to fresh reachability and availability checks.
- **If an interactable is replaced, hot-reloaded, or moved to a new scene with a different stable ID**: invalidate the old focus immediately and do not map it onto the new object.
- **If a target changes `target_enabled` from true to false while focused**: clear focus and cancel any pending `Use`; report `target_disabled`.
- **If the browser restores while the mouse is already over a target**: allow focus to refresh only on the next valid gameplay focus update; never auto-execute `Use` on the restore frame.
- **If the player presses `Use` with no world focus (`NoFocus` state)**: emit `use_blocked(no_focus)` and provide a subtle visual pulse (~0.1s) on the player character (not on a target) to acknowledge the input was received. Do not display a Toast.
- **If the player presses Tab for keyboard focus cycling and the candidate pool is empty**: do nothing; remain in `NoFocus`.
- **If the player presses Tab for keyboard focus cycling and the candidate pool has exactly one target**: select that target as focus (no cycling needed).
- **If the player presses Tab and wraps around the candidate list**: seamlessly return to the highest-scoring candidate without a gap or double-press requirement.
- **If the player presses `Use` within `input_buffer_window` (default 0.10s) before the focus stabilizes or before `UseLocked` releases**: buffer the input and re-evaluate at the end of the window. If still valid, proceed with `Use`; if the window expires, discard silently.
- **If the player presses `Use` during `UseLocked` on a different target than the locked one**: block with `target_busy` on the new target; do not queue.
- **If a `movement_blocked` event and a `interaction_focus_changed` event occur on the same frame**: `interaction_focus_changed` takes priority per the event priority rule (`interaction_focus_changed` > `movement_blocked`); the `movement_blocked` event is suppressed for that frame.
- **If the same target ID is reused after the original object was destroyed and re-created**: treat as a new target; do not carry over old focus state, sticky bonuses, or pending Use. Requires fresh reachability and availability checks.
- **If `acquire_margin >= retain_margin` due to configuration error**: log a validation warning on startup; fall back to `acquire_margin = retain_margin * 0.5` to preserve hysteresis.

## Dependencies

硬依赖：

- `平台与会话壳`：提供 `input_gate_open` / `input_gate_reacquire` / `input_gate_closed`，并负责加载、恢复、后台挂起、overlay 和第一下恢复输入消费。本系统不得自行判断浏览器生命周期或绕过壳层门禁。
- `场景单位物理设计`：每个使用本系统的可进入场景必须提供 Scene Physics Contract。没有物理契约时，本系统只能使用保守阻断规则，不得猜测单位碰撞、遮挡、尺度、特殊表面或动态物理行为。
- 场景可行走区域与碰撞边界：每个使用本系统的场景必须提供可行走区域、阻挡体、边界和临时锁定区域。没有这些数据时，本系统只能关闭移动或进入安全阻断态。
- 交互目标契约：每个可交互对象必须提供稳定 ID、交互锚点、交互半径、可用状态、优先级、忙碌状态和阻断原因。本系统只消费这些数据，不推断领域后果。
- 输入映射：MVP 依赖键盘移动和单一 `Use` 输入动作。gamepad、touch、指针锁、拖拽和长按不是 launch 范围。

软依赖：

- `UI / HUD / 航图界面`：显示当前焦点、可用提示、阻断原因和交互反馈；若 UI 尚未完整，本系统仍可通过调试提示或最小提示运行。
- `反馈、特效与音频语义`：表现 `interaction_focus_changed`、`interaction_used`、`use_blocked`、`movement_blocked` 等语义事件；MVP 可先用轻量视觉提示替代完整音画反馈。
- `内容数据与状态注册表`：长期应提供稳定目标 ID 和交互类型定义；MVP 可以由场景作者临时配置，但不得使用显示名或节点路径作为最终交互身份。
- `本地存档与世界状态持久化`：本系统不直接写档；若某些场景需要保存玩家位置或交互状态，应由场景或领域系统把可保存状态交给存档系统。

下游系统契约：

| System | Depends on This System For | Must Provide Back |
|---|---|---|
| `场景单位物理设计` | 本系统执行其物理契约中的移动、碰撞、阻断和可达性判断 | Scene Physics Contract、恢复规则和物理行为优先级 |
| `飞艇家园 Hub` | 舱室内移动、站点焦点、工作台/货架/舱门/伙伴驻点 `Use` 入口 | 舱室边界、站点锚点、站点可用状态、站点领域处理结果 |
| `探索 / 搜撤场景` | 探索点移动、搜索点/撤离点/风险点焦点和 `Use` 入口 | 探索区域、可搜目标、撤离锚点、威胁阻断、领域处理结果 |
| `空港 / 村镇状态与集市交易` | 摊位、NPC、公告点和市场入口的焦点与 `Use` 请求 | 摊位锚点、NPC 可用状态、市场忙碌/关闭原因、交易 UI 入口 |
| `世界修复与解锁` | 修复节点的触达、焦点和 `Use` 请求 | 修复节点锚点、材料/情报可用性摘要、修复忙碌/锁定原因 |
| `玩家知识与情报` | 地点到达事件 `player_arrived_at(location_id)` | 地点边界定义、location_id 的注册表解析 |
| `UI / HUD / 航图界面` | 当前焦点、提示文案、阻断原因和输入阻断状态 | 模态 UI 是否阻断世界输入、提示展示策略 |
| `反馈、特效与音频语义` | 移动阻断、焦点变化、Use 请求、Use 阻断等语义事件 | 只返回表现完成或忽略；不得改变规则结果 |

边界声明：

- 本系统不拥有货币、库存、资源、修复、市场、模块安装、探索奖励、战斗、剧情或存档结果。
- 本系统不打开领域 UI；它只发送 `interaction_used`，由领域系统决定是否打开 UI。
- 本系统不做自动寻路、不跨场景保持焦点、不跨房间远程交互。
- 本系统不拥有场景单位物理设计；它不决定物体是否弹性、可推动、滑动、可破坏、透视、反射或作为特殊表面。
- 本系统不直接订阅窗口焦点、暂停、退出等平台事件；这些都由 `平台与会话壳` 归一化后传入。
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
| `accel_time` | `0.0s`（MVP），角色达到全速的时间。0 为瞬时加速 | `0.00-0.25s` | 无加速（二进制手感，MVP 可接受） | 角色感觉迟钝、不跟手 |
| `input_deadzone` | `0.0`（键盘无死区），手柄实现时设为 `0.15` | `0.00-0.25` | 无漂移容忍 | 微小的手柄漂移导致角色爬行 |
| `input_buffer_window` | `0.10s`，Use 键缓冲窗口 | `0.00-0.20s` | 零缓冲，提前按键被丢弃 | 缓冲过长导致误触跨状态执行 |
| `accel_curve` | `linear`（MVP），加速曲线形状 | `linear` / `ease_in` / `ease_out` | N/A（MVP 不启用加速） | N/A（MVP 不启用加速） |

**强制配置约束：**
- `base_move_speed <= max_move_speed`：若违反，base 值被 clamp 持续截断，浪费配置值。
- `acquire_margin < retain_margin`：若违反，滞回机制崩溃，焦点在边界处高频抖动。
- 焦点权重总和须保持 `1.0`（`focus_weight_pointer + focus_weight_proximity + focus_weight_priority + focus_weight_stickiness = 1.0`）。

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

本系统不直接产生视觉或音频资源；它向 `反馈、特效与音频语义` 系统输出标准化语义事件，
由反馈系统负责表现。本节定义语义事件契约和最低表现需求。

### Semantic Event Contract

每个语义事件包含以下字段，供反馈系统消费：

| Field | Type | Description |
|---|---|---|
| `event_type` | enum | `interaction_focus_changed` / `interaction_used` / `use_blocked` / `movement_blocked` / `input_gate_changed` |
| `timestamp` | float | 事件发生时刻（秒，游戏时间） |
| `source_system` | string | 固定为 `player_movement_interaction` |
| `payload` | dict | 事件特定数据（见各事件定义） |

### Event Definitions

#### interaction_focus_changed

触发时机：世界焦点从旧目标切换为新目标（包括清空为 NoFocus）。

| Payload Key | Type | Description |
|---|---|---|
| `previous_focus_id` | stable id / null | 旧焦点目标 ID，null 表示之前无焦点 |
| `new_focus_id` | stable id / null | 新焦点目标 ID，null 表示清空 |
| `transition_reason` | enum | `acquired` / `lost` / `switched` / `cleared` |
| `interaction_type` | string / null | 新焦点目标的简短显示提示文案 |

**最低视觉需求：**
- 焦点获取 (`acquired` / `switched`)：目标上方或附近出现轻量高亮轮廓（2px, 半透明暖色），
  持续时间约 0.25s 淡入
- 焦点丢失 (`lost` / `cleared`)：高亮轮廓 0.15s 淡出
- 高亮不得遮挡目标本体，不得改变玩家移动速度或输入响应
- 色盲安全：高亮颜色需同时传递形状变化（轮廓线从虚线变实线），不单独依赖色相区分

**最低音频需求：**
- `acquired`：轻柔短音（~80ms），音高略升，音量低于环境音 6dB
- `lost` / `cleared`：无音频（避免频繁切换造成噪声）
- `switched`：极短音（~40ms），与 `acquired` 同音色但半音高

#### interaction_used

触发时机：`use_gate = Allowed`，`interaction_used` 成功发送给领域系统。

| Payload Key | Type | Description |
|---|---|---|
| `target_id` | stable id | 被使用的目标 ID |
| `interaction_type` | string | 目标简短显示名 |

**最低视觉需求：**
- 目标上出现确认闪光（单帧白色闪白 + 0.2s 快速衰减至透明）
- 闪白不应遮盖目标本体，面积限制在锚点半径内
- 若领域系统在 0.3s 内接受锁定：闪光延续为稳定微亮（表示交互进行中）

**最低音频需求：**
- 短促确认音（~120ms），中性正向音色
- 音高略低于 `interaction_focus_changed` 获取音，与 `use_blocked` 形成对比

#### use_blocked

触发时机：`use_gate = Blocked(reason)`，`interaction_used` 未被发送。

| Payload Key | Type | Description |
|---|---|---|
| `target_id` | stable id / null | 被尝试使用的目标 ID |
| `block_reason` | enum | `input_closed` / `ui_modal_blocked` / `no_focus` / `target_disabled` / `blocked` / `too_far` / `target_busy` |

**最低视觉需求：**
- 若 `target_id` 非空且 `block_reason` 为 `too_far` 或 `blocked`：
  目标轮廓快速闪烁一次（0.1s 红色微闪），提示"这个目标不可达"
- 若 `block_reason` 为 `target_busy`：
  目标轮廓微闪黄色（0.15s），表示"稍等"
- 若 `block_reason` 为 `no_focus`、`input_closed`、`ui_modal_blocked`：
  不产生目标级反馈（无目标可高亮），改为玩家角色上短暂微闪（~0.1s 半透明脉冲），仅表示"输入已收到但当前无对象"。由 UI 系统负责显示阻断原因文本
- 所有阻断反馈面积限制在目标锚点范围内，不扩散到场景

**最低音频需求：**
- 统一轻阻断音（~60ms），低频闷音，音量低于环境音 8dB
- 不同 `block_reason` 使用相同音色，由 UI 负责区分具体原因
- `no_focus` / `input_closed` 不触发音频
- 阻断音与 `interaction_used` 确认音应有明显区别，形成"成功/失败"音频对

#### movement_blocked

触发时机：玩家有移动输入但因碰撞或边界无法位移，且距离上次同方向阻断事件超过
`movement_block_event_delay`。

| Payload Key | Type | Description |
|---|---|---|
| `block_direction` | vector2 | 移动输入方向（归一化） |
| `block_type` | enum | `collision` / `boundary` / `scene_locked` |

**最低视觉需求：**
- 不产生目标级视觉反馈
- 可选：玩家脚下出现微弱的接触方向指示（短暂粉尘或地面微光，优先级低）

**最低音频需求：**
- `collision`：极轻碰撞音（~40ms），类似脚步碰到硬物（音量低于环境音 10dB）
- `boundary`：无音频（边界不可见是关卡设计问题，不应靠音频弥补）
- `scene_locked`：无音频
- 阻断事件限流必须和调参中的 `movement_block_event_delay` 一致，不能每帧刷音效

#### input_gate_changed

触发时机：输入门状态变化（`InputClosed` / `InputReacquire` / `InputOpen`）。

| Payload Key | Type | Description |
|---|---|---|
| `previous_gate` | enum | 旧门状态 |
| `new_gate` | enum | 新门状态 |

**最低视觉需求：**
- `InputOpen`：无视觉（正常状态）
- `InputClosed`（因 overlay 或 pause）：场景整体微暗（10-15% 暗度覆盖，0.3s 过渡），
  表示世界输入暂停
- `InputReacquire`：暗度覆盖保留，等待第一下恢复输入被消费后自动移除

**最低音频需求：**
- `InputOpen` → `InputClosed`：轻声闷音（~100ms），表示世界"静音"
- `InputClosed` → `InputOpen`：轻声开音（~100ms），比关闭音略亮
- `InputReacquire`：不额外触发音频

### Cross-Cutting Rules

1. **表现优先级**：`interaction_used` > `use_blocked` > `interaction_focus_changed` > `movement_blocked`。
   同一帧内同时触发多个事件时，只播放优先级最高的事件反馈。
2. **音频预算**：移动/交互反馈总并发数不超过 3 个音源；超出时丢弃最早播放中的非循环音。
3. **Web 约束**：所有音频必须在 `平台与会话壳` 的音频激活（用户手势）之后才能播放；
   激活前的语义事件保留但不播放历史音频。
4. **色盲安全**：所有焦点和阻断视觉反馈必须同时包含非色相维度（轮廓样式、亮度变化、形状变化）；
   不得仅依赖红/绿/黄色区分状态。
5. **表现可关闭**：玩家设置中可关闭交互辅助视觉高亮；关闭后仅保留 `interaction_used` 闪白和
   `use_blocked` 红色微闪，移除焦点轮廓和持续高亮。

### MVP Scope Boundary

MVP 阶段：
- 焦点高亮 → 轻量轮廓（无动画曲线，无粒子）
- 确认/阻断反馈 → 单色闪白 + 简单阻断闪烁
- 音频 → 2-3 个短音效（焦点、确认、阻断），可复用引擎内置音或极简合成音
- 不做：焦点切换缓动动画、交互区域光晕、距离渐变透明度、角色动画绑定、
  音频空间化（3D 定位）、触觉反馈

MVP 之后可以迭代增强但不阻塞 launch：
- 角色面向焦点目标的转身动画
- 距离渐变的焦点提示强度
- 交互区域地面投影光标
- 手柄和触屏的独立反馈方案

## UI Requirements

本系统不拥有 UI 控件或布局；它向 `UI / HUD / 航图界面` 系统提供显示所需的数据契约。
UI 系统负责渲染，本系统负责判定。

### Data Contract

本系统向 UI 层暴露以下只读数据流，UI 每帧或按需拉取：

| Data Point | Type | Update Frequency | Description |
|---|---|---|---|
| `world_focus_id` | stable id / null | on change | 当前世界焦点目标 ID |
| `world_focus_display_hint` | string / null | on change | 焦点目标的简短显示名（由领域系统提供） |
| `world_focus_state` | enum | on change | `NoFocus` / `Candidate` / `Focused` / `UsePending` / `UseLocked` |
| `last_block_reason` | enum / null | on use_blocked | 最近一次 `Use` 阻断原因 |
| `last_block_target_id` | stable id / null | on use_blocked | 最近一次被阻断的目标 ID |
| `input_gate_state` | enum | on change | `InputOpen` / `InputClosed` / `InputReacquire` |
| `movement_state` | enum | on change | `Idle` / `Moving` / `Blocked` / `Rooted` |

### UI Elements

#### 1. World Focus Indicator（世界焦点指示器）

**触发条件：** `world_focus_state` 为 `Focused`、`UsePending` 或 `UseLocked`

**位置：** 焦点目标上方（世界空间锚点偏移），非屏幕固定位置

**内容：**
- 目标名称（`world_focus_display_hint`）：单行，不超过 12 字符，截断时显示省略号
- 焦点状态视觉区分：
  - `Focused`：白色/暖色名称，静态
  - `UsePending`：名称微亮闪烁（等待验证）
  - `UseLocked`：名称旁出现小进度指示器（旋转或填充），表示交互进行中

**行为：**
- 目标名称在 `Focused` 状态下延迟 0.3s 后才显示（避免快速切换时闪烁）
- `UsePending` 立即显示，不做延迟
- 切换到 `NoFocus` 时淡出 0.2s
- 名称不得遮挡目标本体超过 15% 面积
- 指示器不响应鼠标事件（点击穿透到世界）

**色盲安全：** 不同焦点状态通过文字样式区分（静态 / 闪烁 / 进度指示器），不单独依赖颜色

#### 2. Block Reason Toast（阻断原因提示）

**触发条件：** `use_blocked` 事件发生，且 `block_reason` 为 `too_far`、`blocked`、`target_busy` 或 `target_disabled`（全部 4 种玩家可纠正的阻断原因）

**位置：** 屏幕底部居中，不跟随目标

**内容：**
- 简短阻断原因文案，按原因映射：

| block_reason | Display Text (zh-CN) | Duration |
|---|---|---|
| `too_far` | 太远了，走近一点 | 2.0s |
| `blocked` | 过不去 | 1.5s |
| `target_busy` | 稍等一下 | 1.5s |
| `target_disabled` | 现在用不了 | 2.0s |

**行为：**
- 淡入 0.15s，停留对应时长，淡出 0.3s
- 新阻断原因覆盖旧 Toast（不排队）
- `no_focus`、`input_closed`、`ui_modal_blocked` 不显示 Toast（属于正常状态，不构成玩家需要纠正的操作）

**色盲安全：** Toast 使用通用中性样式（白色文字 + 半透明深色底），不依赖颜色编码

#### 3. Input Gate Overlay（输入门状态覆盖层）

**触发条件：** `input_gate_state` 为 `InputClosed`（因 overlay/pause）或 `InputReacquire`

**内容：**
- `InputClosed`（场景暗度覆盖）：半透明黑色覆盖层（opacity 0.10-0.15），覆盖全场景
- `InputReacquire`：与 `InputClosed` 相同视觉，附加居中提示文字"按任意键或点击继续"（12px，白色半透明）
  - 提示文字在首次鼠标点击或键盘按下后立即消失
  - 提示文字不响应 `focus` 事件（避免和世界焦点 UI 混淆）

**行为：**
- 覆盖层过渡时间 0.3s（与 Visual 部分一致）
- 覆盖层不阻止 UI 控件响应（菜单、设置等仍可操作）
- `InputOpen` 时覆盖层移除，过渡 0.15s

#### 4. Interaction Prompt Hint（交互提示）

**触发条件：** `world_focus_state` 为 `Focused`，目标支持 `Use` 操作

**位置：** 世界焦点指示器下方偏移

**内容：**
- 按键提示：`[E]` 或 `[Space]`（可配置），形式为小号半透明按键图标
- 操作名称：`使用`、`交谈`、`查看` 等（由领域系统提供）

**行为：**
- 与焦点指示器同步显示/隐藏
- 焦点在 `UseLocked` 时隐藏提示（已在交互中）
- 提示不响应点击（不是按钮，只是提示）

#### 5. Focus Debug Overlay（焦点调试面板）— 开发阶段可选

**触发条件：** 开发者开关（非玩家功能）

**内容：**
- 当前世界焦点 ID、状态、分数
- 候选目标列表（ID + focus_score + reach_limit + distance）
- 最近阻断原因和时间戳
- 输入门和移动状态

**行为：**
- 固定在屏幕左上角，小号等宽字体
- 不影响游戏性能查询范围（复用已有候选池数据）
- 发布版本不编译此面板

### UI Interaction Rules

1. **焦点分离**：UI `Control` 焦点和世界交互焦点不共享。当 UI 控件（菜单按钮、输入框、滑块）
   获得焦点时，世界焦点指示器隐藏，世界 `Use` 输入全部阻断为 `ui_modal_blocked`。
2. **模态阻断**：当 HUD 模态面板（设置、存档菜单、对话面板）打开时，世界焦点冻结当前值，
   提示和 Toast 全部隐藏，`input_gate_state` 对外报告 `InputClosed`。
3. **点击穿透**：所有世界空间 UI 元素（焦点指示器、提示）不拦截鼠标事件；
   玩家点击这些元素位置时应穿透过到世界，允许移动或选中目标。
4. **暂停行为**：`InputClosed`（overlay/pause 原因）时，现有焦点指示器和提示立即隐藏；
   恢复时焦点不自动恢复（需玩家重新移动或指向目标触发焦点刷新）。
5. **场景切换**：场景过渡开始时立即清除所有 UI 元素（指示器、Toast、覆盖层、提示）；
   新场景不继承旧 UI 状态。
6. **本地化**：所有玩家可见文案（阻断原因、提示操作名、输入恢复提示）必须经过本地化表；
   不得硬编码中文字符串。

### MVP Scope Boundary

MVP 阶段：
- World Focus Indicator：基础名称显示，`Focused`/`UsePending`/`UseLocked` 三种状态
- Block Reason Toast：全部 4 种玩家可纠正原因（`too_far`、`blocked`、`target_busy`、`target_disabled`）
- Input Gate Overlay：简单暗度覆盖，带"按任意键或点击继续"文字
- Interaction Prompt Hint：固定 `[E] 使用` 文字

MVP 不做：
- 焦点指示器平滑跟随动画
- Toast 多条排队和优先级管理
- 自定义按键图标渲染（直接用文本 `[E]`）
- 焦点调试面板（可在 dev build 中保留但非必需）
- 控制器/触屏的独立 UI 方案
- 阻断原因图标（只用文字 Toast）

## Acceptance Criteria

### Movement

- [ ] **AC-MOV-001 — Basic Movement**: 当 `InputOpen` 且玩家按下移动键（WASD/方向键）时，角色以
  `base_move_speed` × `input_magnitude` 的速度沿输入方向移动。验证：灰盒场景中测量 1 秒位移量，
  误差在 ±5% 内。
- [ ] **AC-MOV-002 — Movement Stop**: 当玩家释放所有移动键时，角色在 1 帧内速度归零（无惯性滑行）。
  验证：逐帧检查释放键后第一帧 `movement_velocity = 0`。
- [ ] **AC-MOV-003 — Speed Cap**: 任何情况下 `movement_velocity` 不超过 `max_move_speed`。
  验证：同时按下两个方向键（斜向移动），速度不超过上限。
- [ ] **AC-MOV-004 — Input Closed Blocks Movement**: 当 `input_gate_state` 为 `InputClosed` 时，
  任何移动输入不产生位移。验证：暂停/后台恢复期间按住移动键，角色位置不变。
- [ ] **AC-MOV-005 — Rooted Blocks Movement**: 当 `movement_state` 为 `Rooted` 时，
  `movement_velocity` 输出为 0，无论输入为何。验证：触发 Rooted 锁定后移动键无效。
- [ ] **AC-MOV-006 — Collision Block**: 当角色被碰撞体或场景边界阻挡时，`movement_state` 变为
  `Blocked`，且角色不穿越阻挡体。验证：走向墙壁，角色停在碰撞边界。
- [ ] **AC-MOV-007 — Collision Block Event Throttle**: `movement_blocked` 事件在持续撞墙时按
  `movement_block_event_delay` 限流发送，不在每帧重复。验证：持续走向墙壁 1 秒，事件计数不超过
  `1 / movement_block_event_delay + 1`。

### Input Gate

- [ ] **AC-GATE-001 — Shell Gating**: `input_gate_state` 仅在 `平台与会话壳` 发出
  `input_gate_open` / `input_gate_closed` / `input_gate_reacquire` 信号时变化。验证：直接调用
  平台窗口焦点事件不改变 gate state。
- [ ] **AC-GATE-002 — Reacquire Consumes First Input**: 当 `input_gate_state` 为 `InputReacquire`
  时，第一次键盘/鼠标输入不产生移动或 `Use`。验证：从后台恢复后，恢复帧的按键被消费，角色不动。
- [ ] **AC-GATE-003 — Reacquire No Backfill**: 在 `InputReacquire` 期间按住移动键，切换到
  `InputOpen` 时角色不自动开始移动。验证：恢复期间按住 W，恢复后角色仍 Idle，需松开再按才走。
- [ ] **AC-GATE-004 — Overlay Closes Gate**: 当壳层显示 overlay 时，`input_gate_state` 切换为
  `InputClosed`，移动和 `Use` 立即阻断。验证：打开设置菜单后按移动键和使用键均无效。

### Interaction Focus

- [ ] **AC-FOCUS-001 — Focus Acquisition**: 当玩家进入交互目标的 `reach_limit` 范围，且目标可用、
  未被遮挡时，候选进入焦点池，得分最高且超过 `min_focus_score` 的候选成为世界焦点。验证：接近
  已配置锚点的目标，焦点指示器出现。
- [ ] **AC-FOCUS-002 — Single World Focus**: 任何时候 `world_focus_id` 最多为一个有效 ID，
  不存在多目标同时高亮。验证：在两个重叠目标之间移动，始终只有一个焦点指示器。
- [ ] **AC-FOCUS-003 — Focus Stability (Hysteresis)**: 当前焦点目标在 `retain_margin` 范围内
  保持焦点，即使有稍近的新目标进入 `acquire_margin`。验证：走向两个相邻目标 A→B→A，焦点不会
  在边界处快速来回切换。
- [ ] **AC-FOCUS-004 — Focus Loss on Leave**: 当当前焦点目标超出 `retain_margin` 范围时，焦点
  清除或切换到下一个有效候选。验证：从目标走远，焦点指示器消失。
- [ ] **AC-FOCUS-005 — Focus Loss on Disable**: 当当前焦点目标的 `target_enabled` 变为 false 时，
  焦点立即清除并报告 `target_disabled`。验证：脚本禁用焦点目标，指示器立即消失。
- [ ] **AC-FOCUS-006 — Focus Loss on Block**: 当当前焦点目标被几何体遮挡时，焦点清除。验证：在
  玩家和目标之间放置阻挡体，焦点消失。
- [ ] **AC-FOCUS-007 — Mouse Priority**: 当鼠标明确指向一个可交互目标时，该目标获得
  `pointer_score = 1`，在得分中占最高权重。验证：玩家站在两个目标之间等距处，鼠标悬停的目标
  成为焦点。
- [ ] **AC-FOCUS-008 — Click Empty Space**: 鼠标点击空世界空间且无目标可交互时，不发出
  `interaction_used`。验证：点击地面，无 `interaction_used` 事件。
- [ ] **AC-FOCUS-009 — Tie-Breaking**: 当两个候选目标的 `focus_score` 相等时，按作者优先级 >
  距离 > ID 字典序打破平局。验证：配置两个等属性等距离目标，焦点稳定选中 ID 靠前者。
- [ ] **AC-FOCUS-010 — Boundary Distance (D == L)**: 当 `distance_to_anchor == reach_limit`
  时，目标视为可达，允许焦点和 `Use`。验证：精确站在 `reach_limit` 边界上，目标仍可获得焦点。
- [ ] **AC-FOCUS-011 — Keyboard Cycle Navigation**: 按 Tab 键在候选目标列表中按 `focus_score`
  降序循环。验证：3 个候选目标（分数 0.8, 0.6, 0.4），按 Tab 依次选中 0.8 → 0.6 → 0.4 → 0.8。
- [ ] **AC-FOCUS-012 — Keyboard Cycle Reset on Mouse**: 键盘循环选中目标后，鼠标指向另一目标时
  循环位置重置为鼠标指向的目标。验证：Tab 选到第 2 个候选后移动鼠标到第 1 个，焦点立即切换到
  鼠标指向目标。

### Use Gate

- [ ] **AC-USE-001 — Successful Use**: 当 `use_gate = Allowed` 且玩家按下 `Use` 键时，
  `interaction_used` 事件发送给领域系统，包含正确的 `target_id`。验证：站在目标范围内按 E，
  检查事件日志包含正确的目标 ID。
- [ ] **AC-USE-002 — Too Far Block**: 当 `distance_to_anchor > reach_limit` 时，按下 `Use` 返回
  `Blocked(too_far)`，不发送 `interaction_used`。验证：在刚好范围外按 E，收到 Toast "太远了"。
- [ ] **AC-USE-003 — Blocked By Geometry**: 当目标和玩家之间有阻挡体时，按下 `Use` 返回
  `Blocked(blocked)`。验证：在有墙隔开的目标前按 E，收到 Toast "过不去"。
- [ ] **AC-USE-004 — Target Busy Block**: 当 `target_busy = true` 时，按下 `Use` 返回
  `Blocked(target_busy)`。验证：在忙碌目标前按 E，收到 Toast "稍等一下"。
- [ ] **AC-USE-005 — No Focus Block**: 当无世界焦点时，按下 `Use` 返回 `Blocked(no_focus)`，
  不显示 Toast，但玩家角色上出现短暂微闪（~0.1s）表示输入已收到。验证：在空场景中按 E，
  玩家角色微闪，无 Toast，无 `interaction_used` 事件。
- [ ] **AC-USE-006 — UI Modal Block**: 当 UI 模态面板打开时，按下 `Use` 返回
  `Blocked(ui_modal_blocked)`。验证：打开菜单后按 E，不发出 `interaction_used`。
- [ ] **AC-USE-007 — Use Lock Prevents Repeat**: 当 `world_focus_state` 为 `UseLocked` 时，
  重复按 `Use` 不发出新的 `interaction_used`。验证：在交互进行中连续按 E，事件日志只有一次
  `interaction_used`。
- [ ] **AC-USE-008 — Use Lock Timeout**: 当领域系统在 `use_lock_timeout_seconds` 内未释放锁时，
  本系统自动取消 `UseLocked`，恢复焦点和移动。验证：模拟领域系统不释放锁，2 秒后玩家恢复控制。
- [ ] **AC-USE-009 — Same-Frame Race Condition**: 当焦点目标在 `UsePending` 帧和 `Use` 执行帧
  之间被禁用或销毁时，`use_gate` 返回 `Blocked(target_disabled)`，不发送 `interaction_used`。
  验证：脚本在玩家按 E 的同帧禁用目标，系统输出 `target_disabled` 而非崩溃。
- [ ] **AC-USE-010 — Input Buffer**: 当玩家在 `input_buffer_window`（默认 0.10s）内提前按下
  `Use` 键（如在 `UseLocked` 释放前 2 帧按下），输入被缓冲并在窗口结束时重新评估。若仍有效则
  执行 `Use`。验证：在锁释放前 0.05s 按 E，锁释放后 0.05s `Use` 自动触发。
- [ ] **AC-USE-011 — Movement Blocked During UseLocked**: 当 `world_focus_state` 为 `UseLocked`
  且 `movement_state` 为 `Rooted` 时，移动输入无效果。验证：交互进行中按 WASD，角色不移动。

### Cross-System Boundaries

- [ ] **AC-BOUND-001 — No Currency Access**: 本系统代码路径不读取或写入货币变量。验证：代码审查
  确认无 currency/money/gold 引用。
- [ ] **AC-BOUND-002 — No Inventory Access**: 本系统代码路径不读取或写入库存数据。验证：代码审查
  确认无 inventory/items 引用。
- [ ] **AC-BOUND-003 — No Save Direct Write**: 本系统不直接调用存档写入 API。验证：代码审查确认
  无 save/write/persist 调用。
- [ ] **AC-BOUND-004 — Domain Consequences**: 所有 `Use` 的领域后果由领域系统执行，本系统只发送
  `interaction_used` 并等待结果。验证：对集市摊位按 E，购买逻辑不在本系统代码中。
- [ ] **AC-BOUND-005 — No Scene Transition Ownership**: 本系统不触发场景切换。验证：代码审查确认
  无场景加载/切换调用。
- [ ] **AC-BOUND-006 — Focus Cleared on Scene Transition**: 场景过渡开始时，世界焦点立即清空，
  新场景不继承旧焦点。验证：场景 A 聚焦目标后触发场景切换，场景 B 加载后 `world_focus_id = null`。
- [ ] **AC-BOUND-007 — ID Reuse Treated as New Target**: 同一稳定 ID 的对象销毁后重建时，
  旧焦点状态、粘性加成和待处理 Use 均不保留。验证：销毁后重建同 ID 目标，焦点需重新获取。

### Visual/Audio Events

- [ ] **AC-VFX-001 — Focus Changed Event**: 当焦点在目标之间切换时，发出 `interaction_focus_changed` 语义事件，
  包含正确的 `previous_focus_id` 和 `new_focus_id`。验证：在两个目标间移动，事件日志记录焦点
  变化序列。
- [ ] **AC-VFX-002 — Use Requested Event**: 当 `interaction_used` 成功发送时，发出 `interaction_used`
  语义事件。验证：按 E 使用目标，事件日志包含正确的 `target_id`。
- [ ] **AC-VFX-003 — Use Blocked Event**: 当 `use_gate = Blocked` 时，发出 `use_blocked` 语义事件，
  包含正确的 `block_reason`。验证：在范围外按 E，事件包含 `block_reason = too_far`。
- [ ] **AC-VFX-004 — Movement Blocked Event**: 当持续移动被碰撞阻挡时，按限流频率发出
  `movement_blocked` 语义事件。验证：走向墙壁，事件日志中有 `movement_blocked` 事件且频率符合限流。
- [ ] **AC-VFX-005 — Input Gate Changed Event**: 当输入门状态变化时，发出 `input_gate_changed`
  语义事件。验证：暂停/恢复时事件日志记录门状态变化。
- [ ] **AC-VFX-006 — Event Priority**: 同一帧内多个事件同时触发时，只发出优先级最高的事件。
  验证：同时触发 `use_blocked` 和 `interaction_focus_changed` 的边界场景，日志只有 `use_blocked`。

### UI Data Contract

- [ ] **AC-UI-001 — Focus Data Updates**: 当 `world_focus_id` 变化时，UI 可拉取到新的焦点数据
  （ID、display_hint、state）。验证：走近目标，UI 拉取到的焦点 ID 与场景目标匹配。
- [ ] **AC-UI-002 — Block Reason Data**: 当 `use_blocked` 发生时，UI 可拉取到最新的
  `last_block_reason`。验证：在范围外按 E 后立即查询 UI 数据，`last_block_reason = too_far`。
- [ ] **AC-UI-003 — Gate State Data**: 当壳层变化时，UI 可拉取到最新的 `input_gate_state`。
  验证：暂停游戏，UI 数据中 `input_gate_state = InputClosed`。
- [ ] **AC-UI-004 — World Focus Frozen on Modal**: 当 UI 模态打开时，`world_focus_id` 对外
  保持模态打开前的值（冻结），但 `world_focus_state` 报告为冻结态，所有 `Use` 输入阻断为
  `ui_modal_blocked`。验证：打开菜单后查询 UI 数据，`world_focus_id` 仍为进入菜单前的目标 ID，
  但按 E 返回 `Blocked(ui_modal_blocked)`。

### Desktop Focus / Recovery

> **Platform Pivot Note**: 原 Web-Specific ACs (AC-WEB-001 ~ AC-WEB-003) 已改写为桌面等价物。浏览器 BFCache、AudioContext 手势要求等不再作为 MVP 约束。

- [ ] **AC-FOCUS-001 — Window Focus Loss Stops Input**: 桌面窗口失焦/最小化时（通过壳层转发），输入门关闭，移动和使用全部阻断。验证：Alt+Tab 切走后回来，角色仍在原位。
- [ ] **AC-FOCUS-002 — Window Restore With Mouse Over Target**: 桌面窗口恢复时若鼠标正悬停在目标上，焦点仅在下一有效玩法帧刷新；恢复帧不自动执行 `Use`。验证：在目标上悬停 → 最小化窗口 → 恢复，角色不自动使用目标，焦点在恢复后重新获取。
- [ ] **AC-FOCUS-003 — Audio Activation Follows Shell**: 音频激活由平台壳层的音频门统一管理，本系统不拥有音频激活逻辑。验证：所有语义事件正常发出，无论音频是否激活。

## Open Questions

### Q1: 斜向移动是否归一化？

**问题：** 当玩家同时按下水平和垂直移动键时（如 W+D），斜向速度是否应该归一化
（`input_magnitude = 0.707`）还是保持 `input_magnitude = 1.0`？

**当前倾向：** 归一化。键盘方向键组合不应比单方向更快，否则会产生"斜向跑更快"的
系统性问题，影响关卡设计和碰撞预期。

**阻塞：** 无。公式已预留 `input_magnitude` 归一化逻辑。

**决定时机：** 灰盒测试时用两种方案试跑飞艇舱室，选手感更好的方案。

---

### Q2: 角色是否需要独立朝向？

**问题：** 角色朝向是否始终跟随移动方向，还是存在"面向一个方向但不动"的状态？
例如：玩家想面朝工作台但身体不动。

**当前倾向：** MVP 阶段朝向跟随移动方向，无移动输入时保持最后朝向。独立朝向
（右摇杆/Mouse Look 控制朝向但不移动）属于 gamepad/双摇杆功能，不在 MVP 范围。

**下游影响：** 交互范围是否需要朝向检查（如"必须面朝目标才能交互"）。当前设计
使用圆形触达范围（无朝向要求），所以此问题不阻塞核心交互。

**决定时机：** MVP 之前确认；若保持朝向跟随，则交互公式无需修改。

---

### Q3: 移动速度在飞艇内部 vs 外部是否需要区分？

**问题：** 飞艇内部（舱室、甲板）属于"家"空间，走动节奏应比空港/探索点更从容。
是否需要为不同场景类型预设不同的 `base_move_speed`？

**当前倾向：** 不预设场景级速度差异。飞艇内部通过更窄的舱室空间、更密的交互点
和目标分布自然产生从容节奏，而不是通过降低速度来强制慢行。`base_move_speed` 保持
全局一致，场景差异由关卡设计（空间尺度、阻挡、锚点密度）实现。

**下游影响：** 若后期发现飞艇内部走动太快，可通过调参中的 `base_move_speed` 全局
调整，或为飞艇场景覆盖 `base_move_speed` 配置。

**决定时机：** 灰盒飞艇 Hub 后决定。

---

### Q4: 场景过渡期间的角色位置如何处理？

**问题：** 当玩家在场景 A（如空港）移动到门口触发场景 B（如飞艇内部）时，角色在
新场景中的初始位置由谁决定？本系统是否提供"从上一个场景的出口位置推导下一个场景
的入口位置"？

**当前倾向：** 本系统不拥有场景过渡逻辑。场景过渡系统应在加载新场景后，根据
`上一场景 ID + 出口 ID` 查询预配置的入口生成点，直接设置角色初始位置。本系统
在新场景中从 Idle 状态开始运行。

**需要协调：** 场景过渡系统（尚未设计）需要在生成点配置中引用本系统的移动边界。

**决定时机：** 场景过渡 GDD 设计时联合确认。

---

### Q5: `movement_velocity` 公式中的 `collision_multiplier` 是二值还是渐进值？

**问题：** 当前 `collision_multiplier` 定义为 `0-1` 范围，但在公式描述中写的是
"完全阻断为 0，可走为 1"。是否允许中间值（如沿墙滑动的部分速度 0.7）？

**当前倾向：** MVP 阶段使用二值（0 或 1），不做沿墙滑动或斜坡速度修正。
Godot 内置 `move_and_slide()` 已提供基础的沿墙滑动行为；若启用，`collision_multiplier`
的实际输出由物理引擎的剩余速度决定，但本系统不主动计算渐进值。

**决定时机：** Godot 物理集成测试时确认，与灰盒碰撞数据一同验证。

---

### Q6: 是否需要为调试目的暴露焦点候选池？

**问题：** 开发阶段需要可视化焦点候选池（所有候选目标的分数、距离、可达性），
以调校焦点权重和滞回参数。这个调试功能是否应该在 MVP 发布版本中保留（隐藏入口）？

**当前倾向：** 调试面板仅在 dev build 中编译（通过 feature flag），发布版本完全移除。
焦点调试数据不消耗额外性能（复用候选池查询结果）。

**决定时机：** 实现开始前确认调试面板的编译条件。
