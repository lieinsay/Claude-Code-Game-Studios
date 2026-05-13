using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 005: Core Atomic Operations - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: add merges into an existing stack and creates overflow", Ac1AddMergesAndOverflows);
Run("AC-2: add to full carry without matching stack fails atomically", Ac2AddFullCarryFails);
Run("AC-3: zero quantity add succeeds without mutation", Ac3ZeroAddNoOp);
Run("AC-4: negative add quantity is rejected", Ac4NegativeAddRejected);
Run("AC-5: remove takes from the largest stack first", Ac5RemoveLargestStackFirst);
Run("AC-6: insufficient remove fails without mutation", Ac6InsufficientRemoveFails);
Run("AC-7: remove equal-stack tie chooses lower slot index", Ac7RemoveTieChoosesLowerSlot);
Run("AC-8: transfer splits source stack and adds target stack", Ac8TransferSplitSucceeds);
Run("AC-9: transfer source shortage fails atomically", Ac9TransferSourceShortageFails);
Run("AC-10: transfer target full returns target full and preserves source", Ac10TransferTargetFullFails);
Run("AC-11: transfer merges target then creates overflow", Ac11TransferMergeAndOverflow);
Run("AC-12: consume removes quantity into destroyed terminal state", Ac12ConsumeRemovesQuantity);
Run("AC-13: insufficient consume fails without mutation", Ac13InsufficientConsumeFails);
Run("AC-14: transfer capacity failure preserves source pool", Ac14TransferFailurePreservesSource);
Run("AC-15: failed core operations leave all pools unchanged", Ac15FailedOperationsAreAtomic);
Run("Regression: consume rejects cargo items", ConsumeRejectsCargoItems);

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

static bool Ac1AddMergesAndOverflows()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 90);

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 30);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return result.Success
        && result.MergeQuantity == 9
        && result.OverflowQuantity == 21
        && stacks.Select(stack => stack.Quantity).SequenceEqual([99, 21]);
}

static bool Ac2AddFullCarryFails()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.full_carry_filler_a", count: 5);
    FillUnique(resources, ResourcePool.OnPerson, "resource.full_carry_filler_a", count: 5);
    RegisterResource(registry, "resource.new_payload_a");
    var before = Snapshot(resources);

    var result = resources.Add(ResourcePool.OnPerson, "resource.new_payload_a", 1);

    return result.Result == ResourceResult.ErrCarrySlotsFull
        && Snapshot(resources) == before;
}

static bool Ac3ZeroAddNoOp()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    var before = Snapshot(resources);

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 0);

    return result.Success
        && result.QuantityChanged == 0
        && Snapshot(resources) == before;
}

static bool Ac4NegativeAddRejected()
{
    var resources = MakeResources().Resources;
    var before = Snapshot(resources);

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", -5);

    return result.Result == ResourceResult.ErrInvalidQuantity
        && Snapshot(resources) == before;
}

static bool Ac5RemoveLargestStackFirst()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.remove_largest", maxStack: 50);
    resources.Add(ResourcePool.InStorage, "resource.remove_largest", 80);

    var result = resources.Remove(ResourcePool.InStorage, "resource.remove_largest", 40);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.remove_largest");

    return result.Success
        && stacks.Select(stack => stack.Quantity).SequenceEqual([10, 30]);
}

static bool Ac6InsufficientRemoveFails()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 50);
    var before = Snapshot(resources);

    var result = resources.Remove(ResourcePool.InStorage, "resource.basic_supply", 60);

    return result.Result == ResourceResult.ErrSourceInsufficient
        && Snapshot(resources) == before;
}

static bool Ac7RemoveTieChoosesLowerSlot()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.remove_tie", maxStack: 30);
    resources.Add(ResourcePool.InStorage, "resource.remove_tie", 60);

    var result = resources.Remove(ResourcePool.InStorage, "resource.remove_tie", 20);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.remove_tie");

    return result.Success
        && stacks.Select(stack => stack.Quantity).SequenceEqual([10, 30]);
}

static bool Ac8TransferSplitSucceeds()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.transfer_split_filler", count: 2);
    FillUnique(resources, ResourcePool.OnPerson, "resource.transfer_split_filler", count: 2);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 50);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 20);

    return result.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 30
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 20
        && resources.GetStacks(ResourcePool.OnPerson, "resource.basic_supply").Single().Quantity == 20;
}

static bool Ac9TransferSourceShortageFails()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 3);
    var before = Snapshot(resources);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 5);

    return result.Result == ResourceResult.ErrSourceInsufficient
        && Snapshot(resources) == before;
}

static bool Ac10TransferTargetFullFails()
{
    var (registry, resources) = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 30);
    RegisterUniqueFillers(registry, "resource.full_carry_filler_b", count: 5);
    FillUnique(resources, ResourcePool.OnPerson, "resource.full_carry_filler_b", count: 5);
    var before = Snapshot(resources);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 10);

    return result.Result == ResourceResult.ErrTargetFull
        && Snapshot(resources) == before;
}

static bool Ac11TransferMergeAndOverflow()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 50);
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 90);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 20);
    var targetStacks = resources.GetStacks(ResourcePool.OnPerson, "resource.basic_supply");

    return result.Success
        && result.MergeQuantity == 9
        && result.OverflowQuantity == 11
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 30
        && targetStacks.Select(stack => stack.Quantity).SequenceEqual([99, 11]);
}

static bool Ac12ConsumeRemovesQuantity()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);

    var result = resources.Consume(ResourcePool.InStorage, "resource.basic_supply", 5);

    return result.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 5
        && TotalQuantityAcrossPools(resources, "resource.basic_supply") == 5;
}

static bool Ac13InsufficientConsumeFails()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 3);
    var before = Snapshot(resources);

    var result = resources.Consume(ResourcePool.InStorage, "resource.basic_supply", 5);

    return result.Result == ResourceResult.ErrSourceInsufficient
        && Snapshot(resources) == before;
}

static bool Ac14TransferFailurePreservesSource()
{
    var (registry, resources) = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 30);
    RegisterUniqueFillers(registry, "resource.full_carry_filler_c", count: 5);
    FillUnique(resources, ResourcePool.OnPerson, "resource.full_carry_filler_c", count: 5);
    var sourceBefore = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply").Select(stack => stack.Quantity).ToArray();

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 10);
    var sourceAfter = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply").Select(stack => stack.Quantity).ToArray();

    return result.Result == ResourceResult.ErrTargetFull
        && sourceAfter.SequenceEqual(sourceBefore);
}

static bool Ac15FailedOperationsAreAtomic()
{
    var (registry, resources) = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 30);
    RegisterUniqueFillers(registry, "resource.full_carry_filler_d", count: 5);
    FillUnique(resources, ResourcePool.OnPerson, "resource.full_carry_filler_d", count: 5);
    var before = Snapshot(resources);

    var add = resources.Add(ResourcePool.InStorage, "resource.unknown_atomic", 1);
    var remove = resources.Remove(ResourcePool.InStorage, "resource.basic_supply", 100);
    var transfer = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 10);
    var consume = resources.Consume(ResourcePool.InStorage, "resource.basic_supply", 100);

    return !add.Success
        && !remove.Success
        && !transfer.Success
        && !consume.Success
        && Snapshot(resources) == before;
}

static bool ConsumeRejectsCargoItems()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.consume_reject", "resource.basic_supply", "light");
    resources.SetCargoModuleVolumeBonus(500);
    var add = resources.AddCargo(ResourcePool.Loaded, "cargo.consume_reject", "resource.basic_supply", 1);
    var before = Snapshot(resources);

    var consume = resources.Consume(ResourcePool.Loaded, "cargo.consume_reject", 1);

    return add.Success
        && consume.Result == ResourceResult.ErrKindMismatch
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

static void RegisterCargo(
    Registry registry,
    string id,
    string linkedResourceId,
    string massClass)
{
    registry.RegisterContent(id, ValidCargo(id, linkedResourceId, massClass));
}

static void RegisterUniqueFillers(Registry registry, string prefix, int count)
{
    for (var i = 0; i < count; i++)
    {
        RegisterResource(
            registry,
            $"{prefix}_{i}",
            stackRule: "unique",
            maxStack: 1,
            supplyClass: "intel");
    }
}

static void FillUnique(ResourcesManager resources, ResourcePool pool, string prefix, int count)
{
    for (var i = 0; i < count; i++)
    {
        resources.Add(pool, $"{prefix}_{i}", 1);
    }
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

static Dictionary<string, object?> ValidCargo(string id, string linkedResourceId, string massClass)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "cargo",
        ["owner_domain"] = "resources",
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 999,
        ["references"] = Array.Empty<string>(),
        ["linked_resource_id"] = linkedResourceId,
        ["mass_class"] = massClass,
        ["handling_class"] = "crate",
        ["stack_rule"] = "unique",
        ["max_stack"] = 1,
    };
}
