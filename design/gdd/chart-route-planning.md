# 航图与航线规划

> **Status**: Approved
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-02
> **Implements Pillar**: 规划先于冒险; 未知带来温和压力
> **System Index**: `design/gdd/systems-index.md`
> **CD Review**: PASS WITH NOTES (2026-05-02)
> **Design Review**: NEEDS REVISION → RESOLVED (2026-05-02 — 1 阻断项 CB-4 已修复，7 推荐修订已合并，QA 建议记录为 OQ-18 ~ OQ-22)
> **Review Log**: `design/gdd/reviews/chart-route-planning-review-log.md`

## Overview

航图与航线规划是《云海织航》的核心决策界面与航线选择系统。在数据层，它消费「内容数据与状态注册表」的静态航线定义——起终点地点、距离带、风险标签——以及「玩家知识与情报」的动态知识状态——已知/未知、风险揭示、来源标注、能力解锁——将这两层信息统一为一张可读的地图语言。在体验层，它是玩家出航前最关键的决策时刻：玩家在航图上阅读每条可选航线的风险标签和来源、看到哪些路线因世界修复而重新稳定、哪些路线因缺少对应能力而暂时阻塞、哪些区域仍完全未知，然后基于自己的准备和判断选择一条航线并提交出航。

航图既是信息呈现系统（航线可见性、风险可视化、阻塞原因、航线筛选），也是决策提交系统（航线选择、出航确认、继续点触发）。它不拥有航线发现状态（属于「玩家知识与情报」），不拥有遭遇生成或风险后果（属于「航行与路线风险」），不绘制最终 UI 控件（属于「UI / HUD / 航图界面」）——但它是连接这三个系统的枢纽：知识系统告诉它"能看到什么"，航线系统告诉它"选完后会发生什么"，UI 系统告诉它"如何被看见和操作"。没有航图，玩家即使拥有情报和能力，也无法将其转化为一次有意义的出航决策。

**明确不在本系统范围内**：航图不展示 NPC 飞船的飞行轨迹或航线交通活跃度（属于后续阶段扩展，依赖「空港 / 村镇状态与集市交易」的贸易流量数据）；不处理玩家委托 NPC 船运货的物流功能（属于「空港 / 村镇状态与集市交易」的市场服务扩展，触及资源、航线可用性和世界状态）。

## Player Fantasy

航图与航线规划服务的是一个双重幻想：**亲手把一张破碎、空白、不可读的航图变成一张有记忆、有证据、可信赖的航路网络**。

### 直接层：亲手画图的掌控感

玩家每次打开航图，不是在看系统给出的列表，而是在使用一件自己亲手参与打造的工具。航图上的每一条实线、每一个风险标记、每一段个人注记，都有来源——来自消耗的情报物品、来自侦察伙伴的报告、来自旧航海日志的坐标、来自玩家自己亲身验证过的目击。航图从不是"自动解锁的"，而是玩家一步一步积累出来的。玩家在出航前用手指沿着一条航线划过——它在一开始可能只是港口杂货商的一句流言，后来被侦察伙伴确认了安全锚地，终于在某次航行后变成了深金实线——这条路是你亲手让它从"传闻"变成"航线"的。

这个幻想的锚点时刻是：**玩家在航图上看到一条自己标注、验证、稳定的航线，意识到它不是系统给的——是你在酒馆里听了传闻、在旧日志里找到坐标、在夜航中亲眼确认了灯塔的节奏后，把它画上去的。**

### 间接层：世界变得可读的安心感

航图不只在记录位置——它在记录意义。一张逐渐被填空的航图让玩家感受到的不是"内容解锁进度"，而是世界正在因你的照料变得可读、可预测、可信任。曾经全是迷雾的区域，现在你能看到安全线和风险带；曾经必须冒险闯入的未知水域，现在你清楚地知道哪里可以补给、哪里需要准备反劫掠装备、哪里只是风景平静的航线。这种从"到处是？"到"我知道这里有什么"的转变，是 Pillar 4「未知带来温和压力」的正面兑现——压力不来自惩罚或倒计时，而是来自情报不足；安宁不来自系统放水，而是来自你比别人更了解这片天。

### 参考感受

参考老航海图的制作传统——不是冷冰冰的导航仪器，而是被反复折叠、标注、修补的手绘海图，带着笔迹、涂改、注记和个人记忆。航图上的信息不应该是完美排版的字体，而应该有"人做过标记"的温度。航图上的一处潦草标注可能代表一次冒险中的仓促判断，一处被划掉的风险标记可能代表"港口流言说这里有礁石，但我亲自飞过——没有。"

同时服务 Pillar 1「规划先于冒险」：每次出航前的航线选择建立在航图信息的深度和可信度之上，准备本身是有回报的技能；以及 Pillar 2「世界会回应照料」：航图本身就是世界被照料过的可见证明——每一条因设施修复而重新稳定的航线、每一个重新点亮的灯塔坐标，都在航图上留下了永久的视觉反馈。

## Detailed Design

### Core Rules

**进入航图**

1. 玩家在飞艇海图室（`home-space.map-room`）与海图桌交互锚点（`home-anchor.chart-table`）交互，触发航图打开。
2. 系统检查 `routes`、`world`、`intel`、`threats` 四个内容域是否均为 `COMPLETE`。若任一域非 `COMPLETE`，航图进入 `ERROR` 状态，显示安全错误提示，不渲染任何航线（来自 #1 内容注册表 GDD 的硬性合约）。
3. 域检查通过后，航图加载：对注册表中所有 `kind=route` 的条目执行批量查询，然后为每条航线调用 `query_route_knowledge(route_id)`。知识状态为 `unknown` 的航线不渲染。
4. 场景以约 0.8s 羊皮纸纹理渐变从横版飞艇过渡到俯视航图视图——这是一个心理切换信号："你现在正在规划，不再是在行走。"

**航线渲染规则**

5. 每条已渲染航线按知识状态套用以下视觉编码（由 #6 玩家知识与情报 GDD 定义）：

| 知识状态 | 航线样式 | 地点节点 | 风险标签 |
|----------|---------|---------|---------|
| `rumored` | 2px 虚线，透明度 60% | 虚线圆 | 部分隐藏，隐藏标签显示为闪烁 `?` |
| `identified` | 实线轮廓 | 空心圆 | 完整显示 |
| `verified` | 实线 + 暖金色发光边缘 | 实心圆 | 完整显示 + 可添加个人标注（手写体风格） |

6. 航线颜色编码：绿色 = 安全/已验证，黄色 = 部分已知，红色 = 高风险且信息不全。
7. 风险标签以图标行形式显示在航线中点下方，来源标注以小号手写体文字显示在风险标签下方。悬停来源文字时弹出 tooltip 展示来源名称和置信度层级。
8. MVP 使用固定视图——2 条航线从同一出发港辐射出去，整个航线网络适配单屏，无需平移/缩放。航图占屏幕约 70%，侧边详情面板占 30%。

**航线可选择性评估**

9. 对每条已渲染航线，航图判断其可选择性：

| 条件 | 不满足时的行为 |
|------|---------------|
| `query_route_accessibility(route_id).traversable == true` | 航线置灰 + tooltip 显示阻塞原因（如"需要灯塔信号解读能力"） |
| `route.origin_location_id` 等于玩家当前停靠地点 | 航线可见但不可选 + tooltip 显示"不在当前港口" |
| 航图状态为 `BROWSING` 或 `ROUTE_SELECTED` | 所有航线不可交互（出航锁定中） |

10. 阻塞航线不是"失败状态"——它是一条承诺：玩家看到它存在，知道可以通过解锁能力或到达对应港口来使其可选。

**信息筛选**

11. MVP 提供单一切换：**"显示/隐藏仅传闻航线"**（默认：显示）。关闭后，仅 `rumored` 状态的航线被隐藏；`identified` 和 `verified` 航线保持可见。该切换状态是航图本地 UI 状态，属于 `progress.routes` 快照包的一部分。

**航线浏览与选择**

12. 浏览：玩家用鼠标悬停或用 Tab/方向键在航线间移动焦点。悬停航线时：线条微微变亮，侧边面板显示航线名称、风险标签摘要、来源标注、一句话概述。这是**浏览**，不是选择。
13. 选择：玩家点击/Enter 一条 `selectable` 航线后：航线闪烁一次暖金色脉冲（0.3s），侧边面板展开显示完整详情（所有风险标签、所有来源、已知风险列表 vs 未知风险计数），面板底部出现"确认出航"按钮。其余航线变暗至 40% 透明度。
14. 取消选择：按 Esc 或点击航图空白区域，回到 `BROWSING` 状态。

**出航确认**

15. 出航采用两步确认（支撑 Pillar 1：规划是有意义的决策）：
    - **第一步**：点击"确认出航"，弹出最终摘要浮层——航线名称、风险摘要、预估距离带、"出航"/"取消"选择。
    - **第二步**：点击"出航"确认。
16. 确认后进入不可逆承诺（以下步骤按顺序执行）：
    a. 航图状态切换为 `DEPARTURE_CONFIRMED`。
    b. 航图调用 `snapshot_package_validity()` 校验即将写入的快照包（Formula 4）。
    c. **校验通过时**：
       i. 航图发出 `route_committed(route_id, destination_id, hazard_tags)` 事件（单次发射）。
       ii. 保存系统创建 `progress.routes` 快照。
       iii. 出航锁定开始（`departure_lock_duration`，默认 2.0s，引用注册表常量 `base_lock_duration`）。
    d. **校验失败时**：
       i. 不发射 `route_committed` 事件。
       ii. 不创建快照。
       iii. 航图回退至 `ERROR` 状态，展示失败原因（含 `violations` 列表中的具体违规项）。
       iv. 玩家可从 ERROR 状态重试（RETRY → LOADING）。
    e. 锁定期间所有航图交互禁用。
    f. 1.5s 墨迹扩散动画沿选中航线从起点描摹至目的地——强化"这段旅程正在被写入你的航图记忆"。
    g. 锁定结束后场景过渡至航行阶段，控制权移交航行与路线风险系统（#10）。

**MVP 两条航线配置**

| 航线 ID | 初始知识 | 静态风险标签 | 可通行 | 来源 | 视觉 |
|--------|---------|------------|--------|------|------|
| `route.sky-reef-arc-01` | `identified` | `safe` | 是 | "空港基础航图" | 绿色实线 + 空心圆端点 |
| `route.storm-cut-01` | `rumored` | `storm`, `low-visibility`（`low-visibility` 隐藏 → 闪烁 `?`） | 是 | "港口传闻"（置信度：不确定） | 黄色虚线 + 虚线圆端点 |

两条航线均以 `location.glass-harbor` 为起点。

**未知空间**

17. 没有已知航线的区域显示羊皮纸/底图纹理，边缘为柔雾渐变。不是黑色虚空，而是"等待被绘制的空白羊皮纸"——空白不是缺失，是可能性。

### States and Transitions

**航图层级状态机**

| 状态 | 含义 | 进入条件 | 有效转出 |
|------|------|---------|---------|
| `LOADING` | 验证内容域就绪状态，查询注册表和情报系统 | 海图桌交互锚点被激活 | `BROWSING`、`ERROR` |
| `BROWSING` | 航图完全渲染，玩家可查看、筛选、悬停阅读详情 | 所有必需域 `COMPLETE` 检查通过，航线数据已加载 | `ROUTE_SELECTED`、`ERROR` |
| `ROUTE_SELECTED` | 已选中一条可航线，高亮，详情面板展开，确认按钮可用 | 玩家在 `BROWSING` 状态下选择一条 `selectable` 航线 | `BROWSING`（取消选择）、`DEPARTURE_CONFIRMED` |
| `DEPARTURE_CONFIRMED` | 已承诺出航，锁定动画播放中，所有交互禁用 | 玩家在 `ROUTE_SELECTED` 状态下通过两步确认提交出航 | 无（航图关闭，控制权移交至航行系统） |
| `ERROR` | 航图无法渲染——内容域非 `COMPLETE` 或查询失败 | 任意内容域非 `COMPLETE`，或注册表查询返回 `FAILED` | `LOADING`（重试） |

**航线选择子状态机（每条已渲染航线独立持有）**

| 状态 | 含义 | 有效转出 |
|------|------|---------|
| `BROWSABLE` | 航线以默认视觉样式渲染，无高亮 | `SELECTED` |
| `SELECTED` | 航线高亮，详情面板填充，确认按钮可用 | `BROWSABLE`（取消选择/选择另一条） |
| `UNAVAILABLE` | 航线可见但置灰，不可选中——tooltip 显示条件名称及失败原因 | `BROWSABLE`（条件变化，如能力解锁或到达对应港口） |
| `LOCKED` | 出航已确认，所有航线均不可交互 | 无（航图关闭） |

**无效转换**：
- `DEPARTURE_CONFIRMED → ROUTE_SELECTED`：不可逆（已承诺出航）
- `LOCKED → SELECTED`：锁定期间所有交互永久禁用
- `ERROR → BROWSING`：必须通过 `LOADING` 重试
- `UNAVAILABLE → SELECTED`：必须先变为 `BROWSABLE`

**未列出的转换**：任何 (state, trigger) 组合未出现在转换表中时，默认返回 `{allowed: false}`——即被拒绝。此规则提供隐式穷举覆盖。

**缺失转换（已识别但属于合法扩展）**：
- `BROWSING + FAIL → ERROR`：浏览期间检测到关键数据损坏（如缓存与注册表不一致）时可触发。
- `BROWSING + RETRY → LOADING`：EC-2 中描述的"玩家可手动重试"路径——非阻断通知中包含重试按钮。

**RETRY 频率限制**：连续 RETRY 之间必须间隔至少 `retry_cooldown`（默认 2.0s，引用注册表常量）。冷却期间重试按钮禁用。此限制防止 `ERROR → LOADING → ERROR` 的紧循环。

**保存/恢复边界**：
- 出航确认时触发保存检查点。快照包 `progress.routes` 包含 `last_committed_route_id`、`departure_state`、`active_filter`、`last_departure_timestamp`。
- 恢复时若 `departure_state = DEPARTURE_CONFIRMED`，航图跳过渲染，直接加载出航锁定序列后移交航行系统——玩家不会重新看到航图，他们在出航过程中恢复。

### Interactions with Other Systems

**上游系统（航图从它们读取）**

| 系统 | 航图请求 | 接口 | 用途 |
|------|---------|------|------|
| #1 内容数据与状态注册表 | 所有 `kind=route` 条目 | `list_by_kind("route")` | 航线静态数据（起终点、距离带、风险标签） |
| #1 内容数据与状态注册表 | 所有 `kind=location` 条目 | `list_by_kind("location")` | 地点节点（港口类型、服务标签、地方身份） |
| #1 内容数据与状态注册表 | 内容域就绪状态 | `domain_state` 查询 | 加载门控：四大域必须全部 `COMPLETE` |
| #6 玩家知识与情报 | 航线知识状态 | `query_route_knowledge(route_id)` | 视觉编码 + 可见/隐藏风险标签 + 来源标注 |
| #6 玩家知识与情报 | 航线可通行性 | `query_route_accessibility(route_id)` | `traversable` + `block_reason`（可引用能力 ID） |
| #6 玩家知识与情报 | 地点知识状态 | `query_location_discovery(location_id)` | 节点视觉编码 + 个人标注 |
| #7 飞艇家园 Hub | 当前停靠地点 | `get_current_docked_location()` | 可选择性条件：只有以当前位置为起点的航线可选 |

**跨系统渲染规则**：当一条航线的知识状态 >= `rumored`，但其端点地点为 `unknown` 时，航图必须以代理节点渲染该端点以完成航线可视化。代理节点使用与航线知识状态匹配的最低视觉层级——例如航线为 `identified` 但目的地为 `unknown`，目的地以空心圆渲染并标注来源"由航线推导"。

**下游系统（航图向它们写入/发出信号）**

| 系统 | 航图提供 | 时机 | 内容 |
|------|---------|------|------|
| #3 本地存档与世界状态持久化 | `progress.routes` 快照包 | 出航确认时 | `last_committed_route_id`、`departure_state`、`active_filter`、`last_departure_timestamp`、`hide_rumored` |
| #10 航行与路线风险 | `route_committed` 事件 | 出航确认后 | `route_id`、`destination_id`、`hazard_tags` |
| #16 UI / HUD / 航图界面 | 航图状态查询接口 | 持续 | `get_chart_state()`、`get_visible_routes()`、`get_selected_route()`、`get_filter_state()` |

**合约边界**：
- 航图**只读**注册表和情报系统，永远不写入知识状态或修改静态定义。
- 航图**只发出事件**给航行系统，事件发出后航图关闭，不再参与航行过程。
- 航图**只提供数据**给 UI 系统：状态枚举、可见航线列表、已选航线 ID、筛选器状态。UI 系统拥有所有视觉渲染、控件位置、输入处理和动画。

## Formulas

### Formula 1 — route_visibility

**航线可见性判定**：决定一条已注册航线是否应在航图上渲染。

```
route_visibility(route_id, hide_rumored) → boolean
```

| 变量 | 类型 | 来源 | 含义 |
|------|------|------|------|
| `knowledge_state` | `enum {unknown, rumored, identified, verified}` | `query_route_knowledge(route_id).state` | 航线知识状态 |
| `hide_rumored` | `boolean` | 航图本地 UI 筛选器状态 | 玩家是否关闭了"显示仅传闻航线" |

**判定逻辑**：

```
1. IF knowledge_state == "unknown" → return false
2. IF hide_rumored AND knowledge_state == "rumored" → return false
3. ELSE → return true
```

**输出**：`true` = 航线渲染；`false` = 航线不渲染。

**设计约束**：此公式只判定可见/不可见——不参与样式、可选择性、排序等其他决策。`unknown` 航线永远不可见，这是硬性边界。

### Formula 2 — route_selectability

**航线可选择性评估**：对每条已渲染航线，评估其当前交互状态。

```
route_selectability(route_id, chart_state, docked_location, hide_rumored, selected_route_id) → enum {hidden, browsable, selected, unavailable, locked}
```

| 变量 | 类型 | 来源 | 含义 |
|------|------|------|------|
| `route_id` | `string` | 注册表 `kind=route` 条目 | 航线 ID |
| `chart_state` | `enum` | 航图层级状态机 | `BROWSING` / `ROUTE_SELECTED` / `DEPARTURE_CONFIRMED` 等 |
| `docked_location` | `string` | `airship_hub.get_current_docked_location()` | 玩家当前停靠地点 ID |
| `hide_rumored` | `boolean` | 航图本地状态 | 筛选器状态 |
| `selected_route_id` | `string \| null` | 航图状态 | 当前已选中的航线 ID（无选中则为 null） |
| `traversable` | `boolean` | `query_route_accessibility(route_id).traversable` | 航线是否可通行 |
| `origin_id` | `string` | 注册表 `route.origin_location_id` | 航线起点地点 ID |

**判定逻辑（短路求值）**：

```
1. IF route_visibility(route_id, hide_rumored) == false → return "hidden"
2. IF chart_state == "DEPARTURE_CONFIRMED" → return "locked"
3. IF traversable == false → return "unavailable"
4. IF origin_id != docked_location → return "unavailable"
5. IF route_id == selected_route_id → return "selected"
6. IF chart_state == "ROUTE_SELECTED" → return "browsable"
7. ELSE → return "browsable"
```

**输出示例**：

| 条件 | 输出 |
|------|------|
| 航线 `rumored`，筛选器关闭 → `hide_rumored=true` | `hidden` |
| 出航已确认，锁定中 | `locked` |
| 航线需要灯塔信号解读能力，玩家未解锁 | `unavailable` |
| 航线起点不是当前停靠港口 | `unavailable` |
| 玩家点击选中了该航线 | `selected` |
| 正常可见、可选但未被选中的航线 | `browsable` |

### Formula 3 — chart_state_transition

**航图层级状态转换函数**：约束所有合法状态变化。

```
chart_state_transition(current_state, trigger, trigger_payload) → {new_state, allowed}
```

| 变量 | 类型 | 含义 |
|------|------|------|
| `current_state` | `LOADING \| BROWSING \| ROUTE_SELECTED \| DEPARTURE_CONFIRMED \| ERROR` | 当前航图状态 |
| `trigger` | `COMPLETE \| SELECT \| DESELECT \| CONFIRM \| FAIL \| RETRY` | 触发事件类型 |
| `trigger_payload` | `dict` | 触发器附带的上下文数据 |

**转换表**：

| 当前状态 | 触发 | 目标状态 | 条件 |
|----------|------|---------|------|
| `LOADING` | `COMPLETE` | `BROWSING` | 四大内容域均为 COMPLETE，航线数据加载成功 |
| `LOADING` | `FAIL` | `ERROR` | 任一内容域非 COMPLETE，或注册表查询返回 FAILED |
| `BROWSING` | `SELECT` | `ROUTE_SELECTED` | `trigger_payload.route_id` 对应航线的 `route_selectability` 返回 `browsable` |
| `ROUTE_SELECTED` | `DESELECT` | `BROWSING` | Esc 或点击空白区域 |
| `ROUTE_SELECTED` | `CONFIRM` | `DEPARTURE_CONFIRMED` | 两步确认完成，`trigger_payload.route_id` 有效 |
| `ERROR` | `RETRY` | `LOADING` | 玩家手动重试 |

**不可逆约束**：

```
IF current_state == "DEPARTURE_CONFIRMED"
  → return {new_state: current_state, allowed: false}  // 任何触发均无效
```

**设计意图**：`DEPARTURE_CONFIRMED` 是终端状态，一旦进入即出航锁定开始，航图将在动画结束后关闭并移交控制权给航行系统。不存在从 `DEPARTURE_CONFIRMED` 返回任何航图浏览状态的路径。玩家通过关闭后重新与海图桌交互来开启新的航图会话。

### Formula 4 — snapshot_package_validity

**快照包有效性校验**：出航确认时验证 `progress.routes` 快照包的完整性和一致性。

```
snapshot_package_validity(pkg) → {valid: boolean, violations: string[]}
```

| 变量 | 类型 | 约束 |
|------|------|------|
| `pkg.domain_id` | `string` | 必须等于 `"progress.routes"` |
| `pkg.fields` | `dict` | 必须包含 `last_committed_route_id`, `departure_state`, `active_filter`, `last_departure_timestamp`，可选含 `hide_rumored` |
| `pkg.fields.departure_state` | `string` | 必须为 `"DEPARTURE_CONFIRMED"`（航图只在出航确认时写入快照） |
| `pkg.fields.last_departure_timestamp` | `float` | 必须为有限值（非 NaN，非 ±Inf），且不得为未来时间戳 |
| `current_time` | `float` | 存档系统提供 | 当前游戏时间戳，用于方向性检查 |
| `timestamp_tolerance` | `float` | 注册表常量 `base_timestamp_tolerance`（默认 300s） | 允许轻微时钟偏差 |
| `route_registry` | `string[]` | `list_by_kind("route")` 的 ID 列表 | 校验 route_id 存在性 |

**判定逻辑**：

```
1. violations = []
2. IF pkg == null OR typeof(pkg.fields) != "dict" → violations.append("malformed snapshot package"); return {valid: false, violations}
3. IF NOT is_finite(current_time) → violations.append("non-finite current_time")
4. IF pkg.domain_id != "progress.routes" → violations.append("wrong domain_id")
5. required = ["last_committed_route_id", "departure_state", "active_filter", "last_departure_timestamp"]
6. FOR EACH field IN required:
     IF field NOT IN pkg.fields → violations.append("missing field: " + field)
7. IF pkg.fields.departure_state != "DEPARTURE_CONFIRMED" → violations.append("invalid departure_state")
8. IF NOT is_finite(pkg.fields.last_departure_timestamp) → violations.append("non-finite timestamp")
9. IF pkg.fields.last_departure_timestamp <= 0 → violations.append("timestamp is epoch or uninitialized")
10. IF pkg.fields.last_departure_timestamp > current_time + timestamp_tolerance → violations.append("timestamp in future")
11. IF pkg.fields.last_committed_route_id NOT IN route_registry → violations.append("route_id not found in registry: " + pkg.fields.last_committed_route_id)
12. IF size(violations) > 0 → return {valid: false, violations}
13. ELSE → return {valid: true, violations: []}
```

**设计意图**：此公式实现了保存系统 GDD (#3) 的快照包合约。当 `valid=false` 时，保存系统必须拒绝写入此快照包。航图必须在收到 `snapshot_rejected` 信号后回退到 `ERROR` 状态。

### Formula 5 — route_display_order

**航线展示排序**：在侧边详情面板或航线列表中确定航线展示优先级。排序确保知识质量始终比物理距离更有权重。

```
route_display_order(route_id) → integer ∈ [101, 303]
```

| 变量 | 类型 | 来源 | 含义 |
|------|------|------|------|
| `knowledge_state` | `enum` | `query_route_knowledge(route_id).state` | 航线知识状态 |
| `distance_band` | `enum {short, medium, long}` | 注册表 `route.distance_band` | 航线距离带 |

**排序计算**：

```
rank_by_knowledge = CASE knowledge_state:
  "verified"   → 1
  "identified" → 2
  "rumored"    → 3

rank_by_distance = CASE distance_band:
  "short"  → 1
  "medium" → 2
  "long"   → 3

display_order = rank_by_knowledge × 100 + rank_by_distance
```

**输出范围**（值越小排越前）：

| 知识 × 距离 | short (1) | medium (2) | long (3) |
|-------------|-----------|------------|----------|
| verified (1) | 101 | 102 | 103 |
| identified (2) | 201 | 202 | 203 |
| rumored (3) | 301 | 302 | 303 |

**设计意图**：知识置信度决定排序层级——玩家投入时间验证过的航线排在最前，这是"信任的回报"。百位置为置信度层：`verified`（100+）> `identified`（200+）> `rumored`（300+）。同级内，短距离优先于长距离（个位：short=1 < medium=2 < long=3），方便玩家找到快速往返航线。

**MVP 两条航线验证**：

- `route.sky-reef-arc-01`：`identified` + `short` → 2×100+1 = 201
- `route.storm-cut-01`：`rumored` + `medium` → 3×100+2 = 302
- 排序结果：`sky-reef-arc-01`(201) < `storm-cut-01`(302) → `sky-reef-arc-01` 排在前面

## Edge Cases

### Loading / Initialization Edge Cases

**EC-1: 内容域部分未就绪**

触发场景：四大内容域中 `routes`、`world`、`intel` 为 `COMPLETE`，但 `threats` 为 `FAILED`（非 LOADING）。玩家与海图桌交互锚点交互。

预期行为：`chart_state_transition(LOADING, FAIL)` 立即触发 `LOADING → ERROR`。ERROR 状态必须明确展示失败的具体域名称和当前状态（如 "threats 域状态：FAILED"），而非泛化"加载失败"。重试按钮触发 `ERROR → LOADING`（RETRY）。

涉及的规则/公式/状态：`chart_state_transition`（LOADING + FAIL → ERROR，ERROR + RETRY → LOADING），加载门控规则（规则 #2），无效转换约束（ERROR → BROWSING 被禁止）。

---

**EC-2: 部分航线情报查询失败**

触发场景：LOADING 阶段对所有 `kind=route` 条目批量调用 `query_route_knowledge()`。5 条航线中 3 条查询成功，2 条返回异常/超时（非全系统故障——全系统故障应为 ERROR）。

预期行为：查询失败的航线被视为 `knowledge_state = unknown`，`route_visibility` 对其返回 `false`，不渲染。航图以 3 条有效航线进入 BROWSING 状态。系统记录内部警告计数器（2/5 失败）。UI 展示非阻断通知："部分航线情报读取失败 (2/5)——未知航线未显示。重试？" 玩家可留在当前航图或手动触发重试（RETRY → LOADING）。航图不因部分数据缺失而整体崩溃。

涉及的规则/公式/状态：`route_visibility`（unknown → false），`chart_state_transition`（LOADING + COMPLETE → BROWSING 仍合法，域状态检查通过），优雅降级。

---

### Player Action Edge Cases

**EC-3: 出航锁定期间快速连点**

触发场景：玩家点击"出航"确认，`DEPARTURE_CONFIRMED` 状态已进入，2.0s 墨迹扩散动画播放中。玩家在锁定期间疯狂点击航图区域和确认按钮。

预期行为：第一个确认事件触发 `chart_state_transition(ROUTE_SELECTED, CONFIRM) → DEPARTURE_CONFIRMED`。此后所有输入事件在 `DEPARTURE_CONFIRMED` 状态下被 `route_selectability` 分支 2 拦截——所有航线返回 `locked`。`chart_state_transition` 的终端状态守卫确保不会产生第二个状态转换。不存在"事件排队"——锁定是不可逆的状态约束，不是可堆叠的定时器。

涉及的规则/公式/状态：`route_selectability` 分支 2，`chart_state_transition` 不可逆约束，航线选择子状态机 LOCKED 状态的"无有效转出"约束。

---

**EC-4: 同一帧内双击确认**

触发场景：玩家输入设备在同一帧内发送两次确认事件（硬件抖动、辅助工具、或帧时序边界情况）。第一个事件处理完毕后，第二个事件到达时航图状态已是 `DEPARTURE_CONFIRMED`。

预期行为：`chart_state_transition` 处理第一个 CONFIRM 触发 → `DEPARTURE_CONFIRMED`。第二个 CONFIRM 触发命中终端状态守卫，返回 `{new_state: DEPARTURE_CONFIRMED, allowed: false}`。不得发出两个 `route_committed` 事件，不得创建两份 `progress.routes` 快照。事件唯一性由状态机保证。

涉及的规则/公式/状态：`chart_state_transition` 终端守卫，`route_committed` 事件发射唯一性。

---

### Data Inconsistency Edge Cases

**EC-5: 已选航线的知识状态被情报系统撤销**

触发场景：玩家在 `ROUTE_SELECTED` 状态下已选中航线 A（`knowledge_state = identified`）。外部情报系统事件（情报物品过期、来源被证伪）将航线 A 的知识状态更新为 `unknown`。航图检测到此变化。

预期行为：`route_visibility(航线A)` 现在返回 `false`（unknown）。航图必须立即强制取消选择：`ROUTE_SELECTED → BROWSING`（系统生成的 DESELECT 触发）。航线 A 从航图上消失，侧边面板清空。若不存在其他可选航线，BROWSING 状态显示空航图消息。通知："航线 [A 名称] 的情报已失效——该航线的知识来源不再可信。"

涉及的规则/公式/状态：`route_visibility` 分支 1（unknown → false），`chart_state_transition`（DESELECT 触发，来源为 system），`route_selectability` 短路求值——hidden 在第一分支即返回。

---

**EC-6: 航图打开期间停靠地点变更**

触发场景：玩家在 BROWSING 状态下查看从港口 A 出发的航线。外部事件将当前停靠地点变更为港口 B。航图检测到变更。

预期行为：`route_selectability` 以新的 `docked_location = 港口B` 重新评估所有航线。起点为港口 A 的航线命中分支 4 → `UNAVAILABLE`。起点为港口 B 且之前为 UNAVAILABLE 的航线可能变为 BROWSABLE。航图保持 BROWSING 状态。若此前有已选航线且其新的可选择性为 UNAVAILABLE，则强制取消选择（同 EC-5）。非阻断通知："当前停靠地已变更为 [港口B 名称]——航线选择已更新。"

涉及的规则/公式/状态：`route_selectability` 分支 4，UNAVAILABLE ↔ BROWSABLE 合法转换。

---

**EC-7: 航图打开期间能力解锁使阻塞航线变为可选**

触发场景：航图处于 BROWSING 状态。航线 X 因 `query_route_accessibility(航线X).traversable == false`（需要"深空导航"能力）而处于 UNAVAILABLE。玩家在航图打开期间获得能力解锁。`traversable` 现在返回 `true`。

预期行为：下次 `route_selectability` 评估时，航线 X 从 UNAVAILABLE 变为 BROWSABLE。视觉变化：置灰恢复为正常颜色，tooltip 移除，可点击。航线列表/侧边面板实时更新。无需状态转换，无需通知——正向变化通过视觉变化被玩家发现（航线从灰色恢复颜色本身就是最好的通知）。变化必须足够明显：颜色过渡动画 0.3s，tooltip 消失。

涉及的规则/公式/状态：`route_selectability` 分支 3（traversable 不再阻止），UNAVAILABLE → BROWSABLE。

---

**EC-8: 航图打开期间航线从注册表删除**

触发场景：航图处于 BROWSING 状态，缓存了 5 条航线。内容热重载或管理操作从注册表中删除了 `kind=route` 条目 R_003。航图的缓存航线列表现已过时。

预期行为：在下次刷新周期中，航图校验其缓存航线列表与注册表的一致性。缓存中存在但注册表中不存在的航线必须被移除。若被删除航线为当前 SELECTED，强制取消选择（`ROUTE_SELECTED → BROWSING`）。若被删除航线是唯一航线，航图显示空状态。记录警告日志。不应崩溃——航线缓存始终是从注册表派生的软状态。

涉及的规则/公式/状态：`chart_state_transition`（DESELECT 如有必要），BROWSING 状态下的航线集完整性校验，航图"只读注册表"合约。

---

### Save/Load Edge Cases

**EC-9: 加载快照引用的 route_id 在注册表中已不存在**

触发场景：存档快照 `progress.routes` 中 `departure_state = DEPARTURE_CONFIRMED`，`last_committed_route_id = "R_007"`。一次内容更新将航线 R_007 从注册表中移除。玩家加载此存档。

预期行为：`snapshot_package_validity` 新增校验项——`last_committed_route_id` 必须在注册表 `list_by_kind("route")` 中存在对应条目。若不存在，violation："route_id not found in registry: R_007"。`valid=false`。存档系统（#3）收到拒绝信号，回退到上一个有效快照或干净状态。航图系统负责在写入快照时验证路由存在性；存档系统负责快照的原子写入和恢复。

涉及的规则/公式/状态：`snapshot_package_validity`（扩展校验项——分支 8），保存/恢复边界规则。

---

**EC-10: 快照时间戳在未来**

触发场景：玩家在时间戳 T=1000 时存档。系统时钟因 NTP 同步、时区变更或手动调整被向后拨动一小时（T→T-3600）。玩家加载存档时 `last_departure_timestamp = 1000` 但当前时间为 -2600。

预期行为：`snapshot_package_validity` 新增校验项——`last_departure_timestamp <= current_time + timestamp_tolerance`，其中 `timestamp_tolerance` 为小值（默认 300s，允许轻微时钟偏差）。若时间戳超出，violation："timestamp in future (saved: 1000, current: -2600)"。`valid=false`。航图进入 ERROR 状态。存档系统不得以未来时间戳的快照自动进入 DEPARTURE_CONFIRMED 状态。

涉及的规则/公式/状态：`snapshot_package_validity`（扩展校验项——分支 7），保存/恢复边界规则。

---

**EC-11: 快照部分写入损坏**

触发场景：存档系统开始写入 `progress.routes` 快照。在写入 `domain_id` 和 `departure_state` 后、写入 `last_departure_timestamp` 前发生断电/崩溃。磁盘上的快照文件被截断——JSON 不完整或字段缺失。

预期行为：下次加载时 `snapshot_package_validity` 检测到缺失必需字段 `last_departure_timestamp` → violation。`valid=false`。存档系统（#3）接收拒绝信号，回退到上一个有效快照或初始状态。航图系统不尝试从无效快照渲染。若损坏快照是仅有的快照，下次海图桌交互从干净的 LOADING 状态开始。

涉及的规则/公式/状态：`snapshot_package_validity`（缺失字段检查——分支 4），存档系统回退合约（#3 GDD），`chart_state_transition`（从干净状态 LOADING 开始）。

---

### Boundary / Empty Edge Cases

**EC-12: 首次打开——所有航线未知**

触发场景：玩家首次与海图桌交互。尚未收集任何情报。注册表中所有航线的 `knowledge_state = unknown`（默认）。`hide_rumored = false`（默认）。

预期行为：`route_visibility` 对所有航线返回 `false`。`get_visible_routes()` 返回空数组。加载门控通过（域均为 COMPLETE，数据加载成功），航图进入 BROWSING 状态但无可渲染航线。航图渲染：羊皮纸背景 + 边缘雾渐变 + 居中上下文消息："航图上尚无已知航线。在世界中收集情报以揭示航线。" 若筛选器切换为"隐藏传闻航线"再切回，无可见变化（所有航线均为 unknown，非 rumored）。不得崩溃、不得显示纯黑屏、不得进入 ERROR 状态——空航图是合法状态，不是错误。

涉及的规则/公式/状态：`route_visibility`（所有航线 → false），`chart_state_transition`（LOADING + COMPLETE → BROWSING），`get_visible_routes()` 返回空数组。

---

**EC-13: 停靠在无出发航线的港口**

触发场景：玩家已知航线 A→B、C→D、E→F（全部 verified）。当前停靠在港口 X。注册表中没有任何以港口 X 为 `origin_location_id` 的航线。玩家打开航图。

预期行为：所有已知航线通过 `route_visibility`（verified → true），但全部命中 `route_selectability` 分支 4（`origin_id != docked_location`）→ `UNAVAILABLE`。所有航线以置灰样式渲染，tooltip 显示："不在当前港口——需要从 [起点名称] 出发。" 航图处于 BROWSING 状态，零条 BROWSABLE 航线。上下文消息："当前港口 [X 名称] 无可用出发航线。前往其他港口以选择航线。" 玩家仍可浏览航线详情和规划——只是无法选择。这不是 ERROR 状态。

涉及的规则/公式/状态：`route_selectability` 分支 4（全部命中），BROWSING 状态下零 BROWSABLE 航线的渲染。

---

**EC-14: 所有航线均为传闻且筛选器关闭传闻显示**

触发场景：玩家有 3 条航线，全部 `knowledge_state = rumored`。玩家切换 `hide_rumored = true`。航图处于 BROWSING。

预期行为：`route_visibility` 对所有 3 条航线返回 `false`（分支 2：`hide_rumored AND rumored → false`）。航图视觉过渡到空状态。消息须与此场景匹配（区别于 EC-12 的"从未有过航线"）："所有航线均为传闻级别——关闭'隐藏传闻航线'以查看。" 将 `hide_rumored` 切回 `false` 后恢复 3 条传闻航线的渲染。此边界情况验证了筛选器状态与全数据集交互时的正确行为——不让航图卡在"全隐藏且无法恢复"的状态。

涉及的规则/公式/状态：`route_visibility` 分支 2，`hide_rumored` 筛选器状态切换。

---

### Cross-System Contract Edge Cases

**EC-15: 情报系统返回 traversable=true 但 knowledge_state=unknown（数据完整性违规）**

触发场景：`query_route_accessibility(航线X).traversable` 返回 `true`。但 `query_route_knowledge(航线X).state` 返回 `unknown`。这是上游数据的逻辑矛盾——玩家不可能"可以通行"一条他们根本不知道存在的航线。

预期行为：航图系统必须在加载或评估阶段检测此矛盾。`route_visibility` 优先——unknown 航线永不渲染，无论 accessibility 返回什么。但必须记录警告日志："route [航线X]: traversable=true but knowledge=unknown —— data consistency violation in intel system." 航图不渲染该航线，不崩溃。这是防御性编程措施——根源在情报系统（#6），但航图不得将不一致数据传播给玩家。`route_selectability` 的短路求值天然预防了此问题——hidden 在第一分支返回，traversable 检查永不执行。

涉及的规则/公式/状态：`route_visibility` 分支 1（unknown → false，无条件优先），`route_selectability` 短路求值（hidden → 立即返回），航图"只读上游系统"合约——航图检测但不修复上游数据问题。

---

**EC-16: 两步确认间隙内风险标签变更**

触发场景：玩家在 ROUTE_SELECTED 状态下选中航线 A。侧边面板显示风险：绿色（safe）。在玩家点击"确认出航"（第一步）之前，情报系统更新了航线 A 的 hazard_tags，新增 `"pirate_activity"`——风险等级从绿色变为红色。玩家正在阅读的选中详情包含过时数据。

预期行为：玩家点击"确认出航"（两步确认的第一步）时，航图必须重新查询 `query_route_accessibility()` 和 `query_route_knowledge()` 获取当前数据，然后展示最终确认浮层。若 hazard_tags 或风险等级已变更，确认浮层必须反映当前状态（红色、pirate_activity），而非选中时的状态。这确保知情同意——玩家在做出最终"出航"决策前看到最新风险。若航线在此期间变为 UNAVAILABLE（`traversable → false`），确认被阻止，航线强制取消选择并通知："航线 [A 名称] 状态已变更——无法出航。"

涉及的规则/公式/状态：两步确认合约（第一步必须刷新数据），`route_selectability` 在确认触发时的重新评估，`chart_state_transition`（CONFIRM 触发携带刷新后的 payload），`route_committed` 事件携带最新 hazard_tags。

## Dependencies

### Upstream Systems (航图从它们读取)

| 系统 | 提供内容 | 接口 | 航图中的使用位置 |
|------|---------|------|-----------------|
| **#1 内容数据与状态注册表** | 所有 `kind=route` 条目（起终点、距离带、风险标签）、所有 `kind=location` 条目（港口类型、服务标签）、内容域就绪状态、`base_lock_duration` 常量、`base_timestamp_tolerance` 常量 | `list_by_kind("route")`、`list_by_kind("location")`、`domain_state` 查询、`get_constant()` | 加载门控（规则 #2），航线渲染数据源（规则 #5-8），可选择性评估数据源（规则 #9），出航锁定持续时间（规则 #16），快照校验（Formula 4） |
| **#6 玩家知识与情报** | 航线知识状态、航线可通行性、地点知识状态、风险标签揭示/隐藏、来源标注、置信度层级 | `query_route_knowledge(route_id)`、`query_route_accessibility(route_id)`、`query_location_discovery(location_id)` | 航线可见性（Formula 1），视觉编码（规则 #5-6），可选择性（Formula 2 分支 3），风险标签展示（规则 #7），来源 tooltip（规则 #7），代理节点渲染（跨系统渲染规则） |
| **#7 飞艇家园 Hub** | 当前停靠地点 ID | `get_current_docked_location()` | 可选择性评估（Formula 2 分支 4），航线筛选（规则 #9） |

### Downstream Systems (航图向它们写入/发出信号)

| 系统 | 航图提供的内容 | 接口/时机 | 使用目的 |
|------|--------------|---------|---------|
| **#3 本地存档与世界状态持久化** | `progress.routes` 快照包（`last_committed_route_id`、`departure_state`、`active_filter`、`last_departure_timestamp`、`hide_rumored`） | 出航确认时写入；恢复时读取 | 出航决策持久化、会话恢复、检查点创建 |
| **#10 航行与路线风险** | `route_committed` 事件（`route_id`、`destination_id`、`hazard_tags`） | 出航确认后发射 | 触发航行阶段、遭遇生成、风险后果评估 |
| **#16 UI / HUD / 航图界面** | 航图状态查询接口（`get_chart_state()`、`get_visible_routes()`、`get_selected_route()`、`get_filter_state()`） | 持续查询 | 航图渲染、控件交互、动画播放、输入处理 |

### 合约边界

- **只读合约**：航图只读注册表（#1）和情报系统（#6），永远不写入知识状态或修改静态定义。
- **事件合约**：航图只发出事件给航行系统（#10），事件发出后航图关闭，不再参与航行过程。
- **数据合约**：航图只提供数据给 UI 系统（#16）——状态枚举、可见航线列表、已选航线 ID、筛选器状态。UI 系统拥有所有视觉渲染、控件位置、输入处理和动画。
- **快照合约**：航图实现 `snapshot_package_validity` 校验（Formula 4），存档系统（#3）在写入前调用。校验失败时存档系统必须拒绝写入并回退。
- **加载门控合约**：四大内容域（`routes`、`world`、`intel`、`threats`）必须全部 `COMPLETE` 航图才进入 BROWSING。此约束由注册表（#1）强制执行——航图不拥有域管理。

## Tuning Knobs

### 时间相关

| 参数 | 默认值 | 安全范围 | 来源 | 调整影响 |
|------|--------|---------|------|---------|
| `base_lock_duration` | 2.0s | 1.5s – 3.0s | 注册表常量 | 出航锁定时长——过短：确认感不足，玩家感觉不到承诺的重量；过长：打断节奏，玩家等待焦虑 |
| `ink_spread_duration` | 1.5s | 1.0s – 2.0s | 规则 #16f | 墨迹扩散动画时长——应略短于 `base_lock_duration`，给锁定结束留下 0.5s 缓冲 |
| `chart_fade_in_duration` | 0.8s | 0.5s – 1.2s | 规则 #4 | 羊皮纸渐变过渡——心理切换信号，过短则突兀，过长则拖沓 |
| `pulse_duration` | 0.3s | 0.2s – 0.5s | 规则 #13 | 航线选中暖金色脉冲——确认反馈，过短则不易察觉，过长则冗余 |
| `color_transition_duration` | 0.3s | 0.2s – 0.5s | EC-7 | 航线可选择性状态变化时的颜色过渡——UNAVAILABLE→BROWSABLE 的视觉恢复速度 |
| `base_timestamp_tolerance` | 300s | 60s – 600s | 注册表常量，Formula 4 | 快照时间戳未来方向的容忍偏差——过小：轻微时钟偏差异常导致误拒；过大：真正损坏的快照可绕过检测 |

### 视觉编码相关

| 参数 | 默认值 | 安全范围 | 来源 | 调整影响 |
|------|--------|---------|------|---------|
| `rumored_opacity` | 60% | 40% – 75% | 规则 #5 | 传闻航线的透明度——过低：难以阅读，玩家忽略传闻航线；过高：与 identified 区分度不足 |
| `rumored_line_width` | 2px | 1.5px – 3px | 规则 #5 | 传闻航线描边宽度——与透明度配合，共同决定可读性 |
| `selected_dimming` | 40% | 30% – 50% | 规则 #13 | 已选航线时其他航线的透明度——过低：背景航线不可见，失去空间上下文；过高：选中航线不突出 |
| `verified_glow_color` | 暖金色 | 暖金色 / 暖琥珀 / 淡金 | 规则 #5 | verified 航线的发光边缘颜色——必须与其他 UI 金/黄色语义一致 |
| `empty_chart_fog_opacity` | （由 UI 系统定义） | — | 规则 #17 | 未知空间边缘雾的浓度——过浓：空白区域像"缺失"，而非"等待被绘制" |

### 排序相关

| 参数 | 默认值 | 安全范围 | 来源 | 调整影响 |
|------|--------|---------|------|---------|
| `knowledge_rank_multiplier` | 100 | 50 – 1000 | Formula 5 | 知识层级在排序中的权重——过小：距离可能压倒知识层级；过大：同知识层级的距离差异被完全抹平。100 确保知识权重至少是距离权重的 100 倍 |
| `distance_rank_short/medium/long` | 1 / 2 / 3 | — | Formula 5 | 距离带排序值——改变间距影响同知识层级内不同距离航线的相对位置 |

### 筛选器相关

| 参数 | 默认值 | 安全范围 | 来源 | 调整影响 |
|------|--------|---------|------|---------|
| `hide_rumored_default` | `false` | — | 规则 #11 | 新航图会话的初始筛选器状态——`true` 会隐藏初始玩家的全部传闻航线（可能只有传闻航线），`false` 是安全默认值 |

### 内容门控相关

| 参数 | 默认值 | 安全范围 | 来源 | 调整影响 |
|------|--------|---------|------|---------|
| `required_domains` | `["routes", "world", "intel", "threats"]` | — | 规则 #2 | 加载门控的必需域列表——增加域会增加加载失败概率；减少域会降低信息完整性保证。应仅随游戏内容范围变更而调整 |

## Visual/Audio Requirements

### 视觉需求

**航图背景**
- 羊皮纸纹理底图，带轻微的老化边缘色偏（暖黄/褐基调）
- 非航线覆盖区域以柔雾渐变过渡到边缘——非黑色虚空，而是"等待被绘制的空白羊皮纸"
- 0.8s 渐变过渡从飞艇场景切换到航图视图（规则 #4）

**航线渲染**
- 三种知识状态视觉编码（规则 #5）：
  - `rumored`：2px 虚线，60% 透明度，端点虚线圆
  - `identified`：实线轮廓，端点空心圆
  - `verified`：实线 + 暖金色发光边缘，端点实心圆
- 颜色编码（规则 #6）：绿色（安全/已验证），黄色（部分已知），红色（高风险+信息不全）
- 风险标签以图标行显示在航线中点下方；来源标注以小号手写体文字显示在风险标签下方
- 悬停时线条变亮；已选航线闪烁暖金色脉冲（0.3s）；其余航线变暗至 40%

**地点节点**
- 已知地点按知识状态渲染（规则 #5 相同层级）
- 代理节点（航线已知但目的地点 unknown）：使用航线知识状态对应的最低视觉层级，标注"由航线推导"

**动画**
- 航线选中脉冲：0.3s 暖金色（规则 #13）
- 墨迹扩散：1.5s 沿选定航线从起点描摹至目的地（规则 #16f）
- UNAVAILABLE→BROWSABLE 颜色过渡：0.3s（EC-7）
- 确认浮层弹出/消失

**错误状态**
- ERROR 显示安全错误提示，包含失败域名称和状态
- 重试按钮可用

### 音频需求

| 音频事件 | 触发时机 | 感觉方向 |
|---------|---------|---------|
| 航图展开 | 海图桌交互锚点激活 → LOADING | 羊皮纸展开/铺平的质感——纸张摩擦 + 轻微的低沉回响，给玩家"切换到了规划模式"的心理信号 |
| 航线悬停 | 焦点移动到一条航线（鼠标悬停或 Tab 切换） | 轻柔的指示音——像指尖划过纸面的细微沙沙声。不打断思考 |
| 航线选中 | 玩家点击/Enter 选择航线 | 确认音——短促而温暖，配合暖金色脉冲。类似墨迹滴落或印章盖下的感觉 |
| 取消选择 | Esc 或点击空白区域 → 回到 BROWSING | 微妙的中性音——不如选中音温暖 |
| 出航确认第一步 | 点击"确认出航"弹出浮层 | 加重——低沉、严肃，提示"这是一个有重量的决策" |
| 出航确认第二步 | 点击"出航" → DEPARTURE_CONFIRMED | 不可逆承诺音——类似锚链落下或船帆展开的低沉声音，配合墨迹扩散动画 |
| 墨迹扩散 | 1.5s 动画期间 | 连续的毛笔/鹅毛笔书写音——笔尖划过纸面的沙沙声，从起点到目的地 |
| ERROR | 加载失败 | 不和谐音——短促但温和，不惊吓 |
| 背景氛围 | BROWSING 期间 | 极微弱的持续环境音——类似旧图书馆或地图室的低频嗡鸣，沉静而专注 |

## UI Requirements

> 以下需求定义航图系统提供给 UI 系统（#16）的数据接口和交互契约。UI 系统拥有所有视觉渲染、控件位置、输入处理和动画的最终决定权。

### 布局需求

- 航图区域占屏幕约 70%，侧边详情面板占 30%（规则 #8）
- MVP 固定视图——2 条航线从同一出发港辐射，整个航线网络适配单屏，无需平移/缩放（规则 #8）

### 航图状态查询接口（由 UI 系统消费）

```
get_chart_state() → enum {LOADING, BROWSING, ROUTE_SELECTED, DEPARTURE_CONFIRMED, ERROR}
get_visible_routes() → Array[String]          // 可见航线 ID 列表（按 display_order 排序）
get_selected_route() → String | null          // 当前选中航线 ID，无选中则为 null
get_filter_state() → {hide_rumored: bool}     // 当前筛选器状态
```

**合约约定**: 以上接口只返回数据和状态枚举——不返回颜色值、位置坐标、透明度、动画关键帧。UI 系统完全拥有视觉层的实现自由。

### UI 控件需求

**航图视图**
- 羊皮纸背景渲染（UI 系统拥有纹理、颜色、渐变的具体实现）
- 航线渲染：UI 系统根据 `get_visible_routes()` 的结果和每条航线的知识状态（由 `query_route_knowledge` 从 #6 获取）套用视觉编码规则
- 地点节点：已知地点按知识状态渲染，代理节点以最低视觉层级渲染
- 未知空间：柔雾渐变边缘

**侧边详情面板**
- 浏览模式（`BROWSING`）：显示当前悬停航线的名称、风险标签摘要、来源标注、一句话概述
- 选中模式（`ROUTE_SELECTED`）：展开显示完整详情——所有风险标签、所有来源标注、已知风险列表 vs 未知风险计数
- 确认出航按钮：仅在 `ROUTE_SELECTED` 状态下显示于面板底部

**筛选器切换**
- 单一开关："显示/隐藏仅传闻航线"（规则 #11）
- 默认：显示（`hide_rumored = false`）
- 即时响应，无过渡动画阻塞交互

**确认浮层**
- 两步确认的第一步展示（规则 #15）
- 内容：航线名称、风险摘要（刷新后数据）、预估距离带、"出航"/"取消"按钮
- 刷新数据后再展示——确保知情同意

**Tooltip**
- 悬停来源标注文字时弹出，展示完整来源名称和置信度层级（规则 #7）
- 悬停阻塞航线时显示阻塞原因（规则 #9-10）

**上下文消息**（空航图、全 UNAVAILABLE 等）
- 区分不同空状态的消息文案（详见 Edge Cases EC-12/13/14）

**ERROR 画面**
- 显示失败域名称、域状态、重试按钮

### 交互需求

**鼠标交互**
- 点击航线 → SELECT（如可选）
- 点击空白区域 → DESELECT
- 悬停航线 → 线条变亮 + 面板显示摘要
- 悬停来源标注 → tooltip

**键盘交互**
- Tab / Shift+Tab → 在航线间移动焦点（顺序按 `route_display_order`）
- Enter → 选中/确认（根据当前上下文）
- Esc → 取消选择/关闭浮层

**锁定状态**
- `DEPARTURE_CONFIRMED` 期间所有交互禁用（规则 #16d）

## Acceptance Criteria

> 以下验收标准按类别分组。每条 AC 必须可被 QA 测试人员验证为**通过/失败**。
> 测试数据以 MVP 配置为基础：两条航线 `route.sky-reef-arc-01`（`identified`, `safe`, 可通行, 起点 `location.glass-harbor`）和 `route.storm-cut-01`（`rumored`, `storm` + `low-visibility`, 可通行, 起点 `location.glass-harbor`），玩家停靠于 `location.glass-harbor`。

### 状态机 (State Machine)

**AC-01 — 加载成功进入浏览**
- **类别**: State Machine
- **前置条件**: 四大内容域（`routes`, `world`, `intel`, `threats`）均为 `COMPLETE`；注册表包含两条 MVP 航线；玩家停靠于 `location.glass-harbor`
- **操作**: 与海图桌交互锚点（`home-anchor.chart-table`）交互
- **预期结果**: 航图从 `LOADING` 转为 `BROWSING`。`route.sky-reef-arc-01` 以绿色实线 + 空心圆渲染；`route.storm-cut-01` 以黄色虚线 + 60% 透明度 + 虚线圆渲染。不进入 `ERROR`。羊皮纸过渡动画播放。侧边面板可见但为空（无已选航线）。

**AC-02 — 内容域未就绪导致 ERROR 并支持重试**
- **类别**: State Machine / Contract
- **前置条件**: `routes`, `world`, `intel` 为 `COMPLETE`；`threats` 为 `FAILED`（非 `COMPLETE`，非 `LOADING`）
- **操作**: 与海图桌交互锚点交互。观察错误提示内容。点击"重试"按钮。在重试前将 `threats` 修复为 `COMPLETE`。
- **预期结果**: 首次交互：`LOADING → ERROR`。ERROR 界面明确显示失败域名称（`threats`）和状态（`FAILED`），而非泛化"加载失败"消息。重试触发 `ERROR → LOADING`（RETRY）。修复后加载成功，`LOADING → BROWSING`。不存在 `ERROR → BROWSING` 的直达路径。

**AC-03 — 航线选中与取消选择**
- **类别**: State Machine
- **前置条件**: 航图处于 `BROWSING` 状态，两条 MVP 航线均可见且可选
- **操作**:
  a. 点击 `route.sky-reef-arc-01`
  b. 等待暖金色脉冲完成（0.3s）
  c. 观察航图变化
  d. 按 Esc 键
- **预期结果**:
  - 步骤 a-b: `BROWSING → ROUTE_SELECTED`。`route.sky-reef-arc-01` 的航线子状态变为 `SELECTED`，其他航线降至 40% 透明度。侧边面板展开显示完整详情。确认出航按钮出现在面板底部。
  - 步骤 d: `ROUTE_SELECTED → BROWSING`。`route.sky-reef-arc-01` 子状态回到 `BROWSABLE`，其他航线恢复 100% 透明度。侧边面板清空。确认出航按钮消失。

**AC-04 — 两步出航确认进入不可逆承诺**
- **类别**: State Machine
- **前置条件**: `ROUTE_SELECTED` 状态，已选中 `route.sky-reef-arc-01`
- **操作**:
  a. 点击"确认出航"按钮（第一步）
  b. 观察确认浮层内容
  c. 在确认浮层中点击"出航"按钮（第二步）
  d. 锁定期间尝试点击航图任意区域
- **预期结果**:
  - 步骤 a: 弹出确认浮层。浮层显示: 航线名称（`route.sky-reef-arc-01`）、风险摘要（`safe`）、预估距离带、"出航"和"取消"两个按钮。
  - 步骤 c: `ROUTE_SELECTED → DEPARTURE_CONFIRMED`。所有航线子状态变为 `LOCKED`。墨迹扩散动画沿选中航线播放（1.5s）。出航锁定时长 2.0s。`route_committed` 事件恰好发射一次，携带 `route_id`、`destination_id`、`hazard_tags`。
  - 步骤 d: 锁定期间所有交互被禁用——点击无响应、按钮不可点击、Tab/Enter 无效。

**AC-05 — 无效状态转换被拒绝**
- **类别**: State Machine / Formula 3
- **前置条件**: 通过自动化测试或手动构造以下触发场景
- **操作与预期结果**（每行为一个子用例）:

| 当前状态 | 触发 | 预期结果 |
|---------|------|---------|
| `DEPARTURE_CONFIRMED` | `SELECT`（尝试重新选航线） | `{new_state: DEPARTURE_CONFIRMED, allowed: false}`。状态不变。航线保持在 `LOCKED`。 |
| `ERROR` | `COMPLETE`（尝试跳过 LOADING 直达 BROWSING） | 转换被禁止。ERROR 状态只能通过 RETRY → LOADING 离开。 |
| `UNAVAILABLE` 的航线 | 对该航线执行 `SELECT` | 航线不能从 `UNAVAILABLE` 直接跳到 `SELECTED`。必须先变为 `BROWSABLE`。 |

### 公式 (Formula)

**AC-06 — Formula 1: 航线可见性判定（三条分支）**
- **类别**: Formula
- **前置条件**: 注册表包含 4 条航线: A=`unknown`, B=`rumored`, C=`identified`, D=`verified`。`hide_rumored = false`。内容域均 COMPLETE。
- **操作**: 打开航图，记录可见航线。设置 `hide_rumored = true`，再次记录。恢复 `hide_rumored = false`。
- **预期结果**:
  - `hide_rumored = false`: A 不可见（`unknown → false`），B/C/D 均可见。
  - `hide_rumored = true`: A 不可见，B 不可见（`hide_rumored AND rumored → false`），C/D 仍可见。
  - 恢复后: B 重新可见。A 始终不可见（`unknown` 是硬性边界）。

**AC-07 — Formula 2: 航线起点不匹配当前港口导致 UNAVAILABLE**
- **类别**: Formula / State Machine
- **前置条件**: BROWSING 状态。新增一条已知航线 R_003: `identified`, `traversable=true`, 起点为 `location.other-port`（非 `glass-harbor`）。玩家停靠于 `glass-harbor`。
- **操作**: 观察 R_003 在航图上的渲染和交互。
- **预期结果**: R_003 可见但置灰。tooltip 显示"不在当前港口——需要从 [other-port 名称] 出发。" 点击 R_003 无效——航线保持在 `UNAVAILABLE` 子状态，不能进入 `SELECTED`。其他以 `glass-harbor` 为起点的航线正常可选。

**AC-08 — Formula 2: 短路求值完整链路**
- **类别**: Formula
- **前置条件**: 构造以下场景并通过自动化单元测试验证 `route_selectability()` 的返回顺序。每条航线代表短路求值的一个分支。
- **操作与预期结果**:

| 航线条件 | chart_state | hide_rumored | docked_location | 预期返回 | 原因（短路分支） |
|---------|-------------|-------------|----------------|---------|----------------|
| `unknown` 航线 | `BROWSING` | `false` | `glass-harbor` | `hidden` | 分支 1: visibility=false |
| `rumored` 航线 | `BROWSING` | `true` | `glass-harbor` | `hidden` | 分支 1: hide_rumored+rumored |
| 任意航线 | `DEPARTURE_CONFIRMED` | — | — | `locked` | 分支 2: 终端状态 |
| `identified` 航线, `traversable=false` | `BROWSING` | `false` | `glass-harbor` | `unavailable` | 分支 3: 不可通行 |
| `identified` 航线, 起点≠当前港口 | `BROWSING` | `false` | `glass-harbor` | `unavailable` | 分支 4: 起点不匹配 |
| 已选中航线 | `ROUTE_SELECTED` | `false` | `glass-harbor` | `selected` | 分支 5: route_id匹配 |
| 未选中航线 | `ROUTE_SELECTED` | `false` | `glass-harbor` | `browsable` | 分支 6: 降级到浏览 |
| `identified` 航线, 未选中 | `BROWSING` | `false` | `glass-harbor` | `browsable` | 分支 7: 默认 |

**AC-09 — Formula 4: 有效快照包通过校验**
- **类别**: Formula
- **前置条件**: 构造一个合法的 `progress.routes` 快照包：
  ```
  {
    domain_id: "progress.routes",
    fields: {
      last_committed_route_id: "route.sky-reef-arc-01",
      departure_state: "DEPARTURE_CONFIRMED",
      active_filter: "default",
      last_departure_timestamp: 999.0,
      hide_rumored: false
    }
  }
  ```
  当前游戏时间 `current_time = 1000.0`。注册表存在 `route.sky-reef-arc-01`。
- **操作**: 调用 `snapshot_package_validity(pkg)`。
- **预期结果**: `{valid: true, violations: []}`。

**AC-10 — Formula 4 + EC-9: 快照引用已删除航线 → 校验失败**
- **类别**: Formula / Edge Case
- **前置条件**: 快照包 `last_committed_route_id = "route.deleted-001"`, `departure_state = "DEPARTURE_CONFIRMED"`, `last_departure_timestamp = 500.0`（有限值）。注册表 `list_by_kind("route")` 不包含 `route.deleted-001`。当前时间 `current_time = 1000.0`。
- **操作**: 调用 `snapshot_package_validity(pkg)`。
- **预期结果**: `{valid: false, violations: ["route_id not found in registry: route.deleted-001"]}`。存档系统必须拒绝此快照包写入。

**AC-11 — Formula 5: MVP 航线排序顺序**
- **类别**: Formula
- **前置条件**: 注册表包含两条 MVP 航线。`query_route_knowledge` 对 `route.sky-reef-arc-01` 返回 `identified`，对 `route.storm-cut-01` 返回 `rumored`。距离带分别为 `short` 和 `medium`。`hide_rumored = false`。
- **操作**: 加载航图，检查侧边面板或航线列表中的展示顺序。
- **预期结果**:
  - `route.sky-reef-arc-01` 的 display_order = `2×100 + 1 = 201`
  - `route.storm-cut-01` 的 display_order = `3×100 + 2 = 302`
  - `sky-reef-arc-01`（201）排在 `storm-cut-01`（302）之前——已验证/已确认的航线排在前，知识置信度决定排序层级。

### 边界情况 (Edge Case)

**AC-12 — EC-3: 出航锁定期间快速连点**
- **类别**: Edge Case
- **前置条件**: 出航确认第二步已点击，"出航"按钮触发 `DEPARTURE_CONFIRMED`。墨迹扩散动画播放中（锁定 2.0s 期间）。`hide_rumored = false`。
- **操作**: 在锁定期间快速连续执行以下操作（间隔 <100ms，共 10 次以上）:
  - 点击航图空白区域
  - 点击任意航线
  - 按 Enter 键
  - 按 Tab 键
- **预期结果**: 所有输入事件被 `route_selectability` 分支 2 拦截，所有航线返回 `locked`。航图状态保持 `DEPARTURE_CONFIRMED`。`chart_state_transition` 的终端状态守卫拒绝所有转换。`route_committed` 事件仅发射一次——无事件排队、无重复快照。锁定结束后航图按正常流程移交控制权至航行系统。

**AC-13 — EC-5: 情报撤销已选航线**
- **类别**: Edge Case
- **前置条件**: 航图处于 `ROUTE_SELECTED`，已选中 `route.sky-reef-arc-01`（`knowledge_state = identified`）。`hide_rumored = false`。
- **操作**: 外部情报系统触发将 `route.sky-reef-arc-01` 的知识状态从 `identified` 更新为 `unknown`（模拟情报物品过期或来源被证伪）。航图检测到此变化。
- **预期结果**:
  - `route_visibility(route.sky-reef-arc-01)` 返回 `false`（`unknown → false`）。
  - 航图从 `ROUTE_SELECTED` 强制转为 `BROWSING`（系统生成的 DESELECT 触发）。
  - 航线从航图上消失。侧边面板清空。
  - 显示通知："航线 [sky-reef-arc-01 名称] 的情报已失效——该航线的知识来源不再可信。"
  - 若航图上存在其他可选航线，玩家可正常浏览和选择。若无可选航线，显示上下文消息。

**AC-14 — EC-12: 首次打开——所有航线未知**
- **类别**: Edge Case
- **前置条件**: 玩家从未收集任何情报。注册表中所有航线的 `knowledge_state = unknown`（默认）。`hide_rumored = false`（默认）。四大内容域均为 `COMPLETE`。
- **操作**: 与海图桌交互锚点交互。
- **预期结果**:
  - `route_visibility` 对所有航线返回 `false`。`get_visible_routes()` 返回空数组。
  - 加载门控通过（域均为 COMPLETE），航图进入 `BROWSING` 状态。
  - 航图渲染: 羊皮纸背景 + 边缘雾渐变 + 居中上下文消息"航图上尚无已知航线。在世界中收集情报以揭示航线。"
  - 切换 `hide_rumored` 为 `true` 再切回 `false`：无可见变化（所有航线均为 `unknown`，非 `rumored`，切换不影响）。
  - 不崩溃、不黑屏、不进入 `ERROR` 状态——空航图是合法状态。

**AC-15 — EC-16: 两步确认间隙内风险标签变更**
- **类别**: Edge Case
- **前置条件**: 航图处于 `ROUTE_SELECTED`，已选中 `route.sky-reef-arc-01`。侧边面板显示风险标签 `safe`（绿色）。玩家尚未点击"确认出航"。
- **操作**:
  a. 外部情报系统将 `route.sky-reef-arc-01` 的 `hazard_tags` 更新为新增 `pirate_activity`（风险等级变为红色）。航图检测到变化。
  b. 玩家点击"确认出航"（第一步）。
  c. 观察确认浮层内容。
- **预期结果**:
  - 步骤 b: `chart_state_transition` 的 CONFIRM 触发在执行前重新查询 `query_route_accessibility()` 和 `query_route_knowledge()`，获取当前数据。
  - 步骤 c: 确认浮层反映当前风险状态——显示 `pirate_activity`，风险等级红色。而非选中时的过时数据（`safe`）。
  - 若在此期间 `traversable` 变为 `false`，确认被阻止，航线强制取消选择，通知："航线 [名称] 状态已变更——无法出航。"
  - `route_committed` 事件携带的 `hazard_tags` 为最新值（含 `pirate_activity`）。

### MVP 配置 (MVP Configuration)

**AC-16 — 两条 MVP 航线视觉编码正确**
- **类别**: MVP
- **前置条件**: 四大内容域 COMPLETE。玩家停靠于 `glass-harbor`。`hide_rumored = false`。
- **操作**: 打开航图，逐一检查两条航线的视觉编码。
- **预期结果**:

| 航线 ID | 线条样式 | 颜色 | 端点圆 | 风险标签可见 | 来源标注 |
|---------|---------|------|--------|------------|---------|
| `route.sky-reef-arc-01` | 实线 | 绿色 | 空心圆 | `safe`（完整显示） | "空港基础航图" |
| `route.storm-cut-01` | 2px 虚线，透明度 60% | 黄色 | 虚线圆 | `storm` 可见, `low-visibility` 显示为闪烁 `?` | "港口传闻" |

- 两条航线均以 `location.glass-harbor` 为起点。tooltip 内容与知识状态匹配（`identified` = 来源名称和置信度层级；`rumored` = 置信度标注"不确定"）。

### 合约边界 (Contract Boundary)

**AC-17 — 只读上游 + 事件下游合约**
- **类别**: Contract
- **前置条件**: BROWSING 状态。记录所有航线的知识状态为基准快照。记录 `route_committed` 事件计数器为 0。
- **操作**: 完成一次完整的"打开航图 → 选择航线 → 两步确认出航"流程。在流程结束后检查基准快照中的知识状态。
- **预期结果**:
  - **只读验证**: 所有航线的知识状态与流程前快照完全一致——航图未修改任何知识状态或注册表静态数据。
  - **事件验证**: `route_committed` 事件恰好发射一次，携带正确的 `route_id`、`destination_id`、`hazard_tags`。
  - 事件发射后航图进入 `DEPARTURE_CONFIRMED`→关闭，航图不再参与航行过程。

**AC-18 — 数据合约 + 加载门控合约**
- **类别**: Contract
- **前置条件**: 航图在 BROWSING 或 ROUTE_SELECTED 状态。UI 系统（#16）可以通过 `get_chart_state()`, `get_visible_routes()`, `get_selected_route()`, `get_filter_state()` 查询航图状态。
- **操作**:
  a. 分别查询四个接口，验证返回值的数据类型和内容。
  b. 设置任意一个内容域为非 COMPLETE，尝试加载航图。
- **预期结果**:
  - **数据合约**: `get_chart_state()` 返回有效状态枚举；`get_visible_routes()` 返回航线 ID 数组（类型 `Array[String]`）；`get_selected_route()` 返回已选航线 ID 或 `null`；`get_filter_state()` 返回 `{hide_rumored: bool}`。所有接口不返回视觉样式数据（颜色、位置、透明度）——这些由 UI 系统拥有。
  - **加载门控**: 非 COMPLETE 域导致 LOADING→ERROR。航图不跳过门控直接进入 BROWSING。

### 可访问性 (Accessibility)

**AC-19 — 键盘导航完整流程**
- **类别**: Accessibility
- **前置条件**: 航图处于 BROWSING 状态。`hide_rumored = false`。至少两条可选航线（MVP 两条）。
- **操作**（全程仅使用键盘，不碰鼠标）:
  a. 按 Tab 键在可见航线之间移动焦点
  b. 焦点停在 `route.sky-reef-arc-01` 上时，按 Enter 键
  c. 观察选中状态后，按 Esc 键取消选择
  d. 按 Tab 移动焦点到 `route.storm-cut-01`，按 Enter 键
  e. 按 Tab 移动焦点到"确认出航"按钮，按 Enter 键（第一步）
  f. 在确认浮层中按 Tab 移动焦点到"出航"按钮，按 Enter 键（第二步）
- **预期结果**:
  - 步骤 a: 焦点在航线间循环移动。悬停航线时线条变亮，侧边面板显示航线名称和摘要。焦点顺序与 `route_display_order` 一致（`sky-reef-arc-01` → `storm-cut-01`）。
  - 步骤 b: Enter 触发的选中行为与鼠标点击完全一致——脉冲、面板展开、其他航线变暗 40%、确认出航按钮出现。
  - 步骤 c: Esc 触发取消选择，可视反馈与鼠标点击空白区域一致。
  - 步骤 d-f: 完整的两步确认出航流程仅通过键盘完成，最终进入 `DEPARTURE_CONFIRMED`。所有可视化反馈（墨迹动画、渐变过渡）正常播放。

**AC-20 — 筛选器切换：隐藏/显示仅传闻航线**
- **类别**: Accessibility
- **前置条件**: 航图处于 BROWSING。仅存入两条 `rumored` 状态的航线（将 MVP 两条航线的知识状态临时改为 `rumored`）。`hide_rumored = false`（默认）。
- **操作**:
  a. 观察两条航线均以传闻样式渲染
  b. 将筛选器切换为 `hide_rumored = true`
  c. 观察航图和上下文消息
  d. 将筛选器切回 `hide_rumored = false`
- **预期结果**:
  - 步骤 b: 两条航线消失。航图显示上下文消息："所有航线均为传闻级别——关闭'隐藏传闻航线'以查看。" 此消息区别于 AC-14 的"从未有过航线"消息。
  - 步骤 c: 两条航线恢复渲染，恢复传闻样式。没有航线"丢失"或永久不可见。
  - 筛选器切换是即时响应（无延迟动画阻塞交互）。
  - `hide_rumored` 状态被正确记录在 `progress.routes` 快照包中（可在出航确认后验证）。

## Open Questions

### 设计阶段待定

| # | 问题 | 影响范围 | 优先级 | 备注 |
|---|------|---------|--------|------|
| OQ-01 | 个人标注功能——自由文本输入还是预设标签（如"危险""安全锚地""有补给"）？ | verified 航线的标注系统（规则 #5） | 低（MVP 后） | 自由文本更符合"制图者"幻想，但需要输入法支持、审核和存档大小考虑。预设标签更安全、更易实现，但降低了个人化程度 |
| OQ-02 | 代理节点的"由航线推导"标注是否随地点知识状态提升而自动更新？ | 跨系统渲染规则（Detailed Design） | 中 | 当前规范：航图检测但不修复。但当地点从 unknown→known 时，代理节点应从"推导标注"变为正常节点——此转换由航图还是 UI 系统负责？ |
| OQ-03 | 多航线网络时是否需要平移/缩放？ | 航图布局（规则 #8） | 中（MVP 后） | MVP 固定视图足够，但扩展后航线网络可能超出单屏 |
| OQ-04 | 航线发现途径——通过情报物品、侦察伙伴报告、旧航海日志、个人目击各占多大比重？ | 知识与情报系统（#6） | 低（属于 #6 范围） | 航图是消费端，但发现途径的多样性影响航图初始空状态的持续时间 |
| OQ-05 | 筛选器未来需要哪些额外维度？（风险等级、距离带、航线类型、个人标注标签） | 筛选器切换（规则 #11） | 低（MVP 后） | MVP 只有 rumor 开关，但 UI 布局应预留 future filter 扩展空间 |

### 跨系统协调

| # | 问题 | 涉及系统 | 优先级 | 备注 |
|---|------|---------|--------|------|
| OQ-06 | NPC 飞船飞行轨迹何时纳入航图？ | 航图（#9）+ 空港/村镇（#14） | 低（Phase 3+） | 已在 Overview 中明确标注不在 MVP 范围。需要贸易流量数据作为源数据 |
| OQ-07 | 玩家委托 NPC 船运货时，航图是否需要显示物流状态？ | 航图（#9）+ 空港/村镇（#14）+ 资源/货物（#5） | 低（Phase 3+） | 明确属于 #14 的市场服务扩展。航图可能只读取结果（"此航线有定期货运服务"），不管理物流 |
| OQ-08 | 天气/世界修复系统（#13）是否需要直接影响航图上的航线渲染？（如：暴风雨动画覆盖在航线上） | 航图（#9）+ 世界修复（#13） | 中（Phase 2+） | 可能影响航图作为"规划工具"的体验——玩家在航图上看到天气，可以在出航前做更明智的决策 |

### 技术实现

| # | 问题 | 影响范围 | 优先级 | 备注 |
|---|------|---------|--------|------|
| OQ-09 | 羊皮纸纹理是静态资源还是程序化生成？ | 航图视觉 | 低 | 程序化可提供更多变化（折痕、污渍），但增加开发成本 |
| OQ-10 | 航线缓存更新策略——轮询间隔还是响应式推送？ | 航图性能 | 中 | EC-5/6/7/8 都依赖"航图检测到变化"。轮询简单但有延迟；推送及时但增加系统间耦合 |
| OQ-11 | 墨迹扩散动画使用什么技术实现？（Godot 4.6: shader、Line2D 动画、AnimatedSprite2D） | 航图动画 | 低（属于 UI 系统 #16） | 由 UI 系统决定 |

### CD-GDD-ALIGN 审查建议 (Creative Director Review, 2026-05-02)

以下建议来自 CD-GDD-ALIGN 审查（判决：PASS WITH NOTES，无阻断项）。全部为非阻断建议，记录于此供后续迭代参考。

| # | 建议 | 来源 | 优先级 | 状态 |
|---|------|------|--------|------|
| OQ-12 | ERROR 消息应使用海图师/幻想语言而非后端域名（如"航图墨水无法凝聚——威胁情报网络中断"而非"threats 域状态：FAILED"）。原则在此 GDD 建立，具体措辞由 UI 系统（#16）实现 | R2 | 低 | 待定 |
| OQ-13 | 首次打开空航图的情感包装——建议 UI 添加罗盘玫瑰、墨水瓶、鹅毛笔等环境元素，让空白羊皮纸感觉是"可能性"而非"未载入" | R3 | 低 | 待定 |
| OQ-14 | 返航后航图的状态变化——建议在已航行的航线上显示"上次航行"时间戳或细微磨损标记，闭合出发与返航之间的叙事循环 | R4 | 低 | 待定 |
| OQ-15 | 分级确认摩擦——post-MVP 考虑 verified 安全航线简化为一步确认，rumored/高风险航线保留两步确认。让额外摩擦承载信息："这条航线需要更多关注"而非仪式化 | R5 | 中 | 待定 |
| OQ-16 | 状态枚举添加中文显示名映射——在状态机表中增加一列将 `LOADING`/`BROWSING`/`ROUTE_SELECTED`/`DEPARTURE_CONFIRMED`/`ERROR` 映射为中文显示名，防止 UI 系统自行发明术语导致语气不一致 | R6 | 低 | 待定 |
| OQ-17 | NPC 飞船轨迹和物流可视化（已在 Overview 排除）的长期路线图——确认属于 Phase 3+ 的空港/村镇系统（#14）扩展 | R1 讨论中提及 | 低 | 已排除 |

### 设计审查建议 (design-review, 2026-05-02)

以下建议来自正式 `/design-review` 审查（systems-designer + qa-lead + creative-director）。判决：NEEDS REVISION（1 个阻断项 CB-4 已修复），其余均为非阻断。

| # | 建议 | 来源 | 优先级 | 状态 |
|---|------|------|--------|------|
| OQ-18 | AC 可测试性审计——7 条 AC（AC-04/05/08/09/10/12/17/18）需要对自动化和手动验证方式做明确标注。纯自动化 AC 应添加 `Verification Method: AUTOMATED ONLY` 标签；手动验证 AC 需确保预期结果可直接由人工 QA 观察 | qa-lead | 中 | 待定 |
| OQ-19 | 12 个覆盖缺口——建议新增 AC-10b~10e（Formula 4 未覆盖分支）、AC-21（EC-2 部分查询失败）、AC-22（EC-4 同帧双击）、AC-23（EC-6 停靠地变更）、AC-24（EC-7 能力解锁）、AC-25（EC-13 零出发航线）、AC-26（DEPARTURE_CONFIRMED 快照恢复）共 9 条新 AC | qa-lead | 中 | 待定 |
| OQ-20 | 调试工具契约——当前 6 条 AC（AC-02/06/07/13/14/20）需要 QA 调试命令（set_route_knowledge, set_domain_state 等）。建议在 AC 章节头部明确调试工具集需求，或写入独立的 QA Tooling Contract 文档 | qa-lead | 中 | 待定 |
| OQ-21 | AC 覆盖缺口——建议新增 AC 覆盖 save/load 恢复路径、route_display_order 验证方法明确化、tooltip 内容精确化、键盘导航在确认浮层中的行为定义 | qa-lead | 低 | 待定 |
| OQ-22 | 公式防御性空值处理——Formula 1 应明确 null/error knowledge_state → 视为 unknown；Formula 2 应处理 LOADING/ERROR 状态下被意外调用的情况；Formula 5 应添加 DEFAULT 分支应对未知枚举值 | systems-designer | 低 | 待定 |
