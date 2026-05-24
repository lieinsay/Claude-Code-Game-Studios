# 当前可玩切片运行时拆分创建审查包

> **日期**: 2026-05-24  
> **状态**: `PENDING_HUMAN_REVIEW`  
> **用途**: 为下一轮 Godot runtime 替换准备人工适合性审查材料。  
> **适用门禁**: `production/content-creation-review-gate.md`  
> **重要边界**: 本文件不是场景 / UI / 单位规格批准书；没有 `APPROVED` 或 `APPROVED_WITH_NOTES` 前，不得新增对应 runtime 节点、Godot 场景、UI 表面、`scene_unit.prototype.*` 或作者化实例。

## 1. 拆分目标

当前游戏运行时应从旧的 `HubRuntime.tscn` / `HubRuntime.cs` 面板与手写节点混合体，拆成下列可审核对象：

| 顺序 | 对象 | 类型 | 拟定稳定 ID | 当前来源 / 状态 | 本轮是否可实现 |
| --- | --- | --- | --- | --- | --- |
| 1 | 初始岛屿场景 | `scene` | `initial_island_scene` | 已有 `spec_drafted`，用户可读性审核 pending | 否，等待人工确认 |
| 2 | 停在岛上的飞船实体 | `unit` | `scene_unit.prototype.hub_docked_ship_hull` + `scene_unit.prototype.hub_airship_envelope` | 已在作者化单位表中存在 | 否，等待本拆分方向确认 |
| 3 | 可移动玩家操作实体 | `unit` | `scene_unit.prototype.player_marker` | 已在作者化实体表中存在 | 否，等待本拆分方向确认 |
| 4 | 进入飞船后的船内场景 | `scene` | `ship_interior_layered` | 已有 `spec_drafted`，用户可读性审核 pending | 否，等待人工确认 |
| 5 | 船内驾驶舱 / 舵台舱可触发航图 UI 的航图台固定单位 | `unit` | `scene_unit.prototype.helm_console` | 已在作者化单位表中存在 | 否，等待本拆分方向确认 |
| 6 | 触发航图台后打开的航图 UI | `ui` | `S4_chart`，后续可拆 `chart-full-screen-surface.md` | 已在 UI 表面总表登记 | 否，等待是否拆独立 UI 规格的人工确认 |
| 7 | 飞船起飞后的航行大场景，包含所有岛屿场景入口 / 远景 / 航线 | `scene` | `voyage_open_world_scene` | 已有 `spec_drafted`，#20 合同 pending | 否，等待人工确认和 #20 合同 |
| 8 | 其他岛屿场景 A：雾灯残骸浮岛 | `scene` | `mist_lamp_wreck_scene` | 已有 `spec_drafted`，用户可读性审核 pending | 否，等待人工确认 |
| 9 | 其他岛屿场景 B：旧集市边缘或替代岛屿候选 | `scene` | `old_market_edge_scene` 或待定新 ID | `old_market_edge_scene` 仍是 `tracked-gap` | 否，必须先人工决定是否纳入当前 demo |

## 2. 本轮建议的人工审查问题

这组审查不是问“能不能做得出来”，而是问“是否应该进入当前项目和当前 demo”。建议人类 reviewer 按下表给出 `APPROVED` / `APPROVED_WITH_NOTES` / `REVISE` / `REJECTED`。

| 对象 | 关键人工判断 |
| --- | --- |
| 初始岛屿 | 是否就是当前 demo 的唯一安全起点；3 秒内是否必须读出浮岛、码头、停靠飞船和登船路径。 |
| 停靠飞船实体 | 是否应作为初始岛屿的世界实体，而不是背景图或 UI 入口；是否需要拆成船体、气囊、坡道等多个固定单位。 |
| 玩家实体 | 当前 `player_marker` 是否足以代表可移动玩家；是否需要更正式的 player actor 原型、动画状态或碰撞体。 |
| 船内场景 | 船内是否是独立可进入场景；驾驶舱、货舱、引擎区是否都应作为物理空间而不是 HUD 区块。 |
| 航图台固定单位 | 航图 UI 是否必须由船内真实世界锚点触发；`helm_console` 是否就是航图台，还是需要独立 `chart_table` 原型。 |
| 航图 UI | `S4_chart` 是否需要拆独立 UI 规格；是否全屏接管输入；是否只做航线选择而不替代航行大场景。 |
| 航行大场景 | 是否承担“所有岛屿都在同一个大航行场景内被看见 / 接近 / 抵达”的职责；第一版是否允许只实现有限航线。 |
| 雾灯残骸浮岛 | 是否继续作为第一条其他岛屿；搜索 / 返航 / 威胁是否都应属于该场景内的单位和行为。 |
| 第二其他岛屿 | 是否选择旧集市边缘，还是先定义一个更小、更适合当前 demo 的第二岛屿；若选择旧集市，市场摊位、NPC、货物和入口会显著扩大范围。 |

## 3. 创建适合性人工审查记录草案

### 3.1 初始岛屿场景

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `initial_island_scene` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 当前 demo 需要一个非 UI 的安全起点，让玩家看到浮岛、码头、停靠飞船和登船路径。
- 不复用已有场景 / UI / 单位的原因: 主菜单或 HUD 不能证明世界起点；船内也不能替代外部出发空间。
- 主要范围风险: 若加入额外 NPC、市场或修复点，会把初始岛屿扩大成未审查 hub。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.2 停靠飞船实体

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `unit` |
| 稳定 ID 或拟定 ID | `scene_unit.prototype.hub_docked_ship_hull`; `scene_unit.prototype.hub_airship_envelope` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 飞船必须在初始岛屿上被读成真实停靠实体，支撑“飞艇是家，不只是载具”。
- 不复用已有场景 / UI / 单位的原因: 出航按钮或航图 UI 不能替代停靠船体。
- 主要范围风险: 若拆成过多可交互部件，会提前引入维修 / 模块玩法。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.3 可移动玩家实体

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `unit` |
| 稳定 ID 或拟定 ID | `scene_unit.prototype.player_marker` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 所有可进入场景都需要一个可移动、可定位、可触发空间交互的玩家实体。
- 不复用已有场景 / UI / 单位的原因: UI 光标或摄像机位置不能证明玩家在世界中存在。
- 主要范围风险: 若现在要求完整角色动画 / 装备 / 战斗碰撞，会超出当前 runtime 替换范围。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.4 飞船内部场景

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `ship_interior_layered` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 船内承载航图、仓储、返航和状态读法，是从岛屿进入航行前的核心空间。
- 不复用已有场景 / UI / 单位的原因: HUD 面板不能替代驾驶舱、货舱、引擎区和出口这些世界空间。
- 主要范围风险: 多层船舱、维修系统和完整房间切换会扩大 #20 合同复杂度。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.5 航图台固定单位

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `unit` |
| 稳定 ID 或拟定 ID | `scene_unit.prototype.helm_console` 或待定 `scene_unit.prototype.chart_table` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 航图 UI 必须由船内可见、可接近、可理解的世界锚点触发。
- 不复用已有场景 / UI / 单位的原因: 快捷键 `M` 可作为辅助入口，但不能证明航图台这个世界对象存在。
- 主要范围风险: 若新增 `chart_table` 原型，需要补单位规格、作者化数据和场景摆放；若复用 `helm_console`，需确认语义是否足够清楚。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.6 航图 UI

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `ui` |
| 稳定 ID 或拟定 ID | `S4_chart`; 建议独立文件 `production/ui-specs/chart-full-screen-surface.md` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 玩家需要在出航前读路线、选择目的地、理解风险和确认离港。
- 不复用已有场景 / UI / 单位的原因: `S4_chart` 已存在总表规格，但若要替代旧 `ChartPanel`，需要更明确的全屏表面输入 / 焦点 / 世界锚点细节。
- 主要范围风险: 航图 UI 不能承包航行大场景；它只负责选择和确认，不替代飞船起飞后的可玩航行。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.7 航行大场景

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `voyage_open_world_scene` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 起飞后需要一个可玩、可读、可验证的航行空间，把岛屿之间的移动从按钮跳转变成世界行动。
- 不复用已有场景 / UI / 单位的原因: 航图 UI、进度条或探索 HUD 不能证明飞船正在穿越空海。
- 主要范围风险: “包含所有岛屿”容易膨胀；第一版应限定可见 / 可抵达岛屿数量和航线问题窗口。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.8 雾灯残骸浮岛

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `mist_lamp_wreck_scene` |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 当前搜索 / 返航 / 威胁闭环已经围绕雾灯残骸建立，适合作为第一条其他岛屿。
- 不复用已有场景 / UI / 单位的原因: 探索 HUD 不能替代残骸、路径、威胁区、返航船体这些世界单位。
- 主要范围风险: 若加入太多探索点或战斗内容，会超出第一版搜索闭环。
- 必须写回规格的调整: 等待人工 reviewer。

### 3.9 第二其他岛屿

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `scene` |
| 稳定 ID 或拟定 ID | `old_market_edge_scene` 或待定新 ID |
| 人工审查人 | PENDING |
| 审查日期 | PENDING |
| 结论 | `PENDING` |
| 必须回写的备注 | PENDING |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 当前理论拆分需要至少两个其他岛屿，第二岛屿可以证明航行大场景不是单一路线。
- 不复用已有场景 / UI / 单位的原因: 第二目的地必须是可进入世界场景；不能只靠航图路线或市场 UI 表示。
- 主要范围风险: 若选择旧集市边缘，会同时引入市场边缘、摊位、NPC、货物、购买 UI 和 #20 物理合同，范围明显大于一个小型岛屿。
- 必须写回规格的调整: 等待人工 reviewer 决定是提升 `old_market_edge_scene`，还是定义更小的第二岛屿候选。

## 4. 建议审批顺序

1. 先审并确认现有三件基础对象: `initial_island_scene`、`ship_interior_layered`、`scene_unit.prototype.player_marker`。
2. 再审船体 / 航图台锚点: `hub_docked_ship_hull`、`hub_airship_envelope`、`helm_console` 或 `chart_table`。
3. 再审 `S4_chart` 是否需要拆独立 UI 规格，以及它与航图台的触发关系。
4. 再审 `voyage_open_world_scene` 的第一版范围，尤其是“包含所有岛屿”的边界。
5. 最后审第二其他岛屿：旧集市边缘是否进入当前 demo，还是先选一个范围更小的岛屿。

## 5. 对 runtime 的直接约束

- 没有本文件中对应对象的人工 `APPROVED` / `APPROVED_WITH_NOTES`，不得新增 Godot runtime 节点或作者化 `scene_unit.prototype.*`。
- 已存在但未完成用户确认的对象，只能作为当前灰盒 / 技术脚手架继续被门禁约束，不能扩写成新功能。
- `HubRuntime.cs` 后续替换方向应是读取作者化场景 / 单位数据并实例化，而不是继续手写 `AddSceneRect` / `AddSceneLabel` 堆出新场景。
- `old_market_edge_scene` 在被人工提升前，仍不得重新出现在航图 runtime 可玩入口中。
