---
name: godot-asset-review
description: "Project-local adapter for the vendored Godot asset review workflow. Use after godot-asset-interview or when reviewing a .godot-ai contract for completeness, safety, MCP executability, acceptance evidence, and execution planning."
argument-hint: "[.godot-ai contract path or source requirement path]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash
---

# Godot Asset Review Adapter

Canonical workflow source: `../../../godot-asset-skills/skills/godot-asset-review/SKILL.md`.

Use the vendored workflow exactly. It writes `.godot-ai/reviews/` and
`.godot-ai/execution-plans/` artifacts, and it must not directly modify Godot
scenes, resources, scripts, or runtime files.
