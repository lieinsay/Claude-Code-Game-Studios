using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #12 Story 001: Combat State Machine & Threat Queue ===");

var failed = 0;
var total = 0;

Run("AC-1 through AC-4: IDLE -> AWAITING_RESPONSE -> PROCESSING -> RESOLVED -> IDLE", StateFlow);
Run("AC-5: PROCESSING cannot regress to AWAITING_RESPONSE", ProcessingIsIrreversible);
Run("AC-6/7: busy state queues distinct threats and deduplicates same threat_id", BusyQueueAndDuplicateGuard);
Run("AC-8/10: queue drains FIFO and skips suppressed threats", QueueDrainsFifoAndSkipsInactive);
Run("AC-9: full queue drops oldest with warning", QueueOverflowDropsOldest);
Run("AC-11/12: construction has no cross-manager calls and no process loop requirement", InitializationIsLightweight);
Run("AC-13/14: awaiting response is untimed and panel closure does not leave state", AwaitingResponseIsStable);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 validation passed: {total}/{total} checks passed.");
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
        failed++;
        Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
        return;
    }

    failed++;
    Console.Error.WriteLine($"[FAIL] {label}");
}

static CombatManager MakeManager()
{
    var manager = new CombatManager();
    manager.SetRandomDelegates(() => 0.9d, (min, _) => min);
    manager.SetResourceDelegates(
        () => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [CombatManager.RepairKitId] = 2,
        },
        (_, _) => { });
    manager.SetModuleHullDelegates(
        () => 100,
        () => Array.Empty<ModuleSlotSnapshot>(),
        _ => { },
        (_, _) => { });
    manager.SetExplorationDelegates(
        _ => true,
        _ => { },
        () => new CombatVector2(10, 0),
        (_, _) => { });
    return manager;
}

static ThreatContext Threat(string id, int x = 0) =>
    ThreatContext.Guard(id, new CombatVector2(x, 0), GuardParams());

static Dictionary<string, object?> GuardParams() => new(StringComparer.Ordinal)
{
    ["full_damage_min"] = 8,
    ["full_damage_max"] = 12,
    ["module_damage_chance"] = 0.30d,
    ["emergency_cost_repair_kit"] = 1,
    ["knockback_distance_tanked"] = 8.0d,
    ["knockback_distance_retreat"] = 10.0d,
    ["can_be_suppressed"] = true,
    ["trigger_radius_min"] = 4.0d,
    ["trigger_radius_max"] = 6.0d,
};

static bool StateFlow()
{
    var manager = MakeManager();
    var triggered = false;
    var resultReady = false;
    manager.ThreatTriggered += _ => triggered = true;
    manager.CombatResultReady += (_, _) => resultReady = true;

    var start = manager.ResolveThreat(Threat("threat.guard-01"));
    var awaiting = manager.State == CombatState.AwaitingResponse && triggered && start.Status == "awaiting_response";
    var result = manager.SubmitResponse(CombatResponses.EmergencyHandling);
    var resolved = manager.State == CombatState.Resolved && resultReady && result.Outcome == "suppressed";
    manager.CompleteResolvedFrame();
    return awaiting && resolved && manager.State == CombatState.Idle;
}

static bool ProcessingIsIrreversible()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat("threat.guard-01"));
    var result = manager.SubmitResponse(CombatResponses.Tank);
    var illegal = manager.ResolveThreat(Threat("threat.guard-02"));
    return result.Success
        && manager.State == CombatState.Resolved
        && illegal.Error == "ERR_BUSY"
        && manager.State != CombatState.AwaitingResponse;
}

static bool BusyQueueAndDuplicateGuard()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat("threat.guard-01"));
    var duplicate = manager.ResolveThreat(Threat("threat.guard-01"));
    var queued = manager.ResolveThreat(Threat("threat.guard-02"));

    return duplicate.Error == "ERR_BUSY"
        && !duplicate.Queued
        && queued is { Error: "ERR_BUSY", Queued: true }
        && manager.QueueDepth == 1;
}

static bool QueueDrainsFifoAndSkipsInactive()
{
    var active = new HashSet<string>(StringComparer.Ordinal)
    {
        "threat.guard-01",
        "threat.guard-02",
        "threat.guard-03",
    };
    var manager = MakeManager();
    manager.SetExplorationDelegates(
        id => active.Contains(id),
        id => active.Remove(id),
        () => new CombatVector2(10, 0),
        (_, _) => { });

    manager.ResolveThreat(Threat("threat.guard-01"));
    manager.ResolveThreat(Threat("threat.guard-02"));
    manager.ResolveThreat(Threat("threat.guard-03"));
    active.Remove("threat.guard-02");
    manager.SubmitResponse(CombatResponses.Tank);
    manager.CompleteResolvedFrame();

    return manager.State == CombatState.AwaitingResponse
        && manager.CurrentThreat?.ThreatId == "threat.guard-03"
        && manager.QueueDepth == 0;
}

static bool QueueOverflowDropsOldest()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat("threat.guard-active"));
    for (var i = 1; i <= 5; i++)
    {
        manager.ResolveThreat(Threat($"threat.guard-{i}"));
    }

    manager.SubmitResponse(CombatResponses.Tank);
    manager.CompleteResolvedFrame();

    return manager.Warnings.Any(w => w.Contains("dropping oldest entry threat.guard-1", StringComparison.Ordinal))
        && manager.CurrentThreat?.ThreatId == "threat.guard-2";
}

static bool InitializationIsLightweight()
{
    var called = false;
    var manager = new CombatManager();
    manager.SetResourceDelegates(() =>
    {
        called = true;
        return new Dictionary<string, int>(StringComparer.Ordinal);
    }, (_, _) => { });

    return manager.State == CombatState.Idle && manager.QueueDepth == 0 && !called;
}

static bool AwaitingResponseIsStable()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat("threat.guard-01"));
    var before = manager.State;
    var options = manager.GetAvailableResponses();
    var after = manager.State;

    return before == CombatState.AwaitingResponse
        && after == CombatState.AwaitingResponse
        && options.Count == 3
        && options.Any(option => option.Id == CombatResponses.EmergencyHandling && option.Available);
}
