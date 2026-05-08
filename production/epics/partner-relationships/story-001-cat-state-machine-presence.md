# Story 001: Cat State Machine & Presence Contract

> **Epic**: Partner & Relationships
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/partner-relationships.md`
**Requirement**: `TR-partner-001`

**ADR Governing Implementation**: ADR-0015 (§1 PartnerManager Autoload #15, §2 Dictionary 后端存储, §5f 猫运行时状态机)
**ADR Decision Summary**: PartnerManager 管理猫的 6 态运行时状态机。状态转换由 Hub 事件驱动——player_entered_zone、hub_state_changed、player_returned_to_hub——而非 _process 定时器。6 态：SLEEPING_ON_INTEL_STATION（默认初始态）→ IDLE_LIVING_QUARTERS（玩家进入生活舱）→ FOLLOWING_PLAYER_TO_BENCH（玩家走向工作台）→ BENCH_ADJACENT（抵达工作台旁）→ IN_NEST（闲置超过 T_nest_settle=20s）→ 嗅辨时进入 SNIFFING（状态门控，持续 T_sniff_lockout=2.5s）。departure_locked 冻结猫状态；in_transit 简化模拟（不渲染，逻辑态保持 idle）；arrival 强制 CAT_IDLE_LIVING_QUARTERS（猫不在入口——R13）。zone boundary 防抖 cooldown=0.5s（E.4.b）。R2 存在性契约：query_partner_present() 在任何 Hub 状态下恒返回 true——猫永远在飞艇上，不消失不死亡。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: query_partner_present() 恒返回 true——零状态分支；cat_state 所有转换通过单一函数——禁止外部直接赋值；初始态从 Hub 上下文派生——不硬编码
- Forbidden: 猫状态由 _process(delta) 驱动的事件或奖励——R15.4；猫出现在入口等待玩家——R13 归港行为；departure_locked 和 arrival 以外的 Hub 状态自行触发猫状态变更（必须经 zone 事件）
- Guardrail: zone boundary 防抖 cooldown=0.5s 但 state machine 正确性不受 cooldown 值影响——cooldown=0 时仅视觉抖动，不产生逻辑错误

---

## Acceptance Criteria

### R2 Presence Contract

- [ ] **AC-1**: GIVEN Hub 状态为 landed / departure_locked / in_transit / arrival 任意值，WHEN query_partner_present()，THEN 返回 true。零例外
- [ ] **AC-2**: GIVEN 新游戏启动 + 任意 Hub 状态，WHEN 初始化，THEN 无猫"不在场"的代码路径。R2 是硬约束——不持有 absent 状态

### Cat State Machine Core

- [ ] **AC-3**: GIVEN 新游戏 + Hub=landed + 玩家未移动，WHEN 初始化，THEN cat_state=SLEEPING_ON_INTEL_STATION。猫在驾驶舱情报台
- [ ] **AC-4**: GIVEN cat_state=SLEEPING_ON_INTEL_STATION + 玩家进入生活舱，WHEN player_entered_zone("living_quarters")，THEN cat_state→IDLE_LIVING_QUARTERS
- [ ] **AC-5**: GIVEN cat_state=IDLE_LIVING_QUARTERS + 玩家走向工作台，WHEN 触发，THEN cat_state→FOLLOWING_PLAYER_TO_BENCH → BENCH_ADJACENT（到达时）
- [ ] **AC-6**: GIVEN cat_state=BENCH_ADJACENT + 玩家离开工作台 reach_limit，WHEN 触发，THEN cat_state→IDLE_LIVING_QUARTERS
- [ ] **AC-7**: GIVEN cat_state=IDLE_LIVING_QUARTERS + 闲置时间 > T_nest_settle (20s)，WHEN 触发，THEN cat_state→IN_NEST
- [ ] **AC-8**: GIVEN cat_state=IN_NEST + 玩家进入生活舱触发半径，WHEN 触发，THEN cat_state→IDLE_LIVING_QUARTERS。猫离开窝

### Hub State Coupling

- [ ] **AC-9**: GIVEN Hub→departure_locked + 猫在任意状态，WHEN hub_state_changed(DEPARTURE_LOCKED)，THEN 猫状态冻结——所有后续 zone 事件不触发状态变更
- [ ] **AC-10**: GIVEN Hub→in_transit，WHEN hub_state_changed(IN_TRANSIT)，THEN 猫不渲染、不可交互。逻辑态保持 idle_living_quarters。query_partner_present() 仍然返回 true
- [ ] **AC-11**: GIVEN Hub→arrival + 猫 pre-departure 状态为 IN_NEST 或任意态，WHEN hub_state_changed(ARRIVAL)，THEN cat_state 强制→IDLE_LIVING_QUARTERS。猫在生活舱暖光区，不在入口（R13）

### Zone Boundary Spam Prevention (E.4.b)

- [ ] **AC-12**: GIVEN cat_state 刚转换完成，WHEN player_entered_zone 在 0.5s 内再次触发，THEN 状态不转换。防抖 cooldown 有效
- [ ] **AC-13**: GIVEN cooldown=0.5s 已过 + 新 zone 事件到达，WHEN 触发，THEN 正常处理转换。cooldown 不永久锁死

### Initialization

- [ ] **AC-14**: GIVEN PartnerManager 在 feature_ready 阶段初始化，WHEN 加载完成，THEN cat_state 从当前 Hub 状态派生。landed → SLEEPING_ON_INTEL_STATION；in_transit → IDLE_LIVING_QUARTERS；arrival → IDLE_LIVING_QUARTERS
- [ ] **AC-15**: GIVEN MVP 唯一伙伴 ID="partner.sky-cat"，WHEN 初始化，THEN partners Dictionary 仅含此一个 key。无第二只伙伴

---

## Implementation Notes

### Cat State Enum & Storage

```gdscript
const CAT_SLEEPING_ON_INTEL_STATION: int = 0
const CAT_IDLE_LIVING_QUARTERS: int = 1
const CAT_FOLLOWING_PLAYER_TO_BENCH: int = 2
const CAT_BENCH_ADJACENT: int = 3
const CAT_SNIFFING: int = 4
const CAT_IN_NEST: int = 5

var cat_state: int = CAT_SLEEPING_ON_INTEL_STATION  # 瞬态——不持久化
var _cat_state_cooldown: float = 0.0
var _state_frozen: bool = false
```

### R2 Presence Contract

```gdscript
func query_partner_present() -> bool:
    return true  # 恒 true——猫永远在飞艇上
```

### State Transition

```gdscript
func _transition_cat_state(target_state: int) -> bool:
    if _state_frozen:
        return false
    if target_state == cat_state:
        return false
    if _cat_state_cooldown > 0.0:
        return false  # E.4.b 防抖

    var old_state := cat_state
    cat_state = target_state
    _cat_state_cooldown = T_CAT_STATE_COOLDOWN
    cat_state_changed.emit(old_state, target_state)
    return true
```

### Hub Event Handlers

```gdscript
func on_hub_state_changed(new_state: int) -> void:
    match new_state:
        HUB_LANDED:
            _state_frozen = false
        HUB_DEPARTURE_LOCKED:
            _state_frozen = true
        HUB_IN_TRANSIT:
            _state_frozen = true  # 简化模拟
        HUB_ARRIVAL:
            _state_frozen = false
            _force_cat_state(CAT_IDLE_LIVING_QUARTERS)  # R13

func on_player_entered_zone(zone_id: StringName) -> void:
    if _state_frozen:
        return
    match zone_id:
        &"living_quarters":
            if cat_state in [CAT_SLEEPING_ON_INTEL_STATION, CAT_IN_NEST]:
                _transition_cat_state(CAT_IDLE_LIVING_QUARTERS)
        &"workbench":
            if cat_state == CAT_IDLE_LIVING_QUARTERS:
                _transition_cat_state(CAT_FOLLOWING_PLAYER_TO_BENCH)
```

### Cooldown Tick

```gdscript
func _process(delta: float) -> void:
    # 仅用于 cooldown 和 sniff lockout 倒计时——不触发状态变更
    if _cat_state_cooldown > 0.0:
        _cat_state_cooldown -= delta
    if _sniff_lockout_remaining > 0.0:
        _sniff_lockout_remaining -= delta
        if _sniff_lockout_remaining <= 0.0:
            # sniff 完成——恢复到 sniff 前状态
            cat_state = _pre_sniff_state
```

---

## Out of Scope

- scout_sniff() 嗅辨算法——属于 Story 002
- 命名和小窝逻辑——属于 Story 003
- Hub 事件的实际信号连接——属于 Story 004
- report_observation_event / reveal_rumor 调用——属于 Story 002
- 猫视觉动画播放——属于 #17 Feedback 系统
- 生活舱暖光区 / 工作台的物理触发区域——属于 Hub #7 场景

---

## QA Test Cases

- **AC-1**: All 4 Hub states → presence=true
- **AC-2**: No absent state code path exists
- **AC-3**: New game → SLEEPING_ON_INTEL_STATION
- **AC-4-8**: All 6 valid state transitions per FSM table
- **AC-9**: departure_locked → state frozen
- **AC-10**: in_transit → simplified simulation
- **AC-11**: arrival → forced IDLE_LIVING_QUARTERS
- **AC-12**: Cooldown blocks rapid re-transition
- **AC-13**: Cooldown expiry → normal transition resumes
- **AC-14**: Init state derivation from Hub context
- **AC-15**: Only partner.sky-cat in partners dict

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/partner-relationships/cat_state_machine_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: airship-hub Epic (Hub states, player_entered_zone), platform-session-shell Epic (Autoload #15 Phase 5)
- Unlocks: Story 002 (sniffing state gate), Story 004 (Hub event signal wiring)
