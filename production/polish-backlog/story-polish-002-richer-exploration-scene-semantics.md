# Polish Story 002: Richer Exploration Scene Semantics

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Runtime Scene Semantics
> **Type**: Polish
> **Estimate**: S / 0.5-1 day
> **Governing ADRs**: ADR-0013 Exploration Scavenge, ADR-0010 Navigation Route Risk, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 001 Navigation / Exploration runtime hardening

## Context

Polish Story 001 moved the playable runtime path onto `NavigationManager` and
`ExplorationManager`, closed the adapter-local fixture risk, and captured
windowed visual evidence. The next narrow Polish risk is presentation semantics:
the Exploration greybox scene proves reachability, but its route progress,
active search point, threat zone, and extraction affordance are mostly static.

This story adds dynamic, domain-snapshot-backed scene semantics while keeping
runtime authority in C# managers and keeping fixture content as MVP Polish data.

## Acceptance Criteria

- [x] GIVEN the player enters Exploration, WHEN the scene appears, THEN it shows a dynamic route progress strip, active search-point readout, and extraction status derived from the current C# runtime snapshot.
- [x] GIVEN the player performs the first search, WHEN the scene updates, THEN the active search-point readout references the recorded ExplorationManager search point and the progress strip advances.
- [x] GIVEN the second search triggers pressure, WHEN the scene updates, THEN a visible threat-zone semantic marker appears and the text reflects the manager-owned threat substate/hull pressure.
- [x] GIVEN the third search completes the loop, WHEN the scene updates, THEN the extraction affordance clearly changes to a return/settlement state without changing domain authority.
- [x] GIVEN the Godot smoke runs, WHEN it reaches Exploration and return-Hub, THEN existing runtime, onboarding, save/load, and performance assertions still pass.

## Implementation Notes

- Keep `HubRuntime.cs` as the Godot scene script and presentation owner.
- Read from `PlayableSliceDomainAdapter.Snapshot`; do not add GDScript runtime authority.
- Presentation-only markers may be added in `HubRuntime.cs`.
- Do not expand to final authored content or Release readiness.

## Required Evidence

- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
- QA evidence note documenting the dynamic scene semantics.

## Completion Notes

- Completed 2026-05-22.
- `HubRuntime.cs` now adds presentation-only dynamic Exploration semantics: route progress fill, search-point semantic label, threat-zone marker, threat semantic label, extraction cargo marker, and extraction semantic label.
- The semantic layer is refreshed from `PlayableSliceDomainAdapter.Snapshot`; it does not decide search, threat, extraction, or persistence outcomes.
- Smoke coverage now asserts initial semantic state, first-search point binding, progress-strip advance, pressure threat-zone visibility, threat text sync, settlement-ready extraction text, and completed search marker text.
- Windowed evidence captured at `production/qa/evidence/polish-002-exploration-semantics-probe.png` and `production/qa/evidence/polish-002-final-hub-probe.png`.
- Remaining Polish boundary: this is still greybox presentation semantics over the authored MVP content slice, not final authored exploration art/content.
