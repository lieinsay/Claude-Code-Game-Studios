# Polish Story 004: Authored Content Validation Guard

> **Phase**: Polish
> **Status**: Complete
> **Layer**: Presentation / Content Integration / QA
> **Type**: Polish
> **Estimate**: S / 0.5 day
> **Governing ADRs**: ADR-0001 Content Registry, ADR-0010 Navigation Route Risk, ADR-0013 Exploration Scavenge, ADR-0019 Desktop Godot .NET/C# Platform Pivot
> **Unlocked By**: Polish Story 003 Authored Route / Search Content Slice

## Context

Polish Story 003 moved the playable route/search data into an authored MVP
content slice. The next risk is that future edits to that JSON could silently
break route IDs, display text, reward ranges, or threat metadata before a human
ever reaches the Godot smoke path.

This story adds a lightweight validation guard to the existing playable-slice
integration runner. It does not create a full content authoring pipeline and
does not make GDScript authoritative. It simply turns basic authored content
structure into automated evidence.

## Acceptance Criteria

- [x] GIVEN the playable-slice integration runner starts, WHEN it reads authored content, THEN it verifies the authored content file exists at the expected C# runtime path.
- [x] GIVEN content metadata is present, WHEN the runner validates it, THEN `content_version`, `content_status`, origin, cargo capacity, and voyage timing are checked.
- [x] GIVEN route rows are present, WHEN validation runs, THEN route IDs, destinations, display names, descriptions, distance bands, and hazard tags are required and route IDs must be unique.
- [x] GIVEN search-point rows are present, WHEN validation runs, THEN search IDs, display names, descriptions, zones, resource rewards, quantity ranges, and threat fields are required or range-checked as appropriate.
- [x] GIVEN the validation guard passes, WHEN docs and QA evidence are read, THEN remaining risk is explicitly scoped to content scale-up, migration strategy, final art/audio, and longer play sessions rather than hidden JSON structure drift.

## Implementation Notes

- Keep validation inside `tests/integration/playable-slice/DomainAdapterProgram.cs` for now so it travels with the adapter regression.
- Use `System.Text.Json`; do not add a dependency or a separate content tool yet.
- This is a Polish guardrail, not a final authoring UI, schema package, or migration framework.
- Route/search IDs remain persistence anchors and must not be renamed without migration planning.

## Completion Notes

- Completed 2026-05-22.
- `DomainAdapterTest.csproj` now validates authored content before exercising Chart/Hub/Navigation/Exploration/Resources/Hull/Persistence.
- Focused integration evidence increased from 43 checks to 101 checks.
- Remaining scope is not blocking for this story: final content scale-up, route/search ID migration tooling, final authored art/audio, and long manual play evidence remain downstream Polish backlog.
