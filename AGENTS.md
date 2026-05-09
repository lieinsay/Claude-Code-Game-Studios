# Claude Code Game Studios - Codex Compatibility

This repository is a Claude Code Game Studios project. Codex must preserve the
Claude Code contract while adding only Codex-specific operating guidance here.

## Source Of Truth

- Read `CLAUDE.md` first for the project structure, engine choice, coding
  standards, collaboration model, and directory-scoped guidance.
- Read any deeper `CLAUDE.md` files before editing files under those
  directories.
- Treat `.claude/` as project-owned Claude Code configuration. Do not rename,
  delete, or reformat Claude agents, skills, hooks, rules, or docs unless the
  task explicitly targets them.

## Codex Operating Rules

- Follow system, developer, and user instructions first.
- Use this file only as the Codex bridge; it must not override Claude Code's
  project files for Claude users.
- Keep changes small, reviewable, and reversible.
- Do not commit generated local state, session logs, engine caches, build
  output, secrets, or machine-local settings.
- Preserve the original Claude slash-command and subagent workflows.
- When adding Codex-specific project guidance, put it in `AGENTS.md` instead of
  editing `CLAUDE.md`, unless the change is intentionally shared with Claude.

## Branch Hygiene

- `codex/clean-9ccc544` is the clean baseline at
  `9ccc5440af4b6e9cfa0014c993cf37c6a81f4222`.
- `codex/dual-agent-base` is the shared Claude+Codex base and should contain
  only compatibility guidance on top of the clean baseline.
- Runtime or game-development work should live on a separate feature/runtime
  branch based on `codex/dual-agent-base`.
