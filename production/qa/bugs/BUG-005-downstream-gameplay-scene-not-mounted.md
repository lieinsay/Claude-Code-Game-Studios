# Bug Report

## Summary

**Title**: Downstream gameplay scene is not mounted after audio activation
**ID**: BUG-005
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open - Deferred
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
3. Observe the resulting panel.

**Expected Result**: When downstream gameplay runtime is in scope, audio confirmation should transition to the Hub or main gameplay scene.

**Actual Result**: The Recovery panel appears with message `Audio accepted. Gameplay scene wiring is not mounted yet.`

## Technical Context

- **Likely affected files**:
  - `src/scenes/SessionShellRuntime.gd`
  - `src/scenes/SessionShell.tscn`
  - Downstream Hub/main gameplay scene wiring, once implemented
- **Related systems**: Platform Session Shell, Hub runtime, resource-facing manual QA
- **Known context**: `SessionShellRuntime.gd` intentionally exposes a clear placeholder recovery message instead of silently failing when gameplay scene wiring is unavailable.

## Impact

This blocks manual runtime cases that require Hub, resource UI, repair UI, route/exploration, save/load UI, or resource UI refresh observation:

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

This is not a ResourcesManager story acceptance failure. Epic #5 resource logic, signal contract, and persistence coverage remain validated by automated tests. This issue tracks downstream scene-flow integration needed for later manual runtime validation.
