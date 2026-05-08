# Epic #16: UI / HUD / 航图界面

> **Status**: In Progress
> **Layer**: Presentation
> **Engine**: Godot 4.6.2
> **Created**: 2026-05-08

## Overview

系统 #16（UI / HUD / 航图界面）是 MVP 的呈现层外壳，负责将 15 个领域系统的数据与状态转换为统一的画面体验。拥有 12 个屏幕/面板（S1–S12）、屏幕状态机（11 状态 16 转换）、单槽模态栈（S7 战斗覆盖例外）、4 层输入路由优先级、Godot 4.6 dual-focus 显式同步策略、信号驱动 + 脏标记批量 HUD 更新。UIManager 不拥有任何领域数据——所有显示数据通过直接方法调用从领域系统查询。

**视觉语言**: 航路修复主义——UI 像可被阅读的航海图，同一本日志的页码被一页页翻开。

## Governing ADR

- **ADR-0012**: UI 屏幕状态机、模态栈与输入路由 — UIManager Autoload #16

## TR Coverage

| TR ID | Description |
|-------|-------------|
| TR-ui-001 | 屏幕状态机、屏幕流与 12 屏清单管理 |
| TR-ui-002 | 模态栈规则（单槽 + S7 战斗覆盖 + 排队策略）+ 4 层输入路由 + dual-focus 同步 |
| TR-ui-003 | HUD 更新策略（信号驱动脏标记批量更新）+ 面板生命周期 + 懒加载缓存池 |
| TR-ui-004 | 动画/过渡时序合约 + 上游数据接口（19 查询）+ 下游语义事件（10 信号）|

## Stories

| # | Story | Type | TR | Risk |
|---|-------|------|-----|------|
| 001 | Screen State Machine & Screen Flow | Logic | TR-ui-001 | LOW |
| 002 | Modal Stack, Combat Override & Input Routing | Logic | TR-ui-002 | LOW |
| 003 | HUD Update, Panel Lifecycle & Cache | Logic | TR-ui-003 | LOW |
| 004 | Upstream Data Contracts & Domain Integration | Integration | TR-ui-004 | LOW |
| 005 | Animation Timing & Downstream Semantic Events | Integration | TR-ui-004 | LOW |
| 006 | Edge Cases, Web Recovery & Accessibility | Integration | TR-ui-001..004 | LOW |

## Story Type Summary

- **Logic (3)**: 001 (screen FSM), 002 (modal stack + input routing), 003 (HUD dirty flags + panel lifecycle)
- **Integration (3)**: 004 (upstream data contracts), 005 (animation + downstream events), 006 (edge cases + web recovery + a11y)

## Definition of Done

- [ ] 3 Logic stories have automated unit tests (GdUnit4) passing
- [ ] 3 Integration stories have integration tests or documented playtest
- [ ] All 12 screens (S1–S12) open/close via UIManager API
- [ ] 4-layer input routing verified: WASD blocked at Layer 0–2, passes at Layer 4
- [ ] Dual-focus: mouse click → grab_focus() sync verified for all interactive controls
- [ ] HUD dirty flags: idle _process zero-cost when no flags set
- [ ] Web tab freeze recovery: delta > 1.0s → full_ui_refresh
- [ ] WCAG AA: all <24px color-coded elements use shape+color+text triple encoding
