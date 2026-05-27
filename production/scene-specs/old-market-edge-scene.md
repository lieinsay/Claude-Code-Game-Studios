# 旧集市边缘场景规格

> **状态**: asset_gate
> **Scene ID**: `old_market_edge_scene`
> **最近更新**: 2026-05-27

## 0. 文件头

| 字段 | 内容 |
| --- | --- |
| Scene ID | `old_market_edge_scene` |
| 玩家可见场景名 | 旧集市边缘 |
| 所属循环节点 | Market |
| 当前生命周期状态 | `asset_gate` |
| 来源 GDD | `design/gdd/port-village-market.md` |
| 来源 story 或设计说明 | `production/session-state/active.md` 2026-05-24 场景集校正记录；`production/scene-specs/scene-coverage-registry.md` |
| 创建适合性人工审查 | `APPROVED_WITH_NOTES` |
| 创建审查记录 | 本文件第 13 节 |
| 最近更新日期 | 2026-05-27 |
| 负责人 | Codex |

## 1. 独立实现 / 资产边界

| 字段 | 内容 |
| --- | --- |
| 独立 Godot 场景 | `src/scenes/market/OldMarketEdgeScene.tscn` |
| 配套脚本 / runtime | `src/scenes/market/OldMarketEdgeScene.cs` |
| 作者化数据 | `src/presentation/playable_slice_authored_content.json` 的 `authored_scenes::old_market_edge_scene` 与 market scene-unit 原型 / 实例 |
| 资产组 | `.godot-ai/contracts/scene/old_market_edge_scene.contract.md` |
| 装配入口 | 本轮只建立独立资产和 #20 合同证据；不把 `route.market` 暴露到当前 `S4_chart` |
| 禁止混入位置 | 旧 `HubRuntime` 灰盒、HUD、按钮、标签、调试入口、`S9_market` 模态 |
| 独立性说明 | 场景本体以独立 `.tscn` 和脚本存在，大型 runtime 只能引用或读取其证据，不得把按钮或市场 UI 当作场景本体。 |

## 2. 场景身份

| 字段 | 内容 |
| --- | --- |
| 场景目的 | 作为后续 Settlement / Market 循环的世界空间入口，让玩家在摊位和关闭货架之间看见“村镇正在恢复”的状态。 |
| 情绪目标 | 旧港边缘、低活力、可恢复的生活痕迹。 |
| 服务的核心幻想 / 支柱 | 世界会回应照料；规划先于冒险。 |
| 玩家 3 秒内应理解 | 这里是一个可步行到摊位前的旧集市边缘，至少有一个默认开放摊位和若干待修复关闭摊位。 |
| 本场景不是什么 | 不是购买 UI、不是通用商店菜单、不是完整玻璃港城市场景、不是当前 `route.ochre` 替代目的地。 |

## 3. 场景物理合同

| 字段 | 内容 |
| --- | --- |
| 物理来源 | Runtime contract + Godot scene evidence |
| 合同场景 ID | `old_market_edge_scene` |
| `physics_contract_complete` 状态 | pass for asset-gate |
| 场景物理类型 | `水平场景` |
| 移动平面 | 单层地面，可四向移动；摊位和云海边界保持阻挡 / 软重叠语义。 |
| Layer / Height Model | `primary_walkable_layer=old_market_edge_ground_01`; walkable: `market_walk_path`; transition: `future_market_route_entry`; blocked: `market_cloudsea_boundary`, `closed_stall_body`; visual: `far_dock_silhouette` |
| Cutaway / Reveal Model | `N/A true`：外部水平场景，无室内剖切。 |
| 单位目录 | `playable_slice_authored_content.json` 中 market scene-unit 原型 / 实例。 |
| 固定单位原型 | 本规格内 asset-gate 原型：market plaza、walk path、general stall、closed stall、notice board、cloudsea boundary。 |
| 实体单位原型 | `production/unit-specs/dynamic-entities/player-controlled-entity.md` |
| 摆放实例 | `scene_unit.instance.old_market_edge_scene.*` |
| 碰撞 / 遮挡 / 比例 | `HubRuntime.DebugScenePhysicsContract("old_market_edge_scene")` |
| 特殊表面 / 动态行为 / 恢复规则 | 云海边界为 gameplay-affecting blocking boundary；摊位锚点为 trigger-only。 |
| 无玩法相关物理单位时的豁免原因 | `N/A true`：本场景有玩法相关物理单位。 |

## 4. 进入 / 离开

| 字段 | 内容 |
| --- | --- |
| 进入来源 | 后续 `route.market` / Settlement 入口；本轮不暴露到当前 S4 航图。 |
| 出生 / 抵达位置 | `WorldLayer/PlayerSpawn` |
| 离开或返回路径 | 后续市场 route / Hub return；本轮记录为 future route handoff。 |
| 取消 / 失败路径 | 若 route 未开放，当前 Chart 不显示旧集市路线。 |
| 存档状态返回行为 | 后续由 SettlementManager + Persistence 负责；本轮不新增存档字段。 |
| 场景切换清理预期 | 关闭市场 UI 时保留世界摊位状态，场景退出不得绕过 SettlementManager。 |

## 5. 空间布局

| 字段 | 内容 |
| --- | --- |
| 主视口构图 | 旧港灰蓝背景、集市广场地面、中央步行路径、左侧开放杂货摊、中央关闭摊位、右侧公告板。 |
| 可行走区域 | `MarketPlazaGround` 与 `MarketWalkPath`；初始 player spawn 在左下路径。 |
| 边界 | `MarketCloudSeaBoundary`、关闭摊位遮挡体、摊位柜台。 |
| 地标 | 开放摊位、关闭摊位、公告板、远处码头剪影。 |
| 交互锚点 | `GeneralStallAnchor`。 |
| 遮挡风险 | 摊位柜体不隐藏可通行 behind-path；互动只通过 soft overlap。 |
| 最低灰盒可读性要求 | 不看文字也能分辨可交易摊位、关闭摊位、公告板和不可通行云海边界。 |

## 6. 关键路径

| 步骤 | 场景动作 | 世界锚点 | 预期结果 |
| --- | --- | --- | --- |
| 1 | 抵达旧集市边缘 | `PlayerSpawn` | 玩家站在市场路径上。 |
| 2 | 靠近默认摊位 | `GeneralStallAnchor` | 后续可打开 `S9_market`，本轮仅证明世界锚点存在。 |
| 3 | 观察关闭摊位 / 公告板 | `ClosedStallBody`, `MarketNoticeBoard` | 读出修复驱动的未来状态变化空间。 |

## 7. 可选内容 / 可读性节拍

| 类型 | 内容 |
| --- | --- |
| 可选观察点 | 公告板和关闭摊位表现市场待恢复状态。 |
| 本地身份细节 | 旧港摊位、修复告示、远码头剪影。 |
| 生活 / 修复 / 损伤痕迹 | 关闭摊位 shutter 与公告板维修痕迹。 |
| 嵌入世界中的玩家引导 | `GeneralStallAnchor` 对应摊位位置。 |
| UI 辅助 | 后续 `S9_market` 只能作为购买确认，不是场景证据。 |

## 8. 状态变体

| 变体 | 触发 / 来源状态 | 世界 / 可玩场景证据 | 允许的 UI 辅助 |
| --- | --- | --- | --- |
| 初始 / 休眠 | `SettlementState.Dormant` | 一个开放摊位 + 一个关闭摊位。 | `S9_market` 可显示默认摊位商品。 |
| 恢复中 | repair node 完成后 future update | 关闭摊位可切换为开放摊位。 | 市场提示可说明新摊位开放。 |
| 阻塞 / 未开放 | route 或摊位不可用 | 当前 Chart 不显示旧集市路线，关闭摊位保持实体遮挡。 | UI 可显示路线未开放或摊位关闭。 |

## 9. 交互合同

| 锚点 ID | 玩家动作 | 输入 / 焦点规则 | 领域负责人 | 禁用 / 失败反馈 | 世界证据 |
| --- | --- | --- | --- | --- | --- |
| `general_stall_anchor` | Use 打开摊位 | 必须靠近 soft overlap；UI 焦点只在模态打开后生效。 | `SettlementManager` + `UIManager` | 摊位关闭或系统不可用时不执行购买。 | `WorldLayer/GeneralStallAnchor` |

## 10. 数据 / 运行时合同

- Godot 场景或运行时表面: `src/scenes/market/OldMarketEdgeScene.tscn`, `src/scenes/market/OldMarketEdgeScene.cs`
- 稳定 ID: `old_market_edge_scene`, `old_market_edge_ground_01`, `general_stall_anchor`
- 读取的领域管理器: `SettlementManager`, `ResourcesManager`（后续 route / UI 接入时）
- 会变更的领域管理器: `N/A true`，本轮资产不写玩法状态
- 持久化字段: `N/A true`，后续由 `progress.settlement-market` 负责
- 信号 / 语义事件: 后续 `ui_purchase_confirmed`
- 焦点和模态边界: 世界锚点不抢 UI 焦点；购买 UI 打开后由 UIManager 管理
- 运行时 debug / smoke hook: `DebugScenePhysicsContract("old_market_edge_scene")`
- 不允许写入的状态: 场景脚本不得直接扣货币、加库存、解锁摊位或写存档

## 11. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 市场广场地面、路径、开放摊位、关闭摊位、公告板、云海边界 | identity / interaction / state_variant | Godot 灰盒资产 | Art |
| P1 | NPC idle、购买确认音效、市场环境声 | feedback / state_variant | missing | Audio / Art |

## 12. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 自动 smoke | `dotnet run --project tests/integration/playable-slice/DomainAdapterTest.csproj`; Godot AI MCP scene open / hierarchy | pending |
| 截图 / 视觉证明 | 非 headless 截图包 | pending |
| Codex 审核 | `.godot-ai/reviews/scene/old_market_edge_scene.review.md` | pass-with-scope |
| 后续反馈记录 | `directed-content-modification` | N/A true |

## 13. 创建适合性记录

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | scene |
| 稳定 ID 或拟定 ID | `old_market_edge_scene` |
| 人工审查人 | user |
| 审查日期 | 2026-05-24 |
| 结论 | `APPROVED_WITH_NOTES` |
| 必须回写的备注 | 旧集市边缘属于当前场景集校正来源，但 2026-05-27 之后不作为当前 demo 第二岛屿；本轮只建立 future market asset-gate，不暴露到 S4_chart。 |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 市场 GDD 已批准，SettlementManager 已完成，缺少世界空间证据。
- 不复用已有场景 / UI / 单位的原因: 市场需要摊位、关闭状态和修复痕迹，不能由 `S9_market` 或 Chart 按钮替代。
- 主要范围风险: 过早暴露 `route.market` 会与赭石岛正式路线冲突。
- 必须写回规格的调整: 当前只做 asset-gate；route / purchase UI 接入留给后续。

## 14. 后续反馈与定向修改

| 字段 | 内容 |
| --- | --- |
| 创建适合性结论 | `APPROVED_WITH_NOTES` |
| 保持可修改状态 | `true` |
| 定向修改入口 | `directed-content-modification` |
| 用户反馈 / 后续定向修改需求 | None |

## 15. 就绪检查清单

- [x] 场景目的、循环角色和情绪目标明确。
- [x] 进入、离开、失败和返回路径明确，但 route 接入留作后续。
- [x] 空间布局列出可行走区域、边界、地标和交互锚点。
- [x] Scene Physics Contract 已链接。
- [x] 固定单位与实体单位已分开引用。
- [x] 场景单位来自世界 / 可玩场景层，而不是 UI/HUD/按钮/标签/调试覆盖层。
- [x] 关键路径和可选可读性节拍已记录。
- [x] 至少三个状态变体已记录。
- [x] 交互锚点说明输入 / 焦点行为和领域负责人。
- [x] 运行时 / 状态合同没有创建新的玩法权威。
- [x] P0 资产 / 音频需求可追溯到身份、交互、状态或反馈。
- [x] 自动证据、截图证据和规格一致性检查路径已命名。
