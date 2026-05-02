# Review Log: 探索 / 搜撤场景

## Review — 2026-05-03 — Verdict: NEEDS REVISION (Revision 1 applied)

**Scope signal**: L (Large — 6 formulas, 5 upstream + 4 downstream dependencies, 21 edge cases, cross-cutting)

**Specialists**: game-designer, systems-designer, economy-designer, level-designer, ux-designer, qa-lead, narrative-director, creative-director (senior synthesis)

**Blocking items**: 3 | **Recommended**: 10

**Summary**: GDD #11 is mechanically rigorous with strong formula design and comprehensive edge case coverage. Three blocking issues identified and resolved in Revision 1: the `compute_loss` formula forced minimum 1-item loss regardless of λ (fixed with λ≤0 guard and max(0,…)), the C6 scout preview table contradicted F-11-03 (unified to formula logic), and Pool 5 initialization was underspecified (now preserves pre-exploration contents). The zone gradient was flattened (A_core Uncommon 0.50→0.35, D_outer 0.10→0.20) to reduce the 7.7x incentive disparity. Search point descriptions were added as mechanical support for the "识荒人" fantasy. Hull damage timing was aligned with #12 (immediate, not deferred to DEPARTED). Two non-deterministic ACs were made deterministic and a Test Tools Requirements appendix was added. The creative-director identified the "识荒人" fantasy-mechanics gap as the most consistent cross-specialist finding, flagged independently by Game Designer and Narrative Director. Overall structurally sound — targeted formula and specification corrections applied.

**Prior verdict resolved**: First review
