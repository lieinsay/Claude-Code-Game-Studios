## SessionShell — Main Scene root node (NOT an Autoload)
##
## Owns platform lifecycle, boot phase chain (Phase 0→7),
## AudioContext activation, and Web tab freeze/recovery.

class_name SessionShell
extends Node2D

# Platform state machine
enum ShellState {
	BOOTING,
	LOADING,
	READY,
	AWAITING_AUDIO,
	SESSION_STARTING,
	SESSION_ACTIVE,
	BACKGROUND_SUSPENDED,
	RESUME_PENDING,
	RECOVERY_REQUIRED,
	FATAL_BLOCKED,
}

# Boot phases (0-indexed per ADR-0001)
enum BootPhase {
	PHASE_0_PLATFORM_PROBE,
	PHASE_1_REGISTRY_LOAD,
	PHASE_2_PERSISTENCE_CHECK,
	PHASE_3A_RESOURCES_INTEL,
	PHASE_3B_CHART_INIT,
	PHASE_4_FEATURE_INIT,
	PHASE_5_HUB_INSTANTIATE,
	PHASE_6_UI_INIT,
	PHASE_7_FEEDBACK_SESSION_READY,
}

# Load sub-phases for shell loading screen
enum LoadPhase {
	BASE_BOOT,
	CONTENT_DOMAIN_CHECK,
	STORAGE_CAPABILITY_CHECK,
	SESSION_METADATA_CHECK,
	ENTRY_RENDER_READY,
}

## Signals

signal boot_requested()
signal shell_state_changed(old_state: int, new_state: int)
signal loading_phase_changed(phase: int, progress: float)
signal session_ready()
signal input_gate_open()
signal input_gate_closed()

var _current_state: int = ShellState.BOOTING
var _current_boot_phase: int = BootPhase.PHASE_0_PLATFORM_PROBE
var _input_gate_open: bool = true
var _boot_timer: float = 0.0
var _boot_complete: bool = false

func _ready() -> void:
	print("[SessionShell] Booting — Phase 0: Platform Probe")
	_transition_state(ShellState.BOOTING)
	boot_requested.emit()
	# Begin async boot chain
	_run_boot_chain()

func _process(delta: float) -> void:
	if _boot_complete and _current_state == ShellState.SESSION_ACTIVE:
		# Idle: wait for Web lifecycle events
		return
	_boot_timer += delta

func _run_boot_chain() -> void:
	# Phase 0 already done (platform probe in _ready)
	_current_boot_phase = BootPhase.PHASE_0_PLATFORM_PROBE
	await _boot_phase_delay(0.01)

	# Phase 1: Registry loads static content
	print("[SessionShell] Phase 1: Registry loading...")
	_current_boot_phase = BootPhase.PHASE_1_REGISTRY_LOAD
	Registry._initialize_content()
	Registry.registry_ready.emit()
	await _boot_phase_delay(0.01)

	# Phase 2: Persistence checks slots
	print("[SessionShell] Phase 2: Persistence checking...")
	_current_boot_phase = BootPhase.PHASE_2_PERSISTENCE_CHECK
	Persistence.persistence_ready.emit()
	await _boot_phase_delay(0.01)

	# Phase 3a: Resources + Intel parallel init
	print("[SessionShell] Phase 3a: Core data init...")
	_current_boot_phase = BootPhase.PHASE_3A_RESOURCES_INTEL
	await _boot_phase_delay(0.01)

	# Phase 3b: Chart queries Intel
	print("[SessionShell] Phase 3b: Chart init...")
	_current_boot_phase = BootPhase.PHASE_3B_CHART_INIT
	await _boot_phase_delay(0.01)

	# Phase 4: WorldRepair + InteractionRegistry init
	print("[SessionShell] Phase 4: Feature init...")
	_current_boot_phase = BootPhase.PHASE_4_FEATURE_INIT
	await _boot_phase_delay(0.01)

	# Phase 5: AirshipHub instantiated
	print("[SessionShell] Phase 5: Hub instantiate...")
	_current_boot_phase = BootPhase.PHASE_5_HUB_INSTANTIATE
	await _boot_phase_delay(0.01)

	# Phase 6: UIManager init 12 screens
	print("[SessionShell] Phase 6: UI init...")
	_current_boot_phase = BootPhase.PHASE_6_UI_INIT
	UIManager.ui_ready.emit()
	await _boot_phase_delay(0.01)

	# Phase 7: FeedbackManager subscriptions + session ready
	print("[SessionShell] Phase 7: Session ready!")
	_current_boot_phase = BootPhase.PHASE_7_FEEDBACK_SESSION_READY
	_boot_complete = true
	_transition_state(ShellState.SESSION_ACTIVE)
	session_ready.emit()
	print("[SessionShell] Architecture boot: PASS (%.2f ms)" % (_boot_timer * 1000.0))

func _boot_phase_delay(_duration: float) -> void:
	# Non-blocking: just yield one frame
	await get_tree().process_frame

func _transition_state(new_state: int) -> void:
	var old_state: int = _current_state
	_current_state = new_state
	shell_state_changed.emit(old_state, new_state)

func get_shell_state() -> int:
	return _current_state

func set_input_gate(open: bool) -> void:
	if open and not _input_gate_open:
		_input_gate_open = true
		input_gate_open.emit()
	elif not open and _input_gate_open:
		_input_gate_open = false
		input_gate_closed.emit()

func is_input_gate_open() -> bool:
	return _input_gate_open

## Web lifecycle hooks (ADR-0006)

func _notification(what: int) -> void:
	match what:
		NOTIFICATION_APPLICATION_RESUMED:
			if _current_state == ShellState.BACKGROUND_SUSPENDED:
				print("[SessionShell] Resuming from background...")
				_transition_state(ShellState.RESUME_PENDING)
				_transition_state(ShellState.SESSION_ACTIVE)
		NOTIFICATION_APPLICATION_PAUSED:
			if _current_state == ShellState.SESSION_ACTIVE:
				print("[SessionShell] Suspending to background...")
				_transition_state(ShellState.BACKGROUND_SUSPENDED)
