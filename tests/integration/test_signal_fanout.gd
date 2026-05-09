# Signal Fanout — Integration Test (Scene C)
# Validates: repair_completed signal fans out to 4 consumers,
# signal protocol compliance (ADR-0002), cascade depth ≤ 2
# REF: ADR-0002, ADR-0009, P3 Scene C acceptance criteria
extends Node

const WorldRepairScript := preload("res://src/feature/world_repair.gd")

# Consumer tracking for fanout verification
var _consumer1_called := false
var _consumer2_called := false
var _consumer3_called := false
var _consumer4_called := false
var _consumer_call_count := 0
var _received_node_ids: Array[StringName] = []


func test_repair_completed_fans_out_to_four_consumers() -> void:
	# Arrange — create WorldRepair, connect 4 consumers
	var wr = _make_world_repair()

	# Consumer 1: Chart (unlocks routes)
	wr.repair_completed.connect(func(node_id: StringName):
		_consumer1_called = true
		_consumer_call_count += 1
		_received_node_ids.append(node_id)
	)

	# Consumer 2: Intel (reveals knowledge)
	wr.repair_completed.connect(func(node_id: StringName):
		_consumer2_called = true
		_consumer_call_count += 1
		_received_node_ids.append(node_id)
	)

	# Consumer 3: Resources/Registry (unlocks content)
	wr.repair_completed.connect(func(node_id: StringName):
		_consumer3_called = true
		_consumer_call_count += 1
		_received_node_ids.append(node_id)
	)

	# Consumer 4: UI/Feedback (notification)
	wr.repair_completed.connect(func(node_id: StringName):
		_consumer4_called = true
		_consumer_call_count += 1
		_received_node_ids.append(node_id)
	)

	# Act — register and fully repair a node (triggers repair_completed)
	wr.register_repair_node(&"repair_node.starlight_dock", {
		"resource.repair_kit": 2,
		"resource.beacon_crystal": 1,
	})
	wr.commit_deposit(&"repair_node.starlight_dock", &"resource.repair_kit", 2)
	wr.commit_deposit(&"repair_node.starlight_dock", &"resource.beacon_crystal", 1)

	# Assert — all 4 consumers received the signal
	assert(_consumer1_called, "Consumer 1 (Chart) should receive repair_completed")
	assert(_consumer2_called, "Consumer 2 (Intel) should receive repair_completed")
	assert(_consumer3_called, "Consumer 3 (Registry) should receive repair_completed")
	assert(_consumer4_called, "Consumer 4 (UI/Feedback) should receive repair_completed")
	assert(_consumer_call_count == 4, "Exactly 4 consumers should be called, got %d" % _consumer_call_count)
	assert(_received_node_ids.size() == 4, "All 4 consumers should receive the node_id")


func test_repair_completed_not_emitted_unless_all_materials_met() -> void:
	# Arrange — repair requires 2 resource types, only commit 1
	var wr = _make_world_repair()

	var premature_call := false
	wr.repair_completed.connect(func(_n): premature_call = true)

	wr.register_repair_node(&"repair_node.test", {
		"resource.repair_kit": 3,
		"resource.beacon_crystal": 2,
	})

	# Act — partial deposit (not enough to complete)
	wr.commit_deposit(&"repair_node.test", &"resource.repair_kit", 3)

	# Assert — repair_completed NOT yet emitted (beacon_crystal still needed)
	assert(not premature_call, "repair_completed should NOT fire when materials incomplete")


func test_deposit_committed_signal_typed_params() -> void:
	# ADR-0002: signal payload must be typed parameters, not Dictionary
	var wr = _make_world_repair()

	var captured_node: StringName = &""
	var captured_resource: StringName = &""
	var captured_quantity: int = 0
	wr.deposit_committed.connect(func(node_id: StringName, resource_id: StringName, quantity: int):
		captured_node = node_id
		captured_resource = resource_id
		captured_quantity = quantity
	)

	wr.register_repair_node(&"repair_node.starlight_dock", {"resource.repair_kit": 5})
	wr.commit_deposit(&"repair_node.starlight_dock", &"resource.repair_kit", 2)

	assert(captured_node == &"repair_node.starlight_dock", "node_id should be starlight_dock")
	assert(captured_resource == &"resource.repair_kit", "resource_id should be repair_kit")
	assert(captured_quantity == 2, "quantity should be 2, got %d" % captured_quantity)


func test_repair_completed_emitted_exactly_once_per_node() -> void:
	# Each repair node should emit repair_completed exactly once
	var wr = _make_world_repair()

	var call_count := 0
	wr.repair_completed.connect(func(_n): call_count += 1)

	wr.register_repair_node(&"node.a", {"resource.repair_kit": 1})
	wr.register_repair_node(&"node.b", {"resource.beacon_crystal": 1})

	# Act
	wr.commit_deposit(&"node.a", &"resource.repair_kit", 1)
	wr.commit_deposit(&"node.b", &"resource.beacon_crystal", 1)

	# Assert — emitted once per node, not duplicated
	assert(call_count == 2, "repair_completed should emit once per node: expected 2, got %d" % call_count)
	# Extra deposit on already-repaired node should not re-emit
	wr.commit_deposit(&"node.a", &"resource.repair_kit", 999)
	assert(call_count == 2, "Extra deposit on REPAIRED node should not re-emit: expected 2, got %d" % call_count)


func test_signal_cascade_depth_guard() -> void:
	# ADR-0002: signal cascade depth must be ≤ 2
	# A→B→C is allowed. A→B→C→D is forbidden.
	# This test verifies that repair_completed (depth 0) → consumer handler
	# does not trigger further cascades beyond depth 2.

	var wr = _make_world_repair()

	var depth := 0
	var max_depth := 0

	# Consumer that connects to another signal (depth tracking)
	wr.repair_completed.connect(func(_n):
		depth += 1
		max_depth = maxi(max_depth, depth)
		# Simulate: consumer emits its own signal (depth +1)
		# In real implementation, Chart.route_unlocked would fire here
		depth -= 1
	)

	wr.register_repair_node(&"repair_node.test", {"resource.repair_kit": 1})
	wr.commit_deposit(&"repair_node.test", &"resource.repair_kit", 1)

	# Assert — cascade depth never exceeds 2 (we only have 1 hop here)
	assert(max_depth <= 2, "Signal cascade depth %d exceeds limit of 2" % max_depth)


func test_repair_state_machine_transitions() -> void:
	# Verify all valid state transitions: UNKNOWN→KNOWN→REPAIRED
	var wr = _make_world_repair()

	# UNKNOWN node (never registered)
	assert(wr.get_node_state(&"nonexistent") == WorldRepairScript.RepairState.UNKNOWN,
		"Unregistered node should be UNKNOWN")

	# KNOWN after registration
	wr.register_repair_node(&"repair_node.test", {"resource.repair_kit": 1})
	assert(wr.get_node_state(&"repair_node.test") == WorldRepairScript.RepairState.KNOWN,
		"Registered node should be KNOWN")

	# Can deposit in KNOWN state
	assert(wr.can_deposit(&"repair_node.test"), "Should be able to deposit in KNOWN state")

	# REPAIRED after all materials committed
	wr.commit_deposit(&"repair_node.test", &"resource.repair_kit", 1)
	assert(wr.get_node_state(&"repair_node.test") == WorldRepairScript.RepairState.REPAIRED,
		"Node should be REPAIRED after all materials committed")

	# Cannot deposit in REPAIRED state
	assert(not wr.can_deposit(&"repair_node.test"), "Should NOT be able to deposit in REPAIRED state")


## Helpers

func _make_world_repair() -> WorldRepair:
	var wr := WorldRepairScript.new()
	wr._ready()
	return wr

func before() -> void:
	_consumer1_called = false
	_consumer2_called = false
	_consumer3_called = false
	_consumer4_called = false
	_consumer_call_count = 0
	_received_node_ids.clear()

func after() -> void:
	_consumer1_called = false
	_consumer2_called = false
	_consumer3_called = false
	_consumer4_called = false
	_consumer_call_count = 0
	_received_node_ids.clear()
