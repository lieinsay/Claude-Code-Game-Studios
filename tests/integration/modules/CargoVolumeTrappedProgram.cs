using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 005: Cargo Bay Effective Volume & Trapped Goods ===");

Run("AC-1 through AC-6: effective volume covers cargo configurations and hull penalties", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var doubleCargo = manager.GetEffectiveCargoVolume();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var scoutCargo = manager.GetEffectiveCargoVolume();
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var doubleScout = manager.GetEffectiveCargoVolume();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var oneDamaged = manager.GetEffectiveCargoVolume();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var uncheckedVolume = manager.GetEffectiveCargoVolume();
    manager.SetHullForTest(20);
    var criticalVolume = manager.GetEffectiveCargoVolume();

    return doubleCargo == 1000
        && scoutCargo == 500
        && doubleScout == 0
        && oneDamaged == 750
        && uncheckedVolume == 950
        && criticalVolume == 760;
});

Run("AC-7/9/10/11/14: volume reduction traps goods without destroying ownership and repair restores access", () =>
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.trap_a", massClass: "heavy");
    RegisterCargo(registry, "cargo.trap_b", massClass: "heavy");
    resources.UpdateCargoBayEffectiveVolume(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.trap_a", ModuleHullManager.BasicSupplyId, 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.trap_b", ModuleHullManager.BasicSupplyId, 1);
    var manager = new ModuleHullManager(resources);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Empty, ModuleActualState.Empty, ModuleVisibleState.Empty);
    manager.ApplyModuleDamage(ModuleHullManager.SlotB, "storm");
    var trappedAfterDamage = resources.GetCargoBayTrappedVolume();
    var ownedAfterDamage = resources.GetQuantity(ResourcePool.Loaded, "cargo.trap_a")
        + resources.GetQuantity(ResourcePool.Loaded, "cargo.trap_b");
    manager.RepairModule(ModuleHullManager.SlotB);
    var trappedAfterRepair = resources.GetCargoBayTrappedVolume();
    manager.UninstallModule(ModuleHullManager.SlotB);
    var trappedAfterRemoval = resources.GetCargoBayTrappedVolume();
    manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Cargo);
    var trappedAfterInstall = resources.GetCargoBayTrappedVolume();

    return trappedAfterDamage == 150
        && ownedAfterDamage == 2
        && trappedAfterRepair == 0
        && trappedAfterRemoval == 400
        && trappedAfterInstall == 0;
});

Run("AC-12/13: resources cargo volume update is called only when V_effective changes", () =>
{
    var (_, resources) = MakeResources();
    var manager = new ModuleHullManager(resources);
    var initialCapacity = resources.GetCargoBayCapacity();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    manager.InspectModule(ModuleHullManager.SlotA);
    var afterScoutInspect = resources.GetCargoBayCapacity();
    manager.ApplyModuleDamage(ModuleHullManager.SlotB, "storm");
    var afterCargoDamage = resources.GetCargoBayCapacity();

    return initialCapacity == 500
        && afterScoutInspect == initialCapacity
        && afterCargoDamage == 250;
});

return Finish("Story 005");
