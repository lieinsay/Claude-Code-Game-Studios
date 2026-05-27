# 初始岛屿场景单位摆放证据

> **日期**: 2026-05-27
> **范围**: 初始岛屿场景 (`initial_island_scene` / runtime `hub_island_dock`)
> **来源计划**: `.omx/plans/prd-scene-unit-placement-taxonomy.md`
> **结果**: PASS，已刷新为独立 Godot 场景资产证据；仍保留最终视觉证明、非 headless 截图和 P0 资产风险

## 变更内容

本证据覆盖 `initial_island_scene` 迁移到独立 Godot 场景资产和作者化场景单位数据的切片：

- `src/scenes/hub/InitialIslandScene.tscn` 和 `src/scenes/hub/InitialIslandScene.cs` 新增独立场景资产。
- `HubRuntime` 挂载 `InitialIslandSceneRuntimeInstance`，并通过 `DebugInitialIslandAssetEvidence()` 暴露独立场景、登船目标和 UI 证据排除。
- `src/presentation/playable_slice_authored_content.json` 新增 `authored_scenes::initial_island_scene`，并刷新 `hub_island_dock` 可复用单位原型和摆放实例。
- `HubRuntime.DebugScenePhysicsContract("hub_island_dock")` 从作者化原型 / 实例数据构建 `scene_unit_catalog`。
- 集成测试和 Godot smoke 检查覆盖 `hub_island_dock` 作者化单位链路、独立场景挂载、登船进入 `ship_interior_layered` 和 UI-only 证据排除。

## 验证命令

| 命令 | 结果 | 备注 |
| --- | --- | --- |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS | 0 warnings / 0 errors。 |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 479/479 checks，通过 `authored_scenes::initial_island_scene`、`hub_island_dock` 原型、实例、场景规格、地面层和 `SceneUnitAuthoringFixture.ValidateScene("hub_island_dock")` 验证。 |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | 验证 `InitialIslandSceneRuntimeInstance`、`DebugInitialIslandAssetEvidence()`、`hub_island_dock` 作者化数据、登船到 `ship_interior_layered`、地面层和 UI 证据排除。当前显示驱动下截图步骤按既有逻辑跳过。 |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 0 errors，107 个既有测试 / Godot source-generator 警告。 |
| `git diff --check` | PASS | 只有 LF/CRLF 提示。 |

## 验收映射

| 验收点 | 证据 |
| --- | --- |
| 初始岛屿有独立规格和独立 Godot 场景 | `production/scene-specs/initial-island-scene.md`; `src/scenes/hub/InitialIslandScene.tscn`; `src/scenes/hub/InitialIslandScene.cs`。 |
| 既有运行时场景映射到设计身份 | 覆盖登记把 `initial_island_scene` 映射到 runtime `hub_island_dock`；规格文件记录两个 ID。 |
| 单位原型可复用并有分类 | 新增 / 刷新 `scene_unit.prototype.hub_island_main_mass`、`hub_dock_plank_walkway`、`hub_docked_ship_hull`、`hub_boarding_ramp`、`hub_airship_envelope`、`hub_waterline` 和复用 `player_marker`。 |
| 摆放实例引用原型 | 新增 `scene_unit.instance.hub_island_dock.*` 记录，并链接 `production/scene-specs/initial-island-scene.md`。 |
| 运行时读取同一份作者化来源 | `HubRuntime` 将 `hub_island_dock` 路由到 `BuildAuthoredSceneUnitCatalog`。 |
| Gate 能发现无效链路 | 集成测试检查原型 ID、允许场景、场景规格引用、地面层和场景作者化校验。 |
| UI 证据仍不算世界单位证据 | Smoke 检查要求独立场景和单位均 `source_layer == world_playable_scene` 且 `ui_evidence_allowed == false`。 |

## 剩余风险

- 这次迁移证明独立 Godot 场景、数据链路和灰盒合同，不等于最终美术、音频或非 headless 截图审核已经完成。
- `hub_dock_plank_walkway`、`hub_boarding_ramp` 等部分初始岛屿子单位仍使用 `pending_spec_replacement`，后续可补 dedicated unit specs。
- 若用户体验后提出调整，按 `directed-content-modification` 写回 `production/scene-specs/initial-island-scene.md` 并同步实现。

## 建议下一步

补初始岛屿 release handoff 截图包和规格一致性检查，或继续迁移 `voyage_open_world_scene`。
