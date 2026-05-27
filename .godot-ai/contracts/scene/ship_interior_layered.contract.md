# Godot Asset Contract: ship_interior_layered

## Metadata

- Asset Type: scene
- Stable ID: ship_interior_layered
- Display Name: 云织号船内分层场景
- Source Requirement: `production/scene-specs/ship-interior-layered-scene.md`
- Lifecycle State: execution-ready

## Intent

- Player/User-facing purpose: 玩家进入云织号内部后，能在真实世界空间中识别驾驶舱 / 航图台、货舱、轮机间和出口路径。
- Design role: 作为 Hub / Chart 循环的船内世界场景，承载 `scene_unit.prototype.chart_table` 并打开 `S4_chart`，但不替代航行大场景。
- In scope: 独立 Godot `.tscn`、配套脚本、船内剖切灰盒、ChartTable 实例、S4_chart 引用、manifest 场景/单位实例追踪、HubRuntime 挂载、smoke/build 证据。
- Non-goals: 不实现完整维修系统、模块替换、复杂 NPC 船员、战斗、宿舍生活系统、最终美术、音频最终资产、旧节点删除或存档 ID 迁移。

## Godot Outputs

- Scene paths: `src/scenes/ship/ShipInteriorLayeredScene.tscn`
- Script paths: `src/scenes/ship/ShipInteriorLayeredScene.cs`
- Resource paths: `src/presentation/playable_slice_authored_content.json`
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `.godot-ai/verification/scene/ship_interior_layered.verification.md`

## Runtime Boundary

- Owns: 船内场景视觉层级、世界单位摆放、场景内 ChartTable 实例、隐藏 S4_chart 场景引用证据、读法标签和状态占位节点。
- Reads: `ChartTable.tscn`、`ChartFullScreenSurface.tscn`、HubRuntime 提供的显示/隐藏状态。
- Emits: 不直接发领域事件；ChartTable 自身仍可发 `ChartOpenRequested`，HubRuntime 负责 Use 输入和 Chart 打开。
- Must not own: Chart 状态机、航线确认、Resources / ModuleHull 权威、Persistence、输入模态权威、旧节点删除。

## Decision Boundaries

- AI may decide: 节点命名、灰盒颜色、房间比例、层级组织、S4_chart 引用节点是否默认隐藏、smoke 断言细节。
- AI must ask before: 删除或替换现有 HubRuntime 节点、迁移 `hub_ship_interior` 稳定运行时 ID、引入新依赖、增加新玩法系统或改变存档格式。

## Acceptance Evidence

- Node/resource evidence: `ShipInteriorLayeredScene.tscn` 存在并加载；包含 `ShipInteriorLayeredSceneRuntimeInstance`、`ShipInteriorWorldLayer`、`ShipInteriorChartTableSocket`、`ChartTableRuntimeInstance`、`ChartTableAnchor`、`S4ChartSceneReference`。
- Visual evidence: smoke 断言独立场景的船体、驾驶舱、货舱、轮机间、走廊和出口节点在进入船内后可见。
- Runtime evidence: HubRuntime 进入 `hub_ship_interior` 后挂载独立场景；靠近 ChartTable + Use 打开独立 `S4ChartRuntimeSurface`。
- Log/test evidence: `dotnet build`、Godot headless smoke、solution build、`git diff --check` 通过。

## Execution Readiness

- Blocking ambiguity: None.
- Required MCP/editor state: Godot 4.6.2 editor ready for scene load/inspect; file-level creation allowed because no destructive replacement is needed.
- Safe to execute: true

## Asset-Type Specific Requirements

- Layout: 横向船体剖切，驾驶舱 / 航图台、货舱、轮机间和出口阈值并列，含前景船体和甲板走廊。
- Entry/exit: HubRuntime 从外部登船进入 `hub_ship_interior`，出口阈值返回 Hub 外部；场景内提供 `ShipInteriorPlayerStart` 和 `ShipExitThreshold` 节点证据。
- Player spawn: `ShipInteriorPlayerStart` 对齐现有 `ShipInteriorPlayerStart` 运行时坐标。
- Boundaries: `ShipInteriorWalkBounds`、船体轮廓、前墙剖切、驾驶舱玻璃和房间隔断可读。
- Landmarks: `CockpitChartBay`、`CargoBay`、`EngineBay`。
- Interaction anchors: `ChartTableRuntimeInstance/ChartTableAnchor`、`ShipExitThreshold`、`StorageCrateProp`。
- Authored world units: ChartTable 必须来自 `src/scenes/units/ChartTable.tscn`；S4_chart 必须由 `src/scenes/ui/ChartFullScreenSurface.tscn` 正式引用。
- State variants: 初始、规划后、异常/损伤占位节点或标签存在；不创建新领域状态。
- Screenshot/smoke evidence: Godot smoke 验证节点、运行时和 S4 打开链路。

## Residual Ambiguity

- Non-blocking assumptions: 本轮以 production-traceable greybox 资产为目标，不声明最终美术 / 音频完成；`hub_ship_interior` 保留为兼容运行时合同 ID。
- Blocking questions: None.
