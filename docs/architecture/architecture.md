# 云海织航 — Master Architecture

## Document Status
- **Version**: 1 (signed off)
- **Last Updated**: 2026-05-04
- **Engine**: Godot 4.6.2 .NET + C#
- **Target Platform**: Desktop-first (Windows primary, Linux secondary)
- **GDDs Covered**: #1–#18 (16 MVP + 2 Vertical Slice)
- **ADRs Referenced**: (none yet — 17 required)
- **Technical Director Sign-Off**: 2026-05-04 — APPROVED WITH CONCERNS (0 blockers, 4 HIGH concerns for ADR authoring)
- **Lead Programmer Feasibility**: 2026-05-04 — FEASIBLE WITH CONCERNS (8 concerns, 0 infeasible)

---

## Engine Knowledge Gap Summary

LLM training cutoff: ~May 2025 (Godot ~4.3). Project pinned: Godot 4.6.2.

| Domain | Risk | Key Changes | Affected Systems |
|--------|------|-------------|-----------------|
| UI / Control | 🔴 HIGH | Dual-focus system (4.6), FoldableContainer (4.5) | #16 UI/HUD |
| C# / .NET | 🔴 HIGH | Godot .NET project workflow, signal delegate patterns, C# string extraction (4.6) | All implementation systems |
| Navigation | ⚠️ MEDIUM | Dedicated 2D nav server `NavigationServer2D` (4.5) | #4 Movement/Interaction |
| Rendering | ⚠️ MEDIUM | Desktop D3D12 default on Windows, glow rework (4.6), Forward+/Compatibility choice | #16 UI/HUD, #17 Feedback/VFX |
| Physics 2D | ✅ LOW | Unchanged since 4.3 | #4, #10, #11, #12 |
| Audio | ✅ LOW | No breaking changes | #17 |
| Animation | ✅ LOW | No breaking changes | #16, #17 |
| Input | ✅ LOW | No breaking changes | #4, #16 |
| Networking | ✅ LOW | Not used in MVP | — |

---

## Technical Requirements Baseline

> Extracted from all 18 GDDs. Each `TR-[slug]-NNN` is a stable, versioned requirement
> that maps to one architectural decision (existing or needed ADR).

| Req ID | GDD | System | Requirement | Domain |
|--------|-----|--------|-------------|--------|
| TR-registry-001 | content-data-state-registry | #1 | Static content definitions with stable IDs across 12 content kinds | Data |
| TR-registry-002 | content-data-state-registry | #1 | `query_entity(id)` returns typed entity; `validate_all()` produces diagnostics | Data |
| TR-registry-003 | content-data-state-registry | #1 | Registry must not own mutable runtime state | Architecture |
| TR-platform-001 | platform-session-shell | #2 | Desktop application shell: loading → start/continue → gameplay | Platform |
| TR-platform-002 | platform-session-shell | #2 | Desktop focus/pause/quit handling with deterministic save boundaries | Platform |
| TR-platform-003 | platform-session-shell | #2 | 15 platform states: boot → title → loading → playing → paused → error | Platform |
| TR-persistence-001 | local-save-world-state-persistence | #3 | Staging → Verify → Promotion save workflow | Save/Load |
| TR-persistence-002 | local-save-world-state-persistence | #3 | 8 snapshot packages: progress.*, state.*, settings.* | Save/Load |
| TR-persistence-003 | local-save-world-state-persistence | #3 | Version migration: save_version field + migration path registry | Save/Load |
| TR-movement-001 | player-movement-interaction | #4 | CharacterBody2D movement with InteractionRegistry autoload | Core |
| TR-movement-002 | player-movement-interaction | #4 | C# abstract InteractionHandler base class for all interactable objects | Core |
| TR-movement-003 | player-movement-interaction | #4 | Interaction focus: nearest-reachable with priority tie-breaking | Core |
| TR-resources-001 | resources-goods-capacity | #5 | 6 resource pools with defined stack rules and capacity types | Economy |
| TR-resources-002 | resources-goods-capacity | #5 | `commit_deposit(node_id, resources)` — atomic, irreversible, Pool 6 terminal | Economy |
| TR-resources-003 | resources-goods-capacity | #5 | 3 capacity types: discrete slots, numeric stacks, weight-based | Economy |
| TR-intel-001 | player-knowledge-intel | #6 | 3 knowledge states per entity: unrevealed → rumored → identified → verified | Progression |
| TR-intel-002 | player-knowledge-intel | #6 | `IntelConsumeResult` algorithm: confidence decay + hidden tag reveal | Progression |
| TR-intel-003 | player-knowledge-intel | #6 | 3 ability unlock paths: Path A (intel-driven), Path B (repair-driven), Path C (composite) | Progression |
| TR-hub-001 | airship-hub | #7 | Walkable side-view airship interior with 10 MVP stations | Gameplay |
| TR-hub-002 | airship-hub | #7 | 2 departure modes: chart departure, direct departure | Gameplay |
| TR-hub-003 | airship-hub | #7 | Room gating: stations unlock via module installation | Gameplay |
| TR-modules-001 | airship-modules-hull-state | #8 | 2 module slots: scout module, cargo/repair module | Gameplay |
| TR-modules-002 | airship-modules-hull-state | #8 | Dual-field model: functional field + cosmetic field per module | Gameplay |
| TR-modules-003 | airship-modules-hull-state | #8 | 4 hull bands with efficiency coefficients; damage = max(per-band) not sum | Gameplay |
| TR-chart-001 | chart-route-planning | #9 | 2 MVP routes with authored hazard tags; traversable + selectable states | Gameplay |
| TR-chart-002 | chart-route-planning | #9 | Route rendering: rumored=dashed, identified=solid, verified=warm gold glow | UI |
| TR-chart-003 | chart-route-planning | #9 | Two-step departure confirmation with ink-spread animation and irreversible lock | Gameplay |
| TR-navigation-001 | navigation-route-risk | #10 | Voyage risk resolution: authored tags → EncounterContext via time-based progression | Gameplay |
| TR-navigation-002 | navigation-route-risk | #10 | 5 formulas: voyage duration, encounter check timing, scout preview, damage, hidden tag reveal | Gameplay |
| TR-navigation-003 | navigation-route-risk | #10 | Dynamic hull band transitions mid-voyage (Option B) | Gameplay |
| TR-exploration-001 | exploration-scavenge-scenario | #11 | 4-zone radial template: 50×35 units, 4-phase session | Gameplay |
| TR-exploration-002 | exploration-scavenge-scenario | #11 | 6 search points with free-search rule; 2 intel points; 2+ threat points | Gameplay |
| TR-exploration-003 | exploration-scavenge-scenario | #11 | Extraction: player-judged (no timer), λ_success/λ_forced with Unique item protection | Gameplay |
| TR-combat-001 | combat-threat-handling | #12 | 1 threat type with 3 response options (fight, evade, retreat) | Gameplay |
| TR-combat-002 | combat-threat-handling | #12 | Decision breath: player chooses response without real-time pressure | Gameplay |
| TR-combat-003 | combat-threat-handling | #12 | 3 outcomes: damaged, knocked back, retreat | Gameplay |
| TR-repair-001 | world-repair-unlock | #13 | 1 repair node: starlight_dock, 3-state machine (unrevealed → known → repaired) | Progression |
| TR-repair-002 | world-repair-unlock | #13 | Batch deposit with `deposit_validation` guarding excess/invalid materials | Progression |
| TR-repair-003 | world-repair-unlock | #13 | Repair completion triggers: route unlock + hazard reduction + ability unlock + world feedback | Progression |
| TR-settlement-001 | port-village-market | #14 | 1 starting settlement with walk-up stall purchasing | World/Economy |
| TR-settlement-002 | port-village-market | #14 | Repair-flag-driven stock changes; no price simulation in MVP | World/Economy |
| TR-settlement-003 | port-village-market | #14 | NPC activity and dialogue respond to repair_completed signal | World |
| TR-partner-001 | partner-relationships | #15 | 1 scout partner (sky-cat) with sniff verb → reveal_rumor() | Narrative |
| TR-partner-002 | partner-relationships | #15 | One-time irreversible naming + nest accumulation (4 items, irreversible) | Narrative |
| TR-partner-003 | partner-relationships | #15 | 6-state runtime machine keyed to Hub states; always present on airship | Narrative |
| TR-ui-001 | ui-hud-chart-interface | #16 | Screen state machine, screen flow, and 12-screen registry | UI |
| TR-ui-002 | ui-hud-chart-interface | #16 | Modal stack, combat override, input routing, and Godot 4.6 dual-focus sync | UI |
| TR-ui-003 | ui-hud-chart-interface | #16 | HUD dirty-flag batch updates, panel lifecycle, and lazy-load cache pool | UI |
| TR-ui-004 | ui-hud-chart-interface | #16 | Animation timing contracts, upstream data query interfaces, and downstream semantic events | UI/Integration |
| TR-feedback-001 | feedback-fx-audio | #17 | Semantic feedback events: route_selected, repair_completed, threat_triggered, etc. | Audio/VFX |
| TR-feedback-002 | feedback-fx-audio | #17 | Minimum visible-repair feedback owned by #13; home-safety feedback by #7; clarity by #16 | Audio/VFX |
| TR-onboarding-001 | onboarding-first-loop | #18 | First-loop guidance: Hub → Chart → Explore → Return → Repair | Meta |

**Count**: 52 technical requirements across 18 systems.

---

## System Layer Map

```
┌──────────────────────────────────────────────────────────────────┐
│  PRESENTATION                                                    │
│  #16 UI / HUD / 航图界面    #17 反馈、特效与音频语义 (VS)         │
├──────────────────────────────────────────────────────────────────┤
│  FEATURE                                                         │
│  #10 航行与路线风险    #11 探索 / 搜撤场景   #12 战斗与威胁处理   │
│  #13 世界修复与解锁    #14 空港 / 村镇状态    #15 伙伴功能与关系   │
│  #18 新手引导与首轮闭环 (VS)                                      │
├──────────────────────────────────────────────────────────────────┤
│  CORE                                                            │
│  #5 资源、货物与容量    #6 玩家知识与情报    #7 飞艇家园 Hub      │
│  #8 飞艇模块与船体状态  #9 航图与航线规划                         │
├──────────────────────────────────────────────────────────────────┤
│  FOUNDATION                                                      │
│  #1 内容数据与状态注册表    #2 平台与会话壳                        │
│  #3 本地存档与世界状态持久化  #4 玩家移动与交互                    │
├──────────────────────────────────────────────────────────────────┤
│  PLATFORM                                                        │
│  Godot 4.6.2  |  C#/.NET  |  Desktop Export  |  user:// JSON      │
└──────────────────────────────────────────────────────────────────┘
```

### Layer Assignment Details

| # | System | Layer | Engine Risk | Rationale |
|---|--------|-------|-------------|-----------|
| 1 | 内容数据与状态注册表 | Foundation | — | Static content contracts — all systems depend on stable IDs |
| 2 | 平台与会话壳 | Foundation | — | Web lifecycle, session management — engine integration boundary |
| 3 | 本地存档与世界状态持久化 | Foundation | — | Serialization/deserialization infrastructure |
| 4 | 玩家移动与交互 | Foundation | ⚠️ MEDIUM | Interaction framework: C# abstract handler base, NavigationServer2D (4.5) |
| 5 | 资源、货物与容量 | Core | — | Shared economy state consumed by all Features |
| 6 | 玩家知识与情报 | Core | — | Shared knowledge state — truth source for chart, repair, exploration |
| 7 | 飞艇家园 Hub | Core | — | Shared spatial context — stations, rooms, preparation |
| 8 | 飞艇模块与船体状态 | Core | — | Shared hull state — consumed by navigation, exploration, combat |
| 9 | 航图与航线规划 | Core | — | Central decision surface — all voyage systems depend on it |
| 10 | 航行与路线风险 | Feature | — | Voyage execution — produces EncounterContext |
| 11 | 探索 / 搜撤场景 | Feature | — | Exploration gameplay — consumes EncounterContext |
| 12 | 战斗与威胁处理 | Feature | — | Thin threat resolution — 1 threat, 3 responses |
| 13 | 世界修复与解锁 | Feature | — | Permanent world change — repair nodes, unlocks |
| 14 | 空港 / 村镇状态与集市交易 | Feature | — | Settlement economy — consumes repair signals |
| 15 | 伙伴功能与关系 | Feature | — | Partner gameplay — 1 scout verb, relationship memory |
| 16 | UI / HUD / 航图界面 | Presentation | 🔴 HIGH | 12 screens, modal stack, input routing — dual-focus system (4.6) |
| 17 | 反馈、特效与音频语义 | Presentation | — | Semantic feedback — VFX, audio events (Vertical Slice) |
| 18 | 新手引导与首轮闭环 | Feature | — | Cross-system flow orchestration (Vertical Slice) |
| 19 | 完整场景构成与验收 | Production / Scene Design | — | Scene completeness gate — design to implementation to QA |
| 20 | 场景单位物理设计 | Gameplay / Scene Physics | ⚠️ MEDIUM | Scene unit physics contract — movement planes, collision, occlusion, scale, special surfaces, dynamic behavior |

### Engine Risk Flags

| System | Risk | Domain | Concern | Mitigation |
|--------|------|--------|---------|------------|
| #4 Movement | ⚠️ MEDIUM | C# / Godot .NET | Abstract `InteractionHandler` base class pattern | Verify C# base class and typed signal pattern against `docs/engine-reference/godot/current-best-practices.md` and ADR-0019 |
| #4 Movement | ⚠️ MEDIUM | Navigation | `NavigationServer2D` (4.5) | Verify 2D nav server API against `docs/engine-reference/godot/modules/navigation.md` |
| #20 Scene Physics | ⚠️ MEDIUM | Physics / 2D | Scene unit physics contracts can drift from greybox/assets and cause unreadable collision | Require explicit Scene Physics Contract per scene, QA stuck-state recovery, and asset replacement checks |
| #16 UI/HUD | 🔴 HIGH | UI/Control | Dual-focus system (4.6) | Verify modal stack + input routing against `docs/engine-reference/godot/modules/ui.md`; `Control.focus_mode` new behavior |

---

## Module Ownership

### FOUNDATION LAYER

| System | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **#1 Registry** | Static entity definitions, ID schemas, 12 content kinds, validation diagnostics | `query_entity(id) → Entity`, `validate_all() → [Diagnostic]` | — (foundational) | `Resource`, `FileAccess` |
| **#2 Platform Shell** | Session lifecycle (15 states), loading screen, audio state, desktop focus/pause/quit state | `GetSessionState()`, `RequestAudioActivation()`, typed session state change signals | #3 (save for continue) | `SceneTree`, `DisplayServer`, Godot .NET lifecycle bindings |
| **#3 Persistence** | Serialization format, save slots, migration registry, Staging→Verify→Promotion workflow | `save(slot, packages)`, `load(slot) → SnapshotData`, `migrate(data, from, to)` | #1 (schema validation), #2 (session lifecycle) | `FileAccess` (4.4+), `ConfigFile` |
| **#4 Movement/Interaction** | Player position, movement state, InteractionRegistry, interaction focus, reachability checks | `RegisterInteractable(node, handler)`, `GetFocus()`, `UseFocused()`, typed interaction signals | #2 (input entry), #20 (scene physics contract) | `CharacterBody2D`, ⚠️ `NavigationServer2D` (4.5), C# abstract base class, `Input` |

### CORE LAYER

| System | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **#5 Resources** | 6 pools, stack rules, capacity state, deposit/withdraw operations | `can_deposit()`, `commit_deposit()`, `query_pool()`, `deposit_committed` signal | #1 (resource definitions), #3 (persistence) | `signal` system |
| **#6 Intel** | Per-entity knowledge state, 3 knowledge levels, rumor confidence, ability unlock state, intel consumption | `query_knowledge_state()`, `consume_intel()`, `query_ability_state()`, `on_repair_completed()` | #1 (entity schemas), #3 (persistence) | `signal` system |
| **#7 Hub** | Airship interior scene, station states, room gating, departure modes | `get_station_state()`, `activate_station()`, `depart(mode)`, Hub state change signals | #4 (movement/interaction), #3 (persistence), #1 (station definitions), #8 (module gating) | `Node2D`, `Area2D`, `AnimationPlayer` |
| **#8 Modules/Hull** | 2 module slots, hull integrity, 4 hull bands, efficiency coefficients, module effects | `get_module_state()`, `install_module()`, `get_hull_band()`, `apply_damage()` | #7 (Hub stations), #5 (resource costs), #3 (persistence) | `signal` system |
| **#9 Chart/Route** | Route visibility, selectability, chart navigation state, departure commit | `get_route_state()`, `select_route()`, `commit_departure()`, `on_route_enhanced()`, `route_committed` signal | #1 (route definitions), #3 (persistence), #6 (knowledge state), #7 (departure from Hub) | `Control`, `AnimationPlayer` |

### FEATURE LAYER

| System | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **#10 Navigation/Risk** | VoyageContext, encounter check state, dynamic hull band transitions, scout preview window, damage accumulation | `start_voyage(route_id) → VoyageContext`, `get_encounter() → EncounterContext`, voyage progress/completion signals | #9 (route_committed), #8 (hull state), #6 (hidden tags) | `Timer`, `signal` system |
| **#11 Exploration** | 4-zone radial scene, 4-phase session state, search/intel/threat point states, extraction anchor | `start_exploration(ctx)`, `search(point_id) → SearchResult`, `extract(anchor) → ExtractionResult` | #10 (EncounterContext), #20 (scene physics contract), #5 (resource yield), #8 (scout efficiency), #4 (movement), #6 (knowledge-gated descriptions) | `Node2D`, `Area2D`, `AnimationPlayer` |
| **#12 Combat** | Threat encounter state, player response choice, outcome resolution | `initiate_threat(ctx)`, `resolve_threat(response) → ThreatResult` | #11 (threat context), #8 (hull damage), #5 (response costs) | `signal` system (thin layer) |
| **#13 Repair** | Repair node state machine, deposited counters, repair progress, unlock results | `get_repair_state()`, `deposit_materials()`, `repair_completed` signal, `visual_state_anchor` | #1 (node definitions), #5 (can/commit_deposit), #6 (knowledge + on_repair_completed trigger), #3 (persistence), #9 (route enhancement) | `signal` system, `AnimationPlayer` (ceremony) |
| **#14 Settlement** | Settlement state, NPC activity, stall states, stock lists, purchase flow | `get_settlement_state()`, `get_stall_goods()`, `purchase()` | #13 (repair_completed → NPC/stock changes), #5 (resource exchange), #4 (interaction focus), #3 (persistence) | `Node2D`, `Area2D` |
| **#15 Partner** | Partner state machine (6 states keyed to Hub), sniff verb, naming state, nest accumulation (4 items) | `get_partner_state()`, `sniff(item_id)`, `name_partner(name)` | #7 (Hub state for state machine keying), #9 (route awareness), #6 (reveal_rumor calls), #1 (partner definitions), #5 (nest items), #3 (persistence) | `Node2D`, `AnimationPlayer` |

### PRESENTATION LAYER

| System | Owns | Exposes | Consumes | Engine APIs |
|--------|------|---------|----------|-------------|
| **#16 UI/HUD** | 12 screens, modal stack, 4-layer input routing, HUD dirty-flag system, animation timing contracts, screen state machine | `push_screen(id)`, `push_modal(id)`, `update_hud(data)`, screen transition signals | #9 (chart data), #8 (module data), #5 (resource data), #11 (exploration data), #13 (repair data), #14 (settlement data) | `Control` hierarchy, 🔴 `dual-focus` (4.6), `AnimationPlayer`, `Theme`, `StyleBox` |
| **#17 Feedback (VS)** | Semantic event subscriptions, VFX triggers, audio cue triggers | `emit_feedback(event_id, ctx)`, `register_feedback_handler(event_id, handler)` | #10, #11, #12, #13, #16 (semantic events) | `AudioStreamPlayer2D`, `CPUParticles2D`, `Tween` |
| **#18 Onboarding (VS)** | First-loop sequence state, guidance triggers, step progression | `start_first_loop()`, `advance_step(id)` | #7, #9, #11, #13, #14, #16 (cross-system orchestration) | `Control` overlay, `AnimationPlayer` |
| **#19 Scene Composition Gate** | Scene completeness gate: purpose, space, physics contract, behavior, state, presentation, technical contract, QA evidence | `scene_complete`, review checklist, asset traceability gate | #20, #7, #11, #13, #14, #16, #17, #18 | Design / QA artifact |
| **#20 Scene Physics** | Scene unit physics contract: scene type, movement plane, collision, occlusion, scale, special surfaces, physical behavior, recovery | `physics_contract_complete`, unit scale ratio, behavior conflict resolution | #4, #7, #11, #13, #14 | `CharacterBody2D`, `CollisionObject2D`, `Area2D`, `StaticBody2D`, `AnimatableBody2D` |

### Engine API Risk Flags (Verified)

| Flag | System | API | Version | Risk | Verified Against |
|------|--------|-----|---------|------|------------------|
| 🔴 | All code | Godot C#/.NET project workflow and `[Signal]` delegate patterns | 4.6 | HIGH | `docs/architecture/adr-0019-desktop-csharp-platform-pivot.md` |
| ⚠️ | #4 | `NavigationServer2D` | 4.5 | MEDIUM | `docs/engine-reference/godot/modules/navigation.md` |
| ⚠️ | #20 | `CollisionObject2D` / `Area2D` / `AnimatableBody2D` scene physics contracts | 4.6 | MEDIUM | `design/gdd/scene-physics-unit-system.md`, `docs/engine-reference/godot/modules/physics.md` |
| 🔴 | #16 | Dual-focus system / `Control.focus_mode` | 4.6 | HIGH | `docs/engine-reference/godot/modules/ui.md` |
| ⚠️ | #1, #3 | `FileAccess` return types | 4.4 | LOW | `docs/engine-reference/godot/deprecated-apis.md` |

### ASCII Dependency Diagram

```
══════════════════════════════════════════════════════════════════════
                        PRESENTATION
 ┌──────────┐  ┌──────────┐
 │ #16 UI   │  │ #17 FB   │
 └────┬─────┘  └────┬─────┘
      │consumes     │consumes (semantic events from Feature)
      ▼             ▼
══════════════════════════════════════════════════════════════════════
                        FEATURE
 ┌──────────┐  ┌──────────┐  ┌──────────┐
 │ #10 Nav  │─▶│ #11 Exp  │─▶│ #12 Cbt  │
 └────┬─────┘  └────┬─────┘  └──────────┘
      │              │
 ┌────┴─────┐  ┌────┴─────┐  ┌──────────┐  ┌──────────┐
 │ #13 Rep  │  │ #14 Stl  │  │ #15 Ptn  │  │ #18 Onb  │
 └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘
      │              │              │              │
      ▼              ▼              ▼              ▼
══════════════════════════════════════════════════════════════════════
                        CORE
 ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
 │ #5 Res   │  │ #6 Int   │  │ #7 Hub   │  │ #8 Mod   │  │ #9 Chrt  │
 └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘
      │              │              │              │              │
      ▼              ▼              ▼              ▼              ▼
══════════════════════════════════════════════════════════════════════
                       FOUNDATION
 ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
 │ #1 Reg   │  │ #2 Plat  │  │ #3 Pers  │  │ #4 Move  │
 └──────────┘  └──────────┘  └──────────┘  └──────────┘
      ▲              ▲              ▲              ▲
      │              │              │              │
══════════════════════════════════════════════════════════════════════
                       PLATFORM
 Godot 4.6.2  │  C#/.NET  │  Desktop Export  │  user:// JSON
══════════════════════════════════════════════════════════════════════
```

### Key Ownership Boundaries (Director Review)

| Boundary | Owner | Constraint |
|----------|-------|------------|
| Static data vs runtime state | #1 Registry | #1 must not own mutable runtime state |
| Serialization vs state management | #3 Persistence | #3 must not become global runtime state manager |
| Interaction entry vs consequences | #4 Movement | #4 provides Use entry point; domain systems own consequences |
| Knowledge vs display | #6 Intel | #6 is truth source; #9 displays/filters only |
| Repair conditions vs settlement display | #13 Repair | #13 owns unlock state; #14 owns stall/NPC presentation |
| Voyage output vs consumption | #10 Navigation | #10 produces EncounterContext; #11/#12 consume it |
| UI layout vs domain data | #16 UI | #16 owns layout/modals/animation; domain systems own data and state machines |

---

## Data Flow

### Frame Update Path

```
Input (Keyboard/Mouse)
  │
  ▼
#2 Platform Shell ─── captures raw input, applies desktop focus/pause guard
  │
  ▼
#4 Movement ─── CharacterBody2D._physics_process(delta)
  │             move_and_slide(), Area2D overlap detection
  │             InteractionRegistry.update_focus()
  ▼
Core Systems ─── Domain systems react to interaction signals
  │             (not per-frame — event-driven, see Event/Signal Path)
  ▼
Scene Tree ─── Godot renders node hierarchy (CanvasItem tree)
  │            #16 HUD: dirty-flag check → update only changed Controls
  ▼
DisplayServer ─── desktop renderer → OS window compositor
```

Frame budget: 60fps target (16.67ms). This is a non-real-time game — most frames are idle 2D rendering. Heavy operations (save, route calculation) fire on discrete events, not per-frame.

### Event/Signal Path

Core loop signal flow:

```
#7 Hub ──depart(mode)──▶ #9 Chart ──route_committed(route_id)──▶ #10 Navigation
                                                                      │
                                                        encounter_triggered(ctx)
                                                                      │
                                      ┌───────────────────────────────┘
                                      ▼
                              #11 Exploration ──threat_detected(ctx)──▶ #12 Combat
                                      │                                      │
                              search_yield()                           threat_result
                                      │                                      │
                                      ▼                                      ▼
                              #5 Resources ◀──────────── loot/add     #8 Modules (damage)
                                      │
                                      │ commit_deposit()
                                      ▼
                              #13 Repair ──repair_completed(node_id)──▶ #6 Intel (ability unlock)
                                      │
                                      ├──▶ #9 Chart (route_enhanced)
                                      ├──▶ #14 Settlement (NPC/stock)
                                      └──▶ #17 Feedback (VFX/audio)
```

Signal contract rules:
- All cross-system communication uses Godot `signal` — no direct method calls across layer boundaries
- Senders fire-and-forget; no waiting on consumer return values
- Signal payloads carry only IDs and context data, never mutable object references

### Save/Load Path

```
SAVE:
  #3 Persistence: request_snapshot() → each domain system returns its state
    ┌─ #5 Resources: pool states, deposited counters
    ├─ #6 Intel: knowledge states, ability states
    ├─ #7 Hub: station states
    ├─ #8 Modules: module states, hull bands
    ├─ #9 Chart: route states
    ├─ #13 Repair: node states, deposited counters
    ├─ #14 Settlement: stall/NPC states
    └─ #15 Partner: partner state, nest items
  ↓
  #3: serialize → write staging file → verify checksum → promote to slot
  ↓
  user:// local save files

LOAD:
  user:// local files → #3: read slot → verify checksum → deserialize
  ↓
  #3: distribute snapshots to domain systems → each restores its own state
  ↓
  #1 Registry: cross-validate restored state (deprecated IDs → warn)
  ↓
  #2 Platform: transition to playing state
```

Key contract: each domain system independently manages its own serialization/deserialization of its state. #3 only orchestrates, version-migrates, and verifies — it does not parse domain state internals.

### Initialisation Order

```
Boot Phase (sequential):
  1. Godot Main Loop start
  2. #2 Platform Shell — session lifecycle, desktop focus/pause guard
  3. #1 Registry — load & validate static content (fail-fast on schema errors)

Restore Phase:
  4. #3 Persistence — check save slots → load or init new game
  5. #5 Resources — init pools (from save or defaults)
  6. #6 Intel — init knowledge states (from save or defaults)

Scene Phase:
  7. #4 Movement — init CharacterBody2D, InteractionRegistry
  8. #7 Hub — instantiate airship scene, station nodes
  9. #8 Modules — init module slots, hull bands
  10. #9 Chart — init route states, connect to #6 signals

Feature Phase (lazy-init OK):
  11. #10 Navigation — connect to #9 route_committed
  12. #13 Repair — connect to #5 deposit_committed, #6 on_repair_completed
  13. #14 Settlement — init settlement scene, connect to #13 repair_completed
  14. #15 Partner — init partner scene, connect to #7 Hub state

Presentation Phase:
  15. #16 UI — init 12 screens, connect HUD to domain signals
  16. #17 Feedback — subscribe to semantic events (VS)

Entry Phase:
  17. #2 Platform: transition → title screen
  18. #18 Onboarding — start first-loop if new game (VS)
```

---

## API Boundaries

> 以下定义层间关键 API 契约的概念形状。旧 GDScript 风格签名仅作为历史 IDL 草案帮助阅读，不是当前实现语法；新实现必须按 ADR-0019 / Control Manifest 使用 Godot .NET / C#、PascalCase 方法、typed C# signals/events 和 DTO 边界。

### FOUNDATION → ALL

```text
# === #1 Registry (Autoload: Registry) ===
# 不变量: 所有 ID 为稳定字符串；实体加载后不可变
# 保证: query_entity 返回类型化 Entity 或 null（绝不返回部分/损坏数据）

func query_entity(entity_id: String) -> Entity
func validate_all() -> Array[Diagnostic]
func resolve_id(partial: String) -> String

# === #3 Persistence (Autoload: Persistence) ===
# 不变量: 同时只有一个 save/load 操作在执行
# 保证: 原子 promotion——staging 文件校验通过后才替换槽位

signal save_completed(slot: int)
signal load_completed(slot: int)

func save(slot: int, packages: Dictionary) -> Error
func load(slot: int) -> Dictionary
func migrate(data: Dictionary, from_ver: int, to_ver: int) -> Dictionary
func list_slots() -> Array[SaveSlotInfo]
```

### FOUNDATION → CORE / FEATURE

```text
# === #4 Movement/Interaction (Autoload: InteractionRegistry) ===
# 不变量: focus 始终为最近可达的可交互对象；无可交互对象时为 null
# 保证: use_focused() 分发到正确的领域处理器
# ⚠️ 引擎风险: C# abstract base class + Godot node binding —— 验证基类模式

signal interaction_focus_changed(focused: Interactable)
signal interaction_used(focused: Interactable)

func register_interactable(node: Node, handler: InteractionHandler) -> void
func unregister_interactable(node: Node) -> void
func get_focus() -> Interactable
func use_focused() -> void
```

### CORE APIs

```text
# === #5 Resources (Autoload: Resources) ===
# 不变量: Pool 6 为终态——一旦提交，资源永久锁定
# 保证: commit_deposit 原子操作——全部成功或全部回滚

signal deposit_committed(node_id: String)
signal pool_changed(pool_id: int)

func can_deposit(node_id: String, required: Dictionary) -> bool
func commit_deposit(node_id: String, resources: Dictionary) -> Result
func query_pool(pool_id: int) -> PoolState
func add_to_pool(pool_id: int, resource_id: String, quantity: int) -> void

# === #6 Intel (Autoload: Intel) ===
# 不变量: knowledge_state 只进不退（永不退化）
# 保证: consume_intel 对相同输入返回确定性结果

signal knowledge_changed(entity_id: String, new_state: int)
signal ability_unlocked(ability_id: String)

func query_knowledge_state(entity_id: String) -> int
func consume_intel(intel_id: String) -> IntelConsumeResult
func query_ability_state(ability_id: String) -> int
func on_repair_completed(node_id: String) -> void
func reveal_rumor(entity_id: String, confidence: int) -> void
```

```text
# === #7 Hub (Scene: AirshipHub) ===
# 不变量: 出发模式在 commit_departure 开始后锁定
# 保证: 站台状态在任何站台交互前准确反映当前状态

signal hub_state_changed(new_state: int)
signal departure_initiated(mode: int)

func get_station_state(station_id: String) -> int
func activate_station(station_id: String) -> void
func depart(mode: int) -> void

# === #8 Modules (Scene child of Hub: ModuleManager) ===
# 不变量: hull_band 损伤取 max-per-band，不求和
# 保证: 效率系数 clamp 至 [0.0, 1.0]

signal hull_band_changed(band_id: int, new_integrity: float)
signal module_installed(slot_id: int, module_id: String)

func get_module_state(slot_id: int) -> ModuleState
func install_module(slot_id: int, module_id: String) -> Result
func get_hull_band(band_id: int) -> HullBandState
func apply_damage(amount: float, band_id: int) -> void

# === #9 Chart (Autoload: Chart) ===
# 不变量: route_committed 不可逆——一旦发射，航线锁定
# 保证: selectability 检查在每次航线选择前运行

signal route_committed(route_id: String)
signal route_enhanced(route_id: String, enhancement: Dictionary)

func get_route_state(route_id: String) -> RouteState
func select_route(route_id: String) -> Result
func commit_departure() -> void
func on_route_enhanced(route_id: String, enhancement: Dictionary) -> void
```

### FEATURE APIs

```text
# === #10 Navigation (Scene: VoyageManager) ===
# 不变量: EncounterContext 一旦产出即不可变
# 保证: 航行以到达或强制中止结束

signal encounter_triggered(context: EncounterContext)
signal voyage_completed(route_id: String)
signal voyage_aborted(reason: String)

func start_voyage(route_id: String) -> VoyageContext
func get_current_encounter() -> EncounterContext
func get_voyage_progress() -> float

# === #11 Exploration (Scene: ExplorationScene) ===
# 不变量: 撤离由玩家判断——无自动撤离计时器
# 保证: Unique 物品在撤离中必定幸存

signal exploration_phase_changed(phase: int)
signal threat_detected(context: ThreatContext)
signal extraction_complete(result: ExtractionResult)

func start_exploration(context: EncounterContext) -> void
func search(point_id: String) -> SearchResult
func extract(anchor_id: String) -> ExtractionResult
```

```text
# === #12 Combat (Scene child of Exploration: ThreatResolver) ===
# 不变量: 玩家始终有 ≥1 个响应选项可用
# 保证: threat_result 在一个决策周期内产出（非阻塞）

signal threat_resolved(result: ThreatResult)

func initiate_threat(context: ThreatContext) -> void
func resolve_threat(response: int) -> ThreatResult

# === #13 Repair (Autoload: WorldRepair) ===
# 不变量: known→repaired 单向转换；已修复节点拒绝所有后续提交
# 保证: deposit_validation 在每次 commit_deposit 调用前运行

signal repair_completed(node_id: String)
signal deposit_accepted(node_id: String, deposited: Dictionary)

func get_repair_state(node_id: String) -> int
func deposit_materials(node_id: String, offer: Dictionary) -> DepositResult
func get_repair_progress(node_id: String) -> float
```

```text
# === #14 Settlement (Scene: Settlement) ===
# 不变量: stall 货品为固定或 repair-flag 驱动——无价格模拟
# 保证: purchase 在扣款前验证可支付性

signal purchase_completed(stall_id: String, good_id: String, quantity: int)
signal settlement_state_changed(settlement_id: String, new_state: int)

func get_settlement_state(settlement_id: String) -> SettlementState
func get_stall_goods(stall_id: String) -> Array[Good]
func purchase(stall_id: String, good_id: String, quantity: int) -> PurchaseResult

# === #15 Partner (Scene child of Hub: Partner) ===
# 不变量: 命名一次性且不可逆；nest 物品不可移除
# 保证: sniff 对 #6 发起 reveal_rumor 调用，confidence ≤ 66

signal partner_state_changed(new_state: int)
signal partner_named(name: String)

func get_partner_state() -> PartnerState
func sniff(item_id: String) -> SniffResult
func name_partner(name: String) -> Result
```

### PRESENTATION APIs

```text
# === #16 UI (Autoload: UIManager) ===
# 不变量: 最多一个模态活跃；战斗覆盖层优先级最高
# 保证: 屏幕转场遵守动画时序合约
# 🔴 引擎风险: dual-focus 系统 (4.6) —— 验证 Control.focus_mode 行为

signal screen_changed(screen_id: int)
signal modal_pushed(modal_id: int)
signal modal_popped()

func push_screen(screen_id: int) -> void
func push_modal(modal_id: int) -> void
func pop_modal() -> void
func update_hud(data: Dictionary) -> void
func show_toast(message: String, duration: float) -> void

# === #17 Feedback (Autoload: FeedbackManager) (VS) ===
# 不变量: feedback handler 不得修改玩法状态
# 保证: emit_feedback 非阻塞——立即返回

func emit_feedback(event_id: String, context: Dictionary) -> void
func register_feedback_handler(event_id: String, handler: Callable) -> void
```

### 引擎类型验证

| API 边界 | 引擎类型 | 版本 | 状态 |
|----------|---------|------|------|
| #4 `InteractionHandler` | C# abstract base class | 4.6.2 .NET | ⚠️ 验证: C# 基类、virtual/abstract 方法和 Godot 节点绑定模式 |
| #4 `NavigationServer2D` | Singleton | 4.5 | ⚠️ 验证: 专用 2D nav server；旧 `Navigation2DServer` 已移除 |
| #4 `CharacterBody2D` | Node class | stable | ✅ 自 4.0 起未变 |
| #1/#3 `FileAccess` | Singleton | 4.4+ | ⚠️ 部分方法返回类型从 `Error` 变为 `bool` |
| #16 `Control.focus_mode` | Enum | 4.6 | 🔴 验证: 新 `FOCUS_ALL` 行为与 dual-focus 系统 |
| #16 `AnimationPlayer` | Node class | stable | ✅ 未变 |
| #17 `AudioStreamPlayer2D` | Node class | stable | ✅ 未变 |

---

## ADR Audit

### Existing ADRs

**0 existing ADRs found.** All architectural decisions were made during this `/create-architecture` session (Phases 1–4) and have not yet been recorded as ADRs.

### ADR Quality Check

No existing ADRs to audit. Quality check criteria (from director gates) will apply to all new ADRs created from the Required list below.

### Traceability Coverage

| Metric | Count |
|--------|-------|
| Technical Requirements total | 52 |
| Covered by existing ADRs | 0 |
| **Gap** | **52** |

| Req ID | Requirement | ADR Coverage | Status |
|--------|-------------|--------------|--------|
| TR-registry-001 | Static content definitions with stable IDs | — | ❌ GAP |
| TR-registry-002 | `query_entity` + `validate_all` | — | ❌ GAP |
| TR-registry-003 | Registry must not own mutable runtime state | — | ❌ GAP |
| TR-platform-001 | Desktop application shell | ADR-0019 | ✅ COVERED |
| TR-platform-002 | Desktop focus/pause/quit handling | ADR-0019 | ✅ COVERED |
| TR-platform-003 | 15 platform states | — | ❌ GAP |
| TR-persistence-001 | Staging→Verify→Promotion | — | ❌ GAP |
| TR-persistence-002 | 8 snapshot packages | — | ❌ GAP |
| TR-persistence-003 | Version migration | — | ❌ GAP |
| TR-movement-001 | CharacterBody2D + InteractionRegistry | — | ❌ GAP |
| TR-movement-002 | C# abstract InteractionHandler | — | ❌ GAP |
| TR-movement-003 | Interaction focus: nearest-reachable | — | ❌ GAP |
| TR-resources-001 | 6 pools, stack rules, capacity types | — | ❌ GAP |
| TR-resources-002 | `commit_deposit` atomic + irreversible | — | ❌ GAP |
| TR-resources-003 | 3 capacity types | — | ❌ GAP |
| TR-intel-001 | 3 knowledge states per entity | — | ❌ GAP |
| TR-intel-002 | IntelConsumeResult algorithm | — | ❌ GAP |
| TR-intel-003 | 3 ability unlock paths | — | ❌ GAP |
| TR-hub-001 | Walkable airship + 10 stations | — | ❌ GAP |
| TR-hub-002 | 2 departure modes | — | ❌ GAP |
| TR-hub-003 | Room gating via module installation | — | ❌ GAP |
| TR-modules-001 | 2 module slots | — | ❌ GAP |
| TR-modules-002 | Dual-field model | — | ❌ GAP |
| TR-modules-003 | 4 hull bands, max-per-band damage | — | ❌ GAP |
| TR-chart-001 | 2 routes, traversable + selectable | — | ❌ GAP |
| TR-chart-002 | Knowledge-gated route rendering | — | ❌ GAP |
| TR-chart-003 | Two-step departure commit | — | ❌ GAP |
| TR-navigation-001 | Voyage risk: authored tags → EncounterContext | — | ❌ GAP |
| TR-navigation-002 | 5 voyage formulas | — | ❌ GAP |
| TR-navigation-003 | Dynamic hull band transitions | — | ❌ GAP |
| TR-exploration-001 | 4-zone radial template | — | ❌ GAP |
| TR-exploration-002 | 6 search + 2 intel + 2+ threat points | — | ❌ GAP |
| TR-exploration-003 | Player-judged extraction with Unique protection | — | ❌ GAP |
| TR-combat-001 | 1 threat type, 3 responses | — | ❌ GAP |
| TR-combat-002 | Decision breath (no real-time pressure) | — | ❌ GAP |
| TR-combat-003 | 3 outcomes: damaged/knocked back/retreat | — | ❌ GAP |
| TR-repair-001 | Repair node 3-state machine | — | ❌ GAP |
| TR-repair-002 | Batch deposit with validation | — | ❌ GAP |
| TR-repair-003 | Repair completion triggers | — | ❌ GAP |
| TR-settlement-001 | Walk-up stall purchasing | — | ❌ GAP |
| TR-settlement-002 | Repair-flag-driven stock changes | — | ❌ GAP |
| TR-settlement-003 | NPC response to repair_completed | — | ❌ GAP |
| TR-partner-001 | Scout partner + sniff verb | — | ❌ GAP |
| TR-partner-002 | Naming + nest accumulation | — | ❌ GAP |
| TR-partner-003 | 6-state machine keyed to Hub | — | ❌ GAP |
| TR-ui-001 | Screen state machine + 12-screen registry | ADR-0012 | ✅ COVERED |
| TR-ui-002 | Modal stack + input routing + dual-focus | ADR-0012 | ✅ COVERED |
| TR-ui-003 | HUD dirty flags + panel lifecycle/cache | ADR-0012 | ✅ COVERED |
| TR-ui-004 | Animation timing + upstream data + semantic events | ADR-0012 | ✅ COVERED |
| TR-feedback-001 | Semantic feedback events | — | ❌ GAP |
| TR-feedback-002 | MVP feedback ownership assignment | — | ❌ GAP |
| TR-onboarding-001 | First-loop guidance | — | ❌ GAP |

## Required ADRs

### Must Have Before Coding Starts (Foundation & Core — 6 ADRs)

| ADR | Title | Covers TRs | Key Decision |
|-----|-------|------------|--------------|
| ADR-0001 | Autoload/Scene Architecture & Boot Order | TR-platform-*, TR-persistence-*, TR-registry-* | Which systems are Autoloads (Registry, Persistence, Resources, Intel, Chart, InteractionRegistry, WorldRepair, UIManager, FeedbackManager) vs Scenes (Hub, Settlement, Exploration, VoyageManager); boot sequence from Phase 3 |
| ADR-0002 | Signal-based Cross-System Communication Protocol | TR-movement-*, TR-resources-*, TR-intel-*, TR-repair-* | All cross-layer communication uses Godot signals; fire-and-forget; payloads carry only IDs and context data; no direct method calls across layers |
| ADR-0003 | Save/Load — Snapshot Package System | TR-persistence-* | Staging→Verify→Promotion workflow; 8 snapshot packages; each domain system owns its serialization; Persistence only orchestrates, migrates, verifies |
| ADR-0004 | Interaction System — C# Handler Base + Registry | TR-movement-* | C# `InteractionHandler` base class; InteractionRegistry autoload; nearest-reachable focus; Use entry dispatches to domain handlers |
| ADR-0005 | Resource Pool Architecture — 6 Pools, Capacity Types, Terminal Deposit | TR-resources-* | 6 pools with defined stack rules; 3 capacity types; `commit_deposit` atomic + irreversible to Pool 6 |
| ADR-0006 | Web Platform Constraints & Engine Compatibility | Historical / superseded by ADR-0019 | Retained for rationale only; Web export no longer governs MVP implementation |
| ADR-0019 | Desktop C# Platform Pivot | TR-platform-* | Godot 4.6.2 .NET/C# desktop-first implementation; Web/GDScript constraints superseded for active MVP work |

### Should Have Before System Build (Core & Feature — 6 ADRs)

| ADR | Title | Covers TRs | Key Decision |
|-----|-------|------------|--------------|
| ADR-0007 | Knowledge State & Ability Unlock Architecture | TR-intel-* | 4-level knowledge (unrevealed→rumored→identified→verified); IntelConsumeResult algorithm; 3 ability unlock paths (A/B/C) |
| ADR-0008 | Chart Route State Machine & Departure Commit | TR-chart-* | Route visibility/selectability states; two-step departure confirmation with ink-spread animation; irreversible route_committed |
| ADR-0009 | Module/Hull Damage Model — max-per-band | TR-modules-* | 2 module slots; dual-field model; 4 hull bands; damage = max(band_damage) not sum; efficiency coefficients |
| ADR-0010 | EncounterContext Contract — Navigation→Exploration Boundary | TR-navigation-*, TR-exploration-* | Voyage produces immutable EncounterContext; exploration consumes it; 5 voyage formulas; 4-zone radial template |
| ADR-0011 | Repair State Machine & Batch Deposit | TR-repair-* | 3-state machine (unrevealed→known→repaired); batch deposit with deposit_validation guard; repair_completed triggers |
| ADR-0012 | UI Screen State Machine & Modal Stack | TR-ui-* | 12 screens; single-modal + combat overlay; 4-layer input routing; signal-driven HUD with dirty-flag batch updates; animation timing contracts |

### Can Defer to Implementation (5 ADRs)

| ADR | Title | Covers TRs |
|-----|-------|------------|
| ADR-0013 | Airship Hub Scene Architecture | TR-hub-* |
| ADR-0014 | Settlement/Market — Repair-flag-driven Stock Changes | TR-settlement-* |
| ADR-0015 | Partner State Machine & Relationship Memory | TR-partner-* |
| ADR-0016 | Feedback System Semantic Event Subscription (VS) | TR-feedback-* |
| ADR-0017 | Onboarding — Cross-system Flow Orchestration (VS) | TR-onboarding-* |

**Total**: 17 ADRs required — 6 Foundation/Core blocking, 6 Core/Feature pre-build, 5 deferrable.

---

## Architecture Principles

1. **Signal-Driven, Not Coupled** — State mutations and significant events cross layer boundaries via Godot signals (fire-and-forget; signal payloads carry IDs and context data, never mutable object references). Read-only state queries may use direct method calls on the owning system's public API (e.g., `get_hull_band()`). Prevents God Objects and shared-state coupling (TD-SYSTEM-BOUNDARY constraint).

2. **Domain Owns State, Not Infrastructure** — Each domain system owns its runtime state and serialization format. #3 Persistence only orchestrates, version-migrates, and verifies — it does not parse domain state internals. #1 Registry owns only static content definitions — it does not hold mutable runtime state. Infrastructure systems are pipes, not reservoirs.

3. **Data-Driven, Never Hardcoded** — All gameplay values (material costs, hazard rates, route definitions, repair thresholds) live in #1 Registry as static content. Systems read config at runtime via `query_entity()`. No magic numbers in code. Tuning does not require recompilation.

4. **Desktop C# Is The Active Platform Contract** — Every new implementation story assumes Godot 4.6.2 .NET/C# and desktop lifecycle boundaries. Browser-only constraints from ADR-0006 are historical unless a future ADR reintroduces Web as a separate target.

5. **Thin MVP, Depth Later** — Each system starts at its MVP thin-slice boundary (defined in systems-index.md). Systems are built to work correctly at minimum scope first; depth is added inside existing systems, not by adding new top-level systems. The thin-slice rules (1 Hub, 2 routes, 1 exploration point, 1 threat, 1 repair node, 1 partner, fixed market) are hard constraints for MVP.

---

## Open Questions

1. **Autoload count vs desktop boot time** — 9 proposed Autoloads (Registry, Persistence, InteractionRegistry, Resources, Intel, Chart, WorldRepair, UIManager, FeedbackManager) may impact initial load. Profile after C# migration; consider lazy-init for Feature-layer Autoloads only if measured.

2. **Dual-focus system (Godot 4.6) with 4-layer input routing** — The interaction between Godot 4.6's new dual-focus system and #16's custom 4-layer input routing priority needs runtime verification. Risk: conflicting focus claims between Godot's built-in focus and the modal stack's input capture.

3. **Desktop save size and atomic promotion** — Save file size needs profiling once all 8 snapshot packages are populated. If save latency exceeds budget, consider compression or selective snapshot packages after measurement.

4. **Desktop audio startup** — Audio no longer depends on browser user activation, but the first desktop build must still validate volume defaults, mute/pause behavior, and focus-loss pause policy.

5. **NavigationServer2D API compatibility (Godot 4.5)** — The dedicated 2D navigation server replaced the unified NavigationServer. Verify the API surface matches usage in #4 Movement/Interaction.

6. **TR Registry population** — 52 technical requirements extracted. They need to be written to `docs/architecture/tr-registry.yaml` with stable IDs before story creation begins.

7. **#17 Feedback and #18 Onboarding are Vertical Slice** — Their ADRs (ADR-0016, ADR-0017) are accepted; implementation remains Vertical Slice / Polish scope. MVP must still provide minimum feedback (owned by #13 repair ceremony, #7 Hub safety, #16 UI clarity) and first-loop guidance (owned by #7, #9, #11, #13, #14, #16 collectively).

8. **C# build and test path** — The first implementation sprint must add a `dotnet build` check and a desktop/headless validation route before retiring GDScript prototype tests.
