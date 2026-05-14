# Story 002: Response Resolution & Settlement Sequence

> **Epic**: Combat / Threat Resolution
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/combat-threat-handling.md`
**Requirement**: `TR-combat-001`, `TR-combat-002`

**ADR Governing Implementation**: ADR-0018 (3 response options, C4 settlement sequence 10 steps, resolve_threat() single entry point)
**ADR Decision Summary**: 威胁结算有三种玩家响应——应急处理（消耗 1 repair_kit，清除威胁，0 伤害）、硬扛（承受 uniform_int(8,12) 伤害 + 30% 模块风险 + 击退 8 单位）、撤退（0 伤害 + retreat_flagged + 击退 10 单位）。C4 结算序列严格按 10 步顺序执行：(1)验证可用条件 → (2)执行资源消耗 → (3)计算船体伤害 → (4)判定模块损伤 → (5)应用船体伤害 → (6)应用模块损伤 → (7)更新威胁状态 → (8)执行击退 → (9)返回 combat_result → (10)#11 恢复探索。应急处理不可用时灰显，硬扛在 hull≤12 时附加警告标记，撤退始终可用。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 结算序列严格按 C4 顺序——不可打乱；应急处理可用性在步骤 1 校验——不满足时硬失败返回 ERR_UNAVAILABLE；硬扛伤害 uniform_int(8,12)——5 个整数值各 1/5 概率
- Forbidden: 在未验证可用性的情况下执行消耗；跳过模块损伤判定直接标记模块（必须走 P=0.30 投骰）
- Guardrail: hull≤12 时硬扛按钮显示警告但不阻止选择；retreat_flagged 一旦设置不可在当前会话中清除

---

## Acceptance Criteria

### Emergency Handling Response

- [ ] **AC-1**: GIVEN Pool 5 含 repair_kit ≥ 1 + 玩家选择"应急处理"，WHEN 结算，THEN:
  - consume_in_combat("repair_kit", 1) 调用 #5
  - hull_damage = 0
  - module_damage = null
  - threat.is_active = false (suppressed)
  - knockback = null
  - retreat_flagged = false
  - outcome = "suppressed"
- [ ] **AC-2**: GIVEN Pool 5 含 repair_kit = 0 + 玩家尝试选择"应急处理"，WHEN check_emergency_available()，THEN 返回 false。按钮灰显 + tooltip "需要 repair_kit ×1（随身物品栏中无可用）"。若强制调用 → 步骤 1 返回 ERR_UNAVAILABLE

### Tank Response

- [ ] **AC-3**: GIVEN 玩家选择"硬扛" + encounter_params={full_damage_min:8, full_damage_max:12}，WHEN 结算，THEN:
  - resources_consumed = null（无消耗）
  - hull_damage = uniform_int(8, 12)
  - module_damage: 30% 概率命中 {slot_id, damage_type:"guard_impact"} 或 null（70%）
  - threat.is_active = true（保持活跃）
  - knockback = {direction: threat→player, distance: 8.0}
  - retreat_flagged = false
  - outcome = "tanked"
- [ ] **AC-4**: GIVEN hull ≤ 12 + 玩家查看硬扛选项，WHEN UI 渲染，THEN 按钮标签附加 "⚠ 船体严重受损"。按钮仍可点击——警告不阻止选择
- [ ] **AC-5**: GIVEN hull ≤ 33（damaged 波段内，最小伤害 8 将 integrity 推入 ≤25 critical 波段），WHEN 玩家悬停在硬扛按钮上，THEN 面板显示交叉波段预览："硬扛可能造成船体结构性恶化"

### Retreat Response

- [ ] **AC-6**: GIVEN 玩家选择"撤退"，WHEN 结算，THEN:
  - hull_damage = 0
  - module_damage = null
  - resources_consumed = null
  - threat.is_active = true（保持活跃）
  - knockback = {direction: threat→player, distance: 10.0}
  - retreat_flagged = true
  - outcome = "retreated"
- [ ] **AC-7**: GIVEN retreat_flagged = true（前次已撤退），WHEN 玩家再次选择"撤退"，THEN retreat_flagged 保持 true。不叠加——布尔值

### Settlement Sequence (C4) Order

- [ ] **AC-8**: GIVEN 玩家选择响应，WHEN _execute_settlement() 执行，THEN 步骤严格按此顺序：
  1. 验证可用条件（不满足→ERR_UNAVAILABLE，不消耗/不改变状态）
  2. 执行资源消耗（仅应急处理→#5.consume_in_combat）
  3. 计算船体伤害（仅硬扛→uniform_int(8,12)）
  4. 判定模块损伤（仅硬扛→P=0.30 投骰）
  5. 应用船体伤害→#8.apply_hull_damage
  6. 应用模块损伤→#8.apply_module_damage
  7. 更新威胁状态（仅应急处理→is_active=false）
  8. 执行击退
  9. 返回 combat_result
  10. #11 恢复探索（threatened→exploring）
- [ ] **AC-9**: GIVEN 步骤 1 验证失败（如选择应急处理但 repair_kit=0），WHEN 检测，THEN 返回 ERR_UNAVAILABLE。不执行步骤 2-10。不改变任何状态

### Decision Breath Behavior

- [ ] **AC-10**: GIVEN state=AWAITING_RESPONSE + 决策面板显示，WHEN 计时，THEN 不限时。无计时器——玩家可无限期停留在决策中
- [ ] **AC-11**: GIVEN AWAITING_RESPONSE + 玩家按 Esc 关闭面板，WHEN 面板关闭，THEN 探索保持暂停。面板可重新打开。状态保持 AWAITING_RESPONSE

---

## Implementation Notes

### _execute_settlement()

```text
func _execute_settlement(response_choice: StringName) -> Dictionary:
    var ctx: Dictionary = _current_threat_context
    var params: Dictionary = ctx.get("encounter_params", {})

    # 步骤 1: 验证可用条件
    if response_choice == &"emergency_handling":
        if not check_emergency_available():
            return {"error": "ERR_UNAVAILABLE", "reason": "repair_kit not available"}

    # 步骤 2: 执行资源消耗
    var resources_consumed: Array = []
    if response_choice == &"emergency_handling":
        ResourcesManager.consume_in_combat("repair_kit", 1)
        resources_consumed.append({"resource_id": &"repair_kit", "quantity": 1})

    # 步骤 3: 计算船体伤害
    var hull_damage: int = calc_hull_damage(response_choice, params)

    # 步骤 4: 判定模块损伤
    var module_damage: Dictionary = calc_module_damage(response_choice, params)

    # 步骤 5: 应用船体伤害
    if hull_damage > 0:
        ModuleHullManager.apply_hull_damage(hull_damage)

    # 步骤 6: 应用模块损伤
    if module_damage.get("module_damaged", false):
        var slot_id: StringName = module_damage.get("target_slot_id", &"")
        ModuleHullManager.apply_module_damage(slot_id, &"guard_impact")

    # 步骤 7: 更新威胁状态
    var threat_result: StringName = &"active"
    if response_choice == &"emergency_handling":
        Exploration.suppress_threat(ctx.get("threat_id", &""))
        threat_result = &"suppressed"

    # 步骤 8: 计算击退
    var knockback: Dictionary = calc_knockback(response_choice, params, ctx)

    # 步骤 9: 构建 combat_result
    var result := {
        "outcome": _outcome_for_response(response_choice),
        "hull_damage": hull_damage,
        "module_damage": module_damage if module_damage.get("module_damaged", false) else null,
        "resources_consumed": resources_consumed if not resources_consumed.is_empty() else null,
        "knockback": knockback,
        "retreat_flagged": response_choice == &"retreat",
    }

    _current_retreat_flagged = _current_retreat_flagged or result["retreat_flagged"]
    result["retreat_flagged"] = _current_retreat_flagged

    return result


func _outcome_for_response(response_choice: StringName) -> String:
    match response_choice:
        &"emergency_handling":
            return "suppressed"
        &"tank":
            return "tanked"
        &"retreat":
            return "retreated"
    return "unknown"
```

### Response Availability

```text
func get_available_responses() -> Array[Dictionary]:
    var responses: Array[Dictionary] = []

    # 应急处理
    var emergency_available: bool = check_emergency_available()
    responses.append({
        "id": &"emergency_handling",
        "label": "应急处理",
        "available": emergency_available,
        "disabled_reason": "需要 repair_kit ×1（随身物品栏中无可用）" if not emergency_available else "",
        "shortcut": KEY_1,
    })

    # 硬扛
    var hull: int = ModuleHullManager.get_hull_integrity()
    var tank_warning: String = ""
    if hull <= 12:
        tank_warning = "⚠ 船体严重受损"
    responses.append({
        "id": &"tank",
        "label": "硬扛",
        "available": true,
        "warning": tank_warning,
        "damage_preview": "8–12 船体伤害",
        "cross_band_warning": hull <= 33 and hull > 25,
        "shortcut": KEY_2,
    })

    # 撤退
    responses.append({
        "id": &"retreat",
        "label": "撤退",
        "available": true,
        "shortcut": KEY_3,
    })

    return responses


func check_emergency_available() -> bool:
    var carried: Dictionary = ResourcesManager.get_carried_contents_by_tag("repair-material")
    return carried.get("repair_kit", 0) >= 1
```

---

## Out of Scope

- calc_hull_damage(), calc_module_damage(), calc_knockback() 的具体实现——属于 Story 003
- #5.consume_in_combat() 的具体实现——属于 resources-goods-capacity Epic
- #8.apply_hull_damage() 和 apply_module_damage() 的具体实现——属于 modules-hull-state Epic
- 决策面板 UI 渲染——属于 #16 UIManager

---

## QA Test Cases

- **AC-1/2**: Emergency handling
  - repair_kit≥1 → suppressed, 0 damage; repair_kit=0 → ERR_UNAVAILABLE

- **AC-3/4/5**: Tank
  - damage 8-12 uniform; module risk 30%; hull≤12→warning; hull≤33→cross-band preview

- **AC-6/7**: Retreat
  - 0 damage; retreat_flagged=true; second retreat→still true

- **AC-8/9**: C4 order
  - Steps 1-10 enforced; step 1 fail→ERR_UNAVAILABLE, no state change

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/combat/ResponseResolutionTest.csproj` — must exist and pass
**Status**: [x] Passing — `dotnet run --project tests/unit/combat/ResponseResolutionTest.csproj -p:UseSharedCompilation=false` (7/7 PASS, 2026-05-14)

## Completion Evidence — 2026-05-14

- Implemented in `src/core/combat/CombatManager.cs`.
- Test runner: `tests/unit/combat/ResponseResolutionTest.csproj`.
- Acceptance coverage:
  - AC-1/2/9: emergency handling consumes carried repair kit, suppresses threat, and hard-fails without mutation when unavailable.
  - AC-3: tank response produces hull damage, optional module damage, knockback, and active threat outcome.
  - AC-4/5: low-hull and cross-band warnings are exposed without blocking tank.
  - AC-6/7: retreat result has no damage, marks retreat once, and remains boolean.
  - AC-8: settlement side effects follow C4 order across #5/#8/#11 injected boundaries.
  - AC-10/11: decision breath has no timer pressure and state remains AWAITING_RESPONSE while UI inspection queries run.

---

## Dependencies

- Depends on: Story 001 (state machine, AWAITING_RESPONSE state), Story 003 (calc formulas), resources-goods-capacity Epic (consume_in_combat, get_carried_contents_by_tag), modules-hull-state Epic (apply_hull_damage, apply_module_damage, get_hull_integrity)
- Unlocks: Story 004 (combat_result contract), Story 006 (EC-12-01/02/05/07 edge cases)
