# Sprint 001 -- 2026-05-09 to 2026-05-16

## Sprint Goal

Turn the project contract from Web/GDScript to desktop Godot .NET/C# and prove the new direction with a Foundation C# spike.

## Capacity

- Total days: 7
- Buffer (20%): 1.5 days reserved for toolchain and migration surprises
- Available: 5.5 days

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S001-01 | Generate and verify Godot .NET project files | godot-csharp-specialist | 0.5 | ADR-0019 | `.csproj` and `.sln` are tracked; `dotnet build` or documented Godot .NET build command succeeds locally. |
| S001-02 | Migrate `SnapshotPackage` to C# | godot-csharp-specialist | 1.0 | S001-01 | C# `SnapshotPackage` supports deterministic `ToDictionary` / restore parity with existing GDScript behavior. |
| S001-03 | Migrate `Registry` to C# | godot-csharp-specialist | 1.0 | S001-01, S001-02 | Registry bootstrap data loads; core query cases match existing `test_registry_query.gd` expectations. |
| S001-04 | Migrate `Persistence` to desktop C# | godot-csharp-specialist | 1.5 | S001-01, S001-02 | Save/load roundtrip passes; Web emergency-save behavior is removed or isolated as legacy. |
| S001-05 | Add C# build/validation command documentation | devops-engineer | 0.5 | S001-01 | `tests/README.md` or equivalent documents the C# build and validation route. |
| S001-06 | Refresh Sprint 1 implementation assumptions | producer | 1.0 | ADR-0019 | Production status and first-loop story notes no longer require Web/GDScript for active development. |

Progress 2026-05-09: active design contracts updated for the desktop C# pivot (`systems-index`, `game-concept`, platform shell, local save/persistence, UI/HUD, accessibility, art bible, Hub UX, and affected GDDs with Web lifecycle/storage/performance assumptions). Remaining work is to sweep production epic/story notes for Web/GDScript-only assumptions before marking S001-06 done.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S001-07 | Add C# parity checks for Foundation spike | test-engineer | 1.0 | S001-02, S001-03, S001-04 | Registry and persistence parity checks run without relying on the old GDScript runner. |
| S001-08 | Decide renderer default for desktop MVP | godot-specialist | 0.5 | S001-01 | Compatibility vs Forward+ decision is recorded in technical preferences or a follow-up ADR note. |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S001-09 | Archive or label legacy GDScript prototype | lead-programmer | 0.5 | S001-07 | Existing `.gd` files are clearly marked as legacy prototype or queued for migration. |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| P3 architecture prototype | Completed before pivot; retained as behavior reference | 0 |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Godot .NET executable is not installed or not on PATH | Medium | High | Locate/install Godot .NET before S001-01; document exact executable path if needed. |
| Godot C# SDK version cannot be inferred safely by hand | Medium | High | Generate project files through Godot .NET editor/CLI rather than guessing SDK metadata. |
| Old Web/GDScript story assumptions leak into implementation | High | Medium | S001-06 blocks gameplay feature work until first-loop assumptions are refreshed. |
| Existing GDScript tests cannot validate C# behavior | High | Medium | S001-07 adds parity checks before retiring old tests. |

## Dependencies on External Factors

- Godot 4.6.2 .NET executable must be available locally.
- .NET SDK must be installed and visible to Godot/dotnet.
- CI cannot be finalized until the local C# build command is known.

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed.
- [ ] C# project files are tracked or a blocker explains why Godot .NET could not generate them.
- [ ] `dotnet build` or the documented Godot .NET build command passes.
- [ ] Registry and persistence parity evidence exists.
- [ ] Active production notes identify desktop C# as the implementation target.
- [ ] No new Web/GDScript-only implementation stories are started.

> **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint`
> before the last story is implemented. The Production → Polish gate requires a QA
> sign-off report, which requires a QA plan.

## Next Steps

1. Run the Godot .NET project generation step.
2. Implement S001-02 through S001-04 as the C# Foundation spike.
3. Add C# validation before migrating additional systems.
