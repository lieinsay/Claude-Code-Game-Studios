# Story 001: Scene Physics Contract Runtime Shape

> **Epic**: Scene Physics Unit System
> **Status**: Ready
> **Layer**: MVP Foundation Retrofit / Gameplay Scene Physics
> **Type**: Integration
> **Manifest Version**: 2026-05-09

## Context

**GDD**: `design/gdd/scene-physics-unit-system.md`  
**Requirement**: `TR-scene-physics-001`

**ADR Governing Implementation**: ADR-0019: Desktop C# Platform Pivot  
**ADR Decision Summary**: new runtime work targets desktop Godot 4.6.2 .NET/C#, with C# source, project files, `dotnet build`, and Godot headless validation as the normal verification path.

**Engine**: Godot 4.6.2 .NET + C# | **Risk**: HIGH  
**Engine Notes**: validate generated C# runtime surfaces with `dotnet build` and Godot headless smoke; do not add new Web-only requirements.

**Control Manifest Rules (this layer)**:
- Required: every enterable 2D scene with gameplay-relevant physical units must provide a Scene Physics Contract before implementation readiness.
- Forbidden: never infer gameplay collision from art alone.
- Guardrail: scene transition work must stay within the existing scene lifecycle and smoke budgets.

---

## Acceptance Criteria

- [ ] GIVEN a 2D scene physics contract is drafted, WHEN review begins, THEN it declares either `水平场景` or `垂直场景`.
- [ ] GIVEN a current playable scene exposes a runtime contract, WHEN smoke queries it, THEN `scene_type`, `movement_plane`, `layer_height_model_ready`, `cutaway_reveal_ready`, `walk_bounds`, `scale_reference`, `collision_semantics`, `occlusion_policy`, `special_surfaces`, `dynamic_behaviors`, `recovery_rule`, `authored_physical_unit_count`, and `source_gdd` are present.
- [ ] GIVEN a current playable scene is active, WHEN smoke asks for the current scene physics contract, THEN the returned `scene_id` follows Hub exterior, ship interior, and Exploration state transitions.
- [ ] GIVEN a scene contract is unknown, WHEN queried, THEN it returns `contract_complete=false` with a diagnostic error rather than a partial passing contract.

---

## Implementation Notes

Keep the runtime contract as a debug/QA surface until a dedicated data asset format is introduced. Do not move player input, Use dispatch, search rewards, repair outcomes, market logic, or persistence state into the contract. Contract fields should be simple, inspectable values that Godot smoke can assert without relying on UI labels.

---

## Out of Scope

- Story 002 owns Layer / Height, Cutaway / Reveal, and Floor State content depth.
- Story 003 owns unit catalog, collision, occlusion, scale, and special-surface catalog rules.
- Story 004 owns dynamic behavior priority and recovery edge cases.

---

## QA Test Cases

- **AC-1**: scene type is mandatory.
  - Given: each current playable scene contract.
  - When: smoke queries the contract.
  - Then: `scene_type` is exactly `水平场景` or `垂直场景`.
  - Edge cases: unknown scene id returns incomplete, not default horizontal.
- **AC-2**: required runtime fields are present.
  - Given: Hub exterior, ship interior, and Exploration contracts.
  - When: smoke reads all contract keys.
  - Then: every required field exists and is non-empty where applicable.
  - Edge cases: zero walk bounds or zero authored physical unit count fails.
- **AC-3**: active contract follows scene state.
  - Given: player moves Hub exterior -> ship interior -> Exploration -> Hub.
  - When: smoke calls `DebugCurrentScenePhysicsContract`.
  - Then: `scene_id` matches the active world scene.
  - Edge cases: Chart/UI screen must not become a fake physical scene contract.

---

## Test Evidence

**Story Type**: Integration  
**Required evidence**:
- `tests/smoke/session_shell_visual_probe.gd` or dedicated scene physics smoke must exist and pass.
- `production/qa/evidence/scene-physics-runtime-contract-shape-evidence.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: existing Polish 016 runtime contract probe.
- Unlocks: Story 002, Story 003.
