using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #17 Story 002: UI and Session Semantic Event Wiring ===");

var failed = 0;
var total = 0;

Run("AC-1: panel events route minor requests without extra focus changes", test_panel_events_route_minor_requests_without_extra_focus_changes);
Run("AC-2: route selection maps to Chart request with visible fallback", test_route_selection_maps_to_chart_request_with_visible_fallback);
Run("AC-3: departure confirmation maps to critical request after transition", test_departure_confirmation_maps_after_transition);
Run("AC-4: core UI action events map to exact cue families and priorities", test_core_ui_action_events_map_to_exact_cue_families_and_priorities);
Run("AC-5: save and load events come from Persistence not UIManager", test_save_and_load_events_come_from_persistence_not_uimanager);
Run("AC-6: hooked MVP channels expose diagnostics and text fallbacks", test_hooked_mvp_channels_expose_diagnostics_and_text_fallbacks);

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

static bool test_panel_events_route_minor_requests_without_extra_focus_changes()
{
    var baseline = ChartUi();
    baseline.SetKeyboardFocus("chart.route_confirm");
    baseline.OpenNonModal(UIManager.PartnerSniffScreenId);
    baseline.CloseNonModal(UIManager.PartnerSniffScreenId);
    var expectedFocus = baseline.KeyboardFocusElementId;

    var ui = ChartUi();
    var feedback = CreateFeedback();
    feedback.ConnectUiSemanticEvents(ui);
    ui.SetKeyboardFocus("chart.route_confirm");
    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    ui.CloseNonModal(UIManager.PartnerSniffScreenId);

    var opened = Request(feedback, UIManager.UIPanelOpenedEventId);
    var closed = Request(feedback, UIManager.UIPanelClosedEventId);

    return ui.KeyboardFocusElementId == expectedFocus
        && opened.Priority == FeedbackPriority.Minor
        && closed.Priority == FeedbackPriority.Minor
        && opened.SourceSystem == "UIManager"
        && closed.SourceSystem == "UIManager"
        && PayloadValue(opened, "panel_id") == UIManager.PartnerSniffScreenId
        && PayloadValue(closed, "panel_id") == UIManager.PartnerSniffScreenId;
}

static bool test_route_selection_maps_to_chart_request_with_visible_fallback()
{
    var ui = ChartUi();
    var feedback = CreateFeedback();
    feedback.ConnectUiSemanticEvents(ui);

    ui.SelectRoute("storm-cut-01");
    var request = Request(feedback, UIManager.UIRouteSelectedEventId);
    var emitted = Event(ui, UIManager.UIRouteSelectedEventId);

    return ui.CurrentScreen == Screen.ChartRouteSelected
        && request.SourceSystem == "UIManager"
        && request.CueFamily == "Chart"
        && request.Priority == FeedbackPriority.Minor
        && request.AudioCueId is not null
        && request.CaptionText is not null
        && request.StatusText is not null
        && PayloadValue(request, "route_id") == "storm-cut-01"
        && PayloadValue(request, "route_name") == "风暴走廊"
        && emitted.CascadeDepth <= UIManager.MaxSemanticCascadeDepth;
}

static bool test_departure_confirmation_maps_after_transition()
{
    var ui = ChartUi();
    ui.SelectRoute("storm-cut-01");
    var feedback = CreateFeedback();
    feedback.ConnectUiSemanticEvents(ui);

    ui.ConfirmDeparture();
    var request = Request(feedback, UIManager.UIDepartureConfirmedEventId);

    return ui.CurrentScreen == Screen.ChartDepartureConfirmed
        && request.SourceSystem == "UIManager"
        && request.CueFamily == "Chart"
        && request.Priority == FeedbackPriority.Critical
        && request.StatusText is not null
        && request.CaptionText is not null
        && PayloadValue(request, "departure_mode") == "chart";
}

static bool test_core_ui_action_events_map_to_exact_cue_families_and_priorities()
{
    var ui = CreateUi();
    var feedback = CreateFeedback();
    feedback.ConnectUiSemanticEvents(ui);

    ui.OpenModalPanel(UIManager.CombatScreenId);
    ui.ResolveThreatResponse("threat.riftshade", "suppressed");
    ui.OpenRepairPanelForNode("beacon_02");
    ui.SubmitRepairMaterials("beacon_02", new Dictionary<string, int>(StringComparer.Ordinal) { ["item.saltcloth"] = 2 });
    ui.OpenModalPanel(UIManager.MarketScreenId, new Dictionary<string, string>(StringComparer.Ordinal) { ["stall_id"] = "stall_weaver" });
    ui.ConfirmPurchase("stall_weaver", "good.rope", 2, 14);
    ui.ConfirmCapacityTransfer("item.saltcloth", UIManager.CarriedPoolId, UIManager.StoragePoolId, 1);

    return Matches(feedback, UIManager.UIThreatResponseChosenEventId, "Exploration HUD", FeedbackPriority.Major)
        && Matches(feedback, UIManager.UIRepairSubmittedEventId, "Repair", FeedbackPriority.Major)
        && Matches(feedback, UIManager.UIPurchaseConfirmedEventId, "Market/Inventory", FeedbackPriority.Major)
        && Matches(feedback, UIManager.UIItemTransferredEventId, "Market/Inventory", FeedbackPriority.Minor);
}

static bool test_save_and_load_events_come_from_persistence_not_uimanager()
{
    var feedback = CreateFeedback();
    var persistence = MakePersistence();
    feedback.ConnectPersistenceEvents(persistence);

    var progressSave = persistence.RequestSaveProgress();
    var progressLoad = persistence.RequestLoadProgress();
    var settingsSave = persistence.RequestSaveSettings();
    var settingsLoad = persistence.RequestLoadSettings();
    var progressSaveRequest = RequestWithPayload(feedback, "ui_save_completed", "artifact_kind", "progress");
    var progressLoadRequest = RequestWithPayload(feedback, "ui_load_completed", "artifact_kind", "progress");
    var settingsSaveRequest = RequestWithPayload(feedback, "ui_save_completed", "artifact_kind", "settings");
    var settingsLoadRequest = RequestWithPayload(feedback, "ui_load_completed", "artifact_kind", "settings");
    var progressSaveRequestCount = feedback.PendingRequests.Count(request =>
        request.EventId == "ui_save_completed"
        && PayloadValue(request, "artifact_kind") == "progress");

    return progressSave.Success
        && progressLoad.Success
        && settingsSave.Success
        && settingsLoad.Success
        && progressSaveRequest.SourceSystem == "Persistence"
        && progressLoadRequest.SourceSystem == "Persistence"
        && settingsSaveRequest.SourceSystem == "Persistence"
        && settingsLoadRequest.SourceSystem == "Persistence"
        && progressSaveRequest.SourceSystem != "UIManager"
        && progressLoadRequest.SourceSystem != "UIManager"
        && settingsSaveRequest.SourceSystem != "UIManager"
        && settingsLoadRequest.SourceSystem != "UIManager"
        && progressSaveRequest.CueFamily == "Session"
        && progressLoadRequest.CueFamily == "Session"
        && settingsSaveRequest.CueFamily == "Session"
        && settingsLoadRequest.CueFamily == "Session"
        && progressSaveRequest.StatusText is not null
        && progressLoadRequest.StatusText is not null
        && settingsSaveRequest.StatusText is not null
        && settingsLoadRequest.StatusText is not null
        && progressSaveRequestCount == 1;
}

static bool test_hooked_mvp_channels_expose_diagnostics_and_text_fallbacks()
{
    var ui = ChartUi();
    var feedback = CreateFeedback();
    feedback.ConnectUiSemanticEvents(ui);

    ui.SelectRoute("storm-cut-01");
    ui.ConfirmDeparture();
    ui.CompleteChartLock();
    ui.EncounterContextReady();
    ui.OnHullIntegrityChanged(85, 64);
    ui.ProcessHudFrame();
    ui.OpenModalPanel(UIManager.CombatScreenId);
    ui.ResolveThreatResponse("threat.riftshade", "suppressed");

    var hookedRequests = feedback.PendingRequests
        .Where(request => request.EventId is UIManager.UIRouteSelectedEventId
            or UIManager.UIDepartureConfirmedEventId
            or UIManager.UIThreatResponseChosenEventId)
        .ToArray();
    var hull = ui.GetHudElementSnapshot(UIManager.ExplorationHullBarElementId);

    return hookedRequests.Length == 3
        && hookedRequests.All(request =>
            request.SourceSystem.Length > 0
            && request.EventId.Length > 0
            && request.CueFamily.Length > 0
            && (request.StatusText is not null || request.CaptionText is not null))
        && feedback.Diagnostics.Any(item =>
            item.EventId == UIManager.UIThreatResponseChosenEventId
            && item.CueFamily == "Exploration HUD"
            && item.Priority == FeedbackPriority.Major)
        && hull.Visible
        && hull.Text == "64/100";
}

static bool Matches(FeedbackManager feedback, string eventId, string cueFamily, FeedbackPriority priority)
{
    var request = Request(feedback, eventId);
    var diagnostic = feedback.Diagnostics.LastOrDefault(item => item.EventId == eventId);

    return request.CueFamily == cueFamily
        && request.Priority == priority
        && request.StatusText is not null
        && diagnostic is not null
        && diagnostic.CueFamily == cueFamily
        && diagnostic.Priority == priority;
}

static FeedbackRequest Request(FeedbackManager feedback, string eventId)
{
    return feedback.PendingRequests.LastOrDefault(item => item.EventId == eventId)
        ?? throw new InvalidOperationException($"Missing feedback request: {eventId}");
}

static FeedbackRequest RequestWithPayload(FeedbackManager feedback, string eventId, string payloadKey, string payloadValue)
{
    return feedback.PendingRequests.LastOrDefault(item =>
        item.EventId == eventId && PayloadValue(item, payloadKey) == payloadValue)
        ?? throw new InvalidOperationException($"Missing feedback request: {eventId} with {payloadKey}={payloadValue}");
}

static UiSemanticEventSnapshot Event(UIManager ui, string eventId)
{
    return ui.GetLatestSemanticEvent(eventId)
        ?? throw new InvalidOperationException($"Missing UI event: {eventId}");
}

static string? PayloadValue(FeedbackRequest request, string key)
{
    return request.Payload.TryGetValue(key, out var value) ? Convert.ToString(value) : null;
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

static Persistence MakePersistence()
{
    var persistence = new Persistence();
    persistence.RegisterDomainSerializer("resources", ValidPackage);
    persistence.RegisterDomainDeserializer("resources", _ => { });
    persistence.RegisterDomainSerializer(PersistenceArtifactKind.Settings, "settings.profile", ValidSettingsPackage);
    persistence.RegisterDomainDeserializer(PersistenceArtifactKind.Settings, "settings.profile", _ => { });
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

static SnapshotPackage ValidSettingsPackage()
{
    var package = new SnapshotPackage
    {
        DomainId = "settings.profile",
        SnapshotSchemaVersion = 1,
        DomainState = SnapshotDomainState.Ready,
    };
    package.ContentDomainVersions["settings.profile"] = "1";
    package.Payload["volume"] = 0.8;
    return package;
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
            },
        };
    }

    public IReadOnlyDictionary<string, string>? GetSelectedRoute()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "storm-cut-01",
            ["name"] = "风暴走廊",
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
            ["searched"] = "1",
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
