# Asset Type Guidance

Use one of these asset types.

## scene
Require layout, entry/exit, player spawn, boundaries, landmarks, interaction anchors, authored world units, state variants, and screenshot/smoke evidence.

## fixed-world-unit
Require reusable scene/prefab boundary, visible form, collision or soft overlap, states, interaction anchors, emitted events, and instance evidence.

## dynamic-entity
Require movement/input or AI, state machine, collision, animation/visual states, signals, persistence needs, and runtime behavior evidence.

## ui
Require Control tree, theme/style, focus rules, responsive sizing, states, input ownership, and screenshot evidence for key states. UI must not replace world-object evidence unless the asset is explicitly UI-only.

## data-resource
Require Resource type/schema, fields, defaults, references, serialization path, owning system, validation rules, and load/inspect evidence.

## interaction-ability
Require trigger, eligibility, input, target rules, success/failure outcomes, emitted events, feedback, authority owner, and test evidence.

## vfx
Require trigger, node/material/particle outputs, parameters, lifecycle, performance constraints, preview scene, and screenshot/video evidence.

## audio
Require trigger, stream path, player/bus setup, volume/lifecycle, loop/fade rules, and runtime/log evidence.

## navigation-physics
Require collision/navigation shapes, layers/masks, reachable/unreachable areas, constraints, and movement/collision evidence.

## system-manager
Require API, state authority, signals, persistence, autoload or scene ownership, tests, and migration/non-goals.

## composite-feature
Use when the request contains multiple independent assets. Split child contracts and define dependencies instead of creating one oversized asset.
