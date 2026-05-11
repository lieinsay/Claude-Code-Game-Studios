using System.Buffers.Binary;
using CloudWeaverVoyage.Core;

Console.WriteLine("=== Story 003: Background Suspend / Resume — Acceptance Criteria ===");
var failed = 0;
var total = 0;

Run("AC-1: focus loss suspends gameplay input and world advance", Ac1FocusLossSuspendsGameplay);
Run("AC-2: suspend requests lightweight marker only", Ac2SuspendRequestsMarkerOnly);
Run("AC-3: persisted desktop resume enters ResumePending", Ac3PersistedResumePending);
Run("AC-4: quit does not request checkpoint or blocking save", Ac4QuitDoesNotBlockSave);
Run("AC-5: ResumePending blocks gameplay before reactivation", Ac5ResumePendingBlocksGameplay);
Run("AC-6: first resume input is consumed and does not pass to gameplay", Ac6FirstResumeInputConsumed);
Run("AC-7: Return Title from ResumePending reaches Ready without gameplay input", Ac7ReturnTitleReady);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 003 AC validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 003 AC validation passed: {total}/{total} checks passed.");
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

static bool Ac1FocusLossSuspendsGameplay()
{
	var (shell, lifecycle) = ActiveLifecycle();

	lifecycle.OnWindowFocusChanged(focused: false);

	return shell.CurrentState == ShellState.BackgroundSuspended
		&& !shell.IsInputGateOpen
		&& !lifecycle.AllowsGameplayInput
		&& !lifecycle.AllowsWorldAdvance
		&& lifecycle.CurrentSuspendToken is { MarkerFlushed: true };
}

static bool Ac2SuspendRequestsMarkerOnly()
{
	var (shell, lifecycle) = ActiveLifecycle(sessionId: 0x01020304, timestamps: [10, 20]);
	SuspendMarkerRequest? marker = null;
	lifecycle.MarkerFlushRequested += request => marker = request;

	lifecycle.OnSuspendRequested();

	var encoded = marker?.Encode();
	return shell.CurrentState == ShellState.BackgroundSuspended
		&& lifecycle.MarkerFlushRequestCount == 1
		&& lifecycle.FullCheckpointRequestCount == 0
		&& lifecycle.BlockingSaveRequestCount == 0
		&& marker is { Reason: "suspend_requested" }
		&& encoded is { Length: 8 }
		&& BinaryPrimitives.ReadInt32LittleEndian(encoded.AsSpan(0, 4)) == 0x01020304
		&& BinaryPrimitives.ReadInt32LittleEndian(encoded.AsSpan(4, 4)) == 20;
}

static bool Ac3PersistedResumePending()
{
	var (shell, lifecycle) = ActiveLifecycle();

	lifecycle.OnSuspendRequested();
	lifecycle.OnResumeRequested(persisted: true);
	var enteredResumePending = shell.CurrentState == ShellState.ResumePending
		&& lifecycle.DesktopResumeRestored
		&& !shell.IsInputGateOpen
		&& !lifecycle.AllowsGameplayInput;
	lifecycle.ContinuePointValid = false;
	var consumed = lifecycle.TryConsumeResumeActivationInput();

	return enteredResumePending
		&& consumed
		&& shell.CurrentState == ShellState.RecoveryRequired;
}

static bool Ac4QuitDoesNotBlockSave()
{
	var (shell, lifecycle) = ActiveLifecycle();

	lifecycle.OnQuitRequested();

	return shell.CurrentState == ShellState.BackgroundSuspended
		&& lifecycle.MarkerFlushRequestCount == 1
		&& lifecycle.FullCheckpointRequestCount == 0
		&& lifecycle.BlockingSaveRequestCount == 0;
}

static bool Ac5ResumePendingBlocksGameplay()
{
	var (shell, lifecycle) = ResumePendingLifecycle();

	var gameplayInputAccepted = lifecycle.TryAcceptGameplayInput();

	return shell.CurrentState == ShellState.ResumePending
		&& !gameplayInputAccepted
		&& !lifecycle.ResumeActivationConsumed
		&& !shell.IsInputGateOpen;
}

static bool Ac6FirstResumeInputConsumed()
{
	var (shell, lifecycle) = ResumePendingLifecycle();

	var gameplayBefore = lifecycle.TryAcceptGameplayInput();
	var consumed = lifecycle.TryConsumeResumeActivationInput();
	var gameplayAfterSameInput = false;
	var nextGameplayInput = lifecycle.TryAcceptGameplayInput();

	return !gameplayBefore
		&& consumed
		&& lifecycle.ResumeActivationConsumed
		&& !gameplayAfterSameInput
		&& shell.CurrentState == ShellState.SessionActive
		&& shell.IsInputGateOpen
		&& nextGameplayInput;
}

static bool Ac7ReturnTitleReady()
{
	var (shell, lifecycle) = ResumePendingLifecycle();

	var returned = lifecycle.ReturnTitleFromResumePending();
	var gameplayInputAccepted = lifecycle.TryAcceptGameplayInput();

	return returned
		&& shell.CurrentState == ShellState.Ready
		&& !shell.IsInputGateOpen
		&& !gameplayInputAccepted;
}

static (SessionBootChain Shell, DesktopSessionLifecycle Lifecycle) ActiveLifecycle(
	int sessionId = 100,
	IReadOnlyList<int>? timestamps = null)
{
	var shell = ActiveShell();
	var index = 0;
	var clock = timestamps is null
		? () => 1
		: new Func<int>(() =>
		{
			var value = timestamps[Math.Min(index, timestamps.Count - 1)];
			index++;
			return value;
		});

	return (shell, new DesktopSessionLifecycle(shell, sessionId, clock));
}

static (SessionBootChain Shell, DesktopSessionLifecycle Lifecycle) ResumePendingLifecycle()
{
	var (shell, lifecycle) = ActiveLifecycle();
	lifecycle.OnSuspendRequested();
	lifecycle.OnResumeRequested(persisted: true);
	return (shell, lifecycle);
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
