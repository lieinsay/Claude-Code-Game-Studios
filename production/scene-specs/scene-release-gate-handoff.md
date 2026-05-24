# Scene Release Gate Handoff

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **Last Updated**: 2026-05-24
> **Purpose**: provide the release checklist / gate-check input for scene readiness.

## Handoff Rule

No current scene can be marked release-ready until both automated/Codex evidence and user readability review pass or receive an explicit user waiver.

```text
release_handoff_ready =
    scene_complete
    AND ui_boundary_passed
    AND codex_review_passed
    AND (user_review_passed OR user_waiver_recorded)
    AND no_unresolved_p0_scene_blockers
```

`Codex PASS` is necessary but not sufficient. A user can block a scene for missing fantasy, missing requirements, unclear identity, undesirable player flow, UI dominance, or new demands that must be written back into the scene spec.

## Required Handoff Packet

Each scene release handoff must include:

- scene spec or equivalent source note
- #20 Scene Physics Contract link or explicit no-physical-units exemption
- scene completeness gate result
- scene-vs-UI boundary result
- automated smoke/build command and result
- screenshot or capture evidence for visual claims
- Codex review verdict and blocker list
- user readability checklist verdict and blocker list
- P0 asset gap status
- waiver table, if any
- release decision: `READY`, `READY_WITH_USER_WAIVER`, or `BLOCKED`

## Current Handoff Snapshot

| Scene ID | Codex / automated status | User review status | Release handoff status | Reason |
| --- | --- | --- | --- | --- |
| `initial_island_scene` | Automated evidence exists under historical `hub_island_dock`; standalone initial-island release packet still needed. | `PENDING` | `BLOCKED` | User readability verdict missing; runtime/spec ID mapping must be clarified. |
| `ship_interior_layered` | Automated evidence exists under historical `hub_ship_interior`; standalone horizontal-layered ship interior release packet still needed. | `PENDING` | `BLOCKED` | User readability verdict missing; horizontal layer/cutaway/P0 asset gaps need release packet. |
| `voyage_open_world_scene` | `TRACKED_GAP` promoted from route/UI flow to required current demo scene. | `NOT_READY` | `BLOCKED` | Independent scene design and #20 contract are missing; user will provide design direction. |
| `mist_lamp_wreck_scene` | Automated exploration evidence exists under historical `exploration_mist_island`; standalone mist-lamp-wreck release packet still needed. | `PENDING` | `BLOCKED` | User readability verdict missing; scene identity must be narrowed from generic mist-island to mist-lamp wreck. |
| `old_market_edge_scene` | `TRACKED_GAP` promoted from future market scene to current demo destination. | `NOT_READY` | `BLOCKED` | No standalone scene spec, #20 contract, or user readability verdict yet. |

## Waiver Requirements

A waiver is valid only when the user explicitly records:

- waiver owner
- waiver date
- exact blocker waived
- accepted player-facing risk
- fallback evidence or greybox limitation
- follow-up owner
- follow-up date or next story

Waivers cannot make UI-only evidence count as scene evidence. They only acknowledge risk for a known missing or conditional item.

## Release Checklist Input

Use this summary in release checklist or gate-check:

- Scene Composition #19: `BLOCKED_FOR_RELEASE`
- Reason: current demo scene set has been corrected to initial island, layered ship interior, voyage open world, mist-lamp wreck, and old market edge. User readability verdicts are pending, and voyage/old-market scene specs remain missing.
- Required before release-ready claim: run `production/playtests/scene-composition-user-readability-checklist.md` for each current demo release candidate after standalone scene specs / Codex review are attached, then resolve or explicitly waive blockers.
- Out of scope for this handoff: fixing readability defects, producing final art/audio, or replacing the global release checklist.
