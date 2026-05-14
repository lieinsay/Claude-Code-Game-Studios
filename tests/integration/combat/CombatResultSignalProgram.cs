using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #12 Story 004: combat_result Contract & Signal Events ===");

var failed = 0;
var total = 0;

Run("AC-1 through AC-3: combat_result contract matches suppressed/tanked/retreated outcomes", CombatResultContract);
Run("AC-4 through AC-6: outcome-specific signal events are emitted", SignalEvents);
Run("AC-7/8: signals emit after state mutation with cascade depth <= 2", SignalTimingAndDepth);
Run("AC-9 through AC-13: downstream #8/#5/#11 cascades execute through injected boundaries", DownstreamCascades);
Run("AC-14/15: retreat_flagged persists across later settlements", RetreatFlagPersists);

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

static CombatManager MakeManager(List<string>? events = null, List<string>? cascades = null)
{
    var manager = new CombatManager();
    manager.SetRandomDelegates(() => 0.1d, (min, _) => min + 2);
    manager.SetResourceDelegates(
        () => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [CombatManager.RepairKitId] = 2,
        },
        (id, quantity) => cascades?.Add($"consume:{id}:{quantity}"));
    manager.SetModuleHullDelegates(
        () => 62,
        () =>
        [
            new ModuleSlotSnapshot(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed, 1.0d, ""),
        ],
        amount => cascades?.Add($"hull:{amount}"),
        (slot, damageType) => cascades?.Add($"module:{slot}:{damageType}"));
    manager.SetExplorationDelegates(
        _ => true,
        id => cascades?.Add($"suppress:{id}"),
        () => new CombatVector2(10, 0),
        (outcome, id) => cascades?.Add($"resume:{outcome}:{id}"));
    manager.ThreatSuppressed += id => events?.Add($"suppressed:{id}");
    manager.ThreatTanked += (id, damage) => events?.Add($"tanked:{id}:{damage}");
    manager.ThreatRetreated += id => events?.Add($"retreated:{id}");
    manager.ThreatResolved += (outcome, id) => events?.Add($"resolved:{outcome}:{id}");
    return manager;
}

static ThreatContext Threat(string id = "threat.guard-01") =>
    ThreatContext.Guard(id, CombatVector2.Zero, Params());

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

static CombatResult Resolve(string response, string id = "threat.guard-01", List<string>? events = null, List<string>? cascades = null)
{
    var manager = MakeManager(events, cascades);
    manager.ResolveThreat(Threat(id));
    return manager.SubmitResponse(response);
}

static bool CombatResultContract()
{
    var suppressed = Resolve(CombatResponses.EmergencyHandling);
    var tanked = Resolve(CombatResponses.Tank);
    var retreated = Resolve(CombatResponses.Retreat);

    return suppressed is
        {
            Outcome: "suppressed",
            HullDamage: 0,
            ModuleDamage: null,
            ResourcesConsumed.Count: 1,
            Knockback: null,
            RetreatFlagged: false,
        }
        && tanked is
        {
            Outcome: "tanked",
            HullDamage: 10,
            ModuleDamage.SlotId: ModuleHullManager.SlotA,
            ResourcesConsumed: null,
            Knockback.Distance: 8.0d,
            RetreatFlagged: false,
        }
        && retreated is
        {
            Outcome: "retreated",
            HullDamage: 0,
            ModuleDamage: null,
            ResourcesConsumed: null,
            Knockback.Distance: 10.0d,
            RetreatFlagged: true,
        };
}

static bool SignalEvents()
{
    var suppressedEvents = new List<string>();
    Resolve(CombatResponses.EmergencyHandling, events: suppressedEvents);
    var tankedEvents = new List<string>();
    Resolve(CombatResponses.Tank, events: tankedEvents);
    var retreatedEvents = new List<string>();
    Resolve(CombatResponses.Retreat, events: retreatedEvents);

    return suppressedEvents.SequenceEqual(["suppressed:threat.guard-01", "resolved:suppressed:threat.guard-01"])
        && tankedEvents.SequenceEqual(["tanked:threat.guard-01:10", "resolved:tanked:threat.guard-01"])
        && retreatedEvents.SequenceEqual(["retreated:threat.guard-01", "resolved:retreated:threat.guard-01"]);
}

static bool SignalTimingAndDepth()
{
    var manager = MakeManager();
    var stateDuringResolved = CombatState.Idle;
    var maxDepth = 0;
    var depth = 0;
    manager.ThreatResolved += (_, _) =>
    {
        depth++;
        maxDepth = Math.Max(maxDepth, depth);
        stateDuringResolved = manager.State;
        depth--;
    };
    manager.ResolveThreat(Threat());
    manager.SubmitResponse(CombatResponses.Tank);

    return stateDuringResolved == CombatState.Resolved && maxDepth <= 1;
}

static bool DownstreamCascades()
{
    var suppressed = new List<string>();
    Resolve(CombatResponses.EmergencyHandling, cascades: suppressed);

    var tanked = new List<string>();
    Resolve(CombatResponses.Tank, cascades: tanked);

    var retreated = new List<string>();
    Resolve(CombatResponses.Retreat, cascades: retreated);

    return suppressed.SequenceEqual(["consume:resource.repair_kit:1", "suppress:threat.guard-01", "resume:suppressed:threat.guard-01"])
        && tanked.SequenceEqual(["hull:10", "module:slot_a:guard_impact", "resume:tanked:threat.guard-01"])
        && retreated.SequenceEqual(["resume:retreated:threat.guard-01"]);
}

static bool RetreatFlagPersists()
{
    var manager = MakeManager();
    manager.ResolveThreat(Threat("threat.guard-01"));
    var retreat = manager.SubmitResponse(CombatResponses.Retreat);
    manager.CompleteResolvedFrame();
    manager.ResolveThreat(Threat("threat.guard-02"));
    var emergency = manager.SubmitResponse(CombatResponses.EmergencyHandling);

    return retreat.RetreatFlagged && emergency.RetreatFlagged;
}
