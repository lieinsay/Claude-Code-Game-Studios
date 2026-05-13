# Story 005: Snapshot Validation & Persistence

> **Epic**: Chart / Route Planning
> **Status**: Done
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/chart-route-planning.md`
**Requirement**: `TR-chart-001`, `TR-chart-003`

**ADR Governing Implementation**: ADR-0008 (Chart Route State Machine — Formula 4 snapshot_package_validity + Section 6 progress.routes Domain Serializer), ADR-0003 (Save System — Canonical JSON, SnapshotPackage API)
**ADR Decision Summary**: Chart 注册 domain serializer 给 Persistence，遵循 ADR-0003 SnapshotPackage 格式。progress.routes 快照包含：last_committed_route_id (StringName→String), departure_state (StringName→String), active_filter ("hide_rumored"/"show_all"), last_departure_timestamp (float), hide_rumored (bool)。快照 schema_version=1, content_domain_versions={routes:1, intel:1}。恢复时若 departure_state=DEPARTURE_CONFIRMED，跳过渲染直接加载出航锁定序列后移交 Navigation。Formula 4 (snapshot_package_validity) 校验：domain_id 匹配、必需字段存在、departure_state 为 DEPARTURE_CONFIRMED、时间戳有效（非 NaN/±∞/未来）、route_id 在注册表中存在。校验失败返回 violations 列表——存档系统必须拒绝写入。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 快照仅存储独立变量——不含派生值（visible_routes 在恢复后由 open_chart() 重新计算）；StringName key 序列化时转为 String；快照写入前必须通过 snapshot_package_validity() 校验
- Forbidden: 快照包含 Node/Resource/Callable/Signal 引用；使用 store_var()/get_var() Variant blob；departure_state 为非 DEPARTURE_CONFIRMED 的快照
- Guardrail: 快照 Schema 验证——缺失必需字段时拒绝写入并回退至安全状态；stale route_id 在注册表中不存在时校验失败

---

## Acceptance Criteria

### Snapshot Schema

- [ ] **AC-1**: GIVEN 出航确认完成，WHEN build_snapshot_package()，THEN 快照 payload 包含：

| Field | Type | Description |
|-------|------|-------------|
| `last_committed_route_id` | String | 最后承诺的航线 ID（StringName→String） |
| `departure_state` | String | 必须为 "DEPARTURE_CONFIRMED" |
| `active_filter` | String | "hide_rumored" 或 "show_all" |
| `last_departure_timestamp` | float | 出航确认时的 Unix 时间戳 |
| `hide_rumored` | bool | 筛选器状态 |

- [ ] **AC-2**: GIVEN 快照，WHEN 检查内容，THEN 不含 _visible_routes（派生自 Registry + hide_rumored）、_route_states（派生自知识查询）、_selected_route_id（会话状态，恢复后无选中）

### Formula 4 — snapshot_package_validity (Valid Case)

- [ ] **AC-3**: GIVEN 合法快照包（domain_id="progress.routes", departure_state="DEPARTURE_CONFIRMED", last_committed_route_id 在注册表中存在, timestamp 有限且非未来），WHEN snapshot_package_validity(pkg, current_time, tolerance, route_registry)，THEN return {valid: true, violations: []}

### Formula 4 — Violation Cases

- [ ] **AC-4**: GIVEN pkg==null 或 payload 非 Dictionary，THEN violations=["malformed snapshot package"]
- [ ] **AC-5**: GIVEN pkg.domain_id != "progress.routes"，THEN violations=["wrong domain_id"]
- [ ] **AC-6**: GIVEN payload 缺少 last_committed_route_id/departure_state/active_filter/last_departure_timestamp 任一，THEN violations=["missing field: <field>"]
- [ ] **AC-7**: GIVEN departure_state != "DEPARTURE_CONFIRMED"，THEN violations=["invalid departure_state"]
- [ ] **AC-8**: GIVEN last_departure_timestamp 为 NaN 或 ±Inf，THEN violations=["non-finite timestamp"]
- [ ] **AC-9**: GIVEN last_departure_timestamp <= 0（epoch 或未初始化），THEN violations=["timestamp is epoch or uninitialized"]
- [ ] **AC-10**: GIVEN last_departure_timestamp > current_time + timestamp_tolerance（未来时间戳），THEN violations=["timestamp in future"]
- [ ] **AC-11**: GIVEN last_committed_route_id 不在 Registry.list_by_kind("route") 中（stale ID），THEN violations=["route_id not found in registry: <route_id>"]

### SnapshotPackage.is_valid() Integration

- [ ] **AC-12**: GIVEN snapshot_schema_version <= 0，WHEN pkg.is_valid() 返回 false，THEN violations=["invalid snapshot_schema_version"]
- [ ] **AC-13**: GIVEN content_domain_versions 为空，WHEN pkg.is_valid() 返回 false，THEN violations=["empty content_domain_versions"]
- [ ] **AC-14**: GIVEN domain_state != DOMAIN_READY，WHEN pkg.is_valid() 返回 false，THEN violations=["domain_state not READY: <state>"]

### Domain Serializer Registration

- [ ] **AC-15**: GIVEN Chart Phase 3b core_data_ready，WHEN 注册 domain serializer，THEN Persistence.register_domain_serializer("progress.routes", _serialize_routes) 调用。提供 build/restore 方法

### Save/Restore Roundtrip

- [ ] **AC-16**: GIVEN departure_state=DEPARTURE_CONFIRMED + last_committed_route_id=sky-reef-arc-01 + hide_rumored=false + timestamp=999.0，WHEN 序列化→Canonical JSON→反序列化→恢复，THEN 所有值一致。hide_rumored=false 正确恢复

### Restore: DEPARTURE_CONFIRMED Path

- [ ] **AC-17**: GIVEN 快照 departure_state=DEPARTURE_CONFIRMED，WHEN 恢复，THEN chart_state→DEPARTURE_CONFIRMED。departure_lock_remaining=0.0（立即移交 Navigation）。不渲染航图 UI——玩家在出航过程中恢复

### Stale route_id Handling

- [ ] **AC-18**: GIVEN 快照 last_committed_route_id 在注册表中已不存在，WHEN 校验，THEN valid: false → 存档系统拒绝写入。恢复时回退至上一个有效快照或初始状态

---

## Implementation Notes

### Snapshot Builder

```text
func _build_snapshot_package(route_id: StringName) -> SnapshotPackage:
    var pkg := SnapshotPackage.new()
    pkg.domain_id = &"progress.routes"
    pkg.snapshot_schema_version = 1
    pkg.content_domain_versions = {&"routes": 1, &"intel": 1}
    pkg.stable_id_refs = [route_id] if not route_id.is_empty() else []
    pkg.payload = {
        "last_committed_route_id": str(route_id),
        "departure_state": "DEPARTURE_CONFIRMED",
        "active_filter": "hide_rumored" if _state["_hide_rumored"] else "show_all",
        "last_departure_timestamp": _state["_last_departure_timestamp"],
        "hide_rumored": _state["_hide_rumored"],
    }
    pkg.domain_state = SnapshotPackage.DOMAIN_READY
    pkg.domain_error_code = ""
    pkg.migration_hint = ""
    return pkg
```

### Formula 4 — snapshot_package_validity

```text
func snapshot_package_validity(pkg: SnapshotPackage, current_time: float,
                                timestamp_tolerance: float, route_registry: Array) -> Dictionary:
    var violations: Array = []

    if pkg == null or typeof(pkg.payload) != TYPE_DICTIONARY:
        violations.append("malformed snapshot package")
        return {"valid": false, "violations": violations}

    if not is_finite(current_time):
        violations.append("non-finite current_time")

    if pkg.domain_id != &"progress.routes":
        violations.append("wrong domain_id")

    # SnapshotPackage.is_valid() 各条件
    if not pkg.is_valid():
        violations.append("SnapshotPackage.is_valid() returned false")
        if pkg.snapshot_schema_version <= 0:
            violations.append("invalid snapshot_schema_version")
        if pkg.content_domain_versions.is_empty():
            violations.append("empty content_domain_versions")
        if pkg.domain_state != SnapshotPackage.DOMAIN_READY:
            violations.append("domain_state not READY: " + str(pkg.domain_state))

    var payload: Dictionary = pkg.payload
    var required: Array[StringName] = [
        &"last_committed_route_id",
        &"departure_state",
        &"active_filter",
        &"last_departure_timestamp"
    ]
    for field in required:
        if not field in payload:
            violations.append("missing field: " + str(field))

    if payload.get("departure_state") != "DEPARTURE_CONFIRMED":
        violations.append("invalid departure_state")

    var ts: float = payload.get("last_departure_timestamp", 0.0)
    if not is_finite(ts):
        violations.append("non-finite timestamp")
    elif ts <= 0.0:
        violations.append("timestamp is epoch or uninitialized")
    elif ts > current_time + timestamp_tolerance:
        violations.append("timestamp in future")

    var route_id_str: String = payload.get("last_committed_route_id", "")
    var route_id := StringName(route_id_str)
    if route_id_str.is_empty() or route_id not in route_registry:
        violations.append("route_id not found in registry: " + route_id_str)

    if violations.size() > 0:
        return {"valid": false, "violations": violations}
    return {"valid": true, "violations": []}
```

### Domain Serializer

```text
func register_serializer() -> void:
    Persistence.register_domain_serializer("progress.routes", _serialize_routes)

func _serialize_routes() -> SnapshotPackage:
    # 仅在 DEPARTURE_CONFIRMED 时写入快照
    var route_id: StringName = _state["_last_committed_route_id"]
    return _build_snapshot_package(route_id)

func _deserialize_routes(snapshot: SnapshotPackage) -> void:
    var payload: Dictionary = snapshot.payload
    _state["_last_committed_route_id"] = StringName(payload.get("last_committed_route_id", ""))
    _state["_last_departure_timestamp"] = payload.get("last_departure_timestamp", 0.0)
    _state["_hide_rumored"] = payload.get("hide_rumored", false)

    var departure_state: StringName = StringName(payload.get("departure_state", "LOADING"))
    if departure_state == &"DEPARTURE_CONFIRMED":
        _state["_chart_state"] = &"DEPARTURE_CONFIRMED"
        _state["_departure_lock_remaining"] = 0.0
        _resume_departure_sequence()
    else:
        _state["_chart_state"] = &"LOADING"
```

### Resume Departure Sequence

```text
func _resume_departure_sequence() -> void:
    # 恢复出航序列——跳过航图渲染，直接移交 Navigation
    var route_id: StringName = _state["_last_committed_route_id"]
    if route_id.is_empty():
        _state["_chart_state"] = &"LOADING"
        return
    # 重新发射 route_committed 以触发 Navigation 航行上下文构建
    var route_data: Dictionary = Registry.get_route_data(route_id)
    var accessibility: Dictionary = _query_route_accessibility(route_id)
    route_committed.emit(
        route_id,
        route_data.get("destination_location_id", &""),
        accessibility.get("hazard_tags", [])
    )
```

---

## Out of Scope

- Canonical JSON sorted-keys 辅助函数——属于存档系统 #3
- Staging→Verify→Promotion 工作流——属于存档系统 #3
- 版本迁移框架——属于存档系统 #3
- _resume_departure_sequence() 后的场景过渡——属于 Navigation #10 + UI #16
- 其他领域（Hub、Modules、Intel）的 progress.* 快照 Schema——各领域自行定义

---

## QA Test Cases

- **AC-3**: Valid snapshot
  - Given: 合法快照包 → snapshot_package_validity() → {valid: true, violations: []}

- **AC-4 through AC-11**: All violation types
  - Given: null pkg → "malformed snapshot package"
  - Given: wrong domain_id → "wrong domain_id"
  - Given: missing field → "missing field: <name>"
  - Given: invalid departure_state → "invalid departure_state"
  - Given: NaN timestamp → "non-finite timestamp"
  - Given: epoch timestamp → "timestamp is epoch or uninitialized"
  - Given: future timestamp → "timestamp in future"
  - Given: stale route_id → "route_id not found in registry"

- **AC-16**: Roundtrip
  - Given: 构建→序列化→JSON→反序列化→恢复 → 所有值一致
  - Verify: StringName↔String 转换正确，hide_rumored bool 正确

- **AC-17**: DEPARTURE_CONFIRMED restore
  - Given: departure_state=DEPARTURE_CONFIRMED → 恢复 → chart_state=DEPARTURE_CONFIRMED → lock_remaining=0.0 → 立即移交

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/chart/snapshot/ChartSnapshotPersistenceTest.csproj` — must exist and pass
**Status**: [x] 42/42 PASS — 2026-05-13；Epic #9 复审通过 — 2026-05-13

---

## Dependencies

- Depends on: Story 003 (departure state, _last_committed_route_id, _last_departure_timestamp), local-save-persistence Epic (SnapshotPackage API, register_domain_serializer, Canonical JSON), content-registry Epic (route registry for stale ID validation)
- Unlocks: Story 006 (get_filter_state for UIManager), Story 008 (EC-9/10/11 save/load edge cases)
