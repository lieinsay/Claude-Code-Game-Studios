# Retired GDScript Prototype Status

The `.gd` files under `src/` were retired after the P3 prototype behavior was
migrated into C# domain classes and parity runners.

ADR-0019 governs new implementation work: new game/system code should be C# by
default. Do not reintroduce GDScript gameplay/system code unless a future ADR
explicitly changes the implementation language boundary.

## Retired Source Mapping

- `src/core/snapshot_package.gd` -> `src/core/persistence/SnapshotPackage.cs`
- `src/core/registry.gd` and `src/core/registry_bootstrap.gd` -> `src/core/content/Registry.cs`
- `src/core/persistence.gd` -> `src/core/persistence/Persistence.cs`
- `src/core/interaction_registry.gd` -> `src/core/interaction/InteractionRegistry.cs`
- `src/core/resources_manager.gd` -> `src/core/resources/ResourcesManager.cs`
- `src/core/intel_manager.gd` -> `src/core/intel/IntelManager.cs`
- `src/core/chart_manager.gd` -> `src/core/chart/ChartManager.cs`
- `src/feature/world_repair.gd` -> `src/features/world_repair/WorldRepair.cs`
- `src/presentation/ui_manager.gd` -> `src/presentation/UIManager.cs`
- `src/presentation/feedback_manager.gd` -> `src/presentation/FeedbackManager.cs`
- `src/session_shell.gd` -> `src/core/boot/SessionBootChain.cs`

The active automated verification is now:

```bash
dotnet build CloudWeaverVoyage.sln
dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj
dotnet run --project tests/unit/registry/IdRegistryCoreTest.csproj
```
