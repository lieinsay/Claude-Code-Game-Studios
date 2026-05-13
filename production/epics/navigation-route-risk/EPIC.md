# Epic: Navigation / Route Risk Resolution

> **Layer**: Core
> **GDD**: design/gdd/navigation-route-risk.md
> **Architecture Module**: Autoload #10 — NavigationManager
> **Status**: Complete
> **Stories**: 8 (001-008)

## Overview

实现《云海织航》的航行阶段风险解析引擎——接收 ChartManager 在出航确认后发出的 `route_committed` 信号，读取航线的静态风险标签（safe/storm/low-visibility/raider 等）和飞艇状态快照（侦察模块效率、船体波段、燃料/能量），在航行过程中将风险标签逐步解析为结构化的 EncounterContext（威胁点配置、探索点参数、资源生成种子），并将航行结果输出至下游系统。核心是 5 个公式：航行持续时间、遭遇检查时机、侦察预览范围、伤害计算、隐藏标签揭示。系统基于时间推进模型（非物理飞行模拟）进行离散风险解析——每个时间 tick 根据当前位置和风险标签概率判定是否触发遭遇，侦察模块效率影响提前预警范围。支持动态船体波段转换（Option B：航行中船体受损时波段实时变化，影响后续遭遇的难度和选项）。MVP 仅实现玩家直接出航路径；NPC 航线与委托货运路线为 Phase 3+ 扩展（架构已预留 VoyageContext → EncounterContext 的类型无关接口）。系统不拥有航图渲染（UIManager）、不拥有探索点内容生成（Exploration）、不拥有战斗结算（Combat）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0010: EncounterContext Type | NavigationManager Autoload #10；EncounterContext 类型定义（威胁配置、探索参数、资源种子）；航行风险标签→遭遇解析算法；侦察模块效率影响预警范围；动态船体波段转换 (Option B)；VoyageContext 类型无关接口预留 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-navigation-001 | Voyage risk resolution: authored tags → EncounterContext via time-based progression | ADR-0010 ✅ |
| TR-navigation-002 | 5 formulas: voyage duration, encounter check timing, scout preview, damage, hidden tag reveal | ADR-0010 ✅ (data bridge; formula implementation details partial) |
| TR-navigation-003 | Dynamic hull band transitions mid-voyage (Option B) | ADR-0010 ✅ (data bridge; band transition details partial) |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/navigation-route-risk.md` are verified
- All Logic and Integration stories have passing test files in `tests/integration/navigation/`
- route_committed signal → EncounterContext resolution pipeline works end-to-end
- 5 formulas produce correct deterministic results with mock inputs
- Scout module efficiency correctly affects encounter preview range
- Dynamic hull band transitions work mid-voyage (band changes cascade to subsequent encounters)
- EncounterContext contract is correctly consumed by Exploration (#11) and Combat (#12)
- Navigation snapshot persists/restores via Persistence (progress.exploration)

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Voyage State Machine & Preflight Checks](story-001-voyage-state-machine-preflight.md) | Logic | TR-navigation-001 | ADR-0010 |
| 002 | [Voyage Duration & Encounter Check Timing](story-002-voyage-duration-check-timing.md) | Logic | TR-navigation-002 | ADR-0010 |
| 003 | [Scout Preview Window & Hidden Tag Reveal](story-003-scout-preview-hidden-tag-reveal.md) | Logic | TR-navigation-002 | ADR-0010 |
| 004 | [Damage Accumulation & Dynamic Hull Band Transitions](story-004-damage-hull-band-transitions.md) | Logic | TR-navigation-002, TR-navigation-003 | ADR-0010 |
| 005 | [Encounter Resolution & EncounterEntry Dispatch](story-005-encounter-resolution-entry-dispatch.md) | Logic | TR-navigation-001, TR-navigation-002 | ADR-0010 |
| 006 | [EncounterContext Production & voyage_completed Signal](story-006-encounter-context-production-signal.md) | Integration | TR-navigation-001 | ADR-0010 |
| 007 | [Voyage Snapshot Persistence](story-007-voyage-snapshot-persistence.md) | Integration | TR-navigation-001, TR-navigation-002 | ADR-0010, ADR-0003 |
| 008 | [Edge Cases & Defensive Error Handling](story-008-edge-cases-defensive-error-handling.md) | Integration | TR-navigation-001, TR-navigation-002, TR-navigation-003 | ADR-0010, ADR-0002, ADR-0006 |

**Summary**: 5 Logic + 3 Integration stories

## Next Step

All stories created. Core Layer 5/5 complete. Ready for Feature Layer epic decomposition.
