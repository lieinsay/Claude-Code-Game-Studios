# Polish Story 011: Delete / Overwrite Prompts

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Godot Runtime UX / Persistence Bridge
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Persistence, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 010 backup / quarantine UX

## Context

Polish Story 010 made invalid durable progress recoverable by moving corrupted
files out of the active load path. The next save-trust gap was preventing
accidental destructive actions: overwriting a valid local progress file and
deleting local/quarantined progress should both require explicit confirmation.

This story keeps the current one-file playable-slice save boundary. It does not
introduce a full save-slot browser, named saves, multi-slot migration, or final
release save-management UI.

## Acceptance Criteria

- [x] GIVEN no local progress exists, WHEN the Hub appears, THEN Delete local progress is visible but disabled.
- [x] GIVEN local progress exists, WHEN the player is in Hub, THEN Delete local progress is enabled.
- [x] GIVEN local progress exists, WHEN the player attempts to save over it, THEN the first action asks for overwrite confirmation and does not overwrite immediately.
- [x] GIVEN overwrite confirmation is pending, WHEN the player loads instead, THEN the overwrite prompt is cancelled.
- [x] GIVEN overwrite confirmation is pending, WHEN the player confirms Save, THEN the local progress is overwritten and remains loadable.
- [x] GIVEN durable or quarantined progress exists, WHEN the player presses Delete, THEN the first action asks for delete confirmation and does not remove files immediately.
- [x] GIVEN delete confirmation is pending, WHEN the player confirms Delete, THEN active and quarantined progress are removed, Load disables, and Delete disables.
- [x] GIVEN existing route/search/onboarding smoke runs, WHEN delete/overwrite prompts are present, THEN the playable loop still passes.
- [x] GIVEN this evidence is read, WHEN remaining risk is assessed, THEN this is a single-slot destructive-action guard, not final save-slot UX or Release readiness.

## Implementation Notes

- Keep runtime behavior in `HubRuntime.cs`.
- Keep `PlayableSliceDomainAdapter` and `Persistence` as canonical save/load authority.
- Add the Delete local progress control dynamically in C# beside existing Hub actions.
- Use two-step confirmation text and button labels instead of modal UI for this scoped polish pass.
- Keep Delete disabled outside Hub so destructive actions do not compete with Chart or Exploration input ownership.

## Completion Notes

- Completed 2026-05-23.
- `HubRuntime` now creates `DeleteProgressButton` at runtime and controls it with the existing Hub action state.
- Save over an existing durable progress file now requires a second Save press; Load cancels pending overwrite confirmation.
- Delete local progress now requires a second Delete press and removes both active durable progress and the quarantine copy.
- `tests/smoke/session_shell_durable_persistence_probe.gd` now covers overwrite confirmation, delete confirmation, cancellation, disabled states, and file removal.
- `tests/smoke/session_shell_visual_probe.gd` now verifies the Delete action visibility, Hub-only enablement, and overwrite prompt inside the existing route/search/save/load loop.
- Remaining scope is not blocking for this story: final save-slot browser, named saves, backup selection UI, long-session QA, final art/audio treatment, and Release readiness remain downstream.
