# Story 001: Settlement State Machine & Stall Lifecycle

> **Epic**: Settlement Market & Port Village Economy
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/port-village-market.md`
**Requirement**: `TR-settlement-001`

**ADR Governing Implementation**: ADR-0014 (§1 SettlementManager Autoload #14, §2 Dictionary 后端存储, §5a 三层状态机)
**ADR Decision Summary**: SettlementManager 作为 Autoload #14，Phase 5 feature_ready 初始化。三层状态机——Settlement: DORMANT → RECOVERING → ACTIVE；Stall: CLOSED → OPEN_BASIC → OPEN_EXPANDED (post-MVP)；NPC: ABSENT → IDLE → ACTIVE (post-MVP)。所有转换单向不可逆——修复是永久的，状态只向前推进。无效转换（CLOSED→OPEN_EXPANDED、OPEN_BASIC→CLOSED、ABSENT→ACTIVE、任何反向转换）必须被状态机拒绝。默认杂货摊 (stall.gh-general) 始终 OPEN_BASIC——确保任何修复完成前至少一个购买点。状态存储在三个 Dictionary 中：settlements (Dict[StringName, SettlementState])、stalls (Dict[StringName, StallState])、npcs (Dict[StringName, NpcState])。摊位/NPC 静态定义从 Registry (#1) query_entity 读取，不硬编码。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 默认杂货摊始终 open_basic——不依赖修复信号；所有状态转换通过单一 transition 函数——禁止外部直接修改状态字段；摊位/NPC 静态定义从 Registry 读取——不硬编码
- Forbidden: 任何反向状态转换 (open_basic→closed, recovering→dormant, active→recovering, idle→absent)；CLOSED→OPEN_EXPANDED 跳过中间状态；ABSENT→ACTIVE 跳过中间状态
- Guardrail: MVP 中 OPEN_EXPANDED 和 NPC_ACTIVE 定义但不可达——状态机必须拒绝任何触发这些转换的尝试

---

## Acceptance Criteria

### State Machine Core — Settlement

- [ ] **AC-1**: GIVEN 新游戏启动，WHEN SettlementManager._init_new_game_state()，THEN settlement.glass-harbor settlement_state=DORMANT, completed_node_ids=[]
- [ ] **AC-2**: GIVEN settlement_state=DORMANT + active_stall_count=1（仅杂货摊），WHEN 首个匹配修复完成后 recalculate_settlement_activity()，THEN active_stall_count=2 → settlement_state→RECOVERING
- [ ] **AC-3**: GIVEN settlement_state=RECOVERING + active_stall_count=2-3，WHEN 全部修复完成后 recalculate_settlement_activity()，THEN active_stall_count=4 → settlement_state→ACTIVE
- [ ] **AC-4**: GIVEN settlement_state=RECOVERING + active_stall_count 未达到 total_stall_count，WHEN 检查，THEN 保持 RECOVERING。不提前进入 ACTIVE

### State Machine Core — Stall

- [ ] **AC-5**: GIVEN 新游戏启动，WHEN 初始化，THEN stall.gh-general stall_state=OPEN_BASIC + settlement_id="settlement.glass-harbor"。杂货摊默认开启——无需任何修复
- [ ] **AC-6**: GIVEN 新游戏启动，WHEN 初始化，THEN stall.gh-lens-workshop / stall.gh-sail-shop / stall.gh-chart-studio 均为 CLOSED
- [ ] **AC-7**: GIVEN stall_state=CLOSED + 匹配的 repair_completed 到达，WHEN F.2 解锁判定通过，THEN stall_state→OPEN_BASIC。合法转换
- [ ] **AC-8**: GIVEN stall_state=OPEN_BASIC，WHEN MVP 中没有第二个匹配修复，THEN 保持 OPEN_BASIC。OPEN_EXPANDED 在状态机中定义但不可达

### State Machine Core — NPC

- [ ] **AC-9**: GIVEN 新游戏启动，WHEN 初始化，THEN npc.atu（杂货摊 NPC）npc_state=IDLE, stall_id="stall.gh-general"
- [ ] **AC-10**: GIVEN 新游戏启动，WHEN 初始化，THEN npc.wei / npc.yun / npc.cen 均为 ABSENT
- [ ] **AC-11**: GIVEN npc_state=ABSENT + 所属摊位 closed→open_basic，WHEN 摊位状态变更触发，THEN npc_state→IDLE。NPC 随摊位恢复

### Invalid Transition Rejection

- [ ] **AC-12**: GIVEN stall_state=CLOSED，WHEN 尝试 closed→open_expanded（跳过中间状态），THEN 状态机拒绝。必须经过 OPEN_BASIC
- [ ] **AC-13**: GIVEN stall_state=OPEN_BASIC，WHEN 尝试 open_basic→closed，THEN 状态机拒绝。修复不可逆
- [ ] **AC-14**: GIVEN npc_state=ABSENT，WHEN 尝试 absent→active（跳过中间状态），THEN 状态机拒绝。必须经过 IDLE
- [ ] **AC-15**: GIVEN settlement_state=RECOVERING，WHEN 尝试 recovering→dormant，THEN 状态机拒绝。修复不可逆
- [ ] **AC-16**: GIVEN settlement_state=ACTIVE，WHEN 尝试 active→recovering 或 active→dormant，THEN 状态机拒绝。已激活不可退化

### Registry Integration

- [ ] **AC-17**: GIVEN SettlementManager 在 feature_ready 阶段初始化，WHEN 启动，THEN 从 Registry.query_entity() 读取 settlement/settlement.glass-harbor、stall、npc 静态定义。若缺失关键实体 → 记录错误，跳过该实体初始化
- [ ] **AC-18**: GIVEN MVP 仅有 1 个定居点（琉璃港），WHEN _init_new_game_state()，THEN settlements 仅包含 settlement.glass-harbor。stalls 包含 4 个摊位。npcs 包含 4 个 NPC。无额外实体

---

## Implementation Notes

### State Enums & Storage Structure

```text
# SettlementManager Autoload #14 — 状态枚举
const SETTLEMENT_DORMANT: int = 0
const SETTLEMENT_RECOVERING: int = 1
const SETTLEMENT_ACTIVE: int = 2

const STALL_CLOSED: int = 0
const STALL_OPEN_BASIC: int = 1
const STALL_OPEN_EXPANDED: int = 2  # post-MVP

const NPC_ABSENT: int = 0
const NPC_IDLE: int = 1
const NPC_ACTIVE: int = 2  # post-MVP

# 定居点状态: Dictionary[StringName, Dictionary]
#   key = settlement_id (e.g. &"settlement.glass-harbor")
#   value = { settlement_state: int, completed_node_ids: Array[StringName] }
var settlements: Dictionary = {}

# 摊位状态: Dictionary[StringName, Dictionary]
#   key = stall_id (e.g. &"stall.gh-lens-workshop")
#   value = { stall_state: int, settlement_id: StringName }
var stalls: Dictionary = {}

# NPC 状态: Dictionary[StringName, Dictionary]
#   key = npc_id (e.g. &"npc.wei")
#   value = { npc_state: int, stall_id: StringName }
var npcs: Dictionary = {}
```

### State Transition Functions

```text
func _transition_stall_state(stall_id: StringName, target_state: int) -> bool:
    var current: int = get_stall_state(stall_id)

    match target_state:
        STALL_OPEN_BASIC:
            if current == STALL_CLOSED:
                stalls[stall_id]["stall_state"] = STALL_OPEN_BASIC
                stall_state_changed.emit(stall_id, STALL_CLOSED, STALL_OPEN_BASIC)
                return true
            # 已是 OPEN_BASIC/OPEN_EXPANDED → 幂等
            return false

        STALL_OPEN_EXPANDED:  # post-MVP — MVP 中拒绝所有输入
            if current == STALL_OPEN_BASIC:
                push_warning("Settlement: OPEN_EXPANDED not reachable in MVP")
                return false
            push_error("Settlement: invalid transition — CLOSED→OPEN_EXPANDED rejected")
            return false

    push_error("Settlement: invalid stall target_state: %d" % target_state)
    return false

func _transition_npc_state(npc_id: StringName, target_state: int) -> bool:
    var current: int = get_npc_state(npc_id)

    match target_state:
        NPC_IDLE:
            if current == NPC_ABSENT:
                npcs[npc_id]["npc_state"] = NPC_IDLE
                npc_state_changed.emit(npc_id, NPC_ABSENT, NPC_IDLE)
                return true
            return false

        NPC_ACTIVE:  # post-MVP — MVP 中拒绝所有输入
            if current == NPC_IDLE:
                push_warning("Settlement: NPC_ACTIVE not reachable in MVP")
                return false
            push_error("Settlement: invalid transition — ABSENT→ACTIVE rejected")
            return false

    push_error("Settlement: invalid NPC target_state: %d" % target_state)
    return false
```

### Initialization

```text
func _init_new_game_state() -> void:
    settlements.clear()
    stalls.clear()
    npcs.clear()

    # 从 Registry 读取定居点定义
    var settlement_defs: Array = Registry.query_by_kind(&"settlement")
    for def in settlement_defs:
        var sid: StringName = def.get("entity_id", &"")
        if sid == &"":
            push_error("Settlement: settlement entry missing entity_id — skipping")
            continue
        settlements[sid] = {
            "settlement_state": SETTLEMENT_DORMANT,
            "completed_node_ids": [],
        }

    # 从 Registry 读取摊位定义
    var stall_defs: Array = Registry.query_by_kind(&"stall")
    for def in stall_defs:
        var sid: StringName = def.get("entity_id", &"")
        if sid == &"":
            push_error("Settlement: stall entry missing entity_id — skipping")
            continue
        var is_default: bool = def.get("is_default_open", false)
        stalls[sid] = {
            "stall_state": STALL_OPEN_BASIC if is_default else STALL_CLOSED,
            "settlement_id": def.get("settlement_id", &""),
        }

    # 从 Registry 读取 NPC 定义
    var npc_defs: Array = Registry.query_by_kind(&"market_npc")
    for def in npc_defs:
        var nid: StringName = def.get("entity_id", &"")
        if nid == &"":
            push_error("Settlement: market_npc entry missing entity_id — skipping")
            continue
        var is_default: bool = def.get("is_default_present", false)
        npcs[nid] = {
            "npc_state": NPC_IDLE if is_default else NPC_ABSENT,
            "stall_id": def.get("stall_id", &""),
        }

    # MVP 守卫
    if not settlements.has(&"settlement.glass-harbor"):
        push_error("Settlement: MVP settlement 'settlement.glass-harbor' not found in Registry")
    if not stalls.has(&"stall.gh-general"):
        push_error("Settlement: MVP default stall 'stall.gh-general' not found in Registry")
```

---

## Out of Scope

- Registry.query_by_kind() / Registry.query_entity() 实现——属于 content-registry Epic
- repair_completed 信号消费逻辑——属于 Story 003
- F.3 recalculate_settlement_activity() 聚合公式——属于 Story 003
- 购买流程 validate/execute——属于 Story 002
- ADR-0003 序列化/反序列化——属于 Story 005
- 摊位视觉外观切换（木板封挡 vs 开张）——属于 #17 Feedback 系统

---

## QA Test Cases

- **AC-1**: New game — settlement DORMANT, completed_node_ids=[], 1 stall open, 1 NPC idle
- **AC-2/3**: Settlement state progression DORMANT→RECOVERING→ACTIVE
- **AC-5/6**: Default stall OPEN_BASIC, other 3 CLOSED
- **AC-9/10**: Default NPC IDLE, other 3 ABSENT
- **AC-7**: CLOSED→OPEN_BASIC valid transition
- **AC-12**: CLOSED→OPEN_EXPANDED rejected
- **AC-13**: OPEN_BASIC→CLOSED rejected
- **AC-14**: ABSENT→ACTIVE rejected
- **AC-15**: RECOVERING→DORMANT rejected
- **AC-16**: ACTIVE→RECOVERING / ACTIVE→DORMANT rejected
- **AC-17**: Missing Registry entity → error logged, skipped
- **AC-18**: MVP scope — only glass-harbor entities exist

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/settlement-market/StateMachineTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: content-registry Epic (query_by_kind, query_entity), platform-session-shell Epic (Autoload #14 Phase 5 feature_ready)
- Unlocks: Story 002 (purchase flow depends on stall state), Story 003 (unlock logic depends on state machine)
