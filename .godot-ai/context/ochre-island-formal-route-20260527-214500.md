# Godot Asset Context: ochre-island-formal-route

- Date: 2026-05-27
- Workflow: `godot-asset-interview -> godot-asset-review -> godot-asset-execute`
- Source Requirement: `production/scene-specs/ochre-island-scene.md`
- Scope: Promote the existing standalone `ochre_island_scene` asset from debug-only evidence into a formal playable route.

## Grounding

- `src/scenes/ochre/OchreIslandScene.tscn` and `src/scenes/units/BandedIronOre.tscn` already exist as independent Godot assets.
- Production evidence may not use old HubRuntime greybox, HUD, buttons, labels, or debug entry as scene proof.
- Formal route evidence must show `route.ochre`, `location.ochre-island`, Resources reward write, and Hub return settlement.

## Constraints

- Keep Resources, Navigation, Hub, Persistence, and Chart as domain authorities.
- Do not replace or delete the existing debug entry; it may stay as diagnostics only.
- Keep `ochre_island_scene` world evidence in the standalone scene asset and authoring data.
