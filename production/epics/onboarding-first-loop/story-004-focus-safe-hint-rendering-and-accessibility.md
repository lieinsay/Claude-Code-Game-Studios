# Story 004: Focus-Safe Hint Rendering and Accessibility

> **Epic**: Onboarding and First Loop
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M / 6-8 hours
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/onboarding-first-loop.md`
**Requirement**: `TR-onboarding-001`

**ADR Governing Implementation**: ADR-0017: Onboarding and First Loop Guidance; ADR-0012: UI Input Routing and Dual Focus
**ADR Decision Summary**: UIManager renders #18 hint/highlight requests while retaining focus, modal, and input ownership. Hints are non-modal, text-labeled, and must not capture mouse or keyboard focus.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Godot 4.6 dual-focus and recursive Control behavior are the main risk. Rendered hint Controls must default to focus disabled and mouse-filter ignore unless a future settings panel is explicitly interactive.

**Control Manifest Rules (Presentation layer)**:
- Required: UIManager owns screen state, modal stack, input routing, focus management, and safe overlay placement.
- Forbidden: onboarding hints must not steal focus, consume gameplay input, or bypass UIManager focus isolation.
- Guardrail: one visible hint by default; no per-frame layout scanning beyond existing UI update points.

---

## Acceptance Criteria

*From GDD `design/gdd/onboarding-first-loop.md`, scoped to this story:*

- [ ] GIVEN an onboarding hint is visible, WHEN keyboard and mouse input continue, THEN the hint does not steal focus or mouse input from the active surface.
- [ ] GIVEN Chart is active, WHEN Hub anchors still exist underneath, THEN onboarding ignores Hub anchors until Chart closes.
- [ ] GIVEN Exploration HUD pressure feedback is visible, WHEN a pressure-loop hint appears, THEN it does not cover resource, threat, hull, or status feedback labels.
- [ ] GIVEN hints or highlights appear, WHEN accessibility is checked, THEN every hint has text and does not rely on color alone.
- [ ] GIVEN a keyboard-only or mouse-only player follows the first loop, WHEN hints are visible, THEN both paths remain valid.
- [ ] GIVEN a hint anchor is missing or unsafe, WHEN rendering is requested, THEN UIManager shows a safe text-only hint or skips the hint without crashing.

---

## Implementation Notes

Derived from ADR-0017 and ADR-0012:

- Add UIManager rendering hooks for `OnboardingHintRequest` or equivalent request values.
- Render hints/highlights as non-modal overlays with focus disabled and mouse filter ignore.
- Place hints near active surfaces without covering critical labels; shorten, reposition, or skip unsafe hints.
- Use text labels for every hint; color may reinforce but never carry the only meaning.
- Hide Hub hints immediately when Chart or Exploration surface is active.
- Keep one visible hint by default unless later tuning explicitly allows two.
- Expose QA snapshots for focus owner, mouse filter mode, visible hint IDs, anchor IDs, and occlusion/readability decisions.

---

## Out of Scope

- Story 001 owns hint scoring.
- Story 002 owns event integration.
- Story 003 owns persistence.
- Story 005 owns full smoke/manual QA evidence.
- Final authored art/audio for hint presentation.

---

## QA Test Cases

- **Manual check AC-1**: Hints do not steal focus
  - Setup: Reach Hub, open Chart, and show an onboarding hint.
  - Verify: keyboard focus remains on the active surface; mouse hover/click behavior for active controls is unchanged.
  - Pass condition: hints are visible but do not activate or block unrelated controls.

- **Manual check AC-2**: Chart suppresses Hub anchors
  - Setup: Show a Hub hint, then open Chart.
  - Verify: Hub hint hides and no underlying Hub anchor appears active.
  - Pass condition: Chart focus isolation remains intact.

- **Manual check AC-3**: Exploration hints avoid feedback labels
  - Setup: Depart to Exploration, trigger pressure feedback, then show pressure-loop hint.
  - Verify: resource, threat, hull, and status labels remain readable.
  - Pass condition: no hint overlaps critical feedback text.

- **Manual check AC-4**: Text and non-color-only meaning
  - Setup: Inspect each first-loop hint/highlight.
  - Verify: each hint has readable text and shape/position cues; color is never sole meaning.
  - Pass condition: Basic accessibility requirements are met.

- **Manual check AC-5**: Keyboard-only and mouse-only paths remain valid
  - Setup: Complete first loop once using keyboard only and once using mouse path where available.
  - Verify: hints do not force the other input method.
  - Pass condition: both routes complete.

- **Manual check AC-6**: Missing anchor fallback
  - Setup: Simulate or remove a highlight anchor in test scene.
  - Verify: UIManager shows safe text-only hint or skips safely.
  - Pass condition: no crash, no focus capture, diagnostic records fallback.

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- `production/qa/evidence/onboarding-focus-safe-hints-evidence.md` plus focused UI/hint regression checks

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001, Story 002
- Unlocks: Story 005

