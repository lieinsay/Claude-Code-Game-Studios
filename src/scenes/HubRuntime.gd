extends Node2D

const SAVE_PATH := "user://smoke_session_state.json"

var _chart_panel: Control
var _chart_status_label: Label
var _save_status_label: Label
var _footer_label: Label
var _hub_action_buttons: Array[Button] = []
var _selected_route := ""
var _current_screen := "hub"


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
	_chart_status_label = find_child("ChartStatusLabel", true, false) as Label
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
	_set_chart_status("已确认出发：探索 HUD 已就绪，可返回 Hub 后继续烟测。")
	_set_footer("探索 HUD：四区模板已加载，返回 Hub 后资源会进入仓储结算。")


func _on_save_pressed() -> void:
	var snapshot := {
		"screen": _current_screen,
		"route": _selected_route,
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
	_set_save_status("加载完成：已恢复 %s" % _current_screen)

	if _current_screen == "chart":
		_show_chart_panel()
		_set_chart_status("已从存档恢复航线：%s" % _selected_route)
	elif _current_screen == "exploration":
		_show_chart_panel()
		_set_chart_status("已从存档恢复探索 HUD：%s" % _selected_route)
	else:
		_show_hub()


func _show_hub() -> void:
	_current_screen = "hub"
	if _chart_panel != null:
		_chart_panel.visible = false
	_set_hub_controls_enabled(true)
	_set_footer("HUD 入口：点击“打开航图 / HUD”或按 M。保存/加载入口在右侧。")
	_grab_button("ChartButton")


func _show_chart_panel() -> void:
	if _chart_panel != null:
		_chart_panel.visible = true
	_set_hub_controls_enabled(false)
	_set_footer("HUD / 航图已接管输入：方向键仅在航图内移动，Esc 返回 Hub。")
	_grab_button("RouteMistButton")


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
