using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #12 Story 002: Response Resolution & Settlement Sequence ===");

var failed = 0;
var total = 0;

Run("AC-1: emergency handling consumes repair kit and suppresses threat", EmergencyHandlingSuppresses);
Run("AC-2/9: emergency without repair kit returns ERR_UNAVAILABLE and mutates nothing", EmergencyUnavailableHardFails);
Run("AC-3: tank applies hull damage, optional module damage, and knockback", TankSettlement);
Run("AC-4/5: tank option exposes low-hull and cross-band warnings without blocking", TankWarnings);
Run("AC-6/7: retreat has no damage, keeps threat active, and retreat_flagged is boolean", RetreatSettlement);
Run("AC-8: settlement sequence follows C4 order", SettlementOrder);
Run("AC-10/11: decision breath has no timer and remains awaiting while UI is inspectable", DecisionBreathStable);

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

static CombatManager MakeManager(
    int repairKits = 1,
    int hull = 100,
    Func<double>? random = null,
    Func<int, int, int>? randomRange = null,
    List<string>? order = null)
{
    var manager = new CombatManager();
    manager.SetRandomDelegates(random ?? (() => 0.9d), randomRange ?? ((min, _) => min));
    manager.SetResourceDelegates(
        () => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [CombatManager.RepairKitId] = repairKits,
        },
        (id, quantity) => order?.Add($"consume:{id}:{quantity}"));
    manager.SetModuleHullDelegates(
        () => hull,
        () =>
        [
            new ModuleSlotSnapshot(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed, 1.0d, ""),
            new ModuleSlotSnapshot(ModuleHullManager.SlotB, ModuleType.Scout, ModuleActualState.Damaged, ModuleVisibleState.Damaged, 0.6d, "old"),
        ],
        amount => order?.Add($"hull:{amount}"),
        (slot, type) => order?.Add($"module:{slot}:{type}"));
    manager.SetExplorationDelegates(
        _ => true,
        id => order?.Add($"suppress:{id}"),
        () => new CombatVector2(10, 0),
        (outcome, id) => order?.Add($"resume:{outcome}:{id}"));
    return manager;
}

static ThreatContext Threat() => ThreatContext.Guard("threat.guard-01", new CombatVector2(0, 0), GuardParams());

static Dictionary<string, object?> GuardParams() => new(StringComparer.Ordinal)
{
    ["full_damage_min"] = 8,
    ["full_damage_max"] = 12,
    ["module_damage_chance"] = 0.30d,
    ["emergency_cost_repair_kit"] = 1,
    ["knockback_distance_tanked"] = 8.0d,
    ["knockback_distance_retreat"] = 10.0d,
    ["can_be_suppressed"] = true,
    ["trigger_radius_min"] = 4.0d,
    ["trigger_radius_max"] = 6.0d,
};

static bool EmergencyHandlingSuppresses()
{
    var order = new List<string>();
    var manager = MakeManager(order: order);
    manager.ResolveThreat(Threat());
    var result = manager.SubmitResponse(CombatResponses.EmergencyHandling);

    return result is
        {
            Outcome: "suppressed",
            HullDamage: 0,
            ModuleDamage: null,
            Knockback: null,
            RetreatFlagged: false,
            ResourcesConsumed.Count: 1,
        }
        && result.ResourcesConsumed![0] == new CombatResourceConsumption(CombatManager.RepairKitId, 1)
        && order.SequenceEqual(["consume:resource.repair_kit:1", "suppress:threat.guard-01", "resume:suppressed:threat.guard-01"]);
}

static bool EmergencyUnavailableHardFails()
{
    var order = new List<string>();
    var manager = MakeManager(repairKits: 0, order: order);
    manager.ResolveThreat(Threat());
    var available = manager.GetAvailableResponses().Single(option => option.Id == CombatResponses.EmergencyHandling);
    var result = manager.SubmitResponse(CombatResponses.EmergencyHandling);

    return !available.Available
        && available.DisabledReason.Contains("repair_kit", StringComparison.Ordinal)
        && result.Error == "ERR_UNAVAILABLE"
        && order.Count == 0
        && manager.State == CombatState.AwaitingResponse;
}

static bool TankSettlement()
{
    var order = new List<string>();
    var manager = MakeManager(
        random: () => 0.1d,
        randomRange: (min, _) => min + 2,
        order: order);
    manager.ResolveThreat(Threat());
    var result = manager.SubmitResponse(CombatResponses.Tank);

    return result.Outcome == "tanked"
        && result.HullDamage == 10
        && result.ModuleDamage is { ModuleDamaged: true, SlotId: ModuleHullManager.SlotA, DamageType: "guard_impact" }
        && result.ResourcesConsumed is null
        && result.Knockback is { Distance: 8.0d }
        && result.RetreatFlagged == false
        && order.SequenceEqual(["hull:10", "module:slot_a:guard_impact", "resume:tanked:threat.guard-01"]);
}

static bool TankWarnings()
{
    var severe = MakeManager(hull: 12);
    severe.ResolveThreat(Threat());
    var severeTank = severe.GetAvailableResponses().Single(option => option.Id == CombatResponses.Tank);

    var cross = MakeManager(hull: 33);
    cross.ResolveThreat(Threat());
    var crossTank = cross.GetAvailableResponses().Single(option => option.Id == CombatResponses.Tank);

    return severeTank.Available
        && severeTank.Warning == "船体严重受损"
        && crossTank.Available
        && crossTank.CrossBandWarning;
}

static bool RetreatSettlement()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat());
    var first = manager.SubmitResponse(CombatResponses.Retreat);
    manager.CompleteResolvedFrame();
    manager.ResolveThreat(ThreatContext.Guard("threat.guard-02", new CombatVector2(0, 0), GuardParams()));
    var second = manager.SubmitResponse(CombatResponses.Retreat);

    return first.Outcome == "retreated"
        && first.HullDamage == 0
        && first.ModuleDamage is null
        && first.ResourcesConsumed is null
        && first.Knockback is { Distance: 10.0d }
        && first.RetreatFlagged
        && second.RetreatFlagged
        && manager.RetreatFlagged;
}

static bool SettlementOrder()
{
    var order = new List<string>();
    var manager = MakeManager(
        random: () => 0.1d,
        randomRange: (min, _) => min,
        order: order);
    manager.ResolveThreat(Threat());
    manager.SubmitResponse(CombatResponses.Tank);

    return order.SequenceEqual(["hull:8", "module:slot_a:guard_impact", "resume:tanked:threat.guard-01"]);
}

static bool DecisionBreathStable()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat());
    var initial = manager.State;
    for (var i = 0; i < 100; i++)
    {
        _ = manager.GetAvailableResponses();
    }

    return initial == CombatState.AwaitingResponse && manager.State == CombatState.AwaitingResponse;
}
