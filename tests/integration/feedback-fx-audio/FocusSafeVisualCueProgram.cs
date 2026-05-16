using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #17 Story 004: Focus-Safe Visual Cue Layer ===");

var failed = 0;
var total = 0;

Run("AC-1: chart feedback overlay keeps Chart focus and Hub inactive", test_chart_feedback_overlay_keeps_chart_focus_and_hub_inactive);
Run("AC-2: exploration feedback overlay preserves HUD labels and controls", test_exploration_feedback_overlay_preserves_hud_labels_and_controls);
Run("AC-3: route selection cue preserves route identity and risk text", test_route_selection_cue_preserves_route_identity_and_risk_text);
Run("AC-4: departure cue confirms transition without blocking it", test_departure_cue_confirms_transition_without_blocking_it);
Run("AC-4: departure cue releases after chart transition", test_departure_cue_releases_after_chart_transition);
Run("AC-4: hub departure cue works without route id", test_hub_departure_cue_works_without_route_id);
Run("AC-5: visual overlay controls are focus disabled and mouse ignored", test_visual_overlay_controls_are_focus_disabled_and_mouse_ignored);
Run("AC-6: scene transition fades non-critical cues without node references", test_scene_transition_fades_non_critical_cues_without_node_references);
Run("QA: clearing presentation outputs also clears visual cue snapshots", test_clear_presentation_outputs_clears_visual_cue_snapshots);

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

static bool test_chart_feedback_overlay_keeps_chart_focus_and_hub_inactive()
{
	var ui = ChartUi();
	var feedback = CreateFeedback();
	feedback.ConnectUiSemanticEvents(ui);
	ui.SetKeyboardFocus("chart.route_confirm");

	ui.SelectRoute("storm-cut-01");
	feedback.ProcessFrame();
	var overlay = LatestOverlay(feedback);

	return ui.CurrentScreen == Screen.ChartRouteSelected
		&& ui.KeyboardFocusElementId == "chart.route_confirm"
		&& ui.ChartVisible
		&& !ui.HubHudVisible
		&& !ui.IsPanelVisible(UIManager.HubHudScreenId)
		&& overlay.TargetSurfaceId == UIManager.ChartScreenId
		&& overlay.Placement == FeedbackVisualPlacement.ChartRouteSelection
		&& !overlay.CapturesMouseInput
		&& !overlay.CapturesKeyboardFocus;
}

static bool test_exploration_feedback_overlay_preserves_hud_labels_and_controls()
{
	var ui = ExplorationUi();
	var feedback = CreateFeedback();
	feedback.ConnectUiSemanticEvents(ui);
	ui.OnHullIntegrityChanged(85, 64);
	ui.OnSearchProgressChanged(2, 6);
	ui.OnScoutPreviewChanged(UIManager.ScoutPreviewPresence);
	ui.ProcessHudFrame();
	ui.OpenModalPanel(UIManager.CombatScreenId);

	ui.ResolveThreatResponse("threat.riftshade", "suppressed");
	feedback.ProcessFrame();
	var overlay = LatestOverlay(feedback);
	var hull = ui.GetHudElementSnapshot(UIManager.ExplorationHullBarElementId);
	var search = ui.GetHudElementSnapshot(UIManager.ExplorationSearchCountElementId);
	var threat = ui.GetHudElementSnapshot(UIManager.ExplorationThreatPreviewElementId);

	return ui.ExplorationHudVisible
		&& hull.Visible
		&& hull.Text == "64/100"
		&& search.Visible
		&& search.Text == "2/6"
		&& threat.Visible
		&& overlay.TargetSurfaceId == UIManager.ExplorationHudScreenId
		&& overlay.ReadabilityGuardToken.Contains(UIManager.ExplorationHullBarElementId, StringComparison.Ordinal)
		&& !overlay.CoversPrimaryText
		&& !overlay.CoversInteractiveControls;
}

static bool test_route_selection_cue_preserves_route_identity_and_risk_text()
{
	var ui = ChartUi();
	var feedback = CreateFeedback();
	feedback.ConnectUiSemanticEvents(ui);

	ui.SelectRoute("storm-cut-01");
	feedback.ProcessFrame();
	var overlay = LatestOverlay(feedback);

	return overlay.AnchorId == "storm-cut-01"
		&& overlay.LabelText == "feedback.route_selected"
		&& overlay.ReadabilityGuardToken.Contains("route_identity", StringComparison.Ordinal)
		&& overlay.ReadabilityGuardToken.Contains("route_risk_text", StringComparison.Ordinal)
		&& !overlay.CoversPrimaryText;
}

static bool test_departure_cue_confirms_transition_without_blocking_it()
{
	var ui = ChartUi();
	var feedback = CreateFeedback();
	feedback.ConnectUiSemanticEvents(ui);
	ui.SelectRoute("storm-cut-01");
	feedback.ProcessFrame();

	var result = ui.ConfirmDeparture();
	feedback.ProcessFrame();
	var overlay = LatestOverlay(feedback);
	var lockResult = ui.CompleteChartLock();

	return result == ScreenResult.Success
		&& lockResult == ScreenResult.Success
		&& ui.CurrentScreen == Screen.Voyage
		&& ui.BlackScreenTransitionStarted
		&& overlay.EventId == UIManager.UIDepartureConfirmedEventId
		&& overlay.Priority == FeedbackPriority.Critical
		&& overlay.Placement == FeedbackVisualPlacement.ChartDepartureTransition
		&& !overlay.BlocksSceneTransition;
}

static bool test_departure_cue_releases_after_chart_transition()
{
	var ui = ChartUi();
	var feedback = CreateFeedback();
	feedback.ConnectUiSemanticEvents(ui);
	ui.SelectRoute("storm-cut-01");
	feedback.ProcessFrame();

	var result = ui.ConfirmDeparture();
	var routeCueReleased = feedback.ReleaseFadedVisualCues();
	feedback.ProcessFrame();
	var activeDeparture = LatestOverlay(feedback);
	var lockResult = ui.CompleteChartLock();
	var fadingDeparture = LatestOverlay(feedback);
	var releasedDeparture = feedback.ReleaseFadedVisualCues();

	return result == ScreenResult.Success
		&& routeCueReleased == 1
		&& activeDeparture.EventId == UIManager.UIDepartureConfirmedEventId
		&& activeDeparture.LifecycleState == FeedbackVisualCueLifecycleState.Active
		&& lockResult == ScreenResult.Success
		&& fadingDeparture.EventId == UIManager.UIDepartureConfirmedEventId
		&& fadingDeparture.LifecycleState == FeedbackVisualCueLifecycleState.Fading
		&& fadingDeparture.FadeRequested
		&& !fadingDeparture.HoldsNodeReference
		&& !fadingDeparture.BlocksSceneTransition
		&& releasedDeparture == 1
		&& feedback.VisualCueOverlays.Count == 0
		&& feedback.ReleasedVisualCueOverlays.Last().EventId == UIManager.UIDepartureConfirmedEventId;
}

static bool test_hub_departure_cue_works_without_route_id()
{
	var ui = CreateUi();
	var feedback = CreateFeedback();
	feedback.ConnectUiSemanticEvents(ui);

	var opened = ui.UseGangway();
	var result = ui.ConfirmDeparture();
	while (!feedback.ProcessFrame().ImmediateReturn)
	{
	}

	var overlay = feedback.VisualCueOverlays.LastOrDefault(item => item.EventId == UIManager.UIDepartureConfirmedEventId)
		?? throw new InvalidOperationException("Expected hub departure visual overlay.");

	return opened == ModalResult.Success
		&& result == ScreenResult.Success
		&& ui.CurrentScreen == Screen.DepartureLocked
		&& overlay.EventId == UIManager.UIDepartureConfirmedEventId
		&& overlay.Priority == FeedbackPriority.Critical
		&& overlay.TargetSurfaceId == "departure_transition"
		&& overlay.AnchorId == "hub"
		&& overlay.Placement == FeedbackVisualPlacement.ChartDepartureTransition
		&& !overlay.BlocksSceneTransition
		&& !overlay.CapturesKeyboardFocus
		&& !overlay.CapturesMouseInput;
}

static bool test_visual_overlay_controls_are_focus_disabled_and_mouse_ignored()
{
	var feedback = CreateFeedback();
	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.focus-safe"));
	feedback.ProcessFrame();
	var overlay = LatestOverlay(feedback);

	return overlay.FocusDisabled
		&& overlay.MouseFilter == MouseFilterMode.Ignore
		&& !overlay.CapturesKeyboardFocus
		&& !overlay.CapturesMouseInput
		&& !overlay.IsModal;
}

static bool test_scene_transition_fades_non_critical_cues_without_node_references()
{
	var feedback = CreateFeedback();
	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.cleanup"));
	feedback.ProcessFrame();
	var cleaned = feedback.HandleSceneTransitionStarted();
	var fading = LatestOverlay(feedback);
	var released = feedback.ReleaseFadedVisualCues();

	return cleaned == 1
		&& fading.LifecycleState == FeedbackVisualCueLifecycleState.Fading
		&& fading.FadeRequested
		&& !fading.HoldsNodeReference
		&& !fading.BlocksSceneTransition
		&& released == 1
		&& feedback.VisualCueOverlays.Count == 0
		&& feedback.ReleasedVisualCueOverlays.Single().LifecycleState == FeedbackVisualCueLifecycleState.Released;
}

static bool test_clear_presentation_outputs_clears_visual_cue_snapshots()
{
	var feedback = CreateFeedback();
	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.clear-released"));
	feedback.ProcessFrame();
	var hadPresentationOutputs = feedback.PresentationOutputs.Count > 0;
	feedback.HandleSceneTransitionStarted();
	feedback.ReleaseFadedVisualCues();
	var hadReleasedCue = feedback.ReleasedVisualCueOverlays.Count == 1;
	feedback.RouteSemanticEvent("ui_route_selected", RoutePayload("route.clear-active"));
	feedback.ProcessFrame();
	var hadActiveCue = feedback.VisualCueOverlays.Count == 1;

	feedback.ClearPresentationOutputs();

	return hadPresentationOutputs
		&& hadReleasedCue
		&& hadActiveCue
		&& feedback.PresentationOutputs.Count == 0
		&& feedback.VisualCueOverlays.Count == 0
		&& feedback.ReleasedVisualCueOverlays.Count == 0;
}

static FeedbackManager CreateFeedback()
{
	var feedback = new FeedbackManager();
	feedback.Initialize();
	return feedback;
}

static UIManager CreateUi()
{
	var ui = new UIManager(new FakeUpstreamDataSource());
	ui.Initialize();
	return ui;
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
	ui.SelectRoute("storm-cut-01");
	ui.ConfirmDeparture();
	ui.CompleteChartLock();
	ui.EncounterContextReady();
	return ui;
}

static FeedbackVisualCueOverlay LatestOverlay(FeedbackManager feedback)
{
	return feedback.VisualCueOverlays.LastOrDefault()
		?? throw new InvalidOperationException("Expected visual overlay.");
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

sealed class FakeUpstreamDataSource : IUiUpstreamDataSource
{
	public IReadOnlyDictionary<string, string>? GetChartState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["state"] = "BROWSING",
		};
	}

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetVisibleRoutes()
	{
		return new[]
		{
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["id"] = "storm-cut-01",
				["name"] = "风暴走廊",
				["risk"] = "中",
			},
		};
	}

	public IReadOnlyDictionary<string, string>? GetSelectedRoute()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["id"] = "storm-cut-01",
			["name"] = "风暴走廊",
			["risk"] = "中",
		};
	}

	public IReadOnlyDictionary<string, string>? GetFilterState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["hide_rumored"] = "false",
		};
	}

	public IReadOnlyDictionary<string, string>? GetHullIntegrity()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["current"] = "64",
			["max"] = "100",
		};
	}

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates() =>
		Array.Empty<IReadOnlyDictionary<string, string>>();

	public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory() =>
		Array.Empty<IReadOnlyDictionary<string, string>>();

	public IReadOnlyDictionary<string, string>? GetStorageState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["current"] = "2",
			["max"] = "10",
		};
	}

	public IReadOnlyDictionary<string, string>? GetCargoState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["current"] = "1",
			["max"] = "5",
		};
	}

	public int? GetCurrency() => 20;

	public IReadOnlyDictionary<string, string>? GetSearchProgress()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["searched"] = "2",
			["total"] = "6",
		};
	}

	public string? GetScoutPreviewLevel() => UIManager.ScoutPreviewPresence;

	public IReadOnlyDictionary<string, string>? GetExtractionState()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["extraction_progress"] = "0.5",
			["is_interrupted"] = "false",
		};
	}

	public IReadOnlyDictionary<string, string>? BuildThreatContext()
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["threat_name"] = "裂帆影",
			["description"] = "从云层边缘扑来",
		};
	}

	public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId)
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["node_id"] = nodeId,
			["node_name"] = "二号雾灯",
		};
	}

	public IReadOnlyDictionary<string, string>? GetStallData(string stallId)
	{
		return new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["stall_id"] = stallId,
			["npc_name"] = "织绳婆婆",
		};
	}

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
