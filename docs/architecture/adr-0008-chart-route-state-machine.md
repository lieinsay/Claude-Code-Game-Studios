# ADR-0008: 航图路线状态机与出航承诺 — Chart Autoload #9

## Status
Accepted

## Date
2026-05-05

## Summary
Chart 作为 Autoload #9，管理 5 状态航图层级状态机（LOADING → BROWSING → ROUTE_SELECTED → DEPARTURE_CONFIRMED + ERROR）、4 状态航线子状态机（BROWSABLE / SELECTED / UNAVAILABLE / LOCKED）、5 条纯函数公式（route_visibility, route_selectability, chart_state_transition, snapshot_package_validity, route_display_order）、两步出航确认流程（不可逆 route_committed 信号）、以及 progress.routes 快照包。Chart 是连接 IntelManager（#6）、Navigation（#10）、UIManager（#16）的枢纽——从上游读取知识状态，对下游发出出航承诺，向 UI 提供只读数据接口。

## Decision Makers
User + Claude Code

## Last Verified
2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Game Logic / Data + UI Interaction |
| **Knowledge Risk** | MEDIUM — 状态机和公式为纯 GDScript 逻辑；UI 交互涉及 Godot 4.6 dual-focus 系统 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md`, `docs/engine-reference/godot/modules/ui.md`, `docs/engine-reference/godot/modules/input.md`, `design/gdd/chart-route-planning.md` |
| **Post-Cutoff APIs Used** | Dual-focus 系统 (4.6) — Chart 内航线选择的 mouse/keyboard focus 同步；`create_tween()` (4.0+) — 墨迹扩散动画由 UIManager 执行，Chart 只提供动画触发信号 |
| **Verification Required** | 四大内容域门控（routes/world/intel/threats 全部 COMPLETE）正确性；route_committed 单次发射唯一性（双击/连点保护）；progress.routes 快照 roundtrip（写入→读取→校验）；RETRY cooldown（2.0s）在快速连续 RETRY 触发下的正确执行 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload 第 6 位加载, Phase 3b core_data_ready)；ADR-0002 (Signal 通信协议 — route_committed/route_selection_failed/route_enhanced 信号)；ADR-0003 (progress.routes 快照包持久化 — domain serializer 注册, SnapshotPackage 格式)；ADR-0005 (ResourcesManager — consume_intel 可能间接改变 route_selectability)；ADR-0006 (Web 单线程约束 — 状态转换在同一帧内完成)；ADR-0007 (IntelManager — query_route_knowledge / query_route_accessibility 查询接口)；ADR-0011 (WorldRepair — repair_completed → route_enhanced 连锁) |
| **Enables** | ADR-0010 (EncounterContext — Navigation #10 消费 route_committed 后构建航行上下文)；Navigation #10 实现 (route_committed 是航行系统的唯一触发入口)；Exploration #11 实现 (航行结束后返回 Hub，Chart 恢复可用) |
| **Blocks** | Navigation 系统故事 — 必须等 route_committed 信号合约确定后才能编写；Exploration 出发过渡故事 — 依赖 DEPARTURE_CONFIRMED 后的控制权移交 |
| **Ordering Note** | 应在 ADR-0007 (IntelManager) Accepted 后立即 Accepted — Chart 的所有上游查询依赖 IntelManager 接口。ADR-0010 (EncounterContext) 依赖本 ADR 的 route_committed 信号签名为 Navigation 构建航行上下文提供 route_id/destination_id/hazard_tags |

## Context

### Problem Statement

《云海织航》的航图是玩家出航前的核心决策界面。GDD #9 定义了完整的状态机、公式和交互规则——5 状态航图层级状态机、4 状态航线子状态机、5 条公式、两步出航确认流程——但未做出架构层面的部署决策：Chart 以何种形式存在、运行时状态如何存储、route_committed 信号的签名与 emit 保证、progress.routes 快照包如何与 ADR-0003 持久化契约集成、以及 Chart 与 IntelManager（#6）、Navigation（#10）、UIManager（#16）的合约边界如何定义。

Chart 是数据/UI 分离架构的关键执行点：Chart 拥有所有航图数据和状态机逻辑，UIManager 拥有所有视觉渲染和输入处理。这个分离必须在架构层面明确——否则 UI 代码会侵入数据层，或数据逻辑泄漏到 UI 控件中。

### Constraints

- **Godot 4.6.2 + GDScript**: 状态机和公式为纯游戏逻辑，无引擎特定 API 依赖；UI 交互层涉及 dual-focus 系统
- **Web 单线程**: 所有状态变更在同一帧内同步完成；`route_committed` 信号同步 emit，fan-out 在 emit 调用栈内完成
- **ADR-0001 启动顺序**: Chart 在 Phase 4 (core_ready) 初始化，依赖 Phase 3 的 Registry + Persistence + Intel
- **ADR-0002 信号协议**: `route_committed` 使用 typed params, sync emit, {noun}_{verb_past} 命名
- **ADR-0003 持久化**: 状态通过 `progress.routes` snapshot package 保存/恢复；Chart 实现 domain serializer
- **ADR-0007 IntelManager**: Chart 通过 `query_route_knowledge()` 和 `query_route_accessibility()` 读取知识状态——只读，永远不写入
- **数据/UI 分离**: Chart 只提供数据（状态枚举、航线列表、选中 ID、筛选器状态）；UIManager 拥有所有视觉渲染、控件位置、输入处理和动画

### Requirements

- 管理 5 状态航图层级状态机 + 4 状态航线子状态机（每条已渲染航线独立持有）
- 实现 5 条公式：route_visibility, route_selectability, chart_state_transition, snapshot_package_validity, route_display_order
- 四大内容域（routes/world/intel/threats）全部 COMPLETE 才能进入 BROWSING
- 两步出航确认：第一步刷新风险数据 → 第二步承诺出航
- `route_committed` 事件单次发射保证（双击/连点/同帧双事件保护）
- `DEPARTURE_CONFIRMED` 是终端状态——不可逆，所有后续触发返回 `allowed: false`
- 实现 `progress.routes` domain serializer，包含 `snapshot_package_validity` 校验
- 提供 4 个只读查询接口给 UIManager：`get_chart_state()`, `get_visible_routes()`, `get_selected_route()`, `get_filter_state()`

## Decision

### 1. Chart 作为 Autoload #9 — Dictionary 状态存储

Chart 在 Phase 3b (core_data_ready) 初始化（ADR-0001: Autoload 第 6 位加载，Phase 3b 在 Intel 就绪后初始化），常驻内存。所有运行时状态存储在 `Dictionary[StringName, Variant]` 中：

```gdscript
# Chart Autoload #9 — 内部状态
var _state: Dictionary = {
    "_chart_state": &"LOADING",           # StringName — 航图层级状态机
    "_route_states": {},                  # Dictionary[StringName, StringName] — route_id → sub-state
    "_visible_routes": [],                # Array[StringName] — 已排序的可见航线 ID
    "_selected_route_id": null,           # StringName | null
    "_hide_rumored": false,               # bool — 筛选器状态
    "_last_departure_timestamp": 0.0,     # float — 上次出航时间戳
    "_last_committed_route_id": &"",      # StringName — 上次承诺的航线 ID
    "_departure_lock_remaining": 0.0,     # float — 锁定剩余时间
    "_retry_cooldown_remaining": 0.0,     # float — RETRY 冷却剩余时间
    "_failed_domain_states": {},          # Dictionary — 失败时各域状态快照
    "_internal_warning_counter": 0,       # int — 部分查询失败计数
}
```

### 2. 航图层级状态机

5 个状态，6 条合法转换：

```
                 ┌──────────┐
                 │ LOADING  │
                 └────┬─────┘
           COMPLETE /   \ FAIL
                   /     \
            ┌─────────┐  ┌───────┐
            │BROWSING │  │ ERROR │
            └────┬────┘  └───┬───┘
       SELECT /  \ DESELECT  │ RETRY
             /    \          │
    ┌──────────────┐         │
    │ROUTE_SELECTED│         │
    └──────┬───────┘         │
      CONFIRM                │
           │                 │
    ┌──────┴──────────┐     │
    │DEPARTURE_CONFIRMED│    │
    │  (terminal)      │     │
    └──────────────────┘     │
                             │
            LOADING ◄────────┘
```

**转换表**：

| 当前状态 | 触发 | 目标状态 | 条件 |
|----------|------|---------|------|
| `LOADING` | `COMPLETE` | `BROWSING` | routes/world/intel/threats 全部 COMPLETE，航线数据加载成功 |
| `LOADING` | `FAIL` | `ERROR` | 任一内容域非 COMPLETE，或注册表查询返回 FAILED |
| `BROWSING` | `SELECT` | `ROUTE_SELECTED` | `trigger_payload.route_id` 对应航线的子状态为 BROWSABLE |
| `ROUTE_SELECTED` | `DESELECT` | `BROWSING` | Esc 或点击空白区域，或系统生成的强制取消选择 (EC-5/6/8) |
| `ROUTE_SELECTED` | `CONFIRM` | `DEPARTURE_CONFIRMED` | 两步确认完成，`trigger_payload.route_id` 有效 |
| `ERROR` | `RETRY` | `LOADING` | 玩家手动重试，且 `retry_cooldown_remaining <= 0` |

**无效转换**：

| 禁止转换 | 原因 |
|----------|------|
| `DEPARTURE_CONFIRMED → *` | 终端状态——出航已承诺，不可逆 |
| `ERROR → BROWSING` | 必须通过 LOADING 重试验证内容域 |
| `UNAVAILABLE → SELECTED` | 航线子状态机约束——必须先变为 BROWSABLE |
| `LOCKED → *` | 出航锁定中，所有交互永久禁用 |

**未列出的转换**: 任何 (state, trigger) 组合未出现在转换表中时，默认返回 `{allowed: false}`。

### 3. 航线子状态机

每条已渲染航线独立持有 4 状态子状态机：

```
  BROWSABLE ──(player select)──→ SELECTED
      ↑                              │
      │ (deselect / select other)    │
      └──────────────────────────────┘
      ↑                              │
      │ (condition change:           │ (chart → DEPARTURE_CONFIRMED)
      │  ability unlock,             │
      │  dock change,                ↓
      │  traversable restored)    LOCKED (terminal)
      │
  UNAVAILABLE ←──(condition change: traversable→false,
                   dock mismatch, knowledge revoked)── BROWSABLE
```

**转换规则**：

| 当前子状态 | 触发 | 目标子状态 | 条件 |
|-----------|------|-----------|------|
| `BROWSABLE` | player_select | `SELECTED` | chart_state == BROWSING |
| `SELECTED` | player_deselect | `BROWSABLE` | Esc 或选择另一条航线 |
| `SELECTED` | system_deselect | `BROWSABLE` | 知识状态撤销、注册表删除、停靠地变更 |
| `BROWSABLE` | condition_change | `UNAVAILABLE` | traversable→false 或 origin != docked_location 或 knowledge→unknown |
| `UNAVAILABLE` | condition_change | `BROWSABLE` | traversable 恢复 + origin == docked_location + knowledge >= rumored |
| `*` | chart_departure_confirmed | `LOCKED` | chart_state → DEPARTURE_CONFIRMED |

### 4. 5 条公式 — 纯函数实现

所有公式实现为 Chart 的纯函数——不修改状态，只返回计算结果：

#### Formula 1: route_visibility

```gdscript
func route_visibility(route_id: StringName, hide_rumored: bool) -> bool:
    var knowledge_state: int = _query_knowledge_state(route_id)
    if knowledge_state == KNOWLEDGE_UNKNOWN:
        return false
    if hide_rumored and knowledge_state == KNOWLEDGE_RUMORED:
        return false
    return true
```

#### Formula 2: route_selectability

```gdscript
func route_selectability(route_id: StringName) -> StringName:
    if not route_visibility(route_id, _state["_hide_rumored"]):
        return &"hidden"
    if _state["_chart_state"] == &"DEPARTURE_CONFIRMED":
        return &"locked"
    var accessibility: Dictionary = IntelManager.query_route_accessibility(route_id)
    if not accessibility.get("traversable", false):
        return &"unavailable"
    var origin_id: StringName = Registry.get_route_origin(route_id)
    var docked: StringName = _get_current_docked_location_safe()
    if origin_id != docked:
        return &"unavailable"
    if route_id == _state["_selected_route_id"]:
        return &"selected"
    if _state["_chart_state"] == &"ROUTE_SELECTED":
        return &"browsable"
    return &"browsable"
```

**短路求值顺序**: hidden → locked → unavailable (traversable) → unavailable (origin) → selected → browsable。hidden 在第一分支返回，避免对不可见航线进行后续查询。

#### Formula 3: chart_state_transition

```gdscript
func chart_state_transition(trigger: StringName, payload: Dictionary) -> Dictionary:
    var current: StringName = _state["_chart_state"]

    # 终端状态守卫
    if current == &"DEPARTURE_CONFIRMED":
        return {"new_state": current, "allowed": false}

    match current:
        &"LOADING":
            if trigger == &"COMPLETE":
                return {"new_state": &"BROWSING", "allowed": true}
            if trigger == &"FAIL":
                return {"new_state": &"ERROR", "allowed": true}
        &"BROWSING":
            if trigger == &"SELECT":
                var route_id: StringName = payload.get("route_id", &"")
                if route_selectability(route_id) == &"browsable":
                    return {"new_state": &"ROUTE_SELECTED", "allowed": true}
        &"ROUTE_SELECTED":
            if trigger == &"DESELECT":
                return {"new_state": &"BROWSING", "allowed": true}
            if trigger == &"CONFIRM":
                return {"new_state": &"DEPARTURE_CONFIRMED", "allowed": true}
        &"ERROR":
            if trigger == &"RETRY":
                if _state["_retry_cooldown_remaining"] <= 0.0:
                    return {"new_state": &"LOADING", "allowed": true}

    return {"new_state": current, "allowed": false}
```

#### Formula 4: snapshot_package_validity

```gdscript
func snapshot_package_validity(pkg: SnapshotPackage, current_time: float,
                                timestamp_tolerance: float, route_registry: Array) -> Dictionary:
    var violations: Array = []

    if pkg == null or typeof(pkg.payload) != TYPE_DICTIONARY:
        violations.append("malformed snapshot package")
        return {"valid": false, "violations": violations}

    if not is_finite(current_time):
        violations.append("non-finite current_time")

    if pkg.domain_id != "progress.routes":
        violations.append("wrong domain_id")

    if not pkg.is_valid():
        violations.append("SnapshotPackage.is_valid() returned false")
        # 收集 is_valid() 各条件的失败
        if pkg.snapshot_schema_version <= 0:
            violations.append("invalid snapshot_schema_version")
        if pkg.content_domain_versions.is_empty():
            violations.append("empty content_domain_versions")
        if pkg.domain_state != SnapshotPackage.DOMAIN_READY:
            violations.append("domain_state not READY: " + str(pkg.domain_state))

    var payload: Dictionary = pkg.payload
    var required: Array[StringName] = [&"last_committed_route_id", &"departure_state",
                                        &"active_filter", &"last_departure_timestamp"]
    for field in required:
        if not field in payload:
            violations.append("missing field: " + field)

    if payload.get("departure_state") != "DEPARTURE_CONFIRMED":
        violations.append("invalid departure_state")

    var ts: float = payload.get("last_departure_timestamp", 0.0)
    if not is_finite(ts):
        violations.append("non-finite timestamp")
    elif ts <= 0.0:
        violations.append("timestamp is epoch or uninitialized")
    elif ts > current_time + timestamp_tolerance:
        violations.append("timestamp in future")

    var route_id: StringName = payload.get("last_committed_route_id", &"")
    if route_id not in route_registry:
        violations.append("route_id not found in registry: " + str(route_id))

    if violations.size() > 0:
        return {"valid": false, "violations": violations}
    return {"valid": true, "violations": []}
```

#### Formula 5: route_display_order

```gdscript
func route_display_order(route_id: StringName) -> int:
    var knowledge_state: int = _query_knowledge_state(route_id)
    var distance_band: StringName = Registry.get_route_distance_band(route_id)

    var rank_by_knowledge: int
    match knowledge_state:
        KNOWLEDGE_VERIFIED:   rank_by_knowledge = 1
        KNOWLEDGE_IDENTIFIED: rank_by_knowledge = 2
        KNOWLEDGE_RUMORED:    rank_by_knowledge = 3
        _:                    return 999  # unknown — 不应出现在可见列表

    var rank_by_distance: int
    match distance_band:
        &"short":  rank_by_distance = 1
        &"medium": rank_by_distance = 2
        &"long":   rank_by_distance = 3
        _:         rank_by_distance = 2  # 未知距离带视为 medium

    return rank_by_knowledge * 100 + rank_by_distance
```

**输出范围**：101–303（verified+short=101 ~ rumored+long=303）。值越小排越前。知识置信度权重（×100）压倒距离权重（+1~3），确保玩家验证过的航线始终排在传闻航线之前。

### 5. route_committed — 不可逆出航承诺

```gdscript
## Chart 定义的出航承诺信号
signal route_committed(route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName])

## Chart 定义的失败通知信号（配对的 fail 信号 — ADR-0002 要求）
signal route_selection_failed(route_id: StringName, reason: StringName)

## Chart 定义的航线增强信号（WorldRepair 修复完成连锁）
signal route_enhanced(route_id: StringName, enhancement_id: StringName)

func _commit_departure(route_id: StringName) -> void:
    # 1. 刷新风险数据 — 两步确认第一步必须展示最新状态
    var accessibility: Dictionary = IntelManager.query_route_accessibility(route_id)
    if not accessibility.get("traversable", false):
        _force_deselect(&"route_not_traversable")
        route_selection_failed.emit(route_id, &"route_not_traversable")
        return

    # 2. 构建并校验快照包
    var pkg: SnapshotPackage = _build_snapshot_package(route_id)
    var timestamp_tolerance: float = Registry.get_constant(&"base_timestamp_tolerance", 300.0)
    var route_registry: Array = Registry.list_by_kind(&"route")
    var validation: Dictionary = snapshot_package_validity(pkg, Time.get_unix_time_from_system(),
                                                            timestamp_tolerance, route_registry)
    if not validation["valid"]:
        _enter_error_state(validation["violations"])
        route_selection_failed.emit(route_id, &"snapshot_invalid")
        return

    # 3. 状态转换 — 终端状态，不可逆
    _state["_chart_state"] = &"DEPARTURE_CONFIRMED"
    _state["_last_committed_route_id"] = route_id
    _state["_last_departure_timestamp"] = Time.get_unix_time_from_system()
    _state["_departure_lock_remaining"] = Registry.get_constant(&"base_lock_duration", 2.0)
    _set_all_routes_locked()

    # 4. 发射 route_committed — 单次 emit，同步 fan-out
    var route_data: Dictionary = Registry.get_route_data(route_id)
    route_committed.emit(
        route_id,
        route_data.get("destination_location_id", &""),
        accessibility.get("hazard_tags", [])
    )

    # 5. 触发快照写入
    Persistence.request_save(SAVE_TRIGGER_DEPARTURE)
```

**单次发射保证**：状态机在 `chart_state_transition` 中执行终端守卫——第一个 CONFIRM 触发后状态变为 `DEPARTURE_CONFIRMED`，第二个 CONFIRM 触发命中终端守卫返回 `allowed: false`。状态机层面的保证不依赖外部去重逻辑。

### 6. progress.routes Domain Serializer

Chart 注册 domain serializer 给 Persistence (#3)，遵循 ADR-0003 SnapshotPackage 格式：

```gdscript
func register_serializer() -> void:
    Persistence.register_domain_serializer("progress.routes", _serialize_routes)

func _serialize_routes() -> SnapshotPackage:
    var pkg := SnapshotPackage.new()
    pkg.domain_id = "progress.routes"
    pkg.snapshot_schema_version = 1
    pkg.content_domain_versions = {"routes": 1, "intel": 1}
    pkg.stable_id_refs = [_state["_last_committed_route_id"]] if not _state["_last_committed_route_id"].is_empty() else []
    pkg.payload = {
        "last_committed_route_id": _state["_last_committed_route_id"],
        "departure_state": _state["_chart_state"],
        "active_filter": "hide_rumored" if _state["_hide_rumored"] else "show_all",
        "last_departure_timestamp": _state["_last_departure_timestamp"],
        "hide_rumored": _state["_hide_rumored"],
    }
    pkg.domain_state = SnapshotPackage.DOMAIN_READY
    pkg.domain_error_code = ""
    pkg.migration_hint = ""
    return pkg

func _deserialize_routes(snapshot: SnapshotPackage) -> void:
    var payload: Dictionary = snapshot.payload
    _state["_last_committed_route_id"] = payload.get("last_committed_route_id", &"")
    _state["_last_departure_timestamp"] = payload.get("last_departure_timestamp", 0.0)
    _state["_hide_rumored"] = payload.get("hide_rumored", false)

    var departure_state: StringName = payload.get("departure_state", &"LOADING")
    if departure_state == &"DEPARTURE_CONFIRMED":
        # 恢复时跳过渲染，直接加载出航锁定序列后移交 Navigation
        _state["_chart_state"] = &"DEPARTURE_CONFIRMED"
        _state["_departure_lock_remaining"] = 0.0  # 已过期 — 立即移交
        _resume_departure_sequence()
    else:
        _state["_chart_state"] = &"LOADING"
```

### 7. 只读查询接口（给 UIManager #16）

```gdscript
func get_chart_state() -> StringName:
    return _state["_chart_state"]

func get_visible_routes() -> Array:
    return _state["_visible_routes"]  # 已按 display_order 排序

func get_selected_route() -> StringName:
    return _state["_selected_route_id"] if _state["_selected_route_id"] != null else &""

func get_filter_state() -> Dictionary:
    return {"hide_rumored": _state["_hide_rumored"]}
```

**合约约定**: 以上接口只返回数据和状态枚举——不返回颜色值、位置坐标、透明度、动画关键帧。UIManager 完全拥有视觉层的实现自由。

### 8. 外部状态变化响应

Chart 连接上游系统的信号以响应航线相关状态的外部变化：

```gdscript
# 在 Phase 3b core_data_ready 中连接
func _on_core_data_ready() -> void:
    IntelManager.knowledge_advanced.connect(_on_knowledge_changed)
    IntelManager.ability_unlocked.connect(_on_ability_changed)
    WorldRepair.repair_completed.connect(_on_repair_completed)

func _on_knowledge_changed(location_id: StringName, _prev: int, _new: int) -> void:
    # 重新评估所有涉及该地点的航线的可选择性
    _reevaluate_routes_for_location(location_id)

func _on_ability_changed(_ability_id: StringName, _unlock_path: StringName) -> void:
    # 重新评估所有航线的 traversable 条件
    _reevaluate_all_routes_accessibility()

func _on_repair_completed(node_id: StringName) -> void:
    # 世界修复完成 → 评估哪些航线因此受益
    # 修复节点可能解锁新航线、降低已知航线风险等级
    var enhanced_routes: Array[StringName] = _evaluate_route_enhancements(node_id)
    for route_id in enhanced_routes:
        route_enhanced.emit(route_id, node_id)
```

**AirshipHub 时序安全守卫**：

```gdscript
## 安全获取当前停靠地点 — AirshipHub 在 Phase 5 才实例化
func _get_current_docked_location_safe() -> StringName:
    if not is_instance_valid(AirshipHub) or not AirshipHub.has_method("get_current_docked_location"):
        return &""  # 早期阶段 — 返回空，所有航线的 origin != "" 判定为 unavailable
    return AirshipHub.get_current_docked_location()
```

`route_selectability()` 仅在航图处于 BROWSING 或 ROUTE_SELECTED 状态时被调用——此时 AirshipHub 必然已经实例化（Phase 5+）。`_get_current_docked_location_safe()` 提供防御性空值保护，防止在异常时序下崩溃。

## Alternatives Considered

### Alternative 1: Chart 作为 UIManager 子系统（无独立 Autoload）

- **Description**: 航图状态和逻辑内嵌在 UIManager (#16) 中，作为 UI 屏幕之一管理。route_committed 信号从 UIManager 发出。
- **Pros**: 减少一个 Autoload 常驻内存；Chart 与 UI 渲染在同一系统中，减少跨系统通信
- **Cons**: 违反 GDD #9 的数据/UI 分离合约——UIManager 成为数据所有者，Chart 的 5 条公式和状态机逻辑与 UI 控件耦合。UIManager 的职责从"12 屏幕管理 + 输入路由"膨胀为"12 屏幕管理 + 输入路由 + 航图数据逻辑 + 航线公式 + 快照序列化"。Navigation (#10) 和 Persistence (#3) 需要从 UIManager 读取数据——Presentation 层被 Core 层系统依赖，违反了 ADR-0001 的层级隔离原则
- **Rejection Reason**: 数据/UI 分离是 GDD #9 和 #16 的明确合约要求。Chart 的数据逻辑（5 条公式、状态机、快照校验）与 UI 渲染（羊皮纸纹理、墨迹动画、控件位置）必须分离到不同系统

### Alternative 2: route_committed 作为可逆操作

- **Description**: 出航确认后允许玩家取消——DEPARTURE_CONFIRMED 可以回退到 BROWSING 或 ROUTE_SELECTED
- **Pros**: 给玩家更多容错空间；实现更简单（无需终端状态守卫）
- **Cons**: 违反 GDD #9 的不可逆约束——DEPARTURE_CONFIRMED 是终端状态。破坏了 Pillar 1「规划先于冒险」——如果出航可以随时取消，规划就没有重量。route_committed 的可逆性会传播到 Navigation (#10) 和 Persistence (#3)——这两个系统也需要支持撤销，增加了跨系统复杂性。快照中 departure_state 不再是可靠的历史记录
- **Rejection Reason**: GDD 明确要求 DEPARTURE_CONFIRMED 不可逆——这是设计意图，不是技术权衡。出航承诺的重量是 Pillar 1 的核心体验

### Alternative 3: 航线状态由 IntelManager 直接管理

- **Description**: Chart 不独立存在——航线可见性和可选择性评估逻辑放在 IntelManager (#6) 中，由 IntelManager 提供 `get_visible_routes()` 和 `get_selectable_routes()` 接口
- **Pros**: 减少一个 Autoload；知识状态和航线评估在同一系统中，无需跨系统查询
- **Cons**: IntelManager 的职责从"玩家知识/能力状态"膨胀为"玩家知识 + 航线评估 + 出航确认 + 快照序列化"。IntelManager 需要知道 Chart 的 UI 状态（筛选器、选中航线）——这是 UI 关注点泄漏到知识系统。UIManager 和 Navigation 都与 IntelManager 耦合——IntelManager 成为 God Object
- **Rejection Reason**: Chart 的筛选器状态（hide_rumored）和选中状态（selected_route_id）是 UI 会话状态，不是知识状态。将它们放入 IntelManager 违反了"知识系统只管理知识"的单一职责原则

## Consequences

### Positive
- Chart 的 5 条公式实现为纯函数——独立可测试，不依赖引擎或 UI 状态
- 数据/UI 分离使 UIManager 可以独立迭代视觉设计而不影响航线逻辑
- `route_committed` 的单次发射保证由状态机本身提供——不依赖外部去重逻辑
- progress.routes 快照包含完整 departure_state，支持"出航中途恢复"场景
- 终端状态守卫 + RETRY cooldown 防止了 ERROR↔LOADING 紧循环
- 短路求值优化——hidden 在第一分支返回，避免对不可见航线进行 3 次跨系统查询

### Negative
- 增加了一个 Autoload（#9）——常驻内存 ~2KB（Dictionary 状态 + 航线缓存）
- Chart 与 4 个上游系统耦合（Registry, IntelManager, AirshipHub, Persistence）——初始化依赖链较长
- 知识状态变化时需重新评估所有航线——最坏情况 O(N) 次 `route_selectability` 调用（MVP 仅 2 条航线，可忽略）
- DEPARTURE_CONFIRMED 的不可逆性意味着没有"撤销出航"的架构路径——如果未来设计需要此功能，需要修改 ADR

### Risks
- **R1: 四大内容域门控过严** — 任一域非 COMPLETE 即进入 ERROR。缓解：ERROR 状态展示具体失败域名称和当前状态，RETRY 按钮可用；`retry_cooldown` 防止紧循环
- **R2: 知识状态变化与航图打开竞态** — 玩家在航图打开期间，IntelManager 可能更新知识状态。缓解：Chart 连接 `knowledge_advanced` 和 `ability_unlocked` 信号，实时重新评估航线可选择性
- **R3: 快照校验时间戳容差** — `timestamp_tolerance`（默认 300s）可能拒绝合法的时钟调整存档。缓解：容差值来自注册表常量，可在 tuning 阶段调整；提供明确的 violation 消息供调试
- **R4: AirshipHub 未就绪时调用 route_selectability** — Chart (Phase 3b) 初始化早于 AirshipHub (Phase 5)。缓解：`_get_current_docked_location_safe()` 防御性空值检查；`route_selectability()` 仅在航图打开后（AirshipHub 已存在）被调用
- **R5: route_committed 信号签名与 ADR-0002 临时目录不一致** — ADR-0002 的临时信号目录定义 `route_committed(route_id: String)` 仅有 1 参数。缓解：本 ADR 以 GDD #9 为权威来源，定义 3 参数签名；ADR-0002 的对应条目应在下一轮 architecture-review 中更新

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| chart-route-planning.md | 5 状态航图层级状态机 + 4 状态航线子状态机 | 第 2-3 节 — 完整状态机定义、转换表、无效转换约束 |
| chart-route-planning.md | 5 条公式 (route_visibility, route_selectability, chart_state_transition, snapshot_package_validity, route_display_order) | 第 4 节 — 纯函数实现，含短路求值、输出范围、MVP 验证 |
| chart-route-planning.md | 两步出航确认 + route_committed 不可逆事件 | 第 5 节 — `_commit_departure()` 含刷新、校验、状态转换、信号发射 |
| chart-route-planning.md | progress.routes 快照包 + 有效性校验 | 第 4 节 Formula 4 + 第 6 节 domain serializer |
| chart-route-planning.md | 四大内容域加载门控 | 第 2 节 LOADING→BROWSING 转换条件 |
| chart-route-planning.md | 数据/UI 分离合约 | 第 7 节 — 只读查询接口只返回数据，不返回视觉属性 |
| chart-route-planning.md | EC-3/EC-4: 双击/连点保护 | 第 5 节 — 终端状态守卫 + 状态机层面单次发射保证 |
| chart-route-planning.md | EC-5/6/7/8: 外部状态变化响应 | 第 8 节 — 信号连接 + `_reevaluate` 方法 |
| player-knowledge-intel.md | query_route_knowledge / query_route_accessibility 接口消费 | 第 1/4 节 — Chart 通过 IntelManager 查询接口获取知识状态 |

## Performance Implications
- **CPU**: 所有 5 条公式为 O(1) 或 O(N)（N = 航线数，MVP N=2）。最坏情况 `_reevaluate_all_routes()` 对每条航线调用 `route_selectability()` — MVP 下 < 0.01ms
- **Memory**: Chart Autoload 常驻 ~2KB（Dictionary 状态 + 航线缓存 + 序列化器注册）。每条航线缓存 ~200 bytes
- **Load Time**: Phase 4 初始化 < 1ms（注册 domain serializer + 连接信号）；`open_chart()` 加载时间取决于 `list_by_kind("route")` 和批量 `query_route_knowledge()` — 估计 < 10ms（MVP 2 条航线）
- **Network**: N/A（单机 Web 游戏）

## Migration Plan
N/A — 新系统，无现有代码迁移。

Chart 的实现顺序：
1. 状态机骨架 + 转换表验证
2. 5 条公式实现 + 单元测试
3. route_committed 信号 + 终端守卫
4. domain serializer + snapshot_package_validity
5. 知识状态变化响应（信号连接 + 重新评估）
6. UIManager 集成（只读查询接口）

## Validation Criteria
- 5 条公式对 MVP 两条航线的手动计算输出与 GDD Formula 1-5 示例一致
- `chart_state_transition` 对 5×6=30 种 (state, trigger) 组合的返回值与转换表一致（含无效转换返回 `allowed: false`）
- 同一帧内两次 CONFIRM 触发只产生一次 `route_committed` 信号发射
- `snapshot_package_validity` 对 6 种违规类型 + SnapshotPackage.is_valid() 失败各自返回正确的 `violations` 列表
- RETRY cooldown 在 2.0s 内阻止第二次 RETRY
- 快照 roundtrip：`_serialize_routes()` → SnapshotPackage → to_canonical_json() → from_canonical_json() → SnapshotPackage → `_deserialize_routes()` → 状态恢复正确
- `route_committed` 信号签名：3 typed params (StringName, StringName, Array[StringName])
- `route_selection_failed` 在 traversable 检查和快照校验失败时正确发射
- `route_enhanced` 在 `repair_completed` 触发后正确评估并发射
- `_get_current_docked_location_safe()` 在 AirshipHub 为 null 时返回空 StringName，不崩溃

## Related Decisions
- ADR-0001 — Autoload #9 启动顺序 (Phase 4 core_ready)
- ADR-0002 — route_committed 信号协议 ({noun}_{verb_past}, typed params, sync emit)
- ADR-0003 — progress.routes 快照包持久化 (domain serializer 注册)
- ADR-0007 — IntelManager 知识查询接口 (query_route_knowledge, query_route_accessibility)
- ADR-0010 — EncounterContext (Navigation #10 消费 route_committed 后构建航行上下文)
- ADR-0012 — UIManager 屏幕状态机 (Chart 数据通过只读接口提供给 UI 渲染)
