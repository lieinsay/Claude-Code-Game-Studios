# Story 003: Scout Preview Window & Hidden Tag Reveal

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-002`

**ADR Governing Implementation**: ADR-0010 (EncounterContext — revealed_hidden_tags 字段)
**ADR Decision Summary**: Formula 3 (T_preview) = N_preview(η_scout) × T_check。N_preview = ⌊η_scout × 2⌋。映射表：η=1.0→2(24s), η=0.95→1(12s), η=0.6→1(12s), η=0→0。侦察模块提供纯信息预览——不提供绕行或减免能力。预览在 UI 中以进度条前方的半透明图标显示。若风险标签为隐藏，预览仅显示 `?`。Formula 5 (P_reveal) = r_base (默认 0.30，每次遭遇检查独立判定)。storm_eye_passage 覆盖为 P_reveal=1.0（揭示所有隐藏标签）。隐藏标签揭露后：航行中转为可见（使用可见遭遇表抽取），航行结束后向 #6 发射更新事件。双侦察模块冗余：η_effective = max(η_A, η_B)。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: N_preview = ⌊η_scout × 2⌋ floor 取整——产生有意义的阶梯差异；双侦察取 max η；隐藏标签预览仅显示 `?` 标记
- Forbidden: 侦察模块提供绕行或减免能力——纯信息预览；unchecked 模块在航行中"意外发现"真实状态
- Guardrail: 无侦察模块时 T_preview=0——遭遇在发生时才知道

---

## Acceptance Criteria

### Formula 3 — Scout Preview Window

- [ ] **AC-1**: GIVEN η_scout=1.0 + T_check=12s，WHEN N_preview + T_preview，THEN N_preview = ⌊1.0×2⌋ = 2, T_preview = 2×12 = 24s
- [ ] **AC-2**: GIVEN η_scout=0.95 + T_check=12s，WHEN N_preview，THEN N_preview = ⌊0.95×2⌋ = ⌊1.9⌋ = 1, T_preview = 12s
- [ ] **AC-3**: GIVEN η_scout=0.6 + T_check=12s，WHEN N_preview，THEN N_preview = ⌊0.6×2⌋ = ⌊1.2⌋ = 1, T_preview = 12s
- [ ] **AC-4**: GIVEN η_scout=0（无模块），WHEN N_preview，THEN = ⌊0×2⌋ = 0, T_preview = 0s。遭遇在发生时才知道

### Preview Content

- [ ] **AC-5**: GIVEN 可见风险标签 + η_scout>0，WHEN 侦察预览，THEN 进度条前方显示预警图标（对应标签的遭遇类型图标）
- [ ] **AC-6**: GIVEN 隐藏风险标签 + η_scout>0，WHEN 侦察预览，THEN 仅显示 `?` 标记——知道前方有东西但不知道是什么
- [ ] **AC-7**: GIVEN η_scout=0（无侦察模块），WHEN 航行中，THEN 进度条上不显示任何预警图标

### Dual Scout Module Redundancy

- [ ] **AC-8**: GIVEN slot_a=scout(η=1.0) + slot_b=scout(η=0.6)，WHEN η_effective，THEN = max(1.0, 0.6) = 1.0。T_preview=24s
- [ ] **AC-9**: GIVEN slot_a=scout(η=0.6) + slot_b=scout(η=0.6)，WHEN η_effective，THEN = max(0.6, 0.6) = 0.6。T_preview=12s

### Unchecked Module in Voyage

- [ ] **AC-10**: GIVEN η_scout=0.95 (unchecked)，WHEN 航行中，THEN 始终使用 η=0.95——不"意外发现"真实状态。unchecked 是已知风险折扣，非航行中的突袭

### Formula 5 — Hidden Tag Reveal

- [ ] **AC-11**: GIVEN hidden_tag + P_reveal=0.30，WHEN 每次遭遇检查独立判定，THEN 每次独立 30% 概率揭露。揭露后转为可见标签——使用可见遭遇表抽取
- [ ] **AC-12**: GIVEN storm_eye_passage 遭遇触发，WHEN 效果，THEN P_reveal=1.0——该次检查立即揭露所有仍隐藏的标签
- [ ] **AC-13**: GIVEN 隐藏标签已在此前检查中被揭露，WHEN 后续检查，THEN 不再重复判定——该标签已转为可见

### Reveal Lifecycle

- [ ] **AC-14**: GIVEN 隐藏标签在航行中被揭露，WHEN 航行结束（任何终态），THEN revealed_hidden_tags 包含该标签。向 #6 发射更新事件——下次航图刷新时该标签从隐藏变为可见
- [ ] **AC-15**: GIVEN 隐藏标签全程未被揭露（所有判定均失败），WHEN 航行结束，THEN 标签保持隐藏状态。航线知识不推进

---

## Implementation Notes

### Formula 3 — Scout Preview Window

```text
func calculate_preview_window(scout_efficiency: float) -> float:
    var n_preview: int = floori(scout_efficiency * 2.0)
    if n_preview <= 0:
        return 0.0
    var t_check: float = calculate_check_interval()
    return float(n_preview) * t_check


func get_effective_scout_efficiency() -> float:
    var slot_a_type: int = ModuleHullManager.get_slot_module_type(&"slot_a")
    var slot_b_type: int = ModuleHullManager.get_slot_module_type(&"slot_b")

    var eff_a: float = ModuleHullManager.get_module_efficiency(&"slot_a") \
        if slot_a_type == ModuleType.SCOUT else 0.0
    var eff_b: float = ModuleHullManager.get_module_efficiency(&"slot_b") \
        if slot_b_type == ModuleType.SCOUT else 0.0

    return maxf(eff_a, eff_b)
```

### Preview Entry Building

```text
func _build_pending_encounters() -> void:
    _pending_encounters.clear()
    var preview_window: float = calculate_preview_window(_active_voyage.get("scout_efficiency", 0.0))
    if preview_window <= 0.0:
        return

    var t_check: float = calculate_check_interval()
    var n_checks: int = calculate_total_checks()

    for i in range(n_checks):
        var check_time: float = float(i + 1) * t_check
        if check_time <= _elapsed_time + preview_window and check_time > _elapsed_time:
            _pending_encounters.append({
                "check_time": check_time,
                "preview_data": _generate_preview(check_time),
            })


func _generate_preview(check_time: float) -> Dictionary:
    var visible_tags: Array = _active_voyage.get("visible_hazard_tags", [])
    var hidden_tags: Array = _active_voyage.get("hidden_hazard_tags", [])

    # 可见标签→正常预览；隐藏标签→仅 ?
    var preview_tags: Array[StringName] = []
    for tag in visible_tags:
        preview_tags.append(tag)  # UIManager 根据 tag 显示对应图标

    var has_hidden: bool = false
    for tag in hidden_tags:
        # 隐藏标签不显示具体内容——仅标记 "有未知"
        has_hidden = true

    return {
        "visible_preview_tags": preview_tags,
        "has_hidden_threat": has_hidden,
    }
```

### Formula 5 — Hidden Tag Reveal

```text
const BASE_REVEAL_CHANCE: float = 0.30

func _check_hidden_tag_reveal(hidden_tag: StringName, force_reveal: bool = false) -> bool:
    if hidden_tag in _revealed_hidden_tags:
        return false  # 已揭露

    if force_reveal:
        _revealed_hidden_tags.append(hidden_tag)
        _active_voyage["visible_hazard_tags"].append(hidden_tag)
        _active_voyage["hidden_hazard_tags"].erase(hidden_tag)
        return true

    if randf() < BASE_REVEAL_CHANCE:
        _revealed_hidden_tags.append(hidden_tag)
        _active_voyage["visible_hazard_tags"].append(hidden_tag)
        _active_voyage["hidden_hazard_tags"].erase(hidden_tag)
        return true

    return false


func reveal_all_hidden_tags() -> void:
    var still_hidden: Array[StringName] = _active_voyage.get("hidden_hazard_tags", []).duplicate()
    for tag in still_hidden:
        _check_hidden_tag_reveal(tag, true)  # force_reveal
```

### Preview Window on Scout Module Damage

```text
func _on_scout_efficiency_changed(new_eff: float) -> void:
    var old_preview: float = calculate_preview_window(_active_voyage.get("scout_efficiency", 0.0))
    _active_voyage["scout_efficiency"] = new_eff
    var new_preview: float = calculate_preview_window(new_eff)

    # 已在队列中的预览遭遇保留；超出新预览范围的移除
    if new_preview < old_preview:
        var cutoff_time: float = _elapsed_time + new_preview
        _pending_encounters = _pending_encounters.filter(
            func(e): return e["check_time"] <= cutoff_time
        )

    # 重新构建新预览范围内的
    _build_pending_encounters()
```

---

## Out of Scope

- 侦察预览图标在 UI 进度条上的具体渲染——属于 UI 系统 #16
- `?` 标记的视觉设计——属于 UI 系统 #16
- 隐藏标签揭露后向 #6 发射的具体事件格式——属于 IntelManager #6 的消费端

---

## QA Test Cases

- **AC-1 through AC-4**: Formula 3
  - η=1.0 → 24s; η=0.95 → 12s; η=0.6 → 12s; η=0 → 0s

- **AC-5/6/7**: Preview content
  - visible tag → icon; hidden tag → ?; no scout → no icons

- **AC-8/9**: Dual scout
  - (1.0, 0.6) → 1.0; (0.6, 0.6) → 0.6

- **AC-11/12/13/14/15**: Hidden tag lifecycle
  - 30% per check → at least one reveal; storm_eye → all revealed
  - Already revealed → no re-check; end voyage → emit to #6; never revealed → stays hidden

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/navigation/scout/ScoutPreviewTest.csproj` — must exist and pass
**Status**: [x] 29/29 PASS — 2026-05-13

---

## Dependencies

- Depends on: Story 001 (VoyageContext visible/hidden hazard_tags), Story 002 (T_check, N_checks), modules-hull-state Epic (get_module_efficiency, get_slot_module_type, ModuleType enum)
- Unlocks: Story 005 (hidden tag interaction with encounter resolution), Story 008 (EC-14/15/16/17/18/19)
