# Story 002: Scene Completeness Gate and Evidence Contract

> **Epic**: Complete Scene Composition and Acceptance
> **Status**: Complete
> **Layer**: Polish Gate / Production Scene Design
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused implementation session)

## Context

**GDD**: `design/gdd/scene-composition-system.md`
**Requirement**: `TR-scene-composition-002`
**Requirement Text**: Scene completion requires creation suitability approval where applicable, independent implementation / asset boundary, scene physics readiness, behavior readiness, state variant readiness, visual/audio readiness, technical contract readiness, automated evidence, Codex review, and implementation-feedback routing.

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: scene evidence must fit the project scene lifecycle and be validated by desktop C# build/smoke where code is involved.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: HIGH
**Engine Notes**: evidence checks should be deterministic and should not depend on final art assets unless the gate is specifically checking asset readiness.
**Performance Note**: No runtime performance impact expected for the gate itself. Any smoke extension must read existing deterministic evidence and remain within the existing scene-transition and smoke budgets.

**Control Manifest Rules (this layer)**:
- Required: physical world exploration is a bottom-layer gameplay contract.
- Forbidden: never infer gameplay collision or scene completeness from art alone.
- Guardrail: scene transition and smoke evidence must remain bounded.

---

## Acceptance Criteria

- [x] GIVEN a scene specification exists, WHEN Codex reviews it, THEN purpose, space, behavior, state, presentation, technical and QA lines are all checked for blockers.
- [x] GIVEN a scene reaches greybox, WHEN automated smoke runs, THEN tests verify visible scene identity nodes, main viewport coverage, interaction anchors, focus isolation and core route behavior relevant to that scene.
- [x] GIVEN a scene reaches asset_gate, WHEN asset requests are audited, THEN every P0 asset maps back to a scene identity, interaction, state variant or feedback requirement.
- [x] GIVEN release readiness is discussed, WHEN any P0 current-scene asset gap remains unresolved, THEN the release gate stays blocked or explicitly records the waiver.
- [x] GIVEN a scene depends on domain systems, WHEN implementation occurs, THEN the scene layer does not create a new gameplay authority or duplicate persistent state.

---

## Implementation Notes

Implement the `scene_complete` evidence contract from GDD #19 as a checklist, script, smoke extension, or production evidence format. The gate must require creation suitability approval where applicable, independent implementation / asset boundary, `scene_physics_ready`, behavior, state variants, visual/audio readiness, technical contract, automated evidence, Codex review, and implementation-feedback routing. It should not mutate domain state.

---

## Out of Scope

- Story 001 owns scene spec template and registry.
- Story 003 owns UI/HUD exclusion checks.
- Story 004 owns implementation-feedback routing and release handoff.

---

## QA Test Cases

- **AC-1**: completeness gate.
  - Given: a scene evidence package.
  - When: gate is evaluated.
  - Then: all GDD #19 readiness dimensions are checked.
  - Edge cases: any false dimension blocks completion.
- **AC-2**: smoke evidence.
  - Given: a greybox scene.
  - When: smoke runs.
  - Then: scene identity, viewport coverage, anchors, focus, and route behavior are verified.
  - Edge cases: node existence alone is insufficient.
- **AC-3**: asset traceability.
  - Given: P0 current-scene asset list.
  - When: audited.
  - Then: every asset maps to identity, interaction, state, or feedback need.
  - Edge cases: unresolved P0 gaps must block or record explicit waiver.

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Gate checklist/script output or updated smoke evidence.
- `production/qa/evidence/scene-composition-completeness-gate-evidence.md`

**Status**: [x] Created and passing -- see `production/qa/evidence/scene-composition-completeness-gate-evidence.md`

---

## Implementation Notes

- Gate contract created at `production/scene-specs/scene-completeness-gate.md`.
- Integration validation added at `tests/integration/scene-composition/SceneCompletenessGateTest.csproj`.
- The gate defines the full `scene_complete` dimension set and blocks on false, pending, tracked-gap, or missing evidence unless an explicit user waiver is recorded.
- Current smoke requirements are tied to existing `tests/smoke/session_shell_visual_probe.gd` evidence for scene identity nodes, viewport coverage, spatial anchors, focus isolation, route behavior, and #20 physical contracts.
- P0 current-scene asset gaps block release readiness unless waiver owner/date/risk/fallback evidence are recorded.
- Scene evidence may read domain state and present it through world anchors, but may not create gameplay authority, duplicate persistent state, mutate domain-owned state, or infer collision from art.

---

## Dependencies

- Depends on: Story 001 complete and pushed; Scene Physics Stories 001-004 complete and pushed.
- Unlocks: Story 003, Story 004.

## Completion Notes

**Completed**: 2026-05-24
**Verdict**: COMPLETE
**Criteria**: 5/5 passing.
**Deviations**: None. The gate requires implementation evidence, Codex consistency review, #20 contract coverage, asset gap handling, and release handoff evidence before release readiness; automated evidence alone is not sufficient.
**Test Evidence**: Integration evidence in `production/qa/evidence/scene-composition-completeness-gate-evidence.md`; automated coverage through `tests/integration/scene-composition/SceneCompletenessGateTest.csproj`.
**Code Review**: Full-mode closure review performed during story-done; no new code edits were made in the closure pass.
**Notes**: Story 001 and #20 dependencies are now formally closed in the same story-done batch.
