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
| `hub_island_dock` | Automated world/physics smoke evidence exists; standalone scene spec and Codex release review still needed. | `PENDING` | `BLOCKED` | User readability verdict missing; standalone release packet incomplete. |
| `hub_ship_interior` | Automated world/physics smoke evidence exists; standalone scene spec and Codex release review still needed. | `PENDING` | `BLOCKED` | User readability verdict missing; room/state/P0 asset gaps need release packet. |
| `chart_table_scene` | Classified as UI-assisted world surface anchored inside `hub_ship_interior`; route buttons cannot count as scene proof. | `PENDING_SCOPE_DECISION` | `BLOCKED` | User must decide whether current surface is acceptable within ship-interior handoff or requires standalone scene work. |
| `exploration_mist_island` | Automated world/physics smoke evidence exists; standalone scene spec and Codex release review still needed. | `PENDING` | `BLOCKED` | User readability verdict missing; release packet incomplete. |
| `repair_node_scene` | `TRACKED_GAP` | `NOT_READY` | `BLOCKED` | No scene spec or #20 contract yet. |
| `market_scene` | `TRACKED_GAP` | `NOT_READY` | `BLOCKED` | No scene spec or #20 contract yet. |

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
- Reason: current scenes have automated/Codex evidence foundations, but user readability verdicts are pending and repair/market remain tracked gaps.
- Required before release-ready claim: run `production/playtests/scene-composition-user-readability-checklist.md` for each current release candidate, attach standalone scene specs / Codex review, and resolve or explicitly waive blockers.
- Out of scope for this handoff: fixing readability defects, producing final art/audio, or replacing the global release checklist.
