---
name: "godot-asset-interview"
description: "Project-local adapter for the vendored Godot asset interview workflow. Use when the user wants to create, plan, specify, or refine Godot scenes, world units, dynamic entities, UI, data resources, interaction abilities, VFX, audio, navigation/physics assets, system managers, or composite features before Godot AI MCP execution."
argument-hint: "[asset request or source requirement path]"
---
> Codex compatibility: migrated from `.claude/skills/godot-asset-interview/SKILL.md`. Original Claude-specific metadata
> such as allowed tools, Task delegation, model hints, and structured input widgets is
> preserved as guidance, not guaranteed runtime enforcement. Original extra metadata:
> `{"user-invocable": "true", "allowed-tools": "Read, Glob, Grep, Write, Edit, Bash"}`.
>
> Invocation in this template uses `$godot-asset-interview`. If structured user input
> is unavailable, ask one concise plain-text question at a time. Codex native subagents
> are the default substitute for Claude `Task` only when delegation is explicitly requested.

# Godot Asset Interview Adapter

Canonical workflow source: `../../../godot-asset-skills/skills/godot-asset-interview/SKILL.md`.

Use the vendored workflow exactly. It creates `.godot-ai/context/`,
`.godot-ai/interviews/`, and `.godot-ai/contracts/` artifacts only; it must not
modify Godot scenes, resources, scripts, or runtime files.
