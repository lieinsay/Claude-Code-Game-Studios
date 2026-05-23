extends SceneTree

const SESSION_SCENE := "res://src/scenes/SessionShell.tscn"
const FRAME_BUDGET_MS := 16.0
const FRAME_SPIKE_CEILING_MS := 20.0
const MEMORY_BUDGET_MIB := 512.0
const DRAW_CALL_BUDGET := 400.0
const SAVE_LOAD_BUDGET_MS := 50.0
const TRANSITION_BUDGET_MS := 500.0
const MONITOR_MEMORY_STATIC := 4
const MONITOR_RENDER_TOTAL_DRAW_CALLS_IN_FRAME := 13

var _failed := false
var _frame_ms := []
var _memory_mib := []
var _draw_calls := []
var _chart_cycle_ms := []
var _save_ms := []
var _load_ms := []


func _init() -> void:
	root.size = Vector2i(1280, 720)
	call_deferred("_run")


func _run() -> void:
	var display_driver := DisplayServer.get_name()
	print("PERF Display driver: %s" % display_driver)

	var packed := load(SESSION_SCENE) as PackedScene
	_expect(packed != null, "SessionShell scene loads")
	if packed == null:
		_finish()
		return

	var boot_start := Time.get_ticks_usec()
	var session := packed.instantiate()
	root.add_child(session)
	await _sample_frames(2)

	session.call("_on_start_pressed")
	await _sample_frames(1)
	session.call("_on_audio_confirmed")
	await _sample_frames(4)
	var boot_to_hub_ms := _elapsed_ms(boot_start)

	var hub := session.find_child("HubRuntime", true, false)
	_expect(hub != null, "HubRuntime is mounted")
	if hub == null:
		_finish()
		return

	await _sample_frames(1)

	for index in range(10):
		var chart_start := Time.get_ticks_usec()
		hub.call("EnterShipInterior")
		await _sample_frames(1)
		hub.call("OnChartPressed")
		await _sample_frames(1)
		hub.call("ShowHub")
		await _sample_frames(1)
		_chart_cycle_ms.append(_elapsed_ms(chart_start))

	hub.call("EnterShipInterior")
	await _sample_frames(1)
	hub.call("OnChartPressed")
	await _sample_frames(1)
	hub.call("OnRouteMistPressed")
	await _sample_frames(1)

	var departure_start := Time.get_ticks_usec()
	hub.call("OnDepartPressed")
	await _sample_frames(2)
	var route_departure_ms := _elapsed_ms(departure_start)

	hub.call("DebugSetPlayerPosition", Vector2(638, 613))
	await _sample_frames(1)
	for index in range(6):
		hub.call("OnExplorationAdvancePressed")
		await _sample_frames(1)

	for index in range(10):
		var save_start := Time.get_ticks_usec()
		hub.call("OnSavePressed")
		await _sample_frames(1)
		_save_ms.append(_elapsed_ms(save_start))

		var load_start := Time.get_ticks_usec()
		hub.call("OnLoadPressed")
		await _sample_frames(1)
		_load_ms.append(_elapsed_ms(load_start))

	hub.call("DebugSetPlayerPosition", Vector2(638, 613))
	await _sample_frames(1)
	for index in range(3):
		hub.call("OnExplorationAdvancePressed")
		await _sample_frames(1)

	var return_start := Time.get_ticks_usec()
	hub.call("ShowHub")
	await _sample_frames(2)
	var return_hub_ms := _elapsed_ms(return_start)

	var frame_stats := _stats(_frame_ms)
	var memory_stats := _stats(_memory_mib)
	var draw_stats := _stats(_draw_calls)
	var chart_stats := _stats(_chart_cycle_ms)
	var save_stats := _stats(_save_ms)
	var load_stats := _stats(_load_ms)

	print("PERF Frame avg/p95/worst ms: %.3f / %.3f / %.3f" % [frame_stats["avg"], frame_stats["p95"], frame_stats["max"]])
	print("PERF Peak memory MiB: %.3f" % memory_stats["max"])
	print("PERF Peak draw calls: %.0f" % draw_stats["max"])
	print("PERF Boot to Hub ms: %.3f" % boot_to_hub_ms)
	print("PERF Chart open/close avg/worst ms: %.3f / %.3f" % [chart_stats["avg"], chart_stats["max"]])
	print("PERF Save p50/p95/max ms: %.3f / %.3f / %.3f" % [save_stats["p50"], save_stats["p95"], save_stats["max"]])
	print("PERF Load p50/p95/max ms: %.3f / %.3f / %.3f" % [load_stats["p50"], load_stats["p95"], load_stats["max"]])
	print("PERF Route departure ms: %.3f" % route_departure_ms)
	print("PERF Return Hub ms: %.3f" % return_hub_ms)
	print("PERF Samples: frames=%d chart=%d save=%d load=%d" % [_frame_ms.size(), _chart_cycle_ms.size(), _save_ms.size(), _load_ms.size()])

	_expect(frame_stats["p95"] <= FRAME_BUDGET_MS, "Frame p95 stays within 16ms budget")
	_expect(frame_stats["max"] <= FRAME_SPIKE_CEILING_MS, "Worst sampled frame stays within 20ms transient ceiling")
	_expect(memory_stats["max"] <= MEMORY_BUDGET_MIB, "Peak static memory stays within 512MiB budget")
	if display_driver == "headless":
		print("SKIP Draw-call budget unavailable under headless display driver")
	else:
		_expect(draw_stats["max"] <= DRAW_CALL_BUDGET, "Peak draw calls stay within 400 budget")
	_expect(save_stats["p95"] <= SAVE_LOAD_BUDGET_MS, "Save p95 stays within 50ms budget")
	_expect(load_stats["p95"] <= SAVE_LOAD_BUDGET_MS, "Load p95 stays within 50ms budget")
	_expect(route_departure_ms <= TRANSITION_BUDGET_MS, "Route departure transition stays within 500ms budget")
	_expect(return_hub_ms <= TRANSITION_BUDGET_MS, "Return Hub transition stays within 500ms budget")

	session.queue_free()
	await _sample_frames(1)
	_finish()


func _sample_frames(count: int) -> void:
	for index in range(count):
		var start := Time.get_ticks_usec()
		await process_frame
		_frame_ms.append(_elapsed_ms(start))
		_memory_mib.append(_memory_static_mib())
		_draw_calls.append(_render_draw_calls())


func _stats(values: Array) -> Dictionary:
	if values.is_empty():
		return {
			"count": 0,
			"min": 0.0,
			"avg": 0.0,
			"p50": 0.0,
			"p95": 0.0,
			"max": 0.0,
		}

	var sorted := values.duplicate()
	sorted.sort()
	var total := 0.0
	for value in sorted:
		total += float(value)

	return {
		"count": sorted.size(),
		"min": _round3(float(sorted[0])),
		"avg": _round3(total / sorted.size()),
		"p50": _round3(float(sorted[_percentile_index(sorted.size(), 0.50)])),
		"p95": _round3(float(sorted[_percentile_index(sorted.size(), 0.95)])),
		"max": _round3(float(sorted[sorted.size() - 1])),
	}


func _percentile_index(count: int, percentile: float) -> int:
	return clampi(int(ceil(float(count - 1) * percentile)), 0, count - 1)


func _elapsed_ms(start_usec: int) -> float:
	return _round3(float(Time.get_ticks_usec() - start_usec) / 1000.0)


func _memory_static_mib() -> float:
	return _round3(float(Performance.get_monitor(MONITOR_MEMORY_STATIC)) / 1024.0 / 1024.0)


func _render_draw_calls() -> float:
	return float(Performance.get_monitor(MONITOR_RENDER_TOTAL_DRAW_CALLS_IN_FRAME))


func _round3(value: float) -> float:
	return round(value * 1000.0) / 1000.0


func _expect(condition: bool, label: String) -> void:
	if condition:
		print("PASS ", label)
		return

	_failed = true
	push_error("FAIL " + label)


func _finish() -> void:
	quit(1 if _failed else 0)
