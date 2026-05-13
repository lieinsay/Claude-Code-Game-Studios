using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 002: Dual Capacity System - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: full carry slots reject a new unmatched resource", Ac1FullCarryRejectsNewResource);
Run("AC-2: full carry slots allow merge into existing stack", Ac2FullCarryAllowsMerge);
Run("AC-3: multi-stack overflow checks all required slots", Ac3MultiStackOverflowRejects);
Run("AC-4: full matching stack reports carry stack full", Ac4FullMatchingStackRejects);
Run("AC-5: storage volume rejects medium overflow", Ac5StorageRejectsMediumOverflow);
Run("AC-6: storage volume accepts light overflow", Ac6StorageAcceptsLightOverflow);
Run("AC-7: cargo bay without module has zero capacity", Ac7CargoBayZeroCapacity);
Run("AC-8: cargo bay module volume rejects heavy overflow", Ac8CargoBayRejectsHeavyOverflow);
Run("AC-9: light mass profile resolves volume and weight", Ac9LightProfile);
Run("AC-10: medium mass profile resolves volume and weight", Ac10MediumProfile);
Run("AC-11: heavy mass profile resolves volume and weight", Ac11HeavyProfile);
Run("AC-12: volume check accounts for every overflow stack", Ac12VolumeCountsOverflowStacks);
Run("Regression: on_person and carried slot pools are independent", OnPersonAndCarriedSlotPoolsAreIndependent);
Run("Regression: transfer from on_person to carried moves between pools", TransferFromOnPersonToCarriedMovesBetweenPools);

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

static bool Ac1FullCarryRejectsNewResource()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.slot_filler_a", count: 5, massClass: "light");
    FillUnique(resources, ResourcePool.OnPerson, "resource.slot_filler_a", count: 5);

    registry.RegisterContent("resource.new_light_payload", ValidResource("resource.new_light_payload", massClass: "light"));
    var result = resources.Add(ResourcePool.OnPerson, "resource.new_light_payload", 1);

    return result.Result == ResourceResult.ErrCarrySlotsFull
        && result.OverflowQuantity == 1
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.new_light_payload") == 0
        && resources.GetUsedSlots(ResourcePool.OnPerson) == 5;
}

static bool Ac2FullCarryAllowsMerge()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.slot_filler_b", count: 4, massClass: "light");
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 90);
    FillUnique(resources, ResourcePool.OnPerson, "resource.slot_filler_b", count: 4);

    var result = resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 5);

    return result.Success
        && result.MergeQuantity == 5
        && result.OverflowQuantity == 0
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 95
        && resources.GetUsedSlots(ResourcePool.OnPerson) == 5;
}

static bool Ac3MultiStackOverflowRejects()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.slot_filler_c", count: 4, massClass: "light");
    FillUnique(resources, ResourcePool.OnPerson, "resource.slot_filler_c", count: 4);

    var result = resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 200);

    return result.Result == ResourceResult.ErrCarrySlotsFull
        && result.OverflowQuantity == 200
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 0
        && resources.GetUsedSlots(ResourcePool.OnPerson) == 4;
}

static bool Ac4FullMatchingStackRejects()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.slot_filler_d", count: 4, massClass: "light");
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 99);
    FillUnique(resources, ResourcePool.OnPerson, "resource.slot_filler_d", count: 4);

    var result = resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 10);

    return result.Result == ResourceResult.ErrCarryStackFull
        && result.MergeQuantity == 0
        && result.OverflowQuantity == 10
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.basic_supply") == 99
        && resources.GetUsedSlots(ResourcePool.OnPerson) == 5;
}

static bool Ac5StorageRejectsMediumOverflow()
{
    var (registry, resources) = MakeResources();
    FillStorageTo920(registry, resources);
    registry.RegisterContent("resource.medium_payload", ValidResource("resource.medium_payload", massClass: "medium"));

    var result = resources.Add(ResourcePool.InStorage, "resource.medium_payload", 1);

    return result.Result == ResourceResult.ErrTargetFull
        && resources.GetUsedVolume(ResourcePool.InStorage) == 920
        && resources.GetQuantity(ResourcePool.InStorage, "resource.medium_payload") == 0;
}

static bool Ac6StorageAcceptsLightOverflow()
{
    var (registry, resources) = MakeResources();
    FillStorageTo920(registry, resources);
    registry.RegisterContent("resource.light_payload", ValidResource("resource.light_payload", massClass: "light"));

    var result = resources.Add(ResourcePool.InStorage, "resource.light_payload", 1);

    return result.Success
        && resources.GetUsedVolume(ResourcePool.InStorage) == 970
        && resources.GetQuantity(ResourcePool.InStorage, "resource.light_payload") == 1;
}

static bool Ac7CargoBayZeroCapacity()
{
    var (registry, resources) = MakeResources();
    registry.RegisterContent("cargo.light_crate", ValidCargo("cargo.light_crate", massClass: "light"));

    var result = resources.Add(ResourcePool.Loaded, "cargo.light_crate", 1);

    return result.Result == ResourceResult.ErrCapacityZero
        && resources.GetTotalVolume(ResourcePool.Loaded) == 0
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.light_crate") == 0;
}

static bool Ac8CargoBayRejectsHeavyOverflow()
{
    var (registry, resources) = MakeResources();
    resources.SetCargoModuleVolumeBonus(500);
    registry.RegisterContent("cargo.loaded_heavy_a", ValidCargo("cargo.loaded_heavy_a", massClass: "heavy"));
    registry.RegisterContent("cargo.loaded_medium_a", ValidCargo("cargo.loaded_medium_a", massClass: "medium"));
    registry.RegisterContent("cargo.loaded_light_a", ValidCargo("cargo.loaded_light_a", massClass: "light"));
    registry.RegisterContent("cargo.heavy_overflow", ValidCargo("cargo.heavy_overflow", massClass: "heavy"));
    resources.Add(ResourcePool.Loaded, "cargo.loaded_heavy_a", 1);
    resources.Add(ResourcePool.Loaded, "cargo.loaded_medium_a", 1);
    resources.Add(ResourcePool.Loaded, "cargo.loaded_light_a", 1);

    var result = resources.Add(ResourcePool.Loaded, "cargo.heavy_overflow", 1);

    return resources.GetUsedVolume(ResourcePool.Loaded) == 370
        && result.Result == ResourceResult.ErrTargetFull
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.heavy_overflow") == 0;
}

static bool Ac9LightProfile()
{
    var (registry, resources) = MakeResources();
    registry.RegisterContent("resource.light_profile", ValidResource("resource.light_profile", massClass: "light"));

    var profile = resources.GetMassProfile("resource.light_profile");

    return profile.MassClass == "light" && profile.Volume == 50 && profile.Weight == 1;
}

static bool Ac10MediumProfile()
{
    var (registry, resources) = MakeResources();
    registry.RegisterContent("resource.medium_profile", ValidResource("resource.medium_profile", massClass: "medium"));

    var profile = resources.GetMassProfile("resource.medium_profile");

    return profile.MassClass == "medium" && profile.Volume == 120 && profile.Weight == 3;
}

static bool Ac11HeavyProfile()
{
    var (registry, resources) = MakeResources();
    registry.RegisterContent("resource.heavy_profile", ValidResource("resource.heavy_profile", massClass: "heavy"));

    var profile = resources.GetMassProfile("resource.heavy_profile");

    return profile.MassClass == "heavy" && profile.Volume == 200 && profile.Weight == 6;
}

static bool Ac12VolumeCountsOverflowStacks()
{
    var (registry, resources) = MakeResources();
    RegisterUniqueFillers(registry, "resource.storage_heavy_fill_b", count: 4, massClass: "heavy");
    FillUnique(resources, ResourcePool.InStorage, "resource.storage_heavy_fill_b", count: 4);
    registry.RegisterContent("resource.heavy_bulk", ValidResource("resource.heavy_bulk", massClass: "heavy"));

    var result = resources.Add(ResourcePool.InStorage, "resource.heavy_bulk", 200);

    return resources.GetUsedVolume(ResourcePool.InStorage) == 800
        && result.Result == ResourceResult.ErrTargetFull
        && result.OverflowQuantity == 200
        && resources.GetQuantity(ResourcePool.InStorage, "resource.heavy_bulk") == 0;
}

static bool OnPersonAndCarriedSlotPoolsAreIndependent()
{
    var first = MakeResources();
    RegisterUniqueFillers(first.Registry, "resource.on_person_filler", count: 5, massClass: "light");
    FillUnique(first.Resources, ResourcePool.OnPerson, "resource.on_person_filler", count: 5);
    first.Registry.RegisterContent("resource.carried_probe", ValidResource("resource.carried_probe", massClass: "light"));
    var carriedResult = first.Resources.Add(ResourcePool.Carried, "resource.carried_probe", 1);

    var second = MakeResources();
    RegisterUniqueFillers(second.Registry, "resource.carried_filler", count: 5, massClass: "light");
    FillUnique(second.Resources, ResourcePool.Carried, "resource.carried_filler", count: 5);
    second.Registry.RegisterContent("resource.on_person_probe", ValidResource("resource.on_person_probe", massClass: "light"));
    var onPersonResult = second.Resources.Add(ResourcePool.OnPerson, "resource.on_person_probe", 1);

    return carriedResult.Success
        && first.Resources.GetUsedSlots(ResourcePool.OnPerson) == 5
        && first.Resources.GetUsedSlots(ResourcePool.Carried) == 1
        && first.Resources.GetQuantity(ResourcePool.OnPerson, "resource.carried_probe") == 0
        && first.Resources.GetQuantity(ResourcePool.Carried, "resource.carried_probe") == 1
        && onPersonResult.Success
        && second.Resources.GetUsedSlots(ResourcePool.Carried) == 5
        && second.Resources.GetUsedSlots(ResourcePool.OnPerson) == 1
        && second.Resources.GetQuantity(ResourcePool.Carried, "resource.on_person_probe") == 0
        && second.Resources.GetQuantity(ResourcePool.OnPerson, "resource.on_person_probe") == 1;
}

static bool TransferFromOnPersonToCarriedMovesBetweenPools()
{
    var (registry, resources) = MakeResources();
    registry.RegisterContent("resource.travel_ration", ValidResource("resource.travel_ration", massClass: "light"));
    var addResult = resources.Add(ResourcePool.OnPerson, "resource.travel_ration", 3);

    var transferResult = resources.Transfer(ResourcePool.OnPerson, ResourcePool.Carried, "resource.travel_ration", 2);

    return addResult.Success
        && transferResult.Success
        && resources.GetQuantity(ResourcePool.OnPerson, "resource.travel_ration") == 1
        && resources.GetQuantity(ResourcePool.Carried, "resource.travel_ration") == 2
        && resources.GetUsedSlots(ResourcePool.OnPerson) == 1
        && resources.GetUsedSlots(ResourcePool.Carried) == 1;
}

static (Registry Registry, ResourcesManager Resources) MakeResources()
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    return (registry, resources);
}

static void FillStorageTo920(Registry registry, ResourcesManager resources)
{
    RegisterUniqueFillers(registry, "resource.storage_heavy_fill_a", count: 4, massClass: "heavy");
    RegisterUniqueFillers(registry, "resource.storage_medium_fill_a", count: 1, massClass: "medium");
    FillUnique(resources, ResourcePool.InStorage, "resource.storage_heavy_fill_a", count: 4);
    FillUnique(resources, ResourcePool.InStorage, "resource.storage_medium_fill_a", count: 1);
}

static void RegisterUniqueFillers(Registry registry, string prefix, int count, string massClass)
{
    for (var i = 0; i < count; i++)
    {
        var id = $"{prefix}_{i}";
        registry.RegisterContent(id, ValidResource(id, stackRule: "unique", maxStack: 1, supplyClass: "intel", massClass: massClass));
    }
}

static void FillUnique(ResourcesManager resources, ResourcePool pool, string prefix, int count)
{
    for (var i = 0; i < count; i++)
    {
        resources.Add(pool, $"{prefix}_{i}", 1);
    }
}

static Dictionary<string, object?> ValidResource(
    string id,
    string stackRule = "stackable",
    int maxStack = 99,
    string supplyClass = "basic",
    string massClass = "light")
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

static Dictionary<string, object?> ValidCargo(string id, string massClass)
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
        ["linked_resource_id"] = "resource.basic_supply",
        ["mass_class"] = massClass,
        ["handling_class"] = "crate",
        ["stack_rule"] = "unique",
        ["max_stack"] = 1,
    };
}
