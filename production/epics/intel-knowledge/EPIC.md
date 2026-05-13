# Epic: Intel / Knowledge System

> **Layer**: Core
> **GDD**: design/gdd/player-knowledge-intel.md
> **Architecture Module**: Autoload #6 — IntelManager
> **Status**: In Progress
> **Stories**: 8 (001-008) — Ready for implementation

## Overview

实现《云海织航》的知识与能力成长层——追踪玩家对空海世界两类永久积累：情报条目（已知的航线、地点、资源、威胁、传闻、旧日志）和能力条目（已解锁的永久行动能力，如读懂风流信号、穿越礁石区、低能见度辨识方向、解读旧灯塔信号发现隐藏航线）。基于 Registry 稳定 ID，每条情报维护 4 态知识状态（unrevealed → rumored → identified → verified），每条能力维护 locked/unlocked 二元状态。系统消费 Registry 的情报条目定义、ResourcesManager 的 Pool 6 提交记录、伙伴 `reveal_rumor()` 输入、世界修复完成信号，产出 `IntelConsumeResult`（confidence decay + hidden tag reveal）。3 条能力解锁路径：Path A (intel-driven — 收集足够验证情报)、Path B (repair-driven — 世界修复解锁)、Path C (composite — 情报 + 修复双重条件)。系统不拥有情报条目的静态定义（Registry）、不拥有 UI 渲染（UIManager）、不拥有探索或战斗的具体结果。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0007: Intel / Knowledge System | IntelManager Autoload #6；4 态知识状态机；IntelConsumeResult 算法；3 条能力解锁路径；confidence decay + hidden tag reveal；依赖 ADR-0003 快照持久化 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-intel-001 | 3 knowledge states per entity: unrevealed → rumored → identified → verified | ADR-0007 ✅ |
| TR-intel-002 | IntelConsumeResult algorithm: confidence decay + hidden tag reveal | ADR-0007 ✅ |
| TR-intel-003 | 3 ability unlock paths: Path A (intel-driven), Path B (repair-driven), Path C (composite) | ADR-0007 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/player-knowledge-intel.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/intel/`
- Intel state transitions (unrevealed→rumored→identified→verified) work correctly
- IntelConsumeResult algorithm produces correct confidence decay on rumor consumption
- Hidden tag reveal correctly cascades to chart route visibility (via ADR-0008)
- 3 ability unlock paths all resolve correctly with mock inputs
- Intel snapshot persists/restores correctly via Persistence (ADR-0003)

## Stories

| # | Title | Type | TR | ADR | Status |
|---|-------|------|----|-----|--------|
| 001 | [Pattern Knowledge Observation & State Machine](story-001-pattern-knowledge-state-machine.md) | Logic | TR-intel-001 | ADR-0007 | Done |
| 002 | [Location Knowledge State Machine & Rumor System](story-002-location-knowledge-rumor-system.md) | Logic | TR-intel-001 | ADR-0007 | Done |
| 003 | [Ability Multi-Path Unlock System](story-003-ability-multi-path-unlock.md) | Logic | TR-intel-003 | ADR-0007 | Ready |
| 004 | [IntelConsumeResult Algorithm](story-004-intel-consume-algorithm.md) | Logic | TR-intel-002 | ADR-0007 | Ready |
| 005 | [Upstream Event Receivers](story-005-upstream-event-receivers.md) | Logic | TR-intel-001, TR-intel-002, TR-intel-003 | ADR-0007 | Ready |
| 006 | [Downstream Query Interface](story-006-downstream-query-interface.md) | Logic | TR-intel-001, TR-intel-002, TR-intel-003 | ADR-0007 | Ready |
| 007 | [Signal Contract & Non-Degradation Guards](story-007-signal-contract-non-degradation.md) | Integration | TR-intel-001, TR-intel-002, TR-intel-003 | ADR-0002, ADR-0007 | Ready |
| 008 | [Persistence & MVP Bootstrap](story-008-persistence-mvp-bootstrap.md) | Integration | TR-intel-001, TR-intel-002, TR-intel-003 | ADR-0001, ADR-0003, ADR-0007 | Ready |

## Next Step

All 8 stories are written and ready for implementation. Start with Story 001 (Pattern Knowledge State Machine) and Story 002 (Location Knowledge & Rumor) as they unblock all other stories.
