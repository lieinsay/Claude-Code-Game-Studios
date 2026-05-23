# Polish Story 010 Evidence: Backup / Quarantine UX for Invalid Durable Progress

> Date: 2026-05-23
> Story: `production/polish-backlog/story-polish-010-backup-quarantine-ux.md`
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- Invalid durable playable progress is moved out of the active load path into `cloudweaver_playable_progress.quarantine.json`.
- The active durable progress file is removed after checksum failure, so stale progress is not offered for load.
- Hub boot and Load feedback explain that the invalid save was isolated and that the player can create a new safe save.
- A fresh save after quarantine recreates `cloudweaver_playable_progress.json`, re-enables Load, and restores continue trust.
- Runtime authority remains C# / Godot .NET; `PlayableSliceDomainAdapter` and `Persistence` remain canonical save/load authority.

## Automated Checks

- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 0 warnings, 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd`
  - PASS.
  - Covers valid durable save/restart/load, corrupt durable import, active-path removal, quarantine creation, disabled Load, quarantine feedback, safe re-save, and cleanup.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Existing domain-backed route/search/onboarding/save/load loop still passes after quarantine UX changes.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 4.971 / 6.940 / 12.467 ms.
  - Peak static memory: 56.360 MiB.
  - Save p50/p95/max: 6.888 / 22.946 / 22.946 ms.
  - Load p50/p95/max: 6.911 / 19.054 / 19.054 ms.
  - Route departure: 19.328 ms.
  - Return Hub: 13.855 ms.
- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 127/127.

## Remaining Conditions

- This is still a one-file playable-slice quarantine path, not a final save-slot browser.
- Player-facing backup selection, delete/overwrite confirmation, save naming, and full-game slot migration remain downstream.
- Long-session and final-window manual QA remain downstream.
- No Release readiness claim is made by this evidence.
