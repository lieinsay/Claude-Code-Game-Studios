# Onboarding and First Loop

> **Status**: Approved - ADR-0017 Accepted
> **System**: #18 Onboarding and First Loop
> **Priority**: Vertical Slice
> **Last Updated**: 2026-05-15
> **Source Scope Brief**: `production/polish-backlog/onboarding-first-loop-scope-brief-2026-05-15.md`

## 1. Overview

Onboarding and First Loop defines low-intrusion guidance for a new player completing the first playable loop: Hub -> Chart -> route selection -> Exploration HUD -> save/load awareness -> return to Hub -> notice changed summaries. It consumes UI focus, panel, highlight, and progress events from existing systems, then presents contextual hints, highlights, and optional checklist text without taking ownership of domain state or blocking player exploration.

## 2. Player Fantasy

The player should feel like they discovered how the airship works rather than being forced through a tutorial. The first loop should make the home, chart, voyage, pressure feedback, and return summary understandable enough that curiosity carries the player forward. Guidance should feel like the airship quietly pointing attention toward useful instruments, not like an external training overlay.

## 3. Detailed Rules

### First-Loop Steps

The onboarding system tracks these first-loop steps:

| Step ID | Player Goal | Completion Signal |
|---|---|---|
| `find_hub_hud` | Identify the Hub/HUD entry surface | Hub UI visible and input reachable |
| `open_chart` | Open Chart by visible entry or keyboard shortcut | Chart panel opened |
| `select_route` | Select a route | Route selection event received |
| `depart_route` | Confirm departure into Exploration HUD | Departure confirmed and Exploration HUD visible |
| `advance_pressure` | Advance the Exploration HUD pressure loop | Resource/threat/hull feedback changes at least once |
| `notice_save_load` | Notice or use Save/Load entries | Save/Load controls visible or used |
| `return_hub` | Return to Hub | Return event processed and Hub visible |
| `notice_summary_change` | Notice cargo/storage/hull/route summary change | Hub summary changed after return |

### Guidance Rules

- Hints are contextual and non-modal. They must not pause the game, block input, or require a specific tutorial action.
- Onboarding overlays must use `MOUSE_FILTER_IGNORE` or equivalent behavior unless a future settings panel is explicitly interactive.
- Hints may highlight existing `highlightable` anchors and may sort candidates by `highlight_priority`.
- Hints must support both keyboard and mouse paths.
- Hints must have text labels and may not rely on color alone.
- If base UI is not discoverable, fix the base UI first; onboarding should clarify, not hide a UI defect.
- Completed hints are not shown again unless onboarding state is reset.
- Onboarding state persists through save/load enough to prevent repeated first-loop hints. Step completion belongs in a new `progress.onboarding` snapshot package under #3's `progress` artifact; player preference such as onboarding reset/disable may live in `settings.onboarding`. ADR-0017 approves this Vertical Slice save domain for implementation.
- #18 reads domain/UI events but does not mutate route, cargo, hull, repair, market, or save state.

### Hint Lifecycle

| State | Meaning | Exit Condition |
|---|---|---|
| `not_started` | No first-loop guidance has been initialized | Hub becomes visible |
| `eligible` | A hint can be shown for the next incomplete step | Delay/timing and focus conditions pass |
| `visible` | Hint or highlight is currently visible | Step completes, timeout expires, or focus changes |
| `completed` | Step is complete and should not repeat | Onboarding reset only |
| `suppressed` | Hint is temporarily hidden | New eligible context appears |

### First Polish Guidance Surfaces

| Surface | Allowed Guidance |
|---|---|
| Hub | Subtle highlight on Chart/HUD entry, one-line hint near active station, summary-change callout after return |
| Chart | Route selection hint and departure confirmation hint that does not steal chart focus |
| Exploration HUD | Pressure-loop hint near existing controls/feedback labels without covering them |
| Save/Load | One-line discoverability hint or status callout; no modal teaching |
| Return Summary | Short visible callout when cargo/storage/hull/route summaries update |

## 4. Formulas

### Hint Priority Score

`hint_priority_score = base_step_priority + blocker_bonus + time_unseen_bonus - completed_penalty - repeat_penalty`

Variables:

- `base_step_priority`: 10-100, authored by step importance.
- `blocker_bonus`: 0 or 40, applied when the player is likely stuck at a required next step.
- `time_unseen_bonus`: `min(seconds_since_step_became_eligible / 5, 20)`.
- `completed_penalty`: 999 when the step is complete.
- `repeat_penalty`: `repeat_count * 20`.

The eligible hint with the highest score may be shown, subject to focus and delay rules.

### Hint Display Duration

`hint_duration_seconds = clamp(2.0 + character_count / 16.0, 3.0, 7.0)`

### Repeat Nudge Delay

`repeat_delay_seconds = min(base_delay_seconds + repeat_count * 5.0, 30.0)`

Default `base_delay_seconds = 8.0`.

### First-Loop Progress

`first_loop_progress_percent = (completed_step_count / total_step_count) * 100`

Default `total_step_count = 8`.

### Highlight Candidate Sort

`highlight_score = highlight_priority + context_match_bonus - occlusion_penalty`

Variables:

- `highlight_priority`: value supplied by #16 anchor metadata.
- `context_match_bonus`: 0-50, based on the current incomplete step.
- `occlusion_penalty`: 0-100, higher when the highlight would cover labels or controls.

## 5. Edge Cases

| Edge Case | Expected Handling |
|---|---|
| Player uses keyboard shortcut before hint appears | Mark the relevant step complete; do not show stale hint. |
| Player opens Chart while Hub hint is visible | Hide Hub hint immediately and let Chart focus remain isolated. |
| Chart is open and underlying Hub has highlightable anchors | Ignore Hub anchors until Chart closes. |
| Save/Load controls are visible but not used | Mark `notice_save_load` complete if the player dwells on or focuses the entries long enough; using them also completes it. |
| Save/load restores mid-loop onboarding state | Restore completed steps and next eligible step; do not replay completed hints. |
| Highlight anchor missing | Show safe text-only hint near the active panel or skip the hint. |
| Hint would cover pressure feedback labels | Reposition, shorten, or delay the hint. |
| Mouse-only player never uses keyboard shortcut | Mouse path remains valid; no keyboard-only requirement is forced. |
| Keyboard-only player never clicks | Keyboard path remains valid; no mouse-only requirement is forced. |
| Player ignores hints | Do not block progress; reduce repeat frequency. |
| Colorblind or low-vision player | Hints include text and shape/position cues; color is never sole meaning. |
| First loop completes before onboarding initializes | Mark all observed completed steps and show only final summary-change callout if still relevant. |

## 6. Dependencies

| System | Dependency Type | Contract |
|---|---|---|
| #16 UI / HUD / 航图界面 | Hard | Provides panel events, focus state, highlight metadata, and safe overlay placement. |
| #2 平台与会话壳 | Soft for first Polish, hard for start/continue onboarding | Provides Start, Continue, Resume, and session lifecycle entry context. |
| #7 飞艇家园 Hub | Hard | Provides Hub entry points and return summary state. |
| #9 航图与航线规划 | Hard | Provides chart open, route selection, and departure signals. |
| #11 探索 / 搜撤场景 | Hard | Provides Exploration HUD progression and return signals for the smoke loop. |
| #13 世界修复与解锁 | Soft for first Polish, hard for expanded onboarding | Provides repair guidance and world-response learning beats. |
| #14 空港 / 村镇状态与集市交易 | Soft | Provides later market and settlement guidance beats. |
| #3 本地存档与世界状态持久化 | Hard | Persists onboarding completion state and restores it safely. |
| #17 反馈、特效与音频语义 | Optional | May improve cue presentation, but #18 must remain understandable without it. |
| Accessibility requirements | Hard | Hints require text labels, keyboard reachability, and non-color-only meaning. |

## 7. Tuning Knobs

| Knob | Default | Range | Owner |
|---|---:|---:|---|
| `base_hint_delay_seconds` | 8.0 | 3.0-20.0 | UX |
| `repeat_delay_increment_seconds` | 5.0 | 0.0-15.0 | UX |
| `max_repeat_delay_seconds` | 30.0 | 10.0-90.0 | UX |
| `hint_min_duration_seconds` | 3.0 | 2.0-5.0 | Accessibility |
| `hint_max_duration_seconds` | 7.0 | 4.0-12.0 | Accessibility |
| `save_load_notice_dwell_seconds` | 1.0 | 0.5-3.0 | UX / QA |
| `summary_change_callout_seconds` | 4.0 | 2.0-8.0 | UX |
| `highlight_occlusion_padding_px` | 8 | 4-20 | UI |
| `max_visible_hints` | 1 | 1-2 | UX |
| `keyboard_shortcut_hint_repeat_limit` | 2 | 0-5 | Accessibility |

## 8. Acceptance Criteria

- GIVEN a new player reaches Hub, WHEN the Hub/HUD entry is visible, THEN onboarding can identify the first eligible hint without stealing focus.
- GIVEN the player opens Chart by mouse or keyboard, WHEN Chart is active, THEN Hub guidance is hidden and underlying Hub anchors are not focusable through onboarding.
- GIVEN Chart is active, WHEN route selection is available, THEN the player can discover and select a route using keyboard only or mouse only.
- GIVEN departure is confirmed, WHEN Exploration HUD appears, THEN onboarding can guide the pressure-loop controls without covering resource, threat, hull, or status feedback labels.
- GIVEN Save/Load entries are visible, WHEN the player focuses, sees, or uses them, THEN onboarding can mark the save/load awareness step complete and visible save/load feedback remains intact.
- GIVEN the player returns to Hub after a loop, WHEN cargo, storage, hull, or route summaries change, THEN onboarding can call out the changed summary without blocking further input.
- GIVEN a step is completed, WHEN the game is saved and loaded, THEN the completed hint does not repeat unless onboarding state is reset.
- GIVEN onboarding completion is saved, WHEN #3 validates the snapshot package, THEN completion data is stored as `progress.onboarding` with valid snapshot fields and not as transient UI state.
- GIVEN hints are visible, WHEN accessibility is checked, THEN every hint has text and does not rely on color alone.
- GIVEN Chart or Exploration HUD is open, WHEN hints or highlights appear, THEN active surface focus isolation remains intact.
- GIVEN the first-loop smoke path runs, WHEN onboarding is enabled, THEN no existing UI/HUD, save/load, focus, or accessibility regression test fails.
