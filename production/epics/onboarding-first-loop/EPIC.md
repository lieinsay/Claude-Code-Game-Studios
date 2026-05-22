# Epic #18: Onboarding and First Loop

> **Status**: Complete
> **Layer**: Presentation
> **GDD**: `design/gdd/onboarding-first-loop.md`
> **Architecture Module**: Presentation service -- `OnboardingManager`
> **Engine**: Godot 4.6.2 .NET + C#
> **Created**: 2026-05-22
> **Stories**: 5 (001-005)

## Overview

System #18 adds low-intrusion first-loop guidance for the current playable route:
Hub -> Chart -> route selection -> Exploration -> Save/Load awareness -> return
Hub -> summary-change awareness.

The implementation must clarify the existing UI and spatial route without taking
ownership of route, cargo, hull, repair, market, save, or focus state. The
system consumes #16 UI events and current playable-loop domain facts, tracks
onboarding step completion, emits hint/highlight requests, and persists completed
steps so hints do not repeat after save/load.

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|------------------|-------------|
| ADR-0017: Onboarding and First Loop Guidance | Implement #18 as a C# `OnboardingManager` service that consumes UI/domain/session events, owns first-loop guidance state, emits hint/highlight requests, and exports `progress.onboarding`; UIManager remains renderer and focus owner. | HIGH |
| ADR-0012: UI Input Routing and Dual Focus | UIManager owns screen state, modal/input routing, focus isolation, highlight metadata, and safe overlay placement. | LOW |
| ADR-0003 / ADR-0019 | Persistence validates snapshot packages; active implementation targets desktop Godot .NET/C#. | MEDIUM |

## TR Coverage

| TR ID | Requirement | Story Coverage |
|-------|-------------|----------------|
| TR-onboarding-001 | First-loop guidance: Hub -> Chart -> Explore -> Return -> Repair | Stories 001-005 |

## Stories

| # | Story | Type | TRs | ADR | Status |
|---|-------|------|-----|-----|--------|
| 001 | [First-Loop Step State and Hint Scoring](story-001-first-loop-step-state-and-hint-scoring.md) | Logic | TR-onboarding-001 | ADR-0017 | Complete |
| 002 | [UI and Domain Event Integration](story-002-ui-and-domain-event-integration.md) | Integration | TR-onboarding-001 | ADR-0017, ADR-0012 | Complete |
| 003 | [Onboarding Persistence Snapshot](story-003-onboarding-persistence-snapshot.md) | Integration | TR-onboarding-001 | ADR-0017, ADR-0003 | Complete |
| 004 | [Focus-Safe Hint Rendering and Accessibility](story-004-focus-safe-hint-rendering-and-accessibility.md) | UI | TR-onboarding-001 | ADR-0017, ADR-0012 | Complete |
| 005 | [First-Loop Smoke Regression and QA Evidence](story-005-first-loop-smoke-regression-and-qa-evidence.md) | Integration | TR-onboarding-001 | ADR-0017, ADR-0019 | Complete |

**Summary**: 1 Logic + 3 Integration + 1 UI stories

## Definition of Done

This epic is complete when:

- All 5 stories are implemented, reviewed, and closed via `/story-done`.
- `OnboardingManager` tracks the eight GDD first-loop steps without mutating
  domain or UI focus state.
- #16 UI/HUD, Chart, Exploration, Save/Load, and return-Hub summary events can
  complete onboarding steps.
- `progress.onboarding` persists completed/suppressed steps and restores them
  without replaying completed hints.
- Hint/highlight rendering is non-modal, focus-safe, text-labeled, and not
  color-only.
- Keyboard-only and mouse-only first-loop walkthroughs pass.
- Existing UI/HUD, save/load, focus, accessibility, and Sprint 003 playable
  smoke evidence do not regress.

## Out of Scope

- Full tutorial campaign.
- Blocking tutorial modals.
- Voiceover, companion teaching, or bespoke cutscenes.
- Teaching every downstream economy, combat, repair, settlement, or partner
  system.
- Replacing UIManager focus ownership or input routing.
- Final art/audio production for onboarding hints.

## Next Step

Epic #18 implementation stories are complete. Next, continue normal Polish backlog work: runtime hardening for Navigation/Exploration beyond the playable fixture, windowed visual capture if required, and final art/audio treatment for onboarding hints.
