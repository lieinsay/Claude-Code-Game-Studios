using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #16 Story 005: Animation Timing & Downstream Semantic Events ===");
var failed = 0;
var total = 0;

Run("AC-1: non-modal open uses 0.25s ease-out parchment tween", Ac1NonModalOpenTween);
Run("AC-2: non-modal close uses 0.15s ease-in parchment tween", Ac2NonModalCloseTween);
Run("AC-3: route selection pulses warm outline 1px to 3px to 1px", Ac3RoutePulse);
Run("AC-4: departure confirmation plays GPU ink-spread shader progress", Ac4InkSpreadShader);
Run("AC-5: ink completion triggers 1.2s linear gate seal", Ac5GateSeal);
Run("AC-6: extraction progress runs 2.5s linear with live text", Ac6ExtractionProgress);
Run("AC-7: repair toast enters, dwells, exits, and auto-removes", Ac7RepairToast);
Run("AC-8: settlement summary enters with 0.5s ease-out tween", Ac8SettlementSummaryEnter);
Run("AC-9: naming modal pops with 0.3s ease-out tween", Ac9NamingPop);
Run("AC-10: departure lock kills active non-modal tween and applies closed final state", Ac10DepartureLockKillsTween);
Run("AC-11: threat interruption kills extraction progress and flashes red", Ac11ThreatInterruptsExtraction);
Run("AC-12: parchment panels use 256px NinePatchRect source texture", Ac12NinePatchTexture);
Run("AC-13: route selected emits ui_route_selected after state change", Ac13RouteSelectedEvent);
Run("AC-14: departure confirmed emits ui_departure_confirmed after state change", Ac14DepartureConfirmedEvent);
Run("AC-15: threat response emits ui_threat_response_chosen", Ac15ThreatResponseEvent);
Run("AC-16: successful repair submission emits ui_repair_submitted", Ac16RepairSubmittedEvent);
Run("AC-17: successful purchase emits ui_purchase_confirmed", Ac17PurchaseConfirmedEvent);
Run("AC-18: item transfer emits ui_item_transferred", Ac18ItemTransferredEvent);
Run("AC-19: partner naming emits ui_naming_confirmed", Ac19NamingConfirmedEvent);
Run("AC-20: settlement close emits ui_settlement_closed", Ac20SettlementClosedEvent);
Run("AC-21: panel open and close emit ui_panel_opened/ui_panel_closed", Ac21PanelOpenCloseEvents);
Run("AC-22: all ui_* contracts are typed, synchronous, and dictionary-free", Ac22SignalContractCompliance);
Run("AC-23: panel opened semantic cascade depth stays at two or less", Ac23CascadeDepth);

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

static bool Ac1NonModalOpenTween()
{
    var ui = CreateUiWithDriver(out var driver);
    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    var animation = DriverAnimation(driver, $"{UIManager.PanelOpenAnimationIdPrefix}{UIManager.PartnerSniffScreenId}");

    return animation.TargetId == UIManager.PartnerSniffScreenId
        && Animation(ui, animation.AnimationId) == animation
        && animation.UsesSceneTreeTween
        && !animation.UsesManualProcessInterpolation
        && Math.Abs(animation.DurationSeconds - UIManager.PanelOpenAnimationSeconds) < 0.001
        && animation.Easing == UiAnimationEasing.EaseOut
        && HasTween(animation, "scale", "0.9", "1.0")
        && HasTween(animation, "modulate.a", "0", "1");
}

static bool Ac2NonModalCloseTween()
{
    var ui = CreateUiWithDriver(out var driver);
    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    ui.CloseNonModal(UIManager.PartnerSniffScreenId);
    var animation = DriverAnimation(driver, $"{UIManager.PanelCloseAnimationIdPrefix}{UIManager.PartnerSniffScreenId}");

    return Animation(ui, animation.AnimationId) == animation
        && animation.UsesSceneTreeTween
        && !animation.UsesManualProcessInterpolation
        && Math.Abs(animation.DurationSeconds - UIManager.PanelCloseAnimationSeconds) < 0.001
        && animation.Easing == UiAnimationEasing.EaseIn
        && HasTween(animation, "scale", "1.0", "0.9")
        && HasTween(animation, "modulate.a", "1", "0");
}

static bool Ac3RoutePulse()
{
    var ui = ChartUiWithDriver(out var driver);
    ui.SelectRoute("storm-cut-01");
    var animation = DriverAnimation(driver, UIManager.RoutePulseAnimationId);

    return animation.TargetId == "storm-cut-01"
        && Animation(ui, animation.AnimationId) == animation
        && Math.Abs(animation.DurationSeconds - UIManager.RoutePulseAnimationSeconds) < 0.001
        && animation.Easing == UiAnimationEasing.EaseInOut
        && HasTween(animation, "outline_width", "1px", "3px")
        && HasTween(animation, "outline_width", "3px", "1px");
}

static bool Ac4InkSpreadShader()
{
    var ui = RouteSelectedUiWithDriver(out var driver);
    ui.ConfirmDeparture();
    var animation = DriverAnimation(driver, UIManager.InkSpreadAnimationId);

    return Animation(ui, animation.AnimationId) == animation
        && animation.UsesSceneTreeTween
        && animation.UsesShaderMaterial
        && !animation.UsesManualProcessInterpolation
        && animation.ShaderUniformName == UIManager.InkProgressShaderUniform
        && animation.ShaderUniformWritesPerFrame == 1
        && Math.Abs(animation.DurationSeconds - UIManager.InkSpreadAnimationSeconds) < 0.001
        && animation.Easing == UiAnimationEasing.EaseOut
        && HasTween(animation, "shader.progress", "0", "1");
}

static bool Ac5GateSeal()
{
    var ui = RouteSelectedUiWithDriver(out var driver);
    ui.ConfirmDeparture();
    var result = ui.CompleteInkSpread();
    var animation = DriverAnimation(driver, UIManager.DepartureGateSealAnimationId);

    return result == ScreenResult.Success
        && Animation(ui, animation.AnimationId) == animation
        && animation.UsesSceneTreeTween
        && Math.Abs(animation.DurationSeconds - UIManager.DepartureGateSealAnimationSeconds) < 0.001
        && animation.Easing == UiAnimationEasing.Linear
        && HasTween(animation, "departure_gate_seal", "open", "sealed");
}

static bool Ac6ExtractionProgress()
{
    var ui = ExplorationUiWithDriver(out var driver);
    ui.ExtractionStarted();
    var animation = DriverAnimation(driver, UIManager.ExtractionProgressAnimationId);

    return Animation(ui, animation.AnimationId) == animation
        && animation.UsesSceneTreeTween
        && Math.Abs(animation.DurationSeconds - UIManager.ExtractionProgressAnimationSeconds) < 0.001
        && animation.Easing == UiAnimationEasing.Linear
        && animation.ProgressTextRealtime
        && HasTween(animation, "progress_percent", "0", "100");
}

static bool Ac7RepairToast()
{
    var ui = CreateUiWithDriver(out var driver);
    ui.OpenRepairPanelForNode("beacon_02");
    ui.SubmitRepairMaterials("beacon_02", new Dictionary<string, int>(StringComparer.Ordinal) { ["item.saltcloth"] = 2 });
    var animation = DriverAnimation(driver, UIManager.RepairToastAnimationId);

    return Animation(ui, animation.AnimationId) == animation
        && animation.UsesSceneTreeTween
        && animation.AutoRemovesOnFinished
        && Math.Abs(animation.DwellSeconds - UIManager.ToastDwellSeconds) < 0.001
        && HasTween(animation, "position.y", "below", "rest")
        && HasTween(animation, "modulate.a", "0", "1");
}

static bool Ac8SettlementSummaryEnter()
{
    var ui = ExtractingUiWithDriver(out var driver);
    ui.ExtractionComplete();
    var animation = DriverAnimation(driver, UIManager.SettlementSummaryEnterAnimationId);

    return ui.CurrentModalId == UIManager.SettlementSummaryScreenId
        && Animation(ui, animation.AnimationId) == animation
        && animation.Easing == UiAnimationEasing.EaseOut
        && Math.Abs(animation.DurationSeconds - UIManager.SettlementSummaryEnterAnimationSeconds) < 0.001;
}

static bool Ac9NamingPop()
{
    var ui = HubArrivingUiWithDriver(out var driver);
    ui.ArrivalComplete(namingEligible: true);
    var animation = DriverAnimation(driver, UIManager.NamingModalPopAnimationId);

    return ui.CurrentModalId == UIManager.NamingScreenId
        && Animation(ui, animation.AnimationId) == animation
        && animation.Easing == UiAnimationEasing.EaseOut
        && Math.Abs(animation.DurationSeconds - UIManager.NamingModalPopAnimationSeconds) < 0.001
        && HasTween(animation, "scale", "0.9", "1.0");
}

static bool Ac10DepartureLockKillsTween()
{
    var ui = CreateUiWithDriver(out var driver);
    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    ui.UseGangway();
    ui.ConfirmDeparture();
    var animationId = $"{UIManager.PanelOpenAnimationIdPrefix}{UIManager.PartnerSniffScreenId}";
    var killed = Animation(ui, animationId);
    var interruption = ui.LastAnimationInterruptionSnapshot;

    return killed.IsKilled
        && DriverKilled(driver, animationId, applyFinalState: true)
        && interruption is not null
        && interruption.ExistingTweenKilled
        && interruption.FinalStateApplied
        && Math.Abs(interruption.FinalOpacity) < 0.001
        && !interruption.FinalVisible
        && Math.Abs(interruption.FinalScale - 0.9) < 0.001
        && !ui.IsPanelVisible(UIManager.PartnerSniffScreenId);
}

static bool Ac11ThreatInterruptsExtraction()
{
    var ui = ExplorationUiWithDriver(out var driver);
    ui.ExtractionStarted();
    var result = ui.ThreatTriggeredDuringExtraction("threat.riftshade");
    var killed = Animation(ui, UIManager.ExtractionProgressAnimationId);
    var interruption = ui.LastAnimationInterruptionSnapshot;

    return result == ScreenResult.Success
        && killed.IsKilled
        && DriverKilled(driver, UIManager.ExtractionProgressAnimationId, applyFinalState: false)
        && interruption is not null
        && interruption.ExistingTweenKilled
        && interruption.FlashCount == UIManager.ThreatInterruptionFlashCount
        && Math.Abs(interruption.FlashSecondsPerBlink - UIManager.ThreatInterruptionFlashSeconds) < 0.001
        && interruption.FlashColorHex == UIManager.DangerRedHex
        && !ui.IsPanelVisible(UIManager.ExtractionProgressScreenId);
}

static bool Ac12NinePatchTexture()
{
    var ui = CreateUiWithDriver(out var driver);
    var expectedPanelIds = ui.ScreenRegistry.Values
        .Where(definition => definition.Type == ScreenType.NonModal)
        .Select(definition => definition.Id)
        .Append(UIManager.ChartScreenId)
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToArray();
    var contracts = ui.ParchmentPanelTextureContracts
        .OrderBy(contract => contract.PanelId, StringComparer.Ordinal)
        .ToArray();
    var configuredContracts = driver.ConfiguredTextures
        .OrderBy(contract => contract.PanelId, StringComparer.Ordinal)
        .ToArray();
    var unknown = ui.GetPanelTextureContract(UIManager.HubHudScreenId);

    return contracts.Select(contract => contract.PanelId).SequenceEqual(expectedPanelIds)
        && configuredContracts.SequenceEqual(contracts)
        && contracts.All(IsParchmentNinePatch)
        && unknown.UsesFullSizeTexture
        && !unknown.UsesNinePatchRect;
}

static bool Ac13RouteSelectedEvent()
{
    var ui = ChartUi();
    var captured = new List<(string RouteId, string RouteName)>();
    ui.UIRouteSelected += (routeId, routeName) => captured.Add((routeId, routeName));
    ui.SelectRoute("storm-cut-01");
    var emitted = Event(ui, UIManager.UIRouteSelectedEventId);

    return ui.CurrentScreen == Screen.ChartRouteSelected
        && captured.SequenceEqual(new[] { ("storm-cut-01", "风暴走廊") })
        && emitted.Arguments.SequenceEqual(new[] { "storm-cut-01", "风暴走廊" })
        && emitted.EmittedAfterAction;
}

static bool Ac14DepartureConfirmedEvent()
{
    var ui = RouteSelectedUi();
    ui.ConfirmDeparture();
    var emitted = Event(ui, UIManager.UIDepartureConfirmedEventId);

    return ui.CurrentScreen == Screen.ChartDepartureConfirmed
        && emitted.Arguments.SequenceEqual(new[] { "storm-cut-01", "chart" })
        && emitted.EmittedAfterAction;
}

static bool Ac15ThreatResponseEvent()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.CombatScreenId);
    var result = ui.ResolveThreatResponse("threat.riftshade", "suppressed");
    var emitted = Event(ui, UIManager.UIThreatResponseChosenEventId);

    return result == ModalResult.Success
        && emitted.Arguments.SequenceEqual(new[] { "threat.riftshade", "suppressed" })
        && emitted.EmittedAfterAction;
}

static bool Ac16RepairSubmittedEvent()
{
    var ui = CreateUi();
    ui.OpenRepairPanelForNode("beacon_02");
    ui.SubmitRepairMaterials("beacon_02", new Dictionary<string, int>(StringComparer.Ordinal) { ["item.saltcloth"] = 2 });
    var emitted = Event(ui, UIManager.UIRepairSubmittedEventId);

    return emitted.Arguments.SequenceEqual(new[] { "beacon_02", "item.saltcloth:2" })
        && emitted.EmittedAfterAction;
}

static bool Ac17PurchaseConfirmedEvent()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.MarketScreenId, new Dictionary<string, string>(StringComparer.Ordinal) { ["stall_id"] = "stall_weaver" });
    ui.ConfirmPurchase("stall_weaver", "good.rope", 2, 14);
    var emitted = Event(ui, UIManager.UIPurchaseConfirmedEventId);

    return emitted.Arguments.SequenceEqual(new[] { "stall_weaver", "good.rope", "2", "14" })
        && emitted.EmittedAfterAction;
}

static bool Ac18ItemTransferredEvent()
{
    var ui = CreateUi();
    ui.ConfirmCapacityTransfer("item.saltcloth", UIManager.CarriedPoolId, UIManager.StoragePoolId, 1);
    var emitted = Event(ui, UIManager.UIItemTransferredEventId);

    return emitted.Arguments.SequenceEqual(new[] { "item.saltcloth", "CARRIED", "STORAGE", "1" })
        && emitted.EmittedAfterAction;
}

static bool Ac19NamingConfirmedEvent()
{
    var ui = CreateUi();
    ui.OpenModalPanel(UIManager.NamingScreenId);
    ui.SubmitPartnerName("partner.sky-cat", "小云");
    var emitted = Event(ui, UIManager.UINamingConfirmedEventId);

    return emitted.Arguments.SequenceEqual(new[] { "partner.sky-cat", "小云" })
        && emitted.EmittedAfterAction;
}

static bool Ac20SettlementClosedEvent()
{
    var ui = ExtractingUi();
    ui.ExtractionComplete();
    ui.CloseSettlementSummary("voyage.001", new[] { "item.saltcloth" }, new[] { "intel.storm" });
    var emitted = Event(ui, UIManager.UISettlementClosedEventId);

    return emitted.Arguments.SequenceEqual(new[] { "voyage.001", "item.saltcloth", "intel.storm" })
        && emitted.EmittedAfterAction
        && !ui.IsModalOpen();
}

static bool Ac21PanelOpenCloseEvents()
{
    var ui = CreateUi();
    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    ui.CloseNonModal(UIManager.PartnerSniffScreenId);
    var opened = Event(ui, UIManager.UIPanelOpenedEventId);
    var closed = Event(ui, UIManager.UIPanelClosedEventId);

    return opened.Arguments.SequenceEqual(new[] { UIManager.PartnerSniffScreenId })
        && closed.Arguments.SequenceEqual(new[] { UIManager.PartnerSniffScreenId })
        && opened.EmittedAfterAction
        && closed.EmittedAfterAction;
}

static bool Ac22SignalContractCompliance()
{
    var ui = CreateUi();
    var expected = new[]
    {
        UIManager.UIRouteSelectedEventId,
        UIManager.UIDepartureConfirmedEventId,
        UIManager.UIThreatResponseChosenEventId,
        UIManager.UIRepairSubmittedEventId,
        UIManager.UIPurchaseConfirmedEventId,
        UIManager.UIItemTransferredEventId,
        UIManager.UINamingConfirmedEventId,
        UIManager.UISettlementClosedEventId,
        UIManager.UIPanelOpenedEventId,
        UIManager.UIPanelClosedEventId,
    };

    return expected.All(eventId =>
    {
        var contract = ui.GetSemanticEventContract(eventId);
        return contract is not null
            && contract.ParameterTypeNames.Count > 0
            && contract.ParameterTypeNames.All(typeName => !string.Equals(typeName, "Dictionary", StringComparison.Ordinal))
            && !contract.UsesDictionaryPayload
            && contract.EmitsSynchronously;
    });
}

static bool Ac23CascadeDepth()
{
    var ui = CreateUi();
    var observedDepth = 0;
    ui.UIPanelOpened += _ =>
    {
        using var feedbackScope = ui.EnterSemanticEventConsumerScope(UIManager.UIPanelOpenedEventId, "feedback");
        observedDepth = Math.Max(observedDepth, Event(ui, UIManager.UIPanelOpenedEventId).CascadeDepth);
        using var terminalScope = ui.EnterSemanticEventConsumerScope(UIManager.UIPanelOpenedEventId, "feedback.consumer");
        observedDepth = Math.Max(observedDepth, Event(ui, UIManager.UIPanelOpenedEventId).CascadeDepth);
    };

    ui.OpenNonModal(UIManager.PartnerSniffScreenId);
    var emitted = Event(ui, UIManager.UIPanelOpenedEventId);
    var contract = ui.GetSemanticEventContract(UIManager.UIPanelOpenedEventId);

    return emitted.CascadeDepth == UIManager.MaxSemanticCascadeDepth
        && observedDepth == UIManager.MaxSemanticCascadeDepth
        && contract is not null
        && contract.MaxCascadeDepth == UIManager.MaxSemanticCascadeDepth;
}

static bool HasTween(UiAnimationSnapshot animation, string propertyName, string fromValue, string toValue)
{
    return animation.PropertyTweens.Any(tween =>
        tween.PropertyName == propertyName
        && tween.FromValue == fromValue
        && tween.ToValue == toValue);
}

static bool IsParchmentNinePatch(PanelTextureContractSnapshot contract)
{
    return contract.UsesNinePatchRect
        && contract.SourceTextureWidth == UIManager.ParchmentTextureSourceSize
        && contract.SourceTextureHeight == UIManager.ParchmentTextureSourceSize
        && !contract.UsesFullSizeTexture;
}

static UiAnimationSnapshot Animation(UIManager ui, string animationId)
{
    return ui.GetAnimationSnapshot(animationId)
        ?? throw new InvalidOperationException($"Missing animation snapshot: {animationId}");
}

static UiAnimationSnapshot DriverAnimation(RecordingAnimationDriver driver, string animationId)
{
    return driver.PlayedTweens.LastOrDefault(animation => animation.AnimationId == animationId)
        ?? throw new InvalidOperationException($"Animation driver did not receive tween: {animationId}");
}

static bool DriverKilled(RecordingAnimationDriver driver, string animationId, bool applyFinalState)
{
    return driver.KilledTweens.Any(kill =>
        kill.AnimationId == animationId
        && kill.ApplyFinalState == applyFinalState);
}

static UiSemanticEventSnapshot Event(UIManager ui, string eventId)
{
    return ui.GetLatestSemanticEvent(eventId)
        ?? throw new InvalidOperationException($"Missing semantic event: {eventId}");
}

static UIManager CreateUi()
{
    return BuildUi(animationDriver: null);
}

static UIManager CreateUiWithDriver(out RecordingAnimationDriver animationDriver)
{
    animationDriver = new RecordingAnimationDriver();
    return BuildUi(animationDriver);
}

static UIManager BuildUi(RecordingAnimationDriver? animationDriver)
{
    var ui = new UIManager(new FakeUpstreamDataSource(), animationDriver);
    ui.Initialize();
    return ui;
}

static UIManager ChartUi()
{
    var ui = CreateUi();
    ui.PressMapKey();
    return ui;
}

static UIManager ChartUiWithDriver(out RecordingAnimationDriver animationDriver)
{
    var ui = CreateUiWithDriver(out animationDriver);
    ui.PressMapKey();
    return ui;
}

static UIManager RouteSelectedUi()
{
    var ui = ChartUi();
    ui.SelectRoute("storm-cut-01");
    return ui;
}

static UIManager RouteSelectedUiWithDriver(out RecordingAnimationDriver animationDriver)
{
    var ui = ChartUiWithDriver(out animationDriver);
    ui.SelectRoute("storm-cut-01");
    return ui;
}

static UIManager ExplorationUi()
{
    var ui = RouteSelectedUi();
    ui.ConfirmDeparture();
    ui.CompleteChartLock();
    ui.EncounterContextReady();
    return ui;
}

static UIManager ExplorationUiWithDriver(out RecordingAnimationDriver animationDriver)
{
    var ui = RouteSelectedUiWithDriver(out animationDriver);
    ui.ConfirmDeparture();
    ui.CompleteChartLock();
    ui.EncounterContextReady();
    return ui;
}

static UIManager ExtractingUi()
{
    var ui = ExplorationUi();
    ui.ExtractionStarted();
    return ui;
}

static UIManager ExtractingUiWithDriver(out RecordingAnimationDriver animationDriver)
{
    var ui = ExplorationUiWithDriver(out animationDriver);
    ui.ExtractionStarted();
    return ui;
}

static UIManager HubArrivingUiWithDriver(out RecordingAnimationDriver animationDriver)
{
    var ui = ExtractingUiWithDriver(out animationDriver);
    ui.ExtractionComplete();
    ui.SettlementConfirmed();
    return ui;
}

sealed class RecordingAnimationDriver : IUiAnimationDriver
{
    public List<UiAnimationSnapshot> PlayedTweens { get; } = new();

    public List<(string AnimationId, bool ApplyFinalState)> KilledTweens { get; } = new();

    public List<PanelTextureContractSnapshot> ConfiguredTextures { get; } = new();

    public UiAnimationSnapshot PlayTween(UiAnimationSnapshot request)
    {
        PlayedTweens.Add(request);
        return request;
    }

    public UiAnimationSnapshot KillTween(UiAnimationSnapshot activeSnapshot, bool applyFinalState)
    {
        KilledTweens.Add((activeSnapshot.AnimationId, applyFinalState));
        return activeSnapshot with { IsKilled = true, FinalStateApplied = applyFinalState };
    }

    public PanelTextureContractSnapshot ConfigurePanelTexture(PanelTextureContractSnapshot contract)
    {
        ConfiguredTextures.Add(contract);
        return contract;
    }
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

    public IReadOnlyDictionary<string, string>? GetHullIntegrity() => null;

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetModuleStates() => Array.Empty<IReadOnlyDictionary<string, string>>();

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetCarriedInventory() => Array.Empty<IReadOnlyDictionary<string, string>>();

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

    public IReadOnlyDictionary<string, string>? GetSearchProgress() => null;

    public string? GetScoutPreviewLevel() => UIManager.ScoutPreviewPresence;

    public IReadOnlyDictionary<string, string>? GetExtractionState()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["extraction_progress"] = "0",
        };
    }

    public IReadOnlyDictionary<string, string>? BuildThreatContext()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["threat_id"] = "threat.riftshade",
        };
    }

    public IReadOnlyDictionary<string, string>? GetRepairState(string nodeId)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["node_id"] = nodeId,
        };
    }

    public IReadOnlyDictionary<string, string>? GetStallData(string stallId)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["stall_id"] = stallId,
        };
    }

    public string? QueryPartnerName() => "灰白猫";

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? GetSniffItems() => Array.Empty<IReadOnlyDictionary<string, string>>();

    public bool? NamingPromptEligibility() => true;

    public string? GetDisplayName(string entityId) => entityId;

    public string? GetDescription(string entityId) => $"description:{entityId}";

    public bool TransferItem(string itemId, string fromPool, string toPool, int quantity) => true;

    public bool DiscardItem(string itemId) => true;

    public bool SubmitRepair(string nodeId, IReadOnlyDictionary<string, int> materials) => true;

    public bool ExecutePurchase(string stallId, string goodId, int quantity, int totalCost) => true;

    public bool SubmitPartnerName(string partnerId, string name) => true;
}
