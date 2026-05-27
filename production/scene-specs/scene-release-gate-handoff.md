# 场景 Release Gate 交接包

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **最后更新**: 2026-05-24
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
| `initial_island_scene` | 独立 Godot 场景、作者化单位、#20 运行时合同和自动证据已建立。 | `directed-content-modification` | `BLOCKED` | 仍需非 headless 截图、最终美术 / 音频和 release packet；旧 HubRuntime 灰盒不得作为替代证据。 |
| `ship_interior_layered` | 独立 Godot 场景、ChartTable / S4_chart 接入、作者化单位、#20 运行时合同和自动证据已建立。 | `directed-content-modification` | `BLOCKED` | 仍需非 headless 截图、最终美术 / 音频和 release packet；旧船内灰盒不得作为替代证据。 |
| `voyage_open_world_scene` | 独立 Godot 航行世界场景、#20 合同、作者化单位和自动证据已建立。 | `directed-content-modification` | `BLOCKED` | 当前是生产可追踪灰盒和证据接入；完整 #10 live driving、非 headless 截图、最终美术 / 音频和 release packet 仍未完成。 |
| `mist_lamp_wreck_scene` | 旧作者化单位链路已撤销；当前不再算现存游戏资产。 | `directed-content-modification` | `BLOCKED` | 必须重新通过 Godot asset workflow 实现，不能刷新旧 release packet。 |
| `ochre_island_scene` | 赭石岛已通过创建适合性审查，独立 Godot 资产、条带状铁矿固定单位、#20 运行时合同、作者化单位链路和 Debug build 按钮入口已实现；当前是唯一保留的合规游戏资产切片。 | `directed-content-modification` | `BLOCKED` | 仍需正式采集奖励 / 返航 domain 写入、截图刷新和 release packet，但这些排在核心 UI / 单位独立实现之后。 |
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
- 原因: 除 `ochre_island_scene` / `banded_iron_ore` 外，旧 Hub / ship / mist 作者化资产已撤销并视为临时测试资产。当前目标回到内容实现阶段，不进入 release packet 刷新。
- release-ready 声明前必需: 先实现已通过适合性的核心 UI / 单位，尤其是 `chart-full-screen-surface` 和 `chart-table`，再按 Godot asset workflow 重建场景资产、自动证据、截图证据、#20 合同和 P0 资产处理。
- 本交接不处理: 修复可读性缺陷、制作最终美术 / 音频、替换全局 release checklist。
