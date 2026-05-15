# Performance Profile: Production to Polish Gate

**Generated:** 2026-05-15
**Scope:** Static, smoke-evidence, qualitative desktop runtime observation, and numeric Godot smoke probe for Production to Polish gate
**Status:** NUMERIC SMOKE PASS WITH LIMITATIONS - not a long-duration release profiler capture

## Performance Budgets

| Metric | Budget | Current Evidence | Status |
| --- | --- | --- | --- |
| Frame time | 16ms gameplay frame budget, 60fps target | Windowed Godot smoke probe: avg 0.507ms, worst 3.980ms across 356 frame samples. Headless cross-check: avg 6.803ms, worst 8.265ms. | PASS |
| Memory | 512MB MVP desktop soft ceiling, peak exploration <=200MB | Windowed Godot smoke probe peak static memory: 52.263MiB. Headless cross-check peak static memory: 48.320MiB. | PASS |
| Draw calls | <=400 for MVP desktop 2D scenes | Windowed Godot smoke probe peak draw calls: 103. Headless draw-call sampling unavailable by display driver. | PASS |
| Desktop boot time | <2s from `boot_requested` to `session_ready` on warm local build | Windowed Godot smoke probe measured boot-to-Hub at 75.655ms. Headless cross-check: 99.321ms. | PASS |
| Save/load | 2MB snapshot, p95 <50ms encode + SHA-256, max 100ms | Windowed Godot smoke probe over 10 cycles: save p50/p95/max 1.075/1.461/1.461ms; load p50/p95/max 0.959/1.469/1.469ms. | PASS |
| Scene transition | <500ms exit cleanup + instantiate + `_Ready()` | Windowed Godot smoke probe: route departure 4.554ms, return Hub 3.202ms, Chart open/close avg/worst 2.156/3.368ms. | PASS |

## Numeric Smoke Probe

**Date:** 2026-05-15
**Probe:** `tests/smoke/session_shell_perf_probe.gd`
**Windowed command:** `D:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe --path . --script tests/smoke/session_shell_perf_probe.gd`
**Windowed renderer:** OpenGL API 3.3.0 NVIDIA 596.21, NVIDIA GeForce RTX 3070 Ti
**Headless command:** same probe with `--headless`

Windowed results:

- Frame samples: 356
- Frame time: avg 0.507ms, worst 3.980ms
- Peak static memory: 52.263MiB
- Peak draw calls: 103
- Boot to Hub: 75.655ms
- Chart open/close: avg 2.156ms, worst 3.368ms across 10 cycles
- Save: p50 1.075ms, p95 1.461ms, max 1.461ms across 10 cycles
- Load: p50 0.959ms, p95 1.469ms, max 1.469ms across 10 cycles
- Route departure: 4.554ms
- Return Hub: 3.202ms

Headless cross-check:

- Frame samples: 356
- Frame time: avg 6.803ms, worst 8.265ms
- Peak static memory: 48.320MiB
- Boot to Hub: 99.321ms
- Save p95: 6.920ms
- Load p95: 6.927ms
- Draw-call budget skipped because the headless display driver does not report render draw calls.

Budget result: all measurable smoke-loop budgets passed. No performance bug was filed.

## Manual Runtime Observation

**Date:** 2026-05-15
**Tester:** Internal user QA
**Duration:** Qualitative short run across Hub, Chart, Exploration HUD, and Save/Load paths

Observed result:

- No visible stutter.
- No crash.
- No progressive slowdown.
- Save/Load remained timely.

This was superseded by the numeric smoke probe above. The manual observation remains useful as qualitative confirmation that no visible stutter, crash, progressive slowdown, or delayed Save/Load feedback was observed.

## Static Hotspots Identified

| # | Location | Issue | Estimated Impact | Fix Effort |
| --- | --- | --- | --- | --- |
| 1 | `src/core/navigation/NavigationManager.cs:678` | Voyage time advancement can loop through repeated checks; needs runtime bound validation with long deltas. | Medium if large resume deltas are common | M |
| 2 | `src/core/interaction/InteractionRegistry.cs:794` | `Tick(double nowSeconds)` is a candidate frame-path entry. Candidate count and sorting/filtering cost should be profiled in populated Hub/Exploration scenes. | Medium | M |
| 3 | `src/feature/ExplorationManager.cs:440` | `ExtractionTick(double delta)` is a timed gameplay path and should be measured during extraction progress. | Low to Medium | S |
| 4 | `src/presentation/UIManager.cs:2621` | Semantic event log scan is bounded by current log size in tests, but should be checked during rapid UI event bursts. | Low | S |

## Current Positive Evidence

- Full C# project sweep passes: 115/115.
- Godot runtime smoke probe passes all UI/HUD checks.
- UI/HUD smoke reports no manual visible performance issue in the latest batch notes.
- Manual runtime observation reports no stutter, no crash, no progressive slowdown, and timely Save/Load feedback.
- Numeric windowed Godot smoke probe passes frame time, memory, draw-call, Save/Load, and scene transition budgets for the current visible loop.
- UI/HUD implementation disables underlying Hub controls while Chart is open, reducing duplicate input/focus work.

## Gaps

- No long-duration 5-minute profile across populated authored content.
- No exported release build profile.
- No overdraw capture.
- No stress profile for high-content Hub, Chart, or Exploration scenes.

## Recommended Runtime Profiling Procedure

1. Run the desktop build in the Godot editor or exported debug build.
2. Capture frame time and memory in these paths:
   - Entry shell to Hub.
   - Hub idle for 60 seconds.
   - Open/close Chart 10 times.
   - Save and load 10 times.
   - Route selection and departure attempt.
   - If exploration is wired, Hub to Chart to Exploration to Hub for 5 minutes.
3. Record:
   - Average and worst frame time.
   - Peak memory.
   - Draw calls during Hub and Chart.
   - Any frame hitch above 50ms.
   - Save/load p50, p95, and max time.

## Verdict

This document satisfies the current numeric performance smoke requirement for the visible runtime path. It does **not** replace a long-duration release-candidate profile across final authored content; that should still be captured before Release.
