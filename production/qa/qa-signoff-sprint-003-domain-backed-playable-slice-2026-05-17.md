# QA Sign-off: Sprint 003 Domain-Backed Playable Slice

**Date:** 2026-05-17
**Sprint:** Sprint 003 Domain-Backed Playable Slice
**QA Plan:** `production/qa/qa-plan-sprint-003-domain-backed-playable-slice-2026-05-17.md`
**Sprint Plan:** `production/sprints/sprint-003-domain-backed-playable-slice.md`
**Automated Evidence:** `production/qa/evidence/sprint-003-domain-backed-playable-smoke-evidence-2026-05-17.md`
**Manual Checklist:** `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md`
**Verdict:** PENDING HUMAN PLAYTEST

## Scope Reviewed

| Task | Result | Evidence |
| --- | --- | --- |
| PVS3-001 Define Godot-to-C# runtime adapter boundary | PASS | `production/sprints/sprint-003-runtime-adapter-boundary.md` |
| PVS3-002 Route Chart selection/departure through domain contracts | PASS | `src/presentation/PlayableSliceDomainAdapter.cs`; Godot smoke PASS |
| PVS3-003 Route search/resource/threat/hull feedback through domain managers | PASS | Adapter test 30/30; Godot smoke PASS |
| PVS3-004 Replace smoke save/load with canonical persistence adapter | PASS | `progress.*` registrations; adapter test save/load coverage |
| PVS3-005 Minimum authored greybox scene pass | PASS | Godot smoke greybox visibility assertions |
| PVS3-006 Domain-backed playable smoke probe | PASS | `production/qa/evidence/sprint-003-domain-backed-playable-smoke-evidence-2026-05-17.md` |
| PVS3-007 Manual playtest and QA sign-off | PENDING | `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md` must be executed by a human tester |

## Automated Evidence

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  PASS 30/30.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  PASS.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  PASS with 5 existing warnings, 0 errors.
- `git diff --check` PASS with LF/CRLF warnings only.

## Manual Evidence

Manual evidence is not complete yet.

Required manual source:

- `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md`

The tester must complete the Hub -> Chart -> Exploration -> Search -> Save ->
Return -> Load route without debug calls and record pass/fail notes.

## Conditions For Approval

QA can change this sign-off from PENDING to APPROVED or APPROVED WITH CONDITIONS
only after:

1. The manual checklist result section is filled with tester, build/commit, and
   route outcome.
2. No S1/S2 launch, movement, prompt, E-use, departure, search, return, or
   save/load failures remain open.
3. Any UX/design concerns are captured as conditions or follow-up tasks.
4. The sign-off explicitly states whether the evidence is strong enough for a
   Production -> Polish gate recheck.

## Open Risks

| Risk | Severity | Disposition |
| --- | --- | --- |
| Human playtest evidence is not yet recorded | High | PVS3-007 blocker |
| Exploration search still uses a documented adapter fixture for the minimum route | Medium | Production follow-up; not hidden as Polish-ready |
| Headless screenshot capture is skipped under the current display driver | Medium | Manual playtest should visually inspect layout and overlap |
| Solution build has 5 existing warnings in older test runners | Low | Track separately; not introduced by Sprint 003 PVS3-007 |

## Final Decision

Sprint 003 is not signed off yet. PVS3-001 through PVS3-006 have enough
automated evidence for QA review, but PVS3-007 remains blocked on human
playtest execution. The project remains in Production and must not advance to
Polish on this pending sign-off.

