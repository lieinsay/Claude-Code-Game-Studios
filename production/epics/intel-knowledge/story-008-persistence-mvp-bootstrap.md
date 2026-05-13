# Story 008: Persistence & MVP Bootstrap

> **Epic**: Intel / Knowledge System
> **Status**: Done
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-001`, `TR-intel-002`, `TR-intel-003`

**ADR Governing Implementation**: ADR-0007 (IntelManager), ADR-0003 (Save System / JSON Serialization), ADR-0001 (Autoload Boot Order)
**ADR Decision Summary**: IntelManager 在 Phase 3 (core_data_ready) 注册 domain serializer "intel" 到 Persistence。progress.intel 快照包含 7 个字段：knowledge_state, pattern_state, ability_state, consumed_intel_ids, rumor_sources, fog_traversal_count, active_crew。起始状态：1 identified route (route.sky-reef-arc-01) + 1 rumored route (route.high-risk-mvp) + 1 identified location (location.glass-harbor) + 所有 patterns undiscovered + 所有 abilities locked。存档恢复时对 consumed_intel_ids 与 Registry 交叉验证——不存在的 ID 保留但标记 warning。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `JSON.stringify()` 对 Dictionary[StringName] 自动转换 StringName → String；反序列化需显式 `StringName(str)`。Godot 4.6 JSON 不支持 Array 内 StringName——需在反序列化后将 string key 转回 StringName。

**Control Manifest Rules (Core layer)**:
- Required: 快照仅含状态数据（int + Array[StringName] + Dictionary[StringName, int]）——不含 Object/Node/Resource 引用
- Forbidden: bare_dictionary_payload（快照中含 Object 引用）；跳过序列化一步的"快速路径"
- Guardrail: 存档恢复时交叉验证——不存在的 ID 保留原始数据不静默删除；查询不存在的 ID 返回安全默认值

---

## Acceptance Criteria

### Snapshot Serialization (Save)

- [ ] **AC-1**: GIVEN knowledge_state 含 3 个地点（identified=2, rumored=1）, pattern_state 含 1 条规律 (score=7, triggered_events=[...], pattern_usage_success=false), ability_state 全 locked，WHEN `_serialize_intel()`，THEN 返回 Dictionary 含 domain_id="intel", knowledge_state, pattern_state, ability_state, consumed_intel_ids, rumor_sources, fog_traversal_count, active_crew——7 个字段齐全
- [ ] **AC-2**: GIVEN 快照 payload，WHEN 检查内容，THEN 不含 Object/Node/Resource 引用——仅 int, StringName/string, Array, Dictionary

### Snapshot Deserialization (Load)

- [ ] **AC-3**: GIVEN 有效快照（1 confirmed pattern + 2 verified locations + 1 unlocked ability + 3 consumed intel + fog_traversal_count=2），WHEN `_deserialize_intel(snapshot)`，THEN 所有 7 个字段恢复到保存时状态
- [ ] **AC-4**: GIVEN 快照中 consumed_intel_ids 含 "intel.legacy-removed"（不在当前 Registry 中），WHEN 反序列化，THEN 该 ID 保留在 consumed_intel_ids 中（不静默删除），`is_intel_consumed("intel.legacy-removed")` 返回 true，记录 migration warning

### Reset for New Game

- [ ] **AC-5**: GIVEN `_init_new_game_state()`，WHEN 初始化完成，THEN:
  - knowledge_state: route.sky-reef-arc-01=IDENTIFIED, route.high-risk-mvp=RUMORED, location.glass-harbor=IDENTIFIED, 其他地点默认 UNKNOWN
  - pattern_state: 3 条规律全 UNDISCOVERED, observation_score=0, triggered_events=[], pattern_usage_success=false
  - ability_state: 3 条能力全 LOCKED
  - consumed_intel_ids: []
  - rumor_sources: {}
  - fog_traversal_count: 0
  - active_crew: []
- [ ] **AC-6**: GIVEN `_init_new_game_state()` 被调用前有旧数据，WHEN 初始化，THEN 所有旧状态被清除——全新起始状态

### Domain Serializer Registration

- [ ] **AC-7**: GIVEN Phase 3 core_data_ready，WHEN IntelManager 初始化，THEN `Persistence.register_domain_serializer("intel", _serialize_intel, _deserialize_intel)` 被调用——Persistence 需要在 IntelManager 之前 ready（Phase 2）

### Version Migration

- [ ] **AC-8**: GIVEN 旧存档中 knowledge_state 的 key 为 string 类型（非 StringName），WHEN 反序列化，THEN 所有 key 转换回 StringName——查询正常
- [ ] **AC-9**: GIVEN 旧存档中 pattern_state 含当前 Registry 中不存在的 pattern_id，WHEN 反序列化，THEN 该 pattern 数据保留在 pattern_state 中（不静默删除），query_pattern_state() 返回 UNDISCOVERED 默认值，记录 migration warning

### Round-Trip Fidelity

- [ ] **AC-10**: GIVEN 任意合法状态组合，WHEN serialized → _pools cleared → deserialized → query 所有接口，THEN 所有查询返回与存档前完全一致的值
- [ ] **AC-11**: GIVEN 读档后 confirmed 规律，WHEN query_pattern_state()，THEN is_confirmed_plus 正确反映存档前的状态（usage_success 持久化正确）
- [ ] **AC-12**: GIVEN 读档后 verified 地点，WHEN query_knowledge_state()，THEN personal_notes 保留

---

## Implementation Notes

### Domain Serializer Registration

```text
func _on_core_data_ready() -> void:
    # Phase 3 — Persistence (Phase 2) 已 ready
    Persistence.register_domain_serializer("intel", _serialize_intel, _deserialize_intel)

    # 从存档恢复或初始化新游戏
    if Persistence.has_saved_game():
        var snapshot: Dictionary = Persistence.load_domain("intel")
        _deserialize_intel(snapshot)
    else:
        _init_new_game_state()
```

### Serialize Intel

```text
func _serialize_intel() -> Dictionary:
    return {
        "domain_id": "intel",
        "knowledge_state": knowledge_state,      # Dict[StringName, int]
        "pattern_state": pattern_state,          # Dict[StringName, Dict]
        "ability_state": ability_state,          # Dict[StringName, int]
        "consumed_intel_ids": consumed_intel_ids,  # Array[StringName]
        "rumor_sources": rumor_sources,           # Dict[StringName, Array]
        "fog_traversal_count": fog_traversal_count,  # int
        "active_crew": active_crew,               # Array[StringName]
    }
```

### Deserialize Intel

```text
func _deserialize_intel(snapshot: Dictionary) -> void:
    # 清空所有状态
    _clear_all_state()

    # 恢复 knowledge_state — key 需显式转为 StringName
    var raw_ks: Dictionary = snapshot.get("knowledge_state", {})
    knowledge_state.clear()
    for key in raw_ks:
        knowledge_state[StringName(key)] = raw_ks[key]

    # 恢复 pattern_state
    var raw_ps: Dictionary = snapshot.get("pattern_state", {})
    pattern_state.clear()
    for key in raw_ps:
        pattern_state[StringName(key)] = raw_ps[key]

    # 恢复 ability_state
    var raw_as: Dictionary = snapshot.get("ability_state", {})
    ability_state.clear()
    for key in raw_as:
        ability_state[StringName(key)] = raw_as[key]

    # 恢复 consumed_intel_ids — string → StringName
    consumed_intel_ids.clear()
    for intel_id in snapshot.get("consumed_intel_ids", []):
        var sid: StringName = StringName(intel_id)
        consumed_intel_ids.append(sid)
        # 交叉验证
        if not _registry.has_intel(sid):
            push_warning("Migration: consumed intel_id '%s' not in current Registry — retained" % sid)

    # 恢复 rumor_sources
    var raw_rs: Dictionary = snapshot.get("rumor_sources", {})
    rumor_sources.clear()
    for key in raw_rs:
        rumor_sources[StringName(key)] = raw_rs[key]

    # 恢复简单值
    fog_traversal_count = snapshot.get("fog_traversal_count", 0)

    active_crew.clear()
    for partner_id in snapshot.get("active_crew", []):
        active_crew.append(StringName(partner_id))

    # 恢复后触发必要的信号（已解锁的能力 emit ability_unlocked）
    for ability_id in ability_state:
        if ability_state[ability_id] == ABILITY_UNLOCKED:
            # 获取解锁路径（从存档中无法精确恢复路径名——使用通用标记）
            ability_unlocked.emit(ability_id, "restored_from_save")

func _clear_all_state() -> void:
    knowledge_state.clear()
    pattern_state.clear()
    ability_state.clear()
    consumed_intel_ids.clear()
    rumor_sources.clear()
    fog_traversal_count = 0
    active_crew.clear()
    personal_notes.clear()
    _completed_repairs.clear()
```

### MVP Starting State

```text
func _init_new_game_state() -> void:
    _clear_all_state()

    # 地点知识起始状态
    knowledge_state[&"route.sky-reef-arc-01"] = KNOWLEDGE_IDENTIFIED
    knowledge_state[&"route.high-risk-mvp"] = KNOWLEDGE_RUMORED
    knowledge_state[&"location.glass-harbor"] = KNOWLEDGE_IDENTIFIED

    # 为 rumored route 添加初始传闻来源
    rumor_sources[&"route.high-risk-mvp"] = [{
        "source_tag": &"port-rumor",
        "hazard_tags": [],  # 具体风险标签来自 Registry
        "confidence": 35,
    }]

    # 为 identified route 添加来源标注
    rumor_sources[&"route.sky-reef-arc-01"] = [{
        "source_tag": &"skyport-basic-chart",
        "hazard_tags": [],  # 由 Registry 提供
        "confidence": 80,   # 权威——航图是可靠来源
    }]

    rumor_sources[&"location.glass-harbor"] = [{
        "source_tag": &"home-port-familiarity",
        "hazard_tags": [],
        "confidence": 95,
    }]

    # 规律状态全为 UNDISCOVERED（默认——无需显式设置）
    # 能力状态全为 LOCKED（默认——无需显式设置）
    # consumed_intel_ids, fog_traversal_count, active_crew 默认为空/0
```

### StringName ↔ String Conversion Helper

```text
# JSON 持久化 StringName key 的辅助方法
static func _dict_keys_to_stringname(d: Dictionary) -> Dictionary:
    var result: Dictionary = {}
    for key in d:
        result[StringName(key)] = d[key]
    return result

static func _array_to_stringname_array(arr: Array) -> Array:
    var result: Array = []
    for item in arr:
        result.append(StringName(item))
    return result
```

---

## Out of Scope

- Persistence 的保存调度和触发时机（local-save-persistence Epic 拥有）
- JSON 文件 I/O——属于 Persistence 系统
- 存档格式的加密/校验——属于 Persistence 系统
- 跨版本存档迁移的完整策略（仅处理本系统范围内的 ID 交叉验证）
- Post-MVP 伙伴的起始状态（仅 MVP 的 sky-cat 实体在 active_crew 初始为空）

---

## QA Test Cases

- **AC-10 and AC-11**: Round-trip fidelity
  - Given: 设置所有状态为非默认值（2 patterns confirmed, 1 confirmed+, 3 locations verified, 2 abilities unlocked, 4 intel consumed, fog_traversal_count=2, active_crew=["partner.sky-cat"]）
  - When: serialized → clear all → deserialized → query all interfaces
  - Then: 所有查询值与存档前完全一致——包括 is_confirmed_plus, personal_notes, rumor_sources
  - Edge case: 空状态 (new game) → serialized → deserialized → 所有查询返回 new game 默认值

- **AC-5**: New game state
  - Given: _init_new_game_state() called
  - When: query all interfaces
  - Then: 3 initial locations/states correct, all patterns undiscovered, all abilities locked, consumed_intel_ids=[], fog_traversal_count=0
  - Edge case: 对 route.sky-reef-arc-01 调用 query_route_knowledge → state=IDENTIFIED, all hazards visible

- **AC-4**: Migration — stale intel ID
  - Given: 快照 consumed_intel_ids=["intel.bird-migration-notes", "intel.legacy-removed"]
  - When: 反序列化（假设 "intel.legacy-removed" 不在当前 Registry 中）
  - Then: consumed_intel_ids 保留两个 ID, is_intel_consumed("intel.legacy-removed")=true, migration warning logged

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/intel/persistence/PersistenceIntegrationTest.csproj` — must exist and pass
**Status**: [x] 43/43 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001-007 (all IntelManager state), local-save-persistence Epic (Persistence.register_domain_serializer), content-registry Epic (Registry.has_intel)
- Unlocks: Full system ready for Production — IntelManager can be initialized in real game sessions
