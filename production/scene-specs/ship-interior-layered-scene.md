# 云织号船内分层场景规格

> **Scene ID**: `ship_interior_layered`
> **运行时合同 ID**: `hub_ship_interior`
> **状态**: spec_drafted
> **负责人**: Scene Composition System (#19) + Scene Physics Unit System (#20)
> **最后更新**: 2026-05-24

## 0. 文件头

| 字段 | 内容 |
| --- | --- |
| Scene ID | `ship_interior_layered` |
| 玩家可见场景名 | 云织号船内分层场景 |
| 所属循环节点 | Hub / Chart |
| 当前生命周期状态 | `spec_drafted` |
| 来源 GDD | `design/gdd/scene-composition-system.md`; `design/gdd/scene-physics-unit-system.md`; `design/gdd/airship-hub.md` |
| 来源 story 或设计说明 | `N/A true` |
| 创建适合性人工审查 | `APPROVED_WITH_NOTES` |
| 创建审查记录 | 本文件“创建适合性人工审查”和“创建适合性记录” |
| 最近更新日期 | 2026-05-24 |
| 负责人 | Codex / user / QA |

## 创建适合性人工审查

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `ship_interior_layered` |
| 人工审查人 | 用户 |
| 审查日期 | 2026-05-24 |
| 结论 | `APPROVED_WITH_NOTES` |
| 必须回写的备注 | 长期场景定义应包含完整多层船舱；完整维修系统、模块替换玩法、复杂 NPC 船员、未审查战斗或宿舍生活系统只是本轮排除，后续仍需开发。 |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 船内承载航图、仓储、返航和状态读法，是从岛屿进入航行前的核心空间。
- 不复用已有场景 / UI / 单位的原因: HUD 面板不能替代驾驶舱、货舱、引擎区、出口等世界空间。
- 主要范围风险: 本轮 runtime 替换不得一次实现完整维修、模块替换、复杂 NPC、战斗或宿舍生活系统。
- 必须写回规格的调整: 长期规格要保留完整多层船舱目标；本轮只实现可支撑航图台、货舱、出口和移动的最小船内空间。

## 独立实现 / 资产边界

| 字段 | 内容 |
| --- | --- |
| 独立 Godot 场景 | `pending`；当前灰盒由 `src/scenes/HubRuntime.*` 装配，不能作为新增永久船内边界。 |
| 配套脚本 / runtime | 当前为 `src/scenes/HubRuntime.cs`；后续独立化需拆出 `ship_interior_layered` 专属运行时或数据驱动入口。 |
| 作者化数据 | `src/presentation/playable_slice_authored_content.json` 中 `hub_ship_interior` 原型与实例。 |
| 资产组 | 船体剖切、驾驶舱、货舱、引擎区、航图台、出口阈值、储物箱。 |
| 装配入口 | Hub shell 只负责挂载或切换，不拥有船内场景本体规则。 |
| 禁止混入位置 | 不得把新增船内规则继续散落进旧 `HubRuntime.tscn`、`HubRuntime.cs` 或 UI 容器。 |
| 删除旧节点要求 | 若替换旧 Godot 节点，删除前必须列出节点路径并询问用户；当前为 `N/A true`。 |

## 1. 场景身份

- 场景目的: 让玩家把云织号读成一个像家的飞艇内部：航线规划、仓储、引擎状态和离开路径都存在于物理空间里。
- 情绪目标: 安全、有生活痕迹、可修复、可读。
- 服务的核心幻想 / 支柱: 飞艇是家，不只是载具；规划先于冒险；世界会回应照料。
- 3 秒识别: 玩家位于飞艇内部，能看到驾驶舱、货舱、引擎区、出口和前景船体结构；长期方向应读出完整多层船舱而不是单层面板。
- 本场景不是什么: 不是 HUD 仪表盘、航线菜单，也不是最终美术完成声明。

## 2. 场景物理合同

| 字段 | 内容 |
| --- | --- |
| 物理来源 | 运行时合同 + 作者化原型 / 实例数据 |
| 合同场景 ID | `hub_ship_interior` |
| `physics_contract_complete` 状态 | 当前灰盒合同通过 |
| 场景物理类型 | 当前运行时标记为 `垂直场景`；设计权威按分层船内场景追踪，等待后续水平分层重分类 |
| 移动平面 | 左 / 右为主，带房间深度和未来垂直连接点 |
| Layer / Height Model | `ship_deck_01` 活动地面；驾驶舱 / 货舱 / 引擎舱是中景房间尺度对象 |
| Cutaway / Reveal Model | `front_wall_removed + active_floor_focus`；上层船体前墙是前景遮挡物 |
| 单位目录 | `HubRuntime.DebugScenePhysicsContract("hub_ship_interior").scene_unit_catalog` |
| 单位原型 | `src/presentation/playable_slice_authored_content.json::scene_unit_prototypes` |
| 摆放实例 | `src/presentation/playable_slice_authored_content.json::scene_unit_instances` 中 `hub_ship_interior` 过滤结果 |
| 碰撞 / 遮挡 / 比例 | #20 运行时合同 + 原型数据 |
| 特殊表面 / 动态行为 / 恢复规则 | 驾驶舱玻璃是 `visual_only_glass`；静态阻挡和触发锚点使用现有优先级 / 恢复规则 |

## 3. 进入 / 离开

- 进入来源: 从初始岛屿 / Hub 外部登船。
- 出生 / 抵达位置: `ShipInteriorPlayerStart` / 摆放实例 `scene_unit.instance.hub_ship_interior.player_marker`。
- 离开或返回路径: `ship_exit_threshold` 摆放实例返回外部 / Hub 流程。
- 取消 / 失败路径: 模态面板拥有焦点时，输入门禁阻止场景切换。
- 存档状态返回行为: `PlayableSliceSceneState` 恢复屏幕、航线、探索步骤、玩家位置和页脚。
- 场景切换清理预期: 不允许残留航图 / 探索面板被当成物理场景证据。

## 4. 空间布局

- 主视口构图: 船体和三个房间尺度区域横向铺在可玩层中。
- 可行走区域: `ShipInteriorWalkBounds`。
- 边界: 船体轮廓、上层前墙、驾驶舱玻璃、房间舱室。
- 地标: 驾驶舱、货舱、引擎舱。
- 交互锚点: 舵台控制台、储物箱、出口阈值。
- 遮挡风险: 上层前墙和驾驶舱玻璃不能超过 #20 限制而遮住玩家或核心锚点。
- 最低灰盒可读性要求: 不读 HUD 文本也能区分驾驶舱 / 货舱 / 引擎区。

## 5. 关键路径

1. 从码头 / 外部进入船内。
2. 靠近驾驶舱、货舱或引擎锚点。
3. 使用舵台 / 出口 / 储物交互，或返回外部流程。

## 6. 可选内容 / 可读性节拍

- 可选观察点: 驾驶舱窗、货物装载显示、引擎磨损覆盖层。
- 本地身份细节: 船体轮廓、房间舱室形状、前景墙剖切。
- 生活 / 修复 / 损伤痕迹: 货物装载和引擎磨损状态 hook。
- 嵌入世界中的玩家引导: 控制台、箱子、出口阈值和房间舱室作为空间锚点。
- UI 辅助: HUD 可以总结仓储 / 船体 / 航线状态，但不能计入场景单位。

## 7. 状态变体

| 变体 | 触发 / 来源状态 | 世界 / 可玩场景证据 | 允许的 UI 辅助 |
| --- | --- | --- | --- |
| 初始 | 开局或正常进入 Hub | 船体、驾驶舱、货舱、引擎、舵台、箱子、出口单位可见 | 仅短提示 / 状态 |
| 进展 / 完成 | 获得货物或规划航线后 | 货物装载填充 / 储物箱状态 hook；舵台仍是航线锚点 | 货物 / 船体数字 |
| 阻塞 / 异常 | 船体受损或输入模态激活 | 引擎磨损覆盖层 / 航线使用禁用反馈 | 允许模态解释 |

## 8. 交互合同

| 锚点 ID | 玩家动作 | 输入 / 焦点规则 | 领域负责人 | 禁用 / 失败反馈 | 世界证据 |
| --- | --- | --- | --- | --- | --- |
| `helm_console_prop` | 打开航图 / 航线规划 | 靠近 + 使用；被模态焦点阻止 | Chart / Hub | 航线不可用反馈 | 舵台控制台实例 |
| `storage_crate_prop` | 读取货物 / 仓储状态 | 靠近 + 使用，或被动可读状态 | Resources | 容量反馈 | 储物箱实例 |
| `ship_exit_threshold` | 离开船内 | 靠近 + 使用 | Hub | 模态焦点拥有输入时阻止 | 出口阈值实例 |

## 9. 数据 / 运行时合同

- Godot 场景或运行时表面: `src/scenes/HubRuntime.cs`。
- 稳定 ID: `src/presentation/playable_slice_authored_content.json` 中的 `scene_unit_prototypes` 和 `scene_unit_instances`。
- 读取的领域管理器: 通过现有 `PlayableSliceDomainAdapter` 读取 Hub、Chart、Resources、ModuleHull。
- 会变更的领域管理器: 场景单位作者数据不变更任何领域管理器。
- 持久化字段: 不新增持久化玩法权威。
- 信号 / 语义事件: 现有航线、货物、保存 / 读取和 Hub 信号。
- 焦点和模态边界: ADR-0012 仍是权威。
- 运行时 debug / smoke hook: `DebugScenePhysicsContract("hub_ship_interior")`。

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 飞艇内部背景 | 身份 | 灰盒 | 美术 |
| P0 | 舵台控制台 | 交互 | 灰盒标记 | 美术 |
| P0 | 储物箱 | 状态 / 交互 | 灰盒标记 | 美术 |
| P0 | 引擎台 / 磨损覆盖层 | 状态 | 灰盒覆盖层 | 美术 |
| P1 | 舱室氛围音 | 反馈 | fallback / 缺失 | 音频 |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 自动 smoke | `tests/smoke/session_shell_visual_probe.gd` | 已通过，等待截图刷新 |
| 聚焦数据验证 | `tests/integration/playable-slice/DomainAdapterTest.csproj` | 已通过 |
| 截图 / 视觉证明 | 既有 visual probe 证据 | 待刷新 |
| Codex 审核 | 实现审查 | 数据链路通过 |
| 后续反馈记录 | `directed-content-modification` 需求记录 | pending |

## 13. 创建适合性记录

本文件顶部“创建适合性人工审查”是权威记录；本节用于对齐当前场景模板。

- 审查问题: 是否应该创建 `ship_interior_layered`，而不是用 HUD、航图 UI 或单个控制台替代。
- 用户结论: `APPROVED_WITH_NOTES`
- 用户要求: 长期保留完整多层船舱目标；本轮只实现最小船内空间。
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
- [ ] 截图证据和规格一致性检查在实现后刷新。
