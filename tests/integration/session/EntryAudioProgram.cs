using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 002: Start / Continue Entry + Audio Activation — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: Start with passed audio creates one token and enters SessionStarting", Ac1StartWithPassedAudioStarts);
Run("AC-2: Start requiring gesture attempts unlock once and awaits confirmation", Ac2StartRequiresGestureAwaitsAudio);
Run("AC-3: Awaiting audio success passes gate and starts pending intent", Ac3AwaitingAudioSuccessStarts);
Run("AC-4: Soft-failed audio can continue muted into SessionStarting", Ac4SoftFailedAudioContinuesMuted);
Run("AC-5: Start ignores old continue context and preserves locked continue state", Ac5StartCreatesNewIntentWithoutContinueMutation);
Run("AC-6: Enabled Continue creates continue token and enters SessionStarting", Ac6ContinueEnabledStarts);
Run("AC-7: PreservedLocked Continue refuses with reason and recovery actions", Ac7PreservedLockedContinueRefuses);
Run("AC-8: Hidden Continue is not selectable", Ac8HiddenContinueRefuses);
Run("AC-9: Repeated Start/Continue clicks dedupe after first token", Ac9RepeatedClicksDedupe);
Run("AC-10: Same in-flight intent returns busy without second token", Ac10SameIntentDoesNotCreateSecondToken);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 002 AC validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 002 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1StartWithPassedAudioStarts()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	flow.SetAudioGate(AudioGate.Pass);

	var result = flow.SelectStart();

	return result.Code == EntryIntentResultCode.Accepted
		&& result.Token?.Intent == EntrySessionIntent.Start
		&& result.Token.TokenId == "token-1"
		&& flow.ActiveToken == result.Token
		&& flow.TokenCreationCount == 1
		&& flow.AudioUnlockAttemptCount == 0
		&& shell.CurrentState == ShellState.SessionStarting;
}

static bool Ac2StartRequiresGestureAwaitsAudio()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.StillRequiresGesture);

	var result = flow.SelectStart();

	return result.Code == EntryIntentResultCode.AwaitingAudioActivation
		&& flow.ActiveToken is null
		&& flow.TokenCreationCount == 0
		&& flow.AudioUnlockAttemptCount == 1
		&& flow.AudioGate == AudioGate.RequiresGesture
		&& shell.CurrentState == ShellState.AwaitingAudioActivation;
}

static bool Ac3AwaitingAudioSuccessStarts()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.StillRequiresGesture);
	flow.SelectStart();

	var result = flow.ConfirmAudioUnlocked();

	return result.Code == EntryIntentResultCode.Accepted
		&& result.Token?.Intent == EntrySessionIntent.Start
		&& flow.AudioGate == AudioGate.Pass
		&& flow.TokenCreationCount == 1
		&& shell.CurrentState == ShellState.SessionStarting;
}

static bool Ac4SoftFailedAudioContinuesMuted()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.SoftFail);
	var awaiting = flow.SelectStart();

	var muted = flow.ContinueMuted();

	return awaiting.Code == EntryIntentResultCode.AwaitingAudioActivation
		&& muted.Code == EntryIntentResultCode.Accepted
		&& muted.Token?.Intent == EntrySessionIntent.Start
		&& flow.AudioGate == AudioGate.Muted
		&& flow.AudioUnlockAttemptCount == 1
		&& flow.TokenCreationCount == 1
		&& shell.CurrentState == ShellState.SessionStarting;
}

static bool Ac5StartCreatesNewIntentWithoutContinueMutation()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	var preserved = new ContinueEntryState(ContinueAvailability.PreservedLocked, "build_version_mismatch");
	flow.SetAudioGate(AudioGate.Muted);
	flow.SetContinueState(preserved);

	var result = flow.SelectStart();

	return result.Code == EntryIntentResultCode.Accepted
		&& result.Token?.Intent == EntrySessionIntent.Start
		&& flow.ContinueState == preserved
		&& flow.ContinueState.LockedReason == "build_version_mismatch"
		&& shell.CurrentState == ShellState.SessionStarting;
}

static bool Ac6ContinueEnabledStarts()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	flow.SetAudioGate(AudioGate.Pass);
	flow.SetContinueState(new ContinueEntryState(ContinueAvailability.Enabled));

	var result = flow.SelectContinue();

	return result.Code == EntryIntentResultCode.Accepted
		&& result.Token?.Intent == EntrySessionIntent.Continue
		&& flow.TokenCreationCount == 1
		&& shell.CurrentState == ShellState.SessionStarting;
}

static bool Ac7PreservedLockedContinueRefuses()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	flow.SetAudioGate(AudioGate.Pass);
	flow.SetContinueState(new ContinueEntryState(ContinueAvailability.PreservedLocked, "migration_required"));

	var result = flow.SelectContinue();

	return result.Code == EntryIntentResultCode.ContinueLocked
		&& result.LockedReason == "migration_required"
		&& result.CanReturnTitle
		&& result.CanStartNewSession
		&& flow.ActiveToken is null
		&& flow.TokenCreationCount == 0
		&& shell.CurrentState == ShellState.Ready;
}

static bool Ac8HiddenContinueRefuses()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	flow.SetAudioGate(AudioGate.Pass);
	flow.SetContinueState(new ContinueEntryState(ContinueAvailability.Hidden));

	var result = flow.SelectContinue();

	return result.Code == EntryIntentResultCode.ContinueHidden
		&& flow.ActiveToken is null
		&& flow.TokenCreationCount == 0
		&& shell.CurrentState == ShellState.Ready;
}

static bool Ac9RepeatedClicksDedupe()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	flow.SetAudioGate(AudioGate.Pass);
	flow.SetContinueState(new ContinueEntryState(ContinueAvailability.Enabled));

	var first = flow.SelectStart();
	var second = flow.SelectStart();
	var third = flow.SelectContinue();

	return first.Code == EntryIntentResultCode.Accepted
		&& second.Code == EntryIntentResultCode.Busy
		&& third.Code == EntryIntentResultCode.Busy
		&& ReferenceEquals(first.Token, second.Token)
		&& ReferenceEquals(first.Token, third.Token)
		&& flow.TokenCreationCount == 1
		&& shell.CurrentState == ShellState.SessionStarting;
}

static bool Ac10SameIntentDoesNotCreateSecondToken()
{
	var shell = ReadyShell();
	var flow = NewFlow(shell, AudioActivationOutcome.Pass);
	flow.SetAudioGate(AudioGate.Muted);

	var first = flow.SelectStart();
	var second = flow.SelectStart();

	return first.Code == EntryIntentResultCode.Accepted
		&& second.Code == EntryIntentResultCode.Busy
		&& second.Token?.TokenId == first.Token?.TokenId
		&& flow.TokenCreationCount == 1
		&& flow.ActiveToken?.TokenId == "token-1";
}

static EntryAudioFlow NewFlow(SessionBootChain shell, params AudioActivationOutcome[] outcomes)
{
	var nextToken = 0;
	var probe = new ScriptedAudioProbe(outcomes);
	return new EntryAudioFlow(
		shell,
		probe,
		nowMs: () => 1234,
		tokenIdFactory: () => $"token-{++nextToken}");
}

static SessionBootChain ReadyShell()
{
	var shell = new SessionBootChain();
	shell.TransitionTo(ShellState.Loading);
	shell.CompleteAllLoadPhases();
	shell.TransitionTo(ShellState.Ready);
	return shell;
}

sealed class ScriptedAudioProbe : IAudioActivationProbe
{
	private readonly Queue<AudioActivationOutcome> outcomes;

	public ScriptedAudioProbe(IEnumerable<AudioActivationOutcome> outcomes)
	{
		this.outcomes = new Queue<AudioActivationOutcome>(outcomes);
	}

	public AudioActivationOutcome TryUnlockForGesture()
	{
		return outcomes.Count == 0 ? AudioActivationOutcome.Pass : outcomes.Dequeue();
	}
}
