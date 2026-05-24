# Story 003: Scene Versus UI Evidence Boundary

> **Epic**: Complete Scene Composition and Acceptance
> **Status**: Complete
> **Layer**: Polish Gate / Production Scene Design
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused implementation session)

## Context

**GDD**: `design/gdd/scene-composition-system.md`
**Requirement**: `TR-scene-composition-003`
**Requirement Text**: UI, HUD, buttons, menus, labels, and debug overlays cannot count as physical scene units or substitute for world/playable scene evidence.

**ADR Governing Implementation**: ADR-0012: UI Input Routing and Dual Focus
**ADR Decision Summary**: UIManager owns UI focus, modal stack, HUD updates, and screen state; world focus and physical scene evidence remain separate from UI controls.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: HIGH
**Engine Notes**: dual-focus behavior and UI input routing are engine-sensitive; use existing UI smoke and focus tests where possible.
**Performance Note**: No runtime performance impact is expected for the boundary document and C# validation. Any smoke extension must reuse existing deterministic node/focus evidence and remain inside the current scene-transition and 60fps budgets.

**Control Manifest Rules (this layer)**:
- Required: UI focus and world focus are isolated.
- Forbidden: never register UI controls as scene physics units.
- Guardrail: UI may assist readability but cannot dominate world identity.

---

## Acceptance Criteria

- [x] GIVEN UI or HUD exists in a scene, WHEN visual QA checks the screen, THEN UI does not dominate or hide the world identity.
- [x] GIVEN a UI/HUD label, button, menu or debug overlay exists, WHEN scene completion is evaluated, THEN it does not count as a physical scene unit, scene identity node, interaction anchor, or physics contract proof.
- [x] GIVEN automated smoke checks scene identity, WHEN UI-only evidence is present without world/playable nodes, THEN the scene fails readiness.
- [x] GIVEN world and UI focus both exist, WHEN modal or semi-modal UI is active, THEN world focus is isolated without deleting the underlying scene evidence.

---

## Implementation Notes

Strengthen smoke/review guidance so UI nodes are explicitly ignored for scene unit counts and physical scene proof. The implementation may add helper assertions that inspect node groups, names, layers, or contract fields, but must preserve ADR-0012 focus ownership.

---

## Out of Scope

- Redesigning UI layout.
- Final visual art for the scene.
- Scene physics contract internals owned by #20.

---

## QA Test Cases

- **AC-1**: UI cannot count as scene evidence.
  - Given: a screen with HUD labels/buttons and no world scene unit.
  - When: scene readiness is evaluated.
  - Then: readiness fails.
  - Edge cases: debug labels and overlay text also fail as evidence.
- **AC-2**: world identity remains visible.
  - Given: current Hub or Exploration scene.
  - When: UI/HUD is present.
  - Then: main world identity remains visible and not hidden by UI.
  - Edge cases: modal UI can cover temporarily but must not be used as scene completion proof.
- **AC-3**: focus isolation.
  - Given: UI modal active.
  - When: smoke checks focus and scene evidence.
  - Then: world input is blocked or isolated while scene evidence remains mounted.

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Updated smoke or QA evidence proving UI nodes cannot satisfy scene evidence.
- `production/qa/evidence/scene-composition-scene-vs-ui-boundary-evidence.md`

**Status**: [x] Created and passing -- see `production/qa/evidence/scene-composition-scene-vs-ui-boundary-evidence.md`

---

## Implementation Notes

- UI-vs-scene boundary contract created at `production/scene-specs/scene-vs-ui-evidence-boundary.md`.
- Completeness gate now links the boundary contract and blocks UI-only evidence packages.
- Coverage registry classifies `chart_table_scene` as a UI-assisted world surface anchored inside `hub_ship_interior`, not standalone scene physics proof.
- Integration validation added at `tests/integration/scene-composition/SceneVsUiBoundaryTest.csproj`.
- The validation checks current smoke and UI focus evidence without starting Story 004's implementation-feedback and release handoff.

---

## Dependencies

- Depends on: Story 002 complete and pushed.
- Unlocks: Story 004.

## Completion Notes

**Completed**: 2026-05-24
**Verdict**: COMPLETE
**Criteria**: 4/4 passing.
**Deviations**: None. UI/HUD/buttons/menus/labels/debug overlays remain assistive-only and cannot substitute for world/playable scene evidence.
**Test Evidence**: Integration evidence in `production/qa/evidence/scene-composition-scene-vs-ui-boundary-evidence.md`; automated coverage through `tests/integration/scene-composition/SceneVsUiBoundaryTest.csproj`.
**Code Review**: Full-mode closure review performed during story-done; no new code edits were made in the closure pass.
**Notes**: Story 002 dependency is now formally closed in the same story-done batch.
