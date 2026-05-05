# Story 003: Background Suspend / Resume

> **Epic**: Platform Session Shell
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-002`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0006: Web Platform Constraints
**ADR Decision Summary**: 标签页隐藏/窗口失焦时壳层进入 BackgroundSuspended——输入停止、世界推进停止。visibilitychange/pagehide 触发轻量 marker flush（非完整 safe checkpoint）。BFCache 恢复 (`pageshow.persisted=true`) 需重新验证继续点。beforeunload/unload 不得启动新序列化。ResumePending 中首次输入只重新激活，不触发玩法动作。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: `JavaScriptBridge` 接收 `visibilitychange`/`pageshow`/`pagehide`/`beforeunload` 事件；单线程 Web 导出——长时间序列化在 beforeunload 中不可靠；`pagehide` 中 ≤20ms 预算。

**Control Manifest Rules (Foundation layer)**:
- Required: 后台挂起时输入不得进入玩法层；恢复时 delta-based resume
- Forbidden: beforeunload 中不得启动阻塞式保存
- Guardrail: pagehide 处理 ≤20ms

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN SessionActive，WHEN 标签页隐藏、窗口失焦、visibilitychange hidden 或浏览器暂停，THEN 进入 BackgroundSuspended——玩法输入停止，世界推进停止
- [ ] **AC-2**: GIVEN SessionActive，WHEN visibilitychange hidden/pagehide/浏览器丢弃页面/刷新前事件，THEN 只请求预编码 marker/lightweight flush，不得请求完整 safe checkpoint
- [ ] **AC-3**: GIVEN pageshow.persisted=true (BFCache 恢复)，WHEN 页面回到前台，THEN 进入 ResumePending 并重新执行恢复检查，不得直接回到 SessionActive
- [ ] **AC-4**: GIVEN beforeunload 或 unload 触发，WHEN 壳层处理关闭前信号，THEN 不得启动新序列化、迁移、备份提升或阻塞式保存
- [ ] **AC-5**: GIVEN 从后台返回进入 ResumePending，WHEN 页面已可见但玩家尚未显式重新激活，THEN 玩法输入仍被阻断——第一下输入只用于重新激活
- [ ] **AC-6**: GIVEN ResumePending，WHEN 玩家按下键盘/鼠标作为恢复操作，THEN 该输入只用于重新激活和同手势音频恢复，不能触发任何普通玩法动作
- [ ] **AC-7**: GIVEN ResumePending，WHEN 玩家选择 Return Title，THEN 返回 Ready 或安全标题状态，不把返回输入传给玩法层

---

## Implementation Notes

- 浏览器事件通过 `JavaScriptBridge` 回调桥接到 Godot: `_on_visibility_change(visible: bool)`, `_on_page_show(persisted: bool)`, `_on_page_hide()`, `_on_before_unload()`
- Lightweight flush: 写入预编码 marker（8 字节: 4 字节 session_id + 4 字节 timestamp）到 IndexedDB——不序列化完整快照
- Marker flush 超时保护: `await _flush_marker()` 但 ≤20ms 硬超时（Godot `OS.get_ticks_msec()` 检查）
- BFCache 恢复检测: `pageshow.persisted` → 标记 `_bfcache_restored = true` → ResumePending 中额外校验存档系统 continue_point 仍有效
- 挂起 token: `{suspend_timestamp: int, session_pos: Vector2, screen: StringName, marker_flushed: bool}`
- Resume 输入捕获: `_resume_activation_consumed: bool`——第一次输入设置此标志为 true 并解锁 `input_gate` (Open)

---

## Out of Scope

- Story 001: BackgroundSuspended/ResumePending 状态定义和转换守卫
- Story 004: Resume 恢复失败后的 RecoveryRequired 路径
- Persistence (#3): lightweight flush 的具体存储实现

---

## QA Test Cases

- **AC-1**: Tab hidden → suspended
  - Given: SessionActive，玩家在 Hub 中
  - When: visibilitychange → hidden
  - Then: shell_state → BACKGROUND_SUSPENDED，`input_gate: Blocked`，world 时间停止
  - Edge cases: 恢复后 delta time 不补偿——游戏时间只拨到恢复时刻

- **AC-5**: First input after resume = reactivation only
  - Given: ResumePending，恢复面板显示 "按下任意键继续"
  - When: 玩家按 W 键
  - Then: 角色不移动——W 输入被消耗为重新激活；`input_gate` → Open
  - Edge cases: 鼠标点击同样只重新激活——不触发 Click-to-Move

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/session/suspend_resume_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (State Machine), ADR-0006 (Web lifecycle events)
- Unlocks: Story 006 (Input Gate 与 Resume 交互)
