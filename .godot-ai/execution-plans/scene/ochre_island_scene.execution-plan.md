# Godot Asset Execution Plan: ochre_island_scene

## Plan Metadata

- Contract path: `.godot-ai/contracts/scene/ochre_island_scene.contract.md`
- Review path: `.godot-ai/reviews/scene/ochre_island_scene.review.md`
- Execution mode: reviewed-auto

## Assets To Create Or Modify

- Create `src/scenes/ochre/OchreIslandScene.tscn`
- Create `src/scenes/ochre/OchreIslandScene.cs`
- Optionally update `src/presentation/playable_slice_authored_content.json` with `ochre_island_scene` records if MCP/editor execution confirms the authoring-data route is needed for evidence.
- Do not delete or replace existing scenes/nodes.

## Godot AI MCP Capabilities Likely Needed

- Session list / activate
- Editor state
- Scene create / save
- Node create
- Node set property
- Script create / attach
- Scene hierarchy read
- Screenshot or preview capture

## Verification Evidence Required

- `.godot-ai/verification/scene/ochre_island_scene.verification.md`
- Hierarchy evidence for island ground, path, ore instance, return anchor, boundary, spawn.
- Screenshot or visual capture.
- Runtime or smoke evidence for movement bounds, harvest anchor, return anchor, and UI evidence exclusion.

## Known Risks To Preserve

- If no Godot editor session exists, stop as blocked.
- If target paths already exist, inspect before modifying and avoid overwriting without clear safety.
- Full route integration from `voyage_open_world_scene` may be a follow-up if not possible within editor asset execution.

