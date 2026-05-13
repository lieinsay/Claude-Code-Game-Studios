using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 003: Cargo Model & Unpack - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: cargo cannot transfer or add into on_person", Ac1CargoCannotEnterOnPerson);
Run("AC-2: cargo cannot transfer into in_storage", Ac2CargoCannotEnterStorage);
Run("AC-3: raw resource cannot enter loaded cargo bay", Ac3ResourceCannotEnterLoaded);
Run("AC-4: cargo exposes positive linked resource quantity", Ac4CargoMetadataExposesPositiveQuantity);
Run("AC-5: unpack merges existing stack and creates overflow stack", Ac5UnpackMergesAndOverflows);
Run("AC-6: unpack into empty storage creates one raw resource stack", Ac6UnpackIntoEmptyStorage);
Run("AC-7: unpack fails atomically when storage full without match", Ac7UnpackFailsWhenStorageFull);
Run("AC-8: unpack checks every heavy overflow stack volume", Ac8UnpackCountsAllHeavyOverflowStacks);
Run("AC-9: full merge succeeds without new storage volume", Ac9FullMergeNeedsNoVolume);
Run("AC-10: unpack clears cargo slot and reduces cargo bay volume", Ac10UnpackClearsCargoSlot);
Run("AC-11: light unpack increases storage volume by 50", Ac11LightUnpackVolume);
Run("AC-12: medium unpack increases storage volume by 120", Ac12MediumUnpackVolume);

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

static bool Ac1CargoCannotEnterOnPerson()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.basic_supply_crate", "resource.basic_supply", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);
    var cargoAdd = resources.AddCargo(ResourcePool.Loaded, "cargo.basic_supply_crate", "resource.basic_supply", 30);

    var transfer = resources.Transfer(ResourcePool.Loaded, ResourcePool.OnPerson, "cargo.basic_supply_crate", 1);
    var directAdd = resources.AddCargo(ResourcePool.OnPerson, "cargo.basic_supply_crate", "resource.basic_supply", 30);

    return cargoAdd.Success
        && transfer.Result == ResourceResult.ErrKindMismatch
        && directAdd.Result == ResourceResult.ErrKindMismatch
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.basic_supply_crate") == 1
        && resources.GetQuantity(ResourcePool.OnPerson, "cargo.basic_supply_crate") == 0;
}

static bool Ac2CargoCannotEnterStorage()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.storage_rejected_crate", "resource.basic_supply", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.storage_rejected_crate", "resource.basic_supply", 30);

    var transfer = resources.Transfer(ResourcePool.Loaded, ResourcePool.InStorage, "cargo.storage_rejected_crate", 1);

    return transfer.Result == ResourceResult.ErrKindMismatch
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.storage_rejected_crate") == 1
        && resources.GetQuantity(ResourcePool.InStorage, "cargo.storage_rejected_crate") == 0;
}

static bool Ac3ResourceCannotEnterLoaded()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.loose_basic", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);

    var add = resources.Add(ResourcePool.Loaded, "resource.loose_basic", 1);

    return add.Result == ResourceResult.ErrKindMismatch
        && resources.GetQuantity(ResourcePool.Loaded, "resource.loose_basic") == 0;
}

static bool Ac4CargoMetadataExposesPositiveQuantity()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.metadata_crate", "resource.basic_supply", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.metadata_crate", "resource.basic_supply", 30);

    var cargo = resources.GetCargoItem(ResourcePool.Loaded, 0);

    return cargo is not null
        && cargo.CargoId == "cargo.metadata_crate"
        && cargo.LinkedResourceId == "resource.basic_supply"
        && cargo.ResourceQuantity > 0;
}

static bool Ac5UnpackMergesAndOverflows()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.merge_crate", "resource.basic_supply", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 90);
    resources.AddCargo(ResourcePool.Loaded, "cargo.merge_crate", "resource.basic_supply", 30);

    var unpack = resources.UnpackCargo(0);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return unpack.Success
        && unpack.MergeQuantity == 9
        && unpack.OverflowQuantity == 21
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.merge_crate") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 120
        && stacks.Select(stack => stack.Quantity).SequenceEqual([99, 21]);
}

static bool Ac6UnpackIntoEmptyStorage()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.empty_storage_crate", "resource.basic_supply", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.empty_storage_crate", "resource.basic_supply", 30);

    var unpack = resources.UnpackCargo(0);
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.basic_supply");

    return unpack.Success
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 30
        && stacks.Count == 1
        && resources.GetUsedVolume(ResourcePool.InStorage) == 50
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.empty_storage_crate") == 0;
}

static bool Ac7UnpackFailsWhenStorageFull()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.new_resource", massClass: "light");
    RegisterCargo(registry, "cargo.new_resource_crate", "resource.new_resource", massClass: "light");
    RegisterUniqueFillers(registry, "resource.full_storage_filler", count: 20, massClass: "light");
    FillUnique(resources, ResourcePool.InStorage, "resource.full_storage_filler", count: 20);
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.new_resource_crate", "resource.new_resource", 10);
    var beforeStorage = resources.GetUsedVolume(ResourcePool.InStorage);

    var unpack = resources.UnpackCargo(0);

    return beforeStorage == 1000
        && unpack.Result == ResourceResult.ErrStorageFull
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.new_resource_crate") == 1
        && resources.GetQuantity(ResourcePool.InStorage, "resource.new_resource") == 0
        && resources.GetUsedVolume(ResourcePool.InStorage) == beforeStorage;
}

static bool Ac8UnpackCountsAllHeavyOverflowStacks()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.heavy_resource", massClass: "heavy");
    RegisterCargo(registry, "cargo.heavy_crate", "resource.heavy_resource", massClass: "heavy");
    RegisterUniqueFillers(registry, "resource.heavy_storage_filler", count: 3, massClass: "heavy");
    FillUnique(resources, ResourcePool.InStorage, "resource.heavy_storage_filler", count: 3);
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.heavy_crate", "resource.heavy_resource", 200);
    var beforeStorage = resources.GetUsedVolume(ResourcePool.InStorage);

    var unpack = resources.UnpackCargo(0);

    return beforeStorage == 600
        && unpack.Result == ResourceResult.ErrStorageFull
        && unpack.OverflowQuantity == 200
        && resources.GetQuantity(ResourcePool.Loaded, "cargo.heavy_crate") == 1
        && resources.GetQuantity(ResourcePool.InStorage, "resource.heavy_resource") == 0
        && resources.GetUsedVolume(ResourcePool.InStorage) == beforeStorage;
}

static bool Ac9FullMergeNeedsNoVolume()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.merge_only_crate", "resource.basic_supply", massClass: "light");
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 90);
    RegisterUniqueFillers(registry, "resource.merge_storage_filler", count: 4, massClass: "heavy");
    FillUnique(resources, ResourcePool.InStorage, "resource.merge_storage_filler", count: 4);
    RegisterUniqueFillers(registry, "resource.merge_storage_light_filler", count: 3, massClass: "light");
    FillUnique(resources, ResourcePool.InStorage, "resource.merge_storage_light_filler", count: 3);
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.merge_only_crate", "resource.basic_supply", 5);
    var beforeStorage = resources.GetUsedVolume(ResourcePool.InStorage);

    var unpack = resources.UnpackCargo(0);

    return beforeStorage == 1000
        && unpack.Success
        && unpack.MergeQuantity == 5
        && unpack.OverflowQuantity == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 95
        && resources.GetUsedVolume(ResourcePool.InStorage) == beforeStorage;
}

static bool Ac10UnpackClearsCargoSlot()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.slot_clear_crate", "resource.basic_supply", massClass: "medium");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.slot_clear_crate", "resource.basic_supply", 10);
    var beforeLoadedVolume = resources.GetUsedVolume(ResourcePool.Loaded);

    var unpack = resources.UnpackCargo(0);

    return beforeLoadedVolume == 120
        && unpack.Success
        && resources.GetStacks(ResourcePool.Loaded).Count == 0
        && resources.GetUsedVolume(ResourcePool.Loaded) == 0;
}

static bool Ac11LightUnpackVolume()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.light_unpack", massClass: "light");
    RegisterCargo(registry, "cargo.light_unpack_crate", "resource.light_unpack", massClass: "light");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.light_unpack_crate", "resource.light_unpack", 10);
    var beforeStorage = resources.GetUsedVolume(ResourcePool.InStorage);

    var unpack = resources.UnpackCargo(0);

    return beforeStorage == 0
        && unpack.Success
        && resources.GetUsedVolume(ResourcePool.InStorage) == 50;
}

static bool Ac12MediumUnpackVolume()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.medium_unpack", massClass: "medium");
    RegisterCargo(registry, "cargo.medium_unpack_crate", "resource.medium_unpack", massClass: "medium");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.medium_unpack_crate", "resource.medium_unpack", 10);
    var beforeStorage = resources.GetUsedVolume(ResourcePool.InStorage);

    var unpack = resources.UnpackCargo(0);

    return beforeStorage == 0
        && unpack.Success
        && resources.GetUsedVolume(ResourcePool.InStorage) == 120;
}

static (Registry Registry, ResourcesManager Resources) MakeResources()
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    return (registry, resources);
}

static void RegisterUniqueFillers(Registry registry, string prefix, int count, string massClass)
{
    for (var i = 0; i < count; i++)
    {
        var id = $"{prefix}_{i}";
        RegisterResource(registry, id, stackRule: "unique", maxStack: 1, supplyClass: "intel", massClass: massClass);
    }
}

static void FillUnique(ResourcesManager resources, ResourcePool pool, string prefix, int count)
{
    for (var i = 0; i < count; i++)
    {
        resources.Add(pool, $"{prefix}_{i}", 1);
    }
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
