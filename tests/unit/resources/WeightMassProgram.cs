using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 004: Weight & Mass Tracking - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: only loaded cargo contributes to total mass", Ac1OnlyLoadedCargoContributes);
Run("AC-2: empty cargo bay mass is zero", Ac2EmptyLoadedMassIsZero);
Run("AC-3: ten light cargo items weigh ten", Ac3TenLightCargoItemsWeighTen);
Run("AC-4: removing heavy cargo reduces mass by six", Ac4RemovingHeavyCargoReducesMass);
Run("AC-5: adding heavy cargo increases mass from zero to six", Ac5AddingHeavyCargoIncreasesMass);
Run("AC-6: unpacking one medium cargo reduces mass from six to three", Ac6UnpackMediumCargoReducesMass);
Run("AC-7: ResourcesManager does not block overweight cargo loading", Ac7OverweightCargoLoadingSucceeds);
Run("AC-8: total loaded mass exposes overweight values", Ac8OverweightValueIsQueryable);
Run("AC-9: mass is recomputed from current mass_class mapping", Ac9MassIgnoresPersistedOrCachedValues);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 004 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 004 validation passed: {total}/{total} checks passed.");
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

static bool Ac1OnlyLoadedCargoContributes()
{
    var (registry, resources) = MakeResources(cargoVolume: 2000);
    RegisterResource(registry, "resource.storage_heavy", massClass: "heavy");
    RegisterResource(registry, "resource.person_medium", massClass: "medium");
    RegisterCargo(registry, "cargo.loaded_light_a", "resource.basic_supply", "light");
    RegisterCargo(registry, "cargo.loaded_light_b", "resource.basic_supply", "light");
    RegisterCargo(registry, "cargo.loaded_medium", "resource.basic_supply", "medium");
    RegisterCargo(registry, "cargo.loaded_heavy", "resource.basic_supply", "heavy");

    resources.AddCargo(ResourcePool.Loaded, "cargo.loaded_light_a", "resource.basic_supply", 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.loaded_light_b", "resource.basic_supply", 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.loaded_medium", "resource.basic_supply", 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.loaded_heavy", "resource.basic_supply", 1);
    resources.Add(ResourcePool.InStorage, "resource.storage_heavy", 5);
    resources.Add(ResourcePool.OnPerson, "resource.person_medium", 3);

    return resources.GetTotalLoadedMass() == 11;
}

static bool Ac2EmptyLoadedMassIsZero()
{
    var (_, resources) = MakeResources();
    return resources.GetTotalLoadedMass() == 0;
}

static bool Ac3TenLightCargoItemsWeighTen()
{
    var (registry, resources) = MakeResources(cargoVolume: 500);
    RegisterCargo(registry, "cargo.light_bulk", "resource.basic_supply", "light");

    var add = resources.Add(ResourcePool.Loaded, "cargo.light_bulk", 10);

    return add.Success
        && resources.GetStacks(ResourcePool.Loaded, "cargo.light_bulk").Count == 10
        && resources.GetTotalLoadedMass() == 10;
}

static bool Ac4RemovingHeavyCargoReducesMass()
{
    var (registry, resources) = MakeResources(cargoVolume: 500);
    RegisterCargo(registry, "cargo.remove_heavy", "resource.basic_supply", "heavy");
    resources.AddCargo(ResourcePool.Loaded, "cargo.remove_heavy", "resource.basic_supply", 1);
    var before = resources.GetTotalLoadedMass();

    var remove = resources.Remove(ResourcePool.Loaded, "cargo.remove_heavy", 1);

    return before == 6
        && remove.Success
        && resources.GetTotalLoadedMass() == 0;
}

static bool Ac5AddingHeavyCargoIncreasesMass()
{
    var (registry, resources) = MakeResources(cargoVolume: 500);
    RegisterCargo(registry, "cargo.add_heavy", "resource.basic_supply", "heavy");
    var before = resources.GetTotalLoadedMass();

    var add = resources.Add(ResourcePool.Loaded, "cargo.add_heavy", 1);

    return before == 0
        && add.Success
        && resources.GetTotalLoadedMass() == 6;
}

static bool Ac6UnpackMediumCargoReducesMass()
{
    var (registry, resources) = MakeResources(cargoVolume: 500);
    RegisterResource(registry, "resource.medium_unpack_mass", massClass: "medium");
    RegisterCargo(registry, "cargo.medium_unpack_mass", "resource.medium_unpack_mass", "medium");
    resources.AddCargo(ResourcePool.Loaded, "cargo.medium_unpack_mass", "resource.medium_unpack_mass", 10);
    resources.AddCargo(ResourcePool.Loaded, "cargo.medium_unpack_mass", "resource.medium_unpack_mass", 10);
    var before = resources.GetTotalLoadedMass();

    var unpack = resources.UnpackCargo(0);

    return before == 6
        && unpack.Success
        && resources.GetTotalLoadedMass() == 3;
}

static bool Ac7OverweightCargoLoadingSucceeds()
{
    var (registry, resources) = MakeResources(cargoVolume: 1000);
    RegisterCargoSet(registry, "overload");
    LoadMassTwenty(resources, "overload");

    var add = resources.Add(ResourcePool.Loaded, "cargo.overload_heavy_extra", 1);

    return add.Success
        && resources.GetTotalLoadedMass() == 26;
}

static bool Ac8OverweightValueIsQueryable()
{
    var (registry, resources) = MakeResources(cargoVolume: 1000);
    RegisterCargoSet(registry, "query_overload");
    LoadMassTwenty(resources, "query_overload");
    resources.Add(ResourcePool.Loaded, "cargo.query_overload_heavy_extra", 1);

    var mass = resources.GetTotalLoadedMass();

    return mass == 26 && mass > 25;
}

static bool Ac9MassIgnoresPersistedOrCachedValues()
{
    var (registry, resources) = MakeResources(cargoVolume: 500);
    RegisterCargo(registry, "cargo.stale_mass_note", "resource.basic_supply", "medium", storedMass: 999);

    var add = resources.Add(ResourcePool.Loaded, "cargo.stale_mass_note", 1);

    return add.Success
        && resources.GetTotalLoadedMass() == 3;
}

static (Registry Registry, ResourcesManager Resources) MakeResources(int cargoVolume = 0)
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    resources.SetCargoModuleVolumeBonus(cargoVolume);
    return (registry, resources);
}

static void RegisterCargoSet(Registry registry, string suffix)
{
    RegisterCargo(registry, $"cargo.{suffix}_heavy_a", "resource.basic_supply", "heavy");
    RegisterCargo(registry, $"cargo.{suffix}_heavy_b", "resource.basic_supply", "heavy");
    RegisterCargo(registry, $"cargo.{suffix}_medium_a", "resource.basic_supply", "medium");
    RegisterCargo(registry, $"cargo.{suffix}_medium_b", "resource.basic_supply", "medium");
    RegisterCargo(registry, $"cargo.{suffix}_light_a", "resource.basic_supply", "light");
    RegisterCargo(registry, $"cargo.{suffix}_light_b", "resource.basic_supply", "light");
    RegisterCargo(registry, $"cargo.{suffix}_heavy_extra", "resource.basic_supply", "heavy");
}

static void LoadMassTwenty(ResourcesManager resources, string suffix)
{
    resources.Add(ResourcePool.Loaded, $"cargo.{suffix}_heavy_a", 1);
    resources.Add(ResourcePool.Loaded, $"cargo.{suffix}_heavy_b", 1);
    resources.Add(ResourcePool.Loaded, $"cargo.{suffix}_medium_a", 1);
    resources.Add(ResourcePool.Loaded, $"cargo.{suffix}_medium_b", 1);
    resources.Add(ResourcePool.Loaded, $"cargo.{suffix}_light_a", 1);
    resources.Add(ResourcePool.Loaded, $"cargo.{suffix}_light_b", 1);
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
    string massClass,
    int? storedMass = null)
{
    registry.RegisterContent(id, ValidCargo(id, linkedResourceId, massClass, storedMass));
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

static Dictionary<string, object?> ValidCargo(
    string id,
    string linkedResourceId,
    string massClass,
    int? storedMass)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    var cargo = new Dictionary<string, object?>(StringComparer.Ordinal)
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

    if (storedMass is not null)
    {
        cargo["stored_mass"] = storedMass.Value;
    }

    return cargo;
}
