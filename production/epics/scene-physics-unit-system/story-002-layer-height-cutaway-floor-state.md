# Story 002: Layer Height Cutaway and Floor State

> **Epic**: Scene Physics Unit System
> **Status**: Ready
> **Layer**: MVP Foundation Retrofit / Gameplay Scene Physics
> **Type**: Integration
> **Manifest Version**: 2026-05-09

## Context

**GDD**: `design/gdd/scene-physics-unit-system.md`  
**Requirement**: `TR-scene-physics-001`

**ADR Governing Implementation**: ADR-0019: Desktop C# Platform Pivot  
**ADR Decision Summary**: all new runtime validation is desktop Godot .NET/C# first; Godot smoke is the source of rendered scene confidence.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: HIGH  
**Engine Notes**: layer and reveal checks should be deterministic data assertions before final renderer-specific behavior is introduced.

**Control Manifest Rules (this layer)**:
- Required: every scene physics contract must declare movement plane and scene physical layering before implementation readiness.
- Forbidden: never treat UI/HUD as world collision, floor, or reveal evidence.
- Guardrail: scene transition cleanup must not leave stale floor or visibility state.

---

## Acceptance Criteria

- [ ] GIVEN a scene is `水平场景`, WHEN movement readability is reviewed, THEN ground-plane up/down/left/right movement, jump/fly height changes, and ground-shadow or landing-height cues are specified.
- [ ] GIVEN a `水平场景` contains buildings, mountains, bridges, caves, roofs, slopes, interiors, foreground canopies or visible height differences, WHEN implementation readiness is reviewed, THEN it declares a Layer / Height Model with `primary_walkable_layer` plus every reachable, unreachable, transition, height-only and blocked layer.
- [ ] GIVEN a scene is `垂直场景`, WHEN movement readability is reviewed, THEN left/right movement, depth layering, foreground/background separation, and vertical traversal methods such as jump, flight, ladders or stairs are specified.
- [ ] GIVEN a scene contains multi-floor buildings, ship compartments, towers, caves, underground spaces, tree houses, elevators or stacked rooms, WHEN design review begins, THEN it declares a Cutaway / Reveal Model that says how the active floor/room is shown and how non-active layers are hidden, faded, outlined or locked.
- [ ] GIVEN a reachable floor or height band exists, WHEN floor switching or vertical traversal is reviewed, THEN `floor_id`, `floor_index`, `is_active_floor`, `visibility_mode`, `walkable_bounds`, `vertical_connectors`, `occluders_hidden_above` and `interactions_enabled` are specified.
- [ ] GIVEN the player, NPCs, enemies, pushables or key interaction points can pass behind a building, tree, rock, ship body, bridge, market stall, wall or other large occluder, WHEN readability is reviewed, THEN that unit declares `behind_object_reveal` and preserves collision, interaction identity and spatial meaning while revealing the hidden subject.
- [ ] GIVEN a foreground occluder can cover the player or a core interaction, WHEN QA reviews the scene, THEN `occluder_peek`, fade, cutout, outline or equivalent reveal keeps identity readable within `identity_occlusion_max_seconds`.

---

## Implementation Notes

Add explicit contract fields and smoke checks for `layer_height_model`, `cutaway_reveal_model`, `floor_state`, horizontal `primary_walkable_layer`, vertical floor indices, active-floor reveal behavior, and behind-object reveal classification or `N/A true`. Preserve the GDD distinction between entering a building and walking behind a building.

---

## Out of Scope

- Story 001 owns base contract availability.
- Story 003 owns collision/occlusion/scale catalog detail.
- Story 004 owns dynamic behavior and stuck recovery.

---

## QA Test Cases

- **AC-1**: horizontal layer model.
  - Given: a horizontal current or fixture scene.
  - When: smoke reads `layer_height_model`.
  - Then: `primary_walkable_layer` and relevant layer categories are present.
  - Edge cases: a simple single-layer scene must still record `N/A true` for absent complex cases.
- **AC-2**: vertical floor state.
  - Given: ship interior contract.
  - When: smoke reads `floor_state`.
  - Then: required floor fields and active-floor visibility are declared.
  - Edge cases: future connectors can be declared not implemented, but not omitted.
- **AC-3**: reveal classification.
  - Given: multi-floor, interior, or foreground occluder scenarios.
  - When: contract is reviewed.
  - Then: cutaway, active-floor, behind-object reveal, or occluder peek is classified correctly.
  - Edge cases: walking behind a building must not be recorded as entering that building.

---

## Test Evidence

**Story Type**: Integration  
**Required evidence**:
- Scene physics smoke coverage for layer/reveal/floor fields.
- `production/qa/evidence/scene-physics-layer-cutaway-floor-state-evidence.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001.
- Unlocks: Story 003, Scene Composition Story 002.
