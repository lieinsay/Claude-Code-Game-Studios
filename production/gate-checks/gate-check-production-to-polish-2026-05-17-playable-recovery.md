# Gate Check: Production to Polish — Playable Recovery Recheck

**Date:** 2026-05-17
**Checked by:** gate-check skill via Codex adapter
**Target gate:** Production to Polish
**Verdict:** FAIL — remain in Production

## Diagnosis

The project has substantial completed C# systems, runner evidence, and UI smoke
coverage, but the previous Production to Polish interpretation was too generous:
Epic completion mostly proves headless logic, integration contracts, test
runners, and documentation. It does not prove the game is production-complete or
ready for Polish.

`production/stage.txt` correctly remains:

```text
Production
```

## Production to Polish Checks

| Requirement | Status | Evidence |
| --- | --- | --- |
| Main gameplay path is playable end-to-end | FAIL | The 2026-05-15 smoke evidence exercised a button-driven Hub/Chart/Exploration HUD loop, not a true player-controlled scene path. |
| Real scene and player interaction exist | FAIL, now recovering | Before this recheck, `HubRuntime` had visible panels and buttons but no movable player entity or spatial interaction point. |
| Hub to Chart to Exploration to Return runs in Godot | PARTIAL | Godot can mount `SessionShell.tscn` and `HubRuntime.tscn`; Chart, Exploration, Save/Load, and Return are present as a runtime bridge. |
| Smoke / QA / playtest evidence proves real playability | FAIL | Existing reports mostly prove headless C# and scripted smoke calls. They do not prove a human can move through and interact with a playable scene. |
| Save/load restores the minimal path | PARTIAL | Save/load restores screen, route, and exploration step. Recovery sprint adds player position to make the playable path restorable. |

## Current Blockers

1. **Playable space was missing.** The game had no movable player in the Hub or
   Exploration runtime surface.
2. **Interactions were panel-first.** Hub and Exploration actions were available
   as buttons or scripted method calls rather than spatial player interactions.
3. **Evidence was mislabeled.** Prior smoke/playtest reports validated a useful
   bridge, but not a production-ready playable core loop.
4. **Polish sprint status is premature.** `sprint-001-polish-stabilization` is
   retained as historical evidence, but it should not drive current work.

## Recovery Action Started

A new Production sprint has been created:

- `production/sprints/sprint-002-playable-vertical-slice-recovery.md`
- `production/qa/qa-plan-sprint-002-playable-vertical-slice-recovery-2026-05-17.md`
- `production/playtests/playtest-checklist-sprint-002-playable-vertical-slice-recovery-2026-05-17.md`
- Goal: **Playable Vertical Slice Recovery**

Minimum path:

1. Launch to a real Hub runtime scene.
2. Move the player with keyboard input.
3. Use at least one Hub spatial interaction point.
4. Open Chart and choose a route.
5. Confirm departure to Exploration.
6. Use at least one Exploration search/event point.
7. Receive visible resource or status feedback.
8. Return to Hub.
9. HUD/status text reflects changes.
10. Save/load restores the route, exploration progress, and player position.

## File Plan

| File | Purpose |
| --- | --- |
| `src/scenes/HubRuntime.gd` | Add minimal movable player marker, Hub/Exploration spatial interaction markers, E-use handling, and player-position persistence. |
| `tests/smoke/session_shell_visual_probe.gd` | Extend Godot smoke coverage from button-only calls to player movement and spatial interactions. |
| `production/sprints/sprint-002-playable-vertical-slice-recovery.md` | Track the new Production sprint and acceptance criteria. |
| `production/qa/qa-plan-sprint-002-playable-vertical-slice-recovery-2026-05-17.md` | Define automated and manual QA requirements for the recovery sprint. |
| `production/playtests/playtest-checklist-sprint-002-playable-vertical-slice-recovery-2026-05-17.md` | Separate human-play evidence from scripted/headless smoke evidence. |
| `production/sprint-status.yaml` | Point active production status at Sprint 002. |
| `production/session-state/active.md` | Record the current recovery context. |

## Chain-of-Verification

Five challenge questions were checked:

1. **Could prior Epic Complete status prove Production completion?** No. It proves
   systems and tests, not a playable vertical slice.
2. **Does the 2026-05-15 smoke check prove a human can play?** No. It uses direct
   method calls and panels.
3. **Is there a Godot runtime path at all?** Yes. `SessionShell.tscn` mounts
   `HubRuntime.tscn`, so recovery can build on it.
4. **Is a Polish sprint appropriate now?** No. The active work must return to
   Production until the playable loop is real and verified.
5. **Is the recovery scope small enough?** Yes. It touches only the current Hub
   runtime bridge, smoke probe, and production tracking files.

**Result:** Verdict remains **FAIL**. Continue Production, do not advance to
Polish.
