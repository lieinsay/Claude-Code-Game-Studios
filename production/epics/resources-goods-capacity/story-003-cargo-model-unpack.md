# Story 003: Cargo Model & Unpack

> **Epic**: Resources, Goods & Capacity
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-003`

**ADR Governing Implementation**: ADR-0005: Resource Pool System
**ADR Decision Summary**: 货物（`kind: cargo`）是对资源的封装包装——`linked_resource_id` 指向资源，货物物品自身的 `resource_quantity` (Q) 声明拆包后获得的资源数量。货物只能存在于货舱（cargo_bay），不可进入随身物品栏或飞艇仓库。`unpack_cargo()` 销毁货物物品，将其 linked_resource 以 Q 数量加入飞艇仓库（in_storage），原子执行。MV 不提供 `pack_cargo`（打包）操作——货物仅由集市系统创建。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: Dictionary 操作原子（单帧内同步）；Array 槽位索引稳定

**Control Manifest Rules (Foundation layer)**:
- Required: 货物只能存在于 `loaded` 池；`unpack_cargo()` 原子（销毁+入仓同一帧）
- Forbidden: 货物进入 `on_person`/`in_storage`/`carried`；裸资源进入 `loaded`（货舱仅接受货物物品）
- Guardrail: unpack 前校验 `unpack_validation`——仓库容积不足时全操作失败

---

## Acceptance Criteria

### Cargo Identity & Constraints

- [ ] **AC-1**: GIVEN 货物物品在货舱中，WHEN `transfer(loaded, on_person, cargo_id, 1)`，THEN 返回 `ERR_KIND_MISMATCH`（货物不可进入随身）
- [ ] **AC-2**: GIVEN 货物物品在货舱中，WHEN `transfer(loaded, in_storage, cargo_id, 1)`，THEN 返回 `ERR_KIND_MISMATCH`（货物不可进入仓库）
- [ ] **AC-3**: GIVEN 裸资源，WHEN `add(loaded, resource_id, 1)`，THEN 返回 `ERR_KIND_MISMATCH`（裸资源不可进入货舱）
- [ ] **AC-4**: GIVEN 货物物品的 `linked_resource_id` 指向 `"resource.basic_supply"`，WHEN 查询货物属性，THEN `resource_quantity` (Q) 为正整数

### Unpack (拆包)

- [ ] **AC-5**: GIVEN 货舱有 1 个货物（linked=basic, Q=30），仓库 basic E=90（max_stack=99），WHEN `unpack_cargo(cargo_slot)`，THEN merge_qty=9 合并到已有堆→99, overflow_qty=21 创建新堆, 货物物品销毁, 仓库 total basic=120
- [ ] **AC-6**: GIVEN 货舱有 1 个货物（linked=basic, Q=30），仓库无 basic 堆，WHEN `unpack_cargo(cargo_slot)`，THEN 仓库新增 basic × 30（1 堆），占用 50 容积（light），货物物品销毁
- [ ] **AC-7**: GIVEN 货舱有货物（linked=new_resource, Q=10），仓库已用 1000/1000 且无匹配堆，WHEN `unpack_cargo(cargo_slot)`，THEN 返回 `ERR_STORAGE_FULL`, 货物保留在货舱, 仓库不变
- [ ] **AC-8**: GIVEN 货舱有货物（linked=heavy_resource, Q=200, max_stack=99），仓库已用 600/1000，无匹配堆，WHEN `unpack_cargo(cargo_slot)`，THEN new_stacks=ceil(200/99)=3, required_volume=3×200=600, 600+600=1200>1000 → `ERR_STORAGE_FULL`

### Unpack Validation with Match

- [ ] **AC-9**: GIVEN 货舱有货物（linked=basic, Q=5），仓库 basic E=90（max_stack=99），仓库已用 980/1000，WHEN `unpack_cargo(cargo_slot)`，THEN merge_qty=5 全合并，overflow_qty=0，不占新容积，拆包成功
- [ ] **AC-10**: GIVEN 拆包后货物物品从货舱消失，WHEN 查询货舱状态，THEN 该槽位清空，货舱已用容积减少对应 amount

### Cargo Unpack Resource volume in Storage

- [ ] **AC-11**: GIVEN 拆包 light 货物（Q=10），仓库无匹配堆，WHEN 拆包完成，THEN 仓库已用容积增加 50（1 堆 light volume）
- [ ] **AC-12**: GIVEN 拆包 medium 货物（Q=10），仓库无匹配堆，WHEN 拆包完成，THEN 仓库已用容积增加 120（1 堆 medium volume）

---

## Implementation Notes

### Cargo Item Structure

```text
# 货物在货舱中的存储格式：
{
    "resource_id": "cargo.iron_crate",      # 货物自身的 stable ID
    "quantity": 1,                           # 货物物品不可堆叠（MVP）
    "kind": "cargo",
    "linked_resource_id": "resource.iron_ingot",  # 拆包后获得的资源 ID
    "resource_quantity": 10,                 # 拆包后获得的资源数量 (Q)
    "mass_class": "medium",                  # 货物的 mass_class（决定容积和重量）
}
```

### Kind Validation

`transfer_validation` 中的 `target_valid_for_kind`:
- item `kind = cargo`: target must be `loaded`（货舱）
- item `kind = resource`: target must NOT be `loaded`（裸资源不可进入货舱）

### Unpack Algorithm

```
algorithm unpack_cargo(cargo_slot_index):
  1. 验证 cargo_slot_index 有效且该槽位有货物
  2. 读取货物的 linked_resource_id, resource_quantity (Q), mass_class
  3. 读取 linked_resource 的 max_stack (从 Registry)
  4. 计算 stack_merge(in_storage, linked_resource_id, Q):
     - merge_qty, overflow_qty
     - 若 overflow_qty > 0: new_stacks, required_volume
  5. 若 overflow_qty = 0 或 volume_available:
     a. 合并已有堆（若 merge_qty > 0）
     b. 创建新堆（若 overflow_qty > 0）
     c. 从货舱移除货物物品（destroy cargo item）
     d. emit cargo_unpacked(cargo_id, linked_resource_id, Q)
     e. emit pool_changed(loaded), pool_changed(in_storage)
     f. return SUCCESS
  6. 否则: return ERR_STORAGE_FULL
```

### Unpack Validation Formula

```
unpack_valid = (has_match AND overflow_qty = 0) OR volume_availability(storage, overflow_volume)
```

若 `overflow_qty = 0`（所有 items 合并到已有堆），不需要额外容积——拆包总是有效的。

### Cargo Creation (Out of Scope for MVP)

货物物品仅由集市系统（Settlement/Market）创建。本系统不提供 `pack_cargo` 操作。集市系统调用 `add(loaded, cargo_id, 1)` 将货物放入货舱。

---

## Out of Scope

- `pack_cargo`（打包裸资源为货物）—— MVP 不提供
- 货物图标/UI 的 `handling_class` 视觉区分（Story 009 / UI Epic）
- 集市系统创建货物的逻辑（Settlement Epic）
- `cargo_unpacked` 信号发射（Story 008）

---

## QA Test Cases

- **AC-7**: Unpack fails when storage full
  - Given: 仓库 1000/1000, 无匹配堆, 货舱有货物 (linked=new_id, Q=10, mass_class=light)
  - When: `unpack_cargo(cargo_slot)`
  - Then: 返回 `ERR_STORAGE_FULL`, 货物保留, 仓库不变
  - Edge cases: 仓库 1000/1000 但有匹配堆 E=90 (max_stack=99) → merge_qty=9, overflow_qty=1 → need 1×50=50 volume → 仍满 → `ERR_STORAGE_FULL`

- **AC-1**: Cargo cannot enter on_person
  - Given: 货舱有货物, 随身有空槽
  - When: `transfer(loaded, on_person, cargo_id, 1)`
  - Then: `ERR_KIND_MISMATCH`
  - Edge cases: 尝试 `add(on_person, cargo_id, 1)` → `ERR_KIND_MISMATCH`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/resources/CargoUnpackTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (stack_merge), Story 002 (volume_availability), content-registry (linked_resource_id, mass_class)
- Unlocks: Story 005 (transfer 需要 kind validation), Story 007 (unpack 入口)
