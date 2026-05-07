# Story 006: Life Trace Anchors

> **Epic**: Airship Hub
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/airship-hub.md`
**Requirement**: `TR-hub-001`

**ADR Governing Implementation**: ADR-0001 (HubManager Autoload #7 — 痕迹锚点查询接口), ADR-0003 (Save System — trace_anchors 在 progress.airship 中持久化)
**ADR Decision Summary**: Hub 暴露 4 个可持久化"痕迹锚点"——少量枚举状态标记，由存档系统在 progress.airship 快照包中保存和恢复。痕迹锚点不产生机械效果，只为满足存档 GDD 的"这仍是我的家"最低生活痕迹要求。4 个锚点各有独立的数据源和 tier 映射：情报台海图（Intel 查询）、货架占用（Resources 容量）、伙伴巢穴（Partner query_nest_state() 4 阶段）、船体修补（WorldRepair 查询）。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 4 个痕迹锚点必须在 progress.airship 中持久化；各锚点的 tier 映射由对应领域系统数据驱动
- Forbidden: 痕迹锚点产生机械效果（遭遇率、效率、容量等）——它们是纯视觉标记；在快照中存储派生 tier 值而非源数据查询结果缓存
- Guardrail: 伙伴巢穴 4 阶段由 #15 query_nest_state() 驱动——Hub 不拥有巢穴状态机，仅持有 tier 镜像

---

## Acceptance Criteria

### Trace Anchor Definitions

- [ ] **AC-1**: GIVEN Hub 已初始化，WHEN 查询 trace_anchors，THEN 返回恰好 4 个锚点：chart_notes、storage_fullness、nest_accumulation、hull_repairs
- [ ] **AC-2**: GIVEN 每个锚点，WHEN 检查属性，THEN 含 trace_id（stable ID）、current_tier（int 0-based）、max_tier、display_name、data_source（领域系统查询路径）

### Chart Notes Anchor (情报台海图)

- [ ] **AC-3**: GIVEN 玩家从未访问情报台或情报系统无已知航线，WHEN 查询 chart_notes tier，THEN tier = 0（干净——海图空白）
- [ ] **AC-4**: GIVEN 玩家已访问情报台且有 1-3 条已知航线，WHEN 查询 chart_notes tier，THEN tier = 1（有笔记——海图出现手写标记和航线）
- [ ] **AC-5**: GIVEN 玩家已访问情报台且有 ≥4 条已知航线或已解锁航线知识能力，WHEN 查询 chart_notes tier，THEN tier = 2（写满了——海图布满标记、手绘修正和密集航线网）
- [ ] **AC-6**: GIVEN chart_notes tier 变化，WHEN 新 tier ≠ 旧 tier，THEN Hub 更新视觉表现（海图 sprite 切换到对应 tier 变体）

### Storage Fullness Anchor (货架占用)

- [ ] **AC-7**: GIVEN 仓库 used_volume ≤ 总容量的 33%，WHEN 查询 storage_fullness tier，THEN tier = 0（空——货架空荡，少量物品）
- [ ] **AC-8**: GIVEN 仓库 used_volume 在 34%-75%，WHEN 查询 storage_fullness tier，THEN tier = 1（部分满——货架有可见摆放但仍有空间）
- [ ] **AC-9**: GIVEN 仓库 used_volume > 75%，WHEN 查询 storage_fullness tier，THEN tier = 2（满——货架充实、物品紧密排列）

### Nest Accumulation Anchor (伙伴巢穴)

- [ ] **AC-10**: GIVEN PartnerSystem.query_nest_state() = 0 (empty)，WHEN 查询 nest_accumulation tier，THEN tier = 0——伙伴驻点角落为空
- [ ] **AC-11**: GIVEN query_nest_state() = 1 (first_object — 旧船帆碎布)，WHEN Hub 同步 nest tier，THEN tier = 1——伙伴驻点出现第一件叙事物品
- [ ] **AC-12**: GIVEN query_nest_state() = 2 (accumulating — 锈蚀的测风链环)，WHEN Hub 同步，THEN tier = 2——巢穴有新物品加入
- [ ] **AC-13**: GIVEN query_nest_state() = 3 (full — 玩家绳头 + 空港徽章残片)，WHEN Hub 同步，THEN tier = 3——巢穴完整，4 件叙事物品俱全，传达"有人在船上住了下来"
- [ ] **AC-14**: GIVEN 伙伴未在队伍中（未招募），WHEN 查询 nest_accumulation tier，THEN tier = 0——无伙伴则无巢穴

### Hull Repairs Anchor (船体修补痕迹)

- [ ] **AC-15**: GIVEN 从未完成世界修复事件或 WorldRepair 无已完成修复记录，WHEN 查询 hull_repairs tier，THEN tier = 0（无——船体无可见修补）
- [ ] **AC-16**: GIVEN 完成 1-2 次世界修复，WHEN 查询 hull_repairs tier，THEN tier = 1（有旧伤——船体可见旧修补痕迹）
- [ ] **AC-17**: GIVEN 完成 ≥3 次世界修复且最近一次在本次返航中，WHEN 查询 hull_repairs tier，THEN tier = 2（有新伤+旧伤补丁——新旧修补痕迹层叠可见）

### Trace Anchor Data Flow

- [ ] **AC-18**: GIVEN trace_anchors 持久化在 progress.airship 中，WHEN Hub 从快照加载，THEN 所有锚点的 tier 值恢复——不重新查询领域系统（快照中的 tier 是权威值）
- [ ] **AC-19**: GIVEN 游戏运行中（非加载），WHEN 领域系统状态变化（如仓库容量变化、新修复完成），THEN Hub 重新查询对应数据源、计算新 tier——若 tier 变化则更新视觉和内存中的 trace_anchors 字典

---

## Implementation Notes

### Trace Anchor Data Model

```gdscript
enum TraceAnchorID {
    CHART_NOTES,
    STORAGE_FULLNESS,
    NEST_ACCUMULATION,
    HULL_REPAIRS,
}

const TRACE_ANCHOR_COUNT: int = 4

# 每个锚点的持久化结构
# {
#     "tier": int,       # 0-based 当前 tier
#     "max_tier": int,   # 该锚点的最大 tier 值
#     "updated_at": int, # 最后更新时的 docking_state（用于调试）
# }

var _trace_anchors: Dictionary = {}  # Dict[int, Dictionary]
```

### Tier Mapping Functions

```gdscript
# === Chart Notes — 数据源：IntelManager ===

func _derive_chart_notes_tier() -> int:
    var known_routes: int = IntelManager.get_known_route_count()
    var has_ability: bool = IntelManager.query_ability_state(&"route_knowledge") >= AbilityState.UNLOCKED

    if known_routes >= 4 or has_ability:
        return 2  # 写满了
    elif known_routes >= 1:
        return 1  # 有笔记
    else:
        return 0  # 干净


# === Storage Fullness — 数据源：ResourcesManager ===

func _derive_storage_fullness_tier() -> int:
    var usage: Dictionary = ResourcesManager.get_storage_summary()
    var used: int = usage.get("used_volume", 0)
    var capacity: int = usage.get("total_capacity", 1000)

    if capacity == 0:
        return 0

    var ratio: float = float(used) / float(capacity)
    if ratio > 0.75:
        return 2  # 满
    elif ratio > 0.33:
        return 1  # 部分满
    else:
        return 0  # 空


# === Nest Accumulation — 数据源：PartnerSystem (#15) ===

func _derive_nest_accumulation_tier() -> int:
    if not _is_partner_in_crew():
        return 0  # 无伙伴 → 无巢穴

    # query_nest_state() 返回 0-3 的 4 阶段值
    return PartnerSystem.query_nest_state()


# === Hull Repairs — 数据源：WorldRepair (#13) ===

func _derive_hull_repairs_tier() -> int:
    var total_repairs: int = WorldRepair.get_completed_repair_count()
    var has_recent: bool = WorldRepair.has_repair_since_last_departure()

    if total_repairs >= 3 and has_recent:
        return 2  # 有新伤+旧伤补丁
    elif total_repairs >= 1:
        return 1  # 有旧伤
    else:
        return 0  # 无
```

### Trace Anchor Refresh Cycle

```gdscript
# 在以下时机刷新所有痕迹锚点：
# 1. arrival → landed 转换完成（_on_landed）
# 2. 玩家在 Hub 中完成影响锚点的操作后（如拆包入库、模块检查）

func _refresh_trace_anchors() -> void:
    var new_tiers: Dictionary = {
        TraceAnchorID.CHART_NOTES: _derive_chart_notes_tier(),
        TraceAnchorID.STORAGE_FULLNESS: _derive_storage_fullness_tier(),
        TraceAnchorID.NEST_ACCUMULATION: _derive_nest_accumulation_tier(),
        TraceAnchorID.HULL_REPAIRS: _derive_hull_repairs_tier(),
    }

    for anchor_id in new_tiers:
        var new_tier: int = new_tiers[anchor_id]
        var old_tier: int = _trace_anchors.get(anchor_id, {}).get("tier", -1)

        if new_tier != old_tier:
            _trace_anchors[anchor_id] = {
                "tier": new_tier,
                "max_tier": _get_max_tier(anchor_id),
                "updated_at": docking_state,
            }
            _on_trace_anchor_changed(anchor_id, old_tier, new_tier)


func _on_trace_anchor_changed(anchor_id: int, old_tier: int, new_tier: int) -> void:
    # 触发视觉更新——具体呈现由 Visual/Feel 类型负责
    _update_anchor_visual(anchor_id, new_tier)
    # 发射信号供 UI 系统（若有相关 HUD 元素）
    trace_anchor_changed.emit(anchor_id, new_tier)
```

### Signal Definition

```gdscript
# ADR-0002 compliant: {noun}_{verb_past}
signal trace_anchor_changed(anchor_id: int, new_tier: int)
```

### Max Tier Constants

```gdscript
const TRACE_ANCHOR_MAX_TIERS: Dictionary = {
    TraceAnchorID.CHART_NOTES: 2,       # 0=干净, 1=有笔记, 2=写满了
    TraceAnchorID.STORAGE_FULLNESS: 2,  # 0=空, 1=部分满, 2=满
    TraceAnchorID.NEST_ACCUMULATION: 3, # 0=empty, 1=first_object, 2=accumulating, 3=full
    TraceAnchorID.HULL_REPAIRS: 2,      # 0=无, 1=有旧伤, 2=有新伤+旧伤补丁
}

func _get_max_tier(anchor_id: int) -> int:
    return TRACE_ANCHOR_MAX_TIERS.get(anchor_id, 1)
```

### Snapshot Integration (for Story 008)

```gdscript
# trace_anchors 作为 progress.airship 的一部分持久化
func _build_trace_anchor_snapshot() -> Dictionary:
    var snapshot: Dictionary = {}
    for anchor_id in _trace_anchors:
        snapshot[anchor_id] = _trace_anchors[anchor_id].duplicate()
    return snapshot

func _restore_trace_anchors(snapshot: Dictionary) -> void:
    _trace_anchors.clear()
    for anchor_id in snapshot:
        _trace_anchors[int(anchor_id)] = snapshot[anchor_id].duplicate()
```

### Partner Nest Narrative Items Mapping

```gdscript
# 4 阶段巢穴的叙事物品——由 PartnerSystem #15 R11 定义
# Hub 仅持有 tier→物品名称映射用于 display_hint / UI 展示
const NEST_STAGE_ITEMS: Dictionary = {
    1: "旧船帆碎布",
    2: "锈蚀的测风链环",
    3: "玩家绳头",
    4: "空港徽章残片",
}

func get_nest_stage_display(tier: int) -> String:
    if tier <= 0:
        return "空"
    return " · ".join(_get_accumulated_items(tier))

func _get_accumulated_items(tier: int) -> Array:
    var items: Array = []
    for i in range(1, tier + 1):
        if NEST_STAGE_ITEMS.has(i):
            items.append(NEST_STAGE_ITEMS[i])
    return items
```

---

## Out of Scope

- 痕迹锚点的具体视觉资产（sprite 变体、动画过渡）——属于 Visual/Feel 类型（截图+sign-off 验证）
- 伙伴巢穴 4 阶段的状态机和叙事物品定义——属于 PartnerSystem #15 R11
- 世界修复记录的查询接口——属于 WorldRepair #13
- 情报台航线数量查询的具体实现——属于 IntelManager #6
- 痕迹锚点 tier 变化的 HUD 通知——属于 UI 系统 #16

---

## QA Test Cases

- **AC-3 through AC-6**: Chart notes tier
  - Given: 0 known routes → chart_notes tier = 0
  - Given: 2 known routes → chart_notes tier = 1
  - Given: 5 known routes → chart_notes tier = 2

- **AC-7 through AC-9**: Storage fullness tier
  - Given: used=200, capacity=1000 → ratio=0.20 → tier = 0
  - Given: used=500, capacity=1000 → ratio=0.50 → tier = 1
  - Given: used=900, capacity=1000 → ratio=0.90 → tier = 2

- **AC-10 through AC-14**: Nest accumulation tier
  - Given: partner not in crew → tier = 0
  - Given: query_nest_state() = 2 → tier = 2

- **AC-18 and AC-19**: Data flow
  - Given: 快照加载 → tier 恢复为快照值（不重新查询）
  - Given: 运行中仓库容量变化 → _refresh_trace_anchors() → tier 重新计算

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/integration/hub/trace_anchors_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (scene/state machine — _on_landed refresh trigger), Story 005 (arrival flow — refresh timing), intel-knowledge Epic (get_known_route_count, query_ability_state), resources-goods-capacity Epic (get_storage_summary), partner-system Epic (query_nest_state), world-repair Epic (get_completed_repair_count, has_repair_since_last_departure), local-save-persistence Epic (trace_anchors in progress.airship)
- Unlocks: Story 008 (trace_anchors snapshot serialization), Story 007 (trace_anchor_changed signal)
