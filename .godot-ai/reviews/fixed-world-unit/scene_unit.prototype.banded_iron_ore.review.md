# Godot Asset Review: scene_unit.prototype.banded_iron_ore

## Review Verdict

- Verdict: pass-with-risks
- Can Execute: true
- Execution Mode Allowed: reviewed-auto
- Blocking Issues: None.
- Risks: Godot AI MCP / editor session availability is unknown; resource reward ID may remain a placeholder until a later economy pass; final art quality is out of scope.
- Required User Decisions: None before non-destructive execution.
- Recommended Execution Plan: `.godot-ai/execution-plans/fixed-world-unit/scene_unit.prototype.banded_iron_ore.execution-plan.md`

## Rubric Notes

- Asset type is supported: `fixed-world-unit`.
- Stable ID is stable and matches the production unit spec.
- Godot outputs are concrete.
- Runtime authority does not duplicate Resources, persistence, navigation, or economy systems.
- In-scope states and non-goals are explicit.
- Acceptance evidence includes reusable unit, instance, visual state, overlap semantics, and smoke/log proof.

## Can Execute Rationale

The unit can be created non-destructively as a reusable greybox asset. Execution must stop if a required editor session is unavailable and the plan cannot be satisfied through approved MCP operations.

