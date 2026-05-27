# Godot Asset Verification: scene_unit.prototype.banded_iron_ore

## Verification Summary

- Contract: `.godot-ai/contracts/fixed-world-unit/scene_unit.prototype.banded_iron_ore.contract.md`
- Review: `.godot-ai/reviews/fixed-world-unit/scene_unit.prototype.banded_iron_ore.review.md`
- Execution Mode: reviewed-auto
- Result: pass
- Changed Godot Outputs: `src/scenes/units/BandedIronOre.tscn`; `src/scenes/units/BandedIronOre.cs`; `src/scenes/units/BandedIronOre.cs.uid`
- Evidence: Godot AI MCP loaded `res://src/scenes/units/BandedIronOre.tscn` and returned the expected hierarchy. `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` passed with 107 existing warnings / 0 errors.
- Failed Checks: None for this asset pass.
- Risks Preserved: The unit exposes the local harvest signal and visible states, but Resources reward wiring remains a follow-up integration task.
- Follow-up Needed: Wire the ore harvest request into the Resources/domain route when `ochre_island_scene` enters the playable route.

## Godot AI MCP Evidence

- Active session: `claude-code-game-studios@2681`
- Project path: `D:/Project/MineCraftMod/Claude-Code-Game-Studios/`
- Godot version: `4.6.2-stable`
- Unit hierarchy after `scene_open`:
  - `/BandedIronOre`
  - `/BandedIronOre/OreBodyAvailable`
  - `/BandedIronOre/DarkIronBandA`
  - `/BandedIronOre/DarkIronBandB`
  - `/BandedIronOre/HarvestedStateOverlay`
  - `/BandedIronOre/BandedIronOreAnchor`
  - `/BandedIronOre/BandedIronOreAnchor/SoftOverlapShape`

## Gate Interpretation

The fixed-world-unit contract has been executed as a non-destructive reusable Godot asset. It satisfies the Godot asset workflow's unit creation step, while release handoff remains pending scene integration, screenshots, and smoke evidence.
