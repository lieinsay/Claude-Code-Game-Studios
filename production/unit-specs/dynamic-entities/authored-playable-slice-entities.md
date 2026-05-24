# 作者化可玩切片实体单位规格

> **范围**: `src/presentation/playable_slice_authored_content.json` 中 `prototype_classification = dynamic_entity` 的真实 `scene_unit.prototype.*`。
> **权威 GDD**: `design/gdd/scene-physics-unit-system.md`
> **边界**: 实体单位必须存在于世界 / 可玩场景层；UI 只能说明状态或结果，不能替代实体本体。

## 通用规则

- 每个原型必须在 `playable_slice_authored_content.json` 的 `unit_spec` 字段回指本文件。
- `source_layer` 必须保持为 `world_playable_scene`。
- `ui_evidence_allowed` 必须为 `false` 或缺省为 false 语义。
- 新增动态实体原型时，必须补本表并通过 authored content 合同测试。

## 创建适合性人工审查

### 玩家操作实体

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `unit` |
| 稳定 ID 或拟定 ID | `scene_unit.prototype.player_marker` |
| 人工审查人 | 用户 |
| 审查日期 | 2026-05-24 |
| 结论 | `APPROVED_WITH_NOTES` |
| 必须回写的备注 | 当前版本可先是可移动图片 / 标记；后续需要扩展移动动画、各种交互动画，并可能包含一定战斗功能。本轮只做可移动、可交互触发、场景切换恢复。 |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 所有可进入场景都需要一个可移动、可定位、可触发空间交互的玩家实体。
- 不复用已有场景 / UI / 单位的原因: UI 光标、摄像机位置或 HUD 状态不能证明玩家在世界中存在。
- 主要范围风险: 本轮不得扩成完整角色动画、装备、战斗或成长系统。
- 必须写回规格的调整: 当前实现范围只包含移动、交互触发和场景切换恢复；动画、交互动画和战斗是后续扩展。

## 动态实体原型

| Prototype ID | 中文名称 | 可出现的场景 | 运行时行为 | 碰撞 / 触发 | 遮挡 | 状态与恢复 | UI 边界 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `scene_unit.prototype.player_marker` | 玩家位置标记 | `hub_island_dock`、`hub_ship_interior`、`exploration_mist_island` | 表示玩家在当前场景的可玩位置和进入点；随场景切换迁移；本轮只要求可移动和可触发空间交互 | `nav_blocker` 语义用于导航和出生点保护，不作为实体伤害体 | `actor` | 场景载入时由实例位置恢复；不得由 HUD 独立生成；移动动画、交互动画和战斗能力为后续扩展 | HUD 可显示玩家状态，但不能替代世界位置 |
| `scene_unit.prototype.exploration_threat_zone` | 探索威胁区域 | `exploration_mist_island` | 在探索搜索路径上提供威胁触发区域，可进入受威胁 / 战斗处理 | `trigger`；必须有触发半径或等价范围语义 | `effect` | 威胁结果写回探索 / 船体状态；重载进度时按搜索步骤恢复 | `S7_combat` 可处理威胁反馈，但威胁区域本体必须在场景数据中存在 |

## 验收

- `tests/integration/playable-slice/DomainAdapterProgram.cs` 必须验证每个动态实体原型有 `unit_spec`、规格文件存在，且文件包含对应 `Prototype ID`。
- 场景实例仍由 `scene_unit_instances` 和对应 `production/scene-specs/*.md` 证明摆放位置。
