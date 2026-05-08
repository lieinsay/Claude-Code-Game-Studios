# Story 002: Search, Scavenge & Intel Formulas

> **Epic**: Exploration / Scavenge Scenario
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/exploration-scavenge-scenario.md`
**Requirement**: `TR-exploration-002`

**ADR Governing Implementation**: ADR-0013 (§4b 搜索与情报接口, §5b F-11-01 搜索产出投骰, §5g F-11-06 情报点产出)
**ADR Decision Summary**: F-11-01 search_yield() 实现自由搜索保证——空结果 search_consumed=false，玩家可继续搜索其他搜索点。按区域（A_core/B_inner/C_mid/D_outer）× 状态变体（unlooted/danger-changed）× 品质档位（Poor/Common/Uncommon）的分层数据表驱动产出。空池守卫——loot_pool 为空时回退为空结果。danger-changed 状态下 empty_chance +0.15、Uncommon 权重 ×0.5。F-11-06 intel_yield() 固定产出 1 个 Q=1 Unique 情报物品。搜索点描述双变体——默认 description + 增强 description_enhanced（由 #6 has_relevant_intel() 门控）。所有数据表从 Registry (#1) 读取，ExplorationManager 内部不硬编码 loot 表。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Feature layer)**:
- Required: 搜索产出必须使用不放回抽样（sample_without_replacement）；空结果 search_consumed=false——搜索点保持可交互；已搜刮的搜索点在本次会话不可再次搜索但 search_consumed 标记为 true；loot_pool 为空时回退为空结果不崩溃
- Forbidden: 硬编码 loot 表或品质权重——必须从 Registry 读取；在 search_yield 外部直接修改搜索点消耗状态；搜索时绕过 Pool 5 容量检查
- Guardrail: search_yield 输入验证——无效 sp_id 返回空结果 + warning 日志，不崩溃

---

## Acceptance Criteria

### F-11-01: search_yield Core

- [ ] **AC-1**: GIVEN state=UNLOOTED + zone=A_core (empty_chance=0.00)，WHEN search_yield() 100次，THEN 0 次空结果。A_core 空概率严格为 0
- [ ] **AC-2**: GIVEN state=UNLOOTED + zone=D_outer (empty_chance=0.35)，WHEN search_yield() 1000次，THEN 空结果比例 ∈ [0.25, 0.45]（统计置信区间）
- [ ] **AC-3**: GIVEN state=LOOTED + 任意搜索点，WHEN search_yield()，THEN 返回 {items: [], is_empty: true, search_consumed: false, message: "这里已经被搜过了"}
- [ ] **AC-4**: GIVEN state=UNLOOTED + loot_pool[sp_id][tier] 为空，WHEN search_yield() 抽中该 tier，THEN 返回空结果 + search_consumed=false + message 不为空。空池守卫生效
- [ ] **AC-5**: GIVEN 搜索产出非空 + 物品可装入 Pool 5，WHEN perform_search(sp_id)，THEN search_consumed=true + 物品进入 Pool 5 + search_performed 发射 + item_picked_up 发射（每种物品 1 次）
- [ ] **AC-6**: GIVEN 搜索产出为空 (is_empty=true)，WHEN perform_search(sp_id)，THEN search_consumed=false + search_performed 发射 (is_empty=true)。该搜索点保持可交互

### Quality Tier Weights

- [ ] **AC-7**: GIVEN state=UNLOOTED + zone=A_core (Poor:0.20, Common:0.45, Uncommon:0.35)，WHEN search_yield() 1000次，THEN 各 tier 产出比例接近设定权重（±5% 容差）
- [ ] **AC-8**: GIVEN state=UNLOOTED + zone=C_mid (Poor:0.35, Common:0.40, Uncommon:0.25)，WHEN search_yield() 1000次，THEN 各 tier 产出比例接近设定权重
- [ ] **AC-9**: GIVEN state=DANGER_CHANGED，WHEN search_yield()，THEN empty_chance 在原始基础上 +0.15；Uncommon 权重 = 原始 ×0.5；差额加给 Poor

### Draw Count

- [ ] **AC-10**: GIVEN 抽中 tier=Poor, draw_count={min:1, max:2}，WHEN search_yield()，THEN 产出物品数 ∈ [1, 2]
- [ ] **AC-11**: GIVEN loot_pool 条目数 < draw_count，WHEN 不放回抽取，THEN 产出数 = min(draw_count, pool.size())。不越界

### Intel Yield (F-11-06)

- [ ] **AC-12**: GIVEN 未交互的情报点，WHEN perform_intel_interaction(intel_point_id)，THEN 返回 {intel_id: StringName, is_empty: false} + intel_discovered 信号发射
- [ ] **AC-13**: GIVEN 已在本次会话中交互过的情报点，WHEN 再次 perform_intel_interaction()，THEN 返回空结果——情报点已枯竭
- [ ] **AC-14**: GIVEN 情报产出 + Pool 5 有空间，WHEN 执行，THEN Q=1 Unique 情报物品进入 Pool 5。Unique 物品 max_stack=1

### Search Point Description Gating

- [ ] **AC-15**: GIVEN 搜索点 + IntelManager.has_relevant_intel(sp_id)=false，WHEN get_search_point_description(sp_id)，THEN 返回 description（默认文字）
- [ ] **AC-16**: GIVEN 搜索点 + IntelManager.has_relevant_intel(sp_id)=true，WHEN get_search_point_description(sp_id)，THEN 返回 description_enhanced（增强文字）
- [ ] **AC-17**: GIVEN state_variant 变化（unlooted→looted→danger-changed），WHEN get_search_point_description(sp_id)，THEN 返回对应状态变体的文字对。每个变体有独立的 description/description_enhanced

### Capacity Gating (EC-11-04/05)

- [ ] **AC-18**: GIVEN Pool 5 已满 (5/5) + 搜索产出非空 + 无可堆叠合并，WHEN perform_search()，THEN capacity_warning 信号发射 + 搜索点 search_consumed 保持 false + 物品暂存等待取舍
- [ ] **AC-19**: GIVEN Pool 5 已满 + 情报产出 Unique 物品，WHEN perform_intel_interaction()，THEN capacity_warning 发射 + 附加 Unique 物品警告标记
- [ ] **AC-20**: GIVEN 搜索产出 resource_id 与 Pool 5 某格相同 + stackable=true + 合并后不超 max_stack，WHEN perform_search()，THEN 自动静默合并——不弹窗。溢出部分触发取舍

---

## Implementation Notes

### search_yield Implementation

```gdscript
func search_yield(sp_id: StringName, state: int, zone: StringName) -> Dictionary:
    if state == STATE_LOOTED:
        return {items: [], is_empty: true, search_consumed: false,
                message: "这里已经被搜过了"}

    # 从 Registry 读取数据表
    var empty_chance := Registry.get_config_float("exploration", "empty_chance.%s.%s" % [_state_to_key(state), zone], 0.0)
    if randf() < empty_chance:
        return {items: [], is_empty: true, search_consumed: false}

    var quality_weights := Registry.get_config_dict("exploration", "quality_weights.%s.%s" % [_state_to_key(state), zone], {})
    var tier := _weighted_random_tier(quality_weights)

    var sp_def := Registry.query_entity(sp_id)
    var pool: Array = sp_def.get("loot_pool", {}).get(tier, [])

    if pool.size() == 0:
        return {items: [], is_empty: true, search_consumed: false,
                message: "这里似乎还能找到些什么，但已经什么都没有了——或许下次再来？"}

    var draw_cfg := DRAW_COUNT_TABLE[tier]
    var draw_count := randi_range(draw_cfg.min, draw_cfg.max)
    var selected := _sample_without_replacement(pool, mini(draw_count, pool.size()))

    # 解析物品数量
    var items := []
    for entry in selected:
        var rid: StringName = entry.resource_id
        var qty_range: Array = entry.quantity_range
        var qty := randi_range(qty_range[0], qty_range[1])
        items.append({resource_id: rid, quantity: qty})

    return {items: items, is_empty: false, search_consumed: true}
```

### Quality Tier Weighted Random

```gdscript
func _weighted_random_tier(weights: Dictionary) -> StringName:
    var total: float = 0.0
    for w in weights.values():
        total += w
    if total <= 0.0:
        return TIER_POOR  # fallback
    var roll := randf() * total
    var cumulative: float = 0.0
    for tier in weights:
        cumulative += weights[tier]
        if roll <= cumulative:
            return tier
    return TIER_POOR  # fallback
```

### Data Tables (from Registry)

```gdscript
# empty_chance 表 — Registry key: "exploration.empty_chance"
# unlooted: {A_core: 0.00, B_inner: 0.05, C_mid: 0.20, D_outer: 0.35}
# danger-changed: {A_core: 0.15, B_inner: 0.20, C_mid: 0.35, D_outer: 0.50}

# quality_weights 表 — Registry key: "exploration.quality_weights"
# unlooted: {A_core: {poor:0.20, common:0.45, uncommon:0.35}, ...}
# danger-changed: {A_core: {poor:0.375, common:0.45, uncommon:0.175}, ...}

# draw_count 表 (不与区域/状态关联)
const DRAW_COUNT_TABLE := {
    TIER_POOR: {min: 1, max: 2},
    TIER_COMMON: {min: 1, max: 2},
    TIER_UNCOMMON: {min: 1, max: 2},
}
```

### Intel Yield

```gdscript
func perform_intel_interaction(intel_point_id: StringName) -> Dictionary:
    if session_intel_interacted.get(intel_point_id, false):
        return {intel_id: &"", is_empty: true, message: "此处已调查过"}

    var intel_id: StringName = _intel_config(intel_point_id).intel_id

    # 容量检查 (Unique 物品占用 1 格)
    if not ResourcesManager.can_add_to_pool(5, intel_id, 1):
        capacity_warning.emit(5, 5)
        return {intel_id: intel_id, is_empty: false, capacity_blocked: true}

    ResourcesManager.add_loot(intel_id, 1)
    session_intel_interacted[intel_point_id] = true
    intel_discovered.emit(intel_id)

    # 写入 IntelManager
    IntelManager.reveal_from_exploration(intel_id)

    return {intel_id: intel_id, is_empty: false}
```

### Search Point Description

```gdscript
func get_search_point_description(sp_id: StringName) -> String:
    var sp_def := Registry.query_entity(sp_id)
    var state_key := _state_to_key(get_exploration_point_state(current_exploration_point_id))
    var desc_key: String = "description_enhanced" if IntelManager.has_relevant_intel(sp_id) else "description"
    # 按状态变体选择文字对
    return sp_def.get("%s.%s" % [state_key, desc_key], sp_def.get(desc_key, ""))
```

---

## Out of Scope

- 威胁触发判定——属于 Story 003
- 提取与结算——属于 Story 005
- Pool 5 容量管理的内部实现（add_loot, can_add_to_pool）——属于 resources-goods-capacity Epic
- IntelManager.has_relevant_intel() 的实现——属于 intel-knowledge Epic
- 取舍界面的 UI 渲染——属于 #16 UIManager

---

## QA Test Cases

- **AC-1–2**: empty_chance 区域正确性 + 统计验证
- **AC-3**: LOOTED state → "已搜过"
- **AC-5–6**: search_consumed 正确设置（非空=true, 空=false）
- **AC-7–9**: Quality weights 统计验证 + danger-changed 修正
- **AC-10–11**: Draw count bounds + cap at pool size
- **AC-12–14**: Intel yield fixed output +枯竭 + Pool 5 容量
- **AC-15–17**: Description gating (has_relevant_intel + state variant)
- **AC-18–20**: Capacity gating trade-off scenarios

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/exploration/search_scavenge_test.gd` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (状态机), resources-goods-capacity Epic (Pool 5 add_loot/can_add_to_pool), intel-knowledge Epic (has_relevant_intel, reveal_from_exploration), content-registry Epic (loot_pool 数据表)
- Unlocks: Story 005 (extraction settlement 消费 Pool 5 状态)
