# old_market_edge_scene 用户可读性验收记录

> **父清单**: [scene-composition-user-readability-checklist.md](scene-composition-user-readability-checklist.md)
> **当前状态**: `PENDING_SCENE_SPEC`
> **Release handoff**: `BLOCKED_FOR_RELEASE`

## 必填上下文

| 字段 | 填写值 |
| --- | --- |
| Scene ID | `old_market_edge_scene` |
| 玩家可见场景名 | 旧集市边缘 |
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
| 我在哪里？ | 约 3 秒内能读出这是旧集市边缘，而不是通用市场 UI 或空岛背景。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我在这里能做什么？ | 探路、接近摊位/建筑/NPC、准备交易或返回的核心行动能从场景锚点读出。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我如何离开或继续？ | 回到航行大场景、进入集市内部或返回飞船的路径可见。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 什么发生了变化？ | 摊位开放、NPC 活跃、货物或修复后变化能在场景中看见。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| UI/HUD 是辅助而不是主导吗？ | `hud_not_dominant = true`；交易 UI 不替代旧集市场景身份。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 场景是否符合预期幻想？ | 旧集市边缘应像一个可抵达、可观察、可进入交易生活的地方。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |

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

