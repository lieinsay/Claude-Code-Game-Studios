# Scope Brief: #18 Onboarding and First Loop

**Date:** 2026-05-15
**Sprint:** Sprint 001 Polish Stabilization
**Status:** Scope defined; formal GDD approved, ADR-0017 accepted
**Related system:** #18 Onboarding / First Loop
**Primary sources:** `design/gdd/systems-index.md`, `design/gdd/ui-hud-chart-interface.md`, `design/ux/hub.md`, `design/ux/chart.md`, `design/ux/exploration.md`, `design/ux/interaction-patterns.md`

## Purpose

Define the narrow Polish entry scope for #18 before any full onboarding implementation begins. The goal is first-loop discoverability without replacing player exploration or adding a heavy tutorial layer.

## Input Gap Resolution

`production/epics/index.md` references `onboarding-first-loop.md`; `design/gdd/onboarding-first-loop.md` now exists, promotes this brief into a formal system GDD, and is design-reviewed. ADR-0017 is accepted and should govern the #18 implementation story split.

## First-Loop Goals

The first loop should help a player discover and complete:

1. Reach or identify the Hub / HUD entry.
2. Open Chart by visible entry or `M`.
3. Select a route.
4. Confirm departure into Exploration HUD.
5. Advance the Exploration HUD pressure loop.
6. Save and Load at least once, or understand where those entries are.
7. Return to Hub.
8. Notice that Hub cargo, storage, hull, and route summaries changed.

## In Scope For First Polish Implementation

1. Consume #16 panel open/close and focus state events.
2. Use existing highlight metadata such as `highlightable` and `highlight_priority`.
3. Provide low-intrusion guidance, not blocking tutorial popups:
   - subtle highlight on first relevant Hub entry
   - one-line contextual hint near the active control
   - optional checklist text for first-loop progress
4. Support both keyboard and mouse paths.
5. Avoid stealing focus from Chart, Exploration HUD, Save/Load, or modal panels.
6. Persist enough onboarding state to avoid repeating completed hints every time.

## Out Of Scope

- Full tutorial campaign.
- Voiceover, animated companion teaching, or bespoke onboarding cutscenes.
- Teaching every downstream economy, combat, repair, settlement, or partner system.
- Blocking the player until a specific tutorial action is completed.
- Replacing #16 focus ownership or input routing.

## Acceptance Criteria

- A new player can find the Chart/HUD entry without developer guidance.
- Chart focus isolation remains intact while onboarding hints are visible.
- The player can discover route selection and departure using keyboard only.
- Exploration HUD pressure controls are highlighted or hinted without obscuring feedback labels.
- Save/Load entries remain visible and understandable.
- Return-to-Hub summary change is called out after the first completed loop.
- Hints do not repeat after completion unless onboarding state is reset.
- Hints have text labels and do not rely on color alone.

## Required QA Evidence

- One manual new-player walkthrough that records time to find Chart/HUD, route selection, Save/Load, and return-Hub summary.
- Keyboard-only walkthrough for the same first loop.
- Focus regression check while Chart and Exploration HUD are open.
- Accessibility check for text clarity and non-color-only guidance.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Onboarding becomes too intrusive and breaks the "Hub as home" fantasy | High | Use subtle contextual hints and avoid modal tutorial locks. |
| Highlight overlays steal focus or mouse input | High | Treat onboarding as visual-only unless explicitly interacting with its own settings. |
| No #18 implementation story split yet | Medium | Create #18 implementation stories from the approved GDD and ADR-0017. |
| First-loop guidance hides actual UI problems | Medium | If a control is not discoverable without hints, fix the base UI first. |

## Done For Sprint 001

This scope brief is sufficient to close P1-004. It does not mark #18 implemented.
