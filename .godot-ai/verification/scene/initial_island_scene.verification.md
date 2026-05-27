# Godot Asset Verification: initial_island_scene

## Verification Summary

- Contract: `.godot-ai/contracts/scene/initial_island_scene.contract.md`
- Review: `.godot-ai/reviews/scene/initial_island_scene.review.md`
- Execution Mode: reviewed-auto
- Result: pass
- Changed Godot Outputs: `src/scenes/hub/InitialIslandScene.tscn`; `src/scenes/hub/InitialIslandScene.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`; `tests/smoke/session_shell_visual_probe.gd`; `tests/integration/playable-slice/DomainAdapterProgram.cs`

## Evidence

- Godot MCP/editor state was inspected before execution: Godot `4.6.2-stable`, project `云海织航`, readiness `ready`.
- `InitialIslandScene.tscn` contains `InitialIslandWorldLayer`, `HubPlayableSkyBackdrop`, `HubIslandMainMass`, `HubDockPlankWalkway`, `HubDockedShipExterior`, `HubDockedShipBalloon`, `HubBoardingRamp`, `BoardingRampSoftOverlap`, `HubWaterlineBoundary`, and `InitialIslandPlayerStart`.
- `InitialIslandScene.cs` exposes `DebugSceneAssetEvidence()` with `scene_id == initial_island_scene`, `runtime_contract_id == hub_island_dock`, `boarding_target_scene_id == ship_interior_layered`, `boarding_target_runtime_contract_id == hub_ship_interior`, and `ui_evidence_allowed_for_scene == false`.
- `HubRuntime` mounts `InitialIslandSceneRuntimeInstance` for exterior Hub space and keeps `hub_island_dock` as the compatibility physics/runtime contract.
- `src/presentation/playable_slice_authored_content.json` now includes `authored_scenes::initial_island_scene` plus `hub_island_dock` world/playable prototypes and instances for player marker, island mass, dock walkway, docked ship hull, boarding ramp, airship envelope, and waterline.
- Godot smoke verifies the initial island independent scene, world layer, player spawn, boarding anchor, docked ship exterior, waterline boundary, UI-evidence exclusion, `hub_island_dock` authored unit catalog/linkage, and the boarding transition into `ship_interior_layered`.

## Commands

| Command | Result |
| --- | --- |
| `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` | PASS, 0 warnings / 0 errors |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS, 479/479 |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS; screenshots skipped by current headless display driver |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS, 107 existing warnings / 0 errors |
| `git diff --check` | PASS; LF/CRLF warnings only |

## Failed Checks

- First focused integration run failed one stale expected content-version assertion after the authored content version advanced to `polish-asset-reset-initial-island-v1`; the assertion was updated and rerun PASS.
- First focused integration attempt also showed a transient Godot editor DLL lock while the editor process held `.godot/mono/temp/bin/Debug/CloudWeaverVoyage.dll`; the editor was stopped through Godot MCP and the test reran PASS.

## Risks Preserved

- This is production-traceable greybox evidence, not final art / audio completion.
- Non-headless screenshot capture remains pending because the current display driver skipped screenshots.
- `hub_island_dock` remains the stable runtime contract ID for compatibility; any future ID migration needs a separate contract/review.
- Some initial-island prototypes still use `legacy_replacement_status: pending_spec_replacement` where dedicated unit specs have not yet been authored.

## Follow-up Needed

- Capture non-headless screenshot evidence when a display-capable Godot run is available.
- Continue the reset workflow for `voyage_open_world_scene` or author missing dedicated unit specs for initial-island dock pieces.
