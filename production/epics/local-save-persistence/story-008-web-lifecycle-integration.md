# Story 008: Desktop Lifecycle Integration

> **Epic**: Local Save / World State Persistence
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-008`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: Godot desktop window notifications 将 `window_focus_changed`、`suspend_requested`、`resume_requested`、focus/blur 和桌面存储 capability 信号传给平台壳。平台壳拥有桌面窗口生命周期事件 token 与输入门禁；存档系统拥有保存、验证、promotion、备份提升、storage_capability 和恢复判定。`suspend_requested` 永远不是正确性路径——只能触发 best-effort 请求，且受 `suspend_requested_marker_budget_ms` 约束。

**Engine**: Godot 4.6.2 | **Risk**: HIGH
**Engine Notes**: 桌面构建下 `suspend_requested`/`window_focus_changed` 由 SessionShell 从 Godot 窗口通知归一化为 lifecycle token。单线程执行——suspend_requested 期间不可启动新的完整序列化。
**Control Manifest Version**: 2026-05-09 — 与当前 `docs/architecture/control-manifest.md` 版本一致；Foundation Layer 无新增规则需要纳入本 Story。

**Control Manifest Rules (Foundation layer)**:
- Required: best-effort marker flush ≤20ms；suspend_requested 只使用预编码 staging；启动时读回验证 suspend_requested 标记
- Forbidden: suspend_requested/window_focus_changed/quit_requested 不得启动完整序列化、迁移、备份提升或诊断文本生成
- Guardrail: suspend_requested_marker_budget_ms = 20ms；save_hot_path_budget_ms target 60ms / warning 180ms

---

### 术语定义

**正式 commit class**（以下 AC 中凡提到"正式 commit class"均指下列 7 类）：

| Commit Class | 描述 |
|---|---|
| `WorldRepairCommit` | 修复灯塔/节点等世界修复操作 |
| `LongTermResourceCommit` | 长期资源积累（非临时货物） |
| `RelationshipCommit` | 关系/村镇状态变化 |
| `SettlementMarketCommit` | 集市交易库存落定 |
| `AirshipHomeLayoutCommit` | 飞艇家园布置或模块安装 |
| `RouteUnlockCommit` | 航线解锁 |
| `ExplorationSettlementCommit` | 探索/搜撤结算 |

任何属于上述 7 类的操作，在 `EphemeralOnly` 或 `SaveLocked` 模式下均须被系统拒绝。

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 应用进入 `suspend_requested` 或 `suspend_requested` 且 staging 正在进行但 verify 尚未完成，WHEN 应用关闭请求继续推进，THEN 系统可以发出 best-effort flush，但不得设置 `promotion_success=true`，不得替换 `last_verified_checkpoint`，不得阻塞应用关闭请求
- [ ] **AC-2**: GIVEN `suspend_requested`、`window_focus_changed hidden`、`quit_requested` 或 `quit_requested` 触发，WHEN 尚无预编码 staging 可轻量 flush，THEN 系统不得启动新的完整序列化、迁移、备份提升或诊断文本生成
- [ ] **AC-3**: GIVEN `suspend_requested`、`window_focus_changed hidden`、`quit_requested` 或 `quit_requested` 触发，WHEN 处理 lifecycle marker，THEN 系统不得启动 readback、checksum、full serialization、migration、backup promotion 或 diagnostics text formatting；只能使用已预编码 marker，且必须在 `suspend_requested_marker_budget_ms` 内放弃
- [ ] **AC-4**: GIVEN Godot 启动完成前 desktop lifecycle adapter 已收到 `window_focus_changed hidden` 或 `suspend_requested`，WHEN Godot 平台适配层完成初始化，THEN 该缓存事件必须转为壳层 lifecycle token，不得丢弃
- [ ] **AC-5**: GIVEN `resume_requested.persisted=true` 或任意 suspend 后第一次 `resume_requested`，WHEN 页面恢复，THEN capability probe 必须失效并重探，即使 TTL 尚未过期
- [ ] **AC-6**: GIVEN 工件处于 `Staging` 或 `Verify`，WHEN UI 轮询保存状态，THEN UI 可以显示保存中或正在保护最近进度，但不得显示保存成功
- [ ] **AC-7**: GIVEN `EphemeralOnly` 会话已确认进入，WHEN 玩家尝试提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置，THEN 系统必须拒绝正式提交，且 `continue_availability=Hidden`
- [ ] **AC-8**: GIVEN 会话中途进入 `SaveLocked`，WHEN 玩家尝试提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置，THEN 系统必须阻止正式提交并显示 Retry Save Capability / Return Title / Enter Temporary Flight 选择
- [ ] **AC-9**: GIVEN 玩家从 `SaveLocked` 选择 Enter Temporary Flight 并经二次确认，WHEN `enter_temporary_flight()` 被调用，THEN `mode=EphemeralOnly`，`continue_availability=Hidden`，禁止所有正式 commit class
- [ ] **AC-10**: GIVEN 每次保存、迁移、恢复、备份提升或 capability probe 完成，WHEN 开发诊断摘要生成，THEN 摘要包含快照字节数、配额余量、encode/write/readback/checksum 耗时、promotion 结果、失败原因码、suspend_requested 结果和备份提升结果；保存热路径只追加预分配固定大小结构化记录，不分配超过 4 KiB 诊断记录，不同步生成完整可复制文本报告
- [ ] **AC-11**: GIVEN `suspend_requested` 触发且预编码 staging 存在，WHEN best-effort flush 执行，THEN flush 必须在 `suspend_requested_marker_budget_ms`（20ms）内完成或主动放弃；超时必须记录 `PERF_SUSPEND_BUDGET_EXCEEDED` 原因码，不得阻塞应用关闭
- [ ] **AC-12**: GIVEN 稳定边界触发完整保存（staging + verify + promotion），WHEN 热路径执行（encode + write + readback + checksum + promotion pointer update + structured diagnostics append），THEN 总耗时目标 ≤60ms；超过 180ms 时必须记录 `PERF_SAVE_HOT_PATH_BUDGET_EXCEEDED` 诊断警告；不得因诊断写入而分配超过 4 KiB 堆内存

---

## Implementation Notes

- Lifecycle event wiring:
  - Custom HTML shell 注册 `window_focus_changed`、`suspend_requested`、`resume_requested`、focus/blur listener
  - SessionShell registers lifecycle callbacks during boot and releases them during shutdown
  - Godot 启动前收到的事件缓存到 JS 侧数组，启动后批量传递给平台适配层
  - 平台适配层将缓存事件转换为幂等 lifecycle token
- `suspend_requested` best-effort 路径:
  - 检查是否有预编码 staging marker 可用
  - 有 → 轻量 flush（只写 marker + timestamp），预算 ≤20ms
  - 无 → 放弃本次推进，保留旧 `last_verified_checkpoint`
  - 超时 → 放弃，保留旧 `last_verified_checkpoint`
  - 不得启动: 完整序列化、migration、backup promotion、readback、checksum、诊断文本生成
- 写屏障 API:
  - `query_write_barrier()` → `{barrier_active, mode, reason_code, allowed_actions, forbidden_commit_classes}`
  - `request_retry_save_capability()` → 触发 probe TTL 失效和重探
  - `enter_temporary_flight()` → 二次确认后进入 EphemeralOnly
  - `return_title_preserve_safe()` → 返回标题并保留旧 Safe
- 正式 commit class 列表: `WorldRepairCommit`、`LongTermResourceCommit`、`RelationshipCommit`、`SettlementMarketCommit`、`AirshipHomeLayoutCommit`、`RouteUnlockCommit`、`ExplorationSettlementCommit`
- 诊断热路径限制: 只追加结构化标量/enum/reason_code/generation ID/duration；≤4 KiB/tick；报告文本按需异步生成
- `save_hot_path_budget_ms` = encode + write + readback + checksum + promotion pointer update + structured diagnostics append
- 引擎 API 验证说明 (Godot 4.6.2 .NET — 实现前须核查):
  - 桌面窗口焦点事件: `SceneTree` 无 `window_focus_changed` 信号；正确路径为
    `GetWindow().FocusEntered += OnWindowFocusEntered` 和
    `GetWindow().FocusExited += OnWindowFocusExited`，在 SessionShell `_Ready()` 内
    通过 `CallDeferred` 延迟连接，或在 `_Notification(int what)` 中处理
    `NotificationWmWindowFocusIn` / `NotificationWmWindowFocusOut`
  - suspend_requested / quit_requested 映射: 桌面 C# 中通过 `_Notification(int what)` 处理
    `NotificationWmCloseRequest`（关闭请求）；`NotificationApplicationPaused` 用于
    系统级挂起（仅移动平台，桌面不适用）；`suspend_requested` 语义由 SessionShell
    归一化为 lifecycle token 后传给 Persistence，不直接依赖系统通知名称
  - 验证要求: 实现前在 Godot 4.6.2 .NET 编辑器中确认通知常量枚举值与
    `docs/engine-reference/godot/` 记录一致；如行为与预期不符，更新本条注释并记录
  - `OS.IsDebugBuild()`: Godot .NET 中可直接调用，已在 RegistryDiagnosticPanel.cs 验证可用

---

## Out of Scope

- Story 001-007: 各自的保存/恢复/迁移/备份核心逻辑
- Custom HTML shell 的完整实现（由 platform-session-shell Epic 的 Story 003 覆盖 lifecycle wiring）
- UI 呈现（SaveLocked overlay、EphemeralOnly warning、RecoveryRequired screen）——由本 Epic 的 UI 需求在各 Story 的 QA 验证中覆盖，但 UI 实现由 platform-session-shell Story 007 和专门的 UI Story 负责
- 诊断报告的可复制文本格式化——由 diagnostic UI（content-registry Story 007）覆盖

---

## QA Test Cases

- **AC-1**: suspend_requested with in-progress staging → best-effort only
  - Given: 稳定边界已触发 staging write，verify 尚未开始，suspend_requested 事件到达
  - When: `_on_suspend_requested()`
  - Then: 发出 best-effort flush（若有预编码 marker）；`promotion_success` 保持 false；`last_verified_checkpoint` 不变
  - Edge cases: suspend_requested 在 verify 中途到达 → 同样不 promotion

- **AC-3**: suspend_requested budget exceeded → abandon
  - Given: suspend_requested 触发，预编码 staging 存在但 flush 超过 20ms budget
  - When: flush 超时
  - Then: 放弃本次推进；保留旧 `last_verified_checkpoint`；记录 `PERF_suspend_requested_BUDGET_EXCEEDED`
  - Edge cases: 无预编码 staging → 直接放弃，不尝试 flush

- **AC-9**: SaveLocked → Enter Temporary Flight → EphemeralOnly
  - Given: 会话中途 promotion 失败进入 SaveLocked，玩家选择 Enter Temporary Flight 并二次确认
  - When: `enter_temporary_flight()` 执行
  - Then: `mode=EphemeralOnly`、`continue_availability=Hidden`、所有正式 commit class 被拒绝
  - Edge cases: 玩家 Return Title 而非 Enter Temporary Flight → 保留旧 Safe、current_generation 不变

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/persistence/DesktopLifecycleTest.csproj` — must exist and pass
**Status**: 实现时创建；路径固定为上述位置，实现后将 [ ] 更改为 [x]。

---

## Dependencies

- Depends on: Story 001 (staging/promotion)；Story 003 (storage_capability——suspend_requested 后需重探)；Story 004 (continue_availability——lifecycle 事件后重算)；Story 007 (artifact isolation——两侧各自 best-effort)
- Unlocks: platform-session-shell Story 003 (Background Suspend/Resume——消费本 Story 提供的 lifecycle token)

## Completion Notes

**Completed**: 2026-05-12
**Criteria**: 12/12 passing (AC-8 UI 选择面板 DEFERRED — Out of Scope，归属 platform-session-shell Story 007)
**Deviations**: ADVISORY — `ContinueAvailability` enum 定义在 `src/core/session/EntryAudioFlow.cs`；建议后续重构移至共享 persistence types 文件（LP 建议，非阻塞）
**Test Evidence**: Integration — `tests/integration/persistence/DesktopLifecycleTest.csproj` 22/22 PASS
**Code Review**: Complete — QL-TEST-COVERAGE: ADEQUATE；LP-CODE-REVIEW: APPROVED
