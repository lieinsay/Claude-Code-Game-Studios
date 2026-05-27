---
name: godot-asset-interview
description: "Codex adapter for the original CCGS `godot-asset-interview` workflow. Canonical source: `.claude/skills/godot-asset-interview/SKILL.md`. Project-local adapter for the vendored Godot asset interview workflow. Use when the user wants to create, plan, specify, or refine Godot scenes, world units, dynamic entities, UI, data resources, interaction abilities, VFX, audio, navigation/physics assets, system managers, or composite features before Godot AI MCP execution."
---

# Codex Adapter: `godot-asset-interview`

Canonical source: `../../../.claude/skills/godot-asset-interview/SKILL.md`

## How To Use This Adapter

1. Read the canonical source file before acting.
2. Follow its workflow in Codex.
3. Translate Claude-only constructs as follows:
   - `AskUserQuestion` -> ask the user directly only when blocked; otherwise
     continue with the narrowest safe assumption.
   - `Task` / subagents -> use Codex delegation only when the user explicitly
     requests parallel or delegated agent work.
   - `Write` / `Edit` -> use `apply_patch` for manual edits.
   - `Bash` -> use the available Codex shell tool with minimal, verifiable
     commands.
   - `WebSearch` -> use Codex web/context documentation tools with primary or
     official sources.
4. Treat `.claude/docs/*`, `.claude/rules/*`, and linked templates as
   canonical until a repo-local Codex-native replacement exists.
5. If the canonical source conflicts with repo `AGENTS.md`, follow
   `AGENTS.md` first.

## Original Description

Project-local adapter for the vendored Godot asset interview workflow. Use when the user wants to create, plan, specify, or refine Godot scenes, world units, dynamic entities, UI, data resources, interaction abilities, VFX, audio, navigation/physics assets, system managers, or composite features before Godot AI MCP execution.

## Maintenance

- Do not edit this generated file directly.
- Update `.claude/skills/godot-asset-interview/SKILL.md` or `tools/sync_codex_adapters.py`, then run:

```bash
python tools/sync_codex_adapters.py
```
