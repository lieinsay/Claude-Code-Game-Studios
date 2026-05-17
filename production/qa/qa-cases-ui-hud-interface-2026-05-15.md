# Manual QA Test Cases: UI / HUD / Chart Interface #16

**Date:** 2026-05-15
**QA Plan:** `production/qa/qa-plan-ui-hud-interface-2026-05-15.md`
**Smoke Report:** `production/qa/smoke-2026-05-15.md`
**Scope:** Epic #16 UI / HUD / Chart Interface, stories 001-006

## Execution Notes

These cases cover the runtime-facing UI/HUD validation that cannot be fully proven by C# logic tests alone. Automated coverage remains the primary guard for deterministic story acceptance. Manual execution focuses on Godot desktop visibility, focus behavior, mouse usability, and save/load discoverability.

Primary runtime scene:

```text
src/scenes/Main.tscn
```

Recommended desktop runtime:

```text
D:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe
```

Key runtime entry points:

- Entry shell: `开始航行`, `设置`, audio confirmation and muted fallback.
- Hub runtime: `打开航图 / HUD M`, `保存 Ctrl+S`, `加载 Ctrl+L`.
- Chart runtime: route buttons, `确认出发`, close / `Esc`.
- Save/load feedback: visible `保存完成` and `加载完成` status messages.

## Test Cases

| ID | Area | Steps | Expected Result | Result | Evidence |
| --- | --- | --- | --- | --- | --- |
| TC-UIHUD-001 | Project launch | Open the project in desktop Godot and run `src/scenes/Main.tscn`. | Runtime starts without crash and presents the entry shell. | PASS | User Batch 1: normal. Smoke report PASS. |
| TC-UIHUD-002 | Entry shell | Use the visible start entry to enter the runtime. Exercise the settings/audio path if available. | Start flow reaches the hub. Audio confirmation remains non-blocking when muted or unavailable. | PASS | `ShellUiTest` 18/18 PASS. Godot runtime probe PASS. |
| TC-UIHUD-003 | Hub discoverability | On the hub screen, inspect visible HUD/chart/save/load affordances. | UI/HUD is visible. Chart, save, and load entry points are discoverable without remembering keyboard shortcuts. | PASS | User Batch 2 and Batch 3 confirmed visibility. |
| TC-UIHUD-004 | Hub mouse input | Click hub action buttons with the mouse. Also verify keyboard shortcuts still work. | Mouse can activate hub entries. Keyboard shortcuts remain available when the hub is the active surface. | PASS | Initial issue fixed by shell mouse release; user later confirmed no issue. Godot probe confirms shell UI releases mouse. |
| TC-UIHUD-005 | Chart focus isolation | Open the chart from the hub, then attempt hub shortcuts and keyboard focus traversal while the chart is open. | Chart owns focus. Hub entries do not respond, steal focus, or remain selectable while the chart panel is open. | PASS | User Batch 2 confirmed bottom entries no longer steal focus. Godot probe confirms hub entries disabled and removed from focus chain. |
| TC-UIHUD-006 | Save/load feedback | Use save and load from visible runtime entries. | Save and load controls are visible. Each action produces completion feedback. | PASS | User Batch 3 confirmed `保存完成` and `加载完成`. |
| TC-UIHUD-007 | Chart route surface | Open the chart and verify route options plus departure control are visible. Select a route and inspect departure affordance. | Chart presents route choices and departure control without obscuring active UI. | PASS WITH NOTES | Runtime chart surface exists and opens correctly. Full downstream exploration loop is outside this UI bridge pass. |
| TC-UIHUD-008 | Desktop recovery | Alt-tab or minimize/restore during runtime. Repeat once with chart open if possible. | UI recovers without input lock, duplicate panels, or hidden focus. | PASS WITH NOTES | Story 006 automated recovery coverage passed. A visible desktop repeat is recommended before final gate evidence. |
| TC-UIHUD-009 | Accessibility spot check | Inspect readable labels, keyboard access, and focus behavior for entry, hub, chart, save, and load surfaces. | Critical controls have readable labels and keyboard paths. Disabled underlying controls do not remain focusable. | PASS WITH NOTES | Edge case desktop/a11y tests passed. Manual spot check focused on runtime discoverability and focus. |
| TC-UIHUD-010 | Regression sweep | Run automated build/test sweep and smoke check after manual fixes. | No automated regressions. Smoke report is PASS. | PASS | 115/115 C# test projects PASS. UI/HUD 134/134 PASS. Smoke report PASS. |

## Bugs Filed

No new QA bug files were opened during this pass.

Previously observed manual issues were fixed before sign-off:

- Hub mouse input was blocked by shell UI hit testing.
- Hub chart/save/load entries stayed keyboard-focusable while the chart was open.

## Residual Notes

- Desktop alt-tab/minimize restore has automated coverage, but the manual evidence for that exact visible flow should be repeated if the release gate requires video or screenshot proof.
- Route selection and departure controls are present in the UI runtime bridge. A later end-to-end playable journey test should validate the downstream exploration transition once that scene flow is in release scope.
