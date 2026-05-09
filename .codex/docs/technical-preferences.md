# Technical Preferences

<!-- Platform/language pivot recorded 2026-05-09 by ADR-0019. -->

## Engine & Language

- **Engine**: Godot 4.6.2 .NET
- **Language**: C# (.NET, primary)
- **Rendering**: Desktop 2D. Start with Compatibility renderer for prototype parity; evaluate Forward+ after the C# Foundation spike.
- **Physics**: Godot 2D physics for MVP; Jolt 3D default is not relevant unless 3D systems are introduced later.

## Input & Platform

- **Target Platforms**: Windows desktop (primary); Linux desktop (secondary after first stable desktop build)
- **Input Methods**: Keyboard/Mouse
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: None (MVP)
- **Touch Support**: None (MVP)
- **Platform Notes**: Web is no longer an MVP target. Remove browser-only assumptions such as AudioContext activation, IndexedDB persistence, `pagehide`, `visibilitychange`, JavaScriptBridge lifecycle callbacks, WebGL 2 limits, and single-threaded Web export constraints from new implementation work. Desktop lifecycle is handled by SessionShell focus, pause, quit, and save boundaries.

## Naming Conventions

- **Classes**: PascalCase (e.g., `InteractionRegistry`, `SnapshotPackage`)
- **Namespaces**: PascalCase rooted at `CloudWeaverVoyage` (e.g., `CloudWeaverVoyage.Core`)
- **Public Members**: PascalCase (e.g., `RouteId`, `CaptureSnapshot`)
- **Private Fields**: `_camelCase` (e.g., `_routeId`, `_hullBandIntegrity`)
- **Locals/Parameters**: camelCase (e.g., `routeId`, `hullBandIntegrity`)
- **Signals/Events**: Godot signal delegates use PascalCase event names with `{Noun}{VerbPast}` (e.g., `DepositCommitted`, `RouteCommitted`); emitted semantic event IDs remain snake_case where data-driven content requires stable IDs.
- **Files**: PascalCase matching class names for C# (e.g., `InteractionRegistry.cs`, `SnapshotPackage.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `AirshipHub.tscn`, `ExplorationScene.tscn`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE only when matching existing data IDs; prefer C# enum values for state names.
- **Autoload names**: PascalCase matching class name (e.g., `Registry`, `Persistence`, `Intel`)

## Performance Budgets

- **Target Framerate**: 60fps (16.67ms frame budget)
- **Frame Budget**: 16ms gameplay frame budget
- **Draw Calls**: ≤ 400 for MVP desktop 2D scenes until profiling says otherwise
- **Memory Ceiling**: 512MB MVP desktop soft ceiling; Autoload pool ≤20MB; AirshipHub scene ≤60MB; Peak memory (Exploration active) ≤200MB

### Sub-budgets

- **Desktop boot time**: <2s from `boot_requested` to `session_ready` on a warm local build
- **Autoload `_Ready()`**: <100ms total across all Autoloads
- **Save/load**: 2MB snapshot, p95 <50ms encode+SHA-256; max 100ms
- **Signal emit**: single emit <0.01ms (consumer count ≤5)
- **Scene transition**: <500ms (exit cleanup + instantiate + `_Ready()`)

## Testing

- **Framework**: C# validation path required for new systems. Existing GdUnit4/GDScript tests remain temporary regression evidence until C# parity tests replace them.
- **Build Check**: `dotnet build` must pass once the Godot .NET project files exist.
- **Minimum Coverage**: Logic stories require automated unit tests or C# headless validation; Integration stories require integration test or documented playtest.
- **Required Tests**: Balance formulas, gameplay systems, signal contracts, state machine transitions, save/load roundtrip.

## Forbidden Patterns

- `dictionary_signal_payload` — signal payload must be typed parameters, not raw dictionaries, unless an ADR explicitly defines a JSON-like cross-system package.
- `untyped_signal_param` — all signal parameters must have explicit type annotations.
- `string_signal_connect` — use typed C# signal/event patterns, not string-based connection names.
- `deferred_emit` — do not defer cross-system signals unless an ADR explicitly requires next-frame ordering.
- `process_connect` — no dynamic connect/disconnect in `_Process()`/`_PhysicsProcess()`.
- `store_var_save` — no Variant blob save data; use Canonical JSON.
- `hardcoded_value` — gameplay values come from Registry/data definitions, not literals inside gameplay logic.
- `direct_cross_autoload_in_ready` — no calling other Autoload methods in `_Ready()`.
- `bare_object_payload` — no Node/Resource/Object/Callable references in persistence payloads.
- `signal_cascade_depth_3plus` — max signal cascade depth = 2.
- `web_lifecycle_requirement` — do not require browser lifecycle behavior for MVP desktop stories.

## Allowed Libraries / Addons

- Godot .NET runtime and .NET SDK required for C#.
- gdUnit4 may remain during migration for legacy GDScript regression checks.
- No new third-party runtime dependencies for MVP without an explicit ADR/request.

## Architecture Decisions Log

| ADR | System | Status |
|-----|--------|--------|
| ADR-0001 | Autoload/Scene Boot Order | Accepted |
| ADR-0002 | Signal Communication Protocol | Accepted |
| ADR-0003 | Save System / JSON Serialization | Accepted |
| ADR-0004 | InteractionHandler Base | Accepted |
| ADR-0005 | Resource Pool System | Accepted |
| ADR-0006 | Web Platform Constraints | Superseded for active MVP by ADR-0019 |
| ADR-0007 | Intel / Knowledge System | Accepted |
| ADR-0008 | Chart Route State Machine | Accepted |
| ADR-0009 | Module / Hull System | Accepted |
| ADR-0010 | EncounterContext Type | Accepted |
| ADR-0011 | WorldRepair State Machine | Accepted |
| ADR-0012 | UI / Input Routing | Accepted |
| ADR-0019 | Desktop C# Platform Pivot | Accepted |

## Engine Specialists

- **Primary**: godot-specialist
- **Language/Code Specialist**: godot-csharp-specialist
- **Shader Specialist**: godot-shader-specialist
- **UI Specialist**: godot-specialist + godot-csharp-specialist for C# UI scripts
- **Additional Specialists**: godot-gdextension-specialist only if native plugins become necessary after profiling
- **Routing Notes**: All new game code is C# by default. Route implementation/review to `godot-csharp-specialist`; route scene-tree and Godot node architecture questions to `godot-specialist`. Prefer C# before considering GDExtension; escalate to native only with profiling evidence.

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs) | godot-csharp-specialist |
| Legacy prototype code (.gd) | godot-gdscript-specialist for migration review only |
| Shader / material files (.gdshader, .tres) | godot-shader-specialist |
| UI / screen files (.tscn with Control root) | godot-specialist |
| Scene / prefab / level files (.tscn) | godot-specialist |
| Native extension / plugin files | godot-gdextension-specialist |
| General architecture review | technical-director |
