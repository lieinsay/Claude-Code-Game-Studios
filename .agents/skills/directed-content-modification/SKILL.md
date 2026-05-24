---
name: directed-content-modification
description: "Codex adapter for the original CCGS `directed-content-modification` workflow. Canonical source: `.claude/skills/directed-content-modification/SKILL.md`. Directed modification workflow for an existing scene, UI surface, or scene unit. Use when the user asks to change, revise, tune, rework, polish, or update a specific scene/UI/unit that already exists, while keeping its specification, Godot/data implementation, assets, and verification evidence synchronized. If the requested change introduces a new scene/UI/unit, route only the new object through the creation suitability gate first."
---

# Codex Adapter: `directed-content-modification`

Canonical source: `../../../.claude/skills/directed-content-modification/SKILL.md`

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

Directed modification workflow for an existing scene, UI surface, or scene unit. Use when the user asks to change, revise, tune, rework, polish, or update a specific scene/UI/unit that already exists, while keeping its specification, Godot/data implementation, assets, and verification evidence synchronized. If the requested change introduces a new scene/UI/unit, route only the new object through the creation suitability gate first.

## Maintenance

- Do not edit this generated file directly.
- Update `.claude/skills/directed-content-modification/SKILL.md` or `tools/sync_codex_adapters.py`, then run:

```bash
python tools/sync_codex_adapters.py
```
