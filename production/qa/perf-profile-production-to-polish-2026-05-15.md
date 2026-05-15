# Performance Profile: Production to Polish Gate

**Generated:** 2026-05-15
**Scope:** Static and smoke-evidence profile for Production to Polish gate
**Status:** PARTIAL - not a target-hardware runtime profile

## Performance Budgets

| Metric | Budget | Current Evidence | Status |
| --- | --- | --- | --- |
| Frame time | 16ms gameplay frame budget, 60fps target | No target-hardware frame capture available | MANUAL PROFILE NEEDED |
| Memory | 512MB MVP desktop soft ceiling, peak exploration <=200MB | No memory capture available | MANUAL PROFILE NEEDED |
| Draw calls | <=400 for MVP desktop 2D scenes | No render profiler capture available | MANUAL PROFILE NEEDED |
| Desktop boot time | <2s from `boot_requested` to `session_ready` on warm local build | Session shell smoke passes, but no reliable wall-clock boot sample captured | MANUAL PROFILE NEEDED |
| Save/load | 2MB snapshot, p95 <50ms encode + SHA-256, max 100ms | Save/load automated tests and UI feedback pass; no timing histogram captured | MANUAL PROFILE NEEDED |
| Scene transition | <500ms exit cleanup + instantiate + `_Ready()` | No measured transition timing available | MANUAL PROFILE NEEDED |

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
- UI/HUD implementation disables underlying Hub controls while Chart is open, reducing duplicate input/focus work.

## Gaps

- No Godot profiler capture.
- No target-hardware frame time sample.
- No memory sample after 5 minutes of Hub / Chart / Save / Load use.
- No draw-call or overdraw capture.
- No save/load timing histogram.

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

This document partially closes the performance evidence gap by identifying budgets and static profiling targets. It does **not** satisfy the Production to Polish performance gate until target-hardware runtime measurements are recorded.
