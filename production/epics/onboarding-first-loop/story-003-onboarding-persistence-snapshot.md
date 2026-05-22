# Story 003: Onboarding Persistence Snapshot

> **Epic**: Onboarding and First Loop
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: S / 4-6 hours
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/onboarding-first-loop.md`
**Requirement**: `TR-onboarding-001`

**ADR Governing Implementation**: ADR-0017: Onboarding and First Loop Guidance; ADR-0003: Local Save and World State Persistence
**ADR Decision Summary**: #18 exports `progress.onboarding` as plain snapshot data. Persistence validates/promotes the package; completed hints must not replay after save/load.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Persistence logic should be headless C# and must not serialize Godot `Node`, `Control`, `Resource`, or runtime object references.

**Control Manifest Rules (Foundation/Persistence layer)**:
- Required: save/load uses Canonical JSON via `SnapshotPackage`; staging -> verify -> promotion remains intact.
- Forbidden: never include `Node`, `Resource`, `Object`, `Callable`, or `RID` references in snapshots.
- Guardrail: save encode + SHA-256 p95 <50ms for a 2MB snapshot.

---

## Acceptance Criteria

*From GDD `design/gdd/onboarding-first-loop.md`, scoped to this story:*

- [x] GIVEN onboarding has completed steps, WHEN progress is saved, THEN `progress.onboarding` contains stable completed step IDs, suppressed step IDs, `first_loop_complete`, and schema version.
- [x] GIVEN progress is loaded, WHEN completed steps are restored, THEN completed hints do not repeat unless onboarding state is reset.
- [x] GIVEN save/load restores mid-loop onboarding state, WHEN the next eligible hint is evaluated, THEN completed steps stay complete and only incomplete steps can become eligible.
- [x] GIVEN malformed or unknown persisted step IDs are loaded, WHEN the package is restored, THEN valid known steps restore and invalid data is diagnosed without crashing.
- [x] GIVEN onboarding preferences are reset or disabled in future settings scope, WHEN progress remains intact, THEN preference state is kept separate from `progress.onboarding`.

---

## Implementation Notes

Derived from ADR-0017 and ADR-0003:

- Register a `progress.onboarding` snapshot provider with the existing Persistence pipeline.
- Persist only plain data: stable string IDs, booleans, integers, and schema version.
- Keep player preferences such as reset/disable out of progress; reserve `settings.onboarding` for future settings scope.
- Validate schema version and unknown step IDs defensively.
- Restore completion before evaluating hints after load.
- Do not persist active hint Control references, focus state, mouse state, or localized display text.

---

## Out of Scope

- Story 004 renders hints.
- Story 005 validates full runtime smoke and manual QA.
- Future settings UI for disabling/resetting onboarding.

---

## QA Test Cases

- **AC-1**: Snapshot contains required fields
  - Given: completed and suppressed onboarding steps
  - When: progress is saved
  - Then: `progress.onboarding` includes completed IDs, suppressed IDs, `first_loop_complete`, and schema version
  - Edge cases: no completed steps, all steps complete, duplicate IDs

- **AC-2**: Completed hints do not repeat after load
  - Given: a saved progress package with `open_chart` completed
  - When: progress is loaded and hints are evaluated
  - Then: `open_chart` does not become visible
  - Edge cases: reset request, suppressed-but-not-completed step, schema migration

- **AC-3**: Mid-loop restore preserves next eligible state
  - Given: steps through `depart_route` are complete and later steps incomplete
  - When: progress is saved and loaded
  - Then: `advance_pressure` is the next eligible guidance target
  - Edge cases: missing intermediate step, out-of-order persisted list

- **AC-4**: Malformed data is diagnosed safely
  - Given: an onboarding snapshot with unknown step IDs or invalid schema
  - When: restore is attempted
  - Then: known valid steps restore, invalid fields are diagnosed, and no crash occurs
  - Edge cases: null payload, wrong field type, future schema version

- **AC-5**: Preferences remain separate
  - Given: progress save/load runs
  - When: snapshot packages are inspected
  - Then: reset/disable preference data is not stored in `progress.onboarding`
  - Edge cases: future `settings.onboarding` absent, settings artifact corrupt

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/onboarding-first-loop/PersistenceSnapshotTest.csproj` -- must exist and pass

**Status**: [x] Created and passing

**Evidence**:
- `dotnet run --project tests/integration/onboarding-first-loop/PersistenceSnapshotTest.csproj` -- PASS, 6/6 checks.
- `dotnet run --project tests/unit/onboarding-first-loop/StepStateHintScoringTest.csproj` -- PASS, 6/6 checks.
- `dotnet run --project tests/integration/onboarding-first-loop/EventIntegrationTest.csproj` -- PASS, 7/7 checks.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` -- PASS, 5 existing warnings, 0 errors.

---

## Dependencies

- Depends on: Story 001, Story 002
- Unlocks: Story 005

## Completion Notes

- `OnboardingManager` now exports `progress.onboarding` through a canonical `SnapshotPackage` and registers with `Persistence`.
- Snapshot payload includes schema version, stable completed step IDs, stable suppressed step IDs, `first_loop_complete`, and completion generation using only JSON-compatible data.
- Restore is defensive: known valid step IDs restore, unknown IDs and malformed fields are diagnosed in `LastRestoreDiagnostics`, and no Godot object references or preferences are persisted.
- Completed hints remain complete after load, and mid-loop restore evaluates the next incomplete step as the eligible target.
