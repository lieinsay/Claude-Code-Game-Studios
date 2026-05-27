# Polish Story 015: Island / Ship Interior and Search Gameplay Design

> **Phase**: Polish
> **Status**: Closed -- Human Verified
> **Layer**: Scene Structure / Gameplay Design / Godot Runtime Presentation
> **Type**: Blocking Design + Implementation Story
> **Estimate**: L / 2-3 days
> **Governing ADRs**: ADR-0012 UI Input Routing, ADR-0016 Feedback/VFX/Audio Semantics, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 014 human focused rerun verdict CONCERN

## Context

Polish Story 014 added technical proximity gates and stronger greybox markers,
and automated smoke remained healthy. The focused human rerun still returned
CONCERN: the Hub does not read as the intended place, the player cannot identify
real room areas, Exploration still lacks gameplay beyond moving to a point, and
returning should feel like piloting or moving the ship back rather than touching
a beacon.

This means the blocker is now design-structural, not just presentation polish.
The next pass must define and implement a more coherent playable-space model:
an island where the ship can dock, an enterable ship interior, a search activity
with actual choices or timing, and a return flow that expresses ship movement.

## Acceptance Criteria

- [x] GIVEN the player enters the Hub area, WHEN they view the scene, THEN the
  space reads as an island/dock environment with a visible ship, not only a flat
  interior UI backdrop.
- [x] GIVEN the player approaches the ship, WHEN they interact, THEN they can
  enter a readable ship interior space.
- [x] GIVEN the player is inside the ship, WHEN they move through it, THEN
  cockpit/helm, cargo/storage, and engine/module spaces are distinct through
  topology, boundaries, and props, not only labels or color blocks.
- [x] GIVEN the player opens Chart, WHEN they do so, THEN the helm interaction
  still owns the Chart affordance and preserves focus/input isolation.
- [x] GIVEN Exploration starts, WHEN the player views the scene, THEN the search
  area has enough authored complexity to suggest an explorable place rather
  than an empty field.
- [x] GIVEN the player searches, WHEN they engage the search site, THEN the
  action involves a small playable interaction such as choosing a scan angle,
  timing a salvage pulse, or checking multiple nearby clues before reward
  settlement.
- [x] GIVEN the player wants to return, WHEN they complete the route, THEN the
  return interaction reads as moving or piloting the ship back, not only walking
  to a beacon.
- [x] GIVEN onboarding hints are active, WHEN the player moves between island,
  ship interior, Exploration, search gameplay, and return, THEN hints support
  the new structure without stealing focus.
- [x] GIVEN existing smoke probes run, WHEN the new scene structure is present,
  THEN visual, durable persistence, long-session, and perf smoke remain passing
  or document non-blocking notes.
- [x] GIVEN human QA reruns the focused checklist, WHEN they evaluate the same
  blockers, THEN Hub/ship identity, Exploration readability, search gameplay,
  and return flow are no longer CONCERN-level blockers.

## Design Notes

- Treat the previous Hub as too abstract. The desired mental model is:
  outdoor island/dock hub -> enter ship -> ship interior rooms -> helm/chart.
- The ship interior does not need final art, but it does need topology:
  connected rooms, visible doorways or thresholds, and clear spatial separation.
- Search gameplay should remain small enough for Polish scope. Prefer one
  reusable micro-interaction over broad content expansion.
- Keep keyboard-only play and existing save/load trust intact.
- Do not introduce a parallel gameplay authority. Existing C# domain managers
  remain canonical; new presentation/gameplay steps should bridge through
  existing runtime state where possible.

## Evidence Targets

- [x] Updated or new design note under `production/polish-backlog/` if the search
  micro-game needs explicit rules before implementation.
- [x] Updated `tests/smoke/session_shell_visual_probe.gd` coverage for island/dock,
  ship entry, interior topology, search micro-game affordance, and ship-return
  flow.
- [x] Automated evidence file under `production/qa/evidence/`.
- [x] Focused manual checklist under `production/playtests/`.

## Implementation Summary

- Split Hub presentation into an exterior island/dock state and an interior ship
  state.
- The default Hub now starts on a docked-island exterior with the visible ship,
  airship envelope, pier, ramp, and boarding interaction.
- The player can enter and leave the ship; ship interior visibility is separate
  from exterior visibility.
- Ship interior now contains connected cockpit/helm, cargo/storage, and
  engine/module areas with a shared corridor, door thresholds, and distinct
  props.
- Chart access is gated to the interior helm path; direct Chart activation from
  the island exterior gives feedback instead of opening the panel.
- Search now requires a three-step micro-interaction: scan calibration, echo
  lock, and salvage pulse before the domain search/reward state advances.
- Return now requires a ship-based two-step interaction: preheat return engines,
  then pilot back to Hub.

## Automated Evidence

- Design note: `production/polish-backlog/story-polish-015-search-return-microgame-design-note.md`
- Evidence file: `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`
- Human QA checklist: `production/playtests/playtest-checklist-polish-015-island-ship-search-gameplay-2026-05-23.md`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`: PASS with existing warnings / 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`: PASS.
- `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd`: PASS.
- `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd`: PASS.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`: PASS; draw-call budget skipped under headless display driver.

## Human QA Boundary

Automated checks prove that the island/dock, ship entry, interior topology,
search micro-game, return preheat/piloting flow, save/load, long-session, and
performance paths are technically healthy. Human QA must still decide whether
the scene now reads as the intended island + ship fantasy and whether the
search/return loops are sufficiently playable for the release-readiness blocker.

## Human QA Rerun 1 -- 2026-05-24

- Verdict: BLOCKED.
- Tester: liein.
- Build/commit: `80cf299`.
- Result: launch passed, but Hub did not read as an island/dock; boarding and
  ship interior appeared text-only; cockpit/helm, cargo/storage, and
  engine/module spaces could not be distinguished; all downstream checks were
  blocked.
- Bug filed: `production/qa/bugs/BUG-003-polish-015-world-scene-hidden-by-text-dashboard.md`.

## BUG-003 Fix Candidate -- 2026-05-24

- `HubRuntime` now renders the playable scene layer above the text dashboard
  instead of allowing the HUD panel to dominate the viewport.
- Added large island, dock, ship hull, airship envelope, and sea/sky silhouettes
  to the Hub exterior.
- Added large hull outline plus cockpit, cargo, and engine bay zones to the ship
  interior.
- Added large Exploration island, pier, return ship, and search/return landmark
  silhouettes.
- Visual smoke now asserts world-layer z-order and main-viewport scene area, not
  only node existence.
- Windowed screenshot evidence:
  `production/qa/evidence/polish-015-fix-final-hub-probe.png` and
  `production/qa/evidence/polish-015-fix-exploration-semantics-probe.png`.
- Durable persistence, long-session, and perf smoke were rerun after the fix
  candidate and passed.

This is not a release-readiness pass. The focused Polish 015 checklist still
needs a human rerun.

## BUG-003 Opaque World-Layer Follow-up -- 2026-05-27

- The 2026-05-24 fix candidate still allowed old dashboard text to bleed
  through semi-transparent world surfaces.
- Hub, ship interior, chart, exploration, and Ochre debug scene primary
  backdrops now use opaque main world surfaces.
- Hub island walk layer, dock/ship hull, ship-interior shell, exploration
  island walk layer, and exploration return-ship silhouette now block Deck text
  bleed-through.
- Visual smoke now asserts opacity for critical scene evidence, in addition to
  world-layer z-order and viewport area.
- Windowed screenshot evidence:
  `production/qa/evidence/polish-015-opaque-world-hub-probe.png` and
  `production/qa/evidence/polish-015-opaque-world-exploration-probe.png`.

## Human QA Rerun 2 -- 2026-05-27

- Verdict: PASS / blocker closed.
- Tester: liein.
- Result: After the opaque world-layer follow-up and manual rerun, the user
  confirmed BUG-003 / Polish 015 can be closed.
- Closed bug: `production/qa/bugs/BUG-003-polish-015-world-scene-hidden-by-text-dashboard.md`.
- Remaining release boundary: This closes the Polish 015 blocker, but does not
  by itself replace the later formal release checklist / gate.

## Release Triage Rule

Polish 015 is complete. A later formal release checklist / gate should use this
story as closed evidence, not as an open blocker.
