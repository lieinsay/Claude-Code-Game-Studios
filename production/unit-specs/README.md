# 场景单位规格目录

> **用途**: 存放可复用场景单位的本体设计。
> **适用对象**: 树、球、门、坡道、残骸、摊位、NPC、资源点、特殊表面、危险区、可推动物、可破坏物等真实存在于世界 / 可玩场景层的对象。
> **语言规则**: 除路径、代码符号、稳定 ID、状态枚举、命令和引擎/API 名称外，本目录文档使用中文。

## 目录结构

实体单位和固定单位必须分开存放。共同模板保留在本目录，具体单位规格进入对应子目录。

| 子目录 | 放什么 | 示例 |
| --- | --- | --- |
| `fixed-scene-objects/` | 默认不自行移动、位置由场景摆放决定，但可能有交互、状态、破坏、再生、开关、遮挡变化的单位 | 树、门、坡道、残骸、摊位、灯塔、资源矿点 |
| `dynamic-entities/` | 会移动、被推动、弹开、飞行、巡逻、携带状态，或需要运行时位置 / 速度 / AI / 物理响应的单位 | 玩家、NPC、敌人、物理球、可推动箱、移动平台 |

不要把两类规格混放在根目录。根目录只放 README、模板、目录级索引或未来自动生成的总表。

## 当前真实规格文件

| 文件 | 覆盖范围 | 状态 |
| --- | --- | --- |
| `dynamic-entities/authored-playable-slice-entities.md` | `playable_slice_authored_content.json` 中已存在的动态实体原型 | 已补真实规格 |
| `fixed-scene-objects/authored-playable-slice-units.md` | `playable_slice_authored_content.json` 中已存在的固定单位原型 | 已补真实规格 |
| `dynamic-entities/physics-ball-example.md` | 示例动态实体 | 示例 |
| `fixed-scene-objects/tree-regenerating-example.md` | 示例固定单位 | 示例 |

## 这个目录解决什么

`production/scene-specs/` 回答“某个场景里放了什么、放在哪里、为什么这样放”。
`production/unit-specs/` 回答“某个单位本体是什么、如何碰撞、如何变化、如何交互、如何复用”。

例如：

- 再生树的砍伐、树桩、再生、资源产出属于单位本体设计，放在本目录。
- 初始岛屿里哪几棵树放在码头左侧、是否挡路、是否参与当前 demo，放在对应场景规格。
- 运行时 `scene_unit.prototype.*` 和 `scene_unit.instance.*` 数据放在 `src/presentation/playable_slice_authored_content.json` 或后续等价作者化数据源。

## 和其他文档的关系

| 层级 | 放什么 | 例子 |
| --- | --- | --- |
| `design/gdd/scene-physics-unit-system.md` | 所有场景单位必须遵守的总规则 | 实体单位 / 固定单位分类、碰撞、遮挡、生命周期、UI 边界 |
| `production/unit-specs/fixed-scene-objects/` | 可复用固定单位本体规格 | 可砍伐再生树、登船坡道、残骸桅杆 |
| `production/unit-specs/dynamic-entities/` | 可复用实体单位本体规格 | 可弹开物理球、可推动箱、NPC、敌人 |
| `production/scene-specs/` | 单位在具体场景中的摆放和场景语义 | 初始岛屿使用哪些单位、摆在哪里、服务什么读法 |
| `src/presentation/playable_slice_authored_content.json` | 机器可读作者化数据 | `scene_unit.prototype.*`、`scene_unit.instance.*` |
| `production/qa/evidence/` | 验证证据 | 原型 / 实例链接、运行时合同、smoke 结果 |

## 什么时候必须写单位规格

下列任一条件成立，就需要在本目录写独立单位规格，不能只在场景规格里一句带过：

- 单位会被多个场景复用。
- 单位有交互、状态、生命周期、破坏、再生、移动、弹性、危险、资源产出或持久化。
- 单位会影响碰撞、遮挡、通行、视线、尺度、玩家读法或 QA 验收。
- 单位属于关键路径、核心幻想或用户需要审核的世界对象。
- 单位看起来像背景，但实际会阻挡、可砍伐、可拾取、可推动、可开关、可修复或可变化。

## 创建适合性人工审查

任何新固定单位、动态实体、NPC、障碍物、门、资源点、带碰撞 / 遮挡 / 状态的 prop，或 `scene_unit.prototype.*`，在进入实现或 `implementation_ready` 前，必须先按 `production/content-creation-review-gate.md` 记录人工适合性审查。结论只有 `APPROVED` 或 `APPROVED_WITH_NOTES` 时才允许继续；`PENDING`、`REVISE`、`REJECTED` 都会阻塞 story-readiness 和 `/dev-story`。

人工审查重点是判断是否应该创建独立单位：能否复用已有单位、是否符合当前场景和核心幻想、是否带来新的物理 / 状态 / 资产复杂度，以及固定单位或动态实体分类是否清楚。

## 最低规格要求

每个单位规格至少回答：

- 它是 `实体单位` 还是 `固定单位`，是否还带有 `动态行为`。
- 它的碰撞、遮挡、比例、可通过性和 UI 边界是什么。
- 它有哪些状态，状态如何变化，是否持久化。
- 玩家如何识别、交互、失败、取消或恢复。
- 它由哪个领域系统拥有规则，场景层只负责什么表现。
- 它能出现在哪些场景，哪些场景只是摆放实例。
- 自动验证、截图、用户审核分别证明什么。

## 命名建议

| 类型 | 文件名建议 | 稳定 ID 建议 |
| --- | --- | --- |
| 固定单位 | `fixed-scene-objects/tree-regenerating.md` | `scene_unit.prototype.tree_regenerating` |
| 实体单位 | `dynamic-entities/physics-ball.md` | `scene_unit.prototype.physics_ball` |
| 门 / 通道 | `fixed-scene-objects/dock-boarding-ramp.md` | `scene_unit.prototype.dock_boarding_ramp` |
| 残骸部件 | `fixed-scene-objects/wreck-mast.md` | `scene_unit.prototype.wreck_mast` |
| 特殊表面 | `fixed-scene-objects/deep-water-boundary.md` | `scene_unit.prototype.deep_water_boundary` |

文件名使用英文小写短横线，正文使用中文。稳定 ID 使用代码友好的英文。
