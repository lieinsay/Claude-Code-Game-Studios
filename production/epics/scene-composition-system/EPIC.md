# Epic: Complete Scene Composition and Acceptance

> **Layer**: Polish Gate / Production Scene Design
> **GDD**: design/gdd/scene-composition-system.md
> **Architecture Module**: #19 Scene Composition Gate
> **Status**: Complete
> **Stories**: 4 (001-004 Complete)

## Overview

Implement the production gate that decides whether an enterable scene is actually complete. The epic turns GDD #19 into reusable scene-spec templates, scene readiness checks, traceable asset/evidence requirements, and a Codex consistency review workflow. Its central boundary is that UI text, buttons, HUD panels, and debug overlays may support a scene but cannot substitute for world/playable scene evidence. User feedback after a real implementation remains always modifiable through `directed-content-modification`; it is not a second release verdict.

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
| TR-scene-composition-002 | Scene completion requires creation suitability approval where applicable, scene physics readiness, independent implementation / asset boundary, behavior readiness, state variant readiness, visual/audio readiness, technical contract readiness, automated evidence, Codex review, and implementation-feedback routing | GDD #19 gate + GDD #20 + ADR-0012 / ADR-0016 / ADR-0017 |
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
| 001 | [Scene Specification Template and Coverage Registry](story-001-scene-spec-template-coverage-registry.md) | Config/Data | Complete | ADR-0001 / ADR-0019 |
| 002 | [Scene Completeness Gate and Evidence Contract](story-002-scene-completeness-gate-evidence.md) | Integration | Complete | ADR-0001 / ADR-0019 |
| 003 | [Scene Versus UI Evidence Boundary](story-003-scene-vs-ui-evidence-boundary.md) | Integration | Complete | ADR-0012 |
| 004 | [Post-Implementation Feedback Routing and Release Gate Handoff](story-004-user-readability-release-gate.md) | Visual/Feel | Complete | ADR-0016 / ADR-0017 |

## Definition of Done

This epic is complete when:

- All stories are implemented, reviewed, and closed via `/story-done`.
- Every current enterable scene has a linked scene spec or an explicit tracked exemption.
- Every current scene spec either passes GDD #20's `physics_contract_complete` gate or explains why no gameplay-relevant physical units exist.
- Scene readiness cannot pass on UI/HUD text, buttons, labels, or debug overlays alone.
- Automated smoke evidence covers runtime mounting, scene identity, contract fields, and no stale state after transitions.
- Implemented scenes provide release evidence, Codex consistency review, and a `directed-content-modification` route for post-implementation user feedback.
- `production/qa/evidence/` contains current scene evidence with build/test commands and remaining risk.

## Next Step

All Story 001-004 closure slices are complete. Next continue implementation/evidence closure for approved independent scene specs; release handoff remains `BLOCKED_FOR_RELEASE` until implementation evidence, #20 contract gaps, screenshots, P0 asset gaps, and tracked waivers are resolved.
