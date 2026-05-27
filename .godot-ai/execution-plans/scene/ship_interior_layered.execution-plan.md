# Godot Asset Execution Plan: ship_interior_layered

- Contract path: `.godot-ai/contracts/scene/ship_interior_layered.contract.md`
- Review path: `.godot-ai/reviews/scene/ship_interior_layered.review.md`
- Execution mode: reviewed-auto
- Assets to create or modify: `src/scenes/ship/ShipInteriorLayeredScene.tscn`; `src/scenes/ship/ShipInteriorLayeredScene.cs`; `src/scenes/HubRuntime.cs`; `src/presentation/playable_slice_authored_content.json`; `tests/smoke/session_shell_visual_probe.gd`; `production/scene-specs/ship-interior-layered-scene.md`; `.godot-ai/verification/scene/ship_interior_layered.verification.md`
- Godot AI MCP capabilities likely needed: session/editor status, scene/resource load, hierarchy inspection, logs.
- Verification evidence required: scene hierarchy includes ChartTable and S4_chart reference; HubRuntime mounts scene; smoke opens S4_chart from chart table; build and diff checks pass.
- Known risks to preserve in verification: legacy HubRuntime greybox helper nodes may remain as fallback and must not be cited as production-ready scene asset proof.
