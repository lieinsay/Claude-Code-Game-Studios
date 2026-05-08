# ADR-0014: 空港/村镇状态与集市交易 — SettlementManager Autoload #14

## Status
Accepted

## Date
2026-05-08

## Summary
SettlementManager 作为 Autoload #14，管理每个定居点的摊位状态、NPC 活跃度和集市购买流程。消费 WorldRepair (#13) 的 `repair_completed` 信号以驱动摊位从 `closed` 解锁至 `open_basic`（MVP 终态），NPC 从 `absent` 恢复至 `idle`。购买流程委托 ResourcesManager (#5) 的 `validate_purchase` / `execute_purchase` 执行——本系统拥有商品定义与定价，但不拥有货物所有权。MVP 规模为 1 个定居点（琉璃港 Glass Harbor），4 个固定摊位（1 个默认杂货摊 + 3 个修复解锁摊），6 种商品（2 通用补给 + 3 独占风味补给 + 1 情报），定居点三态状态机（dormant → recovering → active）。修复是不可逆的——状态只向前推进。货币的唯一获取途径是 #11 探索中的搜索点产出（`currency.cloud-coins`）。所有状态以 Dictionary[StringName, Variant] 存储，通过 ADR-0003 Canonical JSON 快照包持久化为 `progress.settlement-market`。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Feature — World/Economy |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构、状态机、信号，无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/port-village-market.md`, `docs/architecture/architecture.md`, `design/gdd/world-repair-unlock.md`, `design/gdd/resources-goods-capacity.md`, `design/gdd/exploration-scavenge-scenario.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | 购买流程 validate→execute 原子性；repair_completed 信号 → 摊位解锁 + NPC 恢复；dormant→recovering→active 聚合状态判定；settlement-market snapshot 往返序列化；杂货摊默认开启；重复 repair 信号去重；16 个边缘情况覆盖 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #14 启动顺序, Phase 5 feature_ready)；ADR-0002 (Signal 通信协议)；ADR-0003 (快照包持久化 — progress.settlement-market)；ADR-0004 (InteractionHandler @abstract — 摊位焦点注册与 use_requested 分发)；ADR-0005 (ResourcesManager — validate_purchase / execute_purchase / get_storage_summary)；ADR-0011 (WorldRepair — repair_completed 信号, linked_location_id 查询) |
| **Enables** | ADR-0015 (Partner — NPC 活跃度可能影响伙伴关系叙事)；ADR-0016 (Feedback — 集市视觉/音频反馈触发: 摊位开张动画、环境音切换) |
| **Blocks** | N/A — Settlement 为 Feature 层终端系统，消费上游信号但不产出被其他系统依赖的新信号（交易结果由 #5 的 purchase_executed 信号传达） |
| **Ordering Note** | 应在 ADR-0011 (WorldRepair) 和 ADR-0005 (ResourcesManager) 之后 Accepted — 核心交互依赖 repair_completed 信号消费和 purchase 接口 |

## Context

### Problem Statement

《云海织航》的空港/村镇集市是玩家消耗探索中获得货币的出口，也是世界修复反馈在村镇侧的可视化载体。GDD #14 定义了 1 个定居点（琉璃港）、4 个固定摊位（NPC 经营）、6 种商品、定居点/摊位/NPC 三层状态机、修复驱动的摊位解锁逻辑和购买流程。但 Settlement 的 Autoload 定位、状态存储结构、信号契约、与 ResourcesManager (#5) 的购买委托边界、与 WorldRepair (#13) 的修复信号消费合同、以及与 Persistence (#3) 的快照格式未在 ADR 中形式化。没有这个 ADR，村镇状态的核心 invariants——修复不可逆（状态只推进）、默认杂货摊始终可交互（确保至少一个购买点）、购买原子性（validate→execute 在同一帧内完成）——会在实现中被分散，失去单一权威来源。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑，无引擎 API 风险
- **ADR-0002 信号协议**: typed params, sync emit, max depth 2, emit-after-mutation
- **ADR-0003 持久化**: `progress.settlement-market` snapshot package
- **ADR-0005 ResourcesManager**: validate_purchase / execute_purchase — 购买原子操作，货物进入 Pool 2 (in_storage)，货币从 player_currency 扣除
- **ADR-0011 WorldRepair**: repair_completed(node_id) 信号消费 — 摊位解锁、NPC 恢复
- **ADR-0001 启动顺序**: SettlementManager 在 Phase 5 (feature_ready) 初始化
- **MVP 边界**: 1 个定居点（琉璃港），4 个摊位（1 default + 3 repair-unlock），6 种商品，无价格模拟/供需模拟

### Requirements

- 3 层状态机: 定居点 (dormant → recovering → active)、摊位 (closed → open_basic)、NPC (absent → idle)
- 修复驱动: repair_completed(node_id) → F.2 判定 → 匹配摊位从 closed→open_basic, NPC absent→idle
- 购买流程: use_requested → 打开摊位界面 → 选择商品 → validate_purchase → execute_purchase
- 6 种 MVP 商品: 2 通用补给 + 3 独占风味补给 + 1 情报
- 默认杂货摊始终 open_basic — 确保任何修复完成前至少一个购买点
- 修复不可逆 — 状态只向前推进，无退化
- 货币来源: #11 探索搜索点产出 `currency.cloud-coins`

## Decision

### 1. SettlementManager 作为 Autoload #14

SettlementManager 在 Phase 5 (feature_ready) 中初始化。`_ready()` 仅执行信号声明、常量定义和默认杂货摊初始化；修复信号连接在收到 `feature_ready` 后执行。

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
# === SettlementManager 状态结构 ===

# 定居点状态: StringName → SettlementState
# SettlementState = {
#   settlement_state: int,     # 0=DORMANT, 1=RECOVERING, 2=ACTIVE
#   completed_node_ids: Array, # Array[StringName] — 已完成修复节点 ID 集合
# }
var settlements: Dictionary = {}  # Dictionary[StringName, Dictionary]

# 摊位状态: StringName → StallState
# StallState = {
#   stall_state: int,       # 0=CLOSED, 1=OPEN_BASIC, 2=OPEN_EXPANDED (post-MVP)
#   settlement_id: StringName,
# }
var stalls: Dictionary = {}  # Dictionary[StringName, Dictionary]

# NPC 状态: StringName → NpcState
# NpcState = {
#   npc_state: int,         # 0=ABSENT, 1=IDLE, 2=ACTIVE (post-MVP)
#   stall_id: StringName,
# }
var npcs: Dictionary = {}  # Dictionary[StringName, Dictionary]
```

**常量定义：**

```gdscript
# 定居点状态枚举
const SETTLEMENT_DORMANT: int = 0
const SETTLEMENT_RECOVERING: int = 1
const SETTLEMENT_ACTIVE: int = 2

# 摊位状态枚举
const STALL_CLOSED: int = 0
const STALL_OPEN_BASIC: int = 1
const STALL_OPEN_EXPANDED: int = 2  # post-MVP

# NPC 状态枚举
const NPC_ABSENT: int = 0
const NPC_IDLE: int = 1
const NPC_ACTIVE: int = 2  # post-MVP

# 购买失败原因常量
const PURCHASE_FAIL_CAPACITY: StringName = &"capacity_full"
const PURCHASE_FAIL_FUNDS: StringName = &"insufficient_funds"
```

### 3. 信号接口

```gdscript
# === 摊位状态变更 ===
signal stall_opened(stall_id: StringName, settlement_id: StringName)
signal stall_state_changed(stall_id: StringName, old_state: int, new_state: int)

# === NPC 状态变更 ===
signal npc_state_changed(npc_id: StringName, old_state: int, new_state: int)

# === 购买事件 ===
signal purchase_completed(good_id: StringName, quantity: int, total_cost: int)
signal purchase_failed(good_id: StringName, reason: StringName)

# === 定居点活跃度 ===
signal settlement_activity_changed(settlement_id: StringName, active_stall_count: int)
```

### 4. 方法接口

#### 4a. 购买流程

```gdscript
# 接收 use_requested (由 #4 InteractionRegistry 分发)
func on_use_requested(target_id: StringName) -> void:
    # 1. 验证 target_id 是否为已开启摊位
    # 2. 若是 → 发射信号通知 UI 打开摊位界面
    # 3. 若否（closed 摊位不应被注册为焦点目标）→ 无操作

# 获取摊位商品列表
func get_stall_goods(stall_id: StringName) -> Array:
    # 返回该摊位当前解锁等级下所有可用商品
    # 从 Registry 读取 good 定义，按 stall_id 和 unlock_level 过滤

# 验证购买
func validate_purchase_request(stall_id: StringName, good_id: StringName, quantity: int) -> Dictionary:
    # 返回 {valid: bool, reason: StringName, total_cost: int}
    # 1. 验证 stall_id 已开启
    # 2. 验证 good_id 在当前解锁等级下可用
    # 3. 计算 total_cost = price × quantity (F.1)
    # 4. 委托 #5.validate_purchase(good_id, quantity) — 检查货币+容量
    # 5. 返回结果

# 执行购买
func execute_purchase(stall_id: StringName, good_id: StringName, quantity: int) -> Dictionary:
    # 返回 {success: bool, good_id: StringName, quantity: int, total_cost: int}
    # 1. 再次 validate（防御性）
    # 2. #5.execute_purchase(good_id, quantity) — 扣除货币、转移货物
    # 3. 发射 purchase_completed
    # 4. 触发持久化快照
```

#### 4b. 修复驱动

```gdscript
# 消费 repair_completed 信号
func on_repair_completed(node_id: StringName) -> void:
    # 1. 查询 node_id 的 linked_location_id (通过 #13 或直接查询 Registry)
    # 2. 匹配 linked_location_id 所属的 settlement
    # 3. 将 node_id 加入该 settlement 的 completed_node_ids (集合去重)
    # 4. 遍历该 settlement 所有摊位:
    #    若 stall.required_node_ids ∩ completed_node_ids 满足 unlock_threshold
    #    → 摊位 closed→open_basic, NPC absent→idle
    # 5. 重算 settlement 活跃度 (F.3)
    # 6. 触发持久化快照
```

#### 4c. 查询接口

```gdscript
# 定居点查询
func get_settlement_state(settlement_id: StringName) -> int
func get_stall_state(stall_id: StringName) -> int
func get_npc_state(npc_id: StringName) -> int

# 获取定居点活跃摊位数量
func get_active_stall_count(settlement_id: StringName) -> int

# 获取玩家可交互的摊位列表（供 #4 注册焦点目标）
func get_interactive_stalls(settlement_id: StringName) -> Array
```

### 5. 核心算法

#### 5a. 三层状态机

```
Settlement:  DORMANT ──[首个修复]──→ RECOVERING ──[全部修复]──→ ACTIVE

Stall:       CLOSED ──[匹配修复]──→ OPEN_BASIC ──[第二个修复]──→ OPEN_EXPANDED (post-MVP)

NPC:          ABSENT ──[摊位开启]──→ IDLE ──[expanded]──→ ACTIVE (post-MVP)
```

**无效转换（状态机拒绝）：**
- `CLOSED → OPEN_EXPANDED`: 拒绝 — 必须经过 OPEN_BASIC
- `OPEN_BASIC → CLOSED`: 拒绝 — 修复不可逆
- `ABSENT → ACTIVE`: 拒绝 — 必须经过 IDLE
- 任何反向转换 (recovering→dormant, active→recovering): 拒绝 — 修复是永久的

#### 5b. F.1 购买总价

```gdscript
func calculate_total_cost(good_id: StringName, quantity: int) -> int:
    var good_def := Registry.query_entity(good_id)
    var price: int = good_def.get("price", 0)
    return price * quantity
```

#### 5c. F.2 摊位解锁判定

```gdscript
func is_stall_unlocked(stall_id: StringName, completed_node_ids: Array) -> bool:
    var stall_def := _get_stall_def(stall_id)
    var required: Array = stall_def.get("required_node_ids", [])
    if required.size() == 0:
        return false  # EC-11: 空 required_node_ids → 永不自动解锁
    for node_id in required:
        if node_id in completed_node_ids:
            return true  # unlock_threshold_basic = 1
    return false
```

#### 5d. F.3 定居点活跃度

```gdscript
func recalculate_settlement_activity(settlement_id: StringName) -> void:
    var active_count := 0
    var total_stalls := 0
    for stall_id in _get_settlement_stalls(settlement_id):
        total_stalls += 1
        if stalls[stall_id].stall_state >= STALL_OPEN_BASIC:
            active_count += 1

    var new_state: int
    if active_count == 0:
        new_state = SETTLEMENT_DORMANT
    elif active_count < total_stalls:
        new_state = SETTLEMENT_RECOVERING
    else:
        new_state = SETTLEMENT_ACTIVE

    if new_state != settlements[settlement_id].settlement_state:
        settlements[settlement_id].settlement_state = new_state
        settlement_activity_changed.emit(settlement_id, active_count)
```

### 6. ADR-0003 序列化

```gdscript
func _serialize_settlement() -> Dictionary:
    var serialized_settlements := {}
    for sid in settlements:
        var s := settlements[sid]
        serialized_settlements[sid] = {
            "settlement_state": s.settlement_state,
            "completed_node_ids": s.completed_node_ids.duplicate(true),
        }

    var serialized_stalls := {}
    for sid in stalls:
        serialized_stalls[sid] = {
            "stall_state": stalls[sid].stall_state,
            "settlement_id": stalls[sid].settlement_id,
        }

    var serialized_npcs := {}
    for nid in npcs:
        serialized_npcs[nid] = {
            "npc_state": npcs[nid].npc_state,
            "stall_id": npcs[nid].stall_id,
        }

    return {
        "domain_id": "settlement-market",
        "settlements": serialized_settlements,
        "stalls": serialized_stalls,
        "npcs": serialized_npcs,
    }
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    SettlementManager (Autoload #14)                         │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │              STATE STORAGE (Dictionary)                        │       │
│  │  settlements: Dict[StringName, SettlementState]               │       │
│  │    settlement.glass-harbor: {state: DORMANT|RECOVERING|ACTIVE,│       │
│  │      completed_node_ids: [repair_node.starlight_dock]}        │       │
│  │  stalls: Dict[StringName, StallState]                         │       │
│  │    stall.gh-lens-workshop: {state: CLOSED|OPEN_BASIC}         │       │
│  │  npcs: Dict[StringName, NpcState]                             │       │
│  │    npc.wei: {state: ABSENT|IDLE}                              │       │
│  └──────────────────────────────────────────────────────────────┘       │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          UPSTREAM (consumes)                                       │   │
│  │  WorldRepair(#13)──→ repair_completed(node_id) → 摊位解锁          │   │
│  │  Resources (#5) ──→ validate_purchase / execute_purchase          │   │
│  │  Interaction(#4)──→ use_requested(stall_id) → 打开购买界面         │   │
│  │  Registry (#1)  ──→ 摊位定义、商品定义、NPC 定义                   │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          DOWNSTREAM (provides)                                      │   │
│  │  Resources (#5) ←── 购买触发 validate+execute                     │   │
│  │  Persistence(#3)←── progress.settlement-market snapshot           │   │
│  │  UI (#16)      ←── stall_opened, purchase_completed/failed        │   │
│  │  Interaction(#4)←── get_interactive_stalls (焦点注册)              │   │
│  │  Feedback (#17)←── stall_opened, npc_state_changed                │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │          3-TIER STATE MACHINE                                   │       │
│  │  Settlement: DORMANT → RECOVERING → ACTIVE                     │       │
│  │  Stall:      CLOSED → OPEN_BASIC → OPEN_EXPANDED (post-MVP)    │       │
│  │  NPC:        ABSENT → IDLE → ACTIVE (post-MVP)                 │       │
│  │  All transitions forward-only — 修复不可逆                       │       │
│  └──────────────────────────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────────────────────────┘
```

## Alternatives Considered

### Alternative A: 摊位界面由 SettlementManager 直接管理

- **Description**: SettlementManager 直接创建和管理购买 UI 节点
- **Pros**: 逻辑和 UI 在同一系统中——简化通信
- **Cons**: Autoload 不应拥有场景节点引用（ADR-0001）；单元测试需要 mock UI；违反逻辑/表现分离
- **Rejection Reason**: 与 ADR-0001 和 ADR-0013 的逻辑/场景分离原则一致——SettlementManager 通过信号通知 UI 层 (#16) 打开/更新购买界面

### Alternative B: 商品购买直接在 #5 Resources 中处理，不经过 Settlement

- **Description**: 摊位只是触发点——use_requested 直接调用 Resources.purchase(good_id)
- **Pros**: 更少的中介层——Settlement 仅状态管理，不参与购买流
- **Cons**: 摊位解锁等级与商品可见性的关联逻辑无处归属；local_identity_tag 等风味数据需要 Settle layer 提供；价格验证需要知道当前摊位上下文
- **Rejection Reason**: GDD 定义摊位为购买流的权威入口——解锁等级决定商品可见性。Settlement 拥有商品-摊位关联和价格上下文，#5 只负责资源转移

### Alternative C: 每个摊位使用独立 Resource/场景实例

- **Description**: 每个摊位是独立的 Godot 场景实例，拥有自己的状态节点
- **Pros**: 场景编辑器可视化；摊位可独立加载/卸载
- **Cons**: 状态分散在多个场景实例中——持久化需要遍历场景树；修复信号需要通知所有实例；与 Dictionary 统一存储模式不一致
- **Rejection Reason**: MVP 仅 4 个摊位——集中式 Dictionary 存储更简单、持久化更容易、测试更直接

## Consequences

### Positive

- **单一村镇权威**: SettlementManager 是所有定居点/摊位/NPC 状态的唯一 owner
- **购买委托清晰**: Settlement 拥有"什么可以买、多少钱"，#5 拥有"资源怎么转移"——边界明确
- **修复不可逆保证**: 状态只向前推进——修复反馈永久可见
- **默认杂货摊始终可用**: 确保任何修复完成前玩家至少有 1 个购买点
- **Registry 驱动**: 摊位定义、商品定义、价格、NPC 均从 Registry 读取——添加新定居点不需要修改 SettlementManager 代码

### Negative

- **Autoload #14**: 增加了 Phase 5 启动约束
- **风味商品机械等价**: 3 种独占风味商品与基础物资包同价同功能——仅通过 display_name/local_identity_tag 区分身份。可能被玩家视为"换皮商品"
- **无库存耗尽**: 商品解锁后始终可购买——缺乏资源稀缺性。Post-MVP 可引入库存限制

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| 修复节点 linked_location_id 未在 Registry 中定义或格式错误 | Low | Medium — 摊位永不解锁 | 初始化时验证所有 stall 的 required_node_ids 在 Registry 中存在 |
| 购买界面打开期间修复信号到达导致商品列表变化 | Low | Low — 当前会话商品列表不变 | E.9 已处理——关闭重开后反映新列表 |
| 货币来源（#11 探索）产出速率与商品定价不匹配 | Medium | Medium — 玩家始终买不起或钱太多 | G.5 联动约束：单次探索期望产出 150-240，覆盖 1-2 个基础物资包 (50-100) |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| port-village-market.md | 规则 1: 4 个固定摊位 + NPC 经营 | stalls + npcs Dictionary + Registry 驱动定义 |
| port-village-market.md | 规则 2: 商品定义 (good_id, price, category, local_identity_tag) | Registry good 实体结构 |
| port-village-market.md | 规则 3: 10 步购买流程 | validate_purchase_request → execute_purchase |
| port-village-market.md | 规则 4: 修复驱动摊位变化 | on_repair_completed + F.2 解锁判定 |
| port-village-market.md | 规则 5: NPC 三态 (absent/idle/active) | NPC_* 枚举 + npc_state_changed 信号 |
| port-village-market.md | 规则 6: 商品可见性 — 解锁后始终可购买 | get_stall_goods() 按 unlock_level 过滤 |
| port-village-market.md | 规则 7: 商品分类 (supply/material/intel/local) | 6 种 MVP 商品 |
| port-village-market.md | F.1 购买总价 | calculate_total_cost() |
| port-village-market.md | F.2 摊位解锁判定 | is_stall_unlocked() |
| port-village-market.md | F.3 定居点活跃度 | recalculate_settlement_activity() |
| port-village-market.md | E.1-E.16 全部 16 个边缘情况 | 方法接口守卫 + 信号合同覆盖 |
| port-village-market.md | AC-H.1.1–H.6.8 全部验收条件 | 方法接口 + 信号合同 + 状态机定义覆盖 |

## Performance Implications

- **CPU**: F.1 total_cost: O(1) 乘法 — <0.001ms。F.2 解锁判定: O(R) 其中 R=required_node_ids (MVP: 1-2) — <0.001ms。F.3 聚合: O(S) 其中 S=摊位数 (MVP: 4) — <0.001ms。购买流程: validate + execute 在 #5 中执行 — <0.1ms
- **Memory**: 1 定居点 × ~200 bytes + 4 摊位 × ~100 bytes + 4 NPC × ~80 bytes。总计 <1KB
- **Load Time**: 启动时从 Persistence snapshot 恢复 — 反序列化 <0.5ms
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. 在 project.godot 中注册 SettlementManager 为 Autoload #14
2. 实现 3 层状态机枚举 + Dictionary 状态结构
3. 实现 on_repair_completed() 信号消费 + F.2 解锁判定
4. 实现 F.3 定居点活跃度聚合
5. 实现 validate_purchase_request() + execute_purchase() 购买流程
6. 实现 get_stall_goods() 按解锁等级过滤商品
7. 实现 get_interactive_stalls() 供 #4 焦点注册
8. 实现 ADR-0003 serializer/deserializer
9. 初始化 MVP: 琉璃港 4 摊位, 杂货摊默认 open_basic, 其余 closed
10. 单元测试: 3 层状态机有效/无效转换, F.1/F.2/F.3 公式, 购买流程验证+执行, 修复信号解锁, 默认杂货摊, 重复信号去重, 存档往返

## Validation Criteria

- 4 个 MVP 摊位定义正确（stall.gh-lens-workshop/sail-shop/chart-studio/general）
- 杂货摊默认 open_basic + NPC 阿图 idle——新游戏可直接交互
- repair_completed(starlight_dock) → 匹配的摊位 (sail-shop) closed→open_basic + NPC absent→idle
- 重复 repair_completed 信号不重复解锁——completed_node_ids 集合去重
- validate_purchase 返回正确失败原因（capacity_full / insufficient_funds）
- execute_purchase 扣除货币 + 转移货物至 Pool 2
- settlement dormant (1 active) → recovering (2-3) → active (4)
- 存档→读档: 摊位状态、NPC 状态、completed_node_ids 一致
- 商品 price=0 时购买不扣货币但记录 error 日志
- 所有 16 个边缘情况正确处理

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — SettlementManager 为 Autoload #14，Phase 5 启动
- **ADR-0002**: Signal 通信协议 — 6 signals typed params, sync emit
- **ADR-0003**: 存档系统 — `progress.settlement-market` snapshot package
- **ADR-0004**: InteractionHandler — 摊位焦点注册与 use_requested 分发
- **ADR-0005**: 资源池系统 — validate_purchase / execute_purchase 委托
- **ADR-0011**: 世界修复 — repair_completed 信号消费 + linked_location_id 查询
- **ADR-0013**: 探索系统 — 货币唯一来源 (search_yield → currency.cloud-coins)
- **GDD #14**: port-village-market.md — 完整村镇设计
