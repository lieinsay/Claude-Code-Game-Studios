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
    AND user_review_passed
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
| `qa_ready` | 自动 smoke、适用时的截图 / 视觉证明、Codex 审核和用户可读性审核路径已命名。 | smoke 只证明节点存在、视觉声明缺截图，或缺人工审核。 |
| `codex_review_passed` | Codex 对目的、空间、行为、状态、表现、技术、QA 线无 blocker。 | 任一 Codex blocker 未解决。 |
| `user_review_passed` | 用户可读性审核无 blocker，或有明确用户 waiver。 | 用户审核缺失、`BLOCKED`，或指出场景身份 / 玩家流程读不出来。 |

## 用户可读性 release handoff

Story 004 增加人工审核清单和 release handoff 包：

- `production/playtests/scene-composition-user-readability-checklist.md`
- `production/scene-specs/scene-release-gate-handoff.md`

Codex 审核是必要条件，但不足以单独通过。用户 verdict 为 `BLOCKED` 时，`user_review_passed = false`，直到 blocker 解决或用户明确 waiver。用户可以因为幻想缺失、需求缺失、身份不清、玩家流程不理想、UI 过强，或新发现需求需要写回场景规格而阻塞。

创建适合性审查早于用户可读性 release handoff。它回答“是否应该创建这个场景”；release handoff 回答“这个场景现在是否足够可读、可交付”。任何新场景没有人工适合性 `APPROVED` / `APPROVED_WITH_NOTES` 时，不得进入实现或 release readiness。规格二次人工审核不是进入实现的硬门；体验验收通过后仍可通过 `directed-content-modification` 定向修改。

必答用户可读性问题：

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
| `initial_island_scene` | `user-review-pending` | 独立规格和作者化单位数据已存在，但用户可读性 verdict、截图刷新和 P0 资产状态仍未完成。 | 用户审核 `production/scene-specs/initial-island-scene.md` 的清单，之后补 release packet。 |
| `ship_interior_layered` | `user-review-pending` | 独立规格和作者化单位数据已存在，但用户可读性 verdict、截图刷新和 P0 资产状态仍未完成。 | 用户审核 `production/scene-specs/ship-interior-layered-scene.md` 的清单，之后补 release packet。 |
| `voyage_open_world_scene` | `spec-drafted-blocked-for-evidence` | 独立规格已存在，但 #20 合同、运行时证据、Codex 审核和用户可读性 verdict 仍缺失。 | 用户先审航行方向，再起草 #20 合同和证据计划。 |
| `mist_lamp_wreck_scene` | `user-review-pending` | 独立规格和作者化单位数据已存在，并已明确为雾灯残骸浮岛目的地；但用户可读性 verdict、截图刷新和 P0 资产状态仍未完成。 | 用户审核 `production/scene-specs/mist-lamp-wreck-scene.md` 的清单，之后补 release packet。 |
| `ochre_island_scene` | `spec-drafted-blocked-for-evidence` | 用户已批准赭石岛作为当前 demo 第二小型资源岛，规格草案已起草；仍需 #20 合同、独立实现 / 资产边界、作者化数据和运行时证据。 | 补 #20 合同、独立 Godot / 资产边界、作者化数据和 release packet。 |
| `old_market_edge_scene` | `tracked-gap-future-market` | 旧集市边缘保留为后续市场内容缺口，不再作为当前 demo 第二岛屿。 | 后续市场阶段再起草旧集市场景规格和 #20 合同。 |
| `repair_node_scene` | `tracked-gap-future` | 尚无当前可进入场景规格或 #20 合同；除非明确加入，否则不属于修正后的当前 demo 场景集。 | 视觉完成声明前起草修复场景规格。 |

## Story 边界

本门禁只定义证据形状和自动验证。Story 003 负责更强的 UI/HUD 排除检查，Story 004 负责用户可读性 / release handoff 工作流。
