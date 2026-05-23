# Epic: Scene Physics Unit System

> **Layer**: MVP Foundation Retrofit / Gameplay Scene Physics
> **GDD**: design/gdd/scene-physics-unit-system.md
> **Architecture Module**: #20 Scene Physics
> **Status**: In Progress
> **Stories**: 4 (001-003 Implemented; 004 Ready)

## Overview

Implement the bottom-layer physical-world exploration contract for authored 2D scenes. The epic turns GDD #20 into runtime-readable Scene Physics Contracts, scene-unit catalogs, collision and occlusion semantics, Layer / Height Models, Cutaway / Reveal Models, Floor State, special-surface policies, dynamic behavior tags, and recovery rules. It does not replace movement input or domain gameplay consequences; it defines how units occupy and communicate physical space so scene design, implementation, QA, and asset replacement stop relying on guesswork.

## Governing ADRs

| ADR / Source | Decision Summary | Engine Risk |
| --- | --- | --- |
| ADR-0004: InteractionHandler and Use Dispatch | Scene units that can be used must integrate with world interaction focus and Use gates without moving domain consequences into the physics contract. | MEDIUM |
| ADR-0019: Desktop C# Platform Pivot | Runtime contract exposure, smoke probes, and new implementation target desktop Godot 4.6.2 .NET/C#. | HIGH |
| GDD #19 / Control Manifest | Scene completion consumes `physics_contract_complete`; physical scene evidence must come from world/playable scene units rather than UI. | MEDIUM |
| Godot 4.6.2 Physics Reference | Implementation stories must verify `CharacterBody2D`, `CollisionObject2D`, `Area2D`, `StaticBody2D`, and `AnimatableBody2D` behavior against pinned engine docs. | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
| --- | --- | --- |
| TR-scene-physics-001 | Every 2D enterable scene with gameplay-relevant physical units must declare horizontal or vertical scene type, movement plane, Layer / Height Model, Cutaway / Reveal Model, and Floor State or explicit N/A true rule | GDD #20 gate + ADR-0019 |
| TR-scene-physics-002 | Scene units must declare collision semantics, occlusion/layering, player-relative scale, special surface policy, and authored physical unit identity independent of UI | GDD #20 gate + ADR-0004 / ADR-0019 |
| TR-scene-physics-003 | Dynamic physical behaviors such as pushable, elastic, slippery, moving-platform, one-way, breakable, mirror, glass, water, current/wind, and trigger-only units must declare parameters, feedback, conflict priority, and recovery rules | GDD #20 gate + ADR-0004 / ADR-0019 |

## Epic Scope

- Define and validate runtime Scene Physics Contract shape for each current playable scene.
- Add or refresh current Hub exterior, ship interior, and Exploration contracts to include Layer / Height, Cutaway / Reveal, Floor State, collision, occlusion, scale, special surfaces, behavior priority, and recovery.
- Establish scene-unit catalog conventions for blockers, soft overlaps, pushables, special surfaces, foreground occluders, height markers, and future dynamic units.
- Add QA/smoke checks that fail when contract fields are missing or UI is used as scene-unit evidence.
- Provide implementation boundaries for future water, glass, mirror, elastic, pushable, moving platform, climbable, breakable, and one-way units.

## Out of Scope

- Player input mapping, interaction focus scoring, and Use dispatch ownership (#4).
- Search rewards, repair completion, market purchase logic, route consequences, or persistence state.
- Full final-art collision meshes for every future scene.
- Real-time physics sandbox behavior that is not declared by a scene spec.

## Stories

| # | Story | Type | Status | ADR |
| --- | --- | --- | --- | --- |
| 001 | [Scene Physics Contract Runtime Shape](story-001-runtime-contract-shape.md) | Integration | Implemented | ADR-0019 |
| 002 | [Layer Height Cutaway and Floor State](story-002-layer-height-cutaway-floor-state.md) | Integration | Implemented | ADR-0019 |
| 003 | [Scene Unit Catalog Collision Occlusion and Scale](story-003-unit-catalog-collision-occlusion-scale.md) | Integration | Implemented | ADR-0004 / ADR-0019 |
| 004 | [Dynamic Physical Behaviors Special Surfaces and Recovery](story-004-dynamic-behaviors-special-surfaces-recovery.md) | Integration | Ready | ADR-0004 / ADR-0019 |

## Definition of Done

This epic is complete when:

- All stories are implemented, reviewed, and closed via `/story-done`.
- Every current gameplay-relevant enterable scene exposes a Scene Physics Contract with all GDD #20 required fields.
- Contracts distinguish horizontal and vertical scenes, including `primary_walkable_layer`, `floor_id`, `floor_index`, cutaway/reveal behavior, and behind-object reveal or explicit N/A true rules.
- Collision semantics, occlusion/layering, unit scale, special surfaces, dynamic behavior priority, and stuck recovery are smoke-testable.
- Formal asset replacement cannot alter collision footprint, occlusion behavior, interaction radius, or size readability without contract re-review.
- Current smoke evidence verifies that scene physics proof comes from world/playable scene units, not UI/HUD nodes.

## Next Step

Run `/story-readiness production/epics/scene-physics-unit-system/story-004-dynamic-behaviors-special-surfaces-recovery.md`, then `/dev-story` for the same file.
