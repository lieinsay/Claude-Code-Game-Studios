# Polish 015 Island / Ship Interior and Search Gameplay Evidence

**Date:** 2026-05-23  
**Story:** `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`  
**Status:** PASS -- Awaiting Human QA  
**Purpose:** Verify the structural scene and gameplay blocker fix before focused human rerun.

This evidence does not establish Release readiness. It records that the new
island/ship structure, search micro-game, return piloting flow, and existing
smoke paths are technically healthy enough for human judgment.

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 5 existing warnings, 0 errors. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Covers island exterior, boarding, ship interior topology, helm-owned Chart, three-step search, two-step return, and save/load restore. |
| `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd` | PASS | Durable save/load, overwrite, delete, quarantine, and recovery still pass after the new interaction gates. |
| `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd` | PASS | Three route/search/save/load/return cycles still pass with the expanded search and return flow. |
| `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd` | PASS | Frame p95, worst sampled frame, memory, save/load, route departure, and return transition budgets pass; draw-call budget unavailable under headless display driver. |

## Coverage Notes

- Hub starts in an island/dock exterior with a visible docked ship, airship
  envelope, pier, and boarding ramp.
- Boarding moves the player into a separate ship interior state.
- Ship interior contains cockpit/helm, cargo/storage, and engine/module areas
  connected by a corridor and separated by thresholds.
- Chart opens from the interior helm interaction path and remains focus-isolated.
- Search requires scan calibration, echo lock, and salvage pulse before rewards
  and pressure state advance.
- Return requires engine preheat before piloting the ship back to the island dock.

## Manual QA Boundary

Focused human QA must decide whether the current greybox now satisfies the
intended fantasy and blocker: island with docked ship, enterable interior,
recognizable ship rooms, search as a small gameplay beat, and return as a ship
movement/piloting action.
