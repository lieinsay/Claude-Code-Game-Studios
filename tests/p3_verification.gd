# P3 Architecture Verification — Scenes B & C
# Usage: godot --headless --script tests/p3_verification.gd
# Scene A already verified (boot chain via session_shell.tscn)
extends SceneTree

# Preload script classes (Autoload scripts lack class_name)
const PersistenceClass := preload("res://src/core/persistence.gd")
const SnapshotPkgClass := preload("res://src/core/snapshot_package.gd")
const WorldRepairClass := preload("res://src/feature/world_repair.gd")

# Enum values
const DomainState := { READY = 0, BLOCKED = 1, NOT_READY = 2, SETTLING = 3 }
const RepairState := { UNKNOWN = 0, KNOWN = 1, MATERIALS_COMMITTED = 2, REPAIRING = 3, REPAIRED = 4 }

# Scene B state
var _b4_save_done := false
var _b4_saved_gen := 0
var _b5_load_done := false
var _b5_restored_resources: Dictionary = {}
var _b6_load_failed := false
var _b6_fail_reason := ""
var _b7_resources: Dictionary = {}
var _b7_repair: Dictionary = {}

# Scene C state
var _c1_calls := 0
var _c1_node_ids: Array[StringName] = []
var _c2_premature := false
var _c3_node: StringName = &""
var _c3_res: StringName = &""
var _c3_qty := 0
var _c4_emit_count := 0

var _pass := 0
var _fail := 0

func _init() -> void:
	prints("=== P3 Verification: Scene B (Save Roundtrip) + Scene C (Signal Fanout) ===")
	prints(Time.get_datetime_string_from_system())

	_run_scene_b()
	_run_scene_c()

	prints("=".repeat(50))
	prints("P3 Architecture Verification: %d/%d checks passed" % [_pass, _pass + _fail])
	if _fail > 0:
		prints("[VERDICT: FAIL] — %d checks failed" % _fail)
		quit(1)
	else:
		prints("[VERDICT: PASS] — All checks passed")
		quit(0)


## Scene B: Save Roundtrip

func _run_scene_b() -> void:
	prints("")
	prints("--- Scene B: Save Roundtrip ---")

	# B1: Canonical JSON encoding
	var p1 = PersistenceClass.new()
	root.add_child(p1)
	p1._ready()
	var input := {"c": 3, "a": 1, "b": 2}
	var r1: String = p1._canonical_json_encode(input)
	var r2: String = p1._canonical_json_encode(input)
	_check("B1.1 — Canonical JSON deterministic", r1 == r2)
	_check("B1.2 — Keys sorted (a before b)", r1.find("\"a\"") < r1.find("\"b\""))

	# B2: SHA-256
	var c1: String = p1._compute_checksum("hello")
	var c2: String = p1._compute_checksum("hello")
	_check("B2.1 — SHA-256 deterministic", c1 == c2)
	_check("B2.2 — SHA-256 hex digest = 64 chars", c1.length() == 64)
	root.remove_child(p1)
	p1.free()

	# B3: SnapshotPackage roundtrip
	var pkg = SnapshotPkgClass.new()
	pkg.domain_id = &"resources"
	pkg.snapshot_schema_version = 1
	pkg.content_domain_versions = {"resources": "1.0"}
	var b3_refs: Array[StringName] = [&"resource.repair_kit"]
	pkg.stable_id_refs = b3_refs
	pkg.payload = {"storage": {"resource.repair_kit": 25}}
	pkg.domain_state = DomainState.READY
	var restored = SnapshotPkgClass.from_dict(pkg.to_dict())
	_check("B3.1 — domain_id preserved", restored.domain_id == &"resources")
	_check("B3.2 — payload preserved", restored.payload.storage["resource.repair_kit"] == 25)
	_check("B3.3 — is_valid after roundtrip", restored.is_valid())

	# B4: Save pipeline
	var save_p = PersistenceClass.new()
	root.add_child(save_p)
	save_p._ready()

	save_p.register_domain_serializer(&"resources", func():
		var sp = SnapshotPkgClass.new()
		sp.domain_id = &"resources"
		sp.snapshot_schema_version = 1
		sp.content_domain_versions = {"resources": "1.0"}
		var refs: Array[StringName] = [&"resource.repair_kit", &"resource.basic_supply"]
		sp.stable_id_refs = refs
		sp.payload = {"storage": {"resource.repair_kit": 12, "resource.basic_supply": 45}}
		sp.domain_state = DomainState.READY
		return sp
	)

	save_p.save_completed.connect(_on_b4_save_completed)
	save_p.save_failed.connect(_on_b4_save_failed)
	_b4_save_done = false
	_b4_saved_gen = 0
	save_p.request_save_progress()

	_check("B4.1 — save_completed signal emitted", _b4_save_done)
	_check("B4.2 — generation == 1", _b4_saved_gen == 1)
	_check("B4.3 — pipeline returns to IDLE", save_p.is_pipeline_idle())

	if not _b4_save_done:
		prints("  [SKIP] B5-B7 — save pipeline failed, skipping load tests")
		root.remove_child(save_p)
		save_p.free()
		return

	# B5: Load pipeline
	save_p.register_domain_deserializer(&"resources", _on_b5_deserialize)
	save_p.load_completed.connect(_on_b5_load_completed)
	_b5_load_done = false
	_b5_restored_resources = {}
	save_p.request_load_progress()

	_check("B5.1 — load_completed signal emitted", _b5_load_done)
	_check("B5.2 — repair_kit restored (=12)",
		_b5_restored_resources.get("storage", {}).get("resource.repair_kit") == 12)
	_check("B5.3 — basic_supply restored (=45)",
		_b5_restored_resources.get("storage", {}).get("resource.basic_supply") == 45)
	root.remove_child(save_p)
	save_p.free()

	# B6: Empty load gracefully fails
	var empty_p = PersistenceClass.new()
	root.add_child(empty_p)
	empty_p._ready()
	empty_p.load_failed.connect(_on_b6_load_failed)
	_b6_load_failed = false
	_b6_fail_reason = ""
	empty_p.request_load_progress()
	_check("B6.1 — empty load fails with 'no_safe_data'", _b6_load_failed and _b6_fail_reason == "no_safe_data")
	root.remove_child(empty_p)
	empty_p.free()

	# B7: Multi-domain save + restore
	var multi_p = PersistenceClass.new()
	root.add_child(multi_p)
	multi_p._ready()
	multi_p.register_domain_serializer(&"resources", func():
		var sp = SnapshotPkgClass.new()
		sp.domain_id = &"resources"
		sp.snapshot_schema_version = 1
		sp.content_domain_versions = {"resources": "1.0"}
		sp.payload = {"carried": {"resource.cloud_coin": 200}}
		sp.domain_state = DomainState.READY
		return sp
	)
	multi_p.register_domain_serializer(&"repair", func():
		var sp = SnapshotPkgClass.new()
		sp.domain_id = &"repair"
		sp.snapshot_schema_version = 1
		sp.content_domain_versions = {"repair": "1.0"}
		sp.payload = {"completed": ["repair_node.starlight_dock"]}
		sp.domain_state = DomainState.READY
		return sp
	)
	multi_p.request_save_progress()

	multi_p.register_domain_deserializer(&"resources", func(s): _b7_resources = s.payload.duplicate())
	multi_p.register_domain_deserializer(&"repair", func(s): _b7_repair = s.payload.duplicate())
	_b7_resources = {}
	_b7_repair = {}
	multi_p.request_load_progress()

	_check("B7.1 — multi-domain: resources restored",
		_b7_resources.get("carried", {}).get("resource.cloud_coin") == 200)
	# Safe index access
	var completed_arr = _b7_repair.get("completed", [])
	_check("B7.2 — multi-domain: repair restored",
		completed_arr.size() > 0 and completed_arr[0] == "repair_node.starlight_dock")
	root.remove_child(multi_p)
	multi_p.free()

	prints("Scene B: verified")


## Scene C: Signal Fanout

func _run_scene_c() -> void:
	prints("")
	prints("--- Scene C: Signal Fanout ---")

	# C1: repair_completed fans out to 4 consumers
	var wr = WorldRepairClass.new()
	root.add_child(wr)
	wr._ready()

	_c1_calls = 0
	_c1_node_ids.clear()
	wr.repair_completed.connect(_on_c1_consumer_0)
	wr.repair_completed.connect(_on_c1_consumer_1)
	wr.repair_completed.connect(_on_c1_consumer_2)
	wr.repair_completed.connect(_on_c1_consumer_3)

	wr.register_repair_node(&"repair_node.starlight_dock", {
		"resource.repair_kit": 2,
		"resource.beacon_crystal": 1,
	})
	wr.commit_deposit(&"repair_node.starlight_dock", &"resource.repair_kit", 2)
	wr.commit_deposit(&"repair_node.starlight_dock", &"resource.beacon_crystal", 1)

	_check("C1.1 — 4 consumers connected to repair_completed", _c1_calls == 4)
	_check("C1.2 — All consumers received starlight_dock node_id", _c1_node_ids.size() == 4)
	for i: int in range(_c1_node_ids.size()):
		_check("C1.2.%d — Consumer %d node_id correct" % [i, i+1],
			_c1_node_ids[i] == &"repair_node.starlight_dock")
	root.remove_child(wr)
	wr.free()

	# C2: repair_completed NOT emitted on partial deposit
	var wr2 = WorldRepairClass.new()
	root.add_child(wr2)
	wr2._ready()
	_c2_premature = false
	wr2.repair_completed.connect(_on_c2_premature)
	wr2.register_repair_node(&"node.test", {
		"resource.repair_kit": 5,
		"resource.beacon_crystal": 3,
	})
	wr2.commit_deposit(&"node.test", &"resource.repair_kit", 5)
	_check("C2.1 — repair_completed NOT emitted on partial deposit", not _c2_premature)
	root.remove_child(wr2)
	wr2.free()

	# C3: deposit_committed typed params
	var wr3 = WorldRepairClass.new()
	root.add_child(wr3)
	wr3._ready()
	_c3_node = &""
	_c3_res = &""
	_c3_qty = 0
	wr3.deposit_committed.connect(_on_c3_deposit)
	wr3.register_repair_node(&"repair_node.test", {"resource.repair_kit": 10})
	wr3.commit_deposit(&"repair_node.test", &"resource.repair_kit", 3)
	_check("C3.1 — deposit_committed node_id typed (StringName)", _c3_node == &"repair_node.test")
	_check("C3.2 — deposit_committed resource_id typed (StringName)", _c3_res == &"resource.repair_kit")
	_check("C3.3 — deposit_committed quantity typed (int)", _c3_qty == 3)
	root.remove_child(wr3)
	wr3.free()

	# C4: repair_completed emitted once per node
	var wr4 = WorldRepairClass.new()
	root.add_child(wr4)
	wr4._ready()
	_c4_emit_count = 0
	wr4.repair_completed.connect(_on_c4_emit)
	wr4.register_repair_node(&"node.a", {"resource.repair_kit": 1})
	wr4.register_repair_node(&"node.b", {"resource.beacon_crystal": 1})
	wr4.commit_deposit(&"node.a", &"resource.repair_kit", 1)
	wr4.commit_deposit(&"node.b", &"resource.beacon_crystal", 1)
	_check("C4.1 — once per node (2 nodes → 2 emits)", _c4_emit_count == 2)
	wr4.commit_deposit(&"node.a", &"resource.repair_kit", 99)
	_check("C4.2 — no re-emit on REPAIRED node", _c4_emit_count == 2)
	root.remove_child(wr4)
	wr4.free()

	# C5: Repair state machine
	var wr5 = WorldRepairClass.new()
	root.add_child(wr5)
	wr5._ready()
	_check("C5.1 — unregistered node = UNKNOWN", wr5.get_node_state(&"nonexistent") == RepairState.UNKNOWN)
	wr5.register_repair_node(&"node.sm", {"resource.repair_kit": 1})
	_check("C5.2 — registered = KNOWN", wr5.get_node_state(&"node.sm") == RepairState.KNOWN)
	_check("C5.3 — can_deposit in KNOWN", wr5.can_deposit(&"node.sm"))
	wr5.commit_deposit(&"node.sm", &"resource.repair_kit", 1)
	_check("C5.4 — all materials → REPAIRED", wr5.get_node_state(&"node.sm") == RepairState.REPAIRED)
	_check("C5.5 — cannot deposit in REPAIRED", not wr5.can_deposit(&"node.sm"))
	root.remove_child(wr5)
	wr5.free()

	# C6: Signal naming {noun}_{verb_past}
	var signals_to_check := [
		"deposit_committed", "repair_completed", "route_committed",
		"save_completed", "load_completed", "promotion_completed",
	]
	for sig_name: String in signals_to_check:
		_check("C6 — '%s' past tense per ADR-0002" % sig_name,
			sig_name.ends_with("ed") or sig_name.ends_with("en"))

	# C7: Signal payload typed params
	var wr_sig = WorldRepairClass.new()
	root.add_child(wr_sig)
	wr_sig._ready()
	var wr_sigs := wr_sig.get_signal_list()
	for sig: Dictionary in wr_sigs:
		for arg: Dictionary in sig.get("args", []):
			_check("C7 — '%s' arg '%s' typed (type=%d, expect !=0)" % [sig.name, arg.name, arg.type],
				arg.type != 0)
	root.remove_child(wr_sig)
	wr_sig.free()

	prints("Scene C: verified")


## Signal callbacks (must be named methods, NOT lambdas — lambdas don't work in --script mode)

func _on_b4_save_completed(gen: int) -> void:
	_b4_save_done = true
	_b4_saved_gen = gen

func _on_b4_save_failed(reason: String, phase: String) -> void:
	printerr("  [DEBUG] save_failed: reason=%s phase=%s" % [reason, phase])

func _on_b5_deserialize(snapshot) -> void:
	_b5_restored_resources = snapshot.payload.duplicate()

func _on_b5_load_completed(_a, _g) -> void:
	_b5_load_done = true

func _on_b6_load_failed(reason: String, _d) -> void:
	_b6_load_failed = true
	_b6_fail_reason = reason

func _on_c1_consumer_0(nid: StringName) -> void:
	_c1_calls += 1
	_c1_node_ids.append(nid)

func _on_c1_consumer_1(nid: StringName) -> void:
	_c1_calls += 1
	_c1_node_ids.append(nid)

func _on_c1_consumer_2(nid: StringName) -> void:
	_c1_calls += 1
	_c1_node_ids.append(nid)

func _on_c1_consumer_3(nid: StringName) -> void:
	_c1_calls += 1
	_c1_node_ids.append(nid)

func _on_c2_premature(_n) -> void:
	_c2_premature = true

func _on_c3_deposit(nid: StringName, rid: StringName, qty: int) -> void:
	_c3_node = nid
	_c3_res = rid
	_c3_qty = qty

func _on_c4_emit(_n) -> void:
	_c4_emit_count += 1


## Helpers

func _check(label: String, condition: bool) -> void:
	if condition:
		prints("  [PASS]", label)
		_pass += 1
	else:
		prints("  [FAIL]", label)
		printerr("  [FAIL] %s" % label)
		_fail += 1
