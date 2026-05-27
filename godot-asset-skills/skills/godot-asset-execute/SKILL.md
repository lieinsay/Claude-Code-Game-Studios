---
name: godot-asset-execute
description: Execute approved or explicitly risk-accepted Godot Asset Contracts using Godot AI MCP, then verify and record evidence. Use when the user asks to execute, implement, create, build, or generate a Godot asset from a .godot-ai contract or execution plan, including direct execution without review when the user explicitly accepts that risk.
---

# Godot Asset Execute

## Purpose

Use this skill to implement a Godot Asset Contract with Godot AI MCP and produce verification evidence. This skill does not re-interview requirements unless the contract is too incomplete to execute.

## Inputs

Accept any of:

- `.godot-ai/execution-plans/<asset-type>/<stable-id>.execution-plan.md`
- `.godot-ai/reviews/<asset-type>/<stable-id>.review.md`
- `.godot-ai/contracts/<asset-type>/<stable-id>.contract.md`

Prefer executing from an execution plan produced by `godot-asset-review`.

## Execution Gate

Execute only when one is true:

- A review says `Can Execute: true` and `Execution Mode Allowed: reviewed-auto` or `reviewed-manual`.
- The user explicitly says to execute without review and accepts direct-execution risk.

If neither is true, route to `godot-asset-review`.

Do not perform destructive operations such as deleting existing scenes, replacing existing nodes, or migrating project structure unless the contract or user explicitly approves exact paths.

## Project Artifacts

Write verification outputs under:

```text
.godot-ai/verification/<asset-type>/<stable-id>.verification.md
```

If executing without review, also write a minimal risk record under:

```text
.godot-ai/reviews/<asset-type>/<stable-id>.review.md
```

## Godot AI MCP Use

Read `references/mcp-execution-notes.md` before performing MCP operations.

Use Godot AI MCP when available. Start with read/status operations:

- `session_manage(op="list")` or equivalent session listing
- `session_activate` when needed
- `editor_state`
- `scene_get_hierarchy` or resource/file reads

Then perform only the operations required by the contract, such as:

- `scene_manage`
- `node_create`
- `node_set_property`
- `script_create`
- `script_attach`
- `resource_manage`
- `ui_manage`
- `theme_manage`
- `animation_manage`
- `material_manage`
- `audio_manage`
- `particle_manage`
- `camera_manage`
- `input_map_manage`
- `scene_save`

If Godot editor session is required but unavailable, stop and report the blocker. File-only assets may proceed without an editor only when the contract allows file-level generation and the required tools are available.

## Execution Workflow

1. Read the contract, review, and execution plan.
2. Confirm the execution gate.
3. Inspect the current Godot/project state before modifying anything.
4. Create or modify assets in the smallest safe order: resources/scripts first when needed, then scenes/nodes, then wiring.
5. Save changed scenes/resources.
6. Run applicable checks: project run, scene load, smoke tests, logs, screenshots, hierarchy reads, or resource inspection.
7. Iterate on fixable failures until the contract's acceptance evidence is satisfied or a real blocker remains.
8. Write verification evidence.

## Verification Report

Every execution must produce:

```md
## Verification Summary
- Contract:
- Review:
- Execution Mode:
- Result: pass | partial | blocked
- Changed Godot Outputs:
- Evidence:
- Failed Checks:
- Risks Preserved:
- Follow-up Needed:
```

Evidence should cite concrete node paths, resource paths, screenshots, logs, test results, or runtime observations. Do not claim completion from intent alone.

## Direct Execution Without Review

When the user explicitly requests direct execution:

1. Write `Execution Mode Allowed: execute-direct-risk-accepted`.
2. Record which review checks were skipped.
3. Execute only non-destructive actions that are clear from the contract.
4. Preserve skipped-review risks in verification.

If a missing decision would materially change the asset, stop and ask rather than guessing.
