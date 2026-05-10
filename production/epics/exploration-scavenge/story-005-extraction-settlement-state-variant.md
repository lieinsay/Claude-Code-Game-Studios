# Story 005: Extraction, Settlement & State Variant Transition

> **Epic**: Exploration / Scavenge Scenario
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/exploration-scavenge-scenario.md`
**Requirement**: `TR-exploration-003`

**ADR Governing Implementation**: ADR-0013 (§4d 提取结算接口, §5e F-11-04 撤离损耗结算, §5f F-11-05 状态变体转换)
**ADR Decision Summary**: EXTRACTING 阶段 2.5s 读条——可被威胁打断（EC-11-11），被打断后进度重置、阶段回 EXPLORING + threatened 子状态。读条完成后进入 DEPARTED 阶段，执行 _finalize_extraction()：1) F-11-04 extraction_loss_settlement() 对 Pool 5 每堆物品独立判定损耗——λ_success=0.08（成功撤离）或 λ_forced=0.25（retreat）；Unique 物品（Q=1, max_stack=1）永不损耗、全量转移；每堆至少保留 1（Q≤1 或 λ≤0 时无损）。2) 通过 extract_carried_to_storage() 批量原子转移至飞艇仓库。3) DEPARTED 结算完成后调用 F-11-05 state_variant_transition() 更新探索点持久状态变体——8 种转换规则，env_threat_active=true 优先进入 danger-changed。4) 结算事务模式——内存中组装完整结算包，一次性写入，失败自动重试（1s/2s/4s/8s，最多 4 次）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: extraction_loss_settlement 必须先判定损耗再批量转移——不可分批；Unique 物品 (Q=1, max_stack=1) 永不损耗，独立于 λ 值；每堆至少保留 1（compute_loss 的 max(0, ceil(Q×λ)) 受限于 min(Q-1, ...)）；撤退提取 (retreat_flagged=true) 使用 λ_forced=0.25
- Forbidden: 在 EXTRACTING 阶段允许玩家移动或交互（除威胁打断）；在 DEPARTED 结算完成前允许玩家进入下一次探索；损耗结算时修改 Unique 物品的 quantity
- Guardrail: DEPARTED 结算写入失败时保留结算包在内存中——提供手动重试按钮；结算包在应用关闭请求前一直保留

---

## Acceptance Criteria

### Extraction Channel

- [ ] **AC-1**: GIVEN session_phase=EXPLORING + 玩家在撤离锚点按 E，WHEN trigger_extraction()，THEN session_phase→EXTRACTING + extraction_started 发射 + 2.5s 读条开始
- [ ] **AC-2**: GIVEN session_phase=EXTRACTING + 读条进行中 (progress=0.5)，WHEN 威胁触发（check_threat_trigger 返回 triggered=true），THEN _interrupt_extraction() 调用 + extraction_interrupted 发射 + 进度重置为 0 + session_phase→EXPLORING (threatened)。EC-11-11 满足
- [ ] **AC-3**: GIVEN 读条被打断 + 威胁处理完毕，WHEN 玩家再次按 E 在撤离锚点，THEN 可重新触发提取——读条重新开始，无额外惩罚
- [ ] **AC-4**: GIVEN session_phase=EXTRACTING + 无威胁打断，WHEN 2.5s 到期，THEN extraction_completed 信号发射 + _finalize_extraction() 执行

### F-11-04: Extraction Loss Settlement

- [ ] **AC-5**: GIVEN carried_stacks=[{resource_id: "basic_supply", quantity: 20, is_unique: false, max_stack: 20}] + retreat_flagged=false, λ_success=0.08，WHEN extraction_loss_settlement()，THEN compute_loss(20, 0.08) = ceil(20×0.08)=2, 保留 18, 损耗 2
- [ ] **AC-6**: GIVEN retreat_flagged=true + λ_forced=0.25 + basic_supply×20，WHEN extraction_loss_settlement()，THEN compute_loss(20, 0.25) = ceil(20×0.25)=5, 保留 15, 损耗 5
- [ ] **AC-7**: GIVEN Unique 物品 (Q=1, max_stack=1, is_unique=true)，WHEN extraction_loss_settlement()，THEN 跳过损耗判定 + 全量转移 + lost=0。无论 retreat_flagged 为何值
- [ ] **AC-8**: GIVEN non-Unique 物品 Q=1（如 cloud_crystal×1），WHEN compute_loss(1, 0.08)，THEN 返回 0——Q≤1 不损耗
- [ ] **AC-9**: GIVEN non-Unique 物品 Q=3 + λ=0.08，WHEN compute_loss(3, 0.08)，THEN ceil(3×0.08)=1, min(3-1, 1)=1, 保留 2, 损耗 1
- [ ] **AC-10**: GIVEN 多堆混合（Unique ×1, non-Unique ×2），WHEN extraction_loss_settlement()，THEN extract_carried_to_storage 收到完整 transfer_batch——原子全成功或全失败
- [ ] **AC-11**: GIVEN λ=0.0（配置为无损），WHEN extraction_loss_settlement()，THEN compute_loss 对所有 Q 返回 0——零损耗

### F-11-05: State Variant Transition

- [ ] **AC-12**: GIVEN current_state=UNLOOTED + all_searched=false + env_threat_active=false，WHEN state_variant_transition()，THEN 返回 UNLOOTED
- [ ] **AC-13**: GIVEN current_state=UNLOOTED + all_searched=true + env_threat_active=false，WHEN state_variant_transition()，THEN 返回 LOOTED
- [ ] **AC-14**: GIVEN current_state=UNLOOTED + any all_searched + env_threat_active=true，WHEN state_variant_transition()，THEN 返回 DANGER_CHANGED（优先规则）
- [ ] **AC-15**: GIVEN current_state=LOOTED + env_threat_active=true，WHEN state_variant_transition()，THEN 返回 DANGER_CHANGED
- [ ] **AC-16**: GIVEN current_state=LOOTED + env_threat_active=false，WHEN state_variant_transition()，THEN 返回 LOOTED（保持）
- [ ] **AC-17**: GIVEN current_state=DANGER_CHANGED + all_searched=false + env_threat_active=false，WHEN state_variant_transition()，THEN 返回 UNLOOTED（威胁清除+未搜完→回退）
- [ ] **AC-18**: GIVEN current_state=DANGER_CHANGED + all_searched=true + env_threat_active=false，WHEN state_variant_transition()，THEN 返回 LOOTED（威胁清除+全搜→终态）
- [ ] **AC-19**: GIVEN current_state=DANGER_CHANGED + env_threat_active=true，WHEN state_variant_transition()，THEN 返回 DANGER_CHANGED（保持）

### DEPARTED Settlement

- [ ] **AC-20**: GIVEN DEPARTED 阶段 + 结算执行，WHEN _finalize_extraction()，THEN 执行顺序：(1) F-11-04 损耗结算 + 批量转移，(2) 情报结算写入 IntelManager，(3) 船体后果汇总展示，(4) F-11-05 状态变体更新，(5) 持久化快照，(6) extraction_completed 信号发射
- [ ] **AC-21**: GIVEN DEPARTED 结算写入失败（模拟 user:// storage 满），WHEN 触发 EC-11-03 重试逻辑，THEN 自动重试 1s→2s→4s→8s（最多 4 次）。全部失败后显示"保存失败。你的探索收获暂时保留。请检查本地存储空间后点击重试。"+ 手动重试按钮
- [ ] **AC-22**: GIVEN extraction_completed 发射后，WHEN Platform #2 收到信号，THEN 过渡回 Hub 场景。ExplorationManager session_phase→IDLE

---

## Implementation Notes

### Extraction Loss Settlement

```text
const LAMBDA_SUCCESS: float = 0.08
const LAMBDA_FORCED: float = 0.25

func extraction_loss_settlement(carried_stacks: Array, retreat_flagged: bool) -> Dictionary:
    var transfer_batch := []
    var result := {transferred: [], lost: [], total_lost_qty: 0}

    for stack in carried_stacks:
        var is_unique: bool = stack.get("is_unique", false)
        var max_stack: int = stack.get("max_stack", 99)
        var quantity: int = stack.get("quantity", 0)
        var resource_id: StringName = stack.get("resource_id", &"")

        if is_unique and max_stack == 1:
            transfer_batch.append({resource_id: resource_id, quantity: quantity})
            result.transferred.append({id: resource_id, qty: quantity, lost: 0})
            continue

        var lambda := LAMBDA_FORCED if retreat_flagged else LAMBDA_SUCCESS
        var loss_qty := compute_loss(quantity, lambda)
        var retained_qty := quantity - loss_qty
        transfer_batch.append({resource_id: resource_id, quantity: retained_qty})

        if loss_qty > 0:
            result.lost.append({id: resource_id, qty: loss_qty})
            result.total_lost_qty += loss_qty
        result.transferred.append({id: resource_id, qty: retained_qty, lost: loss_qty})

    var success := ResourcesManager.extract_carried_to_storage(transfer_batch)
    if not success:
        push_error("Exploration: extract_carried_to_storage failed")
        # EC-11-03 重试逻辑
        _schedule_settlement_retry(transfer_batch, result)

    return result

func compute_loss(qty: int, lambda: float) -> int:
    if qty <= 1:
        return 0
    if lambda <= 0.0:
        return 0
    return mini(qty - 1, maxi(0, ceili(float(qty) * lambda)))
```

### State Variant Transition

```text
func state_variant_transition(current_state: int, all_searched: bool, env_threat_active: bool) -> int:
    if env_threat_active:
        return STATE_DANGER_CHANGED  # 优先规则

    match current_state:
        STATE_UNLOOTED:
            return STATE_LOOTED if all_searched else STATE_UNLOOTED
        STATE_LOOTED:
            return STATE_LOOTED  # env_threat 已处理
        STATE_DANGER_CHANGED:
            if all_searched:
                return STATE_LOOTED
            else:
                return STATE_UNLOOTED

    return current_state
```

### Finalize Extraction

```text
func _finalize_extraction() -> void:
    # (1) 损耗结算 + 批量转移
    var carried := ResourcesManager.get_pool_contents("pool_5")
    var settlement := extraction_loss_settlement(carried, session_retreat_flagged)

    # (2) 情报结算
    for intel_id in session_intel_interacted:
        if session_intel_interacted[intel_id]:
            IntelManager.reveal_from_exploration(intel_id)

    # (3) 船体后果汇总（已在触发时即时写入，此处汇总展示）
    var damage_summary := _build_damage_summary()

    # (4) 状态变体更新
    var all_searched := _all_search_points_consumed()
    var new_state := state_variant_transition(
        get_exploration_point_state(current_exploration_point_id),
        all_searched,
        _has_active_environmental_threat()
    )
    exploration_points[current_exploration_point_id].state_variant = new_state

    # (5) 持久化
    var success := _trigger_settlement_snapshot(settlement, damage_summary)
    if not success:
        _attempt_settlement_retry(settlement, damage_summary, 0)
        return

    # (6) 信号
    extraction_completed.emit(settlement.transferred.size(), session_intel_interacted.size())

    # (7) 回 IDLE — 由 Platform #2 在收到 extraction_completed 后触发
    _transition_phase(PHASE_DEPARTED)
```

### Settlement Retry

```text
const RETRY_DELAYS := [1.0, 2.0, 4.0, 8.0]  # 秒

var _pending_settlement: Dictionary = {}  # 保留在内存中直到写入成功
var _retry_count: int = 0

func _attempt_settlement_retry(settlement: Dictionary, damage_summary: Dictionary, attempt: int) -> void:
    if attempt >= RETRY_DELAYS.size():
        _pending_settlement = {settlement: settlement, damage_summary: damage_summary}
        # 通知 UI 显示手动重试按钮
        _emit_settlement_failed_ui()
        return

    _retry_count = attempt
    # 使用场景层提供的延迟回调机制 (非 await——避免跨帧状态不一致)
    _schedule_retry_callback(RETRY_DELAYS[attempt], func():
        var success := _trigger_settlement_snapshot(settlement, damage_summary)
        if not success:
            _attempt_settlement_retry(settlement, damage_summary, attempt + 1)
        else:
            _pending_settlement = {}
            extraction_completed.emit(settlement.transferred.size(), session_intel_interacted.size())
            _transition_phase(PHASE_DEPARTED)
    )
```

---

## Out of Scope

- ResourcesManager.extract_carried_to_storage() 的批量原子转移实现——属于 resources-goods-capacity Epic
- IntelManager.reveal_from_exploration() 实现——属于 intel-knowledge Epic
- 结算摘要 UI 面板——属于 #16 UIManager
- 提取读条 UI（进度条）——由场景层渲染
- 船体损伤脉冲动画——由场景层/Feedback #17 负责

---

## QA Test Cases

- **AC-1–4**: Extraction channel lifecycle (start → progress → interrupt → retry → complete)
- **AC-5–6**: λ_success=0.08 vs λ_forced=0.25 loss calculation
- **AC-7**: Unique item protection (loss=0 always)
- **AC-8–9**: Q≤1 edge cases
- **AC-10**: Mixed stacks atomic batch transfer
- **AC-12–19**: All 8 state variant transitions
- **AC-20**: Settlement execution order
- **AC-21**: Settlement retry fallback (1s/2s/4s/8s)
- **AC-22**: Post-extraction Hub transition

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/exploration/ExtractionSettlementTest.csproj` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (状态机), Story 002 (Pool 5 容量), Story 003 (retreat_flagged 标记), resources-goods-capacity Epic (extract_carried_to_storage, Pool 5), intel-knowledge Epic (reveal_from_exploration), persistence Epic (ADR-0003 snapshot)
- Unlocks: Story 006 (DEPARTED 持久化边界情况)
