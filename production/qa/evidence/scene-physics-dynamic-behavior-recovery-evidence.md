# Scene Physics Dynamic Behavior Recovery Evidence

> **Story**: `production/epics/scene-physics-unit-system/story-004-dynamic-behaviors-special-surfaces-recovery.md`
> **Date**: 2026-05-24
> **Result**: PASS

## Scope

Story 004 verifies that current playable Scene Physics Contracts declare dynamic behavior tags, parameters, feedback, conflict priority, fallback rules, and stuck recovery paths before any real dynamic physics simulation is introduced.

Covered scenes:

- `hub_island_dock`
- `hub_ship_interior`
- `exploration_mist_island`

## Runtime Evidence

`HubRuntime.DebugScenePhysicsContract(scene_id)` now exposes:

- `physical_behavior_ready`
- `recovery_ready`
- `dynamic_behaviors`
- `behavior_priority_table`
- `behavior_conflict_rule`
- `behavior_fallback_rules`
- `missing_priority_blocks_readiness`
- `stuck_recovery_seconds`
- `recovery_table`

Each `dynamic_behaviors` entry declares:

- `unit_id`
- `behavior_label`
- `applicable_behavior_tags`
- `parameters`
- `feedback`
- `affected_unit_types`
- `conflict_priority`
- `fallback_rule`
- `recovery_action`
- `source_layer`
- `ui_evidence_allowed`

The governing conflict rule is `effective_behavior = highest_priority(applicable_behavior_tags)`. Missing priorities block implementation readiness.

## Behavior Coverage

- `hub_island_dock`: waterline hazardous boundary, boarding-ramp trigger-only anchor, airship envelope visual-only height marker.
- `hub_ship_interior`: exit-threshold trigger-only anchor, storage crate static blocker, cockpit glass visual-only surface.
- `exploration_mist_island`: sea boundary hazardous water/void, threat-zone warning, search and return trigger-only anchors, visual-only fog.

All behavior and recovery evidence comes from `world_playable_scene`; UI/HUD may explain state but cannot satisfy the physical contract.

## Recovery Coverage

Recovery table entries cover:

- outside-walk-bounds clamp
- blocked static or hazard-boundary clamp
- visible safe floor, boundary, or exit-anchor feedback
- UI-only evidence rejection

`stuck_recovery_seconds` is bounded at 2.0 seconds for the current contract data.

## Verification

- PASS: `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` (5 existing warnings, 0 errors)
- PASS: `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- PASS: `git diff --check` (LF/CRLF warnings only)

## Dependencies

Story 001 is complete and pushed as `d8903ad01ae8caf4431984b081f5b73c8f6ce03a`.
Story 002 is complete and pushed as `054b3035d10c19308216d9c30ba2cd1f7d6647e7`.
Story 003 is complete and pushed as `cf141163e8fe92927dc7b583d844f7e8017ca7e5`.

## Out of Scope

This story does not add new Godot physics bodies, per-frame simulation, asset meshes, domain rewards, route consequences, hull damage rules, repair logic, market purchase logic, or final art/audio assets.
