# Polish Story 003 Evidence: Authored Route / Search Content Slice

> Date: 2026-05-22  
> Story: `production/polish-backlog/story-polish-003-authored-route-search-content-slice.md`  
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- Playable route/search data moved from MVP fixture naming into `src/presentation/playable_slice_authored_content.json`.
- Content now carries `content_version = polish-003-authored-route-search-v1` and `content_status = polish_authored`.
- Route and search-point authored text are consumed by `PlayableSliceDomainAdapter` and exposed through C# runtime snapshots.
- Runtime authority remains C# / Godot .NET; authored content only feeds existing managers and presentation labels.

## Automated Checks

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 43/43.
  - Covers authored content version/status and search-point display name in adapter snapshot.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers authored content version/status, authored search-point name, authored route display after load, and existing runtime/onboarding/save-load loop.
- `godot --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS under NVIDIA OpenGL windowed driver.
  - Captured `production/qa/evidence/polish-003-authored-content-exploration-probe.png`.
  - Captured `production/qa/evidence/polish-003-final-hub-probe.png`.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.694 / 6.873 / 12.270 ms.
  - Peak static memory: 56.123 MiB.
  - Save p50/p95/max: 6.901 / 22.844 / 22.844 ms.
  - Load p50/p95/max: 6.900 / 9.139 / 9.139 ms.
  - Route departure: 16.997 ms.
  - Return Hub: 13.805 ms.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 5 existing warnings, 0 errors.

## Remaining Conditions

- This is an authored MVP content slice, not the final content authoring pipeline.
- Final route/search table scale-up, authored Exploration art, and long manual play evidence remain downstream Polish scope.
- No Release readiness claim is made by this evidence.
