# Playtest Checklist: Polish 014 Readable Space and Movement-Driven Interaction

**Date Created:** 2026-05-23  
**Status:** READY FOR HUMAN QA  
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
Build/Commit: [fill in git commit]
Tester: [fill in tester name]
Duration: [fill in duration]
```

## Focused Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene in a normal window | Entry screen appears without errors | [ ] | |
| 2 | Start a session and enter Hub | Hub reads as a station/interior, not only a UI panel | [ ] | |
| 3 | Move around Hub for at least 2 minutes | Movement remains responsive; authored room areas stay visible | [ ] | |
| 4 | Visit cockpit/helm, cargo/storage, and engine/module areas | Each area is visually distinct without relying only on tiny labels | [ ] | |
| 5 | Open Chart from the helm and close it | Chart focus isolates correctly; returning to Hub is clear | [ ] | |
| 6 | Select the first route and confirm departure | Exploration starts with visible island/search/return landmarks | [ ] | |
| 7 | Try to search before walking to the search wreck | Search does not complete; UI or footer explains that the player must move closer | [ ] | |
| 8 | Move to the search wreck and press `E` or the enabled search action | Search completes and reward/pressure feedback appears | [ ] | |
| 9 | Try to return before walking to the return beacon | Return does not complete; UI or footer explains that the player must move closer | [ ] | |
| 10 | Move to the return beacon and press `E` or the enabled return action | Player returns to Hub; cargo/storage/hull summaries update | [ ] | |
| 11 | Save and load once during Exploration if time allows | Movement-gated Exploration still restores correctly | [ ] | |
| 12 | Exit cleanly | No visible errors, hangs, or unrecoverable state remain | [ ] | |

## Human Judgment Questions

| Question | Notes |
|----------|-------|
| Does the Hub now read as a station or inhabited ship interior? | |
| Can you identify cockpit/helm, cargo/storage, and engine/module areas without developer guidance? | |
| Does Exploration now feel like a playable area instead of an empty/static image? | |
| Does the search loop now require meaningful movement? | |
| Does the return loop now require meaningful movement? | |
| Are disabled search/return affordances understandable rather than confusing? | |
| Are the new greybox visuals enough for a release-readiness rerun, even if final art is still missing? | |
| What is the single highest-priority improvement before a formal release gate? | |

## Result

**Verdict:** `[PASS / PASS WITH CONDITIONS / CONCERN / BLOCKED]`  
**Tester:** `[fill in]`  
**Build/Commit:** `[fill in]`  
**Duration:** `[fill in]`

**Release Triage Decision:**

- [ ] Proceed to formal release checklist/gate.
- [ ] Open another blocking Polish story before release checklist.
- [ ] Continue ordinary non-blocking Polish backlog.

**Top Findings:**

1. `[fill in]`
2. `[fill in]`
3. `[fill in]`
