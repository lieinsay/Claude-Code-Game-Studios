# ADR-0019: Desktop C# Platform Pivot

## Status

Accepted

## Date

2026-05-09

## Last Verified

2026-05-09

## Decision Makers

User, Technical Director, Codex

## Summary

The project is pivoting from a Web-first Godot/GDScript target to a desktop-first Godot .NET/C# target. This ADR supersedes Web-export constraints for active development while preserving the existing Autoload, signal, persistence, and MVP thin-slice architecture as design intent.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 .NET |
| **Domain** | Platform / Scripting / Build |
| **Knowledge Risk** | HIGH — Godot 4.6.2 is post-cutoff and C# project behavior must be verified against engine docs |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/current-best-practices.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md`, official Godot 4.6 docs via Context7 |
| **Post-Cutoff APIs Used** | Godot 4.6 C#/.NET project workflow; Godot 4.6 desktop rendering defaults; C# automatic string extraction noted in 4.6 changes |
| **Verification Required** | Godot .NET editor can open the project; `dotnet build` succeeds; desktop headless validation can run; exported Windows desktop build launches the SessionShell scene |

> **Note**: Godot 4 C# projects cannot export to Web. This is the reason Web is no longer a supported primary target for this project.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001, ADR-0002, ADR-0003, ADR-0006 |
| **Enables** | C# migration stories for Foundation/Core systems; Desktop MVP first-loop Sprint |
| **Blocks** | New implementation stories that assume Web/GDScript constraints |
| **Ordering Note** | Update technical preferences and production plans before migrating gameplay code so implementers do not read stale Web/GDScript constraints. |

## Context

### Problem Statement

The project was configured as Web-first with GDScript because Godot 4 C# cannot export to Web. The user has now decided not to target Web and wants desktop plus C#, which makes the existing Web-only constraints actively misleading for future implementation.

### Current State

- `CLAUDE.md`, technical preferences, engine reference docs, ADR-0006, the architecture registry, production stories, and several tests describe Web-first behavior.
- `project.godot` currently autoloads GDScript implementations.
- The P3 prototype proves the architecture shape, but the active implementation language and platform are changing.
- Existing GDD mechanics remain valid because the game loop and systems are not inherently Web-specific.

### Constraints

- Preserve the approved MVP loop and system boundaries.
- Do not rewrite all GDDs before implementation can resume.
- Do not delete ADR-0006; retain it as historical Web-export rationale and explicitly supersede active development constraints.
- Use C# as the primary implementation language for gameplay, persistence, systems, UI coordination, and tests.
- Avoid new third-party dependencies unless explicitly requested.

### Requirements

- The active target is desktop, initially Windows; Linux can be added after the first desktop build is stable.
- All new game code must be C# unless a specific asset, shader, or throwaway prototype requires otherwise.
- Web lifecycle behavior must be removed from active implementation stories and replaced by desktop focus, pause, quit, and save boundaries.
- Save/load remains Canonical JSON using `user://`, but no longer depends on IndexedDB or browser lifecycle events.
- Signal and state-machine architecture remains the project integration pattern.

## Decision

The project will move to **Godot 4.6.2 .NET, desktop-first, C# primary**. Web export is removed from the MVP target. ADR-0006 remains as a superseded Web-platform decision record, but ADR-0019 governs all new implementation work.

### Architecture

```
Desktop OS
  |
  v
Godot 4.6.2 .NET runtime
  |
  v
SessionShell.cs
  |-- owns desktop lifecycle: boot, title, loading, playing, paused, quitting, error
  |-- connects Autoload signals during boot phases
  |
  +--> Foundation C# Autoloads
  |      Registry.cs
  |      Persistence.cs
  |      InteractionRegistry.cs
  |      ResourcesManager.cs
  |
  +--> Core / Feature C# Autoloads
  |      IntelManager.cs
  |      ChartManager.cs
  |      WorldRepair.cs
  |
  +--> Presentation C# Autoloads
         UIManager.cs
         FeedbackManager.cs
```

### Key Interfaces

```csharp
namespace CloudWeaverVoyage.Core;

public interface ISnapshotSerializable
{
    SnapshotPackage CaptureSnapshot();
    SnapshotRestoreResult RestoreSnapshot(SnapshotPackage package);
}

public enum DesktopLifecycleState
{
    Boot,
    Title,
    Loading,
    Playing,
    Paused,
    Quitting,
    Error
}
```

### Implementation Guidelines

- C# script filenames use PascalCase, matching class names: `Registry.cs`, `Persistence.cs`, `SessionShell.cs`.
- Use `partial` Godot node classes and `[Signal]` delegate patterns for Godot-facing signals.
- Use standard .NET collections internally; use Godot collections only when data crosses Godot serialization, inspector, or scripting boundaries.
- Keep save payloads plain, deterministic data structures that can be serialized to Canonical JSON.
- Establish signal connections during boot phases, not in per-frame callbacks.
- Prefer explicit desktop lifecycle hooks over browser-specific JavaScriptBridge callbacks.
- Keep the existing GDScript prototype until the C# Foundation spike passes; remove or archive it after parity is demonstrated.

## Alternatives Considered

### Alternative 1: Keep Web + GDScript

- **Description**: Continue with the current Web-first architecture and GDScript implementation.
- **Pros**: Lowest immediate migration cost; existing prototype and docs remain aligned.
- **Cons**: Contradicts the user's target; blocks C#; keeps browser-specific constraints that no longer serve the product.
- **Estimated Effort**: Low now, high later if the project pivots again.
- **Rejection Reason**: The user explicitly wants desktop plus C#.

### Alternative 2: Mixed GDScript + C#

- **Description**: Keep GDScript for UI/scene scripts and use C# for heavier systems.
- **Pros**: Gradual migration; less churn in scene scripts.
- **Cons**: Preserves cross-language boundary complexity; implementers must constantly choose a language; weakens the stated direction.
- **Estimated Effort**: Medium.
- **Rejection Reason**: A clean C# primary direction is simpler for the next implementation phase.

### Alternative 3: Switch Engine To Unity

- **Description**: Move to Unity because C# is native to its workflow.
- **Pros**: Mature C# tooling and desktop build pipeline.
- **Cons**: Massive rewrite of Godot-specific architecture, docs, tests, and project assets; unnecessary for a 2D Godot-shaped MVP.
- **Estimated Effort**: Very high.
- **Rejection Reason**: Godot .NET supports the desired desktop+C# path without changing engines.

## Consequences

### Positive

- C# becomes available for all gameplay and system code.
- Browser lifecycle, WebGL 2, IndexedDB, single-threaded Web export, and AudioContext constraints no longer dominate design.
- Desktop builds can use stronger .NET tooling, compiler checks, and future profiling options.
- The project can simplify persistence and lifecycle stories.

### Negative

- Existing GDScript implementation must be migrated or retired.
- Existing GdUnit4 tests need a C#-compatible validation path.
- Documentation and stories containing Web/GDScript assumptions must be revised.
- Web export is no longer a supported MVP target.

### Neutral

- The game concept, GDD system boundaries, ADR signal architecture, and MVP thin-slice remain valid.
- Compatibility renderer can remain temporarily for 2D stability even though desktop can support Forward+.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Godot .NET toolchain missing locally | Medium | High | Add setup check for Godot .NET executable and .NET SDK before code migration. |
| Stale Web constraints remain in stories | High | Medium | Run a targeted production-doc refresh before Sprint 1 implementation. |
| Big-bang code rewrite breaks verified prototype behavior | Medium | High | Migrate Foundation spike first and keep GDScript prototype until parity tests pass. |
| C# signal syntax differs from GDScript assumptions | Medium | Medium | Validate signal delegate patterns in first spike and update ADR-0002 if needed. |
| CI still runs only GdUnit4 GDScript tests | High | Medium | Add `dotnet build` and a desktop/headless validation step before removing old tests. |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| CPU (frame time) | 16ms Web-first frame budget | 16ms desktop gameplay budget with more headroom | 16.67ms at 60fps |
| Memory | 200MB browser tab ceiling | 512MB MVP desktop soft ceiling | <=512MB MVP |
| Load Time | <2s warm Web boot | <2s desktop boot to SessionShell ready | <2s warm desktop boot |
| Network | Not applicable | Not applicable | None |

## Migration Plan

1. Update project contract files: `CLAUDE.md`, `.claude/docs/technical-preferences.md`, engine reference docs, architecture registry, production state.
2. Add C# project setup once a Godot .NET executable is available locally. Ensure `.csproj` and `.sln` are tracked for Godot C#.
3. Migrate `SnapshotPackage`, `Registry`, and `Persistence` to C# as the Foundation spike.
4. Add `dotnet build` and a headless desktop validation command to CI/local verification.
5. Migrate `ResourcesManager`, `IntelManager`, `ChartManager`, `WorldRepair`, `UIManager`, `FeedbackManager`, and `SessionShell` in dependency order.
6. Refresh production stories that contain Web/GDScript assumptions.
7. Retire the GDScript P3 prototype after C# parity checks pass.

**Rollback plan**: Revert this ADR and the contract updates, keep the GDScript prototype as the authoritative implementation, and restore ADR-0006 as active.

## Validation Criteria

- [ ] `CLAUDE.md` and technical preferences identify Godot .NET/C# desktop as the active target.
- [ ] Godot .NET project files are present and build locally with `dotnet build`.
- [ ] Desktop SessionShell launches from `project.godot`.
- [ ] C# Foundation spike passes snapshot, registry, and persistence parity checks.
- [ ] Production stories no longer require Web lifecycle handling for Sprint 1.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/platform-session-shell.md` | #2 Platform Session Shell | Own the session lifecycle and input gate for the active platform. | Replaces browser lifecycle ownership with desktop lifecycle ownership while preserving SessionShell as the owner. |
| `design/gdd/local-save-world-state-persistence.md` | #3 Persistence | Preserve world state and support save/load roundtrip. | Keeps Canonical JSON and `user://`, removes IndexedDB/browser emergency-save assumptions. |
| `design/gdd/ui-hud-chart-interface.md` | #16 UI/HUD | Maintain input routing and desktop keyboard/mouse clarity. | Keeps signal-driven UI coordination and removes Web tab-freeze recovery as an MVP requirement. |

## Related

- Supersedes active implementation constraints from `docs/architecture/adr-0006-web-platform-constraints.md`.
- Related: `docs/architecture/adr-0001-autoload-scene-boot-order.md`
- Related: `docs/architecture/adr-0002-signal-communication-protocol.md`
- Related: `docs/architecture/adr-0003-save-system-snapshot-json.md`
