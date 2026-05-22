# Onboarding Focus-Safe Hints Evidence

> Date: 2026-05-22
> Scope: Epic #18 Story 004 -- Focus-Safe Hint Rendering and Accessibility
> Verdict: PASS for focused headless UI/hint regression evidence

## Evidence Summary

- `UIManager.RenderOnboardingHint(...)` records a focus-safe onboarding hint snapshot without changing keyboard focus, mouse hover, modal stack, screen state, or gameplay/domain state.
- One visible onboarding hint is kept by default; a later hint replaces the prior visible snapshot.
- Chart and Exploration active surfaces reject stale Hub anchors before rendering.
- Exploration pressure hints preserve resource, threat, hull, search/status readability guards.
- Missing or unsafe anchors fall back to a safe text-only hint; missing hint text is skipped with diagnostics.
- Every rendered hint snapshot has text and explicitly marks `ColorOnlyMeaning = false`.

## Automated Checks

- `dotnet run --project tests/integration/onboarding-first-loop/FocusSafeHintRenderingTest.csproj`
  - PASS 7/7 checks.

## Acceptance Mapping

| AC | Evidence |
|----|----------|
| AC-1 Hint does not steal keyboard or mouse input | Snapshot asserts focus disabled, `MouseFilterMode.Ignore`, non-modal, no keyboard/mouse capture, and unchanged focused element. |
| AC-2 Chart ignores Hub anchors | Chart-active render request for `hub.hud.status` is skipped with `inactive_hub_anchor`. |
| AC-3 Exploration pressure hint avoids feedback labels | Readability guard preserves `S5_hull_bar`, `S5_search_count`, `S5_threat_preview`, `S5_carried_grid`, and status text. |
| AC-4 Text and non-color-only meaning | Rendered hints require non-empty text keys and mark color-only meaning false. |
| AC-5 Keyboard-only and mouse-only paths remain valid | Focus activation remains valid before/after hint render; hint mouse filter ignores pointer input. |
| AC-6 Missing/unsafe anchor fallback | Unsafe anchor request renders a text-only fallback on `onboarding.safe_text` without crash or focus capture. |

## Manual Carry-Forward

Story 005 should validate the same behavior in the full playable smoke/manual route once hints are connected to the runtime scene.
