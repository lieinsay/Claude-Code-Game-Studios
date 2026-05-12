# Epic: Player Movement & Interaction

> **Layer**: Foundation
> **GDD**: design/gdd/player-movement-interaction.md
> **Architecture Module**: Autoload #4 — InteractionRegistry
> **Status**: Complete
> **Stories**: 7 (001-007) — Complete

## Overview

实现《云海织航》的基础玩家动作层——将壳层开放后的键鼠输入转化为角色移动 (CharacterBody2D + WASD/Click-to-Move)、可达性判断、交互焦点仲裁和 Use 入口。InteractionRegistry 作为 Autoload #4 管理所有可交互对象的注册/注销、优先级仲裁（最近可达 + 类型优先级 tie-breaking）、交互提示显示和输入路由。所有可交互对象继承 `@abstract InteractionHandler` 基类，定义 `get_interaction_label()` / `can_interact()` / `execute()` 契约。本系统支撑"飞艇是家"的身体感（舱室间自由走动、靠近工作台/货架/舱门）和"规划先于冒险"的可读操作节奏（距离不足、壳层 overlay、状态锁定时安全阻断误触）。移动和交互不拥有具体后果——购买、采集、修复、安装模块、触发探索结果均由对应领域系统拥有。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0004: InteractionHandler @abstract | InteractionRegistry Autoload #4 + @abstract InteractionHandler 基类；WASD + Click-to-Move 双重移动；交互焦点仲裁规则（nearest-reachable + priority tie-break）；Approach+E 交互模式 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-movement-001 | CharacterBody2D movement with InteractionRegistry autoload | ADR-0004 ✅ |
| TR-movement-002 | @abstract InteractionHandler base class for all interactable objects | ADR-0004 ✅ |
| TR-movement-003 | Interaction focus: nearest-reachable with priority tie-breaking | ADR-0004 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/player-movement-interaction.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/movement/`
- All Visual/Feel and UI stories (interaction prompts, focus indicator, movement feel) have evidence docs with sign-off in `production/qa/evidence/`
- CharacterBody2D movement works with WASD + Click-to-Move, no input conflict
- InteractionRegistry correctly arbitrates nearest-reachable target with priority tie-breaking
- InteractionHandler @abstract base class is usable by all downstream interactable objects
- Interaction blocked during: shell overlay visible, BackgroundSuspended, scene transition, combat PROCESSING
- Interaction prompts follow interaction-patterns.md: Approach+E pattern, cyan outline 2px, [E] label

## Stories

| # | Title | Type | TR | ADR | Status |
|---|-------|------|----|-----|--------|
| 001 | [Movement System](story-001-movement-system.md) | Logic | TR-movement-001 | ADR-0004 | Done |
| 002 | [Input Gate & Shell Integration](story-002-input-gate-shell-integration.md) | Integration | TR-movement-002 | ADR-0001, ADR-0002, ADR-0006 | Done |
| 003 | [Interaction Focus & Candidate Selection](story-003-interaction-focus-candidate-selection.md) | Logic | TR-movement-003 | ADR-0004 | Done |
| 004 | [Use Gate & Dispatch](story-004-use-gate-dispatch.md) | Logic | TR-movement-004 | ADR-0004 | Done |
| 005 | [Interactable Base Class & Registry](story-005-interactable-base-class-registry.md) | Integration | TR-movement-005 | ADR-0004 | Done |
| 006 | [Semantic Events & UI Data Contract](story-006-semantic-events-ui-data.md) | Integration | TR-movement-006 | ADR-0002, ADR-0004 | Done |
| 007 | [Cross-System Boundaries & Desktop Lifecycle Constraints](story-007-cross-system-boundaries-web.md) | Integration | TR-movement-007 | ADR-0001, ADR-0006 | Done |

## Next Step

Epic #4 is complete. Downstream Hub, exploration, settlement, repair, and UI systems can now consume the movement/input/focus/Use contract.
