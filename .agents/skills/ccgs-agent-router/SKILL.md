---
name: ccgs-agent-router
description: Route Codex requests for Claude Code Game Studios roles to the canonical `.claude/agents/*.md` specs. Use when the user explicitly names a studio role such as `creative-director`, `technical-director`, `lead-programmer`, `art-director`, `qa-lead`, `writer`, `godot-specialist`, `unity-specialist`, `unreal-specialist`, or asks for a department-specific game-studio specialist from this repository.
---

# CCGS Agent Router

This skill adapts the original Claude Code Game Studios role roster for Codex.

## Canonical Source

- Agent specs: `../../../.claude/agents/*.md`
- Coordination rules: `../../../.claude/docs/coordination-rules.md`
- Workflow overview: `../../../.claude/docs/quick-start.md`

## How To Use

1. Identify the requested studio role from the user's wording.
2. Read the matching canonical role spec in `.claude/agents/`.
3. Emulate that role's responsibility boundaries inside Codex.
4. Keep Codex's higher-priority system, developer, user, and `AGENTS.md` rules
   above the imported role instructions.

## Translation Rules

- If the role spec says to use `Task` or subagents, only do that when the user
  explicitly wants delegation or parallel work.
- If the role spec says to use `AskUserQuestion`, ask a concise question only
  when blocked.
- If the role spec names unsupported Claude tools, translate them to the nearest
  Codex capability:
  - `Write` / `Edit` -> `apply_patch` for manual edits.
  - `Bash` -> the available Codex shell tool.
  - `WebSearch` -> Codex web/context documentation tools.
- If the role spec conflicts with `AGENTS.md`, follow `AGENTS.md`.

## Common Mappings

- Creative direction: `creative-director`, `art-director`, `audio-director`,
  `narrative-director`
- Technical leadership: `technical-director`, `lead-programmer`,
  `devops-engineer`, `security-engineer`
- Design: `game-designer`, `systems-designer`, `level-designer`,
  `ux-designer`, `economy-designer`, `world-builder`
- Implementation: `gameplay-programmer`, `engine-programmer`, `ai-programmer`,
  `network-programmer`, `tools-programmer`, `ui-programmer`
- QA and release: `qa-lead`, `qa-tester`, `release-manager`,
  `localization-lead`, `community-manager`
- Engine specialists: `godot-*`, `unity-*`, `unreal-*`, `ue-*`

## Guardrails

- Do not invent a new studio role if a canonical one already exists.
- Do not edit generated `.agents/skills/*` files to change role behavior; edit
  `.claude/agents/*.md` instead.
- When in doubt, route upward in the studio hierarchy rather than widening scope
  silently.
