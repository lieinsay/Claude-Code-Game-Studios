# Polish 001 Navigation / Exploration Runtime Hardening Evidence

> Date: 2026-05-22
> Scope: Polish Story 001 -- Navigation / Exploration runtime hardening beyond the playable fixture
> Verdict: PASS for C# adapter regression, Godot headless visual smoke, perf probe, and solution build

## Evidence Summary

- `PlayableSliceDomainAdapter` now uses `NavigationManager` to produce the playable route `EncounterContext`.
- `ExplorationManager` now consumes that context, owns the active exploration point, records runtime search points, and owns the threat substate after pressure.
- Resources, carried rewards, hull pressure, route commitment, onboarding progress, and canonical save/load remain under C# manager authority.
- Remaining fixtures are content/presentation fixtures: seeded MVP routes, playable search loot pools, and greybox interaction markers.

## Automated Checks

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 40/40 checks.
  - Adds NavigationManager `Arrived` / EncounterContext destination and ExplorationManager `Exploring` / search-point / threat-substate checks.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers Hub -> Chart -> Navigation EncounterContext -> ExplorationManager search/pressure -> canonical save/load -> return Hub with onboarding enabled.
  - Screenshot capture skipped because the current display driver is `headless`.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.599 / 6.899 / 12.136 ms.
  - Peak static memory: 55.586 MiB.
  - Save p50/p95/max: 6.909 / 21.032 / 21.032 ms.
  - Load p50/p95/max: 6.892 / 7.237 / 7.237 ms.
  - Route departure: 13.913 ms.
  - Return Hub: 13.789 ms.
  - Draw-call budget skipped under headless display driver.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS.
  - 5 existing warnings, 0 errors.

## Acceptance Mapping

| AC | Evidence |
|----|----------|
| AC-1 Navigation/Exploration entry contract | DomainAdapter runner verifies `NavigationState == Arrived`, `EncounterDestinationId == location.mist-short`, and `ExplorationPhase == Exploring`. |
| AC-2 Labels derive from C# snapshots | Godot smoke verifies resource, threat, hull, cargo, search-point, and Exploration substate values through `HubRuntime.DebugDomainSnapshot()`. |
| AC-3 Canonical save/load consistency | DomainAdapter and Godot smoke verify mid-exploration restore keeps step, carried rewards, hull pressure, Exploration active session, and onboarding next hint. |
| AC-4 Visual smoke | `session_shell_visual_probe.gd` PASS. |
| AC-5 Performance smoke | `session_shell_perf_probe.gd` PASS within current Polish thresholds. |
| AC-6 Docs updated | Story, document index, production flowchart, collaboration plan, and active session state updated. |

## Remaining Conditions

- Route seeding and search loot tables are still adapter-local MVP content fixtures.
- The playable route now consumes real C# Navigation/Exploration managers, but broader authored route tables and richer exploration scene content remain downstream Polish scope.
- No Release readiness claim is made by this evidence.
