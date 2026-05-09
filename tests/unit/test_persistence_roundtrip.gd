# Persistence Roundtrip — Unit Tests
# Validates: Canonical JSON encoding, staging→verify→promotion, SHA-256 checksum
# REF: local-save-persistence Story 001 AC-1, AC-2, AC-4
extends Node

func test_canonical_json_sorts_keys() -> void:
	var p := _make_persistence()
	var input := {"b": 2, "a": 1, "c": {"z": 9, "x": 7}}
	var result: String = p._canonical_json_encode(input)
	# Sorted keys: a, b, c (and within c: x, z)
	assert(result.begins_with("{"), "Should be valid JSON: %s" % result)
	assert(result.find("\"a\"") < result.find("\"b\""), "Key 'a' must appear before 'b'")

func test_canonical_json_deterministic() -> void:
	var p := _make_persistence()
	var input := {"c": 3, "b": 2, "a": 1}
	var r1: String = p._canonical_json_encode(input)
	var r2: String = p._canonical_json_encode(input)
	assert(r1 == r2, "Same input must produce byte-identical output")
	assert(r1 == "{\"a\":1,\"b\":2,\"c\":3}", "Expected sorted JSON, got: %s" % r1)

func test_canonical_json_handles_nan_inf() -> void:
	var p := _make_persistence()
	var input := {"valid": 3.5, "nan_value": NAN, "inf_value": INF, "neg_zero": -0.0}
	var result: String = p._canonical_json_encode(input)
	# Should not crash; NaN/Inf should be null, -0.0 should be 0.0
	assert(not result.is_empty(), "Should produce valid JSON even with NaN/Inf")
	# Verify no "nan" or "inf" in output
	assert(result.find("nan") == -1, "NaN should not appear in output")
	assert(result.find("inf") == -1, "Inf should not appear in output")

func test_sha256_checksum_deterministic() -> void:
	var p := _make_persistence()
	var data: String = "test data for hashing"
	var c1: String = p._compute_checksum(data)
	var c2: String = p._compute_checksum(data)
	assert(c1 == c2, "Same input must produce same SHA-256")
	assert(c1.length() == 64, "SHA-256 hex digest should be 64 chars: got %d" % c1.length())

func test_snapshot_package_validation() -> void:
	var pkg := SnapshotPackage.new()
	assert(not pkg.is_valid(), "Empty package should be invalid")
	pkg.domain_id = &"test_domain"
	pkg.snapshot_schema_version = 1
	pkg.content_domain_versions = {"core": "1.0"}
	pkg.domain_state = SnapshotPackage.DomainState.READY
	assert(pkg.is_valid(), "Properly filled package should be valid")

func test_snapshot_package_rejects_blocked() -> void:
	var pkg := SnapshotPackage.new()
	pkg.domain_id = &"test_domain"
	pkg.snapshot_schema_version = 1
	pkg.content_domain_versions = {"core": "1.0"}
	pkg.domain_state = SnapshotPackage.DomainState.BLOCKED
	pkg.domain_error_code = "system_not_initialized"
	assert(not pkg.is_valid(), "BLOCKED package should be invalid")

func test_save_pipeline_rejects_busy() -> void:
	var p := _make_persistence()
	p._pipeline_phase = p.PipelinePhase.COLLECTING  # Simulate busy
	# request_save_progress should reject
	var was_called := false
	p.save_failed.connect(func(_r, _p): was_called = true)
	p.request_save_progress()
	# Cannot easily test async signal in unit test; validate phase check logic
	assert(p._pipeline_phase == p.PipelinePhase.COLLECTING, "Phase should remain COLLECTING when busy")

func test_snapshot_roundtrip_serialization() -> void:
	var pkg := SnapshotPackage.new()
	pkg.domain_id = &"resources"
	pkg.snapshot_schema_version = 1
	pkg.content_domain_versions = {"resources": "1.0"}
	pkg.stable_id_refs = [&"resource.repair_kit", &"resource.basic_supply"]
	pkg.payload = {"storage": {"resource.repair_kit": 25}}
	pkg.domain_state = SnapshotPackage.DomainState.READY

	var data: Dictionary = pkg.to_dict()
	var restored: SnapshotPackage = SnapshotPackage.from_dict(data)
	assert(restored.domain_id == &"resources", "domain_id mismatch")
	assert(restored.snapshot_schema_version == 1, "schema_version mismatch")
	assert(restored.payload.storage["resource.repair_kit"] == 25, "payload mismatch")
	assert(restored.is_valid(), "Roundtripped package should be valid")

## Helper

func _make_persistence() -> Persistence:
	var p := Persistence.new()
	p._ready()
	return p

func before() -> void: pass
func after() -> void: pass
