# Godot Asset Interview: old_market_edge_scene

## Ambiguity Resolution

- Human suitability: treated as `APPROVED_WITH_NOTES` from the 2026-05-24 user scene-set correction, with the later 2026-05-27 note that old market is future market content and not the current demo second island.
- Scope: independent market-edge world scene asset, scene-unit authoring, and #20 contract evidence.
- Out of scope: exposing `route.market`, implementing full `S9_market`, live purchases, NPC animation, final art/audio, and release screenshot packet.

## Non-Goals

- No route UI button restoration.
- No generic shop menu as scene evidence.
- No direct Resources or Persistence writes from the scene script.

## Acceptance Evidence

- Scene opens in Godot AI MCP and exposes market world-layer nodes.
- Authored content lists `old_market_edge_scene` and its world/playable units.
- `HubRuntime.DebugScenePhysicsContract("old_market_edge_scene")` returns a complete #20 contract.
- C# build, playable-slice integration, solution build, and `git diff --check` pass.
