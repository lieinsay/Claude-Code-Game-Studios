# Playtest Checklist: Polish 015 Island / Ship Interior and Search Gameplay

**Date Created:** 2026-05-23  
**Status:** EXECUTED - BLOCKED
**Scope:** Focused rerun for Polish Story 015 release-readiness blocker  
**Story:** `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`  
**Automated Evidence Target:** `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`

This checklist retests the blockers from Polish 014. It does not establish
Release readiness by itself.

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
| 2 | Start a session and enter Hub | The first Hub scene reads as an island/dock with a visible ship | [ ] | |
| 3 | Move to the boarding ramp and press `E` | Player enters a separate ship interior | [ ] | |
| 4 | Move through the ship interior | Cockpit/helm, cargo/storage, and engine/module spaces are distinct through layout and props | [ ] | |
| 5 | Try opening Chart from the island exterior if possible | Chart does not open until the player is inside at the helm | [ ] | |
| 6 | Open Chart from the ship helm and depart | Chart focus isolates correctly; route departure starts Exploration | [ ] | |
| 7 | View the Exploration scene before searching | The area has enough authored complexity to read as a place | [ ] | |
| 8 | Move to the search wreck and press `E` once | Search begins a scan/calibration step instead of instantly resolving | [ ] | |
| 9 | Press `E` again at the search wreck | Search advances to an echo/lock step without instant teleport or confusion | [ ] | |
| 10 | Press `E` a third time at the search wreck | Salvage resolves and resource/threat/cargo/hull feedback appears | [ ] | |
| 11 | Move to the docked ship return point and press `E` once | Return begins engine preheat and keeps the player in Exploration | [ ] | |
| 12 | Press `E` again at the ship return point | Player pilots/returns to the island Hub exterior; summaries update | [ ] | |
| 13 | Save and load once during Exploration if time allows | Expanded interaction state does not break persistence trust | [ ] | |
| 14 | Exit cleanly | No visible errors, hangs, or unrecoverable state remain | [ ] | |

## Human Judgment Questions

| Question | Notes |
|----------|-------|
| Does Hub now read as an island/dock with a visible ship? | |
| Does entering the ship interior feel clear? | |
| Can you identify cockpit/helm, cargo/storage, and engine/module spaces without developer guidance? | |
| Does Exploration now feel more like an authored place? | |
| Does the three-step search feel like a real gameplay beat, even if small? | |
| Does return read more like ship movement or piloting than touching a beacon? | |
| Are onboarding and disabled affordances still understandable? | |
| Are the greybox visuals and micro-interactions enough to unblock a formal release-readiness rerun? | |
| What is the single highest-priority improvement before a formal release gate? | |

## Result

**Verdict:** `BLOCKED`
**Tester:** `liein`
**Build/Commit:** `80cf299`
**Duration:** `focused early-route pass`

**Release Triage Decision:**

- [ ] Proceed to formal release checklist/gate.
- [x] Open another blocking Polish story before release checklist.
- [ ] Continue ordinary non-blocking Polish backlog.

**Top Findings:**

1. Step 1 passed: the project launches normally in a window and reaches the entry flow.
2. Step 2 failed: Hub still does not read as an island/dock; tester did not discover a real island scene.
3. Steps 3-4 failed: boarding/ship interior is perceived as text-only, with no readable cockpit/helm, cargo/storage, or engine/module spaces.
4. Step 5 failed: no island was discovered, so Chart gating cannot be meaningfully evaluated.
5. All later route/search/return checks are blocked because the visible scene identity is still missing.

## Follow-up Fix Note

The first implementation pass created scene nodes that automated smoke could
find, but the live layout still presented a text dashboard as the dominant
surface. The follow-up fix must make the island, docked ship, boarding path,
ship interior zones, Exploration island, search wreck, and return ship occupy
the main viewport before release-readiness can be retested.
