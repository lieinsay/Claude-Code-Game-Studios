using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// 桌面生命周期事件类型，由 SessionShell 从 Godot 窗口通知归一化后传入。
/// </summary>
public enum DesktopLifecycleEvent
{
    /// <summary>窗口获得焦点（NotificationWmWindowFocusIn）。</summary>
    WindowFocusGained = 0,

    /// <summary>窗口失去焦点或最小化（NotificationWmWindowFocusOut）。</summary>
    WindowFocusLost = 1,

    /// <summary>操作系统挂起或最小化请求（SessionShell 归一化后的语义 token）。</summary>
    SuspendRequested = 2,

    /// <summary>从挂起或最小化恢复。</summary>
    ResumeRequested = 3,

    /// <summary>应用关闭请求（NotificationWmCloseRequest）。</summary>
    QuitRequested = 4,
}

/// <summary>
/// 生命周期事件的幂等 token，由 SessionShell 创建后传给存档系统。
/// </summary>
/// <param name="Event">事件类型。</param>
/// <param name="Persisted">true 表示从真实挂起状态恢复，probe TTL 必须失效并重探。</param>
/// <param name="Timestamp">事件到达时间戳。</param>
public sealed record DesktopLifecycleToken(
    DesktopLifecycleEvent Event,
    bool Persisted,
    DateTimeOffset Timestamp);

/// <summary>
/// 正式 commit class，代表在 EphemeralOnly 或 SaveLocked 模式下必须被拒绝的操作类别。
/// </summary>
public enum CommitClass
{
    /// <summary>修复灯塔/节点等世界修复操作。</summary>
    WorldRepair = 0,

    /// <summary>长期资源积累（非临时货物）。</summary>
    LongTermResource = 1,

    /// <summary>关系/村镇状态变化。</summary>
    Relationship = 2,

    /// <summary>集市交易库存落定。</summary>
    SettlementMarket = 3,

    /// <summary>飞艇家园布置或模块安装。</summary>
    AirshipHomeLayout = 4,

    /// <summary>航线解锁。</summary>
    RouteUnlock = 5,

    /// <summary>探索/搜撤结算。</summary>
    ExplorationSettlement = 6,
}

/// <summary>
/// suspend_requested best-effort flush 的执行结果。
/// </summary>
/// <param name="Attempted">是否尝试了 flush（false = 无预编码 staging，直接跳过）。</param>
/// <param name="Completed">flush 是否在预算内完成。</param>
/// <param name="BudgetExceeded">是否因超出 20ms 预算而放弃。</param>
/// <param name="ReasonCode">原因码；成功时为 OK，跳过时为 NO_STAGING_AVAILABLE，超时时为 PERF_SUSPEND_BUDGET_EXCEEDED。</param>
/// <param name="Elapsed">本次 flush 实际耗时。</param>
public sealed record SuspendFlushResult(
    bool Attempted,
    bool Completed,
    bool BudgetExceeded,
    string ReasonCode,
    TimeSpan Elapsed);

/// <summary>
/// 写屏障查询结果，包含当前禁止的正式 commit class 列表。
/// </summary>
/// <param name="BarrierActive">是否存在活跃写屏障。</param>
/// <param name="Mode">当前写屏障模式。</param>
/// <param name="ReasonCode">原因码；无屏障时为空。</param>
/// <param name="ForbiddenCommitClasses">当前被禁止的 commit class 列表；无屏障时为空列表。</param>
public sealed record WriteBarrierQuery(
    bool BarrierActive,
    WriteBarrierMode Mode,
    string ReasonCode,
    IReadOnlyList<CommitClass> ForbiddenCommitClasses);

/// <summary>
/// 桌面生命周期与写屏障的纯逻辑策略。
/// 所有方法均为无副作用纯函数；不依赖 Godot 引擎。
/// </summary>
public static class DesktopLifecyclePolicy
{
    /// <summary>suspend_requested best-effort flush 的最大预算（毫秒）。</summary>
    public const int SuspendBudgetMs = 20;

    /// <summary>suspend flush 在预算内成功完成时的原因码。</summary>
    public const string ReasonCodeOk = "OK";

    /// <summary>suspend flush 超出预算时写入诊断记录的原因码。</summary>
    public const string ReasonCodeSuspendBudgetExceeded = "PERF_SUSPEND_BUDGET_EXCEEDED";

    /// <summary>无预编码 staging 可用时写入诊断记录的原因码。</summary>
    public const string ReasonCodeNoStagingAvailable = "NO_STAGING_AVAILABLE";

    /// <summary>保存热路径超出 180ms 警告阈值时写入诊断记录的原因码。</summary>
    public const string ReasonCodeSaveHotPathBudgetExceeded = "PERF_SAVE_HOT_PATH_BUDGET_EXCEEDED";

    /// <summary>保存热路径目标耗时（毫秒）。超出此值应记录 advisory 日志。</summary>
    public const int SaveHotPathTargetMs = 60;

    /// <summary>保存热路径警告阈值（毫秒）。超出此值必须写入 PERF 诊断原因码。</summary>
    public const int SaveHotPathWarningMs = 180;

    private static readonly IReadOnlyList<CommitClass> AllCommitClasses =
        Enum.GetValues<CommitClass>().ToList().AsReadOnly();

    private static readonly IReadOnlyList<CommitClass> NoCommitClasses =
        Array.Empty<CommitClass>();

    /// <summary>
    /// 评估 suspend_requested / quit_requested 事件下是否执行 best-effort flush，以及结果。
    /// 此方法不包含任何序列化、checksum、readback 或迁移逻辑；只判断是否在预算内完成了 flush marker 写入。
    /// </summary>
    /// <param name="token">生命周期 token。</param>
    /// <param name="hasPreEncodedStaging">是否存在已预编码的 staging marker 可供轻量 flush。</param>
    /// <param name="elapsed">本次 flush 实际耗时（由调用方计时后传入）。</param>
    /// <returns>flush 执行结果。</returns>
    public static SuspendFlushResult EvaluateSuspendFlush(
        DesktopLifecycleToken token,
        bool hasPreEncodedStaging,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(token);

        // AC-2/3: 非 suspend / quit 事件不启动 flush
        if (token.Event != DesktopLifecycleEvent.SuspendRequested
            && token.Event != DesktopLifecycleEvent.QuitRequested)
        {
            return new SuspendFlushResult(
                Attempted: false,
                Completed: false,
                BudgetExceeded: false,
                ReasonCode: ReasonCodeNoStagingAvailable,
                Elapsed: elapsed);
        }

        // AC-1/3: 无预编码 staging → 直接放弃，不尝试 flush
        if (!hasPreEncodedStaging)
        {
            return new SuspendFlushResult(
                Attempted: false,
                Completed: false,
                BudgetExceeded: false,
                ReasonCode: ReasonCodeNoStagingAvailable,
                Elapsed: elapsed);
        }

        // AC-3/11: 有 staging 但超出 20ms 预算 → 放弃并记录原因码
        if (elapsed > TimeSpan.FromMilliseconds(SuspendBudgetMs))
        {
            return new SuspendFlushResult(
                Attempted: true,
                Completed: false,
                BudgetExceeded: true,
                ReasonCode: ReasonCodeSuspendBudgetExceeded,
                Elapsed: elapsed);
        }

        // AC-1: 有 staging 且在预算内 → flush 成功
        return new SuspendFlushResult(
            Attempted: true,
            Completed: true,
            BudgetExceeded: false,
            ReasonCode: ReasonCodeOk,
            Elapsed: elapsed);
    }

    /// <summary>
    /// 将 Godot 启动前缓冲的早期生命周期事件转换为幂等 lifecycle token。
    /// 相同 Event 类型的重复调用产生独立 token，由调用方负责去重（取最近一次）。
    /// </summary>
    /// <param name="bufferedEvent">缓冲的事件类型。</param>
    /// <param name="persisted">是否为真实挂起后的恢复；影响 probe TTL 失效判定。</param>
    /// <returns>新建的 lifecycle token，时间戳为当前 UTC 时间。</returns>
    public static DesktopLifecycleToken ProcessBufferedEvent(
        DesktopLifecycleEvent bufferedEvent,
        bool persisted = false)
    {
        return new DesktopLifecycleToken(bufferedEvent, persisted, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 判断 resume_requested 事件是否需要使当前 capability probe 失效并重探。
    /// 以下两种情况必须失效：token.Persisted=true，或为挂起后第一次 resume（无论 Persisted 值）。
    /// </summary>
    /// <param name="token">resume 生命周期 token。</param>
    /// <param name="wasEverSuspended">本会话中是否曾经进入过 suspend 状态。</param>
    /// <returns>true 表示必须使 probe 失效并重探。</returns>
    public static bool ShouldInvalidateProbeOnResume(
        DesktopLifecycleToken token,
        bool wasEverSuspended)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.Event != DesktopLifecycleEvent.ResumeRequested)
        {
            return false;
        }

        // AC-5: persisted=true（真实 BFCache / OS 级挂起恢复）→ 必须失效
        if (token.Persisted)
        {
            return true;
        }

        // AC-5: 任意 suspend 后第一次 resume → 必须失效，即使 TTL 未过期
        return wasEverSuspended;
    }

    /// <summary>
    /// 根据当前存档流水线阶段，返回 UI 层允许展示的最高状态字符串。
    /// 只有 Idle 且 saveSucceeded=true 时才允许显示"保存成功"；否则绝不返回"save_completed"。
    /// </summary>
    /// <param name="phase">当前存档流水线阶段。</param>
    /// <param name="saveSucceeded">调用方通过 SaveCompleted 事件确认本次保存已成功完成。</param>
    /// <returns>UI 层可展示的状态字符串。</returns>
    public static string GetSaveProgressDisplayState(
        PersistencePipelinePhase phase,
        bool saveSucceeded = false)
    {
        return phase switch
        {
            PersistencePipelinePhase.Idle when saveSucceeded => "save_completed",
            PersistencePipelinePhase.Idle => "idle",
            PersistencePipelinePhase.Collecting
                or PersistencePipelinePhase.WritingStaging
                or PersistencePipelinePhase.Verifying
                or PersistencePipelinePhase.Promoting => "saving_in_progress",
            PersistencePipelinePhase.Aborting => "save_failed",
            _ => "idle",
        };
    }

    /// <summary>
    /// 查询当前写屏障状态及被禁止的正式 commit class 列表。
    /// EphemeralOnly 或 SaveLocked 模式下所有 7 类正式 commit 均被禁止。
    /// </summary>
    /// <param name="mode">当前写屏障模式。</param>
    /// <param name="capability">当前存储能力。</param>
    /// <returns>写屏障查询结果。</returns>
    public static WriteBarrierQuery QueryWriteBarrier(
        WriteBarrierMode mode,
        StorageCapability capability)
    {
        // capability=EphemeralOnly 优先级最高，等同于 EphemeralOnly 模式
        var effectiveMode = capability == StorageCapability.EphemeralOnly
            ? WriteBarrierMode.EphemeralOnly
            : mode;

        return effectiveMode switch
        {
            WriteBarrierMode.EphemeralOnly => new WriteBarrierQuery(
                BarrierActive: true,
                Mode: WriteBarrierMode.EphemeralOnly,
                ReasonCode: "storage_ephemeral_only",
                ForbiddenCommitClasses: AllCommitClasses),

            WriteBarrierMode.SaveLocked => new WriteBarrierQuery(
                BarrierActive: true,
                Mode: WriteBarrierMode.SaveLocked,
                ReasonCode: "storage_save_locked",
                ForbiddenCommitClasses: AllCommitClasses),

            _ => new WriteBarrierQuery(
                BarrierActive: false,
                Mode: WriteBarrierMode.None,
                ReasonCode: string.Empty,
                ForbiddenCommitClasses: NoCommitClasses),
        };
    }

    /// <summary>
    /// 执行"进入临时试航"模式转换。
    /// 前提：当前必须是 SaveLocked 模式（玩家已经过二次确认）。
    /// 转换成功后 mode=EphemeralOnly，continue_availability=Hidden，禁止所有正式 commit。
    /// </summary>
    /// <param name="currentMode">当前写屏障模式。</param>
    /// <returns>新的写屏障模式和 Continue 可用性。</returns>
    /// <exception cref="InvalidOperationException">当前模式不是 SaveLocked 时抛出。</exception>
    public static (WriteBarrierMode NewMode, ContinueAvailability NewAvailability) EnterTemporaryFlight(
        WriteBarrierMode currentMode)
    {
        if (currentMode != WriteBarrierMode.SaveLocked)
        {
            throw new InvalidOperationException(
                $"enter_temporary_flight requires SaveLocked mode, but current mode is {currentMode}.");
        }

        return (WriteBarrierMode.EphemeralOnly, ContinueAvailability.Hidden);
    }

    /// <summary>
    /// 检查诊断记录字节数是否在 4 KiB 预算内。
    /// 保存热路径每次追加的诊断记录不得超过 4096 字节。
    /// </summary>
    /// <param name="recordBytes">本次诊断记录的字节数。</param>
    /// <returns>true = 在预算内；false = 超出预算。</returns>
    public static bool IsDiagnosticRecordWithinBudget(int recordBytes)
    {
        return recordBytes <= 4096;
    }

    /// <summary>
    /// 评估保存热路径耗时，返回诊断原因码。
    /// 超过 SaveHotPathWarningMs（180ms）时返回 PERF_SAVE_HOT_PATH_BUDGET_EXCEEDED。
    /// 在 SaveHotPathTargetMs（60ms）内时返回空字符串（无需记录）。
    /// </summary>
    /// <param name="elapsed">热路径实际耗时。</param>
    /// <returns>原因码；在预算内时为空字符串。</returns>
    public static string EvaluateSaveHotPathBudget(TimeSpan elapsed)
    {
        if (elapsed > TimeSpan.FromMilliseconds(SaveHotPathWarningMs))
        {
            return ReasonCodeSaveHotPathBudgetExceeded;
        }

        return string.Empty;
    }
}
