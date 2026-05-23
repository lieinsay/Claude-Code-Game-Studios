extends SceneTree

const SESSION_SCENE := "res://src/scenes/SessionShell.tscn"
const CYCLE_COUNT := 3

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
	session.call("_on_start_pressed")
	await process_frame
	session.call("_on_audio_confirmed")
	await process_frame
	await process_frame

	var hub := session.find_child("HubRuntime", true, false)
	_expect(hub != null, "HubRuntime is mounted")
	if hub == null:
		_finish()
		return

	hub.call("DebugClearDurableProgress")
	await process_frame
	_expect(_button_disabled(session, "LoadButton"), "long-session probe starts with Load disabled")

	var last_generation := 0
	for cycle in range(CYCLE_COUNT):
		hub.call("OnChartPressed")
		await process_frame
		hub.call("OnRouteMistPressed")
		await process_frame
		hub.call("OnDepartPressed")
		await process_frame
		_expect(_is_panel_visible(session, "ExplorationPanel"), "cycle %d reaches Exploration HUD" % [cycle + 1])

		hub.call("DebugSetPlayerPosition", Vector2(638, 613))
		await process_frame
		hub.call("OnExplorationAdvancePressed")
		await process_frame
		hub.call("OnExplorationAdvancePressed")
		await process_frame
		var pressure_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
		_expect(int(pressure_snapshot.get("exploration_step", 0)) == 2, "cycle %d reaches mid-exploration pressure" % [cycle + 1])
		_expect(int(pressure_snapshot.get("hull_integrity", 0)) > 0, "cycle %d keeps hull above failure state" % [cycle + 1])

		await _save_with_confirmation(hub, session)
		var saved_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
		var generation := int(saved_snapshot.get("persistence_generation", 0))
		_expect(generation > last_generation, "cycle %d advances persistence generation" % [cycle + 1])
		last_generation = generation
		_expect(bool(hub.call("DebugDurableProgressExists")), "cycle %d leaves durable progress present" % [cycle + 1])

		hub.call("OnLoadPressed")
		await process_frame
		var loaded_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
		_expect(_is_panel_visible(session, "ExplorationPanel"), "cycle %d load restores Exploration HUD" % [cycle + 1])
		_expect(int(loaded_snapshot.get("exploration_step", 0)) == 2, "cycle %d load restores pressure step" % [cycle + 1])
		_expect(str(loaded_snapshot.get("last_load_status", "")).contains("canonical progress loaded"), "cycle %d load uses canonical Persistence" % [cycle + 1])

		hub.call("DebugSetPlayerPosition", Vector2(1058, 613))
		await process_frame
		hub.call("OnExplorationReturnPressed")
		await process_frame
		var return_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
		_expect(not _is_panel_visible(session, "ExplorationPanel"), "cycle %d returns to Hub" % [cycle + 1])
		_expect(int(return_snapshot.get("reward_carried", 0)) == 0, "cycle %d clears carried rewards after Hub return" % [cycle + 1])
		_expect(int(return_snapshot.get("reward_in_storage", 0)) >= 2, "cycle %d preserves returned rewards in storage" % [cycle + 1])
		_expect(not _button_disabled(session, "LoadButton"), "cycle %d keeps Load available in Hub" % [cycle + 1])
		_expect(not _button_disabled(session, "DeleteProgressButton"), "cycle %d keeps Delete available in Hub" % [cycle + 1])

	hub.call("OnLoadPressed")
	await process_frame
	var final_snapshot := hub.call("DebugDomainSnapshot") as Dictionary
	_expect(_is_panel_visible(session, "ExplorationPanel"), "final load restores the latest saved Exploration HUD")
	_expect(int(final_snapshot.get("exploration_step", 0)) == 2, "final load restores latest saved pressure step")
	_expect(int(final_snapshot.get("persistence_generation", 0)) == last_generation, "final load keeps latest persistence generation")

	hub.call("DebugClearDurableProgress")
	session.queue_free()
	await process_frame
	_finish()


func _save_with_confirmation(hub: Node, session: Node) -> void:
	hub.call("OnSavePressed")
	await process_frame
	if _label_text(session, "SaveStatusLabel").contains("覆盖确认"):
		hub.call("OnSavePressed")
		await process_frame
	_expect(_label_text(session, "SaveStatusLabel").contains("保存完成"), "save completes after required confirmation")


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
