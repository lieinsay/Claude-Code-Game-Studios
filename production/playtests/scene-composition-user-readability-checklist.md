# 场景构成用户可读性检查清单

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **最后更新**: 2026-05-24
> **目的**: 在任何 release-readiness handoff 前，把主观的场景可读性评审记录成可追踪的人工验收结论。

## 使用规则

当某个场景包的自动化场景证据和 Codex review 已通过后，再运行这份清单。即使 Codex review 通过，用户 review 仍然可以阻塞该场景。

允许的 verdict：

- `PASS`
- `PASS_WITH_CONDITIONS`
- `BLOCKED`
- `WAIVED_BY_USER`

`BLOCKED` 会阻止 release gate handoff，直到 blocker 被解决，或用户明确 waiver。`WAIVED_BY_USER` 必须记录用户、日期、接受的风险、fallback evidence 和 follow-up owner。

## 必填上下文

| 字段 | 必填值 |
| --- | --- |
| Scene ID |  |
| 玩家可见场景名 |  |
| 测试的 build 或 commit |  |
| 测试的 runtime path |  |
| 自动化证据链接 |  |
| Codex review verdict | PASS / PASS_WITH_CONDITIONS / BLOCKED |
| 用户 reviewer |  |
| Review 日期 |  |
| 截图 / 录屏路径（如有视觉结论） |  |

## 可读性问题

Reviewer 应该在没有开发者解释的情况下回答。

| 问题 | 通过标准 | Verdict | 备注 / blocker |
| --- | --- | --- | --- |
| 我在哪里？ | 约 3 秒内能读出地点身份和氛围。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我在这里能做什么？ | 核心可用行动能从 world/playable anchors 读出，而不只靠 UI。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我如何离开或继续？ | 出口、返回或继续路径可见，或能通过场景锚点发现。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 什么发生了变化？ | 相关状态变化能在 world/playable scene 中看见，或由反馈清楚支持。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| UI/HUD 是辅助而不是主导吗？ | `hud_not_dominant = true`；UI 不隐藏或替代世界身份。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 场景是否符合预期幻想？ | 缺失的幻想、需求、不可接受流程或新增诉求都被记录。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |

## Blocker 与 Waiver 记录

| Scene ID | Reviewer verdict | Blocker / condition | Waiver owner | Waiver date | 接受的风险 | Fallback evidence | Follow-up owner | Follow-up date / next story |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
|  | PASS / PASS_WITH_CONDITIONS / BLOCKED / WAIVED_BY_USER |  |  |  |  |  |  |  |

## Release Handoff 决策

只有同时满足以下条件，场景才可以进入 release handoff：

- Codex review 不是 `BLOCKED`
- user review 是 `PASS`、`PASS_WITH_CONDITIONS` 或 `WAIVED_BY_USER`
- 所有 `PASS_WITH_CONDITIONS` 条目都有 owner 和 follow-up date
- 每个 `BLOCKED` 条目都已解决，或已被用户明确 waiver
- 没有未 waiver 的 P0 scene asset gap
- 没有用 UI-only evidence 作为场景证明

## 当前场景 Review 队列

| Scene ID | 当前 release-readiness 状态 | 必要用户动作 |
| --- | --- | --- |
| `hub_island_dock` | `BLOCKED_PENDING_USER_REVIEW` | 附上独立 scene spec / Codex review 后运行 checklist。 |
| `hub_ship_interior` | `BLOCKED_PENDING_USER_REVIEW` | 附上独立 scene spec / Codex review 后运行 checklist。 |
| `chart_table_scene` | `BLOCKED_PENDING_USER_REVIEW_OR_SCOPE_DECISION` | 用户必须决定当前 UI-assisted surface 是否可作为 ship-interior handoff 的一部分，或是否需要独立 scene spec。 |
| `exploration_mist_island` | `BLOCKED_PENDING_USER_REVIEW` | 附上独立 scene spec / Codex review 后运行 checklist。 |
| `repair_node_scene` | `BLOCKED_TRACKED_GAP` | 用户 release review 前，需要先起草 scene spec 和 #20 contract。 |
| `market_scene` | `BLOCKED_TRACKED_GAP` | 用户 release review 前，需要先起草 scene spec 和 #20 contract。 |
