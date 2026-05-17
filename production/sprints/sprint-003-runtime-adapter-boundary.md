# Sprint 003 Runtime Adapter Boundary

Date: 2026-05-17
Stage: Production
Related sprint: `production/sprints/sprint-003-domain-backed-playable-slice.md`
Story: PVS3-001

## Purpose

Sprint 002 proved a human-playable greybox loop, but the loop was still driven by
`HubRuntime.gd` smoke state. Sprint 003 must move runtime authority into the
implemented C# domain managers while keeping the Godot scene as a thin playable
presentation and input shell.

This note defines the adapter boundary for the next implementation pass. It does
not advance the project to Polish.

## Boundary Decision

`HubRuntime.cs` remains responsible for:

- Godot node lookup, prompt visibility, labels, marker positions, and greybox
  layout.
- Player input sampling and spatial interaction checks for the current greybox
  scene.
- Calling a Godot-friendly adapter API and rendering the adapter snapshot.
- Debug helpers required by the smoke probe, as long as those helpers read
  domain-backed state.

The C# runtime adapter owns:

- Manager construction and fixture seeding for the vertical slice route.
- Cross-manager delegate wiring.
- Calls into domain methods for route selection, departure, search,
  resources/hull mutation, feedback routing, and save/load.
- Conversion between C# domain snapshots and Godot-friendly dictionaries or
  primitive values.

The adapter should be a thin C# node or bridge object, not a new gameplay system.
No new dependency is required.

## Authority Matrix

| Runtime concern | Authority | Adapter responsibility |
| --- | --- | --- |
| Hub docking, stations, departure context, arrival snapshot | `HubManager` | Expose current station/departure/arrival summary to Godot and route helm interactions through Hub/Chart calls. |
| Chart visibility, selected route, route confirmation | `ChartManager` | Seed the minimum route fixture, inject knowledge/traversal/docked-location delegates, and expose selected route/display data. |
| Voyage transition context | `NavigationManager` plus `ChartManager` route commit | Convert confirmed route into an encounter or fallback context for the greybox exploration scene. |
| Exploration phase, search yield, extraction, session restore | `ExplorationManager` | Call `EnterExplorationWithContext`, `PerformSearch`, extraction/return methods, and expose `SerializeExploration`-derived status. |
| Resources and cargo | `ResourcesManager` | Wire exploration loot into `Add`/pool delegates, expose cargo/storage usage, and register `progress.resources` persistence. |
| Modules and hull | `ModuleHullManager` | Expose departure readiness, hull integrity, scout efficiency, damage hooks, and register `progress.airship.modules_hull` persistence. |
| Canonical save/load | `Persistence` | Register all available domain serializers/deserializers and make Ctrl+S/Ctrl+L call `RequestSaveProgress` / `RequestLoadProgress`. |
| Feedback, save/load cues, route/search status | `FeedbackManager` plus `UIManager` semantic events where available | Route user-facing events through feedback requests, then surface selected status/cue text in Godot labels. |
| Greybox player marker and local collision affordances | Godot scene | Keep as presentation-only evidence; do not treat marker position or label text as canonical domain progress except for scene resume spawn hints. |

## Manager Wiring

The adapter should initialize the vertical slice in this order:

1. Create and initialize `Registry` content if the scene is not already supplied
   one by `SessionShell`.
2. Create `ResourcesManager`, `ModuleHullManager`, `HubManager`,
   `ChartManager`, `NavigationManager`, `ExplorationManager`, `UIManager`,
   `FeedbackManager`, and `Persistence`.
3. Seed the minimum route/search fixture needed for the playable slice.
4. Wire delegates:
   - `ChartManager` knowledge, traversal, and docked-location delegates.
   - `HubManager` route, cargo, module, repair, partner, and departure query
     delegates.
   - `ExplorationManager` loot, carried-stack, extraction, intel, hull damage,
     scout efficiency, and snapshot delegates.
   - `FeedbackManager.ConnectPersistenceEvents(persistence)` and
     `FeedbackManager.ConnectUiSemanticEvents(uiManager)` when UI events are
     used.
5. Register persistence serializers/deserializers:
   - `ResourcesManager.RegisterPersistence(persistence)`.
   - `ModuleHullManager.RegisterPersistence(persistence)`.
   - Adapter-owned wrappers for managers that expose payload dictionaries but
     not a direct `RegisterPersistence` method, including `ChartManager` via
     `BuildSnapshotPayload`/`RestoreFromSnapshot` and `ExplorationManager` via
     `SerializeExploration`/`DeserializeExploration`.
   - `HubManager.BuildSnapshotPackage` should be used if the slice needs hub
     arrival/departure state in the progress artifact.

## Playable Flow Contract

The next implementation pass should make this evidence path domain-backed:

1. Player moves to the helm in Godot and presses E.
2. Adapter opens/selects a route through `ChartManager`; `_selected_route` must
   become a display cache, not the source of truth.
3. Departure confirmation flows through `ChartManager` and `HubManager`, then
   enters exploration with `NavigationManager`/`ExplorationManager` context.
4. Player moves to a search point and presses E.
5. Search calls `ExplorationManager.PerformSearch`; loot/cargo feedback comes
   from `ResourcesManager`, and any threat/hull result uses `ExplorationManager`
   delegates plus `ModuleHullManager`.
6. Return to Hub calls extraction/arrival methods and refreshes Godot labels
   from manager snapshots.
7. Ctrl+S and Ctrl+L call `Persistence.RequestSaveProgress` and
   `Persistence.RequestLoadProgress`; gate evidence must not depend on
   `user://smoke_session_state.json`.

## Smoke Probe Expectations

`tests/smoke/session_shell_visual_probe.gd` can keep driving the same human path,
but its assertions should shift from smoke labels to adapter evidence:

- Route assertion reads the adapter/Chart selected route.
- Search assertion reads exploration/resource state after `PerformSearch`.
- Save/load assertion proves `Persistence` restored domain snapshots.
- Return assertion confirms Hub/Exploration summaries are refreshed from domain
  state.

Legacy debug methods may remain only as test harness conveniences around the
same domain-backed adapter calls.

## Open Risks

- Some managers are headless-first and not Godot-node-native. The adapter should
  expose a small Godot-friendly surface rather than modifying each manager for
  scene concerns.
- `ChartManager` and `ExplorationManager` expose serializable payload methods but
  may need adapter-owned `SnapshotPackage` wrappers for canonical persistence.
- Greybox player position remains presentation-local in Sprint 003 unless a
  later story deliberately promotes it into a domain navigation contract.
- This boundary resolves PVS3-001 only. PVS3-002 through PVS3-007 are still
  required before a Production -> Polish PASS can be considered.

## 2026-05-17 Implementation Update

- PVS3-002A migrated the runtime shell from `HubRuntime.gd` to
  `HubRuntime.cs`; the scene now attaches the C# script directly.
- `PlayableSliceDomainAdapter` currently wires the minimum playable slice to
  `ChartManager`, `HubManager`, `ResourcesManager`, and `ModuleHullManager`.
- GDScript runtime authority has been removed for this scene, but the boundary
  remains presentation-only for Godot: canonical persistence and richer
  `ExplorationManager` / `NavigationManager` integration are still Sprint 003
  follow-up work.
