# Polish 013 Automated Preflight Evidence

**Date:** 2026-05-23  
**Story:** `production/polish-backlog/story-polish-013-human-long-session-release-triage.md`  
**Status:** PASS WITH NOTES  
**Purpose:** Establish the automated baseline before human long-session release triage.

This evidence does not establish Release readiness. It only records whether the
current build and smoke probes are healthy enough for a human windowed triage
session.

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 0 warnings, 0 errors. |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 127/127 checks passing. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Visual/runtime route/search/save/load loop passed; screenshots skipped under current headless display driver. |
| `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd` | PASS | Cross-launch durable progress, corrupt-save quarantine, overwrite, delete, and disabled-state checks passed. |
| `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd` | PASS | Three route/search/save/load/return cycles plus final latest-state load passed. |
| `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd` | PASS WITH RERUN | First run failed only the worst-frame transient ceiling at 20.388ms against a 20ms limit; immediate rerun passed with worst 19.569ms. |

## Initial Assessment

- Known S1/S2 bugs: No open S1/S2 found in `production/qa/bugs`; historical S2 bugs are `Verified Fixed` or `Resolved - Fixed`.
- Build health: PASS.
- Domain adapter health: PASS.
- Godot smoke health: PASS WITH NOTES due one transient headless perf first-run spike.
- Human QA readiness: READY for windowed long-session triage.

## Release Triage Boundary

Proceed to human long-session QA. The only automated note is a transient
headless performance first-run spike that passed on immediate rerun and did not
affect p95 frame, save/load, memory, or transition budgets.

Do not proceed to a formal release checklist/gate until
`production/playtests/playtest-checklist-polish-013-human-long-session-release-triage-2026-05-23.md`
is executed and its findings are classified.
