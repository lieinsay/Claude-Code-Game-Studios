using System;
using Godot;
using CloudWeaverVoyage.Presentation;

public partial class HubRuntime : Node2D
{
	private static readonly Vector2 HubPlayerStart = new(158, 610);
	private static readonly Vector2 ExplorationPlayerStart = new(168, 610);
	private static readonly Rect2 HubWalkBounds = new(new Vector2(132, 380), new Vector2(1016, 252));
	private static readonly Rect2 ExplorationWalkBounds = new(new Vector2(132, 390), new Vector2(1016, 246));
	private const float PlayerSpeed = 260.0f;
	private const float InteractionRadius = 74.0f;

	private readonly PlayableSliceDomainAdapter domain = new();
	private readonly OnboardingManager onboarding = new();
	private Control? chartPanel;
	private Control? explorationPanel;
	private Control? hubRoot;
	private Control? playableLayer;
	private Control? sceneLayer;
	private Control? interactionLayer;
	private ColorRect? playerMarker;
	private Label? interactionPromptLabel;
	private ColorRect? hubHelmMarker;
	private ColorRect? hubStorageMarker;
	private ColorRect? explorationSearchMarker;
	private ColorRect? explorationReturnMarker;
	private ColorRect? explorationRouteProgressFill;
	private ColorRect? explorationThreatZone;
	private ColorRect? extractionCargoProp;
	private readonly Godot.Collections.Array<CanvasItem> hubSceneItems = [];
	private readonly Godot.Collections.Array<CanvasItem> explorationSceneItems = [];
	private Label? chartStatusLabel;
	private Label? explorationRouteLabel;
	private Label? explorationResourceLabel;
	private Label? explorationThreatLabel;
	private Label? explorationHullLabel;
	private Label? explorationRecoveryLabel;
	private Label? explorationPointSemanticLabel;
	private Label? explorationThreatSemanticLabel;
	private Label? explorationExtractionSemanticLabel;
	private Label? storageValueLabel;
	private Label? cargoValueLabel;
	private Label? hullValueLabel;
	private Label? chartStationLabel;
	private Label? cargoStationLabel;
	private Label? saveStatusLabel;
	private Label? runtimeHintLabel;
	private Label? footerLabel;
	private readonly Button?[] hubActionButtons = new Button?[3];
	private string selectedRoute = "";
	private string currentScreen = "hub";
	private int explorationStep;
	private Vector2 playerPosition = HubPlayerStart;
	private string nearestInteraction = "";

	public override void _Ready()
	{
		CacheNodes();
		domain.RegisterOnboarding(onboarding);
		CreatePlayableLayer();
		WireButtons();
		ShowHub();
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

		if (IsVisible(explorationPanel))
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

		if (IsVisible(chartPanel))
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
		currentScreen = "chart";
		domain.OpenChart();
		ShowChartPanel();
		SetChartStatus("HUD / 航图已打开：选择一条 MVP 航线后确认出发。");
		UpdateOnboardingHint();
	}

	public void OnRouteMistPressed() => SelectDomainRoute("route.mist");

	public void OnRouteMarketPressed() => SelectDomainRoute("route.market");

	public void OnDepartPressed()
	{
		if (string.IsNullOrEmpty(selectedRoute))
		{
			SetChartStatus("请选择航线后再确认出发。");
			GrabButton("RouteMistButton");
			return;
		}

		if (!domain.ConfirmDeparture())
		{
			SetChartStatus($"出航失败：{domain.Snapshot.LastStatus}");
			GrabButton("DepartButton");
			return;
		}

		currentScreen = "exploration";
		explorationStep = 0;
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

		domain.AdvanceExploration();
		explorationStep = domain.Snapshot.ExplorationStep;
		SetExplorationStatus();
		UpdateOnboardingHint();
		if (explorationStep >= 3)
		{
			SetFooter("一轮探索压力循环完成：返回 Hub 后可继续复测闭环。Ctrl+S 保存，Ctrl+L 加载。");
			GrabButton("ExplorationReturnButton");
		}
		else
		{
			SetFooter("探索推进：压力、威胁、船体反馈已更新。Ctrl+S 保存，Ctrl+L 加载，Esc 返回 Hub。");
			GrabButton("ExplorationAdvanceButton");
		}
	}

	public void OnExplorationReturnPressed()
	{
		domain.ReturnToHub();
		ShowHub();
		UpdateOnboardingHint();
	}

	public void OnSavePressed()
	{
		onboarding.ObserveSaveLoadAwareness(visibleOrUsed: true, ownerStateAlreadyMutated: true);
		var result = domain.SaveSceneState(new PlayableSliceSceneState(
			currentScreen,
			selectedRoute,
			explorationStep,
			playerPosition.X,
			playerPosition.Y,
			LabelText("Footer")));
		SetSaveStatus(result.Success
			? $"保存完成：canonical progress gen {result.Generation}"
			: $"保存失败：{result.Reason}");
		UpdateOnboardingHint();
	}

	public void OnLoadPressed()
	{
		var (result, restored) = domain.LoadSceneState();
		if (!result.Success)
		{
			SetSaveStatus($"加载失败：{result.Reason}");
			return;
		}

		currentScreen = string.IsNullOrWhiteSpace(restored.Screen) ? "hub" : restored.Screen;
		selectedRoute = restored.Route ?? "";
		explorationStep = Math.Max(0, restored.ExplorationStep);
		playerPosition = new Vector2(restored.PlayerX, restored.PlayerY);
		SetSaveStatus($"加载完成：canonical progress gen {result.Generation} / {currentScreen}");

		if (currentScreen == "chart")
		{
			ShowChartPanel();
			SetChartStatus($"已从存档恢复航线：{selectedRoute}");
		}
		else if (currentScreen == "exploration")
		{
			ShowExplorationSurface();
			SetSaveStatus($"加载完成：canonical progress gen {result.Generation} / 探索 HUD");
		}
		else
		{
			ShowHub();
		}
		UpdateOnboardingHint();
	}

	public void ShowHub()
	{
		currentScreen = "hub";
		if (chartPanel is not null)
		{
			chartPanel.Visible = false;
		}
		if (explorationPanel is not null)
		{
			explorationPanel.Visible = false;
		}
		SetHubControlsEnabled(true);
		UpdateHubSummary();
		SetWorldMode("hub");
		SetFooter("HUD 入口：点击“打开航图 / HUD”或按 M。保存/加载可用按钮或 Ctrl+S / Ctrl+L。");
		GrabButton("ChartButton");
		UpdateOnboardingHint();
	}

	public void TrySpatialInteraction()
	{
		UpdateSpatialInteraction();
		if (nearestInteraction == "hub_helm")
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
		else
		{
			SetFooter("附近没有可交互点：移动到标记旁按 E。");
		}
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
		chartPanel = FindControl("ChartPanel");
		explorationPanel = FindControl("ExplorationPanel");
		chartStatusLabel = FindChild("ChartStatusLabel", true, false) as Label;
		explorationRouteLabel = FindChild("ExplorationRouteLabel", true, false) as Label;
		explorationResourceLabel = FindChild("ExplorationResourceLabel", true, false) as Label;
		explorationThreatLabel = FindChild("ExplorationThreatLabel", true, false) as Label;
		explorationHullLabel = FindChild("ExplorationHullLabel", true, false) as Label;
		explorationRecoveryLabel = FindChild("ExplorationRecoveryLabel", true, false) as Label;
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
			ZIndex = 4,
		};
		playableLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		hubRoot.AddChild(playableLayer);

		sceneLayer = new Control
		{
			Name = "WorldSceneLayer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = -4,
		};
		sceneLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		playableLayer.AddChild(sceneLayer);

		interactionLayer = new Control
		{
			Name = "WorldInteractionLayer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 2,
		};
		interactionLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		playableLayer.AddChild(interactionLayer);

		AddHubGreyboxSet();
		AddExplorationGreyboxSet();
		hubHelmMarker = AddWorldMarker("HelmInteractPoint", new Vector2(316, 594), new Color(0.22f, 0.58f, 0.72f), "舵台 E");
		hubStorageMarker = AddWorldMarker("StorageInteractPoint", new Vector2(536, 594), new Color(0.58f, 0.45f, 0.26f), "仓储 E");
		explorationSearchMarker = AddWorldMarker("SearchInteractPoint", new Vector2(592, 594), new Color(0.46f, 0.67f, 0.33f), "搜索 E");
		explorationReturnMarker = AddWorldMarker("ReturnInteractPoint", new Vector2(1012, 594), new Color(0.64f, 0.39f, 0.28f), "返航 E");

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
	}

	private void AddHubGreyboxSet()
	{
		AddSceneRect(hubSceneItems, "HubIslandWalkBoundary", HubWalkBounds.Position, HubWalkBounds.Size, new Color(0.13f, 0.26f, 0.25f, 0.90f));
		AddSceneRect(hubSceneItems, "HubIslandUpperEdge", new Vector2(132, 380), new Vector2(1016, 10), new Color(0.46f, 0.66f, 0.60f, 0.95f));
		AddSceneRect(hubSceneItems, "HubIslandLowerEdge", new Vector2(132, 622), new Vector2(1016, 10), new Color(0.46f, 0.66f, 0.60f, 0.95f));
		AddSceneRect(hubSceneItems, "HubShipHull", new Vector2(236, 448), new Vector2(772, 174), new Color(0.19f, 0.26f, 0.31f, 0.92f));
		AddSceneRect(hubSceneItems, "HubBoardingRamp", new Vector2(168, 568), new Vector2(96, 34), new Color(0.48f, 0.42f, 0.31f, 0.96f));
		AddSceneLabel(hubSceneItems, "HubBoardingRampLabel", new Vector2(168, 544), new Vector2(104, 22), "登船坡道");
		AddSceneRect(hubSceneItems, "HubCabinRoom", new Vector2(274, 464), new Vector2(184, 64), new Color(0.21f, 0.34f, 0.40f, 0.94f));
		AddSceneLabel(hubSceneItems, "HubCabinRoomLabel", new Vector2(296, 472), new Vector2(140, 22), "驾驶舱");
		AddSceneRect(hubSceneItems, "HubCargoRoom", new Vector2(506, 464), new Vector2(184, 64), new Color(0.37f, 0.30f, 0.21f, 0.94f));
		AddSceneLabel(hubSceneItems, "HubCargoRoomLabel", new Vector2(530, 472), new Vector2(136, 22), "货舱");
		AddSceneRect(hubSceneItems, "HubEngineRoom", new Vector2(766, 464), new Vector2(184, 64), new Color(0.28f, 0.30f, 0.38f, 0.94f));
		AddSceneLabel(hubSceneItems, "HubEngineRoomLabel", new Vector2(788, 472), new Vector2(140, 22), "轮机间");
		AddSceneRect(hubSceneItems, "HubDeckFloor", new Vector2(88, 526), new Vector2(1096, 112), new Color(0.18f, 0.23f, 0.24f, 0.88f));
		AddSceneRect(hubSceneItems, "HubDeckRail", new Vector2(104, 502), new Vector2(1064, 12), new Color(0.43f, 0.55f, 0.56f, 0.95f));
		AddSceneRect(hubSceneItems, "HelmConsoleProp", new Vector2(286, 546), new Vector2(150, 58), new Color(0.14f, 0.38f, 0.48f, 0.95f));
		AddSceneLabel(hubSceneItems, "HelmConsoleLabel", new Vector2(298, 552), new Vector2(126, 22), "航图舵台");
		AddSceneRect(hubSceneItems, "StorageCrateProp", new Vector2(520, 548), new Vector2(132, 56), new Color(0.42f, 0.34f, 0.22f, 0.95f));
		AddSceneRect(hubSceneItems, "StorageCrateBand", new Vector2(536, 564), new Vector2(100, 10), new Color(0.68f, 0.59f, 0.39f, 0.95f));
		AddSceneLabel(hubSceneItems, "StorageCrateLabel", new Vector2(527, 552), new Vector2(118, 22), "仓储货箱");
		AddSceneRect(hubSceneItems, "ModuleBenchProp", new Vector2(792, 548), new Vector2(156, 56), new Color(0.26f, 0.30f, 0.38f, 0.92f));
		AddSceneLabel(hubSceneItems, "ModuleBenchLabel", new Vector2(806, 552), new Vector2(128, 22), "模块检修台");
	}

	private void AddExplorationGreyboxSet()
	{
		AddSceneRect(explorationSceneItems, "ExplorationIslandWalkBoundary", ExplorationWalkBounds.Position, ExplorationWalkBounds.Size, new Color(0.12f, 0.27f, 0.24f, 0.90f));
		AddSceneRect(explorationSceneItems, "ExplorationIslandUpperEdge", new Vector2(132, 390), new Vector2(1016, 12), new Color(0.45f, 0.64f, 0.54f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationIslandLowerEdge", new Vector2(132, 624), new Vector2(1016, 12), new Color(0.45f, 0.64f, 0.54f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationDockedShip", new Vector2(156, 546), new Vector2(154, 74), new Color(0.20f, 0.27f, 0.33f, 0.96f));
		AddSceneRect(explorationSceneItems, "ExplorationBoardingRamp", new Vector2(290, 580), new Vector2(92, 28), new Color(0.48f, 0.42f, 0.31f, 0.96f));
		AddSceneLabel(explorationSceneItems, "ExplorationBoardingRampLabel", new Vector2(166, 522), new Vector2(132, 22), "靠岸空艇");
		AddSceneRect(explorationSceneItems, "ExplorationIslandPath", new Vector2(342, 584), new Vector2(610, 20), new Color(0.32f, 0.44f, 0.36f, 0.95f));
		AddSceneRect(explorationSceneItems, "ExplorationSkyField", new Vector2(92, 500), new Vector2(1092, 138), new Color(0.10f, 0.20f, 0.25f, 0.90f));
		AddSceneRect(explorationSceneItems, "ExplorationRouteTrail", new Vector2(176, 586), new Vector2(820, 14), new Color(0.34f, 0.54f, 0.50f, 0.95f));
		explorationRouteProgressFill = AddSceneRect(explorationSceneItems, "ExplorationRouteProgressFill", new Vector2(176, 586), new Vector2(0, 14), new Color(0.78f, 0.70f, 0.36f, 0.98f));
		explorationPointSemanticLabel = AddSceneLabel(explorationSceneItems, "ExplorationPointSemanticLabel", new Vector2(186, 508), new Vector2(360, 24), "搜索点：待接近");
		AddSceneRect(explorationSceneItems, "SearchWreckProp", new Vector2(558, 540), new Vector2(170, 66), new Color(0.28f, 0.42f, 0.29f, 0.96f));
		AddSceneRect(explorationSceneItems, "SearchWreckHighlight", new Vector2(590, 556), new Vector2(104, 12), new Color(0.62f, 0.75f, 0.42f, 0.98f));
		AddSceneLabel(explorationSceneItems, "SearchWreckLabel", new Vector2(574, 548), new Vector2(138, 22), "漂浮残骸");
		explorationThreatZone = AddSceneRect(explorationSceneItems, "ExplorationThreatZone", new Vector2(768, 528), new Vector2(150, 68), new Color(0.62f, 0.25f, 0.22f, 0.52f));
		explorationThreatSemanticLabel = AddSceneLabel(explorationSceneItems, "ExplorationThreatSemanticLabel", new Vector2(756, 506), new Vector2(190, 24), "威胁区：未触发");
		AddSceneRect(explorationSceneItems, "ReturnBeaconProp", new Vector2(986, 526), new Vector2(104, 82), new Color(0.50f, 0.28f, 0.24f, 0.96f));
		AddSceneRect(explorationSceneItems, "ReturnBeaconCore", new Vector2(1024, 540), new Vector2(28, 52), new Color(0.78f, 0.62f, 0.42f, 0.98f));
		AddSceneLabel(explorationSceneItems, "ReturnBeaconLabel", new Vector2(990, 532), new Vector2(96, 22), "返航信标");
		extractionCargoProp = AddSceneRect(explorationSceneItems, "ExtractionCargoProp", new Vector2(930, 602), new Vector2(110, 24), new Color(0.68f, 0.58f, 0.34f, 0.94f));
		explorationExtractionSemanticLabel = AddSceneLabel(explorationSceneItems, "ExplorationExtractionSemanticLabel", new Vector2(924, 508), new Vector2(246, 24), "撤离：可随时返航");
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
			ZIndex = -2,
		};
		sceneLayer?.AddChild(rect);
		group.Add(rect);
		return rect;
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
			ZIndex = -1,
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
		WireButton("RouteMistButton", OnRouteMistPressed);
		WireButton("RouteMarketButton", OnRouteMarketPressed);
		WireButton("DepartButton", OnDepartPressed);
		WireButton("ChartCloseButton", ShowHub);
		WireButton("ExplorationAdvanceButton", OnExplorationAdvancePressed);
		WireButton("ExplorationReturnButton", OnExplorationReturnPressed);
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
		GrabButton("DepartButton");
		UpdateOnboardingHint();
	}

	private void ShowChartPanel()
	{
		if (chartPanel is not null)
		{
			chartPanel.Visible = true;
		}
		if (explorationPanel is not null)
		{
			explorationPanel.Visible = false;
		}
		SetHubControlsEnabled(false);
		SetWorldMode("chart");
		SetFooter("HUD / 航图已接管输入：方向键仅在航图内移动，Esc 返回 Hub。");
		GrabButton("RouteMistButton");
	}

	private void ShowExplorationSurface()
	{
		if (chartPanel is not null)
		{
			chartPanel.Visible = false;
		}
		if (explorationPanel is not null)
		{
			explorationPanel.Visible = true;
		}
		SetHubControlsEnabled(false);
		SetWorldMode("exploration");
		SetExplorationStatus();
		SetFooter("探索 HUD 已接管输入：点击“推进探索 / 搜索”产生压力变化。Ctrl+S 保存，Ctrl+L 加载，Esc 返回 Hub。");
		GrabButton("ExplorationAdvanceButton");
	}

	private void SetExplorationStatus()
	{
		var snapshot = domain.Snapshot;
		if (explorationRouteLabel is not null)
		{
			explorationRouteLabel.Text = $"路线：{RouteName()}；探索进度 {snapshot.ExplorationStep}/3";
		}
		UpdateExplorationSceneSemantics(snapshot);

		if (explorationStep <= 0)
		{
			SetExplorationLabels(
				$"资源压力：补给稳定；载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}",
				$"威胁反馈：{snapshot.ThreatText}；侦察 100%",
				$"船体状态：{snapshot.HullIntegrity}/100 完整，可继续探索",
				"恢复提示：点击“推进探索 / 搜索”开始压力循环");
		}
		else if (explorationStep == 1)
		{
			SetExplorationLabels(
				$"资源压力：搜索消耗 1 补给；发现信标水晶 {snapshot.RewardCarried} 箱，载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}",
				$"威胁反馈：{snapshot.ThreatText}；云影扰动已标记",
				$"船体状态：{snapshot.HullIntegrity}/100 完整，暂无损伤",
				"恢复提示：可继续搜索，或返回 Hub 保留收益");
		}
		else if (explorationStep == 2)
		{
			SetExplorationLabels(
				$"资源压力：补给继续消耗；载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}",
				$"威胁反馈：{snapshot.ThreatText}；遭遇警报，侦察 72%",
				$"船体状态：{snapshot.HullIntegrity}/100 轻微擦伤",
				"恢复提示：建议返回 Hub 检查船体，或继续承担风险");
		}
		else
		{
			SetExplorationLabels(
				$"资源压力：载货 {snapshot.CargoUsed}/{snapshot.CargoCapacity}；收益已锁定",
				$"威胁反馈：{snapshot.ThreatText}；可安全撤离",
				$"船体状态：{snapshot.HullIntegrity}/100，可返航",
				"恢复提示：一轮压力循环完成，点击返回 Hub 闭环");
		}
	}

	private void SetExplorationLabels(string resourceText, string threatText, string hullText, string recoveryText)
	{
		if (explorationResourceLabel is not null) explorationResourceLabel.Text = resourceText;
		if (explorationThreatLabel is not null) explorationThreatLabel.Text = threatText;
		if (explorationHullLabel is not null) explorationHullLabel.Text = hullText;
		if (explorationRecoveryLabel is not null) explorationRecoveryLabel.Text = recoveryText;
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

		var activePoint = string.IsNullOrWhiteSpace(snapshot.LastSearchPointId)
			? "待接近"
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

		if (extractionCargoProp is not null)
		{
			extractionCargoProp.Color = step >= 3
				? new Color(0.84f, 0.70f, 0.32f, 0.98f)
				: new Color(0.68f, 0.58f, 0.34f, 0.94f);
		}
		if (explorationExtractionSemanticLabel is not null)
		{
			explorationExtractionSemanticLabel.Text = step >= 3
				? $"撤离：收益锁定 {snapshot.CargoUsed}/{snapshot.CargoCapacity}"
				: $"撤离：携带 {snapshot.CargoUsed}/{snapshot.CargoCapacity}";
		}

		SetMarkerLabel(explorationSearchMarker, step >= 3 ? "已搜索" : $"搜索 {Math.Min(step + 1, 3)}/3");
		SetMarkerLabel(explorationReturnMarker, step >= 3 ? "结算 E" : "返航 E");
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
			? "未命名航线"
			: domain.GetRouteDisplayName(selectedRoute);

	private void SetHubControlsEnabled(bool enabled)
	{
		foreach (var button in hubActionButtons)
		{
			if (button is not null)
			{
				button.Disabled = !enabled;
				button.FocusMode = enabled ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
			}
		}
	}

	private void SetWorldMode(string mode)
	{
		SetSceneGroupVisible(hubSceneItems, mode == "hub");
		SetSceneGroupVisible(explorationSceneItems, mode == "exploration");
		if (hubHelmMarker is not null) hubHelmMarker.Visible = mode == "hub";
		if (hubStorageMarker is not null) hubStorageMarker.Visible = mode == "hub";
		if (explorationSearchMarker is not null) explorationSearchMarker.Visible = mode == "exploration";
		if (explorationReturnMarker is not null) explorationReturnMarker.Visible = mode == "exploration";
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
			var helmDistance = DistanceToMarker(hubHelmMarker);
			var storageDistance = DistanceToMarker(hubStorageMarker);
			if (helmDistance <= InteractionRadius && helmDistance <= storageDistance)
			{
				nearestInteraction = "hub_helm";
				prompt = "按 E 使用舵台：打开航图并选择航线。";
			}
			else if (storageDistance <= InteractionRadius)
			{
				nearestInteraction = "hub_storage";
				prompt = "按 E 检查仓储：确认资源与货舱状态。";
			}
		}
		else if (currentScreen == "exploration")
		{
			var searchDistance = DistanceToMarker(explorationSearchMarker);
			var returnDistance = DistanceToMarker(explorationReturnMarker);
			if (searchDistance <= InteractionRadius && searchDistance <= returnDistance)
			{
				nearestInteraction = "exploration_search";
				prompt = "按 E 搜索事件点：获得资源或压力反馈。";
			}
			else if (returnDistance <= InteractionRadius)
			{
				nearestInteraction = "exploration_return";
				prompt = "按 E 返回 Hub：结算当前探索反馈。";
			}
		}

		if (interactionPromptLabel is not null)
		{
			interactionPromptLabel.Text = prompt;
		}
	}

	private float DistanceToMarker(Control? marker)
	{
		return marker is null || !marker.Visible
			? float.PositiveInfinity
			: playerPosition.DistanceTo(marker.Position + (marker.Size * 0.5f));
	}

	private Rect2 CurrentWalkBounds() => currentScreen == "exploration" ? ExplorationWalkBounds : HubWalkBounds;

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
		if (chartStatusLabel is not null) chartStatusLabel.Text = text;
	}

	private void SetSaveStatus(string text)
	{
		if (saveStatusLabel is not null) saveStatusLabel.Text = text;
	}

	private void SetFooter(string text)
	{
		if (footerLabel is not null) footerLabel.Text = text;
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
			OnboardingManager.FindHubHudStepId => "新手提示：查看 Hub 状态，按 M 或靠近舵台按 E 打开航图。",
			OnboardingManager.OpenChartStepId => "新手提示：打开航图后选择一条可见航线。",
			OnboardingManager.SelectRouteStepId => "新手提示：选择“雾海短程”或旧集市航道，然后确认出发。",
			OnboardingManager.DepartRouteStepId => "新手提示：确认出发会进入探索 HUD。",
			OnboardingManager.AdvancePressureStepId => "新手提示：靠近漂浮残骸按 E 搜索，观察资源、威胁和船体反馈。",
			OnboardingManager.NoticeSaveLoadStepId => "新手提示：Ctrl+S 保存、Ctrl+L 加载；按钮也可用。",
			OnboardingManager.ReturnHubStepId => "新手提示：靠近返航信标按 E 返回 Hub。",
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

	private static void OnButtonMouseEntered(Button button)
	{
		if (button.Visible && !button.Disabled)
		{
			button.GrabFocus();
		}
	}

}
