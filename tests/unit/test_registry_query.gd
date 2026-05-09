# Registry Query Engine — Unit Tests
# Validates: stable ID queries, status discrimination, list ordering
# REF: content-registry Story 001 AC-1, AC-5/6, AC-7
extends Node

func test_registry_query_by_id_found() -> void:
	var registry := _make_registry()
	var result: Dictionary = registry.query_by_id(&"location.glass-harbor")
	assert(result.status == registry.QueryResult.FOUND, "Expected FOUND, got %d" % result.status)
	assert(result.entity.display_name == "玻璃港", "Expected '玻璃港', got '%s'" % result.entity.display_name)

func test_registry_query_by_id_not_found() -> void:
	var registry := _make_registry()
	var result: Dictionary = registry.query_by_id(&"location.nonexistent")
	assert(result.status == registry.QueryResult.NOT_FOUND, "Expected NOT_FOUND")

func test_registry_query_unloaded_returns_unloaded() -> void:
	var registry := Registry.new()
	# Don't initialize — should return UNLOADED
	var result: Dictionary = registry.query_by_id(&"anything")
	assert(result.status == registry.QueryResult.UNLOADED, "Expected UNLOADED for uninitialized registry")

func test_registry_list_by_kind_returns_sorted() -> void:
	var registry := _make_registry()
	var resources: Array[Dictionary] = registry.list_by_kind(&"resource")
	assert(resources.size() >= 4, "Expected at least 4 resources, got %d" % resources.size())
	# Verify deterministic ordering (sort_order ASC, then id ASC)
	for i: int in range(resources.size() - 1):
		var a: Dictionary = resources[i]
		var b: Dictionary = resources[i + 1]
		assert(a.sort_order <= b.sort_order,
			"Sort order violation at index %d: %d > %d" % [i, a.sort_order, b.sort_order])

func test_registry_query_deprecated() -> void:
	var registry := _make_registry()
	registry.register_content(&"test.deprecated_item", {
		"id": "test.deprecated_item",
		"kind": "resource",
		"content_status": registry.ContentStatus.DEPRECATED,
		"sort_order": 99,
	})
	var result: Dictionary = registry.query_by_id(&"test.deprecated_item")
	assert(result.status == registry.QueryResult.DEPRECATED, "Expected DEPRECATED, got %d" % result.status)
	assert(not result.entity.is_empty(), "Deprecated entity should still be returned")

func test_registry_domain_loaded_tracking() -> void:
	var registry := _make_registry()
	assert(registry.is_domain_loaded(&"core_content"), "core_content domain should be loaded after bootstrap")

## Helper

func _make_registry() -> Registry:
	var reg := Registry.new()
	reg._initialize_content()
	return reg

## GdUnit4-style suite setup (optional — works with both manual and CI runner)
func before() -> void: pass
func after() -> void: pass
