# Story 002: UI and Session Semantic Event Wiring

> **Epic**: Feedback, VFX, and Audio Semantics
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/feedback-fx-audio.md`
**Requirement**: `TR-feedback-001`

**ADR Governing Implementation**: ADR-0016 with ADR-0012 and ADR-0003 boundaries
**ADR Decision Summary**: #17 consumes approved #16 UI semantic events and #2/#3 save/load completion events. UIManager keeps screen/focus ownership; Persistence/SessionShell remain the source for save/load completion.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Event wiring must preserve ADR-0002 typed event boundaries and must not add browser lifecycle dependencies.

**Control Manifest Rules (Presentation layer)**:
- Required: Cross-system communication uses typed C# events/signals; UIManager owns semantic UI events.
- Forbidden: Do not treat save/load completion as a #16 UI event table entry; do not connect/disconnect dynamically in per-frame code.
- Guardrail: Signal emit cost remains small; event fan-out must not exceed ADR-0002 cascade expectations.

---

## Acceptance Criteria

*From GDD `design/gdd/feedback-fx-audio.md`, scoped to this story:*

- [ ] GIVEN #16 emits `ui_panel_opened` or `ui_panel_closed`, WHEN #17 consumes it, THEN a minor context-change feedback request is created without changing focus state.
- [ ] GIVEN a route is selected, WHEN `ui_route_selected` is consumed, THEN the route receives a visible selection confirmation request and any optional audio has a text-equivalent path.
- [ ] GIVEN departure is confirmed, WHEN `ui_departure_confirmed` is consumed, THEN a major or critical cue confirms the irreversible transition without delaying the transition.
- [ ] GIVEN #16 emits `ui_threat_response_chosen`, `ui_repair_submitted`, `ui_purchase_confirmed`, or `ui_item_transferred`, WHEN #17 consumes them, THEN each maps to the intended cue family and priority band.
- [ ] GIVEN save or load completes, WHEN the session event is consumed, THEN player-facing text still confirms completion and the source is #2/#3 session or persistence state rather than the #16 UI event table.
- [ ] GIVEN the current MVP runtime bridge already exposes route selection, departure, Exploration HUD pressure, threat, hull, and return-Hub summary feedback, WHEN #17 hooks are connected, THEN those channels remain understandable without becoming dependent on final VFX/audio assets.

---

## Implementation Notes

Derived from ADR-0016:

- First-pass #16 sources are `ui_panel_opened`, `ui_panel_closed`, `ui_route_selected`, `ui_departure_confirmed`, `ui_threat_response_chosen`, `ui_repair_submitted`, `ui_purchase_confirmed`, and `ui_item_transferred`.
- Save/load completion comes from #2/#3 session or persistence events such as `SaveCompleted` / `LoadCompleted` or the final typed equivalent, then maps to `ui_save_completed` / `ui_load_completed` as #17 semantic event IDs.
- `ui_departure_confirmed` should be major or critical because it confirms an irreversible transition, but #17 must not block or delay the transition.
- Route selection feedback belongs to the Chart cue family; repair to Repair; threat/hull/resource pressure to Exploration HUD; purchase/item transfer to Market/Inventory; save/load to Session.
- Event handlers should read facts after the owning system has completed mutation and should write only to #17 diagnostics/output sinks.

---

## Out of Scope

- Story 001 owns router scoring/coalescing internals.
- Story 003 owns missing asset, muted audio, and subtitle fallback behavior.
- Story 004 owns visual overlay rendering/focus behavior.
- Story 005 owns full smoke/performance regression evidence.

---

## QA Test Cases

- **AC-1**: Panel events route without focus changes
  - Given: a UIManager test double with active Chart focus and `FeedbackManager` subscribed
  - When: `ui_panel_opened` and `ui_panel_closed` are emitted
  - Then: #17 records minor requests and the UI focus snapshot remains unchanged
  - Edge cases: repeated open/close in one frame; unknown panel ID

- **AC-2**: Route selected maps to visible confirmation
  - Given: UIManager emits `ui_route_selected(route_id, route_name)`
  - When: #17 consumes it
  - Then: the request uses the Chart cue family, includes route context, and has status or caption text available if audio is requested
  - Edge cases: empty route name; route ID only

- **AC-3**: Departure confirmed does not delay transition
  - Given: UIManager enters `CHART_DEPARTURE_CONFIRMED`
  - When: `ui_departure_confirmed` is emitted
  - Then: #17 records a major or critical request and UIManager transition state remains progressed
  - Edge cases: Hub departure path; chart route departure path

- **AC-4**: Core UI action events map to cue families
  - Given: threat response, repair submission, purchase confirmation, and item transfer events
  - When: #17 consumes each event
  - Then: each produces the expected cue family, priority band, and source system in diagnostics
  - Edge cases: zero quantity transfer; repair material list empty; purchase quantity one

- **AC-5**: Save/load events come from #2/#3
  - Given: Persistence or SessionShell emits save/load completion
  - When: #17 consumes the event
  - Then: `source_system` is `Persistence` or `SessionShell`, not `UIManager`, and visible completion status is requested
  - Edge cases: settings load vs progress load; repeated completion event

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/feedback-fx-audio/SemanticEventWiringTest.csproj` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003, Story 004, Story 005
