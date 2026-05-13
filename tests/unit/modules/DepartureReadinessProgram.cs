using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 004: Furnace Capacity & Departure Readiness ===");

Run("AC-1 through AC-4: furnace ratings and all main M_max combinations", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var doubleCargo = manager.GetMaxLoad();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var scoutCargo = manager.GetMaxLoad();
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var doubleScout = manager.GetMaxLoad();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var cargoDamagedScoutGood = manager.GetMaxLoad();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var scoutDamagedCargoGood = manager.GetMaxLoad();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var doubleDamaged = manager.GetMaxLoad();

    return doubleCargo == 24
        && scoutCargo == 20
        && doubleScout == 16
        && cargoDamagedScoutGood == 14
        && scoutDamagedCargoGood == 16
        && doubleDamaged == 10;
});

Run("AC-5/6: critical and unchecked multipliers use floor", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    manager.SetHullForTest(20);
    var critical = manager.GetMaxLoad();
    manager.SetHullForTest(100);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var uncheckedLoad = manager.GetMaxLoad();
    return critical == 19 && uncheckedLoad == 22;
});

Run("AC-7 through AC-14: can_depart returns all blocking reasons and success", () =>
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.departure_heavy", massClass: "heavy");
    resources.UpdateCargoBayEffectiveVolume(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.departure_heavy", ModuleHullManager.BasicSupplyId, 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.departure_heavy", ModuleHullManager.BasicSupplyId, 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.departure_heavy", ModuleHullManager.BasicSupplyId, 1);
    var manager = new ModuleHullManager(resources);
    var success = manager.CanDepart();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty);
    var noFurnace = manager.CanDepart();
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var overloaded = manager.CanDepart();
    manager.SetHullForTest(0);
    var combined = manager.CanDepart();

    return success.CanDepart
        && noFurnace.Reasons.Contains("no_furnace")
        && overloaded.Reasons.Contains("overloaded")
        && combined.Reasons.Contains("overloaded")
        && combined.Reasons.Contains("hull_destroyed");
});

return Finish("Story 004");
