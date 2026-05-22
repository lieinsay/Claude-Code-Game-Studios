# Polish Story 002 Evidence: Richer Exploration Scene Semantics

> Date: 2026-05-22  
> Story: `production/polish-backlog/story-polish-002-richer-exploration-scene-semantics.md`  
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- `HubRuntime.cs` now renders presentation-only Exploration semantics from the C# `PlayableSliceDomainAdapter.Snapshot`.
- Added dynamic route progress fill, active search-point label, threat-zone marker, threat semantic text, extraction cargo marker, and extraction status text.
- The smoke path verifies these semantics across entry, first search, pressure/threat, loop completion, return Hub, save/load, and onboarding hint state.
- Runtime authority remains C# / Godot .NET; no GDScript runtime authority was added.

## Automated Checks

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 40/40.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers dynamic search-point label, route progress strip, threat-zone visibility, extraction status, completed search marker, canonical save/load, and return-Hub onboarding state.
- `godot --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS under NVIDIA OpenGL windowed driver.
  - Captured `production/qa/evidence/polish-002-exploration-semantics-probe.png`.
  - Captured `production/qa/evidence/polish-002-final-hub-probe.png`.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.791 / 6.908 / 12.135 ms.
  - Peak static memory: 56.124 MiB.
  - Save p50/p95/max: 6.897 / 20.049 / 20.049 ms.
  - Load p50/p95/max: 6.898 / 8.278 / 8.278 ms.
  - Route departure: 18.043 ms.
  - Return Hub: 13.804 ms.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 5 existing warnings, 0 errors.

## Remaining Conditions

- Dynamic scene semantics are greybox presentation polish over MVP fixture data.
- Final authored Exploration art, route tables, and richer authored search content remain downstream Polish scope.
- No Release readiness claim is made by this evidence.
