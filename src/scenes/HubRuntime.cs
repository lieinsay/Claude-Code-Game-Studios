using System;
using Godot;
using CloudWeaverVoyage.Presentation;

public partial class HubRuntime : Node2D
{
	private static readonly Vector2 HubPlayerStart = new(158, 610);
	private static readonly Vector2 ShipInteriorPlayerStart = new(246, 610);
	private static readonly Vector2 ExplorationPlayerStart = new(168, 610);
	private static readonly Vector2 OchreIslandPlayerStart = new(286, 536);
	private static readonly Rect2 HubWalkBounds = new(new Vector2(132, 380), new Vector2(1016, 252));
	private static readonly Rect2 ShipInteriorWalkBounds = new(new Vector2(196, 424), new Vector2(836, 208));
	private static readonly Rect2 ExplorationWalkBounds = new(new Vector2(132, 390), new Vector2(1016, 246));
	private static readonly Rect2 OchreIslandWalkBounds = new(new Vector2(140, 292), new Vector2(900, 278));
	private const string DurableProgressFileName = "cloudweaver_playable_progress.json";
	private const string DurableProgressPath = $"user://{DurableProgressFileName}";
	private const string QuarantinedProgressFileName = "cloudweaver_playable_progress.quarantine.json";
	private const string QuarantinedProgressPath = $"user://{QuarantinedProgressFileName}";
	private const string AuthoredContentPath = "src/presentation/playable_slice_authored_content.json";
	private const string OchreIslandScenePath = "res://src/scenes/ochre/OchreIslandScene.tscn";
	private const float PlayerSpeed = 260.0f;
	private const float InteractionRadius = 74.0f;
	private static readonly SceneUnitAuthoringFixture SceneUnitAuthoring = SceneUnitAuthoringFixture.Load(AuthoredContentPath);

	private readonly PlayableSliceDomainAdapter domain = new();
	private readonly OnboardingManager onboarding = new();
	private Control? hubRoot;
	private Control? playableLayer;
	private Control? sceneLayer;
	private Control? interactionLayer;
	private ColorRect? playerMarker;
	private Label? interactionPromptLabel;
	private ColorRect? hubShipEntryMarker;
	private ColorRect? hubShipExitMarker;
	private ColorRect? hubHelmMarker;
	private ColorRect? hubStorageMarker;
	private ColorRect? explorationSearchMarker;
	private ColorRect? explorationReturnMarker;
	private ColorRect? ochreOreMarker;
	private ColorRect? ochreReturnMarker;
	private ColorRect? explorationRouteProgressFill;
	private ColorRect? explorationThreatZone;
	private ColorRect? explorationSearchPulseFill;
	private ColorRect? explorationReturnPrepFill;
	private ColorRect? hubCargoLoadFill;
	private ColorRect? hubEngineWearOverlay;
	private Label? hubCabinStatusLabel;
	private Label? hubCargoStatusLabel;
	private Label? hubEngineStatusLabel;
	private readonly Godot.Collections.Array<CanvasItem> hubSceneItems = [];
	private readonly Godot.Collections.Array<CanvasItem> hubExteriorSceneItems = [];
	private readonly Godot.Collections.Array<CanvasItem> hubInteriorSceneItems = [];
	private readonly Godot.Collections.Array<CanvasItem> chartSceneItems = [];
	private readonly Godot.Collections.Array<CanvasItem> explorationSceneItems = [];
	private readonly Godot.Collections.Array<CanvasItem> ochreSceneItems = [];
	private ColorRect? chartMistSelectionFrame;
	private Label? explorationPointSemanticLabel;
	private Label? explorationThreatSemanticLabel;
	private Label? explorationExtractionSemanticLabel;
	private Label? ochreOreSemanticLabel;
	private Label? ochreReturnSemanticLabel;
	private Label? storageValueLabel;
	private Label? cargoValueLabel;
	private Label? hullValueLabel;
	private Label? chartStationLabel;
	private Label? cargoStationLabel;
	private Label? saveStatusLabel;
	private Label? runtimeHintLabel;
	private Label? footerLabel;
	private Button? deleteProgressButton;
	private Button? ochreDebugButton;
	private readonly Button?[] hubActionButtons = new Button?[5];
	private string selectedRoute = "";
	private string currentScreen = "hub";
	private string hubSpace = "exterior";
	private int explorationStep;
	private int searchPulseStage;
	private int returnPrepStage;
	private int ochreReturnPrepStage;
	private bool ochreOreHarvested;
	private Vector2 playerPosition = HubPlayerStart;
	private string nearestInteraction = "";
	private bool hasLoadableProgress;
	private bool pendingOverwriteConfirmation;
	private bool pendingDeleteConfirmation;
	private string lastDurableImportFailure = "";

	public override void _Ready()
	{
		CacheNodes();
		domain.RegisterOnboarding(onboarding);
		TryImportDurableProgressFromDisk(updateStatus: false);
		CreatePlayableLayer();
		WireButtons();
		ShowHub();
		if (hasLoadableProgress)
		{
			SetSaveStatus("检测到本地航行日志：点击读取恢复。");
		}
		else if (!string.IsNullOrWhiteSpace(lastDurableImportFailure))
		{
			SetSaveStatus($"本地存档已隔离：{lastDurableImportFailure}");
		}
		else
		{
			SetSaveStatus("暂无可读取航行日志：记录后可读取。");
		}
		onboarding.ObserveHubVisible(inputReachable: true, ownerStateAlreadyMutated: true);
		UpdateOnboardingHint();
	}

	public override void _Process(double delta)
	{
		if (playerMarker is null || currentScreen == "chart")
		{
			return;
		}

		var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (direction != Vector2.Zero)
		{
			playerPosition += direction.Normalized() * PlayerSpeed * (float)delta;
			playerPosition = ClampToCurrentWalkBounds(playerPosition);
			playerMarker.Position = playerPosition - (playerMarker.Size * 0.5f);
		}

		UpdateSpatialInteraction();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo)
		{
			return;
		}

		if (currentScreen == "exploration" || currentScreen == "ochre_dev")
		{
			if (key.Keycode == Key.Escape)
			{
				ShowHub();
				GetViewport().SetInputAsHandled();
			}
			else if (key.Keycode == Key.E)
			{
				TrySpatialInteraction();
				GetViewport().SetInputAsHandled();
			}
			else if (IsSaveShortcut(key))
			{
				OnSavePressed();
				GetViewport().SetInputAsHandled();
			}
			else if (IsLoadShortcut(key))
			{
				OnLoadPressed();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (currentScreen == "chart")
		{
			if (key.Keycode == Key.Escape)
			{
				ShowHub();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (key.Keycode == Key.M)
		{
			OnChartPressed();
			GetViewport().SetInputAsHandled();
		}
		else if (key.Keycode == Key.E)
		{
			TrySpatialInteraction();
			GetViewport().SetInputAsHandled();
		}
		else if (IsSaveShortcut(key))
		{
			OnSavePressed();
			GetViewport().SetInputAsHandled();
		}
		else if (IsLoadShortcut(key))
		{
			OnLoadPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	public void OnChartPressed()
	{
		if (currentScreen == "hub" && hubSpace != "interior")
		{
			SetFooter("需要先从岛上登船，进入驾驶舱航台后再打开航图。");
			return;
		}

		currentScreen = "chart";
		domain.OpenChart();
		ShowChartSurface();
		SetChartStatus("航图桌已展开：选择一条可读航线后确认离港。");
		UpdateOnboardingHint();
	}

	public void OnRouteMistPressed() => SelectDomainRoute("route.mist");

	public void OnDepartPressed()
	{
		if (string.IsNullOrEmpty(selectedRoute))
		{
			SetChartStatus("请选择航线后再确认出发。");
			return;
		}

		if (!domain.ConfirmDeparture())
		{
			SetChartStatus($"出航失败：{domain.Snapshot.LastStatus}");
			return;
		}

		currentScreen = "exploration";
		explorationStep = 0;
		searchPulseStage = 0;
		returnPrepStage = 0;
		playerPosition = ExplorationPlayerStart;
		ShowExplorationSurface();
		UpdateOnboardingHint();
	}

	public void OnExplorationAdvancePressed()
	{
		if (currentScreen != "exploration")
		{
			return;
		}

		UpdateSpatialInteraction();
		if (nearestInteraction != "exploration_search")
		{
			SetFooter("需要移动到漂浮残骸旁边再按 E 搜索。");
			return;
		}

		if (searchPulseStage < 2)
		{
			searchPulseStage += 1;
			UpdateExplorationMicroGameSemantics();
			SetFooter(searchPulseStage == 1
				? "扫描 1/3：校准雾灯残骸角度，再按 E 读取回声。"
				: "扫描 2/3：回声已锁定，再按 E 执行打捞脉冲。");
			return;
		}

		domain.AdvanceExploration();
		explorationStep = domain.Snapshot.ExplorationStep;
		searchPulseStage = 0;
		SetExplorationStatus();
		UpdateOnboardingHint();
		if (explorationStep >= 3)
		{
			SetFooter("一轮探索压力循环完成：回到空艇旁预热并驾驶返航。Ctrl+S 保存，Ctrl+L 加载。");
		}
		else
		{
			SetFooter("打捞完成：压力、威胁、船体反馈已更新；再次靠近残骸可启动下一轮扫描。Ctrl+S 保存，Ctrl+L 加载。");
		}
	}

	public void OnExplorationReturnPressed()
	{
		UpdateSpatialInteraction();
		if (currentScreen == "exploration" && nearestInteraction != "exploration_return")
		{
			SetFooter("需要移动到空艇返航舵旁边再按 E 准备返航。");
			return;
		}

		if (currentScreen == "exploration" && returnPrepStage < 1)
		{
			returnPrepStage += 1;
			UpdateExplorationMicroGameSemantics();
			SetFooter("返航 1/2：空艇引擎已预热，再按 E 驾驶返航。");
			return;
		}

		returnPrepStage = 0;
		domain.ReturnToHub();
		ShowHub();
		UpdateOnboardingHint();
	}

	public void OnOchreHarvestPressed()
	{
		if (currentScreen != "ochre_dev")
		{
			return;
		}

		UpdateSpatialInteraction();
		if (nearestInteraction != "ochre_ore")
		{
			SetFooter("需要移动到条带状铁矿旁边再按 E 采集。");
			return;
		}

		if (ochreOreHarvested)
		{
			SetFooter("条带状铁矿已采集：开发入口保留状态可读性，不写入正式 Resources 奖励。");
			return;
		}

		ochreOreHarvested = true;
		UpdateOchreSceneSemantics();
		SetFooter("条带状铁矿采集完成：已验证世界锚点和采集后状态；正式资源奖励仍待后续 runtime 集成。");
	}

	public void OnOchreReturnPressed()
	{
		if (currentScreen != "ochre_dev")
		{
			return;
		}

		UpdateSpatialInteraction();
		if (nearestInteraction != "ochre_return")
		{
			SetFooter("需要移动到赭石岛返航点旁边再按 E。");
			return;
		}

		if (ochreReturnPrepStage < 1)
		{
			ochreReturnPrepStage += 1;
			UpdateOchreSceneSemantics();
			SetFooter("赭石岛返航 1/2：返航锚点已预热，再按 E 返回 Hub。");
			return;
		}

		ochreReturnPrepStage = 0;
		playerPosition = HubPlayerStart;
		ShowHub();
		SetFooter("已从赭石岛开发入口返回 Hub；正式 playable route 未被替换。");
	}

	public void OnSavePressed()
	{
		if (Godot.FileAccess.FileExists(DurableProgressPath) && !pendingOverwriteConfirmation)
		{
			pendingOverwriteConfirmation = true;
			pendingDeleteConfirmation = false;
			SetSaveStatus("覆盖确认：再次记录将覆盖当前本地航行日志。");
			RefreshSaveDeleteAffordance();
			return;
		}

		pendingOverwriteConfirmation = false;
		pendingDeleteConfirmation = false;
		onboarding.ObserveSaveLoadAwareness(visibleOrUsed: true, ownerStateAlreadyMutated: true);
		var result = domain.SaveSceneState(new PlayableSliceSceneState(
			currentScreen,
			selectedRoute,
			explorationStep,
			playerPosition.X,
			playerPosition.Y,
			LabelText("Footer")));
		if (!result.Success)
		{
			SetSaveStatus($"保存失败：{result.Reason}");
			UpdateOnboardingHint();
			return;
		}

		var durableSaved = TryWriteDurableProgressToDisk(out var durableReason);
		hasLoadableProgress = durableSaved;
		lastDurableImportFailure = "";
		SetSaveStatus(durableSaved
			? $"保存完成：本地航行日志 gen {result.Generation} 可读取"
			: $"保存完成：核心进度 gen {result.Generation} / 本地写入失败 {durableReason}");
		RefreshSaveDeleteAffordance();
		UpdateOnboardingHint();
	}

	public void OnLoadPressed()
	{
		pendingOverwriteConfirmation = false;
		pendingDeleteConfirmation = false;
		if (!Godot.FileAccess.FileExists(DurableProgressPath) && Godot.FileAccess.FileExists(QuarantinedProgressPath))
		{
			hasLoadableProgress = false;
			lastDurableImportFailure = "存档校验失败，已隔离；可重新保存新进度。";
			SetSaveStatus($"本地存档不可用：{lastDurableImportFailure}");
			RefreshSaveDeleteAffordance();
			return;
		}

		var importResult = TryImportDurableProgressFromDisk(updateStatus: false);
		if (!importResult && !hasLoadableProgress)
		{
			SetSaveStatus(string.IsNullOrWhiteSpace(lastDurableImportFailure)
				? "暂无可读取航行日志：请先记录。"
				: $"本地存档不可用：{lastDurableImportFailure}");
			RefreshSaveDeleteAffordance();
			return;
		}

		var (result, restored) = domain.LoadSceneState();
		if (!result.Success)
		{
			SetSaveStatus($"加载失败：{result.Reason}");
			RefreshSaveDeleteAffordance();
			return;
		}

		currentScreen = string.IsNullOrWhiteSpace(restored.Screen) ? "hub" : restored.Screen;
		selectedRoute = restored.Route ?? "";
		explorationStep = Math.Max(0, restored.ExplorationStep);
		playerPosition = new Vector2(restored.PlayerX, restored.PlayerY);
		var restoredScreenName = currentScreen switch
		{
			"chart" => "航图桌",
			"exploration" => "雾海搜撤记录",
			"ochre_dev" => "赭石岛开发入口",
			_ => "空艇停泊区",
		};
		SetSaveStatus($"加载完成：本地航行日志 gen {result.Generation} / {restoredScreenName}");

		if (currentScreen == "chart")
		{
			ShowChartSurface();
			SetChartStatus($"已从存档恢复航线：{selectedRoute}");
		}
		else if (currentScreen == "exploration")
		{
			ShowExplorationSurface();
			SetSaveStatus($"加载完成：本地航行日志 gen {result.Generation} / 雾海搜撤记录");
		}
		else if (currentScreen == "ochre_dev")
		{
			ShowOchreDevSurface();
			SetSaveStatus($"加载完成：本地航行日志 gen {result.Generation} / 赭石岛开发入口");
		}
		else
		{
			ShowHub();
		}
		RefreshSaveDeleteAffordance();
		UpdateOnboardingHint();
	}

	/// <summary>Handles the two-step delete confirmation for local durable progress.</summary>
	public void OnDeleteProgressPressed()
	{
		var hasAnyLocalProgress = Godot.FileAccess.FileExists(DurableProgressPath) || Godot.FileAccess.FileExists(QuarantinedProgressPath);
		if (!hasAnyLocalProgress)
		{
			pendingOverwriteConfirmation = false;
			pendingDeleteConfirmation = false;
			SetSaveStatus("没有可删除的本地航行日志。");
			RefreshSaveDeleteAffordance();
			return;
		}

		if (!pendingDeleteConfirmation)
		{
			pendingOverwriteConfirmation = false;
			pendingDeleteConfirmation = true;
			SetSaveStatus("删除确认：再次点击删除将移除本地航行日志与隔离副本。");
			RefreshSaveDeleteAffordance();
			return;
		}

		using var directory = DirAccess.Open("user://");
		directory?.Remove(DurableProgressFileName);
		directory?.Remove(QuarantinedProgressFileName);
		hasLoadableProgress = false;
		lastDurableImportFailure = "";
		pendingOverwriteConfirmation = false;
		pendingDeleteConfirmation = false;
		SetSaveStatus("已删除本地航行日志：记录后可再次读取。");
		RefreshSaveDeleteAffordance();
	}

	public void ShowHub()
	{
		currentScreen = "hub";
		hubSpace = "exterior";
		SetHubControlsEnabled(true);
		UpdateHubSummary();
		SetWorldMode("hub");
		SetFooter("玻璃港停泊浮岛：移动到登船坡道按 E 进入云织号。航行日志可用按钮或 Ctrl+S / Ctrl+L。");
		GrabButton("SaveButton");
		UpdateOnboardingHint();
	}

	public void DebugEnterOchreIslandScene()
	{
		currentScreen = "ochre_dev";
		ochreOreHarvested = false;
		ochreReturnPrepStage = 0;
		playerPosition = OchreIslandPlayerStart;
		ShowOchreDevSurface();
	}

	public void OnOchreDebugButtonPressed()
	{
		if (!OS.IsDebugBuild())
		{
			return;
		}

		DebugEnterOchreIslandScene();
	}

	public void TrySpatialInteraction()
	{
		UpdateSpatialInteraction();
		if (nearestInteraction == "hub_enter_ship")
		{
			EnterShipInterior();
		}
		else if (nearestInteraction == "hub_exit_ship")
		{
			ExitShipInterior();
		}
		else if (nearestInteraction == "hub_helm")
		{
			OnChartPressed();
		}
		else if (nearestInteraction == "hub_storage")
		{
			SetFooter("仓储已检查：基础补给、信标水晶和修理包状态已同步。");
			SetSaveStatus("交互完成：仓储状态可见");
			UpdateOnboardingHint();
		}
		else if (nearestInteraction == "exploration_search")
		{
			OnExplorationAdvancePressed();
		}
		else if (nearestInteraction == "exploration_return")
		{
			OnExplorationReturnPressed();
		}
		else if (nearestInteraction == "ochre_ore")
		{
			OnOchreHarvestPressed();
		}
		else if (nearestInteraction == "ochre_return")
		{
			OnOchreReturnPressed();
		}
		else
		{
			SetFooter("附近没有可交互点：移动到标记旁按 E。");
		}
	}

	public void EnterShipInterior()
	{
		if (currentScreen != "hub")
		{
			return;
		}

		hubSpace = "interior";
		playerPosition = ShipInteriorPlayerStart;
		SetWorldMode("hub");
		SetFooter("已进入云织号船内：沿走廊前往驾驶舱、货舱或轮机间。");
	}

	public void ExitShipInterior()
	{
		if (currentScreen != "hub")
		{
			return;
		}

		hubSpace = "exterior";
		playerPosition = HubPlayerStart;
		SetWorldMode("hub");
		SetFooter("已回到岛上停泊区：飞船停在码头，可再次登船。");
	}

	public void DebugSetPlayerPosition(Vector2 position)
	{
		playerPosition = ClampToCurrentWalkBounds(position);
		if (playerMarker is not null)
		{
			playerMarker.Position = playerPosition - (playerMarker.Size * 0.5f);
		}
		UpdateSpatialInteraction();
	}

	public Vector2 DebugPlayerPosition() => playerPosition;

	public Vector2 DebugWalkBoundsSize() => CurrentWalkBounds().Size;

	public bool DebugPlayerWithinWalkBounds() => CurrentWalkBounds().HasPoint(playerPosition);

	public string DebugInteractionPrompt() => interactionPromptLabel?.Text ?? "";

	public bool DebugNodeVisible(string nodeName) =>
		FindChild(nodeName, true, false) is CanvasItem item && item.Visible;

	/// <summary>Returns the current nearest spatial interaction id for smoke tests.</summary>
	public string DebugNearestInteraction() => nearestInteraction;

	/// <summary>Reports whether a named interaction marker is visible for smoke tests.</summary>
	public bool DebugMarkerVisible(string nodeName) =>
		FindChild(nodeName, true, false) is Control marker && marker.Visible;

	/// <summary>Returns the current player distance to a named interaction marker for smoke tests.</summary>
	public float DebugDistanceToMarker(string nodeName) =>
		FindChild(nodeName, true, false) is Control marker ? DistanceToMarker(marker) : float.PositiveInfinity;

	/// <summary>Returns descendant node paths whose names contain the supplied fragment for smoke diagnostics.</summary>
	public Godot.Collections.Array<string> DebugDescendantNames(string nameFragment)
	{
		var matches = new Godot.Collections.Array<string>();
		CollectDescendantNames(this, nameFragment, matches);
		return matches;
	}

	public string DebugHubSpace() => hubSpace;

	public string DebugCurrentScreen() => currentScreen;

	public Godot.Collections.Dictionary DebugCurrentScenePhysicsContract()
	{
		var sceneId = currentScreen == "exploration"
			? "exploration_mist_island"
			: currentScreen == "ochre_dev"
				? "ochre_island_scene"
			: hubSpace == "interior" ? "hub_ship_interior" : "hub_island_dock";
		return DebugScenePhysicsContract(sceneId);
	}

	public Godot.Collections.Dictionary DebugScenePhysicsContract(string sceneId)
	{
		return sceneId switch
		{
			"hub_island_dock" => BuildScenePhysicsContract(
				sceneId,
				"水平场景",
				HubWalkBounds,
				"ground plane supports up/down/left/right movement; height_only_layer uses airship envelope, mast silhouettes, and ground-shadow/landing-height cues as visual references only",
				"primary_walkable_layer=hub_dock_ground; walkable_layer: island_dock_path; transition_layer: boarding_ramp_to_ship_interior; height_only_layer: airship_envelope, mast_silhouette; blocked_layer: waterline, dock_posts, docked_ship_hull; visual_layer: sky_horizon",
				"N/A true for floor_cutaway/interior_instance; no passable path behind docked ship hull in current slice, so behind_object_reveal=N/A true; foreground occluders must use occluder_peek if added",
				"N/A true: single exterior ground floor; floor_id=hub_dock_ground; floor_index=0; is_active_floor=true; visibility_mode=full_visible; walkable_bounds=HubWalkBounds; vertical_connectors=boarding_ramp_to_ship_interior; occluders_hidden_above=none; interactions_enabled=boarding_ramp",
				"hub_dock_ground",
				"hub_dock_ground",
				0,
				true,
				"full_visible",
				"boarding_ramp_to_ship_interior",
				"none",
				"boarding_ramp",
				"N/A true: no passable behind-object route in current Hub exterior slice; docked ship hull remains blocking_static and does not disappear",
				"2.2m player height = 28px marker; walkable dock/island width 1016px",
				"z_world_background < z_scene_units < z_interaction_markers < z_prompt",
				"blocking_static: island edge, waterline, dock posts, docked ship hull; soft_overlap: boarding ramp; height_marker: airship envelope and mast silhouettes",
				"water: blocking_static hazard boundary; glass: none; mirror: none; elastic: none; pushable: none in current slice",
				"Clamp player back into HubWalkBounds; boarding ramp remains soft_overlap so stuck states can exit through E interaction",
				0),
			"hub_ship_interior" => BuildScenePhysicsContract(
				sceneId,
				"垂直场景",
				ShipInteriorWalkBounds,
				"left/right primary movement across ship deck; room depth is visual; future up/down traversal must use declared ladders/stairs or floor connectors before implementation",
				"floor_id=ship_deck_01; floor_index=1; walkable_layer: cockpit_bay, cargo_bay, engine_bay, exit_threshold; visual_layer: upper_hull_shell, cockpit_window; transition_layer: exit_door_to_hub_dock; blocked_layer: hull_outline, bay_separators; depth_layer: front_wall_removed_room_depth",
				"front_wall_removed + active_floor_focus; current deck stays fully readable; upper hull/window line remains visual reference; non-active future decks must fade/lock interactions before implementation readiness",
				"floor_id=ship_deck_01; floor_index=1; is_active_floor=true; visibility_mode=front_wall_removed; walkable_bounds=ShipInteriorWalkBounds; vertical_connectors=future_ladders_stairs_declared_not_implemented; occluders_hidden_above=upper_hull_front_wall; interactions_enabled=helm,storage,engine,exit",
				"ship_deck_01",
				"ship_deck_01",
				1,
				true,
				"front_wall_removed",
				"future_ladders_stairs_declared_not_implemented",
				"upper_hull_front_wall",
				"helm,storage,engine,exit",
				"N/A true: interior uses front_wall_removed + active_floor_focus, not behind-object passage",
				"2.2m player height = 28px marker; cockpit/cargo/engine bays each read as one room-scale unit",
				"z_world_background < z_hull_shell < z_room_volumes < z_props < z_interaction_markers",
				"blocking_static: hull outline, bay separators, exterior door frame; soft_overlap: helm, storage, engine, exit anchors; height_marker: upper hull and window line",
				"stairs_ladders: represented as future vertical connectors; glass: cockpit window visual-only; mirror: none; elastic: none; pushable: storage crates not pushable in current slice",
				"Clamp player into ShipInteriorWalkBounds; exit door soft_overlap returns to hub_island_dock if pathing feels trapped",
				0),
			"exploration_mist_island" => BuildScenePhysicsContract(
				sceneId,
				"水平场景",
				ExplorationWalkBounds,
				"ground plane supports up/down/left/right movement; mast, beacon beam, and threat zone are height_only cues with ground-shadow/landing-height readability, not extra walkable floors",
				"primary_walkable_layer=mist_island_path; walkable_layer: shoreline_path, search_wreck_approach, return_ship_approach; transition_layer: return_helm_to_hub_dock; height_only_layer: mast, beacon_beam, threat_zone_overlay; blocked_layer: sea, cliff_edge, wreck_body, return_ship_hull; visual_layer: mist_horizon",
				"N/A true for floor_cutaway/interior_instance; search wreck and return ship are blocking units with no passable behind-path in current slice, so behind_object_reveal=N/A true; future foreground occluders must declare occluder_peek",
				"N/A true: single exterior island floor; floor_id=mist_island_path; floor_index=0; is_active_floor=true; visibility_mode=full_visible; walkable_bounds=ExplorationWalkBounds; vertical_connectors=return_helm_to_hub_dock; occluders_hidden_above=none; interactions_enabled=search_scan_arc,return_helm",
				"mist_island_path",
				"mist_island_path",
				0,
				true,
				"full_visible",
				"return_helm_to_hub_dock",
				"none",
				"search_scan_arc,return_helm",
				"N/A true: no passable behind-object route in current Exploration slice; wreck and return ship hull keep collision/interaction identity",
				"2.2m player height = 28px marker; search wreck is about 6 player-widths; return ship is about 5 player-widths",
				"z_world_background < z_island_body_path < z_wreck_return_ship_threat_units < z_interaction_markers",
				"blocking_static: island edge, sea, cliff edge, return ship hull, search wreck body; soft_overlap: search scan arc, return helm; height_marker: mast, beacon beam, threat zone overlay",
				"water: blocking_static hazard boundary; glass: none; mirror: none; elastic: none; pushable: none in current slice",
				"Clamp player into ExplorationWalkBounds; search and return stay as soft_overlap anchors so failed movement cannot block progression",
				0),
			"ochre_island_scene" => BuildScenePhysicsContract(
				sceneId,
				"水平场景",
				new Rect2(new Vector2(140, 292), new Vector2(900, 278)),
				"ground plane supports up/down/left/right movement; height_only cues are visual-only in first pass and never imply jump/fly traversal",
				"primary_walkable_layer=ochre_island_ground_01; walkable_layer: ochre_walk_path; transition_layer: ochre_return_anchor_to_voyage_or_hub; height_only_layer: none in first pass; blocked_layer: rock_wall_boundary, cloudsea_boundary, ore_body_when_blocking; visual_layer: ochre_rock_face",
				"N/A true for floor_cutaway/interior_instance; behind_object_reveal=N/A true because banded iron ore and return beacon keep collision/interaction identity and do not hide passable behind-paths",
				"N/A true: single exterior island floor; floor_id=ochre_island_ground_01; floor_index=0; is_active_floor=true; visibility_mode=full_visible; walkable_bounds=OchreIslandScene ground; vertical_connectors=ochre_return_anchor_to_voyage_or_hub; occluders_hidden_above=none; interactions_enabled=banded_iron_ore_anchor,ochre_return_anchor",
				"ochre_island_ground_01",
				"ochre_island_ground_01",
				0,
				true,
				"full_visible",
				"ochre_return_anchor_to_voyage_or_hub",
				"none",
				"banded_iron_ore_anchor,ochre_return_anchor",
				"N/A true: no passable behind-object route in current Ochre Island slice; ore and return anchor keep collision and interaction identity",
				"2.2m player height = 28px marker; banded ore is about 3 player-widths; return anchor clear width >= 1.1x player",
				"z_world_background < z_island_ground_path < z_ore_return_boundary_units < z_interaction_markers",
				"blocking_static: ochre island edge, rock wall, cloudsea boundary; soft_overlap: player marker, walk path, banded iron ore anchor, return anchor",
				"ore=resource_node + breakable + trigger_only; cloudsea=gameplay_affecting blocking_static boundary; glass=none; mirror=none; elastic=none; pushable=none in current slice",
				"Clamp player into OchreIslandScene walk bounds; ore and return remain soft_overlap anchors so failed movement cannot block progression",
				6),
			_ => new Godot.Collections.Dictionary
			{
				["scene_id"] = sceneId,
				["contract_complete"] = false,
				["diagnostic_error"] = "Unknown scene physics contract.",
				["error"] = "Unknown scene physics contract.",
			},
		};
	}

	private static Godot.Collections.Dictionary BuildScenePhysicsContract(
		string sceneId,
		string sceneType,
		Rect2 walkBounds,
		string movementReadability,
		string layerHeightModel,
		string cutawayRevealModel,
		string floorState,
		string primaryWalkableLayer,
		string floorId,
		int floorIndex,
		bool isActiveFloor,
		string visibilityMode,
		string verticalConnectors,
		string occludersHiddenAbove,
		string interactionsEnabled,
		string behindObjectReveal,
		string scaleReference,
		string occlusionPolicy,
		string collisionSemantics,
		string specialSurfaces,
		string recoveryRule,
		int authoredPhysicalUnitCount)
	{
		var sceneUnitCatalog = BuildSceneUnitCatalog(sceneId);
		var authoringDiagnostics = BuildAuthoringDiagnostics(sceneId);
		var authoringApplies = HasSceneUnitAuthoring(sceneId);
		return new Godot.Collections.Dictionary
		{
			["scene_id"] = sceneId,
			["contract_complete"] = true,
			["scene_type"] = sceneType,
			["movement_plane"] = sceneType == "垂直场景"
				? "left_right_primary_with_room_depth_and_future_vertical_connectors"
				: "ground_plane_four_directional",
			["movement_readability"] = movementReadability,
			["layer_height_model_ready"] = true,
			["cutaway_reveal_ready"] = true,
			["layer_height_model"] = layerHeightModel,
			["cutaway_reveal_model"] = cutawayRevealModel,
			["floor_state"] = floorState,
			["primary_walkable_layer"] = primaryWalkableLayer,
			["floor_id"] = floorId,
			["floor_index"] = floorIndex,
			["is_active_floor"] = isActiveFloor,
			["visibility_mode"] = visibilityMode,
			["vertical_connectors"] = verticalConnectors,
			["occluders_hidden_above"] = occludersHiddenAbove,
			["interactions_enabled"] = interactionsEnabled,
			["behind_object_reveal"] = behindObjectReveal,
			["identity_occlusion_max_seconds"] = 1.0f,
			["walk_bounds_position"] = walkBounds.Position,
			["walk_bounds_size"] = walkBounds.Size,
			["scale_reference"] = scaleReference,
			["occlusion_policy"] = occlusionPolicy,
			["collision_semantics"] = collisionSemantics,
			["special_surfaces"] = specialSurfaces,
			["unit_catalog_ready"] = authoredPhysicalUnitCount > 0,
			["collision_ready"] = true,
			["occlusion_ready"] = true,
			["scale_ready"] = true,
			["special_surface_ready"] = true,
			["physical_behavior_ready"] = true,
			["recovery_ready"] = true,
			["scene_unit_catalog"] = sceneUnitCatalog,
			["scene_unit_authoring_source"] = authoringApplies
				? AuthoredContentPath
				: "not_migrated_first_slice",
			["scene_unit_authoring_ready"] = !authoringApplies || authoringDiagnostics.Count == 0,
			["prototype_instance_linkage_ready"] = authoringApplies && authoringDiagnostics.Count == 0,
			["scene_unit_authoring_diagnostics"] = authoringDiagnostics,
			["collision_table"] = BuildCollisionTable(sceneId),
			["occlusion_layers"] = BuildOcclusionLayers(sceneId),
			["scale_table"] = BuildScaleTable(sceneId),
			["special_surface_table"] = BuildSpecialSurfaceTable(sceneId),
			["asset_replacement_rule"] = "Formal asset replacement must preserve collision footprint, occlusion behavior, interaction radius, and player_unit-relative size readability unless this Scene Physics Contract is re-reviewed.",
			["physical_unit_source_layer"] = "world_playable_scene",
			["ui_evidence_allowed"] = false,
			["dynamic_behaviors"] = BuildPhysicalBehaviorCatalog(sceneId),
			["behavior_priority_table"] = BuildBehaviorPriorityTable(sceneId),
			["behavior_conflict_rule"] = "effective_behavior = highest_priority(applicable_behavior_tags); missing priority blocks implementation readiness",
			["behavior_fallback_rules"] = BuildBehaviorFallbackRules(sceneId),
			["missing_priority_blocks_readiness"] = true,
			["stuck_recovery_seconds"] = 2.0f,
			["recovery_table"] = BuildRecoveryTable(sceneId, recoveryRule),
			["recovery_rule"] = recoveryRule,
			["authored_physical_unit_count"] = authoredPhysicalUnitCount,
			["source_gdd"] = "design/gdd/scene-physics-unit-system.md",
		};
	}

	private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildSceneUnitCatalog(string sceneId)
	{
		return sceneId switch
		{
			"ochre_island_scene" => BuildAuthoredSceneUnitCatalog(sceneId),
			_ => [],
		};
	}

	private static bool HasSceneUnitAuthoring(string sceneId) =>
		sceneId is "ochre_island_scene";

	private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildAuthoredSceneUnitCatalog(string sceneId)
	{
		var catalog = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		foreach (var instance in SceneUnitAuthoring.InstancesForScene(sceneId))
		{
			if (!SceneUnitAuthoring.Prototypes.TryGetValue(instance.PrototypeId, out var prototype))
			{
				continue;
			}

			var item = BuildSceneUnit(
				instance.UnitId,
				prototype.UnitType,
				prototype.Collision,
				prototype.OcclusionLayer,
				prototype.ScaleRule,
				prototype.SourceLayer,
				prototype.UiEvidenceAllowed);
			item["prototype_id"] = prototype.PrototypeId;
			item["prototype_classification"] = prototype.PrototypeClassification;
			item["instance_id"] = instance.InstanceId;
			item["scene_spec"] = instance.SceneSpec;
			item["godot_node_path"] = instance.GodotNodePath;
			item["floor_id"] = instance.FloorId;
			item["floor_index"] = instance.FloorIndex;
			item["placement_layer"] = instance.Layer;
			item["placement_position"] = new Vector2(instance.X, instance.Y);
			item["source_gdd"] = prototype.SourceGdd;
			item["domain_owner"] = prototype.DomainOwner;
			item["state_hook"] = instance.StateHook;
			item["interaction_anchor_id"] = instance.InteractionAnchorId;
			catalog.Add(item);
		}

		return catalog;
	}

	private static Godot.Collections.Array<string> BuildAuthoringDiagnostics(string sceneId)
	{
		var diagnostics = new Godot.Collections.Array<string>();
		if (!HasSceneUnitAuthoring(sceneId))
		{
			return diagnostics;
		}

		foreach (var diagnostic in SceneUnitAuthoring.ValidateScene(sceneId))
		{
			diagnostics.Add(diagnostic);
		}

		return diagnostics;
	}

	private static Godot.Collections.Dictionary BuildSceneUnit(
		string unitId,
		string unitType,
		string collision,
		string occlusionLayer,
		string scaleRule,
		string sourceLayer,
		bool uiEvidenceAllowed)
	{
		return new Godot.Collections.Dictionary
		{
			["unit_id"] = unitId,
			["unit_type"] = unitType,
			["collision"] = collision,
			["occlusion_layer"] = occlusionLayer,
			["scale_rule"] = scaleRule,
			["source_layer"] = sourceLayer,
			["ui_evidence_allowed"] = uiEvidenceAllowed,
		};
	}

	private static string BuildCollisionTable(string sceneId) =>
		sceneId switch
		{
			"hub_island_dock" => "blocking_static: hub_island_main_mass, hub_docked_ship_hull, hub_waterline; soft_overlap: player_marker, hub_dock_plank_walkway, hub_boarding_ramp; height_marker: hub_airship_envelope",
			"hub_ship_interior" => "blocking_static: hub_interior_hull_outline, hub_interior_cockpit_bay, hub_interior_cargo_bay, hub_interior_engine_bay, storage_crate_prop, cockpit_window_glass, upper_hull_front_wall; soft_overlap: player_marker, helm_console_prop, ship_exit_threshold",
			"exploration_mist_island" => "blocking_static: exploration_island_mass, exploration_cliff_edge, return_ship_hull, mist_sea_boundary; soft_overlap: player_marker, exploration_island_path, search_wreck_prop, return_helm_anchor, mist_horizon_fog; height_marker: search_wreck_mast, return_beacon_beam, exploration_threat_zone",
			"ochre_island_scene" => "blocking_static: ochre_island_mass, ochre_cloudsea_boundary; soft_overlap: player_marker, ochre_island_path, banded_iron_ore, ochre_return_anchor",
			_ => "",
		};

	private static string BuildOcclusionLayers(string sceneId) =>
		sceneId switch
		{
			"hub_ship_interior" => "background: cockpit_window_glass; midground_floor: ship_deck_01; midground_object: room volumes, props, player; foreground_occluder: upper_hull_front_wall; height_shadow: upper hull/window references; ui_overlay: not physical evidence",
			_ => "background: horizon/fog/sky; midground_floor: walkable ground/path; midground_object: player, interactables, blockers, landmarks; foreground_occluder: only if declared with occluder_peek; height_shadow: mast/beacon/envelope cues; ui_overlay: not physical evidence",
		};

	private static string BuildScaleTable(string sceneId) =>
		sceneId switch
		{
			"hub_island_dock" => "player_unit=1.0; door_or_passage hub_boarding_ramp clear width >= 1.1x player; landmark hub_docked_ship_hull >= 2.0x player; height_marker hub_airship_envelope visual-only",
			"hub_ship_interior" => "player_unit=1.0; room bays are room-scale landmarks; helm/storage/engine anchors are 0.8-1.3x player-readable interactables; exit threshold clear width >= 1.1x player",
			"exploration_mist_island" => "player_unit=1.0; search_wreck about 6 player-widths; return_ship about 5 player-widths; beacon/mast height markers visual-only; path clear width >= 1.1x player",
			"ochre_island_scene" => "player_unit=1.0; banded_iron_ore resource node 1.5-2.5x player width; return anchor clear width >= 1.1x player; island mass reads as landmark >= 2.0x player",
			_ => "",
		};

	private static string BuildSpecialSurfaceTable(string sceneId) =>
		sceneId switch
		{
			"hub_island_dock" => "water=gameplay_affecting blocking_static hazard boundary with no passability implication; glass=none; mirror=none; fog/cloud=visual_only sky backdrop",
			"hub_ship_interior" => "glass=cockpit_window visual_only blocking_static, transparent but not passable/interactable; mirror=none; water=none; reflective_metal=visual_only hull trim",
			"exploration_mist_island" => "water=gameplay_affecting blocking_static sea boundary; fog/cloud=visual_only mist horizon with no collision/passability implication; glass=none; mirror=none; ledge_or_void=blocking_static cliff edge",
			"ochre_island_scene" => "cloudsea=gameplay_affecting blocking_static boundary with no passability implication; ore=resource_node + trigger_only + breakable state; glass=none; mirror=none; water=none",
			_ => "",
		};

	private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildPhysicalBehaviorCatalog(string sceneId)
	{
		return sceneId switch
		{
			"hub_island_dock" => new Godot.Collections.Array<Godot.Collections.Dictionary>
			{
				BuildPhysicalBehavior("hub_waterline", "hazardous", "hazardous + blocking_static", "deep water boundary; blocks ground units; no automatic damage in current slice", "shoreline stop feedback and path clamp", "player_unit, pushable_unit", 80, "hazardous boundary wins over visual water; no passability inference", "clamp player back into HubWalkBounds", "world_playable_scene", false),
				BuildPhysicalBehavior("hub_boarding_ramp", "trigger_only", "trigger_only + soft_overlap", "spatial Use anchor; no entity collision; requires proximity and Use dispatch", "boarding prompt near ramp", "player_unit", 30, "trigger_only never changes collision footprint", "escape interaction returns to ship interior or remains on dock if rejected", "world_playable_scene", false),
				BuildPhysicalBehavior("hub_airship_envelope", "visual_only_height_marker", "height_marker + visual_only", "height cue only; no collision, push, hazard, or attraction", "large envelope silhouette and height cue", "player_unit readability only", 10, "visual_only loses to any gameplay-affecting behavior", "no stuck state possible; ignore for movement", "world_playable_scene", false),
			},
			"hub_ship_interior" => new Godot.Collections.Array<Godot.Collections.Dictionary>
			{
				BuildPhysicalBehavior("ship_exit_threshold", "trigger_only", "trigger_only + soft_overlap", "spatial exit anchor; no entity collision; requires proximity and Use dispatch", "exit prompt at threshold", "player_unit", 30, "trigger_only never changes room collision", "escape interaction returns to hub_island_dock", "world_playable_scene", false),
				BuildPhysicalBehavior("storage_crate_prop", "blocking_static", "blocking_static", "static crate blocks readability footprint; pushable behavior is not implemented until a later contract adds priority", "solid crate silhouette", "player_unit", 20, "blocking_static applies unless a future pushable tag with priority is declared", "clamp player into ShipInteriorWalkBounds; crate does not move", "world_playable_scene", false),
				BuildPhysicalBehavior("cockpit_window_glass", "visual_only_glass", "glass_clear + visual_only", "transparent surface; not passable and not interactable", "window transparency, no Use prompt", "player_unit readability only", 10, "visual_only cannot imply passage or interaction", "no stuck state possible; ignore for movement", "world_playable_scene", false),
			},
			"exploration_mist_island" => new Godot.Collections.Array<Godot.Collections.Dictionary>
			{
				BuildPhysicalBehavior("mist_sea_boundary", "hazardous", "hazardous + water_deep + blocking_static", "deep water/void boundary; blocks ground units; no current/wind force in current slice", "shoreline, cliff, and sea feedback", "player_unit, pushable_unit", 90, "hazardous resolves before water visual and before trigger_only", "clamp player back into ExplorationWalkBounds", "world_playable_scene", false),
				BuildPhysicalBehavior("exploration_threat_zone", "hazardous_warning", "hazardous + height_marker + visual_warning", "world-layer threat warning only; domain threat consequences stay with ExplorationManager", "visible threat zone overlay and text synced to domain state", "player_unit", 70, "hazardous warning wins over visual height marker but does not own damage", "safe-floor return via route/return ship flow", "world_playable_scene", false),
				BuildPhysicalBehavior("search_wreck_prop", "trigger_only", "trigger_only + soft_overlap", "three-step scan anchor; no entity collision; requires proximity and Use dispatch", "scan calibration, echo lock, salvage pulse feedback", "player_unit", 40, "trigger_only is ignored until proximity and Use gate pass", "reset scan stage by moving away or returning to Hub; no forced stuck state", "world_playable_scene", false),
				BuildPhysicalBehavior("return_helm_anchor", "trigger_only", "trigger_only + soft_overlap", "two-step return anchor; no entity collision; requires proximity and Use dispatch", "engine preheat and piloting prompt", "player_unit", 35, "trigger_only cannot override hazardous boundary clamp", "escape interaction returns to hub_island_dock after preheat", "world_playable_scene", false),
				BuildPhysicalBehavior("mist_horizon_fog", "visual_only_fog", "fog_or_cloud + visual_only", "atmospheric fog; no collision, slow, blindness, or current/wind force", "mist backdrop only", "player_unit readability only", 10, "visual_only loses to every gameplay-affecting tag", "no stuck state possible; ignore for movement", "world_playable_scene", false),
			},
			"ochre_island_scene" => new Godot.Collections.Array<Godot.Collections.Dictionary>
			{
				BuildPhysicalBehavior("ochre_cloudsea_boundary", "hazardous", "hazardous + blocking_static", "cloudsea island edge blocks ground units; no damage in current slice", "cloudsea boundary and clamp feedback", "player_unit, pushable_unit", 80, "hazardous boundary wins over trigger_only anchors", "clamp player back into OchreIslandScene walk bounds", "world_playable_scene", false),
				BuildPhysicalBehavior("banded_iron_ore", "resource_node", "resource_node + trigger_only + breakable", "spatial Use anchor; harvest changes ore state and requests Resources reward in later runtime integration", "ore available/harvested visual state", "player_unit", 45, "resource_node trigger never changes collision footprint unless a future mining contract declares it", "keep ore visible and mark blocked/harvested; never replace with UI-only reward", "world_playable_scene", false),
				BuildPhysicalBehavior("ochre_return_anchor", "trigger_only", "trigger_only + soft_overlap", "spatial return anchor; no entity collision; requires proximity and Use dispatch", "return beacon greybox prompt", "player_unit", 35, "trigger_only cannot override hazardous boundary clamp", "return to voyage/hub flow when Navigation integration is wired", "world_playable_scene", false),
				BuildPhysicalBehavior("ochre_rock_face", "visual_only_landmark", "visual_only + height_only", "readability landmark; no collision, reward, or passability rule", "ochre rock face silhouette", "player_unit readability only", 10, "visual_only loses to every gameplay-affecting tag", "no stuck state possible; ignore for movement", "world_playable_scene", false),
			},
			_ => [],
		};
	}

	private static Godot.Collections.Dictionary BuildPhysicalBehavior(
		string unitId,
		string behaviorLabel,
		string applicableBehaviorTags,
		string parameters,
		string feedback,
		string affectedUnitTypes,
		int conflictPriority,
		string fallbackRule,
		string recoveryAction,
		string sourceLayer,
		bool uiEvidenceAllowed)
	{
		return new Godot.Collections.Dictionary
		{
			["unit_id"] = unitId,
			["behavior_label"] = behaviorLabel,
			["applicable_behavior_tags"] = applicableBehaviorTags,
			["parameters"] = parameters,
			["feedback"] = feedback,
			["affected_unit_types"] = affectedUnitTypes,
			["conflict_priority"] = conflictPriority,
			["fallback_rule"] = fallbackRule,
			["recovery_action"] = recoveryAction,
			["source_layer"] = sourceLayer,
			["ui_evidence_allowed"] = uiEvidenceAllowed,
		};
	}

	private static string BuildBehaviorPriorityTable(string sceneId) =>
		sceneId switch
		{
			"hub_island_dock" => "hazardous:80 > trigger_only:30 > visual_only_height_marker:10",
			"hub_ship_interior" => "trigger_only:30 > blocking_static:20 > visual_only_glass:10",
			"exploration_mist_island" => "hazardous:90 > hazardous_warning:70 > trigger_only:40/35 > visual_only_fog:10",
			"ochre_island_scene" => "hazardous:80 > resource_node:45 > trigger_only:35",
			_ => "",
		};

	private static string BuildBehaviorFallbackRules(string sceneId) =>
		sceneId switch
		{
			"hub_island_dock" => "If a behavior tag lacks priority, implementation readiness fails; waterline clamp and ramp escape interaction are the recovery fallback.",
			"hub_ship_interior" => "If a behavior tag lacks priority, implementation readiness fails; static crate remains non-pushable until pushable priority and recovery are declared.",
			"exploration_mist_island" => "If a behavior tag lacks priority, implementation readiness fails; hazardous boundary clamp wins over trigger-only anchors and visual fog.",
			"ochre_island_scene" => "If a behavior tag lacks priority, implementation readiness fails; cloudsea clamp wins over ore and return triggers.",
			_ => "",
		};

	private static Godot.Collections.Array<Godot.Collections.Dictionary> BuildRecoveryTable(string sceneId, string recoveryRule)
	{
		return new Godot.Collections.Array<Godot.Collections.Dictionary>
		{
			new()
			{
				["stuck_state"] = "outside_walk_bounds",
				["recovery_action"] = recoveryRule,
				["visible_feedback"] = "player remains in world/playable scene layer; UI may describe the recovery but cannot satisfy it",
				["source_layer"] = "world_playable_scene",
				["ui_evidence_allowed"] = false,
			},
			new()
			{
				["stuck_state"] = sceneId == "hub_ship_interior" ? "blocked_by_static_crate" : "blocked_by_hazard_boundary",
				["recovery_action"] = sceneId == "hub_ship_interior" ? "clamp to nearest safe floor inside ShipInteriorWalkBounds" : "clamp to nearest safe floor inside active walk bounds",
				["visible_feedback"] = "safe floor, boundary, or exit anchor remains visible after recovery",
				["source_layer"] = "world_playable_scene",
				["ui_evidence_allowed"] = false,
			},
		};
	}

	public int DebugSearchPulseStage() => searchPulseStage;

	public int DebugReturnPrepStage() => returnPrepStage;

	public int DebugOchreReturnPrepStage() => ochreReturnPrepStage;

	public bool DebugOchreOreHarvested() => ochreOreHarvested;

	public bool DebugDurableProgressExists() => Godot.FileAccess.FileExists(DurableProgressPath);

	public string DebugDurableProgressPath() => ProjectSettings.GlobalizePath(DurableProgressPath);

	/// <summary>Returns whether invalid durable progress has been isolated for smoke-test diagnostics.</summary>
	public bool DebugQuarantinedProgressExists() => Godot.FileAccess.FileExists(QuarantinedProgressPath);

	/// <summary>Returns the OS path for the isolated invalid durable progress file.</summary>
	public string DebugQuarantinedProgressPath() => ProjectSettings.GlobalizePath(QuarantinedProgressPath);

	public void DebugClearDurableProgress()
	{
		using var directory = DirAccess.Open("user://");
		if (!Godot.FileAccess.FileExists(DurableProgressPath))
		{
			directory?.Remove(QuarantinedProgressFileName);
			hasLoadableProgress = false;
			lastDurableImportFailure = "";
			pendingOverwriteConfirmation = false;
			pendingDeleteConfirmation = false;
			SetSaveStatus("暂无可读取航行日志：记录后可读取。");
			RefreshSaveDeleteAffordance();
			return;
		}

		directory?.Remove(DurableProgressFileName);
		directory?.Remove(QuarantinedProgressFileName);
		hasLoadableProgress = false;
		lastDurableImportFailure = "";
		pendingOverwriteConfirmation = false;
		pendingDeleteConfirmation = false;
		SetSaveStatus("暂无可读取航行日志：记录后可读取。");
		RefreshSaveDeleteAffordance();
	}

	public void DebugWriteCorruptDurableProgress()
	{
		using var file = Godot.FileAccess.Open(DurableProgressPath, Godot.FileAccess.ModeFlags.Write);
		file?.StoreString("{\"generation\":1,\"domains\":{},\"_checksum\":\"not-a-valid-checksum\"}");
		hasLoadableProgress = false;
		lastDurableImportFailure = "";
		pendingOverwriteConfirmation = false;
		pendingDeleteConfirmation = false;
		RefreshSaveDeleteAffordance();
	}

	public Godot.Collections.Dictionary DebugDomainSnapshot()
	{
		var snapshot = domain.Snapshot;
		return new Godot.Collections.Dictionary
		{
			["chart_state"] = snapshot.ChartState,
			["content_version"] = snapshot.ContentVersion,
			["content_status"] = snapshot.ContentStatus,
			["selected_route"] = snapshot.SelectedRouteId,
			["selected_route_name"] = snapshot.SelectedRouteName,
			["visible_route_count"] = snapshot.VisibleRouteCount,
			["committed_route"] = snapshot.CommittedRouteId,
			["committed_destination"] = snapshot.CommittedDestinationId,
			["hub_docking_state"] = snapshot.HubDockingState,
			["hub_departure_mode"] = snapshot.HubDepartureMode,
			["hub_last_route"] = snapshot.HubLastRoute,
			["navigation_state"] = snapshot.NavigationState,
			["navigation_progress"] = snapshot.NavigationProgress,
			["encounter_destination"] = snapshot.EncounterDestinationId,
			["encounter_result"] = snapshot.EncounterResult,
			["encounter_damage"] = snapshot.EncounterDamage,
			["exploration_phase"] = snapshot.ExplorationPhase,
			["exploration_substate"] = snapshot.ExplorationSubstate,
			["exploration_point"] = snapshot.ExplorationPointId,
			["exploration_step"] = snapshot.ExplorationStep,
			["last_search_point"] = snapshot.LastSearchPointId,
			["last_search_point_name"] = snapshot.LastSearchPointName,
			["last_search_message"] = snapshot.LastSearchMessage,
			["scene_search_point_text"] = explorationPointSemanticLabel?.Text ?? "",
			["scene_threat_text"] = explorationThreatSemanticLabel?.Text ?? "",
			["scene_extraction_text"] = explorationExtractionSemanticLabel?.Text ?? "",
			["basic_supply_in_storage"] = snapshot.BasicSupplyInStorage,
			["repair_kits_in_storage"] = snapshot.RepairKitsInStorage,
			["reward_in_storage"] = snapshot.RewardInStorage,
			["reward_carried"] = snapshot.RewardCarried,
			["cargo_used"] = snapshot.CargoUsed,
			["hull_integrity"] = snapshot.HullIntegrity,
			["threat_text"] = snapshot.ThreatText,
			["persistence_generation"] = snapshot.PersistenceGeneration,
			["last_save_status"] = snapshot.LastSaveStatus,
			["last_load_status"] = snapshot.LastLoadStatus,
			["last_status"] = snapshot.LastStatus,
		};
	}

	/// <summary>Returns runtime onboarding state for smoke tests and QA diagnostics.</summary>
	public Godot.Collections.Dictionary DebugOnboardingSnapshot()
	{
		var hint = onboarding.EvaluateNextHint();
		var steps = new Godot.Collections.Dictionary();
		foreach (var stepId in onboarding.StepIds)
		{
			steps[stepId] = onboarding.GetStepProgress(stepId).State.ToString();
		}

		return new Godot.Collections.Dictionary
		{
			["progress_percent"] = onboarding.FirstLoopProgressPercent,
			["first_loop_complete"] = onboarding.IsFirstLoopComplete,
			["active_surface"] = onboarding.ActiveSurface.ToString(),
			["next_hint_step"] = hint?.StepId ?? "",
			["hint_text"] = runtimeHintLabel?.Text ?? "",
			["hint_mouse_filter"] = runtimeHintLabel is null ? -1 : (int)runtimeHintLabel.MouseFilter,
			["steps"] = steps,
		};
	}

	private void CacheNodes()
	{
		hubRoot = FindControl("HubRoot");
		storageValueLabel = FindChild("StorageValue", true, false) as Label;
		cargoValueLabel = FindChild("CargoValue", true, false) as Label;
		hullValueLabel = FindChild("HullValue", true, false) as Label;
		chartStationLabel = FindChild("ChartStation", true, false) as Label;
		cargoStationLabel = FindChild("CargoStation", true, false) as Label;
		saveStatusLabel = FindChild("SaveStatusLabel", true, false) as Label;
		runtimeHintLabel = FindChild("RuntimeHintLabel", true, false) as Label;
		if (runtimeHintLabel is not null)
		{
			runtimeHintLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			runtimeHintLabel.FocusMode = Control.FocusModeEnum.None;
		}
		footerLabel = FindChild("Footer", true, false) as Label;
		hubActionButtons[0] = FindChild("ChartButton", true, false) as Button;
		hubActionButtons[1] = FindChild("SaveButton", true, false) as Button;
		hubActionButtons[2] = FindChild("LoadButton", true, false) as Button;
		deleteProgressButton = CreateDeleteProgressButton();
		hubActionButtons[3] = deleteProgressButton;
	}

	private void CreatePlayableLayer()
	{
		if (hubRoot is null)
		{
			return;
		}

		playableLayer = new Control
		{
			Name = "PlayableVerticalSliceLayer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 12,
		};
		playableLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		hubRoot.AddChild(playableLayer);

		sceneLayer = new Control
		{
			Name = "WorldSceneLayer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 0,
		};
		sceneLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		playableLayer.AddChild(sceneLayer);

		interactionLayer = new Control
		{
			Name = "WorldInteractionLayer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 20,
		};
		interactionLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		playableLayer.AddChild(interactionLayer);

		AddHubGreyboxSet();
		AddChartGreyboxSet();
		AddExplorationGreyboxSet();
		AddOchreGreyboxSet();
		hubShipEntryMarker = AddWorldMarker("ShipEntryInteractPoint", new Vector2(202, 584), new Color(0.45f, 0.62f, 0.52f), "登船 E");
		hubShipExitMarker = AddWorldMarker("ShipExitInteractPoint", new Vector2(224, 584), new Color(0.45f, 0.52f, 0.62f), "下船 E");
		hubHelmMarker = AddWorldMarker("HelmInteractPoint", new Vector2(316, 594), new Color(0.22f, 0.58f, 0.72f), "舵台 E");
		hubStorageMarker = AddWorldMarker("StorageInteractPoint", new Vector2(536, 594), new Color(0.58f, 0.45f, 0.26f), "仓储 E");
		explorationSearchMarker = AddWorldMarker("SearchInteractPoint", new Vector2(592, 594), new Color(0.46f, 0.67f, 0.33f), "搜索 E");
		explorationReturnMarker = AddWorldMarker("ReturnInteractPoint", new Vector2(204, 594), new Color(0.64f, 0.39f, 0.28f), "驾驶返航 E");
		ochreOreMarker = AddWorldMarker("OchreOreInteractPoint", new Vector2(609, 391), new Color(0.64f, 0.50f, 0.30f), "采集 E");
		ochreReturnMarker = AddWorldMarker("OchreReturnInteractPoint", new Vector2(829, 411), new Color(0.44f, 0.58f, 0.63f), "返航 E");

		playerMarker = new ColorRect
		{
			Name = "PlayerMarker",
			Color = new Color(0.91f, 0.84f, 0.45f),
			CustomMinimumSize = new Vector2(28, 28),
			Size = new Vector2(28, 28),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		interactionLayer.AddChild(playerMarker);

		interactionPromptLabel = new Label
		{
			Name = "SpatialInteractionPrompt",
			Position = new Vector2(76, 112),
			Size = new Vector2(1120, 28),
			HorizontalAlignment = HorizontalAlignment.Center,
			Text = "WASD / 方向键移动，靠近可交互点按 E。",
		};
		interactionPromptLabel.AddThemeFontSizeOverride("font_size", 18);
		interactionLayer.AddChild(interactionPromptLabel);

		ochreDebugButton = CreateOchreDebugButton();
		hubActionButtons[4] = ochreDebugButton;
	}

	private void AddHubGreyboxSet()
	{
		AddSceneRect(hubSceneItems, "HubPlayableSkyBackdrop", new Vector2(0, 126), new Vector2(1280, 520), new Color(0.12f, 0.22f, 0.27f, 1.0f));
		AddSceneRect(hubSceneItems, "HubPlayableFarMist", new Vector2(0, 162), new Vector2(1280, 62), new Color(0.36f, 0.50f, 0.52f, 0.46f));
		AddSceneRect(hubSceneItems, "HubPlayableSeaHorizon", new Vector2(0, 592), new Vector2(1280, 88), new Color(0.08f, 0.22f, 0.31f, 1.0f));
		AddSceneEllipse(hubExteriorSceneItems, "HubIslandMainMass", new Vector2(468, 522), new Vector2(438, 154), new Color(0.18f, 0.36f, 0.28f, 1.0f));
		AddSceneEllipse(hubExteriorSceneItems, "HubIslandGrassCap", new Vector2(464, 472), new Vector2(382, 78), new Color(0.34f, 0.55f, 0.42f, 1.0f));
		AddSceneRect(hubExteriorSceneItems, "HubDockPlankWalkway", new Vector2(120, 562), new Vector2(390, 54), new Color(0.48f, 0.40f, 0.28f, 1.0f));
		AddSceneRect(hubExteriorSceneItems, "HubDockPostLeft", new Vector2(144, 520), new Vector2(18, 100), new Color(0.30f, 0.23f, 0.16f, 0.98f));
		AddSceneRect(hubExteriorSceneItems, "HubDockPostRight", new Vector2(458, 520), new Vector2(18, 100), new Color(0.30f, 0.23f, 0.16f, 0.98f));
		AddScenePolygon(hubExteriorSceneItems, "HubDockedShipHullSilhouette",
			[
				new Vector2(522, 480),
				new Vector2(934, 470),
				new Vector2(1016, 526),
				new Vector2(952, 584),
				new Vector2(558, 584),
				new Vector2(488, 530),
			],
			new Color(0.16f, 0.23f, 0.30f, 1.0f));
		AddSceneEllipse(hubExteriorSceneItems, "HubDockedShipEnvelopeSilhouette", new Vector2(756, 396), new Vector2(292, 68), new Color(0.54f, 0.68f, 0.70f, 0.96f));
		AddSceneRect(hubExteriorSceneItems, "HubShipMastForward", new Vector2(636, 430), new Vector2(10, 126), new Color(0.52f, 0.60f, 0.58f, 0.96f));
		AddSceneRect(hubExteriorSceneItems, "HubShipMastRear", new Vector2(846, 430), new Vector2(10, 126), new Color(0.52f, 0.60f, 0.58f, 0.96f));
		AddSceneRect(hubSceneItems, "HubIslandWalkBoundary", HubWalkBounds.Position, HubWalkBounds.Size, new Color(0.13f, 0.26f, 0.25f, 1.0f));
		AddSceneRect(hubExteriorSceneItems, "HubIslandUpperEdge", new Vector2(132, 380), new Vector2(1016, 10), new Color(0.46f, 0.66f, 0.60f, 0.95f));
		AddSceneRect(hubExteriorSceneItems, "HubIslandLowerEdge", new Vector2(132, 622), new Vector2(1016, 10), new Color(0.46f, 0.66f, 0.60f, 0.95f));
		AddSceneRect(hubExteriorSceneItems, "HubDockWaterline", new Vector2(72, 632), new Vector2(1136, 18), new Color(0.10f, 0.26f, 0.34f, 0.78f));
		AddSceneRect(hubExteriorSceneItems, "HubDockPier", new Vector2(156, 568), new Vector2(224, 34), new Color(0.42f, 0.36f, 0.27f, 0.96f));
		AddSceneRect(hubExteriorSceneItems, "HubDockedShipExterior", new Vector2(382, 448), new Vector2(448, 126), new Color(0.18f, 0.25f, 0.31f, 1.0f));
		AddSceneRect(hubExteriorSceneItems, "HubDockedShipBalloon", new Vector2(446, 390), new Vector2(320, 54), new Color(0.43f, 0.58f, 0.62f, 0.90f));
		AddSceneRect(hubExteriorSceneItems, "HubDockedShipCabinDoor", new Vector2(410, 536), new Vector2(56, 38), new Color(0.54f, 0.48f, 0.34f, 0.98f));
		AddSceneRect(hubExteriorSceneItems, "HubBoardingRamp", new Vector2(236, 558), new Vector2(174, 28), new Color(0.48f, 0.42f, 0.31f, 0.96f));
		AddSceneLabel(hubExteriorSceneItems, "HubBoardingRampLabel", new Vector2(208, 532), new Vector2(174, 22), "岛上码头 / 登船坡道");
		AddSceneLabel(hubExteriorSceneItems, "HubIslandDockIdentityLabel", new Vector2(438, 414), new Vector2(360, 28), "停泊浮岛：云织号靠岸，可登船进入内部");
		AddSceneRect(hubInteriorSceneItems, "HubInteriorBackdrop", new Vector2(0, 126), new Vector2(1280, 520), new Color(0.09f, 0.13f, 0.16f, 1.0f));
		AddScenePolygon(hubInteriorSceneItems, "HubInteriorHullOutline",
			[
				new Vector2(156, 450),
				new Vector2(248, 378),
				new Vector2(1002, 378),
				new Vector2(1128, 452),
				new Vector2(1018, 632),
				new Vector2(236, 632),
			],
			new Color(0.14f, 0.19f, 0.23f, 1.0f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorDeckSpine", new Vector2(214, 540), new Vector2(820, 42), new Color(0.42f, 0.48f, 0.44f, 0.92f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorCockpitBay", new Vector2(244, 408), new Vector2(248, 122), new Color(0.18f, 0.37f, 0.44f, 0.96f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorCargoBay", new Vector2(520, 408), new Vector2(250, 122), new Color(0.42f, 0.32f, 0.20f, 0.96f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorEngineBay", new Vector2(798, 408), new Vector2(250, 122), new Color(0.28f, 0.31f, 0.43f, 0.96f));
		AddSceneLabel(hubInteriorSceneItems, "HubStationIdentityLabel", new Vector2(440, 424), new Vector2(360, 28), "云织号船内：驾驶舱 / 货舱 / 轮机间");
		AddSceneRect(hubInteriorSceneItems, "HubShipInteriorShell", new Vector2(220, 442), new Vector2(804, 166), new Color(0.13f, 0.18f, 0.22f, 1.0f));
		AddSceneRect(hubInteriorSceneItems, "HubCabinRoom", new Vector2(274, 464), new Vector2(184, 64), new Color(0.21f, 0.34f, 0.40f, 0.94f));
		AddSceneRect(hubInteriorSceneItems, "HubCabinGlow", new Vector2(278, 456), new Vector2(176, 6), new Color(0.56f, 0.82f, 0.92f, 0.98f));
		AddSceneLabel(hubInteriorSceneItems, "HubCabinRoomLabel", new Vector2(288, 470), new Vector2(154, 24), "驾驶舱 / 航台");
		AddSceneRect(hubInteriorSceneItems, "HubCabinWindow", new Vector2(304, 490), new Vector2(124, 10), new Color(0.48f, 0.68f, 0.78f, 0.92f));
		AddSceneRect(hubInteriorSceneItems, "HubCabinNavigationSlate", new Vector2(302, 506), new Vector2(128, 14), new Color(0.12f, 0.27f, 0.34f, 0.96f));
		hubCabinStatusLabel = AddSceneLabel(hubInteriorSceneItems, "HubCabinStatusLabel", new Vector2(286, 528), new Vector2(162, 22), "驾驶舱：待规划");
		AddSceneRect(hubInteriorSceneItems, "HubCargoRoom", new Vector2(506, 464), new Vector2(184, 64), new Color(0.37f, 0.30f, 0.21f, 0.94f));
		AddSceneRect(hubInteriorSceneItems, "HubCargoGlow", new Vector2(510, 456), new Vector2(176, 6), new Color(0.86f, 0.66f, 0.34f, 0.98f));
		AddSceneLabel(hubInteriorSceneItems, "HubCargoRoomLabel", new Vector2(530, 472), new Vector2(136, 22), "货舱");
		AddSceneRect(hubInteriorSceneItems, "HubCargoShelfLeft", new Vector2(520, 490), new Vector2(34, 30), new Color(0.55f, 0.46f, 0.29f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "HubCargoShelfRight", new Vector2(642, 490), new Vector2(34, 30), new Color(0.55f, 0.46f, 0.29f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "HubCargoLoadTrack", new Vector2(560, 504), new Vector2(72, 10), new Color(0.24f, 0.21f, 0.16f, 0.96f));
		hubCargoLoadFill = AddSceneRect(hubInteriorSceneItems, "HubCargoLoadFill", new Vector2(560, 504), new Vector2(0, 10), new Color(0.78f, 0.64f, 0.34f, 0.98f));
		hubCargoStatusLabel = AddSceneLabel(hubInteriorSceneItems, "HubCargoStatusLabel", new Vector2(518, 528), new Vector2(160, 22), "货舱：空载");
		AddSceneRect(hubInteriorSceneItems, "HubEngineRoom", new Vector2(766, 464), new Vector2(184, 64), new Color(0.28f, 0.30f, 0.38f, 0.94f));
		AddSceneRect(hubInteriorSceneItems, "HubEngineGlow", new Vector2(770, 456), new Vector2(176, 6), new Color(0.72f, 0.76f, 0.96f, 0.98f));
		AddSceneLabel(hubInteriorSceneItems, "HubEngineRoomLabel", new Vector2(788, 472), new Vector2(140, 22), "轮机间");
		AddSceneRect(hubInteriorSceneItems, "HubEngineCoilLeft", new Vector2(794, 492), new Vector2(42, 28), new Color(0.34f, 0.43f, 0.56f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "HubEngineCoilRight", new Vector2(882, 492), new Vector2(42, 28), new Color(0.34f, 0.43f, 0.56f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "HubEnginePowerConduit", new Vector2(836, 504), new Vector2(46, 8), new Color(0.64f, 0.70f, 0.48f, 0.95f));
		hubEngineWearOverlay = AddSceneRect(hubInteriorSceneItems, "HubEngineWearOverlay", new Vector2(800, 486), new Vector2(118, 36), new Color(0.64f, 0.25f, 0.21f, 0.35f));
		hubEngineStatusLabel = AddSceneLabel(hubInteriorSceneItems, "HubEngineStatusLabel", new Vector2(778, 528), new Vector2(160, 22), "轮机间：稳定");
		AddSceneRect(hubInteriorSceneItems, "HubDeckFloor", new Vector2(196, 526), new Vector2(836, 112), new Color(0.18f, 0.23f, 0.24f, 0.88f));
		AddSceneRect(hubInteriorSceneItems, "HubDeckRail", new Vector2(216, 502), new Vector2(780, 12), new Color(0.43f, 0.55f, 0.56f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorDoorLineCabinCargo", new Vector2(474, 470), new Vector2(10, 52), new Color(0.58f, 0.58f, 0.46f, 0.88f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorDoorLineCargoEngine", new Vector2(728, 470), new Vector2(10, 52), new Color(0.58f, 0.58f, 0.46f, 0.88f));
		AddSceneRect(hubInteriorSceneItems, "HubInteriorMainAisle", new Vector2(276, 536), new Vector2(672, 8), new Color(0.50f, 0.55f, 0.48f, 0.82f));
		AddSceneRect(hubInteriorSceneItems, "HelmConsoleProp", new Vector2(286, 546), new Vector2(150, 58), new Color(0.14f, 0.38f, 0.48f, 0.95f));
		AddSceneLabel(hubInteriorSceneItems, "HelmConsoleLabel", new Vector2(298, 552), new Vector2(126, 22), "航图舵台");
		AddSceneRect(hubInteriorSceneItems, "HelmConsoleArrow", new Vector2(344, 528), new Vector2(28, 16), new Color(0.74f, 0.88f, 0.92f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "StorageCrateProp", new Vector2(520, 548), new Vector2(132, 56), new Color(0.42f, 0.34f, 0.22f, 0.95f));
		AddSceneRect(hubInteriorSceneItems, "StorageCrateBand", new Vector2(536, 564), new Vector2(100, 10), new Color(0.68f, 0.59f, 0.39f, 0.95f));
		AddSceneLabel(hubInteriorSceneItems, "StorageCrateLabel", new Vector2(527, 552), new Vector2(118, 22), "仓储货箱");
		AddSceneRect(hubInteriorSceneItems, "HubInteriorExitDoor", new Vector2(218, 552), new Vector2(42, 48), new Color(0.48f, 0.42f, 0.31f, 0.96f));
		AddSceneLabel(hubInteriorSceneItems, "HubMovementCueLabel", new Vector2(430, 606), new Vector2(380, 22), "船内走廊连接驾驶舱、货舱、轮机间");
	}

	private void AddChartGreyboxSet()
	{
		AddSceneRect(chartSceneItems, "ChartCabinBackdrop", new Vector2(0, 126), new Vector2(1280, 520), new Color(0.08f, 0.12f, 0.13f, 1.0f));
		AddSceneRect(chartSceneItems, "ChartTableShadow", new Vector2(128, 210), new Vector2(1024, 398), new Color(0.05f, 0.08f, 0.08f, 0.82f));
		AddSceneRect(chartSceneItems, "ChartTableSurface", new Vector2(150, 190), new Vector2(980, 382), new Color(0.31f, 0.24f, 0.16f, 0.98f));
		AddSceneRect(chartSceneItems, "ChartTableBrassRimTop", new Vector2(172, 212), new Vector2(936, 10), new Color(0.70f, 0.55f, 0.31f, 0.98f));
		AddSceneRect(chartSceneItems, "ChartTableBrassRimBottom", new Vector2(172, 548), new Vector2(936, 10), new Color(0.70f, 0.55f, 0.31f, 0.98f));
		AddSceneRect(chartSceneItems, "ChartParchmentMap", new Vector2(242, 238), new Vector2(654, 270), new Color(0.73f, 0.66f, 0.48f, 0.98f));
		AddSceneRect(chartSceneItems, "ChartParchmentInnerTint", new Vector2(266, 262), new Vector2(606, 222), new Color(0.60f, 0.62f, 0.50f, 0.72f));
		AddSceneEllipse(chartSceneItems, "ChartOriginGlassHarborNode", new Vector2(342, 374), new Vector2(32, 22), new Color(0.32f, 0.50f, 0.48f, 0.98f));
		AddSceneLabel(chartSceneItems, "ChartOriginGlassHarborLabel", new Vector2(286, 402), new Vector2(118, 24), "玻璃港");
		AddScenePolygon(chartSceneItems, "ChartRouteMistLine",
			[
				new Vector2(362, 366),
				new Vector2(640, 302),
				new Vector2(650, 318),
				new Vector2(370, 382),
			],
			new Color(0.46f, 0.66f, 0.62f, 0.92f));
		AddSceneEllipse(chartSceneItems, "ChartMistDestinationNode", new Vector2(684, 304), new Vector2(34, 22), new Color(0.40f, 0.64f, 0.56f, 0.98f));
		chartMistSelectionFrame = AddSceneRect(chartSceneItems, "ChartRouteMistSelectionFrame", new Vector2(630, 274), new Vector2(112, 64), new Color(0.84f, 0.78f, 0.38f, 0.38f));
		chartMistSelectionFrame.Visible = false;
		AddSceneLabel(chartSceneItems, "ChartMistRouteLabel", new Vector2(610, 334), new Vector2(180, 24), "雾海短程 / 雾灯残骸");
		AddSceneRect(chartSceneItems, "ChartCompassPlate", new Vector2(930, 252), new Vector2(124, 124), new Color(0.18f, 0.29f, 0.30f, 0.95f));
		AddSceneRect(chartSceneItems, "ChartCompassNeedle", new Vector2(988, 270), new Vector2(8, 88), new Color(0.76f, 0.64f, 0.38f, 0.98f));
		AddSceneLabel(chartSceneItems, "ChartCompassLabel", new Vector2(934, 378), new Vector2(116, 24), "罗经稳定");
		AddSceneRect(chartSceneItems, "ChartRiskNotePanel", new Vector2(918, 422), new Vector2(184, 86), new Color(0.20f, 0.22f, 0.18f, 0.92f));
		AddSceneLabel(chartSceneItems, "ChartRiskNoteLabel", new Vector2(930, 430), new Vector2(160, 62), "当前航线：雾带低威胁；其他目的地待审查后开放");
		AddSceneLabel(chartSceneItems, "ChartSceneInstructionLabel", new Vector2(340, 520), new Vector2(594, 26), "选择航线后确认离港，航程会进入雾海搜撤。");
	}

	private void AddExplorationGreyboxSet()
	{
		AddSceneRect(explorationSceneItems, "ExplorationPlayableSkyBackdrop", new Vector2(0, 126), new Vector2(1280, 520), new Color(0.10f, 0.20f, 0.25f, 1.0f));
		AddSceneRect(explorationSceneItems, "ExplorationPlayableMistHorizon", new Vector2(0, 160), new Vector2(1280, 72), new Color(0.42f, 0.52f, 0.50f, 0.44f));
		AddSceneRect(explorationSceneItems, "ExplorationPlayableSea", new Vector2(0, 596), new Vector2(1280, 84), new Color(0.07f, 0.22f, 0.30f, 1.0f));
		AddSceneEllipse(explorationSceneItems, "ExplorationPlayableIslandBody", new Vector2(650, 510), new Vector2(468, 152), new Color(0.16f, 0.35f, 0.27f, 1.0f));
		AddSceneEllipse(explorationSceneItems, "ExplorationPlayableIslandUpperTrail", new Vector2(650, 462), new Vector2(380, 68), new Color(0.34f, 0.52f, 0.39f, 1.0f));
		AddSceneRect(explorationSceneItems, "ExplorationPlayablePier", new Vector2(122, 566), new Vector2(276, 46), new Color(0.47f, 0.38f, 0.26f, 1.0f));
		AddScenePolygon(explorationSceneItems, "ExplorationReturnShipHullSilhouette",
			[
				new Vector2(102, 520),
				new Vector2(298, 510),
				new Vector2(354, 548),
				new Vector2(306, 610),
				new Vector2(130, 610),
				new Vector2(72, 560),
			],
			new Color(0.16f, 0.24f, 0.31f, 1.0f));
		AddSceneEllipse(explorationSceneItems, "ExplorationReturnShipEnvelope", new Vector2(218, 474), new Vector2(132, 42), new Color(0.52f, 0.66f, 0.68f, 0.94f));
		AddSceneRect(explorationSceneItems, "ExplorationIslandWalkBoundary", ExplorationWalkBounds.Position, ExplorationWalkBounds.Size, new Color(0.12f, 0.27f, 0.24f, 1.0f));
		AddSceneRect(explorationSceneItems, "ExplorationIslandUpperEdge", new Vector2(132, 390), new Vector2(1016, 12), new Color(0.45f, 0.64f, 0.54f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationIslandLowerEdge", new Vector2(132, 624), new Vector2(1016, 12), new Color(0.45f, 0.64f, 0.54f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationDockedShip", new Vector2(156, 546), new Vector2(154, 74), new Color(0.20f, 0.27f, 0.33f, 0.96f));
		AddSceneRect(explorationSceneItems, "ExplorationReturnShipHelm", new Vector2(196, 558), new Vector2(68, 34), new Color(0.18f, 0.38f, 0.48f, 0.96f));
		explorationReturnPrepFill = AddSceneRect(explorationSceneItems, "ExplorationReturnPrepFill", new Vector2(202, 586), new Vector2(0, 8), new Color(0.86f, 0.66f, 0.40f, 0.98f));
		AddSceneRect(explorationSceneItems, "ExplorationBoardingRamp", new Vector2(290, 580), new Vector2(92, 28), new Color(0.48f, 0.42f, 0.31f, 0.96f));
		AddSceneLabel(explorationSceneItems, "ExplorationBoardingRampLabel", new Vector2(166, 522), new Vector2(132, 22), "靠岸空艇");
		AddSceneRect(explorationSceneItems, "ExplorationIslandPath", new Vector2(342, 584), new Vector2(610, 20), new Color(0.32f, 0.44f, 0.36f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationSkyField", new Vector2(92, 500), new Vector2(1092, 138), new Color(0.10f, 0.20f, 0.25f, 1.0f));
		AddSceneRect(explorationSceneItems, "ExplorationIslandMass", new Vector2(390, 430), new Vector2(470, 154), new Color(0.18f, 0.34f, 0.27f, 1.0f));
		AddSceneRect(explorationSceneItems, "ExplorationCliffEdge", new Vector2(416, 444), new Vector2(418, 18), new Color(0.56f, 0.66f, 0.50f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationSearchPathSteps", new Vector2(472, 578), new Vector2(86, 14), new Color(0.62f, 0.57f, 0.40f, 0.95f));
		AddSceneLabel(explorationSceneItems, "ExplorationIslandIdentityLabel", new Vector2(432, 466), new Vector2(356, 26), "雾海浮岛：沿路径接近残骸搜索");
		AddSceneRect(explorationSceneItems, "ExplorationRouteTrail", new Vector2(176, 586), new Vector2(820, 14), new Color(0.34f, 0.54f, 0.50f, 0.95f));
		explorationRouteProgressFill = AddSceneRect(explorationSceneItems, "ExplorationRouteProgressFill", new Vector2(176, 586), new Vector2(0, 14), new Color(0.78f, 0.70f, 0.36f, 0.98f));
		explorationPointSemanticLabel = AddSceneLabel(explorationSceneItems, "ExplorationPointSemanticLabel", new Vector2(186, 508), new Vector2(360, 24), "搜索点：未接近残骸");
		AddSceneRect(explorationSceneItems, "SearchWreckProp", new Vector2(558, 540), new Vector2(170, 66), new Color(0.28f, 0.42f, 0.29f, 0.96f));
		AddSceneRect(explorationSceneItems, "SearchWreckMast", new Vector2(630, 510), new Vector2(12, 42), new Color(0.56f, 0.48f, 0.34f, 0.95f));
		AddSceneRect(explorationSceneItems, "SearchWreckSignalGlow", new Vector2(594, 574), new Vector2(96, 10), new Color(0.78f, 0.84f, 0.46f, 0.98f));
		AddSceneRect(explorationSceneItems, "SearchWreckHighlight", new Vector2(590, 556), new Vector2(104, 12), new Color(0.62f, 0.75f, 0.42f, 0.98f));
		AddSceneRect(explorationSceneItems, "SearchClueLeft", new Vector2(526, 536), new Vector2(24, 24), new Color(0.68f, 0.62f, 0.38f, 0.95f));
		AddSceneRect(explorationSceneItems, "SearchClueRight", new Vector2(706, 572), new Vector2(24, 24), new Color(0.68f, 0.62f, 0.38f, 0.95f));
		AddSceneRect(explorationSceneItems, "SearchScanArc", new Vector2(548, 526), new Vector2(196, 8), new Color(0.54f, 0.74f, 0.68f, 0.90f));
		explorationSearchPulseFill = AddSceneRect(explorationSceneItems, "SearchPulseFill", new Vector2(584, 604), new Vector2(0, 8), new Color(0.72f, 0.88f, 0.50f, 0.98f));
		AddSceneLabel(explorationSceneItems, "SearchWreckLabel", new Vector2(574, 548), new Vector2(138, 22), "漂浮残骸");
		explorationThreatZone = AddSceneRect(explorationSceneItems, "ExplorationThreatZone", new Vector2(768, 528), new Vector2(150, 68), new Color(0.62f, 0.25f, 0.22f, 0.52f));
		explorationThreatSemanticLabel = AddSceneLabel(explorationSceneItems, "ExplorationThreatSemanticLabel", new Vector2(756, 506), new Vector2(190, 24), "威胁区：未触发");
		AddSceneRect(explorationSceneItems, "ReturnBeaconProp", new Vector2(986, 526), new Vector2(104, 82), new Color(0.50f, 0.28f, 0.24f, 0.96f));
		AddSceneRect(explorationSceneItems, "ReturnBeaconBeam", new Vector2(1032, 478), new Vector2(12, 52), new Color(0.86f, 0.66f, 0.40f, 0.72f));
		AddSceneRect(explorationSceneItems, "ReturnBeaconCore", new Vector2(1024, 540), new Vector2(28, 52), new Color(0.78f, 0.62f, 0.42f, 0.98f));
		AddSceneLabel(explorationSceneItems, "ReturnBeaconLabel", new Vector2(990, 532), new Vector2(96, 22), "返航信标");
		explorationExtractionSemanticLabel = AddSceneLabel(explorationSceneItems, "ExplorationExtractionSemanticLabel", new Vector2(924, 508), new Vector2(246, 24), "撤离：可随时返航");
	}

	private void AddOchreGreyboxSet()
	{
		var scene = ResourceLoader.Load<PackedScene>(OchreIslandScenePath);
		if (scene?.Instantiate() is CanvasItem ochreScene)
		{
			ochreScene.Name = "OchreIslandSceneRuntimeInstance";
			ochreScene.Visible = false;
			sceneLayer?.AddChild(ochreScene);
			ochreSceneItems.Add(ochreScene);
		}
		else
		{
			AddSceneRect(ochreSceneItems, "OchreIslandSceneLoadFailure", new Vector2(140, 292), new Vector2(900, 278), new Color(0.46f, 0.16f, 0.16f, 1.0f));
			AddSceneLabel(ochreSceneItems, "OchreIslandSceneLoadFailureLabel", new Vector2(330, 410), new Vector2(520, 28), "赭石岛场景资产加载失败");
		}

		ochreOreSemanticLabel = AddSceneLabel(ochreSceneItems, "OchreOreSemanticLabel", new Vector2(520, 454), new Vector2(300, 24), "矿脉：可采集 / reward pending");
		ochreReturnSemanticLabel = AddSceneLabel(ochreSceneItems, "OchreReturnSemanticLabel", new Vector2(760, 498), new Vector2(270, 24), "返航：待预热");
	}

	private ColorRect AddWorldMarker(string nodeName, Vector2 markerPosition, Color markerColor, string labelText)
	{
		var marker = new ColorRect
		{
			Name = nodeName,
			Color = markerColor,
			Position = markerPosition,
			CustomMinimumSize = new Vector2(92, 38),
			Size = new Vector2(92, 38),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		interactionLayer?.AddChild(marker);

		var label = new Label
		{
			Name = $"{nodeName}Label",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Text = labelText,
		};
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.AddThemeFontSizeOverride("font_size", 16);
		marker.AddChild(label);
		return marker;
	}

	private ColorRect AddSceneRect(Godot.Collections.Array<CanvasItem> group, string nodeName, Vector2 position, Vector2 size, Color color)
	{
		var rect = new ColorRect
		{
			Name = nodeName,
			Color = color,
			Position = position,
			CustomMinimumSize = size,
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 0,
		};
		sceneLayer?.AddChild(rect);
		group.Add(rect);
		return rect;
	}

	private Polygon2D AddScenePolygon(Godot.Collections.Array<CanvasItem> group, string nodeName, Vector2[] points, Color color)
	{
		var polygon = new Polygon2D
		{
			Name = nodeName,
			Polygon = points,
			Color = color,
			ZIndex = 0,
		};
		sceneLayer?.AddChild(polygon);
		group.Add(polygon);
		return polygon;
	}

	private Polygon2D AddSceneEllipse(Godot.Collections.Array<CanvasItem> group, string nodeName, Vector2 center, Vector2 radius, Color color)
	{
		var points = new Vector2[28];
		for (var index = 0; index < points.Length; index++)
		{
			var angle = Mathf.Tau * index / points.Length;
			points[index] = center + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
		}
		return AddScenePolygon(group, nodeName, points, color);
	}

	private Label AddSceneLabel(Godot.Collections.Array<CanvasItem> group, string nodeName, Vector2 position, Vector2 size, string text)
	{
		var label = new Label
		{
			Name = nodeName,
			Position = position,
			Size = size,
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 2,
		};
		label.AddThemeFontSizeOverride("font_size", 15);
		sceneLayer?.AddChild(label);
		group.Add(label);
		return label;
	}

	private void WireButtons()
	{
		WireButton("ChartButton", OnChartPressed);
		WireButton("SaveButton", OnSavePressed);
		WireButton("LoadButton", OnLoadPressed);
		WireButton("DeleteProgressButton", OnDeleteProgressPressed);
		WireButton("OchreDebugButton", OnOchreDebugButtonPressed);
	}

	private void WireButton(string nodeName, Action callback)
	{
		if (FindChild(nodeName, true, false) is not Button button)
		{
			return;
		}

		button.Pressed += callback;
		button.MouseEntered += () => OnButtonMouseEntered(button);
	}

	private void SelectDomainRoute(string routeId)
	{
		if (!domain.SelectRoute(routeId))
		{
			SetChartStatus($"航线不可选：{routeId}。{domain.Snapshot.LastStatus}");
			return;
		}

		selectedRoute = domain.Snapshot.SelectedRouteId;
		SetChartStatus($"已选择航线：{RouteName()}。按“确认出发”进入探索。");
		UpdateChartSceneSelection();
		UpdateOnboardingHint();
	}

	private void ShowChartSurface()
	{
		SetHubControlsEnabled(false);
		SetWorldMode("chart");
		UpdateChartSceneSelection();
		SetFooter("航图世界表面已展开：当前仅保留作者化航图场景和调试入口；Esc 返回船内。");
	}

	private void ShowExplorationSurface()
	{
		SetHubControlsEnabled(false);
		SetWorldMode("exploration");
		SetExplorationStatus();
		SetFooter("雾海搜撤开始：移动到残骸旁按 E 完成三段扫描，回到空艇旁按 E 预热并驾驶返航。");
	}

	private void ShowOchreDevSurface()
	{
		SetHubControlsEnabled(false);
		SetWorldMode("ochre_dev");
		UpdateOchreSceneSemantics();
		SetFooter("赭石岛开发入口：移动到条带状铁矿按 E 采集，或到返航点按 E 两步返回 Hub。");
	}

	private void UpdateChartSceneSelection()
	{
		if (chartMistSelectionFrame is not null)
		{
			chartMistSelectionFrame.Visible = currentScreen == "chart" && selectedRoute == "route.mist";
		}
	}

	private void SetExplorationStatus()
	{
		var snapshot = domain.Snapshot;
		UpdateExplorationSceneSemantics(snapshot);

		if (explorationStep <= 0)
		{
			SetExplorationLabels(
				$"资源压力：补给稳定；载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}",
				$"威胁反馈：{snapshot.ThreatText}；侦察 100%",
				$"船体状态：{snapshot.HullIntegrity}/100 完整，可继续探索",
				"恢复提示：靠近残骸后完成扫描、回声、打捞三段搜索");
		}
		else if (explorationStep == 1)
		{
			SetExplorationLabels(
				$"资源压力：搜索消耗 1 补给；发现信标水晶 {snapshot.RewardCarried} 箱，载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}",
				$"威胁反馈：{snapshot.ThreatText}；云影扰动已标记",
				$"船体状态：{snapshot.HullIntegrity}/100 完整，暂无损伤",
				"恢复提示：可继续扫描搜索，或回到空艇准备返航");
		}
		else if (explorationStep == 2)
		{
			SetExplorationLabels(
				$"资源压力：补给继续消耗；载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}",
				$"威胁反馈：{snapshot.ThreatText}；遭遇警报，侦察 72%",
				$"船体状态：{snapshot.HullIntegrity}/100 轻微擦伤",
				"恢复提示：建议驾驶空艇返航检查船体，或继续承担风险");
		}
		else
		{
			SetExplorationLabels(
				$"资源压力：载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}；收益已锁定",
				$"威胁反馈：{snapshot.ThreatText}；可安全撤离",
				$"船体状态：{snapshot.HullIntegrity}/100，可返航",
				"恢复提示：一轮压力循环完成，回到空艇驾驶返航闭环");
		}
	}

	private void SetExplorationLabels(string resourceText, string threatText, string hullText, string recoveryText)
	{
		SetFooter($"{RouteName()} | {resourceText} | {threatText} | {hullText} | {recoveryText}");
	}

	private void UpdateExplorationSceneSemantics(PlayableSliceSnapshot snapshot)
	{
		var step = Math.Clamp(snapshot.ExplorationStep, 0, 3);
		if (explorationRouteProgressFill is not null)
		{
			var width = 820.0f * (step / 3.0f);
			explorationRouteProgressFill.Size = new Vector2(width, explorationRouteProgressFill.Size.Y);
			explorationRouteProgressFill.CustomMinimumSize = explorationRouteProgressFill.Size;
		}
		UpdateExplorationMicroGameSemantics();

		var activePoint = string.IsNullOrWhiteSpace(snapshot.LastSearchPointId)
			? "未接近残骸"
			: string.IsNullOrWhiteSpace(snapshot.LastSearchPointName) ? snapshot.LastSearchPointId : snapshot.LastSearchPointName;
		if (explorationPointSemanticLabel is not null)
		{
			explorationPointSemanticLabel.Text = $"搜索点：{activePoint} / {snapshot.ExplorationSubstate}";
		}

		var threatActive = snapshot.ExplorationSubstate == "Threatened" || step >= 2;
		if (explorationThreatZone is not null)
		{
			explorationThreatZone.Visible = currentScreen == "exploration" && threatActive;
		}
		if (explorationThreatSemanticLabel is not null)
		{
			explorationThreatSemanticLabel.Text = threatActive
				? $"威胁区：{snapshot.ThreatText} / 船体 {snapshot.HullIntegrity}"
				: "威胁区：未触发";
			explorationThreatSemanticLabel.Visible = currentScreen == "exploration";
		}

		if (explorationExtractionSemanticLabel is not null)
		{
			explorationExtractionSemanticLabel.Text = step >= 3
				? $"撤离：收益锁定 {snapshot.CargoUsed}/{snapshot.CargoCapacity}"
				: $"撤离：携带 {snapshot.CargoUsed}/{snapshot.CargoCapacity}";
		}

		SetMarkerLabel(explorationSearchMarker, step >= 3 ? "已搜索" : searchPulseStage <= 0 ? "扫描 E" : $"扫描 {searchPulseStage + 1}/3");
		SetMarkerLabel(explorationReturnMarker, step >= 3 ? "驾驶返航 E" : returnPrepStage <= 0 ? "预热返航 E" : "起航 E");
	}

	private void UpdateExplorationMicroGameSemantics()
	{
		if (explorationSearchPulseFill is not null)
		{
			var width = 120.0f * (Math.Clamp(searchPulseStage, 0, 2) / 2.0f);
			explorationSearchPulseFill.Size = new Vector2(width, explorationSearchPulseFill.Size.Y);
			explorationSearchPulseFill.CustomMinimumSize = explorationSearchPulseFill.Size;
			explorationSearchPulseFill.Visible = currentScreen == "exploration";
		}
		if (explorationReturnPrepFill is not null)
		{
			var width = returnPrepStage > 0 ? 56.0f : 0.0f;
			explorationReturnPrepFill.Size = new Vector2(width, explorationReturnPrepFill.Size.Y);
			explorationReturnPrepFill.CustomMinimumSize = explorationReturnPrepFill.Size;
			explorationReturnPrepFill.Visible = currentScreen == "exploration";
		}
		SetMarkerLabel(explorationSearchMarker, searchPulseStage <= 0 ? "扫描 E" : $"扫描 {searchPulseStage + 1}/3");
		SetMarkerLabel(explorationReturnMarker, returnPrepStage <= 0 ? "预热返航 E" : "起航 E");
	}

	private void UpdateOchreSceneSemantics()
	{
		if (ochreOreSemanticLabel is not null)
		{
			ochreOreSemanticLabel.Text = ochreOreHarvested
				? "矿脉：已采集 / reward pending"
				: "矿脉：可采集 / reward pending";
		}
		if (ochreReturnSemanticLabel is not null)
		{
			ochreReturnSemanticLabel.Text = ochreReturnPrepStage > 0
				? "返航：已预热，再按 E 返回"
				: "返航：待预热";
		}
		SetMarkerLabel(ochreOreMarker, ochreOreHarvested ? "已采集" : "采集 E");
		SetMarkerLabel(ochreReturnMarker, ochreReturnPrepStage > 0 ? "返回 E" : "返航 E");
	}

	private static void SetMarkerLabel(Control? marker, string text)
	{
		if (marker?.FindChild($"{marker.Name}Label", false, false) is Label label)
		{
			label.Text = text;
		}
	}

	private void UpdateHubSummary()
	{
		var snapshot = domain.Snapshot;
		var cargoText = $"已用 {snapshot.CargoUsed} / 有效容量 {snapshot.CargoCapacity} / 受困货物 0";
		var hullText = snapshot.HullIntegrity >= 100
			? $"完整度 {snapshot.HullIntegrity} / 承载带稳定 / 可出航"
			: $"完整度 {snapshot.HullIntegrity} / 承载带轻伤 / 可出航";
		if (explorationStep <= 0)
		{
			var chartIdle = string.IsNullOrEmpty(selectedRoute) ? "航图：待规划" : $"航图：{RouteName()} / 待规划";
			SetHubLabels(snapshot.StorageText, cargoText, hullText, chartIdle, "货舱：可进入");
		}
		else if (explorationStep == 1)
		{
			SetHubLabels(snapshot.StorageText, cargoText, hullText, $"航图：{RouteName()} 进度 1/3", $"货舱：信标水晶 {snapshot.RewardInStorage + snapshot.RewardCarried} 箱待结算");
		}
		else if (explorationStep == 2)
		{
			SetHubLabels(snapshot.StorageText, cargoText, hullText, $"航图：{RouteName()} {snapshot.ThreatText} 2/3", $"货舱：载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}");
		}
		else
		{
			SetHubLabels(snapshot.StorageText, $"{cargoText} / 收益锁定", hullText.Replace("可出航", "可返航"), $"航图：{RouteName()} 压力循环完成 3/3", $"货舱：收益锁定 {snapshot.CargoUsed}/{snapshot.CargoCapacity}");
		}
		UpdateHubInteriorSemantics(snapshot);
	}

	private void UpdateHubInteriorSemantics(PlayableSliceSnapshot snapshot)
	{
		if (hubCabinStatusLabel is not null)
		{
			hubCabinStatusLabel.Text = explorationStep <= 0
				? (string.IsNullOrWhiteSpace(selectedRoute) ? "驾驶舱：待规划" : $"驾驶舱：{RouteName()}")
				: $"驾驶舱：{RouteName()} {Math.Min(explorationStep, 3)}/3";
		}

		var totalRewards = snapshot.RewardInStorage + snapshot.RewardCarried;
		if (hubCargoStatusLabel is not null)
		{
			hubCargoStatusLabel.Text = explorationStep >= 3
				? $"货舱：收益锁定 x{totalRewards}"
				: totalRewards > 0 ? $"货舱：信标水晶 x{totalRewards}" : "货舱：空载";
		}
		if (hubCargoLoadFill is not null)
		{
			var fillWidth = snapshot.CargoCapacity <= 0
				? 0.0f
				: Math.Clamp(snapshot.CargoUsed / (float)snapshot.CargoCapacity, 0.0f, 1.0f) * 72.0f;
			hubCargoLoadFill.Visible = currentScreen == "hub" && fillWidth > 0.0f;
			hubCargoLoadFill.Size = new Vector2(fillWidth, hubCargoLoadFill.Size.Y);
			hubCargoLoadFill.CustomMinimumSize = hubCargoLoadFill.Size;
		}

		if (hubEngineStatusLabel is not null)
		{
			hubEngineStatusLabel.Text = snapshot.HullIntegrity >= 100
				? "轮机间：稳定 100/100"
				: $"轮机间：擦伤 {snapshot.HullIntegrity}/100";
		}
		if (hubEngineWearOverlay is not null)
		{
			hubEngineWearOverlay.Visible = currentScreen == "hub" && snapshot.HullIntegrity < 100;
		}
	}

	private void SetHubLabels(string storageText, string cargoText, string hullText, string chartText, string cargoStationText)
	{
		if (storageValueLabel is not null) storageValueLabel.Text = storageText;
		if (cargoValueLabel is not null) cargoValueLabel.Text = cargoText;
		if (hullValueLabel is not null) hullValueLabel.Text = hullText;
		if (chartStationLabel is not null) chartStationLabel.Text = chartText;
		if (cargoStationLabel is not null) cargoStationLabel.Text = cargoStationText;
	}

	private string RouteName() =>
		string.IsNullOrWhiteSpace(selectedRoute)
			? "待选择航线"
			: domain.GetRouteDisplayName(selectedRoute);

	private void SetHubControlsEnabled(bool enabled)
	{
		for (var index = 0; index < hubActionButtons.Length; index++)
		{
			var button = hubActionButtons[index];
			if (button is not null)
			{
				var buttonEnabled = enabled && (index != 2 || hasLoadableProgress);
				if (index == 3)
				{
					buttonEnabled = enabled && HasAnyDurableProgressFile();
				}
				if (index == 4)
				{
					buttonEnabled = enabled && OS.IsDebugBuild();
				}
				button.Disabled = !buttonEnabled;
				button.FocusMode = buttonEnabled ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
			}
		}
		RefreshSaveDeleteAffordance();
	}

	private void SetWorldMode(string mode)
	{
		SetSceneGroupVisible(hubSceneItems, mode == "hub");
		SetSceneGroupVisible(hubExteriorSceneItems, mode == "hub" && hubSpace == "exterior");
		SetSceneGroupVisible(hubInteriorSceneItems, mode == "hub" && hubSpace == "interior");
		SetSceneGroupVisible(chartSceneItems, mode == "chart");
		SetSceneGroupVisible(explorationSceneItems, mode == "exploration");
		SetSceneGroupVisible(ochreSceneItems, mode == "ochre_dev");
		if (mode == "hub")
		{
			UpdateHubInteriorSemantics(domain.Snapshot);
		}
		if (mode == "chart")
		{
			UpdateChartSceneSelection();
		}
		if (hubShipEntryMarker is not null) hubShipEntryMarker.Visible = mode == "hub" && hubSpace == "exterior";
		if (hubShipExitMarker is not null) hubShipExitMarker.Visible = mode == "hub" && hubSpace == "interior";
		if (hubHelmMarker is not null) hubHelmMarker.Visible = mode == "hub" && hubSpace == "interior";
		if (hubStorageMarker is not null) hubStorageMarker.Visible = mode == "hub" && hubSpace == "interior";
		if (explorationSearchMarker is not null) explorationSearchMarker.Visible = mode == "exploration";
		if (explorationReturnMarker is not null) explorationReturnMarker.Visible = mode == "exploration";
		if (ochreOreMarker is not null) ochreOreMarker.Visible = mode == "ochre_dev";
		if (ochreReturnMarker is not null) ochreReturnMarker.Visible = mode == "ochre_dev";
		if (playerMarker is not null)
		{
			if (mode == "chart")
			{
				playerMarker.Visible = false;
			}
			else
			{
				playerMarker.Visible = true;
				if (mode == "hub" && currentScreen == "hub" && playerPosition == ExplorationPlayerStart)
				{
					playerPosition = HubPlayerStart;
				}
				playerPosition = ClampToCurrentWalkBounds(playerPosition);
				playerMarker.Position = playerPosition - (playerMarker.Size * 0.5f);
			}
		}
		UpdateSpatialInteraction();
	}

	private static void SetSceneGroupVisible(Godot.Collections.Array<CanvasItem> group, bool visible)
	{
		foreach (var item in group)
		{
			item.Visible = visible;
		}
	}

	private void UpdateSpatialInteraction()
	{
		nearestInteraction = "";
		var prompt = "WASD / 方向键移动，靠近可交互点按 E。";
		if (currentScreen == "hub")
		{
			if (hubSpace == "exterior")
			{
				var entryDistance = DistanceToMarker(hubShipEntryMarker);
				if (entryDistance <= InteractionRadius)
				{
					nearestInteraction = "hub_enter_ship";
					prompt = "按 E 从岛上码头登船，进入云织号内部。";
				}
			}
			else
			{
				var exitDistance = DistanceToMarker(hubShipExitMarker);
				var helmDistance = DistanceToMarker(hubHelmMarker);
				var storageDistance = DistanceToMarker(hubStorageMarker);
				var nearestHubDistance = Math.Min(exitDistance, Math.Min(helmDistance, storageDistance));
				if (exitDistance <= InteractionRadius && exitDistance <= nearestHubDistance)
				{
					nearestInteraction = "hub_exit_ship";
					prompt = "按 E 下船回到岛上码头。";
				}
				else if (helmDistance <= InteractionRadius && helmDistance <= nearestHubDistance)
				{
					nearestInteraction = "hub_helm";
					prompt = "按 E 使用驾驶舱航台：打开航图并选择航线。";
				}
				else if (storageDistance <= InteractionRadius && storageDistance <= nearestHubDistance)
				{
					nearestInteraction = "hub_storage";
					prompt = "按 E 检查货舱：确认资源与货物状态。";
				}
			}
		}
		else if (currentScreen == "exploration")
		{
			var searchDistance = DistanceToMarker(explorationSearchMarker);
			var returnDistance = DistanceToMarker(explorationReturnMarker);
			if (searchDistance <= InteractionRadius && searchDistance <= returnDistance)
			{
				nearestInteraction = "exploration_search";
				prompt = searchPulseStage <= 0
					? "按 E 开始搜索微交互：扫描残骸角度。"
					: $"按 E 继续搜索微交互：扫描 {searchPulseStage + 1}/3。";
			}
			else if (returnDistance <= InteractionRadius)
			{
				nearestInteraction = "exploration_return";
				prompt = returnPrepStage <= 0
					? "按 E 预热空艇返航引擎。"
					: "按 E 驾驶空艇返航。";
			}
		}
		else if (currentScreen == "ochre_dev")
		{
			var oreDistance = DistanceToMarker(ochreOreMarker);
			var returnDistance = DistanceToMarker(ochreReturnMarker);
			if (oreDistance <= InteractionRadius && oreDistance <= returnDistance)
			{
				nearestInteraction = "ochre_ore";
				prompt = ochreOreHarvested
					? "条带状铁矿已采集：正式 Resources 奖励待后续接入。"
					: "按 E 采集条带状铁矿：验证世界资源锚点。";
			}
			else if (returnDistance <= InteractionRadius)
			{
				nearestInteraction = "ochre_return";
				prompt = ochreReturnPrepStage <= 0
					? "按 E 预热赭石岛返航锚点。"
					: "按 E 从赭石岛开发入口返回 Hub。";
			}
		}

		if (interactionPromptLabel is not null)
		{
			interactionPromptLabel.Text = prompt;
		}
		RefreshExplorationActionAffordance();
	}

	private void RefreshExplorationActionAffordance()
	{
	}

	private float DistanceToMarker(Control? marker)
	{
		return marker is null || !marker.Visible
			? float.PositiveInfinity
			: playerPosition.DistanceTo(marker.Position + (marker.Size * 0.5f));
	}

	private static void CollectDescendantNames(Node node, string nameFragment, Godot.Collections.Array<string> matches)
	{
		foreach (var child in node.GetChildren())
		{
			if (child.Name.ToString().Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
			{
				matches.Add(child.GetPath().ToString());
			}

			CollectDescendantNames(child, nameFragment, matches);
		}
	}

	private Rect2 CurrentWalkBounds()
	{
		if (currentScreen == "exploration")
		{
			return ExplorationWalkBounds;
		}
		if (currentScreen == "ochre_dev")
		{
			return OchreIslandWalkBounds;
		}

		return hubSpace == "interior" ? ShipInteriorWalkBounds : HubWalkBounds;
	}

	private Vector2 ClampToCurrentWalkBounds(Vector2 position)
	{
		var bounds = CurrentWalkBounds();
		return position.Clamp(bounds.Position, bounds.Position + bounds.Size);
	}

	private static bool IsSaveShortcut(InputEventKey key) =>
		key.CtrlPressed && !key.AltPressed && !key.MetaPressed && key.Keycode == Key.S;

	private static bool IsLoadShortcut(InputEventKey key) =>
		key.CtrlPressed && !key.AltPressed && !key.MetaPressed && key.Keycode == Key.L;

	private void SetChartStatus(string text)
	{
		SetFooter(text);
	}

	private void SetSaveStatus(string text)
	{
		if (saveStatusLabel is not null) saveStatusLabel.Text = text;
	}

	private void SetFooter(string text)
	{
		if (footerLabel is not null) footerLabel.Text = text;
		if (interactionPromptLabel is not null) interactionPromptLabel.Text = text;
	}

	private void UpdateOnboardingHint()
	{
		if (runtimeHintLabel is null)
		{
			return;
		}

		var hint = onboarding.EvaluateNextHint();
		runtimeHintLabel.Text = hint is null
			? "新手提示：首轮航线已完成，可继续自由探索。"
			: HintText(hint.StepId);
	}

	private static string HintText(string stepId)
	{
		return stepId switch
		{
			OnboardingManager.FindHubHudStepId => "新手提示：查看船内状态，按 M 或靠近舵台按 E 打开航图桌。",
			OnboardingManager.OpenChartStepId => "新手提示：打开航图后选择一条可见航线。",
			OnboardingManager.SelectRouteStepId => "新手提示：选择“雾海短程”，然后确认出发。",
			OnboardingManager.DepartRouteStepId => "新手提示：确认出发会进入雾海搜撤。",
			OnboardingManager.AdvancePressureStepId => "新手提示：靠近漂浮残骸按 E 搜索，观察资源、威胁和船体反馈。",
			OnboardingManager.NoticeSaveLoadStepId => "新手提示：Ctrl+S 保存、Ctrl+L 加载；按钮也可用。",
			OnboardingManager.ReturnHubStepId => "新手提示：靠近返航信标按 E 返回空艇。",
			OnboardingManager.NoticeSummaryChangeStepId => "新手提示：返回后查看货舱、船体和航图摘要变化。",
			_ => "新手提示：继续完成当前首轮目标。",
		};
	}

	private string LabelText(string nodeName) => (FindChild(nodeName, true, false) as Label)?.Text ?? "";

	private void GrabButton(string nodeName)
	{
		if (FindChild(nodeName, true, false) is Button button && button.Visible && !button.Disabled)
		{
			button.GrabFocus();
		}
	}

	private Control? FindControl(string nodeName) => FindChild(nodeName, true, false) as Control;

	private static bool IsVisible(CanvasItem? item) => item is not null && item.Visible;

	private Button? CreateDeleteProgressButton()
	{
		if (FindChild("DeleteProgressButton", true, false) is Button existing)
		{
			return existing;
		}
		if (FindChild("ActionStack", true, false) is not VBoxContainer actionStack)
		{
			return null;
		}

		var button = new Button
		{
			Name = "DeleteProgressButton",
			Text = "删除本地航行日志",
			FocusMode = Control.FocusModeEnum.All,
			Disabled = true,
		};
		actionStack.AddChild(button);
		if (saveStatusLabel is not null)
		{
			actionStack.MoveChild(button, saveStatusLabel.GetIndex());
		}
		return button;
	}

	private Button? CreateOchreDebugButton()
	{
		if (FindChild("OchreDebugButton", true, false) is Button existing)
		{
			existing.Visible = OS.IsDebugBuild();
			return existing;
		}
		if (interactionLayer is null)
		{
			return null;
		}

		var button = new Button
		{
			Name = "OchreDebugButton",
			Text = "赭石岛调试入口",
			Position = new Vector2(1018, 132),
			CustomMinimumSize = new Vector2(190, 38),
			Size = new Vector2(190, 38),
			FocusMode = Control.FocusModeEnum.All,
			ZIndex = 30,
			Visible = OS.IsDebugBuild(),
		};
		interactionLayer.AddChild(button);
		return button;
	}

	private static void OnButtonMouseEntered(Button button)
	{
		if (button.Visible && !button.Disabled)
		{
			button.GrabFocus();
		}
	}

	private bool TryWriteDurableProgressToDisk(out string reason)
	{
		reason = string.Empty;
		var json = domain.ExportProgressJson();
		if (string.IsNullOrWhiteSpace(json))
		{
			reason = "empty_progress_manifest";
			return false;
		}

		using var file = Godot.FileAccess.Open(DurableProgressPath, Godot.FileAccess.ModeFlags.Write);
		if (file is null)
		{
			reason = Godot.FileAccess.GetOpenError().ToString();
			return false;
		}

		file.StoreString(json);
		return true;
	}

	private bool TryImportDurableProgressFromDisk(bool updateStatus)
	{
		if (!Godot.FileAccess.FileExists(DurableProgressPath))
		{
			hasLoadableProgress = false;
			lastDurableImportFailure = Godot.FileAccess.FileExists(QuarantinedProgressPath)
				? "存档校验失败，已隔离；可重新保存新进度。"
				: "";
			if (updateStatus)
			{
				SetSaveStatus(string.IsNullOrWhiteSpace(lastDurableImportFailure)
					? "暂无可读取航行日志：记录后可读取。"
					: $"本地存档已隔离：{lastDurableImportFailure}");
			}
			RefreshSaveDeleteAffordance();
			return false;
		}

		string json;
		using (var file = Godot.FileAccess.Open(DurableProgressPath, Godot.FileAccess.ModeFlags.Read))
		{
			if (file is null)
			{
				hasLoadableProgress = false;
				lastDurableImportFailure = Godot.FileAccess.GetOpenError().ToString();
				if (updateStatus)
				{
					SetSaveStatus($"本地存档读取失败：{lastDurableImportFailure}");
				}
				RefreshSaveDeleteAffordance();
				return false;
			}

			json = file.GetAsText();
		}

		if (domain.TryImportProgressJson(json, out var reason))
		{
			hasLoadableProgress = true;
			lastDurableImportFailure = "";
			if (updateStatus)
			{
				SetSaveStatus("本地航行日志已导入，可读取核心进度。");
			}
			RefreshSaveDeleteAffordance();
			return true;
		}

		hasLoadableProgress = false;
		lastDurableImportFailure = reason == "checksum_mismatch"
			? "存档校验失败，已隔离；可重新保存新进度。"
			: reason;
		QuarantineDurableProgress(json);
		if (updateStatus)
		{
			SetSaveStatus($"本地存档导入失败：{lastDurableImportFailure}");
		}
		RefreshSaveDeleteAffordance();
		return false;
	}

	private static void QuarantineDurableProgress(string json)
	{
		using var directory = DirAccess.Open("user://");
		directory?.Remove(QuarantinedProgressFileName);
		if (directory?.Rename(DurableProgressFileName, QuarantinedProgressFileName) == Error.Ok)
		{
			return;
		}

		using (var quarantineFile = Godot.FileAccess.Open(QuarantinedProgressPath, Godot.FileAccess.ModeFlags.Write))
		{
			quarantineFile?.StoreString(json);
		}
		directory?.Remove(DurableProgressFileName);
	}

	private bool HasAnyDurableProgressFile() =>
		Godot.FileAccess.FileExists(DurableProgressPath) || Godot.FileAccess.FileExists(QuarantinedProgressPath);

	private void RefreshSaveDeleteAffordance()
	{
		if (hubActionButtons[2] is Button loadButton && currentScreen == "hub")
		{
			loadButton.Disabled = !hasLoadableProgress;
			loadButton.FocusMode = hasLoadableProgress ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
		}
		if (hubActionButtons[1] is Button saveButton)
		{
			saveButton.Text = pendingOverwriteConfirmation ? "确认覆盖航行日志" : "记录航行  Ctrl+S";
		}
		if (deleteProgressButton is not null)
		{
			var enabled = currentScreen == "hub" && HasAnyDurableProgressFile();
			deleteProgressButton.Disabled = !enabled;
			deleteProgressButton.FocusMode = enabled ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
			deleteProgressButton.Text = pendingDeleteConfirmation ? "确认删除本地航行日志" : "删除本地航行日志";
		}
		if (ochreDebugButton is not null)
		{
			var enabled = currentScreen == "hub" && OS.IsDebugBuild();
			ochreDebugButton.Visible = OS.IsDebugBuild();
			ochreDebugButton.Disabled = !enabled;
			ochreDebugButton.FocusMode = enabled ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
		}
	}

}
