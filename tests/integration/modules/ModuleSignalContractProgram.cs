using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 006: Module Signal Contract ===");

Run("AC-1/2/4: post-voyage signal order is actual, slot, efficiency after mutation", () =>
{
    var manager = MakeManager();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Installed);
    var events = new List<string>();
    double efficiencyDuringSignal = -1;
    manager.ActualStateChanged += (_, _, _) => events.Add("actual");
    manager.SlotStateChanged += (_, _, _) => events.Add("slot");
    manager.ModuleEfficiencyChanged += (slot, _, _) =>
    {
        events.Add("efficiency");
        efficiencyDuringSignal = manager.GetModuleEfficiency(slot);
    };
    manager.DepartureReadinessChanged += (_, _) => events.Add("departure");

    manager.CompleteVoyage(new Dictionary<string, bool>(StringComparer.Ordinal) { [ModuleHullManager.SlotA] = true });

    return events.Count >= 3
        && events[0] == "actual"
        && events[1] == "slot"
        && events.IndexOf("efficiency") > events.IndexOf("slot")
        && Math.Abs(efficiencyDuringSignal - 0.95d) < 0.001d;
});

Run("AC-3: hull integrity signal precedes hull band signal", () =>
{
    var manager = MakeManager();
    var events = new List<string>();
    manager.HullIntegrityChanged += (_, _) => events.Add("integrity");
    manager.HullBandChanged += (_, _) => events.Add("band");

    manager.ApplyHullDamage(30);

    return EventLog(events) == "integrity>band";
});

Run("AC-5: mutation attempts during signal handlers return ERR_BUSY", () =>
{
    var manager = MakeManager();
    ModuleHullResult result = ModuleHullResult.Success;
    manager.SlotStateChanged += (_, _, _) => result = manager.UninstallModule(ModuleHullManager.SlotB).Result;

    manager.UninstallModule(ModuleHullManager.SlotB);

    return result == ModuleHullResult.ErrBusy;
});

Run("AC-6/7: departure readiness is deduplicated and emitted only on changed readiness", () =>
{
    var (registry, resources) = MakeResources();
    RegisterCargo(registry, "cargo.signal_heavy", massClass: "heavy");
    resources.UpdateCargoBayEffectiveVolume(1000);
    resources.AddCargo(ResourcePool.Loaded, "cargo.signal_heavy", ModuleHullManager.BasicSupplyId, 1);
    resources.AddCargo(ResourcePool.Loaded, "cargo.signal_heavy", ModuleHullManager.BasicSupplyId, 1);
    var manager = new ModuleHullManager(resources);
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Scout, ModuleActualState.Installed, ModuleVisibleState.Unchecked);
    var count = 0;
    manager.DepartureReadinessChanged += (_, _) => count++;
    manager.InspectModule(ModuleHullManager.SlotA);
    var afterNoChange = count;
    manager.UninstallModule(ModuleHullManager.SlotB);

    return afterNoChange == 0 && count == 1;
});

return Finish("Story 006");
