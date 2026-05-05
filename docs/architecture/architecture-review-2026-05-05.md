# Architecture Review Report

**Date**: 2026-05-05
**Engine**: Godot 4.6.2 + GDScript
**GDDs Reviewed**: 18 (systems #1-#18)
**ADRs Reviewed**: 12 (ADR-0001 through ADR-0012, all Accepted)

---

## Traceability Summary

Total requirements: **52**
- ✅ Covered: **34** (65.4%)
- ⚠️ Partial: **9** (17.3%)
- ❌ Gaps: **9** (17.3%)

### Coverage Matrix

| TR-ID | System | Status | Covering ADR(s) |
|-------|--------|--------|-----------------|
| TR-registry-001 | #1 Registry | ⚠️ PARTIAL | ADR-0001 (Autoload positioning only — no domain ADR for content schema/query API) |
| TR-registry-002 | #1 Registry | ⚠️ PARTIAL | ADR-0001 (Autoload positioning only) |
| TR-registry-003 | #1 Registry | ⚠️ PARTIAL | ADR-0001 (Autoload positioning only) |
| TR-platform-001 | #2 Platform | ✅ COVERED | ADR-0001, ADR-0006 |
| TR-platform-002 | #2 Platform | ✅ COVERED | ADR-0001, ADR-0006 |
| TR-platform-003 | #2 Platform | ✅ COVERED | ADR-0001, ADR-0006 |
| TR-persistence-001 | #3 Persistence | ✅ COVERED | ADR-0003 |
| TR-persistence-002 | #3 Persistence | ✅ COVERED | ADR-0003 |
| TR-persistence-003 | #3 Persistence | ✅ COVERED | ADR-0003 |
| TR-movement-001 | #4 Movement | ✅ COVERED | ADR-0001, ADR-0004 |
| TR-movement-002 | #4 Movement | ✅ COVERED | ADR-0004 |
| TR-movement-003 | #4 Movement | ✅ COVERED | ADR-0004 |
| TR-resources-001 | #5 Resources | ✅ COVERED | ADR-0005 |
| TR-resources-002 | #5 Resources | ✅ COVERED | ADR-0005 |
| TR-resources-003 | #5 Resources | ✅ COVERED | ADR-0005 |
| TR-intel-001 | #6 Intel | ✅ COVERED | ADR-0007 |
| TR-intel-002 | #6 Intel | ✅ COVERED | ADR-0007 |
| TR-intel-003 | #6 Intel | ✅ COVERED | ADR-0007 |
| TR-hub-001 | #7 Hub | ⚠️ PARTIAL | ADR-0004 (interaction), ADR-0008 (departure), ADR-0009 (room gating) — ADR-0013 deferred |
| TR-hub-002 | #7 Hub | ⚠️ PARTIAL | ADR-0008 (departure modes) — ADR-0013 deferred |
| TR-hub-003 | #7 Hub | ⚠️ PARTIAL | ADR-0009 (room gating via module install) — ADR-0013 deferred |
| TR-modules-001 | #8 Modules | ✅ COVERED | ADR-0009 |
| TR-modules-002 | #8 Modules | ✅ COVERED | ADR-0009 |
| TR-modules-003 | #8 Modules | ✅ COVERED | ADR-0009 |
| TR-chart-001 | #9 Chart | ✅ COVERED | ADR-0008 |
| TR-chart-002 | #9 Chart | ✅ COVERED | ADR-0008 |
| TR-chart-003 | #9 Chart | ✅ COVERED | ADR-0008 |
| TR-navigation-001 | #10 Navigation | ✅ COVERED | ADR-0010 |
| TR-navigation-002 | #10 Navigation | ✅ COVERED | ADR-0010 |
| TR-navigation-003 | #10 Navigation | ✅ COVERED | ADR-0010 |
| TR-exploration-001 | #11 Exploration | ✅ COVERED | ADR-0010 |
| TR-exploration-002 | #11 Exploration | ✅ COVERED | ADR-0010 |
| TR-exploration-003 | #11 Exploration | ✅ COVERED | ADR-0010 |
| TR-combat-001 | #12 Combat | ❌ GAP | **No ADR exists.** Not listed in deferred backlog. |
| TR-combat-002 | #12 Combat | ❌ GAP | **No ADR exists.** |
| TR-combat-003 | #12 Combat | ❌ GAP | **No ADR exists.** |
| TR-repair-001 | #13 Repair | ✅ COVERED | ADR-0011 |
| TR-repair-002 | #13 Repair | ✅ COVERED | ADR-0011 |
| TR-repair-003 | #13 Repair | ✅ COVERED | ADR-0011 |
| TR-settlement-001 | #14 Settlement | ❌ GAP | ADR-0014 deferred — no target date |
| TR-settlement-002 | #14 Settlement | ❌ GAP | ADR-0014 deferred |
| TR-settlement-003 | #14 Settlement | ⚠️ PARTIAL | ADR-0002 (repair_completed signal contract) — ADR-0014 deferred |
| TR-partner-001 | #15 Partner | ❌ GAP | ADR-0015 deferred |
| TR-partner-002 | #15 Partner | ❌ GAP | ADR-0015 deferred |
| TR-partner-003 | #15 Partner | ❌ GAP | ADR-0015 deferred |
| TR-ui-001 | #16 UI | ✅ COVERED | ADR-0012 |
| TR-ui-002 | #16 UI | ✅ COVERED | ADR-0012 |
| TR-ui-003 | #16 UI | ✅ COVERED | ADR-0012 |
| TR-ui-004 | #16 UI | ✅ COVERED | ADR-0006, ADR-0012 |
| TR-feedback-001 | #17 Feedback | ⚠️ PARTIAL | ADR-0002 (semantic event signals) — ADR-0016 deferred |
| TR-feedback-002 | #17 Feedback | ⚠️ PARTIAL | ADR-0011, ADR-0001 (MVP feedback ownership assignment) — ADR-0016 deferred |
| TR-onboarding-001 | #18 Onboarding | ❌ GAP | ADR-0017 deferred |

---

## Coverage Gaps

### Critical (must resolve before Production)

| TR-ID | System | Requirement | Suggested ADR |
|-------|--------|-------------|---------------|
| TR-combat-001 | #12 Combat | 1 threat type with 3 response options | ADR-0018: Threat Resolution Architecture |
| TR-combat-002 | #12 Combat | Decision breath (no real-time pressure) | (part of ADR-0018) |
| TR-combat-003 | #12 Combat | 3 outcomes: damaged/knocked back/retreat | (part of ADR-0018) |

### Deferred (acceptable for Pre-Production)

| System | Deferred ADR | TRs Affected |
|--------|-------------|--------------|
| #7 Hub | ADR-0013 | TR-hub-001/002/003 |
| #14 Settlement | ADR-0014 | TR-settlement-001/002/003 |
| #15 Partner | ADR-0015 | TR-partner-001/002/003 |
| #17 Feedback | ADR-0016 | TR-feedback-001/002 |
| #18 Onboarding | ADR-0017 | TR-onboarding-001 |

### Partial Coverage (acceptable with caveat)

| System | Issue |
|--------|-------|
| #1 Registry | ADR-0001 establishes Autoload positioning but no domain ADR covers content schemas, query API design, or state ownership enforcement |

---

## Cross-ADR Conflicts

**No conflicts detected.**

Dependency graph analysis:
- All 12 ADRs form a DAG with no cycles
- Topological sort is feasible: ADR-0001 → 0002/0003 → 0004/0005/0006 → 0007/0008 → 0009 → 0010/0011 → 0012
- Data ownership boundaries are consistent:
  - #5 Resources owns all resource state (ADR-0005)
  - #6 Intel owns all knowledge state (ADR-0007)
  - #9 Chart owns route data; #16 UI owns visual rendering (ADR-0008, ADR-0012)
  - #10 Navigation produces EncounterContext; #11 Exploration consumes it (ADR-0010)
- No performance budget conflicts: all ADR guardrails sum within 16ms frame budget
- No state management conflicts: ownership is clearly assigned per ADR

---

## ADR Dependency Order

### Foundation (no dependencies)
1. ADR-0001: Autoload/Scene Boot Order

### Depends on Foundation
2. ADR-0002: Signal Communication Protocol (requires ADR-0001)
3. ADR-0003: Save System/Snapshot JSON (requires ADR-0001)

### Depends on Foundation + Signal + Save
4. ADR-0004: InteractionHandler @abstract (requires ADR-0001, ADR-0002)
5. ADR-0005: Resource Pool System (requires ADR-0001, 0002, 0003, 0004)
6. ADR-0006: Web Platform Constraints (requires ADR-0001, 0003)

### Core Layer
7. ADR-0007: Intel/Knowledge System (requires ADR-0001, 0002, 0003, 0006)
8. ADR-0008: Chart Route State Machine (requires ADR-0001, 0002, 0003, 0006, 0007)
9. ADR-0009: Module/Hull System (requires ADR-0001, 0002, 0003, 0005, 0006, 0007)

### Feature Layer
10. ADR-0010: EncounterContext Type (requires ADR-0001, 0002, 0003, 0009)
11. ADR-0011: WorldRepair State Machine (requires ADR-0001, 0002, 0003, 0005, 0006, 0007)
12. ADR-0012: UI Input Routing/Dual Focus (requires ADR-0001, 0002, 0006, 0008)

All dependencies are satisfied — no ADR depends on a Proposed or non-existent ADR.

---

## Engine Compatibility Issues

### Version Consistency
- All 12 ADRs agree on **Godot 4.6.2**. No stale version references.

### Deprecated API References
- **None found.** All ADRs use modern Godot 4 patterns: `signal.connect(callable)`, `instantiate()`, `@abstract`, typed signals.

### Post-Cutoff API Registry
| API | Version | ADRs Using | Risk |
|-----|---------|------------|------|
| `@abstract` decorator | 4.5 | ADR-0001, ADR-0004 | MEDIUM — runtime behavior on missing impl needs Web export verification |
| `NavigationServer2D` | 4.5 | ADR-0001 | MEDIUM — dedicated 2D nav server API surface verification |
| `FileAccess.store_*` returns bool | 4.4 | ADR-0003 | LOW — verified in engine reference docs |
| `duplicate_deep()` | 4.5 | ADR-0003 | LOW — verified for nested Dictionary deep copy |
| Dual-focus system | 4.6 | ADR-0001, ADR-0012 | HIGH — interaction with custom 4-layer input routing needs runtime verification |
| `JavaScriptBridge` lifecycle | 4.x | ADR-0003, ADR-0006 | MEDIUM — `visibilitychange`/`pagehide` reliability varies by browser |

### Documentation Issues (non-blocking)
- **ADR-0008**: `create_tween()` listed as "Post-Cutoff API" but is a Godot 4.0 feature (pre-cutoff). Remove the label.
- **ADR-0012**: `FoldableContainer (4.5)` referenced in Engine Compatibility table but never used in ADR body. Dangling reference — remove from table or document actual usage.

### Engine Specialist Findings
- No Web export conflicts detected. All ADRs respect single-threaded, WebGL 2, IndexedDB, and AudioContext constraints.
- ADR-0012 correctly identifies dual-focus system (4.6) as 🔴 HIGH risk requiring runtime verification.
- No deprecated pattern usage: all signal connections use `sender.signal.connect(receiver.method)`, all state uses Dictionary (not Godot Resource/.tres), all save data uses Canonical JSON (not `store_var()`).

---

## GDD Revision Flags

No GDD revision flags — all GDD assumptions are consistent with verified engine behaviour and Accepted ADRs.

---

## Architecture Document Coverage

`docs/architecture/architecture.md` (v1, 2026-05-04) was authored before ADRs existed. Coverage assessment:

| Check | Status |
|-------|--------|
| All 18 systems appear in layer map | ✅ |
| Data flow covers cross-system communication | ✅ |
| API boundaries cover integration requirements | ✅ |
| Orphaned architecture (systems with no GDD) | ✅ None |
| ADR Audit section current | ❌ Still shows "0 existing ADRs found" — needs update |
| Traceability section current | ❌ Shows 52 GAPs — now 17 resolved |

**Recommendation**: Update `architecture.md` ADR Audit and Traceability sections to reflect current 12 Accepted ADRs. This is non-blocking for Pre-Production but should be done before Production.

---

## Verdict: CONCERNS

**0 blocking issues. 1 critical gap. 6 deferred ADRs.**

### Blocking Issues (none for Pre-Production)
No Foundation or Core layer gaps. All 12 Must-Have and Should-Have ADRs are Accepted. The architecture is sufficient for Pre-Production prototyping.

### Required ADRs (before Production)
1. **ADR-0018: Combat/Threat Resolution** — covers TR-combat-001/002/003. Critical: System #12 has zero architecture coverage and is not even in the deferred backlog.
2. **ADR-0013: Airship Hub Scene Architecture** — deferred, covers TR-hub-001/002/003
3. **ADR-0014: Settlement/Market** — deferred, covers TR-settlement-001/002/003

### Recommended Actions
1. Create ADR-0018 (Combat) before starting any combat implementation — this is the #1 priority gap
2. Update `architecture.md` ADR Audit section to reflect current state
3. Fix ADR-0008 and ADR-0012 documentation issues (non-blocking)
4. Deferred ADRs 0013-0017 can be authored during Feature-layer implementation

---

## History

| Date | Verdict | Covered | Partial | Gaps | Notes |
|------|---------|---------|---------|------|-------|
| 2026-05-05 | CONCERNS | 34 (65.4%) | 9 (17.3%) | 9 (17.3%) | Initial review — 12 ADRs Accepted |
