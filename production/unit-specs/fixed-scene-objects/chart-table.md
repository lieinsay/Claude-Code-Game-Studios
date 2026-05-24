# 航图台固定单位规格

> **Prototype ID**: `scene_unit.prototype.chart_table`  
> **中文名称**: 航图台 / 星图桌  
> **单位分类**: `fixed_scene_object`  
> **生命周期状态**: `spec_drafted`  
> **创建适合性人工审查**: `APPROVED`  
> **来源 GDD**: `design/gdd/ui-hud-chart-interface.md`, `design/gdd/airship-hub.md`, `design/gdd/scene-physics-unit-system.md`  
> **最后更新**: 2026-05-24

## 创建适合性人工审查

| 字段 | 内容 |
| --- | --- |
| 创建对象类型 | `unit` |
| 稳定 ID 或拟定 ID | `scene_unit.prototype.chart_table` |
| 人工审查人 | 用户 |
| 审查日期 | 2026-05-24 |
| 结论 | `APPROVED` |
| 必须回写的备注 | 新增独立航图台 / 星图桌固定单位；不要复用 `helm_console` 作为航图台本体。 |

审查问题摘要:

- 适合当前项目 / 阶段的原因: 航图 UI 必须由船内真实世界锚点触发，而不是旧 `ChartPanel` 或抽象按钮。
- 不复用已有场景 / UI / 单位的原因: `helm_console` 可保留为驾驶 / 起飞控制语义；航线规划需要独立可读的航图台本体。
- 主要范围风险: 本轮不得把航图台扩成完整驾驶模拟、维修台、模块台或市场入口。
- 必须写回规格的调整: `chart_table` 是独立固定单位，后续才可加入 authored content 和 runtime。

## 1. 单位身份

- 它是什么: 船内驾驶舱 / 教师仓区域中的航图台、星图桌或等价导航桌面。
- 玩家为什么需要它: 玩家靠近它并使用后打开航图 UI，完成航线选择和出航确认前的规划。
- 它服务哪个场景: `ship_interior_layered`。
- 它不是什么: 不是 `helm_console`，不是旧 `ChartPanel`，不是航行大场景，也不是市场 / 修复 / 模块台。

## 2. 物理与交互

| 字段 | 内容 |
| --- | --- |
| 单位类型 | 固定交互控制台 |
| 碰撞 | `blocking_static` + `soft_overlap_interaction_anchor` |
| 遮挡 | 前景 / 中景交互物件；不得遮住玩家出生点或出口 |
| 比例 | 应明显小于船舱房间，大于普通箱子，玩家能读出“桌面 / 台面” |
| 可通过性 | 本体阻挡，交互范围可 soft-overlap |
| 交互 | 靠近 + Use 打开 `S4_chart` / `chart-full-screen-surface` |
| 禁用反馈 | 航图不可用时给出短提示，不能把玩家推到旧面板 |

## 3. 状态与生命周期

| 状态 | 触发 | 世界表现 | UI 关系 |
| --- | --- | --- | --- |
| idle | 船内普通状态 | 航图台可见，可接近 | 可提示按 Use |
| focused | 玩家在交互范围内 | 高亮 / 提示可用 | Use 打开航图 UI |
| chart_open | `S4_chart` 打开 | 世界输入隔离或暂停 | 航图 UI 接管焦点 |
| disabled | 航线系统不可用或更高优先级 UI 占用 | 不消失，只显示禁用反馈 | 不打开航图 |

## 4. 数据与运行时合同

- 领域负责人: Chart / Hub。
- 可调用 UI: `S4_chart`，后续独立规格 `production/ui-specs/chart-full-screen-surface.md`。
- 作者化数据: 二审通过后才允许新增到 `src/presentation/playable_slice_authored_content.json`。
- 不允许写入的状态: 航图台不直接写资源、战斗、市场或修复状态。

## 5. QA / 用户审核

- [ ] 二审确认航图台和 `helm_console` 的职责分离。
- [ ] 二审确认航图台作为世界对象触发 UI，而不是 UI 替代世界对象。
- [ ] 二审确认本轮不实现驾驶模拟、维修或模块台功能。

用户二审结论: `PENDING`
