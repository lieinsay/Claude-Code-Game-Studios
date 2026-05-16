# Story 003: Accessible Fallbacks, Subtitles and Missing Assets

> **Epic**: Feedback, VFX, and Audio Semantics
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 governs active implementation; implement in Godot .NET/C# desktop code unless a later ADR grants an exception.

## Context

**GDD**: `design/gdd/feedback-fx-audio.md`
**Requirement**: `TR-feedback-001`, `TR-feedback-002`

**ADR Governing Implementation**: ADR-0016: missing asset fallback and subtitle request contract
**ADR Decision Summary**: Missing VFX/audio assets are non-fatal; meaningful audio must have visible text or subtitle fallback. Whenever a `FeedbackRequest` contains `CaptionText`, #17 emits or requests `subtitle_requested`.

**Engine**: Godot 4.6.2 .NET | **Risk**: HIGH
**Engine Notes**: No post-cutoff audio API is required. UI caption/status rendering remains UIManager or subtitle-layer owned.

**Control Manifest Rules (Presentation layer)**:
- Required: UIManager owns rendering; #17 requests status/caption output.
- Forbidden: Audio-only meaningful information; missing asset crash; direct UI focus mutation from fallback paths.
- Guardrail: Missing asset diagnostics are rate-limited in development and do not spam player-facing output.

---

## Acceptance Criteria

*From GDD `design/gdd/feedback-fx-audio.md`, scoped to this story:*

- [x] GIVEN a missing visual asset, WHEN the relevant feedback request is processed, THEN the visual cue is skipped, text/status fallback is preserved, and the game does not crash.
- [x] GIVEN a missing audio asset, WHEN the relevant feedback request is processed, THEN the audio cue is skipped, caption/status fallback is preserved, and the game does not crash.
- [x] GIVEN audio is muted or unavailable, WHEN a meaningful audio cue would play, THEN equivalent visible text or caption remains available.
- [x] GIVEN a feedback request contains `caption_text`, WHEN #17 processes it, THEN #17 emits or requests `subtitle_requested` and #16 or the subtitle layer can render the text.
- [x] GIVEN save or load completes, WHEN audio and VFX assets are missing, THEN player-facing text still confirms completion.
- [x] GIVEN the caption layer is unavailable, WHEN a caption would be requested, THEN #17 falls back to existing UI status text if present.
- [x] GIVEN color alone would carry meaning, WHEN a fallback is generated, THEN text, icon, motion, or label also carries the meaning.

---

## Implementation Notes

Derived from ADR-0016:

- Treat missing asset IDs as no-op for that channel plus a non-fatal, rate-limited diagnostic.
- Meaningful audio requires `CaptionText` or `StatusText`; if `CaptionText` is present, request `subtitle_requested(caption_text, event_id, priority)`.
- Caption duration should follow the GDD formula: `clamp(1.5 + character_count / 14.0, 2.0, 6.0)`.
- Muted audio/device-unavailable should not change gameplay state and should not suppress visible feedback.
- Save/load completion should always have a text-readable fallback because Session feedback is critical for player trust.

---

## Out of Scope

- Story 002 owns event source wiring.
- Story 004 owns visual overlay placement and focus isolation.
- Full subtitle settings UI is outside this epic.
- Final authored VFX/audio asset production is outside this epic.

---

## QA Test Cases

- **AC-1**: Missing visual asset skips only visual channel
  - Given: a request with a missing `VisualCueId` and valid `StatusText`
  - When: the request is processed
  - Then: no exception is thrown, visual output is skipped, status output remains, and a non-fatal diagnostic is recorded
  - Edge cases: missing visual plus missing audio; duplicate missing asset in same cooldown window

- **AC-2**: Missing audio asset keeps text fallback
  - Given: a request with a missing `AudioCueId` and meaningful caption/status text
  - When: the request is processed
  - Then: audio output is skipped and visible text remains available
  - Edge cases: audio disabled globally; device unavailable

- **AC-3**: Caption text requests subtitle
  - Given: a request with `CaptionText`
  - When: #17 processes the request
  - Then: `subtitle_requested` or equivalent subtitle-layer request is emitted with text, event ID, and priority
  - Edge cases: empty caption rejected or converted to status fallback; long caption duration capped at 6.0s

- **AC-4**: Save/load remains text-readable without assets
  - Given: save/load completion requests with missing audio and visual assets
  - When: #17 processes them
  - Then: player-facing "save complete" / "load complete" text is still requested
  - Edge cases: rapid save/load completion; load after queue clear

- **AC-5**: Color-only fallback is invalid
  - Given: a fallback cue definition that encodes meaning only by color
  - When: the fallback is validated
  - Then: diagnostics reject it or add text/icon/label metadata before output
  - Edge cases: small status element under 24px; warning vs success colors

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/feedback-fx-audio/AccessibleFallbacksTest.csproj` — must exist and pass
- `production/qa/evidence/feedback-fx-audio-accessibility-evidence.md` — manual accessibility note if visual/audio rendering is involved

**Status**: [x] Created and passing — `dotnet run --project tests/integration/feedback-fx-audio/AccessibleFallbacksTest.csproj -p:UseSharedCompilation=false` passed 7/7 on 2026-05-16.

---

## Dependencies

- Depends on: Story 001
- Unlocks: Story 004, Story 005

## Completion Notes

**Completed**: 2026-05-16
**Criteria**: 7/7 passing.
**Deviations**: None. The implementation remains headless C# presentation routing; final authored VFX/audio asset playback and visual overlay placement stay in downstream Stories 004/005.
**Test Evidence**: Integration evidence at `tests/integration/feedback-fx-audio/AccessibleFallbacksTest.csproj`; `dotnet run --project tests/integration/feedback-fx-audio/AccessibleFallbacksTest.csproj -p:UseSharedCompilation=false` PASS 7/7. Story 001 and Story 002 feedback regression runners also PASS, and `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 0 warnings, 0 errors.
**Code Review**: Complete — `$code-review src/presentation/FeedbackManager.cs tests/integration/feedback-fx-audio/AccessibleFallbacksProgram.cs` approved after sanitized output and caption-layer audio fallback fixes.
