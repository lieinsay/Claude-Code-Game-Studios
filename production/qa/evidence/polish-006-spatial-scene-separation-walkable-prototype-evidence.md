# Polish Story 006 Evidence: Spatial Scene Separation and Walkable Prototype

> Date: 2026-05-22  
> Story: `production/polish-backlog/story-polish-006-spatial-scene-separation-walkable-prototype.md`  
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- Human QA from `production/playtests/playtest-checklist-polish-route-search-loop-2026-05-22.md` identified that UI and scene felt mixed together and that the scene read like a static image.
- `HubRuntime.cs` now separates scene art from interaction/player markers with `WorldSceneLayer` and `WorldInteractionLayer`.
- Hub and Exploration now expose separate walkable bounds, and player/debug positions are clamped to the current scene's bounds.
- Hub spatial anchors now include island boundary, ship hull, boarding ramp, cockpit room, cargo room, and engine room.
- Exploration spatial anchors now include island boundary, docked ship, boarding ramp, and island path.
- Runtime authority remains C# / Godot .NET; no GDScript runtime authority was introduced.

## Automated Checks

- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 5 existing warnings, 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers layer separation, Hub walkable bounds, Hub ship/room anchors, Exploration walkable bounds, Exploration dock/path anchors, and the existing domain-backed route/search/save/load loop.
  - First run timed out due to a leftover Godot process after build; stale process was stopped and rerun passed.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.660 / 6.891 / 11.767 ms.
  - Peak static memory: 56.317 MiB.
  - Save p50/p95/max: 6.897 / 20.510 / 20.510 ms.
  - Load p50/p95/max: 6.910 / 7.470 / 7.470 ms.
  - Route departure: 16.925 ms.
  - Return Hub: 13.819 ms.
- `git diff --check`
  - PASS with CRLF warnings only.

## Remaining Conditions

- This is a greybox spatial prototype, not final art/audio.
- The ship rooms are volumes/anchors, not full room interiors.
- Cross-launch persistence trust remains a separate QA/design risk from the human checklist.
- No Release readiness claim is made by this evidence.
