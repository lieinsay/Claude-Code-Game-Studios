# Bug Report

## Summary

**Title**: Shell Entry buttons are visible but do not respond to clicks
**ID**: BUG-002
**Severity**: S2-Major
**Priority**: P1-Immediate
**Status**: Verified Fixed
**Reported**: 2026-05-13
**Reporter**: User during `/team-qa sprint`

## Classification

- **Category**: UI
- **System**: Platform Session Shell
- **Frequency**: Always, based on reported run
- **Regression**: Unknown

## Environment

- **Build**: Local working tree after BUG-001 fix candidate
- **Platform**: Windows desktop
- **Engine**: Godot 4.6.2 .NET
- **Scene/Level**: `res://src/scenes/SessionShell.tscn`
- **Game State**: Entry screen visible

## Reproduction Steps

**Preconditions**: The project launches visibly and reaches the `Cloud Weaver Voyage` Entry screen.

1. Click `Start Enter`.
2. Click `Settings Tab`.
3. Press `Enter` or `Tab` with the Entry screen focused.
4. Observe whether the UI changes.

**Expected Result**: Entry actions should produce visible feedback. Start should advance to the next shell step; Settings should open a visible settings/diagnostic path or an explicit not-ready message.

**Actual Result**: Buttons are visible, but clicking them produces no visible response.

## Technical Context

- **Likely affected files**:
  - `src/scenes/SessionShell.tscn`
  - `src/scenes/SessionShellRuntime.gd`
  - `src/scenes/ShellUi.tscn`
  - `tests/integration/session/ShellUiProgram.cs`
- **Related systems**: Platform Session Shell, Shell UI, manual QA runtime smoke
- **Possible root cause**: The visible shell scene had no Godot-instantiable runtime script connected to the Entry buttons. The C# node script files are excluded from the pure .NET project build and cannot currently be instantiated by Godot in this project configuration.

## Evidence

- **Visual**: User screenshot shows Entry screen with `Start Enter` and `Settings Tab`, followed by report that clicking buttons has no response.
- **Engine context**: Directly attaching the C# node scripts caused Godot headless errors: associated C# class could not be found.

## Fix Candidate

Applied 2026-05-13:

- `src/scenes/SessionShell.tscn`: attached a minimal Godot runtime binding script, `res://src/scenes/SessionShellRuntime.gd`.
- `src/scenes/SessionShellRuntime.gd`: wires Entry buttons and keyboard shortcuts.
  - Start shows the Audio Activation panel.
  - Confirm Audio / Continue Muted shows a clear downstream-not-mounted recovery message.
  - Settings toggles the diagnostic overlay if present.
  - Esc/return actions restore Entry.
- `tests/integration/session/ShellUiProgram.cs`: added regression coverage that the visible runtime scene has a script and button handlers.

Verification completed:

- `dotnet run --project tests/integration/session/ShellUiTest.csproj` - PASS, 10/10
- Godot headless load of `res://src/scenes/SessionShell.tscn` - PASS, no C# script-instantiation errors after switching to the GDScript runtime binder

Manual verification:

- 2026-05-13 user confirmed `TC-RGC-002` passed after retest.

## Related Issues

- `production/qa/bugs/BUG-001-shell-ui-stuck-loading.md`
- `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md` - TC-RGC-002

## Notes

The GDScript runtime binder is intentionally narrow and scene-local. It does not move gameplay/resource logic out of the C# foundation layer; it only restores visible shell button behavior needed for manual QA.

## Verification Record

**Verified**: 2026-05-13
**Result**: VERIFIED FIXED
**Method**: User manual retest confirmed Entry button and keyboard behavior works.
