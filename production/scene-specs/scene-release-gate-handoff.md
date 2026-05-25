# 场景 Release Gate 交接包

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **最后更新**: 2026-05-25
> **目的**: 提供场景就绪度的 release checklist / gate-check 输入。
> **语言规则**: 除路径、代码符号、命令、稳定 ID、状态枚举、ADR/TR 编号等必要内容外，本目录文档必须使用中文。

## 交接规则

任何当前场景都不能被标记为 release-ready，除非自动证据、Codex 规格一致性检查和必要的 release 证据都通过。实现后用户反馈不再作为二次审核门；反馈只进入 `directed-content-modification`。

```text
release_handoff_ready =
    scene_complete
    AND ui_boundary_passed
    AND codex_review_passed
    AND no_unresolved_p0_scene_blockers
```

`Codex PASS` 是必要条件，用于证明规格与证据一致。用户实现后反馈只记录为后续修改需求，不再产生二次 release 结论。

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
- 实现后反馈记录入口：`directed-content-modification`
- P0 资产缺口状态
- waiver 表（如有）
- release 决策：`READY`、`READY_WITH_USER_WAIVER` 或 `BLOCKED`

## 当前交接快照

| Scene ID | Codex / 自动状态 | 后续反馈入口 | Release handoff 状态 | 原因 |
| --- | --- | --- | --- | --- |
| `initial_island_scene` | 初始岛屿作者化单位链路已实现；仍需截图刷新和 release packet。 | `directed-content-modification` | `BLOCKED` | P0 资产缺口需要进入交接包。 |
| `ship_interior_layered` | 船内作者化单位链路已实现并有自动证据；仍需截图刷新和 release packet。 | `directed-content-modification` | `BLOCKED` | 水平分层、剖切、P0 资产缺口需要进入交接包。 |
| `voyage_open_world_scene` | 航行大场景规格、作者化数据、#20 灰盒运行时合同、主动驾驶视角灰盒和自动证据已通过；headless 截图仍未形成 release 视觉包。 | `directed-content-modification` | `BLOCKED` | 自动证据已补齐，但 release packet 仍缺窗口化截图 / 视觉证明、P0 最终美术 / 音频资产处理或明确 waiver。 |
| `mist_lamp_wreck_scene` | 雾灯残骸浮岛作者化单位链路已实现并有自动证据；仍需截图刷新和 release packet。 | `directed-content-modification` | `BLOCKED` | P0 资产缺口和截图刷新需要进入交接包。 |
| `ochre_island_scene` | 赭石岛规格、作者化数据、#20 灰盒运行时合同、铁矿采集和返航自动证据已通过；headless 截图因显示驱动跳过。 | `directed-content-modification` | `BLOCKED` | 自动证据已补齐，但 release packet 仍缺窗口化截图 / 视觉证明、P0 最终美术 / 音频资产处理或明确 waiver。 |
| `old_market_edge_scene` | `TRACKED_GAP`，保留为后续市场内容候选，不属于当前 demo 第二岛屿。 | `directed-content-modification` | `BLOCKED` | 后续市场阶段再补独立场景规格和 #20 合同。 |

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
- 原因: 当前 demo 场景集已修正为初始岛屿、分层船内、航行大场景、雾灯残骸和赭石岛。赭石岛和航行大场景的灰盒自动证据已补齐；当前场景集的截图证据和 P0 最终资产 / 音频缺口仍未进入 release packet 或 waiver。
- release-ready 声明前必需: 在独立场景规格 / Codex 审核附上后，补齐截图证据、P0 资产缺口处理和 release packet / waiver；下一步重点是窗口化截图 / 视觉证明与 P0 资产 / 音频处理。
- 本交接不处理: 修复可读性缺陷、制作最终美术 / 音频、替换全局 release checklist。
