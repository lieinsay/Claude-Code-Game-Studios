namespace CloudWeaverVoyage.Presentation;

/// <summary>
/// Required Godot scene node used by the shell UI Control binding.
/// </summary>
/// <param name="Name">Stable Godot node name.</param>
/// <param name="Type">Expected Godot node type.</param>
public sealed record ShellUiSceneNode(string Name, string Type);

/// <summary>
/// Required panel and action nodes for one shell UI screen.
/// </summary>
/// <param name="Screen">Presenter screen represented by the panel.</param>
/// <param name="PanelName">Stable Godot panel node name.</param>
/// <param name="ActionNodeNames">Keyboard-reachable button node names for the panel.</param>
public sealed record ShellUiSceneScreen(
	ShellUiScreen Screen,
	string PanelName,
	IReadOnlyList<string> ActionNodeNames);

/// <summary>
/// Defines the minimum concrete Godot Control scene contract for the shell UI.
/// </summary>
public static class ShellUiSceneContract
{
	/// <summary>Scene path used by the runtime SessionShell scene.</summary>
	public const string ScenePath = "res://src/scenes/ShellUi.tscn";

	/// <summary>CanvasLayer value reserved for shell UI above HUD and gameplay.</summary>
	public const int CanvasLayer = 100;

	/// <summary>Required global node names and Godot node types.</summary>
	public static IReadOnlyList<ShellUiSceneNode> RequiredNodes { get; } =
	[
		new("ShellUiLayer", "CanvasLayer"),
		new("ShellUiRoot", "Control"),
		new("LoadingPhaseLabel", "Label"),
		new("LoadingProgress", "ProgressBar"),
		new("ContinueLockReasonLabel", "Label"),
	];

	/// <summary>Required per-screen panels and keyboard action buttons.</summary>
	public static IReadOnlyList<ShellUiSceneScreen> Screens { get; } =
	[
		new(
			ShellUiScreen.Loading,
			"LoadingPanel",
			["CancelLoadingButton"]),
		new(
			ShellUiScreen.Entry,
			"EntryPanel",
			[
				"StartButton",
				"ContinueButton",
				"ContinueLockedButton",
				"NewSessionButton",
				"ReturnTitleButton",
				"SettingsButton",
			]),
		new(
			ShellUiScreen.AudioActivation,
			"AudioActivationPanel",
			[
				"ConfirmAudioButton",
				"ContinueMutedButton",
				"AudioReturnTitleButton",
			]),
		new(
			ShellUiScreen.EphemeralWarning,
			"EphemeralWarningPanel",
			[
				"ContinueWithoutSavingButton",
				"EphemeralReturnButton",
			]),
		new(
			ShellUiScreen.Resume,
			"ResumePanel",
			[
				"ReactivateButton",
				"ResumeReturnTitleButton",
			]),
		new(
			ShellUiScreen.Recovery,
			"RecoveryPanel",
			[
				"RetryButton",
				"RecoveryNewSessionButton",
				"RecoveryReturnTitleButton",
				"RecoveryErrorDetailsButton",
			]),
		new(
			ShellUiScreen.Fatal,
			"FatalPanel",
			[
				"FatalRetryButton",
				"FatalReturnTitleButton",
				"FatalErrorDetailsButton",
			]),
	];
}
