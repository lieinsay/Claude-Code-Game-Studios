# ship_interior_layered 用户可读性验收记录

> **父清单**: [scene-composition-user-readability-checklist.md](scene-composition-user-readability-checklist.md)
> **当前状态**: `PENDING_USER_REVIEW`
> **Release handoff**: `BLOCKED_FOR_RELEASE`

## 必填上下文

| 字段 | 填写值 |
| --- | --- |
| Scene ID | `ship_interior_layered` |
| 历史来源 ID | `hub_ship_interior` |
| 玩家可见场景名 | 云织号船内分层水平场景 |
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
| 我在哪里？ | 约 3 秒内能读出这是飞船内部，且能区分驾驶舱、货舱、轮机间或走廊。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我在这里能做什么？ | 舵轮、情报、货物、模块、维修等核心行动能从空间锚点读出。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我如何离开或继续？ | 舱门、航行入口、返航/离船路径可见或可通过舱段锚点发现。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 什么发生了变化？ | 船体损伤、货物、模块、生活痕迹等状态变化能在船内空间看见。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 分层和遮挡可读吗？ | 水平场景的层级、前景遮挡、设备背后通行和 behind-object reveal 不让玩家迷路或丢失角色。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| UI/HUD 是辅助而不是主导吗？ | `hud_not_dominant = true`；UI 不替代船内空间身份。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 场景是否符合预期幻想？ | 船内应像“家”和整备空间，而不是一组功能面板。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |

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

