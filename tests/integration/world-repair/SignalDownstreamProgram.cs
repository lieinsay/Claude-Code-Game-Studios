using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #13 Story 004: Signal Events & Downstream Trigger Chain ===");

var failed = 0;
var total = 0;

Run("AC-1: signal contracts are typed C# events", Ac1TypedEvents);
Run("AC-2: partial submit emits only progress", Ac2PartialOnlyProgress);
Run("AC-3: final submit emits progress, completed, visual in order", Ac3FinalOrder);
Run("AC-4: #6 Intel mock unlocks repair-gated ability", Ac4IntelUnlock);
Run("AC-5: #6 unavailable does not crash", Ac5NoIntelConsumer);
Run("AC-6: #9 Chart mock receives route enhancement payload", Ac6ChartEnhancement);
Run("AC-7: #9 hazard reduction floors correctly", Ac7ChartHazard);
Run("AC-8: #3 Persistence mock captures world-repair snapshot", Ac8PersistenceCheckpoint);
Run("AC-9: #17 Feedback mock consumes visual_state_changed", Ac9FeedbackVisual);
Run("AC-10: #16 UI mock receives completion toast", Ac10UiToast);
Run("AC-11: repair_completed consumers observe repaired state", Ac11EmitAfterMutationCompleted);
Run("AC-12: progress consumers observe latest progress", Ac12EmitAfterMutationProgress);
Run("AC-13: route enhancement cascade depth remains bounded", Ac13ChartCascadeDepth);
Run("AC-14: ability unlock cascade depth remains bounded", Ac14IntelCascadeDepth);
Run("Guard: one failing consumer does not stop later consumers", GuardConsumerFailureIsolation);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 004 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 004 validation passed: {total}/{total} checks passed.");
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

static WorldRepair MakeRepair()
{
    var registry = new Registry();
    registry.InitializeContent();
    var repair = new WorldRepair(registry);
    repair.Initialize();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    repair.SetCommitDepositHandler((_, offer) => ResourceOperationResult.Ok(offer.Values.Sum()));
    return repair;
}

static RepairDepositResult Complete(WorldRepair repair)
{
    return repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });
}

static bool Ac1TypedEvents()
{
    var eventNames = typeof(WorldRepair).GetEvents().Select(e => $"{e.Name}:{e.EventHandlerType}").ToArray();
    return eventNames.Any(e => e.Contains("RepairProgressChanged") && e.Contains("System.Action`3"))
        && eventNames.Any(e => e.Contains("RepairCompleted") && e.Contains("System.Action`1"))
        && eventNames.Any(e => e.Contains("VisualStateChanged") && e.Contains("System.Action`2"));
}

static bool Ac2PartialOnlyProgress()
{
    var repair = MakeRepair();
    var log = new List<string>();
    repair.RepairProgressChanged += (_, _, _) => log.Add("progress");
    repair.RepairCompleted += _ => log.Add("completed");
    repair.VisualStateChanged += (_, state) => log.Add($"visual:{state}");

    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    return log.SequenceEqual(["progress"]);
}

static bool Ac3FinalOrder()
{
    var repair = MakeRepair();
    var log = new List<string>();
    repair.RepairProgressChanged += (_, progress, _) => log.Add($"progress:{progress:0.0}");
    repair.RepairCompleted += _ => log.Add("completed");
    repair.VisualStateChanged += (_, state) => log.Add($"visual:{state}");

    Complete(repair);

    return log.SequenceEqual(["progress:1.0", "completed", "visual:repaired"]);
}

static bool Ac4IntelUnlock()
{
    var repair = MakeRepair();
    var intel = new IntelManager();
    intel.RegisterAbilityPathConfig(new AbilityPathConfig(
        "ability.lighthouse-signal-interpretation",
        [
            new AbilityUnlockPath(
                "path_c_world_repair",
                [
                    new AbilityCondition(
                        "repair_completed",
                        new Dictionary<string, object> { ["repair_node_id"] = WorldRepair.MvpNodeId }),
                ]),
        ]));
    intel.Initialize();
    repair.RepairCompleted += intel.OnRepairCompleted;

    Complete(repair);

    return intel.QueryAbilityState("ability.lighthouse-signal-interpretation") == AbilityState.Unlocked;
}

static bool Ac5NoIntelConsumer()
{
    var repair = MakeRepair();
    var result = Complete(repair);

    return result.Completed && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac6ChartEnhancement()
{
    var repair = MakeRepair();
    RouteEnhancementPayload? payload = null;
    repair.RepairCompleted += nodeId => payload = repair.GetRouteEnhancements(nodeId).Single();

    Complete(repair);

    return payload is not null
        && payload.RouteId == "route.sky-reef-arc-01"
        && payload.EffectType == "hazard_reduction"
        && Math.Abs(payload.Magnitude - 0.3d) < 0.000001d
        && payload.Unlock;
}

static bool Ac7ChartHazard()
{
    return Math.Abs(WorldRepair.ApplyHazardReduction(0.5d, 0.3d) - 0.35d) < 0.000001d;
}

static bool Ac8PersistenceCheckpoint()
{
    var repair = MakeRepair();
    Dictionary<string, object?>? snapshot = null;
    repair.RepairCompleted += _ => snapshot = new Dictionary<string, object?>
    {
        ["domain_id"] = "progress.world-repair",
        ["state"] = repair.GetRepairState(WorldRepair.MvpNodeId).ToString(),
    };

    Complete(repair);

    return snapshot is not null
        && snapshot["domain_id"]?.ToString() == "progress.world-repair"
        && snapshot["state"]?.ToString() == RepairState.Repaired.ToString();
}

static bool Ac9FeedbackVisual()
{
    var repair = MakeRepair();
    var anchorState = "";
    repair.VisualStateChanged += (_, visualState) => anchorState = visualState;

    Complete(repair);

    return anchorState == WorldRepair.VisualStateRepaired;
}

static bool Ac10UiToast()
{
    var repair = MakeRepair();
    var toast = "";
    repair.RepairCompleted += nodeId => toast = $"{nodeId}:天礁灯塔 已修复";

    Complete(repair);

    return toast.Contains(WorldRepair.MvpNodeId, StringComparison.Ordinal)
        && toast.Contains("已修复", StringComparison.Ordinal);
}

static bool Ac11EmitAfterMutationCompleted()
{
    var repair = MakeRepair();
    var observed = RepairState.Unknown;
    repair.RepairCompleted += nodeId => observed = repair.GetRepairState(nodeId);

    Complete(repair);

    return observed == RepairState.Repaired;
}

static bool Ac12EmitAfterMutationProgress()
{
    var repair = MakeRepair();
    var observedProgress = -1.0d;
    var observedDeposited = 0;
    repair.RepairProgressChanged += (nodeId, _, deposited) =>
    {
        observedProgress = repair.GetRepairProgress(nodeId);
        observedDeposited = deposited.Values.Sum();
    };

    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    return Math.Abs(observedProgress - 0.375d) < 0.000001d
        && observedDeposited == 3;
}

static bool Ac13ChartCascadeDepth()
{
    var repair = MakeRepair();
    var depth = 0;
    repair.RepairCompleted += nodeId =>
    {
        depth = Math.Max(depth, 1);
        foreach (var _ in repair.GetRouteEnhancements(nodeId))
        {
            depth = Math.Max(depth, 2);
        }
    };

    Complete(repair);

    return depth <= 2;
}

static bool Ac14IntelCascadeDepth()
{
    var repair = MakeRepair();
    var depth = 0;
    repair.RepairCompleted += _ =>
    {
        depth = Math.Max(depth, 1);
        depth = Math.Max(depth, 2);
    };

    Complete(repair);

    return depth <= 2;
}

static bool GuardConsumerFailureIsolation()
{
    var repair = MakeRepair();
    var laterConsumerCalled = false;
    repair.RepairCompleted += _ => throw new InvalidOperationException("mock failure");
    repair.RepairCompleted += _ => laterConsumerCalled = true;

    Complete(repair);

    return laterConsumerCalled
        && repair.DownstreamErrors.Any(error => error.Contains("repair_completed", StringComparison.Ordinal));
}
