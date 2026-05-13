using System.Reflection;
using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 006: State Machine & Pool Transitions - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: resource appears in only one pool", Ac1ResourceOnlyInOnPerson);
Run("AC-2: transfer conserves total quantity", Ac2TransferConservesTotal);
Run("AC-3: deposited cannot transfer out", Ac3DepositedCannotTransferOut);
Run("AC-4: deposited cannot be removed", Ac4DepositedCannotBeRemoved);
Run("AC-5: consumed resources leave all pools", Ac5ConsumeLeavesDestroyedTerminalState);
Run("AC-6: storage to on-person transfer succeeds", Ac6StorageToOnPersonAllowed);
Run("AC-7: raw resources cannot enter loaded cargo bay", Ac7RawResourceCannotEnterLoaded);
Run("AC-8: carried extraction moves all carried contents to storage", Ac8ExtractCarriedToStorage);
Run("AC-9: extraction loss applies stackable formula", Ac9ExtractionLossFormula);
Run("AC-10: single unique item survives extraction failure", Ac10UniqueSingleSurvivesLoss);
Run("AC-11: on-person inventory is unaffected by carried loss", Ac11OnPersonUnaffectedByLoss);
Run("AC-12: pools remain private implementation details", Ac12PoolsRemainPrivate);

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

static bool Ac1ResourceOnlyInOnPerson()
{
    var resources = MakeResources().Resources;

    var add = resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 7);

    return add.Success
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 7
        && CanonicalPools()
            .Where(pool => pool != ResourcePool.OnPerson)
            .All(pool => resources.GetQuantity(pool, "resource.basic_supply") == 0);
}

static bool Ac2TransferConservesTotal()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 10);

    var result = resources.Transfer(ResourcePool.OnPerson, ResourcePool.InStorage, "resource.basic_supply", 4);

    return result.Success
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 6
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 4
        && TotalQuantityAcrossPools(resources, "resource.basic_supply") == 10;
}

static bool Ac3DepositedCannotTransferOut()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Deposited, "resource.basic_supply", 5);
    var before = Snapshot(resources);

    var result = resources.Transfer(ResourcePool.Deposited, ResourcePool.InStorage, "resource.basic_supply", 5);

    return result.Result == ResourceResult.ErrKindMismatch
        && Snapshot(resources) == before
        && resources.GetQuantity(ResourcePool.Deposited, "resource.basic_supply") == 5;
}

static bool Ac4DepositedCannotBeRemoved()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Deposited, "resource.basic_supply", 5);
    var before = Snapshot(resources);

    var result = resources.Remove(ResourcePool.Deposited, "resource.basic_supply", 1);

    return result.Result == ResourceResult.ErrKindMismatch
        && Snapshot(resources) == before;
}

static bool Ac5ConsumeLeavesDestroyedTerminalState()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);

    var result = resources.Consume(ResourcePool.InStorage, "resource.basic_supply", 4);

    return result.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 6
        && TotalQuantityAcrossPools(resources, "resource.basic_supply") == 6;
}

static bool Ac6StorageToOnPersonAllowed()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 8);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 3);

    return result.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 5
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 3;
}

static bool Ac7RawResourceCannotEnterLoaded()
{
    var resources = MakeResources().Resources;
    resources.SetCargoModuleVolumeBonus(500);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 8);
    var before = Snapshot(resources);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.Loaded, "resource.basic_supply", 3);

    return result.Result == ResourceResult.ErrKindMismatch
        && Snapshot(resources) == before;
}

static bool Ac8ExtractCarriedToStorage()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.basic_supply", 7);

    var result = resources.ExtractCarriedToStorage();

    return result.Success
        && result.Retained.GetValueOrDefault("resource.basic_supply") == 7
        && result.Lost.Count == 0
        && resources.GetQuantity(ResourcePool.Carried, "resource.basic_supply") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 7;
}

static bool Ac9ExtractionLossFormula()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.basic_supply", 10);

    var result = resources.ApplyExtractionLoss(0.4d);

    return result.Success
        && result.Lost.GetValueOrDefault("resource.basic_supply") == 4
        && result.Retained.GetValueOrDefault("resource.basic_supply") == 6
        && resources.GetQuantity(ResourcePool.Carried, "resource.basic_supply") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 6;
}

static bool Ac10UniqueSingleSurvivesLoss()
{
    var (registry, resources) = MakeResources();
    RegisterResource(
        registry,
        "resource.intel_fragment",
        stackRule: "unique",
        maxStack: 1,
        supplyClass: "intel");
    resources.Add(ResourcePool.Carried, "resource.intel_fragment", 1);

    var result = resources.ApplyExtractionLoss(0.4d);

    return result.Success
        && result.Lost.GetValueOrDefault("resource.intel_fragment") == 0
        && result.Retained.GetValueOrDefault("resource.intel_fragment") == 1
        && resources.GetQuantity(ResourcePool.Carried, "resource.intel_fragment") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.intel_fragment") == 1;
}

static bool Ac11OnPersonUnaffectedByLoss()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 10);
    resources.Add(ResourcePool.Carried, "resource.basic_supply", 10);

    var result = resources.ApplyExtractionLoss(0.4d);

    return result.Success
        && result.Lost.GetValueOrDefault("resource.basic_supply") == 4
        && result.Retained.GetValueOrDefault("resource.basic_supply") == 6
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 10
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 6
        && resources.GetQuantity(ResourcePool.Carried, "resource.basic_supply") == 0;
}

static bool Ac12PoolsRemainPrivate()
{
    const BindingFlags instancePublic = BindingFlags.Instance | BindingFlags.Public;
    const BindingFlags instancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    var type = typeof(ResourcesManager);

    return type.GetField("_pools", instancePrivate) is not null
        && type.GetField("_pools", instancePublic) is null
        && type.GetProperty("Pools", instancePublic) is null;
}

static (Registry Registry, ResourcesManager Resources) MakeResources()
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    return (registry, resources);
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
