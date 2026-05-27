---
name: "directed-content-modification"
description: "Directed modification workflow for an existing scene, UI surface, or scene unit. Use when the user asks to change, revise, tune, rework, polish, or update a specific scene/UI/unit that already exists, while keeping its specification, Godot/data implementation, assets, and verification evidence synchronized. If the requested change introduces a new scene/UI/unit, route only the new object through the creation suitability gate first."
argument-hint: "[scene|ui|unit]:[id-or-file] [requested change]"
---
> Codex compatibility: migrated from `.claude/skills/directed-content-modification/SKILL.md`. Original Claude-specific metadata
> such as allowed tools, Task delegation, model hints, and structured input widgets is
> preserved as guidance, not guaranteed runtime enforcement. Original extra metadata:
> `{"user-invocable": "true", "allowed-tools": "Read, Glob, Grep, Write, Edit, Bash"}`.
>
> Invocation in this template uses `$directed-content-modification`. If structured user input
> is unavailable, ask one concise plain-text question at a time. Codex native subagents
> are the default substitute for Claude `Task` only when delegation is explicitly requested.

# Directed Content Modification

Use this workflow when the user asks to modify an already-approved scene, UI, or
unit. This is not a new-content creation path. It keeps the existing
specification and implementation aligned while preserving independent object
boundaries.

## 1. Resolve Target

Identify the target object and load its governing files:

- Scene: `production/scene-specs/*.md`
- UI: `production/ui-specs/*.md`
- Fixed unit: `production/unit-specs/fixed-scene-objects/*.md`
- Dynamic unit: `production/unit-specs/dynamic-entities/*.md`

Then find implementation references:

- Godot scenes / components under `src/`
- C# / GDScript runtime or presenter files under `src/`
- Authoring data such as `src/presentation/playable_slice_authored_content.json`
- Existing tests, smoke probes, screenshots, and QA evidence under `tests/` and
  `production/qa/evidence/`

If the target cannot be found, stop and ask the user for the intended object.

## 2. Classify Scope

Classify the requested change before editing:

| Scope | Action |
| --- | --- |
| Existing-object modification | Continue in this workflow. |
| Adds a new scene/UI/unit | Pause only that new object and request creation suitability approval via `production/content-creation-review-gate.md`; after approval, route that object through `godot-asset-interview -> godot-asset-review -> godot-asset-execute` before production Godot implementation. |
| Deletes non-compliant legacy Godot nodes | Ask the user before deletion; list file, node names, reason, and replacement path. |
| Cross-system redesign | Recommend the appropriate design workflow before implementation. |

Examples that stay in this workflow:

- Move a scene interaction anchor.
- Change a UI's priority, opening condition, focus behavior, or layout boundary.
- Adjust a unit's collision, occlusion, scale, state, asset group, or recovery
  rule.
- Replace an approved object's implementation with a cleaner independent scene
  or asset group.

Examples that trigger a new creation gate:

- Add a new NPC to a scene.
- Add a new modal or HUD surface.
- Add a new reusable resource node, obstacle, or dynamic entity.
- Split part of a scene into a new enterable scene.

## 3. Preserve Independent Boundaries

Every edited object must remain independently traceable:

- A scene should keep or gain an independent `.tscn`, asset group, authoring data,
  or runtime boundary.
- A UI surface should keep or gain an independent `.tscn`, component, registry
  entry, or asset group.
- A unit prototype should keep or gain an independent scene/resource/data
  prototype or asset group.

Do not solve the change by scattering hidden nodes into a legacy Godot scene or a
large runtime script. Large files may compose, mount, or reference the object;
they should not become the object's only implementation.

## 4. Edit Specification First

Update the relevant specification before or alongside implementation:

- Record the requested change and rationale.
- Update identity, boundaries, interaction rules, data/runtime contract, assets,
  QA evidence, and user notes as needed.
- If the change follows a user experience验收 note, preserve that note and mark
  how it was addressed.
- If the change invalidates prior evidence, mark the old evidence stale and name
  the replacement evidence needed.

No second formal user review is required before implementation. The user can
continue modifying the object after验收.

## 5. Implement Narrowly

Apply the smallest implementation change that satisfies the updated spec:

- Edit Godot scenes, C# / GDScript, authoring data, and assets only within the
  target object's boundary or its mounting references.
- Avoid unrelated refactors.
- Do not add new dependencies.
- If implementation exposes a missing new object, stop and route that object
  through the creation gate and the required `.godot-ai` asset workflow.
- For approved new Godot scene/UI/unit assets, require the matching
  `.godot-ai/contracts/`, `.godot-ai/reviews/`, `.godot-ai/execution-plans/`,
  and `.godot-ai/verification/` artifacts from `godot-asset-skills`; use Godot AI
  MCP through `addons/godot_ai` when executing scene/resource/node work.

## 6. Verify

Run verification proportional to risk:

- Documentation-only update: run formatting / diff checks.
- Data or C# change: run the focused unit/integration test where available.
- Godot scene/UI change: run the relevant smoke probe and refresh screenshot or
  visual evidence when possible.
- Boundary change: verify the object can still be traced from spec to runtime
  implementation and evidence.

## 7. Report

Final report must include:

- Target object changed.
- Specification file updated.
- Implementation / asset files updated.
- Verification run and result.
- Any stale evidence or remaining user体验 check.
- Whether the change stayed inside the existing object or triggered a new
  creation gate.

Verdict vocabulary:

- `UPDATED`: spec and implementation are synchronized.
- `UPDATED_PENDING_EXPERIENCE_CHECK`: implementation is done but user needs to
  inspect feel/readability.
- `BLOCKED_NEW_OBJECT`: requested change needs a new scene/UI/unit approval.
- `BLOCKED_LEGACY_DELETE_CONFIRMATION`: deletion requires explicit user
  confirmation.
