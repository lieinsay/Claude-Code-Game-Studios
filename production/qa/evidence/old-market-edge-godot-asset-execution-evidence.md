# 旧集市边缘 Godot 资产执行证据

> **日期**: 2026-05-27
> **范围**: `old_market_edge_scene`
> **流程**: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
> **结果**: PASS；本轮目标是 future market asset-gate，不暴露 `route.market`

## 产物

| 类型 | 路径 | 状态 |
| --- | --- | --- |
| 场景资产 | `src/scenes/market/OldMarketEdgeScene.tscn` | 已创建 |
| 场景脚本 | `src/scenes/market/OldMarketEdgeScene.cs` | 已创建 |
| 场景规格 | `production/scene-specs/old-market-edge-scene.md` | 已创建 |
| 作者化数据 | `src/presentation/playable_slice_authored_content.json` | 已接入 `authored_scenes::old_market_edge_scene` 和 7 个摆放实例 |
| 运行时合同 | `HubRuntime.DebugScenePhysicsContract("old_market_edge_scene")` | 已接入 |
| Godot AI 合同 | `.godot-ai/contracts/scene/old_market_edge_scene.contract.md` | 已创建 |
| Godot AI 审查 | `.godot-ai/reviews/scene/old_market_edge_scene.review.md` | `pass-with-scope` |

## 验收映射

| 验收点 | 证据 |
| --- | --- |
| 旧集市边缘具有独立 Godot 资产边界 | `src/scenes/market/OldMarketEdgeScene.tscn` |
| 市场不是 UI-only 证据 | 世界层含 `MarketPlazaGround`、`GeneralStallBody`、`ClosedStallBody`、`GeneralStallAnchor`、`MarketNoticeBoard`、`MarketCloudSeaBoundary` |
| 当前不恢复旧 market 航图按钮 | `S4_chart` 仍不暴露 `RouteMarketButton` / `ChartRouteMarketLine` |
| 领域权威边界 | 场景脚本只暴露 `DebugSceneAssetEvidence()`，不写 Resources、Settlement 或 Persistence |
| #20 运行时合同 | `HubRuntime.DebugScenePhysicsContract("old_market_edge_scene")` |

## 验证命令

| 命令 / 工具 | 结果 |
| --- | --- |
| Godot AI MCP `scene_open` + `scene_get_hierarchy` for `OldMarketEdgeScene.tscn` | PASS；Godot `4.6.2-stable`，打开 `res://src/scenes/market/OldMarketEdgeScene.tscn`，层级返回 17 个节点 |
| Godot AI MCP `node_get_properties("/OldMarketEdgeScene/WorldLayer/GeneralStallAnchor")` | PASS；`Area2D`，位置 `(370, 512)`，`monitoring=true` |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS；0 warnings / 0 errors |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS；1085/1085 |
| `dotnet run --project tests/integration/session/ShellUiTest.csproj` | PASS；18/18 |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS；旧集市 #20 合同、7 个场景单位、UI 证据排除、`route.market` 未暴露均通过 |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS；106 existing warnings / 0 errors |
| `git diff --check` | PASS；仅报告既有 LF/CRLF 提示 |

## 剩余风险

- 本轮不声明完整市场可玩闭环；`route.market`、SettlementManager 世界交互、`S9_market` 购买 UI、NPC 表现、截图包和最终美术 / 音频仍是后续工作。
- 当前旧集市不属于当前 demo 第二岛屿，不能替代 `route.ochre`。
