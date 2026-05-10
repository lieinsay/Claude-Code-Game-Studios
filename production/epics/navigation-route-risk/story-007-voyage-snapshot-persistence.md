# Story 007: Voyage Snapshot Persistence

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-001`, `TR-navigation-002`

**ADR Governing Implementation**: ADR-0010 (progress.voyage snapshot 结构, EncounterContext 序列化), ADR-0003 (Canonical JSON 持久化, StringName↔String 转换)
**ADR Decision Summary**: progress.voyage snapshot 包含完整航行状态用于断点续传和存档恢复。mid-voyage save (IN_PROGRESS) 导出完整快照：route_id, D_accumulated, elapsed_time, N_checks_total, resolved_encounters[], pending_encounters[], revealed_hidden_tags[], hull_integrity_departure, scout_efficiency_snapshot, hull_band_snapshot, voyage_state。读档时若 voyage_state==IN_PROGRESS，从 elapsed_time 恢复计时。读档时若 voyage_state==ARRIVED 但 #6 知识状态未更新（崩溃发生在步骤 (2) 前），检测不一致并重新发送 route_travel_completed。cross-version 存档：已结算遭遇保留为不可变历史，未触发检查使用当前版本遭遇表。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 快照使用 Canonical JSON 序列化（ADR-0003）；StringName 在序列化前转为 String，反序列化后转回 StringName；mid-voyage 存档包含完整航行状态——恢复后计时从 elapsed_time 继续；cross-version 存档保留已结算遭遇不可变
- Forbidden: 使用 store_var()/get_var() Variant blob 存盘；在存档中存储 Node/Resource/Object/Callable 引用
- Guardrail: 快照大小 < 2MB（ADR-0003 约束）；序列化 + SHA-256 p95 < 50ms

---

## Acceptance Criteria

### Mid-Voyage Save (IN_PROGRESS)

- [ ] **AC-1**: GIVEN voyage_state=IN_PROGRESS + elapsed_time=45s + D_accumulated=8 + 3 次已结算遭遇，WHEN _capture_voyage_snapshot()，THEN snapshot 包含：
  - route_id, D_accumulated, elapsed_time, N_checks_total
  - resolved_encounters (完整 Array), pending_encounters (完整 Array)
  - revealed_hidden_tags, hull_integrity_departure
  - scout_efficiency_snapshot, hull_band_snapshot, voyage_state
- [ ] **AC-2**: GIVEN 存档时 StringName 字段（route_id, encounter_type, hazard_tag 等），WHEN 序列化为 Canonical JSON，THEN StringName → String。反序列化时 String → StringName。往返无损

### Mid-Voyage Load (IN_PROGRESS)

- [ ] **AC-3**: GIVEN 存档中 voyage_state=IN_PROGRESS + elapsed_time=45s，WHEN 读档，THEN:
  - 航行从 elapsed_time=45s 恢复计时（不是 0）
  - 进度条显示 45/T_voyage × 100%
  - 所有内部状态恢复：_resolved_encounters, _pending_encounters, _revealed_hidden_tags, _accumulated_damage, _damaged_slots
  - 已触发的遭遇检查不重复——_last_check_time 恢复正确
- [ ] **AC-4**: GIVEN 读档恢复 + IN_PROGRESS，WHEN 后续遭遇检查触发，THEN 使用当前版本的遭遇表——不从存档中恢复旧的表定义。T_voyage 和 T_check 基于当前配置重算

### Terminal State Save & Load

- [ ] **AC-5**: GIVEN voyage_state=ARRIVED + EncounterContext 已构建，WHEN 步骤 (5) 持久化，THEN progress.voyage snapshot 包含完整 encounter_context Dictionary
- [ ] **AC-6**: GIVEN 读档时 voyage_state=ARRIVED + encounter_context 存在，WHEN Exploration 启动，THEN 直接消费存档中的 encounter_context——不重复触发 Navigation。Navigation 保持 IDLE 状态
- [ ] **AC-7**: GIVEN 读档时 voyage_state=ARRIVED 但 #6 知识状态未更新（崩溃发生在步骤 (2) 前），WHEN 检测，THEN 重新发送 route_travel_completed 事件给 #6。re-send 幂等——不会重复推进已更新的知识状态

### Crash Recovery

- [ ] **AC-8**: GIVEN 航行结束写入中途崩溃（步骤 (1) #8 完成，步骤 (2) #6 未完成），WHEN 读档恢复，THEN:
  - voyage_state=ARRIVED（来自步骤 (5) 存档的 encounter_context）
  - 检测到 #6 知识状态与 voyage_result 不一致 → 重发 route_travel_completed
  - 检测到 #8 hull_integrity 与存档中的 accumulated_damage 不一致 → #8 已写入，不重复
- [ ] **AC-9**: GIVEN 崩溃发生在步骤 (1) 前（#8 船体伤害未写入），WHEN 读档恢复，THEN:
  - voyage_state=ARRIVED（来自快照）
  - detected_damage_not_applied → 重写 apply_hull_damage(accumulated_damage) 到 #8
- [ ] **AC-10**: GIVEN 崩溃发生在步骤 (3) 前（voyage_completed 未发射），WHEN 读档恢复，THEN:
  - Exploration 从存档中读取 encounter_context 直接进入 ARRIVING
  - 不依赖 Navigation 重发 voyage_completed——存档路径绕过信号

### Cross-Version Save Compatibility

- [ ] **AC-11**: GIVEN 旧版本存档中包含 10 个已结算遭遇（含旧版 encounter_type），WHEN 读档，THEN 已结算遭遇保留为不可变历史——不因新版本遭遇表变更而丢失/修改
- [ ] **AC-12**: GIVEN 旧版本存档 + mid-voyage（IN_PROGRESS）+ 仍有 3 个未触发检查，WHEN 读档恢复 + 后续检查，THEN 未触发的检查使用当前版本的遭遇表——可能抽取到新版本的遭遇条目
- [ ] **AC-13**: GIVEN 存档中的 encounter_type 在当前版本遭遇表中不存在（已移除的旧条目），WHEN 显示已结算遭遇历史，THEN 保留原始 encounter_type 字符串——不映射到新类型。标记为 "legacy" 来源

### Snapshot Format

- [ ] **AC-14**: GIVEN progress.voyage snapshot，WHEN 序列化为 JSON，THEN 顶层字段顺序：route_id, voyage_result, elapsed_time, resolved_encounters, accumulated_damage, revealed_hidden_tags, hull_band_arrival, damaged_slots, encounter_context。缺失字段在反序列化时填充安全默认值
- [ ] **AC-15**: GIVEN snapshot 在反序列化后，WHEN 所有 StringName 字段验证，THEN 所有 &"..." 字面量正确还原。无 String 残留

### Snapshot Build & Write

- [ ] **AC-16**: GIVEN _persist_voyage_snapshot(ctx) 调用，WHEN 执行，THEN 调用 Persistence.capture_snapshot("progress.voyage", snapshot)。不直接操作文件 I/O——通过 Persistence #3 Autoload
- [ ] **AC-17**: GIVEN Persistence.capture_snapshot 返回失败（如存储满），WHEN 处理，THEN 记录错误日志 + 尝试降级保存（仅存 route_id + voyage_result）。不阻塞航程终态流程——玩家不应因存档失败而卡住

---

## Implementation Notes

### Snapshot Capture

```text
func _capture_voyage_snapshot() -> Dictionary:
    return {
        "route_id": _active_voyage.get("route_id", &""),
        "voyage_result": _voyage_state_to_result(),
        "elapsed_time": _elapsed_time,
        "resolved_encounters": _resolved_encounters.duplicate(true),
        "pending_encounters": _pending_encounters.duplicate(true),
        "accumulated_damage": _accumulated_damage,
        "revealed_hidden_tags": _revealed_hidden_tags.duplicate(true),
        "hull_integrity_departure": _active_voyage.get("hull_integrity_departure", 100),
        "scout_efficiency_snapshot": _active_voyage.get("scout_efficiency", 0.0),
        "hull_band_snapshot": _active_voyage.get("hull_band", &"intact"),
        "voyage_state": _voyage_state,
        "damaged_slots": _damaged_slots.duplicate(true),
        "_snapshot_version": 1,  # 快照 schema 版本——cross-version 兼容性
    }


func _persist_voyage_snapshot(ctx: Dictionary) -> void:
    var snapshot: Dictionary = _capture_voyage_snapshot()
    snapshot["encounter_context"] = ctx

    var result: Dictionary = Persistence.capture_snapshot("progress.voyage", snapshot)
    if not result.get("success", false):
        push_error("Navigation: voyage snapshot persistence failed: %s" % result.get("error", "unknown"))
        _attempt_degraded_save(snapshot)


func _attempt_degraded_save(snapshot: Dictionary) -> void:
    var minimal := {
        "route_id": snapshot.get("route_id", &""),
        "voyage_result": snapshot.get("voyage_result", &""),
        "voyage_state": snapshot.get("voyage_state", &""),
        "encounter_context": snapshot.get("encounter_context", {}),
        "_degraded": true,
    }
    Persistence.capture_snapshot("progress.voyage", minimal)
```

### Snapshot Restoration

```text
func _restore_voyage_from_snapshot(snapshot: Dictionary) -> void:
    var state: StringName = snapshot.get("voyage_state", &"IDLE")

    if state == &"IN_PROGRESS":
        _restore_in_progress_voyage(snapshot)
    elif state == &"ARRIVED" or state == &"FORCED_LANDING":
        _restore_completed_voyage(snapshot)
    # IDLE / RETREATED / ABORTED_PREFLIGHT → 无需恢复航行状态


func _restore_in_progress_voyage(snapshot: Dictionary) -> void:
    _voyage_state = &"IN_PROGRESS"

    # 恢复核心状态
    _elapsed_time = snapshot.get("elapsed_time", 0.0)
    _accumulated_damage = snapshot.get("accumulated_damage", 0)
    _resolved_encounters = _deserialize_encounters(snapshot.get("resolved_encounters", []))
    _pending_encounters = _deserialize_encounters(snapshot.get("pending_encounters", []))
    _revealed_hidden_tags = _deserialize_string_name_array(snapshot.get("revealed_hidden_tags", []))
    _damaged_slots = _deserialize_string_name_array(snapshot.get("damaged_slots", []))

    # 恢复 VoyageContext
    _active_voyage["route_id"] = StringName(snapshot.get("route_id", &""))
    _active_voyage["hull_integrity_departure"] = snapshot.get("hull_integrity_departure", 100)
    _active_voyage["scout_efficiency"] = snapshot.get("scout_efficiency_snapshot", 0.0)
    _active_voyage["hull_band"] = StringName(snapshot.get("hull_band_snapshot", &"intact"))

    # 重算 T_voyage 和 T_check（基于当前配置——不从存档恢复旧值）
    _recalculate_timing_from_restored_state()

    # 恢复 last_check_time 防止重复触发
    if _resolved_encounters.size() > 0:
        var last: Dictionary = _resolved_encounters[-1]
        _last_check_time = last.get("time_offset", 0.0)
    else:
        _last_check_time = 0.0


func _restore_completed_voyage(snapshot: Dictionary) -> void:
    var ctx: Dictionary = snapshot.get("encounter_context", {})
    if ctx.is_empty():
        return

    # 检测 #8 船体伤害是否已写入
    var current_hull: int = ModuleHullManager.get_hull_integrity()
    var expected_hull: int = _active_voyage.get("hull_integrity_departure", 100) - snapshot.get("accumulated_damage", 0)
    if current_hull > expected_hull:
        # 船体伤害未写入——重新应用
        ModuleHullManager.apply_hull_damage(snapshot.get("accumulated_damage", 0))

    # 检测 #6 知识状态是否已更新
    var route_id: StringName = StringName(snapshot.get("route_id", &""))
    var voyage_result: StringName = StringName(snapshot.get("voyage_result", &""))
    if voyage_result == &"arrived":
        var knowledge_state: int = IntelManager.get_route_knowledge_state(route_id)
        if knowledge_state != KNOWLEDGE_VERIFIED:
            IntelManager.notify_route_travel_completed(route_id, voyage_result)

    # Exploration 将通过自己的恢复路径消费 encounter_context
    # Navigation 恢复完成后保持在 IDLE 状态
    _voyage_state = &"IDLE"
```

### Serialization Helpers

```text
func _serialize_for_json(value: Variant) -> Variant:
    if value is StringName:
        return String(value)
    elif value is Array:
        var result: Array = []
        for item in value:
            result.append(_serialize_for_json(item))
        return result
    elif value is Dictionary:
        var result: Dictionary = {}
        for key in value:
            result[_serialize_for_json(key)] = _serialize_for_json(value[key])
        return result
    return value


func _deserialize_string_name_array(raw: Array) -> Array[StringName]:
    var result: Array[StringName] = []
    for item in raw:
        result.append(StringName(item) if item is String else item)
    return result


func _deserialize_encounters(raw: Array) -> Array:
    var result: Array = []
    for entry in raw:
        var deserialized: Dictionary = {}
        for key in entry:
            var value = entry[key]
            # encounter_type, hazard_tag, special_effect_tags 转为 StringName
            if key in ["encounter_type", "hazard_tag", "voyage_result"]:
                deserialized[key] = StringName(value) if value is String else value
            elif key == "special_effect_tags":
                deserialized[key] = _deserialize_string_name_array(value)
            else:
                deserialized[key] = value
        result.append(deserialized)
    return result
```

### Persistence Integration

```text
# Navigation 在 _finalize_voyage() 步骤 (5) 中调用
func _persist_voyage_snapshot(ctx: Dictionary) -> void:
    var snapshot := _capture_voyage_snapshot()
    snapshot["encounter_context"] = ctx
    Persistence.capture_snapshot("progress.voyage", snapshot)

# Navigation 在 session restore 时被 Persistence 调用
func restore_from_snapshot(snapshot: Dictionary) -> void:
    if snapshot.is_empty():
        return
    _restore_voyage_from_snapshot(snapshot)
```

---

## Out of Scope

- Persistence.capture_snapshot() 的具体实现和 Canonical JSON 序列化——属于 #3 local-save-persistence Epic
- 存档的跨会话持久化（user:// storage 写入/读取）——属于 #3 Persistence
- progress.voyage snapshot 的 schema 迁移系统——属于 #3 Persistence 的存档迁移逻辑
- 存档文件的加密/压缩——属于 #3 Persistence

---

## QA Test Cases

- **AC-1/2**: Mid-voyage save
  - IN_PROGRESS + partial state → full snapshot captured; StringName↔String roundtrip

- **AC-3/4**: Mid-voyage load
  - elapsed_time restored → progress bar correct; no duplicate checks; current encounter table used

- **AC-5/6/7**: Terminal state save/load
  - ARRIVED snapshot contains encounter_context; Exploration reads from save; crash recovery re-sends

- **AC-8/9/10**: Crash recovery — 3 crash points
  - Between (1)/(2): re-send knowledge update
  - Before (1): re-apply hull damage
  - Before (3): Exploration reads from save

- **AC-11/12/13**: Cross-version
  - Old encounters preserved; new checks use current table; legacy types marked

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/navigation/VoyageSnapshotPersistenceTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (VoyageContext, voyage_state), Story 004 (D_accumulated, damaged_slots), Story 005 (resolved_encounters, pending_encounters), Story 006 (EncounterContext, voyage_completed), local-save-persistence Epic (capture_snapshot, restore_from_snapshot), modules-hull-state Epic (get_hull_integrity, apply_hull_damage), intel-knowledge Epic (get_route_knowledge_state, notify_route_travel_completed)
- Unlocks: Story 008 (EC-26/27/28 cross-version save, crash recovery edge cases)
