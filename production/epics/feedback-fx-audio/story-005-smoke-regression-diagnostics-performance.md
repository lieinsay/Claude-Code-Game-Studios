# Story 005: Smoke Regression, Diagnostics and Performance

> **Epic**: Feedback, VFX, and Audio Semantics
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/feedback-fx-audio.md`
**Requirement**: `TR-feedback-001`, `TR-feedback-002`

**ADR Governing Implementation**: ADR-0016 and ADR-0019 validation criteria
**ADR Decision Summary**: The first #17 implementation is only complete when router, fallback, focus isolation, smoke loop, and performance evidence prove that feedback hooks did not regress the existing UI/HUD runtime bridge.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Verification must include missing asset fallback, audio-muted fallback, subtitle request emission, focus non-interference, and no added frame/save-load timing regressions.

**Control Manifest Rules (Presentation layer)**:
- Required: Existing UI/HUD and save/load smoke paths remain understandable without #17.
- Forbidden: #17 must not add Web lifecycle dependencies or introduce per-frame idle polling.
- Guardrail: Numeric smoke performance remains within current frame, memory, draw-call, and save/load timing budgets.

---

## Acceptance Criteria

*From GDD `design/gdd/feedback-fx-audio.md`, scoped to this story:*

- [ ] GIVEN automated or manual QA runs the Hub -> Chart -> Exploration -> Save/Load -> Hub smoke loop, WHEN #17 hooks are connected, THEN existing UI/HUD and accessibility regression checks still pass.
- [ ] GIVEN load restores a state with old queued feedback, WHEN load completes, THEN transient cue queue is cleared and load-complete status is still allowed.
- [ ] GIVEN rapid save/load spam occurs, WHEN repeated completion cues share a coalesce key, THEN repeated cues are coalesced while the latest status remains visible.
- [ ] GIVEN missing visual/audio assets are configured during smoke, WHEN the smoke loop runs, THEN the UI loop remains playable and no crash occurs.
- [ ] GIVEN diagnostics are inspected after smoke, WHEN requests were routed, skipped, coalesced, or downgraded to fallback, THEN diagnostics identify event ID, source system, cue family, priority, and fallback reason.
- [ ] GIVEN numeric smoke is run with #17 enabled, WHEN frame, memory, draw-call, and save/load timings are captured, THEN results remain within the current budgets.

---

## Implementation Notes

Derived from ADR-0016:

- Clear transient feedback queue on load; allow the load-complete status after the clear.
- Rate-limit repeated missing asset diagnostics so smoke logs remain readable.
- Diagnostics are for tests and QA; they should not expose mutable internal state or become player-facing debug UI.
- Preserve the existing smoke path: Hub/HUD visibility, Chart route departure, Exploration HUD pressure feedback, Save/Load, return-to-Hub, and Hub summary sync.
- Re-run relevant existing checks after #17 hooks are enabled: UI/HUD desktop a11y, session shell smoke, and numeric perf probe.

---

## Out of Scope

- Story 005 does not add new cue families beyond those wired in Stories 002-004.
- Long-duration release-candidate soak/performance testing is a later release-readiness task.
- Full audio mix quality review is out of scope.

---

## QA Test Cases

- **AC-1**: Smoke loop survives #17 hooks
  - Given: #17 router, fallbacks, and overlays are enabled
  - When: QA runs Hub -> Chart -> Exploration -> Save/Load -> Hub
  - Then: UI/HUD visibility, focus containment, save/load text, and Hub summary sync still pass
  - Edge cases: keyboard-only path; mouse-only path

- **AC-2**: Load clears stale transient cues
  - Given: a route or exploration feedback cue is queued
  - When: load completes
  - Then: old transient cue state is cleared and load-complete status is visible
  - Edge cases: caption active during load; missing asset diagnostic queued

- **AC-3**: Rapid save/load completion coalesces
  - Given: repeated save/load completion events inside the coalescing window
  - When: #17 processes them
  - Then: cues are merged or rate-limited and latest status remains visible
  - Edge cases: save and load use distinct coalesce keys; same key outside 0.25s

- **AC-4**: Diagnostics are sufficient and safe
  - Given: routed, skipped, fallback, and coalesced requests occurred during smoke
  - When: diagnostics are queried
  - Then: event ID, source system, cue family, priority, and fallback reason are available without mutable payload references
  - Edge cases: empty diagnostics after queue clear; rate-limited missing asset warning

- **AC-5**: Numeric smoke remains in budget
  - Given: the current numeric smoke probe runs with #17 enabled
  - When: frame time, memory, draw calls, and save/load p95 timings are captured
  - Then: they remain within the current MVP desktop budgets and no performance regression is recorded
  - Edge cases: asset-missing path; muted-audio path

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/feedback-fx-audio/SmokeRegressionTest.csproj` — must exist and pass
- `production/qa/evidence/feedback-fx-audio-smoke-evidence.md` — smoke/a11y/perf notes
- Existing probes remain passing: `tests/integration/ui-hud-interface/EdgeCasesDesktopA11yTest.csproj`, `tests/integration/session/ShellUiTest.csproj`, `tests/smoke/session_shell_perf_probe.gd`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001, Story 002, Story 003, Story 004
- Unlocks: #17 epic closeout and #18 onboarding story split/implementation planning
