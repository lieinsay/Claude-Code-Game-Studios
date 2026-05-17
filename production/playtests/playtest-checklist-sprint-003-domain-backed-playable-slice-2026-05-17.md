# Playtest Checklist: Sprint 003 Domain-Backed Playable Slice

**Date Created:** 2026-05-17
**Status:** EXECUTED -- PASS
**Sprint:** `production/sprints/sprint-003-domain-backed-playable-slice.md`
**QA Plan:** `production/qa/qa-plan-sprint-003-domain-backed-playable-slice-2026-05-17.md`
**Automated Evidence:** `production/qa/evidence/sprint-003-domain-backed-playable-smoke-evidence-2026-05-17.md`
**Purpose:** Confirm that the C# domain-backed greybox route is playable by a
human, not only by automated smoke probes or debug snapshots.

## Tester Setup

```text
Engine: Godot 4.6.2 .NET
Platform: Windows desktop
Input: Keyboard and mouse
Entry scene: project default -> src/scenes/SessionShell.tscn
Build/Commit: 0437df3
```

## Required Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene | Entry screen appears without errors | [x] | User manual report: no problems. |
| 2 | Start a session and continue past the audio prompt | Hub scene appears with authored greybox deck/helm/storage/module landmarks | [x] | User manual report: no problems. |
| 3 | Move with WASD and arrow keys | Player marker visibly moves; pressing `S` moves down and does not save | [x] | User manual report: no problems. |
| 4 | Move near the helm console | Prompt indicates `E` can open the chart | [x] | User manual report: no problems. |
| 5 | Press `E` near the helm | Chart panel opens; Hub action buttons are disabled while Chart is open | [x] | User manual report: no problems. |
| 6 | Select `雾海短程` / `route.mist` | Route selection visibly updates from the Chart surface | [x] | User manual report: no problems. |
| 7 | Confirm departure | Exploration surface appears with sky/trail/search/return landmarks | [x] | User manual report: no problems. |
| 8 | Move near the search wreck | Prompt indicates `E` can search the point | [x] | User manual report: no problems. |
| 9 | Press `E` once at the search point | Resource pressure and low-threat feedback change visibly | [x] | User manual report: no problems. |
| 10 | Press `E` again at the search point if prompted/available | Carried reward/hull/threat feedback advances visibly | [x] | User manual report: no problems. |
| 11 | Save in Exploration with `Ctrl+S` or the Save control | Save feedback reports success without breaking movement controls | [x] | User manual report: no problems. |
| 12 | Move near the return beacon | Prompt indicates `E` can return to Hub | [x] | User manual report: no problems. |
| 13 | Press `E` near the return beacon | Hub appears; cargo/storage/hull/route summaries reflect exploration state | [x] | User manual report: no problems. |
| 14 | Load the saved Exploration state with `Ctrl+L` or the Load control | Exploration screen, route, pressure, carried rewards, and load feedback restore | [x] | User manual report: no problems. |
| 15 | Continue searching after load | Route pressure loop can continue without errors | [x] | User manual report: no problems. |

## Domain-Backed Evidence Observations

Record tester-visible proof that the route no longer reads as smoke-only:

- [x] Chart selection/departure appears coherent and route-specific.
- [x] Search changes resource/cargo feedback.
- [x] Search changes threat or hull feedback.
- [x] Return-to-Hub summaries reflect exploration results.
- [x] Save/load restores the canonical route state rather than only resetting UI text.
- [x] Hub and Exploration read as spatial greybox surfaces, not only plain panels.

## Failure Rules

File an S1/S2 bug if any of these happen:

- The project cannot launch into the Hub.
- The player marker cannot move with keyboard input.
- `S` triggers save instead of movement.
- No prompt appears near helm, search wreck, or return beacon.
- Pressing `E` near a required interaction point does nothing.
- Route departure does not enter Exploration.
- Search gives no visible resource/threat/hull feedback.
- Return to Hub loses all exploration state.
- Save/load fails or restores the wrong screen/progress/state.

File a design/UX note if:

- The route is technically completable but the tester cannot understand the
  objective without this checklist.
- Greybox landmarks are visible but too abstract to support Production -> Polish
  gate evidence.
- Feedback is present but not legible or not connected to player action.

## Result

**Verdict:** PASS

**Tester:** User manual test report, 2026-05-17

**Build/Commit:** `0437df3`

**Duration:** Not recorded

**Top Findings:**

1. No blocking launch, movement, prompt, E-use, departure, search, return, or save/load issues reported.
2. Domain-backed greybox route is human-playable after PVS3-006 automated evidence.
3. No additional manual UX defects reported in this pass.

**Gate Impact:**

This checklist is executed with PASS and closes PVS3-007. It supports the
Production -> Polish gate recheck that passed with conditions on 2026-05-17.
