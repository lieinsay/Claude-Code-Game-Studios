## SnapshotPackage — RefCounted data class for domain save state
##
## Typed container for a single domain's save data.
## Used by Persistence to collect, validate, and restore state.
## REF: ADR-0003, control-manifest.md Required pattern.

class_name SnapshotPackage
extends RefCounted

enum DomainState {
	READY,
	BLOCKED,
	NOT_READY,
	SETTLING,
}

var domain_id: StringName = &""
var snapshot_schema_version: int = 1
var content_domain_versions: Dictionary = {}
var stable_id_refs: Array[StringName] = []
var payload: Dictionary = {}
var domain_state: int = DomainState.NOT_READY
var domain_error_code: String = ""
var migration_hint: String = ""

func is_valid() -> bool:
	if domain_id.is_empty():
		return false
	if snapshot_schema_version <= 0:
		return false
	if content_domain_versions.is_empty():
		return false
	if domain_state != DomainState.READY:
		return false
	if not domain_error_code.is_empty():
		return false
	return true

func to_dict() -> Dictionary:
	return {
		"domain_id": domain_id,
		"snapshot_schema_version": snapshot_schema_version,
		"content_domain_versions": content_domain_versions,
		"stable_id_refs": stable_id_refs,
		"payload": payload,
		"domain_state": domain_state,
		"domain_error_code": domain_error_code,
		"migration_hint": migration_hint,
	}

static func from_dict(data: Dictionary) -> SnapshotPackage:
	var pkg := SnapshotPackage.new()
	pkg.domain_id = data.get("domain_id", &"")
	pkg.snapshot_schema_version = data.get("snapshot_schema_version", 0)
	pkg.content_domain_versions = data.get("content_domain_versions", {})
	pkg.stable_id_refs = data.get("stable_id_refs", [])
	pkg.payload = data.get("payload", {})
	pkg.domain_state = data.get("domain_state", DomainState.NOT_READY)
	pkg.domain_error_code = data.get("domain_error_code", "")
	pkg.migration_hint = data.get("migration_hint", "")
	return pkg
