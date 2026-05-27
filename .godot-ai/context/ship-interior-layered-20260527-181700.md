# Godot Asset Context: ship-interior-layered

## Original Request

继续 Godot asset workflow，处理 `production/scene-specs/ship-interior-layered-scene.md` 对应的 `ship_interior_layered` 独立场景资产。

## Referenced Files

- `production/scene-specs/ship-interior-layered-scene.md`
- `production/unit-specs/fixed-scene-objects/chart-table.md`
- `production/ui-specs/chart-full-screen-surface.md`
- `src/scenes/units/ChartTable.tscn`
- `src/scenes/ui/ChartFullScreenSurface.tscn`
- `src/scenes/HubRuntime.cs`
- `src/presentation/playable_slice_authored_content.json`
- `tests/smoke/session_shell_visual_probe.gd`

## Known Facts

- `ship_interior_layered` 创建适合性为 `APPROVED_WITH_NOTES`。
- 规格要求本轮只实现可支撑航图台、货舱、出口和移动的最小船内空间，长期保留完整多层船舱目标。
- 当前运行时合同 ID 是 `hub_ship_interior`，但旧 `HubRuntime` 灰盒不能继续作为 production-ready 证据。
- `scene_unit.prototype.chart_table` 已有独立 `ChartTable.tscn` / `ChartTable.cs`。
- `S4_chart` 已有独立 `ChartFullScreenSurface.tscn` / `ChartFullScreenSurface.cs`。
- 本任务明确要求在船内场景中正式引用或实例化 ChartTable 与 S4_chart。

## Constraints

- 不删除旧 Godot 节点；如需删除必须另行列出路径并请求确认。
- 独立船内场景不能拥有 Chart、Resources、Navigation 或 Persistence 领域状态。
- UI 只能作为引用和运行时打开目标，不能替代世界 / 可玩场景证据。
- 文档更新使用中文，稳定 ID、路径、命令和枚举保持英文。

## Open Questions

- 无阻塞问题。用户已要求自主执行，未授权删除旧节点，因此本轮采用新增独立场景并由 HubRuntime 挂载的非破坏性路径。
