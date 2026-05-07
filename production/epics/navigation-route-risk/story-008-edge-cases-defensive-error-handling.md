# Story 008: Edge Cases & Defensive Error Handling

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-001`, `TR-navigation-002`, `TR-navigation-003`

**ADR Governing Implementation**: ADR-0010 (fallback context, EncounterContext validation, writing order enforcement), ADR-0002 (signal consistency, max cascade depth 2), ADR-0006 (Web platform constraints — browser tab throttling, engine delta)
**ADR Decision Summary**: GDD 定义了 36 个边缘案例覆盖 11 个类别：状态转换边界、数值边界、航行中波段动态变化、隐藏标签与揭示、模块状态变化、遭遇效果叠加与冲突、玩家行为、存档与恢复、上游数据一致性、平台与计时、配置错误防御。这些边缘案例分布在 Stories 001-007 的实现中，本 Story 作为系统级边缘案例的集中验证和防御性错误处理层，确保所有跨 Story 的边缘行为正确、所有防御性检查到位。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: FORCED_LANDING 优先于 ARRIVED；retreat 在 0% 进度合法；终态下拒绝所有操作；多标签伤害取 max 不叠加；N_checks=0 零遭遇合法；波段边界用 ≥/≤ 判断；浏览器标签页切换后遭遇按 elapsed_time 排队结算不丢失；T_check 硬下限 4s；Δ_hull 范围验证 + 失败回退到 T_base=12s
- Forbidden: 信任 #9 的预检结果而不在 VOYAGE_PREPARING 结尾重新查询 #8；在终态下接受新的 route_committed；产生负值 hull_integrity；遭遇检查间隔 < 4s；使用挂钟时间
- Guardrail: ε=0.01s 抵达容差；单次伤害上限 6 点——一次检查不可能跨越两个波段；fallback context 触发记录 internal_error_log

---

## Acceptance Criteria

### State Transition Edge Cases

- [ ] **AC-1**: GIVEN FORCED_LANDING（hull≤0）与 ARRIVED（progress≥100%）同时触发，WHEN 同一帧判定，THEN FORCED_LANDING 优先。航程以 FORCED_LANDING 结束——不抵达
- [ ] **AC-2**: GIVEN 玩家在 99.9% 进度时触发撤退，WHEN 确认，THEN → RETREATED。玩家的显式决定优先于逼近的抵达
- [ ] **AC-3**: GIVEN 出航锁结束后立即撤退（进度 0%），WHEN 确认，THEN → RETREATED。D_accumulated=0，无遭遇结算。合法操作——不惩罚
- [ ] **AC-4**: GIVEN 终态下 UI 继续发送操作事件，WHEN 终态守卫检查，THEN 所有操作返回 {allowed: false}。终态不可逆——拒绝所有后续操作

### Numeric Boundary Edge Cases

- [ ] **AC-5**: GIVEN hull_integrity_departure=3 + d_check=6，WHEN hull_effective，THEN = max(0, 3-6) = 0。超量 3 点伤害丢弃——不产生负值
- [ ] **AC-6**: GIVEN hull_integrity_departure=100 + 17 次连续 max d=6 检查（总伤害 102），WHEN 第 17 次检查后，THEN hull_effective=0。FORCED_LANDING 触发。无负值产生
- [ ] **AC-7**: GIVEN N_checks=0（T_voyage_base < T_check），WHEN 航行执行，THEN 零遭遇，正常抵达 ARRIVED。合法行为
- [ ] **AC-8**: GIVEN 空遭遇条目集（所有标签隐藏未揭示 + 标签集为空），WHEN d_check = max(∅)，THEN = 0——显式定义。检查仍然计数为"已结算"
- [ ] **AC-9**: GIVEN 所有命中条目的 d_entry 均为 0（如 calm_passage + storm_eye_passage），WHEN d_check，THEN = 0。非伤害效果照常应用
- [ ] **AC-10**: GIVEN 航线零风险标签（hazard_tags=[]），WHEN 每次检查，THEN d_check=0。航程正常完成

### Hull Band Boundary Values

- [ ] **AC-11**: GIVEN hull=76，WHEN _get_hull_band(76)，THEN → intact (≥76)
- [ ] **AC-12**: GIVEN hull=75，WHEN _get_hull_band(75)，THEN → damaged (26-75)
- [ ] **AC-13**: GIVEN hull=26，WHEN _get_hull_band(26)，THEN → damaged (≥26)
- [ ] **AC-14**: GIVEN hull=25，WHEN _get_hull_band(25)，THEN → critical (1-25)
- [ ] **AC-15**: GIVEN hull=0，WHEN _get_hull_band(0)，THEN → destroyed (≤0)

### Dynamic Hull Band Transitions

- [ ] **AC-16**: GIVEN intact→damaged 波段跨越，WHEN _check_hull_band_transition()，THEN s_hull: 1.0→0.9, Δ_hull: 0→-0.10。T_voyage 重算。进度不跳回。波段变更事件发射
- [ ] **AC-17**: GIVEN damaged→critical 波段跨越，WHEN 处理，THEN s_hull: 0.9→0.75, Δ_hull: -0.10→-0.20。T_check: 10.8s→9.6s。已调度检查不回溯
- [ ] **AC-18**: GIVEN 单次检查 max 伤害 6 点，WHEN 检查结算，THEN 不可能一次跨越两个波段。AC-18 验证系统约束：intact→damaged 需 -25 点，单次上限 6 点

### Hidden Tag Edge Cases

- [ ] **AC-19**: GIVEN 所有隐藏标签全程未被揭示（概率 0.7^N_checks），WHEN 航程结束，THEN 标签保持隐藏。N=5 时约 16.8%，N=10 时约 2.8%。这是设计意图——不是 bug
- [ ] **AC-20**: GIVEN storm_eye_passage 触发 + 所有隐藏标签已揭示，WHEN 效果，THEN 仅对当前仍隐藏的标签生效——不重复更新
- [ ] **AC-21**: GIVEN 注册表有标签但 #6 无该标签条目，WHEN 查询，THEN 默认 hidden=true（悲观策略）。记录警告

### Module State Edge Cases

- [ ] **AC-22**: GIVEN lightning_proximity 击中侦察模块 + η_scout 1.0→0.6，WHEN 重算预览窗口，THEN N_preview: 2→1。超出新预览范围的图标移除。已在队列中的保留
- [ ] **AC-23**: GIVEN 侦察槽为空（无模块安装），WHEN lightning_proximity 触发 20% 概率，THEN 跳过模块伤害检定。不崩溃

### Encounter Effect Edge Cases

- [ ] **AC-24**: GIVEN wind_shear 连续命中导致检查间隔缩短，WHEN 多次叠加，THEN T_check = max(4s, T_calculated)。硬下限 4s——防止遭遇触发过快
- [ ] **AC-25**: GIVEN turbulence_zone 惩罚在两次检查之间，WHEN 新检查触发 + 惩罚过期，THEN 效果在新检查结算后清除。无"惩罚在结算中途过期"的模糊窗口

### Upstream Data Consistency

- [ ] **AC-26**: GIVEN route_id 在注册表中不存在，WHEN _preflight_check()，THEN → ABORTED_PREFLIGHT。原因："route_id [id] not found in content registry"
- [ ] **AC-27**: GIVEN #9 的 hazard_tags 与 Registry 不一致，WHEN _resolve_hazard_tags()，THEN 以 Registry 为准。补入缺失标签 + 排除多余标签——均记录警告
- [ ] **AC-28**: GIVEN #6 query_route_knowledge 超时/失败，WHEN _preflight_check()，THEN → ABORTED_PREFLIGHT。不缓存过期知识
- [ ] **AC-29**: GIVEN #9 允许出航但 #10 VOYAGE_PREPARING 结尾 can_depart() 返回 false，WHEN TOCTOU 重检，THEN → ABORTED_PREFLIGHT。不信任 #9 的预检结果

### Platform & Timing Edge Cases

- [ ] **AC-30**: GIVEN 浏览器标签页切出 + Δt=30s（标签页挂起），WHEN 恢复，THEN elapsed_time 仅累加实际 delta——不按挂钟时间跳跃。遭遇按 elapsed_time 排队结算——不丢失
- [ ] **AC-31**: GIVEN 浮点累积导致 elapsed_time ≈ T_voyage + 0.001s，WHEN 抵达判定，THEN ε=0.01s 容差——正确触发 ARRIVED。防止浮点误差跳过抵达

### Config Validation

- [ ] **AC-32**: GIVEN Δ_hull 配置为 -0.80（超出范围），WHEN 启动验证，THEN clamp 到 -0.50 并记录告警。验证失败时回退到 T_base=12s, Δ_hull=0
- [ ] **AC-33**: GIVEN T_voyage_base < T_check（配置错误或极短航线），WHEN 计算 N_checks，THEN N_checks=0。零遭遇航行。记录警告

### Re-entrancy & Duplicate Signal Guards

- [ ] **AC-34**: GIVEN voyage_state=VOYAGE_PREPARING，WHEN 第二个 route_committed 到达，THEN 拒绝。记录警告。保持当前 PREPARING 状态
- [ ] **AC-35**: GIVEN voyage_state=IN_PROGRESS，WHEN route_committed 到达，THEN 拒绝。航程不可重入

### Complete Passive Voyage

- [ ] **AC-36**: GIVEN 玩家完全不操作（不撤退、不交互），WHEN 航程推进至 100%，THEN 终态为 ARRIVED（若 hull>0）或 FORCED_LANDING（若 hull≤0）。航程是"观察并决策"的体验——被动完成合法

### Multi-System Write Consistency

- [ ] **AC-37**: GIVEN 航行结束 5 步写入顺序，WHEN 任一步骤失败，THEN 后续步骤仍执行。不因单一写入失败阻塞整套流程。每步失败记录错误日志
- [ ] **AC-38**: GIVEN IN_PROGRESS → IN_PROGRESS 转换被触发（无意调用），WHEN 状态机处理，THEN 无操作——不重置计时器，不重复调度检查

---

## Implementation Notes

### State Transition Guard

```gdscript
func _is_terminal_state(state: StringName) -> bool:
    return state in [&"ARRIVED", &"RETREATED", &"FORCED_LANDING", &"ABORTED_PREFLIGHT"]


func _guard_terminal_state() -> bool:
    if _is_terminal_state(_voyage_state):
        push_warning("Navigation: operation rejected — voyage is in terminal state %s" % _voyage_state)
        return true
    return false


func _on_route_committed(route_id: StringName, destination_id: StringName,
                          hazard_tags: Array[StringName]) -> void:
    if _voyage_state != &"IDLE":
        push_warning("Navigation: route_committed received while in %s state — rejected" % _voyage_state)
        return
    # ... continue with preflight
```

### FORCED_LANDING Priority

```gdscript
func _process_voyage(delta: float) -> void:
    if _voyage_state != &"IN_PROGRESS":
        return

    _elapsed_time += delta
    var total_duration: float = _active_voyage.get("total_duration", 60.0)

    while _should_trigger_next_check():
        _resolve_encounter_check()

    # FORCED_LANDING 优先于 ARRIVED
    if _get_hull_integrity_effective() <= 0:
        _voyage_state = &"FORCED_LANDING"
        _finalize_voyage()
        return

    if _elapsed_time >= total_duration - ARRIVAL_EPSILON:
        _elapsed_time = total_duration
        _voyage_state = &"ARRIVED"
        _finalize_voyage()
```

### Config Validation at Startup

```gdscript
func _validate_configuration() -> void:
    # 验证 Δ_hull 范围
    for band in HULL_BAND_CHECK_OFFSETS:
        var delta: float = HULL_BAND_CHECK_OFFSETS[band]
        if delta < -0.50 or delta > 0.0:
            push_error("Navigation: Δ_hull for %s = %.2f out of range [-0.5, 0] — clamping" % [band, delta])
            HULL_BAND_CHECK_OFFSETS[band] = clampf(delta, -0.50, 0.0)

    # 验证 T_check 不会 ≤ 0
    var worst_t_check: float = BASE_CHECK_INTERVAL * (1.0 - 0.50)
    if worst_t_check <= 0:
        push_error("Navigation: worst-case T_check = %.2f — falling back to defaults" % worst_t_check)
        BASE_CHECK_INTERVAL = 12.0
        for band in HULL_BAND_CHECK_OFFSETS:
            HULL_BAND_CHECK_OFFSETS[band] = 0.0

    # 验证遭遇表概率总和
    _validate_encounter_tables()

    # 验证距离带配置
    for band in DISTANCE_DURATION:
        var duration: float = DISTANCE_DURATION[band]
        if duration <= 0:
            push_error("Navigation: T_distance for %s = %.1f invalid — using 60s fallback" % [band, duration])
            DISTANCE_DURATION[band] = 60.0
```

### Browser Tab Throttling Safe Timer

```gdscript
# 使用引擎 _process(delta) — 不是挂钟时间
# 标签页挂起时 Godot 暂停 _process 调用
# 恢复后 delta 变大但 elapsed_time 按实际经过帧数累加

func _process(delta: float) -> void:
    if _voyage_state == &"IN_PROGRESS":
        _process_voyage(delta)

# 遭遇不会"错过"——按 elapsed_time 排队
# 标签页恢复后可能一次 _process 中触发多个排队遭遇
func _process_voyage(delta: float) -> void:
    _elapsed_time += delta
    # while 循环处理所有排队遭遇
    while _should_trigger_next_check():
        _resolve_encounter_check()
```

### Empty Encounter Set Handling

```gdscript
func _max_damage(hits: Array[Dictionary]) -> int:
    if hits.is_empty():
        return 0  # 显式定义：空集 → 0
    var max_d: int = 0
    for hit in hits:
        max_d = maxi(max_d, hit.get("damage_amount", 0))
    return max_d
```

### IN_PROGRESS → IN_PROGRESS No-Op

```gdscript
func _transition_to(new_state: StringName) -> Dictionary:
    if _is_terminal_state(_voyage_state):
        return {"allowed": false, "reason": "terminal state"}

    if new_state == _voyage_state:
        return {"allowed": true, "state": _voyage_state}  # no-op, no side effects

    # ... normal transition logic
```

### Retreat at 0% Progress

```gdscript
func request_retreat() -> void:
    if _voyage_state != &"IN_PROGRESS":
        return

    # retreat 在任何时刻合法——包括 0% 进度
    _voyage_state = &"RETREATED"
    _finalize_voyage()
    # _finalize_voyage() 中的 _write_damage_to_hull() 在 D_accumulated=0 时跳过写入


func is_retreat_allowed() -> bool:
    return _voyage_state == &"IN_PROGRESS"
```

---

## Out of Scope

- 浏览器标签页切出/恢复时的具体 UI 提示（"你离开了 X 秒"）——属于 UX 设计，OQ-07
- 存档加密和压缩——属于 #3 Persistence 的系统级安全
- 跨版本存档迁移的具体策略（哪些存档可迁移、哪些废弃）——属于 #3 Persistence
- 配置值的热重载（运行时修改遭遇表/距离带）——Phase 2+

---

## QA Test Cases

- **AC-1 through AC-4**: State transition edges
  - FORCED_LANDING > ARRIVED; retreat at 99.9%; retreat at 0%; terminal reject all

- **AC-5 through AC-10**: Numeric edges
  - 3-6=0 overflow discard; 100-(17×6)=0 forced; N=0 zero encounters; empty set→0; all-zero→0 with effects; zero tags→normal

- **AC-11 through AC-15**: Band boundaries
  - 76=intact, 75/26=damaged, 25=critical, 0=destroyed

- **AC-16/17**: Dynamic band transitions
  - intact→damaged: s=1.0→0.9, Δ=0→-0.10; damaged→critical: s=0.9→0.75, Δ=-0.10→-0.20

- **AC-24**: wind_shear stacking → T_check floor 4s

- **AC-26 through AC-29**: Upstream data consistency
  - invalid route_id→ABORT; tag mismatch→Registry wins; #6 timeout→ABORT; TOCTOU can_depart false→ABORT

- **AC-30/31**: Platform edges
  - tab suspend→no clock drift; float epsilon→correct arrival

- **AC-34/35**: Re-entrancy
  - PREPARING+second→reject; IN_PROGRESS+second→reject

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/navigation/edge_cases_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: All prior navigation stories (001-007), modules-hull-state Epic, intel-knowledge Epic, content-registry Epic, local-save-persistence Epic
- Unlocks: Navigation/Risk system-level integration testing, QA smoke tests for voyage flows
