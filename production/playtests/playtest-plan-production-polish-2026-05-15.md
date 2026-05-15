# Playtest Execution Plan: Production to Polish Gate

**Date:** 2026-05-15
**Status:** PENDING EXECUTION
**Gate:** Production to Polish
**Purpose:** Fill the playtest evidence gap identified by `production/gate-checks/gate-check-production-to-polish-2026-05-15.md`.

## Required Sessions

| Session | Focus | Target Tester | Required Outcome |
| --- | --- | --- | --- |
| 001 | New player experience | First-time tester or internal tester acting cold | Confirms whether the game communicates the goal and controls within the first 2 minutes. |
| 002 | Mid-game systems | Returning/internal tester | Confirms Hub, Chart, Save/Load, inventory/resource, and interaction systems are understandable without developer guidance. |
| 003 | Difficulty curve | Returning/internal tester | Confirms route risk, resource pressure, threat handling, and recovery feel fair rather than confusing. |

## Shared Test Build

Use the current desktop Godot .NET project state unless a commit/build id is supplied before execution.

```text
Engine: Godot 4.6.2 .NET
Platform: Windows desktop
Input: Keyboard and mouse
Entry scene: src/scenes/Main.tscn
```

## Required Route

Each session should attempt the longest available visible route:

1. Launch the project.
2. Start a new session.
3. Reach Hub.
4. Find and open UI/HUD / Chart.
5. Select a route if available.
6. Confirm departure if available.
7. Observe whether exploration or the next gameplay surface appears.
8. Save and load at least once.
9. Record where the playable loop stops if it does not complete.

## Evidence Rules

- Do not mark a session PASS unless a human has executed it.
- If a tester gets stuck, record the exact screen, action attempted, and wording that confused them.
- If the route/exploration transition is not yet wired, record that as a product gap rather than a tester failure.
- Attach screenshots or video when available, but written notes are acceptable for internal gate evidence.

## Gate Acceptance Criteria

The Production to Polish gate can be reconsidered when:

- All three session files have status `EXECUTED`.
- The top three findings are categorized as design, bug, balance, or polish.
- Any S1/S2 blockers are fixed or explicitly accepted with owner/date.
- Fun hypothesis is marked `validated`, `revised`, or `not validated` with evidence.
