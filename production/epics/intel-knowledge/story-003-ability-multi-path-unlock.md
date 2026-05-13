# Story 003: Ability Multi-Path Unlock System

> **Epic**: Intel / Knowledge System
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-003`

**ADR Governing Implementation**: ADR-0007 (IntelManager Autoload #6)
**ADR Decision Summary**: 3 条 MVP 能力各含 2-4 条独立解锁路径。跨路径 OR 逻辑——任意单一路径完全满足即解锁。路径内 AND 逻辑——所有子条件必须满足。解锁路径定义采用数据驱动配置 (ability_unlock_paths Dictionary)，添加新路径无需修改算法代码。每次状态变更事件后遍历所有 locked 能力调用 check_unlock_conditions()。已解锁能力跳过检查。能力解锁后永久有效——unlocked → locked 无效转换。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: ability_unlock_paths 为数据驱动配置（Registry 加载）；check_unlock_conditions() 对每条能力 OR 跨路径、每条路径 AND 跨条件
- Forbidden: 硬编码能力解锁逻辑；unlocked 状态被逆转
- Guardrail: 启动时验证每条能力的每条路径的每个条件类型有对应 evaluator 处理器

---

## Acceptance Criteria

### OR/AND Logic

- [ ] **AC-1**: GIVEN 某能力有 3 条路径，其中仅 Path B 的两个条件都已满足，WHEN `check_unlock_conditions(ability_id)`，THEN 返回 true——Path A 未满足不影响
- [ ] **AC-2**: GIVEN 某能力 Path B 有 2 个条件，仅 1 个满足，WHEN `check_unlock_conditions(ability_id)`，THEN 返回 false——AND 逻辑：必须全部满足

### Ability: bird-flight-understanding

- [ ] **AC-3 (Path A)**: GIVEN `pattern.bird-flight-direction` 状态为 confirmed，WHEN check_unlock_conditions，THEN Path A 满足 → 能力解锁
- [ ] **AC-4 (Path B)**: GIVEN `intel.bird-migration-notes` 已消耗 AND triggered_events["pattern.bird-flight-direction"] 非空（至少 1 个观测事件），WHEN check_unlock_conditions，THEN Path B 满足 → 能力解锁
- [ ] **AC-5 (Path B — 不满足)**: GIVEN intel 已消耗但从未触发任何鸟类观测事件（triggered_events 为空），WHEN check_unlock_conditions，THEN Path B 不满足 → 若其他路径也不满足，能力保持 locked
- [ ] **AC-6 (Path C)**: GIVEN `partner.old-sailor` in active_crew AND triggered_events["pattern.bird-flight-direction"] 包含至少 1 个 passive_observation 类型事件，WHEN check_unlock_conditions，THEN Path C 满足 → 能力解锁（即使规律仍为 undiscovered）

### Ability: lighthouse-signal-interpretation

- [ ] **AC-7 (Path C)**: GIVEN `repair_lighthouse_01` 修复完成（repair_completed 条件），WHEN check_unlock_conditions，THEN Path C 满足 → 能力解锁（即使从未观察过灯塔规律）
- [ ] **AC-8 (Path D)**: GIVEN `partner.lighthouse-keeper-descendant` in active_crew AND 至少 1 个灯塔地点已 verified，WHEN check_unlock_conditions，THEN Path D 满足 → 能力解锁

### Ability: fog-navigation

- [ ] **AC-9 (Path C)**: GIVEN fog_traversal_count >= 3，WHEN check_unlock_conditions，THEN Path C 满足 → 能力解锁
- [ ] **AC-10 (Path D)**: GIVEN `partner.cartographer` in active_crew AND triggered_events["pattern.fog-navigation"] 包含至少 2 个任意类型事件，WHEN check_unlock_conditions，THEN Path D 满足 → 能力解锁

### Non-Degradation & Idempotency

- [ ] **AC-11**: GIVEN 能力已解锁，WHEN check_unlock_conditions 被调用，THEN 直接返回 true（跳过条件检查——首行短路），不重复 emit ability_unlocked 信号
- [ ] **AC-12**: GIVEN 能力已解锁，WHEN 后续事件（伙伴离队、intel 消耗）发生，THEN 能力保持 unlocked——unlocked → locked 为无效转换

### Concurrent Path Satisfaction

- [ ] **AC-13**: GIVEN 两条路径同时满足（如 consume_intel 同时满足 Path A pattern 条件 + Path B intel 条件），WHEN check_unlock_conditions，THEN 能力解锁一次，unlock_path 标注第一条检测到的路径

### Startup Validation

- [ ] **AC-14**: GIVEN 某路径的条件类型在 condition_evaluators 中无对应处理器，WHEN 系统启动，THEN 记录 error 日志："missing condition evaluator for type [X] in ability [Y] path [Z]"

---

## Implementation Notes

### Data-Driven Path Configuration

```text
# 从 Registry 加载到 IntelManager
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

### Core Algorithm: check_unlock_conditions()

```text
func check_unlock_conditions(ability_id: StringName) -> bool:
    # 已解锁 → 短路
    if ability_state.get(ability_id, ABILITY_LOCKED) == ABILITY_UNLOCKED:
        return true

    var paths: Array = ability_unlock_paths.get(ability_id, {}).get("paths", [])
    for path in paths:
        if _path_satisfied(path):
            # 解锁！
            ability_state[ability_id] = ABILITY_UNLOCKED
            ability_unlocked.emit(ability_id, path.get("path_id", ""))
            return true

    return false

func _path_satisfied(path: Dictionary) -> bool:
    for condition in path.get("conditions", []):
        if not _condition_met(condition):
            return false
    return true
```

### Condition Evaluators

```text
# 条件类型 → evaluator 映射
var _condition_evaluators: Dictionary = {}

func _init_evaluators() -> void:
    _condition_evaluators = {
        "pattern_state": _eval_pattern_state,
        "intel_consumed": _eval_intel_consumed,
        "observation_event_count": _eval_observation_event_count,
        "observation_event_type_count": _eval_observation_event_type_count,
        "partner_in_crew": _eval_partner_in_crew,
        "repair_completed": _eval_repair_completed,
        "location_visit_count": _eval_location_visit_count,
        "fog_traversal_count": _eval_fog_traversal_count,
    }

func _condition_met(condition: Dictionary) -> bool:
    var evaluator: Callable = _condition_evaluators.get(condition["type"])
    if evaluator == null:
        push_error("missing condition evaluator for type %s" % condition["type"])
        return false
    return evaluator.call(condition)

func _eval_pattern_state(cond: Dictionary) -> bool:
    var ps: Dictionary = pattern_state.get(cond["pattern_id"], {})
    return _compute_pattern_state(ps.get("observation_score", 0), cond["pattern_id"]) >= cond["required_state"]

func _eval_intel_consumed(cond: Dictionary) -> bool:
    return cond["intel_id"] in consumed_intel_ids

func _eval_observation_event_count(cond: Dictionary) -> bool:
    var ps: Dictionary = pattern_state.get(cond["pattern_id"], {})
    return ps.get("triggered_events", []).size() >= cond["min_count"]

func _eval_observation_event_type_count(cond: Dictionary) -> bool:
    var ps: Dictionary = pattern_state.get(cond["pattern_id"], {})
    var events: Array = ps.get("triggered_events", [])
    var match_count: int = 0
    var event_type: StringName = cond["event_type"]
    for event_id in events:
        if _get_event_type(cond["pattern_id"], event_id) == event_type:
            match_count += 1
    return match_count >= cond["min_count"]

func _eval_partner_in_crew(cond: Dictionary) -> bool:
    return cond["partner_id"] in active_crew

func _eval_repair_completed(cond: Dictionary) -> bool:
    return cond["repair_node_id"] in _completed_repairs

func _eval_location_visit_count(cond: Dictionary) -> bool:
    var count: int = 0
    var required_state: int = cond.get("required_state", KNOWLEDGE_VERIFIED)
    var location_tag: StringName = cond.get("location_tag", "")
    for loc_id in _locations_with_tag(location_tag):
        if knowledge_state.get(loc_id, KNOWLEDGE_UNKNOWN) >= required_state:
            count += 1
    return count >= cond["min_count"]

func _eval_fog_traversal_count(cond: Dictionary) -> bool:
    return fog_traversal_count >= cond["min_count"]
```

### Re-evaluation Trigger

```text
# 在每个上游事件方法完成后调用
func _reevaluate_ability_unlocks() -> void:
    for ability_id in ability_unlock_paths:
        if ability_state.get(ability_id, ABILITY_LOCKED) == ABILITY_LOCKED:
            check_unlock_conditions(ability_id)
```

### Startup Validation

```text
func _validate_ability_paths() -> void:
    for ability_id in ability_unlock_paths:
        var paths: Array = ability_unlock_paths[ability_id].get("paths", [])
        for path in paths:
            for condition in path.get("conditions", []):
                if not _condition_evaluators.has(condition["type"]):
                    push_error("Ability %s path %s: missing condition evaluator for type '%s'"
                              % [ability_id, path.get("path_id", "?"), condition["type"]])
```

---

## Out of Scope

- 能力解锁后机械效果的激活（由下游系统在收到 `ability_unlocked` 信号后自行实现）
- 伙伴系统的伙伴加入/离开判定（伙伴系统拥有，通过 `on_partner_joined`/`on_partner_left` 通知本系统）
- 修复系统的修复完成判定（修复系统拥有，通过 `on_repair_completed` 通知本系统）
- Post-MVP 能力（完整游戏 10-15 条能力）的定义

---

## QA Test Cases

- **AC-3 through AC-10**: All 4 paths for bird-flight, all 4 for lighthouse, all 4 for fog
  - Given: Mock 满足特定路径的所有条件
  - When: check_unlock_conditions(ability_id)
  - Then: 返回 true，ability_state → UNLOCKED, ability_unlocked signal emitted with correct path_id
  - Edge case: 仅部分条件满足 → 返回 false，状态保持 locked

- **AC-13**: Concurrent path satisfaction
  - Given: pattern.bird-flight-direction = confirmed (Path A satisfied) AND intel consumed + observation exists (Path B satisfied)
  - When: check_unlock_conditions("ability.bird-flight-understanding")
  - Then: 能力解锁一次，unlock_path = "path_a_pattern_confirmed"（第一条被检测到的路径）

- **AC-11**: Idempotency
  - Given: ability already UNLOCKED
  - When: check_unlock_conditions called again
  - Then: 返回 true（不重复 emit ability_unlocked）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/intel/ability/AbilityUnlockTest.csproj` — must exist and pass
**Status**: [x] 23/23 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (pattern state for pattern_state condition), Story 002 (knowledge_state for location_visit_count condition), content-registry Epic (Registry provides ability path definitions)
- Unlocks: Story 005 (re-evaluation triggers in event receivers), Story 006 (query_ability_state)
