# Polish Story 003: Authored Route / Search Content Slice

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Content Integration
> **Type**: Polish
> **Estimate**: S / 0.5-1 day
> **Governing ADRs**: ADR-0010 Navigation Route Risk, ADR-0013 Exploration Scavenge, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 002 Richer Exploration Scene Semantics

## Context

Polish Story 001 moved playable route/search data out of adapter code into a
JSON data boundary. Polish Story 002 made the Exploration scene read runtime
semantics from C# snapshots. The remaining near-term content risk is that the
data boundary is still named and treated like an MVP fixture.

This story turns the current playable route/search data into a small authored
content slice with explicit content version/status, route descriptions, and
search-point display text while keeping manager authority unchanged.

## Acceptance Criteria

- [x] GIVEN the playable adapter initializes, WHEN content is loaded, THEN the runtime snapshot exposes an authored content version and status.
- [x] GIVEN Chart opens, WHEN visible routes are reported, THEN route display names still come from authored route data.
- [x] GIVEN Exploration search advances, WHEN the first search is recorded, THEN the runtime snapshot exposes the authored search-point display name as well as the stable search-point ID.
- [x] GIVEN the Godot smoke reaches Exploration, WHEN semantic labels update, THEN the scene uses authored search-point text while preserving manager-owned search/threat/cargo/hull state.
- [x] GIVEN docs are updated, WHEN Polish backlog is read, THEN old MVP fixture wording is replaced with authored content slice wording and no Release readiness claim is made.

## Implementation Notes

- Keep `PlayableSliceDomainAdapter` as the only bridge into C# managers.
- Keep authored content data as a small JSON slice for this story; do not build the final full content pipeline.
- Do not add GDScript runtime authority.
- Keep stable route/search IDs unchanged for persistence and smoke continuity.

## Completion Notes

- Completed 2026-05-22.
- Replaced `src/presentation/playable_slice_runtime_fixture.json` with `src/presentation/playable_slice_authored_content.json`.
- Authored content now includes `content_version`, `content_status`, route descriptions, and search-point display/description text.
- `PlayableSliceDomainAdapter.Snapshot` exposes content version/status and `LastSearchPointName`.
- `HubRuntime.DebugDomainSnapshot()` exposes authored content metadata for smoke and QA diagnostics.
- Godot smoke now asserts content version/status and verifies Exploration semantic labels use authored search-point text.
- Remaining Polish boundary: this is an authored MVP content slice, not the final route/search content pipeline or final authored Exploration art.

