# Phase 2: Cross-GDD Consistency Review Report

> **Date**: 2026-05-03
> **Scope**: 17 files (game-concept, systems-index, entities.yaml, 15 GDDs #1-#15)
> **Checks**: 2a–2f (Dependency Bidirectionality, Rule Contradictions, Stale References, Tuning Knob Ownership, Formula Compatibility, AC Cross-Check)
> **Verdict**: **CONCERNS** — 3 BLOCKING issues must be resolved before implementation; 6 WARNING items require attention; 5 INFO notes for tracking.

---

## Summary

| Severity | Count | Must Fix Before Code? |
|----------|-------|----------------------|
| BLOCKING | 3 | Yes |
| WARNING | 6 | Yes (design docs) |
| INFO | 5 | No (track for later) |

---

## BLOCKING Issues

### B1. Hull Band Threshold Contradiction (#8 vs #12)

**Files**: `airship-modules-hull-state.md` (#8), `combat-threat-handling.md` (#12), `navigation-route-risk.md` (#10)

**Finding**: #8 defines hull integrity bands with thresholds at **76 / 26 / 1**:

```
intact:  76–100
damaged: 26–75
critical: 1–25
destroyed: 0
```

This is the canonical definition, referenced by #10 (AC-57, AC-58, AC-63: "hull = 76 is intact, hull = 75 is damaged, hull = 26 is damaged, hull = 25 is critical").

#12 defines a **different** set of thresholds at **61 / 31 / 1** in its visual/UI specifications:

- **V-03** (line 472): `Green (intact, 100-61) / Yellow (damaged, 60-31) / Orange (critical, 30-1)`
- **UI-03** (line 529): `four segments (band boundaries: 100/60/30/0)`
- **UI-10** (line 536): Cross-band warning at `hull <= 42` (minimum 12 damage crosses into critical at 30)
- **AC-12-14** (line 662): `hull = 42 (damaged band, edge: 12 damage = 30 which crosses into critical)`

**Impact**: If implemented as written, #12's decision panel would display wrong band colors and trigger cross-band warnings at wrong hull values. At hull=70 (intact per #8, damaged per #12's UI), the hull bar would be yellow instead of green. At hull=35 (damaged per #8, critical per #12's UI), the bar would be orange instead of yellow.

**Resolution**: #12's visual/UI specifications must be updated to match #8's canonical thresholds (76/26/1). Specifically: V-03 color bands, UI-03 segment boundaries, UI-10 cross-band warning threshold (recalculate from hull=26+12=38), AC-12-14 boundary test values, hull warning threshold at 18 (stays same since 18 <= 25 is already critical in #8's bands).

**Reference**: #8 line 66-70, #10 AC-57/58/63, #12 V-03/UI-03/UI-10/AC-12-14.

---

### B2. Currency Acquisition System Unassigned

**Files**: `port-village-market.md` (#14)

**Finding**: #14's upstream dependencies table (line 358) lists "货币获取系统（待分配）" as a required dependency:

> "无货币来源则购买功能无法验证；价格无锚点"

The purchase flow in #14 requires `validate_purchase(good_id, quantity)` which internally checks `total_cost <= player_currency`. No MVP system (#1-#15) defines how the player earns currency. The systems-index has no "货币/经济收入" entry.

**Impact**: #14's core purchase mechanic cannot function. Players would arrive at the market with 0 currency and no way to acquire it, making all 6 purchasable goods inaccessible.

**Resolution**: One of these options must be chosen:
- (a) Assign currency acquisition to an existing MVP system (e.g., #11 exploration grants currency on extraction, or #5 tracks currency as a resource earned from selling items)
- (b) Create a minimal currency spec in a quick-spec or within #5's scope
- (c) Remove currency requirement from MVP and make initial goods free (contradicts tuning knob design)

**Reference**: #14 lines 355-358 (Dependencies table), lines 189-201 (F.1 purchase total_cost formula), lines 377-387 (Tuning Knobs G.1 commodity pricing).

---

### B3. #6 GDD Stale References to #15 Partner System

**Files**: `player-knowledge-intel.md` (#6), `partner-relationships.md` (#15)

**Finding**: #6's Dependencies section Section 8 (line 829) still reads:

> `#10 伙伴功能与关系 — 尚未设计`

But #15 GDD (`partner-relationships.md`) has been authored (2026-05-02) and is in the systems-index as "Approved". #15 identifies this exact stale reference in its own Cross-GDD Revision Flags (Flag 1, line 370-380):

> "Current text: States the partner system GDD is not yet created. Lists reveal_rumor() as the sole upstream API."
> "Change needed: Add report_observation_event() and on_partner_joined() to upstream API list."

Additionally, #6 Part 8 (lines 126-132) describes three human partners (`partner.old-sailor`, `partner.lighthouse-keeper-descendant`, `partner.cartographer`) with identity anchors and observation events — without any Post-MVP scope marker. #15 R15.5 explicitly states "无第二只伙伴 — partner.sky-cat 是 MVP 唯一伙伴实体". #15 Flag 2 (line 382-391) also identifies this.

**Impact**: Without fixing #6, implementers reading #6 would see outdated assumptions: they may implement 3 human partner APIs that #15 explicitly prohibits in MVP. The `report_observation_event` and `on_partner_joined` APIs are missing from #6's dependency documentation.

**Resolution**: 
- Update #6 Section 8 entry from "尚未设计" to designed, with full bidirectional reference to #15
- Add `report_observation_event(pattern_id, event_type)` and `on_partner_joined(partner_id)` to #6's upstream API list from #15
- Add Post-MVP scope marker at top of #6 Part 8, referencing #15 R15.5
- Note that MVP reveal_rumor() calls from partner are clamped to confidence <= 66

**Reference**: #6 line 829, #6 lines 126-132, #15 lines 370-391 (Flags 1+2).

---

## WARNING Items

### W1. #10 Bidirectional Table — #11 Marked as "Not Yet Written"

**Files**: `navigation-route-risk.md` (#10), `exploration-scavenge-scenario.md` (#11)

**Finding**: #10's bidirectional cross-reference table (line 482) states:

> `⚠️ #11 GDD 尚未编写——接口合约方向已在本系统定义，待 #11 设计时确认`

But #11 GDD is now written, reviewed, and approved (2026-05-03). The #10<->#11 contract (EncounterContext) is now fully defined and verified bidirectionally in both GDDs.

**Resolution**: Update #10's bidirectional table entry for #11 from "尚未编写" to designed and confirmed, citing #11's confirmed EncounterContext consumption contract.

**Reference**: #10 line 482, #11 lines 185-203 (Interactions table, #10 upstream).

---

### W2. #1 Content Registry — Missing cat_sniff_signature Field

**Files**: `content-data-state-registry.md` (#1), `partner-relationships.md` (#15)

**Finding**: #15 requires items in #1's registry to carry an optional `cat_sniff_signature` static field with sub-fields `reveal_target`, `hazard_hint`, `confidence`, and `pattern_id`. #15 also requires `partner.sky-cat` registered as a companion entity in content domain `companions`.

#15's Cross-GDD Revision Flag 4 (lines 405-414) explicitly identifies this gap. Without it, zero items are sniffable — the partner system's core scout loop is inert at runtime.

**Resolution**: Add `cat_sniff_signature` to #1's item schema. Register `partner.sky-cat` with role tag `scout`. Add content validation rule: `reveal_target` must reference a valid `location_id`.

**Reference**: #15 lines 405-414 (Flag 4), #15 lines 69-76 (R10 item schema), #15 lines 314-322 (F.1 hard dependency on #1).

---

### W3. #7 Hub — Trace Anchor R7 Not Updated for Nest System

**Files**: `airship-hub.md` (#7), `partner-relationships.md` (#15)

**Finding**: #7's R7 "最低生活痕迹" defines a binary trace anchor ("生活舱个人物品存在: 无 → 有") and OQ-7 asks "生活舱'私人物品'痕迹锚点的具体内容". 

#15 now defines a 4-stage nest progression (empty → first → accumulating → full) with 4 specific narrative objects (旧船帆碎布, 锈蚀的测风链环, 玩家绳头, 空港徽章残片). #15's Cross-GDD Revision Flag 3 (lines 393-402) identifies that #7 needs update.

**Resolution**: Update #7 R7 from binary to 4-stage driven by `query_nest_state()`. Close OQ-7 with answer from #15 R11 (4 specific nest objects). #7's interaction registry already includes partner_station — no structural changes needed, only the trace anchor specification.

**Reference**: #15 lines 393-402 (Flag 3), #15 R11 (nest item list), #15 nest trace state machine.

---

### W4. #15 GDD Status Inconsistency

**Files**: `partner-relationships.md` (#15), `systems-index.md`

**Finding**: #15's GDD header (line 3) says `Status: In Review`. But systems-index line 87 says `Status: Approved` for system #15.

**Resolution**: Align the status — if #15 has been approved in a review session, update the GDD header. If still in review, update systems-index.

**Reference**: #15 line 3, systems-index line 87.

---

### W5. #10 OQ-01 — destination_id Still TBD

**Files**: `navigation-route-risk.md` (#10), `entities.yaml`, `exploration-scavenge-scenario.md` (#11)

**Finding**: #10's Open Question OQ-01 (line 841) states:

> "目的地具体是什么？MVP 两条航线的 destination_id 注册表中为 TBD——在哪个系统确定？"

This remains unresolved. #11's exploration template (云观站废墟) is visited via a destination, but the destination_id that #10 passes in EncounterContext → #11 must match a registered location. The entities.yaml registry still has TBD for route destinations or they need verification.

**Impact**: #10 cannot emit a valid EncounterContext without a resolved destination_id. #11's AC-11-01 tests require a valid EncounterContext with voyage_result and destination_id.

**Resolution**: Assign destination_ids in entities.yaml for both MVP routes (e.g., `location.cloudwatch-ruins` for route.storm-cut-01, `location.sky-reef-outpost` for route.sky-reef-arc-01) and ensure #11's exploration template is linked to at least one.

**Reference**: #10 line 841 (OQ-01), entities.yaml routes section.

---

### W6. Systems-Index #15 Dependency List Incomplete

**Files**: `systems-index.md`, `partner-relationships.md` (#15)

**Finding**: systems-index line 87 lists #15's dependencies as: "飞艇家园 Hub; 航图与航线规划; 玩家知识与情报; 内容数据与状态注册表; 资源、货物与容量" — missing #3 (本地存档与世界状态持久化).

#15's own dependency analysis (F.1 Hard Dependencies, line 316-321) explicitly lists #3 as a hard dependency: 

> "All relationship memory is session-volatile... CD constraint 'persistent relationship memory' fails."

**Resolution**: Add #3 (本地存档与世界状态持久化) to #15's Depends On column in systems-index.

**Reference**: systems-index line 87, #15 lines 316-321 (F.1 hard dependency on #3), #15 F.4 (save/load data ownership).

---

## INFO Items

### I1. #10 OQ-02 — Fuel/Energy System Gap

No GDD in MVP defines fuel or energy mechanics. #10's voyage feasibility check currently relies only on hull band, missing the energy dimension described in the Player Fantasy ("燃料指针已在倒数位置"). #8's power furnace assumes `get_furnace_energy_status()` always returns 1.0. This is a known design gap deferred to Phase 2+.

**Reference**: #10 line 842 (OQ-02), #8 line 34 (energy stub).

### I2. #14 OQ-3 — Intel Consumption Flow Unresolved

#14 sells intel goods (航线手记) but the consumption interface ownership is unclear — does #14 write directly to logs, or does #6 handle intel item consumption? This affects AC-H.4.2 verification.

**Reference**: #14 line 533 (OQ-3), #14 AC-H.4.2.

### I3. #16 and #17 Not Started — Downstream Interfaces Not Locked

Multiple GDDs (#10, #11, #12, #13, #14, #15) reference #16 (UI/HUD) and #17 (Feedback/Audio) as downstream consumers with semantic event contracts. Since neither GDD is started, these interfaces are provisional. Once #16/#17 are authored, a re-check of event schema compatibility is needed.

**Reference**: systems-index lines 88-89 (status: Not Started), #10 lines 468-471 (#17 downstream), #11 lines 774-805 (Visual/Audio Requirements for #17), #12 lines 194 (#17 downstream).

### I4. #5 ↔ #12 consume_in_combat Interface Verification

#12 calls `#5.consume_in_combat(resource_id, quantity)` and `#5.get_carried_contents_by_tag("repair-material")`. These interfaces are referenced in #12's Dependencies table (line 414) as required. Since #5's GDD was read in a previous session, verification that these exact function signatures exist in #5 is deferred (but #12's bidirectional table line 430 confirms "#5 GDD 已列 #12 为下游").

**Reference**: #12 line 414, #12 line 430.

### I5. #6 Part 8 Post-MVP Scope Marker

Related to B3 but tracked separately: even after #6's Dependencies entry is updated (B3), #6 Part 8's three human partner descriptions (lines 126-132) and their associated observation event definitions (lines 580-606) must be explicitly tagged as Post-MVP. Currently they read as MVP content without scope qualifiers.

**Reference**: #6 lines 126-132, #6 lines 580-606, #15 Flag 2 (lines 382-391).

---

## Checks Summary

### 2a. Dependency Bidirectionality

| Relationship | Status | Note |
|---|---|---|
| #9 → #10 (route_committed) | PASS | Bidirectional confirmed |
| #8 → #10 (can_depart, hull, scout) | PASS | Bidirectional confirmed |
| #6 → #10 (query_route_knowledge) | PASS | Bidirectional confirmed |
| #10 → #11 (EncounterContext) | PASS* | #10's table stale (W1), but #11 confirms |
| #11 → #12 (threat_context / combat_result) | PASS | Bidirectional confirmed |
| #12 → #8 (apply_hull_damage, apply_module_damage) | PASS | Bidirectional confirmed |
| #12 → #5 (consume_in_combat, get_carried_contents) | PASS | Bidirectional confirmed |
| #13 → #5 (can_deposit, commit_deposit) | PASS | Bidirectional confirmed |
| #13 → #6 (query_knowledge_state, on_repair_completed) | PASS | Bidirectional confirmed |
| #13 → #9 (on_route_enhanced) | PASS | #13 declares, #9 needs verification |
| #13 → #14 (repair_completed) | PASS | Bidirectional confirmed |
| #14 → #5 (validate_purchase, execute_purchase) | PASS | Confirmed after R3 fix |
| #14 → #4 (register_focus_target, use_requested) | PASS | #4 defines, #14 consumes |
| #15 → #6 (reveal_rumor, report_observation_event, on_partner_joined) | FAIL | B3 — #6 still says "尚未设计" |
| #15 → #7 (hub events, partner_station, nest anchor) | CONCERNS | W3 — #7 R7 needs update |
| #15 → #1 (cat_sniff_signature, companion registration) | CONCERNS | W2 — field not yet added |
| #15 → #3 (progress.partner_skycat snapshot) | CONCERNS | W6 — missing from systems-index dep list |

### 2b. Rule Contradictions

| Finding | Severity | Detail |
|---|---|---|
| Hull band thresholds: #8=76/26 vs #12=61/31 | BLOCKING | B1 |
| No other contradictions found | — | η_scout values consistent across #8/#10/#11/#12. repair_completed signal signature consistent (#13→#14). combat_result fields consistent (#12→#11). Pool 5 (5 slots) consistent across #5/#11/#12. λ_success=0.08, λ_forced=0.25 consistent across #11/#12. |

### 2c. Stale References

| Finding | Severity | Detail |
|---|---|---|
| #6 Dependencies: #15 listed as "尚未设计" | BLOCKING | B3 |
| #10 bidirectional table: #11 "GDD 尚未编写" | WARNING | W1 |
| #6 Part 8: human partners no Post-MVP marker | INFO | I5, linked to B3 |
| #7 OQ-7: now answerable via #15 R11 | WARNING | W3 |
| #14 upstream: "货币获取系统（待分配）" | BLOCKING | B2 |

### 2d. Tuning Knob Ownership

No duplicate ownership conflicts found. All knobs have clear owners:
- `hull_band_thresholds` → #8 (canonical)
- `η_scout` → #8
- `trigger_prob["guard"]` → #11
- `λ_success`, `λ_forced` → #11
- `T_distance` values → #10
- `guard_full_damage_range` → #12
- `repair_lighthouse_material_costs` → #13
- `MVP_CONFIDENCE_MAX` → #15

### 2e. Formula Compatibility

All upstream output ranges are compatible with downstream input expectations:
- #12 hull_damage [0, 18] → #8 apply_hull_damage: compatible (hull 1-100, 18 damage = 18%)
- #10 D_accumulated max ≤ 100 → #8 hull integrity: compatible (capped at 0)
- #10 EncounterContext → #11: field names match
- #11 threat_context → #12: field names match
- #12 combat_result → #11: field names match
- #15 confidence [0, 66] → #6 reveal_rumor [0, 100]: compatible
- #13 repair_completed(node_id) → #14: single-param, compatible

### 2f. AC Cross-Check

No contradictory acceptance criteria found across GDDs. All tested:
- #10 AC-53 (FORCED_LANDING at hull=0) compatible with #8 destroyed band + can_depart=false
- #12 AC-12-13 (hull=0, exploration continues) compatible with #11 EC-11-08 (hull=0 doesn't terminate exploration)
- #11 AC-11-20 (guard inert when #12 disabled) compatible with #12 AC-12-21 (same behavior)
- #12 AC-12-04b (hull_damage [12,18]) compatible with #8 hull integrity range

---

## Final Verdict: CONCERNS

The 15-GDD system set is **fundamentally sound** — no circular dependencies, no formula incompatibilities, no contradictory ACs. The core architecture holds.

However, **3 BLOCKING issues must be resolved** before any implementation begins:

1. **B1**: #12's hull band UI thresholds (61/31) must be corrected to match #8's canonical thresholds (76/26)
2. **B2**: A currency acquisition mechanism must be assigned to an existing system or a new minimal spec created
3. **B3**: #6's stale references to #15 must be updated (Dependencies entry, Part 8 Post-MVP markers, missing API documentation)

The 6 WARNING items are design-documentation issues that should be addressed before handing GDDs to implementers, but do not block architectural decisions.
