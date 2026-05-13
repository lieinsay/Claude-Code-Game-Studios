using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 007: Specialized Operations - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: discard removes from allowed pool into destroyed terminal", Ac1DiscardRemovesFromOnPerson);
Run("AC-2: discard executes directly after caller confirmation", Ac2DiscardHasNoUiConfirmationGate);
Run("AC-3: discard rejects deposited terminal pool", Ac3DiscardRejectsDeposited);
Run("AC-4: consume in combat consumes from carried", Ac4ConsumeInCombatConsumesCarried);
Run("AC-5: consume in combat insufficient source is atomic", Ac5ConsumeInCombatInsufficientIsAtomic);
Run("AC-6: consume in combat touches carried only", Ac6ConsumeInCombatUsesCarriedOnly);
Run("AC-7: commit deposit atomically moves multiple resources to deposited", Ac7CommitDepositMovesCosts);
Run("AC-8: commit deposit fails atomically on shortage", Ac8CommitDepositShortageIsAtomic);
Run("AC-9: can deposit is side-effect-free", Ac9CanDepositHasNoSideEffects);
Run("AC-10: execute purchase moves listed goods to storage", Ac10ExecutePurchaseMovesListedToStorage);
Run("AC-11: list for sale moves storage goods to listed", Ac11ListForSaleMovesStorageToListed);
Run("AC-12: add loot adds to carried when capacity allows", Ac12AddLootAddsToCarried);
Run("AC-13: add loot rejects full carried pool without matching stack", Ac13AddLootRejectsFullCarried);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 007 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 007 validation passed: {total}/{total} checks passed.");
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

static bool Ac1DiscardRemovesFromOnPerson()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 5);

    var result = resources.Discard(ResourcePool.OnPerson, "resource.basic_supply", 3);

    return result.Success
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 2
        && TotalQuantityAcrossPools(resources, "resource.basic_supply") == 2;
}

static bool Ac2DiscardHasNoUiConfirmationGate()
{
    var method = typeof(ResourcesManager).GetMethod(
        nameof(ResourcesManager.Discard),
        [typeof(ResourcePool), typeof(string), typeof(int)]);
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 2);

    var result = resources.Discard(ResourcePool.OnPerson, "resource.basic_supply", 1);

    return method is not null
        && method.GetParameters().Length == 3
        && result.Success
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 1;
}

static bool Ac3DiscardRejectsDeposited()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Deposited, "resource.basic_supply", 1);
    var before = Snapshot(resources);

    var result = resources.Discard(ResourcePool.Deposited, "resource.basic_supply", 1);

    return result.Result == ResourceResult.ErrKindMismatch
        && Snapshot(resources) == before;
}

static bool Ac4ConsumeInCombatConsumesCarried()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 5);

    var result = resources.ConsumeInCombat("resource.repair_kit", 2);

    return result.Success
        && resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 3
        && TotalQuantityAcrossPools(resources, "resource.repair_kit") == 3;
}

static bool Ac5ConsumeInCombatInsufficientIsAtomic()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 1);
    var before = Snapshot(resources);

    var result = resources.ConsumeInCombat("resource.repair_kit", 5);

    return result.Result == ResourceResult.ErrSourceInsufficient
        && Snapshot(resources) == before;
}

static bool Ac6ConsumeInCombatUsesCarriedOnly()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 4);
    resources.Add(ResourcePool.InStorage, "resource.repair_kit", 10);

    var result = resources.ConsumeInCombat("resource.repair_kit", 2);

    return result.Success
        && resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 2
        && resources.GetQuantity(ResourcePool.InStorage, "resource.repair_kit") == 10;
}

static bool Ac7CommitDepositMovesCosts()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 2);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 3);
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 3);
    var costs = Costs(("resource.basic_supply", 5), ("resource.repair_kit", 3));

    var result = resources.CommitDeposit("repair_node.starlight_dock", costs);

    return result.Success
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 0
        && resources.GetQuantity(ResourcePool.Carried, "resource.repair_kit") == 0
        && resources.GetQuantity(ResourcePool.Deposited, "resource.basic_supply") == 5
        && resources.GetQuantity(ResourcePool.Deposited, "resource.repair_kit") == 3;
}

static bool Ac8CommitDepositShortageIsAtomic()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 2);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 3);
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 2);
    var costs = Costs(("resource.basic_supply", 5), ("resource.repair_kit", 3));
    var before = Snapshot(resources);

    var result = resources.CommitDeposit("repair_node.starlight_dock", costs);

    return result.Result == ResourceResult.ErrSourceInsufficient
        && Snapshot(resources) == before;
}

static bool Ac9CanDepositHasNoSideEffects()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 5);
    var costs = Costs(("resource.basic_supply", 3));
    var before = Snapshot(resources);

    var first = resources.CanDeposit("repair_node.starlight_dock", costs);
    var second = resources.CanDeposit("repair_node.starlight_dock", costs);
    var third = resources.CanDeposit("repair_node.starlight_dock", costs);
    var emptyCosts = resources.CanDeposit("repair_node.starlight_dock", Costs());

    return first && second && third && emptyCosts
        && Snapshot(resources) == before;
}

static bool Ac10ExecutePurchaseMovesListedToStorage()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.market_good");
    resources.Add(ResourcePool.Listed, "resource.market_good", 3);

    var result = resources.ExecutePurchase("resource.market_good", 3);

    return result.Success
        && resources.GetQuantity(ResourcePool.Listed, "resource.market_good") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.market_good") == 3;
}

static bool Ac11ListForSaleMovesStorageToListed()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.market_good");
    resources.Add(ResourcePool.InStorage, "resource.market_good", 5);

    var result = resources.ListForSale("resource.market_good", 5, 100);

    return result.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.market_good") == 0
        && resources.GetQuantity(ResourcePool.Listed, "resource.market_good") == 5;
}

static bool Ac12AddLootAddsToCarried()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.loot_scrap");

    var result = resources.AddLoot("resource.loot_scrap", 3);

    return result.Success
        && resources.GetQuantity(ResourcePool.Carried, "resource.loot_scrap") == 3;
}

static bool Ac13AddLootRejectsFullCarried()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.unique_token", stackRule: "unique", maxStack: 1, supplyClass: "intel");
    RegisterResource(registry, "resource.loot_new");
    resources.Add(ResourcePool.Carried, "resource.unique_token", 5);
    var before = Snapshot(resources);

    var result = resources.AddLoot("resource.loot_new", 1);

    return result.Result == ResourceResult.ErrCarrySlotsFull
        && Snapshot(resources) == before;
}

static (Registry Registry, ResourcesManager Resources) MakeResources()
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    return (registry, resources);
}

static Dictionary<string, int> Costs(params (string ResourceId, int Quantity)[] entries)
{
    return entries.ToDictionary(entry => entry.ResourceId, entry => entry.Quantity, StringComparer.Ordinal);
}

static int TotalQuantityAcrossPools(ResourcesManager resources, string resourceId)
{
    return CanonicalPools().Sum(pool => resources.GetQuantity(pool, resourceId));
}

static string Snapshot(ResourcesManager resources)
{
    return string.Join(
        "|",
        CanonicalPools().Select(pool =>
            $"{pool}:{string.Join(",", resources.GetStacks(pool).Select(stack => $"{stack.SlotIndex}:{stack.ResourceId}:{stack.Quantity}"))}"));
}

static ResourcePool[] CanonicalPools()
{
    return
    [
        ResourcePool.OnPerson,
        ResourcePool.InStorage,
        ResourcePool.Loaded,
        ResourcePool.Listed,
        ResourcePool.Carried,
        ResourcePool.Deposited,
    ];
}

static void RegisterResource(
    Registry registry,
    string id,
    string stackRule = "stackable",
    int maxStack = 99,
    string supplyClass = "basic",
    string massClass = "light")
{
    registry.RegisterContent(id, ValidResource(id, stackRule, maxStack, supplyClass, massClass));
}

static Dictionary<string, object?> ValidResource(
    string id,
    string stackRule,
    int maxStack,
    string supplyClass,
    string massClass)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "resource",
        ["owner_domain"] = "resources",
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 999,
        ["references"] = Array.Empty<string>(),
        ["unit"] = "crate",
        ["stack_rule"] = stackRule,
        ["material_tags"] = new[] { "test-material" },
        ["supply_class"] = supplyClass,
        ["max_stack"] = maxStack,
        ["mass_class"] = massClass,
    };
}
