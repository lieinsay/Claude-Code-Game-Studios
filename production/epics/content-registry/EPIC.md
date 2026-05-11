# Epic: Content Registry

> **Layer**: Foundation
> **GDD**: design/gdd/content-data-state-registry.md
> **Architecture Module**: Autoload #1 — Registry
> **Status**: In Progress
> **Stories**: 8 created

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [ID Registry Core + Query Engine](story-001-id-registry-core-query.md) | Logic | Done | ADR-0001 |
| 002 | [Schema Validation](story-002-schema-validation.md) | Logic | Done | ADR-0001 |
| 003 | [Content Lifecycle](story-003-content-lifecycle.md) | Logic | Done | ADR-0001 |
| 004 | [Reference Integrity](story-004-reference-integrity.md) | Logic | Done | ADR-0001 |
| 005 | [Domain Loading & Decision UI Gating](story-005-domain-loading-decision-gating.md) | Integration | Complete | ADR-0001, ADR-0002 |
| 006 | [Diagnostic System](story-006-diagnostic-system.md) | Logic | Complete | ADR-0001 |
| 007 | [Diagnostic UI — Dev Tools](story-007-diagnostic-ui.md) | UI | In Progress | ADR-0001, ADR-0012 |
| 008 | [Player-Facing Boundary](story-008-player-facing-boundary.md) | Integration | Complete | ADR-0001, ADR-0002 |

## Overview

实现《云海织航》的静态内容契约层——全游戏唯一的内容定义目录与校验入口。该系统定义 12 种内容类型（resource、cargo、module、home-space、home-anchor、route、location、repair-node、stall-good、companion、threat、intel）的稳定 ID、Schema、受控词表、引用关系和只读查询契约。Registry 不拥有任何玩家进度、运行时状态、库存或解锁结果；下游系统通过稳定 ID 引用内容定义，查询必须区分 UNLOADED / NOT_FOUND / VERSION_INCOMPATIBLE / Deprecated / Retired。ID 一旦进入 Active 就不可复用。所有列表查询按 `sort_order ASC, id ASC` 确定性排序。MVP 需覆盖 7 个内容域 (resources, airship, world, routes, intel, companions, threats)，支持局部加载和内容包版本校验。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Autoload/Scene Boot Order | Registry 在 Phase 2 (foundation_start) 启动，提供全游戏共享的只读内容查询 | LOW |
| ADR-0002: Signal Communication Protocol | Registry 的静态内容加载完成信号 `domain_ready` 遵循 typed params + sync emit 协议 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-registry-001 | Static content definitions with stable IDs across 12 content kinds | ADR-0001 ✅ |
| TR-registry-002 | query_entity(id) returns typed entity; validate_all() produces diagnostics | ADR-0001 ✅ |
| TR-registry-003 | Registry must not own mutable runtime state | ADR-0001 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/content-data-state-registry.md` are verified (~42 ACs covering ID stability, schema validation, query contracts, domain completeness, diagnostic precedence)
- All Logic and Integration stories have passing test files in `tests/unit/registry/`
- All Visual/Feel and UI stories (diagnostic tool panels) have evidence docs with sign-off in `production/qa/evidence/`
- Content package validation rejects duplicate IDs, missing references, runtime field contamination, and invalid controlled vocabulary
- Web platform: content domain FAILED/VERSION_INCOMPATIBLE produces safe error UI, not half-loaded gameplay

## Next Step

Run `/story-readiness production/epics/content-registry/story-007-diagnostic-ui.md`, then implement Story 007.
