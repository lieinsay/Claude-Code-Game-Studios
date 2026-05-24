# 场景覆盖登记表

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-001-scene-spec-template-coverage-registry.md`
> **最后更新**: 2026-05-24
> **登记规则**: 每个可进入场景在被视为 production-ready 前，必须链接场景规格、等价完整来源说明、待补规格缺口，或明确的 #20 豁免。
> **语言规则**: 除路径、代码符号、命令、稳定 ID、状态枚举、ADR/TR 编号等必要内容外，本目录文档必须使用中文。

## 登记规则

- 场景是玩家可进入的世界 / 可玩空间，不是 UI 面板。
- UI/HUD/按钮/标签/调试覆盖层可以辅助证据，但不能满足场景单位、物理单位或可读性证据。
- #19 登记行可以摘要 #20 状态，但 #20 仍是物理单位细节的唯一来源。
- 修复点和市场场景即使尚未完整实现，也必须追踪，因为 #19 要求它们在被视为视觉完成前具备规格。
- `Scene spec status` 表示 #19 场景构成规格状态，不表示运行时功能完成。

## 状态词汇

| 状态 | 含义 |
| --- | --- |
| `covered-by-current-note` | 现有 story/GDD/证据已包含足够材料用于 Story 001 覆盖，但 release gate 前可能仍需抽取为独立规格。 |
| `template-ready` | 可复用模板已存在；具体场景规格仍需起草。 |
| `tracked-gap` | 场景已知且需要，但尚无完整可进入场景规格。 |
| `exempt-no-physical-units` | 场景没有玩法相关物理单位，并明确说明 #20 不适用。 |
| `spec_drafted` | 独立场景规格已起草；仍可能需要 Codex 审核、用户审核、运行时证据或截图证据。 |

## 当前可进入场景覆盖

| Scene ID | 玩家可见场景 | 当前进入来源 | 场景规格状态 | #20 物理输入 | 当前证据 / 来源 | 必要下一步 |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_island_scene` | 初始岛屿 | 新游戏开始 / 返回原点 | `covered-by-current-note` | 历史运行时合同位于 `hub_island_dock`；独立规格中必须决定是保留别名还是重命名，并按带世界 / 可玩单位的水平场景处理 | `production/polish-backlog/story-polish-015-island-ship-interior-and-search-gameplay-design.md`; `production/qa/evidence/polish-015-island-ship-interior-and-search-gameplay-evidence.md`; `production/qa/evidence/scene-physics-runtime-contract-shape-evidence.md`; `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md` | 抽取独立初始岛屿规格，并决定运行时 ID 是否继续映射到 `hub_island_dock`。 |
| `ship_interior_layered` | 云织号船内分层水平场景 | 从初始岛屿登船 / 从航行返回 | `spec_drafted` | 运行时合同位于 `hub_ship_interior`；第一条原型 / 摆放实例作者化切片已链接 `src/presentation/playable_slice_authored_content.json` 和 `production/scene-specs/ship-interior-layered-scene.md` | `design/gdd/airship-hub.md`; `production/scene-specs/ship-interior-layered-scene.md`; `production/qa/evidence/scene-unit-placement-taxonomy-evidence.md`; `production/qa/evidence/scene-physics-layer-cutaway-floor-state-evidence.md`; `production/qa/evidence/scene-physics-unit-catalog-evidence.md` | 等待用户按场景规格中的审核清单确认；之后再进入 release handoff。 |
| `voyage_open_world_scene` | 航行大场景 | 从初始岛屿前往 demo 目的地 | `spec_drafted` | implementation readiness 前需要 #20 合同：伪 3D 相机对齐飞行、航线边界、风险物、云 / 雾特殊表面、恢复规则 | `production/scene-specs/voyage-open-world-scene.md`; `design/gdd/navigation-route-risk.md`; `design/gdd/scene-composition-system.md` | 用户先审航行场景方向；通过后再起草 #20 合同和运行时证据计划。 |
| `mist_lamp_wreck_scene` | 雾灯残骸 | 从 `voyage_open_world_scene` 抵达 | `spec_drafted` | 运行时合同位于 `exploration_mist_island`；原型 / 摆放实例作者化已链接 `src/presentation/playable_slice_authored_content.json` 和 `production/scene-specs/mist-lamp-wreck-scene.md` | `design/gdd/exploration-scavenge-scenario.md`; `production/scene-specs/mist-lamp-wreck-scene.md`; `production/polish-backlog/story-polish-015-search-return-microgame-design-note.md`; `production/qa/evidence/scene-unit-placement-mist-lamp-wreck-evidence.md`; `production/qa/evidence/scene-physics-unit-catalog-evidence.md`; `production/qa/evidence/scene-physics-dynamic-behavior-recovery-evidence.md` | 等待用户按场景规格中的审核清单确认；之后再进入 release handoff。 |
| `old_market_edge_scene` | 旧集市边缘 | 从 `voyage_open_world_scene` 抵达 | `tracked-gap` | implementation readiness 前需要 #20 合同，因为市场边缘、摊位、NPC、货物和可通行性都是物理场景单位 | `design/gdd/port-village-market.md`; `design/gdd/scene-composition-system.md` | 将旧集市从未来市场缺口提升为当前 demo 目的地；视觉完成声明前需起草场景规格。 |
| `repair_node_scene` | 世界修复 / 解锁点 | 未来修复地点入口 | `tracked-gap` | implementation readiness 前需要 #20 合同，因为修复点会包含玩法相关物理单位 | `design/gdd/world-repair-unlock.md`; `design/gdd/scene-composition-system.md` | 在任何修复场景被视为视觉完成前起草规格；除非明确加入，否则不属于当前 demo 可读性队列。 |

## 非场景 / UI 表面

这些表面可以出现在玩家流程中，但不能计入场景单位或物理验收证据。

| 表面 | 分类 | 证据规则 |
| --- | --- | --- |
| HUD 状态面板 | UI 辅助 | 可摘要资源、船体、航线或威胁状态；不能证明场景身份或物理单位就绪。 |
| 航图按钮 / 航线按钮 | UI 控件 | 可确认航线选择；不能替代舵台 / 桌面世界锚点。 |
| 保存 / 读取 / 删除按钮 | UI 控件 | 仅是持久化入口；不是场景单位。 |
| 调试标签 / smoke-only hook | 调试证据 | 可帮助自动断言；不能满足人工可读性或场景单位证据。 |
| 新手引导提示文字 | UI 辅助 | 可引导第一轮；不能替代可见世界锚点。 |

## Story 001 验收映射

| 验收项 | 覆盖方式 |
| --- | --- |
| 新的可进入场景在 production readiness 前需要场景规格 | `production/scene-specs/scene-spec-template.md` 定义可复用必需结构和清单。本登记表要求每行链接规格、等价说明、追踪缺口或明确豁免。 |
| 2D 场景规格包含或链接 #20 Scene Physics Contract | 登记表包含 `#20 物理输入` 列，并为初始岛屿、分层船内、航行大场景、雾灯残骸、旧集市边缘记录运行时合同链接或缺口。 |
| 无物理单位场景必须明确说明 #20 不适用 | 模板包含豁免字段；当前 demo 没有静默豁免行。除非后续提升为物理场景，否则航图桌面被视为船内支持表面。 |
| 当前 demo 场景在 release readiness 前需要规格 | `initial_island_scene`, `ship_interior_layered`, `voyage_open_world_scene`, `mist_lamp_wreck_scene`, `old_market_edge_scene` 均需要独立规格或明确映射来源说明。 |
| 未来修复场景在视觉完成前需要规格 | `repair_node_scene` 仍是未来追踪缺口，视觉完成声明前必须处理。 |

## 后续队列

1. Story 002 应把这些登记状态转换为具体完整性 / 证据门禁，不重新定义 #20 物理细节。
2. Story 003 应强化登记行和 release 证据中的 UI-vs-scene 证据拒绝规则。
3. Story 004 应在 release readiness 前，把用户可读性审核问题和结论挂到每个当前 demo 场景：初始岛屿、船内、航行大场景、雾灯残骸、旧集市边缘。

## Release Handoff 状态

Release handoff 输入位于 `production/scene-specs/scene-release-gate-handoff.md`。当前状态为 `BLOCKED_FOR_RELEASE`，直到用户可读性审核被记录或明确豁免，并且修复 / 市场追踪缺口被解决或豁免。
