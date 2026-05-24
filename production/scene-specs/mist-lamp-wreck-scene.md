# 雾灯残骸场景规格

> **Scene ID**: `mist_lamp_wreck_scene`
> **运行时合同 ID**: `exploration_mist_island`
> **状态**: spec_drafted
> **负责人**: Scene Composition System (#19) + Scene Physics Unit System (#20)
> **最后更新**: 2026-05-24

## 1. 场景身份

- 场景目的: 提供第一个探索目的地，让玩家读到具体残骸、搜索它、看到压力升级，并返回飞艇。
- 情绪目标: 安静的不确定感、专注打捞、清晰的撤退判断。
- 服务的核心幻想 / 支柱: 未知带来温和压力；规划先于冒险；世界会回应照料。
- 3 秒识别: 玩家位于雾气中的浮岛，能看到残骸、线索、返航船、雾 / 水边界和威胁 / 准备标记。
- 本场景不是什么: 不是通用雾场、纯文字搜索菜单，也不是最终探索系统。

## 2. 场景物理合同

| 字段 | 内容 |
| --- | --- |
| 物理来源 | 运行时合同 + 作者化原型 / 实例数据 |
| 合同场景 ID | `exploration_mist_island` |
| `physics_contract_complete` 状态 | 当前灰盒合同通过 |
| 场景物理类型 | `水平场景` |
| 移动平面 | `ExplorationWalkBounds` 内的地面平面四方向移动 |
| Layer / Height Model | `mist_wreck_ground_01` 活动地面；岛、路径、残骸、船、雾 / 水边界和警告标记 |
| Cutaway / Reveal Model | 当前切片为 N/A true；没有可通行的物体背后路线 |
| 单位目录 | `HubRuntime.DebugScenePhysicsContract("exploration_mist_island").scene_unit_catalog` |
| 单位原型 | `src/presentation/playable_slice_authored_content.json::scene_unit_prototypes` |
| 摆放实例 | `src/presentation/playable_slice_authored_content.json::scene_unit_instances` 中 `exploration_mist_island` 过滤结果 |
| 碰撞 / 遮挡 / 比例 | #20 运行时合同 + 原型数据 |
| 特殊表面 / 动态行为 / 恢复规则 | 雾海边界影响玩法；雾和高度标记是视觉 / 可读性策略；威胁区是动态警告证据 |

## 3. 进入 / 离开

- 进入来源: 从 `voyage_open_world_scene` 抵达，或由当前可玩航线结算进入。
- 出生 / 抵达位置: `ExplorationPlayerStart` / 摆放实例 `scene_unit.instance.exploration_mist_island.player_marker`。
- 离开或返回路径: 返航船上的 `return_helm_anchor`；先预热，再驾驶返回。
- 取消 / 失败路径: 搜索和返回交互受距离、模态 / 输入焦点门禁限制。
- 存档状态返回行为: 持久化进度恢复探索步骤、航线、玩家状态、携带奖励和最后搜索点。
- 场景切换清理预期: 返回 Hub 时隐藏探索面板和世界单位。

## 4. 空间布局

- 主视口构图: 雾地平线和海面边界在后方，浮岛路径居中，右侧是残骸，左侧是返航船。
- 可行走区域: `ExplorationWalkBounds`。
- 边界: 悬崖边、雾海边界、岛屿上 / 下边缘。
- 地标: 残骸主体、桅杆、返航船船体、返航信标、威胁区。
- 交互锚点: 搜索残骸和返航舵点。
- 遮挡风险: 残骸和返航船是阻挡 / 可读性对象；当前版本没有背后可通行路线，因此 behind-object reveal 为 N/A true。
- 最低灰盒可读性要求: 不读 HUD 文本也能区分残骸 / 搜索区、返航船、水边界和威胁标记。

## 5. 关键路径

1. 从航线流程抵达雾灯残骸场景。
2. 靠近残骸，完成三阶段扫描 / 回声 / 打捞搜索。
3. 移动到返航船，使用返航舵点预热并返回。

## 6. 可选内容 / 可读性节拍

- 可选观察点: 桅杆、线索碎片、扫描弧、返航信标、威胁区。
- 本地身份细节: 雾地平线、浮岛、悬崖边、残骸剪影。
- 生活 / 修复 / 损伤痕迹: 搜索压力后的威胁区和船体 / 货物反馈。
- 嵌入世界中的玩家引导: 搜索光、残骸高亮、返航船舵、信标光束。
- UI 辅助: 航线 / 资源 / 威胁 / 船体标签可以总结状态，但不能计入场景单位。

## 7. 状态变体

| 变体 | 触发 / 来源状态 | 世界 / 可玩场景证据 | 允许的 UI 辅助 |
| --- | --- | --- | --- |
| 初始 | 抵达但尚未搜索 | 残骸、桅杆、线索、返航船、雾和水边界可见 | 航线和威胁摘要 |
| 进展 / 完成 | 搜索步骤推进 | 脉冲填充、威胁区、货物道具、信标 / 返回状态 | 搜索步骤和货物数字 |
| 阻塞 / 异常 | 未靠近锚点或保存 / 读取模态拥有焦点 | 交互提示缺失 / 禁用；返航预热保持阶段化 | 禁用 / 失败反馈 |

## 8. 交互合同

| 锚点 ID | 玩家动作 | 输入 / 焦点规则 | 领域负责人 | 禁用 / 失败反馈 | 世界证据 |
| --- | --- | --- | --- | --- | --- |
| `search_wreck_prop` | 扫描 / 回声 / 打捞 | 靠近 + 使用；三阶段表现门禁 | Exploration / Resources / Threat | 距离过远时直接命令被阻止 | 残骸道具、桅杆、扫描弧、线索碎片 |
| `return_helm_anchor` | 预热 / 驾驶返回 | 靠近 + 使用；两阶段返回门禁 | Hub / Navigation / Persistence | 预热完成前玩家留在场景内 | 返航船船体和舵点 |

## 9. 数据 / 运行时合同

- Godot 场景或运行时表面: `src/scenes/HubRuntime.cs`。
- 稳定 ID: `src/presentation/playable_slice_authored_content.json` 中的 `scene_unit_prototypes` 和 `scene_unit_instances`。
- 读取的领域管理器: 通过现有 `PlayableSliceDomainAdapter` 读取 Navigation、Exploration、Resources、ModuleHull、Hub。
- 会变更的领域管理器: 不新增玩法权威；现有 `AdvanceExploration()` 和 `ReturnToHub()` 仍是权威。
- 持久化字段: 现有持久化进度和 playable-slice 快照。
- 信号 / 语义事件: 搜索、压力、返回、保存 / 读取、Hub 摘要同步。
- 焦点和模态边界: ADR-0012 仍是权威。
- 运行时 debug / smoke hook: `DebugScenePhysicsContract("exploration_mist_island")`。

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 雾岛 / 残骸背景 | 身份 | 灰盒 | 美术 |
| P0 | 残骸、桅杆、线索碎片 | 交互 | 灰盒标记 | 美术 |
| P0 | 返航飞艇和舵点 | 离开 / 返回 | 灰盒标记 | 美术 |
| P0 | 威胁区 / 警告覆盖层 | 压力 | 灰盒覆盖层 | 美术 |
| P1 | 雾 / 残骸氛围音和扫描提示 | 反馈 | fallback / 缺失 | 音频 |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 自动 smoke | `tests/smoke/session_shell_visual_probe.gd`; `production/qa/evidence/scene-unit-placement-mist-lamp-wreck-evidence.md` | PASS |
| 聚焦数据验证 | `tests/integration/playable-slice/DomainAdapterTest.csproj`; `production/qa/evidence/scene-unit-placement-mist-lamp-wreck-evidence.md` | PASS |
| 截图 / 视觉证明 | 既有 visual probe 证据 | 待刷新；当前 headless 运行因显示驱动限制跳过截图 |
| Codex 审核 | 实现审查 | 追踪性 / 数据链路 PASS |
| 用户可读性审核 | `production/playtests/scene-readability-mist-lamp-wreck.md` 与本文件用户审核清单 | pending |

## 12. 用户审核清单

用户审核只判断玩家体验和设计方向，不需要逐项审查技术实现。

- [ ] 雾灯残骸是否符合“安静不确定、打捞、可撤退判断”的体验。
- [ ] 玩家 3 秒内能否看出自己在雾中浮岛和残骸现场。
- [ ] 残骸、桅杆、线索、返航船、雾 / 水边界、威胁区是否都应该作为世界对象存在。
- [ ] 搜索锚点和返航舵点的位置、层级、遮挡和交互关系是否合理。
- [ ] 不看 UI 时，玩家是否能知道要搜索残骸、看到压力变化、找到返航船。
- [ ] UI/HUD 是否只是辅助，没有替代残骸场景本体。
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
