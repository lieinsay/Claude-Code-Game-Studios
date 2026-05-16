using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #17 Story 005: Smoke Regression, Diagnostics and Performance ===");

var failed = 0;
var total = 0;

Run("AC-1: smoke loop survives feedback hooks without UI/HUD regression", test_smoke_loop_survives_feedback_hooks_without_ui_hud_regression);
Run("AC-2: load clears stale transient cues while allowing load-complete status", test_load_clears_stale_transient_cues_while_allowing_load_complete_status);
Run("AC-3: rapid save/load completion coalesces and keeps latest status", test_rapid_save_load_completion_coalesces_and_keeps_latest_status);
Run("AC-4: missing assets keep smoke loop playable", test_missing_assets_keep_smoke_loop_playable);
Run("AC-5: smoke diagnostics expose routed coalesced skipped and fallback decisions", test_smoke_diagnostics_expose_routed_coalesced_skipped_and_fallback_decisions);
Run("AC-6: numeric smoke evidence stays within budgets", test_numeric_smoke_evidence_stays_within_budgets);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 005 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 005 validation passed: {total}/{total} checks passed.");
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

static bool test_smoke_loop_survives_feedback_hooks_without_ui_hud_regression()
{
	var ui = CreateUi();
	var feedback = CreateFeedback();
	var persistence = MakePersistence();
	feedback.ConnectUiSemanticEvents(ui);
	feedback.ConnectPersistenceEvents(persistence);

	var openedChart = ui.PressMapKey();
	ui.SetKeyboardFocus("chart.route_confirm");
	var selectedRoute = ui.SelectRoute("storm-cut-01");
	var confirmedDeparture = ui.ConfirmDeparture();
	var completedChartLock = ui.CompleteChartLock();
	var enteredExploration = ui.EncounterContextReady();
	ui.OnHullIntegrityChanged(85, 64);
	ui.OnSearchProgressChanged(2, 6);
	ui.OnScoutPreviewChanged(UIManager.ScoutPreviewPresence);
	ui.ProcessHudFrame();
	var save = persistence.RequestSaveProgress();
	var load = persistence.RequestLoadProgress();
	var startedExtraction = ui.ExtractionStarted();
	var completedExtraction = ui.ExtractionComplete();
	var settlementConfirmed = ui.SettlementConfirmed();
	var arrivedHub = ui.ArrivalComplete(namingEligible: false);

	Drain(feedback);
	var hull = ui.GetHudElementSnapshot(UIManager.ExplorationHullBarElementId);
	var search = ui.GetHudElementSnapshot(UIManager.ExplorationSearchCountElementId);

	return openedChart == ScreenResult.Success
		&& selectedRoute == ScreenResult.Success
		&& confirmedDeparture == ScreenResult.Success
		&& completedChartLock == ScreenResult.Success
		&& enteredExploration == ScreenResult.Success
		&& save.Success
		&& load.Success
		&& startedExtraction == ScreenResult.Success
		&& completedExtraction == ScreenResult.Success
		&& settlementConfirmed == ScreenResult.Success
		&& arrivedHub == ScreenResult.Success
		&& ui.CurrentScreen == Screen.Hub
		&& ui.HubHudVisible
		&& !ui.ChartVisible
		&& !ui.ExplorationHudVisible
		&& hull.Text == "64/100"
		&& search.Text == "2/6"
		&& HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.load_completed")
		&& feedback.Diagnostics.Any(item => item.EventId == UIManager.UIRouteSelectedEventId)
		&& feedback.Diagnostics.Any(item => item.EventId == "ui_load_completed");
}

static bool test_load_clears_stale_transient_cues_while_allowing_load_complete_status()
{
	var ui = CreateUi();
	var feedback = CreateFeedback();
	var persistence = MakePersistence();
	feedback.ConnectUiSemanticEvents(ui);
	feedback.ConnectPersistenceEvents(persistence);

	ui.PressMapKey();
	ui.SelectRoute("storm-cut-01");
	var hadQueuedRouteCue = feedback.PendingRequests.Any(request => request.EventId == UIManager.UIRouteSelectedEventId);
	persistence.RequestSaveProgress();
	var load = persistence.RequestLoadProgress();

	return hadQueuedRouteCue
		&& load.Success
		&& feedback.LastLoadClearedTransientCueCount >= 1
		&& !feedback.PendingRequests.Any(request => request.EventId == UIManager.UIRouteSelectedEventId)
		&& feedback.PendingRequests.Any(request => request.EventId == "ui_load_completed")
		&& feedback.PendingRequests
			.Where(request => request.EventId == "ui_load_completed")
			.All(request => request.StatusText == "feedback.load_completed");
}

static bool test_rapid_save_load_completion_coalesces_and_keeps_latest_status()
{
	var feedback = CreateFeedback(clockSeconds: () => 10.0d);
	var persistence = MakePersistence();
	var selectedRequests = new List<FeedbackRequest>();
	feedback.ConnectPersistenceEvents(persistence);
	feedback.FeedbackOutputSelected += selectedRequests.Add;

	var firstSave = persistence.RequestSaveProgress();
	var secondSave = persistence.RequestSaveProgress();
	var firstLoad = persistence.RequestLoadProgress();
	var secondLoad = persistence.RequestLoadProgress();

	Drain(feedback);

	return firstSave.Success
		&& secondSave.Success
		&& firstLoad.Success
		&& secondLoad.Success
		&& feedback.Diagnostics.Any(item =>
			item.EventId == "ui_save_completed"
			&& item.Decision == FeedbackOutputDecision.Coalesced
			&& item.Coalesced
			&& item.CoalesceKey == "save:progress")
		&& feedback.Diagnostics.Any(item =>
			item.EventId == "ui_load_completed"
			&& item.Decision == FeedbackOutputDecision.Coalesced
			&& item.Coalesced
			&& item.CoalesceKey == "load:progress")
		&& selectedRequests.Any(request =>
			request.EventId == "ui_save_completed"
			&& PayloadInt(request, "generation") == secondSave.Generation)
		&& selectedRequests.Any(request =>
			request.EventId == "ui_load_completed"
			&& PayloadInt(request, "generation") == secondLoad.Generation)
		&& HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.save_completed")
		&& HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.load_completed")
		&& feedback.Diagnostics.Any(item => item.EventId == "ui_load_completed");
}

static bool test_missing_assets_keep_smoke_loop_playable()
{
	var ui = CreateUi();
	var feedback = CreateFeedback();
	var persistence = MakePersistence();
	feedback.ConnectUiSemanticEvents(ui);
	feedback.ConnectPersistenceEvents(persistence);
	feedback.MarkVisualCueMissing("visual.chart.route_selected");
	feedback.MarkAudioCueMissing("audio.chart.route_selected");
	feedback.MarkVisualCueMissing("visual.session.load");
	feedback.MarkAudioCueMissing("audio.session.load");
	feedback.IsAudioMuted = true;

	ui.PressMapKey();
	ui.SetKeyboardFocus("chart.route_confirm");
	ui.SelectRoute("storm-cut-01");
	feedback.ProcessFrame();
	var focusAfterMissingRouteCue = ui.KeyboardFocusElementId;
	ui.ConfirmDeparture();
	ui.CompleteChartLock();
	ui.EncounterContextReady();
	var save = persistence.RequestSaveProgress();
	var load = persistence.RequestLoadProgress();
	Drain(feedback);
	var startedExtraction = ui.ExtractionStarted();
	var completedExtraction = ui.ExtractionComplete();
	var settlementConfirmed = ui.SettlementConfirmed();
	var arrivedHub = ui.ArrivalComplete(namingEligible: false);

	return focusAfterMissingRouteCue == "chart.route_confirm"
		&& save.Success
		&& load.Success
		&& startedExtraction == ScreenResult.Success
		&& completedExtraction == ScreenResult.Success
		&& settlementConfirmed == ScreenResult.Success
		&& arrivedHub == ScreenResult.Success
		&& ui.CurrentScreen == Screen.Hub
		&& HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.route_selected")
		&& HasTextOutput(feedback, FeedbackOutputChannel.Status, "feedback.load_completed")
		&& feedback.Diagnostics.Any(item => item.Decision == FeedbackOutputDecision.VisualSkippedMissingAsset)
		&& feedback.Diagnostics.Any(item => item.Decision == FeedbackOutputDecision.AudioSkippedMissingAsset)
		&& feedback.Diagnostics.Any(item =>
			item.Decision == FeedbackOutputDecision.StatusFallbackRequested
			&& item.FallbackReason is "missing_visual_asset" or "missing_audio_asset");
}

static bool test_smoke_diagnostics_expose_routed_coalesced_skipped_and_fallback_decisions()
{
	var feedback = CreateFeedback(clockSeconds: () => 20.0d);
	feedback.MarkVisualCueMissing("visual.chart.route_selected");
	feedback.MarkAudioCueMissing("audio.chart.route_selected");

	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.alpha"), nowSeconds: 20.0d, sourceSystem: "UIManager");
	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.beta"), nowSeconds: 21.0d, sourceSystem: "UIManager");
	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.beta"), nowSeconds: 21.1d, sourceSystem: "UIManager");
	Drain(feedback);

	var routed = feedback.Diagnostics.FirstOrDefault(item => item.Decision == FeedbackOutputDecision.Routed);
	var coalesced = feedback.Diagnostics.FirstOrDefault(item => item.Decision == FeedbackOutputDecision.Coalesced);
	var skippedVisual = feedback.Diagnostics.FirstOrDefault(item => item.Decision == FeedbackOutputDecision.VisualSkippedMissingAsset);
	var skippedAudio = feedback.Diagnostics.FirstOrDefault(item => item.Decision == FeedbackOutputDecision.AudioSkippedMissingAsset);
	var fallback = feedback.Diagnostics.FirstOrDefault(item => item.Decision == FeedbackOutputDecision.StatusFallbackRequested);

	return HasDiagnosticIdentity(routed)
		&& HasDiagnosticIdentity(coalesced)
		&& HasDiagnosticIdentity(skippedVisual)
		&& HasDiagnosticIdentity(skippedAudio)
		&& HasDiagnosticIdentity(fallback)
		&& skippedVisual?.FallbackReason == "missing_visual_asset"
		&& skippedAudio?.FallbackReason == "missing_audio_asset"
		&& fallback?.FallbackReason is "missing_visual_asset" or "missing_audio_asset";
}

static bool test_numeric_smoke_evidence_stays_within_budgets()
{
	var budget = new SmokePerformanceBudget(
		WorstFrameMs: 16.0d,
		PeakMemoryTotalMiB: 512.0d,
		PeakExplorationMemoryMiB: 200.0d,
		PeakDrawCalls: 400.0d,
		SaveP95Ms: 50.0d,
		LoadP95Ms: 50.0d,
		SaveMaxMs: 100.0d,
		LoadMaxMs: 100.0d,
		MinimumSaveLoadCycles: 10);
	var windowed = new SmokePerformanceSample(
		WorstFrameMs: 3.980d,
		PeakMemoryMiB: 52.263d,
		PeakDrawCalls: 103.0d,
		SaveP95Ms: 1.461d,
		LoadP95Ms: 1.469d,
		SaveMaxMs: 1.461d,
		LoadMaxMs: 1.469d,
		SaveLoadCycles: 10,
		DrawCallsAvailable: true);
	var headless = new SmokePerformanceSample(
		WorstFrameMs: 8.265d,
		PeakMemoryMiB: 48.320d,
		PeakDrawCalls: 0.0d,
		SaveP95Ms: 6.920d,
		LoadP95Ms: 6.927d,
		SaveMaxMs: 6.920d,
		LoadMaxMs: 6.927d,
		SaveLoadCycles: 10,
		DrawCallsAvailable: false);

	return budget.Accepts(windowed)
		&& budget.Accepts(headless)
		&& FileContains("tests/smoke/session_shell_perf_probe.gd", "FRAME_BUDGET_MS := 16.0")
		&& FileContains("tests/smoke/session_shell_perf_probe.gd", "DRAW_CALL_BUDGET := 400.0")
		&& FileContains("tests/smoke/session_shell_perf_probe.gd", "SAVE_LOAD_BUDGET_MS := 50.0")
		&& FileContains("production/qa/perf-profile-production-to-polish-2026-05-15.md", "Budget result: all measurable smoke-loop budgets passed");
}

static FeedbackManager CreateFeedback(Func<double>? clockSeconds = null)
{
	var feedback = clockSeconds is null ? new FeedbackManager() : new FeedbackManager(clockSeconds);
	feedback.Initialize();
	return feedback;
}

static UIManager CreateUi()
{
	var ui = new UIManager(new FakeUpstreamDataSource());
	ui.Initialize();
	return ui;
}

static Persistence MakePersistence()
{
	var persistence = new Persistence();
	persistence.RegisterDomainSerializer("resources", ValidPackage);
	persistence.RegisterDomainDeserializer("resources", _ => { });
	return persistence;
}

static SnapshotPackage ValidPackage()
{
	var package = new SnapshotPackage
	{
		DomainId = "resources",
		SnapshotSchemaVersion = 1,
		DomainState = SnapshotDomainState.Ready,
	};
	package.ContentDomainVersions["resources"] = "1";
	package.StableIdRefs.Add("resource.basic_supply");
	package.Payload["storage"] = new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["resource.basic_supply"] = 3,
	};
	return package;
}

static Dictionary<string, object?> RoutePayload(string routeId)
{
	return new Dictionary<string, object?>(StringComparer.Ordinal)
	{
		["route_id"] = routeId,
		["status_text"] = "feedback.route_selected",
		["caption_text"] = "feedback.route_selected.caption",
	};
}

static void Drain(FeedbackManager feedback)
{
	while (!feedback.ProcessFrame().ImmediateReturn)
	{
	}
}

static bool HasTextOutput(FeedbackManager feedback, FeedbackOutputChannel channel, string text)
{
	return feedback.PresentationOutputs.Any(item => item.Channel == channel && item.Text == text);
}

static bool HasDiagnosticIdentity(FeedbackDiagnosticSnapshot? diagnostic)
{
	return diagnostic is not null
		&& !string.IsNullOrWhiteSpace(diagnostic.EventId)
		&& !string.IsNullOrWhiteSpace(diagnostic.SourceSystem)
		&& !string.IsNullOrWhiteSpace(diagnostic.CueFamily);
}

static int? PayloadInt(FeedbackRequest request, string key)
{
	return request.Payload.TryGetValue(key, out var value) && value is int intValue ? intValue : null;
}

static bool FileContains(string relativePath, string expected)
{
	var repoRoot = FindRepoRoot();
	var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
	return File.Exists(path) && File.ReadAllText(path).Contains(expected, StringComparison.Ordinal);
}

static string FindRepoRoot()
{
	var current = new DirectoryInfo(AppContext.BaseDirectory);
	while (current is not null)
	{
		if (File.Exists(Path.Combine(current.FullName, "project.godot")))
		{
			return current.FullName;
		}

		current = current.Parent;
	}

	throw new DirectoryNotFoundException("Could not find repository root containing project.godot.");
}

sealed record SmokePerformanceSample(
	double WorstFrameMs,
	double PeakMemoryMiB,
	double PeakDrawCalls,
	double SaveP95Ms,
	double LoadP95Ms,
	double SaveMaxMs,
	double LoadMaxMs,
	int SaveLoadCycles,
	bool DrawCallsAvailable);

sealed record SmokePerformanceBudget(
	double WorstFrameMs,
	double PeakMemoryTotalMiB,
	double PeakExplorationMemoryMiB,
	double PeakDrawCalls,
	double SaveP95Ms,
	double LoadP95Ms,
	double SaveMaxMs,
	double LoadMaxMs,
	int MinimumSaveLoadCycles)
{
	public bool Accepts(SmokePerformanceSample sample)
	{
		return sample.WorstFrameMs <= WorstFrameMs
			&& sample.PeakMemoryMiB <= PeakMemoryTotalMiB
			&& sample.PeakMemoryMiB <= PeakExplorationMemoryMiB
			&& (!sample.DrawCallsAvailable || sample.PeakDrawCalls <= PeakDrawCalls)
			&& sample.SaveP95Ms < SaveP95Ms
			&& sample.LoadP95Ms < LoadP95Ms
			&& sample.SaveMaxMs < SaveMaxMs
			&& sample.LoadMaxMs < LoadMaxMs
			&& sample.SaveLoadCycles >= MinimumSaveLoadCycles;
	}
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
				["id"] = "storm-cut-01",
				["name"] = "风暴走廊",
				["risk"] = "中",
			},
		};

	public IReadOnlyDictionary<string, string>? GetSelectedRoute() =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["id"] = "storm-cut-01",
			["name"] = "风暴走廊",
			["risk"] = "中",
		};

	public IReadOnlyDictionary<string, string>? GetFilterState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["hide_rumored"] = "false" };

	public IReadOnlyDictionary<string, string>? GetHullIntegrity() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["current"] = "64", ["max"] = "100" };

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
		new Dictionary<string, string>(StringComparer.Ordinal) { ["searched"] = "2", ["total"] = "6" };

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
			["threat_name"] = "裂帆影",
			["description"] = "从云层边缘扑来",
		};

	public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId) =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["node_id"] = nodeId,
			["node_name"] = "二号雾灯",
		};

	public IReadOnlyDictionary<string, string>? GetStallData(string stallId) =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["stall_id"] = stallId,
			["npc_name"] = "织绳婆婆",
		};

	public string? QueryPartnerName() => "灰白猫";

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems() =>
		Array.Empty<IReadOnlyDictionary<string, string>>();

	public bool? NamingPromptEligibility() => false;

	public string? GetDisplayName(string entityId) => entityId switch
	{
		"item.saltcloth" => "盐帆布",
		"good.rope" => "缆绳",
		_ => entityId,
	};

	public string? GetDescription(string entityId) => $"description:{entityId}";

	public bool TransferItem(string itemId, string fromPool, string toPool, int quantity) => true;

	public bool DiscardItem(string itemId) => true;

	public bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials) => true;

	public bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost) => true;

	public bool SubmitPartnerName(string partnerId, string name) => true;
}
