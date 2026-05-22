# Playtest Checklist: Polish Route/Search Loop Human QA

**Date Created:** 2026-05-22  
**Status:** READY FOR HUMAN EXECUTION  
**Scope:** Polish Story 001-005 route/search runtime, authored content, validation guard, and ID migration guard  
**Automated Evidence:**  
- `production/qa/evidence/polish-001-navigation-exploration-runtime-hardening-evidence.md`
- `production/qa/evidence/polish-002-richer-exploration-scene-semantics-evidence.md`
- `production/qa/evidence/polish-003-authored-route-search-content-evidence.md`
- `production/qa/evidence/polish-004-authored-content-validation-guard-evidence.md`
- `production/qa/evidence/polish-005-route-search-id-migration-guard-evidence.md`

**Purpose:** Decide whether the current authored route/search loop feels clear,
coherent, and worth expanding, or whether content pacing/name/feedback changes
are needed before the next Polish story.

This checklist does **not** establish Release readiness.

## Tester Setup

```text
Engine: Godot 4.6.2 .NET
Platform: Windows desktop
Input: Keyboard and mouse
Entry scene: project default -> src/scenes/SessionShell.tscn
Build/Commit: fill in before test
Tester: fill in before test
Duration: 10-15 minutes minimum
```

## Required Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene | Entry screen appears without errors | [ ] | |
| 2 | Start a session and continue past the audio prompt | Hub appears with greybox deck/helm/storage/module landmarks | [ ] | |
| 3 | Move with WASD and arrow keys for at least 20 seconds | Player marker moves predictably; `S` moves down and does not save | [ ] | |
| 4 | Read the active onboarding hint without clicking it | Hint is visible, non-blocking, and does not steal input | [ ] | |
| 5 | Move near the helm console | Prompt indicates `E` can open Chart | [ ] | |
| 6 | Press `E` near the helm | Chart opens; Hub controls are disabled while Chart is open | [ ] | |
| 7 | Select `雾海短程` | Route name, route selection, and departure intent are clear | [ ] | |
| 8 | Confirm departure | Exploration surface appears with route/search/return landmarks | [ ] | |
| 9 | Before searching, read the Exploration labels | Route, search point, extraction, resource, threat, and hull text are understandable | [ ] | |
| 10 | Move near the search point and press `E` once | First search records `雾灯残骸`; resource/cargo feedback changes visibly | [ ] | |
| 11 | Search a second time | Threat/hull/cargo pressure escalates and still feels understandable | [ ] | |
| 12 | Save in Exploration with `Ctrl+S` or the Save control | Save succeeds and movement remains responsive | [ ] | |
| 13 | Return to Hub through the return beacon | Hub appears; cargo/storage/hull/route summaries reflect exploration state | [ ] | |
| 14 | Load the saved Exploration state with `Ctrl+L` or Load control | Exploration restores route display, pressure step, carried rewards, and load feedback | [ ] | |
| 15 | Continue searching after load until the pressure loop completes | Search marker/extraction text reach a clear completed or settlement-ready state | [ ] | |
| 16 | Return to Hub again | Final Hub summaries remain coherent and no stale onboarding hint appears | [ ] | |

## Human Judgment Questions

Answer in short notes. These are intentionally subjective.

| Question | Notes |
|----------|-------|
| Is the first route objective clear without reading this checklist? | |
| Do `雾海短程`, `雾灯残骸`, `剪云裂隙`, and `返航浮标箱` fit the intended tone? | |
| Does the first search reward feel satisfying enough? | |
| Does threat/hull pressure arrive too early, too late, or about right? | |
| Is the save/load moment understandable and trustworthy? | |
| Do the greybox landmarks support navigation, or do they need visual/audio treatment before content expansion? | |
| Would you rather expand content next, tune pacing next, or improve presentation next? | |

## Failure Rules

File an S1/S2 bug if any of these happen:

- Project cannot launch into Hub.
- Player marker cannot move with keyboard input.
- `S` triggers save instead of movement.
- No prompt appears near helm, search point, or return beacon.
- Pressing `E` near a required interaction point does nothing.
- Route departure does not enter Exploration.
- Search gives no visible resource/threat/hull feedback.
- Save/load fails or restores the wrong screen/progress/state.
- Return to Hub loses all exploration state.

File a Polish design/UX note if:

- The loop is completable but the objective is unclear.
- The authored names feel off-tone, placeholder-like, or confusing.
- The route/search content feels too thin to expand from.
- Pressure changes are legible but not fun.
- Greybox visuals are understandable but too abstract for the next presentation pass.

## Result

**Verdict:** Pending human execution

Choose one after testing:

- `PASS`: Loop is clear enough to expand content.
- `PASS WITH CONDITIONS`: Loop works, but pacing/text/presentation needs targeted polish.
- `CONCERN`: No blocker, but the next Polish story should address major feel/readability issues.
- `BLOCKED`: A required route step failed or the loop cannot be manually completed.

**Tester:**  
**Build/Commit:**  
**Duration:**  

**Top Findings:**

1. 
2. 
3. 

**Recommended Next Polish Story:**

-
