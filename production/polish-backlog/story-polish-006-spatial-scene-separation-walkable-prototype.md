# Polish Story 006: Spatial Scene Separation and Walkable Prototype

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Runtime Scene
> **Type**: Polish
> **Estimate**: S / 0.5-1 day
> **Governing ADRs**: ADR-0012 UI Input Routing, ADR-0013 Exploration Scavenge, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish route/search human QA checklist, 2026-05-22

## Context

The 2026-05-22 human playtest passed the route/search loop with conditions but
found that the UI and scene felt mixed together, and that the world read like a
static image with little spatial function. The recommended next story was to
separate HUD/panel responsibilities from the playable scene and create a first
walkable Hub/island/ship prototype before expanding route/search content.

This story makes a small, verifiable presentation pass inside `HubRuntime.cs`
without changing runtime authority. C# managers still own route/search,
resources, hull, onboarding, and persistence.

## Acceptance Criteria

- [x] GIVEN the Hub runtime starts, WHEN the playable layer is created, THEN scene art and interaction/player markers live on separate runtime layers.
- [x] GIVEN the player is in Hub, WHEN movement is processed, THEN position is clamped to a Hub walkable boundary instead of a full-screen rectangle.
- [x] GIVEN the Hub scene is visible, WHEN smoke checks spatial anchors, THEN the scene exposes a walkable island boundary, ship hull, boarding ramp, cockpit room, cargo room, and engine room.
- [x] GIVEN the player departs to Exploration, WHEN the scene changes, THEN Exploration exposes its own walkable island boundary, docked ship, boarding ramp, and path.
- [x] GIVEN existing route/search/save/load smoke runs, WHEN the new scene structure is present, THEN the domain-backed playable loop still passes.
- [x] GIVEN QA evidence is read, WHEN remaining risk is assessed, THEN this story is clearly a spatial prototype, not final art/audio or Release readiness.

## Implementation Notes

- Keep all new runtime scene logic in Godot .NET / C#.
- Do not add GDScript runtime authority.
- Keep visual treatment greybox and testable; final art/audio remains downstream.
- This story responds to human QA by improving spatial structure, not by expanding content tables.

## Completion Notes

- Completed 2026-05-22.
- `HubRuntime` now creates separate `WorldSceneLayer` and `WorldInteractionLayer` children under the playable layer.
- Hub movement clamps to a Hub walkable boundary; Exploration movement clamps to a separate Exploration walkable boundary.
- Hub scene now includes island boundary, ship hull, boarding ramp, cockpit/cargo/engine room volumes, and existing helm/storage/module anchors.
- Exploration scene now includes island boundary, docked ship, boarding ramp, island path, and existing search/return/threat/extraction anchors.
- Smoke evidence validates the new layer split, walkable boundaries, room/ship anchors, and continued route/search/save/load loop.
- Remaining scope is not blocking for this story: final art/audio, richer collision/navigation, and room interiors remain downstream Polish backlog. Cross-launch persistence trust was later addressed by Polish Story 007.
