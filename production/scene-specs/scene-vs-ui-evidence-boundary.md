# Scene Versus UI Evidence Boundary

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-003-scene-vs-ui-evidence-boundary.md`
> **Last Updated**: 2026-05-24
> **Purpose**: define which evidence is allowed to prove a scene, and which UI/HUD evidence must be ignored.

## Boundary Rule

Scene evidence must originate from the `world_playable_scene` layer or from a documented #20 Scene Physics Contract that itself rejects UI evidence. UI/HUD may assist comprehension, but it cannot satisfy scene completeness.

`ui_boundary_passed` is true only when all rows below pass:

```text
ui_boundary_passed =
    hud_not_dominant
    AND no_ui_as_scene_unit
    AND no_ui_as_identity_node
    AND no_ui_as_interaction_anchor
    AND no_ui_as_physics_contract_proof
    AND ui_only_evidence_fails
    AND modal_focus_isolated
    AND world_evidence_remains_mounted
```

## Evidence Classification

| Evidence source | Allowed use | Forbidden use |
| --- | --- | --- |
| World/playable terrain, walkable bounds, landmarks, props, doors, ramps, wrecks, stalls, NPCs, and return beacons | Scene identity, physical units, interaction anchors, state variants, viewport readability, #20 contract evidence | None when authored as world/playable evidence and linked to a domain owner |
| Scene Physics Contract fields | Physical scene proof when `physical_unit_source_layer = world_playable_scene` and `ui_evidence_allowed = false` | Proof of scene completion if the contract is missing, pending, failed, or inferred from art alone |
| HUD labels, status panels, route text, save/load text, onboarding hints, buttons, menus, modal panels, debug labels, and smoke-only overlay text | Assistive text, accessibility support, focus-routing proof, debug diagnostics | Physical scene units, scene identity nodes, interaction anchors, viewport identity, physical contract proof, or human readability replacement |

## UI Dominance Gate

Visual QA must record `hud_not_dominant = true` before a scene can pass release readiness.

| Check | Pass threshold | Blocks when |
| --- | --- | --- |
| `primary_scene_viewport_share` | Target 65%; acceptable range 55-85% for MVP greybox scenes | Main world identity is below 55%, hidden, or only visible as a narrow strip behind UI |
| `world_identity_visible_with_hud` | At least one world/playable identity node is visible while HUD is present | Only labels, panels, buttons, or debug overlays identify the place |
| `core_anchor_visible_with_hud` | At least one relevant spatial anchor is visible for the active scene | The only available action is a UI button with no world/playable anchor |

Temporary modals may cover the scene while active. They do not erase existing world evidence, but their open state cannot be used as scene completion evidence.

## Automated Rejection Cases

The following synthetic cases must fail scene readiness:

| Case ID | Evidence package | Expected result |
| --- | --- | --- |
| `ui_only_surface` | HUD title, save/load buttons, route button, debug label; no world/playable scene nodes | `scene_readiness = fail` |
| `debug_overlay_only` | Debug current-scene label and smoke hook text; no visible scene identity node | `scene_readiness = fail` |
| `button_only_interaction` | Clickable route/search/return button; no helm/table/wreck/return-ship/repair/stall/NPC anchor | `scene_readiness = fail` |
| `ui_physics_contract` | Any physics contract or unit catalog row with `physical_unit_source_layer != world_playable_scene` or `ui_evidence_allowed = true` | `scene_readiness = fail` |

## Automated Smoke Evidence Requirements

Smoke or integration evidence for scene identity must prove:

- visible world/playable identity nodes
- main viewport coverage through world/playable scene nodes
- spatial interaction anchors, not buttons alone
- current #20 physical contract fields with `physical_unit_source_layer = world_playable_scene`
- `ui_evidence_allowed = false` for every physical unit, dynamic behavior, and recovery row
- focus isolation when a modal or semi-modal UI is active
- underlying world evidence remains mounted while UI is active

Current runtime smoke already includes world evidence for Hub exterior, ship interior, Chart table surface, and Exploration. UI evidence in the same smoke remains assistive only.

## Focus Isolation Boundary

ADR-0012 remains the input authority. When modal or semi-modal UI is active:

- UIManager owns UI focus, modal stack, and input routing.
- World movement/use input is blocked or isolated according to the active input layer.
- Disabled or unavailable UI controls must leave the focus chain or reject activation.
- Underlying world/playable scene evidence must remain mounted and visible when the UI mode is a panel overlay rather than a full scene transition.
- Focus isolation cannot be used to delete, hide, or replace the scene evidence required by #19.

## Current Scene Classification

| Scene / surface | Classification | Boundary result |
| --- | --- | --- |
| `hub_island_dock` | Enterable world/playable scene with HUD assistance | UI cannot count; world nodes and #20 contract remain required |
| `hub_ship_interior` | Enterable world/playable scene with room/status UI assistance | UI cannot count; ship interior units and #20 contract remain required |
| `chart_table_scene` | Authored chart table surface anchored inside ship interior; UI-assisted but not a separate enterable physical scene yet | Chart buttons/route UI do not count; the table surface can support scene evidence only as a world anchor inside `hub_ship_interior` until a standalone #20 contract exists |
| `exploration_mist_island` | Enterable world/playable scene with HUD pressure/readout assistance | UI cannot count; island body, wreck, return beacon, and #20 contract remain required |
| `repair_node_scene` | Future enterable repair site | UI repair panel cannot count; repair point/station/NPC/world anchor and #20 contract are required before visual completion |
| `market_scene` | Future enterable market/settlement scene | Market UI cannot count; stall/NPC/goods/passability anchors and #20 contract are required before visual completion |

## Review Checklist

- [ ] UI/HUD does not dominate or hide the active world identity.
- [ ] UI/HUD/buttons/menus/labels/debug overlays are ignored for scene unit counts.
- [ ] UI/HUD/buttons/menus/labels/debug overlays are ignored for identity nodes and interaction anchors.
- [ ] UI/HUD/buttons/menus/labels/debug overlays are ignored for #20 physical contract proof.
- [ ] UI-only evidence packages fail readiness.
- [ ] Modal or semi-modal focus isolates world input without deleting underlying scene evidence.
- [ ] Any exception is an explicit user waiver, not an automated pass.
