using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #13 Story 001: Repair State Machine & Node Lifecycle ===");

var failed = 0;
var total = 0;

Run("AC-1: new game initializes starlight_dock as unrevealed with empty progress", Ac1NewGameInitialState);
Run("AC-2: physical arrival promotes unrevealed to known without intel gate", Ac2PhysicalArrivalReveals);
Run("AC-3: intel reveal promotes unrevealed to known", Ac3IntelRevealReveals);
Run("AC-4: known can transition to repaired", Ac4KnownToRepaired);
Run("AC-5: known cannot regress to unrevealed", Ac5KnownCannotRegress);
Run("AC-6: repaired cannot regress to known", Ac6RepairedCannotRegressKnown);
Run("AC-7: repaired cannot regress to unrevealed", Ac7RepairedCannotRegressUnrevealed);
Run("AC-8: arrival at repaired node leaves state unchanged and emits no visual event", Ac8ArrivalAtRepairedIsNoOp);
Run("AC-9: first arrival emits known visual state and enables interaction", Ac9KnownVisualAndInteraction);
Run("AC-10: no intel hides material detail but keeps interaction available", Ac10NoIntelUiContract);
Run("AC-11: unknown node arrival warns and creates no ghost node", Ac11UnknownNodeWarns);
Run("AC-12: registry definition is read and missing registry node is skipped", Ac12RegistryDefinitionAndMissingNode);
Run("AC-13: MVP initializes exactly starlight_dock", Ac13MvpSingleNode);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 validation passed: {total}/{total} checks passed.");
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

static WorldRepair MakeRepairWithRegistry()
{
    var registry = new Registry();
    registry.InitializeContent();
    var repair = new WorldRepair(registry);
    repair.Initialize();
    return repair;
}

static bool Ac1NewGameInitialState()
{
    var repair = MakeRepairWithRegistry();
    var snapshot = repair.GetNodeSnapshot(WorldRepair.MvpNodeId);

    return snapshot is not null
        && snapshot.RepairState == RepairState.Unrevealed
        && snapshot.Deposited.Count == 0
        && Math.Abs(snapshot.RepairProgress) < 0.000001d;
}

static bool Ac2PhysicalArrivalReveals()
{
    var repair = MakeRepairWithRegistry();

    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);

    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known;
}

static bool Ac3IntelRevealReveals()
{
    var repair = MakeRepairWithRegistry();

    repair.OnIntelRevealedRepairNode(WorldRepair.MvpNodeId);

    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known;
}

static bool Ac4KnownToRepaired()
{
    var repair = MakeRepairWithRegistry();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);

    var result = repair.TryTransitionState(WorldRepair.MvpNodeId, RepairState.Repaired);

    return result.Allowed
        && result.NewState == RepairState.Repaired
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac5KnownCannotRegress()
{
    var repair = MakeRepairWithRegistry();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);

    var result = repair.TryTransitionState(WorldRepair.MvpNodeId, RepairState.Unrevealed);

    return !result.Allowed
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Known;
}

static bool Ac6RepairedCannotRegressKnown()
{
    var repair = MakeRepaired();

    var result = repair.TryTransitionState(WorldRepair.MvpNodeId, RepairState.Known);

    return !result.Allowed
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac7RepairedCannotRegressUnrevealed()
{
    var repair = MakeRepaired();

    var result = repair.TryTransitionState(WorldRepair.MvpNodeId, RepairState.Unrevealed);

    return !result.Allowed
        && repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired;
}

static bool Ac8ArrivalAtRepairedIsNoOp()
{
    var repair = MakeRepaired();
    var visualEvents = 0;
    repair.VisualStateChanged += (_, _) => visualEvents++;

    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);

    return repair.GetRepairState(WorldRepair.MvpNodeId) == RepairState.Repaired
        && visualEvents == 0;
}

static bool Ac9KnownVisualAndInteraction()
{
    var repair = MakeRepairWithRegistry();
    var visualState = "";
    repair.VisualStateChanged += (_, state) => visualState = state;

    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);

    var info = repair.GetRepairInteractionInfo(WorldRepair.MvpNodeId, intelIdentified: true);
    return visualState == WorldRepair.VisualStateKnown
        && info.VisualState == WorldRepair.VisualStateKnown
        && info.InteractionAvailable;
}

static bool Ac10NoIntelUiContract()
{
    var repair = MakeRepairWithRegistry();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);

    var info = repair.GetRepairInteractionInfo(WorldRepair.MvpNodeId, intelIdentified: false);

    return info.NodeExists
        && info.InteractionAvailable
        && !info.MaterialsRevealed
        && info.MaterialLabels.Values.All(label => label == "?")
        && info.UnlockPreview == "unknown_effect";
}

static bool Ac11UnknownNodeWarns()
{
    var repair = MakeRepairWithRegistry();

    repair.OnPlayerArrivedAtRepairNode("repair_node.missing");

    return repair.GetRepairState("repair_node.missing") == RepairState.Unknown
        && repair.GetRepairNodeIds().SequenceEqual([WorldRepair.MvpNodeId])
        && repair.Warnings.Count > 0;
}

static bool Ac12RegistryDefinitionAndMissingNode()
{
    var repair = MakeRepairWithRegistry();
    var definition = repair.GetRepairNodeDefinition(WorldRepair.MvpNodeId);

    var emptyRegistry = new Registry();
    emptyRegistry.InitializeContent();
    var missing = new WorldRepair();
    missing.Initialize();

    return definition is not null
        && definition.LinkedLocationId == "location.glass-harbor-outskirts"
        && definition.RequiredResources["resource.repair_kit"] == 4
        && definition.RequiredResources["resource.basic_supply"] == 4
        && missing.GetRepairNodeIds().Count == 0
        && missing.Errors.Count > 0;
}

static bool Ac13MvpSingleNode()
{
    var repair = MakeRepairWithRegistry();

    return repair.GetRepairNodeIds().SequenceEqual([WorldRepair.MvpNodeId]);
}

static WorldRepair MakeRepaired()
{
    var repair = MakeRepairWithRegistry();
    repair.OnPlayerArrivedAtRepairNode(WorldRepair.MvpNodeId);
    repair.TryTransitionState(WorldRepair.MvpNodeId, RepairState.Repaired);
    return repair;
}
