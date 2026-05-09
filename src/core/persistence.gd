## Persistence — Autoload #2 (Foundation)
##
## Save/load orchestration. Owns serialization, deserialization, version
## migration, and the staging→verify→promotion pipeline using Canonical JSON.
##
## Does NOT own runtime state — domain systems own their state.
## Persistence serializes and restores it.
## REF: ADR-0003, control-manifest.md

extends Node

# Domain state for snapshot readiness
enum DomainState {
	READY,
	BLOCKED,
	NOT_READY,
	SETTLING,
}

# Pipeline phase
enum PipelinePhase {
	IDLE,
	COLLECTING,
	WRITING_STAGING,
	VERIFYING,
	PROMOTING,
	ABORTING,
}

# Artifact kind
enum ArtifactKind {
	PROGRESS,
	SETTINGS,
}

## Signals

signal persistence_ready()
signal save_completed(generation: int)
signal save_failed(reason: String, phase: String)
signal promotion_completed(artifact: StringName, generation: int)
signal load_completed(artifact: StringName, generation: int)
signal load_failed(reason: String, domain: StringName)

var _pipeline_phase: int = PipelinePhase.IDLE
var _current_generation: int = 0
var _safe_pointer: String = ""
var _domain_serializers: Dictionary[StringName, Callable] = {}
var _domain_deserializers: Dictionary[StringName, Callable] = {}
# In-memory staging for P3 prototype (production uses FileAccess)
var _staging_data: Dictionary = {}
var _safe_data: Dictionary = {}
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

## Public API

func register_domain_serializer(domain_id: StringName, serializer: Callable) -> void:
	_domain_serializers[domain_id] = serializer

func register_domain_deserializer(domain_id: StringName, deserializer: Callable) -> void:
	_domain_deserializers[domain_id] = deserializer

func request_save_progress() -> void:
	if _pipeline_phase != PipelinePhase.IDLE:
		save_failed.emit("pipeline_busy", "request_save")
		return
	_collect_and_save(ArtifactKind.PROGRESS)

func request_load_progress() -> void:
	if not _safe_data.is_empty():
		_restore_domains(_safe_data.get("domains", {}))
		load_completed.emit(&"progress", _current_generation)
		return
	load_failed.emit("no_safe_data", &"progress")

func get_current_generation() -> int:
	return _current_generation

func is_pipeline_idle() -> bool:
	return _pipeline_phase == PipelinePhase.IDLE

## Staging → Verify → Promotion pipeline

func _collect_and_save(artifact: int) -> void:
	_pipeline_phase = PipelinePhase.COLLECTING

	# Step 1: Collect SnapshotPackages from all registered domains
	var manifest: Dictionary = {
		"generation": _current_generation + 1,
		"artifact": artifact,
		"timestamp": Time.get_unix_time_from_system(),
		"schema_version": 1,
		"domains": {},
	}

	for domain_id: StringName in _domain_serializers:
		var serializer: Callable = _domain_serializers[domain_id]
		if not serializer.is_valid():
			save_failed.emit("invalid_serializer", "collect")
			_pipeline_phase = PipelinePhase.IDLE
			return
		var snapshot: SnapshotPackage = serializer.call()
		if not snapshot.is_valid():
			save_failed.emit("invalid_snapshot:%s" % domain_id, "collect")
			_pipeline_phase = PipelinePhase.IDLE
			return
		manifest.domains[domain_id] = snapshot.to_dict()

	# Step 2: Encode to Canonical JSON, compute SHA-256
	_pipeline_phase = PipelinePhase.WRITING_STAGING
	var encoded: String = _canonical_json_encode(manifest)

	# Step 3: Write staging (in-memory for prototype)
	_staging_data = manifest
	var checksum: String = _compute_checksum(encoded)
	manifest["_checksum"] = checksum

	# Step 4: Verify (readback + checksum + schema)
	_pipeline_phase = PipelinePhase.VERIFYING
	var re_encoded: String = _canonical_json_encode(manifest)
	var re_checksum: String = _compute_checksum(re_encoded)
	if re_checksum != checksum:
		_pipeline_phase = PipelinePhase.ABORTING
		save_failed.emit("checksum_mismatch", "verify")
		_pipeline_phase = PipelinePhase.IDLE
		return

	# Step 5: Promote
	_pipeline_phase = PipelinePhase.PROMOTING
	_safe_data = manifest
	_current_generation = manifest.generation
	_pipeline_phase = PipelinePhase.IDLE
	save_completed.emit(_current_generation)
	promotion_completed.emit(&"progress", _current_generation)

func _restore_domains(domains: Dictionary) -> void:
	for domain_id: StringName in domains:
		if _domain_deserializers.has(domain_id):
			var deserializer: Callable = _domain_deserializers[domain_id]
			if deserializer.is_valid():
				var snapshot: SnapshotPackage = SnapshotPackage.from_dict(domains[domain_id])
				deserializer.call(snapshot)

## Canonical JSON encoding (ADR-0003)

func _canonical_json_encode(data: Dictionary) -> String:
	# Godot JSON.stringify does not guarantee key ordering.
	# Pre-sort keys recursively for canonical output.
	var sorted: Dictionary = _sort_dict_keys(data)
	return JSON.stringify(sorted, "", false)

func _sort_dict_keys(d: Dictionary) -> Dictionary:
	var result := {}
	var keys: Array[String] = []
	for key: String in d.keys():
		keys.append(key)
	keys.sort()
	for key: String in keys:
		var value = d[key]
		if value is Dictionary:
			result[key] = _sort_dict_keys(value)
		elif value is float:
			# Canonical: -0.0 → 0.0, NaN/Inf → null
			if is_nan(value) or is_inf(value):
				result[key] = null
			elif value == 0.0:
				result[key] = 0.0
			else:
				result[key] = value
		else:
			result[key] = value
	return result

## SHA-256 checksum (ADR-0003)

func _compute_checksum(data: String) -> String:
	return data.sha256_text()
