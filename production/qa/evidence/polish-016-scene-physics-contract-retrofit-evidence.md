# Polish 016 Scene Physics Contract Retrofit Evidence

**Date:** 2026-05-24  
**Story:** `production/polish-backlog/story-polish-016-scene-physics-contract-retrofit.md`  
**Status:** PASS

## Scope

This evidence covers the retrofit required after the bottom-layer gameplay change: physical-world exploration is now a core scene contract, and scene evidence must come from world/playable scene units rather than UI panels, buttons, labels, or text dashboards.

## Verification

| Check | Result | Notes |
| --- | --- | --- |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | Latest rerun completed with 5 existing warnings / 0 errors. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Verifies Hub exterior, ship interior, and Exploration Scene Physics Contracts, including Layer / Height, Cutaway / Reveal, and Floor State fields, in the full playable route. |
| `git diff --check` | PASS | Only LF/CRLF working-copy warnings were reported. |

## Contract Coverage

- `hub_island_dock`: declares `水平场景`, ground-plane four-directional movement, `primary_walkable_layer=hub_dock_ground`, exterior single-floor state, Cutaway / Reveal N/A true rule, walk bounds, player-relative scale, z-layer occlusion policy, blocking island/water/dock/ship semantics, soft-overlap boarding, water boundary policy, dynamic behavior extension rule, and stuck-state clamp recovery.
- `hub_ship_interior`: declares `垂直场景`, left/right primary movement with room depth and future vertical connectors, `floor_id=ship_deck_01`, `floor_index=1`, `front_wall_removed + active_floor_focus`, walk bounds, room-scale unit reference, z-layer occlusion policy, hull/bay blocking semantics, soft-overlap helm/storage/engine/exit anchors, cockpit glass as visual-only, dynamic behavior extension rule, and stuck-state exit recovery.
- `exploration_mist_island`: declares `水平场景`, ground-plane four-directional movement, `primary_walkable_layer=mist_island_path`, exterior single-floor state, Cutaway / Reveal N/A true rule, walk bounds, player-relative scale, z-layer occlusion policy, island/water/cliff/wreck/return-ship blocking semantics, soft-overlap search/return anchors, water boundary policy, dynamic behavior extension rule, and stuck-state clamp recovery.

## Scene vs UI Boundary

The smoke checks assert authored physical scene unit counts and world-layer contracts. UI/HUD text can still assist the player, but it is not accepted as proof that a scene exists, that physics are declared, or that the scene is complete.

## Remaining Risk

The current playable slice still uses greybox runtime rectangles and debug contracts rather than final Godot `CollisionObject2D` assets, pushable bodies, water traversal, mirror/glass gameplay, or elastic surfaces. Those remain future implementation work and must declare contract entries before becoming playable scene units.
