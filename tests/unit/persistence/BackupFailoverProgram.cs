using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 006: Backup Failover — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: Main corrupt + backup valid → BackupPromoted, old main quarantined", Ac1MainCorruptBackupValid);
Run("AC-1: After promotion, generation advances and checkpoint_summary set", Ac1PromotionCheckpointSummary);
Run("AC-2: Main corrupt + backup needs migration → BackupPreservedLocked", Ac2BackupNeedsMigration);
Run("AC-2 edge: version incompatible only (no migration flag) → BackupPreservedLocked", Ac2VersionIncompatibleNoMigration);
Run("AC-3: Main corrupt + no backup → NoUsableBackup", Ac3NoBackup);
Run("AC-3: Main corrupt + backup integrity fail → NoUsableBackup", Ac3BackupIntegrityFail);
Run("AC-4: Main usable → NotNeeded", Ac4MainUsable);
Run("AC-5: Promotion step failure → backup preserved, old main stays quarantined", Ac5PromotionStepFailure);
Run("AC-5: Mid-sequence failure → mainQuarantined and backup retained", Ac5MidSequenceFailure);
Run("AC-6: Backup promotion success → ContinueState Enabled, checkpoint_summary set", Ac6PromotionSuccess);
Run("AC-7: Promotion fails → backup retained, Continue not Enabled", Ac7PromotionFails);
Run("Regression: EvaluateFailover does not modify external state", RegressionEvaluateIsPure);
Run("Regression: step names match ADR-defined sequence", RegressionStepNamesMatchSpec);

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
// Fixtures
// ---------------------------------------------------------------------------

static SaveArtifactProbe MainCorrupt() => new(
    Present: true,
    ParseOk: false,
    StructureOk: false,
    IntegrityOk: false,
    VersionCompatible: false,
    StableIdsResolved: false,
    MigrationRequired: false);

static SaveArtifactProbe MainUsableProbe() => new(
    Present: true,
    ParseOk: true,
    StructureOk: true,
    IntegrityOk: true,
    VersionCompatible: true,
    StableIdsResolved: true,
    MigrationRequired: false,
    Generation: 2);

static SaveArtifactProbe BackupValid() => new(
    Present: true,
    ParseOk: true,
    StructureOk: true,
    IntegrityOk: true,
    VersionCompatible: true,
    StableIdsResolved: true,
    MigrationRequired: false,
    Generation: 1);

static SaveArtifactProbe BackupNeedsMigration() => new(
    Present: true,
    ParseOk: true,
    StructureOk: true,
    IntegrityOk: true,
    VersionCompatible: true,
    StableIdsResolved: true,
    MigrationRequired: true);

static SaveArtifactProbe BackupVersionIncompatible() => new(
    Present: true,
    ParseOk: true,
    StructureOk: true,
    IntegrityOk: true,
    VersionCompatible: false,
    StableIdsResolved: true,
    MigrationRequired: false);

static SaveArtifactProbe BackupAbsent() => new(
    Present: false,
    ParseOk: false,
    StructureOk: false,
    IntegrityOk: false,
    VersionCompatible: false,
    StableIdsResolved: false,
    MigrationRequired: false);

static SaveArtifactProbe BackupIntegrityFail() => new(
    Present: true,
    ParseOk: true,
    StructureOk: false,
    IntegrityOk: false,
    VersionCompatible: true,
    StableIdsResolved: true,
    MigrationRequired: false);

// ---------------------------------------------------------------------------
// AC-1: Main corrupt, backup valid → BackupPromoted, old main Quarantined
// ---------------------------------------------------------------------------

static bool Ac1MainCorruptBackupValid()
{
    var outcome = BackupFailoverPolicy.EvaluateFailover(MainCorrupt(), BackupValid());
    if (outcome != BackupFailoverOutcome.BackupPromoted)
    {
        return false;
    }

    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid());

    return result.Outcome == BackupFailoverOutcome.BackupPromoted
        && result.Success
        && result.OldMainState == ArtifactState.Quarantined
        && result.BackupRetained;
}

// ---------------------------------------------------------------------------
// AC-1 supplement: checkpoint_summary is set on successful promotion
// ---------------------------------------------------------------------------

static bool Ac1PromotionCheckpointSummary()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid());

    return result.Success
        && result.CheckpointSummary == BackupFailoverPolicy.RecoveryCheckpointSummary
        && result.PromotedGeneration > 0;
}

// ---------------------------------------------------------------------------
// AC-2: backup needs migration → BackupPreservedLocked
// ---------------------------------------------------------------------------

static bool Ac2BackupNeedsMigration()
{
    var outcome = BackupFailoverPolicy.EvaluateFailover(MainCorrupt(), BackupNeedsMigration());
    return outcome == BackupFailoverOutcome.BackupPreservedLocked;
}

// ---------------------------------------------------------------------------
// AC-2 edge: version incompatible only → BackupPreservedLocked
// ---------------------------------------------------------------------------

static bool Ac2VersionIncompatibleNoMigration()
{
    var outcome = BackupFailoverPolicy.EvaluateFailover(MainCorrupt(), BackupVersionIncompatible());
    return outcome == BackupFailoverOutcome.BackupPreservedLocked;
}

// ---------------------------------------------------------------------------
// AC-3a: no backup → NoUsableBackup
// ---------------------------------------------------------------------------

static bool Ac3NoBackup()
{
    var outcome = BackupFailoverPolicy.EvaluateFailover(MainCorrupt(), BackupAbsent());
    return outcome == BackupFailoverOutcome.NoUsableBackup;
}

// ---------------------------------------------------------------------------
// AC-3b: backup integrity fail → NoUsableBackup
// ---------------------------------------------------------------------------

static bool Ac3BackupIntegrityFail()
{
    var outcome = BackupFailoverPolicy.EvaluateFailover(MainCorrupt(), BackupIntegrityFail());
    return outcome == BackupFailoverOutcome.NoUsableBackup;
}

// ---------------------------------------------------------------------------
// AC-4: main usable → NotNeeded
// ---------------------------------------------------------------------------

static bool Ac4MainUsable()
{
    var outcome = BackupFailoverPolicy.EvaluateFailover(MainUsableProbe(), BackupValid());
    return outcome == BackupFailoverOutcome.NotNeeded;
}

// ---------------------------------------------------------------------------
// AC-5: promotion step failure → backup preserved, old main quarantined
// ---------------------------------------------------------------------------

static bool Ac5PromotionStepFailure()
{
    // Fail the promote-to-safe step — backup must be retained, main quarantined
    var failSteps = new BackupPromotionStepResults(PromoteToSafe: false);
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid(), failSteps);

    return !result.Success
        && result.BackupRetained
        && result.OldMainState == ArtifactState.Quarantined;
}

// ---------------------------------------------------------------------------
// AC-5 supplement: mid-sequence failure (readback verify) → same invariants
// ---------------------------------------------------------------------------

static bool Ac5MidSequenceFailure()
{
    var failSteps = new BackupPromotionStepResults(ReadbackVerify: false);
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid(), failSteps);

    return !result.Success
        && result.BackupRetained
        && result.OldMainState == ArtifactState.Quarantined;
}

// ---------------------------------------------------------------------------
// AC-6: promotion success → ContinueState.Enabled, checkpoint_summary set
// ---------------------------------------------------------------------------

static bool Ac6PromotionSuccess()
{
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid());

    return result.Success
        && result.Outcome == BackupFailoverOutcome.BackupPromoted
        && result.ContinueState.Availability == ContinueAvailability.Enabled
        && result.CheckpointSummary == BackupFailoverPolicy.RecoveryCheckpointSummary;
}

// ---------------------------------------------------------------------------
// AC-7: promotion fails → backup preserved, Continue not Enabled
// ---------------------------------------------------------------------------

static bool Ac7PromotionFails()
{
    var failSteps = new BackupPromotionStepResults(ValidateBackup: false);
    var result = BackupFailoverPolicy.ExecutePromotion(MainCorrupt(), BackupValid(), failSteps);

    return !result.Success
        && result.BackupRetained
        && result.ContinueState.Availability != ContinueAvailability.Enabled;
}

// ---------------------------------------------------------------------------
// Regression: EvaluateFailover is a pure function — call twice, same result
// ---------------------------------------------------------------------------

static bool RegressionEvaluateIsPure()
{
    var main = MainCorrupt();
    var backup = BackupValid();

    var r1 = BackupFailoverPolicy.EvaluateFailover(main, backup);
    var r2 = BackupFailoverPolicy.EvaluateFailover(main, backup);

    return r1 == r2 && r1 == BackupFailoverOutcome.BackupPromoted;
}

// ---------------------------------------------------------------------------
// Regression: step names match the ADR-0003 defined sequence labels
// ---------------------------------------------------------------------------

static bool RegressionStepNamesMatchSpec()
{
    return BackupFailoverPolicy.StepValidateBackup == "validate_backup"
        && BackupFailoverPolicy.StepWritePromotedStaging == "copy_to_staging_as_new_generation"
        && BackupFailoverPolicy.StepReadbackVerify == "readback_verify"
        && BackupFailoverPolicy.StepPromoteToSafe == "promote_to_safe"
        && BackupFailoverPolicy.StepQuarantineOriginalMain == "quarantine_original_main";
}
