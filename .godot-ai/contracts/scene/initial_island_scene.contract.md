# Godot Asset Contract: initial_island_scene

## Metadata

- Asset Type: scene
- Stable ID: initial_island_scene
- Display Name: 初始岛屿 / Hub 外部码头
- Source Requirement: `production/scene-specs/initial-island-scene.md`
- Lifecycle State: review-ready

## Intent

- Player/User-facing purpose: 给玩家一个真实的外部世界起点：浮岛、码头、停靠云织号、登船坡道和水线 / 云海边界在同一可玩空间中可读。
- Design role: 作为进入 `ship_interior_layered` 的前置世界空间，证明玩家从安全岛屿码头登船，而不是从 HUD、按钮、标签或调试入口进入船内。
- In scope: 独立 Godot 场景、灰盒世界层节点、玩家出生点、登船软重叠锚点、停靠船体 / 气囊轮廓、水线边界、作者化数据、HubRuntime 挂载证据、smoke / integration 验证。
- Non-goals: 主菜单、HUD 教程、市场、村镇、NPC hub、最终美术 / 音频、存档格式迁移、删除旧 `HubRuntime` 节点、替换 `ship_interior_layered`。

## Godot Outputs

- Scene paths: `src/scenes/hub/InitialIslandScene.tscn`
- Script paths: `src/scenes/hub/InitialIslandScene.cs`
- Resource paths: `src/presentation/playable_slice_authored_content.json`
- Test/preview paths: `tests/smoke/session_shell_visual_probe.gd`; `tests/integration/playable-slice/DomainAdapterTest.csproj`; `.godot-ai/verification/scene/initial_island_scene.verification.md`

## Runtime Boundary

- Owns: `initial_island_scene` 本地场景层级、可见世界灰盒、出生点、登船锚点节点证据、场景资产自检字典。
- Reads: `hub_island_dock` 运行时合同、作者化场景 / 单位数据、`ship_interior_layered` 接续路径、玩家输入焦点状态。
- Emits: 本地 `BoardingRequested` 信号和 debug evidence；实际登船切换仍由 `HubRuntime` 现有输入路径处理。
- Must not own: Hub 全局状态、Chart / Persistence / Resources / ModuleHull 权威、HUD 控件、调试按钮、旧 HubRuntime 灰盒证据、`hub_ship_interior` 内部场景规则。

## Decision Boundaries

- AI may decide: 节点组织、灰盒尺寸、非最终颜色、世界层节点命名，只要保留规格身份、登船路径和 #20 物理合同语义。
- AI must ask before: 删除或替换现有 `HubRuntime` 节点 / 文件、迁移 `hub_island_dock` 稳定运行时 ID、改动存档格式、引入新依赖、添加市场 / NPC / 新经济系统、改变登船目标。

## Acceptance Evidence

- Node/resource evidence: `InitialIslandScene.tscn` 包含 `InitialIslandWorldLayer`、`HubPlayableSkyBackdrop`、`HubIslandMainMass`、`HubDockPlankWalkway`、`HubDockedShipExterior`、`HubDockedShipBalloon`、`HubBoardingRamp`、`HubWaterlineBoundary`、`InitialIslandPlayerStart`、`BoardingRampSoftOverlap`。
- Visual evidence: Godot smoke 证明独立场景挂载后外部码头世界层可见，旧 HubRuntime 文本 / HUD / 按钮不作为场景证据。
- Runtime evidence: `HubRuntime.DebugInitialIslandAssetEvidence()` 报告 `scene_id == initial_island_scene`、`runtime_contract_id == hub_island_dock`、登船目标 `ship_interior_layered`、出生点 / 登船锚点 / 水线边界就绪，且 `ui_evidence_allowed_for_scene == false`。
- Log/test evidence: `dotnet build`、Godot headless smoke、`tests/integration/playable-slice/DomainAdapterTest.csproj`、solution build、`git diff --check`。

## Execution Readiness

- Blocking ambiguity: None.
- Required MCP/editor state: Godot editor session inspected before execution; file-level scene / C# generation is allowed because outputs are concrete and non-destructive.
- Safe to execute: true

## Asset-Type Specific Requirements

- Layout: 水平外部码头场景；岛体和木栈道在中下部，停靠云织号在右侧，登船坡道连接码头和船体，水线 / 云海边界在底部。
- Entry/exit: 新游戏、船内离开、探索返回进入 `initial_island_scene`；登船坡道进入 `ship_interior_layered` / `hub_ship_interior`。
- Player spawn: `InitialIslandPlayerStart`，对应作者化实例 `scene_unit.instance.hub_island_dock.player_marker`。
- Boundaries: `HubIslandWalkBoundary`、`HubWaterlineBoundary`、岛屿边缘、船体阻挡、码头柱。
- Landmarks: 浮岛主地形、木栈道、停靠船体、飞艇气囊、登船坡道。
- Interaction anchors: `BoardingRampSoftOverlap` / `hub_boarding_ramp`。
- Authored world units: player marker、island mass、dock plank walkway、docked ship hull、boarding ramp、airship envelope、waterline。
- State variants: 初始、返航 / 完成、模态阻塞。
- Screenshot/smoke evidence: Headless smoke must verify hierarchy/runtime evidence; non-headless screenshot remains follow-up if unavailable.

## Residual Ambiguity

- Non-blocking assumptions: 本轮以生产可追踪灰盒为目标；最终美术、音频和人工截图审核后续补齐。
- Blocking questions: None.
