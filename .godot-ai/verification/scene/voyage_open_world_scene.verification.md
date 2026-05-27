# Godot Asset Verification: voyage_open_world_scene

- Date: 2026-05-27
- Contract: `.godot-ai/contracts/scene/voyage_open_world_scene.contract.md`
- Review: `.godot-ai/reviews/scene/voyage_open_world_scene.review.md`
- Execution Plan: `.godot-ai/execution-plans/scene/voyage_open_world_scene.execution-plan.md`
- Changed Godot Outputs: `src/scenes/voyage/VoyageOpenWorldScene.tscn`; `src/scenes/voyage/VoyageOpenWorldScene.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`; `tests/smoke/session_shell_visual_probe.gd`; `tests/integration/playable-slice/DomainAdapterProgram.cs`

## Evidence

- `VoyageOpenWorldScene.tscn` contains `VoyageWorldLayer`, `VoyageTakeoffTrail`, `VoyageShipBowForeground`, `VoyageCockpitWindowFrame`, `VoyageRouteCorridor`, `VoyageBeaconChain`, `VoyageFogBank`, `VoyageWreckageField`, `VoyageBirdSilhouette`, `VoyageDestinationMistLampSilhouette`, and `VoyageRetreatBeacon`.
- `VoyageOpenWorldScene.cs` exposes `DebugSceneAssetEvidence()` with route, destination, arrival, takeoff, active driving, fog, wreckage, bird, destination, retreat, and UI-evidence exclusion fields.
- `HubRuntime` mounts `VoyageOpenWorldSceneRuntimeInstance` during the existing departure / exploration surface and exposes `DebugVoyageOpenWorldAssetEvidence()`.
- `src/presentation/playable_slice_authored_content.json` includes `authored_scenes::voyage_open_world_scene`, 8 voyage prototypes, and 8 placed units on `voyage_air_lane_01`.
- Smoke verifies the independent voyage asset, world nodes, debug evidence, authored unit catalog, #20 contract, and UI-only evidence rejection.

## Verification Commands

- PASS: `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` -- 0 warnings / 0 errors.
- PASS: `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` -- 679/679 checks.
- PASS: `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` -- smoke passed; screenshots skipped by current headless display driver.
- PASS: `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` -- 107 existing warnings / 0 errors.
- PASS: `git diff --check` -- only LF/CRLF working-copy warnings.

## Known Gaps

- Headless display may skip screenshot capture by existing smoke logic.
- This is production-traceable greybox / asset evidence, not final art, final audio, or complete live driving gameplay.
