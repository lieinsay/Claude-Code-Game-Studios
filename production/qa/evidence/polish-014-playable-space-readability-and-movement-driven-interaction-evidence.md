# Polish 014 Playable Space Readability and Movement-Driven Interaction Evidence

**Date:** 2026-05-23  
**Story:** `production/polish-backlog/story-polish-014-playable-space-readability-and-movement-driven-interaction.md`  
**Status:** PASS -- Awaiting Human QA  
**Purpose:** Verify the release-readiness blocker fix before focused human rerun.

This evidence does not establish Release readiness. It only records that the
implemented greybox readability and movement-gating changes are technically
healthy enough for a focused human playtest.

## Change Summary

- Hub now has stronger station/interior readability: cockpit/helm, cargo/storage,
  and engine/module areas are highlighted with separate authored geometry,
  glow bands, room labels, and a deck identity label.
- Exploration now has visible authored landmarks: island mass, cliff edge, path
  steps, search wreck mast/signal, return beacon beam, and island identity text.
- Exploration search and return now require proximity to the relevant landmark.
  The UI buttons disable and explain the movement requirement when the player is
  too far away.
- Keyboard-only spatial interaction remains supported through the existing `E`
  interaction path.

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 0 warnings, 0 errors. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Covers Hub room identity nodes, Exploration landmarks, and movement-required search/return flow. |
| `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd` | PASS | Durable save/load, overwrite, delete, quarantine, and recovery still pass after movement-gating changes. |
| `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd` | PASS | Three route/search/save/load/return cycles plus final latest-state load still pass. |
| `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd` | PASS | Frame p95, worst sampled frame, memory, save/load, route departure, and return transition budgets pass; draw-call budget unavailable under headless display driver. |

## Test Isolation Note

The durable persistence and long-session probes share `user://` durable progress
state and should be run sequentially. A parallel run can create false failures
because one probe may clear or overwrite progress while the other is checking it.
Sequential reruns passed.

## Manual QA Boundary

Focused human QA is still required. The next tester should verify whether the
new greybox treatment actually resolves the subjective blockers from Polish 013:
Hub place identity, Exploration readability, and movement-driven play.
