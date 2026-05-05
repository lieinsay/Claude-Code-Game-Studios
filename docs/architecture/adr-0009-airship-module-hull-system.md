# ADR-0009: 飞艇模块与船体伤害模型 — AirshipModuleSystem Autoload #8

## Status
Proposed

## Date
2026-05-05

## Summary
AirshipModuleSystem 作为 Autoload #8，管理飞艇的双模块槽位（A/B）、船体完整性（0-100/4 波段）、动力炉载重模型、以及适航判定。2 个模块槽位均为开放槽位，支持同型双装或异型搭配。每个槽位维护双域状态（actual_state / visible_state）驱动效率系数计算。6 个 typed signal 遵循 emit-after-mutation 时序。swap_module 两阶段操作（验证→执行）保证原子性。所有状态通过 ADR-0003 Canonical JSON 快照包持久化。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Game Logic |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构与信号，无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/airship-modules-hull-state.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | swap_module 两阶段原子性（验证失败不回退）；双域状态机 unchecked→inspect/repair 路径正确性；can_depart() 多阻断条件同时返回；trapped 货物在 V_effective 恢复后自动释放 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #8 启动顺序, Phase 4 foundation_ready)；ADR-0002 (Signal 通信协议)；ADR-0003 (快照包持久化)；ADR-0005 (ResourcesManager — consume_for_module, get_total_loaded_mass, cargo_module_volume_bonus, repair_value)；ADR-0006 (Web 单线程约束)；ADR-0007 (ability_state 查询) |
| **Enables** | ADR-0010 (EncounterContext — 消费适航判定和侦察效率)；ADR-0011 (修复状态机)；ADR-0013 (Hub 场景架构 — 消费槽位状态) |
| **Blocks** | 航行系统、探索系统、战斗系统 — 均依赖适航判定和损伤接口 |
| **Ordering Note** | 应在 ADR-0005 (ResourcesManager) 和 ADR-0007 (IntelManager) 之后 Accepted — 依赖资源查询和知识状态查询 |

## Context

### Problem Statement

《云海织航》的飞艇不是一个整体数值块，而是由可拼装模块和会受伤的船体组成的机械体。GDD #8 定义了完整的模块槽位状态机（双域 actual/visible）、船体完整性波段系统、动力炉载重模型、swap_module 原子交换、以及综合适航判定，但未做出架构部署决策：AirshipModuleSystem 以何种形式存在、状态如何存储、模块操作与 Hub 物理槽位的权责边界在哪里、以及如何在 6 个上下游系统之间协调载重/容积/适航判定。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑，无引擎 API 风险
- **Web 单线程**: 所有状态变更在同一帧内同步完成
- **ADR-0002 信号协议**: typed params, sync emit, max depth 2, emit-after-mutation
- **ADR-0003 持久化**: `progress.airship` snapshot package
- **ADR-0005 ResourcesManager**: 载重数据、容积加成、材料消耗由 ResourcesManager 提供；本系统消费并综合判定
- **ADR-0001 启动顺序**: AirshipModuleSystem 在 Phase 4 (foundation_ready) 初始化，依赖 Phase 3 的 ResourcesManager
- **Hub 边界**: Hub (#7) 拥有槽位物理位置和交互锚点；本系统拥有槽位效果逻辑、状态机和适航判定

### Requirements

- 2 个开放模块槽位 (A/B)，每槽位可安装任意类型模块（scout / cargo）
- 双域状态模型: actual_state（真实物理状态）+ visible_state（玩家可见状态）
- 船体完整性 0–100，4 波段 (intact/damaged/critical/destroyed)，含伤痕计数器
- 动力炉载重模型: 侦察炉=8, 货仓炉=12, 载重 = floor(Σ rating × η_final)
- swap_module 两阶段原子操作: 验证全部前提条件 → 执行（卸旧→退材料→装新→扣材料）
- 适航判定 can_depart(): 综合动力炉 + 船体 + 载重三维度
- 6 个 typed signals: slot_state_changed, actual_state_changed, hull_integrity_changed, hull_band_changed, module_efficiency_changed, departure_readiness_changed

## Decision

### 1. AirshipModuleSystem 作为 Autoload #8

AirshipModuleSystem 在 Phase 4 (foundation_ready) 中初始化。`_ready()` 仅执行信号声明和常量定义；实际状态初始化在收到 `foundation_ready` 信号后执行。

```
Autoload 顺序 (Phase 4):
  #7 AirshipHub          ──┐
  #8 AirshipModuleSystem ──┤ 并行接收 foundation_ready
  #13 WorldRepair        ──┘
```

### 2. Dictionary 后端存储

```gdscript
# === AirshipModuleSystem 状态结构 ===

# 模块槽位状态: StringName → SlotState
# SlotState = {
#   visible_state: int,     # 0=EMPTY, 1=INSTALLED, 2=DAMAGED, 3=UNCHECKED
#   actual_state: int,      # 0=EMPTY, 1=INSTALLED, 2=DAMAGED
#   module_type: StringName, # "scout" / "cargo" / "" (empty)
#   efficiency: float       # η_visible (before hull band correction)
# }
var module_slots: Dictionary = {}  # Dictionary[StringName, Dictionary]
# Keys: "slot_a", "slot_b"

# 船体状态
var hull_integrity: int = 100       # 0–100
var hull_scars: int = 0             # >= 0, no ceiling
var hull_band: StringName = "intact"  # intact / damaged / critical / destroyed

# 适航缓存 (避免无变化信号噪音)
var _cached_can_depart: bool = true
var _cached_depart_reasons: Array = []
```

**常量定义：**

```gdscript
# 槽位状态枚举
const SLOT_EMPTY: int = 0
const SLOT_INSTALLED: int = 1
const SLOT_DAMAGED: int = 2
const SLOT_UNCHECKED: int = 3

# 模块类型
const MODULE_SCOUT: StringName = &"scout"
const MODULE_CARGO: StringName = &"cargo"

# 动力炉载重额定值
const FURNACE_RATING_SCOUT: int = 8
const FURNACE_RATING_CARGO: int = 12

# 效率系数表 [module_type][visible_state]
const EFFICIENCY_TABLE: Dictionary = {
    MODULE_SCOUT: {SLOT_EMPTY: 0.0, SLOT_UNCHECKED: 0.95, SLOT_INSTALLED: 1.0, SLOT_DAMAGED: 0.6},
    MODULE_CARGO: {SLOT_EMPTY: 0.0, SLOT_UNCHECKED: 0.95, SLOT_INSTALLED: 1.0, SLOT_DAMAGED: 0.5}
}

# 船体波段
const BAND_INTACT: StringName = &"intact"
const BAND_DAMAGED: StringName = &"damaged"
const BAND_CRITICAL: StringName = &"critical"
const BAND_DESTROYED: StringName = &"destroyed"

# 波段效率修正
const HULL_EFFICIENCY_BAND: Dictionary = {
    BAND_INTACT: 1.0, BAND_DAMAGED: 1.0, BAND_CRITICAL: 0.8, BAND_DESTROYED: 0.0
}

# 波段阈值
const BAND_INTACT_MIN: int = 76
const BAND_DAMAGED_MIN: int = 26
const BAND_CRITICAL_MIN: int = 1
```

### 3. 信号接口

6 个 typed signal，遵循 emit-after-mutation + ADR-0002 命名：

```gdscript
# 模块 visible_state 变更
signal slot_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName)

# 模块 actual_state 变更 (航行系统写入损伤时)
signal actual_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName)

# 船体完整性值变更
signal hull_integrity_changed(old_value: int, new_value: int)

# 船体波段变更
signal hull_band_changed(old_band: StringName, new_band: StringName)

# 模块效率系数变更
signal module_efficiency_changed(slot_id: StringName, old_eff: float, new_eff: float)

# 适航状态变更 (仅在 can 或 reasons 实际变更时触发)
signal departure_readiness_changed(can_depart: bool, reasons: Array[StringName])
```

**信号发射顺序**: `actual_state_changed` → `slot_state_changed` → `module_efficiency_changed` → `departure_readiness_changed`。船体信号 (`hull_integrity_changed` → `hull_band_changed`) 独立于模块信号链。

### 4. 方法接口

#### 4a. 模块操作

```gdscript
# 安装模块
func install_module(slot_id: StringName, module_type: StringName) -> int:
    # 返回 ResourceResult enum (ERR_SLOT_OCCUPIED / ERR_INSUFFICIENT_RESOURCES / SUCCESS)
    # Pre: slot must be EMPTY; ResourcesManager.consume_for_module() must succeed

# 卸下模块
func uninstall_module(slot_id: StringName) -> Dictionary:
    # 返回 {result: int, refund: Dictionary}
    # installed → 退还 75% 材料 (向上取整)
    # damaged / unchecked → 不退还材料
    # Pre: slot must not be EMPTY

# 模块交换 — 两阶段原子操作
func swap_module(slot_id: StringName, new_module_type: StringName) -> int:
    # Phase 1 (验证): 检查槽位非空 / 材料充足 / 若旧货仓→新非货仓则 cargo_bay empty
    # Phase 2 (执行): 卸旧→退材料→装新→扣材料 (净消耗 = max(0, 新成本 − 退款))
    # 同类型交换被拒绝
    # 返回 ResourceResult

# 检查模块 (unchecked → installed/damaged)
func inspect_module(slot_id: StringName) -> int:
    # visible_state 同步为 actual_state
    # 免费操作 — 0 材料消耗

# 维修模块
func repair_module(slot_id: StringName) -> int:
    # unchecked 状态 → 消耗 repair_kit × 2 (全额, 无论 actual_state)
    # damaged 状态 → 消耗 repair_kit × 2
    # visible_state 和 actual_state 均置为 INSTALLED

# 查询槽位状态
func get_slot_state(slot_id: StringName) -> Dictionary:
    # 返回 {visible_state: int, actual_state: int, module_type: StringName, efficiency: float}
```

#### 4b. 船体操作

```gdscript
# 应用中探索船体损伤 (由 #12 Combat 调用)
func apply_hull_damage(amount: int) -> void:
    # integrity = max(0, integrity - amount)
    # 跨波段: hull_scars += 1 (基础) + 每个新进入波段 +1
    # 触发 hull_integrity_changed + hull_band_changed (若波段变更)

# 应用中探索模块损伤 (由 #12 Combat 调用)
func apply_module_damage(slot_id: StringName, damage_type: StringName) -> void:
    # Pre: slot 已安装, actual_state != DAMAGED
    # actual_state → DAMAGED
    # damage_type 透传至 FeedbackManager (#17)

# 维修船体 (由 Hub Station 10 触发)
func repair_hull(materials: Array[StringName]) -> Dictionary:
    # 返回 {result: int, integrity_restored: int, new_integrity: int}
    # R_total = Σ repair_value(m) for m in materials
    # integrity = min(100, integrity + R_total)
    # 若 R_total < 1: 拒绝 (保证每次至少恢复 1 点)
    # 若 integrity >= 100: 拒绝 (防止浪费材料)

# 查询船体状态
func get_hull_state() -> Dictionary:
    # 返回 {integrity: int, scars: int, band: StringName}
```

#### 4c. 适航判定

```gdscript
# 综合适航判定
func can_depart() -> Dictionary:
    # 返回 {can: bool, reasons: Array[StringName]}
    # reasons 可能包含: "overloaded", "no_furnace", "hull_destroyed"
    # 所有阻断条件同时返回

# 查询最大载重
func get_max_load() -> int:
    # M_max = floor(Σ furnace_rating(i) × η_final(i))
    # η_final = η_visible × η_hull_band

# 查询有效货舱容积
func get_effective_cargo_volume() -> int:
    # V_effective = Σ cargo_module_volume_bonus × η_final for each cargo slot
```

### 5. 核心算法

#### 5a. 效率系数计算链

```
η_visible = EFFICIENCY_TABLE[module_type][visible_state]
η_final = η_visible × HULL_EFFICIENCY_BAND[hull_band]
```

#### 5b. 最大载重

```
M_max = floor(Σ furnace_rating(i) × η_final(i)) for each installed module
```

完整场景表：

| 配置 | 载重公式 | M_max |
|------|---------|-------|
| 双 cargo 完好 | floor(12×1.0 + 12×1.0) | 24 |
| scout+cargo 完好 | floor(8×1.0 + 12×1.0) | 20 |
| 双 scout 完好 | floor(8×1.0 + 8×1.0) | 16 |
| cargo damaged + scout 完好 | floor(8×1.0 + 12×0.5) | 14 |
| scout damaged + cargo 完好 | floor(8×0.6 + 12×1.0) | 16 |
| 双 damaged | floor(8×0.6 + 12×0.5) | 10 |

#### 5c. 船体波段判定

```
Band(integrity):
  integrity = 0        → destroyed
  1 ≤ integrity ≤ 25   → critical
  26 ≤ integrity ≤ 75  → damaged
  76 ≤ integrity ≤ 100 → intact
```

波段惩罚表：

| 波段 | 航速 | 燃料 | 模块 η 修正 | 额外限制 |
|------|------|------|-----------|---------|
| intact | 1.0 | 1.0 | ×1.0 | 无 |
| damaged | 0.9 | 1.15 | ×1.0 | 无 |
| critical | 0.75 | 1.3 | ×0.8 | 高风险航线封锁 |
| destroyed | — | — | ×0 | 无法出航 |

#### 5d. 适航判定

```
can_depart():
  reasons = []
  if M_max == 0: reasons.append("no_furnace")
  if integrity == 0: reasons.append("hull_destroyed")
  if M_loaded > M_max: reasons.append("overloaded")
  return {can: len(reasons) == 0, reasons: reasons}
```

#### 5e. swap_module 两阶段算法

```
Phase 1 — 验证 (任何失败 → 返回错误, 无状态变更):
  1. slot_id 非空 (visible_state != EMPTY)
  2. new_module_type != current module_type (同类型拒绝)
  3. 若当前为 cargo 且 new 非 cargo: cargo_bay.used_volume == 0
  4. 计算净消耗: net_cost[resource] = max(0, install_cost − refund)
     退款 = installed ? ceil(old_cost × 0.75) : 0
  5. ResourcesManager.consume_for_module() 预演通过

Phase 2 — 执行:
  1. 记录旧模块信息
  2. 设置 slot → EMPTY (中间状态)
  3. 若旧模块 installed: ResourcesManager 发放退款
  4. ResourcesManager 扣除净消耗
  5. 设置 slot → INSTALLED (新模块)
  6. 触发信号链
```

#### 5f. 跨波段伤痕计数

```
apply_hull_damage(amount):
  old_band = current_band
  integrity = max(0, integrity - amount)
  new_band = compute_band(integrity)
  
  hull_scars += 1  # 基础损伤事件
  
  # 每个新进入的波段 +1
  for each band in [damaged, critical, destroyed]:
    if old_band was above this band AND new_band is at or below:
      hull_scars += 1
```

#### 5g. 返航后状态处理

```
on_voyage_return(damage_amount: int, damaged_slots: Array[StringName]):
  # 1. 应用船体损伤
  apply_hull_damage(damage_amount)
  
  # 2. 写入模块 actual_state
  for each slot:
    if slot_id in damaged_slots:
      slot.actual_state = DAMAGED
    # installed 的保持不变
  
  # 3. 更新 visible_state
  for each slot:
    if old actual_state == INSTALLED:
      slot.visible_state = UNCHECKED  # η → 0.95
    elif old actual_state == DAMAGED:
      # visible_state 维持 DAMAGED (玩家已知它坏了)
      pass
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│              AirshipModuleSystem (Autoload #8)                        │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    STATE STORAGE (Dictionary)                  │   │
│  │                                                                │   │
│  │  module_slots: Dict[StringName, SlotState]  (2 slots)         │   │
│  │    slot_a / slot_b: {visible_state, actual_state,             │   │
│  │                      module_type, efficiency}                  │   │
│  │  hull_integrity: int   (0–100)                                 │   │
│  │  hull_scars: int       (≥0, no ceiling)                       │   │
│  │  hull_band: StringName  (intact/damaged/critical/destroyed)   │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              UPSTREAM (consumes)                                │  │
│  │                                                                │  │
│  │  Hub (#7)      ──→ install / uninstall / swap / inspect / repair│
│  │  Resources (#5)──→ get_total_loaded_mass / consume_for_module  │  │
│  │  Combat (#12)  ──→ apply_hull_damage / apply_module_damage    │  │
│  │  Navigation(#10)─→ on_voyage_return (writes damage results)   │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              DOWNSTREAM (provides)                              │  │
│  │                                                                │  │
│  │  Navigation(#10)←── can_depart / get_max_load / scout η       │  │
│  │  Exploration(#11)←── hull band + penalties for risk events    │  │
│  │  Resources (#5) ←── update_cargo_bay_effective_volume          │  │
│  │  UI (#16)       ←── get_slot_state / get_hull_state           │  │
│  │  Persistence(#3)←── progress.airship snapshot                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              SIGNALS (6 typed, emit-after-mutation)             │  │
│  │                                                                │  │
│  │  slot_state_changed / actual_state_changed                     │  │
│  │  hull_integrity_changed / hull_band_changed                    │  │
│  │  module_efficiency_changed / departure_readiness_changed       │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

#### 安装材料成本

```gdscript
# 由本 ADR 定义, ResourcesManager (#5) 确认或覆盖
const SCOUT_INSTALL_COST: Dictionary = {
    "basic_supply": 5,
    "repair_kit": 2
}
const CARGO_INSTALL_COST: Dictionary = {
    "basic_supply": 3,
    "repair_kit": 3
}
const MODULE_REPAIR_COST: Dictionary = {
    "repair_kit": 2
}
const UNINSTALL_REFUND_RATIO: float = 0.75  # ceil applied per resource
```

#### 耐久化接口 (ADR-0003 集成)

```gdscript
# 在 foundation_ready 阶段注册
func _on_foundation_ready() -> void:
    Persistence.register_domain_serializer("airship", _serialize_airship)

func _serialize_airship() -> Dictionary:
    return {
        "domain_id": "airship",
        "modules": {
            "slot_a": _serialize_slot("slot_a"),
            "slot_b": _serialize_slot("slot_b")
        },
        "hull_integrity": hull_integrity,
        "hull_scars": hull_scars
    }

func _serialize_slot(slot_id: StringName) -> Dictionary:
    slot = module_slots[slot_id]
    return {
        "visible_state": slot.visible_state,
        "actual_state": slot.actual_state,
        "module_type": slot.module_type,
        "efficiency": slot.efficiency
    }
```

#### 动力炉能量接口 (Provisional)

```gdscript
# Stub — 当前始终返回 1.0
# 未来能量系统实现后将实际查询燃料/能量水平
func get_furnace_energy_status(furnace_id: StringName) -> float:
    return 1.0
```

#### MVP 起始状态

```gdscript
func _init_new_game_state() -> void:
    # 槽 B 预装货仓模块
    module_slots["slot_a"] = {
        "visible_state": SLOT_EMPTY, "actual_state": SLOT_EMPTY,
        "module_type": &"", "efficiency": 0.0
    }
    module_slots["slot_b"] = {
        "visible_state": SLOT_INSTALLED, "actual_state": SLOT_INSTALLED,
        "module_type": MODULE_CARGO, "efficiency": 1.0
    }
    hull_integrity = 100
    hull_scars = 0
    hull_band = BAND_INTACT
    # M_max = 12 (仅货仓动力炉)
```

## Alternatives Considered

### Alternative A: 模块作为 Hub Node 子节点

- **Description**: 每个模块是 Hub 场景中的 Node2D 子节点，模块状态绑定到 Node 属性
- **Pros**: 与 Hub 交互点的物理位置天然绑定；可视化编辑
- **Cons**: 场景切换时模块状态必须额外序列化/恢复（违反 ADR-0003 快照模型）；模块效果计算（载重、容积）需要在 Hub 场景中查询，增加跨 Autoload→Scene 通信复杂度；单元测试困难（需要完整 Hub 场景）
- **Rejection Reason**: ADR-0001 确定的数据所有权模型——Autoload 拥有状态，Scene 拥有视觉呈现。模块状态是跨场景持久化状态，属于 Autoload 范畴

### Alternative B: 船体完整性使用多段独立值 (而非 0-100 单一值)

- **Description**: 4 个波段各自有独立的损伤值，damage = max(per-band) 非求和
- **Pros**: 更细粒度的部位损伤模型——不同波段可独立受损和修复
- **Cons**: 增加状态复杂度（4 个独立值 vs 1 个）；GDD #8 已明确定义 integrity 为 0-100 单一值 + 4 波段判定；与 pilot 心理模型不同——"船体完整性 50/100"比"intact:80, damaged:20, critical:0, destroyed:0"更直观
- **Rejection Reason**: GDD #8 的 integrity 0-100 模型已足够表达设计意图——单值 + 波段阈值提供清晰的状态转换和 UI 展示。多段值增加复杂度未增加 gameplay 深度

## Consequences

### Positive

- **集中式适航判定**: can_depart() 是唯一入口——综合动力炉 + 船体 + 载重三维度。消除了多系统各自判定导致的不一致
- **双域状态机**: actual/visible 分离实现了"返航不确定→检查→确认"的 gameplay 循环，不增加运行时复杂度
- **swap_module 原子性**: 两阶段设计保证不会出现"卸下了但装不上"的中间状态
- **信号契约完整**: 6 个 typed signal 覆盖所有状态变更——Hub 和 UI 通过信号同步而非轮询
- **持久化简单**: Dictionary 直接映射到 Canonical JSON

### Negative

- **Autoload #8 依赖**: Hub、Navigation、Combat、Resources 均依赖本系统——增加了 Phase 4 启动约束
- **floor() 舍入损失**: 最坏情况下损失 21.9% 载重（单 damaged cargo + critical 波段）
- **动力炉能量接口为 stub**: 当前 get_furnace_energy_status() 始终返回 1.0——能量系统实现后需修订本 ADR
- **trapped 货物通知路径较长**: 模块效率变更 → signal → UI 查询 → 检测 trapped → 通知

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| swap_module Phase 1 验证通过后 Phase 2 中 ResourcesManager 状态变更导致失败 | Very Low — 单线程同步执行 | Medium — 可能的材料不一致 | 单线程保证 Phase 1→2 之间无其他代码执行。若 Phase 2 失败（如 consume 返回 error），记录 critical error 日志——这是 bug 而非 runtime 条件 |
| 双侦察冗余保护被误解为"第二个侦察无用" | Low | Low — 玩家可能认为双侦察是浪费 | 文档和 tooltip 强调"冗余保护"——一个受损时另一个接管。GDD 规则 12 已明确此行为 |
| V_effective 变更后 trapped 货物未正确通知玩家 | Medium | Medium — 玩家困惑"货去哪了" | 模块效率变更时主动调用 ResourcesManager.update_cargo_bay_effective_volume()；UI 通过 module_efficiency_changed 信号检测并显示通知 |
| 能量系统未来实现与当前动力炉 stub 不兼容 | Low — 已预留接口 | Medium — 需要修订本 ADR | get_furnace_energy_status() 接口已定义契约——能量系统只需返回 float 0.0–1.0 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| airship-modules-hull-state.md | 2 开放模块槽位, 2 模块类型 (scout/cargo), 双域 actual/visible 状态 | `module_slots` Dictionary + 4 级 visible_state 枚举 + 双域字段 |
| airship-modules-hull-state.md | 动力炉载重: scout=8, cargo=12, M_max = floor(Σ rating × η_final) | `get_max_load()` 公式 + FURNACE_RATING 常量 |
| airship-modules-hull-state.md | 船体完整性 0-100, 4 波段 (intact/damaged/critical/destroyed) | `hull_integrity` + `compute_band()` + 波段惩罚表 |
| airship-modules-hull-state.md | 船体伤痕计数器 + 跨波段额外计数 | `hull_scars` + `apply_hull_damage()` 跨波段算法 |
| airship-modules-hull-state.md | swap_module 两阶段原子操作 | Phase 1 验证 (全部前提) → Phase 2 执行 (卸旧→退款→装新→扣款) |
| airship-modules-hull-state.md | 效率系数计算链: η_visible × η_hull_band = η_final | 双层效率乘法——EFFICIENCY_TABLE × HULL_EFFICIENCY_BAND |
| airship-modules-hull-state.md | 适航判定 can_depart(): 动力炉 + 船体 + 载重 | `can_depart()` 返回 `{can, reasons}` — all blocking conditions |
| airship-modules-hull-state.md | 返航后 unchecked 状态 + actual/visible 同步逻辑 | `on_voyage_return()` — actual=installed → visible=unchecked; actual=damaged → visible 维持 damaged |
| airship-modules-hull-state.md | 6 个 typed signals (emit-after-mutation, 顺序约定) | Signal declarations + emit order: actual_state → slot_state → module_efficiency → departure_readiness |
| airship-modules-hull-state.md | 安装材料成本 + 75% 退款 + damaged/unchecked 卸下无退款 | Install cost constants + UNINSTALL_REFUND_RATIO + 状态门控退款 |

## Performance Implications

- **CPU**: 所有操作 O(1) — 2 个槽位的循环。`can_depart()` 每次调用 ≤ 3 次 Dictionary 查询。`swap_module()` Phase 1 验证 ≤ 5 次资源查询。所有操作 < 0.1ms
- **Memory**: 2 个槽位状态 × ~150 bytes + 船体状态 ~50 bytes。总计 < 500 bytes
- **Load Time**: 启动时无文件 I/O — 状态从 Persistence snapshot 恢复。反序列化 < 0.5ms
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. 在 project.godot 中注册 AirshipModuleSystem 为 Autoload #8
2. 实现 Dictionary 状态结构和常量枚举
3. 实现模块操作: install / uninstall / swap / inspect / repair
4. 实现船体操作: apply_hull_damage / apply_module_damage / repair_hull
5. 实现适航判定: can_depart / get_max_load / get_effective_cargo_volume
6. 实现返航处理: on_voyage_return
7. 实现 ADR-0003 serializer/deserializer 注册
8. 实现 MVP 起始状态 (槽 B 预装 cargo)
9. 单元测试: 所有效率系数场景 (D.1 表), swap_module 全部 AC (37-42), can_depart 多阻断条件, 跨波段伤痕计数, trapped 货物触发

## Validation Criteria

- 2 个槽位 × 4 种 visible_state × 2 种 module_type → 全部效率系数计算正确
- swap_module: 验证失败无状态变更；同类型拒绝；cargo→scout 时 cargo_bay 非空拒绝
- can_depart(): 多阻断条件同时返回 (overloaded + hull_destroyed)
- apply_hull_damage(): 跨波段伤痕计数正确 (见 GDD AC-29)
- 返航后: installed→unchecked (η=0.95), damaged→damaged (η 保持)
- unchecked + 直接维修: 消耗 repair_kit×2, 不先获取 actual_state 信息
- 存档→读档: 双域状态 + 船体完整性 + 伤痕计数完全恢复

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — AirshipModuleSystem 为 Autoload #8，Phase 4 启动
- **ADR-0002**: Signal 通信协议 — 6 signals typed params, sync emit, {noun}_{verb_past}
- **ADR-0003**: 存档系统 — `progress.airship` snapshot package
- **ADR-0005**: 资源池系统 — consume_for_module, get_total_loaded_mass, cargo_module_volume_bonus, repair_value
- **ADR-0006**: Web 平台约束 — 单线程保证 swap_module 两阶段原子性
- **ADR-0007**: 知识状态 — ability_state 查询（模块相关能力解锁条件）
- **GDD #8**: airship-modules-hull-state.md — 完整状态机、公式、边缘情况
- **GDD #7**: airship-hub.md — 槽位物理位置和交互锚点
