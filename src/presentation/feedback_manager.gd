## FeedbackManager — Autoload #9 (Presentation)
##
## Semantic event hub for VFX, audio, and sensory feedback.
## Vertical Slice system — MVP stub subscribes to signals
## but defers full implementation to Production Sprint 2+.

extends Node

## Signals

signal feedback_triggered(event_id: StringName, params: Dictionary)
signal ui_event_consumed(event_id: StringName)

var _subscriptions: Dictionary[StringName, Callable] = {}
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

## Public API

func subscribe(event_id: StringName, callback: Callable) -> void:
	_subscriptions[event_id] = callback

func emit_feedback(event_id: StringName, params: Dictionary = {}) -> void:
	# Stub: full VFX/audio implementation deferred (ADR-0016)
	feedback_triggered.emit(event_id, params)
	var cb: Callable = _subscriptions.get(event_id, Callable())
	if cb.is_valid():
		cb.call(params)

## Semantic event consumers (stubs)

func on_route_selected(_route_id: StringName, _dest_id: StringName) -> void:
	fb_trigger("route_selected")

func on_repair_completed(_node_id: StringName) -> void:
	fb_trigger("world_repair_completed")

func on_threat_triggered(_threat_id: StringName) -> void:
	fb_trigger("threat_warning")

func fb_trigger(event: StringName) -> void:
	emit_feedback(event)
