using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 007: Module Snapshot Persistence ===");

Run("AC-1/2: snapshot contains only owned primitive state, not derived gates", () =>
{
    var manager = MakeManager();
    var snapshot = manager.BuildProgressAirshipSnapshot();
    var encoded = Persistence.CanonicalJsonEncode(snapshot);
    return snapshot.ContainsKey("modules")
        && snapshot.ContainsKey("hull_integrity")
        && snapshot.ContainsKey("hull_scars")
        && !encoded.Contains("M_max", StringComparison.OrdinalIgnoreCase)
        && !encoded.Contains("can_depart", StringComparison.OrdinalIgnoreCase)
        && !encoded.Contains("System.Object", StringComparison.Ordinal);
});

Run("AC-3/4/9/10: save-load roundtrip restores slots, hull, scars, and string keys", () =>
{
    var source = MakeManager();
    source.UnlockScoutModule();
    source.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    source.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    source.SetHullForTest(45, scars: 3);
    var snapshot = source.BuildProgressAirshipSnapshot();
    var target = MakeManager();
    var restored = target.RestoreFromProgressAirship(snapshot);

    return restored
        && target.GetSlotState(ModuleHullManager.SlotA).ModuleType == ModuleType.Scout
        && target.GetSlotState(ModuleHullManager.SlotB).VisibleState == ModuleVisibleState.Damaged
        && target.GetHullState() is { Integrity: 45, Scars: 3, Band: HullBand.Damaged };
});

Run("AC-5: new game starting state matches MVP bootstrap", () =>
{
    var manager = MakeManager();
    return manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == ModuleVisibleState.Empty
        && manager.GetSlotState(ModuleHullManager.SlotB).ModuleType == ModuleType.Cargo
        && manager.GetHullState() is { Integrity: 100, Scars: 0, Band: HullBand.Intact }
        && manager.GetMaxLoad() == 12;
});

Run("AC-6/7: invalid snapshot uses safe defaults and stale module type is skipped with warning", () =>
{
    var invalid = MakeManager();
    var invalidRestored = invalid.RestoreFromProgressAirship(new Dictionary<string, object?>(StringComparer.Ordinal));
    var stale = MakeManager();
    var snapshot = stale.BuildProgressAirshipSnapshot();
    var modules = (Dictionary<string, object?>)snapshot["modules"]!;
    modules[ModuleHullManager.SlotA] = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["module_type"] = 99,
        ["visible_state"] = (int)ModuleVisibleState.Installed,
        ["actual_state"] = (int)ModuleActualState.Installed,
    };
    var staleRestored = stale.RestoreFromProgressAirship(snapshot);

    return !invalidRestored
        && invalid.GetSlotState(ModuleHullManager.SlotB).ModuleType == ModuleType.Cargo
        && staleRestored
        && stale.GetSlotState(ModuleHullManager.SlotA).ModuleType == ModuleType.Empty
        && stale.Warnings.Any(warning => warning.Contains("stale module type", StringComparison.Ordinal));
});

Run("AC-8: snapshot package validates and persistence registration roundtrips package", () =>
{
    var manager = MakeManager();
    var package = manager.BuildSnapshotPackage();
    var validation = package.ValidateContract();
    var restored = MakeManager();

    return validation.Valid && restored.TryRestoreFromSnapshotPackage(package);
});

return Finish("Story 007");
