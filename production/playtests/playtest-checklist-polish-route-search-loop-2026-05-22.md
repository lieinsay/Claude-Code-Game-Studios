# Playtest Checklist: Polish Route/Search Loop Human QA

**Date Created:** 2026-05-22  
**Status:** EXECUTED -- PASS WITH CONDITIONS
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
Build/Commit: d755e80
Tester: liein
Duration: 10 minutes
```

## Required Route

| Step | Action | Expected Result | Pass/Fail | Notes |
|------|--------|-----------------|-----------|-------|
| 1 | Launch the project from the default scene | Entry screen appears without errors | [x] | |
| 2 | Start a session and continue past the audio prompt | Hub appears with greybox deck/helm/storage/module landmarks | [x] | |
| 3 | Move with WASD and arrow keys for at least 20 seconds | Player marker moves predictably; `S` moves down and does not save | [x] | |
| 4 | Read the active onboarding hint without clicking it | Hint is visible, non-blocking, and does not steal input | [x] | |
| 5 | Move near the helm console | Prompt indicates `E` can open Chart | [x] | |
| 6 | Press `E` near the helm | Chart opens; Hub controls are disabled while Chart is open | [x] | |
| 7 | Select `雾海短程` | Route name, route selection, and departure intent are clear | [x] | |
| 8 | Confirm departure | Exploration surface appears with route/search/return landmarks | [x] | |
| 9 | Before searching, read the Exploration labels | Route, search point, extraction, resource, threat, and hull text are understandable | [x] | |
| 10 | Move near the search point and press `E` once | First search records `雾灯残骸`; resource/cargo feedback changes visibly | [x] | |
| 11 | Search a second time | Threat/hull/cargo pressure escalates and still feels understandable | [x] | |
| 12 | Save in Exploration with `Ctrl+S` or the Save control | Save succeeds and movement remains responsive | [x] | |
| 13 | Return to Hub through the return beacon | Hub appears; cargo/storage/hull/route summaries reflect exploration state | [x] | |
| 14 | Load the saved Exploration state with `Ctrl+L` or Load control | Exploration restores route display, pressure step, carried rewards, and load feedback | [x] | |
| 15 | Continue searching after load until the pressure loop completes | Search marker/extraction text reach a clear completed or settlement-ready state | [x] | |
| 16 | Return to Hub again | Final Hub summaries remain coherent and no stale onboarding hint appears | [x] | |

## Human Judgment Questions

Answer in short notes. These are intentionally subjective.

| Question | Notes |
|----------|-------|
| Is the first route objective clear without reading this checklist? | 一般明确 |
| Do `雾海短程`, `雾灯残骸`, `剪云裂隙`, and `返航浮标箱` fit the intended tone? | 一般符合 |
| Does the first search reward feel satisfying enough? | 一般满意 |
| Does threat/hull pressure arrive too early, too late, or about right? | 暂时无所谓 |
| Is the save/load moment understandable and trustworthy? | 没有持久化的保存文件，关闭游戏下一次无法恢复 |
| Do the greybox landmarks support navigation, or do they need visual/audio treatment before content expansion? | 过于抽象而且地图过于小，只是一个没有任何效果的图片 |
| Would you rather expand content next, tune pacing next, or improve presentation next? | 当务之急还是要让场景更合理，比如能在岛上行走，岛有边界，能上船，船上有各种房间，都能移动 |

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

**Verdict:** `PASS WITH CONDITIONS`
**Tester:**  liein
**Build/Commit:** d755e80
**Duration:** 10 minutes

**Conditions:**

- Current Hub -> Chart -> Exploration -> Search -> Save -> Return -> Load loop is manually completable.
- Cross-launch persistence is not trusted yet: tester reports no persistent save file after closing the game, so the next pass should verify quit/relaunch continue behavior before treating save/load as complete player-facing persistence.
- Presentation should be prioritized before content expansion: tester reports UI and scene are mixed together, and the scene reads like a static image rather than a usable space.
- Do not use this PASS WITH CONDITIONS as Release readiness.

**Top Findings:**

1. UI and scene are mixed together, which makes the player feel like they are operating panels over a background rather than moving through a game space.
2. The current scene reads like a static image with little gameplay function; it needs walkable boundaries, meaningful spatial layout, and interactable places before content scale-up.
3. Save/load works during the tested route, but tester reports that closing the game leaves no trusted persistent save to restore on the next launch.

**Recommended Next Polish Story:**

- Polish Story 006: Spatial Scene Separation and Walkable Hub/Island Prototype. Focus on separating HUD/Chart panels from the playable scene, adding walkable island/ship boundaries, boarding access, and room-like ship spaces before expanding route/search content.
