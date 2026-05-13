# Story 002: Deposit Validation & Batch Commit

> **Epic**: World Repair & Unlock
> **Status**: Done
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/world-repair-unlock.md`
**Requirement**: `TR-repair-002`

**ADR Governing Implementation**: ADR-0011 (§4c validate_deposit + submit_deposit, §5b deposit_validation 算法)
**ADR Decision Summary**: 修复材料提交通过两个方法完成——validate_deposit（5 种 violation 检测）和 submit_deposit（原子提交链：验证→commit_deposit→更新 deposited 计数器→进度重算→完成判定→信号发射）。分批提交允许同一节点的不同材料跨多次访问提交，deposited 计数器记录累计已提交量。每次提交至少包含一种需求材料的部分或全部数量。数量选择器默认填充 min(carried, required - deposited) 防止误提交多余材料。已满足需求的材料不可再提交（垃圾邮件守卫）。deposit_validation 检查 5 种 violation：invalid_node、empty_offer、invalid_material、excess_quantity、already_repaired。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: submit_deposit 必须先调用 validate_deposit——验证失败则返回 violation 不消耗材料；deposited 计数器仅在 commit_deposit 成功后更新；数量选择器默认 min(carried, required-deposited) 防止误提交
- Forbidden: 跳过验证直接调用 commit_deposit；对已满足需求量的材料接受提交；提交零数量或负数材料
- Guardrail: commit_deposit 失败时 deposited 计数器不回退（#5 保证原子性——失败则全部回滚）

---

## Acceptance Criteria

### validate_deposit — 5 Violation Types

- [ ] **AC-1**: GIVEN node_id 不在 Registry 中，WHEN validate_deposit("invalid_node", {repair_kit: 1})，THEN → {valid: false, violations: ["invalid_node"]}
- [ ] **AC-2**: GIVEN offer 为空 Dictionary 或所有 quantity <= 0，WHEN validate_deposit(node_id, {}) 或 validate_deposit(node_id, {repair_kit: 0})，THEN → {valid: false, violations: ["empty_offer"]}
- [ ] **AC-3**: GIVEN offer 包含不在 required_resources 中的 resource_id，WHEN validate_deposit(node_id, {invalid_material: 1})，THEN → {valid: false, violations: ["invalid_material"]}
- [ ] **AC-4**: GIVEN offer[repair_kit] = 5 但 required[repair_kit] = 4 且 deposited[repair_kit] = 0，WHEN validate_deposit(node_id, {repair_kit: 5})，THEN → {valid: false, violations: ["excess_quantity"]}。5 > 4-0
- [ ] **AC-5**: GIVEN repair_state=REPAIRED，WHEN validate_deposit(node_id, {repair_kit: 1})，THEN → {valid: false, violations: ["already_repaired"]}
- [ ] **AC-6**: GIVEN 多种 violation 同时存在（如 invalid_material + excess_quantity），WHEN validate_deposit，THEN 返回所有 violation 数组。不短路——全部检查

### Batch Commit (Partial Deposit)

- [ ] **AC-7**: GIVEN required_resources={repair_kit: 4, basic_supply: 4} + deposited={} + 玩家提交 {repair_kit: 3}，WHEN submit_deposit，THEN commit_deposit 被调用 + deposited[repair_kit]→3 + repair_progress 更新 + repair_completion=false。节点保持 KNOWN
- [ ] **AC-8**: GIVEN deposited={repair_kit: 3} + 玩家再次到达提交 {repair_kit: 1, basic_supply: 4}，WHEN submit_deposit，THEN deposited[repair_kit]→4 + deposited[basic_supply]→4 + repair_completion=true → 状态→REPAIRED
- [ ] **AC-9**: GIVEN deposited={repair_kit: 3, basic_supply: 4} + 玩家尝试提交 {repair_kit: 2}，WHEN validate_deposit，THEN excess_quantity——required[repair_kit]-deposited[repair_kit]=1，2 > 1

### Single-Shot Full Commit

- [ ] **AC-10**: GIVEN 玩家携带 repair_kit≥4 + basic_supply≥4 + deposited={}，WHEN 单次提交 {repair_kit: 4, basic_supply: 4}，THEN deposited 一次性满足所有需求 + repair_completion=true + 状态→REPAIRED。单次提交完成修复

### submit_deposit Atomic Chain

- [ ] **AC-11**: GIVEN validate_deposit 返回 valid=false，WHEN submit_deposit，THEN 返回 {result: ERR_VALIDATION_FAILED, violations: [...]}。不调用 commit_deposit，不更新 deposited，不发射信号
- [ ] **AC-12**: GIVEN commit_deposit（#5 原子操作）失败，WHEN submit_deposit，THEN deposited 计数器不变，返回 {result: ERR_COMMIT_FAILED}。不发射 repair_progress_changed

### Edge Guards

- [ ] **AC-13**: GIVEN 同种材料已满足需求（deposited[repair_kit]=4, required[repair_kit]=4），WHEN 再次提交 repair_kit，THEN validate_deposit 返回 excess_quantity——缺口为 0，任何正数都过量
- [ ] **AC-14**: GIVEN offer 中包含多种材料，其中一种 excess_quantity 另一种 valid，WHEN validate_deposit，THEN 整体 valid=false。不部分接受

---

## Implementation Notes

### VIOLATION Constants

```text
# WorldRepair Autoload #13 — Violation 类型常量
const VIOLATION_INVALID_NODE: StringName = &"invalid_node"
const VIOLATION_EMPTY_OFFER: StringName = &"empty_offer"
const VIOLATION_INVALID_MATERIAL: StringName = &"invalid_material"
const VIOLATION_EXCESS_QUANTITY: StringName = &"excess_quantity"
const VIOLATION_ALREADY_REPAIRED: StringName = &"already_repaired"
```

### validate_deposit

```text
func validate_deposit(node_id: StringName, offer: Dictionary) -> Dictionary:
    var violations: Array[StringName] = []

    # 1. 节点存在性
    if not repair_nodes.has(node_id):
        violations.append(VIOLATION_INVALID_NODE)
        return {"valid": false, "violations": violations}

    # 2. 已修复守卫
    if get_repair_state(node_id) == REPAIR_STATE_REPAIRED:
        violations.append(VIOLATION_ALREADY_REPAIRED)
        return {"valid": false, "violations": violations}

    # 3. 空提交检查
    var has_positive: bool = false
    for qty in offer.values():
        if qty > 0:
            has_positive = true
            break
    if not has_positive:
        violations.append(VIOLATION_EMPTY_OFFER)
        return {"valid": false, "violations": violations}

    # 4. 材料类型 + 数量验证
    var required: Dictionary = _get_required_resources(node_id)
    var deposited: Dictionary = repair_nodes[node_id].get("deposited", {})

    for rid in offer:
        var qty: int = offer[rid]
        if not required.has(rid):
            violations.append(VIOLATION_INVALID_MATERIAL)
        else:
            var needed: int = required[rid] - deposited.get(rid, 0)
            if qty > needed:
                violations.append(VIOLATION_EXCESS_QUANTITY)

    return {"valid": violations.is_empty(), "violations": violations}
```

### submit_deposit

```text
func submit_deposit(node_id: StringName, offer: Dictionary) -> Dictionary:
    # 步骤 1: 验证
    var validation: Dictionary = validate_deposit(node_id, offer)
    if not validation["valid"]:
        return {
            "result": ERR_VALIDATION_FAILED,
            "violations": validation["violations"],
            "deposited": get_deposited(node_id),
            "progress": get_repair_progress(node_id),
            "completed": false,
        }

    # 步骤 2: 原子提交至 #5 (Pool 6 终态)
    var commit_result: Dictionary = ResourcesManager.commit_deposit(node_id, offer)
    if commit_result.get("result", -1) != OK:
        return {
            "result": ERR_COMMIT_FAILED,
            "violations": [],
            "deposited": get_deposited(node_id),
            "progress": get_repair_progress(node_id),
            "completed": false,
        }

    # 步骤 3: 更新 deposited 计数器
    for rid in offer:
        var qty: int = offer[rid]
        var current: int = repair_nodes[node_id]["deposited"].get(rid, 0)
        repair_nodes[node_id]["deposited"][rid] = current + qty

    # 步骤 4: 重新计算进度
    var progress: float = _compute_repair_progress(node_id)
    repair_nodes[node_id]["repair_progress"] = progress

    # 步骤 5: 发射进度变更信号
    repair_progress_changed.emit(node_id, progress, get_deposited(node_id))

    # 步骤 6: 检查完成
    var completed: bool = _check_repair_completion(node_id)
    if completed:
        _transition_state(node_id, REPAIR_STATE_REPAIRED)
        repair_completed.emit(node_id)
        visual_state_changed.emit(node_id, &"repaired")

    return {
        "result": OK,
        "violations": [],
        "deposited": get_deposited(node_id),
        "progress": progress,
        "completed": completed,
    }
```

### _get_required_resources Helper

```text
func _get_required_resources(node_id: StringName) -> Dictionary:
    var node_def: Dictionary = Registry.query_entity(node_id)
    if node_def.is_empty():
        return {}

    var required: Dictionary = {}
    for entry in node_def.get("required_resources", []):
        for rid in entry:
            required[rid] = entry[rid]
    return required
```

---

## Out of Scope

- ResourcesManager.commit_deposit() 的具体实现——属于 resources-goods-capacity Epic
- Registry.query_entity() 的具体实现——属于 content-registry Epic
- repair_progress 计算和 repair_completion 判定——属于 Story 003
- 提交 UI 面板（数量选择器、确认弹窗）——属于 #16 UIManager

---

## QA Test Cases

- **AC-1 through AC-6**: validate_deposit — all 5 violations + mixed violations
- **AC-7/8**: Batch commit — 2-visit flow depositing repair_kit×3 then repair_kit×1 + basic_supply×4
- **AC-9**: Excess quantity on partially-filled node
- **AC-10**: Single-shot — full commit in one visit
- **AC-11/12**: Validation fail / commit fail → no state change
- **AC-13**: Already-satisfied material → excess_quantity
- **AC-14**: Mixed valid/excess → overall invalid

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/world-repair/DepositValidationTest.csproj` — must exist and pass
**Status**: [x] Done — 2026-05-13 — `dotnet run --project tests/unit/world-repair/DepositValidationTest.csproj --no-restore` PASS 14/14; `dotnet build CloudWeaverVoyage.sln --no-restore` PASS; `git diff --check` PASS

---

## Dependencies

- Depends on: Story 001 (state machine, repair_nodes storage), resources-goods-capacity Epic (commit_deposit, deposit_committed signal), content-registry Epic (query_entity 返回 required_resources)
- Unlocks: Story 003 (formulas feed into submit_deposit result), Story 005 (persisted deposited counters recovered on load)
