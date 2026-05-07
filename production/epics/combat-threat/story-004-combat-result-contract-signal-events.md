# Story 004: combat_result Contract & Signal Events

> **Epic**: Combat / Threat Resolution
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/combat-threat-handling.md`
**Requirement**: `TR-combat-003`

**ADR Governing Implementation**: ADR-0018 (combat_result contract, signal events — threat_resolved/threat_suppressed/threat_tanked/threat_retreated, downstream cascades to #5/#8/#11)
**ADR Decision Summary**: combat_result 包含 6 个字段：outcome("suppressed"/"tanked"/"retreated"), hull_damage(int), module_damage({slot_id, damage_type}|null), resources_consumed([{resource_id, quantity}]|null), knockback({direction, distance}|null), retreat_flagged(bool)。CombatManager 声明 4 个信号：threat_resolved（所有结算完成）、threat_suppressed（应急处理成功）、threat_tanked（硬扛完成）、threat_retreated（撤退完成）。信号遵循 ADR-0002：typed params, sync emit, emit-after-mutation, max cascade depth 2。下游级联：hull_damage>0→#8.apply_hull_damage；module_damage!=null→#8.apply_module_damage；resources_consumed!=null→#5.consume_in_combat；retreat_flagged→#11 使用 λ_forced=0.25；knockback→#11 执行 move_and_collide。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: combat_result 所有 6 字段非 null（module_damage/resources_consumed/knockback 可为 null）；信号在状态变更完成后 emit（emit-after-mutation）；信号 cascade depth ≤ 1（threat_resolved→#17→terminate）
- Forbidden: 在 PROCESSING（结算中途）发射信号；修改 combat_result 后继续引用（深拷贝给 #11）；retreat_flagged 在会话中清除
- Guardrail: combat_result 大小 < 500 bytes；信号 emit 开销 < 0.01ms

---

## Acceptance Criteria

### combat_result Contract

- [ ] **AC-1**: GIVEN outcome="suppressed"，WHEN combat_result，THEN: outcome="suppressed", hull_damage=0, module_damage=null, resources_consumed=[{resource_id:"repair_kit", quantity:1}], knockback=null, retreat_flagged=false
- [ ] **AC-2**: GIVEN outcome="tanked" + hull_damage=10 + module_damage hit slot_a，WHEN combat_result，THEN: outcome="tanked", hull_damage=10, module_damage={slot_id:"slot_a", damage_type:"guard_impact"}, resources_consumed=null, knockback={direction:Vector2, distance:8.0}, retreat_flagged=false
- [ ] **AC-3**: GIVEN outcome="retreated"，WHEN combat_result，THEN: outcome="retreated", hull_damage=0, module_damage=null, resources_consumed=null, knockback={direction:Vector2, distance:10.0}, retreat_flagged=true

### Signal Events

- [ ] **AC-4**: GIVEN 结算完成 + outcome="suppressed"，WHEN emit，THEN:
  - threat_resolved.emit("suppressed", threat_id)
  - threat_suppressed.emit(threat_id)
  - threat_tanked 和 threat_retreated 不发射
- [ ] **AC-5**: GIVEN 结算完成 + outcome="tanked"，WHEN emit，THEN:
  - threat_resolved.emit("tanked", threat_id)
  - threat_tanked.emit(threat_id, hull_damage)
- [ ] **AC-6**: GIVEN 结算完成 + outcome="retreated"，WHEN emit，THEN:
  - threat_resolved.emit("retreated", threat_id)
  - threat_retreated.emit(threat_id)

### Signal Timing (emit-after-mutation)

- [ ] **AC-7**: GIVEN 结算序列步骤 1-9 全部完成 + combat_result 已构建 + _state=RESOLVED，WHEN 发射信号，THEN 信号在状态变更完成后 emit——不在 PROCESSING 期间 emit
- [ ] **AC-8**: GIVEN 信号发射，WHEN 检查 cascade depth，THEN threat_resolved → #17 消费 → 终结（depth=1）。不超过 depth=2

### Downstream Cascades to #8

- [ ] **AC-9**: GIVEN outcome="tanked" + hull_damage=10，WHEN C4 步骤 5，THEN ModuleHullManager.apply_hull_damage(10) 被调用。integrity 扣减 10
- [ ] **AC-10**: GIVEN outcome="tanked" + module_damage hit slot_a，WHEN C4 步骤 6，THEN ModuleHullManager.apply_module_damage("slot_a", "guard_impact") 被调用。η_scout: 1.0→0.6 或 η_cargo: 0.5

### Downstream Cascades to #5

- [ ] **AC-11**: GIVEN outcome="suppressed" + resources_consumed=[{repair_kit, 1}]，WHEN C4 步骤 2，THEN ResourcesManager.consume_in_combat("repair_kit", 1) 被调用。repair_kit 从 Pool 5 永久移除

### Downstream Cascades to #11

- [ ] **AC-12**: GIVEN outcome="retreated" + retreat_flagged=true，WHEN #11 消费 combat_result，THEN Exploration 记录 retreat_flagged。若后续撤离 → extraction_loss_settlement 使用 λ_forced=0.25
- [ ] **AC-13**: GIVEN knockback={direction, distance} 非 null，WHEN #11 消费 combat_result，THEN Exploration 执行 move_and_collide(player, direction * distance)。击退不穿越碰撞体

### Retreat Flagged Persistence

- [ ] **AC-14**: GIVEN retreat_flagged=true（来自前次撤退），WHEN 后续威胁结算（任何结果），THEN combat_result.retreat_flagged 仍为 true。retreat_flagged 不可在当前会话中清除
- [ ] **AC-15**: GIVEN retreat_flagged=true + 后续选择应急处理清除另一威胁，WHEN combat_result，THEN retreat_flagged=true。应急处理不取消撤退标记

---

## Implementation Notes

### combat_result Signal Declaration

```gdscript
# CombatManager Autoload #12 — signal declarations
signal threat_resolved(outcome: String, threat_id: StringName)
signal threat_suppressed(threat_id: StringName)
signal threat_tanked(threat_id: StringName, hull_damage: int)
signal threat_retreated(threat_id: StringName)

# 消费方:
#   #17 (Feedback) — threat_resolved → 播放结算音效/视觉
#   #16 (UI) — threat_resolved → 更新 HUD, 移除威胁指示器
#   #11 (Exploration) — combat_result_ready(combat_result) → 恢复探索状态, 执行击退
```

### Signal Emission Logic

```gdscript
func _emit_resolution_signals(result: Dictionary) -> void:
    var outcome: String = result.get("outcome", "")
    var threat_id: StringName = _current_threat_context.get("threat_id", &"")

    match outcome:
        "suppressed":
            threat_suppressed.emit(threat_id)
        "tanked":
            threat_tanked.emit(threat_id, result.get("hull_damage", 0))
        "retreated":
            threat_retreated.emit(threat_id)

    # threat_resolved 在所有具体信号之后 emit
    threat_resolved.emit(outcome, threat_id)
```

### combat_result Structure Validation

```gdscript
func _validate_combat_result(result: Dictionary) -> bool:
    var required_fields: Array[StringName] = [
        &"outcome", &"hull_damage", &"module_damage",
        &"resources_consumed", &"knockback", &"retreat_flagged",
    ]
    for field in required_fields:
        if not result.has(field):
            push_error("Combat: combat_result missing required field: %s" % field)
            return false

    var valid_outcomes: Array = ["suppressed", "tanked", "retreated"]
    if result["outcome"] not in valid_outcomes:
        push_error("Combat: invalid outcome: %s" % result["outcome"])
        return false

    if not result["retreat_flagged"] is bool:
        push_error("Combat: retreat_flagged must be bool")
        return false

    return true
```

### Backward Compatibility — #12 Unavailable

```gdscript
# 当 #12 未实现或不可用时，#11 的防御性处理
# （此函数属于 #11 Exploration，此处仅作为合约参考）
func _resolve_threat_safe(threat_context: Dictionary) -> Dictionary:
    if not Combat or not Combat.has_method("resolve_threat"):
        # EC-12-10: 威胁点保持 inert，不影响探索
        return {"outcome": "unavailable", "hull_damage": 0,
                "module_damage": null, "resources_consumed": null,
                "knockback": null, "retreat_flagged": false}
    return Combat.resolve_threat(threat_context)
```

---

## Out of Scope

- combat_result 消费端的 UI 更新——属于 #16 UIManager
- combat_result 消费端的音效播放——属于 #17 Feedback
- Exploration 的 move_and_collide 执行——属于 #11 Exploration
- λ_forced=0.25 的撤离损耗计算——属于 #11 Exploration 的 extraction_loss_settlement

---

## QA Test Cases

- **AC-1/2/3**: combat_result per outcome
  - suppressed: damage=0, module=null, resources consumed, knockback=null
  - tanked: damage 8-12, module hit possible, knockback 8.0
  - retreated: damage=0, module=null, knockback 10.0, retreat_flagged=true

- **AC-4/5/6**: Signal events per outcome
  - suppressed → threat_resolved + threat_suppressed
  - tanked → threat_resolved + threat_tanked
  - retreated → threat_resolved + threat_retreated

- **AC-14/15**: retreat_flagged persistence
  - Second retreat → still true; emergency after retreat → still true

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/combat/combat_result_signal_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (settlement sequence, combat_result construction), Story 003 (formula results feed into combat_result), modules-hull-state Epic, resources-goods-capacity Epic, exploration-scavenge Epic
- Unlocks: Story 006 (EC-12-05/08/10 edge cases)
