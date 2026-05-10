# Story 002: Voyage Duration & Encounter Check Timing

> **Epic**: Navigation / Route Risk Resolution
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/navigation-route-risk.md`
**Requirement**: `TR-navigation-002`

**ADR Governing Implementation**: ADR-0010 (EncounterContext Type — Navigation 生产端时间推进)
**ADR Decision Summary**: 航行以时间推进——使用引擎 delta 而非挂钟时间（桌面窗口失焦/恢复安全）。Formula 1 (T_voyage) = T_distance / s_hull + ΣT_flat + ΣT_temp。T_distance 由距离带确定（short=60s, medium=120s, long=180s）。s_hull 由船体波段确定（intact=1.0, damaged=0.9, critical=0.75）。Formula 2 (T_check) = T_base × (1 + Δ_hull)。T_base=12s 默认。Δ_hull 偏移（intact=0, damaged=-0.10, critical=-0.20）——船越破遭遇越密集。N_checks = ⌊T_voyage_base / T_check⌋。关键约束：T_voyage 的基准部分（T_distance/s_hull）在启动时固定，遭遇效果叠加到基准上。N_checks 以基准时长计算——防止遭遇→延长时间→更多遭遇的正反馈循环。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Core layer)**:
- Required: 使用引擎 _process(delta) 推进 elapsed_time——不用挂钟时间；进度条到达 100% 的判定 epsilon=0.01s；N_checks 以 T_voyage_base 计算不受遭遇效果影响
- Forbidden: 使用挂钟时间计时（桌面窗口失焦/恢复节流会导致跳过遭遇）
- Guardrail: T_check 硬下限 4s——防止 wind_shear 连续命中使间隔过短；T_voyage 上限防止无限延长

---

## Acceptance Criteria

### Formula 1 — Voyage Total Duration

- [ ] **AC-1**: GIVEN distance=short + hull=intact + 无遭遇效果，WHEN T_voyage，THEN = 60/1.0 + 0 + 0 = 60s
- [ ] **AC-2**: GIVEN distance=medium + hull=damaged (s=0.9) + 2×gentle_crosswind (各+5s)，WHEN T_voyage，THEN = 120/0.9 + 10 + 0 ≈ 143.3s
- [ ] **AC-3**: GIVEN distance=long + hull=critical (s=0.75) + 1×turbulence_zone (等效+3s)，WHEN T_voyage，THEN = 180/0.75 + 0 + 3 = 243.0s

### Formula 1 — Dynamic Recalculation on Band Change

- [ ] **AC-4**: GIVEN hull=intact (s=1.0) + T_voyage_base=60s，WHEN hull→damaged (s=0.9) mid-voyage，THEN T_voyage 重算 = (T_distance/s_hull_new) + ΣT_flat + ΣT_temp。T_distance=60 → 60/0.9=66.7s (base)。进度不跳回——当前%保持，到达100%时间变长

### Formula 2 — Encounter Check Timing

- [ ] **AC-5**: GIVEN T_base=12s + hull=intact (Δ=0)，WHEN T_check，THEN = 12 × 1.0 = 12s
- [ ] **AC-6**: GIVEN T_base=12s + hull=damaged (Δ=-0.10)，WHEN T_check，THEN = 12 × 0.9 = 10.8s
- [ ] **AC-7**: GIVEN T_base=12s + hull=critical (Δ=-0.20)，WHEN T_check，THEN = 12 × 0.8 = 9.6s

### Formula 2 — Encounter Count

- [ ] **AC-8**: GIVEN short + intact → T_voyage_base=60s + T_check=12s，WHEN N_checks，THEN = ⌊60/12⌋ = 5
- [ ] **AC-9**: GIVEN medium + damaged → T_voyage_base=133.3s + T_check=10.8s，WHEN N_checks，THEN = ⌊133.3/10.8⌋ = 12（而非 intact 的 10）
- [ ] **AC-10**: GIVEN short + intact + 2×gentle_crosswind (T_voyage=70s)，WHEN N_checks，THEN = ⌊60/12⌋ = 5——不受遭遇效果影响（正反馈循环预防）

### N_checks = 0 合法

- [ ] **AC-11**: GIVEN T_voyage_base < T_check（极短航线），WHEN N_checks = 0，THEN 航程零遭遇，正常抵达 ARRIVED。这是合法行为，非错误

### Formula 2 — T_check Hard Lower Limit

- [ ] **AC-12**: GIVEN wind_shear 多次叠加使 T_check 低于 4s，WHEN 计算，THEN T_check = max(4s, T_calculated)。硬下限防止触发过快

### Timing with Engine Delta

- [ ] **AC-13**: GIVEN 航程 IN_PROGRESS，WHEN 桌面窗口切换导致 delta 变大，THEN elapsed_time 正确累积。遭遇按 elapsed_time 触发——恢复后排队结算，不"错过"
- [ ] **AC-14**: GIVEN elapsed_time ≥ T_voyage，WHEN 抵达判定，THEN 使用 epsilon=0.01s 容差。浮点比较防止跳过抵达触发

---

## Implementation Notes

### Formula 1 — Voyage Total Duration

```text
const DISTANCE_DURATION: Dictionary = {
    &"short": 60.0,
    &"medium": 120.0,
    &"long": 180.0,
}

const HULL_SPEED_COEFFICIENTS: Dictionary = {
    &"intact": 1.0,
    &"damaged": 0.9,
    &"critical": 0.75,
}

var _flat_time_penalties: float = 0.0   # ΣT_flat
var _temp_time_penalties: float = 0.0   # ΣT_temp

func calculate_voyage_duration() -> float:
    var distance_band: StringName = _active_voyage.get("distance_band", &"medium")
    var hull_band: StringName = _active_voyage.get("hull_band", &"intact")

    var t_distance: float = DISTANCE_DURATION.get(distance_band, 120.0)
    var s_hull: float = HULL_SPEED_COEFFICIENTS.get(hull_band, 1.0)

    var base: float = t_distance / s_hull
    return base + _flat_time_penalties + _temp_time_penalties


func recalculate_voyage_duration_for_band_change(new_hull_band: StringName) -> void:
    var distance_band: StringName = _active_voyage.get("distance_band", &"medium")
    var t_distance: float = DISTANCE_DURATION.get(distance_band, 120.0)
    var s_hull: float = HULL_SPEED_COEFFICIENTS.get(new_hull_band, 1.0)

    var new_base: float = t_distance / s_hull
    var new_total: float = new_base + _flat_time_penalties + _temp_time_penalties

    _active_voyage["total_duration"] = new_total
    _active_voyage["hull_band"] = new_hull_band

    # 不修改 elapsed_time——进度不跳回
    # 进度条百分比 = elapsed_time / new_total → 百分比变小，到达 100% 时间变长
```

### Formula 2 — Encounter Check Timing

```text
const BASE_CHECK_INTERVAL: float = 12.0
const CHECK_INTERVAL_MIN: float = 4.0

const HULL_BAND_CHECK_OFFSETS: Dictionary = {
    &"intact": 0.0,
    &"damaged": -0.10,
    &"critical": -0.20,
}

func calculate_check_interval() -> float:
    var hull_band: StringName = _active_voyage.get("hull_band", &"intact")
    var delta_hull: float = HULL_BAND_CHECK_OFFSETS.get(hull_band, 0.0)
    var interval: float = BASE_CHECK_INTERVAL * (1.0 + delta_hull)
    return maxf(CHECK_INTERVAL_MIN, interval)


func calculate_total_checks() -> int:
    var t_voyage_base: float = _get_voyage_base_duration()
    var t_check: float = calculate_check_interval()
    if t_voyage_base < t_check:
        return 0
    return floori(t_voyage_base / t_check)


func _get_voyage_base_duration() -> float:
    var distance_band: StringName = _active_voyage.get("distance_band", &"medium")
    var hull_band: StringName = _active_voyage.get("hull_band", &"intact")
    var t_distance: float = DISTANCE_DURATION.get(distance_band, 120.0)
    var s_hull: float = HULL_SPEED_COEFFICIENTS.get(hull_band, 1.0)
    return t_distance / s_hull
```

### Time Advancement with Engine Delta

```text
const ARRIVAL_EPSILON: float = 0.01

func _process_voyage(delta: float) -> void:
    if _voyage_state != &"IN_PROGRESS":
        return

    _elapsed_time += delta
    var total_duration: float = _active_voyage.get("total_duration", 60.0)

    # 遭遇检查
    while _should_trigger_next_check():
        _resolve_encounter_check()

    # 抵达判定 (epsilon 防止浮点误差)
    if _elapsed_time >= total_duration - ARRIVAL_EPSILON:
        _elapsed_time = total_duration
        _voyage_state = &"ARRIVED"
        _finalize_voyage()
        return

    # 迫降判定
    if _get_hull_integrity_effective() <= 0:
        _voyage_state = &"FORCED_LANDING"
        _finalize_voyage()
        return


func _should_trigger_next_check() -> bool:
    if _elapsed_time >= _active_voyage.get("total_duration", 0.0):
        return false
    var next_check_time: float = _last_check_time + calculate_check_interval()
    # wind_shear penalty 影响下一次检查时间
    if _active_voyage.has("next_check_offset"):
        next_check_time += _active_voyage.get("next_check_offset", 0.0)
    return _elapsed_time >= next_check_time
```

---

## Out of Scope

- 遭遇检查的具体遭遇解析——属于 Story 005
- 侦察预览窗口计算——属于 Story 003
- 伤害累积和船体波段动态转换——属于 Story 004

---

## QA Test Cases

- **AC-1/2/3**: Formula 1
  - Given: short+intact → 60s; medium+damaged+2×crosswind → ~143.3s; long+critical+turbulence → 243s

- **AC-5/6/7**: Formula 2
  - Given: intact → 12s; damaged → 10.8s; critical → 9.6s

- **AC-8/9/10/11**: N_checks
  - Given: short+intact → 5; medium+damaged → 12; N_checks base on T_voyage_base; N=0 → 零遭遇正常

- **AC-13**: Browser tab switch — delta spike → no missed encounters; epsilon arrival

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/navigation/TimingFormulasTest.csproj` — must exist and pass
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (VoyageContext with distance_band, hull_band), modules-hull-state Epic (hull_band enum values, coefficients)
- Unlocks: Story 003 (preview window uses T_check), Story 004 (band change triggers duration recalculation), Story 008 (EC-06/07/11/20/33/34)
