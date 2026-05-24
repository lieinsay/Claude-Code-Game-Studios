# 场景构成实现后反馈提示表

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **最后更新**: 2026-05-25
> **目的**: 在真实实现可体验后，把用户反馈整理成可修改需求。此文件不是二次审核门，也不产生 release 判决。

## 使用规则

这份表只在用户体验真实实现后辅助记录问题、偏好和修改方向。创建适合性审查已经通过的 scene/UI/unit，不需要再等待人工二审才能进入实现。

若用户提出修改，使用 `directed-content-modification`：

- 明确目标 scene/UI/unit。
- 修改对应规格文档。
- 修改对应 Godot 实现、资产或资产组。
- 更新证据与 release handoff。

发布门禁仍由自动证据、Codex 规格一致性检查、#20 合同、截图证据、P0 资产缺口和 waiver 记录决定。

## 反馈上下文

| 字段 | 必填值 |
| --- | --- |
| Scene ID |  |
| 玩家可见场景名 |  |
| 测试的 build 或 commit |  |
| 测试的 runtime path |  |
| 自动化证据链接 |  |
| Codex 规格一致性结果 |  |
| 反馈提出者 |  |
| 反馈日期 |  |
| 截图 / 录屏路径（如有） |  |

## 体验反馈问题

这些问题用于定位修改点，不是判决项。

| 问题 | 期望 | 反馈 / 修改点 |
| --- | --- | --- |
| 我在哪里？ | 玩家能快速读出地点身份和氛围。 |  |
| 我在这里能做什么？ | 核心行动能从 world/playable anchors 读出，而不只靠 UI。 |  |
| 我如何离开或继续？ | 出口、返回或继续路径可见，或能通过场景锚点发现。 |  |
| 什么发生了变化？ | 相关状态变化能在 world/playable scene 中看见，或由反馈清楚支持。 |  |
| UI/HUD 是否只是辅助？ | UI 不隐藏或替代世界身份。 |  |
| 场景是否符合预期幻想？ | 缺失幻想、需求偏差、不可接受流程或新增诉求都被记录为修改需求。 |  |

## 定向修改记录

| Scene ID | 修改目标 | 文档更新 | 实现更新 | Owner | Follow-up story / commit |
| --- | --- | --- | --- | --- | --- |
|  | scene / UI / unit / asset group |  |  |  |  |

## Release Handoff 提醒

本文件不决定 release handoff。场景进入 release handoff 仍需满足：

- Codex 规格一致性检查无 blocker。
- 独立实现、独立资产或资产组边界明确。
- #20 Scene Physics Contract 覆盖或有明确豁免。
- 自动 smoke / build / 截图证据齐全。
- 没有未处理 P0 scene asset gap。
- 没有用 UI-only evidence 作为场景证明。

## 当前反馈记录索引

每个真实实现后的反馈记录都应有独立文件；本文件保留为模板与索引。

| Scene ID | 独立记录文件 | 当前状态 | 下一步 |
| --- | --- | --- | --- |
| `initial_island_scene` | [scene-readability-initial-island.md](scene-readability-initial-island.md) | `feedback-template` | 实现后如有调整诉求，走 `directed-content-modification`。 |
| `ship_interior_layered` | [scene-readability-ship-interior-layered.md](scene-readability-ship-interior-layered.md) | `feedback-template` | 实现后如有调整诉求，走 `directed-content-modification`。 |
| `voyage_open_world_scene` | [scene-readability-voyage-open-world.md](scene-readability-voyage-open-world.md) | `feedback-template` | 先补 #20 合同、运行时证据和规格一致性检查。 |
| `mist_lamp_wreck_scene` | [scene-readability-mist-lamp-wreck.md](scene-readability-mist-lamp-wreck.md) | `feedback-template` | 实现后如有调整诉求，走 `directed-content-modification`。 |
| `ochre_island_scene` | [scene-readability-ochre-island.md](scene-readability-ochre-island.md) | `feedback-template` | 先补 #20 合同、独立实现 / 资产边界、作者化数据和运行时证据。 |
