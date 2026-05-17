# QA Sign-off: Sprint 003 Domain-Backed Playable Slice

**Date:** 2026-05-17
**Sprint:** Sprint 003 Domain-Backed Playable Slice
**QA Plan:** `production/qa/qa-plan-sprint-003-domain-backed-playable-slice-2026-05-17.md`
**Sprint Plan:** `production/sprints/sprint-003-domain-backed-playable-slice.md`
**Automated Evidence:** `production/qa/evidence/sprint-003-domain-backed-playable-smoke-evidence-2026-05-17.md`
**Manual Checklist:** `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md`
**Verdict:** APPROVED WITH CONDITIONS -- Production gate recheck ready

## Scope Reviewed

| Task | Result | Evidence |
| --- | --- | --- |
| PVS3-001 Define Godot-to-C# runtime adapter boundary | PASS | `production/sprints/sprint-003-runtime-adapter-boundary.md` |
| PVS3-002 Route Chart selection/departure through domain contracts | PASS | `src/presentation/PlayableSliceDomainAdapter.cs`; Godot smoke PASS |
| PVS3-003 Route search/resource/threat/hull feedback through domain managers | PASS | Adapter test 30/30; Godot smoke PASS |
| PVS3-004 Replace smoke save/load with canonical persistence adapter | PASS | `progress.*` registrations; adapter test save/load coverage |
| PVS3-005 Minimum authored greybox scene pass | PASS | Godot smoke greybox visibility assertions |
| PVS3-006 Domain-backed playable smoke probe | PASS | `production/qa/evidence/sprint-003-domain-backed-playable-smoke-evidence-2026-05-17.md` |
| PVS3-007 Manual playtest and QA sign-off | PASS | `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md` executed PASS by user manual report |

## Automated Evidence

- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
  PASS 30/30.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  PASS.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  PASS with 5 existing warnings, 0 errors.
- `git diff --check` PASS with LF/CRLF warnings only.

## Manual Evidence

Manual evidence is complete for the Sprint 003 PVS3-007 gate.

Manual source:

- `production/playtests/playtest-checklist-sprint-003-domain-backed-playable-slice-2026-05-17.md`

The user reports the Hub -> Chart -> Exploration -> Search -> Save -> Return ->
Load route was manually tested with no problems. The checklist records all
required route steps as PASS for commit `0437df3`.

## Conditions For Approval

QA approves Sprint 003 with conditions because:

1. The manual checklist result section is filled with tester, build/commit, and
   route outcome.
2. No S1/S2 launch, movement, prompt, E-use, departure, search, return, or
   save/load failures were reported.
3. Automated evidence and manual evidence now cover the same playable route.
4. Remaining risks are suitable for Polish entry conditions, not for blocking
   the Production -> Polish gate.

## Open Risks

| Risk | Severity | Disposition |
| --- | --- | --- |
| Human playtest evidence is now user-reported rather than video/screenshot captured | Medium | Accepted for Sprint 003 sign-off; gate recheck should cite checklist and smoke evidence together |
| Exploration search still uses a documented adapter fixture for the minimum route | Medium | Production follow-up; not hidden as Polish-ready |
| Headless screenshot capture is skipped under the current display driver | Medium | Manual playtest reported no problems; future visual QA can add captured evidence |
| Solution build has 5 existing warnings in older test runners | Low | Track separately; not introduced by Sprint 003 PVS3-007 |

## Final Decision

Sprint 003 is **APPROVED WITH CONDITIONS** for Production recovery evidence.
PVS3-001 through PVS3-007 are complete, and the project is ready for a fresh
Production -> Polish gate check. This sign-off does not itself change the stage
to Polish.
