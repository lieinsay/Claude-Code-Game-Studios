# 场景与 UI 证据边界

> **Epic**: #19 Complete Scene Composition and Acceptance
> **Story**: `production/epics/scene-composition-system/story-003-scene-vs-ui-evidence-boundary.md`
> **最后更新**: 2026-05-24
> **目的**: 定义哪些证据可以证明场景，哪些 UI/HUD 证据必须被忽略。
> **语言规则**: 除路径、代码符号、命令、稳定 ID、状态枚举、ADR/TR 编号等必要内容外，本目录文档必须使用中文。

## 边界规则

场景证据必须来自 `world_playable_scene` 层，或来自明确拒绝 UI 证据的 #20 Scene Physics Contract。UI/HUD 可以辅助理解，但不能满足场景完整性。

只有下列所有行通过时，`ui_boundary_passed` 才为 true：

```text
ui_boundary_passed =
    hud_not_dominant
    AND no_ui_as_scene_unit
    AND no_ui_as_identity_node
    AND no_ui_as_interaction_anchor
    AND no_ui_as_physics_contract_proof
    AND ui_only_evidence_fails
    AND modal_focus_isolated
    AND world_evidence_remains_mounted
```

## 证据分类

| 证据来源 | 允许用途 | 禁止用途 |
| --- | --- | --- |
| 世界 / 可玩地形、可行走边界、地标、道具、门、坡道、残骸、摊位、NPC、返航信标 | 场景身份、物理单位、交互锚点、状态变体、视口可读性、#20 合同证据 | 当它们已作为世界 / 可玩证据作者化且链接领域负责人时，不额外禁止 |
| Scene Physics Contract 字段 | 当 `physical_unit_source_layer = world_playable_scene` 且 `ui_evidence_allowed = false` 时，可作为物理场景证明 | 合同缺失、pending、failed 或仅从美术推断时，不能证明场景完成 |
| HUD 标签、状态面板、航线文字、保存 / 读取文字、新手提示、按钮、菜单、模态面板、调试标签、smoke-only 覆盖文本 | 辅助文字、可访问性支持、焦点路由证明、调试诊断 | 物理场景单位、场景身份节点、交互锚点、视口身份、物理合同证明或人工可读性替代 |

## UI 主导门禁

视觉 QA 必须在场景通过 release readiness 前记录 `hud_not_dominant = true`。

| 检查 | 通过阈值 | 阻塞条件 |
| --- | --- | --- |
| `primary_scene_viewport_share` | 目标 65%；MVP 灰盒场景可接受范围为 55-85% | 主要世界身份低于 55%、被隐藏，或只剩 UI 后方的一条窄带 |
| `world_identity_visible_with_hud` | HUD 存在时至少一个世界 / 可玩身份节点可见 | 只有标签、面板、按钮或调试覆盖层能说明玩家在哪里 |
| `core_anchor_visible_with_hud` | 当前场景至少一个相关空间锚点可见 | 唯一可用动作是 UI 按钮，没有世界 / 可玩锚点 |

临时模态可以在激活时覆盖场景。它们不会抹掉既有世界证据，但打开状态不能被用作场景完成证据。

## 自动拒绝案例

以下合成案例必须使场景就绪失败：

| Case ID | 证据包 | 预期结果 |
| --- | --- | --- |
| `ui_only_surface` | HUD 标题、保存 / 读取按钮、航线按钮、调试标签；没有世界 / 可玩场景节点 | `scene_readiness = fail` |
| `debug_overlay_only` | 调试当前场景标签和 smoke hook 文本；没有可见场景身份节点 | `scene_readiness = fail` |
| `button_only_interaction` | 可点击的航线 / 搜索 / 返回按钮；没有 helm、table、wreck、return ship、repair、stall 或 NPC 锚点 | `scene_readiness = fail` |
| `ui_physics_contract` | 任意物理合同或单位目录行出现 `physical_unit_source_layer != world_playable_scene` 或 `ui_evidence_allowed = true` | `scene_readiness = fail` |

## 自动 smoke 证据要求

用于场景身份的 smoke 或 integration 证据必须证明：

- 可见的世界 / 可玩身份节点
- 通过世界 / 可玩场景节点证明主视口覆盖
- 空间交互锚点，而不是只有按钮
- 当前 #20 物理合同字段包含 `physical_unit_source_layer = world_playable_scene`
- 每个物理单位、动态行为、恢复行都有 `ui_evidence_allowed = false`
- 模态或半模态 UI 激活时焦点隔离正确
- UI 激活时底层世界证据仍然挂载

当前运行时 smoke 已包含 Hub 外部、船内、航图桌面表面和探索场景的世界证据。同一 smoke 中的 UI 证据仍然只是辅助。

## 焦点隔离边界

ADR-0012 仍是输入权威。模态或半模态 UI 激活时：

- UIManager 拥有 UI 焦点、模态栈和输入路由。
- 世界移动 / 使用输入根据当前输入层被阻止或隔离。
- 禁用或不可用 UI 控件必须离开焦点链，或拒绝激活。
- 当 UI 模式是面板覆盖而不是完整场景切换时，底层世界 / 可玩场景证据必须保持挂载和可见。
- 焦点隔离不能被用来删除、隐藏或替代 #19 要求的场景证据。

## 当前场景分类

| 场景 / 表面 | 分类 | 边界结果 |
| --- | --- | --- |
| `initial_island_scene` | 可进入世界 / 可玩场景，HUD 只辅助；历史证据可能仍使用 `hub_island_dock` | UI 不能计入；岛屿、码头、飞艇外部、登船路径和 #20 合同仍必需 |
| `ship_interior_layered` | 可进入的水平分层船内场景，房间 / 状态 UI 只辅助；历史证据可能仍使用 `hub_ship_interior` | UI 不能计入；船内单位、层级、遮挡、behind-object reveal 和 #20 合同仍必需 |
| `voyage_open_world_scene` | 当前 demo 必需航行场景，不是航线按钮 UI 或进度条 | UI / 进度条不能计入；伪 3D 世界运动、航线边界、风险物、目的地剪影和 #20 合同必需 |
| `mist_lamp_wreck_scene` | 可进入世界 / 可玩目的地场景；历史证据可能仍使用 `exploration_mist_island` | UI 不能计入；残骸主体、雾 / 灯身份、搜索锚点、返航信标和 #20 合同仍必需 |
| `old_market_edge_scene` | 当前 demo 必需目的地场景，从未来市场缺口提升而来 | 市场 UI 不能计入；市场边缘几何、摊位 / 建筑 / NPC 锚点、可通行性和 #20 合同必需 |
| `repair_node_scene` | 未来可进入修复地点 | 修复 UI 面板不能计入；修复点 / 工作站 / NPC / 世界锚点和 #20 合同在视觉完成前必需 |

## 审核清单

- [ ] UI/HUD 没有主导或隐藏当前世界身份。
- [ ] UI/HUD/按钮/菜单/标签/调试覆盖层被排除在场景单位计数之外。
- [ ] UI/HUD/按钮/菜单/标签/调试覆盖层被排除在身份节点和交互锚点之外。
- [ ] UI/HUD/按钮/菜单/标签/调试覆盖层被排除在 #20 物理合同证明之外。
- [ ] UI-only 证据包会失败。
- [ ] 模态或半模态焦点能隔离世界输入，但不会删除底层场景证据。
- [ ] 任何例外都是明确用户 waiver，而不是自动通过。
