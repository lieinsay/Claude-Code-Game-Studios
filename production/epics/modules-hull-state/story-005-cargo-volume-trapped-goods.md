# Story 005: Cargo Bay Effective Volume & Trapped Goods

> **Epic**: Modules & Hull State
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — V_effective + trapped goods + update_cargo_bay_effective_volume)
**ADR Decision Summary**: V_effective = V_base + V_bonus × η_final_A + V_bonus × η_final_B。V_base=0（由资源系统 #5 定义），V_bonus=500（每个货仓模块）。货仓模块效率下降导致 V_effective 减小时，超出部分货物变为 trapped 状态（可见但不可交互——is_accessible=false）。修复/重新安装使 V_effective 恢复后 trapped 货物自动恢复。货物不会因模块 damage 而丢失——只有模块 destroyed 时才触发丢失（EC-05 in resources-goods-capacity.md）。模块系统主动调用资源系统 #5 的 update_cargo_bay_effective_volume() 接口——不依赖 mass_changed 信号。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: V_effective 变更时主动调用 ResourcesManager.update_cargo_bay_effective_volume(new_volume)；trapped 货物条件 = total_loaded_volume > V_effective
- Forbidden: 模块 damage 导致货物丢失（trapped ≠ lost）；依赖 mass_changed 信号间接通知 trapped 条件
- Guardrail: 所有货仓模块卸下→V_effective=0→所有货物 trapped；修复/重新安装后自动恢复可访问

---

## Acceptance Criteria

### V_effective Calculation

- [ ] **AC-1**: GIVEN 双货仓 installed（intact 波段），WHEN 查询 V_effective，THEN = 0 + 500×1.0 + 500×1.0 = 1000
- [ ] **AC-2**: GIVEN 侦察+货仓 installed，WHEN V_effective，THEN = 0 + 0 + 500×1.0 = 500
- [ ] **AC-3**: GIVEN 双侦察，WHEN V_effective，THEN = 0 + 0 + 0 = 0
- [ ] **AC-4**: GIVEN 双货仓 + 一个 damaged（η=0.5），WHEN V_effective，THEN = 0 + 500×0.5 + 500×1.0 = 750
- [ ] **AC-5**: GIVEN 双货仓 unchecked（η=0.95），WHEN V_effective，THEN = 0 + 500×0.95×2 = 950
- [ ] **AC-6**: GIVEN 双货仓 installed + critical 波段（η_hull_band=0.8），WHEN V_effective，THEN = 0 + 500×(1.0×0.8)×2 = 800

### Trapped Goods on Volume Reduction

- [ ] **AC-7**: GIVEN 货仓模块 installed（V_effective=500）+ 装载 400 体积货物，WHEN 该模块从 installed→damaged（V_effective→250），THEN 150 体积货物变为 trapped（is_accessible=false）
- [ ] **AC-8**: GIVEN trapped 货物，WHEN 在 UI 中查看，THEN 灰显且不可交互——tooltip 显示"货物困锁——修复货仓模块以取回"
- [ ] **AC-9**: GIVEN 货物 trapped（V_effective=250, loaded=400），WHEN 修复货仓模块（V_effective→500），THEN 所有 trapped 货物自动恢复可访问（is_accessible=true）

### Trapped Goods on Module Removal

- [ ] **AC-10**: GIVEN 仅一个货仓模块 installed + 装载 200 体积货物，WHEN 卸下该货仓模块（V_effective→0），THEN 所有 200 体积货物 trapped
- [ ] **AC-11**: GIVEN 所有货物 trapped（V_effective=0），WHEN 重新安装货仓模块，THEN 所有 trapped 货物自动恢复

### update_cargo_bay_effective_volume Interface

- [ ] **AC-12**: GIVEN 模块状态变更导致 V_effective 变化，WHEN 变更完成，THEN ModuleHullManager 主动调用 ResourcesManager.update_cargo_bay_effective_volume(new_volume)——资源系统内部检测 trapped 条件
- [ ] **AC-13**: GIVEN V_effective 变更，WHEN 变更前后的 V_effective 值相同，THEN 不调用 update_cargo_bay_effective_volume——避免无变化调用

### No Cargo Loss on Module Damage

- [ ] **AC-14**: GIVEN 货仓模块 damaged + 货物 trapped，WHEN 查询货物所有权，THEN 所有货物仍属于玩家——damaged 导致 trapped 但不会丢失货物

---

## Implementation Notes

### V_effective Calculation

```text
const CARGO_VOLUME_BONUS: int = 500  # 每个货仓模块的容积加成

func get_effective_cargo_volume() -> int:
    var total: float = 0.0

    for slot_id in SLOT_IDS:
        var slot: Dictionary = _slots[slot_id]
        if slot["visible_state"] == VisibleState.EMPTY:
            continue

        if slot["module_type"] != ModuleType.CARGO:
            continue

        # η_final = η_visible × η_hull_band
        var eta_visible: float = get_module_efficiency(slot_id)
        var eta_hull_band: float = get_hull_band_efficiency_multiplier()
        var eta_final: float = eta_visible * eta_hull_band

        total += float(CARGO_VOLUME_BONUS) * eta_final

    return floori(total)
```

### Update Cargo Bay Volume on Any Efficiency Change

```text
var _cached_v_effective: int = 0

func _on_module_state_changed(slot_id: StringName) -> void:
    _check_and_update_cargo_volume()
    _check_departure_readiness()


func _on_hull_integrity_changed() -> void:
    _check_and_update_cargo_volume()
    _check_departure_readiness()


func _check_and_update_cargo_volume() -> void:
    var new_v: int = get_effective_cargo_volume()
    if new_v != _cached_v_effective:
        _cached_v_effective = new_v
        # 主动通知资源系统——不依赖信号间接路径
        ResourcesManager.update_cargo_bay_effective_volume(new_v)
```

### Integration Points in Existing Methods

```text
# install_module / uninstall_module / repair_module / check_module / swap_module
# 每个方法的末尾都调用：
#   _check_and_update_cargo_volume()
#   _check_departure_readiness()

# apply_hull_damage 末尾同样调用
```

### Trapped Goods Detection (Reference — Owned by Resources #5)

```text
# 资源系统 #5 的 update_cargo_bay_effective_volume() 实现参考:
# func update_cargo_bay_effective_volume(new_volume: int) -> void:
#     _cargo_bay_effective_volume = new_volume
#     var total_loaded: int = _get_total_loaded_volume()
#     if total_loaded > new_volume:
#         _mark_excess_as_trapped(total_loaded - new_volume)
#     else:
#         _unmark_all_trapped()
```

### Slot-Level Cargo Contribution Query

```text
func get_slot_cargo_volume_contribution(slot_id: StringName) -> int:
    var slot: Dictionary = _slots.get(slot_id, {})
    if slot.is_empty() or slot["visible_state"] == VisibleState.EMPTY:
        return 0
    if slot["module_type"] != ModuleType.CARGO:
        return 0

    var eta_visible: float = get_module_efficiency(slot_id)
    var eta_hull_band: float = get_hull_band_efficiency_multiplier()
    var eta_final: float = eta_visible * eta_hull_band

    return floori(float(CARGO_VOLUME_BONUS) * eta_final)
```

---

## Out of Scope

- trapped 货物的具体 UI 表现（灰显、tooltip）——属于 UI 系统 #16
- ResourcesManager.update_cargo_bay_effective_volume() 的具体实现——属于资源系统 #5
- 模块 destroyed 时（非 damaged）货物丢失的具体规则——属于资源系统 #5 EC-05
- 货物装卸的具体交互流程——属于 Hub #7 + 资源系统 #5

---

## QA Test Cases

- **AC-1 through AC-6**: V_effective across configurations
  - 双货仓完好 = 1000
  - 侦察+货仓 = 500
  - 双侦察 = 0
  - 一个damaged = 750
  - unchecked(×2) = 950
  - critical band + 双货仓 = 800

- **AC-7**: Volume reduction → trapped
  - Given: V_effective=500, loaded_volume=400
  - When: cargo module installed→damaged, V_effective→250
  - Then: 150 trapped

- **AC-12**: update_cargo_bay_effective_volume called
  - Given: cargo module damaged → V_effective changes from 500 to 250
  - When: _check_and_update_cargo_volume()
  - Then: ResourcesManager.update_cargo_bay_effective_volume(250) called

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/modules/CargoVolumeTrappedTest.csproj` — must exist and pass
**Status**: [x] PASS — 3/3 checks in `tests/integration/modules/CargoVolumeTrappedTest.csproj`

---

## Dependencies

- Depends on: Story 001 (module state changes trigger V_effective recalculation), Story 002 (swap may change cargo type), Story 003 (hull band affects η_final), resources-goods-capacity Epic (update_cargo_bay_effective_volume interface, trapped goods logic)
- Unlocks: Story 006 (module_efficiency_changed → UI checks V_effective for trapped), Story 007 (V_effective in snapshot context)
