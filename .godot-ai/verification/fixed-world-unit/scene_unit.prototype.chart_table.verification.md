# Godot Asset Verification: scene_unit.prototype.chart_table

## Verification Summary

- Contract: `.godot-ai/contracts/fixed-world-unit/scene_unit.prototype.chart_table.contract.md`
- Review: `.godot-ai/reviews/fixed-world-unit/scene_unit.prototype.chart_table.review.md`
- Execution Mode: reviewed-auto
- Result: pass
- Changed Godot Outputs: `src/scenes/units/ChartTable.tscn`; `src/scenes/units/ChartTable.cs`; `src/presentation/playable_slice_authored_content.json`; `src/scenes/HubRuntime.cs`; `tests/smoke/session_shell_visual_probe.gd`
- Evidence: Godot AI MCP loaded `res://src/scenes/units/ChartTable.tscn` and returned the expected hierarchy with `ChartTable`, visible table/body/map/highlight nodes, `ChartTableAnchor`, and `SoftOverlapShape`. `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` passed with 0 warnings / 0 errors. `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` passed.
- Failed Checks: None.
- Risks Preserved: First implementation is greybox; final art/audio and release screenshot evidence remain downstream.
- Follow-up Needed: Produce release-quality screenshot evidence and final asset/audio treatment when the release packet resumes.

## Godot AI MCP Evidence

- Active session: `claude-code-game-studios@2681`
- Project path: `D:/Project/MineCraftMod/Claude-Code-Game-Studios/`
- Godot version: `4.6.2-stable`
- Unit hierarchy after `scene_open`:
  - `/ChartTable`
  - `/ChartTable/TableBody`
  - `/ChartTable/BrassRimTop`
  - `/ChartTable/BrassRimBottom`
  - `/ChartTable/ProjectionGlow`
  - `/ChartTable/MapSurface`
  - `/ChartTable/RouteLine`
  - `/ChartTable/FocusHighlight`
  - `/ChartTable/DisabledOverlay`
  - `/ChartTable/StateLabel`
  - `/ChartTable/ChartTableAnchor`
  - `/ChartTable/ChartTableAnchor/SoftOverlapShape`

## Runtime Evidence

- Hub smoke verifies `ChartTableInteractPoint`, `ChartTableRuntimeInstance`, `ChartTableAnchor`, authored `scene_unit.prototype.chart_table` catalog linkage, and proximity + Use opening `S4_chart`.
- `hub_ship_interior` now reports authored physical unit evidence from `src/presentation/playable_slice_authored_content.json` with `ui_evidence_allowed == false`.
