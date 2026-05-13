# Bug Report

## Summary

**Title**: Shell UI remains on Loading Session Shell indefinitely
**ID**: BUG-001
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

- **Build**: Local working tree after Resources, Goods & Capacity #5 completion
- **Platform**: Windows desktop
- **Engine**: Godot 4.6.2 .NET
- **Scene/Level**: `res://src/scenes/SessionShell.tscn`
- **Game State**: Fresh visible launch from Godot editor/debug run

## Reproduction Steps

**Preconditions**: Godot 4.6.2 .NET is available and the project opens successfully.

1. Launch the project visibly from Godot.
2. Wait for shell startup.
3. Observe the initial screen.

**Expected Result**: The shell should leave Loading after boot reaches an entry-ready state, or show an actionable entry/recovery screen.

**Actual Result**: The screen remains on `Loading Session Shell` / `Checking game data...` indefinitely with only `Cancel Esc` visible.

## Technical Context

- **Likely affected files**:
  - `src/scenes/ShellUi.tscn`
  - `src/scenes/SessionShell.cs`
  - `src/presentation/ShellUiPresenter.cs`
  - `tests/integration/session/ShellUiProgram.cs`
- **Related systems**: Platform Session Shell, Shell UI, manual QA runtime smoke
- **Possible root cause**: The mounted `ShellUi.tscn` defaulted to a visible Loading panel while dynamic runtime binding from `ShellUiPresenter` into live Godot controls was not wired. The existing `SessionShell.RunBootChain()` method is empty, so no runtime code changed the visible panel after startup.

## Evidence

- **Visual**: User screenshot shows `Loading Session Shell`, `Checking game data...`, and `Cancel Esc` remaining visible in a Godot DEBUG window.
- **Automated context**: Prior headless smoke could load `SessionShell.tscn`, but did not verify visible panel transition.

## Fix Candidate

Applied 2026-05-13:

- `src/scenes/ShellUi.tscn`: default visible state now shows Entry instead of Loading and hides unavailable Continue/Locked/New Session controls.
- `tests/integration/session/ShellUiProgram.cs`: added regression coverage that the static scene default cannot strand visible runtime on Loading.

Verification completed:

- `dotnet run --no-build --project tests/integration/session/ShellUiTest.csproj` - PASS, 9/9
- `dotnet build CloudWeaverVoyage.sln --no-restore` - PASS
- Godot headless load of `res://src/scenes/SessionShell.tscn` - PASS

Manual verification:

- 2026-05-13 user relaunch showed the `Cloud Weaver Voyage` Entry screen instead of the Loading screen.

## Related Issues

- `production/qa/qa-cases-resources-goods-capacity-2026-05-13.md` - TC-RGC-001
- `production/qa/smoke-2026-05-13.md` - visible runtime checks were deferred to manual QA

## Notes

This is not a ResourcesManager story acceptance failure. It blocks manual QA startup and should be verified before QA sign-off advances.

## Verification Record

**Verified**: 2026-05-13
**Result**: VERIFIED FIXED
**Method**: User visible relaunch screenshot reached Entry screen.
**Follow-up**: BUG-002 tracks the next issue: Entry buttons were visible but not interactive.
