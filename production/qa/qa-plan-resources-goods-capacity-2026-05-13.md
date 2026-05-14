## QA Plan: Resources, Goods & Capacity #5

**Date**: 2026-05-13
**Stage**: Pre-Production - Desktop C# Foundation Ready
**Engine**: Godot 4.6.2 .NET
**Scope**: `production/epics/resources-goods-capacity/EPIC.md`
**Smoke Report**: `production/qa/smoke-2026-05-13.md`

---

### Scope

This QA cycle covers the completed Resources, Goods & Capacity epic (#5), including all 9 implemented stories:

- Story 001: Resource Identity & Stack Merge
- Story 002: Dual Capacity System
- Story 003: Cargo Model & Unpack
- Story 004: Weight & Mass Tracking
- Story 005: Core Atomic Operations
- Story 006: State Machine & Pool Transitions
- Story 007: Specialized Operations
- Story 008: Signal Contract & Reentry Guard
- Story 009: Persistence & External Integration

The primary QA objective is to confirm that the C# resource contract is stable enough for downstream systems and that the manual runtime gaps from smoke-check are explicitly tracked.

---

### Story Classification

| Story | Type | Automated Required | Manual Required | Blocker? |
|-------|------|--------------------|-----------------|----------|
| 001 Resource Identity & Stack Merge | Logic | Yes | No | No |
| 002 Dual Capacity System | Logic | Yes | No | No |
| 003 Cargo Model & Unpack | Logic | Yes | No | No |
| 004 Weight & Mass Tracking | Logic | Yes | No | No |
| 005 Core Atomic Operations | Logic | Yes | No | No |
| 006 State Machine & Pool Transitions | Logic | Yes | No | No |
| 007 Specialized Operations | Integration | Yes | No, integration test satisfies story gate | No |
| 008 Signal Contract & Reentry Guard | Integration | Yes | No, integration test satisfies story gate | No |
| 009 Persistence & External Integration | Integration | Yes | No, integration test satisfies story gate | No |

---

### Automated Test Requirements

| Story | Expected Test Evidence | Status |
|-------|------------------------|--------|
| 001 | `tests/unit/resources/StackMergeTest.csproj` | PASS |
| 002 | `tests/unit/resources/CapacitySystemTest.csproj` | PASS |
| 003 | `tests/unit/resources/CargoUnpackTest.csproj` | PASS |
| 004 | `tests/unit/resources/WeightMassTest.csproj` | PASS |
| 005 | `tests/unit/resources/CoreOperationsTest.csproj` | PASS |
| 006 | `tests/unit/resources/ResourcesStateMachineTest.csproj` | PASS |
| 007 | `tests/integration/resources/SpecializedOpsTest.csproj` | PASS |
| 008 | `tests/integration/resources/ResourcesSignalContractTest.csproj` | PASS |
| 009 | `tests/integration/resources/ResourcesPersistenceIntegrationTest.csproj` | PASS |

Additional sprint gate evidence:

- `dotnet build CloudWeaverVoyage.sln --no-restore` - PASS
- All C# test projects - PASS (47/47 projects, 511/511 reported checks)
- Godot headless startup - PASS
- `res://src/scenes/SessionShell.tscn` headless load - PASS
- `res://src/scenes/ShellUi.tscn` headless load - PASS
- `git diff --check` - PASS, LF/CRLF advisory warnings only

---

### Manual QA Scope

Manual QA is not required to close the individual #5 Logic/Integration stories, because all story acceptance gates are covered by automated C# tests. It is required as a sprint hand-off check for the visible runtime paths not covered by the headless smoke pass.

Manual QA should verify:

- Visible game launch reaches the main menu without crash.
- Keyboard/mouse input responds in the main menu and shell UI.
- Hub scene is reachable and does not regress after resource integration.
- Chart/departure flow remains usable enough to enter the intended route loop.
- Exploration return path can hand resources back to inventory/storage when the runtime loop is available.
- Resource inventory/storage/cargo presentation shows quantities and capacity states coherently when connected by downstream UI.
- Repair deposit UI path can consume resources atomically when wired to the runtime interaction.
- Save/load runtime path preserves the resource progress snapshot.
- No visible frame-rate drops are introduced in the main menu, Hub, or resource-facing screens.
- No obvious memory growth over a 5-minute Hub -> Exploration -> Hub observation loop.

---

### Smoke Test Scope

Use `tests/smoke/critical-paths.md` as the base checklist.

Current smoke status from `production/qa/smoke-2026-05-13.md`: **PASS WITH WARNINGS**.

Warnings carried into manual QA:

- No QA plan existed before this document.
- Visible runtime smoke checklist was not fully executed in the headless pass.
- Manual QA must verify menu/input, Hub -> Chart -> Exploration -> Return, resource inventory presentation, repair deposit UI, and frame-rate/memory observations.

---

### Out Of Scope

- Market pricing, market inventory refresh, and economy balancing.
- Combat timing, threat consequence tuning, and in-combat UI.
- Exploration loot generation design beyond resource intake/return boundaries.
- Final resource HUD visual polish, icon art, sound effects, and animation polish.
- Console, mobile, web, gamepad, and touch-specific testing.
- Full release certification, localization pass, and accessibility certification.

---

### Entry Criteria

QA can begin when all of the following are true:

- Epic #5 stories are marked Complete.
- Smoke check is PASS or PASS WITH WARNINGS.
- Build passes with 0 errors.
- Required automated tests pass.
- Manual QA warnings are listed in this QA plan.

Current entry status: **MET WITH WARNINGS**.

---

### Exit Criteria

QA cycle is complete when:

- All automated story evidence remains PASS.
- Manual runtime checks are marked PASS, PASS WITH NOTES, FAIL, or BLOCKED.
- Any FAIL or BLOCKED result has a bug report or explicit deferral note.
- QA sign-off report is written to `production/qa/qa-signoff-resources-goods-capacity-2026-05-13.md`.

---

### Manual QA Sessions

**Session 1 - Runtime Critical Path**

Focus: visible startup, input, Hub, Chart, departure, exploration return, resource presentation, repair deposit UI.

Expected duration: 45-60 minutes.

**Session 2 - Regression And Stability**

Focus: save/load observation, repeated Hub -> Exploration -> Hub loop, frame-rate target, 5-minute memory-growth observation, notes capture.

Expected duration: 30-45 minutes.

---

### QA Verdict Before Execution

**Ready for manual QA with warnings.**

There are no story-level blockers. The remaining warnings are runtime/manual validation items that should be captured in the sign-off report.
