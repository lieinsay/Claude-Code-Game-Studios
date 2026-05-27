# 场景完整性门禁与证据合同

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-002-scene-completeness-gate-evidence.md`
> **最后更新**: 2026-05-24
> **目的**: 定义任何 release-readiness 声明前，场景证据包如何被审核。
> **语言规则**: 除路径、代码符号、命令、稳定 ID、状态枚举、ADR/TR 编号等必要内容外，本目录文档必须使用中文。

## 门禁规则

只有下列所有维度都通过时，`scene_complete` 才为 true。任何 `fail`、`pending`、`tracked-gap` 或缺失证据都会阻塞完成，除非证据包中记录了用户明确批准的 waiver。

```text
scene_complete =
    creation_review_passed
    AND independent_boundary_ready
    AND purpose_ready
    AND scene_physics_ready
    AND space_ready
    AND behavior_ready
    AND state_ready
    AND presentation_ready
    AND technical_ready
    AND qa_ready
    AND codex_review_passed
```

## 必需维度

| 维度 | 必需证据 | 阻塞条件 |
| --- | --- | --- |
| `creation_review_passed` | 新建场景在进入 implementation readiness 或 release-readiness 前，已按 `production/content-creation-review-gate.md` 记录人工适合性审查，结论为 `APPROVED` 或 `APPROVED_WITH_NOTES`。 | 人工适合性审查缺失、`PENDING`、`REVISE`、`REJECTED`，或用户备注未写回规格。 |
| `independent_boundary_ready` | 场景有独立 Godot 场景、独立资产组、作者化数据和 / 或 runtime 边界；大型旧场景 / 大脚本只负责装配引用。 | 场景本体只散落在旧 Godot 节点、大脚本或临时灰盒中，无法作为独立对象追踪、替换或删除。 |
| `purpose_ready` | 场景身份、循环角色、情绪目标和 3 秒识别目标已记录。 | 目的含糊、只命名 UI 屏幕，或没有说明玩家为什么进入。 |
| `scene_physics_ready` | #20 Scene Physics Contract 通过，或场景明确记录 `N/A true` 因为没有玩法相关物理单位。 | #20 合同缺失、pending、failed 或被静默跳过。 |
| `space_ready` | 进入、离开、可行走区域、边界、地标、交互锚点均来自世界 / 可玩场景证据。 | 只有节点存在或 HUD 文本作为空间证明。 |
| `behavior_ready` | 关键路径、可选行为、取消 / 失败路径、交互锚点已记录。 | 主要动作只存在于 UI 按钮或无锚点命令中。 |
| `state_ready` | 至少三个状态变体，或明确豁免；每个变体都有来源状态和世界 / 可玩场景证据。 | 变体缺失、只有 UI 文本变化，或来源状态含糊。 |
| `presentation_ready` | P0/P1 视觉、VFX、音频需求可追溯到身份、交互、状态或反馈。 | 当前场景存在未解决 P0 资产缺口，且没有用户 waiver。 |
| `technical_ready` | Godot 场景 / 运行时表面、稳定 ID、读取 / 变更的领域管理器、持久化字段、信号、焦点边界、debug/smoke hook 已记录。 | 场景层创建新玩法权威、复制持久化状态或绕过领域负责人。 |
| `qa_ready` | 自动 smoke、适用时的截图 / 视觉证明和规格一致性检查路径已命名。 | smoke 只证明节点存在、视觉声明缺截图，或缺少规格一致性检查。 |
| `codex_review_passed` | Codex 对目的、空间、行为、状态、表现、技术、QA 线无 blocker。 | 任一 Codex blocker 未解决。 |

## 实现后反馈记录

实现后反馈不是二次审核门。若用户在体验真实实现后提出调整，只记录为 `directed-content-modification` 需求，并按定向修改流程更新对应场景 / UI / 单位文档和实现。

- `production/playtests/scene-composition-user-readability-checklist.md`
- `production/scene-specs/scene-release-gate-handoff.md`

Codex 审核用于检查规格与证据一致性。用户反馈不形成二次结论，也不阻塞一审已批准的规格；新需求需要写回场景规格或后续 story 时，走 `directed-content-modification`。

创建适合性审查回答“是否应该创建这个场景”，是唯一人工前置硬门。任何新场景没有人工适合性 `APPROVED` / `APPROVED_WITH_NOTES` 时，不得进入实现或 release readiness。

实现后自检问题：

- 我在哪里？
- 我能在这里做什么？
- 我如何离开或继续？
- 发生了什么变化？
- UI/HUD 是否只是辅助，而不是主导？
- 场景是否符合预期幻想？

## 自动 smoke 证据要求

对于灰盒或更高完成度的运行时场景，自动 smoke 必须验证所有适用行：

| Smoke 项 | 必需证明 |
| --- | --- |
| 可见场景身份节点 | 当前场景的世界 / 可玩场景节点存在且可见。 |
| 主视口覆盖 | 场景美术占据主视口，足以证明空间身份，而不是文字条。 |
| 交互锚点 | 主要动作有空间锚点，例如 ramp、helm、wreck、return ship、repair point、stall 或 NPC。 |
| 焦点隔离 | 当前状态不可用的 UI 控件离开焦点链，或被阻止。 |
| 核心路线行为 | 场景切换保留与该场景相关的预期循环行为。 |
| 物理合同证据 | 运行时 smoke 或等价证据链接到 #20 合同字段。 |

节点存在本身不足。smoke 包必须把节点可见性和视口覆盖、锚点语义、状态转换、非 UI 场景证据结合起来。

## 场景与 UI 边界

Story 003 的配套边界合同位于 `production/scene-specs/scene-vs-ui-evidence-boundary.md`。

完整性门禁必须把以下内容视为仅辅助证据：

- HUD 标签和状态面板
- 按钮、菜单、模态面板和航线控件
- 保存 / 读取 / 删除控件
- 新手引导提示文本
- 调试标签、调试覆盖层和 smoke-only 诊断文本

这些表面不能满足 `space_ready`、`scene_physics_ready`、`behavior_ready`、`presentation_ready` 或 `qa_ready`，除非同时有世界 / 可玩场景证据。即使所有 UI 控件可见、可点击且标签正确，UI-only 证据包也会失败。

视觉可读性中，任何 release-readiness 声明前都必须记录 `hud_not_dominant = true`。`primary_scene_viewport_share` 目标为 65%，当主要世界身份被隐藏、低于 55%，或退化为 UI 背后的文字条时阻塞。

## 资产门禁要求

每个当前场景的 P0 资产行必须映射到以下之一：

- `identity`
- `interaction`
- `state_variant`
- `feedback`

未解决的 P0 缺口会阻塞 release readiness，除非证据包包含：

- waiver owner
- waiver date
- explicit risk accepted
- temporary greybox or fallback evidence

灰盒可以支撑 `greybox` 或 `asset_gate` 生命周期状态。仅凭灰盒不能让 release readiness 的 `scene_complete=true`。

## 领域权威边界

场景证据可以读取领域状态，并通过世界 / 可玩场景锚点表现。它不得：

- 创建新的玩法权威
- 复制持久化状态
- 绕过所属领域去变更资源、航线、修复、市场、探索、反馈、新手引导、保存 / 读取或 UI 焦点状态
- 仅凭美术推断玩法碰撞、可通行性或物理行为

技术合同必须为每个可变玩法后果命名领域负责人。

## 当前门禁快照

| Scene ID | 门禁状态 | 阻塞原因 | 必要下一步 |
| --- | --- | --- | --- |
| `initial_island_scene` | `implemented-asset-slice` | 独立 Godot 场景、作者化单位、#20 运行时合同和 smoke / integration 证据已建立；仍缺非 headless 截图和最终美术 / 音频。 | 后续补截图包和子单位 dedicated specs；如有调整诉求走 `directed-content-modification`。 |
| `ship_interior_layered` | `implemented-asset-slice` | 独立 Godot 场景、ChartTable / S4_chart 接入、作者化单位和 smoke / integration 证据已建立；仍缺非 headless 截图和最终美术 / 音频。 | 后续补截图包和更多船内子单位 specs；如有调整诉求走 `directed-content-modification`。 |
| `voyage_open_world_scene` | `implemented-asset-slice` | 独立 Godot 航行世界场景、#20 合同、作者化单位和自动证据已建立；当前不声明完整 #10 live driving、音频或非 headless 截图完成。 | 后续补实时驾驶任务、截图包和最终美术 / 音频；下一条 reset 场景建议重建 `mist_lamp_wreck_scene`。 |
| `mist_lamp_wreck_scene` | `asset-reset-required` | 旧作者化单位数据已撤销；当前只剩运行壳 / 历史灰盒参考，不能作为现存游戏资产证据。 | 后续按 Godot asset workflow 重新实现，不做 release packet。 |
| `ochre_island_scene` | `implemented-asset-slice` | 当前唯一保留的合规游戏资产切片；仍不是 release-ready，因为正式路线接入、domain 写入和截图证据尚未完成。 | 等核心 UI / 单位独立实现后，再补正式路线接入和 release packet。 |
| `old_market_edge_scene` | `tracked-gap-future-market` | 旧集市边缘保留为后续市场内容缺口，不再作为当前 demo 第二岛屿。 | 后续市场阶段再起草旧集市场景规格和 #20 合同。 |
| `repair_node_scene` | `tracked-gap-future` | 尚无当前可进入场景规格或 #20 合同；除非明确加入，否则不属于修正后的当前 demo 场景集。 | 视觉完成声明前起草修复场景规格。 |

## Story 边界

本门禁只定义证据形状和自动验证。Story 003 负责更强的 UI/HUD 排除检查；实现后反馈统一进入 `directed-content-modification`。
