# Sprint 003 Domain-Backed Playable Smoke Evidence

Date: 2026-05-17
Stage: Production
Sprint: `production/sprints/sprint-003-domain-backed-playable-slice.md`
Story: PVS3-006 -- Domain-backed playable smoke probe

## Verdict

**PASS for PVS3-006 automated smoke evidence.**

This evidence supports the current Production recovery sprint only. It does not
authorize a Production -> Polish PASS by itself because PVS3-007 manual playtest
and QA sign-off remain open.

## Automated Commands

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 30/30 adapter checks pass. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Covers movement, spatial E-use, Chart departure, Exploration search, canonical save/load restore, return-to-Hub sync, and greybox visibility. |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 5 existing warnings, 0 errors. |
| `git diff --check` | PASS | Only existing LF/CRLF warnings reported. |

## QA Area Mapping

### QA-1 Runtime Bridge Presence

- `src/scenes/HubRuntime.tscn` attaches `src/scenes/HubRuntime.cs`.
- `src/scenes/HubRuntime.cs` owns Godot input, presentation nodes, spatial
  prompts, and debug harness calls.
- `src/presentation/PlayableSliceDomainAdapter.cs` exposes domain snapshots used
  by smoke assertions rather than relying only on rendered labels.

Evidence:

- `tests/smoke/session_shell_visual_probe.gd` calls `DebugDomainSnapshot()` and
  asserts Chart, Hub, Resources, Hull, and Persistence snapshot values.

### QA-2 Chart and Departure

- Opening the helm route flows through the adapter into `ChartManager`.
- Route selection commits `route.mist`.
- Departure commits the Chart route and updates Hub transit state through
  `HubManager`.

Evidence:

- Godot smoke asserts selected route data and in-transit domain snapshot values.
- `DomainAdapterTest.csproj` verifies route opening, route selection, departure
  commit, and Hub in-transit state outside Godot.

### QA-3 Exploration Search and Domain Mutation

- Exploration search mutates domain-backed resource and hull state through the
  playable slice adapter.
- Search consumes basic supply, carries beacon crystal rewards, applies hull
  pressure, and extracts carried rewards to storage when returning to Hub.

Evidence:

- Godot smoke asserts basic supply decreases, carried rewards increase, hull
  integrity changes, and return-to-Hub storage sync occurs.
- `DomainAdapterTest.csproj` verifies the same resource/hull mutations in the
  headless adapter path.

### QA-4 Canonical Persistence

- Ctrl+S / save now calls `PlayableSliceDomainAdapter.SaveSceneState`.
- Ctrl+L / load now calls `PlayableSliceDomainAdapter.LoadSceneState`.
- The adapter registers canonical progress domains:
  `progress.resources`, `progress.airship.modules_hull`, `progress.airship`,
  `progress.routes`, and `progress.playable_slice`.

Evidence:

- Godot smoke saves after search, mutates by returning to Hub, loads, then
  verifies restored exploration screen, carried rewards, storage, and
  `last_load_status`.
- `DomainAdapterTest.csproj` verifies save/load restoration through the C#
  `Persistence` pipeline.

### QA-5 Greybox Presentation

- Hub mode exposes authored greybox deck, helm, storage, and module bench props.
- Exploration mode exposes sky field, route trail, search wreck, and return
  beacon props.
- Smoke assertions verify the Hub and Exploration prop sets switch correctly
  across departure and return.

Evidence:

- `tests/smoke/session_shell_visual_probe.gd` calls `DebugNodeVisible(...)` for
  Hub and Exploration greybox nodes.

## Residual Risks

- PVS3-007 manual playtest and QA sign-off were completed after this automated
  evidence package; together they support the passed Production -> Polish gate
  recheck.
- Exploration search currently uses a documented adapter fixture around the
  route pressure/reward event; broader `NavigationManager` /
  `ExplorationManager` runtime contract coverage remains a Production follow-up.
- Headless screenshot capture is still skipped under the current display driver,
  so visual evidence is asserted by node visibility and layout state instead of
  captured image pixels.
- The solution build still reports 5 existing warnings in older test runners;
  they are not introduced by Sprint 003 PVS3-006.
