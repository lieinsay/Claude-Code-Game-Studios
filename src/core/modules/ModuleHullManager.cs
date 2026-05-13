using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Module type installed in an open airship module slot.
/// </summary>
public enum ModuleType
{
    Empty = 0,
    Scout = 1,
    Cargo = 2,
}

/// <summary>
/// Physical module state written by voyage, combat, and repair systems.
/// </summary>
public enum ModuleActualState
{
    Empty = 0,
    Installed = 1,
    Damaged = 2,
}

/// <summary>
/// Player-visible module state used for planning and efficiency calculations.
/// </summary>
public enum ModuleVisibleState
{
    Empty = 0,
    Installed = 1,
    Damaged = 2,
    Unchecked = 3,
}

/// <summary>
/// Hull integrity band and its navigation penalties.
/// </summary>
public enum HullBand
{
    Intact = 0,
    Damaged = 1,
    Critical = 2,
    Destroyed = 3,
}

/// <summary>
/// Result codes for module and hull operations.
/// </summary>
public enum ModuleHullResult
{
    Success = 0,
    ErrInvalidSlot = 1,
    ErrSlotOccupied = 2,
    ErrSlotEmpty = 3,
    ErrInsufficientResources = 4,
    ErrInvalidState = 5,
    ErrSameModuleType = 6,
    ErrCargoBayNotEmpty = 7,
    ErrBusy = 8,
    ErrModuleNotAvailable = 9,
    ErrHullAlreadyFull = 10,
    ErrInvalidModuleType = 11,
}

/// <summary>
/// Read-only snapshot of one module slot.
/// </summary>
public sealed record ModuleSlotSnapshot(
    string SlotId,
    ModuleType ModuleType,
    ModuleActualState ActualState,
    ModuleVisibleState VisibleState,
    double Efficiency,
    string DamageType)
{
    /// <summary>Whether the slot can receive interaction focus in the hub.</summary>
    public bool IsInteractable => true;
}

/// <summary>
/// Hull state summary for UI, persistence, and tests.
/// </summary>
public sealed record HullStateSnapshot(int Integrity, int Scars, HullBand Band);

/// <summary>
/// Penalty multipliers derived from the current hull band.
/// </summary>
public sealed record HullBandPenalties(
    double SpeedMultiplier,
    double FuelMultiplier,
    double ModuleEfficiencyMultiplier,
    bool HighRiskBlocked);

/// <summary>
/// Structured departure gate result consumed by Hub and navigation systems.
/// </summary>
public sealed record DepartureReadiness(bool CanDepart, IReadOnlyList<string> Reasons);

/// <summary>
/// Operation result with optional refund and user-facing reason.
/// </summary>
public sealed record ModuleHullOperationResult(
    ModuleHullResult Result,
    IReadOnlyDictionary<string, int> Refund,
    string Message = "")
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success => Result == ModuleHullResult.Success;

    /// <summary>Creates a successful operation result.</summary>
    public static ModuleHullOperationResult Ok(IReadOnlyDictionary<string, int>? refund = null)
    {
        return new ModuleHullOperationResult(
            ModuleHullResult.Success,
            refund ?? new Dictionary<string, int>(StringComparer.Ordinal));
    }

    /// <summary>Creates a failed operation result.</summary>
    public static ModuleHullOperationResult Fail(ModuleHullResult result, string message = "")
    {
        return new ModuleHullOperationResult(result, new Dictionary<string, int>(StringComparer.Ordinal), message);
    }
}

/// <summary>
/// Core C# implementation of Epic #8 Modules & Hull State.
/// </summary>
public sealed class ModuleHullManager
{
    /// <summary>Open module slot A.</summary>
    public const string SlotA = "slot_a";

    /// <summary>Open module slot B.</summary>
    public const string SlotB = "slot_b";

    /// <summary>Basic supply stable resource ID.</summary>
    public const string BasicSupplyId = "resource.basic_supply";

    /// <summary>Repair kit stable resource ID.</summary>
    public const string RepairKitId = "resource.repair_kit";

    private const int ScoutFurnaceRating = 8;
    private const int CargoFurnaceRating = 12;
    private const int CargoVolumeBonus = 500;
    private const int HullIntegrityMax = 100;
    private const int HullRepairValuePerKit = 5;
    private const double RefundRatio = 0.75d;

    private static readonly string[] SlotIds = [SlotA, SlotB];

    private static readonly Dictionary<ModuleType, Dictionary<string, int>> InstallCosts = new()
    {
        [ModuleType.Scout] = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [BasicSupplyId] = 5,
            [RepairKitId] = 2,
        },
        [ModuleType.Cargo] = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [BasicSupplyId] = 3,
            [RepairKitId] = 3,
        },
    };

    private readonly Dictionary<string, SlotState> slots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> cachedEfficiency = new(StringComparer.Ordinal);
    private readonly ResourcesManager? resources;
    private int hullIntegrity = HullIntegrityMax;
    private int hullScars;
    private HullBand hullBand = HullBand.Intact;
    private bool scoutAvailable;
    private bool isMutating;
    private int cachedCargoVolume;
    private DepartureReadiness cachedDeparture = new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a module/hull manager. When a ResourcesManager is supplied, material
    /// costs, loaded mass, and cargo bay volume updates are integrated.
    /// </summary>
    public ModuleHullManager(ResourcesManager? resources = null)
    {
        this.resources = resources;
        ApplyStartingState();
    }

    /// <summary>Visible slot state changed after mutation.</summary>
    public event Action<string, string, string>? SlotStateChanged;

    /// <summary>Actual slot state changed after mutation.</summary>
    public event Action<string, string, string>? ActualStateChanged;

    /// <summary>Hull integrity changed after mutation.</summary>
    public event Action<int, int>? HullIntegrityChanged;

    /// <summary>Hull band changed after mutation.</summary>
    public event Action<string, string>? HullBandChanged;

    /// <summary>Final module efficiency changed after mutation.</summary>
    public event Action<string, double, double>? ModuleEfficiencyChanged;

    /// <summary>Departure readiness changed after mutation.</summary>
    public event Action<bool, IReadOnlyList<string>>? DepartureReadinessChanged;

    /// <summary>Machine-readable non-fatal restore and stale-data warnings.</summary>
    public IReadOnlyList<string> Warnings => warnings;

    private readonly List<string> warnings = [];

    /// <summary>Whether the scout module has been delivered by the exploration reward chain.</summary>
    public bool IsScoutModuleAvailable => scoutAvailable;

    /// <summary>Returns all canonical slot IDs in stable order.</summary>
    public IReadOnlyList<string> GetSlotIds()
    {
        return SlotIds.ToArray();
    }

    /// <summary>Returns a copy of one slot state.</summary>
    public ModuleSlotSnapshot GetSlotState(string slotId)
    {
        return IsValidSlot(slotId)
            ? BuildSlotSnapshot(slotId, slots[slotId])
            : new ModuleSlotSnapshot(slotId, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty, 0.0d, string.Empty);
    }

    /// <summary>Returns true for every valid physical slot, including empty slots.</summary>
    public bool IsSlotInteractable(string slotId)
    {
        return IsValidSlot(slotId);
    }

    /// <summary>Returns the visible-state efficiency before hull-band multiplication.</summary>
    public double GetModuleEfficiency(string slotId)
    {
        return IsValidSlot(slotId) ? GetVisibleEfficiency(slots[slotId]) : 0.0d;
    }

    /// <summary>Returns the final efficiency including hull-band penalties.</summary>
    public double GetFinalModuleEfficiency(string slotId)
    {
        return IsValidSlot(slotId) ? GetVisibleEfficiency(slots[slotId]) * GetHullEfficiencyMultiplier() : 0.0d;
    }

    /// <summary>Returns the current hull state.</summary>
    public HullStateSnapshot GetHullState()
    {
        return new HullStateSnapshot(hullIntegrity, hullScars, hullBand);
    }

    /// <summary>Returns penalty multipliers for the current hull band.</summary>
    public HullBandPenalties GetHullBandPenalties()
    {
        return hullBand switch
        {
            HullBand.Intact => new HullBandPenalties(1.0d, 1.0d, 1.0d, false),
            HullBand.Damaged => new HullBandPenalties(0.9d, 1.15d, 1.0d, false),
            HullBand.Critical => new HullBandPenalties(0.75d, 1.3d, 0.8d, true),
            HullBand.Destroyed => new HullBandPenalties(0.0d, 0.0d, 0.0d, true),
            _ => new HullBandPenalties(1.0d, 1.0d, 1.0d, false),
        };
    }

    /// <summary>Installs a module into an empty slot, consuming module materials when available.</summary>
    public ModuleHullOperationResult InstallModule(string slotId, ModuleType moduleType)
    {
        if (!TryBeginMutation(out var busy))
        {
            return busy;
        }

        try
        {
            if (!IsValidSlot(slotId))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidSlot);
            }

            if (!IsInstallableModule(moduleType))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidModuleType);
            }

            if (!CanInstallModuleType(moduleType))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrModuleNotAvailable);
            }

            var slot = slots[slotId];
            if (slot.VisibleState != ModuleVisibleState.Empty)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrSlotOccupied);
            }

            var before = CaptureDerivedState();
            var cost = GetInstallCost(moduleType);
            if (!ConsumeMaterials(cost))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInsufficientResources);
            }

            slot.ModuleType = moduleType;
            slot.ActualState = ModuleActualState.Installed;
            slot.VisibleState = ModuleVisibleState.Installed;
            slot.DamageType = string.Empty;
            EmitSlotState(slotId, ModuleVisibleState.Empty, ModuleVisibleState.Installed);
            EmitDerivedChanges(before, [slotId]);
            return ModuleHullOperationResult.Ok();
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Uninstalls a module and grants the installed-state refund when eligible.</summary>
    public ModuleHullOperationResult UninstallModule(string slotId)
    {
        if (!TryBeginMutation(out var busy))
        {
            return busy;
        }

        try
        {
            if (!IsValidSlot(slotId))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidSlot);
            }

            var slot = slots[slotId];
            if (slot.VisibleState == ModuleVisibleState.Empty)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrSlotEmpty);
            }

            var before = CaptureDerivedState();
            var oldVisible = slot.VisibleState;
            var refund = oldVisible == ModuleVisibleState.Installed
                ? CalculateRefund(slot.ModuleType)
                : new Dictionary<string, int>(StringComparer.Ordinal);
            GrantMaterials(refund);
            ClearSlot(slot);
            EmitSlotState(slotId, oldVisible, ModuleVisibleState.Empty);
            EmitDerivedChanges(before, [slotId]);
            var message = oldVisible switch
            {
                ModuleVisibleState.Damaged => "module_damaged_no_refund",
                ModuleVisibleState.Unchecked => "module_unchecked_no_refund",
                _ => string.Empty,
            };
            return new ModuleHullOperationResult(ModuleHullResult.Success, refund, message);
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Atomically replaces one installed module with another module type.</summary>
    public ModuleHullOperationResult SwapModule(string slotId, ModuleType newModuleType)
    {
        if (!TryBeginMutation(out var busy))
        {
            return busy;
        }

        try
        {
            if (!IsValidSlot(slotId))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidSlot);
            }

            if (!IsInstallableModule(newModuleType))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidModuleType);
            }

            if (!CanInstallModuleType(newModuleType))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrModuleNotAvailable);
            }

            var slot = slots[slotId];
            if (slot.VisibleState == ModuleVisibleState.Empty)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrSlotEmpty);
            }

            var oldType = slot.ModuleType;
            if (oldType == newModuleType)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrSameModuleType, "same_module_type");
            }

            if (oldType == ModuleType.Cargo
                && newModuleType != ModuleType.Cargo
                && GetCargoBayUsedVolume() > 0)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrCargoBayNotEmpty);
            }

            var refund = slot.VisibleState == ModuleVisibleState.Installed
                ? CalculateRefund(oldType)
                : new Dictionary<string, int>(StringComparer.Ordinal);
            var netCost = CalculateNetCost(GetInstallCost(newModuleType), refund);
            if (!CanConsumeMaterials(netCost))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInsufficientResources);
            }

            var before = CaptureDerivedState();
            var oldVisible = slot.VisibleState;
            ConsumeMaterials(netCost);
            slot.ModuleType = newModuleType;
            slot.ActualState = ModuleActualState.Installed;
            slot.VisibleState = ModuleVisibleState.Installed;
            slot.DamageType = string.Empty;
            if (oldVisible != ModuleVisibleState.Installed)
            {
                EmitSlotState(slotId, oldVisible, ModuleVisibleState.Installed);
            }

            EmitDerivedChanges(before, [slotId]);
            return ModuleHullOperationResult.Ok(refund);
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Checks an unchecked module, synchronizing visible state to actual state without cost.</summary>
    public ModuleHullOperationResult InspectModule(string slotId)
    {
        if (!TryBeginMutation(out var busy))
        {
            return busy;
        }

        try
        {
            if (!IsValidSlot(slotId))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidSlot);
            }

            var slot = slots[slotId];
            if (slot.VisibleState != ModuleVisibleState.Unchecked)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidState);
            }

            var before = CaptureDerivedState();
            var newVisible = slot.ActualState == ModuleActualState.Damaged
                ? ModuleVisibleState.Damaged
                : ModuleVisibleState.Installed;
            slot.VisibleState = newVisible;
            EmitSlotState(slotId, ModuleVisibleState.Unchecked, newVisible);
            EmitDerivedChanges(before, [slotId]);
            return ModuleHullOperationResult.Ok();
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Repairs a damaged or unchecked module, consuming repair materials.</summary>
    public ModuleHullOperationResult RepairModule(string slotId)
    {
        if (!TryBeginMutation(out var busy))
        {
            return busy;
        }

        try
        {
            if (!IsValidSlot(slotId))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidSlot);
            }

            var slot = slots[slotId];
            if (slot.VisibleState is not (ModuleVisibleState.Damaged or ModuleVisibleState.Unchecked))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidState);
            }

            var cost = new Dictionary<string, int>(StringComparer.Ordinal) { [RepairKitId] = 2 };
            if (!ConsumeMaterials(cost))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInsufficientResources);
            }

            var before = CaptureDerivedState();
            var oldActual = slot.ActualState;
            var oldVisible = slot.VisibleState;
            slot.ActualState = ModuleActualState.Installed;
            slot.VisibleState = ModuleVisibleState.Installed;
            slot.DamageType = string.Empty;
            if (oldActual != ModuleActualState.Installed)
            {
                EmitActualState(slotId, oldActual, ModuleActualState.Installed);
            }

            EmitSlotState(slotId, oldVisible, ModuleVisibleState.Installed);
            EmitDerivedChanges(before, [slotId]);
            return ModuleHullOperationResult.Ok();
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Applies voyage completion actual-state writes and unchecked transitions.</summary>
    public void CompleteVoyage(IReadOnlyDictionary<string, bool>? damagedSlots = null)
    {
        if (!TryBeginMutation(out _))
        {
            return;
        }

        try
        {
            var before = CaptureDerivedState();
            var affected = new List<string>();
            foreach (var slotId in SlotIds)
            {
                var slot = slots[slotId];
                if (slot.VisibleState == ModuleVisibleState.Empty)
                {
                    continue;
                }

                var oldActual = slot.ActualState;
                var oldVisible = slot.VisibleState;
                if (damagedSlots?.GetValueOrDefault(slotId) == true)
                {
                    slot.ActualState = ModuleActualState.Damaged;
                    slot.DamageType = "voyage";
                }
                else if (oldActual != ModuleActualState.Damaged)
                {
                    slot.ActualState = ModuleActualState.Installed;
                }

                if (slot.ActualState != oldActual)
                {
                    EmitActualState(slotId, oldActual, slot.ActualState);
                }

                if (oldVisible != ModuleVisibleState.Damaged)
                {
                    slot.VisibleState = ModuleVisibleState.Unchecked;
                }

                if (slot.VisibleState != oldVisible)
                {
                    EmitSlotState(slotId, oldVisible, slot.VisibleState);
                    affected.Add(slotId);
                }
            }

            EmitDerivedChanges(before, affected);
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Applies hull damage from navigation, exploration, or combat threat resolution.</summary>
    public void ApplyHullDamage(int amount)
    {
        if (amount <= 0 || !TryBeginMutation(out _))
        {
            return;
        }

        try
        {
            var before = CaptureDerivedState();
            var oldIntegrity = hullIntegrity;
            var oldBand = hullBand;
            hullIntegrity = Math.Max(0, hullIntegrity - amount);
            hullScars += 1 + CountNewBandEntries(oldIntegrity, hullIntegrity);
            hullBand = GetBandForIntegrity(hullIntegrity);

            HullIntegrityChanged?.Invoke(oldIntegrity, hullIntegrity);
            if (hullBand != oldBand)
            {
                HullBandChanged?.Invoke(BandName(oldBand), BandName(hullBand));
            }

            EmitDerivedChanges(before, SlotIds);
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Repairs hull integrity by consuming repair kits.</summary>
    public ModuleHullOperationResult RepairHull(int repairKitCount)
    {
        if (!TryBeginMutation(out var busy))
        {
            return busy;
        }

        try
        {
            if (hullIntegrity >= HullIntegrityMax)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrHullAlreadyFull, "hull_full");
            }

            if (repairKitCount <= 0)
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInvalidState);
            }

            var cost = new Dictionary<string, int>(StringComparer.Ordinal) { [RepairKitId] = repairKitCount };
            if (!ConsumeMaterials(cost))
            {
                return ModuleHullOperationResult.Fail(ModuleHullResult.ErrInsufficientResources);
            }

            var before = CaptureDerivedState();
            var oldIntegrity = hullIntegrity;
            var oldBand = hullBand;
            hullIntegrity = Math.Min(HullIntegrityMax, hullIntegrity + repairKitCount * HullRepairValuePerKit);
            hullBand = GetBandForIntegrity(hullIntegrity);
            HullIntegrityChanged?.Invoke(oldIntegrity, hullIntegrity);
            if (hullBand != oldBand)
            {
                HullBandChanged?.Invoke(BandName(oldBand), BandName(hullBand));
            }

            EmitDerivedChanges(before, SlotIds);
            return ModuleHullOperationResult.Ok();
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Applies direct module damage from combat/threat handling.</summary>
    public void ApplyModuleDamage(string slotId, string damageType)
    {
        if (!IsValidSlot(slotId) || !TryBeginMutation(out _))
        {
            return;
        }

        try
        {
            var slot = slots[slotId];
            if (slot.VisibleState == ModuleVisibleState.Empty || slot.ActualState == ModuleActualState.Damaged)
            {
                return;
            }

            var before = CaptureDerivedState();
            var oldActual = slot.ActualState;
            var oldVisible = slot.VisibleState;
            slot.ActualState = ModuleActualState.Damaged;
            slot.VisibleState = ModuleVisibleState.Damaged;
            slot.DamageType = string.IsNullOrWhiteSpace(damageType) ? string.Empty : damageType;
            EmitActualState(slotId, oldActual, ModuleActualState.Damaged);
            EmitSlotState(slotId, oldVisible, ModuleVisibleState.Damaged);
            EmitDerivedChanges(before, [slotId]);
        }
        finally
        {
            EndMutation();
        }
    }

    /// <summary>Marks the scout module as available for installation.</summary>
    public void UnlockScoutModule()
    {
        scoutAvailable = true;
    }

    /// <summary>Returns true when a module type may be installed by the player.</summary>
    public bool CanInstallModuleType(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Cargo => true,
            ModuleType.Scout => scoutAvailable,
            _ => false,
        };
    }

    /// <summary>Returns maximum load from module furnaces and hull efficiency.</summary>
    public int GetMaxLoad()
    {
        return (int)Math.Floor(slots.Values.Sum(slot => GetFurnaceRating(slot.ModuleType) * GetVisibleEfficiency(slot) * GetHullEfficiencyMultiplier()));
    }

    /// <summary>Returns one slot's furnace load contribution after final efficiency.</summary>
    public int GetSlotFurnaceContribution(string slotId)
    {
        return IsValidSlot(slotId)
            ? (int)Math.Floor(GetFurnaceRating(slots[slotId].ModuleType) * GetFinalModuleEfficiency(slotId))
            : 0;
    }

    /// <summary>Returns cargo bay effective volume from installed cargo modules.</summary>
    public int GetEffectiveCargoVolume()
    {
        return (int)Math.Floor(slots.Values
            .Where(slot => slot.ModuleType == ModuleType.Cargo)
            .Sum(slot => CargoVolumeBonus * GetVisibleEfficiency(slot) * GetHullEfficiencyMultiplier()));
    }

    /// <summary>Returns one slot's cargo volume contribution.</summary>
    public int GetSlotCargoVolumeContribution(string slotId)
    {
        if (!IsValidSlot(slotId) || slots[slotId].ModuleType != ModuleType.Cargo)
        {
            return 0;
        }

        return (int)Math.Floor(CargoVolumeBonus * GetFinalModuleEfficiency(slotId));
    }

    /// <summary>Returns scout risk visibility efficiency with redundant dual-scout protection.</summary>
    public double GetScoutVisibilityEfficiency()
    {
        var scoutEfficiencies = slots.Values
            .Where(slot => slot.ModuleType == ModuleType.Scout && slot.VisibleState != ModuleVisibleState.Empty)
            .Select(slot => GetVisibleEfficiency(slot) * GetHullEfficiencyMultiplier())
            .ToArray();
        return scoutEfficiencies.Length == 0 ? 0.0d : scoutEfficiencies.Max();
    }

    /// <summary>Evaluates departure readiness from furnace load, hull integrity, and loaded mass.</summary>
    public DepartureReadiness CanDepart()
    {
        var reasons = new List<string>();
        var maxLoad = GetMaxLoad();
        var loaded = resources?.GetTotalLoadedMass() ?? 0;
        if (maxLoad <= 0)
        {
            reasons.Add("no_furnace");
        }

        if (loaded > maxLoad)
        {
            reasons.Add("overloaded");
        }

        if (hullIntegrity <= 0)
        {
            reasons.Add("hull_destroyed");
        }

        return new DepartureReadiness(reasons.Count == 0, reasons);
    }

    /// <summary>Builds a pure JSON-compatible progress.airship payload for module/hull state.</summary>
    public Dictionary<string, object?> BuildProgressAirshipSnapshot()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["domain"] = "modules_hull",
            ["version"] = 1,
            ["modules"] = SlotIds.ToDictionary(
                slotId => slotId,
                slotId => (object?)SerializeSlot(slots[slotId]),
                StringComparer.Ordinal),
            ["hull_integrity"] = hullIntegrity,
            ["hull_scars"] = hullScars,
            ["scout_module_available"] = scoutAvailable,
        };
    }

    /// <summary>Builds an ADR-0003 snapshot package for progress.airship module/hull data.</summary>
    public SnapshotPackage BuildSnapshotPackage()
    {
        var package = new SnapshotPackage
        {
            DomainId = "progress.airship.modules_hull",
            SnapshotSchemaVersion = 1,
            DomainState = SnapshotDomainState.Ready,
        };
        package.ContentDomainVersions["modules-hull-state"] = "2026-05-09";
        package.StableIdRefs.AddRange(SlotIds);
        foreach (var (key, value) in BuildProgressAirshipSnapshot())
        {
            package.Payload[key] = value;
        }

        return package;
    }

    /// <summary>Restores module/hull state from a progress.airship payload.</summary>
    public bool RestoreFromProgressAirship(IReadOnlyDictionary<string, object?> snapshot)
    {
        if (!ValidateSnapshot(snapshot))
        {
            ApplyStartingState();
            return false;
        }

        ApplyStartingState();
        var modules = ReadObjectMap(snapshot, "modules");
        foreach (var (slotId, value) in modules)
        {
            if (!IsValidSlot(slotId))
            {
                warnings.Add($"stale slot skipped:{slotId}");
                continue;
            }

            var data = ToObjectMap(value);
            var moduleType = (ModuleType)ReadInt(data, "module_type");
            if (!Enum.IsDefined(moduleType))
            {
                warnings.Add($"stale module type skipped:{slotId}:{(int)moduleType}");
                ClearSlot(slots[slotId]);
                continue;
            }

            slots[slotId].ModuleType = moduleType;
            slots[slotId].VisibleState = (ModuleVisibleState)ReadInt(data, "visible_state");
            slots[slotId].ActualState = (ModuleActualState)ReadInt(data, "actual_state");
            slots[slotId].DamageType = ReadString(data, "damage_type");
        }

        hullIntegrity = Math.Clamp(ReadInt(snapshot, "hull_integrity", HullIntegrityMax), 0, HullIntegrityMax);
        hullScars = Math.Max(0, ReadInt(snapshot, "hull_scars"));
        scoutAvailable = ReadBool(snapshot, "scout_module_available");
        hullBand = GetBandForIntegrity(hullIntegrity);
        ResetDerivedCaches();
        return true;
    }

    /// <summary>Restores from an ADR-0003 snapshot package.</summary>
    public bool TryRestoreFromSnapshotPackage(SnapshotPackage package)
    {
        return package.DomainId == "progress.airship.modules_hull"
            && RestoreFromProgressAirship(package.Payload);
    }

    /// <summary>Returns true when a snapshot has the required module map field.</summary>
    public static bool ValidateSnapshot(IReadOnlyDictionary<string, object?> snapshot)
    {
        return snapshot.Count > 0
            && snapshot.ContainsKey("modules")
            && snapshot["modules"] is IReadOnlyDictionary<string, object?>;
    }

    /// <summary>Registers module/hull serializers with the persistence pipeline.</summary>
    public void RegisterPersistence(Persistence persistence)
    {
        persistence.RegisterDomainSerializer("progress.airship.modules_hull", BuildSnapshotPackage);
        persistence.RegisterDomainDeserializer("progress.airship.modules_hull", package => TryRestoreFromSnapshotPackage(package));
    }

    /// <summary>Applies the MVP new-game module/hull starting state.</summary>
    public void ApplyStartingState()
    {
        slots.Clear();
        slots[SlotA] = new SlotState();
        slots[SlotB] = new SlotState
        {
            ModuleType = ModuleType.Cargo,
            ActualState = ModuleActualState.Installed,
            VisibleState = ModuleVisibleState.Installed,
        };
        hullIntegrity = HullIntegrityMax;
        hullScars = 0;
        hullBand = HullBand.Intact;
        scoutAvailable = false;
        warnings.Clear();
        ResetDerivedCaches();
    }

    /// <summary>Sets a slot directly for tests and system restore paths.</summary>
    public void SetSlotForTest(
        string slotId,
        ModuleType moduleType,
        ModuleActualState actualState,
        ModuleVisibleState visibleState)
    {
        if (!IsValidSlot(slotId))
        {
            return;
        }

        slots[slotId].ModuleType = moduleType;
        slots[slotId].ActualState = actualState;
        slots[slotId].VisibleState = visibleState;
        slots[slotId].DamageType = string.Empty;
        ResetDerivedCaches();
    }

    /// <summary>Sets hull state directly for tests and deterministic restore paths.</summary>
    public void SetHullForTest(int integrity, int scars = 0)
    {
        hullIntegrity = Math.Clamp(integrity, 0, HullIntegrityMax);
        hullScars = Math.Max(0, scars);
        hullBand = GetBandForIntegrity(hullIntegrity);
        ResetDerivedCaches();
    }

    private bool TryBeginMutation(out ModuleHullOperationResult busy)
    {
        if (isMutating)
        {
            busy = ModuleHullOperationResult.Fail(ModuleHullResult.ErrBusy);
            return false;
        }

        isMutating = true;
        busy = ModuleHullOperationResult.Ok();
        return true;
    }

    private void EndMutation()
    {
        isMutating = false;
    }

    private DerivedState CaptureDerivedState()
    {
        return new DerivedState(
            SlotIds.ToDictionary(slotId => slotId, GetFinalModuleEfficiency, StringComparer.Ordinal),
            GetEffectiveCargoVolume(),
            CanDepart());
    }

    private void EmitDerivedChanges(DerivedState before, IReadOnlyList<string> affectedSlots)
    {
        foreach (var slotId in affectedSlots.Distinct(StringComparer.Ordinal))
        {
            var oldEfficiency = before.Efficiency.GetValueOrDefault(slotId, 0.0d);
            var newEfficiency = GetFinalModuleEfficiency(slotId);
            if (!NearlyEqual(oldEfficiency, newEfficiency))
            {
                cachedEfficiency[slotId] = newEfficiency;
                ModuleEfficiencyChanged?.Invoke(slotId, oldEfficiency, newEfficiency);
            }
        }

        var newCargoVolume = GetEffectiveCargoVolume();
        if (newCargoVolume != before.CargoVolume)
        {
            cachedCargoVolume = newCargoVolume;
            resources?.UpdateCargoBayEffectiveVolume(newCargoVolume);
        }

        var afterDeparture = CanDepart();
        if (!ReadinessEqual(before.Departure, afterDeparture))
        {
            cachedDeparture = afterDeparture;
            DepartureReadinessChanged?.Invoke(afterDeparture.CanDepart, afterDeparture.Reasons);
        }
    }

    private void ResetDerivedCaches()
    {
        cachedEfficiency.Clear();
        foreach (var slotId in SlotIds)
        {
            cachedEfficiency[slotId] = GetFinalModuleEfficiency(slotId);
        }

        cachedCargoVolume = GetEffectiveCargoVolume();
        resources?.UpdateCargoBayEffectiveVolume(cachedCargoVolume);
        cachedDeparture = CanDepart();
    }

    private void EmitSlotState(string slotId, ModuleVisibleState oldState, ModuleVisibleState newState)
    {
        SlotStateChanged?.Invoke(slotId, VisibleName(oldState), VisibleName(newState));
    }

    private void EmitActualState(string slotId, ModuleActualState oldState, ModuleActualState newState)
    {
        ActualStateChanged?.Invoke(slotId, ActualName(oldState), ActualName(newState));
    }

    private bool ConsumeMaterials(IReadOnlyDictionary<string, int> cost)
    {
        if (cost.Count == 0)
        {
            return true;
        }

        if (resources is null)
        {
            return true;
        }

        return resources.CommitDeposit("module_hull", cost).Success;
    }

    private bool CanConsumeMaterials(IReadOnlyDictionary<string, int> cost)
    {
        return cost.Count == 0 || resources is null || resources.CanDeposit("module_hull", cost);
    }

    private void GrantMaterials(IReadOnlyDictionary<string, int> refund)
    {
        if (resources is null)
        {
            return;
        }

        foreach (var (resourceId, quantity) in refund)
        {
            if (quantity > 0)
            {
                resources.Add(ResourcePool.InStorage, resourceId, quantity);
            }
        }
    }

    private int GetCargoBayUsedVolume()
    {
        return resources?.GetCargoBayUsage().GetValueOrDefault("used_volume") as int? ?? 0;
    }

    private ModuleSlotSnapshot BuildSlotSnapshot(string slotId, SlotState slot)
    {
        return new ModuleSlotSnapshot(
            slotId,
            slot.ModuleType,
            slot.ActualState,
            slot.VisibleState,
            GetVisibleEfficiency(slot),
            slot.DamageType);
    }

    private static Dictionary<string, object?> SerializeSlot(SlotState slot)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["module_type"] = (int)slot.ModuleType,
            ["visible_state"] = (int)slot.VisibleState,
            ["actual_state"] = (int)slot.ActualState,
            ["efficiency"] = GetVisibleEfficiency(slot),
            ["damage_type"] = slot.DamageType,
        };
    }

    private static bool IsValidSlot(string slotId)
    {
        return SlotIds.Contains(slotId, StringComparer.Ordinal);
    }

    private static bool IsInstallableModule(ModuleType moduleType)
    {
        return moduleType is ModuleType.Scout or ModuleType.Cargo;
    }

    private static IReadOnlyDictionary<string, int> GetInstallCost(ModuleType moduleType)
    {
        return InstallCosts.TryGetValue(moduleType, out var cost)
            ? new Dictionary<string, int>(cost, StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private static Dictionary<string, int> CalculateRefund(ModuleType moduleType)
    {
        return GetInstallCost(moduleType)
            .ToDictionary(
                pair => pair.Key,
                pair => (int)Math.Ceiling(pair.Value * RefundRatio),
                StringComparer.Ordinal);
    }

    private static Dictionary<string, int> CalculateNetCost(
        IReadOnlyDictionary<string, int> installCost,
        IReadOnlyDictionary<string, int> refund)
    {
        return installCost.Keys
            .Union(refund.Keys, StringComparer.Ordinal)
            .Select(key => new KeyValuePair<string, int>(
                key,
                Math.Max(0, installCost.GetValueOrDefault(key) - refund.GetValueOrDefault(key))))
            .Where(pair => pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static int GetFurnaceRating(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Scout => ScoutFurnaceRating,
            ModuleType.Cargo => CargoFurnaceRating,
            _ => 0,
        };
    }

    private double GetHullEfficiencyMultiplier()
    {
        return GetHullBandPenalties().ModuleEfficiencyMultiplier;
    }

    private static double GetVisibleEfficiency(SlotState slot)
    {
        if (slot.VisibleState == ModuleVisibleState.Empty || slot.ModuleType == ModuleType.Empty)
        {
            return 0.0d;
        }

        return slot.ModuleType switch
        {
            ModuleType.Scout => slot.VisibleState switch
            {
                ModuleVisibleState.Installed => 1.0d,
                ModuleVisibleState.Damaged => 0.6d,
                ModuleVisibleState.Unchecked => 0.95d,
                _ => 0.0d,
            },
            ModuleType.Cargo => slot.VisibleState switch
            {
                ModuleVisibleState.Installed => 1.0d,
                ModuleVisibleState.Damaged => 0.5d,
                ModuleVisibleState.Unchecked => 0.95d,
                _ => 0.0d,
            },
            _ => 0.0d,
        };
    }

    private static HullBand GetBandForIntegrity(int integrity)
    {
        return integrity switch
        {
            >= 76 => HullBand.Intact,
            >= 26 => HullBand.Damaged,
            >= 1 => HullBand.Critical,
            _ => HullBand.Destroyed,
        };
    }

    private static int CountNewBandEntries(int fromIntegrity, int toIntegrity)
    {
        var count = 0;
        if (fromIntegrity >= 76 && toIntegrity <= 75)
        {
            count++;
        }

        if (fromIntegrity >= 26 && toIntegrity <= 25)
        {
            count++;
        }

        if (fromIntegrity > 0 && toIntegrity == 0)
        {
            count++;
        }

        return count;
    }

    private static string VisibleName(ModuleVisibleState state)
    {
        return state.ToString().ToLowerInvariant();
    }

    private static string ActualName(ModuleActualState state)
    {
        return state.ToString().ToLowerInvariant();
    }

    private static string BandName(HullBand band)
    {
        return band.ToString().ToLowerInvariant();
    }

    private static void ClearSlot(SlotState slot)
    {
        slot.ModuleType = ModuleType.Empty;
        slot.ActualState = ModuleActualState.Empty;
        slot.VisibleState = ModuleVisibleState.Empty;
        slot.DamageType = string.Empty;
    }

    private static bool ReadinessEqual(DepartureReadiness left, DepartureReadiness right)
    {
        return left.CanDepart == right.CanDepart && left.Reasons.SequenceEqual(right.Reasons, StringComparer.Ordinal);
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 0.0001d;
    }

    private static IReadOnlyDictionary<string, object?> ReadObjectMap(IReadOnlyDictionary<string, object?> data, string key)
    {
        return data.TryGetValue(key, out var value) ? ToObjectMap(value) : new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, object?> ToObjectMap(object? value)
    {
        return value switch
        {
            IReadOnlyDictionary<string, object?> readOnly => readOnly,
            IDictionary<string, object?> mutable => new Dictionary<string, object?>(mutable, StringComparer.Ordinal),
            _ => new Dictionary<string, object?>(StringComparer.Ordinal),
        };
    }

    private static int ReadInt(IReadOnlyDictionary<string, object?> data, string key, int fallback = 0)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
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

    private static string ReadString(IReadOnlyDictionary<string, object?> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            int i => i != 0,
            long l => l != 0,
            _ => false,
        };
    }

    private sealed record DerivedState(
        IReadOnlyDictionary<string, double> Efficiency,
        int CargoVolume,
        DepartureReadiness Departure);

    private sealed class SlotState
    {
        public ModuleType ModuleType { get; set; } = ModuleType.Empty;

        public ModuleActualState ActualState { get; set; } = ModuleActualState.Empty;

        public ModuleVisibleState VisibleState { get; set; } = ModuleVisibleState.Empty;

        public string DamageType { get; set; } = string.Empty;
    }
}
