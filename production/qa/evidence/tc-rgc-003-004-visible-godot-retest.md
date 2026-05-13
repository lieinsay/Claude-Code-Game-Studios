# TC-RGC-003/004 Visible Godot Retest Evidence

**Date**: 2026-05-13
**Engine**: Godot 4.6.2 .NET
**Result**: PASS

## Scope

- TC-RGC-003 — Hub Runtime Reachability
- TC-RGC-004 — Resource Inventory Presentation

## Evidence

- Visible Godot run launched `res://src/scenes/SessionShell.tscn`.
- Flow executed: Entry -> audio confirmation -> `HubRuntime.tscn` mounted under `GameplayLayer`.
- Screenshot: [tc-rgc-003-004-visible-godot-2026-05-13.png](tc-rgc-003-004-visible-godot-2026-05-13.png)

## Observed State

- Hub title visible: `云织号空艇中枢`
- Stations visible: helm ready, chart pending, cargo enterable, module table enabled
- Storage visible: `基础补给 x10 / 修理包 x4`
- Cargo visible: `已用 0 / 有效容量 500 / 受困货物 0`
- Module visible: `槽位 A 空置 / 槽位 B 货舱模块已安装`
- Hull visible: `完整度 100 / 承载带稳定 / 可出航`

## Verification

- Visible Godot run with a temporary SceneTree retest harness — PASS.
- The harness loaded `SessionShell.tscn`, invoked the Start/audio-confirmation path, verified the Hub/resource labels above, saved the screenshot evidence, and was removed after the run.

## Remaining Scope

TC-RGC-005 through TC-RGC-009 remain blocked by downstream interaction/UI wiring and are not part of this retest.
