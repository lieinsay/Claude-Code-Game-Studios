---
name: godot-asset-interview
description: Interview the user to clarify any independent Godot game asset request into a reviewable Godot Asset Contract. Use when the user wants to create, plan, specify, or refine Godot scenes, world units, dynamic entities, UI, data resources, interaction abilities, VFX, audio, navigation/physics assets, system managers, or composite features, especially before Godot AI MCP execution.
---

# Godot Asset Interview

## Purpose

Use this skill to turn an unclear Godot asset idea into a concrete, reviewable contract. This skill interviews and writes artifacts only; it does not modify Godot projects, call Godot AI MCP write tools, or execute implementation.

The default pipeline is:

```text
godot-asset-interview -> godot-asset-review -> godot-asset-execute
```

## Project Artifacts

Store project-local outputs under `.godot-ai/` in the current repository or workspace:

```text
.godot-ai/
  context/
  interviews/
  contracts/
  reviews/
  execution-plans/
  verification/
```

Do not write back to source requirement files unless the user explicitly asks for that.

## Workflow

1. Create a task slug from the requested asset or source file.
2. Read any referenced requirement files before asking questions.
3. Save a context snapshot to `.godot-ai/context/<slug>-<timestamp>.md` with the original request, referenced files, known facts, constraints, and open questions.
4. Interview one round at a time until ambiguity is low enough and all hard gates pass.
5. Save the transcript summary to `.godot-ai/interviews/<slug>-<timestamp>.md`.
6. Save each final contract to `.godot-ai/contracts/<asset-type>/<stable-id>.contract.md`.

If the request is a composite feature, split it into multiple contracts and write a parent composite contract that lists child assets and dependencies.

## Interview Loop

Ask exactly one focused question per round. Show progress in this form:

```text
Round <n> | Target: <dimension> | Ambiguity: <percent>

<question>
```

Prefer questions that clarify human intent, boundaries, and acceptance evidence. Gather discoverable project facts with file reads or search instead of asking the user.

Use these ambiguity dimensions:

- Intent Clarity
- Asset Type Clarity
- Scope Clarity
- Runtime Boundary Clarity
- Visual/Interaction Contract Clarity
- Decision Boundary Clarity
- Acceptance Evidence Clarity
- Brownfield Integration Clarity

Use this scoring rule unless a stronger local rule exists:

```text
ambiguity = 1 - average(resolved dimension scores)
```

Treat `<= 0.20` as the default threshold for final contract generation. Continue interviewing if any hard gate is unresolved, even when the score is below threshold.

## Hard Gates

Do not emit a final contract until all are explicit:

- Asset type
- Stable ID
- In-scope behavior and outputs
- Out-of-scope/non-goals
- Decision boundaries: what AI may decide vs must ask
- Acceptance evidence
- Execution separation: interview must not execute

If gates are incomplete, emit only a draft summary and the next interview question.

## Asset Types

Classify each independent asset as one of:

- `scene`
- `fixed-world-unit`
- `dynamic-entity`
- `ui`
- `data-resource`
- `interaction-ability`
- `vfx`
- `audio`
- `navigation-physics`
- `system-manager`
- `composite-feature`

Read `references/contract-schema.md` when drafting the contract. Read `references/asset-type-guidance.md` when classification or type-specific sections are unclear.

## Contract Requirements

Use the mixed schema: common required fields plus asset-type-specific sections.

Every contract must include:

- Metadata
- Intent
- Godot outputs
- Runtime boundary
- Decision boundaries
- Acceptance evidence
- Execution readiness
- Asset-type-specific requirements
- Residual ambiguity

Prefer concrete paths, node names, signal names, resource IDs, and test/preview surfaces. Mark unknowns as blocking ambiguity rather than inventing important decisions.

## Final Output

End with:

- Contract path(s)
- Final ambiguity percentage
- Remaining non-blocking assumptions
- Recommended next step: `godot-asset-review`

Never claim the asset was created. This skill only creates the contract artifacts.
