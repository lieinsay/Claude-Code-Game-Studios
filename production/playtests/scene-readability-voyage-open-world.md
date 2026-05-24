# voyage_open_world_scene 用户可读性验收记录

> **父清单**: [scene-composition-user-readability-checklist.md](scene-composition-user-readability-checklist.md)
> **当前状态**: `PENDING_SCENE_DESIGN`
> **Release handoff**: `BLOCKED_FOR_RELEASE`

## 设计前置说明

该场景合并“初始岛屿 → 雾灯残骸”和“初始岛屿 → 旧集市边缘”两条航道，作为当前 demo 的航行大场景。它需要先独立设计，再进入人工 readability review。

核心要求：玩家视角始终与飞船前进方向保持一致；飞船可以拐弯、前进、后退；运动表现应主要来自世界、云层、航标、风险物、远近岛屿轮廓的变化。

## 必填上下文

| 字段 | 填写值 |
| --- | --- |
| Scene ID | `voyage_open_world_scene` |
| 包含航道 | 初始岛屿→雾灯残骸；初始岛屿→旧集市边缘 |
| 玩家可见场景名 | 航行大场景 |
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
| 我在哪里？ | 约 3 秒内能读出自己正在空海航行，而不是停在地图 UI 或进度条界面。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我在这里能做什么？ | 前进、转向、后退、撤退或继续航行的核心行动能从场景反馈读出。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 我如何离开或继续？ | 抵达目的地、返航、撤退或进入下一场景的路径可读。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 什么发生了变化？ | 风险、航向、距离、目的地接近、世界层级运动能被看见。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 伪 3D 航行成立吗？ | 视角跟随飞船前进方向；世界变化提供运动感；转向/后退不会让玩家迷失。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| UI/HUD 是辅助而不是主导吗？ | `hud_not_dominant = true`；UI 不替代航行世界和风险物。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| 场景是否符合预期幻想？ | 航行应成为重要玩法，而不是航图确认后的等待条。 | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |

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

