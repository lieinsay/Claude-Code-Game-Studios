# Story 001: Pattern Knowledge Observation & State Machine

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
**ADR Decision Summary**: 3 条规律 (pattern.bird-flight-direction, pattern.lighthouse-signals, pattern.fog-navigation) 各拥有 6 个独立观测事件。observation_score = SUM(weight(e)) for unique triggered event IDs。5 种事件权重: narrative_hint=1, log_fragment=2, partner_comment=3, passive_observation=4, active_investigation=7。4 级状态机: undiscovered → partially_observed (≥5) → confirmed (≥10) + confirmed+ 增强层 (confirmed AND pattern_usage_success=true)。同一观测事件仅计首次触发——证据多样性优先于重复次数。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 纯 C# Dictionary[StringName] 存储，无引擎 API 依赖。C# 的 `StringName` 在 Dictionary key 中自动转换 string，反序列化时需 `StringName(str)` 显式转换。

**Control Manifest Rules (Core layer)**:
- Required: observation_score 仅对 triggered_events 集合中的唯一事件 ID 累加；pattern_state 由 IntelManager 唯一拥有
- Forbidden: 下游系统自行缓存 pattern 状态；在 `_process()` 中动态计算 observation_score
- Guardrail: pattern_usage_success 可由系统在分数不足时提前设置，但不激活 confirmed+ 直到分数达标

---

## Acceptance Criteria

### Observation Score Accumulation

- [ ] **AC-1**: GIVEN 玩家首次进入有鸟实体的开放空域，WHEN `report_observation_event(&"pattern.bird-flight-direction", &"bird-narrative-hint")`，THEN observation_score = 1（narrative_hint 权重），事件 ID 记录在 triggered_events 集合中
- [ ] **AC-2**: GIVEN 同一事件 ID 已触发，WHEN 再次调用 `report_observation_event()` 传入相同 event_id，THEN observation_score 不变，triggered_events 集合不增加重复条目
- [ ] **AC-3**: GIVEN pattern 已有 observation_score=3 (bird-narrative-hint=1 + bird-log-migration=2)，WHEN 触发 bird-passive-island (weight=4)，THEN observation_score = 3+4 = 7

### State Transitions

- [ ] **AC-4**: GIVEN observation_score = 3 (< partial_threshold=5)，WHEN `compute_pattern_state()`，THEN 返回 PATTERN_UNDISCOVERED (0)，规律在日志中不可见
- [ ] **AC-5**: GIVEN observation_score = 7 (≥ partial_threshold=5, < confirmation_threshold=10)，WHEN `compute_pattern_state()`，THEN 返回 PATTERN_PARTIALLY_OBSERVED (1)，`pattern_state_changed` 信号 emit
- [ ] **AC-6**: GIVEN observation_score = 11 (≥ confirmation_threshold=10)，WHEN `compute_pattern_state()`，THEN 返回 PATTERN_CONFIRMED (2)，基础机械收益激活；信号 `pattern_state_changed` emit
- [ ] **AC-7**: GIVEN observation_score >= 10 且 pattern_usage_success=false，WHEN `is_confirmed_plus()`，THEN 返回 false——基础收益激活但增强收益未激活
- [ ] **AC-8**: GIVEN observation_score >= 10 且 pattern_usage_success=true，WHEN `is_confirmed_plus()`，THEN 返回 true——增强机械收益激活

### Pattern Usage Success (Early Setting)

- [ ] **AC-9**: GIVEN observation_score = 7 (partially_observed) 且 pattern_usage_success 被设为 true，WHEN `is_confirmed_plus()`，THEN 仍返回 false（confirmed 前置条件未满足）。随后 observation_score 累积至 11，状态变为 confirmed，`is_confirmed_plus()` 自动变为 true——无需再次触发 usage_success

### Undiscovered Pattern Not in Log

- [ ] **AC-10**: GIVEN 规律状态为 PATTERN_UNDISCOVERED，WHEN `get_pattern_log()`，THEN 该规律不出现在返回列表中——保持神秘感

### Threshold Override Per Pattern

- [ ] **AC-11**: GIVEN 某规律的 partial_threshold_override=7, confirmation_threshold_override=14，WHEN observation_score=8（达到覆盖的 partial 但未达覆盖的 confirmation），THEN 状态为 partially_observed

### Invalid State Transitions (Non-Degradation)

- [ ] **AC-12**: GIVEN 规律状态为 confirmed，WHEN observation_score 被尝试降低或状态被尝试回退，THEN 状态保持 confirmed——confirmed → partially_observed 和 confirmed → undiscovered 为无效转换
- [ ] **AC-13**: GIVEN 规律状态为 partially_observed，WHEN 尝试退回 undiscovered，THEN 状态保持 partially_observed——不可退化

---

## Implementation Notes

### Data Structures

```text
# PatternState Dictionary (per pattern_id)
# {
#   observation_score: int,
#   triggered_events: Array[StringName],  # unique event IDs
#   pattern_usage_success: bool
# }
var pattern_state: Dictionary = {}  # Dictionary[StringName, Dictionary]

# State Enums
const PATTERN_UNDISCOVERED: int = 0
const PATTERN_PARTIALLY_OBSERVED: int = 1
const PATTERN_CONFIRMED: int = 2

# Event Type Weights
const WEIGHT_NARRATIVE_HINT: int = 1
const WEIGHT_LOG_FRAGMENT: int = 2
const WEIGHT_PARTNER_COMMENT: int = 3
const WEIGHT_PASSIVE_OBSERVATION: int = 4
const WEIGHT_ACTIVE_INVESTIGATION: int = 7

# Default Thresholds (per-pattern override in _threshold_overrides)
const PARTIAL_THRESHOLD_DEFAULT: int = 5
const CONFIRMATION_THRESHOLD_DEFAULT: int = 10
```

### Core Algorithm: report_observation_event()

```text
func report_observation_event(pattern_id: StringName, event_id: StringName) -> void:
    # 防御性校验
    if not _validate_pattern_id(pattern_id):
        push_warning("unregistered observation event %s for pattern %s" % [event_id, pattern_id])
        return

    # 获取或初始化 pattern state
    var ps: Dictionary = _get_or_init_pattern(pattern_id)

    # 去重检查
    if event_id in ps["triggered_events"]:
        return  # 同一事件仅计一次

    # 获取事件权重
    var weight: int = _get_event_weight(pattern_id, event_id)

    # 累加分数
    var old_score: int = ps["observation_score"]
    var old_state: int = _compute_pattern_state(old_score, pattern_id)
    ps["triggered_events"].append(event_id)
    ps["observation_score"] = old_score + weight

    # 判定新状态
    var new_score: int = ps["observation_score"]
    var new_state: int = _compute_pattern_state(new_score, pattern_id)

    # Emit 观测事件信号
    pattern_observed.emit(pattern_id, event_id, new_score)

    # Emit 状态变更信号（如状态变化）
    if new_state != old_state:
        pattern_state_changed.emit(pattern_id, old_state, new_state)
```

### State Computation

```text
func _compute_pattern_state(score: int, pattern_id: StringName) -> int:
    var partial_threshold: int = _get_threshold(pattern_id, "partial")
    var confirmation_threshold: int = _get_threshold(pattern_id, "confirmation")

    if score >= confirmation_threshold:
        return PATTERN_CONFIRMED
    elif score >= partial_threshold:
        return PATTERN_PARTIALLY_OBSERVED
    else:
        return PATTERN_UNDISCOVERED

func is_confirmed_plus(pattern_id: StringName) -> bool:
    var ps: Dictionary = pattern_state.get(pattern_id, {})
    return (_compute_pattern_state(ps.get("observation_score", 0), pattern_id) == PATTERN_CONFIRMED
            and ps.get("pattern_usage_success", false))
```

### Event Weight Lookup

```text
# 从 Registry 加载的事件权重表，按 pattern_id → event_id → weight 索引
# 若事件不在定义表中（未注册事件），返回 0 并记录 warning
var _event_weight_table: Dictionary = {}  # Dict[StringName, Dict[StringName, int]]

func _get_event_weight(pattern_id: StringName, event_id: StringName) -> int:
    var pattern_events: Dictionary = _event_weight_table.get(pattern_id, {})
    return pattern_events.get(event_id, 0)
```

### Pattern Usage Success

```text
func report_pattern_usage_success(pattern_id: StringName) -> void:
    var ps: Dictionary = _get_or_init_pattern(pattern_id)
    var was_confirmed_plus: bool = is_confirmed_plus(pattern_id)

    ps["pattern_usage_success"] = true

    # 仅当 confirmed 状态已达成时才激活 confirmed+
    if not was_confirmed_plus and is_confirmed_plus(pattern_id):
        pattern_usage_confirmed.emit(pattern_id)
```

### Threshold Override Table

```text
# 按规律覆盖阈值（MVP 全部使用默认值，此机制为调优预留）
var _threshold_overrides: Dictionary = {}  # Dict[StringName, {partial: int, confirmation: int}]

func _get_threshold(pattern_id: StringName, threshold_type: String) -> int:
    var overrides: Dictionary = _threshold_overrides.get(pattern_id, {})
    if threshold_type == "partial":
        return overrides.get("partial", PARTIAL_THRESHOLD_DEFAULT)
    else:
        return overrides.get("confirmation", CONFIRMATION_THRESHOLD_DEFAULT)
```

### Startup Validation

在 `_ready()` 或 `core_data_ready` 初始化时:

```text
func _validate_thresholds() -> void:
    for pattern_id in _event_weight_table:
        var partial: int = _get_threshold(pattern_id, "partial")
        var confirmation: int = _get_threshold(pattern_id, "confirmation")
        if partial >= confirmation:
            push_error("Pattern %s: partial_threshold (%d) >= confirmation_threshold (%d) — partially_observed state unreachable"
                       % [pattern_id, partial, confirmation])
```

---

## Out of Scope

- 观测事件的具体触发条件判定（属于探索/航行/伙伴系统）
- UI 渲染——图鉴日志、航图叠加层（属于 UI 系统）
- pattern_usage_success 的具体判定逻辑（探索/航行系统拥有判定条件，本 Story 仅接收 `report_pattern_usage_success()` 调用）
- confirmed vs confirmed+ 的机械收益激活（收益激活由下游系统在收到 `pattern_state_changed` / `pattern_usage_confirmed` 信号后自行实现）

---

## QA Test Cases

- **AC-1 through AC-3**: Observation score accumulation and dedup
  - Given: 空 pattern_state
  - When: report_observation_event("pattern.bird-flight-direction", "bird-narrative-hint") × 3 + report_observation_event("pattern.bird-flight-direction", "bird-log-migration")
  - Then: observation_score = 1 + 2 = 3, triggered_events = ["bird-narrative-hint", "bird-log-migration"]
  - Edge case: 传入空 event_id "" → warning log, 不崩溃, triggered_events 不变

- **AC-5**: partially_observed transition
  - Given: observation_score = 3 (2 events: narrative_hint=1 + log=2)
  - When: report_observation_event("pattern.bird-flight-direction", "bird-passive-island") (weight=4)
  - Then: observation_score=7, state=partially_observed, pattern_state_changed signal emitted with (UNDISCOVERED→PARTIALLY_OBSERVED)

- **AC-9**: Early pattern_usage_success
  - Given: observation_score=7 (partially_observed), manually set pattern_usage_success=true
  - When: is_confirmed_plus() → false. Then report_observation_event adds 4 points → score=11
  - Then: state=confirmed, is_confirmed_plus() → true, pattern_usage_confirmed emitted

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/intel/pattern/PatternStateMachineTest.csproj` — must exist and pass
**Status**: [x] 24/24 PASS — 2026-05-13

---

## Dependencies

- Depends on: content-registry Epic (Registry provides pattern static definitions + event weight table)
- Unlocks: Story 003 (ability multi-path unlock — depends on pattern state), Story 005 (upstream event receivers), Story 006 (downstream queries)
