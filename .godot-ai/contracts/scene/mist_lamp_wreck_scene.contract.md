# Godot Asset Contract: mist_lamp_wreck_scene

## Identity

- Stable ID: `mist_lamp_wreck_scene`
- Runtime contract ID: `exploration_mist_island`
- Display role: 雾灯残骸浮岛，承接航行抵达后的搜索、打捞和返航起飞。
- Return target: `initial_island_scene` / `hub_island_dock`

## Asset Boundary

- Scene path: `src/scenes/mist/MistLampWreckScene.tscn`
- Script path: `src/scenes/mist/MistLampWreckScene.cs`
- Authoring data: `src/presentation/playable_slice_authored_content.json`
- Runtime mount: `HubRuntime` 在探索状态挂载 `MistLampWreckSceneRuntimeInstance`。

## Required World Evidence

- 世界层: `MistLampWorldLayer`
- 玩家出生: `MistLampPlayerStart`
- 岛屿 / 路径: `MistIslandMass`, `MistIslandPath`
- 搜索目标: `MistLampWreckBody`, `MistSearchScanAnchor`
- 返航目标: `MistReturnShipHull`, `MistReturnHelmAnchor`
- 起飞路径: `MistReturnTakeoffTrail`
- 边界: `MistWaterBoundary`

## Non-Goals

- 不新增岛屿本体威胁区。
- 不实现完整实时返航驾驶。
- 不重写现有探索、资源、存档或导航领域权威。
- 不把旧探索灰盒、HUD、按钮、标签或调试入口计入生产场景证据。

## Acceptance Evidence

- Smoke 验证独立场景挂载、核心节点、debug evidence、UI-only 证据拒绝和 `exploration_mist_island` #20 合同。
- Integration 验证 authored scene、原型 / 实例链路、`mist_wreck_ground_01` floor、场景规格追踪和 `SceneUnitAuthoringFixture.ValidateScene("exploration_mist_island")`。
