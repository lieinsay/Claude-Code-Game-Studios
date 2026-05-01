# Review Log — 飞艇模块与船体状态

## Review — 2026-05-01 — Verdict: MAJOR REVISION NEEDED → REVISED (awaiting re-review)

Scope signal: M
Specialists: game-designer, systems-designer, economy-designer, qa-lead, gameplay-programmer, creative-director
Blocking items: 7 | Recommended: 8 | Resolved in revision: 7 blockers + 8 recommendations

Summary: 首轮 Full review 发现 7 项阻断和 8 项建议。CD-GDD-ALIGN 判决 MAJOR REVISION NEEDED，核心诊断：(1) 模块状态模型存在实现阻断——unchecked 未入转换表、双字段模型缺失、can_depart 返回类型矛盾、hull_scars 完全未定义；(2) 槽位锁定与 "选择即身份" 的 Fantasy 断裂——CD 建议修正 Fantasy 但用户选择重新设计为开放槽位系统；(3) 全文以系统思维（状态机/效率系数）组织而非体验思维（玩家做什么、看到什么、感受到什么）。修订中全部 7 项阻断已解决，包括开放槽位重构（双货仓 M_max=24/V=1000、双侦察冗余保护）、unchecked 完整状态模型、hull_scars 生命周期定义、跨波段伤害链、满血修复保护、信号契约和材料成本参数。建议在新会话中重审以获取干净的 specialist 分析。

---

## Re-Review — 2026-05-01 — Verdict: CONDITIONAL APPROVAL (2 blockers + 6 recommendations)

Scope: Systems Designer (formulas + boundary tests + mathematical correctness only)
Reference: 8 specific boundary tests requested by user

### Formula Boundary Test Results

#### Test 1: M_max = 0 but integrity > 0 (all modules empty, hull intact)

**Result: PASS — no degenerate output.**
- D.1: floor(12×0 + 8×0) = floor(0) = 0. Correct.
- D.5: can_depart = (0>0=false) AND (integrity>0=true) AND (M_loaded≤0=true) = false.
- Reason returned: "no_furnace" per AC-19. Correct.
- EC-03 explicitly documents this state. No mathematical error.
- **Design note**: Integrity could be 100 while M_max=0. This is intentional — furnace power and hull integrity are independent dimensions. Verified.

#### Test 2: integrity = 0 — destroyed forever or repairable?

**Result: PASS — repairable, correctly documented.**
- Rule 18: "ship hull can be repaired at Hub Station 10."
- Rule 14: "destroyed（0）：无法出航...必须紧急修复恢复到至少 1 点。"
- State machine: destroyed → (emergency repair, integrity ≥ 1) → critical.
- D.6 constraint: "若 R_total = 0 且 integrity_old = 0，修复操作拒绝执行（必须使用有效修复材料）。"
- With one `repair_kit` (repair_value = 25 per Tuning Knobs): integrity_new = min(100, 0+25) = 25 → enters critical band. Ship can now depart again.
- **No degenerate state.** integrity=0 is NOT permanent destruction.

#### Test 3: M_loaded exactly equal to M_max — can depart?

**Result: PASS — ≤ allows equality.**
- D.5: M_loaded ≤ M_max. When M_loaded = M_max, the condition is true.
- Example: M_max = 20, M_loaded = 20, integrity = 100 → can_depart = (20>0) AND (100>0) AND (20≤20) = true.
- The overload block triggers only when M_loaded > M_max (strictly greater).
- **No bug.**

#### Test 4: floor on M_max — rounding losses > 5%

**Result: FAIL — three configurations produce rounding losses exceeding 5%.**

Analysis of all 63 possible state combinations (2 modules × 5 visible_states each × 2 types):

| Configuration | Pre-floor sum | M_max | Lost | Loss % |
|---|---|---|---|---|
| Single damaged scout | 8 × 0.6 = 4.8 | **4** | 0.8 | **16.7%** |
| Single unchecked scout | 8 × 0.95 = 7.6 | **7** | 0.6 | **7.9%** |
| Both damaged | 4.8 + 6.0 = 10.8 | **10** | 0.8 | **7.4%** |
| Scout damaged + cargo unchecked | 4.8 + 11.4 = 16.2 | **16** | 0.2 | 1.2% |
| Scout unchecked + cargo damaged | 7.6 + 6.0 = 13.6 | **13** | 0.6 | 4.4% |

The three configurations exceeding 5% are all single-module or dual-damaged scenarios involving the scout module. The root cause: scout's R_furnace=8, which produces fractional values when multiplied by 0.6 (4.8) or 0.95 (7.6).

**Why this matters**: A player running a single scout configuration (M_max=8 nominal) who takes module damage drops to M_max=4 — a 50% effective capacity loss, but 16.7% of the remaining capacity is lost to rounding. The "true" capacity before floor is 4.8 weight units.

**Recommendation**: Either (a) scale R_furnace values by 10× and divide M_max by 10 at the comparison point, eliminating all fractions; or (b) adjust R_furnace_scout to a value that produces integer results at all η values (e.g., R_furnace_scout = 10 makes 10×0.6=6.0, 10×0.95=9.5→still fractional); or (c) accept the rounding as intentional friction and document the worst-case loss explicitly in the Tuning Knobs section.

#### Test 5: Critical hull band ×0.8 stacking with damaged module

**Result: PASS for correctness, FAIL for documentation completeness.**

Multiplicative stacking is correct and consistent:
- Damaged scout at critical: 0.6 × 0.8 = **0.48**
- Damaged cargo at critical: 0.5 × 0.8 = **0.40**
- Installed at critical: 1.0 × 0.8 = **0.80**
- Unchecked at critical: 0.95 × 0.8 = **0.76**

However, the D.3 example only shows the installed×critical case (1.0×0.8=0.8). The damaged×critical and unchecked×critical cases are not shown. The text "与模块自身效率叠加" confirms multiplicative stacking but lacks a formal combined formula.

**Missing**: η_final = η_visible × η_hull_band, where η_hull_band = 1.0 (intact/damaged) or 0.8 (critical). This simple formula is implicit from the prose but never presented as a single expression.

**Also**: There is ambiguity about whether the critical band ×0.8 applies to V_effective (D.4) and scout recon range, or only to M_max (D.1). D.3 says "模块效率额外 × 0.8" which should apply to ALL module-derived effects, but D.4 formula does not include the η_hull_band factor. An implementer could accidentally apply it to D.1 but forget D.4.

#### Test 6: V_effective with one cargo empty, one installed

**Result: PASS.**
- V_effective = 0 + 500×0 + 500×1.0 = **500**
- Formula produces correct output. D.4 example table confirms: "侦察+货仓 installed：V_effective = 0 + 0 + 500×1.0 = 500"
- **No bug.**

#### Test 7: Over-repair — can player waste materials?

**Result: PASS — behavior is correctly defined but UX risk exists.**

- D.6: integrity_new = min(100, integrity_old + R_total). Excess is discarded.
- EC-07: "多余修复值不保留、不退款。UI 应在修复确认前显示'将恢复至 100/100'以提示溢出。"
- Rule 18: repair rejected entirely if integrity ≥ 100 (prevents full-health waste).
- **However**: if integrity = 99 and player uses a repair_kit (25 repair value), 24 points are wasted. The UI warning is documented but the UX of losing 96% of a repair kit's value is worth flagging.
- **No max per action** — rule 19 states "单次修复量无上限". Players can burn multiple repair_kits in one action, potentially wasting large amounts if near 100.

**Recommendation**: Consider a soft warning at repair confirm when the waste exceeds some threshold (e.g., >50% of total repair value would be discarded).

#### Test 8: Unchecked state — efficiency 0.95 while actually damaged

**Result: PASS for correctness, FAIL for gameplay impact assessment.**

- D.2: unchecked η = 0.95 regardless of actual_state. This is intentional per EC-04.
- When a module is unchecked and actually damaged: visible efficiency = 0.95, while "true" efficiency would be 0.6 (scout) or 0.5 (cargo).
- EC-04: "航程中不会'突然发现模块是坏的'" — the 0.95 is a known risk discount.
- **Gameplay concern**: The penalty for flying with a damaged module without checking is only 5% (1.0→0.95) vs. the full penalty of 40-50% (0.6/0.5). This creates a perverse incentive: players may rationally choose to NEVER check modules, accepting a permanent 5% efficiency tax to avoid the risk of discovering damage and paying the full penalty (or repair costs). The unchecked→damaged reveal is a punishment, so the rational meta-strategy is to never check.
- **Tuning suggestion**: The Tuning Knob `efficiency_unchecked` has range 0.85–1.0. At 1.0, checking has zero mechanical meaning. At 0.95, the penalty may be too weak to motivate checking. Consider lowering the default to something like 0.85 or 0.80 so that the gap between unchecked and damaged is smaller, reducing the "never check" incentive. Or, make unchecked modules degrade toward their actual_state over multiple voyages.

---

### Formula-by-Formula Defect Report

#### D.1: M_max = floor(Σ (R_furnace(i) × η_module(i)))

| Issue | Severity | Detail |
|---|---|---|
| Energy multiplier missing from formula | LOW | D.1 does not include `energy_status(i)`. The GDD defines `get_furnace_energy_status()` as always 1.0 in MVP. No bug currently, but when energy system is added, D.1 silently breaks. The formula should be written as `floor(Σ (R_furnace(i) × η_module(i) × energy_status(i)))` with a note that energy_status defaults to 1.0. |
| "for all installed modules" phrasing is wrong | LOW | The formula sums over all NON-EMPTY modules (empty ones contribute 0 anyway). "Installed" is a specific state (η=1.0). The phrase should be "for all non-empty modules" or simply "for all modules in slot A and slot B." |
| floor() produces >5% losses (see Test 4) | MEDIUM | 16.7% loss on single damaged scout. No design rationale for floor vs. alternatives. |
| Variable table lists η range as {0, 0.5, 0.6, 0.95, 1.0} | LOW | The set is incomplete. At critical hull band, η_final can be {0, 0.4, 0.48, 0.76, 0.8} after ×0.8 stacking. If the variable table means "before hull band modifier," that should be stated. |

#### D.2: efficiency_table lookup

| Issue | Severity | Detail |
|---|---|---|
| No hull band interaction visible | MEDIUM | D.2 only returns η_visible. The η_final (after hull critical ×0.8) is not represented in any formula. See D.3 interaction gap. |
| unchecked is a flat 0.95 — state-independent | DESIGN | Intentional per EC-04, but see Test 8 gameplay concern. |

#### D.3: hull integrity bands

| Issue | Severity | Detail |
|---|---|---|
| Band boundary at 76 is off-by-one from "entered" logic | MEDIUM | If integrity = 75, band = damaged. If damage event drops integrity from 76 to 75, does hull_scars increment for "entering damaged"? AC-13 says yes: 100→75 gives hull_scars += 2 (base + entering damaged). But what about 76→75? The GDD says "进入 damaged" happens at ≤75. So 76→75 crosses the boundary and triggers the +1. This is correct — but the boundary value 76 for intact means intact is [76,100], and the transition happens when integrity drops from 76 to 75. This is mathematically consistent but could be clarified: "波段转换发生在 integrity 从 intact_min 降至 intact_min-1 时。" |
| **CRITICAL**: Cross-band hull_scars count inconsistency | **BLOCKER** | See cross-band damage chain analysis below. |
| critical band ×0.8 scope ambiguous | MEDIUM | See Test 5. The modifier should apply to all η-dependent outputs (M_max, V_effective, scout visibility), but D.4 and scout formulas don't show the factor. |

#### D.4: V_effective

| Issue | Severity | Detail |
|---|---|---|
| Missing hull critical modifier | MEDIUM | If hull is critical, V_effective should also be ×0.8 per D.3's "模块效率额外 × 0.8". D.4 formula doesn't include this factor. |
| V_bonus sourcing unclear | LOW | V_bonus = 500 is listed as a tuning knob in resources GDD #5 (`cargo_module_volume_bonus`). This GDD's Tuning Knobs section correctly notes the value belongs to #5 but uses it in formulas. No conflict, but cross-reference could be more explicit. |

#### D.5: can_depart()

| Issue | Severity | Detail |
|---|---|---|
| No formula bug | NONE | Boolean logic is sound. All edge cases documented (EC-01 through EC-04, AC-18 through AC-21). |
| M_loaded type: resources GDD says int | LOW | Resources GDD's `total_loaded_mass` output is type int. M_max is int (via floor). Comparison int ≤ int is safe. No type coercion issues. |

#### D.6: hull repair

| Issue | Severity | Detail |
|---|---|---|
| No cap per repair action | LOW | Rule 19: "单次修复量无上限". Player can go from 0→100 in one action if they have 4 repair_kits (25×4=100). This is by design, not a bug, but worth noting as a tuning concern — it removes any "repair over time" pacing. |
| Waste potential (see Test 7) | LOW | Documented by EC-07 but UX risk remains. |

---

### Cross-Band Damage Chain: BLOCKER-Level Inconsistency

Rule 17 and EC-12 / AC-29 contain a math error in the hull_scars count.

**The facts:**
- Rule 17: "若一次伤害事件跨越多个波段（如 integrity 从 30 一次受到 35 点伤害 → clamp 至 0），波段依次触发：先进入 damaged → 再进入 critical → 最后进入 destroyed。每个新进入的波段使 hull_scars += 1（本次伤害事件本身已贡献 +1）。"
- AC-29 / EC-12: "integrity 从 30 一次受到 35 点伤害降至 0 时，hull_scars 累计：基础事件 +1，进入 damaged +1，进入 critical +1，进入 destroyed +1（共计 +4）。"

**The error:**
At integrity = 30, the ship is ALREADY in the `damaged` band (26-75). The damage event does NOT cause a new entry into the damaged band — the ship was already there. Therefore:

- Base damage event: +1
- Entering critical (from damaged): +1
- Entering destroyed (from critical): +1
- **Total: +3** (not +4)

"进入 damaged +1" should NOT be counted because integrity=30 is already in the damaged band.

**Cross-check with AC-13**: integrity 100→75, hull_scars += 2 (base +1, entering damaged +1). This is CORRECT — 100 is in intact, 75 is in damaged, so entering damaged is a new band entry.

**Cross-check with AC-14**: integrity 26→25. Starting in damaged (26), ending in critical (25). Base +1, entering critical +1. Total = +2. This is CORRECT.

**The inconsistency**: AC-29 counts "进入 damaged" when starting at 30 (already in damaged), while the rule of "每个新进入的波段" should only count bands that the ship was NOT already in.

**If the GDD intends to count ALL bands the damage "passes through" (including the starting band), then AC-13 (100→75) should also count the intact band, giving +3 instead of +2. But AC-13 says +2, confirming that only newly entered bands count.**

**Verdict**: EC-12 and AC-29 are wrong. The correct count for 30→0 is +3 (base + critical + destroyed), not +4. The text "先进入 damaged" in rule 17 is also wrong for this example — the ship starts at 30 which is already in damaged.

**Fix options**:
1. Change EC-12 and AC-29 to say +3, and remove "进入 damaged +1" from the breakdown.
2. Change the example to start from intact (e.g., integrity 80→0): base +1, entering damaged +1, entering critical +1, entering destroyed +1 = +4 (correct for this case).
3. Redefine the rule to count all bands the damage "touches" regardless of starting band. But this would break AC-13's count.

---

### Missing Formulas

| # | Formula | Where Needed | Severity |
|---|---|---|---|
| MF-1 | `η_final(i) = η_visible(i) × η_hull_band` where η_hull_band = 1.0 for intact/damaged, 0.8 for critical | D.2 and D.3 interaction — stated in prose but never expressed as a formula | MEDIUM |
| MF-2 | `hull_scars_new = hull_scars_old + 1 + count_bands_entered(band_before_damage, band_after_damage)` where count_bands_entered excludes the starting band | Rule 17 + EC-12 — described in words but no formal function | LOW |
| MF-3 | `damage_clamped = min(current_integrity, raw_damage)` → `integrity_new = max(0, integrity_old - damage_clamped)` | EC-06 describes this (max(0, 5-15)=0) but no formal formula | LOW |
| MF-4 | Scout recon visibility formula — damaged = 60% range; what is the base range in units? Is it a boolean (extra segment shown yes/no) or a continuous value? | Rule 12 — "额外显示一段风险标注...damaged 时可见范围缩减为 60%" — efficiency coefficient is produced but the consumer (navigation #10) has no formula to interpret it | LOW |

---

### Underspecified / Ambiguous Formulas

| # | Issue | Location |
|---|---|---|
| US-1 | D.1's "向下取整" (floor) has no design rationale. Why floor and not round or ceiling? What problem does floor solve that integer weight units don't? | D.1 |
| US-2 | D.3 critical band modifier: does "模块效率额外 × 0.8" apply to (a) only furnace output (M_max), (b) all module-derived numeric effects (M_max + V_effective + scout visibility), or (c) everything including qualitative effects? The D.3 table header says "模块效率额外修正" which strongly implies (b), but D.4 formula doesn't include the factor. | D.3, D.4 |
| US-3 | The term "叠加" in D.3 ("与模块自身效率叠加") is ambiguous — does it mean multiplicative (a×b) or additive (a+b-1)? The example (1.0×0.8=0.8) confirms multiplication, but the word "叠加" could be misread as addition by a Chinese-speaking implementer. Use "相乘" (multiply) instead of "叠加" (stack/add). | D.3 |
| US-4 | What is the scout module's efficiency output used FOR? Rule 12 describes a qualitative effect (extra risk segment on map). The efficiency coefficient (η_scout) is produced but there is no formula connecting η=0.6 to "60% range". Is 60% of "extra segment" = 0.6 of a segment shown? Or does efficiency scale continuously (η=0.6 means 60% of full segment, η=0.8 means 80%)? Navigation system #10 will need to know. | Rule 12, D.2 |
| US-5 | `unchecked` state lifecycle: the state machine shows transitions INTO unchecked (from installed, damaged) but no row for unchecked → unchecked (when player takes a second voyage without checking). The post-voyage process (rule 14, step 3) always sets visible_state = unchecked, which handles this implicitly, but the state machine table is incomplete. | States and Transitions table |
| US-6 | "参与航程的模块" (modules participating in a voyage) — is an `empty` slot considered "participating"? What about a `damaged` module from a previous voyage that was never repaired? The text implies "all non-empty modules" but this isn't stated. | Post-voyage flow (lines 149-154) |

---

### Dependencies Check

| System | Expected Reference | Found? | Status |
|---|---|---|---|
| Resources #5 → provides `get_total_loaded_mass()` | Yes in Interactions + D.5 | YES | Aligned |
| Resources #5 → provides `cargo_module_volume_bonus` | Yes in rule 11 | YES | Aligned |
| Resources #5 → provides `consume_for_module()` | Yes in Interactions | YES | Aligned |
| Resources #5 → `repair_value(m)` | D.6 references but Resources #5 Tuning Knobs only define `repair_kit` repair value | PARTIAL | Resources GDD doesn't expose a generic `repair_value(m)` function — only `hull_repair_value_per_repair_kit = 25`. The modules GDD assumes a generic mapping that doesn't exist in Resources yet. |
| Hub #7 → provides slot physical positions, interaction anchors | Yes in Interactions + Dependencies | YES | Aligned |
| Navigation #10 → consumes scout efficiency, hull band, can_depart, M_max | Yes in Interactions | YES | Aligned |

---

### Registry Gaps

The following cross-system facts in this GDD should be registered in `entities.yaml` but are not:

| Fact | Type | Current Registry Status |
|---|---|---|
| `furnace_rating_scout` = 8 | constant | **Missing** |
| `furnace_rating_cargo` = 12 | constant | **Missing** |
| `cargo_module_volume_bonus` = 500 | constant | **Missing** (owned by Resources #5 but used here) |
| `hull_integrity_max` = 100 | constant | **Missing** |
| `hull_band_intact_min` = 76 | constant | **Missing** |
| `hull_band_damaged_min` = 26 | constant | **Missing** |
| `hull_band_critical_min` = 1 | constant | **Missing** |
| M_max formula | formula | **Missing** |

---

### Summary

| Category | Count |
|---|---|
| Formulas tested at boundaries | 6 (D.1–D.6) |
| Boundary tests passed cleanly | 5 / 8 |
| **BLOCKER findings** | **2** (cross-band hull_scars count inconsistency; missing hull critical modifier in D.4/V_effective) |
| **MEDIUM findings** | **5** (floor loss >5%; D.2/D.3 stacking not formalized; US-2 ambiguity on critical scope; US-3 ambiguous "叠加" wording; US-4 scout efficiency-to-visibility mapping) |
| **LOW findings** | **6** (energy multiplier missing; "installed" phrasing; D.2 value set incomplete; V_bonus sourcing; no repair cap; M_loaded type note) |
| Missing formulas identified | 4 |
| Underspecified items | 6 |
| Registry gaps | 8 constants + 1 formula |

**BLOCKER-1 (SD-CROSSBAND-SCARS)**: EC-12 and AC-29 claim hull_scars += 4 for damage from 30→0, but "进入 damaged" should not count (ship already at 30, which is IN the damaged band). Correct count is +3 (base + critical + destroyed). The text "先进入 damaged" in rule 17 is also incorrect for this example.

**BLOCKER-2 (SD-CRITICAL-VEFFECTIVE)**: D.3 states critical hull band applies "模块效率额外 × 0.8" to ALL module efficiency. D.4 (V_effective) formula does not include the η_hull_band factor. An implementer would naturally apply critical ×0.8 to D.1 (M_max) but could miss D.4, creating an inconsistency where M_max drops at critical but cargo volume doesn't. The combined formula η_final = η_visible × η_hull_band must be explicitly defined and applied to all formulas that use η.

**Verdict**: CONDITIONAL APPROVAL — the two blockers must be resolved before implementation. The six recommendations should be addressed but do not block implementation.

---

## Re-Review — Economy Designer — 2026-05-01 — Verdict: MAJOR REVISION NEEDED (4 blockers + 5 recommendations)

Scope: Economic health, resource flow modeling, incentive structures, progression pacing, degenerate strategy detection, and fantasy-economy alignment.

Reference: User-requested economic investigation of 7 specific areas plus open-ended exploit/strategy detection.

### E-1 (BLOCKER): 50% Uninstall Refund Creates Experimentation Lock-In — Fantasy Contradiction

**The math of a configuration swap:**

Starting resources (from `resources-goods-capacity.md` Starting State): `basic_supply` × 10, `repair_kit` × 2.

The player begins with cargo pre-installed in slot B (free). After first exploration, scout module is acquired (free). Player installs scout in slot A: cost `basic_supply` × 5 + `repair_kit` × 2. Remaining: `basic_supply` × 5, `repair_kit` × 0.

Now the player wants to experiment with dual-cargo (uninstall scout from A, install cargo in A):

| Action | basic_supply cost | repair_kit cost |
|--------|-------------------|-----------------|
| Uninstall scout from A (50% refund, floor) | −2 (refund) | −1 (refund) |
| Net loss from uninstall | 3 | 1 |
| Install cargo in A | 3 | 3 |
| **Total resources needed** | **6** | **4** |
| **Player has** | **5** | **0** |

**Result: IMPOSSIBLE.** The player literally cannot afford a single configuration change after installing the scout module — they lack the repair_kits. A round-trip experiment (scout+cargo → dual-scout → scout+cargo) costs `basic_supply` × 13 + `repair_kit` × 8, which is **130% of starting basic_supply and 400% of starting repair_kits**.

**Why this is a blocker:** The Fantasy section explicitly states "两个槽位都是开放的——玩家可以选择装两个货仓模块最大化运力、装两个侦察模块获得冗余视野、或一侦察一货仓走平衡路线" and "模块选择即身份表达." But the economics make module choice a one-time commitment. Once a module is installed, changing configuration requires resources the player does not have. This is not "identity expression" — it is "identity lock-in." The fantasy promises freedom; the economy delivers a sunk-cost trap.

**Additionally**, the `floor()` on 50% refund creates uneven effective refund rates:
- Cargo install cost `basic_supply` × 3 → refund 1 (33.3%, not 50%)
- Scout install cost `basic_supply` × 5 → refund 2 (40%, not 50%)
- Cargo install cost `repair_kit` × 3 → refund 1 (33.3%)
- Scout install cost `repair_kit` × 2 → refund 1 (50%)

The effective refund rate varies from 33.3% to 50% depending on material and module type. This is an undocumented artifact of `floor()`, not an intentional design gradient.

**Recommendation options:**
- **(A)** Raise refund to 100% for installed modules. This makes configuration changes free, fully supporting the "identity expression" fantasy. The cost of installation becomes a one-time unlock fee. Risk: reduces economic weight of decisions.
- **(B)** Raise refund to 80% with ceiling. Reduces experimentation tax while preserving some commitment weight. At 80%, a round-trip experiment costs ~4 basic + 3 repair — expensive but achievable after 1-2 explorations.
- **(C)** Keep 50% but add a "module swap" operation that charges only the DIFFERENCE in install costs (not a full uninstall+reinstall cycle). This preserves commitment while removing the double-penalty of swapping.
- **(D)** Keep current rates but significantly reduce install costs (e.g., scout = basic×2 + repair×1, cargo = basic×1 + repair×2). This preserves the percentage economics while making experimentation accessible with starting resources.

**Recommended: (C)** — it preserves the fantasy of "module choice has weight" while removing the punitive double-cost of experimentation. A "swap" operation charges `max(0, new_cost - refund_of_old)` and handles the transition in one atomic step.

---

### E-2 (BLOCKER): Unchecked Repair Creates Perverse Incentive — Undermines Inspection Mechanic

**The incentive structure:**

EC-05 states: "若模块实际上未受损，维修不消耗材料（或消耗微量'检查'材料）" — meaning repairing an unchecked but actually-undamaged module costs 0 or trivial materials.

This creates a dominant strategy:

1. After every voyage, go to repair station.
2. Repair ALL unchecked modules without inspecting them.
3. If module is actually undamaged: cost = 0 (or trivial). Module efficiency restored 0.95 → 1.0.
4. If module is actually damaged: cost = normal repair cost. Module efficiency restored from damaged state to 1.0.

Compare to the "intended" flow of inspecting first:
- Inspect → discover "installed" (undamaged): efficiency restored 0.95 → 1.0. Cost: 0.
- Inspect → discover "damaged": efficiency drops 0.95 → 0.5/0.6. Now must pay repair cost to restore to 1.0.
- Total: same outcome as direct repair, but with an extra step and a moment of worsened capability.

**The player who repairs directly saves a step AND avoids the psychological punishment of seeing their module revealed as damaged.** There is zero incentive to ever inspect a module. The inspection mechanic exists on paper but is strategically dominated by direct repair.

**Why this is a blocker:** The unchecked state is the central post-voyage mechanic — it creates the tension of "what happened to my ship?" If the optimal strategy is to skip it entirely, an entire state machine branch becomes dead content. This is a systems-design waste of a well-conceived mechanic.

The Systems Designer's Test 8 flagged the complementary problem (unchecked penalty too weak to motivate checking), but this is the OTHER side of the same coin: even with a stronger unchecked penalty, direct repair at zero/trivial cost for undamaged modules makes inspection strictly inferior.

**Recommendation options:**
- **(A)** Make repair of unchecked modules cost the FULL repair cost regardless of actual state. This forces the player to inspect first, since repairing an undamaged module wastes materials. Inspection becomes the rational choice.
- **(B)** Make unchecked modules unrepairable — player MUST inspect before repair. This enforces the intended flow mechanically. Risk: feels restrictive.
- **(C)** Keep free repair for undamaged modules but add a non-zero "inspection cost" to direct repair (e.g., repair of unchecked always costs at least 1 basic_supply for diagnosis). Inspection-only is free. This creates a small incentive to inspect without blocking the "just fix it" path.
- **(D)** Accept that direct repair is the dominant strategy and remove the inspection mechanic entirely. Unchecked would then only serve as a post-voyage efficiency tax that auto-clears on docking or after a time delay.

**Recommended: (A)** with a modification — repair of unchecked modules costs the FULL repair cost unconditionally. Players who inspect first and find the module undamaged save the repair cost. This makes inspection a meaningful risk/reward decision: "Should I check and risk finding damage, or just pay to fix it? But what if it's fine and I waste materials?"

---

### E-3 (BLOCKER): Hull Repair Granularity — All-or-Nothing Healing Creates Waste Trap

**The granularity problem:**

`hull_repair_value_per_repair_kit` = 25 integrity. Starting repair_kits = 2.

This means healing comes in 25-point chunks only. If integrity = 99, any repair wastes 24/25 = 96% of a repair_kit's value. The GDD acknowledges this in EC-07 ("多余修复值不保留、不退款") but provides no mitigation beyond a UI warning.

The problem is compounded by the hull band thresholds:
- intact → damaged at integrity ≤ 75
- damaged → critical at integrity ≤ 25

If a player takes 5 damage (integrity = 95), they face a dilemma:
- Don't repair: need to take 20 more damage before any penalty. Fine.
- Repair with a kit: wastes 20/25 of the kit. Terrible value.
- There is no middle option.

If a player takes 26 damage (integrity = 74, entering damaged band), they face:
- Don't repair: endure 10% speed penalty and 15% fuel penalty on NEXT voyage.
- Repair with one kit: integrity → 99, 1 point wasted. Good value (24/25 used).
- This is the only scenario where repair is economically rational.

The 25-point chunk creates a "repair threshold" — repair is only sensible when integrity drops by at least 20 points (to minimize waste). Below that, repair is economically irrational. This pushes players into a degenerate pattern: deliberately let integrity degrade until near the band threshold, then repair in one big chunk. The fantasy of "I patch up my ship after every voyage" is economically punished.

**Why this is a blocker:** The game's core fantasy is "飞艇是家，不只是载具" and "伤痕是航志." Players are supposed to care about their ship's condition and maintain it. The repair economy should support frequent, small repairs (the "always maintain" fantasy) as well as dramatic post-disaster repairs (the "barely survived" fantasy). The current design only supports the latter.

**Recommendation options:**
- **(A)** Reduce `hull_repair_value_per_repair_kit` to 5-10 and proportionally reduce the cost (fractional kit consumption). This enables granular repairs. Risk: different balancing of repair_kit economy.
- **(B)** Keep repair_kit at 25 but allow fractional use — repair_kit has "durability" or "charges" (e.g., 5 charges per kit, each restoring 5 integrity). Player can choose how many charges to spend.
- **(C)** Add a second repair material: "patch_material" with low repair_value (e.g., 3-5 per unit) and different acquisition sources. Repair_kit is for major repairs, patch_material for routine maintenance.
- **(D)** Move waste prevention into mechanics: repair at full health is rejected (already in rule 18), and repair that would waste >50% of value gives an "inefficient repair" warning with an alternative to use fewer materials. But requires multi-material repair confirmation UI.

**Recommended: (B)** — fractional kit usage preserves the repair_kit as a recognizable item while enabling granular repairs. A repair_kit with 5 charges (5 integrity per charge = 25 total) lets players use 1 charge to fix light damage and 5 charges for a full repair.

---

### E-4 (BLOCKER): Trapped-Goods Loss Formula Discontinuity — Punishes Middle-Sized Shipments

**Loss formula analysis** (from `resources-goods-capacity.md` EC-05, referenced by this GDD's module-destroyed scenario):

`loss = min(Q-1, max(1, ceil(Q×0.4)))`

| Q | loss | retention | loss % |
|---|------|-----------|--------|
| 1 | 0 | 1 | 0.0% |
| 2 | 1 | 1 | 50.0% |
| 3 | 2 | 1 | **66.7%** (worst!) |
| 4 | 2 | 2 | 50.0% |
| 5 | 2 | 3 | 40.0% |
| 10 | 4 | 6 | 40.0% |
| 20 | 8 | 12 | 40.0% |
| 100 | 40 | 60 | 40.0% |

**Three problems:**

1. **Discontinuity at Q=3**: A shipment of 3 units loses 66.7% — more than Q=2 (50%) AND more than Q=4 (50%). This is an artifact of `ceil(Q×0.4)` jumping from ceil(1.2)=2 to staying at ceil(1.6)=2 while Q climbs from 3 to 5. Q=3 is uniquely punished.

2. **Incentive to split**: The per-stack formula incentivizes shipping goods as many small Q=1 stacks rather than one large stack — but the player cannot control this because Q is set by the market system (`空港 / 村镇状态与集市交易`) when goods are purchased. The market system's Q values will determine whether players are unknowingly buying "fragile" (Q=3) or "robust" (Q=1) cargo packages. This is an invisible risk the player cannot manage.

3. **"At least 1 preserved" constraint `min(Q-1, ...)` is well-intentioned but creates the discontinuity.** The formula has two competing goals: (a) asymptotic 40% loss for large Q, and (b) protect single-unit shipments. The `min(Q-1, max(1, ceil(Q×0.4)))` structure means the two terms cross at Q=3, creating the discontinuity.

**Why this is a blocker:** The loss formula is the primary economic consequence of module destruction in combat — it determines what's at stake when a player's cargo module is hit. A mathematical discontinuity at Q=3 means players who happen to buy goods in stacks of 3 (as determined by the market system, outside player control) are disproportionately punished. This is feel-bad design: the player did nothing wrong, but the math punishes them harder for reasons they can't understand or predict.

**Additional note**: The modules GDD should own the loss formula since it owns module destruction consequences, but the loss formula currently lives in resources-goods-capacity.md EC-05. Ownership ambiguity is a maintenance risk.

**Recommendation options:**
- **(A)** Adopt a flat percentage: `loss = floor(Q × 0.4)` with special case `max(0, min(Q-1, loss))` to protect Q=1. This gives Q=1: loss 0, Q=2: loss 0, Q=3: loss 1, Q=4: loss 1, Q=5: loss 2, etc. Smoother gradient.
- **(B)** Adopt a tiered system: Q=1-5 → loss 1 (minimum 1 preserved), Q=6-15 → 40%, Q=16+ → capped at some maximum. Predictable and player-understandable.
- **(C)** Reframe the formula around "preservation" rather than "loss": `retention = max(1, Q - max(1, ceil(Q×0.4)))` simplifies to loss computed per-original but expressed as what the player KEEPS. Better communication, same math.
- **(D)** Move loss calculation to a per-shipment total rather than per-stack: sum all Q values in cargo bay, apply loss to total, then distribute proportionally across stacks. Eliminates stack-size sensitivity entirely.

**Recommended: (A)** with the constraint that after applying `floor(Q × 0.4)`, a post-check ensures `retention ≥ 1` for any Q ≥ 1. This is simpler, produces a smooth curve, and preserves the "at least 1 protected" pillar 4 constraint.

---

### E-5 (RECOMMENDED): Module Install Costs Exceed Starting Resources — First-Exploration Boom/Bust

**The numbers:**

| Action | basic_supply required | repair_kit required |
|--------|----------------------|---------------------|
| Starting resources | 10 | 2 |
| Install scout in slot A | 5 | 2 |
| Remaining after scout install | 5 | **0** |
| Uninstall scout + install cargo in A | 6 | 4 |
| Uninstall cargo + install scout in B | 10 | 5 |
| Any dual-module configuration change | 6-13 | 4-8 |

**Analysis:** After the first exploration and scout installation, the player has ZERO repair_kits. They cannot:
- Reinstall cargo if they ever uninstall it (needs 3 repair_kits, have 0)
- Repair a damaged module (needs 2 repair_kits, have 0)
- Repair hull at all (needs 1+ repair_kit, have 0)

The player MUST succeed at their second exploration to earn repair_kits before they have any economic agency. If the second exploration damages a module, the player is stuck with reduced efficiency until they accumulate repair_kits through exploration — but exploration with damaged modules is harder (less cargo space, lower M_max, lower scout visibility).

This creates a "boom/bust" dynamic: successful early explorations unlock the full economic game; failed or damaging explorations create a downward spiral that's hard to escape.

**Is this intentional?** The fantasy section describes "双货仓的飞艇侧面多出两块亲手'拼上去'的舱室" and rapid configuration changes. But the economic reality is that the player spends their entire starting repair_kit stockpile on the FIRST module installation, and all subsequent economic decisions are gated behind successful exploration outcomes.

**Recommendation options:**
- **(A)** Increase starting repair_kits from 2 to 5. Gives the player ~2 configuration changes or repairs before needing exploration income.
- **(B)** Reduce install costs: scout = basic×3 + repair×1, cargo = basic×2 + repair×2. Total repair_kit cost for first scout install = 1, leaving 1 in reserve.
- **(C)** Add a "new captain's stipend" — an NPC gives the player repair_kits × 3 as part of the scout module delivery, framed as "starter supplies for your new module."
- **(D)** Keep current costs but accept the tight early game as intentional friction. Document in the GDD that the first 2-3 voyages are a "proving period" where module experimentation is unavailable.

**Recommended: (B)** combined with E-1 recommendation (C) — lower base costs + swap operation. Total repair_kit cost for initial setup would be 1 (scout install), leaving 1 for first repair. After first successful exploration, player earns enough to experiment.

---

### E-6 (RECOMMENDED): No Wealth Ceiling or Sink — Long-Term Resource Accumulation Has No Outlet

**The resource flow model:**

```
Faucets (income):
  - Exploration loot (frequency/quantity TBD by exploration GDD)
  - Market trading (TBD by settlement GDD)
  - Starting resources (one-time)

Sinks (expenditure):
  - Module installation (one-time per configuration)
  - Module repair (occasional, combat-dependent)
  - Hull repair (occasional, voyage-dependent)
  - Route consumption (navigation supplies — TBD by navigation GDD)
```

**The problem:** Module installation and repair are sporadic, event-driven sinks. They do not create a steady resource drain. Once the player settles on a configuration and maintains good repair habits, resources accumulate without meaningful outlets.

Specific concerns:
1. **Module installation is a one-time sink**: After the player has their preferred modules installed (scout + cargo, dual cargo, etc.), installation costs disappear from the economy. There's no reason to keep spending materials on modules.
2. **Repair is infrequent**: Repair only triggers when modules/hull are damaged in combat. If the player takes safe routes, repair costs approach zero.
3. **No ongoing module maintenance**: Unlike hull integrity (which degrades via voyage events), modules don't have wear-and-tear. A module installed at hour 1 is identical to the same module at hour 50, assuming no combat damage.
4. **No upgrade path**: Modules have no tiers, no improvements. The scout module is the scout module — it never gets better. This means the "模块选择即身份表达" identity is static rather than evolving.

**The long-term economic curve**: Resources accumulate monotonically after the early game. The only "exciting" economic moments are post-combat repair decisions. The rest is hoarding.

**Recommendation options:**
- **(A)** Add module degradation: each voyage reduces module efficiency by 1-3% (cumulative), requiring periodic maintenance repair. This creates a steady, predictable resource sink that reinforces the "maintain your ship" fantasy.
- **(B)** Add optional module upgrades: spend resources to upgrade furnace_rating (+2 to M_max), volume_bonus (+100 to V_effective), or scout range. Creates new resource sinks while deepening the identity fantasy.
- **(C)** Tie module health to hull integrity: when hull takes damage, installed modules have a chance of taking minor damage too. This increases repair frequency without needing module degradation.
- **(D)** Accept the current design as MVP-appropriate. Module economics are a foundation — deeper sinks (upgrades, degradation, tiered modules) are post-MVP features. Document this as a known limitation.

**Recommended: (D) for MVP** with explicit documentation that module degradation/upgrades are planned post-MVP features. The current economy is sufficient for the 2-3 hour MVP loop. However, the GDD should note this as an Open Question with recommended post-MVP direction.

---

### E-7 (RECOMMENDED): Repair Material Acquisition Rate Is Undefined — Cross-System Dependency Risk

**The unanswerable question:** How many repair_kits does a player earn per exploration run?

This single number determines:
- Whether hull integrity is a meaningful constraint or a non-issue
- Whether module repair is a strategic decision or a trivial formality
- Whether the 50% uninstall refund is a real cost or just busywork
- Whether players avoid or embrace risky routes

**Current state:**
- Starting repair_kits: 2 (from resources GDD Starting State)
- Hull repair per kit: 25 integrity (from modules GDD Tuning Knobs)
- Module repair per action: repair_kit × 2 (from modules GDD Tuning Knobs)
- Acquisition rate: **NOT DEFINED**

This is the single most important open economic variable. It's owned by the exploration GDD (#11), but the exploration GDD is not yet designed. The modules GDD has an implicit assumption about repair material scarcity that must be explicitly stated as a design constraint.

**The economic stability condition**: For hull integrity to matter without being punitive, the repair_kit acquisition rate should roughly balance damage intake for a "typical" voyage:
- If average voyage damage = 15-20 integrity and gives 1 repair_kit (25 repair): player slowly gains repair surplus. Hull slowly degrades but is manageable. Good.
- If average damage = 5-10 and gives 1 repair_kit: hull becomes trivial. Repair kits pile up.
- If average damage = 25-35 and gives 0-1 repair_kit: hull degrades faster than repairable. Players avoid risk entirely.

**Recommendation:** Add to this GDD's Dependencies section a design constraint:

> **Exploration repair_kit acquisition constraint**: The exploration system (#11) should target 0.5-1.5 repair_kits earned per typical voyage (adjusted by risk level), such that a player running "medium risk" voyages can maintain hull integrity through regular repair but experiences net hull degradation when consistently taking high-risk routes. Module repair costs (2 repair_kits per module) should represent ~1-3 voyages of repair_kit income, making module repair a meaningful but not punitive decision.

This gives the exploration designer a target to calibrate against.

---

### E-8 (RECOMMENDED): No Economic Distinction Between Module Types Beyond Costs

**Observation:** The two module types have different install costs (scout: basic-heavy, cargo: repair-heavy) but no other economic differentiation. Both:
- Have equal repair costs (repair_kit × 2)
- Have the same damaged refund rate (0%)
- Have the same uninstall refund rate (50%)
- Have no ongoing operating costs
- Have no upgrade paths

The cost asymmetry (basic vs repair_kit emphasis) is the only economic signal about module identity. This is thin for a system that supposedly represents "身份表达."

**Recommendation:** Consider differentiating module economics further:
- Scout modules (sensitive instruments) might cost more to repair but less to install
- Cargo modules (rugged containers) might be cheaper to repair but heavier on fuel
- This would make the economic identity of each module match its functional identity

This is a low-priority recommendation for post-MVP.

---

### E-9 (RECOMMENDED): No Mechanism to Signal Repair Material Scarcity Before First Voyage

**The new player problem:** A first-time player has repair_kits × 2 and has no idea whether this is "a lot" or "very little." They don't know:
- How much damage a typical voyage inflicts
- How many repair_kits they'll find in exploration
- What the consequences of running out are

The current design relies on the player learning through failure. The first time they run out of repair_kits and face a damaged module they can't fix, they'll understand scarcity — but this is a punitive learning curve.

**Recommendation:** The Hub departure confirmation (airship-hub.md R9) already shows "模块完好度摘要." Consider adding a subtle indicator: "维修物资: 充足 / 有限 / 耗尽" based on repair_kit count in storage. This gives players a soft warning before they depart without repair materials, without being a tutorial popup. The threshold could be: ≥3 = 充足 (green), 1-2 = 有限 (yellow), 0 = 耗尽 (red).

This would be implemented in the Hub system (#7) or UI system (#16), not this GDD — but the design intent should be noted here as a cross-system concern.

---

### Additional Economic Observations (Non-Binding)

**AO-1: Scout module "redundancy protection" has hidden economic value.** Rule 12 states "任一侦察模块完好即提供完整侦察效果." This means dual-scout configuration saves the player from EVER needing to repair a scout module for functional reasons — if one is damaged, the other covers. This makes dual-scout the most repair-efficient configuration, saving repair_kit × 2 per damaged scout event. The GDD doesn't acknowledge this economic advantage, which is significant. A player running dual-scout effectively pays zero repair costs for scout functionality.

**AO-2: Cargo module efficiency loss has cascading economic effects.** When a cargo module drops to 50% efficiency (damaged), M_max drops from 20 to 14 (scout+cargo) or from 24 to 18 (dual cargo). If the player had cargo loaded near the old M_max, they're now overloaded and blocked from departure. This creates a "double punishment": not only did you take module damage, but you also can't leave until you either repair the module OR unload cargo. For a player who loaded up for a long trading route, this is potentially game-disrupting. Consider whether the overload block should be a soft limit (can depart with speed penalty) rather than a hard block.

**AO-3: The hull integrity → hull_scars relationship creates an economic gradient across band thresholds.** Since each band transition adds +1 scar, a player who lets hull decay from 100→0 in one catastrophic event gets 4 scars (base + 3 band transitions). A player who repairs frequently and bounces between intact↔damaged accumulates more scars over time for the same total damage (each crossing adds +1, even if repeatedly crossing the same threshold). Over a long game, the frequent-repairer will have MORE scars than the risk-taker who takes one big hit. Is this intentional? It seems to punish the "careful maintainer" archetype with more visible scarring.

**AO-4: Starting cargo pre-install solves chicken-and-egg but creates a "why would I ever uninstall this?" trap.** Since the cargo module is free (pre-installed) but reinstalling it costs basic×3 + repair×3, the player has a strong disincentive to ever uninstall it. This means the "open slot" design is effectively single-slot in the early game — slot B is permanently occupied by the cargo module unless the player accepts a large economic penalty. Combined with E-1, this means the player's "choice" is really about slot A only for the first several hours.

---

### Summary

| Category | Count |
|----------|-------|
| BLOCKER findings | 4 (experimentation lock-in; unchecked repair exploit; repair granularity waste; loss formula discontinuity) |
| RECOMMENDED findings | 5 (starting resources insufficient; no long-term sink; repair acquisition undefined; no module economic differentiation; no scarcity signaling) |
| Additional observations | 4 (scout redundancy value; cargo double-punishment; scars maintenance paradox; cargo pre-install lock-in) |

**BLOCKER-1 (E-ECON-SWAP-LOCK)**: 50% uninstall refund + floor() + high install costs make configuration changes economically impossible with starting resources. Fantasy of "open slots" and "identity expression through module choice" is undermined by an economy that locks the player into their first configuration. Combined with E-5 (install costs exceed starting resources after first scout install), the player has effectively zero economic agency for module choices in the early game.

**BLOCKER-2 (E-ECON-REPAIR-EXPLOIT)**: Unchecked + direct repair with zero/trivial cost for undamaged modules creates a dominant strategy that makes the inspection mechanic irrelevant. Players will never inspect modules — they will always repair directly, bypassing the intended post-voyage tension loop.

**BLOCKER-3 (E-ECON-REPAIR-GRANULARITY)**: Hull repair comes in 25-point chunks only, making small repairs economically irrational (up to 96% waste). This pushes players toward a degenerate "let it degrade then bulk repair" pattern that contradicts the "always maintain your ship" fantasy.

**BLOCKER-4 (E-ECON-LOSS-DISCONTINUITY)**: The cargo loss formula `loss = min(Q-1, max(1, ceil(Q×0.4)))` produces a 66.7% loss rate at Q=3 — worse than Q=2 or Q=4. This is a mathematical artifact, not intentional design. Since Q is set by the market system outside player control, players can be disproportionately punished without agency.

**Verdict: MAJOR REVISION NEEDED** — the four blockers above must be resolved before the module economy can support the promised player fantasy. The five recommendations should be addressed but do not independently block implementation. The four additional observations are for design awareness only.

**Cross-cutting theme**: The GDD's economic parameters are individually reasonable but create emergent problems when composed. The 50% refund, high install costs, 0% damaged refund, 25-point repair granularity, and starting resource pool interact to create a highly constrained early game where the player has very little economic freedom. The fantasy promises "freedom to express identity through module choice" — the economy delivers "pick one configuration and hope you picked right."

---

## Review — 2026-05-01 — Verdict: MAJOR REVISION NEEDED → ROUND 2 REVISED (awaiting re-review)

Scope signal: M
Specialists: game-designer, systems-designer, economy-designer, qa-lead, gameplay-programmer, creative-director
Blocking items: 8 | Recommended: 8 | Resolved in Round 2: 8 blockers + 2 recommendations

Summary: Round 2 full adversarial review with 5 specialists + creative-director synthesis. Found 8 blocking issues: B1 (damaged→unchecked state machine inconsistency), B2 (unchecked repair exploit from zero cost), B3 (50% uninstall refund locking player into first config), B4 (25-point repair granularity forcing degenerate bulk-repair), B5 (hull_scars count error for 30→0 cross-band), B6 (missing η_hull_band factor in D.4), B7 (Hub GDD unaware of unchecked state), B8 (missing actual_state_changed signal). All 8 blockers resolved in-session: B3 increased refund to 75% + swap_module atomic operation; B4 reduced repair_kit to 5 integrity/kit; B5 corrected scars math; B6 added η_final formula; B7 added unchecked to Hub visual spec; B8 completed signal contract. Cross-system impacts resolved in airship-hub.md and resources-goods-capacity.md. Combined prior Systems Designer (2) + Economy Designer (4) blockers now resolved.

Prior verdict resolved: Yes (Round 1 7 blockers + 8 recommendations resolved in Round 1; Systems Designer 2 blockers resolved; Economy Designer 4 blockers resolved)

---

## Re-Review (Round 2) — Systems Designer — 2026-05-01 — Verdict: CONDITIONAL APPROVAL (2 blockers + 4 recommendations)

Scope: Formula boundary testing only — 7 specific test items requested by user.
Tag: [systems-designer]

### Executive Summary

Round 2 revisions successfully fixed the 2 prior Systems Designer blockers (B5: cross-band hull_scars, B6: η_final formula). However, the introduction of η_final (D.2b) and the critical band multiplier (×0.8) created **new boundary problems** that did not exist in Round 1. Additionally, the destroyed band's η_hull_band remains undefined — a gap that would cause runtime issues if any consumer queries η_final while integrity=0. Total findings: 2 BLOCKER, 4 MEDIUM, 3 LOW.

---

### Test 1: D.1 M_max — floor() rounding loss across all η_final combinations

**Status: FAIL — 10 single-module and numerous dual-module configurations exceed 5% rounding loss.**

The addition of η_final (D.2b) multiplies the number of pre-floor fractional values. Below is the complete single-module matrix (R_scout=8, R_cargo=12):

| Module type | visible_state | Band | η_final | Pre-floor | Floor | Loss | Loss% |
|------------|-------------|------|---------|-----------|-------|------|-------|
| scout | installed | intact/damaged | 1.00 | 8.00 | 8 | 0.00 | 0% |
| scout | installed | **critical** | **0.80** | **6.40** | **6** | **0.40** | **6.25%** |
| scout | unchecked | intact/damaged | 0.95 | 7.60 | 7 | 0.60 | **7.89%** |
| scout | unchecked | critical | 0.76 | 6.08 | 6 | 0.08 | 1.32% |
| scout | damaged | intact/damaged | 0.60 | 4.80 | 4 | 0.80 | **16.67%** |
| scout | damaged | **critical** | **0.48** | **3.84** | **3** | **0.84** | **21.88%** |
| cargo | installed | intact/damaged | 1.00 | 12.00 | 12 | 0.00 | 0% |
| cargo | installed | **critical** | **0.80** | **9.60** | **9** | **0.60** | **6.25%** |
| cargo | unchecked | intact/damaged | 0.95 | 11.40 | 11 | 0.40 | 3.51% |
| cargo | unchecked | critical | 0.76 | 9.12 | 9 | 0.12 | 1.32% |
| cargo | damaged | intact/damaged | 0.50 | 6.00 | 6 | 0.00 | 0% |
| cargo | damaged | **critical** | **0.40** | **4.80** | **4** | **0.80** | **16.67%** |

**6 single-module configurations exceed 5%** (bold rows above). The worst case is damaged scout in critical band: 21.88% loss — nearly a quarter of the remaining capacity is discarded by floor().

**Worst dual-module combinations (>5% loss):**

| Slot A | Slot B | Band | Pre-floor | Floor | Loss% |
|--------|--------|------|-----------|-------|-------|
| scout dmg (0.48) | cargo dmg (0.40) | critical | 3.84+4.80=8.64 | 8 | 7.41% |
| scout dmg (0.48) | cargo inst (0.80) | critical | 3.84+9.60=13.44 | 13 | 3.27% |
| scout dmg (0.48) | cargo unchk (0.76) | critical | 3.84+9.12=12.96 | 12 | 7.41% |
| scout dmg (0.60) | cargo dmg (0.50) | intact | 4.80+6.00=10.80 | 10 | 7.41% |
| scout dmg (0.60) | cargo unchk (0.95) | intact | 4.80+11.40=16.20 | 16 | 1.23% |
| scout unchk (0.95) | cargo dmg (0.50) | intact | 7.60+6.00=13.60 | 13 | 4.41% |
| scout dmg (0.48) | scout dmg (0.48) | critical | 7.68 | 7 | 8.85% |

**Root cause**: R_furnace_scout=8 produces fractions with ALL η values except 1.0 and 0.5 (8×0.5=4.0, 8×0.25=2.0, etc.). The critical band ×0.8 then makes previously integral values (8.0, 12.0) fractional too.

**Comparison to Round 1**: Round 1 had 3 configurations >5% (damaged scout intact 16.7%, unchecked scout intact 7.9%, both damaged intact 7.4%). Round 2 adds: scout installed critical 6.25%, cargo installed critical 6.25%, scout damaged critical 21.88%, cargo damaged critical 16.67%. The critical band multiplier introduced in Round 2 **tripled** the number of problematic configurations.

**VERDICT: MEDIUM**. Not degenerate (no negatives, no division by zero), but significant precision loss with no documented design rationale for floor().

**Fix options**:
- (a) Scale R_furnace_scout to 10 (10×0.6=6.0, 10×0.8=8.0, 10×0.48=4.8 still fractional). Incomplete fix.
- (b) Scale all R_furnace values by 5×: scout=40, cargo=60. Then divide displayed M_max by 5. This eliminates ALL fractions (40×0.6=24, 40×0.48=19.2... still broken). Hmm, 40×0.48=19.2 — still fractional. Need LCM analysis.
- (c) The only fractional-producing η_final values are 0.6, 0.95, 0.8, 0.48, 0.4, 0.76. With R_scout=8: 8×0.6=4.8, 8×0.95=7.6, 8×0.8=6.4, 8×0.48=3.84, 8×0.76=6.08. All produce fractions because R=8 doesn't divide evenly. With R_cargo=12: 12×0.8=9.6 (fraction), 12×0.4=4.8 (fraction). A scale factor of 25 would fix all: scout=200, cargo=300, M_max/25. But this is a large multiplier.
- (d) **Recommended**: Document the worst-case losses in Tuning Knobs, keep floor() as intentional (conservative capacity calculation — the ship slightly under-reports capacity rather than over-reporting), and add a note explaining the design rationale. For MVP, 16-22% loss on a damaged-scout-critical-edge-case is acceptable friction — the player has bigger problems at that point (critical hull + damaged module + only one module installed).

---

### Test 2: D.2b η_final — verify all 4 hull bands

**Status: CONDITIONAL PASS — correct for 3 bands, undefined for destroyed band.**

D.2b: `η_final = η_visible × η_hull_band`

η_hull_band per D.3:
| Band | η_hull_band | Status |
|------|------------|--------|
| intact (76-100) | 1.0 | Correct |
| damaged (26-75) | 1.0 | Correct |
| critical (1-25) | 0.8 | Correct |
| destroyed (0) | **N/A** | **UNDEFINED** |

The issue: D.2b's variable table says `η_hull_band` range is "intact/damaged=1.0, critical=0.8, destroyed=N/A." D.3's table says the destroyed row has "—" for all modifiers (模块效率额外修正 = —).

**What happens when η_final is queried at integrity=0?**

The GDD correctly notes that destroyed band means "无法出航" and can_depart() checks integrity>0 first. However:
- `hull_band_changed` fires when band transitions to destroyed
- `module_efficiency_changed` may fire as a consequence (signal ordering: actual_state_changed → slot_state_changed → module_efficiency_changed → departure_readiness_changed)
- UI consumers may query η_final to display module status even when ship is destroyed
- V_effective (D.4) uses η_final and might be queried to show trapped goods status

If η_hull_band is literally undefined (null/NaN) at destroyed band, any query to η_final would propagate that undefined value through D.1 and D.4, potentially causing NaN in UI displays or error states in state consumers.

**Additionally**: When the player repairs from integrity=0→5 (destroyed→critical), η_hull_band transitions from "undefined" to 0.8. The `module_efficiency_changed` signal would need to compute old_eff (undefined) vs new_eff (e.g., 0.8). This is a state transition crossing an undefined gap.

**VERDICT: BLOCKER**. An undefined variable in a formula that has defined consumers is a runtime hazard.

**Fix**: Define η_hull_band = 0 for destroyed band. Rationale: when the hull is destroyed, all module effects are nullified — furnaces produce no lift, cargo bays hold no accessible volume, scout instruments return no data. This is consistent with the "—" entries in D.3 (which mean "not applicable because ship cannot operate") and produces safe values: η_final=0, M_max=0, V_effective=0, all of which are handled correctly by existing edge cases and formulas.

Also: add `η_hull_band = 0` explicitly to the D.3 table's destroyed row (replacing "—" in the 模块效率额外修正 column) and to the D.2b variable table.

---

### Test 3: D.3 Band transitions — boundary value verification

**Status: PASS — no off-by-one errors found.**

Band definitions:
| Range | Band |
|-------|------|
| 76–100 | intact |
| 26–75 | damaged |
| 1–25 | critical |
| 0 | destroyed |

State machine transition conditions (verified symmetric):

| Transition | Condition | Boundary | Result |
|-----------|-----------|----------|--------|
| intact→damaged | integrity ≤ 75 | 76→75 crosses | ✓ |
| damaged→intact | integrity ≥ 76 | 75→76 crosses | ✓ |
| damaged→critical | integrity ≤ 25 | 26→25 crosses | ✓ |
| critical→damaged | integrity ≥ 26 | 25→26 crosses | ✓ |
| critical→destroyed | integrity = 0 | 1→0 crosses | ✓ |
| destroyed→critical | integrity ≥ 1 | 0→1 crosses | ✓ |

All transitions are symmetric (upward threshold = downward threshold + 1). The band definitions and state machine agree exactly.

**Additional boundary test — "entered" semantics for hull_scars:**

| Damage event | Starting band | Bands entered | Scars | Correct? |
|-------------|--------------|--------------|-------|-----------|
| 100→75 | intact | damaged | +2 | ✓ (base + damaged) |
| 76→75 | intact | damaged | +2 | ✓ (base + damaged) |
| 75→26 | damaged | — | +1 | ✓ (base only, no new band) |
| 26→25 | damaged | critical | +2 | ✓ (base + critical) |
| 25→1 | critical | — | +1 | ✓ (base only, no new band) |
| 1→0 | critical | destroyed | +2 | ✓ (base + destroyed) |
| 100→0 | intact | damaged, critical, destroyed | +4 | ✓ (base + 3 bands) |
| 30→0 | damaged | critical, destroyed | +3 | ✓ (Round 2 fix correct) |

All consistent. No off-by-one errors. The Round 2 fix (B5) correctly changed 30→0 from +4 to +3.

**VERDICT: PASS**.

---

### Test 4: D.5 can_depart — all edge combinations

**Status: PASS — all 8 truth table combinations produce correct results.**

D.5: `can_depart = (M_max > 0) AND (integrity > 0) AND (M_loaded ≤ M_max)`

| # | M_max>0 | integrity>0 | M_loaded≤M_max | Result | Reasons |
|---|---------|-------------|----------------|--------|---------|
| 1 | T | T | T | **true** | [] |
| 2 | F | T | T | false | ["no_furnace"] |
| 3 | T | F | T | false | ["hull_destroyed"] |
| 4 | T | T | F | false | ["overloaded"] |
| 5 | F | F | T | false | ["no_furnace", "hull_destroyed"] |
| 6 | F | T | F | false | ["no_furnace", "overloaded"] |
| 7 | T | F | F | false | ["hull_destroyed", "overloaded"] |
| 8 | F | F | F | false | ["no_furnace", "hull_destroyed", "overloaded"] |

**Special case — M_max=0 and M_loaded=0**: Row 5 above assumes M_loaded=0 (empty cargo). 0≤0 is true, so overloaded condition is NOT triggered. Result: ["no_furnace", "hull_destroyed"] only. Correct — an empty cargo bay on a destroyed, unpowered ship is not "overloaded."

No degenerate outputs. All reasons arrays correctly enumerate failing conditions per AC-21.

**VERDICT: PASS**.

---

### Test 5: State machine — post-voyage flow, damaged module reset check

**Status: PASS — damaged modules do NOT reset to unchecked.**

**Path analysis:**

**Path A: damaged (known) → voyage → post-voyage**
- State machine row: `damaged → 航行中再次受损 → damaged | 维持对应值（不提升至 0.95）`
- Post-voyage flow (line 127): `出航前 actual=damaged 的模块：visible_state 维持 damaged（效率不恢复）`
- Result: visible stays `damaged`, η stays at 0.5/0.6. ✓ No reset.

**Path B: damaged (known) → voyage → no additional damage**
- State machine: no explicit "damaged + no new damage" row, but post-voyage flow covers it: actual=damaged pre-voyage → visible stays damaged regardless.
- Result: visible stays `damaged`. ✓ No reset. The module doesn't "get better" by taking a safe voyage.

**Path C: unchecked (actual=damaged) → voyage → post-voyage**
- Pre-voyage actual=damaged → post-voyage visible becomes `damaged` (per flow)
- This reveals the hidden damage after one grace voyage
- Result: visible becomes `damaged`. ✓ No reset to unchecked — the damage is revealed, not hidden further.

**Path D: unchecked (actual=installed) → voyage → no damage**
- Pre-voyage actual=installed → post-voyage visible becomes `unchecked`
- Result: visible stays `unchecked` (was already unchecked). η stays 0.95. ✓ Consistent.

**The "grace period" design**: The unchecked state hides damage for exactly ONE post-voyage period. If the player departs again without checking and the module was actually damaged, the damage is revealed upon return. This prevents the "never check" exploit from being indefinite — the hidden damage benefit lasts exactly one voyage.

**VERDICT: PASS**. The Round 2 fix (B1) correctly prevents damaged→unchecked reset. The actual_state-based post-voyage logic is consistent across all paths.

---

### Test 6: swap_module math — net cost verification

**Status: PASS for non-negative outputs. AMBIGUITY for multi-material arithmetic.**

Rule 10a: `net = max(0, new_install_cost − refund_for_old)`, where `refund_for_old = ceil(old_install_cost × 0.75)` per material, or 0 if damaged.

Using tuning values:
- scout: basic×5 + repair×2
- cargo: basic×3 + repair×3

**Scenario: scout→cargo swap (both installed)**

| Material | refund (ceil×0.75) | new | max(0, new-refund) |
|----------|---------------------|-----|---------------------|
| basic_supply | ceil(5×0.75)=4 | 3 | max(0, −1) = **0** |
| repair_kit | ceil(2×0.75)=2 | 3 | max(0, 1) = **1** |

Net: 0 basic + 1 repair_kit. Non-negative. ✓

**Scenario: cargo→scout swap (both installed)**

| Material | refund (ceil×0.75) | new | max(0, new-refund) |
|----------|---------------------|-----|---------------------|
| basic_supply | ceil(3×0.75)=3 | 5 | max(0, 2) = **2** |
| repair_kit | ceil(3×0.75)=3 | 2 | max(0, −1) = **0** |

Net: 2 basic + 0 repair. Non-negative. ✓

**Scenario: swap with damaged old module**
- refund_for_old = 0 for all materials
- net = full new install cost. Non-negative. ✓

**Scenario: swap with unchecked old module**
- Per state machine: unchecked→uninstall gives 0 refund (same as damaged)
- net = full new install cost. Non-negative. ✓
- Design note: player should check first (free) to potentially get 75% refund.

**Scenario: swap with empty old slot**
- Nothing to refund. net = full new install cost. Correct.

**Ambiguity**: The formula `max(0, new_install_cost − refund_for_old)` treats costs as scalars, but they are multi-material vectors. The per-material interpretation (shown above) is the only one that produces sensible results without requiring a resource-value conversion table. This should be specified explicitly: "For each material type independently, net_cost[m] = max(0, new_cost[m] − refund[m]). Total net cost is the sum of positive differences across all material types."

**VERDICT: PASS with documentation note**. No degenerate outputs (no negative costs, no division). Formula produces non-negative values under both per-material and aggregate interpretations. Add explicit per-material clarification.

---

### Test 7: hull_scars cross-band calculation — comprehensive verification

**Status: PASS — all scenarios consistent.**

**Base rule**: Each damage event gives `hull_scars += 1` (base) + `+1 per newly entered band`. "Newly entered" means a band the ship was NOT in before the damage event.

**Verified scenarios:**

| Start integrity | End integrity | Start band | Bands entered | Scars Δ | Formula |
|----------------|--------------|------------|--------------|---------|---------|
| 100 | 100 | intact | — | +1 | base only (1 dmg event) |
| 100 | 76 | intact | — | +1 | base only (stayed intact) |
| 100 | 75 | intact | damaged | +2 | base + damaged |
| 76 | 75 | intact | damaged | +2 | base + damaged |
| 75 | 26 | damaged | — | +1 | base only (stayed damaged) |
| 50 | 25 | damaged | critical | +2 | base + critical |
| 26 | 25 | damaged | critical | +2 | base + critical |
| 25 | 1 | critical | — | +1 | base only (stayed critical) |
| 5 | 0 | critical | destroyed | +2 | base + destroyed |
| 1 | 0 | critical | destroyed | +2 | base + destroyed |
| 80 | 50 | intact | damaged | +2 | base + damaged |
| 80 | 25 | intact | damaged, critical | +3 | base + damaged + critical |
| 80 | 0 | intact | damaged, critical, destroyed | +4 | base + 3 bands (AC-29 correct) |
| 30 | 0 | damaged | critical, destroyed | +3 | base + 2 bands (EC-12/AC-29 correct after Round 2 fix) |
| 100 | 0 | intact | damaged, critical, destroyed | +4 | base + 3 bands |
| 26 | 0 | damaged | critical, destroyed | +3 | base + 2 bands |

**Special cases**:
- Repair events (integrity increase): no scars added (scars are damage-only). ✓
- Band transitions upward (destroyed→critical, critical→damaged, damaged→intact): no scars added. ✓
- 0 damage event (integrity unchanged): no damage event, so no base +1. As defined: "每次航行损伤事件（integrity 减少的任意事件）". If integrity doesn't decrease, it's not a damage event. ✓

**Consistency check — AC-13**: 100→75, +2. Correct. ✓
**Consistency check — AC-14**: 26→25, +2. Correct. ✓
**Consistency check — AC-29 (corrected)**: 30→0, +3. Correct. ✓
**Consistency check — AC-29 (intact case)**: 80→0, +4. Correct. ✓

**VERDICT: PASS**. The Round 2 fix (B5) correctly resolves the cross-band inconsistency. All boundary scenarios produce consistent, non-degenerate results. No remaining off-by-one or double-counting errors.

---

### Additional Finding: D.4 Variable Table — Missing η_final Values

**Status: MEDIUM**

D.4 variable table lists η_final_A and η_final_B value set as: `0/0.4/0.5/0.8/0.95/1.0`

This set is incomplete. The full set of possible η_final values for a cargo module is:

| visible_state | Band | η_final | In listed set? |
|--------------|------|---------|----------------|
| — (empty slot) | any | 0 | ✓ |
| damaged | critical | 0.5×0.8=**0.40** | ✓ |
| damaged | intact/damaged | **0.50** | ✓ |
| unchecked | critical | 0.95×0.8=**0.76** | **MISSING** |
| installed | critical | 1.0×0.8=**0.80** | ✓ |
| unchecked | intact/damaged | **0.95** | ✓ |
| installed | intact/damaged | **1.00** | ✓ |

The value 0.76 (cargo unchecked + critical band) is missing. It would be produced when a player returns from a voyage with hull in critical band and an unchecked cargo module. While the authoritative source is the D.2b formula (not the enumerated set), an incomplete enumeration in a variable table can mislead implementers into thinking the set is exhaustive.

**Fix**: Change the value set to `0.0–1.0` (continuous range, matching D.1's η_final variable table) with a note listing the common discrete values. Or add 0.76 to the enumerated set and note it derives from unchecked × critical.

---

### Additional Finding: swap_module Per-Material Arithmetic Ambiguity

**Status: LOW**

Rule 10a: `net = max(0, new_install_cost − refund_for_old)`

The formula uses scalar subtraction notation but the operands are multi-material cost vectors (e.g., `{basic: 5, repair: 2}`). The AC-02a/AC-02b examples compute refund per-material with per-material ceil. The swap formula must specify whether subtraction is per-material or aggregate.

Both interpretations produce non-negative outputs, so this is not a math degeneracy. But implementation divergence is likely without explicit specification.

**Recommendation**: Add to rule 10a: "For each material type m, net_cost[m] = max(0, new_install_cost[m] − refund_for_old[m]). The total net cost is the set of all material types where net_cost[m] > 0."

---

### Complete Findings Summary

| # | Test | Status | Severity |
|---|------|--------|----------|
| 1 | D.1 floor() >5% loss | FAIL — 10+ configs exceed 5% | MEDIUM |
| 2 | D.2b η_hull_band for destroyed | FAIL — undefined value | **BLOCKER** |
| 3 | D.3 band boundaries | PASS — no off-by-one | — |
| 4 | D.5 can_depart edge combos | PASS — all 8 correct | — |
| 5 | State machine reset check | PASS — no damaged→unchecked | — |
| 6 | swap_module math | PASS — non-negative, minor ambiguity | LOW |
| 7 | hull_scars cross-band | PASS — all scenarios consistent | — |
| 8 | D.4 variable table incomplete | Missing 0.76 | MEDIUM |
| 9 | swap_module per-material spec | Ambiguous subtraction | LOW |
| 10 | D.1 "installed" phrasing | Should be "non-empty modules" | LOW |

### BLOCKER Findings

**BLOCKER-1 (SD-DESTROYED-ETA-NULL)**: η_hull_band is undefined ("N/A") for the destroyed band. This propagates through D.2b → D.1 → D.4, creating an undefined value chain. Any consumer querying η_final, M_max, or V_effective while integrity=0 receives undefined/null/NaN. This is a runtime hazard, especially during destroyed→critical repair transitions when the band changes and signals fire.

**Fix**: Define η_hull_band = 0 for destroyed band. Update D.2b variable table: `intact=1.0, damaged=1.0, critical=0.8, destroyed=0`. Update D.3 table destroyed row: replace "—" with `0` in "模块效率额外修正" column. Add rationale: "When hull is structurally collapsed (destroyed), no modules produce useful output — furnaces provide no lift, cargo bays provide no accessible volume, instruments return no data."

**BLOCKER-2 (SD-CRITICAL-FLOOR-SILENT-NERF)**: The Round 2 addition of critical band's η_hull_band=0.8 significantly expanded the number of D.1 configurations suffering >5% floor() loss. The worst case (scout damaged + critical: 21.88%) was not possible in Round 1. The GDD added a critical-stacking example (0.48, 0.4) but did not surface that this creates a floor-loss penalty on top of the already-severe critical band penalty.

**Fix**: Document the worst-case floor loss in Tuning Knobs with a new parameter: `floor_rounding_loss_max = 21.9%` (scout damaged in critical band, 3.84→3) with a note explaining the design rationale for floor(). Alternatively, change floor() to round() which would reduce worst-case loss from 21.88% to 9.38% (3.84→4 instead of 3.84→3) — still >5% but half the error. Or scale R_furnace values to make all η combinations produce integral results (requires analysis of all η_final×R products).

### Recommendations (Non-Blocking)

1. **D.4 variable table**: Add 0.76 to η_final value set, or change to continuous range notation `0.0–1.0`.
2. **swap_module specification**: Clarify per-material subtraction in rule 10a.
3. **D.1 variable table**: Change η_final range from `0.0–1.0` to explicit `0.0–1.0` (continuous) to avoid confusion with D.4's discrete set.
4. **D.1 example table**: Add at least one critical-band example showing η_final in action (e.g., "双货仓 installed + critical: ⌊12×0.8 + 12×0.8⌋ = 19").
5. **"Installed" wording**: In D.1, change "for all installed modules" to "for all non-empty modules."
6. **D.2b/D.3 terminology**: Consider changing "模块效率额外修正" header to "η_hull_band" for formula-consistency with D.2b.

### Cross-Reference: Registry Status

8 constants and 1 formula remain missing from entities.yaml (carried forward from Round 1 systems designer review). These do not block formula correctness but should be registered before implementation handoff.

---

## Formula Boundary Test Record (Complete)

| Formula | Boundary Tested | Input Range | Output Range | Degenerate? | Verdict |
|---------|----------------|-------------|--------------|-------------|---------|
| D.1 M_max | All η_final × R_furnace products (14 values × 2 slots = 196 combos) | η∈[0,1], R∈{8,12} | [0,24] | No (floor loss ≤21.88%) | PASS with caveat |
| D.2 η_visible | Table lookup, all 4 states × 2 types | state∈{empty,unchecked,installed,damaged}, type∈{scout,cargo} | {0,0.5,0.6,0.95,1.0} | No | PASS |
| D.2b η_final | η_visible × η_hull_band (all combos) | η_visible∈{0,0.5,0.6,0.95,1.0}, band∈{intact,dmg,crit,destroyed} | [0,1.0] | **Yes: η_hull_band undefined for destroyed** | FAIL |
| D.3 band() | 0,1,25,26,75,76,100 | integrity∈[0,100] | {destroyed,critical,dmg,intact} | No | PASS |
| D.4 V_effective | V_base + 500×(η_A+η_B) | η_A,η_B∈[0,1] | [0,1000] | No | PASS |
| D.5 can_depart | All 8 truth table combos | Boolean×3 | {true,false} | No | PASS |
| D.6 repair | integrity_old + R_total, capped 100 | integrity∈[0,99], R_total≥0 | [1,100] | No (R_total<1 rejected) | PASS |
| swap_module | max(0, new-refund) per material | All cost combos | All ≥0 | No (max prevents negatives) | PASS with note |
| hull_scars | cross-band spanning all 4 bands | integrity transitions | [0,∞) | No | PASS |

---

**Final Verdict: CONDITIONAL APPROVAL** — 2 blockers must be resolved before implementation. Both are fixable with targeted edits to D.2b (add η_hull_band=0 for destroyed) and Tuning Knobs (document worst-case floor loss). The remaining findings are documentation improvements that do not affect formula correctness.

**Verified Round 2 fixes**: B5 (cross-band scars +3) confirmed correct. B6 (η_final formula) confirmed correct but needs destroyed-band completion. B4 (repair_kit=5/kit) produces no new degenerate values. B3 (swap_module + 75% refund + ceil) produces non-negative results.

---

## Re-Review — 2026-05-01 — Verdict: CONDITIONAL APPROVAL (all blockers resolved in-session)

Scope signal: M
Specialists: game-designer, systems-designer, economy-designer, qa-lead, gameplay-programmer, creative-director
Blocking items: 5 (from CD synthesis) | Recommended: 5 | Resolved in Round 3: 5 blockers + 5 recommendations

Summary: Full re-review with creative-director synthesis after Round 2 specialists. CD-CONDITIONAL-APPROVAL with 5 mandatory fixes: (1) SD-DESTROYED-ETA-NULL — η_hull_band=0 defined for destroyed band, D.2b and D.3 tables updated; (2) E-SWAP-CARGO-BAY — swap_module now checks cargo bay occupancy before removing cargo module, rejects with ERR_CARGO_BAY_NOT_EMPTY; (3) GP-SWAP-ATOMIC — swap_module replaced with explicit two-phase validation (Phase 1: preconditions; Phase 2: uninstall+install) per GDScript signal contract constraints; (4) QA-SWAP-NO-AC — 6 swap_module ACs added (AC-37 to AC-42); (5) QA-TYPE-TAGS — all 36+ ACs tagged with Logic/UI/Integration type labels. 5 recommended improvements also applied: floor_rounding_loss_max documented in Tuning Knobs (21.9% worst-case); AC-43 band re-entry added; AC-22 split into AC-22a-h; boundary ACs AC-04c/AC-04d added; D.4 η_final value set completed (0.76, 0.48 added).

Cross-system impacts: airship-hub.md (unchecked state visual spec), resources-goods-capacity.md (repair_kit=5 integrity/kit, starting_repair_kit_qty=4).

Prior verdict resolved: Yes (Systems Designer 2 blockers resolved; Economy Designer 4 blockers resolved; Round 2 full review 8 blockers resolved)

GDD Status: CONDITIONAL APPROVAL — implementation-ready pending systems-index approval.
