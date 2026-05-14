# Epic: Combat / Threat Resolution

> **Layer**: Feature
> **GDD**: design/gdd/combat-threat-handling.md
> **Architecture Module**: Autoload #12 — CombatManager
> **Status**: In Progress
> **Stories**: 6 (001-006)

## Overview

实现《云海织航》探索循环中的薄层威胁结算引擎 — CombatManager Autoload #12。消费 Exploration (#11) 在守卫威胁触发时传入的 `threat_context`，驱动一个 4 态微观状态机（IDLE → AWAITING_RESPONSE → PROCESSING → RESOLVED），以数据驱动方式解析威胁配置（从 Registry 经 EncounterContext → #11 → encounter_params 传入），产出 `combat_result` 结构体并级联写入 #8 (Module/Hull) 和 #5 (Resources)。核心是：1 种威胁类型（guard）、3 种玩家响应（应急处理 / 硬扛 / 撤退）、3 种结算结果（suppressed / tanked / retreated）。威胁状态通过 #11 探索点快照持久化（ADR-0003 `progress.exploration`），CombatManager 本身无独立持久化层。设计核心差异化点：决策呼吸（Decision Breath）——威胁触发后探索暂停，不限时，玩家基于当前船体状态和随身物资做出判断，无倒计时压力。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0018: Threat Resolution System | CombatManager Autoload #12；4-state micro state machine；resolve_threat() single entry point；3 response options；combat_result contract；threat queue FIFO max depth 4；data-driven threat configuration；threat persistence via #11 exploration snapshot | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-combat-001 | 1 threat type with 3 response options (emergency_handling / tank / retreat) | ADR-0018 ✅ |
| TR-combat-002 | Decision breath — player chooses without real-time pressure | ADR-0018 ✅ |
| TR-combat-003 | 3 outcomes — damaged, knocked back, retreat (retreat_flagged → λ_forced=0.25) | ADR-0018 ✅ |

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Combat State Machine & Threat Queue](story-001-combat-state-machine-threat-queue.md) | Logic | TR-combat-001 | ADR-0018 — Complete |
| 002 | [Response Resolution & Settlement Sequence](story-002-response-resolution-settlement-sequence.md) | Logic | TR-combat-001, TR-combat-002 | ADR-0018 — Complete |
| 003 | [Damage, Module & Knockback Formulas](story-003-damage-module-knockback-formulas.md) | Logic | TR-combat-003 | ADR-0018 |
| 004 | [combat_result Contract & Signal Events](story-004-combat-result-contract-signal-events.md) | Integration | TR-combat-003 | ADR-0018 |
| 005 | [Data-Driven Threat Configuration](story-005-data-driven-threat-configuration.md) | Integration | TR-combat-001 | ADR-0018 |
| 006 | [Edge Cases & Defensive Error Handling](story-006-edge-cases-defensive-handling.md) | Integration | TR-combat-001, TR-combat-002, TR-combat-003 | ADR-0018 |

**Summary**: 3 Logic + 3 Integration stories

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/combat-threat-handling.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/combat/` and `tests/integration/combat/`
- 4-state micro FSM transitions correctly under all inputs
- resolve_threat() returns correct combat_result for all 3 response types
- 5 formulas produce correct deterministic results
- Threat queue FIFO works correctly with overflow handling
- combat_result contract is correctly consumed by Exploration (#11)
- Downstream cascades correctly write to #5 (consume_in_combat) and #8 (apply_hull_damage, apply_module_damage)
- CombatManager is independently testable with mock #5/#8/#11 injection

## Next Step

Story 001 and Story 002 are complete with passing C# unit evidence. Continue with Story 003 + Story 004 for damage/module/knockback formulas and combat_result signal integration.

## Implementation Evidence

- 2026-05-14: Story 001 + Story 002 implemented in `src/core/combat/CombatManager.cs`.
- Test evidence:
  - `dotnet run --project tests/unit/combat/StateMachineTest.csproj -p:UseSharedCompilation=false` — 7/7 PASS.
  - `dotnet run --project tests/unit/combat/ResponseResolutionTest.csproj -p:UseSharedCompilation=false` — 7/7 PASS.
