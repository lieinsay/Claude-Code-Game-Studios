# Godot Asset Interview: voyage_open_world_scene

## 访谈结论

- Intent Clarity: resolved. 创建 `voyage_open_world_scene` 独立 Godot 场景资产，证明出航后进入空海航行世界，而不是航图 UI 直接跳转。
- Visual/Interaction Contract Clarity: resolved. 资产必须包含起飞过渡、主动驾驶视角、航标链、浓雾、残骸、大鸟剪影、目的地轮廓和撤退锚点。
- Runtime Scope Clarity: resolved. 本轮不重写 #10 导航状态、实时驾驶后果或存档格式；只做可追踪世界空间资产、运行时挂载和验证证据。
- Brownfield Integration Clarity: resolved. 非破坏性接入 `HubRuntime` 的出航后探索表面，保留现有 `exploration_mist_island` 搜撤路径。
- Evidence Boundary Clarity: resolved. HUD、按钮、标签、航图面板、进度条和调试入口不能作为 production-ready 航行场景证据。

## 决策

- 使用独立 `src/scenes/voyage/VoyageOpenWorldScene.tscn` 和 `VoyageOpenWorldScene.cs`。
- 通过 `DebugVoyageOpenWorldAssetEvidence()` 和 `DebugScenePhysicsContract("voyage_open_world_scene")` 暴露 QA / smoke 证据。
- 在 `playable_slice_authored_content.json` 中新增作者化场景、8 个原型和 8 个摆放实例。
