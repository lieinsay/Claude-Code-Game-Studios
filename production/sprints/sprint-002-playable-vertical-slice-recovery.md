# Sprint 002 -- 2026-05-17 to 2026-05-22

## Sprint Goal

Recover a real playable vertical slice: launch into Hub, move a player avatar,
interact with Hub and Exploration points, complete a route loop, and restore it
through save/load.

## Context

- Stage: `Production`
- Gate recheck: `production/gate-checks/gate-check-production-to-polish-2026-05-17-playable-recovery.md`
- Verdict: Production to Polish is **FAIL** until the playable slice is verified.
- Historical note: `sprint-001-polish-stabilization` remains evidence, but it is
  no longer the active sprint.

## Capacity

- Total days: 6
- Buffer (20%): 1 day reserved for unplanned work
- Available: 5 days

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| PVS-001 | Launch to real Hub runtime scene | godot-specialist / developer | 0.5 | Existing `SessionShell.tscn` | Starting a new session mounts `HubRuntime` and visible Hub state. |
| PVS-002 | Player can move in Hub | godot-specialist / developer | 0.75 | PVS-001 | A visible player marker responds to project movement actions. |
| PVS-003 | Hub spatial interaction opens Chart | godot-specialist / developer | 0.75 | PVS-002 | Moving near the Hub helm point shows an E-use prompt and opens Chart. |
| PVS-004 | Chart route selection and departure works | ui-programmer / developer | 0.75 | PVS-003 | Player can select one route, confirm departure, and enter Exploration. |
| PVS-005 | Exploration search/event point works | gameplay-programmer / developer | 1.0 | PVS-004 | Moving near a search point and pressing E changes resource/threat/HUD feedback. |
| PVS-006 | Return to Hub syncs state | gameplay-programmer / developer | 0.75 | PVS-005 | Moving near a return point and pressing E returns to Hub with cargo/hull/route status visible. |
| PVS-007 | Save/load restores the minimal path | godot-specialist / developer | 0.75 | PVS-005 | Save/load restores route, exploration step, active screen, and player position. |
| PVS-008 | Smoke evidence covers playable interactions | qa-lead / developer | 0.5 | PVS-001..PVS-007 | Godot smoke probe verifies movement, spatial Hub interaction, spatial Exploration interaction, save/load, and return. |

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| PVS-009 | Add manual playtest checklist for the slice | qa-lead | 0.5 | PVS-008 | Checklist distinguishes human play evidence from scripted/headless evidence. |

### Nice to Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|--------------|---------------------|
| PVS-010 | Replace placeholder markers with authored greybox art | art-director / technical-artist | 0.75 | PVS-008 | Visuals improve without expanding mechanics or changing acceptance criteria. |

## Progress Notes

- **PVS-001 to PVS-008 started 2026-05-17:** Existing `HubRuntime` now includes a
  minimal playable layer, visible player marker, Hub helm/storage points,
  Exploration search/return points, E-use prompts, and player-position save/load.
- **Smoke probe updated 2026-05-17:** `tests/smoke/session_shell_visual_probe.gd`
  now checks player movement via input actions and spatial interaction prompts.
- **PVS-009 complete — 2026-05-17:** Manual playtest checklist created at
  `production/playtests/playtest-checklist-sprint-002-playable-vertical-slice-recovery-2026-05-17.md`.
- **Manual playtest PASS — 2026-05-17:** User completed the playable slice route
  after the WASD/save shortcut conflict was fixed. Other manual tests reported
  no issues.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Placeholder scene is still too UI-like | Medium | Medium | Manual test confirms the greybox loop is playable; keep final art/content as follow-up and do not block Sprint 002 recovery. |
| Existing C# domain systems remain loosely connected to runtime | Medium | High | Accepted as follow-up Production work: replace smoke-state stubs with domain-backed adapters before final Production to Polish PASS. |
| Save/load is still a smoke save rather than full persistence integration | Medium | Medium | Manual and smoke tests confirm minimal path restore; full persistence integration remains required before final gate PASS. |

## QA Plan

`production/qa/qa-plan-sprint-002-playable-vertical-slice-recovery-2026-05-17.md`

The smoke probe is the automated acceptance gate. The manual playtest checklist
is required before any Production to Polish PASS claim.

## Definition of Done for this Sprint

- [x] All Must Have tasks completed
- [x] Godot smoke probe passes with playable movement and spatial interactions
- [x] Manual playtest confirms a human can complete Hub -> Chart -> Exploration -> Return
- [x] Save/load restores the minimal path
- [x] No S1/S2 bugs remain open in the playable slice
- [x] Production to Polish gate can be rechecked without relying on headless-only evidence
