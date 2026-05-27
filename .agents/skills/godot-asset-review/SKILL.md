---
name: godot-asset-review
description: "Codex adapter for the original CCGS `godot-asset-review` workflow. Canonical source: `.claude/skills/godot-asset-review/SKILL.md`. Project-local adapter for the vendored Godot asset review workflow. Use after godot-asset-interview or when reviewing a .godot-ai contract for completeness, safety, MCP executability, acceptance evidence, and execution planning."
---

# Codex Adapter: `godot-asset-review`

Canonical source: `../../../.claude/skills/godot-asset-review/SKILL.md`

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

Project-local adapter for the vendored Godot asset review workflow. Use after godot-asset-interview or when reviewing a .godot-ai contract for completeness, safety, MCP executability, acceptance evidence, and execution planning.

## Maintenance

- Do not edit this generated file directly.
- Update `.claude/skills/godot-asset-review/SKILL.md` or `tools/sync_codex_adapters.py`, then run:

```bash
python tools/sync_codex_adapters.py
```
