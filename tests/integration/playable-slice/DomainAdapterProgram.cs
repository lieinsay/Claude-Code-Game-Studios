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

static JsonElement OptionalMigrationArray(JsonElement element, string propertyName)
{
	if (!element.TryGetProperty("id_migrations", out var migrations) || migrations.ValueKind != JsonValueKind.Object)
	{
		return default;
	}

	return RequiredArray(migrations, propertyName);
}

var projectRoot = FindProjectRoot();
var contentPath = Path.Combine(projectRoot, "src", "presentation", "playable_slice_authored_content.json");
Check(File.Exists(contentPath), "authored playable content file exists");
using var contentDocument = JsonDocument.Parse(File.ReadAllText(contentPath));
var contentRoot = contentDocument.RootElement;
var routes = RequiredArray(contentRoot, "routes");
var searchPoints = RequiredArray(contentRoot, "search_points");
var sceneUnitPrototypes = RequiredArray(contentRoot, "scene_unit_prototypes");
var sceneUnitInstances = RequiredArray(contentRoot, "scene_unit_instances");
var routeMigrations = OptionalMigrationArray(contentRoot, "route_ids");
var searchPointMigrations = OptionalMigrationArray(contentRoot, "search_point_ids");
var routeIds = new HashSet<string>(StringComparer.Ordinal);
var routeDestinations = new HashSet<string>(StringComparer.Ordinal);
var searchPointIds = new HashSet<string>(StringComparer.Ordinal);
var sceneUnitPrototypeIds = new HashSet<string>(StringComparer.Ordinal);
var sceneUnitPrototypeAllowedSceneIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
var sceneUnitInstanceIds = new HashSet<string>(StringComparer.Ordinal);
var ochreIslandUnitIds = new HashSet<string>(StringComparer.Ordinal);
var routeMigrationSources = new HashSet<string>(StringComparer.Ordinal);
var searchPointMigrationSources = new HashSet<string>(StringComparer.Ordinal);
var unitSpecContentByPath = new Dictionary<string, string>(StringComparer.Ordinal);
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
Check(sceneUnitPrototypes.ValueKind == JsonValueKind.Array && sceneUnitPrototypes.GetArrayLength() >= 6, "authored content keeps only the approved Ochre scene-unit prototypes");
Check(sceneUnitInstances.ValueKind == JsonValueKind.Array && sceneUnitInstances.GetArrayLength() >= 6, "authored content keeps only approved Ochre placed scene-unit instances");

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

if (sceneUnitPrototypes.ValueKind == JsonValueKind.Array)
{
	foreach (var prototype in sceneUnitPrototypes.EnumerateArray())
	{
		var prototypeId = RequiredString(prototype, "prototype_id");
		var classification = RequiredString(prototype, "prototype_classification");
		var allowedSceneIds = RequiredArray(prototype, "allowed_scene_ids");

		Check(prototypeId.StartsWith("scene_unit.prototype.", StringComparison.Ordinal), $"scene-unit prototype id '{prototypeId}' uses prototype namespace");
		Check(sceneUnitPrototypeIds.Add(prototypeId), $"scene-unit prototype id '{prototypeId}' is unique");
		Check(classification is "dynamic_entity" or "fixed_scene_object", $"scene-unit prototype '{prototypeId}' has allowed classification");
		Check(!string.IsNullOrWhiteSpace(RequiredString(prototype, "unit_type")), $"scene-unit prototype '{prototypeId}' has unit type");
		Check(!string.IsNullOrWhiteSpace(RequiredString(prototype, "collision")), $"scene-unit prototype '{prototypeId}' has collision semantics");
		Check(!string.IsNullOrWhiteSpace(RequiredString(prototype, "occlusion_layer")), $"scene-unit prototype '{prototypeId}' has occlusion layer");
		Check(!string.IsNullOrWhiteSpace(RequiredString(prototype, "scale_rule")), $"scene-unit prototype '{prototypeId}' has scale rule");
		Check(RequiredString(prototype, "source_layer") == "world_playable_scene", $"scene-unit prototype '{prototypeId}' is world/playable evidence");
		Check(!prototype.TryGetProperty("ui_evidence_allowed", out var uiEvidence) || uiEvidence.ValueKind == JsonValueKind.False, $"scene-unit prototype '{prototypeId}' rejects UI evidence");
		var sourceGdd = RequiredString(prototype, "source_gdd");
		var isShipInteriorChartTablePrototype = prototypeId == "scene_unit.prototype.chart_table";
		Check(sourceGdd == "design/gdd/scene-physics-unit-system.md"
			|| (isShipInteriorChartTablePrototype && sourceGdd == "design/gdd/ui-hud-chart-interface.md"),
			$"scene-unit prototype '{prototypeId}' traces to #20 GDD or the approved ChartTable source GDD");
		var unitSpecPath = RequiredString(prototype, "unit_spec");
		var legacyReplacementStatus = RequiredString(prototype, "legacy_replacement_status");
		var expectedUnitSpecRoot = classification == "dynamic_entity"
			? "production/unit-specs/dynamic-entities/"
			: "production/unit-specs/fixed-scene-objects/";
		if (!string.IsNullOrWhiteSpace(unitSpecPath))
		{
			var unitSpecFullPath = Path.Combine(projectRoot, unitSpecPath.Replace('/', Path.DirectorySeparatorChar));
			Check(unitSpecPath.StartsWith(expectedUnitSpecRoot, StringComparison.Ordinal), $"scene-unit prototype '{prototypeId}' points to the expected unit spec directory");
			Check(File.Exists(unitSpecFullPath), $"scene-unit prototype '{prototypeId}' unit spec file exists");
			if (File.Exists(unitSpecFullPath))
			{
				if (!unitSpecContentByPath.TryGetValue(unitSpecFullPath, out var unitSpecContent))
				{
					unitSpecContent = File.ReadAllText(unitSpecFullPath);
					unitSpecContentByPath[unitSpecFullPath] = unitSpecContent;
				}

				Check(unitSpecContent.Contains(prototypeId, StringComparison.Ordinal), $"scene-unit prototype '{prototypeId}' is listed in its unit spec");
			}
		}
		else
		{
			Check(legacyReplacementStatus == "pending_spec_replacement", $"scene-unit prototype '{prototypeId}' without a unit spec is marked for legacy replacement");
		}
		var allowedScenes = allowedSceneIds.ValueKind == JsonValueKind.Array
			? allowedSceneIds.EnumerateArray()
				.Select(scene => scene.GetString() ?? string.Empty)
				.Where(scene => !string.IsNullOrWhiteSpace(scene))
				.ToHashSet(StringComparer.Ordinal)
			: new HashSet<string>(StringComparer.Ordinal);
		sceneUnitPrototypeAllowedSceneIds[prototypeId] = allowedScenes;
		if (isShipInteriorChartTablePrototype)
		{
			Check(allowedScenes.SetEquals(new[] { "hub_ship_interior", "ship_interior_layered" }), $"scene-unit prototype '{prototypeId}' is scoped to the approved ship interior asset slice");
		}
		else
		{
			Check(allowedScenes.SetEquals(new[] { "ochre_island_scene" }), $"scene-unit prototype '{prototypeId}' is scoped to the approved Ochre asset slice");
		}
	}
}

if (sceneUnitInstances.ValueKind == JsonValueKind.Array)
{
	foreach (var instance in sceneUnitInstances.EnumerateArray())
	{
		var instanceId = RequiredString(instance, "instance_id");
		var prototypeId = RequiredString(instance, "prototype_id");
		var sceneId = RequiredString(instance, "scene_id");
		var unitId = RequiredString(instance, "unit_id");

		Check(instanceId.StartsWith("scene_unit.instance.", StringComparison.Ordinal), $"scene-unit instance id '{instanceId}' uses instance namespace");
		Check(sceneUnitInstanceIds.Add(instanceId), $"scene-unit instance id '{instanceId}' is unique");
		Check(sceneUnitPrototypeIds.Contains(prototypeId), $"scene-unit instance '{instanceId}' references known prototype");
		var isOchreIsland = sceneId == "ochre_island_scene";
		var isShipInterior = sceneId == "hub_ship_interior";
		var expectedFloorId = sceneId switch
		{
			"ochre_island_scene" => "ochre_island_ground_01",
			"hub_ship_interior" => "ship_deck_01",
			_ => string.Empty,
		};
		var expectedSceneSpec = sceneId switch
		{
			"ochre_island_scene" => "production/scene-specs/ochre-island-scene.md",
			"hub_ship_interior" => "production/scene-specs/ship-interior-layered-scene.md",
			_ => string.Empty,
		};

		Check(isOchreIsland || isShipInterior, $"scene-unit instance '{instanceId}' belongs to an approved asset slice");
		Check(sceneUnitPrototypeAllowedSceneIds.TryGetValue(prototypeId, out var allowedScenes) && allowedScenes.Contains(sceneId), $"scene-unit instance '{instanceId}' uses prototype allowed in its scene");
		Check(!string.IsNullOrWhiteSpace(unitId), $"scene-unit instance '{instanceId}' has runtime unit id");
		if (isOchreIsland)
		{
			Check(ochreIslandUnitIds.Add(unitId), $"scene-unit instance unit id '{unitId}' is unique in ochre island");
		}
		Check(!string.IsNullOrWhiteSpace(RequiredString(instance, "godot_node_path")), $"scene-unit instance '{instanceId}' has Godot placement reference");
		Check(RequiredString(instance, "floor_id") == expectedFloorId, $"scene-unit instance '{instanceId}' has expected floor id");
		Check(RequiredString(instance, "scene_spec") == expectedSceneSpec, $"scene-unit instance '{instanceId}' traces to scene spec");
		Check(!string.IsNullOrWhiteSpace(RequiredString(instance, "layer")), $"scene-unit instance '{instanceId}' has placement layer");
	}
}

foreach (var expectedOchreUnit in new[]
{
	"player_marker",
	"ochre_island_mass",
	"ochre_island_path",
	"banded_iron_ore",
	"ochre_return_anchor",
	"ochre_cloudsea_boundary",
})
{
	Check(ochreIslandUnitIds.Contains(expectedOchreUnit), $"ochre-island placed units cover runtime catalog unit '{expectedOchreUnit}'");
}

var sceneUnitAuthoring = SceneUnitAuthoringFixture.Load(contentPath);
var ochreUnitDiagnostics = sceneUnitAuthoring.ValidateScene("ochre_island_scene");
Check(ochreUnitDiagnostics.Count == 0, $"scene-unit authoring validates for ochre island ({string.Join("; ", ochreUnitDiagnostics)})");
var shipInteriorDiagnostics = sceneUnitAuthoring.ValidateScene("hub_ship_interior");
Check(shipInteriorDiagnostics.Count == 0, $"scene-unit authoring validates for ship interior ({string.Join("; ", shipInteriorDiagnostics)})");

Check(routeMigrations.ValueKind == JsonValueKind.Array, "route id migration map is explicit");
if (routeMigrations.ValueKind == JsonValueKind.Array)
{
	foreach (var migration in routeMigrations.EnumerateArray())
	{
		var source = RequiredString(migration, "from");
		var target = RequiredString(migration, "to");
		Check(source.StartsWith("route.", StringComparison.Ordinal), $"route migration source '{source}' uses route namespace");
		Check(routeMigrationSources.Add(source), $"route migration source '{source}' is unique");
		Check(!routeIds.Contains(source), $"route migration source '{source}' is not an active route id");
		Check(routeIds.Contains(target), $"route migration target '{target}' resolves to active route");
		Check(!string.IsNullOrWhiteSpace(RequiredString(migration, "reason")), $"route migration '{source}' records a reason");
	}
}

Check(searchPointMigrations.ValueKind == JsonValueKind.Array, "search point id migration map is explicit");
if (searchPointMigrations.ValueKind == JsonValueKind.Array)
{
	foreach (var migration in searchPointMigrations.EnumerateArray())
	{
		var source = RequiredString(migration, "from");
		var target = RequiredString(migration, "to");
		Check(source.StartsWith("sp.", StringComparison.Ordinal), $"search migration source '{source}' uses search namespace");
		Check(searchPointMigrationSources.Add(source), $"search migration source '{source}' is unique");
		Check(!searchPointIds.Contains(source), $"search migration source '{source}' is not an active search point id");
		Check(searchPointIds.Contains(target), $"search migration target '{target}' resolves to active search point");
		Check(!string.IsNullOrWhiteSpace(RequiredString(migration, "reason")), $"search migration '{source}' records a reason");
	}
}

var adapter = new PlayableSliceDomainAdapter();
adapter.OpenChart();
var opened = adapter.Snapshot;
Check(opened.ChartState == "Browsing", "adapter opens ChartManager into Browsing");
Check(opened.ContentVersion == "polish-asset-reset-ship-interior-v1", "adapter loads the asset-reset authored content version");
Check(opened.ContentStatus == "polish_authored", "adapter reports authored playable content status");
Check(opened.VisibleRouteCount >= 2, "adapter exposes seeded visible routes");
Check(adapter.GetRouteDisplayName("route.playable-mist") == "雾海短程", "adapter resolves legacy route id display name through migration map");
Check(adapter.SelectRoute("route.playable-mist"), "adapter accepts legacy route id through migration map");
var migratedSelected = adapter.Snapshot;
Check(migratedSelected.SelectedRouteId == "route.mist", "legacy route selection resolves to current route id");

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
var exportedProgressJson = adapter.ExportProgressJson();
Check(exportedProgressJson.Contains("\"progress.playable_slice\"", StringComparison.Ordinal), "adapter exports canonical progress JSON for durable storage");
var corruptedProgressJson = exportedProgressJson.Replace("\"hull_integrity\":94", "\"hull_integrity\":95", StringComparison.Ordinal);
var corruptedAdapter = new PlayableSliceDomainAdapter();
Check(!corruptedAdapter.TryImportProgressJson(corruptedProgressJson, out var corruptedReason)
	&& corruptedReason == "checksum_mismatch", "adapter rejects corrupted durable progress checksum");
var restartedAdapter = new PlayableSliceDomainAdapter();
Check(restartedAdapter.TryImportProgressJson(exportedProgressJson, out var importReason), $"adapter imports canonical progress JSON after restart ({importReason})");
var restartedLoad = restartedAdapter.LoadSceneState();
var restartedLoaded = restartedAdapter.Snapshot;
Check(restartedLoad.Result.Success, "restarted adapter loads imported durable progress");
Check(restartedLoad.State.Screen == "exploration", "restarted adapter restores saved screen from durable progress");
Check(restartedLoaded.ExplorationStep == 2, "restarted adapter restores exploration step from durable progress");
Check(restartedLoaded.LastSearchPointId == "sp.playable.2", "restarted adapter restores search point from durable progress");
Check(restartedLoaded.RewardCarried == 2, "restarted adapter restores carried rewards from durable progress");
Check(restartedLoaded.HullIntegrity == 94, "restarted adapter restores hull damage from durable progress");

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

var legacySave = adapter.SaveSceneState(new PlayableSliceSceneState("exploration", "route.playable-mist", loaded.ExplorationStep, 592, 594, "saved with legacy route id"));
var legacyLoad = adapter.LoadSceneState();
Check(legacySave.Success, "Persistence saves scene state containing legacy route id");
Check(legacyLoad.Result.Success, "Persistence reloads scene state containing legacy route id");
Check(legacyLoad.State.Route == "route.mist", "Persistence migrates legacy route id to current route id on restore");

Console.WriteLine($"RESULT {total - failed}/{total} passing");
return failed == 0 ? 0 : 1;
