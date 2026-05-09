## WorldRepair — Autoload #7 (Feature)
##
## Sole Feature-layer Autoload exception.
## Owns repair conditions, state changes, and unlock results.
## repair_completed signal consumed by 4 cross-layer systems.

extends Node

enum RepairState {
	UNKNOWN,
	KNOWN,
	MATERIALS_COMMITTED,
	REPAIRING,
	REPAIRED,
}

## Signals

signal repair_completed(node_id: StringName)
signal repair_failed(node_id: StringName, reason: String)
signal deposit_committed(node_id: StringName, resource_id: StringName, quantity: int)

var _repair_nodes: Dictionary = {}
var _completed_node_ids: Array[StringName] = []
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

## Public API

func register_repair_node(node_id: StringName, requirements: Dictionary) -> void:
	_repair_nodes[node_id] = {
		"state": RepairState.KNOWN,
		"requirements": requirements,
		"deposited": {},
	}

func can_deposit(node_id: StringName) -> bool:
	if not _repair_nodes.has(node_id):
		return false
	var node: Dictionary = _repair_nodes[node_id]
	return node.state < RepairState.REPAIRED

func commit_deposit(node_id: StringName, resource_id: StringName, quantity: int) -> bool:
	if not can_deposit(node_id):
		repair_failed.emit(node_id, "cannot_deposit")
		return false
	var node: Dictionary = _repair_nodes[node_id]
	if not node.deposited.has(resource_id):
		node.deposited[resource_id] = 0
	node.deposited[resource_id] += quantity
	deposit_committed.emit(node_id, resource_id, quantity)
	# Check if all requirements met
	var all_met: bool = true
	for req_id: StringName in node.requirements:
		var required: int = node.requirements[req_id]
		var deposited: int = node.deposited.get(req_id, 0)
		if deposited < required:
			all_met = false
			break
	if all_met:
		node.state = RepairState.REPAIRED
		_completed_node_ids.append(node_id)
		repair_completed.emit(node_id)
	return true

func get_completed_nodes() -> Array[StringName]:
	return _completed_node_ids

func get_node_state(node_id: StringName) -> int:
	var node: Dictionary = _repair_nodes.get(node_id, {})
	return node.get("state", RepairState.UNKNOWN)
