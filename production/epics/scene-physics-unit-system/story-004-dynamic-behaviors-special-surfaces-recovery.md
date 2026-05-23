# Story 004: Dynamic Physical Behaviors Special Surfaces and Recovery

> **Epic**: Scene Physics Unit System
> **Status**: Complete
> **Layer**: MVP Foundation Retrofit / Gameplay Scene Physics
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Estimate**: S (1 focused implementation session)

## Context

**GDD**: `design/gdd/scene-physics-unit-system.md`
**Requirement**: `TR-scene-physics-003`
**Requirement Text**: Dynamic physical behaviors such as pushable, elastic, slippery, moving-platform, one-way, breakable, mirror, glass, water, current/wind, and trigger-only units must declare parameters, feedback, conflict priority, and recovery rules.

**ADR Governing Implementation**: ADR-0004: InteractionHandler and Use Dispatch; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: physical interactions can feed the shared world interaction entry point, but domain consequences stay with their owner systems; verification is desktop Godot .NET/C# first.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: MEDIUM
**Engine Notes**: future Godot 2D physics bodies must be verified against pinned docs before replacing debug contracts with real collision bodies.
**Performance Note**: This story only adds deterministic runtime contract data and smoke assertions. It must not add live physics simulation, per-frame priority solving, or new dynamic Godot bodies.

**Control Manifest Rules (this layer)**:
- Required: physical behavior tags must be explicit when they affect pathing, search, threat, return, repair, or market interaction.
- Forbidden: never add hidden physics behavior from art or node naming.
- Guardrail: stuck-state recovery must be testable.

---

## Acceptance Criteria

- [x] GIVEN a scene unit has elastic, slippery, sticky, conveyor, moving-platform, one-way, climbable, breakable, deformable, hazardous, attracting, teleport, current/wind or trigger-only behavior, WHEN implementation readiness is reviewed, THEN behavior label, parameters, feedback, affected unit types and conflict priority are specified.
- [x] GIVEN multiple physical behavior tags are combined on one unit or surface, WHEN design review runs, THEN priority and fallback rules are defined so collision, damage, movement and state updates cannot contradict each other.
- [x] GIVEN a dynamic object can move, push, bounce, break, carry or attract the player, WHEN QA reviews the scene, THEN there is a visible escape, reset or recovery path from stuck states.
- [x] GIVEN this GDD itself is reviewed, WHEN the user completes review, THEN missing physical unit requirements are added before status can move from `In Design` to `Approved`.

---

## Implementation Notes

Represent behavior tags and priorities as contract data before adding any real physics simulation. `effective_behavior = highest_priority(applicable_behavior_tags)` is the governing rule; if priority is absent, implementation readiness must fail. Recovery must name a concrete action such as clamp, reset, escape interaction, safe floor return, or object respawn.

Implemented in `HubRuntime.DebugScenePhysicsContract(scene_id)` as deterministic runtime contract data. Each current playable scene now exposes `physical_behavior_ready`, `recovery_ready`, `dynamic_behaviors`, `behavior_priority_table`, `behavior_conflict_rule`, `behavior_fallback_rules`, `missing_priority_blocks_readiness`, `stuck_recovery_seconds`, and `recovery_table`. The contract remains debug/QA data only and does not add live simulation, physics bodies, or domain consequences.

---

## Out of Scope

- Final bespoke physics for every future dynamic unit.
- Domain rewards, damage, repair, purchase, or search consequences.

---

## QA Test Cases

- **AC-1**: behavior tag readiness.
  - Given: a scene unit with any dynamic physical behavior.
  - When: reviewed.
  - Then: label, parameters, feedback, affected units, and priority exist.
  - Edge cases: trigger-only units still declare behavior even without collision.
- **AC-2**: conflict resolution.
  - Given: a unit with two or more behavior tags.
  - When: `effective_behavior` is evaluated.
  - Then: the highest-priority applicable behavior is deterministic.
  - Edge cases: missing priority blocks implementation readiness.
- **AC-3**: recovery.
  - Given: a player or pushable can become stuck through dynamic behavior.
  - When: QA reviews or smoke simulates the stuck state.
  - Then: visible escape, reset, or recovery exists.
  - Edge cases: infinite bounce loops and unreachable pushed objects fail.

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Unit or smoke test for behavior priority if implemented in code.
- QA evidence for stuck recovery and dynamic-behavior readiness.
- `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md`

**Status**: [x] Created and passing in `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md`

---

## Dependencies

- Depends on: Story 003 (`story-003-unit-catalog-collision-occlusion-scale.md`) complete and pushed.
- Unlocks: Scene Composition Story 002 and future dynamic-scene implementation stories.

## Completion Notes

**Completed**: 2026-05-24
**Verdict**: COMPLETE
**Criteria**: 4/4 passing.
**Deviations**: None. Dynamic behavior priority, fallback, and recovery remain deterministic contract data and do not introduce hidden physics or UI-based proof.
**Test Evidence**: Integration/smoke evidence in `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md`; automated coverage through `tests/smoke/session_shell_visual_probe.gd`.
**Code Review**: Full-mode closure review performed during story-done; no new code edits were made in the closure pass.
**Notes**: Story 003 dependency is now formally closed in the same story-done batch.
