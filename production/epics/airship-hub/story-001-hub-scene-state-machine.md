# Story 001: Hub Scene Foundation & Docking State Machine

> **Epic**: Airship Hub
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-001`

**ADR Governing Implementation**: ADR-0001 (Autoload/Scene Boot Order — Hub Scene Phase 4 scene_ready), ADR-0004 (InteractionHandler @abstract)
**ADR Decision Summary**: Hub 场景在 Phase 4 (scene_ready) 加载，HubManager Autoload #7 管理站点注册和交互路由。双层四舱横版剖面布局：上层驾驶舱+生活舱，下层工程舱+货舱，中部楼梯连通。4 态停靠状态机 (landed → departure_locked → in_transit → arrival → landed)。2 种玩家生成点：首航→舵轮附近甲板中心，返航→舱门位置。

**Engine**: Godot 4.6.2 | **Risk**: LOW
**Engine Notes**: SceneTree.change_scene_to_file() 用于场景切换。Hub 场景需在前往航图/探索期间保持内存驻留——ResourceLoader 不卸载 Hub 场景（single-threaded 桌面环境避免返航时 2-5s 完整重载）。使用 Texture Atlas 打包 sprite，draw call ≤ 10。

**Control Manifest Rules (Core layer)**:
- Required: Hub 拥有飞艇内部空间布局、碰撞体、可行走区域；HubManager 为 Autoload #7
- Forbidden: Hub 场景退化为 UI 面板容器——所有交互必须在场景中身体化（R1）
- Guardrail: departure_locked 期间移动冻结 (Rooted)，所有站点 report disabled

---

## Acceptance Criteria

### Scene Structure

- [ ] **AC-1**: GIVEN Hub 场景已加载，WHEN 检查场景树，THEN 双层四舱布局存在——上层驾驶舱+生活舱，下层工程舱+货舱，中部楼梯/梯子连通
- [ ] **AC-2**: GIVEN Hub 场景，WHEN 玩家在飞艇内移动，THEN 碰撞体约束玩家行走范围在舱室内和楼梯区域——不可穿墙或走出船体

### Docking State Machine

- [ ] **AC-3**: GIVEN Hub 场景首次加载，WHEN 初始化完成，THEN docking_state = landed
- [ ] **AC-4**: GIVEN landed 状态，WHEN 玩家自由行走和与站点交互，THEN 所有站点 is_enabled() 根据当前模块/伙伴状态返回正确值
- [ ] **AC-5**: GIVEN landed 状态 + 玩家在舱门确认出航，WHEN 确认完成，THEN docking_state → departure_locked（移动冻结，2s 动画）
- [ ] **AC-6**: GIVEN departure_locked，WHEN 动画完成，THEN docking_state → in_transit（Hub 不可进入，玩家在航图/自由飞行场景）
- [ ] **AC-7**: GIVEN in_transit + 航行/探索结束，WHEN 返航触发，THEN docking_state → arrival（抵达动画播放）
- [ ] **AC-8**: GIVEN arrival，WHEN 抵达动画完成，THEN docking_state → landed（移动恢复）

### departure_locked → landed Fallback

- [ ] **AC-9**: GIVEN departure_locked + 航图系统拒绝出航（无有效航线），WHEN 拒绝原因码返回，THEN docking_state → landed，显示拒绝原因提示，移动恢复
- [ ] **AC-10**: GIVEN departure_locked + 看门狗超时（departure_lock_duration × 3），WHEN 超时触发，THEN docking_state 强制 → landed，解除移动冻结，error 日志记录

### Player Spawn Points

- [ ] **AC-11**: GIVEN Hub 场景首次加载（新游戏），WHEN 玩家生成，THEN 位置在舵轮附近甲板中心——第一视角可见舵轮和窗外云海
- [ ] **AC-12**: GIVEN 玩家完成航行/探索后返航，WHEN arrival → landed 转换完成，THEN 玩家生成在舱门位置——随后可走向舵轮完成停靠

### Scene Lifecycle

- [ ] **AC-13**: GIVEN Hub 场景已加载，WHEN 前往航图/探索场景，THEN Hub 场景保持内存驻留（ResourceLoader 不卸载）——返航时直接切换到已加载的 Hub 场景

---

## Implementation Notes

### Docking State Machine

```text
enum DockingState {
    LANDED,
    DEPARTURE_LOCKED,
    IN_TRANSIT,
    ARRIVAL,
}

var docking_state: int = DockingState.LANDED

func _transition_docking(new_state: int) -> void:
    var old_state: int = docking_state
    if not _can_transition_docking(old_state, new_state):
        push_warning("Invalid docking transition: %d → %d" % [old_state, new_state])
        return

    docking_state = new_state
    match new_state:
        DockingState.LANDED:
            _on_landed(old_state)
        DockingState.DEPARTURE_LOCKED:
            _on_departure_locked()
        DockingState.IN_TRANSIT:
            _on_in_transit()
        DockingState.ARRIVAL:
            _on_arrival()

func _can_transition_docking(from_state: int, to_state: int) -> bool:
    match from_state:
        DockingState.LANDED:
            return to_state == DockingState.DEPARTURE_LOCKED
        DockingState.DEPARTURE_LOCKED:
            return to_state in [DockingState.IN_TRANSIT, DockingState.LANDED]
        DockingState.IN_TRANSIT:
            return to_state == DockingState.ARRIVAL
        DockingState.ARRIVAL:
            return to_state == DockingState.LANDED
        _:
            return false

func _on_landed(from_state: int) -> void:
    # 恢复移动控制、重新启用所有站点交互
    _set_movement_rooted(false)
    if from_state == DockingState.ARRIVAL:
        # 返航后生成在舱门
        _spawn_player_at_door()
    elif from_state == DockingState.DEPARTURE_LOCKED:
        # 出航被拒绝——恢复控制
        pass
    # else: 首次加载——生成在舵轮附近（在 _ready() 中处理）

func _on_departure_locked() -> void:
    _set_movement_rooted(true)
    _disable_all_stations_during_lock()
    _start_departure_lock_timer()
    _start_watchdog_timer()

func _on_in_transit() -> void:
    # Hub 内部不可进入——场景切换由 SceneTree 管理
    pass

func _on_arrival() -> void:
    _set_movement_rooted(true)
    _play_arrival_animation()
```

### Player Spawn Points

```text
enum SpawnReason { FIRST_LOAD, RETURN_FROM_VOYAGE, SAVE_LOAD }

func _get_spawn_position(reason: int) -> Vector2:
    match reason:
        SpawnReason.FIRST_LOAD:
            return _helm_spawn_point  # 舵轮附近甲板中心
        SpawnReason.RETURN_FROM_VOYAGE:
            return _door_spawn_point  # 舱门位置
        SpawnReason.SAVE_LOAD:
            return _door_spawn_point  # 安全默认——舱门（R8 规则）
```

### Departure Lock Timer + Watchdog

```text
var base_lock_duration: float = 2.0
var _lock_timer: float = 0.0
var _watchdog_timer: float = 0.0

func _start_departure_lock_timer() -> void:
    var duration: float = clampi(base_lock_duration, 1.5, 3.0)
    _lock_timer = duration

func _start_watchdog_timer() -> void:
    _watchdog_timer = base_lock_duration * 3.0

func _process(delta: float) -> void:
    if docking_state == DockingState.DEPARTURE_LOCKED:
        _lock_timer -= delta
        if _lock_timer <= 0.0:
            _complete_departure_lock()

        _watchdog_timer -= delta
        if _watchdog_timer <= 0.0:
            push_error("Departure lock watchdog triggered — forcing landed")
            _transition_docking(DockingState.LANDED)
```

### Startup Validation

```text
func _validate_config() -> void:
    if is_nan(base_lock_duration) or base_lock_duration < 1.0 or base_lock_duration > 5.0:
        push_error("base_lock_duration invalid (%.2f) — using default 2.0" % base_lock_duration)
        base_lock_duration = 2.0
```

---

## Out of Scope

- 具体站点的交互逻辑（属于 Story 002）
- 离开模式的具体实装（属于 Story 004）
- 抵达动画的具体视觉表现（属于 Visual/Feel 类型——Story 005 定义逻辑）
- 场景切换的具体过渡实现（属于 Story 008）
- HubManager Autoload 注册（属于 Story 007）

---

## QA Test Cases

- **AC-4 through AC-10**: State machine round-trip
  - Given: landed
  - When: 舱门确认出航 → departure_locked (2s) → in_transit → 返航 → arrival (1-2s) → landed
  - Then: 所有状态转换正确，landed 后移动恢复
  - Edge case: departure_locked 中超时 → watchdog 强制 landed

- **AC-9**: Departure rejected
  - Given: departure_locked, 航图系统返回拒绝
  - When: _on_chart_rejected("NO_VALID_ROUTE")
  - Then: docking_state → landed, 拒绝提示显示, 移动恢复

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/integration/hub/DockingStateMachineTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: platform-session-shell Epic (scene boot order), player-movement-interaction Epic (movement system, Rooted state), content-registry Epic (hub.* ID namespace)
- Unlocks: Story 002-008 (all depend on scene and state machine)
