# Playtest Checklist: Sprint 002 Playable Vertical Slice Recovery

**Date Created:** 2026-05-17
**Status:** EXECUTED — PASS
**Sprint:** `production/sprints/sprint-002-playable-vertical-slice-recovery.md`
**Purpose:** Confirm that the recovered vertical slice is playable by a human,
not only by scripted smoke probes or headless C# runners.

## Tester Setup

```text
Engine: Godot 4.6.2 .NET
Platform: Windows desktop
Input: Keyboard and mouse
Entry scene: project default -> src/scenes/SessionShell.tscn
```

Use the current workspace build unless a specific commit/build id is supplied.

## Required Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project | Entry screen appears | [x] | Passed in manual test. |
| 2 | Start a new session and continue past audio prompt | Hub scene appears | [x] | Passed in manual test. |
| 3 | Move with WASD or arrow keys | Player marker visibly moves | [x] | Passed after save shortcut conflict fix. |
| 4 | Move near the helm point | Prompt says E can use the helm | [x] | Passed in manual test. |
| 5 | Press E near helm | Chart panel opens | [x] | Passed in manual test. |
| 6 | Select `雾海短程` | Chart status shows the route selection | [x] | Passed in manual test. |
| 7 | Confirm departure | Exploration HUD appears | [x] | Passed in manual test. |
| 8 | Move near the search point | Prompt says E can search the event point | [x] | Passed in manual test. |
| 9 | Press E near search point | Resource/threat/HUD feedback changes visibly | [x] | Passed in manual test. |
| 10 | Save while in Exploration | Save status reports success | [x] | Passed using `Ctrl+S` / button after conflict fix. |
| 11 | Move near the return point | Prompt says E can return to Hub | [x] | Passed in manual test. |
| 12 | Press E near return point | Hub appears and summary reflects Exploration state | [x] | Passed in manual test. |
| 13 | Load the save | Exploration state restores with selected route/progress | [x] | Passed using `Ctrl+L` / button. |

## Failure Rules

File an S1/S2 bug if any of these happen:

- The project cannot launch into Hub.
- The player marker cannot move.
- No prompt appears near a required interaction point.
- Pressing E near helm, search, or return does nothing.
- Route departure does not enter Exploration.
- Search gives no visible feedback.
- Return to Hub loses all Exploration state.
- Save/load fails or restores the wrong screen/progress.

File a design/UX note if:

- The tester completes the route but does not understand what changed.
- The tester can complete the route only after reading this checklist.
- The placeholder markers are legible but feel too abstract for a future gate.

## Result

**Verdict:** PASS

**Tester:** User manual test report, 2026-05-17

**Build/Commit:** Current workspace state, Sprint 002 playable recovery branch/worktree

**Top Findings:**

1. Initial WASD/save shortcut conflict found: `S` triggered save while moving down.
2. Conflict fixed by moving save/load shortcuts to `Ctrl+S` / `Ctrl+L`.
3. User reported other manual tests had no problems after the shortcut fix.

**Gate Impact:**

This file can now be used as Sprint 002 manual playable evidence. It proves the
recovered greybox loop is human-playable, while final Production to Polish still
requires a fresh gate check that weighs remaining greybox/domain-integration
risks.
