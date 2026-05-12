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
/// Synchronous result returned by a persistence operation request.
/// </summary>
public sealed record PersistenceOperationResult(
    bool Success,
    string? Reason,
    PersistencePipelinePhase Phase,
    int Generation);

/// <summary>
/// 单个持久化工件的 durable metadata 快照。
/// </summary>
/// <param name="ArtifactKind">工件类型。</param>
/// <param name="State">当前工件状态。</param>
/// <param name="CurrentGeneration">当前 Safe generation。</param>
/// <param name="ManifestPointer">当前 manifest 指向的工件路径。</param>
/// <param name="LastVerifiedCheckpoint">最近已验证 checkpoint generation。</param>
/// <param name="CheckpointSummary">玩家可见 checkpoint 摘要。</param>
/// <param name="ReasonCode">当前锁定或失败原因码。</param>
/// <param name="BackupGeneration">当前备份工件 generation。</param>
/// <param name="BackupPromotionResult">最近一次备份提升结果。</param>
/// <param name="StorageCapability">该工件自己的存储能力。</param>
public sealed record PersistenceArtifactMetadata(
    PersistenceArtifactKind ArtifactKind,
    ArtifactState State,
    int CurrentGeneration,
    string ManifestPointer,
    int LastVerifiedCheckpoint,
    string CheckpointSummary,
    string ReasonCode,
    int BackupGeneration,
    string BackupPromotionResult,
    StorageCapability StorageCapability);

/// <summary>
/// C# Foundation persistence pipeline for deterministic snapshot save/load validation.
/// </summary>
public sealed class Persistence
{
    private readonly Dictionary<PersistenceArtifactKind, Dictionary<string, Func<SnapshotPackage>>> domainSerializers =
        CreateSerializerMap();
    private readonly Dictionary<PersistenceArtifactKind, Dictionary<string, Action<SnapshotPackage>>> domainDeserializers =
        CreateDeserializerMap();
    private readonly Dictionary<PersistenceArtifactKind, ArtifactSlot> artifactSlots = CreateArtifactSlots();

    private sealed class ArtifactSlot
    {
        public ArtifactSlot(PersistenceArtifactKind artifactKind)
        {
            ArtifactKind = artifactKind;
        }

        public PersistenceArtifactKind ArtifactKind { get; }
        public Dictionary<string, object?> StagingData { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, object?> SafeData { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, object?> BackupData { get; set; } = new(StringComparer.Ordinal);
        public int CurrentGeneration { get; set; }
        public int BackupGeneration { get; set; }
        public int LastVerifiedCheckpoint { get; set; }
        public string ManifestPointer { get; set; } = string.Empty;
        public string CheckpointSummary { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public string BackupPromotionResult { get; set; } = string.Empty;
        public StorageCapability StorageCapability { get; set; } = StorageCapability.PersistentAvailable;
        public ArtifactStatus RecoveryStatus { get; set; } =
            new(ArtifactState.Missing, false, false, false, false);
    }

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
    /// 备份被提升为新的 Safe 工件后触发。
    /// </summary>
    public event Action<int>? BackupPromoted;

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
    public int CurrentGeneration => GetSlot(PersistenceArtifactKind.Progress).CurrentGeneration;

    /// <summary>
    /// 当前自动备份工件的独立 generation。
    /// </summary>
    public int BackupGeneration => GetSlot(PersistenceArtifactKind.Progress).BackupGeneration;

    /// <summary>
    /// 当前 progress 主工件状态。
    /// </summary>
    public ArtifactState ProgressArtifactState => GetSlot(PersistenceArtifactKind.Progress).RecoveryStatus.State;

    /// <summary>
    /// 最近一次 checkpoint 的玩家可见摘要。
    /// </summary>
    public string CheckpointSummary => GetSlot(PersistenceArtifactKind.Progress).CheckpointSummary;

    /// <summary>
    /// 是否已有独立自动备份工件。
    /// </summary>
    public bool HasBackup => GetSlot(PersistenceArtifactKind.Progress).BackupData.Count > 0;

    /// <summary>
    /// Gets whether the pipeline can accept a new request.
    /// </summary>
    public bool IsPipelineIdle => PipelinePhase == PersistencePipelinePhase.Idle;

    /// <summary>
    /// Registers a domain snapshot serializer.
    /// </summary>
    public void RegisterDomainSerializer(string domainId, Func<SnapshotPackage> serializer)
    {
        RegisterDomainSerializer(PersistenceArtifactKind.Progress, domainId, serializer);
    }

    /// <summary>
    /// 为指定工件注册领域快照序列化器。
    /// </summary>
    /// <param name="artifactKind">要写入的工件类型。</param>
    /// <param name="domainId">领域稳定 ID。</param>
    /// <param name="serializer">领域快照序列化器。</param>
    public void RegisterDomainSerializer(
        PersistenceArtifactKind artifactKind,
        string domainId,
        Func<SnapshotPackage> serializer)
    {
        domainSerializers[artifactKind][domainId] = serializer;
    }

    /// <summary>
    /// Registers a domain snapshot deserializer.
    /// </summary>
    public void RegisterDomainDeserializer(string domainId, Action<SnapshotPackage> deserializer)
    {
        RegisterDomainDeserializer(PersistenceArtifactKind.Progress, domainId, deserializer);
    }

    /// <summary>
    /// 为指定工件注册领域快照反序列化器。
    /// </summary>
    /// <param name="artifactKind">要读取的工件类型。</param>
    /// <param name="domainId">领域稳定 ID。</param>
    /// <param name="deserializer">领域快照反序列化器。</param>
    public void RegisterDomainDeserializer(
        PersistenceArtifactKind artifactKind,
        string domainId,
        Action<SnapshotPackage> deserializer)
    {
        domainDeserializers[artifactKind][domainId] = deserializer;
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
    /// 请求同步保存设置工件，独立于 progress 工件 promotion。
    /// </summary>
    /// <returns>设置保存请求结果。</returns>
    public PersistenceOperationResult RequestSaveSettings()
    {
        if (PipelinePhase != PersistencePipelinePhase.Idle)
        {
            SaveFailed?.Invoke("pipeline_busy", "request_save_settings");
            return new PersistenceOperationResult(false, "pipeline_busy", PipelinePhase, GetSlot(PersistenceArtifactKind.Settings).CurrentGeneration);
        }

        return CollectAndSave(PersistenceArtifactKind.Settings);
    }

    /// <summary>
    /// Requests a synchronous progress load from the current safe artifact.
    /// </summary>
    public PersistenceOperationResult RequestLoadProgress()
    {
        return RequestLoadArtifact(PersistenceArtifactKind.Progress);
    }

    /// <summary>
    /// 请求从 settings 当前 Safe 工件恢复设置。
    /// </summary>
    /// <returns>设置读取请求结果。</returns>
    public PersistenceOperationResult RequestLoadSettings()
    {
        return RequestLoadArtifact(PersistenceArtifactKind.Settings);
    }

    /// <summary>
    /// 设置指定工件自己的存储能力判定。
    /// </summary>
    /// <param name="artifactKind">工件类型。</param>
    /// <param name="storageCapability">该工件当前存储能力。</param>
    public void SetArtifactStorageCapability(
        PersistenceArtifactKind artifactKind,
        StorageCapability storageCapability)
    {
        GetSlot(artifactKind).StorageCapability = storageCapability;
    }

    /// <summary>
    /// 设置恢复前检查得到的工件状态。
    /// </summary>
    /// <param name="artifactKind">工件类型。</param>
    /// <param name="status">恢复状态。</param>
    /// <param name="reasonCode">机器可读原因码。</param>
    public void SetArtifactRecoveryStatus(
        PersistenceArtifactKind artifactKind,
        ArtifactStatus status,
        string reasonCode = "")
    {
        var slot = GetSlot(artifactKind);
        slot.RecoveryStatus = status;
        slot.ReasonCode = reasonCode;
    }

    /// <summary>
    /// 使用 progress 工件自己的状态和存储能力计算 Continue。
    /// </summary>
    /// <returns>Continue 查询结果。</returns>
    public ContinueStateResult QueryContinueState()
    {
        var progress = GetSlot(PersistenceArtifactKind.Progress);
        var settings = GetSlot(PersistenceArtifactKind.Settings);
        return ContinueStateQuery.QueryContinueState(
            progress.StorageCapability,
            progress.RecoveryStatus,
            settings.RecoveryStatus,
            progress.CurrentGeneration);
    }

    /// <summary>
    /// 读取单个工件的 durable metadata。
    /// </summary>
    /// <param name="artifactKind">工件类型。</param>
    /// <returns>该工件当前 metadata 快照。</returns>
    public PersistenceArtifactMetadata GetArtifactMetadata(PersistenceArtifactKind artifactKind)
    {
        var slot = GetSlot(artifactKind);
        return new PersistenceArtifactMetadata(
            slot.ArtifactKind,
            slot.RecoveryStatus.State,
            slot.CurrentGeneration,
            slot.ManifestPointer,
            slot.LastVerifiedCheckpoint,
            slot.CheckpointSummary,
            slot.ReasonCode,
            slot.BackupGeneration,
            slot.BackupPromotionResult,
            slot.StorageCapability);
    }

    /// <summary>
    /// 导出以 artifact kind 为前缀的 durable metadata 字典。
    /// </summary>
    /// <returns>包含 progress.* 和 settings.* 键的 metadata 字典。</returns>
    public IReadOnlyDictionary<string, object?> ExportDurableMetadata()
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var artifactKind in new[] { PersistenceArtifactKind.Progress, PersistenceArtifactKind.Settings })
        {
            var slot = GetSlot(artifactKind);
            var prefix = GetArtifactKindKey(artifactKind);
            metadata[$"{prefix}.artifact_state"] = (int)slot.RecoveryStatus.State;
            metadata[$"{prefix}.current_generation"] = slot.CurrentGeneration;
            metadata[$"{prefix}.manifest_pointer"] = slot.ManifestPointer;
            metadata[$"{prefix}.last_verified_checkpoint"] = slot.LastVerifiedCheckpoint;
            metadata[$"{prefix}.checkpoint_summary"] = slot.CheckpointSummary;
            metadata[$"{prefix}.reason_code"] = slot.ReasonCode;
            metadata[$"{prefix}.backup_generation"] = slot.BackupGeneration;
            metadata[$"{prefix}.backup_promotion_result"] = slot.BackupPromotionResult;
            metadata[$"{prefix}.storage_capability"] = (int)slot.StorageCapability;
        }

        return metadata;
    }

    /// <summary>
    /// 请求按 Story 006 规则执行备份故障转移。
    /// </summary>
    /// <param name="mainProbe">主继续点工件探针。</param>
    /// <param name="backupProbe">自动备份工件探针。</param>
    /// <param name="steps">提升流程各阶段结果；为空时视为全部成功。</param>
    /// <param name="storageCapability">当前持久化能力。</param>
    /// <returns>备份提升执行结果，包含 Continue 重新计算结果。</returns>
    public BackupPromotionExecutionResult RequestBackupFailover(
        SaveArtifactProbe mainProbe,
        SaveArtifactProbe backupProbe,
        BackupPromotionStepResults? steps = null,
        StorageCapability storageCapability = StorageCapability.PersistentAvailable)
    {
        var progress = GetSlot(PersistenceArtifactKind.Progress);
        var result = BackupFailoverPolicy.ExecutePromotion(mainProbe, backupProbe, steps, storageCapability);

        if (result.Success && progress.BackupData.Count == 0)
        {
            var missingPayloadState = ContinueStateQuery.QueryContinueState(
                storageCapability,
                mainProbe.ToArtifactStatus(mainProbe.Present ? ArtifactState.Quarantined : ArtifactState.Missing),
                settingsStatus: null,
                mainProbe.Generation);

            result = result with
            {
                Success = false,
                Phase = BackupPromotionPhase.Failed,
                OldMainState = mainProbe.Present ? ArtifactState.Quarantined : ArtifactState.Missing,
                BackupRetained = false,
                ContinueState = missingPayloadState,
                CheckpointSummary = string.Empty,
                PromotedGeneration = 0,
                FailureReason = "backup_payload_missing",
            };
        }

        if (result.Success)
        {
            progress.SafeData = CloneManifest(progress.BackupData);
            progress.SafeData["generation"] = result.PromotedGeneration;
            progress.SafeData["_promoted_from_backup_generation"] = progress.BackupGeneration;
            progress.SafeData.Remove("_checksum");
            progress.SafeData["_checksum"] = ComputeChecksum(CanonicalJsonEncode(progress.SafeData));

            progress.CurrentGeneration = result.PromotedGeneration;
            progress.LastVerifiedCheckpoint = result.PromotedGeneration;
            progress.ManifestPointer = BuildManifestPointer(PersistenceArtifactKind.Progress, result.PromotedGeneration);
            progress.RecoveryStatus = new ArtifactStatus(ArtifactState.Safe, true, true, true, false);
            progress.ReasonCode = string.Empty;
            progress.CheckpointSummary = result.CheckpointSummary;
            progress.BackupPromotionResult = result.Outcome.ToString();
            BackupPromoted?.Invoke(progress.CurrentGeneration);
            return result;
        }

        progress.RecoveryStatus = progress.RecoveryStatus with { State = result.OldMainState };
        progress.BackupPromotionResult = result.Outcome.ToString();
        progress.ReasonCode = result.FailureReason;
        if (result.Outcome == BackupFailoverOutcome.NotNeeded)
        {
            progress.RecoveryStatus = progress.RecoveryStatus with { State = ArtifactState.Safe };
        }

        return result;
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
        var slot = GetSlot(artifactKind);
        var artifactKey = GetArtifactKindKey(artifactKind);
        if (slot.StorageCapability != StorageCapability.PersistentAvailable)
        {
            slot.ReasonCode = "storage_not_writable";
            SaveFailed?.Invoke("storage_not_writable", "capability");
            return new PersistenceOperationResult(false, "storage_not_writable", PipelinePhase, slot.CurrentGeneration);
        }

        PipelinePhase = PersistencePipelinePhase.Collecting;
        var previousSafe = slot.SafeData.Count > 0 ? CloneManifest(slot.SafeData) : null;
        var manifest = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["generation"] = slot.CurrentGeneration + 1,
            ["artifact"] = (int)artifactKind,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["schema_version"] = 1,
            ["domains"] = new Dictionary<string, object?>(StringComparer.Ordinal),
        };

        var domains = (Dictionary<string, object?>)manifest["domains"]!;
        foreach (var (domainId, serializer) in domainSerializers[artifactKind])
        {
            var snapshot = serializer();
            if (!snapshot.IsValid())
            {
                PipelinePhase = PersistencePipelinePhase.Idle;
                slot.ReasonCode = $"invalid_snapshot:{domainId}";
                SaveFailed?.Invoke($"invalid_snapshot:{domainId}", "collect");
                return new PersistenceOperationResult(false, $"invalid_snapshot:{domainId}", PipelinePhase, slot.CurrentGeneration);
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
            slot.ReasonCode = "checksum_mismatch";
            SaveFailed?.Invoke("checksum_mismatch", "verify");
            PipelinePhase = PersistencePipelinePhase.Idle;
            return new PersistenceOperationResult(false, "checksum_mismatch", PipelinePhase, slot.CurrentGeneration);
        }

        manifest["_checksum"] = checksum;
        slot.StagingData = manifest;

        PipelinePhase = PersistencePipelinePhase.Promoting;
        slot.SafeData = CloneManifest(slot.StagingData);
        if (previousSafe is not null)
        {
            slot.BackupData = previousSafe;
            slot.BackupGeneration = Convert.ToInt32(slot.BackupData["generation"], CultureInfo.InvariantCulture);
        }

        slot.CurrentGeneration = Convert.ToInt32(slot.SafeData["generation"], CultureInfo.InvariantCulture);
        slot.LastVerifiedCheckpoint = slot.CurrentGeneration;
        slot.ManifestPointer = BuildManifestPointer(artifactKind, slot.CurrentGeneration);
        slot.RecoveryStatus = new ArtifactStatus(ArtifactState.Safe, true, true, true, false);
        slot.ReasonCode = string.Empty;
        PipelinePhase = PersistencePipelinePhase.Idle;

        if (artifactKind == PersistenceArtifactKind.Progress)
        {
            SaveCompleted?.Invoke(slot.CurrentGeneration);
        }

        PromotionCompleted?.Invoke(artifactKey, slot.CurrentGeneration);
        return new PersistenceOperationResult(true, null, PipelinePhase, slot.CurrentGeneration);
    }

    private PersistenceOperationResult RequestLoadArtifact(PersistenceArtifactKind artifactKind)
    {
        var slot = GetSlot(artifactKind);
        var artifactKey = GetArtifactKindKey(artifactKind);
        if (slot.SafeData.Count == 0)
        {
            LoadFailed?.Invoke("no_safe_data", artifactKey);
            return new PersistenceOperationResult(false, "no_safe_data", PipelinePhase, slot.CurrentGeneration);
        }

        if (slot.SafeData.TryGetValue("domains", out var domainsValue)
            && domainsValue is IReadOnlyDictionary<string, object?> domains)
        {
            RestoreDomains(artifactKind, domains);
        }

        LoadCompleted?.Invoke(artifactKey, slot.CurrentGeneration);
        return new PersistenceOperationResult(true, null, PipelinePhase, slot.CurrentGeneration);
    }

    private void RestoreDomains(
        PersistenceArtifactKind artifactKind,
        IReadOnlyDictionary<string, object?> domains)
    {
        foreach (var (domainId, snapshotData) in domains)
        {
            if (!domainDeserializers[artifactKind].TryGetValue(domainId, out var deserializer)
                || snapshotData is not IReadOnlyDictionary<string, object?> snapshotMap)
            {
                continue;
            }

            deserializer(SnapshotPackage.FromDictionary(snapshotMap));
        }
    }

    private ArtifactSlot GetSlot(PersistenceArtifactKind artifactKind)
    {
        return artifactSlots[artifactKind];
    }

    private static Dictionary<PersistenceArtifactKind, ArtifactSlot> CreateArtifactSlots()
    {
        return new Dictionary<PersistenceArtifactKind, ArtifactSlot>
        {
            [PersistenceArtifactKind.Progress] = new(PersistenceArtifactKind.Progress),
            [PersistenceArtifactKind.Settings] = new(PersistenceArtifactKind.Settings),
        };
    }

    private static Dictionary<PersistenceArtifactKind, Dictionary<string, Func<SnapshotPackage>>> CreateSerializerMap()
    {
        return new Dictionary<PersistenceArtifactKind, Dictionary<string, Func<SnapshotPackage>>>
        {
            [PersistenceArtifactKind.Progress] = new(StringComparer.Ordinal),
            [PersistenceArtifactKind.Settings] = new(StringComparer.Ordinal),
        };
    }

    private static Dictionary<PersistenceArtifactKind, Dictionary<string, Action<SnapshotPackage>>> CreateDeserializerMap()
    {
        return new Dictionary<PersistenceArtifactKind, Dictionary<string, Action<SnapshotPackage>>>
        {
            [PersistenceArtifactKind.Progress] = new(StringComparer.Ordinal),
            [PersistenceArtifactKind.Settings] = new(StringComparer.Ordinal),
        };
    }

    private static string GetArtifactKindKey(PersistenceArtifactKind artifactKind)
    {
        return artifactKind == PersistenceArtifactKind.Settings ? "settings" : "progress";
    }

    private static string BuildManifestPointer(PersistenceArtifactKind artifactKind, int generation)
    {
        return $"{GetArtifactKindKey(artifactKind)}/gen_{generation:0000}.json";
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
