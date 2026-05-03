# Active Design Session

<!-- STATUS -->
Epic: Systems Design
Feature: Cross-GDD Holism Review (Phase 2+3 Fixes Applied)
Task: All 5 BLOCKING/CRITICAL issues resolved — awaiting user review
<!-- /STATUS -->

## Current: /review-all-gdds Critical Fixes Complete

- **Phase 2 Report**: `production/session-state/gdd-cross-review-2026-05-03.md`
- **Phase 3 Report**: `production/session-state/phase3-game-design-holism-review.md`
- **Status**: All 5 issues fixed across 3 GDDs + entities.yaml

### Resolution Summary

| ID | Issue | Severity | Status | Fix |
|----|-------|----------|--------|-----|
| B1 | Cross-band warning threshold stale | BLOCKING | ✅ FIXED | Recalculated 37→33 (after C1 cascade) |
| B2 | Currency acquisition unassigned | BLOCKING | ✅ Already resolved (entities.yaml + #14 deps) |
| B3 | #6 stale references to #15 | BLOCKING | ✅ Already resolved (#6 already updated) |
| C1 | Tank combat trap option | CRITICAL | ✅ FIXED | Damage 8-12, module 30%, threshold 33 |
| C2 | repair_kit supply gap (5→4) | CRITICAL | ✅ FIXED | Lighthouse now needs 4 repair_kit (matches starting quantity) |

### Files Modified

- `design/gdd/combat-threat-handling.md` — C1 Tank rebalance (~20 edits)
- `design/gdd/world-repair-unlock.md` — C2 repair_kit 5→4 (7 edits)
- `design/registry/entities.yaml` — Synced constants + formula description

### Next Steps

- User reviews the changes (git diff)
- If approved, proceed to Phase 4 (cross-system scenario walkthrough) or Phase 5 (final verdict)
