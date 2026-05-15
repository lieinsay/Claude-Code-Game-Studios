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
Run("Regression: shell UI releases mouse input during gameplay", ShellRuntimeReleasesMouseToGameplay);
Run("Regression: audio confirmation mounts Hub runtime instead of recovery placeholder", AudioConfirmationMountsHubRuntime);
Run("Regression: Hub runtime exposes discoverable UI and save/load controls", HubRuntimeExposesSmokeControls);
Run("Regression: Hub runtime wires UI and save/load controls", HubRuntimeWiresSmokeControls);
Run("Regression: Chart panel traps focus away from Hub controls", ChartPanelTrapsFocusAwayFromHubControls);
Run("Regression: route departure opens exploration HUD surface", RouteDepartureOpensExplorationSurface);
Run("Regression: exploration pressure loop mutates and persists", ExplorationPressureLoopMutatesAndPersists);
Run("Regression: Hub summary syncs exploration pressure state", HubSummarySyncsExplorationPressureState);

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
		&& script.Contains("_mount_hub_runtime()", StringComparison.Ordinal)
		&& script.Contains("res://src/scenes/HubRuntime.tscn", StringComparison.Ordinal);
}

static bool ShellRuntimeReleasesMouseToGameplay()
{
	var repoRoot = FindRepoRoot();
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "SessionShellRuntime.gd");
	var script = File.ReadAllText(runtimeScriptPath);

	return script.Contains("_shell_ui_root = _find_control(\"ShellUiRoot\")", StringComparison.Ordinal)
		&& script.Contains("_set_shell_mouse_passthrough(false)", StringComparison.Ordinal)
		&& script.Contains("_set_shell_mouse_passthrough(true)", StringComparison.Ordinal)
		&& script.Contains("Control.MOUSE_FILTER_IGNORE", StringComparison.Ordinal)
		&& script.Contains("Control.MOUSE_FILTER_STOP", StringComparison.Ordinal);
}

static bool AudioConfirmationMountsHubRuntime()
{
	var repoRoot = FindRepoRoot();
	var sessionShellPath = Path.Combine(repoRoot, "src", "scenes", "SessionShell.tscn");
	var hubScenePath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.tscn");
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "SessionShellRuntime.gd");
	var sessionShell = File.ReadAllText(sessionShellPath);
	var hubScene = File.ReadAllText(hubScenePath);
	var runtimeScript = File.ReadAllText(runtimeScriptPath);

	return HasNode(sessionShell, "GameplayLayer", "Node2D")
		&& HasNode(hubScene, "HubRuntime", "Node2D")
		&& HasNode(hubScene, "CargoValue", "Label")
		&& HasNode(hubScene, "ModuleValue", "Label")
		&& HasNode(hubScene, "HullValue", "Label")
		&& HasNode(hubScene, "ChartButton", "Button")
		&& HasNode(hubScene, "SaveButton", "Button")
		&& HasNode(hubScene, "LoadButton", "Button")
		&& hubScene.Contains("受困货物 0", StringComparison.Ordinal)
		&& hubScene.Contains("云织号空艇中枢", StringComparison.Ordinal)
		&& runtimeScript.Contains("_gameplay_layer.add_child(_active_gameplay)", StringComparison.Ordinal)
		&& runtimeScript.Contains("_hide_shell_panels()", StringComparison.Ordinal)
		&& !runtimeScript.Contains("Gameplay scene wiring is not mounted yet.", StringComparison.Ordinal);
}

static bool HubRuntimeExposesSmokeControls()
{
	var repoRoot = FindRepoRoot();
	var hubScenePath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.tscn");
	var hubScene = File.ReadAllText(hubScenePath);

	return hubScene.Contains("path=\"res://src/scenes/HubRuntime.gd\"", StringComparison.Ordinal)
		&& hubScene.Contains("script = ExtResource(\"1_hub_runtime\")", StringComparison.Ordinal)
		&& HasNode(hubScene, "RuntimeHintLabel", "Label")
		&& HasNode(hubScene, "ActionStack", "VBoxContainer")
		&& HasNode(hubScene, "ChartButton", "Button")
		&& HasNode(hubScene, "SaveButton", "Button")
		&& HasNode(hubScene, "LoadButton", "Button")
		&& HasNode(hubScene, "SaveStatusLabel", "Label")
		&& HasNode(hubScene, "ChartPanel", "PanelContainer")
		&& HasNode(hubScene, "ChartTitleLabel", "Label")
		&& HasNode(hubScene, "ChartStatusLabel", "Label")
		&& HasNode(hubScene, "RouteMistButton", "Button")
		&& HasNode(hubScene, "RouteMarketButton", "Button")
		&& HasNode(hubScene, "DepartButton", "Button")
		&& HasNode(hubScene, "ChartCloseButton", "Button")
		&& HasNode(hubScene, "ExplorationPanel", "PanelContainer")
		&& HasNode(hubScene, "ExplorationTitleLabel", "Label")
		&& HasNode(hubScene, "ExplorationRouteLabel", "Label")
		&& HasNode(hubScene, "ExplorationResourceLabel", "Label")
		&& HasNode(hubScene, "ExplorationThreatLabel", "Label")
		&& HasNode(hubScene, "ExplorationHullLabel", "Label")
		&& HasNode(hubScene, "ExplorationRecoveryLabel", "Label")
		&& HasNode(hubScene, "ExplorationAdvanceButton", "Button")
		&& HasNode(hubScene, "ExplorationReturnButton", "Button")
		&& hubScene.Contains("打开航图 / HUD  M", StringComparison.Ordinal)
		&& hubScene.Contains("保存  S", StringComparison.Ordinal)
		&& hubScene.Contains("加载  L", StringComparison.Ordinal)
		&& hubScene.Contains("HUD / 航图界面", StringComparison.Ordinal)
		&& hubScene.Contains("推进探索 / 搜索", StringComparison.Ordinal)
		&& hubScene.Contains("探索 HUD", StringComparison.Ordinal);
}

static bool HubRuntimeWiresSmokeControls()
{
	var repoRoot = FindRepoRoot();
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.gd");
	var script = File.ReadAllText(runtimeScriptPath);

	return script.Contains("_wire_button(\"ChartButton\", _on_chart_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"SaveButton\", _on_save_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"LoadButton\", _on_load_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"RouteMistButton\", _on_route_mist_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"RouteMarketButton\", _on_route_market_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"DepartButton\", _on_depart_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"ExplorationAdvanceButton\", _on_exploration_advance_pressed)", StringComparison.Ordinal)
		&& script.Contains("_wire_button(\"ExplorationReturnButton\", _show_hub)", StringComparison.Ordinal)
		&& script.Contains("KEY_M", StringComparison.Ordinal)
		&& script.Contains("KEY_S", StringComparison.Ordinal)
		&& script.Contains("KEY_L", StringComparison.Ordinal)
		&& script.Contains("FileAccess.open(SAVE_PATH, FileAccess.WRITE)", StringComparison.Ordinal)
		&& script.Contains("FileAccess.open(SAVE_PATH, FileAccess.READ)", StringComparison.Ordinal)
		&& script.Contains("JSON.stringify(snapshot)", StringComparison.Ordinal)
		&& script.Contains("JSON.parse_string", StringComparison.Ordinal)
		&& script.Contains("user://smoke_session_state.json", StringComparison.Ordinal);
}

static bool ChartPanelTrapsFocusAwayFromHubControls()
{
	var repoRoot = FindRepoRoot();
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.gd");
	var script = File.ReadAllText(runtimeScriptPath);

	return script.Contains("if _is_visible(_chart_panel):", StringComparison.Ordinal)
		&& script.Contains("if key.keycode == KEY_ESCAPE:", StringComparison.Ordinal)
		&& script.Contains("_set_hub_controls_enabled(false)", StringComparison.Ordinal)
		&& script.Contains("_set_hub_controls_enabled(true)", StringComparison.Ordinal)
		&& script.Contains("button.disabled = not enabled", StringComparison.Ordinal)
		&& script.Contains("Control.FOCUS_ALL if enabled else Control.FOCUS_NONE", StringComparison.Ordinal);
}

static bool RouteDepartureOpensExplorationSurface()
{
	var repoRoot = FindRepoRoot();
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.gd");
	var hubScenePath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.tscn");
	var script = File.ReadAllText(runtimeScriptPath);
	var scene = File.ReadAllText(hubScenePath);

	return HasNode(scene, "ExplorationPanel", "PanelContainer")
		&& HasNode(scene, "ExplorationAdvanceButton", "Button")
		&& HasNode(scene, "ExplorationReturnButton", "Button")
		&& script.Contains("func _show_exploration_surface() -> void:", StringComparison.Ordinal)
		&& script.Contains("_chart_panel.visible = false", StringComparison.Ordinal)
		&& script.Contains("_exploration_panel.visible = true", StringComparison.Ordinal)
		&& script.Contains("_set_exploration_status()", StringComparison.Ordinal)
		&& script.Contains("资源压力：", StringComparison.Ordinal)
		&& script.Contains("威胁反馈：", StringComparison.Ordinal)
		&& script.Contains("船体状态：", StringComparison.Ordinal)
		&& script.Contains("恢复提示：", StringComparison.Ordinal);
}

static bool ExplorationPressureLoopMutatesAndPersists()
{
	var repoRoot = FindRepoRoot();
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.gd");
	var script = File.ReadAllText(runtimeScriptPath);

	return script.Contains("var _exploration_step := 0", StringComparison.Ordinal)
		&& script.Contains("func _on_exploration_advance_pressed() -> void:", StringComparison.Ordinal)
		&& script.Contains("_exploration_step = min(_exploration_step + 1, 3)", StringComparison.Ordinal)
		&& script.Contains("\"exploration_step\": _exploration_step", StringComparison.Ordinal)
		&& script.Contains("_exploration_step = max(0, int(parsed.get(\"exploration_step\", 0)))", StringComparison.Ordinal)
		&& script.Contains("搜索消耗 1", StringComparison.Ordinal)
		&& script.Contains("中威胁", StringComparison.Ordinal)
		&& script.Contains("94/100", StringComparison.Ordinal)
		&& script.Contains("一轮压力循环完成", StringComparison.Ordinal)
		&& script.Contains("S 保存，L 加载", StringComparison.Ordinal);
}

static bool HubSummarySyncsExplorationPressureState()
{
	var repoRoot = FindRepoRoot();
	var runtimeScriptPath = Path.Combine(repoRoot, "src", "scenes", "HubRuntime.gd");
	var script = File.ReadAllText(runtimeScriptPath);

	return script.Contains("_storage_value_label = find_child(\"StorageValue\"", StringComparison.Ordinal)
		&& script.Contains("_cargo_value_label = find_child(\"CargoValue\"", StringComparison.Ordinal)
		&& script.Contains("_hull_value_label = find_child(\"HullValue\"", StringComparison.Ordinal)
		&& script.Contains("_chart_station_label = find_child(\"ChartStation\"", StringComparison.Ordinal)
		&& script.Contains("_cargo_station_label = find_child(\"CargoStation\"", StringComparison.Ordinal)
		&& script.Contains("func _update_hub_summary() -> void:", StringComparison.Ordinal)
		&& script.Contains("_update_hub_summary()", StringComparison.Ordinal)
		&& script.Contains("云晶 x2", StringComparison.Ordinal)
		&& script.Contains("已用 180", StringComparison.Ordinal)
		&& script.Contains("完整度 94", StringComparison.Ordinal)
		&& script.Contains("压力循环完成 3/3", StringComparison.Ordinal)
		&& script.Contains("收益锁定 260/500", StringComparison.Ordinal);
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
