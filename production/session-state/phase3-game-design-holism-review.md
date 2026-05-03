# Phase 3: Game Design Holism Review

**Project**: 《云海织航》(Cloud Sea Navigation)
**Date**: 2026-05-04
**Reviewer**: Creative Director (Claude Opus agent)
**Scope**: All 16 MVP GDDs + 3 foundation files (19 total)
**Method**: Cross-system interaction analysis across 7 categories

---

## 3a: Progression Loop Competition

### Systems Claiming "Progression" Role

| System | Progression Claim | Mechanism |
|--------|-------------------|-----------|
| #6 Player Knowledge & Intel | "Progression" (systems-index category) | Knowledge type advancement, ability unlocks |
| #13 World Repair & Unlock | "Progression" (systems-index category) | Permanent world node repair, irreversible unlocks |
| #7 Airship Hub | Implicit progression | Nest trace anchor R7 (4-stage visual), room existence gating |
| #15 Partner Relationships | Implicit progression | Naming ceremony, nest accumulation (4-item), 6-state runtime |
| #14 Port/Village Market | Implicit progression | Stall unlocking via repair, settlement activity tiers |

### Analysis

**Finding 3a-1: Dual Progression is Complementary, Not Competitive (INFO)**

World Repair (#13) and Player Knowledge (#6) occupy different progression niches:

- **World Repair**: Visible, external, permanent world-state change. Players see the lighthouse light up. This is the "proof of impact" progression.
- **Player Knowledge**: Internal, capability-unlocking. Players gain abilities (bird-flight-understanding, lighthouse-signal-interpretation, fog-navigation). This is the "I got better" progression.

These form a dual-axis progression system that is well-designed — they target different player motivations (impact vs mastery) and do not interfere with each other. The systems-index correctly lists both under "Progression" without conflict.

**Finding 3a-2: Hidden Dependency — Lighthouse Repair Blocks Knowledge Ability (WARNING)**

Path C in World Repair (#13): `repair_node.starlight_dock → unlock lighthouse-signal-interpretation ability`. This means one of the three MVP abilities in Knowledge (#6) is gated behind world repair. The Knowledge GDD (#6 Section 6.1) lists `lighthouse-signal-interpretation` as a defined MVP ability but does NOT reference the repair dependency. This is a broken bidirectional link: #6 should list #13 as a dependency for ability unlock gating.

**Finding 3a-3: Nest Progression is Fully Ornamental but Multi-System (INFO)**

The nest trace anchor R7 in Hub (#7) drives 4-stage visual progression based on `query_nest_state()` from Partner (#15). This involves:
- Partner (#15): nest accumulation logic, 4 items, irreversible
- Hub (#7): visual rendering of nest stage (R7 anchor)
- UI (#16, S3 Hub Screen): display of nest state

This is well-designed ornamental progression — it provides emotional payoff without mechanical complexity. However, the Partner GDD (#15, Section 5 Edge Cases) does not explicitly state what happens if the player never finds nest items (0/4 state). The Hub GDD states the nest is always visible regardless of stage, which resolves this, but the cross-reference is missing.

### Verdict: NO CRITICAL ISSUES. One broken bidirectional dependency link (3a-2).

---

## 3b: Cognitive Load Analysis

### Per-Phase Cognitive Load Assessment

| Game Phase | Active Systems | Concurrent Decisions | Load Level |
|------------|---------------|---------------------|------------|
| Hub (idle) | #4, #7, #8, #15 | Module config, departure mode, interaction | LOW — familiar space, no time pressure |
| Chart Planning | #6, #8, #9 | Route selection, module choice, departure confirm | MEDIUM — focused decision surface, 7 route_selectability branches |
| Navigation/Voyage | #10, #12 | Encounter response, retreat timing | MEDIUM — time-gated (12s checks), but single-threaded decisions |
| Exploration Scene | #4, #11, #12, #5 | Search point selection, item management, threat handling, extraction decision | HIGH — 4 concurrent concerns |
| Return/Market | #5, #13, #14 | Sell/buy, deposit repair materials, hull repair | LOW-MEDIUM — sequential operations, no pressure |

### Findings

**Finding 3b-1: Exploration Scene is the Cognitive Load Peak (WARNING)**

During the EXPLORING phase of #11, the player simultaneously manages:
1. **Position/reachability** (#4): movement within the 4-zone radial template (50x35 units)
2. **Inventory management** (#5): Pool 5 (carried) capacity tracking, item pickup decisions
3. **Threat awareness** (#11/#12): scout preview level (NONE/PRESENCE/FULL), potential guard sentinel encounters
4. **Search point strategy**: 6 search points, free-search rule (empty results don't consume attempts), decision on when to extract
5. **Extraction timing**: weighing λ_success=0.08 vs risk of continuing

This is 5 concurrent decision layers in a single scene — the highest in the entire game. The scout preview system (#10/#11) partially mitigates this by providing advance threat information, but the free-search rule creates an optimization puzzle: "Should I search everything since empty results are free?"

**Recommendation**: In Vertical Slice, consider adding a "search fatigue" mechanic (e.g., each search point attempt after the 4th reduces scout preview by 1 level) to create soft pressure to extract, reducing the optimization incentive.

**Finding 3b-2: Chart Planning Has Many Branches But Low Time Pressure (INFO)**

The route_selectability formula (#9) has 7 short-circuit branches, but all are evaluated before departure — the player has unlimited time to plan. The departure confirmation checklist (mandatory: hull check, module check; advisory: partner feedback) adds 2-3 decision points. This is well-designed: complexity is front-loaded into a no-pressure planning phase.

**Finding 3b-3: Combat Decision is a Clean Ternary Choice (INFO)**

The combat response (#12) presents exactly 3 options (Emergency Handling / Tank / Retreat) with clear consequences. This is excellent cognitive load management — the player faces a high-stakes moment but with a constrained, comprehensible decision space. The UI modal stack (#16, S11 Combat Override) correctly isolates this as a single-modal override.

**Finding 3b-4: Market Simplicity is Deliberate Load Reduction (POSITIVE)**

The market (#14) uses fixed prices, no supply/demand simulation, and 1 default open stall in MVP. This is explicitly scoped as "thin" and correctly avoids adding cognitive load to the return phase. The per-settlement activity tier (dormant/recovering/active) adds flavor without mechanics.

### Verdict: Exploration scene is the cognitive load bottleneck. Consider mitigation in Vertical Slice.

---

## 3c: Dominant Strategy Risks

### Finding 3c-1: Emergency Handling Dominates Tank — Tank May Be a Trap Option (CRITICAL)

**The Numbers:**

| Response | Cost | Hull Damage | Module Damage | Threat Result | Extraction Loss |
|----------|------|-------------|---------------|---------------|-----------------|
| Emergency Handling | 1 repair_kit | 0 | 0 | Permanently cleared | λ_success=0.08 |
| Tank | 0 repair_kit | 12-18 (uniform) | 50% chance | Permanently cleared | λ_success=0.08 |
| Retreat | 0 repair_kit | 0 | 0 | Not cleared, retreat_flagged=true | λ_forced=0.25 |

**Analysis:**

When repair_kit is available, Emergency Handling is strictly dominant: 1 repair_kit saves 12-18 hull damage (average 15) and 50% module damage risk.

When repair_kit is depleted, the Reteat vs Tank comparison is:
- Tank: 12-18 hull damage (avg 15), 50% module damage → normal extraction (λ=0.08)
- Retreat: 0 damage → forced extraction (λ=0.25)

Given hull_max=100 and 4 hull bands, 15 damage moves the player ~15% toward the next (worse) band. In damaged band (26-75), encounter checks are 10% more frequent. The expected additional damage from more frequent encounters must be weighed against the 17% extraction loss difference (0.25 - 0.08 = 0.17).

**Strategic implication**: With starting repair_kit=4, the player can Emergency Handle at most 4 times. After that, the dominant strategy becomes "always Retreat" — making Tank a trap option that is almost never the correct choice.

**Root cause**: The 3 options are priced on different axes (consumable vs hull vs extraction rate) without a common valuation framework. There is no situation where Tank is the best choice — it's either Emergency Handling (when repair_kit available) or Retreat (when not).

**Recommendation**: Consider one of:
- (A) Reduce Tank damage to 5-10 and remove module damage chance, making it the "safe but painful" option when out of repair_kit
- (B) Make Retreat cost 1 search point attempt (adding a non-damage cost), making Tank comparatively more attractive
- (C) Add a 4th option "Evade" with intermediate costs, creating a smoother gradient

### Finding 3c-2: Scout+Cargo is the Only Rational Module Configuration (WARNING)

MVP has 2 open slots (#8), each accepting scout or cargo module.

| Config | Scout η | Cargo Capacity | Threat Preview |
|--------|---------|---------------|----------------|
| scout+scout | 2× scout_rating | Low (base only) | N_preview = floor(η×2) → max 4 checks |
| scout+cargo | 1× scout_rating | Medium | N_preview = floor(η×2) → max 2 checks |
| cargo+cargo | 0 scout | High | N_preview = 0 checks |

With only 1 threat type in MVP (guard sentinel), the marginal value of a second scout module is low — you already know what you'll face. The marginal value of cargo capacity is high — more extracted items = more coins, more repair materials. Scout+cargo is the dominant configuration. Scout+scout is only rational for the first 1-2 voyages when the player doesn't yet know what guard sentinels are (Knowledge state: unknown).

**This is acceptable for MVP** because the configuration space is intentionally small. For Vertical Slice with multiple threat types and module varieties, the scout preview value increases. Not a critical issue for current scope.

### Finding 3c-3: Free-Search Creates Exploration Exhaustion Incentive (WARNING)

The free-search rule (#11): "Empty search point results do NOT consume search attempts." Since extraction loss (λ) applies to carried items, the optimal strategy is:
1. Search ALL 6 points (free attempts for empties)
2. Pick the best items (fill Pool 5 to capacity)
3. Extract

This reduces the "meaningful choices" in exploration to a single item-selection decision at the end, rather than moment-to-moment search-or-extract tension. The scout preview helps with threat awareness but doesn't create search pressure.

**Recommendation**: Consider capping free searches to 2-3 per exploration session, or applying a small hull integrity penalty for extended exploration (e.g., "fatigue" — each search point after the 4th checks hull at 5% degrade chance).

### Verdict: ONE CRITICAL (3c-1). Tank is a trap option. Two warnings on module and search strategy.

---

## 3d: Economic Loop Analysis

### The Core Economic Loop

```
Exploration (#11) → Items (Pool 5) → Market (#14) → cloud-coins → Supplies (Pool 2)
                                                          ↓
                    World Repair (#13) ← repair_kit + basic_supply
                          ↓
                    New routes, reduced hazards
                          ↓
                    Better exploration → More items
```

### Resource Flow Map

| Resource | Sources | Sinks | Renewable? | Starting Qty |
|----------|---------|-------|------------|--------------|
| repair_kit | Exploration search points ONLY | Module repair (#8), Hull repair (#8), Combat Emergency (#12), World repair (#13) | NO — finite per exploration point | 4 |
| cloud-coins | Exploration looting ONLY | Market purchases (#14) | YES — per exploration session | 0 (implied) |
| basic_supply | Market purchase, exploration looting | World repair (#13), personal use | YES — market restocks | 1 bundle (implied) |
| route_notes | Exploration looting | Knowledge advancement (#6) | YES — per exploration | 0 |

### Findings

**Finding 3d-1: repair_kit is the Lynchpin Resource — and It Has a Critical Supply Gap (CRITICAL)**

repair_kit is consumed by 4 systems:
- #8.2 Module repair: `repair_module()` formula — costs repair_kit
- #8.3 Hull repair: hull integrity restoration — costs repair_kit
- #12 Combat: Emergency Handling — costs 1 repair_kit per use
- #13 World Repair: `repair_node.starlight_dock` requires 5 repair_kit + 4 basic_supply

Starting quantity: 4 (entities.yaml). Lighthouse requirement: 5. This means:

> **The first world repair is mathematically impossible without finding at least 1 repair_kit during exploration.**

The player starts with 4. If they use ANY repair_kit before attempting the lighthouse (for module repair, hull repair, or combat), they need even more from exploration.

**Supply chain analysis:**
- repair_kit source: "Not purchasable at market; found only in exploration" (entities.yaml, #5 Section 3.1.3)
- Exploration has 6 search points per session
- Each search point has a loot table — repair_kit drop rate is not specified in any GDD
- If repair_kit drop rate is low (e.g., 10%), expected repair_kit per exploration session = 0.6, requiring ~2 sessions to find 1

**Compounding risk**: If the player uses Emergency Handling (1 repair_kit each), they deplete their starting stock and may NEVER accumulate enough for the lighthouse. This creates a potential dead-end state where the player has spent all repair_kits on combat survival and cannot progress the world repair.

**Recommendation**: 
- (A) Add 1-2 repair_kit as a guaranteed early exploration find (e.g., first search point at cloudwatch-ruins always contains 1 repair_kit)
- (B) Reduce lighthouse requirement to 3 repair_kit (allowing 1 to be used for emergencies)
- (C) Add an NPC-given repair_kit as a quest reward tied to first exploration return
- (D) Make A-Tu's general store (#14) sell 1 repair_kit per market visit at a high coin price

**Finding 3d-2: cloud-coins Has a Single Source — Exploration Success Dependency (WARNING)**

Currency flow:
- Source: Exploration looting (#11, search points) → sell at market (#14)
- Sink: Market purchases (supplies for next voyage, possibly repair materials)
- No alternative income: no quests, no trading, no passive income

If a player has a bad exploration run (forced retreat, λ_forced=0.25 extraction loss, few valuable items), they may return with insufficient coins to buy supplies for the next voyage. This creates a potential downward spiral:
```
Bad run → Few coins → Can't buy supplies → Harder next run → Worse results → Fewer coins...
```

**Mitigating factors in MVP:**
- Free-search rule means exploration always yields SOME items
- Fixed prices mean coin requirements are predictable
- Only 1 stall open — limited purchase options mean lower coin demand
- 2 routes available (one safe, one risky) — player can choose easier route after a bad run

**This is NOT critical for MVP** due to the mitigating factors, but the single-source currency model needs diversification in Vertical Slice.

**Finding 3d-3: The Market-Stall-to-Repair Feedback Loop is Well-Designed (POSITIVE)**

The unlocking chain (#14): `repair_node → stall opens → new items available → more resources for next repair`. This creates a satisfying "I fixed this, and now the world offers me more" loop. The settlement activity tier (dormant → recovering → active) based on stall count reinforces this visually.

**Finding 3d-4: repair_kit Cannot Be Purchased — Design Intent Check (INFO)**

The entities.yaml explicitly states repair_kit is "Not purchasable at market." This is a deliberate design choice to make repair_kit a "found-only" resource, forcing exploration engagement. The intention is sound — it prevents players from farming coins and buying their way through world repair. However, combined with 3d-1, the supply constraint may be too tight for MVP.

### Verdict: ONE CRITICAL (3d-1). repair_kit supply gap makes first world repair potentially impossible. Currency single-source is a warning for Vertical Slice.

---

## 3e: Difficulty Curve Compatibility

### Current State: No Scaling Mechanism in MVP

Every quantitative value across all 16 systems is fixed:

| System | Fixed Values |
|--------|--------------|
| #10 Navigation | T_base=12s (encounter interval), damage tags fixed, hazard intensity fixed |
| #12 Combat | Tank damage 12-18 (uniform), no enemy scaling |
| #8 Modules | furnace_rating: scout=8, cargo=12 (fixed), M_max formula uses fixed ratings |
| #13 Repair | Fixed material requirements per node |
| #14 Market | Fixed prices, no supply/demand |
| #11 Exploration | Fixed number of search points (6), fixed zone template size |

### Findings

**Finding 3e-1: Zero Difficulty Curve in MVP (WARNING — Acceptable for MVP Scope)**

There is no mechanism that increases challenge as the player progresses. The first voyage and the tenth voyage face identical encounter timing, identical combat damage, and identical costs. This is appropriate for MVP scope (which explicitly aims to prove the core loop, not deliver a full difficulty arc), but it is a significant gap for Vertical Slice planning.

**What a difficulty curve should address in Vertical Slice:**
- As players unlock more routes via world repair, those routes should present harder challenges
- As players accumulate knowledge, encounters should evolve (not just more damage, but new patterns)
- Hull scars (currently narrative-only) could introduce mechanical effects at higher counts

**Finding 3e-2: Hull Integrity is a Snowball Mechanic, Not a Difficulty Curve (WARNING)**

The hull integrity system (#8) creates a negative feedback loop:
```
Lower hull → Faster encounter checks (-10% damaged, -20% critical) → More damage → Lower hull → ...
```

This is a death spiral, not a difficulty curve. It punishes struggling players more, rather than challenging skilled players. The retreat mechanic (#12) provides an escape valve, but the dynamic hull band transitions (#10, Option B) mean that taking damage mid-voyage can cascade.

**Recommendation**: In Vertical Slice, add a "recovery" mechanic — e.g., burning 1 cloud-coins worth of supplies during navigation to restore 5 hull integrity (once per voyage). This gives skilled players who've accumulated resources a way to break the spiral.

**Finding 3e-3: Route-Gated Difficulty is Player-Chosen, Not Progression-Driven (INFO)**

The MVP's 2 routes offer a difficulty choice:
- sky-reef-arc-01: identified, safe, short
- storm-cut-01: rumored, storm+low-visibility, medium

This is horizontal difficulty (player chooses their challenge) rather than vertical difficulty (challenge increases with progress). The world repair system (#13) is supposed to create vertical difficulty by unlocking harder routes — but with only 1 repair node in MVP, this progression isn't realized. For MVP, this is acceptable scope limitation.

**Finding 3e-4: Combat Damage 12-18 is High Relative to MVP's "Mild Pressure" Pillar (WARNING)**

See also 3f-2. With hull_max=100, a single Tank encounter deals 12-18% of total hull. After 2 Tank encounters, the player is likely in "damaged" band (≤75). After 4-5, they're in "critical" (≤25). Given that encounter checks happen every 12s (or faster as hull degrades), a bad voyage can spiral quickly.

Compare to Pillar 4 ("Unknown Brings Mild Pressure"): 12-18 damage is not "mild" — it represents a significant chunk of the player's primary survival resource. The Emergency Handling option exists precisely because Tank damage is so high, but Emergency Handling consumes the game's scarcest resource (repair_kit).

### Verdict: No critical issues for MVP scope. The zero difficulty curve and snowball hull mechanics must be addressed in Vertical Slice design.

---

## 3f: Pillar Alignment Check

### Pillar 1: "Plan Before Adventure"

**Assessment: STRONG ALIGNMENT**

Supporting systems:
- #9 Chart/Route Planning: 5-state chart flow, route_selectability with 7 branches, scout preview integration, two-step departure confirmation
- #8 Module Configuration: deliberate module choice before departure, unchecked state penalty (η=0.95) for non-inspection
- #6 Knowledge: rewards prior investigation (route visibility gated by knowledge state)
- #10 Scout Preview: N_preview = floor(η_scout × 2) provides forward information rewarding planning
- #16 Departure Checklist Screen (S6): mandatory checks + advisory partner feedback

The entire pre-voyage flow (Hub → Chart → Confirm → Depart) is built around this pillar. Every system in the planning phase reinforces deliberate preparation.

**No issues found.**

### Pillar 2: "World Responds to Care"

**Assessment: STRONG ALIGNMENT**

Supporting systems:
- #13 World Repair: permanent, irreversible world state changes (lighthouse lights up, route becomes traversable)
- #14 Market: stall unlocking tied to repair progress, settlement activity tiers
- #8 Hull Scars: narrative counter of damage sustained (though currently no mechanical effect)
- #7 Nest Trace: 4-stage visual progression in Hub responding to partner nest accumulation
- #15 Partner: nest items are irreversibly placed, creating a permanent "cared-for" visual

The "world responds" pillar is delivered primarily through #13 and #14. The irreversibility of repairs reinforces that player actions have permanent consequences.

**One note**: Hull scars (#8) are narrative-only — they count but don't affect gameplay. For Vertical Slice, consider making scars visible on the airship exterior (cosmetic only, consistent with the anti-pillar of "no numeric-only growth"). This would strengthen the "world remembers what you've been through" feeling.

**No issues found.**

### Pillar 3: "Airship is Home, Not Just Vehicle"

**Assessment: STRONG ALIGNMENT**

Supporting systems:
- #7 Walkable Hub: side-view airship interior, room existence gated by module installation
- #15 Partner R2: "Cat is always present on the airship" — the companion makes it home
- #15 Nest: nest accumulation creates a growing "lived-in" space
- #7 Departure Modes: gangway (fixed route) vs helm (free flight) — reinforces ship as a place with multiple ways to leave
- #16 S3 Hub Screen: presents airship interior as the default game view
- #5 Storage: Pool 2 (in_storage) is the "home storage" — items stored in the airship

The airship-as-home fantasy is the most consistently supported pillar. Every system that touches the Hub reinforces it as a living space, not a menu.

**No issues found.**

### Pillar 4: "Unknown Brings Mild Pressure"

**Assessment: MOSTLY ALIGNED — ONE CONCERN**

Supporting systems:
- #9 Route Visibility: unknown routes are NEVER visible — true unknown
- #10 Scout Preview: N_preview checks ahead, but hidden tags reveal at only 30% per check — partial information maintains mild pressure
- #11 Free-Search: empty results don't consume attempts — reduces pressure
- #12 Retreat: always available — escape hatch maintains "mild" not "severe" pressure
- #6 Knowledge: knowledge progression reduces unknown over time — pressure naturally decreases with experience
- #16 Semantic Colors: warning colors at thresholds — communicates pressure without alarm

**Finding 3f-1: Combat Damage 12-18 Crosses from "Mild" to "Significant" Pressure (WARNING)**

Pillar 4 specifies "mild pressure" from the unknown. However, combat damage of 12-18 (12-18% of hull_max=100) is significant:
- 1 Tank encounter: 12-18% hull loss
- 2 Tank encounters: possibly entering "damaged" band (≤75)
- 4 Tank encounters: possibly entering "critical" band (≤25)
- Damaged band: +10% encounter frequency → more encounters → more damage (snowball)

The Emergency Handling option exists because the designers recognized Tank damage is too punishing, but Emergency Handling consumes the game's scarcest resource. The pressure ends up being "manage my repair_kit economy" rather than "face the unknown."

**Recommendation**: Reduce Tank damage to 6-10, or add a "glancing hit" outcome (50% chance to take only 3-5 damage). This keeps combat threatening without crossing into "severe" territory.

**Finding 3f-2: Hidden Tag Reveal at 30% Per Check Creates Long Periods of Uncertainty (INFO)**

In navigation (#10), hidden tags reveal at 30% per check (with T_base=12s). Expected checks to first reveal: ~3.3 checks = ~40 seconds. This creates a tension-building mechanic — the player knows something is hidden but doesn't know what. This is well-calibrated for "mild pressure."

### Pillar 5: "Few Deep Relationships Over Many Collections"

**Assessment: STRONG ALIGNMENT**

Supporting systems:
- #15 Partner: single sky-cat companion with 6-state runtime machine, naming ceremony (one-time, irreversible, max 3 skips), nest accumulation (4 items, irreversible)
- #15 Nest: 4 items, each placed irreversibly, visual progression
- #14 Market: 1 default stall (A-Tu), not a crowd — few relationships
- #7 Hub R7: nest trace anchor gives the partner's presence a physical manifestation in the home
- #6 Partner Observation Events: `reveal_rumor()` with confidence ≤66, making the partner a source of imperfect but valuable intel

The "few deep relationships" pillar is realized through the single partner system. The naming ceremony (one-time, irreversible) is the emotional anchor — it creates a moment of commitment. The nest accumulation provides ongoing visible proof of the relationship deepening.

**No issues found.**

### Anti-Pillar Checks

| Anti-Pillar | Status | Evidence |
|-------------|--------|----------|
| No brutal PvP | CONFIRMED | Single-player only; no multiplayer systems in MVP |
| No hard timer pressure | CONFIRMED | No countdown timers; retreat always available (#12); navigation uses delta time not wall clock (#10) |
| No infinite creature collection | CONFIRMED | 1 partner, 1 threat type in MVP |
| No pure fetch-quest trade | CONFIRMED | Exploration has risk/reward tension (#11); market uses currency from exploration, not quest turn-ins (#14) |
| No numeric-only growth | MOSTLY CONFIRMED | Growth is qualitative (knowledge, repair, nest), but module furnace_rating (#8) is a pure number — consider adding qualitative module effects in Vertical Slice |

### Verdict: ONE WARNING (3f-1). Combat damage is notably above "mild." All other pillars show strong alignment.

---

## 3g: Player Fantasy Coherence

### The Target Fantasy

From game-concept.md: "玩家是云海飞艇的船长，与自己的猫伙伴一起在飞艇上生活，规划航线，在低压但充满未知的云海中探索，收集资源，修复世界的伤痕，见证世界因自己的行动而逐渐复苏。"

Translation: "The player is the captain of a cloud-sea airship, living on the airship with their cat partner, planning routes, exploring in a low-pressure but unknown-filled cloud sea, collecting resources, repairing the world's scars, and witnessing the world gradually recover through their actions."

### Fantasy-to-System Mapping

| Fantasy Element | Delivering Systems | Coherence |
|-----------------|-------------------|-----------|
| "I am a captain" | #9 Chart planning, #10 Navigation decisions, #12 Combat command | HIGH — all decision systems cast player as captain |
| "I live on my airship" | #7 Walkable hub, #15 Nest, #5 Home storage | HIGH — ship is a place, not a menu |
| "My cat is my partner" | #15 All partner mechanics, #7 R7 nest anchor, #6 Partner observation | HIGH — cat is always present, mechanically and visually |
| "I explore the unknown" | #11 Exploration scenes, #6 Knowledge progression, #10 Hidden tags | HIGH — unknown is mechanical, not just thematic |
| "I repair the world" | #13 World repair (permanent), #14 Market revival | HIGH — repair has visible, permanent consequences |
| "The world recovers" | #13 Irreversible repair, #14 Settlement tiers, #7 Hub progression | HIGH — recovery is visual and mechanical |

### Findings

**Finding 3g-1: The Fantasy is Remarkably Coherent (POSITIVE)**

All 16 systems contribute to the captain-home-explorer-restorer fantasy without contradictions. There are no systems that undermine the fantasy or pull in a different direction. This is a strong indicator of a focused design vision.

**Finding 3g-2: The Cat is the Emotional Anchor — and It's Well-Protected (POSITIVE)**

The partner system (#15) has multiple design protections:
- R2: Cat always present on airship (can never be lost, dismissed, or killed)
- Naming: one-time, irreversible — creates emotional commitment
- Nest: 4 items, irreversible — creates visible proof of shared history
- Confidence ≤66: the cat is helpful but not omniscient — it's a companion, not a tool

These protections ensure the emotional core of the game cannot be undermined by mechanical outcomes. The cat is never in danger, never leaves, and always has a visible presence.

**Finding 3g-3: One Tension — "Captain Making Tactical Decisions" vs "Pillar 4 Mild Pressure" (INFO)**

The combat system (#12) positions the player as a captain making tactical decisions (Emergency Handle/Tank/Retreat), which supports the "I am a captain" fantasy. However, as noted in 3f-1, the damage values create significant pressure. There is a tension between:

- Fantasy: "I calmly assess threats and make smart captain decisions"
- Mechanic: "One wrong decision costs 15% of my hull and possibly a module"

This tension is manageable in MVP, but for Vertical Slice, the difficulty tuning should lean toward "more decisions, less damage per decision" to preserve both the captain fantasy and the mild-pressure pillar.

**Finding 3g-4: The "Plan → Execute → Return → See Impact" Loop is the Fantasy Delivery Vehicle (POSITIVE)**

The core loop from game-concept.md is fully realized by the system set:
1. Plan (Hub #7 → Chart #9 → Module config #8) — Captain fantasy
2. Execute (Navigation #10 → Exploration #11 → Combat #12) — Explorer fantasy
3. Return (Extraction #11 → Market #14 → Storage #5) — Survivor fantasy
4. See Impact (Repair #13 → Settlement activity #14 → Nest #15) — Restorer fantasy

Each phase of the loop delivers a distinct facet of the fantasy, and the transitions between phases are clean. The UI (#16) maps each phase to its own screen set, reinforcing the phase boundaries.

### Verdict: NO ISSUES. Player fantasy is well-supported and internally consistent across all 16 systems.

---

## Summary

### Issue Count

| Severity | Count | Category |
|----------|-------|----------|
| CRITICAL | 2 | 3c-1 (Tank trap option), 3d-1 (repair_kit supply gap) |
| WARNING | 5 | 3a-2 (broken dependency link), 3b-1 (exploration cognitive load), 3c-2 (module config dominance), 3c-3 (free-search exhaust), 3e-2 (hull snowball), 3e-4 (combat damage severity), 3f-1 (mild pressure vs combat damage) |
| INFO | 7 | Various architectural observations |
| POSITIVE | 5 | Well-designed patterns identified |

### Critical Issues Requiring Action

| ID | Issue | Affected Systems | Proposed Fix |
|----|-------|-----------------|--------------|
| **3c-1** | Tank is a trap option — never optimal when repair_kit available or depleted | #12 Combat | Reduce Tank damage to 5-10, remove module damage chance, OR add a 4th response option |
| **3d-1** | First world repair impossible without finding repair_kit in exploration (need 5, start with 4) | #13 World Repair, #5 Resources | Guarantee 1-2 repair_kit in early exploration, OR reduce lighthouse to 3 repair_kit, OR add 1 purchasable repair_kit at market |

### Warnings to Address in Vertical Slice

1. **3a-2**: Knowledge (#6) should list World Repair (#13) as dependency for ability unlock gating
2. **3b-1**: Exploration scene cognitive load — consider soft search limits or fatigue mechanic
3. **3c-2**: Scout+cargo module dominance — address when more threat types added
4. **3c-3**: Free-search exhaustion incentive — cap free searches at 2-3
5. **3e-1**: Zero difficulty curve — design scaling mechanisms for Vertical Slice
6. **3e-2**: Hull integrity death spiral — add recovery mechanic
7. **3f-1**: Combat damage 12-18 crosses "mild pressure" threshold — reduce to 6-10

### Design Strengths

1. **Dual-axis progression** (World Repair + Knowledge) provides complementary motivation channels
2. **Clean phase transitions** with well-defined decision scopes per phase
3. **Partner system protections** ensure emotional anchor integrity
4. **Market-to-repair feedback loop** creates satisfying "fix world → world helps back" cycle
5. **Cognitive load management** through focused decision points (ternary combat choice, unlimited planning time)
6. **Pillar alignment** is strong across 4 of 5 pillars; only Pillar 4 has a tension point
7. **Fantasy coherence** across all 16 systems without contradictions

### Overall Verdict

**APPROVED WITH CONDITIONS.** The 16-system MVP design is holistically sound. The core loop is intact, the pillars are well-supported, and the player fantasy is coherent. Two critical issues (Tank trap option and repair_kit supply gap) must be resolved before implementation. Seven warnings should be tracked for Vertical Slice design. No design contradictions were found across any system pair.

---

*End of Phase 3: Game Design Holism Review*
