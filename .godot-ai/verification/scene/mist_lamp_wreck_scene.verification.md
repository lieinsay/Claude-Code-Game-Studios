# Godot Asset Verification: mist_lamp_wreck_scene

- Date: 2026-05-27
- Contract: `.godot-ai/contracts/scene/mist_lamp_wreck_scene.contract.md`
- Review: `.godot-ai/reviews/scene/mist_lamp_wreck_scene.review.md`
- Execution Plan: `.godot-ai/execution-plans/scene/mist_lamp_wreck_scene.execution-plan.md`
- Changed Godot Outputs: `src/scenes/mist/MistLampWreckScene.tscn`; `src/scenes/mist/MistLampWreckScene.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`; `tests/smoke/session_shell_visual_probe.gd`; `tests/integration/playable-slice/DomainAdapterProgram.cs`

## Evidence

- `MistLampWreckScene.tscn` contains `MistLampWorldLayer`, `MistIslandMass`, `MistIslandPath`, `MistLampWreckBody`, `MistSearchScanAnchor`, `MistReturnShipHull`, `MistReturnHelmAnchor`, `MistReturnTakeoffTrail`, and `MistWaterBoundary`.
- `MistLampWreckScene.cs` exposes `DebugSceneAssetEvidence()` with scene IDs, return targets, world layer readiness, player spawn, search, return, takeoff, boundary, no-threat-zone, and UI-evidence exclusion fields.
- `HubRuntime` mounts `MistLampWreckSceneRuntimeInstance` during the existing exploration surface and exposes `DebugMistLampWreckAssetEvidence()`.
- `src/presentation/playable_slice_authored_content.json` includes `authored_scenes::mist_lamp_wreck_scene`, 8 mist-lamp prototypes, and 9 placed `exploration_mist_island` units on `mist_wreck_ground_01`.
- Smoke verifies the independent mist-lamp asset, world nodes, debug evidence, authored unit catalog, #20 contract, return path evidence, and UI-only evidence rejection.

## Verification Commands

- PASS: `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` -- 0 warnings / 0 errors.
- PASS: `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` -- 891/891 checks.
- PASS: `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` -- smoke passed; screenshots skipped by current headless display driver.
- PASS: `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` -- 107 existing warnings / 0 errors.
- PASS: `git diff --check` -- only LF/CRLF working-copy warnings after trailing whitespace fix.

## Known Gaps

- Godot AI MCP editor transport was unavailable in this Codex desktop session: `http://127.0.0.1:8000/mcp` request failed. Headless Godot smoke is the execution evidence for this pass.
- Headless display may skip screenshot capture by existing smoke logic.
- This is production-traceable greybox / asset evidence, not final art, final audio, or complete live return-flight gameplay.
