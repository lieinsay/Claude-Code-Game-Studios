# QA Plan: Sprint 003 Domain-Backed Playable Slice

Date: 2026-05-17
Stage: Production
Sprint: `production/sprints/sprint-003-domain-backed-playable-slice.md`
Adapter boundary: `production/sprints/sprint-003-runtime-adapter-boundary.md`

## Verdict Scope

This QA plan validates Sprint 003 only. Passing this plan can support a future
Production -> Polish recheck, but it does not by itself authorize a Polish PASS.

## QA Objective

Prove that the playable Hub -> Chart -> Exploration -> Return route is no longer
only a smoke-state demo. The route must remain human-playable while Chart,
Exploration, Resources/Hull, Feedback, and save/load evidence comes from C#
domain managers or documented adapter wrappers around those managers.

## Entry Criteria

- PVS3-001 runtime adapter boundary is complete.
- `HubRuntime.tscn` loads a C# scene script or equivalent Godot-to-C# bridge for
  the running runtime scene.
- The Sprint 002 smoke path is not regressed.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
  passes before domain integration begins.

## Test Areas

### QA-1: Runtime Bridge Presence

Evidence required:

- The running Godot scene exposes a runtime adapter or bridge object.
- The bridge can return a domain snapshot with at least:
  - selected route id/name,
  - exploration phase/progress,
  - cargo/storage usage,
  - hull integrity/readiness,
  - last save/load result.
- Smoke tests assert this snapshot rather than only rendered label strings.

Fail condition:

- `HubRuntime.cs` or any other Godot scene script remains the only authority for route, exploration step,
  resources, hull, and save/load state.

### QA-2: Chart and Departure

Evidence required:

- Opening the helm route calls `ChartManager.OpenChart`.
- Selecting a route calls `ChartManager.SelectRoute`.
- Confirming departure flows through `ChartManager` and `HubManager`, then enters
  exploration through the adapter's navigation/exploration contract.
- The Godot route label is refreshed from manager display data.

Fail condition:

- `_selected_route` is the only route/departure state used by the running scene.

### QA-3: Exploration Search and Domain Mutation

Evidence required:

- Searching calls `ExplorationManager.PerformSearch` or a documented adapter
  method that delegates into it.
- Loot/resource changes are visible through `ResourcesManager` snapshots.
- Hull/threat feedback is either produced by `ExplorationManager` delegates and
  `ModuleHullManager` or explicitly marked unavailable with a blocking risk.
- Returning to Hub refreshes cargo, storage, route pressure, and hull summaries
  from manager snapshots.

Fail condition:

- Exploration labels are updated only by `_exploration_step` branches.

### QA-4: Canonical Persistence

Evidence required:

- Ctrl+S / Save calls `Persistence.RequestSaveProgress`.
- Ctrl+L / Load calls `Persistence.RequestLoadProgress`.
- `ResourcesManager` and `ModuleHullManager` are registered with Persistence.
- Chart and Exploration have adapter-owned `SnapshotPackage` wrappers if they do
  not expose direct registration methods.
- The smoke probe mutates state after saving, loads, and verifies domain state is
  restored.

Fail condition:

- Final gate evidence depends on `user://smoke_session_state.json`.

### QA-5: Greybox Presentation

Evidence required:

- Hub and Exploration screens read as spatial game surfaces, not only panels.
- Interaction markers, player marker, prompt placement, and feedback labels do
  not overlap at 1280x720.
- The route remains playable with keyboard movement and E-use interactions.

Fail condition:

- The playable route is only button/panel driven or text-only without spatial
  affordances.

## Required Automated Verification

- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
- Relevant focused C# tests for any new adapter/core code
- `git diff --check`

## Required Manual Verification

Manual tester must complete:

1. Start game from `SessionShell`.
2. Move around Hub using WASD/arrow keys.
3. Approach helm and press E.
4. Select a route and depart.
5. Move to search point and press E.
6. Confirm resource/threat/hull feedback changes.
7. Save, mutate state, load, and confirm restored domain-backed state.
8. Return to Hub and confirm summaries sync.

## Exit Criteria

Sprint 003 QA can sign off only when:

- PVS3-001 through PVS3-007 are complete.
- Automated smoke proves domain-backed state mutation and canonical save/load.
- Human playtest completes the same path without debug-only calls.
- Remaining risks are documented in the QA sign-off and gate recheck.

## Known Risk Before Implementation

The current running scene is GDScript-first. Existing C# Godot node scripts are
not the active runtime path, so PVS3-002 must first establish a real bridge
surface for `HubRuntime` before claiming domain-backed playability.
