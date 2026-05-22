# Story 001: First-Loop Step State and Hint Scoring

> **Epic**: Onboarding and First Loop
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Estimate**: M / 6-8 hours
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/onboarding-first-loop.md`
**Requirement**: `TR-onboarding-001`

**ADR Governing Implementation**: ADR-0017: Onboarding and First Loop Guidance
**ADR Decision Summary**: Implement #18 as a C# `OnboardingManager` service that owns first-loop step state, scores eligible hints, emits hint/highlight requests, and never owns UI focus or gameplay input.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Godot dual-focus behavior affects later rendering stories; this logic story should remain headless C# and avoid direct Control dependencies.

**Control Manifest Rules (Presentation layer)**:
- Required: UIManager owns screen state, modal stack, input routing, and focus management.
- Forbidden: #18 must not mutate route, cargo, hull, repair, market, save, onboarding renderer, or UI focus state.
- Guardrail: Onboarding evaluation is O(number of onboarding steps) on relevant events; no per-frame polling.

---

## Acceptance Criteria

*From GDD `design/gdd/onboarding-first-loop.md`, scoped to this story:*

- [x] GIVEN a new first-loop session, WHEN onboarding initializes, THEN it tracks the eight stable steps: `find_hub_hud`, `open_chart`, `select_route`, `depart_route`, `advance_pressure`, `notice_save_load`, `return_hub`, and `notice_summary_change`.
- [x] GIVEN a step completion signal arrives, WHEN the step is incomplete, THEN the step becomes `completed` and first-loop progress updates deterministically.
- [x] GIVEN all prior steps are complete or eligible, WHEN the manager evaluates guidance, THEN it chooses the highest scoring eligible hint using `hint_priority_score = base_step_priority + blocker_bonus + time_unseen_bonus - completed_penalty - repeat_penalty`.
- [x] GIVEN a completed step, WHEN hints are evaluated again, THEN that step does not become visible unless onboarding state is reset.
- [x] GIVEN invalid, duplicate, or out-of-order events, WHEN they are consumed, THEN the manager remains deterministic and does not mark unrelated steps complete.

---

## Implementation Notes

Derived from ADR-0017:

- Add an `OnboardingManager` C# service or headless manager class before any UI rendering.
- Use ADR-0017 state names: `NotStarted`, `Eligible`, `Visible`, `Completed`, `Suppressed`.
- Use stable string step IDs from the GDD; do not generate IDs from display text.
- Add immutable hint request values equivalent to `OnboardingHintRequest(StepId, HintTextKey, HighlightAnchorId, Priority, DurationSeconds)`.
- Keep this story free of Godot `Control`, `Node`, mouse filter, or focus ownership logic.
- Expose read-only diagnostics for tests: current step state, progress percent, selected hint request, repeat counts, and last ignored event reason.

---

## Out of Scope

- Story 002 wires UI/domain/session events.
- Story 003 persists `progress.onboarding`.
- Story 004 renders hints/highlights in UIManager.
- Story 005 provides end-to-end smoke and QA evidence.

---

## QA Test Cases

- **AC-1**: Stable first-loop steps exist
  - Given: a fresh `OnboardingManager`
  - When: it initializes
  - Then: all eight GDD step IDs exist in `not_started` or first eligible state with stable ordering
  - Edge cases: duplicate step registration, missing step ID, unknown step lookup

- **AC-2**: Step completion is deterministic
  - Given: a manager with incomplete steps
  - When: the `open_chart` completion event is consumed after `find_hub_hud`
  - Then: only `open_chart` becomes completed and progress percent updates
  - Edge cases: duplicate completion event, completion event before prerequisite, unknown event

- **AC-3**: Hint scoring selects highest eligible hint
  - Given: multiple eligible hints with known base priority, blocker bonus, time unseen, and repeat count
  - When: the manager evaluates hints
  - Then: the highest computed score is selected deterministically
  - Edge cases: equal scores use stable step order; completed step receives completed penalty

- **AC-4**: Completed steps do not repeat
  - Given: a completed `select_route` step
  - When: hints are evaluated repeatedly
  - Then: `select_route` does not become visible again
  - Edge cases: reset clears completion; suppressed state does not erase completion

- **AC-5**: Invalid events are isolated
  - Given: a manager with known state
  - When: invalid, duplicate, and out-of-order events are consumed
  - Then: unrelated step state remains unchanged and diagnostics record the ignored event
  - Edge cases: null/empty step ID, unknown event type, repeated event burst

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/onboarding-first-loop/StepStateHintScoringTest.csproj` -- must exist and pass

**Status**: [x] Created and passing

**Evidence**:
- `dotnet run --project tests/unit/onboarding-first-loop/StepStateHintScoringTest.csproj` -- PASS, 6/6 checks.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` -- PASS, 5 existing warnings, 0 errors.

---

## Dependencies

- Depends on: ADR-0017 Accepted
- Unlocks: Story 002, Story 003

## Completion Notes

- Added `src/presentation/OnboardingManager.cs` as a headless C# manager with stable GDD step IDs, ADR-0017 states, deterministic completion ordering, progress diagnostics, hint scoring, repeat counts, and ignored-event reasons.
- Added `tests/unit/onboarding-first-loop/StepStateHintScoringTest.csproj` and focused runner coverage for all five acceptance criteria plus score cap/penalty regression.
- Kept this story free of Godot `Control`, `Node`, focus ownership, and gameplay-domain mutation.
