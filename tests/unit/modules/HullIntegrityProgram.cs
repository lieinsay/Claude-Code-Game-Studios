using CloudWeaverVoyage.Core;
using static ModuleHullTestSupport;

Console.WriteLine("=== Epic #8 Story 003: Hull Integrity, Bands & Scars ===");

Run("AC-1 through AC-5: hull bands transition and clamp correctly", () =>
{
    var manager = MakeManager();
    var start = manager.GetHullState();
    manager.ApplyHullDamage(25);
    var damaged = manager.GetHullState();
    manager.SetHullForTest(26);
    manager.ApplyHullDamage(1);
    var critical = manager.GetHullState();
    manager.SetHullForTest(5);
    manager.ApplyHullDamage(15);
    var destroyed = manager.GetHullState();

    return start is { Integrity: 100, Band: HullBand.Intact, Scars: 0 }
        && damaged is { Integrity: 75, Band: HullBand.Damaged }
        && critical is { Integrity: 25, Band: HullBand.Critical }
        && destroyed is { Integrity: 0, Band: HullBand.Destroyed };
});

Run("AC-6/7: band penalties match table and critical multiplies module efficiency", () =>
{
    var manager = MakeManager();
    manager.SetHullForTest(20);
    var penalties = manager.GetHullBandPenalties();
    manager.SetSlotForTest(ModuleHullManager.SlotA, ModuleType.Cargo, ModuleActualState.Installed, ModuleVisibleState.Installed);
    return penalties.SpeedMultiplier == 0.75d
        && penalties.FuelMultiplier == 1.3d
        && penalties.ModuleEfficiencyMultiplier == 0.8d
        && penalties.HighRiskBlocked
        && Math.Abs(manager.GetFinalModuleEfficiency(ModuleHullManager.SlotA) - 0.8d) < 0.001d;
});

Run("AC-8 through AC-11: scars count base events, crossings, and band re-entry", () =>
{
    var manager = MakeManager();
    manager.ApplyHullDamage(10);
    var baseScars = manager.GetHullState().Scars;
    manager.SetHullForTest(80);
    manager.ApplyHullDamage(80);
    var intactToDestroyed = manager.GetHullState().Scars;
    manager.SetHullForTest(30);
    manager.ApplyHullDamage(35);
    var damagedToDestroyed = manager.GetHullState().Scars;
    manager.SetHullForTest(20);
    manager.RepairHull(2);
    var beforeReEntry = manager.GetHullState().Scars;
    manager.ApplyHullDamage(10);
    var reEntryDelta = manager.GetHullState().Scars - beforeReEntry;

    return baseScars == 1
        && intactToDestroyed == 4
        && damagedToDestroyed == 3
        && reEntryDelta == 2;
});

Run("AC-13 through AC-16: hull repair consumes kits, clamps, rejects full hull, and restores destroyed", () =>
{
    var (_, resources) = MakeResources(repair: 10);
    var manager = new ModuleHullManager(resources);
    manager.SetHullForTest(50);
    var repair = manager.RepairHull(2);
    var at60 = manager.GetHullState();
    manager.SetHullForTest(95);
    manager.RepairHull(2);
    var at100 = manager.GetHullState();
    var full = manager.RepairHull(1);
    manager.SetHullForTest(0);
    manager.RepairHull(1);
    var restored = manager.GetHullState();

    return repair.Success
        && at60.Integrity == 60
        && at100.Integrity == 100
        && full.Result == ModuleHullResult.ErrHullAlreadyFull
        && restored is { Integrity: 5, Band: HullBand.Critical };
});

return Finish("Story 003");
