# Story 006: Persistence, Session Recovery & Edge Cases

> **Epic**: Exploration / Scavenge Scenario
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/exploration-scavenge-scenario.md`
**Requirement**: `TR-exploration-001`, `TR-exploration-002`, `TR-exploration-003`

**ADR Governing Implementation**: ADR-0013 (§7 ADR-0003 序列化, §5a 状态机, §5f F-11-05 状态变体转换, §6 威胁优先级排序); ADR-0003 (Canonical JSON 快照包)
**ADR Decision Summary**: `progress.exploration` snapshot package 持久化探索点状态（state_variant, search_points, intel_points, threat_points, env_threat_active）和活跃会话快照（phase, point_id, search_consumed, intel_interacted, threats_active, retreat_flagged）。快照时机：(1) 每次搜索完成后 (2) 威胁结算完成后 (3) 进入 EXTRACTING 时 (4) DEPARTED 结算完成时。恢复活跃会话时：PHASE_EXPLORING → 恢复至该阶段，搜索点/威胁状态保持一致，显示"你在探索中中断了"提示；PHASE_EXTRACTING → 读条是原子操作，恢复至 EXPLORING，玩家位于撤离锚点旁，需重新触发撤离。本 Story 覆盖 EC-11-01/02/03/08/09/13/20/21 等边缘情况——会话中断恢复、容量边界、多威胁排序、hull=0 不终止、页面失去焦点、存储配额满。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 持久化快照必须在状态变更后立即写入——不可延迟至帧末；恢复时以实际 Pool 5 状态为准（从 ResourcesManager 读取）——exploration snapshot 仅持久化探索点状态；恢复后发现不一致时静默修复
- Forbidden: EXTRACTING 阶段快照持久化 extraction 进度为"可恢复"——读条原子操作，中断后必须重新开始；fallback context 掩盖上游 bug 时静默——必须记录 internal_error_log
- Guardrail: localStorage 配额满时 HUD 显示非阻塞警告——30s 防抖，不重复刷屏

---

## Acceptance Criteria

### EC-11-01: Tab Close During EXPLORING

- [ ] **AC-1**: GIVEN session_phase=EXPLORING + 已搜索 3/6 搜索点 + Pool 5 有 2 格物品 + 已触发 1 个环境威胁 (env_threat_active=true)，WHEN 浏览器标签页关闭 → 重新打开 → 恢复会话，THEN session_phase=EXPLORING + 已搜索的 3 点不可再次搜索 + Pool 5 恢复 2 格物品 + env_threat_active=true + 显示"你在探索中中断了"提示。最近一次快照后的进度丢失（最多 1 次搜索或 1 次威胁结算）
- [ ] **AC-2**: GIVEN 恢复后 + Pool 5 快照与实际 ResourcesManager 状态不一致（模拟持久化损坏），WHEN _restore_active_session()，THEN 以 ResourcesManager 实际 Pool 5 状态为准 + 静默修复 occupied_slots + 不通知玩家（EC-11-09）

### EC-11-02: Tab Close During EXTRACTING

- [ ] **AC-3**: GIVEN session_phase=EXTRACTING + 读条进行到 1.5s，WHEN 浏览器标签页关闭 → 重新打开 → 恢复会话，THEN session_phase=EXPLORING（不是 DEPARTED）+ 玩家位置在撤离锚点旁 + 提取进度未保留 + 需重新触发撤离。读条是原子操作——不完整则不计数

### EC-11-03: DEPARTED Settlement Write Failure

- [ ] **AC-4**: GIVEN DEPARTED 结算写入因 localStorage 配额满而失败，WHEN 自动重试 4 次（1s/2s/4s/8s）全部失败，THEN UI 显示"保存失败。你的探索收获暂时保留。请检查浏览器存储空间后点击重试。"+ 手动重试按钮。结算包保留在内存中
- [ ] **AC-5**: GIVEN 手动重试按钮点击 + localStorage 已清理，WHEN 重试，THEN 结算包成功写入 + extraction_completed 发射 + session_phase→DEPARTED→IDLE

### EC-11-08: Hull Reaches Zero During Exploration

- [ ] **AC-6**: GIVEN session_phase=EXPLORING + 环境威胁触发后 hull=0，WHEN 检查，THEN 探索系统不自行终止探索 + HUD 显示"船体严重损毁"警告。撤离锚点仍然可用
- [ ] **AC-7**: GIVEN hull=0 + 玩家仍在探索中，WHEN 搜索/交互，THEN 正常操作不受影响。hull==0 的全局后果由 ModulesManager (#8) 负责

### EC-11-09: Pool 5 State Inconsistency

- [ ] **AC-8**: GIVEN occupied_slots=3 但实际格位占用=4（因持久化损坏），WHEN 进入探索点或搜索后执行一致性扫描，THEN 以实际格位状态为准修正 occupied_slots。静默修复，不通知玩家

### EC-11-13: Cleared Threat Zone Re-entry

- [ ] **AC-9**: GIVEN 之前已清除的威胁（is_active=false）+ 玩家再次走过其原始 trigger_radius，WHEN check_threat_trigger()，THEN 直接返回 {triggered: false}。该区域在本会话内永久安全
- [ ] **AC-10**: GIVEN 威胁被清除后，WHEN 查询 scout_preview_level()，THEN 预览标记不变（进入时快照）。已知轻度 UI 不一致——威胁已清除但标记仍在

### EC-11-20: Page Loses Focus / Long Idle

- [ ] **AC-11**: GIVEN session_phase=EXPLORING + 玩家 idle，WHEN 页面 visibilitychange→hidden 或 >30分钟无交互，THEN 探索无全局计时器——无惩罚。恢复时 phase 保持 EXPLORING + session_substate→SUBSTATE_IDLE
- [ ] **AC-12**: GIVEN session_phase=EXTRACTING + 读条进行中，WHEN 页面 visibilitychange→hidden 时间 >5s，THEN 读条中断并重置（计时器在后台不可靠）+ session_phase→EXPLORING + 玩家在锚点旁
- [ ] **AC-13**: GIVEN session_phase=ARRIVING + 页面隐藏 >5s，WHEN 恢复，THEN 跳过 ARRIVING 描述文本 → 自动进入 EXPLORING

### EC-11-21: localStorage Quota Exceeded

- [ ] **AC-14**: GIVEN localStorage.setItem() 抛出 QuotaExceededError 在 EXPLORING 阶段快照时，WHEN 检测到，THEN HUD 显示非阻塞警告 "⚠ 存储空间不足，探索进度可能无法保存。" + 30s 内不重复显示
- [ ] **AC-15**: GIVEN 快照失败后 + 30s 防抖已过 + 再次快照仍失败，THEN 再次显示警告（不累积，替换上一条）

### ADR-0003 Serialization Roundtrip

- [ ] **AC-16**: GIVEN 探索点状态包含 state_variant=LOOTED + 全部 6 个搜索点 consumed + 2 个情报点 interacted + 2 个威胁 inactive，WHEN _serialize_exploration() → JSON.stringify() → JSON.parse() → _deserialize_exploration()，THEN 所有字段一致无丢失
- [ ] **AC-17**: GIVEN 活跃会话 (PHASE_EXPLORING)，WHEN 序列化往返，THEN session 快照字段完整——phase, point_id, search_consumed, intel_interacted, threats_active, retreat_flagged
- [ ] **AC-18**: GIVEN 无活跃会话 (PHASE_IDLE)，WHEN _serialize_exploration()，THEN active_session={}——空对象，不携带过期会话数据

### Defensive: Invalid State Recovery

- [ ] **AC-19**: GIVEN 存档中 exploration_points 包含未在 Registry 中注册的 point_id（数据迁移残留），WHEN _deserialize_exploration()，THEN 跳过该条目 + 记录 warning 日志。不崩溃
- [ ] **AC-20**: GIVEN 存档中 active_session.phase=PHASE_DEPARTED（不应持久化的终态），WHEN 恢复，THEN 忽略活跃会话——视为 IDLE。记录 warning

---

## Implementation Notes

### Snapshot Triggers

```gdscript
# 快照触发点:
# (1) 每次搜索完成后 → perform_search() 返回前
# (2) 威胁结算完成后 → on_combat_result() 或 _handle_environmental_threat() 返回前
# (3) 进入 EXTRACTING → trigger_extraction() / force_extraction() 中
# (4) DEPARTED 结算完成 → _finalize_extraction() 中

func _trigger_snapshot() -> void:
    var snapshot := _serialize_exploration()
    var success := Persistence.capture_snapshot("progress.exploration", snapshot)
    if not success:
        _handle_snapshot_failure()

var _last_quota_warning_time: float = 0.0
const QUOTA_WARNING_COOLDOWN: float = 30.0

func _handle_snapshot_failure() -> void:
    var now := Time.get_ticks_msec() / 1000.0
    if now - _last_quota_warning_time >= QUOTA_WARNING_COOLDOWN:
        _last_quota_warning_time = now
        # 发射信号通知 UI 显示非阻塞警告
        _emit_quota_warning()
```

### Session Restore

```gdscript
func _restore_active_session(snapshot: Dictionary) -> void:
    var session := snapshot.get("active_session", {})
    if session.is_empty():
        return  # 无活跃会话

    var phase: int = session.get("phase", PHASE_IDLE)

    match phase:
        PHASE_EXPLORING:
            _restore_exploring_session(session)
        PHASE_EXTRACTING:
            _restore_interrupted_extraction(session)
        _:
            push_warning("Exploration: cannot restore phase %d — ignoring active session" % phase)

func _restore_exploring_session(session: Dictionary) -> void:
    current_exploration_point_id = session.get("point_id", &"")
    session_search_consumed = session.get("search_consumed", {})
    session_intel_interacted = session.get("intel_interacted", {})
    session_threats_active = session.get("threats_active", {})
    session_retreat_flagged = session.get("retreat_flagged", false)

    # 以 ResourcesManager 实际 Pool 5 为准（EC-11-09）
    _reconcile_pool_state()

    # 快照 η_scout（恢复时不重进 ARRIVING——直接回 EXPLORING）
    _snapshot_eta_scout()

    _transition_phase(PHASE_EXPLORING)
    # 发射 interrupted 提示信号（场景层显示"你在探索中中断了"）
    _emit_session_restored_notice()

func _restore_interrupted_extraction(session: Dictionary) -> void:
    # EXTRACTING 不可恢复——回 EXPLORING
    current_exploration_point_id = session.get("point_id", &"")
    session_search_consumed = session.get("search_consumed", {})
    session_intel_interacted = session.get("intel_interacted", {})
    session_threats_active = session.get("threats_active", {})
    session_retreat_flagged = session.get("retreat_flagged", false)

    _reconcile_pool_state()
    _snapshot_eta_scout()

    _transition_phase(PHASE_EXPLORING)
    # 场景层将玩家放置在撤离锚点旁
    _emit_extraction_interrupted_on_restore()

func _reconcile_pool_state() -> void:
    # 以 actual Pool 5 为准（EC-11-09 一致性扫描）
    var actual_occupied := ResourcesManager.get_pool_occupied("pool_5")
    var tracked_occupied := ResourcesManager.get_pool_tracked("pool_5")
    if actual_occupied != tracked_occupied:
        push_warning("Exploration: Pool 5 inconsistency detected — auto-correcting")
        ResourcesManager.reconcile_pool_5()
```

### Page Visibility Handling

```gdscript
# 由 Platform #2 调用——ExplorationManager 不直接监听 visibilitychange
func on_page_hidden() -> void:
    match session_phase:
        PHASE_ARRIVING:
            _mark_arriving_interrupted_by_visibility = true
        PHASE_EXTRACTING:
            _mark_extraction_interrupted_by_visibility = true

func on_page_visible() -> void:
    match session_phase:
        PHASE_ARRIVING:
            if _mark_arriving_interrupted_by_visibility:
                _mark_arriving_interrupted_by_visibility = false
                skip_arriving()  # 自动跳过 ARRIVING
        PHASE_EXTRACTING:
            if _mark_extraction_interrupted_by_visibility:
                _mark_extraction_interrupted_by_visibility = false
                _interrupt_extraction(&"page_visibility")  # 读条中断
        PHASE_EXPLORING:
            session_substate = SUBSTATE_IDLE  # 恢复为 idle
```

### Serialization

```gdscript
func _serialize_exploration() -> Dictionary:
    var serialized_points := {}
    for point_id in exploration_points:
        var pt := exploration_points[point_id]
        serialized_points[point_id] = {
            "state_variant": pt.state_variant,
            "search_points": pt.search_points.duplicate(true),
            "intel_points": pt.intel_points.duplicate(true),
            "threat_points": pt.threat_points.duplicate(true),
            "env_threat_active": pt.env_threat_active,
        }

    var session_snapshot := {}
    if session_phase == PHASE_EXPLORING or session_phase == PHASE_EXTRACTING:
        session_snapshot = {
            "phase": session_phase,
            "point_id": current_exploration_point_id,
            "search_consumed": session_search_consumed.duplicate(true),
            "intel_interacted": session_intel_interacted.duplicate(true),
            "threats_active": session_threats_active.duplicate(true),
            "retreat_flagged": session_retreat_flagged,
        }
    # ARRIVING/DEPARTED/IDLE 不持久化会话快照

    return {
        "domain_id": "exploration",
        "points": serialized_points,
        "active_session": session_snapshot,
    }

func _deserialize_exploration(snapshot: Dictionary) -> void:
    var points_data := snapshot.get("points", {})
    for point_id in points_data:
        var data := points_data[point_id]
        if not Registry.has_entity(point_id):
            push_warning("Exploration: skipping unknown point '%s' in snapshot" % point_id)
            continue
        exploration_points[point_id] = {
            "state_variant": data.get("state_variant", STATE_UNLOOTED),
            "search_points": data.get("search_points", {}),
            "intel_points": data.get("intel_points", {}),
            "threat_points": data.get("threat_points", {}),
            "env_threat_active": data.get("env_threat_active", false),
        }
```

---

## Out of Scope

- Persistence.capture_snapshot() / restore_snapshot() 内部实现——属于 local-save-persistence Epic
- localStorage 配额检测与 QuotaExceededError 捕获——属于 Platform #2
- HUD 警告渲染（存储警告、中断提示、船体严重损毁警告）——属于 #16 UIManager
- ResourcesManager.reconcile_pool_5() 实现——属于 resources-goods-capacity Epic
- 页面 visibilitychange 事件的初始监听——属于 Platform #2

---

## QA Test Cases

- **AC-1**: Tab close during EXPLORING → restore with correct state
- **AC-2**: Pool 5 inconsistency → auto-correct silently
- **AC-3**: Tab close during EXTRACTING → restore to EXPLORING at anchor
- **AC-4–5**: Settlement retry (4 attempts + manual retry)
- **AC-6**: hull=0 → warning but no auto-termination
- **AC-8**: Pool 5 inconsistency scan & silent fix
- **AC-9**: Cleared threat zone → permanently safe
- **AC-11–13**: Page visibility handling (idle resume / extraction interrupt / arriving skip)
- **AC-14–15**: localStorage quota warning with 30s cooldown
- **AC-16–18**: Serialization roundtrip fidelity (all states)
- **AC-19–20**: Defensive invalid state recovery

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/exploration/persistence_recovery_test.gd` — must exist and pass, OR documented playtest covering all ACs
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001–005 (all exploration stories), local-save-persistence Epic (capture_snapshot, restore_snapshot, ADR-0003), platform-session-shell Epic (visibilitychange, localStorage 配额), resources-goods-capacity Epic (reconcile_pool_5)
- Unlocks: N/A — 这是探索 Epic 的最后一个 Story
