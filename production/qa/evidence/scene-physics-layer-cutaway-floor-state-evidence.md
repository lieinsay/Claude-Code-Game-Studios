# Scene Physics Layer Cutaway Floor State Evidence

**Story:** `production/epics/scene-physics-unit-system/story-002-layer-height-cutaway-floor-state.md`  
**Status:** PASS  
**Date:** 2026-05-24  
**Scope:** Story 002 only -- Layer / Height Model, Cutaway / Reveal Model, Floor State, behind-object reveal classification, and occlusion readability budget.

## Evidence Summary

- Current playable scene contracts expose direct Story 002 fields:
  - `movement_readability`
  - `primary_walkable_layer`
  - `floor_id`
  - `floor_index`
  - `is_active_floor`
  - `visibility_mode`
  - `vertical_connectors`
  - `occluders_hidden_above`
  - `interactions_enabled`
  - `behind_object_reveal`
  - `identity_occlusion_max_seconds`
- Horizontal scenes (`hub_island_dock`, `exploration_mist_island`) declare:
  - four-direction ground-plane movement readability
  - height-only cue handling for jump/fly/high elements
  - `primary_walkable_layer`
  - walkable, transition, height-only, blocked, and visual layer categories
  - behind-object reveal classification as `N/A true` for the current slice where no passable behind-object route exists
- Vertical scene (`hub_ship_interior`) declares:
  - left/right primary movement
  - depth layering and foreground/background visual separation
  - future ladder/stair connector policy
  - `front_wall_removed` visibility
  - `active_floor_focus` reveal behavior
- All current contracts declare floor state fields:
  - `floor_id`
  - `floor_index`
  - `is_active_floor`
  - `visibility_mode`
  - `walkable_bounds`
  - `vertical_connectors`
  - `occluders_hidden_above`
  - `interactions_enabled`
- The smoke probe continues to require world/playable scene evidence. UI, HUD, labels, buttons, and debug overlays do not count as floors, collision, reveal, or physical scene units.

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
Notes: Headless screenshot saves were skipped by the current display driver; Layer / Height, Cutaway / Reveal, Floor State, and behind-object reveal contract assertions passed.
```

## Acceptance Coverage

- AC-1: PASS -- horizontal contracts declare ground-plane up/down/left/right movement and height-only cue handling.
- AC-2: PASS -- horizontal contracts expose `primary_walkable_layer` plus walkable, transition, height-only, blocked, and visual layers.
- AC-3: PASS -- the vertical ship interior contract declares left/right movement, depth layering, foreground/background separation, and future ladder/stair connector policy.
- AC-4: PASS -- the ship interior contract declares `front_wall_removed` and `active_floor_focus`; horizontal scenes explicitly classify cutaway/interior reveal as `N/A true`.
- AC-5: PASS -- all current contracts expose `floor_id`, `floor_index`, `is_active_floor`, `visibility_mode`, `walkable_bounds`, `vertical_connectors`, `occluders_hidden_above`, and `interactions_enabled`.
- AC-6: PASS -- current horizontal scenes classify behind-object reveal as `N/A true` without confusing it with entering a building/interior, and preserve collision/interaction identity.
- AC-7: PASS -- current contracts expose `identity_occlusion_max_seconds=1.0`, inside the GDD readability budget.

## Dependency Note

Story 001 is complete and pushed in commit `d8903ad`.

## Out Of Scope Preserved

- Story 003 remains responsible for formal scene-unit catalog, collision, occlusion, scale, and special-surface catalog detail.
- Story 004 remains responsible for dynamic physical behavior priority and recovery edge cases.
