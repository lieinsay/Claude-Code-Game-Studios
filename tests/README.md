# Test Infrastructure

**Engine**: Godot 4.6.2
**Test Framework**: C# parity runners during migration; GdUnit4 legacy regression tests
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-05-05

## Directory Layout

```
tests/
  csharp/        # C# parity and migration validation runners
  unit/           # Isolated unit tests (formulas, state machines, logic)
  integration/    # Cross-system and save/load tests
  smoke/          # Critical path test list for /smoke-check gate
  evidence/       # Screenshot logs and manual test sign-off records
```

## Running Tests

```bash
# C# migration validation:
dotnet build CloudWeaverVoyage.sln
dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj

# Legacy GDScript headless (requires Godot CLI):
godot --headless --script tests/gdunit4_runner.gd

# In-editor:
# GdUnit4 panel → Run All Tests
```

## Installing GdUnit4

1. Open Godot → AssetLib → search "GdUnit4" → Download & Install
2. Enable the plugin: Project → Project Settings → Plugins → GdUnit4
3. Restart the editor
4. Verify: `res://addons/gdunit4/` exists

## Test Naming

- **Files**: `[system]_[feature]_test.gd`
- **Functions**: `test_[scenario]_[expected]`
- **Example**: `combat_damage_test.gd` → `test_base_attack_returns_expected_damage()`

## Story Type → Test Evidence

| Story Type | Required Evidence | Location |
|---|---|---|
| Logic | Automated unit test — must pass | `tests/unit/[system]/` |
| Integration | Integration test OR playtest doc | `tests/integration/[system]/` |
| Visual/Feel | Screenshot + lead sign-off | `tests/evidence/` |
| UI | Manual walkthrough OR interaction test | `tests/evidence/` |
| Config/Data | Smoke check pass | `production/qa/smoke-*.md` |

## CI

Tests run automatically on every push to `main` and on every pull request.
A failed test suite blocks merging.
