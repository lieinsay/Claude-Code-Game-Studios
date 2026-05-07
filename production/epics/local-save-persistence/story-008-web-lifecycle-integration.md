# Story 008: Web Lifecycle Integration

> **Epic**: Local Save / World State Persistence
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/local-save-world-state-persistence.md`
**Requirement**: `TR-persistence-008`

**ADR Governing Implementation**: ADR-0003: Save System / JSON Serialization; ADR-0006: Web Platform Constraints
**ADR Decision Summary**: custom HTML shell + JavaScriptBridge 将 `visibilitychange`、`pagehide`、`pageshow`、focus/blur 和浏览器 capability 信号传给平台壳。平台壳拥有浏览器事件 token 与输入门禁；存档系统拥有保存、验证、promotion、备份提升、storage_capability 和恢复判定。`pagehide` 永远不是正确性路径——只能触发 best-effort 请求，且受 `pagehide_marker_budget_ms` 约束。

**Engine**: Godot 4.6.2 | **Risk**: HIGH
**Engine Notes**: Web 导出下 `pagehide`/`visibilitychange` 需要 custom HTML shell 中的 JS listener 通过 `JavaScriptBridge` 回调到 Godot；Godot 启动前收到的事件需缓存并在平台适配层初始化后转为 lifecycle token。单线程执行——pagehide 期间不可启动新的完整序列化。

**Control Manifest Rules (Foundation layer)**:
- Required: best-effort marker flush ≤20ms；pagehide 只使用预编码 staging；启动时读回验证 pagehide 标记
- Forbidden: pagehide/visibilitychange/beforeunload 不得启动完整序列化、迁移、备份提升或诊断文本生成
- Guardrail: pagehide_marker_budget_ms = 20ms；save_hot_path_budget_ms target 60ms / warning 180ms

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 页面进入 `pagehide` 或 `suspend_requested` 且 staging 正在进行但 verify 尚未完成，WHEN 页面关闭继续推进，THEN 系统可以发出 best-effort flush，但不得设置 `promotion_success=true`，不得替换 `last_verified_checkpoint`，不得阻塞页面关闭
- [ ] **AC-2**: GIVEN `pagehide`、`visibilitychange hidden`、`beforeunload` 或 `unload` 触发，WHEN 尚无预编码 staging 可轻量 flush，THEN 系统不得启动新的完整序列化、迁移、备份提升或诊断文本生成
- [ ] **AC-3**: GIVEN `pagehide`、`visibilitychange hidden`、`beforeunload` 或 `unload` 触发，WHEN 处理 lifecycle marker，THEN 系统不得启动 readback、checksum、full serialization、migration、backup promotion 或 diagnostics text formatting；只能使用已预编码 marker，且必须在 `pagehide_marker_budget_ms` 内放弃
- [ ] **AC-4**: GIVEN Godot 启动完成前 JS shim 已收到 `visibilitychange hidden` 或 `pagehide`，WHEN Godot 平台适配层完成初始化，THEN 该缓存事件必须转为壳层 lifecycle token，不得丢弃
- [ ] **AC-5**: GIVEN `pageshow.persisted=true` 或任意 suspend 后第一次 `pageshow`，WHEN 页面恢复，THEN capability probe 必须失效并重探，即使 TTL 尚未过期
- [ ] **AC-6**: GIVEN 工件处于 `Staging` 或 `Verify`，WHEN UI 轮询保存状态，THEN UI 可以显示保存中或正在保护最近进度，但不得显示保存成功
- [ ] **AC-7**: GIVEN `EphemeralOnly` 会话已确认进入，WHEN 玩家尝试提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置，THEN 系统必须拒绝正式提交，且 `continue_availability=Hidden`
- [ ] **AC-8**: GIVEN 会话中途进入 `SaveLocked`，WHEN 玩家尝试提交世界修复、长期资源积累、关系/村镇变化或飞艇家园布置，THEN 系统必须阻止正式提交并显示 Retry Save Capability / Return Title / Enter Temporary Flight 选择
- [ ] **AC-9**: GIVEN 玩家从 `SaveLocked` 选择 Enter Temporary Flight 并经二次确认，WHEN `enter_temporary_flight()` 被调用，THEN `mode=EphemeralOnly`，`continue_availability=Hidden`，禁止所有正式 commit class
- [ ] **AC-10**: GIVEN 每次保存、迁移、恢复、备份提升或 capability probe 完成，WHEN 开发诊断摘要生成，THEN 摘要包含快照字节数、配额余量、encode/write/readback/checksum 耗时、promotion 结果、失败原因码、pagehide 结果和备份提升结果；保存热路径只追加预分配固定大小结构化记录，不分配超过 4 KiB 诊断记录，不同步生成完整可复制文本报告

---

## Implementation Notes

- Lifecycle event wiring:
  - Custom HTML shell 注册 `visibilitychange`、`pagehide`、`pageshow`、focus/blur listener
  - `JavaScriptBridge.create_callback()` 创建持久回调引用——在页面结束前不得释放
  - Godot 启动前收到的事件缓存到 JS 侧数组，启动后批量传递给平台适配层
  - 平台适配层将缓存事件转换为幂等 lifecycle token
- `pagehide` best-effort 路径:
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

---

## Out of Scope

- Story 001-007: 各自的保存/恢复/迁移/备份核心逻辑
- Custom HTML shell 的完整实现（由 platform-session-shell Epic 的 Story 003 覆盖 lifecycle wiring）
- UI 呈现（SaveLocked overlay、EphemeralOnly warning、RecoveryRequired screen）——由本 Epic 的 UI 需求在各 Story 的 QA 验证中覆盖，但 UI 实现由 platform-session-shell Story 007 和专门的 UI Story 负责
- 诊断报告的可复制文本格式化——由 diagnostic UI（content-registry Story 007）覆盖

---

## QA Test Cases

- **AC-1**: Pagehide with in-progress staging → best-effort only
  - Given: 稳定边界已触发 staging write，verify 尚未开始，pagehide 事件到达
  - When: `_on_pagehide()`
  - Then: 发出 best-effort flush（若有预编码 marker）；`promotion_success` 保持 false；`last_verified_checkpoint` 不变
  - Edge cases: pagehide 在 verify 中途到达 → 同样不 promotion

- **AC-3**: Pagehide budget exceeded → abandon
  - Given: pagehide 触发，预编码 staging 存在但 flush 超过 20ms budget
  - When: flush 超时
  - Then: 放弃本次推进；保留旧 `last_verified_checkpoint`；记录 `PERF_PAGEHIDE_BUDGET_EXCEEDED`
  - Edge cases: 无预编码 staging → 直接放弃，不尝试 flush

- **AC-9**: SaveLocked → Enter Temporary Flight → EphemeralOnly
  - Given: 会话中途 promotion 失败进入 SaveLocked，玩家选择 Enter Temporary Flight 并二次确认
  - When: `enter_temporary_flight()` 执行
  - Then: `mode=EphemeralOnly`、`continue_availability=Hidden`、所有正式 commit class 被拒绝
  - Edge cases: 玩家 Return Title 而非 Enter Temporary Flight → 保留旧 Safe、current_generation 不变

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/persistence/web_lifecycle_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (staging/promotion)；Story 003 (storage_capability——pagehide 后需重探)；Story 004 (continue_availability——lifecycle 事件后重算)；Story 007 (artifact isolation——两侧各自 best-effort)
- Unlocks: platform-session-shell Story 003 (Background Suspend/Resume——消费本 Story 提供的 lifecycle token)
