# Story 003: Damage, Module & Knockback Formulas

> **Epic**: Combat / Threat Resolution
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/combat-threat-handling.md`
**Requirement**: `TR-combat-003`

**ADR Governing Implementation**: ADR-0018 (§4 response options damage values, §5 settlement sequence steps 3/4/8)
**ADR Decision Summary**: 威胁结算的核心公式为 3 个纯函数——calc_hull_damage（硬扛时 uniform_int(8,12)，其他 0）、calc_module_damage（仅硬扛，P=0.30 命中 + eligible_modules 过滤仅 actual_state=installed 的槽位）、calc_knockback（应急处理=0，硬扛=8.0，撤退=10.0）。所有伤害值来自 encounter_params（威胁配置表 C8），CombatManager 内部不硬编码数值。check_emergency_available 查询 Pool 5 中 repair_kit ≥ 1。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: calc_hull_damage 仅 "tank" 产生伤害——其他响应返回 0；calc_module_damage eligible_modules 按 actual_state 过滤——排除 actual_state=damaged 的槽位；calc_knockback 方向 = threat.position → player.position
- Forbidden: 硬扛伤害取其他分布（必须是 uniform）；模块损伤判定使用 visible_state 而非 actual_state；击退方向退化时崩溃（必须 fallback）
- Guardrail: hull_damage 范围 8-12 各值 1/5 概率；knockback_distance_tanked(8.0) > trigger_radius_max(6.0)

---

## Acceptance Criteria

### Formula F-12-02: calc_hull_damage

- [ ] **AC-1**: GIVEN response_choice="tank" + encounter_params={full_damage_min:8, full_damage_max:12}，WHEN calc_hull_damage()，THEN 返回值 ∈ [8, 12]。闭区间均匀随机——每个整数概率 1/5
- [ ] **AC-2**: GIVEN response_choice="emergency_handling"，WHEN calc_hull_damage()，THEN = 0
- [ ] **AC-3**: GIVEN response_choice="retreat"，WHEN calc_hull_damage()，THEN = 0
- [ ] **AC-4**: GIVEN 1,000 次 calc_hull_damage("tank", guard_params)，WHEN 统计分布，THEN 每个值 (8,9,10,11,12) 出现次数 ≈ 200（±50 容差）。无值超出 [8,12]

### Formula F-12-03: calc_module_damage

- [ ] **AC-5**: GIVEN response_choice="tank" + encounter_params.module_damage_chance=0.30 + eligible_modules=[slot_a, slot_b]，WHEN calc_module_damage()，THEN:
  - 30%: randf() < 0.30 → {module_damaged: true, target_slot_id: slot_a 或 slot_b}
  - 70%: → {module_damaged: false, target_slot_id: null}
- [ ] **AC-6**: GIVEN response_choice="tank" + eligible_modules=[]（所有槽位空或已受损），WHEN calc_module_damage()，THEN count(eligible_modules)=0 → {module_damaged: false, target_slot_id: null}。不崩溃
- [ ] **AC-7**: GIVEN slot_a 的 actual_state=damaged + slot_b actual_state=installed，WHEN 构建 eligible_modules，THEN eligible_modules 仅包含 slot_b。slot_a 被 actual_state 过滤排除
- [ ] **AC-8**: GIVEN slot_a 的 visible_state=unchecked（但 actual_state=installed），WHEN 构建 eligible_modules，THEN slot_a 仍包含在 eligible_modules 中——过滤使用 actual_state 而非 visible_state
- [ ] **AC-9**: GIVEN response_choice="emergency_handling" 或 "retreat"，WHEN calc_module_damage()，THEN → {module_damaged: false, target_slot_id: null}

### Formula F-12-04: check_emergency_available

- [ ] **AC-10**: GIVEN Pool 5 含 repair_kit ≥ 1，WHEN check_emergency_available()，THEN = true
- [ ] **AC-11**: GIVEN Pool 5 含 repair_kit = 0（空堆），WHEN check_emergency_available()，THEN = false。0 < 1
- [ ] **AC-12**: GIVEN Pool 5 不含 repair_kit key，WHEN carried_inventory.get("repair_kit", 0)，THEN 默认值 0 → false。不崩溃

### Formula F-12-05: calc_knockback

- [ ] **AC-13**: GIVEN response_choice="emergency_handling"，WHEN calc_knockback()，THEN distance=0。无击退
- [ ] **AC-14**: GIVEN response_choice="tank" + params.knockback_distance_tanked=8.0，WHEN calc_knockback()，THEN {direction: threat→player normalized, distance: 8.0}
- [ ] **AC-15**: GIVEN response_choice="retreat" + params.knockback_distance_retreat=10.0，WHEN calc_knockback()，THEN {direction: threat→player normalized, distance: 10.0}
- [ ] **AC-16**: GIVEN 玩家与威胁位置重叠（方向零向量退化），WHEN calc_knockback()，THEN fallback: 使用威胁 facing 方向。若 threat 无 facing → 使用随机单位向量。不崩溃

---

## Implementation Notes

### calc_hull_damage

```text
func calc_hull_damage(response_choice: StringName, encounter_params: Dictionary) -> int:
    if response_choice != &"tank":
        return 0

    var d_min: int = encounter_params.get("full_damage_min", 8)
    var d_max: int = encounter_params.get("full_damage_max", 12)
    return randi_range(d_min, d_max)
```

### calc_module_damage

```text
func calc_module_damage(response_choice: StringName, encounter_params: Dictionary) -> Dictionary:
    if response_choice != &"tank":
        return {"module_damaged": false, "target_slot_id": &""}

    var chance: float = encounter_params.get("module_damage_chance", 0.30)
    if randf() >= chance:
        return {"module_damaged": false, "target_slot_id": &""}

    var eligible: Array[StringName] = _get_eligible_module_slots()
    if eligible.is_empty():
        return {"module_damaged": false, "target_slot_id": &""}

    var target: StringName = eligible[randi() % eligible.size()]
    return {"module_damaged": true, "target_slot_id": target}


func _get_eligible_module_slots() -> Array[StringName]:
    var eligible: Array[StringName] = []
    for slot_id in [&"slot_a", &"slot_b"]:
        var actual_state: int = ModuleHullManager.get_slot_actual_state(slot_id)
        if actual_state == ModuleState.INSTALLED:
            eligible.append(slot_id)
    return eligible
```

### check_emergency_available

```text
func check_emergency_available() -> bool:
    var carried: Dictionary = ResourcesManager.get_carried_contents_by_tag("repair-material")
    return carried.get("repair_kit", 0) >= 1
```

### calc_knockback

```text
func calc_knockback(response_choice: StringName, encounter_params: Dictionary,
                    threat_context: Dictionary) -> Dictionary:
    var distance: float = 0.0
    match response_choice:
        &"tank":
            distance = encounter_params.get("knockback_distance_tanked", 8.0)
        &"retreat":
            distance = encounter_params.get("knockback_distance_retreat", 10.0)
        _:
            return {"direction": Vector2.ZERO, "distance": 0.0}

    var threat_pos: Vector2 = threat_context.get("position", Vector2.ZERO)
    var player_pos: Vector2 = _get_player_position()
    var direction: Vector2 = (player_pos - threat_pos).normalized()

    # 退化方向 fallback
    if direction == Vector2.ZERO:
        direction = _get_threat_facing(threat_context)
        if direction == Vector2.ZERO:
            direction = Vector2(randf() * 2.0 - 1.0, randf() * 2.0 - 1.0).normalized()

    return {"direction": direction, "distance": distance}


func _get_player_position() -> Vector2:
    return Exploration.get_player_position()


func _get_threat_facing(threat_context: Dictionary) -> Vector2:
    return threat_context.get("facing", Vector2.ZERO)
```

---

## Out of Scope

- #8.get_slot_actual_state() 的具体实现——属于 modules-hull-state Epic
- #5.get_carried_contents_by_tag() 的具体实现——属于 resources-goods-capacity Epic
- #11.get_player_position() 的具体实现——属于 exploration-scavenge Epic
- 击退位移的具体执行（move_and_collide）——属于 #11 Exploration

---

## QA Test Cases

- **AC-1 through AC-4**: calc_hull_damage
  - tank→8-12 uniform; others→0; 1000 trials distribution check

- **AC-5 through AC-9**: calc_module_damage
  - eligible_modules filter by actual_state; empty→no damage; unchecked but installed→included
  - emergency/retreat→no module risk

- **AC-10/11/12**: check_emergency_available
  - kit≥1→true; kit=0→false; no key→false

- **AC-13 through AC-16**: calc_knockback
  - emergency→0; tank→8.0; retreat→10.0; zero vector→fallback

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/combat/DamageFormulasTest.csproj` — must exist and pass
**Status**: [x] Passing — `dotnet run --project tests/unit/combat/DamageFormulasTest.csproj -p:UseSharedCompilation=false` (4/4 grouped checks PASS, 2026-05-14)

## Completion Evidence — 2026-05-14

- Implemented in `src/core/combat/CombatManager.cs`.
- Test runner: `tests/unit/combat/DamageFormulasTest.csproj`.
- Acceptance coverage:
  - AC-1 through AC-4: `CalcHullDamage` returns uniform integer damage in [8,12] for tank and 0 for emergency/retreat.
  - AC-5 through AC-9: `CalcModuleDamage` uses module chance, empty-safe handling, and actual_state installed filtering while ignoring visible_state as an eligibility blocker.
  - AC-10 through AC-12: `CheckEmergencyAvailable` reads carried repair materials and treats zero/missing stacks as unavailable.
  - AC-13 through AC-16: `CalcKnockback` returns 0/null for emergency, configured tank/retreat distances, normalized threat-to-player direction, facing fallback, and final random unit fallback.

---

## Dependencies

- Depends on: modules-hull-state Epic (get_slot_actual_state, ModuleState enum), resources-goods-capacity Epic (get_carried_contents_by_tag), exploration-scavenge Epic (get_player_position, is_threat_active)
- Unlocks: Story 002 (response resolution calls these formulas), Story 006 (EC-12-01/03/04/06/07 edge cases)
