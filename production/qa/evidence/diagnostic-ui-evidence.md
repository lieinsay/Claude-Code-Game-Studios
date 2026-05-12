# Diagnostic UI Evidence — Story 007

> **Story**: Content Registry Story 007: Diagnostic UI — Dev Tools
> **Type**: UI
> **Status**: [ ] Not yet executed — awaiting live Godot session
> **Evidence Path**: `production/qa/evidence/diagnostic-ui-evidence.md`

## Setup

1. Open Godot 4.6.2 editor with this project
2. Run `SessionShell.tscn` in debug mode (F5 or editor play button)
3. Confirm `OS.IsDebugBuild()` returns true (standard editor run)
4. Press **F12** to open the Registry Diagnostic Panel

---

## AC-1: Five Panels Visible

**Requirement**: GIVEN registry has any error, WHEN diagnostic tool opened, THEN must see five panels.

**Setup**: Launch in debug mode; ensure Registry has at least one validation error (add a malformed definition to `RegistryBootstrap.cs` for testing, or rely on existing content).

**Steps**:
1. Press F12 to open panel
2. Verify the following are visible:
   - Left column: **Error List** (ItemList with error entries or "No errors" message)
   - Center top: **Registry Overview** (RichTextLabel with summary statistics)
   - Center bottom: **Content Item Inspector** (RichTextLabel showing "(Select an error to inspect)" or selected item details)
   - Right top: **Reference Graph** (RichTextLabel with node list)
   - Right bottom: **Query Tester** (LineEdit + Run button + result label)

**Edge case**: If registry has no errors, Error List shows "(No errors)" and all other panels remain visible.

**Pass condition**: All five panels visible with content or placeholder text.

**Result**: [ ] PASS  [ ] FAIL  
**Screenshot**: `[attach screenshot here]`

---

## AC-2: High Severity Issues Visible on First Screen

**Requirement**: GIVEN Registry Overview open with fatal/error diagnostics, THEN high-severity issues must be visible on first screen.

**Setup**: Ensure at least one fatal or error diagnostic exists.

**Steps**:
1. Open panel (F12)
2. Look at the Registry Overview label (center top)
3. Verify it shows `[!] Fatal issues detected` or `[!] Errors detected` message
4. Verify error/fatal counts are visible without scrolling

**Pass condition**: Severity summary visible in Overview without requiring scroll.

**Result**: [ ] PASS  [ ] FAIL

---

## AC-3: Error Detail Fields on Copy

**Requirement**: GIVEN error displayed, WHEN viewing/copying, THEN must include severity, error_code, content_id, source_ref, blocking_scope, suggested_action.

**Steps**:
1. Select an error from the Error List
2. Verify Content Item Inspector shows all required fields
3. Press "Copy Error" button
4. Paste in text editor and confirm all 16 fields are present including:
   - severity
   - error_code
   - content_id
   - source_ref
   - blocking_scope
   - suggested_action

**Pass condition**: All six required fields present in single-item copy output.

**Result**: [ ] PASS  [ ] FAIL  
**Sample output**:
```
[paste copied output here]
```

---

## AC-4: Batch Copy Table Format

**Requirement**: GIVEN multiple errors, WHEN batch copy pressed, THEN output is Registry Diagnostic Summary markdown table.

**Steps**:
1. Ensure Error List has 2+ entries
2. Press "Copy All Errors"
3. Paste in text editor
4. Verify output contains markdown table header:
   `| severity | error_code | content_id | kind | field_path | blocking_scope | suggested_action |`
5. Verify each row corresponds to a listed error

**Pass condition**: Valid markdown table with correct columns; row count matches Error List count.

**Result**: [ ] PASS  [ ] FAIL  
**Sample output**:
```
[paste copied output here]
```

---

## AC-5: Reference Graph Error-Chain-Only Mode

**Requirement**: GIVEN Reference Graph panel open with large graph, WHEN toggling error-chain-only, THEN only error nodes and their chains are shown.

**Setup**: Registry with 50+ items and at least 3 with reference errors.

**Steps**:
1. Open panel (F12)
2. Look at Reference Graph — should show all content nodes
3. Check "Error chains only" checkbox
4. Verify graph now shows only nodes with `[ERR]` prefix and their reference targets
5. Uncheck "Error chains only"
6. Verify all nodes return

**Edge case**: If no errors, error-chain-only mode shows "(No error chains — registry is clean)".

**Pass condition**: Non-error nodes absent in error-chain-only mode; restore on toggle.

**Result**: [ ] PASS  [ ] FAIL

---

## AC-6: Full Keyboard Navigation

**Requirement**: GIVEN keyboard-only navigation, THEN must reach all panels with visible focus indicator.

**Steps** (no mouse):
1. Press F12 to open panel
2. Press Tab — focus moves to Close button (visible focus ring)
3. Press Tab — focus moves to SeverityFilter dropdown
4. Press Tab — focus moves to KindFilter dropdown
5. Press Tab — focus moves to DomainFilter dropdown
6. Press Tab — focus moves to Error List (ItemList)
7. Press Arrow Down/Up — moves selection within Error List
8. Press Tab — focus moves to ErrorChainToggle (CheckButton)
9. Press Tab — focus moves to QueryInput (LineEdit)
10. Press Tab — focus moves to RunQueryButton
11. Press Tab — focus moves to CopySingleButton
12. Press Tab — focus moves to BatchCopyButton
13. Press Esc or activate Close button — panel closes

**Pass condition**: Every interactive control reachable by Tab with visible focus ring. Arrow keys work in list. No crash on empty list arrow key press.

**Result**: [ ] PASS  [ ] FAIL  
**Notes**:

---

## Sign-off

| Role | Status | Date |
|------|--------|------|
| Developer | [ ] Verified | |
| QA | [ ] Signed off | |
