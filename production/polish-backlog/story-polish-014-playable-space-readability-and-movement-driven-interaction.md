# Polish Story 014: Playable Space Readability and Movement-Driven Interaction

> **Phase**: Polish
> **Status**: Implemented -- Human QA Concern
> **Layer**: Godot Runtime Presentation / Playable Interaction
> **Type**: Polish
> **Estimate**: M / 1 day
> **Governing ADRs**: ADR-0012 UI Input Routing, ADR-0016 Feedback/VFX/Audio Semantics, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 013 human long-session release readiness triage

## Context

Polish Story 013 proved that the current playable slice is stable and that
save/load, cross-launch restore, overwrite, delete, quarantine, and repeated
route/search/return cycles are trustworthy enough for continued Polish work.

The human triage did not approve moving directly to a formal release checklist.
The blocker is presentation/gameplay readability: the tester could not identify
the Hub as a station or find cockpit/cargo/engine-room spaces, Exploration has
no meaningful image/art treatment, and the route/search loop can be completed by
clicking through UI with little reason to move.

This story is a narrow release-readiness blocker fix. It is not final art, full
content expansion, named saves, controller support, or a complete encounter
system.

## Acceptance Criteria

- [ ] GIVEN the Hub starts, WHEN the player enters the playable space, THEN cockpit/helm, cargo/storage, and engine/module areas are visually distinct without relying only on labels.
- [ ] GIVEN the player moves through Hub, WHEN they approach each major area, THEN room boundaries and affordances make the space read as a ship/station rather than a flat UI backdrop.
- [ ] GIVEN Exploration starts, WHEN the player views the scene, THEN the island/search/return space has visible authored landmarks and no longer reads as an empty/static image.
- [ ] GIVEN the player wants to search, WHEN they are not near the search landmark, THEN search cannot be completed purely from UI without moving into the interaction area.
- [ ] GIVEN the player wants to return, WHEN they are not near the return landmark, THEN return cannot be completed purely from UI without moving into the interaction area.
- [ ] GIVEN onboarding hints are active, WHEN the player follows the route/search loop, THEN hints support movement-driven interaction without stealing focus.
- [ ] GIVEN existing smoke probes run, WHEN the new presentation/interaction constraints are present, THEN visual, durable persistence, long-session, and perf smoke remain passing or documented with non-blocking notes.
- [ ] GIVEN human QA reruns the focused route/search loop, WHEN the tester evaluates the release-readiness blocker, THEN Hub place identity, Exploration readability, and movement-driven play are no longer blockers.

## Implementation Notes

- Prefer authored greybox geometry, color/material separation, spatial markers,
  and small readable props over broad final-art production.
- Keep `HubRuntime.cs` as the runtime authority for generated Godot nodes unless
  an existing scene asset is already the local pattern.
- Do not introduce parallel gameplay state; derive labels and dynamic visual
  states from existing `PlayableSliceDomainAdapter` snapshots.
- Keep Chart/HUD UI available for status and commands, but make core search and
  return progression depend on spatial proximity / movement interaction.
- Preserve keyboard-only play.

## Evidence Targets

- [x] Updated `tests/smoke/session_shell_visual_probe.gd` coverage for distinct Hub
  room identity, Exploration landmarks, and movement-required search/return.
- [x] Updated or new manual focused checklist under `production/playtests/`.
- [x] Evidence file under `production/qa/evidence/`.

## Implementation Summary

- Added stronger Hub greybox identity: station deck title, brighter cockpit/helm
  glow, cargo/storage glow, engine/module glow, room-specific labels, and a
  movement cue that points players through the authored spaces.
- Added stronger Exploration greybox identity: island mass, cliff edge, path
  steps, search wreck mast/signal, return beacon beam, and island identity text.
- Changed Exploration search and return actions so UI buttons and direct action
  calls require the player to stand near the search wreck or return beacon.
- Kept keyboard-only play intact: `E` still performs the nearest valid spatial
  interaction, while disabled UI labels explain that the player must move close
  first.

## Automated Evidence

- Evidence file: `production/qa/evidence/polish-014-playable-space-readability-and-movement-driven-interaction-evidence.md`
- Manual focused checklist: `production/playtests/playtest-checklist-polish-014-readable-space-and-movement-2026-05-23.md`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`: PASS, 0 warnings / 0 errors.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`: PASS.
- `godot --headless --path . -s tests/smoke/session_shell_durable_persistence_probe.gd`: PASS.
- `godot --headless --path . -s tests/smoke/session_shell_long_session_probe.gd`: PASS.
- `godot --headless --path . -s tests/smoke/session_shell_perf_probe.gd`: PASS; draw-call budget skipped under headless display driver.

## Human QA Boundary

Automated checks prove the presentation nodes exist, the movement/proximity
gates work, and persistence/performance smoke still pass. The final release
blocker remains human-readable: a tester must confirm the Hub now reads as a
station/interior, Exploration no longer feels empty, and the search/return loop
now requires meaningful movement.

## Human QA Result

- Checklist: `production/playtests/playtest-checklist-polish-014-readable-space-and-movement-2026-05-23.md`
- Verdict: CONCERN.
- Tester: liein.
- Build/Commit: `ecb1fe6`.
- Duration: 10 minutes.
- Technical stability passed, but the release-readiness blocker remains.
- The current greybox still does not read as the intended island/docked-ship
  scene or as an enterable ship interior.
- Search has movement gating, but not designed search gameplay.
- Return should become a ship/piloting return flow rather than only a beacon.
- Next blocking story: `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`.

## Release Triage Rule

Do not run a formal release checklist/gate until this story is complete or the
release-readiness blocker is explicitly waived by the user.
