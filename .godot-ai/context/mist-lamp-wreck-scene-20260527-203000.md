# Godot 资产上下文：mist_lamp_wreck_scene

- 日期: 2026-05-27
- 来源规格: `production/scene-specs/mist-lamp-wreck-scene.md`
- 工作流: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
- 资产 ID: `mist_lamp_wreck_scene`
- 兼容运行时合同 ID: `exploration_mist_island`

## 背景

`mist_lamp_wreck_scene` 是当前 demo 的第一个离岛探索目的地，从 `voyage_open_world_scene` 抵达，并通过返航舵点起飞返回 `initial_island_scene` / `hub_island_dock`。用户备注要求岛屿本体没有威胁；危险只属于前往或返回的航行过程。

## 关键约束

- 不能把旧 `HubRuntime` 探索灰盒、HUD、按钮、标签或调试入口作为 production-ready 场景证据。
- 独立资产必须有可追踪 Godot 场景、脚本、作者化单位和运行时 debug/smoke 证据。
- 保留现有 `exploration_mist_island` runtime ID，避免破坏探索、存档和测试合同。
- 返航证据必须表现为返航船、舵点和起飞路径，而不是纯画面切换。

## 当前实现目标

- 新增 `src/scenes/mist/MistLampWreckScene.tscn`。
- 新增 `src/scenes/mist/MistLampWreckScene.cs`，暴露 `DebugSceneAssetEvidence()`。
- 在 `HubRuntime` 的探索世界层挂载 `MistLampWreckSceneRuntimeInstance`。
- 在 `playable_slice_authored_content.json` 中登记 `authored_scenes::mist_lamp_wreck_scene` 和 9 个 `exploration_mist_island` 物理单位实例。
