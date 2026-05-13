# Epic: Modules & Hull State

> **Layer**: Core
> **GDD**: design/gdd/airship-modules-hull-state.md
> **Architecture Module**: Autoload #8 — ModuleHullManager
> **Status**: **Complete**
> **Stories**: 8 (001-008) — Complete

## Overview

实现《云海织航》的飞艇机械定制层与适航状态层——定义 MVP 两类核心模块（侦察模块 Scout、货仓/维修模块 Cargo）的安装/卸下/损伤三态、效果计算（侦察效率 η ∈ [0, 1.0]，货仓容积加成）、船体完整性 4 波段模型（intact → damaged → critical → destroyed，η 从 1.0 → 0 递减）以及载重适航判定。每个模块维护双域模型：功能域（actual_state: installed/damaged/empty, efficiency）和外观域（visible_state: installed/damaged/unchecked/empty）。2 个模块槽位均可自由选择模块类型（双侦察、双货仓、或一侦察一货仓）。系统从 Hub 接收槽位状态查询和安装/卸下请求，从 ResourcesManager 接收当前货舱装载质量和容量值，向 Hub 返回模块效果，向 Navigation 提供载重适航判定 (can_depart)，向 Persistence 导出模块安装与船体损伤快照。swap_module 为两阶段语义的原子替换操作（验证旧模块、净消耗与货舱状态后再安装新模块），防止模块丢失。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0009: Module / Hull System | ModuleHullManager Autoload #8；2 槽位 + 双域模型；4 波段船体完整性；max(per-band) 伤害模型；swap_module 两阶段语义；can_depart 载重适航判定 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-modules-001 | 2 module slots: scout module, cargo/repair module | ADR-0009 ✅ |
| TR-modules-002 | Dual-field model: functional field + cosmetic field per module | ADR-0009 ✅ |
| TR-modules-003 | 4 hull bands with efficiency coefficients; damage = max(per-band) not sum | ADR-0009 ✅ |

## Definition of Done

This epic is complete when:
- [x] All stories are implemented, reviewed, and closed via `/story-done`
- [x] All acceptance criteria from `design/gdd/airship-modules-hull-state.md` are verified
- [x] All Logic and Integration stories have passing C# test files in `tests/unit/modules/` and `tests/integration/modules/`
- [x] Module install/swap/remove operations work correctly on both slots
- [x] Dual-field model: functional damage doesn't change cosmetic state, and vice versa
- [x] 4 hull bands transition correctly on damage; η coefficients apply to relevant systems
- [x] swap_module two-phase semantics prevent module loss (phase 1 validation, phase 2 atomic replacement)
- [x] can_depart correctly gates departure based on hull band + cargo mass
- [x] Module snapshot persists/restores via Persistence (ADR-0003)
- [x] apply_hull_damage and apply_module_damage interfaces work for Combat system (ADR-0018)

## Stories

| # | Story | Type | TRs | ADRs |
|---|-------|------|-----|------|
| 001 | [Module Slot State Machine & Dual-Field Model](story-001-slot-state-machine-dual-field.md) | Logic | TR-modules-001, TR-modules-002 | ADR-0009 |
| 002 | [Module Swap Two-Phase Operation](story-002-module-swap-two-phase.md) | Logic | TR-modules-001 | ADR-0009 |
| 003 | [Hull Integrity, Bands & Scars](story-003-hull-integrity-bands-scars.md) | Logic | TR-modules-003 | ADR-0009 |
| 004 | [Furnace Capacity & Departure Readiness](story-004-furnace-capacity-departure-readiness.md) | Logic | TR-modules-001, TR-modules-003 | ADR-0009 |
| 005 | [Cargo Bay Effective Volume & Trapped Goods](story-005-cargo-volume-trapped-goods.md) | Integration | TR-modules-001 | ADR-0009 |
| 006 | [Module Signal Contract](story-006-module-signal-contract.md) | Integration | TR-modules-001, TR-modules-002 | ADR-0009, ADR-0002 |
| 007 | [Module Snapshot Persistence](story-007-module-snapshot-persistence.md) | Integration | TR-modules-001, TR-modules-003 | ADR-0009, ADR-0003 |
| 008 | [Scout Module Acquisition & Combat Damage Interfaces](story-008-scout-acquisition-combat-damage.md) | Integration | TR-modules-001 | ADR-0009, ADR-0018 |

**Summary**: 4 Logic + 4 Integration stories

## Next Step

Epic #8 complete. Next production step from `docs/reference/production-flowchart.md`: continue #9 Chart Route Planning; BUG-005 scene reachability has been fixed, while richer downstream runtime UI remains later scope.
