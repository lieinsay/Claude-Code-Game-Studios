# Story 007: Signal Contract & HUD Integration

> **Epic**: Airship Hub
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-001`

**ADR Governing Implementation**: ADR-0002 (Signal Communication Protocol — typed params + sync emit + max cascade depth 2), ADR-0001 (HubManager Autoload #7 — signal producer for Hub events), ADR-0012 (UI/Input Routing — HUD data contract)
**ADR Decision Summary**: Hub 发射 7 个跨系统 signal 遵循 `{名词}_{动词过去时}` 命名规范，所有参数 typed。HUD 常驻指标（船体完整性、仓库余量、货舱装载）由 Hub 提供数据——按需更新（on-change），非每帧轮询。departure_locked 期间所有模态面板强制关闭。Hub 信号的级联深度 ≤ 2 跳。Signal payload 不得携带 Node/Resource/Object/Callable 引用。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 所有 Hub signal 使用 typed 参数，遵循 ADR-0002 `{名词}_{动词过去时}` 命名；HUD 指标更新为 on-change 非每帧；departure_locked → 所有面板强制关闭
- Forbidden: signal payload 携带 Dictionary/Node/Resource 引用；signal 级联深度 > 2；HUD 每帧轮询 Hub 状态
- Guardrail: signal emit 前状态已写入（emit-after-mutation）；departure_locked 中任意站点 Use 被阻断为 target_disabled

---

## Acceptance Criteria

### Hub Signal Definitions

- [ ] **AC-1**: GIVEN HubManager Autoload #7，WHEN 检查 signal 声明，THEN 以下 7 个 signal 已声明且参数均为 typed：

| Signal | Parameters | Emit Timing |
|--------|-----------|-------------|
| `departure_initiated` | `mode: StringName, route_id: StringName` | departure_locked → in_transit 转换时 |
| `departure_rejected` | `mode: StringName, reason_code: StringName` | departure_locked → landed 回退时 |
| `arrival_completed` | `from_route_id: StringName` | arrival → landed 转换完成时 |
| `station_activated` | `station_id: StringName, interaction_type: StringName` | 站点 request_use() 成功时 |
| `station_released` | `station_id: StringName` | 站点 release() 完成时 |
| `module_slot_changed` | `slot_id: StringName, old_state: int, new_state: int` | sync_module_slot_state() 检测到状态变化时 |
| `room_existence_changed` | `room_id: StringName, now_exists: bool` | room_exists() 结果变化时 |

- [ ] **AC-2**: GIVEN 所有 Hub signal，WHEN 检查命名，THEN 遵循 `{名词}_{动词过去时}` 格式——与 Godot 内置 signal 风格一致

### Signal Emit Contract

- [ ] **AC-3**: GIVEN 任意 Hub signal emit，WHEN 检查 emit 时序，THEN signal emit 在状态变更之后——所有 consumer 读取的是已更新状态（emit-after-mutation）
- [ ] **AC-4**: GIVEN 任意 Hub signal，WHEN 检查 payload，THEN 不含 Dictionary 包裹、不含 Node/Resource/Object/Callable 引用——所有参数为 primitive/StringName/int/float/bool
- [ ] **AC-5**: GIVEN 任意 Hub signal 级联，WHEN 追踪 consumer→后续 emit，THEN 级联深度不超过 2 跳（Hub→A→B 允许；Hub→A→B→C 禁止）

### HUD Indicator Data Contract

- [ ] **AC-6**: GIVEN Hub 场景活跃（landed 或 arrival），WHEN HUD 查询关键指标，THEN Hub 提供以下 3 个数据点：

| Indicator | Data Source | Type | Update Trigger |
|-----------|------------|------|---------------|
| 船体完整性 (hull_integrity) | ModulesSystem #8 | float (0.0–1.0) | module_slot_changed signal |
| 仓库余量 (storage_capacity) | ResourcesManager #5 | Dictionary {used, total} | station_released (storage-shelf) |
| 货舱装载 (cargo_load) | ResourcesManager #5 | Dictionary {used, total} 或 null (无货舱) | station_released (cargo-bay), room_existence_changed |

- [ ] **AC-7**: GIVEN Hub 指标数据，WHEN HUD 请求更新，THEN Hub 按需提供当前值（HUD 缓存并仅在 signal 触发时刷新）——禁止 HUD 每帧调用 `_process()` 轮询 Hub 状态
- [ ] **AC-8**: GIVEN 货舱模块未安装（room_exists(cargo_hold) = false），WHEN HUD 查询 cargo_load，THEN 返回 null——HUD 显示"无货舱"

### Departure Lock Panel Management

- [ ] **AC-9**: GIVEN docking_state → departure_locked，WHEN 状态转换完成，THEN 所有已打开的站点面板强制关闭——UIManager.close_all_hub_panels()
- [ ] **AC-10**: GIVEN departure_locked 期间 + 玩家对任意站点按 Use，WHEN is_enabled() 评估，THEN 返回 false——Use 被阻断为 target_disabled（面板不打开）

### Cross-System Signal Integration

- [ ] **AC-11**: GIVEN departure_initiated(mode="chart", route_id="route_01")，WHEN signal emit，THEN ChartSystem #9 接收并开始航线导航流程
- [ ] **AC-12**: GIVEN departure_initiated(mode="direct", route_id="")，WHEN signal emit，THEN NavigationSystem #10 接收并启动自由飞行界面
- [ ] **AC-13**: GIVEN arrival_completed(from_route_id="route_01")，WHEN signal emit，THEN 存档系统 #3 在下一个稳定边界触发 progress.airship 快照保存
- [ ] **AC-14**: GIVEN module_slot_changed(slot_id="cargo_module", old_state=1, new_state=0)，WHEN signal emit，THEN 资源系统 #5 接收并以规则处理货舱内容（部分损失 + 可回收货箱）

### HUD Accessibility

- [ ] **AC-15**: GIVEN HUD 数值变更（如仓库余量从 920/1000 → 850/1000），WHEN 更新显示，THEN 有非颜色反馈（容量条轻微闪烁或数值跳动）——不可仅依赖颜色传达变化
- [ ] **AC-16**: GIVEN 船体完整性分段条形（3-5 段），WHEN 显示，THEN 同时使用颜色编码（绿/黄/红）+ 段数区分——不可仅依赖颜色

---

## Implementation Notes

### Signal Declarations (HubManager Autoload #7)

```text
# HubManager.cs — Autoload #7
extends Node

# === Hub → External Signals (ADR-0002 compliant) ===

# Mode A: 舱门出航 → 航图系统
signal departure_initiated(mode: StringName, route_id: StringName)

# 出航被拒绝（航图系统拒绝或加载超时）
signal departure_rejected(mode: StringName, reason_code: StringName)

# 返航抵达完成
signal arrival_completed(from_route_id: StringName)

# 站点交互
signal station_activated(station_id: StringName, interaction_type: StringName)
signal station_released(station_id: StringName)

# 模块槽状态变更
signal module_slot_changed(slot_id: StringName, old_state: int, new_state: int)

# 舱室存在性变更（货舱拼装/移除）
signal room_existence_changed(room_id: StringName, now_exists: bool)
```

### Emit-After-Mutation Pattern

```text
# All signal emits follow this pattern:
# 1. Mutate state first
# 2. Emit signal second
# Consumers always read post-mutation state

func _transition_docking(new_state: int) -> void:
    var old_state: int = docking_state
    docking_state = new_state  # ← mutation first

    match new_state:
        DockingState.IN_TRANSIT:
            # signal emit second — consumers read docking_state == IN_TRANSIT
            departure_initiated.emit(_last_departure_mode, _last_departure_route)
        DockingState.LANDED:
            if old_state == DockingState.ARRIVAL:
                arrival_completed.emit(_last_arrival_route)
```

### HUD Data Provider Interface

```text
# Hub 提供 HUD 数据的查询接口——UI 系统 #16 按需调用
# Hub 不拥有 HUD 节点的引用——仅提供数据

func get_hud_indicators() -> Dictionary:
    return {
        "hull_integrity": _get_hull_integrity(),
        "storage_capacity": _get_storage_display(),
        "cargo_load": _get_cargo_load_display(),
    }


func _get_hull_integrity() -> float:
    # 从模块系统 #8 查询船体完整性
    return ModulesSystem.get_hull_integrity()


func _get_storage_display() -> Dictionary:
    var summary: Dictionary = ResourcesManager.get_storage_summary()
    return {
        "used": summary.get("used_volume", 0),
        "total": summary.get("total_capacity", 1000),
    }


func _get_cargo_load_display() -> Variant:
    if not room_exists(&"cargo_hold"):
        return null  # HUD 显示"无货舱"

    var usage: Dictionary = ResourcesManager.get_cargo_bay_usage()
    return {
        "used": usage.get("used_volume", 0),
        "total": usage.get("total_capacity", 500),
    }
```

### HUD Update Registration (On-Change Pattern)

```text
# HUD 系统 #16 在 _ready() 中连接 Hub signal
# 每次 signal 触发 → HUD 查询对应指标 → 更新显示
# 不使用 _process() 轮询

# HUD 侧（UI 系统 #16 拥有）:
func _ready() -> void:
    HubManager.station_released.connect(_on_station_released)
    HubManager.module_slot_changed.connect(_on_module_slot_changed)
    HubManager.room_existence_changed.connect(_on_room_existence_changed)
    HubManager.arrival_completed.connect(_on_arrival_completed)


func _on_station_released(station_id: StringName) -> void:
    match station_id:
        &"storage-shelf":
            _refresh_storage_display()
        &"cargo-bay":
            _refresh_cargo_display()


func _on_module_slot_changed(slot_id: StringName, old_state: int, new_state: int) -> void:
    _refresh_hull_integrity()


func _refresh_storage_display() -> void:
    var data: Dictionary = HubManager.get_hud_indicators().get("storage_capacity", {})
    _update_storage_bar(data.get("used", 0), data.get("total", 1000))


func _refresh_hull_integrity() -> void:
    var integrity: float = HubManager.get_hud_indicators().get("hull_integrity", 1.0)
    _update_hull_bar(integrity)
```

### Departure Lock Panel Force Close

```text
func _on_departure_locked() -> void:
    _set_movement_rooted(true)
    _disable_all_stations_during_lock()

    # 强制关闭所有已打开的站点面板
    UIManager.close_all_hub_panels()

    _start_departure_lock_timer()
    _start_watchdog_timer()
```

### Signal Cascade Depth Enforcement

```text
# 静态分析辅助：Hub signal → 下游 consumer → 下游再 emit 的深度检查
# Hub 信号的直接 consumer:
#   departure_initiated → ChartSystem.initiate_departure() / NavigationSystem.initiate_free_flight()
#     → 这些系统可能 emit 自己的 signal（如 route_committed）
#     → Hub 不连接这些二级 signal ← cascade depth = 1（Hub→Chart→X），Hub 侧 depth = 2（含 Chart emit）
#     注意：Hub→Chart→X 的第三跳（Chart emit → X）是 Chart 的责任，Hub 不参与
#
# 规则：Hub signal handler 内部不 emit 其他 Hub signal（避免 Hub→Hub 自级联）
```

### Station is_enabled() with Departure Lock

```text
# HubStation.is_enabled() 已包含 departure_locked 检查（Story 002 AC-13）
func is_enabled() -> bool:
    if HubManager.docking_state != HubManager.DockingState.LANDED:
        return false
    return _state == StationState.READY and _check_conditions()
```

---

## Out of Scope

- HUD 的具体 UI 呈现（控件布局、样式、动画）——属于 UI 系统 #16
- 船体完整性分段条的具体视觉设计——属于 UI 系统 #16
- 站点面板的具体 UI 实现——属于 UI 系统 #16
- ChartSystem 接收 departure_initiated 后的航线导航逻辑——属于 Chart & Route Planning #9
- NavigationSystem 接收后的自由飞行逻辑——属于 Navigation & Route Risk #10
- 资源系统处理货舱模块摧毁后的内容规则——属于 Resources #5 + Modules #8

---

## QA Test Cases

- **AC-3**: Emit-after-mutation
  - Given: docking_state = DEPARTURE_LOCKED, _last_departure_mode = "chart"
  - When: _transition_docking(IN_TRANSIT)
  - Then: docking_state = IN_TRANSIT (已变更), departure_initiated 在变更后 emit

- **AC-4**: Signal payload types
  - Given: 所有 7 个 Hub signal
  - When: 检查参数类型
  - Then: 无 Dictionary/Node/Resource/Object/Callable 参数

- **AC-9**: Departure lock panel close
  - Given: 玩家打开了仓库面板（storage-shelf 交互中）
  - When: docking_state → departure_locked
  - Then: UIManager.close_all_hub_panels() 被调用，仓库面板关闭

- **AC-14**: module_slot_changed → resources handling
  - Given: cargo_module slot_state = INSTALLED, 货舱中有货物
  - When: module_slot_changed("cargo_module", INSTALLED, EMPTY)
  - Then: 资源系统 #5 接收 signal → 处理货舱内容

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/hub/SignalContractTest.csproj` — must exist and pass
**Status**: [x] Created and passing
**Review evidence**: 2026-05-12 Codex review reran `dotnet run --no-build --project tests/integration/hub/SignalContractTest.csproj` — PASS (4/4 checks)

---

## Dependencies

- Depends on: Story 001 (state machine — signal emit timing), Story 002 (station registration — station_activated/station_released), Story 003 (module slot state — module_slot_changed), Story 004 (departure flow — departure_initiated/departure_rejected), Story 005 (arrival flow — arrival_completed), Story 006 (trace_anchor_changed), chart-route-planning Epic (departure_initiated consumer), navigation-route-risk Epic (departure_initiated consumer for direct mode), UI-HUD Epic (HUD consumer)
- Unlocks: Story 008 (signal emit during snapshot save boundaries)
