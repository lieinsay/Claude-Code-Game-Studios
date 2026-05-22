# Onboarding First-Loop Smoke Evidence

> Date: 2026-05-22
> Scope: Epic #18 Story 005 -- First-Loop Smoke Regression and QA Evidence
> Verdict: PASS for automated C# regression, Godot headless smoke, and fresh performance probe

## Evidence Summary

- Runtime onboarding is connected through C# `HubRuntime`, `PlayableSliceDomainAdapter`, `OnboardingManager`, and canonical `Persistence`.
- The Godot smoke route completes Hub -> Chart -> route selection -> Exploration -> Save/Load awareness -> return Hub with onboarding enabled.
- Completed onboarding steps persist through `progress.onboarding`; loading a mid-loop save resumes at `return_hub` instead of replaying the save/load hint.
- Runtime hint presentation remains non-modal and mouse-transparent through the C# scene label contract.
- The fresh performance probe no longer calls obsolete GDScript snake_case runtime methods and exits successfully under headless Godot.

## Automated Checks

- `dotnet run --project tests/integration/onboarding-first-loop/FirstLoopRegressionTest.csproj`
  - PASS 6/6 checks.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers runtime onboarding step progression, focus-safe hint mouse filter, domain-backed route/exploration/return, canonical save/load, and non-replay after load.
  - Screenshot capture skipped because the current display driver is `headless`.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.815 / 6.860 / 17.187 ms.
  - Peak static memory: 55.581 MiB.
  - Save p50/p95/max: 6.902 / 29.041 / 29.041 ms.
  - Load p50/p95/max: 6.914 / 8.971 / 8.971 ms.
  - Route departure: 13.789 ms.
  - Return Hub: 13.800 ms.
  - Draw-call budget skipped under headless display driver.
  - Headless transient frame policy: p95 must stay within 16 ms; single-frame worst must stay within a 20 ms spike ceiling.

## Acceptance Mapping

| AC | Evidence |
|----|----------|
| AC-1 Smoke regressions stay green | `session_shell_visual_probe.gd` PASS with onboarding progression and canonical save/load assertions. |
| AC-2 Keyboard/mouse walkthroughs complete | `FirstLoopRegressionTest.csproj` covers keyboard-valid and mouse-path-valid hint snapshots; Godot smoke covers keyboard/spatial input path. |
| AC-3 Completed hints do not replay after load | C# runner and Godot smoke both verify mid-loop load resumes at `return_hub`, not `notice_save_load`. |
| AC-4 Disabled/reset config remains completable | C# runner verifies base route completion without onboarding wiring and reset returns to first eligible hint. |
| AC-5 Performance budgets | Fresh Godot perf probe PASS; obsolete C# migration method-name mismatch repaired. |
| AC-6 QA sign-off prepared | See `production/qa/qa-signoff-onboarding-first-loop.md`. |

## Remaining Conditions

- Windowed screenshot capture is still skipped in headless CI-style runs; visual inspection can be repeated manually in the Godot editor if a pixel artifact is required.
- Navigation/Exploration runtime hardening beyond the current playable fixture remains a broader Polish backlog item, not an Epic #18 Story 005 blocker.
