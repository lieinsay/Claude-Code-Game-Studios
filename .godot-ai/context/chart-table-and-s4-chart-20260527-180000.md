# Godot Asset Context: chart-table-and-s4-chart

## Original Request

开始下一步任务。

## Resolved Next Task

`production/session-state/active.md` marks the corrected 2026-05-27 priority as implementing approved independent core UI / unit assets, especially `chart-full-screen-surface` and `chart-table`, through `godot-asset-interview -> godot-asset-review -> godot-asset-execute`.

## Referenced Files

- `production/unit-specs/fixed-scene-objects/chart-table.md`
- `production/ui-specs/chart-full-screen-surface.md`
- `production/unit-specs/README.md`
- `production/ui-specs/README.md`
- `src/scenes/HubRuntime.cs`
- `src/presentation/playable_slice_authored_content.json`

## Known Facts

- `scene_unit.prototype.chart_table` has creation suitability `APPROVED`.
- `S4_chart` has creation suitability `APPROVED`.
- Existing `ChartPanel` / HubRuntime-embedded chart visuals are temporary and cannot be release evidence.
- No existing Godot scene exists for either `ChartTable` or `ChartFullScreenSurface`.
- Godot editor MCP session is available for this project, Godot `4.6.2-stable`, readiness `ready`.

## Constraints

- Do not delete or replace existing Godot nodes without explicit path-level approval.
- Create independent assets and wire them non-destructively.
- Chart UI must remain UI evidence only and must not prove voyage or island scenes.
- Chart table must be world/playable evidence with `ui_evidence_allowed == false`.

## Open Questions

- Final art direction, audio, and animation are still P0/P1 asset-production follow-ups.
- Release screenshots remain downstream evidence, not a blocker for the first independent asset implementation.
