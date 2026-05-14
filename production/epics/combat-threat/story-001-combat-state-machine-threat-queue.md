# Story 001: Combat State Machine & Threat Queue

> **Epic**: Combat / Threat Resolution
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/combat-threat-handling.md`
**Requirement**: `TR-combat-001`

**ADR Governing Implementation**: ADR-0018 (CombatManager Autoload #12, 4-state micro state machine, threat queue FIFO max depth 4, re-entrancy guard)
**ADR Decision Summary**: CombatManager 在 Phase 5 中初始化为 Autoload #12。`_ready()` 仅执行信号声明和内部常量定义（≤5ms）。核心为 4 态微观状态机：IDLE（无活跃威胁）→ AWAITING_RESPONSE（决策呼吸，探索暂停）→ PROCESSING（结算序列执行，≤1 帧）→ RESOLVED（combat_result 返回 #11，1 帧后转 IDLE）。重入防护：状态 ≠ IDLE 时，到达的 threat_context 加入 FIFO 队列（最大深度 4）。当前威胁结算完成后自动出队处理。队列满时最早条目被丢弃 + 警告日志。同一时间只有一个威胁处于活跃结算状态。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 状态机转换必须不可逆（PROCESSING 不能回退到 AWAITING_RESPONSE）；队列 FIFO 顺序保证；resolve_threat() 在状态 ≠ IDLE 时返回 ERR_BUSY
- Forbidden: 跳过队列直接处理新到达的 threat_context；在 AWAITING_RESPONSE 期间恢复探索移动
- Guardrail: 队列深度上限 4——溢出丢弃 + 警告日志；_ready() ≤ 5ms

---

## Acceptance Criteria

### State Machine Transitions

- [ ] **AC-1**: GIVEN CombatManager 处于 IDLE + threat_context 到达，WHEN resolve_threat() 调用，THEN state: IDLE → AWAITING_RESPONSE。探索暂停
- [ ] **AC-2**: GIVEN AWAITING_RESPONSE + 玩家选择响应选项，WHEN _submit_response(response_choice)，THEN state: AWAITING_RESPONSE → PROCESSING
- [ ] **AC-3**: GIVEN PROCESSING + 结算序列（C4 步骤 1-9）完成，WHEN 结算结束，THEN state: PROCESSING → RESOLVED。combat_result 已构建
- [ ] **AC-4**: GIVEN RESOLVED + combat_result 已返回 #11，WHEN #11 恢复探索控制，THEN state: RESOLVED → IDLE（1 帧内）
- [ ] **AC-5**: GIVEN 任何状态进入 PROCESSING，WHEN 尝试回退到 AWAITING_RESPONSE，THEN 拒绝——PROCESSING 不可逆。结算序列一旦开始必须完成

### Re-entrancy Guard

- [ ] **AC-6**: GIVEN state ≠ IDLE（AWAITING_RESPONSE / PROCESSING / RESOLVED），WHEN 新的 resolve_threat() 调用到达，THEN 返回 ERR_BUSY 或等效错误。threat_context 加入队列等待处理
- [ ] **AC-7**: GIVEN state = AWAITING_RESPONSE + 同一威胁重复触发（如玩家在决策呼吸期间走回触发半径），WHEN #11 再次调用 resolve_threat()，THEN 拒绝重复的同一 threat_id（队列去重）

### Threat Queue

- [ ] **AC-8**: GIVEN PROCESSING 中 + 2 个新 threat_context 到达，WHEN 当前结算完成 + RESOLVED→IDLE，THEN 队列头部 threat_context 自动出队 → resolve_threat() → IDLE→AWAITING_RESPONSE。FIFO 顺序
- [ ] **AC-9**: GIVEN 队列中有 4 个待处理威胁（满），WHEN 第 5 个 threat_context 到达，THEN 最早进入队列的条目被丢弃。记录警告日志："threat queue full — dropping oldest entry [threat_id]"
- [ ] **AC-10**: GIVEN 队列中有待处理威胁 + 当前结算完成，WHEN 出队处理下一个，THEN 验证 threat_id 对应的威胁点仍 active（未被前一个结算的应急处理清除）。若已 suppressed → 跳过该条目

### CombatManager Autoload Initialization

- [ ] **AC-11**: GIVEN CombatManager 在 Phase 5 中初始化，WHEN `_ready()` 执行，THEN 仅声明信号和内部常量。不查询 #5/#8/#11——在收到 `feature_ready` 信号后初始化威胁队列
- [ ] **AC-12**: GIVEN `_ready()` 执行，WHEN 计时，THEN ≤ 5ms。无 `_process()` / `_physics_process()`——纯事件驱动

### AWAITING_RESPONSE State Behavior

- [ ] **AC-13**: GIVEN state = AWAITING_RESPONSE，WHEN 处理，THEN 探索移动暂停。决策面板显示。不限时——无计时器压力
- [ ] **AC-14**: GIVEN AWAITING_RESPONSE + 玩家按 Esc 关闭面板查看小地图，WHEN 操作，THEN 面板关闭但状态保持 AWAITING_RESPONSE。屏幕顶部显示"威胁活跃"指示器。点击指示器重新打开面板。快捷键仍可提交响应

---

## Implementation Notes

### State Machine

```text
# CombatManager Autoload #12
enum CombatState {
    IDLE,
    AWAITING_RESPONSE,
    PROCESSING,
    RESOLVED,
}

var _state: int = CombatState.IDLE
var _threat_queue: Array[Dictionary] = []
const MAX_QUEUE_DEPTH: int = 4

signal threat_resolved(outcome: String, threat_id: StringName)
signal threat_suppressed(threat_id: StringName)
signal threat_tanked(threat_id: StringName, hull_damage: int)
signal threat_retreated(threat_id: StringName)


func resolve_threat(threat_context: Dictionary) -> Dictionary:
    if _state != CombatState.IDLE:
        _enqueue_threat(threat_context)
        return {"error": "ERR_BUSY", "queued": true}

    _state = CombatState.AWAITING_RESPONSE
    _current_threat_context = threat_context

    # 通知 #16 显示决策面板 —— #16 消费此信号
    threat_triggered.emit(threat_context)

    # 等待玩家响应（通过 _submit_response 回调）
    return {"status": "awaiting_response"}


func _enqueue_threat(threat_context: Dictionary) -> void:
    var threat_id: StringName = threat_context.get("threat_id", &"")
    # 去重：同一 threat_id 已在队列中或正在处理中 → 跳过
    if threat_id == _current_threat_context.get("threat_id", &""):
        return
    for entry in _threat_queue:
        if entry.get("threat_id", &"") == threat_id:
            return

    if _threat_queue.size() >= MAX_QUEUE_DEPTH:
        var dropped: Dictionary = _threat_queue.pop_front()
        push_warning("Combat: threat queue full — dropping oldest entry %s" % dropped.get("threat_id", &"?"))

    _threat_queue.append(threat_context)
```

### State Transitions

```text
func _submit_response(response_choice: StringName) -> void:
    if _state != CombatState.AWAITING_RESPONSE:
        return

    _state = CombatState.PROCESSING
    var result: Dictionary = _execute_settlement(response_choice)
    _state = CombatState.RESOLVED

    # 返回 combat_result 给 #11
    combat_result_ready.emit(result)

    # 发射威胁结算信号
    _emit_resolution_signals(result)

    # 1 帧后转 IDLE（或处理队列中下一个威胁）
    _transition_to_idle_or_next.call_deferred()


func _transition_to_idle_or_next() -> void:
    _state = CombatState.IDLE
    _current_threat_context = {}
    _current_response_choice = &""

    # 处理队列中下一个威胁
    if not _threat_queue.is_empty():
        var next_threat: Dictionary = _threat_queue.pop_front()
        # 验证威胁仍 active
        if _is_threat_still_active(next_threat.get("threat_id", &"")):
            resolve_threat(next_threat)
        else:
            # 威胁已被应急处理清除——跳过
            _transition_to_idle_or_next.call_deferred()
```

### Threat Queue Management

```text
func get_queue_depth() -> int:
    return _threat_queue.size()


func clear_queue() -> void:
    _threat_queue.clear()


func _is_threat_still_active(threat_id: StringName) -> bool:
    # 查询 #11 的威胁点状态
    return Exploration.is_threat_active(threat_id)
```

### Signal Declarations

```text
signal threat_triggered(threat_context: Dictionary)
# 消费方: #16 (UI — 显示决策面板), #17 (Feedback — 警报音)

signal combat_result_ready(result: Dictionary)
# 消费方: #11 (Exploration — 恢复探索状态, 应用击退)
```

---

## Out of Scope

- 决策面板 UI 的具体实现——属于 #16 UIManager
- 结算序列的具体执行（步骤 1-9）——属于 Story 002
- combat_result 的信号事件消费——属于 Story 004
- 威胁配置的 Registry 查询——属于 Story 005

---

## QA Test Cases

- **AC-1 through AC-5**: State transitions
  - IDLE→AWAITING_RESPONSE→PROCESSING→RESOLVED→IDLE; PROCESSING 不可逆

- **AC-6/7**: Re-entrancy
  - state≠IDLE → ERR_BUSY; duplicate threat_id → rejected

- **AC-8/9/10**: Queue behavior
  - FIFO order; overflow → drop oldest + warn; suppressed threat → skipped

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/combat/StateMachineTest.csproj` — must exist and pass
**Status**: [x] Passing — `dotnet run --project tests/unit/combat/StateMachineTest.csproj -p:UseSharedCompilation=false` (7/7 PASS, 2026-05-14)

## Completion Evidence — 2026-05-14

- Implemented in `src/core/combat/CombatManager.cs`.
- Test runner: `tests/unit/combat/StateMachineTest.csproj`.
- Acceptance coverage:
  - AC-1 through AC-4: state flow IDLE -> AWAITING_RESPONSE -> PROCESSING -> RESOLVED -> IDLE.
  - AC-5: PROCESSING cannot regress to AWAITING_RESPONSE.
  - AC-6/7: busy calls return ERR_BUSY, distinct threats queue, duplicate `threat_id` is deduplicated.
  - AC-8/10: FIFO drain and inactive/suppressed threat skip.
  - AC-9: queue overflow drops oldest and records warning.
  - AC-11/12: constructor initialization stays light and event-driven.
  - AC-13/14: decision breath remains untimed and inspectable while state stays AWAITING_RESPONSE.

---

## Dependencies

- Depends on: modules-hull-state Epic (hull integrity queries), exploration-scavenge Epic (#11 threat triggering interface), ADR-0018
- Unlocks: Story 002 (response resolution), Story 004 (signal events), Story 006 (edge cases)
