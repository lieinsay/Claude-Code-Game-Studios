# Godot Asset Execution Plan: scene_unit.prototype.chart_table

- Contract path: `.godot-ai/contracts/fixed-world-unit/scene_unit.prototype.chart_table.contract.md`
- Review path: `.godot-ai/reviews/fixed-world-unit/scene_unit.prototype.chart_table.review.md`
- Execution mode: reviewed-auto
- Assets to create or modify: `src/scenes/units/ChartTable.tscn`; `src/scenes/units/ChartTable.cs`; `src/presentation/playable_slice_authored_content.json`; `src/scenes/HubRuntime.cs`; `tests/smoke/session_shell_visual_probe.gd`
- Godot AI MCP capabilities likely needed: session list/activate, scene open, hierarchy read, logs read.
- Verification evidence required: Godot scene load hierarchy, smoke pass, dotnet build, git diff check.
- Known risks to preserve in verification: Greybox art only; release screenshot and final audio/art remain follow-up.
