# Technical Preferences

<!-- Engine configured 2026-05-05. Values sourced from architecture.md, VERSION.md, and ADRs. -->

## Engine & Language

- **Engine**: Godot 4.6.2
- **Language**: GDScript
- **Rendering**: Compatibility renderer (WebGL 2 — Web-first target)
- **Physics**: Godot 2D physics (Jolt default in 4.6, not used for MVP)

## Input & Platform

- **Target Platforms**: Web desktop browsers (primary); Windows/Linux desktop (secondary)
- **Input Methods**: Keyboard/Mouse
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: None (MVP)
- **Touch Support**: None (MVP)
- **Platform Notes**: Single-threaded Web export (no SharedArrayBuffer requirement); AudioContext activation requires user gesture; `pagehide`/`visibilitychange` best-effort save ≤20ms budget; IndexedDB storage via `user://` mapping; WebGL 2 Compatibility renderer constraints apply to all rendering decisions.

## Naming Conventions

- **Classes**: PascalCase (e.g., `InteractionRegistry`, `SnapshotPackage`)
- **Variables**: snake_case (e.g., `route_id`, `hull_band_integrity`)
- **Signals/Events**: `{noun}_{verb_past}` (e.g., `deposit_committed`, `route_committed`)
- **Files**: snake_case (e.g., `interaction_registry.gd`, `snapshot_package.gd`)
- **Scenes/Prefabs**: PascalCase (e.g., `AirshipHub.tscn`, `ExplorationScene.tscn`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `DOMAIN_READY`, `KNOWLEDGE_UNREVEALED`)
- **Autoload names**: PascalCase matching class name (e.g., `Registry`, `Persistence`, `Intel`)

## Performance Budgets

- **Target Framerate**: 60fps (16.67ms frame budget)
- **Frame Budget**: 16ms (game logic headroom after engine overhead ~12ms)
- **Draw Calls**: ≤ 200 (WebGL 2 Compatibility renderer)
- **Memory Ceiling**: 200MB total heap (Web browser tab); Autoload pool ≤10MB; AirshipHub scene ≤30MB; Peak memory (Exploration active) ≤100MB

### Sub-budgets

- **Web boot time**: <2s from `boot_requested` to `session_ready` (warm cache)
- **Autoload _ready()**: <100ms total across all 9 Autoloads
- **Save/load**: 2MB snapshot, p95 <50ms encode+SHA-256; max 100ms
- **Signal emit**: single emit <0.01ms (consumer count ≤5)
- **Scene transition**: <500ms (exit cleanup + instantiate + _ready())

## Testing

- **Framework**: GUT (Godot Unit Test) — gdUnit4
- **Minimum Coverage**: Logic stories require automated unit tests (BLOCKING gate); Integration stories require integration test or documented playtest
- **Required Tests**: Balance formulas, gameplay systems, signal contracts, state machine transitions, save/load roundtrip

## Forbidden Patterns

- `dictionary_signal_payload` — signal payload must be typed parameters, not Dictionary
- `untyped_signal_param` — all signal parameters must have explicit type annotations
- `string_signal_connect` — use `sender.signal_name.connect(receiver.method)` not `connect("name", ...)`
- `deferred_emit` — use synchronous `.emit()` not `.emit.call_deferred()` for cross-system signals
- `process_connect` — no dynamic connect/disconnect in `_process()`/`_physics_process()`
- `store_var_save` — no `store_var()`/`get_var()` Variant blob for save data (use Canonical JSON)
- `hardcoded_value` — all gameplay values must come from Registry (data-driven)
- `direct_cross_autoload_in_ready` — no calling other Autoload methods in `_ready()`
- `bare_dictionary_payload` — no Node/Resource/Object/Callable references in signal payload
- `signal_cascade_depth_3plus` — max signal cascade depth = 2

## Allowed Libraries / Addons

- gdUnit4 (testing)
- GDScript built-in only (no external addons for MVP)

## Architecture Decisions Log

| ADR | System | Status |
|-----|--------|--------|
| ADR-0001 | Autoload/Scene Boot Order | Accepted |
| ADR-0002 | Signal Communication Protocol | Accepted |
| ADR-0003 | Save System / JSON Serialization | Accepted |
| ADR-0004 | InteractionHandler @abstract | Accepted |
| ADR-0005 | Resource Pool System | Accepted |
| ADR-0006 | Web Platform Constraints | Accepted |
| ADR-0007 | Intel / Knowledge System | Accepted |
| ADR-0008 | Chart Route State Machine | Accepted |
| ADR-0009 | Module / Hull System | Accepted |
| ADR-0010 | EncounterContext Type | Accepted |
| ADR-0011 | WorldRepair State Machine | Accepted |
| ADR-0012 | UI / Input Routing | Accepted |

## Engine Specialists

- **Primary**: godot-specialist
- **Language/Code Specialist**: godot-gdscript-specialist
- **Shader Specialist**: godot-shader-specialist
- **UI Specialist**: godot-specialist (GDscript UI via Control nodes)
- **Additional Specialists**: N/A (MVP — no GDExtension, no C#, no networking)
- **Routing Notes**: All game code is GDScript — route to godot-gdscript-specialist for code review and implementation. Route rendering/shader questions to godot-shader-specialist. Use godot-specialist for general engine questions and scene architecture.

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.gd) | godot-gdscript-specialist |
| Shader / material files (.gdshader, .tres) | godot-shader-specialist |
| UI / screen files (.tscn with Control root) | godot-specialist |
| Scene / prefab / level files (.tscn) | godot-specialist |
| Native extension / plugin files | N/A (MVP — no GDExtension) |
| General architecture review | technical-director |
