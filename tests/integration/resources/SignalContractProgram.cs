using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 008: Signal Contract & Reentry Guard - Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: add emits resource_added then pool_changed", Ac1AddSignals);
Run("AC-2: remove emits resource_removed then pool_changed", Ac2RemoveSignals);
Run("AC-3: transfer emits transfer_completed and both pool_changed signals", Ac3TransferSignals);
Run("AC-4: unpack emits cargo_unpacked and pool_changed signals", Ac4UnpackSignals);
Run("AC-5: commit deposit emits deposit_committed", Ac5DepositCommitted);
Run("AC-6: loaded add emits mass_changed", Ac6MassChanged);
Run("AC-7: failed add emits no state signals", Ac7FailedAddNoSignals);
Run("AC-8: failed transfer emits no transfer or pool_changed signals", Ac8FailedTransferNoSignals);
Run("AC-9: transfer_completed precedes pool_changed", Ac9TransferOrder);
Run("AC-10: pool_changed sees post-mutation storage summary", Ac10PoolChangedSeesNewState);
Run("AC-11: resource_added sees post-mutation storage summary", Ac11ResourceAddedSeesNewState);
Run("AC-12: mutation during signal returns ERR_BUSY", Ac12ReentryMutationBusy);
Run("AC-13: query during signal succeeds", Ac13QueryDuringSignalAllowed);
Run("AC-14: failed deposit emits deposit_failed with reason", Ac14DepositFailedSignal);
Run("AC-15: discard emits resource_removed and pool_changed", Ac15DiscardSignals);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 008 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 008 validation passed: {total}/{total} checks passed.");
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

static bool Ac1AddSignals()
{
    var resources = MakeResources().Resources;
    var recorder = AttachRecorder(resources);

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 5);

    return result.Success
        && recorder.ResourceAdded.SequenceEqual([(ResourcePool.InStorage, "resource.basic_supply", 5)])
        && recorder.PoolChanged.SequenceEqual([ResourcePool.InStorage]);
}

static bool Ac2RemoveSignals()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 10);
    var recorder = AttachRecorder(resources);

    var result = resources.Remove(ResourcePool.InStorage, "resource.basic_supply", 3);

    return result.Success
        && recorder.ResourceRemoved.SequenceEqual([(ResourcePool.InStorage, "resource.basic_supply", 3)])
        && recorder.PoolChanged.SequenceEqual([ResourcePool.InStorage]);
}

static bool Ac3TransferSignals()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 8);
    var recorder = AttachRecorder(resources);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 5);

    return result.Success
        && recorder.TransferCompleted.SequenceEqual([(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 5)])
        && recorder.PoolChanged.SequenceEqual([ResourcePool.InStorage, ResourcePool.OnPerson]);
}

static bool Ac4UnpackSignals()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.iron");
    RegisterCargo(registry, "cargo.iron", "resource.iron", massClass: "medium");
    resources.SetCargoModuleVolumeBonus(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.iron", "resource.iron", 30);
    var recorder = AttachRecorder(resources);

    var result = resources.UnpackCargo(0);

    return result.Success
        && recorder.CargoUnpacked.SequenceEqual([("cargo.iron", "resource.iron", 30)])
        && recorder.PoolChanged.SequenceEqual([ResourcePool.Loaded, ResourcePool.InStorage]);
}

static bool Ac5DepositCommitted()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 5);
    var recorder = AttachRecorder(resources);

    var result = resources.CommitDeposit("repair_node.starlight_dock", Costs(("resource.basic_supply", 5)));

    return result.Success
        && recorder.DepositCommitted.SequenceEqual(["repair_node.starlight_dock"]);
}

static bool Ac6MassChanged()
{
    var (registry, resources) = MakeResources();
    RegisterResource(registry, "resource.iron");
    RegisterCargo(registry, "cargo.heavy", "resource.iron", massClass: "heavy");
    resources.SetCargoModuleVolumeBonus(500);
    var recorder = AttachRecorder(resources);

    var result = resources.AddCargo(ResourcePool.Loaded, "cargo.heavy", "resource.iron", 10);

    return result.Success
        && recorder.MassChanged.SequenceEqual([6]);
}

static bool Ac7FailedAddNoSignals()
{
    var resources = MakeResources().Resources;
    var recorder = AttachRecorder(resources);

    var result = resources.Add(ResourcePool.Carried, "resource.unknown", 5);

    return !result.Success && recorder.TotalStateSignalCount == 0;
}

static bool Ac8FailedTransferNoSignals()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 1);
    var recorder = AttachRecorder(resources);

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 5);

    return result.Result == ResourceResult.ErrSourceInsufficient
        && recorder.TransferCompleted.Count == 0
        && recorder.PoolChanged.Count == 0;
}

static bool Ac9TransferOrder()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 6);
    var order = new List<string>();
    resources.TransferCompleted += (_, _, _, _) => order.Add("transfer_completed");
    resources.PoolChanged += pool => order.Add($"pool_changed:{pool}");

    var result = resources.Transfer(ResourcePool.InStorage, ResourcePool.OnPerson, "resource.basic_supply", 5);

    return result.Success
        && order.SequenceEqual(["transfer_completed", "pool_changed:InStorage", "pool_changed:OnPerson"]);
}

static bool Ac10PoolChangedSeesNewState()
{
    var resources = MakeResources().Resources;
    var seenQuantity = -1;
    resources.PoolChanged += pool =>
    {
        if (pool == ResourcePool.InStorage)
        {
            seenQuantity = resources.GetStorageSummary().GetValueOrDefault("resource.basic_supply");
        }
    };

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 5);

    return result.Success && seenQuantity == 5;
}

static bool Ac11ResourceAddedSeesNewState()
{
    var resources = MakeResources().Resources;
    var seenQuantity = -1;
    resources.ResourceAdded += (pool, _, _) =>
    {
        if (pool == ResourcePool.InStorage)
        {
            seenQuantity = resources.GetStorageSummary().GetValueOrDefault("resource.basic_supply");
        }
    };

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 5);

    return result.Success && seenQuantity == 5;
}

static bool Ac12ReentryMutationBusy()
{
    var resources = MakeResources().Resources;
    ResourceOperationResult? nested = null;
    var addedSignals = 0;
    resources.ResourceAdded += (_, _, _) =>
    {
        addedSignals++;
        nested = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 5);
    };

    var outer = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 3);

    return outer.Success
        && nested?.Result == ResourceResult.ErrBusy
        && addedSignals == 1
        && resources.GetQuantity(ResourcePool.InStorage, "resource.basic_supply") == 3;
}

static bool Ac13QueryDuringSignalAllowed()
{
    var resources = MakeResources().Resources;
    var queryWorked = false;
    resources.PoolChanged += _ => queryWorked = resources.GetStorageSummary().Count >= 0;

    var result = resources.Add(ResourcePool.InStorage, "resource.basic_supply", 1);

    return result.Success && queryWorked;
}

static bool Ac14DepositFailedSignal()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.InStorage, "resource.basic_supply", 1);
    var recorder = AttachRecorder(resources);

    var result = resources.CommitDeposit("repair_node.starlight_dock", Costs(("resource.basic_supply", 5)));

    return result.Result == ResourceResult.ErrSourceInsufficient
        && recorder.DepositFailed.SequenceEqual([("repair_node.starlight_dock", "ERR_SOURCE_INSUFFICIENT")]);
}

static bool Ac15DiscardSignals()
{
    var resources = MakeResources().Resources;
    resources.Add(ResourcePool.Carried, "resource.basic_supply", 5);
    var recorder = AttachRecorder(resources);

    var result = resources.Discard(ResourcePool.Carried, "resource.basic_supply", 3);

    return result.Success
        && recorder.ResourceRemoved.SequenceEqual([(ResourcePool.Carried, "resource.basic_supply", 3)])
        && recorder.PoolChanged.SequenceEqual([ResourcePool.Carried]);
}

static SignalRecorder AttachRecorder(ResourcesManager resources)
{
    var recorder = new SignalRecorder();
    resources.ResourceAdded += (pool, resourceId, quantity) => recorder.ResourceAdded.Add((pool, resourceId, quantity));
    resources.ResourceRemoved += (pool, resourceId, quantity) => recorder.ResourceRemoved.Add((pool, resourceId, quantity));
    resources.TransferCompleted += (from, to, resourceId, quantity) => recorder.TransferCompleted.Add((from, to, resourceId, quantity));
    resources.CargoUnpacked += (cargoId, resourceId, quantity) => recorder.CargoUnpacked.Add((cargoId, resourceId, quantity));
    resources.DepositCommitted += nodeId => recorder.DepositCommitted.Add(nodeId);
    resources.DepositFailed += (nodeId, reason) => recorder.DepositFailed.Add((nodeId, reason));
    resources.MassChanged += mass => recorder.MassChanged.Add(mass);
    resources.PoolChanged += pool => recorder.PoolChanged.Add(pool);
    return recorder;
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

static void RegisterCargo(Registry registry, string id, string linkedResourceId, string massClass)
{
    registry.RegisterContent(id, new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "cargo",
        ["owner_domain"] = "resources",
        ["status"] = "Active",
        ["name_key"] = $"content.{id}.name",
        ["description_key"] = $"content.{id}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 999,
        ["references"] = new[] { linkedResourceId },
        ["stack_rule"] = "unique",
        ["max_stack"] = 1,
        ["mass_class"] = massClass,
        ["handling_class"] = "crate",
        ["linked_resource_id"] = linkedResourceId,
    });
}

static void RegisterResource(Registry registry, string id)
{
    var key = id.Replace('.', '_').Replace('-', '_');
    registry.RegisterContent(id, new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["id"] = id,
        ["kind"] = "resource",
        ["owner_domain"] = "resources",
        ["status"] = "Active",
        ["name_key"] = $"content.{key}.name",
        ["description_key"] = $"content.{key}.desc",
        ["schema_version"] = 1,
        ["tags"] = new[] { "test" },
        ["sort_order"] = 998,
        ["references"] = Array.Empty<string>(),
        ["unit"] = "crate",
        ["stack_rule"] = "stackable",
        ["material_tags"] = new[] { "test-material" },
        ["supply_class"] = "basic",
        ["max_stack"] = 99,
        ["mass_class"] = "light",
    });
}

sealed class SignalRecorder
{
    public List<(ResourcePool Pool, string ResourceId, int Quantity)> ResourceAdded { get; } = [];
    public List<(ResourcePool Pool, string ResourceId, int Quantity)> ResourceRemoved { get; } = [];
    public List<(ResourcePool From, ResourcePool To, string ResourceId, int Quantity)> TransferCompleted { get; } = [];
    public List<(string CargoId, string ResourceId, int Quantity)> CargoUnpacked { get; } = [];
    public List<string> DepositCommitted { get; } = [];
    public List<(string NodeId, string Reason)> DepositFailed { get; } = [];
    public List<int> MassChanged { get; } = [];
    public List<ResourcePool> PoolChanged { get; } = [];
    public int TotalStateSignalCount =>
        ResourceAdded.Count
        + ResourceRemoved.Count
        + TransferCompleted.Count
        + CargoUnpacked.Count
        + DepositCommitted.Count
        + MassChanged.Count
        + PoolChanged.Count;
}
