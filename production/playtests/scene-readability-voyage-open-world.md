# voyage_open_world_scene 实现后反馈记录

> **父表**: [scene-composition-user-readability-checklist.md](scene-composition-user-readability-checklist.md)
> **当前状态**: `feedback-template`
> **Release handoff**: `BLOCKED_FOR_RELEASE`，原因见 release handoff；本文件不产生发布判决。

## 设计前置说明

该场景合并“初始岛屿 -> 雾灯残骸”和“初始岛屿 -> 赭石岛”两条航道，作为当前 demo 的航行大场景。独立规格见 [voyage-open-world-scene.md](../scene-specs/voyage-open-world-scene.md)；仍需 #20 contract、运行证据和 Codex 规格一致性检查。

核心要求：玩家视角始终与飞船前进方向保持一致；飞船可以拐弯、前进、后退；运动表现应主要来自世界、云层、航标、风险物、远近岛屿轮廓的变化。第一版遭遇战不做主动开火，只做规避、甩脱、干扰、硬抗或撤退。

## 上下文

| 字段 | 填写值 |
| --- | --- |
| Scene ID | `voyage_open_world_scene` |
| 包含航道 | 初始岛屿到雾灯残骸；初始岛屿到赭石岛 |
| 玩家可见场景名 | 航行大场景 |
| 测试的 build 或 commit |  |
| 测试的 runtime path |  |
| 自动化证据链接 |  |
| Codex 规格一致性结果 |  |
| 反馈提出者 |  |
| 反馈日期 |  |
| 截图 / 录屏路径 |  |

## 体验反馈问题

| 问题 | 期望 | 反馈 / 修改点 |
| --- | --- | --- |
| 我在哪里？ | 能快速读出自己正在空海航行，而不是停在地图 UI 或进度条界面。 |  |
| 我在这里能做什么？ | 前进、转向、后退、撤退或继续航行的核心行动能从场景反馈读出。 |  |
| 我如何离开或继续？ | 抵达目的地、返航、撤退或进入下一场景的路径可读。 |  |
| 什么发生了变化？ | 风险、航向、距离、目的地接近、世界层级运动能被看见。 |  |
| 伪 3D 航行成立吗？ | 视角跟随飞船前进方向；世界变化提供运动感；转向/后退不会让玩家迷失。 |  |
| UI/HUD 是否只是辅助？ | UI 不替代航行世界和风险物。 |  |
| 场景是否符合预期幻想？ | 航行是重要玩法，而不是航图确认后的等待条。 |  |

## 定向修改记录

| 修改目标 | 文档更新 | 实现更新 | Owner | Follow-up story / commit |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |
