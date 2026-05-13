# Story 007: Specialized Operations

> **Epic**: Resources, Goods & Capacity
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-002`
**GDD Acceptance Criteria**: `AC-RES-011.2` (`can_deposit()` 无副作用) plus the GDD operation contract rows for `discard()`, `consume_in_combat()`, `commit_deposit()`, `execute_purchase()`, `list_for_sale()`, and `add_loot()`

**ADR Governing Implementation**: ADR-0005 (核心操作 API), ADR-0004 (Interactable 子类 Use 入口), ADR-0002 (Signal 通信)
**ADR Decision Summary**: 在 4 个核心操作（add/remove/transfer/consume）之上，实现 5 个领域专属操作：`discard()`（玩家驱动丢弃，需二次确认）；`consume_in_combat()`（consume(Pool 5) 薄封装）；`commit_deposit()`（不可逆修复提交）；`execute_purchase()` / `list_for_sale()`（集市交易原子转移）；`add_loot()`（探索拾取入口）。这些操作封装核心操作并添加领域特定验证（确认门控、supply_class 过滤、repair_node_id 验证）。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: 所有封装为同步薄层——无 `await`；确认门控由调用方 UI 处理（本 Story 只提供操作入口）

**Control Manifest Rules (Foundation layer)**:
- Required: `discard()` 有效目标池 on_person/in_storage/loaded/carried；`commit_deposit()` 不可逆；`consume_in_combat()` 仅操作 Pool 5
- Forbidden: `discard()` 对 `deposited` 池操作；`commit_deposit()` 被调用两次（幂等守卫）；`consume_in_combat()` 操作 Pool 5 以外的池
- Guardrail: 确认对话框由调用方 UI 负责——本 Story 不实现 UI

---

## Acceptance Criteria

### discard() Operation

- [x] **AC-1**: GIVEN 随身 basic × 5，WHEN `discard(on_person, basic_id, 3)`，THEN 随身 basic 变为 2, 丢弃的 3 进入 destroyed 终态, 返回 SUCCESS
- [x] **AC-2**: GIVEN `discard()` 被调用，WHEN 调用方在调用前已显示确认对话框，THEN 本操作直接执行（不再次弹出确认——确认由调用方处理）
- [x] **AC-3**: GIVEN `discard(deposited, id, 1)`，WHEN 尝试丢弃 deposited 池中的资源，THEN 返回错误（deposited 终态不可丢弃）

### consume_in_combat() Operation

- [x] **AC-4**: GIVEN `carried` (Pool 5) 有 repair_kit × 5，WHEN `consume_in_combat(repair_kit_id, 2)`，THEN carried repair_kit 变为 3, 消耗的 2 进入 destroyed 终态, 返回 SUCCESS
- [x] **AC-5**: GIVEN `carried` 有 repair_kit × 1，WHEN `consume_in_combat(repair_kit_id, 5)`，THEN 返回 `ERR_SOURCE_INSUFFICIENT`, carried 不变
- [x] **AC-6**: GIVEN 调用 `consume_in_combat(id, qty)`，WHEN 检查实现，THEN 内部调用 `consume(&"carried", id, qty)`——无其他逻辑

### commit_deposit() Operation

- [x] **AC-7**: GIVEN `commit_deposit(repair_node_id, {basic: 5, repair: 3})`，WHEN 各池有足够资源，THEN 所有指定资源移除, 进入 deposited 终态, 返回 SUCCESS
- [x] **AC-8**: GIVEN 任一资源不足，WHEN `commit_deposit(repair_node_id, {basic: 100})` 但仓库 basic × 10，THEN 返回 `ERR_SOURCE_INSUFFICIENT`, 所有资源保留在原池（原子失败）
- [x] **AC-9**: GIVEN `can_deposit(repair_node_id, costs)` 被调用，WHEN 检查资源可用性，THEN 返回 true/false, 不产生副作用

### execute_purchase() & list_for_sale() Operations

- [x] **AC-10**: GIVEN `execute_purchase(good_id, 3)` 被执行，WHEN 购买完成，THEN 资源从 `listed` → `in_storage`, 返回 SUCCESS
- [x] **AC-11**: GIVEN `list_for_sale(resource_id, 5, 100)` 被调用，WHEN 上架完成，THEN 资源从 `in_storage` → `listed`, 返回 SUCCESS

### add_loot() Operation

- [x] **AC-12**: GIVEN `add_loot(resource_id, Q)`，WHEN carried (Pool 5) 有空槽且容量充足，THEN 资源添加到 carried, 返回 SUCCESS
- [x] **AC-13**: GIVEN carried 5/5 槽满且无匹配堆，WHEN `add_loot(new_id, 1)`，THEN 返回 `ERR_CARRY_SLOTS_FULL`, carried 不变

---

## Implementation Notes

### discard()

```text
func discard(pool_id: StringName, resource_id: StringName, quantity: int) -> ResourceResult:
    # 有效目标池守卫: on_person, in_storage, loaded, carried
    # deposited 终态不可丢弃；listed 由集市系统拥有
    if pool_id not in [&"on_person", &"in_storage", &"loaded", &"carried"]:
        return ERR_CANNOT_DISCARD_FROM_POOL
    # 委托给 remove() —— 资源进入 destroyed 终态
    return remove(pool_id, resource_id, quantity)
```

### consume_in_combat()

```text
func consume_in_combat(resource_id: StringName, quantity: int) -> ResourceResult:
    # consume(Pool 5, resource_id, quantity) 的薄封装
    # 仅操作 carried 池
    return consume(&"carried", resource_id, quantity)
```

### commit_deposit()

```text
func commit_deposit(repair_node_id: StringName, resource_costs: Dictionary) -> ResourceResult:
    # resource_costs: { resource_id: quantity, ... }
    # 1. 验证所有 resource_id 在 Registry 中
    # 2. 验证所有 source pool 中数量充足
    # 3. 原子执行所有 remove（若任一步失败则整体失败）
    # 4. 资源进入 deposited 终态
    # 5. emit deposit_committed(repair_node_id)
```

### add_loot()

```text
func add_loot(resource_id: StringName, quantity: int) -> ResourceResult:
    # 探索拾取入口——委托给 add(carried, resource_id, quantity)
    # 内部使用 add_loot_valid = slot_capacity_check OR (has_match AND E+Q<=max_stack)
    return add(&"carried", resource_id, quantity)
```

### Domain Ownership

| Operation | Owner | Notes |
|-----------|-------|-------|
| `discard()` | 本系统 | 玩家驱动的丢弃 |
| `consume_in_combat()` | 战斗系统调用 | Pool 5 专属 |
| `commit_deposit()` | 修复系统调用 | 不可逆终态 |
| `execute_purchase()` | 集市系统调用 | listed → in_storage |
| `list_for_sale()` | 集市系统调用 | in_storage → listed |
| `add_loot()` | 探索系统调用 | carried 添加 |

---

## Out of Scope

- discard 确认对话框 UI（UI/HUD Epic）
- commit_deposit 确认对话框 UI（UI/HUD Epic）
- 探索系统的 loot 生成和 `add_loot()` 调用时机（Exploration Epic）
- 战斗系统的 `consume_in_combat()` 调用时机（Combat Epic）
- 集市系统的价格/库存逻辑（Settlement Epic）
- 信号发射（Story 008）

---

## QA Test Cases

- **AC-7**: Atomic multi-resource deposit
  - Given: 仓库 basic×5, repair×3; repair_node costs={basic:5, repair:3}
  - When: `commit_deposit(repair_node, costs)`
  - Then: SUCCESS, 仓库 basic=0, repair=0, deposited 包含两种资源
  - Edge cases: 任一不足 → 全失败, 仓库不变

- **AC-9**: can_deposit is side-effect-free
  - Given: 仓库 basic×5
  - When: 连续 3 次调用 `can_deposit(node, {basic: 3})`
  - Then: 每次返回 true, 仓库 basic 仍为 5（无消耗）
  - Edge cases: costs 为空 → true

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/resources/SpecializedOpsTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-13 (13/13 checks)

---

## Completion Notes

**Completed**: 2026-05-13
**Criteria**: 13/13 passing
**Deviations**: None. Readiness metadata was corrected from nonexistent `TR-resources-007` to active `TR-resources-002`; non-registered specialized operation rows remain anchored to the GDD operation contract and ADR-0005. UI confirmation, signal/reentry behavior, combat/exploration call timing, and market pricing/inventory remain out of scope.
**Test Evidence**: Integration — `tests/integration/resources/SpecializedOpsTest.csproj` passes 13/13 checks.
**Code Review**: Complete — APPROVED. Local review found no blocking ADR, architecture, standards, or testability issues.

---

## Dependencies

- Depends on: Story 005 (add/remove/consume core ops), Story 006 (state machine/pool boundaries)
- Unlocks: Exploration Epic (add_loot), Combat Epic (consume_in_combat), WorldRepair Epic (commit_deposit), Settlement Epic (execute_purchase/list_for_sale)
