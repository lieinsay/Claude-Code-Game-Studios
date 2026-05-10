# Story 007: Cross-System Boundaries & Desktop Lifecycle Constraints

> **Epic**: Player Movement & Interaction
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-007`

**ADR Governing Implementation**: ADR-0001: Autoload/Scene Boot Order; ADR-0019: Desktop C# Platform Pivot
**ADR Decision Summary**: 本系统不拥有货币、库存、资源、修复、市场、模块安装、探索奖励、战斗、剧情或存档结果。不触发场景切换——只在新场景加载后被场景过渡系统放置。场景过渡时立即清空焦点。桌面窗口生命周期事件由壳层归一化后传入——本系统不直接订阅 `window_focus_changed`/`suspend_requested`。音频在用户手势激活前静默。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: `CharacterBody2D` 位置由场景过渡系统通过 `global_position` 设置；`Input.is_anything_pressed()` 用于 desktop resume 恢复检测。

**Control Manifest Rules (Foundation layer)**:
- Required: 场景过渡时清空焦点 + 注销所有 Interactable；`direct_cross_autoload_in_ready` 禁止
- Forbidden: 不得直接读写 currency/inventory/save/repair/market 变量；不得触发场景切换；不得直接订阅桌面窗口生命周期事件
- Guardrail: 音频在用户手势激活前不播放

---

## Acceptance Criteria

### Cross-System Boundaries

- [ ] **AC-1**: GIVEN 本系统代码路径，WHEN 代码审查，THEN 无 currency/money/gold 引用（不读写货币）
- [ ] **AC-2**: GIVEN 本系统代码路径，WHEN 代码审查，THEN 无 inventory/items 引用（不读写库存）
- [ ] **AC-3**: GIVEN 本系统代码路径，WHEN 代码审查，THEN 无 save/write/persist 调用（不直接写档）
- [ ] **AC-4**: GIVEN 玩家对集市摊位按 E，WHEN `interaction_used` 发出，THEN 购买逻辑不由本系统执行——由领域系统在 `handle_use()` 中处理
- [ ] **AC-5**: GIVEN 任何代码路径，WHEN 代码审查，THEN 无场景加载/切换调用（不触发场景过渡）
- [ ] **AC-6**: GIVEN 场景 A 中玩家聚焦目标，WHEN 场景过渡触发，THEN 场景 B 加载后 `world_focus_id = null`（不继承旧焦点）
- [ ] **AC-7**: GIVEN 同一稳定 ID 的对象被销毁后重建，WHEN 新对象注册，THEN 旧焦点状态、粘性加成和待处理 Use 均不保留

### Desktop Lifecycle

- [ ] **AC-8**: GIVEN 桌面窗口 `suspend_requested`/`window_focus_changed=hidden` 通过壳层转发为 `input_gate_closed`，WHEN 窗口失焦或暂停，THEN 输入门关闭，角色位置不变
- [ ] **AC-9**: GIVEN 用户首次点击/按键（audio device readiness 激活）之前，WHEN 玩家按移动键或在目标间切换焦点，THEN 语义事件正常发出但反馈系统不播放音频
- [ ] **AC-10**: GIVEN 桌面窗口恢复时鼠标正悬停在目标上，WHEN 恢复帧执行，THEN 焦点仅在下一有效玩法帧刷新；恢复帧不自动执行 Use

---

## Implementation Notes

### Cross-System Boundaries

- 本系统 "不拥有" 清单: currency, inventory, resources, save data, repair state, market stock, module installation, exploration rewards, combat results, story progress
- 领域后果分离: `handle_use()` 由领域系统实现——本系统只调用并等待 `UseResult`
- 场景过渡协议:
  1. 旧场景 exit_cleanup → 清空焦点（若当前焦点属于该场景）
  2. 所有 Interactable.unregister()
  3. scene.queue_free()
  4. 新场景加载 → 场景过渡系统放置 Player → 新场景 _ready() → Interactable 注册
  5. 新场景 `world_focus_id = null`（不继承）
- ID 重用规则: `interaction_id` 相同的不同实例视为不同目标；`unregister` → 清除相关状态 → 新 `register` 重新开始

### Desktop Lifecycle Constraints

- 桌面窗口生命周期事件边界: 本系统只接收壳层归一化后的 `input_gate_open`/`input_gate_closed`/`input_gate_reacquire` 信号——不直接订阅 `window_focus_changed`、`suspend_requested`、`resume_requested`、`focus`、`blur`
- desktop resume 恢复:
  - `resume_requested.persisted=true` → 壳层发出 `input_gate_reacquire`
  - 恢复帧不自动执行 Use（即使鼠标悬停在目标上）
  - 焦点在下一有效玩法帧刷新
- 音频手势门: 壳层在首次用户交互后激活 audio device readiness → FeedbackManager 开始播放音频。在此之前，所有语义事件正常 emit，FeedbackManager 自行判断是否静默

---

## Out of Scope

- Story 002: Input gate 状态机和壳层信号的具体实现
- Story 005: 注册/注销机制（本 Story 只验证边界隔离）
- Story 006: 语义事件的具体发射（本 Story 只验证 桌面约束下事件行为）
- 壳层如何检测桌面窗口生命周期（由 platform-session-shell 拥有）

---

## QA Test Cases

- **AC-6**: Focus cleared on scene transition
  - Given: 场景 A 中 `world_focus_id="hub.helm"`、`focus_state="Focused"`
  - When: 触发场景过渡到场景 B
  - Then: exit_cleanup 中焦点清空 → 所有 Interactable 注销 → 场景 B 加载后 `query_focus_state()` 返回 `world_focus_id=""`、`focus_state="NoFocus"`
  - Edge cases: 场景过渡期间按 E → `use_gate=Blocked(input_closed)`（门在过渡开始时关闭）

- **AC-10**: desktop resume restore with mouse over target
  - Given: 鼠标悬停在 target A 上，标签页被隐藏（desktop resume），然后切回
  - When: `resume_requested.persisted=true` → 壳层发出 `input_gate_reacquire`
  - Then: 恢复帧不自动 Use；`pointer_score` 在下一有效玩法帧刷新；焦点在键盘/鼠标有效输入后才重新获取
  - Edge cases: 恢复时 target A 已被禁用 → 焦点不获取 A

- **AC-4**: Domain consequence separation
  - Given: 玩家对摊位 target（interaction_type="trade"）按 E
  - When: `handle_use()` 返回 ACCEPTED
  - Then: 本系统不执行购买/库存修改/货币扣减——这些只在领域系统的 `handle_use()` 中发生
  - Edge cases: 领域系统返回 REJECTED → 本系统报告 `target_disabled`/`blocked`，不执行任何回退（领域系统自己处理）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/movement/CrossSystemBoundariesTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-006 (所有核心系统)；platform-session-shell Story 001 (input gate signals)
- Unlocks: 所有下游领域系统的交互实现（Hub/Exploration/Settlement/WorldRepair）
