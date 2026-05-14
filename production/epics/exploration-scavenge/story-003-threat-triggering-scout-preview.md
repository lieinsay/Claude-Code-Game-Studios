# Story 003: Threat Triggering, Scout Preview & Environmental Handling

> **Epic**: Exploration / Scavenge Scenario
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/exploration-scavenge-scenario.md`
**Requirement**: `TR-exploration-002`

**ADR Governing Implementation**: ADR-0013 (§4c 威胁管理接口, §5c F-11-02 威胁触发判定, §5d F-11-03 侦察预览映射, §6 威胁优先级排序, §8 CombatManager 战斗结果回调合同)
**ADR Decision Summary**: F-11-02 threat_trigger() 判定两种威胁类别——环境威胁（environmental）靠近必触发（trigger_prob=1.0），由 Exploration 自行处理（施加 hull_damage 或封锁路径）；守卫威胁（guard）靠近 70% 概率触发 + 交互 100% 触发，传递 threat_context 至 CombatManager (#12)。F-11-03 scout_preview_level() 将 η_scout 映射为 3 档预览等级——PREVIEW_NONE (η≤0)、PREVIEW_PRESENCE (0<η<1)、PREVIEW_FULL (η≥1)。η_scout 在 enter_exploration() 时一次性快照。多威胁同时触发按确定性优先级排序——环境 > 守卫、距离近 > 远、同距离 dict 序。守卫在 #12 不可用时惰性降级（inert），不触发不伤害。威胁清除后 is_active=false，本会话内永久安全。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 环境威胁 trigger_prob 固定为 1.0（踩到必触发）；守卫威胁交互触发 100%；威胁 is_active=false 时直接返回 {triggered: false}；多威胁按环境优先+距离排序确定性处理
- Forbidden: 环境威胁传递至 CombatManager（仅由 Exploration 自行处理）；已清除的威胁（is_active=false）再次触发
- Guardrail: #12 不可用时守卫 inert——记录 warning 日志，不崩溃；threat_point 在探索点模板中缺失时记录错误并跳过

---

## Acceptance Criteria

### F-11-02: Threat Trigger Core

- [ ] **AC-1**: GIVEN 环境威胁 + is_active=true + 玩家进入 trigger_radius (2-3 单位)，WHEN check_threat_trigger(player_pos, "proximity")，THEN triggered=true 100%（100/100 测试）。环境威胁靠近必触发
- [ ] **AC-2**: GIVEN 守卫威胁 + is_active=true + 玩家进入 trigger_radius (4-6 单位)，WHEN check_threat_trigger(player_pos, "proximity")，THEN triggered=true ~70%。统计 500 次逼近，比例 ∈ [0.60, 0.80]
- [ ] **AC-3**: GIVEN 守卫威胁 + is_active=true + 玩家对威胁点按 E (trigger_type="interaction")，WHEN check_threat_trigger()，THEN triggered=true 100%。交互必定触发
- [ ] **AC-4**: GIVEN 环境威胁 + is_active=true + 玩家对威胁点按 E (trigger_type="interaction")，WHEN check_threat_trigger()，THEN triggered=true 100%
- [ ] **AC-5**: GIVEN 任意威胁 + is_active=false（已清除），WHEN check_threat_trigger()，THEN triggered=false。不重复触发
- [ ] **AC-6**: GIVEN 守卫威胁 + 玩家在 trigger_radius 外，WHEN check_threat_trigger(player_pos, "proximity")，THEN triggered=false。距离不足不触发

### Environmental Threat Handling

- [ ] **AC-7**: GIVEN 环境威胁触发，WHEN _handle_environmental_threat()，THEN hull_damage 通过 ModulesManager.apply_hull_damage() 写入 #8 + threat_triggered 信号发射 + env_threat_active=true
- [ ] **AC-8**: GIVEN 环境威胁类型为 "block_path"（封锁路径），WHEN 触发，THEN hull_damage=0 + 路径封锁标记设置 + 场景层收到通知更新覆盖层
- [ ] **AC-9**: GIVEN 环境威胁触发后 + 未清除 (env_threat_active=true)，WHEN 退出探索，THEN F-11-05 state_variant_transition 消费此标记

### Guard Threat Handling

- [ ] **AC-10**: GIVEN 守卫威胁触发 + CombatManager (#12) 可用，WHEN _handle_guard_threat()，THEN threat_context 正确构建 + CombatManager.initiate_threat(threat_context) 调用 + threat_triggered 信号发射 + session_substate→SUBSTATE_THREATENED
- [ ] **AC-11**: GIVEN 守卫威胁触发 + CombatManager (#12) 不可用（返回 unavailable），WHEN _handle_guard_threat()，THEN 守卫 inert——不触发不伤害 + threat_point.is_active 保持 true + warning 日志。符合 EC-11-12
- [ ] **AC-12**: GIVEN CombatManager 返回 combat_result，WHEN on_combat_result(result)，THEN outcome="suppressed" → 威胁清除 (is_active=false) + threat_cleared 信号；outcome="tanked" → 威胁保持活跃；outcome="retreated" → session_retreat_flagged=true + force_extraction(EXTRACTION_RETREAT)

### F-11-03: Scout Preview

- [ ] **AC-13**: GIVEN η_scout=0 (无侦察模块)，WHEN scout_preview_level()，THEN 返回 PREVIEW_NONE。无预览——威胁点不可见
- [ ] **AC-14**: GIVEN η_scout=0.48 (Scout 受损+critical band)，WHEN scout_preview_level()，THEN 返回 PREVIEW_PRESENCE。红色感叹号但不显示类型
- [ ] **AC-15**: GIVEN η_scout=0.6 / 0.76 / 0.8 / 0.95 (Scout 正常但非满分)，WHEN scout_preview_level()，THEN 全部返回 PREVIEW_PRESENCE
- [ ] **AC-16**: GIVEN η_scout=1.0 (Scout 正常+intact band)，WHEN scout_preview_level()，THEN 返回 PREVIEW_FULL。完整预览——类型+方位文本
- [ ] **AC-17**: GIVEN η_scout 在探索中因船体受损而变化，WHEN 查询 scout_preview_level()，THEN 返回进入时的快照值——不反映实时变化。已知轻度 UI 不一致（EC-11-13）

### Multi-Threat Simultaneous Trigger (EC-11-10)

- [ ] **AC-18**: GIVEN 2 个威胁（1 环境 + 1 守卫）同时在触发半径内，WHEN check_threat_trigger()，THEN 环境威胁先处理 → 守卫威胁后处理。优先级: 环境 > 守卫
- [ ] **AC-19**: GIVEN 2 个同类型威胁同距离 + 不同 threat_id，WHEN _sort_threats_by_priority()，THEN 按 threat_id 字典序确定顺序。确定性排序

### build_threat_context

- [ ] **AC-20**: GIVEN 守卫威胁触发，WHEN build_threat_context(threat_point, trigger_type)，THEN 返回 threat_type, threat_id, position, encounter_params（含 full_damage_range, module_damage_chance, emergency_cost, knockback_distance_*, can_be_suppressed）。从 Registry THREAT_CONFIG_TABLE 查询
- [ ] **AC-21**: GIVEN 环境威胁触发，WHEN build_threat_context(threat_point, trigger_type)，THEN 返回 threat_type="environmental", encounter_params=null——无战斗参数

---

## Implementation Notes

### Threat Trigger

```text
func check_threat_trigger(player_pos: Vector2, trigger_type: StringName) -> Dictionary:
    var triggered_threats := []
    for tp_id in session_threats_active:
        var tp := _threat_point_config(tp_id)
        if not tp.get("is_active", false):
            continue
        var result := _threat_trigger_single(tp, trigger_type, player_pos)
        if result.triggered:
            triggered_threats.append(result)

    if triggered_threats.size() > 1:
        triggered_threats = _sort_threats_by_priority(triggered_threats, player_pos)

    for threat in triggered_threats:
        _handle_triggered_threat(threat)
        if session_phase == PHASE_EXTRACTING:
            _interrupt_extraction(&"threat")
            break  # 被打断——不再处理后续威胁

    return {triggered: triggered_threats.size() > 0, threats: triggered_threats}

func _threat_trigger_single(tp: Dictionary, trigger_type: StringName, player_pos: Vector2) -> Dictionary:
    if trigger_type == "interaction":
        return {triggered: true, threat_point: tp,
                context: _build_threat_context(tp, "interaction")}

    if trigger_type == "proximity":
        var dist := player_pos.distance_to(tp.position)
        if dist > tp.trigger_radius:
            return {triggered: false}
        var prob := TRIGGER_PROB_TABLE[tp.threat_category]
        if randf() < prob:
            return {triggered: true, threat_point: tp,
                    context: _build_threat_context(tp, "proximity")}

    return {triggered: false}
```

### Scout Preview

```text
var _eta_scout_snapshot: float = 0.0

func _snapshot_eta_scout() -> void:
    _eta_scout_snapshot = ModulesManager.get_scout_efficiency()

func get_scout_preview_level() -> int:
    if _eta_scout_snapshot <= 0.0:
        return PREVIEW_NONE
    elif _eta_scout_snapshot >= 1.0:
        return PREVIEW_FULL
    else:
        return PREVIEW_PRESENCE
```

### Threat Priority Sort

```text
func _sort_threats_by_priority(threats: Array, player_pos: Vector2) -> Array:
    threats.sort_custom(func(a, b):
        var cat_a := a.threat_point.threat_category
        var cat_b := b.threat_point.threat_category
        if cat_a != cat_b:
            return cat_a == THREAT_ENVIRONMENTAL  # 环境优先
        var dist_a := player_pos.distance_to(a.threat_point.position)
        var dist_b := player_pos.distance_to(b.threat_point.position)
        if abs(dist_a - dist_b) < 0.01:
            return a.threat_point.id < b.threat_point.id  # dict order
        return dist_a < dist_b
    )
    return threats
```

### Combat Result Callback

```text
func on_combat_result(result: Dictionary) -> void:
    match result.get("outcome", &""):
        &"suppressed":
            var threat_id: StringName = result.get("threat_id", &"")
            session_threats_active[threat_id] = false
            threat_cleared.emit(threat_id)
        &"tanked":
            pass  # 威胁保持活跃
        &"retreated":
            session_retreat_flagged = true
            force_extraction(EXTRACTION_RETREAT)

    session_substate = SUBSTATE_IDLE  # 威胁处理完毕
```

### Trigger Probability Table

```text
const TRIGGER_PROB_TABLE := {
    THREAT_ENVIRONMENTAL: 1.0,   # 环境威胁必触发
    THREAT_GUARD: 0.70,          # 守卫威胁 70% proximity 触发
}
```

---

## Out of Scope

- CombatManager 的 threat resolution 实现——属于 combat-threat Epic
- 威胁触发的视觉/音频效果——属于 #17 Feedback 或场景层
- 场景层威胁标记渲染（感叹号/标签）——属于 UI/HUD Epic
- ModulesManager 的 apply_hull_damage 实现——属于 modules-hull-state Epic

---

## QA Test Cases

- **AC-1**: Environmental proximity 100% trigger (100/100)
- **AC-2**: Guard proximity ~70% trigger (statistical, 500 trials)
- **AC-3–4**: Interaction always triggers (both categories)
- **AC-5**: Cleared threat (is_active=false) → no trigger
- **AC-7–8**: Environmental damage or block path applied
- **AC-10**: Guard → threat_context passed to CombatManager
- **AC-11**: Guard + #12 unavailable → inert, no damage
- **AC-12**: Combat result handling (suppressed/tanked/retreated)
- **AC-13–16**: Scout preview mapping for all η_scout edge values
- **AC-18–19**: Multi-threat deterministic priority ordering
- **AC-20–21**: build_threat_context field completeness

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/exploration/threat/ThreatTriggeringTest.csproj` — must exist and pass
**Status**: [x] 31/31 PASS — 2026-05-13; rerun PASS — 2026-05-14 Epic #11/#15 review

---

## Dependencies

- Depends on: Story 001 (状态机), modules-hull-state Epic (η_scout, apply_hull_damage), combat-threat Epic (initiate_threat, combat_result contract), content-registry Epic (THREAT_CONFIG_TABLE)
- Unlocks: Story 005 (retreat_flagged → λ_forced), Story 006 (持久化威胁活跃状态)
