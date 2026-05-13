# Epic: Exploration / Scavenge Scenario

> **Layer**: Feature
> **GDD**: design/gdd/exploration-scavenge-scenario.md
> **Architecture Module**: Autoload #11 — ExplorationManager
> **Status**: Complete
> **Stories**: 6 (001-006)

## Overview

实现《云海织航》核心循环第三步——ExplorationManager Autoload #11。管理探索/搜撤场景的 4 阶段状态机（ARRIVING → EXPLORING → EXTRACTING → DEPARTED），消费 Navigation (#10) 的 EncounterContext 以决定入场模式（安全抵达 vs 迫降），执行 6 个核心公式（搜索产出投骰、威胁触发判定、侦察预览映射、撤离损耗结算、状态变体转换、情报点产出），管理 6 个搜索点 + 2 个情报点 + 2+ 威胁点 + 1 撤离锚点的交互生命周期，并在撤离成功后结算资源、情报和船体后果。探索点模板（MVP: 云观站废墟，50×35 单位，4 区域辐条式）由 Registry (#1) 数据驱动。核心差异化点：自由搜索保证（空结果不消耗搜索次数）、撤离是玩家的判断而非被迫逃命（无全局计时器）、侦察效率影响威胁预览精度、守卫威胁在 #12 不可用时惰性降级。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0013: Exploration/Scavenge System | ExplorationManager Autoload #11；4-phase state machine (ARRIVING→EXPLORING→EXTRACTING→DEPARTED)；Dictionary 后端存储；6 个核心公式；10 个 typed 信号；EncounterContext 消费（ADR-0010 合同）；逻辑/场景分离（Autoload 不含节点引用）；ADR-0003 progress.exploration snapshot；数据驱动探索点模板 (Registry #1) | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-exploration-001 | 4-zone radial template: 50×35 units, 4-phase session | ADR-0013 |
| TR-exploration-002 | 6 search points with free-search rule; 2 intel points; 2+ threat points | ADR-0013 |
| TR-exploration-003 | Extraction: player-judged (no timer), λ_success/λ_forced with Unique item protection | ADR-0013 |

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Exploration State Machine & Phase Transitions](story-001-state-machine-phase-transitions.md) | Logic | TR-exploration-001 | ADR-0013 |
| 002 | [Search, Scavenge & Intel Formulas](story-002-search-scavenge-intel-formulas.md) | Logic | TR-exploration-002 | ADR-0013 |
| 003 | [Threat Triggering, Scout Preview & Environmental Handling](story-003-threat-triggering-scout-preview.md) | Logic | TR-exploration-002 | ADR-0013 |
| 004 | [EncounterContext Consumption & ARRIVING Entry](story-004-encounter-context-arriving-entry.md) | Integration | TR-exploration-001 | ADR-0013 |
| 005 | [Extraction, Settlement & State Variant Transition](story-005-extraction-settlement-state-variant.md) | Integration | TR-exploration-003 | ADR-0013 |
| 006 | [Persistence, Session Recovery & Edge Cases](story-006-persistence-session-recovery-edge-cases.md) | Integration | TR-exploration-001, TR-exploration-002, TR-exploration-003 | ADR-0013 |

**Summary**: 3 Logic + 3 Integration stories

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All 26 acceptance criteria from `design/gdd/exploration-scavenge-scenario.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/exploration/` and `tests/integration/exploration/`
- 4-phase state machine transitions correctly under all inputs; all 7 invalid transitions rejected
- F-11-01 search_yield() correctly applies zone weights + free-search guarantee (empty → search_consumed=false)
- F-11-02 threat_trigger(): environmental 100% trigger, guard ~70% proximity + 100% interaction
- F-11-03 scout_preview_level() correctly maps all η_scout values to 3 preview tiers
- F-11-04 extraction_loss_settlement(): λ=0.08/0.25 correct; Unique items protected; per-stack min 1 retained
- F-11-05 state_variant_transition(): all 8 transitions correct
- F-11-06 intel_yield(): fixed output of 1 Unique intel item per intel point
- EncounterContext fallback correctly handles all 5 failure conditions
- Multi-threat simultaneous trigger resolved deterministically (env > guard, near > far, dict order)
- Extraction channel 2.5s, interruptible by threats, progress resets on interrupt
- Pool 5 capacity gating correctly triggers trade-off UI (EC-11-04/05)
- Guard threats gracefully inert when CombatManager (#12) unavailable (EC-11-12)
- Session recovery: tab close during EXPLORING → restores; during EXTRACTING → resets to anchor
- DEPARTED settlement: atomic batch transfer + 4-retry fallback + manual retry button
- ExplorationManager is independently testable with mock #5/#6/#8/#10/#12 injection

## Next Step

All stories created. Feature Layer 3/5 unblocked epics complete. Next: settlement-market #14 (ADR-0014 deferred), partner-relationships #15 (ADR-0015 deferred).
