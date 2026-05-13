# Bug Report

## Summary

**Title**: Downstream gameplay scene is not mounted after audio activation
**ID**: BUG-005
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Resolved - Fixed
**Reported**: 2026-05-13
**Reporter**: User during `/team-qa sprint`

## Classification

- **Category**: Scene Wiring
- **System**: Runtime Session Flow
- **Frequency**: Always, based on reported run
- **Regression**: Unknown

## Environment

- **Build**: Local working tree after BUG-002/BUG-003/BUG-004 fix candidates
- **Platform**: Windows desktop
- **Engine**: Godot 4.6.2 .NET
- **Scene/Level**: `res://src/scenes/SessionShell.tscn`
- **Game State**: Audio Activation panel visible

## Reproduction Steps

**Preconditions**: Shell UI is visible and `Start Enter` reaches the Audio Activation panel.

1. Click `Start Enter` from the Entry panel.
2. Click `Continue Muted M` from the Audio Activation panel.
3. Observe the resulting scene.

**Expected Result**: Audio confirmation transitions to the Hub or main gameplay scene.

**Actual Result**: Fixed 2026-05-13. Audio confirmation now mounts `res://src/scenes/HubRuntime.tscn` under `SessionShell/GameplayLayer` and hides shell panels. Recovery is only used if Hub scene loading fails.

## Technical Context

- **Likely affected files**:
  - `src/scenes/SessionShellRuntime.gd`
  - `src/scenes/SessionShell.tscn`
  - Downstream Hub/main gameplay scene wiring, once implemented
- **Related systems**: Platform Session Shell, Hub runtime, resource-facing manual QA
- **Known context**: `SessionShellRuntime.gd` now loads a minimal Hub runtime scene after audio activation; the previous placeholder recovery message is guarded by `ShellUiTest`.

## Impact

This previously blocked manual runtime cases that required the Hub to be reachable:

- TC-RGC-003
- TC-RGC-004
- TC-RGC-005
- TC-RGC-006
- TC-RGC-007
- TC-RGC-008
- TC-RGC-009

## Related Issues

- `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md` - TC-RGC-003 through TC-RGC-009
- `production/qa/bugs/BUG-002-shell-entry-buttons-not-interactive.md`
- `production/qa/bugs/BUG-004-shell-labeled-shortcuts-not-wired.md`

## Notes

## Resolution Evidence

- `src/scenes/SessionShellRuntime.gd` mounts `HubRuntime.tscn` from audio confirmation instead of showing the old placeholder recovery message.
- `src/scenes/SessionShell.tscn` now owns a `GameplayLayer` for downstream scene mounting.
- `src/scenes/HubRuntime.tscn` provides a stable initial Hub surface with station, storage, cargo, module, and hull indicators.
- `tests/integration/session/ShellUiTest.csproj` includes a regression check that fails if the old `Gameplay scene wiring is not mounted yet` placeholder returns.
- `godot --headless --quit --path .` loads the project successfully.

Remaining blocked manual cases after this fix are downstream feature/UI wiring gaps, not BUG-005: runtime transfer/pickup, repair deposit UI, route/exploration loop, runtime save/load UI, and mutation-driven resource UI refresh.
