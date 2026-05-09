---
name: story-done
description: "Codex adapter for the original CCGS `story-done` workflow. Canonical source: `.claude/skills/story-done/SKILL.md`. End-of-story completion review. Reads the story file, verifies each acceptance criterion against the implementation, checks for GDD/ADR deviations, prompts code review, updates story status to Complete, and surfaces the next ready story from the sprint."
---

# Codex Adapter: `story-done`

Canonical source: `../../../.claude/skills/story-done/SKILL.md`

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

End-of-story completion review. Reads the story file, verifies each acceptance criterion against the implementation, checks for GDD/ADR deviations, prompts code review, updates story status to Complete, and surfaces the next ready story from the sprint.

## Maintenance

- Do not edit this generated file directly.
- Update `.claude/skills/story-done/SKILL.md` or `tools/sync_codex_adapters.py`, then run:

```bash
python tools/sync_codex_adapters.py
```
