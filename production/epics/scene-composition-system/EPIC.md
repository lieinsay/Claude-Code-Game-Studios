# Epic: Complete Scene Composition and Acceptance

> **Layer**: Polish Gate / Production Scene Design
> **GDD**: design/gdd/scene-composition-system.md
> **Architecture Module**: #19 Scene Composition Gate
> **Status**: In Progress
> **Stories**: 4 (001 Implemented, 002-004 Ready)

## Overview

Implement the production gate that decides whether an enterable scene is actually complete. The epic turns GDD #19 into reusable scene-spec templates, scene readiness checks, traceable asset/evidence requirements, and a dual-review workflow where automated evidence and Codex review are necessary but not sufficient without user readability review. Its central boundary is that UI text, buttons, HUD panels, and debug overlays may support a scene but cannot substitute for world/playable scene evidence.

## Governing ADRs

| ADR / Source | Decision Summary | Engine Risk |
| --- | --- | --- |
| ADR-0001: Autoload/Scene Boot Order | Scene transition cleanup, mounting, and lifecycle evidence must stay compatible with the project scene boot contract. | LOW |
| ADR-0012: UI Input Routing and Dual Focus | UI/HUD can assist scene readability but must not own world focus, scene units, or physical completion evidence. | HIGH |
| ADR-0016: Feedback/VFX/Audio Semantics | Scene completion evidence must include semantic feedback ownership without hardwiring final assets into gameplay logic. | LOW |
| ADR-0017: Onboarding First Loop | First-loop guidance can point at scene anchors but cannot replace readable spatial scene construction. | HIGH |
| ADR-0019: Desktop C# Platform Pivot | New runtime evidence and smoke checks target desktop Godot 4.6.2 .NET/C#. | HIGH |
| GDD #20 / Control Manifest | Every gameplay-relevant scene must pass the Scene Physics Contract gate before scene completion can pass. | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
| --- | --- | --- |
| TR-scene-composition-001 | Every enterable scene must provide a scene specification covering purpose, entry/exit, spatial structure, interaction anchors, state variants, assets, audio/VFX, technical contract, and QA evidence before implementation readiness | GDD #19 gate + ADR-0001 / ADR-0019 |
| TR-scene-composition-002 | Scene completion requires scene physics readiness, behavior readiness, state variant readiness, visual/audio readiness, technical contract readiness, automated evidence, Codex review, and user readability review | GDD #19 gate + GDD #20 + ADR-0012 / ADR-0016 / ADR-0017 |
| TR-scene-composition-003 | UI, HUD, buttons, menus, labels, and debug overlays cannot count as physical scene units or substitute for world/playable scene evidence | GDD #19 gate + ADR-0012 |

## Epic Scope

- Create the reusable scene specification structure for current and future enterable scenes.
- Define how scene specs link to GDD #20 Scene Physics Contracts.
- Add traceability from scene specs to runtime nodes, smoke evidence, screenshots, assets, and human QA checklists.
- Refresh current Polish scene acceptance around Hub exterior, ship interior, Exploration, repair, and settlement entry points.
- Preserve the distinction between UI assistance and world/playable scene proof.

## Out of Scope

- Owning the gameplay consequences of search, repair, market, route, resource, or partner systems.
- Final art production, final audio production, or asset creation.
- Replacing the UI/HUD, movement, feedback, onboarding, or persistence systems.
- Implementing full physical simulation; that belongs to #20 and scene-specific stories.

## Stories

| # | Story | Type | Status | ADR |
| --- | --- | --- | --- | --- |
| 001 | [Scene Specification Template and Coverage Registry](story-001-scene-spec-template-coverage-registry.md) | Config/Data | Implemented | ADR-0001 / ADR-0019 |
| 002 | [Scene Completeness Gate and Evidence Contract](story-002-scene-completeness-gate-evidence.md) | Integration | Ready | ADR-0001 / ADR-0019 |
| 003 | [Scene Versus UI Evidence Boundary](story-003-scene-vs-ui-evidence-boundary.md) | Integration | Ready | ADR-0012 |
| 004 | [User Readability Review and Release Gate Handoff](story-004-user-readability-release-gate.md) | Visual/Feel | Ready | ADR-0016 / ADR-0017 |

## Definition of Done

This epic is complete when:

- All stories are implemented, reviewed, and closed via `/story-done`.
- Every current enterable scene has a linked scene spec or an explicit tracked exemption.
- Every current scene spec either passes GDD #20's `physics_contract_complete` gate or explains why no gameplay-relevant physical units exist.
- Scene readiness cannot pass on UI/HUD text, buttons, labels, or debug overlays alone.
- Automated smoke evidence covers runtime mounting, scene identity, contract fields, and no stale state after transitions.
- Human QA checklists record user readability verdicts before release-readiness claims.
- `production/qa/evidence/` contains current scene evidence with build/test commands and remaining risk.

## Next Step

Run `/story-readiness production/epics/scene-composition-system/story-002-scene-completeness-gate-evidence.md`, then `/dev-story` for the same file.
