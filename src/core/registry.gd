## Registry — Autoload #1 (Foundation)
##
## Static content catalog. Owns stable IDs, schemas, controlled vocabularies,
## reference integrity, and read-only query contracts.
##
## Boot order: loaded first — all other systems depend on stable IDs.
## Phase 2: loads static content, emits registry_ready.

extends Node

# Content lifecycle states
enum ContentStatus {
	DRAFT,
	ACTIVE,
	DEPRECATED,
	RETIRED,
}

# Query result discrimination
enum QueryResult {
	FOUND,
	NOT_FOUND,
	UNLOADED,
	DEPRECATED,
	VERSION_INCOMPATIBLE,
}

# Core storage: Dictionary[StringName, Dictionary]
var _content: Dictionary[StringName, Dictionary] = {}
var _domain_loaded: Dictionary[StringName, bool] = {}
var _initialized: bool = false

## Signals

# Emitted when Registry finishes loading static content
signal registry_ready()
# Emitted when a specific domain finishes loading
signal domain_ready(domain: StringName)

func _ready() -> void:
	_initialize_content()

func _initialize_content() -> void:
	RegistryBootstrap.bootstrap(self)
	_initialized = true

## Public API

func query_by_id(entity_id: StringName) -> Dictionary:
	if not _initialized:
		return {"status": QueryResult.UNLOADED, "entity": null, "error": "registry_not_initialized"}
	var entity: Dictionary = _content.get(entity_id, {})
	if entity.is_empty():
		return {"status": QueryResult.NOT_FOUND, "entity": null, "error": "id_not_found"}
	var ent_status: int = entity.get("content_status", ContentStatus.DRAFT)
	match ent_status:
		ContentStatus.DEPRECATED:
			return {"status": QueryResult.DEPRECATED, "entity": entity, "error": "entity_deprecated"}
		ContentStatus.RETIRED:
			return {"status": QueryResult.NOT_FOUND, "entity": null, "error": "entity_retired"}
	return {"status": QueryResult.FOUND, "entity": entity, "error": null}

func list_by_kind(kind: StringName) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for id: StringName in _content:
		var entity: Dictionary = _content[id]
		if entity.get("kind", "") == kind and entity.get("content_status", ContentStatus.DRAFT) <= ContentStatus.ACTIVE:
			result.append(entity)
	result.sort_custom(_sort_by_order_and_id)
	return result

func register_content(entity_id: StringName, definition: Dictionary) -> void:
	_content[entity_id] = definition

func is_domain_loaded(domain: StringName) -> bool:
	return _domain_loaded.get(domain, false)

func set_domain_loaded(domain: StringName) -> void:
	_domain_loaded[domain] = true
	domain_ready.emit(domain)

func is_initialized() -> bool:
	return _initialized

static func _sort_by_order_and_id(a: Dictionary, b: Dictionary) -> bool:
	var order_a: int = a.get("sort_order", 0)
	var order_b: int = b.get("sort_order", 0)
	if order_a != order_b:
		return order_a < order_b
	return str(a.get("id", "")) < str(b.get("id", ""))
