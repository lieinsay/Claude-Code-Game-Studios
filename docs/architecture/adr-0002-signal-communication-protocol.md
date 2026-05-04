# ADR-0002: 基于 Signal 的跨系统通信协议

## Status
Proposed

## Date
2026-05-04

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Signal 通信协议 |
| **Knowledge Risk** | LOW — Godot signal 系统自 4.0 起稳定，跨版本无 breaking changes |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/deprecated-apis.md`, `docs/engine-reference/godot/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None — signal 核心 API (`signal` keyword, `.connect()`, `.emit()`) 自 4.0 起未变 |
| **Verified Non-Issues** | Variadic args (4.5): signal 声明不支持 `...` — 不影响本 ADR; `call_deferred` emit (4.x): 存在但被本 ADR 明确禁止作为跨系统通信机制 |
| **Verification Required** | Signal 连接数剖面（确保无内存泄漏）；signal 级联深度 ≤ 2 跳静态检查；signal 参数数量 ≤ 6；smoke emit 测试确保 emitter/receiver 类型签名匹配 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload 清单、启动顺序 — 所有 signal 的 producer/consumer 身份由 ADR-0001 确定) |
| **Enables** | ADR-0004 (InteractionHandler 信号契约), ADR-0005 (资源池信号契约), ADR-0007~0012 (所有 Core/Feature 系统信号定义) |
| **Blocks** | 所有涉及跨系统交互的故事 — 必须先确定信号契约才能实现任何 producer/consumer |
| **Ordering Note** | 应在 ADR-0001 之后、ADR-0004/0005 之前 Accepted |

## Context

### Problem Statement

ADR-0001 确定了 9 Autoload + 4 Scene 的架构，并规定所有跨层通信使用 Godot signal。但没有定义统一的 signal 命名规范、payload 格式、错误通知模式、连接管理策略，以及「读查询（直接方法调用）vs 状态变更（signal）」的明确边界。没有这些标准，每个系统会自行发明通信模式，导致不一致的 API 和调试困难。

### Constraints

- **Godot 4.6.2**: 使用原生 `signal` 关键字声明 typed 参数；`signal.emit(args)` 同步调用；`signal.connect(callable)` 建立连接
- **Web 单线程**: 所有 signal emit 同步执行（当前帧内逐个调用所有 connected callables）；无并发安全问题，但长链需控制深度
- **架构原则 1**: "State mutations and significant events cross layer boundaries via Godot signals. Read-only state queries may use direct method calls on the owning system's public API"
- **架构原则 2**: "Domain Owns State, Not Infrastructure" — signal payload 不能携带可变对象引用
- **ADR-0001**: fire-and-forget 语义已确立；启动信号链已定义命名模式；`cross_phase_direct_call` 和 `boot_sequencer_god_object` 已禁止
- **GDScript 约束**: signal 参数声明不支持默认值；signal 参数数量硬上限 6 个

### Requirements

- 统一的 signal 命名规范，从信号名即可识别 producer 和事件类型
- 所有跨系统 signal 必须有 typed 参数列表，IDE 可自动补全和重构
- 必须有明确的错误/拒绝通知模式
- Signal 连接必须在可预测的时机建立（不依赖运行时动态连接）
- Signal payload 不得携带 Node 引用、Resource 引用或可变对象引用
- Signal 级联深度不超过 2 跳（A→B→C 允许；A→B→C→D 禁止）

## Decision

### 1. 命名规范

所有跨系统 signal 遵循：**`{名词}_{动词过去时}`**

| 元素 | 规则 | 示例 |
|------|------|------|
| 名词 | 系统或实体名（snake_case） | `resource`, `route`, `hull_band` |
| 动词 | 过去时，描述已发生的事件 | `deposited`, `committed`, `changed` |
| 失败对 | 成功用过去时，失败用 `_failed` 后缀 | `deposit_committed` / `deposit_failed` |

此规范与 Godot 内置 signal 风格一致：`pressed`, `timeout`, `finished`, `tree_entered`。

**Signal 命名不得与继承链上的内置 signal 重名**。例如，`Control` 子类不能声明 `signal pressed(...)` —— 这会遮蔽内置 `Control.pressed`，导致 UI 交互异常。

### 2. Payload 格式

**规则**: 所有 signal 使用 Godot typed 参数列表。所有参数必须显式声明类型。

```gdscript
# ✅ 正确 — typed 参数，显式类型
signal resource_deposited(node_id: String, amount: int)
signal hull_band_changed(band_id: int, new_integrity: float)

# ❌ 禁止 — Dictionary 包裹
signal resource_deposited(data: Dictionary)

# ❌ 禁止 — 无类型参数
signal foo(data)

# ❌ 禁止 — 可变对象引用
signal focus_changed(target: Node)
signal exploration_started(context: EncounterContext)  # Resource 引用
```

**Payload 内容规则**:
- 允许: `String`, `int`, `float`, `bool`, `StringName`, `Vector2`, `Vector2i`, 以及 typed `Array[Primitive]`
- 禁止: `Node`, `Resource`, `Object`, `Callable`, `Dictionary`（除非作为 `_failed` 的 diagnostics，且为只读快照）
- 实体用 `String` ID 引用（如 `node_id: String`, `route_id: String`）
- 复杂上下文通过 ID 查询对应 Autoload 的公共读 API 获取，不通过 signal payload 传递

**Signal 参数无默认值** — GDScript 不支持 signal 参数默认值语法。emit 必须提供全部声明参数。可选信息使用空值哨兵（`""`, `-1`, `0`）。

### 3. 读查询 vs 状态变更

```
┌──────────────────────────────────────────────────────────────┐
│                    跨系统通信决策树                            │
│                                                              │
│  需要跨系统交互？                                              │
│       │                                                      │
│       ├── 读取另一个系统的状态？                               │
│       │       └── ✅ 直接方法调用 on 公共读 API                │
│       │           示例: Intel.query_knowledge_state(entity_id) │
│       │                 Chart.get_route_state(route_id)       │
│       │                                                      │
│       ├── 请求另一个系统执行操作（可能失败）？                   │
│       │       └── ✅ 直接方法调用，返回 Result                 │
│       │           示例: Resources.commit_deposit(id, resources)│
│       │                 → 返回 DepositResult                  │
│       │           操作成功后由执行系统发射 signal               │
│       │                                                      │
│       └── 通知另一个系统"某事已发生"？                          │
│               └── ✅ Signal (fire-and-forget)                 │
│                   示例: repair_completed(node_id)             │
│                        route_committed(route_id)              │
└──────────────────────────────────────────────────────────────┘
```

**关键区分**:
- **查询/请求** → 有返回值，调用方需要结果才能继续 → 直接方法调用
- **通知** → 无返回值，发送方不关心谁在听 → Signal

### 4. 成功/失败 Signal 对

所有可能失败的操作，由执行系统发射成对 signal：

```gdscript
# 资源提交
signal deposit_committed(node_id: String)
signal deposit_failed(node_id: String, reason: String)

# 航线选择
signal route_committed(route_id: String)
signal route_selection_failed(route_id: String, reason: String)

# 存档操作
signal save_completed(slot: int)
signal save_failed(slot: int, reason: String)
```

**规则**:
- `xxx_failed` 的 `reason` 为机器可读的错误码（`String`），如 `"insufficient_funds"`, `"slot_full"`, `"target_busy"`
- 失败 signal 的参数签名必须与成功 signal 共享第一个参数（操作目标 ID），加上 `reason: String`
- 消费者监听成功 signal 用于正向流程；监听失败 signal 用于 UI 提示/日志

### 5. Signal 连接管理

**所有跨系统 signal 连接在启动阶段统一建立。**

| 阶段 | 谁连接 | 连接什么 |
|------|--------|---------|
| Phase 3a | Resources, Intel | Persistence 的 save/load 信号 |
| Phase 3b | Chart | Intel 的 `knowledge_changed` |
| Phase 4 | WorldRepair | Resources 的 `deposit_committed`, `deposit_failed` |
| Phase 4 | InteractionRegistry | 无（等待 Scene 注册） |
| Phase 5 | AirshipHub | InteractionRegistry 的 `interaction_used` |
| Phase 6 | UIManager | Resources, Intel, Chart, Modules 的域信号（用于 HUD dirty-flag） |
| Phase 7 | FeedbackManager | 所有语义事件 signal |

**连接语法**: 使用 `sender.signal_name.connect(receiver.method_name)`，不使用字符串 `connect("signal_name", ...)`（deprecated since 4.0）。

```gdscript
# ✅ 正确 — Signal.connect() 类型安全 API
Resources.deposit_committed.connect(_on_deposit_committed)
Resources.deposit_failed.connect(_on_deposit_failed)

# ❌ 禁止 — 字符串连接
Resources.connect("deposit_committed", _on_deposit_committed)
```

**规则**:
- 禁止在 `_process()` / `_physics_process()` 中动态 connect/disconnect
- 禁止条件连接（"只在 X 状态下连接 Y signal"）——始终连接，在 handler 内部做状态门禁
- Scene 的 signal 连接在其实例化时建立、`queue_free()` 时由 Godot 自动断开
- 一次性连接可使用 `CONNECT_ONE_SHOT` 标志

### 6. Signal 发射规则

**所有跨系统 signal 使用同步 `emit()`，禁止异步变体。**

```gdscript
# ✅ 正确 — 同步 emit
deposit_committed.emit("repair_node_01")

# ❌ 禁止 — 延迟 emit（打破执行顺序可预测性）
deposit_committed.emit.call_deferred("repair_node_01")
```

`signal.emit()` 在当前帧同步遍历所有 connected callables，按连接顺序依次执行。这保证了执行顺序可预测、可调试。

### 7. Signal 级联深度限制

```
✅ 允许 — 深度 ≤ 2:
  Hub.departure_initiated → Chart.route_committed → Navigation (VoyageManager)

✅ 允许 — 扇出 (深度 1):
  Repair.repair_completed ─┬→ Intel.on_repair_completed
                           ├→ Chart.on_route_enhanced
                           ├→ Settlement (NPC/stock refresh)
                           └→ FeedbackManager.emit_feedback

❌ 禁止 — 深度 ≥ 3:
  A → B → C → D
  改为: A 直接 signal 到 B/C/D，或 B 持有 C/D 的直接引用
```

### 8. 跨系统 Signal 目录

以下为所有跨系统 signal 的完整注册表。

**Foundation → Core / Feature**:

| Signal | Producer | Consumers | Payload |
|--------|----------|-----------|---------|
| `save_completed` | Persistence | SessionShell, UIManager | `slot: int` |
| `save_failed` | Persistence | SessionShell, UIManager | `slot: int, reason: String` |
| `load_completed` | Persistence | SessionShell, Resources, Intel, Chart, WorldRepair, Hub | `slot: int` |
| `load_failed` | Persistence | SessionShell | `slot: int, reason: String` |
| `interaction_focus_changed` | InteractionRegistry | UIManager | `focused_id: String` |
| `interaction_used` | InteractionRegistry | AirshipHub, Settlement, ExplorationScene | `focused_id: String` |

**Core Systems**:

| Signal | Producer | Consumers | Payload |
|--------|----------|-----------|---------|
| `deposit_committed` | Resources | WorldRepair, UIManager | `node_id: String` |
| `deposit_failed` | Resources | WorldRepair, UIManager | `node_id: String, reason: String` |
| `pool_changed` | Resources | UIManager | `pool_id: int` |
| `knowledge_changed` | Intel | Chart, UIManager | `entity_id: String, new_state: int` |
| `ability_unlocked` | Intel | UIManager, FeedbackManager | `ability_id: String` |
| `hub_state_changed` | AirshipHub | Partner, UIManager | `new_state: int` |
| `departure_initiated` | AirshipHub | Chart, UIManager | `mode: int` |
| `hull_band_changed` | ModuleManager (#8) | UIManager, VoyageManager | `band_id: int, new_integrity: float` |
| `module_installed` | ModuleManager (#8) | AirshipHub, UIManager | `slot_id: int, module_id: String` |
| `route_committed` | Chart | VoyageManager, UIManager | `route_id: String` |
| `route_selection_failed` | Chart | UIManager | `route_id: String, reason: String` |
| `route_enhanced` | Chart | UIManager, FeedbackManager | `route_id: String, enhancement_id: String` |

**Feature Systems**:

| Signal | Producer | Consumers | Payload |
|--------|----------|-----------|---------|
| `encounter_triggered` | VoyageManager (#10) | ExplorationScene | `encounter_id: String` |
| `voyage_completed` | VoyageManager (#10) | UIManager, AirshipHub, FeedbackManager | `route_id: String` |
| `voyage_aborted` | VoyageManager (#10) | UIManager, AirshipHub | `reason: String` |
| `exploration_phase_changed` | ExplorationScene (#11) | UIManager | `phase: int` |
| `threat_detected` | ExplorationScene (#11) | ThreatResolver (#12) | `threat_id: String` |
| `extraction_completed` | ExplorationScene (#11) | Resources, AirshipHub, UIManager | `anchor_id: String` |
| `threat_resolved` | ThreatResolver (#12) | ModuleManager (#8), UIManager, FeedbackManager | `result_type: int, band_id: int, damage: float` |
| `repair_completed` | WorldRepair (#13) | Intel, Chart, Settlement, FeedbackManager | `node_id: String` |
| `deposit_accepted` | WorldRepair (#13) | UIManager | `node_id: String, deposited_amount: int` |
| `deposit_rejected` | WorldRepair (#13) | UIManager | `node_id: String, reason: String` |
| `purchase_completed` | Settlement (#14) | Resources, UIManager | `stall_id: String, good_id: String, quantity: int` |
| `purchase_failed` | Settlement (#14) | UIManager | `stall_id: String, reason: String` |
| `settlement_state_changed` | Settlement (#14) | UIManager | `settlement_id: String, new_state: int` |
| `partner_state_changed` | Partner (#15) | UIManager, AirshipHub | `new_state: int` |
| `partner_named` | Partner (#15) | UIManager, FeedbackManager | `name: String` |

**Presentation**:

| Signal | Producer | Consumers | Payload |
|--------|----------|-----------|---------|
| `screen_changed` | UIManager | SessionShell | `screen_id: int` |
| `modal_pushed` | UIManager | SessionShell | `modal_id: int` |
| `modal_popped` | UIManager | SessionShell | — |

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    SIGNAL FLOW — GAME LOOP                        │
│                                                                   │
│  session_ready                                                    │
│      │                                                            │
│      ▼                                                            │
│  Hub ──departure_initiated(mode)──▶ Chart                        │
│                                        │                          │
│                          route_committed(route_id)                │
│                                        │                          │
│                                        ▼                          │
│                              VoyageManager (#10)                  │
│                                        │                          │
│                        encounter_triggered(encounter_id)          │
│                                        │                          │
│                    ┌───────────────────┤                          │
│                    ▼                   ▼                          │
│            ExplorationScene     voyage_completed                  │
│            (#11)                voyage_aborted                    │
│                │                   │                              │
│    threat_detected(threat_id)      ▼                              │
│                │              AirshipHub.welcome_back()           │
│                ▼                                                  │
│         ThreatResolver (#12)                                      │
│                │                                                  │
│         threat_resolved(result_type, band_id, damage)             │
│                │                                                  │
│                ▼                                                  │
│         ModuleManager (#8)  ←── 船体损伤                          │
│                                                                   │
│  exploration 中:                                                  │
│    extraction_completed → Resources.add_to_pool()                 │
│                        → AirshipHub.welcome_back()               │
│                                                                   │
│  Hub 中:                                                          │
│    Resources.commit_deposit(node_id, resources)                   │
│        → 成功: deposit_committed.emit(node_id)                    │
│        → 失败: deposit_failed.emit(node_id, "insufficient")       │
│        → WorldRepair.deposit_materials()                          │
│            → repair_completed.emit(node_id)                       │
│                ├── Intel.on_repair_completed()                    │
│                ├── Chart.route_enhanced.emit(route_id)            │
│                ├── Settlement (NPC/stock)                         │
│                └── FeedbackManager.emit_feedback()                │
└──────────────────────────────────────────────────────────────────┘

COMMUNICATION PATTERNS:

  Read Query (direct call):
    System A ──get_state(id)──▶ System B ──return value──▶ System A

  Action Request (direct call → signal on result):
    System A ──commit_deposit(args)──▶ System B
                                         │ (validates, executes)
                                         │
                               success: emit deposit_committed
                               failure: emit deposit_failed

  Notification (signal fire-and-forget):
    System A ──emit repair_completed(id)──▶ System B
                                            System C
                                            System D
```

### Key Interfaces

```gdscript
# === 信号声明模板 ===
# 每个 Autoload 在 _ready() 中只声明信号（不连接）

# 成功信号
signal [noun]_[verb_past]([target_id]: String, [additional typed params])

# 失败信号 — 签名与成功信号共享第一个参数
signal [noun]_[verb]_failed([target_id]: String, reason: String)

# === 信号连接模板 ===
# 在对应的 on_[phase]_ready() 中建立连接
# 语法: sender.signal_name.connect(receiver.method_name)

func on_core_ready() -> void:
    Resources.deposit_committed.connect(_on_deposit_committed)
    Resources.deposit_failed.connect(_on_deposit_failed)

func _on_deposit_committed(node_id: String) -> void:
    pass

func _on_deposit_failed(node_id: String, reason: String) -> void:
    UIManager.show_toast("提交失败: " + reason, 3.0)

# === 读查询模板 (直接方法调用，非 signal) ===

func query_knowledge_state(entity_id: String) -> int:
    return _knowledge_states.get(entity_id, KNOWLEDGE_UNREVEALED)

func get_route_state(route_id: String) -> RouteState:
    return _route_states[route_id]
```

## Alternatives Considered

### Alternative A: EventBus 中心化

- **Description**: 所有 signal 通过一个中央 `EventBus` Autoload 路由。系统调用 `EventBus.emit("event_name", data)`；消费者通过 `EventBus.on("event_name", callable)` 订阅
- **Pros**: 集中调试；运行时动态订阅灵活；producer 不知道 consumer
- **Cons**: 失去 Godot typed signal 的类型安全；字符串事件名易拼写错误；`EventBus` 成为 God Object 和单点故障；无法用 IDE 重构/查找引用；Dictionary payload 失去编译期检查
- **Rejection Reason**: 牺牲 Godot 原生的类型安全 signal 系统，换来一个不可类型检查的字符串路由层。Godot signal 本身已提供松耦合，无需外挂事件总线

### Alternative B: 直接方法调用 + 返回值

- **Description**: 所有跨系统交互都用直接方法调用，被调用方返回 Result 类型。不使用 signal 进行状态变更通知
- **Pros**: 调用链可追踪；有返回值；调试时堆栈清晰
- **Cons**: 调用方必须知道被调用方的存在和 API → 紧耦合；扇出困难（一个事件通知 4 个系统需要连续调用 4 次）；违反 fire-and-forget 语义
- **Rejection Reason**: 违反架构原则 1。读查询使用直接方法调用是正确的；但状态变更通知必须用 signal 实现扇出和松耦合

### Alternative C: Signal 仅成功 + 返回值处理失败

- **Description**: 操作失败不发射 signal，由调用方检查直接方法调用的返回值。Signal 只在操作成功时发射
- **Pros**: 减少 signal 数量；失败处理在调用点，上下文更丰富
- **Cons**: "操作"和"通知"耦合在一起——如果 A 请求 B 执行操作且 B 执行中需要通知 C/D/E（扇出），C/D/E 也需要知道失败。这在 WorldRepair 场景中很明显：`commit_deposit` 的失败需要同时通知调用方和 UIManager
- **Rejection Reason**: 操作结果和状态变更通知的消费者不完全重叠。成对 signal 允许消费者按需订阅成功或失败，避免调用方承担转发失败通知的责任

## Consequences

### Positive

- **类型安全**: 所有 signal 有 typed 参数，IDE 可自动补全、重构、查找引用。编译器 + 运行时双重类型校验
- **可调试**: Signal 命名规范使栈追踪可读；成对 xxx_completed/xxx_failed 使事件流完整；同步 `emit()` 保证执行顺序可预测
- **松耦合**: Producer 不需要知道 consumer 的存在（emit 不依赖 connect）；新增 consumer 只需连接 signal，不修改 producer
- **可测试**: 单元测试中连接 test callable 到 signal，验证 emit 的参数正确性；无需 mock 整个事件总线
- **Web 安全**: 无 Dictionary payload 意味着无意外引用传递；ID 查询模式保证数据所有权清晰

### Negative

- **Signal 数量多**: 34 个跨系统 signal 需要维护目录。每个新增操作需要声明 1-2 个 signal
- **间接调试**: 相比直接方法调用，signal 链的调试需要追踪多个 emit 点。需要在连接点添加日志
- **启动连接成本**: 所有 signal 在启动时统一连接，增加启动阶段的初始化代码量
- **命名强制**: 团队必须遵守命名规范，code review 需要检查 signal 命名

### Risks

- **Risk**: Signal 目录与实际实现不同步（添加了 signal 但未更新此 ADR 的目录表）
  - **Mitigation**: `/architecture-review` Phase 4 会交叉检查 signal 注册表与实际代码。Story 实现时必须引用此 ADR 中的 signal 名称
- **Risk**: Signal 级联深度超过 2 跳导致调试困难
  - **Mitigation**: `/code-review` 静态检查 signal 连接链深度。级联超限的实现不得合并
- **Risk**: 开发者使用 Dictionary payload "因为更方便"
  - **Mitigation**: 本 ADR 声明 `dictionary_signal_payload` 和 `untyped_signal_param` 为 forbidden patterns；code review 强制执行；所有 signal 参数必须显式类型
- **Risk**: Signal 参数类型不匹配在编译期静默通过，运行时 emit 才暴露
  - **Mitigation**: 启动 smoke test 遍历所有 signal 并 emit 一次哨兵值，验证类型匹配；单元测试覆盖所有 signal payload 类型

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| #1 content-data-state-registry | 下游系统只能通过稳定 ID 引用内容定义 | Signal payload 中的实体强制使用 String ID，禁止传递对象引用 |
| #4 player-movement-interaction | Use 分发到正确的领域处理器，领域系统可以接受/拒绝 | `interaction_used` signal + 领域系统通过读查询 API 返回 Result；拒绝用 `interaction_rejected` |
| #5 resources-goods-capacity | commit_deposit 原子操作 — 全部成功或全部回滚 | `deposit_committed` / `deposit_failed` 成对 signal |
| #13 world-repair-unlock | repair_completed 跨系统触发 4 个下游系统 | `repair_completed` signal 扇出到 Intel, Chart, Settlement, FeedbackManager（深度 1，扇出 4） |

## Performance Implications

- **CPU**: Signal emit 在当前帧同步遍历所有 connected callables — O(N) where N = consumer count。典型 consumer count ≤ 5，单次 emit < 0.01ms。34 个 signal 的 emit 频率由游戏事件驱动（非 per-frame），总 CPU 开销可忽略
- **Memory**: 每个 signal 连接约 50 bytes（slot + map entry overhead）。~200 连接 × 50 bytes ≈ 10KB 额外内存。可忽略
- **Load Time**: 启动时建立 ~200 个 signal 连接，总计 < 1ms

## Migration Plan

项目尚无代码，此为初始标准。实现时：

1. 在每个 Autoload 的 GDScript 中声明本文档列出的 signal（typed 参数，无默认值）
2. 在对应 `on_[phase]_ready()` 中建立连接，使用 `sender.signal_name.connect(receiver.method)` 语法
3. 每次新增跨系统 signal 时更新本文档的 Signal 目录表
4. Code review 检查：signal 命名是否符合过去时规范、payload 是否 typed、是否包含对象引用、是否使用同步 `emit()`

## Validation Criteria

- 所有 34 个 signal 在 GDScript 中使用 `signal` 关键字声明，显式 typed 参数
- 所有 signal 连接在启动阶段完成（Phase 3-7），不在 `_process()` 中动态连接
- Signal 名全部遵循 `{名词}_{动词过去时}` 规范，无内置 signal 名称冲突
- 无 signal payload 包含 `Node`, `Resource`, `Object`, `Callable` 或裸 `Dictionary`
- 所有 signal emit 使用同步 `.emit()`，无 `.emit.call_deferred()`
- Signal 参数数量 ≤ 6，全部声明参数在 emit 时提供
- Signal 级联深度 ≤ 2 跳
- 所有 `xxx_failed` signal 的 `reason` 参数为 `String` 类型、机器可读的错误码
- 启动 smoke test 覆盖所有 signal 连接的类型验证

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构与启动顺序 — 定义 producer/consumer 身份和连接时机
- **ADR-0004**: InteractionHandler @abstract 基类 — 定义 `interaction_used` / `interaction_rejected` 的具体 payload
- **ADR-0005**: Resource Pool Architecture — 定义 `deposit_committed` / `deposit_failed` 的具体错误码
- **Master Architecture**: `docs/architecture/architecture.md` — Signal contract rules (L312-315) 和 Core loop signal flow (L289-309)
