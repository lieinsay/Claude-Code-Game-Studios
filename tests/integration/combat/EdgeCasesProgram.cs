using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #12 Story 006: Edge Cases & Defensive Handling ===");

var failed = 0;
var total = 0;

Run("AC-1/2: low hull tank reaches hull=0, exploration continues, departure blocks", LowHullTank);
Run("AC-3/4: tank warning predicts damaged->critical crossing only when relevant", CrossBandPreview);
Run("AC-5 through AC-7: module eligibility excludes empty/damaged actual states but includes unchecked installed slots", ModuleEligibility);
Run("AC-8/11/12: retreat flag persists across emergency, tank, and multiple retreats", RetreatPersistence);
Run("AC-9: degenerate knockback uses facing or random unit fallback", DegenerateKnockback);
Run("AC-10: tank applies hull damage before simultaneous module damage", HullBeforeModule);
Run("AC-13: zero-quantity repair_kit stack disables emergency handling", ZeroQuantityRepairKit);
Run("AC-14: Exploration keeps guard threats inert when #12 is unavailable", CombatUnavailableInert);
Run("AC-15 through AC-17: invalid context, missing params, and invalid response are defensive", DefensiveInputs);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 006 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 006 validation passed: {total}/{total} checks passed.");
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

static Dictionary<string, object?> Params() => new(StringComparer.Ordinal)
{
    ["full_damage_min"] = 12,
    ["full_damage_max"] = 12,
    ["module_damage_chance"] = 1.0d,
    ["emergency_cost_repair_kit"] = 1,
    ["knockback_distance_tanked"] = 8.0d,
    ["knockback_distance_retreat"] = 10.0d,
    ["can_be_suppressed"] = true,
    ["trigger_radius_max"] = 6.0d,
};

static CombatManager MakeManager(
    ModuleHullManager? moduleHull = null,
    int repairKits = 1,
    List<string>? order = null,
    CombatVector2? playerPosition = null)
{
    moduleHull ??= new ModuleHullManager();
    var manager = new CombatManager();
    manager.SetRandomDelegates(() => 0.0d, (min, _) => min);
    manager.SetResourceDelegates(
        () => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [CombatManager.RepairKitId] = repairKits,
        },
        (id, quantity) => order?.Add($"consume:{id}:{quantity}"));
    manager.SetModuleHullDelegates(
        () => moduleHull.GetHullState().Integrity,
        () => moduleHull.GetSlotIds().Select(moduleHull.GetSlotState).ToArray(),
        amount =>
        {
            order?.Add("hull");
            moduleHull.ApplyHullDamage(amount);
        },
        (slot, damageType) =>
        {
            order?.Add("module");
            moduleHull.ApplyModuleDamage(slot, damageType);
        });
    manager.SetExplorationDelegates(
        _ => true,
        _ => { },
        () => playerPosition ?? new CombatVector2(10, 0),
        (_, _) => { });
    return manager;
}

static ThreatContext Threat(string id = "threat.guard-01", CombatVector2? facing = null) =>
    ThreatContext.Guard(id, CombatVector2.Zero, Params(), facing);

static bool LowHullTank()
{
    var moduleHull = new ModuleHullManager();
    moduleHull.SetHullForTest(10);
    var manager = MakeManager(moduleHull);
    manager.ResolveThreat(Threat());
    manager.SubmitResponse(CombatResponses.Tank);

    var hull = moduleHull.GetHullState();
    var departure = moduleHull.CanDepart();
    return hull is { Integrity: 0, Band: HullBand.Destroyed }
        && !departure.CanDepart
        && departure.Reasons.Contains("hull_destroyed");
}

static bool CrossBandPreview()
{
    var damagedHull = new ModuleHullManager();
    damagedHull.SetHullForTest(33);
    var damagedManager = MakeManager(damagedHull);
    damagedManager.ResolveThreat(Threat());
    var damagedTank = damagedManager.GetAvailableResponses().Single(option => option.Id == CombatResponses.Tank);

    var intactHull = new ModuleHullManager();
    intactHull.SetHullForTest(76);
    var intactManager = MakeManager(intactHull);
    intactManager.ResolveThreat(Threat());
    var intactTank = intactManager.GetAvailableResponses().Single(option => option.Id == CombatResponses.Tank);

    return damagedTank.CrossBandWarning && !intactTank.CrossBandWarning;
}

static bool ModuleEligibility()
{
    var emptyHull = new ModuleHullManager();
    emptyHull.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty);
    emptyHull.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty);
    var emptyDamage = MakeManager(emptyHull).CalcModuleDamage(CombatResponses.Tank, Params());

    var mixedHull = new ModuleHullManager();
    mixedHull.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    mixedHull.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var mixedDamage = MakeManager(mixedHull).CalcModuleDamage(CombatResponses.Tank, Params());

    return emptyDamage is { ModuleDamaged: false, SlotId: null }
        && mixedDamage is { ModuleDamaged: true, SlotId: ModuleHullManager.SlotB };
}

static bool RetreatPersistence()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat("threat.guard-01"));
    var retreatOne = manager.SubmitResponse(CombatResponses.Retreat);
    manager.CompleteResolvedFrame();
    manager.ResolveThreat(Threat("threat.guard-02"));
    var retreatTwo = manager.SubmitResponse(CombatResponses.Retreat);
    manager.CompleteResolvedFrame();
    manager.ResolveThreat(Threat("threat.guard-03"));
    var tank = manager.SubmitResponse(CombatResponses.Tank);
    manager.CompleteResolvedFrame();
    manager.ResolveThreat(Threat("threat.guard-04"));
    var emergency = manager.SubmitResponse(CombatResponses.EmergencyHandling);

    return retreatOne.RetreatFlagged
        && retreatTwo.RetreatFlagged
        && tank.RetreatFlagged
        && emergency.RetreatFlagged;
}

static bool DegenerateKnockback()
{
    var facing = MakeManager(playerPosition: CombatVector2.Zero)
        .CalcKnockback(CombatResponses.Tank, Params(), Threat(facing: new CombatVector2(0, 3)));
    var randomManager = MakeManager(playerPosition: CombatVector2.Zero);
    randomManager.SetRandomDelegates(() => 0.0d, (min, _) => min);
    var random = randomManager.CalcKnockback(CombatResponses.Tank, Params(), Threat());

    return facing is { Direction.Y: > 0.99d }
        && random is { Direction.X: > 0.99d };
}

static bool HullBeforeModule()
{
    var order = new List<string>();
    var moduleHull = new ModuleHullManager();
    moduleHull.SetHullForTest(8);
    moduleHull.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var manager = MakeManager(moduleHull, order: order);
    manager.ResolveThreat(Threat());
    manager.SubmitResponse(CombatResponses.Tank);

    return order.SequenceEqual(["hull", "module"])
        && moduleHull.GetHullState().Integrity == 0
        && moduleHull.GetSlotState(ModuleHullManager.SlotA).ActualState == ModuleActualState.Damaged;
}

static bool ZeroQuantityRepairKit()
{
    var manager = MakeManager(repairKits: 0);
    manager.ResolveThreat(Threat());
    var emergency = manager.GetAvailableResponses().Single(option => option.Id == CombatResponses.EmergencyHandling);
    return !emergency.Available;
}

static bool CombatUnavailableInert()
{
    var mgr = new ExplorationManager();
    mgr.EnterExploration("location.ruins");
    mgr.SkipArriving();
    mgr.SetIsCombatManagerAvailableDelegate(() => false);
    var threat = new ExplorationManager.ThreatPoint(
        "threat.guard-01",
        ExplorationManager.ThreatCategory.Guard,
        5.0d,
        0.0d);
    mgr.RegisterThreatPoint(threat);
    var result = mgr.CheckThreatTrigger(0.0d, "interaction");

    return result.Count == 1
        && result[0].Triggered
        && threat.IsActive
        && mgr.CurrentSubstate == ExplorationSubstate.Threatened;
}

static bool DefensiveInputs()
{
    var manager = MakeManager();
    var invalidContext = manager.ResolveThreat(null);
    var defaults = manager.ValidateThreatParams(null);
    manager.ResolveThreat(Threat());
    var invalidResponse = manager.SubmitResponse("invalid_response");

    return invalidContext.Error == "ERR_INVALID_CONTEXT"
        && defaults.FullDamageMin == 8
        && defaults.FullDamageMax == 12
        && invalidResponse.Error == "ERR_INVALID_RESPONSE"
        && invalidResponse.HullDamage == 0;
}
