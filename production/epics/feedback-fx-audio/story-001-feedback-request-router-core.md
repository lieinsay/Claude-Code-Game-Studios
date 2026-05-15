# Story 001: Feedback Request Router Core

> **Epic**: Feedback, VFX, and Audio Semantics
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/feedback-fx-audio.md`
**Requirement**: `TR-feedback-001`

**ADR Governing Implementation**: ADR-0016: Feedback, VFX, and Audio Semantics
**ADR Decision Summary**: Implement #17 as a C# `FeedbackManager` presentation service behind a typed semantic router. The manager validates event IDs and payload shape, builds immutable `FeedbackRequest` values, applies priority/cooldown/coalescing, and exposes diagnostics without writing domain state.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: Audio APIs are stable; UI focus/rendering behavior must follow Godot 4.6.2 references. No post-cutoff audio API is required for this story.

**Control Manifest Rules (Presentation layer)**:
- Required: UIManager remains the owner of screen state, modal stack, input routing, and focus management.
- Forbidden: #17 must not persist UI state or mutate domain state; no Dictionary payload cross-system signals.
- Guardrail: Router work is O(1) per event, no per-frame work when the queue is empty, and resident overhead should remain below 1 MiB for the first implementation.

---

## Acceptance Criteria

*From GDD `design/gdd/feedback-fx-audio.md`, scoped to this story:*

- [ ] GIVEN a supported #16 semantic event, WHEN it is emitted after state mutation, THEN #17 creates a feedback request without writing back to domain state.
- [ ] GIVEN a semantic event is normalized, WHEN #17 builds a request, THEN it records `event_id`, `source_system`, `priority`, `coalesce_key`, optional `visual_cue_id`, optional `audio_cue_id`, optional `caption_text`, optional `status_text`, and read-only `payload`.
- [ ] GIVEN multiple candidate feedback requests, WHEN channel conflict occurs, THEN `priority_score = base_priority + urgency_bonus + novelty_bonus - cooldown_penalty` chooses the highest-priority request deterministically.
- [ ] GIVEN repeated identical events arrive rapidly, WHEN they share a `coalesce_key` within the default 0.25s window, THEN cues are merged or rate-limited while the latest status remains visible.
- [ ] GIVEN the feedback queue is empty, WHEN frames advance, THEN `FeedbackManager` does no per-frame polling work.
- [ ] GIVEN tests inspect diagnostics, WHEN requests are routed, coalesced, or skipped, THEN diagnostics expose enough data to verify event ID, priority, coalesce key, and output decisions.

---

## Implementation Notes

Derived from ADR-0016:

- Add `FeedbackPriority` values `Ambient=0`, `Minor=1`, `Major=2`, `Critical=3`.
- Add immutable `FeedbackRequest` with the ADR-0016 fields exactly: `EventId`, `SourceSystem`, `Priority`, `CoalesceKey`, `VisualCueId`, `AudioCueId`, `CaptionText`, `StatusText`, `Payload`.
- Keep `FeedbackManager` presentation-only. It may emit/request visual, audio, caption, status, and diagnostic outputs, but it must not call domain mutation methods.
- Use stable semantic event IDs from the GDD and UIManager constants; reject or diagnose unsupported event IDs without crashing.
- Coalescing uses `coalesce_key` and default `coalesce_window_seconds = 0.25`.
- Higher-priority cue channels may interrupt lower-priority channels; lower-priority ambient cues may be delayed or dropped.

---

## Out of Scope

- Story 002 wires live #16 UIManager and #2/#3 session/persistence events.
- Story 003 implements missing asset, muted audio, and subtitle output behavior.
- Story 004 renders focus-safe visual overlays.
- Story 005 runs end-to-end smoke/performance regression.

---

## QA Test Cases

- **AC-1**: Supported event creates request without state writes
  - Given: a `FeedbackManager` initialized with a fake domain write sentinel and a supported event ID
  - When: the event is consumed after the simulated owner mutation flag is set
  - Then: one `FeedbackRequest` is recorded and the domain write sentinel remains untouched
  - Edge cases: unsupported event ID, null optional payload fields, empty payload

- **AC-2**: Request field contract is complete
  - Given: a supported event with optional visual, audio, caption, status, and payload context
  - When: the event is normalized
  - Then: all ADR-0016 request fields are populated or explicitly null and payload is not mutated by #17
  - Edge cases: optional context missing, unknown payload key, source system omitted

- **AC-3**: Priority score is deterministic
  - Given: ambient, minor, major, and critical requests with known urgency, novelty, and cooldown values
  - When: the router selects the next output
  - Then: the request with the highest computed priority score is selected
  - Edge cases: tied score uses stable ordering; critical interrupts ambient

- **AC-4**: Coalescing keeps latest status
  - Given: two events with the same `coalesce_key` inside 0.25s
  - When: both are routed
  - Then: the cue is merged or rate-limited and the newest status text remains visible in diagnostics
  - Edge cases: same event outside 0.25s; different `coalesce_key` in the same frame

- **AC-5**: Idle queue has no per-frame work
  - Given: an initialized manager with no pending requests
  - When: the test advances a simulated frame tick
  - Then: no poll/update counter increments and no output is produced
  - Edge cases: queued request drains then next idle frame stays zero-cost

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/feedback-fx-audio/FeedbackRouterCoreTest.csproj` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: ADR-0016 Accepted, `src/presentation/FeedbackManager.cs` existing stub
- Unlocks: Story 002, Story 003
