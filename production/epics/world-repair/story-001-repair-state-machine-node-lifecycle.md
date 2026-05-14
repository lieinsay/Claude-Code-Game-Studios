# Story 001: Repair State Machine & Node Lifecycle

> **Epic**: World Repair & Unlock
> **Status**: Done
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/world-repair-unlock.md`
**Requirement**: `TR-repair-001`

**ADR Governing Implementation**: ADR-0011 (§1 WorldRepair Autoload #13, §2 Dictionary 后端存储, §5a 三态状态机)
**ADR Decision Summary**: WorldRepair 作为 Autoload #13，Phase 5 feature_ready 初始化。修复节点三态状态机——unrevealed（初始）→ known（物理到达 OR 情报≥identified）→ repaired（材料集齐，终态）。known→repaired 单向不可逆；禁止 known→unrevealed（知识不可退化）、repaired→known/unrevealed（修复不可撤销）。物理到达始终触发 unrevealed→known，不依赖情报门控。状态存储在 Dictionary[StringName, RepairNodeState] 中，每个节点包含 repair_state、deposited 计数器、repair_progress 浮点值。修复节点静态定义从 Registry (#1) query_entity 读取，WorldRepair 内部不硬编码节点属性。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: on_player_arrived_at_repair_node 始终推进 unrevealed→known——不依赖情报门控；repaired 终态拒绝所有后续状态变更；修复节点静态定义从 Registry 读取——不硬编码
- Forbidden: known→unrevealed 逆向转换；repaired→known 或 repaired→unrevealed；对 repaired 节点触发任何状态变更
- Guardrail: 所有状态转换通过单一 transition 函数——禁止外部直接修改 repair_state

---

## Acceptance Criteria

### State Machine Core

- [ ] **AC-1**: GIVEN 新游戏启动，WHEN WorldRepair._init_new_game_state()，THEN starlight_dock 节点 repair_state=UNREVEALED, deposited={}, repair_progress=0.0
- [ ] **AC-2**: GIVEN repair_state=UNREVEALED + 玩家物理到达 linked_location_id，WHEN Exploration (#11) 调用 on_player_arrived_at_repair_node("repair_node.starlight_dock")，THEN repair_state→KNOWN。物理到达始终触发——不检查情报系统
- [ ] **AC-3**: GIVEN repair_state=UNREVEALED + 情报系统将 knowledge_state 推进至 ≥identified（未物理到达），WHEN #6 通知，THEN repair_state→KNOWN。情报揭示也可触发节点发现
- [ ] **AC-4**: GIVEN repair_state=KNOWN + 全部 required_resources 满足，WHEN submit_deposit 完成后 repair_completion()==true，THEN repair_state→REPAIRED。单向终态转换

### Invalid Transition Rejection

- [ ] **AC-5**: GIVEN repair_state=KNOWN，WHEN 尝试 known→unrevealed，THEN 状态机拒绝。知识不可退化
- [ ] **AC-6**: GIVEN repair_state=REPAIRED，WHEN 尝试 repaired→known，THEN 状态机拒绝。修复不可撤销
- [ ] **AC-7**: GIVEN repair_state=REPAIRED，WHEN 尝试 repaired→unrevealed，THEN 状态机拒绝。已修复不可遗忘
- [ ] **AC-8**: GIVEN repair_state=REPAIRED + 玩家到达节点位置，WHEN on_player_arrived_at_repair_node() 调用，THEN 状态保持 REPAIRED。不重复触发转换

### Node Lifecycle

- [ ] **AC-9**: GIVEN 玩家首次到达 unrevealed 节点，WHEN 状态→KNOWN，THEN visual_state_anchor 为 "known"（灰暗/破损视觉）。修复交互入口可用——可查看需求清单和提交材料
- [ ] **AC-10**: GIVEN repair_state=KNOWN + 情报系统无知识（knowledge_state < identified），WHEN 查询节点信息用于 UI，THEN 材料清单中未通过情报确认的资源显示为"？"，解锁预览显示"未知效果"。但交互不阻止——物理可见性优先
- [ ] **AC-11**: GIVEN 未注册的 node_id 调用 on_player_arrived_at_repair_node()，WHEN 处理，THEN 记录警告，不崩溃，不创建幽灵节点

### Node Initialization & Registry Integration

- [ ] **AC-12**: GIVEN WorldRepair 在 feature_ready 阶段初始化，WHEN 启动，THEN 从 Registry.query_entity("repair_node.starlight_dock") 读取静态定义。若 Registry 中缺失该节点 → 记录错误，跳过该节点初始化
- [ ] **AC-13**: GIVEN MVP 仅 1 个修复节点，WHEN _init_new_game_state()，THEN 仅 starlight_dock 存在于 repair_nodes Dictionary 中。无额外节点

---

## Implementation Notes

### REPAIR_STATE Enum & Storage Structure

```text
# WorldRepair Autoload #13 — 状态枚举
const REPAIR_STATE_UNREVEALED: int = 0
const REPAIR_STATE_KNOWN: int = 1
const REPAIR_STATE_REPAIRED: int = 2

# 修复节点状态存储
# repair_nodes: Dictionary[StringName, Dictionary]
#   key = node_id (e.g. &"repair_node.starlight_dock")
#   value = {
#     "repair_state": int,       # 0/1/2
#     "deposited": Dictionary,   # Dict[StringName, int] — {resource_id: count}
#     "repair_progress": float,  # 0.0–1.0
#   }
var repair_nodes: Dictionary = {}
```

### State Transition Function

```text
func _transition_state(node_id: StringName, target_state: int) -> bool:
    var current: int = get_repair_state(node_id)

    match target_state:
        REPAIR_STATE_KNOWN:
            if current == REPAIR_STATE_UNREVEALED:
                repair_nodes[node_id]["repair_state"] = REPAIR_STATE_KNOWN
                return true
            # 已是 KNOWN/REPAIRED → 无操作（非错误）
            return false

        REPAIR_STATE_REPAIRED:
            if current == REPAIR_STATE_KNOWN:
                repair_nodes[node_id]["repair_state"] = REPAIR_STATE_REPAIRED
                return true
            # 已是 REPAIRED → 幂等，无操作
            # 是 UNREVEALED → 拒绝（必须先 known）
            push_warning("WorldRepair: invalid transition — cannot go from %d to REPAIRED" % current)
            return false

    # unrevealed 没有合法转入（仅作为初始状态存在）
    push_error("WorldRepair: invalid target_state: %d" % target_state)
    return false
```

### on_player_arrived_at_repair_node

```text
func on_player_arrived_at_repair_node(node_id: StringName) -> void:
    if not repair_nodes.has(node_id):
        push_warning("WorldRepair: unknown repair node — %s" % node_id)
        return

    var current: int = get_repair_state(node_id)
    if current == REPAIR_STATE_UNREVEALED:
        _transition_state(node_id, REPAIR_STATE_KNOWN)
        # visual_state_anchor 切换至 "known"
        visual_state_changed.emit(node_id, &"known")

    # 已是 KNOWN/REPAIRED → 不重复 emit
```

### Node Initialization

```text
func _init_new_game_state() -> void:
    repair_nodes.clear()

    # 从 Registry 读取修复节点静态定义——不硬编码
    var node_defs: Array = Registry.query_by_kind(&"repair_node")
    for node_def in node_defs:
        var node_id: StringName = node_def.get("node_id", &"")
        if node_id == &"":
            push_error("WorldRepair: repair_node entry missing node_id — skipping")
            continue

        repair_nodes[node_id] = {
            "repair_state": REPAIR_STATE_UNREVEALED,
            "deposited": {},
            "repair_progress": 0.0,
        }

    # MVP 守卫：确保 starlight_dock 存在
    if not repair_nodes.has(&"repair_node.starlight_dock"):
        push_error("WorldRepair: MVP node 'repair_node.starlight_dock' not found in Registry")
```

---

## Out of Scope

- Registry.query_by_kind() 的实现——属于 content-registry Epic
- Exploration (#11) 中修复锚点交互检测——属于 exploration-scavenge Epic
- 修复 UI 面板中的材料清单渲染——属于 #16 UIManager
- 情报系统知识推进触发 repair_state 变更——属于 intel-knowledge Epic 的 on_repair_completed 回调

---

## QA Test Cases

- **AC-1**: New game — starlight_dock = UNREVEALED, empty deposited
- **AC-2/3**: Physical arrival / intel reveal → KNOWN
- **AC-4**: All materials committed → REPAIRED
- **AC-5/6/7**: Invalid transitions — KNOWN→UNREVEALED, REPAIRED→KNOWN, REPAIRED→UNREVEALED all rejected
- **AC-8**: Arrival at REPAIRED node → stays REPAIRED
- **AC-10**: No intel — UI shows "？" but interaction available
- **AC-11**: Unknown node_id → warning, no crash
- **AC-12**: Missing Registry definition → error logged, node skipped
- **AC-13**: MVP — only starlight_dock initialized

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/world-repair/WorldRepairStateMachineTest.csproj` — must exist and pass
**Status**: [x] Done — 2026-05-13 — `dotnet run --project tests/unit/world-repair/WorldRepairStateMachineTest.csproj --no-restore` PASS 13/13; `dotnet build CloudWeaverVoyage.sln --no-restore` PASS; `git diff --check` PASS

---

## Dependencies

- Depends on: content-registry Epic (query_by_kind, query_entity), intel-knowledge Epic (knowledge_state 查询), exploration-scavenge Epic (on_player_arrived_at_repair_node 调用)
- Unlocks: Story 002 (deposit_validation 依赖状态机已实现), Story 004 (signal events 依赖状态转换)
