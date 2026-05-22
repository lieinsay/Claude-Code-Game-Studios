# Polish Story 007 Evidence: Cross-Launch Persistence Trust

> Date: 2026-05-22  
> Story: `production/polish-backlog/story-polish-007-cross-launch-persistence-trust.md`  
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- The playable slice now writes canonical promoted progress to `user://cloudweaver_playable_progress.json` after a successful `Persistence.RequestSaveProgress()`.
- Fresh runtime startup imports the durable progress manifest before load, then restores through canonical `Persistence.RequestLoadProgress()`.
- Imported durable progress now verifies the manifest checksum before domain restore.
- `Persistence` now restores domains in registered-deserializer order, preventing canonical JSON key ordering from changing resource/playable-slice restore semantics.
- Runtime authority remains C# / Godot .NET; no GDScript runtime authority or parallel save model was introduced.

## Automated Checks

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 127/127.
  - Covers canonical progress JSON export/import into a fresh `PlayableSliceDomainAdapter`.
  - Covers checksum rejection for corrupted durable progress JSON.
  - Restores saved screen, exploration step, last search point, carried rewards, and hull pressure.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 5 existing warnings, 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd`
  - PASS.
  - Covers saving in one SessionShell, freeing it, booting a fresh SessionShell, loading, and restoring the Exploration HUD from durable progress.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Existing playable loop still passes after durable persistence bridge.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.187 / 6.880 / 13.725 ms.
  - Peak static memory: 56.336 MiB.
  - Save p50/p95/max: 6.901 / 21.212 / 21.212 ms.
  - Load p50/p95/max: 6.911 / 16.239 / 16.239 ms.
  - Route departure: 17.385 ms.
  - Return Hub: 13.799 ms.

## Remaining Conditions

- This is a playable-slice durable progress bridge, not the final save-slot UX.
- Backup/quarantine UI and full-game save migration tooling remain downstream.
- Human QA should still re-run close/relaunch trust manually in a windowed build.
- No Release readiness claim is made by this evidence.
