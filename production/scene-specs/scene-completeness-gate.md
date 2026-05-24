# Scene Completeness Gate and Evidence Contract

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-002-scene-completeness-gate-evidence.md`
> **Last Updated**: 2026-05-24
> **Purpose**: define how a scene evidence package is reviewed before any release-readiness claim.

## Gate Rule

`scene_complete` is true only when every required dimension below passes. Any `fail`, `pending`, `tracked-gap`, or missing evidence blocks completion unless a user-approved waiver is recorded in the evidence package.

```text
scene_complete =
    purpose_ready
    AND scene_physics_ready
    AND space_ready
    AND behavior_ready
    AND state_ready
    AND presentation_ready
    AND technical_ready
    AND qa_ready
    AND codex_review_passed
    AND user_review_passed
```

## Required Dimensions

| Dimension | Required evidence | Blocks when |
| --- | --- | --- |
| `purpose_ready` | Scene identity, loop role, emotional target, and 3-second identity target are documented. | Purpose is vague, only names a UI screen, or does not state why the player enters. |
| `scene_physics_ready` | #20 Scene Physics Contract passes, or the scene explicitly records `N/A true` because no gameplay-relevant physical units exist. | #20 contract is missing, pending, failed, or silently skipped. |
| `space_ready` | Entry, exit, walkable area, boundaries, landmarks, and interaction anchors are documented from world/playable scene evidence. | Node existence or HUD text is the only spatial proof. |
| `behavior_ready` | Critical path, optional behavior, cancellation/failure path, and interaction anchors are documented. | The primary action exists only as a UI button or unanchored command. |
| `state_ready` | At least three state variants or explicit exemption are documented with source state and world/playable scene evidence. | Variants are missing, only UI text changes, or source state is ambiguous. |
| `presentation_ready` | P0/P1 visual, VFX, and audio needs are traced to identity, interaction, state, or feedback. | Any P0 current-scene asset gap is unresolved and has no explicit user waiver. |
| `technical_ready` | Godot scene/runtime surface, stable IDs, domain managers read, domain managers mutated, persistence fields, signals, focus/modal boundaries, and debug/smoke hooks are documented. | Scene layer creates new gameplay authority, duplicates persistent state, or bypasses domain owners. |
| `qa_ready` | Automated smoke, screenshot/visual proof when applicable, Codex review, and user readability review paths are named. | Smoke only proves node existence, screenshots are missing for visual claims, or human review is absent. |
| `codex_review_passed` | Codex review has no blocker across purpose, space, behavior, state, presentation, technical, and QA lines. | Any Codex blocker remains open. |
| `user_review_passed` | User readability review has no blocker or has an explicit user waiver. | User review is missing, BLOCKED, or says the scene identity/player flow does not read. |

## User Readability Release Handoff

Story 004 adds the human review checklist and release handoff packet:

- `production/playtests/scene-composition-user-readability-checklist.md`
- `production/scene-specs/scene-release-gate-handoff.md`

Codex review is necessary but not sufficient. A user verdict of `BLOCKED` keeps `user_review_passed = false` and prevents release handoff until the blocker is resolved or explicitly waived by the user. User review may block for missing fantasy, missing requirements, unclear identity, undesirable player flow, UI dominance, or newly discovered demands that must be written back into the scene spec.

Required user readability questions:

- Where am I?
- What can I do here?
- How do I leave or continue?
- What changed?
- Does UI/HUD support rather than dominate?
- Does the scene match the intended fantasy?

## Automated Smoke Evidence Requirements

For a greybox or stronger runtime scene, automated smoke evidence must verify all applicable rows:

| Smoke line | Required proof |
| --- | --- |
| Visible scene identity nodes | Named world/playable scene nodes exist and are visible for the active scene. |
| Main viewport coverage | Scene art occupies the main viewport enough to prove spatial identity, not a text-only strip. |
| Interaction anchors | Primary actions have spatial anchors such as ramp, helm, wreck, return ship, repair point, stall, or NPC. |
| Focus isolation | UI controls that are not available in the active scene state leave the focus chain or are blocked. |
| Core route behavior | Route/scene transitions preserve the expected loop behavior relevant to that scene. |
| Physical contract evidence | Runtime smoke or equivalent evidence links to #20 contract fields for gameplay-relevant physical units. |

Node existence alone is insufficient. The smoke package must pair node visibility with viewport coverage, anchor semantics, state transitions, and non-UI scene evidence.

## Scene Versus UI Boundary

Story 003 adds the companion boundary contract at `production/scene-specs/scene-vs-ui-evidence-boundary.md`.

The completeness gate must treat the following as assistive-only evidence:

- HUD labels and status panels
- buttons, menus, modal panels, and route controls
- save/load/delete controls
- onboarding hint text
- debug labels, debug overlays, and smoke-only diagnostic text

These surfaces cannot satisfy `space_ready`, `scene_physics_ready`, `behavior_ready`, `presentation_ready`, or `qa_ready` unless paired with world/playable scene evidence. A UI-only evidence package fails readiness even when every UI control is visible, clickable, and correctly labelled.

For readability, `hud_not_dominant = true` must be recorded before any release-readiness claim. `primary_scene_viewport_share` targets 65% and blocks when the main world identity is hidden, below 55%, or reduced to a text-only strip behind UI.

## Asset Gate Requirements

Every P0 current-scene asset row must map to one of:

- `identity`
- `interaction`
- `state_variant`
- `feedback`

Unresolved P0 gaps block release readiness unless the evidence package includes:

- waiver owner
- waiver date
- explicit risk accepted
- temporary greybox or fallback evidence

Greybox can support `greybox` or `asset_gate` lifecycle states. It cannot by itself make `scene_complete=true` for release readiness.

## Domain Authority Boundary

Scene evidence may read domain state and present it through world/playable scene anchors. It must not:

- create a new gameplay authority
- duplicate persistent state
- mutate resources, route, repair, market, exploration, feedback, onboarding, save/load, or UI focus state outside the owning domain
- infer gameplay collision, passability, or physical behavior from art alone

The technical contract must name the domain owner for every mutable gameplay consequence.

## Current Gate Snapshot

| Scene ID | Gate status | Blocking reason | Required next evidence |
| --- | --- | --- | --- |
| `initial_island_scene` | `blocked-for-release` | Covered by current notes and historical `hub_island_dock` #20 runtime evidence, but no standalone initial-island scene spec or user release-readiness review yet. | Extract standalone scene spec, clarify runtime/spec ID mapping, attach Codex review, attach user readability verdict. |
| `ship_interior_layered` | `blocked-for-release` | Covered by current notes and historical `hub_ship_interior` runtime evidence, but design now requires horizontal layered ship-interior treatment rather than vertical-only assumptions. | Extract standalone spec, include room state variants, layer/cutaway/behind-object rules, P0 asset gaps, and user readability verdict. |
| `voyage_open_world_scene` | `spec-drafted-blocked-for-evidence` | Current demo requires a combined voyage open-world scene for both destination routes; standalone scene spec exists, but #20 contract, runtime evidence, Codex review, and user readability verdict are still missing. | Review `production/scene-specs/voyage-open-world-scene.md`, then draft #20 contract and evidence plan before implementation readiness or readability review. |
| `mist_lamp_wreck_scene` | `blocked-for-release` | Covered only indirectly by historical `exploration_mist_island` notes and #20 runtime evidence; standalone identity as mist-lamp wreck is not yet extracted. | Extract standalone mist-lamp-wreck spec, attach Codex review, attach user readability verdict. |
| `old_market_edge_scene` | `tracked-gap-current-demo` | Current demo requires old market edge as a destination scene; old market was previously tracked only as a future market gap. | Draft old-market-edge scene spec and #20 contract before visual completion claim. |
| `repair_node_scene` | `tracked-gap-future` | No current enterable scene spec or #20 contract; not part of the corrected current demo scene set unless explicitly added. | Draft repair scene spec before visual completion claim. |

## Story Boundary

This gate defines evidence shape and automated validation. Story 003 owns stronger UI/HUD exclusion checks, and Story 004 owns the user readability/release handoff workflow.
