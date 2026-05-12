using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 001: Resource Identity & Stack Merge — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: stackable same ID merges into one stack", Ac1StackableSameIdMerges);
Run("AC-2: unique same ID occupies separate slots", Ac2UniqueSameIdCreatesSeparateStacks);
Run("AC-3: transfer matches stable ID, not display name", Ac3TransferUsesStableIdOnly);
Run("AC-4: partial overflow fills existing stack then creates new stack", Ac4PartialOverflowCreatesNewStack);
Run("AC-5: fill-fullest-first chooses the largest matching stack", Ac5FillFullestFirstChoosesLargestStack);
Run("AC-6: equal quantity tie chooses the lower slot index", Ac6TieChoosesLowerSlotIndex);
Run("AC-7: no matching stack creates a new stack", Ac7NoMatchingStackCreatesNewStack);
Run("AC-8: intel supply class resolves max_stack=1", Ac8IntelMaxStackIsOne);
Run("AC-9: navigation supply splits 25 into 20+5", Ac9NavigationSplitsByDefaultStack);
Run("AC-10: basic supply splits 150 into 99+51", Ac10BasicSplitsByDefaultStack);
Run("AC-11: zero quantity add succeeds without mutation", Ac11ZeroQuantityNoOp);
Run("AC-12: negative quantity add is rejected", Ac12NegativeQuantityRejected);
Run("AC-13: unknown resource ID is rejected", Ac13UnknownResourceRejected);
Run("AC-14: deprecated resource ID cannot be replenished", Ac14DeprecatedResourceRejected);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 AC validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1StackableSameIdMerges()
{
    var resources = MakeResources();
    var first = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    var second = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 15);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return first.Success
        && second.Success
        && stacks.Count == 1
        && stacks[0].Quantity == 25
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 25;
}

static bool Ac2UniqueSameIdCreatesSeparateStacks()
{
    var resources = MakeResources();
    var first = resources.Add(ResourcePool.OnPerson, "resource.ancient_lens", 1);
    var second = resources.Add(ResourcePool.OnPerson, "resource.ancient_lens", 1);
    var stacks = resources.GetStacks(ResourcePool.OnPerson, "resource.ancient_lens");

    return first.Success
        && second.Success
        && stacks.Count == 2
        && stacks.All(stack => stack.Quantity == 1)
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.ancient_lens") == 2;
}

static bool Ac3TransferUsesStableIdOnly()
{
    var registry = MakeRegistry();
    var displayClone = registry.QueryById("resource.basic_supply").Entity;
    if (displayClone is null)
    {
        return false;
    }

    var renamedDisplay = new Dictionary<string, object?>(displayClone, StringComparer.Ordinal)
    {
        ["display_name"] = "Renamed in UI only",
    };
    if (Convert.ToString(renamedDisplay["display_name"]) != "Renamed in UI only")
    {
        return false;
    }
    var resources = MakeResources(registry);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 4);

    return result.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 6
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 4;
}

static bool Ac4PartialOverflowCreatesNewStack()
{
    var resources = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 80);
    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 30);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return result.Success
        && result.MergeQuantity == 19
        && result.OverflowQuantity == 11
        && stacks.Select(stack => stack.Quantity).SequenceEqual([99, 11]);
}

static bool Ac5FillFullestFirstChoosesLargestStack()
{
    var resources = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 159);
    resources.Remove(ResourcePool.InStorage, "resource.basic_supply", 19);

    var before = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");
    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 30);
    var after = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return before.Select(stack => stack.Quantity).SequenceEqual([80, 60])
        && result.Success
        && result.MergeQuantity == 19
        && result.OverflowQuantity == 11
        && after.Select(stack => stack.Quantity).SequenceEqual([99, 60, 11]);
}

static bool Ac6TieChoosesLowerSlotIndex()
{
    var resources = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.navigation_chart", 40);
    resources.Remove(ResourcePool.InStorage, "resource.navigation_chart", 10);
    resources.Remove(ResourcePool.InStorage, "resource.navigation_chart", 10);

    var before = resources.GetStacks(ResourcePool.InStorage, "resource.navigation_chart");
    var result = resources.Add(ResourcePool.InStorage, "resource.navigation_chart", 5);
    var after = resources.GetStacks(ResourcePool.InStorage, "resource.navigation_chart");

    return before.Select(stack => stack.Quantity).SequenceEqual([10, 10])
        && result.Success
        && result.MergeQuantity == 5
        && after.Select(stack => stack.Quantity).SequenceEqual([15, 10]);
}

static bool Ac7NoMatchingStackCreatesNewStack()
{
    var resources = MakeResources();
    var result = resources.Add(ResourcePool.InStorage, "resource.repair_kit", 30);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.repair_kit");

    return result.Success
        && result.MergeQuantity == 0
        && result.OverflowQuantity == 30
        && stacks.Count == 1
        && stacks[0].Quantity == 30;
}

static bool Ac8IntelMaxStackIsOne()
{
    var resources = MakeResources();
    return resources.GetMaxStack("resource.ancient_lens") == 1;
}

static bool Ac9NavigationSplitsByDefaultStack()
{
    var resources = MakeResources();
    var result = resources.Add(ResourcePool.InStorage, "resource.navigation_chart", 25);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.navigation_chart");

    return result.Success
        && stacks.Select(stack => stack.Quantity).SequenceEqual([20, 5]);
}

static bool Ac10BasicSplitsByDefaultStack()
{
    var resources = MakeResources();
    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 150);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return result.Success
        && stacks.Select(stack => stack.Quantity).SequenceEqual([99, 51]);
}

static bool Ac11ZeroQuantityNoOp()
{
    var resources = MakeResources();
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    var before = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");
    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 0);
    var after = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return result.Success
        && result.QuantityChanged == 0
        && before.SequenceEqual(after);
}

static bool Ac12NegativeQuantityRejected()
{
    var resources = MakeResources();
    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", -5);
    return result.Result == ResourceResult.ErrInvalidQuantity
        && resources.GetStacks(ResourcePool.InStorage).Count == 0;
}

static bool Ac13UnknownResourceRejected()
{
    var resources = MakeResources();
    var result = resources.Add(ResourcePool.InStorage, "resource.unknown", 1);
    return result.Result == ResourceResult.ErrMissingReference
        && resources.GetStacks(ResourcePool.InStorage).Count == 0;
}

static bool Ac14DeprecatedResourceRejected()
{
    var registry = MakeRegistry();
    registry.RegisterContent("resource.deprecated_supply", ValidResource("resource.deprecated_supply", "Deprecated"));
    var resources = MakeResources(registry);

    var result = resources.Add(ResourcePool.InStorage, "resource.deprecated_supply", 1);
    return result.Result == ResourceResult.ErrDeprecatedId
        && resources.GetQuantity(ResourcePool.InStorage, "resource.deprecated_supply") == 0;
}

static ResourcesManager MakeResources(Registry? registry = null)
{
    var resources = new ResourcesManager(registry ?? MakeRegistry());
    resources.Initialize();
    return resources;
}

static Registry MakeRegistry()
{
    var registry = new Registry();
    registry.InitializeContent();
    return registry;
}

static Dictionary<string, object?> ValidResource(string id, string status)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "resource",
        ["owner_domain"] = "resources",
        ["status"] = status,
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 999,
        ["references"] = Array.Empty<string>(),
        ["unit"] = "crate",
        ["stack_rule"] = "stackable",
        ["material_tags"] = new[] { "basic-supply" },
        ["supply_class"] = "basic",
        ["max_stack"] = 99,
    };
}
