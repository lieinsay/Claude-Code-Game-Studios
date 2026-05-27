# Godot Asset Verification: ship_interior_layered

## Verification Summary

- Contract: `.godot-ai/contracts/scene/ship_interior_layered.contract.md`
- Review: `.godot-ai/reviews/scene/ship_interior_layered.review.md`
- Execution Mode: reviewed-auto
- Result: pass
- Changed Godot Outputs: `src/scenes/ship/ShipInteriorLayeredScene.tscn`; `src/scenes/ship/ShipInteriorLayeredScene.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`; `tests/smoke/session_shell_visual_probe.gd`
- Evidence:
  - Godot AI MCP opened `res://src/scenes/ship/ShipInteriorLayeredScene.tscn` successfully in Godot 4.6.2.
  - Scene hierarchy contains `ShipInteriorLayeredScene`, `ShipInteriorWorldLayer`, `ShipInteriorChartTableSocket`, `ChartTableRuntimeInstance`, `ChartTableAnchor`, `ShipExitThreshold`, `ShipInteriorPlayerStart`, and `SceneReferences/S4ChartSceneReference`.
  - `ShipInteriorLayeredScene.tscn` directly instances `res://src/scenes/units/ChartTable.tscn` and exports `res://src/scenes/ui/ChartFullScreenSurface.tscn` as the formal `S4_chart` reference.
  - Hub smoke verifies `ShipInteriorLayeredSceneRuntimeInstance`, `DebugShipInteriorAssetEvidence().scene_id == ship_interior_layered`, ChartTable instance/anchor readiness, `S4_chart` reference readiness, and `ui_evidence_allowed_for_scene == false`.
  - Hub smoke verifies proximity + Use still opens `S4ChartRuntimeSurface`, route selection highlights `RouteMistSelectionFrame`, and departure commits `route.mist`.
  - `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` passed with 0 warnings / 0 errors.
  - `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` passed; screenshots were skipped because the current display driver is headless.
  - `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` passed 303/303 checks, including ship-interior authoring validation.
  - `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` passed with 0 warnings / 0 errors on the final rerun.
  - `git diff --check` passed with LF/CRLF working-copy warnings only.
- Failed Checks: None.
- Risks Preserved:
  - Visuals are production-traceable greybox, not final art or audio.
  - `hub_ship_interior` remains the runtime compatibility contract ID; a future migration can rename or route scene IDs separately.
  - Existing HubRuntime helper nodes remain as non-destructive fallback/status scaffolding and are not the production-ready scene asset evidence.
- Follow-up Needed:
  - Capture non-headless screenshots for release packet evidence.
  - Continue Godot asset workflow for the next reset scene or unit instead of treating older temporary greyboxes as production-ready.
