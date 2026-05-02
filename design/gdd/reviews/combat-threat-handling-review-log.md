# Combat & Threat Handling — Review Log

## Review — 2026-05-02 — Verdict: MAJOR REVISION NEEDED → REVISED

**Scope signal**: M
**Specialists**: game-designer, systems-designer, qa-lead, ux-designer, gameplay-programmer, creative-director
**Blocking items**: 11 | **Recommended**: 6

**Summary**: The GDD has strong bones — the three-outcome model cleanly delivers Pillar 4, formulas are rigorous, edge cases are thoughtful, and the Player Fantasy section is one of the best-written in the project. However, the review found 11 blocking issues across three categories: (1) three phantom interfaces (`apply_hull_damage`, `apply_module_damage` in #8; `consume_in_combat` in #5) that were called but not defined in their upstream GDDs; (2) cross-system data contract inconsistencies (field name mismatches, missing `combat_result` fields, `retreat_flagged` having no consumer in #11); and (3) a binding creative decision that the audiovisual language (red flashes, camera shake, alarm stings, heartbeat pulse) deployed action/horror-game tropes that directly contradicted the restrained, captain's-log fantasy stated in the Player Fantasy section.

**Revision applied 2026-05-02**: All 11 blockers resolved:
1. `apply_hull_damage()` / `apply_module_damage()` added to #8 Core Rules (rules 20–21) with full contract specs
2. `consume_in_combat()` / `get_carried_contents_by_tag()` added to #5 Operations table
3. `retreat_flagged` consumer wired in #11: F-11-04 now takes `retreat_flagged` boolean instead of `voyage_result` enum
4. `combat_result` schema aligned: `resources_dropped` → `resources_consumed`, `knockback` and `retreat_flagged` added to #11
5. V/A Requirements section fully rewritten per CD binding tonal decision — all action-game tropes replaced with restrained, logbook-appropriate audiovisual language
6. `resolve_threat` signature unified to 1-param (stateful service pattern)
7. EC-12-04 module damage filter semantics clarified to use `actual_state` (not `visible_state`)
8. Re-entry guard added (FIFO queue, 4-cap) for AWAITING_RESPONSE state
9. Threat persistent state durability across save/load defined
10. AC-12-04b changed from [SEEDED] to [RANGE] with statistical assertion
11. AC-12-22 changed from [SEEDED] to [DETERMINISTIC]

**Cross-GDD changes**: #8 (`airship-modules-hull-state.md`), #5 (`resources-goods-capacity.md`), #11 (`exploration-scavenge-scenario.md`) all updated with interface definitions, `build_threat_context()` function, `facing` property on `threat_point`, and schema corrections.

**Prior verdict resolved**: No — first review. 11 blockers resolved in revision round 1.

## Review — 2026-05-03 — Verdict: NEEDS REVISION → REVISED

**Scope signal**: M
**Specialists**: game-designer, systems-designer, qa-lead, ux-designer, economy-designer, creative-director
**Blocking items**: 6 | **Recommended**: 12

**Summary**: Second-pass review after round-1 blocker resolution. The GDD is structurally strong — 8/8 sections, rigorous formulas, comprehensive edge cases. The review found 6 blocking specification corrections (no redesigns): repair_kit lacked a canonical entity ID in the content registry; the decision panel could not be dismissed, trapping players in a modal that contradicted the "calm captain" fantasy; AC-12-04b CI math was incorrect (99% bounds computed wrong); cross-band warning threshold was off by 12 points (hull≤30 should be hull≤42); Tank warning at hull≤5 was dangerously low for 12-18 damage attacks; AC-12-10 was mislabeled [SEEDED] when its single-element output was deterministic. 12 recommended items flagged for playtest monitoring — notably the Tank/Emergency Handling economic inversion, decision panel information density, and repair_kit economy unverifiability without exploration loot tables. Creative Director assessment: fantasy alignment at 85%; remaining gap is almost entirely the panel dismiss issue.

**Revision applied 2026-05-03**: All 6 blockers resolved:
1. `repair_kit` registered in entities.yaml items section (category: supply); all GDD references standardized to canonical ID `"repair_kit"`
2. Panel dismiss enabled (Esc/overlay click); UI-15 Threat Active indicator added for persistent awareness; keyboard mapping defined as semantic [E][T][R]
3. AC-12-04b CI corrected: proportion [0.459, 0.541], mean ±0.16 (99% CI at n=1000)
4. EC-12-02 cross-band threshold fixed: hull≤30 → hull≤42; V-05, UI-10, state table, AC-12-14 updated
5. Tank warning threshold raised: hull≤5 → hull≤18; C3, Tuning Knobs #7, V-07, UI-10, AC-12-02c, registry constant updated
6. AC-12-10 relabeled [SEEDED] → [DETERMINISTIC]

**Cross-file changes**: registry (`entities.yaml`) — repair_kit item registered, hull_warning_threshold constant updated (5→18).

**Prior verdict resolved**: Yes — all 11 round-1 blockers previously resolved. 6 round-2 blockers now resolved.

## Review — 2026-05-03 — Verdict: APPROVED

**Scope signal**: M
**Mode**: Lean (no specialist agents)
**Blocking items**: 0 | **Recommended**: 3

**Summary**: Revision 2 acceptance review. All 6 prior blockers verified as resolved. Focus-area checks pass: resolve_threat/calc_hull_damage contracts aligned with #8 #5; hull_warning_threshold (5→18) consistent across GDD + registry; all 12 combat formulas/constants registered in entities.yaml. Three recommended items found and immediately applied: repair_kit registry entry missing `material_tags: [repair-material]` (would break `get_carried_contents_by_tag`), F-12-03 variable table clarified from `installed_modules` to `eligible_modules` with explicit filtering description, and all stale `installed_modules` references unified to `eligible_modules`. This is the lean re-review confirming Revision 2 acceptance. No blockers remain.

**Prior verdict resolved**: Yes — Revision 2 (NEEDS REVISION with 6 blockers) now resolved. GDD APPROVED.
