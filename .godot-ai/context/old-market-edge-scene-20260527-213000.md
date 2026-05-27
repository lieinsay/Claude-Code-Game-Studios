# Godot Asset Context: old_market_edge_scene

## Source Request

用户要求继续下一步。当前场景登记表中 `old_market_edge_scene` 是剩余 future market tracked-gap，来源为 `design/gdd/port-village-market.md` 与 2026-05-24 场景集校正记录。

## Current Boundary

- Build an independent Godot scene asset for the old market edge.
- Do not expose `route.market` in current `S4_chart`; existing tests intentionally ensure the tracked-gap old market route is not selectable in the current demo route surface.
- Do not use HUD, labels, buttons, debug entry, or `S9_market` modal as production-ready scene evidence.
- This pass is an asset-gate for future market content, not the complete market purchase UI.

## Inputs

- Scene spec: `production/scene-specs/old-market-edge-scene.md`
- GDD: `design/gdd/port-village-market.md`
- Runtime authority: `SettlementManager`, `ResourcesManager`, `UIManager`
- Godot editor: MCP session `claude-code-game-studios@8b92`, Godot `4.6.2-stable`, readiness `ready`
