using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #14 Story 006: Edge Cases, UI Integration & Defensive Handling ===");

var failed = 0;
var total = 0;

Run("E.1/E.2/E.3/E.16: closed stalls, capacity full, insufficient funds, and exact currency are defended", PurchaseBoundaries);
Run("E.4/E.8/E.10/E.11/E.12: repair unlock edge cases are idempotent and non-crashing", RepairBoundaries);
Run("E.5/E.6/E.9/E.13/E.14: UI-facing data stays snapshot-only and session goods can remain stable", UiPersistenceBoundaries);
Run("E.7/E.15/E.23/E.24: narrative fallback, zero price, and missing Registry data produce diagnostics without crashes", DefensiveDiagnostics);
Run("AC-19 through AC-22: UI and Feedback signal boundaries expose the expected event stream", UiFeedbackSignals);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 006 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 006 validation passed: {total}/{total} checks passed.");
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

static bool PurchaseBoundaries()
{
    var (_, resources, settlement) = MakeFixture(currency: 150);
    var closedInteractive = settlement.GetInteractiveStalls(SettlementManager.MvpSettlementId).SequenceEqual(["stall.gh-general"]);
    resources.Add(ResourcePool.InStorage, "good.route-notes", 19);
    var capacity = settlement.ValidatePurchaseRequest("stall.gh-general", "good.basic-supply-bundle", 1);

    var poor = MakeFixture(currency: 30).Settlement.ValidatePurchaseRequest("stall.gh-general", "good.basic-supply-bundle", 1);
    var exactFixture = MakeFixture(currency: 150);
    var exact = exactFixture.Settlement.ExecutePurchase("stall.gh-general", "good.basic-supply-bundle", 3);

    return closedInteractive
        && !capacity.Valid && capacity.Reason == SettlementManager.PurchaseFailCapacity
        && !poor.Valid && poor.Reason == SettlementManager.PurchaseFailFunds
        && exact.Success
        && exactFixture.Resources.GetPlayerCurrency() == 0;
}

static bool RepairBoundaries()
{
    var (_, _, settlement) = MakeFixture();
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var first = settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId).ToArray();
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var duplicateStable = settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId).SequenceEqual(first);

    settlement.OnRepairCompleted("repair_node.old_lighthouse");
    settlement.OnRepairCompleted("repair_node.chart_archive");
    settlement.OnRepairCompleted("repair_node.grand_bazaar");
    var allMax = settlement.GetInteractiveStalls(SettlementManager.MvpSettlementId).Count == 4;
    var emptyRequired = !settlement.IsStallUnlocked("stall.gh-general", ["repair_node.starlight_dock"]);
    settlement.OnRepairCompleted("repair_node.not_in_registry");

    return duplicateStable && allMax && emptyRequired;
}

static bool UiPersistenceBoundaries()
{
    var (_, _, settlement) = MakeFixture();
    var sessionGoods = settlement.CapturePurchaseSessionGoods("stall.gh-general");
    settlement.OnRepairCompleted("repair_node.starlight_dock");
    var reopenedGoods = settlement.CapturePurchaseSessionGoods("stall.gh-sail-shop");
    var payload = settlement.SerializeSettlement();

    var emptySettlement = MakeFixture().Settlement;
    emptySettlement.DeserializeSettlement(AllClosedPayload());

    return sessionGoods.SequenceEqual(["good.basic-supply-bundle", "good.repair-canvas"])
        && reopenedGoods.Contains("good.storm-resistant-coating")
        && !payload.ContainsKey("ui_state")
        && emptySettlement.GetInteractiveStalls(SettlementManager.MvpSettlementId).Count == 0
        && settlement.ClampPurchaseQuantity("good.basic-supply-bundle", -5) == 1
        && settlement.ClampPurchaseQuantity("good.basic-supply-bundle", 99) == settlement.GetMaxAffordable("good.basic-supply-bundle");
}

static bool DefensiveDiagnostics()
{
    var (registry, resources, settlement) = MakeFixture(currency: 0);
    var name = settlement.GetNpcDisplayName("npc.atu");
    RegisterGood(registry, "good.free-edge", "stall.gh-general", price: 0);
    var free = settlement.ExecutePurchase("stall.gh-general", "good.free-edge", 3);
    var missingPrice = settlement.CalculateTotalCost("good.missing-price", 1);

    var missing = new SettlementManager(new Registry());
    missing.InitNewGameState();

    return name == "摊主"
        && free.Success
        && resources.GetQuantity(ResourcePool.InStorage, "good.free-edge") == 3
        && missingPrice == 0
        && settlement.Errors.Any(error => error.Contains("price=0", StringComparison.Ordinal))
        && missing.Errors.Count >= 2;
}

static Dictionary<string, object?> AllClosedPayload()
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["settlements"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [SettlementManager.MvpSettlementId] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["settlement_state"] = 0,
                ["completed_node_ids"] = Array.Empty<string>(),
            },
        },
        ["stalls"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["stall.gh-general"] = Stall("stall.gh-general", 0),
            ["stall.gh-lens-workshop"] = Stall("stall.gh-lens-workshop", 0),
            ["stall.gh-sail-shop"] = Stall("stall.gh-sail-shop", 0),
            ["stall.gh-chart-studio"] = Stall("stall.gh-chart-studio", 0),
        },
        ["npcs"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["npc.atu"] = Npc("stall.gh-general", 0),
            ["npc.wei"] = Npc("stall.gh-lens-workshop", 0),
            ["npc.yun"] = Npc("stall.gh-sail-shop", 0),
            ["npc.cen"] = Npc("stall.gh-chart-studio", 0),
        },
    };
}

static Dictionary<string, object?> Stall(string stallId, int state)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["stall_state"] = state,
        ["settlement_id"] = SettlementManager.MvpSettlementId,
    };
}

static Dictionary<string, object?> Npc(string stallId, int state)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["npc_state"] = state,
        ["stall_id"] = stallId,
    };
}

static bool UiFeedbackSignals()
{
    var (_, _, settlement) = MakeFixture(currency: 100);
    var events = new List<string>();
    settlement.StallOpened += (_, _) => events.Add("stall_opened");
    settlement.StallStateChanged += (_, _, _) => events.Add("stall_state_changed");
    settlement.NpcStateChanged += (_, _, _) => events.Add("npc_state_changed");
    settlement.PurchaseCompleted += (_, _, _) => events.Add("purchase_completed");
    settlement.PurchaseFailed += (_, _) => events.Add("purchase_failed");
    settlement.SettlementActivityChanged += (_, _) => events.Add("settlement_activity_changed");

    settlement.OnRepairCompleted("repair_node.starlight_dock");
    settlement.ExecutePurchase("stall.gh-general", "good.basic-supply-bundle", 1);
    settlement.ExecutePurchase("stall.gh-general", "good.repair-canvas", 2);

    return events.Contains("stall_opened")
        && events.Contains("stall_state_changed")
        && events.Contains("npc_state_changed")
        && events.Contains("purchase_completed")
        && events.Contains("purchase_failed")
        && events.Contains("settlement_activity_changed");
}

static void RegisterGood(Registry registry, string id, string stallId, int price)
{
    registry.RegisterContent(id, new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "good",
        ["owner_domain"] = "resources",
        ["status"] = "Active",
        ["name_key"] = $"content.{id}.name",
        ["description_key"] = $"content.{id}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 999,
        ["references"] = Array.Empty<string>(),
        ["stall_id"] = stallId,
        ["available_stall_ids"] = new[] { stallId },
        ["price"] = price,
        ["required_stall_state"] = 1,
        ["supply_class"] = "basic",
        ["stack_rule"] = "stackable",
        ["max_stack"] = 99,
        ["mass_class"] = "light",
        ["material_tags"] = new[] { "basic" },
    });
}
