using System;
using System.Buffers.Binary;

namespace CloudWeaverVoyage.Core;

/// <summary>
/// Normalizes desktop lifecycle callbacks into shell state transitions.
/// </summary>
public sealed class DesktopSessionLifecycle
{
	private readonly SessionBootChain shell;
	private readonly Func<int> timestampProvider;
	private SuspendToken? suspendToken;
	private bool desktopResumeRestored;
	private bool resumeActivationConsumed;

	public DesktopSessionLifecycle(SessionBootChain shell, int sessionId, Func<int>? timestampProvider = null)
	{
		this.shell = shell;
		SessionId = sessionId;
		this.timestampProvider = timestampProvider ?? (() => Environment.TickCount);
	}

	/// <summary>Raised when the shell requests a lightweight suspend marker.</summary>
	public event Action<SuspendMarkerRequest>? MarkerFlushRequested;

	/// <summary>Stable integer session id encoded into lightweight markers.</summary>
	public int SessionId { get; }

	/// <summary>Number of lightweight marker requests emitted by lifecycle events.</summary>
	public int MarkerFlushRequestCount { get; private set; }

	/// <summary>Number of full checkpoint requests emitted by lifecycle events.</summary>
	public int FullCheckpointRequestCount { get; private set; }

	/// <summary>Number of blocking save requests emitted during quit handling.</summary>
	public int BlockingSaveRequestCount { get; private set; }

	/// <summary>Whether the shell currently allows gameplay input through.</summary>
	public bool AllowsGameplayInput => shell.CurrentState == ShellState.SessionActive && shell.IsInputGateOpen;

	/// <summary>Whether world simulation should advance this frame.</summary>
	public bool AllowsWorldAdvance => shell.CurrentState == ShellState.SessionActive;

	/// <summary>Whether the first resume activation input has already been consumed.</summary>
	public bool ResumeActivationConsumed => resumeActivationConsumed;

	/// <summary>Whether the latest resume came from a persisted desktop restore path.</summary>
	public bool DesktopResumeRestored => desktopResumeRestored;

	/// <summary>Whether the resume validation can currently trust the continue point.</summary>
	public bool ContinuePointValid { get; set; } = true;

	/// <summary>Last suspend token created for BackgroundSuspended.</summary>
	public SuspendToken? CurrentSuspendToken => suspendToken;

	/// <summary>Last emitted marker request.</summary>
	public SuspendMarkerRequest? LastMarkerRequest { get; private set; }

	/// <summary>
	/// Normalized focus callback from the desktop shell.
	/// </summary>
	public void OnWindowFocusChanged(bool focused)
	{
		if (!focused)
		{
			Suspend("window_focus_lost");
			return;
		}

		RequestResume(persisted: false);
	}

	/// <summary>
	/// Normalized desktop suspend callback.
	/// </summary>
	public void OnSuspendRequested()
	{
		Suspend("suspend_requested");
	}

	/// <summary>
	/// Normalized desktop resume callback.
	/// </summary>
	public void OnResumeRequested(bool persisted = true)
	{
		RequestResume(persisted);
	}

	/// <summary>
	/// Normalized desktop quit callback. This path must not request blocking saves.
	/// </summary>
	public void OnQuitRequested()
	{
		if (shell.CurrentState == ShellState.SessionActive)
		{
			Suspend("quit_requested");
		}
		else if (shell.CurrentState == ShellState.BackgroundSuspended)
		{
			RequestMarker("quit_requested");
		}
	}

	/// <summary>
	/// Attempts to pass a gameplay input through the shell gate.
	/// </summary>
	public bool TryAcceptGameplayInput()
	{
		return AllowsGameplayInput;
	}

	/// <summary>
	/// Consumes the first keyboard or mouse input in ResumePending as reactivation only.
	/// </summary>
	public bool TryConsumeResumeActivationInput()
	{
		if (shell.CurrentState != ShellState.ResumePending || resumeActivationConsumed)
		{
			return false;
		}

		resumeActivationConsumed = true;
		shell.ResolveResume(BuildResumeContext(playerReactivated: true));
		return true;
	}

	/// <summary>
	/// Returns from ResumePending to the title-safe Ready state without passing input to gameplay.
	/// </summary>
	public bool ReturnTitleFromResumePending()
	{
		if (shell.CurrentState != ShellState.ResumePending)
		{
			return false;
		}

		suspendToken = null;
		desktopResumeRestored = false;
		resumeActivationConsumed = false;
		return shell.TransitionTo(ShellState.RecoveryRequired, new SessionTransitionContext
		{
			WindowForeground = true,
			SuspendTokenValid = false,
			ContentDomainAvailable = true,
			ResumeCheckFailed = true,
		}) && shell.TransitionTo(ShellState.Ready);
	}

	private void Suspend(string reason)
	{
		if (shell.CurrentState != ShellState.SessionActive)
		{
			return;
		}

		suspendToken = new SuspendToken(
			timestampProvider(),
			SessionPosition.Zero,
			"session",
			MarkerFlushed: false);
		resumeActivationConsumed = false;
		desktopResumeRestored = false;

		if (shell.TransitionTo(ShellState.BackgroundSuspended))
		{
			RequestMarker(reason);
		}
	}

	private void RequestResume(bool persisted)
	{
		if (shell.CurrentState != ShellState.BackgroundSuspended)
		{
			return;
		}

		desktopResumeRestored = persisted;
		resumeActivationConsumed = false;
		shell.TransitionTo(ShellState.ResumePending, new SessionTransitionContext
		{
			WindowForeground = true,
			WindowInteractive = true,
		});
	}

	private void RequestMarker(string reason)
	{
		var marker = new SuspendMarkerRequest(SessionId, timestampProvider(), reason);
		MarkerFlushRequestCount++;
		LastMarkerRequest = marker;
		suspendToken = suspendToken is null
			? null
			: suspendToken with { MarkerFlushed = true };
		MarkerFlushRequested?.Invoke(marker);
	}

	private SessionTransitionContext BuildResumeContext(bool playerReactivated)
	{
		return new SessionTransitionContext
		{
			WindowForeground = true,
			WindowInteractive = true,
			SuspendTokenValid = suspendToken is not null,
			PlayerReactivated = playerReactivated,
			FocusRestored = true,
			ContentDomainAvailable = ContinuePointValid,
			ResumeCheckFailed = desktopResumeRestored && !ContinuePointValid,
		};
	}
}

/// <summary>
/// Lightweight marker request for suspend and quit lifecycle boundaries.
/// </summary>
public sealed record SuspendMarkerRequest(int SessionId, int Timestamp, string Reason)
{
	public byte[] Encode()
	{
		var bytes = new byte[8];
		BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), SessionId);
		BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), Timestamp);
		return bytes;
	}
}

/// <summary>
/// Minimal suspend token retained by the shell while gameplay is backgrounded.
/// </summary>
public sealed record SuspendToken(
	int SuspendTimestamp,
	SessionPosition SessionPosition,
	string Screen,
	bool MarkerFlushed);

/// <summary>
/// Engine-independent position snapshot used by the suspend token.
/// </summary>
public readonly record struct SessionPosition(double X, double Y)
{
	public static SessionPosition Zero { get; } = new(0, 0);
}
