# Story 006: State Machine & Pool Transitions

> **Epic**: Resources, Goods & Capacity
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-006`

**ADR Governing Implementation**: ADR-0005: Resource Pool System
**ADR Decision Summary**: 6 个规范池（`on_person`/`in_storage`/`loaded`/`listed`/`carried`/`deposited`）+ 2 个终态（`deposited` 和 `destroyed`）。`deposited` 为不可逆终态——资源不可从修复节点取回。`destroyed` 为终态——资源退出所有池。资源不能同时在两个池中。跨池转移必须通过原子 `transfer` 原语包裹。状态即"当前所在池"——通过 `transfer()` 在各池间移动。探索失败时 `carried` 池按 `extraction_loss_ratio` 部分进入 `destroyed`，剩余自动回到 `in_storage`。`on_person` 中的物品不参与探索风险。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Dictionary 六池状态在单对象内管理；所有转移在同一 tick 完成

**Control Manifest Rules (Foundation layer)**:
- Required: 所有状态变更通过 ResourcesManager 原子操作——领域系统不得直接修改池内容
- Forbidden: 绕过状态机写入 `_pools`；资源同时存在于两个池
- Guardrail: `deposited` 和 `destroyed` 为终态——不可逆

---

## Acceptance Criteria

### Pool Boundaries

- [ ] **AC-1**: GIVEN 资源在 `on_person` 中，WHEN 检查所有 6 个池，THEN 该资源仅在 `on_person` 中出现——不在其他任何池中
- [ ] **AC-2**: GIVEN `transfer(on_person, in_storage, id, qty)` 成功，WHEN 检查池状态，THEN `on_person` 中数量减少 qty, `in_storage` 中数量增加 qty, 总量守恒

### Terminal States

- [ ] **AC-3**: GIVEN `commit_deposit(repair_node_id, costs)` 成功，WHEN 尝试 `transfer(deposited, in_storage, resource_id, qty)`，THEN 操作失败——资源不可从修复节点移出
- [ ] **AC-4**: GIVEN `consume()` 成功，WHEN 检查所有 6 个池，THEN 消耗的资源不出现在任何池中（destroyed 终态）
- [ ] **AC-5**: GIVEN `commit_deposit()` 在 UI 确认流程中，WHEN 玩家点击"取消"，THEN 不执行 `commit_deposit()`——资源保留在原池

### Pool-to-Pool Transition Rules

- [ ] **AC-6**: GIVEN 资源在 `in_storage`，WHEN `transfer(in_storage, on_person, id, qty)`，THEN 转移成功（仓库→随身允许）
- [ ] **AC-7**: GIVEN 资源在 `in_storage`，WHEN `transfer(in_storage, loaded, id, qty)` 且 item kind=cargo，THEN 转移成功（仓库→货舱装载允许）
- [ ] **AC-8**: GIVEN 资源在 `loaded`，WHEN `transfer(loaded, in_storage, cargo_id, 1)`，THEN 转移成功（货舱→仓库卸货允许）
- [ ] **AC-9**: GIVEN 资源在 `carried`，WHEN `extract_carried_to_storage()`（探索结算），THEN 全部 carried 内容归入 `in_storage`，`carried` 清空

### Exploration Loss (carried pool)

- [ ] **AC-10**: GIVEN `carried` 中有 basic × 10（stackable），探索失败 loss_ratio=0.4，WHEN 结算损失，THEN loss = min(10-1, max(1, ceil(10×0.4))) = 4, retention = 6 → 6 归入 `in_storage`
- [ ] **AC-11**: GIVEN `carried` 中有 intel × 1（unique, Q=1），探索失败，WHEN 结算损失，THEN loss = 0（Q=1 unique 物品不可被完全摧毁）, retention = 1 → 1 归入 `in_storage`
- [ ] **AC-12**: GIVEN `on_person` 中有 basic × 10，探索失败，WHEN 结算损失，THEN `on_person` 中的物品完全不受影响——仅 `carried` 受探索失败影响

### Non-Transferable Ownership

- [ ] **AC-13**: GIVEN 领域系统直接修改 `_pools` 内容，WHEN 代码审查，THEN 所有修改通过 ResourcesManager 公共 API——无直接写入 `_pools` 的代码路径

---

## Implementation Notes

### State Machine

```
States (Pool):

  on_person ──→ in_storage ──→ loaded ──→ carried ──→ in_storage (探索成功)
      │              │            │           │
      │              │            │           └──→ destroyed (探索失败部分损失)
      │              │            │
      │              │            └──→ listed (上架集市)
      │              │            └──→ deposited (直接提交修复)
      │              │
      │              └──→ listed (上架集市)
      │              └──→ deposited (提交修复)
      │
      └──→ carried (进入探索选带)
      └──→ deposited (直接提交修复)
      └──→ destroyed (消耗)

  listed ──→ in_storage (下架取回)
       ──→ destroyed (被买走)

  carried ──→ in_storage (撤离成功)
        ──→ destroyed (探索失败按比例)

  deposited ──→ [终态 — 不可逆]
  destroyed ──→ [终态 — 不可逆]
```

### Terminal State Enforcement

```gdscript
func transfer(from_pool: StringName, to_pool: StringName, ...) -> ResourceResult:
    # deposited 不可转出（终态守卫）
    if from_pool == &"deposited":
        return ERR_CANNOT_TRANSFER_FROM_DEPOSITED
    # destroyed 资源不存在于任何池——无法 transfer
```

### Exploration Loss Formula Interface

本系统不拥有 `extraction_loss_ratio` 参数——它由探索系统拥有。本系统暴露结算接口：

```gdscript
func extract_carried_to_storage() -> Dictionary:
    # 探索成功撤离: 全部 carried → in_storage
    # 返回转移摘要

func apply_extraction_loss(loss_ratio: float) -> Dictionary:
    # 探索失败: 按 loss_ratio 从 carried 中移除, 剩余归入 in_storage
    # Pillar 4 约束: Q=1 unique 物品不受损失
    # 返回损失/保留摘要
```

### Cross-Pool Integrity

每次 `transfer()` 内部:
1. 从源池 `remove()` → 临时持有
2. 向目标池 `add()` → 若失败则回滚（当前实现为：目标容量已在步骤 1 前预验证，步骤 2 不会失败）
3. 同一帧内完成——无中间可见状态

---

## Out of Scope

- `commit_deposit()` 的具体实现（Story 007）
- `extract_carried_to_storage()` 和 `apply_extraction_loss()` 的探索调用逻辑（Exploration Epic）
- `listed` 池的管理（Settlement Epic 通过 ResourcesManager API 操作）
- 信号发射（Story 008）

---

## QA Test Cases

- **AC-3**: Deposited cannot be withdrawn
  - Given: `commit_deposit(repair_node, {basic: 5})` 已成功
  - When: `transfer(deposited, in_storage, basic_id, 5)`
  - Then: 返回错误, 资源仍在 deposited 中
  - Edge cases: `remove(deposited, ...)` → 也返回错误

- **AC-11**: Unique items survive exploration failure
  - Given: carried 中有 intel (Q=1, unique), loss_ratio=0.4
  - When: `apply_extraction_loss(0.4)`
  - Then: intel 0 损失, 完整归入 in_storage
  - Edge cases: carried 中有 basic×5 和 intel×1 → basic 损失按公式, intel 零损失

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/resources/state_machine_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 005 (core operations), Story 001-004 (all foundational logic)
- Unlocks: Story 007 (specialized ops), Story 009 (persistence)
