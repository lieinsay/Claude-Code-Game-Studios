# 雾灯残骸浮岛场景单位与独立资产证据

> **日期**: 2026-05-27
> **范围**: `mist_lamp_wreck_scene` / runtime `exploration_mist_island`
> **工作流**: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
> **结果**: PASS；当前实现为生产可追踪灰盒，非 release-ready

## 变更摘要

- 新增独立 Godot 场景 `src/scenes/mist/MistLampWreckScene.tscn` 与脚本 `src/scenes/mist/MistLampWreckScene.cs`。
- `HubRuntime` 在探索状态挂载 `MistLampWreckSceneRuntimeInstance`，并通过 `DebugMistLampWreckAssetEvidence()` 暴露世界层、搜索、返航、起飞、边界和 UI-only 证据拒绝字段。
- `src/presentation/playable_slice_authored_content.json` 更新为 `polish-asset-reset-mist-lamp-wreck-v1`，登记 `authored_scenes::mist_lamp_wreck_scene`、8 个雾灯残骸原型和 9 个 `exploration_mist_island` 摆放实例。
- `tests/smoke/session_shell_visual_probe.gd` 验证独立场景挂载、核心节点、返航目标、无岛屿威胁区和 #20 合同。
- `tests/integration/playable-slice/DomainAdapterProgram.cs` 验证 authored scene、prototype / instance 链路、floor `mist_wreck_ground_01` 和 `SceneUnitAuthoringFixture.ValidateScene("exploration_mist_island")`。

## 接受映射

| 验收项 | 证据 |
| --- | --- |
| 雾灯残骸有独立场景资产 | `src/scenes/mist/MistLampWreckScene.tscn`。 |
| 运行时兼容旧探索合同 | `scene_id=mist_lamp_wreck_scene`；`runtime_contract_id=exploration_mist_island`。 |
| 岛屿本体没有威胁区 | `MistLampWreckScene.DebugSceneAssetEvidence().island_has_threat_zone=false`；场景不包含 `MistThreatZone`。 |
| 返航路径不是纯画面切换 | `MistReturnShipHull`、`MistReturnHelmAnchor`、`MistReturnTakeoffTrail` 和 return target 字段记录返回 `initial_island_scene` / `hub_island_dock`。 |
| 作者化单位来自世界 / 可玩层 | `playable_slice_authored_content.json` 中 9 个实例均指向 `MistLampWreckScene.tscn::MistLampWorldLayer/...`。 |
| UI 证据无效 | 独立场景、原型、实例和 smoke 均记录 `ui_evidence_allowed=false`。 |

## 验证命令

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS | 0 warnings / 0 errors. |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 891/891 checks. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | 独立场景、核心节点、debug evidence 和 #20 合同通过；截图因当前 headless 显示驱动跳过。 |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 107 existing warnings / 0 errors. |
| `git diff --check` | PASS | 仅 LF/CRLF working-copy warnings。 |

## 剩余风险

- 当前是生产可追踪灰盒，不声明最终美术或音频完成。
- 当前不重写 #10 live driving 或完整返航飞行玩法，只保留返航起飞路径证据。
- 非 headless 截图仍需后续补证。
