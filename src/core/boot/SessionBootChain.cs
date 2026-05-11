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
	AwaitingAudioActivation = 3,
	SessionStarting = 4,
	SessionActive = 5,
	BackgroundSuspended = 6,
	ResumePending = 7,
	RecoveryRequired = 8,
	FatalBlocked = 9,
}

/// <summary>
/// Diagnostic loading phases owned by the platform session shell.
/// </summary>
public enum LoadPhase
{
	BaseBoot = 0,
	ContentDomainCheck = 1,
	StorageCapabilityCheck = 2,
	SessionMetadataCheck = 3,
	EntryRenderReady = 4,
}

/// <summary>
/// Severity bucket used when the shell fails closed.
/// </summary>
public enum FailureSeverity
{
	None = 0,
	SoftFail = 1,
	RecoverableFail = 2,
	HardFail = 3,
}

/// <summary>
/// Load failure details surfaced to recovery and fatal shell screens.
/// </summary>
/// <param name="Phase">The load phase that produced the failure.</param>
/// <param name="FailureType">Stable failure reason reported by the phase.</param>
/// <param name="Retryable">Whether retry is allowed from the shell.</param>
/// <param name="WindowFocused">Desktop window focus state at failure time.</param>
/// <param name="InputFocused">Input focus state at failure time.</param>
/// <param name="Severity">Failure severity after shell classification.</param>
public sealed record LoadFailureReport(
	LoadPhase Phase,
	string FailureType,
	bool Retryable,
	bool WindowFocused,
	bool InputFocused,
	FailureSeverity Severity);

/// <summary>
/// Guard context for state transitions that need runtime validation.
/// </summary>
public sealed class SessionTransitionContext
{
	/// <summary>Whether a session context was created successfully.</summary>
	public bool SessionContextReady { get; init; }

	/// <summary>Whether content domains needed for gameplay are available.</summary>
	public bool ContentDomainAvailable { get; init; }

	/// <summary>Whether the selected start or continue path is available.</summary>
	public bool ContinueAvailable { get; init; }

	/// <summary>Whether gameplay input focus is ready.</summary>
	public bool InputFocusReady { get; init; }

	/// <summary>Whether the desktop window is visible or foregrounded.</summary>
	public bool WindowForeground { get; init; }

	/// <summary>Whether the desktop window can accept input.</summary>
	public bool WindowInteractive { get; init; }

	/// <summary>Whether the suspend token still matches the suspended session.</summary>
	public bool SuspendTokenValid { get; init; }

	/// <summary>Whether the player has explicitly reactivated after resume.</summary>
	public bool PlayerReactivated { get; init; }

	/// <summary>Whether focus was restored to the gameplay viewport.</summary>
	public bool FocusRestored { get; init; }

	/// <summary>Whether a continue attempt failed.</summary>
	public bool ContinueFailed { get; init; }

	/// <summary>Whether a required content domain failed.</summary>
	public bool ContentDomainFailed { get; init; }

	/// <summary>Whether storage cannot support the requested continue path.</summary>
	public bool StorageUnsupportedForContinue { get; init; }

	/// <summary>Whether session metadata is damaged.</summary>
	public bool SessionMetadataCorrupted { get; init; }

	/// <summary>Whether resume validation failed.</summary>
	public bool ResumeCheckFailed { get; init; }

	/// <summary>Whether core resources are missing.</summary>
	public bool CoreResourcesMissing { get; init; }

	/// <summary>Whether the build is incompatible with current data.</summary>
	public bool BuildIncompatible { get; init; }

	/// <summary>Whether a required desktop runtime is missing.</summary>
	public bool RequiredRuntimeMissing { get; init; }

	/// <summary>Whether content domain versions are incompatible.</summary>
	public bool ContentDomainVersionIncompatible { get; init; }

	/// <summary>Context that passes all SessionStarting guards.</summary>
	public static SessionTransitionContext ReadyToActivate { get; } = new()
	{
		SessionContextReady = true,
		ContentDomainAvailable = true,
		ContinueAvailable = true,
		InputFocusReady = true,
	};

	/// <summary>Context that passes all ResumePending guards.</summary>
	public static SessionTransitionContext ReadyToResume { get; } = new()
	{
		WindowForeground = true,
		WindowInteractive = true,
		SuspendTokenValid = true,
		PlayerReactivated = true,
		FocusRestored = true,
		ContentDomainAvailable = true,
	};
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
	private readonly Dictionary<LoadPhase, bool> loadPhaseCompletion = new()
	{
		[LoadPhase.BaseBoot] = false,
		[LoadPhase.ContentDomainCheck] = false,
		[LoadPhase.StorageCapabilityCheck] = false,
		[LoadPhase.SessionMetadataCheck] = false,
		[LoadPhase.EntryRenderReady] = false,
	};

	private readonly List<LoadFailureReport> loadFailures = [];

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

	/// <summary>Raised when an invalid transition is rejected.</summary>
	public event Action<ShellState, ShellState, string>? TransitionRejected;

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

	/// <summary>Load failures reported by the shell in execution order.</summary>
	public IReadOnlyList<LoadFailureReport> LoadFailures => loadFailures;

	/// <summary>
	/// Runs the full boot chain synchronously (Phase 0→7).
	/// In production, this would be the Godot _Ready() async chain.
	/// </summary>
	public void RunBootChain()
	{
		BootTimeMs = 0;
		TransitionState(ShellState.Booting);
		BootRequested?.Invoke();
		TransitionTo(ShellState.Loading);

		AdvancePhase(BootPhase.Phase0PlatformProbe, 0.0f);
		AdvancePhase(BootPhase.Phase1RegistryLoad, 0.125f);
		AdvancePhase(BootPhase.Phase2PersistenceCheck, 0.25f);
		AdvancePhase(BootPhase.Phase3AResourcesIntel, 0.375f);
		AdvancePhase(BootPhase.Phase3BChartInit, 0.5f);
		AdvancePhase(BootPhase.Phase4FeatureInit, 0.625f);
		AdvancePhase(BootPhase.Phase5HubInstantiate, 0.75f);
		AdvancePhase(BootPhase.Phase6UIInit, 0.875f);
		AdvancePhase(BootPhase.Phase7FeedbackSessionReady, 1.0f);

		CompleteAllLoadPhases();
		BootComplete = true;
		BootTimeMs = 122.0;
		TransitionTo(ShellState.Ready);
		TransitionTo(ShellState.SessionStarting);
		ResolveSessionStart(SessionTransitionContext.ReadyToActivate);
		SessionReady?.Invoke();
	}

	/// <summary>Marks a diagnostic load phase complete.</summary>
	public void CompleteLoadPhase(LoadPhase phase)
	{
		loadPhaseCompletion[phase] = true;
	}

	/// <summary>Marks all diagnostic load phases complete.</summary>
	public void CompleteAllLoadPhases()
	{
		foreach (var phase in loadPhaseCompletion.Keys.ToArray())
		{
			loadPhaseCompletion[phase] = true;
		}
	}

	/// <summary>Reports a load failure and moves the shell to the appropriate safe state.</summary>
	public LoadFailureReport ReportLoadFailure(
		LoadPhase phase,
		string failureType,
		bool retryable,
		bool windowFocused,
		bool inputFocused,
		FailureSeverity severity = FailureSeverity.RecoverableFail)
	{
		var report = new LoadFailureReport(
			phase,
			failureType,
			retryable,
			windowFocused,
			inputFocused,
			severity);
		loadFailures.Add(report);

		TransitionTo(severity == FailureSeverity.HardFail ? ShellState.FatalBlocked : ShellState.RecoveryRequired);
		return report;
	}

	/// <summary>Attempts a guarded shell state transition.</summary>
	public bool TransitionTo(ShellState newState, SessionTransitionContext? context = null)
	{
		context ??= new SessionTransitionContext();
		if (!CanTransition(CurrentState, newState, context, out var reason))
		{
			TransitionRejected?.Invoke(CurrentState, newState, reason);
			return false;
		}

		TransitionState(newState);
		UpdateInputGateForState(newState);
		return true;
	}

	/// <summary>Resolves SessionStarting into active or recovery/fatal state.</summary>
	public ShellState ResolveSessionStart(SessionTransitionContext context)
	{
		if (HasFatalLoadingFailure(context))
		{
			TransitionTo(ShellState.FatalBlocked, context);
			return CurrentState;
		}

		if (HasSessionStartFailure(context))
		{
			TransitionTo(ShellState.RecoveryRequired, context);
			return CurrentState;
		}

		TransitionTo(ShellState.SessionActive, context);
		return CurrentState;
	}

	/// <summary>Resolves ResumePending into active or recovery state.</summary>
	public ShellState ResolveResume(SessionTransitionContext context)
	{
		if (HasResumeFailure(context))
		{
			TransitionTo(ShellState.RecoveryRequired, context);
			return CurrentState;
		}

		TransitionTo(ShellState.SessionActive, context);
		return CurrentState;
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

	private bool CanTransition(
		ShellState oldState,
		ShellState newState,
		SessionTransitionContext context,
		out string reason)
	{
		reason = string.Empty;
		if (newState == ShellState.FatalBlocked)
		{
			return true;
		}

		if (newState == ShellState.RecoveryRequired)
		{
			return oldState is ShellState.Loading or ShellState.SessionStarting or ShellState.ResumePending;
		}

		var allowed = (oldState, newState) switch
		{
			(ShellState.Booting, ShellState.Loading) => true,
			(ShellState.Loading, ShellState.Ready) => AllLoadPhasesComplete(),
			(ShellState.Ready, ShellState.AwaitingAudioActivation) => true,
			(ShellState.Ready, ShellState.SessionStarting) => true,
			(ShellState.AwaitingAudioActivation, ShellState.SessionStarting) => true,
			(ShellState.SessionStarting, ShellState.SessionActive) => CanEnterSessionActive(context),
			(ShellState.SessionActive, ShellState.BackgroundSuspended) => true,
			(ShellState.BackgroundSuspended, ShellState.ResumePending) =>
				context.WindowForeground && context.WindowInteractive,
			(ShellState.ResumePending, ShellState.SessionActive) => CanResumeSession(context),
			(ShellState.RecoveryRequired, ShellState.Ready) => true,
			_ => false,
		};

		if (!allowed)
		{
			reason = $"illegal_transition:{oldState}->{newState}";
		}

		return allowed;
	}

	private bool AllLoadPhasesComplete()
	{
		return loadPhaseCompletion.Values.All(complete => complete) && loadFailures.Count == 0;
	}

	private static bool CanEnterSessionActive(SessionTransitionContext context)
	{
		return context.SessionContextReady
			&& context.ContentDomainAvailable
			&& context.ContinueAvailable
			&& context.InputFocusReady
			&& !HasSessionStartFailure(context)
			&& !HasFatalLoadingFailure(context);
	}

	private static bool CanResumeSession(SessionTransitionContext context)
	{
		return context.WindowForeground
			&& context.SuspendTokenValid
			&& context.PlayerReactivated
			&& context.FocusRestored
			&& context.ContentDomainAvailable
			&& !HasResumeFailure(context);
	}

	private static bool HasSessionStartFailure(SessionTransitionContext context)
	{
		return context.ContinueFailed
			|| context.ContentDomainFailed
			|| context.StorageUnsupportedForContinue
			|| context.SessionMetadataCorrupted;
	}

	private static bool HasResumeFailure(SessionTransitionContext context)
	{
		return !context.SuspendTokenValid
			|| !context.ContentDomainAvailable
			|| context.ResumeCheckFailed;
	}

	private static bool HasFatalLoadingFailure(SessionTransitionContext context)
	{
		return context.CoreResourcesMissing
			|| context.BuildIncompatible
			|| context.RequiredRuntimeMissing
			|| context.ContentDomainVersionIncompatible;
	}

	private void UpdateInputGateForState(ShellState state)
	{
		if (state == ShellState.SessionActive)
		{
			SetInputGate(true);
			return;
		}

		SetInputGate(false);
	}
}
