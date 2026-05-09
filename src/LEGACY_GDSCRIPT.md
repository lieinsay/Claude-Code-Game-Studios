# Legacy GDScript Prototype Status

The `.gd` files under `src/` are retained as behavior references from the P3
prototype while the active implementation pivots to desktop Godot .NET/C#.

ADR-0019 governs new implementation work: new game/system code should be C# by
default. The legacy GDScript files should not receive new feature work unless a
story explicitly calls for prototype-only investigation or migration review.

## Migrated Foundation Files

- `src/core/snapshot_package.gd` -> `src/core/SnapshotPackage.cs`
- `src/core/registry.gd` and `src/core/registry_bootstrap.gd` -> `src/core/Registry.cs`
- `src/core/persistence.gd` -> `src/core/Persistence.cs`

## Queued Migration References

- `src/session_shell.gd`
- `src/core/interaction_registry.gd`
- `src/core/interactable.gd`
- `src/core/resources_manager.gd`
- `src/core/intel_manager.gd`
- `src/core/chart_manager.gd`
- `src/feature/world_repair.gd`
- `src/presentation/ui_manager.gd`
- `src/presentation/feedback_manager.gd`

Use these files to preserve behavior while migrating systems in dependency
order; do not treat them as the active language direction.
