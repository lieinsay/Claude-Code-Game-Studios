# Godot Asset Contract: scene_unit.prototype.chart_table

## Metadata

- Asset Type: fixed-world-unit
- Stable ID: scene_unit.prototype.chart_table
- Display Name: 航图台 / 星图桌
- Source Requirement: `production/unit-specs/fixed-scene-objects/chart-table.md`
- Lifecycle State: review-ready

## Intent

- Player/User-facing purpose: 玩家在云织号船内识别并接近一个真实世界航图台，按 Use 打开 `S4_chart`。
- Design role: 给航图 UI 提供船内世界锚点，避免旧 `ChartPanel`、按钮或 `helm_console` 冒充航图台本体。
- In scope: 独立固定单位场景、可读桌体/星图投影、soft-overlap 交互锚点、`idle` / `focused` / `chart_open` / `disabled` 状态、Hub 船内实例证据。
- Non-goals: 舵轮模拟、完整驾驶系统、资源/市场/维修状态、航行大场景、最终美术和音频。

## Godot Outputs

- Scene paths: `src/scenes/units/ChartTable.tscn`
- Script paths: `src/scenes/units/ChartTable.cs`
- Resource paths: `src/presentation/playable_slice_authored_content.json`
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `.godot-ai/verification/fixed-world-unit/scene_unit.prototype.chart_table.verification.md`

## Runtime Boundary

- Owns: Local table visual state, interaction anchor identity, open-chart intent signal.
- Reads: Whether chart is available and whether the fullscreen chart is open.
- Emits: `chart_open_requested`
- Must not own: Route selection, departure confirmation, Navigation, Resources, Persistence, voyage scene authority.

## Decision Boundaries

- AI may decide: Greybox dimensions, colors, child node names, highlight shape, local collision/overlap size, exact ship-interior placement within existing walk bounds.
- AI must ask before: Deleting/replacing existing authored scene nodes, adding new route IDs, adding final art/audio dependencies, changing Chart/Navigation domain behavior, reclassifying helm/domain IDs.

## Acceptance Evidence

- Node/resource evidence: Reusable `ChartTable.tscn` exists with script and soft-overlap anchor.
- Visual evidence: The table has a visible tabletop, projection/map surface, focused/disabled feedback nodes, and a label distinguishing it from helm/storage.
- Runtime evidence: Hub ship interior places the table and proximity + Use opens `S4_chart`.
- Log/test evidence: Smoke test confirms prototype/instance authoring data, interaction prompt, chart open, and `ui_evidence_allowed == false`.

## Execution Readiness

- Blocking ambiguity: None.
- Required MCP/editor state: Godot editor session or file-level scene creation plus Godot load verification.
- Safe to execute: true

## Asset-Type Specific Requirements

- Reusable scene/prefab boundary: `src/scenes/units/ChartTable.tscn`
- Visible form: Rectangular brass-rimmed chart table with projected/map surface.
- Collision or soft overlap: Body is `blocking_static`; interaction anchor is `soft_overlap`.
- States: `idle`, `focused`, `chart_open`, `disabled`.
- Interaction anchors: `ChartTableAnchor`.
- Emitted events: `chart_open_requested`.
- Instance evidence: `scene_unit.instance.hub_ship_interior.chart_table` in `src/presentation/playable_slice_authored_content.json`.

## Residual Ambiguity

- Non-blocking assumptions: First pass greybox is acceptable until art/audio assets are produced.
- Blocking questions: None.
