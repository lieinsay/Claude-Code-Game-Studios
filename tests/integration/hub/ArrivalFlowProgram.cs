using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 005: Arrival Flow & State Continuity ===");
var failed = 0;
var total = 0;

Run("AC-1..3: in_transit return triggers arrival, roots movement, completes landed", AcArrivalTransition);
Run("AC-4..7: departure snapshot preserves external state summaries for continuity checks", AcContinuitySnapshot);
Run("AC-8/9: return spawn is door and movement restores after landed", AcReturnSpawn);
Run("AC-10/11: load derivation clears busy and disables cargo bay when cargo room absent", AcDeriveStations);
Run("AC-12/13: arrival snapshots degrade to in_transit and corrupt snapshots use defaults", AcEdgeCases);

if (failed > 0)
{
	Console.Error.WriteLine($"ArrivalFlow failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"ArrivalFlow passed: {total}/{total} checks passed.");
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

static bool AcArrivalTransition()
{
	var hub = new HubManager();
	hub.BeginDeparture(HubDepartureMode.Direct);
	hub.AdvanceTime(2.0);
	var arrival = hub.TriggerArrival();
	var rooted = hub.Movement.IsRooted;
	var landed = hub.CompleteArrivalAnimation();
	return arrival && rooted && landed && hub.DockingState == HubDockingState.Landed && !hub.Movement.IsRooted;
}

static bool AcContinuitySnapshot()
{
	var hub = new HubManager(queries: new HubDomainQueries
	{
		StorageUsedVolume = () => 50,
		CargoUsedVolume = () => 300,
		CompletedRepairCount = () => 2,
	});
	hub.SyncModuleSlotState(HubIds.ScoutModule, HubModuleSlotState.Installed);
	hub.BeginDeparture(HubDepartureMode.Direct);
	var modules = (IReadOnlyDictionary<string, object?>)hub.DepartureSnapshot["modules"]!;
	return (int)hub.DepartureSnapshot["storage_used"]! == 50
		&& (int)hub.DepartureSnapshot["cargo_used"]! == 300
		&& (int)hub.DepartureSnapshot["hull_repair_count"]! == 2
		&& (int)modules[HubIds.ScoutModule]! == (int)HubModuleSlotState.Installed;
}

static bool AcReturnSpawn()
{
	var hub = new HubManager();
	hub.BeginDeparture(HubDepartureMode.Direct);
	hub.AdvanceTime(2.0);
	hub.TriggerArrival();
	hub.CompleteArrivalAnimation();
	var move = hub.Movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1, 0);
	return hub.CurrentSpawnPosition == HubIds.DoorSpawn && move.MovementVelocity > 0;
}

static bool AcDeriveStations()
{
	var hub = new HubManager();
	var rest = hub.GetStation(HubIds.RestPoint);
	rest.HandleUse("player");
	hub.DeriveAllStationStates();
	return rest.State == HubStationState.Ready
		&& hub.GetStation(HubIds.CargoBay).State == HubStationState.Disabled;
}

static bool AcEdgeCases()
{
	var hub = new HubManager();
	hub.BeginDeparture(HubDepartureMode.Direct);
	hub.AdvanceTime(2.0);
	hub.TriggerArrival();
	var snapshot = hub.BuildProgressAirshipSnapshot();
	var corrupt = new HubManager();
	var restored = corrupt.RestoreFromProgressAirship(new Dictionary<string, object?>());
	return (int)snapshot["docking_state"]! == (int)HubDockingState.InTransit
		&& !restored
		&& corrupt.DockingState == HubDockingState.Landed
		&& corrupt.CurrentSpawnPosition == HubIds.HelmSpawn
		&& corrupt.Warnings.Any(warning => warning.Contains("safe defaults", StringComparison.Ordinal));
}
