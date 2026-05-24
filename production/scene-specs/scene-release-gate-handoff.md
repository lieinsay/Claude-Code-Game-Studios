# 场景 Release Gate 交接包

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **最后更新**: 2026-05-24
> **目的**: 提供场景就绪度的 release checklist / gate-check 输入。
> **语言规则**: 除路径、代码符号、命令、稳定 ID、状态枚举、ADR/TR 编号等必要内容外，本目录文档必须使用中文。

## 交接规则

任何当前场景都不能被标记为 release-ready，除非自动 / Codex 证据与用户可读性审核都通过，或获得明确用户 waiver。

```text
release_handoff_ready =
    scene_complete
    AND ui_boundary_passed
    AND codex_review_passed
    AND (user_review_passed OR user_waiver_recorded)
    AND no_unresolved_p0_scene_blockers
```

`Codex PASS` 是必要条件，但不足以单独通过。用户可以因为幻想缺失、需求缺失、身份不清、玩家流程不理想、UI 过强，或新需求需要写回场景规格而阻塞场景。

## 必需交接包

每个场景 release handoff 必须包含：

- 场景规格或等价来源说明
- 独立 Godot 场景、独立资产组、作者化数据或 runtime 边界说明
- #20 Scene Physics Contract 链接，或明确的无物理单位豁免
- 场景完整性门禁结果
- 场景 vs UI 边界结果
- 自动 smoke / build 命令和结果
- 用于视觉声明的截图或捕获证据
- Codex 审核 verdict 和 blocker 列表
- 用户可读性清单 verdict 和 blocker 列表
- P0 资产缺口状态
- waiver 表（如有）
- release 决策：`READY`、`READY_WITH_USER_WAIVER` 或 `BLOCKED`

## 当前交接快照

| Scene ID | Codex / 自动状态 | 用户审核状态 | Release handoff 状态 | 原因 |
| --- | --- | --- | --- | --- |
| `initial_island_scene` | 初始岛屿作者化单位链路已实现；仍需截图刷新和 release packet。 | `PENDING` | `BLOCKED` | 缺用户可读性 verdict；P0 资产缺口需要进入交接包。 |
| `ship_interior_layered` | 船内作者化单位链路已实现并有自动证据；仍需截图刷新和 release packet。 | `PENDING` | `BLOCKED` | 缺用户可读性 verdict；水平分层、剖切、P0 资产缺口需要进入交接包。 |
| `voyage_open_world_scene` | 独立规格已起草；#20 合同、运行时证据、Codex 审核和用户可读性 verdict 仍缺失。 | `NOT_READY` | `BLOCKED` | 场景设计存在，但实现 / 证据 / 可读性门禁尚未完成。 |
| `mist_lamp_wreck_scene` | 雾灯残骸浮岛作者化单位链路已实现并有自动证据；仍需截图刷新和 release packet。 | `PENDING` | `BLOCKED` | 缺用户可读性 verdict；需确认目的地岛身份和残骸搜索空间是否符合预期。 |
| `ochre_island_scene` | 赭石岛已通过创建适合性审查并起草规格；仍无 #20 合同、独立实现 / 资产边界、作者化数据或运行时证据。 | `PENDING` | `BLOCKED` | 尚无 #20 合同、用户可读性 verdict、作者化单位和运行时证据；规格二次人工审核不是实现硬门。 |
| `old_market_edge_scene` | `TRACKED_GAP`，保留为后续市场内容候选，不属于当前 demo 第二岛屿。 | `NOT_READY` | `BLOCKED` | 后续市场阶段再补独立场景规格、#20 合同和用户可读性 verdict。 |

## Waiver 要求

waiver 只有在用户明确记录以下内容时才有效：

- waiver owner
- waiver date
- 被豁免的具体 blocker
- 接受的玩家可见风险
- fallback 证据或灰盒限制
- follow-up owner
- follow-up date 或 next story

waiver 不能让 UI-only 证据计入场景证据。它只能承认某个已知缺口或条件项的风险。

## Release Checklist 输入

在 release checklist 或 gate-check 中使用以下摘要：

- Scene Composition #19: `BLOCKED_FOR_RELEASE`
- 原因: 当前 demo 场景集已修正为初始岛屿、分层船内、航行大场景、雾灯残骸和赭石岛。用户可读性 verdict 待补，赭石岛 #20 / 独立实现边界 / 运行时证据、航行 #20 / 运行时证据仍缺失；旧集市边缘已降为后续市场内容缺口。
- release-ready 声明前必需: 在独立场景规格 / Codex 审核附上后，对每个当前 demo release candidate 运行 `production/playtests/scene-composition-user-readability-checklist.md`，然后解决或明确 waiver blocker。
- 本交接不处理: 修复可读性缺陷、制作最终美术 / 音频、替换全局 release checklist。
