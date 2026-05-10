# Story 008: Scout Module Acquisition & Combat Damage Interfaces

> **Epic**: Modules & Hull State
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — scout acquisition + apply_hull_damage/apply_module_damage), ADR-0018 (Combat / Threat Resolution — 中探索威胁结算调用)
**ADR Decision Summary**: 侦察模块通过首次探索任务完成后 NPC 交付获得——非开局拥有。货仓模块开局预装在槽 B。apply_hull_damage(amount) 由战斗系统 #12 在威胁结算期间（硬扛后）调用以应用中探索船体损伤。apply_module_damage(slot_id, damage_type) 由战斗系统 #12 调用以标记模块为 damaged——damage_type 为传递字符串（MVP 中为 "guard_impact"），#8 仅存储此值。若 slot 已为 damaged 或 empty，静默无操作返回（防御性安全网）。两个接口均需适航状态重检。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: apply_hull_damage(amount) — amount > 0 时应用损伤，amount ≤ 0 时静默无操作；apply_module_damage(slot_id, damage_type) — 有效 installed 槽位→标记 damaged，已 damaged/empty→无操作
- Forbidden: 中探索损伤导致模块 destroyed（MVP 中模块只有 damaged，destroyed 由航行系统返航时写入）；apply_hull_damage 越过 Story 003 的 band/scars 逻辑直接修改 integrity
- Guardrail: 每次损伤后自动调用 _check_departure_readiness()——中探索的船体/模块损伤可能立即影响适航状态（若玩家在探索中有出航检查点）

---

## Acceptance Criteria

### Starting State Verification

- [ ] **AC-1**: GIVEN 新游戏，WHEN 查询模块可用性，THEN 侦察模块不可用——玩家尚未获得。仅货仓模块（槽 B installed）可用
- [ ] **AC-2**: GIVEN 新游戏，WHEN 玩家在 Hub 工程舱查看槽 A，THEN 显示为空槽位——可交互但无模块可安装（侦察模块尚未获得）
- [ ] **AC-3**: GIVEN 货仓模块预装，WHEN 玩家查看槽 B，THEN 显示货仓模块 installed——η=1.0，V_effective=500，M_max=12

### Scout Module Acquisition

- [ ] **AC-4**: GIVEN 玩家完成首次探索并成功返航，WHEN NPC 交付侦察模块物品，THEN 侦察模块在库存中可见——玩家可在 Hub 工程舱将其安装至任意空槽位
- [ ] **AC-5**: GIVEN 侦察模块已获得，WHEN 安装在空槽位 A，THEN slot_a visible_state=installed, module_type=SCOUT, M_max 增加 8，航线风险可见度提升
- [ ] **AC-6**: GIVEN 侦察模块已获得但两个槽位均已占用，WHEN 玩家尝试安装，THEN 同 Story 001 AC-7——ERR_SLOT_OCCUPIED。玩家需先卸下一模块或使用 swap_module

### apply_hull_damage Interface

- [ ] **AC-7**: GIVEN integrity=100 (intact) + amount=30，WHEN Combat #12 调用 apply_hull_damage(30)，THEN integrity→70 (damaged band)，hull_scars += 1（基础事件），波段→damaged（航速×0.9, 燃料×1.15）
- [ ] **AC-8**: GIVEN integrity=30 (damaged) + amount=35，WHEN apply_hull_damage(35)，THEN integrity→0 (destroyed)，hull_scars += 1（基础事件）+ 进入 critical +1 + 进入 destroyed +1 = +3，can_depart → false
- [ ] **AC-9**: GIVEN amount=0 或 amount=-5，WHEN apply_hull_damage(amount)，THEN 静默无操作返回——integrity 不变，无 signal emit

### apply_module_damage Interface

- [ ] **AC-10**: GIVEN slot_a = scout installed，WHEN apply_module_damage("slot_a", "guard_impact")，THEN actual_state → DAMAGED, visible_state → DAMAGED, η→0.6, damage_type 存储为 "guard_impact"
- [ ] **AC-11**: GIVEN slot_a 已为 damaged，WHEN apply_module_damage("slot_a", "guard_impact") 再次调用，THEN 静默无操作——不二次损坏。伤痕计数不增加（模块伤痕不由本接口管理）
- [ ] **AC-12**: GIVEN slot_a = empty，WHEN apply_module_damage("slot_a", "guard_impact")，THEN 静默无操作——空槽位无模块可损坏

### Post-Exploration Damage to Departure Readiness

- [ ] **AC-13**: GIVEN 中探索中，M_max=20（侦察+货仓）, M_loaded=15, can_depart=true，WHEN apply_hull_damage 使 integrity→0（destroyed），THEN can_depart → {false, ["hull_destroyed"]}
- [ ] **AC-14**: GIVEN 中探索中，M_max=12（仅货仓）, M_loaded=15（已超载），WHEN apply_module_damage 使货仓→damaged（M_max→6），THEN can_depart → {false, ["overloaded"]}

### damage_type Storage

- [ ] **AC-15**: GIVEN apply_module_damage("slot_a", "guard_impact")，WHEN 查询 slot_a 的 damage_type，THEN 返回 "guard_impact"——#8 仅存储此值，其含义由反馈系统 #17 消费

---

## Implementation Notes

### Starting State (already covered in Story 007 — restated here for context)

```text
# 新游戏起始状态在 Story 007 _apply_starting_state() 中完整定义
# slot_a: empty
# slot_b: cargo installed
# integrity: 100, hull_scars: 0
```

### Scout Module Acquisition Integration

```text
# 侦察模块的"获得"由探索系统 #11 或任务系统管理
# ModuleHullManager 提供查询接口——检查侦察模块是否已解锁

var _scout_module_available: bool = false

func is_scout_module_available() -> bool:
    return _scout_module_available


func unlock_scout_module() -> void:
    if _scout_module_available:
        return
    _scout_module_available = true
    # 发射信号通知 Hub/UI——"侦察模块已解锁，可安装在空槽位"
    # 具体信号由 UI 系统 #16 定义


func can_install_module_type(module_type: int) -> bool:
    match module_type:
        ModuleType.SCOUT:
            return _scout_module_available
        ModuleType.CARGO:
            return true  # 货仓模块始终可安装（玩家可能卸下后重新安装）
        _:
            return false
```

### install_module Update — Scout Availability Gate

```text
func install_module(slot_id: StringName, module_type: int) -> int:
    # ... existing validation ...

    # 侦察模块可用性检查
    if not can_install_module_type(module_type):
        return ERR_MODULE_NOT_AVAILABLE

    # ... continue with install ...
```

### apply_hull_damage Interface

```text
func apply_hull_damage(amount: int) -> void:
    if amount <= 0:
        return  # 静默无操作

    # 复用 Story 003 的损伤逻辑
    var old_integrity: int = _hull_integrity
    var old_band: int = _hull_band

    _hull_integrity = maxi(HULL_INTEGRITY_MIN, _hull_integrity - amount)
    _hull_scars += 1  # 基础事件

    var new_band: int = _get_hull_band(_hull_integrity)
    _count_band_crossings(old_integrity, _hull_integrity, old_band)

    # Emit signals
    hull_integrity_changed.emit(old_integrity, _hull_integrity)

    if new_band != old_band:
        _hull_band = new_band
        hull_band_changed.emit(old_band, new_band)

    # 中探索损伤可能影响适航——即时重检
    _check_and_update_cargo_volume()
    _check_departure_readiness()
```

### apply_module_damage Interface

```text
var _module_damage_types: Dictionary = {}  # Dict[StringName, String]

func apply_module_damage(slot_id: StringName, damage_type: StringName) -> void:
    if not _is_valid_slot(slot_id):
        return  # 静默无操作

    var slot: Dictionary = _slots[slot_id]

    # 防御性安全网——已空或已损坏则无操作
    if slot["visible_state"] == VisibleState.EMPTY:
        return
    if slot["actual_state"] == ActualState.DAMAGED:
        return  # 已损坏——不二次损坏

    var old_actual: int = slot["actual_state"]
    var old_visible: int = slot["visible_state"]

    # 标记 actual_state 为 damaged
    slot["actual_state"] = ActualState.DAMAGED
    # visible_state 同步——中探索损伤是即时可感知的
    slot["visible_state"] = VisibleState.DAMAGED

    # 存储 damage_type
    _module_damage_types[slot_id] = damage_type

    # Emit signals in order
    actual_state_changed.emit(slot_id, _actual_state_to_string(old_actual), &"damaged")

    if old_visible != VisibleState.DAMAGED:
        _emit_slot_changed(slot_id, old_visible, VisibleState.DAMAGED)
        # ↑ emits slot_state_changed + efficiency_changed + checks departure readiness


func get_module_damage_type(slot_id: StringName) -> StringName:
    return _module_damage_types.get(slot_id, "")
```

### Mid-Exploration can_depart Context

```text
# 中探索期间，玩家可能在特定检查点尝试出航
# apply_hull_damage 和 apply_module_damage 末尾均调用 _check_departure_readiness()
# 这确保 can_depart() 始终反映最新损伤状态

# 注意：中探索出航检查点是否存在的问题属于 Navigation #10 和 Exploration #11
# ModuleHullManager 仅提供准确的 can_depart() 查询——不做任何出航决策
```

### get_installed_slots for Combat System

```text
func get_installed_slots() -> Array[StringName]:
    var installed: Array[StringName] = []
    for slot_id in SLOT_IDS:
        var slot: Dictionary = _slots[slot_id]
        if slot["visible_state"] != VisibleState.EMPTY:
            installed.append(slot_id)
    return installed

# Combat #12 在威胁结算前调用此方法——获取可被损坏的模块列表
# 在调用 apply_module_damage 前过滤已损坏槽位（防御性安全网在 #8 侧也生效）
```

---

## Out of Scope

- 侦察模块 NPC 交付的具体剧情和对话——属于叙事系统 + 探索系统 #11
- Combat #12 在威胁结算中调用 apply_hull_damage/apply_module_damage 的具体逻辑——属于 Combat #12 + ADR-0018
- damage_type 字符串的语义消费（如 "guard_impact"→特定 VFX/音效）——属于反馈系统 #17
- 中探索出航检查点的存在性和触发条件——属于 Navigation #10 + Exploration #11
- 探索系统首次返航后的侦察模块交付事件链——属于探索系统 #11

---

## QA Test Cases

- **AC-1 through AC-3**: Starting state
  - Given: new game → scout not available, cargo in slot_b installed
  - Given: slot_a=empty, interactable but no module available

- **AC-7 and AC-8**: apply_hull_damage
  - Given: integrity=100, amount=30 → integrity=70, band→damaged, scars+1
  - Given: integrity=30, amount=35 → integrity=0, band→destroyed, scars+3

- **AC-10 through AC-12**: apply_module_damage
  - Given: slot_a=scout installed → apply_module_damage → damaged (η=0.6)
  - Given: slot_a=already damaged → apply_module_damage → no-op
  - Given: slot_a=empty → apply_module_damage → no-op

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/modules/CombatDamageInterfaceTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (slot state machine), Story 003 (hull integrity/band/scars), Story 004 (can_depart), Story 007 (starting state), combat-threat Epic (apply_hull_damage/apply_module_damage caller), exploration-scavenge Epic (scout module delivery event)
- Unlocks: — (final module story; all external damage sources reference these interfaces)
