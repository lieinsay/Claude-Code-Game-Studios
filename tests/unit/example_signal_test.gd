# Example unit test — validates signal naming convention from ADR-0002
# This is a placeholder test to confirm the test framework is functional.
# Replace with real system tests during implementation.
#
# Run: godot --headless --script tests/gdunit4_runner.gd

extends Node

# GdUnit4 test suite
# Requires: addons/gdunit4/ installed

func test_signal_naming_convention() -> void:
	# All signals must follow {noun}_{verb_past} pattern
	var signal_name := "deposit_committed"
	assert(signal_name.ends_with("ed") or signal_name.ends_with("en"),
		"Signal name must be past tense: %s" % signal_name)

func test_signal_typed_params() -> void:
	# All signal params must be explicitly typed (no Dictionary payloads)
	# This test validates the pattern — real tests use actual signal declarations
	pass

func test_canonical_json_deterministic() -> void:
	# ADR-0003: Same input → byte-identical output
	var input := {"b": 1, "a": 2}
	# Sorted keys: {"a": 2, "b": 1}
	# Real implementation uses CanonicalJSON helper in Persistence Autoload
	pass
