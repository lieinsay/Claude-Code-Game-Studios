# Scene Unit Prototype and Placement Workflow Evidence

> **Date**: 2026-05-24
> **Scope**: Cloudweaver ship interior first slice (`ship_interior_layered` / runtime `hub_ship_interior`)
> **Source Plan**: `.omx/plans/prd-scene-unit-placement-taxonomy.md`
> **Result**: PASS with residual art/editor scope risks

## What Changed

This evidence covers the first scene-unit authoring slice requested after the scene-unit taxonomy interview:

- `单位原型` and `摆放实例` are now documented in #20 and linked from #19 scene-composition rules.
- `src/presentation/playable_slice_authored_content.json` contains reusable ship-interior scene-unit prototypes and placed instances.
- `src/presentation/SceneUnitAuthoring.cs` loads and validates those records.
- `HubRuntime.DebugScenePhysicsContract("hub_ship_interior")` exposes prototype/instance linkage through the scene-unit catalog while preserving the existing contract shape.
- The ship-interior scene spec records where GDD rules, scene data, runtime hooks, and QA evidence live.

## Verification Commands

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 328/328 checks, including scene-unit prototype/instance data validation. |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 0 errors. Warnings include one transient Godot DLL copy retry and existing test warnings. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Verifies `hub_ship_interior` authored scene-unit data, prototype-instance linkage, authored content source, empty diagnostics, scene spec traceability, Godot placement references, floor assignment, and UI-evidence rejection. |
| `git diff --check` | PASS | LF/CRLF warnings only. |

## Acceptance Mapping

| Acceptance | Evidence |
| --- | --- |
| Unit prototypes are reusable and classified | `scene_unit_prototypes` in `src/presentation/playable_slice_authored_content.json`; C# validation checks `dynamic_entity` / `fixed_scene_object`. |
| Placed instances reference prototypes | `scene_unit_instances` in authored content; C# validation checks each `prototype_id` resolves. |
| Runtime contract is not only an isolated hardcoded catalog | `HubRuntime.DebugScenePhysicsContract("hub_ship_interior")` now builds the ship-interior catalog from `SceneUnitAuthoringFixture`. |
| Godot placement remains visible in the data | Each placed instance has a `godot_node_path` and placement position. |
| UI evidence remains invalid | Existing and new smoke checks require `source_layer == world_playable_scene` and `ui_evidence_allowed == false`. |
| Scope stays limited to one scene | Only `hub_ship_interior` uses authored prototype/instance linkage in this pass; other current scenes remain on existing contract paths. |

## Remaining Risks

- This is not a full level editor. It proves a data-backed Godot placement pattern for one scene.
- Godot node paths are authored as stable references, but this pass does not yet introspect every node path against a live `.tscn` hierarchy.
- The current scene remains greybox; formal art/audio replacement is still blocked by existing asset gaps.
- Other scenes are not migrated and should not be treated as covered by this first slice.

## Recommended Next Step

Use this first slice as the pattern for either `initial_island_scene` or `mist_lamp_wreck_scene` only after user review confirms the ship-interior workflow is understandable and useful.
