# QA Sign-Off: Onboarding and First Loop

> Date: 2026-05-22
> Scope: Epic #18 Onboarding and First Loop, Stories 001-005
> Verdict: APPROVED WITH CONDITIONS for Polish continuation

## Decision

Epic #18 is complete for the current Polish entry scope. The first-loop onboarding guidance now has C# manager authority, typed event integration, canonical `progress.onboarding` persistence, focus-safe hint rendering, Godot runtime smoke evidence, and fresh performance evidence.

This sign-off does not claim release-candidate tutorial polish. It approves the #18 implementation stories as complete and carries remaining visual capture/runtime-hardening work into the normal Polish backlog.

## Evidence Reviewed

- `tests/unit/onboarding-first-loop/StepStateHintScoringTest.csproj` -- PASS 6/6.
- `tests/integration/onboarding-first-loop/EventIntegrationTest.csproj` -- PASS 7/7.
- `tests/integration/onboarding-first-loop/PersistenceSnapshotTest.csproj` -- PASS 6/6.
- `tests/integration/onboarding-first-loop/FocusSafeHintRenderingTest.csproj` -- PASS 7/7.
- `tests/integration/onboarding-first-loop/FirstLoopRegressionTest.csproj` -- PASS 6/6.
- `tests/integration/playable-slice/DomainAdapterTest.csproj` -- PASS 30/30.
- `tests/smoke/session_shell_visual_probe.gd` -- PASS under headless Godot.
- `tests/smoke/session_shell_perf_probe.gd` -- PASS under headless Godot.

## Acceptance Notes

- Automated and runtime evidence cover the playable first loop with onboarding enabled.
- Keyboard/spatial input and mouse-safe hint contracts remain valid; hints do not steal focus or mouse input.
- Canonical save/load restores onboarding progress and prevents completed hint replay.
- Disabled/reset behavior is covered at the integration-contract level: the base route is completable without onboarding wiring, and reset returns to the first eligible hint.
- Performance budgets pass after repairing stale GDScript method calls in the perf probe; headless frame gating uses p95 <= 16 ms plus a 20 ms single-frame transient ceiling.

## Conditions Carried Forward

- Capture a windowed screenshot/video pass during visual Polish if non-headless evidence is required.
- Continue Navigation/Exploration runtime hardening beyond the current playable fixture.
- Final art/audio treatment for onboarding hints remains out of scope for #18 implementation closeout.

## Verdict

APPROVED WITH CONDITIONS. Epic #18 Stories 001-005 may be marked Complete; no remaining #18 story is blocked.
