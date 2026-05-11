using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 001: Platform State Machine Core — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: loading completes into Ready and reports entry-ready phase", Ac1LoadingCompletesIntoReady);
Run("AC-2: valid SessionStarting context enters SessionActive", Ac2SessionStartingActivates);
Run("AC-3: invalid SessionStarting context enters RecoveryRequired", Ac3SessionStartingFailureRecovers);
Run("AC-4: BackgroundSuspended foreground restore enters ResumePending without gameplay input", Ac4SuspendedRestoresToResumePending);
Run("AC-5: valid ResumePending context returns SessionActive", Ac5ResumePendingActivates);
Run("AC-6: invalid resume context enters RecoveryRequired", Ac6ResumeFailureRecovers);
Run("AC-7: fatal load failure enters FatalBlocked", Ac7FatalFailureBlocks);
Run("AC-8: load failure report contains phase, type, retry and focus fields", Ac8FailureReportIncludesDiagnostics);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 001 AC validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 001 AC validation passed: {total}/{total} checks passed.");
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
		Console.Error.WriteLine($"[FAIL] {label}: {ex.GetType().Name}: {ex.Message}");
		failed++;
		return;
	}

	failed++;
	Console.Error.WriteLine($"[FAIL] {label}");
}

static bool Ac1LoadingCompletesIntoReady()
{
	var shell = new SessionBootChain();
	var transitions = new List<(ShellState Old, ShellState New)>();
	var phases = new List<BootPhase>();
	shell.ShellStateChanged += (oldState, newState) => transitions.Add((oldState, newState));
	shell.LoadingPhaseChanged += (phase, _) => phases.Add(phase);

	var loading = shell.TransitionTo(ShellState.Loading);
	shell.CompleteAllLoadPhases();
	var ready = shell.TransitionTo(ShellState.Ready);

	return loading
		&& ready
		&& shell.CurrentState == ShellState.Ready
		&& !shell.IsInputGateOpen
		&& transitions.Contains((ShellState.Loading, ShellState.Ready))
		&& shell.LoadFailures.Count == 0;
}

static bool Ac2SessionStartingActivates()
{
	var shell = ReadyShell();
	var starting = shell.TransitionTo(ShellState.SessionStarting);
	var finalState = shell.ResolveSessionStart(SessionTransitionContext.ReadyToActivate);

	return starting
		&& finalState == ShellState.SessionActive
		&& shell.CurrentState == ShellState.SessionActive
		&& shell.IsInputGateOpen;
}

static bool Ac3SessionStartingFailureRecovers()
{
	var shell = ReadyShell();
	shell.TransitionTo(ShellState.SessionStarting);
	var finalState = shell.ResolveSessionStart(new SessionTransitionContext
	{
		SessionContextReady = true,
		ContentDomainAvailable = false,
		ContinueAvailable = true,
		InputFocusReady = true,
		ContentDomainFailed = true,
	});

	return finalState == ShellState.RecoveryRequired
		&& !shell.IsInputGateOpen;
}

static bool Ac4SuspendedRestoresToResumePending()
{
	var shell = ActiveShell();
	var suspended = shell.TransitionTo(ShellState.BackgroundSuspended);
	var resumePending = shell.TransitionTo(ShellState.ResumePending, new SessionTransitionContext
	{
		WindowForeground = true,
		WindowInteractive = true,
	});

	return suspended
		&& resumePending
		&& shell.CurrentState == ShellState.ResumePending
		&& !shell.IsInputGateOpen;
}

static bool Ac5ResumePendingActivates()
{
	var shell = ResumePendingShell();
	var finalState = shell.ResolveResume(SessionTransitionContext.ReadyToResume);

	return finalState == ShellState.SessionActive
		&& shell.IsInputGateOpen;
}

static bool Ac6ResumeFailureRecovers()
{
	var shell = ResumePendingShell();
	var finalState = shell.ResolveResume(new SessionTransitionContext
	{
		WindowForeground = true,
		SuspendTokenValid = false,
		PlayerReactivated = true,
		FocusRestored = true,
		ContentDomainAvailable = true,
	});

	return finalState == ShellState.RecoveryRequired
		&& !shell.IsInputGateOpen;
}

static bool Ac7FatalFailureBlocks()
{
	var shell = ReadyShell();
	var report = shell.ReportLoadFailure(
		LoadPhase.ContentDomainCheck,
		"content_domain_version_incompatible",
		retryable: false,
		windowFocused: true,
		inputFocused: true,
		FailureSeverity.HardFail);

	return shell.CurrentState == ShellState.FatalBlocked
		&& report.Severity == FailureSeverity.HardFail
		&& !shell.IsInputGateOpen;
}

static bool Ac8FailureReportIncludesDiagnostics()
{
	var shell = LoadingShell();
	var report = shell.ReportLoadFailure(
		LoadPhase.StorageCapabilityCheck,
		"user_storage_probe_timeout",
		retryable: true,
		windowFocused: false,
		inputFocused: false);

	return shell.CurrentState == ShellState.RecoveryRequired
		&& shell.LoadFailures.Count == 1
		&& report.Phase == LoadPhase.StorageCapabilityCheck
		&& report.FailureType == "user_storage_probe_timeout"
		&& report.Retryable
		&& !report.WindowFocused
		&& !report.InputFocused
		&& report.Severity == FailureSeverity.RecoverableFail;
}

static SessionBootChain LoadingShell()
{
	var shell = new SessionBootChain();
	shell.TransitionTo(ShellState.Loading);
	return shell;
}

static SessionBootChain ReadyShell()
{
	var shell = LoadingShell();
	shell.CompleteAllLoadPhases();
	shell.TransitionTo(ShellState.Ready);
	return shell;
}

static SessionBootChain ActiveShell()
{
	var shell = ReadyShell();
	shell.TransitionTo(ShellState.SessionStarting);
	shell.ResolveSessionStart(SessionTransitionContext.ReadyToActivate);
	return shell;
}

static SessionBootChain ResumePendingShell()
{
	var shell = ActiveShell();
	shell.TransitionTo(ShellState.BackgroundSuspended);
	shell.TransitionTo(ShellState.ResumePending, new SessionTransitionContext
	{
		WindowForeground = true,
		WindowInteractive = true,
	});
	return shell;
}
