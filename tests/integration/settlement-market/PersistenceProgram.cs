using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #14 Story 005: Persistence & State Recovery ===");

var failed = 0;
var total = 0;

Run("AC-1/2: new game initializes defaults and writes initial progress snapshot", NewGameInitialSnapshot);
Run("AC-3/4/9: serialize and restore recovering and active states without loss", RoundtripRecovery);
Run("AC-5/10/14: restore dedupes nodes and reconciles damaged settlement/NPC state", DefensiveReconciliation);
Run("AC-6/7/8: purchase and repair trigger snapshots while no-ops do not", SnapshotTriggers);
Run("AC-11/12/13: unknown stall/NPC are skipped and unknown completed node is retained with warning", UnknownSnapshotData);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 005 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 005 validation passed: {total}/{total} checks passed.");
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

static (Registry Registry, ResourcesManager Resources, SettlementManager Settlement) MakeFixture(int currency = 500)
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    resources.Add(ResourcePool.InStorage, "resource.cloud_coin", currency);
    var settlement = new SettlementManager(registry);
    settlement.SetResourcesManager(resources);
    settlement.InitNewGameState();
    return (registry, resources, settlement);
}

static bool NewGameInitialSnapshot()
{
    var registry = new Registry();
    registry.InitializeContent();
    var settlement = new SettlementManager(registry);
    settlement.SetPersistenceDelegates(() => null, () => true);

    settlement.OnFeatureReady();

    return settlement.GetSettlementState(SettlementManager.MvpSettlementId) == SettlementState.Dormant
        && settlement.GetStallState(SettlementManager.DefaultStallId) == StallState.OpenBasic
        && settlement.GetNpcState("npc.atu") == NpcState.Idle
        && settlement.SnapshotTriggerCount == 1;
}

static bool RoundtripRecovery()
{
    var (_, _, source) = MakeFixture();
    source.OnRepairCompleted("repair_node.starlight_dock");
    var recoveringPayload = source.SerializeSettlement();

    var recovering = MakeFixture().Settlement;
    recovering.DeserializeSettlement(recoveringPayload);

    source.OnRepairCompleted("repair_node.old_lighthouse");
    source.OnRepairCompleted("repair_node.chart_archive");
    var activePayload = source.SerializeSettlement();
    var active = MakeFixture().Settlement;
    active.DeserializeSettlement(activePayload);

    return recovering.GetSettlementState(SettlementManager.MvpSettlementId) == SettlementState.Recovering
        && recovering.GetStallState("stall.gh-sail-shop") == StallState.OpenBasic
        && recovering.GetNpcState("npc.yun") == NpcState.Idle
        && recovering.GetCompletedNodeIds(SettlementManager.MvpSettlementId).SequenceEqual(["repair_node.starlight_dock"])
        && active.GetSettlementState(SettlementManager.MvpSettlementId) == SettlementState.Active
        && active.GetInteractiveStalls(SettlementManager.MvpSettlementId).Count == 4
        && active.GetCompletedNodeIds(SettlementManager.MvpSettlementId).Count == 3;
}

static bool DefensiveReconciliation()
{
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["settlements"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [SettlementManager.MvpSettlementId] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["settlement_state"] = (int)SettlementState.Active,
                ["completed_node_ids"] = new[] { "repair_node.starlight_dock", "repair_node.starlight_dock" },
            },
        },
        ["stalls"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["stall.gh-general"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["stall_state"] = (int)StallState.OpenBasic,
                ["settlement_id"] = SettlementManager.MvpSettlementId,
            },
            ["stall.gh-sail-shop"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["stall_state"] = (int)StallState.OpenBasic,
                ["settlement_id"] = SettlementManager.MvpSettlementId,
            },
        },
        ["npcs"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["npc.atu"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["npc_state"] = (int)NpcState.Idle,
                ["stall_id"] = "stall.gh-general",
            },
            ["npc.yun"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["npc_state"] = (int)NpcState.Absent,
                ["stall_id"] = "stall.gh-sail-shop",
            },
        },
    };

    var settlement = MakeFixture().Settlement;
    settlement.DeserializeSettlement(payload);

    return settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId).SequenceEqual(["repair_node.starlight_dock"])
        && settlement.GetNpcState("npc.yun") == NpcState.Idle
        && settlement.GetSettlementState(SettlementManager.MvpSettlementId) == SettlementState.Recovering
        && settlement.Warnings.Count >= 2;
}

static bool SnapshotTriggers()
{
    var (_, _, settlement) = MakeFixture(currency: 200);
    settlement.SetPersistenceDelegates(() => null, () => true);
    settlement.ExecutePurchase("stall.gh-general", "good.basic-supply-bundle", 1);
    var afterPurchase = settlement.SnapshotTriggerCount;
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var afterRepair = settlement.SnapshotTriggerCount;
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    settlement.OnUseRequested("stall.gh-lens-workshop");

    return afterPurchase == 1
        && afterRepair == 2
        && settlement.SnapshotTriggerCount == 2;
}

static bool UnknownSnapshotData()
{
    var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["settlements"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [SettlementManager.MvpSettlementId] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["settlement_state"] = 0,
                ["completed_node_ids"] = new[] { "repair_node.future_content" },
            },
        },
        ["stalls"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["stall.future"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["stall_state"] = 1,
                ["settlement_id"] = SettlementManager.MvpSettlementId,
            },
        },
        ["npcs"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["npc.future"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["npc_state"] = 1,
                ["stall_id"] = "stall.future",
            },
        },
    };

    var settlement = MakeFixture().Settlement;
    settlement.DeserializeSettlement(payload);

    return settlement.GetStallState("stall.future") == StallState.Closed
        && settlement.GetNpcState("npc.future") == NpcState.Absent
        && settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId).SequenceEqual(["repair_node.future_content"])
        && settlement.Warnings.Count >= 3;
}
