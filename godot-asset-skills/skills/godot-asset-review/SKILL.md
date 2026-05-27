---
name: godot-asset-review
description: Review Godot Asset Contracts for completeness, safety, MCP executability, and acceptance evidence. Use after godot-asset-interview or when the user provides a .godot-ai contract and wants review, approval, execution planning, audit, or review-then-execute handoff for Godot scenes, units, UI, resources, abilities, VFX, audio, physics, managers, or composite features.
---

# Godot Asset Review

## Purpose

Use this skill to decide whether a Godot Asset Contract is ready for execution. This skill reviews contracts, writes review artifacts, and may hand off to `godot-asset-execute` when the user explicitly asks for reviewed automatic execution. It does not directly modify Godot projects.

## Inputs

Accept any of:

- `.godot-ai/contracts/<asset-type>/<stable-id>.contract.md`
- A contract pasted by the user
- A source requirement file plus an instruction to review the generated contract

If no contract exists, route to `godot-asset-interview` instead of inventing a final contract.

## Project Artifacts

Write review outputs under:

```text
.godot-ai/
  reviews/<asset-type>/<stable-id>.review.md
  execution-plans/<asset-type>/<stable-id>.execution-plan.md
```

Do not write back to source requirement files unless the user explicitly asks.

## Review Workflow

1. Read the contract and any referenced source requirements.
2. Read `references/review-rubric.md`.
3. Check the common schema and the asset-type-specific fields.
4. Identify blocking ambiguity, scope creep, unsafe actions, missing evidence, and missing Godot output paths.
5. Produce a verdict.
6. Write the review artifact and, when executable, an execution plan.
7. If the user requested reviewed automatic execution and `Can Execute: true`, hand off to `godot-asset-execute`.

## Verdict Contract

Every review must include:

```md
## Review Verdict
- Verdict: pass | pass-with-risks | blocked
- Can Execute: true | false
- Execution Mode Allowed: reviewed-auto | reviewed-manual | execute-direct-risk-accepted
- Blocking Issues:
- Risks:
- Required User Decisions:
- Recommended Execution Plan:
```

Rules:

- `pass + reviewed-auto`: may hand off to `godot-asset-execute` when the user asked for automatic execution after review.
- `pass + reviewed-manual`: ready, but wait for user execution request.
- `pass-with-risks`: execute only when the user accepted the risks; record the risks in the execution plan.
- `blocked`: do not execute; recommend `godot-asset-interview` or contract revision.
- `execute-direct-risk-accepted`: use only when the user explicitly said to skip review and execute directly; still record missing review coverage.

## Review Focus

Check these gates:

- Asset type is one of the supported taxonomy values.
- Stable ID exists and is path/name safe.
- Godot output paths are concrete enough to execute.
- Runtime authority is clear: owns, reads, emits, and must-not-own.
- In-scope and non-goals prevent accidental system expansion.
- Decision boundaries say what AI may decide and what needs confirmation.
- Acceptance evidence includes at least one appropriate proof surface: node/resource evidence, visual evidence, runtime evidence, log/test evidence.
- Composite contracts split independent child assets cleanly.
- Destructive edits, deletions, migrations, or replacement of existing nodes require explicit user approval.

## Execution Plan

When `Can Execute: true`, write a concise execution plan with:

- Contract path
- Review path
- Execution mode
- Assets to create or modify
- Godot AI MCP capabilities likely needed
- Verification evidence required
- Known risks to preserve in verification

Do not call Godot AI MCP write tools in this skill.
