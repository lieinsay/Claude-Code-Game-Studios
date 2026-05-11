namespace CloudWeaverVoyage.Core;

/// <summary>
/// Shell-level input gate used before gameplay and HUD routing.
/// </summary>
public enum ShellInputGateState
{
	Open = 0,
	Reacquire = 1,
	Blocked = 2,
}

/// <summary>
/// Engine-independent shell input action categories.
/// </summary>
public enum ShellInputAction
{
	Move = 0,
	Use = 1,
	Inventory = 2,
	Map = 3,
	Cancel = 4,
	MouseClick = 5,
	MouseMove = 6,
	SystemShortcut = 7,
}

/// <summary>
/// Resulting route for a shell input event.
/// </summary>
public enum ShellInputRoute
{
	ShellOverlay = 0,
	Reactivation = 1,
	PauseMenu = 2,
	Gameplay = 3,
	System = 4,
	Ignored = 5,
}

/// <summary>
/// Input event abstraction for deterministic shell gate tests.
/// </summary>
/// <param name="Action">Normalized action.</param>
/// <param name="Pressed">Whether this event is an activation press.</param>
public sealed record ShellInputEvent(ShellInputAction Action, bool Pressed = true);

/// <summary>
/// Input routing result emitted by the shell gate.
/// </summary>
/// <param name="Route">Where the input was routed.</param>
/// <param name="HandledByShell">Whether shell consumed the event.</param>
/// <param name="GameplayAllowed">Whether gameplay should receive the event.</param>
public sealed record ShellInputResult(
	ShellInputRoute Route,
	bool HandledByShell,
	bool GameplayAllowed);

/// <summary>
/// Computes and applies SessionShell input gate rules.
/// </summary>
public sealed class ShellInputGate
{
	private readonly SessionBootChain shell;

	/// <summary>
	/// Creates an input gate bound to the shell state machine.
	/// </summary>
	public ShellInputGate(SessionBootChain shell)
	{
		this.shell = shell;
	}

	/// <summary>Raised when ResumePending is reactivated by the first trusted input.</summary>
	public event Action? SessionReactivated;

	/// <summary>Raised when Esc opens the shell pause path.</summary>
	public event Action? PauseRequested;

	/// <summary>Whether the desktop window is foregrounded.</summary>
	public bool WindowForeground { get; set; } = true;

	/// <summary>Whether gameplay focus is currently valid.</summary>
	public bool InputFocusReady { get; set; } = true;

	/// <summary>Whether a shell overlay is currently visible and owns focus.</summary>
	public bool OverlayVisible { get; set; }

	/// <summary>Whether a HUD/UI modal should block world input.</summary>
	public bool ModalBlocksGameplay { get; set; }

	/// <summary>Whether the first ResumePending activation has been consumed.</summary>
	public bool ResumeActivationConsumed { get; private set; }

	/// <summary>Current computed input gate state.</summary>
	public ShellInputGateState CurrentGate => ComputeGate();

	/// <summary>
	/// Routes a normalized input event through the shell gate.
	/// </summary>
	public ShellInputResult RouteInput(ShellInputEvent input)
	{
		if (input.Action == ShellInputAction.SystemShortcut)
		{
			return new ShellInputResult(ShellInputRoute.System, HandledByShell: false, GameplayAllowed: false);
		}

		var gate = ComputeGate();
		if (gate == ShellInputGateState.Blocked)
		{
			return new ShellInputResult(ShellInputRoute.ShellOverlay, HandledByShell: true, GameplayAllowed: false);
		}

		if (gate == ShellInputGateState.Reacquire)
		{
			if (IsTrustedActivation(input))
			{
				ResumeActivationConsumed = true;
				SessionReactivated?.Invoke();
				shell.ResolveResume(SessionTransitionContext.ReadyToResume);
				return new ShellInputResult(ShellInputRoute.Reactivation, HandledByShell: true, GameplayAllowed: false);
			}

			return new ShellInputResult(ShellInputRoute.Ignored, HandledByShell: true, GameplayAllowed: false);
		}

		if (input.Action == ShellInputAction.Cancel)
		{
			PauseRequested?.Invoke();
			return new ShellInputResult(ShellInputRoute.PauseMenu, HandledByShell: true, GameplayAllowed: false);
		}

		return new ShellInputResult(ShellInputRoute.Gameplay, HandledByShell: false, GameplayAllowed: true);
	}

	private ShellInputGateState ComputeGate()
	{
		if (shell.CurrentState == ShellState.ResumePending && WindowForeground)
		{
			return ShellInputGateState.Reacquire;
		}

		if (shell.CurrentState != ShellState.SessionActive
			|| !WindowForeground
			|| !InputFocusReady
			|| OverlayVisible
			|| ModalBlocksGameplay)
		{
			return ShellInputGateState.Blocked;
		}

		return ShellInputGateState.Open;
	}

	private static bool IsTrustedActivation(ShellInputEvent input)
	{
		return input.Pressed
			&& input.Action is ShellInputAction.Move
				or ShellInputAction.Use
				or ShellInputAction.Inventory
				or ShellInputAction.Map
				or ShellInputAction.Cancel
				or ShellInputAction.MouseClick;
	}
}
