## Consistency Check Report
Date: 2026-04-30
Registry entries checked: 0 entities, 0 items, 0 formulas, 0 constants (registry is empty)
GDDs scanned: 6 (content-data-state-registry, platform-session-shell, local-save-world-state-persistence, player-movement-interaction, resources-goods-capacity, player-knowledge-intel)

> **Note**: `design/registry/entities.yaml` is empty — no cross-system entities, items, formulas, or constants have been registered yet. This report is based on direct cross-GDD dependency and reference analysis.

---

### Conflicts Found (must resolve before architecture)

🔴 **player-movement-interaction.md ↔ player-knowledge-intel.md — Bidirectional Reference Gap**
   Intel GDD (`player-knowledge-intel.md`) requires `player_arrived_at(location_id)` event from the movement system (lines 180, 797, 858, 1013-1015).
   Movement GDD (`player-movement-interaction.md`) does NOT define `player_arrived_at`, does NOT reference the intel system in its Interactions table (lines 100-112) or downstream contracts (lines 455-462).
   The intel GDD itself documents this gap at line 800: "注意——双向引用缺口：当前版本的移动系统 GDD 尚未包含 player_arrived_at 事件或对本系统的引用。"
   → **Resolution**: Either add `player_arrived_at` event to the movement GDD (Section 3 Interactions table + downstream contract), or provide an equivalent arrival event in the exploration GDD (`探索/搜撤场景`) and update the intel GDD's dependency to point there instead.

---

### Misalignments (one-side claims alignment that isn't fully true)

⚠️ **resources-goods-capacity.md bidirectional check claims movement GDD lists it**
   Resources GDD (`resources-goods-capacity.md`) line 558: "玩家移动与交互 | ✅ Interactions 中列出本系统为下游 | 已对齐"
   But movement GDD (`player-movement-interaction.md`) Interactions table (lines 100-112) and downstream contracts (lines 455-462) do NOT list `资源、货物与容量` as a downstream system.
   The relationship is implicit: movement broadcasts `use_requested`, and resources can consume it for storage/pickup/cargo interactions. This is architecturally sound but not explicitly documented in movement GDD.
   → **Resolution**: Either add `资源、货物与容量` to the movement GDD's Interactions table (as a system that receives `use_requested`), or correct the resources GDD's bidirectional check to note "implicit via use_requested delegation, not explicitly listed in movement GDD."

---

### Verified Alignments (bidirectional references confirmed correct)

✅ **content-data-state-registry ↔ resources-goods-capacity**: Registry lists resources as downstream (line 549); resources lists registry as upstream (line 514). Aligned.

✅ **content-data-state-registry ↔ player-knowledge-intel**: Registry lists intel as downstream (line 550); intel lists registry as upstream (line 777-781). Aligned.

✅ **local-save-world-state-persistence ↔ resources-goods-capacity**: Persistence lists resources (line 444); resources lists persistence (line 515). Snapshot domain `progress.resources` consistent across both GDDs. Aligned.

✅ **local-save-world-state-persistence ↔ player-knowledge-intel**: Persistence lists intel (line 445); intel lists persistence (line 784-787). Snapshot domain `progress.intel` consistent across both GDDs. Aligned.

✅ **local-save-world-state-persistence ↔ platform-session-shell**: Persistence lists platform shell (line 442); platform shell lists persistence (line 272). Continue state API ownership clearly assigned to persistence. Aligned.

✅ **player-knowledge-intel ↔ resources-goods-capacity**: Intel lists resources as upstream for `consume_intel()` (line 789-794); resources lists intel as downstream for intel item filtering and consumption (lines 528-529). `consume_intel()` ownership clearly assigned to intel system. Aligned.

---

### Registry Status

⚠️ **Entity registry is empty.** All four sections (entities, items, formulas, constants) have zero entries. This means no cross-system facts have been formally registered. Several facts SHOULD be registered:

**Recommended registry entries:**

| Type | Name | Source | Cross-referenced In |
|------|------|--------|---------------------|
| domain | `progress.resources` | local-save-world-state-persistence.md | resources-goods-capacity.md |
| domain | `progress.intel` | local-save-world-state-persistence.md | player-knowledge-intel.md |
| domain | `progress.airship` | local-save-world-state-persistence.md | (飞艇家园 Hub — not yet designed) |
| constant | `carry_base_slots` | resources-goods-capacity.md | value: 5, unit: slots |
| constant | `max_stack_basic` | resources-goods-capacity.md | value: 99 |
| constant | `max_stack_intel` | resources-goods-capacity.md | value: 1 |

---

### Unverifiable References (no conflict, informational)

ℹ️ All 6 approved GDDs reference 12+ systems that are not yet designed. These forward references are expected and not conflicts — they become testable when those GDDs are authored.

ℹ️ `content-data-state-registry.md` notes at line 542: "实现阶段可能需要平台加载能力，但这属于后续架构/ADR，不在本 GDD 中把 `平台与会话壳` 设为设计依赖。" This is an explicit decision, not a gap.

---

### Clean Entries (no issues found)

✅ 4/6 GDDs have complete and correct bidirectional references within the approved set.
✅ All snapshot domain IDs (`progress.resources`, `progress.intel`) are consistent across GDDs.
✅ All ownership boundaries are clearly assigned with no conflicts.
✅ `consume_intel()` ownership is correctly assigned to intel system with clear resource system contract.

---

Verdict: **RESOLVED** — both issues fixed 2026-04-30

### Resolution Log

🔴 → ✅ **player-movement-interaction.md ↔ player-knowledge-intel.md**: Added `玩家知识与情报` row to movement GDD's Interactions table (line 109) and downstream contracts (line 462), defining `player_arrived_at(location_id)` event. Updated intel GDD line 800 to confirm bidirectional reference established.

⚠️ → ✅ **resources-goods-capacity.md bidirectional check**: Corrected line 558 to accurately note implicit alignment via `use_requested` delegation rather than claiming explicit listing in movement GDD's Interactions table.
