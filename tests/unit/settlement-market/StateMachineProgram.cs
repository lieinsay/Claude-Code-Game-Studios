using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Feature;

Console.WriteLine("=== Epic #14 Story 001: Settlement State Machine & Stall Lifecycle ===");

var failed = 0;
var total = 0;

Run("AC-1/5/6/9/10/18: new game initializes MVP settlement, stalls, and NPCs from Registry", NewGameDefaults);
Run("AC-2/3/4: activity progresses dormant to recovering to active only at thresholds", ActivityThresholds);
Run("AC-7/8/11: closed stall opens to open_basic and unlocks coupled NPC; expanded remains unreachable", StallAndNpcOpen);
Run("AC-12 through AC-16: invalid and reverse transitions are rejected", InvalidTransitionsRejected);
Run("AC-17: missing Registry entities produce diagnostics without ghost state", MissingRegistryDiagnostics);

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

static SettlementManager MakeSettlement()
{
    var registry = new Registry();
    registry.InitializeContent();
    var settlement = new SettlementManager(registry);
    settlement.InitNewGameState();
    return settlement;
}

static bool NewGameDefaults()
{
    var settlement = MakeSettlement();

    return settlement.GetSettlementState(SettlementManager.MvpSettlementId) == SettlementState.Dormant
        && settlement.GetCompletedNodeIds(SettlementManager.MvpSettlementId).Count == 0
        && settlement.GetSettlementStalls(SettlementManager.MvpSettlementId).Count == 4
        && settlement.GetStallState(SettlementManager.DefaultStallId) == StallState.OpenBasic
        && settlement.GetStallState("stall.gh-lens-workshop") == StallState.Closed
        && settlement.GetStallState("stall.gh-sail-shop") == StallState.Closed
        && settlement.GetStallState("stall.gh-chart-studio") == StallState.Closed
        && settlement.GetNpcState("npc.atu") == NpcState.Idle
        && settlement.GetNpcState("npc.wei") == NpcState.Absent
        && settlement.GetNpcState("npc.yun") == NpcState.Absent
        && settlement.GetNpcState("npc.cen") == NpcState.Absent
        && settlement.Errors.Count == 0;
}

static bool ActivityThresholds()
{
    var settlement = MakeSettlement();
    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var dormant = settlement.GetSettlementState(SettlementManager.MvpSettlementId);

    settlement.TransitionStallState("stall.gh-sail-shop", StallState.OpenBasic);
    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var recoveringTwo = settlement.GetSettlementState(SettlementManager.MvpSettlementId);

    settlement.TransitionStallState("stall.gh-lens-workshop", StallState.OpenBasic);
    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var recoveringThree = settlement.GetSettlementState(SettlementManager.MvpSettlementId);

    settlement.TransitionStallState("stall.gh-chart-studio", StallState.OpenBasic);
    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var active = settlement.GetSettlementState(SettlementManager.MvpSettlementId);

    return dormant == SettlementState.Dormant
        && recoveringTwo == SettlementState.Recovering
        && recoveringThree == SettlementState.Recovering
        && active == SettlementState.Active;
}

static bool StallAndNpcOpen()
{
    var settlement = MakeSettlement();
    var stallChanged = false;
    var npcChanged = false;
    settlement.StallStateChanged += (stallId, oldState, newState) =>
        stallChanged = stallId == "stall.gh-sail-shop" && oldState == 0 && newState == 1;
    settlement.NpcStateChanged += (npcId, oldState, newState) =>
        npcChanged = npcId == "npc.yun" && oldState == 0 && newState == 1;

    var opened = settlement.TransitionStallState("stall.gh-sail-shop", StallState.OpenBasic);
    var expanded = settlement.TransitionStallState("stall.gh-sail-shop", StallState.OpenExpanded);

    return opened
        && !expanded
        && stallChanged
        && npcChanged
        && settlement.GetStallState("stall.gh-sail-shop") == StallState.OpenBasic
        && settlement.GetNpcState("npc.yun") == NpcState.Idle;
}

static bool InvalidTransitionsRejected()
{
    var settlement = MakeSettlement();

    var closedToExpanded = settlement.TransitionStallState("stall.gh-sail-shop", StallState.OpenExpanded);
    settlement.TransitionStallState("stall.gh-sail-shop", StallState.OpenBasic);
    var openToClosed = settlement.TransitionStallState("stall.gh-sail-shop", StallState.Closed);
    var absentToActive = settlement.TransitionNpcState("npc.wei", NpcState.Active);
    settlement.TransitionStallState("stall.gh-lens-workshop", StallState.OpenBasic);
    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var recoveringToDormant = settlement.TransitionSettlementState(SettlementManager.MvpSettlementId, SettlementState.Dormant);
    settlement.TransitionStallState("stall.gh-chart-studio", StallState.OpenBasic);
    settlement.RecalculateSettlementActivity(SettlementManager.MvpSettlementId);
    var activeToRecovering = settlement.TransitionSettlementState(SettlementManager.MvpSettlementId, SettlementState.Recovering);

    return !closedToExpanded
        && !openToClosed
        && !absentToActive
        && !recoveringToDormant
        && !activeToRecovering
        && settlement.GetSettlementState(SettlementManager.MvpSettlementId) == SettlementState.Active;
}

static bool MissingRegistryDiagnostics()
{
    var emptyRegistry = new Registry();
    emptyRegistry.InitializeContent();
    var settlement = new SettlementManager(new Registry());
    settlement.InitNewGameState();

    return settlement.GetSettlementStalls(SettlementManager.MvpSettlementId).Count == 0
        && settlement.Errors.Count >= 2;
}
