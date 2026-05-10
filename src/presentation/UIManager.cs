using System;
using System.Collections.Generic;

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
    private readonly List<string> modalStack = new();
    private string modalPanel = string.Empty;

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

    /// <summary>Initializes and emits UIReady.</summary>
    public void Initialize()
    {
        IsInitialized = true;
        UIReady?.Invoke();
    }

    /// <summary>Transitions to a new screen. Returns false if already on that screen.</summary>
    public bool TransitionScreen(Screen newScreen)
    {
        if (newScreen == CurrentScreen)
        {
            return false;
        }

        var oldScreen = CurrentScreen;
        CurrentScreen = newScreen;
        ScreenChanged?.Invoke(oldScreen, newScreen);
        return true;
    }

    /// <summary>
    /// Opens a modal panel. Combat (S7_combat) overrides current modal;
    /// other panels are rejected if a modal is already open.
    /// </summary>
    public bool OpenModal(string panelId)
    {
        if (!string.IsNullOrEmpty(modalPanel))
        {
            if (panelId == "S7_combat")
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
        UIPanelOpened?.Invoke(panelId);
        return true;
    }

    /// <summary>Closes the current modal, restoring the previous one from stack if any.</summary>
    public void CloseModal()
    {
        var closedId = modalPanel;
        if (modalStack.Count > 0)
        {
            modalPanel = modalStack[^1];
            modalStack.RemoveAt(modalStack.Count - 1);
        }
        else
        {
            modalPanel = string.Empty;
            ActiveInputLayer = InputLayer.World;
        }

        UIPanelClosed?.Invoke(closedId);
    }

    /// <summary>Returns whether a modal panel is currently open.</summary>
    public bool IsModalOpen()
    {
        return !string.IsNullOrEmpty(modalPanel);
    }
}
