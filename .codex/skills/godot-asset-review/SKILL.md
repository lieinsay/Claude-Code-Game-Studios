---
name: "godot-asset-review"
description: "Project-local adapter for the vendored Godot asset review workflow. Use after godot-asset-interview or when reviewing a .godot-ai contract for completeness, safety, MCP executability, acceptance evidence, and execution planning."
argument-hint: "[.godot-ai contract path or source requirement path]"
---
> Codex compatibility: migrated from `.claude/skills/godot-asset-review/SKILL.md`. Original Claude-specific metadata
> such as allowed tools, Task delegation, model hints, and structured input widgets is
> preserved as guidance, not guaranteed runtime enforcement. Original extra metadata:
> `{"user-invocable": "true", "allowed-tools": "Read, Glob, Grep, Write, Edit, Bash"}`.
>
> Invocation in this template uses `$godot-asset-review`. If structured user input
> is unavailable, ask one concise plain-text question at a time. Codex native subagents
> are the default substitute for Claude `Task` only when delegation is explicitly requested.

# Godot Asset Review Adapter

Canonical workflow source: `../../../godot-asset-skills/skills/godot-asset-review/SKILL.md`.

Use the vendored workflow exactly. It writes `.godot-ai/reviews/` and
`.godot-ai/execution-plans/` artifacts, and it must not directly modify Godot
scenes, resources, scripts, or runtime files.
