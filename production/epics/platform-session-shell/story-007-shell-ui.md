# Story 007: Shell UI — Entry, Loading & Error Screens

> **Epic**: Platform Session Shell
> **Status**: Ready
> **Layer**: Foundation
> **Type**: UI
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/platform-session-shell.md`
**Requirement**: `TR-platform-001`

*Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time.*

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order, ADR-0006: Web Platform Constraints, ADR-0012: UI / Input Routing
**ADR Decision Summary**: 壳层 UI 是进入/加载/音频/恢复/错误提示的焦点所有者——包含 Loading 屏幕、Start/Continue 入口、Audio 激活确认、PreservedLocked 说明、EphemeralOnly 警告、FatalBlocked 安全错误 UI、RecoveryRequired 重试界面。壳层 UI 仅在 SessionActive 无 overlay 时将焦点交给下游。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Godot Control 节点——Web 兼容（Compatibility 渲染器）；loading 动画使用 `_process()` 中的 delta 驱动但不能超过 2ms/frame。

**Control Manifest Rules (Foundation layer)**:
- Required: 键盘优先——所有 UI 必须有键盘快捷键
- Forbidden: 不得使用营销式大卡片或装饰 hero——工具密度优先
- Guardrail: 壳层 UI 渲染 <2ms/frame

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN 继续点不存在，WHEN 渲染入口，THEN Continue 必须隐藏或不可选——不得出现伪装成可继续的默认会话
- [ ] **AC-2**: GIVEN 继续点存在但结构损坏/校验失败/版本不匹配/内容域标识不匹配，WHEN 渲染入口，THEN Continue 保持为锁定/禁用态并给出可理解提示——继续点不得被自动删除或覆盖
- [ ] **AC-3**: GIVEN EphemeralOnly 存储，WHEN 玩家选择 Start，THEN 先显示临时会话无保存提示；确认后进入临时会话
- [ ] **AC-4**: GIVEN FatalBlocked，WHEN 渲染安全错误 UI，THEN 只保留安全错误态与重试/返回类入口——不得进入玩法
- [ ] **AC-5**: GIVEN RecoveryRequired，WHEN 渲染恢复 UI，THEN 显示 Retry/New Session/Return Title 选项
- [ ] **AC-6**: GIVEN Ready/AwaitingAudioActivation/ResumePending/RecoveryRequired/FatalBlocked 任一 UI 状态显示，WHEN 只使用键盘操作，THEN 玩家必须能到达主操作、返回/退出操作和可用错误详情/无声继续入口
- [ ] **AC-7**: GIVEN Loading 状态，WHEN 渲染加载屏幕，THEN 必须显示加载进度（子阶段文本或进度条）——不显示空白屏幕
- [ ] **AC-8**: GIVEN PreservedLocked 继续点，WHEN 渲染 Continue 入口，THEN 显示锁定原因路径 + Return Title + New Session 按钮

---

## Implementation Notes

- 壳层 UI 使用独立 CanvasLayer（layer 最高，高于 HUD 和玩法层）——仅在非 SessionActive 或无 overlay 时降低
- Loading 屏幕: 显示加载子阶段文本 (`loading_phase_changed` 信号驱动) + 动画进度条
- Entry 屏幕 (Ready): Start 按钮（快捷键 Enter）+ Continue 按钮（Enable/Locked/Hidden 三态）+ Settings 入口
- PreservedLocked 面板: 锁定图标 + 原因文本 + "开始新会话" 按钮 + "返回标题" 按钮——Continue 按钮灰显不可点击
- EphemeralOnly 确认弹窗: "⚠ 本次进度不会保存——浏览器存储不可用。是否继续？" + "继续（不保存）" / "返回"
- FatalBlocked 屏幕: 错误图标 + 面向玩家的安全提示文本 + "重试" / "刷新页面" 按钮——无 ERR_* 代码
- RecoveryRequired 屏幕: "无法恢复会话" + Retry/New Session/Return Title——保留继续点状态
- 键盘导航: Tab 顺序 Start→Continue→Settings→Return Title；Enter=确认；Esc=返回

---

## Out of Scope

- Story 002: Start/Continue 逻辑和 Audio Activation 逻辑
- Story 004: failure_severity 判定
- ADR-0012 (UIManager): 正式游戏 UI/HUD——壳层 UI 独立于 UIManager

---

## QA Test Cases

- **AC-6**: Keyboard-only navigation through all shell states
  - Setup: Ready 状态
  - Verify: Tab→Start focus→Enter→触发 Start（如有 audio gate→AwaitingAudioActivation→Enter→确认→SessionStarting）
  - Pass condition: 从 Ready 到 SessionActive 的完整流程仅用键盘完成
  - Edge cases: FatalBlocked 中 Tab→Retry→Enter→触发重试

- **AC-7**: Loading screen with phase text
  - Setup: 启动游戏，Loading 状态
  - Verify: 屏幕显示当前子阶段文本（如 "正在检查游戏数据..."）和进度条
  - Pass condition: 子阶段文本随 loading_phase_changed 信号更新
  - Edge cases: 加载超时（>10s）→显示 "加载时间较长——请稍候" + 取消按钮

---

## Test Evidence

**Story Type**: UI
**Required evidence**: `production/qa/evidence/shell-ui-evidence.md` — manual walkthrough with screenshots + keyboard navigation verification
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (State Machine states), Story 002 (Entry logic), Story 004 (Failure states), Story 005 (Ephemeral/PreservedLocked states)
- Unlocks: None — Story 007 是 platform-session-shell Epic 的最终 UI 层
