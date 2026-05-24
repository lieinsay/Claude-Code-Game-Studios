# mist_lamp_wreck_scene 用户可读性验收记录

> **父清单**: [scene-composition-user-readability-checklist.md](scene-composition-user-readability-checklist.md)
> **当前状态**: `PENDING_USER_REVIEW`
> **Release handoff**: `BLOCKED_FOR_RELEASE`

## 必填上下文

| 字段 | 填写值 |
| --- | --- |
| Scene ID | `mist_lamp_wreck_scene` |
| 玩家可见场景名 | 雾灯残骸 |
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
| 我在哪里？ | 约 3 秒内能读出这是雾灯残骸，而不是泛雾海空地。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我在这里能做什么？ | 搜索、打捞、调查残骸或撤离的核心行动能从场景锚点读出。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我如何离开或继续？ | 返航船位、撤离点或回到航行大场景的路径可见。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 什么发生了变化？ | 搜索进度、残骸状态、危险变化或收益锁定能在世界中看见。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| UI/HUD 是辅助而不是主导吗？ | `hud_not_dominant = true`；UI 不替代残骸、雾灯、搜索锚点。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 场景是否符合预期幻想？ | 雾灯残骸应像一个具体目的地，有残损结构、雾、线索和撤离判断。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |

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

