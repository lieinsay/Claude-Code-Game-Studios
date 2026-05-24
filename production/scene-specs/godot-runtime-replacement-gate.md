# Godot 旧运行时设计删除与替换门禁

> **状态**: `invalid_legacy_runtime_design`
> **适用范围**: `src/scenes/ShellUi.tscn`, `src/scenes/HubRuntime.tscn`, `src/scenes/HubRuntime.cs`
> **结论**: 这些内容是在生产规格门禁出现前创建的旧且错误设计，不能作为规格保留，必须 `delete_or_replace`。
> **最后更新**: 2026-05-24

## 门禁目的

本文件不是场景规格、UI 规格或单位规格。它只用于阻止旧 Godot runtime 设计被误登记为合规内容。

任何后续工作如果触碰这些范围，必须先删除旧实现，或用已经通过人工适合性审查的正确设计替换。不得把旧节点补写成“规格文档”来绕过 `production/content-creation-review-gate.md`。

## 禁止保留的旧设计

| 旧实现位置 | 旧设计问题 | 处理要求 |
| --- | --- | --- |
| `src/scenes/ShellUi.tscn` | `LoadingPanel`、`EntryPanel`、`AudioActivationPanel`、`EphemeralWarningPanel`、`ResumePanel`、`RecoveryPanel`、`FatalPanel` 是旧 shell UI 面板集合，创建时没有独立 UI 规格和人工适合性审查。 | 从玩家路径删除，或替换为 `production/ui-specs/runtime-ui-surface-registry.md` 及后续独立 UI 规格覆盖的表面。 |
| `src/scenes/HubRuntime.tscn` | `HubCanvas`、`Header`、`Deck`、`ChartPanel`、`ExplorationPanel` 和站点 / 状态 / 按钮面板把场景推进、航图、探索反馈压成 UI 面板，不能证明真实场景空间。 | 删除面板优先的灰盒布局；替换为场景规格驱动的可进入空间和 UIManager 规格表面。 |
| `src/scenes/HubRuntime.cs` | 通过代码临时生成大量世界节点、UI 标签和交互点，绕过 `scene_unit.prototype.*`、场景规格和单位规格。 | 删除手写节点生成路径；替换为 `src/presentation/playable_slice_authored_content.json` 或等价作者化数据源驱动的场景 / 单位实例。 |

## 明确无效的旧节点

这些节点没有规范文档支持，也不应补成 legacy 规格：

| 旧节点 | 当前问题 | 允许继续的唯一方式 |
| --- | --- | --- |
| `ModuleBenchProp` | 旧船内“模块台”想法未通过当前 demo 内容审查，且没有单位规格。 | 删除；若以后需要模块台，必须重新走人工适合性审查、单位规格和场景摆放规格。 |
| `EngineInteractPoint` | 旧引擎交互入口把未来维修 / 模块玩法提前放进 demo，未通过审查。 | 删除；若以后需要引擎交互，必须由对应系统规格、UI 规格、单位规格共同批准。 |
| `ExtractionCargoProp` | 旧撤离货物道具把探索奖励实体化，但没有当前场景单位规格和状态规则。 | 删除；若以后需要撤离货物，必须从探索 / 资源 / 单位规格重新设计。 |
| `ChartPanel` | 旧航图 UI 面板未作为正确 `full_screen_surface` 独立审核。 | 替换为经审查的航图 UI 规格和世界锚点流程。 |
| `ExplorationPanel` | 旧探索 UI 面板把搜索 / 返回行为 UI 化，不能替代雾灯残骸场景。 | 替换为 `mist_lamp_wreck_scene` 和其单位 / HUD 规格支持的流程。 |

## 正确替换入口

后续替换必须落到这些规格或数据源，而不是把旧 Godot 节点原样登记：

| 类型 | 正确入口 |
| --- | --- |
| 场景规格 | `production/scene-specs/initial-island-scene.md`, `production/scene-specs/ship-interior-layered-scene.md`, `production/scene-specs/voyage-open-world-scene.md`, `production/scene-specs/mist-lamp-wreck-scene.md` |
| UI 规格 | `production/ui-specs/runtime-ui-surface-registry.md`，以及后续按 `production/ui-specs/ui-spec-template.md` 起草并通过人工审查的独立 UI 规格 |
| 固定单位规格 | `production/unit-specs/fixed-scene-objects/authored-playable-slice-units.md` |
| 动态实体规格 | `production/unit-specs/dynamic-entities/authored-playable-slice-entities.md` |
| 作者化数据 | `src/presentation/playable_slice_authored_content.json` |
| 创建审查 | `production/content-creation-review-gate.md` |

## 工作规则

- 旧 runtime 节点可以作为删除 / 替换清单证据，但不能作为 scene/ui/unit 规格存在。
- 新增或重建任何场景、UI、单位前，必须先有人类审查其是否适合创建。
- 如果需要保留某个旧概念，必须把它当作全新内容重新审查；不得使用“它已经在 Godot 里存在”作为理由。
- `old_market_edge_scene` 仍是 `tracked-gap`，不能因为 `RouteMarketButton` 或旧航线按钮存在而进入实现。
- 所有替换工作完成前，release readiness 不能声明这些旧 runtime 表面或节点为合规。

## 验收判断

替换故事只有同时满足以下条件，才能关闭本门禁：

- `ShellUi.tscn` 中旧 shell 面板不再是玩家流程入口，或全部被已审查 UI 规格替换。
- `HubRuntime.tscn` 中 `ChartPanel`、`ExplorationPanel` 和面板优先布局不再承担场景完成证明。
- `HubRuntime.cs` 不再手写创建 `ModuleBenchProp`、`EngineInteractPoint`、`ExtractionCargoProp` 等无规格节点。
- 当前 demo 场景由场景规格、单位规格、UI 规格和作者化数据共同驱动。
- 回归测试能证明旧节点没有被当作规格保留。
