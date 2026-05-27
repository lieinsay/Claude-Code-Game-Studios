# Mist-Lamp Wreck Scene Unit Placement Evidence

> **Date**: 2026-05-24
> **Scope**: Mist-lamp wreck scene (`mist_lamp_wreck_scene` / runtime `exploration_mist_island`)
> **Source Plan**: `.omx/plans/prd-scene-unit-placement-taxonomy.md`
> **Result**: PASS with residual editor/live-node-path review risks

## What Changed

This evidence covers the second scene-unit authoring slice after the Cloudweaver ship interior:

- `production/scene-specs/mist-lamp-wreck-scene.md` now defines the standalone scene spec for the existing `exploration_mist_island` runtime scene.
- `production/scene-specs/scene-coverage-registry.md` marks `mist_lamp_wreck_scene` as `spec_drafted` and links the new spec plus authored content.
- `src/presentation/playable_slice_authored_content.json` now contains mist-lamp wreck reusable prototypes and placed instances.
- `HubRuntime.DebugScenePhysicsContract("exploration_mist_island")` now builds its scene-unit catalog from authored prototype/instance data instead of a separate hardcoded catalog branch.
- Integration and Godot smoke checks now validate authored scene-unit linkage for both `hub_ship_interior` and `exploration_mist_island`.

## Verification Commands

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj` | PASS | 580/580 checks, including prototype allowed-scene coverage and instance-to-prototype scene compatibility. |
| `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` | PASS | 0 errors, 5 existing warnings in unrelated tests. |
| `godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd` | PASS | Verifies `exploration_mist_island` authored data, prototype-instance linkage, authored content source, empty diagnostics, scene spec traceability, Godot placement references, floor assignment, and UI-evidence rejection. |
| `git diff --check` | PASS | LF/CRLF warnings only. |

## Acceptance Mapping

| Acceptance | Evidence |
| --- | --- |
| Mist-lamp wreck has a standalone spec | `production/scene-specs/mist-lamp-wreck-scene.md`. |
| Existing runtime scene maps to the new scene identity | Coverage registry maps `mist_lamp_wreck_scene` to runtime `exploration_mist_island`; scene spec records both IDs. |
| Unit prototypes are reusable and classified | New `scene_unit.prototype.*` records include `dynamic_entity` / `fixed_scene_object`, collision, occlusion, scale, owner, and allowed scenes. |
| Placed instances reference prototypes | New `scene_unit.instance.exploration_mist_island.*` records reference known prototypes and the mist-lamp wreck scene spec. |
| Runtime reads the same authoring source | `HubRuntime` routes `exploration_mist_island` through `BuildAuthoredSceneUnitCatalog`. |
| Gate can fail on invalid linkage | Integration checks validate prototype IDs, allowed scene IDs, scene spec references, floor IDs, and `SceneUnitAuthoringFixture.ValidateScene("exploration_mist_island")`. |
| UI evidence remains invalid | Smoke checks require world/playable source data and reject UI-only scene-unit evidence. |

## Remaining Risks

- Godot node paths are stable authored references, but this pass still does not introspect each path against a serialized `.tscn` scene.
- This is still a greybox placement slice; final art, audio, and readability polish remain separate work.
- `hub_island_dock` and `voyage_open_world_scene` have since been migrated through their own Godot asset workflows; destination scenes, especially `mist_lamp_wreck_scene`, still need fresh migration before claiming full project coverage.

## Recommended Next Step

Run the Godot asset workflow for `production/scene-specs/mist-lamp-wreck-scene.md`, then replace the remaining `exploration_mist_island` greybox evidence with an independent `mist_lamp_wreck_scene` asset.
