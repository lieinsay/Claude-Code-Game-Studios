using System.Diagnostics;
using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #14 Story 004: Repair Signal & Resources Integration ===");

var failed = 0;
var total = 0;

Run("AC-1/2/3: WorldRepair repair_completed is consumed without blocking and emits settlement signals", RepairSignalWiring);
Run("AC-4/5/6: use_requested opens only known open stalls", UseRequestedBoundary);
Run("AC-7/8/9: purchases delegate to #5 and fail safely when unavailable", ResourcesDelegation);
Run("AC-10/11: interactive stall query excludes closed stalls and includes newly opened stalls", InteractiveStalls);
Run("AC-12: all six settlement signal contracts are exposed as typed events", SignalContractExposed);
Run("AC-13: feature_ready connects, initializes/restores, and registers open stalls in order", FeatureReadySequence);

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

static (Registry Registry, ResourcesManager Resources, SettlementManager Settlement, WorldRepair Repair) MakeFixture(int currency = 500)
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    resources.Add(ResourcePool.InStorage, "resource.cloud_coin", currency);
    var settlement = new SettlementManager(registry);
    settlement.SetResourcesManager(resources);
    settlement.InitNewGameState();
    var repair = new WorldRepair(registry);
    repair.SetCommitDepositHandler((_, _) => ResourceOperationResult.Ok(1));
    repair.Initialize();
    return (registry, resources, settlement, repair);
}

static bool RepairSignalWiring()
{
    var (_, _, settlement, repair) = MakeFixture();
    var opened = false;
    var activity = false;
    settlement.StallOpened += (stallId, _) => opened = stallId == "stall.gh-sail-shop";
    settlement.SettlementActivityChanged += (_, activeCount) => activity = activeCount == 2;
    settlement.ConnectSignals(repair);

    var stopwatch = Stopwatch.StartNew();
    repair.OnPlayerArrivedAtRepairNode("repair_node.starlight_dock");
    var result = repair.SubmitDeposit("repair_node.starlight_dock", new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });
    stopwatch.Stop();

    return result.Completed
        && opened
        && activity
        && settlement.GetStallState("stall.gh-sail-shop") == StallState.OpenBasic
        && stopwatch.Elapsed.TotalMilliseconds < 100;
}

static bool UseRequestedBoundary()
{
    var (_, _, settlement, _) = MakeFixture();
    var opened = new List<string>();
    settlement.StallOpened += (stallId, _) => opened.Add(stallId);

    settlement.OnUseRequested("stall.gh-general");
    settlement.OnUseRequested("stall.gh-lens-workshop");
    settlement.OnUseRequested("unknown.target");

    return opened.SequenceEqual(["stall.gh-general"]);
}

static bool ResourcesDelegation()
{
    var (_, resources, settlement, _) = MakeFixture(currency: 100);
    var valid = settlement.ValidatePurchaseRequest("stall.gh-general", "good.basic-supply-bundle", 1);
    var executed = settlement.ExecutePurchase("stall.gh-general", "good.basic-supply-bundle", 1);

    var missingRegistry = new Registry();
    missingRegistry.InitializeContent();
    var noResources = new SettlementManager(missingRegistry);
    noResources.InitNewGameState();
    var unavailable = noResources.ValidatePurchaseRequest("stall.gh-general", "good.basic-supply-bundle", 1);

    return valid.Valid
        && executed.Success
        && resources.GetQuantity(ResourcePool.InStorage, "good.basic-supply-bundle") == 1
        && !unavailable.Valid
        && unavailable.Reason == "system_unavailable";
}

static bool InteractiveStalls()
{
    var (_, _, settlement, _) = MakeFixture();
    var initial = settlement.GetInteractiveStalls(SettlementManager.MvpSettlementId);
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var repaired = settlement.GetInteractiveStalls(SettlementManager.MvpSettlementId);

    return initial.SequenceEqual(["stall.gh-general"])
        && repaired.SequenceEqual(["stall.gh-general", "stall.gh-sail-shop"]);
}

static bool SignalContractExposed()
{
    var (_, _, settlement, _) = MakeFixture();
    var count = 0;
    settlement.StallOpened += (_, _) => count++;
    settlement.StallStateChanged += (_, _, _) => count++;
    settlement.NpcStateChanged += (_, _, _) => count++;
    settlement.PurchaseCompleted += (_, _, _) => count++;
    settlement.PurchaseFailed += (_, _) => count++;
    settlement.SettlementActivityChanged += (_, _) => count++;

    settlement.OnRepairCompleted("repair_node.starlight_dock");
    settlement.ExecutePurchase("stall.gh-general", "good.basic-supply-bundle", 1);
    settlement.ExecutePurchase("stall.gh-lens-workshop", "good.lens-maintenance-kit", 1);

    return count >= 6;
}

static bool FeatureReadySequence()
{
    var registry = new Registry();
    registry.InitializeContent();
    var settlement = new SettlementManager(registry);
    var registered = new List<string>();
    settlement.SetPersistenceDelegates(() => null, () => true);
    settlement.SetFocusTargetRegistration((targetId, _, _) => registered.Add(targetId));

    settlement.OnFeatureReady();

    return settlement.IsInitialized
        && settlement.SnapshotTriggerCount == 1
        && registered.SequenceEqual(["stall.gh-general"]);
}
