# Story 001: Resource Identity & Stack Merge

> **Epic**: Resources, Goods & Capacity
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-001`

**ADR Governing Implementation**: ADR-0005: Resource Pool System
**ADR Decision Summary**: 资源身份仅由 Registry 稳定 ID 确定（`resource.*`）；`stack_rule` 决定 `stackable`（同 ID 合并，上限 max_stack）或 `unique`（每件单独一槽，max_stack=1）；`supply_class` 提供默认 max_stack（basic=99, repair=99, navigation=20, local-specialty=10, intel=1）；栈合并算法 fill fullest first（优先合并到已有最大堆，多堆数量相同时选最低槽位索引）

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Dictionary[StringName, Dictionary] 六池存储；O(1) 键查找；StringName interning 保证身份比较效率

**Control Manifest Rules (Foundation layer)**:
- Required: 资源身份仅基于稳定 ID；stack_merge fill fullest first 确定性算法
- Forbidden: 使用显示名/文件路径/节点引用作为资源身份；`bare_dictionary_payload`；`hardcoded_value` (max_stack 来自 Registry)
- Guardrail: stack_merge O(N) where N ≤ 5 (同 ID 堆数)

---

## Acceptance Criteria

### Resource Identity

- [x] **AC-1**: GIVEN 两个同 `resource_id` 的 stackable 资源，WHEN 添加到同一池，THEN 它们合并到同一堆，数量求和
- [x] **AC-2**: GIVEN 两个同 `resource_id` 的 unique 资源，WHEN 添加到同一池，THEN 它们各自占据独立槽位，不合并
- [x] **AC-3**: GIVEN 资源显示名被修改，WHEN 执行 `transfer()` 匹配，THEN 操作仅基于稳定 ID 匹配，不依赖显示名

### Stack Merge Algorithm

- [x] **AC-4**: GIVEN 目标池有 basic 堆 E=80（max_stack=99），WHEN `add(basic, 30)`，THEN merge_qty=19 合并到已有堆（达 99），overflow_qty=11 创建新堆
- [x] **AC-5**: GIVEN 目标池有 basic 堆 E=80 和 E=60（两个匹配堆），WHEN `add(basic, 30)`，THEN 优先合并到 E=80 的堆（fill fullest first），merge_qty=19，overflow_qty=11
- [x] **AC-6**: GIVEN 目标池有两个相同数量（E=60 each）的匹配堆，WHEN `add(resource, 30)`，THEN 合并到较低槽位索引的堆
- [x] **AC-7**: GIVEN 目标池无该 resource_id 的匹配堆，WHEN `add(resource, Q)`，THEN merge_qty=0，overflow_qty=Q，创建新堆（检查容量）

### Supply Class Defaults

- [x] **AC-8**: GIVEN `supply_class=intel` 的资源，WHEN 查询其 max_stack，THEN 返回 1（unique，不可堆叠）
- [x] **AC-9**: GIVEN `supply_class=navigation` 的资源，WHEN `add(navigation_id, 25)` 到有空槽的池，THEN 生成一个 20 堆 + 一个 5 堆（不出现单堆 25）
- [x] **AC-10**: GIVEN `supply_class=basic` 的资源，WHEN `add(basic_id, 150)` 到有空槽的池，THEN 生成一个 99 堆 + 一个 51 堆

### Edge Cases

- [x] **AC-11**: GIVEN `add(pool, id, 0)`，WHEN 调用零数量操作，THEN 返回 SUCCESS 且无状态变更
- [x] **AC-12**: GIVEN `add(pool, id, -5)`，WHEN 调用负数量操作，THEN 返回 `ERR_INVALID_QUANTITY`
- [x] **AC-13**: GIVEN 资源 ID 不在注册表中，WHEN `add(pool, unknown_id, 1)`，THEN 返回 `ERR_MISSING_REFERENCE`
- [x] **AC-14**: GIVEN 资源 ID 标记为 `deprecated`，WHEN `add(pool, deprecated_id, 1)`，THEN 返回 `ERR_DEPRECATED_ID`（已有库存可正常使用，但不可补充）

---

## Implementation Notes

### Resource Identity

```text
# 资源身份仅由稳定 ID 确定
# 运行时存储: { "resource_id": StringName, "quantity": int }
# 不存储显示名、文件路径或节点引用
```

- `resource_id` 格式: `"resource.basic_supply"`, `"cargo.iron_crate"` 等
- 身份匹配使用 `StringName` 直接比较（O(1)）

### Stack Merge Algorithm (from ADR-0005 Section 5)

```
algorithm stack_merge(pool_id, resource_id, quantity):
  1. 读取 stack_rule 和 max_stack（从 Registry）
  2. 在目标池中查找 resource_id 匹配的所有堆
  3. has_match = (匹配堆数量 > 0)
  4. 若有匹配:
     a. 选择已有数量最大的堆（fill fullest first）
     b. 若多堆数量相同，选最低槽位索引
     c. merge_qty = min(quantity, max_stack - E)
     d. overflow_qty = quantity - merge_qty
  5. 若无匹配:
     merge_qty = 0, overflow_qty = quantity
  6. 若 overflow_qty > 0:
     a. new_stacks = ceil(overflow_qty / max_stack)
     b. 检查容量（槽位制或容积制）
  7. 容量充足 → 执行合并+创建新堆 → SUCCESS
     否则 → ERR_TARGET_FULL
```

### Supply Class → max_stack Mapping

| supply_class | max_stack | stack_rule |
|-------------|-----------|------------|
| `basic` | 99 | stackable |
| `repair` | 99 | stackable |
| `navigation` | 20 | stackable |
| `local-specialty` | 10 | stackable |
| `intel` | 1 | unique |

### Determinism

合并优先级规则保证确定性行为：
1. fill fullest first（已有最大堆优先）
2. 数量相同时最低槽位索引优先
3. 无歧义——任何状态下给定相同输入产生相同输出

---

## Out of Scope

- 容量检查逻辑（Story 002）
- `transfer()` 的跨池转移验证（Story 005）
- `unpack_cargo()` 的拆包合并（Story 003）
- 信号发射（Story 008）

---

## QA Test Cases

- **AC-4**: Stack merge with partial overflow
  - Given: 仓库有 basic 堆 E=80 (max_stack=99)
  - When: `add(in_storage, "resource.basic_supply", 30)`
  - Then: 已有堆数量=99, 新堆数量=11, 总 basic=110
  - Edge cases: Q 恰好填满 (Q=19) → overflow_qty=0

- **AC-5**: Fill fullest first priority
  - Given: 仓库有 basic E=80 (槽位 0) 和 E=60 (槽位 1)
  - When: `add(in_storage, "resource.basic_supply", 30)`
  - Then: 槽位 0 堆=99, 槽位 1 堆=60, 新堆=11
  - Edge cases: E=80 和 E=80 相同数量 → 选槽位 0

- **AC-11**: Zero quantity no-op
  - Given: 任意池状态
  - When: `add(pool, id, 0)`, `remove(pool, id, 0)`, `transfer(a, b, id, 0)`
  - Then: 所有操作返回 SUCCESS, 池内容不变

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/resources/StackMergeTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-12

---

## Dependencies

- Depends on: content-registry Story 001 (Registry 提供 stack_rule/max_stack/supply_class)
- Unlocks: Story 002 (容量检查需要 max_stack), Story 005 (原子操作需要 stack_merge)

## Implementation Completion Notes

**Implemented**: 2026-05-12
**Criteria**: 14/14 passing
**Files**: `src/core/resources/ResourcesManager.cs`, `tests/unit/resources/StackMergeProgram.cs`, `tests/unit/resources/StackMergeTest.csproj`
**Verification**: `dotnet run --project tests/unit/resources/StackMergeTest.csproj` PASS; `dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj` PASS.
**Code Review**: Complete — approved with suggestions on 2026-05-12

## Completion Notes

**Completed**: 2026-05-12
**Criteria**: 14/14 passing
**Deviations**: None blocking. Advisory notes: `ResourcesManager.AddCore`, `RemoveCore`, and `Transfer` exceed the 40-line method guideline; AC-3's test expresses display-name independence indirectly; `ResourcesManager` depends on concrete `Registry` for now, matching current codebase patterns.
**Test Evidence**: Logic unit test `tests/unit/resources/StackMergeTest.csproj` exists and passes; `dotnet build CloudWeaverVoyage.sln --no-restore` passes after restore assets are present.
**Code Review**: Complete — `/code-review` verdict APPROVED WITH SUGGESTIONS.
**Gate Notes**: `production/review-mode.txt` is `full`, but Codex subagent gates were not spawned because the active Codex contract only allows subagents when the user explicitly asks for delegation/parallel agents. QA coverage and lead review were completed locally.
**Next Recommended**: `production/epics/resources-goods-capacity/story-002-dual-capacity-system.md` — Story 002: Dual Capacity System.
