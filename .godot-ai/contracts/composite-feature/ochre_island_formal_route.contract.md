# Godot Asset Contract: ochre_island_formal_route

## Identity

- Stable ID: `ochre_island_formal_route`
- Display Name: 赭石岛正式航线闭环
- Source Requirement: `production/scene-specs/ochre-island-scene.md`
- Asset Kind: composite-feature

## Required Outputs

- `src/presentation/playable_slice_authored_content.json` includes `authored_scenes::ochre_island_scene` and `route.ochre`.
- `src/core/content/Registry.cs` registers `resource.banded_iron_ore`.
- `src/presentation/PlayableSliceDomainAdapter.cs` exposes formal ore harvest into Resources carried pool and Hub return extraction into storage.
- `src/scenes/HubRuntime.cs` routes `route.ochre` into `ochre_island` and keeps debug entry diagnostic-only.
- `src/scenes/ui/ChartFullScreenSurface.*` exposes a selectable Ochre Island route without exposing old-market production evidence.
- `tests/integration/playable-slice/DomainAdapterProgram.cs` and `tests/smoke/session_shell_visual_probe.gd` prove route selection, destination, harvest, return, and UI-evidence exclusion.

## Acceptance

- Formal route selection commits `route.ochre`.
- Navigation encounter destination is `location.ochre-island`.
- Harvest writes `resource.banded_iron_ore x1` into carried resources.
- Return extracts the ore into storage and returns to Hub / initial island.
- Debug entry still opens the same independent scene but is not used as production-ready proof.
- Godot AI MCP can open `res://src/scenes/ochre/OchreIslandScene.tscn` and verify the ore instance hierarchy.
