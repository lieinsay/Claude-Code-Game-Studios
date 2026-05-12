using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 008: Scene Persistence & Transition Lifecycle ===");
var failed = 0;
var total = 0;

Run("AC-1/2: snapshot schema stores only independent variables", AcSnapshotSchema);
Run("AC-3/4/14/15: resident scene transition helpers enforce cache and budget", AcSceneLifecycle);
Run("AC-5..8: save boundary degrades transient states", AcSaveBoundaries);
Run("AC-9..13/16/17: restore handles landed, in_transit, transient and stale IDs", AcRestore);
Run("AC-18: desktop suspend flush budget is <=20ms", AcSuspendBudget);
Run("Integration: progress.airship SnapshotPackage validates under ADR-0003", AcSnapshotPackage);

if (failed > 0)
{
	Console.Error.WriteLine($"PersistenceTransition failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"PersistenceTransition passed: {total}/{total} checks passed.");
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

static bool AcSnapshotSchema()
{
	var hub = new HubManager();
	hub.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Installed);
	var snapshot = hub.BuildProgressAirshipSnapshot();
	var modules = (IReadOnlyDictionary<string, object?>)snapshot["module_slot_state"]!;
	return snapshot.Keys.Order(StringComparer.Ordinal).SequenceEqual(new[]
		{
			"departure_snapshot",
			"docking_state",
			"last_departure_mode",
			"last_departure_route",
			"module_slot_state",
			"spawn_reason",
			"trace_anchors",
		}, StringComparer.Ordinal)
		&& !snapshot.ContainsKey("room_state")
		&& !snapshot.ContainsKey("station_state")
		&& modules.ContainsKey(HubIds.CargoModule);
}

static bool AcSceneLifecycle()
{
	return HubManager.CanKeepHubSceneResident(hubInstanceCached: true, targetSceneLoaded: true)
		&& !HubManager.CanKeepHubSceneResident(hubInstanceCached: false, targetSceneLoaded: true)
		&& HubManager.IsReturnTransitionWithinBudget(TimeSpan.FromMilliseconds(499))
		&& !HubManager.IsReturnTransitionWithinBudget(TimeSpan.FromMilliseconds(501));
}

static bool AcSaveBoundaries()
{
	var landed = new HubManager();
	var landedSnapshot = landed.BuildProgressAirshipSnapshot();
	var locked = new HubManager();
	locked.BeginDeparture(HubDepartureMode.Direct);
	var lockedSnapshot = locked.BuildProgressAirshipSnapshot();
	var transit = new HubManager();
	transit.BeginDeparture(HubDepartureMode.Direct);
	transit.AdvanceTime(2.0);
	var transitSnapshot = transit.BuildProgressAirshipSnapshot();
	transit.TriggerArrival();
	var arrivalSnapshot = transit.BuildProgressAirshipSnapshot();
	return (int)landedSnapshot["docking_state"]! == (int)HubDockingState.Landed
		&& (int)lockedSnapshot["docking_state"]! == (int)HubDockingState.Landed
		&& (int)transitSnapshot["docking_state"]! == (int)HubDockingState.InTransit
		&& (int)arrivalSnapshot["docking_state"]! == (int)HubDockingState.InTransit;
}

static bool AcRestore()
{
	var source = new HubManager();
	source.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Installed);
	var snapshot = source.BuildProgressAirshipSnapshot();
	var landed = new HubManager();
	landed.RestoreFromProgressAirship(snapshot);

	var inTransitSnapshot = new Dictionary<string, object?>(snapshot, StringComparer.Ordinal)
	{
		["docking_state"] = (int)HubDockingState.InTransit,
	};
	var arrival = new HubManager();
	arrival.RestoreFromProgressAirship(inTransitSnapshot);

	var transientSnapshot = new Dictionary<string, object?>(snapshot, StringComparer.Ordinal)
	{
		["docking_state"] = (int)HubDockingState.DepartureLocked,
	};
	var degraded = new HubManager();
	degraded.RestoreFromProgressAirship(transientSnapshot);

	var modules = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		[HubIds.CargoModule] = (int)HubModuleSlotState.Installed,
		["stale_module"] = (int)HubModuleSlotState.Installed,
	};
	var staleSnapshot = new Dictionary<string, object?>(snapshot, StringComparer.Ordinal)
	{
		["module_slot_state"] = modules,
	};
	var stale = new HubManager();
	stale.RestoreFromProgressAirship(staleSnapshot);

	return landed.DockingState == HubDockingState.Landed
		&& landed.RoomExists(HubIds.CargoHold)
		&& arrival.DockingState == HubDockingState.Arrival
		&& degraded.DockingState == HubDockingState.Landed
		&& degraded.Warnings.Any(w => w.Contains("degraded", StringComparison.Ordinal))
		&& !stale.ModuleSlotState.ContainsKey("stale_module")
		&& stale.Warnings.Any(w => w.Contains("stale module slot", StringComparison.Ordinal));
}

static bool AcSuspendBudget()
{
	return HubManager.IsSuspendFlushWithinBudget(TimeSpan.FromMilliseconds(20))
		&& !HubManager.IsSuspendFlushWithinBudget(TimeSpan.FromMilliseconds(21));
}

static bool AcSnapshotPackage()
{
	var hub = new HubManager();
	var package = hub.BuildSnapshotPackage();
	return package.IsValid() && package.ValidateContract().Valid;
}
