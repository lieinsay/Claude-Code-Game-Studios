# Bug Report

## Summary
**Title**: Polish 015 world scene is hidden behind the text dashboard
**ID**: BUG-003
**Severity**: S2-Major
**Priority**: P1-Immediate
**Status**: Closed
**Reported**: 2026-05-24
**Reporter**: liein
**Closed**: 2026-05-27

## Classification
- **Category**: Visual / Gameplay
- **System**: HubRuntime playable-space presentation
- **Frequency**: Always
- **Regression**: Yes - Polish 015 automated smoke reported scene nodes present, but human QA still sees a text-only dashboard.

## Environment
- **Build**: `80cf299`
- **Platform**: Windows desktop
- **Scene/Level**: `src/scenes/SessionShell.tscn` -> `HubRuntime`
- **Game State**: New session, focused Polish 015 manual QA route

## Reproduction Steps
**Preconditions**: Launch the project from the default scene in a normal Godot 4.6.2 .NET window.

1. Start a session from the entry screen.
2. Continue through audio activation into Hub.
3. Look for the island/dock scene and visible ship.
4. Try to identify a boarding ramp and enter the ship.
5. Try to distinguish cockpit/helm, cargo/storage, and engine/module spaces.

**Expected Result**: Hub reads as an island/dock with a visible docked ship; boarding leads into a readable ship interior with distinct spaces.
**Actual Result**: Tester sees mostly text. The island/dock is not discovered, boarding/interior reads as text-only, and downstream Chart/search/return checks are blocked.

## Technical Context
- **Likely affected files**:
  - `src/scenes/HubRuntime.tscn`
  - `src/scenes/HubRuntime.cs`
  - `tests/smoke/session_shell_visual_probe.gd`
- **Related systems**: Polish Story 015, playable slice smoke, Hub/Exploration runtime presentation
- **Possible root cause**: `HubRuntime.tscn` keeps the central `Deck` text dashboard as the dominant visual surface, while runtime-created scene nodes are too small and/or drawn behind the HUD. Existing smoke checks asserted node existence but not actual viewport dominance.

## Evidence
- **Manual QA**: `production/playtests/playtest-checklist-polish-015-island-ship-search-gameplay-2026-05-23.md`
- **Visual**: Tester reported "只有文字" and "没有真正的场景"; all checks after step 5 are blocked.

## Related Issues
- Polish Story 014 human QA concern: Hub/Exploration did not read as intended places.
- Polish Story 015 was created to resolve that blocker but did not pass human QA on build `80cf299`.

## Notes
Do not proceed to formal release checklist/gate until this bug is fixed and the focused Polish 015 human route is rerun.

## Fix Candidate
**Implemented**: 2026-05-24
**Status**: Superseded by opaque world-layer follow-up
**Change**: `HubRuntime` now draws the playable world layer above the text dashboard, adds large viewport-scale island/dock/ship silhouettes, adds large ship-interior cockpit/cargo/engine bays, and extends the visual smoke probe to assert main-viewport scene coverage rather than node existence only.
**Automated Evidence**:
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with existing warnings / 0 errors.
- Godot headless visual smoke PASS.
- Godot Windows-display visual smoke PASS and captured screenshots:
  - `production/qa/evidence/polish-015-fix-final-hub-probe.png`
  - `production/qa/evidence/polish-015-fix-exploration-semantics-probe.png`
- Godot durable persistence, long-session, and perf smoke PASS.

This first fix candidate was superseded by the opaque world-layer follow-up because semi-transparent world art still allowed the old Deck text to bleed through.

## Opaque World-Layer Follow-up
**Implemented**: 2026-05-27
**Status**: Verified / Closed
**Change**: The 2026-05-24 fix still allowed old Deck text to show through semi-transparent world art. `HubRuntime` now makes the primary world backdrops, island walk layers, ship hull/shell surfaces, and exploration return-ship silhouette opaque enough that the old dashboard no longer reads through the playable scene. The visual smoke probe now asserts those opacity contracts.
**Automated Evidence**:
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with existing warnings / 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` PASS.
- `godot --path . -s tests/smoke/session_shell_visual_probe.gd` PASS and captured screenshots:
  - `production/qa/evidence/polish-015-opaque-world-hub-probe.png`
  - `production/qa/evidence/polish-015-opaque-world-exploration-probe.png`

## Closure
**Closed**: 2026-05-27
**Human Verification**: User reran the focused route manually and confirmed the BUG-003 / Polish 015 blocker can be closed.
**Resolution**: Fixed.

Closure basis:

- The text-dashboard bleed-through no longer dominates Hub / ship / Exploration world surfaces.
- Smoke now asserts critical scene evidence opacity instead of only node existence.
- Build and Godot smoke pass after the follow-up.
- User accepted the manual rerun outcome and requested BUG-003 / Polish 015 closure.
