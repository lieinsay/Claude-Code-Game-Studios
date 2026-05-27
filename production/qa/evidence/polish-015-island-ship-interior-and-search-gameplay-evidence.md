# Polish 015 Island / Ship Interior and Search Gameplay Evidence

**Date:** 2026-05-23  
**Story:** `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`  
**Status:** PASS -- Human verified / closed
**Purpose:** Verify the structural scene and gameplay blocker fix before focused human rerun.

This evidence does not establish Release readiness. It records that the new
island/ship structure, search micro-game, return piloting flow, and existing
smoke paths are technically healthy and that the focused BUG-003 / Polish 015
blocker has been manually accepted for closure.

## Commands

| Command | Result | Notes |
|---------|--------|-------|
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | Existing warnings, 0 errors. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Covers island exterior, boarding, ship interior topology, helm-owned Chart, three-step search, two-step return, and save/load restore. |
| `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd` | PASS | Durable save/load, overwrite, delete, quarantine, and recovery still pass after the new interaction gates. |
| `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd` | PASS | Three route/search/save/load/return cycles still pass with the expanded search and return flow. |
| `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd` | PASS | Frame p95, worst sampled frame, memory, save/load, route departure, and return transition budgets pass; draw-call budget unavailable under headless display driver. |
| `godot --display-driver windows --rendering-driver opengl3 --audio-driver Dummy --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Windowed visual smoke saved screenshots after BUG-003 fix. |

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

## BUG-003 Follow-up -- 2026-05-24

The first human QA rerun on build `80cf299` returned `BLOCKED`: the tester saw
mostly text, did not discover the island/dock scene, and could not identify the
boarding flow or ship-interior rooms. This exposed a smoke-test blind spot: the
test asserted scene nodes existed, but not that they dominated the visible
viewport.

Fix candidate:

- `HubRuntime` now renders the playable world layer above the text dashboard.
- Hub exterior has large viewport-scale sky, island mass, dock planks, docked
  ship hull, and airship envelope silhouettes.
- Ship interior has a large hull outline plus cockpit, cargo, and engine bays.
- Exploration has a large island body, pier, return ship hull/envelope, search
  wreck, and route/search/return landmarks.
- `tests/smoke/session_shell_visual_probe.gd` now asserts world-layer z-order
  and main-viewport scene area, so future smoke cannot pass from hidden nodes
  alone.

Windowed screenshot evidence:

- `production/qa/evidence/polish-015-fix-final-hub-probe.png`
- `production/qa/evidence/polish-015-fix-exploration-semantics-probe.png`

## BUG-003 Opaque World-Layer Fix -- 2026-05-27

The prior fix candidate still allowed the old text dashboard to show through
semi-transparent world surfaces. This made the scene more visible than before,
but it still read as text layered behind greybox art.

Second fix candidate:

- Hub, ship interior, chart, exploration, and Ochre debug scene primary world
  backdrops now use opaque main surfaces.
- Hub island walk layer, dock/ship hull, ship-interior shell, exploration
  island walk layer, and exploration return-ship silhouette now block Deck
  text bleed-through.
- `tests/smoke/session_shell_visual_probe.gd` now asserts the critical world
  surfaces are opaque enough to prevent the old dashboard from dominating.

Additional automated evidence:

- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`: PASS with existing warnings / 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`: PASS.
- `godot --path . -s tests/smoke/session_shell_visual_probe.gd`: PASS, screenshots refreshed.

Windowed screenshot evidence:

- `production/qa/evidence/polish-015-opaque-world-hub-probe.png`
- `production/qa/evidence/polish-015-opaque-world-exploration-probe.png`

## Manual QA Closure -- 2026-05-27

- Verdict: PASS / closed.
- Tester: liein.
- User confirmation: BUG-003 / Polish 015 can be closed after manual rerun.
- Closure scope: island with docked ship, enterable interior, recognizable ship
  rooms, search as a small gameplay beat, return as ship movement / piloting,
  and no old text-dashboard bleed-through dominating the world layer.
- Release boundary: This closes the focused Polish 015 blocker only; it is not a
  substitute for the later formal release checklist / gate.
