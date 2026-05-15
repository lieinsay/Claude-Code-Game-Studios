# Playtest Report: Session 003 Difficulty Curve

## Session Info

- **Status:** EXECUTED - PASSED WITH NOTES
- **Date:** 2026-05-15
- **Build:** Current workspace, exact commit/build ID to be filled before execution
- **Duration:** Not timed; initial route departure blocked, retests passed after BUG-006 and Hub summary sync fixes
- **Tester:** Internal user QA
- **Platform:** Windows desktop
- **Input Method:** Keyboard and mouse
- **Session Type:** Returning / difficulty and pressure test

## Test Focus

Does the current route risk, resource pressure, threat handling, damage/recovery, and save/load loop feel understandable and fair?

## First Impressions (First 5 Minutes)

- **Understood the goal?** Yes, for route risk and departure choice
- **Understood the controls?** Yes
- **Emotional response:** No blocker or confusion reported after final retest
- **Notes:** Initial run found that departure stayed on Chart. After BUG-006 and Hub summary sync fixes, tester retested route departure, three-step pressure loop, Save/Load, return Hub, and Hub summary sync and reported no issue.

## Gameplay Flow

### What worked well

- Route risk / departure choice was understandable.
- Route departure now reaches Exploration HUD.
- Three-step pressure loop is visible.
- Returning Hub now syncs cargo, storage, hull, and route pressure summary.

### Pain points

- Initial run: departure did not enter exploration or the next gameplay surface.
- Retest: departure now reaches the visible Exploration HUD bridge.
- Second retest: Hub summary sync issue was fixed and manually confirmed.

### Confusion points

- No confusion reported in route departure, pressure-loop feedback, Save/Load restore, return Hub, or Hub summary sync.
- Difficulty tuning remains coarse because the loop is deterministic MVP smoke content, not final balance.

### Moments of delight

- None reported after final retest.

## Bugs Encountered

| # | Description | Severity | Reproducible |
| --- | --- | --- | --- |
| 1 | Route departure attempt remained on the Chart interface instead of entering exploration or another gameplay surface. | S2-Major | Yes - verified fixed in BUG-006 retest |
| 2 | No visible resource, threat, damage, or recovery feedback appeared after departure attempt. | S2-Major | Yes - bridge feedback verified in BUG-006 retest |

## Feature-Specific Feedback

### Route Risk

- **Understood purpose?** Yes
- **Found engaging?** Unable to evaluate beyond selection
- **Suggestions:** Departure should either transition to gameplay or display an explicit not-ready reason.

### Resource Pressure

- **Understood purpose?** Yes
- **Found engaging?** Adequate for MVP smoke
- **Suggestions:** Replace deterministic smoke values with real domain values in later production work.

### Threat / Damage / Recovery

- **Understood purpose?** Yes
- **Found engaging?** Adequate for MVP smoke
- **Suggestions:** Replace deterministic smoke values with real threat/damage/recovery systems in later production work.

## Quantitative Data

- **Routes attempted:** At least one route/departure path retested successfully.
- **Threat events encountered:** Deterministic low-threat then medium-threat feedback visible.
- **Resources gained/lost:** Deterministic supply/cargo/reward feedback visible.
- **Damage events:** Deterministic hull damage feedback visible at 94/100.
- **Recovery actions:** Return Hub and Save/Load restore verified.

## Overall Assessment

- **Would play again?** Not evaluated
- **Difficulty:** Understandable for MVP smoke; not final-tuned
- **Pacing:** Adequate for the three-step smoke loop
- **Session length preference:** Not evaluated
- **Fun hypothesis:** Partially validated for route departure, visible pressure feedback, Save/Load restore, and Hub return summary. Not final-validated for production balance or authored encounters.

## Top 3 Priorities From This Session

1. BUG-006 retest passed: route departure now leaves Chart and reaches the visible Exploration HUD bridge.
2. Pressure-loop retest passed: `推进探索 / 搜索` mutates resource pressure, threat, hull, and recovery feedback over three steps.
3. Hub summary sync retest passed: returning Hub updates cargo, storage, hull, and route pressure summary.

## Retest Note

After the BUG-006 fix candidate, internal user QA retested route departure and reported: "正常". This verifies the route departure blocker fixed for the current visible runtime bridge.

## Pressure Loop Implementation Note

**Implemented**: 2026-05-15

The runtime bridge now exposes a deterministic three-step pressure loop with Hub summary sync:

1. Search creates resource pressure and low-threat feedback.
2. Second advance escalates threat and shows hull damage feedback.
3. Third advance locks rewards and completes the return-ready pressure loop.
4. Returning to Hub syncs cargo, storage, hull, and route pressure summary text.

Automated evidence:

- `dotnet run --project tests/integration/session/ShellUiTest.csproj -p:UseSharedCompilation=false`: PASS, 18/18.
- Godot headless smoke probe `tests/smoke/session_shell_visual_probe.gd`: PASS, including pressure loop, save/load restore, return-to-Hub, and Hub summary sync checks.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`: PASS, 0 warnings, 0 errors.

Manual retest completed on 2026-05-15; the MVP pressure loop is validated for smoke pacing, while final authored encounter balance remains future tuning scope.

## Final Manual Retest

**Date:** 2026-05-15
**Tester:** Internal user QA
**Result:** Passed with notes

Manual result:

- Route departure works.
- Three-step pressure loop works.
- Save/Load restore works.
- Return Hub works.
- Hub summary sync works.
- No new issue reported.
