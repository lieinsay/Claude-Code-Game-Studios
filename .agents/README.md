# Codex Adapter Layer

Last updated: 2026-05-09

This directory makes the Claude-first studio template directly consumable from
Codex without maintaining a second copy of the workflows.

## Design Principles

- `.claude/` remains the canonical source.
- `.agents/skills/*/SKILL.md` files are adapters for Codex. They tell Codex
  which canonical skill to read and how to translate Claude-only tool language.
- Generated adapters should stay thin so Claude and Codex do not drift apart.

## What To Edit

- Workflow text: `.claude/skills/*/SKILL.md`
- Studio roles: `.claude/agents/*.md`
- Codex adapter generation: `tools/sync_codex_adapters.py`
- Codex-only routing notes: `AGENTS.md`, `.agentlens/INDEX.md`, or this file

## Resync

```bash
python tools/sync_codex_adapters.py
python tools/sync_codex_adapters.py --check
```

## Additional Entry Point

- `.agents/skills/ccgs-agent-router/SKILL.md` routes requests for studio roles
  such as `creative-director`, `lead-programmer`, `qa-lead`, `art-director`,
  `godot-specialist`, `unity-specialist`, or `unreal-specialist` to the
  canonical `.claude/agents/*.md` definitions.
