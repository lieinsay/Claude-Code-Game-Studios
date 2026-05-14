# Story 006: Edge Cases, R15 Guards & Defensive Handling

> **Epic**: Partner & Relationships
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/partner-relationships.md`
**Requirement**: `TR-partner-001`, `TR-partner-002`, `TR-partner-003`

**ADR Governing Implementation**: ADR-0015 (§5c scout_sniff 守卫, §5e 命名边缘情况, §5d 小窝上限, R15 硬禁止, Consequences Risks)
**ADR Decision Summary**: 本 Story 覆盖 GDD 全部 30+ 边缘情况（E.1 命名 7 个 / E.2 嗅辨 10 个 / E.3 小窝 4 个 / E.4 状态机 4 个 / E.5 接口 5 个 / E.6 存在性 4 个）以及 R15 6 条硬禁止的系统层验证。边缘情况守卫分布在多个函数中：scout_sniff() 的状态门控/已嗅辨去重/null签名兜底/concurrent防止/物品不消费/departure中断；submit/skip_naming() 的空字符串拒绝/3次跳过锁/已完成拒绝/长度截断；_accumulate_nest_item() 的 CAP 守卫/终态静默跳过；猫状态机的 cooldown 防抖/arrival 强制重置/departure 冻结/in_transit 简化。R15 硬禁止需在数据模型和 API 层面可验证：无好感度字段、无礼物函数、无事件树、无定时器奖励、无第二只伙伴工厂、无招募/解雇 API。接口合约边缘情况：reveal_rumor 失败不重试、forward compat 未知字段静默忽略、命名 UI 阻塞 departure。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Performance**: No frame-loop work expected. Defensive guards run only on explicit partner actions, Hub events, save/load restore, or integration tests. R15 audit checks must remain code-review/test-time verification and must not add _process-driven partner rewards, polling, or state mutation.

**Control Manifest Rules (Feature layer)**:
- Required: 所有 R15 禁止项在代码审查中可明确验证——不存在相关字段/函数；嗅辨不消费物品——物品在背包保持完整；命名终态后所有代码路径拒绝新命名
- Forbidden: 任何 _process 驱动的伙伴奖励/状态变更——R15.4；对已嗅辨物品调用 #6 API；sniffing 状态期间接受新 sniff
- Guardrail: reveal_rumor 失败静默——只记录 warning（dev build），玩家不可见

---

## Acceptance Criteria

### E.1 Naming Edge Cases

- [x] **AC-1**: GIVEN 玩家从未成功嗅辨（sniff_success_occurred=false），WHEN 多次归港，THEN naming UI 永不触发。猫未证明自己（E.1.a）
- [x] **AC-2**: GIVEN 第 3 次 skip + 应用关闭请求瞬间，WHEN skip_naming()，THEN name="那只猫" 被写入。静默——无通知弹窗（E.1.b）
- [x] **AC-3**: GIVEN naming_state=PROMPTED + 玩家提交 "   "（纯空格），WHEN submit_partner_name("   ")，THEN 拒绝 {error: "name_empty"}。skip_count 不增加（E.1.c）
- [x] **AC-4**: GIVEN 命名 UI 打开时存档，WHEN 读档，THEN naming_state=PENDING + skip_count 保留。下次归港重新触发（E.1.e）
- [x] **AC-5**: GIVEN 命名 UI 打开时 Hub→departure_locked，WHEN 触发，THEN departure 推迟——命名 modal 先于 departure 控件可用（E.1.g）

### E.2 Sniffing Edge Cases

- [x] **AC-6**: GIVEN 背包为空，WHEN get_sniffable_items()，THEN 返回 []。嗅辨面板显示空状态（E.2.a）
- [x] **AC-7**: GIVEN 背包物品全部无 cat_sniff_signature，WHEN get_sniffable_items()，THEN 返回 []（E.2.b）
- [x] **AC-8**: GIVEN 同一 item_id 给猫两次，WHEN 第 2 次 scout_sniff()，THEN REACTION_ALREADY_SMELLED。无 #6 调用。物品仍在背包（E.2.c）
- [x] **AC-9**: GIVEN raw_confidence=90 + MVP_CONFIDENCE_MAX=66，WHEN F.1 截断，THEN final=66。永不达 67 权威（E.2.e）
- [x] **AC-10**: GIVEN cat_sniff_signature 存在但 reveal_target=""，WHEN scout_sniff()，THEN REACTION_CONFUSED。防御性检查（E.2.f）
- [x] **AC-11**: GIVEN item_A 和 item_B 指向同一 reveal_target + confidence 不同，WHEN 依次 sniff，THEN 两次 reveal_rumor() 各自调用。不合并、不查重（E.2.g）
- [x] **AC-12**: GIVEN UI spam——10ms 内 5 次 scout_sniff() 调用，WHEN 处理，THEN 仅 1 次成功。其余被 SNIFFING state gate 拒绝（E.2.h）
- [x] **AC-13**: GIVEN sniff 动画进行中 + Hub→departure_locked，WHEN 检查数据，THEN 嗅辨数据已在动画前提交——无丢失。动画可能截断（E.2.i）
- [x] **AC-14**: GIVEN 多个可嗅辨物品 + 玩家 sniff 1 个后立即出航，WHEN 检查，THEN 剩余物品仍在背包。未来归港仍可嗅辨——无惩罚（E.2.j）

### E.3 Nest Edge Cases

- [x] **AC-15**: GIVEN 从未成功嗅辨，WHEN 检查，THEN nest_state=EMPTY 永久。Hub 不渲染小窝痕迹（E.3.a）
- [x] **AC-16**: GIVEN nest_state=FULL + 第 5 次嗅辨，WHEN _accumulate_nest_item()，THEN 静默跳过。nest_items=[0,1,2,3] 不变（E.3.b）
- [x] **AC-17**: GIVEN nest_items=[0,1] (accumulating) + 存档→读档，WHEN 恢复，THEN nest_items=[0,1] + Hub 渲染 2 件物件（E.3.c）
- [x] **AC-18**: GIVEN 存档写入失败（crash）+ 内存中 nest_items 新增 1 件，WHEN 下次读取，THEN 快照中不含新物件——玩家可重新嗅辨触发。数据不损坏（E.3.d）

### E.4 State Machine Edge Cases

- [x] **AC-19**: GIVEN 存档时 cat_state=SNIFFING，WHEN 读档，THEN cat_state 从 Hub 派生。嗅辨动画不恢复（E.4.a）
- [x] **AC-20**: GIVEN 玩家在 1s 内 5 次进出生活舱，WHEN 观察 cat_state，THEN 最多 2 次转换。cooldown 防抖有效（E.4.b）
- [x] **AC-21**: GIVEN pre-departure 猫在 IN_NEST + arrival 后，WHEN 检查，THEN cat_state=IDLE_LIVING_QUARTERS。猫在生活舱，不在入口（E.4.c）
- [x] **AC-22**: GIVEN 猫在 IN_NEST + 玩家进入生活舱，WHEN player_entered_zone("living_quarters")，THEN cat_state→IDLE_LIVING_QUARTERS。猫离开窝（E.4.d）

### E.5 Interface Contract Edge Cases

- [x] **AC-23**: GIVEN IntelManager.reveal_rumor() 抛出异常，WHEN scout_sniff() 处理，THEN 异常被捕获。本地状态正确提交。记录 warning（E.5.a）
- [x] **AC-24**: GIVEN pattern_id="pattern.nonexistent"（不存在于 #6），WHEN report_observation_event() 调用，THEN 不抛异常——#6 负责验证，Partner 只透传（E.5.b）
- [x] **AC-25**: GIVEN cat_sniff_signature 包含未知字段（forward compat），WHEN _get_sniff_signature() 读取，THEN 只读取已知字段（reveal_target/hazard_hint/confidence/pattern_id）。未知字段静默忽略（E.5.e）

### E.6 Presence & Init Edge Cases

- [x] **AC-26**: GIVEN in_transit 期间调用 query_partner_present()，WHEN 返回，THEN true。猫在逻辑上仍在飞艇（E.6.a）
- [x] **AC-27**: GIVEN player_returned_to_hub 因 bug 重复发射 3 次，WHEN 处理，THEN 命名仅触发 1 次。后续调用因 naming_state≠PENDING 而 no-op（E.6.b）
- [x] **AC-28**: GIVEN Hub 事件先于 PartnerManager 订阅发出，WHEN sync_with_hub_state()，THEN cat_state 正确。初始化竞态已处理（E.6.c）

### R15 Hard Prohibition Verification

- [x] **AC-29**: GIVEN PartnerManager 的完整 API 和数据模型，WHEN 审计，THEN 不存在 affection / friendship / bond / relationship_level 字段。R15.1 可验证
- [x] **AC-30**: GIVEN 代码库搜索，WHEN 查找伙伴交互入口，THEN scout_sniff 是唯一物品交互路径。无 gift / donate / present 函数。R15.2 可验证
- [x] **AC-31**: GIVEN 猫行为代码路径，WHEN 审计，THEN 无事件触发条件 / 故事节点 / 对话分支引用。猫行为仅由状态机 + Hub 事件驱动。R15.3 可验证
- [x] **AC-32**: GIVEN 猫状态变更逻辑，WHEN 审计，THEN 无 _process 驱动的伙伴奖励或状态突变。所有状态变更均为事件驱动。R15.4 可验证
- [x] **AC-33**: GIVEN partners Dictionary，WHEN 初始化后，THEN 仅含 partner.sky-cat。无伙伴工厂/生成器接受其他 partner_id。R15.5 可验证
- [x] **AC-34**: GIVEN API 搜索，WHEN 查找，THEN 无 recruit / dismiss / remove / add 伙伴函数。on_partner_joined 仅 bootstrap 调用 1 次。R15.6 可验证

---

## Implementation Notes

### E.5.a Reveal Rumor Failure

```text
func _safe_reveal_rumor(reveal_target: StringName, hazard_hint: StringName, confidence: int) -> bool:
    if IntelManager == null:
        push_warning("Partner: IntelManager unavailable")
        return false
    var ok := IntelManager.reveal_rumor(reveal_target, &"partner.sky-cat", [hazard_hint], confidence)
    if not ok:
        push_warning("Partner: reveal_rumor failed for target '%s'" % reveal_target)
    return ok
```

### R15 Verification — Data Model Audit

```text
# R15.1 验证: 在 PartnerState 中搜索 forbidden 字段
const FORBIDDEN_FIELDS: Array = ["affection", "friendship", "bond", "relationship_level"]

func _debug_verify_r15_guards() -> bool:
    var p := partners[MVP_PARTNER_ID]
    for field in FORBIDDEN_FIELDS:
        if field in p:
            push_error("Partner: R15.1 violation — forbidden field '%s' found!" % field)
            return false

    # R15.2 验证: 物品永不消费
    # (在 scout_sniff 中——无 item.remove 或 quantity-- 调用)

    # R15.4 验证: _process 不产生奖励
    # (cat_state 变更仅由 Hub 事件触发——check _process 实现)

    # R15.5 验证: 仅 partner.sky-cat
    if partners.size() != 1 or not partners.has(MVP_PARTNER_ID):
        push_error("Partner: R15.5 violation — unexpected partner count or ID!")
        return false

    return true
```

### E.2.h Spam Prevention

```text
# sniffing 状态门控已在 Story 002 scout_sniff() Step 0 实现
# 验证方式: mock 快速连续调用 → assert 仅 1 次成功
```

### E.1.g Naming Blocks Departure

```text
# 由 #16 UI 层实现——命名 modal 打开期间 departure 按钮不可用
# Partner 系统仅提供状态: naming_state == PROMPTED → UI 阻塞 departure
```

---

## Out of Scope

- UI 层命名 modal 的 departure 阻塞——属于 #16 UIManager
- UI 层嗅辨面板的空状态渲染——属于 #16 UIManager
- IntelManager 对未知 pattern_id 的处理——属于 intel-knowledge Epic
- 猫动画的 departure 截断视觉表现——属于 #17 Feedback 系统
- Hub 痕迹锚点渲染——属于 Hub #7

---

## QA Test Cases

- **AC-1-5**: All E.1 naming edge cases
- **AC-6-14**: All E.2 sniffing edge cases
- **AC-15-18**: All E.3 nest edge cases
- **AC-19-22**: All E.4 state machine edge cases
- **AC-23-25**: All E.5 interface contract edge cases
- **AC-26-28**: All E.6 presence & init edge cases
- **AC-29-34**: All 6 R15 hard prohibitions verified

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/partner-relationships/EdgeCasesTest.csproj` — must exist and pass, OR documented playtest covering all ACs
**Status**: [x] Created — `dotnet run --project tests/integration/partner-relationships/EdgeCasesTest.csproj` PASS (34/34); rerun PASS — 2026-05-14 Epic #11/#15 review

---

## Dependencies

- Depends on: Story 001 (cat state machine), Story 002 (scout_sniff edge cases), Story 003 (naming/nest edge cases), Story 004 (integration guardrails), Story 005 (persistence edge cases)
- Unlocks: N/A — 这是 Partner & Relationships Epic 的最后一个 Story

## Completion Notes

**Completed**: 2026-05-14
**Criteria**: 34/34 passing
**Deviations**: None
**Test Evidence**: Integration test at `tests/integration/partner-relationships/EdgeCasesTest.csproj` — 34/34 checks passing
**Code Review**: Complete — `/code-review tests/integration/partner-relationships/EdgeCasesProgram.cs tests/integration/partner-relationships/EdgeCasesTest.csproj` approved with suggestions; suggestion to add this project to the manually enumerated CI workflow remains follow-up scope
**Review Gates**: QL-TEST-COVERAGE and LP-CODE-REVIEW subagent gates skipped under Codex adapter rules because subagent delegation was not explicitly requested; local coverage/review checks completed
