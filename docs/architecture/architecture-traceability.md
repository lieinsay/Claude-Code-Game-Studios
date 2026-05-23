# Architecture Traceability Index

> **Status**: Active
> **Last Updated**: 2026-05-24
> **Purpose**: 追踪每个 ADR → TR → GDD → Code 的完整链路。由 `/architecture-review` 审计覆盖率。

---

## Coverage Summary

| Layer | Systems | ADRs | TRs | TR Covered | TR Partial | TR Gap | Coverage % |
|-------|---------|------|-----|------------|------------|--------|------------|
| Foundation | #1-#4, #6 (Platform) | ADR-0001~0006 | 15 | 15 | 0 | 0 | 100% |
| Core | #5-#9 | ADR-0007~0012 | 15 | 15 | 0 | 0 | 100% |
| Feature | #10-#15 | ADR-0010, ADR-0011, ADR-0013, ADR-0014, ADR-0015, ADR-0018 | 18 | 18 | 0 | 0 | 100% |
| Presentation | #16-#17 | ADR-0012 | 6 | 4 | 2 | 0 | 66.7% |
| Design Gates | #19-#20 | Pending ADR decision | 0 | 0 | 0 | 0 | N/A |
| **Total** | **20 systems (18 ADR-covered + 2 design-gate)** | **16 ADRs** | **54** | **54** | **0** | **0** | **100% for registered TRs** |

> **Note**: ADR-0016 and ADR-0017 are now Accepted. Zero TR gaps — all 54 TRs now have ADR coverage paths.
> **Design Gate Note**: #19 Scene Composition and #20 Scene Physics Unit Design are In Design GDDs. They define the bottom-layer "physical world exploration" contract and require a future ADR/TR expansion only when implementation scope moves beyond existing Movement, Exploration, Interaction, and Presentation contracts.

---

## Full Traceability Matrix

### Foundation Layer

| ADR | System | GDD | TR IDs | Implementation |
|-----|--------|-----|--------|---------------|
| ADR-0001 | #1 Content Registry, Platform Shell | `content-data-state-registry.md` | TR-registry-001, TR-registry-002, TR-registry-003 | `src/core/registry.gd` (pending) |
| ADR-0001 | #2 Platform/Session Shell | `platform-session-shell.md` | TR-platform-001, TR-platform-002, TR-platform-003 | `src/core/session_shell.gd` (pending) |
| ADR-0002 | All systems (cross-cutting) | (signal naming convention) | — | All signal declarations |
| ADR-0003 | #3 Persistence | `local-save-world-state-persistence.md` | TR-persistence-001, TR-persistence-002, TR-persistence-003 | `src/core/persistence.gd` (pending) |
| ADR-0004 | #4 Movement/Interaction | `player-movement-interaction.md` | TR-movement-001, TR-movement-002, TR-movement-003 | `src/core/interaction_registry.gd` (pending) |
| ADR-0005 | #5 Resources | `resources-goods-capacity.md` | TR-resources-001, TR-resources-002, TR-resources-003 | `src/core/resources_manager.gd` (pending) |
| ADR-0006 | All systems (cross-cutting) | (Web platform constraints) | — | Project settings, export config |

### Core Layer

| ADR | System | GDD | TR IDs | Implementation |
|-----|--------|-----|--------|---------------|
| ADR-0007 | #6 Intel/Knowledge | `player-knowledge-intel.md` | TR-intel-001, TR-intel-002, TR-intel-003 | `src/core/intel_manager.gd` (pending) |
| ADR-0008 | #9 Chart/Route Planning | `chart-route-planning.md` | TR-chart-001, TR-chart-002, TR-chart-003 | `src/core/chart_manager.gd` (pending) |
| ADR-0009 | #8 Module/Hull | `airship-modules-hull-state.md` | TR-modules-001, TR-modules-002, TR-modules-003 | `src/core/module_hull_manager.gd` (pending) |
| ADR-0010 | #10 Navigation (cross-system) | `navigation-route-risk.md` | TR-navigation-001, TR-navigation-002, TR-navigation-003 | `src/feature/navigation_manager.gd` (pending) |
| ADR-0001 | #7 Airship Hub | `airship-hub.md` | TR-hub-001, TR-hub-002, TR-hub-003 | `src/core/hub_manager.gd` (pending) |

### Feature Layer

| ADR | System | GDD | TR IDs | Implementation | Status |
|-----|--------|-----|--------|---------------|--------|
| ADR-0010 | #10 Navigation | `navigation-route-risk.md` | TR-navigation-001, TR-navigation-002, TR-navigation-003 | pending | Accepted |
| ADR-0011 | #13 World Repair | `world-repair-unlock.md` | TR-repair-001, TR-repair-002, TR-repair-003 | pending | Accepted |
| ADR-0018 | #12 Combat/Threat | `combat-threat-handling.md` | TR-combat-001, TR-combat-002, TR-combat-003 | `src/core/combat/CombatManager.cs` + `tests/unit/combat/**` + `tests/integration/combat/**` (37/37 grouped checks PASS) | Accepted + implemented |
| ADR-0013 | #11 Exploration | `exploration-scavenge-scenario.md` | TR-exploration-001, TR-exploration-002, TR-exploration-003 | `src/feature/ExplorationManager.cs` + `tests/unit/exploration/**` + `tests/integration/exploration/**` (287/287 PASS) | Accepted + implemented |
| ADR-0014 | #14 Settlement | `port-village-market.md` | TR-settlement-001, TR-settlement-002, TR-settlement-003 | `src/core/settlement/SettlementManager.cs` + `tests/unit/settlement-market/**` + `tests/integration/settlement-market/**` (31/31 PASS) | Accepted + implemented |
| ADR-0015 | #15 Partner | `partner-relationships.md` | TR-partner-001, TR-partner-002, TR-partner-003 | `src/features/partner_relationships/PartnerManager.cs` + `tests/unit/partner-relationships/**` + `tests/integration/partner-relationships/**` (119/119 PASS) | Accepted + implemented |
| ADR-0017 | #18 Onboarding | `onboarding-first-loop.md` | TR-onboarding-001 | pending | **Accepted** |

### Presentation Layer

| ADR | System | GDD | TR IDs | Implementation | Status |
|-----|--------|-----|--------|---------------|--------|
| ADR-0012 | #16 UI/HUD | `ui-hud-chart-interface.md` | TR-ui-001, TR-ui-002, TR-ui-003, TR-ui-004 | `src/presentation/UIManager.cs` + `tests/unit/ui-hud-interface/*Test.csproj` (Stories 001-003 PASS; Stories 004-006 pending) | Accepted + partial implemented |
| ADR-0016 | #17 Feedback/VFX/Audio | `feedback-fx-audio.md` | TR-feedback-001, TR-feedback-002 | pending | **Accepted** |

### Design Gate Layer

| ADR | System | GDD | TR IDs | Implementation | Status |
|-----|--------|-----|--------|---------------|--------|
| — | #19 Scene Composition | `scene-composition-system.md` | — | pending | In Design; consumes #20 |
| — | #20 Scene Physics Unit Design | `scene-physics-unit-system.md` | — | pending | In Design; defines Scene Physics Contract |

---

## GDD → ADR Reverse Index

For designers: given a GDD, which ADRs govern its implementation?

| GDD System | # | Primary ADR | Supporting ADRs |
|------------|----|-------------|-----------------|
| Content Registry | #1 | ADR-0001 | ADR-0002 |
| Platform/Session Shell | #2 | ADR-0001, ADR-0006 | ADR-0002 |
| Persistence | #3 | ADR-0003 | ADR-0002 |
| Movement/Interaction | #4 | ADR-0004 | ADR-0001, ADR-0002 |
| Resources | #5 | ADR-0005 | ADR-0002 |
| Intel/Knowledge | #6 | ADR-0007 | ADR-0002, ADR-0003, ADR-0005 |
| Airship Hub | #7 | ADR-0001 (Hub section) | ADR-0002, ADR-0003, ADR-0004 |
| Module/Hull | #8 | ADR-0009 | ADR-0002, ADR-0003 |
| Chart/Route Planning | #9 | ADR-0008 | ADR-0002, ADR-0003, ADR-0007 |
| Navigation | #10 | ADR-0010 | ADR-0002, ADR-0008, ADR-0009 |
| Exploration | #11 | ADR-0013 | ADR-0010, ADR-0018 |
| Combat/Threat | #12 | ADR-0018 | ADR-0002, ADR-0005, ADR-0009 |
| World Repair | #13 | ADR-0011 | ADR-0002, ADR-0005, ADR-0007 |
| Settlement/Market | #14 | ADR-0014 | ADR-0002, ADR-0005, ADR-0011, ADR-0013 |
| Partner | #15 | ADR-0015 | ADR-0007 |
| UI/HUD | #16 | ADR-0012 | ADR-0002 |
| Feedback/VFX/Audio | #17 | ADR-0016 | ADR-0002 |
| Onboarding | #18 | ADR-0017 | ADR-0008 |
| Scene Composition | #19 | — | ADR-0001, ADR-0004, ADR-0012, ADR-0013 |
| Scene Physics Unit Design | #20 | — | ADR-0001, ADR-0004, ADR-0012, ADR-0013 |

---

## ADR Dependency Graph

```
Foundation (6 ADRs)
  ADR-0001 ──→ ADR-0004 (InteractionRegistry Autoload)
  ADR-0002 ──→ (all others — signal protocol)
  ADR-0003 ──→ ADR-0007, ADR-0008, ADR-0009 (snapshot persistence)
  ADR-0005 ──→ ADR-0007, ADR-0011, ADR-0018 (resource consumption)
  ADR-0006 ──→ (all — Web constraints)

Core (5 ADRs + cross-system)
  ADR-0007 ──→ ADR-0008 (query_route_knowledge)
  ADR-0008 ──→ ADR-0010 (route_committed → voyage start)
  ADR-0009 ──→ ADR-0010 (can_depart → voyage start)
  ADR-0010 ──→ ADR-0013 (EncounterContext → Exploration)

Feature (7 Accepted)
  ADR-0018 ──→ (no downstream ADRs yet — Threat → Exploration)
  ADR-0011 ──→ ADR-0007, ADR-0008, ADR-0014 (repair_completed 驱动摊位解锁)
  ADR-0013 ──→ ADR-0014 (探索产出的货币为集市提供资金来源)
  ADR-0014 ──→ (no downstream ADRs yet — Settlement 为 Feature 层终端系统)
  ADR-0015 ──→ (no downstream — Partner 为 Feature 层终端系统, query interface for Hub #7)
  ADR-0017 ── (no downstream ADRs yet — Onboarding VS)

Presentation (2 Accepted)
  ADR-0012 ──→ (no downstream ADRs — UI consumes all)
  ADR-0016 ──→ (no downstream ADRs yet — Feedback VS)
```

**Circular dependency check**: No cycles detected. All dependency edges are unidirectional (Foundation → Core → Feature → Presentation).

---

## Gap Analysis

### Critical Gaps (block Production entry)

| Gap | System | Impact | Required By |
|-----|--------|--------|-------------|
| — | — | No critical gaps remaining | — |

### Non-Critical Gaps

| Gap | System | Impact |
|-----|--------|--------|
| #19 | Scene Composition | No ADR/TR yet; design-gate GDD depends on #20 and should trigger ADR only when implementation expands scene authoring/runtime contracts |
| #20 | Scene Physics Unit Design | No ADR/TR yet; foundation physics contract for physical-world exploration, movement, collision, occlusion, scale, and special surfaces |

### Partial Coverage

| TR ID | System | Partial Reason |
|-------|--------|---------------|
| TR-navigation-002 | #10 Navigation | ADR-0010 covers data bridge but not formula implementation details |
| TR-navigation-003 | #10 Navigation | ADR-0010 covers data bridge but not hull band transitions mid-voyage |
| TR-combat-002 | #12 Combat | ADR-0018 covers decision breath state machine but UI panel spec is in ADR-0012 |

---

## Vertical Slice ADR Implementation Triggers

| ADR | System | Priority | Recommended Trigger | Earliest Date |
|-----|--------|----------|--------------------|---------------|
| ADR-0016 | #17 Feedback | 🟡 MEDIUM | Use for VFX/Audio implementation story split | Production Sprint 2+ |
| ADR-0017 | #18 Onboarding | 🟢 LOW | Use for Vertical Slice onboarding implementation story split | Production Sprint 4+ |

---

## Verification Checklist

- [ ] All Accepted ADRs have Engine Compatibility section with Godot 4.6.2 stamped
- [ ] All Accepted ADRs have GDD Requirements Addressed section with TR ID linkage
- [ ] All Accepted ADRs have ADR Dependencies section
- [ ] Zero circular dependencies in ADR graph
- [ ] Zero Foundation layer gaps (100% coverage achieved)
- [ ] All Core layer TRs have ADR coverage (100% achieved)
- [x] Vertical Slice ADR acceptance and implementation trigger schedule documented
- [ ] TR Registry has 54 active entries across 18 ADR-covered systems
- [ ] #19/#20 design-gate GDDs reviewed before ADR/TR expansion
