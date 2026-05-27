# Godot Asset Contract Review Rubric

Use this rubric to produce the review verdict.

## Blocking Issues

Mark `blocked` when any is true:

- Asset type is missing or unsupported.
- Stable ID is missing.
- Godot outputs are too vague to locate or create.
- In-scope and non-goals are not explicit.
- Runtime authority is unclear enough to create duplicate systems.
- Acceptance evidence is missing.
- Contract requires destructive edits without explicit path-level approval.
- Composite feature bundles independent assets without child contracts.
- Required user decisions remain open.

## Pass With Risks

Use `pass-with-risks` when execution is possible but notable risk remains:

- Visual details are under-specified but allowed for AI discretion.
- Existing project integration points are inferred but not proven.
- Some evidence is manual instead of automated.
- Godot editor availability is uncertain.
- Asset paths may need minor adjustment to match project conventions.

## Pass

Use `pass` when:

- Required fields are concrete.
- Non-goals prevent scope creep.
- Decision boundaries are clear.
- Execution plan can be written without inventing major behavior.
- Acceptance evidence can prove completion.

## Evidence Expectations

- Scenes: hierarchy, screenshots, movement/interaction smoke.
- Fixed units: prefab/scene path, states, collision/overlap, instance evidence.
- Dynamic entities: runtime behavior, state transitions, collisions, logs/tests.
- UI: screenshots for states, focus/input behavior, Control tree.
- Data resources: schema/path/defaults, load/inspect evidence.
- Abilities: trigger/eligibility/success/failure event proof.
- VFX/audio: preview or runtime trigger evidence.
- Navigation/physics: reachable/unreachable and collision proof.
- System managers: API/tests/signals/persistence evidence.
