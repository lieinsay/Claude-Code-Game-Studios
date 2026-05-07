# Story 004: Departure Modes & Confirmation Gate

> **Epic**: Airship Hub
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-002`

**ADR Governing Implementation**: ADR-0001 (Autoload Boot Order — Chart/Navigation 场景切换), ADR-0002 (Signal Protocol — departure_initiated)
**ADR Decision Summary**: Hub 支持两种离开模式——Mode A（舱门→固定航线，安全可预测，积累航线知识）和 Mode B（舵轮→自主飞行，可进入未知但遭遇风险更高，不积累航线知识）。出航确认对话框（R9）展示整备站点检查清单+航线风险评级+货舱容量+模块完好度摘要——所有未访问站点以警告列出并附带软机械后果（5-10%），但确认按钮始终可点击。departure_lock_duration 默认 2.0s (clamped 1.5-3.0)。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: Mode A 舱门→航图系统；Mode B 舵轮→自主飞行界面；确认对话框始终可点击
- Forbidden: 强制玩家访问任意站点才能出航；departure_lock_duration 配置不在 1.0-5.0 范围时使用默认值
- Guardrail: departure_locked 中再次 Use 舱门/舵轮→被阻断为 target_disabled；看门狗超时→强制 landed

---

## Acceptance Criteria

### Mode A: Chart Departure (舱门)

- [ ] **AC-1**: GIVEN landed + 玩家在舱门 anchor_radius 内，WHEN 按 Use，THEN 弹出 Mode A 确认对话框——显示航线名称+风险标注
- [ ] **AC-2**: GIVEN Mode A 确认对话框，WHEN 玩家按确认，THEN docking_state → departure_locked（移动冻结 2s），动画完成后场景切换到航图系统
- [ ] **AC-3**: GIVEN Mode A 确认，WHEN 航图系统无有效航线（拒绝出航），THEN departure_locked → landed，显示拒绝原因提示，移动恢复

### Mode B: Direct Departure (舵轮)

- [ ] **AC-4**: GIVEN landed + 玩家在舵轮 anchor_radius 内，WHEN 按 Use，THEN 弹出 Mode B 确认对话框——显示"自主飞行 — 进入未知区域"警告+当前朝向提示
- [ ] **AC-5**: GIVEN Mode B 确认，WHEN 玩家按确认，THEN docking_state → departure_locked（2s），舵轮特写+船体转向动画，场景切换到自由飞行界面
- [ ] **AC-6**: GIVEN 无已知航线且所有模块为空，WHEN 玩家到舵轮按 Use，THEN Mode B 仍然可用——自主飞行不依赖航线或模块

### R9: Departure Confirmation Dialog

- [ ] **AC-7**: GIVEN 玩家在舱门/舵轮按 Use，WHEN 确认对话框显示，THEN 确认对话框包含：
  - 整备站点检查清单（情报台/伙伴驻点/货舱的已访问/未访问状态）
  - 航线风险评级（Mode A：来自 IntelManager；Mode B："未知"）
  - 货舱剩余容量（来自 ResourcesManager）
  - 模块完好度摘要（正常/有损伤/未安装，逐槽列出）
- [ ] **AC-8**: GIVEN 玩家从未访问任何整备站点，WHEN 确认对话框显示，THEN 所有站点标记为"未访问"并附带软机械后果：
  - 情报台未读→遭遇率+10% + 航线风险"未知"
  - 伙伴驻点未交互→"无侦察简报"
  - 货舱未检查→容量显示"未确认"
  - 模块未检查→效率 95%
- [ ] **AC-9**: GIVEN 确认对话框中所有站点均为"未访问"状态，WHEN 玩家查看确认按钮，THEN 确认按钮始终可点击——不阻断出航
- [ ] **AC-10**: GIVEN 玩家访问了情报台但未访问伙伴驻点，WHEN 确认对话框显示，THEN 情报台="已读"（无遭遇率惩罚），伙伴驻点="未交互"（无侦察简报）——两者以不同视觉层级呈现

### Departure Lock Timer

- [ ] **AC-11**: GIVEN departure_lock_duration=2.0s，WHEN 玩家确认出航（Mode A 或 B），THEN 确认时刻到场景切换的间隔=2.0s±0.1s
- [ ] **AC-12**: GIVEN departure_lock_duration 配置为 NaN 或 <1.0s 或 >5.0s，WHEN Hub 启动验证，THEN 配置被拒绝——使用默认值 2.0s
- [ ] **AC-13**: GIVEN departure_locked + 看门狗超时（duration×3），WHEN 超时触发，THEN 强制恢复 landed，错误日志记录

### Input Race Protection

- [ ] **AC-14**: GIVEN departure_locked，WHEN 玩家再次对舱门/舵轮按 Use，THEN is_enabled() 返回 false——不重复触发出航
- [ ] **AC-15**: GIVEN departure_locked + 玩家连按移动键，THEN 移动系统已 Rooted——所有移动输入被忽略

### Mode B Risk Contract

- [ ] **AC-16**: GIVEN Mode B departure，WHEN 查询 departure_context，THEN 上下文含 `departure_mode="direct"`, `known_route=false`——航行系统据此应用更高遭遇风险倍率
- [ ] **AC-17**: GIVEN Mode B departure，WHEN 航行结束返航，THEN 自主飞行经过的区域不自动转化为已知航线信息

---

## Implementation Notes

### Departure Flow (Mode A — Chart)

```gdscript
func _on_door_use_requested() -> void:
    if docking_state != DockingState.LANDED:
        return

    # 展示 R9 确认对话框（Mode A）
    var confirmed: bool = await _show_departure_confirmation(&"chart")
    if not confirmed:
        return

    # 进入 departure_locked
    _transition_docking(DockingState.DEPARTURE_LOCKED)
    _play_departure_animation(&"door")

    # 等待动画 + timer
    await _wait_departure_lock()

    # 构建 Hub 状态包传递给航图系统
    var state_package: Dictionary = _build_hub_state_package()
    var result: Dictionary = ChartSystem.initiate_departure(state_package)

    if not result.get("accepted", false):
        _transition_docking(DockingState.LANDED)
        _show_rejection_reason(result.get("reason", "未知原因"))
        return

    _transition_docking(DockingState.IN_TRANSIT)
    # SceneTree 切换到航图场景
```

### Departure Flow (Mode B — Direct)

```gdscript
func _on_helm_use_requested() -> void:
    if docking_state != DockingState.LANDED:
        return

    var confirmed: bool = await _show_departure_confirmation(&"direct")
    if not confirmed:
        return

    _transition_docking(DockingState.DEPARTURE_LOCKED)
    _play_departure_animation(&"helm")

    await _wait_departure_lock()

    var state_package: Dictionary = _build_hub_state_package()
    state_package["departure_mode"] = "direct"
    state_package["known_route"] = false

    NavigationSystem.initiate_free_flight(state_package)
    _transition_docking(DockingState.IN_TRANSIT)
```

### R9: Confirmation Dialog Data Assembly

```gdscript
func _build_departure_confirmation_data(mode: StringName) -> Dictionary:
    return {
        "mode": mode,
        "checklist": _build_checklist(),
        "route_risk": _get_route_risk_rating(mode),
        "cargo_capacity": _get_cargo_capacity_display(),
        "module_summary": _get_module_summary(),
    }

func _build_checklist() -> Array:
    var items: Array = []
    # 情报台
    items.append({
        "station": "情报台",
        "visited": _station_visited(&"intel-desk"),
        "warning": "航线风险评级标注"未知" + 本次出航遭遇率 +10%",
    })
    # 伙伴驻点
    items.append({
        "station": "伙伴驻点",
        "visited": _station_visited(&"partner-post"),
        "warning": "不提供本次出航的侦察简报",
    })
    # 货舱
    items.append({
        "station": "货舱",
        "visited": _station_visited(&"cargo-bay"),
        "warning": "出航确认中货舱容量显示"未确认"而非精确数值",
    })
    # 模块
    for slot_id in [&"module_slot_a", &"module_slot_b"]:
        items.append({
            "station": "模块接口 %s" % slot_id,
            "visited": _station_visited(slot_id),
            "warning": "该模块本次运行效率降至 95%",
        })
    return items

func _get_route_risk_rating(mode: StringName) -> String:
    if mode == &"direct":
        return "未知 — 自主飞行"
    if _station_visited(&"intel-desk"):
        return IntelManager.get_route_risk_summary()
    return "未知"
```

### Hub State Package

```gdscript
func _build_hub_state_package() -> Dictionary:
    return {
        "docked_location_id": _current_docked_location,
        "module_slots": _module_slot_state,
        "cargo_summary": ResourcesManager.get_cargo_bay_usage(),
        "storage_summary": ResourcesManager.get_storage_summary(),
        "active_crew": _active_crew,
    }
```

### Uninformed Departure Penalty Tracking

```gdscript
# 记录出航前的站点访问状态——传递给航行系统
func _get_departure_penalties() -> Dictionary:
    var penalties: Dictionary = {}
    if not _station_visited(&"intel-desk"):
        penalties["encounter_rate_bonus"] = 0.10  # +10%
    if not _station_visited(&"module_slot_a") or not _station_visited(&"module_slot_b"):
        penalties["module_efficiency"] = 0.95  # 95%
    return penalties
```

---

## Out of Scope

- 航图系统的地图和路线选择 UI（属于 Chart & Route Planning #9）
- 自主飞行界面/俯视地图的实装（属于 Navigation & Route Risk #10）
- 航线风险的具体计算（属于 Navigation #10 + IntelManager #6）
- 确认对话框的 UI 呈现和动画（属于 UI 系统 #16）
- Mode B 遭遇风险的具体倍率（属于 Navigation #10 定义）
- 软机械后果的长期调优（需玩测数据反馈后调整）

---

## QA Test Cases

- **AC-7 through AC-10**: Confirmation dialog
  - Given: 从未访问任何整备站点
  - When: 舱门 Use → 确认对话框
  - Then: 4 项站点全部标记"未访问"+ 附带后果文本；确认按钮可点击
  - Given: 访问情报台后再到舱门 → 情报台="已读"无惩罚, 其他 3 项仍警告

- **AC-1 and AC-4**: Both modes available
  - Given: landed, 舱门和舵轮同时 anchor_radius 覆盖
  - When: 焦点系统按 priority+距离选择唯一焦点
  - Then: 舱门 Use → Mode A; 舵轮 Use → Mode B; 两者独立不干扰

- **AC-16**: Mode B risk contract
  - Given: Mode B departure
  - When: departure_context built
  - Then: departure_mode="direct", known_route=false

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/integration/hub/departure_modes_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (station registration), Story 003 (module slot state), intel-knowledge Epic (route risk queries), resources-goods-capacity Epic (capacity queries), chart-route-planning Epic (Mode A receiver), navigation-route-risk Epic (Mode B receiver)
- Unlocks: Story 005 (arrival flow after departure), Story 007 (departure_initiated signal)
