using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 002: Module Swap Two-Phase Operation ===");

Run("AC-1/8/9: scout to cargo consumes net repair only", () =>
{
    var (_, resources) = MakeResources(basic: 20, repair: 20);
    var manager = new ModuleHullManager(resources);
    manager.UnlockScoutModule();
    manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);
    var basicBeforeSwap = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId);
    var repairBeforeSwap = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId);

    var result = manager.SwapModule(ModuleHullManager.SlotA, ModuleType.Cargo);

    return result.Success
        && manager.GetSlotState(ModuleHullManager.SlotA).ModuleType == ModuleType.Cargo
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId) == basicBeforeSwap
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId) == repairBeforeSwap - 1;
});

Run("AC-2: cargo to scout consumes net basic only", () =>
{
    var (_, resources) = MakeResources(basic: 20, repair: 20);
    var manager = new ModuleHullManager(resources);
    manager.UnlockScoutModule();
    var basicBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId);
    var repairBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId);

    var result = manager.SwapModule(ModuleHullManager.SlotB, ModuleType.Scout);

    return result.Success
        && manager.GetSlotState(ModuleHullManager.SlotB).ModuleType == ModuleType.Scout
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId) == basicBefore - 2
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId) == repairBefore;
});

Run("AC-3: insufficient net cost fails without mutation", () =>
{
    var (_, resources) = MakeResources(basic: 20, repair: 2);
    var manager = new ModuleHullManager(resources);
    manager.UnlockScoutModule();
    manager.InstallModule(ModuleHullManager.SlotA, ModuleType.Scout);
    var before = manager.GetSlotState(ModuleHullManager.SlotA);

    var result = manager.SwapModule(ModuleHullManager.SlotA, ModuleType.Cargo);

    return result.Result == ModuleHullResult.ErrInsufficientResources
        && manager.GetSlotState(ModuleHullManager.SlotA).ModuleType == before.ModuleType
        && manager.GetSlotState(ModuleHullManager.SlotA).VisibleState == before.VisibleState;
});

Run("AC-4: cargo bay must be empty before swapping cargo to scout", () =>
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.swap_block", massClass: "medium");
    resources.UpdateCargoBayEffectiveVolume(500);
    resources.AddCargo(ResourcePool.Loaded, "cargo.swap_block", ModuleHullManager.BasicSupplyId, 1);
    var manager = new ModuleHullManager(resources);
    manager.UnlockScoutModule();

    var result = manager.SwapModule(ModuleHullManager.SlotB, ModuleType.Scout);

    return result.Result == ModuleHullResult.ErrCargoBayNotEmpty
        && manager.GetSlotState(ModuleHullManager.SlotB).ModuleType == ModuleType.Cargo
        && (int)resources.GetCargoBayUsage()["used_volume"]! > 0;
});

Run("AC-5/6: same type and empty slot swaps reject", () =>
{
    var manager = MakeManager();
    var same = manager.SwapModule(ModuleHullManager.SlotB, ModuleType.Cargo);
    var empty = manager.SwapModule(ModuleHullManager.SlotA, ModuleType.Cargo);
    return same.Result == ModuleHullResult.ErrSameModuleType
        && empty.Result == ModuleHullResult.ErrSlotEmpty;
});

Run("AC-7: damaged old module has no refund and pays full new cost", () =>
{
    var (_, resources) = MakeResources(basic: 20, repair: 20);
    var manager = new ModuleHullManager(resources);
    manager.UnlockScoutModule();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Damaged, ModuleVisibleState.Damaged);
    var basicBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId);
    var repairBefore = resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId);

    var result = manager.SwapModule(ModuleHullManager.SlotA, ModuleType.Scout);

    return result.Success
        && manager.GetSlotState(ModuleHullManager.SlotA).ModuleType == ModuleType.Scout
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.BasicSupplyId) == basicBefore - 5
        && resources.GetQuantity(ResourcePool.InStorage, ModuleHullManager.RepairKitId) == repairBefore - 2;
});

return Finish("Story 002");
