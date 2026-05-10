# Story 005: Core Atomic Operations

> **Epic**: Resources, Goods & Capacity
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-005`

**ADR Governing Implementation**: ADR-0005: Resource Pool System
**ADR Decision Summary**: 7 种原子操作返回类型化 `ResourceResult` enum（SUCCESS + 11 种 ERR_* 错误码）。所有操作为全成功或全失败——不产生中间状态或部分变更。`add()` 优先合并已有同 ID 堆；`remove()` 从数量最大的堆开始移除（多堆拆分）；`transfer()` 支持拆分（qty < 源堆数量），原子执行源移除+目标添加；`consume()` 语义同 remove（资源进入 destroyed 终态）。所有操作在单帧内同步完成。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 引擎主线程同步执行；无并发竞争；操作在单 tick 内完成

**Control Manifest Rules (Foundation layer)**:
- Required: 所有操作返回 `ResourceResult` enum；原子 all-or-nothing；`transfer()` 源移除+目标添加同一帧
- Forbidden: 操作中的 `await` 或延迟执行（破坏原子性）；部分成功/部分失败
- Guardrail: 零数量返回 SUCCESS（无变更）；负数量返回 `ERR_INVALID_QUANTITY`

---

## Acceptance Criteria

### add() Operation

- [ ] **AC-1**: GIVEN 仓库有 basic E=90（max_stack=99），WHEN `add(in_storage, basic_id, 30)`，THEN merge_qty=9 合并→已有堆 99, overflow_qty=21 创建新堆（1 堆 21）, 返回 SUCCESS
- [ ] **AC-2**: GIVEN 随身 5/5 槽已满且无匹配堆，WHEN `add(on_person, new_id, 1)`，THEN 返回 `ERR_CARRY_SLOTS_FULL`, 随身状态未变更
- [ ] **AC-3**: GIVEN 仓库有 basic × 10，WHEN `add(in_storage, basic_id, 0)`，THEN 返回 SUCCESS, 池内容不变
- [ ] **AC-4**: GIVEN 任意池，WHEN `add(pool, id, -5)`，THEN 返回 `ERR_INVALID_QUANTITY`

### remove() Operation

- [ ] **AC-5**: GIVEN 仓库 basic: [堆0: 50, 堆1: 30]（总量 80），WHEN `remove(in_storage, basic_id, 40)`，THEN 从最大堆（50）移除 40 → 堆0 剩 10, 堆1 仍 30, 返回 SUCCESS
- [ ] **AC-6**: GIVEN 仓库 basic: [堆0: 50]（总量 50），WHEN `remove(in_storage, basic_id, 60)`，THEN 返回 `ERR_SOURCE_INSUFFICIENT`, 仓库不变
- [ ] **AC-7**: GIVEN 仓库 basic: [堆0: 30, 堆1: 30]（两个相同数量堆），WHEN `remove(in_storage, basic_id, 20)`，THEN 从最低槽位索引的堆移除 20

### transfer() Operation

- [ ] **AC-8**: GIVEN 仓库 basic × 50（一个堆），随身 2/5 槽且无 basic 堆，WHEN `transfer(in_storage, on_person, basic_id, 20)`，THEN 仓库 basic 变为 30, 随身获得 basic × 20（1 堆）, 返回 SUCCESS
- [ ] **AC-9**: GIVEN 仓库 basic × 3，WHEN `transfer(in_storage, on_person, basic_id, 5)`，THEN 返回 `ERR_SOURCE_INSUFFICIENT`, 仓库和随身均未变更
- [ ] **AC-10**: GIVEN 仓库 basic × 30，随身 5/5 槽满且无匹配堆，WHEN `transfer(in_storage, on_person, basic_id, 10)`，THEN 目标容量不足 → `ERR_TARGET_FULL`, 仓库不变
- [ ] **AC-11**: GIVEN 仓库 basic × 50，WHEN `transfer(in_storage, on_person, basic_id, 20)` 且目标有匹配堆 E=90（max_stack=99），THEN merge_qty=9 合并→99, overflow_qty=11 创建新堆

### consume() Operation

- [ ] **AC-12**: GIVEN 仓库 basic × 10，WHEN `consume(in_storage, basic_id, 5)`，THEN 仓库 basic 变为 5, 消耗的 5 进入 destroyed 终态, 返回 SUCCESS
- [ ] **AC-13**: GIVEN 仓库 basic × 3，WHEN `consume(in_storage, basic_id, 5)`，THEN 返回 `ERR_SOURCE_INSUFFICIENT`, 仓库 basic 仍为 3

### Atomicity Guarantee

- [ ] **AC-14**: GIVEN `transfer()` 执行期间发生容量不足，THEN 源移除和目标添加都不执行——源池完整保留
- [ ] **AC-15**: GIVEN 任何操作返回非 SUCCESS，THEN 池状态与操作前完全一致（无部分变更）

---

## Implementation Notes

### ResourceResult Enum (from ADR-0005 Section 2)

```text
enum ResourceResult {
    SUCCESS,
    ERR_TARGET_FULL,
    ERR_SOURCE_INSUFFICIENT,
    ERR_CAPACITY_ZERO,
    ERR_INVALID_QUANTITY,
    ERR_MISSING_REFERENCE,
    ERR_DEPRECATED_ID,
    ERR_STORAGE_FULL,
    ERR_CARRY_SLOTS_FULL,
    ERR_CARRY_STACK_FULL,
    ERR_CARGO_NOT_IN_BAY,
    ERR_BUSY,
    ERR_KIND_MISMATCH,
}
```

### Atomic Operation Pattern

```text
func add(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 1. 验证: quantity >= 0, resource_id 在 Registry 中且未 retired
    # 2. 计算: stack_merge(pool_id, resource_id, quantity)
    # 3. 检查: 容量（槽位/容积）
    # 4. 若通过: 执行合并 + 创建新堆
    # 5. emit 信号 (Story 008)
    # 6. return SUCCESS or ERR_*
```

### remove() Multi-Stack Strategy

```
algorithm remove(pool_id, resource_id, quantity):
  1. 验证总量: total >= quantity
  2. 按数量降序排列匹配堆（同数量时按槽位索引升序）
  3. 从最大堆开始扣除:
     for stack in sorted_stacks:
       taken = min(stack.quantity, remaining)
       stack.quantity -= taken
       remaining -= taken
       if stack.quantity == 0: remove stack
       if remaining == 0: break
  4. return SUCCESS
```

### transfer() Atomic Guarantee

```
algorithm transfer(from_pool, to_pool, resource_id, quantity):
  1. 验证: source_count >= quantity
  2. 验证: target_valid_for_kind (gating by item kind)
  3. 计算: stack_merge(to_pool, resource_id, quantity) → 确定目标容量需求
  4. 若目标容量充足:
     a. 在源池中: remove(from_pool, resource_id, quantity)
     b. 在目标池中: add(to_pool, resource_id, quantity)  # 跳过检查——已通过步骤 3
     c. return SUCCESS
  5. 否则: return ERR_TARGET_FULL (保持容错信息)
```

关键: 步骤 3 预先验证目标容量——若失败则不执行任何操作。步骤 4a 和 4b 在同一帧内完成——无 `await`。

### transfer_validation Formula

```
transfer_valid = (source_count >= Q) AND target_valid_for_kind AND target_can_take
```

其中:
- `target_valid_for_kind`: cargo items → target must be `loaded`; resources → target must NOT be `loaded`
- `target_can_take`: stack_merge 容量检查（槽位制或容积制）

---

## Out of Scope

- `discard()` 操作（Story 007）
- `consume_in_combat()` 封装（Story 007）
- `commit_deposit()` / `execute_purchase()` / `list_for_sale()` 领域专属操作（Story 007）
- 信号发射（Story 008）
- 重入防护 `ERR_BUSY`（Story 008）

---

## QA Test Cases

- **AC-8**: Transfer with split
  - Given: 仓库 basic × 50 (1 堆), 随身 2/5 槽, 无 basic 堆
  - When: `transfer(in_storage, on_person, basic_id, 20)`
  - Then: 仓库 basic=30, 随身 basic=20, SUCCESS
  - Edge cases: transfer 全部 50 → 仓库堆被移除, 随身 50

- **AC-14**: Atomic failure — source unchanged
  - Given: 仓库 basic × 30, 随身 5/5 满且无匹配堆
  - When: `transfer(in_storage, on_person, basic_id, 10)`
  - Then: `ERR_TARGET_FULL`, 仓库 basic 仍 30, 随身不变
  - Edge cases: 源不足 + 目标满 → 优先报告源不足（更根本的失败原因）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/resources/CoreOperationsTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (stack_merge), Story 002 (capacity checks), Story 003 (kind validation)
- Unlocks: Story 006 (state machine), Story 007 (specialized operations)
