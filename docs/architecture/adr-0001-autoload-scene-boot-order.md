# ADR-0001: Autoload/Scene 架构与启动顺序

## Status
Proposed

## Date
2026-05-04

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Autoload/SceneTree 架构 |
| **Knowledge Risk** | HIGH — 此版本远超 LLM 训练截止日期 (May 2025) |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md` |
| **Post-Cutoff APIs Used** | `@abstract` 装饰器 (4.5) — InteractionHandler 基类; `NavigationServer2D` (4.5) — #4 导航; Dual-focus 系统 (4.6) — #16 UI 输入路由 |
| **Verification Required** | Web 导出下 9 Autoload 启动时间剖面; Dual-focus 与 4 层输入路由交互验证; `add_child()` 场景叠加内存剖面; 如果启动 >2s 则 WorldRepair 降级为延迟 Autoload |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0002 (Signal 通信协议), ADR-0004 (InteractionHandler), ADR-0006 (Web 平台约束) |
| **Blocks** | 所有后续 ADR 的实现故事 — 必须先确定 Autoload 清单和启动顺序才能编写任何系统的故事 |
| **Ordering Note** | 必须作为第一个 ADR 被 Accepted，其他 ADR 引用此 ADR 中的 Autoload 名称和初始化阶段 |

## Context

### Problem Statement

《云海织航》有 18 个系统（4 Foundation + 5 Core + 7 Feature + 2 Presentation），需要在 Godot 4.6.2 Web-first 环境中确定哪些系统作为 Autoload 常驻内存、哪些作为 Scene 按需实例化，以及它们的初始化顺序如何保证依赖关系正确。错误的 Autoload 划分会导致 Web 启动时间过长或运行时状态丢失；错误的初始化顺序会导致系统在依赖就绪前被调用。

### Constraints

- **Web-first**: 单线程执行，无 `Thread` 假设；所有 Autoload `_ready()` 串行执行，直接影响首帧时间
- **Godot 4.6.2**: Autoload 按 `project.godot` 中声明顺序加载，`_ready()` 按 SceneTree 顺序自顶向下调用。主场景在所有 Autoload 初始化完成后才加载
- **兼容性渲染器**: WebGL 2 约束，无 `Compositor` 后处理
- **内存**: 浏览器标签页内存有限（通常 200MB–1GB），常驻 Autoload 需控制内存占用
- **MVP 范围**: 16 系统 MVP + 2 系统 Vertical Slice（#17 Feedback, #18 Onboarding）
- **架构原则**: 信号驱动跨层通信，域系统拥有自身状态，基础设施只做管道不做水库

### Requirements

- 必须支持 Foundation → Core → Feature → Presentation 的分层初始化顺序
- Autoload 必须在 `_ready()` 中不做重 I/O，避免阻塞首帧
- Feature Scene（Exploration, Settlement, VoyageManager）必须按需创建、用后销毁
- 启动链必须可暂停、可展示加载进度、可在阶段失败时安全停止
- 必须兼容 Web 浏览器 AudioContext 用户手势激活、标签页冻结/恢复

## Decision

### 1. Autoload/Scene 划分标准

每个系统的归属依据以下规则：

| 特性 | Autoload | Scene（常驻子节点） | Scene（按需） |
|------|----------|---------------------|-------------------|
| 跨场景状态访问 | 是 — 始终可用 | 仅当父节点处于活动状态时 | 否 — 销毁时状态丢失 |
| 空间节点（2D 碰撞器等） | 否 — 无场景树位置 | 是 — 属于空间 | 是 |
| 信号消费者计数 | >= 3 跨层消费者 | 1-2 消费者，共置 | 仅限场景内消费者 |
| 状态生命周期 | 永久（游戏长度） | 由场景驻留决定 | 临时（单次场景使用） |

### 2. Autoload 与 Scene 划分

**9 个 Autoload**（始终常驻，按 `project.godot` 声明顺序加载）：

| 顺序 | Autoload 名称 | 所属系统 | 层级 | 划分理由 |
|------|-------------|----------|------|----------|
| 1 | `Registry` | #1 内容数据与状态注册表 | Foundation | 静态内容唯一目录 — 所有系统的基础依赖 |
| 2 | `Persistence` | #3 本地存档与世界状态持久化 | Foundation | 存档编排 — 状态生命周期 > 任何单个场景 |
| 3 | `InteractionRegistry` | #4 玩家移动与交互 | Foundation | 可交互对象注册中心 — 跨所有场景 |
| 4 | `Resources` | #5 资源、货物与容量 | Core | 6 资源池 — 跨 Hub/Exploration/Settlement 共享 |
| 5 | `Intel` | #6 玩家知识与情报 | Core | 知识状态 — 3+ 消费者 (#9, #13, #11, #15) |
| 6 | `Chart` | #9 航图与航线规划 | Core | 航线状态 — 3+ 消费者；在 Hub↔Exploration 往返间保持 |
| 7 | `WorldRepair` | #13 世界修复与解锁 | Feature | 见下方 "WorldRepair 例外论证" |
| 8 | `UIManager` | #16 UI / HUD / 航图界面 | Presentation | 12 屏幕管理 — 覆盖所有场景，始终在最顶层 |
| 9 | `FeedbackManager` | #17 反馈、特效与音频语义 | Presentation | 语义事件中心 — 所有系统发出事件 (VS) |

**WorldRepair 例外论证** — Feature 层唯一的 Autoload：
- `repair_completed` 信号被 4 个跨层系统消费 (#6 Intel, #9 Chart, #14 Settlement, #17 Feedback)
- 修复进度是游戏长度的、不可逆的（known→repaired 单行道），不同于临时的探索/战斗状态
- Settlement 按需创建——如果 WorldRepair 不是 Autoload，Settlement 实例化后将错过先前的 repair_completed 事件
- 对比其他 Feature 系统：
  - #10 航行: Scene — 状态仅在航行期间有效
  - #11 探索: Scene — 状态对于每次探索运行都是临时的
  - #12 战斗: Scene 子节点 — 状态对于每次威胁都是临时的（但结果通过信号传递给 #8）
  - **#13 修复: Autoload** — 永久世界状态，4 跨层信号消费者
  - #14 聚落: Scene — 仅在访问时相关

**4 个 Scene**（按需实例化）：

| Scene | 所属系统 | 层级 | 生命周期 | 实例化时机 | 销毁时机 |
|-------|----------|------|----------|-----------|---------|
| `AirshipHub` | #7 飞艇家园 Hub | Core | **常驻子节点** | Phase 5 启动时，或从 Exploration/Settlement 返回时重新显示 | 仅在游戏退出时销毁；Exploration 期间隐藏但不移除 |
| `VoyageManager` | #10 航行与路线风险 | Feature | 按需 | `route_committed` 信号触发后 | 航行完成或中止后 |
| `ExplorationScene` | #11 探索 / 搜撤场景 | Feature | 按需 | `encounter_triggered` 信号触发后 | 撤离完成、返回 Hub 后 |
| `Settlement` | #14 空港 / 村镇状态与集市交易 | Feature | 按需 | 玩家在 Hub 选择访问空港时 | 返回 Hub 后 |

**依附于 Scene 的系统**：
- `#8 飞艇模块与船体状态` — 作为 `AirshipHub` 的 `ModuleManager` 子节点。然而 `HullState` 数据由 #8 领域系统拥有（独立于场景树），ModuleManager 节点反映并变异该状态
- `#12 战斗与威胁处理` — 作为 `ExplorationScene` 的 `ThreatResolver` 子节点。威胁结果通过信号传递给 #8 影响船体（Hub 常驻 → 信号线始终活跃）
- `#15 伙伴功能与关系` — 作为 `AirshipHub` 的 `Partner` 子节点。`PartnerState` 数据由 #15 领域系统拥有，Partner 节点反映该状态
- `#18 新手引导与首轮闭环` — 跨系统编排，作为 `OnboardingManager` Autoload（VS 阶段添加，不在 MVP Foundation ADR 范围）

**特殊处理**：
- `#2 平台与会话壳` — 不作为独立 Autoload。`SessionShell` 作为**主场景根节点**（`Project Settings → Application → Run → Main Scene`），在所有 Autoload 初始化完成后加载。其职责：会话生命周期状态机、壳层 overlay、加载/错误/暂停界面

### 3. SessionShell（主场景根节点）与 UIManager（Autoload）的边界

| 职责 | SessionShell | UIManager |
|------|-------------|-----------|
| 平台级状态 (15 states) | 拥有 | 读取 |
| 壳层 overlay（加载、错误、暂停） | 拥有 — 始终渲染在最顶层 | 不管理 |
| 游戏内 UI（12 屏幕、模态栈、HUD） | 不管理 | 拥有 |
| 输入路由（4-layer priority） | 不管理 | 拥有 |
| AudioContext 激活 | 拥有 | 不管理 |
| 标签页焦点恢复 | 拥有 | 响应（通过信号） |
| Toast/非关键消息 | 调用 `UIManager.show_toast()` | 实现 |

**Overlay 优先级规则**：
1. SessionShell 覆盖层（FatalError 最高, Pause, Loading）始终渲染在 UIManager 屏幕栈之上
2. 当 SessionShell 覆盖层处于活动状态时，UIManager 的 4 层输入路由被抑制
3. SessionShell 调用 `UIManager.show_toast()` 处理非关键消息，但拥有自己的关键覆盖层

### 4. Autoload 最小 `_ready()` 契约

每个 Autoload 的 `_ready()` 只做：
- 常量初始化（字典、数组、默认值）
- 信号声明
- null 安全检查

**绝对不在 `_ready()` 中做**：
- 文件 I/O、资源加载
- 场景实例化
- 调用其他 Autoload 的方法（除非该 Autoload 声明顺序在本 Autoload 之前）
- 播放音频
- 启动协程或 Timer

真正的初始化通过信号链延迟执行。初始化完成后，每个 Autoload 发出对应就绪信号。

### 5. 启动顺序 —— 信号链异步启动

Godot 实际启动时序：
1. 引擎初始化 → 按 `project.godot` 声明顺序加载全部 9 个 Autoload → 调用各 `_ready()`
2. 加载主场景（`SessionShell.tscn`）→ `SessionShell._ready()` 可以安全使用所有 Autoload
3. `SessionShell._ready()` → emit `boot_requested` → 信号链接管

```
Phase 0 — 平台探测:
  SessionShell.on_boot_requested()
    → 探测 Web 存储能力（IndexedDB 可用性）
    → AudioContext 解锁延迟到首次用户手势
    → emit platform_ready(storage_capability)

Phase 1 — 静态内容加载 (FatalError 域):
  Registry.on_platform_ready()
    → 按内容域加载静态定义（资源、模块、航线、修复节点等 12 种）
    → validate_all() → 失败则 emit boot_fatal("registry_corrupt", diagnostics)
    → emit registry_ready()
  ⚠ 此阶段失败为致命错误 — 静态内容损坏不可恢复，展示 "游戏文件损坏，请刷新页面"

Phase 2 — 存档检查 (RecoverableError 域):
  Persistence.on_registry_ready()
    → 检查可用 save slots
    → 主 slot 校验 → 失败则尝试备用 slot
    → 判定 continue_availability (Enabled / PreservedLocked / Hidden)
    → 全部 slot 损坏 → emit boot_recoverable("save_corrupt", "存档数据已损坏。开始新游戏？")
    → emit persistence_ready(continue_state)

Phase 3a — Core 数据并行初始化 (Resources + Intel 无相互依赖):
  Resources.on_persistence_ready() → 初始化 6 资源池（from save or defaults）
  Intel.on_persistence_ready()      → 初始化知识状态（from save or defaults）
  → 两者就绪后 emit core_data_ready()

Phase 3b — Core 依赖初始化 (Chart 依赖 Intel):
  Chart.on_core_data_ready()
    → 查询 Intel 获取初始航线可见性/selectability 状态
    → 连接 Intel.knowledge_changed 信号
    → emit core_ready()

Phase 4 — Feature Autoload 初始化:
  WorldRepair.on_core_ready()         → 连接 Resources.deposit_committed, Intel.on_repair_completed
  InteractionRegistry.on_core_ready() → 就绪，等待场景注册可交互对象
  → emit foundation_ready()

Phase 5 — Scene 实例化:
  SessionShell.on_foundation_ready()
    → 实例化 AirshipHub (add_child 到根节点)
    → Hub._ready() → 初始化 10 站台、伙伴节点、ModuleManager
    → emit hub_ready()

Phase 6 — Presentation 初始化:
  UIManager.on_hub_ready()
    → 初始化 12 屏幕、模态栈
    → 连接 HUD dirty-flag 到域信号
    → emit ui_ready()

Phase 7 — 入口:
  FeedbackManager.on_ui_ready()   → 订阅语义事件 (VS)
  SessionShell.on_ui_ready()
    → 判定 continue_state:
      - Enabled → Title Screen (Start + Continue)
      - PreservedLocked → Title Screen (Start + Continue[锁定，展示原因])
      - Hidden → Title Screen (Start only)
    → emit session_ready()

Phase 8 — 玩家进入:
  用户点击 Start 或 Continue（同一手势内完成 AudioContext 解锁）
    → Start: 进入新游戏 → OnboardingManager 接管 (VS)
    → Continue: Persistence.load() → 分发快照到各领域系统 → 进入 Hub
```

**启动失败分类**：

| 失败类别 | 适用 Phase | 用户界面 | 可用操作 |
|----------|-----------|---------|---------|
| **FatalError** | Phase 1 (Registry 损坏)、引擎初始化失败 | "游戏文件已损坏。请刷新页面或重新安装。" | 退出（无重试按钮） |
| **RecoverableError** | Phase 2 (存档损坏)、Phase 3-8 (瞬态错误) | Phase 2: "存档数据已损坏。开始新游戏？" Phase 3-8: 特定上下文错误 | 重试（最多 3 次）、备用路径、开始新游戏 |

**启动序列器约束** — 防止 God Object：
- 启动序列器（SessionShell）仅管理信号发射顺序，不直接调用领域系统的初始化方法
- 初始化信号携带快照数据作为 payload（如 `persistence_ready` 携带 `continue_state`），各系统自行消费
- 序列器按顺序发射信号；各系统在自己信号处理中自主初始化
- 序列器不得访问领域系统内部状态或绕过公共 API

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                     SceneTree Root                               │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  SessionShell (Main Scene Root Node)                        │  │
│  │  会话生命周期: boot → title → loading → playing → paused → error  │
│  │  Overlay 层 (FatalError / Pause / Loading) — 最高优先级       │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Active Scene Layer (add_child / remove_child)              │  │
│  │  ┌──────────┐  ┌──────────────────┐  ┌──────────┐         │  │
│  │  │AirshipHub│  │ExplorationScene  │  │Settlement│         │  │
│  │  │(常驻)    │  │(按需创建/销毁)    │  │(按需创建/销毁)│      │  │
│  │  │ ├ModuleMgr│  │ ├ThreatResolver │  │          │         │  │
│  │  │ └Partner │  │ └...             │  │          │         │  │
│  │  └──────────┘  └──────────────────┘  └──────────┘         │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  UIManager Layer (Autoload, 始终在最顶层)                    │  │
│  │  ┌──────────────────────────────────────────────────────┐  │  │
│  │  │  12 Screens + Modal Stack + Toast + HUD              │  │  │
│  │  └──────────────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Autoloads (invisible, always present)                     │  │
│  │  [1] Registry  [2] Persistence  [3] InteractionRegistry    │  │
│  │  [4] Resources  [5] Intel  [6] Chart                       │  │
│  │  [7] WorldRepair  [8] UIManager  [9] FeedbackManager       │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```gdscript
# === SessionShell (主场景根节点) ===
# 不变量: 同一时刻只有一个 phase 活跃；phase 转换不可逆
# 约束: 启动序列器只管理信号发射顺序，不直接调用领域系统的初始化方法

signal boot_requested()
signal platform_ready(storage_capability: int)
signal registry_ready()
signal persistence_ready(continue_state: int)
signal core_data_ready()
signal core_ready()
signal foundation_ready()
signal hub_ready()
signal ui_ready()
signal session_ready()

signal boot_fatal(reason: String, diagnostics: Dictionary)
signal boot_recoverable(reason: String, user_message: String, retry_count: int)

func get_session_state() -> int
func request_pause() -> void
func request_resume() -> void

# === 每个 Autoload 的最小 _ready() 契约 ===
# _ready() 只做同步轻量操作 —— 常量赋值、信号声明、null 初始化
# 禁止: 文件 I/O、场景实例化、调用其他 Autoload 方法、播放音频、启动协程

# 示例 —— Registry Autoload:
#   _ready(): 初始化空字典、声明信号
#   on_platform_ready(): 加载静态内容、运行 validate_all()

# === 场景切换协议 ===
# 所有场景切换必须遵循以下步骤:

# 旧场景退出:
#   1. 显式 stop() 所有运行中的 Tween
#   2. 取消所有活跃的 await 协程
#   3. 对旧场景根 Control 节点调用 release_focus()
#   4. 断开外部信号连接
#   5. remove_child(old_scene)
#   6. old_scene.queue_free()

# 新场景进入:
#   1. 实例化新场景
#   2. add_child(new_scene)
#   3. 等待 new_scene._ready() 完成
#   4. 对新场景默认 Control 调用 grab_focus()

# === Scene 生命周期接口 ===
# AirshipHub (常驻 Scene):
func prepare_for_departure() -> void   # 隐藏 Hub，准备航行
func welcome_back() -> void           # 从 Exploration/Settlement 返回时恢复

# ExplorationScene / Settlement (按需 Scene):
#   实例化 → add_child(scene) → scene._ready()
#   完成 → 退出清理 → remove_child(scene) → scene.queue_free()

# === Overlay 优先级合约 ===
# SessionShell overlay 始终在 UIManager screen stack 之上
# 当 SessionShell overlay 活跃时，UIManager 的 4 层输入路由被抑制
# SessionShell 调用 UIManager.show_toast() 处理非关键消息
# SessionShell 拥有自己的关键 overlay（FatalError, Pause, Loading）

# === 常驻场景保证 ===
# AirshipHub 在正常游戏期间永远不会从场景树中移除（仅在游戏退出时销毁）
# ModuleManager (#8) 和 Partner (#15) 作为 Hub 子节点，状态随 Hub 驻留
# 子节点的领域状态通过自有序列化器保存/加载，不依赖父节点的节点树
# 信号线在场景节点被遮挡时保持活跃（Godot 的信号系统不依赖可见性）
```

## Alternatives Considered

### Alternative A: 全 Autoload 模式

- **Description**: 所有 18 系统注册为 Autoload，始终在内存中
- **Pros**: 实现简单，无需管理 Scene 生命周期；所有系统随时可用
- **Cons**: Web 首帧加载 18 个 Autoload 全部 `_ready()` → 启动时间不可接受（估 >5s on slow connections）；Feature Scene（Exploration 4-zone radial 50×35 units）的 Node 子树在不需要时占用内存
- **Rejection Reason**: 违反 Web-first 启动性能约束。Feature 系统（#10–#15）只在特定游戏阶段需要，不应常驻

### Alternative B: 全 Scene 模式

- **Description**: 所有系统均为 Scene，通过一个 Root Autoload 管理生命周期
- **Pros**: 最小 Autoload 内存占用；启动极快（只需加载 Root）
- **Cons**: Root Autoload 成为 God Object —— 需要知道所有系统的创建时机和依赖关系；跨系统状态共享（资源池、知识状态、航线状态）需要在 Scene 之间手动传递，容易出现数据不一致；信号连接在 Scene 切换时容易断裂
- **Rejection Reason**: 违反 "Domain Owns State, Not Infrastructure" 原则。Core 层系统（#5 Resources, #6 Intel, #9 Chart）的状态在多个 Feature 之间共享，Autoload 保证状态在 Scene 切换时不丢失

### Alternative C: 按需 Autoload（运行时动态注册）

- **Description**: 运行时通过 `SceneTree.root.add_child()` 动态注册/移除 Autoload
- **Pros**: 理论上最优 —— 只在需要时加载，不需要时卸载
- **Cons**: Godot 4.x 的 Autoload 系统设计为静态配置。`EditorPlugin.add_autoload_singleton()` 和 `remove_autoload_singleton()` 是编辑器专用 API，在导出的游戏中不可用。运行时添加的节点可通过 `get_node("/root/Name")` 访问，但不受 Autoload 生命周期保护；移除时可能导致其他系统的信号引用悬空
- **Rejection Reason**: 运行时动态 Autoload 管理没有官方 API 支持。混合模式（固定 Autoload + 按需 Scene 的 `add_child()/remove_child()`）是官方推荐的运行时场景管理方式，已足够满足需求

## Consequences

### Positive

- **明确的分层加载**: Foundation Autoload → Core Autoload → Scene → UI，每层就绪后才进入下一层，依赖关系由显式信号链保证
- **可控启动时间**: Autoload `_ready()` 最小化；重 I/O 延迟到信号链，可展示加载进度
- **Scene 干净隔离**: Feature Scene 按需创建、用后 `queue_free()`，内存占用按需增长
- **Web 兼容**: SessionShell 集中管理标签页焦点、AudioContext、存储能力判定，不分散在多个 Autoload 中
- **可测试性**: 每个 Autoload 的 `on_[phase]_ready` 方法可独立调用测试，不依赖 Godot 的隐式 Autoload 顺序
- **常驻 Hub 保证**: AirshipHub 作为常驻子节点，其子节点状态（ModuleManager #8、Partner #15）在 Exploration 往返间不丢失，信号线在遮挡时保持活跃

### Negative

- **启动信号链复杂度**: 9 个 phase + 多条信号连接，调试启动失败需要追踪信号链
- **AirshipHub 常驻**: Hub 在玩家进入 Exploration 时不销毁（只隐藏），占用内存。对于 Web 目标，需要剖面确认 Hub 场景的内存占用在可接受范围
- **9 个 Autoload 的 _ready() 仍串行执行**: 即使 _ready() 最小化，Godot 仍逐个调用，Web 端冷启动时逐个编译 GDScript 可能累积延迟
- **信号连接时序依赖**: 如果某个 Autoload 的 `on_[phase]_ready` 未正确连接到上一个 phase 的信号，启动链静默断裂

### Risks

- **Risk**: 9 Autoload 在 Web 端的首帧延迟超标（目标 <2s 从 `boot_requested` 到 `session_ready`）
  - **Mitigation**: 在 ADR-0006 中设定启动时间预算；如果 Phase 1 静态内容加载超过 500ms，引入按内容域懒加载；如果总启动 >2s，将 WorldRepair、FeedbackManager 降级为延迟初始化
- **Risk**: Signal-chain boot 在某个 phase 失败后无法恢复
  - **Mitigation**: SessionShell 设置 15s 总启动超时；区分 FatalError（Registry 损坏、引擎失败）和 RecoverableError（存档损坏、瞬态错误）；RecoverableError 最多重试 3 次后升级为 FatalError
- **Risk**: Dual-focus 系统 (4.6) 与自定义 4 层输入路由冲突
  - **Mitigation**: 场景切换协议包含显式 `release_focus()`→`grab_focus()` 步骤；ADR-0012 中专设 dual-focus 兼容验证任务；MVP 阶段如果冲突严重，可回退到 Godot 4.6 的 `Control.focus_mode` 默认行为
- **Risk**: Feature Scene queue_free() 时活跃的 Tween/await 协程导致崩溃
  - **Mitigation**: 场景退出协议要求显式 stop() 所有 Tween、取消所有 await 协程，断开外部信号连接后再 queue_free()

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| #1 content-data-state-registry | Registry 作为 Foundation Autoload 最先初始化，静态内容 fail-fast | Registry 在 Autoload 序列第 1 位；Phase 1 `registry_ready` 前完成 validate_all()；失败为 FatalError |
| #2 platform-session-shell | 15 平台状态、音频激活、标签页恢复、Start/Continue 入口 | SessionShell 作为主场景根节点管理全部 15 状态；Phase 7 判定 continue_state 并呈现 Title Screen |
| #2 platform-session-shell | 存储能力权威判定由存档系统拥有，壳层只读取 | `storage_capability` 由 Persistence Autoload 判定，SessionShell 通过 `persistence_ready` 信号携带的 `continue_state` 读取 |
| #3 local-save-world-state-persistence | Staging→Verify→Promotion 工作流；领域系统拥有自身序列化 | Persistence 为 Autoload 第 2 位；Phase 2 完成 slot 检查；每个领域 Autoload 独立管理自身状态的序列化/反序列化 |
| #4 player-movement-interaction | InteractionRegistry Autoload + @abstract InteractionHandler | InteractionRegistry 为 Autoload 第 3 位；Phase 4 初始化完成后等待 Scene 注册可交互对象 |
| #5 resources-goods-capacity | 6 资源池常驻可查询 | Resources 为 Autoload 第 4 位；Phase 3a 并行初始化 6 池 |
| #6 player-knowledge-intel | 知识状态常驻、只进不退 | Intel 为 Autoload 第 5 位；Phase 3a 并行初始化知识状态 |
| #9 chart-route-planning | 航线状态在 Exploration→Hub 往返间保持 | Chart 为 Autoload 第 6 位；Phase 3b 在 Intel 就绪后初始化；状态在 Scene 切换时不丢失 |
| #13 world-repair-unlock | repair_completed 信号跨系统触发 (4 消费者) | WorldRepair 为 Autoload 第 7 位；Phase 4 连接 Resources.deposit_committed |
| #16 ui-hud-chart-interface | 12 屏幕、模态栈、4 层输入路由 | UIManager 为 Autoload 第 8 位；Phase 6 初始化全部屏幕；始终渲染在最顶层 |
| #17 feedback-fx-audio | 语义事件订阅 | FeedbackManager 为 Autoload 第 9 位 (VS)；Phase 7 订阅语义事件 |

## Performance Implications

- **CPU**: Autoload `_ready()` 串行执行 — 目标 <100ms 总计（仅常量初始化和信号声明）。Phase 1 静态内容加载（Registry）可能消耗 200–500ms，需异步分帧或懒加载按内容域
- **Memory**: 9 Autoload 常驻内存 — 目标 <10MB 总计（纯数据结构，无 Node 子树）。AirshipHub Scene 常驻 — 需剖面确认 <30MB。Feature Scene 按需创建/销毁 — 峰值内存出现在 Exploration 活跃时
- **Load Time**: 首帧目标 <2s（从 `boot_requested` 到 `session_ready`）。Phase 1（静态内容加载）最重；如果超标，引入 lazy-load by content domain。如果总启动 >2s，WorldRepair、FeedbackManager 降级为延迟 Autoload
- **Network**: 无 — 所有内容为本地资源（Web 导出 .pck 包内）

## Migration Plan

此为项目第一个 ADR，无现有代码迁移。实现时：

1. 在 `project.godot` 中按声明顺序注册 9 个 Autoload
2. 创建 `SessionShell.tscn` 作为主场景（`Project Settings → Application → Run → Main Scene`）
3. 为每个 Autoload 创建最小 GDScript 骨架（空 `_ready()` + signal 声明 + `on_[phase]_ready` 桩方法）
4. 实现 SessionShell 的 9-phase 信号链（Phase 0–8）
5. 实现 FatalError / RecoverableError 分类处理
6. 每完成一个 phase，profile 启动时间增量
7. 实现场景切换协议（退出清理 → `queue_free()` → 实例化 → `grab_focus()`）

## Validation Criteria

- 所有 9 个 Autoload 的 `_ready()` 在 <100ms 内完成（总合）
- 从 `boot_requested` 到 `session_ready` <2s（Web 桌面浏览器，warm cache）
- Phase 1 静态内容加载失败时 → `boot_fatal` 触发，玩家看到 "游戏文件已损坏" 界面（无重试按钮）
- Phase 2 存档全部损坏时 → `boot_recoverable` 触发，玩家可选择 "开始新游戏"
- Continue 可用时 → Title Screen 展示 Start + Continue；无存档时 → 仅 Start
- AirshipHub → ExplorationScene → AirshipHub 往返 10 次内存无泄漏（queue_free 正确清理）
- 场景切换时显式 release_focus()/grab_focus() — dual-focus 兼容验证
- 标签页隐藏→恢复后，SessionShell 正确处理 `InputReacquire` 状态
- 所有系统在自身初始化 phase 之前被调用 → 断言失败（防御性检查）
- 如果启动分析显示 Web 加载时间 >2s，WorldRepair 实现延迟初始化

## Related Decisions

- **ADR-0002** (待创建): Signal 通信协议 — 定义此 ADR 中 Autoload 之间的信号契约细节
- **ADR-0004** (待创建): InteractionHandler @abstract 基类 — 定义 InteractionRegistry 管理的处理器接口
- **ADR-0006** (待创建): Web 平台约束 — 定义此 ADR 中启动时间预算、IndexedDB 存储、AudioContext 的具体验证标准
- **Master Architecture**: `docs/architecture/architecture.md` — Phase 3 初始化顺序和 Autoload/Scene 划分的来源
