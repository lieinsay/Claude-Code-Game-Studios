# Systems Index: 云海织航

> **Status**: Draft
> **Created**: 2026-04-26
> **Last Updated**: 2026-04-29
> **Source Concept**: `design/gdd/game-concept.md`
> **Art Bible**: `design/art/art-bible.md`
> **Review Mode**: Full

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
| 4 | 玩家移动与交互 | Core | MVP | Approved | 2026-04-29 | 平台与会话壳 |
| 5 | 资源、货物与容量 | Economy | MVP | Not Started | — | 内容数据与状态注册表; 本地存档与世界状态持久化 |
| 6 | 玩家知识与情报 | Progression | MVP | Not Started | — | 内容数据与状态注册表; 本地存档与世界状态持久化 |
| 7 | 飞艇家园 Hub | Gameplay | MVP | Not Started | — | 玩家移动与交互; 本地存档与世界状态持久化; 内容数据与状态注册表 |
| 8 | 飞艇模块与船体状态 | Gameplay | MVP | Not Started | — | 飞艇家园 Hub; 资源、货物与容量; 本地存档与世界状态持久化 |
| 9 | 航图与航线规划 | Gameplay | MVP | Not Started | — | 内容数据与状态注册表; 本地存档与世界状态持久化; 玩家知识与情报 |
| 10 | 航行与路线风险 | Gameplay | MVP | Not Started | — | 航图与航线规划; 飞艇模块与船体状态; 玩家知识与情报 |
| 11 | 探索 / 搜撤场景 | Gameplay | MVP | Not Started | — | 资源、货物与容量; 飞艇模块与船体状态; 航行与路线风险; 玩家移动与交互 |
| 12 | 战斗与威胁处理 | Gameplay | MVP | Not Started | — | 探索 / 搜撤场景; 飞艇模块与船体状态; 资源、货物与容量 |
| 13 | 世界修复与解锁 | Progression | MVP | Not Started | — | 资源、货物与容量; 玩家知识与情报; 本地存档与世界状态持久化; 航图与航线规划 |
| 14 | 空港 / 村镇状态与集市交易 | World / Economy | MVP | Not Started | — | 世界修复与解锁; 资源、货物与容量; 玩家移动与交互; 本地存档与世界状态持久化 |
| 15 | 伙伴功能与关系 | Narrative / Gameplay | MVP | Not Started | — | 飞艇家园 Hub; 航图与航线规划; 玩家知识与情报 |
| 16 | UI / HUD / 航图界面 | UI | MVP | Not Started | — | 航图与航线规划; 飞艇模块与船体状态; 资源、货物与容量; 探索 / 搜撤场景; 世界修复与解锁; 空港 / 村镇状态与集市交易 |
| 17 | 反馈、特效与音频语义 | Audio / Presentation | Vertical Slice | Not Started | — | 航行与路线风险; 探索 / 搜撤场景; 战斗与威胁处理; 世界修复与解锁; UI / HUD / 航图界面 |
| 18 | 新手引导与首轮闭环 | Meta | Vertical Slice | Not Started | — | 飞艇家园 Hub; 航图与航线规划; 探索 / 搜撤场景; 世界修复与解锁; 空港 / 村镇状态与集市交易; UI / HUD / 航图界面 |

---

## System Descriptions

### 1. 内容数据与状态注册表

Static content definitions, IDs, schemas, content domains, validation diagnostics, and query contracts for resources, cargo, modules, routes, locations, repair nodes, market goods, partners, threats, and intel.

Scope boundary: this system does not own mutable runtime state.

### 2. 平台与会话壳

Web-first application shell: loading, start/continue, audio activation, tab focus recovery, keyboard/mouse entry points, and session lifecycle.

### 3. 本地存档与世界状态持久化

Save/load persistence for resources, modules, route state, repair state, settlement state, market goods state, exploration state, and settings.

Scope boundary: persistence serializes and restores domain-owned state; it must not become the runtime state manager.

### 4. 玩家移动与交互

Movement, reachability, collision/reach checks, interaction focus, and the Use entry point for airship, settlement, market stall, and exploration interactions.

Scope boundary: concrete consequences such as buying, harvesting, repairing, or installing modules belong to the owning domain system.

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

Map-style route planning: safe route, unknown/high-risk route, nodes, route selection, risk reading, route availability, and planning feedback.

Scope boundary: the route map displays and selects routes; it does not own discovery state.

### 10. 航行与路线风险

Authored risk tags, route outcomes, encounter tables, safety/risk difference, return margin, and travel consequences.

Scope boundary: this system produces route outcomes or `EncounterContext`; it does not implement combat or exploration rules directly.

### 11. 探索 / 搜撤场景

One short exploration-point template for search, risk judgment, carrying choices, and extraction. The MVP template can use state variants but not a second dungeon rhythm.

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

One scout partner function and one shallow relationship feedback beat. The scout reveals risk or resource information and gives the airship a human relationship anchor.

Scope boundary: no crew collection, party management, affection ladder, or event tree in MVP.

Creative constraint: the scout partner must have one memorable identity beat and one persistent relationship memory so Pillar 5 is visible in MVP.

### 16. UI / HUD / 航图界面

HUD, route map, preparation screens, cargo/extraction UI, market purchasing UI, repair feedback UI, and core state display.

### 17. 反馈、特效与音频语义

Semantic feedback language for route selection, repair completion, danger warnings, market purchase, extraction, beacon activation, ambience, and audio cues.

Scope boundary: MVP needs basic feedback; this system becomes a fuller production system during vertical slice.

MVP owner note: minimum visible recovery feedback belongs to `世界修复与解锁`; minimum home-safety feedback belongs to `飞艇家园 Hub`; minimum clarity feedback belongs to `UI / HUD / 航图界面`.

### 18. 新手引导与首轮闭环

Guidance for the first complete loop: prepare in the airship, pick route, explore, return, buy or repair, see world response.

Scope boundary: MVP may validate this manually; vertical slice formalizes onboarding.

MVP owner note: this system is not the only owner of the first loop. MVP GDDs for Hub, Route Map, Exploration, World Repair, Settlement/Market, and UI must each include their part of the first-loop handoff.

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
2. **平台与会话壳** — Web application lifecycle and input entry.
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
| UI / HUD / 航图界面 | UX | Too much state can overwhelm Web UI. | Prioritize route, cargo, risk, repair, and market purchase clarity. |

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
| Total systems identified | 18 |
| Design docs started | 4 |
| Design docs reviewed | 4 |
| Design docs approved | 4 |
| MVP systems designed | 4 / 16 |
| Vertical Slice systems designed | 0 / 2 |

---

## Next Steps

- [ ] Review and approve this systems enumeration.
- [ ] Design MVP-tier systems first with `$design-system [system-name]`.
- [ ] Start with `$design-system 内容数据与状态注册表`.
- [ ] Run `$design-review` on each completed GDD.
- [ ] Run `$review-all-gdds` after all MVP GDDs are authored.
- [ ] Run `$gate-check technical-setup` when Systems Design artifacts are complete.
- [ ] Prototype the highest-risk loop early: `Hub -> 航图 -> 探索 -> 返回 -> 修复 -> 存档恢复`.
