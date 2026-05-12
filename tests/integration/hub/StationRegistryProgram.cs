using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 002: Station Registration & Interaction Routing ===");
var failed = 0;
var total = 0;

Run("AC-1/2/3: exactly 10 Interactable stations register with stable IDs and types", AcRegistration);
Run("AC-4..8: station ready/busy/disabled state machine handles pending disable", AcStationStateMachine);
Run("AC-9/10: Use routes through InteractionRegistry and Hub delegates without domain ownership", AcUseRouting);
Run("AC-11/12: display hints include live summaries", AcDisplayHints);
Run("AC-13: departure_locked blocks Use via Epic #4 Use Gate as target_disabled", AcDepartureLockBlocksUseGate);

if (failed > 0)
{
	Console.Error.WriteLine($"StationRegistry failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"StationRegistry passed: {total}/{total} checks passed.");
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

static bool AcRegistration()
{
	var hub = new HubManager();
	var expectedTypes = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		[HubIds.IntelDesk] = "read",
		[HubIds.PartnerPost] = "talk",
		[HubIds.ModuleSlotA] = "use",
		[HubIds.ModuleSlotB] = "use",
		[HubIds.StorageShelf] = "open",
		[HubIds.CargoBay] = "open",
		[HubIds.Door] = "use",
		[HubIds.Helm] = "use",
		[HubIds.RestPoint] = "rest",
		[HubIds.RepairPoint] = "repair",
	};

	return hub.InteractionRegistry.CandidateCount == 10
		&& HubIds.StationIds.All(id => hub.InteractionRegistry.GetInteractable(id) is Interactable)
		&& hub.Stations.All(station => station.InteractionId.StartsWith("hub.interactable.", StringComparison.Ordinal)
			&& station.AnchorRadius > 0
			&& station.Priority is >= 0 and <= 1
			&& station.GetDisplayHint().Length > 0
			&& expectedTypes[station.InteractionId] == station.InteractionType);
}

static bool AcStationStateMachine()
{
	var hub = new HubManager(queries: new HubDomainQueries { PartnerInCrew = () => true });
	var station = hub.GetStation(HubIds.PartnerPost);
	var accepted = station.HandleUse("player") == UseResult.Accepted && station.State == HubStationState.Busy;
	station.Release();
	var released = station.State == HubStationState.Ready;
	station.Disable();
	var disabled = !station.IsEnabled && station.State == HubStationState.Disabled;
	station.Enable();
	var enabled = station.State == HubStationState.Ready;
	station.HandleUse("player");
	station.Disable();
	station.Release();
	return accepted && released && disabled && enabled && station.State == HubStationState.Disabled;
}

static bool AcUseRouting()
{
	var hub = new HubManager(queries: new HubDomainQueries { KnownRouteCount = () => 2 });
	hub.InteractionRegistry.SetInputGate(MovementInputGateState.InputOpen);
	hub.InteractionRegistry.SetFocus(HubIds.IntelDesk);
	var activated = string.Empty;
	hub.StationActivated += (id, type) => activated = $"{id}:{type}";

	var result = hub.InteractionRegistry.TryUse("player", new WorldVector2(0, 1), 0);

	return result.Allowed
		&& result.DomainResult == UseResult.Accepted
		&& activated == $"{HubIds.IntelDesk}:read"
		&& hub.GetStation(HubIds.IntelDesk).State == HubStationState.Busy;
}

static bool AcDisplayHints()
{
	var hub = new HubManager(queries: new HubDomainQueries
	{
		StorageUsedVolume = () => 920,
		StorageTotalCapacity = () => 1000,
	});
	return hub.GetStation(HubIds.StorageShelf).GetDisplayHint() == "仓库 · 920/1000"
		&& hub.GetStation(HubIds.ModuleSlotB).GetDisplayHint() == "模块接口 · 空槽";
}

static bool AcDepartureLockBlocksUseGate()
{
	var hub = new HubManager();
	hub.InteractionRegistry.SetInputGate(MovementInputGateState.InputOpen);
	hub.InteractionRegistry.SetFocus(HubIds.Door);
	hub.BeginDeparture(HubDepartureMode.Direct);

	var result = hub.InteractionRegistry.TryUse("player", new WorldVector2(-1, 1), 0);
	return !result.Allowed && result.BlockReason == "target_disabled";
}
