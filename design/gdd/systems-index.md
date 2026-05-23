# Systems Index: 云海织航

> **Status**: Draft
> **Created**: 2026-04-26
> **Last Updated**: 2026-05-24
> **Source Concept**: `design/gdd/game-concept.md`
> **Art Bible**: `design/art/art-bible.md`
> **Review Mode**: Full

> **Platform Pivot Note (2026-05-09)**: ADR-0019 supersedes Web-first / GDScript implementation constraints for active production work. MVP implementation now targets desktop Godot 4.6.2 .NET with C#; remaining Web references in older GDD text are historical unless this index or a refreshed system GDD restates them as current desktop requirements.

---

## Overview

《云海织航》需要一组围绕“可步行飞艇家园 -> 航线规划 -> 短探索 / 搜撤 -> 带回材料或情报 -> 修复世界节点 -> 空港 / 村镇 / 航线出现可见变化”的系统。系统拆分的核心目标不是堆功能，而是保护三个主轴：玩家能在飞艇中整备并居住，能通过航图规划低压但有取舍的探索，能把资源、情报和贸易行为反哺到世界状态中。

本索引采用薄层 MVP 原则。MVP 中很多系统只需要最小可验证版本；后续深度扩展不新增大量顶层系统，而是在既有系统内逐步扩展内容量、规则复杂度和表现质量。

---

## Director Review Notes

> **Technical Director Review (TD-SYSTEM-BOUNDARY)**: CONCERNS accepted 2026-04-26.
> Boundaries are architecturally workable, but future GDDs and architecture must prevent God Object and shared-state coupling risks.

Required TD boundary constraints:

- `内容数据与状态注册表` owns only static content definitions, IDs, schemas, and query contracts. It must not own mutable runtime state.
- `本地存档与世界状态持久化` owns serialization, deserialization, and version migration. It must not become a global runtime state manager.
- `玩家移动与交互` owns movement, reachability, interaction focus, and the Use entry point. Specific interaction consequences belong to domain systems.
- `航行与路线风险` should produce authored outcomes or `EncounterContext`; `探索 / 搜撤场景` and `战斗与威胁处理` consume that context and must not create hidden cycles.
- `世界修复与解锁` owns repair conditions, state changes, and unlock results. `空港 / 村镇状态与集市交易` owns how those results appear as stalls, stock, NPC activity, and purchasable goods.
- `玩家知识与情报` is the source of truth for known, unknown, rumor, and risk information. `航图与航线规划` displays, filters, and selects routes.
- `反馈、特效与音频语义` should subscribe to semantic events such as route selected, trade completed, repair completed, and threat triggered. It should not directly own gameplay state.

> **Producer Review (PR-SCOPE)**: OPTIMISTIC accepted 2026-04-26.
> MVP is feasible only if several systems remain thin layers rather than full subgames.

Required Producer scope guardrails:

- `战斗与威胁处理` is a threat-resolution layer in MVP: 1 threat type, 1-2 responses, and only damaged / knocked back / retreat outcomes.
- `空港 / 村镇状态与集市交易` is walk-up stall purchasing plus fixed or repair-flag-driven stock changes. No price simulation, supply/demand simulation, stock refresh economy, or trade-route algorithm in MVP.
- `伙伴功能与关系` is 1 scout verb plus 1 shallow relationship feedback beat. No affection ladder, party system, event tree, or crew collection in MVP.
- `航行与路线风险` uses authored risk tags and encounter tables. No wind-field simulation, continuous piloting model, or complex consumption model in MVP.
- `探索 / 搜撤场景` uses 1 exploration-point template with state variants. No second dungeon rhythm in MVP.
- `飞艇模块与船体状态` uses 2 modules and simplified ship damage. No subsystem repair tree in MVP.
- Do not add a second exploration point, second partner, or second repair chain before the full loop is stable.
- All world changes are single-source state changes. No bidirectional propagation or continuous simulation in MVP.

> **Creative Director Review (CD-SYSTEMS)**: CONCERNS accepted 2026-04-26.
> The system set covers the core fantasy, but GDD authoring must explicitly preserve first-loop ownership, settlement needs, minimum relationship delivery, and MVP feedback ownership.

Required CD vision constraints:

- `新手引导与首轮闭环` can remain a Vertical Slice system for full onboarding, but MVP GDDs must still assign ownership for proving the first loop: `飞艇家园 Hub` starts the loop, `航图与航线规划` chooses the route, `探索 / 搜撤场景` delivers the outing, `世界修复与解锁` closes the loop, and `UI / HUD / 航图界面` keeps the steps understandable.
- `空港 / 村镇状态与集市交易` must bind stalls and goods to settlement needs, local identity, and repair-state changes. It must not degrade into a generic shop or pure errand trade.
- `伙伴功能与关系` must deliver at least one memorable scout partner identity beat and one persistent relationship memory, not just a utility button.
- MVP-level proof of visible recovery and home safety must be owned by `世界修复与解锁`, `飞艇家园 Hub`, and `UI / HUD / 航图界面`, even though the fuller `反馈、特效与音频语义` system is Vertical Slice.

### Emergency Ship Tier

If schedule pressure requires a graceful fallback, ship this minimum:

`1 Hub + 1 settlement + 1 safe route + 1 risky route variant + 1 exploration point + 1 repair outcome + fixed market + threat-only combat + save/load`

This tier preserves the product identity better than cutting the walkable airship, route planning, or world repair loop.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---:|---|---|---|---|---|---|
| 1 | 内容数据与状态注册表 | Core | MVP | Approved | `design/gdd/content-data-state-registry.md` | — |
| 2 | 平台与会话壳 | Core | MVP | Approved | `design/gdd/platform-session-shell.md` | — |
| 3 | 本地存档与世界状态持久化 | Persistence | MVP | Approved | `design/gdd/local-save-world-state-persistence.md` | 内容数据与状态注册表; 平台与会话壳 |
| 4 | 玩家移动与交互 | Core | MVP | Approved | `design/gdd/player-movement-interaction.md` | 平台与会话壳 |
| 5 | 资源、货物与容量 | Economy | MVP | Approved | `design/gdd/resources-goods-capacity.md` | 内容数据与状态注册表; 本地存档与世界状态持久化 |
| 6 | 玩家知识与情报 | Progression | MVP | Approved | `design/gdd/player-knowledge-intel.md` | 内容数据与状态注册表; 本地存档与世界状态持久化 |
| 7 | 飞艇家园 Hub | Gameplay | MVP | Approved | `design/gdd/airship-hub.md` | 玩家移动与交互; 本地存档与世界状态持久化; 内容数据与状态注册表 |
| 8 | 飞艇模块与船体状态 | Gameplay | MVP | Approved | `design/gdd/airship-modules-hull-state.md` | 飞艇家园 Hub; 资源、货物与容量; 本地存档与世界状态持久化 |
| 9 | 航图与航线规划 | Gameplay | MVP | Approved | `design/gdd/chart-route-planning.md` | 内容数据与状态注册表; 本地存档与世界状态持久化; 玩家知识与情报; 飞艇家园 Hub |
| 10 | 航行与路线风险 | Gameplay | MVP | Approved | `design/gdd/navigation-route-risk.md` | 航图与航线规划; 飞艇模块与船体状态; 玩家知识与情报; 飞艇家园 Hub; 资源、货物与容量 |
| 11 | 探索 / 搜撤场景 | Gameplay | MVP | Approved | `design/gdd/exploration-scavenge-scenario.md` | 场景单位物理设计; 资源、货物与容量; 飞艇模块与船体状态; 航行与路线风险; 玩家移动与交互; 玩家知识与情报 |
| 12 | 战斗与威胁处理 | Gameplay | MVP | Approved | `design/gdd/combat-threat-handling.md` | 探索 / 搜撤场景; 飞艇模块与船体状态; 资源、货物与容量 |
| 13 | 世界修复与解锁 | Progression | MVP | Approved | `design/gdd/world-repair-unlock.md` | 资源、货物与容量; 玩家知识与情报; 本地存档与世界状态持久化; 航图与航线规划 |
| 14 | 空港 / 村镇状态与集市交易 | World / Economy | MVP | Approved | `design/gdd/port-village-market.md` | 世界修复与解锁; 资源、货物与容量; 玩家移动与交互; 本地存档与世界状态持久化 |
| 15 | 伙伴功能与关系 | Narrative / Gameplay | MVP | Approved | `design/gdd/partner-relationships.md` | 飞艇家园 Hub; 航图与航线规划; 玩家知识与情报; 内容数据与状态注册表; 资源、货物与容量; 本地存档与世界状态持久化 |
| 16 | UI / HUD / 航图界面 | UI | MVP | Designed | `design/gdd/ui-hud-chart-interface.md` | 航图与航线规划; 飞艇模块与船体状态; 资源、货物与容量; 探索 / 搜撤场景; 世界修复与解锁; 空港 / 村镇状态与集市交易 |
| 17 | 反馈、特效与音频语义 | Audio / Presentation | Vertical Slice | Approved | `design/gdd/feedback-fx-audio.md` | 航行与路线风险; 探索 / 搜撤场景; 战斗与威胁处理; 世界修复与解锁; UI / HUD / 航图界面 |
| 18 | 新手引导与首轮闭环 | Meta | Vertical Slice | Approved | `design/gdd/onboarding-first-loop.md` | 飞艇家园 Hub; 航图与航线规划; 探索 / 搜撤场景; 世界修复与解锁; 空港 / 村镇状态与集市交易; UI / HUD / 航图界面 |
| 19 | 完整场景构成与验收 | Production / Scene Design | Polish Gate | In Design | `design/gdd/scene-composition-system.md` | 场景单位物理设计; 飞艇家园 Hub; 探索 / 搜撤场景; 世界修复与解锁; 空港 / 村镇状态与集市交易; UI / HUD / 航图界面; 反馈、特效与音频语义; 新手引导与首轮闭环 |
| 20 | 场景单位物理设计 | Gameplay / Scene Physics | MVP Foundation | In Design | `design/gdd/scene-physics-unit-system.md` | 玩家移动与交互; 飞艇家园 Hub; 探索 / 搜撤场景; 空港 / 村镇状态与集市交易; 世界修复与解锁 |

---

## System Descriptions

### 1. 内容数据与状态注册表

Static content definitions, IDs, schemas, content domains, validation diagnostics, and query contracts for resources, cargo, modules, routes, locations, repair nodes, market goods, partners, threats, and intel.

Scope boundary: this system does not own mutable runtime state.

### 2. 平台与会话壳

Desktop application shell: loading, start/continue, audio activation, window focus / pause recovery, keyboard/mouse entry points, and session lifecycle.

### 3. 本地存档与世界状态持久化

Save/load persistence for resources, modules, route state, repair state, settlement state, market goods state, exploration state, and settings.

Scope boundary: persistence serializes and restores domain-owned state; it must not become the runtime state manager.

### 4. 玩家移动与交互

Movement, reachability, collision/reach checks, interaction focus, and the Use entry point for airship, settlement, market stall, and exploration interactions.

Scope boundary: concrete consequences such as buying, harvesting, repairing, or installing modules belong to the owning domain system. Scene unit physics such as horizontal/vertical scene type, collision semantics, unit scale, occlusion, special surfaces, and dynamic physical behaviors belong to #20; movement consumes those contracts.

### 5. 资源、货物与容量

Materials, cargo, supplies, purchasable goods, carried loot, cargo capacity, and tradeoffs between what to bring and what to return with.

### 6. 玩家知识与情报

Known/unknown route information, rumors, risk clues, old logs, resource hints, scouting discoveries, and permanent player knowledge growth.

Scope boundary: this is the truth source for discovered information. The map reads from it.

### 7. 飞艇家园 Hub

Walkable side-view airship interior: rooms, interaction points, home feeling, preparation zones, storage spots, partner station, and module access.

### 8. 飞艇模块与船体状态

Two MVP modules plus simplified ship state: scout module, cargo/repair module, ship damage, repair readiness, and module effects on route/exploration choices.

### 9. 航图与航线规划

Pre-voyage map-style decision surface: consumes static route definitions from Content Registry (#1) and dynamic knowledge state from Player Knowledge & Intel (#6), renders routes with knowledge-gated visual encoding (rumored=dashed, identified=solid, verified=warm gold glow), evaluates route selectability via traversability and origin-location match, commits departure via two-step confirmation with ink-spread animation and irreversible lock. MVP: 2 routes, 1 starting port, fixed view, rumor toggle.

Scope boundary: chart displays, filters, and selects routes; it does not own discovery state or voyage execution. NPC ship trajectories and cargo delegation are excluded (belong to #14 in Phase 3+).

GDD: `design/gdd/chart-route-planning.md` — 11 sections, CD reviewed PASS WITH NOTES (2026-05-02).

### 10. 航行与路线风险

Voyage risk resolution engine: consumes `route_committed` events from #9, resolves authored hazard tags into concrete encounters via time-based progression, and outputs `EncounterContext` to downstream systems (#11, #17, #8, #6, #3). 5 formulas: voyage duration, encounter check timing, scout preview window, damage accumulation (max-not-sum), hidden tag reveal probability. Dynamic hull band transitions mid-voyage (Option B). Scout module as pure information layer — preview window only, no avoidance. Architecture reserves `VoyageContext` for NPC/delegated routes (Phase 3+). MVP: 2 routes (sky-reef-arc-01 safe/short/5 checks, storm-cut-01 risky/medium/10 checks) with authored encounter tables.

Scope boundary: this system produces route outcomes or `EncounterContext`; it does not implement combat or exploration rules directly.

GDD: `design/gdd/navigation-route-risk.md` — CD-GDD-ALIGN (full): APPROVE WITH NOTES. 1 blocker resolved (rule #7 scout preview alignment), 5 warnings accepted. 8/8 required + 3/3 optional sections, 95 ACs, 36 edge cases, 7 open questions.

### 11. 探索 / 搜撤场景

Exploration-point scene consuming `EncounterContext` from #10: 4-zone radial template (云观站废墟, 50×35 units), 4-phase session (ARRIVING→EXPLORING→EXTRACTING→DEPARTED), 6 search points with free-search rule (empty results don't consume attempts), 2 intel points, 2+ threat points (environmental handled by #11, guard delegated to #12), 1 extraction anchor (player-judged, no timer). Scout efficiency η_scout maps to 3-tier threat preview (none/presence/full). 3 state variants: unlooted → looted → danger-changed. 6 formulas (search_yield, threat_trigger, scout_preview_level, extraction_loss_settlement, state_variant_transition, intel_yield). Extraction loss via λ_success/λ_forced with Unique item protection (Pillar 4). Pool 5 preserves pre-exploration contents. Exploration now treats authored physical world exploration as bottom-layer play: paths, obstacles, pushables, special surfaces, shadows/height cues, and dynamic physical behavior are governed by #20.

GDD: `design/gdd/exploration-scavenge-scenario.md` — full-mode 8-agent adversarial review (×2). Revision 2 applied (2026-05-03): 5 blockers resolved (F-11-04 atomicity, build_threat_context env guard, F-11-01 empty pool guard, registry sync, 4 GM commands) + R1 knowledge-gated descriptions. 8/8 required + Visual/Audio + UI + Test Tools sections, 6 formulas, 21 edge cases, 13 tuning knobs, 26 ACs, 6 open questions. APPROVED.

### 12. 战斗与威胁处理

Thin MVP threat-resolution layer: 1 threat type, 1-2 player responses, damaged / knocked back / retreat outcomes.

Scope boundary: threat handling supports extraction pressure. It is not a full combat game, enemy AI suite, weapon system, or status-effect system in MVP.

### 13. 世界修复与解锁

Conversion of materials or intelligence into permanent repair outcomes: lighthouse, beacon, facility, route node, route stability, or visible world state change.

Scope boundary: owns repair conditions and unlock state; presentation in settlement stalls or NPC activity belongs to the settlement system.

### 14. 空港 / 村镇状态与集市交易

Starting settlement/port state, NPC activity, walk-up market stalls, purchasable supplies/materials/parts/local goods/intelligence, and repair-driven changes to stock or liveliness.

Scope boundary: MVP market is fixed or flag-driven. No price simulation, supply/demand simulation, stock refresh economy, or trade-route algorithm.

Creative constraint: each stall and good must answer a settlement need or local identity question. Market changes should show why the village or port is recovering, not merely add a better shop list.

### 15. 伙伴功能与关系

MVP partner `partner.sky-cat` (航海猫) — last of the pre-Fragmentation airship crew cat lineage. Scout verb: sniff items brought back by the player, producing `reveal_rumor()` calls to #6 with confidence hard-capped at 66 (never authoritative). Relationship memory: one-time irreversible naming + nest accumulation in living quarters (4 items, irreversible). Cat always present on airship (R2), driven by 6-state runtime machine keyed to Hub states. No affection values, no gift menu, no event tree (R15).

CD constraints delivered: naming moment (memorable identity beat) + nest traces (persistent relationship memory). Pillars: P5 primary, P4/P3 auxiliary.

Scope boundary: 1 scout partner only in MVP; no crew collection, party management, affection ladder, or event tree.

GDD: `design/gdd/partner-relationships.md` — 8 required sections + self-check complete, CD reviewed APPROVE (2026-05-02). 5 cross-GDD revision flags (F.5) for #6, #7, #1, #16.

### 16. UI / HUD / 航图界面

MVP 呈现层外壳，将 15 个领域系统的数据与状态转换为统一的画面体验。拥有 12 个屏幕（S1-S12）、模态栈（单模态 + 战斗覆盖）、输入路由（4 层优先级）、HUD 更新策略（信号驱动 + 脏标记批量更新）、12 个动画时序合约。视觉语言使用航路修复主义（UI 像可被阅读的航海图），统一 UI 语义色板（8 色）覆盖 #8/#12/#13 的色值冲突，统一玩家面向术语（船体完整性、随身物品栏、货舱、云海币）。完整屏幕流状态机覆盖 Hub→航图→探索→返回 闭环。WCAG AA 对比度合规，桌面窗口失焦、暂停与恢复兼容。

Scope boundary: #16 拥有布局、模态管理、屏幕流、输入路由、动画时序和无障碍。#16 不发明新机制——领域系统拥有数据和状态机。

GDD: `design/gdd/ui-hud-chart-interface.md` — 12 sections (8 required + Visual/Audio + UI Requirements + Open Questions), CD-GDD-ALIGN: APPROVED 2026-05-03 (3 non-blocking suggestions applied). 20 ACs, 13 edge cases, 18 tuning knobs, 6 open questions.

### 17. 反馈、特效与音频语义

Semantic feedback language for route selection, repair completion, danger warnings, market purchase, extraction, beacon activation, ambience, and audio cues.

Scope boundary: MVP needs basic feedback; this system becomes a fuller production system during vertical slice.

MVP owner note: minimum visible recovery feedback belongs to `世界修复与解锁`; minimum home-safety feedback belongs to `飞艇家园 Hub`; minimum clarity feedback belongs to `UI / HUD / 航图界面`.

GDD: `design/gdd/feedback-fx-audio.md` - approved 2026-05-15. ADR-0016 accepted 2026-05-15.

### 18. 新手引导与首轮闭环

Guidance for the first complete loop: prepare in the airship, pick route, explore, return, buy or repair, see world response.

Scope boundary: MVP may validate this manually; vertical slice formalizes onboarding.

MVP owner note: this system is not the only owner of the first loop. MVP GDDs for Hub, Route Map, Exploration, World Repair, Settlement/Market, and UI must each include their part of the first-loop handoff.

GDD: `design/gdd/onboarding-first-loop.md` - approved 2026-05-15. ADR-0017 accepted 2026-05-15.

### 19. 完整场景构成与验收

Cross-system scene composition standard for every enterable scene. Defines the required bridge from design to implementation to QA: scene purpose, spatial layout, player behavior, scene physics contract, state variants, visual/audio assets, technical contracts, smoke evidence, and human readability review. It prevents a scene from being treated as complete merely because runtime nodes exist or UI text describes it.

Scope boundary: this system does not own gameplay rules, resources, repair, market, exploration, feedback, persistence, or UI state. It owns the scene-completeness gate and the dual review requirement: Codex review plus user review before release-readiness claims.

GDD: `design/gdd/scene-composition-system.md` - in design 2026-05-24.

### 20. 场景单位物理设计

Scene unit physics standard for authored 2D spaces. Defines horizontal vs vertical scene physics types, movement planes, collision semantics, occlusion/layering, unit scale, special surfaces, dynamic physical behavior tags, behavior conflict priority, and recovery rules for stuck or misleading physical states.

Scope boundary: this system does not own player input, interaction focus, resource rules, exploration rewards, market logic, or repair outcomes. It owns how scene units occupy space and communicate physical behavior. `完整场景构成与验收` consumes this system as its Scene Physics Contract gate.

GDD: `design/gdd/scene-physics-unit-system.md` - in design 2026-05-24.

---

## Categories

| Category | Description | Systems |
|---|---|---|
| **Core** | Foundational contracts and player action surfaces | 内容数据与状态注册表; 平台与会话壳; 玩家移动与交互 |
| **Gameplay** | Main player actions and core loop systems | 飞艇家园 Hub; 飞艇模块与船体状态; 航图与航线规划; 航行与路线风险; 探索 / 搜撤场景; 战斗与威胁处理 |
| **Progression** | Long-term growth and state change | 玩家知识与情报; 世界修复与解锁 |
| **Economy** | Goods, cargo, capacity, and market purchasing | 资源、货物与容量; 空港 / 村镇状态与集市交易 |
| **Persistence** | Save state and continuity | 本地存档与世界状态持久化 |
| **World** | Settlement life and world response | 空港 / 村镇状态与集市交易 |
| **UI** | Player-facing information and decisions | UI / HUD / 航图界面 |
| **Audio / Presentation** | Semantic feedback and audiovisual language | 反馈、特效与音频语义 |
| **Narrative** | Character relationship and light story delivery | 伙伴功能与关系 |
| **Meta** | Onboarding and complete first-session flow | 新手引导与首轮闭环 |

---

## Priority Tiers

| Tier | Definition | Systems |
|---|---|---|
| **MVP** | Required for the first complete loop to function and prove the core fantasy | Systems 1-16 |
| **Vertical Slice** | Required to make the first complete loop feel player-ready rather than manually testable | Systems 17-18 |
| **Alpha** | No new top-level systems planned; expand depth inside approved systems | Additional content, deeper combat/threats, more routes, more settlement stock changes |
| **Full Vision** | No new top-level systems planned; expand breadth and polish | More settlements, routes, partners, exploration variants, audio/visual polish, PC-later expansion |

> **Creative Director Note**: Full onboarding remains Vertical Slice, but MVP systems 7, 9, 11, 13, and 16 must collectively prove a complete first loop. If the player cannot understand and complete `Hub -> 航图 -> 探索 -> 返回 -> 修复`, the MVP does not yet prove the core fantasy.

---

## Dependency Map

### Foundation Layer

1. **内容数据与状态注册表** — foundational static content and IDs used by all gameplay systems.
2. **平台与会话壳** — Desktop application lifecycle and input entry.
3. **本地存档与世界状态持久化** — depends on content data and platform shell.
4. **玩家移动与交互** — depends on platform shell.

### Core Layer

1. **资源、货物与容量** — depends on content data and persistence.
2. **玩家知识与情报** — depends on content data and persistence.
3. **飞艇家园 Hub** — depends on movement/interaction, persistence, and content data.
4. **飞艇模块与船体状态** — depends on airship Hub, resources/cargo, and persistence.
5. **航图与航线规划** — depends on content data, persistence, and player knowledge/intelligence.

### Feature Layer

1. **航行与路线风险** — depends on route planning, ship modules/state, and player knowledge/intelligence.
2. **探索 / 搜撤场景** — depends on resources/cargo, ship modules/state, route risk, and movement/interaction.
3. **战斗与威胁处理** — depends on exploration/extraction, ship modules/state, and resources/cargo.
4. **世界修复与解锁** — depends on resources/cargo, player knowledge/intelligence, persistence, and route planning.
5. **空港 / 村镇状态与集市交易** — depends on world repair/unlocks, resources/cargo, movement/interaction, and persistence.
6. **伙伴功能与关系** — depends on airship Hub, route planning, and player knowledge/intelligence.

### Presentation Layer

1. **UI / HUD / 航图界面** — depends on route planning, modules, resources/cargo, exploration, repair, and settlement/market.
2. **反馈、特效与音频语义** — depends on travel risk, exploration, combat/threats, repair, and UI semantic events.

### Polish / Meta Layer

1. **新手引导与首轮闭环** — depends on Hub, route map, exploration, repair, settlement/market, and UI.

---

## Circular Dependencies

No blocking circular dependencies were accepted into the top-level system graph.

Potential cycles and resolutions:

- **世界修复与解锁 <-> 空港 / 村镇状态与集市交易**: break by making world repair the owner of unlock state, while settlement/market consumes repair flags and presents stalls, stock, and NPC activity.
- **航图与航线规划 <-> 玩家知识与情报**: break by making intelligence the truth source for known/unknown/risk information, while route map only displays, filters, and selects.
- **航行与路线风险 <-> 探索 / 搜撤场景 <-> 战斗与威胁处理**: break by having route risk produce `EncounterContext` or route outcomes, consumed by exploration and threat handling.

---

## Bottleneck Systems

- **内容数据与状态注册表**: all other systems require stable IDs and schemas.
- **本地存档与世界状态持久化**: required for the core promise of permanent world change.
- **资源、货物与容量**: shared by extraction, repair, market purchasing, and airship preparation.
- **航图与航线规划**: central decision surface connecting exploration, risk, repair, and travel.
- **世界修复与解锁**: central pillar system for world response and long-term identity.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|---|---|---|---|
| 内容数据与状态注册表 | Architecture | Can become a God Object if it owns runtime state. | Limit to static content definitions, IDs, schemas, and query contracts. |
| 本地存档与世界状态持久化 | Architecture / Technical | Can become a global state manager or coupling point. | Limit to serialization, deserialization, and migration. Domain systems own state. |
| 资源、货物与容量 | Design | Touches extraction, repair, trade, and preparation; can become overcomplicated. | MVP uses simple materials, supplies, cargo limit, and clear use cases. |
| 航图与航线规划 | Design / UX | Core planning surface can become noisy or unclear. | MVP uses 2 routes, authored risk tags, and clear safe/risky contrast. |
| 航行与路线风险 | Scope | Risk model can grow into simulation. | Use authored tags and encounter tables only. |
| 探索 / 搜撤场景 | Scope | Can grow into many dungeon types. | MVP uses 1 exploration-point template with state variants. |
| 战斗与威胁处理 | Scope | Can become a full combat subgame. | MVP threat-resolution layer only: 1 threat, 1-2 responses, 3 outcomes. |
| 世界修复与解锁 | Design | If feedback is weak, the core fantasy fails. | MVP includes 1 permanent repair outcome with visible route/settlement change. |
| 空港 / 村镇状态与集市交易 | Scope / Design | Market can become a full economy. | MVP uses walk-up stall purchase plus fixed or repair-flag-driven stock changes. |
| UI / HUD / 航图界面 | UX | Too much state can overwhelm desktop UI clarity. | Prioritize route, cargo, risk, repair, and market purchase clarity. |
| 完整场景构成与验收 | Production / UX | Can become paper process if it is not tied to runtime evidence and user review. | Require smoke evidence, screenshots, asset traceability, and explicit user review before scene acceptance. |
| 场景单位物理设计 | Gameplay / Scene Physics | Can become an uncontrolled physics sandbox or cause unreadable collision behavior. | Keep physics tags explicit, require conflict priority, and require recovery paths for stuck states. |

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|---:|---|---|---|---|---|
| 1 | 内容数据与状态注册表 | MVP | Foundation | systems-designer, technical-director | M |
| 2 | 平台与会话壳 | MVP | Foundation | technical-director, godot-specialist | S |
| 3 | 本地存档与世界状态持久化 | MVP | Foundation | systems-designer, technical-director | M |
| 4 | 玩家移动与交互 | MVP | Foundation | game-designer, gameplay-programmer | M |
| 5 | 资源、货物与容量 | MVP | Core | economy-designer, systems-designer | M |
| 6 | 玩家知识与情报 | MVP | Core | systems-designer, narrative-director | M |
| 7 | 飞艇家园 Hub | MVP | Core | game-designer, level-designer, ux-designer | M |
| 8 | 飞艇模块与船体状态 | MVP | Core | systems-designer, gameplay-programmer | M |
| 9 | 航图与航线规划 | MVP | Core | systems-designer, ux-designer | L |
| 10 | 航行与路线风险 | MVP | Feature | systems-designer, game-designer | M |
| 11 | 探索 / 搜撤场景 | MVP | Feature | game-designer, level-designer | L |
| 12 | 战斗与威胁处理 | MVP | Feature | game-designer, gameplay-programmer | M |
| 13 | 世界修复与解锁 | MVP | Feature | systems-designer, game-designer | L |
| 14 | 空港 / 村镇状态与集市交易 | MVP | Feature | economy-designer, level-designer, narrative-director | L |
| 15 | 伙伴功能与关系 | MVP | Feature | narrative-director, systems-designer | M |
| 16 | UI / HUD / 航图界面 | MVP | Presentation | ux-designer, ui-programmer | L |
| 17 | 反馈、特效与音频语义 | Vertical Slice | Presentation | art-director, audio-director, technical-artist | M |
| 18 | 新手引导与首轮闭环 | Vertical Slice | Polish / Meta | ux-designer, game-designer, qa-tester | M |
| 19 | 完整场景构成与验收 | Polish Gate | Production / Scene Design | level-designer, ux-designer, qa-tester, technical-director | M |
| 20 | 场景单位物理设计 | MVP Foundation | Gameplay / Scene Physics | level-designer, gameplay-programmer, qa-tester, technical-director | M |

Effort estimates: S = 1 focused design session; M = 2-3 sessions; L = 4+ sessions.

---

## MVP Thin-Slice Rules

The MVP version of each system must stay within these bounds:

- 1 walkable airship Hub.
- 2 ship modules.
- 1 starting settlement/port.
- 2 routes: 1 safe route and 1 unknown/high-risk route.
- 1 exploration point template.
- 1 threat type.
- 1 scout partner function.
- 1 permanent world repair result.
- Fixed or repair-flag-driven stall goods.
- Local save/load for resources, modules, route state, repair state, and settlement/market state.

---

## Progress Tracker

| Metric | Count |
|---|---:|
| Total systems identified | 20 |
| Design docs started | 20 |
| Design docs reviewed | 18 |
| Design docs approved | 18 |
| Design docs needing revision | 2 |
| MVP systems designed | 16 / 17 |
| Vertical Slice systems designed | 2 / 2 |

---

## Next Steps

- [x] Review and approve this systems enumeration.
- [x] Design MVP-tier systems #1-#15.
- [x] Run `/design-review` on completed GDDs (#1-#15 reviewed, all approved).
- [x] Complete Revision 1 re-review for #11 探索/搜撤场景.
- [x] Design #16 UI/HUD/航图界面 (last MVP system) — COMPLETE 2026-05-03, CD APPROVED.
- [x] Run `/review-all-gdds` for holistic cross-GDD consistency — 2026-05-08, 14 blockers resolved, 25 warnings noted.
- [x] Run `/review-all-gdds` re-check — 2026-05-08, **PASS** (0 blockers, 15 warnings, 17/25 resolved).
- [x] Design #17 反馈、特效与音频语义 — COMPLETE 2026-05-15.
- [x] Design-review #17 反馈、特效与音频语义 — APPROVED 2026-05-15.
- [x] Accept ADR-0016 反馈、特效与音频语义 — ACCEPTED 2026-05-15.
- [x] Design #18 新手引导与首轮闭环 — COMPLETE 2026-05-15.
- [x] Design-review #18 新手引导与首轮闭环 — APPROVED 2026-05-15.
- [x] Accept ADR-0017 新手引导与首轮闭环 — ACCEPTED 2026-05-15.
- [ ] Review #19 完整场景构成与验收 with Codex and user before using it as a release gate.
- [ ] Review and approve #20 场景单位物理设计 as an MVP Foundation retrofit before using physical world exploration as a bottom-layer gameplay contract.
- [ ] Run `/gate-check technical-setup` when Systems Design artifacts are complete.
- [ ] Prototype the highest-risk loop: `Hub -> 航图 -> 探索 -> 返回 -> 修复 -> 存档恢复`.
