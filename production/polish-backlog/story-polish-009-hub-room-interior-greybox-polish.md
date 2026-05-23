# Polish Story 009: Hub Room Interior Greybox Polish

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Runtime Scene
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 006, Polish Story 007 manual QA update, Polish Story 008 save-slot UX

## Context

Polish Story 006 separated scene art from interaction markers and added Hub room
volumes, but the rooms still read as simple anchors rather than usable interior
spaces. After Polish Story 007 closed the current cross-launch persistence trust
risk and Polish Story 008 improved load availability feedback, the next narrow
polish pass is to make the Hub cockpit, cargo room, and engine room communicate
function without changing gameplay authority.

This story remains a greybox presentation pass inside `HubRuntime.cs`. C#
managers still own route, cargo/resources, hull, onboarding, save/load, and
return-Hub state.

## Acceptance Criteria

- [x] GIVEN the Hub scene is visible, WHEN smoke checks interior nodes, THEN the cockpit exposes a window/navigation slate, cargo exposes shelves/load track, and engine exposes coil/conduit details.
- [x] GIVEN the Hub starts with no returned cargo or damage, WHEN smoke checks room semantics, THEN cargo load fill and engine damage overlay are hidden.
- [x] GIVEN route progress returns from Exploration, WHEN Hub summary updates, THEN cockpit interior status reflects route progress.
- [x] GIVEN returned cargo exists, WHEN Hub summary updates, THEN cargo interior status and load fill reflect the returned rewards.
- [x] GIVEN hull pressure exists, WHEN Hub summary updates, THEN engine interior status and damage overlay reflect the hull pressure.
- [x] GIVEN existing route/search/save/load smoke runs, WHEN room interior polish is present, THEN the domain-backed playable loop still passes.
- [x] GIVEN this evidence is read, WHEN remaining risk is assessed, THEN this story is a greybox interior readability pass, not final art/audio or Release readiness.

## Implementation Notes

- Keep all new runtime scene logic in Godot .NET / C#.
- Do not add GDScript runtime authority.
- Keep room details simple, readable, and smoke-testable.
- Drive dynamic room status from `PlayableSliceSnapshot`; do not introduce a parallel room state model.

## Completion Notes

- Completed 2026-05-23.
- `HubRuntime` now adds cockpit window/navigation slate, cargo shelves/load track/fill, engine coils/conduit/wear overlay, interior dividers, and a shared aisle.
- Hub interior status labels now derive from existing selected route, exploration step, reward/cargo, and hull snapshot values.
- The cargo load fill is hidden while empty and grows after returned cargo; the engine wear overlay is hidden at full hull and appears after hull pressure.
- `tests/smoke/session_shell_visual_probe.gd` now asserts the new interior nodes and dynamic room semantics across the existing playable route/search/save/load loop.
- Remaining scope is not blocking for this story: final room art, authored props, collision/navigation mesh, audio ambience, save-slot UX, and Release readiness remain downstream.
