# Godot Asset Execution Plan: S4_chart

- Contract path: `.godot-ai/contracts/ui/S4_chart.contract.md`
- Review path: `.godot-ai/reviews/ui/S4_chart.review.md`
- Execution mode: reviewed-auto
- Assets to create or modify: `src/scenes/ui/ChartFullScreenSurface.tscn`; `src/scenes/ui/ChartFullScreenSurface.cs`; `src/scenes/HubRuntime.cs`; `tests/smoke/session_shell_visual_probe.gd`
- Godot AI MCP capabilities likely needed: session list/activate, scene open, hierarchy read, logs read.
- Verification evidence required: Godot scene load hierarchy, runtime smoke confirms chart mode uses independent surface, dotnet build, git diff check.
- Known risks to preserve in verification: Only current MVP route is fully selectable; release screenshot and final art/audio remain downstream.
