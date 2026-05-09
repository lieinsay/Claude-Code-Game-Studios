## ChartManager — Autoload #6 (Core)
##
## Autoload name: "Chart"
## Owns route state, route selection, and departure commitment.
## Consumes knowledge from Intel, static data from Registry.

class_name ChartManager
extends Node

enum ChartState {
	IDLE,
	BROWSING,
	ROUTE_SELECTED,
	DEPARTURE_CONFIRMED,
	DEPARTURE_LOCKED,
}

## Signals

signal route_selected(route_id: StringName, destination_id: StringName)
signal route_committed(route_id: StringName, destination_id: StringName, hazard_tags: Array[String])
signal departure_locked(route_id: StringName)

var _current_state: int = ChartState.IDLE
var _selected_route: StringName = &""
var _selected_destination: StringName = &""
var _routes: Dictionary = {}
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

## Public API

func register_route(route_id: StringName, route_data: Dictionary) -> void:
	_routes[route_id] = route_data

func select_route(route_id: StringName) -> bool:
	if not _routes.has(route_id):
		return false
	_selected_route = route_id
	_selected_destination = _routes[route_id].get("destination_id", "")
	_current_state = ChartState.ROUTE_SELECTED
	route_selected.emit(route_id, _selected_destination)
	return true

func commit_departure() -> bool:
	if _current_state != ChartState.ROUTE_SELECTED:
		return false
	var route_data: Dictionary = _routes.get(_selected_route, {})
	var hazard_tags: Array[String] = []
	route_data.get("hazard_tags", []).assign(hazard_tags)
	route_committed.emit(_selected_route, _selected_destination, hazard_tags)
	_current_state = ChartState.DEPARTURE_LOCKED
	departure_locked.emit(_selected_route)
	return true

func route_selectability(route_id: StringName) -> Dictionary:
	# Returns {selectable: bool, reason: String}
	if not _initialized:
		return {"selectable": false, "reason": "chart_not_initialized"}
	if not _routes.has(route_id):
		return {"selectable": false, "reason": "route_not_found"}
	var route: Dictionary = _routes[route_id]
	if not route.get("traversable", false):
		return {"selectable": false, "reason": "route_not_traversable"}
	if _current_state == ChartState.DEPARTURE_LOCKED:
		return {"selectable": false, "reason": "departure_locked"}
	return {"selectable": true, "reason": ""}

func get_current_state() -> int:
	return _current_state

func get_selected_route() -> StringName:
	return _selected_route
