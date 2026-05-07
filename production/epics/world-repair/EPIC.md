# Epic: World Repair & Unlock

> **Layer**: Feature
> **GDD**: design/gdd/world-repair-unlock.md
> **Architecture Module**: Autoload #13 — WorldRepair
> **Status**: In Progress
> **Stories**: 6 (001-006)

## Overview

实现《云海织航》核心推进循环的收束点——WorldRepair Autoload #13。管理修复节点的三态状态机（unrevealed → known → repaired），消费 ResourcesManager (#5) 的 commit_deposit 终态操作执行分批提交算法，在材料集齐时触发 known→repaired 转换，并通过 repair_completed 信号 fan-out 到 6 个下游系统（#3 存档检查点、#6 能力解锁、#9 航线增强、#14 NPC 状态、#17 视觉锚点、UI toast）。MVP 规模为 1 个修复节点（天礁灯塔 starlight_dock），修复后解锁 1 条航线 + 1 个能力，直接驱动世界视觉变化（灯塔重亮、光束、粒子）。核心差异化点：物理到达优先于情报门控——玩家总是可以与修复节点交互，知识状态仅影响 UI 提示精度。分批提交模型支撑"每次归来都修补一点"的 Pillar 2 幻想。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0011: World Repair State Machine | WorldRepair Autoload #13；3-state state machine (unrevealed→known→repaired)；Dictionary 后端存储；validate_deposit 5 种 violation；submit_deposit 原子提交；repair_completed fan-out 6 路下游触发链；Registry 驱动修复节点定义；ADR-0003 progress.world-repair snapshot | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-repair-001 | 1 repair node: starlight_dock, 3-state machine (unrevealed → known → repaired) | ADR-0011 |
| TR-repair-002 | Batch deposit with deposit_validation guarding excess/invalid materials | ADR-0011 |
| TR-repair-003 | Repair completion triggers: route unlock + hazard reduction + ability unlock + world feedback | ADR-0011 |

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Repair State Machine & Node Lifecycle](story-001-repair-state-machine-node-lifecycle.md) | Logic | TR-repair-001 | ADR-0011 |
| 002 | [Deposit Validation & Batch Commit](story-002-deposit-validation-batch-commit.md) | Logic | TR-repair-002 | ADR-0011 |
| 003 | [Repair Progress, Completion & Route Enhancement Formulas](story-003-formulas-progress-completion-enhancement.md) | Logic | TR-repair-002, TR-repair-003 | ADR-0011 |
| 004 | [Signal Events & Downstream Trigger Chain](story-004-signal-events-downstream-chain.md) | Integration | TR-repair-003 | ADR-0011 |
| 005 | [Persistence & State Recovery](story-005-persistence-state-recovery.md) | Integration | TR-repair-001, TR-repair-002 | ADR-0011 |
| 006 | [Edge Cases, MVP Visual/Audio & Defensive Handling](story-006-edge-cases-visual-audio-defensive.md) | Integration | TR-repair-001, TR-repair-002, TR-repair-003 | ADR-0011 |

**Summary**: 3 Logic + 3 Integration stories

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/world-repair-unlock.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/world-repair/` and `tests/integration/world-repair/`
- 3-state FSM transitions correctly under all inputs; all 4 invalid transitions rejected
- validate_deposit() correctly detects all 5 violation types
- submit_deposit() atomic chain (validate→commit→counter→progress→completion) works correctly
- repair_progress() and repair_completion() formulas produce correct deterministic results
- repair_completed signal correctly fans out to 6 downstream systems
- ADR-0003 progress.world-repair snapshot correctly serializes/deserializes all state
- Batch deposit mid-save/load preserves deposited counter integrity
- MVP visual feedback: sprite switch + modulate breathing + light beam + particles function correctly
- WorldRepair is independently testable with mock #5/#6/#9 injection

## Next Step

All stories created. Feature Layer 2/3 unblocked epics complete. Next: blocked epics — exploration-scavenge #11 (ADR-0013 deferred), settlement-market #14 (ADR-0014 deferred), partner-relationships #15 (ADR-0015 deferred).
