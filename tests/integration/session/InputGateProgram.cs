using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 006: Input Gate & Shell Overlay Control — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: active foreground focus without modal opens input gate", Ac1ActiveOpenGate);
Run("AC-2: ResumePending foreground computes Reacquire", Ac2ResumePendingReacquire);
Run("AC-3: shell overlay captures keyboard and mouse input", Ac3OverlayCapturesInput);
Run("AC-4: Esc in active open gate opens shell pause path", Ac4EscOpensPause);
Run("AC-5: ResumePending first trusted input only reactivates", Ac5ReacquireConsumesFirstInput);
Run("AC-6: open gate lets gameplay and UI inputs propagate", Ac6OpenGatePropagatesGameplayInputs);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 006 AC validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 006 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1ActiveOpenGate()
{
	var gate = new ShellInputGate(ActiveShell())
	{
		WindowForeground = true,
		InputFocusReady = true,
		OverlayVisible = false,
		ModalBlocksGameplay = false,
	};

	return gate.CurrentGate == ShellInputGateState.Open;
}

static bool Ac2ResumePendingReacquire()
{
	var gate = new ShellInputGate(ResumePendingShell())
	{
		WindowForeground = true,
		InputFocusReady = true,
	};

	return gate.CurrentGate == ShellInputGateState.Reacquire;
}

static bool Ac3OverlayCapturesInput()
{
	var gate = new ShellInputGate(ActiveShell())
	{
		OverlayVisible = true,
	};

	var actions = new[]
	{
		ShellInputAction.Move,
		ShellInputAction.Use,
		ShellInputAction.Inventory,
		ShellInputAction.Map,
		ShellInputAction.MouseClick,
	};

	return actions
		.Select(action => gate.RouteInput(new ShellInputEvent(action)))
		.All(result => result.Route == ShellInputRoute.ShellOverlay
			&& result.HandledByShell
			&& !result.GameplayAllowed);
}

static bool Ac4EscOpensPause()
{
	var gate = new ShellInputGate(ActiveShell());
	var paused = false;
	gate.PauseRequested += () => paused = true;

	var result = gate.RouteInput(new ShellInputEvent(ShellInputAction.Cancel));

	return paused
		&& result.Route == ShellInputRoute.PauseMenu
		&& result.HandledByShell
		&& !result.GameplayAllowed;
}

static bool Ac5ReacquireConsumesFirstInput()
{
	var shell = ResumePendingShell();
	var gate = new ShellInputGate(shell);
	var reactivated = false;
	gate.SessionReactivated += () => reactivated = true;

	var first = gate.RouteInput(new ShellInputEvent(ShellInputAction.Move));
	var second = gate.RouteInput(new ShellInputEvent(ShellInputAction.Move));

	return first.Route == ShellInputRoute.Reactivation
		&& first.HandledByShell
		&& !first.GameplayAllowed
		&& reactivated
		&& gate.ResumeActivationConsumed
		&& shell.CurrentState == ShellState.SessionActive
		&& second.Route == ShellInputRoute.Gameplay
		&& !second.HandledByShell
		&& second.GameplayAllowed;
}

static bool Ac6OpenGatePropagatesGameplayInputs()
{
	var gate = new ShellInputGate(ActiveShell());
	var gameplayActions = new[]
	{
		ShellInputAction.Move,
		ShellInputAction.Use,
		ShellInputAction.Inventory,
		ShellInputAction.Map,
	};

	return gameplayActions
		.Select(action => gate.RouteInput(new ShellInputEvent(action)))
		.All(result => result.Route == ShellInputRoute.Gameplay
			&& !result.HandledByShell
			&& result.GameplayAllowed);
}

static SessionBootChain ActiveShell()
{
	var shell = new SessionBootChain();
	shell.TransitionTo(ShellState.Loading);
	shell.CompleteAllLoadPhases();
	shell.TransitionTo(ShellState.Ready);
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
