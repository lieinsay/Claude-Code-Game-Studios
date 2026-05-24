# Scene Coverage Registry

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-001-scene-spec-template-coverage-registry.md`
> **Last Updated**: 2026-05-24
> **Registry Rule**: every enterable scene must link a scene spec, a complete equivalent source note, a pending spec gap, or an explicit #20 exemption before it can be considered production-ready.

## Registry Rules

- A scene is a player-enterable world/playable space, not a UI panel.
- UI/HUD/buttons/labels/debug overlays may support evidence but cannot satisfy scene-unit, physical-unit, or readability evidence.
- #19 registry rows may summarize #20 status, but #20 remains the only source of physical-unit detail.
- Repair and market scene entries are tracked even while their full enterable scenes are not yet implemented, because #19 requires their specs before they can be considered visually complete.
- `Scene spec status` means the #19 composition spec status, not runtime feature completion.

## Status Vocabulary

| Status | Meaning |
| --- | --- |
| `covered-by-current-note` | Existing story/GDD/evidence contains enough scene-spec material for Story 001 coverage, but may need extraction to a standalone spec before release gate. |
| `template-ready` | Reusable template exists; concrete scene spec still needs to be drafted. |
| `tracked-gap` | Scene is known and required, but no complete enterable-scene spec exists yet. |
| `exempt-no-physical-units` | Scene has no gameplay-relevant physical units and explicitly states why #20 does not apply. |

## Current Enterable Scene Coverage

| Scene ID | Player-facing scene | Current entry source | Scene spec status | #20 physics input | Current evidence / source | Required next action |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_island_scene` | 初始岛屿 | New game start / return-to-origin | `covered-by-current-note` | Historical runtime contract exists under `hub_island_dock`; must be renamed or mapped in standalone spec as a horizontal scene with world/playable units | `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`; `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`; `production/qa/evidence/scene-physics-runtime-contract-shape-evidence.md`; `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md` | Extract standalone initial-island spec and decide whether runtime ID remains aliased to `hub_island_dock` or is renamed. |
| `ship_interior_layered` | 云织号船内分层水平场景 | Boarding from initial island / return from voyage | `spec_drafted` | Runtime contract exists under `hub_ship_interior`; first prototype/placed-instance authoring slice now links to `src/presentation/playable_slice_authored_content.json` and `production/scene-specs/ship-interior-layered-scene.md` | `design/gdd/airship-hub.md`; `production/scene-specs/ship-interior-layered-scene.md`; `production/qa/evidence/scene-physics-layer-cutaway-floor-state-evidence.md`; `production/qa/evidence/scene-physics-unit-catalog-evidence.md` | Use the ship-interior slice to validate unit prototype/placed-instance workflow before migrating other scenes. |
| `voyage_open_world_scene` | 航行大场景 | Departure from initial island to demo destinations | `spec_drafted` | #20 contract required before implementation readiness: pseudo-3D camera-aligned flight, route boundaries, risk objects, cloud/fog special surfaces, recovery rules | `production/scene-specs/voyage-open-world-scene.md`; `design/gdd/navigation-route-risk.md`; `design/gdd/scene-composition-system.md` | Review and refine independent voyage scene spec before implementation readiness or readability review. |
| `mist_lamp_wreck_scene` | 雾灯残骸 | Voyage arrival from `voyage_open_world_scene` | `covered-by-current-note` | Historical runtime evidence exists under `exploration_mist_island`, but standalone spec must narrow the scene identity to mist-lamp wreck instead of generic mist-island scavenge | `design/gdd/exploration-scavenge-scenario.md`; `production/polish-backlog/story-polish-015-search-return-microgame-design-note.md`; `production/qa/evidence/scene-physics-unit-catalog-evidence.md`; `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md` | Extract standalone mist-lamp-wreck spec and map existing exploration evidence to the new scene identity. |
| `old_market_edge_scene` | 旧集市边缘 | Voyage arrival from `voyage_open_world_scene` | `tracked-gap` | #20 contract required before implementation readiness because market edges, stalls, NPCs, goods, and passability are physical scene units | `design/gdd/port-village-market.md`; `design/gdd/scene-composition-system.md` | Promote old market from future market gap to current demo destination; draft scene spec before visual completion claim. |
| `repair_node_scene` | World repair / unlock point | Future repair-site entry | `tracked-gap` | #20 contract required before implementation readiness because repair point will contain gameplay-relevant physical units | `design/gdd/world-repair-unlock.md`; `design/gdd/scene-composition-system.md` | Draft scene spec before any repair scene is considered visually complete; not part of the current demo readability queue unless explicitly added. |

## Non-Scene / UI Surfaces

These surfaces can appear in the player flow but cannot be counted as scene units or physical acceptance evidence.

| Surface | Classification | Evidence rule |
| --- | --- | --- |
| HUD status panels | UI assistance | May summarize resources, hull, route, or threat state; cannot prove scene identity or physical unit readiness. |
| Chart buttons / route buttons | UI controls | May confirm route choice; cannot replace the helm/table world anchor. |
| Save / load / delete buttons | UI controls | Persistence affordances only; not scene units. |
| Debug labels / smoke-only hooks | Debug evidence | May help automated assertions; cannot satisfy human readability or scene-unit evidence. |
| Onboarding hint text | UI assistance | May guide first loop; cannot replace visible world anchors. |

## Story 001 Acceptance Mapping

| Acceptance criterion | Coverage |
| --- | --- |
| New enterable scene requires a scene specification before production readiness | `production/scene-specs/scene-spec-template.md` defines the reusable required shape and checklist. This registry requires every scene row to link a spec, equivalent note, tracked gap, or explicit exemption. |
| 2D scene spec includes or links a #20 Scene Physics Contract | The registry has a `#20 physics input` column and current runtime-contract links or tracked gaps for initial island, layered ship interior, voyage open world, mist-lamp wreck, and old market edge. |
| No-physical-unit scenes must explicitly state why #20 does not apply | Template includes an exemption field; no current demo row is silently exempt. Chart table is now treated as a ship-interior support surface unless later promoted to a physical scene. |
| Current demo scenes need specs before release readiness | `initial_island_scene`, `ship_interior_layered`, `voyage_open_world_scene`, `mist_lamp_wreck_scene`, and `old_market_edge_scene` each need standalone specs or explicit mapped source notes before release-readiness claims. |
| Future repair scenes need specs before visual completion | `repair_node_scene` remains a tracked future gap with required next actions before visual completion claims. |

## Follow-Up Queue

1. Story 002 should convert these registry statuses into a concrete completeness/evidence gate without redefining #20 physics detail.
2. Story 003 should harden UI-vs-scene evidence rejection for registry rows and release evidence.
3. Story 004 should attach user readability review questions and verdicts to each current demo scene row before release readiness: initial island, ship interior, voyage open world, mist-lamp wreck, and old market edge.

## Release Handoff Status

Release handoff input lives in `production/scene-specs/scene-release-gate-handoff.md`. Current status is `BLOCKED_FOR_RELEASE` until user readability reviews are recorded or explicitly waived, and until repair/market tracked gaps are resolved or waived.
