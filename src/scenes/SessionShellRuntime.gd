extends Node2D

var _panels: Array[Control] = []
var _loading_panel: Control
var _entry_panel: Control
var _audio_panel: Control
var _recovery_panel: Control
var _shell_ui_root: Control
var _diagnostic_panel: CanvasItem
var _gameplay_layer: Node
var _hub_scene: PackedScene
var _active_gameplay: Node


func _ready() -> void:
	_cache_nodes()
	_load_gameplay_scenes()
	_wire_buttons()
	_show_entry()


func _unhandled_input(event: InputEvent) -> void:
	if not event is InputEventKey:
		return

	var key := event as InputEventKey
	if not key.pressed or key.echo:
		return

	if key.keycode == KEY_ESCAPE:
		_show_entry()
		get_viewport().set_input_as_handled()
	elif _handle_entry_shortcut(key.keycode):
		get_viewport().set_input_as_handled()
	elif _handle_audio_shortcut(key.keycode):
		get_viewport().set_input_as_handled()
	elif _handle_recovery_shortcut(key.keycode):
		get_viewport().set_input_as_handled()


func _cache_nodes() -> void:
	_shell_ui_root = _find_control("ShellUiRoot")
	_loading_panel = _find_control("LoadingPanel")
	_entry_panel = _find_control("EntryPanel")
	_audio_panel = _find_control("AudioActivationPanel")
	_recovery_panel = _find_control("RecoveryPanel")
	_diagnostic_panel = find_child("RegistryDiagnosticPanel", true, false) as CanvasItem
	_gameplay_layer = find_child("GameplayLayer", true, false)
	_panels = [_loading_panel, _entry_panel, _audio_panel, _recovery_panel]


func _load_gameplay_scenes() -> void:
	_hub_scene = load("res://src/scenes/HubRuntime.tscn") as PackedScene


func _wire_buttons() -> void:
	_wire_button("StartButton", _on_start_pressed)
	_wire_button("SettingsButton", _on_settings_pressed)
	_wire_button("ConfirmAudioButton", _on_audio_confirmed)
	_wire_button("ContinueMutedButton", _on_audio_confirmed)
	_wire_button("AudioReturnTitleButton", _show_entry)
	_wire_button("RetryButton", _show_entry)
	_wire_button("RecoveryNewSessionButton", _show_entry)
	_wire_button("RecoveryReturnTitleButton", _show_entry)
	_wire_button("RecoveryErrorDetailsButton", _on_settings_pressed)
	_wire_button("CancelLoadingButton", _show_entry)


func _wire_button(node_name: String, callback: Callable) -> void:
	var button := find_child(node_name, true, false) as Button
	if button != null and not button.pressed.is_connected(callback):
		button.pressed.connect(callback)
		if not button.mouse_entered.is_connected(_on_button_mouse_entered.bind(button)):
			button.mouse_entered.connect(_on_button_mouse_entered.bind(button))


func _handle_entry_shortcut(keycode: Key) -> bool:
	if not _is_visible(_entry_panel):
		return false

	if keycode == KEY_ENTER:
		_on_start_pressed()
		return true

	if keycode == KEY_TAB:
		_on_settings_pressed()
		return true

	return false


func _handle_audio_shortcut(keycode: Key) -> bool:
	if not _is_visible(_audio_panel):
		return false

	if keycode == KEY_ENTER:
		_on_audio_confirmed()
		return true

	if keycode == KEY_M:
		_on_audio_confirmed()
		return true

	return false


func _handle_recovery_shortcut(keycode: Key) -> bool:
	if not _is_visible(_recovery_panel):
		return false

	if keycode == KEY_R:
		_show_entry()
		return true

	if keycode == KEY_N:
		_show_entry()
		return true

	if keycode == KEY_D:
		_on_settings_pressed()
		return true

	return false


func _on_start_pressed() -> void:
	var prompt := find_child("AudioPromptLabel", true, false) as Label
	if prompt != null:
		prompt.text = "启用船内声场后登船。"

	_show_only(_audio_panel)
	_grab_button("ConfirmAudioButton")


func _on_audio_confirmed() -> void:
	if _mount_hub_runtime():
		_hide_shell_panels()
		return

	var message := find_child("RecoveryMessageLabel", true, false) as Label
	if message != null:
		message.text = "音频已启用，但空艇中枢场景挂载失败。"

	_show_only(_recovery_panel)
	_grab_button("RetryButton")


func _on_settings_pressed() -> void:
	if _diagnostic_panel != null:
		_diagnostic_panel.visible = not _diagnostic_panel.visible
		return

	var message := find_child("RecoveryMessageLabel", true, false) as Label
	if message != null:
		message.text = "设置与诊断面板尚未初始化。"

	_show_only(_recovery_panel)
	_grab_button("RetryButton")


func _on_button_mouse_entered(button: Button) -> void:
	if button.visible and not button.disabled:
		button.grab_focus()


func _show_entry() -> void:
	if _active_gameplay != null:
		_active_gameplay.queue_free()
		_active_gameplay = null

	_show_only(_entry_panel)
	_grab_button("StartButton")


func _show_only(target: Control) -> void:
	_set_shell_mouse_passthrough(false)
	for panel in _panels:
		if panel != null:
			panel.visible = panel == target


func _hide_shell_panels() -> void:
	for panel in _panels:
		if panel != null:
			panel.visible = false
	_set_shell_mouse_passthrough(true)


func _set_shell_mouse_passthrough(enabled: bool) -> void:
	if _shell_ui_root != null:
		_shell_ui_root.mouse_filter = Control.MOUSE_FILTER_IGNORE if enabled else Control.MOUSE_FILTER_STOP


func _mount_hub_runtime() -> bool:
	if _gameplay_layer == null or _hub_scene == null:
		return false

	if _active_gameplay != null and is_instance_valid(_active_gameplay):
		return true

	_active_gameplay = _hub_scene.instantiate()
	_gameplay_layer.add_child(_active_gameplay)
	return true


func _grab_button(node_name: String) -> void:
	var button := find_child(node_name, true, false) as Button
	if button != null:
		button.grab_focus()


func _find_control(node_name: String) -> Control:
	return find_child(node_name, true, false) as Control


func _is_visible(item: CanvasItem) -> bool:
	return item != null and item.visible
