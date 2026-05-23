# Story 003: Scene Unit Catalog Collision Occlusion and Scale

> **Epic**: Scene Physics Unit System
> **Status**: Implemented
> **Layer**: MVP Foundation Retrofit / Gameplay Scene Physics
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused implementation session)

## Context

**GDD**: `design/gdd/scene-physics-unit-system.md`  
**Requirement**: `TR-scene-physics-002`
**Requirement Text**: Scene units must declare collision semantics, occlusion/layering, player-relative scale, special surface policy, and authored physical unit identity independent of UI.

**ADR Governing Implementation**: ADR-0004: InteractionHandler and Use Dispatch; ADR-0019: Desktop C# Platform Pivot  
**ADR Decision Summary**: interactable scene units integrate through world focus and Use dispatch, while runtime validation remains desktop Godot .NET/C# first.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: MEDIUM  
**Engine Notes**: collision and overlap semantics should map cleanly to `CollisionObject2D`, `Area2D`, `StaticBody2D`, and related Godot 2D bodies when implementation moves past debug contracts.
**Performance Note**: No new physics bodies or renderer effects are expected in this story; the catalog remains deterministic debug/QA contract data and must not add per-frame work.

**Control Manifest Rules (this layer)**:
- Required: all interactable objects must use the project interaction entry point; scene physics does not own domain consequences.
- Forbidden: never infer gameplay collision from art alone.
- Guardrail: physical scene proof must come from world/playable scene units.

---

## Acceptance Criteria

- [x] GIVEN a scene contains gameplay-relevant units, WHEN implementation readiness is reviewed, THEN every unit declares collision behavior.
- [x] GIVEN units overlap visually, WHEN readability is reviewed, THEN foreground, midground, background, Y-sort/floor sorting, temporary occluders, and flying-unit shadow/body layering are specified.
- [x] GIVEN a scene contains player, NPC, props, doors, passages or landmarks, WHEN asset readiness is reviewed, THEN each category has a relative size rule tied to `player_unit`.
- [x] GIVEN a formal asset replaces a greybox unit, WHEN QA reviews the scene, THEN the replacement preserves collision footprint, occlusion behavior, interaction radius and size readability unless the physics contract is re-reviewed.
- [x] GIVEN a scene contains water, mirror, glass, fog, cloud, transparent fabric, reflective metal, breakable floor, ledge or void, WHEN design review begins, THEN each special surface is marked as gameplay-affecting or `visual_only`.
- [x] GIVEN a special surface is gameplay-affecting, WHEN implementation readiness is reviewed, THEN movement, collision, occlusion, reflection/refraction, interaction, audio, state and performance behavior are specified.
- [x] GIVEN a special surface is `visual_only`, WHEN QA reviews it, THEN it does not mislead the player about passability, interactivity, danger, height or collision.

---

## Implementation Notes

Create or formalize a scene-unit catalog shape that can be checked by smoke and QA. The catalog should distinguish `blocking_static`, `blocking_dynamic`, `pushable`, `soft_overlap`, `height_marker`, occlusion layers, scale references, and special-surface policy. Do not allow UI nodes to satisfy authored physical unit count.

Implemented in `HubRuntime.DebugScenePhysicsContract(scene_id)` as runtime-readable contract data for the current Hub exterior, ship interior, and Exploration playable scenes. Each contract now exposes `scene_unit_catalog`, `collision_table`, `occlusion_layers`, `scale_table`, `special_surface_table`, `asset_replacement_rule`, `physical_unit_source_layer`, and `ui_evidence_allowed`; smoke checks assert these are authored world/playable scene units rather than UI evidence.

---

## Out of Scope

- Story 004 owns dynamic behavior priority.
- Scene Composition stories own broader scene spec and human review.

---

## QA Test Cases

- **AC-1**: collision catalog.
  - Given: each current gameplay scene.
  - When: smoke or review reads the scene-unit catalog.
  - Then: every gameplay-relevant unit has collision semantics.
  - Edge cases: soft-overlap interaction anchors are not blockers.
- **AC-2**: occlusion and scale.
  - Given: player, landmarks, props, and interactables.
  - When: QA reviews the contract.
  - Then: sorting and scale are defined relative to `player_unit`.
  - Edge cases: flying body and shadow have distinct layers.
- **AC-3**: special surfaces.
  - Given: water, glass, mirror, fog, cloud, or similar surfaces.
  - When: reviewed.
  - Then: each is gameplay-affecting with full behavior or explicitly `visual_only`.
  - Edge cases: visual-only glass cannot imply passability or interaction.

---

## Test Evidence

**Story Type**: Integration  
**Required evidence**:
- Scene physics smoke or contract audit covering unit catalog fields.
- `production/qa/evidence/scene-physics-unit-catalog-evidence.md`

**Status**: [x] Created and passing in `production/qa/evidence/scene-physics-unit-catalog-evidence.md`

---

## Dependencies

- Depends on: Story 001 (`story-001-runtime-contract-shape.md`) and Story 002 (`story-002-layer-height-cutaway-floor-state.md`) implemented and pushed; formal `/story-done` closure remains downstream.
- Unlocks: Story 004, Scene Composition Story 002.
