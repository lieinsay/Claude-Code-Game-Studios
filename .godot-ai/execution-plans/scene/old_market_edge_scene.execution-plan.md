# Godot Asset Execution Plan: old_market_edge_scene

## Steps

1. Create `src/scenes/market/OldMarketEdgeScene.tscn` and `OldMarketEdgeScene.cs`.
2. Add authored content scene metadata, market scene-unit prototypes, and placed instances.
3. Add `HubRuntime.DebugScenePhysicsContract("old_market_edge_scene")` support for #20 evidence.
4. Update `DomainAdapterProgram` validation for the new approved asset slice.
5. Update scene registry / gate docs and session state.
6. Verify with Godot AI MCP, build, integration tests, solution build, and `git diff --check`.

## Godot AI MCP Required

- `session_activate`
- `editor_state`
- `scene_open`
- `scene_get_hierarchy`
- `node_get_properties`

## Out Of Scope

- `route.market` exposure in `S4_chart`.
- Purchase modal implementation.
- Final art/audio or non-headless screenshots.
