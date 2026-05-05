# Smoke Test: Critical Paths

**Purpose**: Run these 10-15 checks in under 15 minutes before any QA hand-off.
**Run via**: `/smoke-check` (which reads this file)
**Update**: Add new entries when new core systems are implemented.

## Core Stability (always run)

1. Game launches to main menu without crash
2. New game / session can be started from the main menu
3. Main menu responds to all inputs without freezing

## Core Mechanic (update per sprint)

4. Hub scene loads with 10 interactable stations
5. Chart opens and displays 2 MVP routes with correct visibility states
6. Player can select a route and confirm departure (two-step confirmation)
7. Exploration scene loads with 4-zone radial template
8. Player can scavenge and return to extraction anchor
9. Return to Hub — resources are correctly added to inventory
10. Repair node deposit works correctly (batch atomic)

## Data Integrity

11. Save game completes without error (staging → verify → promotion)
12. Load game restores correct state (all domain snapshots valid)
13. Save file checksum verification passes on round-trip

## Performance

14. No visible frame rate drops on target hardware (60fps target)
15. No memory growth over 5 minutes of play (Hub ↔ Exploration round-trips)
