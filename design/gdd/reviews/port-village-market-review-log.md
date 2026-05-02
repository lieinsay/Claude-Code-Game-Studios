# Review Log: 空港/村镇状态与集市交易 (System #14)

---

## Review — 2026-05-02 — Verdict: NEEDS REVISION (Revision 1 Applied)

Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, ux-designer, qa-lead, narrative-director, creative-director
Blocking items: 5 | Recommended: 6
Prior verdict resolved: First review

Summary: 该 GDD 拥有项目目前最优秀的 Player Fantasy 写作——"集市是村落的生命体征"精准锚定 Pillar 2 的情感落点。但设计意图层与可执行精确层之间存在系统性缺口：三个公式/信号断裂（#13 信号签名不匹配、F.2 集合交集无法计数、F.3 状态机空洞）、5 条 AC 不可测试、叙事资产缺失、摊位同质化破坏自身 fantasy。Revision 1 修复了全部 5 项阻塞项和 6 项建议项：切换到注册表查询的信号模型、基数匹配公式、open_expanded 限定为 post-MVP、添加 3 种独占风味商品、重写全部 AC（16 → 24 条）、新增 8 个边界情况。creative-director 建议下一轮评审仅派遣 systems-designer + narrative-director 即可。

---

## Re-review — 2026-05-02 — Verdict: NEEDS REVISION (Revision 2 Applied)

Scope signal: L
Specialists: none (lean mode — single-session analysis)
Blocking items: 2 | Recommended: 3
Prior verdict resolved: Yes — all 5 R1 blockers addressed

Summary: R1 的全部 5 项阻塞项均已妥善解决。R2 发现 2 项新阻塞项——均为文档层面修正：（1）系统交叉引用编号与 systems-index 不一致（#8→#10、#1→#7、OQ-3 中 #8→#6）；（2）`validate_purchase` 签名与 #5 的已发布 2 参数接口冲突——通过统一为 2 参数传入解决，#5 从内容注册表读取价格。3 项建议项：F.1 中 `remaining_capacity` 缺少接口定义（已在交互表中新增 `get_remaining_capacity(pool_id)`）、叙事资产缺失（`glass-harbor.md`）、`use_requested` 信号签名标注。Revision 2 已应用全部修正。

---

## Re-review — 2026-05-03 — Verdict: APPROVED (Revision 3 Applied)

Scope signal: L
Specialists: none (lean mode — single-session, focused 3-point check)
Blocking items: 1 | Recommended: 4
Prior verdict resolved: Yes — both R2 blockers addressed

Summary: R2 的 2 项阻断项已确认修复。R3 聚焦 3 个跨系统契约检查：(1) stall_unlock 与 #13 `repair_completed` 信号契约对齐（签名一致，新增 `linked_location_id → settlement_id` 映射）；(2) purchase_total 与 #5 容量校验发现 1 项阻断——补给品目标池标注为 Pool 3（货舱）与 #5 的 Pool 2（仓库）消耗模型冲突，已修正为 Pool 2；(3) settlement_activity F.3 聚合逻辑在 MVP 范围内正确，补充了 `active_stall_count = 0` 边界情况。附加修复：Dependencies 节残留的旧 3-param 签名统一、货币/容量查询接口与 #5 实际暴露的 `get_storage_summary()` 对齐。
