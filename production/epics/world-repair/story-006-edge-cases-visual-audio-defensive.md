# Story 006: Edge Cases, MVP Visual/Audio & Defensive Handling

> **Epic**: World Repair & Unlock
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/world-repair-unlock.md`
**Requirement**: `TR-repair-001`, `TR-repair-002`, `TR-repair-003`

**ADR Governing Implementation**: ADR-0011 (§5e visual_state_changed, §Risks table, MVP 视觉规格)
**ADR Decision Summary**: GDD 定义了 12 个边缘案例 + Creative Director 约束要求 MVP 必须承担"可见恢复"反馈的归属权（#17 为 Vertical Slice，修复本身的视觉/世界反馈在本系统内定义最低版本）。视觉规格：known 状态灰暗/破损 sprite → repaired 状态发光 sprite + modulate 呼吸动画（±10% opacity, 周期 3s）+ 半透明光束从灯塔向航线方向 + 暖色光点粒子（6-8 个，上浮，2-4s 生命周期，48px 半径生成）。音频规格：提交材料短促确认音 <0.5s + 最后提交嗡鸣渐强→清脆"叮"声 2-3s。仪式持续 5.0s ± 0.5s。桌面窗口暂停恢复后仪式基于 delta 继续。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 修复完成视觉仪式持续 repair_ceremony_duration_sec (5.0s)；灯塔呼吸动画 modulate.a 在 [0.9, 1.0] 间正弦波动（周期 3s）；标签页恢复后仪式基于 delta 继续——不跳过不卡死
- Forbidden: 对已提交 material 仍接受重复提交（excess_quantity 守卫）；仪式期间阻止 UI 关闭（UI 关闭按钮必须可交互）
- Guardrail: repair_progress 在 |required|=0 时返回 0.0；hazard 降低不可为负（max(0, hazard-reduction)）

---

## Acceptance Criteria

### EC-13-01: Excess Quantity Prevention

- [ ] **AC-1**: GIVEN required[repair_kit]=4 + deposited[repair_kit]=4，WHEN 尝试提交 {repair_kit: 1}，THEN validate_deposit → excess_quantity。UI 数量选择器上限 = max(0, required - deposited)。多余材料保留在源池

### EC-13-02: Already Repaired Idempotent Guard

- [ ] **AC-2**: GIVEN repair_state=REPAIRED + 玩家到达节点位置，WHEN UI 检查，THEN 不显示修复交互入口。若直接调用 submit_deposit → already_repaired violation
- [ ] **AC-3**: GIVEN repair_state=REPAIRED + on_player_arrived_at_repair_node() 调用，THEN 状态保持 REPAIRED。不重复 emit visual_state_changed

### EC-13-03: Position Check — Not at Repair Node

- [ ] **AC-4**: GIVEN 玩家位置不等于 linked_location_id，WHEN UI 检查交互可用性，THEN #11 不传递修复交互入口。API 层 submit_deposit 在位置不匹配时调用方不应调用（由 Exploration #11 门控）

### EC-13-04: Physical Arrival Without Intel

- [ ] **AC-5**: GIVEN repair_state=UNREVEALED + knowledge_state < identified + 玩家物理到达，WHEN on_player_arrived_at_repair_node，THEN state→KNOWN。可查看需求清单、提交材料。材料清单中未通过情报确认的资源显示"？"，解锁预览显示"未知效果"
- [ ] **AC-6**: GIVEN 节点 KNOWN + 后续情报揭示（knowledge_state ≥ identified），WHEN UI 刷新，THEN 材料清单中之前显示"？"的资源更新为具体名称和数量。解锁预览从"未知效果"更新为具体航线/能力名称

### EC-13-05: Knowledge Regression on Save Load

- [ ] **AC-7**: GIVEN 修复节点 repair_state=REPAIRED + knowledge_state 回退至 <identified（存档回滚），WHEN 加载后重新评估，THEN repair_state 保持 REPAIRED（终态优先于知识状态）。知识状态服从修复状态

### EC-13-06: Atomic commit_deposit Failure

- [ ] **AC-8**: GIVEN #5 commit_deposit 返回错误（如 Pool 6 写入失败），WHEN submit_deposit 检测，THEN 返回 ERR_COMMIT_FAILED。deposited 计数器不变，材料保留在源池，不发射 repair_progress_changed。向玩家显示"提交失败，材料未消耗"

### EC-13-07: Mid-Repair Departure (Leave & Return)

- [ ] **AC-9**: GIVEN 分批提交 repair_kit×3 → deposited[repair_kit]=3, repair_state=KNOWN → 玩家离开探索点，WHEN 返回并继续，THEN deposited 计数器保持 3。可继续提交剩余材料。进度不丢失

### EC-13-08: Mid-Batch Save/Load

- [ ] **AC-10**: GIVEN 分批提交中途存档（deposited={repair_kit: 2}）→ 读档，WHEN 恢复，THEN deposited={repair_kit: 2}, repair_progress=0.25, repair_state=KNOWN。可继续提交

### EC-13-09: Route Hazard Below Zero Guard

- [ ] **AC-11**: GIVEN 航线 hazard=0.1 + hazard_reduction=0.3（30%），WHEN 增强应用，THEN new_hazard = max(0, 0.1 - 0.1×0.3) = max(0, 0.07) = 0.07。不为负。若 hazard=0 → new_hazard=0

### EC-13-10: New Game / Save Reset

- [ ] **AC-12**: GIVEN 新游戏启动，WHEN _init_new_game_state()，THEN 所有修复节点 repair_state=UNREVEALED, deposited={}, repair_progress=0.0

### EC-13-11: Last Material Commit — Bag Slot Empty

- [ ] **AC-13**: GIVEN 最后一批材料提交完成后背包空间变化，WHEN 修复完成，THEN 仪式独立于背包状态触发。不因背包空间变化产生额外事件

### EC-13-12: Browser Tab Suspend During Ceremony

- [ ] **AC-14**: GIVEN 修复仪式播放中（5.0s 动画），WHEN 桌面窗口被暂停（suspend_requested），THEN 动画暂停。恢复后基于 `delta` 继续——不跳过剩余动画、不卡死、不从头开始。仪式总时长基于累计 delta（非挂钟时间）

### MVP Visual Feedback (Creative Director Constraint)

- [ ] **AC-15**: GIVEN repair_state=KNOWN，WHEN 玩家查看灯塔，THEN sprite 为灰暗/破损状态。无光晕、无光束、无粒子
- [ ] **AC-16**: GIVEN repair_state→REPAIRED + visual_state_changed("repaired") 发射，WHEN visual anchor 切换，THEN:
  - 灯塔 sprite 切换为发光版本（暖黄色光晕）
  - modulate.a 在 [0.9, 1.0] 间正弦波动（周期约 3s）——呼吸式明暗变化
  - 半透明光束 (Color(1.0, 0.9, 0.6, 0.3)) 从灯塔顶部向关联航线方向射出，持续循环
  - 暖色光点粒子 6-8 个，在灯塔周围 48px 半径内生成，向上浮升，生命周期 2-4s
- [ ] **AC-17**: GIVEN 仪式持续 repair_ceremony_duration_sec (5.0s ± 0.5s)，WHEN 计时，THEN 5.0s 后视觉稳定在 repaired 状态（呼吸动画持续循环——光束和粒子持续）。仪式期间 UI 关闭按钮可交互——关闭 UI 后动画继续播放至完成

### MVP Audio Feedback

- [ ] **AC-18**: GIVEN 每次 submit_deposit（含分批），WHEN 材料提交，THEN 短促确认音播放（<0.5s）。金属/石料碰撞感
- [ ] **AC-19**: GIVEN 最后一批材料提交（repair_completion=true），WHEN 仪式开始，THEN 持续嗡鸣渐强 → 清脆"叮"声（总时长 2-3s）。暗示装置启动

### Defensive Input Validation

- [ ] **AC-20**: GIVEN submit_deposit 被调用时 node_id 为空字符串或非 StringName，WHEN 验证，THEN validate_deposit 返回 invalid_node。不崩溃
- [ ] **AC-21**: GIVEN offer 参数包含非整数 quantity（如 float 或负值），WHEN validate_deposit，THEN quantity<=0 → empty_offer；非整数自动 floor 或 reject（实现选择）
- [ ] **AC-22**: GIVEN Registry 中 required_resources 格式错误（非数组或空），WHEN _get_required_resources()，THEN 返回空 Dictionary。repair_progress=0.0, repair_completion=false。不崩溃
- [ ] **AC-23**: GIVEN repair_ceremony_duration_sec 配置为负值，WHEN 仪式计时，THEN clamp 至最小 0.5s。不无限等待

---

## Implementation Notes

### Visual Feedback — MVP Minimal Spec

```text
# WorldRepair Autoload #13 — MVP 视觉反馈（#17 就绪前由本系统直接管理）

# 灯塔视觉状态
var _beacon_visual_state: StringName = &"known"  # known / repaired
var _ceremony_elapsed: float = 0.0
var _ceremony_active: bool = false

const CEREMONY_DURATION: float = 5.0
const BREATHING_PERIOD: float = 3.0
const BREATHING_AMPLITUDE: float = 0.1  # ±10% opacity


func _process(delta: float) -> void:
    if not _ceremony_active:
        return

    _ceremony_elapsed += delta

    # 呼吸动画（持续循环——即使仪式结束后仍运行）
    var breath: float = sin(_ceremony_elapsed * TAU / BREATHING_PERIOD)
    var alpha: float = 1.0 - (BREATHING_AMPLITUDE / 2.0) + (breath * BREATHING_AMPLITUDE / 2.0)
    _apply_beacon_modulate(Color(1.0, 1.0, 1.0, alpha))

    # 光束持续循环（始终可见）
    _update_beacon_beam(delta)

    # 粒子在仪式期间持续生成，仪式结束后停止新生成但现有粒子自然消散
    if _ceremony_elapsed <= CEREMONY_DURATION:
        _spawn_beacon_particles(delta)
    # 粒子生命周期 2-4s——仪式结束后最多 4s 全部消散

    # 仪式完成信号
    if _ceremony_elapsed >= CEREMONY_DURATION and _ceremony_active:
        _ceremony_active = false
        # 不做特殊处理——视觉保持在 repaired 循环状态


func _trigger_repair_ceremony(node_id: StringName) -> void:
    _beacon_visual_state = &"repaired"
    _ceremony_elapsed = 0.0
    _ceremony_active = true
    # 音频：嗡鸣渐强开始
    _play_ceremony_audio()
```

### Visual Sprite & Beam

```text
func _apply_beacon_sprite(state: StringName) -> void:
    # 通过 visual_state_anchor 获取对应的 sprite 节点
    # known → 灰暗/破损纹理；repaired → 发光纹理（暖黄色光晕）
    # 具体 sprite 切换由 Feedback (#17) 或场景中的 Beacon node 消费 visual_state_changed 信号执行
    pass  # 实现细节由 #17 或场景树中的 Beacon 节点处理


func _update_beacon_beam(_delta: float) -> void:
    # 半透明光束 Color(1.0, 0.9, 0.6, 0.3)
    # 从灯塔顶部向 route.sky-reef-arc-01 方向射出
    # 单条射线 sprite，持续循环
    pass  # 实现细节由 #17 或场景树中的 Beacon 节点处理


func _spawn_beacon_particles(_delta: float) -> void:
    # 暖色光点，6-8 个
    # 在灯塔周围 48px 半径内随机位置生成
    # 向上浮升，生命周期 2-4s
    pass  # 实现细节由 #17 或场景树中的 Beacon 节点处理
```

### Audio Feedback

```text
func _play_deposit_confirm_audio() -> void:
    # 短促确认音 <0.5s — 金属/石料碰撞感
    # 使用 Godot AudioStreamPlayer 播放一次性音效
    pass  # 具体音频资源路径由 audio-director 定义


func _play_ceremony_audio() -> void:
    # 持续嗡鸣渐强 → 清脆"叮"声 (2-3s)
    # 可通过 AnimationPlayer 或代码驱动 AudioStreamPlayer 进度
    pass  # 具体音频资源路径由 audio-director 定义
```

### Browser Tab Suspend Safe

```text
# Godot 桌面构建中，suspend_requested/window_focus_changed 事件会暂停 `_process` 调用。
# 当标签页恢复时，`_process` 恢复——delta 会是自上次 process 以来经过的实际时间。
# 
# 这意味着 _ceremony_elapsed += delta 在恢复后自动包含暂停期间的时间。
# 
# 为保持仪式完整性，我们使用：
#   _ceremony_elapsed += delta  # delta 可能很大（暂停数秒后一次性跳变）
# 
# 仪式不跳过——即使 delta 大到跳过整个仪式时长：
#   _ceremony_elapsed 直接跳至 ≥ CEREMONY_DURATION
#   → 视觉立即切换至 repaired 稳态（呼吸动画、光束、粒子自然消散）
#   → 用户看到的是：切回标签页 → 灯塔已处于 repaired 视觉
#
# 这是正确的行为：标签页暂停时玩家不应错过仪式——仪式在后台"完成"
# 但视觉在恢复时即时呈现最终状态。
```

### Config Validation

```text
func _validate_ceremony_config() -> void:
    # 防御性配置验证——在 feature_ready 阶段调用
    if CEREMONY_DURATION <= 0.0:
        push_error("WorldRepair: repair_ceremony_duration_sec must be > 0 — clamping to 0.5")
        # CEREMONY_DURATION is const — 实际修复需改为可配置 tuning knob
```

---

## Out of Scope

- Beacon sprite 资源（灰暗/发光两张纹理）的制作——属于 art-director
- 音频资源（确认音、嗡鸣、叮声）的制作——属于 sound-designer
- 灯塔视觉节点的具体场景实现——属于 world-builder + #17 Feedback
- #14 村镇 NPC 活跃度变化——属于 settlement-market Epic (ADR-0014 deferred)
- 航线高亮覆盖在航图上的渲染——属于 #9 Chart + #16 UIManager

---

## QA Test Cases

- **AC-1**: Excess quantity → rejected
- **AC-2/3**: Already repaired → no UI + API rejection
- **AC-4**: Wrong position → no interaction (gated by #11)
- **AC-5/6**: Physical without intel → "？" display; intel reveal → updates to specific names
- **AC-7**: Knowledge regression → repaired state preserved
- **AC-8**: commit_deposit failure → state unchanged + user message
- **AC-9**: Leave mid-batch → progress preserved
- **AC-10**: Save/load mid-batch → progress identical
- **AC-11**: Hazard floor → ≥0 always
- **AC-12**: New game → all unrevealed
- **AC-13**: Bag slot change → no side effects
- **AC-14**: Tab suspend → delta-based continuation
- **AC-15/16/17**: Visual — known→repaired sprite, breathing, beam, particles, ceremony duration
- **AC-18/19**: Audio — deposit confirm, ceremony audio
- **AC-20/21/22/23**: Defensive guards — null node_id, non-int qty, malformed required_resources, negative ceremony duration

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/world-repair/EdgeCasesTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (validate_deposit/submit_deposit), Story 003 (formulas), Story 004 (signals), Story 005 (persistence), resources-goods-capacity Epic (commit_deposit 失败语义), exploration-scavenge Epic (位置检查、物理交互锚点), intel-knowledge Epic (knowledge_state 查询)
- Unlocks: World-repair system-level integration testing, QA smoke tests for repair flows
