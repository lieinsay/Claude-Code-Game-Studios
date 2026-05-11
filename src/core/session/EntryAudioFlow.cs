namespace CloudWeaverVoyage.Core;

/// <summary>
/// Player entry intent accepted by the platform session shell.
/// </summary>
public enum EntrySessionIntent
{
	Start = 0,
	Continue = 1,
}

/// <summary>
/// Continue entry availability reported by persistence metadata.
/// </summary>
public enum ContinueAvailability
{
	Enabled = 0,
	PreservedLocked = 1,
	Hidden = 2,
}

/// <summary>
/// Audio activation state for entry flow decisions.
/// </summary>
public enum AudioGate
{
	RequiresGesture = 0,
	Pass = 1,
	SoftFail = 2,
	HardFail = 3,
	Muted = 4,
}

/// <summary>
/// Result of a trusted audio unlock attempt.
/// </summary>
public enum AudioActivationOutcome
{
	Pass = 0,
	StillRequiresGesture = 1,
	SoftFail = 2,
	HardFail = 3,
}

/// <summary>
/// Stable result codes for entry intent handling.
/// </summary>
public enum EntryIntentResultCode
{
	Accepted = 0,
	AwaitingAudioActivation = 1,
	Busy = 2,
	ContinueHidden = 3,
	ContinueLocked = 4,
	AudioHardFail = 5,
	InvalidState = 6,
}

/// <summary>
/// Existing continue-point metadata needed by the entry shell.
/// </summary>
/// <param name="Availability">Whether Continue can be selected.</param>
/// <param name="LockedReason">Persistence-owned reason when Continue is preserved but locked.</param>
public sealed record ContinueEntryState(
	ContinueAvailability Availability,
	string? LockedReason = null);

/// <summary>
/// In-flight token binding a single accepted entry intent.
/// </summary>
/// <param name="Intent">Accepted Start or Continue intent.</param>
/// <param name="TokenId">Stable token identifier for this attempt.</param>
/// <param name="CreatedAtMs">Creation time in milliseconds.</param>
public sealed record EntryIntentToken(
	EntrySessionIntent Intent,
	string TokenId,
	long CreatedAtMs);

/// <summary>
/// Entry intent handling result surfaced to shell UI and tests.
/// </summary>
/// <param name="Code">Stable outcome code.</param>
/// <param name="Token">Created or existing in-flight token, when one exists.</param>
/// <param name="LockedReason">Persistence-owned Continue lock reason, when refused as preserved locked.</param>
/// <param name="Message">Diagnostic message for refusal or awaiting state.</param>
public sealed record EntryIntentResult(
	EntryIntentResultCode Code,
	EntryIntentToken? Token = null,
	string? LockedReason = null,
	string? Message = null)
{
	/// <summary>Whether Return Title should be offered for this result.</summary>
	public bool CanReturnTitle => Code == EntryIntentResultCode.ContinueLocked;

	/// <summary>Whether New Session should be offered for this result.</summary>
	public bool CanStartNewSession => Code == EntryIntentResultCode.ContinueLocked;
}

/// <summary>
/// Trusted audio activation probe invoked only from entry user gestures.
/// </summary>
public interface IAudioActivationProbe
{
	/// <summary>Attempts to unlock the audio device for the current user gesture.</summary>
	AudioActivationOutcome TryUnlockForGesture();
}

/// <summary>
/// Coordinates Start/Continue entry intent tokens with audio activation gating.
/// </summary>
public sealed class EntryAudioFlow
{
	private readonly SessionBootChain shell;
	private readonly IAudioActivationProbe audioProbe;
	private readonly Func<long> nowMs;
	private readonly Func<string> tokenIdFactory;
	private EntrySessionIntent? pendingAudioIntent;

	/// <summary>
	/// Creates an entry/audio coordinator bound to the supplied session shell.
	/// </summary>
	/// <param name="shell">Session shell state machine to transition.</param>
	/// <param name="audioProbe">Audio activation probe called from entry gestures.</param>
	/// <param name="nowMs">Clock used for token creation.</param>
	/// <param name="tokenIdFactory">Token id factory used for deterministic tests.</param>
	public EntryAudioFlow(
		SessionBootChain shell,
		IAudioActivationProbe audioProbe,
		Func<long>? nowMs = null,
		Func<string>? tokenIdFactory = null)
	{
		this.shell = shell;
		this.audioProbe = audioProbe;
		this.nowMs = nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		this.tokenIdFactory = tokenIdFactory ?? (() => Guid.NewGuid().ToString("N"));
		ContinueState = new ContinueEntryState(ContinueAvailability.Hidden);
	}

	/// <summary>Current audio gate state.</summary>
	public AudioGate AudioGate { get; private set; } = AudioGate.RequiresGesture;

	/// <summary>Current continue entry state supplied by persistence metadata.</summary>
	public ContinueEntryState ContinueState { get; private set; }

	/// <summary>Active in-flight Start or Continue token.</summary>
	public EntryIntentToken? ActiveToken { get; private set; }

	/// <summary>Number of in-flight tokens created by this flow.</summary>
	public int TokenCreationCount { get; private set; }

	/// <summary>Number of audio unlock attempts made by this flow.</summary>
	public int AudioUnlockAttemptCount { get; private set; }

	/// <summary>Updates audio gate state from shell settings or audio callbacks.</summary>
	public void SetAudioGate(AudioGate gate)
	{
		AudioGate = gate;
	}

	/// <summary>Updates Continue availability without mutating the preserved continue point.</summary>
	public void SetContinueState(ContinueEntryState state)
	{
		ContinueState = state;
	}

	/// <summary>Handles a Start gesture.</summary>
	public EntryIntentResult SelectStart()
	{
		return HandleEntryGesture(EntrySessionIntent.Start);
	}

	/// <summary>Handles a Continue gesture.</summary>
	public EntryIntentResult SelectContinue()
	{
		if (ContinueState.Availability == ContinueAvailability.Hidden)
		{
			return new EntryIntentResult(
				EntryIntentResultCode.ContinueHidden,
				Message: "continue_hidden");
		}

		if (ContinueState.Availability == ContinueAvailability.PreservedLocked)
		{
			return new EntryIntentResult(
				EntryIntentResultCode.ContinueLocked,
				LockedReason: ContinueState.LockedReason,
				Message: "continue_preserved_locked");
		}

		return HandleEntryGesture(EntrySessionIntent.Continue);
	}

	/// <summary>Completes an awaited audio activation as successful.</summary>
	public EntryIntentResult ConfirmAudioUnlocked()
	{
		if (shell.CurrentState != ShellState.AwaitingAudioActivation || pendingAudioIntent is null)
		{
			return new EntryIntentResult(
				EntryIntentResultCode.InvalidState,
				ActiveToken,
				Message: "audio_activation_not_pending");
		}

		AudioGate = AudioGate.Pass;
		return StartPendingIntent();
	}

	/// <summary>Continues the pending entry intent with persistent muted audio.</summary>
	public EntryIntentResult ContinueMuted()
	{
		if (shell.CurrentState != ShellState.AwaitingAudioActivation || pendingAudioIntent is null)
		{
			return new EntryIntentResult(
				EntryIntentResultCode.InvalidState,
				ActiveToken,
				Message: "audio_activation_not_pending");
		}

		AudioGate = AudioGate.Muted;
		return StartPendingIntent();
	}

	private EntryIntentResult HandleEntryGesture(EntrySessionIntent intent)
	{
		if (ActiveToken is not null)
		{
			return BusyResult();
		}

		if (shell.CurrentState != ShellState.Ready)
		{
			return new EntryIntentResult(
				EntryIntentResultCode.InvalidState,
				ActiveToken,
				Message: $"invalid_entry_state:{shell.CurrentState}");
		}

		return AudioGate switch
		{
			AudioGate.Pass or AudioGate.Muted => StartIntent(intent),
			AudioGate.HardFail => BlockForAudioHardFail(),
			AudioGate.RequiresGesture or AudioGate.SoftFail => TryAudioUnlock(intent),
			_ => new EntryIntentResult(EntryIntentResultCode.InvalidState, Message: "unknown_audio_gate"),
		};
	}

	private EntryIntentResult TryAudioUnlock(EntrySessionIntent intent)
	{
		AudioUnlockAttemptCount++;
		var outcome = audioProbe.TryUnlockForGesture();
		return outcome switch
		{
			AudioActivationOutcome.Pass => StartIntent(intent, AudioGate.Pass),
			AudioActivationOutcome.StillRequiresGesture => AwaitAudio(intent, AudioGate.RequiresGesture),
			AudioActivationOutcome.SoftFail => AwaitAudio(intent, AudioGate.SoftFail),
			AudioActivationOutcome.HardFail => BlockForAudioHardFail(),
			_ => AwaitAudio(intent, AudioGate.SoftFail),
		};
	}

	private EntryIntentResult AwaitAudio(EntrySessionIntent intent, AudioGate gate)
	{
		AudioGate = gate;
		pendingAudioIntent = intent;
		if (!shell.TransitionTo(ShellState.AwaitingAudioActivation))
		{
			pendingAudioIntent = null;
			return new EntryIntentResult(
				EntryIntentResultCode.InvalidState,
				ActiveToken,
				Message: "await_audio_transition_rejected");
		}

		return new EntryIntentResult(
			EntryIntentResultCode.AwaitingAudioActivation,
			Message: "awaiting_audio_activation");
	}

	private EntryIntentResult StartPendingIntent()
	{
		if (pendingAudioIntent is not { } intent)
		{
			return new EntryIntentResult(
				EntryIntentResultCode.InvalidState,
				ActiveToken,
				Message: "entry_intent_not_pending");
		}

		pendingAudioIntent = null;
		return StartIntent(intent);
	}

	private EntryIntentResult StartIntent(EntrySessionIntent intent, AudioGate? gate = null)
	{
		if (ActiveToken is not null)
		{
			return BusyResult();
		}

		if (gate is not null)
		{
			AudioGate = gate.Value;
		}

		var token = new EntryIntentToken(intent, tokenIdFactory(), nowMs());
		ActiveToken = token;
		TokenCreationCount++;

		if (!shell.TransitionTo(ShellState.SessionStarting))
		{
			ActiveToken = null;
			TokenCreationCount--;
			return new EntryIntentResult(
				EntryIntentResultCode.InvalidState,
				Message: "session_starting_transition_rejected");
		}

		return new EntryIntentResult(EntryIntentResultCode.Accepted, token);
	}

	private EntryIntentResult BusyResult()
	{
		return new EntryIntentResult(
			EntryIntentResultCode.Busy,
			ActiveToken,
			Message: "entry_intent_busy");
	}

	private EntryIntentResult BlockForAudioHardFail()
	{
		AudioGate = AudioGate.HardFail;
		shell.TransitionTo(ShellState.FatalBlocked);
		return new EntryIntentResult(
			EntryIntentResultCode.AudioHardFail,
			Message: "audio_hard_fail");
	}
}
