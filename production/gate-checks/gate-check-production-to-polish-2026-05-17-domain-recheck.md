# Gate Check: Production to Polish -- Domain Recheck

**Date:** 2026-05-17
**Checked by:** gate-check skill via Codex adapter
**Review mode:** `lean`
**Target gate:** Production to Polish
**Verdict:** FAIL -- remain in Production

## Scope Resolution

`production/stage.txt` remains:

```text
Production
```

This recheck treats Sprint 002 as recovery evidence, not as a Polish pass. The
new greybox route proves that a human can now complete a minimal Hub -> Chart ->
Exploration -> Return loop, but the runtime is still a Godot smoke bridge whose
gameplay state is owned by `src/scenes/HubRuntime.gd` rather than by the C#
domain managers and canonical persistence path.

## Required Artifacts

| Requirement | Status | Evidence |
| --- | --- | --- |
| `src/` has active subsystem code | PASS | `src/core`, `src/presentation`, and `src/scenes` contain active C# and Godot runtime code. |
| Core mechanics from GDD implemented | CONCERNS | C# managers exist for Hub, Chart, Exploration, Resources, Navigation, Modules/Hull, Combat, UI, Feedback, and Persistence, but the playable Godot route does not yet consume them as runtime authority. |
| Main gameplay path playable end-to-end | PASS WITH CONDITIONS | Sprint 002 smoke and manual checklist prove a human-playable greybox loop: move, use helm, select route, depart, search, return, save, and load. |
| Logic and integration tests exist | PASS | Existing production evidence includes C# unit/integration runners and the Godot playable smoke probe. |
| Logic stories have corresponding tests | PASS | Existing story evidence maps logic/integration stories to C# test projects. |
| Smoke check report exists and passes | PASS WITH CONDITIONS | `tests/smoke/session_shell_visual_probe.gd` covers player movement and spatial interactions; no Sprint 002 smoke report file has been promoted yet. |
| QA plan exists | PASS | `production/qa/qa-plan-sprint-002-playable-vertical-slice-recovery-2026-05-17.md`. |
| QA sign-off exists | PASS WITH CONDITIONS | `production/qa/qa-signoff-sprint-002-playable-vertical-slice-recovery-2026-05-17.md` approves greybox recovery only. |
| At least 3 playtest sessions documented | PASS WITH CONDITIONS | The 2026-05-15 three-session set exists, and Sprint 002 has a focused manual checklist PASS. These still validate smoke/MVP clarity more than authored production content. |
| Playtests cover new player, mid-game systems, and difficulty curve | CONCERNS | Coverage exists, but the difficulty and fun findings repeatedly describe deterministic smoke content rather than production balance. |
| Fun hypothesis validated or revised | FAIL | Current evidence partially validates clarity and route feedback. It does not validate fun for domain-backed encounters, resource pressure, threat, or authored greybox presentation. |

## Quality Checks

| Requirement | Status | Evidence |
| --- | --- | --- |
| Tests are passing | PASS WITH CONDITIONS | Latest recorded evidence is passing; this recheck should be followed by a fresh smoke/build sweep after Sprint 003 implementation. |
| No critical/blocker bugs | PASS WITH CONDITIONS | No S1/S2 bug remains open for Sprint 002 greybox recovery; domain integration risks are tracked as Production blockers, not bugs. |
| Core loop plays as designed | FAIL | The route is playable, but core loop authority is still duplicated in `HubRuntime.gd` labels and smoke save data instead of real C# managers. |
| Performance within budget | PASS WITH CONDITIONS | Sprint 001 numeric smoke profile passed; domain-backed runtime must be reprofiled after integration. |
| Playtest findings reviewed | PASS | Existing playtest reports and Sprint 002 checklist are reviewed. |
| No confusion loops | PASS WITH CONDITIONS | No confusion was reported for the greybox route, but authored presentation and domain-backed feedback are untested. |
| Difficulty curve matches design | CONCERNS | Deterministic smoke values are understandable but not a real difficulty curve. |
| UX specs exist for implemented screens | CONCERNS | Hub, Chart, Exploration, and interaction-pattern docs exist; pause/menu and domain-backed feedback UX coverage still need confirmation before final gate. |
| Interaction pattern library up to date | CONCERNS | Pattern docs cover the UI bridge, but not the next domain-backed spatial/runtime adapter layer. |
| Accessibility compliance verified | PASS WITH CONDITIONS | Keyboard movement, E-use prompts, and visible text feedback are covered in Sprint 002; domain-backed feedback and upgraded greybox presentation need a fresh spot check. |

## Director Panel

Not executed. The canonical workflow requests parallel director review, but this
Codex App session did not receive an explicit user request for delegated
subagents. Director review is therefore absent evidence, not approval.

## Answers to Current Production Questions

1. **Is the greybox playable slice enough as minimal evidence?** Yes for
   proving recovery from panel-only demo to human-playable greybox. No for
   Production -> Polish PASS.
2. **Does the current Hub/Exploration feedback need real C# domain managers and
   persistence?** Yes. This is now the primary Production blocker.
3. **Does the greybox need a minimum presentation upgrade?** Yes. It does not
   need final art, but it needs authored spatial landmarks, clearer deck/world
   composition, and feedback that feels like a game scene rather than only UI
   panels.
4. **Should the project create/update the next Production sprint?** Yes. Sprint
   003 should focus on domain-backed runtime authority and minimum greybox
   presentation before another gate attempt.
5. **Can the project enter Polish now?** No.

## Blockers

1. **Runtime authority is still stub-owned.** `HubRuntime.gd` stores route,
   exploration step, resource/threat/hull labels, and save/load snapshot itself.
2. **Persistence path is still smoke-specific.** The save file is
   `user://smoke_session_state.json`, not the canonical C# persistence pipeline.
3. **Greybox presentation remains too panel-like for final Production evidence.**
   The player can move and use spatial points, but the scene still reads mostly
   as a UI dashboard.
4. **Fun validation is partial.** Existing human reports validate usability and
   understandable smoke feedback, not domain-backed encounters or route pressure.

## Required Next Sprint

Create or activate:

- `production/sprints/sprint-003-domain-backed-playable-slice.md`

Minimum PASS path for the next recheck:

1. Hub, Chart, Exploration, Resources/Hull, and Persistence runtime state comes
   from C# domain managers or explicit Godot-to-C# adapter seams.
2. Save/load uses the production persistence flow or a documented adapter around
   it, not a standalone smoke JSON file.
3. The same human-playable route remains intact after domain integration.
4. The greybox scene gets enough spatial composition and feedback presentation
   to read as an in-world playable slice.
5. Fresh smoke, build, QA sign-off, and manual playtest evidence are recorded.

## Chain-of-Verification

Five challenge questions were checked:

1. **Did I over-credit Sprint 002?** No. Sprint 002 is credited for playable
   recovery, not for Polish readiness.
2. **Is any FAIL item already solved elsewhere?** No. C# managers exist, but
   `HubRuntime.gd` does not consume them as runtime authority.
3. **Could the missing Sprint 002 QA sign-off alone explain the fail?** No. A
   sign-off can approve greybox recovery, but domain authority and presentation
   blockers remain.
4. **Could the greybox presentation be deferred to Polish?** Not fully. Polish
   can refine art, but Production must prove the playable scene is a game loop,
   not merely a panel demo with a marker.
5. **Is the minimal path to PASS clear?** Yes: domain-backed runtime adapter,
   canonical persistence, minimum greybox scene upgrade, fresh smoke/build/manual
   evidence.

**Chain-of-Verification Result:** verdict remains **FAIL**.

