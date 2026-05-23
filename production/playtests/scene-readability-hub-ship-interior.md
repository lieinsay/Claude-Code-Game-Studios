# hub_ship_interior 用户可读性验收记录

> **父清单**: [scene-composition-user-readability-checklist.md](scene-composition-user-readability-checklist.md)
> **当前状态**: `PENDING_USER_REVIEW`
> **Release handoff**: `BLOCKED_FOR_RELEASE`

## 必填上下文

| 字段 | 填写值 |
| --- | --- |
| Scene ID | `hub_ship_interior` |
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

## Verdict

| 字段 | 填写值 |
| --- | --- |
| 用户最终 verdict | PENDING / PASS / PASS_WITH_CONDITIONS / BLOCKED / WAIVED_BY_USER |
| Blocker / condition |  |
| Waiver owner |  |
| Waiver date |  |
| 接受的风险 |  |
| Fallback evidence |  |
| Follow-up owner |  |
| Follow-up date / next story |  |

