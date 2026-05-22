# Polish Story 001: Navigation / Exploration Runtime Hardening

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Feature Integration
> **Type**: Integration
> **Estimate**: M / 1-2 days
> **Governing ADRs**: ADR-0013 Exploration Scavenge, ADR-0010 Navigation Route Risk, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Sprint 003 domain-backed playable slice; Epic #18 Onboarding Story 001-005 Complete

## Context

The current C# `HubRuntime` proves the playable vertical slice with real
`ChartManager`, `HubManager`, `ResourcesManager`, `ModuleHullManager`, canonical
`Persistence`, and #18 onboarding. The remaining Polish risk is narrower:
`PlayableSliceDomainAdapter.AdvanceExploration()` still uses a documented
minimum exploration fixture instead of consuming the fuller Navigation and
Exploration manager contracts that already passed headless story tests.

This story converts that risk into a measurable Polish hardening task without
reopening Production or replacing the C# runtime authority.

## Acceptance Criteria

- [x] GIVEN the runtime playable slice departs from Chart, WHEN exploration starts, THEN the runtime records a Navigation/Exploration entry contract instead of only incrementing a local fixture counter.
- [x] GIVEN exploration advances through search/pressure, WHEN resources, threat, hull, and route state change, THEN labels are derived from C# domain snapshots and not duplicated string-only fixture state.
- [x] GIVEN canonical save/load is used mid-exploration, WHEN the session restores, THEN Navigation/Exploration progress, carried rewards, hull pressure, and onboarding next-step state remain consistent.
- [x] GIVEN the Godot visual smoke runs, WHEN the route completes, THEN existing Hub -> Chart -> Exploration -> Save/Load -> Return + onboarding checks still pass.
- [x] GIVEN the performance smoke runs, WHEN budgets are sampled, THEN frame p95, memory, save/load p95, and transitions remain inside current Polish thresholds or a concrete variance is documented.
- [x] GIVEN the hardening work is complete, WHEN docs are updated, THEN `docs/document-index.md`, `docs/reference/production-flowchart.md`, and `docs/reference/multiplayer-collaboration-plan.md` cite this story as the active Polish runtime-hardening lane.

## Implementation Notes

- Keep `HubRuntime.cs` as the Godot scene script and presentation owner.
- Keep C# managers as runtime authority; do not introduce or revive `HubRuntime.gd`.
- Prefer extending `PlayableSliceDomainAdapter` over adding a second runtime adapter.
- Preserve the current smoke/debug APIs unless a replacement is added in the same change.
- If a fuller manager contract cannot be safely wired in one step, document the remaining fixture boundary and add a focused regression test proving the narrowed boundary.

## Required Evidence

- Focused C# integration runner for the hardened adapter path, or an extension of `tests/integration/playable-slice/DomainAdapterTest.csproj`.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
- Updated QA/evidence note if smoke or perf behavior changes.

## Dependencies

- Depends on: Epic #18 Story 005, Sprint 003 PVS3-001..PVS3-007
- Unlocks: broader Polish visual/audio polish and release-readiness review prep

## Completion Notes

- Completed 2026-05-22.
- `PlayableSliceDomainAdapter` now creates a real `NavigationManager` and `ExplorationManager` for the playable runtime path.
- Chart departure commits still flow through `ChartManager` and `HubManager`, then `NavigationManager` produces an `EncounterContext` and `ExplorationManager.EnterExplorationWithContext(...)` owns the active exploration point.
- Runtime search/pressure now records Exploration search points, uses `ExplorationManager.PerformSearch(...)`, applies threat state through `ExplorationManager.CheckThreatTrigger(...)`, and keeps Resources/Hull mutations in the existing C# managers.
- Canonical persistence now includes `progress.navigation` and `progress.exploration` adapter-owned snapshot wrappers alongside `progress.playable_slice`.
- Remaining Polish fixture boundary: route content, playable search loot pools, and the spatial greybox markers are still authored MVP fixtures. They are presentation/content fixtures, not runtime authority.
- Evidence: `tests/integration/playable-slice/DomainAdapterTest.csproj` PASS 40/40; `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS; `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd` PASS; `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 5 existing warnings.
