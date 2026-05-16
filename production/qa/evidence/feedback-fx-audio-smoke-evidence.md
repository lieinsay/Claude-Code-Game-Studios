# Feedback FX Audio Smoke Evidence

**Story:** `production/epics/feedback-fx-audio/story-005-smoke-regression-diagnostics-performance.md`  
**Type:** Integration  
**Status:** Automated evidence created and passing on 2026-05-16; runtime/manual smoke evidence remains valid from the current production-to-polish profile.

## Automated Evidence

| Evidence | Purpose | Status |
| --- | --- | --- |
| `tests/integration/feedback-fx-audio/SmokeRegressionTest.csproj` | Story 005 integrated smoke/regression checks for #17 router, fallbacks, diagnostics, load clearing, coalescing, focus safety, and numeric budget evidence. | PASS 6/6 |
| `tests/integration/ui-hud-interface/EdgeCasesDesktopA11yTest.csproj` | Existing UI/HUD desktop accessibility and focus regression probe. | PASS 25/25 |
| `tests/integration/session/ShellUiTest.csproj` | Existing Hub -> Chart -> Exploration -> Save/Load -> Hub runtime bridge regression probe. | PASS 18/18 |
| `tests/smoke/session_shell_perf_probe.gd` | Godot runtime numeric smoke probe for frame, memory, draw-call, save/load, and transition budgets. | PASS, exit 0 |

## Acceptance Criteria Mapping

| AC | Evidence |
| --- | --- |
| AC-1 Smoke loop survives #17 hooks | `SmokeRegressionProgram.test_smoke_loop_survives_feedback_hooks_without_ui_hud_regression`, plus existing Shell UI and UI/HUD a11y probes. |
| AC-2 Load clears stale transient cues | `SmokeRegressionProgram.test_load_clears_stale_transient_cues_while_allowing_load_complete_status`. |
| AC-3 Rapid save/load coalesces | `SmokeRegressionProgram.test_rapid_save_load_completion_coalesces_and_keeps_latest_status`. |
| AC-4 Missing assets keep smoke loop playable | `SmokeRegressionProgram.test_missing_assets_keep_smoke_loop_playable`. |
| AC-5 Diagnostics are sufficient and safe | `SmokeRegressionProgram.test_smoke_diagnostics_expose_routed_coalesced_skipped_and_fallback_decisions`. |
| AC-6 Numeric smoke remains in budget | `SmokeRegressionProgram.test_numeric_smoke_evidence_stays_within_budgets`, `tests/smoke/session_shell_perf_probe.gd`, and `production/qa/perf-profile-production-to-polish-2026-05-15.md`. |

## Numeric Smoke Budget Baseline

Current accepted budget evidence is recorded in `production/qa/perf-profile-production-to-polish-2026-05-15.md`:

- Windowed worst frame time: 3.980ms against <=16ms.
- Windowed peak static memory: 52.263MiB against <=512MiB total and <=200MiB Exploration peak.
- Windowed peak draw calls: 103 against <=400.
- Windowed save p95/max: 1.461ms / 1.461ms against p95 <50ms and max <100ms.
- Windowed load p95/max: 1.469ms / 1.469ms against p95 <50ms and max <100ms.
- Headless cross-check frame, memory, save, and load budgets pass; draw-call sampling is skipped under headless display driver.

## Deferred Runtime Notes

AC-1, AC-4, and AC-6 remain valid candidates for a future packaged-build or release-candidate smoke replay because final authored content can change frame and asset behavior. That follow-up is not a Story 005 blocker as long as automated integration evidence and the current numeric smoke probe pass.
