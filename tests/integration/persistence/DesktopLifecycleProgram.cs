using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 008: Desktop Lifecycle Integration — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: suspend_requested with pre-encoded staging within 20ms → Completed=true", Ac1SuspendWithStagingWithinBudget);
Run("AC-1: suspend_requested without staging → Attempted=false", Ac1SuspendWithoutStaging);
Run("AC-2: window_focus_lost → no flush attempted", Ac2WindowFocusLostNoFlush);
Run("AC-2: quit_requested without staging → no flush attempted", Ac2QuitWithoutStagingNoFlush);
Run("AC-3: suspend budget exceeded → BudgetExceeded=true, PERF_SUSPEND_BUDGET_EXCEEDED", Ac3SuspendBudgetExceeded);
Run("AC-3: no staging → ReasonCode=NO_STAGING_AVAILABLE", Ac3NoStagingReasonCode);
Run("AC-4: buffered early event → converted to idempotent token", Ac4BufferedEventToToken);
Run("AC-5: resume.Persisted=true → ShouldInvalidateProbeOnResume=true", Ac5ResumePersistedInvalidatesProbe);
Run("AC-5: first resume after suspend → ShouldInvalidateProbeOnResume=true even if not persisted", Ac5FirstResumeAfterSuspendInvalidatesProbe);
Run("AC-6: WritingStaging phase → display state is 'saving_in_progress', not 'save_completed'", Ac6StagingPhaseNotSaveCompleted);
Run("AC-6: Idle + saveSucceeded=false → display state is 'idle'", Ac6IdleNotSaveCompleted);
Run("AC-6: Idle + saveSucceeded=true → display state is 'save_completed'", Ac6IdleSaveSucceeded);
Run("AC-7: EphemeralOnly mode → all 7 commit classes forbidden", Ac7EphemeralOnlyAllCommitsForbidden);
Run("AC-8: SaveLocked mode → all 7 commit classes forbidden", Ac8SaveLockedAllCommitsForbidden);
Run("AC-9: EnterTemporaryFlight from SaveLocked → EphemeralOnly + Hidden", Ac9EnterTemporaryFlightSuccess);
Run("AC-9: EnterTemporaryFlight from non-SaveLocked → throws InvalidOperationException", Ac9EnterTemporaryFlightFromNoneThrows);
Run("AC-10: diagnostic record boundary — hot-path append ≤4096 bytes allowed, >4096 rejected", Ac10DiagnosticHotPathBudget);
Run("AC-11: suspend flush at exactly 20ms → within budget (boundary)", Ac11SuspendExactlyAtBudget);
Run("AC-11: diagnostic record ≤4096 bytes → within budget; >4096 → over budget", Ac11DiagnosticRecordBudget);
Run("AC-12: hot path <60ms → no reason code; >180ms → PERF_SAVE_HOT_PATH_BUDGET_EXCEEDED", Ac12HotPathBudget);
Run("AC-12: hot path at exactly 180ms → no reason code (boundary)", Ac12HotPathExactBoundary);
Run("Regression: EvaluateSuspendFlush is a pure function — same input produces same result", RegressionEvaluateSuspendFlushIsPure);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 008 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 008 AC validation passed: {total}/{total} checks passed.");
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

static DesktopLifecycleToken SuspendToken() =>
    new(DesktopLifecycleEvent.SuspendRequested, Persisted: false, DateTimeOffset.UtcNow);

static DesktopLifecycleToken QuitToken() =>
    new(DesktopLifecycleEvent.QuitRequested, Persisted: false, DateTimeOffset.UtcNow);

static DesktopLifecycleToken FocusLostToken() =>
    new(DesktopLifecycleEvent.WindowFocusLost, Persisted: false, DateTimeOffset.UtcNow);

static DesktopLifecycleToken ResumeToken(bool persisted) =>
    new(DesktopLifecycleEvent.ResumeRequested, Persisted: persisted, DateTimeOffset.UtcNow);

// ---------------------------------------------------------------------------
// AC-1: suspend_requested with pre-encoded staging within budget
// ---------------------------------------------------------------------------

static bool Ac1SuspendWithStagingWithinBudget()
{
    var token = SuspendToken();
    var elapsed = TimeSpan.FromMilliseconds(10);

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: true, elapsed);

    return result.Attempted
        && result.Completed
        && !result.BudgetExceeded
        && result.ReasonCode == DesktopLifecyclePolicy.ReasonCodeOk;
}

// ---------------------------------------------------------------------------
// AC-1: suspend_requested without staging → Attempted=false
// ---------------------------------------------------------------------------

static bool Ac1SuspendWithoutStaging()
{
    var token = SuspendToken();

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: false, TimeSpan.Zero);

    return !result.Attempted
        && !result.Completed
        && result.ReasonCode == DesktopLifecyclePolicy.ReasonCodeNoStagingAvailable;
}

// ---------------------------------------------------------------------------
// AC-2: window_focus_lost → no flush attempted (not a suspend/quit event)
// ---------------------------------------------------------------------------

static bool Ac2WindowFocusLostNoFlush()
{
    var token = FocusLostToken();

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: true, TimeSpan.FromMilliseconds(5));

    return !result.Attempted && !result.Completed;
}

// ---------------------------------------------------------------------------
// AC-2: quit_requested without staging → no flush attempted
// ---------------------------------------------------------------------------

static bool Ac2QuitWithoutStagingNoFlush()
{
    var token = QuitToken();

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: false, TimeSpan.Zero);

    return !result.Attempted
        && result.ReasonCode == DesktopLifecyclePolicy.ReasonCodeNoStagingAvailable;
}

// ---------------------------------------------------------------------------
// AC-3: suspend budget exceeded
// ---------------------------------------------------------------------------

static bool Ac3SuspendBudgetExceeded()
{
    var token = SuspendToken();
    var elapsed = TimeSpan.FromMilliseconds(25);

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: true, elapsed);

    return result.Attempted
        && !result.Completed
        && result.BudgetExceeded
        && result.ReasonCode == DesktopLifecyclePolicy.ReasonCodeSuspendBudgetExceeded;
}

// ---------------------------------------------------------------------------
// AC-3: no staging → ReasonCode=NO_STAGING_AVAILABLE
// ---------------------------------------------------------------------------

static bool Ac3NoStagingReasonCode()
{
    var token = SuspendToken();

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: false, TimeSpan.Zero);

    return result.ReasonCode == DesktopLifecyclePolicy.ReasonCodeNoStagingAvailable
        && !result.Attempted;
}

// ---------------------------------------------------------------------------
// AC-4: buffered early event → idempotent lifecycle token
// ---------------------------------------------------------------------------

static bool Ac4BufferedEventToToken()
{
    var token = DesktopLifecyclePolicy.ProcessBufferedEvent(
        DesktopLifecycleEvent.WindowFocusLost, persisted: false);

    return token.Event == DesktopLifecycleEvent.WindowFocusLost
        && !token.Persisted
        && token.Timestamp <= DateTimeOffset.UtcNow;
}

// ---------------------------------------------------------------------------
// AC-5: resume.Persisted=true → must invalidate probe
// ---------------------------------------------------------------------------

static bool Ac5ResumePersistedInvalidatesProbe()
{
    var token = ResumeToken(persisted: true);

    return DesktopLifecyclePolicy.ShouldInvalidateProbeOnResume(token, wasEverSuspended: false);
}

// ---------------------------------------------------------------------------
// AC-5: first resume after suspend (not persisted) → must invalidate probe
// ---------------------------------------------------------------------------

static bool Ac5FirstResumeAfterSuspendInvalidatesProbe()
{
    var token = ResumeToken(persisted: false);

    return DesktopLifecyclePolicy.ShouldInvalidateProbeOnResume(token, wasEverSuspended: true);
}

// ---------------------------------------------------------------------------
// AC-6: WritingStaging phase → "saving_in_progress", not "save_completed"
// ---------------------------------------------------------------------------

static bool Ac6StagingPhaseNotSaveCompleted()
{
    var state = DesktopLifecyclePolicy.GetSaveProgressDisplayState(
        PersistencePipelinePhase.WritingStaging, saveSucceeded: false);

    return state == "saving_in_progress" && state != "save_completed";
}

// ---------------------------------------------------------------------------
// AC-6: Idle + saveSucceeded=false → "idle"
// ---------------------------------------------------------------------------

static bool Ac6IdleNotSaveCompleted()
{
    var state = DesktopLifecyclePolicy.GetSaveProgressDisplayState(
        PersistencePipelinePhase.Idle, saveSucceeded: false);

    return state == "idle";
}

// ---------------------------------------------------------------------------
// AC-6: Idle + saveSucceeded=true → "save_completed"
// ---------------------------------------------------------------------------

static bool Ac6IdleSaveSucceeded()
{
    var state = DesktopLifecyclePolicy.GetSaveProgressDisplayState(
        PersistencePipelinePhase.Idle, saveSucceeded: true);

    return state == "save_completed";
}

// ---------------------------------------------------------------------------
// AC-7: EphemeralOnly → all 7 commit classes forbidden
// ---------------------------------------------------------------------------

static bool Ac7EphemeralOnlyAllCommitsForbidden()
{
    var query = DesktopLifecyclePolicy.QueryWriteBarrier(
        WriteBarrierMode.EphemeralOnly, StorageCapability.EphemeralOnly);

    return query.BarrierActive
        && query.Mode == WriteBarrierMode.EphemeralOnly
        && query.ForbiddenCommitClasses.Count == Enum.GetValues<CommitClass>().Length
        && query.ForbiddenCommitClasses.Contains(CommitClass.WorldRepair)
        && query.ForbiddenCommitClasses.Contains(CommitClass.LongTermResource)
        && query.ForbiddenCommitClasses.Contains(CommitClass.Relationship)
        && query.ForbiddenCommitClasses.Contains(CommitClass.SettlementMarket)
        && query.ForbiddenCommitClasses.Contains(CommitClass.AirshipHomeLayout)
        && query.ForbiddenCommitClasses.Contains(CommitClass.RouteUnlock)
        && query.ForbiddenCommitClasses.Contains(CommitClass.ExplorationSettlement);
}

// ---------------------------------------------------------------------------
// AC-8: SaveLocked → all 7 commit classes forbidden
// ---------------------------------------------------------------------------

static bool Ac8SaveLockedAllCommitsForbidden()
{
    var query = DesktopLifecyclePolicy.QueryWriteBarrier(
        WriteBarrierMode.SaveLocked, StorageCapability.WriteLocked);

    return query.BarrierActive
        && query.Mode == WriteBarrierMode.SaveLocked
        && query.ForbiddenCommitClasses.Count == Enum.GetValues<CommitClass>().Length;
}

// ---------------------------------------------------------------------------
// AC-9: EnterTemporaryFlight from SaveLocked → EphemeralOnly + Hidden
// ---------------------------------------------------------------------------

static bool Ac9EnterTemporaryFlightSuccess()
{
    var (newMode, newAvailability) = DesktopLifecyclePolicy.EnterTemporaryFlight(WriteBarrierMode.SaveLocked);

    return newMode == WriteBarrierMode.EphemeralOnly
        && newAvailability == ContinueAvailability.Hidden;
}

// ---------------------------------------------------------------------------
// AC-9: EnterTemporaryFlight from non-SaveLocked → throws
// ---------------------------------------------------------------------------

static bool Ac9EnterTemporaryFlightFromNoneThrows()
{
    try
    {
        DesktopLifecyclePolicy.EnterTemporaryFlight(WriteBarrierMode.None);
        return false;
    }
    catch (InvalidOperationException)
    {
        return true;
    }
}

// ---------------------------------------------------------------------------
// AC-10: diagnostic hot-path record must be ≤4096 bytes per append
// ---------------------------------------------------------------------------

static bool Ac10DiagnosticHotPathBudget()
{
    // 合法诊断记录：结构化标量字段，不超过 4 KiB
    var smallRecord = 512;
    var maxAllowed = 4096;
    var overBudget = 4097;

    return DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(smallRecord)
        && DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(maxAllowed)
        && !DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(overBudget);
}

// ---------------------------------------------------------------------------
// AC-11: suspend flush at exactly 20ms → within budget (boundary)
// ---------------------------------------------------------------------------

static bool Ac11SuspendExactlyAtBudget()
{
    var token = SuspendToken();
    var elapsed = TimeSpan.FromMilliseconds(DesktopLifecyclePolicy.SuspendBudgetMs);

    var result = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: true, elapsed);

    // 实现使用 > 而非 >=，因此恰好 20ms 视为在预算内
    return result.Attempted
        && result.Completed
        && !result.BudgetExceeded
        && result.ReasonCode == DesktopLifecyclePolicy.ReasonCodeOk;
}

// ---------------------------------------------------------------------------
// AC-11: diagnostic record budget
// ---------------------------------------------------------------------------

static bool Ac11DiagnosticRecordBudget()
{
    return DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(4096)
        && DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(0)
        && !DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(4097)
        && !DesktopLifecyclePolicy.IsDiagnosticRecordWithinBudget(8192);
}

// ---------------------------------------------------------------------------
// AC-12: hot path budget evaluation
// ---------------------------------------------------------------------------

static bool Ac12HotPathBudget()
{
    var withinTarget = DesktopLifecyclePolicy.EvaluateSaveHotPathBudget(TimeSpan.FromMilliseconds(40));
    var withinWarning = DesktopLifecyclePolicy.EvaluateSaveHotPathBudget(TimeSpan.FromMilliseconds(100));
    var exceededWarning = DesktopLifecyclePolicy.EvaluateSaveHotPathBudget(TimeSpan.FromMilliseconds(200));

    return withinTarget == string.Empty
        && withinWarning == string.Empty
        && exceededWarning == DesktopLifecyclePolicy.ReasonCodeSaveHotPathBudgetExceeded;
}

// ---------------------------------------------------------------------------
// AC-12: hot path at exactly 180ms → no reason code (boundary, uses >)
// ---------------------------------------------------------------------------

static bool Ac12HotPathExactBoundary()
{
    var atBoundary = DesktopLifecyclePolicy.EvaluateSaveHotPathBudget(
        TimeSpan.FromMilliseconds(DesktopLifecyclePolicy.SaveHotPathWarningMs));

    // 实现使用 > 而非 >=，因此恰好 180ms 不触发警告
    return atBoundary == string.Empty;
}

// ---------------------------------------------------------------------------
// Regression: EvaluateSuspendFlush is pure (same input → same output)
// ---------------------------------------------------------------------------

static bool RegressionEvaluateSuspendFlushIsPure()
{
    var token = SuspendToken();
    var elapsed = TimeSpan.FromMilliseconds(10);

    var r1 = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: true, elapsed);
    var r2 = DesktopLifecyclePolicy.EvaluateSuspendFlush(token, hasPreEncodedStaging: true, elapsed);

    return r1.Attempted == r2.Attempted
        && r1.Completed == r2.Completed
        && r1.BudgetExceeded == r2.BudgetExceeded
        && r1.ReasonCode == r2.ReasonCode;
}
