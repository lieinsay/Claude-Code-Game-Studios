extends Node2D

const SAVE_PATH := "user://smoke_session_state.json"

var _chart_panel: Control
var _exploration_panel: Control
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


func _ready() -> void:
	_cache_nodes()
	_wire_buttons()
	_show_hub()


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
		elif key.keycode == KEY_S:
			_on_save_pressed()
			get_viewport().set_input_as_handled()
		elif key.keycode == KEY_L:
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
	elif key.keycode == KEY_S:
		_on_save_pressed()
		get_viewport().set_input_as_handled()
	elif key.keycode == KEY_L:
		_on_load_pressed()
		get_viewport().set_input_as_handled()


func _cache_nodes() -> void:
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
	_show_exploration_surface()


func _on_exploration_advance_pressed() -> void:
	if _current_screen != "exploration":
		return

	_exploration_step = min(_exploration_step + 1, 3)
	_set_exploration_status()
	if _exploration_step >= 3:
		_set_footer("一轮探索压力循环完成：返回 Hub 后可继续复测闭环。S 保存，L 加载。")
		_grab_button("ExplorationReturnButton")
	else:
		_set_footer("探索推进：压力、威胁、船体反馈已更新。S 保存，L 加载，Esc 返回 Hub。")
		_grab_button("ExplorationAdvanceButton")


func _on_save_pressed() -> void:
	var snapshot := {
		"screen": _current_screen,
		"route": _selected_route,
		"exploration_step": _exploration_step,
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
	_set_footer("HUD 入口：点击“打开航图 / HUD”或按 M。保存/加载入口在右侧。")
	_grab_button("ChartButton")


func _show_chart_panel() -> void:
	if _chart_panel != null:
		_chart_panel.visible = true
	if _exploration_panel != null:
		_exploration_panel.visible = false
	_set_hub_controls_enabled(false)
	_set_footer("HUD / 航图已接管输入：方向键仅在航图内移动，Esc 返回 Hub。")
	_grab_button("RouteMistButton")


func _show_exploration_surface() -> void:
	if _chart_panel != null:
		_chart_panel.visible = false
	if _exploration_panel != null:
		_exploration_panel.visible = true
	_set_hub_controls_enabled(false)
	_set_exploration_status()
	_set_footer("探索 HUD 已接管输入：点击“推进探索 / 搜索”产生压力变化。S 保存，L 加载，Esc 返回 Hub。")
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
