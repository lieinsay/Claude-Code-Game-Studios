# Story 004: Damage Accumulation & Dynamic Hull Band Transitions

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-002`, `TR-navigation-003`

**ADR Governing Implementation**: ADR-0010 (EncounterContext — accumulated_damage, hull_band_arrival, damaged_slots 字段), ADR-0009 (ModuleHullManager — apply_hull_damage, apply_module_damage 接口)
**ADR Decision Summary**: Formula 4 — d_check = max(d_entry_1, ..., d_entry_k)：单次遭遇检查可能命中多个风险标签，取所有命中条目伤害最大值而非求和。D_accumulated 在航行中实时累积于内存，航行结束时一次性写入 #8。hull_integrity_effective = max(0, hull_integrity_departure - D_accumulated)。Option B 动态船体波段转换：当 hull_integrity_effective 跨越波段阈值时，s_hull 和 Δ_hull 立即更新，T_voyage 和 T_check 重算。已调度但未触发的检查时间不回溯——仅下次新检查使用新 T_check。超量伤害丢弃（不产生负值）。波段边界：hull≥76=intact, 26-75=damaged, 1-25=critical, ≤0=destroyed。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: d_check = max() 非累加——防止多标签航线伤害不合理叠加；超量伤害丢弃——hull_integrity_effective 最低为 0；波段转换时已调度检查不回溯——仅新检查用新 T_check
- Forbidden: 每个标签的伤害累加（如 3+4=7）；波段转换时进度条跳回——进度保持当前%前进
- Guardrail: 单次检查伤害上限 6 点——一次检查不可能跨越两个波段；超出 hull_integrity_departure 的伤害丢弃

---

## Acceptance Criteria

### Formula 4 — Single Check Damage (max rule)

- [ ] **AC-1**: GIVEN 同一次检查命中 storm→turbulence_zone(d=3) + low-visibility→hidden_reef(d=4)，WHEN d_check，THEN = max(3, 4) = 4。非 3+4=7
- [ ] **AC-2**: GIVEN 同一次检查命中 3 个标签 d={2, 0, 5}，WHEN d_check，THEN = max(2, 0, 5) = 5
- [ ] **AC-3**: GIVEN 一次检查零命中（所有标签隐藏未揭示/标签集为空），WHEN d_check = max(空集)，THEN = 0——显式定义

### Formula 4 — D_accumulated and hull_integrity_effective

- [ ] **AC-4**: GIVEN hull_departure=85 + D_accumulated=18，WHEN hull_effective，THEN = max(0, 85-18) = 67。仍在 damaged 波段 (26-75)
- [ ] **AC-5**: GIVEN hull_departure=3 + d_check=6，WHEN hull_effective，THEN = max(0, 3-6) = 0。超量伤害 3 点丢弃——不产生负值
- [ ] **AC-6**: GIVEN hull_effective reach 0，WHEN 检测，THEN voyage_state → FORCED_LANDING。FORCED_LANDING 优先于 ARRIVED

### Option B — Dynamic Hull Band Transitions

- [ ] **AC-7**: GIVEN intact (hull≥76) + D_accumulated 使 hull_effective→75，WHEN 波段跨越，THEN:
  - s_hull 从 1.0→0.9, Δ_hull 从 0→-0.10
  - T_voyage 重算: T_distance/0.9 + ΣT_flat + ΣT_temp（基准变长）
  - T_check 重算: 12×(1-0.10)=10.8s
  - 进度条不跳回——当前%保持，到达 100% 时间变长
  - 波段变更事件发射
- [ ] **AC-8**: GIVEN damaged (hull≤75) + D_accumulated 使 hull_effective→25，WHEN 波段跨越，THEN:
  - s_hull 从 0.9→0.75, Δ_hull 从 -0.10→-0.20
  - T_check 从 10.8s→9.6s
- [ ] **AC-9**: GIVEN hull_effective 跨越多波段（一次检查），WHEN 处理，THEN 逐波段触发——每个阈值跨越时发出独立事件。当前最大单次伤害 6 点，一次检查不可能跨两个波段

### Band Boundary Values

- [ ] **AC-10**: GIVEN hull=76，WHEN _get_hull_band(76)，THEN → intact (≥76)
- [ ] **AC-11**: GIVEN hull=75，WHEN _get_hull_band(75)，THEN → damaged (26-75)
- [ ] **AC-12**: GIVEN hull=25，WHEN _get_hull_band(25)，THEN → critical (1-25)
- [ ] **AC-13**: GIVEN hull=0，WHEN _get_hull_band(0)，THEN → destroyed (≤0)

### Module Damage During Voyage

- [ ] **AC-14**: GIVEN lightning_proximity 遭遇 + 20% 概率击中侦察模块，WHEN 命中，THEN 调用 ModuleHullManager.apply_module_damage("slot_a"/"slot_b", "lightning_strike")。η_scout 立即更新
- [ ] **AC-15**: GIVEN 侦察槽为空（无模块安装），WHEN lightning_proximity 触发，THEN 跳过模块伤害检定。不崩溃

### Damage Writing at Voyage End

- [ ] **AC-16**: GIVEN voyage_state→ARRIVED + D_accumulated=18，WHEN _finalize_voyage()，THEN 调用 ModuleHullManager.apply_hull_damage(18)
- [ ] **AC-17**: GIVEN voyage_state→RETREATED + D_accumulated=5，WHEN _finalize_voyage()，THEN 调用 ModuleHullManager.apply_hull_damage(5)。撤退不减免伤害

---

## Implementation Notes

### Formula 4 — Damage Accumulation

```text
func _resolve_encounter_check() -> void:
    var hits: Array[Dictionary] = _collect_encounter_hits()
    if hits.is_empty():
        d_check = 0
    else:
        d_check = _max_damage(hits)

    _accumulated_damage += d_check
    _last_check_time = _elapsed_time

    # 记录已结算遭遇
    for hit in hits:
        _resolved_encounters.append(hit)

    # 检查船体波段转换
    _check_hull_band_transition()

    # 检查迫降
    if _get_hull_integrity_effective() <= 0:
        _voyage_state = &"FORCED_LANDING"
        _finalize_voyage()

    # 处理特殊效果
    _apply_special_effects(hits)


func _max_damage(hits: Array[Dictionary]) -> int:
    if hits.is_empty():
        return 0
    var max_d: int = 0
    for hit in hits:
        max_d = maxi(max_d, hit.get("damage_amount", 0))
    return max_d


func _get_hull_integrity_effective() -> int:
    var departure: int = _active_voyage.get("hull_integrity_departure", 100)
    return maxi(0, departure - _accumulated_damage)
```

### Dynamic Hull Band Transitions

```text
func _get_hull_band(integrity: int) -> StringName:
    if integrity >= 76:
        return &"intact"
    elif integrity >= 26:
        return &"damaged"
    elif integrity >= 1:
        return &"critical"
    else:
        return &"destroyed"


func _check_hull_band_transition() -> void:
    var old_band: StringName = _active_voyage.get("hull_band", &"intact")
    var effective_integrity: int = _get_hull_integrity_effective()
    var new_band: StringName = _get_hull_band(effective_integrity)

    if new_band == old_band:
        return

    # 波段变更
    _active_voyage["hull_band"] = new_band

    # 重算 T_voyage 和 T_check
    recalculate_voyage_duration_for_band_change(new_band)
    var new_t_check: float = calculate_check_interval()

    # 已调度但未触发的检查时间不回溯——仅下次检查用新 T_check
    # 进度条不跳回——进度保持当前百分比前进

    # 发射波段变更事件
    hull_band_changed_during_voyage.emit(old_band, new_band, effective_integrity)
```

### Module Damage During Voyage

```text
func _apply_module_damage_if_hit(special_effects: Array[StringName]) -> void:
    if &"module_damage_20pct_scout" in special_effects:
        if randf() < 0.20:
            # 查找安装了 scout 的槽位
            for slot_id in [&"slot_a", &"slot_b"]:
                if ModuleHullManager.get_slot_module_type(slot_id) == ModuleType.SCOUT:
                    var state: int = ModuleHullManager.get_slot_actual_state(slot_id)
                    if state != ActualState.EMPTY:
                        ModuleHullManager.apply_module_damage(slot_id, &"lightning_strike")
                        _damaged_slots.append(slot_id)
                        _on_scout_efficiency_changed(get_effective_scout_efficiency())
                        break
```

### Damage Writing at Voyage End

```text
func _write_damage_to_hull() -> void:
    if _accumulated_damage > 0:
        ModuleHullManager.apply_hull_damage(_accumulated_damage)

    for slot_id in _damaged_slots:
        # 已在遭遇结算时通过 apply_module_damage 实时写入——此处为最终确认
        pass
```

---

## Out of Scope

- apply_hull_damage() 和 apply_module_damage() 的具体实现——属于 #8 ModuleHullManager
- 波段变更的 UI 反馈和视觉效果——属于 UI #16 和反馈 #17
- 船体波段的具体影响（速度系数、检查间隔偏移）已在 Story 002 中定义

---

## QA Test Cases

- **AC-1/2/3**: max() damage rule
  - 多标签 → max; 空集 → 0

- **AC-4/5/6**: D_accumulated
  - 85-18=67; 3-6=0 (overflow discarded); hull→0 → FORCED_LANDING

- **AC-7/8/9**: Dynamic band transitions
  - intact→damaged (76→75): s=1.0→0.9, Δ=0→-0.10, T_check: 12→10.8s
  - damaged→critical (26→25): s=0.9→0.75, Δ=-0.10→-0.20, T_check: 10.8→9.6s
  - Progress bar: % stays, ETA increases — no jump back

- **AC-10/11/12/13**: Band boundaries
  - 76=intact, 75=damaged, 25=critical, 0=destroyed

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/navigation/damage/DamageHullBandTest.csproj` — must exist and pass
**Status**: [x] 22/22 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (VoyageContext, hull_integrity_departure), Story 002 (T_voyage, T_check, recalculate), modules-hull-state Epic (apply_hull_damage, apply_module_damage, get_hull_band, ModuleType, ActualState enum)
- Unlocks: Story 005 (damage as part of EncounterEntry), Story 008 (EC-01/06/11/12/13/17)
