# Playtest Checklist: Polish 014 Readable Space and Movement-Driven Interaction

**Date Created:** 2026-05-23
**Status:** EXECUTED -- CONCERN
**Scope:** Focused rerun for Polish Story 014 release-readiness blocker
**Story:** `production/polish-backlog/story-polish-014-playable-space-readability-and-movement-driven-interaction.md`
**Automated Evidence Target:** `production/qa/evidence/polish-014-playable-space-readability-and-movement-driven-interaction-evidence.md`

This checklist only retests the blockers found in Polish 013. It does not
establish Release readiness by itself.

## Tester Setup

```text
Engine: Godot 4.6.2 .NET
Platform: Windows desktop
Input: Keyboard and mouse
Entry scene: project default -> src/scenes/SessionShell.tscn
Build/Commit: ecb1fe6
Tester: liein
Duration: 10 minutes
```

## Focused Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene in a normal window | Entry screen appears without errors | [x] | |
| 2 | Start a session and enter Hub | Hub reads as a station/interior, not only a UI panel | [x] | Launch/start path works, but later human judgment says the place identity still fails. |
| 3 | Move around Hub for at least 2 minutes | Movement remains responsive; authored room areas stay visible | [ ] | Movement works, but tester did not see rooms that felt created/authored. |
| 4 | Visit cockpit/helm, cargo/storage, and engine/module areas | Each area is visually distinct without relying only on tiny labels | [ ] | Tester did not see distinct areas; only helm and storage color blocks were apparent. |
| 5 | Open Chart from the helm and close it | Chart focus isolates correctly; returning to Hub is clear | [x] | |
| 6 | Select the first route and confirm departure | Exploration starts with visible island/search/return landmarks | [x] | |
| 7 | Try to search before walking to the search wreck | Search does not complete; UI or footer explains that the player must move closer | [x] | |
| 8 | Move to the search wreck and press `E` or the enabled search action | Search completes and reward/pressure feedback appears | [x] | |
| 9 | Try to return before walking to the return beacon | Return does not complete; UI or footer explains that the player must move closer | [x] | |
| 10 | Move to the return beacon and press `E` or the enabled return action | Player returns to Hub; cargo/storage/hull summaries update | [x] | |
| 11 | Save and load once during Exploration if time allows | Movement-gated Exploration still restores correctly | [x] | |
| 12 | Exit cleanly | No visible errors, hangs, or unrecoverable state remain | [x] | |

## Human Judgment Questions

| Question | Notes |
|----------|-------|
| Does the Hub now read as a station or inhabited ship interior? | No. It does not read as the intended place. Correct direction should be an island scene where a ship can dock, with the player able to enter the ship interior. |
| Can you identify cockpit/helm, cargo/storage, and engine/module areas without developer guidance? | No. Tester could not identify the areas; the scene reads as only helm and storage color blocks. |
| Does Exploration now feel like a playable area instead of an empty/static image? | No. Searching is still only movement to a point, and the search itself needs to become gameplay. |
| Does the search loop now require meaningful movement? | Some movement is meaningful now, but the concrete search gameplay still needs design. |
| Does the return loop now require meaningful movement? | The intended return should involve piloting or moving the ship back, not only touching a return beacon. |
| Are disabled search/return affordances understandable rather than confusing? | No issue reported. |
| Are the new greybox visuals enough for a release-readiness rerun, even if final art is still missing? | No. The current treatment does not satisfy the release-readiness blocker. |
| What is the single highest-priority improvement before a formal release gate? | Add enough scene complexity and gameplay structure: island/docked ship context, explorable ship interior, designed search gameplay, and more intentional return flow. |

## Result

**Verdict:** `CONCERN`
**Tester:** `liein`
**Build/Commit:** `ecb1fe6`
**Duration:** `10 minutes`

**Release Triage Decision:**

- [ ] Proceed to formal release checklist/gate.
- [x] Open another blocking Polish story before release checklist.
- [ ] Continue ordinary non-blocking Polish backlog.

**Top Findings:**

1. Hub place identity still fails: it does not read as an island dock with a ship interior, and tester cannot identify the intended interior areas.
2. Exploration still lacks designed search gameplay: movement gating exists, but search remains a simple move-to-point action instead of a playable activity.
3. Return flow needs design: intended behavior is piloting or moving the ship back, not only walking to a return beacon.

## Action Routing

- **Design changes needed:** Required. The slice needs a revised spatial model and gameplay concept before more implementation polish: island/docked ship context, enterable ship interior, authored room topology, search gameplay, and ship-return interaction.
- **Balance adjustments:** None identified.
- **Bug reports:** None identified; steps 1-2 and 5-12 passed.
- **Polish items:** Final art/audio remains downstream, but the current issue is not merely polish because the intended playable fantasy and scene structure are not yet communicated.
