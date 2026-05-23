# Story 015 Search / Return Microgame Design Note

**Date:** 2026-05-23  
**Status:** Implemented for focused QA  
**Scope:** Small reusable interaction layer for the Polish 015 blocker

## Goal

The previous search loop required movement but still resolved as a single click.
This note defines the smallest interaction change that gives the player a
recognizable action without introducing a separate gameplay authority.

## Search Interaction

The search site resolves through three deliberate steps while the player is near
the wreck:

1. **Scan calibration:** the first interaction fills a scan progress strip and
   communicates that the player is aligning with the wreck.
2. **Echo lock:** the second interaction confirms a readable signal and keeps
   rewards unsettled.
3. **Salvage pulse:** the third interaction calls the existing canonical
   exploration advance path and applies resource, threat, cargo, and hull state.

The search micro-game is intentionally state-light. It does not create a new
domain model; it gates the existing `AdvanceExploration()` call through runtime
presentation state.

## Return Interaction

Return now happens at the docked ship rather than a remote beacon:

1. **Preheat:** the first interaction at the ship return helm fills a short
   engine-preheat strip and keeps the player in Exploration.
2. **Pilot return:** the second interaction calls the existing canonical
   `ReturnToHub()` path and lands the player back on the island dock exterior.

## QA Focus

Human QA should judge whether this is enough of a gameplay beat for the current
release-readiness blocker. It is not intended to be the final exploration
system, route content, encounter system, or final art pass.
