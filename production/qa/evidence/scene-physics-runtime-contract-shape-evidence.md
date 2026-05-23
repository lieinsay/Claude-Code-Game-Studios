# Scene Physics Runtime Contract Shape Evidence

**Story:** `production/epics/scene-physics-unit-system/story-001-runtime-contract-shape.md`  
**Status:** PASS  
**Date:** 2026-05-24  
**Scope:** Story 001 only -- runtime contract shape, active scene contract lookup, and unknown-scene diagnostic behavior.

## Evidence Summary

- `HubRuntime.DebugScenePhysicsContract(scene_id)` exposes complete contracts for current playable scenes:
  - `hub_island_dock`
  - `hub_ship_interior`
  - `exploration_mist_island`
- `HubRuntime.DebugCurrentScenePhysicsContract()` follows actual playable world state transitions:
  - Hub exterior returns `hub_island_dock`
  - Ship interior returns `hub_ship_interior`
  - Exploration returns `exploration_mist_island`
- Each complete contract exposes the Story 001 required runtime shape:
  - `scene_id`
  - `contract_complete`
  - `scene_type`
  - `movement_plane`
  - `layer_height_model_ready`
  - `cutaway_reveal_ready`
  - `walk_bounds_size`
  - `scale_reference`
  - `collision_semantics`
  - `occlusion_policy`
  - `special_surfaces`
  - `dynamic_behaviors`
  - `recovery_rule`
  - `authored_physical_unit_count`
  - `source_gdd`
- Unknown scene ids return `contract_complete=false` with `diagnostic_error` and do not default to either `水平场景` or `垂直场景`.
- Evidence remains bound to world/playable scene units. UI, HUD, labels, buttons, and debug overlays are not counted as physical scene units.

## Verification

```text
dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false
Result: PASS
Warnings: 5 existing warnings
Errors: 0
```

```text
godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd
Result: PASS
Notes: Headless screenshot saves were skipped by the current display driver; runtime contract assertions passed.
```

## Story 001 Acceptance Coverage

- AC-1: PASS -- smoke verifies `hub_island_dock` and `exploration_mist_island` declare `水平场景`, while `hub_ship_interior` declares `垂直场景`.
- AC-2: PASS -- smoke checks every Story 001 required key on all three current playable scene contracts.
- AC-3: PASS -- smoke verifies current scene contract ids across Hub exterior, ship interior, and Exploration transitions; Chart/UI does not become a physical scene contract.
- AC-4: PASS -- smoke verifies an unknown scene id returns an incomplete contract with diagnostic error instead of a partial passing contract.

## Out Of Scope Preserved

- Story 002 remains responsible for deeper Layer / Height, Cutaway / Reveal, and Floor State content.
- Story 003 remains responsible for the formal scene-unit catalog, collision, occlusion, scale, and special-surface catalog rules.
- Story 004 remains responsible for dynamic behavior priority and recovery edge cases.
