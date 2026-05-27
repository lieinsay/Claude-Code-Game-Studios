# Godot Asset Execution Plan: voyage_open_world_scene

- Contract path: `.godot-ai/contracts/scene/voyage_open_world_scene.contract.md`
- Review path: `.godot-ai/reviews/scene/voyage_open_world_scene.review.md`
- Assets to create or modify: `src/scenes/voyage/VoyageOpenWorldScene.tscn`; `src/scenes/voyage/VoyageOpenWorldScene.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`; `tests/smoke/session_shell_visual_probe.gd`; `tests/integration/playable-slice/DomainAdapterProgram.cs`; `production/scene-specs/voyage-open-world-scene.md`; `.godot-ai/verification/scene/voyage_open_world_scene.verification.md`

## Steps

1. Create independent Godot scene asset with world-layer voyage composition.
2. Add debug evidence API for route, takeoff, cockpit view, risk windows, destination silhouette, retreat anchor, and UI-evidence rejection.
3. Mount the scene non-destructively in `HubRuntime` during the existing departure / exploration surface.
4. Add `voyage_open_world_scene` Scene Physics Contract, behavior, recovery, and authored unit linkage.
5. Update authored content with scene, prototypes, and instances.
6. Extend integration and smoke checks.
7. Update Chinese production docs and `.godot-ai` verification evidence.
8. Run project build, integration, Godot smoke, solution build, and `git diff --check`.
