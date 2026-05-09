# AgentLens Index

Last updated: 2026-05-09

## How To Read This Repository

- `AGENTS.md`: Codex entry point, compatibility rules, and maintenance
  commands.
- `.agents/README.md`: explains the Codex adapter layer and why `.claude/`
  remains canonical.
- `.agents/skills/INDEX.md`: generated Codex skill list.
- `.agents/skills/ccgs-agent-router/SKILL.md`: routes Codex requests for studio
  roles to `.claude/agents/*.md`.
- `.claude/skills/*/SKILL.md`: canonical workflow definitions.
- `.claude/agents/*.md`: canonical studio role definitions.
- `.claude/docs/*`: process docs, templates, rule references, and engine
  guidance.

## Recommended Read Order

1. `AGENTS.md`
2. `.agents/README.md`
3. `.claude/docs/quick-start.md`
4. `.claude/docs/workflow-catalog.yaml`
5. The relevant `.agents/skills/*/SKILL.md`
6. The linked `.claude/skills/*/SKILL.md` or `.claude/agents/*.md`

## Directory Responsibilities

- `.agents/`: Codex adapter layer. It mirrors and routes to canonical Claude
  assets without owning workflow body text.
- `.claude/`: Claude Code structure and canonical studio definitions.
- `design/`: GDDs, visual design, narrative, and level design assets.
- `docs/`: architecture docs, ADRs, engine references, and examples.
- `production/`: stage planning, sprint tracking, release tracking, and session
  state.
- `src/`: game source code.
- `tests/`: test suites and verification assets.

## Maintenance Rules

- To change workflow behavior, edit `.claude/skills/*/SKILL.md`, then run
  `python tools/sync_codex_adapters.py`.
- To change a studio role, edit `.claude/agents/*.md`.
- To change Codex routing or generation behavior, edit `AGENTS.md`,
  `.agents/README.md`, `.agents/skills/ccgs-agent-router/SKILL.md`, or
  `tools/sync_codex_adapters.py`.
