# 赭石岛场景规格

> **负责人**: Scene Composition System (#19) + Scene Physics Unit System (#20)
> **适用范围**: `ochre_island_scene`
> **证据边界**: UI、HUD、按钮、标签、菜单、调试覆盖层只能辅助理解，不能计入场景单位或物理验收证据。

## 文件头

| 字段 | 内容 |
| --- | --- |
| Scene ID | `ochre_island_scene` |
| 玩家可见场景名 | 赭石岛 |
| 所属循环节点 | Exploration / Resource |
| 当前生命周期状态 | `spec_drafted` |
| 来源 GDD | `design/gdd/scene-composition-system.md`; `design/gdd/scene-physics-unit-system.md`; `design/gdd/resources-goods-capacity.md` |
| 来源 story 或设计说明 | 用户 2026-05-24 指定：赭石岛是可采条带状铁矿的小型资源岛 |
| 创建适合性人工审查 | `APPROVED_WITH_NOTES` |
| 创建审查记录 | 本文件“创建适合性人工审查” |
| 最近审核日期 | 2026-05-24 |
| 审核负责人 | 用户 / Codex |

## 独立实现 / 资产边界

| 字段 | 内容 |
| --- | --- |
| 独立 Godot 场景 | `src/scenes/ochre/OchreIslandScene.tscn`；已建立独立灰盒场景资产，不能复用旧市场或探索面板。 |
| 配套脚本 / runtime | `src/scenes/ochre/OchreIslandScene.cs`；仅承载资源岛本地信号，Resources / Navigation / Hub 仍为领域权威。`HubRuntime` 的 debug 入口必须实例化 `OchreIslandScene.tscn`，不得另画一套运行时赭石岛。 |
| 作者化数据 | Godot 场景内已实例化 `BandedIronOreInstance`、`OchreReturnAnchor` 和 `PlayerSpawn`；`src/presentation/playable_slice_authored_content.json` runtime 作者化链路待补。 |
| 资产组 | 赭色岛体、条带状铁矿、返航点、岛屿边界、采集反馈。 |
| 装配入口 | 航行大场景只负责抵达；赭石岛拥有自身场景本体和资源点实例。 |
| 禁止混入位置 | 不得把赭石岛只写成旧市场场景、探索面板或无独立边界的临时节点。 |
| 删除旧节点要求 | 若替代旧 Godot 节点，删除前必须列出节点路径并询问用户；当前为 `N/A true`。 |

## 创建适合性人工审查

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `ochre_island_scene` |
| 人工审查人 | 用户 |
| 审查日期 | 2026-05-24 |
| 结论 | `APPROVED_WITH_NOTES` |
| 必须回写的备注 | 中文概念名为赭石岛；用途是一个可以采条带状铁矿的小型资源岛；会新增固定单位条带状铁矿。 |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 当前 demo 需要第二个小型非市场目的地，证明航行大场景可以抵达不止一个岛屿。
- 不复用已有场景 / UI / 单位的原因: 旧集市边缘范围过大；雾灯残骸已承担搜索岛定位，不能同时承担资源岛定位。
- 主要范围风险: 不得在本轮引入市场、NPC、完整经济链或复杂采矿系统。
- 必须写回规格的调整: 赭石岛的核心交互是靠近条带状铁矿并采集 / 获取资源。

## 1. 场景身份

- 场景目的: 提供一个小型资源岛，让玩家从航行大场景抵达第二个非战斗、非市场目的地，并采集条带状铁矿。
- 情绪目标: 安静、干燥、矿物感、短暂停靠补给。
- 服务的核心幻想 / 支柱: 规划先于冒险；世界会回应照料；航行连接多个可读目的地。
- 玩家 3 秒内应理解: 我在一座赭色岩质小岛上，能看到条带状铁矿矿脉、可行走采集路径、返航点和边界。
- 本场景不是什么: 不是旧集市、不是市场 UI、不是大型矿场、不是完整经济链入口。

## 2. 场景物理合同

| 字段 | 内容 |
| --- | --- |
| 物理来源 | 独立 Godot 灰盒资产 + 作者化数据 + `HubRuntime.DebugScenePhysicsContract("ochre_island_scene")` |
| 合同场景 ID | `ochre_island_scene` |
| `physics_contract_complete` 状态 | true；仍需 playable route 接入和截图刷新 |
| 场景物理类型 | `水平场景` |
| 移动平面 | 小型岛屿地面平面四方向移动 |
| Layer / Height Model | 岛体地面、矿脉前景、边界、返航点 |
| Cutaway / Reveal Model | N/A true；第一版不包含建筑剖切 |
| 单位目录 | 玩家位置、岛体、路径、条带状铁矿、返航点、边界 |
| 固定单位原型 | `production/unit-specs/fixed-scene-objects/banded-iron-ore.md`；其余岛体 / 路径 / 返航点待单体规格 |
| 实体单位原型 | `production/unit-specs/dynamic-entities/player-controlled-entity.md` |
| 摆放实例 | `src/scenes/ochre/OchreIslandScene.tscn::WorldLayer/BandedIronOreInstance`；`src/presentation/playable_slice_authored_content.json` 已记录 6 个场景单位实例。 |
| 碰撞 / 遮挡 / 比例 | `blocking_static` 岛体 / 云海边界，`soft_overlap` 路径 / 矿脉 / 返航点；矿脉必须可读为资源点，不能只是纹理 |
| 特殊表面 / 动态行为 / 恢复规则 | `cloudsea` 边界 clamp 优先于矿脉和返航触发；矿脉为 `resource_node + trigger_only + breakable_state` |
| 无玩法相关物理单位时的豁免原因 | N/A true |

## 3. 进入 / 离开

- 进入来源: 从 `voyage_open_world_scene` 抵达。
- 出生 / 抵达位置: 赭石岛安全着陆点 / 玩家出生点。
- 离开或返回路径: 靠近返航点，起飞并经航行过程返回初始岛屿或船内。
- 取消 / 失败路径: 容量不足、采集不可用或输入焦点被 UI 占用时给出禁用反馈。
- 存档状态返回行为: 恢复玩家位置、矿脉采集状态、携带资源和返航状态。
- 场景切换清理预期: 关闭仅属于本场景的采集提示和临时交互焦点；不得清理全局资源状态。

## 4. 空间布局

- 主视口构图: 赭色岛体占中下部，条带状铁矿作为中景 / 前景可读资源点，返航点位于岛屿安全边缘。
- 可行走区域: 小型环形或短路径区域。
- 边界: 岛屿边缘、云海 / 水线、岩壁不可通行边界。
- 地标: 条带状铁矿、返航点、赭色岩面。
- 交互锚点: 条带状铁矿、返航点。
- 遮挡风险: 矿脉不能遮住玩家到返航点的最低可读路径。
- 最低灰盒可读性要求: 不读 UI 也能看出这是资源岛，并能找到矿脉和返航点。

## 5. 关键路径

1. 从航行大场景抵达赭石岛。
2. 玩家移动到条带状铁矿。
3. 靠近 + Use 采集 / 获取资源。
4. 玩家移动到返航点。
5. 返航点触发起飞，并经航行过程返回。

## 6. 可选内容 / 可读性节拍

- 可选观察点: 赭色岩壁、条带矿脉纹理、远处云海。
- 本地身份细节: 岛体小、矿脉集中、无市场摊位和 NPC。
- 生活 / 修复 / 损伤痕迹: N/A true；本轮不做聚落或维修空间。
- 嵌入世界中的玩家引导: 矿脉形状、采集路径和返航点位置承担主要引导。
- UI 辅助（如有）: 只允许显示采集提示、容量提示或失败原因。

## 7. 状态变体

| 变体 | 触发 / 来源状态 | 世界 / 可玩场景证据 | 允许的 UI 辅助 |
| --- | --- | --- | --- |
| 初始 | 首次抵达 | 矿脉可见，返航点可见，路径可通行 | 采集提示 |
| 采集完成 | 条带状铁矿已采集 | 矿脉变暗、标记消失或显示已采集状态 | 获得资源反馈 |
| 阻塞 / 异常 | 容量不足、系统禁止采集或返航不可用 | 世界对象保持可见，不删除矿脉或返航点 | 禁用原因提示 |

## 8. 交互合同

| 锚点 ID | 玩家动作 | 输入 / 焦点规则 | 领域负责人 | 禁用 / 失败反馈 | 世界证据 |
| --- | --- | --- | --- | --- | --- |
| `banded_iron_ore_anchor` | 靠近 + Use 采集 | 世界输入可用，玩家在 soft overlap 范围内 | Resources | 容量不足或已采集时短提示 | 条带状铁矿固定单位 |
| `ochre_return_anchor` | 靠近 + Use 返航 | 世界输入可用，采集状态不阻塞返航 | Navigation / Hub | 返航不可用时短提示 | 返航点世界锚点 |

## 9. 数据 / 运行时合同

- Godot 场景或运行时表面: `src/scenes/ochre/OchreIslandScene.tscn`、`src/scenes/ochre/OchreIslandScene.cs`、`HubRuntime.DebugScenePhysicsContract("ochre_island_scene")`、Debug build `OchreDebugButton` / `HubRuntime.DebugEnterOchreIslandScene()`；debug 入口通过 `ResourceLoader.Load<PackedScene>("res://src/scenes/ochre/OchreIslandScene.tscn")` 挂载同一个场景资产，不得复用旧市场、探索面板或手写重复灰盒作为场景本体。
- 稳定 ID: `ochre_island_scene`。
- 读取的领域管理器: Resources、Navigation、Scene。
- 会变更的领域管理器: Resources（采集结果）、Navigation / Hub（返航）。
- 持久化字段: 玩家位置、矿脉采集状态、携带资源、返航状态。
- 信号 / 语义事件: ore_harvested、resource_collected、return_departure_requested。
- 焦点和模态边界: 场景交互不得被常驻 UI 替代；采集提示不抢占世界输入。
- 运行时 debug / smoke hook: `tests/smoke/session_shell_visual_probe.gd` 校验 #20 物理合同、作者化单位链接、边界 / overlap / special surface / dynamic behavior，并通过 Debug build 按钮入口验证矿脉采集状态、返航两步和不替换 `route.mist`。

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 赭色小岛灰盒 | 场景身份、边界、移动平面 | `src/scenes/ochre/OchreIslandScene.tscn` | Scene / Art |
| P0 | 条带状铁矿灰盒 / 图标 | 资源点身份、采集状态 | `src/scenes/units/BandedIronOre.tscn`; `banded-iron-ore.md` | Unit / Art |
| P1 | 采集反馈音 / 视觉反馈 | 成功、失败、容量不足 | 待制作 | Audio / UX |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 自动 smoke | 场景载入、玩家移动、矿脉采集、返航触发 | partial；Godot AI MCP hierarchy PASS，#20 runtime contract smoke PASS，debug 入口实例化独立 `OchreIslandScene.tscn`、采集 / 返航 PASS；正式 playable route、Resources 奖励和 Navigation 返航写入待补 |
| 截图 / 视觉证明 | 赭石岛首屏、矿脉、返航点、采集后状态 | pending |
| Codex 审核 | 规格与 authored content 对齐检查 | partial；`.godot-ai/verification/composite-feature/ochre_island_resource_slice.verification.md`、`production/qa/evidence/ochre-island-godot-asset-execution-evidence.md` 和 `DomainAdapterTest` 已记录资产 / 作者化 / 运行时合同证据 |
| 后续反馈记录 | `directed-content-modification` 需求记录 | pending |

实现后自检问题:

- 我在哪里？
- 不看开发说明，我能在这里做什么？
- 我如何离开或继续？
- 相关动作之后发生了什么变化？
- UI/HUD 是否只是辅助场景，而不是主导或替代场景？

## 13. 创建适合性记录

本文件顶部“创建适合性人工审查”是权威记录；本节用于对齐当前场景模板。

- 审查问题: 是否应该创建 `ochre_island_scene`，而不是复用旧市场、雾灯残骸或 UI。
- 用户结论: `APPROVED_WITH_NOTES`
- 用户要求: 中文概念名为赭石岛；用途是可采条带状铁矿的小型资源岛；会新增固定单位条带状铁矿。
- 删除旧 Godot 节点确认: `N/A true`
- 进入实现条件: 创建适合性已通过；独立实现 / 资产边界和 QA 证据路径已记录。

## 14. 后续反馈与定向修改

| 字段 | 内容 |
| --- | --- |
| 创建适合性结论 | `APPROVED_WITH_NOTES` |
| 保持可修改状态 | `true` |
| 定向修改入口 | `directed-content-modification` |
| 用户反馈 / 后续定向修改需求 | None |

后续反馈只记录新的修改需求，不作为本规格的二次审核门。


## 15. 就绪检查清单

- [ ] 场景目的、循环角色和情绪目标明确。
- [ ] 创建适合性人工审查已通过；未通过时不能进入 `implementation_ready`。
- [ ] 进入、离开、失败和返回路径明确。
- [ ] 空间布局列出可行走区域、边界、地标和交互锚点。
- [ ] Scene Physics Contract 已链接并通过，或 #20 豁免明确。
- [ ] 固定单位与实体单位已分开引用；单位本体规则不只散落在本场景规格中。
- [ ] 场景单位来自世界 / 可玩场景层，而不是 UI/HUD/按钮/标签/调试覆盖层。
- [ ] 关键路径和可选可读性节拍已记录。
- [ ] 至少三个状态变体已记录，或明确豁免。
- [ ] 交互锚点说明输入 / 焦点行为和领域负责人。
- [ ] 运行时 / 状态合同没有创建新的玩法权威。
- [ ] P0 资产 / 音频需求可追溯到身份、交互、状态或反馈。
- [x] 自动证据、截图证据和规格一致性检查路径已命名。
