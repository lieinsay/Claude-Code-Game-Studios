# Playtest Checklist: Polish 013 Human Long-Session Release Triage

**Date Created:** 2026-05-23  
**Status:** EXECUTED -- PASS WITH CONDITIONS  
**Scope:** Polish Story 001-012 release-readiness triage  
**Story:** `production/polish-backlog/story-polish-013-human-long-session-release-triage.md`  
**Automated Evidence Target:** `production/qa/evidence/polish-013-release-readiness-automated-preflight-evidence.md`

This checklist decides whether the current MVP playable slice is ready for a
formal release checklist/gate, or whether another narrow Polish story is needed.
It does not establish Release readiness by itself.

## Tester Setup

```text
Engine: Godot 4.6.2 .NET
Platform: Windows desktop
Input: Keyboard and mouse
Entry scene: project default -> src/scenes/SessionShell.tscn
Build/Commit: [fill in git commit]
Tester: liein
Duration: 10 minutes
```

## Required Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene in a normal window | Entry screen appears without errors | [x] | |
| 2 | Start a session and continue past the audio prompt | Hub appears; controls are clear; no stuck loading state | [x] | |
| 3 | Move around Hub for at least 2 minutes | Movement remains responsive; `S` does not trigger Save | [x] | |
| 4 | Visit helm, storage/cargo area, module/engine room, and room boundaries | Spatial layout reads as a playable space, not only a panel backdrop | [ ] | Tester did not find cockpit, storage/cargo, module, or engine-room spaces. Yellow-point area has boundaries, but room identity is not visible enough. |
| 5 | Open Chart from the helm | Chart focus isolates correctly; Hub movement/input does not leak | [ ] | Tester could only open Chart from the helm/航台. This did not block progression, but the expected interaction affordance was not clear enough. |
| 6 | Select the first route and confirm departure | Exploration starts with route/search/return landmarks visible | [x] | |
| 7 | Read Exploration labels before acting | Objective, resource, threat, hull, and return text are understandable | [x] | |
| 8 | Search once, then move for at least 1 minute | Reward feedback appears; movement remains responsive | [x] | |
| 9 | Save during Exploration | Save succeeds; status text is understandable | [x] | |
| 10 | Save again over the same file | First press requests overwrite confirmation; second press confirms | [x] | |
| 11 | Load the saved Exploration state | Screen, route, pressure step, rewards, hull, and feedback restore | [x] | |
| 12 | Return to Hub | Cargo/storage/hull/route summaries reflect Exploration state | [x] | |
| 13 | Close the game completely and relaunch | Session starts cleanly; existing local progress is detected | [x] | |
| 14 | Use Continue/Load after relaunch | Durable progress restores to the expected latest state | [x] | |
| 15 | Repeat route/search/save/load/return for two more cycles | No state drift, input trap, stale hint, or save/load confusion appears | [x] | |
| 16 | Delete local progress from Hub | First press asks for confirmation; second press deletes; Load disables | [x] | |
| 17 | Relaunch after deleting progress | Deleted progress does not reappear; new session remains possible | [x] | |
| 18 | If possible, corrupt or replace the progress file and relaunch | Invalid progress is quarantined or safely rejected; no crash occurs | [x] | |
| 19 | Run one final new route/search/return loop | Core loop remains playable after all persistence edge cases | [x] | |
| 20 | Exit cleanly | No visible errors, hangs, or unrecoverable state remain | [x] | |

## Human Judgment Questions

| Question | Notes |
|----------|-------|
| Is the first objective clear without developer guidance? | Generally clear. |
| Does the Hub read as a place the player can inhabit? | No. Tester could not visually identify the Hub as a station or inhabited space. |
| Does Exploration feel like a playable area rather than a static image? | No. Exploration has no meaningful image/art treatment. |
| Are save/load/overwrite/delete moments trustworthy? | Trustworthy. |
| Does cross-launch Continue/Load feel reliable enough for an MVP release candidate? | Generally reliable. |
| Are any art/audio gaps severe enough to block release triage? | Yes for release-readiness: there is effectively no art treatment. |
| Are onboarding hints useful without stealing attention or input? | Generally useful. |
| Does the current route/search content feel too thin for a release-candidate demo? | Yes. Exploration completes with clicking only; movement is unnecessary and there is little gameplay. |
| What is the single highest-priority improvement before a formal release gate? | Scene and UI are technically normal, but the playable space needs visible place identity and actual movement-driven interaction. |

## Blocker Rules

File or promote a Release blocker if any of these occur:

- The project cannot launch into Hub.
- Keyboard movement fails or becomes trapped.
- `S` saves instead of moving down.
- Chart, Exploration, Save, Load, Overwrite, Delete, or Return input can leave the player stuck.
- Save/load restores the wrong screen, route, pressure step, cargo, or hull state.
- Closing and relaunching loses trusted progress unexpectedly.
- Delete confirmation removes progress without a second confirmation.
- Corrupt progress crashes, silently restores stale state, or prevents starting fresh.
- Critical labels are unreadable, missing, or only communicated by color/audio.
- Presentation is so unclear that a tester cannot complete the loop without this checklist.

Track as non-blocking Polish follow-up if:

- The loop is complete and trusted but feels visually plain.
- Room/interior or island layout needs richer authored art.
- Audio, VFX, or route names could improve tone but are not required for comprehension.
- More routes, named saves, a full save browser, or content scale-up would improve the next milestone.

## Result

**Verdict:** `PASS WITH CONDITIONS`  
**Tester:** `liein`  
**Build/Commit:** `ee28d34`  
**Duration:** `10 minutes`

**Release Triage Decision:**

- [ ] Proceed to formal release checklist/gate.
- [x] Open one blocking Polish story before release checklist.
- [ ] Continue ordinary non-blocking Polish backlog.

**Top Findings:**

1. Core stability and persistence trust passed: launch, save/load, cross-launch restore, overwrite, delete, quarantine, and final loop all completed.
2. Release readiness should not proceed yet because Hub and Exploration do not read as authored playable spaces; tester could not identify Hub rooms and Exploration has no meaningful image/art treatment.
3. Current route/search content is mechanically too thin for a release-candidate demo: completion is mostly clicking through UI, with movement not required for play.
