# ADR-0005: 资源池系统 — Autoload ResourcesManager + 六池 Dictionary 架构

## Status
Accepted

## Date
2026-05-04

## Summary
ResourcesManager 作为 Autoload #5，使用 Dictionary[StringName, Dictionary] 六池结构管理所有游戏内资源状态。7 种原子操作返回类型化 ResourceResult，双容量制（槽位/容积）在统一 stack_merge 算法中检查。7 个 typed signal 遵循 emit-after-mutation 时序。池 1-3 通过 Canonical JSON 快照包持久化。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-04

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Data / Resource Management |
| **Knowledge Risk** | LOW — Dictionary、typed Array、signal emit 均为 Godot 4.x stable API |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md` |
| **Post-Cutoff APIs Used** | None — 核心数据操作仅使用 Godot 4.x stable Dictionary/Array/StringName |
| **Verification Required** | `JSON.stringify()` 对 nested Dictionary[StringName] 的序列化行为（StringName 序列化为 String 的保真度）；大量 (100+) 栈合并运算在 60fps 下的 CPU 开销 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (ResourcesManager 为 Autoload #5，Phase 5 foundation_ready 初始化)；ADR-0002 (typed signal 契约 — `pool_changed`、`resource_added`、`resource_removed` 等)；ADR-0003 (progress.resources 快照包 — 池 1-3 完整状态序列化)；ADR-0004 (Interactable 子类处理存储点/拾取点/货舱的 Use 入口) |
| **Enables** | ADR-0006 (Web 平台约束 — 存档数据的序列化路径)；ADR-0007 (内容注册表 — 资源/货物 Schema 定义)；ADR-0010 (航行系统 — consume_for_route)；ADR-0011 (探索系统 — add_loot/extract)；ADR-0012 (战斗系统 — consume_in_combat)；ADR-0013 (世界修复 — commit_deposit)；ADR-0014 (集市交易 — execute_purchase) |
| **Blocks** | 所有依赖资源转移的下游系统 — 航行消耗、探索拾取、修复提交、集市交易、情报消耗 |
| **Ordering Note** | 必须在 ADR-0001/0002/0003/0004 之后 Accepted；在内容注册表 ADR-0007 之前 Accepted（注册表需本系统的 Schema 约束）；在所有消费资源的领域系统之前 Accepted |

## Context

### Problem Statement

GDD #2 `resources-goods-capacity` 定义了《云海织航》的完整资源与物流契约：6 个规范池（随身物品栏、飞艇仓库、货舱、集市摊位库存、探索局内池、修复节点提交）、7 种原子操作（add/remove/transfer/consume/discard/unpack_cargo/consume_in_combat）、双容量制（槽位制 + 容积制）、栈合并算法、信号契约和 23 个边缘情况。本 ADR 需决定：ResourcesManager 的 Autoload 位置、六池的运行时数据结构、操作 API 的 Result 类型、容量检查策略、信号签名，以及如何与持久化快照和交互入口集成。

### Constraints

- **ADR-0001**: ResourcesManager 为 Autoload #5（在 Persistence #3、InteractionRegistry #4 之后），Phase 5 `foundation_ready` 初始化
- **ADR-0002**: 状态变更使用 typed signal；读查询使用直接方法调用；禁止 Dictionary signal payload
- **ADR-0003**: 池 1-3（on_person、in_storage、loaded）必须持久化为 `progress.resources` 快照包；遵循 Canonical JSON 编码
- **ADR-0004**: 存储点/拾取点/货舱的 Use 入口通过 Interactable 子类 → `handle_use()` → 委托给 ResourcesManager 操作
- **GDD #2**: 6 池、原子操作全集、栈合并公式、双容量制、23 个 EC、信号契约（7 个 signal）、起始状态定义
- **Web 单线程**: 所有资源操作在单帧内同步完成；操作间无并发
- **原子性**: 所有操作为全成功或全失败 — 不产生中间状态

### Requirements

- 6 个规范池的运行时存储（每个资源以 `{id: StringName, quantity: int}` 表示）
- 7 种原子操作返回类型化 Result（成功/失败 + 失败原因）
- 双容量检查：槽位制（随身/局内 — 每堆 1 槽）+ 容积制（仓库/货舱 — mass_class → volume 映射）
- 栈合并算法：同 ID 堆优先合并到已有最大堆；溢出时检查容量
- 7 个 typed signal（遵循 ADR-0002），emit-after-mutation 时序
- 快照序列化：池 1-3 完整状态 → Canonical JSON（ADR-0003）
- 起始状态注入：`reset_for_new_game(starting_snapshot)`

## Decision

### 1. ResourcesManager — Autoload #5

ResourcesManager 是 Autoload #5，Phase 5 `foundation_ready` 初始化。它是所有资源状态的唯一权威源。

```
Boot order (ADR-0001):
  #1 Registry        → Phase 3 registry_ready
  #2 SessionShell     → Phase 1 (host)
  #3 Persistence      → Phase 3 persistence_ready
  #4 InteractionRegistry → Phase 4 core_ready
  #5 ResourcesManager → Phase 5 foundation_ready  ← 本 ADR
  #6 Intel            → Phase 6 hub_ready
  ... (remaining Autoloads)
```

ResourcesManager 依赖：
- Persistence 已 ready（可注册 domain serializer）
- InteractionRegistry 已 ready（可接收 use_requested 分发结果）
- 内容注册表已 ready（可查询资源的 stack_rule、mass_class、supply_class）

### 2. 六池数据结构

选择 `Dictionary[StringName, Dictionary]` 作为六池的运行时存储 — 轻量、可序列化、O(1) 键查找。

```gdscript
# === ResourcesManager (Autoload #5) — 核心数据结构 ===
# 位置: src/core/resources/resources_manager.gd

class_name ResourcesManager
extends Node

# --- 六池存储 ---
# 格式: { pool_id: { "stacks": Array[Dictionary], "capacity": Dictionary } }
# 每个 stack: { "resource_id": StringName, "quantity": int }
# capacity: 见容量系统

var _pools: Dictionary = {}
# 键: &"on_person" / &"in_storage" / &"loaded" / &"listed" / &"carried" / &"deposited"

# --- ResourceResult 枚举 ---
enum ResourceResult {
    SUCCESS,                    # 操作成功
    ERR_TARGET_FULL,            # 目标池满（容积或槽位不足）
    ERR_SOURCE_INSUFFICIENT,    # 源池持有量不足
    ERR_CAPACITY_ZERO,          # 货舱基础容积为 0（无模块）
    ERR_INVALID_QUANTITY,       # quantity < 0
    ERR_MISSING_REFERENCE,      # resource_id 不在注册表中
    ERR_DEPRECATED_ID,          # resource_id 已弃用
    ERR_STORAGE_FULL,           # 拆包目标仓库满
    ERR_CARRY_SLOTS_FULL,       # 随身槽位满
    ERR_CARRY_STACK_FULL,       # 匹配堆已达 max_stack
    ERR_CARGO_NOT_IN_BAY,       # 货物不在货舱中
    ERR_BUSY,                   # 重入防护 — 操作进行中
    ERR_KIND_MISMATCH,          # 货物进入非货舱池 / 裸资源进入货舱
}
```

**设计原理**:
- `Dictionary` 而非 typed class：6 个池结构一致（每组 `{stacks: Array, capacity: Dict}`），Dictionary 提供统一的遍历和序列化路径。存储的 stack entries 也是 `{id, qty}` Dictionary — JSON 直接映射
- `StringName` 键：pool_id 和 resource_id 使用 `StringName` — O(1) 比较、引擎内部 interning
- `_pools` 为私有 — 所有外部访问通过公共 API

### 3. 资源操作 API

所有操作返回 `ResourceResult`，原子执行（全成功或全失败）。

```gdscript
# --- 核心操作 (原子) ---

func add(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 添加资源到目标池
    # stackable: 优先合并到已有最大堆；溢出时创建新堆
    # unique: 每件单独一槽
    pass

func remove(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 从目标池移除指定数量
    # 多堆拆分：从数量最大的堆开始移除，不足时拆下一个堆
    pass

func transfer(from_pool: StringName, to_pool: StringName,
              resource_id: StringName, quantity: int) -> ResourceResult:
    # 跨池转移。原子：源移除 + 目标添加在同一帧完成
    # 支持拆分：源堆保留剩余
    pass

func consume(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 领域系统驱动的消耗（修复、制造、航线消耗）
    # 语义同 remove — 资源进入 destroyed 终态
    pass

func discard(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 玩家驱动的丢弃 — 永久销毁
    # 有效目标池: on_person, in_storage, loaded, carried
    # 需二次确认（调用方在调用前显示确认对话框）
    pass

# --- 货物专属操作 ---

func unpack_cargo(cargo_slot_index: int) -> ResourceResult:
    # 销毁货舱中指定槽位的货物物品
    # 将其 linked_resource_id 的资源以货物声明数量加入仓库
    # 原子：货物销毁 + 资源入仓在同一帧完成
    pass

# --- 战斗专属操作 ---

func consume_in_combat(resource_id: StringName, quantity: int) -> ResourceResult:
    # consume(Pool 5, resource_id, quantity) 的薄封装
    # 从随身物品栏 (carried) 消耗
    pass

# --- 修复专属操作 ---

func can_deposit(repair_node_id: StringName, resource_costs: Dictionary) -> bool:
    # 检查资源是否足够提交修复 — 读查询，不修改状态
    pass

func commit_deposit(repair_node_id: StringName, resource_costs: Dictionary) -> ResourceResult:
    # 提交资源到修复节点 — 不可逆终态操作
    # 原子：从多个池移除 → 资源进入 deposited 终态
    pass

# --- 集市专属操作 ---

func validate_purchase(good_id: StringName, quantity: int) -> bool:
    pass

func execute_purchase(good_id: StringName, quantity: int) -> ResourceResult:
    # 购买：资源从 listed → in_storage（或 on_person）
    pass

func list_for_sale(resource_id: StringName, quantity: int, price: int) -> ResourceResult:
    # 上架：资源从 in_storage/loaded → listed
    pass

# --- 起始状态 ---

func reset_for_new_game(starting_snapshot: Dictionary) -> void:
    # 由 Persistence 在 new_game() 时调用
    # starting_snapshot 包含池 1-3 的初始内容
    pass
```

### 4. 双容量系统

```gdscript
# --- 容量配置 (由内容注册表提供 static 数据，本系统持有 runtime 值) ---

const CARRY_BASE_SLOTS: int = 5          # 随身物品栏基础槽位
const CARRIED_BASE_SLOTS: int = 5         # 探索局内池基础槽位
const STORAGE_BASE_VOLUME: int = 1000     # 仓库基础容积
const CARGO_BAY_BASE_VOLUME: int = 0      # 货舱基础容积 (固定 0)
const CARGO_MODULE_VOLUME_BONUS: int = 500 # 货物模块提供的容积加成

# mass_class → {volume, weight} 映射
const MASS_CLASS_TABLE: Dictionary = {
    &"light":  { "volume": 50,  "weight": 1 },
    &"medium": { "volume": 120, "weight": 3 },
    &"heavy":  { "volume": 200, "weight": 6 },
}

# --- 容量查询 (读查询 — 直接方法调用) ---

func get_carry_capacity() -> int:
    # 随身槽位 = CARRY_BASE_SLOTS + carry_slot_bonus (由伙伴/背包系统注入)
    pass

func get_storage_capacity() -> int:
    # 仓库容积 = STORAGE_BASE_VOLUME + storage_volume_bonus
    pass

func get_cargo_bay_capacity() -> int:
    # 货舱容积 = CARGO_BAY_BASE_VOLUME + cargo_module_volume_bonus
    pass

func get_total_loaded_mass() -> int:
    # sum(weight_value × stack_count for each stack in cargo_bay)
    # 仅货舱内容贡献重量。随身和仓库不计入。
    pass

# --- 槽位可用性检查 (内部) ---

func _slot_available(pool_id: StringName, slot_cost: int) -> bool:
    # used_slots + slot_cost <= total_slots
    pass

func _volume_available(pool_id: StringName, item_volume: int) -> bool:
    # used_volume + item_volume <= total_volume
    pass
```

**容量加成注入接口**:

```gdscript
# 供伙伴系统/背包系统/模块系统调用
func set_carry_slot_bonus(bonus: int) -> void:
    # 伙伴系统通过此接口注入随身槽位加成
    pass

func set_carry_volume_bonus(bonus: int) -> void:
    pass

func set_storage_volume_bonus(bonus: int) -> void:
    # 模块系统通过此接口注入仓库扩展加成
    pass

func set_cargo_module_volume_bonus(bonus: int) -> void:
    # 模块系统通过此接口注入货舱容积加成
    pass
```

### 5. 栈合并算法

```
算法 stack_merge(pool_id, resource_id, quantity):
  1. 读取该资源的 stack_rule 和 max_stack（从内容注册表）
  2. 在目标池中查找 resource_id == target 的所有堆
  3. has_match = (匹配堆数量 > 0)
  4. 若有匹配:
       a. 选择已有数量最大的堆（fill fullest first）
       b. 若多堆数量相同，选最低槽位索引
       c. merge_qty = min(quantity, max_stack - 选中堆的 E)
       d. overflow_qty = quantity - merge_qty
  5. 若无匹配:
       merge_qty = 0
       overflow_qty = quantity
  6. 若 overflow_qty > 0:
       a. 计算所需新堆数: new_stacks = ceil(overflow_qty / max_stack)
       b. 槽位制池: 检查 used_slots + new_stacks <= total_slots
       c. 容积制池: 检查 used_volume + new_stacks × item_volume <= total_volume
  7. 若容量充足 → 执行合并 + 创建新堆 → SUCCESS
     否则 → ERR_TARGET_FULL (附具体约束: 槽位 or 容积)
```

### 6. 信号契约

遵循 ADR-0002: typed params、{noun}_{verb_past} 命名、emit-after-mutation。

```gdscript
# --- 信号 (fire-and-forget 状态变更通知) ---

signal pool_changed(pool_id: StringName)
# 消费者: UIManager — 任意池内容变更后按需重查

signal resource_added(pool_id: StringName, resource_id: StringName, quantity: int)
# 消费者: UIManager, FeedbackManager — add() 成功后

signal resource_removed(pool_id: StringName, resource_id: StringName, quantity: int)
# 消费者: UIManager, FeedbackManager — remove() / consume() / discard() 成功后

signal transfer_completed(from_pool: StringName, to_pool: StringName,
                          resource_id: StringName, quantity: int)
# 消费者: UIManager — transfer() 成功后

signal cargo_unpacked(cargo_id: StringName, resource_id: StringName, quantity: int)
# 消费者: UIManager, FeedbackManager — unpack_cargo() 成功后

signal deposit_committed(repair_node_id: StringName)
# 消费者: WorldRepair, FeedbackManager — commit_deposit() 成功后

signal deposit_failed(repair_node_id: StringName, reason: StringName)
# 消费者: WorldRepair, UIManager — commit_deposit() 失败时
# ADR-0002 配对的 xxx_failed 信号 — 使非调用消费者 (UIManager) 无需调用方转发即可响应失败

signal mass_changed(new_mass: int)
# 消费者: AirshipModuleSystem, UIManager — 货舱内容变更后
```

**重入防护**: 信号处理器不得在回调中调用本系统的变更方法 — 返回 `ERR_BUSY`。可安全调用查询方法（`get_*`、`can_*`、`validate_*`）。若需级联操作（如信号回调中触发新 transfer），使用 `call_deferred()`。

### 7. 持久化集成

遵循 ADR-0003 快照包契约:

```gdscript
# ResourcesManager 注册到 Persistence:
# Persistence.register_domain_serializer("resources", _serialize_resources)

func _serialize_resources() -> Dictionary:
    # 返回 progress.resources 快照包
    # 仅序列化池 1-3 (on_person, in_storage, loaded)
    # 池 4-6 由对应领域系统自行持久化
    return {
        "domain": "resources",
        "version": 1,
        "pools": {
            "on_person": _serialize_pool(&"on_person"),
            "in_storage": _serialize_pool(&"in_storage"),
            "loaded": _serialize_pool(&"loaded"),
        },
        # 容量加成也持久化（伙伴/模块系统可能在会话间保留）
        "bonuses": {
            "carry_slot_bonus": _carry_slot_bonus,
            "carry_volume_bonus": _carry_volume_bonus,
            "storage_volume_bonus": _storage_volume_bonus,
            "cargo_module_volume_bonus": _cargo_module_volume_bonus,
        },
    }

func _deserialize_resources(snapshot: Dictionary) -> void:
    # 由 Persistence 在 load 时调用
    # 恢复池 1-3 + 容量加成
    pass
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                    RESOURCE POOL ARCHITECTURE                         │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │              ResourcesManager (Autoload #5)                   │    │
│  │              Phase 5: foundation_ready                        │    │
│  │                                                               │    │
│  │  ┌──────────────────────────────────────────────────────┐   │    │
│  │  │               Six-Pool Storage (_pools)                │   │    │
│  │  │                                                       │   │    │
│  │  │  Pool 1: on_person   Pool 2: in_storage              │   │    │
│  │  │  (槽位制, 5 槽)      (容积制, 1000)                   │   │    │
│  │  │  本系统直接管理       本系统直接管理                    │   │    │
│  │  │                                                       │   │    │
│  │  │  Pool 3: loaded      Pool 4: listed                   │   │    │
│  │  │  (容积制, 0+500)     (集市系统管理)                    │   │    │
│  │  │  本系统直接管理       通过原语间接操作                   │   │    │
│  │  │                                                       │   │    │
│  │  │  Pool 5: carried     Pool 6: deposited                │   │    │
│  │  │  (槽位制, 5 槽)      (终态 — 不可逆)                   │   │    │
│  │  │  探索系统管理         修复系统触发                       │   │    │
│  │  └──────────────────────────────────────────────────────┘   │    │
│  │                                                               │    │
│  │  Operations:    Signals (emit-after-mutation):                 │    │
│  │  add()          pool_changed(pool_id)                         │    │
│  │  remove()       resource_added(pool_id, resource_id, qty)     │    │
│  │  transfer()     resource_removed(pool_id, resource_id, qty)   │    │
│  │  consume()      transfer_completed(from, to, id, qty)         │    │
│  │  discard()      cargo_unpacked(cargo_id, resource_id, qty)    │    │
│  │  unpack_cargo() deposit_committed(repair_node_id)              │    │
│  │  consume_in_    mass_changed(new_mass)                         │    │
│  │    combat()                                                   │    │
│  │  commit_deposit()                                             │    │
│  │  execute_purchase()                                           │    │
│  │  list_for_sale()                                              │    │
│  └────────┬──────────────┬──────────────┬───────────────────────┘    │
│           │              │              │                             │
│           ▼              ▼              ▼                             │
│  ┌────────────┐ ┌────────────┐ ┌──────────────────────────┐        │
│  │ Persistence│ │ UIManager  │ │  Domain Systems           │        │
│  │ (快照包)    │ │ (容量条/HUD│ │                           │        │
│  │            │ │  物品栏)    │ │  ┌──────────────────┐    │        │
│  └────────────┘ └────────────┘ │  │ AirshipModule     │    │        │
│                                │  │ (载重适航判定)     │    │        │
│  Interactable 子类:             │  └──────────────────┘    │        │
│  ┌────────────────────┐        │                          │        │
│  │ StorageRack        │        │  ┌──────────────────┐    │        │
│  │  handle_use() →    │        │  │ Exploration       │    │        │
│  │  ResourcesManager  │        │  │ (add_loot,        │    │        │
│  │  .transfer()       │        │  │  extract_carried) │    │        │
│  └────────────────────┘        │  └──────────────────┘    │        │
│                                │                          │        │
│  ┌────────────────────┐        │  ┌──────────────────┐    │        │
│  │ CargoArea          │        │  │ Settlement        │    │        │
│  │  handle_use() →    │        │  │ (购买/出售/上架)   │    │        │
│  │  ResourcesManager  │        │  └──────────────────┘    │        │
│  │  .unpack_cargo()   │        │                          │        │
│  └────────────────────┘        │  ┌──────────────────┐    │        │
│                                │  │ WorldRepair       │    │        │
│  ┌────────────────────┐        │  │ (commit_deposit)  │    │        │
│  │ PickupPoint        │        │  └──────────────────┘    │        │
│  │  handle_use() →    │        │                          │        │
│  │  ResourcesManager  │        │  ┌──────────────────┐    │        │
│  │  .add()            │        │  │ Combat            │    │        │
│  └────────────────────┘        │  │ (consume_in_      │    │        │
│                                │  │  combat)          │    │        │
│                                │  └──────────────────┘    │        │
│                                └──────────────────────────┘        │
└──────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```gdscript
# === Interactable 子类示例: 货舱存储点 ===

class_name CargoArea
extends Interactable

func _ready() -> void:
    interaction_id = &"hub.interactable.cargo_area"
    interaction_type = &"use"

func handle_use(player_id: StringName) -> Interactable.UseResult:
    # 打开货舱 UI（通过 UIManager），不直接操作资源
    # 货舱 UI 内部的 transfer/unpack 操作调用 ResourcesManager API
    UIManager.open_cargo_bay_screen()
    return Interactable.UseResult.ACCEPTED

# === Interactable 子类示例: 世界拾取点 ===

class_name PickupPoint
extends Interactable

@export var pickup_resource_id: StringName
@export var pickup_quantity: int = 1

func handle_use(player_id: StringName) -> Interactable.UseResult:
    var result = ResourcesManager.add(&"carried", pickup_resource_id, pickup_quantity)
    match result:
        ResourceResult.SUCCESS:
            queue_free()  # 拾取后从世界移除
            return Interactable.UseResult.ACCEPTED
        ResourceResult.ERR_CARRY_SLOTS_FULL, ResourceResult.ERR_CARRY_STACK_FULL:
            return Interactable.UseResult.REJECTED
        _:
            return Interactable.UseResult.REJECTED
```

## Alternatives Considered

### Alternative A: Godot Resource 类型化池

- **Description**: 每个资源堆定义为 `ResourceStack` (extends Resource)，包含 `resource_id: StringName`、`quantity: int`、`mass_class: StringName` 等。池为 `Array[ResourceStack]`
- **Pros**: 类型安全 — IDE 自动补全字段；`.tres` 文件可作初始状态资源
- **Cons**: 每个 stack 需要 Resource 实例化和引用计数开销；序列化为 `.tres` 格式不符合 ADR-0003 的 Canonical JSON 要求；100+ 堆时 Resource 对象数量过多
- **Rejection Reason**: GDScript Dictionary 已提供足够结构；Godot Resource 的引用计数和 `.tres` 序列化与 Canonical JSON 快照包目标冲突。Dictionary 可直接 `JSON.stringify()` 而无需转换层

### Alternative B: 分离的 PoolManager 子 Autoloads

- **Description**: 每个池有独立的 Manager（`CarryManager`、`StorageManager`、`CargoBayManager`），各自为独立 Autoload
- **Pros**: 职责分离清晰；单个 Manager 代码量小
- **Cons**: 跨池 transfer 需要 Manager 间协调 — 引入临时耦合；信号需跨 Manager 传播 — 违反 ADR-0002 的 max cascade depth 2；Autoload 数量膨胀（+6 Autoloads）
- **Rejection Reason**: ADR-0001 已固定 10 个 Autoload 上限。6 个独立池 Manager 会浪费 Autoload 配额。单 ResourcesManager 内部维护 6 池 — 跨池操作在同一对象内原子完成，无需跨 Autoload 协调

### Alternative C: 事件溯源 (Event Sourcing)

- **Description**: 所有资源变更为不可变事件日志；当前状态从事件重放计算
- **Pros**: 完整审计追踪；时间旅行调试；天然支持撤销
- **Cons**: 事件日志随时间无限增长；每次查询需重放所有历史事件 → CPU 开销随游戏进程增长；JSON 存档体积膨胀
- **Rejection Reason**: 对资源系统的需求过度。GDD 不要求资源操作审计。快照包直接序列化当前状态（ADR-0003）— 事件溯源产生的日志与快照目标冲突。不可逆操作（deposited/destroyed）已提供足够的因果追踪

## Consequences

### Positive

- **单一权威源**: ResourcesManager 是所有资源状态的唯一读写入口 — 下游系统不缓存或复制数量，消除同步 bug
- **原子操作保证**: 所有操作为全成功或全失败 — 无 partial transfer 或 half-consumed 状态
- **序列化直接**: Dictionary 结构 → JSON.stringify() 零转换层（除 StringName → String，Godot 自动处理）
- **容量检查统一**: 槽位制和容积制在同一个 `stack_merge` 算法中统一处理 — 下游系统无需区分池类型
- **类型化 Result**: 每个操作返回具体失败原因 — UI 可显示精确错误信息（"物品栏满" vs "堆叠已达上限"）
- **可测试**: 所有操作为纯数据变换 — 无引擎依赖即可单元测试

### Negative

- **Dictionary 类型安全弱**: `_pools` 内部为 `Dictionary`，字段拼写错误在运行时才暴露。通过公共 API 方法（add/remove/transfer）强制所有访问路径来缓解
- **Mass_class 表硬编码**: mass_class → {volume, weight} 映射在当前设计中为 const Dictionary。未来若需要运行时更改（如装备效果改变货物重量），需重构为动态查询
- **6 池在同一对象**: 随着下游系统增多，ResourcesManager API 表面积可能扩大（当前 15+ 公共方法）

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| 信号回调中的重入 mutation（下游在 `resource_added` 回调中调用 `transfer()`） | Low — 同步 emit 使调用栈可预测 | Medium — 可能导致嵌套状态变更和数据不一致 | `ERR_BUSY` 守卫 — 操作开始时设置 `_busy` 标志，结束时清除。重入调用立即返回 `ERR_BUSY` |
| 栈合并在大量堆 (100+) 时 CPU 开销超预算 | Low — 匹配堆数通常 ≤ 5 | Low — 即使退化，O(N) 查询仍 < 0.5ms | 合并算法 O(N)。若未来堆数增长，添加 resource_id → stack_index 索引加速查找。实现阶段 profile 验证 |
| `JSON.stringify()` 对 StringName 的处理在 4.6.2 中可能不稳定 | Low — Godot 4.x 已知 StringName → String 自动转换 | Medium — 存档兼容性问题 | 验证项要求测试 StringName → JSON → StringName 往返。若引擎有 bug，序列化前显式 `String(str(name))` 转换 |
| 容量加成注入接口被多系统同时调用导致数据竞争 | Low — Godot 单线程无真正并发 | Low — 加成值运行时不频繁变更 | 加成在初始化阶段设置；Godot 同步模型保证无竞争 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| #2 resources-goods-capacity | 6 规范池 (Rules 8-9, 19) | `_pools` Dictionary 结构 — 6 键，每键存 stacks + capacity |
| #2 resources-goods-capacity | 原子操作全集 (Rules 14-15) | 7+ 操作全部返回 `ResourceResult` — 全成功或全失败 |
| #2 resources-goods-capacity | 栈合并算法 (Formula: stack_merge) | `stack_merge` 算法 — fill fullest first + overflow 检查 |
| #2 resources-goods-capacity | 双容量制: 槽位 + 容积 (Rules 8-11) | `_slot_available()` / `_volume_available()` — 由 `stack_merge` 统一调用 |
| #2 resources-goods-capacity | 重量与适航 (Rules 12-13, Formula: total_loaded_mass) | `get_total_loaded_mass()` + `mass_changed` signal — 模块系统消费 |
| #2 resources-goods-capacity | 信号契约 (Rules 20-23) | 7 typed signals — emit-after-mutation, 重入防护 (ERR_BUSY) |
| #2 resources-goods-capacity | 持久化池 1-3 (Rule 19, EC-06/07) | `_serialize_resources()` → progress.resources 快照包 |
| #2 resources-goods-capacity | 起始状态注入 (Starting State) | `reset_for_new_game(starting_snapshot)` |
| #2 resources-goods-capacity | 23 个 Edge Cases | 操作 API 覆盖所有 EC 对应的失败原因 (ERR_* 枚举值) |
| #4 player-movement-interaction | 存储点/拾取点/货舱 Use 入口 | Interactable 子类 → handle_use() → ResourcesManager 操作 (Key Interfaces 示例) |

## Performance Implications

- **CPU**: 每次资源操作 ~10-50 次 Dictionary 查找 (O(1) each)。`stack_merge` O(N) where N ≤ 5 (同 ID 堆数)。单操作 < 0.1ms — 可忽略。信号 emit 同步触发 UI 更新 — UI 刷新开销应 < 1ms
- **Memory**: 6 池 Dictionary ~200 bytes 固定开销。每个 stack entry (Dictionary) ~100 bytes。100 堆 → ~10KB — 可忽略
- **Load Time**: 快照反序列化 → Dictionary 重建 6 池。100 堆 → < 2ms — 可忽略
- **Network**: 无 — 所有资源处理为本地

## Migration Plan

项目尚无代码。实现顺序：

1. 创建 `src/core/resources/resources_manager.gd` — `ResourceResult` enum + 信号声明 + `_pools` 初始化
2. 在 Phase 5 `foundation_ready` 中调用 `ResourcesManager.on_foundation_ready()`
3. 实现容量系统 — `mass_class` 映射 + 容量查询方法
4. 实现核心操作 — `add()` / `remove()` / `transfer()`（含栈合并算法）
5. 实现专属操作 — `consume()` / `discard()` / `unpack_cargo()` / `consume_in_combat()`
6. 实现修复集市接口 — `commit_deposit()` / `execute_purchase()` / `list_for_sale()`
7. 实现持久化集成 — `_serialize_resources()` / `_deserialize_resources()` + 注册到 Persistence
8. 实现起始状态 — `reset_for_new_game()`
9. 创建 Interactable 子类 — `StorageRack`、`CargoArea`、`PickupPoint`
10. 单元测试 — 所有 7 个操作 × 成功路径 + 每个 `ERR_*` 失败路径 + 栈合并边界 (0 qty, overflow, max_stack 边界)

## Validation Criteria

- `add()` / `remove()` / `transfer()` / `consume()` 全成功或全失败 — 无部分状态
- `stack_merge` 算法 fill fullest first — 确定性行为
- `transfer()` 在源不足或目标满时原子失败 — 源堆不变
- `unpack_cargo()` 在仓库满时失败 — 货物保留在货舱
- 信号在 mutation 完成后触发 — 处理器读到完整状态
- 信号回调中调用变更方法返回 `ERR_BUSY`
- `reset_for_new_game()` 正确恢复池 1-3 起始状态
- `_serialize_resources()` → `_deserialize_resources()` 往返一致（包括 StringName 身份）
- `get_total_loaded_mass()` 仅在货舱内容变更后变化
- 容量加成通过注入接口设置后立即反映在容量查询中

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — ResourcesManager 的 Autoload 位置 (#5) 和 Phase 5 初始化
- **ADR-0002**: Signal 通信协议 — 7 个 typed signal 的契约定义
  - ⚠️ **Supersession**: 本 ADR 的资源相关 signal 签名优先于 ADR-0002 中的暂定条目。`pool_changed(pool_id: StringName)` 替代 ADR-0002 的 `pool_changed(pool_id: int)`；`deposit_committed(repair_node_id: StringName)` 替代 ADR-0002 的 `deposit_committed(node_id: String)`；新增 `deposit_failed(repair_node_id: StringName, reason: StringName)` 配对 signal。ADR-0002 的 signal 目录应在本 ADR Accepted 后同步更新。
- **ADR-0003**: 存档系统 — progress.resources 快照包 + Canonical JSON 序列化
- **ADR-0004**: InteractionHandler — Interactable 子类（StorageRack/CargoArea/PickupPoint）的 Use 委托
- **GDD #2**: `design/gdd/resources-goods-capacity.md` — 完整的资源/货物/容量规则、公式、EC 和信号契约
- **GDD #4**: `design/gdd/player-movement-interaction.md` — 存储点/拾取点/货舱的交互入口定义
