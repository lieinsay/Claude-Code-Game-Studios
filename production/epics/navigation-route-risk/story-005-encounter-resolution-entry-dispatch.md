# Story 005: Encounter Resolution & EncounterEntry Dispatch

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-001`, `TR-navigation-002`

**ADR Governing Implementation**: ADR-0010 (EncounterContext — EncounterEntry 子结构, 12 encounter_type 常量, 10 special_effect_tags 常量)
**ADR Decision Summary**: 每条航线有遭遇表映射 `hazard_tag → EncounterEntry[]`。每次遭遇检查触发时，对每个可见标签从其遭遇表中等概率抽取一个条目，对每个隐藏标签先判定 P_reveal=0.30 再抽取。多标签同时命中时，d_check = max(d_entry_1, ..., d_entry_k) 取最大值而非求和。storm_eye_passage 覆盖 P_reveal=1.0（揭示所有隐藏标签）。遭遇条目产出后，立即发出 encounter_triggered 信号给 #17，并应用特殊效果到当前航行状态。MVP 定义了 3 个遭遇表（safe/storm/low-visibility）共 12 个条目。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 每次检查对所有可见标签抽取遭遇条目；d_check = max() 非累加；遭遇条目产出后立即发出 encounter_triggered 信号；特殊效果在遭遇结算时立即应用——不等待航程结束
- Forbidden: 跳过隐藏标签的 reveal 判定直接读取其遭遇表；将多标签伤害叠加（如 3+4=7）；在未进行 reveal 判定的情况下消费隐藏标签的遭遇表
- Guardrail: 每个标签的遭遇表概率总和必须归一化到 100%；单次遭遇伤害上限 6 点

---

## Acceptance Criteria

### Encounter Table Lookup

- [ ] **AC-1**: GIVEN visible tag=`safe` + encounter table loaded，WHEN draw_encounter_entry("safe")，THEN 从 safe 表的 4 个条目中等概率抽取一个。概率分布：calm_passage 40%, gentle_crosswind 35%, minor_debris 20%, scenic_discovery 5%
- [ ] **AC-2**: GIVEN visible tag=`storm` + encounter table loaded，WHEN draw，THEN 从 storm 表的 5 个条目中等概率抽取：storm_cell_edge 30%, turbulence_zone 25%, lightning_proximity 20%, wind_shear 15%, storm_eye_passage 10%
- [ ] **AC-3**: GIVEN visible tag=`low-visibility` + 已揭示，WHEN draw，THEN 从 low-visibility 表的 3 个条目中等概率抽取：dense_fog_bank 40%, hidden_reef_proximity 35%, false_horizon 25%

### Multi-Tag Resolution (max rule)

- [ ] **AC-4**: GIVEN 一次检查命中 storm→turbulence_zone(d=3) + low-visibility→hidden_reef_proximity(d=4)，WHEN _resolve_encounter_check()，THEN d_check = max(3, 4) = 4。resolved_encounters 追加 2 个条目——每条命中标签独立记录
- [ ] **AC-5**: GIVEN 一次检查命中 3 个标签 d={2, 0, 5}，WHEN d_check，THEN = max(2, 0, 5) = 5
- [ ] **AC-6**: GIVEN 所有命中条目的 d_entry 均为 0（如 calm_passage(0) + storm_eye_passage(0)），WHEN d_check，THEN = 0。非伤害效果（揭示标签、减速）仍然应用

### Special Effects Application

- [ ] **AC-7**: GIVEN 抽取到 gentle_crosswind（effect=`voyage_duration_penalty_5s`），WHEN 应用效果，THEN ΣT_flat += 5。T_voyage 增加 5s
- [ ] **AC-8**: GIVEN 抽取到 turbulence_zone（effect=`speed_penalty_15pct`），WHEN 应用效果，THEN _temp_time_penalties 增加等效值（约 +3s 对 12s 周期）。效果在下一检查结算后过期
- [ ] **AC-9**: GIVEN 抽取到 wind_shear（effect=`next_check_early_5s`），WHEN 应用效果，THEN _next_check_offset -= 5s。若多次叠加，受 T_check_min=4s 硬下限约束
- [ ] **AC-10**: GIVEN 抽取到 lightning_proximity（effect=`module_damage_20pct_scout`），WHEN 应用效果 + 20% randf() 命中，THEN ModuleHullManager.apply_module_damage(scout_slot, "lightning_strike")。η_scout 立即更新——预览窗口重算
- [ ] **AC-11**: GIVEN 抽取到 storm_eye_passage（effect=`reveal_all_hidden_tags`），WHEN 应用效果，THEN 调用 reveal_all_hidden_tags()——所有隐藏标签 P_reveal=1.0 强制揭露
- [ ] **AC-12**: GIVEN 抽取到 dense_fog_bank（effect=`scout_window_halved_next`），WHEN 应用效果，THEN 下一次遭遇检查的 T_preview 减半。效果在下一检查后恢复
- [ ] **AC-13**: GIVEN 抽取到 hidden_reef_proximity（effect=`bypass_scout`），WHEN 应用，THEN 本遭遇不出现在侦察预览中——即使 η_scout>0，该遭遇也仅在触发时才显示
- [ ] **AC-14**: GIVEN 抽取到 false_horizon（effect=`time_estimate_bias_15pct`），WHEN 应用效果，THEN UI 显示的剩余时间估算偏离 ±15%。实际 T_voyage 和遭遇计时不受影响

### Hidden Tag Reveal in Encounter Resolution

- [ ] **AC-15**: GIVEN hidden_tag=`low-visibility` + P_reveal=0.30，WHEN 每次遭遇检查前判定，THEN 30% 概率揭露。揭露后该标签参与当次检查的遭遇抽取——reveal 判定在 encounter draw 之前
- [ ] **AC-16**: GIVEN hidden_tag 已在前次检查中被揭露，WHEN 后续检查，THEN 不再重复判定。该标签此后作为 visible tag 参与抽取
- [ ] **AC-17**: GIVEN storm_eye_passage 触发 + 存在未揭露的隐藏标签，WHEN _apply_special_effects()，THEN 所有 hidden_tags 立即揭露。已在 _revealed_hidden_tags 中的类不再重复处理

### EncounterEntry Structure

- [ ] **AC-18**: GIVEN 遭遇条目抽取完成，WHEN 构建 EncounterEntry Dictionary，THEN 包含 6 个字段：encounter_type, hazard_tag, damage_amount, special_effect_tags, was_hidden, time_offset
- [ ] **AC-19**: GIVEN 条目来自 visible tag，WHEN was_hidden，THEN = false
- [ ] **AC-20**: GIVEN 条目来自 hidden tag（经 reveal 判定后揭露），WHEN was_hidden，THEN = true。time_offset 记录航行开始至此遭遇的 elapsed_time

### Encounter Dispatch Signal

- [ ] **AC-21**: GIVEN 遭遇条目构建完成，WHEN 发射信号，THEN encounter_triggered.emit(entry: Dictionary)——typed Dictionary。在遭遇结算完成、d_check 计算后、波段转换检查前发射
- [ ] **AC-22**: GIVEN 同一次检查命中 2 个标签，WHEN 发射信号，THEN 每个 EncounterEntry 独立发射一次 encounter_triggered。共 2 次 emit

### Encounter Table Validation

- [ ] **AC-23**: GIVEN 遭遇表加载完成，WHEN 验证，THEN 每个标签的条目概率总和 = 100%（1.0）。偏离超过 0.01 时记录警告并归一化
- [ ] **AC-24**: GIVEN 请求的 hazard_tag 在遭遇表中不存在，WHEN draw_encounter_entry("unknown_tag")，THEN 返回空条目 {d_entry=0, no effects} 并记录警告。不崩溃

---

## Implementation Notes

### Encounter Table Definition

```text
const ENCOUNTER_TABLES: Dictionary = {
    &"safe": [
        {"type": &"calm_passage",          "weight": 0.40, "d_min": 0, "d_max": 0, "effects": []},
        {"type": &"gentle_crosswind",      "weight": 0.35, "d_min": 0, "d_max": 0, "effects": [&"voyage_duration_penalty_5s"]},
        {"type": &"minor_debris",          "weight": 0.20, "d_min": 1, "d_max": 2, "effects": []},
        {"type": &"scenic_discovery",      "weight": 0.05, "d_min": 0, "d_max": 0, "effects": [&"reveal_landmark"]},
    ],
    &"storm": [
        {"type": &"storm_cell_edge",       "weight": 0.30, "d_min": 1, "d_max": 3, "effects": [&"minor_slow"]},
        {"type": &"turbulence_zone",       "weight": 0.25, "d_min": 2, "d_max": 4, "effects": [&"speed_penalty_15pct"]},
        {"type": &"lightning_proximity",   "weight": 0.20, "d_min": 3, "d_max": 6, "effects": [&"module_damage_20pct_scout"]},
        {"type": &"wind_shear",            "weight": 0.15, "d_min": 1, "d_max": 2, "effects": [&"next_check_early_5s"]},
        {"type": &"storm_eye_passage",     "weight": 0.10, "d_min": 0, "d_max": 0, "effects": [&"reveal_all_hidden_tags"]},
    ],
    &"low-visibility": [
        {"type": &"dense_fog_bank",        "weight": 0.40, "d_min": 0, "d_max": 0, "effects": [&"scout_window_halved_next"]},
        {"type": &"hidden_reef_proximity", "weight": 0.35, "d_min": 2, "d_max": 4, "effects": [&"bypass_scout"]},
        {"type": &"false_horizon",         "weight": 0.25, "d_min": 0, "d_max": 0, "effects": [&"time_estimate_bias_15pct"]},
    ],
}

func _validate_encounter_tables() -> void:
    for tag in ENCOUNTER_TABLES:
        var entries: Array = ENCOUNTER_TABLES[tag]
        var total: float = 0.0
        for entry in entries:
            total += entry["weight"]
        if absf(total - 1.0) > 0.01:
            push_warning("Encounter table %s weights sum to %.3f — normalizing" % [tag, total])
            for entry in entries:
                entry["weight"] /= total
```

### Encounter Entry Drawing

```text
func _draw_encounter_entry(hazard_tag: StringName) -> Dictionary:
    var table: Array = ENCOUNTER_TABLES.get(hazard_tag, [])
    if table.is_empty():
        push_warning("Navigation: no encounter table for tag %s" % hazard_tag)
        return _build_empty_entry(hazard_tag)

    var roll: float = randf()
    var cumulative: float = 0.0
    for entry_def in table:
        cumulative += entry_def["weight"]
        if roll <= cumulative:
            var d_entry: int = randi_range(entry_def["d_min"], entry_def["d_max"]) \
                if entry_def["d_max"] > 0 else 0
            var effects: Array = entry_def["effects"].duplicate()
            return {
                "encounter_type": entry_def["type"],
                "hazard_tag": hazard_tag,
                "damage_amount": d_entry,
                "special_effect_tags": effects,
                "was_hidden": false,  # caller sets this
                "time_offset": _elapsed_time,
            }
    return _build_empty_entry(hazard_tag)


func _build_empty_entry(hazard_tag: StringName) -> Dictionary:
    return {
        "encounter_type": &"none",
        "hazard_tag": hazard_tag,
        "damage_amount": 0,
        "special_effect_tags": [],
        "was_hidden": false,
        "time_offset": _elapsed_time,
    }
```

### Full Encounter Resolution

```text
func _resolve_encounter_check() -> void:
    # 1. 对隐藏标签进行 reveal 判定（在抽取前）
    _process_hidden_tag_reveals()

    # 2. 收集所有命中遭遇条目
    var hits: Array[Dictionary] = []
    var visible_tags: Array = _active_voyage.get("visible_hazard_tags", [])
    var hidden_tags: Array = _active_voyage.get("hidden_hazard_tags", [])

    for tag in visible_tags:
        var entry: Dictionary = _draw_encounter_entry(tag)
        if entry["encounter_type"] != &"none":
            hits.append(entry)

    for tag in hidden_tags:
        if tag in _revealed_hidden_tags:
            var entry: Dictionary = _draw_encounter_entry(tag)
            entry["was_hidden"] = true
            if entry["encounter_type"] != &"none":
                hits.append(entry)

    # 3. 计算 d_check (max rule)
    var d_check: int = _max_damage(hits)

    # 4. 累积伤害
    _accumulated_damage += d_check
    _last_check_time = _elapsed_time

    # 5. 记录所有已结算遭遇
    for hit in hits:
        _resolved_encounters.append(hit)
        encounter_triggered.emit(hit.duplicate(true))

    # 6. 应用特殊效果
    _apply_special_effects(hits)

    # 7. 检查波段转换
    _check_hull_band_transition()

    # 8. 检查迫降
    if _get_hull_integrity_effective() <= 0:
        _voyage_state = &"FORCED_LANDING"
        _finalize_voyage()


func _process_hidden_tag_reveals() -> void:
    var hidden_tags: Array = _active_voyage.get("hidden_hazard_tags", []).duplicate()
    for tag in hidden_tags:
        _check_hidden_tag_reveal(tag, false)
```

### Special Effects Application

```text
func _apply_special_effects(hits: Array[Dictionary]) -> void:
    for entry in hits:
        for effect in entry.get("special_effect_tags", []):
            match effect:
                &"voyage_duration_penalty_5s":
                    _flat_time_penalties += 5.0
                &"minor_slow":
                    _temp_time_penalties += 2.0  # ~2s 等效对 12s 周期
                &"speed_penalty_15pct":
                    _temp_time_penalties += 3.0  # ~3s 等效对 12s 周期
                &"module_damage_20pct_scout":
                    _apply_module_damage_if_hit([&"module_damage_20pct_scout"])
                &"next_check_early_5s":
                    _next_check_offset -= 5.0
                &"reveal_all_hidden_tags":
                    reveal_all_hidden_tags()
                &"reveal_landmark":
                    _revealed_landmarks.append(entry.get("encounter_type", &""))
                &"scout_window_halved_next":
                    _active_voyage["_scout_window_multiplier_next"] = 0.5
                &"bypass_scout":
                    pass  # 遇已在 resolved_encounters 中，was_hidden 处理预览
                &"time_estimate_bias_15pct":
                    _active_voyage["_time_estimate_bias"] = 1.0 + (randf() * 0.3 - 0.15)
```

### encounter_triggered Signal

```text
signal encounter_triggered(entry: Dictionary)
# entry: EncounterEntry Dictionary — encounter_type, hazard_tag, damage_amount,
#        special_effect_tags, was_hidden, time_offset
# 消费方: #17 反馈/特效/音频 — 播放遭遇视觉效果和音频提示
# 在遭遇结算完成、d_check 计算后立即发射——每个 EncounterEntry 独立发射一次
```

---

## Out of Scope

- encounter_triggered 信号的具体视觉/音频消费——属于 #17 反馈/特效/音频语义
- EncounterContext 的完整构建和 voyage_completed 信号——属于 Story 006
- 遭遇表的运行时修改/热重载——MVP 使用硬编码表，Phase 2+ 考虑 Registry 数据驱动
- 遭遇伤害对 hull_integrity 的写入——属于 Story 004 的 D_accumulated 累积 + 航程结束时一次性写入

---

## QA Test Cases

- **AC-1/2/3**: Encounter table lookup
  - safe → {calm_passage 40%, gentle_crosswind 35%, minor_debris 20%, scenic_discovery 5%}
  - storm → {storm_cell_edge 30%, turbulence_zone 25%, lightning_proximity 20%, wind_shear 15%, storm_eye_passage 10%}
  - low-visibility → {dense_fog_bank 40%, hidden_reef_proximity 35%, false_horizon 25%}

- **AC-4/5/6**: Multi-tag max rule
  - storm(3)+low-vis(4)→4; 3 tags {2,0,5}→5; all-zero→0 with effects applying

- **AC-7 through AC-14**: Special effects — 8 effects
  - gentle_crosswind→ΣT_flat+5; turbulence_zone→temp+3; wind_shear→offset-5
  - lightning_proximity→20% module hit; storm_eye→reveal all; dense_fog→preview halved next
  - hidden_reef→bypass scout; false_horizon→time estimate bias

- **AC-21/22**: encounter_triggered signal
  - Each EncounterEntry → one emit; 2 hits → 2 emits

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/navigation/EncounterResolutionTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (VoyageContext visible/hidden hazard_tags), Story 002 (T_check, N_checks, elapsed_time), Story 003 (hidden tag reveal Formula 5), Story 004 (d_check max rule, D_accumulated, hull band transitions)
- Unlocks: Story 006 (EncounterContext production from resolved_encounters), Story 008 (EC-01/02/08/09/10/20/21/22/24/35)
