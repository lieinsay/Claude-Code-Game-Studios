# Godot Asset Execution Plan: mist_lamp_wreck_scene

## Scope

将 `production/scene-specs/mist-lamp-wreck-scene.md` 对应的雾灯残骸浮岛做成独立 Godot 场景资产，并接入当前 playable slice。

## Steps

1. 创建 `src/scenes/mist/MistLampWreckScene.tscn` 与 `MistLampWreckScene.cs`。
2. 在场景内放置岛屿、路径、残骸、搜索锚点、返航船、返航舵点、起飞尾迹和雾海边界。
3. 在 `HubRuntime` 中挂载 `MistLampWreckSceneRuntimeInstance`，并暴露 `DebugMistLampWreckAssetEvidence()`。
4. 更新 `playable_slice_authored_content.json`，登记 scene、prototype 和 `exploration_mist_island` 实例。
5. 更新 integration 与 Godot smoke，验证独立资产证据、作者化单位、UI-only 证据拒绝和返航路径。
6. 更新生产文档、`.godot-ai` 证据和 session state。
7. 跑完整验证链并提交推送。

## Guardrails

- 不删除旧运行时节点，除非另行取得明确确认。
- 不把旧灰盒、HUD、按钮、标签或调试入口列为 production-ready 证据。
- 不新增岛屿本体威胁区。
