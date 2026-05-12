# Story 005: Arrival Flow & State Continuity

> **Epic**: Airship Hub
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-001`

**ADR Governing Implementation**: ADR-0001 (Autoload Boot Order — Hub Scene Phase 4 scene_ready), ADR-0002 (Signal Protocol — arrival_completed)
**ADR Decision Summary**: 返航不是重置——Hub 在 arrival→landed 转换后保留货舱内容、模块安装状态、仓库内容、世界修复痕迹。返航后玩家生成在舱门位置——"推门进舱"瞬间是"归港之锚"核心情感节拍。arrival 动画 ~1-2s，传达"到家了"的安稳感。快照加载时 station_state 统一派生为 ready（busy 是瞬态不持久化）；departure_locked 快照降级为 landed。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: arrival→landed 转换后 R5 状态连续性验证（货舱/模块/仓库/修复痕迹保留）；返航生成点在舱门位置
- Forbidden: arrival 后硬编码重置任何持久状态；departure_locked 快照直接恢复（必须降级为 landed）
- Guardrail: arrival 动画期间 suspend_requested → best-effort 存档捕获 in_transit，重载时重新触发 arrival → landed

---

## Acceptance Criteria

### Arrival → Landed Transition

- [ ] **AC-1**: GIVEN docking_state = in_transit + 航行/探索结束，WHEN 返航触发，THEN docking_state → arrival，播放抵达动画（~1-2s）
- [ ] **AC-2**: GIVEN docking_state = arrival，WHEN 抵达动画完成，THEN docking_state → landed，移动恢复，所有站点根据当前模块/伙伴状态重新启用
- [ ] **AC-3**: GIVEN arrival 动画播放中，WHEN 动画未完成，THEN 移动保持冻结——玩家不可行走或交互

### R5: State Continuity After Return

- [ ] **AC-4**: GIVEN 出航前货舱中有 300 体积货物，WHEN 航行结束返航 → landed，THEN 货舱内容保留（或按航行中事件结果更新——如部分损失），不被重置为空
- [ ] **AC-5**: GIVEN 出航前模块槽 A 为 installed（侦察模块），WHEN 返航 → landed，THEN 模块槽 A 仍为 installed（或按航行损伤结果更新为 damaged）——不被重置为 empty
- [ ] **AC-6**: GIVEN 出航前仓库中有 50 铁矿石，WHEN 返航 → landed，THEN 仓库内容保留（不被重置）
- [ ] **AC-7**: GIVEN 出航前船体维修点有旧修补痕迹，WHEN 航行中完成世界修复事件后返航，THEN 船体维修点出现新修补痕迹——旧痕迹保留+新痕迹添加

### Return Spawn Point (R8)

- [ ] **AC-8**: GIVEN docking_state = arrival → landed 转换完成，WHEN 玩家生成，THEN 位置在舱门（door_spawn_point）——第一视角面对舱门
- [ ] **AC-9**: GIVEN 返航后玩家生成在舱门，WHEN 玩家移动，THEN 可走向舵轮完成停靠——生成后移动系统已恢复，无额外冻结

### Post-Arrival Station State Derivation

- [ ] **AC-10**: GIVEN Hub 从快照加载，WHEN station_state 派生，THEN 所有站点根据当前条件派生为 ready 或 disabled——busy 不持久化，加载时统一派生为 ready（交互已中断，释放锁）
- [ ] **AC-11**: GIVEN 加载时 cargo_module 未安装，WHEN 货舱相关站点（cargo-bay）派生状态，THEN cargo-bay station = disabled（货舱 not_exists）

### Arrival Edge Cases

- [ ] **AC-12**: GIVEN arrival 动画播放中 + 桌面窗口 suspend_requested 触发，WHEN best-effort 存档捕获，THEN 存档中 docking_state = in_transit（arrival 是瞬态不持久化）——重载时重新触发 arrival → landed
- [ ] **AC-13**: GIVEN progress.airship 快照损坏，WHEN Hub 加载失败，THEN 使用安全默认状态：全部站点 ready、生成点甲板中心（首次加载逻辑）、痕迹锚点初始值——并显示警告提示

---

## Implementation Notes

### Arrival Flow

```text
func trigger_arrival() -> void:
    if docking_state != DockingState.IN_TRANSIT:
        push_warning("trigger_arrival called in state %d — ignored" % docking_state)
        return

    _transition_docking(DockingState.ARRIVAL)


func _on_arrival() -> void:
    _set_movement_rooted(true)
    _play_arrival_animation()


func _on_arrival_animation_completed() -> void:
    _transition_docking(DockingState.LANDED)


func _on_landed(from_state: int) -> void:
    _set_movement_rooted(false)

    match from_state:
        DockingState.ARRIVAL:
            # 返航——生成在舱门
            _spawn_player_at_door()
            # 重新派生站点状态（基于当前模块/伙伴状态）
            _derive_all_station_states()
            # 触发痕迹锚点更新
            _refresh_trace_anchors()
        DockingState.DEPARTURE_LOCKED:
            # 出航被拒绝——恢复控制，不移动玩家位置
            pass
        _:
            # 首次加载——生成在舵轮附近（在 _ready() 中处理）
            pass
```

### R5: State Continuity Verification

```text
# Hub 不拥有这些状态——仅验证下游系统在返航后保留了状态
func _verify_state_continuity() -> Dictionary:
    var issues: Array = []

    # 模块系统保留模块状态
    var pre_departure_modules: Dictionary = _departure_snapshot.get("modules", {})
    var post_arrival_modules: Dictionary = _get_current_module_states()
    for slot_id in pre_departure_modules:
        var expected: int = pre_departure_modules[slot_id]
        var actual: int = post_arrival_modules.get(slot_id, -1)
        if actual == -1:
            issues.append("模块槽 %s 状态丢失" % slot_id)

    # 资源系统保留仓库内容
    var pre_storage: int = _departure_snapshot.get("storage_used", -1)
    var post_storage: int = ResourcesManager.get_storage_used_volume()
    if pre_storage >= 0 and post_storage != pre_storage:
        # 差异可能合法（航行中消耗/获取资源由航行系统记录）
        pass

    return {"issues": issues, "ok": issues.is_empty()}
```

### Post-Load Station State Derivation

```text
# 快照中仅存储独立变量（module_slot_state、trace_anchors）
# station_state 在加载时重新派生——不双重存储
func _derive_all_station_states() -> void:
    for station in _stations.values():
        if not station._check_conditions():
            station.disable()
        else:
            # busy 是瞬态——加载时统一派生为 ready
            station._state = HubStation.StationState.READY


# 货舱站点条件检查
func _check_cargo_bay_condition() -> bool:
    return room_exists(&"cargo_hold")
```

### Departure Snapshot for Continuity Verification

```text
# 出航前保存关键状态摘要——返航后用于验证 R5 连续性
var _departure_snapshot: Dictionary = {}

func _capture_departure_snapshot() -> void:
    _departure_snapshot = {
        "modules": _get_current_module_states(),
        "storage_used": ResourcesManager.get_storage_used_volume(),
        "cargo_used": ResourcesManager.get_cargo_bay_usage().get("used_volume", 0),
        "hull_repair_count": _get_repair_trace_count(),
    }

func _get_current_module_states() -> Dictionary:
    var states: Dictionary = {}
    for slot_id in _module_slot_state:
        states[slot_id] = _module_slot_state[slot_id]
    return states
```

### Arrival Animation Contract

```text
func _play_arrival_animation() -> void:
    # 视觉/Feel 类型——具体动画由 AnimationPlayer 驱动
    # Hub 仅负责：
    # 1. 触发动画播放
    # 2. 动画完成后发射 arrival_completed 信号
    # 3. arrival_completed → _transition_docking(LANDED)
    _arrival_animation_player.play("arrival_sequence")
    # AnimationPlayer 的 animation_finished 信号连接到 _on_arrival_animation_completed
```

### Snapshot Corruption Fallback

```text
func _load_from_snapshot(snapshot: Dictionary) -> bool:
    if snapshot.is_empty() or not _validate_snapshot(snapshot):
        push_warning("progress.airship 快照损坏或为空——使用安全默认状态")
        _apply_safe_defaults()
        return false

    _apply_snapshot_state(snapshot)
    return true


func _validate_snapshot(snapshot: Dictionary) -> bool:
    # 验证必需字段存在且类型正确
    var required_fields: Array = ["docking_state", "module_slot_state", "trace_anchors"]
    for field in required_fields:
        if not snapshot.has(field):
            return false
    return true


func _apply_safe_defaults() -> void:
    docking_state = DockingState.LANDED
    _module_slot_state.clear()
    _trace_anchors.clear()
    _derive_all_station_states()
    _spawn_player_at_helm()  # 安全默认——舵轮附近甲板中心
    UIManager.show_warning("存档数据异常，已使用默认状态恢复")
```

---

## Out of Scope

- 抵达动画的具体视觉表现（属于 Visual/Feel 类型——截图+sign-off 验证）
- 航行中事件对货舱/模块的具体影响计算（属于 Navigation #10 和 Modules #8）
- 痕迹锚点的具体视觉 tier 切换（属于 Story 006 的逻辑 + Visual/Feel 类型）
- 存档快照的完整 Schema 定义（属于 Story 008）
- 舱门进入动画的相机过渡（属于 Visual/Feel 类型）

---

## QA Test Cases

- **AC-1 through AC-3**: Arrival → Landed transition
  - Given: in_transit, 返航触发 → arrival (移动冻结, 动画播放) → 动画完成 → landed (移动恢复)
  - Edge case: arrival 动画中不可移动

- **AC-8**: Return spawn point
  - Given: arrival → landed 完成
  - When: 玩家生成
  - Then: position = door_spawn_point，第一视角面对舱门

- **AC-10**: Post-load station state derivation
  - Given: 快照中 module_slot_state = {cargo_module: installed}
  - When: _derive_all_station_states()
  - Then: cargo-bay = ready（条件满足）, 所有站点 state = ready（busy 不持久化）

- **AC-12**: Arrival + suspend_requested edge case
  - Given: arrival 动画播放中, suspend_requested 触发
  - When: best-effort 存档
  - Then: docking_state 捕获为 in_transit（arrival 瞬态不持久化）
  - When: 重载
  - Then: 重新触发 arrival → landed

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/integration/hub/ArrivalFlowTest.csproj` — must exist and pass
**Status**: [x] Created and passing

---

## Dependencies

- Depends on: Story 001 (state machine — arrival/landed transitions), Story 003 (module slot state for station derivation), Story 004 (departure flow — what happens before arrival), resources-goods-capacity Epic (storage/cargo state), modules-hull-state Epic (module slot state), world-repair Epic (repair traces)
- Unlocks: Story 008 (arrival state captured in snapshot), Story 007 (arrival_completed signal)
