# Epics Index

> **Last Updated**: 2026-05-12
> **Engine**: Godot 4.6.2 .NET + C# (desktop-first)
> **ADR Coverage**: 17 Accepted + 2 Deferred (ADR-0019 platform pivot active)

## Active Implementation Contract

ADR-0019 governs all new implementation work. Existing story files may still
contain GDScript snippets, Web lifecycle references, or browser-storage examples
from the original planning pass; treat those as historical pseudocode unless a
story has been explicitly refreshed for C#. Implement systems in desktop
Godot .NET/C# by default and translate old examples through the current
technical preferences and control manifest.

## Foundation Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [content-registry](content-registry/EPIC.md) | #1 | content-data-state-registry.md | ADR-0001, ADR-0002 | 3 | 8 (001-008) | **Complete** |
| [platform-session-shell](platform-session-shell/EPIC.md) | #2 | platform-session-shell.md | ADR-0001, ADR-0006 | 3 | 7 (001-007) | Complete |
| [local-save-persistence](local-save-persistence/EPIC.md) | #3 | local-save-world-state-persistence.md | ADR-0003, ADR-0006 | 3 | 8 (001-008) | **Complete** |
| [player-movement-interaction](player-movement-interaction/EPIC.md) | #4 | player-movement-interaction.md | ADR-0004 | 3 | 7 (001-007) | **Complete** |
| [resources-goods-capacity](resources-goods-capacity/EPIC.md) | #5 | resources-goods-capacity.md | ADR-0005 | 3 | 9 (001-009) | In Progress |

## Core Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [intel-knowledge](intel-knowledge/EPIC.md) | #6 | player-knowledge-intel.md | ADR-0007 | 3 | 8 (001-008) | In Progress |
| [airship-hub](airship-hub/EPIC.md) | #7 | airship-hub.md | ADR-0001, ADR-0002, ADR-0003, ADR-0004 | 3 | 8 (001-008) | **Complete — reviewed 2026-05-12** |
| [modules-hull-state](modules-hull-state/EPIC.md) | #8 | airship-modules-hull-state.md | ADR-0009 | 3 | 8 (001-008) | In Progress |
| [chart-route-planning](chart-route-planning/EPIC.md) | #9 | chart-route-planning.md | ADR-0008 | 3 | 8 (001-008) | In Progress |
| [navigation-route-risk](navigation-route-risk/EPIC.md) | #10 | navigation-route-risk.md | ADR-0010 | 3 | 8 (001-008) | In Progress |

## Feature Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| exploration-scavenge | #11 | exploration-scavenge-scenario.md | ADR-0013 | 3 | 6 (001-006) | In Progress |
| combat-threat | #12 | combat-threat-handling.md | ADR-0018 | 3 | 6 (001-006) | In Progress |
| world-repair | #13 | world-repair-unlock.md | ADR-0011 | 3 | 6 (001-006) | In Progress |
| [settlement-market](settlement-market/EPIC.md) | #14 | port-village-market.md | ADR-0014 | 3 | 6 (001-006) | In Progress |
| [partner-relationships](partner-relationships/EPIC.md) | #15 | partner-relationships.md | ADR-0015 | 3 | 6 (001-006) | In Progress |

## Presentation Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [ui-hud-interface](ui-hud-interface/EPIC.md) | #16 | ui-hud-chart-interface.md | ADR-0012 | 4 | 6 (001-006) | In Progress |
| feedback-fx-audio | #17 | feedback-fx-audio.md | (ADR-0016 deferred) | 2 | — | Blocked — ADR-0016 |
| onboarding-first-loop | #18 | onboarding-first-loop.md | (ADR-0017 deferred) | 1 | — | Blocked — ADR-0017 |

## Deferred ADR Status

| ADR | System | Priority | Recommended Trigger |
|-----|--------|----------|---------------------|
| ADR-0016 | #17 Feedback | MEDIUM | Before VFX/Audio implementation |
| ADR-0017 | #18 Onboarding | LOW | Vertical Slice phase |
| ADR-0019 | Platform/C# Pivot | ACTIVE | Governs all new implementation stories |
