# Story 001: Platform State Machine Core

> **Epic**: Platform Session Shell
> **Status**: Complete — 2026-05-11
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-001`, `TR-platform-003`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: SessionShell 在 Phase 1 (engine_init) 第一个启动，管理 10 主状态 + 加载子阶段。平台状态机是确定性的——每个状态有明确允许的玩家输入和退出条件。加载子阶段 (BaseBoot/ContentDomainCheck/StorageCapabilityCheck/SessionMetadataCheck/EntryRenderReady) 用于诊断分类但不升级为主状态。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM (desktop lifecycle constraints)
**Engine Notes**: 单线程 桌面构建；Compatibility 渲染器；Godot window notifications用于桌面窗口生命周期事件。

**Control Manifest Rules (Foundation layer)**:
- Required: 状态转换必须通过守卫检查；非法转换记录诊断 warning 并拒绝
- Forbidden: `direct_cross_autoload_in_ready` — SessionShell 在 `_ready()` 中不调用其他 Autoload
- Guardrail: Autoload `_ready()` ≤5ms；状态转换 <0.1ms

---

## Acceptance Criteria

- [x] **AC-1**: GIVEN 基础资源、内容域状态和会话元数据检查完成，WHEN 检查全部通过，THEN 状态转入 `Ready`，并显示可交互的 Start/Continue 入口
- [x] **AC-2**: GIVEN 游戏处于 SessionStarting，WHEN 会话上下文、内容域、可继续性和输入焦点全部通过，THEN 状态转入 `SessionActive`
- [x] **AC-3**: GIVEN 游戏处于 SessionStarting，WHEN 继续会话失败、内容域失败、存储状态不支持继续或会话元数据损坏，THEN 状态转入 `RecoveryRequired`
- [x] **AC-4**: GIVEN 游戏处于 BackgroundSuspended，WHEN 页面恢复可见且可交互，THEN 状态转入 `ResumePending`，仍不得接受普通玩法输入
- [x] **AC-5**: GIVEN ResumePending 且页面已回前台、挂起 token 有效、玩家完成重新激活、焦点恢复且内容域可用，THEN 状态转入 `SessionActive`
- [x] **AC-6**: GIVEN ResumePending 且挂起 token 无效、内容域不可恢复或恢复检查失败，THEN 状态转入 `RecoveryRequired`
- [x] **AC-7**: GIVEN 核心资源缺失、构建不兼容、缺少必需 desktop runtime 或内容域版本不兼容，WHEN 加载失败，THEN 状态转入 `FatalBlocked`
- [x] **AC-8**: GIVEN 加载子阶段中任一失败，WHEN 报告错误，THEN 必须包含失败子阶段 (BaseBoot/ContentDomainCheck/StorageCapabilityCheck/SessionMetadataCheck/EntryRenderReady)、失败类型、是否可重试、桌面窗口焦点/焦点状态

---

## Implementation Notes

- 状态使用枚举 `enum ShellState { BOOTING, LOADING, READY, AWAITING_AUDIO, SESSION_STARTING, SESSION_ACTIVE, BACKGROUND_SUSPENDED, RESUME_PENDING, RECOVERY_REQUIRED, FATAL_BLOCKED }`
- 状态转换通过 `transition_to(new_state: ShellState, context: Dictionary) -> bool` 单入口——返回 false 时记录诊断 warning
- 每个转换有对应守卫函数：`_can_boot_to_loading()`, `_can_loading_to_ready()` 等
- 加载子阶段使用独立枚举 `enum LoadPhase { BASE_BOOT, CONTENT_DOMAIN_CHECK, STORAGE_CAPABILITY_CHECK, SESSION_METADATA_CHECK, ENTRY_RENDER_READY }`
- 加载进度通过信号 `loading_phase_changed(phase: StringName, progress: float)` 通知 UI
- 状态变更 emit `shell_state_changed(old_state: StringName, new_state: StringName)` (emit-after-mutation)

---

## Out of Scope

- Story 002: Start/Continue 入口逻辑和 Audio Activation
- Story 003: Background Suspend/Resume 的桌面窗口生命周期事件处理
- Story 004: failure_severity 判定和 Recovery 路径

---

## QA Test Cases

- **AC-1**: Loading → Ready transition
  - Given: 所有加载子阶段通过
  - When: `transition_to(READY, {})`
  - Then: 返回 true，shell_state_changed(LOADING, READY) emit
  - Edge cases: 任一个子阶段失败→转入 RecoveryRequired 或 FatalBlocked

- **AC-8**: Load phase tracked in failures
  - Given: StorageCapabilityCheck 阶段 user:// storage probe 超时
  - When: 加载失败
  - Then: 诊断报告包含 `load_phase: STORAGE_CAPABILITY_CHECK`, `retryable: true`
  - Edge cases: 多个子阶段失败→报告每个阶段的失败，按执行顺序排列

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/session/SessionStateMachineTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-11

---

## Dependencies

- Depends on: None — Story 001 是 platform-session-shell 的前置
- Unlocks: Story 002-007 (所有后续 Story 依赖状态机)

## Implementation Notes

**Implemented**: 2026-05-11
**Criteria**: 8/8 passing
**Test Evidence**: Logic test at `tests/unit/session/SessionStateMachineTest.csproj` — 8 acceptance checks passing.
**Code Review**: Local review complete — no blocking issues found.
