# Playtest Checklist: Sprint 003 Domain-Backed Playable Slice

**Date Created:** 2026-05-17
**Status:** READY FOR HUMAN EXECUTION
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
Build/Commit: current workspace build, record exact commit before execution
```

## Required Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene | Entry screen appears without errors | [ ] | |
| 2 | Start a session and continue past the audio prompt | Hub scene appears with authored greybox deck/helm/storage/module landmarks | [ ] | |
| 3 | Move with WASD and arrow keys | Player marker visibly moves; pressing `S` moves down and does not save | [ ] | |
| 4 | Move near the helm console | Prompt indicates `E` can open the chart | [ ] | |
| 5 | Press `E` near the helm | Chart panel opens; Hub action buttons are disabled while Chart is open | [ ] | |
| 6 | Select `雾海短程` / `route.mist` | Route selection visibly updates from the Chart surface | [ ] | |
| 7 | Confirm departure | Exploration surface appears with sky/trail/search/return landmarks | [ ] | |
| 8 | Move near the search wreck | Prompt indicates `E` can search the point | [ ] | |
| 9 | Press `E` once at the search point | Resource pressure and low-threat feedback change visibly | [ ] | |
| 10 | Press `E` again at the search point if prompted/available | Carried reward/hull/threat feedback advances visibly | [ ] | |
| 11 | Save in Exploration with `Ctrl+S` or the Save control | Save feedback reports success without breaking movement controls | [ ] | |
| 12 | Move near the return beacon | Prompt indicates `E` can return to Hub | [ ] | |
| 13 | Press `E` near the return beacon | Hub appears; cargo/storage/hull/route summaries reflect exploration state | [ ] | |
| 14 | Load the saved Exploration state with `Ctrl+L` or the Load control | Exploration screen, route, pressure, carried rewards, and load feedback restore | [ ] | |
| 15 | Continue searching after load | Route pressure loop can continue without errors | [ ] | |

## Domain-Backed Evidence Observations

Record tester-visible proof that the route no longer reads as smoke-only:

- [ ] Chart selection/departure appears coherent and route-specific.
- [ ] Search changes resource/cargo feedback.
- [ ] Search changes threat or hull feedback.
- [ ] Return-to-Hub summaries reflect exploration results.
- [ ] Save/load restores the canonical route state rather than only resetting UI text.
- [ ] Hub and Exploration read as spatial greybox surfaces, not only plain panels.

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

**Verdict:** PENDING HUMAN PLAYTEST

**Tester:** TBD

**Build/Commit:** TBD

**Duration:** TBD

**Top Findings:**

1. TBD
2. TBD
3. TBD

**Gate Impact:**

This checklist must be executed before Sprint 003 can close PVS3-007. A PASS or
APPROVED WITH CONDITIONS result can support the next Production -> Polish gate
recheck; this file alone is not a gate PASS until a human tester fills the
result section.

