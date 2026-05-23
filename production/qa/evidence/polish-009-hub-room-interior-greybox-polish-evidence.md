# Polish Story 009 Evidence: Hub Room Interior Greybox Polish

> Date: 2026-05-23
> Story: `production/polish-backlog/story-polish-009-hub-room-interior-greybox-polish.md`
> Verdict: PASS WITH CONDITIONS

## Evidence Summary

- `HubRuntime.cs` now extends the Hub greybox from room volumes into readable interior details: cockpit window/navigation slate, cargo shelves/load track/fill, engine coils/conduit/wear overlay, interior dividers, and a shared aisle.
- Cockpit, cargo, and engine room status labels are presentation-only and derive from existing route progress, reward/cargo, and hull values.
- Cargo load fill starts hidden while empty and grows after returned cargo.
- Engine wear overlay starts hidden at full hull and appears after the playable loop applies hull pressure.
- Runtime authority remains C# / Godot .NET; no GDScript runtime authority or parallel room state model was introduced.

## Automated Checks

- `dotnet restore CloudWeaverVoyage.sln`
  - PASS.
  - Required because several test projects were missing `project.assets.json` before the first `--no-restore` build.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  - PASS with 107 warnings, 0 errors.
  - Warnings are the existing broad-solution Godot source-generator/test nullability warnings observed during the build.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
  - PASS.
  - Covers new Hub cockpit/cargo/engine interior details, hidden initial cargo/damage indicators, returned cargo fill, hull damage overlay, dynamic room status labels, and the existing Hub -> Chart -> Exploration -> Save/Load -> Return loop.
  - Also covers the latest save-slot UX feedback path from Polish Story 008 (`canonical progress and local durable progress`).
  - Runtime screenshots were skipped because the headless display driver cannot provide them.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`
  - PASS.
  - Frame avg/p95/worst: 5.504 / 6.895 / 15.465 ms.
  - Peak static memory: 49.922 MiB.
  - Save p50/p95/max: 6.903 / 11.817 / 11.817 ms.
  - Load p50/p95/max: 6.897 / 9.123 / 9.123 ms.
  - Route departure: 15.779 ms.
  - Return Hub: 13.804 ms.
- `git diff --check`
  - PASS with LF/CRLF conversion warnings only.

## Remaining Conditions

- This is still greybox readability, not final room art.
- Room interiors are semantic presentation details, not final authored props, collisions, navigation mesh, or audio ambience.
- Save-slot UX, backup/quarantine UI, long-play QA, and Release readiness remain downstream.
