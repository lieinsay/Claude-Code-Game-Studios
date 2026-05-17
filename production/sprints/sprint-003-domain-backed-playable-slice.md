# Sprint 003 -- 2026-05-17 to 2026-05-24

## Sprint Goal

Convert the recovered greybox vertical slice from a smoke-state bridge into a
domain-backed playable slice: Hub, Chart, Exploration, Resources/Hull, feedback,
and save/load should run through the implemented C# managers or documented
Godot-to-C# adapters while preserving the human-playable route.

## Context

- Stage: `Production`
- Previous sprint: `production/sprints/sprint-002-playable-vertical-slice-recovery.md`
- Gate recheck: `production/gate-checks/gate-check-production-to-polish-2026-05-17-domain-recheck.md`
- Verdict: Production -> Polish is **FAIL** until the playable route is
  domain-backed and visually reads as a minimal game scene.

## Capacity

- Total days: 8
- Buffer (25%): 2 days reserved for integration risk
- Available: 6 days

## Tasks

### Must Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
| --- | --- | --- | --- | --- | --- |
| PVS3-001 | Define Godot-to-C# runtime adapter boundary | godot-csharp-specialist / developer | 0.75 | Existing C# managers | A short adapter note identifies which C# managers own Hub, Chart, Exploration, Resources/Hull, Feedback, and Persistence state; no new dependency is introduced. |
| PVS3-002 | Route Chart selection/departure through `ChartManager` and Navigation/Exploration entry contracts | godot-csharp-specialist / developer | 1.0 | PVS3-001 | The playable Chart route no longer relies solely on `_selected_route`; route availability, selection, and departure evidence come from domain calls or adapter fixtures. |
| PVS3-003 | Route search/resource/threat/hull feedback through domain managers | gameplay-programmer / developer | 1.5 | PVS3-001, PVS3-002 | Exploration search mutates domain-backed resource/threat/hull state or a documented adapter around those managers; UI labels reflect manager snapshots. |
| PVS3-004 | Replace smoke save/load with canonical persistence adapter | godot-csharp-specialist / developer | 1.0 | PVS3-001, PVS3-003 | Save/load uses the C# persistence pipeline or an explicit adapter around `Persistence`; no final gate evidence depends on `user://smoke_session_state.json`. |
| PVS3-005 | Minimum authored greybox scene pass | technical-artist / developer | 1.0 | PVS3-002 | Hub and Exploration surfaces have clear deck/world zones, landmarks, interaction affordances, and feedback placement beyond plain panel/status rows. |
| PVS3-006 | Domain-backed playable smoke probe | qa-lead / developer | 0.75 | PVS3-002..PVS3-005 | Godot smoke verifies movement, spatial interactions, domain-backed state mutation, canonical save/load restore, and return-to-Hub summary sync. |
| PVS3-007 | Manual playtest and QA sign-off for domain-backed slice | qa-lead / developer | 0.75 | PVS3-006 | A human completes the route without debug calls; QA sign-off is APPROVED or APPROVED WITH CONDITIONS for Production gate evidence. |

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
| --- | --- | --- | --- | --- | --- |
| PVS3-008 | Update UX interaction patterns for spatial/domain-backed route | ux-designer / developer | 0.5 | PVS3-005 | `design/ux/interaction-patterns.md` records the E-use spatial route, prompt behavior, save/load feedback, and domain-backed status pattern. |
| PVS3-009 | Fresh performance smoke after domain integration | performance-analyst / developer | 0.5 | PVS3-006 | Numeric smoke or perf evidence confirms frame, memory, transition, and save/load budgets still pass. |

## Risks

| Risk | Probability | Impact | Mitigation |
| --- | --- | --- | --- |
| C# managers are headless-first and not Godot-node-ready | High | High | Use a thin adapter or C# Node bridge with explicit ownership; do not rewrite managers. |
| Persistence API needs packaging data not exposed by HubRuntime | Medium | High | Start with a narrow session snapshot adapter and document any remaining canonical gap. |
| Greybox visual pass expands into art production | Medium | Medium | Limit to authored shapes, zones, labels, and feedback placement; no final art dependency. |
| Existing smoke tests assume direct GDScript calls | Medium | Medium | Keep debug helpers for tests but assert domain-backed labels/snapshots after actions. |

## QA Plan

- `production/qa/qa-plan-sprint-003-domain-backed-playable-slice-2026-05-17.md`

## Progress Notes

### 2026-05-17 -- PVS3-001 Complete

- Added `production/sprints/sprint-003-runtime-adapter-boundary.md`.
- Added `production/qa/qa-plan-sprint-003-domain-backed-playable-slice-2026-05-17.md`.
- Decision: `HubRuntime.cs` remains a Godot presentation/input shell; C# domain
  managers own Hub, Chart, Exploration, Resources/Hull, Feedback, and
  Persistence authority.
- Next implementation should start with PVS3-002/PVS3-003 by adding a thin
  Godot-friendly runtime adapter and moving route/search evidence off
  smoke-state fields.

### 2026-05-17 -- PVS3-002/PVS3-003 Complete

- Added `src/presentation/PlayableSliceDomainAdapter.cs` as a headless,
  Godot-wrap-ready adapter around `ChartManager`, `HubManager`,
  `ResourcesManager`, and `ModuleHullManager`.
- Migrated `src/scenes/HubRuntime.tscn` from `HubRuntime.gd` to the C# scene
  script `src/scenes/HubRuntime.cs`; the prior GDScript runtime authority was
  removed.
- Project binding fix: `CloudWeaverVoyage.csproj` now uses
  `Godot.NET.Sdk/4.6.2`, `project.godot` uses the matching assembly name, and
  `NuGet.config` points to the local GodotSharp SDK package source.
- Added `tests/integration/playable-slice/DomainAdapterTest.csproj`; the runner
  proves route opening, route selection, departure commit, Hub in-transit,
  ResourcesManager supply/reward mutation, ModuleHullManager hull damage, and
  return-to-Hub extraction snapshots.
- Updated `tests/smoke/session_shell_visual_probe.gd` to drive the C# scene
  script and assert Chart/Hub/Resources/Hull domain snapshots after the same
  playable movement/E-use path.
- Verification:
  - `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 5 existing warnings, 0 errors.
  - `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` PASS 22/22.
  - `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS; screenshot capture remains skipped under the headless display driver.
- Remaining Production risk: PVS3-004 canonical persistence is still open; the
  runtime continues to use the temporary smoke save file until that story
  replaces it.

### 2026-05-17 -- PVS3-004 Complete

- Replaced `HubRuntime.cs` save/load calls with
  `PlayableSliceDomainAdapter.SaveSceneState` and `LoadSceneState`; the scene no
  longer writes or reads `user://smoke_session_state.json`.
- `PlayableSliceDomainAdapter` now owns a `Persistence` pipeline and registers:
  `progress.resources`, `progress.airship.modules_hull`, `progress.airship`,
  `progress.routes`, and `progress.playable_slice`.
- `progress.playable_slice` records the presentation-local restore hints that
  canonical domain managers do not own: current screen, selected route,
  exploration step, player position, footer text, and carried exploration
  rewards.
- Updated `tests/integration/playable-slice/DomainAdapterTest.csproj`; the
  runner proves canonical save/load restores exploration screen, resource
  carried/storage state, and hull damage after an intervening return-to-Hub
  mutation.
- Updated `tests/smoke/session_shell_visual_probe.gd`; the Godot route now
  asserts canonical progress generation and canonical load status in the C#
  domain snapshot.
- Verification:
  - `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` PASS 30/30.
  - `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS.
  - `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 0 warnings, 0 errors on the final incremental run.
- Remaining Production risk: PVS3-005 minimum authored greybox scene pass and
  PVS3-006/PVS3-007 smoke/manual QA are still open before another
  Production -> Polish gate attempt.

### 2026-05-17 -- PVS3-005 Complete

- Added an authored greybox layer in `HubRuntime.cs` above the existing runtime
  surface:
  - Hub deck floor and rail.
  - Helm console prop and label.
  - Storage crate prop, band, and label.
  - Module bench prop and label.
  - Exploration sky field and route trail.
  - Search wreck prop/highlight/label.
  - Return beacon prop/core/label.
- The greybox layer is presentation-only; manager state remains owned by
  `PlayableSliceDomainAdapter` and the C# domain managers.
- Updated `tests/smoke/session_shell_visual_probe.gd` to assert that Hub props
  are visible in Hub mode, Exploration props are hidden in Hub mode, Exploration
  props appear after departure, and Hub props return after spatial return.
- Verification:
  - `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 5 existing warnings, 0 errors.
  - `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS.
- Remaining Production risk: PVS3-006 should formalize the final smoke evidence
  for the domain-backed greybox route, then PVS3-007 needs a human manual
  playtest / QA sign-off before another Production -> Polish gate attempt.

### 2026-05-17 -- PVS3-006 Complete

- Added automated evidence package:
  `production/qa/evidence/sprint-003-domain-backed-playable-smoke-evidence-2026-05-17.md`.
- Evidence maps the smoke route to QA-1 through QA-5: C# runtime bridge,
  Chart/Hub departure, Resources/Hull mutation, canonical Persistence save/load,
  and minimum greybox presentation.
- Verification:
  - `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` PASS 30/30.
  - `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS.
  - `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 5 existing warnings, 0 errors.
  - `git diff --check` PASS with LF/CRLF warnings only.
- Remaining Production risk: PVS3-007 manual playtest / QA sign-off is now the
  last must-have Sprint 003 blocker before another Production -> Polish gate
  recheck. Automated smoke evidence is not a Polish PASS.

### 2026-05-17 -- PVS3-007 Started

- Added manual playtest checklist:
  `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md`.
- Added pending QA sign-off:
  `production/qa/qa-signoff-sprint-003-domain-backed-playable-slice-2026-05-17.md`.
- PVS3-007 remains open until a human tester completes the checklist and fills
  the sign-off result. No Production -> Polish PASS is authorized yet.

## Definition of Done

- [ ] Must Have tasks PVS3-001 through PVS3-007 complete
- [x] Godot smoke probe passes and proves domain-backed state mutation
- [x] `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` passes
- [x] Relevant C# unit/integration tests pass
- [ ] Manual playtest confirms Hub -> Chart -> Exploration -> Return remains playable
- [x] Save/load uses canonical persistence or a documented adapter around it
- [x] Greybox presentation is sufficient for Production gate evidence
- [ ] Production -> Polish gate can be rechecked without relying on smoke-state stubs
