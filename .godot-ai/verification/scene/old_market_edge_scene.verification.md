# Godot Asset Verification: old_market_edge_scene

## Verification Summary

- Contract: `.godot-ai/contracts/scene/old_market_edge_scene.contract.md`
- Review: `.godot-ai/reviews/scene/old_market_edge_scene.review.md`
- Execution Mode: reviewed-auto
- Result: pass
- Changed Godot Outputs: `src/scenes/market/OldMarketEdgeScene.tscn`; `src/scenes/market/OldMarketEdgeScene.cs`
- Evidence: Godot AI MCP loaded `res://src/scenes/market/OldMarketEdgeScene.tscn`, returned the expected world-layer hierarchy, and verified the `GeneralStallAnchor` interaction area. Runtime smoke verified the scene physics contract, authored scene-unit linkage, and the rule that UI/HUD/chart labels cannot satisfy scene evidence.
- Failed Checks: None for this asset pass.
- Risks Preserved: This is a future market asset-gate scene. It is not release-ready market gameplay and does not expose `route.market` in the current S4 chart.
- Follow-up Needed: Add route exposure, Settlement / S9 market interaction, NPC / purchase flow, final art/audio, and non-headless screenshots in later market passes.

## Godot AI MCP Evidence

- Active session: `claude-code-game-studios@8b92`
- Project path: `D:/Project/MineCraftMod/Claude-Code-Game-Studios/`
- Godot version: `4.6.2-stable`
- Scene hierarchy after `scene_open`:
  - `/OldMarketEdgeScene`
  - `/OldMarketEdgeScene/WorldLayer`
  - `/OldMarketEdgeScene/WorldLayer/MarketSkyBackdrop`
  - `/OldMarketEdgeScene/WorldLayer/MarketFarDockSilhouette`
  - `/OldMarketEdgeScene/WorldLayer/MarketCloudSeaBoundary`
  - `/OldMarketEdgeScene/WorldLayer/MarketPlazaGround`
  - `/OldMarketEdgeScene/WorldLayer/MarketWalkPath`
  - `/OldMarketEdgeScene/WorldLayer/GeneralStallBody`
  - `/OldMarketEdgeScene/WorldLayer/GeneralStallAwning`
  - `/OldMarketEdgeScene/WorldLayer/GeneralStallGoodsCrates`
  - `/OldMarketEdgeScene/WorldLayer/GeneralStallAnchor`
  - `/OldMarketEdgeScene/WorldLayer/GeneralStallAnchor/GeneralStallSoftOverlapShape`
  - `/OldMarketEdgeScene/WorldLayer/ClosedStallBody`
  - `/OldMarketEdgeScene/WorldLayer/ClosedStallShutter`
  - `/OldMarketEdgeScene/WorldLayer/MarketNoticeBoard`
  - `/OldMarketEdgeScene/WorldLayer/NoticeBoardRepairMarks`
  - `/OldMarketEdgeScene/WorldLayer/PlayerSpawn`
- Supplemental property check:
  - `/OldMarketEdgeScene/WorldLayer/GeneralStallAnchor` is `Area2D`, position `(370, 512)`, `monitoring=true`.

## Automated Verification

- `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false`: PASS, 0 warnings / 0 errors.
- `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`: PASS, 1085/1085.
- `dotnet run --project tests/integration/session/ShellUiTest.csproj`: PASS, 18/18.
- `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`: PASS.
- `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false`: PASS, 106 existing warnings / 0 errors.

## Gate Interpretation

The scene contract has been executed as a standalone world/playable Godot asset and runtime-readable #20 contract. It remains an asset-gate for future market work, not a current demo route or S4 chart exposure.
