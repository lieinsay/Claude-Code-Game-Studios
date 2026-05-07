# Story 004: Signal Events & Downstream Trigger Chain

> **Epic**: World Repair & Unlock
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/world-repair-unlock.md`
**Requirement**: `TR-repair-003`

**ADR Governing Implementation**: ADR-0011 (§3 信号接口, §5e 6 路下游触发链, fan-out 模式)
**ADR Decision Summary**: WorldRepair 声明 3 个信号——repair_progress_changed（每次提交后 emit）、repair_completed（材料集齐 known→repaired 转换后 emit）、visual_state_changed（修复完成后 emit）。repair_completed 信号 fan-out 到 6 个下游系统：(1)#6 Intel——on_repair_completed 触发能力解锁 Path C 重评估；(2)#9 Chart——on_route_enhanced 航线从不可通行变可通行 + hazard 降低 30%；(3)#14 Settlement——消费 repair_completed 信号驱动 NPC 活跃度/对话变化；(4)#3 Persistence——capture_snapshot 触发存档检查点；(5)#17 Feedback——消费 visual_state_changed 信号驱动灯塔重亮动画；(6)UI (#16)——toast "天礁灯塔 已修复" + 解锁摘要。信号遵循 ADR-0002：typed params, sync emit, emit-after-mutation, max cascade depth 2。repair_completed 在状态机转换完成后发射——回调中可安全查询 get_repair_state()。下游消费顺序由 Godot signal 连接顺序决定（按连接顺序同步调用）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: repair_completed 信号在状态机 known→repaired 转换后 emit（emit-after-mutation）；3 个信号均为 typed params（node_id: StringName / + progress: float, deposited: Dictionary）；fan-out 深度 ≤ 1——所有 6 个消费者直接从 repair_completed 连接
- Forbidden: 在 submit_deposit 中途（commit_deposit 前）发射 repair_completed；使用 Dictionary payload 代替 typed params；cascade depth ≥ 2
- Guardrail: 任一消费者回调异常不阻止其他消费者执行（Godot signal 默认行为——各连接独立调用）

---

## Acceptance Criteria

### Signal Declarations

- [ ] **AC-1**: GIVEN WorldRepair Autoload 初始化，WHEN 信号声明，THEN:
  - repair_progress_changed(node_id: StringName, progress: float, deposited: Dictionary)
  - repair_completed(node_id: StringName)
  - visual_state_changed(node_id: StringName, visual_state: StringName)
  所有 3 个信号采用 typed params——符合 ADR-0002

### Signal Emission Order

- [ ] **AC-2**: GIVEN 提交材料后 repair_completion=false，WHEN submit_deposit 完成，THEN 仅 repair_progress_changed 发射。repair_completed 和 visual_state_changed 不发射
- [ ] **AC-3**: GIVEN 提交最后一批材料后 repair_completion=true，WHEN submit_deposit 完成，THEN 发射顺序为: (1) repai_progress_changed (progress=1.0), (2) repair_completed, (3) visual_state_changed("repaired")。严格按此顺序

### Fan-out to #6 — Ability Unlock

- [ ] **AC-4**: GIVEN repair_completed("repair_node.starlight_dock") 信号发射，WHEN #6 Intel 消费，THEN on_repair_completed(node_id) 被调用 → 重评估 ability.lighthouse-signal-interpretation Path C 条件 → ability_state→unlocked
- [ ] **AC-5**: GIVEN #6 未实现或不可用（如测试环境），WHEN repair_completed 发射，THEN 不崩溃——#6 连接不存在时信号发射无消费者，正常完成

### Fan-out to #9 — Route Enhancement

- [ ] **AC-6**: GIVEN repair_completed 信号发射，WHEN #9 Chart 消费，THEN on_route_enhanced("route.sky-reef-arc-01", {effect: "hazard_reduction", magnitude: 0.3}) 被调用。航线从 traversable=false→true，hazard 降低 30%
- [ ] **AC-7**: GIVEN route_enhancement 应用前 route.sky-reef-arc-01 hazard=0.5，WHEN 增强，THEN new_hazard = max(0, 0.5 - 0.5×0.3) = 0.35。hazard 降低但不可为负

### Fan-out to #3 — Persistence Checkpoint

- [ ] **AC-8**: GIVEN repair_completed 信号发射，WHEN #3 Persistence 消费，THEN capture_snapshot("progress.world-repair", snapshot) 被调用。存档检查点写入

### Fan-out to #17 — Visual Anchor

- [ ] **AC-9**: GIVEN visual_state_changed("repair_node.starlight_dock", "repaired") 信号发射，WHEN #17 Feedback 消费，THEN visual_state_anchor 切换为 repaired。灯塔 sprite 从灰暗→发光，光晕呼吸动画启动，光束+粒子激活

### UI Toast

- [ ] **AC-10**: GIVEN repair_completed 信号发射，WHEN UI (#16) 消费，THEN 全屏中央 toast 显示"天礁灯塔 已修复" + 解锁内容摘要。3 秒后自动消失或点击关闭

### Emit-after-Mutation

- [ ] **AC-11**: GIVEN repair_completed 信号已连接消费者回调，WHEN 回调中调用 get_repair_state(node_id)，THEN 返回 REPAIRED。状态机转换已完成——emit 在 mutation 之后
- [ ] **AC-12**: GIVEN repair_progress_changed 信号已连接消费者回调，WHEN 回调中调用 get_repair_progress(node_id)，THEN 返回最新 progress 值。deposited 计数器已更新

### Cascade Depth

- [ ] **AC-13**: GIVEN repair_completed → #9 on_route_enhanced 调用，WHEN 追踪调用链，THEN #9 内部可能发射 route_enhanced 信号（depth 1）。总 cascade ≤ 2。不超过 ADR-0002 限制
- [ ] **AC-14**: GIVEN repair_completed → #6 on_repair_completed → 能力解锁 → 可能发射 ability_unlocked 信号（depth 1）。总 cascade ≤ 2

---

## Implementation Notes

### Signal Declaration

```gdscript
# WorldRepair Autoload #13 — 信号声明（遵循 ADR-0002 typed params）
signal repair_progress_changed(node_id: StringName, progress: float, deposited: Dictionary)
signal repair_completed(node_id: StringName)
signal visual_state_changed(node_id: StringName, visual_state: StringName)
```

### Emit Call in submit_deposit

```gdscript
func submit_deposit(node_id: StringName, offer: Dictionary) -> Dictionary:
    # ... 验证 + commit_deposit + 计数器更新 + progress 计算 ...

    # 步骤 5: emit repair_progress_changed（每次提交后）
    repair_progress_changed.emit(node_id, progress, get_deposited(node_id))

    # 步骤 6: 若完成 → 状态转换 + 发射信号
    if completed:
        # 先转换状态（emit-after-mutation 的前置条件）
        _transition_state(node_id, REPAIR_STATE_REPAIRED)

        # 再发射信号——消费者回调中可安全查询状态
        repair_completed.emit(node_id)
        visual_state_changed.emit(node_id, &"repaired")

    # ...
```

### Fan-out Connection Points (contract reference)

```gdscript
# 消费方连接——在各自的 _ready() 或 feature_ready 回调中建立
# 以下为合约参考，实际实现位于各消费系统中：

# #6 Intel — on_repair_completed → ability unlock Path C re-eval
# WorldRepair.repair_completed.connect(Intel.on_repair_completed)

# #9 Chart — route unlock + hazard enhancement
# WorldRepair.repair_completed.connect(Chart._on_world_repair_completed)

# #14 Settlement — NPC activity/dialogue update
# WorldRepair.repair_completed.connect(Settlement._on_repair_completed)

# #3 Persistence — checkpoint snapshot
# WorldRepair.repair_completed.connect(Persistence._on_world_repair_checkpoint)

# #17 Feedback — visual anchor state change
# WorldRepair.visual_state_changed.connect(Feedback._on_visual_state_changed)

# #16 UI — toast display
# WorldRepair.repair_completed.connect(UIManager._on_repair_completed_toast)
```

### Consumer-side Contract (from #6 and #9 perspective)

```gdscript
# === #6 Intel — on_repair_completed consumer ===
func on_repair_completed(repair_node_id: StringName) -> void:
    # 重评估所有依赖 repair 的 ability unlock path
    # MVP: lighthouse-signal-interpretation Path C
    # "Path C: repair_node.starlight_dock.repaired == true AND location.glass-harbor-outskirts visited"
    _re_evaluate_repair_gated_abilities(repair_node_id)


# === #9 Chart — _on_world_repair_completed consumer ===
func _on_world_repair_completed(repair_node_id: StringName) -> void:
    var enhancements: Array = WorldRepair.get_route_enhancements(repair_node_id)
    for entry in enhancements:
        var route_id: StringName = entry["route_id"]

        # 1. 航线解锁
        if entry["unlock"]:
            _set_route_traversable(route_id, true)

        # 2. Hazard 降低
        if entry["effect_type"] == &"hazard_reduction":
            var current: float = _get_route_hazard(route_id)
            var reduction: float = current * entry["magnitude"]
            var new_hazard: float = maxf(current - reduction, 0.0)
            _set_route_hazard(route_id, new_hazard)
```

### Fan-out Safety

```gdscript
# Godot signal 默认行为：若消费者回调中抛出异常，后续连接的回调不被调用。
# 但 ADR-0002 要求任一消费者异常不阻止其他消费者。
# 消费方应自行处理异常——生产方（WorldRepair）在 emit 时不做额外保护
# （严格遵循 ADR-0002：信号发射者不负责消费者异常处理）
```

---

## Out of Scope

- Intel.on_repair_completed 的具体实现——属于 intel-knowledge Epic
- Chart._on_world_repair_completed 的具体实现——属于 chart-route-planning Epic
- Persistence 存档检查点的具体实现——属于 local-save-persistence Epic
- Feedback 视觉锚点渲染——属于 #17（Vertical Slice）
- UI toast 的具体渲染——属于 #16 UIManager
- Settlement NPC 状态变更——属于 settlement-market Epic (ADR-0014 deferred)

---

## QA Test Cases

- **AC-1**: Signal declarations — typed params verified
- **AC-2**: Partial submit → only repair_progress_changed
- **AC-3**: Final submit → progress_changed → completed → visual_state_changed
- **AC-4**: repair_completed → #6 ability unlock triggered
- **AC-5**: #6 unavailable → no crash
- **AC-6/7**: repair_completed → #9 route unlock + hazard reduction
- **AC-8**: repair_completed → #3 checkpoint
- **AC-9**: visual_state_changed → #17 anchor switch
- **AC-10**: repair_completed → UI toast
- **AC-11/12**: Emit-after-mutation — consumers see updated state
- **AC-13/14**: Cascade depth ≤ 2

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/world-repair/signal_downstream_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine transitions), Story 002 (submit_deposit signal emission), Story 003 (route_enhancement output), intel-knowledge Epic (#6 on_repair_completed), chart-route-planning Epic (#9 on_route_enhanced), local-save-persistence Epic (#3 capture_snapshot)
- Unlocks: Story 006 (downstream edge cases — #6/#9 unavailable, cascade guard)
