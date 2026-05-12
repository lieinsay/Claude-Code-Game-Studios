using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 007: Artifact Isolation — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: Settings-only save restores new settings while progress stays at last verified generation", Ac1SettingsOnlySaveDoesNotTouchProgress);
Run("AC-2: Progress-only save restores new progress while settings stays at last verified generation", Ac2ProgressOnlySaveDoesNotTouchSettings);
Run("AC-3: Settings save failure does not block progress promotion or overwrite old settings", Ac3SettingsFailureProgressSucceeds);
Run("AC-4: Progress save failure does not block settings promotion or overwrite old progress", Ac4ProgressFailureSettingsSucceeds);
Run("AC-5: Settings Quarantined + progress Safe keeps Continue Enabled", Ac5SettingsQuarantinedDoesNotAffectContinue);
Run("AC-6: Progress Quarantined + settings Safe blocks Continue without deleting settings", Ac6ProgressQuarantinedBlocksContinueSettingsPreserved);
Run("AC-7: Durable metadata uses artifact-kind prefixes and independent generations", Ac7DurableMetadataPrefixedAndIndependent);
Run("AC-8: Progress storage capability controls formal progress save when settings can write", Ac8ProgressCapabilityBlocksProgressOnly);
Run("AC-8: Settings rollback does not block progress save when progress can write", Ac8SettingsCapabilityDoesNotBlockProgress);
Run("Regression: Settings promotion does not emit progress SaveCompleted", RegressionSettingsPromotionDoesNotEmitProgressSaveCompleted);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 007 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 007 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1SettingsOnlySaveDoesNotTouchProgress()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.SettingsValue = "settings-new";
    var settingsSave = fixture.Persistence.RequestSaveSettings();

    fixture.ClearRestored();
    fixture.Persistence.RequestLoadSettings();
    fixture.Persistence.RequestLoadProgress();

    var progressMeta = fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Progress);
    var settingsMeta = fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Settings);

    return settingsSave.Success
        && fixture.RestoredSettings == "settings-new"
        && fixture.RestoredProgress == "progress-initial"
        && progressMeta.CurrentGeneration == 1
        && settingsMeta.CurrentGeneration == 2;
}

static bool Ac2ProgressOnlySaveDoesNotTouchSettings()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.ProgressValue = "progress-new";
    var progressSave = fixture.Persistence.RequestSaveProgress();

    fixture.ClearRestored();
    fixture.Persistence.RequestLoadProgress();
    fixture.Persistence.RequestLoadSettings();

    var progressMeta = fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Progress);
    var settingsMeta = fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Settings);

    return progressSave.Success
        && fixture.RestoredProgress == "progress-new"
        && fixture.RestoredSettings == "settings-initial"
        && progressMeta.CurrentGeneration == 2
        && settingsMeta.CurrentGeneration == 1;
}

static bool Ac3SettingsFailureProgressSucceeds()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.SettingsValue = "settings-new";
    fixture.ProgressValue = "progress-new";
    fixture.BlockSettings = true;

    var settingsSave = fixture.Persistence.RequestSaveSettings();
    var progressSave = fixture.Persistence.RequestSaveProgress();

    fixture.BlockSettings = false;
    fixture.ClearRestored();
    fixture.Persistence.RequestLoadSettings();
    fixture.Persistence.RequestLoadProgress();

    return !settingsSave.Success
        && progressSave.Success
        && fixture.RestoredSettings == "settings-initial"
        && fixture.RestoredProgress == "progress-new";
}

static bool Ac4ProgressFailureSettingsSucceeds()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.SettingsValue = "settings-new";
    fixture.ProgressValue = "progress-new";
    fixture.BlockProgress = true;

    var progressSave = fixture.Persistence.RequestSaveProgress();
    var settingsSave = fixture.Persistence.RequestSaveSettings();

    fixture.BlockProgress = false;
    fixture.ClearRestored();
    fixture.Persistence.RequestLoadProgress();
    fixture.Persistence.RequestLoadSettings();

    return !progressSave.Success
        && settingsSave.Success
        && fixture.RestoredProgress == "progress-initial"
        && fixture.RestoredSettings == "settings-new";
}

static bool Ac5SettingsQuarantinedDoesNotAffectContinue()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.Persistence.SetArtifactRecoveryStatus(
        PersistenceArtifactKind.Settings,
        new ArtifactStatus(ArtifactState.Quarantined, false, true, true, false),
        "settings_corrupt");

    var result = fixture.Persistence.QueryContinueState();
    var settingsMeta = fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Settings);

    return result.Availability == ContinueAvailability.Enabled
        && result.ArtifactKind == "progress"
        && settingsMeta.State == ArtifactState.Quarantined
        && settingsMeta.CurrentGeneration == 1;
}

static bool Ac6ProgressQuarantinedBlocksContinueSettingsPreserved()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.Persistence.SetArtifactRecoveryStatus(
        PersistenceArtifactKind.Progress,
        new ArtifactStatus(ArtifactState.Quarantined, false, true, true, false),
        "progress_corrupt");

    var result = fixture.Persistence.QueryContinueState();
    fixture.ClearRestored();
    fixture.Persistence.RequestLoadSettings();

    return result.Availability != ContinueAvailability.Enabled
        && fixture.RestoredSettings == "settings-initial"
        && fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Settings).State == ArtifactState.Safe;
}

static bool Ac7DurableMetadataPrefixedAndIndependent()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();
    fixture.ProgressValue = "progress-new";
    fixture.Persistence.RequestSaveProgress();

    var metadata = fixture.Persistence.ExportDurableMetadata();

    return metadata.ContainsKey("progress.current_generation")
        && metadata.ContainsKey("settings.current_generation")
        && metadata.ContainsKey("progress.manifest_pointer")
        && metadata.ContainsKey("settings.manifest_pointer")
        && metadata.ContainsKey("progress.last_verified_checkpoint")
        && metadata.ContainsKey("settings.last_verified_checkpoint")
        && metadata.ContainsKey("progress.checkpoint_summary")
        && metadata.ContainsKey("settings.checkpoint_summary")
        && metadata.ContainsKey("progress.reason_code")
        && metadata.ContainsKey("settings.reason_code")
        && metadata.ContainsKey("progress.backup_generation")
        && metadata.ContainsKey("settings.backup_generation")
        && (int)metadata["progress.current_generation"]! == 2
        && (int)metadata["settings.current_generation"]! == 1;
}

static bool Ac8ProgressCapabilityBlocksProgressOnly()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.Persistence.SetArtifactStorageCapability(
        PersistenceArtifactKind.Settings,
        StorageCapability.PersistentAvailable);
    fixture.Persistence.SetArtifactStorageCapability(
        PersistenceArtifactKind.Progress,
        StorageCapability.WriteLocked);

    fixture.SettingsValue = "settings-new";
    fixture.ProgressValue = "progress-new";

    var settingsSave = fixture.Persistence.RequestSaveSettings();
    var progressSave = fixture.Persistence.RequestSaveProgress();
    var continueState = fixture.Persistence.QueryContinueState();

    return settingsSave.Success
        && !progressSave.Success
        && continueState.StorageCapability == StorageCapability.WriteLocked
        && continueState.WriteBarrier == WriteBarrierMode.SaveLocked
        && fixture.Persistence.GetArtifactMetadata(PersistenceArtifactKind.Progress).ReasonCode == "storage_not_writable";
}

static bool Ac8SettingsCapabilityDoesNotBlockProgress()
{
    var fixture = ArtifactFixture.Create();
    fixture.SaveBoth();

    fixture.Persistence.SetArtifactStorageCapability(
        PersistenceArtifactKind.Settings,
        StorageCapability.WriteLocked);
    fixture.Persistence.SetArtifactStorageCapability(
        PersistenceArtifactKind.Progress,
        StorageCapability.PersistentAvailable);

    fixture.SettingsValue = "settings-new";
    fixture.ProgressValue = "progress-new";

    var settingsSave = fixture.Persistence.RequestSaveSettings();
    var progressSave = fixture.Persistence.RequestSaveProgress();
    var continueState = fixture.Persistence.QueryContinueState();

    fixture.ClearRestored();
    fixture.Persistence.RequestLoadSettings();
    fixture.Persistence.RequestLoadProgress();

    return !settingsSave.Success
        && progressSave.Success
        && continueState.Availability == ContinueAvailability.Enabled
        && fixture.RestoredSettings == "settings-initial"
        && fixture.RestoredProgress == "progress-new";
}

static bool RegressionSettingsPromotionDoesNotEmitProgressSaveCompleted()
{
    var fixture = ArtifactFixture.Create();
    var saveCompletedCount = 0;
    fixture.Persistence.SaveCompleted += _ => saveCompletedCount++;

    var progressSave = fixture.Persistence.RequestSaveProgress();
    var settingsSave = fixture.Persistence.RequestSaveSettings();

    return progressSave.Success
        && settingsSave.Success
        && saveCompletedCount == 1;
}

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

sealed class ArtifactFixture
{
    public Persistence Persistence { get; } = new();
    public string ProgressValue { get; set; } = "progress-initial";
    public string SettingsValue { get; set; } = "settings-initial";
    public string RestoredProgress { get; private set; } = string.Empty;
    public string RestoredSettings { get; private set; } = string.Empty;
    public bool BlockProgress { get; set; }
    public bool BlockSettings { get; set; }

    public static ArtifactFixture Create()
    {
        var fixture = new ArtifactFixture();
        fixture.Persistence.RegisterDomainSerializer(
            PersistenceArtifactKind.Progress,
            "progress.resources",
            fixture.SerializeProgress);
        fixture.Persistence.RegisterDomainDeserializer(
            PersistenceArtifactKind.Progress,
            "progress.resources",
            fixture.RestoreProgress);
        fixture.Persistence.RegisterDomainSerializer(
            PersistenceArtifactKind.Settings,
            "settings.profile",
            fixture.SerializeSettings);
        fixture.Persistence.RegisterDomainDeserializer(
            PersistenceArtifactKind.Settings,
            "settings.profile",
            fixture.RestoreSettings);
        return fixture;
    }

    public void SaveBoth()
    {
        Persistence.RequestSaveProgress();
        Persistence.RequestSaveSettings();
    }

    public void ClearRestored()
    {
        RestoredProgress = string.Empty;
        RestoredSettings = string.Empty;
    }

    private SnapshotPackage SerializeProgress()
    {
        return MakePackage("progress.resources", "resources", "progress_value", ProgressValue, BlockProgress);
    }

    private SnapshotPackage SerializeSettings()
    {
        return MakePackage("settings.profile", "settings", "settings_value", SettingsValue, BlockSettings);
    }

    private void RestoreProgress(SnapshotPackage package)
    {
        RestoredProgress = package.Payload.TryGetValue("progress_value", out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;
    }

    private void RestoreSettings(SnapshotPackage package)
    {
        RestoredSettings = package.Payload.TryGetValue("settings_value", out var value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;
    }

    private static SnapshotPackage MakePackage(
        string domainId,
        string contentDomain,
        string payloadKey,
        string payloadValue,
        bool blocked)
    {
        var package = new SnapshotPackage
        {
            DomainId = domainId,
            SnapshotSchemaVersion = 1,
            DomainState = blocked ? SnapshotDomainState.Blocked : SnapshotDomainState.Ready,
            DomainErrorCode = blocked ? "blocked_for_test" : string.Empty,
        };
        package.ContentDomainVersions[contentDomain] = "1";
        package.StableIdRefs.Add($"{contentDomain}.fixture");
        package.Payload[payloadKey] = payloadValue;
        return package;
    }
}
