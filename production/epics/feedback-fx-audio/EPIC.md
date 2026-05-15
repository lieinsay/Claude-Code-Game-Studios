# Epic #17: Feedback, VFX, and Audio Semantics

> **Status**: Ready
> **Layer**: Presentation
> **GDD**: `design/gdd/feedback-fx-audio.md`
> **Architecture Module**: Presentation service — `FeedbackManager`
> **Engine**: Godot 4.6.2 .NET + C#
> **Created**: 2026-05-15
> **Stories**: 5 (001-005)

## Overview

System #17 turns stable gameplay and UI semantic events into readable feedback:
visual cue requests, optional audio cue requests, visible status text, captions,
and QA diagnostics. It is presentation-only. It consumes facts after their owning
systems mutate state and must never write route, inventory, hull, repair, market,
save, onboarding, or UI focus state.

The first Polish slice prioritizes a minimal semantic router, #16/#2/#3 event
wiring, missing-asset and muted-audio fallbacks, focus-safe overlays, and smoke
regression evidence. Full authored VFX, final audio mix, music, adaptive
soundscape, and settings UI remain out of this epic.

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|------------------|-------------|
| ADR-0016: Feedback, VFX, and Audio Semantics | `FeedbackManager` consumes approved semantic events, normalizes them into `FeedbackRequest`, applies priority/coalescing, routes status/caption/audio/visual outputs, and exposes diagnostics without mutating domain state. | HIGH |
| ADR-0012: UI Input Routing and Dual Focus | UIManager owns layout, screen state, modal/input routing, focus isolation, and semantic UI events consumed by #17. | LOW |
| ADR-0002 / ADR-0003 / ADR-0019 | Typed signal/event protocol, save/load completion boundaries, and desktop C# implementation target. | MEDIUM |

## TR Coverage

| TR ID | Requirement | Story Coverage |
|-------|-------------|----------------|
| TR-feedback-001 | Semantic feedback events: route_selected, repair_completed, threat_triggered, etc. | Stories 001, 002, 003, 005 |
| TR-feedback-002 | Minimum visible-repair feedback owned by #13; home-safety feedback by #7; clarity by #16 | Stories 003, 004, 005 |

## Stories

| # | Story | Type | TRs | ADR |
|---|-------|------|-----|-----|
| 001 | [Feedback Request Router Core](story-001-feedback-request-router-core.md) | Logic | TR-feedback-001 | ADR-0016 |
| 002 | [UI and Session Semantic Event Wiring](story-002-ui-session-semantic-event-wiring.md) | Integration | TR-feedback-001 | ADR-0016, ADR-0012 |
| 003 | [Accessible Fallbacks, Subtitles and Missing Assets](story-003-accessible-fallbacks-subtitles-missing-assets.md) | Integration | TR-feedback-001, TR-feedback-002 | ADR-0016 |
| 004 | [Focus-Safe Visual Cue Layer](story-004-focus-safe-visual-cue-layer.md) | UI | TR-feedback-002 | ADR-0016, ADR-0012 |
| 005 | [Smoke Regression, Diagnostics and Performance](story-005-smoke-regression-diagnostics-performance.md) | Integration | TR-feedback-001, TR-feedback-002 | ADR-0016, ADR-0019 |

**Summary**: 1 Logic + 3 Integration + 1 UI stories

## Definition of Done

This epic is complete when:

- All 5 stories are implemented, reviewed, and closed via `/story-done`.
- `FeedbackManager` exposes the ADR-0016 request contract, priority/coalescing behavior, and diagnostics.
- #16 UI semantic events and #2/#3 save/load completion events route into #17 without changing ownership.
- Missing VFX/audio assets and muted audio do not crash and do not remove meaningful visible feedback.
- `caption_text` requests produce `subtitle_requested` or an equivalent subtitle-layer request.
- Chart and Exploration HUD focus isolation regressions continue to pass with feedback overlays enabled.
- Hub -> Chart -> Exploration -> Save/Load -> Hub smoke checks pass with #17 hooks connected.
- Numeric smoke performance remains within current frame, memory, draw-call, and save/load timing budgets.

## Out of Scope

- Final authored VFX asset production.
- Final audio mix, music, or adaptive soundscape.
- Subtitle/settings UI beyond the renderable caption/status fallback path.
- Replacing UIManager layout, modal, screen-state, or focus ownership.
- Full #18 onboarding guidance.

## Next Step

Run `/story-readiness production/epics/feedback-fx-audio/story-001-feedback-request-router-core.md`, then `/dev-story` for Story 001.
