extends Node2D

const SAVE_PATH := "user://smoke_session_state.json"
const HUB_PLAYER_START := Vector2(158, 610)
const EXPLORATION_PLAYER_START := Vector2(168, 610)
const PLAYER_SPEED := 260.0
const INTERACTION_RADIUS := 74.0

var _chart_panel: Control
var _exploration_panel: Control
var _hub_root: Control
var _playable_layer: Control
var _player_marker: ColorRect
var _interaction_prompt_label: Label
var _hub_helm_marker: ColorRect
var _hub_storage_marker: ColorRect
var _exploration_search_marker: ColorRect
var _exploration_return_marker: ColorRect
var _chart_status_label: Label
var _exploration_route_label: Label
var _exploration_resource_label: Label
var _exploration_threat_label: Label
var _exploration_hull_label: Label
var _exploration_recovery_label: Label
var _storage_value_label: Label
var _cargo_value_label: Label
var _hull_value_label: Label
var _chart_station_label: Label
var _cargo_station_label: Label
var _save_status_label: Label
var _footer_label: Label
var _hub_action_buttons: Array[Button] = []
var _selected_route := ""
var _current_screen := "hub"
var _exploration_step := 0
var _player_position := HUB_PLAYER_START
var _nearest_interaction := ""


func _ready() -> void:
	_cache_nodes()
	_create_playable_layer()
	_wire_buttons()
	_show_hub()


func _process(delta: float) -> void:
	if _player_marker == null:
		return

	if _current_screen == "chart":
		return

	var direction := Input.get_vector(&"move_left", &"move_right", &"move_up", &"move_down")
	if direction != Vector2.ZERO:
		_player_position += direction.normalized() * PLAYER_SPEED * delta
		_player_position = _player_position.clamp(Vector2(76, 150), Vector2(1204, 650))
		_player_marker.position = _player_position - (_player_marker.size * 0.5)

	_update_spatial_interaction()


func _unhandled_input(event: InputEvent) -> void:
	if not event is InputEventKey:
		return

	var key := event as InputEventKey
	if not key.pressed or key.echo:
		return

	if _is_visible(_exploration_panel):
		if key.keycode == KEY_ESCAPE:
			_show_hub()
			get_viewport().set_input_as_handled()
		elif key.keycode == KEY_E:
			_try_spatial_interaction()
			get_viewport().set_input_as_handled()
		elif _is_save_shortcut(key):
			_on_save_pressed()
			get_viewport().set_input_as_handled()
		elif _is_load_shortcut(key):
			_on_load_pressed()
			get_viewport().set_input_as_handled()
		return

	if _is_visible(_chart_panel):
		if key.keycode == KEY_ESCAPE:
			_show_hub()
			get_viewport().set_input_as_handled()
		return

	if key.keycode == KEY_M:
		_on_chart_pressed()
		get_viewport().set_input_as_handled()
	elif key.keycode == KEY_E:
		_try_spatial_interaction()
		get_viewport().set_input_as_handled()
	elif _is_save_shortcut(key):
		_on_save_pressed()
		get_viewport().set_input_as_handled()
	elif _is_load_shortcut(key):
		_on_load_pressed()
		get_viewport().set_input_as_handled()


func _cache_nodes() -> void:
	_hub_root = _find_control("HubRoot")
	_chart_panel = _find_control("ChartPanel")
	_exploration_panel = _find_control("ExplorationPanel")
	_chart_status_label = find_child("ChartStatusLabel", true, false) as Label
	_exploration_route_label = find_child("ExplorationRouteLabel", true, false) as Label
	_exploration_resource_label = find_child("ExplorationResourceLabel", true, false) as Label
	_exploration_threat_label = find_child("ExplorationThreatLabel", true, false) as Label
	_exploration_hull_label = find_child("ExplorationHullLabel", true, false) as Label
	_exploration_recovery_label = find_child("ExplorationRecoveryLabel", true, false) as Label
	_storage_value_label = find_child("StorageValue", true, false) as Label
	_cargo_value_label = find_child("CargoValue", true, false) as Label
	_hull_value_label = find_child("HullValue", true, false) as Label
	_chart_station_label = find_child("ChartStation", true, false) as Label
	_cargo_station_label = find_child("CargoStation", true, false) as Label
	_save_status_label = find_child("SaveStatusLabel", true, false) as Label
	_footer_label = find_child("Footer", true, false) as Label
	_hub_action_buttons = [
		find_child("ChartButton", true, false) as Button,
		find_child("SaveButton", true, false) as Button,
		find_child("LoadButton", true, false) as Button,
	]


func _create_playable_layer() -> void:
	if _hub_root == null:
		return

	_playable_layer = Control.new()
	_playable_layer.name = "PlayableVerticalSliceLayer"
	_playable_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_playable_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_playable_layer.z_index = 4
	_hub_root.add_child(_playable_layer)

	_hub_helm_marker = _add_world_marker("HelmInteractPoint", Vector2(316, 594), Color(0.22, 0.58, 0.72, 1), "舵台 E")
	_hub_storage_marker = _add_world_marker("StorageInteractPoint", Vector2(536, 594), Color(0.58, 0.45, 0.26, 1), "仓储 E")
	_exploration_search_marker = _add_world_marker("SearchInteractPoint", Vector2(592, 594), Color(0.46, 0.67, 0.33, 1), "搜索 E")
	_exploration_return_marker = _add_world_marker("ReturnInteractPoint", Vector2(1012, 594), Color(0.64, 0.39, 0.28, 1), "返航 E")

	_player_marker = ColorRect.new()
	_player_marker.name = "PlayerMarker"
	_player_marker.color = Color(0.91, 0.84, 0.45, 1)
	_player_marker.custom_minimum_size = Vector2(28, 28)
	_player_marker.size = Vector2(28, 28)
	_player_marker.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_playable_layer.add_child(_player_marker)

	_interaction_prompt_label = Label.new()
	_interaction_prompt_label.name = "SpatialInteractionPrompt"
	_interaction_prompt_label.position = Vector2(76, 112)
	_interaction_prompt_label.size = Vector2(1120, 28)
	_interaction_prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_interaction_prompt_label.add_theme_font_size_override("font_size", 18)
	_interaction_prompt_label.text = "WASD / 方向键移动，靠近可交互点按 E。"
	_playable_layer.add_child(_interaction_prompt_label)


func _add_world_marker(node_name: String, marker_position: Vector2, marker_color: Color, label_text: String) -> ColorRect:
	var marker := ColorRect.new()
	marker.name = node_name
	marker.color = marker_color
	marker.position = marker_position
	marker.custom_minimum_size = Vector2(92, 38)
	marker.size = Vector2(92, 38)
	marker.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_playable_layer.add_child(marker)

	var label := Label.new()
	label.name = "%sLabel" % node_name
	label.set_anchors_preset(Control.PRESET_FULL_RECT)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 16)
	label.text = label_text
	marker.add_child(label)
	return marker


func _wire_buttons() -> void:
	_wire_button("ChartButton", _on_chart_pressed)
	_wire_button("SaveButton", _on_save_pressed)
	_wire_button("LoadButton", _on_load_pressed)
	_wire_button("RouteMistButton", _on_route_mist_pressed)
	_wire_button("RouteMarketButton", _on_route_market_pressed)
	_wire_button("DepartButton", _on_depart_pressed)
	_wire_button("ChartCloseButton", _show_hub)
	_wire_button("ExplorationAdvanceButton", _on_exploration_advance_pressed)
	_wire_button("ExplorationReturnButton", _show_hub)


func _wire_button(node_name: String, callback: Callable) -> void:
	var button := find_child(node_name, true, false) as Button
	if button != null and not button.pressed.is_connected(callback):
		button.pressed.connect(callback)
		if not button.mouse_entered.is_connected(_on_button_mouse_entered.bind(button)):
			button.mouse_entered.connect(_on_button_mouse_entered.bind(button))


func _on_chart_pressed() -> void:
	_current_screen = "chart"
	_show_chart_panel()
	_set_chart_status("HUD / 航图已打开：选择一条 MVP 航线后确认出发。")


func _on_route_mist_pressed() -> void:
	_selected_route = "route.mist"
	_set_chart_status("已选择航线：雾海短程。按“确认出发”进入探索。")
	_grab_button("DepartButton")


func _on_route_market_pressed() -> void:
	_selected_route = "route.market"
	_set_chart_status("已选择航线：旧集市航道。按“确认出发”进入探索。")
	_grab_button("DepartButton")


func _on_depart_pressed() -> void:
	if _selected_route.is_empty():
		_set_chart_status("请选择航线后再确认出发。")
		_grab_button("RouteMistButton")
		return

	_current_screen = "exploration"
	_exploration_step = 0
	_player_position = EXPLORATION_PLAYER_START
	_show_exploration_surface()


func _on_exploration_advance_pressed() -> void:
	if _current_screen != "exploration":
		return

	_exploration_step = min(_exploration_step + 1, 3)
	_set_exploration_status()
	if _exploration_step >= 3:
		_set_footer("一轮探索压力循环完成：返回 Hub 后可继续复测闭环。Ctrl+S 保存，Ctrl+L 加载。")
		_grab_button("ExplorationReturnButton")
	else:
		_set_footer("探索推进：压力、威胁、船体反馈已更新。Ctrl+S 保存，Ctrl+L 加载，Esc 返回 Hub。")
		_grab_button("ExplorationAdvanceButton")


func _on_save_pressed() -> void:
	var snapshot := {
		"screen": _current_screen,
		"route": _selected_route,
		"exploration_step": _exploration_step,
		"player_x": _player_position.x,
		"player_y": _player_position.y,
		"footer": _label_text("Footer"),
	}
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file == null:
		_set_save_status("保存失败：无法打开 user:// 存档。")
		return

	file.store_string(JSON.stringify(snapshot))
	_set_save_status("保存完成：user://smoke_session_state.json")


func _on_load_pressed() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		_set_save_status("加载失败：未找到可继续的本地存档。")
		return

	var file := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if file == null:
		_set_save_status("加载失败：无法读取 user:// 存档。")
		return

	var parsed = JSON.parse_string(file.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		_set_save_status("加载失败：存档格式无效。")
		return

	_current_screen = str(parsed.get("screen", "hub"))
	_selected_route = str(parsed.get("route", ""))
	_exploration_step = max(0, int(parsed.get("exploration_step", 0)))
	_player_position = Vector2(
		float(parsed.get("player_x", HUB_PLAYER_START.x)),
		float(parsed.get("player_y", HUB_PLAYER_START.y))
	)
	_set_save_status("加载完成：已恢复 %s" % _current_screen)

	if _current_screen == "chart":
		_show_chart_panel()
		_set_chart_status("已从存档恢复航线：%s" % _selected_route)
	elif _current_screen == "exploration":
		_show_exploration_surface()
		_set_save_status("加载完成：已恢复探索 HUD")
	else:
		_show_hub()


func _show_hub() -> void:
	_current_screen = "hub"
	if _chart_panel != null:
		_chart_panel.visible = false
	if _exploration_panel != null:
		_exploration_panel.visible = false
	_set_hub_controls_enabled(true)
	_update_hub_summary()
	_set_world_mode("hub")
	_set_footer("HUD 入口：点击“打开航图 / HUD”或按 M。保存/加载可用按钮或 Ctrl+S / Ctrl+L。")
	_grab_button("ChartButton")


func _show_chart_panel() -> void:
	if _chart_panel != null:
		_chart_panel.visible = true
	if _exploration_panel != null:
		_exploration_panel.visible = false
	_set_hub_controls_enabled(false)
	_set_world_mode("chart")
	_set_footer("HUD / 航图已接管输入：方向键仅在航图内移动，Esc 返回 Hub。")
	_grab_button("RouteMistButton")


func _show_exploration_surface() -> void:
	if _chart_panel != null:
		_chart_panel.visible = false
	if _exploration_panel != null:
		_exploration_panel.visible = true
	_set_hub_controls_enabled(false)
	_set_world_mode("exploration")
	_set_exploration_status()
	_set_footer("探索 HUD 已接管输入：点击“推进探索 / 搜索”产生压力变化。Ctrl+S 保存，Ctrl+L 加载，Esc 返回 Hub。")
	_grab_button("ExplorationAdvanceButton")


func _set_exploration_status() -> void:
	if _exploration_route_label != null:
		_exploration_route_label.text = "路线：%s；探索进度 %d/3" % [_route_name(), _exploration_step]

	if _exploration_step <= 0:
		_set_exploration_labels(
			"资源压力：补给稳定；载货 0/500",
			"威胁反馈：暂无遭遇；侦察 100%",
			"船体状态：100/100 完整，可继续探索",
			"恢复提示：点击“推进探索 / 搜索”开始压力循环"
		)
	elif _exploration_step == 1:
		_set_exploration_labels(
			"资源压力：搜索消耗 1 补给；发现云晶 1 箱，载货 80/500",
			"威胁反馈：低威胁；云影扰动已标记",
			"船体状态：100/100 完整，暂无损伤",
			"恢复提示：可继续搜索，或返回 Hub 保留收益"
		)
	elif _exploration_step == 2:
		_set_exploration_labels(
			"资源压力：补给继续消耗；载货 180/500",
			"威胁反馈：中威胁；遭遇警报，侦察 72%",
			"船体状态：94/100 轻微擦伤",
			"恢复提示：建议返回 Hub 检查船体，或继续承担风险"
		)
	else:
		_set_exploration_labels(
			"资源压力：载货 260/500；收益已锁定",
			"威胁反馈：威胁已解除；可安全撤离",
			"船体状态：94/100，可返航",
			"恢复提示：一轮压力循环完成，点击返回 Hub 闭环"
		)


func _set_exploration_labels(resource_text: String, threat_text: String, hull_text: String, recovery_text: String) -> void:
	if _exploration_resource_label != null:
		_exploration_resource_label.text = resource_text
	if _exploration_threat_label != null:
		_exploration_threat_label.text = threat_text
	if _exploration_hull_label != null:
		_exploration_hull_label.text = hull_text
	if _exploration_recovery_label != null:
		_exploration_recovery_label.text = recovery_text


func _update_hub_summary() -> void:
	if _exploration_step <= 0:
		var chart_idle := "航图：待规划"
		if not _selected_route.is_empty():
			chart_idle = "航图：%s / 待规划" % _route_name()
		_set_hub_labels(
			"基础补给 x10 / 修理包 x4",
			"已用 0 / 有效容量 500 / 受困货物 0",
			"完整度 100 / 承载带稳定 / 可出航",
			chart_idle,
			"货舱：可进入"
		)
	elif _exploration_step == 1:
		_set_hub_labels(
			"基础补给 x9 / 云晶 x1 / 修理包 x4",
			"已用 80 / 有效容量 500 / 受困货物 0",
			"完整度 100 / 承载带稳定 / 可出航",
			"航图：%s 进度 1/3" % _route_name(),
			"货舱：云晶 1 箱待结算"
		)
	elif _exploration_step == 2:
		_set_hub_labels(
			"基础补给 x8 / 云晶 x2 / 修理包 x4",
			"已用 180 / 有效容量 500 / 受困货物 0",
			"完整度 94 / 承载带轻伤 / 可出航",
			"航图：%s 中威胁 2/3" % _route_name(),
			"货舱：载货 180/500"
		)
	else:
		_set_hub_labels(
			"基础补给 x8 / 云晶 x3 / 修理包 x4",
			"已用 260 / 有效容量 500 / 受困货物 0 / 收益锁定",
			"完整度 94 / 承载带轻伤 / 可返航",
			"航图：%s 压力循环完成 3/3" % _route_name(),
			"货舱：收益锁定 260/500"
		)


func _set_hub_labels(storage_text: String, cargo_text: String, hull_text: String, chart_text: String, cargo_station_text: String) -> void:
	if _storage_value_label != null:
		_storage_value_label.text = storage_text
	if _cargo_value_label != null:
		_cargo_value_label.text = cargo_text
	if _hull_value_label != null:
		_hull_value_label.text = hull_text
	if _chart_station_label != null:
		_chart_station_label.text = chart_text
	if _cargo_station_label != null:
		_cargo_station_label.text = cargo_station_text


func _route_name() -> String:
	if _selected_route == "route.mist":
		return "雾海短程"
	if _selected_route == "route.market":
		return "旧集市航道"
	return "未命名航线"


func _set_hub_controls_enabled(enabled: bool) -> void:
	for button in _hub_action_buttons:
		if button != null:
			button.disabled = not enabled
			button.focus_mode = Control.FOCUS_ALL if enabled else Control.FOCUS_NONE


func _set_world_mode(mode: String) -> void:
	if _hub_helm_marker != null:
		_hub_helm_marker.visible = mode == "hub"
	if _hub_storage_marker != null:
		_hub_storage_marker.visible = mode == "hub"
	if _exploration_search_marker != null:
		_exploration_search_marker.visible = mode == "exploration"
	if _exploration_return_marker != null:
		_exploration_return_marker.visible = mode == "exploration"
	if _player_marker != null:
		if mode == "chart":
			_player_marker.visible = false
		else:
			_player_marker.visible = true
			if mode == "hub" and _current_screen == "hub" and _player_position == EXPLORATION_PLAYER_START:
				_player_position = HUB_PLAYER_START
			_player_marker.position = _player_position - (_player_marker.size * 0.5)
	_update_spatial_interaction()


func _update_spatial_interaction() -> void:
	_nearest_interaction = ""
	var prompt := "WASD / 方向键移动，靠近可交互点按 E。"

	if _current_screen == "hub":
		var helm_distance := _distance_to_marker(_hub_helm_marker)
		var storage_distance := _distance_to_marker(_hub_storage_marker)
		if helm_distance <= INTERACTION_RADIUS and helm_distance <= storage_distance:
			_nearest_interaction = "hub_helm"
			prompt = "按 E 使用舵台：打开航图并选择航线。"
		elif storage_distance <= INTERACTION_RADIUS:
			_nearest_interaction = "hub_storage"
			prompt = "按 E 检查仓储：确认资源与货舱状态。"
	elif _current_screen == "exploration":
		var search_distance := _distance_to_marker(_exploration_search_marker)
		var return_distance := _distance_to_marker(_exploration_return_marker)
		if search_distance <= INTERACTION_RADIUS and search_distance <= return_distance:
			_nearest_interaction = "exploration_search"
			prompt = "按 E 搜索事件点：获得资源或压力反馈。"
		elif return_distance <= INTERACTION_RADIUS:
			_nearest_interaction = "exploration_return"
			prompt = "按 E 返回 Hub：结算当前探索反馈。"

	if _interaction_prompt_label != null:
		_interaction_prompt_label.text = prompt


func _distance_to_marker(marker: Control) -> float:
	if marker == null or not marker.visible:
		return INF
	return _player_position.distance_to(marker.position + (marker.size * 0.5))


func _try_spatial_interaction() -> void:
	_update_spatial_interaction()
	if _nearest_interaction == "hub_helm":
		_on_chart_pressed()
	elif _nearest_interaction == "hub_storage":
		_set_footer("仓储已检查：基础补给、云晶和修理包状态已同步。")
		_set_save_status("交互完成：仓储状态可见")
	elif _nearest_interaction == "exploration_search":
		_on_exploration_advance_pressed()
	elif _nearest_interaction == "exploration_return":
		_show_hub()
	else:
		_set_footer("附近没有可交互点：移动到标记旁按 E。")


func _is_save_shortcut(key: InputEventKey) -> bool:
	return key.ctrl_pressed and not key.alt_pressed and not key.meta_pressed and key.keycode == KEY_S


func _is_load_shortcut(key: InputEventKey) -> bool:
	return key.ctrl_pressed and not key.alt_pressed and not key.meta_pressed and key.keycode == KEY_L


func _set_chart_status(text: String) -> void:
	if _chart_status_label != null:
		_chart_status_label.text = text


func _set_save_status(text: String) -> void:
	if _save_status_label != null:
		_save_status_label.text = text


func _set_footer(text: String) -> void:
	if _footer_label != null:
		_footer_label.text = text


func _label_text(node_name: String) -> String:
	var label := find_child(node_name, true, false) as Label
	return "" if label == null else label.text


func _debug_set_player_position(position: Vector2) -> void:
	_player_position = position
	if _player_marker != null:
		_player_marker.position = _player_position - (_player_marker.size * 0.5)
	_update_spatial_interaction()


func _debug_player_position() -> Vector2:
	return _player_position


func _debug_interaction_prompt() -> String:
	return "" if _interaction_prompt_label == null else _interaction_prompt_label.text


func _grab_button(node_name: String) -> void:
	var button := find_child(node_name, true, false) as Button
	if button != null and button.visible and not button.disabled:
		button.grab_focus()


func _find_control(node_name: String) -> Control:
	return find_child(node_name, true, false) as Control


func _is_visible(item: CanvasItem) -> bool:
	return item != null and item.visible


func _on_button_mouse_entered(button: Button) -> void:
	if button.visible and not button.disabled:
		button.grab_focus()
