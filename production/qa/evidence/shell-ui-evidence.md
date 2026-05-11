# Shell UI Evidence — Platform Session Shell Story 007

> Date: 2026-05-11
> Evidence Type: UI model walkthrough + Godot Control scene verification + screenshot evidence
> Story: `production/epics/platform-session-shell/story-007-shell-ui.md`

## Scope

This evidence verifies the shell UI contract as a C# presenter model and a concrete Godot `Control` scene. `ShellUiPresenter` produces the entry, loading, audio activation, resume, recovery, fatal, and ephemeral warning screens with keyboard-reachable actions. `src/scenes/ShellUi.tscn` provides the CanvasLayer/Control node hierarchy used by the runtime `SessionShell` scene.

## Walkthrough

| AC | Evidence |
| --- | --- |
| AC-1 | `ContinueAvailability.Hidden` renders the Entry screen with Start and Settings only; no selectable Continue action is emitted. |
| AC-2 | `ContinueAvailability.PreservedLocked` renders a disabled Continue action with the persistence-owned lock reason plus New Session and Return Title. |
| AC-3 | Ephemeral storage renders a no-save warning model before start confirmation with Continue Without Saving and Return actions. |
| AC-4 | `FatalBlocked` renders only safe Retry, Return Title, and Error Details actions; no gameplay entry action is emitted. |
| AC-5 | `RecoveryRequired` renders Retry, New Session, Return Title, and Error Details. |
| AC-6 | Ready, AwaitingAudioActivation, ResumePending, RecoveryRequired, and FatalBlocked all expose shortcuts for every action. |
| AC-7 | Loading renders a nonblank loading model with `LoadPhase` and progress. |
| AC-8 | PreservedLocked Continue includes lock reason, Return Title, and New Session. |
| Scene | `ShellUi.tscn` contains concrete panels and button nodes for Loading, Entry, AudioActivation, EphemeralWarning, Resume, Recovery, and Fatal. |

## Verification

- `dotnet run --project tests/integration/session/ShellUiTest.csproj --no-restore`
- Expected result: `Story 007 validation passed: 8/8 checks passed.`
- `godot --headless --scene res://src/scenes/SessionShell.tscn --quit-after 2`
- `godot --display-driver windows --rendering-driver opengl3 --audio-driver Dummy --scene res://src/scenes/ShellUi.tscn --quit-after 2`
- Screenshot capture: `godot --display-driver windows --rendering-driver opengl3 --audio-driver Dummy --scene res://src/scenes/ShellUi.tscn --write-movie production/qa/evidence/shell-ui-loading.png --quit-after 2`

## Screenshot Evidence

- Loading screen frame: `production/qa/evidence/shell-ui-loading00000000.png`

## Notes

- The FatalBlocked action uses desktop-safe retry/return/error-detail actions. The old web-specific "refresh page" wording is intentionally not emitted.
- Godot 4.6.2 headless + movie maker crashed on the dummy texture path during screenshot capture. The same scene captured successfully with the Windows display driver and Dummy audio. Headless scene loading itself passes.
