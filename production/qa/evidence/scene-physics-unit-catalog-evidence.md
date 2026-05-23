# Scene Physics Unit Catalog Evidence

> **Story**: `production/epics/scene-physics-unit-system/story-003-unit-catalog-collision-occlusion-scale.md`
> **Date**: 2026-05-24
> **Result**: PASS

## Scope

Story 003 verifies that current playable scenes expose authored scene-unit catalogs with collision, occlusion, player-relative scale, and special-surface policy. Evidence must come from the `world_playable_scene` layer, not UI/HUD/text labels.

Covered scenes:

- `hub_island_dock`
- `hub_ship_interior`
- `exploration_mist_island`

## Runtime Evidence

`HubRuntime.DebugScenePhysicsContract(scene_id)` now exposes the Story 003 contract fields:

- `unit_catalog_ready`
- `collision_ready`
- `occlusion_ready`
- `scale_ready`
- `special_surface_ready`
- `scene_unit_catalog`
- `collision_table`
- `occlusion_layers`
- `scale_table`
- `special_surface_table`
- `asset_replacement_rule`
- `physical_unit_source_layer`
- `ui_evidence_allowed`

Each `scene_unit_catalog` entry declares:

- `unit_id`
- `unit_type`
- `collision`
- `occlusion_layer`
- `scale_rule`
- `source_layer`
- `ui_evidence_allowed`

The smoke probe verifies catalog size equals `authored_physical_unit_count`, includes blocking collision units and soft-overlap interaction anchors, ties scale rules to `player_unit`, and rejects UI-only scene evidence.

## Contract Tables

- Collision semantics distinguish `blocking_static`, `blocking_dynamic`, `pushable`, `soft_overlap`, `height_marker`, and `visual_only`.
- Occlusion layers include `background`, `midground_floor`, `midground_object`, `foreground_occluder`, `height_shadow`, and `ui_overlay: not physical evidence`.
- Scale table anchors all relative readability to `player_unit=1.0`.
- Special surfaces are classified as `gameplay_affecting` or `visual_only`.
- Asset replacement rule requires preserving collision footprint, occlusion behavior, interaction radius, and size readability unless the Scene Physics Contract is re-reviewed.

## Verification

- PASS: `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` (0 warnings, 0 errors)
- PASS: `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- PASS: `git diff --check` (LF/CRLF warnings only)

## Dependencies

Story 001 is implemented and pushed as `d8903ad01ae8caf4431984b081f5b73c8f6ce03a`.
Story 002 is implemented and pushed as `054b3035d10c19308216d9c30ba2cd1f7d6647e7`.
Formal `/story-done` closure remains downstream.

## Out of Scope

Story 004 owns dynamic behavior conflict priority, richer dynamic special-surface parameters, and recovery behavior validation.
