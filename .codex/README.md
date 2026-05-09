# Codex Project Home

Last updated: 2026-05-09

This directory is the native Codex surface for Claude Code Game Studios. It is
generated from `.claude/` so Codex can work without breaking Claude Code users.

## Entry Points

- `../AGENTS.md`: repository-level Codex contract.
- `project.toml`: machine-readable map of entrypoints and branch roles.
- `agents/*.toml`: migrated studio role prompts.
- `skills/*/SKILL.md`: migrated workflow skills.
- `docs/`: migrated project process docs and templates.
- `rules/`: path-scoped coding and content rules.
- `hooks/`: copied hook scripts for manual/workflow use.

## Canonical Source

`.claude/` remains the source of truth. To update this directory, change the
matching `.claude` source and run:

```bash
python tools/sync_codex_from_claude.py
python tools/sync_codex_from_claude.py --check
```

## Branch Map

- `main`: clean Claude+Codex compatibility baseline.
- `codex/dual-agent-base`: named baseline reference matching `main`.
- `codex/clean-9ccc544`: untouched clean-start anchor at
  `9ccc5440af4b6e9cfa0014c993cf37c6a81f4222`.
- `develop`: long-lived project development branch.
