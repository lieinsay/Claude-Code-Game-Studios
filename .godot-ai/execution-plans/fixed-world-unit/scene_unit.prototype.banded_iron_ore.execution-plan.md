# Godot Asset Execution Plan: scene_unit.prototype.banded_iron_ore

## Plan Metadata

- Contract path: `.godot-ai/contracts/fixed-world-unit/scene_unit.prototype.banded_iron_ore.contract.md`
- Review path: `.godot-ai/reviews/fixed-world-unit/scene_unit.prototype.banded_iron_ore.review.md`
- Execution mode: reviewed-auto

## Assets To Create Or Modify

- Create `src/scenes/units/BandedIronOre.tscn`
- Create `src/scenes/units/BandedIronOre.cs`
- Optionally update `src/presentation/playable_slice_authored_content.json` with the reusable prototype and one `ochre_island_scene` instance if needed for evidence.
- Do not delete or replace existing scenes/nodes.

## Godot AI MCP Capabilities Likely Needed

- Session list / activate
- Editor state
- Scene create / save
- Node create
- Node set property
- Script create / attach
- Scene hierarchy read

## Verification Evidence Required

- `.godot-ai/verification/fixed-world-unit/scene_unit.prototype.banded_iron_ore.verification.md`
- Reusable unit hierarchy evidence.
- Available / harvested / blocked state evidence.
- Soft-overlap interaction anchor evidence.
- Instance evidence inside `ochre_island_scene`.

## Known Risks To Preserve

- Resource reward may use an existing/basic placeholder until economy naming is finalized.
- If no Godot editor session exists, stop as blocked.
- Do not implement regeneration or complex mining behavior.

