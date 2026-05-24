# Story 004: Post-Implementation Feedback Routing and Release Gate Handoff

> **Epic**: Complete Scene Composition and Acceptance
> **Status**: Complete
> **Layer**: Polish Gate / Production Scene Design
> **Type**: Visual/Feel
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused implementation session)

## Context

**GDD**: `design/gdd/scene-composition-system.md`
**Requirement**: `TR-scene-composition-002`
**Requirement Text**: Scene completion requires creation suitability approval where applicable, independent implementation / asset boundary, scene physics readiness, behavior readiness, state variant readiness, visual/audio readiness, technical contract readiness, automated evidence, Codex review, and implementation-feedback routing.

**ADR Governing Implementation**: ADR-0016: Feedback/VFX/Audio Semantics; ADR-0017: Onboarding First Loop; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: feedback and onboarding can support scene clarity, but release-readiness still requires independent scene evidence and desktop-validated proof.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: HIGH
**Engine Notes**: screenshots or windowed runs may support evidence, but claims must include exact build/test context.
**Performance Note**: No runtime performance impact is expected for the checklist and release handoff documents. Any optional manual capture must record the build/smoke context rather than adding runtime systems.

**Control Manifest Rules (this layer)**:
- Required: semantic feedback and onboarding must not steal gameplay ownership.
- Forbidden: do not declare release-ready while P0 scene blockers remain.
- Guardrail: release handoff records implementation blockers, asset gaps, and waivers explicitly.

---

## Acceptance Criteria

- [x] GIVEN creation suitability is approved, WHEN a scene/UI/unit spec is generated, THEN implementation can proceed without a second human verdict gate after the user edits are written back.
- [x] GIVEN Codex review reports a blocker or the release packet has missing implementation evidence, WHEN production planning runs, THEN the scene cannot enter release gate until the blocker is resolved or explicitly waived.
- [x] GIVEN the user experiences the real implementation and asks for a change, WHEN the change targets a specific scene/UI/unit, THEN the work enters `directed-content-modification` and updates both the document and implementation.
- [x] GIVEN a scene reaches release handoff, WHEN the packet is reviewed, THEN independent implementation / asset boundary, #20 contract coverage, screenshots, P0 asset state, and Codex consistency evidence are recorded.

---

## Implementation Notes

Create or refresh the implementation-feedback prompt and release-gate handoff format for current scene readiness. The prompt may capture concrete experience questions such as location identity, available action, exit path, state change, and UI dominance, but it does not produce a release verdict. Story 015 focused evidence remains useful historical context; current blockers come from implementation evidence, #20 contract gaps, screenshot evidence, P0 asset gaps, and tracked waivers.

---

## Out of Scope

- Fixing any scene readability bug discovered by QA.
- Final art/audio production.
- Replacing the release checklist.

---

## QA Test Cases

- **AC-1**: no second human gate.
  - Setup: approve creation suitability and complete the scene/UI/unit spec.
  - Verify: release documents do not require a post-implementation user approval/rejection outcome.
  - Pass condition: implementation can proceed after the approved spec is ready and evidence paths are named.
- **AC-2**: feedback routing.
  - Setup: user requests a concrete change after trying the implementation.
  - Verify: the target scene/UI/unit document and implementation change through `directed-content-modification`.
  - Pass condition: feedback is recorded as a modifiable change request, not as a release verdict.
- **AC-3**: release handoff.
  - Setup: prepare release gate input.
  - Verify: unresolved implementation evidence, #20 contract gaps, screenshot gaps, P0 asset gaps, and waivers are listed.
  - Pass condition: no silent promotion of scenes with missing evidence.

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/playtests/` focused feedback prompt or release-gate handoff note.
- `production/qa/evidence/scene-composition-user-readability-release-gate-evidence.md`

**Status**: [x] Created and passing -- see `production/qa/evidence/scene-composition-user-readability-release-gate-evidence.md`

---

## Implementation Notes

- Non-gating implementation-feedback prompt created at `production/playtests/scene-composition-user-readability-checklist.md`.
- Release handoff packet created at `production/scene-specs/scene-release-gate-handoff.md`.
- Completeness gate now links feedback routing and release handoff requirements.
- Coverage registry now records current #19 release handoff status as `BLOCKED_FOR_RELEASE` until implementation evidence, #20 contract gaps, screenshot evidence, and P0 asset gaps are resolved or explicitly waived.
- Integration validation added at `tests/integration/scene-composition/UserReadabilityReleaseGateTest.csproj`.
- The current scene release snapshot remains blocked; this story defines handoff evidence and does not waive or fix implementation evidence / asset blockers.

---

## Dependencies

- Depends on: Story 003 complete and pushed; Scene Physics Stories 001-004 complete and pushed.
- Unlocks: release checklist/gate after implementation evidence, #20 contract gaps, screenshot evidence, and P0 asset gaps are resolved or waived.

## Completion Notes

**Completed**: 2026-05-24
**Verdict**: COMPLETE
**Criteria**: 4/4 passing.
**Deviations**: None. This story completes the feedback-routing and handoff contract; it does not implement missing scene evidence or final assets.
**Test Evidence**: Visual/Feel handoff evidence in `production/qa/evidence/scene-composition-user-readability-release-gate-evidence.md`; automated checklist/handoff coverage through `tests/integration/scene-composition/UserReadabilityReleaseGateTest.csproj`.
**Code Review**: Full-mode closure review performed during story-done; no new code edits were made in the closure pass.
**Notes**: Scene Composition #19 remains `BLOCKED_FOR_RELEASE`. Do not claim release-ready until implementation evidence, #20 contract gaps, screenshot evidence, P0 asset gaps, and tracked waivers are resolved.
