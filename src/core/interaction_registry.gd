## InteractionRegistry — Autoload #3 (Foundation)
##
## Interactable registration center spanning all scenes.
## Owns 5-state focus machine and dual-channel dispatch
## (interaction_used signal + handle_use() method call).

class_name InteractionRegistry
extends Node

enum FocusState {
	IDLE,
	FOCUSING,
	FOCUSED,
	UNFOCUSING,
	BLOCKED,
}

enum UseResult {
	ACCEPTED,
	REJECTED,
	BUSY,
}

## Signals

signal interaction_used(target_id: StringName, result: int)
signal focus_changed(old_target: StringName, new_target: StringName)

var _interactables: Dictionary[StringName, Node] = {}
var _focus_target: StringName = &""
var _focus_state: int = FocusState.IDLE
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

func register_interactable(target_id: StringName, node: Node) -> void:
	_interactables[target_id] = node

func unregister_interactable(target_id: StringName) -> void:
	if _focus_target == target_id:
		_clear_focus()
	_interactables.erase(target_id)

func get_interactable(target_id: StringName) -> Node:
	return _interactables.get(target_id, null)

func set_focus(target_id: StringName) -> void:
	if target_id == _focus_target:
		return
	var old_target: StringName = _focus_target
	_focus_target = target_id
	_focus_state = FocusState.FOCUSED
	focus_changed.emit(old_target, target_id)

func _clear_focus() -> void:
	var old_target: StringName = _focus_target
	_focus_target = &""
	_focus_state = FocusState.IDLE
	focus_changed.emit(old_target, &"")

func get_focus_target() -> StringName:
	return _focus_target
