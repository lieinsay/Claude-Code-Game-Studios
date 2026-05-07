# Story 006: Edge Cases & Defensive Error Handling

> **Epic**: Combat / Threat Resolution
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/combat-threat-handling.md`
**Requirement**: `TR-combat-001`, `TR-combat-002`, `TR-combat-003`

**ADR Governing Implementation**: ADR-0018 (§5 settlement sequence ordering for EC-12-07, §7 persistence strategy for EC-12-05/08, §9 signal events)
**ADR Decision Summary**: GDD 定义了 10 个边缘案例覆盖：低船体硬扛→hull=0、硬扛伤害跨越波段边界、全部模块槽位为空、模块损伤命中已受损槽位、撤退后应急处理同一威胁、击退方向退化、硬扛 hull=0 同时模块损伤、多次撤退标记、repair_kit 零数量堆、#12 未实现或不可用。本 Story 作为系统级边缘案例的集中验证，确保所有跨 Story 的边缘行为正确、防御性检查到位。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: hull=0 不终止探索——玩家仍可撤离；eligible_modules 按 actual_state 过滤——不选 damaged 槽位；retreat_flagged 跨威胁持久化——应急处理不清除；C4 结算顺序保证 hull_damage 先于 module_damage 应用
- Forbidden: hull=0 时终止探索或阻止撤离；模块损伤选中已受损槽位（二次受损）；击退方向退化时崩溃
- Guardrail: hull≤33 时显示交叉波段警告——不阻止选择；#12 不可用时威胁点保持 inert——不阻塞探索流程

---

## Acceptance Criteria

### EC-12-01: Low Hull + Tank → hull=0

- [ ] **AC-1**: GIVEN hull=10 + player selects tank + damage roll=12，WHEN C4 步骤 5 apply_hull_damage(12)，THEN integrity = max(0, 10-12) = 0。hull_band→destroyed。探索不终止——玩家仍可撤离。HUD 显示"船体严重损毁"警告
- [ ] **AC-2**: GIVEN hull=0（destroyed），WHEN 后续 #8 can_depart()，THEN 返回 {false, ["hull_destroyed"]}。船体不可再出航。但撤离锚点仍然可用

### EC-12-02: Tank Damage Crosses Band Boundary

- [ ] **AC-3**: GIVEN hull=33（damaged 波段）+ tank damage=8，WHEN integrity→25，THEN 跨越 damaged→critical 波段。hull≤33 时硬扛按钮悬停显示"硬扛可能造成船体结构性恶化"预览
- [ ] **AC-4**: GIVEN hull=76（intact 波段）+ tank damage=8，WHEN integrity→68，THEN 仍 intact。无波段跨越警告

### EC-12-03: All Module Slots Empty

- [ ] **AC-5**: GIVEN slot_a=empty + slot_b=empty + player selects tank，WHEN calc_module_damage()，THEN eligible_modules=[] → {module_damaged: false}。不崩溃。决策面板不显示模块风险提示

### EC-12-04: Module Damage Targets Already Damaged Slot

- [ ] **AC-6**: GIVEN slot_a.actual_state=damaged + slot_b.actual_state=installed，WHEN calc_module_damage() 投骰命中，THEN eligible_modules 仅包含 slot_b（按 actual_state 过滤）。slot_a 不被选中
- [ ] **AC-7**: GIVEN slot_a.visible_state=unchecked (but actual_state=installed) + slot_b.actual_state=installed，WHEN eligible_modules，THEN slot_a 仍包含——过滤使用 actual_state 而非 visible_state

### EC-12-05: Retreat Then Emergency Handling Same Threat

- [ ] **AC-8**: GIVEN 玩家先撤退（retreat_flagged=true），后获得 repair_kit，返回同一威胁点选择应急处理，WHEN 结算，THEN threat→suppressed (is_active=false)。retreat_flagged 保持 true——应急处理不清除。撤离损耗 λ_forced=0.25 仍生效

### EC-12-06: Knockback Direction Degenerate

- [ ] **AC-9**: GIVEN player.position == threat.position（方向零向量），WHEN calc_knockback()，THEN fallback: threat.facing 方向。若 threat 无 facing→随机单位向量。不崩溃，不返回零向量

### EC-12-07: Tank → hull=0 + Simultaneous Module Damage

- [ ] **AC-10**: GIVEN hull=8 + tank damage=12 + module_damage 投骰命中，WHEN C4 结算，THEN:
  - 步骤 5 先执行: apply_hull_damage(12) → integrity=0
  - 步骤 6 后执行: apply_module_damage(slot, "guard_impact") → 模块标记为 damaged
  - 船体 destroyed 波段下 η_effective = η_visible × 0 = 0（#8 D.2b rule）
  - 修复船体后模块效率恢复至其 damaged 状态对应值

### EC-12-08: Multiple Retreats

- [ ] **AC-11**: GIVEN 同一探索会话中对 3 个不同威胁选择撤退，WHEN 所有结算完成，THEN retreat_flagged 保持 true（布尔值——不叠加）。撤离损耗 λ_forced=0.25——不由撤退次数决定
- [ ] **AC-12**: GIVEN retreat_flagged=true（前次撤退）+ 对另一威胁选择硬扛，WHEN 结算，THEN combat_result.retreat_flagged=true。硬扛不清除撤退标记

### EC-12-09: repair_kit Zero-Quantity Stack

- [ ] **AC-13**: GIVEN Pool 5 含 repair_kit ×0（空堆异常），WHEN check_emergency_available()，THEN quantity=0 < 1 → false。应急处理按钮灰显。零数量堆不应正常存在，但系统安全处理

### EC-12-10: #12 Unavailable

- [ ] **AC-14**: GIVEN #11 调用 resolve_threat() 但 #12 未实现/不可用，WHEN Exploration 防御性检查，THEN 威胁保持 inert（不触发、不伤害）。threat.is_active 保持 true。当 #12 就绪后正常结算

### Additional Defensive Guards

- [ ] **AC-15**: GIVEN threat_context 为 null 或非 Dictionary，WHEN resolve_threat()，THEN 返回 {error: "ERR_INVALID_CONTEXT"}。不崩溃
- [ ] **AC-16**: GIVEN encounter_params 为 null 或缺失，WHEN 读取配置值，THEN 全部使用 DEFAULT_THREAT_PARAMS 安全默认值
- [ ] **AC-17**: GIVEN response_choice 不是 ["emergency_handling", "tank", "retreat"] 之一，WHEN _execute_settlement()，THEN 返回 {error: "ERR_INVALID_RESPONSE"}。不执行任何结算步骤

---

## Implementation Notes

### EC-12-01: Low Hull Tank Guard

```gdscript
func _execute_settlement(response_choice: StringName) -> Dictionary:
    # ... 步骤 1-4 ...

    # 步骤 5: 应用船体伤害
    if hull_damage > 0:
        var hull_before: int = ModuleHullManager.get_hull_integrity()
        ModuleHullManager.apply_hull_damage(hull_damage)
        var hull_after: int = ModuleHullManager.get_hull_integrity()

        # EC-12-01: hull=0 不终止探索——仅记录
        if hull_after <= 0:
            push_warning("Combat: hull reached 0 after tank damage. Exploration continues; departure blocked.")

    # 步骤 6: 应用模块损伤（即使 hull=0 仍执行——正确记录模块状态）
    if module_damage.get("module_damaged", false):
        ModuleHullManager.apply_module_damage(
            module_damage["target_slot_id"], &"guard_impact"
        )
```

### EC-12-04: eligible_modules Filter

```gdscript
func _get_eligible_module_slots() -> Array[StringName]:
    var eligible: Array[StringName] = []
    for slot_id in [&"slot_a", &"slot_b"]:
        var actual_state: int = ModuleHullManager.get_slot_actual_state(slot_id)
        # EC-12-04: 按 actual_state 过滤——仅 installed 槽位可被选中
        # actual_state=damaged → 排除（无论 visible_state 为何）
        # actual_state=empty → 排除
        # actual_state=installed (even if visible_state=unchecked) → 包含
        if actual_state == ModuleState.INSTALLED:
            eligible.append(slot_id)
    return eligible
```

### EC-12-05: retreat_flagged Persistence

```gdscript
# CombatManager 内部状态
var _current_retreat_flagged: bool = false

func _execute_settlement(response_choice: StringName) -> Dictionary:
    # ...
    var result: Dictionary = {
        # ...
        "retreat_flagged": _current_retreat_flagged or (response_choice == &"retreat"),
    }

    # EC-12-05: retreat_flagged 一旦设置，不被应急处理或硬扛清除
    if response_choice == &"retreat":
        _current_retreat_flagged = true

    return result


func reset_retreat_flagged() -> void:
    # 仅在探索会话结束时由 #11 调用
    _current_retreat_flagged = false
```

### EC-12-06: Knockback Direction Fallback

```gdscript
func calc_knockback(response_choice: StringName, encounter_params: Dictionary,
                    threat_context: Dictionary) -> Dictionary:
    # ... distance calculation ...

    var threat_pos: Vector2 = threat_context.get("position", Vector2.ZERO)
    var player_pos: Vector2 = _get_player_position()
    var direction: Vector2 = (player_pos - threat_pos).normalized()

    # EC-12-06: 退化方向 fallback
    if direction == Vector2.ZERO or direction.length_squared() < 0.0001:
        direction = threat_context.get("facing", Vector2.ZERO) as Vector2
        if direction == Vector2.ZERO or direction.length_squared() < 0.0001:
            # 随机单位向量——最后手段
            var angle: float = randf() * TAU
            direction = Vector2(cos(angle), sin(angle))
        else:
            direction = direction.normalized()

    return {"direction": direction, "distance": distance}
```

### EC-12-10: #12 Unavailable Guard

```gdscript
# 此防御属于 #11 Exploration
func _on_threat_triggered(threat_context: Dictionary) -> void:
    if not Combat or not Combat.has_method("resolve_threat"):
        # EC-12-10: #12 不可用——威胁保持 inert
        push_warning("Exploration: CombatManager unavailable — threat %s remains inert" %
            threat_context.get("threat_id", &"?"))
        return
    var result: Dictionary = Combat.resolve_threat(threat_context)
    _handle_combat_result(result)
```

### Defensive Input Validation

```gdscript
const VALID_RESPONSES: Array[StringName] = [&"emergency_handling", &"tank", &"retreat"]

func _submit_response(response_choice: StringName) -> void:
    if response_choice not in VALID_RESPONSES:
        push_error("Combat: invalid response_choice: %s" % response_choice)
        return

    if _state != CombatState.AWAITING_RESPONSE:
        return

    _state = CombatState.PROCESSING
    # ...


func resolve_threat(threat_context: Dictionary) -> Dictionary:
    if threat_context == null or not threat_context is Dictionary:
        push_error("Combat: resolve_threat called with null or invalid threat_context")
        return {"error": "ERR_INVALID_CONTEXT"}

    if not threat_context.has("encounter_params") or not threat_context["encounter_params"] is Dictionary:
        threat_context["encounter_params"] = DEFAULT_THREAT_PARAMS.duplicate()

    # ...
```

---

## Out of Scope

- hull=0 时的 HUD 警告显示——属于 #16 UIManager
- λ_forced=0.25 撤离损耗计算——属于 #11 Exploration
- 修复船体后模块效率恢复——属于 #8 ModuleHullManager
- Cross-band preview 的具体 UI 实现——属于 #16 UIManager 的决策面板渲染

---

## QA Test Cases

- **AC-1/2**: EC-12-01 — hull=10 + tank(12) → integrity=0; exploration continues; can_depart→false
- **AC-3/4**: EC-12-02 — hull=33→25 crosses band; hull=76→68 stays intact
- **AC-5**: EC-12-03 — empty slots→no module risk
- **AC-6/7**: EC-12-04 — actual_state filter; unchecked→included
- **AC-8**: EC-12-05 — retreat+emergency→retreat_flagged stays true
- **AC-9**: EC-12-06 — zero vector→fallback
- **AC-10**: EC-12-07 — hull_damage before module_damage in C4
- **AC-11/12**: EC-12-08 — multi-retreat→bool; tank after retreat→still true
- **AC-13**: EC-12-09 — repair_kit×0→emergency unavailable
- **AC-14**: EC-12-10 — #12 unavailable→threat inert

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/combat/edge_cases_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: All prior combat stories (001-005), modules-hull-state Epic, resources-goods-capacity Epic, exploration-scavenge Epic
- Unlocks: Combat system-level integration testing, QA smoke tests for threat resolution flows
