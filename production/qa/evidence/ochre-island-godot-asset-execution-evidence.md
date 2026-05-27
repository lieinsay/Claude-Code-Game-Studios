# 赭石岛 Godot 资产执行证据

> **日期**: 2026-05-27
> **范围**: `ochre_island_scene` + `scene_unit.prototype.banded_iron_ore`
> **流程**: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
> **结果**: PASS，正式 playable route / Resources / 返航 domain 写入已闭环；仍保留截图刷新和 release handoff 风险

## 产物

| 类型 | 路径 | 状态 |
| --- | --- | --- |
| 场景资产 | `src/scenes/ochre/OchreIslandScene.tscn` | 已创建 |
| 场景脚本 | `src/scenes/ochre/OchreIslandScene.cs` | 已创建 |
| 固定单位资产 | `src/scenes/units/BandedIronOre.tscn` | 已创建 |
| 固定单位脚本 | `src/scenes/units/BandedIronOre.cs` | 已创建 |
| 作者化数据 | `src/presentation/playable_slice_authored_content.json` | 已接入 `authored_scenes::ochre_island_scene`、`route.ochre` 和 6 个场景单位实例 |
| 正式航线入口 | `S4_chart` / `HubRuntime.OnRouteOchrePressed()` | 已接入，选择 `route.ochre` 后进入 `ochre_island` |
| 资源写入 | `PlayableSliceDomainAdapter.HarvestOchreOre()` | 已接入，写入 `resource.banded_iron_ore` carried pool |
| 返航结算 | `PlayableSliceDomainAdapter.ReturnToHub()` | 已接入，返航后 ore 从 carried 结算到 storage |
| 运行时合同 | `HubRuntime.DebugScenePhysicsContract("ochre_island_scene")` | 已接入 |
| 开发验证入口 | Debug build `OchreDebugButton` -> `HubRuntime.DebugEnterOchreIslandScene()` | 已接入，实例化 `src/scenes/ochre/OchreIslandScene.tscn`，不替换正式 playable route |
| Godot AI 合同 | `.godot-ai/contracts/composite-feature/ochre_island_resource_slice.contract.md` | 已创建 |
| Godot AI 审查 | `.godot-ai/reviews/composite-feature/ochre_island_resource_slice.review.md` | `pass-with-risks` |
| Godot AI 验证 | `.godot-ai/verification/composite-feature/ochre_island_formal_route.verification.md` | `pass` |

## Godot AI MCP 证据

Godot AI MCP 会话:

- Session: `claude-code-game-studios@8b92`
- Godot: `4.6.2-stable`
- Project: `D:/Project/MineCraftMod/Claude-Code-Game-Studios/`
- Readiness: `ready`

`BandedIronOre.tscn` hierarchy 验证:

- `/BandedIronOre`
- `/BandedIronOre/OreBodyAvailable`
- `/BandedIronOre/DarkIronBandA`
- `/BandedIronOre/DarkIronBandB`
- `/BandedIronOre/HarvestedStateOverlay`
- `/BandedIronOre/BandedIronOreAnchor`
- `/BandedIronOre/BandedIronOreAnchor/SoftOverlapShape`

`OchreIslandScene.tscn` hierarchy 验证:

- `/OchreIslandScene`
- `/OchreIslandScene/WorldLayer`
- `/OchreIslandScene/WorldLayer/OchreIslandGround`
- `/OchreIslandScene/WorldLayer/WalkPath`
- `/OchreIslandScene/WorldLayer/CloudSeaBoundary`
- `/OchreIslandScene/WorldLayer/RockWallBoundary`
- `/OchreIslandScene/WorldLayer/PlayerSpawn`
- `/OchreIslandScene/WorldLayer/BandedIronOreInstance`
- `/OchreIslandScene/WorldLayer/OchreReturnAnchor`
- `/OchreIslandScene/WorldLayer/OchreReturnAnchor/ReturnSoftOverlapShape`
- `/OchreIslandScene/WorldLayer/ReturnBeaconGreybox`
- `/OchreIslandScene/WorldLayer/HarvestStateMarkers`

补充 MCP 属性证据:

- `node_get_properties("/OchreIslandScene/WorldLayer/BandedIronOreInstance")` PASS
- `BandedIronOreInstance.position = (655, 390)`
- `BandedIronOreInstance.script = res://src/scenes/units/BandedIronOre.cs`
- `BandedIronOreInstance.Harvested = false`

## 验收映射

| 验收点 | 证据 |
| --- | --- |
| 赭石岛具有独立 Godot 资产边界 | `src/scenes/ochre/OchreIslandScene.tscn`。 |
| 条带状铁矿具有可复用固定单位边界 | `src/scenes/units/BandedIronOre.tscn`。 |
| 场景包含矿脉实例 | `BandedIronOreInstance` 位于 `OchreIslandScene/WorldLayer`。 |
| 矿脉不是 UI-only 证据 | 矿脉为 `Node2D` 场景单位，带 `Area2D` + `CollisionShape2D` soft overlap 锚点。 |
| 返航点不是 UI-only 证据 | `OchreReturnAnchor` 为世界层 `Area2D`，带 `ReturnSoftOverlapShape`。 |
| C# 脚本可编译 | `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS，0 errors。 |
| #20 作者化单位链路 | `DomainAdapterTest` 验证 `ochre_island_scene` 的 6 个 scene units、spec path、floor id、Godot node path。 |
| 正式路线入口 | `DomainAdapterTest` 和 smoke 验证 `route.ochre` 选择、`location.ochre-island` destination、Navigation encounter context 和 `ochre_island` runtime screen。 |
| Resources 奖励写入 | `DomainAdapterTest` 和 smoke 验证 `resource.banded_iron_ore` 采集后进入 carried pool，返航后进入 storage。 |
| #20 运行时合同 | `session_shell_visual_probe.gd` 验证 `cloudsea`、`blocking_static`、`soft_overlap`、作者化单位链接和动态行为恢复规则。 |
| 稳定开发入口 | `session_shell_visual_probe.gd` 验证 Debug build 显示 `OchreDebugButton`，点击后可进入由同一个 `OchreIslandScene.tscn` 实例化的赭石岛；该入口不替换已经提交的正式 playable route。 |

## 验证命令

| 命令 / 工具 | 结果 |
| --- | --- |
| Godot AI MCP `scene_open` + `scene_get_hierarchy` for `BandedIronOre.tscn` | PASS |
| Godot AI MCP `scene_open` + `scene_get_hierarchy` for `OchreIslandScene.tscn` | PASS |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS，921/921 passing |
| `dotnet run --project tests/integration/session/ShellUiTest.csproj` | PASS，18/18 checks passed |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS；验证正式 `route.ochre`、赭石岛场景进入、矿点 / 返航点交互、ore carried/storage 结算和 debug 入口不替换正式路线 |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS，0 warnings / 0 errors |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS，106 个既有 Godot source-generator / 测试警告，0 errors |
| `git diff --check` | PASS，仅 LF/CRLF working-copy warnings |

## 剩余风险

- 当前产物已接入正式 `route.ochre` playable route；但仍是生产可追踪灰盒，不声明最终美术 / 音频完成。
- 尚未刷新 release handoff 截图。
- 完整实时航行表现仍沿用当前 fast-forward Navigation 合同，非最终 90-120 秒驾驶体验。

## 建议下一步

补非 headless 截图证据、最终美术 / 音频和完整实时航行表现；如玩家反馈要求改岛形、矿点或返航节奏，走 `directed-content-modification`。
