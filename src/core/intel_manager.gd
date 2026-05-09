## IntelManager — Autoload #5 (Core)
##
## Autoload name: "Intel"
## Owns player knowledge state: known/unknown route information, rumors,
## risk clues, discoveries. Source of truth for discovered information.

class_name IntelManager
extends Node

enum KnowledgeState {
	UNREVEALED,
	REVEALED,
	RUMORED,
	VERIFIED,
}

enum KnowledgeDomain {
	LOCATION,
	ROUTE,
	HAZARD,
	PATTERN,
	ABILITY,
}

## Signals

signal knowledge_revealed(domain: int, subject_id: StringName, new_state: int)
signal rumor_revealed(location_id: StringName, source_tag: StringName, confidence: int)
signal pattern_observed(pattern_id: StringName)

var _knowledge: Dictionary = {}
var _initialized: bool = false

func _ready() -> void:
	_initialized = true

## Public API

func reveal_rumor(location_id: StringName, source_tag: StringName, hazard_hints: Array, confidence: int) -> void:
	if not _initialized:
		return
	# Clamp confidence per ADR-0015: max 66 for partner-originated rumors
	var final_confidence: int = mini(confidence, 100)
	if source_tag == &"partner.sky-cat":
		final_confidence = mini(final_confidence, 66)
	_knowledge[location_id] = {
		"state": KnowledgeState.RUMORED,
		"source": source_tag,
		"confidence": final_confidence,
		"hazards": hazard_hints,
	}
	rumor_revealed.emit(location_id, source_tag, final_confidence)

func query_knowledge(subject_id: StringName, domain: int = KnowledgeDomain.LOCATION) -> int:
	var entry: Dictionary = _knowledge.get(subject_id, {})
	return entry.get("state", KnowledgeState.UNREVEALED)

func report_observation_event(pattern_id: StringName, event_type: StringName) -> void:
	pattern_observed.emit(pattern_id)

func on_partner_joined(partner_id: StringName) -> void:
	# Stub: re-evaluate ability unlock conditions
	pass

func get_all_knowledge() -> Dictionary:
	return _knowledge.duplicate()
