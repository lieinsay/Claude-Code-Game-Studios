# Godot Asset Execution Plan: initial_island_scene

## Plan Metadata

- Contract path: `.godot-ai/contracts/scene/initial_island_scene.contract.md`
- Review path: `.godot-ai/reviews/scene/initial_island_scene.review.md`
- Execution mode: reviewed-auto

## Assets To Create Or Modify

- Create `src/scenes/hub/InitialIslandScene.tscn`
- Create `src/scenes/hub/InitialIslandScene.cs`
- Modify `src/scenes/HubRuntime.cs` to mount `InitialIslandSceneRuntimeInstance` for exterior Hub space and expose `DebugInitialIslandAssetEvidence()`.
- Modify `src/presentation/playable_slice_authored_content.json` to add `authored_scenes::initial_island_scene` and `hub_island_dock` scene-unit prototypes / instances.
- Modify `tests/smoke/session_shell_visual_probe.gd` to verify independent initial-island scene evidence and boarding path into `ship_interior_layered`.
- Modify `tests/integration/playable-slice/DomainAdapterProgram.cs` to validate initial-island authored data.
- Update `production/scene-specs/initial-island-scene.md`, `production/session-state/active.md`, and `.godot-ai/verification/scene/initial_island_scene.verification.md`.

## Godot AI MCP Capabilities Used Or Checked

- `session_activate`
- `editor_state`
- File-level scene/script edits are used for deterministic C#/.tscn output after confirming a ready editor session.

## Verification Evidence Required

- `.godot-ai/verification/scene/initial_island_scene.verification.md`
- Hierarchy/runtime evidence for `InitialIslandSceneRuntimeInstance`, world layer, spawn, boarding anchor, waterline, and ship exterior.
- Smoke evidence for exterior scene visibility, current physics contract, and boarding transition into `ship_interior_layered`.
- Integration evidence for authored scene/unit records, allowed scene IDs, scene spec paths, and `SceneUnitAuthoringFixture.ValidateScene("hub_island_dock")`.
- Build and diff evidence.

## Known Risks To Preserve

- This pass does not claim final art/audio.
- Existing `hub_island_dock` runtime ID remains for compatibility.
- Old `HubRuntime` helper nodes may remain as scaffolding but must not be cited as production-ready scene evidence.
