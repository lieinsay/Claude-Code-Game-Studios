using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #13 Story 003: Repair Progress, Completion & Route Enhancement Formulas ===");

var failed = 0;
var total = 0;

Run("AC-1: progress averages per required entry", Ac1ProgressAverage);
Run("AC-2: complete deposited resources progress to 1.0", Ac2ProgressComplete);
Run("AC-3: over-deposited entries clamp to 1.0", Ac3ProgressOverDepositedClamps);
Run("AC-4: empty requirements return progress 0.0", Ac4EmptyProgress);
Run("AC-5: zero required quantity contributes satisfied", Ac5ZeroRequirementSatisfied);
Run("AC-6: completion false when one required resource is short", Ac6CompletionFalse);
Run("AC-7: completion true when all requirements met", Ac7CompletionTrue);
Run("AC-8: completion tolerates over-deposited resources", Ac8CompletionOverDeposited);
Run("AC-9: empty requirements never complete", Ac9EmptyNeverComplete);
Run("AC-10: route enhancement payload contains route, effect, magnitude, unlock", Ac10RouteEnhancementPayload);
Run("AC-11: hazard reduction applies proportional floor guard", Ac11HazardReduction);
Run("AC-12: pre-repair non-traversable route yields unlock=true", Ac12UnlockFromPreState);
Run("AC-13: submit completion pairs progress exactly with completion", Ac13SubmitProgressCompletionConsistency);
Run("AC-14: batch submit yields expected intermediate and final progress", Ac14BatchProgress);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 003 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 003 validation passed: {total}/{total} checks passed.");
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

static bool Nearly(double actual, double expected)
{
    return Math.Abs(actual - expected) < 0.000001d;
}

static bool Ac1ProgressAverage()
{
    var repair = MakeRepair();

    var progress = repair.RepairProgress(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 2,
        ["resource.basic_supply"] = 1,
    });

    return Nearly(progress, 0.375d);
}

static bool Ac2ProgressComplete()
{
    var repair = MakeRepair();

    var progress = repair.RepairProgress(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });

    return Nearly(progress, 1.0d);
}

static bool Ac3ProgressOverDepositedClamps()
{
    var repair = new WorldRepair();
    repair.RegisterRepairNode("repair_node.single", new Dictionary<string, int> { ["resource.repair_kit"] = 4 });

    return Nearly(repair.RepairProgress("repair_node.single", new Dictionary<string, int> { ["resource.repair_kit"] = 5 }), 1.0d);
}

static bool Ac4EmptyProgress()
{
    var repair = new WorldRepair();
    repair.RegisterRepairNode("repair_node.empty", new Dictionary<string, int>());

    return Nearly(repair.RepairProgress("repair_node.empty", new Dictionary<string, int>()), 0.0d);
}

static bool Ac5ZeroRequirementSatisfied()
{
    var repair = new WorldRepair();
    repair.RegisterRepairNode("repair_node.zero", new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 0,
        ["resource.basic_supply"] = 4,
    });

    return Nearly(repair.RepairProgress("repair_node.zero", new Dictionary<string, int> { ["resource.basic_supply"] = 2 }), 0.75d);
}

static bool Ac6CompletionFalse()
{
    var repair = MakeRepair();

    return !repair.RepairCompletion(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 3,
    });
}

static bool Ac7CompletionTrue()
{
    var repair = MakeRepair();

    return repair.RepairCompletion(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });
}

static bool Ac8CompletionOverDeposited()
{
    var repair = MakeRepair();

    return repair.RepairCompletion(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 5,
        ["resource.basic_supply"] = 4,
    });
}

static bool Ac9EmptyNeverComplete()
{
    var repair = new WorldRepair();
    repair.RegisterRepairNode("repair_node.empty", new Dictionary<string, int>());

    return !repair.RepairCompletion("repair_node.empty", new Dictionary<string, int>());
}

static bool Ac10RouteEnhancementPayload()
{
    var repair = MakeRepair();
    var payload = repair.GetRouteEnhancements(WorldRepair.MvpNodeId).Single();

    return payload.RouteId == "route.sky-reef-arc-01"
        && payload.EffectType == "hazard_reduction"
        && Nearly(payload.Magnitude, 0.3d)
        && payload.Unlock;
}

static bool Ac11HazardReduction()
{
    return Nearly(WorldRepair.ApplyHazardReduction(0.2d, 0.3d), 0.14d)
        && Nearly(WorldRepair.ApplyHazardReduction(0.0d, 0.3d), 0.0d);
}

static bool Ac12UnlockFromPreState()
{
    var repair = MakeRepair();

    return repair.GetRouteEnhancements(WorldRepair.MvpNodeId).Single().Unlock;
}

static bool Ac13SubmitProgressCompletionConsistency()
{
    var repair = MakeRepair();

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });

    return result.Completed
        && Nearly(result.Progress, 1.0d)
        && Nearly(repair.GetRepairProgress(WorldRepair.MvpNodeId), 1.0d);
}

static bool Ac14BatchProgress()
{
    var repair = MakeRepair();

    var first = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });
    var second = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 1,
        ["resource.basic_supply"] = 4,
    });

    return Nearly(first.Progress, 0.375d)
        && !first.Completed
        && Nearly(second.Progress, 1.0d)
        && second.Completed;
}
