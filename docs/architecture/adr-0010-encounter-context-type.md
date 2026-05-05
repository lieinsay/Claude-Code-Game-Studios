# ADR-0010: EncounterContext 跨系统类型契约 — Navigation→Exploration/Combat 数据桥

## Status
Proposed

## Date
2026-05-05

## Summary
EncounterContext 是 Navigation (#10) 在航程结束时产出的结构化遭遇数据包，被 Exploration (#11) 消费以生成探索场景、被 Combat (#12) 通过 Exploration 间接消费以构建威胁上下文。本 ADR 定义 EncounterContext 的 Dictionary 结构、VoyageResult 枚举、EncounterEntry 子结构、类型所有权边界、ADR-0003 序列化契约、以及 voyage_completed 信号合约。所有字段使用 Dictionary[StringName, Variant] 存储 — 遵循 ADR-0005/0007/0009 的统一 Dictionary 模式。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Data Contract |
| **Knowledge Risk** | LOW — 纯 GDScript Dictionary 数据结构，无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/navigation-route-risk.md`, `design/gdd/exploration-scavenge-scenario.md`, `design/gdd/combat-threat-handling.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | EncounterContext 字段完整性（exploration GDD AC-11-01 验证 voyage_result + destination_id）；EncounterEntry 遭遇表索引正确性；fallback context 在 null/malformed 输入时的降级行为 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload 启动顺序 — Navigation #10, Exploration #11 的 Phase 定位)；ADR-0002 (Signal 通信协议 — voyage_completed 信号)；ADR-0003 (快照包持久化 — progress.voyage snapshot 包含 EncounterContext)；ADR-0009 (船体状态 — Navigation 通过 apply_hull_damage 写入 #8，EncounterContext 携带 accumulated_damage 摘要) |
| **Enables** | ADR-0011 (修复状态机 — 消费 voyage_result 判断是否触发修复流程)；ADR-0012 (输入路由 — exploration 场景中的交互焦点切换) |
| **Blocks** | Exploration (#11) 场景生成逻辑、Combat (#12) 威胁上下文构建 — 均依赖 EncounterContext 的结构化契约 |
| **Ordering Note** | 应在 ADR-0009 (船体状态) 之后定义 — EncounterContext.accumulated_damage 写入 #8 的接口已在 ADR-0009 中定义 |

## Context

### Problem Statement

《云海织航》的航行阶段 (#10 Navigation) 在航程结束时产出结构化遭遇数据，探索阶段 (#11 Exploration) 和战斗阶段 (#12 Combat) 消费这些数据以生成探索场景和威胁遭遇。GDD #10 定义了 EncounterContext 的概念和字段概要（route_id、destination_id、voyage_result、resolved_encounters），GDD #11 在 Interactions 表中确认消费 EncounterContext，但三个 GDD 均未定义 EncounterContext 的**正式类型结构、字段类型约束、所有权边界、序列化格式和跨系统消费合约**。如果没有一个正式的 ADR 锚定这个跨系统类型，Navigation、Exploration 和 Combat 的实现将基于各自对 GDD 的解读，在系统边界处产生字段名不一致、类型假设冲突和序列化断裂。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯数据结构，无引擎 API 风险
- **ADR-0002 信号协议**: typed params — voyage_completed 信号携带 EncounterContext Dictionary
- **ADR-0003 持久化**: EncounterContext 作为 progress.voyage snapshot 的一部分序列化为 Canonical JSON
- **ADR-0005/0007/0009 一致性**: 所有跨系统状态使用 Dictionary[StringName, Variant] — EncounterContext 遵循同一模式
- **Web 单线程**: 航行结束时的多系统写入（#8 船体 → #6 知识 → #11 EncounterContext → #17 反馈 → #3 存档）在同一帧内顺序执行
- **GDD #10 写入顺序约定**: (1) #8 船体伤害 (2) #6 路线知识更新 (3) #11 EncounterContext (4) #17 状态变更事件 (5) 存档 — EncounterContext 在步骤 (3) 产出

### Requirements

- EncounterContext 必须包含：route_id、destination_id、voyage_result、resolved_encounters 列表
- resolved_encounters 中每个 EncounterEntry 必须包含：encounter_type、damage_amount、special_effect_tags
- voyage_result 必须是枚举值：ARRIVED / RETREATED / FORCED_LANDING
- 结构必须直接映射到 JSON.stringify() 用于 ADR-0003 快照持久化
- Exploration (#11) 必须能仅凭 EncounterContext（不回调 Navigation）决定场景生成参数
- 必须定义 fallback context 用于上游数据异常时的降级行为（GDD #11 EC-11-07）

## Decision

### 1. EncounterContext 作为跨系统 Dictionary 类型

EncounterContext 不创建 Godot Resource 子类或独立 Autoload — 它是 Navigation (#10) 在航程终态时构建、通过信号发射、由 Exploration (#11) 接收的普通 Dictionary。所有权遵循 **生产者构建、消费者校验、无共享引用** 原则。

```
所有权流:
  Navigation (#10)  _build_encounter_context() → Dictionary
         │
         │  voyage_completed(ctx: Dictionary)
         ▼
  Exploration (#11)  _validate_encounter_context(ctx) → EncounterContext | FallbackContext
         │
         │  build_threat_context(encounter_entry)
         ▼
  Combat (#12)      消费 ThreatContext (由 Exploration 从 EncounterContext 派生)
```

### 2. EncounterContext 完整结构定义

```gdscript
# EncounterContext — Navigation (#10) 在航程终态时产出
# 类型别名 (文档约定, 非 GDScript class):
#   EncounterContext := Dictionary[StringName, Variant]
#   EncounterEntry  := Dictionary[StringName, Variant]

# === EncounterContext 字段 ===
# {
#   route_id: StringName,              # 航线 ID (e.g. &"route.sky-reef-arc-01")
#   destination_id: StringName,        # 目的地 ID (e.g. &"cloudwatch-ruins")
#   voyage_result: StringName,         # VoyageResult 枚举值
#   resolved_encounters: Array,        # Array[EncounterEntry]
#   accumulated_damage: int,           # 航程累计船体伤害 (写入 #8 的值)
#   revealed_hidden_tags: Array,       # Array[StringName] — 航程中新揭示的隐藏风险标签
#   hull_band_arrival: StringName,     # 抵达/终态时的船体波段
#   forced_landing_position: StringName, # 迫降位置 ID (仅 FORCED_LANDING 时有效, 否则 &"")
#   damaged_slots: Array,              # Array[StringName] — 航程中受损的模块槽位 ID
# }

# === VoyageResult 枚举 ===
const VOYAGE_RESULT_ARRIVED: StringName = &"arrived"
const VOYAGE_RESULT_RETREATED: StringName = &"retreated"
const VOYAGE_RESULT_FORCED_LANDING: StringName = &"forced_landing"

# === EncounterEntry 子结构 ===
# {
#   encounter_type: StringName,        # 遭遇类型 ID (e.g. &"storm_cell_edge")
#   hazard_tag: StringName,            # 来源风险标签 (e.g. &"storm")
#   damage_amount: int,                # 本次遭遇造成的船体伤害量
#   special_effect_tags: Array,        # Array[StringName] — 特殊效果标记
#   was_hidden: bool,                  # 此遭遇来自隐藏标签 (true) 还是可见标签 (false)
#   time_offset: float,                # 遭遇在航程中的发生时间 (秒, 从 0 开始)
# }
```

### 3. 按 voyage_result 的字段有效性

| 字段 | ARRIVED | RETREATED | FORCED_LANDING |
|------|---------|-----------|----------------|
| `route_id` | ✅ 有效 | ✅ 有效 | ✅ 有效 |
| `destination_id` | ✅ 有效 | ✅ 有效 | ✅ 有效 |
| `voyage_result` | `"arrived"` | `"retreated"` | `"forced_landing"` |
| `resolved_encounters` | ✅ 完整列表 | ✅ 截至撤退点 | ✅ 完整列表 (含致命遭遇) |
| `accumulated_damage` | ✅ sum of all | ✅ sum of resolved | ✅ sum of all |
| `revealed_hidden_tags` | ✅ | ✅ (已揭示的) | ✅ |
| `hull_band_arrival` | ✅ intact/damaged/critical | ✅ 当前波段 | `"destroyed"` |
| `forced_landing_position` | `""` | `""` | ✅ 迫降点 ID |
| `damaged_slots` | ✅ | ✅ | ✅ |

### 4. 信号合约

```gdscript
# Navigation (#10) 在航程终态时发射
# 遵循 ADR-0002: typed params, sync emit, emit-after-mutation
signal voyage_completed(encounter_context: Dictionary)

# 消费方: Exploration (#11)
# Exploration 在 _on_voyage_completed(ctx) 中:
#   1. 校验 ctx 字段完整性 (_validate_encounter_context)
#   2. 若校验失败 → 构建 fallback context (见下文)
#   3. 根据 voyage_result 决定场景入口 (arrived → 正常入口, forced_landing → 坠机点)
#   4. 进入 ARRIVING 阶段
```

### 5. Fallback Context (防御性降级)

```gdscript
# 当 EncounterContext 为 null、缺失关键字段或 voyage_result 无效时
# Exploration 构建 fallback context — 不阻塞玩家体验, 记录内部错误日志

func _build_fallback_context() -> Dictionary:
    return {
        "route_id": &"unknown",
        "destination_id": &"cloudwatch-ruins-fallback",
        "voyage_result": VOYAGE_RESULT_ARRIVED,
        "resolved_encounters": [],
        "accumulated_damage": 0,
        "revealed_hidden_tags": [],
        "hull_band_arrival": &"intact",
        "forced_landing_position": &"",
        "damaged_slots": [],
    }
```

**Fallback 触发条件** (任一项为真):
- `ctx` 为 null 或非 Dictionary 类型
- `ctx.route_id` 缺失或为空 StringName
- `ctx.destination_id` 缺失或为空 StringName
- `ctx.voyage_result` 缺失或不是有效 VoyageResult 枚举值
- `ctx.resolved_encounters` 不是 Array 类型

### 6. ADR-0003 序列化

EncounterContext 作为 `progress.voyage` snapshot package 的一部分持久化:

```gdscript
# Navigation 在航程结束时调用 Persistence
# progress.voyage snapshot 包含:
# {
#   route_id: StringName,
#   voyage_result: StringName,
#   elapsed_time: float,
#   resolved_encounters: Array[EncounterEntry],
#   accumulated_damage: int,
#   revealed_hidden_tags: Array[StringName],
#   hull_band_arrival: StringName,
#   damaged_slots: Array[StringName],
#   encounter_context: Dictionary  # 完整 EncounterContext — 供 Exploration 从存档恢复
# }
```

**存档恢复**: 若读档时 `voyage_state == ARRIVED` 或 `FORCED_LANDING` 且 `encounter_context` 存在，Exploration 直接消费存档中的 EncounterContext 进入对应场景 — 不重复触发 Navigation。

### 7. 遭遇表索引 (EncounterEntry 的 encounter_type 全集)

MVP 中定义的 11 个 encounter_type:

| encounter_type | 来源标签 | 伤害量 | 特殊效果 |
|---------------|---------|--------|---------|
| `calm_passage` | `safe` | 0 | — |
| `gentle_crosswind` | `safe` | 0 | `["voyage_duration_penalty_5s"]` |
| `minor_debris` | `safe` | 1–2 | — |
| `scenic_discovery` | `safe` | 0 | `["reveal_landmark"]` |
| `storm_cell_edge` | `storm` | 1–3 | `["minor_slow"]` |
| `turbulence_zone` | `storm` | 2–4 | `["speed_penalty_15pct"]` |
| `lightning_proximity` | `storm` | 3–6 | `["module_damage_20pct_scout"]` |
| `wind_shear` | `storm` | 1–2 | `["next_check_early_5s"]` |
| `storm_eye_passage` | `storm` | 0 | `["reveal_all_hidden_tags"]` |
| `dense_fog_bank` | `low-visibility` | 0 | `["scout_window_halved_next"]` |
| `hidden_reef_proximity` | `low-visibility` | 2–4 | `["bypass_scout"]` |
| `false_horizon` | `low-visibility` | 0 | `["time_estimate_bias_15pct"]` |

```gdscript
# 遭遇类型常量 (Navigation #10 定义)
const ENCOUNTER_CALM_PASSAGE: StringName = &"calm_passage"
const ENCOUNTER_GENTLE_CROSSWIND: StringName = &"gentle_crosswind"
const ENCOUNTER_MINOR_DEBRIS: StringName = &"minor_debris"
const ENCOUNTER_SCENIC_DISCOVERY: StringName = &"scenic_discovery"
const ENCOUNTER_STORM_CELL_EDGE: StringName = &"storm_cell_edge"
const ENCOUNTER_TURBULENCE_ZONE: StringName = &"turbulence_zone"
const ENCOUNTER_LIGHTNING_PROXIMITY: StringName = &"lightning_proximity"
const ENCOUNTER_WIND_SHEAR: StringName = &"wind_shear"
const ENCOUNTER_STORM_EYE_PASSAGE: StringName = &"storm_eye_passage"
const ENCOUNTER_DENSE_FOG_BANK: StringName = &"dense_fog_bank"
const ENCOUNTER_HIDDEN_REEF_PROXIMITY: StringName = &"hidden_reef_proximity"
const ENCOUNTER_FALSE_HORIZON: StringName = &"false_horizon"

# 特殊效果标记常量
const EFFECT_VOYAGE_DURATION_PENALTY_5S: StringName = &"voyage_duration_penalty_5s"
const EFFECT_MINOR_SLOW: StringName = &"minor_slow"
const EFFECT_SPEED_PENALTY_15PCT: StringName = &"speed_penalty_15pct"
const EFFECT_MODULE_DAMAGE_20PCT_SCOUT: StringName = &"module_damage_20pct_scout"
const EFFECT_NEXT_CHECK_EARLY_5S: StringName = &"next_check_early_5s"
const EFFECT_REVEAL_ALL_HIDDEN_TAGS: StringName = &"reveal_all_hidden_tags"
const EFFECT_REVEAL_LANDMARK: StringName = &"reveal_landmark"
const EFFECT_SCOUT_WINDOW_HALVED_NEXT: StringName = &"scout_window_halved_next"
const EFFECT_BYPASS_SCOUT: StringName = &"bypass_scout"
const EFFECT_TIME_ESTIMATE_BIAS_15PCT: StringName = &"time_estimate_bias_15pct"
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    CROSS-SYSTEM DATA FLOW                                 │
│                                                                           │
│  Navigation (#10)                                                         │
│  ┌────────────────────────────────────────────────────────────┐         │
│  │  Voyage State Machine (IN_PROGRESS → ARRIVED/RETREATED/    │         │
│  │                        FORCED_LANDING)                      │         │
│  │                            │                                 │         │
│  │  每 12s 遭遇检查:            │  终态:                          │         │
│  │    EncounterEntry           │  _build_encounter_context()    │         │
│  │    {encounter_type,         │  → EncounterContext Dictionary │         │
│  │     damage_amount,          │                                 │         │
│  │     special_effect_tags}    │                                 │         │
│  └────────────────────────────┬─────────────────────────────────┘         │
│                               │                                            │
│                               │ voyage_completed(ctx: Dictionary)          │
│                               ▼                                            │
│  ┌────────────────────────────────────────────────────────────┐         │
│  │              EncounterContext (Dictionary)                   │         │
│  │  route_id, destination_id, voyage_result,                   │         │
│  │  resolved_encounters[], accumulated_damage,                 │         │
│  │  revealed_hidden_tags[], hull_band_arrival,                 │         │
│  │  forced_landing_position, damaged_slots[]                   │         │
│  └─────────────────────┬──────────────────────────────────────┘         │
│                        │                                                  │
│          ┌─────────────┼─────────────┐                                    │
│          ▼             ▼             ▼                                    │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐                            │
│  │Exploration│  │  Combat   │  │Persistence│                            │
│  │   (#11)   │  │  (#12)    │  │   (#3)    │                            │
│  │           │  │           │  │           │                            │
│  │消费完整ctx │  │通过       │  │progress.  │                            │
│  │生成探索场景│  │Exploration│  │voyage     │                            │
│  │           │  │间接消费   │  │snapshot   │                            │
│  │ARRIVING   │  │ThreatCtx  │  │持久化     │                            │
│  │阶段入口   │  │           │  │           │                            │
│  └───────────┘  └───────────┘  └───────────┘                            │
│                                                                           │
│  Combat 的 ThreatContext 由 Exploration 从 EncounterContext 派生:          │
│    Exploration._build_threat_context(encounter_entry) → ThreatContext     │
│    → Combat.initiate_threat(threat_ctx)                                   │
└──────────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

#### Navigation (#10) 生产端

```gdscript
# Navigation 内部 — 航程终态时调用
func _build_encounter_context() -> Dictionary:
    return {
        "route_id": _active_voyage.route_id,
        "destination_id": _active_voyage.destination_id,
        "voyage_result": _active_voyage.result,
        "resolved_encounters": _resolved_encounters.duplicate(true),
        "accumulated_damage": _accumulated_damage,
        "revealed_hidden_tags": _revealed_hidden_tags.duplicate(true),
        "hull_band_arrival": _current_hull_band,
        "forced_landing_position": _forced_landing_position,
        "damaged_slots": _damaged_slots.duplicate(true),
    }

# 发射信号
func _finalize_voyage() -> void:
    var ctx := _build_encounter_context()
    # 步骤 (1)(2) 已完成: #8 船体伤害, #6 知识更新
    # 步骤 (3): 发出 EncounterContext
    voyage_completed.emit(ctx)
    # 步骤 (4)(5): #17 反馈事件, #3 存档
```

#### Exploration (#11) 消费端

```gdscript
# Exploration 接收 voyage_completed 信号
func _on_voyage_completed(ctx: Dictionary) -> void:
    var validated := _validate_encounter_context(ctx)
    _current_encounter_context = validated
    _enter_arriving_phase(validated)

func _validate_encounter_context(ctx: Dictionary) -> Dictionary:
    if ctx == null or not ctx is Dictionary:
        return _build_fallback_context()
    if not ctx.get("route_id") or ctx.route_id == &"":
        return _build_fallback_context()
    if not ctx.get("destination_id") or ctx.destination_id == &"":
        return _build_fallback_context()
    var result := ctx.get("voyage_result", &"")
    if result not in [VOYAGE_RESULT_ARRIVED, VOYAGE_RESULT_RETREATED, VOYAGE_RESULT_FORCED_LANDING]:
        return _build_fallback_context()
    if not ctx.get("resolved_encounters") is Array:
        return _build_fallback_context()
    return ctx

# 从 EncounterContext 派生威胁上下文 (传递给 Combat #12)
func _build_threat_context(encounter_entry: Dictionary) -> Dictionary:
    return {
        "threat_type": _threat_type_for_encounter(encounter_entry.encounter_type),
        "encounter_entry": encounter_entry,
        "hull_band_arrival": _current_encounter_context.hull_band_arrival,
        "scout_efficiency": _scout_efficiency,
    }
```

#### Persistence (#3) 集成

```gdscript
# Navigation 在 voyage_completed 发射后调用
func _persist_voyage_snapshot() -> void:
    var snapshot := {
        "route_id": _active_voyage.route_id,
        "voyage_result": _active_voyage.result,
        "elapsed_time": _elapsed_time,
        "resolved_encounters": _resolved_encounters.duplicate(true),
        "accumulated_damage": _accumulated_damage,
        "revealed_hidden_tags": _revealed_hidden_tags.duplicate(true),
        "hull_band_arrival": _current_hull_band,
        "damaged_slots": _damaged_slots.duplicate(true),
        "encounter_context": _build_encounter_context(),
    }
    Persistence.capture_snapshot("progress.voyage", snapshot)
```

## Alternatives Considered

### Alternative A: EncounterContext 作为 Godot Resource 子类

- **Description**: 创建 `class_name EncounterContext extends Resource`，定义 `@export` 字段
- **Pros**: IDE 类型安全、`.tres` 文件可视化编辑、类型检查自动执行
- **Cons**: Resource 序列化与 ADR-0003 Canonical JSON 不兼容；需要额外的 Resource→JSON 转换层；Exploration 和 Combat 需要依赖 Resource 文件加载而非纯数据传递
- **Rejection Reason**: ADR-0003 规定所有持久化状态使用 Canonical JSON。Dictionary 直接映射到 JSON.stringify() — Resource 需要 `.tres` 或自定义序列化器，增加转换层复杂度且违背统一存储模式（ADR-0005/0007/0009 均使用 Dictionary）

### Alternative B: EncounterContext 字段分散在多个信号参数中

- **Description**: 不使用聚合 Dictionary，而是 Navigation 发射多个独立信号：`route_arrived(route_id, destination_id)` + `encounters_resolved(entries)` + `voyage_damage_accumulated(amount)` 等
- **Pros**: 信号更细粒度，消费方只连接需要的信号
- **Cons**: 信号到达顺序不确定（即使 sync emit，连接顺序可能变化）；消费方必须自己聚合状态，导致 Exploration 内部出现"等待所有信号到达"的复杂状态机；信号级联深度增加（违反 ADR-0002 max depth 2）
- **Rejection Reason**: GDD #10 和 #11 均约定 EncounterContext 为聚合结构。Exploration 的 AC-11-01 要求一个完整的 EncounterContext 对象。拆分信号增加 Exploration 的聚合复杂度且无 gameplay 收益

### Alternative C: Exploration 直接查询 Navigation 获取 EncounterContext

- **Description**: Navigation 不发射信号，Exploration 在需要时调用 `Navigation.get_last_encounter_context()` 拉取
- **Pros**: 拉取模型 — Exploration 控制数据获取时机
- **Cons**: 引入 Navigation→Exploration 的时序耦合 — Exploration 必须知道 Navigation 已终态才能拉取；Navigation 需要保持 EncounterContext 在内存中直到 Exploration 拉取，增加状态生命周期管理；违反"生产者完成后关闭"的 GDD 合约
- **Rejection Reason**: GDD #10 明确约定"事件发出后本系统关闭，不再参与探索过程"。信号推送模型允许 Navigation 在发射 voyage_completed 后完全关闭航程状态

## Consequences

### Positive

- **单一跨系统契约**: 所有消费方 (Exploration, Combat, Persistence, UI) 从同一个 EncounterContext Dictionary 读取，消除字段名不一致
- **类型安全降级**: Fallback context 保证即使上游数据损坏，玩家仍可进入探索（不阻塞 game flow），同时记录错误供调试
- **持久化简单**: Dictionary 直接序列化为 JSON — 不依赖 Resource 序列化或自定义转换器
- **信号耦合最小**: Navigation 只发射一个 voyage_completed 信号 — 新增消费方（如 Phase 3+ 的 #14 委托航程）只需连接同一信号
- **存档恢复路径明确**: progress.voyage snapshot 包含完整 EncounterContext — 读档后 Exploration 可不经过 Navigation 直接恢复

### Negative

- **Dictionary 缺乏编译时类型检查**: 字段名拼写错误只能在运行时通过 validation 捕获
- **EncounterType 枚举分散**: encounter_type 常量在 Navigation 定义，但 Exploration 和 Combat 需要知道这些值以映射场景元素 — 需在 Registry (#1) 中注册 encounter_types 内容定义
- **ThreatContext 派生逻辑在 Exploration**: Combat 不直接消费 EncounterContext — Exploration 作为中间层派生 ThreatContext。如果 Exploration 派生逻辑错误，Combat 收到错误上下文

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| EncounterContext 字段随 GDD 迭代增减，但 ADR 未同步更新 | Medium | Medium — 消费方使用过期字段假设 | 新增 encounter_type 或 EncounterEntry 字段时，Registry (#1) 更新 encounter_types 内容定义；ADR-0010 的 encounter_type 全集由 Registry 查询代替硬编码列表 |
| Fallback context 掩盖了上游 bug | Medium | Low — 玩家无感知但数据丢失 | Fallback 触发时写入 internal_error_log；QA smoke test 检查日志中无 fallback 触发 |
| EncounterContext 在信号回调中被修改 | Very Low — 信号回调为只读消费 | Medium — 修改会影响后续消费方 | .duplicate(true) 在 _build_encounter_context() 中深拷贝所有 Array；消费方文档约定只读 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| navigation-route-risk.md | EncounterContext 产出: route_id, destination_id, voyage_result, resolved_encounters[] | EncounterContext 结构定义 (§2) + 按 voyage_result 的字段有效性表 (§3) |
| navigation-route-risk.md | 航行结束多系统写入顺序: (1) #8 → (2) #6 → (3) #11 EncounterContext → (4) #17 → (5) #3 | 写入顺序在 Decision §1 和 Navigation 生产端 §Key Interfaces 中体现 |
| navigation-route-risk.md | 11 个 MVP EncounterEntry 类型 + 3 个风险标签遭遇表 | 遭遇表索引 (§7) — 10 个特殊效果标记常量 |
| navigation-route-risk.md | AC-90: EncounterContext 包含 voyage_result + 完整 resolved_encounters | EncounterContext 字段定义 + voyage_completed 信号合约 |
| exploration-scavenge-scenario.md | AC-11-01: EncounterContext 含 voyage_result 和 destination_id — 决定 ARRIVING 入口 | _validate_encounter_context() 校验 + voyage_result 路由 |
| exploration-scavenge-scenario.md | EC-11-07: EncounterContext 缺失/格式错误时的 fallback | Fallback context 定义 (§5) + 触发条件枚举 |
| exploration-scavenge-scenario.md | Interactions 表: 消费 EncounterContext {route_id, destination_id, voyage_result, resolved_encounters[]} | 完整的 EncounterContext Dictionary 结构 — 比 GDD 最小约定多携带 accumulated_damage 等字段供场景生成使用 |
| combat-threat-handling.md | build_threat_context(encounter_entry) — Exploration 从 EncounterContext 派生威胁上下文 | ThreatContext 派生接口 (§Key Interfaces — Exploration 消费端) |

## Performance Implications

- **CPU**: Navigation 构建 EncounterContext — O(N) 其中 N = resolved_encounters 数量 (MVP 最多 20 个)。Dictionary 构建 + Array.duplicate(true) — < 0.05ms。Exploration 校验 — 4 次 Dictionary.get() + 2 次类型检查 — < 0.01ms
- **Memory**: 完整 EncounterContext — ~2KB (20 个 EncounterEntry × ~100 bytes + 顶层字段)。航行结束后 Context 存活至 Exploration 场景加载完成 (~2-5 秒)，然后随 Navigation 清理
- **Load Time**: 存档恢复时 — EncounterContext 从 JSON 解析，< 0.5ms
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. Navigation (#10) 实现 `_build_encounter_context()` 和 `voyage_completed` 信号
2. Exploration (#11) 实现 `_validate_encounter_context()` 和 `_build_fallback_context()`
3. Exploration (#11) 实现 `_on_voyage_completed()` 信号处理 + ARRIVING 阶段入口路由
4. Exploration (#11) 实现 `_build_threat_context()` 供 Combat (#12) 消费
5. Navigation (#10) 实现 `_persist_voyage_snapshot()` 供 Persistence (#3) 存档
6. Registry (#1) 注册 11 个 encounter_type 内容定义 + 10 个特殊效果标记
7. 单元测试: EncounterContext 构建 (所有 3 种 voyage_result), fallback 触发 (null/缺字段/无效枚举), 符号约 (字段完整性), 遭遇表常量与 EncounterEntry 一致性

## Validation Criteria

- EncounterContext 所有 9 个顶层字段在 voyage_completed 信号负载中存在且类型正确
- voyage_result 为 ARRIVED 时 forced_landing_position 为空字符串
- voyage_result 为 FORCED_LANDING 时 forced_landing_position 非空
- resolved_encounters 中每个条目包含 6 个字段: encounter_type, hazard_tag, damage_amount, special_effect_tags, was_hidden, time_offset
- Fallback context 在 5 种触发条件下正确构建（null ctx, 缺 route_id, 缺 destination_id, 无效 voyage_result, resolved_encounters 非 Array）
- progress.voyage snapshot 包含完整 encounter_context — JSON 序列化/反序列化往返无字段丢失
- Exploration 在 ARRIVED vs FORCED_LANDING 下正确路由到不同场景入口（正常入口 vs 坠机点）
- Combat 消费的 ThreatContext 由 Exploration 正确从 EncounterEntry 派生 — threat_type 与 encounter_type 映射正确

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — Navigation (#10) Phase 5, Exploration (#11) Phase 5 启动顺序
- **ADR-0002**: Signal 通信协议 — voyage_completed typed signal, sync emit, emit-after-mutation
- **ADR-0003**: 存档系统 — progress.voyage snapshot package 包含 EncounterContext
- **ADR-0009**: 飞艇模块与船体状态 — Navigation 通过 apply_hull_damage/apply_module_damage 写入 #8
- **GDD #10**: navigation-route-risk.md — EncounterContext 生产端定义
- **GDD #11**: exploration-scavenge-scenario.md — EncounterContext 消费端定义
- **GDD #12**: combat-threat-handling.md — ThreatContext 消费（通过 Exploration 派生）
