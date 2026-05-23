# Scene Composition Spec Coverage Evidence

> **Story**: `production/epics/scene-composition-system/story-001-scene-spec-template-coverage-registry.md`
> **Date**: 2026-05-24
> **Result**: PASS
> **Story Type**: Config/Data

## Scope

Story 001 creates the reusable #19 scene-spec structure and the current coverage registry. It does not implement the Story 002 computed completeness gate, the Story 003 UI-vs-scene enforcement layer, or the Story 004 human readability release handoff.

## Created Artifacts

- `production/scene-specs/scene-spec-template.md`
  - Reusable scene specification template and checklist.
  - Requires identity, entry/exit, spatial layout, critical path, behavior, state variants, #20 Scene Physics Contract link/exemption, assets/audio, technical contract, QA evidence, Codex review, and user review.
  - Explicitly states that UI/HUD/buttons/labels/debug overlays cannot count as scene units or physical evidence.
- `production/scene-specs/scene-coverage-registry.md`
  - Current enterable scene coverage registry.
  - Tracks Hub exterior, ship interior, chart table surface, Exploration, repair node scene, and market scene.
  - Links current Hub/ship/exploration rows to Scene Physics evidence from #20 Stories 001-004.
  - Marks repair and market scene entries as tracked gaps that must receive specs before visual completion claims.

## Acceptance Coverage

| AC | Result | Evidence |
| --- | --- | --- |
| Spec required before production readiness | PASS | Template defines the required shape; registry requires each row to link a spec, equivalent note, tracked gap, or explicit exemption. |
| 2D specs include or link #20 Scene Physics Contract | PASS | Registry includes `#20 physics input` per row and links #20 evidence for `hub_island_dock`, `hub_ship_interior`, and `exploration_mist_island`. |
| No-physical-unit scenes state why #20 does not apply | PASS | Template includes an explicit exemption field. Registry does not silently exempt current scenes; chart table is flagged for Story 002/003 decision. |
| Repair or market scenes need specs before visual completion | PASS | Registry rows `repair_node_scene` and `market_scene` are tracked gaps with required next action before visual completion claims. |

## #20 Dependency Input

Scene Composition Story 001 consumes the completed Scene Physics contract work instead of redefining physical detail:

- `production/qa/evidence/scene-physics-runtime-contract-shape-evidence.md`
- `production/qa/evidence/scene-physics-layer-cutaway-floor-state-evidence.md`
- `production/qa/evidence/scene-physics-unit-catalog-evidence.md`
- `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md`

Latest known Scene Physics push: `8d0e8778407fbad061f7c55c3c75ca74ef173ebe`.

## Boundary Verification

- Scene units are defined as world/playable scene layer evidence.
- UI, HUD, route buttons, save/load buttons, onboarding hints, labels, and debug overlays are listed as non-scene/UI surfaces.
- The registry does not claim Chart, repair, or market completeness from UI controls.
- Story 002+ work is explicitly deferred to the follow-up queue.

## Verification

```text
git diff --check
Result: PASS
Notes: LF/CRLF warnings may appear for existing files; no whitespace errors.
```

No `dotnet build` or Godot smoke was required for Story 001 because the implementation was documentation/data only and did not modify runtime code.
