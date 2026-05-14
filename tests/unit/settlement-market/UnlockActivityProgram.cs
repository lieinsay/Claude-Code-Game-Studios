using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #14 Story 003: Repair-Driven Unlock & NPC State ===");

var failed = 0;
var total = 0;

Run("AC-1 through AC-4: F.2 unlock check handles matches, misses, empty requirements, and threshold=1", UnlockFormula);
Run("AC-5 through AC-10/18/19: repair_completed manages completed nodes, idempotency, and stall unlocks", RepairCompletedFlow);
Run("AC-11 through AC-15: F.3 activity aggregation emits correct active count", ActivityAggregation);
Run("AC-16/17: NPC couples through stall_id and missing stall reference warns only", NpcCoupling);

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

static SettlementManager MakeSettlement()
{
    var registry = new Registry();
    registry.InitializeContent();
    var settlement = new SettlementManager(registry);
    settlement.InitNewGameState();
    return settlement;
}

static bool UnlockFormula()
{
    var settlement = MakeSettlement();

    return settlement.IsStallUnlocked("stall.gh-sail-shop", ["repair_node.starlight_dock"])
        && !settlement.IsStallUnlocked("stall.gh-lens-workshop", ["repair_node.starlight_dock"])
        && !settlement.IsStallUnlocked("stall.gh-general", ["repair_node.starlight_dock"])
        && settlement.IsStallUnlocked("stall.gh-sail-shop", ["repair_node.starlight_dock", "repair_node.old_lighthouse"]);
}

static bool RepairCompletedFlow()
{
    var settlement = MakeSettlement();
    var opened = new List<string>();
    settlement.StallOpened += (stallId, _) => opened.Add(stallId);

    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var firstNodes = settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId);
    var sailOpen = settlement.GetStallState("stall.gh-sail-shop") == StallState.OpenBasic
        && settlement.GetNpcState("npc.yun") == NpcState.Idle;

    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var duplicateStable = settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId).SequenceEqual(firstNodes)
        && opened.Count(id => id == "stall.gh-sail-shop") == 1;

    settlement.OnRepairCompleted("repair_node.old_lighthouse");
    settlement.OnRepairCompleted("repair_node.chart_archive");
    var allOpen = settlement.GetInteractiveStalls(SettlementManager.MvpSettlementId).Count == 4;
    settlement.OnRepairCompleted("repair_node.grand_bazaar");
    var mvpMaxIgnored = settlement.GetInteractiveStalls(SettlementManager.MvpSettlementId).Count == 4;
    settlement.OnRepairCompleted("repair_node.unknown_unmatched");

    return firstNodes.SequenceEqual(["repair_node.starlight_dock"])
        && sailOpen
        && duplicateStable
        && allOpen
        && mvpMaxIgnored;
}

static bool ActivityAggregation()
{
    var settlement = MakeSettlement();
    var activity = new List<(string SettlementId, int ActiveCount)>();
    settlement.SettlementActivityChanged += (settlementId, activeCount) => activity.Add((settlementId, activeCount));

    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var dormant = settlement.GetSettlementState(SettlementManager.MvpSettlementId);
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var recoveringTwo = settlement.GetSettlementState(SettlementManager.MvpSettlementId);
    settlement.OnRepairCompleted("repair_node.old_lighthouse");
    var recoveringThree = settlement.GetSettlementState(SettlementManager.MvpSettlementId);
    settlement.OnRepairCompleted("repair_node.chart_archive");
    var active = settlement.GetSettlementState(SettlementManager.MvpSettlementId);

    return dormant == SettlementState.Dormant
        && recoveringTwo == SettlementState.Recovering
        && recoveringThree == SettlementState.Recovering
        && active == SettlementState.Active
        && activity.Contains((SettlementManager.MvpSettlementId, 4));
}

static bool NpcCoupling()
{
    var registry = new Registry();
    registry.InitializeContent();
    registry.RegisterContent("npc.test-missing-stall", ValidNpc("npc.test-missing-stall", "stall.missing"));
    var settlement = new SettlementManager(registry);
    settlement.InitNewGameState();

    settlement.TransitionStallState("stall.gh-lens-workshop", StallState.OpenBasic);

    return settlement.GetNpcState("npc.wei") == NpcState.Idle
        && settlement.GetNpcState("npc.test-missing-stall") == NpcState.Absent
        && settlement.Warnings.Any(warning => warning.Contains("missing stall", StringComparison.Ordinal));
}

static Dictionary<string, object?> ValidNpc(string id, string stallId)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "npc",
        ["owner_domain"] = "world",
        ["status"] = "Active",
        ["name_key"] = $"content.{id}.name",
        ["description_key"] = $"content.{id}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 999,
        ["references"] = Array.Empty<string>(),
        ["stall_id"] = stallId,
        ["is_default_present"] = false,
    };
}
