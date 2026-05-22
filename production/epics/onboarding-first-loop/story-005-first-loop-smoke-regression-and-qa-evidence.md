# Story 005: First-Loop Smoke Regression and QA Evidence

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

**ADR Governing Implementation**: ADR-0017: Onboarding and First Loop Guidance; ADR-0019: Desktop Godot .NET/C# Platform Pivot
**ADR Decision Summary**: #18 is complete only when the first-loop guidance can run through the existing Hub -> Chart -> Exploration -> Save/Load -> Hub path without regressing UI focus, persistence, smoke, accessibility, or performance evidence.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: This story must verify runtime behavior in Godot, not only headless C# manager state. The current fresh performance probe timeout from the Polish gate should be addressed here or tracked as a separate Polish blocker.

**Control Manifest Rules (Presentation layer)**:
- Required: desktop C# implementation, typed events, and no per-frame onboarding polling.
- Forbidden: onboarding must not replace UIManager focus/input routing or domain authority.
- Guardrail: existing frame, memory, save/load, and scene transition budgets must not regress.

---

## Acceptance Criteria

*From GDD `design/gdd/onboarding-first-loop.md`, scoped to this story:*

- [x] GIVEN the first-loop smoke path runs with onboarding enabled, WHEN the route completes, THEN no existing UI/HUD, save/load, focus, playable slice, or accessibility regression fails.
- [x] GIVEN keyboard-only and mouse-oriented walkthroughs are executed, WHEN hints are visible, THEN both complete Hub -> Chart -> Exploration -> Save/Load awareness -> return Hub.
- [x] GIVEN completed hints are saved and loaded, WHEN the route resumes, THEN completed hints do not replay and the next incomplete step remains eligible.
- [x] GIVEN onboarding is disabled or reset in test configuration, WHEN the first loop runs, THEN base UI remains understandable and route completion still works.
- [x] GIVEN performance smoke runs after onboarding integration, WHEN budgets are measured, THEN frame, memory, save/load, and transition budgets remain within current Polish entry thresholds or any variance is documented with a fix plan.
- [x] GIVEN QA sign-off is prepared, WHEN evidence is reviewed, THEN the report cites automated tests, Godot smoke, manual walkthroughs, accessibility checks, and any remaining conditions.

---

## Implementation Notes

Derived from ADR-0017 and the Production -> Polish gate conditions:

- Extend `tests/smoke/session_shell_visual_probe.gd` or add a focused onboarding smoke probe to cover visible hints and completed-step progression.
- Add focused integration tests for save/load restoration and event completion if not already covered by Stories 002/003.
- Re-run or repair `tests/smoke/session_shell_perf_probe.gd`; the gate recorded a headless timeout as a Polish entry condition.
- Produce QA evidence under `production/qa/evidence/` and a sign-off or checklist under `production/qa/` or `production/playtests/`.
- Keep any non-blocking tuning issue in the Polish backlog rather than reopening Production.

---

## Out of Scope

- Implementing new onboarding behavior not covered by Stories 001-004.
- Full tutorial campaign, cutscenes, voiceover, or final art/audio.
- Release-candidate long-duration profiling.

---

## QA Test Cases

- **AC-1**: Existing smoke regressions stay green
  - Given: onboarding is enabled
  - When: the Hub -> Chart -> Exploration -> Save/Load -> Hub smoke path runs
  - Then: existing visual smoke and onboarding-specific checks pass
  - Edge cases: Chart open focus isolation, load after return, repeated route attempt

- **AC-2**: Keyboard-only and mouse-oriented walkthroughs complete
  - Given: first-loop hints visible
  - When: one walkthrough uses keyboard-only path and another uses mouse-oriented path
  - Then: both complete the route without onboarding blocking input
  - Edge cases: shortcut before hint, click before hint, ignored hint

- **AC-3**: Save/load does not replay completed hints
  - Given: several onboarding steps are completed and saved
  - When: progress is loaded
  - Then: completed hints do not replay and next incomplete step remains eligible
  - Edge cases: load mid-Exploration, load after return Hub, reset state

- **AC-4**: Disabled/reset configuration does not hide UI defects
  - Given: onboarding disabled or reset in test configuration
  - When: first loop is attempted
  - Then: base UI route remains completable
  - Edge cases: no hint anchor, disabled hint layer, corrupted settings

- **AC-5**: Performance smoke is updated
  - Given: onboarding integration is present
  - When: performance smoke runs
  - Then: measurable budgets pass or a specific perf issue is filed with reproduction steps
  - Edge cases: headless driver timeout, draw-call monitor unavailable, transient frame spike

- **AC-6**: QA evidence is complete
  - Given: all story tests and manual checks are available
  - When: QA sign-off is written
  - Then: it cites automated tests, Godot smoke, manual walkthroughs, accessibility checks, and remaining risks
  - Edge cases: manual-only concern, skipped screenshot, performance condition

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/onboarding-first-loop/FirstLoopRegressionTest.csproj` or equivalent focused runner -- must exist and pass
- `tests/smoke/session_shell_visual_probe.gd` or onboarding smoke probe -- must pass
- `production/qa/evidence/onboarding-first-loop-smoke-evidence.md`
- `production/qa/qa-signoff-onboarding-first-loop.md`

**Status**: [x] Created and passing

**Evidence**:
- `tests/integration/onboarding-first-loop/FirstLoopRegressionTest.csproj` -- PASS, 6/6 checks.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` -- PASS, runtime onboarding + playable loop smoke.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd` -- PASS, frame/memory/save/load/transition budgets.
- `production/qa/evidence/onboarding-first-loop-smoke-evidence.md` -- created, PASS summary.
- `production/qa/qa-signoff-onboarding-first-loop.md` -- created, APPROVED WITH CONDITIONS.

---

## Dependencies

- Depends on: Stories 001-004
- Unlocks: Polish onboarding completion review

## Completion Notes

- Connected runtime onboarding through C# `HubRuntime`, `PlayableSliceDomainAdapter.RegisterOnboarding(...)`, and `OnboardingManager`; no GDScript runtime authority was reintroduced.
- Added `RuntimeHintLabel` updates and smoke diagnostics so Godot verifies first-loop progression and hint mouse transparency.
- Repaired `tests/smoke/session_shell_perf_probe.gd` after the C# runtime migration by replacing obsolete snake_case calls with C# method names.
- Added Story 005 focused regression runner and QA evidence/sign-off docs.
