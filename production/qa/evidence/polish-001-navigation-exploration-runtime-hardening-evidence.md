# Polish 001 Navigation / Exploration Runtime Hardening Evidence

> Date: 2026-05-22
> Scope: Polish Story 001 -- Navigation / Exploration runtime hardening beyond the playable fixture
> Verdict: PASS for C# adapter regression, Godot headless visual smoke, perf probe, and solution build

## Evidence Summary

- `PlayableSliceDomainAdapter` now uses `NavigationManager` to produce the playable route `EncounterContext`.
- `ExplorationManager` now consumes that context, owns the active exploration point, records runtime search points, and owns the threat substate after pressure.
- Resources, carried rewards, hull pressure, route commitment, onboarding progress, and canonical save/load remain under C# manager authority.
- Seeded MVP routes, starting modules, initial resources, and playable search loot pools now live in `src/presentation/playable_slice_runtime_fixture.json` instead of being hardcoded in adapter methods.
- Navigation preflight now queries `ModuleHullManager.CanDepart()` after fixture-installed starting modules, instead of accepting all route departures.
- Windowed visual evidence is captured at `production/qa/evidence/polish-001-windowed-session-shell-hub-probe.png`.
- Remaining fixture scope is content/presentation only: the fixture JSON and greybox interaction markers are still MVP Polish content, not final authored route/exploration content.

## Automated Checks

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  - PASS 40/40 checks.
  - Adds NavigationManager `Arrived` / EncounterContext destination and ExplorationManager `Exploring` / search-point / threat-substate checks.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers Hub -> Chart -> Navigation EncounterContext -> ExplorationManager search/pressure -> canonical save/load -> return Hub with onboarding enabled.
  - Screenshot capture skipped because the current display driver is `headless`.
- `godot --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS under NVIDIA OpenGL windowed driver.
  - Captures `production/qa/evidence/polish-001-windowed-session-shell-hub-probe.png`.
  - The smoke now asserts final Hub screenshot state has no stale return-Hub onboarding hint.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.616 / 6.906 / 12.192 ms.
  - Peak static memory: 55.586 MiB.
  - Save p50/p95/max: 6.899 / 19.053 / 19.053 ms.
  - Load p50/p95/max: 6.922 / 7.177 / 7.177 ms.
  - Route departure: 14.715 ms.
  - Return Hub: 13.818 ms.
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

- Route seeding and search loot tables are now data-backed MVP content fixtures, not adapter-local code branches.
- The playable route consumes real C# Navigation/Exploration managers, but broader authored route tables and richer exploration scene content remain downstream Polish scope.
- No Release readiness claim is made by this evidence.
