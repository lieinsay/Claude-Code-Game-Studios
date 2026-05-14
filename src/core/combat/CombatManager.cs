using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>Threat resolution micro state owned by CombatManager.</summary>
public enum CombatState
{
    Idle = 0,
    AwaitingResponse = 1,
    Processing = 2,
    Resolved = 3,
}

/// <summary>Stable response identifiers accepted by CombatManager.</summary>
public static class CombatResponses
{
    /// <summary>Emergency handling consumes one repair kit and suppresses the threat.</summary>
    public const string EmergencyHandling = "emergency_handling";

    /// <summary>Tank accepts hull/module risk and keeps the threat active.</summary>
    public const string Tank = "tank";

    /// <summary>Retreat keeps the threat active and marks the exploration session as forced.</summary>
    public const string Retreat = "retreat";
}

/// <summary>Simple 2D vector value kept independent of Godot runtime for C# tests.</summary>
public readonly record struct CombatVector2(double X, double Y)
{
    /// <summary>Zero vector.</summary>
    public static readonly CombatVector2 Zero = new(0.0d, 0.0d);

    /// <summary>Returns vector length squared.</summary>
    public double LengthSquared => X * X + Y * Y;

    /// <summary>Returns the normalized vector, or zero for a degenerate vector.</summary>
    public CombatVector2 Normalized()
    {
        var length = Math.Sqrt(LengthSquared);
        return length <= 0.000001d ? Zero : new CombatVector2(X / length, Y / length);
    }

    /// <summary>Subtracts another vector.</summary>
    public static CombatVector2 operator -(CombatVector2 left, CombatVector2 right) =>
        new(left.X - right.X, left.Y - right.Y);
}

/// <summary>Incoming threat payload derived by Exploration from EncounterContext.</summary>
public sealed record ThreatContext(
    string ThreatId,
    string ThreatType,
    CombatVector2 Position,
    IReadOnlyDictionary<string, object?> EncounterParams,
    CombatVector2 Facing)
{
    /// <summary>Creates a minimal guard context for tests and current MVP callers.</summary>
    public static ThreatContext Guard(
        string threatId,
        CombatVector2 position,
        IReadOnlyDictionary<string, object?>? encounterParams = null,
        CombatVector2? facing = null)
    {
        return new ThreatContext(
            threatId,
            "guard",
            position,
            encounterParams ?? new Dictionary<string, object?>(StringComparer.Ordinal),
            facing ?? CombatVector2.Zero);
    }
}

/// <summary>Validated, data-driven threat tuning values.</summary>
public sealed record ThreatParams(
    string ThreatCategory,
    int FullDamageMin,
    int FullDamageMax,
    double ModuleDamageChance,
    int EmergencyCostRepairKit,
    double KnockbackDistanceTanked,
    double KnockbackDistanceRetreat,
    bool CanBeSuppressed,
    double TriggerRadiusMin,
    double TriggerRadiusMax)
{
    /// <summary>Safe MVP defaults used when EncounterContext omits fields.</summary>
    public static ThreatParams Defaults => new(
        "guard",
        FullDamageMin: 8,
        FullDamageMax: 12,
        ModuleDamageChance: 0.30d,
        EmergencyCostRepairKit: 1,
        KnockbackDistanceTanked: 8.0d,
        KnockbackDistanceRetreat: 10.0d,
        CanBeSuppressed: true,
        TriggerRadiusMin: 4.0d,
        TriggerRadiusMax: 6.0d);
}

/// <summary>Combat result module damage payload.</summary>
public sealed record CombatModuleDamage(bool ModuleDamaged, string? SlotId, string DamageType);

/// <summary>Combat result resource consumption payload.</summary>
public sealed record CombatResourceConsumption(string ResourceId, int Quantity);

/// <summary>Combat result knockback payload.</summary>
public sealed record CombatKnockback(CombatVector2 Direction, double Distance);

/// <summary>Result returned after a threat response has settled.</summary>
public sealed record CombatResult(
    string Outcome,
    int HullDamage,
    CombatModuleDamage? ModuleDamage,
    IReadOnlyList<CombatResourceConsumption>? ResourcesConsumed,
    CombatKnockback? Knockback,
    bool RetreatFlagged,
    string? Error = null,
    string? Reason = null)
{
    /// <summary>True when no error code is present.</summary>
    public bool Success => string.IsNullOrEmpty(Error);

    /// <summary>Creates an error result that still preserves the six-field contract.</summary>
    public static CombatResult Fail(string error, string reason = "") =>
        new(
            Outcome: "error",
            HullDamage: 0,
            ModuleDamage: null,
            ResourcesConsumed: null,
            Knockback: null,
            RetreatFlagged: false,
            Error: error,
            Reason: reason);
}

/// <summary>Immediate result of resolve_threat.</summary>
public sealed record ResolveThreatResult(
    string Status,
    string? Error = null,
    bool Queued = false,
    string? ThreatId = null,
    CombatResult? CombatResult = null);

/// <summary>UI-facing response availability contract.</summary>
public sealed record CombatResponseOption(
    string Id,
    string Label,
    bool Available,
    string DisabledReason = "",
    string Warning = "",
    string DamagePreview = "",
    bool CrossBandWarning = false);

/// <summary>
/// Combat / Threat Resolution Autoload #12 core logic.
/// </summary>
public sealed class CombatManager
{
    /// <summary>Maximum queued threats while another threat is awaiting or processing.</summary>
    public const int MaxQueueDepth = 4;

    /// <summary>Repair kit resource ID consumed by emergency handling.</summary>
    public const string RepairKitId = "resource.repair_kit";

    private readonly Queue<ThreatContext> _threatQueue = new();
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    private ThreatContext? _currentThreat;
    private CombatResult? _lastResult;
    private bool _retreatFlagged;
    private Func<double> _random = () => Random.Shared.NextDouble();
    private Func<int, int, int> _randomRange = (min, max) => min == max ? min : Random.Shared.Next(min, max + 1);
    private Func<IReadOnlyDictionary<string, int>> _getRepairMaterials = () => new Dictionary<string, int>(StringComparer.Ordinal);
    private Func<int> _getHullIntegrity = () => 100;
    private Func<IReadOnlyList<ModuleSlotSnapshot>> _getModuleSlots = () => Array.Empty<ModuleSlotSnapshot>();
    private Func<CombatVector2> _getPlayerPosition = () => CombatVector2.Zero;
    private Func<string, bool> _isThreatActive = _ => true;
    private Action<string, int> _consumeInCombat = (_, _) => { };
    private Action<int> _applyHullDamage = _ => { };
    private Action<string, string> _applyModuleDamage = (_, _) => { };
    private Action<string> _suppressThreat = _ => { };
    private Action<string, string> _resumeExploration = (_, _) => { };

    /// <summary>Fired when a threat enters decision breath.</summary>
    public event Action<ThreatContext>? ThreatTriggered;

    /// <summary>Fired when a full combat_result is ready for Exploration.</summary>
    public event Action<CombatResult, string>? CombatResultReady;

    /// <summary>Fired for every completed outcome after state mutation.</summary>
    public event Action<string, string>? ThreatResolved;

    /// <summary>Fired for emergency-handling suppression.</summary>
    public event Action<string>? ThreatSuppressed;

    /// <summary>Fired for tank resolution.</summary>
    public event Action<string, int>? ThreatTanked;

    /// <summary>Fired for retreat resolution.</summary>
    public event Action<string>? ThreatRetreated;

    /// <summary>Current combat micro-state.</summary>
    public CombatState State { get; private set; } = CombatState.Idle;

    /// <summary>Non-fatal warning log, including queue overflow and config repair.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>Defensive error log for invalid inputs or config clamps.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>Most recent completed combat result.</summary>
    public CombatResult? LastResult => _lastResult;

    /// <summary>Current active threat, if any.</summary>
    public ThreatContext? CurrentThreat => _currentThreat;

    /// <summary>Current queued threat count.</summary>
    public int QueueDepth => _threatQueue.Count;

    /// <summary>Whether any retreat has occurred in the active exploration session.</summary>
    public bool RetreatFlagged => _retreatFlagged;

    /// <summary>Injects deterministic random sources for tests.</summary>
    public void SetRandomDelegates(Func<double> random, Func<int, int, int> randomRange)
    {
        _random = random;
        _randomRange = randomRange;
    }

    /// <summary>Injects #5 resource boundaries.</summary>
    public void SetResourceDelegates(
        Func<IReadOnlyDictionary<string, int>> getRepairMaterials,
        Action<string, int> consumeInCombat)
    {
        _getRepairMaterials = getRepairMaterials;
        _consumeInCombat = consumeInCombat;
    }

    /// <summary>Injects #8 module and hull boundaries.</summary>
    public void SetModuleHullDelegates(
        Func<int> getHullIntegrity,
        Func<IReadOnlyList<ModuleSlotSnapshot>> getModuleSlots,
        Action<int> applyHullDamage,
        Action<string, string> applyModuleDamage)
    {
        _getHullIntegrity = getHullIntegrity;
        _getModuleSlots = getModuleSlots;
        _applyHullDamage = applyHullDamage;
        _applyModuleDamage = applyModuleDamage;
    }

    /// <summary>Injects #11 exploration boundaries.</summary>
    public void SetExplorationDelegates(
        Func<string, bool> isThreatActive,
        Action<string> suppressThreat,
        Func<CombatVector2> getPlayerPosition,
        Action<string, string> resumeExploration)
    {
        _isThreatActive = isThreatActive;
        _suppressThreat = suppressThreat;
        _getPlayerPosition = getPlayerPosition;
        _resumeExploration = resumeExploration;
    }

    /// <summary>Clears pending threats.</summary>
    public void ClearQueue() => _threatQueue.Clear();

    /// <summary>Resets the exploration-session retreat flag when #11 starts a fresh session.</summary>
    public void ResetRetreatFlagged() => _retreatFlagged = false;

    /// <summary>Single entry point called by Exploration when a guard threat triggers.</summary>
    public ResolveThreatResult ResolveThreat(ThreatContext? threatContext)
    {
        if (threatContext is null || string.IsNullOrWhiteSpace(threatContext.ThreatId))
        {
            _errors.Add("Combat: resolve_threat called with null or invalid threat_context");
            return new ResolveThreatResult("error", Error: "ERR_INVALID_CONTEXT");
        }

        var validatedContext = threatContext with
        {
            EncounterParams = ValidateThreatParams(threatContext.EncounterParams).ToDictionary()
        };

        if (State != CombatState.Idle)
        {
            var queued = EnqueueThreat(validatedContext);
            return new ResolveThreatResult("busy", Error: "ERR_BUSY", Queued: queued, ThreatId: validatedContext.ThreatId);
        }

        _currentThreat = validatedContext;
        State = CombatState.AwaitingResponse;
        ThreatTriggered?.Invoke(validatedContext);
        return new ResolveThreatResult("awaiting_response", ThreatId: validatedContext.ThreatId);
    }

    /// <summary>Submits the player response and runs the strict C4 settlement sequence.</summary>
    public CombatResult SubmitResponse(string responseChoice)
    {
        if (State != CombatState.AwaitingResponse || _currentThreat is null)
        {
            return CombatResult.Fail("ERR_NOT_AWAITING_RESPONSE");
        }

        if (!IsValidResponse(responseChoice))
        {
            _errors.Add($"Combat: invalid response_choice: {responseChoice}");
            return CombatResult.Fail("ERR_INVALID_RESPONSE", responseChoice);
        }

        State = CombatState.Processing;
        var result = ExecuteSettlement(responseChoice);
        if (!result.Success)
        {
            State = CombatState.AwaitingResponse;
            return result;
        }

        _lastResult = result;
        State = CombatState.Resolved;
        var threatId = _currentThreat.ThreatId;
        CombatResultReady?.Invoke(result, threatId);
        EmitResolutionSignals(result, threatId);
        return result;
    }

    /// <summary>
    /// Moves RESOLVED to IDLE, then starts the next still-active queued threat if present.
    /// Tests call this explicitly to model the one-frame handoff from Exploration.
    /// </summary>
    public void CompleteResolvedFrame()
    {
        if (State != CombatState.Resolved)
        {
            return;
        }

        State = CombatState.Idle;
        _currentThreat = null;
        StartNextQueuedThreat();
    }

    /// <summary>Returns current UI response availability without changing state.</summary>
    public IReadOnlyList<CombatResponseOption> GetAvailableResponses()
    {
        var currentParams = _currentThreat is null
            ? ThreatParams.Defaults
            : ValidateThreatParams(_currentThreat.EncounterParams);
        var hull = _getHullIntegrity();
        var emergencyAvailable = currentParams.CanBeSuppressed && CheckEmergencyAvailable(currentParams);
        var disabledReason = currentParams.CanBeSuppressed
            ? "需要 repair_kit x1（随身物品栏中无可用）"
            : "该威胁不可应急处理";

        return
        [
            new CombatResponseOption(
                CombatResponses.EmergencyHandling,
                "应急处理",
                emergencyAvailable,
                emergencyAvailable ? "" : disabledReason),
            new CombatResponseOption(
                CombatResponses.Tank,
                hull <= 12 ? "硬扛 - 船体严重受损" : "硬扛",
                true,
                Warning: hull <= 12 ? "船体严重受损" : "",
                DamagePreview: $"{currentParams.FullDamageMin}-{currentParams.FullDamageMax} 船体伤害",
                CrossBandWarning: hull <= 33 && hull > 25),
            new CombatResponseOption(CombatResponses.Retreat, "撤退", true),
        ];
    }

    /// <summary>Pure hull damage formula F-12-02.</summary>
    public int CalcHullDamage(string responseChoice, IReadOnlyDictionary<string, object?> encounterParams)
    {
        if (responseChoice != CombatResponses.Tank)
        {
            return 0;
        }

        var parameters = ValidateThreatParams(encounterParams);
        return _randomRange(parameters.FullDamageMin, parameters.FullDamageMax);
    }

    /// <summary>Pure module damage formula F-12-03 with actual_state filtering.</summary>
    public CombatModuleDamage CalcModuleDamage(string responseChoice, IReadOnlyDictionary<string, object?> encounterParams)
    {
        if (responseChoice != CombatResponses.Tank)
        {
            return new CombatModuleDamage(false, null, "guard_impact");
        }

        var parameters = ValidateThreatParams(encounterParams);
        if (_random() >= parameters.ModuleDamageChance)
        {
            return new CombatModuleDamage(false, null, "guard_impact");
        }

        var eligible = GetEligibleModuleSlots();
        if (eligible.Count == 0)
        {
            return new CombatModuleDamage(false, null, "guard_impact");
        }

        var index = Math.Clamp(_randomRange(0, eligible.Count - 1), 0, eligible.Count - 1);
        return new CombatModuleDamage(true, eligible[index], "guard_impact");
    }

    /// <summary>Checks emergency handling availability against carried repair materials.</summary>
    public bool CheckEmergencyAvailable(ThreatParams? parameters = null)
    {
        var cost = Math.Max(1, parameters?.EmergencyCostRepairKit ?? ThreatParams.Defaults.EmergencyCostRepairKit);
        var carried = _getRepairMaterials();
        return carried.GetValueOrDefault(RepairKitId, 0) >= cost;
    }

    /// <summary>Pure knockback formula F-12-05.</summary>
    public CombatKnockback? CalcKnockback(
        string responseChoice,
        IReadOnlyDictionary<string, object?> encounterParams,
        ThreatContext threatContext)
    {
        var parameters = ValidateThreatParams(encounterParams);
        var distance = responseChoice switch
        {
            CombatResponses.Tank => parameters.KnockbackDistanceTanked,
            CombatResponses.Retreat => parameters.KnockbackDistanceRetreat,
            _ => 0.0d,
        };

        if (distance <= 0.0d)
        {
            return null;
        }

        var direction = (_getPlayerPosition() - threatContext.Position).Normalized();
        if (direction.LengthSquared < 0.0001d)
        {
            direction = threatContext.Facing.Normalized();
        }

        if (direction.LengthSquared < 0.0001d)
        {
            var angle = _random() * Math.Tau;
            direction = new CombatVector2(Math.Cos(angle), Math.Sin(angle)).Normalized();
        }

        return new CombatKnockback(direction, distance);
    }

    /// <summary>Validates and fills threat parameters from EncounterContext values.</summary>
    public ThreatParams ValidateThreatParams(IReadOnlyDictionary<string, object?>? values)
    {
        values ??= new Dictionary<string, object?>(StringComparer.Ordinal);
        var defaults = ThreatParams.Defaults;
        var min = ReadInt(values, "full_damage_min", ReadInt(values, "hull_damage_min", defaults.FullDamageMin));
        var max = ReadInt(values, "full_damage_max", ReadInt(values, "hull_damage_max", defaults.FullDamageMax));
        if (max < min)
        {
            _warnings.Add($"Combat: full_damage_min ({min}) > full_damage_max ({max}) - swapping");
            (min, max) = (max, min);
        }

        var triggerRadiusMax = ReadDouble(values, "trigger_radius_max", ReadDouble(values, "trigger_radius", defaults.TriggerRadiusMax));
        var knockbackTanked = ReadDouble(values, "knockback_distance_tanked", defaults.KnockbackDistanceTanked);
        if (knockbackTanked <= triggerRadiusMax)
        {
            _errors.Add($"Combat: knockback_distance_tanked ({knockbackTanked:0.0}) <= trigger_radius_max ({triggerRadiusMax:0.0}) - clamping");
            knockbackTanked = triggerRadiusMax + 2.0d;
        }

        return new ThreatParams(
            ReadString(values, "threat_category", ReadString(values, "threat_type", defaults.ThreatCategory)),
            min,
            max,
            ReadDouble(values, "module_damage_chance", defaults.ModuleDamageChance),
            Math.Max(1, ReadInt(values, "emergency_cost_repair_kit", defaults.EmergencyCostRepairKit)),
            knockbackTanked,
            ReadDouble(values, "knockback_distance_retreat", defaults.KnockbackDistanceRetreat),
            ReadBool(values, "can_be_suppressed", defaults.CanBeSuppressed),
            ReadDouble(values, "trigger_radius_min", defaults.TriggerRadiusMin),
            triggerRadiusMax);
    }

    private bool EnqueueThreat(ThreatContext threatContext)
    {
        if (_currentThreat?.ThreatId == threatContext.ThreatId
            || _threatQueue.Any(entry => entry.ThreatId == threatContext.ThreatId))
        {
            return false;
        }

        if (_threatQueue.Count >= MaxQueueDepth)
        {
            var dropped = _threatQueue.Dequeue();
            _warnings.Add($"threat queue full - dropping oldest entry {dropped.ThreatId}");
        }

        _threatQueue.Enqueue(threatContext);
        return true;
    }

    private void StartNextQueuedThreat()
    {
        while (_threatQueue.Count > 0)
        {
            var next = _threatQueue.Dequeue();
            if (!_isThreatActive(next.ThreatId))
            {
                continue;
            }

            ResolveThreat(next);
            return;
        }
    }

    private CombatResult ExecuteSettlement(string responseChoice)
    {
        var currentThreat = _currentThreat!;
        var parameters = ValidateThreatParams(currentThreat.EncounterParams);

        if (responseChoice == CombatResponses.EmergencyHandling
            && (!parameters.CanBeSuppressed || !CheckEmergencyAvailable(parameters)))
        {
            return CombatResult.Fail("ERR_UNAVAILABLE", "repair_kit not available");
        }

        var resourcesConsumed = new List<CombatResourceConsumption>();
        if (responseChoice == CombatResponses.EmergencyHandling)
        {
            _consumeInCombat(RepairKitId, parameters.EmergencyCostRepairKit);
            resourcesConsumed.Add(new CombatResourceConsumption(RepairKitId, parameters.EmergencyCostRepairKit));
        }

        var hullDamage = CalcHullDamage(responseChoice, currentThreat.EncounterParams);
        var moduleRoll = CalcModuleDamage(responseChoice, currentThreat.EncounterParams);
        if (hullDamage > 0)
        {
            _applyHullDamage(hullDamage);
        }

        CombatModuleDamage? moduleDamage = null;
        if (moduleRoll.ModuleDamaged && !string.IsNullOrWhiteSpace(moduleRoll.SlotId))
        {
            _applyModuleDamage(moduleRoll.SlotId, moduleRoll.DamageType);
            moduleDamage = moduleRoll;
        }

        if (responseChoice == CombatResponses.EmergencyHandling)
        {
            _suppressThreat(currentThreat.ThreatId);
        }

        var knockback = CalcKnockback(responseChoice, currentThreat.EncounterParams, currentThreat);
        if (responseChoice == CombatResponses.Retreat)
        {
            _retreatFlagged = true;
        }

        var outcome = responseChoice switch
        {
            CombatResponses.EmergencyHandling => "suppressed",
            CombatResponses.Tank => "tanked",
            CombatResponses.Retreat => "retreated",
            _ => "unknown",
        };
        var result = new CombatResult(
            outcome,
            hullDamage,
            moduleDamage,
            resourcesConsumed.Count > 0 ? resourcesConsumed : null,
            knockback,
            _retreatFlagged);
        _resumeExploration(outcome, currentThreat.ThreatId);
        return result;
    }

    private void EmitResolutionSignals(CombatResult result, string threatId)
    {
        switch (result.Outcome)
        {
            case "suppressed":
                ThreatSuppressed?.Invoke(threatId);
                break;
            case "tanked":
                ThreatTanked?.Invoke(threatId, result.HullDamage);
                break;
            case "retreated":
                ThreatRetreated?.Invoke(threatId);
                break;
        }

        ThreatResolved?.Invoke(result.Outcome, threatId);
    }

    private IReadOnlyList<string> GetEligibleModuleSlots()
    {
        return _getModuleSlots()
            .Where(slot => slot.ActualState == ModuleActualState.Installed)
            .Select(slot => slot.SlotId)
            .ToArray();
    }

    private static bool IsValidResponse(string responseChoice)
    {
        return responseChoice is CombatResponses.EmergencyHandling
            or CombatResponses.Tank
            or CombatResponses.Retreat;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> values, string key, int fallback)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            int i => i,
            long l => checked((int)l),
            double d => checked((int)d),
            float f => checked((int)f),
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => fallback,
        };
    }

    private static double ReadDouble(IReadOnlyDictionary<string, object?> values, string key, double fallback)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => fallback,
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> values, string key, bool fallback)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            int i => i != 0,
            long l => l != 0,
            _ => fallback,
        };
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> values, string key, string fallback)
    {
        return values.TryGetValue(key, out var value) && value is not null
            ? value.ToString() ?? fallback
            : fallback;
    }
}

internal static class ThreatParamsExtensions
{
    public static Dictionary<string, object?> ToDictionary(this ThreatParams parameters)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["threat_category"] = parameters.ThreatCategory,
            ["full_damage_min"] = parameters.FullDamageMin,
            ["full_damage_max"] = parameters.FullDamageMax,
            ["module_damage_chance"] = parameters.ModuleDamageChance,
            ["emergency_cost_repair_kit"] = parameters.EmergencyCostRepairKit,
            ["knockback_distance_tanked"] = parameters.KnockbackDistanceTanked,
            ["knockback_distance_retreat"] = parameters.KnockbackDistanceRetreat,
            ["can_be_suppressed"] = parameters.CanBeSuppressed,
            ["trigger_radius_min"] = parameters.TriggerRadiusMin,
            ["trigger_radius_max"] = parameters.TriggerRadiusMax,
        };
    }
}
