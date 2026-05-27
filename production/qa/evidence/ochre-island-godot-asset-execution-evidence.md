# 赭石岛 Godot 资产执行证据

> **日期**: 2026-05-27
> **范围**: `ochre_island_scene` + `scene_unit.prototype.banded_iron_ore`
> **流程**: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
> **结果**: PASS，仍保留 playable route、截图刷新和 release handoff 风险

## 产物

| 类型 | 路径 | 状态 |
| --- | --- | --- |
| 场景资产 | `src/scenes/ochre/OchreIslandScene.tscn` | 已创建 |
| 场景脚本 | `src/scenes/ochre/OchreIslandScene.cs` | 已创建 |
| 固定单位资产 | `src/scenes/units/BandedIronOre.tscn` | 已创建 |
| 固定单位脚本 | `src/scenes/units/BandedIronOre.cs` | 已创建 |
| 作者化数据 | `src/presentation/playable_slice_authored_content.json` | 已接入 |
| 运行时合同 | `HubRuntime.DebugScenePhysicsContract("ochre_island_scene")` | 已接入 |
| 开发验证入口 | Debug build `OchreDebugButton` -> `HubRuntime.DebugEnterOchreIslandScene()` | 已接入，不替换正式 playable route |
| Godot AI 合同 | `.godot-ai/contracts/composite-feature/ochre_island_resource_slice.contract.md` | 已创建 |
| Godot AI 审查 | `.godot-ai/reviews/composite-feature/ochre_island_resource_slice.review.md` | `pass-with-risks` |
| Godot AI 验证 | `.godot-ai/verification/composite-feature/ochre_island_resource_slice.verification.md` | `pass` |

## Godot AI MCP 证据

Godot AI MCP 会话:

- Session: `claude-code-game-studios@2681`
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
| #20 运行时合同 | `session_shell_visual_probe.gd` 验证 `cloudsea`、`blocking_static`、`soft_overlap`、作者化单位链接和动态行为恢复规则。 |
| 稳定开发入口 | `session_shell_visual_probe.gd` 验证 Debug build 显示 `OchreDebugButton`，点击后可进入赭石岛、矿脉采集状态可切换、返航锚点两步返回 Hub，且 `committed_route` 仍保持 `route.mist`。 |

## 验证命令

| 命令 / 工具 | 结果 |
| --- | --- |
| Godot AI MCP `scene_open` + `scene_get_hierarchy` for `BandedIronOre.tscn` | PASS |
| Godot AI MCP `scene_open` + `scene_get_hierarchy` for `OchreIslandScene.tscn` | PASS |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS，897/897 passing |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS，0 warnings，0 errors |

## 剩余风险

- 当前产物是独立 Godot 资产、#20 运行时合同和 debug 入口闭环，不等于已接入 `voyage_open_world_scene` 或当前 playable route。
- 尚未刷新 release handoff 截图。
- 条带状铁矿的 Resources 奖励写入仍是下一步 runtime 集成任务。

## 建议下一步

补矿脉采集奖励、返航锚点触发的正式 domain 写入、截图证据和 release packet；真实 playable route 等这些闭环稳定后再接。
