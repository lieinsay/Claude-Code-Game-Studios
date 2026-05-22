using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #18 Story 002: UI and Domain Event Integration ===");

var failed = 0;
var total = 0;

Run("AC-1: Hub visibility completes first step only when reachable", test_hub_visibility_completes_when_reachable);
Run("AC-2: Chart activation completes chart step and suppresses Hub hints", test_chart_activation_completes_and_suppresses_hub_hints);
Run("AC-3: route and departure events complete after mutation in order", test_route_and_departure_complete_after_mutation);
Run("AC-4: exploration pressure feedback completes pressure step", test_exploration_pressure_completes_step);
Run("AC-5: save/load visibility or use completes awareness step", test_save_load_awareness_completes_step);
Run("AC-6: return Hub summary changes complete final steps", test_return_hub_summary_completes_final_steps);
Run("REG-1: full adapter connection completes all eight steps without focus ownership", test_full_adapter_connection_completes_all_steps);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 002 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 002 validation passed: {total}/{total} checks passed.");
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
		failed++;
		Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
		return;
	}

	failed++;
	Console.Error.WriteLine($"[FAIL] {label}");
}

static bool test_hub_visibility_completes_when_reachable()
{
	var blocked = new OnboardingManager();
	var blockedResult = blocked.ObserveHubVisible(inputReachable: false, ownerStateAlreadyMutated: true);

	var onboarding = new OnboardingManager();
	var result = onboarding.ObserveHubVisible(inputReachable: true, ownerStateAlreadyMutated: true);

	return !blockedResult.Accepted
		&& blocked.GetStepProgress(OnboardingManager.FindHubHudStepId).State == OnboardingStepState.Eligible
		&& result.Accepted
		&& onboarding.ActiveSurface == OnboardingSurface.Hub
		&& onboarding.GetStepProgress(OnboardingManager.FindHubHudStepId).State == OnboardingStepState.Completed
		&& onboarding.ObservedEvents.Last().EventId == "hub_visible";
}

static bool test_chart_activation_completes_and_suppresses_hub_hints()
{
	var onboarding = new OnboardingManager();
	var ui = CreateUi();
	onboarding.ConnectUiEvents(ui);

	onboarding.ObserveHubVisible(inputReachable: true, ownerStateAlreadyMutated: true);
	ui.PressMapKey();

	return onboarding.GetStepProgress(OnboardingManager.OpenChartStepId).State == OnboardingStepState.Completed
		&& onboarding.ActiveSurface == OnboardingSurface.Chart
		&& onboarding.SuppressedHintStepIds.Contains(OnboardingManager.FindHubHudStepId)
		&& ui.KeyboardFocusElementId is not null;
}

static bool test_route_and_departure_complete_after_mutation()
{
	var onboarding = PreparedThroughChart();
	var adapter = new PlayableSliceDomainAdapter();
	onboarding.ConnectPlayableSliceEvents(adapter);

	var rejectedDeparture = onboarding.ObserveDepartureConfirmed("route.mist", ownerStateAlreadyMutated: false);
	adapter.OpenChart();
	var selected = adapter.SelectRoute("route.mist");
	var departed = adapter.ConfirmDeparture();

	return !rejectedDeparture.Accepted
		&& selected
		&& departed
		&& onboarding.GetStepProgress(OnboardingManager.SelectRouteStepId).State == OnboardingStepState.Completed
		&& onboarding.GetStepProgress(OnboardingManager.DepartRouteStepId).State == OnboardingStepState.Completed
		&& onboarding.ActiveSurface == OnboardingSurface.Exploration
		&& onboarding.ObservedEvents.Any(item => item.EventId == "departure_confirmed" && item.Accepted);
}

static bool test_exploration_pressure_completes_step()
{
	var onboarding = PreparedThroughDeparture(out var adapter);
	var unchanged = onboarding.ObserveExplorationPressureChanged(adapter.Snapshot, ownerStateAlreadyMutated: true);

	adapter.AdvanceExploration();

	return !unchanged.Accepted
		&& onboarding.GetStepProgress(OnboardingManager.AdvancePressureStepId).State == OnboardingStepState.Completed
		&& onboarding.ObservedEvents.Any(item => item.EventId == "exploration_pressure_changed" && item.Accepted)
		&& adapter.Snapshot.ThreatText == "低威胁";
}

static bool test_save_load_awareness_completes_step()
{
	var onboarding = PreparedThroughPressure(out var adapter);
	var ignored = onboarding.ObserveSaveLoadAwareness(visibleOrUsed: false, ownerStateAlreadyMutated: true);
	var save = adapter.SaveSceneState(new PlayableSliceSceneState("exploration", "route.mist", 1, 120.0f, 240.0f, "test"));

	return !ignored.Accepted
		&& save.Success
		&& onboarding.GetStepProgress(OnboardingManager.NoticeSaveLoadStepId).State == OnboardingStepState.Completed
		&& onboarding.ActiveSurface == OnboardingSurface.Session
		&& onboarding.ObservedEvents.Any(item => item.EventId == "save_load_awareness" && item.Accepted);
}

static bool test_return_hub_summary_completes_final_steps()
{
	var onboarding = PreparedThroughSaveLoad(out var adapter);
	adapter.ReturnToHub();

	return onboarding.GetStepProgress(OnboardingManager.ReturnHubStepId).State == OnboardingStepState.Completed
		&& onboarding.GetStepProgress(OnboardingManager.NoticeSummaryChangeStepId).State == OnboardingStepState.Completed
		&& onboarding.ActiveSurface == OnboardingSurface.Hub
		&& onboarding.IsFirstLoopComplete
		&& onboarding.ObservedEvents.Any(item => item.EventId == "hub_summary_changed" && item.Accepted);
}

static bool test_full_adapter_connection_completes_all_steps()
{
	var onboarding = new OnboardingManager();
	var adapter = new PlayableSliceDomainAdapter();
	onboarding.ConnectPlayableSliceEvents(adapter);
	onboarding.ObserveHubVisible(inputReachable: true, ownerStateAlreadyMutated: true);

	adapter.OpenChart();
	adapter.SelectRoute("route.mist");
	adapter.ConfirmDeparture();
	adapter.AdvanceExploration();
	adapter.SaveSceneState(new PlayableSliceSceneState("exploration", "route.mist", 1, 220.0f, 330.0f, "saved"));
	adapter.ReturnToHub();

	return onboarding.IsFirstLoopComplete
		&& Math.Abs(onboarding.FirstLoopProgressPercent - 100.0d) < 0.001d
		&& onboarding.ObservedEvents.Count(item => item.Accepted) >= 8;
}

static OnboardingManager PreparedThroughChart()
{
	var onboarding = new OnboardingManager();
	onboarding.ObserveHubVisible(inputReachable: true, ownerStateAlreadyMutated: true);
	onboarding.ObserveChartActive(ownerStateAlreadyMutated: true);
	return onboarding;
}

static OnboardingManager PreparedThroughDeparture(out PlayableSliceDomainAdapter adapter)
{
	var onboarding = PreparedThroughChart();
	adapter = new PlayableSliceDomainAdapter();
	onboarding.ConnectPlayableSliceEvents(adapter);
	adapter.OpenChart();
	adapter.SelectRoute("route.mist");
	adapter.ConfirmDeparture();
	return onboarding;
}

static OnboardingManager PreparedThroughPressure(out PlayableSliceDomainAdapter adapter)
{
	var onboarding = PreparedThroughDeparture(out adapter);
	adapter.AdvanceExploration();
	return onboarding;
}

static OnboardingManager PreparedThroughSaveLoad(out PlayableSliceDomainAdapter adapter)
{
	var onboarding = PreparedThroughPressure(out adapter);
	adapter.SaveSceneState(new PlayableSliceSceneState("exploration", "route.mist", 1, 220.0f, 330.0f, "saved"));
	return onboarding;
}

static UIManager CreateUi()
{
	var ui = new UIManager(new FakeUpstreamDataSource());
	ui.Initialize();
	return ui;
}

sealed class FakeUpstreamDataSource : IUiUpstreamDataSource
{
	public IReadOnlyDictionary<string, string>? GetChartState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["state"] = "BROWSING" };

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetVisibleRoutes() =>
		new[]
		{
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["id"] = "route.mist",
				["name"] = "雾海短程",
			},
		};

	public IReadOnlyDictionary<string, string>? GetSelectedRoute() =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["id"] = "route.mist",
			["name"] = "雾海短程",
		};

	public IReadOnlyDictionary<string, string>? GetFilterState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["hide_rumored"] = "false" };

	public IReadOnlyDictionary<string, string>? GetHullIntegrity() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["current"] = "94" };

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates() =>
		Array.Empty<IReadOnlyDictionary<string, string>>();

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory() =>
		Array.Empty<IReadOnlyDictionary<string, string>>();

	public IReadOnlyDictionary<string, string>? GetStorageState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["current"] = "2", ["max"] = "10" };

	public IReadOnlyDictionary<string, string>? GetCargoState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["current"] = "1", ["max"] = "5" };

	public int? GetCurrency() => 20;

	public IReadOnlyDictionary<string, string>? GetSearchProgress() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["searched"] = "1", ["total"] = "6" };

	public string? GetScoutPreviewLevel() => UIManager.ScoutPreviewPresence;

	public IReadOnlyDictionary<string, string>? GetExtractionState() =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["extraction_progress"] = "0.5",
			["is_interrupted"] = "false",
		};

	public IReadOnlyDictionary<string, string>? BuildThreatContext() =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["threat_name"] = "低威胁",
			["description"] = "探索压力可见",
		};

	public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId) =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["node_id"] = nodeId };

	public IReadOnlyDictionary<string, string>? GetStallData(string stallId) =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["stall_id"] = stallId };

	public string? QueryPartnerName() => "灰白猫";

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems() =>
		Array.Empty<IReadOnlyDictionary<string, string>>();

	public bool? NamingPromptEligibility() => true;

	public string? GetDisplayName(string entityId) => entityId;

	public string? GetDescription(string entityId) => $"description:{entityId}";

	public bool TransferItem(string itemId, string fromPool, string toPool, int quantity) => true;

	public bool DiscardItem(string itemId) => true;

	public bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials) => true;

	public bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost) => true;

	public bool SubmitPartnerName(string partnerId, string name) => true;
}
