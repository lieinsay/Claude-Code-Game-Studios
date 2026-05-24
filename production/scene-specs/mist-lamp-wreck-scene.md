# 雾灯残骸场景规格

> **Scene ID**: `mist_lamp_wreck_scene`
> **Runtime Contract ID**: `exploration_mist_island`
> **Status**: spec_drafted
> **Owner**: Scene Composition System (#19) + Scene Physics Unit System (#20)
> **Last Updated**: 2026-05-24

## 1. Scene Identity

- Purpose: Provide the first exploration destination where the player reads a concrete wreck, searches it, sees pressure escalate, and returns to the ship.
- Emotional target: Quiet uncertainty, salvage focus, and clear retreat judgment.
- Core fantasy / pillars served: 未知带来温和压力; 规划先于冒险; 世界会回应照料.
- 3-second read: The player is on a misty floating island with a wreck, clues, a return ship, fog/water boundary, and threat/readiness markers.
- What this scene is not: It is not a generic fog field, text-only search menu, or final exploration system.

## 2. Scene Physics Contract

| Field | Value |
| --- | --- |
| Physics source | Runtime contract + authored prototype/instance data |
| Contract scene ID | `exploration_mist_island` |
| `physics_contract_complete` status | pass for current greybox contract |
| Scene physics type | `水平场景` |
| Movement plane | ground-plane four-directional movement within `ExplorationWalkBounds` |
| Layer / Height Model | `mist_wreck_ground_01` active floor; island, path, wreck, ship, fog/water boundary, and warning markers |
| Cutaway / Reveal Model | N/A true for current pass; no passable behind-object route in current slice |
| Unit catalog | `HubRuntime.DebugScenePhysicsContract("exploration_mist_island").scene_unit_catalog` |
| Unit prototypes | `src/presentation/playable_slice_authored_content.json::scene_unit_prototypes` |
| Placed unit instances | `src/presentation/playable_slice_authored_content.json::scene_unit_instances` filtered to `exploration_mist_island` |
| Collision / occlusion / scale | #20 runtime contract + prototype data |
| Special surfaces / dynamic behaviors / recovery | mist sea boundary is gameplay-affecting; fog and height markers are visual/readability policies; threat zone is dynamic warning evidence |

## 3. Entry / Exit

- Entry source: Arrival from `voyage_open_world_scene` or current playable route resolution.
- Spawn / arrival position: `ExplorationPlayerStart` / placed instance `scene_unit.instance.exploration_mist_island.player_marker`.
- Exit or return path: `return_helm_anchor` at the return ship; preheat then pilot return.
- Cancel / failure path: Search and return interactions are gated by proximity and modal/input focus.
- Saved-state return behavior: Durable progress restores exploration step, route, player state, carried reward, and last search point.
- Scene transition cleanup expectations: Returning to Hub hides exploration panels and world units.

## 4. Spatial Layout

- Main viewport composition: mist horizon and sea boundary behind a floating island path with a wreck to the right and return ship to the left.
- Walkable area: `ExplorationWalkBounds`.
- Boundaries: cliff edge, mist sea boundary, island upper/lower edges.
- Landmarks: wreck body, mast, return ship hull, return beacon, threat zone.
- Interaction anchors: search wreck and return helm anchor.
- Occlusion risks: wreck and return ship are blocking/readability objects; current pass treats behind-object reveal as N/A true because no passable behind route exists.
- Minimum greybox readability requirement: player can distinguish wreck/search area, return ship, water boundary, and threat marker without reading HUD text.

## 5. Critical Path

1. Arrive at the mist-lamp wreck scene from the route flow.
2. Approach the wreck and perform the three-step scan/echo/salvage search.
3. Move to the return ship and use the return helm to preheat and return.

## 6. Optional / Readability Beats

- Optional observation points: mast, clue shards, scan arc, return beacon, threat zone.
- Local identity details: mist horizon, floating island, cliff edge, wreck silhouette.
- Life / repair / damage traces: threat zone and hull/cargo feedback after search pressure.
- Player guidance embedded in the world: search glow, wreck highlight, return ship helm, beacon beam.
- UI assistance: route/resource/threat/hull labels may summarize state but cannot count as scene units.

## 7. State Variants

| Variant | Trigger / source state | World/playable scene evidence | UI assistance allowed |
| --- | --- | --- | --- |
| Initial | Arrive before search | wreck, mast, clues, return ship, fog and water boundary visible | route and threat summary |
| Progressed / completed | Search steps advance | pulse fill, threat zone, cargo prop, beacon/return state | search step and cargo numbers |
| Blocked / abnormal | Not near anchor or save/load modal owns focus | interaction prompt absent/disabled; return preheat remains staged | disabled/failure feedback |

## 8. Interaction Contract

| Anchor ID | Player action | Input / focus rule | Domain owner | Disabled / failure feedback | World evidence |
| --- | --- | --- | --- | --- | --- |
| `search_wreck_prop` | Scan / echo / salvage | Approach + use; three-stage presentation gate | Exploration / Resources / Threat | direct command blocked when too far | wreck prop, mast, scan arc, clue shards |
| `return_helm_anchor` | Preheat / pilot return | Approach + use; two-stage return gate | Hub / Navigation / Persistence | keeps player in scene until preheat completes | return ship hull and helm anchor |

## 9. Data / Runtime Contract

- Godot scene or runtime surface: `src/scenes/HubRuntime.cs`.
- Stable IDs: `scene_unit_prototypes` and `scene_unit_instances` in `src/presentation/playable_slice_authored_content.json`.
- Domain managers read: Navigation, Exploration, Resources, ModuleHull, Hub through existing `PlayableSliceDomainAdapter`.
- Domain managers mutated: no new gameplay authority; existing `AdvanceExploration()` and `ReturnToHub()` remain canonical.
- Persistence fields: existing durable progress and playable-slice snapshot.
- Signals / semantic events: search, pressure, return, save/load, hub summary sync.
- Focus and modal boundaries: ADR-0012 remains authoritative.
- Runtime debug/smoke hooks: `DebugScenePhysicsContract("exploration_mist_island")`.

## 10. Asset And Audio Needs

| Priority | Need | Supports identity / interaction / state / feedback | Current source | Gap owner |
| --- | --- | --- | --- | --- |
| P0 | mist island / wreck background | identity | greybox | art |
| P0 | wreck, mast, clue shards | interaction | greybox marker | art |
| P0 | return airship and helm | exit / return | greybox marker | art |
| P0 | threat zone / warning overlay | pressure | greybox overlay | art |
| P1 | fog/wreck ambience and scan cues | feedback | fallback / missing | audio |

## 11. QA Evidence

| Evidence type | Required artifact | Status |
| --- | --- | --- |
| Automated smoke | `tests/smoke/session_shell_visual_probe.gd`; `production/qa/evidence/scene-unit-placement-mist-lamp-wreck-evidence.md` | PASS |
| Focused data validation | `tests/integration/playable-slice/DomainAdapterTest.csproj`; `production/qa/evidence/scene-unit-placement-mist-lamp-wreck-evidence.md` | PASS |
| Screenshot / visual proof | existing visual probe evidence | pending refresh; current headless run skips screenshots with display-driver limitation |
| Codex review | implementation review | PASS for traceability/data linkage |
| User readability review | `production/playtests/scene-readability-mist-lamp-wreck.md` | pending |

## Readiness Checklist

- [x] Scene purpose, loop role, and emotional target are explicit.
- [x] Entry, exit, failure, and return paths are explicit.
- [x] Spatial layout names walkable space, boundaries, landmarks, and interaction anchors.
- [x] Scene Physics Contract is linked and passing for the current runtime contract.
- [x] Unit prototypes and placed instances are linked.
- [x] Scene units come from world/playable scene layer, not UI/HUD/buttons/labels/debug overlays.
- [x] Critical path and optional readability beats are documented.
- [x] At least three state variants are documented.
- [x] Interaction anchors name input/focus behavior and domain owner.
- [x] Runtime/state contract does not create a new gameplay authority.
- [ ] P0 asset/audio needs are resolved with final assets.
- [ ] Screenshot evidence and user review are refreshed after implementation.
