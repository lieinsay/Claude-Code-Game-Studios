# Story 001: Scene Specification Template and Coverage Registry

> **Epic**: Complete Scene Composition and Acceptance
> **Status**: Implemented
> **Layer**: Polish Gate / Production Scene Design
> **Type**: Config/Data
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused documentation/data session)

## Context

**GDD**: `design/gdd/scene-composition-system.md`  
**Requirement**: `TR-scene-composition-001`
**Requirement Text**: Every enterable scene must provide a scene specification covering purpose, entry/exit, spatial structure, interaction anchors, state variants, assets, audio/VFX, technical contract, and QA evidence before implementation readiness.

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order; ADR-0019: Desktop C# Platform Pivot  
**ADR Decision Summary**: scene lifecycle and runtime evidence must respect the established scene boot/transition contract, and new runtime validation targets desktop Godot .NET/C#.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: MEDIUM  
**Engine Notes**: this story is mostly documentation/data, but any runtime hooks must remain compatible with desktop smoke validation.
**Performance Note**: No runtime performance impact expected. This story creates reusable production documentation and registry data only; future runtime hooks remain Story 002+ scope.

**Control Manifest Rules (this layer)**:
- Required: every enterable 2D scene with gameplay-relevant physical units must provide a Scene Physics Contract before implementation readiness.
- Forbidden: never treat UI/HUD as scene-unit evidence.
- Guardrail: scene transition evidence should not duplicate persistent gameplay state.

---

## Acceptance Criteria

- [x] GIVEN a new可进入场景 is proposed, WHEN design starts, THEN a scene specification exists before implementation work is treated as production-ready.
- [x] GIVEN a 2D scene specification is drafted, WHEN review begins, THEN it includes or links a Scene Physics Contract that passes `design/gdd/scene-physics-unit-system.md`.
- [x] GIVEN a scene has no gameplay-relevant physical units, WHEN review begins, THEN the spec explicitly states why #20 does not apply.
- [x] GIVEN repair or market systems are implemented, WHEN this system is applied, THEN their可进入场景 specs must be created before they are considered visually complete.

---

## Implementation Notes

Create a reusable scene spec template or checklist that includes the GDD #19 required shape: identity, entry/exit, spatial layout, critical path, behavior, state variants, Scene Physics Contract, assets, audio/VFX, technical contract, QA evidence, and human review. Also create or update a coverage registry for current scenes and required exemptions.

---

## Out of Scope

- Story 002 owns computed completeness gate and evidence status.
- Story 003 owns UI-vs-scene proof boundaries.
- Story 004 owns human QA/release handoff.

---

## QA Test Cases

- **AC-1**: spec required before readiness.
  - Given: a proposed enterable scene.
  - When: production readiness is checked.
  - Then: missing scene spec blocks readiness.
  - Edge cases: Polish backlog scene notes can be accepted only if linked and complete.
- **AC-2**: physics contract link.
  - Given: a scene spec with physical units.
  - When: review begins.
  - Then: it links or embeds a #20 contract.
  - Edge cases: no-physics scenes must explicitly justify exemption.
- **AC-3**: current coverage.
  - Given: Hub exterior, ship interior, Exploration, repair, and market scene entries.
  - When: registry is reviewed.
  - Then: each is linked to a spec, pending spec, or explicit tracked gap.

---

## Test Evidence

**Story Type**: Config/Data  
**Required evidence**:
- Scene spec template or coverage registry diff.
- `production/qa/evidence/scene-composition-spec-coverage-evidence.md`

**Status**: [x] Created and passing -- see `production/qa/evidence/scene-composition-spec-coverage-evidence.md`

---

## Implementation Notes

- Reusable template created at `production/scene-specs/scene-spec-template.md`.
- Current coverage registry created at `production/scene-specs/scene-coverage-registry.md`.
- Registry covers current enterable scene entries for Hub exterior, ship interior, Chart table surface, Exploration, repair node scene, and market scene.
- Current Hub exterior, ship interior, and Exploration rows consume #20 Scene Physics evidence from Stories 001-004 instead of restating physical details.
- Repair and market rows are tracked gaps: they must receive scene specs before either scene is considered visually complete.
- UI/HUD/buttons/labels/debug overlays are explicitly classified as non-scene evidence.

---

## Dependencies

- Depends on: Scene Physics Stories 001-004 implemented and pushed. Latest known #20 push: `8d0e8778407fbad061f7c55c3c75ca74ef173ebe`.
- Unlocks: Story 002.
