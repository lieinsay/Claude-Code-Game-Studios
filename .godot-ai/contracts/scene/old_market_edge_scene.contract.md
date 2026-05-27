# Godot Asset Contract: old_market_edge_scene

## Identity

- Asset Type: scene
- Stable ID: `old_market_edge_scene`
- Lifecycle State: execution-ready
- Source Spec: `production/scene-specs/old-market-edge-scene.md`
- Source GDD: `design/gdd/port-village-market.md`

## Scope

Create a standalone Godot scene asset for the old market edge future market stop. The scene must contain a world/playable layer, player spawn, market plaza, walk path, one open stall, one closed stall, a soft-overlap stall interaction anchor, a notice board, and a cloudsea boundary.

## Constraints

- The scene script may expose diagnostic evidence but must not own purchases, currency, inventory, stall unlocks, or persistence.
- `route.market` remains hidden from current `S4_chart`.
- `S9_market` and any HUD text can assist later purchase flow but cannot prove scene readiness.
- Production-ready evidence comes from the standalone scene, authored scene units, #20 contract, and Godot MCP inspection.

## Required Outputs

- `src/scenes/market/OldMarketEdgeScene.tscn`
- `src/scenes/market/OldMarketEdgeScene.cs`
- `src/presentation/playable_slice_authored_content.json` authoring for the scene and world units
- `HubRuntime.DebugScenePhysicsContract("old_market_edge_scene")`
- `production/scene-specs/old-market-edge-scene.md`
- `.godot-ai/verification/scene/old_market_edge_scene.verification.md`

## Acceptance Checks

- Godot AI MCP opens the scene and returns the expected hierarchy.
- Authored content validation accepts the scene and unit authoring.
- The runtime physics contract rejects UI-only evidence and reports world/playable units.
- Existing shell UI checks still prove `route.market` is not exposed in the current chart.
