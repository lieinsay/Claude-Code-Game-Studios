using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #13 Story 005: Persistence & State Recovery ===");

var failed = 0;
var total = 0;

Run("AC-1: serialization stores state and deposited but omits repair_progress", Ac1SerializeOmitsProgress);
Run("AC-2: serialization includes every runtime node", Ac2SerializeAllNodes);
Run("AC-3: deserialization restores known state and recomputes progress", Ac3DeserializeKnown);
Run("AC-4: deserialization preserves repaired terminal state", Ac4DeserializeRepaired);
Run("AC-5: mid-batch save/load keeps deposited and progress", Ac5MidBatchRoundTrip);
Run("AC-6: restored mid-batch state can continue to completion", Ac6ContinueAfterLoad);
Run("AC-7: Pool 6 cross-validation rebuilds missing deposited counters", Ac7Pool6Rebuild);
Run("AC-8: Pool 6 cross-validation leaves matching counters unchanged", Ac8Pool6Match);
Run("AC-9: completed checkpoint snapshot contains repaired state", Ac9CheckpointRepaired);
Run("AC-10: unknown material in deposited is preserved", Ac10UnknownMaterialPreserved);
Run("AC-11: orphan node state is retained but deposit rejects invalid_node", Ac11OrphanRetainedInvalidForSubmit);
Run("AC-12: persistence serializer registers and saves progress.world-repair", Ac12PersistenceRegistration);
Run("AC-13: restore keeps current registry nodes missing from old snapshots", Ac13RestoreSeedsMissingRegistryNodes);

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

static WorldRepair MakeRepair()
{
    var registry = new Registry();
    registry.InitializeContent();
    var repair = new WorldRepair(registry);
    repair.Initialize();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    repair.SetCommitDepositHandler((_, offer) => ResourceOperationResult.Ok(offer.Values.Sum()));
    return repair;
}

static Dictionary<string, object?> Snapshot(params (string NodeId, RepairState State, Dictionary<string, int> Deposited)[] nodes)
{
    return new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["domain_id"] = "progress.world-repair",
        ["nodes"] = nodes.ToDictionary(
            node => node.NodeId,
            node => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["repair_state"] = (int)node.State,
                ["deposited"] = node.Deposited.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal),
            },
            StringComparer.Ordinal),
    };
}

static bool Nearly(double actual, double expected)
{
    return Math.Abs(actual - expected) < 0.000001d;
}

static bool Ac1SerializeOmitsProgress()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 2,
        ["resource.basic_supply"] = 0,
    });

    var snapshot = repair.SerializeWorldRepair();
    var nodes = (IReadOnlyDictionary<string, object?>)snapshot["nodes"]!;
    var node = (IReadOnlyDictionary<string, object?>)nodes[WorldRepair.MvpNodeId]!;
    var deposited = (IReadOnlyDictionary<string, object?>)node["deposited"]!;

    return (int)node["repair_state"]! == (int)RepairState.Known
        && Convert.ToInt32(deposited["resource.repair_kit"]) == 2
        && !node.ContainsKey("repair_progress");
}

static bool Ac2SerializeAllNodes()
{
    var repair = MakeRepair();
    repair.RegisterRepairNode("repair_node.future", new Dictionary<string, int> { ["resource.basic_supply"] = 1 });

    var nodes = (IReadOnlyDictionary<string, object?>)repair.SerializeWorldRepair()["nodes"]!;

    return nodes.ContainsKey(WorldRepair.MvpNodeId)
        && nodes.ContainsKey("repair_node.future");
}

static bool Ac3DeserializeKnown()
{
    var repair = MakeRepair();

    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Known, new() { ["resource.repair_kit"] = 3 })));

    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known
        && repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 3
        && Nearly(repair.GetRepairProgress(WorldRepair.MvpNodeId), 0.375d);
}

static bool Ac4DeserializeRepaired()
{
    var repair = MakeRepair();

    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Repaired, new()
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    })));

    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired
        && repair.GetCompletedNodes().Contains(WorldRepair.MvpNodeId);
}

static bool Ac5MidBatchRoundTrip()
{
    var repair = MakeRepair();
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int> { ["resource.repair_kit"] = 3 });
    var snapshot = repair.SerializeWorldRepair();

    var loaded = MakeRepair();
    loaded.DeserializeWorldRepair(snapshot);

    return loaded.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known
        && loaded.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 3
        && Nearly(loaded.GetRepairProgress(WorldRepair.MvpNodeId), 0.375d);
}

static bool Ac6ContinueAfterLoad()
{
    var repair = MakeRepair();
    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Known, new() { ["resource.repair_kit"] = 3 })));
    repair.SetCommitDepositHandler((_, offer) => ResourceOperationResult.Ok(offer.Values.Sum()));

    var result = repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 1,
        ["resource.basic_supply"] = 4,
    });

    return result.Completed
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired
        && Nearly(result.Progress, 1.0d);
}

static bool Ac7Pool6Rebuild()
{
    var repair = MakeRepair();
    repair.SetPool6DepositQuery(nodeId => nodeId == WorldRepair.MvpNodeId
        ? new Dictionary<string, int> { ["resource.repair_kit"] = 3, ["resource.basic_supply"] = 2 }
        : new Dictionary<string, int>());

    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Known, new() { ["resource.repair_kit"] = 1 })));

    var deposited = repair.GetDeposited(WorldRepair.MvpNodeId);
    return deposited["resource.repair_kit"] == 3
        && deposited["resource.basic_supply"] == 2
        && repair.Warnings.Any(warning => warning.Contains("mismatch", StringComparison.Ordinal));
}

static bool Ac8Pool6Match()
{
    var repair = MakeRepair();
    repair.SetPool6DepositQuery(_ => new Dictionary<string, int> { ["resource.repair_kit"] = 3 });

    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Known, new() { ["resource.repair_kit"] = 3 })));

    return repair.GetDeposited(WorldRepair.MvpNodeId)["resource.repair_kit"] == 3
        && repair.Warnings.Count == 0;
}

static bool Ac9CheckpointRepaired()
{
    var repair = MakeRepair();
    Complete(repair);

    var package = repair.BuildSnapshotPackage();
    var nodes = (IReadOnlyDictionary<string, object?>)package.Payload["nodes"]!;
    var node = (IReadOnlyDictionary<string, object?>)nodes[WorldRepair.MvpNodeId]!;

    return package.DomainId == "progress.world-repair"
        && (int)node["repair_state"]! == (int)RepairState.Repaired;
}

static bool Ac10UnknownMaterialPreserved()
{
    var repair = MakeRepair();

    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Known, new()
    {
        ["resource.removed_future"] = 7,
    })));

    return repair.GetDeposited(WorldRepair.MvpNodeId)["resource.removed_future"] == 7;
}

static bool Ac11OrphanRetainedInvalidForSubmit()
{
    var repair = MakeRepair();

    repair.DeserializeWorldRepair(Snapshot(("repair_node.orphan", RepairState.Known, new() { ["resource.repair_kit"] = 1 })));
    var result = repair.SubmitDeposit("repair_node.orphan", new Dictionary<string, int> { ["resource.repair_kit"] = 1 });

    return repair.GetRepairState("repair_node.orphan") == RepairState.Known
        && result.Result == RepairSubmitResult.ErrValidationFailed
        && result.Violations.Contains(RepairDepositViolation.InvalidNode);
}

static bool Ac12PersistenceRegistration()
{
    var repair = MakeRepair();
    var persistence = new Persistence();
    repair.RegisterPersistence(persistence);

    var result = persistence.RequestSaveProgress();

    return result.Success && result.Generation == 1;
}

static bool Ac13RestoreSeedsMissingRegistryNodes()
{
    var repair = MakeRepair();
    repair.RegisterRepairNode("repair_node.future", new Dictionary<string, int> { ["resource.basic_supply"] = 1 });

    repair.DeserializeWorldRepair(Snapshot((WorldRepair.MvpNodeId, RepairState.Known, new() { ["resource.repair_kit"] = 2 })));

    return repair.GetRepairNodeIds().Contains("repair_node.future")
        && repair.GetRepairState("repair_node.future") == RepairState.Unrevealed
        && repair.GetDeposited("repair_node.future").Count == 0
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known;
}

static void Complete(WorldRepair repair)
{
    repair.SubmitDeposit(WorldRepair.MvpNodeId, new Dictionary<string, int>
    {
        ["resource.repair_kit"] = 4,
        ["resource.basic_supply"] = 4,
    });
}
