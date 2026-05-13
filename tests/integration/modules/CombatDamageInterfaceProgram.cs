using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 008: Scout Acquisition & Combat Damage Interfaces ===");

Run("AC-1 through AC-6: scout starts unavailable, unlock allows install, occupied slots reject", () =>
{
    var manager = MakeManager();
    var startUnavailable = !manager.IsScoutModuleAvailable
        && !manager.CanInstallModuleType(ModuleType.Scout)
        && manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == ModuleVisibleState.Empty
        && manager.GetSlotState(ModuleHullManager.SlotB).ModuleType == ModuleType.Cargo;
    var unavailableInstall = manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);
    manager.UnlockScoutModule();
    var install = manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);
    var occupied = manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);

    return startUnavailable
        && unavailableInstall.Result == ModuleHullResult.ErrModuleNotAvailable
        && install.Success
        && occupied.Result == ModuleHullResult.ErrSlotOccupied
        && manager.GetMaxLoad() == 20;
});

Run("AC-7 through AC-9: apply_hull_damage handles damage, crossings, and non-positive no-op", () =>
{
    var manager = MakeManager();
    manager.ApplyHullDamage(30);
    var damaged = manager.GetHullState();
    manager.SetHullForTest(30);
    manager.ApplyHullDamage(35);
    var destroyed = manager.GetHullState();
    manager.ApplyHullDamage(0);
    manager.ApplyHullDamage(-5);
    var afterNoOp = manager.GetHullState();

    return damaged is { Integrity: 70, Band: HullBand.Damaged, Scars: 2 }
        && destroyed is { Integrity: 0, Band: HullBand.Destroyed, Scars: 3 }
        && afterNoOp == destroyed;
});

Run("AC-10 through AC-12/15: apply_module_damage damages installed slot once and stores damage type", () =>
{
    var manager = MakeManager();
    manager.UnlockScoutModule();
    manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);
    manager.ApplyModuleDamage(ModuleHullManager.SlotA, "guard_impact");
    var damaged = manager.GetSlotState(ModuleHullManager.SlotA);
    manager.ApplyModuleDamage(ModuleHullManager.SlotA, "second_hit");
    var second = manager.GetSlotState(ModuleHullManager.SlotA);
    manager.ApplyModuleDamage("invalid", "guard_impact");
    manager.ApplyModuleDamage(ModuleHullManager.SlotB, "cargo_hit");

    return damaged.ActualState == ModuleActualState.Damaged
        && damaged.VisibleState == ModuleVisibleState.Damaged
        && Math.Abs(damaged.Efficiency - 0.6d) < 0.001d
        && damaged.DamageType == "guard_impact"
        && second.DamageType == "guard_impact"
        && manager.GetSlotState(ModuleHullManager.SlotB).DamageType == "cargo_hit";
});

Run("AC-13/14: damage interfaces immediately refresh departure readiness", () =>
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.damage_heavy", massClass: "heavy");
    resources.UpdateCargoBayEffectiveVolume(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.damage_heavy", ModuleHullManager.BasicSupplyId, 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.damage_heavy", ModuleHullManager.BasicSupplyId, 1);
    var manager = new ModuleHullManager(resources);
    manager.UnlockScoutModule();
    manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);
    var before = manager.CanDepart();
    manager.ApplyHullDamage(100);
    var hullDestroyed = manager.CanDepart();
    manager.SetHullForTest(100);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty);
    manager.ApplyModuleDamage(ModuleHullManager.SlotB, "cargo_hit");
    var overloaded = manager.CanDepart();

    return before.CanDepart
        && hullDestroyed.Reasons.Contains("hull_destroyed")
        && overloaded.Reasons.Contains("overloaded");
});

return Finish("Story 008");
