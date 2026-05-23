# Story 004: User Readability Review and Release Gate Handoff

> **Epic**: Complete Scene Composition and Acceptance
> **Status**: Complete
> **Layer**: Polish Gate / Production Scene Design
> **Type**: Visual/Feel
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused implementation session)

## Context

**GDD**: `design/gdd/scene-composition-system.md`
**Requirement**: `TR-scene-composition-002`
**Requirement Text**: Scene completion requires scene physics readiness, behavior readiness, state variant readiness, visual/audio readiness, technical contract readiness, automated evidence, Codex review, and user readability review.

**ADR Governing Implementation**: ADR-0016: Feedback/VFX/Audio Semantics; ADR-0017: Onboarding First Loop; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: feedback and onboarding can support scene clarity, but release-readiness still requires actual scene readability and desktop-validated evidence.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: HIGH
**Engine Notes**: human QA may use screenshots or windowed runs, but claims must include exact build/test context.
**Performance Note**: No runtime performance impact is expected for the checklist and release handoff documents. Any optional manual capture must record the build/smoke context rather than adding runtime systems.

**Control Manifest Rules (this layer)**:
- Required: semantic feedback and onboarding must not steal gameplay ownership.
- Forbidden: do not declare release-ready while P0 scene blockers remain.
- Guardrail: release handoff records waivers explicitly.

---

## Acceptance Criteria

- [x] GIVEN Codex review passes, WHEN the user reviews the same scene, THEN missing fantasy, missing requirements, unclear identity, or undesirable player flow can still block the scene.
- [x] GIVEN either Codex or user review reports BLOCKED, WHEN production planning runs, THEN the scene cannot enter release gate until the blocker is resolved or explicitly waived by the user.
- [x] GIVEN a scene reaches playtest_ready, WHEN human QA evaluates it, THEN the tester can answer where they are, what they can do, how to leave, and what changed without developer guidance.
- [x] GIVEN this GDD itself is reviewed, WHEN the user completes review, THEN any missing demand is added here before status can move from `In Design` to `Approved`.

---

## Implementation Notes

Create or refresh the human QA checklist and release-gate handoff format for current scene readiness. The checklist must force subjective readability into concrete answers: location identity, available action, exit path, state change, UI dominance, and blockers/waivers. Story 015 focused human QA remains the current release gate blocker until rerun or waived.

---

## Out of Scope

- Fixing any scene readability bug discovered by QA.
- Final art/audio production.
- Replacing the release checklist.

---

## QA Test Cases

- **AC-1**: user review can block.
  - Setup: run a focused scene QA checklist after automated evidence passes.
  - Verify: checklist includes a user verdict and blocker field.
  - Pass condition: a BLOCKED user verdict prevents release gate handoff unless explicitly waived.
- **AC-2**: readability questions.
  - Setup: tester reaches the target scene without developer explanation.
  - Verify: tester can answer where they are, what they can do, how to leave, and what changed.
  - Pass condition: all answers are recorded as PASS/PASS WITH CONDITIONS or blockers are filed.
- **AC-3**: release handoff.
  - Setup: prepare release gate input.
  - Verify: unresolved scene blockers and waivers are listed.
  - Pass condition: no silent promotion of blocked scenes.

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/playtests/` focused checklist or release-gate handoff note.
- `production/qa/evidence/scene-composition-user-readability-release-gate-evidence.md`

**Status**: [x] Created and passing -- see `production/qa/evidence/scene-composition-user-readability-release-gate-evidence.md`

---

## Implementation Notes

- Human review checklist created at `production/playtests/scene-composition-user-readability-checklist.md`.
- Release handoff packet created at `production/scene-specs/scene-release-gate-handoff.md`.
- Completeness gate now links the user checklist and release handoff requirements.
- Coverage registry now records current #19 release handoff status as `BLOCKED_FOR_RELEASE` until user readability reviews are recorded or explicitly waived.
- Integration validation added at `tests/integration/scene-composition/UserReadabilityReleaseGateTest.csproj`.
- The current scene release snapshot remains blocked; this story defines handoff evidence and does not waive or fix scene readability blockers.

---

## Dependencies

- Depends on: Story 003 complete and pushed; Scene Physics Stories 001-004 complete and pushed.
- Unlocks: release checklist/gate only after current human QA blockers are resolved or waived.

## Completion Notes

**Completed**: 2026-05-24
**Verdict**: COMPLETE WITH NOTES
**Criteria**: 4/4 passing.
**Deviations**: Advisory only: this story completes the checklist and handoff contract, but it does not provide the actual user readability verdicts or an explicit user waiver required for release readiness.
**Test Evidence**: Visual/Feel handoff evidence in `production/qa/evidence/scene-composition-user-readability-release-gate-evidence.md`; automated checklist/handoff coverage through `tests/integration/scene-composition/UserReadabilityReleaseGateTest.csproj`.
**Code Review**: Full-mode closure review performed during story-done; no new code edits were made in the closure pass.
**Notes**: Scene Composition #19 remains `BLOCKED_FOR_RELEASE`. Do not claim release-ready until user readability reviews pass or the user explicitly records waivers.
