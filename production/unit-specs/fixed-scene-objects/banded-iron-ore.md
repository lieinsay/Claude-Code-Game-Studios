# 条带状铁矿固定单位规格

> **Prototype ID**: `scene_unit.prototype.banded_iron_ore`
> **中文名称**: 条带状铁矿
> **单位分类**: `fixed_scene_object`
> **生命周期状态**: `spec_drafted`
> **创建适合性人工审查**: `APPROVED_WITH_NOTES`
> **来源 GDD**: `design/gdd/scene-physics-unit-system.md`, `design/gdd/resources-goods-capacity.md`
> **最后更新**: 2026-05-24

## 0. 文件头

| 字段 | 内容 |
| --- | --- |
| Unit ID | `scene_unit.prototype.banded_iron_ore` |
| 单位名称 | 条带状铁矿 |
| 单位分类 | `fixed_scene_object` |
| 存放目录 | `production/unit-specs/fixed-scene-objects/` |
| 生命周期状态 | `spec_drafted` |
| 创建适合性人工审查 | `APPROVED_WITH_NOTES` |
| 来源 GDD | `design/gdd/scene-physics-unit-system.md`; `design/gdd/resources-goods-capacity.md` |
| 关联场景 / UI | `ochre_island_scene` |
| 独立实现入口 | `src/scenes/units/BandedIronOre.tscn`; `src/scenes/units/BandedIronOre.cs` |
| 最后更新 | 2026-05-24 |

## 1. 独立实现 / 资产边界

| 字段 | 内容 |
| --- | --- |
| 独立原型实现 | `scene_unit.prototype.banded_iron_ore`；已建立独立 Godot 灰盒单位 `src/scenes/units/BandedIronOre.tscn`。 |
| 配套脚本 / 行为 | `src/scenes/units/BandedIronOre.cs` 暴露本地采集请求信号；Resources 奖励写入仍由后续 runtime 集成负责。 |
| 资产组 | 条带状矿脉、采集前 / 后状态、禁用反馈、采集音效。 |
| 摆放实例来源 | `src/scenes/ochre/OchreIslandScene.tscn::WorldLayer/BandedIronOreInstance`；`ochre_island_scene` 只引用矿脉原型，不静默改变本体规则。 |
| 禁止混入位置 | 不得把矿脉只画成地面纹理、市场商品或 UI 按钮。 |
| 删除旧节点要求 | 若替代旧 Godot 节点，删除前必须列出节点路径并询问用户；当前为 `N/A true`。 |

## 2. 单位身份

- 单位是什么: 赭石岛地表或岩壁上可识别的条带状铁矿矿脉。
- 玩家 3 秒内应如何识别: 能看出这是矿脉 / 资源点，而不是普通地面纹理。
- 它服务的场景幻想 / 功能: 玩家抵达赭石岛后能靠近并采集基础矿物资源。
- 它不是什么: 不是完整矿场系统、冶炼系统、经济链入口或市场商品摊位。
- 不能被 UI 替代的原因: 赭石岛的资源岛身份需要世界资源点支撑。

## 3. 分类与边界

| 字段 | 内容 |
| --- | --- |
| 单位分类 | `fixed_scene_object` |
| 存放目录 | `production/unit-specs/fixed-scene-objects/` |
| 是否可移动 | 否 |
| 是否可交互 | 是，靠近 + Use 采集 / 获取资源 |
| 是否有状态 | 是，`available` / `harvested` / `blocked` |
| 是否持久化 | 条件性；采集改变世界状态时必须按场景进度持久化。 |
| 领域 owner | Resources |
| 表现 owner | Scene Composition / Art |
| UI 是否可替代本体 | `否` |
| 创建审查记录 | 本文件“创建适合性记录” |

## 4. 物理合同

| 字段 | 内容 |
| --- | --- |
| 碰撞类型 | `blocking_static` 或贴地 / 贴壁资源体；交互范围为 `soft_overlap` |
| 遮挡层 | `midground_object` |
| 比例规则 | 玩家能读出“矿脉 / 资源点”，不能像普通地面纹理。 |
| 可通过性 | 本体可阻挡或贴壁；采集范围可 soft-overlap |
| 特殊表面 | `N/A true` |
| 动态行为标签 | `breakable` / `resource_node` |
| 恢复规则 | 已采集或容量不足时保持世界对象可见，只改变状态和反馈。 |

## 5. 状态与生命周期

| 状态 | 进入条件 | 世界表现 | 玩法影响 | 退出条件 |
| --- | --- | --- | --- | --- |
| `available` | 初次抵达或未采集 | 矿脉可见，可交互 | 可采集基础资源 | 采集成功 |
| `harvested` | 本轮采集完成 | 高亮消失、矿脉变暗或显示已采集 | 增加资源或记录已采集状态 | 由持久化 / 后续再生规则决定 |
| `blocked` | 容量不足或系统禁止 | 保持可见，给禁用反馈 | 不改变资源 | 条件解除 |

矿脉再生、工具效率、冶炼、市场交易和完整经济链是后续开发范围，不属于本轮。

## 6. 交互规则

| 玩家动作 | 输入 / 焦点规则 | 成功结果 | 失败 / 禁用反馈 | UI 辅助 |
| --- | --- | --- | --- | --- |
| 采集 / 获取资源 | 靠近 + Use；世界输入未被模态 UI 抢占 | 进入 `harvested`，增加基础铁矿或等价资源占位 | 已采集或容量不足时短提示 | UI 只解释容量、已采集或禁用原因 |

UI 只能解释单位状态、可用动作或失败原因；不能成为唯一交互实体。

## 7. 固定 / 实体专属规则

### 7.1 固定单位规则

- 适用性: 适用。
- 固定原因: 矿脉绑定赭石岛地形，位置由场景摆放决定。
- 位置是否可被场景实例覆盖: 可由 `ochre_island_scene` 摆放实例决定。
- 是否可破坏 / 开关 / 采集 / 再生: 本轮可采集；再生为后续范围。
- 状态变化是否改变碰撞或遮挡: 本轮不改变碰撞；采集后改变视觉状态。
- 是否需要 `behind_object_reveal`: 本轮 `N/A true`。
- 是否会生成资源、实体或交互锚点: 采集成功会生成或增加基础资源。

### 7.2 实体单位规则

- 适用性: `N/A true`，本文件只覆盖固定单位。

## 8. 场景使用规则

| 场景 | 使用方式 | 实例要求 | 后续反馈记录 |
| --- | --- | --- | --- |
| `ochre_island_scene` | 资源岛核心采集锚点 | 必须支持可见矿脉、采集范围、采集后状态和返航路径不被遮挡 | `directed-content-modification` |

## 9. 作者化数据要求

- 原型 ID: `scene_unit.prototype.banded_iron_ore`
- 允许场景: `ochre_island_scene`
- 必需实例字段: `instance_id`、`scene_id`、`position`、`floor_id`、`interaction_anchor`、`initial_state`
- 可覆盖字段: 位置、初始状态、资源数量、是否可采集
- 不允许实例静默覆盖的字段: UI 证据边界、碰撞类型、复杂经济链范围
- 运行时验证 hook: 矿脉采集、容量不足和采集后状态 smoke

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 条带状铁矿本体灰盒或资产 | 资源点身份 | 待制作 | Unit / Art |
| P0 | 采集成功、已采集和容量不足反馈 | 交互和失败反馈 | 待制作 | UI / Audio |
| P1 | 采集粒子或矿脉状态变化 | 状态可读性 | 待制作 | Art |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 数据验证 | 条带状铁矿原型和实例字段完整 | PASS；Godot AI MCP hierarchy PASS，`scene_unit.prototype.banded_iron_ore` 和实例已纳入作者化数据 / `DomainAdapterTest` |
| 运行时 smoke | 靠近 + Use 采集；采集后状态改变；正式 Resources 写入 | PASS；正式 `route.ochre` 可验证靠近 + Use、`resource.banded_iron_ore` carried pool 写入、返航 storage 结算和采集后状态。容量不足反馈仍属后续容量压力用例。 |
| 截图 / 视觉证明 | 矿脉首屏、采集后状态和返航路径 | pending |
| 后续反馈记录 | `directed-content-modification` 需求记录 | pending |

## 12. 创建适合性记录

- 审查问题: 是否应该创建条带状铁矿固定资源点，而不是复用残骸、储物箱、市场 UI 或地面纹理。
- 用户结论: `APPROVED_WITH_NOTES`
- 用户要求: 本轮先作为固定资源点，支持靠近 + 采集 / 获取资源；复杂采矿工具、矿脉再生、冶炼、完整经济链或市场交易属于后续开发。
- 删除旧 Godot 节点确认: `N/A true`
- 进入实现条件: 创建适合性已通过；独立实现 / 资产边界和 QA 证据路径已记录。

## 13. 后续反馈与定向修改

- 保持可修改状态: `true`
- 定向修改入口: `directed-content-modification`
- 后续修改目标: 复杂采矿、再生、冶炼和经济链需要另行审批。
- 用户反馈: None

## 14. 就绪检查清单

- [x] 创建适合性人工审查已记录，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`。
- [x] 独立实现 / 资产边界已明确，不能只混入旧大场景、大脚本或临时节点。
- [x] 单位分类、目录、物理合同和 UI 边界一致。
- [ ] 状态变化可读，不需要靠 UI 独自解释。
- [ ] 场景实例只引用原型，不静默改变本体规则。
- [ ] 最终状态保持可修改，后续需求走 `directed-content-modification`。
