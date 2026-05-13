# Bug Report

## Summary

**Title**: Shell labeled shortcuts beyond Enter/Tab/Esc are not wired
**ID**: BUG-004
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

- **Build**: Local working tree after BUG-002/BUG-003 fix candidates
- **Platform**: Windows desktop
- **Engine**: Godot 4.6.2 .NET
- **Scene/Level**: `res://src/scenes/SessionShell.tscn`
- **Game State**: Entry, Audio Activation, or Recovery shell panels visible

## Reproduction Steps

**Preconditions**: Shell UI is visible and buttons show shortcut labels such as `M`, `R`, `N`, or `D`.

1. Reach a shell panel with labeled shortcut buttons.
2. Press the matching keyboard shortcut shown in the button label.
3. Observe whether the action fires.

**Expected Result**: Every visible shortcut label should invoke the same behavior as clicking that button.

**Actual Result**: `Enter`, `Tab`, and `Esc` work, but other visible shortcut labels do not respond.

## Technical Context

- **Likely affected files**:
  - `src/scenes/SessionShellRuntime.gd`
  - `tests/integration/session/ShellUiProgram.cs`
  - `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md`
- **Related systems**: Platform Session Shell, Shell UI, manual QA runtime smoke
- **Possible root cause**: The runtime shell binding handled only Entry shortcuts and global Esc. Shortcuts for the Audio Activation and Recovery panels were present in button labels but not mapped in `_unhandled_input`.

## Fix Candidate

Applied 2026-05-13:

- `src/scenes/SessionShellRuntime.gd`: added panel-specific shortcut handlers.
  - Entry: `Enter` = Start, `Tab` = Settings.
  - Audio Activation: `Enter` = Activate Audio, `M` = Continue Muted.
  - Recovery: `R` = Retry/Return to Entry, `N` = New Session/Return to Entry, `D` = Error Details/Settings.
  - Global: `Esc` = Return to Entry.
- `tests/integration/session/ShellUiProgram.cs`: regression check now verifies the runtime script includes `M/R/N/D` shortcut mappings.
- `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md`: added shell button behavior reference.

Verification completed:

- `dotnet run --project tests/integration/session/ShellUiTest.csproj` - PASS, 10/10
- `dotnet build CloudWeaverVoyage.sln --no-restore` - PASS, 0 warnings, 0 errors
- Godot headless load of `res://src/scenes/SessionShell.tscn` - PASS

Manual verification:

- 2026-05-13 user confirmed `TC-RGC-002` passed after retest, including labeled shortcut behavior.

## Related Issues

- `production/qa/bugs/BUG-002-shell-entry-buttons-not-interactive.md`
- `production/qa/bugs/BUG-003-shell-button-hover-does-not-focus.md`
- `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md` - TC-RGC-002

## Notes

This is a shell UI input consistency issue, not a ResourcesManager story acceptance failure.

## Verification Record

**Verified**: 2026-05-13
**Result**: VERIFIED FIXED
**Method**: User manual retest confirmed shell labeled shortcuts work.
