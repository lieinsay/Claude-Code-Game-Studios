# Polish Story 012: Long-Session Save / Load Soak

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Godot Runtime QA / Persistence Bridge
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Persistence, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 011 delete / overwrite prompts

## Context

Polish Stories 007-011 closed the current cross-launch, load-availability,
quarantine, overwrite, and delete trust gaps for the playable-slice durable
progress file. The remaining release-adjacent risk was repeated use: save/load
trust needed evidence across multiple route/search/return cycles, not only one
short path.

This story adds an automated headless soak probe. It is not a replacement for
human long-session QA, final windowed capture, or release sign-off.

## Acceptance Criteria

- [x] GIVEN a fresh SessionShell, WHEN the long-session probe starts, THEN durable progress is cleared and Load starts disabled.
- [x] GIVEN repeated route/search cycles, WHEN each cycle reaches mid-exploration pressure, THEN hull state remains valid and the Exploration HUD is active.
- [x] GIVEN repeated saves over existing durable progress, WHEN overwrite confirmation is required, THEN the probe confirms it and persistence generation advances.
- [x] GIVEN repeated loads, WHEN each cycle loads progress, THEN the Exploration HUD, pressure step, and canonical Persistence load status restore.
- [x] GIVEN repeated Hub returns, WHEN rewards are extracted, THEN carried rewards clear and storage retains returned rewards.
- [x] GIVEN the final durable progress remains, WHEN the final load runs, THEN it restores the latest saved generation and pressure step.
- [x] GIVEN this evidence is read, WHEN remaining risk is assessed, THEN this is automated soak coverage, not final human long-session QA or Release readiness.

## Implementation Notes

- Keep the probe in `tests/smoke/` so it can run with Godot headless.
- Use existing `HubRuntime` debug helpers and public button handlers.
- Exercise the overwrite-confirmation path introduced in Story 011.
- Keep cycle count bounded so the smoke remains fast and deterministic.

## Completion Notes

- Completed 2026-05-23.
- Added `tests/smoke/session_shell_long_session_probe.gd`.
- The probe runs three route/search/save/load/return cycles, confirms overwrites when required, checks generation advancement, checks canonical load status, and verifies final latest-state restoration.
- Remaining scope is not blocking for this story: human long-session QA, final windowed QA, release gate checklist, named save slots, and full save-management UI remain downstream.
