# Polish Story 008 Evidence: Save-Slot UX and Continue Trust

> Date: 2026-05-23  
> Story: `production/polish-backlog/story-polish-008-save-slot-ux-continue-trust.md`  
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- `HubRuntime` now presents load availability as a runtime affordance, not only a technical save/load operation.
- With no durable progress, the Hub disables Load and shows "暂无可加载进度".
- After a durable save, feedback states local progress is loadable.
- On restart with valid durable progress, the Hub enables Load and shows "检测到本地进度".
- With corrupt durable progress, the Hub disables Load and reports checksum failure instead of restoring stale state.
- Runtime authority remains C# / Godot .NET; no GDScript runtime authority or parallel save schema was introduced.

## Automated Checks

- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 5 existing warnings, 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd`
  - PASS.
  - Covers no-save disabled Load affordance, no-progress status text, durable save feedback, restart detection, restart load restore, corrupt-file disabled Load affordance, and checksum failure status text.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Existing domain-backed route/search/onboarding/save/load loop still passes after affordance changes.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.098 / 6.909 / 13.222 ms.
  - Peak static memory: 56.347 MiB.
  - Save p50/p95/max: 6.911 / 10.738 / 10.738 ms.
  - Load p50/p95/max: 6.888 / 17.041 / 17.041 ms.
  - Route departure: 18.592 ms.
  - Return Hub: 13.824 ms.

## Remaining Conditions

- This is not a final save-slot browser.
- Backup/quarantine UI, delete/overwrite confirmation, save naming, and full-game slot migration remain downstream.
- Long-session and final-window manual QA remain downstream.
- No Release readiness claim is made by this evidence.
