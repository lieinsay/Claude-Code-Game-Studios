using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 009: Persistence & External Integration - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: serialize resources includes persisted pool structures", Ac1SerializePools);
Run("AC-2: snapshot payload contains only stable data", Ac2SnapshotPayloadStableOnly);
Run("AC-3: deserialize restores pool state", Ac3DeserializeRoundTrip);
Run("AC-4: deserialize restores carry capacity bonus", Ac4CarryBonusRestored);
Run("AC-5: reset for new game applies provided starting snapshot", Ac5ResetProvidedSnapshot);
Run("AC-6: reset clears previous runtime data", Ac6ResetClearsOldData);
Run("AC-7: cargo bay usage reports occupied volume and stacks", Ac7CargoBayUsage);
Run("AC-8: cargo bay destruction creates losses and crates", Ac8CargoBayDestroyed);
Run("AC-9: empty cargo bay usage allows module removal", Ac9EmptyCargoBayUsage);
Run("AC-10: carried intel query returns intel only", Ac10CarriedIntel);
Run("AC-11: carried tag query returns matching repair material", Ac11CarriedTagQuery);
Run("AC-12: missing carried tag query returns empty", Ac12MissingTagQuery);
Run("AC-13: default new-game bootstrap matches MVP start", Ac13DefaultStartingState);
Run("AC-14: max_stack migration splits or logs overflow", Ac14MaxStackMigration);
Run("AC-15: mass_class migration recalculates cargo capacity", Ac15MassClassMigration);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 009 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 009 validation passed: {total}/{total} checks passed.");
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

static bool Ac1SerializePools()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.iron");
    RegisterCargo(registry, "cargo.iron", "resource.iron", "medium");
    resources.SetCargoModuleVolumeBonus(500);
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    resources.AddCargo(ResourcePool.Loaded, "cargo.iron", "resource.iron", 30);

    var snapshot = resources.BuildProgressResourcesSnapshot();
    var pools = Map(snapshot, "pools");

    return (string)snapshot["domain"]! == "resources"
        && pools.ContainsKey("on_person")
        && pools.ContainsKey("in_storage")
        && pools.ContainsKey("loaded")
        && List(pools, "in_storage").Count == 1
        && List(pools, "loaded").Count == 1;
}

static bool Ac2SnapshotPayloadStableOnly()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);

    var package = resources.BuildSnapshotPackage();
    var validation = package.ValidateContract();
    var encoded = Persistence.CanonicalJsonEncode(package.ToDictionary());

    return validation.Valid
        && !encoded.Contains("display", StringComparison.OrdinalIgnoreCase)
        && !encoded.Contains(".tscn", StringComparison.OrdinalIgnoreCase)
        && !encoded.Contains("System.Object", StringComparison.Ordinal);
}

static bool Ac3DeserializeRoundTrip()
{
    var (registry, source) = MakeResources();
    RegisterResource(registry, "resource.iron");
    RegisterCargo(registry, "cargo.roundtrip", "resource.iron", "medium");
    source.SetCargoModuleVolumeBonus(500);
    source.Add(ResourcePool.OnPerson, "resource.repair_kit", 4);
    source.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    source.AddCargo(ResourcePool.Loaded, "cargo.roundtrip", "resource.iron", 30);

    var target = new ResourcesManager(registry);
    var restored = target.RestoreFromProgressResources(source.BuildProgressResourcesSnapshot());

    return restored
        && target.GetQuantity(ResourcePool.OnPerson, "resource.repair_kit") == 4
        && target.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 10
        && target.GetQuantity(ResourcePool.Loaded, "cargo.roundtrip") == 1
        && target.GetCargoItem(ResourcePool.Loaded, 0)?.LinkedResourceId == "resource.iron";
}

static bool Ac4CarryBonusRestored()
{
    var resources = MakeResources().Resources;
    var restored = resources.RestoreFromProgressResources(Snapshot(
        Pools(),
        Bonuses(carrySlotBonus: 2)));

    return restored && resources.GetCarryCapacity() == 7;
}

static bool Ac5ResetProvidedSnapshot()
{
    var resources = MakeResources().Resources;
    resources.SetCargoModuleVolumeBonus(500);
    resources.Add(ResourcePool.OnPerson, "resource.basic_supply", 5);

    var restored = resources.ResetForNewGame(Snapshot(Pools(
        storage: [Stack(0, "resource.basic_supply", 10), Stack(1, "resource.repair_kit", 4)])));

    return restored
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 10
        && resources.GetQuantity(ResourcePool.InStorage, "resource.repair_kit") == 4
        && resources.GetStacks(ResourcePool.OnPerson).Count == 0
        && resources.GetStacks(ResourcePool.Loaded).Count == 0
        && resources.GetCargoBayCapacity() == 0;
}

static bool Ac6ResetClearsOldData()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 99);
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 3);

    resources.ResetForNewGame(Snapshot(Pools(storage: [Stack(0, "resource.repair_kit", 4)])));

    return resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 0
        && resources.GetQuantity(ResourcePool.InStorage, "resource.repair_kit") == 4
        && resources.GetStacks(ResourcePool.Carried).Count == 0;
}

static bool Ac7CargoBayUsage()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.iron");
    RegisterCargo(registry, "cargo.usage", "resource.iron", "medium");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.usage", "resource.iron", 30);

    var usage = resources.GetCargoBayUsage();
    var stacks = (List<object?>)usage["stacks"]!;

    return (int)usage["used_volume"]! == 120
        && stacks.Count == 1
        && ((Dictionary<string, object?>)stacks[0]!)["resource_id"]!.ToString() == "cargo.usage";
}

static bool Ac8CargoBayDestroyed()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.loss_a", "resource.iron", "light", stackRule: "stackable", maxStack: 99);
    RegisterCargo(registry, "cargo.loss_b", "resource.iron", "light", stackRule: "stackable", maxStack: 99);
    RegisterCargo(registry, "cargo.loss_c", "resource.iron", "light", stackRule: "stackable", maxStack: 99);
    resources.RestoreFromProgressResources(Snapshot(
        Pools(loaded:
        [
            Stack(0, "cargo.loss_a", 10, "resource.iron", 30),
            Stack(1, "cargo.loss_b", 1, "resource.iron", 30),
            Stack(2, "cargo.loss_c", 3, "resource.iron", 30),
        ]),
        Bonuses(cargoModuleVolumeBonus: 500)));

    var result = resources.HandleCargoBayModuleDestroyed();

    return result.Success
        && result.Losses.Sum(loss => loss.LossQuantity) == 6
        && result.Crates.Sum(crate => crate.Quantity) == 8
        && resources.GetUsedVolume(ResourcePool.Loaded) == 0
        && resources.GetCargoBayCapacity() == 0;
}

static bool Ac9EmptyCargoBayUsage()
{
    var resources = MakeResources().Resources;
    resources.SetCargoModuleVolumeBonus(500);

    var usage = resources.GetCargoBayUsage();

    return (int)usage["used_volume"]! == 0
        && ((List<object?>)usage["stacks"]!).Count == 0;
}

static bool Ac10CarriedIntel()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.test_intel", supplyClass: "intel", stackRule: "unique", maxStack: 1);
    resources.Add(ResourcePool.Carried, "resource.basic_supply", 5);
    resources.Add(ResourcePool.Carried, "resource.test_intel", 1);
    resources.Add(ResourcePool.Carried, "resource.repair_kit", 3);

    var intel = resources.GetCarriedIntel();

    return intel.Count == 1
        && intel.GetValueOrDefault("resource.test_intel") == 1;
}

static bool Ac11CarriedTagQuery()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.tagged_repair", tags: ["repair-material"]);
    RegisterResource(registry, "resource.tagged_basic", tags: ["basic-supply"]);
    resources.Add(ResourcePool.Carried, "resource.tagged_repair", 3);
    resources.Add(ResourcePool.Carried, "resource.tagged_basic", 5);

    var repair = resources.GetCarriedContentsByTag("repair-material");

    return repair.Count == 1
        && repair.GetValueOrDefault("resource.tagged_repair") == 3;
}

static bool Ac12MissingTagQuery()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.basic_supply", 5);

    return resources.GetCarriedContentsByTag("nonexistent").Count == 0;
}

static bool Ac13DefaultStartingState()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 99);

    var reset = resources.ResetForNewGame();

    return reset
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 10
        && resources.GetQuantity(ResourcePool.InStorage, "resource.repair_kit") == 4
        && resources.GetCargoBayCapacity() == 500
        && resources.GetStacks(ResourcePool.OnPerson).Count == 0
        && resources.GetStacks(ResourcePool.Loaded).Count == 0;
}

static bool Ac14MaxStackMigration()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.migrated_basic", maxStack: 50);
    RegisterResource(registry, "resource.migration_filler", stackRule: "unique", maxStack: 1);
    var fillers = Enumerable.Range(0, 19)
        .Select(index => Stack(index, "resource.migration_filler", 1))
        .ToArray();
    var storage = fillers.Concat([Stack(19, "resource.migrated_basic", 80)]).ToArray();

    resources.RestoreFromProgressResources(Snapshot(Pools(storage: storage)));
    var stacks = resources.GetStacks(ResourcePool.InStorage, "resource.migrated_basic");

    return stacks.Select(stack => stack.Quantity).SequenceEqual([50])
        && resources.MigrationLog.Any(entry =>
            entry.ResourceId == "resource.migrated_basic"
            && entry.Quantity == 30
            && entry.ReasonCode == "ERR_RESTORE_CAPACITY_OVERFLOW");
}

static bool Ac15MassClassMigration()
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.heavy_after_migration", "resource.iron", "heavy");

    resources.RestoreFromProgressResources(Snapshot(
        Pools(loaded:
        [
            Stack(0, "cargo.heavy_after_migration", 1, "resource.iron", 30),
            Stack(1, "cargo.heavy_after_migration", 1, "resource.iron", 30),
            Stack(2, "cargo.heavy_after_migration", 1, "resource.iron", 30),
        ]),
        Bonuses(cargoModuleVolumeBonus: 500)));

    return resources.GetQuantity(ResourcePool.Loaded, "cargo.heavy_after_migration") == 2
        && resources.GetUsedVolume(ResourcePool.Loaded) == 400
        && resources.MigrationLog.Any(entry =>
            entry.ResourceId == "cargo.heavy_after_migration"
            && entry.Quantity == 1);
}

static (Registry Registry, ResourcesManager Resources) MakeResources()
{
    var registry = new Registry();
    registry.InitializeContent();
    var resources = new ResourcesManager(registry);
    resources.Initialize();
    return (registry, resources);
}

static Dictionary<string, object?> Snapshot(
    Dictionary<string, object?> pools,
    Dictionary<string, object?>? bonuses = null)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["domain"] = "resources",
        ["version"] = 1,
        ["pools"] = pools,
        ["bonuses"] = bonuses ?? Bonuses(),
    };
}

static Dictionary<string, object?> Pools(
    Dictionary<string, object?>[]? onPerson = null,
    Dictionary<string, object?>[]? storage = null,
    Dictionary<string, object?>[]? loaded = null)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["on_person"] = (onPerson ?? []).Cast<object?>().ToList(),
        ["in_storage"] = (storage ?? []).Cast<object?>().ToList(),
        ["loaded"] = (loaded ?? []).Cast<object?>().ToList(),
    };
}

static Dictionary<string, object?> Bonuses(
    int carrySlotBonus = 0,
    int storageVolumeBonus = 0,
    int cargoModuleVolumeBonus = 0)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["carry_slot_bonus"] = carrySlotBonus,
        ["carry_volume_bonus"] = 0,
        ["storage_volume_bonus"] = storageVolumeBonus,
        ["cargo_module_volume_bonus"] = cargoModuleVolumeBonus,
    };
}

static Dictionary<string, object?> Stack(
    int slot,
    string resourceId,
    int quantity,
    string linkedResourceId = "",
    int resourceQuantity = 0)
{
    var stack = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["slot_index"] = slot,
        ["resource_id"] = resourceId,
        ["quantity"] = quantity,
    };
    if (!string.IsNullOrWhiteSpace(linkedResourceId))
    {
        stack["linked_resource_id"] = linkedResourceId;
    }

    if (resourceQuantity > 0)
    {
        stack["resource_quantity"] = resourceQuantity;
    }

    return stack;
}

static IReadOnlyDictionary<string, object?> Map(IReadOnlyDictionary<string, object?> data, string key)
{
    return (IReadOnlyDictionary<string, object?>)data[key]!;
}

static List<object?> List(IReadOnlyDictionary<string, object?> data, string key)
{
    return (List<object?>)data[key]!;
}

static void RegisterResource(
    Registry registry,
    string id,
    string stackRule = "stackable",
    int maxStack = 99,
    string supplyClass = "basic",
    string massClass = "light",
    string[]? tags = null)
{
    registry.RegisterContent(id, ResourceDefinition(id, stackRule, maxStack, supplyClass, massClass, tags ?? []));
}

static void RegisterCargo(
    Registry registry,
    string id,
    string linkedResourceId,
    string massClass,
    string stackRule = "unique",
    int maxStack = 1)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    registry.RegisterContent(id, new Dictionary<string, object?>(StringComparer.Ordinal)
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
        ["stack_rule"] = stackRule,
        ["max_stack"] = maxStack,
    });
}

static Dictionary<string, object?> ResourceDefinition(
    string id,
    string stackRule,
    int maxStack,
    string supplyClass,
    string massClass,
    string[] tags)
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
        ["max_stack"] = maxStack,
        ["supply_class"] = supplyClass,
        ["mass_class"] = massClass,
        ["material_tags"] = tags.Length == 0 ? new[] { "test-material" } : tags,
    };
}
