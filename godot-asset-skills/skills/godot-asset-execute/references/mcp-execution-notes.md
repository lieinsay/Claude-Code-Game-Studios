# Godot AI MCP Execution Notes

Prefer the current connected Godot AI MCP tools over manual file edits when editor state matters.

## Safe Order

1. Read editor/session state.
2. Activate the intended session.
3. Inspect existing scenes/resources before modifying.
4. Create resources/scripts needed by scenes.
5. Create or update scenes/nodes.
6. Attach scripts and wire signals.
7. Save.
8. Verify with hierarchy, logs, screenshots, runs, or tests.

## Stop Conditions

Stop before editing when:

- No editor session exists and the contract requires editor operations.
- A target path already exists and overwrite permission is unclear.
- The plan deletes or replaces existing nodes without explicit approval.
- The contract lacks a decision that would materially change output.

## Verification Hints

- Use screenshots for visual/UI/scene/VFX evidence.
- Use hierarchy reads for node existence and organization.
- Use logs for script/runtime errors.
- Use smoke tests or project run for movement, interaction, return, and state transitions.
- Use resource reads/inspection for data assets.
