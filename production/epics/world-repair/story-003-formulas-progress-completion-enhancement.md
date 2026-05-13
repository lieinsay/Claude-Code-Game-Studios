# Story 003: Repair Progress, Completion & Route Enhancement Formulas

> **Epic**: World Repair & Unlock
> **Status**: Done
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/world-repair-unlock.md`
**Requirement**: `TR-repair-002`, `TR-repair-003`

**ADR Governing Implementation**: ADR-0011 (§5c repair_progress 计算, §5d repair_completion 判定, §5e route_enhancement 输出)
**ADR Decision Summary**: 三个纯函数公式——repair_progress 计算已提交材料占需求的比例（各项 min(deposited/required, 1.0) 取平均，0.0-1.0），repair_completion 判定全部需求是否满足（任一不足→false），route_enhancement 输出修复后关联航线的增强效果列表（hazard_reduction 比例 + route unlock）。repair_progress 每项独立计算后取平均——不是总数量/总需求。route_enhancement 中 hazard 降低做 max(0, hazard - reduction) 底限保护。全部公式从 Registry 读取节点定义——不硬编码材料清单或航线。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: repair_progress 按需求项独立计算后取平均——不是总量平均；repair_completion 全部需求满足才返回 true；route_enhancement hazard 做 max(0, hazard-reduction) 底限
- Forbidden: 硬编码 starlight_dock 材料清单或航线 ID；repair_progress 返回负值或 >1.0
- Guardrail: |required_resources|=0 时 repair_progress=0.0, repair_completion=false

---

## Acceptance Criteria

### Formula: repair_progress

- [ ] **AC-1**: GIVEN required={repair_kit: 4, basic_supply: 4} + deposited={repair_kit: 2, basic_supply: 1}，WHEN _compute_repair_progress，THEN progress = (min(2/4, 1.0) + min(1/4, 1.0)) / 2 = (0.5 + 0.25) / 2 = 0.375
- [ ] **AC-2**: GIVEN required={repair_kit: 4, basic_supply: 4} + deposited={repair_kit: 4, basic_supply: 4}，WHEN 计算，THEN progress = (1.0 + 1.0) / 2 = 1.0
- [ ] **AC-3**: GIVEN required={repair_kit: 4} + deposited={repair_kit: 5}（超额但不应发生——excess_quantity 守卫在 Story 002 中阻止），WHEN 防御性计算，THEN min(5/4, 1.0) = 1.0。progress=1.0（不因超额溢出）
- [ ] **AC-4**: GIVEN required={}（空需求——不应存在的配置），WHEN 计算，THEN return 0.0。不除零崩溃
- [ ] **AC-5**: GIVEN required={repair_kit: 0, basic_supply: 4}（repair_kit 需求为 0），WHEN 计算，THEN repair_kit 项视为已满足（contrib=1.0），basic_supply 按比例。不除零

### Formula: repair_completion

- [ ] **AC-6**: GIVEN required={repair_kit: 4, basic_supply: 4} + deposited={repair_kit: 4, basic_supply: 3}，WHEN _check_repair_completion，THEN false——basic_supply 不足
- [ ] **AC-7**: GIVEN required={repair_kit: 4, basic_supply: 4} + deposited={repair_kit: 4, basic_supply: 4}，WHEN 判定，THEN true
- [ ] **AC-8**: GIVEN deposited={repair_kit: 5, basic_supply: 4}（超额但 deposited 包含多出量），WHEN 判定，THEN deposited>=required 全部满足 → true
- [ ] **AC-9**: GIVEN |required|=0，WHEN 判定，THEN false。空需求不算完成

### Formula: route_enhancement

- [ ] **AC-10**: GIVEN 修复节点 starlight_dock + Registry 定义 unlocked_routes=[route.sky-reef-arc-01] + route_enhancement={effect: hazard_reduction, magnitude: 0.3}，WHEN repair_completion 触发后查询 route_enhancement，THEN 返回 [{route_id: "route.sky-reef-arc-01", effect_type: "hazard_reduction", magnitude: 0.3, unlock: true}]
- [ ] **AC-11**: GIVEN 航线当前 hazard=0.2 + hazard_reduction=0.3，WHEN 增强应用，THEN max(0, 0.2 - 0.2×0.3) = max(0, 0.14) = 0.14。不对负值做底限——仅为正数验证。若 hazard 极低 → max(0, hazard - reduction_absolute) 保证 ≥0
- [ ] **AC-12**: GIVEN Registry 中节点定义含 pre_repair_route_state.traversable=false，WHEN repair_completion 后，THEN route_enhancement 输出 unlock: true。航线从不可通行→可通行

### Integration with submit_deposit

- [ ] **AC-13**: GIVEN submit_deposit 完成后 repair_completion=true，WHEN 检查，THEN repair_progress 恰好 1.0。不浮点精度误差导致 progress=0.9999 但 completion=true
- [ ] **AC-14**: GIVEN 分批提交：第一次 repair_kit×3（progress=0.375），第二次 repair_kit×1 + basic_supply×4（progress=1.0, completed=true），WHEN 每次提交后查询 get_repair_progress，THEN 返回正确中间值和终值

---

## Implementation Notes

### repair_progress Formula

```text
func _compute_repair_progress(node_id: StringName) -> float:
    var required: Dictionary = _get_required_resources(node_id)
    if required.is_empty():
        return 0.0

    var deposited: Dictionary = repair_nodes[node_id].get("deposited", {})
    var total_satisfaction: float = 0.0
    var entry_count: int = 0

    for rid in required:
        var required_qty: int = required[rid]
        entry_count += 1

        if required_qty <= 0:
            # 零需求项——视为已满足
            total_satisfaction += 1.0
            continue

        var deposited_qty: int = deposited.get(rid, 0)
        var satisfaction: float = minf(float(deposited_qty) / float(required_qty), 1.0)
        total_satisfaction += satisfaction

    if entry_count == 0:
        return 0.0

    var progress: float = total_satisfaction / float(entry_count)
    return clampf(progress, 0.0, 1.0)
```

### repair_completion Formula

```text
func _check_repair_completion(node_id: StringName) -> bool:
    var required: Dictionary = _get_required_resources(node_id)
    if required.is_empty():
        return false

    var deposited: Dictionary = repair_nodes[node_id].get("deposited", {})

    for rid in required:
        var required_qty: int = required[rid]
        if required_qty <= 0:
            continue  # 零需求跳过
        var deposited_qty: int = deposited.get(rid, 0)
        if deposited_qty < required_qty:
            return false

    return true
```

### route_enhancement Query

```text
func get_route_enhancements(node_id: StringName) -> Array[Dictionary]:
    # 仅在 repair_completion 后调用才有意义
    # 返回修复节点关联的航线增强效果列表
    var node_def: Dictionary = Registry.query_entity(node_id)
    if node_def.is_empty():
        return []

    var unlocked_routes: Array = node_def.get("unlocked_routes", [])
    var enhancement: Dictionary = node_def.get("route_enhancement", {})
    var pre_state: Dictionary = node_def.get("pre_repair_route_state", {})

    var results: Array[Dictionary] = []
    for route_id in unlocked_routes:
        var entry: Dictionary = {
            "route_id": route_id,
            "effect_type": enhancement.get("effect", &""),
            "magnitude": enhancement.get("magnitude", 0.0),
            "unlock": pre_state.get("traversable", true) == false,
        }
        results.append(entry)

    return results
```

### Hazard Floor Guard (consumed by #9)

```text
# 此防御属于 #9 Chart ——此处作为合约参考
# 当 #9 应用 route_enhancement 时：
func apply_hazard_reduction(current_hazard: float, reduction_magnitude: float) -> float:
    var reduction_absolute: float = current_hazard * reduction_magnitude
    return maxf(current_hazard - reduction_absolute, 0.0)
```

---

## Out of Scope

- route_enhancement 在 #9 Chart 中的实际应用——属于 chart-route-planning Epic
- hazard 值从 Registry 或 EncounterContext 的读取——属于 navigation-route-risk Epic
- repair_progress 在 UI 进度条中的渲染——属于 #16 UIManager

---

## QA Test Cases

- **AC-1 through AC-5**: repair_progress — normal, complete, over-deposited defense, empty required, zero-qty entry
- **AC-6 through AC-9**: repair_completion — partial, complete, over-deposited, empty required
- **AC-10 through AC-12**: route_enhancement — unlock + hazard reduction, hazard floor, pre_repair traversable check
- **AC-13**: Progress=1.0 ↔ completion=true consistency
- **AC-14**: Batch commit intermediate progress values

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/world-repair/FormulasTest.csproj` — must exist and pass
**Status**: [x] Done — 2026-05-13 — `dotnet run --project tests/unit/world-repair/FormulasTest.csproj --no-restore` PASS 14/14; `dotnet build CloudWeaverVoyage.sln --no-restore` PASS; `git diff --check` PASS

---

## Dependencies

- Depends on: Story 001 (repair_nodes storage, Registry integration), Story 002 (deposited counter structure), content-registry Epic (query_entity 返回 required_resources/unlocked_routes/route_enhancement)
- Unlocks: Story 004 (route_enhancement 输出被 downstream trigger chain 消费), Story 006 (progress edge cases)
