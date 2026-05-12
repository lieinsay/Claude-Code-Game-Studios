using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 006: Backup Failover — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: Main corrupt + direct backup valid -> BackupPromoted, old main Quarantined, Continue Enabled", Ac1MainCorruptBackupPromoted);
Run("AC-2: Main corrupt + backup requires migration -> BackupPreservedLocked, Continue PreservedLocked", Ac2BackupMigrationPreservedLocked);
Run("AC-2: Main corrupt + backup version incompatible -> BackupPreservedLocked", Ac2BackupVersionIncompatiblePreservedLocked);
Run("AC-3: Main corrupt + no backup -> NoUsableBackup, Continue not Enabled", Ac3NoBackupNoUsableBackup);
Run("AC-3: Main corrupt + backup integrity failure -> NoUsableBackup", Ac3InvalidBackupNoUsableBackup);
Run("AC-4: Main usable -> NotNeeded and current Safe remains Enabled", Ac4MainUsableNotNeeded);
Run("AC-5: Promotion executes required ordered steps", Ac5PromotionStepOrder);
Run("AC-5: Promotion readback failure preserves backup and keeps Continue locked", Ac5ReadbackFailurePreservesBackup);
Run("AC-6: Persistence creates independent backup after successful promotion", Ac6SavePromotionCreatesBackup);
Run("AC-6: Successful failover promotes backup to new generation and writes checkpoint summary", Ac6SuccessfulFailoverUpdatesPersistenceState);
Run("AC-7: Persistence failover failure keeps backup retained and old main Quarantined", Ac7FailedFailoverRetainsBackup);
Run("Regression: WriteLocked storage still allows restored Continue with SaveLocked barrier", RegressionWriteLockedPromotedContinueEnabled);
Run("Regression: Main migration required does not trigger backup promotion", RegressionMainMigrationRequiredDoesNotPromoteBackup);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 006 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 006 AC validation passed: {total}/{total} checks passed.");
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
// AC implementations
// ---------------------------------------------------------------------------

static bool Ac1MainCorruptBackupPromoted()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid());

    return result.Outcome == BackupFailoverOutcome.BackupPromoted
        && result.Success
        && result.OldMainState == ArtifactState.Quarantined
        && result.BackupRetained
        && result.ContinueState.Availability == ContinueAvailability.Enabled
        && result.CheckpointSummary == BackupFailoverPolicy.RecoveryCheckpointSummary;
}

static bool Ac2BackupMigrationPreservedLocked()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupMigrationRequired());

    return result.Outcome == BackupFailoverOutcome.BackupPreservedLocked
        && !result.Success
        && result.BackupRetained
        && result.ContinueState.Availability == ContinueAvailability.PreservedLocked
        && result.ContinueState.ReasonCode == ContinueStateQuery.ReasonMigrationRequired;
}

static bool Ac2BackupVersionIncompatiblePreservedLocked()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupVersionIncompatible());

    return result.Outcome == BackupFailoverOutcome.BackupPreservedLocked
        && !result.Success
        && result.ContinueState.Availability == ContinueAvailability.PreservedLocked
        && result.ContinueState.ReasonCode == ContinueStateQuery.ReasonVersionIncompatible;
}

static bool Ac3NoBackupNoUsableBackup()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupMissing());

    return result.Outcome == BackupFailoverOutcome.NoUsableBackup
        && !result.Success
        && result.ContinueState.Availability != ContinueAvailability.Enabled
        && result.OldMainState == ArtifactState.Quarantined;
}

static bool Ac3InvalidBackupNoUsableBackup()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupIntegrityFailed());

    return result.Outcome == BackupFailoverOutcome.NoUsableBackup
        && !result.Success
        && result.BackupRetained
        && result.ContinueState.Availability != ContinueAvailability.Enabled;
}

static bool Ac4MainUsableNotNeeded()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainSafe(), BackupValid());

    return result.Outcome == BackupFailoverOutcome.NotNeeded
        && result.OldMainState == ArtifactState.Safe
        && result.ContinueState.Availability == ContinueAvailability.Enabled
        && result.ContinueState.CurrentGeneration == 2;
}

static bool Ac5PromotionStepOrder()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid());
    var expected = new[]
    {
        BackupFailoverPolicy.StepValidateBackup,
        BackupFailoverPolicy.StepWritePromotedStaging,
        BackupFailoverPolicy.StepReadbackVerify,
        BackupFailoverPolicy.StepPromoteToSafe,
        BackupFailoverPolicy.StepQuarantineOriginalMain,
    };

    return result.StepOrder.SequenceEqual(expected);
}

static bool Ac5ReadbackFailurePreservesBackup()
{
    var result = BackupFailoverPolicy.ExecutePromotion(
        MainCorrupt(),
        BackupValid(),
        new BackupPromotionStepResults(ReadbackVerify: false));

    return result.Outcome == BackupFailoverOutcome.BackupPromoted
        && !result.Success
        && result.Phase == BackupPromotionPhase.Failed
        && result.StepOrder.SequenceEqual(new[]
        {
            BackupFailoverPolicy.StepValidateBackup,
            BackupFailoverPolicy.StepWritePromotedStaging,
            BackupFailoverPolicy.StepReadbackVerify,
        })
        && result.OldMainState == ArtifactState.Quarantined
        && result.BackupRetained
        && result.ContinueState.Availability != ContinueAvailability.Enabled
        && result.FailureReason == "readback_verify_failed";
}

static bool Ac6SavePromotionCreatesBackup()
{
    var persistence = MakePersistence();

    var first = persistence.RequestSaveProgress();
    var noBackupAfterFirst = !persistence.HasBackup && persistence.BackupGeneration == 0;

    var second = persistence.RequestSaveProgress();

    return first.Success
        && second.Success
        && noBackupAfterFirst
        && persistence.HasBackup
        && persistence.BackupGeneration == 1
        && persistence.CurrentGeneration == 2;
}

static bool Ac6SuccessfulFailoverUpdatesPersistenceState()
{
    var persistence = MakePersistenceWithBackup();
    int? promotedGeneration = null;
    persistence.BackupPromoted += generation => promotedGeneration = generation;

    var result = persistence.RequestBackupFailover(MainCorrupt(), BackupValid());

    return result.Success
        && persistence.CurrentGeneration == 3
        && promotedGeneration == 3
        && persistence.ProgressArtifactState == ArtifactState.Safe
        && persistence.CheckpointSummary == BackupFailoverPolicy.RecoveryCheckpointSummary
        && result.ContinueState.Availability == ContinueAvailability.Enabled
        && result.PromotedGeneration == 3;
}

static bool Ac7FailedFailoverRetainsBackup()
{
    var persistence = MakePersistenceWithBackup();

    var result = persistence.RequestBackupFailover(
        MainCorrupt(),
        BackupValid(),
        new BackupPromotionStepResults(WritePromotedStaging: false));

    return !result.Success
        && persistence.HasBackup
        && persistence.CurrentGeneration == 2
        && persistence.ProgressArtifactState == ArtifactState.Quarantined
        && result.BackupRetained
        && result.ContinueState.Availability != ContinueAvailability.Enabled;
}

static bool RegressionWriteLockedPromotedContinueEnabled()
{
    var result = BackupFailoverPolicy.ExecutePromotion(
        MainCorrupt(),
        BackupValid(),
        storageCapability: StorageCapability.WriteLocked);

    return result.Success
        && result.ContinueState.Availability == ContinueAvailability.Enabled
        && result.ContinueState.WriteBarrier == WriteBarrierMode.SaveLocked;
}

static bool RegressionMainMigrationRequiredDoesNotPromoteBackup()
{
    var result = BackupFailoverPolicy.ExecutePromotion(
        MainSafe() with { MigrationRequired = true },
        BackupValid());

    return result.Outcome == BackupFailoverOutcome.NotNeeded
        && !result.Success
        && result.OldMainState == ArtifactState.Safe
        && result.ContinueState.Availability == ContinueAvailability.PreservedLocked
        && result.ContinueState.ReasonCode == ContinueStateQuery.ReasonMigrationRequired;
}

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

static SaveArtifactProbe MainSafe() =>
    new(
        Present: true,
        ParseOk: true,
        StructureOk: true,
        IntegrityOk: true,
        VersionCompatible: true,
        StableIdsResolved: true,
        MigrationRequired: false,
        Generation: 2);

static SaveArtifactProbe MainCorrupt() =>
    new(
        Present: true,
        ParseOk: false,
        StructureOk: true,
        IntegrityOk: false,
        VersionCompatible: true,
        StableIdsResolved: true,
        MigrationRequired: false,
        Generation: 2);

static SaveArtifactProbe BackupValid() =>
    new(
        Present: true,
        ParseOk: true,
        StructureOk: true,
        IntegrityOk: true,
        VersionCompatible: true,
        StableIdsResolved: true,
        MigrationRequired: false,
        Generation: 1);

static SaveArtifactProbe BackupMigrationRequired() =>
    BackupValid() with { MigrationRequired = true };

static SaveArtifactProbe BackupVersionIncompatible() =>
    BackupValid() with { VersionCompatible = false };

static SaveArtifactProbe BackupIntegrityFailed() =>
    BackupValid() with { IntegrityOk = false };

static SaveArtifactProbe BackupMissing() =>
    new(
        Present: false,
        ParseOk: false,
        StructureOk: false,
        IntegrityOk: false,
        VersionCompatible: false,
        StableIdsResolved: false,
        MigrationRequired: false,
        Generation: 0);

static Persistence MakePersistenceWithBackup()
{
    var persistence = MakePersistence();
    persistence.RequestSaveProgress();
    persistence.RequestSaveProgress();
    return persistence;
}

static Persistence MakePersistence()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidResourcesSerializer);
    return persistence;
}

static SnapshotPackage ValidResourcesSerializer()
{
    var pkg = new SnapshotPackage
    {
        DomainId = "resources",
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Ready,
        DomainErrorCode = string.Empty,
    };
    pkg.ContentDomainVersions["resources"] = "1";
    pkg.StableIdRefs.Add("resource.supplies");
    pkg.Payload["pools"] = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["supplies"] = 5,
    };
    return pkg;
}
