# Feedback, VFX, and Audio Semantics

> **Status**: Approved - ADR-0016 Accepted
> **System**: #17 Feedback, VFX, and Audio Semantics
> **Priority**: Vertical Slice
> **Last Updated**: 2026-05-15
> **Source Scope Brief**: `production/polish-backlog/feedback-fx-audio-scope-brief-2026-05-15.md`

## 1. Overview

Feedback, VFX, and Audio Semantics defines how gameplay and UI semantic events become readable audiovisual feedback. It consumes stable events from UI/HUD, route risk, exploration, threat handling, repair, market, and save/load systems, then maps them to non-blocking visual cues, short audio cues, and visible text or subtitle fallbacks. This system does not own gameplay state, resolve domain rules, or replace the MVP clarity already delivered by UI/HUD #16.

## 2. Player Fantasy

The player should feel that the airship, chart, and cloud world respond immediately and intelligibly to their actions. Selecting a route should feel deliberate, pressure changes should feel legible rather than surprising, repair should feel like the world answering back, and danger should be noticeable without turning the game into a noisy alert panel. Feedback must support the quiet航路修复主义 tone: crafted, tactile, readable, and restrained.

## 3. Detailed Rules

### Ownership Rules

- #17 consumes semantic events after the owning domain system has already changed state.
- #17 may request visuals, audio, captions, haptics-like screen pulses, or status text, but it must not mutate route, inventory, hull, repair, market, save, or onboarding state.
- Missing feedback assets degrade to a no-op or visible text fallback without blocking the game loop.
- Meaningful audio cues must have an equivalent visible text, caption, or status update.
- Visual cues must be brief, non-modal, and must not capture keyboard focus or mouse input unless a future settings UI explicitly belongs to #17.
- Full VFX/audio implementation must follow ADR-0016 for FeedbackManager initialization, event routing, audio readiness, and asset fallback behavior.

### MVP-to-Polish Event Set: UI Events

The first Polish implementation must support these #16 semantic events:

| Event | Required Feedback Intent | Minimum Fallback |
|---|---|---|
| `ui_panel_opened` | Confirm panel context changed | Panel title/status text remains visible |
| `ui_panel_closed` | Confirm player returned to prior surface | Focus/selection state remains visible |
| `ui_route_selected` | Confirm selected route and risk identity | Route label or selected-route text |
| `ui_departure_confirmed` | Confirm irreversible departure | Departure status text |
| `ui_threat_response_chosen` | Confirm response and consequence band | Threat feedback label |
| `ui_repair_submitted` | Confirm repair attempt or completion | Repair result text |
| `ui_purchase_confirmed` | Confirm purchase and stock/resource change | Purchase status text |
| `ui_item_transferred` | Confirm cargo/storage movement | Inventory/cargo delta text |

### MVP-to-Polish Event Set: Session and Persistence Events

Save/load completion is not a #16 UI event contract. It is consumed as a session/persistence semantic event from #2/#3 and may be displayed by #16:

| Event | Source | Required Feedback Intent | Minimum Fallback |
|---|---|---|---|
| `ui_save_completed` | #3 persistence via #2 session shell | Confirm save completed | Save status text |
| `ui_load_completed` | #3 persistence via #2 session shell | Confirm load completed | Load status text |

### Feedback Request Contract

Each semantic event is normalized into a feedback request:

| Field | Meaning |
|---|---|
| `event_id` | Stable semantic event name |
| `source_system` | Owning domain or presentation system |
| `priority` | `critical`, `major`, `minor`, or `ambient` |
| `coalesce_key` | Key used to merge repeated events |
| `visual_cue_id` | Optional cue family and variant |
| `audio_cue_id` | Optional audio cue family and variant |
| `caption_text` | Required when audio conveys meaningful information |
| `status_text` | Optional short UI status fallback |
| `payload` | Typed contextual data, read-only to #17 |

When `caption_text` is present, #17 must emit or request the accessibility-facing `subtitle_requested` signal/event required by `design/accessibility-requirements.md`. UI rendering remains owned by #16 or its subtitle layer.

### Priority Rules

- Critical cues are reserved for irreversible state, failure, danger, or load/save recovery outcomes.
- Major cues communicate deliberate player choices such as route departure, repair, purchase, or threat response.
- Minor cues communicate navigation, panel, selection, and item transfer confirmation.
- Ambient cues communicate low-importance environmental response and may be skipped under load.
- Higher-priority cues may interrupt lower-priority cues in the same presentation channel.
- Repeated cues with the same `coalesce_key` merge inside the coalescing window.

### Cue Families

| Family | Examples | Notes |
|---|---|---|
| Chart | route selected, departure confirmed, route risk pulse | Must preserve route readability |
| Exploration HUD | resource pressure, threat response, hull change | Must not obscure controls or feedback labels |
| Repair | repair submitted, repair complete, unlock visible | Must make permanent change feel distinct |
| Market/Inventory | purchase, item transfer, stock changed | Must remain light and quick |
| Session | save complete, load complete, restore warning | Must be text-readable without audio |
| Global Warning | error, blocked action, critical damage | Use sparingly |

## 4. Formulas

### Feedback Priority Score

`priority_score = base_priority + urgency_bonus + novelty_bonus - cooldown_penalty`

Variables:

- `base_priority`: critical = 100, major = 60, minor = 30, ambient = 10.
- `urgency_bonus`: 0-25, based on domain-provided urgency such as hull danger or irreversible departure.
- `novelty_bonus`: 0-10, applied when the event has not appeared recently.
- `cooldown_penalty`: 0-50, based on repeated events inside the cooldown window.

The highest `priority_score` plays first when channels conflict.

### Coalescing Window

`should_coalesce = (event.coalesce_key == previous.coalesce_key) and (now - previous.time <= coalesce_window_seconds)`

Default `coalesce_window_seconds = 0.25`.

### Cue Cooldown Remaining

`cooldown_remaining = max(0, last_played_at + cooldown_seconds - now)`

Events with remaining cooldown may still update visible status text, but their audio/VFX may be skipped.

### Caption Duration

`caption_duration_seconds = clamp(1.5 + character_count / 14.0, 2.0, 6.0)`

Short captions stay long enough to read; longer captions are capped to avoid blocking the UI.

### Channel Budget

`active_channel_count <= max_channels_by_priority[highest_active_priority]`

Default channel budgets:

| Highest Active Priority | Max Simultaneous Channels |
|---|---:|
| critical | 3 |
| major | 2 |
| minor | 1 |
| ambient | 1 |

## 5. Edge Cases

| Edge Case | Expected Handling |
|---|---|
| Missing visual asset | Skip visual cue; preserve text/status fallback; log non-fatal diagnostic in development. |
| Missing audio asset | Skip audio cue; preserve caption/status fallback; no crash. |
| Duplicate semantic events in one frame | Coalesce by `coalesce_key`; keep newest payload where safe. |
| Critical and ambient cue conflict | Critical cue plays; ambient cue is delayed or dropped. |
| Audio disabled or device unavailable | All meaningful audio must remain visible through captions/status text. |
| Caption layer unavailable | Fall back to existing UI status text if present. |
| Scene transition during active cue | Stop or fade non-critical cues; do not hold references to freed nodes. |
| Load restores a state with old queued feedback | Clear transient cue queue on load; allow load-complete status. |
| Payload missing optional context | Use generic cue variant and safe fallback text. |
| Color-only cue would carry meaning | Add icon, text, motion, or label; color alone is invalid. |
| Rapid save/load spam | Coalesce repeated completion cues while preserving latest status. |
| Focused modal or chart is open | Feedback overlay must not steal focus or expose underlying Hub controls. |

## 6. Dependencies

| System | Dependency Type | Contract |
|---|---|---|
| #16 UI / HUD / 航图界面 | Hard | Emits stable UI semantic events and owns layout/focus. |
| #2 平台与会话壳 | Hard for polished audio/session feedback | Provides audio readiness, mute/resume context, and session lifecycle events. |
| #10 航行与路线风险 | Soft for first Polish, hard for expanded route feedback | Supplies route risk and encounter semantic events. |
| #11 探索 / 搜撤场景 | Soft for first Polish, hard for expanded exploration feedback | Supplies exploration outcome and pressure events. |
| #12 战斗与威胁处理 | Soft | Supplies threat response and danger events. |
| #13 世界修复与解锁 | Soft for first Polish, hard for repair polish | Supplies repair submitted/completed/unlock events. |
| #14 空港 / 村镇状态与集市交易 | Soft | Supplies purchase and stock-change events. |
| #3 本地存档与世界状态持久化 | Soft | Supplies save/load completion or recovery events. |
| Accessibility requirements | Hard | Meaningful audio and color cues need equivalent visible information. |

## 7. Tuning Knobs

| Knob | Default | Range | Owner |
|---|---:|---:|---|
| `coalesce_window_seconds` | 0.25 | 0.1-0.5 | UI / Audio |
| `minor_cue_cooldown_seconds` | 0.4 | 0.0-1.5 | UI |
| `major_cue_cooldown_seconds` | 0.8 | 0.2-2.0 | UI / Audio |
| `ambient_cue_cooldown_seconds` | 3.0 | 1.0-10.0 | Audio |
| `caption_chars_per_second` | 14 | 10-20 | Accessibility |
| `visual_pulse_duration_seconds` | 0.35 | 0.15-0.75 | Technical Art |
| `critical_pulse_duration_seconds` | 0.7 | 0.35-1.2 | Technical Art |
| `max_simultaneous_minor_cues` | 1 | 0-2 | UI |
| `audio_ducking_db_for_critical` | -4 | -8-0 | Audio |
| `missing_asset_log_level` | warning | silent/warning/error | QA / Dev |

## 8. Acceptance Criteria

- GIVEN a supported #16 semantic event, WHEN it is emitted after state mutation, THEN #17 creates a feedback request without writing back to domain state.
- GIVEN a route is selected, WHEN `ui_route_selected` is consumed, THEN the route receives a visible selection confirmation and any optional audio has a text-equivalent path.
- GIVEN departure is confirmed, WHEN `ui_departure_confirmed` is consumed, THEN a major or critical cue confirms the irreversible transition without delaying the transition.
- GIVEN Exploration HUD pressure changes, WHEN resource, threat, or hull feedback events are consumed, THEN visible labels remain readable and no control is obscured.
- GIVEN save or load completes, WHEN the session event is consumed, THEN player-facing text still confirms completion even when audio and VFX assets are missing.
- GIVEN a missing visual or audio asset, WHEN the relevant feedback request is processed, THEN the game does not crash and the UI loop remains understandable.
- GIVEN audio is muted or unavailable, WHEN a meaningful audio cue would play, THEN equivalent visible text or caption remains available.
- GIVEN a feedback request contains `caption_text`, WHEN #17 processes it, THEN #17 emits or requests `subtitle_requested` and #16 or the subtitle layer can render the text.
- GIVEN Chart or Exploration HUD is active, WHEN feedback overlays appear, THEN underlying Hub controls do not regain focus and mouse input remains routed to the active surface.
- GIVEN repeated identical events arrive rapidly, WHEN they share a `coalesce_key`, THEN cues are merged or rate-limited while the latest status remains visible.
- GIVEN `ui_save_completed` or `ui_load_completed` is consumed, WHEN the source event is traced, THEN the source is #2/#3 session or persistence state rather than the #16 UI event table.
- GIVEN automated or manual QA runs the Hub -> Chart -> Exploration -> Save/Load -> Hub smoke loop, WHEN #17 hooks are connected, THEN existing UI/HUD and accessibility regression checks still pass.
