# Polish Story 010: Backup / Quarantine UX for Invalid Durable Progress

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Godot Runtime UX / Persistence Bridge
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Persistence, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 008 save-slot UX and continue trust; Polish Story 009 hub room/interior greybox polish

## Context

Polish Story 008 made load availability visible and prevented the runtime from
offering a Load action when durable progress fails validation. The remaining
trust gap was recovery language and file handling: an invalid durable progress
file should be moved out of the active load path, preserve diagnostic evidence,
and tell the player that they can safely continue with a new save.

This story keeps the current one-file playable-slice save boundary. It does not
introduce a full save-slot browser, overwrite prompts, save naming, cloud save,
or final release migration tooling.

## Acceptance Criteria

- [x] GIVEN durable playable progress fails checksum validation, WHEN the Hub imports progress on boot, THEN the invalid file is removed from the active save path.
- [x] GIVEN durable playable progress fails checksum validation, WHEN quarantine succeeds, THEN a quarantine copy remains available for diagnostics.
- [x] GIVEN an invalid durable progress file was quarantined, WHEN the Hub appears, THEN Load is disabled and status text explains that the save was isolated.
- [x] GIVEN an invalid durable progress file was quarantined, WHEN the player attempts Load, THEN the runtime does not restore stale state and explains that a new safe save can replace it.
- [x] GIVEN an invalid durable progress file was quarantined, WHEN the player saves again, THEN a fresh durable progress file is written and Load becomes available again.
- [x] GIVEN the existing route/search/onboarding smoke runs, WHEN quarantine UX changes are present, THEN the playable loop still passes.
- [x] GIVEN this evidence is read, WHEN remaining risk is assessed, THEN this is current playable-slice invalid-save recovery polish, not final save-slot UX or Release readiness.

## Implementation Notes

- Keep all runtime file handling in `HubRuntime.cs`.
- Keep `PlayableSliceDomainAdapter` and `Persistence` as canonical save/load authority.
- Store the quarantined invalid payload beside the active durable progress file under `user://`.
- Keep debug helpers limited to smoke-test setup and diagnostics.

## Completion Notes

- Completed 2026-05-23.
- `HubRuntime` now writes invalid durable progress to `cloudweaver_playable_progress.quarantine.json`.
- Invalid durable progress is removed from `cloudweaver_playable_progress.json` so future loads cannot restore stale state.
- Boot and Load feedback now state that checksum-failed progress was isolated and that a new safe save can replace it.
- `tests/smoke/session_shell_durable_persistence_probe.gd` now covers quarantine creation, active-path removal, disabled Load, safe re-save, and quarantine cleanup.
- Remaining scope is not blocking for this story: final save-slot browser, delete/overwrite prompts, backup selection UI, long-session QA, and Release readiness remain downstream.
