# 单位规格模板

> **Unit ID**:
> **单位名称**:
> **单位分类**: `fixed_scene_object` / `dynamic_entity`
> **存放目录**: `production/unit-specs/fixed-scene-objects/` 或 `production/unit-specs/dynamic-entities/`
> **生命周期状态**: concept_needed / spec_drafted / user_review / implementation_ready / greybox / accepted / blocked
> **创建适合性人工审查**: PENDING / APPROVED / APPROVED_WITH_NOTES / REVISE / REJECTED
> **来源 GDD**: `design/gdd/scene-physics-unit-system.md`
> **最后更新**:

## 1. 单位身份

- 单位是什么:
- 玩家 3 秒内应如何识别:
- 它服务的场景幻想 / 功能:
- 它不是什么:

## 2. 分类与边界

| 字段 | 内容 |
| --- | --- |
| 单位分类 | `fixed_scene_object` / `dynamic_entity` |
| 存放目录 | 固定单位必须放 `fixed-scene-objects/`；实体单位必须放 `dynamic-entities/` |
| 是否可移动 | 是 / 否 / 条件性 |
| 是否可交互 | 是 / 否 / 条件性 |
| 是否有状态 | 是 / 否 |
| 是否持久化 | 是 / 否 / 由场景决定 |
| 领域 owner |  |
| 表现 owner |  |
| UI 是否可替代本体 | 必须为否 |
| 创建审查记录 | 链接 `production/content-creation-review-gate.md` 格式记录 |

## 3. 物理合同

| 字段 | 内容 |
| --- | --- |
| 碰撞类型 | `blocking_static` / `blocking_dynamic` / `pushable` / `soft_overlap` / `height_marker` / 其他 |
| 遮挡层 | `background` / `midground_floor` / `midground_object` / `foreground_occluder` / `height_shadow` |
| 比例规则 | 相对 `player_unit` |
| 可通过性 | 可通过 / 不可通过 / 条件通过 |
| 特殊表面 | 无 / `water_*` / `glass_*` / `fog_or_cloud` / 其他 |
| 动态行为标签 | 无 / `elastic` / `slippery` / `breakable` / `hazardous` / 其他 |
| 恢复规则 | 卡住、掉出边界、状态异常时如何恢复 |

## 4. 状态与生命周期

| 状态 | 进入条件 | 世界表现 | 玩法影响 | 退出条件 |
| --- | --- | --- | --- | --- |
| 初始 |  |  |  |  |
| 变化中 |  |  |  |  |
| 完成 / 冷却 |  |  |  |  |
| 异常 / 恢复 |  |  |  |  |

## 5. 交互规则

| 玩家动作 | 输入 / 焦点规则 | 成功结果 | 失败 / 禁用反馈 | UI 辅助 |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

UI 只能解释单位状态、可用动作或失败原因；不能成为唯一交互实体。

## 6. 固定 / 实体专属规则

只填写与当前单位分类匹配的部分；另一部分写 `N/A true`。

### 固定单位规则

- 固定原因:
- 位置是否可被场景实例覆盖:
- 是否可破坏 / 开关 / 采集 / 再生:
- 状态变化是否改变碰撞或遮挡:
- 是否需要 `behind_object_reveal`:
- 是否会生成资源、实体或交互锚点:

### 实体单位规则

- 运动来源: 玩家推动 / AI / 物理冲量 / 路径 / 脚本 / 其他
- 运行时状态: 位置、速度、朝向、AI、携带物、生命值或其他
- 碰撞响应: 推动、弹开、阻挡、伤害、触发或其他
- 安全复位: 掉出边界、卡住、离开有效 floor/layer 时如何恢复
- 是否持久化位置 / 速度 / 状态:

## 7. 场景使用规则

| 场景 | 使用方式 | 实例要求 | 是否需要用户审核 |
| --- | --- | --- | --- |
|  |  |  |  |

## 8. 作者化数据要求

- 原型 ID:
- 允许场景:
- 必需实例字段:
- 可覆盖字段:
- 不允许实例静默覆盖的字段:
- 运行时验证 hook:

## 9. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 |  |  |  |  |
| P1 |  |  |  |  |

## 10. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 数据验证 |  | pending |
| 运行时 smoke |  | pending |
| 截图 / 视觉证明 |  | pending |
| 用户审核 |  | pending |

## 11. 用户审核清单

- [ ] 创建适合性人工审查已记录，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`。
- [ ] 审查人确认这个单位需要独立原型，而不是已有单位的状态、皮肤或摆放实例。
- [ ] 人工备注已写回本规格、story 或后续任务。
- [ ] 玩家能否看出这个单位是什么。
- [ ] 这个单位的分类是否符合直觉：实体 / 固定单位 / 特殊表面 / 交互锚点。
- [ ] 碰撞、遮挡、比例和可通过性是否符合玩家预期。
- [ ] 状态变化是否可读，不需要靠 UI 独自解释。
- [ ] 失败、禁用或恢复反馈是否合理。
- [ ] 放到具体场景时，是否还需要额外摆放限制或状态覆盖。

用户审核结论: `PENDING`

创建适合性结论: `PENDING` / `APPROVED` / `APPROVED_WITH_NOTES` / `REVISE` / `REJECTED`

用户备注:

- 待用户填写。
