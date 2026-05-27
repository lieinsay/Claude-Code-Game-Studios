# Godot Asset Contract: voyage_open_world_scene

## Identity

- Stable ID: `voyage_open_world_scene`
- Display role: 航行大场景，连接初始岛屿 / 船内出航与雾灯残骸抵达。
- Runtime contract ID: `voyage_open_world_scene`
- Route evidence: `route.mist` -> `location.mist-short`

## Asset Boundary

- Scene path: `src/scenes/voyage/VoyageOpenWorldScene.tscn`
- Script path: `src/scenes/voyage/VoyageOpenWorldScene.cs`
- Authoring data: `src/presentation/playable_slice_authored_content.json`
- Runtime mount: `HubRuntime` adds `VoyageOpenWorldSceneRuntimeInstance` to the departure / exploration world layer.

## Required World Evidence

- 起飞过渡: `VoyageTakeoffTrail` and receding initial-island silhouette.
- 主动驾驶视角: `VoyageShipBowForeground` and `VoyageCockpitWindowFrame`.
- 航行空间: `VoyageRouteCorridor` and `VoyageBeaconChain`.
- 问题窗口: `VoyageFogBank`, `VoyageWreckageField`, `VoyageBirdSilhouette`.
- 抵达方向: `VoyageDestinationMistLampSilhouette`.
- 失败 / 撤退路径: `VoyageRetreatBeacon`.

## Non-Goals

- 不实现完整 60-75 秒实时驾驶循环。
- 不修改 #10 导航权威、存档格式或探索搜撤结算。
- 不把航图 UI、HUD、按钮、标签、进度条或调试入口计入场景证据。

## Acceptance Evidence

- Smoke verifies the scene mount, core nodes, debug evidence, UI-evidence rejection, and `voyage_open_world_scene` physics contract.
- Integration verifies authored scene, prototype-instance linkage, floor ID `voyage_air_lane_01`, and scene spec traceability.
