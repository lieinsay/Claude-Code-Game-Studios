# Polish Story 005: Route / Search ID Migration Guard

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Content Integration / Persistence
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0003 Save/Load, ADR-0001 Content Registry, ADR-0010 Navigation Route Risk, ADR-0013 Exploration Scavenge, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 004 Authored Content Validation Guard

## Context

Polish Story 004 made authored route/search content structure visible to
automated evidence. The next persistence risk is ID drift: `route_id` and
`search_point_id` values are now save anchors, so future renames or removals
need a migration path before content changes land.

This story adds a small ID migration guard inside the authored content slice and
teaches the C# adapter to resolve legacy route/search IDs to current authored
IDs at runtime boundaries.

## Acceptance Criteria

- [x] GIVEN authored content is loaded, WHEN validation runs, THEN route/search ID migration maps are explicit and checked for unique sources, non-active sources, active targets, and recorded reasons.
- [x] GIVEN an old route ID is selected, WHEN the adapter receives it, THEN it resolves to the current route ID before calling ChartManager.
- [x] GIVEN scene-local persistence contains an old route ID, WHEN canonical progress is loaded, THEN `PlayableSliceDomainAdapter` restores the current route ID.
- [x] GIVEN an old search-point ID is present in a playable slice payload, WHEN it is restored, THEN the adapter resolves it before deriving display text.
- [x] GIVEN docs and evidence are read, WHEN remaining risks are assessed, THEN route/search renames are no longer hidden-risk work; future content scale-up and final content/art decisions remain downstream Polish scope.

## Implementation Notes

- Keep migration data in `src/presentation/playable_slice_authored_content.json` near the authored content it protects.
- Keep migration resolution in `PlayableSliceDomainAdapter`; do not add GDScript runtime authority.
- This is not a full save migration framework. It is a local guard for current playable route/search IDs until a broader content pipeline exists.
- New ID removals or renames must add migration entries before deleting old IDs from authored content.

## Completion Notes

- Completed 2026-05-22.
- Added `id_migrations.route_ids` and `id_migrations.search_point_ids` to the authored content slice.
- `PlayableSliceDomainAdapter` now resolves legacy route IDs for route selection, route display lookup, save-scene normalization, and playable slice restore.
- `PlayableSliceDomainAdapter` resolves legacy search-point IDs during playable slice restore and display-name fallback.
- Focused integration evidence is now `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` PASS 118/118.
- Remaining scope is not blocking for this story: content scale-up, final authored route/search design, final art/audio treatment, and long manual play evidence remain downstream Polish backlog.
