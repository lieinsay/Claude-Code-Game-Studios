# Story 006: Module Signal Contract

> **Epic**: Modules & Hull State
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`, `TR-modules-002`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — 6 typed signals with emit ordering), ADR-0002 (Signal Communication Protocol — typed params + sync emit)
**ADR Decision Summary**: 模块系统通过 6 个 Godot signal 向 Hub 和 UI 系统广播状态变更（emit-after-mutation）。Signal 命名遵循 `{名词}_{动词过去时}`。发射顺序约定：actual_state_changed → slot_state_changed → module_efficiency_changed → departure_readiness_changed。船体信号（hull_integrity_changed 先于 hull_band_changed）独立于模块信号链。departure_readiness_changed 仅在 can/reasons 与上次缓存值不同时触发。所有 signal 参数为 typed 类型——无 Dictionary/Node/Resource 引用。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 6 个 signal 均使用 typed 参数；emit-after-mutation——回调中可安全调用 get_* 查询方法；departure_readiness_changed 仅在值实际变化时 emit
- Forbidden: signal 参数携带 Dictionary/Node/Resource 引用；在状态变更前 emit signal；回调中调用变更方法（重入防护返回 ERR_BUSY）
- Guardrail: signal 级联深度 ≤ 2 跳；Hub/UI 不得在 signal handler 中调用模块系统的变更方法

---

## Acceptance Criteria

### Signal Definitions

- [ ] **AC-1**: GIVEN ModuleHullManager Autoload #8，WHEN 检查 signal 声明，THEN 以下 6 个 signal 已声明且参数均为 typed：

| Signal | Parameters | Emit Timing |
|--------|-----------|-------------|
| `slot_state_changed` | `slot_id: StringName, old_state: StringName, new_state: StringName` | visible_state 变更后 |
| `actual_state_changed` | `slot_id: StringName, old_state: StringName, new_state: StringName` | actual_state 变更后（由航行系统写入） |
| `hull_integrity_changed` | `old_value: int, new_value: int` | integrity 值变更后 |
| `hull_band_changed` | `old_band: StringName, new_band: StringName` | hull band 变更后 |
| `module_efficiency_changed` | `slot_id: StringName, old_eff: float, new_eff: float` | η_final 变更后 |
| `departure_readiness_changed` | `can_depart: bool, reasons: Array[StringName]` | can_depart 结果变更后 |

### Signal Emit Ordering

- [ ] **AC-2**: GIVEN 返航后模块 actual_state 被航行系统写入 + visible_state 变为 unchecked，WHEN emit 发生，THEN 顺序为：actual_state_changed → slot_state_changed → module_efficiency_changed → departure_readiness_changed
- [ ] **AC-3**: GIVEN 船体受到伤害（integrity 和 band 同时变化），WHEN emit 发生，THEN hull_integrity_changed 先于 hull_band_changed

### Emit-After-Mutation

- [ ] **AC-4**: GIVEN slot_state_changed 发射，WHEN consumer 回调中调用 get_module_efficiency(slot_id)，THEN 返回新状态对应的效率值——状态已先于 signal 更新
- [ ] **AC-5**: GIVEN 任何 signal handler 中，WHEN consumer 尝试调用模块变更方法（install_module、uninstall_module、swap_module 等），THEN 被重入防护拦截——返回 ERR_BUSY

### Departure Readiness Signal Deduplication

- [ ] **AC-6**: GIVEN 模块状态变更但不影响 can_depart 结果（如侦察模块 check→installed 不改变 M_max 超载状态），WHEN signal chain 执行，THEN departure_readiness_changed 不 emit——can/reasons 与缓存值相同
- [ ] **AC-7**: GIVEN 货仓模块被卸下导致 M_max 从 24→12 且超载（can_depart 从 true→false），WHEN signal chain 执行，THEN departure_readiness_changed 触发——can/reasons 发生变化

### Hub Integration Contract

- [ ] **AC-8**: GIVEN slot_state_changed("slot_a", "installed", "damaged")，WHEN Hub 接收，THEN Hub 更新该槽位的视觉表现（状态灯黄、模块外观损伤痕迹）
- [ ] **AC-9**: GIVEN hull_integrity_changed(50, 35)，WHEN UI/HUD 接收，THEN HUD 船体完整性指示更新——分段条形缩短、颜色可能变化
- [ ] **AC-10**: GIVEN departure_readiness_changed(false, ["overloaded"])，WHEN Hub 接收，THEN Hub 出航确认按钮置灰或显示阻断原因

### Signal Naming Convention

- [ ] **AC-11**: GIVEN 所有 6 个 signal，WHEN 检查命名，THEN 遵循 `{名词}_{动词过去时}` 格式——与 Godot 内置 signal 风格和 ADR-0002 一致

---

## Implementation Notes

### Signal Declarations

```text
# ModuleHullManager.cs — Autoload #8
extends Node

# === Module Signals ===
signal slot_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName)
signal actual_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName)
signal module_efficiency_changed(slot_id: StringName, old_eff: float, new_eff: float)

# === Hull Signals ===
signal hull_integrity_changed(old_value: int, new_value: int)
signal hull_band_changed(old_band: StringName, new_band: StringName)

# === Departure Signal ===
signal departure_readiness_changed(can_depart: bool, reasons: Array[StringName])
```

### Emit Helper — Slot State Change

```text
func _emit_slot_changed(slot_id: StringName, old_visible: int, new_visible: int) -> void:
    # StringName conversion for signal params
    var old_s: StringName = _visible_state_to_string(old_visible)
    var new_s: StringName = _visible_state_to_string(new_visible)
    slot_state_changed.emit(slot_id, old_s, new_s)

    # 效率可能已变化
    _emit_efficiency_if_changed(slot_id)

    # 适航状态可能已变化
    _check_departure_readiness()


func _visible_state_to_string(state: int) -> StringName:
    match state:
        VisibleState.EMPTY:
            return &"empty"
        VisibleState.INSTALLED:
            return &"installed"
        VisibleState.DAMAGED:
            return &"damaged"
        VisibleState.UNCHECKED:
            return &"unchecked"
        _:
            return &"unknown"
```

### Emit Helper — Efficiency Change (Deduplicated)

```text
# Per-slot cached efficiency for deduplication
var _cached_efficiency: Dictionary = {}  # Dict[StringName, float]

func _emit_efficiency_if_changed(slot_id: StringName) -> void:
    var new_eff: float = _get_effective_efficiency(slot_id)  # η_final
    var old_eff: float = _cached_efficiency.get(slot_id, -1.0)

    if not is_equal_approx(old_eff, new_eff):
        _cached_efficiency[slot_id] = new_eff
        module_efficiency_changed.emit(slot_id, old_eff, new_eff)
```

### Emit Ordering — Post-Voyage Example

```text
func on_voyage_completed(module_damage_flags: Dictionary) -> void:
    for slot_id in SLOT_IDS:
        var slot: Dictionary = _slots[slot_id]
        if slot["visible_state"] == VisibleState.EMPTY:
            continue

        # 1. actual_state 变更（由航行系统写入）
        var old_actual: int = slot["actual_state"]
        var was_damaged: bool = module_damage_flags.get(slot_id, false)
        slot["actual_state"] = ActualState.DAMAGED if was_damaged else ActualState.INSTALLED

        if slot["actual_state"] != old_actual:
            var old_s: StringName = _actual_state_to_string(old_actual)
            var new_s: StringName = _actual_state_to_string(slot["actual_state"])
            actual_state_changed.emit(slot_id, old_s, new_s)

        # 2. visible_state 变更
        var old_visible: int = slot["visible_state"]
        if old_visible != VisibleState.DAMAGED:
            slot["visible_state"] = VisibleState.UNCHECKED

        if slot["visible_state"] != old_visible:
            _emit_slot_changed(slot_id, old_visible, slot["visible_state"])
            # ↑ 内部调用 _emit_efficiency_if_changed + _check_departure_readiness

    # 船体变更独立于模块信号链——可能已在 apply_hull_damage 中触发
```

### Re-entrancy Guard

```text
var _is_mutating: bool = false

func _guard_mutation() -> bool:
    if _is_mutating:
        return false
    _is_mutating = true
    return true

func _release_mutation() -> void:
    _is_mutating = false

# 在每个变更方法入口：
# func install_module(...):
#     if not _guard_mutation():
#         return ERR_BUSY
#     # ... perform mutation ...
#     _release_mutation()
#     return OK
```

### Hub Connection Example

```text
# HubManager._ready():
func _connect_module_signals() -> void:
    ModuleHullManager.slot_state_changed.connect(_on_slot_state_changed)
    ModuleHullManager.hull_integrity_changed.connect(_on_hull_integrity_changed)
    ModuleHullManager.hull_band_changed.connect(_on_hull_band_changed)
    ModuleHullManager.departure_readiness_changed.connect(_on_departure_readiness_changed)


func _on_slot_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName) -> void:
    # 更新 Hub 中对应槽位的视觉表现
    _update_slot_visual(slot_id, new_state)
    # 若槽位为 cargo_module，检查 room_exists 变化
    if slot_id == &"slot_b":  # or whichever slot holds cargo_module
        _check_cargo_hold_existence()


func _on_departure_readiness_changed(can_depart: bool, reasons: Array[StringName]) -> void:
    # 更新出航确认按钮状态
    if can_depart:
        _enable_departure_button()
    else:
        _disable_departure_button(reasons)
```

---

## Out of Scope

- Hub 接收 signal 后的具体视觉更新逻辑（属于 Hub Story 003 — 模块槽状态显示）
- HUD 船体完整性条的具体实现（属于 UI 系统 #16）
- 出航确认 UI 的阻断原因显示（属于 UI 系统 #16）
- 航行系统调用 on_voyage_completed 的具体实现（属于 Navigation #10）

---

## QA Test Cases

- **AC-1**: Signal declarations
  - Given: ModuleHullManager.cs
  - When: 检查 signal 声明
  - Then: 6 signals, all typed params, no Dictionary

- **AC-2**: Emit ordering
  - Given: 返航后 module damaged
  - When: on_voyage_completed
  - Then: actual_state_changed emit before slot_state_changed emit before module_efficiency_changed

- **AC-6**: Deduplication
  - Given: scout module check (unchecked→installed), M_max unchanged, not overloaded
  - When: _check_departure_readiness()
  - Then: departure_readiness_changed NOT emitted (can/reasons same as cached)

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/modules/SignalContractTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-005 (all state mutations produce signals), airship-hub Epic (signal consumers), ui-hud-interface Epic (HUD signal consumers)
- Unlocks: Story 007 (signal emit during save boundaries documented)
