# Polish Story 012 Evidence: Long-Session Save / Load Soak

> Date: 2026-05-23
> Story: `production/polish-backlog/story-polish-012-long-session-save-load-soak.md`
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- Added a bounded headless soak probe for repeated playable-slice save/load use.
- The probe runs three Hub -> Chart -> Exploration -> pressure -> Save -> Load -> Return cycles.
- Repeated saves exercise the overwrite-confirmation path introduced by Polish Story 011.
- Each cycle verifies generation advancement, durable progress presence, canonical load status, returned rewards, and continued Hub Load/Delete availability.
- The final load restores the latest saved generation and pressure step.

## Automated Checks

- `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd`
  - PASS.
  - Covers three repeated route/search/save/load/return cycles plus final latest-state load.

## Remaining Conditions

- This is automated soak coverage only.
- Human long-session QA, final windowed QA, release gate checklist, named save slots, and full save-management UI remain downstream.
- No Release readiness claim is made by this evidence.
