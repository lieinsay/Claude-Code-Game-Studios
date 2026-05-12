using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 001: Hub Scene Foundation & Docking State Machine ===");
var failed = 0;
var total = 0;

Run("AC-1/2: layout exposes two layers, four rooms, central ladder and walkable bounds", AcLayout);
Run("AC-3/11/13: first load starts landed at helm and keeps resident-scene contract explicit", AcInitialState);
Run("AC-5/6/8/15: departure lock roots movement, timer enters transit, arrival restores movement", AcRoundTrip);
Run("AC-9: chart rejection returns to landed with reason and movement restored", AcRejectedDeparture);
Run("AC-10: watchdog timeout forces landed and records error", AcWatchdog);

if (failed > 0)
{
	Console.Error.WriteLine($"DockingStateMachine failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"DockingStateMachine passed: {total}/{total} checks passed.");
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

static bool AcLayout()
{
	var hub = new HubManager();
	var rooms = hub.Layout.Rooms.Select(room => room.RoomId).ToHashSet(StringComparer.Ordinal);
	return hub.Layout.LayerCount == 2
		&& hub.Layout.HasCentralLadder
		&& hub.Layout.HasWalkableBounds
		&& rooms.SetEquals([HubIds.Cockpit, HubIds.LivingQuarters, HubIds.EngineeringBay, HubIds.CargoHold]);
}

static bool AcInitialState()
{
	var hub = new HubManager();
	return hub.DockingState == HubDockingState.Landed
		&& hub.CurrentSpawnPosition == HubIds.HelmSpawn
		&& HubManager.CanKeepHubSceneResident(hubInstanceCached: true, targetSceneLoaded: true);
}

static bool AcRoundTrip()
{
	var hub = new HubManager();
	var states = new List<HubDockingState>();
	hub.DockingStateChanged += (_, next) => states.Add(next);

	var locked = hub.BeginDeparture(HubDepartureMode.Direct);
	var rootedStep = hub.Movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1, 0);
	hub.AdvanceTime(2.0);
	var arrival = hub.TriggerArrival();
	var arrivalRooted = hub.Movement.IsRooted;
	var landed = hub.CompleteArrivalAnimation();

	return locked
		&& rootedStep.State == MovementState.Rooted
		&& hub.DockingState == HubDockingState.Landed
		&& states.SequenceEqual([HubDockingState.DepartureLocked, HubDockingState.InTransit, HubDockingState.Arrival, HubDockingState.Landed])
		&& arrival
		&& arrivalRooted
		&& landed
		&& !hub.Movement.IsRooted
		&& hub.CurrentSpawnPosition == HubIds.DoorSpawn;
}

static bool AcRejectedDeparture()
{
	var hub = new HubManager(queries: new HubDomainQueries
	{
		ChartDepartureRequest = _ => new HubDepartureRequestResult(false, "NO_VALID_ROUTE"),
	});

	hub.BeginDeparture(HubDepartureMode.Chart);
	hub.AdvanceTime(2.0);

	return hub.DockingState == HubDockingState.Landed
		&& hub.LastRejectionReason == "NO_VALID_ROUTE"
		&& !hub.Movement.IsRooted;
}

static bool AcWatchdog()
{
	var hub = new HubManager(departureLockDurationSeconds: 2.0);
	hub.AutoCompleteDepartureLock = false;
	hub.BeginDeparture(HubDepartureMode.Chart);
	hub.AdvanceTime(6.1);
	return hub.DockingState == HubDockingState.Landed
		&& hub.Errors.Contains("departure_lock_watchdog_forced_landed");
}
