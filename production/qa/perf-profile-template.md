# Performance Profile Evidence Template

Use this template when capturing future smoke, polish, release-candidate, or regression performance evidence.

## Header

**Date:** YYYY-MM-DD
**Build / Commit:** TBD
**Engine:** Godot 4.6.2 .NET + C#
**Runtime Mode:** Windowed desktop / exported debug / exported release / headless
**Machine:** CPU, GPU, RAM, OS
**Scenario:** Hub / Chart / Exploration / Save-Load / Long-duration soak / Release candidate
**Verdict:** PASS / PASS WITH LIMITATIONS / FAIL

## Commands

```powershell
& 'D:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe' --path . --script tests/smoke/session_shell_perf_probe.gd
```

Add any alternate command, exported build path, or profiler setup notes here.

## Budget Table

| Metric | Budget | Measured | Status | Notes |
| --- | --- | --- | --- | --- |
| Frame time | 16ms gameplay frame budget, 60fps target | TBD avg / TBD worst | TBD | Capture at least 300 frames for smoke. |
| Memory | 512MiB MVP desktop soft ceiling, peak exploration <=200MiB | TBD peak | TBD | Use static memory or OS working set consistently. |
| Draw calls | <=400 for MVP desktop 2D scenes | TBD peak | TBD | Windowed run required; headless may report 0. |
| Boot to Hub | <2s warm local build | TBD | TBD | Start timer at scene instantiate or process launch, note method. |
| Chart open/close | <500ms transition budget | TBD avg / TBD worst | TBD | Run at least 10 cycles. |
| Save | p95 <50ms, max <100ms | TBD p50 / TBD p95 / TBD max | TBD | Run at least 10 cycles. |
| Load | p95 <50ms, max <100ms | TBD p50 / TBD p95 / TBD max | TBD | Run at least 10 cycles. |
| Route departure | <500ms transition budget | TBD | TBD | Include Chart -> Exploration or final authored equivalent. |
| Return Hub | <500ms transition budget | TBD | TBD | Include summary sync. |

## Scenario Checklist

- [ ] Entry shell reaches Hub.
- [ ] Hub idles for a representative sample.
- [ ] Chart opens and closes repeatedly.
- [ ] Route selection and departure execute.
- [ ] Exploration or authored gameplay loop advances.
- [ ] Save and Load run repeatedly.
- [ ] Return Hub completes and summary syncs.
- [ ] Any spike above 50ms is noted with scenario context.
- [ ] Any budget breach has a bug ID or accepted risk.

## Raw Results

Paste tool output or profiler measurements here.

```text
TBD
```

## Interpretation

- **Pass criteria:** all required metrics are under budget, no visible stutter, no crash, no progressive slowdown.
- **Pass with limitations:** smoke metrics pass but release-representative content, long-duration profile, or exported build profile is missing.
- **Fail criteria:** any hard budget breach without accepted risk, crash, progressive slowdown, or data-loss issue.

## Follow-up

| Item | Owner | Due | Status |
| --- | --- | --- | --- |
| TBD | TBD | TBD | TBD |
