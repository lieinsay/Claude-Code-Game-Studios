# Story 003: Hull Integrity, Bands & Scars

> **Epic**: Modules & Hull State
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-003`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — 4 波段船体完整性 + max(per-band) 伤害模型 + hull_scars 计数器)
**ADR Decision Summary**: 船体完整性（0-100 整数值）分为 4 个波段：intact(76-100, 无惩罚)、damaged(26-75, 航速-10%/燃料+15%)、critical(1-25, 航速-25%/燃料+30%/模块效率×0.8/高风险封锁)、destroyed(0, 无法出航)。船体伤痕（hull_scars）为 ≥0 整数值，初始 0，每次 integrity 减少事件 +1。跨波段伤害链中每个新进入的波段额外 +1。修复消耗 repair_kit，每个 kit 恢复 5 integrity。修复后补丁保留视觉痕迹。integrity=0 时无法出航——即使动力炉正常。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: integrity 范围 [0, 100] —— 不可低于 0，不可超过 100；波段判定使用区间 [76-100]=intact, [26-75]=damaged, [1-25]=critical, [0]=destroyed
- Forbidden: integrity 低于 0（clamp 至 0）；修复溢出超过 100（clamp 至 100，多余修复值不保留不退款）；integrity=100 时接受修复操作（拒绝并提示"船体结构完好"）
- Guardrail: 跨波段伤害链的伤痕计数包含"新进入"波段检测——已在目标波段中的不计入转换

---

## Acceptance Criteria

### Hull Integrity & Band Transitions

- [ ] **AC-1**: GIVEN 新游戏初始化，WHEN 查询 hull state，THEN integrity=100，波段=intact，hull_scars=0
- [ ] **AC-2**: GIVEN integrity=100 (intact)，WHEN 受到 25 点伤害→integrity=75，THEN 波段→damaged，航速×0.9，燃料×1.15
- [ ] **AC-3**: GIVEN integrity=26 (damaged)，WHEN 受到 1 点伤害→integrity=25，THEN 波段→critical，航速×0.75，燃料×1.3，模块效率额外×0.8，高风险航线封锁
- [ ] **AC-4**: GIVEN integrity=1 (critical)，WHEN 受到 1 点伤害→integrity=0，THEN 波段→destroyed，can_depart() 返回 false（含 "hull_destroyed"）
- [ ] **AC-5**: GIVEN integrity=5 (critical)，WHEN 受到 15 点伤害，THEN integrity = max(0, 5-15) = 0——不出现负值

### Band Penalty Application

- [ ] **AC-6**: GIVEN 各波段，WHEN 查询惩罚系数，THEN:

| 波段 | 航速修正 | 燃料修正 | 模块效率修正 | 高风险封锁 |
|------|---------|---------|------------|----------|
| intact (76-100) | 1.0 | 1.0 | 1.0 | false |
| damaged (26-75) | 0.9 | 1.15 | 1.0 | false |
| critical (1-25) | 0.75 | 1.3 | 0.8 | true |
| destroyed (0) | — | — | 0 | true (无法出航) |

- [ ] **AC-7**: GIVEN critical 波段，WHEN 查询 η_hull_band，THEN = 0.8——此值与模块自身效率叠加（η_final = η_visible × 0.8）

### Hull Scars Counter

- [ ] **AC-8**: GIVEN integrity=100 + hull_scars=0，WHEN 受到 10 点伤害→integrity=90，THEN hull_scars += 1（基础事件 +1）
- [ ] **AC-9**: GIVEN integrity=80 (intact)，WHEN 一次受到 80 点伤害→integrity=0，THEN hull_scars 累计：基础事件 +1 + 进入 damaged +1 + 进入 critical +1 + 进入 destroyed +1 = +4
- [ ] **AC-10**: GIVEN integrity=30 (已处于 damaged)，WHEN 一次受到 35 点伤害→integrity=0，THEN hull_scars 累计：基础事件 +1 + 进入 critical +1（integrity=30 不在 critical）+ 进入 destroyed +1 = +3。"进入 damaged"不计（已在 damaged 波段中）
- [ ] **AC-11**: GIVEN integrity=20 (critical)→修复至 30 (damaged)→再受 10 点伤害→integrity=20 (critical)，WHEN 第二次伤害事件，THEN hull_scars 增量 = +1（基础事件）+ 进入 critical +1 = +2——验证波段重新进入被正确计数
- [ ] **AC-12**: GIVEN hull_scars，WHEN 查询，THEN 无机械效果（MVP 中仅驱动视觉和叙事），无上限

### Hull Repair

- [ ] **AC-13**: GIVEN integrity=50 (damaged) + repair_kit×2，WHEN 执行修复（消耗 2 个 repair_kit），THEN integrity = min(100, 50 + 2×5) = 60，波段仍为 damaged
- [ ] **AC-14**: GIVEN integrity=95 (intact) + repair_kit×2，WHEN 执行修复，THEN integrity = min(100, 95 + 10) = 100，多余修复值被丢弃
- [ ] **AC-15**: GIVEN integrity=100，WHEN 执行修复，THEN 操作被拒绝并提示"船体结构完好"——不消耗材料
- [ ] **AC-16**: GIVEN integrity=0 (destroyed) + 充足 repair_kit，WHEN 执行修复恢复至 ≥1，THEN 波段变为 critical——恢复出航能力（结构勉强支撑）

---

## Implementation Notes

### Hull Integrity Data Model

```text
const HULL_INTEGRITY_MAX: int = 100
const HULL_INTEGRITY_MIN: int = 0

enum HullBand {
    INTACT,     # 76-100
    DAMAGED,    # 26-75
    CRITICAL,   # 1-25
    DESTROYED,  # 0
}

var _hull_integrity: int = HULL_INTEGRITY_MAX
var _hull_scars: int = 0
var _hull_band: int = HullBand.INTACT
```

### Band Determination

```text
func _get_hull_band(integrity: int) -> int:
    if integrity >= 76:
        return HullBand.INTACT
    elif integrity >= 26:
        return HullBand.DAMAGED
    elif integrity >= 1:
        return HullBand.CRITICAL
    else:
        return HullBand.DESTROYED


func _get_band_boundaries(band: int) -> Dictionary:
    match band:
        HullBand.INTACT:
            return {"min": 76, "max": 100}
        HullBand.DAMAGED:
            return {"min": 26, "max": 75}
        HullBand.CRITICAL:
            return {"min": 1, "max": 25}
        HullBand.DESTROYED:
            return {"min": 0, "max": 0}
        _:
            return {"min": 0, "max": 0}
```

### Apply Hull Damage

```text
func apply_hull_damage(amount: int) -> void:
    if amount <= 0:
        return

    var old_integrity: int = _hull_integrity
    var old_band: int = _hull_band

    # 计算新 integrity（clamp 至 0）
    _hull_integrity = maxi(HULL_INTEGRITY_MIN, _hull_integrity - amount)

    # 基础伤痕：每次 integrity 减少事件 +1
    _hull_scars += 1

    # 跨波段伤痕：计算经过的所有波段边界
    var new_band: int = _get_hull_band(_hull_integrity)
    _count_band_crossings(old_integrity, _hull_integrity, old_band)

    # 波段变更
    if new_band != old_band:
        _hull_band = new_band
        hull_band_changed.emit(old_band, new_band)

    # 完整性变更信号
    hull_integrity_changed.emit(old_integrity, _hull_integrity)


func _count_band_crossings(from_integrity: int, to_integrity: int, starting_band: int) -> void:
    # 按 integrity 降序检查经过的波段——每个"新进入"波段 +1
    var crossed_bands: Array[int] = []

    # 判断从 from_integrity 降到 to_integrity 穿过了哪些波段边界
    # 波段边界值（进入该波段的阈值）：intact=76, damaged=26, critical=1, destroyed=0

    # 穿过 damaged 边界（进入 26-75）
    if from_integrity >= 76 and to_integrity <= 75:
        if starting_band != HullBand.DAMAGED and starting_band != HullBand.CRITICAL and starting_band != HullBand.DESTROYED:
            crossed_bands.append(HullBand.DAMAGED)

    # 穿过 critical 边界（进入 1-25）
    if from_integrity >= 26 and to_integrity <= 25:
        if starting_band != HullBand.CRITICAL and starting_band != HullBand.DESTROYED:
            crossed_bands.append(HullBand.CRITICAL)

    # 穿过 destroyed 边界（到达 0）
    if to_integrity <= 0 and from_integrity > 0:
        crossed_bands.append(HullBand.DESTROYED)

    _hull_scars += crossed_bands.size()
```

### Band Penalty Query

```text
const BAND_PENALTIES: Dictionary = {
    HullBand.INTACT: {
        "speed_multiplier": 1.0,
        "fuel_multiplier": 1.0,
        "module_efficiency_multiplier": 1.0,
        "high_risk_blocked": false,
    },
    HullBand.DAMAGED: {
        "speed_multiplier": 0.9,
        "fuel_multiplier": 1.15,
        "module_efficiency_multiplier": 1.0,
        "high_risk_blocked": false,
    },
    HullBand.CRITICAL: {
        "speed_multiplier": 0.75,
        "fuel_multiplier": 1.3,
        "module_efficiency_multiplier": 0.8,
        "high_risk_blocked": true,
    },
    HullBand.DESTROYED: {
        "speed_multiplier": 0.0,
        "fuel_multiplier": 0.0,
        "module_efficiency_multiplier": 0.0,
        "high_risk_blocked": true,
    },
}

func get_band_penalties() -> Dictionary:
    return BAND_PENALTIES.get(_hull_band, {}).duplicate()

func get_hull_band_efficiency_multiplier() -> float:
    return BAND_PENALTIES.get(_hull_band, {}).get("module_efficiency_multiplier", 1.0)
```

### Hull Repair

```text
const HULL_REPAIR_VALUE_PER_KIT: int = 5

func repair_hull(kit_count: int) -> int:
    if _hull_integrity >= HULL_INTEGRITY_MAX:
        return ERR_HULL_ALREADY_FULL

    if kit_count <= 0:
        return ERR_INVALID_PARAMETER

    # 消耗 repair_kit
    var cost: Dictionary = {"repair_kit": kit_count}
    if not ResourcesManager.consume_for_module(cost):
        return ERR_INSUFFICIENT_RESOURCES

    var old_integrity: int = _hull_integrity
    var old_band: int = _hull_band

    var repair_amount: int = kit_count * HULL_REPAIR_VALUE_PER_KIT
    _hull_integrity = mini(HULL_INTEGRITY_MAX, _hull_integrity + repair_amount)

    var new_band: int = _get_hull_band(_hull_integrity)

    hull_integrity_changed.emit(old_integrity, _hull_integrity)

    if new_band != old_band:
        _hull_band = new_band
        hull_band_changed.emit(old_band, new_band)

    return OK
```

### Band Re-Entry Detection (for AC-11)

```text
# _count_band_crossings 自动处理波段重新进入
# 因为 starting_band 参数为事件前的波段
# 若玩家修复从 critical→damaged→再受伤害进入 critical
# 第二次事件的 starting_band = damaged（非 critical）
# 所以"进入 critical"被正确计数
```

---

## Out of Scope

- 适航判定 can_depart() 的完整实现（属于 Story 004）
- 航行损伤事件的具体来源和伤害量计算（属于 Navigation #10 + Combat #12）
- 船体修补痕迹在 Hub 中的视觉表现（属于 Visual/Feel 类型）
- repair_kit 的材料定义和修复值（repair_value）——由资源系统 #5 定义确认

---

## QA Test Cases

- **AC-2 through AC-5**: Band transitions
  - Given: integrity=100 → damage 25 → 75 (damaged band, speed×0.9, fuel×1.15)
  - Given: integrity=26 → damage 1 → 25 (critical band, speed×0.75, fuel×1.3, high_risk_blocked)
  - Given: integrity=1 → damage 1 → 0 (destroyed band, can_depart=false)
  - Given: integrity=5 → damage 15 → 0 (clamped, not negative)

- **AC-9 and AC-10**: Hull scars cross-band
  - Given: integrity=80(intact)→damage 80→0: scars+4
  - Given: integrity=30(damaged)→damage 35→0: scars+3 (no "entering damaged")

- **AC-11**: Band re-entry
  - Given: integrity=20(critical)→repair→30(damaged)→damage 10→20(critical)
  - Then: second event scars increase = +2 (base +1, entering critical +1)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/modules/HullIntegrityTest.csproj` — must exist and pass
**Status**: [x] PASS — 4/4 checks in `tests/unit/modules/HullIntegrityTest.csproj`; 2026-05-13 Epic #8 复审复跑通过

---

## Dependencies

- Depends on: Story 001 (module slot state for η_final interaction), resources-goods-capacity Epic (repair_kit material, consume_for_module)
- Unlocks: Story 004 (can_depart uses band state), Story 007 (hull snapshot), combat-threat Epic (apply_hull_damage caller)
