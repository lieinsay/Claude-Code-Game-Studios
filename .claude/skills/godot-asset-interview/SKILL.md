---
name: godot-asset-interview
description: "Project-local adapter for the vendored Godot asset interview workflow. Use when the user wants to create, plan, specify, or refine Godot scenes, world units, dynamic entities, UI, data resources, interaction abilities, VFX, audio, navigation/physics assets, system managers, or composite features before Godot AI MCP execution."
argument-hint: "[asset request or source requirement path]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash
---

# Godot Asset Interview Adapter

Canonical workflow source: `../../../godot-asset-skills/skills/godot-asset-interview/SKILL.md`.

Use the vendored workflow exactly. It creates `.godot-ai/context/`,
`.godot-ai/interviews/`, and `.godot-ai/contracts/` artifacts only; it must not
modify Godot scenes, resources, scripts, or runtime files.
