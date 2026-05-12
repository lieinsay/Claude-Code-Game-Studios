# Story 005: Interactable Base Class & Registry

> **Epic**: Player Movement & Interaction
> **Status**: Done
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/player-movement-interaction.md`
**Requirement**: `TR-movement-005`

**ADR Governing Implementation**: ADR-0004: InteractionHandler @abstract
**ADR Decision Summary**: 所有可交互对象必须继承 `Interactable extends Node2D` @abstract 基类。基类定义 6 个字段（interaction_id: StringName、anchor_radius、priority、interaction_type、is_enabled、is_busy）+ @abstract `handle_use()` 方法。`InteractionRegistry`（Autoload #3）管理注册/注销 + 候选池 + 焦点状态机 + Use Gate + 分发。注册在 `_ready()` 中执行，注销在 `queue_free()` 前执行。场景过渡时先清空焦点、再注销目标、最后释放场景。

**Engine**: Godot 4.6.2 | **Risk**: MEDIUM
**Engine Notes**: `@abstract` 装饰器自 Godot 4.5 引入；`StringName` 用于稳定 ID（O(1) 比较）；未实现 `handle_use()` 的子类在场景加载时报错。

**Control Manifest Rules (Foundation layer)**:
- Required: `Interactable` 是唯一可交互对象基类；`@abstract handle_use()` 强制子类实现；`interaction_id: StringName` 作为稳定身份
- Forbidden: 不得绕过 `Interactable` 直接 emit `interaction_used`；不得使用显示名/节点路径作为交互身份
- Guardrail: `max_focus_candidates_per_query = 8`；候选池 O(N) where N ≤ 8

---

## Acceptance Criteria

- [ ] **AC-1**: GIVEN `Interactable` 基类定义了 `@abstract handle_use(player_id: StringName) -> UseResult`，WHEN 子类未实现此方法，THEN 场景加载时报错
- [ ] **AC-2**: GIVEN 场景实例化，WHEN 每个 `Interactable._ready()` 执行，THEN `InteractionRegistry.register(self)` 被调用，候选池包含该目标
- [ ] **AC-3**: GIVEN 场景即将销毁，WHEN exit cleanup 协议执行，THEN 所有 Interactable 在 `queue_free()` 前调用 `unregister(self)`，候选池移除这些目标
- [ ] **AC-4**: GIVEN 场景过渡开始，WHEN exit cleanup 执行，THEN 先清空焦点（若当前焦点属于此场景）、再注销所有 Interactable、再 `queue_free()`
- [ ] **AC-5**: GIVEN 同一稳定 ID 的对象销毁后重建，WHEN 新对象注册，THEN 旧焦点状态、粘性加成和待处理 Use 均不保留（视为全新目标）
- [ ] **AC-6**: GIVEN 任何可交互对象，WHEN 查询其交互身份，THEN 必须通过 `interaction_id: StringName` 字段，不得使用 display_name、node_path 或临时引用
- [ ] **AC-7**: GIVEN `InteractionRegistry` 作为 Autoload #3，WHEN `_ready()` 执行，THEN 只声明信号和初始化空候选池——不做文件 I/O、场景实例化、音频播放

---

## Implementation Notes

- `Interactable` 基类结构:
  ```text
  @icon("res://assets/icons/interactable.svg")
  class_name Interactable
  extends Node2D

  @export var interaction_id: StringName
  @export var anchor_radius: float = 0.45
  @export var priority: float = 0.5
  @export var interaction_type: StringName  # "talk"/"use"/"trade"/"repair"/"open"

  @abstract func handle_use(player_id: StringName) -> UseResult

  func is_enabled() -> bool: return true
  func is_busy() -> bool: return false
  func get_display_hint() -> String: return ""
  func get_anchor_position() -> Vector2: return global_position

  enum UseResult { ACCEPTED, REJECTED, BUSY }
  ```
- `InteractionRegistry` Autoload 职责:
  - `register(target: Interactable)` / `unregister(target: Interactable)`
  - 候选池管理: `Array[Interactable]`，≤8 候选
  - 焦点状态机（5 态）—— Story 003
  - Use Gate —— Story 004
  - Keyboard Tab 焦点循环
  - 公共读查询: `query_focus_state() -> Dictionary`
- 注册/注销生命周期:
  ```
  Scene _ready() → Interactable._ready() → register(self) → 候选池 += target
  Scene exit_cleanup() → clear_focus(for this scene) → Interactable.unregister(self) → scene.queue_free()
  ```
- 场景过渡期间候选池暂时为空 → 焦点自动进入 NoFocus
- `StringName` 用于 interaction_id — O(1) 比较

---

## Out of Scope

- Story 003: 焦点状态机和评分公式的具体实现
- Story 004: Use Gate 判定和 Use dispatch 流程
- Story 006: 语义事件发送
- 各领域系统 `Interactable` 子类的具体实现（Hub IntelStation、Exploration ScavengePoint 等）

---

## QA Test Cases

- **AC-1**: @abstract enforcement
  - Given: 子类 `BrokenInteractable extends Interactable` 未实现 `handle_use()`
  - When: 场景包含 BrokenInteractable 实例化
  - Then: Godot 引擎报错，场景加载失败
  - Edge cases: 子类实现了 `handle_use()` 但返回类型不是 `UseResult` → C# 类型错误

- **AC-3**: Unregister before queue_free
  - Given: 场景中有 3 个 Interactable，候选池包含它们
  - When: 场景 exit_cleanup → unregister 全部 3 个 → queue_free
  - Then: 候选池为空；之后任何 Use Gate 检查无悬垂引用
  - Edge cases: 场景销毁时某个 Interactable 处于 UseLocked → 先 force_release_lock → unregister

- **AC-5**: ID reuse treated as new target
  - Given: 目标 ID "hub.interactable.helm" 被销毁 → 同 ID 新实例创建
  - When: 新实例注册到 Registry
  - Then: focus_stickiness_bonus=0（无旧焦点加成）；pending Use 不保留
  - Edge cases: 同 ID 在不同场景中 → 各自独立（场景切换时旧场景的全部注销）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/movement/MovementInteractableRegistryTest.csproj` — must exist and pass
**Status**: [x] Implemented and passing (2026-05-12)

---

## Dependencies

- Depends on: ADR-0001 (InteractionRegistry Autoload #3 位置 + Phase 4 初始化)；ADR-0002 (信号 typed params 契约)
- Unlocks: Story 003 (候选池数据来源)；Story 004 (handle_use 分发目标)；Story 007 (场景过渡时 unregister 时机)
