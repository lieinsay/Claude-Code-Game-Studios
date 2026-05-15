extends SceneTree

const SESSION_SCENE := "res://src/scenes/SessionShell.tscn"
const SCREENSHOT_PATH := "user://session_shell_hub_probe.png"

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
	_expect(_label_text(session, "Header") == "云织号空艇中枢", "Hub header uses Chinese text")
	_expect(_label_text(session, "CargoValue").contains("受困货物 0"), "Cargo status reports trapped goods in Chinese")
	_expect(_label_text(session, "HullValue").contains("可出航"), "Hull status uses Chinese text")
	_expect(_button_text(session, "ChartButton").contains("航图 / HUD"), "HUD and Chart entry is visible")
	_expect(_button_text(session, "SaveButton").contains("保存"), "Save entry is visible")
	_expect(_button_text(session, "LoadButton").contains("加载"), "Load entry is visible")

	var hub := session.find_child("HubRuntime", true, false)
	hub.call("_on_save_pressed")
	await process_frame
	_expect(_label_text(session, "SaveStatusLabel").contains("保存完成"), "Save action gives visible success feedback")

	hub.call("_on_load_pressed")
	await process_frame
	_expect(_label_text(session, "SaveStatusLabel").contains("加载完成"), "Load action gives visible success feedback")

	hub.call("_on_chart_pressed")
	await process_frame
	_expect(_is_panel_visible(session, "ChartPanel"), "Chart panel is visible after HUD entry")
	_expect(_label_text(session, "ChartTitleLabel") == "HUD / 航图界面", "Chart panel identifies the UI/HUD surface")
	_expect(_button_disabled(session, "ChartButton"), "Chart entry is disabled while Chart panel is open")
	_expect(_button_disabled(session, "SaveButton"), "Save entry is disabled while Chart panel is open")
	_expect(_button_disabled(session, "LoadButton"), "Load entry is disabled while Chart panel is open")
	_expect(_button_focus_mode(session, "ChartButton") == Control.FOCUS_NONE, "Chart entry leaves focus chain while Chart panel is open")
	_expect(_button_focus_mode(session, "SaveButton") == Control.FOCUS_NONE, "Save entry leaves focus chain while Chart panel is open")
	_expect(_button_focus_mode(session, "LoadButton") == Control.FOCUS_NONE, "Load entry leaves focus chain while Chart panel is open")

	if DisplayServer.get_name() == "headless":
		print("SKIP Runtime screenshot unavailable with current display driver")
	else:
		await RenderingServer.frame_post_draw
		var texture := root.get_texture()
		if texture == null:
			print("SKIP Runtime screenshot unavailable with current display driver")
		else:
			var image := texture.get_image()
			var saved := image.save_png(SCREENSHOT_PATH)
			_expect(saved == OK, "Runtime screenshot saved to %s" % ProjectSettings.globalize_path(SCREENSHOT_PATH))

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


func _control_mouse_filter(root_node: Node, node_name: String) -> int:
	var control := root_node.find_child(node_name, true, false) as Control
	return -1 if control == null else control.mouse_filter


func _is_panel_visible(root_node: Node, node_name: String) -> bool:
	var panel := root_node.find_child(node_name, true, false) as CanvasItem
	return panel != null and panel.visible


func _expect(condition: bool, label: String) -> void:
	if condition:
		print("PASS ", label)
		return

	_failed = true
	push_error("FAIL " + label)


func _finish() -> void:
	quit(1 if _failed else 0)
