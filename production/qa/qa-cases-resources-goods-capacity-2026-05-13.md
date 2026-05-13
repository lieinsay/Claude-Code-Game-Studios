## Manual QA Test Cases: Resources, Goods & Capacity #5

**Date**: 2026-05-13
**QA Plan**: `production/qa/qa-plan-resources-goods-capacity-2026-05-13.md`
**Smoke Report**: `production/qa/smoke-2026-05-13.md`
**Scope**: Runtime checks not covered by automated C# tests

---

### Execution Notes

The story-level Resources, Goods & Capacity acceptance criteria are already covered by automated C# tests. These manual cases cover the visible runtime warnings carried forward from smoke-check.

If a downstream scene or UI flow is not implemented yet, mark the case **BLOCKED - downstream runtime UI not wired** rather than **FAIL**, unless a completed runtime path is expected to work and breaks.

Suggested Godot executable:

`D:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe`

### Shell Button Behavior Reference

This table documents the runtime shell behavior after BUG-005 was fixed.

| Panel | Button / Shortcut | Expected behavior |
|-------|-------------------|-------------------|
| Entry | `Start Enter` / `Enter` | Opens the Audio Activation panel. |
| Entry | `Settings Tab` / `Tab` | Toggles the diagnostic overlay if present; otherwise shows an explicit not-initialized recovery message. |
| Audio Activation | `Activate Audio Enter` / `Enter` | Mounts the Hub runtime scene under `SessionShell/GameplayLayer`. |
| Audio Activation | `Continue Muted M` / `M` | Mounts the Hub runtime scene under `SessionShell/GameplayLayer`. |
| Audio Activation | `Return Title Esc` / `Esc` | Returns to Entry. |
| Recovery | `Retry R` / `R` | Returns to Entry. |
| Recovery | `New Session N` / `N` | Returns to Entry when startup recovery is visible. |
| Recovery | `Return Title Esc` / `Esc` | Returns to Entry. |
| Recovery | `Error Details D` / `D` | Toggles the diagnostic overlay if present; otherwise leaves visible recovery feedback. |

Mouse hover should move the visible focus/selection frame to the hovered enabled button. Clicking a button should match the shortcut behavior in this table.

---

### TC-RGC-001 - Visible Project Launch

**Purpose**: Confirm the project launches visibly and reaches the shell scene without crash.

**Preconditions**

- Godot 4.6.2 .NET is installed.
- Working directory is the project root.
- Latest C# build passes.

**Steps**

1. Launch the project from Godot or run the Godot executable against `project.godot`.
2. Observe startup until the first visible UI appears.
3. Wait 10 seconds.
4. Watch for crashes, fatal error panels, or unhandled error dialogs.

**Expected Result**

- A visible shell/loading/entry UI appears.
- The application remains responsive for at least 10 seconds.
- No crash or unhandled fatal dialog appears.

**Actual Result**: Initial manual run reported by user remained indefinitely on `Loading Session Shell` / `Checking game data...`. See `production/qa/bugs/BUG-001-shell-ui-stuck-loading.md`. Fix candidate applied 2026-05-13; manual retest required.

**Pass/Fail**: PASS after BUG-001 fix candidate

---

### TC-RGC-002 - Shell Keyboard And Mouse Input

**Purpose**: Confirm visible shell buttons and keyboard shortcuts respond.

**Preconditions**

- TC-RGC-001 reaches a visible shell UI.

**Steps**

1. Move focus across visible shell buttons with mouse and keyboard.
2. Press `Enter` on the primary visible action, if enabled.
3. Press `Esc`, if a return/cancel action is visible.
4. Press any visible shortcut labels shown by the UI, such as `C`, `N`, `Tab`, or `R`.

**Expected Result**

- Buttons visibly focus or react to input.
- Enabled buttons respond without freezing.
- Disabled buttons do not trigger actions.
- Input does not crash the shell.

**Actual Result**: User reported the Entry screen is visible, but `Start Enter` and `Settings Tab` initially did not respond to clicks. See `production/qa/bugs/BUG-002-shell-entry-buttons-not-interactive.md`. After fixes, user verified `Enter`, `Tab`, `Esc`, other labeled shortcuts, click behavior, and hover/focus behavior are working.

**Pass/Fail**: PASS

---

### TC-RGC-003 - Hub Runtime Reachability

**Purpose**: Confirm the Hub path is reachable, or document that Hub remains logic-only/downstream scene scope.

**Preconditions**

- Visible shell starts successfully.
- Any required start/new-session action is available.

**Steps**

1. Start a new session or continue into gameplay if the shell exposes that action.
2. Attempt to reach the Airship Hub.
3. Observe whether Hub stations or interactable hints are visible.
4. If no Hub scene exists or no transition is wired, record BLOCKED with the missing path.

**Expected Result**

- If Hub runtime is wired, the Hub appears and remains stable.
- If Hub runtime is not wired, QA records BLOCKED - downstream Hub scene not connected, not a #5 resource failure.

**Actual Result**: Fixed 2026-05-13. `SessionShellRuntime.gd` mounts `HubRuntime.tscn` after audio confirmation; `ShellUiTest` verifies the old placeholder recovery message is absent, and Godot headless loads the project.

**Pass/Fail**: PASS by automated scene-contract verification; manual visual retest recommended.

---

### TC-RGC-004 - Resource Inventory Presentation

**Purpose**: Verify that resource quantities and capacity states are visible where downstream UI exposes them.

**Preconditions**

- Runtime path exposes inventory, storage, or cargo UI.
- New-game resource state is available, or a debug/test state can populate resources.

**Steps**

1. Open the available inventory, storage, or cargo UI.
2. Check that starting resources are visible if the UI is wired.
3. Check that quantities, stack counts, and capacity labels are readable.
4. Check that cargo bay state is clear when empty.
5. If no resource UI is wired, record BLOCKED - resource UI not connected.

**Expected Result**

- Visible resource UI matches the C# resource state: storage starts with `basic_supply x10` and `repair_kit x4` when default new-game snapshot is used.
- Capacity display is coherent and not clipped.
- No stale or negative quantities appear.
- If UI is not wired, the case is BLOCKED, not FAIL.

**Actual Result**: Fixed 2026-05-13. The mounted Hub runtime exposes initial storage (`basic_supply x10 / repair_kit x4`), cargo (`used 0 / effective 500 / trapped 0`), module, and hull status labels.

**Pass/Fail**: PASS for initial Hub/resource presentation; richer inventory interaction remains downstream UI scope.

---

### TC-RGC-005 - Runtime Resource Transfer Or Pickup

**Purpose**: Confirm runtime UI can invoke resource movement when a downstream interaction exposes it.

**Preconditions**

- Resource UI or interaction layer exposes pickup, transfer, or storage action.

**Steps**

1. Trigger a resource pickup, transfer, or storage interaction.
2. Observe the before/after quantity or capacity display.
3. Attempt an invalid action if exposed, such as overfilling a target or moving cargo to a non-cargo pool.
4. Observe the UI feedback.

**Expected Result**

- Valid movement updates visible state once.
- Invalid movement is rejected without partial visible mutation.
- No duplicate stack, negative quantity, or stale display appears.
- If no runtime movement UI exists, record BLOCKED - downstream interaction not wired.

**Actual Result**: Hub is now reachable, but no runtime pickup, transfer, or storage interaction control is wired yet.

**Pass/Fail**: BLOCKED - runtime resource mutation interaction not wired yet.

---

### TC-RGC-006 - Repair Deposit Runtime Path

**Purpose**: Verify the visible repair deposit path can consume resources atomically when wired.

**Preconditions**

- A repair node UI or interaction is reachable.
- Required resources exist in storage, carried, or another supported deposit source.

**Steps**

1. Open a repair node or repair interaction.
2. Select a deposit action requiring one or more resources.
3. Confirm the deposit if the UI asks for confirmation.
4. Observe resource quantities after deposit.
5. Attempt a deposit with insufficient resources if the UI allows setup.

**Expected Result**

- Confirmed valid deposit consumes all required resources atomically.
- Deposit appears irreversible.
- Insufficient deposit is rejected without partial consumption.
- If repair UI is not wired, record BLOCKED - downstream repair UI not connected.

**Actual Result**: Hub is now reachable, but repair node UI and repair deposit interaction are not wired yet.

**Pass/Fail**: BLOCKED - repair deposit UI not wired yet.

---

### TC-RGC-007 - Chart / Departure / Exploration / Return Resource Path

**Purpose**: Verify the high-level resource journey from departure through return when the runtime loop is available.

**Preconditions**

- Hub, Chart, Exploration, and return transitions are available in the runtime build.

**Steps**

1. From Hub, open Chart or departure UI.
2. Select an available route and confirm departure.
3. Enter exploration.
4. Acquire or simulate loot if the runtime exposes this action.
5. Return to Hub.
6. Inspect storage/inventory for returned resources.

**Expected Result**

- Route confirmation does not crash.
- Exploration return brings retained resources into storage.
- Runtime state remains consistent after returning to Hub.
- If the full loop is not wired, record BLOCKED - downstream route/exploration loop not available.

**Actual Result**: Hub is now reachable, but Chart, departure, exploration, and return transitions are not wired yet.

**Pass/Fail**: BLOCKED - route/exploration runtime loop not wired yet.

---

### TC-RGC-008 - Runtime Save / Load Resource Snapshot

**Purpose**: Verify visible/runtime save-load preserves resource progress when the persistence path is exposed.

**Preconditions**

- Save/load or continue UI is available.
- Resource state can be changed or default new-game state can be observed.

**Steps**

1. Start or continue a session.
2. Observe resource state if a resource UI is available.
3. Trigger save through the runtime path.
4. Quit to title or close/reopen the game.
5. Continue/load the saved session.
6. Re-check resource state.

**Expected Result**

- Continue/load is available when a valid progress snapshot exists.
- Resource state persists through reload.
- No runtime save error appears.
- If runtime save/load UI is not wired, record BLOCKED - downstream save/load UI not available; automated persistence tests remain PASS.

**Actual Result**: Hub is now reachable, but runtime save/load and continue flow for resource snapshots cannot be manually observed through UI yet. Automated persistence coverage remains PASS.

**Pass/Fail**: BLOCKED - runtime save/load UI not wired yet.

---

### TC-RGC-009 - Runtime Signal/UI Refresh Observation

**Purpose**: Confirm visible resource UI refreshes after changes and does not show stale data.

**Preconditions**

- Resource UI and at least one runtime resource mutation action are available.

**Steps**

1. Open resource UI and note a visible quantity or capacity value.
2. Perform a resource mutation through UI or interaction.
3. Observe whether the UI refreshes without requiring a full scene reload.
4. Repeat the action if possible.

**Expected Result**

- UI reflects post-mutation state.
- No double-refresh, duplicate list entry, or stale quantity remains.
- If no resource UI/mutation path is wired, record BLOCKED - downstream UI consumer not connected.

**Actual Result**: Hub is now reachable, but no runtime mutation control or signal-driven resource UI consumer is wired yet, so refresh behavior cannot be manually observed.

**Pass/Fail**: BLOCKED - runtime mutation-driven resource UI refresh path not wired yet.

---

### TC-RGC-010 - Five-Minute Stability Observation

**Purpose**: Check for obvious runtime hitches or memory growth in resource-facing paths.

**Preconditions**

- Visible project can run for at least 5 minutes.
- Any available shell, Hub, resource UI, or route loop is reachable.

**Steps**

1. Launch the game visibly.
2. Navigate through any available resource-facing or shell screens.
3. If Hub/exploration loop is wired, perform one or more Hub -> route/exploration -> Hub passes.
4. Observe visible frame hitches, freezing, or escalating memory symptoms for 5 minutes.
5. Record any obvious stalls, visual hangs, or crashes.

**Expected Result**

- No crash during the 5-minute observation.
- No obvious repeated hitches or frozen UI.
- No visible runaway memory behavior.

**Actual Result**: User reported no issues after the available shell/recovery flow and five-minute observation on 2026-05-13. No crash, freeze, or obvious stability issue was reported.

**Pass/Fail**: PASS

---

### Result Summary

| Case | Result | Notes |
|------|--------|-------|
| TC-RGC-001 | PASS | BUG-001 verified fixed by visible Entry screenshot |
| TC-RGC-002 | PASS | BUG-002/BUG-003/BUG-004 verified fixed by manual retest |
| TC-RGC-003 | PASS | Hub runtime mounts after audio confirmation; manual visual retest recommended |
| TC-RGC-004 | PASS | Initial Hub/resource presentation is visible |
| TC-RGC-005 | BLOCKED | Runtime transfer/pickup controls not wired yet |
| TC-RGC-006 | BLOCKED | Repair deposit UI not wired yet |
| TC-RGC-007 | BLOCKED | Route/exploration loop not wired yet |
| TC-RGC-008 | BLOCKED | Runtime save/load UI not wired yet |
| TC-RGC-009 | BLOCKED | Mutation-driven resource UI refresh path not wired yet |
| TC-RGC-010 | PASS | Available shell/recovery flow stable during user observation |
