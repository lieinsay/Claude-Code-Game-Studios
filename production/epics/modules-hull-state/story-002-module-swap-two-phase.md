# Story 002: Module Swap Two-Phase Operation

> **Epic**: Modules & Hull State
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — swap_module 两阶段语义)
**ADR Decision Summary**: swap_module(slot_id, new_module_type) 一步完成卸下旧模块和安装新模块。操作分两阶段：（1）验证阶段——检查所有前提条件（槽位非空、材料充足、货舱占用保护、同类型拒绝、净消耗预演），任何条件失败返回错误且无状态变更；（2）执行阶段——按序执行卸下旧模块、发放退款、安装新模块、扣除材料。净消耗按资源类型逐项计算（各资源净消耗 = max(0, 新模块安装成本 − 旧模块退款)），退还不能跨资源类型抵扣。damaged 模块退款 = 0。同类型交换被拒绝。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: swap_module 两阶段（验证→执行）保证原子性——不会出现"卸下但装不上"的中间状态；卸下+安装期间槽位不暴露 empty 窗口
- Forbidden: 同类型模块交换（如 cargo→cargo）——返回错误且不消耗材料；货舱有货物时允许 swap 为非货仓模块（必须先清空货舱）
- Guardrail: 验证阶段中任何条件失败→返回错误码，不进行任何状态变更或材料操作

---

## Acceptance Criteria

### Swap Success Path

- [ ] **AC-1**: GIVEN slot_a = 侦察模块 installed，WHEN swap_module("slot_a", CARGO)，THEN 侦察模块被卸下（退还 basic×4 + repair_kit×2），货仓模块被安装（消耗 basic×3 + repair_kit×3），净消耗 = basic:0 + repair:1，slot_a 最终 module_type=CARGO + visible_state=installed
- [ ] **AC-2**: GIVEN slot_b = 货仓模块 installed，WHEN swap_module("slot_b", SCOUT)，THEN 货仓模块卸下（退还 basic×3 + repair_kit×3），侦察模块安装（消耗 basic×5 + repair_kit×2），净消耗 = basic:2 + repair:0

### Swap Rejection Paths

- [ ] **AC-3**: GIVEN slot_a = 侦察模块 installed + 仓库中 repair_kit=0，WHEN swap_module("slot_a", CARGO)（净消耗 repair=1），THEN 验证阶段失败→ERR_INSUFFICIENT_RESOURCES，槽位保持侦察模块不变，不消耗任何材料
- [ ] **AC-4**: GIVEN slot_b = 货仓模块 installed + 货舱中有货物（used_volume > 0），WHEN swap_module("slot_b", SCOUT)，THEN 验证阶段失败→ERR_CARGO_BAY_NOT_EMPTY，槽位保持货仓模块，货物不受影响
- [ ] **AC-5**: GIVEN slot_a = 侦察模块 installed，WHEN swap_module("slot_a", SCOUT)（同类型），THEN 验证阶段失败→提示"模块类型相同，无需更换"，不消耗材料
- [ ] **AC-6**: GIVEN slot_a = empty，WHEN swap_module("slot_a", CARGO)，THEN 验证阶段失败→ERR_SLOT_EMPTY（无可卸载模块），不消耗材料

### Swap with Damaged Module

- [ ] **AC-7**: GIVEN slot_a = 货仓模块 damaged，WHEN swap_module("slot_a", SCOUT)，THEN refund_for_old=0（damaged 无退款），净消耗 = 侦察模块完整安装成本（basic×5 + repair_kit×2），操作成功后 slot_a visible_state=installed + module_type=SCOUT

### Net Cost Calculation

- [ ] **AC-8**: GIVEN 旧模块安装成本 {basic: 5, repair: 2} + 新模块 {basic: 3, repair: 3}，WHEN 计算净消耗，THEN:
  - basic: max(0, 3 − ceil(5×0.75)) = max(0, 3−4) = 0
  - repair: max(0, 3 − ceil(2×0.75)) = max(0, 3−2) = 1
  - 总净消耗 = {repair: 1}

- [ ] **AC-9**: GIVEN 净消耗为 {repair: 1}，WHEN 仓库中 repair_kit=1，THEN 验证通过——消耗 1 个 repair_kit 后操作完成

### Two-Phase Atomicy

- [ ] **AC-10**: GIVEN swap_module 执行中（已验证通过、正在执行），WHEN 安装阶段材料扣除失败（如并发消耗导致不足），THEN 整个操作回滚——旧模块恢复原状、退款撤销、槽位状态不变。但在 Godot 单线程环境下，回调期间无并发可能——此 AC 为防御性规范

---

## Implementation Notes

### swap_module Two-Phase Implementation

```text
func swap_module(slot_id: StringName, new_module_type: int) -> int:
    # === Phase 1: Validation ===
    if not _is_valid_slot(slot_id):
        return ERR_INVALID_PARAMETER

    var slot: Dictionary = _slots[slot_id]
    var old_visible: int = slot["visible_state"]

    # 槽位必须非空
    if old_visible == VisibleState.EMPTY:
        return ERR_SLOT_EMPTY

    var old_type: int = slot["module_type"]

    # 同类型拒绝
    if old_type == new_module_type:
        push_warning("swap_module: 模块类型相同，无需更换")
        return ERR_SAME_MODULE_TYPE

    # 若旧模块为货仓且新模块为非货仓——检查货舱是否已清空
    if old_type == ModuleType.CARGO and new_module_type != ModuleType.CARGO:
        var usage: Dictionary = ResourcesManager.get_cargo_bay_usage()
        if usage.get("used_volume", 0) > 0:
            return ERR_CARGO_BAY_NOT_EMPTY

    # 计算净消耗
    var net_cost: Dictionary = _calculate_swap_net_cost(old_type, old_visible, new_module_type)

    # 预演：检查材料是否充足
    if not ResourcesManager.can_consume_for_module(net_cost):
        return ERR_INSUFFICIENT_RESOURCES

    # === Phase 2: Execution ===
    var old_actual: int = slot["actual_state"]

    # 1. 卸下旧模块
    var refund: Dictionary = _get_swap_refund(old_type, old_visible)
    slot["module_type"] = ModuleType.EMPTY  # 不暴露 empty 窗口——立即安装
    slot["actual_state"] = ActualState.EMPTY
    slot["visible_state"] = VisibleState.EMPTY

    # 2. 发放退款（若有）
    if not refund.is_empty():
        ResourcesManager.grant_resources(refund)

    # 3. 安装新模块
    var install_cost: Dictionary = _get_install_cost(new_module_type)

    # 4. 扣除净消耗材料
    if not net_cost.is_empty():
        ResourcesManager.consume_for_module(net_cost)

    # 5. 设置新模块状态
    slot["module_type"] = new_module_type
    slot["actual_state"] = ActualState.INSTALLED
    slot["visible_state"] = VisibleState.INSTALLED

    _emit_slot_changed(slot_id, old_visible, VisibleState.INSTALLED)
    return OK
```

### Net Cost Calculation

```text
func _calculate_swap_net_cost(old_type: int, old_visible: int, new_type: int) -> Dictionary:
    var install_cost: Dictionary = _get_install_cost(new_type)
    var refund: Dictionary = _get_swap_refund(old_type, old_visible)

    var net_cost: Dictionary = {}
    # 合并所有资源类型
    var all_resources: Array = []
    for res in install_cost:
        if res not in all_resources:
            all_resources.append(res)
    for res in refund:
        if res not in all_resources:
            all_resources.append(res)

    for resource in all_resources:
        var cost_val: int = install_cost.get(resource, 0)
        var refund_val: int = refund.get(resource, 0)
        var net: int = maxi(0, cost_val - refund_val)
        if net > 0:
            net_cost[resource] = net

    return net_cost


func _get_swap_refund(module_type: int, visible_state: int) -> Dictionary:
    # damaged/unchecked 无退款
    if visible_state != VisibleState.INSTALLED:
        return {}
    return _get_uninstall_refund(module_type)
```

### Cargo Bay Protection Gate

```text
# swap_module 的货舱占用保护在验证阶段
# 货舱有货物 + 旧模块为货仓 + 新模块非货仓 → 拒绝
# 若旧模块为货仓 + 新模块也为货仓 → 允许（货舱容积可能变化但货物仍可访问）
```

### Error Codes

```text
enum SwapError {
    OK = 0,
    ERR_INVALID_PARAMETER,
    ERR_SLOT_EMPTY,
    ERR_SAME_MODULE_TYPE,
    ERR_CARGO_BAY_NOT_EMPTY,
    ERR_INSUFFICIENT_RESOURCES,
}
```

---

## Out of Scope

- 货舱 trapped 货物的具体处理逻辑（属于 Story 005）
- 资源系统 consume_for_module / can_consume_for_module 的具体实现（属于 Resources #5）
- swap 操作的 UI 确认面板（属于 UI 系统 #16）
- 安装/卸下材料成本的具体数值定义（属于资源系统 #5）

---

## QA Test Cases

- **AC-1**: Scout→Cargo swap
  - Given: slot_a=scout installed, warehouse basic=10, repair=5
  - When: swap_module("slot_a", CARGO)
  - Then: refund basic×4+repair×2, consume basic×3+repair×3, net=repair:1, slot_a=cargo installed

- **AC-4**: Cargo→Scout with cargo bay occupied
  - Given: slot_b=cargo installed, cargo_bay used_volume=300
  - When: swap_module("slot_b", SCOUT)
  - Then: ERR_CARGO_BAY_NOT_EMPTY, slot_b unchanged

- **AC-5**: Same-type swap
  - Given: slot_a=scout installed
  - When: swap_module("slot_a", SCOUT)
  - Then: ERR_SAME_MODULE_TYPE, no material change

- **AC-8**: Net cost calculation
  - Given: old=scout(basic×5, repair×2), new=cargo(basic×3, repair×3)
  - When: _calculate_swap_net_cost
  - Then: basic=0, repair=1

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/modules/ModuleSwapTest.csproj` — must exist and pass
**Status**: [x] PASS — 6/6 checks in `tests/unit/modules/ModuleSwapTest.csproj`; 2026-05-13 Epic #8 复审复跑通过

---

## Dependencies

- Depends on: Story 001 (slot state machine, install/uninstall), resources-goods-capacity Epic (get_cargo_bay_usage, can_consume_for_module, consume_for_module, grant_resources)
- Unlocks: Story 005 (cargo bay volume changes on swap), Story 007 (swap reflected in snapshot)
