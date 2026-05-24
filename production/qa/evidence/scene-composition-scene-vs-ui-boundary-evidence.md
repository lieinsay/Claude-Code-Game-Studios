# Scene Composition Scene Versus UI Boundary Evidence

> **Story**: `production/epics/scene-composition-system/story-003-scene-vs-ui-evidence-boundary.md`
> **Date**: 2026-05-24
> **Result**: PASS
> **Story Type**: Integration

## Scope

Story 003 hardens the #19 boundary that scenes are world/playable spaces, not UI/HUD surfaces. It does not redesign UI layout, add final scene art, alter #20 physics internals, or implement Story 004's feedback-routing handoff.

## Created Artifacts

- `production/scene-specs/scene-vs-ui-evidence-boundary.md`
  - Defines `ui_boundary_passed`.
  - Classifies world/playable evidence versus assistive UI evidence.
  - Defines `hud_not_dominant`, `primary_scene_viewport_share`, world identity, and core anchor checks.
  - Lists synthetic UI-only evidence packages that must fail readiness.
  - Defines focus-isolation requirements that preserve underlying scene evidence.
  - Classifies `chart_table_scene` as a UI-assisted world surface anchored inside `hub_ship_interior`.
- `tests/integration/scene-composition/SceneVsUiBoundaryTest.csproj`
- `tests/integration/scene-composition/SceneVsUiBoundaryProgram.cs`
  - Validates the boundary document, completeness gate, registry, scene template, existing Godot smoke evidence, and UI focus regression labels.

## Acceptance Coverage

| AC | Result | Evidence |
| --- | --- | --- |
| UI/HUD does not dominate or hide world identity | PASS | Boundary document requires `hud_not_dominant`, `primary_scene_viewport_share`, visible world identity, and visible core anchors; integration test verifies current smoke checks main viewport coverage. |
| UI/HUD label, button, menu, or debug overlay does not count as scene proof | PASS | Boundary and completeness gate classify these as assistive-only; integration test verifies template, registry, and smoke still reject UI evidence for physical proof. |
| UI-only evidence without world/playable nodes fails readiness | PASS | Boundary defines `ui_only_surface`, `debug_overlay_only`, `button_only_interaction`, and `ui_physics_contract` as failing cases; integration test verifies the gate blocks UI-only evidence. |
| Modal/semi-modal UI isolates focus without deleting scene evidence | PASS | Boundary preserves ADR-0012 focus ownership; integration test verifies existing smoke and UI focus regression coverage. |

## Verification

```text
dotnet run --project tests/integration/scene-composition/SceneVsUiBoundaryTest.csproj
Result: PASS
Checks: 5/5
```

```text
dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false
Result: PASS
Warnings: 5 existing warnings
Errors: 0
```

```text
godot --headless --path . -s tests/smoke/session_shell_visual_probe.gd
Result: PASS
Notes: Headless screenshot saves were skipped by the current display driver; runtime assertions passed.
```

```text
git diff --check
Result: PASS
Notes: LF/CRLF warnings may appear for existing files; no whitespace errors.
```

## Boundary Notes

- UI/HUD/buttons/menus/labels/debug overlays remain assistive-only.
- Current runtime smoke may use debug hooks to inspect evidence, but debug text itself cannot satisfy scene readiness.
- Chart route UI remains UI; only the authored chart table surface can support scene evidence as a ship-interior world anchor until a standalone #20 contract exists.
- Story 004 owns implementation-feedback routing and release handoff.
