# Story 008: Scene Persistence & Transition Lifecycle

> **Epic**: Airship Hub
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-001`

**ADR Governing Implementation**: ADR-0001 (Autoload Boot Order — Hub Scene Phase 4 scene_ready, ResourceLoader 不卸载), ADR-0003 (Save System — progress.airship 快照包, Canonical JSON, Staging→Verify→Promotion), ADR-0019 (desktop lifecycle constraints — 单线程, suspend_requested best-effort)
**ADR Decision Summary**: Hub 场景在前往航图/探索期间保持内存驻留（ResourceLoader 不卸载），仅切换活动场景。progress.airship 快照包仅存储独立变量（module_slot_state、trace_anchors、departure_snapshot），room_state 和 station_state 在加载时重新派生——不双重存储。departure_locked 快照降级为 landed。JSON 序列化使用 Canonical JSON（sorted keys, NFC, finite floats only, StringName→String 转换）。快照损坏时使用安全默认状态。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: progress.airship 快照包仅存储独立变量——room_state 和 station_state 加载时重新派生；departure_locked 快照降级为 landed；StringName 序列化时转为 String、反序列化转回 StringName
- Forbidden: 双重存储（派生状态与源数据同时持久化）；store_var()/get_var() Variant blob 作为存档格式；在 departure_locked/in_transit/arrival 瞬态触发完整存档
- Guardrail: 快照损坏→安全默认状态+警告提示；场景加载超时→保留 departure_locked→恢复 landed；desktop suspend_requested 在 20ms budget 内完成 best-effort flush

---

## Acceptance Criteria

### progress.airship Snapshot Schema

- [ ] **AC-1**: GIVEN Hub 需要持久化状态，WHEN 构建 progress.airship 快照包，THEN 包含以下字段（仅独立变量）：

| Field | Type | Description |
|-------|------|-------------|
| `docking_state` | int | 停靠状态（仅持久化 landed/in_transit；departure_locked/arrival 瞬态不持久化） |
| `module_slot_state` | Dict[String, int] | 模块槽状态镜像（key 为 String，非 StringName） |
| `trace_anchors` | Dict[int, Dict] | 4 个痕迹锚点的 tier 值 |
| `last_departure_mode` | String | 最近一次出航模式（"chart"/"direct"/""） |
| `last_departure_route` | String | 最近一次出航路线 ID |
| `departure_snapshot` | Dict | 出航前状态摘要（用于 R5 连续性验证） |
| `spawn_reason` | int | 生成点原因（用于确定加载后玩家位置） |

- [ ] **AC-2**: GIVEN progress.airship 快照，WHEN 检查内容，THEN 不包含 room_state（派生自 module_slot_state + room_exists()）和 station_state（派生自 _check_conditions() + 加载时统一为 ready）——不双重存储

### Scene Memory-Resident Lifecycle

- [ ] **AC-3**: GIVEN Hub 场景已加载 + 玩家出航（Mode A 或 B），WHEN SceneTree 切换到航图/自由飞行场景，THEN Hub 场景保持内存驻留——ResourceLoader 不卸载 Hub 场景资源
- [ ] **AC-4**: GIVEN 玩家返航，WHEN SceneTree 切回 Hub 场景，THEN Hub 场景无需完整重载——直接切换到已驻留的场景实例（避免 2-5s 重载等待）

### Save Boundary Rules

- [ ] **AC-5**: GIVEN docking_state = landed，WHEN 存档系统触发 progress.airship 保存，THEN 完整快照被序列化并写入
- [ ] **AC-6**: GIVEN docking_state = departure_locked + 存档触发，WHEN 快照构建，THEN docking_state 降级为 landed——departure_locked 不是稳定边界，不持久化
- [ ] **AC-7**: GIVEN docking_state = in_transit，WHEN 存档触发，THEN 快照中 docking_state = in_transit——作为稳定航行状态可持久化
- [ ] **AC-8**: GIVEN docking_state = arrival，WHEN suspend_requested/存档触发，THEN 快照中 docking_state = in_transit——arrival 是瞬态动画，不持久化

### Snapshot Restoration

- [ ] **AC-9**: GIVEN progress.airship 快照 docking_state = landed，WHEN Hub 从快照加载，THEN 恢复为 landed——module_slot_state 和 trace_anchors 恢复快照值，station_state 重新派生
- [ ] **AC-10**: GIVEN 快照 docking_state = in_transit，WHEN Hub 加载，THEN 重新触发 arrival → landed 转换（玩家返回 Hub，抵达动画播放，生成在舱门）
- [ ] **AC-11**: GIVEN 快照 docking_state = departure_locked（手动构造的测试夹具或 suspend_requested 捕获的瞬态），WHEN Hub 加载，THEN 降级为 landed——日志记录 "departure_locked 降级为 landed"，出航确认丢失

### Snapshot Corruption & Safe Defaults

- [ ] **AC-12**: GIVEN progress.airship 快照缺少必需字段或 JSON 解析失败，WHEN Hub 加载，THEN 使用安全默认状态——所有站点 ready、生成点甲板中心、痕迹锚点初始值、docking_state = landed——并显示警告提示
- [ ] **AC-13**: GIVEN 快照中 module_slot_state 包含已不存在的模块 ID（stale ID），WHEN 加载验证，THEN 跳过该条目并记录警告——不因单条 stale 数据拒绝整个快照

### Scene Transition Atomicy

- [ ] **AC-14**: GIVEN departure_locked + 航图场景加载中，WHEN 加载超时（duration × 3），THEN 保留 departure_locked → 显示错误提示 → 恢复 landed——Hub 场景未卸载
- [ ] **AC-15**: GIVEN 返航 + Hub 场景已在内存中，WHEN 切换回 Hub，THEN 切换在 <500ms 内完成（无重载开销）——满足 ADR-0001 的场景过渡预算

### StringName↔String Serialization

- [ ] **AC-16**: GIVEN module_slot_state 字典的 key 为 StringName（如 `&"cargo_module"`），WHEN 序列化到 Canonical JSON，THEN key 转换为 String（`"cargo_module"`）
- [ ] **AC-17**: GIVEN 快照 JSON 中 module_slot_state 的 key 为 String，WHEN 反序列化恢复，THEN key 转换回 StringName——Hub 内部始终使用 StringName

### Desktop Lifecycle Integration

- [ ] **AC-18**: GIVEN 桌面窗口 suspend_requested 事件触发，WHEN best-effort 存档执行，THEN progress.airship 序列化+flush 在 ≤20ms budget 内完成——若超时则部分写入由 backup 故障转移保护

---

## Implementation Notes

### progress.airship Snapshot Builder

```text
# HubManager 提供快照构建接口——由存档系统 #3 调用
func build_progress_airship_snapshot() -> Dictionary:
    var snapshot: Dictionary = {}

    # docking_state: 瞬态降级
    match docking_state:
        DockingState.DEPARTURE_LOCKED, DockingState.ARRIVAL:
            snapshot["docking_state"] = DockingState.LANDED if docking_state == DockingState.DEPARTURE_LOCKED else DockingState.IN_TRANSIT
        _:
            snapshot["docking_state"] = docking_state

    # 模块槽状态——StringName → String for JSON
    snapshot["module_slot_state"] = _serialize_module_slot_state()

    # 痕迹锚点
    snapshot["trace_anchors"] = _build_trace_anchor_snapshot()

    # 出航上下文
    snapshot["last_departure_mode"] = str(_last_departure_mode) if _last_departure_mode else ""
    snapshot["last_departure_route"] = str(_last_departure_route) if _last_departure_route else ""

    # 出航前状态摘要（R5 连续性验证）
    snapshot["departure_snapshot"] = _departure_snapshot.duplicate()

    # 生成点原因
    snapshot["spawn_reason"] = _spawn_reason

    return snapshot
```

### Snapshot Restoration

```text
func restore_from_progress_airship(snapshot: Dictionary) -> bool:
    if not _validate_snapshot_schema(snapshot):
        push_error("progress.airship 快照 Schema 验证失败——使用安全默认状态")
        _apply_safe_defaults()
        return false

    # 恢复模块槽状态——String → StringName
    _restore_module_slot_state(snapshot.get("module_slot_state", {}))

    # 恢复痕迹锚点
    _restore_trace_anchors(snapshot.get("trace_anchors", {}))

    # 恢复出航上下文
    _last_departure_mode = StringName(snapshot.get("last_departure_mode", "")) if snapshot.get("last_departure_mode", "") else &""
    _last_departure_route = StringName(snapshot.get("last_departure_route", "")) if snapshot.get("last_departure_route", "") else &""

    # 恢复出航前状态摘要
    _departure_snapshot = snapshot.get("departure_snapshot", {}).duplicate()

    # 恢复 docking_state —— 处理瞬态降级
    var saved_state: int = snapshot.get("docking_state", DockingState.LANDED)
    _spawn_reason = snapshot.get("spawn_reason", SpawnReason.SAVE_LOAD)

    match saved_state:
        DockingState.LANDED:
            _transition_docking(DockingState.LANDED)
            _derive_all_station_states()
        DockingState.IN_TRANSIT:
            # 航行中存档——重载时触发返航
            _transition_docking(DockingState.ARRIVAL)
        _:
            # departure_locked 或其他非法值——降级
            push_warning("progress.airship docking_state=%d 降级为 landed" % saved_state)
            _transition_docking(DockingState.LANDED)
            _derive_all_station_states()

    return true
```

### StringName↔String Conversion

```text
func _serialize_module_slot_state() -> Dictionary:
    var serialized: Dictionary = {}
    for slot_id in _module_slot_state:
        serialized[str(slot_id)] = _module_slot_state[slot_id]
    return serialized


func _restore_module_slot_state(serialized: Dictionary) -> void:
    _module_slot_state.clear()
    for key_str in serialized:
        var slot_id := StringName(key_str)

        # 验证 slot ID 仍存在于注册表中（stale ID 检测）
        if not ContentRegistry.is_valid_slot_id(slot_id):
            push_warning("module_slot_state 快照包含 stale slot ID: %s ——跳过" % key_str)
            continue

        _module_slot_state[slot_id] = serialized[key_str]
```

### Snapshot Schema Validation

```text
func _validate_snapshot_schema(snapshot: Dictionary) -> bool:
    if snapshot.is_empty():
        return false

    var required_fields: Array = [
        "docking_state",
        "module_slot_state",
        "trace_anchors",
        "spawn_reason",
    ]

    for field in required_fields:
        if not snapshot.has(field):
            push_error("progress.airship 快照缺少必需字段: %s" % field)
            return false

    # 类型验证
    if not (typeof(snapshot["docking_state"]) == TYPE_INT):
        return false
    if not (typeof(snapshot["module_slot_state"]) == TYPE_DICTIONARY):
        return false

    return true


func _validate_stale_ids() -> void:
    # 清理 stale module ID
    var stale_slots: Array = []
    for slot_id in _module_slot_state:
        if not ContentRegistry.is_valid_slot_id(slot_id):
            stale_slots.append(slot_id)

    for slot_id in stale_slots:
        push_warning("移除 stale module slot: %s" % slot_id)
        _module_slot_state.erase(slot_id)
```

### Scene Transition Atomicy

```text
# SceneTree 场景切换——Hub 场景保持驻留
func _switch_to_navigation_scene(target_scene_path: String, mode: StringName) -> void:
    # 1. 确保 Hub 场景不被卸载
    var hub_scene: Node = get_tree().current_scene

    # 2. 加载目标场景（异步或同步）
    var target_scene: PackedScene = ResourceLoader.load(target_scene_path)
    if target_scene == null:
        push_error("目标场景加载失败: %s" % target_scene_path)
        _on_scene_load_failed(mode)
        return

    # 3. 切换活动场景——Hub 保留在内存中
    # Godot 4.x: change_scene_to_file() 会卸载当前场景
    # 需要在切换前手动将 Hub 从 scene tree 临时移除并保存引用
    get_tree().root.remove_child(hub_scene)

    var new_scene: Node = target_scene.instantiate()
    get_tree().root.add_child(new_scene)
    get_tree().current_scene = new_scene

    # Hub 场景引用保存在 HubManager Autoload 中
    _cached_hub_scene = hub_scene


func _return_to_hub_scene() -> void:
    if _cached_hub_scene == null:
        push_error("Hub 场景引用丢失——需要完整重载")
        _reload_hub_scene()
        return

    var current_scene: Node = get_tree().current_scene
    get_tree().root.remove_child(current_scene)
    current_scene.queue_free()

    get_tree().root.add_child(_cached_hub_scene)
    get_tree().current_scene = _cached_hub_scene

    # 触发 arrival flow
    trigger_arrival()
```

### Scene Load Failure Handling

```text
func _on_scene_load_failed(mode: StringName) -> void:
    # 目标场景加载失败——保持 Hub 场景不变
    # Hub 在 departure_locked 中——恢复到 landed
    push_error("场景加载失败 (mode=%s)——恢复 Hub 为 landed" % mode)

    _transition_docking(DockingState.LANDED)
    UIManager.show_error("出航失败：目标场景加载异常，请重试")

    # departure_rejected signal 通知相关系统
    departure_rejected.emit(mode, &"SCENE_LOAD_FAILED")
```

### Desktop suspend_requested Best-Effort Flush

```text
# 在 HubManager._ready() 中注册 桌面生命周期回调
func _register_web_lifecycle() -> void:
    if desktop lifecycle support unavailable:
        return

    # SessionShell receives desktop suspend/focus events
    var js_code: String = """
    document.addEventListener('window_focus_changed', function() {
        if (document.visibilityState === 'hidden') {
            // 通知 Godot 执行 best-effort flush
        }
    });
    """
    # 具体 JS 互操作由存档系统 #3 处理
    # Hub 仅确保在 suspend_requested 前 docking_state 调至稳定值
```

### Domain Serializer Registration

```text
# HubManager 在 Persistence 系统 core_data_ready 时注册领域序列化器
func register_domain_serializer() -> void:
    Persistence.register_domain(&"airship", {
        "build": build_progress_airship_snapshot,
        "restore": restore_from_progress_airship,
        "validate": _validate_snapshot_schema,
    })
```

---

## Out of Scope

- 存档系统的 Staging→Verify→Promotion 工作流——属于 local-save-persistence Epic #3
- Canonical JSON sorted-keys 辅助函数的实现——属于存档系统 #3
- Desktop lifecycle notification handling的具体实现——属于存档系统 #3 + platform-session-shell #2
- SceneTree.change_scene_to_file() vs 手动场景管理的最终方案选择——属于 platform-session-shell #2
- 航图/自由飞行场景的具体加载逻辑——属于 Chart #9 和 Navigation #10
- 版本迁移的完整框架（migration pipeline）——属于存档系统 #3

---

## QA Test Cases

- **AC-1 and AC-2**: Snapshot schema
  - Given: Hub 在 landed 状态，cargo_module=installed, chart_notes tier=1
  - When: build_progress_airship_snapshot()
  - Then: 返回 Dictionary 含 docking_state=0, module_slot_state={"cargo_module": 1}, trace_anchors={0: {tier:1,...}}, 不含 room_state 或 station_state

- **AC-6**: departure_locked save degradation
  - Given: docking_state = DEPARTURE_LOCKED
  - When: build_progress_airship_snapshot()
  - Then: snapshot["docking_state"] = LANDED (0)

- **AC-11**: departure_locked snapshot restoration
  - Given: 手动构造的快照 docking_state = 2 (DEPARTURE_LOCKED)
  - When: restore_from_progress_airship()
  - Then: Hub 初始化为 landed, 日志记录降级

- **AC-16 and AC-17**: StringName↔String roundtrip
  - Given: _module_slot_state = {&"cargo_module": 1, &"scout_module": 1}
  - When: serialize → deserialize
  - Then: _module_slot_state key 恢复为 StringName，值不变

- **AC-12**: Corrupted snapshot
  - Given: snapshot = {} 或缺少 docking_state
  - When: restore_from_progress_airship()
  - Then: return false, _apply_safe_defaults(), UIManager 显示警告

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/hub/PersistenceTransitionTest.csproj` — must exist and pass
**Status**: [x] Created and passing

---

## Dependencies

- Depends on: Story 001-007 (all Hub stories — snapshot captures full Hub state), local-save-persistence Epic (SnapshotPackage API, Canonical JSON, register_domain), platform-session-shell Epic (SceneTree lifecycle, scene switching), content-registry Epic (stable ID validation for stale detection)
- Unlocks: — (final Hub story; all subsequent Hub work references this snapshot Schema)
