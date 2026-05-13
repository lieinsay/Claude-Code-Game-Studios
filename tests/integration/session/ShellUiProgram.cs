using CloudWeaverVoyage.Core;
using CloudWeaverVoyage.Presentation;

Console.WriteLine("=== Story 007: Shell UI — Entry, Loading & Error Screens — Model + Scene Checks ===");
var failed = 0;
var total = 0;

Run("AC-1: missing continue point hides Continue", Ac1MissingContinueHidden);
Run("AC-2/AC-8: preserved locked Continue shows reason and safe actions", Ac2LockedContinueReasonAndActions);
Run("AC-3: EphemeralOnly Start warning has confirm and return actions", Ac3EphemeralWarning);
Run("AC-4: FatalBlocked keeps safe error actions only", Ac4FatalSafeActions);
Run("AC-5: RecoveryRequired exposes retry new session return title", Ac5RecoveryActions);
Run("AC-6: shell UI states are keyboard navigable", Ac6KeyboardNavigation);
Run("AC-7: loading screen includes phase and progress", Ac7LoadingProgress);
Run("Scene: Godot Control scene covers presenter panels and action nodes", SceneContractMatchesPresenter);
Run("Regression: scene default does not strand visible runtime on loading panel", SceneDefaultAvoidsLoadingStrand);
Run("Regression: visible runtime scene has runtime script and button handlers", RuntimeSceneHasScriptAndButtonHandlers);

if (failed > 0)
{
	Console.Error.WriteLine($"Story 007 validation failed: {failed}/{total} checks failed.");
	return 1;
}

Console.WriteLine($"Story 007 validation passed: {total}/{total} checks passed.");
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

static bool Ac1MissingContinueHidden()
{
	var model = ShellUiPresenter.Render(new ShellUiContext(
		ShellState.Ready,
		new ContinueEntryState(ContinueAvailability.Hidden),
		StorageCapability.PersistentAvailable));

	return model.Screen == ShellUiScreen.Entry
		&& model.ContinueState == ShellContinueVisualState.Hidden
		&& model.Actions.Any(action => action.Id == "start")
		&& model.Actions.All(action => action.Id != "continue");
}

static bool Ac2LockedContinueReasonAndActions()
{
	var model = ShellUiPresenter.Render(new ShellUiContext(
		ShellState.Ready,
		new ContinueEntryState(ContinueAvailability.PreservedLocked, "content_domain_mismatch"),
		StorageCapability.PersistentAvailable));

	return model.ContinueState == ShellContinueVisualState.Locked
		&& model.ContinueLockReason == "content_domain_mismatch"
		&& model.Actions.Any(action => action.Id == "continue_locked" && !action.Enabled)
		&& model.Actions.Any(action => action.Id == "return_title")
		&& model.Actions.Any(action => action.Id == "new_session");
}

static bool Ac3EphemeralWarning()
{
	var model = ShellUiPresenter.EphemeralWarning();

	return model.Screen == ShellUiScreen.EphemeralWarning
		&& model.MessageKey == "shell.warning.ephemeral_no_save"
		&& model.Actions.Any(action => action.Id == "continue_without_saving")
		&& model.Actions.Any(action => action.Id == "return")
		&& model.KeyboardNavigable;
}

static bool Ac4FatalSafeActions()
{
	var model = ShellUiPresenter.Render(new ShellUiContext(
		ShellState.FatalBlocked,
		new ContinueEntryState(ContinueAvailability.Hidden),
		StorageCapability.PersistentAvailable,
		FailureMessageKey: "shell.fatal.version_incompatible"));

	return model.Screen == ShellUiScreen.Fatal
		&& model.MessageKey == "shell.fatal.version_incompatible"
		&& model.Actions.Any(action => action.Id == "retry")
		&& model.Actions.Any(action => action.Id == "return_title")
		&& model.Actions.All(action => action.Id != "start" && action.Id != "continue");
}

static bool Ac5RecoveryActions()
{
	var model = ShellUiPresenter.Render(new ShellUiContext(
		ShellState.RecoveryRequired,
		new ContinueEntryState(ContinueAvailability.Enabled),
		StorageCapability.PersistentAvailable));

	return model.Screen == ShellUiScreen.Recovery
		&& model.Actions.Any(action => action.Id == "retry")
		&& model.Actions.Any(action => action.Id == "new_session")
		&& model.Actions.Any(action => action.Id == "return_title");
}

static bool Ac6KeyboardNavigation()
{
	var models = new[]
	{
		ShellUiPresenter.Render(new ShellUiContext(
			ShellState.Ready,
			new ContinueEntryState(ContinueAvailability.Enabled),
			StorageCapability.PersistentAvailable)),
		ShellUiPresenter.Render(new ShellUiContext(
			ShellState.AwaitingAudioActivation,
			new ContinueEntryState(ContinueAvailability.Hidden),
			StorageCapability.PersistentAvailable,
			AudioMutedContinueAvailable: true)),
		ShellUiPresenter.Render(new ShellUiContext(
			ShellState.ResumePending,
			new ContinueEntryState(ContinueAvailability.Hidden),
			StorageCapability.PersistentAvailable)),
		ShellUiPresenter.Render(new ShellUiContext(
			ShellState.RecoveryRequired,
			new ContinueEntryState(ContinueAvailability.Hidden),
			StorageCapability.PersistentAvailable)),
		ShellUiPresenter.Render(new ShellUiContext(
			ShellState.FatalBlocked,
			new ContinueEntryState(ContinueAvailability.Hidden),
			StorageCapability.PersistentAvailable)),
	};

	return models.All(model => model.HasActions && model.KeyboardNavigable);
}

static bool Ac7LoadingProgress()
{
	var model = ShellUiPresenter.Render(new ShellUiContext(
		ShellState.Loading,
		new ContinueEntryState(ContinueAvailability.Hidden),
		StorageCapability.PersistentAvailable,
		LoadPhase.StorageCapabilityCheck,
		0.42f));

	return model.Screen == ShellUiScreen.Loading
		&& model.LoadingPhase == LoadPhase.StorageCapabilityCheck
		&& Math.Abs(model.LoadingProgress - 0.42f) < 0.001f
		&& model.Actions.Any(action => action.Id == "cancel_loading");
}

static bool SceneContractMatchesPresenter()
{
	var repoRoot = FindRepoRoot();
	var scenePath = Path.Combine(repoRoot, "src", "scenes", "ShellUi.tscn");
	var sessionShellPath = Path.Combine(repoRoot, "src", "scenes", "SessionShell.tscn");
	var scene = File.ReadAllText(scenePath);
	var sessionShell = File.ReadAllText(sessionShellPath);

	if (!sessionShell.Contains(ShellUiSceneContract.ScenePath, StringComparison.Ordinal))
	{
		return false;
	}

	if (!scene.Contains($"layer = {ShellUiSceneContract.CanvasLayer}", StringComparison.Ordinal))
	{
		return false;
	}

	foreach (var node in ShellUiSceneContract.RequiredNodes)
	{
		if (!HasNode(scene, node.Name, node.Type))
		{
			return false;
		}
	}

	foreach (var screen in ShellUiSceneContract.Screens)
	{
		if (!HasNode(scene, screen.PanelName, "PanelContainer"))
		{
			return false;
		}

		foreach (var actionNodeName in screen.ActionNodeNames)
		{
			if (!HasNode(scene, actionNodeName, "Button"))
			{
				return false;
			}
		}
	}

	return true;
}

static bool SceneDefaultAvoidsLoadingStrand()
{
	var repoRoot = FindRepoRoot();
	var scenePath = Path.Combine(repoRoot, "src", "scenes", "ShellUi.tscn");
	var scene = File.ReadAllText(scenePath);
	var loadingBlock = GetNodeBlock(scene, "LoadingPanel");
	var entryBlock = GetNodeBlock(scene, "EntryPanel");
	var startBlock = GetNodeBlock(scene, "StartButton");
	var continueBlock = GetNodeBlock(scene, "ContinueButton");
	var lockedBlock = GetNodeBlock(scene, "ContinueLockedButton");
	var lockReasonBlock = GetNodeBlock(scene, "ContinueLockReasonLabel");

	return loadingBlock.Contains("visible = false", StringComparison.Ordinal)
		&& !entryBlock.Contains("visible = false", StringComparison.Ordinal)
		&& !startBlock.Contains("visible = false", StringComparison.Ordinal)
		&& continueBlock.Contains("visible = false", StringComparison.Ordinal)
		&& lockedBlock.Contains("visible = false", StringComparison.Ordinal)
		&& lockReasonBlock.Contains("visible = false", StringComparison.Ordinal);
}

static bool RuntimeSceneHasScriptAndButtonHandlers()
{
	var repoRoot = FindRepoRoot();
	var sessionShellPath = Path.Combine(repoRoot, "src", "scenes", "SessionShell.tscn");
	var sessionShell = File.ReadAllText(sessionShellPath);

	return sessionShell.Contains("path=\"res://src/scenes/SessionShellRuntime.gd\"", StringComparison.Ordinal)
		&& sessionShell.Contains("script = ExtResource(\"3_session_runtime\")", StringComparison.Ordinal)
		&& RuntimeScriptWiresButtons(repoRoot);
}

static bool RuntimeScriptWiresButtons(string repoRoot)
{
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "SessionShellRuntime.gd");
	var script = File.ReadAllText(runtimeScriptPath);

	return script.Contains("_wire_button(\"StartButton\", _on_start_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"SettingsButton\", _on_settings_pressed)", StringComparison.Ordinal)
		&& script.Contains("button.mouse_entered.connect(_on_button_mouse_entered.bind(button))", StringComparison.Ordinal)
		&& script.Contains("button.grab_focus()", StringComparison.Ordinal)
		&& script.Contains("KEY_M", StringComparison.Ordinal)
		&& script.Contains("KEY_R", StringComparison.Ordinal)
		&& script.Contains("KEY_N", StringComparison.Ordinal)
		&& script.Contains("KEY_D", StringComparison.Ordinal)
		&& script.Contains("_show_only(_audio_panel)", StringComparison.Ordinal)
		&& script.Contains("Audio accepted. Gameplay scene wiring is not mounted yet.", StringComparison.Ordinal);
}

static bool HasNode(string scene, string nodeName, string nodeType)
{
	return scene.Contains($"[node name=\"{nodeName}\" type=\"{nodeType}\"", StringComparison.Ordinal);
}

static string GetNodeBlock(string scene, string nodeName)
{
	var marker = $"[node name=\"{nodeName}\"";
	var start = scene.IndexOf(marker, StringComparison.Ordinal);
	if (start < 0)
	{
		return string.Empty;
	}

	var next = scene.IndexOf("\n[node ", start + marker.Length, StringComparison.Ordinal);
	return next < 0 ? scene[start..] : scene[start..next];
}

static string FindRepoRoot()
{
	var current = new DirectoryInfo(AppContext.BaseDirectory);
	while (current is not null)
	{
		if (File.Exists(Path.Combine(current.FullName, "project.godot")))
		{
			return current.FullName;
		}

		current = current.Parent;
	}

	throw new DirectoryNotFoundException("Could not find repository root containing project.godot.");
}
