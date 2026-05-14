using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #12 Story 005: Data-Driven Threat Configuration ===");

var failed = 0;
var total = 0;

Run("AC-1/12/13: Registry returns guard config and unknown threat_type returns null for fallback", RegistryThreatConfig);
Run("AC-2/10/11: CombatManager reads all values from encounter_params and ignores unknown keys", DataDrivenReadiness);
Run("AC-3/5: validation clamps knockback radius and swaps inverted damage range", ConfigurationValidation);
Run("AC-4: can_be_suppressed=false disables emergency handling even with repair kit", CanBeSuppressedGate);
Run("AC-6 through AC-9: missing encounter_params fields use safe defaults", SafeDefaults);

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

static CombatManager MakeManager(int repairKit = 1)
{
    var manager = new CombatManager();
    manager.SetRandomDelegates(() => 0.9d, (min, _) => min);
    manager.SetResourceDelegates(
        () => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [CombatManager.RepairKitId] = repairKit,
        },
        (_, _) => { });
    manager.SetModuleHullDelegates(
        () => 100,
        () => Array.Empty<ModuleSlotSnapshot>(),
        _ => { },
        (_, _) => { });
    manager.SetExplorationDelegates(
        _ => true,
        _ => { },
        () => new CombatVector2(10, 0),
        (_, _) => { });
    return manager;
}

static bool RegistryThreatConfig()
{
    var registry = new Registry();
    registry.InitializeContent();
    var guard = registry.GetThreatConfig("guard");
    var unknown = registry.GetThreatConfig("unknown_type");

    return guard is not null
        && RequiredKeys().All(guard.ContainsKey)
        && (string?)guard["threat_category"] == "guard"
        && unknown is null;
}

static bool DataDrivenReadiness()
{
    var manager = MakeManager();
    var config = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["full_damage_min"] = 2,
        ["full_damage_max"] = 2,
        ["module_damage_chance"] = 0.0d,
        ["emergency_cost_repair_kit"] = 1,
        ["knockback_distance_tanked"] = 14.0d,
        ["knockback_distance_retreat"] = 16.0d,
        ["can_be_suppressed"] = true,
        ["trigger_radius_max"] = 6.0d,
        ["future_patrol_noise"] = "ignored",
    };
    var damage = manager.CalcHullDamage(CombatResponses.Tank, config);
    var knockback = manager.CalcKnockback(CombatResponses.Retreat, config, ThreatContext.Guard("threat.patrol-01", CombatVector2.Zero, config));

    return damage == 2 && knockback is { Distance: 16.0d };
}

static bool ConfigurationValidation()
{
    var manager = MakeManager();
    var validated = manager.ValidateThreatParams(new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["full_damage_min"] = 12,
        ["full_damage_max"] = 8,
        ["knockback_distance_tanked"] = 5.0d,
        ["trigger_radius_max"] = 6.0d,
    });

    return validated.FullDamageMin == 8
        && validated.FullDamageMax == 12
        && Math.Abs(validated.KnockbackDistanceTanked - 8.0d) < 0.001d
        && manager.Warnings.Any(w => w.Contains("swapping", StringComparison.Ordinal))
        && manager.Errors.Any(e => e.Contains("clamping", StringComparison.Ordinal));
}

static bool CanBeSuppressedGate()
{
    var manager = MakeManager(repairKit: 2);
    var config = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["can_be_suppressed"] = false,
        ["trigger_radius_max"] = 6.0d,
    };
    manager.ResolveThreat(ThreatContext.Guard("threat.guard-01", CombatVector2.Zero, config));
    var emergency = manager.GetAvailableResponses().Single(option => option.Id == CombatResponses.EmergencyHandling);

    return !emergency.Available && emergency.DisabledReason.Contains("不可应急处理", StringComparison.Ordinal);
}

static bool SafeDefaults()
{
    var manager = MakeManager();
    var defaults = manager.ValidateThreatParams(new Dictionary<string, object?>(StringComparer.Ordinal));

    return defaults.FullDamageMin == 8
        && defaults.FullDamageMax == 12
        && Math.Abs(defaults.ModuleDamageChance - 0.30d) < 0.001d
        && defaults.EmergencyCostRepairKit == 1
        && Math.Abs(defaults.KnockbackDistanceTanked - 8.0d) < 0.001d
        && Math.Abs(defaults.KnockbackDistanceRetreat - 10.0d) < 0.001d;
}

static string[] RequiredKeys() =>
[
    "threat_category",
    "full_damage_min",
    "full_damage_max",
    "module_damage_chance",
    "emergency_cost_repair_kit",
    "knockback_distance_tanked",
    "knockback_distance_retreat",
    "can_be_suppressed",
    "trigger_radius_min",
    "trigger_radius_max",
];
