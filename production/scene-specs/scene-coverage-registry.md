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
| `hub_island_dock` | Glass Port island dock / Hub exterior | Start / return-to-Hub | `covered-by-current-note` | Runtime contract complete for `hub_island_dock`; horizontal scene with world/playable units | `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`; `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`; `production/qa/evidence/scene-physics-runtime-contract-shape-evidence.md`; `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md` | Extract standalone scene spec before Story 002 completeness gate or release-gate claim. |
| `hub_ship_interior` | Cloudweaver ship interior | Boarding ramp from Hub exterior | `covered-by-current-note` | Runtime contract complete for `hub_ship_interior`; vertical scene with active-floor/front-wall reveal policy | `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`; `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`; `production/qa/evidence/scene-physics-layer-cutaway-floor-state-evidence.md`; `production/qa/evidence/scene-physics-unit-catalog-evidence.md` | Extract standalone spec that separates cockpit, cargo, engine, exit, state variants, and P0 asset gaps. |
| `chart_table_scene` | Chart table surface | Ship interior helm anchor | `covered-by-current-note` | Not currently a separate #20 playable scene contract; treated as authored scene surface inside ship-interior access until standalone physical scene is required | `tests/smoke/session_shell_visual_probe.gd`; `design/gdd/scene-composition-system.md`; `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md` | Story 002/003 must decide whether Chart remains an anchored UI-assisted scene surface or needs its own #20 contract. |
| `exploration_mist_island` | Mist-island scavenge scene | Chart departure to `route.mist` | `covered-by-current-note` | Runtime contract complete for `exploration_mist_island`; horizontal scene with search/return anchors, hazard boundaries, and recovery | `production/polish-backlog/story-polish-015-search-return-microgame-design-note.md`; `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`; `production/qa/evidence/scene-physics-unit-catalog-evidence.md`; `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md` | Extract standalone spec before expanding exploration content or claiming release readiness. |
| `repair_node_scene` | World repair / unlock point | Future repair-site entry | `tracked-gap` | #20 contract required before implementation readiness because repair point will contain gameplay-relevant physical units | `design/gdd/world-repair-unlock.md`; `design/gdd/scene-composition-system.md` | Draft scene spec before any repair scene is considered visually complete. |
| `market_scene` | Old market / settlement trade scene | Future market/settlement entry | `tracked-gap` | #20 contract required before implementation readiness because market stalls, NPCs, goods, and passability are physical scene units | `design/gdd/port-village-market.md`; `design/gdd/scene-composition-system.md` | Draft scene spec before market scene greybox or visual completion claim. |

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
| 2D scene spec includes or links a #20 Scene Physics Contract | The registry has a `#20 physics input` column and current runtime-contract links for Hub exterior, ship interior, and Exploration. |
| No-physical-unit scenes must explicitly state why #20 does not apply | Template includes an exemption field; no current row is silently exempt. Chart table is flagged for Story 002/003 decision instead of being treated as exempt. |
| Repair and market scenes need specs before visual completion | `repair_node_scene` and `market_scene` are tracked gaps with required next actions before visual completion claims. |

## Follow-Up Queue

1. Story 002 should convert these registry statuses into a concrete completeness/evidence gate without redefining #20 physics detail.
2. Story 003 should harden UI-vs-scene evidence rejection for registry rows and release evidence.
3. Story 004 should attach user readability review questions and verdicts to each scene row before release readiness.
