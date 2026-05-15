# ADR-0017: Onboarding and First Loop Guidance

## Status

Accepted

## Date

2026-05-15

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 .NET |
| **Domain** | UI / Input / Persistence / Presentation |
| **Knowledge Risk** | HIGH — Godot 4.6.2 is post-cutoff; onboarding overlays must respect dual-focus and recursive Control behavior. |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/modules/ui.md`, `docs/engine-reference/godot/modules/input.md`, `docs/engine-reference/godot/breaking-changes.md`, `docs/engine-reference/godot/deprecated-apis.md`, `design/gdd/onboarding-first-loop.md` |
| **Post-Cutoff APIs Used** | Godot 4.6 dual-focus behavior; 4.5 recursive Control disable / mouse-filter behavior where needed. |
| **Verification Required** | Keyboard-only loop, mouse-only loop, active panel focus isolation, save/load persistence of completed steps, and non-color-only hint readability. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (OnboardingManager reserved as VS autoload), ADR-0002 (signal protocol), ADR-0003 (progress snapshot validation), ADR-0012 (UIManager focus/input routing and highlight metadata), ADR-0019 (desktop C# pivot) |
| **Enables** | #18 Onboarding Polish implementation stories |
| **Blocks** | None after acceptance; this ADR governs future #18 onboarding implementation stories |
| **Ordering Note** | #18 may consume #17 cues when available, but it must remain understandable without #17. |

## Context

### Problem Statement

The MVP smoke loop is now playable and discoverable through #16, but formal onboarding remains deferred. #18 needs a technical boundary before implementation: where first-loop progress is stored, whether onboarding owns input, how hints/highlights avoid stealing focus, how it consumes #16 focus/panel state, and how save/load prevents repeated first-loop hints.

### Constraints

- Onboarding must be low-intrusion and non-modal.
- #16 owns screen flow, modal stack, focus routing, and highlight anchor metadata.
- #18 must not mutate route, cargo, hull, repair, market, or save state.
- #3 owns persistence validation and promotion; #18 may only provide a snapshot package.
- Godot 4.6 dual-focus means visual overlays cannot assume mouse hover and keyboard focus are the same thing.
- Hints need text and cannot rely on color alone.

### Requirements

- Track first-loop steps from Hub discovery through return-Hub summary change.
- Consume UI panel/focus events and domain progress events after mutation.
- Present hints/highlights as visual-only overlays with no focus or mouse capture by default.
- Persist completion as `progress.onboarding`; store preferences such as reset/disable separately under `settings.onboarding`.
- Support keyboard-only and mouse-only walkthroughs.

## Decision

Implement #18 as an `OnboardingManager` C# Vertical Slice service that consumes UI/domain/session events, maintains first-loop step state, produces hint requests for UIManager to render, and exports a `progress.onboarding` snapshot package for #3. UIManager remains the renderer and focus owner; OnboardingManager never directly owns Control focus or gameplay input.

### Architecture Diagram

```text
UIManager / Hub / Chart / Exploration / Persistence
    panel, focus, progress, save/load events
        |
        v
OnboardingManager (#18)
    - evaluates first-loop step completion
    - scores eligible hints
    - persists progress.onboarding
    - emits hint/highlight requests
        |
        v
UIManager hint layer
    - renders text/highlight
    - keeps active surface focus isolated
```

### Key Interfaces

```csharp
public enum OnboardingStepState
{
    NotStarted = 0,
    Eligible = 1,
    Visible = 2,
    Completed = 3,
    Suppressed = 4,
}

public sealed record OnboardingStepProgress(
    string StepId,
    OnboardingStepState State,
    int CompletionGeneration,
    int RepeatCount);

public sealed record OnboardingHintRequest(
    string StepId,
    string HintTextKey,
    string? HighlightAnchorId,
    int Priority,
    double DurationSeconds);

public interface IOnboardingHintSink
{
    void ShowOnboardingHint(OnboardingHintRequest request);
    void HideOnboardingHint(string stepId);
}
```

Persistence contract:

```text
domain_id = "progress.onboarding"
payload = {
  completed_steps: string[],
  suppressed_steps: string[],
  first_loop_complete: bool,
  schema_version: int
}
```

UI contract:

```text
Hint/highlight Controls must default to:
- focus disabled
- mouse_filter = Ignore
- no gameplay input consumption
- text label present
- color never the only signal
```

## Alternatives Considered

### Alternative 1: Put onboarding entirely inside UIManager

- **Description**: UIManager would track steps, render hints, and persist completion.
- **Pros**: Fewer services and simpler rendering.
- **Cons**: UIManager would gain meta-progression state and persistence ownership beyond #16.
- **Rejection Reason**: #16 owns presentation and focus; #18 owns first-loop guidance state.

### Alternative 2: Blocking tutorial modal

- **Description**: Use modal tutorial panels that require the player to perform steps in order.
- **Pros**: Easy to test and hard for players to miss.
- **Cons**: Contradicts the low-intrusion fantasy and risks breaking the Hub-as-home feeling.
- **Rejection Reason**: #18 GDD explicitly requires contextual, non-modal guidance.

### Alternative 3: No persistent onboarding state

- **Description**: Recompute hints from current runtime state every launch.
- **Pros**: Avoids adding a save domain.
- **Cons**: Repeats completed hints after load and cannot reliably suppress stale guidance.
- **Rejection Reason**: The GDD requires completed hints not to repeat after save/load.

## Consequences

### Positive

- First-loop guidance state is separated from UI rendering and domain state.
- UIManager keeps focus/input authority, reducing risk from Godot 4.6 dual-focus behavior.
- Save/load can suppress completed hints reliably.
- The onboarding system can be expanded later without changing base UI ownership.

### Negative

- Adds a new Vertical Slice save domain that #3 must validate.
- Requires careful event ordering so onboarding observes facts after domain/UI state changes.
- Requires a small UI hint rendering layer even before final authored onboarding content exists.

### Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Hint overlay steals focus or mouse input | High | UIManager renders hints with focus disabled and `mouse_filter=Ignore`. |
| Onboarding hides base UI discoverability defects | Medium | Rule: fix base UI first when a control is not discoverable without hints. |
| `progress.onboarding` violates #3 snapshot rules | Medium | Use plain data only; include schema version and stable step IDs. |
| Hints repeat after load | Medium | Save completed/suppressed step IDs under `progress.onboarding`. |
| #18 depends on #17 polish cues | Low | #17 is optional; #18 must remain text-readable without it. |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `onboarding-first-loop.md` | Low-intrusion contextual hints. | Hints are non-modal requests rendered by UIManager. |
| `onboarding-first-loop.md` | Hints must not steal focus. | UIManager owns rendering with focus disabled and `mouse_filter=Ignore`. |
| `onboarding-first-loop.md` | Completed hints persist through save/load. | Adds `progress.onboarding` snapshot package. |
| `ui-hud-chart-interface.md` | #18 consumes panel events and focus state. | OnboardingManager consumes #16 events and highlight metadata. |
| `local-save-world-state-persistence.md` | Persistence owns validation and promotion. | #18 only exports a snapshot package; #3 validates/promotes it. |

## Performance Implications

- **CPU**: O(number of onboarding steps) when relevant events arrive; no per-frame polling required.
- **Memory**: Small step state table and hint state; expected under 100 KiB.
- **Load Time**: Minimal; OnboardingManager can initialize after UIManager and restore from #3 when progress loads.
- **Network**: None.

## Migration Plan

1. Add `OnboardingManager` as the #18 Vertical Slice service only after ADR acceptance.
2. Define stable step IDs and `progress.onboarding` DTOs.
3. Wire #16 panel/focus events and current smoke-loop domain events into OnboardingManager.
4. Add UIManager hint/highlight rendering methods that preserve focus isolation.
5. Add save/load tests for completed and suppressed hint state.
6. Add keyboard-only, mouse-only, and focus-isolation QA coverage.

## Validation Criteria

- Keyboard-only player can complete Hub -> Chart -> Exploration -> Save/Load awareness -> Hub summary path.
- Mouse-only player can complete the same path.
- Chart and Exploration HUD retain focus isolation while hints are visible.
- Completed hints do not repeat after save/load.
- Hints have text and do not rely on color alone.
- Base UI remains understandable if onboarding is disabled.

## Related Decisions

- ADR-0001: Autoload and scene boot order.
- ADR-0002: Signal communication protocol.
- ADR-0003: Save system snapshot validation.
- ADR-0012: UIManager input routing, focus isolation, and semantic UI events.
- ADR-0016: Feedback semantics, optional cue presentation.
- ADR-0019: Desktop Godot .NET/C# platform pivot.
- GDD #18: `design/gdd/onboarding-first-loop.md`.
