# Godot Asset Context: voyage_open_world_scene

- Date: 2026-05-27
- Workflow: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
- Target spec: `production/scene-specs/voyage-open-world-scene.md`
- Target scene ID: `voyage_open_world_scene`
- Required boundary: 独立航行世界场景，不能由航图 UI、HUD、按钮、标签、进度条或旧调试入口替代。

## 当前上下文

- `initial_island_scene` 和 `ship_interior_layered` 已完成独立 Godot 场景资产，并通过登船路径连接。
- 当前出航流程在 `HubRuntime.OnDepartPressed()` 后进入 `exploration` 表面；#10 导航和存档仍使用既有 `route.mist` / `exploration_mist_island` 合同。
- 本轮目标是补上 `voyage_open_world_scene` 作为出航后、抵达雾灯残骸前的前置世界空间证据，同时不重写 #10 live driving 或存档格式。

## 相关文件

- `src/scenes/HubRuntime.cs`
- `src/presentation/playable_slice_authored_content.json`
- `tests/smoke/session_shell_visual_probe.gd`
- `tests/integration/playable-slice/DomainAdapterProgram.cs`
- `production/scene-specs/voyage-open-world-scene.md`
- `production/scene-specs/scene-coverage-registry.md`
