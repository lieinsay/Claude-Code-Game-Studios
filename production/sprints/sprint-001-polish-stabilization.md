# Sprint 001 -- 2026-05-18 to 2026-05-22

## Sprint Goal

Stabilize the Production to Polish handoff by converting the remaining gate concerns into measurable Polish backlog work: numeric performance evidence, interaction pattern review, and explicit #17/#18 deferred scope.

## Context

- Stage: `Production`
- Gate report: `production/gate-checks/gate-check-production-to-polish-2026-05-15.md`
- Current gate verdict: `CONCERNS`
- Producer feasibility gate: skipped in Codex because repository `AGENTS.md` only allows subagents when the user explicitly requests delegated or parallel agents.
- Milestone source: no `production/milestones/` directory found.
- Risk register source: no `production/risk-register/` directory found.

## Capacity

- Total days: 5
- Buffer (20%): 1 day reserved for unplanned work
- Available: 4 days

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| P1-001 | Capture numeric Godot performance profile for the smoke loop | performance-analyst / developer | 1.0 | Current Hub -> Chart -> Exploration -> Save/Load -> Hub loop | `production/qa/perf-profile-production-to-polish-2026-05-15.md` is updated with average/worst frame time, peak memory, draw calls, and save/load p50/p95/max; any budget breach has a bug filed. |
| P1-002 | Review interaction pattern library against latest runtime bridge | ux-designer / ui-programmer | 0.75 | UI/HUD smoke loop fixed | `design/ux/interaction-patterns.md` covers Chart focus isolation, Exploration HUD pressure controls, Save/Load feedback, return-to-Hub, and Hub summary sync; mismatches are either corrected or filed. |
| P1-003 | Define #17 Feedback Polish scope brief | producer / game-designer | 0.75 | Accepted #17 deferral | Feedback backlog item defines priority events, MVP-vs-Polish boundary, acceptance criteria, and required QA evidence before VFX/audio implementation begins. |
| P1-004 | Define #18 Onboarding Polish scope brief | ux-designer / qa-lead | 0.75 | Accepted #18 deferral | Onboarding backlog item defines first-loop discoverability goals, highlight trigger scope, acceptance criteria, and manual QA checks. |
| P1-005 | Polish sprint QA sign-off | qa-lead | 0.75 | P1-001 through P1-004 | Sprint smoke passes, QA evidence is recorded, no S1/S2 bugs remain open, and QA sign-off is APPROVED or APPROVED WITH CONDITIONS. |

## Progress Notes

- **P1-001 complete — 2026-05-15:** `tests/smoke/session_shell_perf_probe.gd` passed in windowed Godot 4.6.2. Results recorded in `production/qa/perf-profile-production-to-polish-2026-05-15.md`. No budget breach found.
- **P1-002 complete — 2026-05-15:** `design/ux/interaction-patterns.md` now covers active panel focus isolation, Save/Load status feedback, Exploration HUD pressure loop, and return-to-Hub summary sync for the current runtime bridge.
- **P1-003 complete — 2026-05-15:** `production/polish-backlog/feedback-fx-audio-scope-brief-2026-05-15.md` defines the #17 first Polish implementation boundary; `design/gdd/feedback-fx-audio.md` is approved, ADR-0016 is accepted, and `production/epics/feedback-fx-audio/` contains 5 ready implementation stories.
- **P1-004 complete — 2026-05-15:** `production/polish-backlog/onboarding-first-loop-scope-brief-2026-05-15.md` defines the #18 first-loop onboarding boundary; `design/gdd/onboarding-first-loop.md` is approved and ADR-0017 is accepted.
- **P1-005 complete — 2026-05-15:** `production/qa/qa-signoff-sprint-001-polish-stabilization-2026-05-15.md` approves the Must Have scope with conditions.
- **P1-006 complete — 2026-05-15:** `production/qa/perf-profile-template.md` provides a reusable profiler evidence template for smoke, polish, and release-candidate captures.
- **P1-007 complete — 2026-05-15:** `production/qa/accessibility-evidence-sprint-001-polish-stabilization-2026-05-15.md` records desktop accessibility evidence for keyboard reachability, focus containment, visible feedback text, and non-color-only status.

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| P1-006 | Add reusable profiler evidence template or helper notes | performance-analyst / tools-programmer | 0.5 | P1-001 | Future perf captures have a repeatable checklist for frame time, memory, draw calls, save/load timing, and scenario notes. |
| P1-007 | Capture desktop accessibility evidence for the restored UI/HUD flow | qa-lead / accessibility-specialist | 0.5 | P1-002 | Manual evidence confirms keyboard reachability, focus containment, visible feedback text, and no hidden underlying Hub focus while Chart/Exploration UI is active. |

Status: both Should Have tasks complete.

### Nice to Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| P1-008 | Run optional director panel review | producer | 0.5 | User explicitly requests delegated agents | Creative, technical, production, and art concerns are summarized as follow-up risks without blocking already accepted scope. |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| #17 Feedback | Accepted deferred Polish/post-gate work from Production to Polish gate; scope brief, GDD, and ADR complete | 0 days remaining for brief/GDD/ADR |
| #18 Onboarding | Accepted deferred Polish/post-gate work from Production to Polish gate; scope brief, GDD, and ADR complete | 0 days remaining for brief/GDD/ADR |
| Numeric performance profile | Gate evidence was qualitative only; numeric smoke probe is now complete | 0 days remaining |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Future long-duration profile reveals a frame or save/load budget breach | Medium | Medium | File a bug and keep release sign-off conditional until the breach is resolved or explicitly accepted. |
| #17/#18 scope expands into full implementation | Medium | High | This sprint only creates scope briefs; implementation moves to later Polish stories unless a blocker is found. |
| Interaction pattern review finds UI spec drift | Medium | Medium | Correct the pattern library first, then create implementation tasks only for material behavior mismatches. |
| No director panel evidence | Low | Medium | Keep the gate verdict at CONCERNS unless the user explicitly requests delegated director review. |

## Dependencies on External Factors

- A local Godot desktop run with profiler or equivalent debug metrics.
- User or QA availability for manual desktop accessibility checks.
- Explicit user request if director panel subagents should be used.

## QA Plan

`production/qa/qa-plan-sprint-001-polish-stabilization-2026-05-15.md`

## Definition of Done for this Sprint

- [x] All Must Have tasks completed
- [x] All task acceptance criteria met
- [x] QA plan exists: `production/qa/qa-plan-sprint-001-polish-stabilization-2026-05-15.md`
- [x] Numeric performance evidence is captured or a blocking bug is filed
- [x] Interaction pattern review is completed
- [x] #17/#18 deferred scope is explicit and actionable
- [x] Smoke check passed: `/smoke-check sprint`
- [x] QA sign-off report is APPROVED or APPROVED WITH CONDITIONS
- [x] No S1 or S2 bugs remain open in delivered sprint scope
- [x] Gate report remains at least `CONCERNS` with no hard blockers reintroduced

## Scope Check

This sprint converts accepted gate concerns into Polish backlog work. If any task expands into implementation beyond the scope briefs above, run `/scope-check` before starting that work.
