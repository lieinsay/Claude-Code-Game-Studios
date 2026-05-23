# Epics Index

> **Last Updated**: 2026-05-24
> **Engine**: Godot 4.6.2 .NET + C# (desktop-first)
> **ADR Coverage**: 19 Accepted (ADR-0019 platform pivot active)

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
| [resources-goods-capacity](resources-goods-capacity/EPIC.md) | #5 | resources-goods-capacity.md | ADR-0005 | 3 | 9 (001-009) | **Complete** |

## Core Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [intel-knowledge](intel-knowledge/EPIC.md) | #6 | player-knowledge-intel.md | ADR-0007 | 3 | 8 (001-008) | **Complete** |
| [airship-hub](airship-hub/EPIC.md) | #7 | airship-hub.md | ADR-0001, ADR-0002, ADR-0003, ADR-0004 | 3 | 8 (001-008) | **Complete — reviewed 2026-05-12** |
| [modules-hull-state](modules-hull-state/EPIC.md) | #8 | airship-modules-hull-state.md | ADR-0009 | 3 | 8 (001-008) | **Complete — reviewed 2026-05-13** |
| [chart-route-planning](chart-route-planning/EPIC.md) | #9 | chart-route-planning.md | ADR-0008 | 3 | 8 (001-008) | **Complete** |
| [navigation-route-risk](navigation-route-risk/EPIC.md) | #10 | navigation-route-risk.md | ADR-0010 | 3 | 8 (001-008) | **Complete — reviewed 2026-05-13** |

## Feature Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [exploration-scavenge](exploration-scavenge/EPIC.md) | #11 | exploration-scavenge-scenario.md | ADR-0013 | 3 | 6 (001-006) | **Complete — reviewed 2026-05-14** |
| [combat-threat](combat-threat/EPIC.md) | #12 | combat-threat-handling.md | ADR-0018 | 3 | 6 (001-006) | **Complete** |
| world-repair | #13 | world-repair-unlock.md | ADR-0011 | 3 | 6 (001-006) | **Complete** |
| [settlement-market](settlement-market/EPIC.md) | #14 | port-village-market.md | ADR-0014 | 3 | 6 (001-006) | **Complete** |
| [partner-relationships](partner-relationships/EPIC.md) | #15 | partner-relationships.md | ADR-0015 | 3 | 6 (001-006) | **Complete — reviewed 2026-05-14** |

## Presentation Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [ui-hud-interface](ui-hud-interface/EPIC.md) | #16 | ui-hud-chart-interface.md | ADR-0012 | 4 | 6 (001-006) | **Complete** |
| [feedback-fx-audio](feedback-fx-audio/EPIC.md) | #17 | feedback-fx-audio.md | ADR-0016 | 2 | 5 (001-005) | **Complete — 2026-05-16** |
| [onboarding-first-loop](onboarding-first-loop/EPIC.md) | #18 | onboarding-first-loop.md | ADR-0017 | 1 | 5 (001-005) | **Complete — 2026-05-22** |

## Vertical Slice ADR Status

| ADR | System | Priority | Recommended Trigger |
|-----|--------|----------|---------------------|
| ADR-0016 | #17 Feedback | COMPLETE | Accepted and implemented for first Polish feedback slice |
| ADR-0017 | #18 Onboarding | COMPLETE | Accepted and implemented for first-loop Polish entry |
| ADR-0019 | Platform/C# Pivot | ACTIVE | Governs all new implementation stories |

## Production to Polish Scope Note

For the 2026-05-15 Production to Polish gate, #17 Feedback and #18 Onboarding were accepted as deferred Polish/post-gate work rather than hard blockers. UI/HUD #16 covers the verified MVP smoke loop feedback and discoverability needed for that gate: Hub/HUD visibility, Chart route departure, Exploration HUD pressure feedback, Save/Load, return-to-Hub, and Hub summary sync. #17 has since completed its first Polish implementation slice; #18 has completed its first-loop onboarding implementation slice with Godot smoke/perf evidence.

Sprint 001 scope briefs and formal GDDs now define the first Polish boundary:

- #17: `production/polish-backlog/feedback-fx-audio-scope-brief-2026-05-15.md`
- #18: `production/polish-backlog/onboarding-first-loop-scope-brief-2026-05-15.md`
- #17 GDD: `design/gdd/feedback-fx-audio.md`
- #18 GDD: `design/gdd/onboarding-first-loop.md`

Both GDDs are reviewed and approved. ADR-0016 and ADR-0017 are accepted architecture contracts; #17 and #18 implementation stories are complete under `production/epics/feedback-fx-audio/` and `production/epics/onboarding-first-loop/`.

## Scene Contract / Polish Gate Layer

| Epic | System # | GDD | Governing ADRs | TRs | Stories | Status |
|------|----------|-----|----------------|-----|---------|--------|
| [scene-composition-system](scene-composition-system/EPIC.md) | #19 | scene-composition-system.md | ADR-0001, ADR-0012, ADR-0016, ADR-0017, ADR-0019, GDD #20 | 3 | Not yet created | Ready |
| [scene-physics-unit-system](scene-physics-unit-system/EPIC.md) | #20 | scene-physics-unit-system.md | ADR-0004, ADR-0019, GDD #19 | 3 | Not yet created | Ready |
