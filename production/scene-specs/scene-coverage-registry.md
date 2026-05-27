# 场景覆盖登记表

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-001-scene-spec-template-coverage-registry.md`
> **最后更新**: 2026-05-27
> **登记规则**: 每个可进入场景在被视为 production-ready 前，必须链接场景规格、等价完整来源说明、待补规格缺口，或明确的 #20 豁免。
> **语言规则**: 除路径、代码符号、命令、稳定 ID、状态枚举、ADR/TR 编号等必要内容外，本目录文档必须使用中文。

## 登记规则

- 场景是玩家可进入的世界 / 可玩空间，不是 UI 面板。
- 新增场景登记前必须先有 `production/content-creation-review-gate.md` 要求的人工适合性审查，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`；否则只能登记为 `tracked-gap` 或审查草案，不能进入实现。
- 通过创建适合性审查后，不再要求规格二次人工审核作为实现硬门；人工备注写回规格、独立实现 / 资产边界明确、证据路径命名后即可进入实现。
- 每个独立可进入场景必须有独立实现、独立资产，或一组可整体追踪的场景资产 / 数据 / runtime 文件；不能只散落在旧 Godot 大场景或大脚本中。
- UI/HUD/按钮/标签/调试覆盖层可以辅助证据，但不能满足场景单位、物理单位或可读性证据。
- #19 登记行可以摘要 #20 状态，但 #20 仍是物理单位细节的唯一来源。
- 修复点和市场场景即使尚未完整实现，也必须追踪，因为 #19 要求它们在被视为视觉完成前具备规格。
- `Scene spec status` 表示 #19 场景构成规格状态，不表示运行时功能完成。

## 状态词汇

| 状态 | 含义 |
| --- | --- |
| `covered-by-current-note` | 现有 story/GDD/证据已包含足够材料用于 Story 001 覆盖，但 release gate 前可能仍需抽取为独立规格。 |
| `template-ready` | 可复用模板已存在；具体场景规格仍需起草。 |
| `tracked-gap` | 场景已知且需要，但尚无完整可进入场景规格。 |
| `asset-reset-required` | 旧灰盒 / 作者化数据已撤销，不能作为现存游戏资产证据；必须重新通过 Godot asset workflow 实现。 |
| `exempt-no-physical-units` | 场景没有玩法相关物理单位，并明确说明 #20 不适用。 |
| `spec_drafted` | 独立场景规格已起草；仍可能需要 Codex 规格一致性检查、运行时证据或截图证据。 |
| `implemented` | 已有独立 Godot 场景资产、运行时挂载、作者化数据和自动验证证据；仍可保留非 headless 截图或最终美术 / 音频风险。 |

## 当前可进入场景覆盖

| Scene ID | 玩家可见场景 | 当前进入来源 | 场景规格状态 | #20 物理输入 | 当前证据 / 来源 | 必要下一步 |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_island_scene` | 初始岛屿 | 新游戏开始 / 返回原点 | `implemented` | 独立 Godot 场景 `src/scenes/hub/InitialIslandScene.tscn` 已挂载为 Hub 外部世界空间；`hub_island_dock` 保留为兼容运行时合同 ID。旧 HubRuntime 灰盒、HUD、按钮、标签或调试入口不得作为 production-ready 证据。 | `production/scene-specs/initial-island-scene.md`; `.godot-ai/verification/scene/initial_island_scene.verification.md`; `production/qa/evidence/scene-unit-placement-initial-island-evidence.md` | 非 headless 截图和部分子单位 dedicated specs 仍是后续风险。 |
| `ship_interior_layered` | 云织号船内分层水平场景 | 从初始岛屿登船 / 从航行返回 | `implemented` | 独立 Godot 场景 `src/scenes/ship/ShipInteriorLayeredScene.tscn` 已挂载为船内世界空间；`hub_ship_interior` 保留为兼容运行时合同 ID。旧 HubRuntime 船内灰盒、HUD、按钮、标签或调试入口不得作为 production-ready 证据。 | `production/scene-specs/ship-interior-layered-scene.md`; `.godot-ai/verification/scene/ship_interior_layered.verification.md`; `src/presentation/playable_slice_authored_content.json` | 非 headless 截图、最终美术 / 音频和更多子单位 dedicated specs 仍是后续风险。 |
| `voyage_open_world_scene` | 航行大场景 | 从初始岛屿前往 demo 目的地 | `implemented` | 独立 Godot 场景 `src/scenes/voyage/VoyageOpenWorldScene.tscn` 已作为出航后的前置航行世界空间挂载；`HubRuntime.DebugScenePhysicsContract("voyage_open_world_scene")` 暴露伪 3D 航行、航线边界、风险物、云 / 雾特殊表面、撤退恢复和作者化单位合同。旧航图 UI、进度条、HUD、按钮、标签或调试入口不得作为 production-ready 航行场景证据。 | `production/scene-specs/voyage-open-world-scene.md`; `.godot-ai/verification/scene/voyage_open_world_scene.verification.md`; `production/qa/evidence/scene-unit-placement-voyage-open-world-evidence.md`; `design/gdd/navigation-route-risk.md`; `design/gdd/scene-composition-system.md` | 当前接入不重写 #10 live driving / 存档格式；非 headless 截图、最终美术 / 音频和完整 60-75s 驾驶玩法仍是后续风险。 |
| `mist_lamp_wreck_scene` | 雾灯残骸浮岛 | 从 `voyage_open_world_scene` 抵达 | `implemented` | 独立 Godot 场景 `src/scenes/mist/MistLampWreckScene.tscn` 已作为航行抵达后的探索世界空间挂载；`exploration_mist_island` 保留为兼容运行时合同 ID。作者化单位位于 `playable_slice_authored_content.json`，floor 为 `mist_wreck_ground_01`。旧探索灰盒、HUD、按钮、标签或调试入口不得作为 production-ready 证据。 | `production/scene-specs/mist-lamp-wreck-scene.md`; `.godot-ai/verification/scene/mist_lamp_wreck_scene.verification.md`; `production/qa/evidence/scene-unit-placement-mist-lamp-wreck-evidence.md`; `design/gdd/exploration-scavenge-scenario.md`; `design/gdd/scene-composition-system.md` | 当前是生产可追踪灰盒；非 headless 截图、最终美术 / 音频和完整返航飞行玩法仍是后续风险。 |
| `ochre_island_scene` | 赭石岛 | 从 `voyage_open_world_scene` 抵达 | `spec_drafted` | #20 合同位于 `HubRuntime.DebugScenePhysicsContract("ochre_island_scene")`；独立 Godot 资产、条带状铁矿原型、6 个作者化场景单位和 Debug build `OchreDebugButton` 开发入口已链接 | `production/scene-specs/ochre-island-scene.md`; `production/unit-specs/fixed-scene-objects/banded-iron-ore.md`; `production/qa/evidence/ochre-island-godot-asset-execution-evidence.md`; `design/gdd/resources-goods-capacity.md`; `design/gdd/scene-composition-system.md` | 补矿脉采集奖励 / 正式返航 domain 写入、截图刷新和 release packet；真实 playable route 等入口闭环稳定后再接。 |
| `old_market_edge_scene` | 旧集市边缘 | 后续市场内容候选 | `tracked-gap` | implementation readiness 前需要 #20 合同，因为市场边缘、摊位、NPC、货物和可通行性都是物理场景单位 | `design/gdd/port-village-market.md`; `design/gdd/scene-composition-system.md` | 当前 demo 第二岛屿改为 `ochre_island_scene`；旧集市保留为后续市场缺口，不进入本轮 runtime 替换。 |
| `repair_node_scene` | 世界修复 / 解锁点 | 未来修复地点入口 | `tracked-gap` | implementation readiness 前需要 #20 合同，因为修复点会包含玩法相关物理单位 | `design/gdd/world-repair-unlock.md`; `design/gdd/scene-composition-system.md` | 在任何修复场景被视为视觉完成前起草规格；除非明确加入，否则不属于当前 demo 可读性队列。 |

## Godot 旧运行时删除规则

门禁出现前已经存在的错误 Godot runtime 设计不能被当作规格保留，也不能补写 legacy 规格。触碰 `src/scenes/ShellUi.tscn`、`src/scenes/HubRuntime.tscn` 或 `src/scenes/HubRuntime.cs` 时，必须先识别不合规旧节点；删除前向用户确认，或用已经通过人工适合性审查且具备独立实现 / 资产边界的 scene/ui/unit 设计替换。

正确替换入口只能是 `production/content-creation-review-gate.md`、通过 `production/ui-specs/ui-spec-template.md` 起草的独立 UI 规格、`production/unit-specs/fixed-scene-objects/docked-airship-entity.md`、`production/unit-specs/dynamic-entities/player-controlled-entity.md`、对应独立 Godot / 资产边界和 `src/presentation/playable_slice_authored_content.json`；旧 runtime 节点存在本身不能作为创建或验收证据。

## 非场景 / UI 表面

这些表面可以出现在玩家流程中，但不能计入场景单位或物理验收证据。

| 表面 | 分类 | 证据规则 |
| --- | --- | --- |
| HUD 状态面板 | UI 辅助 | 可摘要资源、船体、航线或威胁状态；不能证明场景身份或物理单位就绪。 |
| 航图按钮 / 航线按钮 | UI 控件 | 可确认航线选择；不能替代舵台 / 桌面世界锚点。 |
| 保存 / 读取 / 删除按钮 | UI 控件 | 仅是持久化入口；不是场景单位。 |
| 调试标签 / smoke-only hook | 调试证据 | 可帮助自动断言；不能满足世界场景可读性或场景单位证据。 |
| 新手引导提示文字 | UI 辅助 | 可引导第一轮；不能替代可见世界锚点。 |

## Story 001 验收映射

| 验收项 | 覆盖方式 |
| --- | --- |
| 新的可进入场景在 production readiness 前需要场景规格 | `production/scene-specs/scene-spec-template.md` 定义可复用必需结构和清单。本登记表要求每行链接规格、等价说明、追踪缺口或明确豁免。 |
| 2D 场景规格包含或链接 #20 Scene Physics Contract | 登记表包含 `#20 物理输入` 列，并为初始岛屿、分层船内、航行大场景、雾灯残骸、赭石岛记录运行时合同链接或缺口；旧集市边缘保留为后续缺口。 |
| 无物理单位场景必须明确说明 #20 不适用 | 模板包含豁免字段；当前 demo 没有静默豁免行。除非后续提升为物理场景，否则航图桌面被视为船内支持表面。 |
| 当前 demo 场景在 release readiness 前需要规格 | `initial_island_scene`, `ship_interior_layered`, `voyage_open_world_scene`, `mist_lamp_wreck_scene`, `ochre_island_scene` 均需要独立规格或明确映射来源说明；前四个已完成本轮独立资产接入。 |
| 未来修复场景在视觉完成前需要规格 | `repair_node_scene` 仍是未来追踪缺口，视觉完成声明前必须处理。 |

## 后续队列

1. Story 002 应把这些登记状态转换为具体完整性 / 证据门禁，不重新定义 #20 物理细节。
2. Story 003 应强化登记行和 release 证据中的 UI-vs-scene 证据拒绝规则。
3. 实现后反馈不进入登记门禁；如用户提出修改，统一走 `directed-content-modification`。

## Release Handoff 状态

Release handoff 输入位于 `production/scene-specs/scene-release-gate-handoff.md`。当前状态为 `BLOCKED_FOR_RELEASE`，直到自动证据、截图证据、#20 合同、P0 缺口处理和修复 / 市场追踪缺口被解决或豁免。
