using System.Text.Json;
using CloudWeaverVoyage.Presentation;

var total = 0;
var failed = 0;

void Check(bool condition, string label)
{
	total++;
	if (condition)
	{
		Console.WriteLine($"PASS {label}");
		return;
	}

	failed++;
	Console.Error.WriteLine($"FAIL {label}");
}

static string FindProjectRoot()
{
	var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
	while (directory is not null)
	{
		if (File.Exists(Path.Combine(directory.FullName, "CloudWeaverVoyage.csproj")))
		{
			return directory.FullName;
		}

		directory = directory.Parent;
	}

	throw new InvalidOperationException("Could not locate project root from current directory.");
}

static string RequiredString(JsonElement element, string propertyName)
{
	return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
		? property.GetString() ?? string.Empty
		: string.Empty;
}

static int RequiredInt(JsonElement element, string propertyName)
{
	return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
		? value
		: 0;
}

static double RequiredDouble(JsonElement element, string propertyName)
{
	return element.TryGetProperty(propertyName, out var property) && property.TryGetDouble(out var value)
		? value
		: 0;
}

static JsonElement RequiredArray(JsonElement element, string propertyName)
{
	return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array
		? property
		: default;
}

var contentPath = Path.Combine(FindProjectRoot(), "src", "presentation", "playable_slice_authored_content.json");
Check(File.Exists(contentPath), "authored playable content file exists");
using var contentDocument = JsonDocument.Parse(File.ReadAllText(contentPath));
var contentRoot = contentDocument.RootElement;
var routes = RequiredArray(contentRoot, "routes");
var searchPoints = RequiredArray(contentRoot, "search_points");
var routeIds = new HashSet<string>(StringComparer.Ordinal);
var routeDestinations = new HashSet<string>(StringComparer.Ordinal);
var searchPointIds = new HashSet<string>(StringComparer.Ordinal);
var routeCount = routes.ValueKind == JsonValueKind.Array ? routes.GetArrayLength() : 0;
var searchPointCount = searchPoints.ValueKind == JsonValueKind.Array ? searchPoints.GetArrayLength() : 0;
var status = RequiredString(contentRoot, "content_status");

Check(RequiredString(contentRoot, "content_version").StartsWith("polish-", StringComparison.Ordinal), "authored content version is explicit");
Check(status == "polish_authored", "authored content status is an allowed Polish value");
Check(RequiredString(contentRoot, "origin_id").StartsWith("location.", StringComparison.Ordinal), "authored content origin uses location id");
Check(RequiredInt(contentRoot, "cargo_capacity") > 0, "authored content cargo capacity is positive");
Check(RequiredDouble(contentRoot, "voyage_fast_forward_seconds") > 0, "authored content voyage fast-forward is positive");
Check(routeCount >= 2, "authored content has multiple route rows");
Check(searchPointCount >= 3, "authored content has multiple search point rows");

if (routes.ValueKind == JsonValueKind.Array)
{
	foreach (var route in routes.EnumerateArray())
	{
		var routeId = RequiredString(route, "route_id");
		var routeDestination = RequiredString(route, "destination_id");
		var chartTags = RequiredArray(route, "chart_hazard_tags");
		var navigationTags = RequiredArray(route, "navigation_hazard_tags");

		Check(routeId.StartsWith("route.", StringComparison.Ordinal), $"route id '{routeId}' uses route namespace");
		Check(routeIds.Add(routeId), $"route id '{routeId}' is unique");
		Check(!string.IsNullOrWhiteSpace(RequiredString(route, "display_name")), $"route '{routeId}' has display name");
		Check(!string.IsNullOrWhiteSpace(RequiredString(route, "description")), $"route '{routeId}' has authored description");
		Check(RequiredString(route, "origin_id") == RequiredString(contentRoot, "origin_id"), $"route '{routeId}' starts at authored origin");
		Check(routeDestination.StartsWith("location.", StringComparison.Ordinal), $"route '{routeId}' destination uses location id");
		Check(routeDestinations.Add(routeDestination), $"route destination '{routeDestination}' is unique");
		Check(!string.IsNullOrWhiteSpace(RequiredString(route, "distance_band")), $"route '{routeId}' has distance band");
		Check(chartTags.ValueKind == JsonValueKind.Array && chartTags.GetArrayLength() > 0, $"route '{routeId}' has chart hazard tags");
		Check(navigationTags.ValueKind == JsonValueKind.Array && navigationTags.GetArrayLength() > 0, $"route '{routeId}' has navigation hazard tags");
	}
}

if (searchPoints.ValueKind == JsonValueKind.Array)
{
	foreach (var searchPoint in searchPoints.EnumerateArray())
	{
		var pointId = RequiredString(searchPoint, "point_id");
		var quantityMin = RequiredInt(searchPoint, "quantity_min");
		var quantityMax = RequiredInt(searchPoint, "quantity_max");
		var threatId = RequiredString(searchPoint, "threat_id");
		var hasThreat = !string.IsNullOrWhiteSpace(threatId);

		Check(pointId.StartsWith("sp.", StringComparison.Ordinal), $"search point id '{pointId}' uses search namespace");
		Check(searchPointIds.Add(pointId), $"search point id '{pointId}' is unique");
		Check(!string.IsNullOrWhiteSpace(RequiredString(searchPoint, "display_name")), $"search point '{pointId}' has display name");
		Check(!string.IsNullOrWhiteSpace(RequiredString(searchPoint, "description")), $"search point '{pointId}' has authored description");
		Check(!string.IsNullOrWhiteSpace(RequiredString(searchPoint, "zone")), $"search point '{pointId}' has zone");
		Check(RequiredString(searchPoint, "reward_resource_id").StartsWith("resource.", StringComparison.Ordinal), $"search point '{pointId}' reward uses resource id");
		Check(quantityMin > 0 && quantityMax >= quantityMin, $"search point '{pointId}' quantity range is valid");
		Check(!hasThreat || RequiredDouble(searchPoint, "threat_trigger_radius") > 0, $"search point '{pointId}' threat radius is valid when present");
		Check(!hasThreat || RequiredInt(searchPoint, "threat_damage") > 0, $"search point '{pointId}' threat damage is valid when present");
		Check(!hasThreat || RequiredDouble(searchPoint, "threat_position") >= 0, $"search point '{pointId}' threat position is valid when present");
	}
}

var adapter = new PlayableSliceDomainAdapter();
adapter.OpenChart();
var opened = adapter.Snapshot;
Check(opened.ChartState == "Browsing", "adapter opens ChartManager into Browsing");
Check(opened.ContentVersion == "polish-003-authored-route-search-v1", "adapter loads authored playable content version");
Check(opened.ContentStatus == "polish_authored", "adapter reports authored playable content status");
Check(opened.VisibleRouteCount >= 2, "adapter exposes seeded visible routes");

Check(adapter.SelectRoute("route.mist"), "adapter selects mist route through ChartManager");
var selected = adapter.Snapshot;
Check(selected.ChartState == "RouteSelected", "ChartManager state is RouteSelected");
Check(selected.SelectedRouteId == "route.mist", "selected route comes from ChartManager");
Check(selected.SelectedRouteName == "雾海短程", "selected route display name is mapped for Godot UI");

Check(adapter.ConfirmDeparture(), "adapter confirms departure through ChartManager and HubManager");
var departed = adapter.Snapshot;
Check(departed.CommittedRouteId == "route.mist", "ChartManager committed route is recorded");
Check(departed.HubLastRoute == "route.mist", "HubManager records chart departure route");
Check(departed.HubDockingState == "InTransit", "HubManager enters InTransit");
Check(departed.NavigationState == "Arrived", "NavigationManager completes the playable route");
Check(departed.EncounterDestinationId == "location.mist-short", "NavigationManager produces EncounterContext destination");
Check(departed.EncounterResult == "arrived", "NavigationManager produces arrived EncounterContext");
Check(departed.ExplorationPhase == "Exploring", "ExplorationManager consumes EncounterContext into Exploring");
Check(departed.ExplorationPointId == "location.mist-short", "ExplorationManager owns the active exploration point");

adapter.AdvanceExploration();
var searched = adapter.Snapshot;
Check(searched.ExplorationStep == 1, "adapter advances hardened exploration contract to step 1");
Check(searched.LastSearchPointId == "sp.playable.1", "ExplorationManager records the first runtime search point");
Check(searched.LastSearchPointName == "雾灯残骸", "adapter exposes authored search point display name");
Check(searched.BasicSupplyInStorage == 9, "ResourcesManager consumes basic supply on first search");
Check(searched.RewardCarried == 1, "ResourcesManager carries first search reward");
Check(searched.CargoUsed == 80, "adapter exposes cargo pressure for step 1");
Check(searched.ThreatText == "低威胁", "adapter exposes threat text for step 1");

adapter.AdvanceExploration();
var damaged = adapter.Snapshot;
Check(damaged.ExplorationStep == 2, "adapter advances hardened exploration contract to step 2");
Check(damaged.LastSearchPointId == "sp.playable.2", "ExplorationManager records the second runtime search point");
Check(damaged.BasicSupplyInStorage == 8, "ResourcesManager consumes second basic supply");
Check(damaged.RewardCarried == 2, "ResourcesManager carries second search reward");
Check(damaged.HullIntegrity == 94, "ModuleHullManager applies hull pressure for step 2");
Check(damaged.ExplorationSubstate == "Threatened", "ExplorationManager owns the runtime threat substate");

var save = adapter.SaveSceneState(new PlayableSliceSceneState("exploration", "route.mist", 2, 592, 594, "saved from test"));
var saved = adapter.Snapshot;
Check(save.Success, "Persistence saves playable slice progress");
Check(saved.PersistenceGeneration == 1, "Persistence records generation after save");

adapter.ReturnToHub();
var returned = adapter.Snapshot;
Check(returned.HubDockingState == "Landed", "HubManager returns to Landed after arrival");
Check(returned.RewardCarried == 0, "ResourcesManager clears carried rewards on Hub return");
Check(returned.RewardInStorage == 2, "ResourcesManager extracts carried rewards to storage on Hub return");

var load = adapter.LoadSceneState();
var loaded = adapter.Snapshot;
Check(load.Result.Success, "Persistence loads playable slice progress");
Check(load.State.Screen == "exploration", "Persistence restores playable slice screen");
Check(loaded.ExplorationStep == 2, "Persistence restores playable slice exploration step");
Check(loaded.ExplorationPhase == "Exploring", "Persistence restores ExplorationManager active session");
Check(loaded.LastSearchPointId == "sp.playable.2", "Persistence restores last ExplorationManager search point");
Check(loaded.RewardCarried == 2, "Persistence restores ResourcesManager carried rewards");
Check(loaded.RewardInStorage == 0, "Persistence restores ResourcesManager storage before Hub return");
Check(loaded.HullIntegrity == 94, "Persistence restores ModuleHullManager hull damage");

Console.WriteLine($"RESULT {total - failed}/{total} passing");
return failed == 0 ? 0 : 1;
