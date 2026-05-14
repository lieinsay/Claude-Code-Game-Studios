using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #12 Story 003: Damage, Module & Knockback Formulas ===");

var failed = 0;
var total = 0;

Run("AC-1 through AC-4: tank hull damage is uniform int [8,12], other responses are 0", HullDamageFormula);
Run("AC-5 through AC-9: module damage chance and actual_state eligibility", ModuleDamageFormula);
Run("AC-10 through AC-12: emergency availability checks carried repair kits safely", EmergencyAvailability);
Run("AC-13 through AC-16: knockback distances and degenerate direction fallback", KnockbackFormula);

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

static CombatManager MakeManager(
    IReadOnlyList<ModuleSlotSnapshot>? slots = null,
    IReadOnlyDictionary<string, int>? repairMaterials = null,
    CombatVector2? playerPosition = null,
    Queue<int>? rolls = null)
{
    var manager = new CombatManager();
    manager.SetRandomDelegates(
        () => 0.0d,
        (min, max) => rolls is { Count: > 0 } ? rolls.Dequeue() : min);
    manager.SetResourceDelegates(
        () => repairMaterials ?? new Dictionary<string, int>(StringComparer.Ordinal),
        (_, _) => { });
    manager.SetModuleHullDelegates(
        () => 100,
        () => slots ?? Array.Empty<ModuleSlotSnapshot>(),
        _ => { },
        (_, _) => { });
    manager.SetExplorationDelegates(
        _ => true,
        _ => { },
        () => playerPosition ?? new CombatVector2(10, 0),
        (_, _) => { });
    return manager;
}

static Dictionary<string, object?> Params() => new(StringComparer.Ordinal)
{
    ["full_damage_min"] = 8,
    ["full_damage_max"] = 12,
    ["module_damage_chance"] = 0.30d,
    ["emergency_cost_repair_kit"] = 1,
    ["knockback_distance_tanked"] = 8.0d,
    ["knockback_distance_retreat"] = 10.0d,
    ["trigger_radius_max"] = 6.0d,
};

static bool HullDamageFormula()
{
    var rolls = new Queue<int>(Enumerable.Range(0, 1000).Select(i => 8 + (i % 5)));
    var manager = MakeManager(rolls: rolls);
    var counts = new Dictionary<int, int>();
    for (var i = 0; i < 1000; i++)
    {
        var damage = manager.CalcHullDamage(CombatResponses.Tank, Params());
        counts[damage] = counts.GetValueOrDefault(damage) + 1;
    }

    return Enumerable.Range(8, 5).All(value => counts.GetValueOrDefault(value) == 200)
        && manager.CalcHullDamage(CombatResponses.EmergencyHandling, Params()) == 0
        && manager.CalcHullDamage(CombatResponses.Retreat, Params()) == 0;
}

static bool ModuleDamageFormula()
{
    var slots = new[]
    {
        new ModuleSlotSnapshot(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged, 0.5d, "old"),
        new ModuleSlotSnapshot(ModuleHullManager.SlotB, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Unchecked, 0.95d, ""),
    };
    var hit = MakeManager(slots, rolls: new Queue<int>([0])).CalcModuleDamage(CombatResponses.Tank, Params());
    var missManager = MakeManager(slots);
    missManager.SetRandomDelegates(() => 0.9d, (min, _) => min);
    var miss = missManager.CalcModuleDamage(CombatResponses.Tank, Params());
    var empty = MakeManager(Array.Empty<ModuleSlotSnapshot>()).CalcModuleDamage(CombatResponses.Tank, Params());
    var emergency = MakeManager(slots).CalcModuleDamage(CombatResponses.EmergencyHandling, Params());
    var retreat = MakeManager(slots).CalcModuleDamage(CombatResponses.Retreat, Params());

    return hit is { ModuleDamaged: true, SlotId: ModuleHullManager.SlotB }
        && miss is { ModuleDamaged: false, SlotId: null }
        && empty is { ModuleDamaged: false, SlotId: null }
        && emergency is { ModuleDamaged: false, SlotId: null }
        && retreat is { ModuleDamaged: false, SlotId: null };
}

static bool EmergencyAvailability()
{
    var available = MakeManager(repairMaterials: new Dictionary<string, int>
    {
        [CombatManager.RepairKitId] = 1,
    }).CheckEmergencyAvailable();
    var zero = MakeManager(repairMaterials: new Dictionary<string, int>
    {
        [CombatManager.RepairKitId] = 0,
    }).CheckEmergencyAvailable();
    var missing = MakeManager(repairMaterials: new Dictionary<string, int>()).CheckEmergencyAvailable();

    return available && !zero && !missing;
}

static bool KnockbackFormula()
{
    var paramsMap = Params();
    var manager = MakeManager(playerPosition: new CombatVector2(10, 0));
    var emergency = manager.CalcKnockback(CombatResponses.EmergencyHandling, paramsMap, ThreatContext.Guard("t", CombatVector2.Zero, paramsMap));
    var tank = manager.CalcKnockback(CombatResponses.Tank, paramsMap, ThreatContext.Guard("t", CombatVector2.Zero, paramsMap));
    var retreat = manager.CalcKnockback(CombatResponses.Retreat, paramsMap, ThreatContext.Guard("t", CombatVector2.Zero, paramsMap));

    var facingFallback = MakeManager(playerPosition: CombatVector2.Zero).CalcKnockback(
        CombatResponses.Tank,
        paramsMap,
        ThreatContext.Guard("t", CombatVector2.Zero, paramsMap, new CombatVector2(0, 2)));

    var randomFallbackManager = MakeManager(playerPosition: CombatVector2.Zero);
    randomFallbackManager.SetRandomDelegates(() => 0.0d, (min, _) => min);
    var randomFallback = randomFallbackManager.CalcKnockback(
        CombatResponses.Tank,
        paramsMap,
        ThreatContext.Guard("t", CombatVector2.Zero, paramsMap));

    return emergency is null
        && tank is { Distance: 8.0d, Direction.X: > 0.99d }
        && retreat is { Distance: 10.0d, Direction.X: > 0.99d }
        && facingFallback is { Direction.Y: > 0.99d }
        && randomFallback is { Direction.X: > 0.99d };
}
