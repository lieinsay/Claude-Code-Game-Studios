# Bug Report

## Summary

**Title**: Shell buttons do not update selection/focus frame on mouse hover
**ID**: BUG-003
**Severity**: S3-Minor
**Priority**: P2-Next Sprint
**Status**: Verified Fixed
**Reported**: 2026-05-13
**Reporter**: User during `/team-qa sprint`

## Classification

- **Category**: UI
- **System**: Platform Session Shell
- **Frequency**: Always, based on reported run
- **Regression**: Unknown

## Environment

- **Build**: Local working tree after BUG-002 fix candidate
- **Platform**: Windows desktop
- **Engine**: Godot 4.6.2 .NET
- **Scene/Level**: `res://src/scenes/SessionShell.tscn`
- **Game State**: Entry screen visible

## Reproduction Steps

**Preconditions**: The Entry screen is visible with `Start Enter` and `Settings Tab` buttons.

1. Move the mouse cursor over `Start Enter`.
2. Move the mouse cursor over `Settings Tab`.
3. Observe the selected/focused button frame.

**Expected Result**: Hovering a button should update the visible selection/focus frame so mouse and keyboard focus agree.

**Actual Result**: Moving the mouse across buttons does not change the selected/focused frame.

## Technical Context

- **Likely affected files**:
  - `src/scenes/SessionShellRuntime.gd`
  - `tests/integration/session/ShellUiProgram.cs`
- **Related systems**: Platform Session Shell, Shell UI, manual QA runtime smoke
- **Possible root cause**: Godot buttons do not automatically grab keyboard focus on hover. The runtime shell binding connected `pressed` handlers but did not connect `mouse_entered` to focus updates.

## Fix Candidate

Applied 2026-05-13:

- `src/scenes/SessionShellRuntime.gd`: each wired shell button now connects `mouse_entered` to `_on_button_mouse_entered(button)`, which calls `grab_focus()` for visible, enabled buttons.
- `tests/integration/session/ShellUiProgram.cs`: regression check now verifies the runtime binder includes hover-to-focus wiring.

Verification completed:

- `dotnet run --project tests/integration/session/ShellUiTest.csproj` - PASS, 10/10
- Godot headless load of `res://src/scenes/SessionShell.tscn` - PASS

Manual verification:

- 2026-05-13 user confirmed `TC-RGC-002` passed after retest, including hover/focus behavior.

## Related Issues

- `production/qa/bugs/BUG-001-shell-ui-stuck-loading.md`
- `production/qa/bugs/BUG-002-shell-entry-buttons-not-interactive.md`
- `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md` - TC-RGC-002

## Notes

This is a shell UI usability issue, not a ResourcesManager story acceptance failure.

## Verification Record

**Verified**: 2026-05-13
**Result**: VERIFIED FIXED
**Method**: User manual retest confirmed shell keyboard/mouse input works.
