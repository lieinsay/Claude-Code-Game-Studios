# Polish Story 007: Cross-Launch Persistence Trust

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Core Persistence / Godot Runtime Bridge
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Persistence, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish route/search human QA checklist, Polish Story 006

## Context

The 2026-05-22 human playtest passed the playable route/search loop with
conditions and called out save/load trust after closing the game as a remaining
risk. Prior Polish stories proved canonical in-memory `Persistence` save/load,
but the playable runtime did not yet write the promoted progress manifest to a
durable Godot user path.

This story closes the engineering side of that risk for the current playable
slice. Runtime authority remains C# / Godot .NET: `Persistence` owns canonical
manifest promotion/import, `PlayableSliceDomainAdapter` exposes progress JSON,
and `HubRuntime` writes/reads the file from `user://`.

## Acceptance Criteria

- [x] GIVEN canonical progress has been saved, WHEN the playable runtime exports progress for platform storage, THEN the exported data is the promoted canonical manifest, not a parallel save format.
- [x] GIVEN a fresh adapter instance, WHEN durable progress JSON is imported and loaded, THEN screen, route/search state, carried rewards, hull pressure, and exploration step restore.
- [x] GIVEN a Godot SessionShell is closed and a new SessionShell is started, WHEN the user loads progress, THEN the Exploration HUD restores from the durable `user://` file.
- [x] GIVEN durable progress JSON has been corrupted, WHEN it is imported, THEN checksum validation rejects it before domain restore.
- [x] GIVEN canonical JSON orders keys deterministically, WHEN imported data is restored, THEN domain restore order follows registered deserializers rather than JSON field order.
- [x] GIVEN smoke tests run repeatedly, WHEN durable progress is cleared, THEN the smoke can remove the test save file and remain repeatable.
- [x] GIVEN this evidence is read, WHEN remaining risk is assessed, THEN this story is cross-launch playable-slice persistence trust, not final release readiness or full save UX.

## Implementation Notes

- Keep file IO in `HubRuntime.cs`; keep `PlayableSliceDomainAdapter` headless.
- Do not introduce GDScript runtime authority.
- Do not create a second save model beside canonical `Persistence`.
- This story proves current playable-slice cross-launch restore only; future full-game save slots, backup failover UI, migration tooling, and cloud/Steam storage are downstream.

## Completion Notes

- Completed 2026-05-22.
- `Persistence` now exports/imports safe artifact manifests, verifies imported checksums, and decodes canonical JSON objects for platform storage bridges.
- `Persistence` restores domains in registered-deserializer order so canonical JSON key sorting cannot change restore semantics.
- `PlayableSliceDomainAdapter` now exposes progress JSON export/import for the playable slice.
- `HubRuntime` writes successful canonical progress saves to `user://cloudweaver_playable_progress.json` and imports that file before load.
- Added `tests/smoke/session_shell_durable_persistence_probe.gd` to prove save, close SessionShell, restart SessionShell, load, and restore.
- Remaining scope is not blocking for this story: full save-slot UX, backup/quarantine UI, long playtest trust, final content scale, and release readiness remain downstream.
