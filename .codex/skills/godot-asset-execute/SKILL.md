---
name: "godot-asset-execute"
description: "Project-local adapter for the vendored Godot asset execution workflow. Use to execute approved or explicitly risk-accepted Godot Asset Contracts through Godot AI MCP and record verification evidence."
argument-hint: "[.godot-ai execution plan, review, or contract path]"
---
> Codex compatibility: migrated from `.claude/skills/godot-asset-execute/SKILL.md`. Original Claude-specific metadata
> such as allowed tools, Task delegation, model hints, and structured input widgets is
> preserved as guidance, not guaranteed runtime enforcement. Original extra metadata:
> `{"user-invocable": "true", "allowed-tools": "Read, Glob, Grep, Write, Edit, Bash"}`.
>
> Invocation in this template uses `$godot-asset-execute`. If structured user input
> is unavailable, ask one concise plain-text question at a time. Codex native subagents
> are the default substitute for Claude `Task` only when delegation is explicitly requested.

# Godot Asset Execute Adapter

Canonical workflow source: `../../../godot-asset-skills/skills/godot-asset-execute/SKILL.md`.

Use the vendored workflow exactly. Prefer Godot AI MCP through
`addons/godot_ai` for scene/resource/node work, and write verification evidence
under `.godot-ai/verification/`.
