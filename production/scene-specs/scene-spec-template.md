# Scene Specification Template and Checklist

> **Owner**: Scene Composition System (#19)
> **Applies To**: every enterable scene before production implementation readiness
> **Depends On**: Scene Physics Unit System (#20) for all physical-unit details
> **Evidence Boundary**: UI, HUD, buttons, labels, menus, and debug overlays can assist but never count as scene units or physical acceptance evidence.

## How To Use

Copy this template for each proposed enterable scene or link an equivalent complete design note from the coverage registry. A scene can enter `implementation_ready` only when every required line below is filled or explicitly marked `N/A true` with a reason.

Do not redefine #20 physics rules here. Link the current Scene Physics Contract, smoke evidence, or exemption. #19 owns scene completeness; #20 owns physical-unit details.

## Header

| Field | Value |
| --- | --- |
| Scene ID |  |
| Player-facing scene name |  |
| Owning loop node | Hub / Chart / Exploration / Repair / Market / Settlement / Other |
| Current lifecycle state | concept_needed / spec_drafted / codex_review / user_review / implementation_ready / greybox / asset_gate / playtest_ready / accepted / blocked |
| Source GDDs |  |
| Source story or design note |  |
| Last reviewed |  |
| Review owners | Codex / user / QA |

## 1. Scene Identity

- Purpose:
- Emotional target:
- Core fantasy / pillars served:
- What the player should understand within 3 seconds:
- What this scene is not:

## 2. Scene Physics Contract

Link to the #20 contract source instead of restating physics detail.

| Field | Value |
| --- | --- |
| Physics source | Runtime contract / design spec / evidence doc / exemption |
| Contract scene ID |  |
| `physics_contract_complete` status | pass / fail / pending / exempt |
| Scene physics type | `水平场景` / `垂直场景` / N/A true |
| Movement plane | link or summary |
| Layer / Height Model | link or summary |
| Cutaway / Reveal Model | link or summary |
| Unit catalog | link or summary |
| Collision / occlusion / scale | link or summary |
| Special surfaces / dynamic behaviors / recovery | link or summary |
| Exemption reason, if no gameplay-relevant physical units |  |

## 3. Entry / Exit

- Entry source:
- Spawn / arrival position:
- Exit or return path:
- Cancel / failure path:
- Saved-state return behavior:
- Scene transition cleanup expectations:

## 4. Spatial Layout

- Main viewport composition:
- Walkable area:
- Boundaries:
- Landmarks:
- Interaction anchors:
- Occlusion risks:
- Minimum greybox readability requirement:

## 5. Critical Path

1. [First scene action]
2. [Second scene action]
3. [Completion or exit action]

## 6. Optional / Readability Beats

- Optional observation points:
- Local identity details:
- Life / repair / damage traces:
- Player guidance embedded in the world:
- UI assistance, if any:

## 7. State Variants

At least three variants are required unless the scene is explicitly exempt.

| Variant | Trigger / source state | World/playable scene evidence | UI assistance allowed |
| --- | --- | --- | --- |
| Initial |  |  |  |
| Progressed / completed |  |  |  |
| Blocked / abnormal |  |  |  |

## 8. Interaction Contract

| Anchor ID | Player action | Input / focus rule | Domain owner | Disabled / failure feedback | World evidence |
| --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |

## 9. Data / Runtime Contract

- Godot scene or runtime surface:
- Stable IDs:
- Domain managers read:
- Domain managers mutated:
- Persistence fields:
- Signals / semantic events:
- Focus and modal boundaries:
- Runtime debug/smoke hooks:

## 10. Asset And Audio Needs

| Priority | Need | Supports identity / interaction / state / feedback | Current source | Gap owner |
| --- | --- | --- | --- | --- |
| P0 |  |  |  |  |
| P1 |  |  |  |  |

## 11. QA Evidence

| Evidence type | Required artifact | Status |
| --- | --- | --- |
| Automated smoke |  | pending |
| Screenshot / visual proof |  | pending |
| Codex review |  | pending |
| User readability review |  | pending |

Human QA must answer:

- Where am I?
- What can I do here without reading a developer explanation?
- How do I leave or continue?
- What changed after the relevant action?
- Does UI/HUD support the scene without dominating or replacing it?

## Readiness Checklist

- [ ] Scene purpose, loop role, and emotional target are explicit.
- [ ] Entry, exit, failure, and return paths are explicit.
- [ ] Spatial layout names walkable space, boundaries, landmarks, and interaction anchors.
- [ ] Scene Physics Contract is linked and passing, or #20 exemption is explicit.
- [ ] Scene units come from world/playable scene layer, not UI/HUD/buttons/labels/debug overlays.
- [ ] Critical path and optional readability beats are documented.
- [ ] At least three state variants are documented or explicitly exempt.
- [ ] Interaction anchors name input/focus behavior and domain owner.
- [ ] Runtime/state contract does not create a new gameplay authority.
- [ ] P0 asset/audio needs are traceable to identity, interaction, state, or feedback.
- [ ] Automated evidence, screenshot evidence, Codex review, and user review paths are named.
