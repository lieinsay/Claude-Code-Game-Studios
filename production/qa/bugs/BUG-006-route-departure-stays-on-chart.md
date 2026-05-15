# Bug Report

## Summary

**Title**: Route departure remains on Chart and provides no gameplay feedback
**ID**: BUG-006
**Severity**: S2-Major
**Priority**: P1-Immediate
**Status**: Verified Fixed
**Reported**: 2026-05-15
**Reporter**: Internal user QA during Production to Polish playtest Session 003

## Classification

- **Category**: Gameplay / Scene Wiring / UI Feedback
- **System**: Chart route departure, Hub runtime bridge, downstream exploration transition
- **Frequency**: Always, based on reported run
- **Regression**: Unknown

## Environment

- **Build**: Local working tree during UI/HUD QA close-out
- **Platform**: Windows desktop
- **Engine**: Godot 4.6.2 .NET
- **Scene/Level**: `res://src/scenes/SessionShell.tscn` with mounted `HubRuntime`
- **Game State**: Hub reachable, Chart open, route selectable

## Reproduction Steps

**Preconditions**: The runtime has launched successfully and the player has reached Hub.

1. Open the Chart / HUD entry from Hub.
2. Select a route.
3. Press the departure confirmation control.
4. Observe the resulting screen and feedback.

**Expected Result**: Confirming departure should either transition to exploration / the next gameplay surface or show a clear not-ready failure reason.

**Actual Result**: The runtime remains on the Chart interface. No resource pressure, threat, damage, recovery, or clear failure feedback appears.

## Technical Context

- **Likely affected files**:
  - `src/scenes/HubRuntime.gd`
  - `src/scenes/HubRuntime.tscn`
  - `tests/smoke/session_shell_visual_probe.gd`
  - `tests/integration/session/ShellUiProgram.cs`
- **Related systems**:
  - Chart Route Planning (#9)
  - Navigation / Route Risk (#10)
  - Exploration / Scavenge (#11)
  - UI / HUD / Chart Interface (#16)
- **Possible root cause**: `HubRuntime.gd` currently treats departure as a local UI state update. `_on_depart_pressed()` sets `_current_screen = "exploration"` and changes labels, but it does not hide the Chart panel, instantiate or switch to an exploration surface, dispatch a route-committed path, or display a not-ready error.

## Evidence

- **Playtest evidence**: `production/playtests/playtest-session-003-difficulty-curve-2026-05-15.md`
- **Observed behavior**: Tester understood route risk and departure choice, selected route, attempted departure, and remained on the Chart interface.
- **Missing feedback**: No visible resource, threat, damage, recovery, or failure explanation appeared.

## Related Issues

- `production/qa/bugs/BUG-005-downstream-gameplay-scene-not-mounted.md` - resolved the earlier Hub mounting blocker. This bug is a later downstream route-departure blocker.
- `production/gate-checks/gate-check-production-to-polish-2026-05-15.md` - Production to Polish gate remains blocked by missing complete visible core-loop proof.
- `production/playtests/playtest-session-003-difficulty-curve-2026-05-15.md` - difficulty cannot be evaluated until this route transition is resolved or explicitly messaged.

## Notes

This bug blocks Production to Polish because the project cannot yet prove the visible core loop from Hub to Chart to Route to post-departure gameplay. A minimal acceptable fix is either:

1. Wire departure to a reachable exploration / gameplay surface, or
2. If downstream gameplay is intentionally unavailable, show an explicit not-ready state and prevent the flow from implying that exploration has started.

## Fix Candidate

**Implemented**: 2026-05-15

Candidate changes:

- `src/scenes/HubRuntime.gd` now hides the Chart panel after departure and opens a visible Exploration HUD bridge.
- `src/scenes/HubRuntime.tscn` now includes `ExplorationPanel` with route, resource pressure, threat, hull, recovery, and return controls.
- `tests/smoke/session_shell_visual_probe.gd` now asserts that departure closes Chart and shows the Exploration HUD feedback surface.
- `tests/integration/session/ShellUiProgram.cs` now has a regression check for the post-departure Exploration HUD surface.

Automated verification:

- `dotnet run --project tests/integration/session/ShellUiTest.csproj -p:UseSharedCompilation=false`: PASS, 18/18.
- Godot headless runtime probe `tests/smoke/session_shell_visual_probe.gd`: PASS, including post-departure Exploration HUD checks.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`: PASS, 0 errors, 5 existing warnings.

Manual verification steps:

1. Launch the desktop runtime.
2. Start a session and reach Hub.
3. Open Chart, select a route, and confirm departure.
4. Confirm the visible screen changes from Chart to `探索 HUD`.
5. Confirm route, resource pressure, threat, hull, and recovery feedback are visible.

## Verification Record

**Verified**: 2026-05-15
**Verified by**: Internal user QA
**Result**: Verified Fixed

Manual retest result:

- Route selection and departure now behave normally.
- Departure no longer leaves the player stranded on the Chart interface.
- The visible `探索 HUD` bridge appears after departure and exposes the expected feedback surface.
