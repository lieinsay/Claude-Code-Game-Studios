# Polish Story 016: Scene Physics Contract Retrofit

> **Phase**: Polish
> **Status**: Implemented -- Verified
> **Layer**: Scene Structure / Runtime Presentation / QA Gate
> **Type**: Design Contract Retrofit
> **Estimate**: M / 1 day
> **Governing GDDs**: GDD #19 Complete Scene Composition, GDD #20 Scene Physics Unit Design
> **Governing ADRs**: ADR-0004 Interaction Handler, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: User design correction that physical-world exploration is bottom-layer gameplay, and that scene must be distinct from UI.

## Context

The new scene design baseline changes the release-readiness question. The current playable slice can no longer be judged only by whether UI panels, labels, buttons, and proximity gates work. Hub exterior, ship interior, and Exploration must each declare a Scene Physics Contract before they can be treated as authored playable scenes.

This story retrofits the current Polish 015 slice without inventing a broad physics sandbox. It records the physical contracts that already exist in the runtime greybox, exposes them to smoke tests, and makes the distinction explicit: scene evidence comes from world/playable scene units, not from HUD or text dashboard controls.

## Acceptance Criteria

- [x] GIVEN Hub exterior is active, WHEN smoke queries the scene physics contract, THEN it declares `水平场景`, walk bounds, blocking static units, soft-overlap boarding, water boundary policy, unit scale, occlusion policy, dynamic behavior extension rules, and stuck-state recovery.
- [x] GIVEN ship interior is active, WHEN smoke queries the scene physics contract, THEN it declares `垂直场景`, room-scale units, hull/bay blocking semantics, soft-overlap helm/storage/engine/exit anchors, cockpit glass as visual-only surface, occlusion policy, and stuck-state recovery.
- [x] GIVEN Exploration is active, WHEN smoke queries the scene physics contract, THEN it declares `水平场景`, island walk bounds, search wreck and return ship blocking semantics, soft-overlap search/return anchors, water boundary policy, threat/height markers, occlusion policy, and stuck-state recovery.
- [x] GIVEN the current runtime switches between Hub exterior, ship interior, and Exploration, WHEN smoke asks for the current physics contract, THEN the returned `scene_id` follows the active world scene state.
- [x] GIVEN a UI/HUD label, button, or dashboard node exists, WHEN evaluating scene completion, THEN it does not count as a physical scene unit or as proof that the scene is complete.
- [x] GIVEN future scene units add pushable, elastic, sliding, breakable, one-way, moving-platform, mirror, glass, or water behavior, THEN the contract must declare the behavior explicitly before implementation readiness.

## Implementation Summary

- Added runtime debug contracts in `HubRuntime` for:
  - `hub_island_dock`
  - `hub_ship_interior`
  - `exploration_mist_island`
- Each contract exposes scene type, movement plane, walk bounds, player-relative scale, occlusion/layering policy, collision semantics, special surface policy, dynamic behavior extension rule, stuck-state recovery, and authored physical unit count.
- Updated the visual smoke probe to assert all three contracts and to verify the active contract follows scene state transitions.
- Updated GDD #19 and #20 to explicitly state that scene is not UI: HUD, buttons, labels, text panels, and overlays may support scene readability, but they cannot substitute for physical scene units.

## Verification Targets

- [x] `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` — PASS, 0 warnings / 0 errors on final rerun.
- [x] `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` — PASS.
- [x] `git diff --check` — PASS, LF/CRLF warnings only.

## Evidence

- `production/qa/evidence/polish-016-scene-physics-contract-retrofit-evidence.md`

## Human QA Boundary

This story proves the current slice has explicit scene physics contracts and smoke coverage. It does not prove final visual quality, final art, true Godot collision bodies, pushable objects, elastic surfaces, water traversal, mirror/glass gameplay, or complete release readiness.

Focused human QA should still judge whether the world scene itself, with UI treated only as assistance, reads as a physical place worth exploring.
