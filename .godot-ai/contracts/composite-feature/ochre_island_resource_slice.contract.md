# Godot Asset Contract: ochre_island_resource_slice

## Metadata

- Asset Type: composite-feature
- Stable ID: ochre_island_resource_slice
- Display Name: 赭石岛资源采集切片
- Source Requirement: `production/scene-specs/ochre-island-scene.md`; `production/unit-specs/fixed-scene-objects/banded-iron-ore.md`
- Lifecycle State: review-ready

## Intent

- Player/User-facing purpose: 玩家抵达赭石岛，识别条带状铁矿，完成一次世界层采集，并通过返航点离开。
- Design role: 关闭当前最小 release blocker，补齐赭石岛 #20 / 独立实现 / 作者化证据闭环。
- In scope: 子合同定义的 scene 和 fixed-world-unit，二者的装配、验证和证据引用。
- Non-goals: 航行大场景实现、市场内容、完整经济链、最终美术、音频成品。

## Godot Outputs

- Scene paths: `src/scenes/ochre/OchreIslandScene.tscn`; `src/scenes/units/BandedIronOre.tscn`
- Script paths: `src/scenes/ochre/OchreIslandScene.cs`; `src/scenes/units/BandedIronOre.cs`
- Resource paths: `src/presentation/playable_slice_authored_content.json` may be updated only for explicit authoring records needed by evidence.
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `.godot-ai/verification/composite-feature/ochre_island_resource_slice.verification.md`

## Runtime Boundary

- Owns: Composition of the approved scene and unit assets into a verifiable Godot asset slice.
- Reads: Child contracts, source specs, existing authoring fixture conventions.
- Emits: Verification evidence and handoff links.
- Must not own: Full runtime route flow, global resource economy, release decision itself.

## Decision Boundaries

- AI may decide: Execution ordering, minimal node hierarchy, verification file structure.
- AI must ask before: Destructive edits, replacing current playable route, adding dependencies, expanding scope beyond approved child contracts.

## Acceptance Evidence

- Node/resource evidence: Both child assets exist and are linked.
- Visual evidence: Screenshot or editor capture of the assembled resource island.
- Runtime evidence: Harvest and return anchors can be exercised or are explicitly blocked by unavailable editor session.
- Log/test evidence: Smoke/build/evidence files cite concrete outputs.

## Execution Readiness

- Blocking ambiguity: Godot AI MCP editor session availability is unknown.
- Required MCP/editor state: Godot editor session for this repository.
- Safe to execute: true

## Asset-Type Specific Requirements

## Child Contracts

| Asset Type | Stable ID | Contract Path | Dependency Role |
| --- | --- | --- | --- |
| scene | ochre_island_scene | `.godot-ai/contracts/scene/ochre_island_scene.contract.md` | Parent playable space |
| fixed-world-unit | scene_unit.prototype.banded_iron_ore | `.godot-ai/contracts/fixed-world-unit/scene_unit.prototype.banded_iron_ore.contract.md` | Resource interaction unit |

## Residual Ambiguity

- Non-blocking assumptions: Parent execution may be blocked while child contracts remain review-ready.
- Blocking questions: None for review; execution must inspect Godot AI MCP/editor availability.

