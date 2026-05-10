# Story 002: Station Registration & Interaction Routing

> **Epic**: Airship Hub
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-001`

**ADR Governing Implementation**: ADR-0004 (InteractionHandler @abstract — 所有站点继承 InteractionHandler), ADR-0001 (HubManager Autoload #7 管理站点注册)
**ADR Decision Summary**: Hub 中每个可交互对象在 InteractionRegistry 中注册为 Interactable，提供 stable_id、anchor_radius、priority、interaction_type、is_enabled()、get_display_hint()。use_requested 信号到达后 Hub 将交互委派给对应领域系统。10 个 MVP 站点各有明确的 interaction_type 和领域系统归属。站点状态机：ready / busy / disabled——busy 态 UseLocked 由领域系统管理。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 所有站点必须注册在 InteractionRegistry 中，含 stable_id (hub.interactable.*)
- Forbidden: Hub 直接拥有拆包、购买、情报查询、修复或模块管理逻辑——这些必须委派给领域系统
- Guardrail: 站点 is_enabled() 在每次交互请求前重新评估；busy→disabled 转换：当前交互完成后站点降为 disabled

---

## Acceptance Criteria

### Registration Contract

- [ ] **AC-1**: GIVEN Hub 已初始化，WHEN 查询 InteractionRegistry，THEN 返回恰好 10 个 Interactable 注册项
- [ ] **AC-2**: GIVEN 每个注册项，WHEN 检查属性，THEN 含 stable_id（格式 `hub.interactable.[name]`）、anchor_radius、priority、interaction_type、is_enabled()、get_display_hint()

### 10 MVP Stations

- [ ] **AC-3**: GIVEN Hub 场景就绪，WHEN 逐站检查 interaction_type，THEN:

| # | stable_id | interaction_type | 领域系统 |
|---|-----------|-----------------|---------|
| 1 | hub.interactable.intel-desk | read | IntelManager (#6) |
| 2 | hub.interactable.partner-post | talk | PartnerSystem (#15) |
| 3 | hub.interactable.module-slot-a | use | ModulesSystem (#8) |
| 4 | hub.interactable.module-slot-b | use | ModulesSystem (#8) |
| 5 | hub.interactable.storage-shelf | open | ResourcesManager (#5) |
| 6 | hub.interactable.cargo-bay | open | ResourcesManager (#5) |
| 7 | hub.interactable.door | use | ChartSystem (#9) / HubManager |
| 8 | hub.interactable.helm | use | HubManager (自主飞行) |
| 9 | hub.interactable.rest-point | rest | — (MVP 无机械功能) |
| 10 | hub.interactable.repair-point | repair | WorldRepair (#13) / ModulesSystem (#8) |

### Station State Machine

- [ ] **AC-4**: GIVEN 站点初始化为 ready，WHEN 玩家执行 Use 且领域系统返回 UseLocked，THEN 站点状态变为 busy
- [ ] **AC-5**: GIVEN 站点为 busy，WHEN 领域系统完成交互并释放锁，THEN 站点状态恢复 ready
- [ ] **AC-6**: GIVEN 站点为 ready，WHEN 站点条件失效（如模块被摧毁、伙伴离船），THEN 站点变为 disabled——is_enabled() 返回 false，视觉灰化
- [ ] **AC-7**: GIVEN 站点为 disabled，WHEN 站点条件恢复（如模块重新安装），THEN 站点恢复 ready
- [ ] **AC-8**: GIVEN 站点为 busy + 条件失效（如交互进行中模块被摧毁），WHEN 当前交互完成，THEN 站点降为 disabled——下次交互请求时判定为 disabled

### Interaction Routing

- [ ] **AC-9**: GIVEN 玩家在情报台 anchor_radius 内按 Use，WHEN use_requested 信号到达，THEN Hub 将交互委派给 IntelManager 的查询接口——Hub 本身不拥有情报查询逻辑
- [ ] **AC-10**: GIVEN 玩家在仓库货架 anchor_radius 内按 Use，WHEN use_requested 信号到达，THEN Hub 委派给 ResourcesManager 的容量查询——Hub 不拥有拆包/存入逻辑

### get_display_hint()

- [ ] **AC-11**: GIVEN 仓库货架站点，WHEN `get_display_hint()`，THEN 返回含站点名称 + 状态摘要的文本（如 `"仓库 · 920/1000"`）
- [ ] **AC-12**: GIVEN 模块接口 B（货舱模块）为空槽，WHEN `get_display_hint()`，THEN 返回 `"模块接口 · 空槽"`

### Departure Lock Interaction Blocking

- [ ] **AC-13**: GIVEN departure_locked 状态，WHEN 玩家对任意站点按 Use，THEN 站点 is_enabled() 返回 false——Use 被阻断为 target_disabled

---

## Implementation Notes

### HubManager Station Registry

```text
# HubManager Autoload #7 — manages station registration
extends Node

var _stations: Dictionary = {}  # Dict[StringName, HubStation]

func register_station(stable_id: StringName, station: HubStation) -> void:
    _stations[stable_id] = station

func get_station(stable_id: StringName) -> HubStation:
    return _stations.get(stable_id, null)

func get_all_stations() -> Array:
    return _stations.values()
```

### HubStation Base Class

```text
# 每个可交互站点继承此基类
class_name HubStation
extends Node2D

enum StationState { READY, BUSY, DISABLED }

@export var stable_id: StringName
@export var interaction_type: StringName  # "read" / "talk" / "use" / "open" / "rest" / "repair"
@export var anchor_radius: float = 64.0
@export var priority: int = 0

var _state: int = StationState.READY

func is_enabled() -> bool:
    # 检查 departure_locked 状态 + 站点自身条件
    if HubManager.docking_state != HubManager.DockingState.LANDED:
        return false
    return _state == StationState.READY and _check_conditions()

func get_display_hint() -> String:
    # 子类 override 提供站点名称 + 状态摘要
    return _build_hint()

func request_use() -> void:
    if not is_enabled():
        return
    _state = StationState.BUSY
    _on_use()

func release() -> void:
    if _state == StationState.BUSY:
        _state = StationState.READY

func disable() -> void:
    if _state == StationState.BUSY:
        # 在当前交互完成后降级——记录待降级标记
        _pending_disable = true
    else:
        _state = StationState.DISABLED

func enable() -> void:
    if _state == StationState.DISABLED:
        _state = StationState.READY

# 子类 override
func _check_conditions() -> bool:
    return true

func _build_hint() -> String:
    return str(stable_id)

func _on_use() -> void:
    pass  # 子类覆盖——委派给领域系统
```

### Interaction Routing Pattern

```text
# 示例：情报台站点
class_name IntelDeskStation
extends HubStation

func _build_hint() -> String:
    # 从 IntelManager 查询已知情报数量
    var intel_count: int = IntelManager.get_available_intel_count()
    return "情报台 · %d 份可读情报" % intel_count

func _on_use() -> void:
    # 委派给 UI 系统打开情报面板
    UIManager.open_intel_panel()
    # UI 关闭时调用 release()
```

---

## Out of Scope

- 各站点的具体领域逻辑（属于各领域系统）
- UI 面板的具体呈现（属于 UI 系统 #16）
- 视觉高亮/灰化渲染（属于 Visual/Feel 类型——截图+sign-off 验证）
- 无障碍：disabled 状态的形状区分+文本标签（属于 UI 系统 #16）
- R9 出航确认对话框（属于 Story 004）

---

## QA Test Cases

- **AC-4 through AC-8**: Station state machine
  - Given: 仓库货架站点=ready
  - When: 玩家 Use → 领域系统 UseLocked → station=busy → 交互完成 → release() → station=ready
  - When: 货舱模块被摧毁 → station=disabled
  - Edge case: busy 态下条件失效 → 当前交互完成 → release() 时检测 _pending_disable → station=disabled

- **AC-13**: Departure lock blocking
  - Given: departure_locked
  - When: 对所有 10 个站点调用 is_enabled()
  - Then: 全部返回 false

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/integration/hub/StationRegistryTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (Hub Scene, docking state machine), platform-session-shell Epic (InteractionRegistry), content-registry Epic (hub.interactable.* IDs)
- Unlocks: Story 004 (舱门/舵轮交互触发 departure), Story 007 (external integration)
