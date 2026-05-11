using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 005: Version Migration — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: migration_required=true, chain_available=false → PreservedLocked (not AlreadyCurrent)", Ac1MigrationRequiredNoChainPreservesLocked);
Run("AC-2: migration_required=true, full chain succeeds → Upgraded + migration record written", Ac2FullChainUpgraded);
Run("AC-3: migration step fails mid-chain → PreservedLocked, original unmodified", Ac3MidChainFailurePreservesLocked);
Run("AC-4: migration_required=false, direct_restore_compatible=true → AlreadyCurrent", Ac4NomigrationDirectCompatibleAlreadyCurrent);
Run("AC-5: migration_required=false, direct_restore_compatible=false → PreservedLocked", Ac5NomigrationIncompatiblePreservesLocked);
Run("AC-6: parse_ok=false → Quarantined (no migration attempted)", Ac6ParseFailureQuarantined);
Run("AC-6: integrity_ok=false → Quarantined (no migration attempted)", Ac6IntegrityFailureQuarantined);
Run("AC-7: migration executes on staging copy; original payload not mutated before promotion", Ac7StagingCopyOriginalUnmodified);
Run("AC-8: successful promotion writes migration record with required fields", Ac8MigrationRecordFields);
Run("AC-9: retry_limit=1 — second migration attempt after failure is rejected → PreservedLocked", Ac9RetryLimitRejected);
Run("Regression: two-step chain [1→2, 2→3] applied in correct order", RegressionTwoStepChainOrder);
Run("Regression: EvaluateArtifact returns correct outcome without executing migration", RegressionEvaluateDoesNotMigrate);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 005 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 005 AC validation passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
    total++;
    try
    {
        if (test())
        {
            Console.WriteLine($"[PASS] {label}");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
        failed++;
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

// ---------------------------------------------------------------------------
// AC-1: migration_required=true, chain_available=false → PreservedLocked
// ---------------------------------------------------------------------------

static bool Ac1MigrationRequiredNoChainPreservesLocked()
{
    // Arrange: migration is required but no steps registered → chain not available
    var migrator = new SaveMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = false,
        DirectRestoreCompatible = false,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 2,
    };
    var payload = MakePayload(1);

    // Act
    var result = migrator.ExecuteMigration(ctx, payload);

    // Assert: must be PreservedLocked, never AlreadyCurrent
    return result.Outcome == MigrationOutcome.PreservedLocked
        && result.MigratedPayload is null
        && result.Record is null;
}

// ---------------------------------------------------------------------------
// AC-2: Full chain succeeds → Upgraded + migration record
// ---------------------------------------------------------------------------

static bool Ac2FullChainUpgraded()
{
    // Arrange: version 1 → 2 → 3 chain, all steps succeed
    var migrator = BuildTwoStepMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
        OldGeneration = 5,
        ArtifactKind = PersistenceArtifactKind.Progress,
    };
    var payload = MakePayload(1);

    // Act
    var result = migrator.ExecuteMigration(ctx, payload);

    // Assert: Upgraded, payload contains version=3, record written
    return result.Outcome == MigrationOutcome.Upgraded
        && result.MigratedPayload is not null
        && result.MigratedPayload.TryGetValue("schema_version", out var ver) && Convert.ToInt32(ver) == 3
        && result.Record is not null
        && result.Record.OldVersion == 1
        && result.Record.NewVersion == 3
        && result.Record.OldGeneration == 5
        && result.Record.NewGeneration == 6
        && migrator.MigrationHistory.Count == 1;
}

// ---------------------------------------------------------------------------
// AC-3: Mid-chain failure → PreservedLocked, original payload preserved
// ---------------------------------------------------------------------------

static bool Ac3MidChainFailurePreservesLocked()
{
    // Arrange: step 1→2 succeeds, step 2→3 throws
    var migrator = new SaveMigrator();
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 1,
        ToVersion = 2,
        MigrationFn = payload =>
        {
            var upgraded = new Dictionary<string, object?>(payload, StringComparer.Ordinal)
            {
                ["schema_version"] = 2,
            };
            return upgraded;
        }
    });
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 2,
        ToVersion = 3,
        MigrationFn = _ => throw new InvalidOperationException("simulated_migration_failure"),
    });

    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
        OldGeneration = 2,
    };
    var original = MakePayload(1);
    var originalVersion = (int)(original["schema_version"] ?? 0);

    // Act
    var result = migrator.ExecuteMigration(ctx, original);

    // Assert: PreservedLocked, original payload must still show version=1
    return result.Outcome == MigrationOutcome.PreservedLocked
        && result.MigratedPayload is null
        && original.TryGetValue("schema_version", out var storedVer)
        && Convert.ToInt32(storedVer) == originalVersion  // unchanged
        && migrator.MigrationHistory.Count == 0;
}

// ---------------------------------------------------------------------------
// AC-4: No migration required + directly compatible → AlreadyCurrent
// ---------------------------------------------------------------------------

static bool Ac4NomigrationDirectCompatibleAlreadyCurrent()
{
    // Arrange
    var migrator = new SaveMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = false,
        DirectRestoreCompatible = true,
        ArtifactSchemaVersion = 3,
        TargetSchemaVersion = 3,
    };
    var payload = MakePayload(3);

    // Act
    var result = migrator.ExecuteMigration(ctx, payload);

    // Assert
    return result.Outcome == MigrationOutcome.AlreadyCurrent
        && result.MigratedPayload is null;
}

// ---------------------------------------------------------------------------
// AC-5: No migration required but NOT directly compatible → PreservedLocked
// ---------------------------------------------------------------------------

static bool Ac5NomigrationIncompatiblePreservesLocked()
{
    // Arrange: migration not required (schema same), but content IDs changed →
    // direct_restore_compatible = false
    var migrator = new SaveMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = false,
        DirectRestoreCompatible = false,
        ArtifactSchemaVersion = 3,
        TargetSchemaVersion = 3,
    };
    var payload = MakePayload(3);

    // Act
    var result = migrator.ExecuteMigration(ctx, payload);

    // Assert: must be PreservedLocked, never AlreadyCurrent
    return result.Outcome == MigrationOutcome.PreservedLocked
        && result.MigratedPayload is null;
}

// ---------------------------------------------------------------------------
// AC-6: parse_ok=false → Quarantined
// ---------------------------------------------------------------------------

static bool Ac6ParseFailureQuarantined()
{
    var migrator = BuildTwoStepMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = false, // corrupt
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
    };
    var payload = MakePayload(1);

    var result = migrator.ExecuteMigration(ctx, payload);

    return result.Outcome == MigrationOutcome.Quarantined
        && result.MigratedPayload is null
        && migrator.MigrationHistory.Count == 0;
}

static bool Ac6IntegrityFailureQuarantined()
{
    var migrator = BuildTwoStepMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = false, // checksum mismatch
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
    };
    var payload = MakePayload(1);

    var result = migrator.ExecuteMigration(ctx, payload);

    return result.Outcome == MigrationOutcome.Quarantined
        && result.MigratedPayload is null;
}

// ---------------------------------------------------------------------------
// AC-7: Migration executes on staging copy; original payload unmodified
// ---------------------------------------------------------------------------

static bool Ac7StagingCopyOriginalUnmodified()
{
    // Arrange: a migration step that would mutate the dictionary in-place if
    // not properly staged. We verify the original reference is unchanged after
    // a successful migration.
    var migrator = new SaveMigrator();
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 1,
        ToVersion = 2,
        MigrationFn = payload =>
        {
            // Return a new dictionary — this is correct migration practice.
            // The test verifies the original was not mutated even if the fn
            // modified its argument.
            var upgraded = new Dictionary<string, object?>(payload, StringComparer.Ordinal)
            {
                ["schema_version"] = 2,
                ["added_field"] = "migration_marker",
            };
            return upgraded;
        }
    });

    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 2,
        OldGeneration = 0,
    };
    var original = MakePayload(1);
    var originalKeyCount = original.Count;

    // Act
    var result = migrator.ExecuteMigration(ctx, original);

    // Assert: promoted payload has the new field; original snapshot count is the same
    return result.Outcome == MigrationOutcome.Upgraded
        && result.MigratedPayload is not null
        && result.MigratedPayload.ContainsKey("added_field")
        && original.Count == originalKeyCount  // original not modified structurally
        && !original.ContainsKey("added_field"); // new field must not appear in original
}

// ---------------------------------------------------------------------------
// AC-8: Migration record contains all required fields
// ---------------------------------------------------------------------------

static bool Ac8MigrationRecordFields()
{
    // Arrange
    var migrator = BuildTwoStepMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
        OldGeneration = 10,
        ArtifactKind = PersistenceArtifactKind.Progress,
    };
    var payload = MakePayload(1);

    // Act
    var result = migrator.ExecuteMigration(ctx, payload);

    // Assert: record present with correct shape
    if (result.Record is null)
    {
        return false;
    }

    var rec = result.Record;
    return rec.ArtifactKind == PersistenceArtifactKind.Progress
        && rec.OldGeneration == 10
        && rec.NewGeneration == 11
        && rec.OldVersion == 1
        && rec.NewVersion == 3
        && rec.ChainVersions.Count == 3      // ["1", "2", "3"]
        && rec.ChainVersions[0] == "1"
        && rec.ChainVersions[1] == "2"
        && rec.ChainVersions[2] == "3"
        && rec.StepDurationsMs.Count == 2    // one entry per step
        && rec.Outcome == MigrationOutcome.Upgraded
        && rec.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1);
}

// ---------------------------------------------------------------------------
// AC-9: retry_limit=1 — second attempt after failure is rejected
// ---------------------------------------------------------------------------

static bool Ac9RetryLimitRejected()
{
    // Arrange: step 1→2 throws, so first attempt fails
    var migrator = new SaveMigrator();
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 1,
        ToVersion = 2,
        MigrationFn = _ => throw new InvalidOperationException("forced_fail"),
    });

    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 2,
    };
    var payload = MakePayload(1);

    // Act: first attempt — must fail
    var first = migrator.ExecuteMigration(ctx, payload);

    // Replace the failing step with a working one — but the retry lock must
    // prevent the second attempt from executing the chain.
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 1,
        ToVersion = 2,
        MigrationFn = p =>
        {
            var upgraded = new Dictionary<string, object?>(p, StringComparer.Ordinal)
            {
                ["schema_version"] = 2,
            };
            return upgraded;
        }
    });

    // Act: second attempt with same artifact version
    var second = migrator.ExecuteMigration(ctx, payload);

    // Assert: both must be PreservedLocked; no history written
    return first.Outcome == MigrationOutcome.PreservedLocked
        && second.Outcome == MigrationOutcome.PreservedLocked
        && migrator.MigrationHistory.Count == 0;
}

// ---------------------------------------------------------------------------
// Regression: two-step chain applied in correct order
// ---------------------------------------------------------------------------

static bool RegressionTwoStepChainOrder()
{
    // If steps are applied in wrong order (2→3 before 1→2), the chain breaks.
    // Register in reverse order to ensure BuildChain sorts correctly.
    var migrator = new SaveMigrator();
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 2,
        ToVersion = 3,
        MigrationFn = payload =>
        {
            // Expects "v2_field" to exist (added by step 1→2).
            if (!payload.ContainsKey("v2_field"))
            {
                throw new InvalidOperationException("v2_field_missing — steps applied out of order");
            }

            var upgraded = new Dictionary<string, object?>(payload, StringComparer.Ordinal)
            {
                ["schema_version"] = 3,
                ["v3_field"] = true,
            };
            return upgraded;
        }
    });
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 1,
        ToVersion = 2,
        MigrationFn = payload =>
        {
            var upgraded = new Dictionary<string, object?>(payload, StringComparer.Ordinal)
            {
                ["schema_version"] = 2,
                ["v2_field"] = "added_by_step_1to2",
            };
            return upgraded;
        }
    });

    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
        OldGeneration = 0,
    };
    var payload = MakePayload(1);

    var result = migrator.ExecuteMigration(ctx, payload);

    return result.Outcome == MigrationOutcome.Upgraded
        && result.MigratedPayload is not null
        && result.MigratedPayload.ContainsKey("v2_field")
        && result.MigratedPayload.ContainsKey("v3_field");
}

// ---------------------------------------------------------------------------
// Regression: EvaluateArtifact does not execute migration
// ---------------------------------------------------------------------------

static bool RegressionEvaluateDoesNotMigrate()
{
    var migrator = BuildTwoStepMigrator();
    var ctx = new ArtifactContext
    {
        ParseOk = true,
        IntegrityOk = true,
        MigrationRequired = true,
        MigrationChainAvailable = true,
        ArtifactSchemaVersion = 1,
        TargetSchemaVersion = 3,
    };

    // Act: evaluate only
    var outcome = migrator.EvaluateArtifact(ctx);

    // Assert: outcome is PreservedLocked (migration_executed=false in ComputeOutcome)
    // and no history was written
    return outcome == MigrationOutcome.PreservedLocked
        && migrator.MigrationHistory.Count == 0;
}

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

static Dictionary<string, object?> MakePayload(int version)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["schema_version"] = version,
        ["data"] = "payload_data",
    };
}

static SaveMigrator BuildTwoStepMigrator()
{
    var migrator = new SaveMigrator();
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 1,
        ToVersion = 2,
        MigrationFn = payload =>
        {
            var upgraded = new Dictionary<string, object?>(payload, StringComparer.Ordinal)
            {
                ["schema_version"] = 2,
            };
            return upgraded;
        }
    });
    migrator.RegisterMigrationStep(new MigrationStep
    {
        FromVersion = 2,
        ToVersion = 3,
        MigrationFn = payload =>
        {
            var upgraded = new Dictionary<string, object?>(payload, StringComparer.Ordinal)
            {
                ["schema_version"] = 3,
            };
            return upgraded;
        }
    });
    return migrator;
}
