# Story 004: Furnace Capacity & Departure Readiness

> **Epic**: Modules & Hull State
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`, `TR-modules-003`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — 动力炉载重模型 + can_depart 适航判定)
**ADR Decision Summary**: 动力炉是模块的内置属性——侦察模块动力炉载重 8，货仓模块动力炉载重 12。M_max = ⌊Σ(R_furnace(i) × η_final(i))⌋ ——使用 η_final（含波段修正）。can_depart() 返回 {can: bool, reasons: [StringName]}——条件为 M_max > 0 AND integrity > 0 AND M_loaded ≤ M_max。至少一个动力炉载重贡献 > 0 时飞艇可出航。飞行能力（动力炉）与船体完整性（波段）是独立 AND 维度。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: M_max 使用 floor() 取整——保守设计，略微低估载重而非高估；M_max = 0 时阻断出航（"no_furnace"）；integrity = 0 时阻断出航（"hull_destroyed"）；M_loaded > M_max 时阻断出航（"overloaded"）
- Forbidden: 阻止玩家卸下最后一个模块（M_max=0 是合法停泊状态——只阻断出航，不阻断卸下）；can_depart() 跳过任一条件检查
- Guardrail: 多个阻断条件同时存在时返回所有原因标识；M_max 计算使用 η_final（含波段修正）——critical 波段下所有模块的载重贡献额外×0.8

---

## Acceptance Criteria

### Furnace Ratings

- [ ] **AC-1**: GIVEN 侦察模块 installed（η_final=1.0），WHEN 查询该槽位动力炉载重贡献，THEN = ⌊8 × 1.0⌋ = 8
- [ ] **AC-2**: GIVEN 货仓模块 installed（η_final=1.0），WHEN 查询该槽位动力炉载重贡献，THEN = ⌊12 × 1.0⌋ = 12
- [ ] **AC-3**: GIVEN empty 槽位，WHEN 查询动力炉载重贡献，THEN = 0

### M_max Calculation — All Combinations

- [ ] **AC-4**: GIVEN 各模块配置（intact 波段），WHEN 计算 M_max，THEN:

| 配置 | 槽 A | 槽 B | M_max |
|------|------|------|-------|
| 双货仓完好 | cargo installed | cargo installed | 24 |
| 侦察+货仓完好 | scout installed | cargo installed | 20 |
| 双侦察完好 | scout installed | scout installed | 16 |
| 货仓 damaged + 侦察完好 | scout installed | cargo damaged | 14 |
| 侦察 damaged + 货仓完好 | scout damaged | cargo installed | 16 |
| 双模块 damaged | scout damaged | cargo damaged | 10 |
| 仅单侦察 | scout installed | empty | 8 |
| 仅单货仓 | empty | cargo installed | 12 |
| 全空 | empty | empty | 0 |

- [ ] **AC-5**: GIVEN 双货仓 installed + critical 波段（η_hull_band=0.8），WHEN 计算 M_max，THEN = ⌊12×0.8 + 12×0.8⌋ = ⌊9.6 + 9.6⌋ = 19

### M_max with unchecked Modules

- [ ] **AC-6**: GIVEN 双货仓 unchecked（η_visible=0.95）+ intact 波段，WHEN M_max，THEN = ⌊12×0.95 + 12×0.95⌋ = ⌊11.4 + 11.4⌋ = 22

### can_depart() Single Conditions

- [ ] **AC-7**: GIVEN M_max = 0（所有模块 empty），WHEN can_depart()，THEN {false, ["no_furnace"]}
- [ ] **AC-8**: GIVEN integrity = 0（destroyed），WHEN can_depart()，THEN {false, ["hull_destroyed"]}
- [ ] **AC-9**: GIVEN M_loaded = 20, M_max = 12（货仓 damaged 导致 M_max 下降），WHEN can_depart()，THEN {false, ["overloaded"]}

### can_depart() Multiple Conditions

- [ ] **AC-10**: GIVEN M_max = 0 + integrity = 0，WHEN can_depart()，THEN {false, ["no_furnace", "hull_destroyed"]}
- [ ] **AC-11**: GIVEN M_loaded = 15, M_max = 10 + integrity = 0，WHEN can_depart()，THEN {false, ["overloaded", "hull_destroyed"]}

### can_depart() Success

- [ ] **AC-12**: GIVEN M_max = 12, integrity = 80, M_loaded = 5，WHEN can_depart()，THEN {true, []}

### Flight Capability vs Hull Integrity Independence

- [ ] **AC-13**: GIVEN 双货仓 installed（M_max=24）+ integrity=0（destroyed），WHEN can_depart()，THEN {false, ["hull_destroyed"]}——动力炉正常但船体崩溃→无法出航
- [ ] **AC-14**: GIVEN 所有模块 empty（M_max=0）+ integrity=100，WHEN can_depart()，THEN {false, ["no_furnace"]}——船体完好但无动力→无法出航

### M_loaded Query

- [ ] **AC-15**: GIVEN can_depart() 调用，WHEN 需要 M_loaded，THEN 从 ResourcesManager.get_total_loaded_mass() 查询——本系统不拥有载重数据

---

## Implementation Notes

### Furnace Ratings

```text
const FURNACE_RATING: Dictionary = {
    ModuleType.SCOUT: 8,
    ModuleType.CARGO: 12,
    ModuleType.EMPTY: 0,
}

func _get_furnace_rating(module_type: int) -> int:
    return FURNACE_RATING.get(module_type, 0)
```

### M_max Calculation

```text
func get_max_cargo_capacity() -> int:
    var total: float = 0.0
    for slot_id in SLOT_IDS:
        var slot: Dictionary = _slots[slot_id]
        if slot["visible_state"] == VisibleState.EMPTY:
            continue

        var module_type: int = slot["module_type"]
        var rating: int = FURNACE_RATING.get(module_type, 0)

        # η_final = η_visible × η_hull_band
        var eta_visible: float = get_module_efficiency(slot_id)
        var eta_hull_band: float = get_hull_band_efficiency_multiplier()
        var eta_final: float = eta_visible * eta_hull_band

        total += float(rating) * eta_final

    return floori(total)  # 保守取整
```

### can_depart() Implementation

```text
func can_depart() -> Dictionary:
    var reasons: Array[StringName] = []

    var m_max: int = get_max_cargo_capacity()

    # 检查 1: 动力炉
    if m_max <= 0:
        reasons.append(&"no_furnace")

    # 检查 2: 船体完整性
    if _hull_integrity <= 0:
        reasons.append(&"hull_destroyed")

    # 检查 3: 载重
    var m_loaded: int = ResourcesManager.get_total_loaded_mass()
    if m_max > 0 and m_loaded > m_max:
        reasons.append(&"overloaded")

    return {
        "can": reasons.is_empty(),
        "reasons": reasons,
    }
```

### Departure Readiness Change Detection

```text
var _cached_can_depart: bool = false
var _cached_reasons: Array[StringName] = []

func _check_departure_readiness() -> void:
    var result: Dictionary = can_depart()
    var can: bool = result["can"]
    var reasons: Array[StringName] = result["reasons"]

    # 仅在值实际变更时 emit——避免无变化信号噪音
    if can != _cached_can_depart or reasons != _cached_reasons:
        _cached_can_depart = can
        _cached_reasons = reasons
        departure_readiness_changed.emit(can, reasons)
```

### Overloaded Departure Block Message

```text
func get_departure_block_messages() -> Array[String]:
    var result: Dictionary = can_depart()
    if result["can"]:
        return []

    var messages: Array[String] = []
    var reasons: Array[StringName] = result["reasons"]

    for reason in reasons:
        match reason:
            &"no_furnace":
                messages.append("无可用动力炉——请至少安装一个模块")
            &"hull_destroyed":
                messages.append("船体结构崩溃——请修复船体至至少 1 点完整性")
            &"overloaded":
                var m_max: int = get_max_cargo_capacity()
                var m_loaded: int = ResourcesManager.get_total_loaded_mass()
                messages.append("当前载重 %d / 最大载重 %d ——请卸货或修复模块以提高载重上限" % [m_loaded, m_max])

    return messages
```

### Hub Integration Point

```text
# Hub 出航确认前调用
# HubManager._on_pre_departure_check():
#     var readiness: Dictionary = ModuleHullManager.can_depart()
#     if not readiness["can"]:
#         # 显示阻断原因——拒绝出航
#         _show_departure_blocked(readiness["reasons"])
#         return
#     # 继续出航流程
```

---

## Out of Scope

- M_loaded 的具体计算（质量类映射表 light=1/medium=3/heavy=6）——属于资源系统 #5
- 出航确认 UI 中阻断原因的显示格式——属于 UI 系统 #16
- 能量系统 get_furnace_energy_status() 的具体实现——当前 stub 返回 1.0
- 超载后卸货的具体交互流程——属于 Hub #7 + UI #16

---

## QA Test Cases

- **AC-4**: All 9 M_max combinations
  - 双货仓完好=24, 侦察+货仓=20, 双侦察=16
  - 货仓damaged+侦察完好=14, 侦察damaged+货仓完好=16
  - 双damaged=10, 单侦察=8, 单货仓=12, 全空=0

- **AC-5**: M_max with critical band
  - Given: 双货仓 installed + critical band
  - When: get_max_cargo_capacity()
  - Then: floor(12×0.8 + 12×0.8) = floor(19.2) = 19

- **AC-7 through AC-12**: can_depart conditions
  - no_furnace: M_max=0 → {false, ["no_furnace"]}
  - hull_destroyed: integrity=0 → {false, ["hull_destroyed"]}
  - overloaded: M_loaded>M_max → {false, ["overloaded"]}
  - combined: M_max=0+integrity=0 → {false, ["no_furnace","hull_destroyed"]}
  - success: M_max=12,integrity=80,M_loaded=5 → {true, []}

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/modules/DepartureReadinessTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (slot state, efficiency), Story 003 (hull integrity, band penalties, η_hull_band), resources-goods-capacity Epic (get_total_loaded_mass, mass_class mapping)
- Unlocks: Story 006 (departure_readiness_changed signal), Story 007 (departure readiness in snapshot), navigation-route-risk Epic (can_depart consumer)
