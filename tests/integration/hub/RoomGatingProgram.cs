using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 003: Room Gating & Module Slot Display ===");
var failed = 0;
var total = 0;

Run("AC-1..4/13: room_exists follows base room and cargo_module mapping", AcRoomExists);
Run("AC-5..10: module slot mirror updates hints and cargo room availability", AcSlotMirror);
Run("AC-11/12: cargo bay content protects cargo_module unequip", AcCargoProtection);

if (failed > 0)
{
	Console.Error.WriteLine($"RoomGating failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"RoomGating passed: {total}/{total} checks passed.");
return 0;

void Run(string label, Func<bool> test)
{
	total++;
	try
	{
		if (test())
		{
			Console.WriteLine($"[PASS] {label}");
			return;
		}
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
		failed++;
		return;
	}

	Console.Error.WriteLine($"[FAIL] {label}");
	failed++;
}

static bool AcRoomExists()
{
	var hub = new HubManager();
	var emptyCargo = !hub.RoomExists(HubIds.CargoHold);
	hub.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Installed);
	var installedCargo = hub.RoomExists(HubIds.CargoHold);
	hub.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Damaged);
	var damagedCargo = hub.RoomExists(HubIds.CargoHold);
	return emptyCargo
		&& installedCargo
		&& damagedCargo
		&& hub.RoomExists(HubIds.Cockpit)
		&& hub.RoomExists(HubIds.LivingQuarters)
		&& hub.RoomExists(HubIds.EngineeringBay)
		&& hub.GetRoomRequiredModule(HubIds.CargoHold) == HubIds.CargoModule
		&& hub.GetRoomRequiredModule(HubIds.Cockpit) is null;
}

static bool AcSlotMirror()
{
	var hub = new HubManager();
	var changed = false;
	hub.ModuleSlotChanged += (slot, oldState, newState) =>
		changed |= slot == HubIds.CargoModule && oldState == (int)HubModuleSlotState.Empty && newState == (int)HubModuleSlotState.Unchecked;

	hub.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Unchecked);
	var uncheckedHint = hub.GetStation(HubIds.ModuleSlotB).GetDisplayHint();
	var cargoReady = hub.GetStation(HubIds.CargoBay).State == HubStationState.Ready && hub.RoomExists(HubIds.CargoHold);
	hub.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Empty);

	return changed
		&& uncheckedHint == "模块接口 · 未检查"
		&& cargoReady
		&& !hub.RoomExists(HubIds.CargoHold)
		&& hub.GetStation(HubIds.CargoBay).State == HubStationState.Disabled;
}

static bool AcCargoProtection()
{
	var blockedHub = new HubManager(queries: new HubDomainQueries { CargoUsedVolume = () => 300 });
	var allowedHub = new HubManager(queries: new HubDomainQueries { CargoUsedVolume = () => 0 });
	return !blockedHub.CanUnequipModule(HubIds.CargoModule)
		&& blockedHub.GetUnequipBlockReason(HubIds.CargoModule) == "请先清空货舱"
		&& allowedHub.CanUnequipModule(HubIds.CargoModule);
}
