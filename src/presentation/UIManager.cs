using System;
using System.Collections.Generic;
using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Screen identifiers for the 12-screen FSM.
/// Numeric values intentionally match the legacy GDScript prototype.
/// </summary>
public enum Screen
{
    None = 0,
    Hub = 1,
    Chart = 2,
    ChartRouteSelected = 3,
    ChartDepartureConfirmed = 4,
    DepartureLocked = 5,
    Voyage = 6,
    Exploration = 7,
    Extracting = 8,
    Settlement = 9,
    HubArriving = 10,
    Combat = 11,
}

/// <summary>
/// Screen or panel classification used by UIManager's registry.
/// </summary>
public enum ScreenType
{
    HudOverlay,
    Fullscreen,
    Modal,
    SemiModal,
    NonModal,
}

/// <summary>
/// Result codes for screen state transitions.
/// </summary>
public enum ScreenResult
{
    Success = 0,
    ErrDepartureLocked = 1,
    ErrModalOpen = 2,
    ErrInvalidScreen = 3,
}

/// <summary>
/// Result codes for modal panel requests.
/// </summary>
public enum ModalResult
{
    Success = 0,
    ErrAnotherModalOpen = 1,
    ErrDepartureLocked = 2,
    ErrInvalidPanel = 3,
    ErrQueued = 4,
}

/// <summary>
/// Registered UI screen or panel metadata.
/// </summary>
public sealed record ScreenDefinition(string Id, ScreenType Type, string OwnerSystem);

/// <summary>
/// Input routing layer priorities.
/// </summary>
public enum InputLayer
{
    Modal = 0,
    SemiModal = 1,
    NonModal = 2,
    Hud = 3,
    World = 4,
}

/// <summary>
/// Owns 12-screen state machine, single-slot modal stack, 4-layer input routing.
/// Consumes data from all domain systems; does not own gameplay state.
/// </summary>
public sealed class UIManager
{
    public const string HubHudScreenId = "S1_hub_hud";
    public const string StationDetailScreenId = "S2_station_detail";
    public const string DepartureConfirmScreenId = "S3_departure_confirm";
    public const string ChartScreenId = "S4_chart";
    public const string ExplorationHudScreenId = "S5_exploration_hud";
    public const string CapacityChoiceScreenId = "S6a_capacity_choice";
    public const string ExtractionProgressScreenId = "S6b_extraction_progress";
    public const string SettlementSummaryScreenId = "S6c_settlement_summary";
    public const string CombatScreenId = "S7_combat";
    public const string RepairScreenId = "S8_repair";
    public const string MarketScreenId = "S9_market";
    public const string NamingScreenId = "S10_naming";
    public const string PartnerSniffScreenId = "S11_partner_sniff";
    public const string ToastScreenId = "S12_toast";
    public const string RegistryDiagnosticToolsPanelId = "registry_diagnostic_tools";

    private readonly List<string> modalStack = new();
    private readonly Queue<string> queuedModals = new();
    private readonly HashSet<string> visiblePanels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ScreenDefinition> screenRegistry = BuildScreenRegistry();
    private string modalPanel = string.Empty;
    private bool routeSidePanelExpanded;
    private bool legacyInitialHubTransitionPending;

    /// <summary>Raised when the active screen changes.</summary>
    public event Action<Screen, Screen>? ScreenChanged;

    /// <summary>Raised when the UI system is ready.</summary>
    public event Action? UIReady;

    /// <summary>Raised when a modal panel opens.</summary>
    public event Action<string>? UIPanelOpened;

    /// <summary>Raised when a modal panel closes.</summary>
    public event Action<string>? UIPanelClosed;

    /// <summary>Current active screen.</summary>
    public Screen CurrentScreen { get; private set; } = Screen.None;

    /// <summary>Current active input layer.</summary>
    public InputLayer ActiveInputLayer { get; private set; } = InputLayer.World;

    /// <summary>Whether the UI system has been initialized.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Whether departure lock is currently rejecting screen and panel requests.</summary>
    public bool DepartureLocked { get; private set; }

    /// <summary>Remaining simulated departure-lock seconds for deterministic tests.</summary>
    public double DepartureLockRemainingSeconds { get; private set; }

    /// <summary>Whether the Hub HUD overlay is visible.</summary>
    public bool HubHudVisible => visiblePanels.Contains(HubHudScreenId);

    /// <summary>Whether the chart fullscreen panel is visible.</summary>
    public bool ChartVisible => visiblePanels.Contains(ChartScreenId);

    /// <summary>Whether the exploration HUD overlay is visible.</summary>
    public bool ExplorationHudVisible => visiblePanels.Contains(ExplorationHudScreenId);

    /// <summary>Whether the route-selected side panel is expanded.</summary>
    public bool RouteSidePanelExpanded => routeSidePanelExpanded;

    /// <summary>Whether route confirmation focus was assigned after route selection.</summary>
    public bool DepartureConfirmButtonFocused { get; private set; }

    /// <summary>Whether the chart departure ink diffusion sequence has started.</summary>
    public bool InkDiffusionStarted { get; private set; }

    /// <summary>Whether the chart departure gate-lock animation has started.</summary>
    public bool DepartureGateLocked { get; private set; }

    /// <summary>Whether the voyage black-screen transition has started.</summary>
    public bool BlackScreenTransitionStarted { get; private set; }

    /// <summary>Registered screen and panel metadata keyed by stable screen ID.</summary>
    public IReadOnlyDictionary<string, ScreenDefinition> ScreenRegistry => screenRegistry;

    /// <summary>Returns the current modal ID, or an empty string when no modal is open.</summary>
    public string CurrentModalId => modalPanel;

    /// <summary>Initializes and emits UIReady.</summary>
    public void Initialize()
    {
        IsInitialized = true;
        TransitionTo(Screen.Hub, validate: false);
        visiblePanels.Add(HubHudScreenId);
        legacyInitialHubTransitionPending = true;
        UIReady?.Invoke();
    }

    /// <summary>Transitions to a new screen. Returns false if already on that screen.</summary>
    public bool TransitionScreen(Screen newScreen)
    {
        if (legacyInitialHubTransitionPending && newScreen == Screen.Hub && CurrentScreen == Screen.Hub)
        {
            legacyInitialHubTransitionPending = false;
            ScreenChanged?.Invoke(Screen.None, Screen.Hub);
            return true;
        }

        legacyInitialHubTransitionPending = false;
        return OpenScreen(newScreen) == ScreenResult.Success;
    }

    /// <summary>Requests a fullscreen screen transition using the Story 001 screen FSM guards.</summary>
    public ScreenResult OpenScreen(Screen newScreen)
    {
        if (DepartureLocked)
        {
            return ScreenResult.ErrDepartureLocked;
        }

        if (IsModalOpen())
        {
            return ScreenResult.ErrModalOpen;
        }

        return TransitionTo(newScreen, validate: true);
    }

    /// <summary>
    /// Opens a modal panel. Combat (S7_combat) overrides current modal;
    /// other panels are rejected if a modal is already open.
    /// </summary>
    public bool OpenModal(string panelId)
    {
        if (DepartureLocked)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(modalPanel))
        {
            if (panelId == CombatScreenId)
            {
                modalStack.Add(modalPanel);
            }
            else
            {
                return false;
            }
        }

        modalPanel = panelId;
        ActiveInputLayer = InputLayer.Modal;
        visiblePanels.Add(panelId);
        UIPanelOpened?.Invoke(panelId);
        return true;
    }

    /// <summary>
    /// Opens a modal panel with ADR-0012 guard semantics.
    /// </summary>
    public ModalResult OpenModalPanel(string panelId)
    {
        if (!string.IsNullOrEmpty(modalPanel))
        {
            if (panelId == CombatScreenId)
            {
                modalStack.Add(modalPanel);
            }
            else if (panelId == NamingScreenId)
            {
                queuedModals.Enqueue(panelId);
                return ModalResult.ErrQueued;
            }
            else
            {
                return ModalResult.ErrAnotherModalOpen;
            }
        }

        if (DepartureLocked)
        {
            return ModalResult.ErrDepartureLocked;
        }

        if (!screenRegistry.TryGetValue(panelId, out var definition) || definition.Type != ScreenType.Modal)
        {
            return ModalResult.ErrInvalidPanel;
        }

        modalPanel = panelId;
        ActiveInputLayer = InputLayer.Modal;
        visiblePanels.Add(panelId);
        UIPanelOpened?.Invoke(panelId);
        return ModalResult.Success;
    }

    /// <summary>Closes the current modal, restoring the previous one from stack if any.</summary>
    public void CloseModal()
    {
        var closedId = modalPanel;
        if (modalStack.Count > 0)
        {
            modalPanel = modalStack[^1];
            modalStack.RemoveAt(modalStack.Count - 1);
            visiblePanels.Add(modalPanel);
        }
        else
        {
            modalPanel = string.Empty;
            if (queuedModals.Count > 0)
            {
                var queued = queuedModals.Dequeue();
                modalPanel = queued;
                visiblePanels.Add(queued);
                ActiveInputLayer = InputLayer.Modal;
            }
            else
            {
                ActiveInputLayer = visiblePanels.Contains(ExtractionProgressScreenId)
                    ? InputLayer.SemiModal
                    : ActiveInputLayerForNonModalOrWorld();
            }
        }

        visiblePanels.Remove(closedId);
        UIPanelClosed?.Invoke(closedId);
    }

    /// <summary>Returns whether a modal panel is currently open.</summary>
    public bool IsModalOpen()
    {
        return !string.IsNullOrEmpty(modalPanel);
    }

    /// <summary>Handles Hub gangway Use and opens the departure confirmation modal.</summary>
    public ModalResult UseGangway()
    {
        return CurrentScreen == Screen.Hub
            ? OpenModalPanel(DepartureConfirmScreenId)
            : ModalResult.ErrInvalidPanel;
    }

    /// <summary>Handles Hub helm Use and opens the departure confirmation modal.</summary>
    public ModalResult UseHelm()
    {
        return UseGangway();
    }

    /// <summary>Handles the M key shortcut to open chart from Hub.</summary>
    public ScreenResult PressMapKey()
    {
        if (DepartureLocked)
        {
            return ScreenResult.ErrDepartureLocked;
        }

        if (IsModalOpen() || CurrentScreen != Screen.Hub)
        {
            return ScreenResult.ErrModalOpen;
        }

        return OpenScreen(Screen.Chart);
    }

    /// <summary>Handles Escape according to Story 001 screen guards.</summary>
    public ScreenResult PressEscape()
    {
        if (CurrentScreen is Screen.Chart or Screen.ChartRouteSelected)
        {
            routeSidePanelExpanded = false;
            DepartureConfirmButtonFocused = false;
            return TransitionTo(Screen.Hub, validate: true);
        }

        if (CurrentScreen is Screen.ChartDepartureConfirmed or Screen.Extracting)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        if (IsModalOpen())
        {
            CloseModal();
            return ScreenResult.Success;
        }

        return ScreenResult.ErrInvalidScreen;
    }

    /// <summary>Confirms departure from either the Hub modal or the chart route-selected state.</summary>
    public ScreenResult ConfirmDeparture()
    {
        if (CurrentScreen == Screen.Hub && modalPanel == DepartureConfirmScreenId)
        {
            EnterDepartureLocked();
            return ScreenResult.Success;
        }

        if (CurrentScreen == Screen.ChartRouteSelected)
        {
            InkDiffusionStarted = true;
            DepartureGateLocked = true;
            return TransitionTo(Screen.ChartDepartureConfirmed, validate: true);
        }

        return ScreenResult.ErrInvalidScreen;
    }

    /// <summary>Completes the 2.0s Hub departure lock and opens the chart screen.</summary>
    public ScreenResult CompleteDepartureLockTimer()
    {
        if (CurrentScreen != Screen.DepartureLocked)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        DepartureLocked = false;
        DepartureLockRemainingSeconds = 0;
        return TransitionTo(Screen.Chart, validate: false);
    }

    /// <summary>Marks a chart route selected and expands the route side panel.</summary>
    public ScreenResult SelectRoute(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId) || CurrentScreen != Screen.Chart)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        routeSidePanelExpanded = true;
        DepartureConfirmButtonFocused = true;
        return TransitionTo(Screen.ChartRouteSelected, validate: true);
    }

    /// <summary>Completes the chart departure lock and starts voyage transition.</summary>
    public ScreenResult CompleteChartLock()
    {
        if (CurrentScreen != Screen.ChartDepartureConfirmed)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        BlackScreenTransitionStarted = true;
        return TransitionTo(Screen.Voyage, validate: true);
    }

    /// <summary>Moves from voyage into exploration when the encounter context is ready.</summary>
    public ScreenResult EncounterContextReady()
    {
        return CurrentScreen == Screen.Voyage
            ? TransitionTo(Screen.Exploration, validate: true)
            : ScreenResult.ErrInvalidScreen;
    }

    /// <summary>Starts extraction and displays the semi-modal progress panel.</summary>
    public ScreenResult ExtractionStarted()
    {
        if (CurrentScreen != Screen.Exploration)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        visiblePanels.Add(ExtractionProgressScreenId);
        ActiveInputLayer = InputLayer.SemiModal;
        return TransitionTo(Screen.Extracting, validate: true);
    }

    /// <summary>Completes extraction and opens the settlement summary modal.</summary>
    public ScreenResult ExtractionComplete()
    {
        if (CurrentScreen != Screen.Extracting)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        visiblePanels.Remove(ExtractionProgressScreenId);
        var result = TransitionTo(Screen.Settlement, validate: true);
        OpenModalPanel(SettlementSummaryScreenId);
        return result;
    }

    /// <summary>Confirms settlement summary and starts the Hub arrival sequence.</summary>
    public ScreenResult SettlementConfirmed()
    {
        if (CurrentScreen != Screen.Settlement)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        if (modalPanel == SettlementSummaryScreenId)
        {
            CloseModal();
        }

        return TransitionTo(Screen.HubArriving, validate: true);
    }

    /// <summary>Completes Hub arrival and optionally opens the partner naming modal.</summary>
    public ScreenResult ArrivalComplete(bool namingEligible)
    {
        if (CurrentScreen != Screen.HubArriving)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        var result = TransitionTo(Screen.Hub, validate: true);
        if (namingEligible)
        {
            OpenModalPanel(NamingScreenId);
        }

        return result;
    }

    /// <summary>Opens a non-modal panel without blocking movement input.</summary>
    public ScreenResult OpenNonModal(string panelId)
    {
        if (!screenRegistry.TryGetValue(panelId, out var definition) || definition.Type != ScreenType.NonModal)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        visiblePanels.Add(panelId);
        ActiveInputLayer = InputLayer.NonModal;
        UIPanelOpened?.Invoke(panelId);
        return ScreenResult.Success;
    }

    /// <summary>Returns true when movement keys should be blocked by active UI.</summary>
    public bool IsMovementInputBlocked()
    {
        return ActiveInputLayer is InputLayer.Modal or InputLayer.SemiModal;
    }

    /// <summary>Returns true when a screen or panel is currently visible.</summary>
    public bool IsPanelVisible(string panelId)
    {
        return visiblePanels.Contains(panelId);
    }

    /// <summary>Closes all currently open panels and clears queued modal state.</summary>
    public void ForceCloseAllPanels()
    {
        var closedPanels = visiblePanels.ToArray();
        modalStack.Clear();
        queuedModals.Clear();
        modalPanel = string.Empty;
        routeSidePanelExpanded = false;
        DepartureConfirmButtonFocused = false;

        foreach (var panelId in closedPanels)
        {
            visiblePanels.Remove(panelId);
            UIPanelClosed?.Invoke(panelId);
        }

        ActiveInputLayer = InputLayer.World;
    }

    /// <summary>
    /// Opens the registry diagnostic developer tools when debug-build gating allows it.
    /// Returns null in release builds or when another non-combat modal already owns input.
    /// </summary>
    public RegistryDiagnosticDevTools? OpenRegistryDiagnosticTools(
        Registry registry,
        IEnumerable<RegistryDiagnosticEvent> diagnostics,
        bool? isDebugBuild = null)
    {
        var tools = RegistryDiagnosticDevTools.TryOpen(registry, diagnostics, isDebugBuild);
        if (tools is null)
        {
            return null;
        }

        return OpenModal("registry_diagnostic_tools") ? tools : null;
    }

    private void EnterDepartureLocked()
    {
        ForceCloseAllPanels();
        DepartureLocked = true;
        DepartureLockRemainingSeconds = 2.0;
        TransitionTo(Screen.DepartureLocked, validate: false);
    }

    private ScreenResult TransitionTo(Screen newScreen, bool validate)
    {
        if (newScreen == CurrentScreen)
        {
            return ScreenResult.ErrInvalidScreen;
        }

        if (validate && !IsValidTransition(CurrentScreen, newScreen))
        {
            return ScreenResult.ErrInvalidScreen;
        }

        var oldScreen = CurrentScreen;
        CurrentScreen = newScreen;
        ApplyScreenVisibility(newScreen);
        ScreenChanged?.Invoke(oldScreen, newScreen);
        return ScreenResult.Success;
    }

    private void ApplyScreenVisibility(Screen screen)
    {
        visiblePanels.Remove(HubHudScreenId);
        visiblePanels.Remove(ChartScreenId);
        visiblePanels.Remove(ExplorationHudScreenId);

        if (screen is Screen.Hub or Screen.HubArriving)
        {
            visiblePanels.Add(HubHudScreenId);
        }

        if (screen is Screen.Chart or Screen.ChartRouteSelected or Screen.ChartDepartureConfirmed)
        {
            visiblePanels.Add(ChartScreenId);
        }

        if (screen is Screen.Exploration or Screen.Extracting)
        {
            visiblePanels.Add(ExplorationHudScreenId);
        }
    }

    private static bool IsValidTransition(Screen current, Screen next)
    {
        return (current, next) switch
        {
            (Screen.None, Screen.Hub) => true,
            (Screen.Hub, Screen.Chart) => true,
            (Screen.Hub, Screen.DepartureLocked) => true,
            (Screen.DepartureLocked, Screen.Chart) => true,
            (Screen.Chart, Screen.ChartRouteSelected) => true,
            (Screen.ChartRouteSelected, Screen.ChartDepartureConfirmed) => true,
            (Screen.ChartDepartureConfirmed, Screen.Voyage) => true,
            (Screen.Chart, Screen.Hub) => true,
            (Screen.ChartRouteSelected, Screen.Hub) => true,
            (Screen.Voyage, Screen.Exploration) => true,
            (Screen.Exploration, Screen.Extracting) => true,
            (Screen.Extracting, Screen.Settlement) => true,
            (Screen.Settlement, Screen.HubArriving) => true,
            (Screen.HubArriving, Screen.Hub) => true,
            _ => false,
        };
    }

    private InputLayer ActiveInputLayerForNonModalOrWorld()
    {
        return visiblePanels.Any(id => screenRegistry.TryGetValue(id, out var definition) && definition.Type == ScreenType.NonModal)
            ? InputLayer.NonModal
            : InputLayer.World;
    }

    private static Dictionary<string, ScreenDefinition> BuildScreenRegistry()
    {
        return new Dictionary<string, ScreenDefinition>(StringComparer.Ordinal)
        {
            [HubHudScreenId] = new(HubHudScreenId, ScreenType.HudOverlay, "hub"),
            [StationDetailScreenId] = new(StationDetailScreenId, ScreenType.NonModal, "hub"),
            [DepartureConfirmScreenId] = new(DepartureConfirmScreenId, ScreenType.Modal, "hub"),
            [ChartScreenId] = new(ChartScreenId, ScreenType.Fullscreen, "chart"),
            [ExplorationHudScreenId] = new(ExplorationHudScreenId, ScreenType.HudOverlay, "exploration"),
            [CapacityChoiceScreenId] = new(CapacityChoiceScreenId, ScreenType.Modal, "resources"),
            [ExtractionProgressScreenId] = new(ExtractionProgressScreenId, ScreenType.SemiModal, "exploration"),
            [SettlementSummaryScreenId] = new(SettlementSummaryScreenId, ScreenType.Modal, "exploration"),
            [CombatScreenId] = new(CombatScreenId, ScreenType.Modal, "combat"),
            [RepairScreenId] = new(RepairScreenId, ScreenType.Modal, "world-repair"),
            [MarketScreenId] = new(MarketScreenId, ScreenType.Modal, "settlement"),
            [NamingScreenId] = new(NamingScreenId, ScreenType.Modal, "partner"),
            [PartnerSniffScreenId] = new(PartnerSniffScreenId, ScreenType.NonModal, "partner"),
            [ToastScreenId] = new(ToastScreenId, ScreenType.NonModal, "feedback"),
            [RegistryDiagnosticToolsPanelId] = new(RegistryDiagnosticToolsPanelId, ScreenType.Modal, "registry"),
        };
    }
}
