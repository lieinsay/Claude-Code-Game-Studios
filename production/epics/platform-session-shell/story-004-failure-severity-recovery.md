# Story 004: Failure Severity & Recovery Paths

> **Epic**: Platform Session Shell
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-001`, `TR-platform-003`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: 所有失败必须 fail closed——停在壳层安全界面，允许重试/返回标题/开始新会话/查看错误。失败不得覆盖有效继续点、污染现有会话、生成损坏状态或自动清空失效续档。failure_severity 分三级：HardFail（进入 FatalBlocked）、SoftFail（带警告路径继续）、RecoverableFail（进入 RecoveryRequired）。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: `user://` 映射到 user:// storage——存储失败可通过此路径检测。

**Control Manifest Rules (Foundation layer)**:
- Required: 失败必须 fail closed；继续点保护（失败不删除/降级/覆盖）
- Forbidden: 不得静默进入半初始化世界
- Guardrail: 失败诊断 <10ms 生成

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 内容域 FAILED，WHEN 玩家尝试进入/恢复会话，THEN 转入 RecoveryRequired——允许 Retry/New Session/Return Title
- [ ] **AC-2**: GIVEN 内容域 VERSION_INCOMPATIBLE，WHEN 玩家尝试进入/恢复会话，THEN 转入 FatalBlocked 或版本不兼容安全态——不得进入玩法
- [ ] **AC-3**: GIVEN 必需内容域集合中同时存在 FAILED 和 LOADING，WHEN 计算聚合 required_content_domain_status，THEN 结果必须为 FAILED
- [ ] **AC-4**: GIVEN 必需内容域集合中任一域为 VERSION_INCOMPATIBLE，WHEN 计算聚合，THEN 结果必须为 VERSION_INCOMPATIBLE
- [ ] **AC-5**: GIVEN 任一失败路径被触发，WHEN 失败被处理，THEN 已存在的有效继续点必须保持原样——不得被删除、覆盖、降级或自动清空
- [ ] **AC-6**: GIVEN 没有硬门槛失败但音频为 SoftFail 或存储为 EphemeralOnly，WHEN 计算 failure_severity，THEN 结果为 SoftFail
- [ ] **AC-7**: GIVEN 存储能力为 WriteLocked 且无其他硬门槛失败，WHEN 计算 failure_severity，THEN 结果为 SoftFail——壳层显示新进度无法可靠保存
- [ ] **AC-8**: GIVEN 内容域失败类型为 Recoverable，WHEN 计算 failure_severity，THEN 结果为 RecoverableFail——进入 RecoveryRequired 而非 FatalBlocked
- [ ] **AC-9**: GIVEN 任一硬门槛失败（基础加载失败/内容域致命/Continue=Hidden/Resume 不就绪/音频 HardFail），WHEN 计算 failure_severity，THEN 结果为 HardFail

---

## Implementation Notes

- `failure_severity` 枚举: `{ NONE, SOFT_FAIL, RECOVERABLE_FAIL, HARD_FAIL }`
- 聚合规则: 遍历所有域状态——任一 FATAL→HardFail；任一 FAILED（Recoverable 类型）→RecoverableFail；任一 FAILED（Fatal 类型）→HardFail；全 PASS + SoftFail 条件→SoftFail
- 继续点保护不变量: 在 `_handle_failure()` 中，`_continue_point` 只读——任何错误处理路径不得调用 `_clear_continue_point()` 或 `_downgrade_continue_point()`
- `required_content_domain_status` 聚合使用预定义域集——不在代码中硬编码域列表
- VERSION_INCOMPATIBLE 优先于 FAILED（在聚合中优先返回）

---

## Out of Scope

- Story 005: storage_capability 的具体判定（由存档系统返回）
- Registry (#1): 内容域状态的具体来源
- Story 007: FatalBlocked/RecoveryRequired 的 UI 渲染

---

## QA Test Cases

- **AC-5**: Continue point preserved on failure
  - Given: 有效继续点存在，内容域加载失败
  - When: failure_severity=RecoverableFail，进入 RecoveryRequired
  - Then: 继续点仍存在且完整——选择 Retry 后 continue_availability 仍为 Enabled
  - Edge cases: 连续 3 次 Retry 失败→继续点仍保留

- **AC-6**: SoftFail with EphemeralOnly
  - Given: storage_capability=EphemeralOnly，内容域全部 COMPLETE，音频 Pass
  - When: 计算 failure_severity
  - Then: SOFT_FAIL——玩家可进入临时会话但收到 "进度不会保存" 警告
  - Edge cases: EphemeralOnly + 内容域 FAILED→RecoverableFail 优先

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/session/FailureSeverityTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (State Machine — RecoveryRequired/FatalBlocked 状态)
- Depends on: Registry (#1) — 内容域状态数据来源
- Unlocks: Story 007 (Failure UI 渲染)
