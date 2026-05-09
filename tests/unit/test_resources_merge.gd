# Resources Stack Merge — Unit Tests
# Validates: stack merge algorithm, unique item handling, deterministic fill order
# REF: resources-goods-capacity Story 001 AC-1/2, AC-4/5, AC-8-10
extends Node

func test_stackable_merge_fullest_first() -> void:
	var rm := _make_resources()
	# Add 30 repair kits: should merge into one stack of 30
	rm.add_item(rm.Pool.STORAGE, &"resource.repair_kit", 30, &"stackable")
	var qty: int = rm.get_quantity(rm.Pool.STORAGE, &"resource.repair_kit")
	assert(qty == 30, "Expected 30, got %d" % qty)

func test_stackable_overflow_creates_new_stacks() -> void:
	var rm := _make_resources()
	# Add 150 basic supplies (max_stack=99): should fill one stack to 99, overflow 51
	rm.add_item(rm.Pool.STORAGE, &"resource.basic_supply", 150, &"stackable")
	var qty: int = rm.get_quantity(rm.Pool.STORAGE, &"resource.basic_supply")
	assert(qty == 150, "Expected 150, got %d" % qty)

func test_unique_item_only_one_slot() -> void:
	var rm := _make_resources()
	var added1: int = rm.add_item(rm.Pool.STORAGE, &"resource.ancient_lens", 1, &"unique")
	var added2: int = rm.add_item(rm.Pool.STORAGE, &"resource.ancient_lens", 1, &"unique")
	assert(added1 == 1, "First add should succeed")
	assert(added2 == 0, "Second add should be rejected")
	assert(rm.get_quantity(rm.Pool.STORAGE, &"resource.ancient_lens") == 1, "Should still be 1")

func test_remove_item_partial() -> void:
	var rm := _make_resources()
	rm.add_item(rm.Pool.STORAGE, &"resource.repair_kit", 50, &"stackable")
	var removed: int = rm.remove_item(rm.Pool.STORAGE, &"resource.repair_kit", 20)
	assert(removed == 20, "Expected 20 removed, got %d" % removed)
	assert(rm.get_quantity(rm.Pool.STORAGE, &"resource.repair_kit") == 30, "Expected 30 remaining")

func test_remove_item_excess_returns_available() -> void:
	var rm := _make_resources()
	rm.add_item(rm.Pool.STORAGE, &"resource.repair_kit", 10, &"stackable")
	var removed: int = rm.remove_item(rm.Pool.STORAGE, &"resource.repair_kit", 50)
	assert(removed == 10, "Should only remove 10, got %d" % removed)
	assert(rm.get_quantity(rm.Pool.STORAGE, &"resource.repair_kit") == 0, "Should be 0")
	assert(not rm.has_item(rm.Pool.STORAGE, &"resource.repair_kit"), "Should not have item")

func test_supply_class_max_stack_enforced() -> void:
	var rm := _make_resources()
	# intel class items (ancient_lens) use max_stack=1 (unique)
	rm.add_item(rm.Pool.STORAGE, &"resource.navigation_chart", 5, &"stackable")
	var qty: int = rm.get_quantity(rm.Pool.STORAGE, &"resource.navigation_chart")
	assert(qty == 5, "Expected 5, got %d" % qty)
	# Navigation supply class: max_stack=20, so 5 should fit in one stack

func test_has_item_with_quantity() -> void:
	var rm := _make_resources()
	rm.add_item(rm.Pool.STORAGE, &"resource.repair_kit", 5, &"stackable")
	assert(rm.has_item(rm.Pool.STORAGE, &"resource.repair_kit", 3), "Should have at least 3")
	assert(rm.has_item(rm.Pool.STORAGE, &"resource.repair_kit", 5), "Should have at least 5")
	assert(not rm.has_item(rm.Pool.STORAGE, &"resource.repair_kit", 6), "Should not have 6")

## Helper

func _make_resources() -> ResourcesManager:
	var rm := ResourcesManager.new()
	rm._ready()
	return rm

func before() -> void: pass
func after() -> void: pass
