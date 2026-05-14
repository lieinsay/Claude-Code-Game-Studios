using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #14 Story 002: Purchase Flow & Price Formula ===");

var failed = 0;
var total = 0;

Run("AC-1/2/3/19: total cost is deterministic and price=0 is defensive", TotalCost);
Run("AC-4 through AC-8: purchase validation covers open, closed, funds, capacity, and unavailable goods", ValidationMatrix);
Run("AC-9/10/11: execute delegates to #5, emits completion/failure, and triggers snapshot", ExecutionSignalsAndSnapshot);
Run("AC-12/13/14: visible goods respect stall unlock state and Registry availability", GoodVisibility);
Run("AC-15/16/17: max affordable and quantity clamp are bounded", QuantityBounds);
Run("AC-18: failure reason constants are stable strings", FailureConstants);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 002 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 002 validation passed: {total}/{total} checks passed.");
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

static bool TotalCost()
{
    var (registry, _, settlement) = MakeFixture();
    RegisterGood(registry, "good.free-test", "stall.gh-general", price: 0);

    return settlement.CalculateTotalCost("good.basic-supply-bundle", 3) == 150
        && settlement.CalculateTotalCost("good.route-notes", 1) == 120
        && settlement.CalculateTotalCost("good.basic-supply-bundle", 0) == 0
        && settlement.CalculateTotalCost("good.free-test", 3) == 0
        && settlement.Errors.Any(error => error.Contains("price=0", StringComparison.Ordinal));
}

static bool ValidationMatrix()
{
    var (_, resources, settlement) = MakeFixture(currency: 200);
    var valid = settlement.ValidatePurchaseRequest("stall.gh-general", "good.basic-supply-bundle", 3);
    var closed = settlement.ValidatePurchaseRequest("stall.gh-sail-shop", "good.storm-resistant-coating", 1);
    var funds = settlement.ValidatePurchaseRequest("stall.gh-general", "good.repair-canvas", 3);
    var unavailable = settlement.ValidatePurchaseRequest("stall.gh-general", "good.route-notes", 1);

    resources.Add(ResourcePool.InStorage, "good.route-notes", 19);
    var capacity = settlement.ValidatePurchaseRequest("stall.gh-general", "good.basic-supply-bundle", 1);

    return valid.Valid && valid.TotalCost == 150
        && !closed.Valid && closed.Reason == "stall_closed"
        && !funds.Valid && funds.Reason == SettlementManager.PurchaseFailFunds
        && !capacity.Valid && capacity.Reason == SettlementManager.PurchaseFailCapacity
        && !unavailable.Valid && unavailable.Reason == "good_unavailable";
}

static bool ExecutionSignalsAndSnapshot()
{
    var (_, resources, settlement) = MakeFixture(currency: 150);
    var completed = false;
    var failed = false;
    settlement.PurchaseCompleted += (goodId, quantity, totalCost) =>
        completed = goodId == "good.basic-supply-bundle" && quantity == 3 && totalCost == 150;
    settlement.PurchaseFailed += (_, _) => failed = true;
    settlement.SetPersistenceDelegates(() => null, () => true);

    var success = settlement.ExecutePurchase("stall.gh-general", "good.basic-supply-bundle", 3);
    var defensiveFail = settlement.ExecutePurchase("stall.gh-sail-shop", "good.storm-resistant-coating", 1);

    return success.Success
        && success.TotalCost == 150
        && resources.GetPlayerCurrency() == 0
        && resources.GetQuantity(ResourcePool.InStorage, "good.basic-supply-bundle") == 3
        && completed
        && defensiveFail is { Success: false, Reason: "stall_closed" }
        && failed
        && settlement.SnapshotTriggerCount == 1;
}

static bool GoodVisibility()
{
    var (_, _, settlement) = MakeFixture();
    var general = settlement.GetStallGoods("stall.gh-general");
    var closed = settlement.GetStallGoods("stall.gh-sail-shop");
    settlement.TransitionStallState("stall.gh-lens-workshop", StallState.OpenBasic);
    var lens = settlement.GetStallGoods("stall.gh-lens-workshop");

    return general.SequenceEqual(["good.basic-supply-bundle", "good.repair-canvas"])
        && closed.Count == 0
        && lens.SequenceEqual(["good.basic-supply-bundle", "good.repair-canvas", "good.lens-maintenance-kit", "good.route-notes"]);
}

static bool QuantityBounds()
{
    var (_, _, settlement) = MakeFixture(currency: 200);

    var maxFour = settlement.GetMaxAffordable("good.basic-supply-bundle");
    var clampZero = settlement.ClampPurchaseQuantity("good.basic-supply-bundle", 0);
    var clampNegative = settlement.ClampPurchaseQuantity("good.basic-supply-bundle", -1);
    var clampFraction = settlement.ClampPurchaseQuantity("good.basic-supply-bundle", 3.9);
    var clampAbove = settlement.ClampPurchaseQuantity("good.basic-supply-bundle", 99);

    var poor = MakeFixture(currency: 30).Settlement.GetMaxAffordable("good.basic-supply-bundle");

    return maxFour == 4
        && poor == 0
        && clampZero == 1
        && clampNegative == 1
        && clampFraction == 3
        && clampAbove == 4;
}

static bool FailureConstants()
{
    return SettlementManager.PurchaseFailCapacity == "capacity_full"
        && SettlementManager.PurchaseFailFunds == "insufficient_funds";
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
