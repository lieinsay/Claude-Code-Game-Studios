using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 004: Departure Modes & Confirmation Gate ===");
var failed = 0;
var total = 0;

Run("AC-1..6: chart and direct departure modes lock then enter transit", AcModes);
Run("AC-7..10: confirmation data lists warnings but keeps confirm enabled", AcConfirmation);
Run("AC-11..13: lock duration validates config and watchdog restores landed", AcDuration);
Run("AC-14/15: departure_locked blocks repeated Use and roots movement", AcRaceProtection);
Run("AC-16/17: direct mode context marks unknown route without route knowledge", AcDirectContext);

if (failed > 0)
{
	Console.Error.WriteLine($"DepartureModes failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"DepartureModes passed: {total}/{total} checks passed.");
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

static bool AcModes()
{
	var chart = new HubManager();
	chart.BeginDeparture(HubDepartureMode.Chart, "route.glass-harbor");
	chart.AdvanceTime(2.0);
	var direct = new HubManager();
	direct.BeginDeparture(HubDepartureMode.Direct);
	direct.AdvanceTime(2.0);
	return chart.DockingState == HubDockingState.InTransit
		&& chart.LastDepartureMode == "chart"
		&& direct.DockingState == HubDockingState.InTransit
		&& direct.LastDepartureMode == "direct";
}

static bool AcConfirmation()
{
	var hub = new HubManager(queries: new HubDomainQueries
	{
		RouteRiskSummary = () => "低风险",
		CargoUsedVolume = () => 10,
		CargoTotalCapacity = () => 100,
	});

	var empty = hub.BuildDepartureConfirmation(HubDepartureMode.Chart);
	hub.GetStation(HubIds.IntelDesk).HandleUse("player");
	hub.GetStation(HubIds.IntelDesk).Release();
	var partial = hub.BuildDepartureConfirmation(HubDepartureMode.Chart);

	return empty.ConfirmEnabled
		&& empty.Checklist.All(item => !item.Visited && item.Warning.Length > 0)
		&& empty.RouteRisk == "未知"
		&& partial.Checklist.Single(item => item.StationId == HubIds.IntelDesk).Visited
		&& !partial.Checklist.Single(item => item.StationId == HubIds.PartnerPost).Visited
		&& partial.RouteRisk == "低风险";
}

static bool AcDuration()
{
	var invalid = new HubManager(departureLockDurationSeconds: double.NaN);
	var tooLow = new HubManager(departureLockDurationSeconds: 0.5);
	var tooHigh = new HubManager(departureLockDurationSeconds: 6.0);
	var watchdog = new HubManager(departureLockDurationSeconds: 2.0);
	watchdog.AutoCompleteDepartureLock = false;
	watchdog.BeginDeparture(HubDepartureMode.Chart);
	watchdog.AdvanceTime(6.1);
	return invalid.DepartureLockDurationSeconds == 2.0
		&& tooLow.DepartureLockDurationSeconds == 2.0
		&& tooHigh.DepartureLockDurationSeconds == 2.0
		&& watchdog.DockingState == HubDockingState.Landed
		&& watchdog.Errors.Contains("departure_lock_watchdog_forced_landed");
}

static bool AcRaceProtection()
{
	var hub = new HubManager();
	hub.InteractionRegistry.SetInputGate(MovementInputGateState.InputOpen);
	hub.InteractionRegistry.SetFocus(HubIds.Helm);
	hub.BeginDeparture(HubDepartureMode.Direct);
	var use = hub.InteractionRegistry.TryUse("player", HubIds.HelmSpawn);
	var move = hub.Movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1, 0);
	return !use.Allowed && use.BlockReason == "target_disabled" && move.State == MovementState.Rooted;
}

static bool AcDirectContext()
{
	var hub = new HubManager();
	hub.BeginDeparture(HubDepartureMode.Direct);
	return hub.LastDepartureContext.DepartureMode == "direct"
		&& !hub.LastDepartureContext.KnownRoute
		&& hub.LastDepartureContext.EncounterRateBonus == 0.10;
}
