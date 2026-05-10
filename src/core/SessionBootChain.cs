using System;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Platform lifecycle state for desktop session shell.
/// Web lifecycle hooks removed per ADR-0019.
/// </summary>
public enum ShellState
{
    Booting = 0,
    Loading = 1,
    Ready = 2,
    SessionStarting = 4,
    SessionActive = 5,
    BackgroundSuspended = 6,
    ResumePending = 7,
    RecoveryRequired = 8,
    FatalBlocked = 9,
}

/// <summary>
/// Boot phases matching ADR-0001 Autoload initialization order.
/// </summary>
public enum BootPhase
{
    Phase0PlatformProbe = 0,
    Phase1RegistryLoad = 1,
    Phase2PersistenceCheck = 2,
    Phase3AResourcesIntel = 3,
    Phase3BChartInit = 4,
    Phase4FeatureInit = 5,
    Phase5HubInstantiate = 6,
    Phase6UIInit = 7,
    Phase7FeedbackSessionReady = 8,
}

/// <summary>
/// Simulates the SessionShell boot chain without Godot dependencies.
/// Manages shell state transitions, boot phases, and input gate.
/// </summary>
public sealed class SessionBootChain
{
    /// <summary>Raised when boot is requested.</summary>
    public event Action? BootRequested;

    /// <summary>Raised when shell state changes.</summary>
    public event Action<ShellState, ShellState>? ShellStateChanged;

    /// <summary>Raised when a boot phase changes with progress.</summary>
    public event Action<BootPhase, float>? LoadingPhaseChanged;

    /// <summary>Raised when the session is fully ready.</summary>
    public event Action? SessionReady;

    /// <summary>Raised when input gate opens.</summary>
    public event Action? InputGateOpen;

    /// <summary>Raised when input gate closes.</summary>
    public event Action? InputGateClosed;

    /// <summary>Current shell state.</summary>
    public ShellState CurrentState { get; private set; } = ShellState.Booting;

    /// <summary>Current boot phase.</summary>
    public BootPhase CurrentBootPhase { get; private set; } = BootPhase.Phase0PlatformProbe;

    /// <summary>Whether the input gate is open.</summary>
    public bool IsInputGateOpen { get; private set; } = true;

    /// <summary>Whether boot has completed.</summary>
    public bool BootComplete { get; private set; }

    /// <summary>Simulated boot time in milliseconds.</summary>
    public double BootTimeMs { get; private set; }

    /// <summary>
    /// Runs the full boot chain synchronously (Phase 0→7).
    /// In production, this would be the Godot _Ready() async chain.
    /// </summary>
    public void RunBootChain()
    {
        BootTimeMs = 0;
        TransitionState(ShellState.Booting);
        BootRequested?.Invoke();

        AdvancePhase(BootPhase.Phase0PlatformProbe, 0.0f);
        AdvancePhase(BootPhase.Phase1RegistryLoad, 0.125f);
        AdvancePhase(BootPhase.Phase2PersistenceCheck, 0.25f);
        AdvancePhase(BootPhase.Phase3AResourcesIntel, 0.375f);
        AdvancePhase(BootPhase.Phase3BChartInit, 0.5f);
        AdvancePhase(BootPhase.Phase4FeatureInit, 0.625f);
        AdvancePhase(BootPhase.Phase5HubInstantiate, 0.75f);
        AdvancePhase(BootPhase.Phase6UIInit, 0.875f);
        AdvancePhase(BootPhase.Phase7FeedbackSessionReady, 1.0f);

        BootComplete = true;
        BootTimeMs = 122.0;
        TransitionState(ShellState.SessionActive);
        SessionReady?.Invoke();
    }

    /// <summary>Sets the input gate state.</summary>
    public void SetInputGate(bool open)
    {
        if (open && !IsInputGateOpen)
        {
            IsInputGateOpen = true;
            InputGateOpen?.Invoke();
        }
        else if (!open && IsInputGateOpen)
        {
            IsInputGateOpen = false;
            InputGateClosed?.Invoke();
        }
    }

    private void AdvancePhase(BootPhase phase, float progress)
    {
        CurrentBootPhase = phase;
        LoadingPhaseChanged?.Invoke(phase, progress);
    }

    private void TransitionState(ShellState newState)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        ShellStateChanged?.Invoke(oldState, newState);
    }
}
