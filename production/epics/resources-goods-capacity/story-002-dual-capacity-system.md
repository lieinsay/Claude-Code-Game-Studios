# Story 002: Dual Capacity System

> **Epic**: Resources, Goods & Capacity
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-003`

**ADR Governing Implementation**: ADR-0005: Resource Pool System
**ADR Decision Summary**: 双容量制——槽位制（随身物品栏/探索局内池）和容积制（飞艇仓库/货舱）。槽位制：每堆占 1 槽，total_slots 基础 5。容积制：mass_class → volume 映射（light=50, medium=120, heavy=200），total_volume 仓库=1000、货舱基础=0+模块500。容量检查由 `stack_merge` 统一调用 `_slot_available()` / `_volume_available()`。mass_class 表硬编码为 const Dictionary。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: const Dictionary 预编译；int 运算无浮点精度问题

**Control Manifest Rules (Foundation layer)**:
- Required: 容量检查在 stack_merge 中统一执行；槽位制 new_stacks = ceil(overflow_qty / max_stack)
- Forbidden: 绕过容量检查直接写入 `_pools`；`hardcoded_value`（容量值来自 const 或注入接口）
- Guardrail: 容量加成通过注入接口设置（`set_carry_slot_bonus()` 等）

---

## Acceptance Criteria

### Slot-Based Capacity (随身/局内)

- [x] **AC-1**: GIVEN 随身 5/5 槽已满且无匹配堆，WHEN `add(on_person, new_resource, 1)`，THEN 返回 `ERR_CARRY_SLOTS_FULL`
- [x] **AC-2**: GIVEN 随身 5/5 槽已满但有 basic 匹配堆 E=90（max_stack=99），WHEN `add(on_person, basic, 5)`，THEN merge 成功（不占新槽），basic 堆变为 95
- [x] **AC-3**: GIVEN 随身 4/5 槽，WHEN `add(on_person, basic, 200)` (max_stack=99)，THEN overflow_qty=200, new_stacks=ceil(200/99)=3, 4+3=7>5 → `ERR_CARRY_SLOTS_FULL`
- [x] **AC-4**: GIVEN 随身有匹配堆但已达 max_stack (E=99)，WHEN `add(on_person, basic, 10)`，THEN merge_qty=0, overflow_qty=10, 需 1 新槽 → 若无空槽则 `ERR_CARRY_STACK_FULL`

### Volume-Based Capacity (仓库/货舱)

- [x] **AC-5**: GIVEN 仓库已用 920/1000，WHEN `add(in_storage, medium_resource, 1)`(volume=120)，THEN 920+120=1040>1000 → `ERR_TARGET_FULL`
- [x] **AC-6**: GIVEN 仓库已用 920/1000，WHEN `add(in_storage, light_resource, 1)`(volume=50)，THEN 920+50=970≤1000 → SUCCESS
- [x] **AC-7**: GIVEN 货舱基础容积为 0（无模块），WHEN `add(loaded, any_cargo, 1)`，THEN 返回 `ERR_CAPACITY_ZERO`
- [x] **AC-8**: GIVEN 货舱模块安装后容积=500，已用 380，WHEN `add(loaded, heavy_cargo, 1)`(volume=200)，THEN 380+200=580>500 → `ERR_TARGET_FULL`

### mass_class Table

- [x] **AC-9**: GIVEN light 资源，WHEN 查询 mass_class，THEN volume=50, weight=1
- [x] **AC-10**: GIVEN medium 资源，WHEN 查询 mass_class，THEN volume=120, weight=3
- [x] **AC-11**: GIVEN heavy 资源，WHEN 查询 mass_class，THEN volume=200, weight=6

### Volume Calculation with Stack Overflow

- [x] **AC-12**: GIVEN 仓库已用 800/1000，WHEN `add(in_storage, heavy_resource, 1)` 且该资源 overflow_qty=200、max_stack=99、volume=200，THEN new_stacks=ceil(200/99)=3, 需 3×200=600 容积, 800+600=1400>1000 → `ERR_TARGET_FULL`

---

## Implementation Notes

### Capacity Constants (from ADR-0005 Section 4)

```text
const CARRY_BASE_SLOTS: int = 5
const CARRIED_BASE_SLOTS: int = 5
const STORAGE_BASE_VOLUME: int = 1000
const CARGO_BAY_BASE_VOLUME: int = 0
const CARGO_MODULE_VOLUME_BONUS: int = 500

const MASS_CLASS_TABLE: Dictionary = {
    &"light":  { "volume": 50,  "weight": 1 },
    &"medium": { "volume": 120, "weight": 3 },
    &"heavy":  { "volume": 200, "weight": 6 },
}
```

### Capacity Check Functions

```text
func _slot_available(pool_id: StringName, new_stacks: int) -> bool:
    var used = _count_used_slots(pool_id)
    var total = _get_pool_total_slots(pool_id)
    return used + new_stacks <= total

func _volume_available(pool_id: StringName, required_volume: int) -> bool:
    var used = _count_used_volume(pool_id)
    var total = _get_pool_total_volume(pool_id)
    return used + required_volume <= total
```

### Pool Capacity Rules

| Pool | Capacity Type | Base Value | Bonus Source |
|------|-------------|------------|--------------|
| `on_person` | slots | 5 | `carry_slot_bonus` (背包/伙伴) |
| `carried` | slots | 5 | `carry_slot_bonus` (共享) |
| `in_storage` | volume | 1000 | `storage_volume_bonus` (模块) |
| `loaded` | volume | 0 | `cargo_module_volume_bonus` (模块=500) |

### Volume for Multi-Stack Overflow

当 `overflow_qty > max_stack` 时，需要多个新堆：
```
new_stacks = ceil(overflow_qty / max_stack)
required_volume = new_stacks × item_volume
```

每堆独立的 volume 占用——与 `unpack_validation` 中的多堆容积计算一致。

### Capacity Bonus Injection

```text
func set_carry_slot_bonus(bonus: int) -> void:
func set_storage_volume_bonus(bonus: int) -> void:
func set_cargo_module_volume_bonus(bonus: int) -> void:
```

加成变更后立即反映在下次容量查询中——无需信号通知。

---

## Out of Scope

- `stack_merge` 算法本身（Story 001）
- 重量计算（Story 004）
- 模块战斗摧毁时的货舱容积归零（Story 009）
- 容量条 UI 渲染（UI/HUD Epic）

---

## QA Test Cases

- **AC-3**: Multi-stack overflow slot check
  - Given: 随身 4/5 槽已用, 无 basic 堆 (max_stack=99)
  - When: `add(on_person, basic_id, 200)`
  - Then: overflow_qty=200, new_stacks=ceil(200/99)=3, 4+3=7>5 → `ERR_CARRY_SLOTS_FULL`
  - Edge cases: overflow_qty=198 (恰好 2 堆) → new_stacks=2, 4+2=6>5 → 仍满

- **AC-12**: Volume overflow with multi-stack
  - Given: 仓库 800/1000, 无 heavy 堆 (max_stack=99, volume=200)
  - When: `add(in_storage, heavy_id, 200)`
  - Then: new_stacks=3, required_volume=600, 800+600>1000 → `ERR_TARGET_FULL`
  - Edge cases: 若有匹配堆 E=98 → merge_qty=1, overflow_qty=199, new_stacks=ceil(199/99)=3

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/resources/CapacitySystemTest.csproj` — must exist and pass
**Status**: [x] Created and passing — 2026-05-12

---

## Dependencies

- Depends on: Story 001 (stack_merge 算法, max_stack), content-registry (mass_class 字段)
- Unlocks: Story 003 (cargo bay capacity), Story 005 (add/transfer 容量校验)

## Implementation Completion Notes

**Implemented**: 2026-05-12
**Criteria**: 12/12 acceptance criteria passing; 2/2 regression checks passing; 14/14 validation checks passing
**Files**: `src/core/resources/ResourcesManager.cs`, `tests/unit/resources/CapacitySystemProgram.cs`, `tests/unit/resources/CapacitySystemTest.csproj`, `CloudWeaverVoyage.sln`, `.github/workflows/tests.yml`
**Verification**: `dotnet run --project tests/unit/resources/CapacitySystemTest.csproj` PASS; `dotnet run --project tests/unit/resources/StackMergeTest.csproj` PASS; `dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj` PASS; `dotnet build CloudWeaverVoyage.sln --no-restore` PASS.
**Notes**: AC-8 test uses a reachable `370/500 + heavy(200)` overflow setup because the declared mass table (`50/120/200`) cannot produce exactly 380 used volume with whole stacks. The overflow behavior and `ERR_TARGET_FULL` branch are the same.
**Metadata**: Requirement corrected to `TR-resources-003` because the current TR registry maps `TR-resources-002` to `commit_deposit`; `TR-resources-003` is the dual-capacity requirement.
**Code Review**: Complete — APPROVED WITH SUGGESTIONS on 2026-05-13; blocking Pool 1/Pool 5 alias issue fixed before closure.
**Ready for**: Story 003 — Cargo Model & Unpack.

## Completion Notes

**Completed**: 2026-05-13
**Criteria**: 12/12 acceptance criteria passing; 2/2 regression checks passing; 14/14 validation checks passing.
**Deviations**: Advisory only — AC-8 uses a reachable `370/500 + heavy(200)` setup because the declared mass table cannot compose exactly 380 used volume from whole stacks; overflow behavior and `ERR_TARGET_FULL` branch are equivalent. Full QA/lead subagent gates were not spawned because the active Codex contract only allows subagents when explicitly requested.
**Test Evidence**: Logic unit test `tests/unit/resources/CapacitySystemTest.csproj` exists and passes; `dotnet run --project tests/unit/resources/StackMergeTest.csproj`, `dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj`, and `dotnet build CloudWeaverVoyage.sln --no-restore` also pass.
**Code Review**: Complete — `/code-review` verdict APPROVED WITH SUGGESTIONS.
**Next Recommended**: `production/epics/resources-goods-capacity/story-003-cargo-model-unpack.md` — Story 003: Cargo Model & Unpack.
