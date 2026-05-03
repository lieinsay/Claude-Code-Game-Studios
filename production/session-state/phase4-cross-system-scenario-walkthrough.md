# Phase 4: Cross-System Scenario Walkthrough

**Project**: 《云海织航》(Cloud Sea Navigation)
**Date**: 2026-05-04
**Reviewer**: Creative Director (Claude Opus agent)
**Scope**: 5 multi-system scenarios across 12 systems
**Method**: Step-by-step player-perspective walkthrough checking activation order, data flow, player experience, and 7 failure modes

---

## Scenarios Walked

| # | Scenario | Systems | Risk Profile |
|---|----------|---------|--------------|
| S1 | 探索→守卫遭遇→硬扛→资源消耗→船体变化 | #11 #12 #5 #8 #16 | High — core tension loop |
| S2 | 修复完成→航线解锁→能力解锁→村镇反应→视觉反馈 | #13 #9 #6 #14 #17 #3 | High — 6-system cascade |
| S3 | 船体重伤+守卫遭遇+模块损坏叠态 | #8 #11 #12 #7 | High — boundary stacking |
| S4 | 分批提交→存档→读档→继续修复 | #13 #5 #3 | Medium — persistence integrity |
| S5 | 情报获取→航图更新→路线重规划→威胁规避 | #6 #9 #10 | Low — read-only decision flow |

---

## BLOCKERS

### 🔴 S1-BLOCKER-1: C4 Settlement Sequence Stale Values — C1 Fix Incomplete

**Systems**: #12 Combat & Threat Handling
**Step**: C4 结算序列步骤 3-4 (combat-threat-handling.md lines 82, 85)

The C1 fix (Tank rebalance: 12-18→8-12, 50%→30%) updated the C3 Response Options table and all downstream Acceptance Criteria, but **missed two values in the C4 settlement pseudocode**:

```
Line 82: → 硬扛: random_int(12, 18)     // Should be: random_int(8, 12)
Line 85: → if random() < 0.5 AND ...     // Should be: random() < 0.30
```

**Impact**: If a programmer implements from C4 rather than C3, Tank will deal the old (higher) damage and higher module risk. This is a documentation-internal contradiction within the same GDD.

**Resolution**: Edit lines 82 and 85 in `combat-threat-handling.md` to match C3/C8 values.

---

## WARNINGS

### ⚠️ S1-WARNING-1: Hull Destroyed During Exploration — Stranding Recovery Path Undocumented

**Systems**: #12 Combat, #8 Hull State, #11 Exploration
**Step**: After Tank damage pushes hull integrity to 0 during exploration

If hull reaches 0 (destroyed band) during exploration combat, `can_depart()` returns false — the airship cannot fly. The player is stranded at the exploration site. Recovery requires self-repair at Hub Station 10 (consuming repair materials from storage), but **no GDD documents this stranding scenario or the self-repair recovery path**. Edge Cases in both #8 and #12 omit the "exploration mid-session hull destruction" case.

**Recommendation**: Add edge case to #8 or #12 documenting: "If hull reaches 0 during exploration, player can still extract to airship and perform emergency repair at Station 10 using materials from storage."

---

### ⚠️ S2-WARNING-1: Ability Unlock Failure Not Propagated Back to #13

**Systems**: #13 World Repair, #6 Intel
**Step**: `repair_completed` signal → #6 `on_repair_completed` → Path C ability unlock

If `ability.lighthouse-signal-interpretation` is already unlocked via another path (Path A or B), or if #6 encounters an internal state conflict during the unlock attempt, **#13 does not receive any error or confirmation**. The repair is marked `repaired` (终态, irreversible) regardless of whether all downstream effects succeeded. This is a one-way fire-and-forget notification with no acknowledgment protocol.

**Recommendation**: Either (a) #6 should define `on_repair_completed` as idempotent (safe to call multiple times), or (b) add a lightweight acknowledgment so #13 can log/report partial downstream failures without blocking the repair itself.

---

### ⚠️ S2-WARNING-2: Quadruple Reward Stacking at Repair Completion

**Systems**: #13 → #9, #6, #14, #17
**Step**: `known → repaired` transition

A single repair completion simultaneously delivers:
1. Route unlock + hazard reduction (#9)
2. Ability unlock (#6)
3. NPC activity/dialogue changes (#14)
4. Visual spectacle — lighthouse relight (#17)

This is a deliberate "big moment" design (the repair fantasy explicitly calls for it). However, **4 simultaneous rewards from one action runs the risk of cognitive overload and reward dilution** — players may not register all 4 effects, diminishing the perceived value of future repairs. The player fantasy section says "看见它照亮的航线重新出现在航图上" which focuses on ONE moment (seeing the route light up). The other 3 rewards compete for attention at the same instant.

**Recommendation**: Consider staging the rewards — immediate visual (#17), route update on next chart open (#9), ability unlock after first use of the route (#6), NPC changes on next port visit (#14). This spreads the dopamine across multiple sessions. Alternatively, document this as a deliberate "overwhelming positive moment" with the understanding that later repair nodes may have fewer stacked rewards.

---

### ⚠️ S2-WARNING-3: visual_state_anchor Interface Between #13 and #17 Undefined

**Systems**: #13 World Repair, #17 Feedback
**Step**: `visual_state_anchor = repaired`

#13 writes `visual_state_anchor = repaired` and states this is "供 #17 消费的视觉锚点". However:
- #17 is marked as "Vertical Slice" — not complete in MVP
- The exact interface (signal? property set? direct write?) is not defined in either GDD
- #13's Visual/Audio Requirements section says "以上 MVP 规格直接写入本系统" because #17 may not be ready

This creates an ownership ambiguity: who actually owns the lighthouse visual state during MVP? If #17 isn't done, #13 provides the fallback specs — but this fallback path isn't formalized as an interface.

**Recommendation**: Add a brief note in #13's Dependencies section clarifying: "If #17 is unavailable at MVP launch, #13 owns the minimal visual implementation defined in §Visual/Audio Requirements. When #17 is ready, it replaces this implementation by consuming `visual_state_anchor`."

---

### ⚠️ S3-WARNING-1: Module Damage → Scout Degradation → More Threats Feedback Loop

**Systems**: #8 Hull State, #12 Combat, #11 Exploration
**Step**: Scout module damaged → η_scout drops → threat preview shrinks → more surprise encounters → more module damage

The chain is: module damaged → η_scout drops from 1.0 to 0.6 (or 0.48 with critical band) → threat preview degrades from "type+position" to "threat exists" → player walks into more threats blind → more module/hull damage. **This is a positive feedback loop** but constrained in MVP by:
- Fixed number of threat points in the exploration template (2+ environmental)
- Tank no longer damages modules on every hit (30% chance)
- Player can always Retreat (0 damage)

**Risk in MVP**: Low. **Risk in expansion**: If more threats are added or module damage chance increases, this loop could spiral.

**Recommendation**: Document the feedback loop explicitly in #8's tuning knobs so future designers know the constraint. No MVP changes needed.

---

### ⚠️ S3-WARNING-2: Cargo Bay Over-capacity When Cargo Module Damaged Mid-Voyage

**Systems**: #8 Hull State, #5 Resources, #7 Hub
**Step**: Cargo module damaged → capacity drops from +500 to +250 → loaded cargo may exceed new capacity

If the player has loaded cargo up to the maximum (e.g., 480/500 volume with one cargo module), and the cargo module gets damaged during combat (capacity drops to 250), **the loaded volume (480) exceeds the new capacity (250)**. What happens to the excess cargo?

- #8 defines the efficiency drop but doesn't address over-capacity
- #5 defines capacity checks for `add()` operations but doesn't define behavior when capacity decreases under existing load
- #7 Hub owns the physical slot but doesn't define cargo ejection

**Recommendation**: Add an edge case to #8 or #5: "When module damage reduces cargo capacity below current loaded volume, excess cargo items are force-transferred to airship storage (Pool 2). If storage is full, they are placed in a temporary overflow state accessible only at port. No cargo is destroyed."

---

### ⚠️ S3-WARNING-3: Silent No-Op When All Modules Already Damaged

**Systems**: #12 Combat, #8 Hull State
**Step**: Tank rolls module damage (30%) but both installed modules are already `damaged`

#8 `apply_module_damage()` is documented as a no-op when the target slot is already damaged ("在已受损模块上返回无错误——不造成二次损坏"). However, **#12's C4 settlement does not filter out already-damaged slots before rolling**, and the player receives no feedback that "the impact hit an already-damaged module — no new damage occurred." From the player's perspective, a 30% roll happened but nothing changed — was it a miss? A bug?

**Recommendation**: #12 should filter eligible slots before rolling. If no undamaged slots exist, skip the module damage roll entirely and optionally show a brief message: "模块均已受损，本次撞击未造成新损坏。"

---

### ⚠️ S4-WARNING-1: Cross-Snapshot Consistency Between deposited Counter and Pool 6

**Systems**: #13 World Repair, #5 Resources, #3 Persistence
**Step**: Save/Load during partial deposit

When the player saves mid-repair (some materials deposited, counters updated), two separate snapshot packages store related state:
- `progress.world-repair` (#3): `deposited` counters, `repair_state`
- `resources` (#5): Pool 6 actual material quantities

On load, if these two snapshots are inconsistent (e.g., #3 loaded an older `progress.world-repair` but #5 loaded a newer `resources`), the deposited counter could show 3 while Pool 6 contains 0 — making repair completion impossible. **AC-15 in #13 validates this scenario** at the acceptance level, but #3's snapshot architecture doesn't explicitly define atomic cross-package consistency.

**Recommendation**: #3 should document that `progress.world-repair` and `resources` snapshots are always saved/loaded in the same atomic transaction. Alternatively, #13 could rebuild `deposited` counters from Pool 6 on load rather than trusting the saved counter.

---

## INFO

### ℹ️ S2-INFO-1: Signal Handler Ordering in 5-Way Notification

**Systems**: #13 → #9, #6, #14, #17, #3
**Step**: `repair_completed` signal emission

The 5 downstream systems react to `repair_completed` in an undefined order (Godot signal connection order). Currently no system's handler depends on another handler's output, so ordering is irrelevant. If a future system's handler depends on another handler having completed first (e.g., #9 route enhancement must complete before #6 ability check), Godot's signal ordering guarantee would need to be relied upon or an explicit sequencing mechanism added.

---

### ℹ️ S3-INFO-1: Dual "Severe" Warning Thresholds

**Systems**: #8 Hull State, #12 Combat, #16 UI
**Step**: Hull at 20 in critical band but above warning threshold

- `critical` hull band: integrity ≤ 25, red label, "-25% speed, +30% fuel, module ×0.8"
- Tank "severe damage" warning: hull ≤ 12, "⚠ 船体严重受损"
- Cross-band preview: hull ≤ 33, "硬扛可能造成船体结构性恶化"

At hull=20, the player sees a red "critical" label AND a cross-band preview warning, but NOT the "severe damage" warning. Three different "severity" indicators with different thresholds may confuse players about their actual risk level. The gap between critical band entry (25) and severe warning (12) is 13 points — almost the full range of a Tank hit.

**Recommendation**: Consider unifying the thresholds or adding a tooltip explaining the difference between "structural state" (band) and "immediate danger" (warning).

---

### ℹ️ S5-INFO-1: Intel Reveal Not Immediately Visible on Chart

**Systems**: #6 Intel, #9 Chart
**Step**: Player consumes intel item → knowledge state updates → chart still shows old state if already open

#9 queries `query_route_knowledge()` at chart-open time (rule 3). If the player consumes intel, closes the research screen, and returns to the chart screen that was left open, the chart shows stale data. The fix is simple (re-query on focus) but not specified in either GDD.

---

### ℹ️ S5-INFO-2: In-Exploration Intel Not Visible Until Return

**Systems**: #11 Exploration, #6 Intel, #9 Chart
**Step**: Player discovers intel during exploration → deposited to #6 on extraction → chart updated after return

This is by design (intel doesn't affect the current voyage, only future planning), but it means the player cannot immediately leverage intel gathered "in the field." This is acceptable for MVP but worth noting as a UX consideration for Vertical Slice.

---

## Summary

| Severity | Count | IDs |
|----------|-------|-----|
| BLOCKER | 1 | S1-BLOCKER-1 (C4 stale values) |
| WARNING | 8 | S1-W-1, S2-W-1/2/3, S3-W-1/2/3, S4-W-1 |
| INFO | 4 | S2-I-1, S3-I-1, S5-I-1/2 |

### Key Finding

The **C1 fix (Tank rebalance) was incomplete** — the C4 settlement pseudocode still has the old values. This is a direct Phase 4 discovery: Phase 2/3 static analysis wouldn't catch it because it requires reading the C4 section as "code to be executed" rather than "description of behavior."

The **hull destruction during exploration** scenario (S1-W-1, S3-W-2) is the most under-specified area across multiple GDDs — what happens when things go wrong at the worst possible moment is only partially described.

### GDDs Flagged for Revision

| GDD | Reason | Severity |
|-----|--------|----------|
| `combat-threat-handling.md` | C4 settlement values stale (12-18→8-12, 0.5→0.30) | BLOCKER |
| `airship-modules-hull-state.md` | Cargo over-capacity on module damage, hull=0 during exploration | WARNING |
| `world-repair-unlock.md` | visual_state_anchor interface ambiguity with #17 | WARNING |
| `local-save-world-state-persistence.md` | Cross-snapshot atomicity not explicitly defined | WARNING |
