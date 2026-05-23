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
| `hub_island_dock` | `blocked-for-release` | Covered by current notes and #20 runtime evidence, but no standalone scene spec or user release-readiness review yet. | Extract standalone scene spec, attach Codex review, attach user readability verdict. |
| `hub_ship_interior` | `blocked-for-release` | Covered by current notes and #20 runtime evidence, but no standalone scene spec or user release-readiness review yet. | Extract standalone scene spec, include room state variants and P0 asset gaps, attach user readability verdict. |
| `chart_table_scene` | `blocked-pending-classification` | Authored chart table exists as a ship-interior anchored surface, but Story 002/003 must classify whether it needs its own #20 contract or remains anchored UI-assisted scene surface. | Add explicit classification and UI-vs-scene proof. |
| `exploration_mist_island` | `blocked-for-release` | Covered by current notes and #20 runtime evidence, but no standalone scene spec or user release-readiness review yet. | Extract standalone scene spec, attach Codex review, attach user readability verdict. |
| `repair_node_scene` | `tracked-gap` | No current enterable scene spec or #20 contract. | Draft repair scene spec before visual completion claim. |
| `market_scene` | `tracked-gap` | No current enterable scene spec or #20 contract. | Draft market scene spec before visual completion claim. |

## Story Boundary

This gate defines evidence shape and automated validation. Story 003 owns stronger UI/HUD exclusion checks, and Story 004 owns the user readability/release handoff workflow.
