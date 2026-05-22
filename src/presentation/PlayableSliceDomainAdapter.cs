using CloudWeaverVoyage.Core;
using System.Text.Json;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Headless domain adapter for the recovered playable vertical slice.
/// Godot scenes should wrap this class instead of owning route/departure state.
/// </summary>
public sealed class PlayableSliceDomainAdapter
{
	private const string OriginId = "location.cloudweaver-hub";
	private const string BasicSupplyId = ModuleHullManager.BasicSupplyId;
	private const string RepairKitId = ModuleHullManager.RepairKitId;
	private const string RewardResourceId = "resource.beacon_crystal";
	private const string ContentPath = "src/presentation/playable_slice_authored_content.json";
	private readonly PlayableSliceRuntimeFixture fixture;
	private readonly ChartManager chart;
	private readonly HubManager hub;
	private readonly NavigationManager navigation;
	private readonly ExplorationManager exploration;
	private readonly ResourcesManager resources;
	private readonly ModuleHullManager modules;
	private readonly Persistence persistence;
	private string lastCommittedRoute = string.Empty;
	private string lastCommittedDestination = string.Empty;
	private EncounterContext? activeEncounterContext;
	private string lastSearchPointId = string.Empty;
	private string lastSearchPointName = string.Empty;
	private string lastSearchMessage = "尚未搜索";
	private string lastStatus = "Domain adapter initialized.";
	private string lastSaveStatus = "尚未保存";
	private string lastLoadStatus = "尚未加载";
	private int explorationStep;
	private PlayableSliceSceneState sceneState = new("hub", "", 0, 158.0f, 610.0f, "");

	public PlayableSliceDomainAdapter()
	{
		fixture = PlayableSliceRuntimeFixture.Load(ContentPath);
		resources = BuildResourcesManager();
		modules = new ModuleHullManager(resources);
		InstallStartingModules();
		chart = BuildChartManager();
		hub = BuildHubManager();
		navigation = BuildNavigationManager();
		exploration = BuildExplorationManager();
		persistence = BuildPersistence();
	}

	/// <summary>Raised after ChartManager opens the chart.</summary>
	public event Action? ChartOpened;

	/// <summary>Raised after ChartManager accepts a selected route.</summary>
	public event Action<string>? RouteSelected;

	/// <summary>Raised after ChartManager and HubManager accept departure.</summary>
	public event Action<string>? DepartureConfirmed;

	/// <summary>Raised after ResourcesManager or ModuleHullManager pressure changes in exploration.</summary>
	public event Action<PlayableSliceSnapshot, PlayableSliceSnapshot>? ExplorationPressureChanged;

	/// <summary>Raised after canonical persistence save/load is used.</summary>
	public event Action<string, bool>? SaveLoadUsed;

	/// <summary>Raised after HubManager returns to Landed and summaries have updated.</summary>
	public event Action<PlayableSliceSnapshot, PlayableSliceSnapshot>? ReturnedToHub;

	/// <summary>Connects onboarding observation and progress persistence to this adapter.</summary>
	public void RegisterOnboarding(OnboardingManager onboardingManager)
	{
		ArgumentNullException.ThrowIfNull(onboardingManager);
		onboardingManager.ConnectPlayableSliceEvents(this);
		onboardingManager.RegisterPersistence(persistence);
	}

	public PlayableSliceSnapshot Snapshot => new(
		ChartState: chart.CurrentState.ToString(),
		ContentVersion: fixture.ContentVersion,
		ContentStatus: fixture.ContentStatus,
		SelectedRouteId: chart.SelectedRouteId,
		SelectedRouteName: RouteName(chart.SelectedRouteId),
		VisibleRouteCount: chart.VisibleRoutes.Count,
		CommittedRouteId: lastCommittedRoute,
		CommittedDestinationId: lastCommittedDestination,
		HubDockingState: hub.DockingState.ToString(),
		HubDepartureMode: hub.LastDepartureMode,
		HubLastRoute: hub.LastDepartureRoute,
		NavigationState: navigation.CurrentState.ToString(),
		NavigationProgress: navigation.GetVoyageProgress(),
		EncounterDestinationId: activeEncounterContext?.DestinationId ?? string.Empty,
		EncounterResult: activeEncounterContext?.VoyageResult ?? string.Empty,
		EncounterDamage: activeEncounterContext?.AccumulatedDamage ?? 0,
		ExplorationPhase: exploration.CurrentPhase.ToString(),
		ExplorationSubstate: exploration.CurrentSubstate.ToString(),
		ExplorationPointId: exploration.CurrentPointId,
		ExplorationStep: explorationStep,
		LastSearchPointId: lastSearchPointId,
		LastSearchPointName: lastSearchPointName,
		LastSearchMessage: lastSearchMessage,
		BasicSupplyInStorage: resources.GetQuantity(ResourcePool.InStorage, BasicSupplyId),
		RepairKitsInStorage: resources.GetQuantity(ResourcePool.InStorage, RepairKitId),
		RewardInStorage: resources.GetQuantity(ResourcePool.InStorage, RewardResourceId),
		RewardCarried: resources.GetQuantity(ResourcePool.Carried, RewardResourceId),
		CargoUsed: CargoUsed,
		CargoCapacity: fixture.CargoCapacity,
		StorageText: StorageText,
		HullIntegrity: modules.GetHullState().Integrity,
		ThreatText: ThreatText,
		PersistenceGeneration: persistence.CurrentGeneration,
		LastSaveStatus: lastSaveStatus,
		LastLoadStatus: lastLoadStatus,
		LastStatus: lastStatus);

	public void OpenChart()
	{
		chart.OpenChart();
		lastStatus = $"ChartManager {chart.CurrentState}: visible routes {chart.VisibleRoutes.Count}.";
		ChartOpened?.Invoke();
	}

	public bool SelectRoute(string routeId)
	{
		var selected = chart.SelectRoute(routeId);
		lastStatus = selected
			? $"ChartManager selected {RouteName(routeId)}."
			: $"ChartManager rejected route {routeId}.";
		if (selected)
		{
			RouteSelected?.Invoke(routeId);
		}

		return selected;
	}

	public string GetRouteDisplayName(string routeId) => RouteName(routeId);

	public bool ConfirmDeparture()
	{
		var routeId = chart.SelectedRouteId;
		if (string.IsNullOrWhiteSpace(routeId))
		{
			lastStatus = "ChartManager rejected departure: no selected route.";
			return false;
		}

		var summary = chart.RequestConfirmDeparture(routeId);
		if (summary is null || !chart.ConfirmDeparture())
		{
			lastStatus = $"ChartManager rejected departure: {routeId}.";
			return false;
		}

		var began = hub.BeginDeparture(HubDepartureMode.Chart, routeId);
		var locked = began && hub.CompleteDepartureLock();
		explorationStep = 0;
		activeEncounterContext = null;
		lastSearchPointId = string.Empty;
		lastSearchPointName = string.Empty;
		lastSearchMessage = "尚未搜索";
		lastStatus = locked
			? $"HubManager departed via {RouteName(routeId)}."
			: $"HubManager rejected departure: {hub.LastRejectionReason}.";
		if (locked)
		{
			var destinationId = summary.TryGetValue("destination_id", out var rawDestination)
				? rawDestination?.ToString() ?? lastCommittedDestination
				: lastCommittedDestination;
			var hazardTags = ReadStringList(summary, "hazard_tags");
			StartNavigationAndExploration(routeId, destinationId, hazardTags);
			DepartureConfirmed?.Invoke(routeId);
		}

		return locked;
	}

	public void AdvanceExploration()
	{
		var before = Snapshot;
		var nextStep = Math.Min(explorationStep + 1, 3);
		if (nextStep == explorationStep)
		{
			lastStatus = "Domain exploration fixture is already complete.";
			return;
		}

		if (nextStep is 1 or 2)
		{
			resources.Remove(ResourcePool.InStorage, BasicSupplyId, 1);
			var searchPoint = fixture.SearchPointForStep(nextStep);
			var search = exploration.PerformSearch(searchPoint.PointId, SearchPointState.Unlooted, searchPoint.Zone);
			lastSearchPointId = searchPoint.PointId;
			lastSearchPointName = searchPoint.DisplayName;
			lastSearchMessage = search.IsEmpty
				? string.IsNullOrWhiteSpace(search.Message) ? "搜索无结果" : search.Message
				: $"搜索获得 {string.Join(", ", search.Items.Select(item => $"{item.ResourceId} x{item.Quantity}"))}";
		}
		else if (nextStep == 3)
		{
			var searchPoint = fixture.SearchPointForStep(nextStep);
			var search = exploration.PerformSearch(searchPoint.PointId, SearchPointState.Unlooted, searchPoint.Zone);
			lastSearchPointId = searchPoint.PointId;
			lastSearchPointName = searchPoint.DisplayName;
			lastSearchMessage = search.IsEmpty
				? string.IsNullOrWhiteSpace(search.Message) ? "搜索无结果" : search.Message
				: $"搜索获得 {string.Join(", ", search.Items.Select(item => $"{item.ResourceId} x{item.Quantity}"))}";
		}

		if (nextStep == 2)
		{
			exploration.CheckThreatTrigger(fixture.SearchPointForStep(nextStep).ThreatPosition, "proximity");
		}

		explorationStep = nextStep;
		lastStatus = $"ResourcesManager/ModuleHullManager advanced exploration to {explorationStep}/3.";
		ExplorationPressureChanged?.Invoke(before, Snapshot);
	}

	public void ReturnToHub()
	{
		var before = Snapshot;
		var extraction = resources.ExtractCarriedToStorage();
		hub.TriggerArrival();
		hub.CompleteArrivalAnimation();
		lastStatus = extraction.Success
			? "HubManager arrival completed; ResourcesManager extracted carried rewards to storage."
			: $"HubManager arrival completed; reward extraction failed: {extraction.Result}.";
		ReturnedToHub?.Invoke(before, Snapshot);
	}

	public PersistenceOperationResult SaveSceneState(PlayableSliceSceneState state)
	{
		sceneState = state with { ExplorationStep = explorationStep };
		var result = persistence.RequestSaveProgress();
		lastSaveStatus = result.Success
			? $"canonical progress saved gen {result.Generation}"
			: $"canonical progress save failed: {result.Reason}";
		lastStatus = lastSaveStatus;
		SaveLoadUsed?.Invoke("save", result.Success);
		return result;
	}

	public (PersistenceOperationResult Result, PlayableSliceSceneState State) LoadSceneState()
	{
		var result = persistence.RequestLoadProgress();
		lastLoadStatus = result.Success
			? $"canonical progress loaded gen {result.Generation}"
			: $"canonical progress load failed: {result.Reason}";
		lastStatus = lastLoadStatus;
		SaveLoadUsed?.Invoke("load", result.Success);
		return (result, sceneState);
	}

	private ResourcesManager BuildResourcesManager()
	{
		var registry = new Registry();
		registry.InitializeContent();
		var manager = new ResourcesManager(registry);
		manager.Initialize();
		foreach (var resource in fixture.InitialResources)
		{
			manager.Add(ResourcePool.InStorage, resource.ResourceId, resource.Quantity);
		}
		return manager;
	}

	private void InstallStartingModules()
	{
		foreach (var module in fixture.StartingModules)
		{
			if (Enum.TryParse<ModuleType>(module.ModuleType, ignoreCase: true, out var moduleType))
			{
				if (moduleType == ModuleType.Scout)
				{
					modules.UnlockScoutModule();
				}
				modules.InstallModule(module.SlotId, moduleType);
			}
		}
	}

	private ChartManager BuildChartManager()
	{
		var manager = new ChartManager();
		foreach (var domain in new[] { "routes", "world", "intel", "threats" })
		{
			manager.SetDomainState(domain, DomainState.Complete);
		}

		manager.SetKnowledgeQueryDelegate(_ => 2);
		manager.SetTraversableQueryDelegate(_ => true);
		manager.SetDockedLocationDelegate(() => fixture.OriginId);
		foreach (var route in fixture.Routes)
		{
			manager.RegisterRoute(new RouteStaticData(
				route.RouteId,
				route.OriginId,
				route.DestinationId,
				route.DistanceBand,
				route.ChartHazardTags));
		}
		manager.RouteCommitted += (routeId, destinationId, _) =>
		{
			lastCommittedRoute = routeId;
			lastCommittedDestination = destinationId;
		};
		return manager;
	}

	private HubManager BuildHubManager()
	{
		var manager = new HubManager(queries: new HubDomainQueries
		{
			KnownRouteCount = () => chart.VisibleRoutes.Count,
			RouteKnowledgeUnlocked = () => true,
			RouteRiskSummary = () => ThreatText,
			CargoUsedVolume = () => CargoUsed,
			CargoTotalCapacity = () => fixture.CargoCapacity,
			StorageUsedVolume = () => resources.GetUsedVolume(ResourcePool.InStorage),
			StorageTotalCapacity = () => resources.GetTotalVolume(ResourcePool.InStorage),
			ChartDepartureRequest = _ => new HubDepartureRequestResult(true, string.Empty),
		});
		manager.AutoCompleteDepartureLock = false;
		return manager;
	}

	private NavigationManager BuildNavigationManager()
	{
		var manager = new NavigationManager();
		manager.SetCanDepartDelegate(_ =>
		{
			var readiness = modules.CanDepart();
			return (readiness.CanDepart, readiness.Reasons);
		});
		manager.SetGetRouteDelegate(routeId => routeId switch
		{
			_ when fixture.RouteById.TryGetValue(routeId, out var route) =>
				(true, route.NavigationHazardTags, route.DistanceBand),
			_ => (false, Array.Empty<string>(), string.Empty),
		});
		manager.SetGetKnowledgeStateDelegate(_ => 2);
		manager.SetGetHullIntegrityDelegate(() => modules.GetHullState().Integrity);
		manager.SetGetHullBandDelegate(() => modules.GetHullState().Band);
		manager.SetGetScoutEfficiencyDelegate(() => modules.GetScoutVisibilityEfficiency());
		manager.SetResolveEncounterDelegate(tag => tag switch
		{
			"medium_threat" => new[] { new EncounterEntry(tag, damage: 2, timePenaltyFlat: 0, timePenaltyTemp: 0) },
			"low_threat" => Array.Empty<EncounterEntry>(),
			_ => Array.Empty<EncounterEntry>(),
		});
		manager.SetApplyHullDamageDelegate(modules.ApplyHullDamage);
		manager.VoyageCompleted += context => activeEncounterContext = context;
		return manager;
	}

	private ExplorationManager BuildExplorationManager()
	{
		var manager = new ExplorationManager();
		manager.SetCanAddToPoolDelegate((_, _) => true);
		manager.SetAddLootDelegate((resourceId, quantity) => resources.Add(ResourcePool.Carried, resourceId, quantity));
		manager.SetRandomDelegate(() => 0.10d);
		manager.SetRandomRangeDelegate((_, _) => 1);
		manager.SetGetScoutEfficiencyDelegate(() => modules.GetScoutVisibilityEfficiency());
		manager.SetApplyExplorationHullDamageDelegate(_ => modules.ApplyHullDamage(fixture.ThreatDamage));
		manager.SetLootPools(fixture.SearchPoints.ToDictionary(
			point => point.PointId,
			point => PlayableSearchLoot(point),
			StringComparer.Ordinal));
		foreach (var point in fixture.SearchPoints.Where(point => !string.IsNullOrWhiteSpace(point.ThreatId)))
		{
			manager.RegisterThreatPoint(new ExplorationManager.ThreatPoint(
				point.ThreatId,
				ExplorationManager.ThreatCategory.Environmental,
				point.ThreatTriggerRadius,
				point.ThreatPosition));
		}
		return manager;
	}

	private static Dictionary<string, List<(string, int, int)>> PlayableSearchLoot(PlayableSearchPoint point) =>
		new(StringComparer.Ordinal)
		{
			["poor"] = new List<(string, int, int)> { (point.RewardResourceId, point.QuantityMin, point.QuantityMax) },
			["common"] = new List<(string, int, int)> { (point.RewardResourceId, point.QuantityMin, point.QuantityMax) },
			["uncommon"] = new List<(string, int, int)> { (point.RewardResourceId, point.QuantityMin, point.QuantityMax) },
		};

	private void StartNavigationAndExploration(string routeId, string destinationId, IReadOnlyList<string> hazardTags)
	{
		navigation.OnRouteCommitted(routeId, destinationId, hazardTags);
		navigation.ProcessVoyage(fixture.VoyageFastForwardSeconds);
		activeEncounterContext ??= navigation.BuildEncounterContext();
		if (exploration.EnterExplorationWithContext(activeEncounterContext))
		{
			exploration.SkipArriving();
		}
		lastStatus = $"NavigationManager {navigation.CurrentState}; ExplorationManager {exploration.CurrentPhase}.";
	}

	private Persistence BuildPersistence()
	{
		var manager = new Persistence();
		resources.RegisterPersistence(manager);
		modules.RegisterPersistence(manager);
		manager.RegisterDomainSerializer("progress.airship", hub.BuildSnapshotPackage);
		manager.RegisterDomainDeserializer("progress.airship", package => hub.RestoreFromProgressAirship(package.Payload));
		manager.RegisterDomainSerializer("progress.routes", BuildChartSnapshotPackage);
		manager.RegisterDomainDeserializer("progress.routes", package => chart.RestoreFromSnapshot(package.Payload));
		manager.RegisterDomainSerializer("progress.navigation", BuildNavigationSnapshotPackage);
		manager.RegisterDomainDeserializer("progress.navigation", package => navigation.RestoreFromVoyageSnapshot(package.Payload));
		manager.RegisterDomainSerializer("progress.exploration", BuildExplorationSnapshotPackage);
		manager.RegisterDomainDeserializer("progress.exploration", RestoreExplorationSnapshotPackage);
		manager.RegisterDomainSerializer("progress.playable_slice", BuildPlayableSliceSnapshotPackage);
		manager.RegisterDomainDeserializer("progress.playable_slice", RestorePlayableSliceSnapshotPackage);
		return manager;
	}

	private SnapshotPackage BuildChartSnapshotPackage()
	{
		var routeId = string.IsNullOrWhiteSpace(lastCommittedRoute)
			? chart.SelectedRouteId
			: lastCommittedRoute;
		var payload = chart.BuildSnapshotPayload(routeId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		payload["domain_id"] = "progress.routes";
		var package = new SnapshotPackage
		{
			DomainId = "progress.routes",
			SnapshotSchemaVersion = 1,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["routes"] = "2026-05-09";
		if (!string.IsNullOrWhiteSpace(routeId))
		{
			package.StableIdRefs.Add(routeId);
		}
		foreach (var (key, value) in payload)
		{
			package.Payload[key] = value;
		}
		return package;
	}

	private SnapshotPackage BuildNavigationSnapshotPackage()
	{
		var package = new SnapshotPackage
		{
			DomainId = "progress.navigation",
			SnapshotSchemaVersion = 1,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["navigation"] = "2026-05-09";
		var payload = navigation.CaptureVoyageSnapshot();
		foreach (var (key, value) in payload)
		{
			package.Payload[key] = value;
		}
		if (!string.IsNullOrWhiteSpace(activeEncounterContext?.RouteId))
		{
			package.StableIdRefs.Add(activeEncounterContext.RouteId);
		}
		if (!string.IsNullOrWhiteSpace(activeEncounterContext?.DestinationId))
		{
			package.StableIdRefs.Add(activeEncounterContext.DestinationId);
		}
		return package;
	}

	private SnapshotPackage BuildExplorationSnapshotPackage()
	{
		var package = new SnapshotPackage
		{
			DomainId = "progress.exploration",
			SnapshotSchemaVersion = 1,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["exploration"] = "2026-05-09";
		var payload = exploration.SerializeExploration();
		if (payload is not null)
		{
			foreach (var (key, value) in payload)
			{
				package.Payload[key] = value;
			}
		}
		if (!string.IsNullOrWhiteSpace(activeEncounterContext?.DestinationId))
		{
			package.StableIdRefs.Add(activeEncounterContext.DestinationId);
		}
		return package;
	}

	private void RestoreExplorationSnapshotPackage(SnapshotPackage package)
	{
		var payload = package.Payload.ToDictionary(
			pair => pair.Key,
			pair => pair.Value?.ToString() ?? string.Empty,
			StringComparer.Ordinal);
		if (payload.Count > 0)
		{
			exploration.DeserializeExploration(payload);
		}
	}

	private SnapshotPackage BuildPlayableSliceSnapshotPackage()
	{
		var state = sceneState with { ExplorationStep = explorationStep };
		var package = new SnapshotPackage
		{
			DomainId = "progress.playable_slice",
			SnapshotSchemaVersion = 1,
			DomainState = SnapshotDomainState.Ready,
		};
		package.ContentDomainVersions["playable-slice"] = "2026-05-17";
		if (!string.IsNullOrWhiteSpace(state.Route))
		{
			package.StableIdRefs.Add(state.Route);
		}
		package.Payload["screen"] = state.Screen;
		package.Payload["route"] = state.Route;
		package.Payload["exploration_step"] = state.ExplorationStep;
		package.Payload["player_x"] = state.PlayerX;
		package.Payload["player_y"] = state.PlayerY;
		package.Payload["footer"] = state.Footer;
		package.Payload["reward_carried"] = resources.GetQuantity(ResourcePool.Carried, RewardResourceId);
		package.Payload["last_search_point_id"] = lastSearchPointId;
		package.Payload["last_search_point_name"] = lastSearchPointName;
		package.Payload["last_search_message"] = lastSearchMessage;
		return package;
	}

	private void RestorePlayableSliceSnapshotPackage(SnapshotPackage package)
	{
		sceneState = new PlayableSliceSceneState(
			ReadString(package.Payload, "screen", "hub"),
			ReadString(package.Payload, "route", ""),
			ReadInt(package.Payload, "exploration_step", 0),
			ReadFloat(package.Payload, "player_x", 158.0f),
			ReadFloat(package.Payload, "player_y", 610.0f),
			ReadString(package.Payload, "footer", ""));
		explorationStep = Math.Max(0, sceneState.ExplorationStep);
		var currentCarried = resources.GetQuantity(ResourcePool.Carried, RewardResourceId);
		if (currentCarried > 0)
		{
			resources.Remove(ResourcePool.Carried, RewardResourceId, currentCarried);
		}
		var restoredCarried = Math.Max(0, ReadInt(package.Payload, "reward_carried", 0));
		if (restoredCarried > 0)
		{
			resources.Add(ResourcePool.Carried, RewardResourceId, restoredCarried);
		}
		lastSearchPointId = ReadString(package.Payload, "last_search_point_id", "");
		lastSearchPointName = ReadString(package.Payload, "last_search_point_name", SearchPointName(lastSearchPointId));
		lastSearchMessage = ReadString(package.Payload, "last_search_message", "尚未搜索");
		lastStatus = "Playable slice scene state restored from canonical progress.";
	}

	private string RouteName(string routeId) =>
		fixture.RouteById.TryGetValue(routeId, out var route)
			? route.DisplayName
			: string.IsNullOrWhiteSpace(routeId) ? "未命名航线" : routeId;

	private string SearchPointName(string pointId) =>
		fixture.SearchPoints.FirstOrDefault(point => point.PointId == pointId)?.DisplayName ?? string.Empty;

	private int CargoUsed => TotalRewards * 80 + (explorationStep >= 2 ? 20 : 0);

	private int TotalRewards =>
		resources.GetQuantity(ResourcePool.InStorage, RewardResourceId)
		+ resources.GetQuantity(ResourcePool.Carried, RewardResourceId);

	private string ThreatText => explorationStep switch
	{
		<= 0 => "暂无遭遇",
		1 => "低威胁",
		2 => "中威胁",
		_ => "威胁已解除",
	};

	private string StorageText =>
		$"基础补给 x{resources.GetQuantity(ResourcePool.InStorage, BasicSupplyId)} / "
		+ $"信标水晶 x{TotalRewards} / "
		+ $"修理包 x{resources.GetQuantity(ResourcePool.InStorage, RepairKitId)}";

	private static string ReadString(IReadOnlyDictionary<string, object?> payload, string key, string fallback) =>
		payload.TryGetValue(key, out var value) ? value?.ToString() ?? fallback : fallback;

	private static int ReadInt(IReadOnlyDictionary<string, object?> payload, string key, int fallback)
	{
		if (!payload.TryGetValue(key, out var value) || value is null)
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	private static float ReadFloat(IReadOnlyDictionary<string, object?> payload, string key, float fallback)
	{
		if (!payload.TryGetValue(key, out var value) || value is null)
		{
			return fallback;
		}
		return Convert.ToSingle(value);
	}

	private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, object> payload, string key)
	{
		if (!payload.TryGetValue(key, out var value) || value is null)
		{
			return Array.Empty<string>();
		}
		if (value is IEnumerable<string> strings)
		{
			return strings.ToArray();
		}
		if (value is System.Collections.IEnumerable enumerable && value is not string)
		{
			return enumerable.Cast<object?>()
				.Select(item => item?.ToString() ?? string.Empty)
				.Where(item => !string.IsNullOrWhiteSpace(item))
				.ToArray();
		}
		return new[] { value.ToString() ?? string.Empty };
	}
}

public sealed record PlayableSliceSnapshot(
	string ChartState,
	string ContentVersion,
	string ContentStatus,
	string SelectedRouteId,
	string SelectedRouteName,
	int VisibleRouteCount,
	string CommittedRouteId,
	string CommittedDestinationId,
	string HubDockingState,
	string HubDepartureMode,
	string HubLastRoute,
	string NavigationState,
	double NavigationProgress,
	string EncounterDestinationId,
	string EncounterResult,
	int EncounterDamage,
	string ExplorationPhase,
	string ExplorationSubstate,
	string ExplorationPointId,
	int ExplorationStep,
	string LastSearchPointId,
	string LastSearchPointName,
	string LastSearchMessage,
	int BasicSupplyInStorage,
	int RepairKitsInStorage,
	int RewardInStorage,
	int RewardCarried,
	int CargoUsed,
	int CargoCapacity,
	string StorageText,
	int HullIntegrity,
	string ThreatText,
	int PersistenceGeneration,
	string LastSaveStatus,
	string LastLoadStatus,
	string LastStatus);

public sealed record PlayableSliceSceneState(
	string Screen,
	string Route,
	int ExplorationStep,
	float PlayerX,
	float PlayerY,
	string Footer);

internal sealed class PlayableSliceRuntimeFixture
{
	public string ContentVersion { get; init; } = "fallback-playable-content-v1";
	public string ContentStatus { get; init; } = "fallback_fixture";
	public string OriginId { get; init; } = "location.cloudweaver-hub";
	public int CargoCapacity { get; init; } = 500;
	public double VoyageFastForwardSeconds { get; init; } = 240.0d;
	public IReadOnlyList<PlayableInitialResource> InitialResources { get; init; } =
	[
		new(ModuleHullManager.BasicSupplyId, 13),
		new(ModuleHullManager.RepairKitId, 7),
	];
	public IReadOnlyList<PlayableStartingModule> StartingModules { get; init; } =
	[
		new(ModuleHullManager.SlotA, "Cargo"),
	];
	public IReadOnlyList<PlayableRouteFixture> Routes { get; init; } =
	[
		new(
			"route.mist",
			"雾海短程",
			"Low-threat fallback route for the playable slice.",
			"location.cloudweaver-hub",
			"location.mist-short",
			"short",
			["fog", "low_threat"],
			["safe", "low_threat"]),
		new(
			"route.market",
			"旧集市航道",
			"Medium fallback route reserved for later market content.",
			"location.cloudweaver-hub",
			"location.old-market",
			"medium",
			["market", "medium_threat"],
			["safe", "medium_threat"]),
	];
	public IReadOnlyList<PlayableSearchPoint> SearchPoints { get; init; } =
	[
		new("sp.playable.1", "雾灯残骸", "Fallback first playable search point.", "A_core", "resource.beacon_crystal", 1, 1, "", 0.0d, 0.0d, 0),
		new("sp.playable.2", "剪云裂隙", "Fallback threat playable search point.", "A_core", "resource.beacon_crystal", 1, 1, "threat.playable-cloud-shear", 2.0d, 0.25d, 6),
		new("sp.playable.3", "返航浮标箱", "Fallback extraction playable search point.", "A_core", "resource.beacon_crystal", 1, 1, "", 0.0d, 0.0d, 0),
	];

	public IReadOnlyDictionary<string, PlayableRouteFixture> RouteById =>
		Routes.ToDictionary(route => route.RouteId, StringComparer.Ordinal);

	public int ThreatDamage => SearchPoints
		.Where(point => !string.IsNullOrWhiteSpace(point.ThreatId))
		.Select(point => point.ThreatDamage)
		.DefaultIfEmpty(0)
		.Max();

	public PlayableSearchPoint SearchPointForStep(int step)
	{
		var index = Math.Clamp(step, 1, SearchPoints.Count) - 1;
		return SearchPoints[index];
	}

	public static PlayableSliceRuntimeFixture Load(string path)
	{
		var fullPath = Path.GetFullPath(path);
		if (!File.Exists(fullPath))
		{
			return new PlayableSliceRuntimeFixture();
		}

		using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
		var root = document.RootElement;
		return new PlayableSliceRuntimeFixture
		{
			OriginId = ReadString(root, "origin_id", "location.cloudweaver-hub"),
			ContentVersion = ReadString(root, "content_version", "fallback-playable-content-v1"),
			ContentStatus = ReadString(root, "content_status", "fallback_fixture"),
			CargoCapacity = ReadInt(root, "cargo_capacity", 500),
			VoyageFastForwardSeconds = ReadDouble(root, "voyage_fast_forward_seconds", 240.0d),
			InitialResources = ReadInitialResources(root),
			StartingModules = ReadStartingModules(root),
			Routes = ReadRoutes(root),
			SearchPoints = ReadSearchPoints(root),
		};
	}

	private static IReadOnlyList<PlayableInitialResource> ReadInitialResources(JsonElement root)
	{
		if (!root.TryGetProperty("initial_resources", out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return new PlayableSliceRuntimeFixture().InitialResources;
		}
		var resources = array.EnumerateArray()
			.Select(item => new PlayableInitialResource(
				ReadString(item, "resource_id", ""),
				ReadInt(item, "quantity", 0)))
			.Where(item => !string.IsNullOrWhiteSpace(item.ResourceId) && item.Quantity > 0)
			.ToArray();
		return resources.Length > 0
			? resources
			: new PlayableSliceRuntimeFixture().InitialResources;
	}

	private static IReadOnlyList<PlayableStartingModule> ReadStartingModules(JsonElement root)
	{
		if (!root.TryGetProperty("starting_modules", out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return new PlayableSliceRuntimeFixture().StartingModules;
		}
		var modules = array.EnumerateArray()
			.Select(item => new PlayableStartingModule(
				ReadString(item, "slot_id", ""),
				ReadString(item, "module_type", "")))
			.Where(item => !string.IsNullOrWhiteSpace(item.SlotId) && !string.IsNullOrWhiteSpace(item.ModuleType))
			.ToArray();
		return modules.Length > 0
			? modules
			: new PlayableSliceRuntimeFixture().StartingModules;
	}

	private static IReadOnlyList<PlayableRouteFixture> ReadRoutes(JsonElement root)
	{
		if (!root.TryGetProperty("routes", out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return new PlayableSliceRuntimeFixture().Routes;
		}
		var routes = array.EnumerateArray()
			.Select(item => new PlayableRouteFixture(
				ReadString(item, "route_id", ""),
				ReadString(item, "display_name", ""),
				ReadString(item, "description", ""),
				ReadString(item, "origin_id", "location.cloudweaver-hub"),
				ReadString(item, "destination_id", ""),
				ReadString(item, "distance_band", "medium"),
				ReadStringArray(item, "chart_hazard_tags"),
				ReadStringArray(item, "navigation_hazard_tags")))
			.Where(route => !string.IsNullOrWhiteSpace(route.RouteId) && !string.IsNullOrWhiteSpace(route.DestinationId))
			.ToArray();
		return routes.Length > 0
			? routes
			: new PlayableSliceRuntimeFixture().Routes;
	}

	private static IReadOnlyList<PlayableSearchPoint> ReadSearchPoints(JsonElement root)
	{
		if (!root.TryGetProperty("search_points", out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return new PlayableSliceRuntimeFixture().SearchPoints;
		}
		var searchPoints = array.EnumerateArray()
			.Select(item => new PlayableSearchPoint(
				ReadString(item, "point_id", ""),
				ReadString(item, "display_name", ""),
				ReadString(item, "description", ""),
				ReadString(item, "zone", "A_core"),
				ReadString(item, "reward_resource_id", "resource.beacon_crystal"),
				ReadInt(item, "quantity_min", 1),
				ReadInt(item, "quantity_max", 1),
				ReadString(item, "threat_id", ""),
				ReadDouble(item, "threat_position", 0.0d),
				ReadDouble(item, "threat_trigger_radius", 0.0d),
				ReadInt(item, "threat_damage", 0)))
			.Where(point => !string.IsNullOrWhiteSpace(point.PointId))
			.ToArray();
		return searchPoints.Length > 0
			? searchPoints
			: new PlayableSliceRuntimeFixture().SearchPoints;
	}

	private static IReadOnlyList<string> ReadStringArray(JsonElement item, string propertyName)
	{
		if (!item.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
		{
			return Array.Empty<string>();
		}
		return array.EnumerateArray()
			.Select(value => value.GetString() ?? string.Empty)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToArray();
	}

	private static string ReadString(JsonElement element, string propertyName, string fallback) =>
		element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
			? value.GetString() ?? fallback
			: fallback;

	private static int ReadInt(JsonElement element, string propertyName, int fallback) =>
		element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
			? number
			: fallback;

	private static double ReadDouble(JsonElement element, string propertyName, double fallback) =>
		element.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var number)
			? number
			: fallback;
}

internal sealed record PlayableInitialResource(string ResourceId, int Quantity);

internal sealed record PlayableStartingModule(string SlotId, string ModuleType);

internal sealed record PlayableRouteFixture(
	string RouteId,
	string DisplayName,
	string Description,
	string OriginId,
	string DestinationId,
	string DistanceBand,
	IReadOnlyList<string> ChartHazardTags,
	IReadOnlyList<string> NavigationHazardTags);

internal sealed record PlayableSearchPoint(
	string PointId,
	string DisplayName,
	string Description,
	string Zone,
	string RewardResourceId,
	int QuantityMin,
	int QuantityMax,
	string ThreatId,
	double ThreatPosition,
	double ThreatTriggerRadius,
	int ThreatDamage);
