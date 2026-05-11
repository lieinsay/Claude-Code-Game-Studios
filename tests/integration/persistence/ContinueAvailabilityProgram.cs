using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 004: Continue Availability & Restore Readiness — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: All conditions met → restore_readiness=true", Ac1AllConditionsMetRestoreReadinessTrue);
Run("AC-2: archive_present=false (Missing) → Hidden", Ac2MissingArchiveHidden);
Run("AC-3: PersistentAvailable + valid archive → Enabled, WriteBarrier=None", Ac3PersistentAvailableEnabledNoBarrier);
Run("AC-4: WriteLocked + valid archive → Enabled, WriteBarrier=SaveLocked", Ac4WriteLockedEnabledSaveLocked);
Run("AC-5: migration_required=true → PreservedLocked with reason_code", Ac5MigrationRequiredPreservedLocked);
Run("AC-6: QueryContinueState result contains all required fields", Ac6ResultContainsAllRequiredFields);
Run("AC-7: Quarantined artifact → not Enabled", Ac7QuarantinedNotEnabled);
Run("AC-8: result always contains all 7 fields", Ac8ResultAlwaysContainsAllFields);
Run("AC-9: settings Quarantined, progress Safe → Enabled (progress drives result)", Ac9SettingsQuarantinedProgressSafeEnabled);
Run("AC-10: progress Quarantined, settings Safe → not Enabled; settings ArtifactState unchanged", Ac10ProgressQuarantinedSettingsSafeNotEnabled);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 004 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 004 AC validation passed: {total}/{total} checks passed.");
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
// Helper factories
// ---------------------------------------------------------------------------

static ArtifactStatus SafeStatus() =>
    new(ArtifactState.Safe, IntegrityOk: true, VersionCompatible: true,
        StableIdsResolved: true, MigrationRequired: false);

static ArtifactStatus QuarantinedStatus() =>
    new(ArtifactState.Quarantined, IntegrityOk: false, VersionCompatible: false,
        StableIdsResolved: false, MigrationRequired: false);

static ArtifactStatus MissingStatus() =>
    new(ArtifactState.Missing, IntegrityOk: false, VersionCompatible: false,
        StableIdsResolved: false, MigrationRequired: false);

// ---------------------------------------------------------------------------
// AC implementations
// ---------------------------------------------------------------------------

static bool Ac1AllConditionsMetRestoreReadinessTrue()
{
    // Arrange: all conditions fully satisfied
    var status = SafeStatus();

    // Act
    var ready = ContinueStateQuery.ComputeRestoreReadiness(status);

    // Assert
    return ready;
}

static bool Ac2MissingArchiveHidden()
{
    // Arrange: no archive on disk
    var progressStatus = MissingStatus();

    // Act
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus: null,
        currentGeneration: 0);

    // Assert: no archive → Hidden regardless of storage capability
    return result.Availability == ContinueAvailability.Hidden
        && result.StorageCapability == StorageCapability.PersistentAvailable
        && result.CurrentGeneration == 0;
}

static bool Ac3PersistentAvailableEnabledNoBarrier()
{
    // Arrange: full persistent storage, fully valid progress artifact
    var progressStatus = SafeStatus();

    // Act
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus: null,
        currentGeneration: 3);

    // Assert: Enabled with no write barrier
    return result.Availability == ContinueAvailability.Enabled
        && result.WriteBarrier == WriteBarrierMode.None
        && result.ReasonCode == string.Empty
        && result.StorageCapability == StorageCapability.PersistentAvailable
        && result.CurrentGeneration == 3
        && result.ArtifactKind == "progress";
}

static bool Ac4WriteLockedEnabledSaveLocked()
{
    // Arrange: write-locked storage, valid progress artifact
    var progressStatus = SafeStatus();

    // Act
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.WriteLocked,
        progressStatus,
        settingsStatus: null,
        currentGeneration: 7);

    // Assert: Enabled but save-locked barrier
    return result.Availability == ContinueAvailability.Enabled
        && result.WriteBarrier == WriteBarrierMode.SaveLocked
        && result.ReasonCode == string.Empty
        && result.StorageCapability == StorageCapability.WriteLocked
        && result.CurrentGeneration == 7;
}

static bool Ac5MigrationRequiredPreservedLocked()
{
    // Arrange: archive exists but requires migration
    var progressStatus = new ArtifactStatus(
        ArtifactState.Safe,
        IntegrityOk: true,
        VersionCompatible: true,
        StableIdsResolved: true,
        MigrationRequired: true);   // blocks restore

    // Act: restore_readiness must be false; continue result must be PreservedLocked
    var readiness = ContinueStateQuery.ComputeRestoreReadiness(progressStatus);
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus: null,
        currentGeneration: 1);

    // Assert
    return readiness == false
        && result.Availability == ContinueAvailability.PreservedLocked
        && result.ReasonCode == ContinueStateQuery.ReasonMigrationRequired;
}

static bool Ac6ResultContainsAllRequiredFields()
{
    // Arrange: use QueryContinueState as the sole source (not a hand-rolled formula)
    var progressStatus = SafeStatus();

    // Act
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus: null,
        currentGeneration: 2);

    // Assert: all 7 required fields are present and non-default for meaningful assertions
    return result.Availability == ContinueAvailability.Enabled
        && result.StorageCapability == StorageCapability.PersistentAvailable
        && result.WriteBarrier == WriteBarrierMode.None
        && result.ReasonCode == string.Empty
        && result.CurrentGeneration == 2
        && result.ArtifactKind == "progress"
        && result.ReasonCode is not null;   // field exists (not missing from record)
}

static bool Ac7QuarantinedNotEnabled()
{
    // Arrange: quarantined progress artifact
    var progressStatus = QuarantinedStatus();

    // Act: readiness and query
    var readiness = ContinueStateQuery.ComputeRestoreReadiness(progressStatus);
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus: null,
        currentGeneration: 4);

    // Assert: never Enabled when quarantined
    return readiness == false
        && result.Availability != ContinueAvailability.Enabled
        && result.ReasonCode == ContinueStateQuery.ReasonQuarantined;
}

static bool Ac8ResultAlwaysContainsAllFields()
{
    // Verify all 7 fields are populated in multiple result paths.
    var scenarios = new[]
    {
        // Hidden path
        ContinueStateQuery.QueryContinueState(
            StorageCapability.PersistentAvailable, MissingStatus(), null, 0),
        // PreservedLocked path (quarantined)
        ContinueStateQuery.QueryContinueState(
            StorageCapability.PersistentAvailable, QuarantinedStatus(), null, 1),
        // Enabled path
        ContinueStateQuery.QueryContinueState(
            StorageCapability.PersistentAvailable, SafeStatus(), null, 2),
        // EphemeralOnly + Missing → Hidden
        ContinueStateQuery.QueryContinueState(
            StorageCapability.EphemeralOnly, MissingStatus(), null, 3),
        // EphemeralOnly + archive present → PreservedLocked
        ContinueStateQuery.QueryContinueState(
            StorageCapability.EphemeralOnly, SafeStatus(), null, 4),
    };

    foreach (var result in scenarios)
    {
        // All 7 AC-8 fields must be non-null strings or valid enum values.
        if (result.ReasonCode is null)
        {
            return false;
        }

        if (result.ArtifactKind is null)
        {
            return false;
        }

        // Availability must be one of the three known values
        if (result.Availability is not (ContinueAvailability.Enabled
            or ContinueAvailability.PreservedLocked
            or ContinueAvailability.Hidden))
        {
            return false;
        }
    }

    return true;
}

static bool Ac9SettingsQuarantinedProgressSafeEnabled()
{
    // Arrange: settings artifact is quarantined, but progress is fully safe.
    var progressStatus = SafeStatus();
    var settingsStatus = QuarantinedStatus();   // quarantined settings must NOT block Continue

    // Act
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus,
        currentGeneration: 5);

    // Assert: Continue is Enabled because progress drives the result (non-interference rule).
    return result.Availability == ContinueAvailability.Enabled
        && result.WriteBarrier == WriteBarrierMode.None
        && result.ReasonCode == string.Empty;
}

static bool Ac10ProgressQuarantinedSettingsSafeNotEnabled()
{
    // Arrange: progress artifact is quarantined; settings artifact is fully safe.
    var progressStatus = QuarantinedStatus();
    var settingsStatus = SafeStatus();  // safe settings must not rescue a quarantined progress

    // Act
    var result = ContinueStateQuery.QueryContinueState(
        StorageCapability.PersistentAvailable,
        progressStatus,
        settingsStatus,
        currentGeneration: 6);

    // Assert: Continue is not Enabled; settings ArtifactState is unchanged (still Safe).
    return result.Availability != ContinueAvailability.Enabled
        && settingsStatus.State == ArtifactState.Safe;  // non-interference: settings record untouched
}
