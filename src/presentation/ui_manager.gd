## UIManager — Autoload #8 (Presentation)
##
## Owns 12-screen state machine, single-slot modal stack, 4-layer input routing.
## Consumes data from all domain systems; does not own gameplay state.

extends Node

enum Screen {
	NONE,
	HUB,
	CHART,
	CHART_ROUTE_SELECTED,
	CHART_DEPARTURE_CONFIRMED,
	DEPARTURE_LOCKED,
	VOYAGE,
	EXPLORATION,
	EXTRACTING,
	SETTLEMENT,
	HUB_ARRIVING,
	COMBAT,
}

enum InputLayer {
	MODAL,
	SEMI_MODAL,
	NON_MODAL,
	HUD,
	WORLD,
}

## Signals

signal screen_changed(old_screen: int, new_screen: int)
signal ui_ready()
signal ui_panel_opened(panel_id: StringName)
signal ui_panel_closed(panel_id: StringName)

var _current_screen: int = Screen.NONE
var _modal_panel: StringName = &""
var _modal_stack: Array[StringName] = []
var _active_input_layer: int = InputLayer.WORLD
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

## Public API

func transition_screen(new_screen: int) -> bool:
	if new_screen == _current_screen:
		return false
	var old_screen: int = _current_screen
	_current_screen = new_screen
	screen_changed.emit(old_screen, new_screen)
	return true

func open_modal(panel_id: StringName) -> bool:
	if _modal_panel != &"":
		# Queue or discard based on panel_id
		if panel_id == &"S7_combat":
			# Combat override: save current modal, force open
			_modal_stack.append(_modal_panel)
		else:
			return false
	_modal_panel = panel_id
	_active_input_layer = InputLayer.MODAL
	ui_panel_opened.emit(panel_id)
	return true

func close_modal() -> void:
	var closed_id: StringName = _modal_panel
	if _modal_stack.size() > 0:
		_modal_panel = _modal_stack.pop_back()
	else:
		_modal_panel = &""
		_active_input_layer = InputLayer.WORLD
	ui_panel_closed.emit(closed_id)

func get_current_screen() -> int:
	return _current_screen

func get_active_input_layer() -> int:
	return _active_input_layer

func is_modal_open() -> bool:
	return _modal_panel != &""
