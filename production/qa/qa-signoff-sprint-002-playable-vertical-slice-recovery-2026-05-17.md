# QA Sign-off: Sprint 002 Playable Vertical Slice Recovery

**Date:** 2026-05-17
**Sprint:** Sprint 002 Playable Vertical Slice Recovery
**QA Plan:** `production/qa/qa-plan-sprint-002-playable-vertical-slice-recovery-2026-05-17.md`
**Sprint Plan:** `production/sprints/sprint-002-playable-vertical-slice-recovery.md`
**Verdict:** APPROVED WITH CONDITIONS -- greybox recovery only

## Scope Reviewed

| Task | Result | Evidence |
| --- | --- | --- |
| PVS-001 Launch to real Hub runtime scene | PASS | `tests/smoke/session_shell_visual_probe.gd`; manual checklist PASS |
| PVS-002 Player can move in Hub | PASS | Smoke probe verifies movement; manual tester moved with WASD/arrow keys |
| PVS-003 Hub spatial interaction opens Chart | PASS | Smoke probe and checklist verify helm prompt plus E-use Chart open |
| PVS-004 Chart route selection and departure works | PASS | Smoke probe and checklist verify route selection plus departure |
| PVS-005 Exploration search/event point works | PASS | Smoke probe and checklist verify search prompt and feedback mutation |
| PVS-006 Return to Hub syncs state | PASS | Smoke probe verifies Hub cargo/hull/storage/route summary sync |
| PVS-007 Save/load restores the minimal path | PASS WITH CONDITION | Smoke and manual evidence verify route/screen/progress/player restore through the smoke save file |
| PVS-008 Smoke evidence covers playable interactions | PASS | Godot smoke probe covers movement, spatial interactions, save/load, and return |
| PVS-009 Manual playtest checklist | PASS | `production/playtests/playtest-checklist-sprint-002-playable-vertical-slice-recovery-2026-05-17.md` executed PASS |
| PVS-010 Authored greybox art | NOT IN SCOPE | Backlog item; now promoted to Sprint 003 gate-risk work |

## Automated Evidence

- `tests/smoke/session_shell_visual_probe.gd` covers SessionShell boot, HubRuntime mount, player movement, helm/search/return spatial prompts, Chart route selection, Exploration feedback, save/load restore, and Hub summary sync.
- The prior Sprint 002 session record reports `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS after the Ctrl+S / Ctrl+L shortcut fix.
- Build health is inherited from the current Production evidence set and must be rerun after Sprint 003 domain integration.

## Manual Evidence

- `production/playtests/playtest-checklist-sprint-002-playable-vertical-slice-recovery-2026-05-17.md` is executed with PASS.
- The tester found the initial `S` movement/save shortcut conflict.
- Save/load shortcuts were changed to `Ctrl+S` / `Ctrl+L`.
- The tester reported no remaining manual route issues after the shortcut fix.

## Conditions

1. This sign-off approves only the Sprint 002 greybox recovery scope.
2. Do not use this sign-off as a Production -> Polish PASS on its own.
3. Replace `HubRuntime.gd` smoke-owned route/resource/threat/hull state with
   C# domain-backed runtime authority before the final Production gate.
4. Replace or wrap `user://smoke_session_state.json` with the canonical
   persistence pipeline before the final Production gate.
5. Add minimum authored greybox landmarks and presentation feedback before the
   next gate recheck.

## Open Risks

| Risk | Severity | Disposition |
| --- | --- | --- |
| Runtime state is still owned by GDScript smoke variables | High | Sprint 003 blocker |
| Save/load is still a smoke JSON path | High | Sprint 003 blocker |
| Scene still reads as UI-panel heavy | Medium | Sprint 003 blocker for final gate evidence |
| Fun validation is partial and deterministic | Medium | Needs domain-backed manual playtest |

## Final Decision

Sprint 002 is approved as a successful playable recovery sprint. The project
remains in Production and must complete a domain-backed playable slice before
another Production -> Polish PASS attempt.

