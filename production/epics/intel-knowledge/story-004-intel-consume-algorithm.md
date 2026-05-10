# Story 004: IntelConsumeResult Algorithm

> **Epic**: Intel / Knowledge System
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-002`

**ADR Governing Implementation**: ADR-0007 (IntelManager Autoload #6)
**ADR Decision Summary**: consume_intel() 按 5 条规则顺序执行：① 已消耗检查 → ERR_INTEL_ALREADY_CONSUMED；② 推进关联地点知识 (unknown/rumored → identified)；③ 对关联规律添加 log_fragment 观测事件 (weight=2)；④ 检查能力解锁条件；⑤ 标记 intel 已消耗。返回 IntelConsumeResult Dictionary 结构 (success, error_code, location_advancements, ability_unlocks, pattern_observations)。单次消耗可产生三重效果（地点推进 + 观测添加 + 能力解锁），此为预期行为——表示高信息密度 intel。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: consume_intel() 返回 Dictionary (非 signal payload——方法返回值可用 Dictionary)；5 条规则按序执行不可乱序
- Forbidden: 已消耗 intel 产生任何状态变更；intel_id 不存在的崩溃行为
- Guardrail: linked_content_ids 为空 → location_advancements 为空数组（不报错——允许纯叙事情报）

---

## Acceptance Criteria

### Basic Consumption Flow

- [ ] **AC-1**: GIVEN intel.bird-migration-notes 未被消耗，WHEN `consume_intel(&"intel.bird-migration-notes")`，THEN 返回 success=true, intel_id="intel.bird-migration-notes", intel_display_name 来自 Registry

### Rule 1: Already Consumed Check

- [ ] **AC-2**: GIVEN intel.bird-migration-notes 已在 consumed_intel_ids 中，WHEN `consume_intel(&"intel.bird-migration-notes")`，THEN 返回 success=false, error_code="ERR_INTEL_ALREADY_CONSUMED", 所有数组为空
- [ ] **AC-3**: GIVEN 重复消耗已消耗 intel，WHEN 检查 consumed_intel_ids、knowledge_state、observation_score，THEN 无任何变更

### Rule 2: Location Advancement

- [ ] **AC-4**: GIVEN intel 定义 linked_content_ids = ["location.whisper-isle", "route.bird-migration-corridor"]，其中 whisper-isle 为 rumored, corridor 为 unknown，WHEN consume_intel，THEN location_advancements 含 2 条目：whisper-isle rumored→identified, corridor unknown→identified
- [ ] **AC-5**: GIVEN intel 的 linked_content_ids 中某地点已为 identified，WHEN consume_intel，THEN 该地点不在 location_advancements 中——已达此级别
- [ ] **AC-6**: GIVEN intel 的 linked_content_ids 中某地点已为 verified，WHEN consume_intel，THEN 该地点不在 location_advancements 中——亲身经历不可覆盖
- [ ] **AC-7**: GIVEN intel 的 linked_content_ids 为空数组（纯叙事情报），WHEN consume_intel，THEN location_advancements 为空数组——不报错

### Rule 3: Pattern Observation Addition

- [ ] **AC-8**: GIVEN intel 定义 linked_patterns = ["pattern.bird-flight-direction"], pattern_event_id="bird-log-migration"，WHEN consume_intel 且该 event_id 未触发过，THEN pattern_observations 含 1 条目：event_id="bird-log-migration", event_type="log_fragment", added_score=2, new_observation_score=2
- [ ] **AC-9**: GIVEN intel 的 pattern_event_id 已在 triggered_events 中（重复触发），WHEN consume_intel，THEN observation_score 不变，该 pattern 不出现 pattern_observations 中——同一事件仅计首次
- [ ] **AC-10**: GIVEN intel 无 linked_patterns，WHEN consume_intel，THEN pattern_observations 为空数组

### Rule 4: Ability Unlock Check

- [ ] **AC-11**: GIVEN intel 定义 unlock_condition_for_abilities = ["ability.bird-flight-understanding"]，且 Path B 条件已满足（intel 已消耗 + 已有鸟类观测事件），WHEN consume_intel 处理后 rule 4 执行，THEN ability_unlocks 含 1 条目：ability_id="ability.bird-flight-understanding", unlock_path 含 "path_b"
- [ ] **AC-12**: GIVEN intel 关联的能力 Path B 条件不满足（无观测事件），WHEN consume_intel，THEN ability_unlocks 为空数组——能力保持 locked

### Rule 5: Mark Consumed

- [ ] **AC-13**: GIVEN consume_intel 成功执行，WHEN 调用 is_intel_consumed(intel_id)，THEN 返回 true

### ERR_INTEL_NOT_FOUND

- [ ] **AC-14**: GIVEN intel_id 在 Registry 中不存在，WHEN `consume_intel(&"intel.nonexistent")`，THEN 返回 success=false, error_code="ERR_INTEL_NOT_FOUND"，不崩溃

### Triple Effect (Expected)

- [ ] **AC-15**: GIVEN intel 同时含 linked_content_ids + linked_patterns + unlock_condition_for_abilities，且所有条件满足，WHEN consume_intel，THEN IntelConsumeResult 的三个数组全部填充——这是正常且预期的（高信息密度 intel）

---

## Implementation Notes

### IntelConsumeResult Structure

```text
# 返回 Dictionary:
# {
#   success: bool,
#   error_code: StringName,           # "" 或 "ERR_INTEL_ALREADY_CONSUMED" / "ERR_INTEL_NOT_FOUND"
#   intel_id: StringName,
#   intel_display_name: String,
#   location_advancements: Array[Dictionary],
#       # [{location_id: StringName, previous_state: int, new_state: int}]
#   ability_unlocks: Array[Dictionary],
#       # [{ability_id: StringName, ability_display_name: String, unlock_path: StringName}]
#   pattern_observations: Array[Dictionary]
#       # [{pattern_id: StringName, event_id: StringName, event_type: StringName,
#       #   added_score: int, new_observation_score: int,
#       #   previous_pattern_state: int, new_pattern_state: int}]
# }
```

### Core Algorithm

```text
func consume_intel(intel_id: StringName) -> Dictionary:
    var result: Dictionary = {
        "success": false,
        "error_code": "",
        "intel_id": intel_id,
        "intel_display_name": "",
        "location_advancements": [],
        "ability_unlocks": [],
        "pattern_observations": [],
    }

    # Rule 1: 已消耗检查
    if intel_id in consumed_intel_ids:
        result["error_code"] = "ERR_INTEL_ALREADY_CONSUMED"
        return result

    # 验证 intel 存在于 Registry
    var intel_def: Dictionary = _registry.lookup_intel(intel_id)
    if intel_def.is_empty():
        result["error_code"] = "ERR_INTEL_NOT_FOUND"
        return result

    result["success"] = true
    result["intel_display_name"] = intel_def.get("display_name", "")

    # Rule 2: 推进关联地点知识
    for location_id in intel_def.get("linked_content_ids", []):
        var advancement: Dictionary = _advance_location_knowledge(location_id)
        if not advancement.is_empty():
            result["location_advancements"].append(advancement)

    # Rule 3: 对关联规律添加 log_fragment 观测事件
    for pattern_id in intel_def.get("linked_patterns", []):
        var event_id: StringName = intel_def.get("pattern_event_id", "")
        if event_id == "":
            continue

        var ps: Dictionary = _get_or_init_pattern(pattern_id)
        if event_id in ps["triggered_events"]:
            continue  # 去重

        var old_score: int = ps["observation_score"]
        var old_state: int = _compute_pattern_state(old_score, pattern_id)

        ps["triggered_events"].append(event_id)
        ps["observation_score"] = old_score + WEIGHT_LOG_FRAGMENT

        var new_score: int = ps["observation_score"]
        var new_state: int = _compute_pattern_state(new_score, pattern_id)

        result["pattern_observations"].append({
            "pattern_id": pattern_id,
            "event_id": event_id,
            "event_type": "log_fragment",
            "added_score": WEIGHT_LOG_FRAGMENT,
            "new_observation_score": new_score,
            "previous_pattern_state": old_state,
            "new_pattern_state": new_state,
        })

        pattern_observed.emit(pattern_id, event_id, new_score)
        if new_state != old_state:
            pattern_state_changed.emit(pattern_id, old_state, new_state)

    # Rule 4: 检查能力解锁条件
    for ability_id in intel_def.get("unlock_condition_for_abilities", []):
        if ability_state.get(ability_id, ABILITY_LOCKED) == ABILITY_LOCKED:
            if check_unlock_conditions(ability_id):
                var ability_display_name: String = _registry.get_ability_display_name(ability_id)
                result["ability_unlocks"].append({
                    "ability_id": ability_id,
                    "ability_display_name": ability_display_name,
                    "unlock_path": _get_unlock_path_used(ability_id),
                })

    # Rule 5: 标记 intel 已消耗
    consumed_intel_ids.append(intel_id)

    # Emit 完成信号
    intel_consumed.emit(intel_id)

    return result
```

### is_intel_consumed Query

```text
func is_intel_consumed(intel_id: StringName) -> bool:
    return intel_id in consumed_intel_ids
```

### Registry Integration Note

`lookup_intel()` 依赖 Registry 提供静态 intel 定义。IntelManager 在 Phase 3 (core_data_ready) 从 Registry 加载 intel 定义缓存:

```text
var _intel_def_cache: Dictionary = {}  # Dict[StringName, Dictionary]

func _cache_intel_defs() -> void:
    for intel_id in _registry.get_all_intel_ids():
        _intel_def_cache[intel_id] = _registry.lookup_intel(intel_id)
```

---

## Out of Scope

- 情报物品的持有和消耗 UI——属于 ResourcesManager
- 消耗前检查 `is_intel_consumed()` 的调用方职责——ResourcesManager 应在允许消耗 UI 前检查
- 情报物品的获取和放置——属于探索系统和资源系统
- IntelConsumeResult 的 UI 动画展示——属于 UI 系统

---

## QA Test Cases

- **AC-2 and AC-3**: Already consumed
  - Given: consumed_intel_ids = ["intel.bird-migration-notes"]
  - When: consume_intel("intel.bird-migration-notes")
  - Then: success=false, error_code="ERR_INTEL_ALREADY_CONSUMED", all arrays empty
  - Verify: consumed_intel_ids still has exactly 1 entry, knowledge_state unchanged, observation_score unchanged

- **AC-15**: Triple effect
  - Given: intel with linked_content_ids=[loc_a], linked_patterns=[pattern_x], unlock_condition_for_abilities=[ability_y]
  - When: All preconditions met → consume_intel
  - Then: location_advancements=[loc_a advancement], pattern_observations=[log_fragment observation], ability_unlocks=[ability_y unlock]

- **AC-14**: ERR_INTEL_NOT_FOUND
  - Given: intel_id = "intel.nonexistent" 不在 Registry 中
  - When: consume_intel(...)
  - Then: success=false, error_code="ERR_INTEL_NOT_FOUND", no crash

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/intel/IntelConsumeAlgorithmTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (pattern observation), Story 002 (location knowledge advancement), Story 003 (ability unlock check), content-registry Epic (Registry lookup_intel)
- Unlocks: Story 005 (consume_intel is called by ResourcesManager), Story 007 (intel_consumed signal)
