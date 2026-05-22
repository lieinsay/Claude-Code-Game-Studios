# Polish Story 005 Evidence: Route / Search ID Migration Guard

> Date: 2026-05-22  
> Story: `production/polish-backlog/story-polish-005-route-search-id-migration-guard.md`  
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- Added explicit route/search ID migration maps to `src/presentation/playable_slice_authored_content.json`.
- Added adapter-side ID resolution for legacy route IDs and legacy search-point IDs.
- The migration guard is validated before the existing playable runtime regression.
- Runtime authority remains C# / Godot .NET. The migration map is content metadata consumed by the C# adapter, not a new GDScript runtime authority.

## Automated Checks

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 118/118.
  - Covers migration map structure, legacy route display lookup, legacy route selection, and legacy route restoration through canonical progress load.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 5 existing warnings, 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Confirms the normal playable route/search/save/load loop still works after adapter-side ID migration resolution.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.697 / 6.866 / 13.016 ms.
  - Peak static memory: 56.123 MiB.
  - Save p50/p95/max: 6.896 / 21.882 / 21.882 ms.
  - Load p50/p95/max: 6.910 / 8.096 / 8.096 ms.
  - Route departure: 16.673 ms.
  - Return Hub: 13.846 ms.
- `git diff --check`
  - PASS with CRLF warnings only.

## Remaining Conditions

- This is a local playable route/search ID guard, not the final project-wide save migration framework.
- Future route/search renames still require explicit migration entries and focused regression evidence before deletion.
- Final route/search content scale-up, final authored art/audio, and long manual play evidence remain downstream Polish scope.
- No Release readiness claim is made by this evidence.
