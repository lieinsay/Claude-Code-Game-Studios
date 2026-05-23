# Polish Story 013: Human Long-Session Release Readiness Triage

> **Phase**: Polish
> **Status**: Complete With Notes
> **Layer**: QA / Release Readiness
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Persistence, ADR-0012 UI Input Routing, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 012 long-session save/load soak

## Context

Polish Stories 007-012 closed the current automated persistence and repeated-use
trust gaps for the playable slice: durable progress survives restart, Load
availability is visible, invalid progress is quarantined, destructive actions
are confirmed, and the headless long-session soak covers repeated
save/load/return cycles.

The remaining release-adjacent risk is human confidence in a real windowed
session. This story does not declare Release readiness. It creates and executes
a focused triage pass that separates blockers from ordinary polish before any
formal Release gate.

## Acceptance Criteria

- [x] GIVEN the current project state is read, WHEN this story starts, THEN all Polish Story 001-012 evidence is treated as prerequisite context rather than re-scoped feature work.
- [x] GIVEN a fresh working tree build, WHEN automated preflight runs, THEN the solution build and playable-slice domain adapter regression pass or blockers are recorded.
- [x] GIVEN the Godot smoke probes run, WHEN visual, durable persistence, long-session, and performance probes complete, THEN pass/fail evidence is recorded for release-readiness triage.
- [x] GIVEN a tester runs the windowed long-session checklist, WHEN the session includes launch, restart, Continue/Load, Save, Overwrite, Delete, Quarantine, route/search/return loops, and final restart, THEN each issue is classified as Release blocker, Polish follow-up, or Post-release/non-MVP.
- [x] GIVEN the tester completes subjective notes, WHEN presentation, art/audio, pacing, and save-trust findings are reviewed, THEN final art/audio treatment is either marked non-blocking or promoted to a release-blocking follow-up story with evidence.
- [x] GIVEN this story is closed, WHEN the next action is chosen, THEN the project either proceeds to a formal release checklist/gate or opens the smallest blocking Polish story.

## Implementation Notes

- Do not change runtime behavior in this story unless preflight reveals a real blocker.
- Keep the checklist executable by one human tester in a 30-45 minute session.
- Record windowed/manual evidence separately from automated smoke evidence.
- Do not create a full release checklist until this triage says there are no known release blockers.
- Treat named save slots, full save-management UI, content scale-up, and final asset production as blockers only if the current MVP slice cannot be trusted or understood without them.

## Evidence Targets

- `production/playtests/playtest-checklist-polish-013-human-long-session-release-triage-2026-05-23.md`
- `production/qa/evidence/polish-013-release-readiness-automated-preflight-evidence.md`

## Closure Rule

This story can close as `COMPLETE WITH NOTES` if automated preflight passes and
manual testing identifies only documented non-blocking Polish or post-MVP work.
It must close as `BLOCKED` if any S1/S2 defect, persistence trust failure,
unrecoverable input trap, or release-blocking presentation/accessibility issue is
found.

## Progress Notes

- Started 2026-05-23.
- Automated preflight evidence recorded in `production/qa/evidence/polish-013-release-readiness-automated-preflight-evidence.md`.
- Human checklist executed by liein on 2026-05-23 with verdict PASS WITH CONDITIONS.
- Stability and persistence passed: launch, save/load, cross-launch restore, overwrite, delete, quarantine, and repeated route/search/return all completed.
- Release checklist/gate should not start yet. Human findings identify a release-readiness presentation/gameplay blocker: Hub room identity is not visually readable, Exploration has no meaningful image/art treatment, and the route/search loop can be completed mostly by clicking UI rather than movement-driven play.
- Next action: open the smallest blocking Polish story focused on authored playable-space readability and movement-driven interaction before formal release checklist/gate.
