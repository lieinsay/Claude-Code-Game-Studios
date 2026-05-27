# Godot Asset Execution Plan: ochre_island_resource_slice

## Plan Metadata

- Contract path: `.godot-ai/contracts/composite-feature/ochre_island_resource_slice.contract.md`
- Review path: `.godot-ai/reviews/composite-feature/ochre_island_resource_slice.review.md`
- Execution mode: reviewed-auto

## Assets To Create Or Modify

- Execute child plan `.godot-ai/execution-plans/fixed-world-unit/scene_unit.prototype.banded_iron_ore.execution-plan.md`
- Execute child plan `.godot-ai/execution-plans/scene/ochre_island_scene.execution-plan.md`
- Write `.godot-ai/verification/composite-feature/ochre_island_resource_slice.verification.md`
- Do not delete or replace existing Godot nodes.

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

- Child verification records for the scene and unit.
- Parent verification summary citing child outputs.
- Evidence that the ore unit is placed in the scene and that UI-only proof is rejected.

## Known Risks To Preserve

- If Godot AI MCP/editor session is unavailable, execution result is `blocked` and no manual scene/node workaround should be used.
- Full release handoff still needs screenshot refresh and smoke evidence after successful asset creation.

