# Story 002: Location Knowledge State Machine & Rumor System

> **Epic**: Intel / Knowledge System
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-001`

**ADR Governing Implementation**: ADR-0007 (IntelManager Autoload #6)
**ADR Decision Summary**: 地点知识 4 级状态机 (unknown→rumored→identified→verified)。所有运行时状态以 Dictionary[StringName, int] 存储。reveal_rumor() 根据 confidence 决定转换：confidence < 67 → unknown→rumored；confidence ≥ 67 → unknown→identified 或 rumored→identified。player_arrived_at() 将任意状态推进至 verified（含 unknown→verified 开拓者路径）。传闻冲突时多来源保留，各来源风险标签独立显示并标注来源名称和置信度。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: knowledge_state 为唯一真相源——航图/航行/探索系统只读查询，不得自行缓存
- Forbidden: verified 状态被任何写入降级；identified 退回 rumored/unknown；rumored 退回 unknown
- Guardrail: 同一来源重复写入同一实体传闻不追加重复标注

---

## Acceptance Criteria

### State Machine Basics

- [ ] **AC-1**: GIVEN 新游戏开始，WHEN `query_knowledge_state(&"route.sky-reef-arc-01")`，THEN 返回 state=IDENTIFIED，sources 含"空港基础航图"
- [ ] **AC-2**: GIVEN 新游戏开始，WHEN `query_knowledge_state(&"route.high-risk-mvp")`，THEN 返回 state=RUMORED，部分风险标签显示，"?" 替代隐藏标签
- [ ] **AC-3**: GIVEN 新游戏开始，WHEN `query_knowledge_state(&"location.glass-harbor")`，THEN 返回 state=IDENTIFIED
- [ ] **AC-4**: GIVEN 任意未初始化的地点 ID（非初始 3 个），WHEN `query_knowledge_state()`，THEN 返回 state=UNKNOWN（默认值，实体在航图中不可见）

### reveal_rumor() — Low Confidence

- [ ] **AC-5**: GIVEN location 状态为 UNKNOWN，WHEN `reveal_rumor(location_id, source_tag, hazard_tags, confidence=40)`，THEN 状态变为 RUMORED，航图显示虚线轮廓 + 部分风险标签 + 来源标注（confidence 40 → 显示 "可靠"）
- [ ] **AC-6**: GIVEN location 状态为 UNKNOWN，WHEN `reveal_rumor(location_id, source_tag, hazard_tags, confidence=25)`，THEN 状态变为 RUMORED，来源标注显示 confidence 25 → "不确定"

### reveal_rumor() — High Confidence (Authority)

- [ ] **AC-7**: GIVEN location 状态为 UNKNOWN，WHEN `reveal_rumor(location_id, source_tag, hazard_tags, confidence=75)` (≥67)，THEN 状态直接变为 IDENTIFIED（跳过 rumored），实体完全可见，所有静态风险标签显示
- [ ] **AC-8**: GIVEN location 状态为 RUMORED，WHEN `reveal_rumor(location_id, source_tag, hazard_tags, confidence=80)` (≥67)，THEN 状态变为 IDENTIFIED，传闻来源标注保留，风险标签被可靠情报替换

### consume_intel() → Location Advancement

- [ ] **AC-9**: GIVEN location 状态为 UNKNOWN，WHEN `consume_intel()` 关联该 location，THEN 状态变为 IDENTIFIED
- [ ] **AC-10**: GIVEN location 状态为 RUMORED，WHEN `consume_intel()` 关联该 location，THEN 状态变为 IDENTIFIED——intel 以可靠信息覆盖传闻
- [ ] **AC-11**: GIVEN location 状态为 IDENTIFIED，WHEN `consume_intel()` 关联该 location，THEN 状态保持 IDENTIFIED（已达此级别）
- [ ] **AC-12**: GIVEN location 状态为 VERIFIED，WHEN `consume_intel()` 关联该 location，THEN 状态保持 VERIFIED（亲身经历不可覆盖）

### player_arrived_at()

- [ ] **AC-13**: GIVEN location 状态为 IDENTIFIED，WHEN `player_arrived_at(location_id)`，THEN 状态变为 VERIFIED，来源标注为"亲身探索"
- [ ] **AC-14**: GIVEN location 状态为 UNKNOWN（开拓者路径），WHEN `player_arrived_at(location_id)`，THEN 状态从 UNKNOWN 直接跳转 VERIFIED——跳过 rumored 和 identified
- [ ] **AC-15**: GIVEN location 已为 VERIFIED，WHEN `player_arrived_at(location_id)` 再次调用，THEN 状态保持 VERIFIED，不重复触发状态变更信号，个人标注不变

### Non-Degradation

- [ ] **AC-16**: GIVEN location 状态为 VERIFIED，WHEN `reveal_rumor()` 以任意 confidence 调用，THEN 状态保持 VERIFIED——传闻被静默丢弃，风险标签和来源标注不变
- [ ] **AC-17**: GIVEN location 状态为 IDENTIFIED，WHEN `reveal_rumor()` 以低 confidence 调用，THEN 状态不退回 RUMORED
- [ ] **AC-18**: GIVEN location 状态为 RUMORED，WHEN 尝试以某种方式退回 UNKNOWN，THEN 状态保持 RUMORED——不可退回

### Duplicate Rumor Source

- [ ] **AC-19**: GIVEN location 已有 source_tag="old-harbormaster" 的传闻，WHEN `reveal_rumor()` 以相同 source_tag 再次调用，THEN 不追加重复来源标注，返回状态表明"已存在该来源的传闻"

### Confidence Text Mapping

- [ ] **AC-20**: GIVEN confidence 值，WHEN 映射为显示文本，THEN: 0-33 → "不确定", 34-66 → "可靠", 67-100 → "权威"

---

## Implementation Notes

### Data Structures

```text
# 地点知识状态: StringName → int (enum)
const KNOWLEDGE_UNKNOWN: int = 0
const KNOWLEDGE_RUMORED: int = 1
const KNOWLEDGE_IDENTIFIED: int = 2
const KNOWLEDGE_VERIFIED: int = 3

var knowledge_state: Dictionary = {}  # Dictionary[StringName, int]

# 传闻来源: StringName → Array[Dictionary]
# 每个 Dictionary: {source_tag: StringName, hazard_tags: Array, confidence: int}
var rumor_sources: Dictionary = {}  # per-location, Dict[StringName, Array]

# 个人标注: StringName → String
var personal_notes: Dictionary = {}
```

### Core Method: reveal_rumor()

```text
func reveal_rumor(location_id: StringName, source_tag: StringName, hazard_tags: Array, confidence: int) -> void:
    var current_state: int = knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN)

    # verified 终态——传闻被拒绝
    if current_state == KNOWLEDGE_VERIFIED:
        return

    # 检测重复来源
    var sources: Array = rumor_sources.get(location_id, [])
    for s in sources:
        if s.get("source_tag") == source_tag:
            return  # 已存在该来源，不追加

    # 置信度钳制 [0, 100]
    confidence = clampi(confidence, 0, 100)

    # 添加来源记录
    sources.append({
        "source_tag": source_tag,
        "hazard_tags": hazard_tags,
        "confidence": confidence,
    })
    rumor_sources[location_id] = sources

    # 状态转换
    var target_state: int
    if confidence >= 67:
        # 权威来源 → identified
        target_state = maxi(current_state, KNOWLEDGE_IDENTIFIED)
    else:
        # 普通传闻 → 至少 rumored
        target_state = maxi(current_state, KNOWLEDGE_RUMORED)

    if target_state != current_state:
        var old_state: int = current_state
        knowledge_state[location_id] = target_state
        rumor_received.emit(location_id, source_tag)
        knowledge_advanced.emit(location_id, old_state, target_state)
```

### Core Method: player_arrived_at()

```text
func player_arrived_at(location_id: StringName) -> void:
    var current_state: int = knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN)

    if current_state == KNOWLEDGE_VERIFIED:
        return  # 已亲身验证——不重复触发

    var old_state: int = current_state
    knowledge_state[location_id] = KNOWLEDGE_VERIFIED
    knowledge_advanced.emit(location_id, old_state, KNOWLEDGE_VERIFIED)

    # 验证后调整所有来源置信度
    _adjust_rumor_confidence_on_verification(location_id)
```

### Location Advancement (used by consume_intel)

```text
func _advance_location_knowledge(location_id: StringName) -> Dictionary:
    # 返回 {previous_state: int, new_state: int} 或空（若未推进）
    var current: int = knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN)

    if current >= KNOWLEDGE_IDENTIFIED:
        return {}  # 已达 identified 或 verified——无需推进

    var old_state: int = current
    knowledge_state[location_id] = KNOWLEDGE_IDENTIFIED
    knowledge_advanced.emit(location_id, old_state, KNOWLEDGE_IDENTIFIED)
    return {"location_id": location_id, "previous_state": old_state, "new_state": KNOWLEDGE_IDENTIFIED}
```

### Confidence Text Mapping

```text
func _confidence_to_label(confidence: int) -> String:
    if confidence <= 33:
        return "不确定"
    elif confidence <= 66:
        return "可靠"
    else:
        return "权威"
```

### Non-Degradation Guard

```text
func _can_transition_location(current: int, target: int) -> bool:
    if current == KNOWLEDGE_VERIFIED:
        return false
    if current == KNOWLEDGE_IDENTIFIED and target == KNOWLEDGE_RUMORED:
        return false
    if current == KNOWLEDGE_RUMORED and target == KNOWLEDGE_UNKNOWN:
        return false
    return true
```

### Lazy Initialization for Unknown Locations

```text
func _ensure_location_initialized(location_id: StringName) -> void:
    if not knowledge_state.has(location_id):
        knowledge_state[location_id] = KNOWLEDGE_UNKNOWN
```

---

## Out of Scope

- 航图视觉渲染（虚线轮廓、风险标签图标、来源标注 tooltip）——属于 UI 系统
- confidence 的来源定义和初始值设定——由伙伴系统和 intel 定义（Registry）提供初始 confidence
- 传闻写入的触发时机（伙伴系统决定何时调用 reveal_rumor）
- 航图叠加层的具体颜色、图标、动画——属于 UX/UI 系统

---

## QA Test Cases

- **AC-14**: Pioneer path (unknown → verified)
  - Given: location.uncharted-isle 从未被初始化或写入
  - When: player_arrived_at("location.uncharted-isle")
  - Then: query_knowledge_state() → state=VERIFIED, sources 含"亲身探索"
  - Edge case: 对已 VERIFIED 的地点再次 player_arrived_at → 状态不变，不重复 emit 信号

- **AC-5 through AC-8**: Confidence-based transitions
  - Given: 3 个 UNKNOWN 地点 A/B/C
  - When: reveal_rumor(A, src, tags, 40) → reveal_rumor(B, src, tags, 75) → reveal_rumor(C, src, tags, 25)
  - Then: A=RUMORED, B=IDENTIFIED, C=RUMORED
  - Edge case: confidence=-10 → 钳制到 0; confidence=150 → 钳制到 100

- **AC-19**: Duplicate source
  - Given: location X has rumor from "old-harbormaster" (confidence=55)
  - When: reveal_rumor(X, "old-harbormaster", different_tags, 80) again
  - Then: rumor_sources[X] still has 1 entry (no duplicate), state unchanged

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/intel/location/LocationKnowledgeStateMachineTest.csproj` — must exist and pass
**Status**: [x] 47/47 PASS — 2026-05-13

---

## Dependencies

- Depends on: content-registry Epic (Registry provides location static definitions)
- Unlocks: Story 003 (ability unlock — depends on location_visit_count condition), Story 004 (consume_intel — depends on _advance_location_knowledge), Story 005/006
