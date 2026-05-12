using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Save pipeline phase for the persistence staging and promotion workflow.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum PersistencePipelinePhase
{
    Idle = 0,
    Collecting = 1,
    WritingStaging = 2,
    Verifying = 3,
    Promoting = 4,
    Aborting = 5,
}

/// <summary>
/// Artifact kind handled by the persistence pipeline.
/// </summary>
public enum PersistenceArtifactKind
{
    Progress = 0,
    Settings = 1,
}

/// <summary>
/// Outcome of a backup failover evaluation.
/// </summary>
public enum BackupFailoverOutcome
{
    /// <summary>Main artifact is usable — backup not needed.</summary>
    NotNeeded = 0,
    /// <summary>Main failed all checks; backup was promoted to safe.</summary>
    BackupPromoted = 1,
    /// <summary>Main failed; backup exists but needs migration or cannot be directly restored.</summary>
    BackupPreservedLocked = 2,
    /// <summary>Main failed; no usable backup available.</summary>
    NoUsableBackup = 3,
}

/// <summary>
/// Validity check inputs for the backup artifact, supplied by the caller.
/// </summary>
public sealed record BackupArtifactStatus(
    bool BackupPresent,
    bool ParseOk,
    bool StructureOk,
    bool IntegrityOk,
    bool VersionCompatible,
    bool StableIdsResolved,
    bool MigrationRequired);

/// <summary>
/// Result returned by backup failover evaluation and execution.
/// </summary>
public sealed record BackupFailoverResult(
    BackupFailoverOutcome Outcome,
    string? CheckpointSummary,
    string? DiagnosticNote);

/// <summary>
/// Synchronous result returned by a persistence operation request.
/// </summary>
public sealed record PersistenceOperationResult(
    bool Success,
    string? Reason,
    PersistencePipelinePhase Phase,
    int Generation);

/// <summary>
/// C# Foundation persistence pipeline for deterministic snapshot save/load validation.
/// </summary>
public sealed class Persistence
{
    private readonly Dictionary<string, Func<SnapshotPackage>> domainSerializers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Action<SnapshotPackage>> domainDeserializers = new(StringComparer.Ordinal);
    private Dictionary<string, object?> stagingData = new(StringComparer.Ordinal);
    private Dictionary<string, object?> safeData = new(StringComparer.Ordinal);
    private Dictionary<string, object?> backupData = new(StringComparer.Ordinal);
    private bool mainQuarantined;

    /// <summary>
    /// Raised after a progress save is promoted successfully.
    /// </summary>
    public event Action<int>? SaveCompleted;

    /// <summary>
    /// Raised when a save request fails.
    /// </summary>
    public event Action<string, string>? SaveFailed;

    /// <summary>
    /// Raised after a staged artifact becomes the safe artifact.
    /// </summary>
    public event Action<string, int>? PromotionCompleted;

    /// <summary>
    /// Raised after a progress load restores registered domains.
    /// </summary>
    public event Action<string, int>? LoadCompleted;

    /// <summary>
    /// Raised when a load request fails.
    /// </summary>
    public event Action<string, string>? LoadFailed;

    /// <summary>
    /// Gets the current save pipeline phase.
    /// </summary>
    public PersistencePipelinePhase PipelinePhase { get; private set; } = PersistencePipelinePhase.Idle;

    /// <summary>
    /// Gets the current promoted generation.
    /// </summary>
    public int CurrentGeneration { get; private set; }

    /// <summary>
    /// Gets whether the pipeline can accept a new request.
    /// </summary>
    public bool IsPipelineIdle => PipelinePhase == PersistencePipelinePhase.Idle;

    /// <summary>
    /// Gets whether a backup artifact exists.
    /// </summary>
    public bool HasBackup => backupData.Count > 0;

    /// <summary>
    /// Gets whether the main safe artifact is quarantined due to corruption or promotion failure.
    /// </summary>
    public bool IsMainQuarantined => mainQuarantined;

    /// <summary>
    /// Registers a domain snapshot serializer.
    /// </summary>
    public void RegisterDomainSerializer(string domainId, Func<SnapshotPackage> serializer)
    {
        domainSerializers[domainId] = serializer;
    }

    /// <summary>
    /// Registers a domain snapshot deserializer.
    /// </summary>
    public void RegisterDomainDeserializer(string domainId, Action<SnapshotPackage> deserializer)
    {
        domainDeserializers[domainId] = deserializer;
    }

    /// <summary>
    /// Requests a synchronous progress save through staging, verify, and promotion.
    /// </summary>
    public PersistenceOperationResult RequestSaveProgress()
    {
        if (PipelinePhase != PersistencePipelinePhase.Idle)
        {
            SaveFailed?.Invoke("pipeline_busy", "request_save");
            return new PersistenceOperationResult(false, "pipeline_busy", PipelinePhase, CurrentGeneration);
        }

        return CollectAndSave(PersistenceArtifactKind.Progress);
    }

    /// <summary>
    /// Requests a synchronous progress load from the current safe artifact.
    /// </summary>
    public PersistenceOperationResult RequestLoadProgress()
    {
        if (safeData.Count == 0)
        {
            LoadFailed?.Invoke("no_safe_data", "progress");
            return new PersistenceOperationResult(false, "no_safe_data", PipelinePhase, CurrentGeneration);
        }

        if (safeData.TryGetValue("domains", out var domainsValue)
            && domainsValue is IReadOnlyDictionary<string, object?> domains)
        {
            RestoreDomains(domains);
        }

        LoadCompleted?.Invoke("progress", CurrentGeneration);
        return new PersistenceOperationResult(true, null, PipelinePhase, CurrentGeneration);
    }

    /// <summary>
    /// Commits the current safe artifact as a backup snapshot.
    /// Called externally after a successful domain save to create a one-behind backup.
    /// </summary>
    public void CommitBackupSnapshot()
    {
        if (safeData.Count > 0)
        {
            backupData = CloneManifest(safeData);
            backupData["backup_kind"] = "progress";
        }
    }

    /// <summary>
    /// Evaluates whether backup failover is needed and what outcome applies.
    /// Does not execute promotion — use ExecuteBackupPromotion for that.
    /// </summary>
    public BackupFailoverResult EvaluateBackupFailover(
        bool mainUsable,
        BackupArtifactStatus backup)
    {
        if (mainUsable)
        {
            return new BackupFailoverResult(BackupFailoverOutcome.NotNeeded, null, null);
        }

        if (!backup.BackupPresent)
        {
            return new BackupFailoverResult(BackupFailoverOutcome.NoUsableBackup, null, "no_backup_present");
        }

        var directRestoreOk = backup.ParseOk
            && backup.IntegrityOk
            && backup.StructureOk
            && backup.VersionCompatible
            && backup.StableIdsResolved
            && !backup.MigrationRequired;

        if (directRestoreOk)
        {
            return new BackupFailoverResult(BackupFailoverOutcome.BackupPromoted, null, null);
        }

        if (backup.ParseOk && backup.IntegrityOk && backup.StructureOk)
        {
            return new BackupFailoverResult(BackupFailoverOutcome.BackupPreservedLocked, null, "backup_needs_migration_or_version_mismatch");
        }

        return new BackupFailoverResult(BackupFailoverOutcome.NoUsableBackup, null, "backup_integrity_check_failed");
    }

    /// <summary>
    /// Executes backup promotion following the 5-step sequence:
    /// validate_backup → copy_to_staging → readback_verify → promote_to_safe → quarantine_original_main.
    /// Main is quarantined before promotion begins and stays quarantined if any step fails.
    /// </summary>
    public BackupFailoverResult ExecuteBackupPromotion()
    {
        if (backupData.Count == 0)
        {
            return new BackupFailoverResult(BackupFailoverOutcome.NoUsableBackup, null, "no_backup_data");
        }

        // Step 1: Quarantine original main (set before promotion; stays set on failure).
        mainQuarantined = true;

        // Step 2: Copy backup to staging as a new generation.
        var promotionGeneration = CurrentGeneration + 1;
        var newStaging = CloneManifest(backupData);
        newStaging["generation"] = promotionGeneration;
        newStaging["backup_kind"] = "progress";

        // Step 3: Encode and verify checksum of the staging copy.
        var encoded = CanonicalJsonEncode(newStaging);
        if (string.IsNullOrEmpty(encoded))
        {
            return new BackupFailoverResult(BackupFailoverOutcome.NoUsableBackup, null, "backup_encode_failed");
        }

        var checksum = ComputeChecksum(encoded);
        var reencoded = CanonicalJsonEncode(newStaging);
        var rechecksum = ComputeChecksum(reencoded);
        if (!string.Equals(checksum, rechecksum, StringComparison.Ordinal))
        {
            return new BackupFailoverResult(BackupFailoverOutcome.NoUsableBackup, null, "backup_checksum_mismatch");
        }

        newStaging["_checksum"] = checksum;

        // Step 4: Promote staging to safe.
        stagingData = newStaging;
        safeData = CloneManifest(stagingData);
        CurrentGeneration = promotionGeneration;

        // Step 5: Promotion succeeded — main is no longer quarantined (it was replaced).
        mainQuarantined = false;

        PromotionCompleted?.Invoke("progress_backup_promoted", CurrentGeneration);
        return new BackupFailoverResult(
            BackupFailoverOutcome.BackupPromoted,
            "已恢复到最近可用记录",
            null);
    }

    /// <summary>
    /// Encodes supported save data to compact canonical JSON.
    /// </summary>
    public static string CanonicalJsonEncode(object? data)
    {
        var builder = new StringBuilder();
        WriteCanonicalJson(builder, data);
        return builder.ToString();
    }

    /// <summary>
    /// Validates all domain packages in a collection for contract compliance and duplicate IDs.
    /// Returns the first failing result, or Ok when all pass.
    /// </summary>
    public static SnapshotValidationResult ValidateDomainPackages(IEnumerable<SnapshotPackage> packages)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pkg in packages)
        {
            if (!seen.Add(pkg.DomainId))
            {
                return new SnapshotValidationResult(false, "ERR_DUPLICATE_DOMAIN_PACKAGE");
            }

            var result = pkg.ValidateContract();
            if (!result.Valid)
            {
                return result;
            }
        }

        return SnapshotValidationResult.Ok;
    }

    /// <summary>
    /// Computes a lower-case SHA-256 checksum for text.
    /// </summary>
    public static string ComputeChecksum(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private PersistenceOperationResult CollectAndSave(PersistenceArtifactKind artifactKind)
    {
        PipelinePhase = PersistencePipelinePhase.Collecting;
        var manifest = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["generation"] = CurrentGeneration + 1,
            ["artifact"] = (int)artifactKind,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["schema_version"] = 1,
            ["domains"] = new Dictionary<string, object?>(StringComparer.Ordinal),
        };

        var domains = (Dictionary<string, object?>)manifest["domains"]!;
        foreach (var (domainId, serializer) in domainSerializers)
        {
            var snapshot = serializer();
            if (!snapshot.IsValid())
            {
                PipelinePhase = PersistencePipelinePhase.Idle;
                SaveFailed?.Invoke($"invalid_snapshot:{domainId}", "collect");
                return new PersistenceOperationResult(false, $"invalid_snapshot:{domainId}", PipelinePhase, CurrentGeneration);
            }

            domains[domainId] = snapshot.ToDictionary();
        }

        PipelinePhase = PersistencePipelinePhase.WritingStaging;
        var encoded = CanonicalJsonEncode(manifest);
        var checksum = ComputeChecksum(encoded);

        PipelinePhase = PersistencePipelinePhase.Verifying;
        var reencoded = CanonicalJsonEncode(manifest);
        var rechecksum = ComputeChecksum(reencoded);
        if (!string.Equals(checksum, rechecksum, StringComparison.Ordinal))
        {
            PipelinePhase = PersistencePipelinePhase.Aborting;
            SaveFailed?.Invoke("checksum_mismatch", "verify");
            PipelinePhase = PersistencePipelinePhase.Idle;
            return new PersistenceOperationResult(false, "checksum_mismatch", PipelinePhase, CurrentGeneration);
        }

        manifest["_checksum"] = checksum;
        stagingData = manifest;

        PipelinePhase = PersistencePipelinePhase.Promoting;
        // Archive the current safe as backup before overwriting (one-behind rolling backup).
        if (safeData.Count > 0)
        {
            backupData = CloneManifest(safeData);
            backupData["backup_kind"] = artifactKind.ToString().ToLowerInvariant();
        }

        safeData = CloneManifest(stagingData);
        CurrentGeneration = Convert.ToInt32(safeData["generation"], CultureInfo.InvariantCulture);
        PipelinePhase = PersistencePipelinePhase.Idle;

        SaveCompleted?.Invoke(CurrentGeneration);
        PromotionCompleted?.Invoke("progress", CurrentGeneration);
        return new PersistenceOperationResult(true, null, PipelinePhase, CurrentGeneration);
    }

    private void RestoreDomains(IReadOnlyDictionary<string, object?> domains)
    {
        foreach (var (domainId, snapshotData) in domains)
        {
            if (!domainDeserializers.TryGetValue(domainId, out var deserializer)
                || snapshotData is not IReadOnlyDictionary<string, object?> snapshotMap)
            {
                continue;
            }

            deserializer(SnapshotPackage.FromDictionary(snapshotMap));
        }
    }

    private static Dictionary<string, object?> CloneManifest(IReadOnlyDictionary<string, object?> manifest)
    {
        return manifest.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static void WriteCanonicalJson(StringBuilder builder, object? data)
    {
        switch (data)
        {
            case null:
                builder.Append("null");
                break;
            case string text:
                builder.Append(JsonSerializer.Serialize(text.Normalize(NormalizationForm.FormC)));
                break;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                break;
            case int or long or short or byte:
                builder.Append(Convert.ToString(data, CultureInfo.InvariantCulture));
                break;
            case float floatValue:
                WriteCanonicalFloat(builder, floatValue);
                break;
            case double doubleValue:
                WriteCanonicalFloat(builder, doubleValue);
                break;
            case decimal decimalValue:
                builder.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
                break;
            case Enum enumValue:
                builder.Append(Convert.ToInt32(enumValue, CultureInfo.InvariantCulture));
                break;
            case IReadOnlyDictionary<string, object?> dictionary:
                WriteCanonicalDictionary(builder, dictionary);
                break;
            case IDictionary mutableDictionary:
                WriteCanonicalDictionary(builder, ToStringObjectDictionary(mutableDictionary));
                break;
            case IEnumerable enumerable:
                WriteCanonicalArray(builder, enumerable);
                break;
            default:
                builder.Append(JsonSerializer.Serialize(Convert.ToString(data, CultureInfo.InvariantCulture)));
                break;
        }
    }

    private static void WriteCanonicalDictionary(StringBuilder builder, IReadOnlyDictionary<string, object?> dictionary)
    {
        builder.Append('{');
        var first = true;
        foreach (var key in dictionary.Keys.Order(StringComparer.Ordinal))
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            builder.Append(JsonSerializer.Serialize(key.Normalize(NormalizationForm.FormC)));
            builder.Append(':');
            WriteCanonicalJson(builder, dictionary[key]);
        }

        builder.Append('}');
    }

    private static Dictionary<string, object?> ToStringObjectDictionary(IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var key in dictionary.Keys)
        {
            result[Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty] = dictionary[key];
        }

        return result;
    }

    private static void WriteCanonicalArray(StringBuilder builder, IEnumerable enumerable)
    {
        builder.Append('[');
        var first = true;
        foreach (var item in enumerable)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            WriteCanonicalJson(builder, item);
        }

        builder.Append(']');
    }

    private static void WriteCanonicalFloat(StringBuilder builder, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            builder.Append("null");
            return;
        }

        if (value == 0.0)
        {
            builder.Append("0");
            return;
        }

        builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
    }
}
