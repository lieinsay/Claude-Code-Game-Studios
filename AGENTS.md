# Claude Code Game Studios - Dual Agent Contract

This repository is a Claude Code Game Studios project with a Codex adapter
layer. The goal is bidirectional compatibility: Claude Code keeps using the
original `.claude/` system, while Codex gets repo-native entry points that route
back to the same canonical source.

## Read Order

1. Read this `AGENTS.md` for Codex-specific rules.
2. Read `.agentlens/INDEX.md` for repository navigation.
3. Read `CLAUDE.md` for the canonical project contract.
4. Read any deeper `CLAUDE.md` before editing files under that directory.
5. For workflows, read `.agents/skills/<skill>/SKILL.md`, then the linked
   canonical `.claude/skills/<skill>/SKILL.md`.
6. For studio roles, use `.agents/skills/ccgs-agent-router/SKILL.md`, then read
   the matching `.claude/agents/*.md` role file.

## Canonical Source

- `.claude/skills/*/SKILL.md` is the source of truth for workflow behavior.
- `.claude/agents/*.md` is the source of truth for studio role behavior.
- `.claude/docs/*`, `.claude/rules/*`, and `.claude/docs/templates/*` remain
  the source of truth for standards, templates, and process guidance.
- `.agents/skills/*/SKILL.md` files are Codex adapters. Do not hand-edit
  generated skill adapters; update the canonical `.claude` file or the sync
  script, then regenerate.

## Codex Translation Rules

- Claude `AskUserQuestion` means: ask the user only when the answer is truly
  blocking; otherwise continue with the narrowest safe assumption.
- Claude `Task` or subagent delegation means: use Codex native subagents only
  when the user explicitly asks for delegation, parallel agents, or subagents.
- Claude `Write` and `Edit` map to Codex file edits. For manual edits, prefer
  `apply_patch`.
- Claude `Bash` maps to the available Codex shell tool with minimal,
  verifiable commands.
- Claude `WebSearch` maps to Codex web/context documentation tools and should
  prefer official or primary sources.
- Claude slash commands such as `/start` map to Codex skill names such as
  `start`, `brainstorm`, `setup-engine`, or `dev-story`.

## Compatibility Boundaries

- Do not rename, delete, or reformat `.claude/` assets unless the task
  explicitly targets the Claude system.
- Do not duplicate workflow logic into `.agents/`; adapters should route back
  to `.claude/`.
- If a change should affect both Claude and Codex, edit the canonical `.claude`
  source first, then run `python tools/sync_codex_adapters.py`.
- If a change is Codex-only, keep it in `AGENTS.md`, `.agentlens/`, `.agents/`,
  or `tools/sync_codex_adapters.py`.
- Do not commit generated local state, session logs, engine caches, build
  output, secrets, or machine-local settings.

## Maintenance Commands

```bash
python tools/sync_codex_adapters.py
python tools/sync_codex_adapters.py --check
git diff --check
```

## Branch Hygiene

- `codex/clean-9ccc544` is the clean baseline at
  `9ccc5440af4b6e9cfa0014c993cf37c6a81f4222`.
- `codex/dual-agent-base` is the shared Claude+Codex base and should contain
  compatibility guidance only on top of the clean baseline.
- Runtime or game-development work should live on a separate feature/runtime
  branch based on `codex/dual-agent-base`.
