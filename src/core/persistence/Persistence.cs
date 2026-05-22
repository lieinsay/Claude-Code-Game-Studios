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
/// 持久化 Artifact 的当前元数据快照，用于测试断言和诊断输出。
/// </summary>
/// <param name="CurrentGeneration">当前已提升的 generation 编号。</param>
/// <param name="State">工件健康状态（Safe / Quarantined / Missing）。</param>
/// <param name="ReasonCode">最近失败或状态转换的机读原因码；正常时为空。</param>
public sealed record ArtifactMetadata(
    int CurrentGeneration,
    ArtifactState State,
    string ReasonCode);

/// <summary>
/// C# Foundation persistence pipeline for deterministic snapshot save/load validation.
/// 支持 settings 与 progress 两路独立工件槽，每路独立 staging/verify/promotion，
/// 互不干扰（ADR-0003 非干扰规则）。
/// </summary>
public sealed class Persistence
{
    // ---------------------------------------------------------------------------
    // 内部工件槽
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 单个工件类型（progress 或 settings）的独立存储槽，
    /// 持有序列化器、反序列化器、暂存/安全数据、generation 和恢复状态。
    /// </summary>
    private sealed class ArtifactSlot
    {
        /// <summary>该槽的领域序列化器，key 为 domain ID。</summary>
        public Dictionary<string, Func<SnapshotPackage>> Serializers { get; } =
            new(StringComparer.Ordinal);

        /// <summary>该槽的领域反序列化器，key 为 domain ID。</summary>
        public Dictionary<string, Action<SnapshotPackage>> Deserializers { get; } =
            new(StringComparer.Ordinal);

        /// <summary>最近成功提升的安全数据 manifest。</summary>
        public Dictionary<string, object?> SafeData { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>当前暂存阶段的 manifest（尚未提升）。</summary>
        public Dictionary<string, object?> StagingData { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>当前已提升的 generation 编号。</summary>
        public int CurrentGeneration { get; set; }

        /// <summary>工件健康状态。</summary>
        public ArtifactState State { get; set; } = ArtifactState.Missing;

        /// <summary>当前存储能力；默认 PersistentAvailable，可由外部注入设置。</summary>
        public StorageCapability StorageCapability { get; set; } =
            StorageCapability.PersistentAvailable;

        /// <summary>最近状态变化的机读原因码。</summary>
        public string ReasonCode { get; set; } = string.Empty;

        /// <summary>最近已验证检查点的摘要，用于 checkpoint_summary 元数据导出。</summary>
        public string CheckpointSummary { get; set; } = string.Empty;

        /// <summary>最近已验证检查点的逻辑时间戳（Unix 秒）。</summary>
        public long LastVerifiedCheckpoint { get; set; }

        /// <summary>当前备份 generation 编号（0 = 尚无备份）。</summary>
        public int BackupGeneration { get; set; }

        /// <summary>manifest pointer，即最近提升的 manifest 路径或占位符。</summary>
        public string ManifestPointer { get; set; } = string.Empty;
    }

    // ---------------------------------------------------------------------------
    // 字段
    // ---------------------------------------------------------------------------

    private readonly ArtifactSlot _progressSlot = new();
    private readonly ArtifactSlot _settingsSlot = new();

    // ---------------------------------------------------------------------------
    // 向后兼容：无 artifact kind 的旧注册接口仍路由到 progress 槽
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 注册 progress 领域的快照序列化器（旧接口，向后兼容）。
    /// </summary>
    /// <param name="domainId">领域 ID，必须与反序列化器匹配。</param>
    /// <param name="serializer">快照序列化工厂。</param>
    /// <example>
    /// <code>
    /// persistence.RegisterDomainSerializer("progress.resources", () => new SnapshotPackage { … });
    /// </code>
    /// </example>
    public void RegisterDomainSerializer(string domainId, Func<SnapshotPackage> serializer)
    {
        _progressSlot.Serializers[domainId] = serializer;
    }

    /// <summary>
    /// 注册 progress 领域的快照反序列化器（旧接口，向后兼容）。
    /// </summary>
    /// <param name="domainId">领域 ID，必须与序列化器匹配。</param>
    /// <param name="deserializer">接收恢复快照的处理程序。</param>
    public void RegisterDomainDeserializer(string domainId, Action<SnapshotPackage> deserializer)
    {
        _progressSlot.Deserializers[domainId] = deserializer;
    }

    // ---------------------------------------------------------------------------
    // 新接口：按 artifact kind 路由序列化器/反序列化器
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 按工件类型注册领域快照序列化器。
    /// </summary>
    /// <param name="artifactKind">目标工件类型（Progress 或 Settings）。</param>
    /// <param name="domainId">领域 ID。</param>
    /// <param name="serializer">快照序列化工厂。</param>
    /// <example>
    /// <code>
    /// persistence.RegisterDomainSerializer(PersistenceArtifactKind.Settings,
    ///     "settings.profile", () => new SnapshotPackage { … });
    /// </code>
    /// </example>
    public void RegisterDomainSerializer(
        PersistenceArtifactKind artifactKind,
        string domainId,
        Func<SnapshotPackage> serializer)
    {
        GetSlot(artifactKind).Serializers[domainId] = serializer;
    }

    /// <summary>
    /// 按工件类型注册领域快照反序列化器。
    /// </summary>
    /// <param name="artifactKind">目标工件类型（Progress 或 Settings）。</param>
    /// <param name="domainId">领域 ID。</param>
    /// <param name="deserializer">接收恢复快照的处理程序。</param>
    public void RegisterDomainDeserializer(
        PersistenceArtifactKind artifactKind,
        string domainId,
        Action<SnapshotPackage> deserializer)
    {
        GetSlot(artifactKind).Deserializers[domainId] = deserializer;
    }

    // ---------------------------------------------------------------------------
    // 事件
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 仅在 progress 工件提升成功后触发；settings 提升不触发此事件（非干扰规则）。
    /// </summary>
    public event Action<int>? SaveCompleted;

    /// <summary>
    /// 任意工件保存请求失败时触发。
    /// </summary>
    public event Action<string, string>? SaveFailed;

    /// <summary>
    /// 任意工件提升成功后触发。
    /// </summary>
    public event Action<string, int>? PromotionCompleted;

    /// <summary>
    /// 进度加载恢复所有已注册领域后触发。
    /// </summary>
    public event Action<string, int>? LoadCompleted;

    /// <summary>
    /// 加载请求失败时触发。
    /// </summary>
    public event Action<string, string>? LoadFailed;

    // ---------------------------------------------------------------------------
    // 属性
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 当前 save pipeline 阶段。
    /// </summary>
    public PersistencePipelinePhase PipelinePhase { get; private set; } =
        PersistencePipelinePhase.Idle;

    /// <summary>
    /// 当前已提升的 progress generation（向后兼容）。
    /// </summary>
    public int CurrentGeneration => _progressSlot.CurrentGeneration;

    /// <summary>
    /// Pipeline 是否处于 Idle 状态，可接受新请求。
    /// </summary>
    public bool IsPipelineIdle => PipelinePhase == PersistencePipelinePhase.Idle;

    // ---------------------------------------------------------------------------
    // 保存 / 加载 — progress
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 请求同步 progress 工件保存（staging → verify → promotion）。
    /// </summary>
    /// <returns>操作结果；Success=false 时含 Reason 说明。</returns>
    /// <example>
    /// <code>
    /// var result = persistence.RequestSaveProgress();
    /// if (!result.Success) Log(result.Reason);
    /// </code>
    /// </example>
    public PersistenceOperationResult RequestSaveProgress()
    {
        if (PipelinePhase != PersistencePipelinePhase.Idle)
        {
            SaveFailed?.Invoke("pipeline_busy", "request_save");
            return new PersistenceOperationResult(false, "pipeline_busy", PipelinePhase, _progressSlot.CurrentGeneration);
        }

        return CollectAndSave(PersistenceArtifactKind.Progress);
    }

    /// <summary>
    /// 请求从当前安全 progress 工件同步恢复所有已注册领域。
    /// </summary>
    /// <returns>操作结果。</returns>
    public PersistenceOperationResult RequestLoadProgress()
    {
        return LoadFromSlot(_progressSlot, "progress");
    }

    // ---------------------------------------------------------------------------
    // 保存 / 加载 — settings
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 请求同步 settings 工件保存（staging → verify → promotion）。
    /// settings 失败不影响 progress 状态；SaveCompleted 不会因此触发。
    /// </summary>
    /// <returns>操作结果；Success=false 时含 Reason 说明。</returns>
    /// <example>
    /// <code>
    /// var result = persistence.RequestSaveSettings();
    /// if (!result.Success) Log(result.Reason);
    /// </code>
    /// </example>
    public PersistenceOperationResult RequestSaveSettings()
    {
        if (PipelinePhase != PersistencePipelinePhase.Idle)
        {
            SaveFailed?.Invoke("pipeline_busy", "request_save_settings");
            return new PersistenceOperationResult(false, "pipeline_busy", PipelinePhase, _settingsSlot.CurrentGeneration);
        }

        return CollectAndSave(PersistenceArtifactKind.Settings);
    }

    /// <summary>
    /// 请求从当前安全 settings 工件同步恢复所有已注册设置领域。
    /// </summary>
    /// <returns>操作结果。</returns>
    public PersistenceOperationResult RequestLoadSettings()
    {
        return LoadFromSlot(_settingsSlot, "settings");
    }

    // ---------------------------------------------------------------------------
    // 工件元数据 / 状态注入
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 返回指定工件类型的当前元数据快照。
    /// </summary>
    /// <param name="artifactKind">目标工件类型。</param>
    /// <returns>包含 generation、状态和 reason code 的元数据记录。</returns>
    /// <example>
    /// <code>
    /// var meta = persistence.GetArtifactMetadata(PersistenceArtifactKind.Progress);
    /// Console.WriteLine($"gen={meta.CurrentGeneration} state={meta.State}");
    /// </code>
    /// </example>
    public ArtifactMetadata GetArtifactMetadata(PersistenceArtifactKind artifactKind)
    {
        var slot = GetSlot(artifactKind);
        return new ArtifactMetadata(slot.CurrentGeneration, slot.State, slot.ReasonCode);
    }

    /// <summary>
    /// 由测试或备份故障转移注入工件恢复状态，覆盖内部 ArtifactState 与 reason code。
    /// 不修改另一侧工件的任何状态（非干扰规则）。
    /// </summary>
    /// <param name="artifactKind">目标工件类型。</param>
    /// <param name="status">新的恢复状态。</param>
    /// <param name="reasonCode">机读原因码。</param>
    /// <example>
    /// <code>
    /// persistence.SetArtifactRecoveryStatus(
    ///     PersistenceArtifactKind.Settings,
    ///     new ArtifactStatus(ArtifactState.Quarantined, false, true, true, false),
    ///     "settings_corrupt");
    /// </code>
    /// </example>
    public void SetArtifactRecoveryStatus(
        PersistenceArtifactKind artifactKind,
        ArtifactStatus status,
        string reasonCode)
    {
        var slot = GetSlot(artifactKind);
        slot.State = status.State;
        slot.ReasonCode = reasonCode;
    }

    /// <summary>
    /// 注入指定工件类型的存储能力，供测试和平台 shell 使用。
    /// 不影响另一侧工件的存储能力（AC-8 非干扰规则）。
    /// </summary>
    /// <param name="artifactKind">目标工件类型。</param>
    /// <param name="capability">新的存储能力值。</param>
    /// <example>
    /// <code>
    /// persistence.SetArtifactStorageCapability(
    ///     PersistenceArtifactKind.Progress,
    ///     StorageCapability.WriteLocked);
    /// </code>
    /// </example>
    public void SetArtifactStorageCapability(
        PersistenceArtifactKind artifactKind,
        StorageCapability capability)
    {
        GetSlot(artifactKind).StorageCapability = capability;
    }

    // ---------------------------------------------------------------------------
    // Continue 状态查询
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 计算 Continue 可用性，完全由 progress 工件状态和 progress 存储能力决定。
    /// settings 工件状态不影响 Continue 结果（AC-5 / AC-8 非干扰规则）。
    /// </summary>
    /// <returns>包含 Availability、WriteBarrier、StorageCapability 的完整结果。</returns>
    /// <example>
    /// <code>
    /// var result = persistence.QueryContinueState();
    /// if (result.Availability == ContinueAvailability.Enabled) ShowContinueButton();
    /// </code>
    /// </example>
    public ContinueStateResult QueryContinueState()
    {
        var progressStatus = BuildArtifactStatus(_progressSlot);
        var settingsStatus = BuildArtifactStatus(_settingsSlot);

        return ContinueStateQuery.QueryContinueState(
            _progressSlot.StorageCapability,
            progressStatus,
            settingsStatus,
            _progressSlot.CurrentGeneration);
    }

    // ---------------------------------------------------------------------------
    // 持久化元数据导出（AC-7）
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 导出所有工件的持久化元数据，以 "progress." 和 "settings." 为 key 前缀，
    /// 两侧 generation 独立存储，互不共用（AC-7）。
    /// </summary>
    /// <returns>包含全部 durable metadata 字段的字典。</returns>
    /// <example>
    /// <code>
    /// var meta = persistence.ExportDurableMetadata();
    /// var gen = (int)meta["progress.current_generation"]!;
    /// </code>
    /// </example>
    public Dictionary<string, object?> ExportDurableMetadata()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        ExportSlotMetadata(result, "progress", _progressSlot);
        ExportSlotMetadata(result, "settings", _settingsSlot);
        return result;
    }

    /// <summary>
    /// 导出指定工件最近提升的安全 manifest，供平台层写入真实耐久介质。
    /// 返回空字典表示该工件尚无安全数据。
    /// </summary>
    /// <param name="artifactKind">目标工件类型。</param>
    /// <returns>可被规范 JSON 编码的 manifest 副本。</returns>
    public Dictionary<string, object?> ExportArtifactManifest(PersistenceArtifactKind artifactKind)
    {
        return CloneManifest(GetSlot(artifactKind).SafeData);
    }

    /// <summary>
    /// 从平台层读取到的 manifest 导入指定工件的安全槽。
    /// 导入只恢复工件槽状态；领域对象仍通过 RequestLoadProgress/Settings 统一反序列化。
    /// </summary>
    /// <param name="artifactKind">目标工件类型。</param>
    /// <param name="manifest">从耐久介质读取的 manifest。</param>
    /// <param name="reason">导入失败时的机读原因码。</param>
    /// <returns>导入是否成功。</returns>
    public bool TryImportArtifactManifest(
        PersistenceArtifactKind artifactKind,
        IReadOnlyDictionary<string, object?> manifest,
        out string reason)
    {
        reason = string.Empty;
        if (!manifest.TryGetValue("domains", out var domainsValue)
            || domainsValue is not IReadOnlyDictionary<string, object?>)
        {
            reason = "missing_domains";
            return false;
        }

        if (!TryReadInt(manifest, "generation", out var generation) || generation <= 0)
        {
            reason = "invalid_generation";
            return false;
        }

        if (manifest.TryGetValue("_checksum", out var checksumValue)
            && !string.IsNullOrWhiteSpace(checksumValue?.ToString()))
        {
            var checksumManifest = CloneManifest(manifest);
            checksumManifest.Remove("_checksum");
            var actualChecksum = ComputeChecksum(CanonicalJsonEncode(checksumManifest));
            if (!string.Equals(checksumValue.ToString(), actualChecksum, StringComparison.Ordinal))
            {
                reason = "checksum_mismatch";
                return false;
            }
        }

        var slot = GetSlot(artifactKind);
        var kindLabel = artifactKind == PersistenceArtifactKind.Progress ? "progress" : "settings";
        slot.SafeData = CloneManifest(manifest);
        slot.CurrentGeneration = generation;
        slot.State = ArtifactState.Safe;
        slot.ReasonCode = string.Empty;
        slot.LastVerifiedCheckpoint = TryReadLong(manifest, "timestamp", out var timestamp) ? timestamp : 0;
        slot.ManifestPointer = $"{kindLabel}:gen{slot.CurrentGeneration}:imported";
        slot.CheckpointSummary = $"gen{slot.CurrentGeneration}";
        return true;
    }

    // ---------------------------------------------------------------------------
    // 静态工具
    // ---------------------------------------------------------------------------

    /// <summary>
    /// 验证一组领域包集合的契约合规性和 ID 唯一性。
    /// 返回第一个失败结果，全部通过时返回 Ok。
    /// </summary>
    /// <param name="packages">待验证的快照包序列。</param>
    /// <returns>首个失败的验证结果，或 <see cref="SnapshotValidationResult.Ok"/>。</returns>
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
    /// 将支持的存档数据编码为压缩规范 JSON（key 按 Unicode 序排序，NFC 归一化）。
    /// </summary>
    /// <param name="data">待编码的对象图。</param>
    /// <returns>规范 JSON 字符串。</returns>
    public static string CanonicalJsonEncode(object? data)
    {
        var builder = new StringBuilder();
        WriteCanonicalJson(builder, data);
        return builder.ToString();
    }

    /// <summary>
    /// 解码由 CanonicalJsonEncode 生成的 JSON 对象，恢复为 persistence 支持的对象图。
    /// </summary>
    /// <param name="json">JSON 对象文本。</param>
    /// <returns>字符串键字典对象图。</returns>
    public static Dictionary<string, object?> CanonicalJsonDecodeObject(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("canonical_json_root_not_object");
        }

        return ReadJsonObject(document.RootElement);
    }

    /// <summary>
    /// 计算文本的 SHA-256 校验和（小写十六进制）。
    /// </summary>
    /// <param name="data">待摘要的 UTF-8 文本。</param>
    /// <returns>64 字符小写十六进制字符串。</returns>
    public static string ComputeChecksum(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ---------------------------------------------------------------------------
    // 私有 — pipeline 核心
    // ---------------------------------------------------------------------------

    private PersistenceOperationResult CollectAndSave(PersistenceArtifactKind artifactKind)
    {
        var slot = GetSlot(artifactKind);
        var kindLabel = artifactKind == PersistenceArtifactKind.Progress ? "progress" : "settings";

        // AC-8: 检查该工件的存储能力
        if (slot.StorageCapability == StorageCapability.WriteLocked
            || slot.StorageCapability == StorageCapability.EphemeralOnly)
        {
            slot.ReasonCode = "storage_not_writable";
            SaveFailed?.Invoke("storage_not_writable", kindLabel);
            return new PersistenceOperationResult(false, "storage_not_writable", PipelinePhase, slot.CurrentGeneration);
        }

        PipelinePhase = PersistencePipelinePhase.Collecting;
        var manifest = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["generation"] = slot.CurrentGeneration + 1,
            ["artifact"] = (int)artifactKind,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["schema_version"] = 1,
            ["domains"] = new Dictionary<string, object?>(StringComparer.Ordinal),
        };

        var domains = (Dictionary<string, object?>)manifest["domains"]!;
        foreach (var (domainId, serializer) in slot.Serializers)
        {
            var snapshot = serializer();
            if (!snapshot.IsValid())
            {
                PipelinePhase = PersistencePipelinePhase.Idle;
                var failReason = $"invalid_snapshot:{domainId}";
                slot.ReasonCode = failReason;
                SaveFailed?.Invoke(failReason, "collect");
                return new PersistenceOperationResult(false, failReason, PipelinePhase, slot.CurrentGeneration);
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
        slot.CurrentGeneration = Convert.ToInt32(slot.SafeData["generation"], CultureInfo.InvariantCulture);
        slot.State = ArtifactState.Safe;
        slot.ReasonCode = string.Empty;
        slot.LastVerifiedCheckpoint = Convert.ToInt64(slot.SafeData["timestamp"], CultureInfo.InvariantCulture);
        slot.ManifestPointer = $"{kindLabel}:gen{slot.CurrentGeneration}";
        slot.CheckpointSummary = $"gen{slot.CurrentGeneration}";

        PipelinePhase = PersistencePipelinePhase.Idle;

        // SaveCompleted 仅在 progress 提升成功时触发（非干扰规则）
        if (artifactKind == PersistenceArtifactKind.Progress)
        {
            SaveCompleted?.Invoke(slot.CurrentGeneration);
        }

        PromotionCompleted?.Invoke(kindLabel, slot.CurrentGeneration);
        return new PersistenceOperationResult(true, null, PipelinePhase, slot.CurrentGeneration);
    }

    private PersistenceOperationResult LoadFromSlot(ArtifactSlot slot, string kindLabel)
    {
        if (slot.SafeData.Count == 0)
        {
            LoadFailed?.Invoke("no_safe_data", kindLabel);
            return new PersistenceOperationResult(false, "no_safe_data", PipelinePhase, slot.CurrentGeneration);
        }

        if (slot.SafeData.TryGetValue("domains", out var domainsValue)
            && domainsValue is IReadOnlyDictionary<string, object?> domains)
        {
            RestoreDomains(slot.Deserializers, domains);
        }

        LoadCompleted?.Invoke(kindLabel, slot.CurrentGeneration);
        return new PersistenceOperationResult(true, null, PipelinePhase, slot.CurrentGeneration);
    }

    // ---------------------------------------------------------------------------
    // 私有 — 工具
    // ---------------------------------------------------------------------------

    private ArtifactSlot GetSlot(PersistenceArtifactKind artifactKind)
    {
        return artifactKind == PersistenceArtifactKind.Progress ? _progressSlot : _settingsSlot;
    }

    private static ArtifactStatus BuildArtifactStatus(ArtifactSlot slot)
    {
        // 没有安全数据且状态为 Missing 时，工件不可用
        var effectiveState = slot.SafeData.Count == 0 && slot.State == ArtifactState.Missing
            ? ArtifactState.Missing
            : slot.State;

        return new ArtifactStatus(
            effectiveState,
            IntegrityOk: effectiveState == ArtifactState.Safe,
            VersionCompatible: true,
            StableIdsResolved: true,
            MigrationRequired: false);
    }

    private static void ExportSlotMetadata(
        Dictionary<string, object?> result,
        string prefix,
        ArtifactSlot slot)
    {
        result[$"{prefix}.current_generation"] = slot.CurrentGeneration;
        result[$"{prefix}.manifest_pointer"] = (object?)slot.ManifestPointer;
        result[$"{prefix}.last_verified_checkpoint"] = slot.LastVerifiedCheckpoint;
        result[$"{prefix}.checkpoint_summary"] = (object?)slot.CheckpointSummary;
        result[$"{prefix}.reason_code"] = (object?)slot.ReasonCode;
        result[$"{prefix}.backup_generation"] = slot.BackupGeneration;
    }

    private static void RestoreDomains(
        Dictionary<string, Action<SnapshotPackage>> deserializers,
        IReadOnlyDictionary<string, object?> domains)
    {
        foreach (var (domainId, deserializer) in deserializers)
        {
            if (!domains.TryGetValue(domainId, out var snapshotData)
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

    private static bool TryReadInt(
        IReadOnlyDictionary<string, object?> data,
        string key,
        out int value)
    {
        value = 0;
        if (!data.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        try
        {
            value = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private static bool TryReadLong(
        IReadOnlyDictionary<string, object?> data,
        string key,
        out long value)
    {
        value = 0;
        if (!data.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        try
        {
            value = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private static Dictionary<string, object?> ReadJsonObject(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ReadJsonValue(property.Value);
        }

        return result;
    }

    private static object? ReadJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ReadJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ReadJsonValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null,
        };
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
