using CloudWeaverVoyage.Core;

namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// High-level shell UI screen rendered above HUD and gameplay.
/// </summary>
public enum ShellUiScreen
{
	Loading = 0,
	Entry = 1,
	AudioActivation = 2,
	EphemeralWarning = 3,
	Resume = 4,
	Recovery = 5,
	Fatal = 6,
	None = 7,
}

/// <summary>
/// Continue entry visual state in the shell UI.
/// </summary>
public enum ShellContinueVisualState
{
	Hidden = 0,
	Enabled = 1,
	Locked = 2,
}

/// <summary>
/// A keyboard-reachable shell action.
/// </summary>
/// <param name="Id">Stable action id.</param>
/// <param name="Shortcut">Keyboard shortcut label.</param>
/// <param name="Enabled">Whether the action can be invoked.</param>
public sealed record ShellUiAction(string Id, string Shortcut, bool Enabled = true);

/// <summary>
/// Immutable shell UI view model for tests and Godot Control binding.
/// </summary>
public sealed record ShellUiModel(
	ShellUiScreen Screen,
	ShellContinueVisualState ContinueState,
	IReadOnlyList<ShellUiAction> Actions,
	string MessageKey,
	LoadPhase? LoadingPhase = null,
	float LoadingProgress = 0f,
	string? ContinueLockReason = null)
{
	/// <summary>Whether the UI model has at least one visible action.</summary>
	public bool HasActions => Actions.Count > 0;

	/// <summary>Whether keyboard-only navigation can reach every visible action.</summary>
	public bool KeyboardNavigable => Actions.All(action => !string.IsNullOrWhiteSpace(action.Shortcut));
}

/// <summary>
/// Input context needed to render shell-level screens deterministically.
/// </summary>
public sealed record ShellUiContext(
	ShellState ShellState,
	ContinueEntryState ContinueState,
	StorageCapability StorageCapability,
	LoadPhase? LoadingPhase = null,
	float LoadingProgress = 0f,
	string? FailureMessageKey = null,
	bool AudioMutedContinueAvailable = false);

/// <summary>
/// Produces shell UI models for entry, loading, recovery, and fatal states.
/// </summary>
public static class ShellUiPresenter
{
	/// <summary>
	/// Renders the current shell state into a keyboard-navigable UI model.
	/// </summary>
	public static ShellUiModel Render(ShellUiContext context)
	{
		return context.ShellState switch
		{
			ShellState.Loading => Loading(context),
			ShellState.Ready => Entry(context),
			ShellState.AwaitingAudioActivation => Audio(context),
			ShellState.ResumePending => Resume(),
			ShellState.RecoveryRequired => Recovery(context),
			ShellState.FatalBlocked => Fatal(context),
			ShellState.SessionActive => new ShellUiModel(
				ShellUiScreen.None,
				ShellContinueVisualState.Hidden,
				Array.Empty<ShellUiAction>(),
				"shell.none"),
			_ => Loading(context),
		};
	}

	/// <summary>
	/// Renders the pre-start storage warning for ephemeral sessions.
	/// </summary>
	public static ShellUiModel EphemeralWarning()
	{
		return new ShellUiModel(
			ShellUiScreen.EphemeralWarning,
			ShellContinueVisualState.Hidden,
			[
				new ShellUiAction("continue_without_saving", "Enter"),
				new ShellUiAction("return", "Esc"),
			],
			"shell.warning.ephemeral_no_save");
	}

	private static ShellUiModel Loading(ShellUiContext context)
	{
		return new ShellUiModel(
			ShellUiScreen.Loading,
			ShellContinueVisualState.Hidden,
			[new ShellUiAction("cancel_loading", "Esc")],
			"shell.loading",
			context.LoadingPhase ?? LoadPhase.BaseBoot,
			Math.Clamp(context.LoadingProgress, 0f, 1f));
	}

	private static ShellUiModel Entry(ShellUiContext context)
	{
		var (visualState, lockReason) = context.ContinueState.Availability switch
		{
			ContinueAvailability.Enabled => (ShellContinueVisualState.Enabled, (string?)null),
			ContinueAvailability.PreservedLocked => (
				ShellContinueVisualState.Locked,
				context.ContinueState.LockedReason ?? "continue_locked"),
			_ => (ShellContinueVisualState.Hidden, (string?)null),
		};

		var actions = new List<ShellUiAction>
		{
			new("start", "Enter"),
		};

		if (visualState == ShellContinueVisualState.Enabled)
		{
			actions.Add(new ShellUiAction("continue", "C"));
		}
		else if (visualState == ShellContinueVisualState.Locked)
		{
			actions.Add(new ShellUiAction("continue_locked", "C", Enabled: false));
			actions.Add(new ShellUiAction("new_session", "N"));
			actions.Add(new ShellUiAction("return_title", "Esc"));
		}

		actions.Add(new ShellUiAction("settings", "Tab"));

		return new ShellUiModel(
			ShellUiScreen.Entry,
			visualState,
			actions,
			context.StorageCapability == StorageCapability.EphemeralOnly
				? "shell.entry.ephemeral_available"
				: "shell.entry",
			ContinueLockReason: lockReason);
	}

	private static ShellUiModel Audio(ShellUiContext context)
	{
		var actions = new List<ShellUiAction>
		{
			new("confirm_audio", "Enter"),
			new("return_title", "Esc"),
		};
		if (context.AudioMutedContinueAvailable)
		{
			actions.Insert(1, new ShellUiAction("continue_muted", "M"));
		}

		return new ShellUiModel(
			ShellUiScreen.AudioActivation,
			ShellContinueVisualState.Hidden,
			actions,
			"shell.audio_activation");
	}

	private static ShellUiModel Resume()
	{
		return new ShellUiModel(
			ShellUiScreen.Resume,
			ShellContinueVisualState.Hidden,
			[
				new ShellUiAction("reactivate", "AnyKey"),
				new ShellUiAction("return_title", "Esc"),
			],
			"shell.resume_pending");
	}

	private static ShellUiModel Recovery(ShellUiContext context)
	{
		return new ShellUiModel(
			ShellUiScreen.Recovery,
			ShellContinueVisualState.Hidden,
			[
				new ShellUiAction("retry", "R"),
				new ShellUiAction("new_session", "N"),
				new ShellUiAction("return_title", "Esc"),
				new ShellUiAction("error_details", "D"),
			],
			context.FailureMessageKey ?? "shell.recovery");
	}

	private static ShellUiModel Fatal(ShellUiContext context)
	{
		return new ShellUiModel(
			ShellUiScreen.Fatal,
			ShellContinueVisualState.Hidden,
			[
				new ShellUiAction("retry", "R"),
				new ShellUiAction("return_title", "Esc"),
				new ShellUiAction("error_details", "D"),
			],
			context.FailureMessageKey ?? "shell.fatal_safe");
	}
}
