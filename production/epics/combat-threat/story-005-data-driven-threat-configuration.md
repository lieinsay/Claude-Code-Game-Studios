# Story 005: Data-Driven Threat Configuration

> **Epic**: Combat / Threat Resolution
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/combat-threat-handling.md`
**Requirement**: `TR-combat-001`

**ADR Governing Implementation**: ADR-0018 (§8 data-driven threat configuration, encounter_params path: Registry→EncounterContext→#11→threat_context.encounter_params)
**ADR Decision Summary**: 所有威胁数值来自 Registry (#1)，通过 EncounterContext (#10) → Exploration (#11) → threat_context.encounter_params 路径传入 CombatManager。CombatManager 内部不硬编码任何数值。威胁配置表 C8 定义了 8 个字段：threat_category, full_damage_range [min, max], module_damage_chance, trigger_radius, emergency_cost_repair_kit, knockback_distance_tanked, knockback_distance_retreat, can_be_suppressed。约束检查由 #11 在加载时执行——确保 knockback_distance_tanked > trigger_radius_max。MVP 仅定义 guard 类型。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 所有威胁数值从 encounter_params 读取——不得硬编码在 CombatManager 中；配置加载时验证 knockback_distance_tanked > trigger_radius_max；缺失的 encounter_params 字段使用安全默认值
- Forbidden: 在 CombatManager 中硬编码 guard 伤害值（8, 12）或概率值（0.30）；跳过配置验证直接使用传入参数
- Guardrail: 缺失字段默认值保守——damage→8-12, module_chance→0.30, knockback→8.0/10.0

---

## Acceptance Criteria

### Threat Configuration Path

- [ ] **AC-1**: GIVEN Registry 中定义了 guard 威胁配置（C8 表），WHEN Exploration 构建 threat_context，THEN encounter_params 包含全部 8 个配置字段。通过 EncounterContext → #11 → threat_context 路径正确传递
- [ ] **AC-2**: GIVEN CombatManager 收到 threat_context.encounter_params，WHEN 结算，THEN 所有伤害/概率/距离值从 encounter_params 读取——不硬编码

### Configuration Validation

- [ ] **AC-3**: GIVEN 威胁配置加载，WHEN 验证，THEN knockback_distance_tanked (8.0) > trigger_radius_max (6.0)。违反 → 记录错误 + clamp knockback_distance_tanked = trigger_radius_max + 2.0
- [ ] **AC-4**: GIVEN encounter_params 中 can_be_suppressed=false，WHEN UI 查询可用响应，THEN 应急处理选项不可用——即使 Pool 5 有 repair_kit
- [ ] **AC-5**: GIVEN encounter_params 中 full_damage_max < full_damage_min（配置错误），WHEN 验证，THEN swap 确保 min ≤ max。记录警告

### Default Values for Missing Fields

- [ ] **AC-6**: GIVEN encounter_params 缺失 full_damage_min，WHEN 读取，THEN 默认值 8。缺失 full_damage_max → 默认值 12
- [ ] **AC-7**: GIVEN encounter_params 缺失 module_damage_chance，WHEN 读取，THEN 默认值 0.30
- [ ] **AC-8**: GIVEN encounter_params 缺失 knockback_distance_tanked/retreat，WHEN 读取，THEN 默认值 8.0 / 10.0
- [ ] **AC-9**: GIVEN encounter_params 缺失 emergency_cost_repair_kit，WHEN 读取，THEN 默认值 1

### Threat Type Extension Readiness

- [ ] **AC-10**: GIVEN 未来添加新威胁类型（如 "patrol"）并在 Registry 注册配置，WHEN Exploration 传递对应的 encounter_params，THEN CombatManager 结算代码无需修改——从 encounter_params 读取所有数值
- [ ] **AC-11**: GIVEN encounter_params 包含未识别的键（未来扩展字段），WHEN 验证，THEN 忽略未知键——不报错。前向兼容

### Configuration Retrieval Interface

- [ ] **AC-12**: GIVEN #11 需要构建 guard 威胁的 threat_context，WHEN 查询，THEN Registry.get_threat_config("guard") 返回 C8 表完整配置。Exploration 将配置打包进 encounter_params 传入 CombatManager
- [ ] **AC-13**: GIVEN Registry 中不存在请求的 threat_type，WHEN get_threat_config("unknown_type")，THEN 返回 null。Exploration 使用 fallback guard 配置 + 记录警告

---

## Implementation Notes

### Threat Configuration Table (C8)

```text
# 定义在 Registry (#1) 中——通过 EncounterContext → #11 传递
# 以下为 guard 类型的完整配置

const GUARD_THREAT_CONFIG: Dictionary = {
    "threat_category": &"guard",
    "full_damage_min": 8,
    "full_damage_max": 12,
    "module_damage_chance": 0.30,
    "emergency_cost_repair_kit": 1,
    "knockback_distance_tanked": 8.0,
    "knockback_distance_retreat": 10.0,
    "can_be_suppressed": true,
    "trigger_radius_min": 4.0,
    "trigger_radius_max": 6.0,
}
```

### CombatManager Configuration Reader

```text
# CombatManager 内部——从 encounter_params 安全读取配置值
# 所有数值来自 encounter_params，不硬编码

const DEFAULT_THREAT_PARAMS: Dictionary = {
    "full_damage_min": 8,
    "full_damage_max": 12,
    "module_damage_chance": 0.30,
    "emergency_cost_repair_kit": 1,
    "knockback_distance_tanked": 8.0,
    "knockback_distance_retreat": 10.0,
    "can_be_suppressed": true,
}


func _get_param(params: Dictionary, key: StringName, default: Variant) -> Variant:
    return params.get(key, DEFAULT_THREAT_PARAMS.get(key, default))


func _validate_threat_params(params: Dictionary) -> Dictionary:
    var validated: Dictionary = params.duplicate()

    # 填充缺失字段
    for key in DEFAULT_THREAT_PARAMS:
        if not validated.has(key):
            validated[key] = DEFAULT_THREAT_PARAMS[key]

    # 确保 min ≤ max
    if validated["full_damage_min"] > validated["full_damage_max"]:
        push_warning("Combat: full_damage_min (%d) > full_damage_max (%d) — swapping" %
            [validated["full_damage_min"], validated["full_damage_max"]])
        var tmp: int = validated["full_damage_min"]
        validated["full_damage_min"] = validated["full_damage_max"]
        validated["full_damage_max"] = tmp

    # 验证击退距离 > 触发半径
    var trigger_max: float = validated.get("trigger_radius_max", 6.0)
    if validated["knockback_distance_tanked"] <= trigger_max:
        push_error("Combat: knockback_distance_tanked (%.1f) <= trigger_radius_max (%.1f) — clamping" %
            [validated["knockback_distance_tanked"], trigger_max])
        validated["knockback_distance_tanked"] = trigger_max + 2.0

    return validated
```

### Exploration-side Configuration Retrieval

```text
# 此函数属于 #11 Exploration，此处作为合约参考
func _build_threat_context_for_guard(threat_id: StringName, position: Vector2) -> Dictionary:
    var config: Dictionary = Registry.get_threat_config(&"guard")
    if config.is_empty():
        push_warning("Exploration: threat config 'guard' not found in registry — using fallback")
        config = Combat.DEFAULT_THREAT_PARAMS.duplicate()

    return {
        "threat_type": &"guard",
        "threat_id": threat_id,
        "position": position,
        "encounter_params": config,
    }
```

### can_be_suppressed Handling

```text
func get_available_responses() -> Array[Dictionary]:
    var params: Dictionary = _current_threat_context.get("encounter_params", {})
    var can_suppress: bool = _get_param(params, &"can_be_suppressed", true)

    var responses: Array[Dictionary] = []

    # 应急处理：可用性取决于 can_be_suppressed AND Pool 5 中有 repair_kit
    var emergency_available: bool = can_suppress and check_emergency_available()
    responses.append({
        "id": &"emergency_handling",
        "available": emergency_available,
        # ...
    })

    return responses
```

---

## Out of Scope

- Registry.get_threat_config() 的具体实现——属于 content-registry Epic
- EncounterContext 中威胁配置字段的生产——属于 navigation-route-risk Epic Story 006
- 威胁配置的热重载——MVP 使用启动时加载的静态配置
- 新威胁类型（patrol, ambush 等）的规则定义——Phase 2+

---

## QA Test Cases

- **AC-1/2**: Configuration path
  - Registry→EncounterContext→#11→threat_context.encounter_params; all values from params

- **AC-3/4/5**: Validation
  - knockback ≤ trigger→clamp; can_be_suppressed=false→no emergency; min>max→swap

- **AC-6 through AC-9**: Default values
  - Missing fields→safe defaults

- **AC-13**: Unknown threat_type
  - Registry miss→fallback guard config + warning

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/combat/ThreatConfigTest.csproj` — must exist and pass
**Status**: [x] Passing — `dotnet run --project tests/integration/combat/ThreatConfigTest.csproj -p:UseSharedCompilation=false` (5/5 grouped checks PASS, 2026-05-14)

## Completion Evidence — 2026-05-14

- Implemented in `src/core/combat/CombatManager.cs` and `src/core/content/Registry.cs`.
- Test runner: `tests/integration/combat/ThreatConfigTest.csproj`.
- Acceptance coverage:
  - AC-1/12/13: Registry exposes complete guard C8 config via `GetThreatConfig("guard")`; unknown threat type returns null for Exploration fallback.
  - AC-2/10/11: CombatManager reads values from `encounter_params`, supports future threat types by data, and ignores unknown keys.
  - AC-3/5: validation clamps unsafe knockback distance and swaps inverted damage ranges.
  - AC-4: `can_be_suppressed=false` disables emergency handling even when repair kits exist.
  - AC-6 through AC-9: missing fields use safe defaults for damage, module chance, knockback, and emergency cost.

---

## Dependencies

- Depends on: content-registry Epic (get_threat_config), navigation-route-risk Epic (EncounterContext 中的威胁配置字段), exploration-scavenge Epic (threat_context 构建)
- Unlocks: Story 006 (EC-12-10 #12 unavailable)
