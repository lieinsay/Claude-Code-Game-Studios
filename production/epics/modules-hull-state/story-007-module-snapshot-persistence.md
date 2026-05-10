# Story 007: Module Snapshot Persistence

> **Epic**: Modules & Hull State
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-modules-hull-state.md`
**Requirement**: `TR-modules-001`, `TR-modules-003`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — progress.airship 快照 Schema), ADR-0003 (Save System — Canonical JSON, SnapshotPackage API)
**ADR Decision Summary**: 模块系统向存档系统导出纯 Dict 的 progress.airship 快照：{modules: {slot_a: {visible_state, actual_state, efficiency, module_type}, slot_b: {...}}, hull_integrity: int, hull_scars: int}。快照不含 Node/Resource/信号引用。从快照恢复后，模块的 visible_state、actual_state、效率、module_type、船体完整性、伤痕计数与存档时一致。StringName→String 转换用于 JSON 序列化。新游戏起始状态：槽 A empty，槽 B 预装货仓模块（installed, η=1.0），integrity=100，hull_scars=0。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 快照仅存储独立变量——不含派生值（如 M_max、V_effective、can_depart 在加载后重新计算）；StringName key 序列化时转为 String
- Forbidden: 快照包含 Node/Resource/Callable/Signal 引用；使用 store_var()/get_var() Variant blob
- Guardrail: 快照 Schema 验证——缺失必需字段时使用安全默认状态；stale module_type ID 跳过并记录警告

---

## Acceptance Criteria

### Snapshot Schema

- [ ] **AC-1**: GIVEN 模块系统需要持久化，WHEN 构建快照，THEN 包含以下字段：

| Field | Type | Description |
|-------|------|-------------|
| `modules` | Dict[String, Dict] | 每个槽位的模块数据（key 为 String） |
| `modules[slot_id].module_type` | int | ModuleType 枚举值 |
| `modules[slot_id].visible_state` | int | VisibleState 枚举值 |
| `modules[slot_id].actual_state` | int | ActualState 枚举值 |
| `modules[slot_id].efficiency` | float | 缓存的效率值（η_visible，不含波段修正） |
| `hull_integrity` | int | 船体完整性 (0-100) |
| `hull_scars` | int | 船体伤痕计数 (≥0) |

- [ ] **AC-2**: GIVEN 快照，WHEN 检查内容，THEN 不含 M_max（派生自 furnace ratings + efficiency）、V_effective（派生自 cargo slot states + band）、can_depart 结果（派生自以上）——不双重存储

### Save / Load Roundtrip

- [ ] **AC-3**: GIVEN slot_a = scout installed (η=1.0), slot_b = cargo damaged (η=0.5), integrity=45 (damaged band), hull_scars=3，WHEN 存档→加载，THEN 所有值恢复与存档时一致
- [ ] **AC-4**: GIVEN 快照从 JSON 反序列化，WHEN 恢复模块数据，THEN slot key 从 String 转换回 StringName，module_type/visible_state/actual_state 从 int 转换回枚举

### Starting State (New Game)

- [ ] **AC-5**: GIVEN 新游戏初始化，WHEN 查询模块状态，THEN:
  - slot_a: empty（module_type=EMPTY, visible_state=EMPTY, actual_state=EMPTY）
  - slot_b: cargo installed（module_type=CARGO, visible_state=INSTALLED, actual_state=INSTALLED, η=1.0）
  - hull_integrity: 100 (intact band)
  - hull_scars: 0
  - M_max: 12（仅货仓动力炉）

### Snapshot Validation

- [ ] **AC-6**: GIVEN 快照缺少 modules 字段，WHEN 加载验证，THEN 返回 false——使用安全默认状态（同新游戏起始状态）
- [ ] **AC-7**: GIVEN 快照中 modules 某 slot 包含已不存在的 module_type 值（stale ID），WHEN 加载，THEN 跳过该条目并记录警告——不因单条 stale 数据拒绝整个快照

### Domain Serializer Registration

- [ ] **AC-8**: GIVEN Persistence 系统 core_data_ready，WHEN ModuleHullManager 注册领域序列化器，THEN 提供 build/restore/validate 三个方法——存档系统据此读写 progress.airship

### StringName↔String Roundtrip

- [ ] **AC-9**: GIVEN modules 字典 key 为 StringName（如 &"slot_a"），WHEN 序列化到 JSON，THEN key 转换为 String（"slot_a"）
- [ ] **AC-10**: GIVEN JSON 中 modules key 为 String，WHEN 反序列化恢复，THEN key 转换回 StringName

---

## Implementation Notes

### Snapshot Builder

```text
func build_snapshot() -> Dictionary:
    var modules_snapshot: Dictionary = {}

    for slot_id in SLOT_IDS:
        var slot: Dictionary = _slots[slot_id]
        modules_snapshot[str(slot_id)] = {
            "module_type": slot["module_type"],
            "visible_state": slot["visible_state"],
            "actual_state": slot["actual_state"],
            "efficiency": get_module_efficiency(slot_id),  # η_visible (不含波段修正)
        }

    return {
        "modules": modules_snapshot,
        "hull_integrity": _hull_integrity,
        "hull_scars": _hull_scars,
    }
```

### Snapshot Restorer

```text
func restore_from_snapshot(snapshot: Dictionary) -> bool:
    if not _validate_snapshot(snapshot):
        push_error("Module snapshot validation failed — using safe defaults")
        _apply_starting_state()
        return false

    # 恢复模块状态
    var modules_data: Dictionary = snapshot.get("modules", {})
    for key_str in modules_data:
        var slot_id := StringName(key_str)

        # Stale ID 检测
        if slot_id not in SLOT_IDS:
            push_warning("Snapshot contains unknown slot: %s — skipping" % key_str)
            continue

        var data: Dictionary = modules_data[key_str]
        var module_type: int = data.get("module_type", ModuleType.EMPTY)

        # Stale module_type 检测
        if module_type not in [ModuleType.EMPTY, ModuleType.SCOUT, ModuleType.CARGO]:
            push_warning("Snapshot contains invalid module_type for %s: %d — skipping" % [key_str, module_type])
            continue

        _slots[slot_id] = {
            "module_type": module_type,
            "visible_state": data.get("visible_state", VisibleState.EMPTY),
            "actual_state": data.get("actual_state", ActualState.EMPTY),
        }

        # 恢复缓存的效率值
        _cached_efficiency[slot_id] = data.get("efficiency", 0.0)

    # 恢复船体状态
    _hull_integrity = clampi(snapshot.get("hull_integrity", HULL_INTEGRITY_MAX), 0, HULL_INTEGRITY_MAX)
    _hull_scars = maxi(0, snapshot.get("hull_scars", 0))
    _hull_band = _get_hull_band(_hull_integrity)

    # 重新计算派生值
    _cached_v_effective = get_effective_cargo_volume()
    _check_departure_readiness()

    return true
```

### Snapshot Validation

```text
func _validate_snapshot(snapshot: Dictionary) -> bool:
    if snapshot.is_empty():
        return false

    if not snapshot.has("modules"):
        push_error("Snapshot missing required field: modules")
        return false

    if not (typeof(snapshot["modules"]) == TYPE_DICTIONARY):
        return false

    # hull_integrity 可选——缺失时使用默认值 100
    # hull_scars 可选——缺失时使用默认值 0

    return true
```

### Starting State (New Game)

```text
func _apply_starting_state() -> void:
    # 清空所有槽位
    for slot_id in SLOT_IDS:
        _slots[slot_id] = {
            "module_type": ModuleType.EMPTY,
            "visible_state": VisibleState.EMPTY,
            "actual_state": ActualState.EMPTY,
        }
        _cached_efficiency[slot_id] = 0.0

    # 槽 B 预装货仓模块（GDD Rule 22）
    _slots[&"slot_b"] = {
        "module_type": ModuleType.CARGO,
        "visible_state": VisibleState.INSTALLED,
        "actual_state": ActualState.INSTALLED,
    }
    _cached_efficiency[&"slot_b"] = 1.0

    # 船体起始状态
    _hull_integrity = HULL_INTEGRITY_MAX
    _hull_scars = 0
    _hull_band = HullBand.INTACT

    # 派生值
    _cached_v_effective = get_effective_cargo_volume()
    _cached_can_depart = true
    _cached_reasons = []
```

### Domain Serializer Registration

```text
func register_domain_serializer() -> void:
    Persistence.register_domain(&"modules_hull", {
        "build": build_snapshot,
        "restore": restore_from_snapshot,
        "validate": _validate_snapshot,
    })
```

### StringName↔String for Slot Keys

```text
# 序列化时: str(&"slot_a") → "slot_a"
# 反序列化时: StringName("slot_a") → &"slot_a"

# 枚举值直接用 int 存储——序列化为 JSON number
# 反序列化时从 int 转回枚举

func _module_type_from_int(value: int) -> int:
    if value in [ModuleType.EMPTY, ModuleType.SCOUT, ModuleType.CARGO]:
        return value
    return ModuleType.EMPTY
```

---

## Out of Scope

- Canonical JSON sorted-keys 辅助函数——属于存档系统 #3
- Staging→Verify→Promotion 工作流——属于存档系统 #3
- 版本迁移框架——属于存档系统 #3
- 其他领域（Hub、Intel、Resources）的 progress.airship 子快照 Schema——各领域系统自行定义

---

## QA Test Cases

- **AC-3**: Roundtrip
  - Given: slot_a=scout installed, slot_b=cargo damaged, integrity=45, scars=3
  - When: build_snapshot() → JSON → restore_from_snapshot()
  - Then: 所有值一致；M_max/V_effective 重新计算（不在快照中但结果一致）

- **AC-5**: New game starting state
  - Given: _apply_starting_state()
  - When: 查询
  - Then: slot_a=empty, slot_b=cargo installed (η=1.0), integrity=100, scars=0, M_max=12

- **AC-7**: Stale module_type
  - Given: 快照 modules={"slot_a": {"module_type": 99, ...}}
  - When: restore_from_snapshot()
  - Then: slot_a 条目被跳过（保持 empty），警告日志记录

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/modules/SnapshotPersistenceTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001-006 (all state captured in snapshot), local-save-persistence Epic (SnapshotPackage API, register_domain, Canonical JSON), content-registry Epic (stable slot ID validation)
- Unlocks: — (final module story; all subsequent module work references this snapshot Schema)
