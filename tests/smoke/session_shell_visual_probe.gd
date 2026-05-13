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
	_expect(session.find_child("HubRuntime", true, false) != null, "HubRuntime is mounted")
	_expect(_label_text(session, "Header") == "云织号空艇中枢", "Hub header uses Chinese text")
	_expect(_label_text(session, "CargoValue").contains("受困货物 0"), "Cargo status reports trapped goods in Chinese")
	_expect(_label_text(session, "HullValue").contains("可出航"), "Hull status uses Chinese text")

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
