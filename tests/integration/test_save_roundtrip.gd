# Save Roundtrip — Integration Test (Scene B)
# Validates: full staging→verify→promotion pipeline + load restore
# Covers save/load cycle with multiple domain states
# REF: ADR-0003, P3 Scene B acceptance criteria
extends Node

const PersistenceScript := preload("res://src/core/persistence.gd")
const SnapshotPkgScript := preload("res://src/core/snapshot_package.gd")

# Signal tracking for async verification
var _save_completed := false
var _load_completed := false
var _saved_generation := 0
var _restored_resources: Dictionary = {}
var _restored_repair: Dictionary = {}

func test_save_pipeline_completes_successfully() -> void:
	# Arrange
	var p = _make_persistence()

	# Register a mock resources domain serializer
	p.register_domain_serializer(&"resources", func() -> SnapshotPackage:
		var pkg := SnapshotPkgScript.new()
		pkg.domain_id = &"resources"
		pkg.snapshot_schema_version = 1
		pkg.content_domain_versions = {"resources": "1.0"}
		pkg.stable_id_refs = [&"resource.repair_kit", &"resource.basic_supply"]
		pkg.payload = {
			"storage": {"resource.repair_kit": 12, "resource.basic_supply": 45},
			"carried": {"resource.beacon_crystal": 3},
		}
		pkg.domain_state = SnapshotPkgScript.DomainState.READY
		return pkg
	)

	# Act — trigger save
	p.save_completed.connect(func(gen: int):
		_save_completed = true
		_saved_generation = gen
	)
	p.request_save_progress()

	# Assert
	assert(_save_completed, "Save should complete successfully")
	assert(_saved_generation == 1, "First save should produce generation 1, got %d" % _saved_generation)
	assert(p.is_pipeline_idle(), "Pipeline should return to IDLE after save")


func test_load_restores_domain_state() -> void:
	# Arrange — first save some state
	var p = _make_persistence()

	p.register_domain_serializer(&"resources", func() -> SnapshotPackage:
		var pkg := SnapshotPkgScript.new()
		pkg.domain_id = &"resources"
		pkg.snapshot_schema_version = 1
		pkg.content_domain_versions = {"resources": "1.0"}
		pkg.stable_id_refs = [&"resource.repair_kit"]
		pkg.payload = {"storage": {"resource.repair_kit": 25, "resource.cloud_coin": 100}}
		pkg.domain_state = SnapshotPkgScript.DomainState.READY
		return pkg
	)

	p.request_save_progress()
	assert(p.get_current_generation() == 1, "Generation should be 1 after save")

	# Register a deserializer that captures restored state
	p.register_domain_deserializer(&"resources", func(snapshot: SnapshotPackage) -> void:
		_restored_resources = snapshot.payload.duplicate()
	)

	# Act — load
	p.load_completed.connect(func(_artifact, _gen):
		_load_completed = true
	)
	p.request_load_progress()

	# Assert
	assert(_load_completed, "Load should complete successfully")
	assert(_restored_resources.has("storage"), "Restored resources should have storage")
	assert(_restored_resources.storage["resource.repair_kit"] == 25,
		"Repair kit count should be 25 after restore, got %s" % str(_restored_resources.storage.get("resource.repair_kit")))
	assert(_restored_resources.storage["resource.cloud_coin"] == 100,
		"Cloud coin should be 100 after restore, got %s" % str(_restored_resources.storage.get("resource.cloud_coin")))


func test_save_then_load_state_identity() -> void:
	# Arrange — full roundtrip: save state A → load → verify state A
	var p = _make_persistence()

	var original_storage := {"resource.repair_kit": 8, "resource.basic_supply": 20, "resource.beacon_crystal": 5}
	var original_carried := {"resource.navigation_chart": 2}

	p.register_domain_serializer(&"resources", func() -> SnapshotPackage:
		var pkg := SnapshotPkgScript.new()
		pkg.domain_id = &"resources"
		pkg.snapshot_schema_version = 1
		pkg.content_domain_versions = {"resources": "1.0"}
		pkg.stable_id_refs = [&"resource.repair_kit", &"resource.basic_supply", &"resource.beacon_crystal", &"resource.navigation_chart"]
		pkg.payload = {"storage": original_storage, "carried": original_carried}
		pkg.domain_state = SnapshotPkgScript.DomainState.READY
		return pkg
	)

	# Act — save
	p.request_save_progress()

	# Capture restored state
	var restored_payload: Dictionary = {}
	p.register_domain_deserializer(&"resources", func(snapshot: SnapshotPackage) -> void:
		restored_payload = snapshot.payload.duplicate()
	)

	# Act — load
	p.request_load_progress()

	# Assert — state identity (every key/value preserved)
	assert(restored_payload.has("storage"), "Restored payload must have storage")
	assert(restored_payload.has("carried"), "Restored payload must have carried")
	for key: String in original_storage:
		assert(restored_payload.storage.get(key) == original_storage[key],
			"Storage '%s' mismatch: expected %d, got %s" % [key, original_storage[key], str(restored_payload.storage.get(key))])
	for key: String in original_carried:
		assert(restored_payload.carried.get(key) == original_carried[key],
			"Carried '%s' mismatch: expected %d, got %s" % [key, original_carried[key], str(restored_payload.carried.get(key))])


func test_empty_load_gracefully_fails() -> void:
	# Arrange — fresh Persistence with no saved data
	var p = _make_persistence()

	var load_failed := false
	var fail_reason := ""
	p.load_failed.connect(func(reason: String, _d):
		load_failed = true
		fail_reason = reason
	)

	# Act
	p.request_load_progress()

	# Assert — load gracefully fails (no data, not a crash)
	assert(load_failed, "Load should fail when no save data exists")
	assert(fail_reason == "no_safe_data", "Reason should be no_safe_data, got: %s" % fail_reason)


func test_multiple_domain_save_roundtrip() -> void:
	# Arrange — two domains (resources + repair) saved and restored together
	var p = _make_persistence()

	p.register_domain_serializer(&"resources", func() -> SnapshotPackage:
		var pkg := SnapshotPkgScript.new()
		pkg.domain_id = &"resources"
		pkg.snapshot_schema_version = 1
		pkg.content_domain_versions = {"resources": "1.0"}
		pkg.payload = {"storage": {"resource.repair_kit": 10}}
		pkg.domain_state = SnapshotPkgScript.DomainState.READY
		return pkg
	)

	p.register_domain_serializer(&"repair", func() -> SnapshotPackage:
		var pkg := SnapshotPkgScript.new()
		pkg.domain_id = &"repair"
		pkg.snapshot_schema_version = 1
		pkg.content_domain_versions = {"repair": "1.0"}
		pkg.payload = {"completed_nodes": ["repair_node.starlight_dock"]}
		pkg.domain_state = SnapshotPkgScript.DomainState.READY
		return pkg
	)

	# Act — save
	p.request_save_progress()

	# Capture restored state for both domains
	var r_resources: Dictionary = {}
	var r_repair: Dictionary = {}
	p.register_domain_deserializer(&"resources", func(s: SnapshotPackage): r_resources = s.payload.duplicate())
	p.register_domain_deserializer(&"repair", func(s: SnapshotPackage): r_repair = s.payload.duplicate())

	p.request_load_progress()

	# Assert — both domains restored correctly
	assert(r_resources.storage["resource.repair_kit"] == 10, "Resources domain mismatch")
	assert(r_repair.completed_nodes[0] == "repair_node.starlight_dock", "Repair domain mismatch")


## Helpers

func _make_persistence() -> Persistence:
	var p := PersistenceScript.new()
	p._ready()
	return p

func before() -> void:
	_save_completed = false
	_load_completed = false
	_saved_generation = 0
	_restored_resources = {}
	_restored_repair = {}

func after() -> void:
	_save_completed = false
	_load_completed = false
	_saved_generation = 0
	_restored_resources = {}
	_restored_repair = {}
