# ADR-0004: InteractionHandler @abstract 基类与 Use 入口分发

## Status
Proposed

## Date
2026-05-04

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Input / Interaction |
| **Knowledge Risk** | MEDIUM — `@abstract` 装饰器自 4.5 引入 (post-cutoff)；Signal 核心稳定 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md` |
| **Post-Cutoff APIs Used** | `@abstract` 装饰器 (4.5) — `Interactable` 基类使用 `@abstract` 标记 `handle_use()` 方法；`StringName` 作为稳定 ID 类型 (4.x stable) |
| **Verification Required** | `@abstract` 在 GDScript 4.6.2 中的运行时错误行为验证（未实现抽象方法的 scene 实例化是否抛出）；`Area2D.mouse_entered`/`mouse_exited` 在 Web 导出中的事件可靠性；`PhysicsRayQueryParameters2D` 射线检测在兼容性渲染器下的精度 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (InteractionRegistry 为 Autoload #3，Phase 4 初始化)；ADR-0002 (Signal 通信协议 — `interaction_focus_changed`、`interaction_used` 信号契约) |
| **Enables** | ADR-0005 (资源池 — Resources 仓库/货架交互)，ADR-0009 (航图 — Hub 舱门/舵轮交互)，ADR-0010 (航行 — 探索触发)，ADR-0013 (世界修复 — 修复节点交互)，ADR-0014 (聚落 — 集市摊位交互) |
| **Blocks** | 所有包含可交互对象的场景实现 — Hub 站点、探索锚点、市集摊位、修复节点 |
| **Ordering Note** | 应在 ADR-0001/0002 之后 Accepted；在实现任何可交互场景之前 Accepted |

## Context

### Problem Statement

《云海织航》有 10+ 种可交互对象分布在 Hub、探索点、空港市集和修复节点中。每种交互的领域后果不同（购买 vs 修复 vs 情报查询 vs 模块安装），但所有交互共享相同的焦点获取、可达性检查和 Use 入口流程。GDD #4 `player-movement-interaction` 定义了 `Interactable` 基类契约和 Use Request Dispatch Pattern，但需要正式的 ADR 决定：`Interactable` 的具体类层次结构、`InteractionRegistry` 如何管理注册/分发、domain handler 如何接受或拒绝 Use 请求、以及如何利用 Godot 4.5 的 `@abstract` 强制子类实现契约方法。

### Constraints

- **Godot 4.6.2**: `@abstract` 装饰器 (4.5+) 可在 GDScript 中标记类和方法的抽象性；`StringName` 用于稳定 ID 比较（性能优于 String）
- **ADR-0001**: `InteractionRegistry` 为 Autoload #3 (#4 玩家移动与交互)。这是系统 #4 唯一的 Autoload — 同时承担 GDD #4 中 `PlayerMovementInteraction` 的职责（输入门、焦点状态机、Use 分发）。`Player` 是 `CharacterBody2D` Scene，由各场景实例化而非 Autoload。
- **ADR-0002**: 状态变更通知使用 typed signal，禁止 Dictionary payload；读查询使用直接方法调用
- **GDD #4**: 定义了 `Interactable` 的 6 个字段、焦点评分公式、Use Gate 公式和 Use Request Dispatch Pattern（signal 通知 + method call 分发）
- **Web 单线程**: 所有焦点查询和 Use Gate 判断在 `_physics_process` 中同步执行
- **MVP 输入**: 仅键盘+鼠标，无 gamepad、touch、指针锁、拖拽或长按

### Requirements

- 所有可交互对象必须继承 `Interactable` 基类并实现 `handle_use()` 方法
- `InteractionRegistry` 管理注册/注销 + 焦点状态机 + Use Gate + 分发
- Use 分发模式: signal（即发即忘通知）+ 直接方法调用（请求-响应）
- 领域系统必须返回 `UseResult`（accepted/rejected/busy）
- 焦点得分公式必须支持鼠标指向优先级 + 滞回稳定性
- 必须支持键盘 Tab 焦点循环（纯键盘无障碍）

## Decision

### 1. Interactable @abstract 基类

选择单一 `@abstract` 基类设计: `Interactable extends Node2D`，使用 Godot 4.5 的 `@abstract` 装饰器强制执行子类契约。

```gdscript
# === Interactable — 所有可交互对象的抽象基类 ===
# 位置: src/core/interaction/interactable.gd
# 继承: Node2D (提供世界空间位置)
# 使用 @abstract 装饰器 (Godot 4.5+)

@icon("res://assets/icons/interactable.svg")
class_name Interactable
extends Node2D

# --- 导出字段 (场景作者在编辑器中配置) ---

@export var interaction_id: StringName
# 稳定交互 ID，由内容注册表分配
# 格式: "{scene}.interactable.{name}" （如 "hub.interactable.intel_station"）

@export var anchor_radius: float = 0.45
# 交互锚点半径 (Godot 米)，与 player_interaction_radius 相加得到 reach_limit

@export var priority: float = 0.5
# 作者配置的交互优先级 (0.0–1.0)，用于焦点评分公式

@export var interaction_type: StringName
# 交互类型: &"talk" / &"use" / &"trade" / &"repair" / &"open" / &"read" / &"rest"
# 用于 UI 提示 ("按 [E] 交谈" vs "按 [E] 使用")

# --- 子类必须重写的方法 ---

@abstract
func handle_use(player_id: StringName) -> UseResult:
    # 领域系统处理 Use 请求的核心入口
    # 必须返回 UseResult（accepted / rejected / busy）
    # 子类必须在返回 accepted 前执行领域逻辑或触发领域系统调用
    pass

# --- 子类可重写的方法 (有默认实现) ---

func is_enabled() -> bool:
    return true
    # 目标当前是否可交互。返回 false 时：
    # - 焦点获取: 该目标不进入候选池
    # - Use Gate: Blocked(target_disabled)

func is_busy() -> bool:
    return false
    # 目标是否正在处理另一个交互。返回 true 时：
    # - Use Gate: Blocked(target_busy)

func get_display_hint() -> String:
    return ""
    # 简短显示名 (≤12 字符)，用于焦点指示器
    # 返回空字符串时 UI 使用 interaction_id 的 humanized 形式

func get_anchor_position() -> Vector2:
    return global_position
    # 交互锚点的世界坐标
    # 默认使用节点的全局位置；子类可重写自定义锚点偏移

# --- 内置功能 (不可重写) ---

func get_stable_id() -> StringName:
    return interaction_id
    # 与内容注册表交互的标准接口

# --- UseResult 枚举 ---
enum UseResult {
    ACCEPTED,   # 交互被接受，进入 UseLocked；领域系统负责释放
    REJECTED,   # 交互被拒绝，输出 block_reason
    BUSY,       # 目标忙碌，稍后重试
}
```

**设计原理**:
- `extends Node2D` 而非 `Node`：可交互对象在场景中有空间位置，`Node2D.global_position` 为默认锚点
- `@abstract` 仅标记 `handle_use()`：这是唯一必须由领域系统实现的变体行为。其余方法（`is_enabled`、`is_busy`、`get_display_hint`）有合理默认值
- `@export` 字段支持编辑器内配置：场景作者可直接在 Inspector 中设置稳定 ID、锚点半径和优先级
- `StringName` 用于稳定 ID：`StringName` 比较是 O(1) pointer compare（Godot 内部 interning），比 `String` 的 O(n) 字符比较更快

**`@abstract` 运行时行为** (Godot 4.6.2):
- 尝试实例化未实现所有 `@abstract` 方法的类 → 引擎报错，scene 实例化失败
- 开发期间的安全网：遗漏 `handle_use()` 实现的子类在场景加载时立即暴露，不会在运行时静默失败
- 编辑器内：未实现抽象方法的 scene 在 Inspector 中显示警告

### 2. InteractionRegistry (Autoload #3) 职责

ADR-0001 已确定 `InteractionRegistry` 为 Autoload #3。本 ADR 明确其完整职责范围：

| 职责 | 描述 | 边界 |
|------|------|------|
| Target Registration | `register(target: Interactable)` / `unregister(target: Interactable)` | 目标注册自身；Registry 维护候选池 |
| Focus State Machine | 运行 GDD #4 的 5 状态焦点机 (NoFocus→Candidate→Focused→UsePending→UseLocked) | Registry 拥有状态机，不拥有领域后果 |
| Focus Score Calculation | 实现 GDD #4 的 `focus_score` 和 `focus_selection` 公式 | 权重可配置，公式固定 |
| Input Gate | 接收 SessionShell 的 `input_gate_open`/`input_gate_closed` 信号 | 壳层拥有门禁判定，Registry 消费 |
| Use Gate | 实现 GDD #4 的 `use_gate` 公式 | 验证但不执行领域后果 |
| Use Dispatch | `interaction_used` signal (fire-and-forget) + `handle_use()` method call (request-response) | Registry 分发，领域系统执行 |
| Keyboard Cycling | Tab 键焦点循环 | 仅无鼠标时激活 |

**职责不包括**:
- 玩家移动（由 `Player` CharacterBody2D Scene 拥有）
- 领域后果（购买、修复、模块安装等）
- 场景 UI 打开（由 `use_requested` → 领域系统 → 领域系统自行调用 UIManager）
- 浏览器生命周期事件（由 SessionShell 拥有）

### 3. Use Request Dispatch Pattern

遵循 ADR-0002 的 "读查询=直接调用 / 状态变更=signal" 边界:

```
玩家按下 Use 键 (E / Space)
       │
       ▼
InteractionRegistry._on_use_pressed()
       │
       ▼
use_gate 检查:
  ├── Blocked → emit use_blocked(reason) → 反馈系统消费
  │   └── 返回 Blocked(reason)
  │
  └── Allowed →
       │
       ├── 1. Signal (fire-and-forget, 反馈系统消费):
       │      interaction_used.emit(target_id: StringName, interaction_type: StringName)
       │      → FeedbackManager 播放确认视觉/音频
       │
       ├── 2. Method Call (request-response, 领域系统消费):
       │      result = target.handle_use(player_id)
       │      → ACCEPTED: 进入 UseLocked，等待领域系统释放
       │      → REJECTED: 恢复焦点，输出 block_reason
       │      → BUSY: 保持焦点，输出 target_busy
       │
       └── 焦点状态: Focused → UsePending → UseLocked (if ACCEPTED)
```

**Signal vs Method Call 分离的理由**:
- `interaction_used` signal 是 fire-and-forget — 反馈系统不需要返回值，也不需要知道领域结果
- `handle_use()` method call 是 request-response — 领域系统必须同步返回 UseResult
- Signal 参数: `target_id: StringName`, `interaction_type: StringName` — 满足 ADR-0002 typed params 要求，无 Dictionary

### 4. 注册/注销生命周期

```
Scene 实例化:
  scene._ready()
    → 实例化所有 Interactable 子节点
    → 每个 Interactable._ready():
        InteractionRegistry.register(self)
        InteractionRegistry 的候选池 += 此目标

Scene 销毁:
  scene.exit_cleanup()  ← ADR-0001 场景退出协议
    → 每个 Interactable 仍存活时:
        InteractionRegistry.unregister(self)
    → InteractionRegistry 的候选池 -= 此目标
    → scene.queue_free()  ← Godot 自动断开所有信号连接
```

**注销时机约束**:
- `unregister()` 必须在 `queue_free()` 之前调用 — 防止 Use Gate 引用已释放对象
- Scene exit cleanup 中必须先清空焦点（若当前焦点属于此 Scene），再注销目标
- 场景过渡期间（exit_cleanup → 新 scene _ready()）InteractionRegistry 候选池暂时为空 → 焦点自动进入 NoFocus

### 5. InteractionRegistry 公共 API

```gdscript
# === InteractionRegistry (Autoload #3) — 公共接口 ===

# --- 目标注册 (领域系统不直接调用，由 Interactable._ready() 调用) ---
func register(target: Interactable) -> void
func unregister(target: Interactable) -> void

# --- 读查询 (直接方法调用) ---
func query_focus_state() -> Dictionary:
    # 返回焦点状态快照，供 UIManager 在每帧或变化时拉取
    #
    # Dictionary 键约定 (typed payload — ADR-0002 精神):
    #   "world_focus_id": StringName     # 当前焦点目标 ID，无焦点时为空
    #   "display_hint": String           # 焦点目标显示名 (≤12 字符)
    #   "focus_state": StringName        # 当前焦点状态机状态名称
    #   "last_block_reason": StringName  # 最后一次 Use Gate 阻断原因，无阻断时为空
    #   "candidate_count": int           # 当前候选池目标数
    #   "is_input_open": bool            # 输入门是否开启
    #
    # 注意: 这是读查询（直接方法调用），非 signal。Dictionary 仅在此方法返回，
    # 不通过 signal 传递。UIManager 每次轮询后解包为本地类型变量。
    pass

func query_candidate_pool() -> Array[Interactable]:
    # 返回当前候选池中的所有目标 (调试用)
    pass

# --- 信号 (fire-and-forget 通知) ---
signal interaction_focus_changed(focused_id: StringName)
# 消费者: UIManager — 更新焦点指示器
# ADR-0002 已注册

signal interaction_used(target_id: StringName, interaction_type: StringName)
# 消费者: FeedbackManager — 播放确认反馈
# 注意: 这是反馈通知，不是领域分发 (领域分发通过 handle_use() 直接调用)
# ADR-0002 已注册，payload 已更新为 typed params
# 
# ⭐ Supersession: 此 ADR 取代 ADR-0002 中 interaction_used 的定义。
# Signal 缩小为 fire-and-forget 反馈通知 (consumer: FeedbackManager)。
# Domain systems 消费 Use 请求通过 handle_use() method call，不通过此 signal。

signal use_blocked(target_id: StringName, block_reason: StringName)
# 消费者: FeedbackManager, UIManager — 播放阻断反馈 + 显示 Toast
# block_reason: &"input_closed" / &"ui_modal_blocked" / &"no_focus" /
#               &"target_disabled" / &"blocked" / &"too_far" / &"target_busy"
```

### 6. 焦点评分公式 (来自 GDD #4)

```
focus_score = clamp(
    0.45 * pointer_score +      # 鼠标明确指向
    0.25 * proximity_score +    # 距离越近分越高
    0.15 * priority_score +     # 作者配置优先级
    0.15 * stickiness_score,    # 当前焦点黏性
    0, 1
)

focus_selection = argmax(focus_score_i + current_focus_bonus_i)
    where current_focus_bonus_i = focus_stickiness_bonus (default 0.08)
          if i == current_focus_target AND current_focus_valid
          else 0

Tie-breaking: higher priority → shorter distance → stable ID lexicographic order
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                 INTERACTION ARCHITECTURE                              │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────┐        │
│  │           InteractionRegistry (Autoload #3)              │        │
│  │                                                          │        │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │        │
│  │  │ Input Gate   │  │ Focus State  │  │ Use Gate     │   │        │
│  │  │ (SessionShell│  │ Machine      │  │ (reachability│   │        │
│  │  │  signals)    │  │ 5 states     │  │  checks)     │   │        │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │        │
│  │                                                          │        │
│  │  ┌──────────────────────────────────────────────────┐   │        │
│  │  │  Candidate Pool                                   │   │        │
│  │  │  Array[Interactable] — registered targets         │   │        │
│  │  │  queried per physics frame (≤8 candidates)        │   │        │
│  │  └──────────────────────────────────────────────────┘   │        │
│  │                                                          │        │
│  │  Signals:                                                │        │
│  │    interaction_focus_changed(focused_id: StringName)     │        │
│  │    interaction_used(target_id: StringName,               │        │
│  │                     interaction_type: StringName)         │        │
│  │    use_blocked(target_id: StringName,                    │        │
│  │                block_reason: StringName)                  │        │
│  └────────┬──────────┬──────────────┬──────────────────────┘        │
│           │          │              │                                │
│           ▼          ▼              ▼                                │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────────────────┐        │
│  │UIManager │ │Feedback  │ │  Domain Systems               │        │
│  │(焦点UI)  │ │Manager   │ │  handle_use() → UseResult     │        │
│  │          │ │(反馈表现) │ │                              │        │
│  └──────────┘ └──────────┘ │  ┌──────────────────────┐    │        │
│                            │  │ AirshipHub           │    │        │
│  Scene Tree:               │  │ 舱门/舵轮/情报台      │    │        │
│                            │  │ handle_use() →       │    │        │
│  ┌────────────────────┐    │  │   ACCEPTED           │    │        │
│  │ Scene Root         │    │  └──────────────────────┘    │        │
│  │  ├ Interactable A  │    │                              │        │
│  │  │  (情报台)        │    │  ┌──────────────────────┐    │        │
│  │  ├ Interactable B  │    │  │ ExplorationScene      │    │        │
│  │  │  (货架)          │    │  │ 搜索点/撤离锚点       │    │        │
│  │  ├ Interactable C  │    │  │ handle_use() →       │    │        │
│  │  │  (舱门)          │    │  │   ACCEPTED           │    │        │
│  │  └ ...             │    │  └──────────────────────┘    │        │
│  └────────────────────┘    │                              │        │
│                            │  ┌──────────────────────┐    │        │
│  Each Interactable:        │  │ Settlement            │    │        │
│    register(self) on _ready│  │ 摊位/NPC              │    │        │
│    unregister(self) before │  │ handle_use() →       │    │        │
│      queue_free()          │  │   ACCEPTED           │    │        │
│                            │  └──────────────────────┘    │        │
│                            │                              │        │
│                            │  ┌──────────────────────┐    │        │
│                            │  │ WorldRepair           │    │        │
│                            │  │ 修复节点              │    │        │
│                            │  │ handle_use() →       │    │        │
│                            │  │   ACCEPTED           │    │        │
│                            │  └──────────────────────┘    │        │
│                            └──────────────────────────────┘        │
└──────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```gdscript
# === Interactable 子类实现示例 (Hub 情报台) ===

class_name IntelStation
extends Interactable

func _ready() -> void:
    interaction_id = &"hub.interactable.intel_station"
    anchor_radius = 0.55
    priority = 0.8
    interaction_type = &"read"

func handle_use(player_id: StringName) -> Interactable.UseResult:
    # 委派给领域系统
    if not Intel.is_available():
        return UseResult.REJECTED
    # 领域系统执行查询并打开 UI
    Intel.open_intel_screen()
    return UseResult.ACCEPTED

func is_enabled() -> bool:
    return Intel.is_available()

func get_display_hint() -> String:
    return "情报台"

# === InteractionRegistry 核心逻辑 (简化的焦点刷新) ===

func _physics_process(_delta: float) -> void:
    if input_gate != INPUT_OPEN:
        return
    _refresh_candidate_pool()
    _update_focus_selection()

func _refresh_candidate_pool() -> void:
    # 局部空间查询 — 使用 Area2D.overlapping_areas 而非全场景扫描
    # 候选目标 ≤ max_focus_candidates_per_query (8)
    # 排除: 未启用、被遮挡 (path_clear=false)、距离超出 reach_limit
    pass

func _update_focus_selection() -> void:
    # 计算每个候选的 focus_score
    # 应用 current_focus_bonus
    # argmax → 唯一焦点
    # emit interaction_focus_changed 仅在焦点变化时
    pass
```

## Alternatives Considered

### Alternative A: Interface/Component 模式

- **Description**: 每个可交互对象持有一个 `InteractionComponent` 子节点，`InteractionRegistry` 通过 `get_node("InteractionComponent")` 查找
- **Pros**: 非 `Node2D` 对象也可以拥有交互行为；松耦合
- **Cons**: 多一层间接引用；`get_node()` 是字符串路径查找，性能差；无法享受 `@abstract` 的编译期检查；子节点生命周期管理增加复杂度
- **Rejection Reason**: GDScript 的 `@abstract` 和 `class_name` 提供了直接的类型安全契约。Component 模式增加了间接层但未提供额外收益。所有可交互对象在当前设计中均为 `Node2D`（有空间位置），单一继承足够

### Alternative B: String-based Dispatch

- **Description**: `InteractionRegistry` 通过 `interaction_type` 字段字符串匹配路由到领域系统。如 `"trade"` → `MarketSystem.handle_interaction()`
- **Pros**: 无需抽象基类；新增交互类型只需添加字符串映射
- **Cons**: 失去类型安全 — 字符串拼写错误在运行时才暴露；无法用 IDE 查找 `handle_use()` 的所有实现；领域系统 API 风格不统一；`InteractionRegistry` 需要知道所有领域系统的 API → 成为 God Object
- **Rejection Reason**: 违反 `boot_sequencer_god_object` 禁止模式。InteractionRegistry 不应知道每个领域系统的处理 API。`@abstract Interactable.handle_use()` 让每个子类自行委派给正确的领域系统，保持 InteractionRegistry 的职责单一

### Alternative C: Signal-Only Dispatch

- **Description**: `interaction_used` signal 直接携带所有信息；领域系统自行连接 signal 并判断是否属于自身管辖
- **Pros**: 完全松耦合；InteractionRegistry 不需要知道领域系统的存在
- **Cons**: 多个领域系统同时收到 signal 但只有一个应响应 → 需要额外的 "claimed" 机制防止重复处理；Signal 无法返回 `UseResult`（即发即忘）；时序不可控 — 无法保证领域系统先于反馈系统处理
- **Rejection Reason**: Use 分发需要返回值（accepted/rejected/busy），signal 不提供返回值语义。ADR-0002 明确规定此类操作用直接方法调用。Hybrid 模式（signal 通知 + method call 分发）已采纳 — signal 用于反馈通知，method call 用于领域分发

## Consequences

### Positive

- **类型安全**: `@abstract` 强制所有子类实现 `handle_use()`，IDE 可查找所有实现和重构
- **统一入口**: 所有交互经过同一个 Use Gate → 单一位置检查可达性、Focus、输入门
- **注册中心松耦合**: InteractionRegistry 只知道 `Interactable` 接口，不知道具体领域系统 API
- **领域自治**: 每个 `Interactable` 子类自行委派给领域系统，InteractionRegistry 不成为 God Object
- **可测试**: `handle_use()` 可直接调用验证 UseResult；焦点公式可独立单元测试
- **可扩展**: 新增交互类型只需创建新 `Interactable` 子类，无需修改 InteractionRegistry

### Negative

- **单 Autoload 职责重**: InteractionRegistry 同时承担注册中心、焦点机、Use Gate、输入门 — 虽不涉及领域逻辑，但代码量较大
- **@abstract 运行时检查依赖引擎**: 未实现 `handle_use()` 的错误在场景加载时捕获，不在编辑期
- **每 Physics Frame 焦点刷新**: 候选池查询和焦点评分在每物理帧运行（即使焦点未变化），需要 profile 确认 CPU 开销可接受

### Risks

- **Risk**: InteractionRegistry 随时间推移吸收过多职责，演变为 Interaction God Object
  - **Mitigation**: 此 ADR 明确列出不应属于 Registry 的职责（移动、领域后果、UI 打开、生命周期）；code review 强制执行
- **Risk**: `@abstract` 在 Godot 4.6.2 中的行为与 4.5 文档不一致
  - **Mitigation**: 验证项明确要求测试 `@abstract` 运行时行为；若引擎有 bug，定义 `assert(false, "must implement handle_use()")` fallback
- **Risk**: 场景过渡时 Interactable 注销和焦点清空的时序竞态
  - **Mitigation**: Scene exit cleanup 协议（ADR-0001）中显式添加 "unregister all Interactables before queue_free()" 步骤
- **Risk**: 多个 Interactable 的 `handle_use()` 在同一帧被重复调用（Use 键按住不放）
  - **Mitigation**: UseLocked 状态阻止重复 Use；`_on_use_pressed()` 中使用 `Input.is_action_just_pressed()` 而非 `is_action_pressed()`

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| #4 player-movement-interaction | Interactable 基类契约: 6 字段 + is_enabled/get_anchor_position/is_busy/get_display_hint | `Interactable` @abstract 基类定义所有字段 + 方法签名 |
| #4 player-movement-interaction | Use Request Dispatch Pattern: signal + method call 双通道 | `interaction_used` signal (fire-and-forget) + `handle_use()` method call (request-response) |
| #4 player-movement-interaction | 焦点分离: UI Control 焦点与 World 焦点不共享 | InteractionRegistry 只管理世界焦点；UI 焦点由 UIManager 管理 |
| #4 player-movement-interaction | 焦点评分公式: 0.45×指针 + 0.25×距离 + 0.15×优先级 + 0.15×黏性 | `focus_score` 公式直接实现在 InteractionRegistry 中 |
| #4 player-movement-interaction | 键盘 Tab 焦点循环 | `focus_cycle_next` 逻辑实现在 InteractionRegistry 中 |
| #4 player-movement-interaction | 所有可交互对象使用稳定 ID (StringName) | `interaction_id: StringName` 字段 |
| #4 player-movement-interaction | 交互目标注册/注销 | `register(target: Interactable)` / `unregister(target: Interactable)` |
| #7 airship-hub | 10 MVP 站点注册为 Interactable | Hub Scene 每个站点实例化对应 Interactable 子类 |

## Performance Implications

- **CPU**: 焦点刷新每 Physics Frame (60Hz) — 候选池查询 O(N) where N ≤ 8。`focus_score` 计算每个候选 ~10 次浮点运算。单帧 < 0.05ms — 可忽略
- **Memory**: 候选池 Array[Interactable] ≤ 8 个引用。Interactable 基类 ~200 bytes/instance。10–20 个活跃 Interactable → ~4KB — 可忽略
- **Load Time**: 每个 Interactable._ready() 调用 register() → 追加到候选池数组。每个 < 1μs — 可忽略
- **Network**: 无 — 所有交互处理为本地

## Migration Plan

项目尚无代码。实现顺序：

1. 创建 `src/core/interaction/interactable.gd` — `Interactable` @abstract 基类 + `UseResult` 枚举
2. 创建 `InteractionRegistry` Autoload 骨架 — 最小 `_ready()` + 信号声明（遵循 ADR-0001）
3. 在 Phase 4 的 `InteractionRegistry.on_core_ready()` 中完成初始化
4. 实现候选池管理 — `register()` / `unregister()` / `_refresh_candidate_pool()`
5. 实现焦点状态机 — 5 状态 + 评分公式 + 滞回
6. 实现 Use Gate + Use Locked timeout
7. 为每个 Hub 站点创建 `Interactable` 子类 (IntelStation, CargoShelf, ModuleSlotA, ModuleSlotB, StorageRack, CargoArea, HatchDoor, Helm, RestPoint, RepairPoint)
8. 在 Scenario（探索点、空港市集、修复节点）中实例化并注册对应的 Interactable 子类

## Validation Criteria

- `Interactable` 基类使用 `@abstract` 标记 `handle_use()` — 未实现的子类在场景加载时报错
- 所有可交互对象继承 `Interactable`，不绕过基类直接 emit signal
- `interaction_used` signal 使用 typed params `(target_id: StringName, interaction_type: StringName)` — 无 Dictionary
- `handle_use()` 返回 `UseResult` 枚举 — 不接受返回 void 或 bool 的实现
- 同一时刻 `InteractionRegistry.candidate_pool` 中只有一个世界焦点
- 焦点切换有滞回 — 当前焦点在 `retain_margin` 内不被新候选抢走
- Tab 键在候选池中循环焦点（纯键盘可用）
- Scene exit cleanup 中所有 Interactable 在 `queue_free()` 前调用 `unregister()`
- `handle_use()` 的 ACCEPTED 响应进入 UseLocked；重复按 Use 键不产生新的 `handle_use()` 调用
- UseLocked timeout (2s) 后自动恢复焦点和移动

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — InteractionRegistry 的 Autoload 位置 (#3) 和 Phase 4 初始化
- **ADR-0002**: Signal 通信协议 — `interaction_focus_changed`、`interaction_used` signal 的 typed params 契约
- **GDD #4**: `design/gdd/player-movement-interaction.md` — 完整的焦点状态机、评分公式、Use Gate 和语义事件规范
- **GDD #7**: `design/gdd/airship-hub.md` — Hub 10 站点的 Interactable 清单
