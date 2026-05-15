# Scope Brief: #17 Feedback, VFX, and Audio Semantics

**Date:** 2026-05-15
**Sprint:** Sprint 001 Polish Stabilization
**Status:** Scope defined; formal GDD approved, ADR-0016 accepted
**Related system:** #17 Feedback / VFX / Audio Semantics
**Primary sources:** `design/gdd/systems-index.md`, `design/gdd/ui-hud-chart-interface.md`, `design/accessibility-requirements.md`, `design/ux/interaction-patterns.md`

## Purpose

Define the narrow Polish entry scope for #17 before any full VFX/audio implementation begins. This brief does not authorize a complete feedback system build; it defines the first implementation boundary and the evidence needed to start safely.

## Input Gap Resolution

`production/epics/index.md` references `feedback-fx-audio.md`; `design/gdd/feedback-fx-audio.md` now exists, promotes this brief into a formal system GDD, and is design-reviewed. ADR-0016 is accepted and now governs the #17 implementation story split in `production/epics/feedback-fx-audio/`.

## In Scope For First Polish Implementation

1. Consume UI semantic events emitted by #16, especially:
   - `ui_panel_opened`
   - `ui_panel_closed`
   - `ui_route_selected`
   - `ui_departure_confirmed`
   - `ui_threat_response_chosen`
   - `ui_repair_submitted`
   - `ui_purchase_confirmed`
   - `ui_item_transferred`
2. Provide a small feedback router contract that maps semantic events to:
   - visual pulse or highlight request
   - optional audio cue request
   - subtitle/text fallback request for meaningful audio
3. Preserve the current MVP runtime bridge feedback channels:
   - route selected / departure confirmed
   - Exploration HUD resource pressure change
   - Exploration HUD threat feedback change
   - Exploration HUD hull feedback change
   - return-to-Hub summary update
4. Implement or specify player-facing text fallback for meaningful audio events, consistent with Basic accessibility requirements.
5. Add QA evidence for event routing and no-crash behavior when feedback assets are missing.

## Out Of Scope

- Full authored VFX asset production.
- Final audio mix, music, or adaptive soundscape.
- Complete subtitle settings UI.
- Full combat, repair, settlement, and partner feedback coverage beyond first-priority semantic events.
- Replacing #16 UI/HUD state ownership.

## Acceptance Criteria

- A feedback contract exists and is documented before implementation starts.
- Missing visual/audio assets degrade to text or no-op without crashes.
- UI/HUD semantic event names remain typed and stable.
- Save/Load, route departure, Exploration HUD pressure changes, and return-to-Hub summary do not depend on #17 to remain understandable.
- Any meaningful audio cue has an equivalent visible text or subtitle path.
- QA can verify event routing with automated or manual evidence.

## Required QA Evidence

- Automated event routing test or equivalent integration check for core #16 semantic events.
- Manual smoke check that missing audio/VFX assets do not block the UI loop.
- Accessibility check that meaningful audio has visible text fallback.
- Regression check that Chart focus isolation and Exploration HUD controls still pass after feedback hooks are connected.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| #17 expands into full asset production during stabilization | High | Keep first task to contract + minimal router; move asset work to later Polish stories. |
| Feedback hooks create event-order coupling | Medium | Consume events after state mutation only; do not write back into domain state. |
| Audio-only feedback violates Basic accessibility | Medium | Require subtitle/text fallback for meaningful audio. |
| #17 implementation story split created | Low | Work through `production/epics/feedback-fx-audio/story-001-*` through `story-005-*` in order; do not expand into full asset production. |

## Done For Sprint 001

This scope brief is sufficient to close P1-003. It does not mark #17 implemented.
