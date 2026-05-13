using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 001: Module Slot State Machine & Dual-Field Model ===");

Run("AC-1: exactly two open slots exist", () =>
{
    var manager = MakeManager();
    return manager.GetSlotIds().SequenceEqual([ModuleHullManager.SlotA, ModuleHullManager.SlotB])
        && manager.IsSlotInteractable(ModuleHullManager.SlotA)
        && manager.IsSlotInteractable(ModuleHullManager.SlotB);
});

Run("AC-3/4: install and uninstall cargo consumes and refunds materials", () =>
{
    var (_, resources) = MakeResources();
    var manager = new ModuleHullManager(resources);
    var basicBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId);
    var repairBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId);

    var install = manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Cargo);
    var uninstall = manager.UninstallModule(ModuleHullManager.SlotA);

    return install.Success
        && uninstall.Success
        && manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == ModuleVisibleState.Empty
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId) == basicBefore
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId) == repairBefore;
});

Run("AC-5/6: damaged and unchecked uninstall grant no refund with distinct messages", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var damaged = manager.UninstallModule(ModuleHullManager.SlotA);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var uncheckedResult = manager.UninstallModule(ModuleHullManager.SlotA);

    return damaged.Success
        && damaged.Refund.Count == 0
        && damaged.Message == "module_damaged_no_refund"
        && uncheckedResult.Success
        && uncheckedResult.Refund.Count == 0
        && uncheckedResult.Message == "module_unchecked_no_refund";
});

Run("AC-7/8: occupied install and empty uninstall reject atomically", () =>
{
    var manager = MakeManager();
    var occupied = manager.InstallModule(ModuleHullManager.SlotB, ModuleType.Cargo);
    var empty = manager.UninstallModule(ModuleHullManager.SlotA);
    return occupied.Result == ModuleHullResult.ErrSlotOccupied
        && empty.Result == ModuleHullResult.ErrSlotEmpty
        && manager.GetSlotState(ModuleHullManager.SlotB).VisibleState == ModuleVisibleState.Installed
        && manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == ModuleVisibleState.Empty;
});

Run("AC-10: efficiency table matches scout/cargo visible states", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var scoutInstalled = manager.GetModuleEfficiency(ModuleHullManager.SlotA);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var scoutDamaged = manager.GetModuleEfficiency(ModuleHullManager.SlotA);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var scoutUnchecked = manager.GetModuleEfficiency(ModuleHullManager.SlotA);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var cargoDamaged = manager.GetModuleEfficiency(ModuleHullManager.SlotA);

    return scoutInstalled == 1.0d
        && scoutDamaged == 0.6d
        && scoutUnchecked == 0.95d
        && cargoDamaged == 0.5d;
});

Run("AC-11/12: voyage completion moves known-good modules to unchecked but known-damaged stays damaged", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    manager.SetSlotForTest(ModuleHullManager.SlotB, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);

    manager.CompleteVoyage(new Dictionary<string, bool>(StringComparer.Ordinal)
    {
        [ModuleHullManager.SlotA] = true,
        [ModuleHullManager.SlotB] = true,
    });

    return manager.GetSlotState(ModuleHullManager.SlotA).ActualState == ModuleActualState.Damaged
        && manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == ModuleVisibleState.Unchecked
        && manager.GetSlotState(ModuleHullManager.SlotB).VisibleState == ModuleVisibleState.Damaged;
});

Run("AC-13 through AC-17: inspect is free; direct repair costs and fixes unchecked/damaged", () =>
{
    var (_, resources) = MakeResources();
    var manager = new ModuleHullManager(resources);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var repairBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId);
    var inspect = manager.InspectModule(ModuleHullManager.SlotA);
    var afterInspect = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Damaged, ModuleVisibleState.Unchecked);
    var repair = manager.RepairModule(ModuleHullManager.SlotA);

    return inspect.Success
        && afterInspect == repairBefore
        && manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == ModuleVisibleState.Installed
        && repair.Success
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId) == repairBefore - 2
        && manager.GetSlotState(ModuleHullManager.SlotA).ActualState == ModuleActualState.Installed;
});

return Finish("Story 001");
