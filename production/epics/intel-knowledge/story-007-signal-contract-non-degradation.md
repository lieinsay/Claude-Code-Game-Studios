# Story 007: Signal Contract & Non-Degradation Guards

> **Epic**: Intel / Knowledge System
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-knowledge-intel.md`
**Requirement**: `TR-intel-001`, `TR-intel-002`, `TR-intel-003`

**ADR Governing Implementation**: ADR-0007 (IntelManager), ADR-0002 (Signal Communication Protocol)
**ADR Decision Summary**: 9 个 typed signal 遵循 ADR-0002 协议——全部 typed params, sync emit, {noun}_{verb_past} 命名, max cascade depth=2。信号 emit 在状态变更之后（emit-after-mutation pattern）。所有信号不得使用 Dictionary payload（dictionary_signal_payload 为 forbidden pattern）。非退化保证——verified/confirmed/unlocked 三种终态在任何写入路径都有防御性检查。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Godot 4 Signal 支持 typed params——`signal knowledge_advanced(location_id: StringName, previous_state: int, new_state: int)`

**Control Manifest Rules (Core layer)**:
- Required: 所有信号 typed params (no Dictionary)；emit-after-mutation 模式；同步 .emit() 非 .emit.call_deferred()
- Forbidden: signal_cascade_depth 超 2；verified→rumored、confirmed→undiscovered、unlocked→locked 转换
- Guardrail: _can_transition_location() / _can_transition_pattern() / ability_state unlocked 跳过作为安全网

---

## Acceptance Criteria

### Signal Declarations & Emit-after-Mutation

- [ ] **AC-1**: GIVEN 地点状态从 RUMORED 变为 IDENTIFIED，WHEN consume_intel() 完成状态变更后，THEN `knowledge_advanced` signal emit（含 location_id, previous_state=1, new_state=2）——信号在状态变更后 emit
- [ ] **AC-2**: GIVEN pattern observation_score 跨过 confirmation_threshold，WHEN report_observation_event() 更新 score 后，THEN `pattern_state_changed` signal emit（含 pattern_id, PARIALLY_OBSERVED→CONFIRMED）

### Signal Typed Params (No Dictionary Payload)

- [ ] **AC-3**: GIVEN 所有 9 个 signal 声明，WHEN 检查 signal 参数，THEN 每个参数有显式类型注解（StringName, int, bool）——无 Dictionary 参数
- [ ] **AC-4**: GIVEN `pattern_observed` signal，WHEN emit，THEN 参数为 (pattern_id: StringName, event_id: StringName, new_score: int)——精确 3 个 typed params

### Signal Cascade Depth ≤ 2

- [ ] **AC-5**: GIVEN IntelManager signal emit 后下游系统响应该 signal 并 emit 自己的 signal，WHEN 追踪调用链，THEN 总 cascade depth ≤ 2（IntelManager emit → consumer reaction → consumer's consumer 可 emit，但 chain 到此为止）

### Non-Degradation: Location Knowledge

- [ ] **AC-6**: GIVEN location 为 VERIFIED，WHEN _can_transition_location(VERIFIED, RUMORED)，THEN 返回 false
- [ ] **AC-7**: GIVEN location 为 IDENTIFIED，WHEN _can_transition_location(IDENTIFIED, RUMORED)，THEN 返回 false
- [ ] **AC-8**: GIVEN location 为 RUMORED，WHEN _can_transition_location(RUMORED, UNKNOWN)，THEN 返回 false

### Non-Degradation: Pattern Knowledge

- [ ] **AC-9**: GIVEN pattern 为 CONFIRMED，WHEN 尝试降低 observation_score 或回退状态，THEN _compute_pattern_state() 仍返回 CONFIRMED——状态不可逆
- [ ] **AC-10**: GIVEN pattern_usage_success=true，WHEN 后续事件发生，THEN pattern_usage_success 保持 true——一旦掌握永久保留

### Non-Degradation: Ability

- [ ] **AC-11**: GIVEN ability 为 UNLOCKED，WHEN 任何写入尝试将其设为 LOCKED，THEN 写入被忽略——ability_state 保持 UNLOCKED
- [ ] **AC-12**: GIVEN ability 为 UNLOCKED 且对应伙伴离开队伍，WHEN check_unlock_conditions 后重新检查，THEN 能力保持 UNLOCKED——不会因条件消失而退化

### All 9 Signal Emit Verification

- [ ] **AC-13**: GIVEN 各操作场景，WHEN 对应状态变更发生，THEN 正确的信号被 emit：

| Signal | Emit Trigger |
|--------|-------------|
| knowledge_advanced | 地点状态变更 (unknown→rumored/identified/verified 等) |
| pattern_observed | 新的观测事件被添加到 triggered_events |
| pattern_state_changed | 规律状态转换 (undiscovered→partially_observed / partially_observed→confirmed) |
| pattern_usage_confirmed | is_confirmed_plus 从 false 变为 true |
| ability_unlocked | 能力从 locked 变为 unlocked |
| intel_consumed | consume_intel() 成功完成 (rule 5 后) |
| intel_consume_failed | consume_intel() 因 ERR_* 失败 |
| rumor_received | reveal_rumor() 成功写入新传闻 |
| rumor_confidence_changed | 来源置信度在验证后调整 |

---

## Implementation Notes

### Full Signal Declarations

```text
# === IntelManager Signal Declarations (ADR-0002 compliant) ===

# 地点知识变化
signal knowledge_advanced(location_id: StringName, previous_state: int, new_state: int)

# 规律观测事件触发
signal pattern_observed(pattern_id: StringName, event_id: StringName, new_score: int)

# 规律状态转换
signal pattern_state_changed(pattern_id: StringName, previous_state: int, new_state: int)

# 规律使用成功 (confirmed+ 激活)
signal pattern_usage_confirmed(pattern_id: StringName)

# 能力解锁
signal ability_unlocked(ability_id: StringName, unlock_path: StringName)

# 情报消耗完成
signal intel_consumed(intel_id: StringName)

# 情报消耗失败
signal intel_consume_failed(intel_id: StringName, reason: StringName)

# 传闻接收
signal rumor_received(location_id: StringName, source_tag: StringName)

# 传闻置信度变更
signal rumor_confidence_changed(source_tag: StringName, location_id: StringName, old_confidence: int, new_confidence: int)
```

### Emit-After-Mutation Pattern

所有方法遵循此模式:

```text
func reveal_rumor(location_id: StringName, source_tag: StringName, hazard_tags: Array, confidence: int) -> void:
    # 1. 防御性检查
    var current_state: int = knowledge_state.get(location_id, KNOWLEDGE_UNKNOWN)
    if current_state == KNOWLEDGE_VERIFIED:
        return
    if not _can_transition_location(current_state, _target_state_for_rumor(confidence)):
        return

    # 2. 状态变更
    var old_state: int = current_state
    _apply_location_transition(location_id, _target_state_for_rumor(confidence))
    _add_rumor_source(location_id, source_tag, hazard_tags, confidence)

    # 3. Emit 信号（状态变更后）
    rumor_received.emit(location_id, source_tag)
    knowledge_advanced.emit(location_id, old_state, knowledge_state[location_id])

    # 4. 触发能力重评估
    _reevaluate_ability_unlocks()
```

### Non-Degradation Guards

```text
# 地点知识非退化
func _can_transition_location(current: int, target: int) -> bool:
    if current == KNOWLEDGE_VERIFIED:
        return false
    if current == KNOWLEDGE_IDENTIFIED and target == KNOWLEDGE_RUMORED:
        return false
    if current == KNOWLEDGE_RUMORED and target == KNOWLEDGE_UNKNOWN:
        return false
    return true

# 规律状态非退化（在 _compute_pattern_state 中隐式保证——分数只增不减）
# triggered_events 只追加不删除——observation_score 单调递增

# 能力状态非退化
# check_unlock_conditions() 中：
# if ability_state[ability_id] == ABILITY_UNLOCKED → return true (短路)
# 不存在任何将 ABILITY_UNLOCKED 改回 ABILITY_LOCKED 的代码路径
```

### Confidence Adjustment on Verification

```text
func _adjust_rumor_confidence_on_verification(location_id: StringName) -> void:
    var sources: Array = rumor_sources.get(location_id, [])
    for source in sources:
        var old_confidence: int = source["confidence"]
        # 此处由下游系统提供验证结果是否与来源一致（简化为所有来源按 +0 处理——实际由探索/航行系统的具体验证逻辑提供）
        # 若验证结果与该来源一致：+25 (max 100)
        # 若验证结果与该来源矛盾：-30 (min 0)
        # 由调用方在 player_arrived_at 中传入验证数据后触发

# 具体调整方法——由外部传入验证结果时调用
func _apply_confidence_adjustment(source_tag: StringName, location_id: StringName, verification_matches: bool) -> void:
    var sources: Array = rumor_sources.get(location_id, [])
    for i in sources.size():
        if sources[i]["source_tag"] == source_tag:
            var old_conf: int = sources[i]["confidence"]
            var new_conf: int
            if verification_matches:
                new_conf = mini(old_conf + 25, 100)
            else:
                new_conf = maxi(old_conf - 30, 0)
            sources[i]["confidence"] = new_conf
            rumor_confidence_changed.emit(source_tag, location_id, old_conf, new_conf)
            break
```

### Signal Cascade Depth Enforcement

IntelManager 自身保证: 所有 signal emit 为同步 `.emit()`——不通过 `.emit.call_deferred()` 延迟。下游消费者若在 signal callback 中再 emit 自己的 signal，则由下游系统负责保证 cascade depth ≤ 2（ADR-0002 契约）。

---

## Out of Scope

- 下游系统对信号的响应实现（Chart 系统收到 knowledge_advanced 后更新航图渲染——属于 Chart 系统）
- signal cascade depth 的运行时检测工具——当前为设计和代码审查约束，非运行时强制执行
- 传闻置信度调整的触发判定（"验证结果是否与来源匹配"由探索/航行系统提供数据）

---

## QA Test Cases

- **AC-6 through AC-12**: Non-degradation
  - Given: 所有状态设为终态 (VERIFIED / CONFIRMED / UNLOCKED)
  - When: 尝试所有降级路径 (reveal_rumor on VERIFIED, retract observation, set ABILITY_LOCKED)
  - Then: 所有状态保持终态不变，无一降级

- **AC-13**: Full signal emit verification
  - Given: 模拟一次完整的 consume_intel 流程（intel 触发 location advancement + pattern observation + ability unlock）
  - When: consume_intel() 执行完成
  - Then: 按顺序 emit: knowledge_advanced (×N locations), pattern_observed, pattern_state_changed (if state changed), ability_unlocked, intel_consumed

- **AC-3 and AC-4**: Typed params
  - Given: 所有 signal 声明
  - When: 检查 signal 参数
  - Then: 每个参数有显式 C# 类型注解——无 Variant 或 Dictionary 参数

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/intel/SignalContractTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-006 (all logic implementing state mutations), ADR-0002 (signal protocol)
- Unlocks: All downstream system integration (signals are the notification backbone)
