# ADR-0016: Feedback, VFX, and Audio Semantics

## Status

Accepted

## Date

2026-05-15

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 .NET |
| **Domain** | Audio / UI / Rendering / Presentation |
| **Knowledge Risk** | HIGH — Godot 4.6.2 is post-cutoff; audio APIs are stable, but UI focus and rendering behavior must follow 4.6.2 references. |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/modules/audio.md`, `docs/engine-reference/godot/modules/ui.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md`, `design/gdd/feedback-fx-audio.md` |
| **Post-Cutoff APIs Used** | None required for audio. UI overlays must respect Godot 4.6 dual-focus and 4.5 recursive Control disable behavior. |
| **Verification Required** | Missing asset fallback, audio-muted fallback, subtitle request emission, focus non-interference while Chart/Exploration HUD is active, and no added frame/save-load timing regressions. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload order), ADR-0002 (signal protocol), ADR-0003 (save/load events), ADR-0012 (UIManager semantic events and focus routing), ADR-0019 (desktop C# pivot) |
| **Enables** | #17 Feedback/VFX/Audio Polish implementation stories |
| **Blocks** | None after acceptance; this ADR governs future #17 VFX/audio implementation stories |
| **Ordering Note** | This ADR may be implemented after UI/HUD #16 is stable. It must not be used to change #16 focus ownership or domain state ownership. |

## Context

### Problem Statement

The project now has a formal #17 GDD, a UI/HUD semantic event table, a C# `FeedbackManager` stub, and production evidence that minimum MVP feedback works through #16. Full Polish feedback still lacks an accepted technical contract for how semantic events become visual cues, audio cues, status text, and subtitles without introducing state coupling, focus leakage, missing-asset crashes, or audio-only accessibility failures.

### Constraints

- ADR-0019 makes desktop Godot .NET/C# the active implementation target.
- ADR-0002 requires typed, emit-after-mutation signal patterns and bounded cascade depth.
- ADR-0012 owns UI layout, modal/input routing, active panel focus isolation, and `ui_*` semantic events.
- #17 must consume state after mutation and must never write domain state.
- Meaningful audio must produce visible text or subtitle fallback per `design/accessibility-requirements.md`.
- Missing VFX/audio assets must be non-fatal.

### Requirements

- Consume UI semantic events from #16 and session/persistence events from #2/#3.
- Normalize events into feedback requests with priority, coalescing, optional visual/audio IDs, and visible fallback text.
- Emit or request `subtitle_requested` whenever `caption_text` conveys meaningful audio information.
- Preserve active Chart/Exploration focus isolation; overlays must not steal focus or mouse input.
- Keep first implementation minimal: event router, fallbacks, and QA hooks before final authored assets.

## Decision

Implement #17 as a C# `FeedbackManager` presentation service behind a typed semantic feedback router. `FeedbackManager` remains a presentation-layer consumer: it subscribes to approved semantic events, converts each event into a `FeedbackRequest`, dispatches optional visual/audio/caption/status outputs, and exposes diagnostics for tests.

### Architecture Diagram

```text
Domain Systems / UIManager / SessionShell / Persistence
    emit-after-mutation semantic events
        |
        v
FeedbackManager (#17)
    - validates event id and payload shape
    - builds FeedbackRequest
    - applies priority, cooldown, coalescing
    - routes outputs
        |
        +--> UIManager status/caption layer
        +--> Audio cue pool
        +--> Visual cue overlay/VFX anchor
        +--> QA diagnostics
```

### Key Interfaces

```csharp
public enum FeedbackPriority
{
    Ambient = 0,
    Minor = 1,
    Major = 2,
    Critical = 3,
}

public sealed record FeedbackRequest(
    string EventId,
    string SourceSystem,
    FeedbackPriority Priority,
    string CoalesceKey,
    string? VisualCueId,
    string? AudioCueId,
    string? CaptionText,
    string? StatusText,
    IReadOnlyDictionary<string, object?> Payload);

public interface IFeedbackSink
{
    void RequestFeedback(FeedbackRequest request);
}
```

Approved first-pass event sources:

| Source | Events |
|---|---|
| #16 UIManager | `ui_panel_opened`, `ui_panel_closed`, `ui_route_selected`, `ui_departure_confirmed`, `ui_threat_response_chosen`, `ui_repair_submitted`, `ui_purchase_confirmed`, `ui_item_transferred` |
| #2/#3 Session/Persistence | `ui_save_completed`, `ui_load_completed` or their final typed equivalents from Persistence/SessionShell |
| Domain systems | `threat_resolved`, `repair_completed`, `voyage_completed`, `purchase_completed`, and related events may map into #17 after the first router story |

Caption rule:

```text
if request.CaptionText is not null:
    emit subtitle_requested(request.CaptionText, request.EventId, request.Priority)
```

Focus rule:

```text
Feedback overlays are visual-only unless explicitly opened as a future settings panel.
Default Godot Control behavior: mouse_filter = Ignore and focus disabled.
```

## Alternatives Considered

### Alternative 1: Keep feedback inside UIManager only

- **Description**: #16 would directly play all audio, VFX, captions, and status cues.
- **Pros**: Fewer objects and less routing.
- **Cons**: Expands UIManager beyond layout/input ownership and couples domain feedback semantics to screen implementation.
- **Rejection Reason**: #16 already owns the largest presentation surface. #17 exists to keep semantic feedback routing separate from UI state ownership.

### Alternative 2: Direct domain-to-asset playback

- **Description**: Each domain system directly plays audio/VFX when it mutates state.
- **Pros**: Local implementation is simple.
- **Cons**: Violates separation of concerns, duplicates fallback logic, and makes accessibility coverage inconsistent.
- **Rejection Reason**: Domain systems should emit state facts; presentation systems decide how to express them.

### Alternative 3: Full asset pipeline before router

- **Description**: Build final authored VFX/audio content and mix before implementing event routing.
- **Pros**: Polished output sooner for a small subset.
- **Cons**: High asset cost before contracts are stable; missing-asset and subtitle behavior remains untested.
- **Rejection Reason**: First Polish scope needs a stable contract and fallback behavior before asset production.

## Consequences

### Positive

- Event routing, fallbacks, captions, and test diagnostics are centralized.
- UIManager remains the owner of layout/focus and does not become a feedback asset system.
- Missing assets and muted audio have a consistent non-fatal path.
- Future authored VFX/audio can plug into a stable request contract.

### Negative

- One more presentation service must be initialized, tested, and kept in event-order sync.
- The first implementation may feel utilitarian until final authored assets are produced.
- ADR-0002 / architecture registry must be extended with #17-specific output events such as `subtitle_requested`.

### Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Feedback event cascade exceeds ADR-0002 depth expectations | Medium | Feedback outputs are terminal presentation requests; they must not write back into domain systems. |
| Audio-only information slips through without captions | Medium | `caption_text` requires `subtitle_requested`; QA checks meaningful audio with muted audio. |
| Overlay steals focus from Chart or Exploration HUD | High | Default `mouse_filter=Ignore`, no focus mode, and regression coverage for active panel isolation. |
| Missing assets crash or spam diagnostics | Medium | Missing asset path is no-op plus rate-limited warning in development builds. |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `feedback-fx-audio.md` | Consume semantic events after state mutation without writing domain state. | `FeedbackManager` is a presentation-only consumer and exposes `FeedbackRequest` outputs. |
| `feedback-fx-audio.md` | Missing visual/audio assets degrade safely. | Missing assets are no-op/status fallback with diagnostics. |
| `feedback-fx-audio.md` | Meaningful audio has visible text or subtitle. | `caption_text` triggers `subtitle_requested`. |
| `ui-hud-chart-interface.md` | #16 emits `ui_*` semantic events for #17. | ADR-0016 consumes the approved first-pass event set from #16. |
| `accessibility-requirements.md` | Subtitle path for meaningful audio. | #17 owns the subtitle request emission contract; #16 renders the UI layer. |

## Performance Implications

- **CPU**: Expected O(1) per event plus small dictionary validation; no per-frame work when the queue is empty.
- **Memory**: Small cue registry and optional audio player pool; first implementation should stay below 1 MiB resident overhead.
- **Load Time**: Router initializes during boot; authored asset loading should be lazy or preloaded only for critical cues.
- **Network**: None.

## Migration Plan

1. Extend the existing `src/presentation/FeedbackManager.cs` stub with `FeedbackRequest`, priority, coalescing, and diagnostics.
2. Wire #16 UI semantic events and #2/#3 save/load completion events into `FeedbackManager`.
3. Add caption/status fallback output to UIManager or a UI-owned subtitle layer.
4. Add tests for missing assets, muted audio, subtitle request, event coalescing, and focus non-interference.
5. Defer authored VFX/audio asset work until the router and QA evidence pass.

## Validation Criteria

- Missing visual/audio asset references do not crash.
- Muted audio still shows text/caption fallback.
- `caption_text` produces `subtitle_requested`.
- Chart and Exploration HUD focus isolation tests continue to pass with feedback overlays active.
- Save/load, route departure, pressure loop, and return-Hub smoke checks remain passing.
- Numeric smoke performance remains within the current budgets.

## Related Decisions

- ADR-0001: Autoload and scene boot order.
- ADR-0002: Signal communication protocol.
- ADR-0003: Save system and persistence events.
- ADR-0012: UIManager input routing and semantic UI events.
- ADR-0019: Desktop Godot .NET/C# platform pivot.
- GDD #17: `design/gdd/feedback-fx-audio.md`.
