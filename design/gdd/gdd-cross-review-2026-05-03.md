# GDD Cross-Review Report — 2026-05-03

> **Skill**: `/review-all-gdds` (full)
> **Scope**: 18 documents — game concept, systems index, entity registry, 15 MVP system GDDs (#1–#15)
> **Review Model**: Opus-tier cross-system synthesis
> **Phases Executed**: Phase 1 (Full Document Load) → Phase 2 (Cross-GDD Consistency) → Phase 3 (Game Design Holism) → Phase 4 (Cross-System Scenario Walkthrough)

---

## Final Verdict: **CONCERNS** — 3 Blockers, 11 Warnings, 9 Info Notes

The 15-GDD system set is **fundamentally sound**. No circular dependencies, no contradictory acceptance criteria, consistent player identity across all systems. The core loop (Hub → Chart → Navigation → Exploration → Return → Repair → Market) is complete and well-specified.

However, **3 BLOCKING issues must be resolved** before implementation begins: a hard data contradiction in hull band thresholds between #8 and #12, an unassigned currency acquisition mechanism, and stale cross-references in #6 to the now-designed #15 partner system.

| Severity | Count | Must Fix Before Code? |
|----------|-------|----------------------|
| BLOCKING | 3 | Yes |
| WARNING | 11 | Yes (design docs + monitoring) |
| INFO | 9 | No (track for later) |

---

## Phase 1: Document Load Summary

Loaded 15 system GDDs covering 15 MVP systems. Pillars: 规划先于冒险, 世界会回应照料, 飞艇是家, 未知带来温和压力, 少量深关系. Anti-pillars: No harsh PvP, no hard time pressure, no mass collection, no pure errand trade, no number-only growth.

Entity registry: 7 entities, 4 items, ~19 formulas, ~17 constants. All 15 GDDs have complete 8-section structure (Overview → Acceptance Criteria).

---

## Phase 2: Cross-GDD Consistency

### Verdict: CONCERNS — 3 Blockers, 6 Warnings, 5 Info

### BLOCKING Issues

#### B1. Hull Band Threshold Contradiction (#8 vs #12) 🔴

**Files**: `airship-modules-hull-state.md` (#8), `combat-threat-handling.md` (#12)

**Finding**: #8 defines hull integrity bands at **76 / 26 / 1**:
```
intact: 76–100 | damaged: 26–75 | critical: 1–25 | destroyed: 0
```
This is the canonical definition, verified by #10's acceptance criteria (AC-57/58/63).

#12 defines **different thresholds at 61 / 31 / 1** in its visual/UI specifications:
- V-03 (line 472): `Green (intact, 100-61) / Yellow (damaged, 60-31) / Orange (critical, 30-1)`
- UI-03 (line 529): `band boundaries: 100/60/30/0`
- AC-12-14 (line 662): cross-band warning at hull=42 (12 damage crosses into critical at 30)

**Impact**: #12's decision panel displays wrong band colors and cross-band warnings. At hull=70 (intact per #8, damaged per #12), bar shows yellow instead of green. At hull=35 (damaged per #8, critical per #12), bar shows orange instead of yellow.

**Resolution**: Update #12's visual/UI specs to match #8's canonical 76/26/1 thresholds. Recalculate: V-03 color bands, UI-03 segment boundaries, UI-10 cross-band warning threshold (from hull=26+12=38), AC-12-14 boundary values.

#### B2. Currency Acquisition System Unassigned 🔴

**Files**: `port-village-market.md` (#14)

**Finding**: #14's purchase flow requires `total_cost <= player_currency`. No MVP system (#1–#15) defines how the player earns currency. #14's own dependencies table lists "货币获取系统（待分配）" as required.

**Impact**: Players arrive at market with 0 currency, making all 6 purchasable goods inaccessible. The purchase mechanic cannot function.

**Resolution options**:
- (a) Assign currency acquisition to #11 (exploration grants currency on extraction)
- (b) Create a minimal currency spec via quick-spec
- (c) Track currency as a #5 resource earned from selling items to stalls

#### B3. #6 GDD — Stale References to #15 Partner System 🔴

**Files**: `player-knowledge-intel.md` (#6), `partner-relationships.md` (#15)

**Finding**: #6's Dependencies section still reads `#10 伙伴功能与关系 — 尚未设计`. But #15 has been authored (2026-05-02) and approved. #6 Part 8 describes three human partners without Post-MVP markers — #15 R15.5 states `partner.sky-cat` is the sole MVP partner. #15's own Cross-GDD Revision Flags (Flags 1+2) identify both issues.

**Impact**: Implementers reading #6 would see outdated assumptions and may implement 3 human partner APIs that #15 explicitly prohibits in MVP. Missing `report_observation_event` and `on_partner_joined` from #6's API documentation.

**Resolution**:
- Update #6 Section 8 from "尚未设计" to designed with full bidirectional reference
- Add `report_observation_event()` and `on_partner_joined()` to #6's upstream API list
- Add Post-MVP scope marker at top of #6 Part 8
- Note MVP confidence clamping (≤66) for partner-originated rumors

---

### WARNING Items (Phase 2)

| # | Issue | Files | Resolution |
|---|-------|-------|------------|
| W1 | #10 bidirectional table: #11 marked "GDD 尚未编写" | `navigation-route-risk.md` #10 | Update to confirm #11 is designed; EncounterContext contract verified |
| W2 | #1 registry missing `cat_sniff_signature` item schema field | `content-data-state-registry.md` #1 | Add schema field per #15 Flag 4; register `partner.sky-cat` companion ID |
| W3 | #7 trace anchor R7 not updated for 4-stage nest | `airship-hub.md` #7 | Update from binary to 4-stage per #15 R11; close OQ-7 |
| W4 | #15 GDD header says "In Review" vs systems-index says "Approved" | `partner-relationships.md` #15 | Align status across documents |
| W5 | #10 OQ-01 destination_id still TBD | `navigation-route-risk.md` #10 | Assign destination_ids in entities.yaml for both MVP routes |
| W6 | systems-index #15 missing #3 (persistence) in Depends On | `systems-index.md` | Add #3 per #15 F.1 hard dependency analysis |

### Core Architecture Verification (Phase 2)

**Dependency graph**: No circular dependencies. All 17 verified cross-system relationships are bidirectional. Foundation → Core → Feature → Presentation layering is respected.

**Formula compatibility**: All upstream output ranges compatible with downstream input expectations:
- #12 hull_damage [0, 18] → #8 hull integrity [0, 100]: compatible
- #15 confidence [0, 66] → #6 reveal_rumor [0, 100]: compatible
- All cross-system struct contracts (EncounterContext, threat_context, combat_result) match field-for-field

**Tuning knob ownership**: No conflicts. All knobs have clear single-owner attribution.

**AC cross-check**: No contradictory acceptance criteria found. Key cross-system ACs verified compatible (hull=0 during exploration, guard inert when #12 unavailable, forced landing flow).

---

## Phase 3: Game Design Holism

### Verdict: PASS — 0 Blockers, 5 Warnings, 4 Info

### 3a: Progression Loop Competition — PASS

The `repair_kit` is the primary contested resource, serving four sinks (world repair, hull repair, module repair, combat emergency). The starting deficit (4 provided, 5 needed for lighthouse) forces exactly one exploration loop before world repair can complete — an elegant single-integer economic driver.

No system "hoards" the core loop. Knowledge is permanent but acquisition requires risk. Multiple paths to ability unlocks (Path A/B/C/D OR logic).

### 3b: Player Attention Budget — PASS

Peak active systems during core loop moments:
- Hub (pre-departure): 3-4 (stations physically separated, visited sequentially)
- Chart selection: 2 (focused decision surface)
- Navigation: 1-2 (passive with punctuated decisions)
- Exploration: 3-4 (systems activate sequentially, not in parallel)
- Return/market: 2-3 (post-loop wind-down)

All within the ≤4 active systems threshold. Well-sequenced phase gates prevent cognitive overload.

### 3c: Dominant Strategy Detection — WARNING (1 item)

**W-3c-01**: Scout module acquired for free (NPC reward), cargo module pre-installed. Module acquisition cost asymmetry should be documented as MVP simplification. Currently acceptable for MVP scope.

Multiple valid play patterns supported: Balanced (scout+cargo), Transport captain (2×cargo), Explorer (2×scout). No strategy strictly dominates.

### 3d: Economic Loop Analysis — PASS (2 info notes)

Complete source-sink mapping for all resources. The economic loop is closed and self-sustaining. Key observations:
- **I-3d-01**: Currency earning method under-specified (linked to B2)
- **I-3d-02**: Navigation consumption from storage (not carry) simplifies departure but slightly weakens preparation fantasy

### 3e: Difficulty Curve Consistency — WARNING (2 items)

**W-3e-01**: Risky route (storm-cut-01) theoretical damage ceiling 120-180 vs 100 hull max. Worst-case RNG may be mathematically unsurvivable. Mitigated by scout preview + emergency handling + retreat option. Monitor in playtesting.

**W-3e-02**: Hull repair economy (5 integrity/repair_kit) vs combat damage (12-18/hit) creates 3-4:1 repair ratio. Sustainability depends on exploration yield rates (not yet defined). Risk of repair deficit spiral if yields are low.

Overall difficulty model is a "ratchet" — world gets easier as player invests in it (Pillar 2).

### 3f: Pillar Alignment — PASS

All 15 systems serve at least one pillar. Zero anti-pillar violations. Notable observations:
- **I-3f-01**: Pillar 4 (Mild Pressure) has disproportionate mechanical weight (6 systems) but this is appropriate — it pervades the exploration loop
- **I-3f-02**: Pillar 5 (Deep Relationships) carried entirely by #15 — fragile if cat delivery underperforms

### 3g: Player Fantasy Coherence — PASS

Consistent "careful captain who repairs the world" identity across all 15 systems. No conflicting fantasies. The tonal tension between "safe home" (#7 Hub) and "dangerous world" (#11/#12) is intentional — documented as the core emotional rhythm (收束—展开 / contract-expand).

---

## Phase 4: Cross-System Scenario Walkthrough

### Scenario 1: Full Departure Flow (Hub → Chart → Navigation)

**Systems**: #7, #9, #8, #5, #10, #4, #6

| Step | Action | Systems | Data Flow |
|------|--------|---------|-----------|
| 1 | Walk to chart station | #4, #7 | Movement + station focus |
| 2 | Chart UI opens, routes rendered | #9, #6 | #9 reads route knowledge from #6 |
| 3 | Player selects route | #9, #8, #5 | `route_selectability`: can_depart() + snapshot valid + route known |
| 4 | Departure confirmation (step 1 of 2) | #9 | Display route summary, risk preview |
| 5 | Confirmation (step 2 of 2) | #9, #8, #5 | Re-check can_depart() + snapshot freshness (EC-9-16 guard) |
| 6 | Departure lock engages | #7 | `landed → departure_locked`, timer starts (2.0s) |
| 7 | In transit | #7, #15 | `departure_locked → in_transit`, cat simplified |
| 8 | Voyage begins | #10 | VoyageContext construction, encounter checks begin |

**Issue S1-01 (INFO)**: After departure lock engages (step 6), there is no re-check of hull integrity during the lock animation. If hull drops during the 2.0s timer (e.g., from a simultaneous event), departure proceeds with the degraded hull. This is acceptable — once locked, the player has committed.

**Issue S1-02 (INFO)**: The two-step confirmation gap (EC-9-16) correctly re-validates at step 5. If `can_depart()` changes between steps 3 and 5, the second step catches it. The max gap duration is player-determined (no timeout on confirmation dialog).

---

### Scenario 2: Exploration → Combat → Extraction (Core Loop)

**Systems**: #10, #11, #12, #8, #5, #6

| Step | Action | Systems | Data Flow |
|------|--------|---------|-----------|
| 1 | Arrival at destination | #10 → #11 | EncounterContext {route_id, destination_id, voyage_result, resolved_encounters} |
| 2 | ARRIVING phase | #11 | Show arrival text (safe landing or forced landing) |
| 3 | EXPLORING phase | #11, #4 | Player moves, searches search points |
| 4 | Search yields items | #11, #5 | `search_yield()` → `add_loot()` → items in Pool 5 |
| 5 | Threat triggered | #11, #12 | `threat_trigger()` → `resolve_threat(threat_context)` |
| 6 | Decision breath | #12 | Panel: hull status + response options, exploration paused |
| 7 | Combat resolution | #12, #8, #5 | Tank: hull_damage→#8, module_damage→#8. Emergency: consume repair_kit→#5 |
| 8 | Combat result returned | #12 → #11 | `combat_result` {outcome, hull_damage, module_damage, knockback, retreat_flagged} |
| 9 | Exploration resumes | #11 | threatened → exploring |
| 10 | Extraction triggered | #11 | EXTRACTING phase, readbar 2.5s |
| 11 | Extraction complete | #11, #5, #6 | `extraction_loss_settlement()` → #5. Intel → #6 |

**Issue S2-01 (WARNING)**: Module efficiency degradation during same exploration session. If hull crosses from intact to damaged band during combat (step 7), η_scout changes from 1.0→0.95 (or 0.6 if damaged). The scout preview level is snapshotted at exploration entry (η at step 1), so the preview display doesn't update. But #8's actual effects (speed, fuel, module efficiency) are real-time. This creates a minor UI inconsistency: preview shows full info but module is actually degraded. Acceptable per #11 design (EC-11-13 acknowledges similar issue for threat previews).

**Issue S2-02 (INFO)**: `retreat_flagged` persists after subsequent threat suppression. Player retreats from threat A (retreat_flagged=true), then uses emergency handling on threat B (is_active=false), then extracts — λ_forced=0.25 still applies. #12 EC-12-05 documents this as intentional. Extraction summary should display "本次探索中曾选择撤退" to avoid player confusion.

---

### Scenario 3: Return → Market → Lighthouse Repair

**Systems**: #7, #5, #14, #13, #8, #9, #6

| Step | Action | Systems | Data Flow |
|------|--------|---------|-----------|
| 1 | Return to hub | #7, #5 | in_transit → landed. Cargo unpacked to storage |
| 2 | Walk to market stalls | #4, #14 | Focus targets registered for open stalls |
| 3 | Open stall UI | #14, #5 | `get_storage_summary()` for currency + capacity |
| 4 | Purchase goods | #14, #5 | `validate_purchase()` → `execute_purchase()`. Currency deducted |
| 5 | Travel to lighthouse | #4, #7, #10 | Short flight to outskirts location |
| 6 | Interact with repair node | #13, #5 | `can_deposit()` check. Show material requirements |
| 7 | Commit materials (batch) | #13, #5 | `commit_deposit()` → Pool 6 terminal. deposited counter updates |
| 8 | Repair completion | #13 | `repair_completion()` → true. known → repaired |
| 9 | Downstream cascade | #13→#9, #13→#6, #13→#14 | Route unlock + ability check + stall unlock |
| 10 | Return to market | #14 | New stall open (closed → open_basic). NPC appears |

**Issue S3-01 (BLOCKER — linked to B2)**: Step 3-4 requires player currency. No system defines currency acquisition. The purchase loop cannot complete. Must be resolved per B2 resolution options.

**Issue S3-02 (INFO)**: Repair batch submission (#13) requires physical presence at lighthouse location. Market is at hub/settlement. This creates a mini-travel loop: buy supplies → fly to outskirts → repair → return to market → see new stalls. This is intentional and supports the exploration rhythm.

**Issue S3-03 (WARNING)**: If player uses repair_kit for combat emergency during scenario 2, it's no longer available for repair in this scenario. The starting 4 repair_kits + 1 deficit = the player must explore at least once AND choose not to use repair_kit in combat. If they use multiple repair_kits in combat, they may need multiple exploration runs before repair is possible. This is the intended economic tension but may feel punishing if not communicated clearly.

---

### Scenario 4: Partner Sniff → Chart Intel

**Systems**: #7, #5, #15, #6, #9, #1

| Step | Action | Systems | Data Flow |
|------|--------|---------|-----------|
| 1 | Return to hub | #7 | landed. cat at sleeping_on_intel_station or idle_living_quarters |
| 2 | Walk to partner station | #4, #7, #15 | Cat state may transition (zone-based). Enter living quarters → idle_living_quarters |
| 3 | Open sniff panel | #15, #5, #1 | Filter inventory items with `cat_sniff_signature != null` |
| 4 | Select item, confirm sniff | #15, #1 | Read `cat_sniff_signature` from #1 registry |
| 5 | Clamp confidence | #15 | `confidence_final = min(confidence, 66)` |
| 6 | Write knowledge | #15 → #6 | `reveal_rumor(location_id, "partner.sky-cat", [hazard_hint], confidence_final)` |
| 7 | Record observation | #15 → #6 | `report_observation_event(pattern_id, "partner_sniff_success")` |
| 8 | Update local state | #15 | Add to `sniffed_items`. If first success: nest_token=true, nest empty→first |
| 9 | Play reaction | #15 | Animation based on confidence_final + R7 mapping |
| 10 | Naming prompt (if eligible) | #15, #7 | `naming_prompt_eligibility` → UI modal. Blocks departure (E.1.g) |
| 11 | Check chart | #9, #6 | Rumor appears on chart as new location marker |

**Issue S4-01 (INFO)**: Naming modal blocks departure (step 10). This fires on return-to-hub after first successful sniff. If player attempts back-to-back voyages, the naming prompt interrupts the flow. This happens exactly once per save — acceptable.

**Issue S4-02 (WARNING)**: If #6 `reveal_rumor()` fails during step 6, the cat still plays the reaction animation and local state advances (E.5.a). The player sees the cat react but no new rumor appears on the chart. The game cannot distinguish this from "cat reaction was too weak to register." This is intentional per the design but may confuse players during playtesting.

---

### Scenario 5: Risky Route Damage Spiral

**Systems**: #10, #8, #11, #12, #5

| Step | Action | Systems | Data Flow |
|------|--------|---------|-----------|
| 1 | Select risky route | #9 | storm-cut-01: 10 encounter checks, high hazard |
| 2 | Voyage begins | #10 | VoyageContext with hull=62 (intact) |
| 3 | Encounter 1: guard threat | #10→#12 | Tank: hull 62→47 (damaged band). Speed -10%, fuel +15% |
| 4 | Encounter 2: environmental | #10 | Damage 5: hull 47→42 |
| 5 | Encounter 3: guard threat | #10→#12 | Tank: hull 42→24 (critical band!). Speed -25%, fuel +30%, η×0.8 |
| 6 | Encounter 4: guard threat | #10→#12 | Hull=24, below tank warning (18). Player retreats. retreat_flagged=true |
| 7 | Encounters 5-10 | #10 | Player retreats from or avoids all remaining threats |
| 8 | Arrival: FORCED_LANDING | #10→#11 | Hull in critical → forced landing. Crash point entry |
| 9 | Exploration begins | #11 | Damaged ship, limited capacity, higher risk |
| 10 | Extraction | #11, #5 | λ_forced=0.25 (retreat_flagged=true). Higher loss on carried goods |

**Issue S5-01 (WARNING — linked to W-3e-01)**: The risky route damage potential is real. In this scenario, 3 encounters pushed hull from 62 to 24. With 7 more encounters, the player MUST retreat from most. Damage ceiling of 120-180 means a fully unlucky run where player tanks everything is mathematically fatal. Mitigation:
- Scout preview lets player see threats early (emergency handling clears for 1 repair_kit)
- Retreat is always available (no damage, but extraction penalty)
- Not all checks trigger combat (some environmental, some pass)

**Issue S5-02 (WARNING)**: Cross-band module efficiency drop (step 5) changes η_scout mid-voyage. If scout was at 1.0 (PREVIEW_FULL), it drops to 0.8 (PREVIEW_PRESENCE). The player loses threat type information mid-flight. This is realistic but unexpected — the chart doesn't warn that damage affects information quality. An emergent gameplay moment that may feel like a bug.

**Issue S5-03 (INFO)**: The safe route (sky-reef-arc-01, 5 checks) serves as the recovery mechanism after a disastrous risky run. The player can run safe routes to gather repair materials, then attempt the risky route again. The "safe→risky→safe" rhythm is an intended gameplay pattern.

---

## Phase 5: Consolidated Findings

### BLOCKING (3) — Must Fix Before Implementation

| ID | Phase | Description | Files to Fix |
|----|-------|-------------|-------------|
| **B1** | 2 | Hull band thresholds: #8 uses 76/26, #12 uses 61/31. Visual/UI specs must match canonical #8 values | `combat-threat-handling.md` #12 |
| **B2** | 2 | Currency acquisition mechanism unassigned. #14 purchase requires currency but no MVP system provides it | `port-village-market.md` #14 (+ new quick-spec or #5/#11 extension) |
| **B3** | 2 | #6 stale references: #15 listed as "尚未设计", Part 8 human partners lack Post-MVP marker, missing API docs | `player-knowledge-intel.md` #6 |

### WARNING (11) — Address Before Handoff to Implementers

| ID | Phase | Description | Files |
|----|-------|-------------|-------|
| W1 | 2 | #10 bidirectional table: #11 marked "GDD 尚未编写" | `navigation-route-risk.md` #10 |
| W2 | 2 | #1 registry missing `cat_sniff_signature` item field and companion registration | `content-data-state-registry.md` #1 |
| W3 | 2 | #7 trace anchor R7 not updated for 4-stage nest | `airship-hub.md` #7 |
| W4 | 2 | #15 GDD status inconsistency ("In Review" vs systems-index "Approved") | `partner-relationships.md` #15 |
| W5 | 2 | #10 OQ-01: destination_id still TBD in entities registry | `navigation-route-risk.md` #10, `entities.yaml` |
| W6 | 2 | systems-index #15 missing #3 (persistence) dependency | `systems-index.md` |
| W-3c-01 | 3 | Scout module free acquisition asymmetry vs cargo pre-installed | #8 (documentation) |
| W-3e-01 | 3 | Risky route damage ceiling (120-180) exceeds hull max (100) | #10, #12 (monitor in playtesting) |
| W-3e-02 | 3 | Hull repair economy sustainability depends on undefined exploration yields | #11 (define minimum yield rates) |
| S2-01 | 4 | Module efficiency degradation during exploration: η snapshotted at entry vs real-time hull effects diverge | #8, #11 (known UI inconsistency, acceptable for MVP) |
| S4-02 | 4 | Partner sniff: cat reaction plays but rumor silently lost if #6 call fails | #15 (acceptable per E.5.a, may confuse playtesters) |

### INFO (9) — Track for Later

| ID | Description |
|----|-------------|
| I-3d-01 | Currency earning method under-specified (linked to B2) |
| I-3d-02 | Navigation consumption from storage (not carry) slightly weakens preparation fantasy |
| I-3f-01 | Pillar 4 has disproportionate mechanical weight (6 systems); Pillar 2 has fewest mechanics but strongest emotional payoff |
| I-3f-02 | Pillar 5 fragile — carried entirely by single system (#15) |
| I-3g-01 | Knowledge system fantasy richness depends on #17 presentation layer (Vertical Slice) |
| I1 (P2) | #10 OQ-02: fuel/energy system deferred to Phase 2+ |
| I2 (P2) | #14 OQ-3: intel consumption flow ownership unclear |
| I3 (P2) | #16/#17 not started — downstream event schemas provisional |
| S1-01 | Departure lock timer (2.0s): no hull re-check during animation |

---

## Phase 6: GDD Flagging

The following GDDs require revision based on this review:

### Must Revise (Blocking)

| GDD | Blockers | Sections to Update |
|-----|----------|--------------------|
| `combat-threat-handling.md` #12 | B1 | V-03, UI-03, UI-10, AC-12-14 — recalculate all hull band thresholds from 61/31 to match #8's 76/26 |
| `port-village-market.md` #14 | B2 | Add currency acquisition mechanism or link to source system; update Dependencies table |
| `player-knowledge-intel.md` #6 | B3 | Update Dependencies entry for #15; add `report_observation_event` + `on_partner_joined` to API list; add Post-MVP marker to Part 8 |

### Should Revise (Warning)

| GDD | Warnings | Sections to Update |
|-----|----------|--------------------|
| `navigation-route-risk.md` #10 | W1, W5 | Update bidirectional table for #11; resolve OQ-01 destination_id |
| `content-data-state-registry.md` #1 | W2 | Add `cat_sniff_signature` to item schema; register `partner.sky-cat` |
| `airship-hub.md` #7 | W3 | Update R7 trace anchor from binary to 4-stage; close OQ-7 |
| `partner-relationships.md` #15 | W4 | Align GDD header status with systems-index |
| `systems-index.md` | W6 | Add #3 to #15's Depends On column |

### No Revision Needed

GDDs #2, #3, #4, #5, #9, #11, #13 — all cross-references verified, no issues found.

---

## Strengths Identified

1. **The repair_kit deficit** (starting 4, lighthouse needs 5) is an elegant single-integer economic driver that forces the core loop without railroading
2. **Multi-path ability unlocking** (Path A/B/C/D OR logic) provides genuine player agency without dominant strategies
3. **The "ratchet" difficulty model** (world gets easier as player invests) perfectly serves Pillar 2 — every repair reduces future difficulty
4. **Consistent tonal identity** across all 15 systems — "careful captain who repairs the world" maintained without exception
5. **Thorough anti-pillar enforcement** — all 5 anti-pillars verified across all systems, zero violations
6. **Well-sequenced attention budget** — systems activate in phases (Hub stations sequential, exploration phases gated, navigation passive), peak 3-4 active systems
7. **The "contract-expand" emotional rhythm** — Hub safety vs exploration danger is intentional and consistently implemented

---

## Recommendations for Next Steps

1. **Immediate (before any code)**: Resolve B1 (hull band sync), B2 (currency source), B3 (#6 stale refs). Estimated: 1-2 GDD editing sessions.
2. **Before implementation sprint**: Resolve W1-W6. Estimated: 1 editing pass across 6 GDDs.
3. **During prototyping**: Test risky route (storm-cut-01) with worst-case RNG to determine if damage ceiling reduction is needed (W-3e-01). Define minimum exploration yield rates for repair_kit (W-3e-02).
4. **Before #15 implementation**: Add `cat_sniff_signature` to #1 registry and author sniffable items so the scout loop has content (W2).
5. **Playtest focus areas**: Pillar 5 delivery (cat emotional connection), Pillar 2 delivery (lighthouse visual feedback), currency economy feel, risky route survivability.
6. **Next GDDs**: System #16 (UI/HUD/航图界面) is the last unstarted MVP system. Its design should consume the downstream interface contracts already defined by #10-#15. Systems #17 (Feedback) and #18 (Onboarding) are Vertical Slice tier.
