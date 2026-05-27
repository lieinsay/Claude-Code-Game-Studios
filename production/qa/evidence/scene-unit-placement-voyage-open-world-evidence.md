# 航行大场景单位摆放证据

> **Scope**: 航行大场景 (`voyage_open_world_scene`)
> **Date**: 2026-05-27

## 摘要

- `src/scenes/voyage/VoyageOpenWorldScene.tscn` 和 `src/scenes/voyage/VoyageOpenWorldScene.cs` 新增独立航行世界场景资产。
- `HubRuntime` 挂载 `VoyageOpenWorldSceneRuntimeInstance`，并通过 `DebugVoyageOpenWorldAssetEvidence()` 暴露起飞过渡、主动驾驶视角、问题窗口、目的地轮廓、撤退锚点和 UI 证据排除。
- `src/presentation/playable_slice_authored_content.json` 新增 `authored_scenes::voyage_open_world_scene`、8 个场景单位原型和 8 个摆放实例。
- `HubRuntime.DebugScenePhysicsContract("voyage_open_world_scene")` 从作者化原型 / 实例数据构建 `scene_unit_catalog`。

## 证据边界

| 规则 | 结果 |
| --- | --- |
| 航行场景不是航图 UI、HUD、按钮、标签或进度条 | PASS |
| 场景包含起飞过渡和主动驾驶视角 | PASS |
| 场景包含雾、残骸、大鸟和目的地轮廓 | PASS |
| 保留进入 `exploration_mist_island` / 雾灯残骸路径证据 | PASS |
| 不重写 #10 live driving / 存档格式 | PASS_WITH_NOTES |

## 验证

最终命令结果记录在 `.godot-ai/verification/scene/voyage_open_world_scene.verification.md`；本文件保留生产 QA 证据索引。
