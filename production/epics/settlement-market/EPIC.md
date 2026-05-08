# Epic: Settlement Market & Port Village Economy

> **Layer**: Feature
> **GDD**: design/gdd/port-village-market.md
> **Architecture Module**: Autoload #14 — SettlementManager
> **Status**: In Progress
> **Stories**: 6 (001-006)

## Overview

实现《云海织航》空港村镇集市交易系统——SettlementManager Autoload #14。管理琉璃港 (Glass Harbor) 4 个固定摊位与 4 位 NPC 的三层状态机（定居点 dormant→recovering→active，摊位 closed→open_basic，NPC absent→idle），消费 WorldRepair (#13) 的 repair_completed 信号驱动摊位解锁与 NPC 恢复，委托 ResourcesManager (#5) 的 validate_purchase / execute_purchase 执行购买原子操作。MVP 提供 6 种商品（2 通用补给 + 3 独占风味补给 + 1 情报），货币来自 Exploration (#11) 搜索点产出。核心差异化点：修复不可逆——状态只向前推进；默认杂货摊始终可用确保任何修复前至少一个购买点；商品通过 local_identity_tag 绑定摊位在地身份——这不是通用商店而是"阅读一个地方的自传"。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0014: Settlement Market System | SettlementManager Autoload #14；3-tier state machine (dormant→recovering→active, closed→open_basic, absent→idle)；Dictionary 后端存储；购买委托 #5 validate/execute；repair_completed 信号消费驱动摊位解锁；Registry 驱动摊位/商品/NPC 定义；ADR-0003 progress.settlement-market snapshot；6 种 MVP 商品 + 4 个固定摊位 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-settlement-001 | 3-tier state machine: settlement, stall, NPC + 4 stalls with NPC operators | ADR-0014 |
| TR-settlement-002 | Purchase flow: validate→execute delegation to #5, F.1 total_cost formula, 6 MVP goods | ADR-0014 |
| TR-settlement-003 | Repair-driven unlock: F.2 unlock check, F.3 activity aggregation, repair_completed consumption, ADR-0003 snapshot | ADR-0014 |

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Settlement State Machine & Stall Lifecycle](story-001-state-machine-stall-lifecycle.md) | Logic | TR-settlement-001 | ADR-0014 |
| 002 | [Purchase Flow & Price Formula](story-002-purchase-flow-price-formula.md) | Logic | TR-settlement-002 | ADR-0014 |
| 003 | [Repair-Driven Unlock & NPC State](story-003-repair-unlock-npc-state.md) | Logic | TR-settlement-002, TR-settlement-003 | ADR-0014 |
| 004 | [Repair Signal & Resources Integration](story-004-signal-resources-integration.md) | Integration | TR-settlement-002, TR-settlement-003 | ADR-0014 |
| 005 | [Persistence & State Recovery](story-005-persistence-state-recovery.md) | Integration | TR-settlement-001, TR-settlement-003 | ADR-0014 |
| 006 | [Edge Cases, UI Integration & Defensive Handling](story-006-edge-cases-ui-defensive.md) | Integration | TR-settlement-001, TR-settlement-002, TR-settlement-003 | ADR-0014 |

**Summary**: 3 Logic + 3 Integration stories

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/port-village-market.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/settlement-market/` and `tests/integration/settlement-market/`
- 3-tier state machine (settlement, stall, NPC) transitions correctly under all inputs; all 4 invalid transitions rejected
- F.1 total_cost calculation produces correct deterministic results
- F.2 stall unlock check correctly computes required_node_ids ∩ completed_node_ids
- F.3 settlement activity aggregation correctly categorizes dormant/recovering/active
- repair_completed signal consumption correctly matches node_id to stall required_node_ids
- Purchase flow correctly delegates validate_purchase / execute_purchase to #5
- Default stall (stall.gh-general) always open_basic — guaranteed purchase point
- All 16 edge cases from GDD correctly handled
- ADR-0003 progress.settlement-market snapshot correctly serializes/deserializes all state
- SettlementManager is independently testable with mock #5/#13/#4 injection

## Next Step

All stories created. Feature Layer 4/5 unblocked. Next: partner-relationships #15 (ADR-0015 deferred) — the final blocked Feature Layer epic.
