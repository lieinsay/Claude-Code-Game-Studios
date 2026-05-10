# Story 004: Weight & Mass Tracking

> **Epic**: Resources, Goods & Capacity
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/resources-goods-capacity.md`
**Requirement**: `TR-resources-004`

**ADR Governing Implementation**: ADR-0005: Resource Pool System
**ADR Decision Summary**: 本系统为所有已装载货物维护重量总值（mass_class → weight_value 映射：light=1, medium=3, heavy=6）。`get_total_loaded_mass()` 仅计算货舱（loaded）中货物的重量值——随身物品栏和飞艇仓库中的物品不计入。超重判定和载重适航由飞艇模块系统拥有——本系统不自行阻止装载（非阻塞式超重）。`mass_changed(new_mass)` 信号在货舱内容变更后触发。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: int 累加无精度问题；信号同步 emit

**Control Manifest Rules (Foundation layer)**:
- Required: `get_total_loaded_mass()` 仅计算货舱内容；重量从当前 mass_class 映射派生（不持久化）
- Forbidden: 自行阻止超重装载（门控由模块系统拥有）；在非货舱池中计入重量
- Guardrail: mass_changed signal 在货舱内容变更后 emit

---

## Acceptance Criteria

### Weight Calculation

- [ ] **AC-1**: GIVEN 货舱中有 2 light（2×1）+ 1 medium（3）+ 1 heavy（6），同时仓库和随身也有物品，WHEN `get_total_loaded_mass()`，THEN 返回 11（仅货舱）
- [ ] **AC-2**: GIVEN 货舱为空，WHEN `get_total_loaded_mass()`，THEN 返回 0
- [ ] **AC-3**: GIVEN 货舱中有 10 light（10×1=10），WHEN `get_total_loaded_mass()`，THEN 返回 10

### Weight Changes on Cargo Operations

- [ ] **AC-4**: GIVEN 货舱中有 1 heavy（weight=6），WHEN 从货舱移除该货物，THEN `get_total_loaded_mass()` 减少 6
- [ ] **AC-5**: GIVEN 货舱为空，WHEN `add(loaded, heavy_cargo, 1)`，THEN `get_total_loaded_mass()` 从 0 变为 6
- [ ] **AC-6**: GIVEN 货舱中有 2 medium（weight=3 each=6），WHEN `unpack_cargo()` 拆包其中 1 个 medium 货物，THEN `get_total_loaded_mass()` 从 6 变为 3

### Non-Blocking Overload

- [ ] **AC-7**: GIVEN 飞船载重上限为 25，货舱已有总重量 20，WHEN `add(loaded, heavy_cargo, 1)`（weight=6, total=26>25），THEN 装载成功完成——`get_total_loaded_mass()` 返回 26——不返回错误（本系统不阻止超重）
- [ ] **AC-8**: GIVEN 超重状态（total_loaded_mass > 载重上限），WHEN 查询 `get_total_loaded_mass()`，THEN 正确返回超重值——模块系统可通过此值自行决定适航门控

### Mass from Current Mapping

- [ ] **AC-9**: GIVEN 货舱中有货物，WHEN 系统启动，THEN `get_total_loaded_mass()` 从当前货舱内容使用当前 mass_class 映射重新计算——不使用存档中持久化的旧质量值

---

## Implementation Notes

### Mass Calculation

```text
func get_total_loaded_mass() -> int:
    var total: int = 0
    var stacks: Array = _pools[&"loaded"]["stacks"]
    for stack in stacks:
        var mass_class: StringName = _get_resource_mass_class(stack["resource_id"])
        if mass_class in MASS_CLASS_TABLE:
            total += MASS_CLASS_TABLE[mass_class]["weight"]
    return total
```

### Weight Values

| mass_class | weight |
|------------|--------|
| `light` | 1 |
| `medium` | 3 |
| `heavy` | 6 |

### Key Design Rules

1. **仅货舱计重**: 随身物品栏 (`on_person`) 和飞艇仓库 (`in_storage`) 中的物品不计入飞行质量
2. **非阻塞超重**: 本系统不阻止装载超过载重上限的货物——超重判定和适航门控由 `飞艇模块与船体状态` 系统拥有
3. **动态计算**: 质量值在每次查询时从当前货舱内容重新计算——不缓存、不持久化
4. **mass_class 变更兼容**: 若版本间 mass_class 变更（EC-09），加载后 `get_total_loaded_mass()` 使用新 mapping 重新计算

### Mass Changed Notification

货舱内容变更后 emit `mass_changed(new_mass: int)` signal（具体 signal 实现见 Story 008）。模块系统监听此信号以重新评估适航状态。

---

## Out of Scope

- `mass_changed` 信号发射（Story 008）
- 模块系统的载重上限和适航放行判定
- 货舱双条 UI（重量条）（UI/HUD Epic）
- 版本迁移时 mass_class 变更处理（Story 009）

---

## QA Test Cases

- **AC-1**: Only cargo bay contributes to mass
  - Given: 货舱: 2 light + 1 medium + 1 heavy; 仓库: 5 heavy (weight=30); 随身: 3 medium (weight=9)
  - When: `get_total_loaded_mass()`
  - Then: 返回 11 (2+3+6), 不含仓库和随身重量
  - Edge cases: 货舱为空 → 返回 0

- **AC-7**: Overload is not blocked by ResourcesManager
  - Given: 货舱已有 5 heavy (weight=30), 载重上限 25
  - When: `add(loaded, heavy_cargo_6, 1)` (weight=6)
  - Then: 返回 SUCCESS; `get_total_loaded_mass()` → 36
  - Edge cases: 卸货后质量正确减少

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/resources/WeightMassTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002 (mass_class table), Story 003 (cargo model)
- Unlocks: Story 008 (mass_changed signal), modules-hull-state Epic (载重适航)
