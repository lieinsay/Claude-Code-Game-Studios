extends SceneTree

const SESSION_SCENE := "res://src/scenes/SessionShell.tscn"

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

	var first_session := await _boot_session(packed)
	var first_hub := first_session.find_child("HubRuntime", true, false)
	_expect(first_hub != null, "first HubRuntime is mounted")
	if first_hub == null:
		_finish()
		return

	first_hub.call("DebugClearDurableProgress")
	await process_frame
	_expect(not bool(first_hub.call("DebugDurableProgressExists")), "durable progress file starts cleared")
	_expect(_button_disabled(first_session, "LoadButton"), "Load button is disabled when no durable progress exists")
	_expect(_label_text(first_session, "SaveStatusLabel").contains("暂无可加载进度"), "Hub explains that no progress can be loaded")

	first_hub.call("OnChartPressed")
	await process_frame
	first_hub.call("OnRouteMistPressed")
	await process_frame
	first_hub.call("OnDepartPressed")
	await process_frame
	first_hub.call("OnExplorationAdvancePressed")
	await process_frame
	first_hub.call("OnExplorationAdvancePressed")
	await process_frame
	first_hub.call("OnSavePressed")
	await process_frame

	var saved_snapshot := first_hub.call("DebugDomainSnapshot") as Dictionary
	_expect(int(saved_snapshot.get("exploration_step", 0)) == 2, "first session saves a mid-exploration step")
	_expect(int(saved_snapshot.get("reward_carried", 0)) == 2, "first session saves carried rewards")
	_expect(int(saved_snapshot.get("hull_integrity", 0)) == 94, "first session saves hull pressure")
	_expect(bool(first_hub.call("DebugDurableProgressExists")), "save writes a durable progress file")
	_expect(_label_text(first_session, "SaveStatusLabel").contains("可加载"), "Save feedback tells the player the progress can be loaded")

	first_session.queue_free()
	await process_frame
	await process_frame

	var restarted_session := await _boot_session(packed)
	var restarted_hub := restarted_session.find_child("HubRuntime", true, false)
	_expect(restarted_hub != null, "restarted HubRuntime is mounted")
	if restarted_hub == null:
		_finish()
		return
	_expect(not _button_disabled(restarted_session, "LoadButton"), "restarted Hub exposes Load when durable progress is present")
	_expect(_label_text(restarted_session, "SaveStatusLabel").contains("检测到本地进度"), "restarted Hub explains that local progress was detected")

	restarted_hub.call("OnLoadPressed")
	await process_frame
	var loaded_snapshot := restarted_hub.call("DebugDomainSnapshot") as Dictionary
	_expect(_is_panel_visible(restarted_session, "ExplorationPanel"), "restarted session loads back into Exploration HUD")
	_expect(int(loaded_snapshot.get("exploration_step", 0)) == 2, "restarted session restores exploration step from durable progress")
	_expect(str(loaded_snapshot.get("last_search_point", "")) == "sp.playable.2", "restarted session restores authored search point from durable progress")
	_expect(int(loaded_snapshot.get("reward_carried", 0)) == 2, "restarted session restores carried reward state from durable progress")
	_expect(int(loaded_snapshot.get("hull_integrity", 0)) == 94, "restarted session restores hull pressure from durable progress")
	_expect(str(loaded_snapshot.get("last_load_status", "")).contains("canonical progress loaded"), "restarted load still goes through canonical Persistence")
	_expect(_label_text(restarted_session, "SaveStatusLabel").contains("加载完成"), "restarted load gives visible success feedback")

	restarted_hub.call("DebugWriteCorruptDurableProgress")
	await process_frame
	restarted_session.queue_free()
	await process_frame
	await process_frame

	var corrupt_session := await _boot_session(packed)
	var corrupt_hub := corrupt_session.find_child("HubRuntime", true, false)
	_expect(corrupt_hub != null, "corrupt-save HubRuntime is mounted")
	if corrupt_hub == null:
		_finish()
		return
	_expect(_button_disabled(corrupt_session, "LoadButton"), "Load button is disabled when durable progress fails validation")
	_expect(not bool(corrupt_hub.call("DebugDurableProgressExists")), "corrupt durable progress is removed from the active save path")
	_expect(bool(corrupt_hub.call("DebugQuarantinedProgressExists")), "corrupt durable progress is quarantined for diagnostics")
	_expect(_label_text(corrupt_session, "SaveStatusLabel").contains("已隔离"), "Corrupt durable progress reports quarantine on boot")
	corrupt_hub.call("OnLoadPressed")
	await process_frame
	_expect(_label_text(corrupt_session, "SaveStatusLabel").contains("校验失败"), "Corrupt durable progress reports checksum failure and does not load")
	_expect(_label_text(corrupt_session, "SaveStatusLabel").contains("可重新保存"), "Corrupt durable progress tells the player a new safe save can replace it")

	corrupt_hub.call("OnSavePressed")
	await process_frame
	_expect(bool(corrupt_hub.call("DebugDurableProgressExists")), "new progress can be saved after quarantine")
	_expect(not _button_disabled(corrupt_session, "LoadButton"), "Load button is re-enabled after saving new progress")
	_expect(_label_text(corrupt_session, "SaveStatusLabel").contains("可加载"), "new save feedback restores continue trust after quarantine")

	corrupt_hub.call("DebugClearDurableProgress")
	await process_frame
	_expect(not bool(corrupt_hub.call("DebugQuarantinedProgressExists")), "debug clear removes quarantined progress")
	corrupt_session.queue_free()
	await process_frame
	_finish()


func _boot_session(packed: PackedScene) -> Node:
	var session := packed.instantiate()
	root.add_child(session)
	await process_frame
	await process_frame
	session.call("_on_start_pressed")
	await process_frame
	session.call("_on_audio_confirmed")
	await process_frame
	await process_frame
	return session


func _label_text(root_node: Node, node_name: String) -> String:
	var label := root_node.find_child(node_name, true, false) as Label
	return "" if label == null else label.text


func _is_panel_visible(root_node: Node, node_name: String) -> bool:
	var panel := root_node.find_child(node_name, true, false) as CanvasItem
	return panel != null and panel.visible


func _button_disabled(root_node: Node, node_name: String) -> bool:
	var button := root_node.find_child(node_name, true, false) as Button
	return false if button == null else button.disabled


func _expect(condition: bool, label: String) -> void:
	if condition:
		print("PASS ", label)
		return

	_failed = true
	push_error("FAIL " + label)


func _finish() -> void:
	quit(1 if _failed else 0)
