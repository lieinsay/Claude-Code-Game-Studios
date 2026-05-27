---
name: "dev-story"
description: "Read a story file and implement it. Loads the full context (story, GDD requirement, ADR guidelines, control manifest), routes to the right programmer agent for the system and engine, implements the code and test, and confirms each acceptance criterion. The core implementation skill — run after /story-readiness, before /code-review and /story-done."
argument-hint: "[story-path]"
---
> Codex compatibility: migrated from `.claude/skills/dev-story/SKILL.md`. Original Claude-specific metadata
> such as allowed tools, Task delegation, model hints, and structured input widgets is
> preserved as guidance, not guaranteed runtime enforcement. Original extra metadata:
> `{"user-invocable": "true", "allowed-tools": "Read, Glob, Grep, Write, Bash, Task, AskUserQuestion"}`.
>
> Invocation in this template uses `$dev-story`. If structured user input
> is unavailable, ask one concise plain-text question at a time. Codex native subagents
> are the default substitute for Claude `Task` only when delegation is explicitly requested.

# Dev Story

This skill bridges planning and code. It reads a story file in full, assembles
all the context a programmer needs, routes to the correct specialist agent, and
drives implementation to completion — including writing the test.

**The loop for every story:**
```
/qa-plan sprint           ← define test requirements before sprint begins
/story-readiness [path]   ← validate before starting
/dev-story [path]         ← implement it  (this skill)
/code-review [files]      ← review it
/story-done [path]        ← verify and close it
```

**After all sprint stories are done:** run `/team-qa sprint` to execute the full QA cycle and get a sign-off verdict before advancing the project stage.

**Output:** Source code + test file in the project's `src/` and `tests/` directories.

---

## Phase 1: Find the Story

**If a path is provided**: read that file directly.

**If no argument**: check `production/session-state/active.md` for the active
story. If found, confirm: "Continuing work on [story title] — is that correct?"
If not found, ask: "Which story are we implementing?" Glob
`production/epics/**/*.md` and list stories with Status: Ready.

---

## Phase 2: Load Full Context

**Before loading any context, verify required files exist.** Extract the ADR path from the story's `ADR Governing Implementation` field, then check:

| File | Path | If missing |
|------|------|------------|
| TR registry | `docs/architecture/tr-registry.yaml` | **STOP** — "TR registry not found. Run `/create-epics` to generate it." |
| Governing ADR | path from story's ADR field | **STOP** — "ADR file [path] not found. Run `/architecture-decision` to create it, or correct the filename in the story's ADR field." |
| Control manifest | `docs/architecture/control-manifest.md` | **WARN and continue** — "Control manifest not found — layer rules cannot be checked. Run `/create-control-manifest`." |

If the TR registry or governing ADR is missing, set the story status to **BLOCKED** in the session state and do not spawn any programmer agent.

Read all of the following simultaneously — these are independent reads. Do not start implementation until all context is loaded:

### The story file
Extract and hold:
- **Story title, ID, layer, type** (Logic / Integration / Visual/Feel / UI / Config/Data)
- **TR-ID** — the GDD requirement identifier
- **Governing ADR** reference
- **Manifest Version** embedded in story header
- **Acceptance Criteria** — every checkbox item, verbatim
- **Implementation Notes** — the ADR guidance section in the story
- **Out of Scope** boundaries
- **Test Evidence** — the required test file path
- **Dependencies** — what must be DONE before this story

### The TR registry
Read `docs/architecture/tr-registry.yaml`. Look up the story's TR-ID.
Read the current `requirement` text — this is the source of truth for what the
GDD requires now. Do not rely on any inline text in the story file (may be stale).

### The governing ADR
Read `docs/architecture/[adr-file].md`. Extract:
- The full Decision section
- The Implementation Guidelines section (this is what the programmer follows)
- The Engine Compatibility section (post-cutoff APIs, known risks)
- The ADR Dependencies section

### The control manifest
Read `docs/architecture/control-manifest.md`. Extract the rules for this story's layer:
- Required patterns
- Forbidden patterns
- Performance guardrails

Check: does the story's embedded Manifest Version match the current manifest header date?
If they differ, use `AskUserQuestion` before proceeding:
- Prompt: "Story was written against manifest v[story-date]. Current manifest is v[current-date]. New rules may apply. How do you want to proceed?"
- Options:
  - `[A] Update story manifest version and implement with current rules (Recommended)`
  - `[B] Implement with old rules — I accept the risk of non-compliance`
  - `[C] Stop here — I want to review the manifest diff first`

If [A]: edit the story file's `Manifest Version:` field to the current manifest date before spawning the programmer. Then read the manifest carefully for new rules.
If [B]: read the manifest carefully for new rules anyway, and note the version mismatch in the Phase 6 summary under "Deviations".
If [C]: stop. Do not spawn any agent. Let the user review and re-run `/dev-story`.

### Dependency validation

After extracting the **Dependencies** list from the story file, validate each:

1. Glob `production/epics/**/*.md` to find each dependency story file.
2. Read its `Status:` field.
3. If any dependency has Status other than `Complete` or `Done`:
   - Use `AskUserQuestion`:
     - Prompt: "Story '[current story]' depends on '[dependency title]' which is currently [status], not Complete. How do you want to proceed?"
     - Options:
       - `[A] Proceed anyway — I accept the dependency risk`
       - `[B] Stop — I'll complete the dependency first`
       - `[C] The dependency is done but status wasn't updated — mark it Complete and continue`
   - If [B]: set story status to **BLOCKED** in session state and stop. Do not spawn any programmer agent.
   - If [C]: ask "May I update [dependency path] Status to Complete?" before continuing.
   - If [A]: note in Phase 6 summary under "Deviations": "Implemented with incomplete dependency: [dependency title] — [status]."

If a dependency file cannot be found: warn "Dependency story not found: [path]. Verify the path or create the story file."

---

### Engine reference
Read `.codex/docs/technical-preferences.md`:
- `Engine:` value — determines which programmer agents to use
- Naming conventions (class names, file names, signal/event names)
- Performance budgets (frame budget, memory ceiling)
- Forbidden patterns

### Production scene/UI/unit specifications

Read the production specification rules before routing or implementation:

- `docs/document-index.md`
- `production/content-creation-review-gate.md`
- `production/scene-specs/scene-coverage-registry.md`
- `production/scene-specs/scene-completeness-gate.md`
- `production/scene-specs/scene-vs-ui-evidence-boundary.md`
- `production/ui-specs/README.md`
- `production/unit-specs/README.md`

Then inspect the story text, acceptance criteria, implementation notes, and test
evidence for scene, UI, or unit work:

- If the story creates or introduces a new enterable scene, route destination,
  runtime UI surface, persistent HUD/panel/modal/overlay, reusable scene unit,
  authored `scene_unit.prototype.*`, NPC, obstacle, door, resource point, or
  world object with collision/occlusion/state, require a human suitability
  review record following `production/content-creation-review-gate.md`. The
  verdict must be `APPROVED` or `APPROVED_WITH_NOTES`, and any notes must already
  be reflected in the story or target spec. If the review is missing, `PENDING`,
  `REVISE`, or `REJECTED`, stop before coding and mark the story BLOCKED unless
  the story's only scope is drafting the review packet or a non-implementation
  spec draft. Codex review, generated rationale, or asking the user during the
  implementation turn does not satisfy this gate.
- If the story creates or introduces a new scene, UI surface, or reusable unit
  and the human suitability gate has passed, require the corresponding
  `godot-asset-skills` artifacts before production implementation:
  `.godot-ai/contracts/<asset-type>/<stable-id>.contract.md`,
  `.godot-ai/reviews/<asset-type>/<stable-id>.review.md`, and
  `.godot-ai/execution-plans/<asset-type>/<stable-id>.execution-plan.md`. The
  review must say `Can Execute: true`. If the contract, review, or execution plan
  is missing, or the review is blocked, stop before coding unless this story's
  only scope is to run `godot-asset-interview` / `godot-asset-review` and produce
  those artifacts.
- When implementing a new scene, UI surface, or reusable unit from approved
  `.godot-ai` artifacts, prefer `godot-asset-execute` and the Godot AI MCP
  installed under `addons/godot_ai` for Godot scene/resource/node work. Do not
  bypass the asset contract by hand-building scattered nodes into a legacy scene
  or large runtime script. After execution, require
  `.godot-ai/verification/<asset-type>/<stable-id>.verification.md` and cite it in
  the affected production spec or QA evidence.
- If the story creates, changes, or claims evidence/readiness for an enterable
  scene, world/playable surface, route destination, repair point, market area,
  transition, scene identity, spatial layout, or scene physics contract, read the
  linked `production/scene-specs/*.md` file before implementation. If the linked
  scene is missing, `tracked-gap`, or the story does not name a scene spec, stop
  before coding and mark the story BLOCKED unless the story's only scope is to
  draft or update that scene spec.
- If the story creates or changes a reusable physical unit, authored
  `scene_unit.prototype.*`, NPC, obstacle, door, prop with collision/occlusion,
  resource point, pushable/moving entity, or stateful world object, read the
  matching `production/unit-specs/fixed-scene-objects/*.md` or
  `production/unit-specs/dynamic-entities/*.md` file. If no concrete unit spec
  exists, stop before runtime implementation unless the story is explicitly a
  unit-spec authoring/migration story.
- If the story creates or changes a persistent HUD, anchored panel, modal,
  semi-modal overlay, full-screen UI surface, toast/hint, debug overlay, display
  priority, input/focus rule, or UI bound to a world anchor or system state, read
  the matching concrete file in `production/ui-specs/`. README/template references
  are not enough for implementation stories; stop before coding unless the story
  is explicitly drafting that UI spec.
- If the story creates or changes a scene, UI, or reusable unit, verify that the
  target object has an independent implementation, independent asset, or a named
  asset/data/runtime group that can be tracked as that object. Do not implement
  new content by scattering nodes into legacy Godot scenes or large runtime
  scripts; those files may only mount or reference approved independent objects.
- If implementation requires deleting non-compliant legacy Godot nodes, ask the
  user before deletion with the file path, node names, reason, and replacement
  path. If the user wants to keep a legacy concept, route it back through
  `production/content-creation-review-gate.md` as a new scene/UI/unit proposal.
- UI/HUD/buttons/labels/menus/modals/debug overlays cannot be used as scene
  identity, scene-unit, interaction-anchor, or #20 physics-contract proof. If an
  acceptance criterion tries to close scene/unit readiness with UI-only evidence,
  stop and surface the mismatch instead of implementing around it.

---

## Phase 3: Route to the Right Programmer

Based on the story's **Layer**, **Type**, and **system name**, determine which
specialist to spawn via Task.

**Config/Data stories — skip agent spawning entirely:**
If the story's Type is `Config/Data`, no programmer agent or engine specialist is needed. Jump directly to Phase 4 (Config/Data note). The implementation is a data file edit — no routing table evaluation, no engine specialist.

### Primary agent routing table

| Story context | Primary agent |
|---|---|
| Foundation layer — any type | `engine-programmer` |
| Any layer — Type: UI | `ui-programmer` |
| Any layer — Type: Visual/Feel | `gameplay-programmer` (implements) |
| Core or Feature — gameplay mechanics | `gameplay-programmer` |
| Core or Feature — AI behaviour, pathfinding | `ai-programmer` |
| Core or Feature — networking, replication | `network-programmer` |
| Config/Data — no code | No agent needed (see Phase 4 Config note) |

### Engine specialist — always spawn as secondary for code stories

Read the `Engine Specialists` section of `.codex/docs/technical-preferences.md`
to get the configured primary specialist. Spawn them alongside the primary agent
when the story involves engine-specific APIs, patterns, or the ADR has HIGH
engine risk.

| Engine | Specialist agents available |
|--------|----------------------------|
| Godot 4 | `godot-specialist`, `godot-gdscript-specialist`, `godot-shader-specialist` |
| Unity | `unity-specialist`, `unity-ui-specialist`, `unity-shader-specialist` |
| Unreal Engine | `unreal-specialist`, `ue-gas-specialist`, `ue-blueprint-specialist`, `ue-umg-specialist`, `ue-replication-specialist` |

**When engine risk is HIGH** (from the ADR or VERSION.md): always spawn the engine
specialist, even for non-engine-facing stories. High risk means the ADR records
assumptions about post-cutoff engine APIs that need expert verification.

---

## Phase 4: Implement

Spawn the chosen programmer agent(s) via Task with the full context package:

Provide the agent with:
1. The complete story file content
2. The current GDD requirement text (from TR registry)
3. The ADR Decision + Implementation Guidelines (verbatim — do not summarise)
4. The control manifest rules for this layer
5. The engine naming conventions and performance budgets
6. Any engine-specific notes from the ADR Engine Compatibility section
7. The test file path that must be created
8. Any loaded `production/scene-specs/`, `production/ui-specs/`, or
   `production/unit-specs/` documents that govern this story
9. Explicit instruction: **implement this story and write the test**

The agent should:
- Create or modify files in `src/` following the ADR guidelines
- Respect all Required and Forbidden patterns from the control manifest
- Respect the loaded scene/UI/unit specifications; do not substitute UI evidence
  for scene or scene-unit proof
- Preserve independent scene/UI/unit boundaries; large runtime files may compose
  approved objects, not become their hidden implementation
- Stay within the story's Out of Scope boundaries (do not touch unrelated files)
- Write clean, doc-commented public APIs

### Config/Data stories (no agent needed)

For Type: Config/Data stories, no programmer agent is required. The implementation
is editing a data file. Read the story's acceptance criteria and make the specified
changes to the data file directly. Note which values were changed and what they
changed from/to.

### Visual/Feel stories

Spawn `gameplay-programmer` to implement the code/animation calls. Note that
Visual/Feel acceptance criteria cannot be auto-verified — the "does it feel right?"
check happens in `/story-done` via manual confirmation.

---

## Phase 5: Write the Test

For **Logic** and **Integration** stories, the test must be written as part of
this implementation — not deferred to later.

Remind the programmer agent:

> "The test file for this story is required at: `[path from Test Evidence section]`.
> The story cannot be closed via `/story-done` without it. Write the test
> alongside the implementation, not after."

Test requirements (from coding-standards.md):
- File name: `[system]_[feature]_test.[ext]`
- Function names: `test_[scenario]_[expected_outcome]`
- Each acceptance criterion must have at least one test function covering it
- No random seeds, no time-dependent assertions, no external I/O
- Test the formula bounds from the GDD Formulas section

For **Visual/Feel** and **UI** stories: no automated test. Remind the agent to
note in the implementation summary what manual evidence will be needed:
"Evidence doc required at `production/qa/evidence/[slug]-evidence.md`."

For **Config/Data** stories: no test file. A smoke check will serve as evidence.

---

## Phase 6: Collect and Summarise

After the programmer agent(s) complete, collect:

- Files created or modified (with paths)
- Test file created (path and number of test functions written)
- Any deviations from the story's Out of Scope boundary (flag these)
- Any questions or blockers the agent surfaced
- Any engine-specific risks the specialist flagged

Present a concise implementation summary:

```
## Implementation Complete: [Story Title]

**Files changed**:
- `src/[path]` — created / modified ([brief description])
- `tests/[path]` — test file ([N] test functions)

**Acceptance criteria covered**:
- [x] [criterion] — implemented in [file:function]
- [x] [criterion] — covered by test [test_name]
- [ ] [criterion] — DEFERRED: requires playtest (Visual/Feel)

**Deviations from scope**: [None] or [list files touched outside story boundary]
**Engine risks flagged**: [None] or [specialist finding]
**Blockers**: [None] or [describe]

Ready for: `/code-review [file1] [file2]` then `/story-done [story-path]`
```

---

## Phase 7: Update Session State

Silently append to `production/session-state/active.md`:

```
## Session Extract — /dev-story [date]
- Story: [story-path] — [story title]
- Files changed: [comma-separated list]
- Test written: [path, or "None — Visual/Feel/Config story"]
- Blockers: [None, or description]
- Next: /code-review [files] then /story-done [story-path]
```

Create `active.md` if it does not exist. Confirm: "Session state updated."

---

## Error Recovery Protocol

If any spawned agent (via Task) returns BLOCKED, errors, or cannot complete:

1. **Surface immediately**: Report "[AgentName]: BLOCKED — [reason]" to the user before continuing to dependent phases
2. **Assess dependencies**: Check whether the blocked agent's output is required by subsequent phases. If yes, do not proceed past that dependency point without user input.
3. **Offer options** via AskUserQuestion with choices:
   - Skip this agent and note the gap in the final report
   - Retry with narrower scope
   - Stop here and resolve the blocker first
4. **Always produce a partial report** — output whatever was completed. Never discard work because one agent blocked.

Common blockers:
- Input file missing (story not found, GDD absent) → redirect to the skill that creates it
- ADR status is Proposed → do not implement; run `/architecture-decision` first
- Scope too large → split into two stories via `/create-stories`
- Conflicting instructions between ADR and story → surface the conflict, do not guess
- Manifest version mismatch → show diff to user, ask whether to proceed with old rules or update story first

## Collaborative Protocol

- **File writes are delegated** — all source code, test files, and evidence docs are written by sub-agents spawned via Task. Each sub-agent enforces the "May I write to [path]?" protocol individually. This orchestrator does not write files directly.
- **Load before implementing** — do not start coding until all context is loaded
  (story, TR-ID, ADR, manifest, engine prefs). Incomplete context produces code
  that drifts from design.
- **The ADR is the law** — implementation must follow the ADR's Implementation
  Guidelines. If the guidelines conflict with what seems "better," flag it in the
  summary rather than silently deviating.
- **Stay in scope** — the Out of Scope section is a contract. If implementing
  the story requires touching an out-of-scope file, stop and surface it:
  "Implementing [criterion] requires modifying [file], which is out of scope.
  Shall I proceed or create a separate story?"
- **Test is not optional for Logic/Integration** — do not mark implementation
  complete without the test file existing
- **Visual/Feel criteria are deferred, not skipped** — mark them as DEFERRED
  in the summary; they will be manually verified in `/story-done`
- **Ask before large structural decisions** — if the story requires an
  architectural pattern not covered by the ADR, surface it before implementing:
  "The ADR doesn't specify how to handle [case]. My plan is [X]. Proceed?"

---

## Recommended Next Steps

- Run `/code-review [file1] [file2]` to review the implementation before closing the story
- Run `/story-done [story-path]` to verify acceptance criteria and mark the story complete
- After all sprint stories are done: run `/team-qa sprint` for the full QA cycle before advancing the project stage
