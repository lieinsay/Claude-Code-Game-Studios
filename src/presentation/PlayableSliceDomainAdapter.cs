using CloudWeaverVoyage.Core;

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
	private readonly ChartManager chart;
	private readonly HubManager hub;
	private readonly ResourcesManager resources;
	private readonly ModuleHullManager modules;
	private string lastCommittedRoute = string.Empty;
	private string lastCommittedDestination = string.Empty;
	private string lastStatus = "Domain adapter initialized.";
	private int explorationStep;

	public PlayableSliceDomainAdapter()
	{
		resources = BuildResourcesManager();
		modules = new ModuleHullManager(resources);
		chart = BuildChartManager();
		hub = BuildHubManager();
	}

	public PlayableSliceSnapshot Snapshot => new(
		ChartState: chart.CurrentState.ToString(),
		SelectedRouteId: chart.SelectedRouteId,
		SelectedRouteName: RouteName(chart.SelectedRouteId),
		VisibleRouteCount: chart.VisibleRoutes.Count,
		CommittedRouteId: lastCommittedRoute,
		CommittedDestinationId: lastCommittedDestination,
		HubDockingState: hub.DockingState.ToString(),
		HubDepartureMode: hub.LastDepartureMode,
		HubLastRoute: hub.LastDepartureRoute,
		ExplorationStep: explorationStep,
		BasicSupplyInStorage: resources.GetQuantity(ResourcePool.InStorage, BasicSupplyId),
		RepairKitsInStorage: resources.GetQuantity(ResourcePool.InStorage, RepairKitId),
		RewardInStorage: resources.GetQuantity(ResourcePool.InStorage, RewardResourceId),
		RewardCarried: resources.GetQuantity(ResourcePool.Carried, RewardResourceId),
		CargoUsed: CargoUsed,
		CargoCapacity: 500,
		StorageText: StorageText,
		HullIntegrity: modules.GetHullState().Integrity,
		ThreatText: ThreatText,
		LastStatus: lastStatus);

	public void OpenChart()
	{
		chart.OpenChart();
		lastStatus = $"ChartManager {chart.CurrentState}: visible routes {chart.VisibleRoutes.Count}.";
	}

	public bool SelectRoute(string routeId)
	{
		var selected = chart.SelectRoute(routeId);
		lastStatus = selected
			? $"ChartManager selected {RouteName(routeId)}."
			: $"ChartManager rejected route {routeId}.";
		return selected;
	}

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
		lastStatus = locked
			? $"HubManager departed via {RouteName(routeId)}."
			: $"HubManager rejected departure: {hub.LastRejectionReason}.";
		return locked;
	}

	public void AdvanceExploration()
	{
		var nextStep = Math.Min(explorationStep + 1, 3);
		if (nextStep == explorationStep)
		{
			lastStatus = "Domain exploration fixture is already complete.";
			return;
		}

		if (nextStep is 1 or 2)
		{
			resources.Remove(ResourcePool.InStorage, BasicSupplyId, 1);
			resources.Add(ResourcePool.Carried, RewardResourceId, 1);
		}
		else if (nextStep == 3)
		{
			resources.Add(ResourcePool.Carried, RewardResourceId, 1);
		}

		if (nextStep == 2)
		{
			modules.ApplyHullDamage(6);
		}

		explorationStep = nextStep;
		lastStatus = $"ResourcesManager/ModuleHullManager advanced exploration to {explorationStep}/3.";
	}

	public void ReturnToHub()
	{
		var extraction = resources.ExtractCarriedToStorage();
		hub.TriggerArrival();
		hub.CompleteArrivalAnimation();
		lastStatus = extraction.Success
			? "HubManager arrival completed; ResourcesManager extracted carried rewards to storage."
			: $"HubManager arrival completed; reward extraction failed: {extraction.Result}.";
	}

	private ResourcesManager BuildResourcesManager()
	{
		var registry = new Registry();
		registry.InitializeContent();
		var manager = new ResourcesManager(registry);
		manager.Initialize();
		manager.Add(ResourcePool.InStorage, BasicSupplyId, 10);
		manager.Add(ResourcePool.InStorage, RepairKitId, 4);
		return manager;
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
		manager.SetDockedLocationDelegate(() => OriginId);
		manager.RegisterRoute(new RouteStaticData(
			"route.mist",
			OriginId,
			"location.mist-short",
			"short",
			new[] { "fog", "low_threat" }));
		manager.RegisterRoute(new RouteStaticData(
			"route.market",
			OriginId,
			"location.old-market",
			"medium",
			new[] { "market", "medium_threat" }));
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
			CargoTotalCapacity = () => 500,
			StorageUsedVolume = () => resources.GetUsedVolume(ResourcePool.InStorage),
			StorageTotalCapacity = () => resources.GetTotalVolume(ResourcePool.InStorage),
			ChartDepartureRequest = _ => new HubDepartureRequestResult(true, string.Empty),
		});
		manager.AutoCompleteDepartureLock = false;
		return manager;
	}

	private static string RouteName(string routeId) => routeId switch
	{
		"route.mist" => "雾海短程",
		"route.market" => "旧集市航道",
		_ => string.IsNullOrWhiteSpace(routeId) ? "未命名航线" : routeId,
	};

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
}

public sealed record PlayableSliceSnapshot(
	string ChartState,
	string SelectedRouteId,
	string SelectedRouteName,
	int VisibleRouteCount,
	string CommittedRouteId,
	string CommittedDestinationId,
	string HubDockingState,
	string HubDepartureMode,
	string HubLastRoute,
	int ExplorationStep,
	int BasicSupplyInStorage,
	int RepairKitsInStorage,
	int RewardInStorage,
	int RewardCarried,
	int CargoUsed,
	int CargoCapacity,
	string StorageText,
	int HullIntegrity,
	string ThreatText,
	string LastStatus);
