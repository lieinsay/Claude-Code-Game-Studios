using System.Reflection;
using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #7 Story 007: Signal Contract & HUD Integration ===");
var failed = 0;
var total = 0;

Run("AC-1..3: departure signal emits after state mutation with typed payload", AcDepartureSignalOrder);
Run("AC-4: Hub signal events avoid Dictionary/Node/Object style payloads", AcSignalPayloadTypes);
Run("AC-5..9: station and panel-close integration emits bounded hub events", AcStationAndPanelEvents);
Run("AC-10..14: module and trace changes emit typed primitive contracts", AcModuleAndTraceEvents);

if (failed > 0)
{
	Console.Error.WriteLine($"SignalContract failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"SignalContract passed: {total}/{total} checks passed.");
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

static bool AcDepartureSignalOrder()
{
	var hub = new HubManager();
	var stateAtEmit = HubDockingState.Landed;
	var modeAtEmit = string.Empty;
	hub.DepartureInitiated += (mode, _, _) =>
	{
		stateAtEmit = hub.DockingState;
		modeAtEmit = mode;
	};
	hub.BeginDeparture(HubDepartureMode.Direct);
	hub.AdvanceTime(2.0);
	return stateAtEmit == HubDockingState.InTransit && modeAtEmit == "direct";
}

static bool AcSignalPayloadTypes()
{
	var forbidden = new[] { typeof(Dictionary<,>), typeof(object) };
	return typeof(HubManager)
		.GetEvents(BindingFlags.Instance | BindingFlags.Public)
		.Where(evt => evt.EventHandlerType is not null)
		.SelectMany(evt => evt.EventHandlerType!.GenericTypeArguments)
		.All(arg => !forbidden.Any(f => arg == f || (arg.IsGenericType && arg.GetGenericTypeDefinition() == f)));
}

static bool AcStationAndPanelEvents()
{
	var hub = new HubManager();
	var activated = false;
	var released = false;
	hub.StationActivated += (id, type) => activated = id == HubIds.StorageShelf && type == "open";
	hub.StationReleased += id => released = id == HubIds.StorageShelf;
	hub.GetStation(HubIds.StorageShelf).HandleUse("player");
	hub.GetStation(HubIds.StorageShelf).Release();
	hub.BeginDeparture(HubDepartureMode.Direct);
	return activated && released && hub.CloseHubPanelsRequested;
}

static bool AcModuleAndTraceEvents()
{
	var module = false;
	var trace = false;
	var routes = 0;
	var hub = new HubManager(queries: new HubDomainQueries { KnownRouteCount = () => routes });
	hub.ModuleSlotChanged += (slot, oldState, newState) =>
		module = slot == HubIds.CargoModule
			&& oldState == (int)HubModuleSlotState.Empty
			&& newState == (int)HubModuleSlotState.Installed;
	hub.GetStation(HubIds.IntelDesk).HandleUse("player");
	hub.GetStation(HubIds.IntelDesk).Release();
	hub.TraceAnchorChanged += (id, oldTier, newTier) =>
		trace = id == HubIds.TraceChartNotes && oldTier == 0 && newTier == 1;
	hub.SyncModuleSlotState(HubIds.CargoModule, HubModuleSlotState.Installed);
	routes = 1;
	hub.RefreshTraceAnchors();
	return module && trace;
}
