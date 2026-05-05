# ADR-0007: 知识状态与能力解锁架构 — IntelManager Autoload #6

## Status
Proposed

## Date
2026-05-05

## Summary
IntelManager 作为 Autoload #6，拥有全部玩家知识状态和能力解锁状态的唯一真相源。管理三类条目——规律类知识（3 条，4 级状态机）、地点类知识（任意数量，4 级状态机）、能力条目（3 条，2 级状态机）——通过 Dictionary 后端存储。6 个上游事件接收接口、7 个下游只读查询接口、6 个 typed signal 用于状态变更通知。多路径能力解锁采用跨路径 OR + 路径内 AND 逻辑，每种能力 2-4 条独立解锁路径。所有状态通过 ADR-0003 Canonical JSON 快照包持久化。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Game Logic / Data |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构与信号，无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/breaking-changes.md`, `design/gdd/player-knowledge-intel.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | 3 条规律 × 6 观测事件的分数累积正确性；3 条能力 × 4 条路径的解锁条件 AND/OR 逻辑；IntelConsumeResult 三重效果（地点推进+观测事件+能力解锁）单次消耗正确性 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #6 启动顺序, Phase 3 core_data_ready)；ADR-0002 (Signal 通信协议)；ADR-0003 (快照包持久化)；ADR-0005 (ResourcesManager consume_intel 调用入口)；ADR-0006 (Web 单线程约束) |
| **Enables** | ADR-0008 (航图路线状态机 — 消费 query_route_knowledge/query_route_accessibility)；ADR-0009 (模块/船体 — 消费 query_ability_state)；ADR-0010 (EncounterContext — 消费 query_pattern_state)；ADR-0013 (Hub 场景)；ADR-0015 (伙伴状态机 — 消费 reveal_rumor) |
| **Blocks** | 航图系统实现、探索系统实现 — 均依赖知识查询接口返回正确状态 |
| **Ordering Note** | 应在 Core 层 ADR 中第一个 Accepted — Chart (#9), Modules (#8) 均依赖 Intel 的查询接口 |

## Context

### Problem Statement

《云海织航》的核心进展层分为两轴：**知识**（玩家知道什么）和**能力**（玩家能做到什么）。GDD #6 定义了完整的状态机、算法和规则，但未做出架构层面的部署决策：IntelManager 以何种形式存在、状态如何存储、如何与 8 个上下游系统通信、观测事件从何进入、能力解锁条件如何检查、以及如何与 ADR-0003 持久化契约集成。

本 ADR 将这些设计规则转化为架构决策，使 IntelManager 成为 Core 层的首个可实现的 Autoload 系统。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑层，无引擎 API 风险
- **Web 单线程**: 所有状态变更在同一帧内同步完成 — 无需锁
- **ADR-0002 信号协议**: 所有信号 typed params, sync emit, max depth 2, {noun}_{verb_past} 命名
- **ADR-0003 持久化**: 状态通过 `progress.intel` snapshot package 保存/恢复
- **ADR-0005 ResourcesManager**: `consume_intel()` 由 ResourcesManager 调用 — IntelManager 定义算法，ResourcesManager 拥有消耗 UI
- **ADR-0001 启动顺序**: IntelManager 在 Phase 3 (core_data_ready) 初始化，依赖 Phase 2 的 Registry + Persistence

### Requirements

- 管理 3 类运行时状态：规律知识 (3 patterns)、地点知识 (N locations)、能力条目 (3 abilities)
- 接收 6 种上游事件：consume_intel, reveal_rumor, player_arrived_at, report_observation_event, report_pattern_usage_success, report_navigation_event, on_partner_joined, on_repair_completed
- 提供 7 种下游只读查询：知识状态、规律状态、能力状态、情报已消耗检查、图鉴日志、能力列表、路线可通行性
- 多路径能力解锁：跨路径 OR + 路径内 AND，每条能力 2-4 条独立路径
- 传闻冲突解决：多来源保留 + 验证后置信度调整
- 不可退化保证：verified/confirmed/unlocked 为终态

## Decision

### 1. IntelManager 作为 Autoload #6

IntelManager 在 Phase 3 (core_data_ready) 中初始化。`_ready()` 仅执行信号声明和 null 检查；实际状态初始化在收到 `core_data_ready` 信号后执行。

```
Autoload 顺序 (Phase 3):
  #4 InteractionRegistry  ──┐
  #5 ResourcesManager      ├── 并行接收 core_data_ready
  #6 IntelManager         ──┘
  #9 Chart                 ── 在 core_ready 后初始化
```

IntelManager 是以下状态的唯一真相源：
- 所有地点知识状态 (knowledge_state)
- 所有规律观测分数和状态 (pattern_state)
- 所有能力解锁状态 (ability_state)
- 已消耗情报 ID 集合 (consumed_intel_ids)
- 传闻来源置信度 (rumor_sources)
- 雾气穿越计数 (fog_traversal_count)
- 活跃伙伴集合 (active_crew)

### 2. Dictionary 后端存储

遵循 ADR-0005 的先例，使用 Dictionary[StringName, ...] 存储所有状态——直接映射到 ADR-0003 Canonical JSON，无需转换层。

```gdscript
# === IntelManager 状态结构 ===

# 地点知识: StringName → int (enum: 0=UNKNOWN, 1=RUMORED, 2=IDENTIFIED, 3=VERIFIED)
var knowledge_state: Dictionary = {}  # Dictionary[StringName, int]

# 规律状态: StringName → PatternState
# PatternState = {
#   observation_score: int,
#   triggered_events: Array[StringName],
#   pattern_usage_success: bool
# }
var pattern_state: Dictionary = {}  # Dictionary[StringName, Dictionary]

# 能力状态: StringName → int (0=LOCKED, 1=UNLOCKED)
var ability_state: Dictionary = {}  # Dictionary[StringName, int]

# 已消耗情报: Array[StringName] — set semantics, no duplicates
var consumed_intel_ids: Array = []  # Array[StringName]

# 传闻来源: StringName → Dictionary[StringName, RumorSource]
# RumorSource = {source_tag: StringName, hazard_tags: Array, confidence: int}
var rumor_sources: Dictionary = {}  # per-location

# 雾气穿越计数: int
var fog_traversal_count: int = 0

# 活跃伙伴: Array[StringName]
var active_crew: Array = []  # Array[StringName]
```

**常量定义：**

```gdscript
# 知识状态枚举
const KNOWLEDGE_UNKNOWN: int = 0
const KNOWLEDGE_RUMORED: int = 1
const KNOWLEDGE_IDENTIFIED: int = 2
const KNOWLEDGE_VERIFIED: int = 3

# 规律状态枚举
const PATTERN_UNDISCOVERED: int = 0
const PATTERN_PARTIALLY_OBSERVED: int = 1
const PATTERN_CONFIRMED: int = 2

# 能力状态枚举
const ABILITY_LOCKED: int = 0
const ABILITY_UNLOCKED: int = 1

# 观测事件权重
const WEIGHT_NARRATIVE_HINT: int = 1
const WEIGHT_LOG_FRAGMENT: int = 2
const WEIGHT_PARTNER_COMMENT: int = 3
const WEIGHT_PASSIVE_OBSERVATION: int = 4
const WEIGHT_ACTIVE_INVESTIGATION: int = 7

# 阈值
const PARTIAL_THRESHOLD_DEFAULT: int = 5
const CONFIRMATION_THRESHOLD_DEFAULT: int = 10
```

### 3. 信号接口 (状态变更通知 — fire-and-forget)

所有信号遵循 ADR-0002: typed params, sync emit, {noun}_{verb_past} 命名：

```gdscript
# === IntelManager 信号声明 ===

# 地点知识变化
signal knowledge_advanced(location_id: StringName, previous_state: int, new_state: int)

# 规律观测事件触发
signal pattern_observed(pattern_id: StringName, event_id: StringName, new_score: int)

# 规律状态转换 (partially_observed 或 confirmed)
signal pattern_state_changed(pattern_id: StringName, previous_state: int, new_state: int)

# 规律使用成功 (confirmed+ 激活)
signal pattern_usage_confirmed(pattern_id: StringName)

# 能力解锁
signal ability_unlocked(ability_id: StringName, unlock_path: StringName)

# 情报消耗完成 (反馈通知)
signal intel_consumed(intel_id: StringName)

# 情报消耗失败
signal intel_consume_failed(intel_id: StringName, reason: StringName)

# 传闻接收
signal rumor_received(location_id: StringName, source_tag: StringName)

# 传闻置信度变更
signal rumor_confidence_changed(source_tag: StringName, location_id: StringName, old_confidence: int, new_confidence: int)
```

### 4. 方法接口

#### 4a. 上游事件接收 (其他系统调用 IntelManager)

```gdscript
# 情报消耗 — 由 ResourcesManager 调用
func consume_intel(intel_id: StringName) -> Dictionary:
    # 返回 IntelConsumeResult Dictionary:
    # {
    #   success: bool,
    #   error_code: StringName,           # "" 或 "ERR_INTEL_ALREADY_CONSUMED" / "ERR_INTEL_NOT_FOUND"
    #   intel_id: StringName,
    #   intel_display_name: String,
    #   location_advancements: Array[Dictionary],  # [{location_id, previous_state: int, new_state: int}]
    #   ability_unlocks: Array[Dictionary],         # [{ability_id, ability_display_name, unlock_path}]
    #   pattern_observations: Array[Dictionary]     # [{pattern_id, event_id, event_type, added_score, new_score, prev_state: int, new_state: int}]
    # }

# 传闻接收 — 由伙伴系统调用
func reveal_rumor(location_id: StringName, source_tag: StringName, hazard_tags: Array, confidence: int) -> void

# 玩家到达地点 — 由移动系统调用
func player_arrived_at(location_id: StringName) -> void

# 观测事件报告 — 由探索/航行/伙伴/交互系统调用
func report_observation_event(pattern_id: StringName, event_id: StringName) -> void

# 规律使用成功 — 由探索/航行系统调用
func report_pattern_usage_success(pattern_id: StringName) -> void

# 航行事件 — 由航行系统调用
func report_navigation_event(event_type: StringName, payload: Dictionary) -> void

# 伙伴加入 — 由伙伴系统调用
func on_partner_joined(partner_id: StringName) -> void

# 伙伴离开 — 由伙伴系统调用
func on_partner_left(partner_id: StringName) -> void

# 修复完成 — 由修复系统调用
func on_repair_completed(repair_node_id: StringName) -> void
```

#### 4b. 下游只读查询 (其他系统从 IntelManager 读取)

```gdscript
# 地点知识查询
func query_knowledge_state(location_id: StringName) -> Dictionary:
    # 返回 {state: int, rumor_sources: Array, verified: bool, personal_notes: String}

# 路线知识查询 (聚合 — 供航图系统)
func query_route_knowledge(route_id: StringName) -> Dictionary:
    # 返回 {state: int, visible_hazards: Array, hidden_hazard_count: int, sources: Array}

# 路线可通行性查询 (聚合 — 供航图/航行系统)
func query_route_accessibility(route_id: StringName) -> Dictionary:
    # 返回 {traversable: bool, blocked_by_ability: StringName, blocked_by_knowledge: bool}

# 规律状态查询
func query_pattern_state(pattern_id: StringName) -> Dictionary:
    # 返回 {state: int, observation_score: int, is_confirmed_plus: bool, triggered_events: Array}

# 能力状态查询
func query_ability_state(ability_id: StringName) -> int:
    # 返回 ABILITY_LOCKED 或 ABILITY_UNLOCKED

# 情报已消耗检查
func is_intel_consumed(intel_id: StringName) -> bool

# 图鉴日志 (规律列表)
func get_pattern_log() -> Array[Dictionary]:
    # 返回已进入 partially_observed 及以上状态的规律列表

# 能力列表
func get_ability_list() -> Array[Dictionary]:
    # 返回所有能力及其状态 {ability_id, display_name, state: int, unlock_hint: String}

# 地点发现状态
func query_location_discovery(location_id: StringName) -> Dictionary:
    # 返回 {state: int, hazard_visibility: Array, sources: Array, personal_notes: String}
```

### 5. 算法设计

#### 5a. consume_intel() 算法

5 条规则按序执行，返回 Dictionary 结构的 IntelConsumeResult：

```
Rule 1: 已消耗检查 — if intel_id in consumed_intel_ids → error
Rule 2: 推进关联地点知识 — unknown/rumored → identified
Rule 3: 添加规律观测事件 — log_fragment (weight=2)
Rule 4: 检查能力解锁条件 — check_unlock_conditions() for linked abilities
Rule 5: 标记 intel 已消耗 — consumed_intel_ids.append(intel_id)
```

#### 5b. 观测分数累积

```
observation_score(pattern_id) = SUM(weight(e)) for each e in triggered_events[pattern_id]
```

每个事件 ID 仅计一次。事件类型权重: narrative_hint=1, log_fragment=2, partner_comment=3, passive_observation=4, active_investigation=7。

#### 5c. 规律状态转换

```
next_state(pattern_id):
  IF observation_score >= confirmation_threshold → CONFIRMED
  ELIF observation_score >= partial_threshold → PARTIALLY_OBSERVED
  ELSE → UNDISCOVERED

is_confirmed_plus(pattern_id):
  next_state == CONFIRMED AND pattern_usage_success == true
```

#### 5d. 能力解锁条件检查

```
ability_unlocked(ability_id) = OR(path_satisfied(p)) for p in unlock_paths[ability_id]
path_satisfied(path) = AND(condition_met(c)) for c in path.conditions
```

3 条 MVP 能力各有 2-4 条解锁路径：

| 能力 | Path A | Path B | Path C | Path D |
|------|--------|--------|--------|--------|
| `ability.bird-flight-understanding` | pattern confirmed | intel consumed + obs event | partner.old-sailor in crew + passive obs | — |
| `ability.lighthouse-signal-interpretation` | pattern confirmed | intel consumed + obs event | repair_lighthouse_01 completed | partner.lighthouse-keeper-descendant in crew + 1 lighthouse visited |
| `ability.fog-navigation` | pattern confirmed | intel consumed + obs event | 3 fog traversals completed | partner.cartographer in crew + 2 fog obs events |

#### 5e. 传闻冲突解决

- `verified` 胜出一切 — 传闻不覆盖
- `identified` 胜出 `rumored` — 可靠情报替换传闻风险标签
- 两个 `rumored` 来源冲突 → 同时保留，各自标注来源名称和置信度
- 玩家亲身验证后: 一致来源 +25 置信度，矛盾来源 -30 置信度 (最低 0)
- 置信度 >= 67 的来源: `reveal_rumor()` 效果等同于可靠情报 (unknown → identified)

#### 5f. 能力解锁条件重评估触发点

每次以下事件发生后，遍历所有 `locked` 能力调用 `check_unlock_conditions()`:
- `consume_intel()` 完成
- `report_observation_event()` 完成
- `report_pattern_usage_success()` 完成
- `player_arrived_at()` 完成
- `report_navigation_event()` 完成
- `on_partner_joined()` 完成
- `on_repair_completed()` 完成

已解锁能力跳过检查。

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                    IntelManager (Autoload #6)                         │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    STATE STORAGE (Dictionary)                  │   │
│  │                                                                │   │
│  │  knowledge_state:    Dict[StringName, int]                    │   │
│  │  pattern_state:      Dict[StringName, PatternState]           │   │
│  │  ability_state:      Dict[StringName, int]                    │   │
│  │  consumed_intel_ids: Array[StringName]                         │   │
│  │  rumor_sources:      Dict[StringName, Dict]                   │   │
│  │  fog_traversal_count: int                                      │   │
│  │  active_crew:        Array[StringName]                         │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              UPSTREAM EVENTS (ingress)                          │  │
│  │                                                                │  │
│  │  ResourcesManager  ──→ consume_intel()                         │  │
│  │  PartnerSystem     ──→ reveal_rumor() / on_partner_joined()    │  │
│  │  MovementSystem    ──→ player_arrived_at()                     │  │
│  │  ExplorationSystem ──→ report_observation_event()              │  │
│  │  NavigationSystem  ──→ report_navigation_event()               │  │
│  │  WorldRepair       ──→ on_repair_completed()                   │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              DOWNSTREAM QUERIES (egress)                        │  │
│  │                                                                │  │
│  │  Chart      ←── query_route_knowledge / query_route_accessibility│
│  │  Navigation ←── query_pattern_state                            │  │
│  │  Exploration←── query_location_discovery                       │  │
│  │  WorldRepair←── query_ability_state                             │  │
│  │  Partner    ←── query_knowledge_state                           │  │
│  │  UIManager  ←── get_pattern_log / get_ability_list             │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │                                        │
│  ┌───────────────────────────┼────────────────────────────────────┐  │
│  │              SIGNALS (state-change notification)                │  │
│  │                                                                │  │
│  │  knowledge_advanced / pattern_state_changed / ability_unlocked │  │
│  │  pattern_observed / pattern_usage_confirmed                     │  │
│  │  intel_consumed / intel_consume_failed                          │  │
│  │  rumor_received / rumor_confidence_changed                      │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

#### consume_intel() 返回结构

```gdscript
# IntelConsumeResult — Dictionary 结构 (遵循 ADR-0002: 禁止 signal Dictionary payload，但方法返回值可用 Dictionary)
# 方法返回 Dictionary 是本项目的已确立模式 (见 ADR-0004 query_focus_state(), ADR-0005 get_storage_summary())
func consume_intel(intel_id: StringName) -> Dictionary:
    # Returns:
    # {
    #   "success": bool,
    #   "error_code": StringName,          # "" on success
    #   "intel_id": StringName,
    #   "intel_display_name": String,
    #   "location_advancements": Array[Dictionary],
    #       # [{location_id: StringName, previous_state: int, new_state: int}]
    #   "ability_unlocks": Array[Dictionary],
    #       # [{ability_id: StringName, ability_display_name: String, unlock_path: StringName}]
    #   "pattern_observations": Array[Dictionary]
    #       # [{pattern_id: StringName, event_id: StringName, event_type: StringName,
    #       #   added_score: int, new_observation_score: int,
    #       #   previous_pattern_state: int, new_pattern_state: int}]
    # }
```

#### 能力解锁路径定义

```gdscript
# 静态配置 — 在 Registry 初始化时加载到 IntelManager
# 每条能力一个解锁定义，含 2-4 条路径，每条路径含 1-3 个条件

var ability_unlock_paths: Dictionary = {
    "ability.bird-flight-understanding": {
        "paths": [
            {
                "path_id": "path_a_pattern_confirmed",
                "conditions": [
                    {"type": "pattern_state", "pattern_id": "pattern.bird-flight-direction", "required_state": PATTERN_CONFIRMED}
                ]
            },
            {
                "path_id": "path_b_intel_observation",
                "conditions": [
                    {"type": "intel_consumed", "intel_id": "intel.bird-migration-notes"},
                    {"type": "observation_event_count", "pattern_id": "pattern.bird-flight-direction", "min_count": 1}
                ]
            },
            {
                "path_id": "path_c_partner_passive",
                "conditions": [
                    {"type": "partner_in_crew", "partner_id": "partner.old-sailor"},
                    {"type": "observation_event_type_count", "pattern_id": "pattern.bird-flight-direction", "event_type": "passive_observation", "min_count": 1}
                ]
            }
        ]
    },
    "ability.lighthouse-signal-interpretation": {
        "paths": [
            {
                "path_id": "path_a_pattern_confirmed",
                "conditions": [
                    {"type": "pattern_state", "pattern_id": "pattern.lighthouse-signals", "required_state": PATTERN_CONFIRMED}
                ]
            },
            {
                "path_id": "path_b_intel_observation",
                "conditions": [
                    {"type": "intel_consumed", "intel_id": "intel.signal-codex"},
                    {"type": "observation_event_count", "pattern_id": "pattern.lighthouse-signals", "min_count": 1}
                ]
            },
            {
                "path_id": "path_c_world_repair",
                "conditions": [
                    {"type": "repair_completed", "repair_node_id": "repair_lighthouse_01"}
                ]
            },
            {
                "path_id": "path_d_partner_visit",
                "conditions": [
                    {"type": "partner_in_crew", "partner_id": "partner.lighthouse-keeper-descendant"},
                    {"type": "location_visit_count", "location_tag": "has_lighthouse", "min_count": 1, "required_state": KNOWLEDGE_VERIFIED}
                ]
            }
        ]
    },
    "ability.fog-navigation": {
        "paths": [
            {
                "path_id": "path_a_pattern_confirmed",
                "conditions": [
                    {"type": "pattern_state", "pattern_id": "pattern.fog-navigation", "required_state": PATTERN_CONFIRMED}
                ]
            },
            {
                "path_id": "path_b_intel_observation",
                "conditions": [
                    {"type": "intel_consumed", "intel_id": "intel.fog-compass-manual"},
                    {"type": "observation_event_count", "pattern_id": "pattern.fog-navigation", "min_count": 1}
                ]
            },
            {
                "path_id": "path_c_experience",
                "conditions": [
                    {"type": "fog_traversal_count", "min_count": 3}
                ]
            },
            {
                "path_id": "path_d_partner_observation",
                "conditions": [
                    {"type": "partner_in_crew", "partner_id": "partner.cartographer"},
                    {"type": "observation_event_count", "pattern_id": "pattern.fog-navigation", "min_count": 2}
                ]
            }
        ]
    }
}
```

#### 持久化接口 (ADR-0003 集成)

```gdscript
# 在 core_data_ready 阶段注册到 Persistence
func _on_core_data_ready() -> void:
    Persistence.register_domain_serializer("intel", _serialize_intel)
    # ... initialize state from scratch or load from save

func _serialize_intel() -> Dictionary:
    return {
        "domain_id": "intel",
        "knowledge_state": knowledge_state,     # Dict[StringName, int]
        "pattern_state": pattern_state,         # Dict[StringName, Dict]
        "ability_state": ability_state,         # Dict[StringName, int]
        "consumed_intel_ids": consumed_intel_ids,  # Array[StringName]
        "rumor_sources": rumor_sources,         # Dict[StringName, Dict]
        "fog_traversal_count": fog_traversal_count,  # int
        "active_crew": active_crew              # Array[StringName]
    }

func _deserialize_intel(snapshot: Dictionary) -> void:
    knowledge_state = snapshot.get("knowledge_state", {})
    pattern_state = snapshot.get("pattern_state", {})
    ability_state = snapshot.get("ability_state", {})
    consumed_intel_ids = snapshot.get("consumed_intel_ids", [])
    rumor_sources = snapshot.get("rumor_sources", {})
    fog_traversal_count = snapshot.get("fog_traversal_count", 0)
    active_crew = snapshot.get("active_crew", [])
    # 恢复后触发必要的信号 (ability_unlocked for each unlocked ability, etc.)
```

#### MVP 起始状态

```gdscript
# 在 core_data_ready 首次初始化时（新游戏）设置
func _init_new_game_state() -> void:
    # 地点知识起始状态
    knowledge_state["route.sky-reef-arc-01"] = KNOWLEDGE_IDENTIFIED
    knowledge_state["route.high-risk-mvp"] = KNOWLEDGE_RUMORED
    knowledge_state["location.glass-harbor"] = KNOWLEDGE_IDENTIFIED
    # 所有其他地点在首次查询时默认为 UNKNOWN

    # 所有规律: UNDISCOVERED (默认)
    # 所有能力: LOCKED (默认)
    # consumed_intel_ids: empty (默认)
    # rumor_sources: empty (默认)
    # fog_traversal_count: 0 (默认)
    # active_crew: empty (默认)
```

#### 不可退化保证

```gdscript
# 地点知识 — 状态转换前防御性检查
func _can_transition_location(current: int, target: int) -> bool:
    if current == KNOWLEDGE_VERIFIED:
        return false  # 终态 — 任何写入被拒绝
    if current == KNOWLEDGE_IDENTIFIED and target == KNOWLEDGE_RUMORED:
        return false  # 可靠信息不可退回传闻
    if current == KNOWLEDGE_RUMORED and target == KNOWLEDGE_UNKNOWN:
        return false  # 已知不可退回未知
    return true

# 规律状态 — 不可退化
# confirmed → partially_observed: 无效
# confirmed/partially_observed → undiscovered: 无效

# 能力状态 — unlocked → locked: 无效
```

## Alternatives Considered

### Alternative A: Godot Resource 类型化知识条目

- **Description**: 每条知识/能力定义为 `KnowledgeEntry extends Resource`，使用 `.tres` 文件定义静态数据，运行时加载
- **Pros**: 编辑器可视化编辑；类型安全
- **Cons**: Resource 序列化与 ADR-0003 Canonical JSON 冲突（需要转换层）；运行时实例化开销；无法直接 `JSON.stringify()`
- **Rejection Reason**: 与 ADR-0005 的 Dictionary 选择一致——Dictionary 直接映射到 Canonical JSON，无需转换层。知识条目的静态定义属于 Registry (ADR 待创建)，运行时状态由 IntelManager 维护

### Alternative B: 每个领域系统自管知识状态

- **Description**: 航图系统拥有路线知识状态、探索系统拥有地点知识状态、航行系统拥有规律观测状态——没有集中的 IntelManager
- **Pros**: 无集中依赖；每个系统自治
- **Cons**: GDD 明确指定「所有玩家知识状态的唯一真相源」——去中心化导致数据重复、来源不一致、传闻冲突无法跨系统仲裁。`consume_intel()` 需要同时更新多个系统的状态 → 要么使用信号链 > 2 跳（违反 ADR-0002），要么 IntelManager 被偷偷重建为协调器
- **Rejection Reason**: 违反 GDD #6 核心架构原则。集中式真相源是知识系统正确性的基础

### Alternative C: 观测事件使用信号而非直接方法调用

- **Description**: 下游系统通过 emit signal 报告观测事件，而非调用 `report_observation_event()`
- **Pros**: 更松耦合——IntelManager 不需要知道谁在报告事件
- **Cons**: 信号无返回值——无法确认事件是否被成功记录。调试困难——无法追踪"谁触发了这个观测"。Godot 信号是 fire-and-forget 语义
- **Rejection Reason**: 观测事件报告是 request-response 语义（"我观察到了这个，请记录"），不是通知语义（"某事发生了"）。ADR-0002 规定 request-response 使用直接方法调用

## Consequences

### Positive

- **单一真相源**: 所有知识/能力状态在 IntelManager 中统一管理 — 无数据重复、无跨系统不一致
- **多路径解锁可配置**: 数据驱动的解锁路径定义 — 添加新路径无需修改算法代码
- **GDScript 纯逻辑**: 无引擎 API 依赖 — 单元测试可直接实例化 IntelManager 并验证状态机逻辑
- **持久化简单**: Dictionary 直接映射到 Canonical JSON — 无需序列化转换层
- **传闻冲突解决**: 多来源保留机制在 GDD 中明确定义 — 架构实现直接映射

### Negative

- **Autoload #6 依赖**: 任何需要知识/能力状态的系统必须依赖 IntelManager — 增加了启动顺序约束
- **Dictionary 类型安全缺失**: GDScript Dictionary 无编译期字段检查 — 需要在运行时验证 snapshot 恢复的数据结构
- **上游接口较多**: 8 个方法接收来自 6 个不同系统的事件 — 每个上游系统需要知道 IntelManager 的调用签名

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| 存档恢复时 snapshot 结构与当前注册表不一致 (ID 重命名/删除) | Medium | High — 知识状态丢失或错位 | 恢复时交叉验证：对每个 key 与 Registry 比对。不存在的 ID 保留原始数据不静默删除；查询不存在的 ID 返回安全默认值 |
| 能力解锁条件路径定义错误导致能力永久锁死 | Low — 每条能力至少 2 条独立路径 | High — 玩家无法推进 | 启动时验证：每条能力的每条路径的每个条件类型在 condition_evaluators 中有对应处理器。缺失处理器 → 启动 error 日志。设计保证每条能力至少一条路径在 MVP 流程中可达 |
| 多系统同时触发重评估导致重复解锁 | Low — 单线程执行保证串行 | Low — 同一帧内多次重评估但能力已解锁后跳过后续检查 | 单线程执行（ADR-0006）保证同一帧内调用串行。已解锁能力在 check_unlock_conditions() 首行被跳过 |
| 传闻来源置信度在长时间游戏中溢出 | Very Low — 有 floor(0) 保护 | Low — 置信度不合理 | 置信度范围 [0, 100] 有 floor/ceiling 约束；验证调整 ±25/±30 不会在正常游戏中溢出 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| player-knowledge-intel.md | Part 1: 唯一真相源 — 所有玩家知识状态的唯一真相源 | IntelManager 作为唯一 Autoload 拥有全部知识/能力状态；其他系统只读查询 |
| player-knowledge-intel.md | Part 2: 规律知识状态机 (undiscovered→partially_observed→confirmed+pattern_usage_success) | `pattern_state` Dictionary 存储 observation_score + triggered_events + pattern_usage_success；状态转换算法 |
| player-knowledge-intel.md | Part 3: 地点知识状态机 + 传闻冲突解决 + 置信度调整 | `knowledge_state` Dictionary + `rumor_sources` per-location；多来源保留 + 验证后 ±25/±30 置信度调整 |
| player-knowledge-intel.md | Part 4: 能力多路径解锁 (Path A/B/C/D) | `ability_unlock_paths` 数据驱动配置；跨路径 OR + 路径内 AND 条件检查 |
| player-knowledge-intel.md | Part 5: IntelConsumeResult 算法 (5 条规则) | `consume_intel()` 方法 — 按序执行已消耗检查→地点推进→观测事件添加→能力解锁检查→标记已消耗 |
| player-knowledge-intel.md | Part 6: 伙伴侦察 `reveal_rumor()` 接口 | `reveal_rumor()` 方法 + 置信度区间映射 (0-33/34-66/67-100) + confidence>=67 等同于可靠情报 |
| player-knowledge-intel.md | Part 7: MVP 起始状态 (1 identified route + 1 rumored route + 其余 unknown) | `_init_new_game_state()` 设置 3 个初始状态 + 所有规律 undiscovers + 所有能力 locked |
| player-knowledge-intel.md | 重评估触发点: 7 种状态变更后检查能力解锁 | 每个上游事件方法完成后调用 `_reevaluate_ability_unlocks()` — 遍历所有 locked 能力 |
| player-knowledge-intel.md | 不可退化: verified/confirmed/unlocked 为终态 | `_can_transition_location()` 防御性检查 + pattern_state 转换前验证 + ability_state 已解锁跳过 |

## Performance Implications

- **CPU**: 所有操作 O(1) Dictionary 查找。`consume_intel()` 最坏情况: 遍历 linked_content_ids (通常 1-3) + linked_patterns (通常 0-1) + 重评估 3 条能力 = < 0.1ms。观测事件触发: O(1) 追加到 Array + 状态检查。能力解锁重评估: 遍历 3 条能力 × 最多 4 条路径 × 最多 3 条件 = 最多 36 次条件检查 — < 0.05ms
- **Memory**: 地点知识 ~100 entries × ~100 bytes = ~10KB。规律状态 3 × ~200 bytes = ~600 bytes。能力状态 3 × ~50 bytes = ~150 bytes。传闻来源 ~50 entries × ~200 bytes = ~10KB。总计 < 50KB
- **Load Time**: 启动时无文件 I/O — 状态从 Persistence snapshot 恢复或初始化为 MVP 默认值。反序列化 < 1ms
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。IntelManager 作为新 Autoload 实现在 `src/` 中。

实现检查清单:
1. 在 project.godot 中注册 IntelManager 为 Autoload #6
2. 实现状态 Dictionary 结构和常量枚举
3. 实现 8 个上游事件接收方法 (consume_intel, reveal_rumor, player_arrived_at, report_observation_event, report_pattern_usage_success, report_navigation_event, on_partner_joined/left, on_repair_completed)
4. 实现 9 个下游只读查询方法
5. 实现能力解锁路径条件评估器 (pattern_state, intel_consumed, observation_event_count, observation_event_type_count, partner_in_crew, repair_completed, location_visit_count, fog_traversal_count)
6. 实现 ADR-0003 serializer/deserializer 注册
7. 实现 MVP 起始状态初始化
8. 单元测试: 每条规律的观测分数累积 + 状态转换；每条能力的每条解锁路径；传闻冲突场景；consume_intel 三重效果；已消耗/不存在的边缘情况

## Validation Criteria

- 3 条规律 × 6 观测事件 → observation_score 计算与 GDD 公式一致
- `partial_threshold=5`, `confirmation_threshold=10` → 状态转换正确（含 confirmed+ 增强判定）
- 3 条能力 × 合计 10 条路径 → 每条路径独立可解锁，任意路径满足即解锁
- `consume_intel()` 返回完整的 IntelConsumeResult Dictionary — 地点推进 + 观测添加 + 能力解锁
- 重复消耗同一 intel → `ERR_INTEL_ALREADY_CONSUMED`
- 传闻冲突: 两个来源的不同风险标签同时保留；验证后置信度正确调整
- 不可退化: verified 地点拒绝 rumor 写入；confirmed 规律不退回；unlocked 能力不被重新锁定
- 存档→读档: 所有状态完全恢复

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — IntelManager 为 Autoload #6，Phase 3 启动
- **ADR-0002**: Signal 通信协议 — 所有信号 typed params, sync emit, max depth 2
- **ADR-0003**: 存档系统 — `progress.intel` snapshot package, Canonical JSON
- **ADR-0005**: 资源池系统 — ResourcesManager 调用 consume_intel() 入口
- **ADR-0006**: Web 平台约束 — 单线程保证同步执行
- **GDD #6**: player-knowledge-intel.md — 完整的状态机、公式、边缘情况定义
- **GDD #1**: content-data-state-registry.md — intel/pattern/ability 静态定义
