using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Epic #16 Story 001: Screen State Machine & Screen Flow ===");
var failed = 0;
var total = 0;

Run("AC-1: Initialize enters HUB and renders S1 Hub HUD", Ac1InitializeHub);
Run("AC-2: Hub gangway/helm use opens S3 while staying on HUB", Ac2HubDepartureModal);
Run("AC-3: Hub departure confirm enters locked state and closes panels", Ac3DepartureLocked);
Run("AC-4: Departure lock timer opens chart fullscreen", Ac4LockTimerOpensChart);
Run("AC-5: M key opens chart from unlocked Hub", Ac5MapKeyOpensChart);
Run("AC-6: Route selection expands side panel and focuses confirm", Ac6RouteSelected);
Run("AC-7: Route departure confirm starts ink and gate lock", Ac7ChartDepartureConfirmed);
Run("AC-8: Chart lock complete enters voyage and hides chart", Ac8ChartLockToVoyage);
Run("AC-9: Esc from chart states returns to Hub", Ac9EscReturnsHub);
Run("AC-10: Encounter context ready enters exploration HUD", Ac10VoyageToExploration);
Run("AC-11: Extraction start enters extracting with S6b", Ac11ExtractionStarted);
Run("AC-12: Extraction complete enters settlement with S6c", Ac12ExtractionComplete);
Run("AC-13: Settlement confirmed enters Hub arriving", Ac13SettlementConfirmed);
Run("AC-14: Arrival with naming eligible returns Hub and opens S10", Ac14ArrivalNamingEligible);
Run("AC-15: Arrival without naming returns Hub only", Ac15ArrivalNoNaming);
Run("AC-16: Departure lock rejects screen, modal, and M key requests", Ac16DepartureLockGuards);
Run("AC-17: Esc cannot reverse chart departure confirmed", Ac17ConfirmedIrreversible);
Run("AC-18: Esc cannot cancel extracting", Ac18ExtractingEscBlocked);
Run("AC-19: S1-S12 registry contains expected types and owners", Ac19ScreenRegistry);
Run("AC-20: S2 non-modal panel does not block movement", Ac20NonModalMovement);
Run("AC-21: Runtime UI aggregate registry is not used as production spec coverage", Ac21NoRuntimeUiAggregateRegistry);

if (failed > 0)
{
    Console.Error.WriteLine($"Story 001 validation failed: {failed}/{total} checks failed.");
    return 1;
}

Console.WriteLine($"Story 001 validation passed: {total}/{total} checks passed.");
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

static bool Ac1InitializeHub()
{
    var ui = CreateUi();
    return ui.CurrentScreen == Screen.Hub
        && ui.HubHudVisible
        && !ui.ChartVisible;
}

static bool Ac2HubDepartureModal()
{
    var gangway = CreateUi();
    var helm = CreateUi();

    return gangway.UseGangway() == ModalResult.Success
        && gangway.CurrentScreen == Screen.Hub
        && gangway.CurrentModalId == UIManager.DepartureConfirmScreenId
        && helm.UseHelm() == ModalResult.Success
        && helm.CurrentScreen == Screen.Hub
        && helm.CurrentModalId == UIManager.DepartureConfirmScreenId;
}

static bool Ac3DepartureLocked()
{
    var ui = CreateUi();
    ui.UseGangway();

    return ui.ConfirmDeparture() == ScreenResult.Success
        && ui.CurrentScreen == Screen.DepartureLocked
        && ui.DepartureLocked
        && Math.Abs(ui.DepartureLockRemainingSeconds - 2.0) < 0.001
        && !ui.IsModalOpen()
        && !ui.IsPanelVisible(UIManager.DepartureConfirmScreenId)
        && !ui.HubHudVisible;
}

static bool Ac4LockTimerOpensChart()
{
    var ui = LockedUi();

    return ui.CompleteDepartureLockTimer() == ScreenResult.Success
        && ui.CurrentScreen == Screen.Chart
        && !ui.DepartureLocked
        && ui.ChartVisible;
}

static bool Ac5MapKeyOpensChart()
{
    var ui = CreateUi();

    return ui.PressMapKey() == ScreenResult.Success
        && ui.CurrentScreen == Screen.Chart
        && ui.ChartVisible;
}

static bool Ac6RouteSelected()
{
    var ui = ChartUi();

    return ui.SelectRoute("route.sky-reef") == ScreenResult.Success
        && ui.CurrentScreen == Screen.ChartRouteSelected
        && ui.RouteSidePanelExpanded
        && ui.DepartureConfirmButtonFocused;
}

static bool Ac7ChartDepartureConfirmed()
{
    var ui = RouteSelectedUi();

    return ui.ConfirmDeparture() == ScreenResult.Success
        && ui.CurrentScreen == Screen.ChartDepartureConfirmed
        && ui.InkDiffusionStarted
        && ui.DepartureGateLocked;
}

static bool Ac8ChartLockToVoyage()
{
    var ui = ChartDepartureConfirmedUi();

    return ui.CompleteChartLock() == ScreenResult.Success
        && ui.CurrentScreen == Screen.Voyage
        && !ui.ChartVisible
        && ui.BlackScreenTransitionStarted;
}

static bool Ac9EscReturnsHub()
{
    var chart = ChartUi();
    var selected = RouteSelectedUi();

    return chart.PressEscape() == ScreenResult.Success
        && chart.CurrentScreen == Screen.Hub
        && !chart.ChartVisible
        && selected.PressEscape() == ScreenResult.Success
        && selected.CurrentScreen == Screen.Hub
        && !selected.ChartVisible;
}

static bool Ac10VoyageToExploration()
{
    var ui = VoyageUi();

    return ui.EncounterContextReady() == ScreenResult.Success
        && ui.CurrentScreen == Screen.Exploration
        && ui.ExplorationHudVisible;
}

static bool Ac11ExtractionStarted()
{
    var ui = ExplorationUi();

    return ui.ExtractionStarted() == ScreenResult.Success
        && ui.CurrentScreen == Screen.Extracting
        && ui.IsPanelVisible(UIManager.ExtractionProgressScreenId)
        && ui.ActiveInputLayer == InputLayer.SemiModal;
}

static bool Ac12ExtractionComplete()
{
    var ui = ExtractingUi();

    return ui.ExtractionComplete() == ScreenResult.Success
        && ui.CurrentScreen == Screen.Settlement
        && ui.CurrentModalId == UIManager.SettlementSummaryScreenId
        && ui.IsPanelVisible(UIManager.SettlementSummaryScreenId);
}

static bool Ac13SettlementConfirmed()
{
    var ui = SettlementUi();

    return ui.SettlementConfirmed() == ScreenResult.Success
        && ui.CurrentScreen == Screen.HubArriving
        && !ui.IsModalOpen();
}

static bool Ac14ArrivalNamingEligible()
{
    var ui = HubArrivingUi();

    return ui.ArrivalComplete(namingEligible: true) == ScreenResult.Success
        && ui.CurrentScreen == Screen.Hub
        && ui.HubHudVisible
        && ui.CurrentModalId == UIManager.NamingScreenId;
}

static bool Ac15ArrivalNoNaming()
{
    var ui = HubArrivingUi();

    return ui.ArrivalComplete(namingEligible: false) == ScreenResult.Success
        && ui.CurrentScreen == Screen.Hub
        && ui.HubHudVisible
        && !ui.IsModalOpen();
}

static bool Ac16DepartureLockGuards()
{
    var ui = LockedUi();

    return ui.OpenScreen(Screen.Chart) == ScreenResult.ErrDepartureLocked
        && ui.OpenModalPanel(UIManager.RepairScreenId) == ModalResult.ErrDepartureLocked
        && ui.PressMapKey() == ScreenResult.ErrDepartureLocked
        && !ui.ChartVisible
        && !ui.HubHudVisible
        && !ui.IsModalOpen();
}

static bool Ac17ConfirmedIrreversible()
{
    var ui = ChartDepartureConfirmedUi();

    return ui.PressEscape() == ScreenResult.ErrInvalidScreen
        && ui.CurrentScreen == Screen.ChartDepartureConfirmed
        && ui.ChartVisible;
}

static bool Ac18ExtractingEscBlocked()
{
    var ui = ExtractingUi();

    return ui.PressEscape() == ScreenResult.ErrInvalidScreen
        && ui.CurrentScreen == Screen.Extracting
        && ui.IsPanelVisible(UIManager.ExtractionProgressScreenId);
}

static bool Ac19ScreenRegistry()
{
    var ui = CreateUi();
    var expected = new Dictionary<string, (ScreenType Type, string Owner)>
    {
        [UIManager.HubHudScreenId] = (ScreenType.HudOverlay, "hub"),
        [UIManager.StationDetailScreenId] = (ScreenType.NonModal, "hub"),
        [UIManager.DepartureConfirmScreenId] = (ScreenType.Modal, "hub"),
        [UIManager.ChartScreenId] = (ScreenType.Fullscreen, "chart"),
        [UIManager.ExplorationHudScreenId] = (ScreenType.HudOverlay, "exploration"),
        [UIManager.CapacityChoiceScreenId] = (ScreenType.Modal, "resources"),
        [UIManager.ExtractionProgressScreenId] = (ScreenType.SemiModal, "exploration"),
        [UIManager.SettlementSummaryScreenId] = (ScreenType.Modal, "exploration"),
        [UIManager.CombatScreenId] = (ScreenType.Modal, "combat"),
        [UIManager.RepairScreenId] = (ScreenType.Modal, "world-repair"),
        [UIManager.MarketScreenId] = (ScreenType.Modal, "settlement"),
        [UIManager.NamingScreenId] = (ScreenType.Modal, "partner"),
        [UIManager.PartnerSniffScreenId] = (ScreenType.NonModal, "partner"),
        [UIManager.StorageScreenId] = (ScreenType.NonModal, "resources"),
    };

    return expected.All(item =>
        ui.ScreenRegistry.TryGetValue(item.Key, out var definition)
        && definition.Type == item.Value.Type
        && definition.OwnerSystem == item.Value.Owner);
}

static bool Ac20NonModalMovement()
{
    var ui = CreateUi();

    return ui.OpenNonModal(UIManager.StationDetailScreenId) == ScreenResult.Success
        && ui.ActiveInputLayer == InputLayer.NonModal
        && !ui.IsMovementInputBlocked();
}

static bool Ac21NoRuntimeUiAggregateRegistry()
{
    var specPath = Path.Combine(FindProjectRoot(), "production", "ui-specs", "runtime-ui-surface-registry.md");
    var readmePath = Path.Combine(FindProjectRoot(), "production", "ui-specs", "README.md");
    var readme = File.ReadAllText(readmePath);

    return !File.Exists(specPath)
        && !readme.Contains("runtime-ui-surface-registry.md", StringComparison.Ordinal)
        && readme.Contains("不得创建“当前运行时 UI 总表”", StringComparison.Ordinal)
        && readme.Contains("ui-spec-template.md", StringComparison.Ordinal)
        && readme.Contains("content-creation-review-gate.md", StringComparison.Ordinal);
}

static UIManager CreateUi()
{
    var ui = new UIManager();
    ui.Initialize();
    return ui;
}

static UIManager LockedUi()
{
    var ui = CreateUi();
    ui.UseGangway();
    ui.ConfirmDeparture();
    return ui;
}

static UIManager ChartUi()
{
    var ui = CreateUi();
    ui.PressMapKey();
    return ui;
}

static UIManager RouteSelectedUi()
{
    var ui = ChartUi();
    ui.SelectRoute("route.sky-reef");
    return ui;
}

static UIManager ChartDepartureConfirmedUi()
{
    var ui = RouteSelectedUi();
    ui.ConfirmDeparture();
    return ui;
}

static UIManager VoyageUi()
{
    var ui = ChartDepartureConfirmedUi();
    ui.CompleteChartLock();
    return ui;
}

static UIManager ExplorationUi()
{
    var ui = VoyageUi();
    ui.EncounterContextReady();
    return ui;
}

static UIManager ExtractingUi()
{
    var ui = ExplorationUi();
    ui.ExtractionStarted();
    return ui;
}

static UIManager SettlementUi()
{
    var ui = ExtractingUi();
    ui.ExtractionComplete();
    return ui;
}

static UIManager HubArrivingUi()
{
    var ui = SettlementUi();
    ui.SettlementConfirmed();
    return ui;
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
