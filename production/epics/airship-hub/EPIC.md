# Epic: Airship Hub

> **Layer**: Core
> **GDD**: design/gdd/airship-hub.md
> **Architecture Module**: Autoload #7 — HubManager
> **Status**: Complete
> **Stories**: 8 (001-008) — Implemented and verified

## Overview

实现《云海织航》的飞艇家园场景——一个横版剖面可步行飞艇内部（2-4 个小舱室/区域），10 个 MVP 站点（情报台、模块接口、货舱、工作台、伙伴驻点、休息处、储物架、航线桌、舱门出口、船体状态面板）。作为核心循环的起点和终点，Hub 承载出航前整备（查看情报、调整货物、检查模块、伙伴简报）和返航后归位（拆包战利品、存入仓库、修复船体、看见世界变化在船上留下痕迹）。玩家在 Hub 中使用 WASD + Click-to-Move 移动，通过 Approach+E 交互模式与各站点交互；站点解锁受模块安装状态门控（Room Gating）。Hub 维护 2 种离开模式：航线出航（Chart → route_committed → 航行过渡）和直接出航（Departure 按钮 + 确认门）。场景在 `scene_ready` 信号后执行 arrive 动画（取决于离开方式：探索返回→暖金渐亮 2.1s，首航→标准入场）。本系统不拥有模块状态、资源库存或航线数据——它消费这些系统的查询接口和信号，通过 Hub 站点呈现。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Autoload/Scene Boot Order | Hub 场景在 Phase 4 (scene_ready) 加载；HubManager Autoload #7 管理站点注册和交互路由 | LOW |
| ADR-0002: Signal Communication Protocol | `departure_initiated`, `station_activated` 等信号遵循 typed params + sync emit 协议 | LOW |
| ADR-0003: Save System | Hub 站点状态、伙伴位置通过 `progress.hub` 快照包持久化 | LOW |
| ADR-0004: InteractionHandler | Hub 内所有交互站点继承 @abstract InteractionHandler；InteractionRegistry 管理焦点仲裁 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-hub-001 | Walkable side-view airship interior with 10 MVP stations | ADR-0001 ✅ |
| TR-hub-002 | 2 departure modes: chart departure, direct departure | ADR-0001 ✅ |
| TR-hub-003 | Room gating: stations unlock via module installation | ADR-0009 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/airship-hub.md` are verified
- All Logic and Integration stories have passing test files in `tests/integration/hub/`
- All Visual/Feel and UI stories (Hub scene, station interaction, arrive/depart animations) have evidence docs with sign-off in `production/qa/evidence/`
- 10 MVP stations are walkable and interactable via Approach+E
- Room gating: locked stations show lock icon + tooltip with unlock condition
- Chart departure flow: route_committed → Confirmation Gate → ink-spread lock → voyage
- Direct departure: available when route was previously verified
- Hub arrival animation varies correctly by departure type
- Hub snapshot persists/restores via `progress.hub`

## Stories

| # | Story | Type | TRs | ADRs |
|---|-------|------|-----|------|
| 001 | [Hub Scene Foundation & Docking State Machine](story-001-hub-scene-state-machine.md) | Logic | TR-hub-001 | ADR-0001, ADR-0004 |
| 002 | [Station Registration & Interaction Routing](story-002-station-registration-interaction.md) | Logic | TR-hub-001 | ADR-0001, ADR-0004 |
| 003 | [Room Gating & Module Slot Display](story-003-room-gating-module-slots.md) | Logic | TR-hub-003 | ADR-0009, ADR-0001 |
| 004 | [Departure Modes & Confirmation Gate](story-004-departure-modes-confirmation-gate.md) | Logic | TR-hub-002 | ADR-0001, ADR-0002 |
| 005 | [Arrival Flow & State Continuity](story-005-arrival-flow-state-continuity.md) | Logic | TR-hub-001 | ADR-0001, ADR-0002 |
| 006 | [Life Trace Anchors](story-006-life-trace-anchors.md) | Logic | TR-hub-001 | ADR-0001, ADR-0003 |
| 007 | [Signal Contract & HUD Integration](story-007-signal-contract-hud-integration.md) | Integration | TR-hub-001 | ADR-0002, ADR-0001, ADR-0012 |
| 008 | [Scene Persistence & Transition Lifecycle](story-008-scene-persistence-transition.md) | Integration | TR-hub-001 | ADR-0001, ADR-0003, ADR-0006 |

**Summary**: 6 Logic + 2 Integration stories — all complete

## Completion Evidence

- `src/core/hub/AirshipHub.cs` implements HubManager, HubStation, docking state, room gating, departure confirmation, arrival continuity, trace anchors, typed signal contracts, and `progress.airship` snapshot lifecycle.
- `tests/integration/hub/DockingStateMachineTest.csproj` — PASS
- `tests/integration/hub/StationRegistryTest.csproj` — PASS
- `tests/integration/hub/RoomGatingTest.csproj` — PASS
- `tests/integration/hub/DepartureModesTest.csproj` — PASS
- `tests/integration/hub/ArrivalFlowTest.csproj` — PASS
- `tests/integration/hub/LifeTraceAnchorsTest.csproj` — PASS
- `tests/integration/hub/SignalContractTest.csproj` — PASS
- `tests/integration/hub/PersistenceTransitionTest.csproj` — PASS
- `dotnet build CloudWeaverVoyage.sln` — PASS
