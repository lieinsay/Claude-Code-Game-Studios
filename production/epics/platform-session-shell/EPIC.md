# Epic: Platform Session Shell

> **Layer**: Foundation
> **GDD**: design/gdd/platform-session-shell.md
> **Architecture Module**: Autoload #2 — SessionShell
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories platform-session-shell`

## Overview

实现《云海织航》的 Web-first 入口与会话生命周期系统。负责将玩家从浏览器页面带入可恢复、可听见、可操作的游戏会话：加载基础资源→展示 Start/Continue 入口→处理首次音频激活→建立键鼠输入入口→标签页失焦/恢复时保护会话状态。该系统定义 10 个主平台状态 (Booting→Loading→Ready→AwaitingAudioActivation→SessionStarting→SessionActive→BackgroundSuspended→ResumePending→RecoveryRequired→FatalBlocked)，管理会话连续性不变量（刷新/关闭/恢复后只能回到最近安全继续点或清楚错误态），并通过 custom HTML shell 接入浏览器生命周期事件 (visibilitychange, pagehide, pageshow)。壳层是"进入和离开世界的门"——在 SessionActive 以外状态阻止玩法输入；SessionActive 内仅保留 Esc 暂停和生命周期级拦截。MVP 支持 ephemeral session（浏览器存储不可用时允许临时会话但明确提示"进度不会保存"）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0001: Autoload/Scene Boot Order | SessionShell 在 Phase 1 (engine_init) 第一个启动，管理会话生命周期 | LOW |
| ADR-0006: Web Platform Constraints | Web 导出约束：单线程导出、AudioContext 用户手势、标签页后台暂停、IndexedDB 持久化边界 | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|--------------|
| TR-platform-001 | Web-first application shell: loading → start/continue → gameplay | ADR-0001, ADR-0006 ✅ |
| TR-platform-002 | Audio activation via user gesture; tab focus recovery with delta-based resume | ADR-0006 ✅ |
| TR-platform-003 | 15 platform states: boot → title → loading → playing → paused → error | ADR-0001 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/platform-session-shell.md` are verified
- All Logic and Integration stories have passing test files in `tests/unit/session/`
- All Visual/Feel and UI stories (shell UI: loading screen, start/continue, error states) have evidence docs with sign-off in `production/qa/evidence/`
- Custom HTML shell captures browser lifecycle events and routes to Godot via JavaScriptBridge
- AudioContext activation works within a single user gesture; soft-fail into silent session with clear indicator
- Tab background → foreground recovery completes within 100ms (delta-based resume)
- Ephemeral session mode works when IndexedDB is unavailable
- FatalBlocked shows safe error UI, never half-initialized world

## Next Step

Run `/create-stories platform-session-shell` to break this epic into implementable stories.
