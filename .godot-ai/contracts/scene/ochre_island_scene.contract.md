# Godot Asset Contract: ochre_island_scene

## Metadata

- Asset Type: scene
- Stable ID: ochre_island_scene
- Display Name: 赭石岛
- Source Requirement: `production/scene-specs/ochre-island-scene.md`
- Lifecycle State: review-ready

## Intent

- Player/User-facing purpose: 玩家从航行大场景抵达一个小型资源岛，识别条带状铁矿，采集资源，然后通过返航点离开。
- Design role: 当前 demo 第二个非市场目的地，证明航行可抵达多个可读世界场景。
- In scope: 赭色岛体灰盒、可行走路径、边界、条带状铁矿摆放、采集锚点、返航锚点、采集前后状态、截图 / smoke 证据。
- Non-goals: 市场、NPC、完整经济链、复杂采矿工具、矿脉再生、冶炼、主动战斗、替换旧市场场景。

## Godot Outputs

- Scene paths: `src/scenes/ochre/OchreIslandScene.tscn`
- Script paths: `src/scenes/ochre/OchreIslandScene.cs`
- Resource paths: `src/presentation/playable_slice_authored_content.json` may gain `ochre_island_scene` authoring records only if execute plan chooses the existing authoring-data route.
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `production/qa/evidence/ochre-island-scene-godot-verification.md`; screenshot under `production/qa/evidence/`.

## Runtime Boundary

- Owns: Local scene node hierarchy, visual greybox layout, local interaction anchors for ore harvest and return.
- Reads: `scene_unit.prototype.banded_iron_ore`, Resources capacity / reward state, Navigation / Hub return availability, player input focus state.
- Emits: `ore_harvested`, `resource_collected`, `return_departure_requested`, local visual state changes.
- Must not own: Global Resources economy, route selection, Hub state, canonical save/load, market content, UI-only gate evidence.

## Decision Boundaries

- AI may decide: Node names, simple greybox dimensions, initial anchor positions, colors within the ochre/mineral identity, screenshot labels in evidence.
- AI must ask before: Deleting or replacing existing `HubRuntime` nodes, migrating project structure, adding dependencies, adding market/NPC/economy systems, changing approved design scope.

## Acceptance Evidence

- Node/resource evidence: Scene hierarchy includes island ground, walk path, ore instance, return anchor, boundary units, and player spawn.
- Visual evidence: Screenshot shows赭色岛体、条带状矿脉、返航点、可行走路径; UI is assistive only.
- Runtime evidence: Player can move within bounds, use ore anchor once, see harvested state, and use return anchor without a modal UI owning the action.
- Log/test evidence: Godot smoke or verification log confirms scene loads, anchors exist, physical units are world/playable evidence, and no UI-only evidence is counted.

## Execution Readiness

- Blocking ambiguity: Godot AI MCP editor session availability is unknown.
- Required MCP/editor state: A Godot editor session for this repository, or an approved file-level asset execution path recorded by review.
- Safe to execute: true

## Asset-Type Specific Requirements

- Layout: Small horizontal island ground plane with a short path from spawn to ore and return anchor.
- Entry/exit: Entry from `voyage_open_world_scene`; exit via `ochre_return_anchor`.
- Player spawn: Safe landing point near island edge, not overlapping ore or boundary.
- Boundaries: Island edge / cloud sea / rock wall block movement or clamp recovery.
- Landmarks: Ochre island mass, banded iron ore, return point.
- Interaction anchors: `banded_iron_ore_anchor`, `ochre_return_anchor`.
- Authored world units: Player marker, island mass, walk path, banded iron ore, return anchor, boundary surface.
- State variants: initial, harvested, blocked.
- Screenshot/smoke evidence: Required before release handoff.

## Residual Ambiguity

- Non-blocking assumptions: First-pass visuals can be greybox; precise art style can be refined later through directed modification.
- Blocking questions: None for contract review; editor session availability is checked during execution.

