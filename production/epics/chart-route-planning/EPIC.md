# Epic: Chart / Route Planning

> **Layer**: Core
> **GDD**: design/gdd/chart-route-planning.md
> **Architecture Module**: Autoload #9 — ChartManager
> **Status**: Complete
> **Stories**: 8 (001-008) — All Done

## Overview

实现《云海织航》的核心决策界面与航线选择系统——将 Registry 的静态航线定义（起终点地点、距离带、风险标签）与 IntelManager 的动态知识状态（unrevealed→rumored→identified→verified）统一为一张可读的航图语言。系统渲染 2 条 MVP 航线（sky-reef-arc-01, 及其他），航线按知识状态使用不同视觉编码：rumored = 虚线, identified = 实线, verified = 暖金发光。玩家在航图上阅读每条可选航线的风险标签和来源、看到因世界修复而重新稳定的路线、因缺少能力而阻塞的路线、以及完全未知的区域，然后基于准备和判断选择航线。出航采用两步确认流程：选择航线→显示航线摘要（风险、距离、预计消耗）→确认门（"出航后无法取消"）→墨水扩散锁定动画 1.2s→route_committed 信号发出→航行过渡。航图既是信息呈现系统（航线可见性、风险可视化、阻塞原因），也是决策提交系统（航线选择、出航确认、继续点触发）。系统不拥有航线发现状态（IntelManager）、不拥有遭遇生成或风险后果（Navigation）、不绘制最终 UI（UIManager）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0008: Chart Route State Machine | ChartManager Autoload #9；航线 3 态可见性 (rumored→identified→verified) + 视觉编码；两步出航确认 + 墨水扩散锁定动画；route_committed 信号触发航行；query_route_knowledge 依赖 ADR-0007 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-chart-001 | 2 MVP routes with authored hazard tags; traversable + selectable states | ADR-0008 ✅ |
| TR-chart-002 | Route rendering: rumored=dashed, identified=solid, verified=warm gold glow | ADR-0008 ✅ |
| TR-chart-003 | Two-step departure confirmation with ink-spread animation and irreversible lock | ADR-0008 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/chart-route-planning.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/chart/`
- All Visual/Feel and UI stories (航图渲染, 航线状态视觉编码, 出航确认面板, 墨水扩散动画) have evidence docs with sign-off in `production/qa/evidence/`
- 2 MVP routes render correctly with authored hazard tags
- Route visual encoding correctly reflects knowledge state (dashed/solid/glow)
- Two-step departure confirmation: Phase 1 (摘要) → Phase 2 (确认/取消) → ink-spread lock → route_committed
- Ink-spread lock animation is irreversible (terminal state, ESC disabled)
- Route visibility updates correctly on intel state change or world repair completion
- Chart snapshot persists/restores via Persistence (progress.exploration)

## Stories

| # | Story | Type | TRs | ADRs |
|---|-------|------|-----|------|
| 001 | [Chart State Machine & Content Domain Gate](story-001-chart-state-machine-content-gate.md) | Logic | TR-chart-001, TR-chart-003 | ADR-0008 |
| 002 | [Route Visibility & Selectability Formulas](story-002-visibility-selectability-formulas.md) | Logic | TR-chart-001, TR-chart-002 | ADR-0008 |
| 003 | [Two-Step Departure Confirmation & route_committed Signal](story-003-departure-confirmation-signal.md) | Logic | TR-chart-003 | ADR-0008 |
| 004 | [Route Display Ordering & Filtering](story-004-display-ordering-filtering.md) | Logic | TR-chart-001 | ADR-0008 |
| 005 | [Snapshot Validation & Persistence](story-005-snapshot-validation-persistence.md) | Integration | TR-chart-001, TR-chart-003 | ADR-0008, ADR-0003 |
| 006 | [UIManager Query Interface & Signal Contract](story-006-uimanager-query-interface-signal-contract.md) | Integration | TR-chart-002 | ADR-0008, ADR-0002 |
| 007 | [External State Change Response](story-007-external-state-change-response.md) | Integration | TR-chart-001 | ADR-0008, ADR-0007, ADR-0011 |
| 008 | [Edge Cases, Error Recovery & Keyboard Navigation](story-008-edge-cases-error-recovery-keyboard.md) | Integration | TR-chart-001, TR-chart-003 | ADR-0008 |

**Summary**: 4 Logic + 4 Integration stories

## Completion Review

Epic #9 is closed as of 2026-05-13. Stories 001-008 are Done, and the C# evidence runners for Chart state, visibility/selectability, departure confirmation, display ordering, snapshot persistence, UI query contract, external state response, and edge cases all pass. Current review verification: `dotnet restore CloudWeaverVoyage.sln`; `dotnet build CloudWeaverVoyage.sln --no-restore` PASS with 1 existing nullable warning in `tests/integration/chart/snapshot/ChartSnapshotPersistenceProgram.cs`; full `tests/**/*.csproj` runner sweep 71/71 PASS, 0 FAIL.

## Next Step

Use Epic #9 as an upstream-complete contract for #10 Navigation Route Risk, #13 World Repair, #15 Partner Relationships, #16 UI/HUD, and #18 Onboarding. Do not reopen Chart unless a downstream consumer finds a contract mismatch.
