# Story 004: EncounterContext Consumption & ARRIVING Entry

> **Epic**: Exploration / Scavenge Scenario
> **Status**: Done
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/exploration-scavenge-scenario.md`
**Requirement**: `TR-exploration-001`

**ADR Governing Implementation**: ADR-0013 (§4a 探索会话生命周期, §5a 状态机, §Key Interfaces — EncounterContext 消费, §8 场景层合同); ADR-0010 (§3–5 EncounterContext 字段定义、信号合约、Fallback Context)
**ADR Decision Summary**: ExplorationManager 在收到 Navigation (#10) 的 voyage_completed 信号后消费 EncounterContext。_validate_encounter_context() 校验 5 种故障条件（null、缺 route_id、缺 destination_id、无效 voyage_result、resolved_encounters 非 Array）——任一命中则构建 fallback context：route_id="unknown", destination_id="cloudwatch-ruins-fallback", voyage_result="arrived", 空 resolved_encounters。有效 ctx 按 voyage_result 路由入场模式：arrived → _enter_arriving_normal()（入口区正常入场）；forced_landing → _enter_arriving_forced_landing()（坠机点入场 + 船体损伤脉冲通知）；retreated → _enter_arriving_retreated()（入口区入场 + 撤退状态）。入场时：快照 η_scout、加载探索点持久状态、初始化会话瞬时数据。ARRIVING 文本由 EncounterContext 字段驱动——arrived=抵达描述，forced_landing=迫降描述。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 进入探索前必须校验 EncounterContext——字段缺失或无效时降级至 fallback context，不阻塞玩家进入探索；voyage_result 路由必须完整覆盖 arrived/forced_landing/retreated 三种值；η_scout 在 ARRIVING 阶段快照——探索过程中不变
- Forbidden: 在 fallback context 触发时静默吞掉错误——必须记录 internal_error_log；依赖 EncounterContext 中未定义的字段（如假定必有 resolved_encounters[0].damage_amount）
- Guardrail: fallback context 确保玩家始终能进入探索——即使上游数据完全损坏

---

## Acceptance Criteria

### EncounterContext Validation

- [ ] **AC-1**: GIVEN ctx=null，WHEN _validate_encounter_context(null)，THEN 返回 fallback context。记录 internal_error_log("Exploration: null EncounterContext — using fallback")
- [ ] **AC-2**: GIVEN ctx.route_id 缺失或为 &""，WHEN _validate_encounter_context(ctx)，THEN 返回 fallback context。记录缺失字段日志
- [ ] **AC-3**: GIVEN ctx.destination_id 缺失或为 &""，WHEN _validate_encounter_context(ctx)，THEN 返回 fallback context
- [ ] **AC-4**: GIVEN ctx.voyage_result = &"invalid_value"（非 arrived/retreated/forced_landing），WHEN _validate_encounter_context(ctx)，THEN 返回 fallback context
- [ ] **AC-5**: GIVEN ctx.resolved_encounters 不是 Array 类型，WHEN _validate_encounter_context(ctx)，THEN 返回 fallback context
- [ ] **AC-6**: GIVEN ctx 所有字段有效 + voyage_result="arrived"，WHEN _validate_encounter_context(ctx)，THEN 返回原始 ctx（无修改）

### Fallback Context Construction

- [ ] **AC-7**: GIVEN fallback 被触发，WHEN _build_fallback_context()，THEN 返回 {route_id: "unknown", destination_id: "cloudwatch-ruins-fallback", voyage_result: "arrived", resolved_encounters: [], accumulated_damage: 0, revealed_hidden_tags: [], hull_band_arrival: "intact", forced_landing_position: "", damaged_slots: []}
- [ ] **AC-8**: GIVEN fallback context 用于进入探索，WHEN 玩家进入，THEN 正常进入——ARRIVING 文本为默认抵达描述。玩家不会感知到异常

### Arrival Mode Routing

- [ ] **AC-9**: GIVEN voyage_result="arrived" + 完整 EncounterContext，WHEN enter_exploration(ctx)，THEN _enter_arriving_normal()：入场位置=入口区 (D_outer, 25,33), ARRIVING 描述为安全抵达文字, 船体状态正常
- [ ] **AC-10**: GIVEN voyage_result="forced_landing" + 完整 EncounterContext + forced_landing_position 非空，WHEN enter_exploration(ctx)，THEN _enter_arriving_forced_landing()：入场位置=坠机点, ARRIVING 描述为迫降文字 + 船体损伤脉冲通知, exploration_phase_changed 携带 damage pulse 标记
- [ ] **AC-11**: GIVEN voyage_result="retreated" + session_retreat_flagged 可能已设置，WHEN enter_exploration(ctx)，THEN _enter_arriving_retreated()：入场位置=入口区, ARRIVING 描述为撤退返回文字
- [ ] **AC-12**: GIVEN voyage_result="forced_landing" + forced_landing_position 为空字符串，WHEN enter_exploration()，THEN fallback 至正常入场 + 记录 warning "forced_landing without position — using normal entry"

### ARRIVING Phase Behavior

- [ ] **AC-13**: GIVEN enter_exploration() 完成，WHEN 进入 ARRIVING 阶段，THEN session_phase=PHASE_ARRIVING + 玩家不可移动或交互 + ARRIVING 文本覆盖层显示
- [ ] **AC-14**: GIVEN ARRIVING 阶段 + 玩家按任意键，WHEN skip_arriving()，THEN PHASE_ARRIVING→PHASE_EXPLORING + 文本覆盖层消失 + 玩家可自由移动
- [ ] **AC-15**: GIVEN ARRIVING 阶段 + 3 秒无操作，WHEN 计时器到期，THEN 自动 skip_arriving()。3 秒超时防止玩家卡在 ARRIVING

### Entry Initialization

- [ ] **AC-16**: GIVEN enter_exploration() 调用，WHEN ARRIVING 开始，THEN η_scout 从 ModulesManager 快照 + 探索点持久状态从 exploration_points 加载 + 会话瞬时状态初始化（search_consumed={}, intel_interacted={}, threats_active 从持久状态复制, retreat_flagged=false）
- [ ] **AC-17**: GIVEN 从存档恢复活跃会话（Story 006），WHEN 重新进入，THEN 不调用 enter_exploration()——直接通过 _restore_active_session() 进入对应阶段。EncounterContext 从存档中读取而非 Navigation 重新发射

---

## Implementation Notes

### EncounterContext Validation

```text
func _validate_encounter_context(ctx) -> Dictionary:
    if ctx == null or not ctx is Dictionary:
        _log_internal_error("Exploration: null or non-Dictionary EncounterContext")
        return _build_fallback_context()

    var missing_fields := []
    if not ctx.get("route_id") or ctx.route_id == &"":
        missing_fields.append("route_id")
    if not ctx.get("destination_id") or ctx.destination_id == &"":
        missing_fields.append("destination_id")

    if missing_fields.size() > 0:
        _log_internal_error("Exploration: EncounterContext missing fields: %s" % missing_fields)
        return _build_fallback_context()

    var result: StringName = ctx.get("voyage_result", &"")
    if result not in [&"arrived", &"retreated", &"forced_landing"]:
        _log_internal_error("Exploration: invalid voyage_result '%s'" % result)
        return _build_fallback_context()

    if not ctx.get("resolved_encounters") is Array:
        _log_internal_error("Exploration: resolved_encounters is not Array")
        return _build_fallback_context()

    return ctx
```

### Arrival Mode Entry

```text
func enter_exploration(ctx: Dictionary) -> void:
    var validated := _validate_encounter_context(ctx)
    encounter_context = validated

    # 确定探索点
    current_exploration_point_id = validated.destination_id
    if current_exploration_point_id == &"":
        current_exploration_point_id = &"exploration_point.cloudwatch-ruins"

    # 加载探索点持久状态
    if not exploration_points.has(current_exploration_point_id):
        _init_exploration_point_state(current_exploration_point_id)
    var pt_state := exploration_points[current_exploration_point_id]

    # 初始化会话瞬时状态
    session_search_consumed = pt_state.search_points.duplicate(true)
    session_intel_interacted = pt_state.intel_points.duplicate(true)
    session_threats_active = pt_state.threat_points.duplicate(true)
    session_retreat_flagged = false

    # 快照 η_scout
    _snapshot_eta_scout()

    # 按 voyage_result 路由入场
    _transition_phase(PHASE_ARRIVING)
    var result: StringName = validated.get("voyage_result", &"arrived")
    match result:
        &"arrived":
            _enter_arriving_normal(validated)
        &"forced_landing":
            _enter_arriving_forced_landing(validated)
        &"retreated":
            _enter_arriving_retreated(validated)

    # 触发持久化快照（EC-11-01: 进入探索时存档）
    _trigger_snapshot()

func _enter_arriving_normal(ctx: Dictionary) -> void:
    # ARRIVING 描述文本由场景层消费 exploration_phase_changed 信号渲染
    pass  # 场景层根据 phase=ARRIVING + voyage_result=arrived 显示对应文字

func _enter_arriving_forced_landing(ctx: Dictionary) -> void:
    var pos: StringName = ctx.get("forced_landing_position", &"")
    if pos == &"":
        push_warning("Exploration: forced_landing without position — using normal entry")
        _enter_arriving_normal(ctx)
        return
    # 场景层收到 phase=ARRIVING + voyage_result=forced_landing
    # 渲染：船体损伤脉冲动画 + 坠机点入场 + 迫降描述文字

func _enter_arriving_retreated(ctx: Dictionary) -> void:
    pass  # 场景层显示撤退返回文字
```

### Fallback Context

```text
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

### Navigation Signal Connection

```text
# 在 feature_ready 中连接 Navigation 信号
func _on_feature_ready() -> void:
    Navigation.voyage_completed.connect(_on_voyage_completed)
    Persistence.register_domain_serializer("exploration", _serialize_exploration)

func _on_voyage_completed(ctx: Dictionary) -> void:
    if session_phase != PHASE_IDLE:
        push_warning("Exploration: voyage_completed received but session is not idle (phase=%d)" % session_phase)
        return
    enter_exploration(ctx)
```

---

## Out of Scope

- Navigation (#10) voyage_completed 信号的发射——属于 navigation-route-risk Epic
- 场景层的 ARRIVING 文本渲染、船体损伤脉冲动画——由场景层/UI 负责
- 活跃会话从存档恢复（_restore_active_session）——属于 Story 006
- 探索点模板的场景实例化——由 Platform #2 的场景管理系统负责

---

## QA Test Cases

- **AC-1–5**: 5 fallback trigger conditions (null, missing route_id, missing destination_id, invalid voyage_result, non-Array resolved_encounters)
- **AC-6**: Valid ctx passes through unchanged
- **AC-7**: Fallback context field completeness
- **AC-9–11**: Arrival mode routing (arrived/forced_landing/retreated)
- **AC-12**: forced_landing without position → fallback to normal
- **AC-13–15**: ARRIVING phase behavior (immobile, skip via key, 3s auto-skip)
- **AC-16**: Entry initialization (η_scout snapshot, persistent state load, transient init)
- **AC-17**: Session restore skips enter_exploration (verified in Story 006)

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/exploration/EncounterContextEntryTest.csproj` — must exist and pass
**Status**: [x] 37/37 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (状态机), navigation-route-risk Epic (voyage_completed 信号, EncounterContext 结构), modules-hull-state Epic (η_scout), content-registry Epic (探索点持久状态初始化)
- Unlocks: Story 005 (extraction 依赖 ARRIVING→EXPLORING 流程), Story 006 (持久化恢复依赖 enter_exploration 完整流程)
