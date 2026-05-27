# Godot Asset Contract: S4_chart

## Metadata

- Asset Type: ui
- Stable ID: S4_chart
- Display Name: 航图全屏表面
- Source Requirement: `production/ui-specs/chart-full-screen-surface.md`
- Lifecycle State: review-ready

## Intent

- Player/User-facing purpose: 玩家从航图台打开全屏/近全屏航图，查看可用航线、风险摘要，并确认或返回。
- Design role: 独立替代旧 `ChartPanel` / HubRuntime 内嵌临时航图，绑定 `scene_unit.prototype.chart_table`，但不替代航行或岛屿世界场景。
- In scope: Independent Control scene, focusable route/confirm/return controls, disabled/empty-state copy, route selection feedback, signals for route selected/departure/close.
- Non-goals: Voyage open-world gameplay, island scene proof, final art/audio/VFX, route data authority, persistence authority.

## Godot Outputs

- Scene paths: `src/scenes/ui/ChartFullScreenSurface.tscn`
- Script paths: `src/scenes/ui/ChartFullScreenSurface.cs`
- Resource paths: None beyond scene/script.
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `.godot-ai/verification/ui/S4_chart.verification.md`

## Runtime Boundary

- Owns: UI Control tree, focus order, local selected-route presentation, close/confirm control signals.
- Reads: Route labels and availability passed by presenter/runtime; currently first route greybox mirrors `route.mist`.
- Emits: `route_selected(route_id)`, `departure_confirmed`, `chart_closed`.
- Must not own: ChartManager route validity, Navigation state, Hub departure state, Resources, Persistence, voyage scene implementation.

## Decision Boundaries

- AI may decide: Control node layout, theme colors, labels, responsive minimum sizes, local child node names.
- AI must ask before: Removing existing UI nodes, adding new route IDs, skipping voyage scene boundaries, changing domain APIs, adding new dependencies.

## Acceptance Evidence

- Node/resource evidence: `ChartFullScreenSurface.tscn` contains a Control tree with route list, risk summary, confirm, and return controls.
- Visual evidence: Fullscreen/near-fullscreen chart surface renders route and risk regions distinctly from the ship world.
- Runtime evidence: HubRuntime loads the UI scene for chart mode; route selection updates presentation; confirm uses existing domain departure flow; Esc/return goes back to ship interior.
- Log/test evidence: Smoke test verifies independent scene load, active chart surface, focus/button nodes, and route departure.

## Execution Readiness

- Blocking ambiguity: None.
- Required MCP/editor state: Godot editor session or file-level scene creation plus Godot load verification.
- Safe to execute: true

## Asset-Type Specific Requirements

- Control tree: Root `ChartFullScreenSurface` with backdrop, route list, risk summary, confirm, and return controls.
- Theme/style: Greybox maritime chart colors with contrast-safe labels.
- Focus rules: Initial focus on `RouteMistButton`; confirm/return are focusable; route selection does not return focus to world.
- Responsive sizing: Root anchors full rect; inner margins and minimum sizes define stable layout.
- States: hidden by parent mode, active, disabled/empty, selected route.
- Input ownership: UI controls own mouse/focus while visible; HubRuntime handles Esc fallback.
- Screenshot evidence: Downstream screenshot required for release packet; smoke/hierarchy evidence is sufficient for first asset execution.

## Residual Ambiguity

- Non-blocking assumptions: Only `route.mist` is selectable in the current first pass; other routes remain disabled text until domain exposure is widened.
- Blocking questions: None.
