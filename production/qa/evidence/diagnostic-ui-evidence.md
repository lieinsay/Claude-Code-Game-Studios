# Diagnostic UI Evidence — Story 007

> Story: `production/epics/content-registry/story-007-diagnostic-ui.md`
> Date: 2026-05-11
> Evidence status: Implementation model verified; screenshot walkthrough pending final Godot Control binding.

## Automated Verification

Command:

```bash
dotnet run --project tests/unit/presentation/DiagnosticUITest.csproj
```

Result:

```text
Story 007 AC validation passed: 7/7 checks passed.
```

Covered checks:

- AC-1: Registry Overview, Error List, Content Item Inspector, Reference Graph, Query Tester, and Copyable Report Panel are visible, nonblank, and keyboard reachable.
- AC-2: Fatal and error diagnostics are sorted into the first viewport overview.
- AC-3: Single diagnostic copy includes the required fields and the full 16-field diagnostic block.
- AC-4: Bulk copy emits a Registry Diagnostic Summary table.
- AC-5: Reference Graph supports error-only mode and excludes unrelated clean nodes.
- AC-6: Keyboard focus reaches filters, Error List, Inspector, Reference Graph, Query Tester, and Copyable Report Panel with the ADR-0012 focus ring token.
- Debug gate: UIManager exposes diagnostic tools only when debug-build gating allows it.

## Manual Walkthrough Slots

These remain for the final Godot Control binding pass before `/story-done`:

- Screenshot: diagnostic tool opened with at least one error.
- Screenshot: Registry Overview with fatal/error visible in the first viewport.
- Screenshot: Reference Graph in all mode.
- Screenshot: Reference Graph in error-only mode.
- Screenshot: Query Tester result.
- Screenshot or short capture: keyboard-only Tab order and visible focus ring.

## Files Under Evidence

- `src/presentation/RegistryDiagnosticDevTools.cs`
- `src/presentation/UIManager.cs`
- `tests/unit/presentation/DiagnosticUITest.csproj`
- `tests/unit/presentation/DiagnosticUIProgram.cs`
