# Godot Asset Verification: ochre_island_formal_route

## Verification Summary

- Contract: `.godot-ai/contracts/composite-feature/ochre_island_formal_route.contract.md`
- Review: `.godot-ai/reviews/composite-feature/ochre_island_formal_route.review.md`
- Execution Mode: reviewed-auto
- Result: pass
- Changed Godot Outputs: `src/scenes/ui/ChartFullScreenSurface.tscn`; `src/scenes/ui/ChartFullScreenSurface.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`
- Evidence: formal route, Resources write, Hub return settlement, Godot smoke, integration test, and Godot AI MCP hierarchy / property checks pass.
- Failed Checks: screenshots skipped by current headless display driver.

## Godot AI MCP Evidence

- Active session: `claude-code-game-studios@8b92`
- Godot version: `4.6.2-stable`
- `editor_state`: ready
- `scene_open("res://src/scenes/ochre/OchreIslandScene.tscn")`: PASS
- `scene_get_hierarchy(depth=4)`: PASS, 18 nodes
- Required nodes present: `WorldLayer`, `OchreIslandGround`, `WalkPath`, `CloudSeaBoundary`, `PlayerSpawn`, `BandedIronOreInstance`, `BandedIronOreAnchor/SoftOverlapShape`, `OchreReturnAnchor/ReturnSoftOverlapShape`, `ReturnBeaconGreybox`
- `node_get_properties("/OchreIslandScene/WorldLayer/BandedIronOreInstance")`: PASS, position `(655, 390)`, script `res://src/scenes/units/BandedIronOre.cs`, `Harvested=false`

## Automated Evidence

- PASS: `dotnet build CloudWeaverVoyage.csproj --no-restore -p:UseSharedCompilation=false` -- 0 warnings / 0 errors.
- PASS: `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` -- 921/921 passing.
- PASS: `dotnet run --project tests/integration/session/ShellUiTest.csproj` -- 18/18 checks passed.
- PASS: `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd`.
- PASS: `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` -- 106 existing Godot source-generator / test warnings, 0 errors.
- PASS: `git diff --check` -- only LF/CRLF working-copy warnings.

## Gate Interpretation

`ochre_island_scene` is now a formal playable route asset slice. The release handoff remains blocked only by non-headless screenshot evidence, final art/audio, complete live driving polish, and release packet work.
