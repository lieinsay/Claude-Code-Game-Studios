# 航图台固定单位规格

> **Prototype ID**: `scene_unit.prototype.chart_table`
> **中文名称**: 航图台 / 星图桌
> **单位分类**: `fixed_scene_object`
> **生命周期状态**: `implementation_priority`
> **创建适合性人工审查**: `APPROVED`
> **来源 GDD**: `design/gdd/ui-hud-chart-interface.md`, `design/gdd/airship-hub.md`, `design/gdd/scene-physics-unit-system.md`
> **最后更新**: 2026-05-24

## 0. 文件头

| 字段 | 内容 |
| --- | --- |
| Unit ID | `scene_unit.prototype.chart_table` |
| 单位名称 | 航图台 / 星图桌 |
| 单位分类 | `fixed_scene_object` |
| 存放目录 | `production/unit-specs/fixed-scene-objects/` |
| 生命周期状态 | `implementation_priority` |
| 创建适合性人工审查 | `APPROVED` |
| 来源 GDD | `design/gdd/ui-hud-chart-interface.md`; `design/gdd/airship-hub.md`; `design/gdd/scene-physics-unit-system.md` |
| 关联场景 / UI | `ship_interior_layered`; `S4_chart` |
| 独立实现入口 | `pending`；目标为独立固定单位原型和资产组 |
| 最后更新 | 2026-05-24 |

## 1. 独立实现 / 资产边界

| 字段 | 内容 |
| --- | --- |
| 独立原型实现 | `scene_unit.prototype.chart_table`；后续进入 authored content 和独立 `.tscn` / `.tres` 或等价原型。 |
| 配套脚本 / 行为 | 打开 `S4_chart` 的交互行为；不直接拥有航行或资源状态。 |
| 资产组 | 航图桌面、星图投影、交互高亮、禁用反馈、打开 / 关闭音效。 |
| 摆放实例来源 | `ship_interior_layered` 只引用航图台原型，不静默改变本体规则。 |
| 禁止混入位置 | 不得复用 `helm_console` 作为本体，不得把航图台只写成旧 `ChartPanel` 按钮。 |
| 删除旧节点要求 | 若替代旧 Godot 节点，删除前必须列出节点路径并询问用户；当前为 `N/A true`。 |

## 2. 单位身份

- 单位是什么: 船内驾驶舱 / 教师仓区域中的航图台、星图桌或等价导航桌面。
- 玩家 3 秒内应如何识别: 能看出这是可接近的世界桌面 / 台面，而不是纯 UI 图标。
- 它服务的场景幻想 / 功能: 玩家靠近它并使用后打开航图 UI，完成航线选择和出航确认前的规划。
- 它不是什么: 不是 `helm_console`，不是旧 `ChartPanel`，不是航行大场景，也不是市场 / 修复 / 模块台。
- 不能被 UI 替代的原因: 航图 UI 必须由船内真实世界锚点触发。

## 3. 分类与边界

| 字段 | 内容 |
| --- | --- |
| 单位分类 | `fixed_scene_object` |
| 存放目录 | `production/unit-specs/fixed-scene-objects/` |
| 是否可移动 | 否 |
| 是否可交互 | 是，靠近 + Use 打开 `S4_chart` |
| 是否有状态 | 是，`idle` / `focused` / `chart_open` / `disabled` |
| 是否持久化 | 否；航线选择由 Chart / Navigation 持久化 |
| 领域 owner | Chart / Hub |
| 表现 owner | Scene Composition / UI |
| UI 是否可替代本体 | `否` |
| 创建审查记录 | 本文件“创建适合性记录” |

## 4. 物理合同

| 字段 | 内容 |
| --- | --- |
| 碰撞类型 | `blocking_static` + `soft_overlap` 交互范围 |
| 遮挡层 | `midground_object` |
| 比例规则 | 明显小于船舱房间，大于普通箱子，玩家能读出“桌面 / 台面”。 |
| 可通过性 | 本体阻挡，交互范围可 soft-overlap |
| 特殊表面 | `N/A true` |
| 动态行为标签 | `N/A true` |
| 恢复规则 | 航图不可用时保持本体可见，只给禁用反馈。 |

## 5. 状态与生命周期

| 状态 | 进入条件 | 世界表现 | 玩法影响 | 退出条件 |
| --- | --- | --- | --- | --- |
| `idle` | 船内普通状态 | 航图台可见，可接近 | 可提示按 Use | 玩家靠近 |
| `focused` | 玩家在交互范围内 | 高亮 / 提示可用 | Use 打开航图 UI | 玩家离开或打开 UI |
| `chart_open` | `S4_chart` 打开 | 世界输入隔离或暂停 | 航图 UI 接管焦点 | Esc 或确认 |
| `disabled` | 航线系统不可用或更高优先级 UI 占用 | 不消失，只显示禁用反馈 | 不打开航图 | 状态恢复 |

## 6. 交互规则

| 玩家动作 | 输入 / 焦点规则 | 成功结果 | 失败 / 禁用反馈 | UI 辅助 |
| --- | --- | --- | --- | --- |
| 打开航图 | 靠近 + Use；世界输入未被模态 UI 抢占 | 打开 `S4_chart` | 航图不可用时短提示 | UI 只解释航线选择和失败原因 |

UI 只能解释单位状态、可用动作或失败原因；不能成为唯一交互实体。

## 7. 固定 / 实体专属规则

### 7.1 固定单位规则

- 适用性: 适用。
- 固定原因: 航图台是船内空间锚点，位置由船内场景摆放决定。
- 位置是否可被场景实例覆盖: 可由 `ship_interior_layered` 摆放实例决定。
- 是否可破坏 / 开关 / 采集 / 再生: 本轮 `N/A true`。
- 状态变化是否改变碰撞或遮挡: 否。
- 是否需要 `behind_object_reveal`: 本轮 `N/A true`。
- 是否会生成资源、实体或交互锚点: 只打开 `S4_chart`，不生成资源或实体。

### 7.2 实体单位规则

- 适用性: `N/A true`，本文件只覆盖固定单位。

## 8. 场景使用规则

| 场景 | 使用方式 | 实例要求 | 后续反馈记录 |
| --- | --- | --- | --- |
| `ship_interior_layered` | 船内航线规划世界锚点 | 不得与 `helm_console` 职责混淆；必须能打开 `S4_chart` | `directed-content-modification` |

## 9. 作者化数据要求

- 原型 ID: `scene_unit.prototype.chart_table`
- 允许场景: `ship_interior_layered`
- 必需实例字段: `instance_id`、`scene_id`、`position`、`floor_id`、`interaction_anchor`
- 可覆盖字段: 位置、朝向、是否可用、提示文本引用
- 不允许实例静默覆盖的字段: 碰撞类型、UI 证据边界、与 `S4_chart` 的绑定关系
- 运行时验证 hook: 航图台打开航图 UI 的 smoke / focus 测试

## 10. 资产与音频需求

| 优先级 | 需求 | 支持身份 / 交互 / 状态 / 反馈 | 当前来源 | 缺口负责人 |
| --- | --- | --- | --- | --- |
| P0 | 航图台 / 星图桌本体灰盒或资产 | 世界锚点身份 | 待制作 | Unit / Art |
| P0 | 可用 / 禁用 / 打开反馈 | 交互和失败反馈 | 待制作 | UI / Audio |
| P1 | 星图投影或微动画 | 航线规划幻想 | 待制作 | Art |

## 11. QA 证据

| 证据类型 | 必需制品 | 状态 |
| --- | --- | --- |
| 数据验证 | 航图台原型和实例字段完整 | pending |
| 运行时 smoke | 靠近 + Use 打开 `S4_chart`，Esc 返回船内 | pending |
| 截图 / 视觉证明 | 航图台在船内可读 | pending |
| 后续反馈记录 | `directed-content-modification` 需求记录 | pending |

## 12. 创建适合性记录

- 审查问题: 是否应该创建独立航图台，而不是复用 `helm_console` 或旧 `ChartPanel`。
- 用户结论: `APPROVED`
- 用户要求: 新增独立航图台 / 星图桌固定单位；不要复用 `helm_console` 作为航图台本体。
- 删除旧 Godot 节点确认: `N/A true`
- 进入实现条件: 创建适合性已通过；独立实现 / 资产边界和 QA 证据路径已记录。

## 13. 后续反馈与定向修改

- 保持可修改状态: `true`
- 定向修改入口: `directed-content-modification`
- 后续修改目标: 驾驶模拟、维修或模块台功能需要另行审批。
- 用户反馈: None

## 14. 就绪检查清单

- [ ] 创建适合性人工审查已记录，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`。
- [ ] 独立实现 / 资产边界已明确，不能只混入旧大场景、大脚本或临时节点。
- [ ] 单位分类、目录、物理合同和 UI 边界一致。
- [ ] 状态变化可读，不需要靠 UI 独自解释。
- [ ] 场景实例只引用原型，不静默改变本体规则。
- [ ] 最终状态保持可修改，后续需求走 `directed-content-modification`。
