# Story 002: Scout Sniff Algorithm & Confidence Clamp

> **Epic**: Partner & Relationships
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/partner-relationships.md`
**Requirement**: `TR-partner-002`

**ADR Governing Implementation**: ADR-0015 (§5c R6 scout_sniff() 6 步算法, §5a F.1 置信度截断, §3 查询接口)
**ADR Decision Summary**: scout_sniff(item_id) 是伙伴系统的唯一物品交互入口。6 步算法：(0) 状态门控——猫不在 SNIFFING 态；(1) 检查 sniffed_items 集合——已嗅辨返回 REACTION_ALREADY_SMELLED；(2) 读取 cat_sniff_signature 静态字段——签名 null 或 reveal_target 空则返回 REACTION_CONFUSED；(3) F.1 confidence_clamp: min(raw, 66)——永不达 67（#6 的权威门槛）；(4) 调用 #6 report_observation_event(pattern_id, "partner_sniff_success")；(5) item_id 加入 sniffed_items + 标记 sniff_success_occurred=true；(6) 选择 R7 反应动画——confidence≥50 → REACTION_CIRCLES_TWICE，<50 → REACTION_RUBS_FACE。物品嗅辨签名 cat_sniff_signature 从 Registry (#1) 只读——本系统不动态生成、不修改。嗅辨永远不消费物品——物品保留在玩家背包中。get_sniffable_items() 过滤玩家背包中具有 cat_sniff_signature 字段的物品。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: F.1 min(raw, 66) 不可跳过——即使 raw=100 也截为 66；scout_sniff 不消费物品——物品保留在背包；cat_sniff_signature 从 Registry 只读——不动态生成
- Forbidden: 对已嗅辨物品调用 reveal_rumor()；对 null/empty reveal_target 调用 reveal_rumor()；scout_sniff 调用期间修改玩家背包
- Guardrail: confidence_final ≤ 66 恒成立——代码审查可验证；cat_sniff_signature 缺失字段 → 默认值兜底（confidence=0, reveal_target=""）

---

## Acceptance Criteria

### State Gate (Step 0)

- [x] **AC-1**: GIVEN cat_state=SNIFFING（前一嗅辨动画进行中），WHEN scout_sniff(another_item) 被调用，THEN 返回 {success: false, reaction_id: -1, error: "cat_busy"}。无数据变更
- [x] **AC-2**: GIVEN cat_state=IDLE_LIVING_QUARTERS 或 BENCH_ADJACENT，WHEN scout_sniff(item)，THEN 状态门控通过——算法进入 Step 1

### Already Sniffed Check (Step 1)

- [x] **AC-3**: GIVEN item_id 已在 sniffed_items 中，WHEN scout_sniff(item_id)，THEN 返回 {success: false, reaction_id: REACTION_ALREADY_SMELLED}。不调用 reveal_rumor()。不修改 sniffed_items
- [x] **AC-4**: GIVEN item_id 不在 sniffed_items 中，WHEN scout_sniff(item_id)，THEN Step 1 通过——进入 Step 2

### Signature Reading (Step 2)

- [x] **AC-5**: GIVEN item.cat_sniff_signature 为 null，WHEN scout_sniff(item_id)，THEN 返回 {success: false, reaction_id: REACTION_CONFUSED}。不调用任何 #6 API。item_id 不加入 sniffed_items
- [x] **AC-6**: GIVEN item.cat_sniff_signature 存在但 reveal_target="" 或 null，WHEN scout_sniff(item_id)，THEN 返回 REACTION_CONFUSED。与 null 签名同处理（E.2.f 防御性检查）

### F.1 Confidence Clamp (Step 3)

- [x] **AC-7**: GIVEN raw_confidence=0, 30, 66, 67, 90, 100（6 参数化用例），WHEN _clamp_confidence(raw)，THEN 分别返回 0, 30, 66, 66, 66, 66。min(raw, 66) 恒成立
- [x] **AC-8**: GIVEN item.cat_sniff_signature 缺失 confidence 字段，WHEN _get_sniff_signature()，THEN confidence 默认 0。不崩溃

### Intel API Calls (Step 4)

- [x] **AC-9**: GIVEN 有效签名 + 未嗅辨物品，WHEN scout_sniff() 成功路径，THEN IntelManager.reveal_rumor(reveal_target, "partner.sky-cat", [hazard_hint], clamped_confidence) 被调用 1 次
- [x] **AC-10**: GIVEN 有效签名 + pattern_id=""，WHEN scout_sniff()，THEN report_observation_event() 不被调用。空 pattern_id 静默跳过
- [x] **AC-11**: GIVEN 有效签名 + pattern_id 非空，WHEN scout_sniff()，THEN IntelManager.report_observation_event(pattern_id, "partner_sniff_success") 被调用 1 次

### State Mutation (Step 5)

- [x] **AC-12**: GIVEN scout_sniff() 成功，WHEN 完成后，THEN item_id ∈ sniffed_items + sniff_success_occurred=true
- [x] **AC-13**: GIVEN scout_sniff() 成功 + 之前 sniff_success_occurred 已是 true，WHEN 完成后，THEN sniff_success_occurred 保持 true。不翻转
- [x] **AC-14**: GIVEN scout_sniff() 成功，WHEN 检查玩家背包，THEN 物品完好——不消费、不移除、不修改数量

### Reaction Selection (Step 6)

- [x] **AC-15**: GIVEN clamped_confidence ≥ 50，WHEN 选择反应动画，THEN reaction_id=REACTION_CIRCLES_TWICE（绕圈两圈后离开）
- [x] **AC-16**: GIVEN clamped_confidence < 50 + > 0，WHEN 选择反应动画，THEN reaction_id=REACTION_RUBS_FACE（蹭脸后原地坐下）

### Sniffable Items Filter

- [x] **AC-17**: GIVEN 玩家背包有 [item_A (有签名), item_B (无签名), item_C (有签名)]，WHEN get_sniffable_items()，THEN 返回 [item_A, item_C]。无签名的 item_B 被过滤
- [x] **AC-18**: GIVEN 玩家背包为空，WHEN get_sniffable_items()，THEN 返回 []。空状态

---

## Implementation Notes

### Core Algorithm

```text
const REACTION_EARS_BACK_TAIL_POINT: int = 0  # 有效线索
const REACTION_CIRCLES_TWICE: int = 1          # 强信号
const REACTION_RUBS_FACE: int = 2              # 弱信号
const REACTION_CONFUSED: int = 3               # 异域物品
const REACTION_ALREADY_SMELLED: int = 4        # 已闻过

func scout_sniff(item_id: StringName) -> Dictionary:
    # Step 0: 状态门控
    if cat_state == CAT_SNIFFING:
        return {"success": false, "reaction_id": -1, "error": &"cat_busy"}

    var p := partners[MVP_PARTNER_ID]

    # Step 1: 已嗅辨检查
    if item_id in p["sniffed_items"]:
        return {"success": false, "reaction_id": REACTION_ALREADY_SMELLED, "error": &"already_sniffed"}

    # Step 2: 读取签名
    var sig := _get_sniff_signature(item_id)
    if sig.is_empty():
        return {"success": false, "reaction_id": REACTION_CONFUSED, "error": &"no_signature"}
    var reveal_target: StringName = sig.get("reveal_target", &"")
    if reveal_target == &"":
        return {"success": false, "reaction_id": REACTION_CONFUSED, "error": &"empty_reveal_target"}

    # Step 3: F.1 截断
    var confidence: int = mini(sig.get("confidence", 0), MVP_CONFIDENCE_MAX)

    # Step 4: #6 API
    var hazard_hint: StringName = sig.get("hazard_hint", &"")
    IntelManager.reveal_rumor(reveal_target, &"partner.sky-cat", [hazard_hint], confidence)
    var pattern_id: StringName = sig.get("pattern_id", &"")
    if pattern_id != &"":
        IntelManager.report_observation_event(pattern_id, &"partner_sniff_success")

    # Step 5: 状态变更
    p["sniffed_items"].append(item_id)
    if not p["sniff_success_occurred"]:
        p["sniff_success_occurred"] = true

    # Step 6: 反应选择
    var reaction_id := REACTION_RUBS_FACE if confidence < 50 else REACTION_CIRCLES_TWICE

    # 进入 sniffing 状态
    _pre_sniff_state = cat_state
    cat_state = CAT_SNIFFING
    _sniff_lockout_remaining = T_SNIFF_LOCKOUT
    sniff_reaction_triggered.emit(reaction_id, item_id)

    return {"success": true, "reaction_id": reaction_id, "error": &""}
```

### Signature Reader

```text
func _get_sniff_signature(item_id: StringName) -> Dictionary:
    var item_def := Registry.query_entity(item_id)
    if item_def.is_empty():
        return {}
    var sig = item_def.get("cat_sniff_signature", null)
    if sig == null:
        return {}
    return sig
```

### Sniffable Items Filter

```text
func get_sniffable_items() -> Array:
    var inventory: Array = ResourcesManager.get_inventory_items()
    var candidates: Array = []
    for item_id in inventory:
        var sig := _get_sniff_signature(item_id)
        if not sig.is_empty():
            candidates.append(item_id)
    return candidates
```

---

## Out of Scope

- IntelManager.reveal_rumor() / report_observation_event() 实现——属于 intel-knowledge Epic
- ResourcesManager.get_inventory_items() 实现——属于 resources-goods-capacity Epic
- Registry.query_entity() 实现——属于 content-registry Epic
- sniff_reaction_triggered 信号的动画播放——属于 #17 Feedback 系统
- 小窝物件累积（_accumulate_nest_item）——属于 Story 003

---

## QA Test Cases

- **AC-1**: SNIFFING state gate → cat_busy
- **AC-3**: Already sniffed → REACTION_ALREADY_SMELLED
- **AC-5**: Null signature → REACTION_CONFUSED
- **AC-6**: Empty reveal_target → REACTION_CONFUSED
- **AC-7**: 6 parameterized clamp cases
- **AC-9**: reveal_rumor called with correct params
- **AC-10/11**: report_observation_event conditional call
- **AC-12**: sniffed_items updated + flag set
- **AC-14**: Item not consumed
- **AC-15/16**: Reaction selection by confidence
- **AC-17/18**: Sniffable filter

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/partner-relationships/ScoutSniffTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-13 (`18/18` checks); rerun PASS — 2026-05-14 Epic #11/#15 review

---

## Dependencies

- Depends on: Story 001 (cat state, SNIFFING gate), content-registry Epic (cat_sniff_signature query), resources-goods-capacity Epic (get_inventory_items), intel-knowledge Epic (reveal_rumor, report_observation_event)
- Unlocks: Story 003 (nest triggered by sniff), Story 004 (actual #6 API integration)

## Completion Notes

**Completed**: 2026-05-13
**Criteria**: 18/18 passing
**Deviations**: None. Registry, inventory, and Intel are injected boundaries; their concrete Autoload wiring remains Story 004 scope.
**Test Evidence**: Logic — `tests/unit/partner-relationships/ScoutSniffTest.csproj` passes 18/18 checks. Story 001 regression `CatStateMachineTest.csproj` passes 15/15 checks.
**Code Review**: Complete — APPROVED WITH SUGGESTIONS. The public API XML comment language issue was fixed before closure.
