# Story 006: Downstream Query Interface

> **Epic**: Intel / Knowledge System
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-001`, `TR-intel-002`, `TR-intel-003`

**ADR Governing Implementation**: ADR-0007 (IntelManager Autoload #6)
**ADR Decision Summary**: 9 个下游只读查询方法供 6 个下游系统（Chart, Navigation, Exploration, WorldRepair, Partner, UIManager）使用。所有查询为 O(1) Dictionary 查找——无副作用，不修改状态。查询不存在的 ID 返回安全的默认值（UNKNOWN/LOCKED/空）而非报错——下游系统不应因查询未注册 ID 而崩溃。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 所有查询方法为纯函数——只读、无副作用
- Forbidden: 查询方法修改任何状态；下游系统缓存查询结果作为"真相源"
- Guardrail: 查询不存在的 ID 返回安全默认值（不崩溃、不报错）

---

## Acceptance Criteria

### query_knowledge_state()

- [ ] **AC-1**: GIVEN location 状态为 RUMORED（confidence=40 的 1 个来源），WHEN `query_knowledge_state(location_id)`，THEN 返回 `{state: 1, rumor_sources: [{source_tag, hazard_tags, confidence: 40}], verified: false, personal_notes: ""}`
- [ ] **AC-2**: GIVEN location 状态为 VERIFIED，WHEN `query_knowledge_state(location_id)`，THEN 返回 verified=true, personal_notes 含用户标注内容
- [ ] **AC-3**: GIVEN 未初始化 location_id，WHEN `query_knowledge_state()`，THEN 返回 state=UNKNOWN (0), rumor_sources=[], verified=false——安全默认值

### query_route_knowledge()

- [ ] **AC-4**: GIVEN route 状态为 IDENTIFIED，WHEN `query_route_knowledge(route_id)`，THEN 返回 state=2, visible_hazards 含所有静态风险标签, hidden_hazard_count=0, sources 含 intel 来源
- [ ] **AC-5**: GIVEN route 状态为 RUMORED 且有 2 个冲突来源，WHEN `query_route_knowledge(route_id)`，THEN hidden_hazard_count > 0（部分标签隐藏），sources 含 2 条来源标注

### query_route_accessibility()

- [ ] **AC-6**: GIVEN route 所有风险标签可见且无能力阻塞，WHEN `query_route_accessibility(route_id)`，THEN 返回 traversable=true
- [ ] **AC-7**: GIVEN route 需要 lighthouse-signal-interpretation 能力但该能力仍 locked，WHEN `query_route_accessibility(route_id)`，THEN 返回 traversable=false, blocked_by_ability="ability.lighthouse-signal-interpretation"

### query_pattern_state()

- [ ] **AC-8**: GIVEN pattern 状态为 confirmed 且 confirmed+，WHEN `query_pattern_state(pattern_id)`，THEN 返回 `{state: 2, observation_score: 14, is_confirmed_plus: true, triggered_events: [...]}`
- [ ] **AC-9**: GIVEN 未初始化 pattern_id，WHEN `query_pattern_state()`，THEN 返回 state=UNDISCOVERED (0), observation_score=0——安全默认值

### query_ability_state()

- [ ] **AC-10**: GIVEN 能力已解锁，WHEN `query_ability_state(ability_id)`，THEN 返回 ABILITY_UNLOCKED (1)
- [ ] **AC-11**: GIVEN 未初始化 ability_id，WHEN `query_ability_state()`，THEN 返回 ABILITY_LOCKED (0)——安全默认值

### get_pattern_log()

- [ ] **AC-12**: GIVEN 3 条规律分别为 undiscovered, partially_observed, confirmed，WHEN `get_pattern_log()`，THEN 返回 2 条目（仅 partially_observed 和 confirmed）——undiscovered 不在列表中
- [ ] **AC-13**: GIVEN 所有规律为 undiscovered，WHEN `get_pattern_log()`，THEN 返回空数组

### get_ability_list()

- [ ] **AC-14**: GIVEN 3 条能力 (2 locked, 1 unlocked)，WHEN `get_ability_list()`，THEN 返回 3 条目——含 ability_id, display_name, state (LOCKED/UNLOCKED), unlock_hint
- [ ] **AC-15**: GIVEN locked 能力，WHEN `get_ability_list()`，THEN unlock_hint 为对应路径提示文本（如"据说老港务长有一本信号手册……"）

### query_location_discovery()

- [ ] **AC-16**: GIVEN location 状态为 IDENTIFIED，WHEN `query_location_discovery(location_id)`，THEN 返回 state=2, hazard_visibility 数组含所有静态风险标签的可见性, sources 含 intel 来源

---

## Implementation Notes

### Query Method Implementations

```text
# 1. 地点知识查询
func query_knowledge_state(location_id: StringName) -> Dictionary:
    return {
        "state": knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN),
        "rumor_sources": rumor_sources.get(location_id, []),
        "verified": knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN) == KNOWLEDGE_VERIFIED,
        "personal_notes": personal_notes.get(location_id, ""),
    }

# 2. 路线知识查询（聚合）
func query_route_knowledge(route_id: StringName) -> Dictionary:
    var state: int = knowledge_state.get(route_id, KNOWLEDGE_UNKNOWN)
    var route_def: Dictionary = _registry.lookup_route(route_id)
    var all_hazards: Array = route_def.get("hazard_tags", [])

    var visible_hazards: Array = []
    var hidden_hazard_count: int = 0

    if state >= KNOWLEDGE_IDENTIFIED:
        visible_hazards = all_hazards.duplicate()
        hidden_hazard_count = 0
    elif state == KNOWLEDGE_RUMORED:
        # 收集所有来源的风险标签（冲突来源并排）
        var revealed_tags: Array = []
        for source in rumor_sources.get(route_id, []):
            for tag in source.get("hazard_tags", []):
                if tag not in revealed_tags:
                    revealed_tags.append(tag)
        visible_hazards = revealed_tags
        hidden_hazard_count = all_hazards.size() - revealed_tags.size()
    # else UNKNOWN → visible=[], hidden=all

    return {
        "state": state,
        "visible_hazards": visible_hazards,
        "hidden_hazard_count": maxi(0, hidden_hazard_count),
        "sources": _collect_sources(route_id),
    }

# 3. 路线可通行性查询
func query_route_accessibility(route_id: StringName) -> Dictionary:
    var route_def: Dictionary = _registry.lookup_route(route_id)
    var required_ability: StringName = route_def.get("required_ability", "")

    if required_ability != "":
        if ability_state.get(required_ability, ABILITY_LOCKED) == ABILITY_LOCKED:
            return {
                "traversable": false,
                "blocked_by_ability": required_ability,
                "blocked_by_knowledge": false,
            }

    # 检查是否有知识阻塞
    var state: int = knowledge_state.get(route_id, KNOWLEDGE_UNKNOWN)
    var blocked_by_knowledge: bool = state == KNOWLEDGE_UNKNOWN

    return {
        "traversable": not blocked_by_knowledge,
        "blocked_by_ability": "",
        "blocked_by_knowledge": blocked_by_knowledge,
    }

# 4. 规律状态查询
func query_pattern_state(pattern_id: StringName) -> Dictionary:
    var ps: Dictionary = pattern_state.get(pattern_id, {})
    var score: int = ps.get("observation_score", 0)
    return {
        "state": _compute_pattern_state(score, pattern_id),
        "observation_score": score,
        "is_confirmed_plus": is_confirmed_plus(pattern_id),
        "triggered_events": ps.get("triggered_events", []),
    }

# 5. 能力状态查询
func query_ability_state(ability_id: StringName) -> int:
    return ability_state.get(ability_id, ABILITY_LOCKED)

# 6. 情报已消耗检查
func is_intel_consumed(intel_id: StringName) -> bool:
    return intel_id in consumed_intel_ids

# 7. 图鉴日志
func get_pattern_log() -> Array:
    var log: Array = []
    for pattern_id in pattern_state:
        var ps: Dictionary = pattern_state[pattern_id]
        var state: int = _compute_pattern_state(ps.get("observation_score", 0), pattern_id)
        if state >= PATTERN_PARTIALLY_OBSERVED:
            var pattern_def: Dictionary = _registry.lookup_pattern(pattern_id)
            log.append({
                "pattern_id": pattern_id,
                "display_name": pattern_def.get("display_name", str(pattern_id)),
                "state": state,
                "observation_score": ps.get("observation_score", 0),
                "is_confirmed_plus": is_confirmed_plus(pattern_id),
                "hint_text": pattern_def.get("hint_text", "") if state == PATTERN_PARTIALLY_OBSERVED else "",
                "description": pattern_def.get("description", "") if state >= PATTERN_CONFIRMED else "",
                "triggered_events": ps.get("triggered_events", []),
            })
    return log

# 8. 能力列表
func get_ability_list() -> Array:
    var list: Array = []
    for ability_id in ability_unlock_paths:
        var ability_def: Dictionary = _registry.lookup_ability(ability_id)
        var state: int = ability_state.get(ability_id, ABILITY_LOCKED)
        list.append({
            "ability_id": ability_id,
            "display_name": ability_def.get("display_name", str(ability_id)),
            "state": state,
            "unlock_hint": ability_def.get("unlock_hint", "") if state == ABILITY_LOCKED else "",
            "description": ability_def.get("description", "") if state == ABILITY_UNLOCKED else "",
        })
    return list

# 9. 地点发现状态
func query_location_discovery(location_id: StringName) -> Dictionary:
    var state: int = knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN)
    var loc_def: Dictionary = _registry.lookup_location(location_id)
    var all_hazards: Array = loc_def.get("hazard_tags", [])

    var hazard_visibility: Array = []
    if state >= KNOWLEDGE_IDENTIFIED:
        for tag in all_hazards:
            hazard_visibility.append({"tag": tag, "visible": true})
    elif state == KNOWLEDGE_RUMORED:
        var revealed: Array = []
        for source in rumor_sources.get(location_id, []):
            for tag in source.get("hazard_tags", []):
                if tag not in revealed:
                    revealed.append(tag)
        for tag in all_hazards:
            hazard_visibility.append({"tag": tag, "visible": tag in revealed})
    # else UNKNOWN → hazard_visibility 为空

    return {
        "state": state,
        "hazard_visibility": hazard_visibility,
        "sources": _collect_sources(location_id),
        "personal_notes": personal_notes.get(location_id, ""),
    }

# Helper
func _collect_sources(entity_id: StringName) -> Array:
    var sources: Array = []
    for source in rumor_sources.get(entity_id, []):
        sources.append({
            "source_tag": source["source_tag"],
            "confidence": source["confidence"],
            "confidence_label": _confidence_to_label(source["confidence"]),
        })
    return sources
```

### Default-Value Guarantee

所有查询对不存在的 ID 返回安全默认值，不使用 `assert()` 或 `push_error()`:

| 查询 | 不存在的 ID 返回值 |
|------|-------------------|
| query_knowledge_state() | state=UNKNOWN, rumor_sources=[], verified=false |
| query_pattern_state() | state=UNDISCOVERED, score=0, is_confirmed_plus=false |
| query_ability_state() | ABILITY_LOCKED |
| is_intel_consumed() | false |
| query_route_knowledge() | state=UNKNOWN, visible_hazards=[], hidden_hazard_count=all |
| query_route_accessibility() | traversable=false, blocked_by_knowledge=true |
| query_location_discovery() | state=UNKNOWN, hazard_visibility=[] |

---

## Out of Scope

- 航图 UI 渲染（如何使用查询结果绘制航线、地点图标、风险标签）——属于 UI 系统
- 查询结果的缓存策略——下游系统自行决定是否缓存（但必须以本系统为真相源）
- Registry 的 lookup_*() 方法实现——属于 content-registry Epic

---

## QA Test Cases

- **AC-1 and AC-3**: query_knowledge_state
  - Given: location_a = RUMORED (source: old-harbormaster, confidence=55), location_b 从未初始化
  - When: query_knowledge_state(location_a) → state=RUMORED, rumor_sources=[{...}], verified=false
  - When: query_knowledge_state(location_b) → state=UNKNOWN, rumor_sources=[], verified=false

- **AC-12 and AC-13**: get_pattern_log
  - Given: bird-flight=confirmed, lighthouse=partially_observed, fog=undiscovered
  - When: get_pattern_log()
  - Then: 返回 2 条目——bird-flight (hint_text="", description 完整) + lighthouse (hint_text 含模糊提示, description="")
  - Edge case: 所有规律 undiscovered → 返回 []

- **AC-14 and AC-15**: get_ability_list
  - Given: bird-flight=unlocked, lighthouse=locked, fog=locked
  - When: get_ability_list()
  - Then: bird-flight 条目 state=UNLOCKED (1), description 填充; lighthouse 条目 state=LOCKED (0), unlock_hint 含路径提示

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/intel/query/QueryInterfaceTest.csproj` — must exist and pass
**Status**: [x] 44/44 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001-004 (all state data), content-registry Epic (Registry lookup methods)
- Unlocks: All downstream systems (Chart, Navigation, Exploration, WorldRepair, Partner, UIManager) can integrate
