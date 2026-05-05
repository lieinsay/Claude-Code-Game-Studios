# ADR-0011: 修复状态机与分批提交 — WorldRepair Autoload #13

## Status
Accepted

## Date
2026-05-05

## Summary
WorldRepair 作为 Autoload #13，管理修复节点的三态状态机（unrevealed → known → repaired）、分批提交算法（deposit_validation → commit_deposit → repair_completion）、以及修复完成后的 6 路下游触发链（#6 能力解锁、#9 航线增强、#14 NPC 状态、#3 存档检查点、#17 视觉锚点、UI toast）。所有修复节点状态以 Dictionary[StringName, Variant] 存储，通过 ADR-0003 Canonical JSON 快照包持久化为 `progress.world-repair`。修复是不可逆的——known→repaired 单向转换，已修复节点拒绝所有后续提交。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Game Logic |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构、状态机、信号，无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/world-repair-unlock.md`, `docs/architecture/architecture.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | deposit_validation 全部 5 种 violation 类型覆盖；repair_completion 触发后 multi-downstream 通知链顺序；已修复节点的 ERR_ALREADY_REPAIRED 幂等守卫；分批提交中途存档→读档进度一致性 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #13 启动顺序, Phase 5 feature_ready)；ADR-0002 (Signal 通信协议)；ADR-0003 (快照包持久化 — progress.world-repair)；ADR-0005 (ResourcesManager — can_deposit / commit_deposit / deposit_committed 信号, Pool 6 终态)；ADR-0007 (IntelManager — on_repair_completed 能力解锁触发) |
| **Enables** | ADR-0014 (Settlement — 消费 repair_completed 信号驱动 NPC/库存)；ADR-0016 (UI — 修复交互面板、提交确认弹窗、进度 toast) |
| **Blocks** | Chart (#9) 航线增强逻辑 — 依赖 repair_completed 后的 on_route_enhanced 调用；Settlement (#14) NPC 恢复 — 依赖 repair_completed 信号 |
| **Ordering Note** | 应在 ADR-0005 (ResourcesManager) 和 ADR-0007 (IntelManager) 之后 Accepted — 核心交互依赖 commit_deposit 和 on_repair_completed |

## Context

### Problem Statement

《云海织航》的核心推进循环是"探索→收集→修复→更多探索"。修复节点（MVP: 天礁灯塔 starlight_dock）是循环的收束点——玩家将分散收集的材料倾注到一个永久的世界改变中。GDD #13 定义了完整的修复流程：三态状态机、分批提交算法、deposit_validation 守卫、以及修复完成后的多下游触发链。但 WorldRepair 的 Autoload 定位、状态存储结构、与 ResourcesManager 的原子提交协作、以及 6 路下游通知的时序契约未在 ADR 中形式化。没有这个 ADR，修复流程的核心 invariants——不可逆性、分批提交进度持久化、multi-downstream 通知顺序——会在实现中被分散到多个系统，失去单一权威来源。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑，无引擎 API 风险
- **ADR-0002 信号协议**: typed params, sync emit, max depth 2, emit-after-mutation
- **ADR-0003 持久化**: `progress.world-repair` snapshot package — 修复状态必须在分批提交间隙可恢复
- **ADR-0005 ResourcesManager**: commit_deposit 是终态原子操作，材料进入 Pool 6 后不可取回
- **ADR-0007 IntelManager**: on_repair_completed 触发能力解锁重评估 (Path C)
- **ADR-0001 启动顺序**: WorldRepair 在 Phase 5 (feature_ready) 初始化
- **MVP 边界**: 1 个修复节点 (starlight_dock)，1 个关联航线，1 个关联能力

### Requirements

- 3 态状态机: unrevealed → known → repaired (known→repaired 单向不可逆)
- 物理到达始终可交互，情报门控仅影响 UI 提示精度
- 分批提交: 同节点材料可分多次提交，deposited 计数器跨会话持久化
- deposit_validation: 5 种 violation 类型 (invalid_node, empty_offer, invalid_material, excess_quantity, already_repaired)
- repair_completion: 全部 required_resources 满足时自动触发 known→repaired
- 6 路下游触发链: repair_completed 信号 → #6/#9/#14/#3/#17/UI

## Decision

### 1. WorldRepair 作为 Autoload #13

WorldRepair 在 Phase 5 (feature_ready) 中初始化。`_ready()` 仅执行信号声明和常量定义；实际状态初始化在收到 `feature_ready` 信号后执行。

```
Autoload 顺序 (Phase 5):
  #10 Navigation          ──┐
  #11 Exploration         ──┤
  #12 Combat              ──┤ 并行接收 feature_ready
  #13 WorldRepair         ──┤
  #14 Settlement          ──┤
  #15 Partner             ──┘
```

### 2. Dictionary 后端存储

```gdscript
# === WorldRepair 状态结构 ===

# 修复节点状态: StringName → RepairNodeState
# RepairNodeState = {
#   repair_state: int,        # 0=UNREVEALED, 1=KNOWN, 2=REPAIRED
#   deposited: Dictionary,    # Dict[StringName, int] — 已提交材料计数
#   repair_progress: float,   # 0.0–1.0
# }
var repair_nodes: Dictionary = {}  # Dictionary[StringName, Dictionary]
# MVP key: &"repair_node.starlight_dock"

# 修复节点静态定义从 Registry (#1) 读取——不在本系统中硬编码
# Registry 字段: node_id, name, linked_location_id, required_resources[],
#                unlocked_routes[], route_enhancement, pre_repair_route_state,
#                visual_state_anchor
```

**常量定义：**

```gdscript
# 修复状态枚举
const REPAIR_STATE_UNREVEALED: int = 0
const REPAIR_STATE_KNOWN: int = 1
const REPAIR_STATE_REPAIRED: int = 2

# deposit_validation violation 类型
const VIOLATION_INVALID_NODE: StringName = &"invalid_node"
const VIOLATION_EMPTY_OFFER: StringName = &"empty_offer"
const VIOLATION_INVALID_MATERIAL: StringName = &"invalid_material"
const VIOLATION_EXCESS_QUANTITY: StringName = &"excess_quantity"
const VIOLATION_ALREADY_REPAIRED: StringName = &"already_repaired"
```

### 3. 信号接口

```gdscript
# 修复完成 — 核心下游触发信号
# 遵循 ADR-0002: typed params, sync emit, emit-after-mutation
signal repair_completed(node_id: StringName)

# 修复进度变更 — 供 UI (#16) 更新进度条/航图标记
signal repair_progress_changed(node_id: StringName, progress: float, deposited: Dictionary)

# 视觉锚点变更 — 供 Feedback (#17) 消费
signal visual_state_changed(node_id: StringName, visual_state: StringName)
```

**信号发射顺序**: `repair_progress_changed` (每次提交后) → `repair_completed` (最后一批材料集齐时) → `visual_state_changed` (修复完成后)。`repair_completed` 在状态机转换（known→repaired）完成后发射——回调中可安全查询 `get_repair_state()`。

### 4. 方法接口

#### 4a. 状态查询

```gdscript
# 查询修复状态
func get_repair_state(node_id: StringName) -> int:
    # 返回 REPAIR_STATE_UNREVEALED / REPAIR_STATE_KNOWN / REPAIR_STATE_REPAIRED
    # 未注册节点返回 REPAIR_STATE_UNREVEALED

# 查询修复进度
func get_repair_progress(node_id: StringName) -> float:
    # 返回 0.0–1.0

# 查询已提交材料
func get_deposited(node_id: StringName) -> Dictionary:
    # 返回 {resource_id: deposited_count}
```

#### 4b. 位置触发

```gdscript
# 玩家到达修复节点位置 (由 Exploration #11 调用)
func on_player_arrived_at_repair_node(node_id: StringName) -> void:
    # 若 repair_state == UNREVEALED → 推进至 KNOWN
    # 物理到达始终触发状态转换——不依赖情报门控
    # 若已是 KNOWN/REPAIRED → 无操作
```

#### 4c. 提交与验证

```gdscript
# 验证提交材料是否合法
func validate_deposit(node_id: StringName, offer: Dictionary) -> Dictionary:
    # 返回 {valid: bool, violations: Array[StringName]}
    # offer 格式: {resource_id: quantity}
    # 5 种 violation:
    #   invalid_node — node_id 不在 Registry 中
    #   empty_offer — offer 为空或所有 quantity <= 0
    #   invalid_material — resource_id 不在 required_resources 中
    #   excess_quantity — offer[rid] > required[rid] - deposited[rid]
    #   already_repaired — repair_state == REPAIRED

# 提交材料 (由 Hub/Exploration 的修复交互点调用)
func submit_deposit(node_id: StringName, offer: Dictionary) -> Dictionary:
    # 返回 {result: int, deposited: Dictionary, progress: float, completed: bool}
    # 流程:
    #   1. validate_deposit(node_id, offer) — 验证失败则返回 violation
    #   2. ResourcesManager.commit_deposit(node_id, offer) — 原子扣除至 Pool 6
    #   3. 更新 deposited 计数器
    #   4. 重新计算 repair_progress
    #   5. 发射 repair_progress_changed
    #   6. 若 repair_completion() → 状态机 known→repaired → 发射 repair_completed
    #      → 触发 6 路下游链
    #      → 发射 visual_state_changed
    #   7. 返回结果
```

### 5. 核心算法

#### 5a. 三态状态机

```
unrevealed ──[on_player_arrived_at_repair_node]──→ known ──[commit_deposit 集齐全部材料]──→ repaired (终态)
```

**无效转换（状态机拒绝）：**
- `known → unrevealed`: 拒绝 — 知识不可退化
- `repaired → known`: 拒绝 — 修复不可撤销
- `repaired → unrevealed`: 拒绝 — 已修复不可遗忘
- 对 `repaired` 节点调用 `submit_deposit`: 返回 `ERR_ALREADY_REPAIRED`

#### 5b. deposit_validation 算法

```
validate_deposit(node_id, offer):
  violations = []
  
  if node_id not in Registry:
    violations.append("invalid_node")
    return {valid: false, violations}
  
  node_def = Registry.query_entity(node_id)
  required = node_def.required_resources
  
  if repair_state == REPAIRED:
    violations.append("already_repaired")
    return {valid: false, violations}
  
  if offer is empty or all quantities <= 0:
    violations.append("empty_offer")
    return {valid: false, violations}
  
  for each (rid, qty) in offer:
    if rid not in required:
      violations.append("invalid_material")
    elif qty > (required[rid] - deposited.get(rid, 0)):
      violations.append("excess_quantity")
  
  return {valid: len(violations) == 0, violations}
```

#### 5c. repair_progress 计算

```
repair_progress(node_id):
  required = Registry.query_entity(node_id).required_resources
  if |required| == 0: return 0.0
  
  total_satisfaction = 0.0
  for each (rid, required_qty) in required:
    if required_qty == 0:
      total_satisfaction += 1.0
    else:
      total_satisfaction += min(deposited.get(rid, 0) / required_qty, 1.0)
  
  return clamp(total_satisfaction / max(|required|, 1), 0.0, 1.0)
```

#### 5d. repair_completion 判定

```
repair_completion(node_id):
  required = Registry.query_entity(node_id).required_resources
  if |required| == 0: return false
  
  for each (rid, required_qty) in required:
    if deposited.get(rid, 0) < required_qty:
      return false
  return true
```

#### 5e. 6 路下游触发链

```
submit_deposit() → repair_completion() == true:
  1. repair_state = REPAIRED  (状态机转换)
  2. emit repair_completed(node_id)
     ├─→ #6 IntelManager.on_repair_completed(node_id)
     │     → 重评估 ability.lighthouse-signal-interpretation Path C 解锁条件
     ├─→ #9 Chart.on_route_enhanced(route_id, {effect: hazard_reduction, magnitude: 0.3})
     │     → route.sky-reef-arc-01 从不可通行变为可通行 + hazard 降低 30%
     ├─→ #14 Settlement (消费 repair_completed 信号)
     │     → NPC 活跃度提升、对话解锁
     ├─→ #3 Persistence.capture_snapshot("progress.world-repair", snapshot)
     │     → 存档检查点
     ├─→ #17 Feedback (消费 visual_state_changed 信号)
     │     → visual_state_anchor = "repaired" → 灯塔重亮动画
     └─→ UI (#16) — toast: "天礁灯塔 已修复" + 解锁摘要
```

### 6. ADR-0003 序列化

```gdscript
# 在 feature_ready 阶段注册
func _on_feature_ready() -> void:
    Persistence.register_domain_serializer("world-repair", _serialize_world_repair)

func _serialize_world_repair() -> Dictionary:
    var serialized_nodes := {}
    for node_id in repair_nodes:
        var node := repair_nodes[node_id]
        serialized_nodes[node_id] = {
            "repair_state": node.repair_state,
            "deposited": node.deposited.duplicate(true),
        }
    return {
        "domain_id": "world-repair",
        "nodes": serialized_nodes,
    }

func _deserialize_world_repair(snapshot: Dictionary) -> void:
    for node_id in snapshot.nodes:
        var data := snapshot.nodes[node_id]
        repair_nodes[node_id] = {
            "repair_state": data.repair_state,
            "deposited": data.deposited,
            "repair_progress": _compute_repair_progress(node_id, data.deposited),
        }
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    WorldRepair (Autoload #13)                              │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │              STATE STORAGE (Dictionary)                    │           │
│  │                                                            │           │
│  │  repair_nodes: Dict[StringName, RepairNodeState]          │           │
│  │    repair_node.starlight_dock: {                           │           │
│  │      repair_state: UNREVEALED | KNOWN | REPAIRED          │           │
│  │      deposited: {repair_kit: int, basic_supply: int}      │           │
│  │      repair_progress: float (0.0–1.0)                     │           │
│  │    }                                                       │           │
│  └──────────────────────────────────────────────────────────┘           │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          UPSTREAM (consumes)                                       │   │
│  │                                                                    │   │
│  │  Registry (#1)  ──→ query_entity(node_id) → 读取修复节点静态定义  │   │
│  │  Resources (#5) ──→ can_deposit / commit_deposit / deposit_committed│  │
│  │  Intel (#6)     ──→ query_knowledge_state (UI 提示精度用)         │   │
│  │  Exploration(#11)──→ on_player_arrived_at_repair_node              │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          DOWNSTREAM (provides)                                      │   │
│  │                                                                    │   │
│  │  Intel (#6)     ←── on_repair_completed → 能力解锁重评估           │   │
│  │  Chart (#9)     ←── on_route_enhanced → 航线解锁 + hazard 降低     │   │
│  │  Settlement(#14)←── repair_completed 信号 → NPC 活跃度             │   │
│  │  Persistence(#3)←── progress.world-repair snapshot                 │   │
│  │  Feedback (#17) ←── visual_state_changed → 灯塔重亮动画            │   │
│  │  UI (#16)       ←── repair_progress_changed / repair_completed     │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          SIGNALS (3 typed, emit-after-mutation)                    │   │
│  │                                                                    │   │
│  │  repair_progress_changed(node_id, progress: float, deposited: Dict)│   │
│  │  repair_completed(node_id: StringName)                              │   │
│  │  visual_state_changed(node_id, visual_state: StringName)           │   │
│  └────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

#### 修复节点静态定义 (Registry #1)

```gdscript
# Registry 中 kind=repair_node 的实体结构
# WorldRepair 在初始化时通过 query_entity 读取，不硬编码
# MVP 定义:
# {
#   "node_id": &"repair_node.starlight_dock",
#   "name": "天礁灯塔",
#   "linked_location_id": &"location.glass-harbor-outskirts",
#   "required_resources": [
#     {&"resource.repair_kit": 4},
#     {&"resource.basic_supply": 4}
#   ],
#   "unlocked_routes": [&"route.sky-reef-arc-01"],
#   "route_enhancement": {
#     "effect": &"hazard_reduction",
#     "magnitude": 0.3
#   },
#   "pre_repair_route_state": {
#     "traversable": false
#   },
#   "visual_state_anchor": &"anchor.starlight_dock_beacon"
# }
```

#### submit_deposit 返回结构

```gdscript
# submit_deposit 返回 Dictionary:
# {
#   "result": int,           # ResourceResult enum
#   "violations": Array,     # 仅在验证失败时填充
#   "deposited": Dictionary, # 更新后的 deposited 计数器
#   "progress": float,       # 更新后的 repair_progress
#   "completed": bool,       # repair_completion() 结果
# }
```

#### MVP 起始状态

```gdscript
func _init_new_game_state() -> void:
    repair_nodes[&"repair_node.starlight_dock"] = {
        "repair_state": REPAIR_STATE_UNREVEALED,
        "deposited": {},
        "repair_progress": 0.0,
    }
```

## Alternatives Considered

### Alternative A: 修复材料一次性提交（不分批）

- **Description**: 玩家必须一次性携带全部所需材料才能发起修复，不存在 `deposited` 计数器
- **Pros**: 状态机更简单——只有 unrevealed→repaired 两态；无需分批验证逻辑；无需进度持久化
- **Cons**: 强制玩家一次性携带所有材料——若容量不足则无法修复；失去了"分批投入、渐进反馈"的仪式感；中断了"每次归来都修补一点"的 Pillar 3 幻想
- **Rejection Reason**: GDD #13 明确选择了分批提交模型——"同一节点的不同材料可跨多次访问分批提交"。分批提交支撑了"每次归来都修补"的核心幻想和灯塔渐亮的美学渐进

### Alternative B: 修复节点状态由 Registry 直接管理

- **Description**: 修复节点的运行时状态存储在 Registry 的可变字段中，而非独立 WorldRepair Autoload
- **Pros**: 减少一个 Autoload；数据与定义在同一个系统中
- **Cons**: Registry 的架构边界的声明是"仅拥有静态内容定义"——添加可变运行时状态违反其 Foundation 层合约；所有读取修复状态的系统需要依赖 Registry 而非 WorldRepair；恢复存档时需要将可变状态注入静态 Registry
- **Rejection Reason**: ADR-0001 和 TD-SYSTEM-BOUNDARY 审查明确要求 Registry 不拥有可变运行时状态。修复状态是 Progression 层可变状态——属于 WorldRepair Autoload

### Alternative C: 修复完成触发使用多个独立信号

- **Description**: 不使用单一的 `repair_completed` 信号，而是分别发射 `ability_unlock_triggered`、`route_enhanced`、`npc_state_changed` 等细粒度信号
- **Pros**: 每个下游系统只连接自己需要的信号——更精确的订阅
- **Cons**: 信号级联深度增加（WorldRepair → 5 个信号 → 5 个系统处理）；发射顺序管理复杂；新增消费方需要新增信号类型；违反 ADR-0002 的 fan-out 优于 deep-chain 原则
- **Rejection Reason**: GDD #13 定义 `repair_completed` 为单一触发事件。6 个下游系统各自从同一信号消费并执行各自逻辑——这是 ADR-0002 推荐的 fan-out 模式。下游消费顺序由各系统自身的信号连接顺序决定（Godot signal 按连接顺序同步调用）

## Consequences

### Positive

- **单一修复权威**: WorldRepair 是所有修复状态的唯一 owner——消除了多系统各自追踪修复进度的不一致风险
- **分批提交 + 持久化**: deposited 计数器跨会话保留——玩家可以在任意时刻中断并继续修复，支撑"每次归来都修补"的幻想
- **物理到达优先于情报门控**: 玩家总是可以与修复节点交互——知识状态只影响 UI 提示精度，不阻止交互。这确保 Pillar 2（世界会回应照料）不被情报系统意外门控
- **fan-out 触发链**: repair_completed 单一信号 → 6 个下游系统并行响应——新增消费方只需连接同一信号
- **Registry 驱动**: 修复节点定义（材料清单、关联航线、增强效果）从 Registry 读取——添加新修复节点不需要修改 WorldRepair 代码

### Negative

- **Autoload #13**: 增加了 Phase 5 启动约束——WorldRepair 依赖 Registry (#1)、Resources (#5)、Intel (#6) 已初始化
- **submit_deposit 与 commit_deposit 的协作复杂度**: 两个系统需要协调"验证→提交→计数器更新→完成判定"的原子链——若 commit_deposit 成功但 deposited 更新前崩溃，需要从存档恢复
- **修复仪式视觉在 MVP 中由 WorldRepair 直接管理**: 灯塔重亮动画、光束、粒子由 WorldRepair 触发——未来迁移至 #17 Feedback 时需要重构视觉层

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| commit_deposit 成功但 deposited 计数器更新前崩溃 | Very Low — 单线程同步执行 | Medium — deposited 与实际 Pool 6 状态不一致 | 存档恢复时从 Pool 6 的终态记录重建 deposited 计数器（而非仅信任 snapshot 中的 deposited 字段） |
| 修复节点静态定义在 Registry 中缺失或格式错误 | Low — Registry 验证在启动时运行 | Medium — 修复功能不可用 | 初始化时验证所有 registered repair_nodes——缺失定义则记录错误并跳过该节点 |
| 多个修复节点时 submit_deposit 的 node_id 混淆 | Low — MVP 仅 1 个节点 | Low | 节点选择由 Hub/Exploration 的物理交互锚点保证——玩家站在哪个节点前就提交到哪个节点 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| world-repair-unlock.md | 3 态状态机: unrevealed→known→repaired, known→repaired 单向不可逆 | 状态机定义 (§5a) + REPAIR_STATE_* 枚举 + 无效转换拒绝 |
| world-repair-unlock.md | 物理到达触发 unrevealed→known，情报门控仅影响 UI 提示精度 | on_player_arrived_at_repair_node() — 无情报前置条件 |
| world-repair-unlock.md | 分批提交: deposit_validation → commit_deposit → deposited 计数器 | validate_deposit() + submit_deposit() 完整算法链 |
| world-repair-unlock.md | 5 种 deposit_validation violation | VIOLATION_* 常量 + validate_deposit 算法 |
| world-repair-unlock.md | repair_completion 判定 + repair_progress 计算 | repair_completion() + repair_progress() 公式 |
| world-repair-unlock.md | 修复完成后 6 路触发: #6 能力解锁, #9 航线增强, #14 NPC, #3 存档, #17 视觉, UI toast | 6 路下游触发链 (§5e) + repair_completed 信号 fan-out |
| world-repair-unlock.md | 不可逆性: Pool 6 终态 + 已修复节点 ERR_ALREADY_REPAIRED | 状态机幂等守卫 + commit_deposit 终态 |
| world-repair-unlock.md | MVP 修复节点: starlight_dock 定义（材料、航线、增强） | Registry 静态定义结构 (§Key Interfaces) |
| world-repair-unlock.md | MVP 视觉: 灯塔发光 sprite, 光晕呼吸动画, 光束, 粒子 | visual_state_changed 信号 + visual_state_anchor = repaired → 视觉规格由本 ADR 引用 GDD 中的 MVP 视觉表 |
| world-repair-unlock.md | 分批提交中途存档→读档进度一致 (AC-15) | ADR-0003 serializer — deposited 计数器完整序列化 |

## Performance Implications

- **CPU**: 所有操作 O(R) 其中 R = required_resources 数量 (MVP: 2 种)。validate_deposit: 遍历 offer 条目 + required_resources — < 0.01ms。repair_progress: 遍历 required_resources — < 0.01ms。submit_deposit: commit_deposit (#5) + 计数器更新 + completion 判定 + 最多 6 个同步信号 — < 0.1ms
- **Memory**: MVP 1 个修复节点 × ~200 bytes。总计 < 500 bytes
- **Load Time**: 启动时从 Persistence snapshot 恢复 — 反序列化 < 0.5ms
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. 在 project.godot 中注册 WorldRepair 为 Autoload #13
2. 实现 Dictionary 状态结构和 REPAIR_STATE_* 枚举
3. 实现 on_player_arrived_at_repair_node() — unrevealed→known 转换
4. 实现 validate_deposit() — 5 种 violation 覆盖
5. 实现 submit_deposit() — 验证→提交→计数器→完成判定→下游触发
6. 实现 repair_progress() 和 repair_completion() 公式
7. 实现 ADR-0003 serializer/deserializer 注册
8. 实现 MVP 起始状态（starlight_dock unrevealed）
9. 实现修复完成视觉: sprite 切换 + modulate 呼吸动画 + 光束 + 粒子 (GDD MVP 视觉规格)
10. 单元测试: 全部 5 种 deposit_validation violation, repair_completion 触发, ERR_ALREADY_REPAIRED 守卫, 分批提交进度计算, 存档→读档 deposited 一致性, 全部 3 种状态机无效转换拒绝

## Validation Criteria

- 3 态状态机全部有效转换通过；全部 4 种无效转换被拒绝
- deposit_validation 全部 5 种 violation 类型正确触发
- repair_completion 在最后一种材料提交后返回 true → known→repaired
- 已修复节点 submit_deposit 返回 ERR_ALREADY_REPAIRED
- repair_completed 信号发射后: #6 能力解锁、#9 航线可通行 + hazard 降低 30%
- 分批提交: 提交 repair_kit×3 → progress=0.375 → 再提交 repair_kit×1 + basic_supply×4 → progress=1.0 → repaired
- 存档→读档: deposited 计数器一致，repair_progress 一致，可继续提交
- 物理到达 unrevealed 节点: 状态推进至 known，交互入口可用，材料清单中未通过情报确认的资源显示"？"
- 灯塔视觉: known 状态灰暗/破损 → repaired 状态发光 + 光晕呼吸 + 光束 + 粒子

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — WorldRepair 为 Autoload #13，Phase 5 启动
- **ADR-0002**: Signal 通信协议 — 3 signals typed params, sync emit, fan-out 模式
- **ADR-0003**: 存档系统 — `progress.world-repair` snapshot package
- **ADR-0005**: 资源池系统 — can_deposit / commit_deposit / Pool 6 终态
- **ADR-0007**: 知识状态 — on_repair_completed 触发能力解锁 Path C
- **GDD #13**: world-repair-unlock.md — 完整修复流程、状态机、公式、边缘情况
- **GDD #1**: content-data-state-registry.md — 修复节点静态定义
- **GDD #14**: port-village-market.md — 消费 repair_completed 驱动 NPC 变化
