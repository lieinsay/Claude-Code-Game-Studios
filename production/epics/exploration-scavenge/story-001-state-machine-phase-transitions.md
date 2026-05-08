# Story 001: Exploration State Machine & Phase Transitions

> **Epic**: Exploration / Scavenge Scenario
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/exploration-scavenge-scenario.md`
**Requirement**: `TR-exploration-001`

**ADR Governing Implementation**: ADR-0013 (§1 ExplorationManager Autoload #11, §2 Dictionary 后端存储, §5a 4 阶段状态机, §3 信号接口)
**ADR Decision Summary**: ExplorationManager 作为 Autoload #11，Phase 5 feature_ready 初始化。管理 4 阶段探索会话状态机：IDLE → ARRIVING → EXPLORING → EXTRACTING → DEPARTED → IDLE。EXPLORING 内含 5 种子状态（idle, moving, searching, threatened, extracting_sub）。IDLE 跳过已加载的从存档恢复的活跃会话（由 Story 006 处理）。7 种无效转换被拒绝。状态存储在 Dictionary[StringName, Variant] 中——session_phase、session_substate、current_exploration_point_id、encounter_context。探索点持久状态独立存储为 exploration_points: Dict[StringName, ExplorationPointState]。逻辑/场景分离——ExplorationManager 不含 2D 场景节点引用，通过信号与方法调用与场景层通信。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 所有状态转换通过单一 `_transition_phase()` 函数——禁止外部直接修改 session_phase；ARRIVING 必须经过才能进入 EXPLORING；EXTRACTING 必须经过才能进入 DEPARTED；EXPLORING 子状态仅在该阶段内有效
- Forbidden: IDLE → EXPLORING (跳过 ARRIVING)；EXPLORING → DEPARTED (跳过 EXTRACTING 读条)；在非 ARRIVING 阶段调用 skip_arriving()；在非 EXPLORING 阶段调用 trigger_extraction()
- Guardrail: 阶段转换时先发射 exploration_phase_changed 信号，再执行进入逻辑——确保消费者在状态变更前收到通知

---

## Acceptance Criteria

### State Machine Core

- [ ] **AC-1**: GIVEN 启动完成 + feature_ready 已发射，WHEN ExplorationManager 初始化，THEN session_phase=PHASE_IDLE, session_substate=SUBSTATE_IDLE, current_exploration_point_id=&"", encounter_context={}
- [ ] **AC-2**: GIVEN session_phase=IDLE + 收到有效 EncounterContext，WHEN enter_exploration(ctx)，THEN session_phase→ARRIVING, current_exploration_point_id 设置为 destination_id, exploration_phase_changed 信号发射 (IDLE, ARRIVING, point_id)
- [ ] **AC-3**: GIVEN session_phase=ARRIVING，WHEN skip_arriving() 调用，THEN session_phase→EXPLORING, exploration_phase_changed 发射 (ARRIVING, EXPLORING, point_id)。子状态初始为 SUBSTATE_IDLE
- [ ] **AC-4**: GIVEN session_phase=EXPLORING + 玩家在撤离锚点触发提取，WHEN trigger_extraction() 调用，THEN session_phase→EXTRACTING, extraction_started 信号发射。读条计时器启动（2.5s）
- [ ] **AC-5**: GIVEN session_phase=EXTRACTING + 读条完成 2.5s 未被中断，WHEN 计时器到期，THEN session_phase→DEPARTED, _finalize_extraction() 执行结算, exploration_phase_changed 发射 (EXTRACTING, DEPARTED, point_id)
- [ ] **AC-6**: GIVEN session_phase=DEPARTED + 结算完成，WHEN 过渡回 Hub 触发，THEN session_phase→IDLE。会话瞬时状态清空

### Invalid Transition Rejection

- [ ] **AC-7**: GIVEN session_phase=IDLE，WHEN 调用 skip_arriving() 或 trigger_extraction() 或 force_extraction()，THEN 无操作——记录 warning
- [ ] **AC-8**: GIVEN session_phase=ARRIVING，WHEN 调用 trigger_extraction() 或 force_extraction()，THEN 状态机拒绝——必须先进入 EXPLORING
- [ ] **AC-9**: GIVEN session_phase=EXPLORING，WHEN 直接调用 _finalize_extraction() 跳过 EXTRACTING，THEN 状态机拒绝——必须经过 EXTRACTING 读条
- [ ] **AC-10**: GIVEN session_phase=EXTRACTING，WHEN 调用 enter_exploration() 或 skip_arriving()，THEN 无操作——提取中不可重新进入
- [ ] **AC-11**: GIVEN session_phase=DEPARTED，WHEN 调用任意阶段转换（除返回 IDLE），THEN 状态机拒绝——结算完成后只接受回到 IDLE
- [ ] **AC-12**: GIVEN session_phase != ARRIVING，WHEN 调用 skip_arriving()，THEN 无操作
- [ ] **AC-13**: GIVEN session_phase != EXPLORING，WHEN 调用 trigger_extraction() 或 force_extraction()，THEN 无操作

### EXPLORING Sub-states

- [ ] **AC-14**: GIVEN session_phase=EXPLORING + session_substate=SUBSTATE_IDLE，WHEN 玩家开始移动，THEN 场景层通知→session_substate→SUBSTATE_MOVING。停止移动→SUBSTATE_IDLE
- [ ] **AC-15**: GIVEN session_phase=EXPLORING + session_substate=SUBSTATE_IDLE，WHEN perform_search(sp_id) 调用，THEN session_substate→SUBSTATE_SEARCHING。搜索动画完成后→SUBSTATE_IDLE
- [ ] **AC-16**: GIVEN session_phase=EXPLORING + 威胁触发，WHEN _handle_environmental_threat 或 _handle_guard_threat 执行，THEN session_substate→SUBSTATE_THREATENED。威胁结算完成后→SUBSTATE_IDLE
- [ ] **AC-17**: GIVEN session_substate=SUBSTATE_SEARCHING 或 SUBSTATE_THREATENED，WHEN 尝试触发新的搜索或提取，THEN 排队或拒绝——不能同时进行多个操作

### Forced Extraction Triggers

- [ ] **AC-18**: GIVEN session_phase=EXPLORING + Pool 5 耗尽（5/5 格全空），WHEN Pool 5 状态变更触发检查，THEN force_extraction(EXTRACTION_FORCED)
- [ ] **AC-19**: GIVEN session_phase=EXPLORING + 所有搜索点已搜 + 所有情报点已交互，WHEN 检查条件，THEN 不强制提取——玩家可自行判断撤离时机（EC-11-16）

---

## Implementation Notes

### Phase & Sub-state Enums

```gdscript
# ExplorationManager Autoload #11 — 阶段枚举
const PHASE_IDLE: int = 0
const PHASE_ARRIVING: int = 1
const PHASE_EXPLORING: int = 2
const PHASE_EXTRACTING: int = 3
const PHASE_DEPARTED: int = 4

# EXPLORING 子状态枚举
const SUBSTATE_IDLE: int = 0
const SUBSTATE_MOVING: int = 1
const SUBSTATE_SEARCHING: int = 2
const SUBSTATE_THREATENED: int = 3
const SUBSTATE_EXTRACTING_SUB: int = 4

# 提取原因枚举
const EXTRACTION_PLAYER: StringName = &"player_initiated"
const EXTRACTION_FORCED: StringName = &"pool_depleted"
const EXTRACTION_RETREAT: StringName = &"retreat"
```

### Phase Transition Function

```gdscript
func _transition_phase(target_phase: int, reason: StringName = &"") -> bool:
    var current := session_phase

    match target_phase:
        PHASE_ARRIVING:
            if current == PHASE_IDLE:
                session_phase = PHASE_ARRIVING
                exploration_phase_changed.emit(current, PHASE_ARRIVING, current_exploration_point_id)
                return true
            push_warning("Exploration: cannot enter ARRIVING from phase %d" % current)
            return false

        PHASE_EXPLORING:
            if current == PHASE_ARRIVING:
                session_phase = PHASE_EXPLORING
                session_substate = SUBSTATE_IDLE
                exploration_phase_changed.emit(current, PHASE_EXPLORING, current_exploration_point_id)
                return true
            if current == PHASE_EXTRACTING:
                # 提取被打断 → 回到 EXPLORING
                session_phase = PHASE_EXPLORING
                session_substate = SUBSTATE_IDLE
                exploration_phase_changed.emit(current, PHASE_EXPLORING, current_exploration_point_id)
                return true
            push_warning("Exploration: cannot enter EXPLORING from phase %d" % current)
            return false

        PHASE_EXTRACTING:
            if current == PHASE_EXPLORING:
                session_phase = PHASE_EXTRACTING
                exploration_phase_changed.emit(current, PHASE_EXTRACTING, current_exploration_point_id)
                return true
            push_warning("Exploration: cannot enter EXTRACTING from phase %d" % current)
            return false

        PHASE_DEPARTED:
            if current == PHASE_EXTRACTING:
                session_phase = PHASE_DEPARTED
                exploration_phase_changed.emit(current, PHASE_DEPARTED, current_exploration_point_id)
                return true
            push_warning("Exploration: cannot enter DEPARTED from phase %d" % current)
            return false

        PHASE_IDLE:
            if current == PHASE_DEPARTED:
                session_phase = PHASE_IDLE
                _clear_session_state()
                exploration_phase_changed.emit(current, PHASE_IDLE, &"")
                return true
            return false

    push_error("Exploration: invalid target_phase: %d" % target_phase)
    return false
```

### Extraction Timer

```gdscript
const EXTRACTION_DURATION: float = 2.5  # 秒

var _extraction_elapsed: float = 0.0
var _extraction_active: bool = false

func trigger_extraction() -> void:
    if session_phase != PHASE_EXPLORING:
        return
    _transition_phase(PHASE_EXTRACTING)
    _extraction_elapsed = 0.0
    _extraction_active = true
    extraction_started.emit(EXTRACTION_PLAYER)

func force_extraction(reason: StringName) -> void:
    if session_phase != PHASE_EXPLORING:
        return
    _transition_phase(PHASE_EXTRACTING)
    _extraction_elapsed = 0.0
    _extraction_active = true
    extraction_started.emit(reason)

# 每帧由场景层调用（或通过 _process）
func _extraction_tick(delta: float) -> void:
    if not _extraction_active or session_phase != PHASE_EXTRACTING:
        return
    _extraction_elapsed += delta
    var progress := clampf(_extraction_elapsed / EXTRACTION_DURATION, 0.0, 1.0)
    extraction_progress_changed.emit(progress)

    if _extraction_elapsed >= EXTRACTION_DURATION:
        _extraction_active = false
        _finalize_extraction()

func _interrupt_extraction(reason: StringName) -> void:
    _extraction_active = false
    _extraction_elapsed = 0.0
    extraction_interrupted.emit(reason)
    _transition_phase(PHASE_EXPLORING)
```

---

## Out of Scope

- 搜索/情报/威胁的具体交互逻辑——属于 Story 002、Story 003
- EncounterContext 消费与入场模式分流——属于 Story 004
- 提取结算（F-11-04 损耗、F-11-05 状态变体）——属于 Story 005
- 持久化与恢复——属于 Story 006
- 2D 场景层的渲染与输入——由场景层（Platform #2 / ExplorationScene）负责

---

## QA Test Cases

- **AC-1**: Post-init state = IDLE with empty fields
- **AC-2**: Valid EncounterContext → ARRIVING, signal emitted
- **AC-3**: skip_arriving() → EXPLORING, signal emitted
- **AC-4**: trigger_extraction() → EXTRACTING, timer starts
- **AC-5**: Timer completes → DEPARTED, settlement runs
- **AC-7–13**: All 7 invalid transitions rejected with warning
- **AC-14–16**: Sub-state transitions correctly (idle↔moving, idle→searching→idle, idle→threatened→idle)
- **AC-17**: Operations blocked during searching/threatened
- **AC-18**: Pool 5 empty → force_extraction triggered

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/exploration/state_machine_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: content-registry Epic (探索点模板定义)
- Unlocks: Story 002 (search/scavenge 依赖状态机已实现), Story 003 (threat triggering 依赖状态机), Story 004 (ARRIVING entry 依赖状态机)
