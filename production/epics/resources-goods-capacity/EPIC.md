# Epic: Resources, Goods & Capacity

> **Layer**: Foundation
> **GDD**: design/gdd/resources-goods-capacity.md
> **Architecture Module**: Autoload #5 — ResourcesManager
> **Status**: Complete
> **Stories**: 9 (001-009) — Story 001-009 Complete

## Overview

实现《云海织航》的资源与物流契约层——定义材料、补给、货物、可购买商品和携带战利品的稳定身份、堆叠规则、货物分类、容量上限以及"带什么去 vs 带回什么"的取舍模型。基于 Registry 的稳定 ID，6 个资源池 (Pool 1-6) 按不同容量类型管理资源流向：Pool 1-3 为飞艇存储 (discrete slots / numeric stacks / weight-based)，Pool 4 为随身携带 (numeric stacks)，Pool 5 为探索中战利品 (numeric stacks, 受撤出损失影响)，Pool 6 为已提交终端池 (不可逆)。核心操作包括 `commit_deposit`（原子不可逆提交至 Pool 6）、`consume_in_combat`（Pool 5 永久移除）、`transfer`（池间转移，容量校验）、`discard`（不可逆丢弃，需确认）。堆叠合并按优先级规则排序。玩家通过整备（出航前分配 Pool 4）、探索中负重判断（继续搜 vs 撤）和返航后分配（修灯塔 vs 补货架 vs 存仓库）间接感受这个系统的秩序感。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0005: Resource Pool System | 6-pool 架构；3 种容量类型；commit_deposit 原子不可逆；consume_in_combat Pool 5 永久移除；堆叠合并优先级；货物分类 mass_class/handling_class | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-resources-001 | 6 resource pools with defined stack rules and capacity types | ADR-0005 ✅ |
| TR-resources-002 | commit_deposit(node_id, resources) — atomic, irreversible, Pool 6 terminal | ADR-0005 ✅ |
| TR-resources-003 | 3 capacity types: discrete slots, numeric stacks, weight-based | ADR-0005 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/resources-goods-capacity.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/resources/`
- All Visual/Feel and UI stories (inventory overlay, capacity indicators, Pool 5 low warning) have evidence docs with sign-off in `production/qa/evidence/`
- 6 resource pools initialize correctly; pool boundaries enforced (no cross-pool leakage)
- commit_deposit is atomic and irreversible (no rollback, no partial commit)
- consume_in_combat permanently removes from Pool 5
- Stack merge priority rules produce deterministic results
- Capacity overflow prevented at transfer boundaries
- HUD Pool 5 bar updates via ResourcesManager signal (dirty-flag batch update per ADR-0012)

## Stories

| # | Title | Type | TR | ADR | Status |
|---|-------|------|----|-----|--------|
| 001 | [Resource Identity & Stack Merge](story-001-resource-identity-stack-merge.md) | Logic | TR-resources-001 | ADR-0005 | Complete |
| 002 | [Dual Capacity System](story-002-dual-capacity-system.md) | Logic | TR-resources-003 | ADR-0005 | Complete |
| 003 | [Cargo Model & Unpack](story-003-cargo-model-unpack.md) | Logic | TR-resources-003 | ADR-0005 | Complete |
| 004 | [Weight & Mass Tracking](story-004-weight-mass-tracking.md) | Logic | TR-resources-003 | ADR-0005 | Complete |
| 005 | [Core Atomic Operations](story-005-core-atomic-operations.md) | Logic | TR-resources-001 | ADR-0005 | Complete |
| 006 | [State Machine & Pool Transitions](story-006-state-machine-pool-transitions.md) | Logic | TR-resources-001 | ADR-0005 | Complete |
| 007 | [Specialized Operations](story-007-specialized-operations.md) | Integration | TR-resources-002 | ADR-0004, ADR-0005 | Complete |
| 008 | [Signal Contract & Reentry Guard](story-008-signal-contract-reentry-guard.md) | Integration | TR-resources-001 | ADR-0002, ADR-0005 | Complete |
| 009 | [Persistence & External Integration](story-009-persistence-external-integration.md) | Integration | TR-resources-001/003 | ADR-0001, ADR-0003, ADR-0005 | Complete |

## Next Step

All Resources, Goods & Capacity stories are complete. Run the sprint close-out sequence next: `/smoke-check sprint`, then `/team-qa sprint`, then `/gate-check` once QA approves.
