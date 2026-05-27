# Godot Asset Review: scene_unit.prototype.chart_table

## Review Verdict

- Verdict: pass
- Can Execute: true
- Execution Mode Allowed: reviewed-auto
- Blocking Issues: None.
- Risks: Visual details are greybox; final art/audio and screenshot evidence remain downstream.
- Required User Decisions: None for non-destructive first implementation.
- Recommended Execution Plan: Create `ChartTable.tscn` / `ChartTable.cs`, register prototype/instance authoring data, wire HubRuntime ship-interior interaction to the table, verify through Godot load plus smoke/build.

## Rubric Notes

- Asset type is supported and stable ID is path/name safe.
- Godot output paths are concrete.
- Runtime authority is bounded to local table visual/interaction state.
- Non-goals prevent Chart/Navigation scope creep.
- Acceptance evidence includes node/resource, runtime, and smoke proof.
- No destructive edits or node deletion are required.
