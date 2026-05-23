extends SceneTree

const SESSION_SCENE := "res://src/scenes/SessionShell.tscn"
const SCREENSHOT_PATH := "user://session_shell_hub_probe.png"
const EXPLORATION_SEMANTICS_SCREENSHOT_PATH := "user://session_shell_exploration_semantics_probe.png"

var _failed := false


func _init() -> void:
	root.size = Vector2i(1280, 720)
	call_deferred("_run")


func _run() -> void:
	var packed := load(SESSION_SCENE) as PackedScene
	_expect(packed != null, "SessionShell scene loads")
	if packed == null:
		_finish()
		return

	var session := packed.instantiate()
	root.add_child(session)
	await process_frame
	await process_frame

	_expect(_is_panel_visible(session, "EntryPanel"), "Entry panel is visible on boot")

	session.call("_on_start_pressed")
	await process_frame
	_expect(_is_panel_visible(session, "AudioActivationPanel"), "Audio activation panel is visible after Start")
	_expect(_label_text(session, "AudioPromptLabel") == "启用音频后开始。", "Audio prompt uses Chinese text")

	session.call("_on_audio_confirmed")
	await process_frame
	await process_frame

	_expect(not _is_panel_visible(session, "EntryPanel"), "Entry panel is hidden after audio confirmation")
	_expect(not _is_panel_visible(session, "AudioActivationPanel"), "Audio panel is hidden after audio confirmation")
	_expect(_control_mouse_filter(session, "ShellUiRoot") == Control.MOUSE_FILTER_IGNORE, "Shell UI releases mouse input to Hub")
	_expect(session.find_child("HubRuntime", true, false) != null, "HubRuntime is mounted")
	_expect(session.find_child("PlayerMarker", true, false) != null, "Playable player marker is mounted")
	_expect(session.find_child("WorldSceneLayer", true, false) != null, "World scene layer is separate from interaction markers")
	_expect(session.find_child("WorldInteractionLayer", true, false) != null, "World interaction layer is separate from scene art")
	_expect(session.find_child("HelmInteractPoint", true, false) != null, "Hub has a spatial helm interaction point")
	var hub := session.find_child("HubRuntime", true, false)
	hub.call("DebugClearDurableProgress")
	await process_frame
	var initial_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	var initial_steps := initial_onboarding.get("steps", {}) as Dictionary
	_expect(str(initial_steps.get("find_hub_hud", "")) == "Completed", "Onboarding completes Hub HUD visibility in runtime")
	_expect(str(initial_onboarding.get("next_hint_step", "")) == "open_chart", "Onboarding next hint starts at opening Chart")
	_expect(int(initial_onboarding.get("hint_mouse_filter", -1)) == Control.MOUSE_FILTER_IGNORE, "Runtime onboarding hint ignores mouse input")
	_expect(_label_text(session, "Header") == "云织号空艇中枢", "Hub header uses Chinese text")
	_expect(_label_text(session, "CargoValue").contains("受困货物 0"), "Cargo status reports trapped goods in Chinese")
	_expect(_label_text(session, "HullValue").contains("可出航"), "Hull status uses Chinese text")
	_expect(_button_text(session, "ChartButton").contains("航图 / HUD"), "HUD and Chart entry is visible")
	_expect(_button_text(session, "SaveButton").contains("保存"), "Save entry is visible")
	_expect(_button_text(session, "LoadButton").contains("加载"), "Load entry is visible")
	_expect(_button_text(session, "DeleteProgressButton").contains("删除"), "Delete local progress entry is visible")
	_expect(_button_disabled(session, "DeleteProgressButton"), "Delete local progress starts disabled with no save")

	_expect(hub.call("DebugNodeVisible", "HubDeckFloor"), "Hub has an authored greybox deck floor")
	_expect(hub.call("DebugNodeVisible", "HubIslandWalkBoundary"), "Hub has a walkable island boundary")
	_expect(hub.call("DebugNodeVisible", "HubShipHull"), "Hub has a ship hull spatial anchor")
	_expect(hub.call("DebugNodeVisible", "HubBoardingRamp"), "Hub has a boarding ramp spatial anchor")
	_expect(hub.call("DebugNodeVisible", "HubCabinRoom"), "Hub has a cockpit room volume")
	_expect(hub.call("DebugNodeVisible", "HubCargoRoom"), "Hub has a cargo room volume")
	_expect(hub.call("DebugNodeVisible", "HubEngineRoom"), "Hub has an engine room volume")
	_expect(hub.call("DebugNodeVisible", "HubCabinWindow"), "Hub cockpit has an interior window detail")
	_expect(hub.call("DebugNodeVisible", "HubCabinNavigationSlate"), "Hub cockpit has an interior navigation slate")
	_expect(hub.call("DebugNodeVisible", "HubCargoShelfLeft"), "Hub cargo room has shelf detail")
	_expect(hub.call("DebugNodeVisible", "HubCargoLoadTrack"), "Hub cargo room has a load track")
	_expect(not hub.call("DebugNodeVisible", "HubCargoLoadFill"), "Hub cargo load fill starts hidden while empty")
	_expect(hub.call("DebugNodeVisible", "HubEngineCoilLeft"), "Hub engine room has coil detail")
	_expect(not hub.call("DebugNodeVisible", "HubEngineWearOverlay"), "Hub engine damage overlay starts hidden at full hull")
	_expect(hub.call("DebugNodeVisible", "HubInteriorMainAisle"), "Hub rooms share an interior aisle")
	_expect(_label_text(session, "HubCabinStatusLabel").contains("待规划"), "Hub cockpit status starts in planning state")
	_expect(_label_text(session, "HubCargoStatusLabel").contains("空载"), "Hub cargo status starts empty")
	_expect(_label_text(session, "HubEngineStatusLabel").contains("稳定"), "Hub engine status starts stable")
	_expect(hub.call("DebugNodeVisible", "HelmConsoleProp"), "Hub has an authored greybox helm console prop")
	_expect(hub.call("DebugNodeVisible", "StorageCrateProp"), "Hub has an authored greybox storage crate prop")
	_expect(not hub.call("DebugNodeVisible", "ExplorationSkyField"), "Exploration greybox field is hidden while in Hub")
	var hub_walk_bounds := hub.call("DebugWalkBoundsSize") as Vector2
	_expect(hub_walk_bounds.x > 900.0 and hub_walk_bounds.y > 200.0, "Hub exposes a meaningful walkable bounds size")
	hub.call("DebugSetPlayerPosition", Vector2(20, 20))
	await process_frame
	var clamped_hub_position := hub.call("DebugPlayerPosition") as Vector2
	_expect(bool(hub.call("DebugPlayerWithinWalkBounds")), "Hub clamps debug player position inside walkable bounds")
	_expect(clamped_hub_position.x >= 132.0 and clamped_hub_position.y >= 380.0, "Hub walkable boundary prevents leaving the island/ship space")
	hub.call("DebugSetPlayerPosition", Vector2(158, 610))
	await process_frame
	var start_position := hub.call("DebugPlayerPosition") as Vector2
	Input.action_press(&"move_right")
	await process_frame
	await process_frame
	Input.action_release(&"move_right")
	await process_frame
	var moved_position := hub.call("DebugPlayerPosition") as Vector2
	_expect(moved_position.x > start_position.x, "Player marker moves with project input actions")

	var save_status_before_move_down := _label_text(session, "SaveStatusLabel")
	Input.action_press(&"move_down")
	await process_frame
	await process_frame
	Input.action_release(&"move_down")
	await process_frame
	_expect(_label_text(session, "SaveStatusLabel") == save_status_before_move_down, "Move-down input does not trigger Save shortcut")

	hub.call("OnSavePressed")
	await process_frame
	_expect(_label_text(session, "SaveStatusLabel").contains("保存完成"), "Save action gives visible success feedback")
	_expect(not _button_disabled(session, "DeleteProgressButton"), "Delete local progress enables after save")

	hub.call("OnLoadPressed")
	await process_frame
	_expect(_label_text(session, "SaveStatusLabel").contains("加载完成"), "Load action gives visible success feedback")

	hub.call("DebugSetPlayerPosition", Vector2(362, 613))
	await process_frame
	_expect(hub.call("DebugInteractionPrompt").contains("使用舵台"), "Moving near the helm reveals a spatial interaction prompt")
	hub.call("TrySpatialInteraction")
	await process_frame
	_expect(_is_panel_visible(session, "ChartPanel"), "Chart panel is visible after spatial helm interaction")
	var chart_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	_expect(str(chart_onboarding.get("next_hint_step", "")) == "select_route", "Onboarding advances to route-selection hint after Chart opens")
	_expect(_label_text(session, "ChartTitleLabel") == "HUD / 航图界面", "Chart panel identifies the UI/HUD surface")
	_expect(_button_disabled(session, "ChartButton"), "Chart entry is disabled while Chart panel is open")
	_expect(_button_disabled(session, "SaveButton"), "Save entry is disabled while Chart panel is open")
	_expect(_button_disabled(session, "LoadButton"), "Load entry is disabled while Chart panel is open")
	_expect(_button_disabled(session, "DeleteProgressButton"), "Delete entry is disabled while Chart panel is open")
	_expect(_button_focus_mode(session, "ChartButton") == Control.FOCUS_NONE, "Chart entry leaves focus chain while Chart panel is open")
	_expect(_button_focus_mode(session, "SaveButton") == Control.FOCUS_NONE, "Save entry leaves focus chain while Chart panel is open")
	_expect(_button_focus_mode(session, "LoadButton") == Control.FOCUS_NONE, "Load entry leaves focus chain while Chart panel is open")
	_expect(_button_focus_mode(session, "DeleteProgressButton") == Control.FOCUS_NONE, "Delete entry leaves focus chain while Chart panel is open")

	var chart_open_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(str(chart_open_snapshot.get("chart_state", "")) == "Browsing", "C# HubRuntime opens ChartManager into Browsing")
	_expect(str(chart_open_snapshot.get("content_version", "")) == "polish-003-authored-route-search-v1", "C# HubRuntime loads authored route/search content version")
	_expect(str(chart_open_snapshot.get("content_status", "")) == "polish_authored", "C# HubRuntime reports authored route/search content status")
	_expect(int(chart_open_snapshot.get("visible_route_count", 0)) >= 2, "C# HubRuntime exposes visible ChartManager routes")

	hub.call("OnRouteMistPressed")
	await process_frame
	var selected_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	_expect(str(selected_onboarding.get("next_hint_step", "")) == "depart_route", "Onboarding advances to departure hint after route selection")
	var route_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(str(route_snapshot.get("selected_route", "")) == "route.mist", "C# HubRuntime route selection is backed by ChartManager state")
	_expect(str(route_snapshot.get("chart_state", "")) == "RouteSelected", "ChartManager enters RouteSelected after route choice")
	hub.call("OnDepartPressed")
	await process_frame
	var departed_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	_expect(str(departed_onboarding.get("next_hint_step", "")) == "advance_pressure", "Onboarding advances to pressure hint after departure")
	var departure_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(str(departure_snapshot.get("committed_route", "")) == "route.mist", "C# HubRuntime departure commits through ChartManager")
	_expect(str(departure_snapshot.get("hub_last_route", "")) == "route.mist", "HubManager records the chart departure route")
	_expect(str(departure_snapshot.get("hub_docking_state", "")) == "InTransit", "HubManager enters InTransit after departure")
	_expect(str(departure_snapshot.get("navigation_state", "")) == "Arrived", "NavigationManager produces a completed route contract")
	_expect(str(departure_snapshot.get("encounter_destination", "")) == "location.mist-short", "NavigationManager produces EncounterContext destination")
	_expect(str(departure_snapshot.get("exploration_phase", "")) == "Exploring", "ExplorationManager consumes EncounterContext before Exploration HUD opens")
	_expect(not _is_panel_visible(session, "ChartPanel"), "Chart panel closes after departure")
	_expect(_is_panel_visible(session, "ExplorationPanel"), "Exploration HUD surface is visible after departure")
	_expect(session.find_child("SearchInteractPoint", true, false) != null, "Exploration has a spatial search interaction point")
	_expect(session.find_child("ReturnInteractPoint", true, false) != null, "Exploration has a spatial return interaction point")
	_expect(hub.call("DebugNodeVisible", "ExplorationIslandWalkBoundary"), "Exploration has a walkable island boundary")
	_expect(hub.call("DebugNodeVisible", "ExplorationDockedShip"), "Exploration has a docked ship spatial anchor")
	_expect(hub.call("DebugNodeVisible", "ExplorationBoardingRamp"), "Exploration has a boarding ramp spatial anchor")
	_expect(hub.call("DebugNodeVisible", "ExplorationIslandPath"), "Exploration has a walkable island path")
	_expect(hub.call("DebugNodeVisible", "ExplorationSkyField"), "Exploration has an authored greybox sky field")
	_expect(hub.call("DebugNodeVisible", "SearchWreckProp"), "Exploration has an authored greybox search wreck prop")
	_expect(hub.call("DebugNodeVisible", "ReturnBeaconProp"), "Exploration has an authored greybox return beacon prop")
	_expect(_label_text(session, "ExplorationPointSemanticLabel").contains("待接近"), "Exploration scene starts with dynamic search-point semantic label")
	_expect(_label_text(session, "ExplorationExtractionSemanticLabel").contains("携带 0/500"), "Exploration scene starts with dynamic extraction status")
	_expect(not hub.call("DebugNodeVisible", "ExplorationThreatZone"), "Exploration threat zone is hidden before pressure")
	_expect(not hub.call("DebugNodeVisible", "HubDeckFloor"), "Hub greybox floor is hidden while in Exploration")
	_expect(_label_text(session, "ExplorationTitleLabel") == "探索 HUD", "Exploration surface has a clear title")
	_expect(_label_text(session, "ExplorationRouteLabel").contains("雾海短程"), "Exploration surface shows selected route")
	_expect(_label_text(session, "ExplorationResourceLabel").contains("资源压力"), "Exploration surface shows resource pressure feedback")
	_expect(_label_text(session, "ExplorationThreatLabel").contains("威胁反馈"), "Exploration surface shows threat feedback")
	_expect(_label_text(session, "ExplorationHullLabel").contains("船体状态"), "Exploration surface shows hull feedback")
	_expect(_label_text(session, "ExplorationRecoveryLabel").contains("恢复提示"), "Exploration surface shows recovery feedback")
	var exploration_walk_bounds := hub.call("DebugWalkBoundsSize") as Vector2
	_expect(exploration_walk_bounds.x > 900.0 and exploration_walk_bounds.y > 200.0, "Exploration exposes a meaningful walkable bounds size")

	hub.call("DebugSetPlayerPosition", Vector2(638, 613))
	await process_frame
	_expect(hub.call("DebugInteractionPrompt").contains("搜索事件点"), "Moving near the search point reveals a spatial search prompt")
	hub.call("TrySpatialInteraction")
	await process_frame
	var pressure_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	var pressure_next_hint := str(pressure_onboarding.get("next_hint_step", ""))
	_expect(pressure_next_hint == "notice_save_load", "Onboarding advances to save/load awareness after pressure feedback (%s)" % pressure_next_hint)
	var search_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(int(search_snapshot.get("exploration_step", 0)) == 1, "C# HubRuntime search advances domain adapter snapshot")
	_expect(str(search_snapshot.get("last_search_point", "")) == "sp.playable.1", "ExplorationManager records runtime search point")
	_expect(str(search_snapshot.get("last_search_point_name", "")) == "雾灯残骸", "Exploration runtime exposes authored search point name")
	_expect(int(search_snapshot.get("basic_supply_in_storage", 0)) == 9, "C# HubRuntime search consumes ResourcesManager supply")
	_expect(int(search_snapshot.get("reward_carried", 0)) == 1, "C# HubRuntime search adds carried ResourcesManager reward")
	_expect(_label_text(session, "ExplorationResourceLabel").contains("搜索消耗 1"), "Spatial search interaction creates resource pressure")
	_expect(_label_text(session, "ExplorationThreatLabel").contains("低威胁"), "Spatial search interaction creates low threat feedback")
	_expect(_label_text(session, "ExplorationPointSemanticLabel").contains("雾灯残骸"), "Exploration semantic label follows authored search point name")
	_expect(_control_width(session, "ExplorationRouteProgressFill") > 250.0, "Exploration route progress strip advances after first search")

	hub.call("OnExplorationAdvancePressed")
	await process_frame
	var damage_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(int(damage_snapshot.get("basic_supply_in_storage", 0)) == 8, "C# HubRuntime second advance consumes another ResourcesManager supply")
	_expect(int(damage_snapshot.get("reward_carried", 0)) == 2, "C# HubRuntime second advance keeps rewards in carried pool before return")
	_expect(int(damage_snapshot.get("hull_integrity", 0)) == 94, "C# HubRuntime second advance applies ModuleHullManager damage")
	_expect(str(damage_snapshot.get("exploration_substate", "")) == "Threatened", "ExplorationManager owns the threat substate after pressure")
	_expect(_label_text(session, "ExplorationResourceLabel").contains("载货 180/500"), "Exploration second advance changes carried cargo")
	_expect(_label_text(session, "ExplorationThreatLabel").contains("中威胁"), "Exploration second advance escalates threat feedback")
	_expect(_label_text(session, "ExplorationHullLabel").contains("94/100"), "Exploration second advance changes hull feedback")
	_expect(hub.call("DebugNodeVisible", "ExplorationThreatZone"), "Exploration semantic threat zone appears after pressure")
	_expect(_label_text(session, "ExplorationThreatSemanticLabel").contains("中威胁"), "Exploration threat semantic label follows manager threat text")

	hub.call("OnSavePressed")
	await process_frame
	_expect(_label_text(session, "SaveStatusLabel").contains("覆盖确认"), "Exploration save asks before overwriting local progress")
	hub.call("OnSavePressed")
	await process_frame
	var saved_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	_expect(str(saved_onboarding.get("next_hint_step", "")) == "return_hub", "Onboarding advances to return-Hub hint after save/load awareness")
	var saved_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(_label_text(session, "SaveStatusLabel").contains("本地进度"), "Exploration state saves through canonical Persistence and local durable progress")
	_expect(int(saved_snapshot.get("persistence_generation", 0)) > 0, "Canonical Persistence records progress generation")

	hub.call("DebugSetPlayerPosition", Vector2(1058, 613))
	await process_frame
	_expect(hub.call("DebugInteractionPrompt").contains("返回 Hub"), "Moving near the return point reveals a spatial return prompt")
	hub.call("TrySpatialInteraction")
	await process_frame
	var completed_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	_expect(bool(completed_onboarding.get("first_loop_complete", false)), "Onboarding first loop completes after Hub return and summary change")
	var return_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(str(return_snapshot.get("hub_docking_state", "")) == "Landed", "HubManager returns to Landed after spatial Hub return")
	_expect(int(return_snapshot.get("reward_carried", 0)) == 0, "ResourcesManager clears carried rewards after spatial Hub return")
	_expect(int(return_snapshot.get("reward_in_storage", 0)) == 2, "ResourcesManager extracts rewards to storage after spatial Hub return")
	_expect(not _is_panel_visible(session, "ExplorationPanel"), "Exploration panel closes on spatial Hub return")
	_expect(hub.call("DebugNodeVisible", "HubDeckFloor"), "Hub greybox floor returns after Exploration")
	_expect(not hub.call("DebugNodeVisible", "ExplorationSkyField"), "Exploration greybox field hides after Hub return")
	_expect(not _button_disabled(session, "ChartButton"), "Hub Chart entry is enabled after Exploration return")
	_expect(_label_text(session, "CargoValue").contains("已用 180"), "Hub cargo summary syncs exploration cargo")
	_expect(_label_text(session, "HullValue").contains("完整度 94"), "Hub hull summary syncs exploration damage")
	_expect(_label_text(session, "StorageValue").contains("信标水晶 x2"), "Hub storage summary syncs exploration rewards")
	_expect(_label_text(session, "ChartStation").contains("中威胁"), "Hub chart station syncs route pressure")
	_expect(_label_text(session, "HubCabinStatusLabel").contains("雾海短程 2/3"), "Hub cockpit interior status syncs route progress")
	_expect(_label_text(session, "HubCargoStatusLabel").contains("信标水晶 x2"), "Hub cargo interior status syncs returned rewards")
	_expect(_control_width(session, "HubCargoLoadFill") > 20.0, "Hub cargo load fill grows after returned cargo")
	_expect(hub.call("DebugNodeVisible", "HubEngineWearOverlay"), "Hub engine damage overlay appears after hull pressure")
	_expect(_label_text(session, "HubEngineStatusLabel").contains("94/100"), "Hub engine interior status syncs hull pressure")

	hub.call("OnLoadPressed")
	await process_frame
	_expect(_is_panel_visible(session, "ExplorationPanel"), "Loading exploration save restores Exploration HUD")
	var loaded_onboarding := hub.call("DebugOnboardingSnapshot") as Dictionary
	_expect(str(loaded_onboarding.get("next_hint_step", "")) == "return_hub", "Loading mid-loop onboarding progress does not replay completed save/load hint")
	_expect(_label_text(session, "ExplorationThreatLabel").contains("中威胁"), "Loading exploration save restores pressure step")
	_expect(_label_text(session, "ExplorationRouteLabel").contains("雾海短程"), "Loading exploration save restores authored route display name")
	var loaded_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(int(loaded_snapshot.get("reward_carried", 0)) == 2, "Canonical load restores carried ResourcesManager rewards")
	_expect(int(loaded_snapshot.get("reward_in_storage", 0)) == 0, "Canonical load restores pre-return ResourcesManager storage state")
	_expect(str(loaded_snapshot.get("exploration_phase", "")) == "Exploring", "Canonical load restores ExplorationManager active session")
	_expect(str(loaded_snapshot.get("last_load_status", "")).contains("canonical progress loaded"), "Canonical load status is exposed in domain snapshot")

	hub.call("OnExplorationAdvancePressed")
	await process_frame
	_expect(_label_text(session, "ExplorationResourceLabel").contains("载货 260/500"), "Exploration third advance locks in rewards")
	_expect(_label_text(session, "ExplorationRecoveryLabel").contains("一轮压力循环完成"), "Exploration third advance completes pressure loop")
	_expect(_label_text(session, "ExplorationExtractionSemanticLabel").contains("收益锁定 260/500"), "Exploration extraction semantic label switches to settlement-ready state")
	_expect(_label_text(session, "SearchInteractPointLabel").contains("已搜索"), "Exploration search marker reflects completed search semantics")
	await _save_runtime_screenshot(root, EXPLORATION_SEMANTICS_SCREENSHOT_PATH, "Exploration semantics screenshot")

	hub.call("OnExplorationReturnPressed")
	await process_frame
	_expect(str((hub.call("DebugOnboardingSnapshot") as Dictionary).get("next_hint_step", "")) == "", "Final Hub screenshot state has no stale return-Hub onboarding hint")
	_expect(_label_text(session, "CargoValue").contains("收益锁定"), "Hub cargo summary syncs completed pressure loop")
	_expect(_label_text(session, "ChartStation").contains("压力循环完成"), "Hub chart station syncs completed pressure loop")
	_expect(_label_text(session, "CargoStation").contains("收益锁定"), "Hub cargo station syncs completed pressure loop")
	_expect(_label_text(session, "HubCargoStatusLabel").contains("收益锁定"), "Hub cargo interior status syncs locked rewards")

	await _save_runtime_screenshot(root, SCREENSHOT_PATH, "Runtime screenshot")

	session.queue_free()
	await process_frame
	_finish()


func _label_text(root_node: Node, node_name: String) -> String:
	var label := root_node.find_child(node_name, true, false) as Label
	return "" if label == null else label.text


func _button_text(root_node: Node, node_name: String) -> String:
	var button := root_node.find_child(node_name, true, false) as Button
	return "" if button == null else button.text


func _button_disabled(root_node: Node, node_name: String) -> bool:
	var button := root_node.find_child(node_name, true, false) as Button
	return false if button == null else button.disabled


func _button_focus_mode(root_node: Node, node_name: String) -> int:
	var button := root_node.find_child(node_name, true, false) as Button
	return -1 if button == null else button.focus_mode


func _control_width(root_node: Node, node_name: String) -> float:
	var control := root_node.find_child(node_name, true, false) as Control
	return 0.0 if control == null else control.size.x


func _control_mouse_filter(root_node: Node, node_name: String) -> int:
	var control := root_node.find_child(node_name, true, false) as Control
	return -1 if control == null else control.mouse_filter


func _is_panel_visible(root_node: Node, node_name: String) -> bool:
	var panel := root_node.find_child(node_name, true, false) as CanvasItem
	return panel != null and panel.visible


func _save_runtime_screenshot(viewport: Window, path: String, label: String) -> void:
	if DisplayServer.get_name() == "headless":
		print("SKIP %s unavailable with current display driver" % label)
		return
	await RenderingServer.frame_post_draw
	var texture := viewport.get_texture()
	if texture == null:
		print("SKIP %s unavailable with current display driver" % label)
		return
	var image := texture.get_image()
	var saved := image.save_png(path)
	_expect(saved == OK, "%s saved to %s" % [label, ProjectSettings.globalize_path(path)])


func _expect(condition: bool, label: String) -> void:
	if condition:
		print("PASS ", label)
		return

	_failed = true
	push_error("FAIL " + label)


func _finish() -> void:
	quit(1 if _failed else 0)
