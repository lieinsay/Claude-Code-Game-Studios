# 初始岛屿场景单位摆放证据

> **日期**: 2026-05-24
> **范围**: 初始岛屿场景 (`initial_island_scene` / runtime `hub_island_dock`)
> **来源计划**: `.omx/plans/prd-scene-unit-placement-taxonomy.md`
> **结果**: PASS，仍保留最终视觉证明、截图和 P0 资产风险

## 变更内容

本证据覆盖 `initial_island_scene` 迁移到作者化场景单位数据的切片：

- `production/scene-specs/initial-island-scene.md` 新增初始岛屿独立场景规格，并把运行时合同映射到 `hub_island_dock`。
- `src/presentation/playable_slice_authored_content.json` 新增初始岛屿可复用单位原型和摆放实例。
- `HubRuntime.DebugScenePhysicsContract("hub_island_dock")` 现在从作者化原型 / 实例数据构建 `scene_unit_catalog`，不再依赖独立硬编码目录分支。
- 集成测试和 Godot smoke 检查新增 `hub_island_dock` 作者化单位链路验证。
- `production/scene-specs/scene-coverage-registry.md`、`scene-completeness-gate.md` 和 `scene-release-gate-handoff.md` 已把初始岛屿状态推进到待 release evidence 刷新。

## 验证命令

| 命令 | 结果 | 备注 |
| --- | --- | --- |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 731/731 checks，通过 `hub_island_dock` 原型、实例、场景规格、地面层和 `SceneUnitAuthoringFixture.ValidateScene("hub_island_dock")` 验证。 |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | 验证 `hub_island_dock` 作者化数据、原型 / 实例链路、作者化内容来源、空诊断、场景规格追踪、Godot 摆放引用、地面层和 UI 证据排除。当前显示驱动下截图步骤按既有逻辑跳过。 |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 0 errors，5 个既有测试警告。 |
| `git diff --check` | PASS | 只有 LF/CRLF 提示。 |

## 验收映射

| 验收点 | 证据 |
| --- | --- |
| 初始岛屿有独立规格 | `production/scene-specs/initial-island-scene.md`。 |
| 既有运行时场景映射到设计身份 | 覆盖登记把 `initial_island_scene` 映射到 runtime `hub_island_dock`；规格文件记录两个 ID。 |
| 单位原型可复用并有分类 | 新增 `scene_unit.prototype.hub_island_main_mass`、`hub_dock_plank_walkway`、`hub_docked_ship_hull`、`hub_boarding_ramp`、`hub_airship_envelope`、`hub_waterline` 和复用 `player_marker`。 |
| 摆放实例引用原型 | 新增 `scene_unit.instance.hub_island_dock.*` 记录，并链接 `production/scene-specs/initial-island-scene.md`。 |
| 运行时读取同一份作者化来源 | `HubRuntime` 将 `hub_island_dock` 路由到 `BuildAuthoredSceneUnitCatalog`。 |
| Gate 能发现无效链路 | 集成测试检查原型 ID、允许场景、场景规格引用、地面层和场景作者化校验。 |
| UI 证据仍不算世界单位证据 | Smoke 检查要求 `source_layer == world_playable_scene` 且 `ui_evidence_allowed == false`。 |

## 剩余风险

- 这次迁移证明数据链路和灰盒合同，不等于最终美术、音频或截图审核已经完成。
- Godot node path 是稳定作者化引用，但本切片仍没有逐个反查 `.tscn` 序列化节点。
- 若用户体验后提出调整，按 `directed-content-modification` 写回 `production/scene-specs/initial-island-scene.md` 并同步实现。

## 建议下一步

补初始岛屿 release handoff 截图包和规格一致性检查，或继续迁移 `voyage_open_world_scene`。
