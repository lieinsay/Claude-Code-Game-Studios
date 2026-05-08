# ADR-0013: 探索/搜撤系统 — ExplorationManager Autoload #11

## Status
Accepted

## Date
2026-05-08

## Summary
ExplorationManager 作为 Autoload #11，管理探索/搜撤场景的 4 阶段状态机（ARRIVING → EXPLORING → EXTRACTING → DEPARTED），消费 Navigation (#10) 在航程抵达时发出的 EncounterContext 以决定入场模式（安全抵达 vs 迫降），执行 6 个核心公式（F-11-01 搜索产出投骰、F-11-02 威胁触发判定、F-11-03 侦察预览映射、F-11-04 撤离损耗结算、F-11-05 状态变体转换、F-11-06 情报点产出），管理搜索点/情报点/威胁点/撤离锚点的交互生命周期，并在撤离成功后结算资源、情报和船体后果。探索点模板（MVP: 云观站废墟）由 Registry (#1) 定义——50×35 单位，4 区域辐条式，6 搜索点 + 2 情报点 + 2+ 威胁点 + 1 撤离锚点。探索状态以 Dictionary[StringName, Variant] 存储，通过 ADR-0003 Canonical JSON 快照包持久化为 `progress.exploration`。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Feature — Game Logic + Scene Integration |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构、状态机、信号，无引擎特定 API 依赖。2D 俯视场景使用 Godot 内置 2D 节点系统 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/exploration-scavenge-scenario.md`, `docs/architecture/architecture.md`, `design/ux/exploration.md`, `design/gdd/navigation-route-risk.md`, `design/gdd/combat-threat-handling.md`, `design/gdd/resources-goods-capacity.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | 4 阶段状态机全部有效转换 + 全部无效转换拒绝；F-11-01 search_yield 区域权重正确性；F-11-02 threat_trigger 环境必触发 + 守卫概率触发；F-11-03 scout_preview_level 3 档映射；F-11-04 extraction_loss λ_success=0.08 与 λ_forced=0.25 正确损耗 + Unique 物品保护；F-11-05 state_variant_transition 全部 8 种转换；EncounterContext fallback 降级；EC-11-01/02 会话中断恢复；EC-11-04/05 容量取舍 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #11 启动顺序, Phase 5 feature_ready)；ADR-0002 (Signal 通信协议)；ADR-0003 (快照包持久化 — progress.exploration)；ADR-0004 (InteractionHandler @abstract — 搜索点/情报点/威胁点/撤离锚点的交互基类)；ADR-0005 (ResourcesManager — Pool 5 读写、extract_carried_to_storage、add_loot、discard)；ADR-0007 (IntelManager — has_relevant_intel 搜索点描述增强门控、intel 揭示)；ADR-0009 (Modules/Hull — η_scout 侦察效率、apply_hull_damage、apply_module_damage)；ADR-0010 (EncounterContext 类型契约 — 消费 voyage_completed 信号) |
| **Enables** | ADR-0014 (Settlement — 消费 exploration_completed 或货币产出来源)；ADR-0015 (Partner — 探索中的伙伴陪同叙事)；ADR-0016 (Feedback — 探索视觉/音频反馈触发) |
| **Blocks** | Combat (#12) 守卫威胁触发 — 依赖 Exploration 传递 threat_context；Settlement (#14) 货币经济 — MVP 中云海币的唯一获取途径是探索搜刮；Intel (#6) 情报获取 — 探索中情报点产出写入 IntelManager |
| **Ordering Note** | 应在 ADR-0010 (EncounterContext) 和 ADR-0005 (ResourcesManager) 之后 Accepted — 核心交互依赖 voyage_completed 信号消费和 Pool 5 操作。应在 ADR-0012 (UI/Input Routing) 之前 Accepted — 探索场景的 HUD 覆盖层需要 ExplorationManager 的信号接口 |

## Context

### Problem Statement

《云海织航》的核心循环第三步——"探索/搜撤"——是玩家离开飞艇、踏上未知地面的时刻。GDD #11 定义了一个 4 阶段探索会话（ARRIVING → EXPLORING → EXTRACTING → DEPARTED）、6 个核心公式（搜索产出、威胁触发、侦察预览、撤离损耗、状态变体转换、情报产出）、21 个边缘情况和 6 个系统集成点。但 Exploration 的 Autoload 定位、状态存储结构、信号契约、与 ResourcesManager (#5) 的容量协作、与 CombatManager (#12) 的威胁传递协议、以及与 Persistence (#3) 的快照粒度未在 ADR 中形式化。没有这个 ADR，探索流程的核心 invariants——自由搜索保证（空结果不消耗搜索次数）、撤离损耗的 Unique 物品保护、威胁触发的环境优先/守卫惰性降级、以及探索点状态变体的持久生命周期——会在实现中被分散到多个系统，失去单一权威来源。

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑 + 2D 俯视场景。探索点场景由 Godot 2D 节点系统渲染，ExplorationManager 管理逻辑状态，不拥有场景节点引用（通过信号与场景层通信）
- **ADR-0002 信号协议**: typed params, sync emit, max depth 2, emit-after-mutation
- **ADR-0003 持久化**: `progress.exploration` snapshot package — 探索会话状态必须在浏览器标签页关闭后可恢复
- **ADR-0005 ResourcesManager**: Pool 5 (carried, 5 格) 读写；extract_carried_to_storage() 原子批量转移
- **ADR-0007 IntelManager**: has_relevant_intel(sp_id) 搜索点描述增强门控
- **ADR-0009 Modules/Hull**: η_scout 侦察效率、apply_hull_damage、apply_module_damage
- **ADR-0010 EncounterContext**: voyage_completed 信号消费、fallback context 降级
- **ADR-0001 启动顺序**: ExplorationManager 在 Phase 5 (feature_ready) 初始化
- **Web 单线程**: 探索结算的多系统写入（#5 转移 → #6 情报 → #8 船体汇总 → #3 存档）在同一帧内顺序执行
- **MVP 边界**: 1 个探索点模板（云观站废墟），50×35 单位，4 区域辐条式；1 种威胁类型（guard）传递至 #12；2 种环境威胁（塌方、不稳定地板）自行处理

### Requirements

- 4 阶段状态机: ARRIVING → EXPLORING → EXTRACTING → DEPARTED，含子状态（idle, moving, searching, threatened, extracting）
- ARRIVING 由 EncounterContext 决定入场模式: voyage_result=arrived → 正常入场；voyage_result=forced_landing → 坠机点入场 + 船体损伤脉冲
- EXPLORING: 玩家自由移动、搜索、交互，无时间限制
- EXTRACTING: 撤离读条 2.5s，可被威胁打断
- DEPARTED: 结算阶段——资源转移、情报写入、船体后果汇总、探索点状态变体更新
- 6 个公式: F-11-01 search_yield, F-11-02 threat_trigger, F-11-03 scout_preview_level, F-11-04 extraction_loss_settlement, F-11-05 state_variant_transition, F-11-06 intel_yield
- 自由搜索保证: 空结果不消耗搜索次数（search_consumed=false）
- 容量约束: Pool 5 满时弹出取舍界面（EC-11-04/05）
- 威胁惰性降级: 守卫威胁在 #12 不可用时保持 inert（EC-11-12）
- 探索点状态变体生命周期: unlooted / looted / danger-changed 持久化

## Decision

### 1. ExplorationManager 作为 Autoload #11

ExplorationManager 在 Phase 5 (feature_ready) 中初始化。`_ready()` 仅执行信号声明和常量定义；实际状态初始化在收到 `feature_ready` 信号后执行。ExplorationManager 管理探索的**逻辑状态**——不直接拥有 2D 场景节点引用，而是通过信号与场景层（由 #2 Platform/Session Shell 管理）通信。

```
Autoload 顺序 (Phase 5):
  #10 Navigation          ──┐
  #11 Exploration         ──┤
  #12 Combat              ──┤ 并行接收 feature_ready
  #13 WorldRepair         ──┤
  #14 Settlement          ──┤
  #15 Partner             ──┘
```

**逻辑/场景分离**: ExplorationManager 拥有所有探索逻辑状态（阶段、子状态、搜索点枯竭、威胁活跃、Pool 5 快照），场景层（`ExplorationScene.tscn`）负责 2D 渲染、玩家精灵移动、动画播放。场景层监听 ExplorationManager 信号更新视觉，ExplorationManager 通过方法调用接收场景层的交互触发（玩家到达搜索点、按 E 等）。

### 2. Dictionary 后端存储

```gdscript
# === ExplorationManager 状态结构 ===

# 探索会话状态
var session_phase: int = PHASE_IDLE           # 当前 4 阶段
var session_substate: int = SUBSTATE_IDLE      # EXPLORING 内的子状态
var current_exploration_point_id: StringName = &""  # 当前探索点 ID
var encounter_context: Dictionary = {}         # 从 #10 消费的 EncounterContext

# 探索点持久状态: StringName → ExplorationPointState
# ExplorationPointState = {
#   state_variant: int,     # 0=UNLOOTED, 1=LOOTED, 2=DANGER_CHANGED
#   search_points: Dictionary,  # Dict[StringName, bool] — true = 已搜索
#   intel_points: Dictionary,   # Dict[StringName, bool] — true = 已交互
#   threat_points: Dictionary,  # Dict[StringName, bool] — true = 已清除
#   env_threat_active: bool,
# }
var exploration_points: Dictionary = {}  # Dictionary[StringName, Dictionary]

# 当前会话瞬时状态（不持久化——从快照恢复时重建）
var session_search_consumed: Dictionary = {}   # 本次会话搜索点消耗状态
var session_intel_interacted: Dictionary = {}   # 本次会话情报点交互状态
var session_retreat_flagged: bool = false       # 本次会话是否有 retreat 战斗结果
var session_threats_active: Dictionary = {}     # 本次会话威胁活跃状态
```

**常量定义：**

```gdscript
# 探索阶段枚举
const PHASE_IDLE: int = 0
const PHASE_ARRIVING: int = 1
const PHASE_EXPLORING: int = 2
const PHASE_EXTRACTING: int = 3
const PHASE_DEPARTED: int = 4

# EXPLORING 子状态枚举
const SUBSTATE_IDLE: int = 0
const SUBSTATE_MOVING: int = 1
const SUBSTATE_SEARCHING: int = 2
const SUBSTATE_THREATENED: int = 3
const SUBSTATE_EXTRACTING_SUB: int = 4

# 探索点状态变体枚举
const STATE_UNLOOTED: int = 0
const STATE_LOOTED: int = 1
const STATE_DANGER_CHANGED: int = 2

# 侦察预览等级枚举
const PREVIEW_NONE: int = 0
const PREVIEW_PRESENCE: int = 1
const PREVIEW_FULL: int = 2

# 威胁类别枚举
const THREAT_ENVIRONMENTAL: StringName = &"environmental"
const THREAT_GUARD: StringName = &"guard"

# 品质档位枚举
const TIER_POOR: StringName = &"poor"
const TIER_COMMON: StringName = &"common"
const TIER_UNCOMMON: StringName = &"uncommon"

# 提取原因枚举
const EXTRACTION_PLAYER: StringName = &"player_initiated"
const EXTRACTION_FORCED: StringName = &"pool_depleted"
const EXTRACTION_RETREAT: StringName = &"retreat"
```

### 3. 信号接口

```gdscript
# === 探索阶段变更 ===
# 遵循 ADR-0002: typed params, sync emit, emit-after-mutation
signal exploration_phase_changed(old_phase: int, new_phase: int, point_id: StringName)

# === 搜索相关 ===
signal search_performed(point_id: StringName, items_found: Array, is_empty: bool)
signal item_picked_up(item_id: StringName, quantity: int)

# === 情报相关 ===
signal intel_discovered(intel_id: StringName)

# === 威胁相关 ===
signal threat_triggered(threat_id: StringName, threat_category: StringName)
signal threat_cleared(threat_id: StringName)

# === 提取相关 ===
signal extraction_started(reason: StringName)
signal extraction_progress_changed(progress: float)  # 0.0–1.0
signal extraction_interrupted(reason: StringName)
signal extraction_completed(items_count: int, intel_count: int)

# === 容量警告 ===
signal capacity_warning(occupied: int, capacity: int)
```

**信号发射顺序**: 阶段信号在状态机转换时最先发射。EXTRACTING 阶段中: `extraction_started` → `extraction_progress_changed` (多次) → `extraction_completed` (或 `extraction_interrupted`)。`threat_triggered` 在威胁判定后发射——由场景层消费以播放触发动画。

### 4. 方法接口

#### 4a. 探索会话生命周期

```gdscript
# 进入探索点 (由 Platform #2 或 Navigation #10 调用)
func enter_exploration(ctx: Dictionary) -> void:
    # 1. _validate_encounter_context(ctx) — 校验或构建 fallback
    # 2. 存储 encounter_context
    # 3. 根据 voyage_result 决定入场模式:
    #    arrived → _enter_arriving_normal()
    #    forced_landing → _enter_arriving_forced_landing()
    #    retreated → _enter_arriving_retreated()
    # 4. Phase → ARRIVING
    # 5. 加载探索点持久状态 (从 exploration_points 字典)
    # 6. 初始化会话瞬时状态
    # 7. 快照 η_scout (进入时一次性)
    # 8. exploration_phase_changed.emit(IDLE, ARRIVING, point_id)

# 跳过 ARRIVING (玩家按任意键)
func skip_arriving() -> void:
    # Phase ARRIVING → EXPLORING
    # exploration_phase_changed.emit(ARRIVING, EXPLORING, point_id)

# 触发提取 (玩家在撤离锚点按 E)
func trigger_extraction() -> void:
    # 1. Phase EXPLORING → EXTRACTING
    # 2. extraction_started.emit(EXTRACTION_PLAYER)
    # 3. 开始读条计时器 (2.5s)
    # 4. 读条期间: 检查威胁打断 (EC-11-11)
    # 5. 读条完成 → _finalize_extraction()
    # 6. 读条被打断 → _interrupt_extraction()

# 强制提取 (Pool 5 耗尽 或 所有搜索点已搜)
func force_extraction(reason: StringName) -> void:
    # 与 trigger_extraction 相同但 reason 为 EXTRACTION_FORCED
```

#### 4b. 搜索与情报

```gdscript
# 执行搜索 (场景层玩家按 E 在搜索点)
func perform_search(sp_id: StringName) -> Dictionary:
    # 返回 {items: Array, is_empty: bool, search_consumed: bool, message: String}
    # 1. 检查 state_variant == LOOTED → 返回 "已搜过"
    # 2. 调用 F-11-01 search_yield(sp_id, state_variant, zone)
    # 3. 若 is_empty → search_consumed=false, 发射 search_performed
    # 4. 若非空 → 检查 Pool 5 容量
    #    a. 若可容纳 → add_loot(), search_consumed=true
    #    b. 若满 → 发射 capacity_warning, 返回物品但不消耗搜索次数
    #              等待玩家取舍决策后调用 confirm_search_pickup()
    # 5. 更新会话搜索状态
    # 6. 发射 search_performed + item_picked_up (每种物品)
    # 7. 触发持久化快照 (EC-11-01)

# 确认搜索拾取 (容量取舍后)
func confirm_search_pickup(sp_id: StringName, accepted_items: Array, discarded_items: Array) -> void:
    # 处理玩家在取舍界面的选择
    # 丢弃 discarded_items → discard()
    # 接受 accepted_items → add_loot()

# 执行情报交互
func perform_intel_interaction(intel_point_id: StringName) -> Dictionary:
    # 返回 {intel_id: StringName, is_empty: bool}
    # 1. 检查是否已交互 → 返回空
    # 2. 调用 F-11-06 intel_yield(intel_point_id)
    # 3. 检查 Pool 5 容量 (Unique 物品占用 1 格)
    # 4. 若满 → 触发 EC-11-05 取舍（附加 Unique 警告）
    # 5. 写入 IntelManager.reveal_tag() — 情报揭示
    # 6. 标记已交互
    # 7. 发射 intel_discovered
```

#### 4c. 威胁管理

```gdscript
# 检查威胁触发 (场景层每帧或玩家移动时调用)
func check_threat_trigger(player_pos: Vector2, trigger_type: StringName) -> Dictionary:
    # 返回 {triggered: bool, threat_id: StringName, context: Dictionary}
    # 调用 F-11-02 threat_trigger 对所有活跃威胁
    # 若多个威胁同时触发 → 按优先级依次处理 (EC-11-10):
    #   1. 环境威胁 > 守卫威胁
    #   2. 同类型距离近者优先

# 处理环境威胁 (内部)
func _handle_environmental_threat(threat_point: Dictionary) -> void:
    # 1. 施加船体损伤 → ModulesManager.apply_hull_damage()
    # 2. 或封锁路径 → 更新探索点布局状态
    # 3. 发射 threat_triggered
    # 4. env_threat_active = true

# 处理守卫威胁 (内部)
func _handle_guard_threat(threat_point: Dictionary) -> void:
    # 1. 构建 threat_context (F-11-02 build_threat_context)
    # 2. 若 CombatManager 可用 → CombatManager.initiate_threat(threat_context)
    # 3. 若 CombatManager 不可用 → 守卫 inert (EC-11-12)
    # 4. 发射 threat_triggered

# 接收战斗结果 (由 CombatManager #12 回调)
func on_combat_result(result: Dictionary) -> void:
    # result = {outcome, hull_damage, module_damage, resources_consumed, knockback, retreat_flagged}
    # 1. 若 retreat_flagged=true → session_retreat_flagged = true
    # 2. 若 outcome=suppressed → 标记威胁点 is_active=false
    # 3. 应用 knockback → 通知场景层移动玩家
    # 4. 恢复子状态: threatened → idle
```

#### 4d. 提取结算

```gdscript
# 完成提取 (读条结束)
func _finalize_extraction() -> void:
    # 1. Phase EXTRACTING → DEPARTED
    # 2. 调用 F-11-04 extraction_loss_settlement(carried_stacks, session_retreat_flagged)
    #     → 批量原子转移至飞艇仓库 (extract_carried_to_storage)
    # 3. 情报结算: 写入 IntelManager
    # 4. 船体后果汇总展示 (已在触发时即时写入 #8, 此处仅汇总)
    # 5. 调用 F-11-05 state_variant_transition → 更新探索点持久状态
    # 6. 发射 extraction_completed
    # 7. 持久化: Persistence.capture_snapshot("progress.exploration", snapshot)
    # 8. 通知 Platform #2 过渡回 Hub
```

#### 4e. 查询接口

```gdscript
# 查询当前阶段
func get_session_phase() -> int

# 查询探索点状态变体
func get_exploration_point_state(point_id: StringName) -> int

# 查询侦察预览等级
func get_scout_preview_level() -> int:
    # 快照值——在 enter_exploration 时计算

# 查询搜索点是否已消耗
func is_search_point_consumed(sp_id: StringName) -> bool

# 查询搜索点描述 (带增强门控)
func get_search_point_description(sp_id: StringName) -> String:
    # 若 IntelManager.has_relevant_intel(sp_id) → 返回 description_enhanced
    # 否则 → 返回 description
    # 按当前 state_variant 选择对应的文字对
```

### 5. 核心算法

#### 5a. 4 阶段状态机

```
IDLE ──[enter_exploration(ctx)]──→ ARRIVING
ARRIVING ──[skip_arriving() / 自动超时]──→ EXPLORING
EXPLORING ──[trigger_extraction() / force_extraction()]──→ EXTRACTING
EXTRACTING ──[读条完成]──→ DEPARTED
EXTRACTING ──[威胁打断]──→ EXPLORING (threatened 子状态)
DEPARTED ──[结算完成]──→ IDLE
```

**无效转换（状态机拒绝）：**
- `IDLE → EXPLORING`: 拒绝 — 必须先经过 ARRIVING
- `IDLE → EXTRACTING`: 拒绝 — 必须进入探索点
- `ARRIVING → EXTRACTING`: 拒绝 — 必须先进入 EXPLORING
- `EXPLORING → DEPARTED`: 拒绝 — 必须经过 EXTRACTING 读条
- `DEPARTED → *` (除 IDLE): 拒绝 — 结算完成后只能回到 IDLE
- 在非 ARRIVING 阶段调用 `skip_arriving()`: 无操作
- 在非 EXPLORING 阶段调用 `trigger_extraction()`: 无操作

#### 5b. F-11-01 搜索产出投骰

```gdscript
func search_yield(sp_id: StringName, state: int, zone: StringName) -> Dictionary:
    if state == STATE_LOOTED:
        return {items: [], is_empty: true, search_consumed: false,
                message: "这里已经被搜过了"}

    var empty_chance := EMPTY_CHANCE_TABLE[state][zone]
    if randf() < empty_chance:
        return {items: [], is_empty: true, search_consumed: false}

    var quality_weights := QUALITY_WEIGHTS_TABLE[state][zone]
    var tier := _weighted_random_tier(quality_weights)
    var pool := _loot_pool_for(sp_id, tier)

    if pool.size() == 0:
        return {items: [], is_empty: true, search_consumed: false,
                message: "这里似乎还能找到些什么，但已经什么都没有了——或许下次再来？"}

    var draw_count := _random_int(DRAW_COUNT_TABLE[tier].min, DRAW_COUNT_TABLE[tier].max)
    var selected := _sample_without_replacement(pool, min(draw_count, pool.size()))

    return {items: selected, is_empty: false, search_consumed: true}
```

**数据表（由 Registry #1 提供）：**

| 区域 | empty_chance (unlooted) | Poor | Common | Uncommon |
|------|------------------------|------|--------|----------|
| A_core | 0.00 | 0.20 | 0.45 | 0.35 |
| B_inner | 0.05 | 0.25 | 0.45 | 0.30 |
| C_mid | 0.20 | 0.35 | 0.40 | 0.25 |
| D_outer | 0.35 | 0.50 | 0.30 | 0.20 |

danger-changed 修正: 所有区域 empty_chance +0.15，Uncommon 权重 ×0.5（差额加给 Poor）。

#### 5c. F-11-02 威胁触发判定

```gdscript
func threat_trigger(threat_point: Dictionary, trigger_type: StringName, player_pos: Vector2) -> Dictionary:
    if not threat_point.is_active:
        return {triggered: false}

    if trigger_type == "interaction":
        return {triggered: true,
                context: _build_threat_context(threat_point, "interaction")}

    if trigger_type == "proximity":
        var dist := player_pos.distance_to(threat_point.position)
        if dist > threat_point.trigger_radius:
            return {triggered: false}
        var prob := TRIGGER_PROB_TABLE[threat_point.threat_category]
        if randf() < prob:
            return {triggered: true,
                    context: _build_threat_context(threat_point, "proximity")}

    return {triggered: false}
```

- 环境威胁 trigger_prob = 1.0（必触发）— 由 Exploration 自行处理
- 守卫威胁 trigger_prob = 0.70（概率触发）— 传递至 CombatManager #12

#### 5d. F-11-03 侦察预览映射

```gdscript
func scout_preview_level(eta_scout: float) -> int:
    if eta_scout <= 0.0:
        return PREVIEW_NONE       # 无预览
    elif eta_scout >= 1.0:
        return PREVIEW_FULL       # 完整预览: 类型+位置
    else:
        return PREVIEW_PRESENCE   # 存在预览: 红色感叹号
```

η_scout 在 enter_exploration() 时一次性快照，探索过程中不变。

#### 5e. F-11-04 撤离损耗结算

```gdscript
func extraction_loss_settlement(carried_stacks: Array, retreat_flagged: bool) -> Dictionary:
    var transfer_batch := []
    var result := {transferred: [], lost: [], total_lost_qty: 0}

    for stack in carried_stacks:
        if stack.is_unique and stack.max_stack == 1:
            transfer_batch.append({resource_id: stack.resource_id, quantity: stack.quantity})
            result.transferred.append({id: stack.resource_id, qty: stack.quantity, lost: 0})
            continue

        var lambda := LAMBDA_FORCED if retreat_flagged else LAMBDA_SUCCESS
        var loss_qty := _compute_loss(stack.quantity, lambda)
        var retained_qty := stack.quantity - loss_qty
        transfer_batch.append({resource_id: stack.resource_id, quantity: retained_qty})

        if loss_qty > 0:
            result.lost.append({id: stack.resource_id, qty: loss_qty})
            result.total_lost_qty += loss_qty

    ResourcesManager.extract_carried_to_storage(transfer_batch)
    return result

func _compute_loss(qty: int, lambda: float) -> int:
    if qty <= 1: return 0
    if lambda <= 0.0: return 0
    return mini(qty - 1, maxi(0, ceili(float(qty) * lambda)))
```

MVP 默认: λ_success = 0.08, λ_forced = 0.25。Unique 物品 (Q=1, max_stack=1) 永不损耗。

#### 5f. F-11-05 状态变体转换

```gdscript
func state_variant_transition(current_state: int, all_searched: bool, env_threat_active: bool) -> int:
    # 转换表:
    # unlooted + !all_searched + !env_threat → unlooted
    # unlooted + all_searched + !env_threat → looted
    # unlooted + * + env_threat → danger-changed
    # looted + * + env_threat → danger-changed
    # looted + * + !env_threat → looted
    # danger-changed + !all_searched + !env_threat → unlooted
    # danger-changed + all_searched + !env_threat → looted
    # danger-changed + * + env_threat → danger-changed
```

env_threat_active=true 优先——环境威胁改变探索点结构。守卫威胁不影响持久状态。

#### 5g. F-11-06 情报点产出

```gdscript
func intel_yield(intel_point_id: StringName) -> Dictionary:
    var intel_id := INTEL_POINT_CONFIG[intel_point_id].intel_id
    return {items: [{resource_id: intel_id, quantity: 1}], is_empty: false}
```

情报点固定产出 1 个 Q=1 Unique 情报物品，不参与搜索投骰。每个会话每情报点仅可交互一次。

### 6. 威胁优先级排序（EC-11-10 多威胁同时触发）

```gdscript
func _sort_threats_by_priority(threats: Array, player_pos: Vector2) -> Array:
    # 排序规则:
    #   1. 环境威胁 > 守卫威胁
    #   2. 同类型中距离近者优先
    #   3. 同距离按 threat_id 字典序
    return threats.sorted_custom(func(a, b):
        if a.threat_category != b.threat_category:
            return a.threat_category == THREAT_ENVIRONMENTAL  # 环境优先
        var dist_a := player_pos.distance_to(a.position)
        var dist_b := player_pos.distance_to(b.position)
        if abs(dist_a - dist_b) < 0.01:
            return a.id < b.id  # 字典序
        return dist_a < dist_b
    )
```

### 7. ADR-0003 序列化

```gdscript
# 在 feature_ready 阶段注册
func _on_feature_ready() -> void:
    Persistence.register_domain_serializer("exploration", _serialize_exploration)

func _serialize_exploration() -> Dictionary:
    var serialized_points := {}
    for point_id in exploration_points:
        var pt := exploration_points[point_id]
        serialized_points[point_id] = {
            "state_variant": pt.state_variant,
            "search_points": pt.search_points.duplicate(true),
            "intel_points": pt.intel_points.duplicate(true),
            "threat_points": pt.threat_points.duplicate(true),
            "env_threat_active": pt.env_threat_active,
        }
    # 若当前有活跃会话，也序列化会话快照
    var session_snapshot := {}
    if session_phase == PHASE_EXPLORING or session_phase == PHASE_EXTRACTING:
        session_snapshot = {
            "phase": session_phase,
            "point_id": current_exploration_point_id,
            "search_consumed": session_search_consumed.duplicate(true),
            "intel_interacted": session_intel_interacted.duplicate(true),
            "threats_active": session_threats_active.duplicate(true),
            "retreat_flagged": session_retreat_flagged,
            # Pool 5 快照由 ResourcesManager 的 progress.resources 独立管理
        }
    return {
        "domain_id": "exploration",
        "points": serialized_points,
        "active_session": session_snapshot,
    }

func _deserialize_exploration(snapshot: Dictionary) -> void:
    for point_id in snapshot.points:
        var data := snapshot.points[point_id]
        exploration_points[point_id] = {
            "state_variant": data.state_variant,
            "search_points": data.search_points,
            "intel_points": data.intel_points,
            "threat_points": data.threat_points,
            "env_threat_active": data.env_threat_active,
        }
    # 活跃会话恢复由 Platform #2 在场景加载后调用 _restore_active_session()
```

**快照时机**: (1) 每次搜索完成后 (2) 威胁结算完成后 (3) 进入 EXTRACTING 时 (4) DEPARTED 结算完成时。快照粒度为每次有意义的探索进度变更。

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    ExplorationManager (Autoload #11)                        │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │              STATE STORAGE (Dictionary)                        │       │
│  │                                                                │       │
│  │  exploration_points: Dict[StringName, ExplorationPointState]  │       │
│  │    exploration_point.cloudwatch-ruins: {                       │       │
│  │      state_variant: UNLOOTED | LOOTED | DANGER_CHANGED        │       │
│  │      search_points: {sp_id: consumed_bool}                    │       │
│  │      intel_points: {ip_id: interacted_bool}                   │       │
│  │      threat_points: {tp_id: active_bool}                      │       │
│  │      env_threat_active: bool                                  │       │
│  │    }                                                           │       │
│  │                                                                │       │
│  │  Session (transient): phase, substate, encounter_context,      │       │
│  │    η_scout_snapshot, retreat_flagged                           │       │
│  └──────────────────────────────────────────────────────────────┘       │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          UPSTREAM (consumes)                                       │   │
│  │                                                                    │   │
│  │  Navigation(#10) ──→ voyage_completed(ctx) → EncounterContext     │   │
│  │  Registry (#1)   ──→ 探索点模板定义 (zones, loot_pools, 威胁配置) │   │
│  │  Resources (#5)  ──→ Pool 5 读写, extract_carried_to_storage      │   │
│  │  Intel (#6)      ──→ has_relevant_intel(sp_id) — 描述增强门控     │   │
│  │  Modules (#8)    ──→ η_scout, apply_hull_damage, apply_module_damage│
│  │  Combat (#12)    ──→ initiate_threat() → combat_result 回调       │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          DOWNSTREAM (provides)                                      │   │
│  │                                                                    │   │
│  │  Resources (#5) ←── add_loot, extract_carried_to_storage           │   │
│  │  Intel (#6)     ←── intel 揭示 → 航线知识推进                      │   │
│  │  Modules (#8)   ←── apply_hull_damage, apply_module_damage         │   │
│  │  Combat (#12)   ←── threat_context (守卫威胁触发时)                │   │
│  │  Persistence(#3)←── progress.exploration snapshot                  │   │
│  │  UI (#16)       ←── exploration_phase_changed, search_performed,   │   │
│  │                     capacity_warning, extraction_* signals         │   │
│  │  Feedback (#17) ←── threat_triggered, item_picked_up,              │   │
│  │                     intel_discovered, extraction_completed         │   │
│  │  Settlement(#14)←── 探索产出的货币为集市交易提供资金来源             │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          SIGNALS (10 typed, emit-after-mutation)                   │   │
│  │                                                                    │   │
│  │  exploration_phase_changed(old_phase: int, new_phase: int,        │   │
│  │                            point_id: StringName)                   │   │
│  │  search_performed(point_id: StringName, items_found: Array,       │   │
│  │                   is_empty: bool)                                  │   │
│  │  item_picked_up(item_id: StringName, quantity: int)               │   │
│  │  intel_discovered(intel_id: StringName)                           │   │
│  │  threat_triggered(threat_id: StringName, threat_category: StringName)│
│  │  threat_cleared(threat_id: StringName)                             │   │
│  │  extraction_started(reason: StringName)                            │   │
│  │  extraction_progress_changed(progress: float)                      │   │
│  │  extraction_interrupted(reason: StringName)                        │   │
│  │  extraction_completed(items_count: int, intel_count: int)          │   │
│  │  capacity_warning(occupied: int, capacity: int)                   │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │          4-PHASE STATE MACHINE                                  │       │
│  │                                                                │       │
│  │  IDLE ──→ ARRIVING ──→ EXPLORING ──→ EXTRACTING ──→ DEPARTED │       │
│  │                      ↑                  ↑       ↓              │       │
│  │                      │                  └─── 打断 ──┘          │       │
│  │                      └── 未被打断的提取完成 ──→ IDLE           │       │
│  │                                                                │       │
│  │  EXPLORING sub-states: idle ↔ moving → searching → idle        │       │
│  │                        threatened → idle                       │       │
│  └──────────────────────────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

#### 探索点模板定义 (Registry #1)

```gdscript
# Registry 中 kind=exploration_point_template 的实体结构
# ExplorationManager 在 enter_exploration 时通过 query_entity 读取
# MVP 定义:
# {
#   "point_id": &"exploration_point.cloudwatch-ruins",
#   "name": "云观站废墟",
#   "size": {"width": 50, "height": 35},
#   "topology": "radial-4-zone",  # 4 区域辐条式
#   "zones": {
#     "A_core": {"search_points": 1, "intel_points": 1, "threat_points": 0},
#     "B_inner": {"search_points": 1, "intel_points": 1, "threat_points": 1},
#     "C_mid": {"search_points": 2, "intel_points": 0, "threat_points": 1},
#     "D_outer": {"search_points": 2, "intel_points": 0, "threat_points": 1}
#   },
#   "extraction_anchor": {"zone": "D_outer", "position": [25, 30]},
#   "arrival_entry": {"zone": "D_outer", "position": [25, 33]},
#   "forced_landing_entry": {"zone": "C_mid", "position": [18, 15]}
# }
```

#### EncounterContext 消费 (ADR-0010 合同)

```gdscript
# Exploration 消费 Navigation 的 voyage_completed 信号
func _on_voyage_completed(ctx: Dictionary) -> void:
    var validated := _validate_encounter_context(ctx)
    enter_exploration(validated)

func _validate_encounter_context(ctx: Dictionary) -> Dictionary:
    if ctx == null or not ctx is Dictionary:
        return _build_fallback_context()
    if not ctx.get("route_id") or ctx.route_id == &"":
        return _build_fallback_context()
    if not ctx.get("destination_id") or ctx.destination_id == &"":
        return _build_fallback_context()
    var result := ctx.get("voyage_result", &"")
    if result not in [&"arrived", &"retreated", &"forced_landing"]:
        return _build_fallback_context()
    if not ctx.get("resolved_encounters") is Array:
        return _build_fallback_context()
    return ctx

func _build_fallback_context() -> Dictionary:
    return {
        "route_id": &"unknown",
        "destination_id": &"cloudwatch-ruins-fallback",
        "voyage_result": &"arrived",
        "resolved_encounters": [],
        "accumulated_damage": 0,
        "revealed_hidden_tags": [],
        "hull_band_arrival": &"intact",
        "forced_landing_position": &"",
        "damaged_slots": [],
    }
```

#### CombatManager 战斗结果回调合同

```gdscript
# CombatManager (#12) → ExplorationManager (#11) 回调
# combat_result 结构:
# {
#   "outcome": StringName,         # "suppressed" | "tanked" | "retreated"
#   "hull_damage": int,            # 本次战斗造成的船体伤害
#   "module_damage": Array,        # [{slot_id: StringName, damage_amount: int}]
#   "resources_consumed": Array,   # [{resource_id: StringName, quantity: int}]
#   "knockback": {                 # 击退向量
#     "direction": Vector2,        # 击退方向 (远离威胁)
#     "distance": float            # 击退距离
#   },
#   "retreat_flagged": bool        # 玩家选择撤退 → true
# }

# Exploration 消费:
func on_combat_result(result: Dictionary) -> void:
    match result.outcome:
        "suppressed":
            # 威胁被清除
            _mark_threat_cleared(result.threat_id)
            threat_cleared.emit(result.threat_id)
        "tanked":
            # 玩家硬扛——威胁保持活跃，船体受损
            # 损伤已由 #12 通过 #8 写入
            pass
        "retreated":
            # 玩家撤退——标记 retreat_flagged
            session_retreat_flagged = true
            # 直接触发强制提取
            force_extraction(EXTRACTION_RETREAT)
```

#### 搜索点配置结构

```gdscript
# 每个搜索点在 Registry 中的定义
# {
#   "sp_id": &"search_point.cloudwatch.core-01",
#   "zone": "A_core",
#   "position": [25, 13],
#   "description": "一堆散落的电器零件，看起来像是通讯设备的残骸",
#   "description_enhanced": "一堆电器零件——其中一些铜线接头和你见过的灯塔继电器图纸吻合",
#   "loot_pool": {
#     "poor": [
#       {"resource_id": &"resource.scrap_metal", "quantity_range": [1, 3]},
#       {"resource_id": &"resource.frayed_wire", "quantity_range": [1, 2]}
#     ],
#     "common": [
#       {"resource_id": &"resource.copper_wire", "quantity_range": [2, 5]},
#       {"resource_id": &"resource.cloud_crystal_fragment", "quantity_range": [1, 3]}
#     ],
#     "uncommon": [
#       {"resource_id": &"resource.cloud_crystal", "quantity_range": [2, 4]},
#       {"resource_id": &"resource.ancient_relay_part", "quantity_range": [1, 2]}
#     ]
#   }
# }
```

### 8. 场景层合同

ExplorationManager 不拥有场景节点引用。场景层（`ExplorationScene.tscn` 或由 Platform #2 管理的场景实例）通过以下合同与 ExplorationManager 通信：

**场景层 → ExplorationManager:**
- 玩家按 E 在搜索点 → `perform_search(sp_id)`
- 玩家按 E 在情报点 → `perform_intel_interaction(ip_id)`
- 玩家按 E 在撤离锚点 → `trigger_extraction()`
- 玩家按任意键跳过 ARRIVING → `skip_arriving()`
- 每帧/移动时 → `check_threat_trigger(player_pos, "proximity")`
- 玩家对威胁点按 E → `check_threat_trigger(player_pos, "interaction")`
- 取舍界面选择 → `confirm_search_pickup(sp_id, accepted, discarded)`

**ExplorationManager → 场景层 (signals):**
- `exploration_phase_changed` → 场景层切换 UI/HUD 状态
- `search_performed` → 播放搜索动画
- `item_picked_up` → 物品获得弹窗动画
- `threat_triggered` → 威胁触发动画 + S7 界面
- `extraction_started/progress_changed/interrupted/completed` → 撤离读条 UI
- `capacity_warning` → HUD 背包闪烁

## Alternatives Considered

### Alternative A: ExplorationManager 直接拥有 2D 场景节点

- **Description**: ExplorationManager 直接管理 `ExplorationScene` 节点引用，调用 `add_child` / `queue_free` 等场景树操作
- **Pros**: 逻辑和表现紧耦合——减少信号通信开销；单点管理探索全流程
- **Cons**: 违反单一职责原则——逻辑状态和视觉表现混合；场景加载/卸载与逻辑状态机耦合，存档恢复时需先加载场景才能恢复状态；单元测试需要 mock 整个场景树
- **Rejection Reason**: ADR-0001 定义 Autoload 为纯逻辑——不应拥有场景节点引用。逻辑/场景分离允许 ExplorationManager 在无渲染上下文的自动化测试中独立验证 4 阶段状态机和 6 个公式

### Alternative B: 多个独立信号代替单一 EncounterContext 消费

- **Description**: 不使用 EncounterContext 聚合 Dictionary，而是 Navigation 发射多个独立信号给 Exploration
- **Pros**: 信号更细粒度
- **Cons**: 已在 ADR-0010 中详细讨论并拒绝——拆分信号增加 Exploration 的聚合复杂度
- **Rejection Reason**: 参见 ADR-0010 Alternative B 的完整分析。Exploration 需要完整的 EncounterContext 以决定入场模式和场景生成参数

### Alternative C: 全局计时器驱动的强制撤离

- **Description**: 借鉴 Dark and Darker 的缩圈机制，探索有时间限制——超时自动强制撤离
- **Pros**: 增加紧张感；防止玩家无限停留
- **Cons**: 与 GDD 的核心设计意图冲突——"撤离是玩家的判断，不是被迫的逃命"；Pillar 4 强调温和压力而非紧迫威胁；Web 环境的标签页后台时计时器不可靠
- **Rejection Reason**: GDD 明确拒绝全局计时器（C1: "无全局计时器"）。玩家判断力——而非时间压力——是核心体验。池容量和威胁密度提供足够的推拉张力

### Alternative D: 搜索点按区域顺序强制解锁

- **Description**: 玩家必须先搜索外圈才能进入内圈——类似地牢的逐层推进
- **Pros**: 控制探索节奏；防止玩家直奔核心区最佳 loot
- **Cons**: 增加不必要的限制——玩家应该能自由判断风险/收益；与"自由搜索"保证冲突；辐条式拓扑本身就提供自然的风险梯度（内圈威胁更密）
- **Rejection Reason**: GDD 的辐条式设计本身就是自平衡的——核心区 loot 更好但威胁更密。强制顺序破坏玩家判断力的核心体验

## Consequences

### Positive

- **单一探索权威**: ExplorationManager 是所有探索状态的唯一 owner——消除了多系统各自追踪搜索点/威胁状态的不一致风险
- **逻辑/场景分离**: Autoload 不含节点引用——单元测试可直接实例化 ExplorationManager 并验证状态机/公式，无需加载 Godot 场景
- **自由搜索保证**: 空结果不消耗搜索次数——支撑 Pillar 4 的温和压力设计，杜绝"翻到空=惩罚"的负面体验
- **威胁惰性降级**: 守卫威胁在 #12 不可用时保持 inert——MVP 分阶段实现时，#11 可先于 #12 交付，守卫威胁暂时无害
- **ADR-0010 消费标准化**: 通过 EncounterContext 单一入口决定入场模式——所有场景生成参数从已验证的 Dictionary 读取
- **数据驱动探索点**: 探索点模板、loot_pools、威胁配置均在 Registry 中定义——添加新探索点不需要修改 ExplorationManager 代码

### Negative

- **Autoload #11**: 增加了 Phase 5 启动约束——ExplorationManager 依赖 #5 (Resources)、#6 (Intel)、#8 (Modules)、#10 (Navigation 的 EncounterContext 信号)
- **信号数量多 (10 个)**: 场景层需要连接多个信号以驱动 UI 更新——但每个信号职责单一，符合 ADR-0002 fan-out 模式
- **逻辑/场景接口面积大**: 场景层需要实现的回调方法较多（perform_search、check_threat_trigger 等）——合同如果不严格执行会导致状态不一致
- **持久化粒度敏感**: 探索会话快照在每次搜索后和威胁结算后写入——如果快照频率过高（搜索点密集时），可能增加 IndexedDB 写入压力

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Pool 5 快照与 ResourcesManager 实际状态不一致 | Low — 同步操作 | Medium — 恢复时物品丢失或重复 | 会话恢复时以 ResourcesManager 的实际 Pool 5 状态为准；exploration snapshot 仅持久化探索点状态和搜索消耗记录 |
| η_scout 进入时快照与实时船体状态脱节 | Low — η_scout 在 ARRIVING 阶段快照，探索中船体可能变化 | Low — 侦察预览不反映实时船体变化 | 已知轻度 UI 不一致（EC-11-13），MVP 接受。P2 可加入重评估触发 |
| 探索点模板在 Registry 中缺失或格式错误 | Low — Registry 启动时验证 | Medium — 探索功能不可用 | enter_exploration() 时验证模板完整性——缺失关键字段则 fallback 到最小可用的默认模板 |
| 多威胁同时触发的处理顺序导致体验不一致 | Low — 每次结算一个威胁 | Low — 玩家依次经历 | 固定优先级排序（环境>守卫、距离近>远、字典序）确保确定性行为 |
| DEPARTED 结算写入事务性失败 | Low — Web 单线程 | Medium — 玩家探索收获丢失 | EC-11-03 重试策略（1s/2s/4s/8s, 最多 4 次）+ 手动重试按钮 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| exploration-scavenge-scenario.md | C1: 探索点模板 — 50×35 单位, 4 区域辐条式, 6 搜索点, 2 情报点, 2+ 威胁点, 1 撤离锚点 | 探索点模板定义 (§Key Interfaces) + Registry 驱动 |
| exploration-scavenge-scenario.md | C2: 4 阶段会话 — ARRIVING → EXPLORING → EXTRACTING → DEPARTED | 状态机定义 (§5a) + PHASE_* 枚举 |
| exploration-scavenge-scenario.md | C3: 进入探索点 — 消费 EncounterContext, arrived vs forced_landing 入场 | enter_exploration() + _validate_encounter_context() |
| exploration-scavenge-scenario.md | C4: 移动与交互 — 系统 #4 2D 俯视移动 + 交互焦点 | 场景层合同 (§8) — 不限制移动实现 |
| exploration-scavenge-scenario.md | C5: 搜索机制 — 自由搜索 + 容量约束 + 搜索点描述增强门控 | perform_search() + F-11-01 + has_relevant_intel 门控 |
| exploration-scavenge-scenario.md | C6: 侦察与威胁预览 — 3 档映射, 进入时快照 | F-11-03 scout_preview_level() + η_scout 快照 |
| exploration-scavenge-scenario.md | C7: 威胁触发 — 环境自行处理, 守卫传递 #12 | F-11-02 threat_trigger() + _handle_environmental_threat() + _handle_guard_threat() |
| exploration-scavenge-scenario.md | C8: 撤离机制 — 2.5s 读条, 可被打断 | trigger_extraction() + _interrupt_extraction() |
| exploration-scavenge-scenario.md | C9: DEPARTED 结算 — 资源转移 + 情报写入 + 船体汇总 | _finalize_extraction() + F-11-04 |
| exploration-scavenge-scenario.md | C10: 状态变体生命周期 — unlooted / looted / danger-changed | F-11-05 state_variant_transition() + EXPLORATION_STATE_* 枚举 |
| exploration-scavenge-scenario.md | F-11-01 搜索产出投骰 — 6 个公式之一 | search_yield() 实现 + 数据表 |
| exploration-scavenge-scenario.md | F-11-02 威胁触发判定 — 6 个公式之二 | threat_trigger() 实现 + TRIGGER_PROB_TABLE |
| exploration-scavenge-scenario.md | F-11-03 侦察预览映射 — 6 个公式之三 | scout_preview_level() 分段阈值 |
| exploration-scavenge-scenario.md | F-11-04 撤离损耗结算 — 6 个公式之四 | extraction_loss_settlement() + compute_loss() |
| exploration-scavenge-scenario.md | F-11-05 状态变体转换 — 6 个公式之五 | state_variant_transition() + 8 种转换规则 |
| exploration-scavenge-scenario.md | F-11-06 情报点产出 — 6 个公式之六 | intel_yield() 固定产出 |
| exploration-scavenge-scenario.md | EC-11-01/02: 会话中断与恢复 | 持久化快照策略 + _restore_active_session() |
| exploration-scavenge-scenario.md | EC-11-03: DEPARTED 结算写入失败 | 事务模式 + 重试 1s/2s/4s/8s |
| exploration-scavenge-scenario.md | EC-11-04/05/06: 容量边界 | capacity_warning 信号 + confirm_search_pickup() |
| exploration-scavenge-scenario.md | EC-11-07: EncounterContext 缺失/格式错误 | _validate_encounter_context() + fallback context |
| exploration-scavenge-scenario.md | EC-11-08: 探索中 hull=0 | HUD 警告 — 不自行终止探索 |
| exploration-scavenge-scenario.md | EC-11-10: 多威胁同时触发 | _sort_threats_by_priority() 确定性排序 |
| exploration-scavenge-scenario.md | EC-11-11: 撤离读条期间威胁触发 | _interrupt_extraction() → EXPLORING + threatened |
| exploration-scavenge-scenario.md | EC-11-12: #12 不可用时守卫 inert | _handle_guard_threat() 惰性降级 |
| exploration-scavenge-scenario.md | EC-11-14–17: 探索点状态边缘 | state_variant_transition 覆盖全部 8 种转换 |
| exploration-scavenge-scenario.md | EC-11-18: 威胁附近未触发撤离 | 正常撤离 — 无额外惩罚 |
| exploration-scavenge-scenario.md | EC-11-20: 页面失去焦点/闲置 | 无全局计时器 — 无惩罚；读条中断并重置 |
| exploration-scavenge-scenario.md | EC-11-21: localStorage 配额满 | HUD 非阻塞警告 + 30s 防抖 |
| exploration-scavenge-scenario.md | AC-11-01–26: 全部 26 个验收条件 | 通过方法接口和信号合同覆盖 26 个 AC |

## Performance Implications

- **CPU**: 所有操作 O(N) 其中 N 为搜索点或威胁点数量 (MVP: 6+3=9)。search_yield: 加权随机 + 不放回抽取 — < 0.1ms。threat_trigger: 距离计算 + 概率判定 — < 0.05ms 每威胁。extraction_loss_settlement: 遍历 Pool 5 最多 5 堆 — < 0.01ms。state_variant_transition: O(1) 查表。全部 6 个公式在单帧内完成无压力。
- **Memory**: MVP 1 个探索点 × ~2KB。探索会话瞬时状态 ~500 bytes。总计 < 3KB。探索点模板静态定义由 Registry 持有，不在 ExplorationManager 中复制。
- **Load Time**: 启动时从 Persistence snapshot 恢复 — 反序列化 < 0.5ms。进入探索点时从 Registry 读取模板 — O(1) query_entity < 0.1ms。
- **Network**: N/A — 单机游戏。
- **Snapshot Write**: 每次快照完整序列化探索点状态 — Dictionary 序列化为 Canonical JSON < 1ms。快照频率: 每次搜索后（最多 6 次）+ 威胁结算后（最多 3 次）+ 提取时（1 次）+ 结算完成（1 次）= 最多 11 次/会话。

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. 在 project.godot 中注册 ExplorationManager 为 Autoload #11
2. 实现 4 阶段状态机 + 5 种子状态枚举 + 全部无效转换拒绝
3. 实现 F-11-01 search_yield() — 区域权重表 + 不放回抽取 + 空池守卫
4. 实现 F-11-02 threat_trigger() — 环境必触发 + 守卫概率 + build_threat_context
5. 实现 F-11-03 scout_preview_level() — 3 档分段映射
6. 实现 F-11-04 extraction_loss_settlement() — compute_loss + Unique 物品保护
7. 实现 F-11-05 state_variant_transition() — 8 种转换
8. 实现 F-11-06 intel_yield() — 固定产出
9. 实现 _validate_encounter_context() + _build_fallback_context() — ADR-0010 合同
10. 实现 10 个 typed 信号声明和发射
11. 实现 ADR-0003 serializer/deserializer + _restore_active_session()
12. 实现多威胁优先级排序 (_sort_threats_by_priority)
13. 实现 Pool 5 容量检查和取舍流程 (confirm_search_pickup)
14. 实现 CombatManager 战斗结果回调 (on_combat_result)
15. 实现场景层合同的方法接口
16. 单元测试: 4 阶段状态机全部有效/无效转换, F-11-01 区域权重正确性 (含 danger-changed 修正), F-11-02 环境必触发/守卫概率/交互必触发, F-11-03 3 档映射全部 η_scout 值, F-11-04 λ_success=0.08 / λ_forced=0.25 / Unique 保护 / Q≤1 边界, F-11-05 全部 8 种转换, F-11-06 固定产出, EncounterContext fallback 全部 5 种条件, EC-11-10 多威胁排序确定性, EC-11-04/05 容量取舍流程, 存档→读档探索点状态一致性

## Validation Criteria

- 4 阶段状态机全部有效转换通过；全部 7 种无效转换被拒绝
- F-11-01: unlooted A_core empty_chance=0.00 → 100 次搜索 0 次空结果；D_outer empty_chance=0.35 → 接近 35% 空结果率
- F-11-02: 环境威胁靠近必触发 (100%)；守卫威胁靠近 ~70% 触发；交互触发 always 100%
- F-11-03: η_scout=0 → PREVIEW_NONE; η_scout=0.48 → PREVIEW_PRESENCE; η_scout=1.0 → PREVIEW_FULL
- F-11-04: Unique 物品 (Q=1, max_stack=1) 损耗量为 0；basic_supply×20 经 λ=0.08 → 损耗 2；经 λ=0.25 → 损耗 5
- F-11-05: 全部 8 种转换正确
- F-11-06: 每情报点 1 个 Unique intel 物品
- EncounterContext fallback 在 5 种条件 (null, 缺 route_id, 缺 destination_id, 无效 voyage_result, resolved_encounters 非 Array) 下正确构建
- 多威胁重叠触发按环境优先 + 距离排序
- 撤离读条 2.5s，进度 0.0→1.0，被打断时重置为 0 + 阶段回 EXPLORING
- 存档→读档: 搜索点消耗状态一致、威胁活跃状态一致、状态变体一致
- DEPARTED 结算: Pool 5 物品正确转移至飞艇仓库，情报写入 #6
- 守卫威胁在 #12 不可用时保持 inert (is_active=true, 不触发不伤害)

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — ExplorationManager 为 Autoload #11，Phase 5 启动
- **ADR-0002**: Signal 通信协议 — 10 signals typed params, sync emit, fan-out 模式
- **ADR-0003**: 存档系统 — `progress.exploration` snapshot package
- **ADR-0004**: InteractionHandler @abstract — 搜索点/情报点/威胁点/撤离锚点交互基类
- **ADR-0005**: 资源池系统 — Pool 5 读写、extract_carried_to_storage
- **ADR-0007**: 知识状态 — has_relevant_intel 描述增强门控、intel 揭示
- **ADR-0009**: 船体状态 — η_scout 侦察效率、apply_hull_damage
- **ADR-0010**: EncounterContext 类型契约 — voyage_completed 信号消费
- **ADR-0011**: 世界修复 — 探索产出为修复提供材料来源
- **ADR-0018**: 战斗系统 — threat_context 传递 + combat_result 回调合同
- **GDD #11**: exploration-scavenge-scenario.md — 完整探索设计
- **GDD #5**: resources-goods-capacity.md — Pool 5 容量系统
- **GDD #10**: navigation-route-risk.md — EncounterContext 生产端
- **GDD #12**: combat-threat-handling.md — 威胁上下文消费
