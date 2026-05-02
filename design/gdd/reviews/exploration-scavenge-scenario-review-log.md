# Review Log: 探索 / 搜撤场景

## Review — 2026-05-03 — Verdict: NEEDS REVISION (Revision 1 applied)

**Scope signal**: L (Large — 6 formulas, 5 upstream + 4 downstream dependencies, 21 edge cases, cross-cutting)

**Specialists**: game-designer, systems-designer, economy-designer, level-designer, ux-designer, qa-lead, narrative-director, creative-director (senior synthesis)

**Blocking items**: 3 | **Recommended**: 10

**Summary**: GDD #11 is mechanically rigorous with strong formula design and comprehensive edge case coverage. Three blocking issues identified and resolved in Revision 1: the `compute_loss` formula forced minimum 1-item loss regardless of λ (fixed with λ≤0 guard and max(0,…)), the C6 scout preview table contradicted F-11-03 (unified to formula logic), and Pool 5 initialization was underspecified (now preserves pre-exploration contents). The zone gradient was flattened (A_core Uncommon 0.50→0.35, D_outer 0.10→0.20) to reduce the 7.7x incentive disparity. Search point descriptions were added as mechanical support for the "识荒人" fantasy. Hull damage timing was aligned with #12 (immediate, not deferred to DEPARTED). Two non-deterministic ACs were made deterministic and a Test Tools Requirements appendix was added. The creative-director identified the "识荒人" fantasy-mechanics gap as the most consistent cross-specialist finding, flagged independently by Game Designer and Narrative Director. Overall structurally sound — targeted formula and specification corrections applied.

**Prior verdict resolved**: First review

---

## Review — 2026-05-03 — Verdict: NEEDS REVISION → APPROVED (Revision 2 applied)

**Scope signal**: L (Large — 6 formulas, 5 upstream + 4 downstream dependencies, 21 edge cases, cross-cutting)

**Specialists**: game-designer, systems-designer, economy-designer, level-designer, ux-designer, qa-lead, narrative-director, gameplay-programmer, creative-director (senior synthesis)

**Blocking items**: 5 | **Recommended**: 8

**Summary**: Re-review of Revision 1 surfaced 5 new blocking items and 1 creative concern. Four specification-level defects fixed: F-11-04 interface mismatch with #5 (per-stack calls broke atomicity) resolved by batch `extract_carried_to_storage()`; `build_threat_context` now correctly guards environmental threats (self-handled by #11); F-11-01 `sample_without_replacement([], 0)` edge case protected with empty-pool guard; registry `max(1,…)` sync'd to `max(0,…)` matching GDD. Four missing GM commands (`gm_set_voyage_result`, `gm_set_carried`, `gm_set_eta_scout`, `gm_set_exploration_state_variant`) added to Test Tools appendix for 8+ ACs. The "识荒人" fantasy-mechanics gap — the single most important creative concern — addressed with dual-variant `description_enhanced` gated on System #6 `has_relevant_intel()`, making search point descriptions knowledge-aware. Creative-director confirmed all fixes are specification-level tightening, not redesign. Revision 2 accepted as Approved.

**Prior verdict resolved**: Yes — NEEDS REVISION (Revision 1) → APPROVED (Revision 2)
