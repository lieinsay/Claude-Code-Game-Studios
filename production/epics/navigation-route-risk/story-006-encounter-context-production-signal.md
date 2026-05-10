# Story 006: EncounterContext Production & voyage_completed Signal

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-001`

**ADR Governing Implementation**: ADR-0010 (EncounterContext 9 顶层字段, VoyageResult 枚举, voyage_completed 信号合约, fallback context, progress.voyage snapshot)
**ADR Decision Summary**: EncounterContext 是 Navigation (#10) 在航程终态时产出的结构化遭遇数据包，由 Exploration (#11) 消费以生成探索场景。由 Navigation 构建、通过 voyage_completed 信号发射、由 Exploration 消费。所有权遵循"生产者构建、消费者校验、无共享引用"原则。包含 9 个顶层字段：route_id, destination_id, voyage_result, resolved_encounters, accumulated_damage, revealed_hidden_tags, hull_band_arrival, forced_landing_position, damaged_slots。voyage_result 为 ARRIVED/RETREATED/FORCED_LANDING 之一。按 voyage_result 不同，字段有效性有差异（forced_landing_position 仅在 FORCED_LANDING 时非空）。fallback context 在 ctx 为 null/缺字段/无效枚举时构建——不阻塞玩家体验。航行结束写入顺序：(1)#8 hull damage → (2)#6 knowledge update → (3)EncounterContext emit → (4)#17 feedback → (5)#3 persist。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: _build_encounter_context() 在航程终态（_finalize_voyage）中构建；voyage_completed 信号携带完整的 Dictionary[StringName, Variant]；消费方（#11）必须校验字段完整性；forced_landing_position 仅在 FORCED_LANDING 时非空
- Forbidden: 在 voyage_completed 发射后再修改 EncounterContext；跳过消费方验证直接信任传入的 ctx；在终态信号发射后 Navigation 继续参与下游逻辑
- Guardrail: Fallback context 触发时写入 internal_error_log；resolved_encounters 深拷贝——防止信号回调中修改原始数据

---

## Acceptance Criteria

### EncounterContext Construction

- [ ] **AC-1**: GIVEN voyage_state→ARRIVED + 航程中有 5 次已结算遭遇，WHEN _build_encounter_context()，THEN 返回 Dictionary 包含全部 9 个字段。voyage_result="arrived", forced_landing_position=""
- [ ] **AC-2**: GIVEN voyage_state→RETREATED + 撤退前 3 次已结算遭遇，WHEN _build_encounter_context()，THEN resolved_encounters 为截至撤退点的 3 个条目。accumulated_damage 为 3 次伤害之和。voyage_result="retreated"
- [ ] **AC-3**: GIVEN voyage_state→FORCED_LANDING + hull≤0，WHEN _build_encounter_context()，THEN voyage_result="forced_landing", forced_landing_position 非空（迫降位置 ID），hull_band_arrival="destroyed"

### EncounterContext 9 Fields

- [ ] **AC-4**: GIVEN _build_encounter_context() 调用，WHEN 返回 ctx，THEN 包含且类型正确：
  - `route_id`: StringName（航线 ID）
  - `destination_id`: StringName（目的地 ID）
  - `voyage_result`: StringName（"arrived"/"retreated"/"forced_landing"）
  - `resolved_encounters`: Array[Dictionary]（完整遭遇列表）
  - `accumulated_damage`: int（累计船体伤害）
  - `revealed_hidden_tags`: Array[StringName]（航程中新揭示的标签）
  - `hull_band_arrival`: StringName（终态船体波段）
  - `forced_landing_position`: StringName（迫降 ID，非迫降时为空字符串）
  - `damaged_slots`: Array[StringName]（受损模块槽位）

### voyage_completed Signal

- [ ] **AC-5**: GIVEN 航程进入终态 + EncounterContext 构建完成，WHEN _finalize_voyage()，THEN voyage_completed.emit(ctx: Dictionary)。遵循 ADR-0002：typed params, sync emit, emit-after-mutation
- [ ] **AC-6**: GIVEN voyage_completed 已发射，WHEN Navigation 后续操作，THEN Navigation 不再参与下游逻辑——信号发射后本系统关闭。Exploration 恢复场景控制权
- [ ] **AC-7**: GIVEN 消费方接收 voyage_completed(ctx)，WHEN 读取 ctx 字段，THEN 不修改 ctx 内容。深拷贝保障：_build_encounter_context() 中所有 Array 已 .duplicate(true)

### Writing Order Enforcement

- [ ] **AC-8**: GIVEN 航程到达终态，WHEN _finalize_voyage() 执行，THEN 按顺序写入：(1) #8 船体伤害 → (2) #6 路线知识更新 → (3) voyage_completed 信号 → (4) #17 状态变更事件 → (5) #3 存档快照。顺序不可打乱
- [ ] **AC-9**: GIVEN 步骤 (1) #8 写入失败（ModuleHullManager 不可用），WHEN _finalize_voyage()，THEN 记录错误日志，继续执行后续步骤。不因单一系统写入失败而阻塞整套写入流程
- [ ] **AC-10**: GIVEN 步骤 (2) #6 更新时 route_id 无效（极端边缘），WHEN query_route_knowledge 无法找到该 route，THEN 跳过知识更新步骤——无操作。记录警告

### Fallback Context

- [ ] **AC-11**: GIVEN ctx 为 null 或非 Dictionary 类型，WHEN _validate_encounter_context(ctx)，THEN 返回 fallback context。9 字段全为默认安全值，voyage_result="arrived"
- [ ] **AC-12**: GIVEN ctx.route_id 缺失或为空 StringName，WHEN 校验，THEN 返回 fallback context。不尝试修复——降级处理
- [ ] **AC-13**: GIVEN ctx.voyage_result 不是 ["arrived", "retreated", "forced_landing"] 之一，WHEN 校验，THEN 返回 fallback context。无效枚举值不通过
- [ ] **AC-14**: GIVEN ctx.resolved_encounters 不是 Array 类型，WHEN 校验，THEN 返回 fallback context。类型不匹配不通过
- [ ] **AC-15**: GIVEN fallback context 被构建，WHEN 触发原因，THEN internal_error_log 记录具体触发条件和原始 ctx 摘要（不记录完整 ctx——避免日志膨胀）

### Downstream Consumption Interface

- [ ] **AC-16**: GIVEN Exploration (#11) 接收 voyage_completed(ctx)，WHEN _on_voyage_completed()，THEN 调用 _validate_encounter_context(ctx) 校验 → 进入 ARRIVING 阶段。voyage_result="arrived"→正常入口，"forced_landing"→坠机点入口
- [ ] **AC-17**: GIVEN voyage_result="retreated"，WHEN Exploration 消费，THEN 不进入探索阶段——Exploration 将控制权返回 AirshipHub。retreated 航程不生成探索场景

---

## Implementation Notes

### _build_encounter_context()

```text
func _build_encounter_context() -> Dictionary:
    return {
        "route_id": _active_voyage.get("route_id", &""),
        "destination_id": _active_voyage.get("destination_id", &""),
        "voyage_result": _voyage_state_to_result(),
        "resolved_encounters": _resolved_encounters.duplicate(true),
        "accumulated_damage": _accumulated_damage,
        "revealed_hidden_tags": _revealed_hidden_tags.duplicate(true),
        "hull_band_arrival": _get_hull_band(_get_hull_integrity_effective()),
        "forced_landing_position": _forced_landing_position \
            if _voyage_state == &"FORCED_LANDING" else &"",
        "damaged_slots": _damaged_slots.duplicate(true),
    }


func _voyage_state_to_result() -> StringName:
    match _voyage_state:
        &"ARRIVED":
            return &"arrived"
        &"RETREATED":
            return &"retreated"
        &"FORCED_LANDING":
            return &"forced_landing"
    return &""  # 不应到达
```

### voyage_completed Signal Declaration

```text
# Navigation Autoload #10
signal voyage_completed(encounter_context: Dictionary)
# 遵循 ADR-0002: typed params, sync emit, emit-after-mutation
# 消费方: Exploration (#11) — _on_voyage_completed(ctx)
#          Persistence (#3) — 在步骤(5)中读取 ctx 写入 snapshot
```

### _finalize_voyage() with Writing Order

```text
func _finalize_voyage() -> void:
    # 步骤 (1): 写入 #8 船体伤害
    _write_damage_to_hull()

    # 步骤 (2): 写入 #6 路线知识更新
    _update_route_knowledge()

    # 步骤 (3): 构建并发射 EncounterContext
    var ctx: Dictionary = _build_encounter_context()
    voyage_completed.emit(ctx)

    # 步骤 (4): 发射 #17 反馈事件
    _emit_voyage_end_feedback(ctx)

    # 步骤 (5): 持久化快照到 #3
    _persist_voyage_snapshot(ctx)

    # 清理内部状态
    _cleanup_voyage_state()


func _write_damage_to_hull() -> void:
    if _accumulated_damage > 0:
        ModuleHullManager.apply_hull_damage(_accumulated_damage)


func _update_route_knowledge() -> void:
    var route_id: StringName = _active_voyage.get("route_id", &"")
    if route_id == &"":
        return

    # 发射路线旅行完成事件——#6 推进知识状态
    var status: StringName = _voyage_state_to_result()
    IntelManager.notify_route_travel_completed(route_id, status)

    # 发射隐藏标签揭示事件——#6 更新标签可见性
    for tag in _revealed_hidden_tags:
        IntelManager.notify_tag_revealed(route_id, tag)


func _emit_voyage_end_feedback(ctx: Dictionary) -> void:
    voyage_state_changed.emit(_previous_state, _voyage_state)
    # 如果航程中有波段转换，hull_band_changed 已在转换时实时发射
```

### _validate_encounter_context() — Exploration (#11) 消费端

```text
# 此函数属于 Exploration (#11)，此处作为合约参考
func _validate_encounter_context(ctx: Dictionary) -> Dictionary:
    if ctx == null or not ctx is Dictionary:
        return _build_fallback_context()
    if not ctx.get("route_id") or ctx.get("route_id") == &"":
        return _build_fallback_context()
    if not ctx.get("destination_id") or ctx.get("destination_id") == &"":
        return _build_fallback_context()
    var result: StringName = ctx.get("voyage_result", &"")
    if result not in [&"arrived", &"retreated", &"forced_landing"]:
        return _build_fallback_context()
    if not ctx.get("resolved_encounters") is Array:
        return _build_fallback_context()
    return ctx


func _build_fallback_context() -> Dictionary:
    push_error("Navigation: EncounterContext validation failed — using fallback context")
    return {
        "route_id": &"unknown",
        "destination_id": &"cloudwatch-ruins-fallback",
        "voyage_result": &"arrived",
        "resolved_encounters": [],
        "accumulated_damage": 0,
        "revealed_hidden_tags": [],
        "hull_band_arrival": &"intact",
        "forced_landing_position": &"",
        "damaged_slots": [],
    }
```

### VoyageResult Constants

```text
const VOYAGE_RESULT_ARRIVED: StringName = &"arrived"
const VOYAGE_RESULT_RETREATED: StringName = &"retreated"
const VOYAGE_RESULT_FORCED_LANDING: StringName = &"forced_landing"
```

---

## Out of Scope

- _validate_encounter_context() 和 _build_fallback_context() 的具体实现——属于 Exploration #11 消费端
- voyage_completed 信号的消费逻辑（场景生成、ARRIVING 阶段入口路由）——属于 Exploration #11
- #8 apply_hull_damage() 和 apply_module_damage() 的具体实现——属于 modules-hull-state Epic
- #6 notify_route_travel_completed() 的具体实现——属于 intel-knowledge Epic
- _persist_voyage_snapshot() 的具体实现——属于 Story 007

---

## QA Test Cases

- **AC-1/2/3**: EncounterContext construction by voyage_result
  - ARRIVED → all 9 fields, forced_landing_position=""
  - RETREATED → partial resolved_encounters
  - FORCED_LANDING → forced_landing_position non-empty, hull_band_arrival="destroyed"

- **AC-4**: All 9 fields type check
  - route_id=StringName, accumulated_damage=int, resolved_encounters=Array[Dictionary]

- **AC-8/9/10**: Writing order
  - (1)→(2)→(3)→(4)→(5); step failure doesn't block rest

- **AC-11 through AC-15**: Fallback
  - null→fallback; empty route_id→fallback; invalid result→fallback; non-array encounters→fallback
  - All fallback triggers → internal_error_log entry

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/navigation/EncounterContextSignalTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (VoyageContext, voyage_state), Story 004 (D_accumulated, damaged_slots, hull_band), Story 005 (resolved_encounters 构建, EncounterEntry), modules-hull-state Epic (apply_hull_damage), intel-knowledge Epic (notify_route_travel_completed, notify_tag_revealed)
- Unlocks: Story 007 (voyage snapshot persistence), Story 008 (EC-28 multi-system write crash recovery, EC-52/53/54/55/56 end state ACs)
