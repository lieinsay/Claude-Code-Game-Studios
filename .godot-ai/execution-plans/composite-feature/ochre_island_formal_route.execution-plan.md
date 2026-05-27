# Godot Asset Execution Plan: ochre_island_formal_route

- Contract path: `.godot-ai/contracts/composite-feature/ochre_island_formal_route.contract.md`
- Review path: `.godot-ai/reviews/composite-feature/ochre_island_formal_route.review.md`
- Execution Mode: reviewed-auto

## Steps

1. Add `route.ochre`, `authored_scenes::ochre_island_scene`, and ore reward authoring data.
2. Register `resource.banded_iron_ore` in the content registry.
3. Add domain adapter harvest and return evidence for carried/storage pools.
4. Add S4 chart selection and HubRuntime formal `ochre_island` state.
5. Extend integration and Godot smoke tests.
6. Use Godot AI MCP to open `OchreIslandScene.tscn` and capture hierarchy / property evidence.
7. Update production specs, QA evidence, and session state.

## Verification

- `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`
- Godot AI MCP `scene_open`, `scene_get_hierarchy`, `node_get_properties`
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`
- `git diff --check`
