# Story 003: Room Gating & Module Slot Display

> **Epic**: Airship Hub
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-003`

**ADR Governing Implementation**: ADR-0009 (Module / Hull System — module slot state), ADR-0001 (HubManager Autoload #7)
**ADR Decision Summary**: 舱室存在性由 `room_exists()` 公式判定——驾驶舱/生活舱/工程舱始终存在，货舱存在性受 cargo_module 安装状态门控。模块槽有 4 种显示状态：empty（空槽）、installed（正常）、damaged（已损伤）、unchecked（返航后未检查）。Hub 不拥有模块状态逻辑——仅从模块系统 #8 读取 slot_state 并驱动视觉表现。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: room_exists() 根据 room_base_exists OR (required_module != null AND module_installed)；module_installed 包括 installed/damaged/unchecked
- Forbidden: Hub 拥有或修改模块效果数据——模块系统 #8 拥有模块定义和效果逻辑
- Guardrail: 货舱模块被摧毁时，货舱内容按资源系统规则处理（Story 008 集成）

---

## Acceptance Criteria

### room_exists() Formula

- [ ] **AC-1**: GIVEN cargo_module 已安装在模块接口 B，WHEN `room_exists(&"cargo_hold")`，THEN 返回 true——货舱存在且可步行进入
- [ ] **AC-2**: GIVEN cargo_module 未安装（模块槽 empty），WHEN `room_exists(&"cargo_hold")`，THEN 返回 false——货舱区域视觉上为船体外部缺口/空白
- [ ] **AC-3**: GIVEN 驾驶舱/生活舱/工程舱，WHEN `room_exists()` 任意条件，THEN 始终返回 true——base_exists=true, required_module=null
- [ ] **AC-4**: GIVEN 模块槽状态为 damaged 但 module 仍 installed，WHEN `room_exists(&"cargo_hold")`，THEN 返回 true——舱室存在性只看安装不看完好度

### Module Slot State Display

- [ ] **AC-5**: GIVEN 模块槽为 empty，WHEN 检查 Hub 视觉表现，THEN 空安装位支架+轮廓线，状态灯灭
- [ ] **AC-6**: GIVEN 模块槽为 installed，WHEN 检查 Hub 视觉表现，THEN 状态灯绿，模块完整外观
- [ ] **AC-7**: GIVEN 模块槽为 damaged，WHEN 检查 Hub 视觉表现，THEN 状态灯黄，模块外观有损伤痕迹
- [ ] **AC-8**: GIVEN 模块槽为 unchecked（返航后未检查），WHEN 检查 Hub 视觉表现，THEN 状态灯黄闪（缓慢），模块外观正常但叠加"?"标记

### Module Slot State Integration

- [ ] **AC-9**: GIVEN Hub 的 module_slot_state 从模块系统 #8 同步，WHEN 模块系统更新 slot 状态，THEN Hub 的 module_slot_state 反映最新值——Hub 不拥有模块状态机，仅持有镜像
- [ ] **AC-10**: GIVEN 模块槽为 empty + 玩家交互安装模块，WHEN 安装完成，THEN 模块系统返回 installed，Hub 更新 slot_state，舱室（若为货舱）从 not_exists → exists

### Cargo Bay Content Protection

- [ ] **AC-11**: GIVEN 货舱 used_volume > 0，WHEN 玩家在模块接口尝试卸下 cargo_module，THEN 操作被拒——UI 显示"请先清空货舱"，卸下按钮置灰
- [ ] **AC-12**: GIVEN 货舱 used_volume = 0，WHEN 玩家尝试卸下 cargo_module，THEN 操作允许——货舱清空后模块可安全卸下

### Room-to-Module Mapping

- [ ] **AC-13**: GIVEN MVP room-to-module mapping，WHEN 查询 room_required_module，THEN: cockpit → null, living_quarters → null, engineering_bay → null, cargo_hold → cargo_module

---

## Implementation Notes

### room_exists() Formula

```text
# Room base existence table
const ROOM_BASE_EXISTS: Dictionary = {
    &"cockpit": true,
    &"living_quarters": true,
    &"engineering_bay": true,
    &"cargo_hold": false,
}

# Room-to-module mapping
const ROOM_REQUIRED_MODULE: Dictionary = {
    &"cockpit": "",
    &"living_quarters": "",
    &"engineering_bay": "",
    &"cargo_hold": &"cargo_module",
}

func room_exists(room_id: StringName) -> bool:
    var base_exists: bool = ROOM_BASE_EXISTS.get(room_id, true)
    if base_exists:
        return true

    var required_module: StringName = ROOM_REQUIRED_MODULE.get(room_id, "")
    if required_module == "":
        return base_exists

    return _is_module_installed(required_module)


func _is_module_installed(module_id: StringName) -> bool:
    var slot_state: int = _module_slot_state.get(module_id, MODULE_SLOT_EMPTY)
    return slot_state in [MODULE_SLOT_INSTALLED, MODULE_SLOT_DAMAGED, MODULE_SLOT_UNCHECKED]
```

### Module Slot State Mirror

```text
enum ModuleSlotState {
    EMPTY,        # 未安装模块
    INSTALLED,    # 已安装，正常工作
    DAMAGED,      # 已安装但有损伤
    UNCHECKED,    # 返航后尚未检查（效率 0.95）
}

# Hub 持有的模块槽状态镜像——由模块系统 #8 同步
var _module_slot_state: Dictionary = {}  # Dict[StringName, int]

# 由模块系统 #8 调用以同步状态
func sync_module_slot_state(slot_id: StringName, state: int) -> void:
    var old_state: int = _module_slot_state.get(slot_id, MODULE_SLOT_EMPTY)
    _module_slot_state[slot_id] = state

    # 若货舱模块状态变更，检查 room_exists 变化
    if slot_id == &"cargo_module":
        var was_exists: bool = _is_module_installed_eval(old_state)
        var now_exists: bool = _is_module_installed(state)
        if not was_exists and now_exists:
            _on_cargo_hold_appeared()
        elif was_exists and not now_exists:
            _on_cargo_hold_disappeared()

func _is_module_installed_eval(state: int) -> bool:
    return state in [MODULE_SLOT_INSTALLED, MODULE_SLOT_DAMAGED, MODULE_SLOT_UNCHECKED]
```

### Cargo Hold Lifecycle

```text
func _on_cargo_hold_appeared() -> void:
    # 货舱拼装到船体——播放拼装动画，开放步行区域
    _play_room_attach_animation(&"cargo_hold")
    _set_room_collision(&"cargo_hold", true)
    cargo_bay_station.enable()

func _on_cargo_hold_disappeared() -> void:
    # 货舱模块被摧毁——内容按资源系统规则处理（部分损失+可回收货箱）
    # 视觉上区域消失，碰撞体移除
    _set_room_collision(&"cargo_hold", false)
    cargo_bay_station.disable()
    repair_point_station.mark_slot_needs_repair(&"cargo_module")
```

### Cargo Bay Content Protection

```text
func can_unequip_module(slot_id: StringName) -> bool:
    if slot_id == &"cargo_module":
        var usage: Dictionary = ResourcesManager.get_cargo_bay_usage()
        return usage.get("used_volume", 0) == 0
    return true

func get_unequip_block_reason(slot_id: StringName) -> String:
    if slot_id == &"cargo_module":
        return "请先清空货舱"
    return ""
```

### Visual Display Mapping

```text
func _get_slot_indicator_color(state: int) -> Color:
    match state:
        MODULE_SLOT_EMPTY:
            return Color.GRAY
        MODULE_SLOT_INSTALLED:
            return Color.GREEN
        MODULE_SLOT_DAMAGED:
            return Color.YELLOW
        MODULE_SLOT_UNCHECKED:
            return Color.YELLOW  # 闪烁由 AnimationPlayer 控制
        _:
            return Color.GRAY

func _get_slot_indicator_shape(state: int) -> String:
    # 无障碍要求：状态不可仅依赖颜色——同时使用形状区分
    match state:
        MODULE_SLOT_EMPTY:
            return "○"   # 空心圆
        MODULE_SLOT_INSTALLED:
            return "✓"   # 对勾
        MODULE_SLOT_DAMAGED:
            return "⚡"  # 闪电（受损）
        MODULE_SLOT_UNCHECKED:
            return "?"   # 问号
        _:
            return "○"
```

---

## Out of Scope

- 模块系统的状态机逻辑——属于 Modules & Hull State Epic (#8)
- 模块效果计算（容积加成、damaged 效果打折）——属于模块系统 #8
- 舱室拼装动画的具体实现（属于 Visual/Feel 类型——Story 008 定义过渡契约）
- 船体维修点的具体修复逻辑（属于 WorldRepair #13）

---

## QA Test Cases

- **AC-1 through AC-4**: room_exists formula
  - Given: cargo_module installed → room_exists("cargo_hold") = true
  - Given: cargo_module empty → room_exists("cargo_hold") = false
  - Given: cargo_module damaged → room_exists("cargo_hold") = true (damaged ≠ not installed)
  - Given: cockpit → room_exists("cockpit") = true (always)

- **AC-11 and AC-12**: Cargo bay content protection
  - Given: cargo_bay used_volume=300, module_slot=cargo_module installed
  - When: can_unequip_module("cargo_module")
  - Then: 返回 false, reason="请先清空货舱"

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/integration/hub/RoomGatingTest.csproj` — must exist and pass
**Status**: [x] Created and passing
**Review evidence**: 2026-05-12 Codex review reran `dotnet run --no-build --project tests/integration/hub/RoomGatingTest.csproj` — PASS (3/3 checks)

---

## Dependencies

- Depends on: Story 001 (scene), Story 002 (station registration), modules-hull-state Epic (#8 — module_slot_state), resources-goods-capacity Epic (#5 — get_cargo_bay_usage)
- Unlocks: Story 004 (departure requires room_exists checks), Story 008 (cargo hold lifecycle persistence)
