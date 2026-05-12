using CloudWeaverVoyage.Core;

Console.WriteLine("=== Epic #4 Story 002: Input Gate & Shell Integration ===");
var failed = 0;
var total = 0;

Run("AC-1: shell open/closed changes interaction gate", Ac1OpenClosed);
Run("AC-2: reacquire consumes the first trusted input", Ac2ReacquireConsumes);
Run("AC-3: held input during reacquire does not backfill movement", Ac3NoBackfill);
Run("AC-4: overlay closed gate blocks movement and Use", Ac4OverlayBlocks);
Run("AC-5: desktop focus loss forwarded by shell closes gate", Ac5FocusLoss);
Run("AC-6: input_gate_changed emits previous and new state", Ac6GateEvent);

if (failed > 0)
{
	Console.Error.WriteLine($"Movement Input Gate failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Movement Input Gate passed: {total}/{total} checks passed.");
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

	Console.Error.WriteLine($"[FAIL] {label}");
	failed++;
}

static bool Ac1OpenClosed()
{
	var registry = new InteractionRegistry();
	registry.ApplyShellGate(ShellInputGateState.Open);
	var opened = registry.InputGateState == MovementInputGateState.InputOpen;
	registry.ApplyShellGate(ShellInputGateState.Blocked);
	return opened && registry.InputGateState == MovementInputGateState.InputClosed;
}

static bool Ac2ReacquireConsumes()
{
	var shell = ResumePendingShell();
	var shellGate = new ShellInputGate(shell);
	var registry = new InteractionRegistry();
	registry.ApplyShellGate(shellGate.CurrentGate);
	var first = shellGate.RouteInput(new ShellInputEvent(ShellInputAction.Move));
	registry.ApplyShellGate(shellGate.CurrentGate);
	return first.Route == ShellInputRoute.Reactivation
		&& !first.GameplayAllowed
		&& registry.InputGateState == MovementInputGateState.InputOpen;
}

static bool Ac3NoBackfill()
{
	var movement = new PlayerMovementController();
	movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputReacquire, 1.0 / 60.0, 0);
	var result = movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1.0 / 60.0, 1.0 / 60.0);
	var afterRelease = movement.PhysicsStep(WorldVector2.Zero, MovementInputGateState.InputOpen, 1.0 / 60.0, 2.0 / 60.0);
	var afterPress = movement.PhysicsStep(new WorldVector2(1, 0), MovementInputGateState.InputOpen, 1.0 / 60.0, 3.0 / 60.0);
	return result.MovementVelocity == 0
		&& afterRelease.MovementVelocity == 0
		&& afterPress.MovementVelocity > 0;
}

static bool Ac4OverlayBlocks()
{
	var shell = ActiveShell();
	var gate = new ShellInputGate(shell) { OverlayVisible = true };
	var registry = ReadyRegistry();
	registry.ApplyShellGate(gate.CurrentGate);
	var movement = new PlayerMovementController();
	var move = movement.PhysicsStep(new WorldVector2(1, 0), registry.InputGateState, 1, 0);
	var use = registry.TryUse("player", WorldVector2.Zero);
	return gate.CurrentGate == ShellInputGateState.Blocked
		&& move.MovementVelocity == 0
		&& !use.Allowed
		&& use.BlockReason == "input_closed";
}

static bool Ac5FocusLoss()
{
	var shell = ActiveShell();
	var lifecycle = new DesktopSessionLifecycle(shell, 7);
	lifecycle.OnWindowFocusChanged(focused: false);
	var gate = new ShellInputGate(shell);
	var registry = new InteractionRegistry();
	registry.ApplyShellGate(gate.CurrentGate);
	return shell.CurrentState == ShellState.BackgroundSuspended
		&& registry.InputGateState == MovementInputGateState.InputClosed;
}

static bool Ac6GateEvent()
{
	var registry = new InteractionRegistry();
	var seen = false;
	registry.InputGateChanged += (oldState, newState) =>
		seen = oldState == MovementInputGateState.InputClosed && newState == MovementInputGateState.InputOpen;
	registry.SetInputGate(MovementInputGateState.InputOpen);
	return seen;
}

static InteractionRegistry ReadyRegistry()
{
	var registry = new InteractionRegistry();
	registry.Register(new StubInteractable("target", new WorldVector2(0.5, 0)));
	registry.SetFocus("target");
	return registry;
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

sealed class StubInteractable : Interactable
{
	public StubInteractable(string id, WorldVector2 position)
		: base(id, position)
	{
	}

	public override UseResult HandleUse(string playerId) => UseResult.Accepted;
}
