using System;
using System.Collections.Generic;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 备份故障转移的顶层判定结果。
/// </summary>
public enum BackupFailoverOutcome
{
    /// <summary>主工件已经可用，不需要备份提升。</summary>
    NotNeeded = 0,

    /// <summary>主工件不可用，备份可直接提升为新的 Safe 工件。</summary>
    BackupPromoted = 1,

    /// <summary>备份可信但不能直接恢复，例如需要迁移或稳定 ID 不能直接解析。</summary>
    BackupPreservedLocked = 2,

    /// <summary>没有可用备份，Continue 不能变为 Enabled。</summary>
    NoUsableBackup = 3,
}

/// <summary>
/// 备份提升流程当前阶段。
/// </summary>
public enum BackupPromotionPhase
{
    /// <summary>尚未执行备份提升。</summary>
    Idle = 0,

    /// <summary>正在把备份提升到新的暂存工件。</summary>
    BackupPromoting = 1,

    /// <summary>正在读回并验证提升后的暂存工件。</summary>
    Verifying = 2,

    /// <summary>备份已经成为新的 Safe 工件。</summary>
    Safe = 3,

    /// <summary>提升失败，外部 Continue 不得进入 Enabled。</summary>
    Failed = 4,
}

/// <summary>
/// 单个存档工件在恢复前检查中得到的可恢复性探针。
/// </summary>
/// <param name="Present">工件是否存在。</param>
/// <param name="ParseOk">JSON 或 manifest 是否可解析。</param>
/// <param name="StructureOk">工件结构是否满足必要字段与布局。</param>
/// <param name="IntegrityOk">checksum 与读回校验是否通过。</param>
/// <param name="VersionCompatible">保存版本是否能被当前构建直接读取。</param>
/// <param name="StableIdsResolved">稳定 ID 引用是否都能直接解析。</param>
/// <param name="MigrationRequired">该工件是否必须先迁移才能恢复。</param>
/// <param name="Generation">工件记录的 generation。</param>
/// <param name="ArtifactKind">工件类型。</param>
public sealed record SaveArtifactProbe(
    bool Present,
    bool ParseOk,
    bool StructureOk,
    bool IntegrityOk,
    bool VersionCompatible,
    bool StableIdsResolved,
    bool MigrationRequired,
    int Generation = 0,
    PersistenceArtifactKind ArtifactKind = PersistenceArtifactKind.Progress)
{
    /// <summary>
    /// 主工件是否通过基础恢复检查；迁移锁定由 Continue 查询继续判定。
    /// </summary>
    public bool MainUsable =>
        Present
        && ParseOk
        && StructureOk
        && IntegrityOk
        && VersionCompatible
        && StableIdsResolved;

    /// <summary>
    /// 备份是否满足直接提升条件。
    /// </summary>
    public bool DirectRestoreOk => MainUsable && !MigrationRequired;

    /// <summary>
    /// 备份是否可信但需要锁定保留，而不是直接提升。
    /// </summary>
    public bool TrustedButLocked =>
        Present
        && ParseOk
        && StructureOk
        && IntegrityOk
        && (MigrationRequired || !VersionCompatible || !StableIdsResolved);

    /// <summary>
    /// 转换为 Continue 查询使用的工件状态。
    /// </summary>
    /// <param name="state">覆盖后的工件状态。</param>
    /// <returns>用于 <see cref="ContinueStateQuery"/> 的工件状态。</returns>
    public ArtifactStatus ToArtifactStatus(ArtifactState state)
    {
        return new ArtifactStatus(
            state,
            IntegrityOk,
            VersionCompatible,
            StableIdsResolved,
            MigrationRequired);
    }
}

/// <summary>
/// 备份提升每个 I/O 阶段的执行结果。
/// </summary>
/// <param name="ValidateBackup">备份验证阶段是否成功。</param>
/// <param name="WritePromotedStaging">写入 promoted staging / new generation 是否成功。</param>
/// <param name="ReadbackVerify">读回验证是否成功。</param>
/// <param name="PromoteToSafe">切换 current pointer 是否成功。</param>
/// <param name="QuarantineOriginalMain">旧主工件进入 Quarantined 是否成功。</param>
public sealed record BackupPromotionStepResults(
    bool ValidateBackup = true,
    bool WritePromotedStaging = true,
    bool ReadbackVerify = true,
    bool PromoteToSafe = true,
    bool QuarantineOriginalMain = true);

/// <summary>
/// 备份提升执行后的完整结果。
/// </summary>
/// <param name="Outcome">备份故障转移判定结果。</param>
/// <param name="Success">提升是否成功完成。</param>
/// <param name="Phase">最终流程阶段。</param>
/// <param name="StepOrder">已执行步骤，按实际顺序记录。</param>
/// <param name="OldMainState">旧主工件最终状态。</param>
/// <param name="BackupRetained">备份副本是否仍被保留。</param>
/// <param name="ContinueState">重新计算后的 Continue 状态。</param>
/// <param name="CheckpointSummary">玩家可见的安全摘要；成功从备份恢复时使用中文提示。</param>
/// <param name="PromotedGeneration">提升成功后新的 generation；失败时为 0。</param>
/// <param name="FailureReason">失败阶段的原因码；成功时为空。</param>
public sealed record BackupPromotionExecutionResult(
    BackupFailoverOutcome Outcome,
    bool Success,
    BackupPromotionPhase Phase,
    IReadOnlyList<string> StepOrder,
    ArtifactState OldMainState,
    bool BackupRetained,
    ContinueStateResult ContinueState,
    string CheckpointSummary,
    int PromotedGeneration,
    string FailureReason);

/// <summary>
/// 备份故障转移的纯逻辑策略，负责判定、步骤顺序和 Continue 重新计算。
/// </summary>
public static class BackupFailoverPolicy
{
    /// <summary>备份成功恢复后写入 checkpoint_summary 的玩家文案。</summary>
    public const string RecoveryCheckpointSummary = "已恢复到最近可用记录";

    /// <summary>备份验证步骤名称。</summary>
    public const string StepValidateBackup = "validate_backup";

    /// <summary>写入 promoted staging / new generation 步骤名称。</summary>
    public const string StepWritePromotedStaging = "copy_to_staging_as_new_generation";

    /// <summary>读回验证步骤名称。</summary>
    public const string StepReadbackVerify = "readback_verify";

    /// <summary>切换 current pointer 步骤名称。</summary>
    public const string StepPromoteToSafe = "promote_to_safe";

    /// <summary>隔离旧主工件步骤名称。</summary>
    public const string StepQuarantineOriginalMain = "quarantine_original_main";

    /// <summary>
    /// 根据主工件与备份工件探针计算备份故障转移结果。
    /// </summary>
    /// <param name="main">主继续点工件探针。</param>
    /// <param name="backup">自动备份工件探针。</param>
    /// <returns>故障转移判定结果。</returns>
    public static BackupFailoverOutcome EvaluateFailover(SaveArtifactProbe main, SaveArtifactProbe backup)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(backup);

        if (main.MainUsable)
        {
            return BackupFailoverOutcome.NotNeeded;
        }

        if (backup.DirectRestoreOk)
        {
            return BackupFailoverOutcome.BackupPromoted;
        }

        if (backup.TrustedButLocked)
        {
            return BackupFailoverOutcome.BackupPreservedLocked;
        }

        return BackupFailoverOutcome.NoUsableBackup;
    }

    /// <summary>
    /// 执行备份提升模型，并返回步骤顺序、旧主工件状态和重新计算后的 Continue 状态。
    /// </summary>
    /// <param name="main">主继续点工件探针。</param>
    /// <param name="backup">自动备份工件探针。</param>
    /// <param name="steps">每个提升阶段的执行结果；为空时全部成功。</param>
    /// <param name="storageCapability">当前持久化能力。</param>
    /// <returns>备份提升执行结果。</returns>
    public static BackupPromotionExecutionResult ExecutePromotion(
        SaveArtifactProbe main,
        SaveArtifactProbe backup,
        BackupPromotionStepResults? steps = null,
        StorageCapability storageCapability = StorageCapability.PersistentAvailable)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(backup);

        steps ??= new BackupPromotionStepResults();
        var outcome = EvaluateFailover(main, backup);

        return outcome switch
        {
            BackupFailoverOutcome.NotNeeded => BuildNotNeededResult(main, storageCapability),
            BackupFailoverOutcome.BackupPreservedLocked => BuildPreservedLockedResult(main, backup, storageCapability),
            BackupFailoverOutcome.NoUsableBackup => BuildNoUsableBackupResult(main, backup, storageCapability),
            _ => ExecutePromotableBackup(main, backup, steps, storageCapability),
        };
    }

    private static BackupPromotionExecutionResult ExecutePromotableBackup(
        SaveArtifactProbe main,
        SaveArtifactProbe backup,
        BackupPromotionStepResults steps,
        StorageCapability storageCapability)
    {
        var executed = new List<string>(5);
        var promotedGeneration = Math.Max(main.Generation, backup.Generation) + 1;

        executed.Add(StepValidateBackup);
        if (!steps.ValidateBackup || !backup.DirectRestoreOk)
        {
            return BuildPromotionFailure(main, backup, storageCapability, executed, "validate_backup_failed");
        }

        executed.Add(StepWritePromotedStaging);
        if (!steps.WritePromotedStaging)
        {
            return BuildPromotionFailure(main, backup, storageCapability, executed, "write_promoted_staging_failed");
        }

        executed.Add(StepReadbackVerify);
        if (!steps.ReadbackVerify)
        {
            return BuildPromotionFailure(main, backup, storageCapability, executed, "readback_verify_failed");
        }

        executed.Add(StepPromoteToSafe);
        if (!steps.PromoteToSafe)
        {
            return BuildPromotionFailure(main, backup, storageCapability, executed, "promote_to_safe_failed");
        }

        executed.Add(StepQuarantineOriginalMain);
        if (!steps.QuarantineOriginalMain)
        {
            return BuildPromotionFailure(main, backup, storageCapability, executed, "quarantine_original_main_failed");
        }

        var continueState = ContinueStateQuery.QueryContinueState(
            storageCapability,
            new ArtifactStatus(ArtifactState.Safe, true, true, true, false),
            settingsStatus: null,
            promotedGeneration);

        return new BackupPromotionExecutionResult(
            BackupFailoverOutcome.BackupPromoted,
            Success: true,
            BackupPromotionPhase.Safe,
            executed.AsReadOnly(),
            ArtifactState.Quarantined,
            BackupRetained: true,
            continueState,
            RecoveryCheckpointSummary,
            promotedGeneration,
            FailureReason: string.Empty);
    }

    private static BackupPromotionExecutionResult BuildNotNeededResult(
        SaveArtifactProbe main,
        StorageCapability storageCapability)
    {
        var continueState = ContinueStateQuery.QueryContinueState(
            storageCapability,
            main.ToArtifactStatus(ArtifactState.Safe),
            settingsStatus: null,
            main.Generation);

        return new BackupPromotionExecutionResult(
            BackupFailoverOutcome.NotNeeded,
            Success: false,
            BackupPromotionPhase.Idle,
            Array.Empty<string>(),
            ArtifactState.Safe,
            BackupRetained: false,
            continueState,
            CheckpointSummary: string.Empty,
            PromotedGeneration: 0,
            FailureReason: string.Empty);
    }

    private static BackupPromotionExecutionResult BuildPreservedLockedResult(
        SaveArtifactProbe main,
        SaveArtifactProbe backup,
        StorageCapability storageCapability)
    {
        var continueState = ContinueStateQuery.QueryContinueState(
            storageCapability,
            backup.ToArtifactStatus(ArtifactState.Safe),
            settingsStatus: null,
            Math.Max(main.Generation, backup.Generation));

        return new BackupPromotionExecutionResult(
            BackupFailoverOutcome.BackupPreservedLocked,
            Success: false,
            BackupPromotionPhase.Failed,
            Array.Empty<string>(),
            main.Present ? ArtifactState.Quarantined : ArtifactState.Missing,
            BackupRetained: backup.Present,
            continueState,
            CheckpointSummary: string.Empty,
            PromotedGeneration: 0,
            FailureReason: continueState.ReasonCode);
    }

    private static BackupPromotionExecutionResult BuildNoUsableBackupResult(
        SaveArtifactProbe main,
        SaveArtifactProbe backup,
        StorageCapability storageCapability)
    {
        var mainState = main.Present ? ArtifactState.Quarantined : ArtifactState.Missing;
        var continueState = ContinueStateQuery.QueryContinueState(
            storageCapability,
            main.ToArtifactStatus(mainState),
            settingsStatus: null,
            main.Generation);

        return new BackupPromotionExecutionResult(
            BackupFailoverOutcome.NoUsableBackup,
            Success: false,
            BackupPromotionPhase.Failed,
            Array.Empty<string>(),
            mainState,
            BackupRetained: backup.Present,
            continueState,
            CheckpointSummary: string.Empty,
            PromotedGeneration: 0,
            FailureReason: continueState.ReasonCode);
    }

    private static BackupPromotionExecutionResult BuildPromotionFailure(
        SaveArtifactProbe main,
        SaveArtifactProbe backup,
        StorageCapability storageCapability,
        IReadOnlyList<string> executed,
        string reason)
    {
        var continueState = ContinueStateQuery.QueryContinueState(
            storageCapability,
            main.ToArtifactStatus(main.Present ? ArtifactState.Quarantined : ArtifactState.Missing),
            settingsStatus: null,
            main.Generation);

        return new BackupPromotionExecutionResult(
            BackupFailoverOutcome.BackupPromoted,
            Success: false,
            BackupPromotionPhase.Failed,
            executed,
            main.Present ? ArtifactState.Quarantined : ArtifactState.Missing,
            BackupRetained: backup.Present,
            continueState,
            CheckpointSummary: string.Empty,
            PromotedGeneration: 0,
            FailureReason: reason);
    }
}
