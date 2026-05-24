# 云织号船内分层场景规格

> **Scene ID**: `ship_interior_layered`
> **Runtime Contract ID**: `hub_ship_interior`
> **Status**: spec_drafted
> **Owner**: Scene Composition System (#19) + Scene Physics Unit System (#20)
> **Last Updated**: 2026-05-24

## 1. Scene Identity

- Purpose: Let the player read Cloudweaver as a home-like airship interior where route planning, storage, engine state, and exit affordances live in physical space.
- Emotional target: Safe, lived-in, repairable, and legible.
- Core fantasy / pillars served: 飞艇是家，不只是载具; 规划先于冒险; 世界会回应照料.
- 3-second read: The player is inside the airship, with cockpit, cargo, engine, exit, and foreground hull structure visible.
- What this scene is not: It is not a HUD dashboard, route menu, or final-art claim.

## 2. Scene Physics Contract

| Field | Value |
| --- | --- |
| Physics source | Runtime contract + authored prototype/instance data |
| Contract scene ID | `hub_ship_interior` |
| `physics_contract_complete` status | pass for current greybox contract |
| Scene physics type | Current runtime says `垂直场景`; design authority tracks this as layered ship interior pending future horizontal-layered reclassification |
| Movement plane | left/right primary with room depth and future vertical connectors |
| Layer / Height Model | `ship_deck_01` active floor; cockpit/cargo/engine room-scale bays as midground objects |
| Cutaway / Reveal Model | `front_wall_removed + active_floor_focus`; upper hull front wall is foreground occluder |
| Unit catalog | `HubRuntime.DebugScenePhysicsContract("hub_ship_interior").scene_unit_catalog` |
| Unit prototypes | `src/presentation/playable_slice_authored_content.json::scene_unit_prototypes` |
| Placed unit instances | `src/presentation/playable_slice_authored_content.json::scene_unit_instances` filtered to `hub_ship_interior` |
| Collision / occlusion / scale | #20 runtime contract + prototype data |
| Special surfaces / dynamic behaviors / recovery | cockpit glass is `visual_only_glass`; static blockers and trigger-only anchors use existing priority/recovery rules |

## 3. Entry / Exit

- Entry source: Boarding from initial island / hub exterior.
- Spawn / arrival position: `ShipInteriorPlayerStart` / placed instance `scene_unit.instance.hub_ship_interior.player_marker`.
- Exit or return path: `ship_exit_threshold` placed instance returns to exterior/hub flow.
- Cancel / failure path: Input gate blocks transitions while modal panels own focus.
- Saved-state return behavior: `PlayableSliceSceneState` restores screen, route, exploration step, player position, and footer.
- Scene transition cleanup expectations: No stale chart/exploration panels remain mounted as physical scene evidence.

## 4. Spatial Layout

- Main viewport composition: ship hull and three room-scale zones across the playable layer.
- Walkable area: `ShipInteriorWalkBounds`.
- Boundaries: hull outline, upper front wall, cockpit glass, room bays.
- Landmarks: cockpit bay, cargo bay, engine bay.
- Interaction anchors: helm console, storage crate, exit threshold.
- Occlusion risks: upper hull front wall and cockpit glass must never hide the player or core anchor beyond #20 limits.
- Minimum greybox readability requirement: cockpit/cargo/engine areas remain distinguishable without reading HUD text.

## 5. Critical Path

1. Enter ship interior from the dock/exterior.
2. Approach cockpit, cargo, or engine anchor.
3. Use helm/exit/storage affordance or return to the exterior flow.

## 6. Optional / Readability Beats

- Optional observation points: cockpit window, cargo load display, engine wear overlay.
- Local identity details: hull outline, room bay shapes, foreground wall cutaway.
- Life / repair / damage traces: cargo load and engine wear state hooks.
- Player guidance embedded in the world: console, crates, exit threshold, and room bays act as spatial anchors.
- UI assistance: HUD may summarize storage/hull/route state but cannot count as scene units.

## 7. State Variants

| Variant | Trigger / source state | World/playable scene evidence | UI assistance allowed |
| --- | --- | --- | --- |
| Initial | Start or normal hub entry | hull, cockpit, cargo, engine, helm, crate, exit units visible | brief prompt/status only |
| Progressed / completed | Cargo gained or route planned | cargo load fill / storage crate state hook; helm remains route anchor | cargo/hull numbers |
| Blocked / abnormal | damaged hull or input modal active | engine wear overlay / disabled route use feedback | modal explanation allowed |

## 8. Interaction Contract

| Anchor ID | Player action | Input / focus rule | Domain owner | Disabled / failure feedback | World evidence |
| --- | --- | --- | --- | --- | --- |
| `helm_console_prop` | Open chart / route planning | Approach + use, blocked by modal focus | Chart / Hub | route not available feedback | helm console instance |
| `storage_crate_prop` | Read cargo/storage state | Approach + use or passive readable state | Resources | capacity feedback | storage crate instance |
| `ship_exit_threshold` | Leave ship interior | Approach + use | Hub | blocked while modal focus owns input | exit threshold instance |

## 9. Data / Runtime Contract

- Godot scene or runtime surface: `src/scenes/HubRuntime.cs`.
- Stable IDs: `scene_unit_prototypes` and `scene_unit_instances` in `src/presentation/playable_slice_authored_content.json`.
- Domain managers read: Hub, Chart, Resources, ModuleHull through existing `PlayableSliceDomainAdapter`.
- Domain managers mutated: none by scene-unit authoring data.
- Persistence fields: no new persistent gameplay authority.
- Signals / semantic events: existing route, cargo, save/load, and hub signals.
- Focus and modal boundaries: ADR-0012 remains authoritative.
- Runtime debug/smoke hooks: `DebugScenePhysicsContract("hub_ship_interior")`.

## 10. Asset And Audio Needs

| Priority | Need | Supports identity / interaction / state / feedback | Current source | Gap owner |
| --- | --- | --- | --- | --- |
| P0 | airship interior background | identity | greybox | art |
| P0 | helm console | interaction | greybox marker | art |
| P0 | storage crates | state / interaction | greybox marker | art |
| P0 | engine bench / wear overlay | state | greybox overlay | art |
| P1 | cabin ambience | feedback | fallback / missing | audio |

## 11. QA Evidence

| Evidence type | Required artifact | Status |
| --- | --- | --- |
| Automated smoke | `tests/smoke/session_shell_visual_probe.gd` | pending rerun |
| Focused data validation | `tests/integration/playable-slice/DomainAdapterTest.csproj` | pending rerun |
| Screenshot / visual proof | existing visual probe evidence | pending refresh |
| Codex review | implementation review | pending |
| User readability review | manual checklist | pending |

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
- [ ] Automated evidence, screenshot evidence, Codex review, and user review are refreshed after implementation.
