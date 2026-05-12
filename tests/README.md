# Test Infrastructure

**Engine**: Godot 4.6.2 .NET
**Test Framework**: C# parity and unit runners
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-05-05

## Directory Layout

```text
tests/
  csharp/        # C# parity and migration validation runners
  unit/          # Isolated C# unit tests (formulas, state machines, logic)
  integration/   # Cross-system and save/load test plans or future C# runners
  smoke/         # Critical path test list for /smoke-check gate
  evidence/      # Screenshot logs and manual test sign-off records
```

## Running Tests

```bash
dotnet build CloudWeaverVoyage.sln
dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj
dotnet run --project tests/unit/registry/IdRegistryCoreTest.csproj
dotnet run --project tests/unit/presentation/DiagnosticUITest.csproj
dotnet run --project tests/unit/persistence/BackupFailoverTest.csproj
dotnet run --project tests/integration/persistence/ArtifactIsolationTest.csproj
```

## Test Naming

- **Files**: `[System][Feature]Test.csproj` for focused runners or grouped `Program.cs` suites
- **Functions**: `ScenarioExpectedResult`
- **Example**: `IdRegistryCoreTest.csproj` -> `Ac7ReadonlyWriteRejectedWithoutMutation()`

## Story Type -> Test Evidence

| Story Type | Required Evidence | Location |
|---|---|---|
| Logic | Automated unit test, must pass | `tests/unit/[system]/` |
| Integration | Integration test OR playtest doc | `tests/integration/[system]/` |
| Visual/Feel | Screenshot + lead sign-off | `tests/evidence/` |
| UI | Manual walkthrough OR interaction test | `tests/evidence/` |
| Config/Data | Smoke check pass | `production/qa/smoke-*.md` |

## CI

Tests run automatically on every push to `main` and on every pull request.
A failed test suite blocks merging.
