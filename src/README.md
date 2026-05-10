# Source Layout

`src` is organized by runtime responsibility, not by sprint history.

## Core

- `core/boot/` - session boot and lifecycle coordination.
- `core/content/` - static content registry and content validation.
- `core/persistence/` - save pipeline and snapshot package contracts.
- `core/interaction/` - interaction focus and dispatch registry.
- `core/resources/` - resource pools and cargo/resource operations.
- `core/intel/` - player knowledge and intel state.
- `core/chart/` - chart, route selection, and departure state.

## Feature

- `features/world_repair/` - world repair progression and deposit state.

## Presentation

- `presentation/` - UI state and feedback event coordination.

## Scenes

- `scenes/` - Godot scene files that form runtime entry points or composed scenes.

Keep new C# gameplay/system files in the narrowest matching directory. Create a
new subdirectory only when a system has a distinct ownership boundary, not just
because a file is new.
