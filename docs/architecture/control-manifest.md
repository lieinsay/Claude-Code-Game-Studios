# Control Manifest

> **Engine**: Godot 4.6.2 .NET + C#
> **Last Updated**: 2026-05-09
> **Manifest Version**: 2026-05-09
> **ADRs Covered**: ADR-0001, ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010, ADR-0011, ADR-0012, ADR-0019
> **Status**: Active — regenerate with `/create-control-manifest update` when ADRs change

This manifest is a programmer's quick-reference extracted from all Accepted ADRs,
technical preferences, and engine reference docs. For the reasoning behind each
rule, see the referenced ADR.

---

## Foundation Layer Rules

*Applies to: scene management, event architecture, save/load, engine initialisation, input framework*

### Required Patterns

- **Autoload `_ready()` must only do constant init, signal declarations, and null checks** — no file I/O, scene instantiation, audio playback, coroutine launch, or cross-Autoload method calls. Source: ADR-0001
- **All cross-system communication uses Godot typed signals/events with explicit parameter types** — use C# `[Signal]` delegate patterns for new code. Source: ADR-0002, ADR-0019
- **Signal naming must follow `{noun}_{verb_past}` convention** — e.g., `deposit_committed`, `route_committed`. Failure pairs use `_failed` suffix. Source: ADR-0002
- **All cross-system signal connections established during boot phases (Phase 3-7)** — never during `_process()` or `_physics_process()`. Source: ADR-0001, ADR-0002
- **Read queries use direct method calls; state mutations use signals** — query = return value needed; notify = fire-and-forget. Source: ADR-0002
- **All signal emit uses synchronous `.emit()`** — no `.emit.call_deferred()` for cross-system signals. Source: ADR-0002
- **Signal connections use typed C# event/signal patterns** — no string-based `connect("name", ...)`. Source: ADR-0002, ADR-0019
- **Save/load uses Canonical JSON with sorted keys, NFC normalization, finite IEEE 754 floats only** — no `store_var()`/`get_var()` Variant blobs as authoritative format. Source: ADR-0003
- **Save workflow must follow Staging → Verify → Promotion** — staging write → readback + checksum verify → atomic rename promotion. Old Safe preserved on any failure. Source: ADR-0003
- **Domain systems export state via `SnapshotPackage` with typed fields** — Persistence orchestrates but never interprets domain payload. Source: ADR-0003
- **`settings` and `progress` artifacts must be independent** — each with own manifest, generation, checksum, and backup. One corrupt artifact must not block the other. Source: ADR-0003
- **All interactable objects must inherit the C# `Interactable` base class and implement `HandleUse()`** — returning `UseResult` enum (ACCEPTED / REJECTED / BUSY). Source: ADR-0004, ADR-0019
- **Every enterable 2D scene with gameplay-relevant physical units must provide a Scene Physics Contract before implementation readiness** — declare horizontal/vertical scene type, movement plane, collision semantics, occlusion/layering, unit scale, special surfaces, dynamic physical behavior tags, conflict priority, and recovery rules. Source: GDD #20, GDD #19
- **Physical world exploration is a bottom-layer gameplay contract, not presentation polish** — scene obstacles, pushables, special surfaces, height/shadow cues, and dynamic physical behaviors must be authored and QA-verified when they shape pathing, search, threat, return, repair, or market interaction. Source: GDD #20, GDD #11, GDD #19
- **InteractionRegistry manages focus state machine + candidate pool + Use Gate + dispatch** — scene interactables register/unregister themselves; Registry owns the 5-state focus machine. Source: ADR-0004
- **Use dispatch is dual-channel**: `interaction_used` signal (fire-and-forget feedback) + `handle_use()` method call (request-response for domain logic). Source: ADR-0004
- **Desktop-first: all new game code is C# unless an ADR grants an exception** — GDScript remains temporary prototype/migration evidence only. Source: ADR-0019
- **Desktop lifecycle events must be handled by SessionShell** — focus loss, pause, quit, error, and save boundaries replace browser lifecycle callbacks. Source: ADR-0019
- **Godot .NET project files are source artifacts** — `.csproj` and `.sln` must be tracked once generated. Source: ADR-0019
- **Web export is not an MVP target** — do not add new requirements that depend on WebGL 2, IndexedDB, AudioContext, or JavaScriptBridge. Source: ADR-0019

### Forbidden Approaches

- **Never call other Autoload methods in `_ready()`** — only the Autoload declared earlier in `project.godot` order is safe. Real init deferred to `on_[phase]_ready` signal handlers. Source: ADR-0001
- **Never use Dictionary as signal payload** — all signal parameters must be individually typed. Source: ADR-0002
- **Never use untyped signal parameters** — every parameter must have explicit type annotation. Source: ADR-0002
- **Never use string-based signal connect** — `connect("signal_name", ...)` is deprecated since Godot 4.0. Source: ADR-0002
- **Never use deferred emit for cross-system signals** — `.emit.call_deferred()` breaks execution order predictability. Source: ADR-0002
- **Never dynamically connect/disconnect signals in `_process()` or `_physics_process()`** — always connect, gate in handler. Source: ADR-0002
- **Never use `store_var()`/`get_var()` as authoritative save format** — Variant blob encoding is non-deterministic. Source: ADR-0003
- **Never include `Node`, `Resource`, `Object`, `Callable`, or `RID` references in snapshot payload** — use String stable IDs. Source: ADR-0003
- **Never bypass `Interactable` base class** — all interactable objects must inherit from it. Source: ADR-0004
- **Never infer gameplay collision from art alone** — collision, pushability, soft overlap, one-way passage, special surface behavior, and dynamic physical behavior must come from the Scene Physics Contract. Source: GDD #20
- **Never introduce browser-only lifecycle requirements for MVP desktop** — no `pagehide`, `visibilitychange`, `beforeunload`, or JavaScriptBridge lifecycle dependency. Source: ADR-0019
- **Never treat C# files as Web export blockers in active MVP work** — Web export is superseded by desktop C# targeting. Source: ADR-0019

### Performance Guardrails

- **Autoload `_ready()` total**: <100ms across all 9 Autoloads — source: ADR-0001
- **Desktop boot time**: <2s from `boot_requested` to `session_ready` (warm local build); <5s hard cap — source: ADR-0001, ADR-0019
- **Save encode + SHA-256**: p95 <50ms for 2MB snapshot; max 100ms — source: ADR-0003
- **Signal emit**: single emit <0.01ms (consumer count ≤5); 200 connections at boot <1ms — source: ADR-0002
- **Scene transition**: <500ms (exit cleanup + instantiate + `_ready()`) — source: ADR-0001
- **MVP desktop memory soft ceiling**: <=512MB peak — source: ADR-0019

---

## Core Layer Rules

*Applies to: core gameplay loop, resources, knowledge/intel, chart/route planning, modules/hull, main player systems*

### Required Patterns

- **ResourcesManager (Autoload #5) is the single source of truth for all resource state** — 6 pools stored as `Dictionary[StringName, Dictionary]`. All operations return typed `ResourceResult` enum. Source: ADR-0005
- **All resource operations must be atomic** — full success or full failure; no partial transfer or half-consumed state. Source: ADR-0005
- **Dual capacity system**: slot-based (on_person, carried) and volume-based (in_storage, loaded, cargo) — checked in unified `stack_merge` algorithm. Source: ADR-0005
- **Stack merge algorithm**: fill fullest stack first; overflow creates new stacks subject to capacity check. Source: ADR-0005
- **Resource signals emit-after-mutation** — consumers read complete state. Re-entrant mutation returns `ERR_BUSY`. Source: ADR-0005
- **IntelManager (Autoload #6) is the single source of truth for all player knowledge and ability state** — knowledge state (4-level), pattern state (3-level), ability state (2-level). Source: ADR-0007
- **Ability unlock uses multi-path OR + intra-path AND logic** — 2-4 independent unlock paths per ability. Re-evaluate on every upstream event. Source: ADR-0007
- **Knowledge state is non-degradable** — VERIFIED/CONFIRMED/UNLOCKED are terminal states; any write attempting downgrade is rejected. Source: ADR-0007
- **Chart (Autoload #9) owns all chart data and state machine logic** — UIManager owns all visual rendering. Data/UI separation: Chart provides read-only queries, never visual attributes. Source: ADR-0008
- **`route_committed` is irreversible** — DEPARTURE_CONFIRMED is a terminal state; state machine gate prevents double-emit. Source: ADR-0008
- **Chart formulas (5) are pure functions** — no side effects, no state mutation; independently testable. Source: ADR-0008
- **AirshipModuleSystem (Autoload #8) manages dual-domain slot state** (actual_state / visible_state) — visible_state drives efficiency; actual_state is ground truth written by damage events. Source: ADR-0009
- **`swap_module` is a two-phase atomic operation** — Phase 1 validates all preconditions; Phase 2 executes (uninstall old → refund → install new → deduct). Any Phase 1 failure → no state change. Source: ADR-0009
- **`can_depart()` is the single entry point for airworthiness** — combines furnace power + hull integrity + load weight. Source: ADR-0009
- **Hull integrity is 0-100 single value with 4-band classification** — intact (76-100), damaged (26-75), critical (1-25), destroyed (0). Cross-band transitions increment scar counter. Source: ADR-0009

### Forbidden Approaches

- **Never cache or duplicate resource quantities outside ResourcesManager** — eliminates sync bugs. Source: ADR-0005
- **Never use Godot Resource (.tres/.res) for runtime resource stacks** — Dictionary maps directly to Canonical JSON without conversion layer. Source: ADR-0005
- **Never use event sourcing for resource operations** — snapshot current state; no replay log. Source: ADR-0005
- **Never distribute knowledge state across multiple systems** — IntelManager is the single source of truth for all knowledge/ability state. Source: ADR-0007
- **Never embed chart data logic in UIManager** — Chart formulas, state machine, and serialization belong to Chart Autoload. Source: ADR-0008
- **Never make `route_committed` reversible** — DEPARTURE_CONFIRMED has no undo path. Source: ADR-0008
- **Never store module state in Hub scene nodes** — module state is cross-scene persistent and belongs to AirshipModuleSystem Autoload. Source: ADR-0009
- **Never skip `can_depart()` before voyage start** — multiple blocking conditions must be reported simultaneously. Source: ADR-0009

### Performance Guardrails

- **Resource operation**: single operation <0.1ms (O(1) Dictionary lookups, O(N) stack merge where N≤5) — source: ADR-0005
- **Intel `consume_intel()`**: worst case <0.1ms (1-3 linked content + 0-1 patterns + re-evaluate 3 abilities × up to 4 paths × up to 3 conditions) — source: ADR-0007

---

## Feature Layer Rules

*Applies to: encounter context, exploration, combat, voyage/navigation, world repair, secondary mechanics*

### Required Patterns

- **EncounterContext is a Dictionary produced by Navigation (#10) and consumed by Exploration (#11)** — no Godot Resource subclass; maps directly to Canonical JSON. Source: ADR-0010
- **Exploration must validate EncounterContext on receipt and build fallback context on failure** — 5 trigger conditions: null ctx, missing route_id, missing destination_id, invalid voyage_result, non-Array resolved_encounters. Source: ADR-0010
- **`voyage_completed` signal carries the complete EncounterContext Dictionary** — single push; Navigation closes voyage state after emit. Source: ADR-0010
- **WorldRepair (Autoload #13) manages 3-state repair node state machine**: unrevealed → known → repaired. known→repaired is one-way irreversible. Source: ADR-0011
- **Physical arrival always triggers unrevealed→known** — no intel gate on interaction; intel only affects UI hint precision. Source: ADR-0011
- **Batch deposit: same node materials can be submitted across multiple sessions** — `deposited` counter persists via ADR-0003 snapshot. Source: ADR-0011
- **`deposit_validation` checks 5 violation types** before commit: invalid_node, empty_offer, invalid_material, excess_quantity, already_repaired. Source: ADR-0011
- **`repair_completed` signal fans out to 6 downstream systems** — single signal, parallel consumption: Intel, Chart, Settlement, Persistence, Feedback, UI. Source: ADR-0011
- **Combat (#12) consumes ThreatContext derived by Exploration from EncounterContext** — Combat never directly reads EncounterContext. Source: ADR-0010

### Forbidden Approaches

- **Never make Exploration query Navigation for EncounterContext after the fact** — Navigation closes voyage state after `voyage_completed` emit. Source: ADR-0010
- **Never split EncounterContext into multiple signals** — single aggregated signal preserves field ordering and avoids consumer-side aggregation complexity. Source: ADR-0010
- **Never require all repair materials in a single deposit** — batch submission is the designed progression loop. Source: ADR-0011
- **Never store repair node mutable state in Registry** — Registry owns static definitions only; WorldRepair owns runtime repair state. Source: ADR-0011

---

## Presentation Layer Rules

*Applies to: UI screens, HUD, modal stack, input routing, focus management, animations*

### Required Patterns

- **UIManager (Autoload #16) owns all UI screen state machine, modal stack, input routing, and focus management** — no panel manages its own input independently. Source: ADR-0012
- **Single-slot modal stack** — at most one modal panel visible simultaneously. Combat (S7) is the only override exception. Source: ADR-0012
- **4-layer input routing priority**: Modal (Layer 0) → Semi-modal (Layer 1) → Non-modal (Layer 2) → HUD Overlay (Layer 3) → World Interaction (Layer 4). Source: ADR-0012
- **Godot 4.6 dual-focus explicit sync**: mouse click must call `grab_focus()` to sync keyboard focus to mouse position. Source: ADR-0012
- **Theme focus StyleBox (keyboard) and hover StyleBox (mouse) must be visually distinct** — focus: #4FB7B2 1.5px solid border; hover: 10% brightness overlay, no border. Source: ADR-0012
- **HUD update is signal-driven + dirty-flag batch** — `_process` returns immediately on idle frames (zero-cost). Source: ADR-0012
- **All UI animations use `create_tween()`** — no manual `_process()` interpolation. ShaderMaterial for GPU-side effects (ink diffusion). Source: ADR-0012

### Forbidden Approaches

- **Never let individual panels handle their own input independently** — crosses modal stack rules, Esc consistency, and S7 combat override. Source: ADR-0012
- **Never rely on Godot built-in focus system alone for input routing** — engine focus has no concept of modal stack or layer-differentiated input blocking. Source: ADR-0012
- **Never persist UI state to ADR-0003 snapshots** — UI state rebuilds from domain data on scene load or tab restore. Source: ADR-0012

---

## Global Rules (All Layers)

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `InteractionRegistry`, `SnapshotPackage` |
| Private fields | `_camelCase` | `_routeId`, `_hullBandIntegrity` |
| Locals/parameters | camelCase | `routeId`, `hullBandIntegrity` |
| Signals/Events | PascalCase C# delegates; stable data IDs may remain snake_case | `DepositCommitted`, `RouteCommitted` |
| Files | PascalCase matching C# class names | `InteractionRegistry.cs`, `SnapshotPackage.cs` |
| Scenes/Prefabs | PascalCase | `AirshipHub.tscn`, `ExplorationScene.tscn` |
| Constants | UPPER_SNAKE_CASE | `DOMAIN_READY`, `KNOWLEDGE_UNREVEALED` |
| Autoload names | PascalCase matching class name | `Registry`, `Persistence`, `Intel` |

### Performance Budgets

| Target | Value |
|--------|-------|
| Framerate | 60fps (16.67ms frame) |
| Frame budget | 16ms (~12ms after engine overhead) |
| Draw calls | ≤400 MVP desktop 2D soft budget |
| Memory ceiling | 512MB total; Autoload pool ≤20MB; AirshipHub ≤60MB; Peak ≤200MB |
| Desktop boot time | <2s `boot_requested` → `session_ready` |

### Approved Libraries / Addons

- Godot .NET runtime + .NET SDK — required for C# implementation
- gdUnit4 — temporary legacy regression support during migration
- No new third-party runtime dependencies for MVP without ADR/user request

### Forbidden Patterns (All Layers)

- `dictionary_signal_payload` — signal payload must be typed parameters, not Dictionary. Source: ADR-0002
- `untyped_signal_param` — all signal parameters must have explicit type annotations. Source: ADR-0002
- `string_signal_connect` — use typed C# event/signal patterns, not `connect("name", ...)`. Source: ADR-0002, ADR-0019
- `deferred_emit` — do not defer cross-system events unless an ADR explicitly requires next-frame ordering. Source: ADR-0002
- `process_connect` — no dynamic connect/disconnect in `_process()`/`_physics_process()`. Source: ADR-0002
- `store_var_save` — no `store_var()`/`get_var()` Variant blob for authoritative save data. Source: ADR-0003
- `hardcoded_value` — all gameplay values must come from Registry (data-driven). Source: ADR-0001, ADR-0005
- `direct_cross_autoload_in_ready` — no calling other Autoload methods in `_ready()`. Source: ADR-0001
- `bare_object_payload` — no Node/Resource/Object/Callable references in signal or persistence payload. Source: ADR-0002, ADR-0019
- `signal_cascade_depth_3plus` — max signal cascade depth = 2 (A→B→C allowed; A→B→C→D forbidden). Source: ADR-0002

### Forbidden APIs (Godot 4.6.2)

These APIs are deprecated or unverified for Godot 4.6.2:

- Browser lifecycle APIs (`pagehide`, `visibilitychange`, JavaScriptBridge lifecycle callbacks) — superseded for active MVP work. Source: ADR-0019
- `store_var()` / `get_var()` — non-deterministic Variant blob encoding; forbidden for save data. Source: ADR-0003
- `ResourceSaver` / `ResourceLoader` for save data — `.tres`/`.res` format is not canonical; violates ADR-0003 deterministic encoding requirement. Source: ADR-0003

### Cross-Cutting Constraints

- **Signal cascade depth ≤2**: A→B→C allowed; A→B→C→D forbidden. Replace deep chains with direct fan-out from the original emitter. Source: ADR-0002
- **Signal parameters should stay small and typed**: if a signal needs many fields, define an explicit package type or ADR-approved context object. Source: ADR-0002, ADR-0019
- **All state mutations emit signals after the mutation completes** — consumers must read complete state, not in-progress state. Source: ADR-0002, ADR-0005
- **All persistent state goes through ADR-0003 SnapshotPackage + Canonical JSON** — no bypassing the Persistence Autoload for direct file I/O. Source: ADR-0003
- **All static content definitions come from Registry (#1)** — no hardcoded resource costs, repair requirements, intel definitions, or route data in gameplay code. Source: ADR-0001, ADR-0005, ADR-0011
- **Desktop C# first: every new system must build under Godot .NET/C# desktop constraints** — CI/local verification must include `dotnet build` once project files exist. Source: ADR-0019
- **No field in `_ready()` beyond constant init + signal declarations + null checks** — real initialization deferred to phase signal handlers. Source: ADR-0001
