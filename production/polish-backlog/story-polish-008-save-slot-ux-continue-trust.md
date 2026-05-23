# Polish Story 008: Save-Slot UX and Continue Trust

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Godot Runtime UX / Persistence Bridge
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Persistence, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 007 cross-launch persistence trust + manual close/relaunch QA PASS

## Context

Polish Story 007 proved that the playable slice can save canonical progress to
`user://`, restart, and load back into the saved state. The next trust gap was
player-facing affordance: the runtime needed to show whether progress was
loadable, avoid inviting a load when no safe progress exists, and give a clear
reason when a durable file fails validation.

This story keeps runtime authority in C# / Godot .NET. It does not introduce a
new save schema, a final slot browser, or GDScript runtime state.

## Acceptance Criteria

- [x] GIVEN no durable playable progress exists, WHEN the Hub appears, THEN the Load control is disabled and status text explains that there is no loadable progress.
- [x] GIVEN a durable progress file exists, WHEN the Hub appears after restart, THEN Load is enabled and status text explains that local progress was detected.
- [x] GIVEN the player saves successfully, WHEN feedback is shown, THEN it states that local progress is now loadable.
- [x] GIVEN a corrupt durable progress file exists, WHEN the runtime imports or loads it, THEN Load is disabled and the status text reports checksum/validation failure instead of restoring stale state.
- [x] GIVEN the existing route/search/onboarding smoke runs, WHEN save-slot affordance changes are present, THEN the playable loop still passes.
- [x] GIVEN this evidence is read, WHEN remaining risk is assessed, THEN this is a current playable-slice continue-trust polish pass, not final save-slot UX or Release readiness.

## Implementation Notes

- Keep all runtime behavior in `HubRuntime.cs`.
- Keep `PlayableSliceDomainAdapter` and `Persistence` as canonical save/load authority.
- Keep the story scoped to one durable playable progress file.
- Final save-slot browser, backup/quarantine UX, delete/overwrite prompts, and long-session trust remain downstream.

## Completion Notes

- Completed 2026-05-23.
- `HubRuntime` now tracks whether local progress is loadable and refreshes the Hub Load button affordance.
- Boot status now reports either "检测到本地进度" or "暂无可加载进度".
- Save feedback now tells the player local progress is loadable.
- Corrupt durable progress disables Load and reports checksum failure without restoring stale state.
- `tests/smoke/session_shell_durable_persistence_probe.gd` now covers no-save, detected-save, and corrupt-save UX.
- Remaining scope is not blocking for this story: full save-slot UX, backup/quarantine UI, delete/overwrite prompts, long-session QA, and Release readiness remain downstream.
