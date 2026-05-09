#!/usr/bin/env python3
"""Generate the native .codex surface from the canonical .claude tree."""

from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
CLAUDE = ROOT / ".claude"
CODEX = ROOT / ".codex"
LAST_UPDATED = "2026-05-09"
DEFAULT_MODEL = "gpt-5.5"
DEFAULT_REASONING = "medium"

FRONTMATTER_RE = re.compile(r"^---\n(.*?)\n---\n", re.DOTALL)


def parse_frontmatter(text: str) -> tuple[dict[str, str], str]:
    match = FRONTMATTER_RE.match(text)
    if not match:
        return {}, text

    data: dict[str, str] = {}
    for line in match.group(1).splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        data[key.strip()] = value.strip().strip('"').strip("'")
    return data, text[match.end() :]


def toml_string(value: str) -> str:
    return json.dumps(value, ensure_ascii=False)


def codex_text(text: str) -> str:
    rewritten = text.replace(".claude/", ".codex/")
    normalized = "\n".join(line.rstrip() for line in rewritten.splitlines())
    if rewritten.endswith("\n"):
        normalized += "\n"
    return normalized


def reset_dir(path: Path) -> None:
    if path.exists():
        shutil.rmtree(path)
    path.mkdir(parents=True, exist_ok=True)


def copy_tree(source: Path, target: Path, *, rewrite_paths: bool = True) -> None:
    reset_dir(target)
    for path in sorted(source.rglob("*")):
        if path.is_dir():
            continue
        rel = path.relative_to(source)
        out = target / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        if path.suffix.lower() in {".md", ".yaml", ".yml", ".json", ".txt"}:
            text = path.read_text(encoding="utf-8")
            if rewrite_paths:
                text = codex_text(text)
            out.write_text(text, encoding="utf-8", newline="\n")
        else:
            shutil.copy2(path, out)


def render_agent(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    meta, body = parse_frontmatter(text)
    name = meta.get("name", path.stem)
    description = meta.get("description", f"Codex migrated agent for {name}.")
    retained = {key: value for key, value in meta.items() if key not in {"name", "description"}}
    source = path.relative_to(ROOT).as_posix()
    instructions = (
        f"<identity>\n"
        f"You are the Codex Game Studios `{name}` agent, migrated from Claude Code Game Studios.\n"
        f"</identity>\n\n"
        f"<codex_compatibility>\n"
        f"- Source file: `{source}`.\n"
        f"- Original Claude-only frontmatter is guidance, not guaranteed runtime enforcement.\n"
        f"- Original metadata retained for operator awareness: {json.dumps(retained, ensure_ascii=False)}.\n"
        f"- Follow root `AGENTS.md`, `.codex/README.md`, and the role boundaries below.\n"
        f"</codex_compatibility>\n\n"
        f"{body.strip()}\n"
    )
    return "\n".join(
        [
            "# Codex Game Studios migrated agent",
            f"name = {toml_string(name)}",
            f"description = {toml_string(description)}",
            f"model = {toml_string(DEFAULT_MODEL)}",
            f"model_reasoning_effort = {toml_string(DEFAULT_REASONING)}",
            f"developer_instructions = {toml_string(instructions)}",
            "",
        ]
    )


def sync_agents() -> None:
    target = CODEX / "agents"
    reset_dir(target)
    for path in sorted((CLAUDE / "agents").glob("*.md")):
        (target / f"{path.stem}.toml").write_text(
            render_agent(path), encoding="utf-8", newline="\n"
        )


def render_skill(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    meta, body = parse_frontmatter(text)
    source = path.relative_to(ROOT).as_posix()
    retained = {key: value for key, value in meta.items() if key not in {"name", "description", "argument-hint"}}
    frontmatter = ["---"]
    for key in ("name", "description", "argument-hint"):
        if key in meta:
            frontmatter.append(f"{key}: {toml_string(meta[key])}")
    frontmatter.append("---")
    compatibility = (
        f"\n> Codex compatibility: migrated from `{source}`. Original Claude-specific metadata\n"
        f"> such as allowed tools, Task delegation, model hints, and structured input widgets is\n"
        f"> preserved as guidance, not guaranteed runtime enforcement. Original extra metadata:\n"
        f"> `{json.dumps(retained, ensure_ascii=False)}`.\n"
        f">\n"
        f"> Invocation in this template uses `${meta.get('name', path.parent.name)}`. If structured user input\n"
        f"> is unavailable, ask one concise plain-text question at a time. Codex native subagents\n"
        f"> are the default substitute for Claude `Task` only when delegation is explicitly requested.\n\n"
    )
    return "\n".join(frontmatter) + compatibility + codex_text(body.lstrip())


def sync_skills() -> None:
    target = CODEX / "skills"
    reset_dir(target)
    for path in sorted((CLAUDE / "skills").glob("*/SKILL.md")):
        out = target / path.parent.name / "SKILL.md"
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(render_skill(path), encoding="utf-8", newline="\n")


def sync_hooks() -> None:
    hooks = CODEX / "hooks"
    reset_dir(hooks)
    (hooks / "README.md").write_text(
        "# Hooks\n\n"
        "Hook scripts are copied for manual/workflow use. Codex global event "
        "interception is not guaranteed.\n",
        encoding="utf-8",
        newline="\n",
    )
    if (CLAUDE / "statusline.sh").exists():
        shutil.copy2(CLAUDE / "statusline.sh", hooks / "statusline.sh")
    scripts = hooks / "scripts"
    scripts.mkdir(parents=True, exist_ok=True)
    for path in sorted((CLAUDE / "hooks").glob("*")):
        if path.is_file():
            shutil.copy2(path, scripts / path.name)


def render_readme() -> str:
    return f"""# Codex Project Home

Last updated: {LAST_UPDATED}

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
"""


def render_project_toml() -> str:
    return f"""name = "Claude Code Game Studios"
compatibility = "claude-and-codex"
baseline_commit = "9ccc5440af4b6e9cfa0014c993cf37c6a81f4222"
updated = "{LAST_UPDATED}"

[entrypoints]
codex_contract = "AGENTS.md"
codex_home = ".codex/README.md"
agent_prompts = ".codex/agents"
skill_workflows = ".codex/skills"
docs = ".codex/docs"
rules = ".codex/rules"
hooks = ".codex/hooks"
adapter_layer = ".agents"
claude_contract = "CLAUDE.md"
sync_codex = "tools/sync_codex_from_claude.py"
sync_agents = "tools/sync_codex_adapters.py"

[canonical_sources]
root = ".claude"
workflows = ".claude/skills/*/SKILL.md"
roles = ".claude/agents/*.md"
docs = ".claude/docs/*"
rules = ".claude/rules/*"
hooks = ".claude/hooks/*"

[branches]
main = "clean Claude+Codex compatibility baseline"
develop = "long-lived active project development branch"
"codex/dual-agent-base" = "named compatibility baseline reference"
"codex/clean-9ccc544" = "untouched clean-start anchor"

[maintenance]
sync_codex = "python tools/sync_codex_from_claude.py"
check_codex = "python tools/sync_codex_from_claude.py --check"
sync_adapters = "python tools/sync_codex_adapters.py"
check_adapters = "python tools/sync_codex_adapters.py --check"
diff_check = "git diff --check"

[local_state]
do_not_commit = [".omx/", "production/session-logs/", ".claude/settings.local.json", "CLAUDE.local.md"]
"""


def expected_files() -> dict[Path, str]:
    files: dict[Path, str] = {
        CODEX / "README.md": render_readme(),
        CODEX / "project.toml": render_project_toml(),
    }
    for path in sorted((CLAUDE / "agents").glob("*.md")):
        files[CODEX / "agents" / f"{path.stem}.toml"] = render_agent(path)
    for path in sorted((CLAUDE / "skills").glob("*/SKILL.md")):
        files[CODEX / "skills" / path.parent.name / "SKILL.md"] = render_skill(path)
    return files


def sync_all() -> None:
    CODEX.mkdir(parents=True, exist_ok=True)
    (CODEX / "README.md").write_text(render_readme(), encoding="utf-8", newline="\n")
    (CODEX / "project.toml").write_text(render_project_toml(), encoding="utf-8", newline="\n")
    sync_agents()
    copy_tree(CLAUDE / "docs", CODEX / "docs")
    sync_hooks()
    copy_tree(CLAUDE / "rules", CODEX / "rules")
    sync_skills()


def check_all() -> int:
    mismatches: list[str] = []
    for path, content in expected_files().items():
        if not path.exists():
            mismatches.append(f"MISSING {path.relative_to(ROOT)}")
            continue
        current = path.read_text(encoding="utf-8")
        if current != content:
            mismatches.append(f"STALE   {path.relative_to(ROOT)}")

    for rel in ("docs", "rules"):
        source_files = sorted((CLAUDE / rel).rglob("*"))
        for source in source_files:
            if source.is_dir():
                continue
            target = CODEX / rel / source.relative_to(CLAUDE / rel)
            if not target.exists():
                mismatches.append(f"MISSING {target.relative_to(ROOT)}")

    expected_hook_files = [CODEX / "hooks" / "README.md"]
    if (CLAUDE / "statusline.sh").exists():
        expected_hook_files.append(CODEX / "hooks" / "statusline.sh")
    expected_hook_files.extend(
        CODEX / "hooks" / "scripts" / source.name
        for source in sorted((CLAUDE / "hooks").glob("*"))
        if source.is_file()
    )
    for path in expected_hook_files:
        if not path.exists():
            mismatches.append(f"MISSING {path.relative_to(ROOT)}")

    if mismatches:
        print("Codex native surface is not in sync:")
        for item in mismatches:
            print(f"- {item}")
        print("Run: python tools/sync_codex_from_claude.py")
        return 1

    print("Codex native surface is in sync.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="Verify .codex output.")
    args = parser.parse_args()

    if not CLAUDE.exists():
        print(f"Missing canonical Claude directory: {CLAUDE}", file=sys.stderr)
        return 1

    if args.check:
        return check_all()

    sync_all()
    print("Wrote native Codex surface under .codex.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
