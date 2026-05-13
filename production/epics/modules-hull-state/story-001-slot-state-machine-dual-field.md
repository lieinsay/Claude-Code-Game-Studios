# Story 001: Module Slot State Machine & Dual-Field Model

> **Epic**: Modules & Hull State
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`, `TR-modules-002`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — ModuleHullManager Autoload #8, 2 slots + dual-field model)
**ADR Decision Summary**: 每个已安装模块槽位内部维护两个字段——actual_state（真实物理状态：installed/damaged，由航行系统写入）和 visible_state（玩家可见状态：installed/damaged/unchecked，返航后自动置为 unchecked）。效率系数基于 visible_state 计算（玩家只能基于可见信息做决策）。模块槽有 4 个 visible 状态：empty/installed/damaged/unchecked，每个状态有对应的效率系数（η_scout: 0/1.0/0.6/0.95, η_cargo: 0/1.0/0.5/0.95）。2 个模块槽位均为开放槽位——每个槽位可安装任意类型的模块。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 每个槽位维护 actual_state 和 visible_state 双字段；效率系数基于 visible_state 计算；返航后出航前 actual=installed 的模块 visible_state 统一置为 unchecked（η=0.95）；返航后出航前 actual=damaged 的模块 visible_state 维持 damaged（η 不提升）
- Forbidden: 模块系统拥有模块效果计算逻辑——Hub 不拥有模块状态机（仅持有镜像）；empty 槽位的交互点不可完全消失（必须是可见的"空安装位"）
- Guardrail: 安装已占用槽位→ERR_SLOT_OCCUPIED；卸下空槽位→ERR_SLOT_EMPTY；checked→visible_state 同步为 actual_state 后效率立即更新

---

## Acceptance Criteria

### Module Slot State Definitions

- [ ] **AC-1**: GIVEN ModuleHullManager Autoload #8 已初始化，WHEN 查询槽位，THEN 恰好 2 个槽位：slot_a 和 slot_b——均为开放槽位（可安装任意类型模块）
- [ ] **AC-2**: GIVEN 每个已安装模块槽位，WHEN 检查内部字段，THEN 包含 actual_state（installed/damaged/empty）和 visible_state（installed/damaged/unchecked/empty）——双字段独立维护

### Install / Uninstall

- [ ] **AC-3**: GIVEN 空槽位，WHEN 执行安装操作（提供模块类型 + 材料），THEN 消耗材料后模块状态变为 installed（visible_state 和 actual_state 均为 installed）
- [ ] **AC-4**: GIVEN installed 状态的模块，WHEN 执行卸下操作，THEN 模块状态变为 empty，玩家获得安装材料的 75% 退还（向上取整）
- [ ] **AC-5**: GIVEN damaged 状态的模块，WHEN 执行卸下操作，THEN 模块状态变为 empty，玩家不获得任何材料退还
- [ ] **AC-6**: GIVEN unchecked 状态的模块（不先检查），WHEN 执行卸下操作，THEN 模块状态变为 empty，玩家不获得任何材料退还——卸下提示区分"模块已受损——卸下不退还材料"和"模块尚未检查——卸下不退还材料，建议先检查"
- [ ] **AC-7**: GIVEN 已占用槽位，WHEN 执行安装操作，THEN 返回 ERR_SLOT_OCCUPIED——不消耗材料，不改变状态
- [ ] **AC-8**: GIVEN 空槽位，WHEN 执行卸下操作，THEN 返回 ERR_SLOT_EMPTY——不授予材料，槽位保持 empty
- [ ] **AC-9**: GIVEN 空槽位，WHEN Hub 查询 is_interactable，THEN 返回 true——空槽位可被 Use 交互聚焦（显示为"空安装位"）

### Efficiency Coefficients

- [ ] **AC-10**: GIVEN 各状态，WHEN 查询效率系数，THEN:

| module_type \ visible_state | empty | unchecked | installed | damaged |
|---|---|---|---|---|
| scout | 0 | 0.95 | 1.0 | 0.6 |
| cargo | 0 | 0.95 | 1.0 | 0.5 |

### Post-Voyage Unchecked Transition

- [ ] **AC-11**: GIVEN 出航前 actual_state = installed + visible_state = installed，WHEN 航行结束返航，THEN actual_state 由航行系统写入（installed 或 damaged），visible_state 自动变为 unchecked（η=0.95）
- [ ] **AC-12**: GIVEN 出航前 actual_state = damaged + visible_state = damaged，WHEN 航行结束返航，THEN visible_state 维持 damaged（η 保持对应值，不提升至 0.95）——受损模块不会因出航而"变好"

### Check Flow

- [ ] **AC-13**: GIVEN unchecked 模块，WHEN 玩家执行检查（免费，0 材料），THEN visible_state 同步为 actual_state——若 actual=installed → installed（η=1.0），若 actual=damaged → damaged（η=对应值）。检查不改变 actual_state
- [ ] **AC-14**: GIVEN unchecked 模块 + actual=installed，WHEN 玩家执行检查，THEN η 从 0.95 恢复 1.0——无需维修材料（模块完好）

### Repair Flow

- [ ] **AC-15**: GIVEN damaged 模块，WHEN 玩家执行维修（消耗 repair_kit × 2），THEN visible_state 和 actual_state 均置为 installed（η=1.0）
- [ ] **AC-16**: GIVEN unchecked 模块 + 玩家直接维修（不先检查），WHEN 执行维修，THEN 消耗 repair_kit × 2（全额），visible_state 和 actual_state 均置为 installed（η=1.0）——材料消耗与 actual_state 无关
- [ ] **AC-17**: GIVEN unchecked 模块 + 先检查（免费）→ actual=installed，WHEN 流程完成，THEN η 恢复 1.0 且无材料消耗——与直接维修路径的"付钱买确定"形成策略取舍

---

## Implementation Notes

### ModuleHullManager Core Structure

```text
# ModuleHullManager.cs — Autoload #8
extends Node

enum ModuleType { EMPTY, SCOUT, CARGO }

enum ActualState { EMPTY, INSTALLED, DAMAGED }
enum VisibleState { EMPTY, INSTALLED, DAMAGED, UNCHECKED }

const SLOT_IDS: Array[StringName] = [&"slot_a", &"slot_b"]

# Per-slot data — dual-field model
var _slots: Dictionary = {}  # Dict[StringName, Dictionary]
# _slots[slot_id] = {
#     "module_type": int (ModuleType),
#     "actual_state": int (ActualState),
#     "visible_state": int (VisibleState),
# }
```

### Efficiency Coefficient Table

```text
const EFFICIENCY_TABLE: Dictionary = {
    ModuleType.SCOUT: {
        VisibleState.EMPTY: 0.0,
        VisibleState.UNCHECKED: 0.95,
        VisibleState.INSTALLED: 1.0,
        VisibleState.DAMAGED: 0.6,
    },
    ModuleType.CARGO: {
        VisibleState.EMPTY: 0.0,
        VisibleState.UNCHECKED: 0.95,
        VisibleState.INSTALLED: 1.0,
        VisibleState.DAMAGED: 0.5,
    },
}

func get_module_efficiency(slot_id: StringName) -> float:
    var slot: Dictionary = _slots.get(slot_id, {})
    if slot.is_empty():
        return 0.0

    var module_type: int = slot.get("module_type", ModuleType.EMPTY)
    var visible_state: int = slot.get("visible_state", VisibleState.EMPTY)

    return EFFICIENCY_TABLE.get(module_type, {}).get(visible_state, 0.0)
```

### Install / Uninstall Operations

```text
func install_module(slot_id: StringName, module_type: int) -> int:
    if not _is_valid_slot(slot_id):
        return ERR_INVALID_PARAMETER

    var slot: Dictionary = _slots[slot_id]
    if slot["visible_state"] != VisibleState.EMPTY:
        return ERR_SLOT_OCCUPIED

    # 消耗材料——调用资源系统 #5
    var cost: Dictionary = _get_install_cost(module_type)
    var consumed: bool = ResourcesManager.consume_for_module(cost)
    if not consumed:
        return ERR_INSUFFICIENT_RESOURCES

    # 安装
    slot["module_type"] = module_type
    slot["actual_state"] = ActualState.INSTALLED
    slot["visible_state"] = VisibleState.INSTALLED

    _emit_slot_changed(slot_id, VisibleState.EMPTY, VisibleState.INSTALLED)
    return OK


func uninstall_module(slot_id: StringName) -> int:
    if not _is_valid_slot(slot_id):
        return ERR_INVALID_PARAMETER

    var slot: Dictionary = _slots[slot_id]
    var old_state: int = slot["visible_state"]

    if old_state == VisibleState.EMPTY:
        return ERR_SLOT_EMPTY

    # 退还材料（仅 installed 状态退还，damaged/unchecked 不退还）
    if old_state == VisibleState.INSTALLED:
        var refund: Dictionary = _get_uninstall_refund(slot["module_type"])
        ResourcesManager.grant_resources(refund)

    slot["module_type"] = ModuleType.EMPTY
    slot["actual_state"] = ActualState.EMPTY
    slot["visible_state"] = VisibleState.EMPTY

    _emit_slot_changed(slot_id, old_state, VisibleState.EMPTY)
    return OK


func _get_install_cost(module_type: int) -> Dictionary:
    match module_type:
        ModuleType.SCOUT:
            return {"basic_supply": 5, "repair_kit": 2}
        ModuleType.CARGO:
            return {"basic_supply": 3, "repair_kit": 3}
        _:
            return {}


func _get_uninstall_refund(module_type: int) -> Dictionary:
    var cost: Dictionary = _get_install_cost(module_type)
    var refund: Dictionary = {}
    for resource in cost:
        refund[resource] = ceili(cost[resource] * 0.75)
    return refund
```

### Post-Voyage Unchecked Transition

```text
# 航行系统 #10 在返航时调用——写入 actual_state
func on_voyage_completed(module_damage_flags: Dictionary) -> void:
    for slot_id in SLOT_IDS:
        var slot: Dictionary = _slots[slot_id]
        if slot["visible_state"] == VisibleState.EMPTY:
            continue

        # 航行系统写入 actual_state
        var was_damaged: bool = module_damage_flags.get(slot_id, false)
        var old_visible: int = slot["visible_state"]

        if was_damaged:
            slot["actual_state"] = ActualState.DAMAGED
        else:
            slot["actual_state"] = ActualState.INSTALLED

        # visible_state 转换
        if old_visible == VisibleState.DAMAGED:
            # 出航前已知 damaged——维持 damaged，η 不提升
            pass  # visible_state stays DAMAGED
        else:
            # 出航前 installed 或 unchecked——统一置为 unchecked
            slot["visible_state"] = VisibleState.UNCHECKED

        if slot["visible_state"] != old_visible:
            _emit_slot_changed(slot_id, old_visible, slot["visible_state"])
```

### Check & Repair Flow

```text
func check_module(slot_id: StringName) -> int:
    var slot: Dictionary = _slots[slot_id]
    if slot["visible_state"] != VisibleState.UNCHECKED:
        return ERR_INVALID_STATE

    # 检查免费——同步 visible_state 到 actual_state
    var old_visible: int = slot["visible_state"]
    match slot["actual_state"]:
        ActualState.INSTALLED:
            slot["visible_state"] = VisibleState.INSTALLED
        ActualState.DAMAGED:
            slot["visible_state"] = VisibleState.DAMAGED

    _emit_slot_changed(slot_id, old_visible, slot["visible_state"])
    return OK


func repair_module(slot_id: StringName) -> int:
    var slot: Dictionary = _slots[slot_id]
    var old_visible: int = slot["visible_state"]

    if old_visible not in [VisibleState.DAMAGED, VisibleState.UNCHECKED]:
        return ERR_INVALID_STATE

    # 消耗维修材料（全额，与 actual_state 无关）
    var cost: Dictionary = {"repair_kit": 2}
    if not ResourcesManager.consume_for_module(cost):
        return ERR_INSUFFICIENT_RESOURCES

    # 维修修复 actual 和 visible
    slot["actual_state"] = ActualState.INSTALLED
    slot["visible_state"] = VisibleState.INSTALLED

    _emit_slot_changed(slot_id, old_visible, VisibleState.INSTALLED)
    return OK
```

### Empty Slot Interactability

```text
func is_slot_interactable(slot_id: StringName) -> bool:
    return _is_valid_slot(slot_id)
    # 空槽位和已安装槽位均可交互——只是交互行为不同
    # Hub 根据 visible_state 决定显示"安装"还是"检查/维修/卸下"
```

---

## Out of Scope

- swap_module 两阶段操作（属于 Story 002）
- 船体完整性波段系统（属于 Story 003）
- 动力炉载重计算和适航判定（属于 Story 004）
- 货舱有效容积和 trapped 货物（属于 Story 005）
- 模块信号契约的完整定义（属于 Story 006）
- 模块安装/维修材料的具体数值定义——由资源系统 #5 确认
- 模块交互 UI 面板的具体实现（属于 UI 系统 #16）

---

## QA Test Cases

- **AC-3 through AC-8**: Install/uninstall cycle
  - Given: slot_a = empty
  - When: install_module("slot_a", CARGO) → 消耗 basic×3 + repair×3 → installed
  - When: uninstall_module("slot_a") → 退还 ceil(3×0.75)=3 basic, ceil(3×0.75)=3 repair → empty
  - When: install → damage → uninstall → 0 退还 → empty

- **AC-10**: Efficiency table
  - Given: scout installed → η=1.0; scout damaged → η=0.6; scout unchecked → η=0.95
  - Given: cargo installed → η=1.0; cargo damaged → η=0.5; cargo unchecked → η=0.95

- **AC-13**: Check flow
  - Given: unchecked + actual=installed → check → installed (η=1.0, 0 cost)
  - Given: unchecked + actual=damaged → check → damaged (η=0.5/0.6, 0 cost)

- **AC-16**: Direct repair (no check)
  - Given: unchecked + actual=installed → repair → installed (η=1.0, cost repair×2)
  - Given: unchecked + actual=damaged → repair → installed (η=1.0, cost repair×2)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/modules/SlotStateMachineTest.csproj` — must exist and pass
**Status**: [x] PASS — 7/7 checks in `tests/unit/modules/SlotStateMachineTest.csproj`; 2026-05-13 Epic #8 复审复跑通过

---

## Dependencies

- Depends on: resources-goods-capacity Epic (consume_for_module, grant_resources), airship-hub Epic (slot physical positions, interaction anchors), local-save-persistence Epic (snapshot registration)
- Unlocks: Story 002-008 (all depend on state machine)
