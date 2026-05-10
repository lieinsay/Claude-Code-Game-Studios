# Story 004: Repair Signal & Resources Integration

> **Epic**: Settlement Market & Port Village Economy
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/port-village-market.md`
**Requirement**: `TR-settlement-002`, `TR-settlement-003`

**ADR Governing Implementation**: ADR-0014 (§3 信号接口, §4a 购买流程, §4b 修复驱动, Architecture Diagram)
**ADR Decision Summary**: SettlementManager 消费 3 条上游信号——#13 repair_completed(node_id) 驱动摊位解锁+NPC 恢复，#4 use_requested(target_id) 打开购买界面，#5 的 purchase_executed 用于审计日志（非购买流程必需）。发送 6 条自有信号——stall_opened, stall_state_changed, npc_state_changed, purchase_completed, purchase_failed, settlement_activity_changed——供 UI (#16) 和 Feedback (#17) 消费。信号连接遵循 ADR-0002：typed params, sync emit, emit-after-mutation, max cascade depth 2。购买流程完整集成链路：玩家按 Use → #4 分发 use_requested → #14 验证 target_id 为已开启摊位 → 发射信号通知 UI 打开界面 → 玩家选择商品 → #14.validate_purchase_request() 委托 #5.validate_purchase() → #14.execute_purchase() 委托 #5.execute_purchase() → 发射 purchase_completed。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 信号连接在 feature_ready 阶段执行——不在 _ready() 中连接；所有信号 emit 在状态变更之后——emit-after-mutation；repair_completed 消费者不阻塞 #13 的信号发射
- Forbidden: 使用 Dictionary 作为信号 payload——所有参数必须 typed；信号级联深度超过 2；在 _process() 中动态 connect/disconnect
- Guardrail: #5 不可用时购买流程安全失败——validate_purchase 返回 false

---

## Acceptance Criteria

### Signal Wiring — repair_completed

- [ ] **AC-1**: GIVEN SettlementManager 收到 feature_ready，WHEN _connect_signals()，THEN WorldRepair.repair_completed.connect(SettlementManager.on_repair_completed)。信号连接建立
- [ ] **AC-2**: GIVEN WorldRepair 发射 repair_completed(&"repair_node.starlight_dock")，WHEN SettlementManager.on_repair_completed() 处理完成，THEN 匹配摊位 stall_opened 信号发射 + settlement_activity_changed 发射。信号级联深度 ≤ 2（repair_completed → stall_opened / settlement_activity_changed → UI/FX 消费）
- [ ] **AC-3**: GIVEN repair_completed 信号到达 + #13 Registry 中 linked_location_id 查询正常，WHEN 处理，THEN 不阻塞 #13 的信号链。#14 的 on_repair_completed 同步执行但快速返回（MVP <0.1ms）

### Signal Wiring — use_requested

- [ ] **AC-4**: GIVEN InteractionRegistry (#4) 发射 use_requested(&"stall.gh-general")，WHEN SettlementManager.on_use_requested() 处理，THEN 验证该 stall_id 是否为已开启摊位。若 OPEN_BASIC → stall_opened 信号通知 UI 打开界面
- [ ] **AC-5**: GIVEN use_requested(&"stall.gh-lens-workshop") + 该摊位为 CLOSED，WHEN on_use_requested()，THEN 无操作。closed 摊位不应被注册为焦点目标——此为防御性检查
- [ ] **AC-6**: GIVEN use_requested(target_id) 的 target_id 不是已知 stall_id，WHEN on_use_requested()，THEN 静默忽略。不崩溃

### Purchase Integration with #5 Resources

- [ ] **AC-7**: GIVEN validate_purchase_request() 调用，WHEN 内部逻辑，THEN 委托 ResourcesManager.validate_purchase(good_id, quantity) 检查货币+容量。SettlementManager 不直接访问 player_currency 或 capacity 字段
- [ ] **AC-8**: GIVEN execute_purchase() 调用，WHEN 逻辑，THEN 委托 ResourcesManager.execute_purchase(good_id, quantity) 执行扣除+转移。SettlementManager 不拥有货物所有权
- [ ] **AC-9**: GIVEN ResourcesManager 不可用（Autoload 尚未就绪），WHEN validate_purchase_request() 中调用，THEN 安全失败返回 {valid: false, reason: "system_unavailable"}。不崩溃

### Interactive Stalls Registration

- [ ] **AC-10**: GIVEN settlement.glass-harbor 的 4 个摊位（1 open + 3 closed），WHEN get_interactive_stalls("settlement.glass-harbor")，THEN 返回仅含 stall.gh-general。closed 摊位不可交互
- [ ] **AC-11**: GIVEN 帆具铺修复后 stall.gh-sail-shop→OPEN_BASIC，WHEN get_interactive_stalls("settlement.glass-harbor")，THEN 返回 [stall.gh-general, stall.gh-sail-shop]

### Signal Documentation — All 6 Settlement Signals

- [ ] **AC-12**: GIVEN SettlementManager 类声明，WHEN 检查，THEN 包含全部 6 个信号：
  - stall_opened(stall_id: StringName, settlement_id: StringName)
  - stall_state_changed(stall_id: StringName, old_state: int, new_state: int)
  - npc_state_changed(npc_id: StringName, old_state: int, new_state: int)
  - purchase_completed(good_id: StringName, quantity: int, total_cost: int)
  - purchase_failed(good_id: StringName, reason: StringName)
  - settlement_activity_changed(settlement_id: StringName, active_stall_count: int)

### Feature Ready Initialization

- [ ] **AC-13**: GIVEN Phase 5 feature_ready 信号到达，WHEN SettlementManager._on_feature_ready()，THEN 顺序执行: (1) _connect_signals() (2) _init_new_game_state() 或 _restore_from_snapshot() (3) 注册所有 open_basic 摊位到 #4 焦点系统

---

## Implementation Notes

### Signal Connection

```text
func _on_feature_ready() -> void:
    _connect_signals()

    # 从 Persistence 恢复或初始化新游戏
    var snapshot := Persistence.restore_snapshot("progress.settlement-market")
    if snapshot.is_empty():
        _init_new_game_state()
    else:
        _deserialize_settlement(snapshot)

    # 注册所有已开启摊位到 #4 焦点系统
    _register_open_stalls()

func _connect_signals() -> void:
    # 消费上游信号
    WorldRepair.repair_completed.connect(on_repair_completed)
    InteractionRegistry.use_requested.connect(on_use_requested)

func _register_open_stalls() -> void:
    for stall_id in stalls:
        if get_stall_state(stall_id) >= STALL_OPEN_BASIC:
            var settlement_id: StringName = stalls[stall_id]["settlement_id"]
            var pos: Vector2 = _get_stall_world_position(stall_id)
            var label: String = _get_stall_label(stall_id)
            InteractionRegistry.register_focus_target(stall_id, pos, label)
```

### use_requested Handler

```text
func on_use_requested(target_id: StringName) -> void:
    if not stalls.has(target_id):
        return  # 不是摊位——静默忽略

    if get_stall_state(target_id) < STALL_OPEN_BASIC:
        return  # closed 摊位不应被注册为焦点目标——防御性检查

    # 发射信号通知 UI 打开购买界面
    stall_opened.emit(target_id, stalls[target_id]["settlement_id"])
```

### Purchase Delegation

```text
func validate_purchase_request(stall_id: StringName, good_id: StringName, quantity: int) -> Dictionary:
    if not _is_resources_available():
        return {"valid": false, "reason": &"system_unavailable", "total_cost": 0}
    # ... (其余逻辑见 Story 002)

func _is_resources_available() -> bool:
    return ResourcesManager != null and ResourcesManager.has_method("validate_purchase")
```

---

## Out of Scope

- InteractionRegistry.register_focus_target() 实现——属于 player-movement-interaction Epic
- WorldRepair.repair_completed 信号发射——属于 world-repair Epic
- ResourcesManager.validate_purchase / execute_purchase 实现——属于 resources-goods-capacity Epic
- Persistence.restore_snapshot / capture_snapshot 实现——属于 local-save-persistence Epic
- 摊位世界位置 (_get_stall_world_position) ——由场景层定义，非 Autoload 拥有
- UI 界面打开/渲染——属于 #16 UIManager

---

## QA Test Cases

- **AC-1**: feature_ready → signals connected
- **AC-2**: repair_completed → stall_opened + activity_changed cascade
- **AC-3**: Signal chain non-blocking (<0.1ms)
- **AC-4**: use_requested(open stall) → stall_opened emitted
- **AC-5**: use_requested(closed stall) → no-op
- **AC-6**: use_requested(unknown) → silent ignore
- **AC-7**: validate → delegates to #5
- **AC-8**: execute → delegates to #5
- **AC-9**: #5 unavailable → safe failure
- **AC-10**: Closed stalls excluded from interactive list
- **AC-11**: Newly opened stall appears in interactive list
- **AC-12**: All 6 signals declared with typed params
- **AC-13**: feature_ready init sequence correct

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/settlement-market/SignalIntegrationTest.csproj` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (purchase flow), Story 003 (unlock logic), world-repair Epic (repair_completed signal), player-movement-interaction Epic (use_requested, register_focus_target), resources-goods-capacity Epic (validate_purchase, execute_purchase)
- Unlocks: Story 006 (UI + edge cases depend on signal wiring)
