## ResourcesManager — Autoload #4 (Core)
##
## Autoload name: "Resources"
## Owns 6 resource pools (carried, storage, repair, supply, currency, cargo).
## Shared by Hub, Exploration, and Settlement.

class_name ResourcesManager
extends Node

# Supply class → max_stack mapping
const SUPPLY_CLASS_CAPACITY := {
	&"basic": 99,
	&"repair": 99,
	&"navigation": 20,
	&"local_specialty": 10,
	&"intel": 1,
}

# Pool identifiers
enum Pool {
	CARRIED,
	STORAGE,
	REPAIR,
	SUPPLY,
	CURRENCY,
	CARGO,
}

## Signals

signal resource_added(pool: int, resource_id: StringName, quantity: int)
signal resource_removed(pool: int, resource_id: StringName, quantity: int)
signal resource_changed(pool: int, resource_id: StringName, new_quantity: int)

var _pools: Dictionary[int, Dictionary] = {}
var _initialized: bool = false

func _ready() -> void:
	for pool: int in range(6):
		_pools[pool] = {}
	_initialized = true

## Public API

func add_item(pool: int, resource_id: StringName, quantity: int, stack_rule: StringName = &"stackable") -> int:
	# Returns: quantity actually added (may be less if capacity-constrained)
	if not _initialized:
		return 0
	var pool_dict: Dictionary = _pools[pool]
	if stack_rule == &"unique":
		if not pool_dict.has(resource_id):
			pool_dict[resource_id] = 1
			resource_added.emit(pool, resource_id, 1)
			return 1
		return 0
	# Stackable: implement "fill fullest first" merge
	var max_stack: int = _get_max_stack(resource_id)
	var remaining: int = quantity
	# Fill existing stacks first (fullest first)
	var existing_stacks: Array = []
	for rid: StringName in pool_dict:
		if rid == resource_id:
			existing_stacks.append({"id": rid, "qty": pool_dict[rid]})
	existing_stacks.sort_custom(func(a, b): return a.qty > b.qty)
	for stack: Dictionary in existing_stacks:
		if remaining <= 0:
			break
		var space: int = max_stack - stack.qty
		if space > 0:
			var to_add: int = mini(remaining, space)
			pool_dict[resource_id] += to_add
			remaining -= to_add
	# Create new stacks for overflow
	while remaining > 0:
		var new_stack_qty: int = mini(remaining, max_stack)
		# For simplicity in prototype, merge into single stack
		if pool_dict.has(resource_id):
			pool_dict[resource_id] += new_stack_qty
		else:
			pool_dict[resource_id] = new_stack_qty
		remaining -= new_stack_qty
	var added: int = quantity - remaining
	if added > 0:
		resource_added.emit(pool, resource_id, added)
	return added

func remove_item(pool: int, resource_id: StringName, quantity: int) -> int:
	# Returns: quantity actually removed
	var pool_dict: Dictionary = _pools[pool]
	if not pool_dict.has(resource_id):
		return 0
	var current: int = pool_dict[resource_id]
	var removed: int = mini(quantity, current)
	pool_dict[resource_id] -= removed
	if pool_dict[resource_id] <= 0:
		pool_dict.erase(resource_id)
	resource_removed.emit(pool, resource_id, removed)
	return removed

func get_quantity(pool: int, resource_id: StringName) -> int:
	return _pools[pool].get(resource_id, 0)

func has_item(pool: int, resource_id: StringName, quantity: int = 1) -> bool:
	return get_quantity(pool, resource_id) >= quantity

func get_pool_contents(pool: int) -> Dictionary:
	return _pools[pool].duplicate()

func _get_max_stack(resource_id: StringName) -> int:
	return SUPPLY_CLASS_CAPACITY.get(resource_id, 99)
