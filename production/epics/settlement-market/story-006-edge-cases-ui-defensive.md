# Story 006: Edge Cases, UI Integration & Defensive Handling

> **Epic**: Settlement Market & Port Village Economy
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/port-village-market.md`
**Requirement**: `TR-settlement-001`, `TR-settlement-002`, `TR-settlement-003`

**ADR Governing Implementation**: ADR-0014 (§4 方法接口守卫, §3 信号合同, Consequences Risks, GDD Requirements Addressed — E.1-E.16)
**ADR Decision Summary**: 本 Story 覆盖 GDD 全部 16 个边缘情况 (E.1-E.16)，确保 SettlementManager 在异常输入、跨系统边界条件、配置错误和并发操作下的鲁棒性。信号合同覆盖：stall_opened / stall_state_changed / npc_state_changed → UI (#16) 更新摊位外观和 NPC 动画；purchase_completed / purchase_failed → UI 显示购买结果；settlement_activity_changed → Feedback (#17) 切换环境氛围（冷/暖色调、环境音）。UI 集成不创建场景节点（Autoload 无场景引用）——仅通过信号通知场景层。防御性处理：Registry 缺失实体回退、重复信号去重、容量/货币边界值、price=0 商品错误日志、UI 打开期间状态变更的会话一致性。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: closed 摊位不注册为焦点目标——E.1；购买界面为只读展示+购买确认模态——E.5 存档不记录界面状态；quantity 输入 range ∈ [1, max_affordable] + 非整数向下取整——E.13
- Forbidden: 摊位界面打开期间收到的修复信号修改当前会话商品列表——E.9 界面关闭重开后反映；不允许 quantity ≤ 0 的购买请求到达 execute_purchase
- Guardrail: 所有 16 个边缘情况必须有对应的守卫逻辑——缺失守卫不应崩溃但需记录 warning

---

## Acceptance Criteria

### E.1 — Interaction with Closed Stall

- [ ] **AC-1**: GIVEN stall_state=CLOSED，WHEN 检查，#4 焦点系统不将该摊位注册为可交互目标。use_requested 不被分发

### E.2 — Capacity Full Purchase

- [ ] **AC-2**: GIVEN 目标资源池无剩余容量 + 玩家尝试购买补给品，WHEN validate_purchase_request()，THEN 返回 {valid: false, reason: "capacity_full"}。购买被阻止，不扣货币

### E.3 — Insufficient Funds

- [ ] **AC-3**: GIVEN player_currency < price × 1，WHEN 检查，THEN 该商品在界面中灰显不可选。validate_purchase_request 返回 {valid: false, reason: "insufficient_funds"}

### E.4 — Duplicate Repair Signal

- [ ] **AC-4**: GIVEN 同一 node_id 的 repair_completed 已处理过一次，WHEN 再次收到，THEN completed_node_ids 不变 + 已 open 摊位不重复解锁。集合去重保证幂等

### E.5 — Save During Purchase UI Open

- [ ] **AC-5**: GIVEN 玩家在购买界面打开时触发存档，WHEN Persistence 捕获快照，THEN progress.settlement-market 快照仅记录摊位/NPC 状态——不记录界面状态。购买为原子操作（validate→execute 同帧完成），不存在"购物车半满"的中间状态
- [ ] **AC-6**: GIVEN 读档后，WHEN 恢复，THEN 摊位界面关闭——玩家回到摊位前。界面不跨会话保持

### E.6 — All Stalls Closed Settlement (Future)

- [ ] **AC-7**: GIVEN 未来定居点不设默认开启摊位 + 所有摊位 CLOSED，WHEN get_interactive_stalls()，THEN 返回 []。F.3 判定 active_stall_count=0 → DORMANT。不阻塞游戏

### E.7 — Missing Narrative File / npc_id

- [ ] **AC-8**: GIVEN design/narrative/glass-harbor.md 不存在或 npc_id 在该文件中未定义，WHEN 读取 NPC 名字/对话，THEN 名字回退为 "摊主"，对话回退为空字符串。购买功能不受影响。日志输出 warning

### E.8 — All Stalls at MVP Max Unlock

- [ ] **AC-9**: GIVEN 全部 4 个摊位均已 OPEN_BASIC（MVP 终态）+ 新的 repair_completed 到达，WHEN 处理，THEN 状态机无有效转换——信号安全忽略。不产生错误

### E.9 — Repair Signal During Purchase UI Open

- [ ] **AC-10**: GIVEN 玩家在购买界面打开时 + 该摊位因修复信号发生解锁等级变化，WHEN 检查，THEN 当前购买会话不受影响——界面继续显示打开时的商品列表。界面关闭并重新打开后，反映新的解锁等级商品列表

### E.10 — Single Repair Matching Multiple Stalls

- [ ] **AC-11**: GIVEN 单个 repair_completed(node_id) 的 node_id 匹配 2 个摊位的 required_node_ids，WHEN 处理，THEN 两个摊位各自独立 CLOSED→OPEN_BASIC + 对应 NPC 各自 ABSENT→IDLE。互不干扰

### E.11 — Empty required_node_ids

- [ ] **AC-12**: GIVEN 某摊位 (非默认杂货摊) 配置了空的 required_node_ids，WHEN F.2 判定，THEN 返回 false。该摊位永远无法从 CLOSED 自动解锁。初始化时记录 warning

### E.12 — Repair Node Not Matching Any Stall

- [ ] **AC-13**: GIVEN repair_completed(node_id) + node_id 不出现在任何摊位的 required_node_ids 中，WHEN 处理，THEN 信号被安全忽略——静默处理。不产生错误日志

### E.13 — Abnormal Quantity Input

- [ ] **AC-14**: GIVEN 购买 UI 中 quantity 输入，WHEN 玩家输入 0、负值或非整数，THEN 0 和负值 clamp 为 1；非整数向下取整。减号按钮在 quantity=1 时灰显
- [ ] **AC-15**: GIVEN quantity 输入值 > max_affordable，WHEN 失焦，THEN clamp 为 max_affordable

### E.14 — Player Leaves Interaction Range with UI Open

- [ ] **AC-16**: GIVEN 玩家在购买界面打开时步行离开摊位交互范围，WHEN 检查，THEN 界面保持打开直到手动关闭或再次按 Use。界面关闭后若仍在范围外，按 Use 无反应

### E.15 — Price = 0 (Configuration Error)

- [ ] **AC-17**: GIVEN 某商品 price=0 + 玩家购买 quantity=3，WHEN execute_purchase()，THEN total_cost=0 → 购买成功（不扣货币）。货物正确进入资源池。日志输出 error: "Settlement: good '%s' has price=0 — configuration error"

### E.16 — Player Currency Equals Total Cost

- [ ] **AC-18**: GIVEN player_currency=150 + total_cost=150（恰好相等），WHEN validate_purchase_request()，THEN total_cost ≤ player_currency → 通过。购买成功后货币归零 + 界面刷新所有商品灰显（货币不足）

### UI Integration Signals

- [ ] **AC-19**: GIVEN stall_opened(stall_id, settlement_id) 发射，WHEN UI (#16) 消费，THEN 打开购买界面，显示该摊位当前解锁等级下所有可用商品及价格
- [ ] **AC-20**: GIVEN purchase_completed(good_id, quantity, total_cost) 发射，WHEN UI 消费，THEN 更新货币显示 + 播放购买确认音效
- [ ] **AC-21**: GIVEN purchase_failed(good_id, reason) 发射，WHEN UI 消费，THEN 显示对应错误提示（"货币不足" / "携带空间不足"）
- [ ] **AC-22**: GIVEN settlement_activity_changed(settlement_id, active_stall_count) 发射，WHEN Feedback (#17) 消费，THEN 切换环境氛围——dormant 冷色调+安静，recovering 暖色调逐渐恢复，active 全暖色调+粒子效果

### Defensive: Registry Entity Missing

- [ ] **AC-23**: GIVEN Registry 中缺失 settlement.glass-harbor 实体定义，WHEN 初始化，THEN 记录 error + 跳过 settlement 初始化。stalls/npcs 仍正常初始化——不级联失败
- [ ] **AC-24**: GIVEN Registry 中某 good_id 的 price 字段缺失，WHEN calculate_total_cost()，THEN price 默认为 0 + 记录 error 日志。不崩溃

---

## Implementation Notes

### E.7 Narrative Fallback

```gdscript
func _get_npc_display_name(npc_id: StringName) -> String:
    var npc_def := Registry.query_entity(npc_id)
    var narrative_key: String = npc_def.get("narrative_key", "")
    if narrative_key == "":
        push_warning("Settlement: NPC '%s' has no narrative_key" % npc_id)
        return "摊主"

    var narrative_data := _load_narrative_data()
    if narrative_data.is_empty():
        push_warning("Settlement: narrative file missing or empty")
        return "摊主"

    var display_name: String = narrative_data.get(narrative_key + ".display_name", "")
    if display_name == "":
        push_warning("Settlement: NPC '%s' narrative key '%s' not found" % [npc_id, narrative_key])
        return "摊主"

    return display_name
```

### E.9 Repair During Purchase

```gdscript
# 购买界面打开期间收到修复信号——不修改当前商品列表
# 实现方式：get_stall_goods() 在界面打开时快照商品列表 → UI 缓存该列表
# 界面关闭重开后重新调用 get_stall_goods() 获取最新列表
# SettlementManager 本身不维护"当前打开界面"状态——这是 UI 层的职责
```

### E.13 Quantity Clamping

```gdscript
func clamp_purchase_quantity(good_id: StringName, requested: int) -> int:
    if requested < 1:
        return 1
    var max_affordable := get_max_affordable(good_id)
    return mini(requested, max_affordable)
```

### E.15/E.16 Edge Cases in validate_purchase_request

```gdscript
# E.15 (price=0) 和 E.16 (货币恰好等于 total_cost) 已在 Story 002 的
# validate_purchase_request() 中自然处理：
# - price=0 → total_cost=0 → ResourcesManager 不扣货币但仍然转移货物
# - player_currency == total_cost → total_cost ≤ player_currency → 通过
# 本 Story 仅需确保日志记录和 UI 刷新正确
```

### Signal Documentation for UI Consumers

```gdscript
# SettlementManager 为 UI/FX 系统提供的信号合同：
#
# UI (#16) 应连接:
#   stall_opened → 打开购买界面，显示 get_stall_goods() 返回的商品列表
#   purchase_completed → 更新货币显示 + 播放购买确认音效
#   purchase_failed → 显示错误提示 (reason: "capacity_full" | "insufficient_funds")
#
# Feedback (#17) 应连接:
#   stall_opened → 摊位开张动画 (木板移除, NPC 出现)
#   npc_state_changed → NPC 动画切换 (absent→idle 闲置动画)
#   settlement_activity_changed → 环境氛围切换 (冷色调→暖色调, 环境音)
```

---

## Out of Scope

- UI 购买界面创建/渲染/销毁——属于 #16 UIManager
- 摊位视觉外观切换（木板封挡→开张）——属于 #17 Feedback/VFX 系统
- NPC 模型显示与动画——属于场景层
- 购买确认音效播放——属于 #17 Feedback 系统
- 环境氛围（冷/暖色调、粒子效果）切换——属于 #17 Feedback 系统
- 叙事文件 glass-harbor.md 的创建与内容——属于 narrative-director
- InteractionRegistry 焦点注册过滤 closed 摊位——属于 player-movement-interaction Epic

---

## QA Test Cases

- **AC-1**: Closed stall not registered as focus target
- **AC-2**: Capacity full → purchase blocked
- **AC-3**: Insufficient funds → good greyed out
- **AC-4**: Duplicate repair → idempotent
- **AC-5/6**: Save during purchase UI → only data state persisted; UI closed on restore
- **AC-7**: 0 active stalls → DORMANT, no interactive targets
- **AC-8**: Missing narrative → fallback display name
- **AC-9**: All stalls at MVP max → new repair ignored
- **AC-10**: Repair during purchase UI → current session unchanged
- **AC-11**: Single repair matching 2 stalls → both unlock
- **AC-12**: Empty required_node_ids → never auto-unlock + warning
- **AC-13**: Unmatched repair node → silent ignore
- **AC-14/15**: Quantity input clamping
- **AC-16**: Leave range with UI open → UI stays until closed
- **AC-17**: price=0 → success + error log
- **AC-18**: player_currency == total_cost → success, currency→0
- **AC-19-22**: UI/FX signal consumption contracts
- **AC-23**: Missing settlement entity → error logged, no cascade
- **AC-24**: Missing price field → default 0 + error log

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/settlement-market/edge_cases_test.gd` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (purchase flow edge cases), Story 003 (unlock edge cases), Story 004 (signal wiring + UI contracts), Story 005 (persistence edge case E.5), content-registry Epic (Registry query_entity for all entities), player-movement-interaction Epic (focus registration filtering)
- Unlocks: N/A — 这是 Settlement Market Epic 的最后一个 Story
