# Story 002: UI and Domain Event Integration

> **Epic**: Onboarding and First Loop
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: M / 6-8 hours
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/onboarding-first-loop.md`
**Requirement**: `TR-onboarding-001`

**ADR Governing Implementation**: ADR-0017: Onboarding and First Loop Guidance; ADR-0012: UI Input Routing and Dual Focus
**ADR Decision Summary**: #18 consumes UI/domain/session events after their owning systems mutate state. UIManager remains focus/input owner and #18 only observes panel, focus, route, pressure, save/load, and return-summary facts.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Integration must respect Godot 4.6 dual-focus behavior and active panel isolation. Do not connect/disconnect signals from frame loops.

**Control Manifest Rules (Presentation layer)**:
- Required: cross-system signals must be typed and emit after mutation.
- Forbidden: no Dictionary payload cross-system signals; no direct domain mutation from onboarding.
- Guardrail: integration handlers should be constant-time and should not run per frame.

---

## Acceptance Criteria

*From GDD `design/gdd/onboarding-first-loop.md`, scoped to this story:*

- [x] GIVEN the player reaches Hub, WHEN Hub UI is visible and input reachable, THEN `find_hub_hud` can complete.
- [x] GIVEN the player opens Chart by mouse or keyboard, WHEN Chart is active, THEN `open_chart` can complete and Hub guidance is hidden.
- [x] GIVEN a route is selected and departure is confirmed, WHEN Chart/Hub domain events are observed after mutation, THEN `select_route` and `depart_route` can complete.
- [x] GIVEN Exploration resource/threat/hull feedback changes, WHEN pressure feedback is visible, THEN `advance_pressure` can complete without covering feedback labels.
- [x] GIVEN Save/Load entries are visible or used, WHEN the player notices or uses them, THEN `notice_save_load` can complete.
- [x] GIVEN the player returns to Hub and summaries change, WHEN cargo/storage/hull/route summaries update, THEN `return_hub` and `notice_summary_change` can complete.

---

## Implementation Notes

Derived from ADR-0017 and ADR-0012:

- Wire #18 to existing #16 UI/HUD events, Chart route events, Hub return events, and playable slice adapter diagnostics through typed C# calls/events.
- Consume events only after the owning system has mutated state.
- Do not let onboarding open/close panels, change route, change cargo, change hull, save/load, or force focus.
- When Chart or Exploration is active, suppress stale Hub hints.
- Treat current Sprint 003 playable route as the first integration surface; broader systems can add events later without changing step IDs.
- Expose integration diagnostics for tests: observed events, completed steps, suppressed hints, and active surface.

---

## Out of Scope

- Story 001 owns scoring/state rules.
- Story 003 owns save/load persistence.
- Story 004 owns rendered hint/highlight Controls.
- Story 005 owns end-to-end smoke and manual evidence.

---

## QA Test Cases

- **AC-1**: Hub visibility completes first step
  - Given: Hub UI is visible and input is reachable
  - When: onboarding consumes the Hub-visible event
  - Then: `find_hub_hud` completes
  - Edge cases: Hub hidden under modal, input locked, duplicate visibility event

- **AC-2**: Chart activation completes chart step and suppresses Hub hints
  - Given: a visible Hub hint and Chart opens
  - When: onboarding consumes Chart-active event
  - Then: `open_chart` completes and Hub hints are hidden/suppressed
  - Edge cases: Chart closes immediately, Chart opens via keyboard, Chart opens via mouse

- **AC-3**: Route and departure events complete in order
  - Given: Chart is active
  - When: route selected and departure confirmed events arrive after mutation
  - Then: `select_route` and `depart_route` complete in order
  - Edge cases: departure without route, route selection changes, failed departure

- **AC-4**: Exploration pressure feedback completes pressure step
  - Given: Exploration surface is active
  - When: resource/threat/hull feedback changes visibly
  - Then: `advance_pressure` completes
  - Edge cases: feedback text unchanged, hidden surface, repeated pressure changes

- **AC-5**: Save/load visibility or use completes awareness step
  - Given: Save/Load controls are visible
  - When: the player focuses, dwells on, or uses Save/Load
  - Then: `notice_save_load` completes
  - Edge cases: Save/Load disabled while Chart is open, failed save, failed load

- **AC-6**: Return summary completes final steps
  - Given: player returns from Exploration to Hub
  - When: cargo/storage/hull/route summaries change
  - Then: `return_hub` and `notice_summary_change` complete
  - Edge cases: return with no changes, load restores pre-return state, duplicate return event

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/onboarding-first-loop/EventIntegrationTest.csproj` -- must exist and pass

**Status**: [x] Created and passing

**Evidence**:
- `dotnet run --project tests/integration/onboarding-first-loop/EventIntegrationTest.csproj` -- PASS, 7/7 checks.
- `dotnet run --project tests/unit/onboarding-first-loop/StepStateHintScoringTest.csproj` -- PASS, 6/6 checks.
- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` -- PASS, 30/30 checks.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` -- PASS, 0 warnings, 0 errors.

---

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003, Story 004, Story 005

## Completion Notes

- Extended `OnboardingManager` with typed UI and playable-slice integration handlers, active-surface diagnostics, observed-event snapshots, and stale Hub hint suppression.
- Added post-mutation typed events to `PlayableSliceDomainAdapter` for chart open, route selection, departure, exploration pressure, save/load use, and return-Hub summary changes.
- Added `tests/integration/onboarding-first-loop/EventIntegrationTest.csproj`, covering all six acceptance criteria plus a full adapter-connected eight-step completion regression.
- Onboarding still observes only; it does not open panels, force focus, mutate routes/cargo/hull, or perform save/load.
