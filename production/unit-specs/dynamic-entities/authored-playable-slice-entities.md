# 作者化可玩切片实体单位规格

> **范围**: `src/presentation/playable_slice_authored_content.json` 中 `prototype_classification = dynamic_entity` 的真实 `scene_unit.prototype.*`。
> **权威 GDD**: `design/gdd/scene-physics-unit-system.md`
> **边界**: 实体单位必须存在于世界 / 可玩场景层；UI 只能说明状态或结果，不能替代实体本体。

## 通用规则

- 每个原型必须在 `playable_slice_authored_content.json` 的 `unit_spec` 字段回指本文件。
- `source_layer` 必须保持为 `world_playable_scene`。
- `ui_evidence_allowed` 必须为 `false` 或缺省为 false 语义。
- 新增动态实体原型时，必须补本表并通过 authored content 合同测试。

## 动态实体原型

| Prototype ID | 中文名称 | 可出现的场景 | 运行时行为 | 碰撞 / 触发 | 遮挡 | 状态与恢复 | UI 边界 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `scene_unit.prototype.player_marker` | 玩家位置标记 | `hub_island_dock`、`hub_ship_interior`、`exploration_mist_island` | 表示玩家在当前场景的可玩位置和进入点；随场景切换迁移 | `nav_blocker` 语义用于导航和出生点保护，不作为实体伤害体 | `actor` | 场景载入时由实例位置恢复；不得由 HUD 独立生成 | HUD 可显示玩家状态，但不能替代世界位置 |
| `scene_unit.prototype.exploration_threat_zone` | 探索威胁区域 | `exploration_mist_island` | 在探索搜索路径上提供威胁触发区域，可进入受威胁 / 战斗处理 | `trigger`；必须有触发半径或等价范围语义 | `effect` | 威胁结果写回探索 / 船体状态；重载进度时按搜索步骤恢复 | `S7_combat` 可处理威胁反馈，但威胁区域本体必须在场景数据中存在 |

## 验收

- `tests/integration/playable-slice/DomainAdapterProgram.cs` 必须验证每个动态实体原型有 `unit_spec`、规格文件存在，且文件包含对应 `Prototype ID`。
- 场景实例仍由 `scene_unit_instances` 和对应 `production/scene-specs/*.md` 证明摆放位置。
