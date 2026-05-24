# 初始岛屿场景规格

> **Scene ID**: `initial_island_scene`
> **运行时合同 ID**: `hub_island_dock`
> **状态**: spec_drafted
> **负责人**: Scene Composition System (#19) + Scene Physics Unit System (#20)
> **最后更新**: 2026-05-24

## 创建适合性人工审查

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `initial_island_scene` |
| 人工审查人 | 用户 |
| 审查日期 | 2026-05-24 |
| 结论 | `APPROVED` |
| 必须回写的备注 | None |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 当前 demo 需要一个非 UI 的安全起点，让玩家看到浮岛、码头、停靠飞船和登船路径。
- 不复用已有场景 / UI / 单位的原因: 主菜单、HUD 或船内空间都不能证明玩家从一个真实外部世界起点出发。
- 主要范围风险: 不应扩成村镇、市场、修复点或 NPC hub。
- 必须写回规格的调整: None。

## 1. 场景身份

- 场景目的: 提供玩家进入游戏和返回 Hub 外部的起点，让玩家看到云织号停泊在浮岛码头，并能通过空间锚点登船。
- 情绪目标: 清晨般安全、出发前的宁静、明确的登船期待。
- 服务的核心幻想 / 支柱: 飞艇是家，不只是载具；规划先于冒险；世界会回应照料。
- 3 秒识别: 玩家位于一座小型浮岛码头，能看到岛体、木栈道、停泊的云织号、登船坡道和水 / 云海边界。
- 本场景不是什么: 不是主菜单、不是航图 UI，也不是船内空间。

## 2. 场景物理合同

| 字段 | 内容 |
| --- | --- |
| 物理来源 | 运行时合同 + 作者化原型 / 实例数据 |
| 合同场景 ID | `hub_island_dock` |
| `physics_contract_complete` 状态 | 当前灰盒合同通过 |
| 场景物理类型 | `水平场景` |
| 移动平面 | `HubWalkBounds` 内的地面平面四方向移动 |
| Layer / Height Model | `hub_dock_ground` 活动地面；岛体、木栈道、停泊船体、水线和飞艇气囊高度标记 |
| Cutaway / Reveal Model | N/A true；当前切片没有可通行的物体背后路线 |
| 单位目录 | `HubRuntime.DebugScenePhysicsContract("hub_island_dock").scene_unit_catalog` |
| 单位原型 | `src/presentation/playable_slice_authored_content.json::scene_unit_prototypes` |
| 摆放实例 | `src/presentation/playable_slice_authored_content.json::scene_unit_instances` 中 `hub_island_dock` 过滤结果 |
| 碰撞 / 遮挡 / 比例 | #20 运行时合同 + 原型数据 |
| 特殊表面 / 动态行为 / 恢复规则 | 水线是玩法边界；登船坡道是 soft_overlap 逃逸 / 进入锚点；飞艇气囊是高度可读性标记 |

## 3. 进入 / 离开

- 进入来源: 新游戏开始、从船内离开、从探索 / 航行返回。
- 出生 / 抵达位置: `PlayerMarker` / 摆放实例 `scene_unit.instance.hub_island_dock.player_marker`。
- 离开或返回路径: `hub_boarding_ramp` 进入 `ship_interior_layered` / 运行时 `hub_ship_interior`。
- 取消 / 失败路径: 模态面板拥有焦点时，登船和移动输入被隔离。
- 存档状态返回行为: 持久化进度恢复当前屏幕、Hub 状态、玩家位置、货物 / 船体摘要和可用交互。
- 场景切换清理预期: 进入船内时隐藏外部码头单位；返回外部时恢复岛屿、码头和停泊船外观。

## 4. 空间布局

- 主视口构图: 岛体和木栈道在中下部，云织号停泊在右侧，登船坡道连接码头与船体，水 / 云海边界在底部。
- 可行走区域: `HubWalkBounds`。
- 边界: 岛屿上下边缘、水线、码头柱、停泊船体。
- 地标: 主岛体、木栈道、停泊船体、飞艇气囊、登船坡道。
- 交互锚点: 登船坡道。
- 遮挡风险: 停泊船体是 blocking_static；当前没有背后通行路线，不应通过隐藏船体来证明通行。
- 最低灰盒可读性要求: 不读 HUD 文本也能看出玩家在浮岛码头、云织号可登船、水线不可通行。

## 5. 关键路径

1. 玩家从初始岛屿 / Hub 外部开始。
2. 玩家观察停泊云织号和登船坡道。
3. 玩家靠近登船坡道并进入船内。

## 6. 可选内容 / 可读性节拍

- 可选观察点: 岛屿边缘、木栈道、停泊船体、飞艇气囊、水线。
- 本地身份细节: 浮岛绿色体块、码头木板、云织号靠岸、底部水 / 云海边界。
- 生活 / 修复 / 损伤痕迹: 后续可以把船体状态、货物回收、修复痕迹回写到外部船体表现。
- 嵌入世界中的玩家引导: 登船坡道、船体门、码头方向形成自然路径。
- UI 辅助: HUD 可以提示登船或当前状态，但不能替代可见登船坡道。

## 7. 状态变体

| 变体 | 触发 / 来源状态 | 世界 / 可玩场景证据 | 允许的 UI 辅助 |
| --- | --- | --- | --- |
| 初始 | 新游戏或普通返回 | 岛体、木栈道、停泊船体、登船坡道和水线可见 | 简短开始 / 登船提示 |
| 进展 / 完成 | 从探索返回或货物入库后 | 停泊船体仍在；后续可加入货物 / 船体状态痕迹 | 货物、船体、航线摘要 |
| 阻塞 / 异常 | 模态面板激活或输入焦点被 UI 占用 | 世界仍可见，但登船 / 移动输入被隔离 | 模态解释或禁用反馈 |

## 8. 交互合同

| 锚点 ID | 玩家动作 | 输入 / 焦点规则 | 领域负责人 | 禁用 / 失败反馈 | 世界证据 |
| --- | --- | --- | --- | --- | --- |
| `hub_boarding_ramp` | 登船进入船内 | 靠近 + 使用；被模态焦点阻止 | Hub / Player Movement | 模态焦点拥有输入时阻止 | 登船坡道、船体门、码头路径 |

## 9. 数据 / 运行时合同

- Godot 场景或运行时表面: `src/scenes/HubRuntime.cs`。
- 稳定 ID: `src/presentation/playable_slice_authored_content.json` 中的 `scene_unit_prototypes` 和 `scene_unit_instances`。
- 读取的领域管理器: Hub、Resources、ModuleHull、Session / Persistence。
- 会变更的领域管理器: 登船只通过现有 Hub 空间切换，不新增玩法权威。
- 持久化字段: 现有 playable-slice progress 和场景状态。
- 信号 / 语义事件: Hub 空间切换、保存 / 读取、登船提示、Hub 摘要同步。
- 焦点和模态边界: ADR-0012 仍是权威。
- 运行时 debug / smoke hook: `DebugScenePhysicsContract("hub_island_dock")`。

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 浮岛主地形 | 身份 / 边界 | 灰盒 | 美术 |
| P0 | 木栈道与登船坡道 | 交互 / 路径 | 灰盒标记 | 美术 |
| P0 | 停泊云织号外观 | 身份 / 进入船内 | 灰盒标记 | 美术 |
| P0 | 水线 / 云海边界 | 边界 / 反馈 | 灰盒 | 美术 |
| P1 | 外部环境音、木码头脚步、登船提示音 | 反馈 | fallback / 缺失 | 音频 |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 自动 smoke | `tests/smoke/session_shell_visual_probe.gd`; `production/qa/evidence/scene-unit-placement-initial-island-evidence.md` | PASS |
| 聚焦数据验证 | `tests/integration/playable-slice/DomainAdapterTest.csproj`; `production/qa/evidence/scene-unit-placement-initial-island-evidence.md` | PASS |
| 截图 / 视觉证明 | 既有 hub exterior visual probe 证据 | 待刷新 |
| Codex 审核 | 实现审查 | PASS |
| 用户可读性审核 | 本文件用户审核清单 | pending |

## 12. 用户审核清单

用户审核只判断玩家体验和设计方向，不需要逐项审查技术实现。

- [ ] 初始岛屿是否符合“安全起点、出发前宁静、云织号可登船”的体验。
- [ ] 玩家 3 秒内能否看出自己在浮岛码头，而不是主菜单或船内。
- [ ] 岛体、木栈道、停泊船体、登船坡道、水线是否都应该作为世界对象存在。
- [ ] 登船坡道的位置、层级、遮挡和交互关系是否合理。
- [ ] 不看 UI 时，玩家是否能知道可以靠近坡道登船。
- [ ] UI/HUD 是否只是辅助，没有替代初始岛屿场景本体。
- [ ] 需要调整的单位分类、摆放、节奏或缺失需求已记录回本规格。

用户审核结论: `PENDING`

用户备注:

- 待用户填写。

## 13. 就绪检查清单

- [x] 场景目的、循环角色和情绪目标明确。
- [x] 进入、离开、失败和返回路径明确。
- [x] 空间布局列出可行走区域、边界、地标和交互锚点。
- [x] Scene Physics Contract 已链接，并通过当前运行时合同。
- [x] 单位原型和摆放实例已链接。
- [x] 场景单位来自世界 / 可玩场景层，而不是 UI/HUD/按钮/标签/调试覆盖层。
- [x] 关键路径和可选可读性节拍已记录。
- [x] 至少三个状态变体已记录。
- [x] 交互锚点说明输入 / 焦点行为和领域负责人。
- [x] 运行时 / 状态合同没有创建新的玩法权威。
- [ ] P0 资产 / 音频需求已用最终资产解决。
- [ ] 截图证据和用户审核在实现后刷新。
