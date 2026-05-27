# Godot Asset Interview Summary: ship-interior-layered

## Interview Result

- Final ambiguity: 12%
- Recommended next step: `godot-asset-review`

## Resolved Dimensions

- Intent Clarity: resolved. Build `ship_interior_layered` as an independent production-traceable Godot scene asset.
- Asset Type Clarity: resolved. Asset type is `scene`.
- Scope Clarity: resolved. Minimal first pass: layered ship interior shell, cockpit/chart area, cargo bay, engine bay, exit threshold, authored unit references, and ChartTable/S4_chart linkage.
- Runtime Boundary Clarity: resolved. Scene owns visual/world layout and instance references only; HubRuntime mounts it and retains domain/state authority.
- Visual/Interaction Contract Clarity: resolved. The scene must make room landmarks and ChartTable readable without HUD text; ChartTable + Use opens S4_chart through existing Hub/Chart authority.
- Decision Boundary Clarity: resolved. AI may choose greybox composition and node organization; AI must ask before deleting/replacing legacy nodes or adding new gameplay systems.
- Acceptance Evidence Clarity: resolved. Evidence includes scene hierarchy, smoke assertions, manifest linkage, Godot load, build, and verification artifact.
- Brownfield Integration Clarity: resolved. Integrate non-destructively with `HubRuntime`, `playable_slice_authored_content.json`, smoke tests, and scene spec status.

## Non-blocking Assumptions

- Existing `hub_ship_interior` runtime contract remains the compatibility ID until a later scene-router rename migrates saves and tests.
- Existing HubRuntime greybox helpers may remain as fallback/background scaffolding, but production evidence must cite `ShipInteriorLayeredScene.tscn`.

## Contract

- `.godot-ai/contracts/scene/ship_interior_layered.contract.md`
