# Scene Composition User Readability Checklist

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-004-user-readability-release-gate.md`
> **Last Updated**: 2026-05-24
> **Purpose**: make subjective scene readability review concrete before any release-readiness handoff.

## Use Rule

Run this checklist after automated scene evidence and Codex review have passed for a scene package. A user review can still block a scene even when Codex review passes.

Allowed verdicts:

- `PASS`
- `PASS_WITH_CONDITIONS`
- `BLOCKED`
- `WAIVED_BY_USER`

`BLOCKED` prevents release gate handoff until the blocker is resolved or explicitly waived by the user. `WAIVED_BY_USER` must name the user, date, accepted risk, fallback evidence, and follow-up owner.

## Required Context

| Field | Required value |
| --- | --- |
| Scene ID |  |
| Player-facing scene name |  |
| Build or commit tested |  |
| Runtime path tested |  |
| Automated evidence links |  |
| Codex review verdict | PASS / PASS_WITH_CONDITIONS / BLOCKED |
| User reviewer |  |
| Review date |  |
| Screenshot / capture path, if visual claim is made |  |

## Readability Questions

The reviewer should answer without developer explanation.

| Question | Required answer | Verdict | Notes / blocker |
| --- | --- | --- | --- |
| Where am I? | Location identity and mood are readable within about 3 seconds. | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| What can I do here? | The available core action is readable from world/playable anchors, not only UI. | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| How do I leave or continue? | Exit, return, or continuation path is visible or discoverable through scene anchors. | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| What changed? | The relevant state change is visible in the world/playable scene or clearly supported by feedback. | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| Does UI/HUD support rather than dominate? | `hud_not_dominant = true`; UI does not hide or replace world identity. | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |
| Does the scene match the intended fantasy? | Missing fantasy, missing requirement, undesirable flow, or new demand is recorded. | PASS / PASS_WITH_CONDITIONS / BLOCKED |  |

## Blocker And Waiver Log

| Scene ID | Reviewer verdict | Blocker / condition | Waiver owner | Waiver date | Accepted risk | Fallback evidence | Follow-up owner | Follow-up date / next story |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
|  | PASS / PASS_WITH_CONDITIONS / BLOCKED / WAIVED_BY_USER |  |  |  |  |  |  |  |

## Release Handoff Decision

The scene may enter release handoff only when:

- Codex review is not `BLOCKED`
- user review is `PASS`, `PASS_WITH_CONDITIONS`, or `WAIVED_BY_USER`
- all `PASS_WITH_CONDITIONS` items have owners and follow-up dates
- every `BLOCKED` item is resolved or explicitly waived by the user
- no P0 scene asset gap remains unresolved without waiver
- no UI-only evidence is used as scene proof

## Current Scene Review Queue

| Scene ID | Current release-readiness status | Required user action |
| --- | --- | --- |
| `hub_island_dock` | `BLOCKED_PENDING_USER_REVIEW` | Run checklist after standalone scene spec / Codex review are attached. |
| `hub_ship_interior` | `BLOCKED_PENDING_USER_REVIEW` | Run checklist after standalone scene spec / Codex review are attached. |
| `chart_table_scene` | `BLOCKED_PENDING_USER_REVIEW_OR_SCOPE_DECISION` | User must decide whether current UI-assisted surface is acceptable inside ship-interior handoff or needs a standalone scene spec. |
| `exploration_mist_island` | `BLOCKED_PENDING_USER_REVIEW` | Run checklist after standalone scene spec / Codex review are attached. |
| `repair_node_scene` | `BLOCKED_TRACKED_GAP` | Draft scene spec and #20 contract before user release review. |
| `market_scene` | `BLOCKED_TRACKED_GAP` | Draft scene spec and #20 contract before user release review. |
