---
name: asset-audit
description: "Codex adapter for the original CCGS `asset-audit` workflow. Canonical source: `.claude/skills/asset-audit/SKILL.md`. Audits game assets for compliance with naming conventions, file size budgets, format standards, and pipeline requirements. Identifies orphaned assets, missing references, and standard violations."
---

# Codex Adapter: `asset-audit`

Canonical source: `../../../.claude/skills/asset-audit/SKILL.md`

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

Audits game assets for compliance with naming conventions, file size budgets, format standards, and pipeline requirements. Identifies orphaned assets, missing references, and standard violations.

## Maintenance

- Do not edit this generated file directly.
- Update `.claude/skills/asset-audit/SKILL.md` or `tools/sync_codex_adapters.py`, then run:

```bash
python tools/sync_codex_adapters.py
```
