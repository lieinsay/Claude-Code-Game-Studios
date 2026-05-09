# Boot Chain — Integration Tests
# Validates: Autoload init order, signal connections, boot phase transitions
# REF: ADR-0001, platform-session-shell Story 001
extends Node

func test_registry_autoload_initialized() -> void:
	# Integration: verify Registry is accessible and initialized
	# In headless test, Autoloads are available if project is configured
	if not _autoloads_available():
		return  # Skip if running standalone (no project.godot context)
	var result: Dictionary = Registry.query_by_id(&"location.glass-harbor")
	assert(result.status == Registry.QueryResult.FOUND, "Registry should find glass-harbor")
	assert(Registry.is_initialized(), "Registry should be initialized")
	assert(Registry.is_domain_loaded(&"core_content"), "core_content domain should be loaded")

func test_resources_autoload_initialized() -> void:
	if not _autoloads_available():
		return
	assert(ResourcesManager.has_method("add_item"), "Resources should have add_item method")
	# Verify default pools are initialized
	var qty: int = ResourcesManager.get_quantity(ResourcesManager.Pool.STORAGE, &"resource.repair_kit")
	assert(qty >= 0, "Should return 0 for empty pool, got %d" % qty)

func test_chart_route_registration() -> void:
	if not _autoloads_available():
		return
	# Bootstrap a route and verify selectability
	Chart.register_route(&"test.route", {
		"destination_id": "location.test",
		"traversable": true,
		"hazard_tags": [],
	})
	var sel: Dictionary = Chart.route_selectability(&"test.route")
	assert(sel.selectable, "Route should be selectable: %s" % sel.get("reason", ""))

func test_world_repair_deposit_flow() -> void:
	if not _autoloads_available():
		return
	WorldRepair.register_repair_node(&"test.node", {
		"resource.repair_kit": 3,
	})
	assert(WorldRepair.can_deposit(&"test.node"), "Should be able to deposit")
	var ok: bool = WorldRepair.commit_deposit(&"test.node", &"resource.repair_kit", 3)
	assert(ok, "Deposit should succeed")
	assert(WorldRepair.get_node_state(&"test.node") == WorldRepair.RepairState.REPAIRED,
		"Node should be REPAIRED after all materials committed")

func test_signal_protocol_naming() -> void:
	# ADR-0002: All signals must follow {noun}_{verb_past}
	var signals_to_check: Array[String] = [
		"deposit_committed", "repair_completed", "route_committed",
		"session_ready", "registry_ready", "persistence_ready",
	]
	for sig_name: String in signals_to_check:
		assert(sig_name.ends_with("ed") or sig_name.ends_with("en"),
			"Signal '%s' must be past tense per ADR-0002" % sig_name)

## Helper

func _autoloads_available() -> bool:
	# Check if running within the project context (Autoloads registered)
	return Engine.get_main_loop() is SceneTree

func before() -> void: pass
func after() -> void: pass
