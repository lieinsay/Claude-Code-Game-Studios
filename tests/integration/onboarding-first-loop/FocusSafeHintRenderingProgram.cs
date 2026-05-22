using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #18 Story 004: Focus-Safe Hint Rendering and Accessibility ===");

var failed = 0;
var total = 0;

Run("AC-1: visible hint does not steal keyboard or mouse input", test_visible_hint_does_not_steal_input);
Run("AC-2: Chart ignores stale Hub anchors", test_chart_ignores_stale_hub_anchors);
Run("AC-3: Exploration pressure hint preserves feedback labels", test_exploration_pressure_hint_preserves_feedback_labels);
Run("AC-4: hints have text and no color-only meaning", test_hints_have_text_and_no_color_only_meaning);
Run("AC-5: keyboard-only and mouse-only paths remain valid", test_keyboard_and_mouse_paths_remain_valid);
Run("AC-6: missing unsafe anchor falls back to safe text-only hint", test_missing_unsafe_anchor_falls_back_to_text_only);
Run("REG-1: one visible hint is kept by default", test_one_visible_hint_by_default);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 004 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 004 validation passed: {total}/{total} checks passed.");
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

static bool test_visible_hint_does_not_steal_input()
{
	var ui = ChartUi();
	ui.SetKeyboardFocus("chart.route_confirm");
	var focusBefore = ui.KeyboardFocusElementId;

	var snapshot = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.SelectRouteStepId,
			"onboarding.chart.select_route",
			"chart.route_list",
			Priority: 70,
			DurationSeconds: 4.0d),
		OnboardingSurface.Chart);

	return snapshot.Visible
		&& ui.KeyboardFocusElementId == focusBefore
		&& snapshot.FocusDisabled
		&& snapshot.MouseFilter == MouseFilterMode.Ignore
		&& !snapshot.CapturesKeyboardFocus
		&& !snapshot.CapturesMouseInput
		&& !snapshot.IsModal;
}

static bool test_chart_ignores_stale_hub_anchors()
{
	var ui = ChartUi();
	var snapshot = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.FindHubHudStepId,
			"onboarding.hub.find_hud",
			"hub.hud.status",
			Priority: 80,
			DurationSeconds: 4.0d),
		OnboardingSurface.Chart);

	return snapshot.Skipped
		&& !snapshot.Visible
		&& snapshot.FallbackReason == "inactive_hub_anchor"
		&& snapshot.TargetSurfaceId == UIManager.ChartScreenId
		&& ui.ChartVisible
		&& !ui.HubHudVisible;
}

static bool test_exploration_pressure_hint_preserves_feedback_labels()
{
	var ui = ExplorationUi();
	var snapshot = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.AdvancePressureStepId,
			"onboarding.exploration.advance_pressure",
			"exploration.pressure",
			Priority: 65,
			DurationSeconds: 4.0d),
		OnboardingSurface.Exploration);

	return snapshot.Visible
		&& snapshot.TargetSurfaceId == UIManager.ExplorationHudScreenId
		&& snapshot.ReadabilityGuardToken.Contains(UIManager.ExplorationHullBarElementId, StringComparison.Ordinal)
		&& snapshot.ReadabilityGuardToken.Contains(UIManager.ExplorationThreatPreviewElementId, StringComparison.Ordinal)
		&& !snapshot.CoversResourceLabel
		&& !snapshot.CoversThreatLabel
		&& !snapshot.CoversHullLabel
		&& !snapshot.CoversStatusLabel;
}

static bool test_hints_have_text_and_no_color_only_meaning()
{
	var ui = ChartUi();
	var rendered = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.SelectRouteStepId,
			"onboarding.chart.select_route",
			"chart.route_list",
			Priority: 70,
			DurationSeconds: 4.0d),
		OnboardingSurface.Chart);
	var skipped = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.SelectRouteStepId,
			string.Empty,
			"chart.route_list",
			Priority: 70,
			DurationSeconds: 4.0d),
		OnboardingSurface.Chart);

	return rendered.HasTextLabel
		&& !rendered.ColorOnlyMeaning
		&& skipped.Skipped
		&& skipped.FallbackReason == "missing_hint_text";
}

static bool test_keyboard_and_mouse_paths_remain_valid()
{
	var ui = ChartUi();
	ui.SetKeyboardFocus("chart.route_confirm");
	var beforeActivation = ui.PressEnterOnFocusedElement();
	var snapshot = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.DepartRouteStepId,
			"onboarding.chart.depart_route",
			"chart.depart_button",
			Priority: 70,
			DurationSeconds: 4.0d),
		OnboardingSurface.Chart);
	var afterActivation = ui.PressEnterOnFocusedElement();

	return beforeActivation == FocusActivationResult.Activated
		&& afterActivation == FocusActivationResult.Activated
		&& snapshot.KeyboardPathValid
		&& snapshot.MousePathValid
		&& snapshot.MouseFilter == MouseFilterMode.Ignore;
}

static bool test_missing_unsafe_anchor_falls_back_to_text_only()
{
	var ui = ChartUi();
	var unsafeAnchors = new HashSet<string>(StringComparer.Ordinal) { "chart.route_list" };
	var snapshot = ui.RenderOnboardingHint(
		new OnboardingHintRequest(
			OnboardingManager.SelectRouteStepId,
			"onboarding.chart.select_route",
			"chart.route_list",
			Priority: 70,
			DurationSeconds: 4.0d),
		OnboardingSurface.Chart,
		unsafeAnchorIds: unsafeAnchors);

	return snapshot.Visible
		&& snapshot.TextOnlyFallback
		&& snapshot.FallbackReason == "text_only_fallback"
		&& snapshot.TargetSurfaceId == "onboarding.safe_text"
		&& snapshot.HasTextLabel;
}

static bool test_one_visible_hint_by_default()
{
	var ui = ChartUi();
	ui.RenderOnboardingHint(
		new OnboardingHintRequest(OnboardingManager.SelectRouteStepId, "hint.one", "chart.route_list", 70, 4.0d),
		OnboardingSurface.Chart);
	ui.RenderOnboardingHint(
		new OnboardingHintRequest(OnboardingManager.DepartRouteStepId, "hint.two", "chart.depart_button", 70, 4.0d),
		OnboardingSurface.Chart);

	return ui.OnboardingHintSnapshots.Count == 1
		&& ui.OnboardingHintSnapshots.Single().StepId == OnboardingManager.DepartRouteStepId;
}

static UIManager ChartUi()
{
	var ui = CreateUi();
	ui.PressMapKey();
	return ui;
}

static UIManager ExplorationUi()
{
	var ui = ChartUi();
	ui.SelectRoute("route.mist");
	ui.ConfirmDeparture();
	ui.CompleteChartLock();
	ui.EncounterContextReady();
	ui.OnHullIntegrityChanged(100, 94);
	ui.OnSearchProgressChanged(1, 3);
	ui.OnScoutPreviewChanged(UIManager.ScoutPreviewPresence);
	ui.ProcessHudFrame();
	return ui;
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
				["risk"] = "低",
			},
		};

	public IReadOnlyDictionary<string, string>? GetSelectedRoute() =>
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["id"] = "route.mist",
			["name"] = "雾海短程",
			["risk"] = "低",
		};

	public IReadOnlyDictionary<string, string>? GetFilterState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["hide_rumored"] = "false" };

	public IReadOnlyDictionary<string, string>? GetHullIntegrity() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["current"] = "94", ["max"] = "100" };

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
		new Dictionary<string, string>(StringComparer.Ordinal) { ["searched"] = "1", ["total"] = "3" };

	public string? GetScoutPreviewLevel() => UIManager.ScoutPreviewPresence;

	public IReadOnlyDictionary<string, string>? GetExtractionState() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["extraction_progress"] = "0.5", ["is_interrupted"] = "false" };

	public IReadOnlyDictionary<string, string>? BuildThreatContext() =>
		new Dictionary<string, string>(StringComparer.Ordinal) { ["threat_name"] = "低威胁", ["description"] = "探索压力可见" };

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
