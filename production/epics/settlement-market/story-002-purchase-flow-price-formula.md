# Story 002: Purchase Flow & Price Formula

> **Epic**: Settlement Market & Port Village Economy
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/port-village-market.md`
**Requirement**: `TR-settlement-002`

**ADR Governing Implementation**: ADR-0014 (§4a 购买流程, §5b F.1 购买总价, §3 信号接口)
**ADR Decision Summary**: 购买流程由 SettlementManager 拥有商品定义与定价，委托 ResourcesManager (#5) 执行资源转移。流程：use_requested → 打开摊位界面 → 选择商品 → validate_purchase_request() → execute_purchase()。validate_purchase_request() 验证摊位已开启、商品当前解锁等级可用、计算 total_cost = price × quantity，然后委托 #5.validate_purchase(good_id, quantity) 检查货币+容量。execute_purchase() 防御性二次验证后调用 #5.execute_purchase() 扣除货币、转移货物至 Pool 2 (in_storage)，发射 purchase_completed 或 purchase_failed 信号。商品价格从 Registry good 实体读取，不硬编码。购买失败原因常量：PURCHASE_FAIL_CAPACITY / PURCHASE_FAIL_FUNDS。MVP 无库存耗尽——商品解锁后始终可购买。quantity 输入边界：range ∈ [1, max_affordable]，max_affordable = min(floor(player_currency / price), remaining_capacity)。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: validate→execute 在同一帧内完成——购买为原子操作；total_cost 从 Registry good.price 读取——不硬编码价格；购买前必须验证 stall_state ≥ OPEN_BASIC
- Forbidden: 跳过 validate 直接 execute；对 closed 摊位执行购买；quantity ≤ 0 时允许购买
- Guardrail: price=0 时购买不扣货币但记录 error 日志——配置错误不应崩溃；货币恰好等于 total_cost 时购买成功（边界值 ≤ 判定）

---

## Acceptance Criteria

### F.1 Total Cost Calculation

- [x] **AC-1**: GIVEN good.basic-supply-bundle price=50 + quantity=3，WHEN calculate_total_cost("good.basic-supply-bundle", 3)，THEN 返回 150
- [x] **AC-2**: GIVEN good.route-notes price=120 + quantity=1，WHEN calculate_total_cost("good.route-notes", 1)，THEN 返回 120
- [x] **AC-3**: GIVEN 任意 good_id + quantity=0，WHEN calculate_total_cost()，THEN 返回 0。但 quantity=0 在 UI 层已被阻止（AC-13）

### Purchase Validation

- [x] **AC-4**: GIVEN stall_state=OPEN_BASIC + good_id 在该摊位解锁等级下可用 + 货币充足 + 容量足够，WHEN validate_purchase_request(stall_id, good_id, quantity)，THEN 返回 {valid: true, reason: "", total_cost: N}
- [x] **AC-5**: GIVEN stall_state=CLOSED，WHEN validate_purchase_request(stall_id, good_id, quantity)，THEN 返回 {valid: false, reason: "stall_closed", total_cost: 0}
- [x] **AC-6**: GIVEN 货币不足 (player_currency < total_cost)，WHEN validate_purchase_request()，THEN 返回 {valid: false, reason: "insufficient_funds", total_cost: N}
- [x] **AC-7**: GIVEN 容量不足 (#5.validate_purchase 返回 false, reason="capacity_full")，WHEN validate_purchase_request()，THEN 返回 {valid: false, reason: "capacity_full", total_cost: N}
- [x] **AC-8**: GIVEN good_id 不在当前摊位解锁等级下可用（如 closed 摊位的商品），WHEN validate_purchase_request()，THEN 返回 {valid: false, reason: "good_unavailable", total_cost: 0}

### Purchase Execution

- [x] **AC-9**: GIVEN validate_purchase_request 返回 {valid: true}，WHEN execute_purchase(stall_id, good_id, quantity)，THEN 委托 #5.execute_purchase() + 发射 purchase_completed(good_id, quantity, total_cost) + 返回 {success: true, good_id, quantity, total_cost}
- [x] **AC-10**: GIVEN execute_purchase() 被调用但内部二次 validate 失败（状态在两次调用间变化），WHEN 检测到，THEN 返回 {success: false, ...} + 发射 purchase_failed(good_id, reason)。防御性验证
- [x] **AC-11**: GIVEN purchase_completed 发射后，WHEN 检查，THEN 触发 ADR-0003 快照 (progress.settlement-market)。购买是状态变更——必须持久化

### Good Visibility by Unlock Level

- [x] **AC-12**: GIVEN stall_state=OPEN_BASIC，WHEN get_stall_goods("stall.gh-lens-workshop")，THEN 返回 [good.basic-supply-bundle, good.repair-canvas, good.route-notes, good.lens-maintenance-kit]。基础商品 + 独占风味商品 + 情报商品
- [x] **AC-13**: GIVEN stall_state=CLOSED，WHEN get_stall_goods("stall.gh-sail-shop")，THEN 返回 []。关闭摊位无可购买商品
- [x] **AC-14**: GIVEN stall.gh-general（默认杂货摊, OPEN_BASIC），WHEN get_stall_goods()，THEN 返回 [good.basic-supply-bundle, good.repair-canvas]。杂货摊仅 2 种通用补给——无独占风味商品

### Quantity Input Validation

- [x] **AC-15**: GIVEN player_currency=200 + good price=50 + remaining_capacity=10，WHEN 计算 max_affordable，THEN max_affordable = min(floor(200/50), 10) = 4
- [x] **AC-16**: GIVEN player_currency=30 + good price=50，WHEN 计算 max_affordable，THEN max_affordable = min(floor(30/50), ...) = 0。商品灰显不可选
- [x] **AC-17**: GIVEN quantity 输入控件 + max_affordable=4，WHEN 玩家尝试输入 0、-1 或非整数值，THEN 0 和负值 clamp 为 1；非整数向下取整。减号按钮在 quantity=1 时灰显

### Purchase Failure Constants

- [x] **AC-18**: GIVEN 购买失败原因，WHEN 检查常量定义，THEN PURCHASE_FAIL_CAPACITY="capacity_full", PURCHASE_FAIL_FUNDS="insufficient_funds"。StringName 类型
- [x] **AC-19**: GIVEN price=0 的商品（配置错误），WHEN execute_purchase()，THEN 购买成功（total_cost=0，不扣货币），但记录 error 日志 "Settlement: good '%s' has price=0"

---

## Implementation Notes

### F.1 Total Cost

```text
func calculate_total_cost(good_id: StringName, quantity: int) -> int:
    var good_def := Registry.query_entity(good_id)
    var price: int = good_def.get("price", 0)
    if price == 0:
        push_error("Settlement: good '%s' has price=0 — configuration error" % good_id)
    return price * quantity
```

### Purchase Validation

```text
func validate_purchase_request(stall_id: StringName, good_id: StringName, quantity: int) -> Dictionary:
    # 1. 验证摊位已开启
    if get_stall_state(stall_id) < STALL_OPEN_BASIC:
        return {"valid": false, "reason": &"stall_closed", "total_cost": 0}

    # 2. 验证商品在当前解锁等级下可用
    var available_goods: Array = get_stall_goods(stall_id)
    if good_id not in available_goods:
        return {"valid": false, "reason": &"good_unavailable", "total_cost": 0}

    # 3. 计算 total_cost
    var total_cost: int = calculate_total_cost(good_id, quantity)

    # 4. 委托 #5 检查货币 + 容量
    var rs_validation: Dictionary = ResourcesManager.validate_purchase(good_id, quantity)
    if not rs_validation.get("valid", false):
        return {
            "valid": false,
            "reason": rs_validation.get("reason", &"unknown"),
            "total_cost": total_cost,
        }

    return {"valid": true, "reason": &"", "total_cost": total_cost}
```

### Purchase Execution

```text
func execute_purchase(stall_id: StringName, good_id: StringName, quantity: int) -> Dictionary:
    # 防御性二次验证
    var validation := validate_purchase_request(stall_id, good_id, quantity)
    if not validation.get("valid", false):
        purchase_failed.emit(good_id, validation.get("reason", &"unknown"))
        return {
            "success": false,
            "good_id": good_id,
            "quantity": quantity,
            "total_cost": validation.get("total_cost", 0),
        }

    var total_cost: int = validation.get("total_cost", 0)

    # 委托 #5 执行资源转移
    var rs_result: Dictionary = ResourcesManager.execute_purchase(good_id, quantity)
    if not rs_result.get("success", false):
        purchase_failed.emit(good_id, rs_result.get("reason", &"unknown"))
        return {"success": false, "good_id": good_id, "quantity": quantity, "total_cost": total_cost}

    purchase_completed.emit(good_id, quantity, total_cost)
    _trigger_snapshot()
    return {"success": true, "good_id": good_id, "quantity": quantity, "total_cost": total_cost}
```

### Good Visibility Filter

```text
func get_stall_goods(stall_id: StringName) -> Array:
    var stall_state: int = get_stall_state(stall_id)
    if stall_state < STALL_OPEN_BASIC:
        return []

    var goods: Array = []
    var all_good_defs: Array = Registry.query_by_kind(&"good")
    for good_def in all_good_defs:
        var good_stall_id: StringName = good_def.get("stall_id", &"")
        var required_unlock: int = good_def.get("required_stall_state", STALL_OPEN_BASIC)
        if good_stall_id == stall_id and stall_state >= required_unlock:
            goods.append(good_def.get("entity_id", &""))

    return goods
```

### Max Affordable

```text
func get_max_affordable(good_id: StringName) -> int:
    var good_def := Registry.query_entity(good_id)
    var price: int = good_def.get("price", 1)
    if price <= 0:
        return 0

    var player_currency: int = ResourcesManager.get_player_currency()
    var capacity_summary: Dictionary = ResourcesManager.get_storage_summary()
    var remaining_capacity: int = capacity_summary.get("total_volume", 0) - capacity_summary.get("used_volume", 0)

    return mini(floori(player_currency / price), remaining_capacity)
```

---

## Out of Scope

- ResourcesManager.validate_purchase() / execute_purchase() 实现——属于 resources-goods-capacity Epic
- ResourcesManager.get_player_currency() / get_storage_summary() 实现——属于 resources-goods-capacity Epic
- 摊位 UI 界面渲染（商品列表、价格显示、购买确认浮层）——属于 #16 UIManager
- use_requested 信号分发——属于 player-movement-interaction Epic
- 情报商品消费后知识条目解锁——属于 intel-knowledge Epic（OQ-3 决议后确定）

---

## QA Test Cases

- **AC-1/2**: total_cost = price × quantity (various goods)
- **AC-3**: quantity=0 → total_cost=0
- **AC-4**: Valid purchase → {valid: true}
- **AC-5**: Closed stall → {valid: false, "stall_closed"}
- **AC-6**: Insufficient funds → {valid: false, "insufficient_funds"}
- **AC-7**: Capacity full → {valid: false, "capacity_full"}
- **AC-8**: Good unavailable at stall → {valid: false, "good_unavailable"}
- **AC-9**: Execute → purchase_completed emitted + snapshot triggered
- **AC-10**: Defensive re-validation failure → purchase_failed
- **AC-12**: OPEN_BASIC stall → 4 goods (2 common + 1 exclusive + 1 intel)
- **AC-13**: CLOSED stall → []
- **AC-14**: Default stall → 2 goods only
- **AC-15**: max_affordable calculation
- **AC-16**: player_currency < price → max_affordable=0
- **AC-17**: Quantity input clamping
- **AC-18**: Failure constant definitions
- **AC-19**: price=0 → success + error log

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/settlement-market/PurchaseFlowTest.csproj` — must exist and pass
**Status**: [x] Created and passing

**Acceptance Evidence (2026-05-14)**:
- `dotnet run --project tests/unit/settlement-market/PurchaseFlowTest.csproj -p:UseSharedCompilation=false` — PASS (6/6 checks)
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` — PASS

---

## Dependencies

- Depends on: Story 001 (state machine), content-registry Epic (query_by_kind, query_entity), resources-goods-capacity Epic (validate_purchase, execute_purchase, get_player_currency, get_storage_summary)
- Unlocks: Story 004 (signal + resources integration builds on purchase flow)
