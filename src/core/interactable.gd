## Interactable — @abstract base class for all interactable objects
##
## All interactable objects in the game MUST extend this class.
## Implements Use entry point returning UseResult enum.
## REF: ADR-0004, control-manifest.md Required pattern.

@icon("res://icon.svg")
class_name Interactable
extends Area2D

enum UseResult {
	ACCEPTED,
	REJECTED,
	BUSY,
}

# Override in subclass
func handle_use(_user: Node2D) -> int:
	return UseResult.REJECTED

func get_interactable_id() -> StringName:
	return name.to_snake_case()

func register_with_registry() -> void:
	InteractionRegistry.register_interactable(get_interactable_id(), self)

func _enter_tree() -> void:
	register_with_registry()

func _exit_tree() -> void:
	InteractionRegistry.unregister_interactable(get_interactable_id())
