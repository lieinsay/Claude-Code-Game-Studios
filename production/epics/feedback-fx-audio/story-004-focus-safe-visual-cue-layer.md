# Story 004: Focus-Safe Visual Cue Layer

> **Epic**: Feedback, VFX, and Audio Semantics
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M / 4-6 hours
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/feedback-fx-audio.md`
**Requirement**: `TR-feedback-002`

**ADR Governing Implementation**: ADR-0016 with ADR-0012 focus/input routing
**ADR Decision Summary**: Feedback overlays are visual-only unless a future settings panel explicitly opens through UIManager. Default overlay behavior is focus disabled and `mouse_filter = Ignore`; Chart and Exploration HUD focus isolation must remain intact.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: UI overlays must respect Godot 4.6 dual-focus and 4.5 recursive Control disable / mouse-filter behavior.

**Control Manifest Rules (Presentation layer)**:
- Required: UIManager owns focus management and 4-layer input routing.
- Forbidden: Feedback overlay input capture, independent panel input handling, or underlying Hub control focus while Chart/Exploration HUD is active.
- Guardrail: Visual cues are brief and non-modal; they must not obscure active controls or labels.

---

## Acceptance Criteria

*From GDD `design/gdd/feedback-fx-audio.md`, scoped to this story:*

- [x] GIVEN Chart is active, WHEN feedback overlays appear, THEN underlying Hub controls do not regain focus and mouse input remains routed to Chart.
- [x] GIVEN Exploration HUD is active, WHEN resource, threat, or hull feedback events are consumed, THEN visible labels remain readable and no control is obscured.
- [x] GIVEN a route is selected, WHEN route selection feedback appears, THEN the route receives visible selection confirmation without covering route identity or risk readability.
- [x] GIVEN departure is confirmed, WHEN a major or critical visual cue appears, THEN it confirms the irreversible transition without delaying or blocking the transition.
- [x] GIVEN a focused modal or chart is open, WHEN feedback overlay Controls are present, THEN they use focus-disabled / mouse-ignore behavior by default.
- [x] GIVEN a scene transition happens during an active cue, WHEN non-critical cues are still active, THEN they stop or fade without holding references to freed nodes.

---

## Implementation Notes

Derived from ADR-0016:

- Feedback visual output should be a UIManager-owned layer or sink; #17 requests visuals but does not own input.
- Default overlay Control settings: no focus mode and `mouse_filter = Ignore`.
- Visual cues must be brief and non-modal. They may pulse/highlight/status-label active surfaces but must not open a modal unless a future settings story explicitly owns that panel.
- Route, Exploration HUD, Repair, Market/Inventory, Session, and Global Warning cue families must preserve the readability of their primary UI text.
- Scene cleanup must follow ADR-0001/ADR-0012 principles: stop tweens, release references, and avoid freed node access.
- Performance: visual cues must stay lightweight and non-modal, with no per-frame work when no cues are active.

---

## Out of Scope

- Story 003 owns missing asset and caption fallback behavior.
- Final authored VFX assets are out of scope.
- A #17 settings screen is out of scope.
- #18 onboarding highlight behavior is a separate system and must not be implemented here.

---

## QA Test Cases

- **AC-1**: Chart overlay does not steal focus
  - Given: Chart is active with route selection focus
  - When: #17 requests a route feedback overlay
  - Then: Chart focus and mouse routing remain active and Hub controls cannot be keyboard-selected
  - Edge cases: overlay appears while route side panel is expanded; Esc closes Chart normally

- **AC-2**: Exploration HUD remains readable
  - Given: Exploration HUD shows resource pressure, threat, and hull labels
  - When: feedback overlays for those events are requested
  - Then: labels remain visible/readable and controls are not covered
  - Edge cases: long status text; small viewport; multiple cue channels active

- **AC-3**: Route selection visual preserves route identity
  - Given: a selected route has name and risk text visible
  - When: a route pulse/highlight is requested
  - Then: the highlight confirms selection without hiding the route name or risk text
  - Edge cases: missing visual asset falls back to Story 003 text path

- **AC-4**: Scene transition releases cues
  - Given: a non-critical cue is active
  - When: the scene transitions or load completes
  - Then: the cue stops or fades and no freed node reference is retained
  - Edge cases: load while caption is active; Chart departure transition

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- `tests/integration/feedback-fx-audio/FocusSafeVisualCueTest.csproj` OR `production/qa/evidence/feedback-fx-audio-focus-visual-evidence.md`
- Existing UI/HUD focus regression must still pass: `tests/integration/ui-hud-interface/EdgeCasesDesktopA11yTest.csproj`

**Status**: [x] Created and passing — `dotnet run --project tests/integration/feedback-fx-audio/FocusSafeVisualCueTest.csproj -p:UseSharedCompilation=false` passed 9/9 on 2026-05-16; `dotnet run --project tests/integration/ui-hud-interface/EdgeCasesDesktopA11yTest.csproj -p:UseSharedCompilation=false` passed 25/25.

---

## Dependencies

- Depends on: Story 001, Story 002, Story 003
- Unlocks: Story 005

## Completion Notes

**Completed**: 2026-05-16
**Criteria**: 6/6 passing. The focused runner contains 9 checks: 6 acceptance criteria plus Hub departure, critical transition release, and QA snapshot reset regressions.
**Deviations**: None. The implementation remains headless C# presentation routing; final authored VFX/audio assets and rendered visual fidelity remain out of scope for this story.
**Test Evidence**: UI evidence at `tests/integration/feedback-fx-audio/FocusSafeVisualCueTest.csproj`; `dotnet run --project tests/integration/feedback-fx-audio/FocusSafeVisualCueTest.csproj -p:UseSharedCompilation=false` PASS 9/9. Existing UI/HUD focus regression `tests/integration/ui-hud-interface/EdgeCasesDesktopA11yTest.csproj` PASS 25/25. Story 001, Story 002, and Story 003 feedback regression runners also PASS, and `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 0 warnings, 0 errors.
**Code Review**: Complete — `$code-review` approved the Story 004 changes after Hub departure payload, critical cue release, and overlay reset fixes.
