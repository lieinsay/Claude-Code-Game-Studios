# Story 001: Voyage State Machine & Preflight Checks

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-001`

**ADR Governing Implementation**: ADR-0010 (EncounterContext Type — Navigation 生产端, voyage_completed 信号)
**ADR Decision Summary**: NavigationManager Autoload #10 接收 ChartManager 的 route_committed 信号后进入 VOYAGE_PREPARING 状态，构建 VoyageContext（route_id, destination_id, hazard_tags, η_scout, hull_band, M_max, M_loaded, knowledge_state）。6 状态航行状态机：VOYAGE_PREPARING → IN_PROGRESS → ARRIVED/RETREATED/FORCED_LANDING + ABORTED_PREFLIGHT。所有终态不可逆转。VOYAGE_PREPARING 结尾重新查询 can_depart()（TOCTOU 防御——不信任 #9 的预检结果）。若 can_depart=false 或上游查询失败 → ABORTED_PREFLIGHT。route_committed 的第二个事件被拒绝（航程不可重入）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: VoyageContext 在 VOYAGE_PREPARING 中构建完成后才能进入 IN_PROGRESS；TOCTOU 防御——_preflight_check() 结尾重新调用 can_depart()；所有终态不可逆——任何触发返回 {allowed: false}
- Forbidden: 信任 #9 的预检结果而不重新查询 #8；在终态下接受新的 route_committed（航程不可重入）；跳过 VOYAGE_PREPARING 直接进入 IN_PROGRESS
- Guardrail: VOYAGE_PREPARING 中上游查询失败 → ABORTED_PREFLIGHT——不保留部分 VoyageContext，不出航

---

## Acceptance Criteria

### State Machine

- [ ] **AC-1**: GIVEN route_committed 信号到达 + can_depart=true + 所有上游查询成功，WHEN _preflight_check()，THEN voyage_state: VOYAGE_PREPARING → IN_PROGRESS。计时器开始
- [ ] **AC-2**: GIVEN route_committed 信号到达 + can_depart=false，WHEN _preflight_check()，THEN voyage_state → ABORTED_PREFLIGHT。不出航
- [ ] **AC-3**: GIVEN route_committed 信号到达 + Registry 查询 route_id 失败（route_id 不在注册表），WHEN _preflight_check()，THEN → ABORTED_PREFLIGHT。原因："route_id [id] not found in content registry"
- [ ] **AC-4**: GIVEN route_committed 信号到达 + IntelManager.query_route_knowledge() 超时/失败，WHEN _preflight_check()，THEN → ABORTED_PREFLIGHT。不缓存过期知识
- [ ] **AC-5**: GIVEN IN_PROGRESS + elapsed_time ≥ T_voyage，WHEN 进度到达 100%，THEN voyage_state → ARRIVED（终态）
- [ ] **AC-6**: GIVEN IN_PROGRESS + 玩家按 Esc/撤退按钮，WHEN 确认撤退，THEN voyage_state → RETREATED（终态）
- [ ] **AC-7**: GIVEN IN_PROGRESS + hull_integrity_effective ≤ 0，WHEN 检测，THEN voyage_state → FORCED_LANDING（终态）
- [ ] **AC-8**: GIVEN 终态（ARRIVED/RETREATED/FORCED_LANDING/ABORTED_PREFLIGHT），WHEN 任何触发到达，THEN 拒绝——所有终态不可逆

### VoyageContext Construction

- [ ] **AC-9**: GIVEN route_committed(route_id, destination_id, hazard_tags)，WHEN _build_voyage_context()，THEN VoyageContext 包含：
  - route_id, destination_id, hazard_tags（来自信号）
  - η_scout, hull_band, hull_integrity, M_max, M_loaded（来自 #8 ModuleHullManager）
  - knowledge_state, visible_hazard_tags, hidden_hazard_tags（来自 #6 IntelManager）
  - distance_band, all_static_hazard_tags（来自 #1 Registry）

### TOCTOU Defense

- [ ] **AC-10**: GIVEN #9 出航时 can_depart=true，WHEN #10 VOYAGE_PREPARING 结尾重新查询 can_depart() 返回 false，THEN → ABORTED_PREFLIGHT。防御 TOCTOU（#9 预检通过后到 #10 航行启动前状态可能变化）

### Re-entrancy Guard

- [ ] **AC-11**: GIVEN voyage_state=VOYAGE_PREPARING 或 IN_PROGRESS，WHEN 第二个 route_committed 信号到达，THEN 拒绝。记录警告。保持当前状态（航程不可重入）

### hazard_tags Consistency Check

- [ ] **AC-12**: GIVEN route_committed 的 hazard_tags 与 Registry 的静态风险标签不一致，WHEN 构建 VoyageContext，THEN 以 Registry 为准。#9 漏掉的标签补入并警告；#9 多出的标签排除并警告

---

## Implementation Notes

### NavigationManager Autoload #10 — State Storage

```text
# Navigation Autoload #10 — 内部状态
var _voyage_state: StringName = &"IDLE"
var _active_voyage: Dictionary = {}
var _elapsed_time: float = 0.0
var _last_check_time: float = 0.0
var _accumulated_damage: int = 0
var _pending_encounters: Array = []
var _resolved_encounters: Array = []
var _revealed_hidden_tags: Array[StringName] = []
var _damaged_slots: Array[StringName] = []
var _forced_landing_position: StringName = &""

const VOYAGE_STATES := [
    &"IDLE", &"VOYAGE_PREPARING", &"IN_PROGRESS",
    &"ARRIVED", &"RETREATED", &"FORCED_LANDING", &"ABORTED_PREFLIGHT",
]
const TERMINAL_STATES := [&"ARRIVED", &"RETREATED", &"FORCED_LANDING", &"ABORTED_PREFLIGHT"]
```

### Signal Receiving & State Machine Entry

```text
func _on_route_committed(route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName]) -> void:
    if _voyage_state != &"IDLE":
        push_warning("Navigation: route_committed received while in %s state — rejected" % _voyage_state)
        return

    _voyage_state = &"VOYAGE_PREPARING"
    _reset_voyage_state()

    var result: Dictionary = _preflight_check(route_id, destination_id, hazard_tags)
    if result["passed"]:
        _voyage_state = &"IN_PROGRESS"
        _active_voyage = result["context"]
        _start_voyage()
    else:
        _voyage_state = &"ABORTED_PREFLIGHT"
        _active_voyage["abort_reason"] = result["reason"]
        voyage_aborted.emit(result["reason"])
```

### _preflight_check()

```text
func _preflight_check(route_id: StringName, destination_id: StringName,
                       signal_hazard_tags: Array[StringName]) -> Dictionary:
    # 1. 验证 route_id 在注册表中存在
    var route_data: Dictionary = Registry.get_route_data(route_id)
    if route_data.is_empty():
        return {"passed": false, "reason": "route_id [%s] not found in content registry" % route_id}

    # 2. hazard_tags 一致性校验——以 Registry 为准
    var registry_tags: Array = route_data.get("hazard_tags", [])
    var effective_tags: Array[StringName] = _resolve_hazard_tags(signal_hazard_tags, registry_tags)

    # 3. 查询 #8 模块/船体状态
    var can_depart_result: Dictionary = ModuleHullManager.can_depart()
    if not can_depart_result.get("can", false):
        return {"passed": false, "reason": "can_depart false: %s" % str(can_depart_result.get("reasons", []))}

    var hull_band: StringName = ModuleHullManager.get_hull_band()
    var hull_integrity: int = ModuleHullManager.get_hull_integrity()
    var scout_efficiency: float = ModuleHullManager.get_module_efficiency(&"slot_a")  \
        if ModuleHullManager.get_slot_module_type(&"slot_a") == ModuleType.SCOUT else 0.0
    var slot_b_eff: float = ModuleHullManager.get_module_efficiency(&"slot_b") \
        if ModuleHullManager.get_slot_module_type(&"slot_b") == ModuleType.SCOUT else 0.0
    scout_efficiency = maxf(scout_efficiency, slot_b_eff)  # 双侦察取 max

    # 4. 查询 #6 知识状态
    var knowledge_result: Dictionary = IntelManager.query_route_knowledge(route_id)
    if knowledge_result.is_empty():
        return {"passed": false, "reason": "Intel query_route_knowledge failed for %s" % route_id}

    # 5. 构建 VoyageContext
    return {
        "passed": true,
        "context": {
            "route_id": route_id,
            "destination_id": destination_id,
            "hazard_tags": effective_tags,
            "visible_hazard_tags": _extract_visible_tags(effective_tags),
            "hidden_hazard_tags": _extract_hidden_tags(effective_tags),
            "distance_band": route_data.get("distance_band", &"medium"),
            "scout_efficiency": scout_efficiency,
            "hull_band": hull_band,
            "hull_integrity_departure": hull_integrity,
            "knowledge_state": knowledge_result.get("state", KNOWLEDGE_UNKNOWN),
        },
    }
```

### _resolve_hazard_tags()

```text
func _resolve_hazard_tags(signal_tags: Array[StringName], registry_tags: Array) -> Array[StringName]:
    var registry_set: Dictionary = {}
    for tag in registry_tags:
        registry_set[tag] = true

    var signal_set: Dictionary = {}
    for tag in signal_tags:
        signal_set[tag] = true

    # 补入 Registry 有但 signal 缺失的标签
    for tag in registry_tags:
        if not signal_set.has(tag):
            push_warning("Navigation: hazard_tag %s in registry but missing from route_committed — adding" % tag)
            signal_tags.append(tag)

    # 排除 signal 有但 Registry 没有的标签
    var resolved: Array[StringName] = []
    for tag in signal_tags:
        if registry_set.has(tag):
            resolved.append(tag)
        else:
            push_warning("Navigation: hazard_tag %s in signal but not in registry — excluding" % tag)

    return resolved
```

### Retreat Flow

```text
func request_retreat() -> void:
    if _voyage_state != &"IN_PROGRESS":
        return
    _voyage_state = &"RETREATED"
    _finalize_voyage()


func is_retreat_allowed() -> bool:
    return _voyage_state == &"IN_PROGRESS"
```

---

## Out of Scope

- can_depart() 的具体实现——属于 #8 ModuleHullManager
- query_route_knowledge() 的具体实现——属于 #6 IntelManager
- 航行中的具体遭遇解析和伤害计算——属于 Story 005
- 撤退确认 UI 面板——属于 UI 系统 #16

---

## QA Test Cases

- **AC-1/2/3/4**: Preflight paths
  - Given: all OK → IN_PROGRESS
  - Given: can_depart=false → ABORTED_PREFLIGHT
  - Given: route_id not in registry → ABORTED_PREFLIGHT
  - Given: Intel query fails → ABORTED_PREFLIGHT

- **AC-5/6/7**: Terminal states
  - Given: elapsed ≥ T_voyage → ARRIVED
  - Given: player retreat → RETREATED
  - Given: hull ≤ 0 → FORCED_LANDING

- **AC-11**: Re-entrancy
  - Given: VOYAGE_PREPARING + second route_committed → rejected

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/navigation/statemachine/NavStateMachineTest.csproj` — must exist and pass
**Status**: [x] 33/33 PASS — 2026-05-13

---

## Dependencies

- Depends on: chart-route-planning Epic (route_committed signal), modules-hull-state Epic (can_depart, get_hull_band, get_hull_integrity, get_module_efficiency, get_slot_module_type), intel-knowledge Epic (query_route_knowledge), content-registry Epic (get_route_data)
- Unlocks: All subsequent navigation stories
