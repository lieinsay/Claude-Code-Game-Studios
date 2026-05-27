---
name: godot-asset-execute
description: "Project-local adapter for the vendored Godot asset execution workflow. Use to execute approved or explicitly risk-accepted Godot Asset Contracts through Godot AI MCP and record verification evidence."
argument-hint: "[.godot-ai execution plan, review, or contract path]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash
---

# Godot Asset Execute Adapter

Canonical workflow source: `../../../godot-asset-skills/skills/godot-asset-execute/SKILL.md`.

Use the vendored workflow exactly. Prefer Godot AI MCP through
`addons/godot_ai` for scene/resource/node work, and write verification evidence
under `.godot-ai/verification/`.
