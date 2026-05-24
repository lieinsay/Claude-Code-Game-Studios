# UI / HUD / 航图界面

> **Status**: Designed (CD APPROVED, suggestions applied)
> **Author**: lieinsay + ux-designer + game-designer + ui-programmer
> **Last Updated**: 2026-05-09
> **Implements Pillar**: P1 (规划先于冒险), P2 (世界会回应照料), P3 (飞艇是家), P4 (未知带来温和压力), P5 (少量深关系胜过大量收集)
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-05-03 — R1 (color deviation rationale in C.9), R2 (post-repair world-change hint in OQ #6), R3 (first-repair guidance in Edge Cases) all applied

> **Platform Pivot Note**: ADR-0019 supersedes Web export constraints for active UI implementation. MVP UI now targets desktop Godot 4.6.2 .NET/C# with keyboard/mouse input, window focus recovery, and desktop renderer validation. Existing WebGL / browser recovery notes are historical unless restated as desktop behavior below.

## Overview

系统 #16（UI / HUD / 航图界面）是 MVP 的呈现层外壳，负责将 15 个领域系统的数据与状态转换为统一的、可被玩家读懂的画面体验。它不发明新机制——它拥有屏幕清单、模态栈、HUD 层、输入路由、焦点管理和每一个承载玩家走完第一轮闭环（Hub → 航图 → 探索 → 返回 → 修复）的视觉过渡。#16 缺失时，Hub 的交互站点、航图的路线编码、探索 HUD 的威胁预览、战斗决策面板、修复材料清单、集市摊位界面全都是彼此沉默的孤立数据。#16 的作用是用统一的视觉语言——航路修复主义：UI 像可被阅读的航海图，不是浮在地图上的菜单——和统一的交互规则（同时最多一个模态、HUD 按状态变化信号刷新、键鼠输入对等、关键决策 1 秒可读）把它们收束起来。领域系统拥有数据和状态机；#16 拥有让它们可见、可导航、可操作的屏幕。

UI 本体规格放在 `production/ui-specs/`。任何常驻 HUD、锚点面板、模态、半模态覆盖层、全屏 UI 表面、toast / hint 或 debug overlay，都必须声明分类、打开方式、绑定对象、输入影响、显示优先级和覆盖规则。UI 可以解释、选择、确认、反馈和补充无障碍信息；不能替代场景单位、世界锚点、场景身份或物理证据。

## Player Fantasy

《云海织航》的 UI 幻想不是"界面好看"——而是**同一本航海日志的页码**被一页页翻开。玩家在 Hub → 航图 → 探索 → 返回 → 修复的闭环中，每一次从 HUD 切换到航图、从航图切换到货舱、从货舱切换到集市摊位时，体验的不是"切换 app"，而是**翻到日志的下一页：羊皮纸的触感延续，手写体风格延续，边角的磨损延续**。

锚定时刻来自碎片拼合——玩家在航图上选中 `route.storm-cut-01`，侧边面板的风险标签中，有一项标注来源为"琉璃港集市，老魏的提醒"。那是在集市买透镜维护套件时，NPC 随口提的一句"风暴走廊最近雾更大了"。这句话现在变成了航图上的一个风险标签——不是系统提示，是玩家在不同地点收集的碎片，在同一本日志的同一页上拼合了。

这本日志的每一页都服务于接下来的决策。翻到货舱单那页，看到上次出航只带回 320/500——你知道这次可以少带补给、多留空间。翻到维修记录那页，看到船体上次的伤痕补丁还在——你知道这次该避开那片区域。日志让规划有依据，让未知有形状，让每一次出发都不是盲目按键，而是翻过前面几页之后的主动选择。

航路修复主义在这一层的意思是：日志不需要完美排版。被划掉的风险标记、货舱单上被反复修改的容量数字、维修清单上从"待修"改成"已修"又改成"需复查"的条目——这些痕迹不是 UI 缺陷，而是**这本日志被人用过、在被人持续书写的证明**。日志里也有空白页——航图上那些柔雾边缘不是"未加载区域"，而是"尚未书写的页码"。它们不让人焦虑，因为玩家知道这本日志还在变厚。

## Detailed Design

### Core Rules

#### C.1 屏幕清单 (Screen Inventory)

| # | 屏幕 ID | 名称 | UI 分类 | 显示优先级 | 所属系统 | 打开触发 | 绑定对象 |
|---|---------|------|------|------|---------|---------|---------|
| S1 | `hud_hub` | Hub 常驻 HUD | `persistent_hud` | P2 当前任务级 | #7 | Hub 场景激活时自动显示 | Hub 场景状态、船体 / 仓库 / 货舱摘要 |
| S2 | `panel_station` | 站点交互面板 | `anchored_panel` 或 `modal_dialog` | P1 或 P2，按站点动作决定 | #7 | 玩家对站点按 Use | 站点世界锚点 |
| S3 | `modal_departure` | 出航确认对话框 | `modal_dialog` | P1 关键决策级 | #7/#9 | 舱门 Use（Mode A）/ 舵轮 Use（Mode B） | 舱门 / 舵轮世界锚点 |
| S4 | `screen_chart` | 航图屏幕 | `full_screen_surface` | P1 关键决策级 | #9 | 出航确认后 或 M 键快捷打开 | 航图桌 / 航线状态 / Hub 快捷入口 |
| S5 | `hud_exploration` | 探索场景 HUD | `persistent_hud` | P2 当前任务级 | #11 | EXPLORING 阶段激活 | 探索阶段、携带物、威胁预览 |
| S6a | `modal_capacity` | 容量取舍面板 | `modal_dialog` | P1 关键决策级 | #11 | 随身物品栏满格时捡到新物品 | 资源拾取事件 |
| S6b | `overlay_extraction` | 撤离读条 | `semi_modal_overlay` | P1 关键决策级 | #11 | 玩家在撤离锚点确认撤离 | 撤离锚点 |
| S6c | `modal_settlement` | 结算摘要 | `modal_dialog` | P1 关键决策级 | #11 | 撤离完成 / 返回 Hub | 探索结算事件 |
| S7 | `modal_threat` | 战斗威胁决策面板 | `modal_dialog` | P0 危机 / 阻断级 | #12 | threat 触发 | 威胁事件 |
| S8 | `panel_repair` | 修复交互面板 | `modal_dialog` | P1 关键决策级 | #13 | 玩家对修复节点按 Use | 修复节点世界锚点 |
| S9 | `modal_market` | 摊位购买界面 | `modal_dialog` | P1 关键决策级 | #14 | 玩家对摊位按 Use | 摊位 / NPC 世界锚点 |
| S10 | `modal_naming` | 伙伴命名模态 | `modal_dialog` | P0 危机 / 阻断级 | #15 | naming_prompt_eligibility=true 且到达序列中 | 到达序列 + 伙伴状态 |
| S11 | `panel_sniff` | 伙伴嗅辨面板 | `anchored_panel` | P2 当前任务级 | #15 | 玩家对伙伴驻点按 Use（嗅辨动词） | 伙伴驻点世界锚点 |
| S12 | `panel_storage` | 仓库/货舱整理界面 | `anchored_panel` | P2 当前任务级 | #5 | 玩家对仓库/货舱锚点按 Use | 仓库 / 货舱世界锚点 |

非模态面板（S2 情报台/仓库实例、S11、S12）：保持玩家移动。
模态面板（S2 模块接口/舱门/舵轮实例、S3、S6a、S6c、S7、S8、S9、S10）：阻断移动。

#### C.1a UI 分类、常驻资格与优先级

| UI 分类 | 是否可常驻 | 打开方式 | 输入影响 | 规格要求 |
| --- | --- | --- | --- | --- |
| `persistent_hud` | 可以，但必须由场景 / 阶段门控 | 场景或阶段激活自动显示 | 默认不抢焦点，不阻挡世界交互 | 必须声明最大屏幕占比、可隐藏条件和被模态覆盖时的可见性 |
| `anchored_panel` | 不可常驻 | 靠近世界锚点并按 Use，或绑定明确快捷键 | 非模态时可保留移动，模态时阻断移动 | 必须链接世界锚点或场景单位，不能从空白 UI 按钮凭空打开 |
| `modal_dialog` | 不可常驻 | 关键决策、系统事件或世界锚点触发 | 阻断世界输入，焦点锁定 | 必须声明提交 / 取消 / 禁用 / 恢复规则 |
| `semi_modal_overlay` | 短时显示 | 世界动作进行中自动显示 | 部分阻断输入 | 必须声明哪些输入仍可用、哪些被阻断 |
| `full_screen_surface` | 不与世界同时常驻 | 明确进入某个 UI 表面 | 接管输入，世界层暂停或隔离 | 必须声明进入 / 离开路径和底层世界状态处理 |
| `toast_or_hint` | 短时 | 系统反馈或失败原因 | 不抢焦点，不阻挡输入 | 必须自动消失，不能承载关键决策 |
| `debug_only` | 只在开发 / QA | debug flag 或 smoke hook | 不计入玩家体验证据 | 必须和玩家 UI 分离 |

| 显示优先级 | 说明 | 例子 | 覆盖规则 |
| --- | --- | --- | --- |
| P0 危机 / 阻断级 | 必须立即处理，否则会丢失安全、叙事或威胁上下文 | S7、S10、FatalError | 可以覆盖 P1/P2；关闭后恢复或丢弃被覆盖上下文必须写清楚 |
| P1 关键决策级 | 会提交领域状态、改变路线、消耗资源或完成结算 | S3、S4、S6a、S6b、S6c、S8、S9 | 可压过 P2/P3；同优先级必须排队或拒绝 |
| P2 当前任务级 | 帮助玩家理解当前场景 / 当前阶段 | S1、S5、S11、S12 | 被 P0/P1 覆盖时让位；不得遮挡主要世界身份 |
| P3 辅助信息级 | 解释、tooltip、短提示、无障碍补充 | Toast、hint、tooltip | 不能抢焦点，不能阻断输入 |
| P4 调试 / 开发级 | smoke、diagnostic、debug overlay | debug label | 不进入玩家 UI 验收，不证明场景完成 |

任何 UI 若没有分类、常驻资格、打开方式、绑定对象和显示优先级，不得进入 `implementation_ready`。

#### C.2 屏幕流 (Screen Flow)

```
HUB (S1 常驻)
  │
  ├─[Use 站点]──→ S2 站点面板（非模态/模态按站点类型）
  │                  │
  │                  └─[Use 舱门/舵轮]──→ S3 出航确认（模态）
  │                                         │
  │                                         └─[确认出航]──→ departure_locked 2.0s
  │                                                            │
  │                                                            ▼
  │                                                     S4 航图屏幕（全屏）
  │                                                       │
  │                              ┌─────────────────────────┤
  │                              │                         │
  │                        [选中路线→确认]           [M键/Esc 返回Hub]
  │                              │                         │
  │                              ▼                         ▼
  │                         墨水扩散 0.6s              Hub 恢复
  │                         锁定 1.2s
  │                              │
  │                              ▼
  │                    航行过渡（黑屏/加载）
  │                              │
  │                              ▼
  │                    EXPLORATION (S5 常驻 HUD)
  │                       │
  │              ┌────────┼────────┐
  │              │        │        │
  │         [物品栏满] [threat触发] [抵达撤离锚点]
  │              │        │        │
  │              ▼        ▼        ▼
  │         S6a 容量    S7 威胁   S6b 撤离读条
  │         取舍面板   决策面板   (半模态)
  │              │        │        │
  │              ▼        ▼        ▼
  │         取舍完成   战斗结算   S6c 结算摘要
  │              │        │        │
  │              └────────┴────────┘
  │                       │
  │                       ▼
  │                返回 Hub（到达序列）
  │                       │
  │                  [naming_prompt_eligibility?]
  │                       │
  │                  ┌────┴────┐
  │                 是          否
  │                  │           │
  │                  ▼           ▼
  │             S10 命名      Hub 恢复
  │             模态            S1 恢复
  │                  │
  │                  ▼
  │             Hub 恢复 / S1 恢复
```

**强制过渡保护**：
- `departure_locked = true` 期间：所有面板强制关闭，`open_screen()`/`open_modal()` 调用被静默拒绝
- 航图 DEPARTURE_CONFIRMED 状态不可逆——确认出航后无法返回 BROWSING
- 探索 EXTRACTING 阶段：除威胁触发外不可取消撤离读条

#### C.3 模态栈规则 (Modal Stack)

**规则 1 — 单模态**：同时最多一个模态面板可见。`UIManager._modal_panel` 单槽。

**规则 2 — 战斗覆盖 (CombatOverride)**：S7（战斗威胁决策面板）是唯一可以覆盖当前模态的面板。

覆盖流程：
1. 保存被覆盖面板状态到 `combat_override_stack`（单槽）：`{panel_id, data_context, scroll_offset, selected_index}`
2. 被覆盖面板 `process_mode = DISABLED`，视觉透明度降至 20%
3. S7 渲染在独立 CanvasLayer（layer=100），高于一切 UI
4. 恢复规则：
   - **应急处理 / 硬扛后**：从 stack 恢复被覆盖面板，复原滚动位置和选中索引——玩家决策上下文不丢失
   - **撤退后**：丢弃被覆盖面板——撤退意味着"放弃当前探索动作"

**规则 3 — 排队策略**：当非覆盖模态已打开，新模态请求的处理：
- S7（威胁）：覆盖——无视当前模态，执行规则 2
- S10（命名）：排队——当前模态关闭后自动打开。命名模态在到达序列中触发，此时通常无其他模态
- 其余模态：丢弃——转为非模态 Toast "当前无法操作"

**规则 4 — 非模态面板共存**：多个非模态面板可以同时打开（如 S11 嗅辨 + S1 Hub HUD），但视觉层级必须区分——后打开的面板覆盖在先打开的面板上方（z-index 递增）。

#### C.4 输入路由 (Input Routing)

事件传播链（优先级从高到低）：

```
Layer 0 (最高)  模态面板（S3/S6a/S6c/S7/S8/S9/S10）
                ↓ 若未消费
Layer 1        半模态覆盖（S6b 撤离读条）
                ↓ 若未消费
Layer 2        非模态面板（S2 非模态实例/S11/S12）
                ↓ 若未消费
Layer 3        常驻 HUD 覆盖层（S1/S5 — 物品栏点击、指标悬停 tooltip）
                ↓ 若未消费
Layer 4        世界交互层（玩家移动 WASD / Use E / 靠近锚点检测）
```

**全局按键**（始终被顶层消费，不向下穿透）：

| 按键 | 行为 | 例外 |
|------|------|------|
| `Esc` | 统一返回上一级：关闭模态→关闭非模态面板→航图取消选择→无效（世界层不消费） | S7 战斗面板中 Esc 无效（必须选择响应）；S10 命名模态中 Esc=跳过 |
| `M` | Hub 中直接打开航图 S4（快捷方式，等价于走向舱门） | 仅在 Layer 4 可用 |
| `Tab` | 焦点前进到下一个可聚焦元素 | 模态打开时焦点在模态内循环 |
| `Shift+Tab` | 焦点后退 | 同 Tab 规则 |
| `Enter` | 确认当前焦点元素的动作 | — |
| `方向键 ↑↓←→` | 列表/网格内导航 | 仅当焦点在列表/网格容器内 |
| `WASD` | 玩家移动 | 仅在 Layer 4 可用，Layer 0-3 打开时被阻断 |
| `E` | Use / 确认上下文动作 | 战斗面板中=应急处理；世界层=交互 |

#### C.5 焦点管理 (Focus Management)

**全局规则**：
- 模态打开时：焦点自动移到第一个可交互元素（`grab_focus()`）
- 模态关闭时：焦点恢复到打开模态之前的元素
- 只读元素（标签、纯文本、状态条）：`focus_mode = Control.FOCUS_NONE`，Tab 链自动跳过
- 灰显/不可用按钮：仍可 Tab 聚焦，但 Enter 无响应 + `tooltip` 解释为何不可用
- Godot 4.6 dual-focus 强制规则：所有可点击控件必须在鼠标点击时显式同步键盘焦点（`grab_focus()` on `MOUSE_BUTTON_PRESSED`）

**Theme 焦点/hover 样式区分**（4.6 dual-focus 要求）：
- `focus` 样式（键盘焦点）：实色边框——航标青 `#4FB7B2` 1.5px 边框
- `hover` 样式（鼠标悬浮）：半透明底色叠加 10% 亮度——不产生边框
- 两套样式视觉上不可混淆

**代表性屏幕焦点顺序**：

航图 BROWSING 状态：`航线列表项1 → 航线列表项2 → 谣言切换开关 → 关闭航图按钮`
航图 ROUTE_SELECTED 状态：`航线名(只读) → 风险标签1...n → 确认出航按钮(默认焦点) → 取消选择 → 关闭航图`

探索 HUD：`物品栏格1→格2→格3→格4→格5 → [搜索进度/船体HP/威胁预览(只读,跳过)] → 存储警告图标(条件)`

修复面板：`节点名(只读) → 材料1(含+/-按钮) → 材料2...n → 解锁预览(只读) → 确认提交 → 取消`

摊位界面：`NPC名(只读) → 商品行1(含购买按钮) → 商品行2...n → 关闭按钮`

#### C.6 HUD 更新策略 (HUD Update Strategy)

**核心原则**：信号驱动 + 脏标记批量更新。HUD 永不做 `_process()` 轮询。

**信号 → HUD 元素映射**：

| 信号（来源系统） | 目标 HUD | 更新行为 |
|---|---|---|
| `hull_integrity_changed(old, new)` (#8) | S1 Hub HUD 船体分段条 | 更新分段亮起数 + 波段色 |
| `hull_band_changed(old_band, new_band)` (#8) | S1/S5 船体条颜色 | 绿/黄/红波段切换（76/26/1） |
| `storage_changed(current, max)` (#5) | S1 Hub HUD 仓库余量 | 更新 "920/1000" 文本 + 进度条 |
| `cargo_changed(current, max, has_module)` (#5) | S1 Hub HUD 货舱装载 | 更新数值 或 显示"无货舱" |
| `carried_changed(slot, item, qty)` (#5) | S5 探索 HUD Pool 5 | 更新指定格图标+数量；5/5 时橙色边框 |
| `search_progress_changed(searched, total)` (#11) | S5 探索 HUD 搜索计数 | 更新 "3/6" 文本 |
| `scout_preview_changed(level)` (#11) | S5 探索 HUD 威胁预览 | 无标记 / ! / 完整标签 三态切换 |
| `storage_capacity_warning(active)` (#5) | S5 探索 HUD 存储警告 | 黄色图标显示/隐藏（30s 防抖） |
| `module_state_changed(slot, state)` (#8) | S1 Hub HUD 模块状态灯 | ✓/⚡/○ 形状+颜色双编码 |
| `currency_changed(new_balance)` (#5) | S9 摊位界面 / S1 Hub HUD | 更新货币显示 |
| `nest_state_changed(stage)` (#15) | S1 Hub 伙伴驻点标记 | 巢穴进度 1-4 小点叠加 |

**批量更新机制**：
```
HUDManager._on_signal(element_id, payload):
    dirty_flags[element_id] = true
    pending_payloads[element_id] = payload

HUDManager._process(_delta):  # process_priority = -10，确保在渲染前完成
    if dirty_flags 中有任何 true:
        for each dirty element:
            update_element(element_id, pending_payloads[element_id])
        dirty_flags.clear()
```

每帧最多执行一次 UI 节点更新。空闲帧 `dirty_flags` 全 false 时 `_process` 零开销。

**HUD 可见性门控**：S1 仅在 Hub 场景激活时可见；S5 仅在 `EXPLORING` 和 `EXTRACTING` 阶段可见（ARRIVING/DEPARTED 隐藏）。

#### C.7 面板生命周期 (Panel Lifecycle)

**非模态面板**（S2 非模态实例 / S11 / S12）：
```
PROXIMITY_ENTER → PRELOAD → READY → ACTIVE → PROXIMITY_EXIT → CLOSE
```
- 进入 1.5x `anchor_radius`：预加载面板数据（异步，不阻塞）
- 玩家按 Use：面板打开（0.25s 羊皮纸翻开动画）
- 离开 2x `anchor_radius`：面板自动关闭（0.15s 合上动画）
- 玩家手动 Esc：立即关闭

**模态面板**（S3 / S6a / S6c / S7 / S8 / S9 / S10）：
- 不依赖距离——由事件触发打开
- 仅手动关闭（Esc / 按钮 / 系统事件如战斗结算）
- S6b 撤离读条：半模态——事件驱动，不可手动关闭（除非被 S7 打断）

**全屏面板**（S4 航图）：
- 场景级别的屏幕切换（visible 切换，非 `change_scene`）
- 进出由屏幕流状态机控制，不由距离或 Esc 直接关闭

**懒加载策略**：
- S4：进入航图时 `load()` 航图场景（首次 5-20ms），缓存 PackedScene
- 站点面板（S2）：使用**单个通用 `StationDetailPanel` 模板**（非 10 个独立场景）。数据在打开时从内容注册表查询绑定
- 模态面板：HUD 初始化时预加载（`preload()`）战斗威胁面板 S7（探索中点触发不可等待）。其余模态首次打开时加载
- 缓存池：最大同时缓存 2 个面板实例（LRU 淘汰）。场景切换时缓存池清空

#### C.8 动画与过渡时序 (Animation & Timing)

| 动画 | 时长 | 缓动 | 触发 | 来源 |
|------|------|------|------|------|
| 面板翻开（羊皮纸） | 0.25s | ease-out | 非模态面板打开 | #16 定义 |
| 面板合上 | 0.15s | ease-in | 非模态面板关闭 | #16 定义 |
| departure_locked | 2.0s | — | 出航确认后 | #7 常量 `base_lock_duration` |
| 航线选中脉冲 | 0.3s | ease-in-out | 航图选中航线 | #9 |
| 墨水扩散 | 0.6s | ease-out | 确认出航第二步 | #9 |
| 出发口封闭+锁定 | 1.2s | linear | 墨水扩散完成后 | #9 |
| 撤离读条 | 2-3s（可配置） | linear | 确认撤离 | #11 |
| 撤离读条打断闪烁 | 0.5s × 3 次 | — | 撤离被威胁打断 | #11 |
| 进度 Toast | 2-3s（入场 0.2s） | ease-out→ease-in | 修复提交 / 物品转移 | #13 / #16 |
| 修复完成公告 | 3s 或点击关闭 | ease-out | 修复节点完成 | #13 |
| 结算摘要入场 | 0.5s | ease-out | 撤离完成 | #11 |
| 命名模态弹出 | 0.3s | ease-out | 到达序列中 naming_eligible | #15 |

**动画性能约束**（Godot 4.6.2 desktop renderer）：
- 所有 UI 动画使用 `create_tween()`（SceneTreeTween），禁止手动 `_process()` 插值
- 墨水扩散动画必须使用 ShaderMaterial + uniform `progress`（GPU 侧完成），禁止 Canvas `draw_*()` 逐帧绘制
- 全屏羊皮纸纹理使用 `NinePatchRect`（9-slice）展开，不使用整张 2048×2048 纹理

#### C.9 UI 语义权威 (UI Semantic Authority)

#16 是 MVP 中所有 UI 颜色、术语、交互模式规范的唯一权威来源。以下规范对所有其他 GDD 的 UI 规格具有覆盖效力。

**统一语义色板**（UI 层专用，覆盖 #13/#8/#12 中的色值冲突）：

| 语义 | UI Hex | 用途 | 对应艺术圣经色 |
|------|--------|------|---------------|
| 危险/不足/损伤 | `#D4644B` | 船体红波段、材料不足、hull≤38 警告 | 警戒锈红 `#C8644B` |
| 满足/完好/安全 | `#5FAF5F` | 材料满足、船体绿波段 | UI 专用绿色（区别于航标青） |
| 可交互/可用 | `#4FB7B2` | 可点击按钮、可通行路线、活跃指示 | 航标青（原值保留） |
| 关键/奖励 | `#C8A34E` | 核心资源、航线线路、重要标记 | 路线金（原值保留） |
| 已修复/完成 | `#E8DCC0` | 修复完成公告、已连通标记 | 补缝帆白（原值保留） |
| 未知/未确认 | `#D8DAD4` | 传闻路线、未探明区域、空状态 | 雾灰白（原值保留） |
| 不可用/灰显 | `#7A7068` | 灰显按钮、disabled 状态、无模块提示 | 暗损灰褐（原值保留） |
| 警告/提醒 | `#E8A840` | 存储警告图标、跨波段警告 ⚠ | UI 专用琥珀色 |

**色板偏差说明**：`#D4644B`（危险红）相比艺术圣经的 `#C8644B`（警戒锈红）略微提亮了红色通道，原因是 UI 危险色在羊皮纸底色（`#E4D2B3` 帆布米）上的 WCAG AA 对比度需求——`#C8644B` 在帆布米上对比度仅 3.8:1（不达标），`#D4644B` 达到 4.52:1（AA 达标）。此偏差仅在 UI 层生效，世界内锈红色资产仍使用 `#C8644B`。

**硬性无障碍要求**：所有 24px 以下的颜色编码必须同时使用形状/图标/文本三重区分。船体波段=色条+分段数+形状(✓/⚡/○)。材料满足=绿色+✓图标+文字"满足"。材料不足=红色+✗图标+文字"不足"。

**术语统一**（玩家面向）：

| 统一术语 | 替代 | 来源冲突 |
|----------|------|---------|
| 船体完整性 | "船体HP"(#11) / "Hull Status"(#12) | #7/#11/#12 |
| 随身物品栏 | "Pool 5"(#5/#12 内部) | #5/#11/#12 |
| 货舱 | "cargo hold"(#7) | 统一中文"货舱" |
| 云海币 | "currency"(#5) | 使用 registry 注册名 `currency.cloud-coins` 的 display_name |

### States and Transitions

#### C.10 屏幕状态机 (Screen State Machine)

UIManager 维护唯一的活跃屏幕状态。状态转换由领域系统事件驱动：

| 当前状态 | 触发事件 | 新状态 | 副作用 |
|---------|---------|--------|--------|
| `HUB` | `use_gangway()` | `HUB` + S3 模态 | 打开出航确认对话框（Mode A） |
| `HUB` | `use_helm()` | `HUB` + S3 模态 | 打开出航确认对话框（Mode B） |
| `HUB` + S3 | `departure_confirmed` | `DEPARTURE_LOCKED` | departure_locked 2.0s，所有面板关闭 |
| `DEPARTURE_LOCKED` | `lock_timer_complete` | `CHART` | 场景切换到 S4 航图 |
| `HUB` | `press_m_key` | `CHART` | M 键快捷打开航图 |
| `CHART` | `route_selected` | `CHART_ROUTE_SELECTED` | 侧边面板展开 |
| `CHART_ROUTE_SELECTED` | `departure_confirmed` | `CHART_DEPARTURE_CONFIRMED` | 墨水扩散 0.6s → 锁定 1.2s |
| `CHART_DEPARTURE_CONFIRMED` | `lock_complete` | `VOYAGE` | 黑屏过渡 → 航行 |
| `CHART` / `CHART_ROUTE_SELECTED` | `esc_pressed` | `HUB` | 关闭航图，恢复 Hub |
| `VOYAGE` | `encounter_context_ready` | `EXPLORATION` | S5 HUD 激活 |
| `EXPLORATION` | `threat_triggered` | `EXPLORATION` + S7 覆盖 | 战斗威胁覆盖层 |
| `EXPLORATION` + S7 | `threat_resolved` | `EXPLORATION` | 恢复 S5 + 被覆盖面板（如有） |
| `EXPLORATION` | `extraction_started` | `EXTRACTING` + S6b | 撤离读条 |
| `EXTRACTING` | `extraction_interrupted` | `EXPLORATION` | S6b 红色闪烁 → 关闭 |
| `EXTRACTING` | `extraction_complete` | `SETTLEMENT` + S6c | 结算摘要 |
| `SETTLEMENT` + S6c | `settlement_confirmed` | `HUB_ARRIVING` | 到达序列 |
| `HUB_ARRIVING` | `arrival_complete + naming_eligible` | `HUB` + S10 | 命名模态弹出 |
| `HUB_ARRIVING` | `arrival_complete + !naming_eligible` | `HUB` | Hub 恢复 + S1 |

#### C.11 模态栈状态 (Modal Stack State)

UIManager 维护以下运行时状态：

| 状态变量 | 类型 | 含义 |
|---------|------|------|
| `_active_screen` | StringName | 当前活跃的全屏面板 ID（HUB/CHART/EXPLORATION） |
| `_modal_panel` | Control|null | 当前打开的模态面板实例（单槽） |
| `_modal_id` | StringName | 当前模态面板 ID |
| `_combat_override_stack` | Dictionary|null | 被战斗覆盖的面板保存状态 |
| `_departure_locked` | bool | 是否处于 departure_locked 期间 |
| `_non_modal_panels` | Array[Control] | 当前打开的非模态面板列表（按 z-index 排序） |
| `_dirty_flags` | Dictionary[StringName, bool] | HUD 元素的脏标记 |
| `_pending_payloads` | Dictionary[StringName, Variant] | 待应用的 HUD 更新数据 |
| `_panel_cache` | Dictionary[StringName, Control] | 面板实例缓存池（LRU，最大 2） |

### Interactions with Other Systems

#### C.12 上游数据接口 (Upstream Data Contracts)

#16 不拥有任何数据——它通过以下接口从领域系统获取显示数据：

| 接口 | 来源系统 | 调用时机 | 返回数据 |
|------|---------|---------|---------|
| `get_chart_state()` | #9 | 进入 S4 / S4 刷新 | chart_state, visible_routes[], selected_route_id |
| `get_visible_routes()` | #9 | S4 BROWSING 渲染 | route[] {id, knowledge_state, distance_band, display_order} |
| `get_selected_route()` | #9 | S4 ROUTE_SELECTED 渲染 | route详情 {name, risk_tags[], sources[], known_risks, unknown_risks} |
| `get_filter_state()` | #9 | S4 谣言切换 | hide_rumored: bool |
| `get_hull_integrity()` | #8 | S1/S5 HUD 更新 | current_hull, max_hull, band{green|yellow|red} |
| `get_module_states()` | #8 | S1 HUD 更新 | module[] {slot_id, installed, efficiency} |
| `get_carried_inventory()` | #5 | S5 HUD 更新 | slot[] {item_id, icon, qty} |
| `get_storage_state()` | #5 | S1 HUD / S12 更新 | current_volume, max_volume |
| `get_cargo_state()` | #5 | S1 HUD / S12 更新 | current_volume, current_mass, max_volume, max_mass, has_module |
| `get_currency()` | #5 | S1/S9 更新 | balance: int |
| `get_search_progress()` | #11 | S5 HUD 更新 | searched_count, total_count |
| `get_scout_preview_level()` | #11 | S5 HUD 更新 | PREVIEW_NONE / PREVIEW_PRESENCE / PREVIEW_FULL |
| `get_extraction_state()` | #11 | S6b 状态 | extraction_progress, is_interrupted |
| `build_threat_context()` | #12 | S7 打开时 | threat_name, description, hull_state, hull_band, options[] |
| `get_repair_state(node_id)` | #13 | S8 打开时 | node_name, current_state, materials[], unlock_preview |
| `get_stall_data(stall_id)` | #14 | S9 打开时 | npc_name, stall_label, goods[], player_currency |
| `query_partner_name()` | #15 | S10/S11 渲染 | partner_name 或 fallback "那只灰白猫" |
| `get_sniff_items()` | #15 | S11 打开时 | item[] {id, name, cat_sniff_signature} (仅 signature!=null) |
| `naming_prompt_eligibility()` | #15 | 到达序列 | bool (4 路合取) |
| `get_display_name(entity_id)` | #1 | 任何面板渲染时 | display_name: String |
| `get_description(entity_id)` | #1 | 悬停 tooltip | description: String |

#### C.13 下游事件发出 (Downstream Events Emitted)

#16 在以下用户交互时刻发出语义事件（供 #17 反馈系统消费）：

| 事件 | 触发时刻 | 携带数据 |
|------|---------|---------|
| `ui_route_selected` | 航图中选中航线 | route_id, route_name |
| `ui_departure_confirmed` | 出航确认第二步 | route_id, departure_mode{A|B} |
| `ui_threat_response_chosen` | 战斗面板选择响应 | threat_id, response{suppressed|tanked|retreated} |
| `ui_repair_submitted` | 修复确认提交 | node_id, materials_submitted[] |
| `ui_purchase_confirmed` | 购买确认 | stall_id, good_id, quantity, total_cost |
| `ui_item_transferred` | 物品在随身栏↔仓库间转移 | item_id, from_pool, to_pool, qty |
| `ui_naming_confirmed` | 伙伴命名确认 | partner_id, chosen_name |
| `ui_settlement_closed` | 结算摘要关闭 | voyage_id, items_brought[], intel_gained[] |
| `ui_panel_opened` | 任何模态/非模态面板打开 | panel_id |
| `ui_panel_closed` | 任何模态/非模态面板关闭 | panel_id |

## Formulas

### D.1 容量条宽度比例

HUD 中仓库余量条和货舱容量条的渲染宽度：

`bar_width_px = clamp(current / max, 0.0, 1.0) × max_bar_width_px`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 当前值 | current | int | 0–max | 当前已用容量 |
| 最大值 | max | int | ≥1 | 最大容量（#5 常量 `storage_base_volume`=1000） |
| 最大像素宽度 | max_bar_width_px | float | 100–400 | 容量条在屏幕上的最大像素宽度 |

**输出范围**：0 到 max_bar_width_px px。current > max 时 clamp 到 max_bar_width_px。
**示例**：仓库 current=920, max=1000, bar_width=200 → 渲染宽度 = (920/1000) × 200 = 184px。

### D.2 船体波段颜色判定

来自 #8 的波段阈值，由 UI 映射为颜色：

```
band_color(hull_integrity):
    if hull_integrity > 26:  return BAND_GREEN   # #5FAF5F
    if hull_integrity > 1:   return BAND_YELLOW  # #E8A840
    if hull_integrity <= 1:  return BAND_RED      # #D4644B
```

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| 船体完整性 | hull_integrity | int | 0–100 | 当前船体值 |
| 绿波段阈值 | — | int | >26 | #8 规范值 76 为起始上限 |
| 黄波段阈值 | — | int | 2–26 | 船体受损波段 |
| 红波段阈值 | — | int | ≤1 | 船体濒危波段 |

**输出范围**：3 种离散颜色。波段切换触发 `hull_band_changed` 信号 → HUD 脏标记。

### D.3 Toast 垂直堆叠偏移

多条 Toast 同时显示时的位置计算：

`toast_y(i) = base_y - i × (TOAST_HEIGHT + TOAST_GAP)`

| 变量 | 符号 | 类型 | 范围 | 描述 |
|------|------|------|------|------|
| Toast 序号 | i | int | 0–2 | 最新 Toast=0（最上方），最多 3 条同时 |
| 基础 Y 坐标 | base_y | float | — | 屏幕底部偏上偏移 |
| Toast 高度 | TOAST_HEIGHT | float | 28–40px | 单条 Toast 高度 |
| Toast 间距 | TOAST_GAP | float | 4–8px | 条间间距 |

**输出范围**：3 个离散 Y 坐标。超出 3 条时最旧的 Toast 被移除（FIFO）。

## Edge Cases

- **若桌面窗口失焦、最小化或系统暂停后恢复（`NOTIFICATION_APPLICATION_FOCUS_IN` / resume equivalent）**：UI 状态从内存或最近有效 UI model 恢复——所有面板的可见性、数据绑定和焦点位置与恢复前一致。信号驱动的 HUD 在暂停期间不接收玩法更新，自然冻结。恢复后第一个状态变化信号触发脏标记→完整刷新。若 `_process` delta > 1.0s（异常大 delta = 系统暂停或恢复），触发一次全量 `_request_full_ui_refresh()`。

- **若多个模态同时请求打开**：UIManager 按优先级裁决。S7（战斗威胁）覆盖当前模态（保存状态→覆盖）。S10（伙伴命名）排队（当前模态关闭后自动打开）。其余模态请求被丢弃并转为非模态 Toast "当前无法操作"。同一优先级不会同时触发（战斗威胁和容量取舍已被 #11 EC-11-04 保护——取舍面板打开时探索暂停）。

- **若面板打开期间底层数据变更（数据竞态）**：面板在每次打开时调用 `bind_data()` 获取领域系统最新数据。面板打开期间不自动刷新——玩家看到的是打开时刻的快照。若数据在面板打开后变更（如其他系统修改了仓库内容），面板关闭后 HUD 在下一帧通过脏标记更新。模态面板关闭时不依赖面板内数据做写回——提交动作通过领域系统 API 完成。

- **若零物品面板打开**：每个面板必须实现空状态视图。嗅辨面板（S11）零合格物品时显示"猫没有闻到任何值得注意的气味——试试从探索中带回更多材料"（#15 E.2.a）。仓库（S12）为空时显示"从探索中带回材料或拆包货物来填充"（#5）。航图（S4）无可见路线时显示"没有可读取的航线——去情报台了解更多信息"。

- **若 departure_locked 期间有面板请求**：UIManager 维护 `_departure_locked` 标志（由 `route_committed` 信号置 true，2.0s 定时器后清）。锁定期间 `open_screen()` 和 `open_modal()` 静默拒绝。锁定开始时 `force_close_all_panels()` 强制关闭所有已打开面板。定时器使用 `SceneTreeTimer`，并在桌面暂停/恢复后由 UIManager 校验锁状态。

- **若命名模态触发条件与到达序列时序冲突**：`naming_prompt_eligibility()` 是 4 路合取（#15 `naming_prompt_eligibility` 公式）。到达序列完成时检查此条件——若 true，S10 在到达序列的最后 0.3s 弹出。若 `naming_skip_count >= NAMING_SKIP_MAX`（3），即使其余 3 条件满足也不弹出。S10 阻断所有 UI 含出航控件（#15 E.1.g）。

- **若船体归零（hull=0）后返回 Hub**：#8 的 `can_depart()` 返回 false。S1 Hub HUD 的船体条显示红波段（hull=1）。货舱/仓库正常显示。玩家需要去维修站点修复船体。UI 在船体条旁显示闪烁的扳手图标（"需要维修"），指引玩家走向维修站点。

- **若货舱模块未安装**：S1 Hub HUD 的货舱装载区域置灰，文本显示"无货舱"而非"0/0"（#7 R6）。S12 货舱整理界面不可打开（Use 货舱锚点时若 `cargo_hold_exists()==false`，显示 Toast"需要先安装货舱模块"）。

- **若战斗威胁覆盖容量取舍面板**：正常情况下不会发生（#11 EC-11-04 保护），但 #16 必须防御性处理。若因竞态发生：S7 覆盖 S6a，S6a 的状态保存到 `combat_override_stack`。应急/硬扛后恢复 S6a（全部物品决策保留）。撤退后丢弃 S6a——但撤退本身意味"放弃当前探索动作"，丢弃合理。

- **若 Tab 导航时面板内无可聚焦元素**：焦点停留在面板容器本身（`focus_mode = FOCUS_ALL`），不穿透到下层。面板关闭时焦点恢复到打开前的元素。若打开前的元素已被销毁（场景切换），焦点移到当前屏幕的第一个可聚焦元素。

- **若玩家在战斗决策面板中按 Esc**：无效。S7 要求必须选择一个响应（E/T/R）。Esc 在此面板中被消费但不触发任何动作。此行为需要在面板中通过视觉提示说明（"选择一个响应以继续"）。

- **若桌面暂停/恢复后 UI 与游戏状态不同步**：`_process` 检测 delta > 1.0s → 调用 `_request_full_ui_refresh()` → 遍历所有活跃 HUD 元素，重新从领域系统拉取最新值并强制写入节点（绕过脏标记优化，确保一致性）。

- **若玩家首次返回 Hub 且携带了修复材料**：此时玩家可能尚未意识到"材料可以用于修复世界节点"。修复站点（S8 锚点）在 Hub 中应有微妙的视觉引导——不同于完整的新手引导教程（#18 的范畴），而是一个低侵入性的环境提示（如修复站点锚点发出微弱脉冲光，或在靠近时显示一行提示文字"这里似乎可以用材料修复"）。具体引导形式和触发条件由 #18（新手引导与首轮闭环）最终定义，但 S8 面板的打开锚点必须预留可被引导系统高亮的标记接口（`highlightable = true` + `highlight_priority` 字段）。（Owner: #18 + ux-designer, Target: Vertical Slice）

## Dependencies

### 上游依赖（硬依赖——#16 若无此系统无法运作）

| # | 系统 | 依赖性质 | #16 消费的接口 |
|---|------|---------|---------------|
| #5 | 资源、货物与容量 | 硬 | `get_carried_inventory()`, `get_storage_state()`, `get_cargo_state()`, `get_currency()` → S1/S5/S9/S12 数据源 |
| #8 | 飞艇模块与船体状态 | 硬 | `get_hull_integrity()`, `get_module_states()` → S1/S5 船体条/模块灯数据源 |
| #9 | 航图与航线规划 | 硬 | `get_chart_state()`, `get_visible_routes()`, `get_selected_route()`, `get_filter_state()` → S4 数据源 |
| #11 | 探索 / 搜撤场景 | 硬 | `get_search_progress()`, `get_scout_preview_level()`, `get_extraction_state()` → S5/S6b/S6c 数据源 |
| #12 | 战斗与威胁处理 | 硬 | `build_threat_context()` → S7 数据源 |
| #13 | 世界修复与解锁 | 硬 | `get_repair_state(node_id)` → S8 数据源 |
| #14 | 空港 / 村镇状态与集市交易 | 硬 | `get_stall_data(stall_id)` → S9 数据源 |

### 上游依赖（软依赖——增强体验但非必需）

| # | 系统 | 依赖性质 | #16 消费的接口 |
|---|------|---------|---------------|
| #1 | 内容数据与状态注册表 | 软 | `get_display_name()`, `get_description()` → 所有面板的文本渲染 |
| #7 | 飞艇家园 Hub | 软 | 站点锚点位置/类型 → S2 触发条件；但 Hub 场景结构来自 #4 移动/交互 |
| #15 | 伙伴功能与关系 | 软 | `query_partner_name()`, `get_sniff_items()`, `naming_prompt_eligibility()` → S10/S11 数据源 |

### 下游依赖（依赖 #16 的系统）

| # | 系统 | 依赖性质 | 消费 #16 的什么 |
|---|------|---------|----------------|
| #17 | 反馈、特效与音频语义 | 软 | 消费 #16 发出的语义事件（`ui_route_selected`, `ui_departure_confirmed`, `ui_threat_response_chosen`, `ui_repair_submitted`, `ui_purchase_confirmed` 等）→ 触发对应的音频/视觉反馈 |
| #18 | 新手引导与首轮闭环 | 软 | 消费 #16 的面板打开/关闭事件和焦点状态 → 引导高亮叠加层定位 |

### 非直接依赖但需读取

| # | 系统 | 原因 |
|---|------|------|
| #1 | 内容数据与状态注册表 | 所有面板的 `display_name` 和 `description` 键通过 #1 查询；`cat_sniff_signature` 字段由 #1 定义 |
| #15 | 伙伴功能与关系 | S10/S11 的 UI 契约由 #15 F.5 标记指定；`naming_prompt_eligibility` 公式的输出来决定 S10 是否弹出 |

## Tuning Knobs

| 参数 | 默认值 | 安全范围 | 单位 | 影响 |
|------|--------|---------|------|------|
| `panel_open_duration` | 0.25 | 0.15–0.4 | 秒 | 非模态面板翻开动画时长。过快=无翻页感；过慢=迟钝 |
| `panel_close_duration` | 0.15 | 0.1–0.3 | 秒 | 非模态面板合上动画时长 |
| `departure_lock_duration` | 2.0 | 1.5–3.0 | 秒 | #7 常量 `base_lock_duration`。#16 直接引用，不重复定义 |
| `ink_spread_duration` | 0.6 | 0.4–1.0 | 秒 | 墨水扩散动画。过快=动画不可见；过慢=玩家等待不耐烦 |
| `chart_lock_duration` | 1.2 | 0.8–2.0 | 秒 | 确认出航后不可逆锁定 |
| `extraction_bar_duration` | 2.5 | 1.5–4.0 | 秒 | 撤离读条时长（#11 常量 `extraction_channel_duration`） |
| `toast_duration` | 3.0 | 2.0–5.0 | 秒 | 单条 Toast 显示时长（不含入场动画 0.2s） |
| `toast_max_count` | 3 | 1–5 | 条 | 同时显示的最大 Toast 数量 |
| `toast_height` | 32 | 28–40 | px | 单条 Toast 高度 |
| `storage_warning_debounce` | 30.0 | 15.0–60.0 | 秒 | 存储空间警告的去抖间隔（#11 指定） |
| `sniff_confirm_debounce` | 0.5 | 0.3–1.0 | 秒 | 嗅辨面板确认按钮去抖（#15 指定） |
| `modal_backdrop_alpha` | 0.6 | 0.4–0.8 | — | 模态遮罩透明度。过低=下层太可见；过高=完全遮蔽世界 |
| `panel_lazy_preload_radius` | 1.5 | 1.2–2.0 | × anchor_radius | 面板预加载触发距离倍数 |
| `panel_auto_close_radius` | 2.0 | 1.5–3.0 | × anchor_radius | 非模态面板自动关闭距离倍数 |
| `panel_cache_max` | 2 | 1–5 | 实例 | 面板缓存池最大实例数 |
| `hud_max_bar_width` | 200 | 100–400 | px | 容量条/船体条最大像素宽度 |
| `focus_outline_width` | 1.5 | 1.0–3.0 | px | 键盘焦点边框宽度 |
| `combat_override_layer` | 100 | 50–200 | CanvasLayer layer | 战斗覆盖层的渲染层级 |

## Visual/Audio Requirements

### 视觉资产需求

| 资产 | 描述 | 规格 | 优先级 |
|------|------|------|--------|
| 羊皮纸面板底纹 | 非模态面板的背景纹理——帆布米 `#E4D2B3` 底色 + 轻微纸纹 + 边缘磨损 | 9-slice（`NinePatchRect`），中心可平铺。不超 256×256 源纹理 | MVP |
| 模态遮罩 | 半透明暗化覆盖层 | 纯色 `#000000`，opacity 0.6 | MVP |
| 航图底图 | 航图屏幕的背景——雾灰白 `#D8DAD4` 云海纹理 + 网格线 | 2048×2048，9-slice 展开 | MVP |
| 墨水扩散 Shader | 确认出航时墨水沿航线向外扩散的效果 | ShaderMaterial，uniform `progress: 0→1`，fragment shader 中距离场判断 | MVP |
| 路线节点图标 | 航图上的站点圆点：空心(未启用)/实心(已确认)/环线叠加(当前选中) | SVG 或 32×32 纹理，航标青/路线金 | MVP |
| 波段指示灯 | 船体状态 ✓/⚡/○ 三种形状 | 16×16 图标，绿色/琥珀/红色+形状 | MVP |
| 焦点边框 | 键盘焦点的航标青 `#4FB7B2` 1.5px 实色边框 | Theme StyleBox | MVP |
| Toast 背景 | 半透明羊皮纸色圆角矩形 | 帆布米底色，opacity 0.85，4px 圆角 | MVP |
| 威胁标记图标 | 无标记 / ! / 完整标签三种状态 | 24×24 图标，琥珀色 `#E8A840` | MVP |
| 存储警告图标 | 黄色三角形警告 | 20×20，琥珀色 | MVP |

### 动画资产需求

- 面板翻开动画：0.25s ease-out，从中心向外展开 + 透明度 0→1
- 面板合上动画：0.15s ease-in，向中心收拢 + 透明度 1→0
- 航线选中脉冲：0.3s ease-in-out，暖金描边宽度从 1px→3px→1px
- 墨水扩散：0.6s ease-out，ShaderMaterial progress 0→1
- Toast 入场：0.2s ease-out，从下方滑入 + 淡入
- 撤离读条中断闪烁：0.5s×3，红色闪烁

### 音频语义事件

由 #17 实现，但 #16 定义触发时机：

| 事件 | 触发时机 | 建议音频方向 |
|------|---------|-------------|
| `ui_panel_open` | 任何面板打开 | 羊皮纸翻开轻响——干燥、短促、纸质感 |
| `ui_panel_close` | 任何面板关闭 | 羊皮纸合上轻响——比翻开更轻 |
| `ui_button_click` | 按钮点击 | 航海日志翻页声——轻、木质感 |
| `ui_route_selected` | 航图选中航线 | 航标确认音——短促、金属/玻璃感 |
| `ui_departure_confirmed` | 出航确认 | 舱门锁闭+航行启动——低沉、有重量 |
| `ui_threat_appeared` | 威胁面板打开 | 警报音——短、锐、但不刺耳（航路修复主义：像仪表警报而非怪物咆哮） |
| `ui_repair_complete` | 修复完成公告 | 复通音——暖、扩散感、像灯塔重新点亮 |
| `ui_toast_appear` | Toast 弹出 | 轻微提示音——可选，默认静音（避免过度打扰） |

## UI Requirements

本系统是 UI 系统本身——以下为实施前必须创建的 UX 规格：

### Phase 4 前必须创建的 UX Spec

| UX Spec | 对应屏幕 | 内容 |
|---------|---------|------|
| `design/ux/hub-hud.md` | S1, S2, S3 | Hub HUD 布局、站点面板通用模板、出航确认布局 |
| `design/ux/chart-screen.md` | S4 | 航图 70/30 布局、路线渲染层级、侧边面板布局、墨水扩散视觉规格 |
| `design/ux/exploration-hud.md` | S5, S6a, S6b, S6c | 探索 HUD 布局、容量取舍面板布局、撤离读条、结算摘要 |
| `design/ux/threat-panel.md` | S7 | 战斗威胁决策面板完整布局、颜色/波段编码、武器提示 |
| `design/ux/repair-panel.md` | S8 | 修复交互面板布局、材料清单+数量选择器规格 |
| `design/ux/market-panel.md` | S9 | 摊位购买界面布局、购买确认浮层 |
| `design/ux/partner-ui.md` | S10, S11 | 命名模态布局、嗅辨面板布局、伙伴站点 hint |
| `design/ux/storage-panel.md` | S12 | 仓库/货舱整理界面布局、网格+排序+标签筛选 |

### Godot 4.6 实施注意

- 在 `design/ux/` 创建之前，故事文件引用本 GDD 的 Detailed Design 作为 UI 规格的临时来源
- Theme 资源：创建单一 `ui_theme.tres`，定义所有颜色常量、字体规格、StyleBox（focus/hover/disabled）
- 字体：不超过 3 种规格——标题（24px）、正文（16px）、标注（12px）。桌面构建使用项目内嵌字体或系统回退字体，并验证 DPI 缩放下的可读性
- 所有可见字符串使用 `tr()` 包装，为后续本地化做准备

## Acceptance Criteria

每个 AC 使用 GIVEN/WHEN/THEN 格式，可由 QA 测试人员独立验证。

### 屏幕显示

- **AC-01**：GIVEN 玩家在 Hub 场景中，WHEN Hub 场景激活完成，THEN S1 HUD 显示船体完整性分段条（颜色按当前波段）、仓库余量（如 "920/1000"）、货舱装载（如 "320/500" 或"无货舱"）。
- **AC-02**：GIVEN 玩家进入 EXPLORING 阶段，WHEN S5 激活，THEN 随身物品栏 5 格显示（图标+数量）、搜索点计数显示（如 "3/6"）、船体 HP 简条显示、威胁预览标记显示（无/!/完整之一）。
- **AC-03**：GIVEN 航图屏幕 S4 打开，WHEN 处于 BROWSING 状态，THEN 可见路线按 `display_order` 排序显示，传闻路线为虚线、已识别为实线、已验证为暖金辉光。不可选路线 60% 透明度。航图占 70% 面积，侧边面板占 30%。

### 交互

- **AC-04**：GIVEN 玩家在航图 BROWSING 状态，WHEN 鼠标悬停一条可见路线，THEN 该路线微亮，侧边面板显示路线名称+风险标签摘要+来源标注+一句话概述。
- **AC-05**：GIVEN 玩家在航图 BROWSING 状态，WHEN 点击一条可选择的路线，THEN 路线发出暖金色脉冲 0.3s，侧边面板展开完整详情（所有风险标签、所有来源、已知 vs 未知风险计数），"确认出航"按钮出现并自动获得焦点。其余路线变暗至 40%。
- **AC-06**：GIVEN 玩家在 Hub 中按 M 键，WHEN 当前无模态面板打开且非 departure_locked，THEN S4 航图屏幕打开。GIVEN 玩家在航图中按 Esc，WHEN 处于 BROWSING 状态，THEN 航图关闭，返回 Hub。
- **AC-07**：GIVEN 探索中随身物品栏已满（5/5），WHEN 玩家拾取新物品，THEN S6a 容量取舍面板以模态方式打开（全屏半透明遮罩+中央面板，新物品列表在左+现有背包在右）。

### 状态变更

- **AC-08**：GIVEN 船体完整性从 30 变为 25（跨黄波段阈值 26），WHEN `hull_band_changed` 信号发出，THEN S1 和 S5 的船体条颜色从绿色变为黄色，同时形状指示灯从 ✓ 变为 ⚡。
- **AC-09**：GIVEN 仓库余量从 900 变为 920，WHEN `storage_changed` 信号发出，THEN S1 HUD 的仓库余量文本更新为 "920/1000"，容量条宽度更新为 184px（200px 最大宽度），上述变更在下一帧的脏标记批量更新中完成。
- **AC-10**：GIVEN `departure_locked = true`，WHEN 玩家尝试 Use 任何站点或按 M 键，THEN 无任何面板打开，移动冻结。

### 过渡

- **AC-11**：GIVEN 玩家在航图 ROUTE_SELECTED 状态，WHEN 点击"确认出航"按钮，THEN 墨水从选中航线向外扩散 0.6s → 出发口封闭动画 → 锁定 1.2s → 航图关闭 → 航行过渡开始。锁定期间不可取消。
- **AC-12**：GIVEN 探索中玩家在撤离锚点确认撤离，WHEN S6b 撤离读条进行中，THEN 屏幕底部居中显示进度条+"撤离中……"。GIVEN 读条进行中被威胁打断，WHEN 威胁触发，THEN 进度条红色闪烁 0.5s×3 次+"撤离中断！"文本，读条关闭。

### 键盘导航

- **AC-13**：GIVEN 模态面板打开（S3/S6a/S7/S8/S9），WHEN 面板激活完成，THEN 焦点自动移到面板内第一个可交互元素。Tab 键在面板内循环焦点，不会穿透到下层 UI 或世界交互层。
- **AC-14**：GIVEN 修复面板 S8 打开且某材料不足（已提交 < 需求量），WHEN Tab 聚焦到确认提交按钮，THEN 按钮灰显（interactable=false），tooltip 显示"材料不足，无法提交"，Enter 键无响应。
- **AC-15**：GIVEN 摊位购买界面 S9 打开且玩家货币不足，WHEN 渲染商品列表，THEN 价格文本灰显+红色提示文字"货币不足"。购买按钮不可交互（interactable=false）。

### 无障碍

- **AC-16**：GIVEN S1 Hub HUD 渲染，WHEN 模块槽状态指示灯显示，THEN 每个指示灯同时使用颜色（绿/黄/红）和形状（✓/⚡/○）区分，不可仅依赖颜色。disabled 状态灰化+文本标签。
- **AC-17**：GIVEN 任何使用颜色编码的状态元素（船体波段、材料满足/不足、路线风险等级），WHEN 元素尺寸 < 24px，THEN 必须同时呈现颜色+形状+边缘特征三重编码。颜色不可作为唯一区分手段。

### 模态栈行为

- **AC-18**：GIVEN 模态面板 S6a（容量取舍）已打开，WHEN 战斗威胁触发（S7 打开），THEN S6a 状态保存到 `combat_override_stack`，S6a 透明度降至 20% 且 `process_mode=DISABLED`，S7 在独立 CanvasLayer（layer=100）渲染。GIVEN 玩家选择应急处理或硬扛，WHEN 战斗结算完成，THEN S6a 恢复至覆盖前的完整状态（滚动位置、选中索引、物品决策上下文不丢失）。
- **AC-19**：GIVEN 无模态面板打开，WHEN 多个非模态面板同时打开（S11 嗅辨 + S12 仓库），THEN 两个面板均可交互，后打开的面板视觉上覆盖在先打开的上方。WASD 移动仍可用。

### 桌面窗口恢复

- **AC-20**：GIVEN 玩家在 Hub 中 S1 HUD 显示正常、S12 仓库面板打开，WHEN 桌面窗口失焦/最小化后恢复（focus-in / resume notification），THEN S1 HUD 显示与恢复前一致，S12 仓库面板仍在打开状态、焦点位置不变，WASD 移动恢复。

## Open Questions

1. **M 键快捷打开航图的接受度**：从 Hub 任意位置按 M 键直接打开航图——跳过走到舱门的步行动画。这是便利性 vs 沉浸感的取舍。需要在 playtest 中验证。（Owner: game-designer, Target: Greybox Vertical Slice）

2. **Toast 堆叠方向**：新 Toast 显示在上方（后进先出视觉栈）还是下方（时间线向下增长）？当前默认：新 Toast 在最上方。（Owner: ux-designer, Target: UX spec 创建时确定）

3. **面板缓存池大小**：当前默认 2 实例（LRU）。MVP 有 5 个模态站点——缓存全部 5 个可以消除重复加载但增加 ~150KB 内存。需在 Greybox 阶段验证内存预算后确定。（Owner: ui-programmer, Target: Greybox Vertical Slice）

4. **墨水扩散 Shader 的桌面渲染兼容性**：ShaderMaterial 路线需要在 Godot 4.6.2 桌面 Compatibility / Forward+ 渲染器上验证。如果目标桌面渲染器的 `texture()` 采样或 uniform 更新出现性能/精度问题，回退方案是什么？（Owner: technical-artist, Target: Technical Prototype）

5. **命名模态的到达序列弹出时机**：当前指定"到达序列的最后 0.3s 弹出"。如果到达序列总时长可配置，这个相对时机是否需要调整为绝对时机（如"到达序列开始后 1.5s"）？（Owner: narrative-director + ux-designer, Target: UX spec 创建时确定）

6. **修复完成后"世界已变化"的持续 UI 标记**：当玩家完成一次修复（#13）返回 Hub 后，航图上受影响的路线/节点应有视觉标记表明"此路线因你的修复而改变"——不是一次性公告，而是持续存在的、可被反复翻阅的痕迹。这个标记应该以什么形式存在？航图上被修复连通的节点是否需要特殊图标？修复记录是否需要像航海日志条目一样可回溯？（Owner: ux-designer + game-designer, Target: UX spec 创建时确定）
