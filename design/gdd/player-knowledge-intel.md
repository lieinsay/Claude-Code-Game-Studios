# 玩家知识与情报

> **Status**: Approved
> **Author**: User + Claude Code
> **Last Updated**: 2026-04-30
> **Review Verdict**: APPROVED (re-review 2026-04-30, all blockers resolved)
> **Implements Pillar**: 规划先于冒险; 未知带来温和压力; 世界会回应照料

## Overview

`玩家知识与情报` 是《云海织航》的知识与能力成长层。它追踪玩家对空海世界逐步加深的两类永久积累：**情报条目**记录已知的航线、地点、资源、威胁、传闻和旧日志——即玩家"知道什么"；**能力条目**记录已解锁的永久行动能力——即玩家"能做到什么"，例如读懂风流信号、穿越礁石区、在低能见度雾区中辨识方向、解读旧灯塔信号以发现隐藏航线。前者让世界变得更可读，后者让世界变得更可达。

在数据层面，它基于 `内容数据与状态注册表` 的稳定 ID 维护每条情报条目的可见性状态和每条能力条目的解锁状态；在玩家层面，它让航线规划不再盲目、风险判断不再猜盲盒、曾经标记为"不可通行"的区域在解锁相关能力后重新点亮。玩家感受的不是知识面板或技能树，而是从"这里我不敢去"到"现在我准备好了"的成长弧线——每一趟探索都在让世界变得更可读，每一次能力解锁都在让世界变得更可达。

## Player Fantasy

`玩家知识与情报` 服务的核心幻想是双重的：**你是遗忘之路的修复者，也是未知之域的开拓者**。

在空海世界中，知识和能力以四种方式进入你的航图：

1. **继承前人的碎片**：残损的航海日志里褪色的坐标，你可以在自己的航图上重新标记出来。被遗弃的灯塔仍在闪烁——不是氛围光，而是携带着几十年前连接过两个聚落的航线编码，只等有人重新学会解读。你不是第一个航过这片天的人，但你是回来重新点亮航标、把断了的记忆接回去的那个人。

2. **亲身走出的经验**：第三次经过同一条航线时，风险描述已从"情报不足"变成你自己写下的判断——"礁石区，西南风时露头"。这不是系统解锁的信息，是你自己跑熟的路。

3. **学会读懂世界**：风的不规则颤动不是随机动画——是老水手知道前方有暗礁的信号。雾的浓度分层不是氛围——是辨识航道边界的参照。灯塔的闪烁节奏不是装饰——是隐藏航线坐标的语言。世界一直在说话，你只是一点一点学会了听。

4. **踏足无人之境**：并非所有岛屿都在旧日志中有记录。有些岛从未有人踏足——或者曾经上去的人没能回来。航图上的空白区有时不是因为记录被遗忘，而是因为那里确实从未被绘制。当你降落在这样的岛上，你可能是第一个看见它的异样生物、独特资源和隐秘地貌的人。你带回来的不只是资源，是这个岛的名字、它的威胁标记、它的资源标注——这些会永久成为航图的一部分。你不是在填一张别人画好的图，你是在亲手画这张图。

这四种来源共同锚定一个玩家时刻：出航前打开航图时，你看到的不是一堆问号，而是一张有记忆、有笔记、有来源、有信心的活地图——以及一些故意留着的未知边缘。你知道哪些线可以安心飞，哪条航线上的风险提示来自老港务长的警告，哪个标记是你自己上次探索时留下的注记，哪个虚线航线是因为你学会了解读灯塔信号而浮现的，哪片空白是你下一次要亲手去探明的。最终，这个系统让玩家同时拥有两种满足：**"这条路终于被接回去了"的修复感，和"这片海是我第一个看见的"的开拓感。**

## Detailed Design

### Core Rules

**Part 1: 知识系统基础**

1. `玩家知识与情报` 是所有玩家知识状态和永久能力的唯一真相源。其他系统只读本系统暴露的查询接口，不得自行缓存或推导知识状态。
2. 本系统管理三类条目：
   - **规律类知识**（Pattern Knowledge）：玩家观察和理解到的世界运作规则。由观测事件触发，记录在玩家图鉴日志中。
   - **地点类知识**（Location Knowledge）：玩家对特定航线、地点、威胁、资源的了解程度。基于传闻、情报物品消耗、侦察报告和亲身探索。
   - **能力条目**（Ability Entries）：已解锁的永久行动能力。解锁路径独立于知识积累。
3. 所有条目以稳定 ID 标识（`pattern.*`、`location_knowledge.*`、`ability.*`），不依赖显示名或运行时引用。

**Part 2: 规律类知识**

4. 每条规律是一个预定义的世界规则，具有独立的观测进度。规律知识不退化——一旦观察到就永久保留。
5. 规律的发现通过两种方式触发：
   - **叙事引导**：游戏通过镜头特写、环境提示引导玩家注意规律（例如第一次看到鸟群朝一个方向飞时给特写，第二次在另一个岛看到同种鸟类落地时再次特写——暗示"跟鸟能找到岛"）。
   - **玩家自主观察**：玩家在游戏世界中触发特定的观测事件（进入特定区域、跟随特定实体、在特定条件下发现新地点等）。
6. 规律知识状态机：
   - `undiscovered`：玩家尚未触发任何观测事件。规律在知识日志中不可见。
   - `partially_observed`：玩家已触发部分观测事件（`observation_score >= partial_threshold`）。规律名称和模糊提示出现在日志中（如"鸟似乎总朝某个方向飞……"）。无机械性收益。
   - `confirmed`：`observation_score >= confirmation_threshold`，**且**玩家至少一次利用该规律成功达成了某件事（如跟着鸟找到了新岛）。完整规律描述记录在日志中，机械性收益激活（如罗盘上的鸟群方向叠加层）。
7. 每条规律的 `observation_score` = 已触发的各独立观测事件的权重之和。同一观测事件只计首次触发——证据的多样性比重复次数重要。
8. 观测事件由下游系统（探索、航行、伙伴、交互）在特定 gameplay 事件发生时调用本系统接口报告。本系统只接收事件、累计分数、判定状态变更。

**Part 3: 地点类知识**

9. 每个可被玩家认知的实体（航线、地点、威胁、资源节点）拥有独立的知识状态。
10. 地点知识状态机：
    - `unknown`：实体在航图/列表中不可见。初始状态。
    - `rumored`：实体存在已知，但细节模糊不可靠。实体以虚线轮廓/传闻标记显示。仅部分风险标签可见——隐藏的风险标签显示为"?"。来源标注（如"据港口传闻"）。
    - `identified`：从可靠来源获得详细信息，但未亲身验证。实体完全可见，所有静态风险标签显示。来源标注（如"据侦察报告"、"旧航海日志坐标"）。
    - `verified`：玩家已亲临/亲历。所有信息确认，可添加个人标注。来源显示"亲身探索"。此状态不可被新传闻覆盖或降级。
11. 状态转换触发：
    - `unknown → rumored`：`reveal_rumor()` 被调用（NPC、旧日志、伙伴侦察返回）。
    - `unknown → identified`：`consume_intel()` 消耗对应情报物品且 `presentation_tier ∈ {clue, warning}`。
    - `rumored → identified`：`consume_intel()` 消耗对应情报物品，或 `reveal_rumor()` 以 `confidence >= 67` 调用。
    - `identified → verified`：玩家抵达/亲历该实体（移动系统/探索系统发送 `location_arrival` 或等价事件）。
    - `rumored → verified`：同上——玩家可跳过 `identified` 直接亲身验证。
12. 传闻冲突解决规则：
    - `verified` 胜出一切——亲身验证不可被覆盖。
    - `identified`（可靠情报）胜出 `rumored`（传闻）——消耗 intel 物品获得的信息替换传闻的风险标签。
    - 两个 `rumored` 来源冲突时（A 说安全，B 说危险），**同时显示双方来源的风险标签，各自标注来源名称和置信度**（如「老水手 (可靠): 礁石区」「港口传闻 (不确定): 礁石区 + 风暴」）。玩家自行判断信哪个——亲身验证后，验证结果与该来源一致的来源获得信任提升（+25），验证结果与该来源矛盾的来源降低信任（-30，最低 0）。这创造"我选择相信老水手而不是港口传闻，结果我是对的"的玩家判断时刻——支撑 Pillar 4「未知带来温和压力」。
    - 传闻来源的置信度采用 **0–100 数值体系**，由伙伴系统或 intel 定义设定初始值。本系统消费、显示、并在验证后调整置信度。显示时按数值区间映射为文本标签：
      - 0–33：`不确定`
      - 34–66：`可靠`
      - 67–100：`权威`
    - 当来源的置信度达到 67（`权威`）后，其 `reveal_rumor()` 效果等同于可靠情报（`unknown → identified` 或 `rumored → identified`）。
    - 信任可逆——若玩家验证结果与之前信任的来源矛盾，该来源的置信度降低。同一来源可在多个不同实体上积累独立的验证/矛盾记录。
13. 传奇知识（`verified`）不可降级。`identified` 不可退回 `unknown`。`rumored` 不可退回 `unknown`。一旦知道就不可"不知道"。

**Part 4: 能力条目**

14. 每个能力是一个独立的永久解锁条目。能力解锁后永久有效，不可退化。
15. 能力解锁采用多路径设计——不同能力有不同获取方式，同一能力也可通过多种路径解锁：
    - 找到并消耗特定 intel 物品
    - 伙伴传授（特定伙伴在场且满足条件）
    - 世界事件触发（如修复特定设施后获得相关能力）
    - 规律知识确认 + 足够经验（混合路径）
16. MVP 三个能力的具体解锁路径见 Formulas 章节。
17. **能力解锁条件重评估规则**：每次发生可能满足任意能力解锁条件的状态变更时，系统必须对所有相关能力重评估 `check_unlock_conditions()`。具体触发点：
    - `consume_intel()` 处理完成后（已存在——见 consume_intel 算法规则 4）
    - `report_observation_event()` 处理完成后——可能满足 Path C/D 的观测条件
    - `report_pattern_usage_success()` 处理完成后——可能满足 Path A 的 confirmed+ 条件
    - `player_arrived_at()` 处理完成后——可能满足 Path D 的地点访问条件
    - `report_navigation_event()` 处理完成后——可能满足 Path C 的经验计数条件
    - 伙伴加入队伍时（由伙伴系统通过 `on_partner_joined(partner_id)` 通知）——可能满足 Path C/D 的伙伴条件
    - 世界修复完成时（由修复系统通过 `on_repair_completed(repair_node_id)` 通知）——可能满足 Path C 的修复条件
    重评估逻辑：对每个处于 `locked` 状态的能力调用 `check_unlock_conditions()`——若返回 true 则解锁。已解锁能力跳过检查（见边缘情况：已解锁能力的重新检查触发）。

**Part 5: 情报物品消耗**

17. 当 `consume_intel(intel_id)` 被资源系统调用时（玩家在物品栏中消耗一个情报物品），本系统：
    - 读取 intel 条目的 `linked_content_ids`，对每个关联的实体将知识状态至少推进到 `identified`（若当前为 `unknown` 或 `rumored`）。
    - 如果 intel 条目是某能力的解锁条件之一，检查该能力的完整解锁条件——若全部满足则解锁能力。
    - 返回 `IntelConsumeResult`（含推进了状态的实体列表和新解锁的能力列表）。
18. 情报物品（`supply_class=intel`）消耗后销毁——每次消耗对应一个特定情报条目的效果。同一情报条目不可重复消耗（已消耗的情报 ID 被记录，重复消耗返回 `ERR_INTEL_ALREADY_CONSUMED`）。

**Part 6: 伙伴侦察**

19. 侦察伙伴通过 `reveal_rumor(location_id, source_tag, rumor_hazards, confidence)` 接口写入知识。调用时目标实体的知识状态按 Part 3 规则推进。

**Part 7: MVP 起始状态**

20. MVP 初始知识状态：
    - 安全航线（`route.sky-reef-arc-01`）：`identified`——玩家出航前就有可靠信息（所有风险标签可见），但尚未亲身飞过。来源标注"空港基础航图"。
    - 高风险航线：`rumored`——玩家知道它存在，距离带可见，但风险标签仅部分显示。来源标注"港口传闻"。
    - 起始空港（`location.glass-harbor`）：`identified`——玩家熟悉自己的出发港。
    - 所有其他地点/航线/威胁/资源节点：`unknown`。
    - 所有规律类知识：`undiscovered`。
    - 所有能力条目：`locked`。

**Part 8: 知识伙伴身份锚点** ⚠️ Post-MVP

> **Scope note**: #15 R15.5 规定 MVP 唯一伙伴实体为 `partner.sky-cat`。以下三个伙伴身份锚点（老水手、灯塔看守后裔、制图师）及其关联的观测事件定义均为 Post-MVP 内容。MVP 中不实现这些伙伴，其观测事件（`partner_comment` 类型）在 MVP 中不可触发。

本系统涉及的三个伙伴各有一个核心身份节拍——他们不是解锁条件的布尔变量，而是有记忆、有动机、与这个世界破碎历史有关系的人。完整的角色弧线和关系记忆由 System #15（伙伴功能与关系）拥有，但本系统在机械层面依赖以下身份锚点：

| 伙伴 ID | 身份锚点 | 与知识系统的关系 |
|--------|---------|----------------|
| `partner.old-sailor` | 曾在碎裂事件前飞过完整航线网络的老水手——他记得世界曾经连通的样子，但记忆零碎且混杂着衰老带来的错乱。他不是"知道鸟往哪飞"，而是"以前在满月前总看到候鸟朝那个方向去——不确定是不是还是那条路"。 | 鸟类飞行规律——他的记忆片段是线索，不是答案。他提供 `partner_comment` 观测事件，而非直接知识。 |
| `partner.lighthouse-keeper-descendant` | 灯塔看守家族的最后一代——她的祖父曾是连接两个聚落的灯塔的看守人，碎裂事件后灯塔停止运行，家族离散。她带着半本烧毁的信号手册旅行，不是为了修复世界，而是为了找到祖父的墓。 | 灯塔信号规律——她的半本手册是 `intel.signal-codex` 的来源。修复灯塔的行为在她看来不是"解锁能力"，而是"点一盏祖父守过的灯"。 |
| `partner.cartographer` | 一个在碎裂事件后出生、从未见过完整航图的年轻制图师。她不修复旧路——她画新图。世界观与老水手形成代际对照：一个记得过去，一个只见过断的。 | 雾气穿行规律——她看待雾不是障碍而是"地图上的未知区域"。她提供 `partner_comment`，但她教的是怎么在没有航线的地方自己画线。 |

### States and Transitions

#### 规律类知识 (Pattern Knowledge) 状态表

| State | 进入条件 | 玩家可见信息 | 机械收益 | 有效转出 |
|-------|---------|-------------|---------|---------|
| `undiscovered` | 初始状态 | 规律在日志中不可见 | 无 | `partially_observed`、`confirmed`（罕见：单次高权重事件使分数同时跨越两阈值且 `pattern_usage_success` 已为 true） |
| `partially_observed` | `observation_score >= partial_threshold` | 规律名称 + 模糊提示文本（如"鸟似乎总朝某个方向飞……"） | 无 | `confirmed` |
| `confirmed` | `observation_score >= confirmation_threshold` | 完整描述文本 + 规律记录在日志中 | 激活对应**基础**机械收益（见下方 confirmed vs confirmed+ 对照表） | 无（终态——分数可继续累积，`pattern_usage_success` 可追加激活增强收益） |
| `confirmed+`（同状态增强层） | `confirmed` **且** `pattern_usage_success == true` | 完整描述文本 + 增强标注（如"已掌握"徽章） | 激活对应**增强**机械收益（见下方对照表） | 无（终态——一旦获得永不丢失） |

无效转换：`confirmed → partially_observed`、`confirmed → undiscovered`、`partially_observed → undiscovered`。

#### 地点类知识 (Location Knowledge) 状态表

| State | 进入条件 | 航图可见信息 | 有效转出 |
|-------|---------|-------------|---------|
| `unknown` | 初始状态 | 实体不可见 | `rumored`、`identified`（罕见：直接通过可靠情报揭示）、`verified`（玩家亲临——开拓者路径） |
| `rumored` | 收到传闻（`reveal_rumor()`） | 虚线轮廓 + 部分风险标签（隐藏标签显示"?"） + 来源标注 | `identified`、`verified` |
| `identified` | 消耗对应 intel 物品，或高置信度传闻 | 实体完全可见 + 全部静态风险标签 + 来源标注 | `verified` |
| `verified` | 玩家亲临/亲历该实体 | 全部信息 + 可添加个人标注 + 来源显示"亲身探索" | 无（终态） |

无效转换：`verified → *`、`identified → unknown`、`identified → rumored`、`rumored → unknown`。

#### 能力条目 (Ability Entries) 状态表

| State | 进入条件 | 玩家可见信息 | 有效转出 |
|-------|---------|-------------|---------|
| `locked` | 初始状态 | 能力名称置灰显示。解锁路径的提示文本可见（如"据说老港务长有一本信号手册……"）。能力不可使用。 | `unlocked` |
| `unlocked` | 任意解锁条件被满足 | 完整名称、图标、描述。机械效果激活。 | 无（终态） |

无效转换：`unlocked → locked`。

### Interactions with Other Systems

| 系统 | 方向 | 本系统提供 | 本系统接收 | 边界 |
|------|------|-----------|-----------|------|
| `内容数据与状态注册表` | 上游 | — | intel/pattern/ability 的静态 ID、`entry_type`、`linked_content_ids`、`source_tags`、`presentation_tier` | 注册表只提供静态定义；本系统拥有所有知识/能力运行时状态 |
| `本地存档与世界状态持久化` | 上游 | `progress.intel` 快照包（所有知识状态 + 能力解锁状态 + 已消耗情报 ID 列表 + 观测事件触发记录） | 保存调度、恢复结果 | 存档系统不解释知识语义 |
| `资源、货物与容量` | 上游 | — | `consume_intel(intel_id)` — 玩家消耗情报物品时调用 | 资源系统拥有情报物品的持有和消耗 UI；本系统处理消耗后的知识/能力解锁效果 |
| `航图与航线规划` | 下游 | `query_route_knowledge(route_id)`（含知识状态 + 可见/隐藏风险标签 + 来源）<br>`query_route_accessibility(route_id)`（含是否可通行 + 阻塞原因）<br>`query_location_discovery(location_id)` | — | 航图只读不写；本系统是展示信息的真相源 |
| `航行与路线风险` | 下游 | `query_route_knowledge()`、`query_pattern_state(pattern_id)` | 航行事件（`route_travel_completed`、`player_entered_zone`、`player_hit_obstacle`）——供规律知识检测 | 航线系统拥有遭遇生成和风险判定；本系统只消费事件用于观测检测 |
| `探索 / 搜撤场景` | 下游 | `query_location_discovery()` | 探索事件（`player_discovered_location`、`player_followed_entity`、`player_observed_signal`）——供规律知识检测和地点知识状态推进 | 探索系统拥有生成规则和撤离判定；本系统消费事件更新知识状态 |
| `世界修复与解锁` | 下游 | `query_ability_state(ability_id)`（修复设施可能解锁能力） | 修复完成事件（`repair_node_completed`）——可能触发能力解锁条件；`on_repair_completed(repair_node_id)`——通知本系统重评估能力解锁条件 | 修复系统拥有解锁语义；本系统只检查能力解锁条件是否满足 |
| `伙伴功能与关系` | 下游 | `query_pattern_state()`、`query_location_discovery()` | `reveal_rumor(location_id, source_tag, rumor_hazards, confidence)` — 伙伴侦察返回情报；`on_partner_joined(partner_id)` — 伙伴加入队伍时通知本系统重评估能力解锁条件 | 伙伴系统拥有关系状态和侦察规则；本系统提供知识写入接口 |
| `UI / HUD / 航图界面` | 下游 | `query_pattern_state()`、`query_ability_state()`、`query_route_knowledge()`、`get_pattern_log()`（图鉴日志列表） | — | UI 只读不写 |
| `玩家移动与交互` | 上游 | — | `player_arrived_at(location_id)` — 玩家到达某地点时推进地点知识状态 | 移动系统拥有到达判定；本系统消费事件更新 knowledge state |

## Formulas

### 观测分数累积 (`observation_score`)

**公式：**

```
observation_score(pattern_id) = SUM(weight(e)) for each e in triggered_events[pattern_id]
```

`triggered_events[pattern_id]` 为已触发的唯一观测事件 ID 集合。每个事件 ID 仅贡献其权重一次——同一事件的后续触发被忽略。

**变量表：**

| 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|
| `pattern_id` | string | `pattern.*` 命名空间 | 被评估的规律 |
| `e` | string | 观测事件 ID | 一个唯一的观测事件实例 |
| `weight(e)` | int | {1, 2, 3, 4, 7} | 事件 `e` 基于其 `event_type` 的权重 |
| `triggered_events[pattern_id]` | set of strings | 已定义事件的任意子集 | 对该规律至少触发过一次的事件 ID 集合 |
| `observation_score(pattern_id)` | int | 0 ~ 21（MVP 每条规律最多 6 个事件） | 累计多样性分数 |

**事件类型权重查询表：**

| event_type | 权重 | 设计说明 |
|-----------|------|---------|
| `narrative_hint` | 1 | 游戏驱动的环境提示/镜头特写。最低——非玩家主动获取。 |
| `log_fragment` | 2 | 找到的文本/日志——前人显性知识。需要探索，但信息明确。 |
| `partner_comment` | 3 | 伙伴明确指出规律。需要正确的伙伴，但无需玩家自行推断。 |
| `passive_observation` | 4 | 玩家在正常探索中自然注意到。真正的玩家-世界互动。 |
| `active_investigation` | 7 | 玩家有意识地测试/追踪/调查。最高的主动性和意图性。 |

**输出范围：** 0 到 21（MVP 每条规律 6 个观测事件，权重 1-7）。完整游戏中无上限——可添加新事件。

**计算示例——鸟类飞行规律（`pattern.bird-flight-direction`）：**

1. 首次在开放空域看到鸟群，镜头跟随 → `bird-narrative-hint`（权重 1）。分数 = 1。
2. 阅读旧研究员的鸟类迁徙笔记 → `bird-log-migration`（权重 2，类型 `log_fragment`）。分数 = 1 + 2 = 3。
3. 在新发现的岛上看到鸟类降落 → `bird-passive-island`（权重 4，类型 `passive_observation`）。分数 = 3 + 4 = 7。**→ 达到 partial_threshold（5），状态变为 `partially_observed`。**
4. 黎明时注意到多种鸟类朝同一方向飞行 → `bird-passive-migration`（权重 4，类型 `passive_observation`）。分数 = 7 + 4 = 11。**→ 达到 confirmation_threshold（10），状态变为 `confirmed`。基础收益激活：罗盘显示鸟群大致方向（±30° 模糊扇形）。**
5. 主动追踪鸟群 30+ 秒并到达新岛 → `bird-active-follow`（权重 7，类型 `active_investigation`）且 `pattern_usage_success` 设为 true。分数 = 11 + 7 = 18。**→ `is_confirmed_plus` 变为 true。增强收益激活：罗盘显示精确方向 + 距离 + 目的地类型。**

---

### 规律状态转换

**基于规则的判定（非连续公式）：**

```
next_state(pattern_id) =
    IF observation_score(pattern_id) >= confirmation_threshold
    THEN "confirmed"
    
    ELSE IF observation_score(pattern_id) >= partial_threshold
    THEN "partially_observed"
    
    ELSE "undiscovered"
```

**confirmed+ 增强层级（独立判定，不影响状态机）：**

```
is_confirmed_plus(pattern_id) =
    next_state(pattern_id) == "confirmed"
    AND pattern_usage_success[pattern_id] == true
```

`confirmed` 状态激活基础机械收益（如罗盘鸟群方向叠加层的基础版本——显示大致方向）。`confirmed+` 在同一状态下激活增强机械收益（如叠加层显示精确方向 + 距离预估 + 目的地类型图标）。`pattern_usage_success` 不再阻塞状态转换——它只区别同一状态内的收益层级。

**变量表：**

| 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|
| `observation_score(pattern_id)` | int | 0 ~ 21 | 从观测分数累积公式得出的当前累积分数 |
| `partial_threshold` | int | 4–7 | 进入 `partially_observed` 的最低分数（默认：5） |
| `confirmation_threshold` | int | 8–14 | 进入 `confirmed` 的最低分数（默认：10） |
| `pattern_usage_success[pattern_id]` | bool | {false, true} | 玩家是否至少一次成功应用了该规律？影响 `confirmed+` 增强收益，不影响状态转换 |
| `next_state(pattern_id)` | enum | {undiscovered, partially_observed, confirmed} | 结果状态 |
| `is_confirmed_plus(pattern_id)` | bool | {false, true} | 是否在 confirmed 状态中获得增强机械收益？ |

**阈值默认值（MVP 统一，可按规律覆盖）：**

| 阈值 | 默认值 | 理由 |
|------|-------|------|
| `partial_threshold` | 5 | 至少 2 种不同类型事件：如 1 条日志（2）+ 1 次被动观测（4）= 6，或 1 次伙伴（3）+ 1 次被动（4）= 7——均超过 5 |
| `confirmation_threshold` | 10 | 至少 3 种不同类型事件：如 1 条日志（2）+ 1 次被动（4）+ 1 次主动（7）= 13 > 10；或 1 次被动（4）+ 1 次伙伴（3）+ 1 次主动（7）= 14 > 10 |

**规律使用成功——机制定义：**

| 规律 ID | 成功事件 | 由谁发出 | 条件 |
|--------|---------|---------|------|
| `pattern.bird-flight-direction` | `pattern_usage_success("pattern.bird-flight-direction")` | 探索系统 | 玩家持续追随鸟实体 >= 30 秒，且在跟随结束 120 秒内抵达先前未验证的地点 |
| `pattern.lighthouse-signals` | `pattern_usage_success("pattern.lighthouse-signals")` | 航行系统 | 玩家解码灯塔闪烁规律（完成交互）并导航至隐藏航线信标 |
| `pattern.fog-navigation` | `pattern_usage_success("pattern.fog-navigation")` | 航行系统 | 玩家完成一次雾区穿越（从边界到边界），不要求 `hazard_hits == 0`——仅需成功穿越即可 |

**confirmed vs confirmed+ 收益对照：**

| 规律 ID | confirmed 基础收益（分数达标） | confirmed+ 增强收益（分数达标 + usage_success） |
|--------|------------------------------|----------------------------------------------|
| `pattern.bird-flight-direction` | 罗盘显示鸟群大致方向（±30° 模糊扇形） | 罗盘显示鸟群精确方向 + 距离预估 + 目的地类型图标 |
| `pattern.lighthouse-signals` | 航图上隐藏航线入口以虚线显示——玩家知道"这里有东西"但不知具体坐标 | 航图上隐藏航线完整可见——具体信标坐标 + 连接的聚落名称 |
| `pattern.fog-navigation` | 雾区中障碍物在接近时（≤30m）显示半透明轮廓 | 雾区中障碍物常时可见轮廓 + 风向指示器始终活跃 + 航道边界可辨识 |

**计算示例——灯塔信号规律：**
- 已触发事件：`lh-narrative-hint`（1）+ `lh-passive-compare`（4）+ `lh-log-codex`（2）+ `lh-active-decode`（7）= 14。分数 = 14 >= 10。
- 状态变为 `confirmed`。激活基础收益：隐藏航线入口以虚线显示。
- 玩家随后解码一座灯塔并导航至隐藏航线 → `pattern_usage_success` 设为 true。
- `is_confirmed_plus` 变为 true。激活增强收益：隐藏航线完整坐标和连接聚落名称显示。
- 即使玩家从未达成 `pattern_usage_success`，基础收益永久保留——理解即收益。

**计算示例——鸟类飞行规律（体现新逻辑）：**
1. 首次在开放空域看到鸟群，镜头跟随 → `bird-narrative-hint`（权重 1）。分数 = 1。
2. 阅读旧研究员的鸟类迁徙笔记 → `bird-log-migration`（权重 2）。分数 = 1 + 2 = 3。
3. 在新发现的岛上看到鸟类降落 → `bird-passive-island`（权重 4）。分数 = 3 + 4 = 7。**→ 达到 partial_threshold（5），状态变为 `partially_observed`。**
4. 黎明时注意到多种鸟类朝同一方向飞行 → `bird-passive-migration`（权重 4）。分数 = 7 + 4 = 11。**→ 达到 confirmation_threshold（10），状态变为 `confirmed`。基础收益激活：罗盘显示鸟群大致方向（±30° 模糊扇形）。**
5. 主动追踪鸟群 30+ 秒并到达新岛 → `bird-active-follow`（权重 7）+ `pattern_usage_success` 设为 true。分数 = 11 + 7 = 18。**→ `is_confirmed_plus` 变为 true。增强收益激活：罗盘显示精确方向 + 距离 + 目的地类型。**

**状态转换规则（不变量）：**
- `confirmed → partially_observed`：无效（知识不可退化）
- `confirmed → undiscovered`：无效
- `partially_observed → undiscovered`：无效
- 一旦 `pattern_usage_success` 为 true，永久保持 true。
- `pattern_usage_success` 不阻塞状态转换——玩家可在分数达标时获得 `confirmed` 状态和基础收益，独立于是否完成 usage_success。

---

### IntelConsumeResult 处理

**数据结构：**

```
IntelConsumeResult {
    success: bool
    error_code: String              // "" 或 "ERR_INTEL_ALREADY_CONSUMED"
    
    intel_id: String                // 如 "intel.bird-migration-notes"
    intel_display_name: String      // 用于 UI 反馈
    
    location_advancements: [LocationAdvancement]
    ability_unlocks: [AbilityUnlock]
    pattern_observations: [PatternObservationAdded]
}

LocationAdvancement {
    location_id: String
    previous_state: KnowledgeState  // unknown | rumored | identified | verified
    new_state: KnowledgeState
}

AbilityUnlock {
    ability_id: String
    ability_display_name: String
    unlock_path: String             // 如 "intel_consumed"
}

PatternObservationAdded {
    pattern_id: String
    event_id: String                // 如 "bird-log-migration"
    event_type: String              // "log_fragment"
    added_score: int
    new_observation_score: int
    previous_pattern_state: PatternState
    new_pattern_state: PatternState
}
```

**处理算法：**

```
function consume_intel(intel_id):
    result = new IntelConsumeResult()
    result.intel_id = intel_id
    
    // 规则 1：已消耗检查
    if intel_id in consumed_intel_ids:
        result.success = false
        result.error_code = "ERR_INTEL_ALREADY_CONSUMED"
        return result
    
    result.success = true
    intel_def = registry.lookup_intel(intel_id)
    result.intel_display_name = intel_def.display_name
    
    // 规则 2：推进关联地点知识
    for each location_id in intel_def.linked_content_ids:
        current = knowledge_state[location_id]
        if current in {unknown, rumored}:
            old_state = current
            knowledge_state[location_id] = identified
            result.location_advancements.append({
                location_id: location_id,
                previous_state: old_state,
                new_state: identified
            })
    
    // 规则 3：对关联规律添加 log_fragment 类型观测事件
    for each pattern_id in intel_def.linked_patterns:
        event_id = intel_def.pattern_event_id
        if event_id not in triggered_events[pattern_id]:
            old_score = observation_score[pattern_id]
            old_pattern_state = pattern_state[pattern_id]
            triggered_events[pattern_id].add(event_id)
            observation_score[pattern_id] = old_score + WEIGHT_LOG_FRAGMENT  // 2
            new_pattern_state = compute_pattern_state(pattern_id)
            result.pattern_observations.append({...})
    
    // 规则 4：检查能力解锁条件
    for each ability_id in intel_def.unlock_condition_for_abilities:
        if ability_state[ability_id] == locked AND check_unlock_conditions(ability_id):
            ability_state[ability_id] = unlocked
            result.ability_unlocks.append({...})
    
    // 规则 5：标记 intel 已消耗
    consumed_intel_ids.add(intel_id)
    
    return result
```

**地点知识推进规则（引用 Detailed Rules — 地点类知识）：**

| 当前状态 | 操作 | 新状态 | 说明 |
|---------|------|-------|------|
| `unknown` | `consume_intel()` | `identified` | 直接跳跃——intel 是可靠信息 |
| `rumored` | `consume_intel()` | `identified` | Intel 以可靠信息覆盖传闻 |
| `identified` | `consume_intel()` | `identified`（不变） | 已达此级别 |
| `verified` | `consume_intel()` | `verified`（不变） | 亲身经历不可覆盖 |

**计算示例——消耗"鸟类迁徙笔记"：**

Intel 定义 `intel.bird-migration-notes`：
- `linked_content_ids`: [`location.whisper-isle`, `route.bird-migration-corridor`]
- `linked_patterns`: [`pattern.bird-flight-direction`]，`pattern_event_id: "bird-log-migration"`
- `unlock_condition_for_abilities`: [`ability.bird-flight-understanding`]

消耗前：
- `location.whisper-isle`：`rumored`（之前收到过传闻）
- `route.bird-migration-corridor`：`unknown`
- `pattern.bird-flight-direction`：`undiscovered`，observation_score = 0
- `ability.bird-flight-understanding`：`locked`

消耗后 `consume_intel("intel.bird-migration-notes")`：
- `location_advancements`：`whisper-isle` rumored→identified，`bird-migration-corridor` unknown→identified
- `pattern_observations`：`bird-flight-direction` 添加 `bird-log-migration`（+2 分），新分数 = 2，状态仍为 `undiscovered`（2 < 5）
- `ability_unlocks`：`bird-flight-understanding` 收到检查——intel 消耗条件是 Path B，check_unlock_conditions 判定 Path B 已满足 → 能力解锁

**已消耗情报的处理：**
- 返回 `success = false`，`error_code = "ERR_INTEL_ALREADY_CONSUMED"`
- 所有数组（`location_advancements`、`ability_unlocks`、`pattern_observations`）均为空
- 不发生任何状态变更
- 资源系统应在允许消耗 UI 之前检查，此处为安全网

---

### 能力解锁条件

每个能力使用**跨路径 OR 逻辑**：任意单一路径完全满足即解锁能力。每条路径内使用 **AND 逻辑**：所有子条件必须满足。

**公式：**

```
ability_unlocked(ability_id) = OR(path_satisfied(p)) for each p in unlock_paths[ability_id]

path_satisfied(path) = AND(condition_met(c)) for each c in path.conditions
```

**变量表：**

| 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|
| `ability_id` | string | `ability.*` 命名空间 | 被检查的能力 |
| `unlock_paths[ability_id]` | list of paths | 每条能力 2–4 条路径 | 所有已定义的解锁路径 |
| `path.conditions` | list of conditions | 每条路径 1–3 个条件 | 单条路径内的条件 |
| `path_satisfied(path)` | bool | {false, true} | 该路径的所有条件是否均已满足？ |
| `ability_unlocked(ability_id)` | bool | {false, true} | 是否有任意一条路径被满足？ |

---

#### 能力：鸟类飞行方向理解 (`ability.bird-flight-understanding`)

**解锁路径（OR——任意一条路径满足即解锁）：**

**Path A — 规律确认：**
| 条件 | 检查方式 |
|------|---------|
| `pattern.bird-flight-direction` 状态 | `confirmed` |

**Path B — 情报消耗 + 观测：**
| 条件 | 检查方式 |
|------|---------|
| `intel.bird-migration-notes` 已消耗 | `intel_id` in `consumed_intel_ids` |
| 至少触发 1 个鸟类相关观测事件（任意类型） | `triggered_events["pattern.bird-flight-direction"]` 非空 |

**计算示例——Path B：**
- 玩家在物品栏中消耗了 `intel.bird-migration-notes`：✓
- 此前已在开放空域看到过鸟群（`bird-narrative-hint` 已触发）：✓
- 结果：Path B 满足 → 能力解锁。消耗关于鸟类迁徙的旧笔记，结合自己亲眼见过的事实，完成了理解。

**Path C — 伙伴 + 观测：**
| 条件 | 检查方式 |
|------|---------|
| 伙伴 `partner.old-sailor` 在队 | `partner.old-sailor` in active_crew |
| 至少触发 1 个鸟类被动观测事件 | `triggered_events["pattern.bird-flight-direction"]` 包含至少 1 个 `passive_observation` 类型事件 |

**计算示例——Path C：**
- 老水手在队：✓
- 玩家触发了 `bird-passive-island`（passive_observation）：✓
- 结果：Path C 满足 → 能力解锁。即使规律仍为 `undiscovered` 且 intel 未消耗。

---

#### 能力：灯塔信号解读 (`ability.lighthouse-signal-interpretation`)

**解锁路径（OR——任意一条路径满足即解锁）：**

**Path A — 规律确认：**
| 条件 | 检查方式 |
|------|---------|
| `pattern.lighthouse-signals` 状态 | `confirmed` |

**Path B — 情报消耗 + 观测：**
| 条件 | 检查方式 |
|------|---------|
| `intel.signal-codex` 已消耗 | `intel_id` in `consumed_intel_ids` |
| 至少触发 1 个灯塔相关观测事件（任意类型） | `triggered_events["pattern.lighthouse-signals"]` 非空 |

**计算示例——Path B：**
- 玩家消耗了 `intel.signal-codex`：✓
- 此前夜间靠近灯塔时已触发镜头特写（`lh-narrative-hint`）：✓
- 结果：Path B 满足 → 能力解锁。旧的信号手册 + 亲眼见过的闪烁灯塔 = 理解了信号语言。

**Path C — 世界事件（修复）：**
| 条件 | 检查方式 |
|------|---------|
| 灯塔修复完成 | `world_repair.repair_node.starlight_dock` completed |

**Path D — 伙伴 + 地点访问：**
| 条件 | 检查方式 |
|------|---------|
| 伙伴 `partner.lighthouse-keeper-descendant` 在队 | in active_crew |
| 访问 >= 1 个灯塔地点 | `location_state[loc] == verified` for at least 1 location with `has_lighthouse = true` |

**计算示例——Path C：**
- 玩家修复了玻璃港郊外的旧灯塔：✓
- 结果：Path C 满足 → 能力解锁。修复灯塔的过程教会了玩家其工作原理。

---

#### 能力：雾气穿行 (`ability.fog-navigation`)

**解锁路径（OR——任意一条路径满足即解锁）：**

**Path A — 规律确认：**
| 条件 | 检查方式 |
|------|---------|
| `pattern.fog-navigation` 状态 | `confirmed` |

**Path B — 情报消耗 + 观测：**
| 条件 | 检查方式 |
|------|---------|
| `intel.fog-compass-manual` 已消耗 | `intel_id` in `consumed_intel_ids` |
| 至少触发 1 个雾气相关观测事件（任意类型） | `triggered_events["pattern.fog-navigation"]` 非空 |

**计算示例——Path B：**
- 玩家消耗了 `intel.fog-compass-manual`：✓
- 此前首次接近雾区时镜头展示了雾的浓度分层（`fog-narrative-hint`）：✓
- 结果：Path B 满足 → 能力解锁。

**Path C — 经验路径（反复尝试）：**
| 条件 | 检查方式 |
|------|---------|
| 成功穿越雾区 | `count(fog_zone_traversal_completed events) >= 3`（完成从边界到边界的穿越即计——不要求 `hazard_hits == 0`） |

**Path D — 伙伴 + 观测：**
| 条件 | 检查方式 |
|------|---------|
| 伙伴 `partner.cartographer` 在队 | in active_crew |
| 触发 >= 2 个雾气相关观测事件 | `triggered_events["pattern.fog-navigation"]` 包含至少 2 个任意类型事件 |

**计算示例——Path C：**
- 玩家艰难穿越 3 次雾区，每次都受了些损毁但最终成功穿越。第 3 次成功时：
- `fog_traversal_success_count = 3 >= 3`：✓
- 结果：Path C 满足 → 能力解锁。最具挑战的路径——但也是最具成就感的。

**计算示例——Path D：**
- 制图师在队：✓
- 触发事件：`fog-narrative-hint`（1 个）+ `fog-passive-wind`（1 个）= 2 个事件 >= 2：✓
- 结果：Path D 满足 → 能力解锁。制图师帮玩家把零散观测拼成有用知识。

---

### 完整观测事件权重表（MVP）

#### 规律：`pattern.bird-flight-direction`

| 事件 ID | event_type | 权重 | 说明 | 触发条件 |
|---------|-----------|------|------|---------|
| `bird-narrative-hint` | `narrative_hint` | 1 | 镜头跟随向一致方向飞行的鸟群 | 玩家首次进入有鸟实体的开放空域 |
| `bird-log-migration` | `log_fragment` | 2 | 旧研究员的笔记描述鸟类向岛屿的迁徙规律 | 消耗 intel `intel.bird-migration-notes` |
| `bird-partner-sailor` | `partner_comment` | 3 | 老水手伙伴评论"鸟儿总是知道陆地在哪" | `partner.old-sailor` 在队，鸟实体首次进入视野 |
| `bird-passive-island` | `passive_observation` | 4 | 玩家注意到鸟类降落在刚发现的岛上 | 玩家发现新岛屿，鸟实体在屏幕上可见 |
| `bird-passive-migration` | `passive_observation` | 4 | 黎明/黄昏时玩家注意到多种鸟类朝同一方向飞行 | 玩家在黎明/黄昏时处于开放空域，>= 2 种鸟类可见 |
| `bird-active-follow` | `active_investigation` | 7 | 玩家主动追随鸟群并到达目的地 | 持续保持与鸟实体 <= 100m 距离 >= 30 秒，且在 120 秒内到达某地点 |

**总分上限：** 1 + 2 + 3 + 4 + 4 + 7 = 21

#### 规律：`pattern.lighthouse-signals`

| 事件 ID | event_type | 权重 | 说明 | 触发条件 |
|---------|-----------|------|------|---------|
| `lh-narrative-hint` | `narrative_hint` | 1 | 镜头推近夜间闪烁的灯塔 | 首次夜间（游戏内时间）接近任意灯塔地点 |
| `lh-log-codex` | `log_fragment` | 2 | 找到并阅读旧灯塔看守的信号手册 | 消耗 intel `intel.signal-codex` |
| `lh-partner-keeper` | `partner_comment` | 3 | 灯塔看守后裔解释"闪烁规律是一种语言" | `partner.lighthouse-keeper-descendant` 在灯塔附近 |
| `lh-passive-compare` | `passive_observation` | 4 | 玩家注意到两座灯塔闪烁节奏不同 | 玩家访问 >= 2 个不同灯塔地点（均 verified 或 identified） |
| `lh-passive-hidden` | `passive_observation` | 4 | 玩家注意到灯塔光束指向看似空旷的天空 | 夜间从灯塔光束指向隐藏航线的角度观测（探索系统检测） |
| `lh-active-decode` | `active_investigation` | 7 | 玩家通过互动成功解码灯塔闪烁规律 | 玩家对灯塔使用观察/互动操作并成功记录闪烁时序 |

**总分上限：** 1 + 2 + 3 + 4 + 4 + 7 = 21

#### 规律：`pattern.fog-navigation`

| 事件 ID | event_type | 权重 | 说明 | 触发条件 |
|---------|-----------|------|------|---------|
| `fog-narrative-hint` | `narrative_hint` | 1 | 首次接近雾区时镜头展示雾的浓度分层 | 首次接近任意雾型区域 |
| `fog-log-manual` | `log_fragment` | 2 | 找到旧雾航手册 | 消耗 intel `intel.fog-compass-manual` |
| `fog-partner-cartographer` | `partner_comment` | 3 | 制图师伙伴解释"雾有层次——跟着更清晰的那层走" | `partner.cartographer` 在队首次进入雾区 |
| `fog-passive-wind` | `passive_observation` | 4 | 玩家注意到雾中风向可见/一致 | 在雾区内风向指示器 UI 活跃 >= 10 秒 |
| `fog-passive-sound` | `passive_observation` | 4 | 玩家注意到雾中障碍物附近声音回声不同 | 在雾区内距离障碍物 <= 50m（音频提示触发） |
| `fog-active-navigate` | `active_investigation` | 7 | 玩家完成雾区穿越 | 从雾区边界到边界穿越完成（不要求 `hazard_hits == 0`——仅需成功穿越）。若为首次成功穿越，同时触发 `pattern_usage_success` |

**总分上限：** 1 + 2 + 3 + 4 + 4 + 7 = 21

**注意——`fog-active-navigate` 与 Path C 经验解锁的关系：** `fog-active-navigate` 事件贡献于规律的 observation_score（用于规律确认）。若为首次成功穿越，同时设置 `pattern_usage_success`（用于 confirmed+ 增强收益）。Path C 能力解锁的穿越次数单独计数（不要求 `hazard_hits == 0`——见雾气穿行能力解锁条件）。observation_score、usage_success 和能力解锁计数器是三组独立的逻辑。

---

### 情报已消耗检查

**公式：**

```
is_intel_consumed(intel_id) = intel_id ∈ consumed_intel_ids
```

**变量表：**

| 符号 | 类型 | 范围 | 说明 |
|------|------|------|------|
| `intel_id` | string | `intel.*` 命名空间 | 被检查的情报物品 |
| `consumed_intel_ids` | set of strings | 随时间增长 | 所有已消耗的情报 ID |
| `is_intel_consumed(intel_id)` | bool | {false, true} | 该情报是否已消耗？ |

**计算示例：**
- `consumed_intel_ids` = {`intel.bird-migration-notes`, `intel.skyreef-warning`}
- `is_intel_consumed("intel.bird-migration-notes")` → `true`
- `is_intel_consumed("intel.signal-codex")` → `false`

**行为：** 资源系统应在允许消耗 UI 之前检查此项。若已消耗：物品被销毁但无效（安全网："这份情报你已经掌握了"）。

---

### 公式到系统的映射总结

| 公式 | 被谁消费 | 由谁产出 |
|------|---------|---------|
| `observation_score` 累积 | 规律状态转换、UI（图鉴日志显示） | 来自探索、航行、伙伴、交互系统的观测事件触发 |
| 规律状态转换 | 所有规律状态查询（UI、能力解锁检查、gameplay 系统） | `observation_score` 累积 + `pattern_usage_success` 事件 |
| `IntelConsumeResult` 处理 | 资源系统（`consume_intel` 的调用方） | `consume_intel()` 处理过程 |
| 能力解锁条件 | 所有能力状态查询（UI、航行、探索 gameplay 检查） | 规律状态、intel 消耗、世界事件、伙伴状态 |
| 观测事件权重表 | `observation_score` 计算 | 每条规律静态定义；由 gameplay 事件触发 |
| 情报已消耗检查 | 资源系统（消耗 UI 前）、consume_intel 算法（安全检查） | `consume_intel()` 追加到已消耗集合 |

### 调优范围总结

| 调优参数 | 安全范围 | 默认值 | 影响 |
|---------|---------|-------|------|
| `narrative_hint` 权重 | 1–2 | 1 | 较高使游戏提示更具影响力 |
| `log_fragment` 权重 | 1–3 | 2 | 较高使文本发现更有价值 |
| `partner_comment` 权重 | 1–4 | 3 | 较高使伙伴选择更重要 |
| `passive_observation` 权重 | 2–5 | 4 | 较高奖励自然探索 |
| `active_investigation` 权重 | 4–9 | 7 | 较高奖励主动调查 |
| `partial_threshold`（可按规律覆盖） | 4–7 | 5 | 较低 = 更快显示提示；较高 = 需要更多证据 |
| `confirmation_threshold`（可按规律覆盖） | 8–14 | 10 | 较低 = 更快获得机制奖励；较高 = 需要更多证据 |
| 鸟类 Path C 观测需求 | 1–2 | 1 | 更多观测 = 更难伙伴路径 |
| 灯塔 Path D 访问需求 | 1–3 | 1 | 更多访问 = 更难伙伴路径 |
| 雾气 Path C 穿越需求 | 2–5 | 3 | 更多穿越 = 更难经验路径 |
| 雾气 Path D 观测需求 | 1–3 | 2 | 更多观测 = 更难伙伴路径 |

## Edge Cases

### 规律类知识边缘情况

**1.1 观测分数溢出**
- **情况**：玩家触发的观测事件数量超出了系统为某规律定义的所有事件。
- **处理**：`observation_score` 仅对 `triggered_events` 集合中的唯一事件 ID 累加权重。未定义的新事件 ID 将被忽略——不参与累加，不报错。系统记录 warning 日志："unregistered observation event [event_id] for pattern [pattern_id]——event ignored"。

**1.2 已确认规律继续接收观测事件**
- **情况**：规律已达 `confirmed` 终态后，新的观测事件继续触发。
- **处理**：`triggered_events` 集合继续追加新事件，`observation_score` 继续累加。状态不再变更（已是终态），但分数可持续增长以备将来使用（如成就系统查询"满分规律数量"）。不产生机械性新收益。

**1.3 pattern_usage_success 被设置但分数未达阈值**
- **情况**：玩家通过世界事件或其他路径在分数不足时触发了 `pattern_usage_success`。
- **处理**：`pattern_usage_success` 被持久化标记为 true，但 `is_confirmed_plus` 仍为 false（因为 `confirmed` 状态的前置条件未满足——`observation_score < confirmation_threshold`）。一旦分数达到阈值，状态转为 `confirmed`，同时 `is_confirmed_plus` 立即变为 true——因为 `pattern_usage_success` 已持久化为 true。这完全支持"先会用，后理解"的玩家路径：玩家先通过实践掌握了规律应用，后续理论观察补齐后自动获得增强收益。

**1.4 下游系统发送畸形事件 ID**
- **情况**：探索或航行系统传入的事件 ID 为空字符串、null，或格式不符合约定。
- **处理**：本系统做防御性校验——无效事件 ID 被静默丢弃，记录 error 日志。不崩溃，不污染 `triggered_events` 集合。下游系统应在其自身逻辑中保证事件 ID 合法性，此处为安全网。

**1.5 规律无观测事件定义**
- **情况**：某规律在注册表中存在，但未定义任何观测事件（配置错误）。
- **处理**：该规律的状态永久停留在 `undiscovered`。`observation_score` 始终为 0。系统在启动时输出 validation warning："pattern [pattern_id] has zero observation events defined——will never progress"。不阻塞游戏。

### 地点类知识边缘情况

**2.1 对已验证地点写入传闻**
- **情况**：伙伴侦察返回了一个玩家已经亲身验证过的地点的传闻（`reveal_rumor()` 对 `verified` 实体调用）。
- **处理**：按 Detailed Rules 地点类知识规则，`verified` 胜出一切。传闻被静默丢弃——不修改知识状态、风险标签、来源标注。伙伴系统可收到返回值表明"该地点已知"，用于伙伴对话分支（如"这个我们不是去过了吗？"）。

**2.2 对未知地点直接亲临**
- **情况**：玩家在没有任何传闻或情报的情况下偶然抵达了某个完全未知的地点。
- **处理**：地点知识状态直接从 `unknown` 跳转为 `verified`（该路径在状态表中明确允许）。来源标注为"亲身探索"。这是"开拓者"幻想的实现——空白航图上你第一个写下它的名字。

**2.3 传闻冲突——两个来源给出不同风险标签**
- **情况**：NPC A 说某航线安全，NPC B 说同一条航线危险。
- **处理**：按 Detailed Rules 传闻冲突解决规则——同时保留双方来源，各自标注来源名称和置信度，两份风险标签分别显示在航图上（用不同标记区分）。玩家自行判断信哪个来源。若玩家亲身验证后结果与某来源一致，该来源的后续 rumor 置信度自动提升一档。传闻不会"出错"——它们只是不同人对同一片天空的不同记忆。

**2.4 同一传闻来源重复写入**
- **情况**：`reveal_rumor()` 以相同的 `source_tag` 和 `location_id` 被调用两次。
- **处理**：如果目标实体已处于 `rumored` 或更高状态：检测到重复 source_tag → 不更新状态，不追加重复来源标注。返回状态表明"已存在该来源的传闻"。

**2.5 所有风险标签均隐藏**
- **情况**：某个 `rumored` 状态的地点，其所有风险标签都被标记为隐藏（`hazard_visibility = hidden`）。
- **处理**：航图显示该实体为虚线轮廓，风险区域显示为 `????`。对玩家而言这是"完全未知的危险"——知道有东西在那里，但不知道是什么。

### 能力条目边缘情况

**3.1 所有解锁路径均不可达**
- **情况**：某能力的全部解锁路径中的条件都因游戏进度变得不可能满足（例如要求的伙伴已永久离开、要求的 intel 物品所在区域已不可达）。
- **处理**：本系统不主动检测"永久不可解锁"——这一判断属于设计/QA 范畴而非运行时逻辑。运行时：能力保持 `locked`。设计约束：每条能力必须保证至少一条解锁路径在 MVP 游戏流程中可达。

**3.2 多条解锁路径同时满足**
- **情况**：一次行动同时满足 Path B（intel 路径）和 Path A（规律路径）。
- **处理**：系统使用 OR 逻辑——第一条被检测到满足的路径即解锁能力。`ability_unlocks` 结果中的 `unlock_path` 字段标注为第一个被检测到的路径。多条路径同时满足不产生重复解锁或冲突。

**3.3 已解锁能力的重新检查触发**
- **情况**：能力已处于 `unlocked` 终态，但后续事件触发 `check_unlock_conditions()`。
- **处理**：在检查之前先判断当前状态——若已为 `unlocked`，直接跳过所有解锁条件检查。避免不必要的计算。

**3.4 伙伴在检查时间点的原子性**
- **情况**：能力解锁条件检查中对伙伴在场的判断基于单一时刻的快照——在检查和实际解锁之间不存在伙伴离队的窗口（同一帧内完成）。
- **处理**：解锁检查使用原子快照：在 `check_unlock_conditions()` 被调用的那一帧内，所有伙伴状态被一次性读取。如果能力因此解锁，即使该伙伴在下一帧离开，能力保持 `unlocked`。

### 情报物品边缘情况

**4.1 消耗不存在的 intel ID**
- **情况**：资源系统调用 `consume_intel()` 传入了一个注册表中不存在的 `intel_id`。
- **处理**：返回 `success = false`，`error_code = "ERR_INTEL_NOT_FOUND"`。所有数组为空。记录 error 日志。资源系统应在其自身层面保证只对已识别的情报物品开放消耗 UI，此处为安全网。

**4.2 Intel 的 linked_content_ids 为空**
- **情况**：情报物品定义中 `linked_content_ids` 为空数组（配置错误或此 intel 为纯叙事/纯能力相关）。
- **处理**：`location_advancements` 保持为空——不报错。算法仅在有内容 ID 时进行地点推进。此设计允许"纯叙事情报"（只影响规律观测或能力解锁，不揭示地点）。

**4.3 单次消耗触发多重效果**
- **情况**：一次 `consume_intel()` 同时推进了地点知识、添加了规律观测事件、且解锁了能力——三重效果。
- **处理**：算法按顺序（规则 2→3→4）依次处理。`IntelConsumeResult` 的三个数组全部被填充。这是正常且预期的——表示该 intel 物品信息密度高，设计上应属于稀有/后期物品。

**4.4 consume_intel 的 linked_content_ids 中的地点 ID 未初始化**
- **情况**：`consume_intel()` 算法访问 `knowledge_state[location_id]` 时，该 ID 尚未被初始化为任何知识状态（例如注册表中存在但从未被任何操作写入过，或 ID 拼写错误）。
- **处理**：算法访问 `knowledge_state[location_id]` 前先检查键是否存在——若不存在（返回 null/undefined），将其初始化为 `unknown`，然后继续正常的 `{unknown, rumored} → identified` 推进逻辑。记录 warning 日志："location_id [id] not found in knowledge_state — initializing to unknown"。此行为同时覆盖了注册表不同步和拼写错误的情况，不崩溃。

**4.5 情报物品在消耗前被销毁/丢失**
- **情况**：玩家持有情报物品，但在消耗之前物品因某原因被销毁（例如飞艇舱室损毁事件）。
- **处理**：本系统无责任——我们只在 `consume_intel()` 被调用时响应。物品丢失不产生对本系统的调用，因此不发生任何状态变更。"持有情报物品"的状态维护完全在资源系统中。

### 跨系统与存档边缘情况

**5.1 存档恢复——intel ID 不存在于当前注册表**
- **情况**：存档中记录的 `consumed_intel_ids` 包含当前游戏版本中已重命名或删除的情报 ID。
- **处理**：存档恢复时，对 `consumed_intel_ids` 中的每个 ID 与注册表做交叉验证。不存在的 ID 被保留在集合中（不静默删除——以防该 ID 在未来版本重新引入），但对 `is_intel_consumed()` 查询返回 true。防止玩家在新版本中重新消耗"同一份"情报。

**5.2 存档恢复——规律/能力 ID 已变更**
- **情况**：已保存的 `observation_score`、`triggered_events`、`pattern_usage_success`、`ability_state` 引用了当前版本中不存在的 ID。
- **处理**：存档恢复时，对每个已保存的知识/能力 ID 与注册表做交叉验证。不存在的 ID 产生 migration warning 但保留原始数据——不静默删除。UI 查询不存在的 ID 时返回安全的默认值（`undiscovered` / `locked`）。

**5.3 全部能力已解锁后的系统行为**
- **情况**：游戏中所有定义的能力条目都处于 `unlocked` 状态。
- **处理**：系统继续正常运行——所有查询接口如常返回状态。`consume_intel()` 仍然推进地点知识和规律观测，只是 `ability_unlocks` 数组保持为空。系统不依赖于"有可解锁内容"作为运行前提。

**5.4 观测事件在存档/读档期间触发**
- **情况**：观测事件在存档过程中被触发（极端罕见）。
- **处理**：观测事件的接收和处理在同一帧内完成（同步调用，无延迟队列）。存档系统保存的是该帧结束后的完整快照。不会出现"事件触发了但未保存"的情况——除非存档本身在事件触发之前被截断，这属于存档系统的边界。

## Dependencies

### 上游依赖（本系统依赖它们）

#### #1 `内容数据与状态注册表` — 已批准 GDD
- **本系统需要**：intel/pattern/ability 的静态定义（`entry_type`、`linked_content_ids`、`source_tags`、`presentation_tier`、`hazard_tags`、`pattern_event_id`）
- **合约**：注册表提供只读静态查询接口。本系统拥有所有知识/能力运行时状态。
- **合约检查**：`consume_intel()` 和所有查询接口在首次引用 ID 时验证注册表中存在对应条目。不存在则返回 `ERR_INTEL_NOT_FOUND` / 安全的默认状态。
- **对端 GDD 中的反向引用**：注册表 GDD 的"Interactions"表格已列出本系统为其下游消费者。

#### #2 `本地存档与世界状态持久化` — 已批准 GDD
- **本系统需要**：`progress.intel` 快照包的保存调度和恢复。
- **合约**：快照包包含 `domain_id = "intel"`，payload 含所有知识状态 + 能力解锁状态 + 已消耗情报 ID 列表 + 观测事件触发记录 + `pattern_usage_success` 标记。
- **合约检查**：恢复时对 payload 中的 ID 与注册表做交叉验证（见边缘情况：存档恢复-id 变更）。
- **对端 GDD 中的反向引用**：存档系统 GDD 的 snapshot package 定义已预留 `progress.intel` 域。

#### #3 `资源、货物与容量` — 已批准 GDD
- **本系统需要**：`consume_intel(intel_id)` 调用入口、`get_carried_intel()` 查询接口。
- **合约**：资源系统拥有情报物品的持有和消耗 UI；本系统处理消耗后的知识/能力解锁效果。资源系统应在允许消耗前检查 `is_intel_consumed()`。
- **合约检查**：本系统返回 `IntelConsumeResult`，资源系统据此更新物品栏和 UI。若返回 `success=false`，资源系统不销毁物品。
- **对端 GDD 中的反向引用**：资源系统 GDD 的合约已写明"情报系统拥有已知/未知状态；本系统只负责情报物品的持有和消耗"。
- **函数所有权澄清**：`consume_intel()` 由本系统定义和实现（算法见 Formulas — IntelConsumeResult 处理）。资源系统的职责是提供物品栏入口、验证物品存在、调用本系统的 `consume_intel()`、并根据返回的 `IntelConsumeResult` 更新 UI 和物品栏状态。不存在双重所有权——本系统拥有算法和状态变更；资源系统拥有物品栏操作和 UI 入口。

#### #11 `玩家移动与交互` — 已批准 GDD
- **本系统需要**：`player_arrived_at(location_id)` 事件——玩家到达某地点时调用。
- **合约**：移动系统拥有到达判定；本系统消费事件，将对应地点的知识状态推进到 `verified`。
- **合约检查**：若 `location_id` 不在注册表中，记录 warning 日志但不报错（可能是动态生成的地点）。
- **对端 GDD 中的反向引用**：移动系统 GDD 的 Interactions 表格和下游契约已包含 `player_arrived_at(location_id)` 事件及对本系统的引用（2026-04-30 修订）。

### 下游依赖（这些系统依赖本系统）

#### #4 `航图与航线规划` — Approved (2026-05-02)
- **下游需要**：`query_route_knowledge(route_id)`（知识状态 + 可见/隐藏风险标签 + 来源）、`query_route_accessibility(route_id)`（是否可通行 + 阻塞原因，阻塞原因可能关联到未解锁能力）。
- **合约**：本系统是航图展示信息的真相源。航图只读不写。若路线未注册（`unknown`），航图不显示该路线。
- **预期接口（待 GDD 确认）**：`get_all_known_routes()`、`get_all_known_locations()`——用于航图初始渲染时的批量查询。
- **潜在冲突**：航线可通行性取决于能力解锁状态——航图系统需要知道"因为没解锁灯塔信号解读能力，所以这条隐藏航线不可见"或"不可通行原因 = 需要灯塔信号解读"。此逻辑在本系统的 `query_route_accessibility()` 中返回，航图系统只消费结果。
- **对端 GDD 中的反向引用**：#9 GDD Dependencies 节已双向标注本系统为数据源。

#### #5 `航行与路线风险` — Approved (2026-05-02)
- **下游需要**：`query_route_knowledge()`、`query_pattern_state(pattern_id)`。
- **上游提供**：航行事件（`route_travel_completed`、`player_entered_zone`、`player_hit_obstacle`、`player_navigated_fog_zone`）——供规律知识检测和观测事件触发。
- **合约**：航线系统拥有遭遇生成和风险判定；本系统只消费事件用于观测检测和能力解锁计数。
- **对端 GDD 中的反向引用**：#10 GDD Dependencies 节已双向标注本系统为数据源。

#### #7 `探索 / 搜撤场景` — Approved (2026-05-03)
- **下游需要**：`query_location_discovery()`。
- **上游提供**：探索事件（`player_discovered_location`、`player_followed_entity`、`player_observed_signal`）——供规律知识检测和地点知识状态推进。
- **合约**：探索系统拥有生成规则和撤离判定；本系统消费事件更新知识状态。
- **对端 GDD 中的反向引用**：#11 GDD Dependencies 节已双向标注本系统为数据源。

#### #9 `世界修复与解锁` — Approved (2026-05-04)
- **下游需要**：`query_ability_state(ability_id)`——修复特定设施的行为可能解锁能力。
- **上游提供**：修复完成事件（`repair_node_completed`）——可能触发能力解锁条件检查。
- **合约**：修复系统拥有解锁的叙事语义；本系统只检查能力解锁条件是否满足。修复事件本身不直接解锁能力——它只是一条路径中的一个条件。
- **对端 GDD 中的反向引用**：#13 GDD Dependencies 节已双向标注本系统为数据源。

#### #15 `伙伴功能与关系` — 已批准 GDD (2026-05-02)
- **下游需要**：`query_pattern_state()`、`query_location_discovery()`——伙伴可能根据玩家已知信息改变对话或行为。
- **上游提供**：`reveal_rumor(location_id, source_tag, rumor_hazards, confidence)`——伙伴侦察返回情报（MVP 中 confidence 上限 66）；`report_observation_event(pattern_id, event_type)`——伙伴的 `partner_comment` 观测事件；`on_partner_joined(partner_id)`——伙伴加入队伍时通知本系统重评估能力解锁条件。
- **合约**：伙伴系统拥有关系状态和侦察规则；本系统提供知识写入接口（`reveal_rumor`）和查询接口。MVP 仅 `partner.sky-cat` 一个伙伴实体（#15 R15.5），#6 Part 8 的三个伙伴身份锚点为 Post-MVP 内容。
- **对端 GDD 中的反向引用**：#15 GDD 已双向标注本系统为下游（Interactions 表）。#15 的 Cross-GDD Revision Flags（Flags 1+2）已识别本节的过期引用。

#### UI / HUD / 航图界面 — Designed (2026-05-03)
- **下游需要**：`query_pattern_state()`、`query_ability_state()`、`query_route_knowledge()`、`get_pattern_log()`（图鉴日志列表）、`get_ability_list()`（所有能力及其解锁状态）。
- **合约**：UI 只读不写。本系统提供所有展示所需的查询接口。
- **对端 GDD 中的反向引用**：#16 GDD Dependencies 节已双向标注本系统为数据源。

### 下游系统接口汇总

| 接口 | 签名 | 返回 | 消费方 |
|------|------|------|--------|
| `query_route_knowledge(route_id)` | String → RouteKnowledge | 知识状态 + 可见风险标签 + 来源标注 | 航图系统、航行系统 |
| `query_route_accessibility(route_id)` | String → RouteAccessibility | 可通行性 + 阻塞原因（关联到未解锁能力 ID） | 航图系统、航行系统 |
| `query_location_discovery(location_id)` | String → LocationKnowledge | 知识状态 + 风险标签可见性 + 来源 + 个人标注 | 航图系统、探索系统、伙伴系统 |
| `query_pattern_state(pattern_id)` | String → PatternState | undiscovered / partially_observed / confirmed + observation_score | 航行系统、探索系统、伙伴系统、UI |
| `query_ability_state(ability_id)` | String → AbilityState | locked / unlocked | 世界修复系统、航行系统、UI |
| `get_pattern_log()` | () → Array<PatternLogEntry> | 所有已进入 partially_observed 及以上状态的规律列表（含名称、状态、描述片段） | UI / 图鉴日志 |
| `get_ability_list()` | () → Array<AbilityEntry> | 所有能力的名称、解锁状态、解锁路径提示文本 | UI |

### 上游系统事件接收接口汇总

| 接口 | 调用方 | 载荷 | 触发时机 |
|------|--------|------|---------|
| `consume_intel(intel_id)` | 资源系统 | intel ID | 玩家消耗情报物品 |
| `reveal_rumor(location_id, source_tag, hazard_tags, confidence)` | 伙伴系统 | 地点 ID + 来源 + 风险标签 + 置信度 | 伙伴侦察返回 |
| `on_partner_joined(partner_id)` | 伙伴系统 | 伙伴 ID | 伙伴加入队伍时通知本系统重评估能力解锁条件 |
| `player_arrived_at(location_id)` | 移动系统 | 地点 ID | 玩家到达某地点 |
| `report_observation_event(pattern_id, event_id)` | 探索/航行/伙伴/交互系统 | 规律 ID + 事件 ID | gameplay 事件触发 |
| `report_pattern_usage_success(pattern_id)` | 探索/航行系统 | 规律 ID | 玩家成功应用规律 |
| `report_navigation_event(event_type, payload)` | 航行系统 | 事件类型 + 载荷 | 航行事件发生 |

## Tuning Knobs

### 观测事件权重

| 参数 | 安全范围 | 默认值 | 影响的 Gameplay 层面 |
|------|---------|-------|---------------------|
| `weight_narrative_hint` | 1–2 | 1 | 环境提示的推动力——设置过高会让玩家觉得"系统帮我太多了"，削弱自主发现的成就感 |
| `weight_log_fragment` | 1–3 | 2 | 文本发现物作为证据来源的价值——设置过高会促使玩家刷文本而忽略实际观察 |
| `weight_partner_comment` | 1–4 | 3 | 伙伴对知识进度的贡献度——设置过高会削弱 solo 玩家的可行性，设置过低会让带伙伴出行感觉不到益处 |
| `weight_passive_observation` | 2–5 | 4 | 自然探索的奖励感——核心调优参数。影响"我偶然注意到了什么"的发生频率和满足感 |
| `weight_active_investigation` | 4–9 | 7 | 主动调查的回报——最重要的高权重参数。影响"我刻意去验证一个假设"的动机强度 |

### 规律状态阈值

| 参数 | 安全范围 | 默认值 | 影响的 Gameplay 层面 |
|------|---------|-------|---------------------|
| `partial_threshold` | 4–7 | 5 | 模糊提示首次出现在日志中的时机——影响玩家从"完全不知道"到"隐约怀疑有什么规律"之间的时间窗口长度。太低：还没注意到规律就被提示了。太高：玩家已经自己看出来了但系统还不承认 |
| `confirmation_threshold` | 8–14 | 10 | 机制奖励激活的时机——影响玩家从"隐约怀疑"到"确认掌握"之间的跨度。太低：规律发现变成快速跑图。太高：玩家感觉被系统卡着不给确认 |
| `per_pattern_threshold_override` | 覆盖上述默认值 | 无（按规律可选） | 允许特定规律设置不同的阈值——例如核心主线规律可降低阈值确保玩家推进，而隐藏的彩蛋规律可提高阈值增加神秘感 |

**硬性约束**：`partial_threshold` 必须严格小于 `confirmation_threshold`。系统启动时应验证此约束——若违反（如设计师误将 partial 设为 12 而 confirmation 设为 10），`partially_observed` 状态将永久不可达。启动时检测到违反应输出 error 级别日志并拒绝进入 gameplay。

### 规律使用成功判定

| 参数 | 安全范围 | 默认值 | 影响的 Gameplay 层面 |
|------|---------|-------|---------------------|
| `bird_follow_duration_seconds` | 20–60 | 30 | 追踪鸟群所需持续跟随时间——影响"主动追踪"的操作难度。太低：随便路过就算追踪成功。太高：操作繁琐 |
| `bird_follow_arrival_window_seconds` | 60–300 | 120 | 追踪结束后允许抵达地点的时间窗口——影响事件判定对"追踪导致了发现"的因果关系置信度 |
| `fog_traversal_success_definition` | {any_completion, low_hits, zero_hits} | `any_completion` | 雾区穿越「成功应用」的定义——`any_completion` = 从边界到边界完成穿越即算（默认）。`low_hits` = hazard_hits <= 2。`zero_hits` = 完美穿越。影响 pattern_usage_success 和 confirmed+ 增强收益的获得难度 |

### 能力解锁条件

| 参数 | 安全范围 | 默认值 | 影响的 Gameplay 层面 |
|------|---------|-------|---------------------|
| `bird_obs_required_path_c` | 1–2 | 1 | 鸟类能力 Path C（伙伴路径）所需的被动观测次数——影响带老水手出行时解锁能力的速度 |
| `lighthouse_visits_required_path_d` | 1–3 | 1 | 灯塔能力 Path D（伙伴路径）所需的灯塔访问次数——影响灯塔解锁伙伴路径的门槛 |
| `fog_traversal_success_required_path_c` | 2–5 | 3 | 雾气能力 Path C（经验路径）所需的穿越次数（从边界到边界完成即计，不要求完美）。影响"硬核自学"路径的长度。2 = 快速。5 = 需要大量探索 |
| `fog_obs_required_path_d` | 1–3 | 2 | 雾气能力 Path D（伙伴路径）所需的观测事件次数——影响带制图师出行时解锁能力的速度 |

### 传闻与风险显示

| 参数 | 安全范围 | 默认值 | 影响的 Gameplay 层面 |
|------|---------|-------|---------------------|
| `rumor_conflict_display_mode` | {labeled_sources, union_blend, trusted_only} | `labeled_sources` | 传闻冲突时的风险标签显示策略——`labeled_sources` = 同时显示所有来源的风险标签，各自标注来源名称和置信度（玩家自行判断）。`union_blend` = 显示所有来源最坏情况的并集。`trusted_only` = 仅显示最高信任来源的标签 |
| `rumor_max_sources_per_entity` | 2–5 | 无上限（实际由伙伴/任务设计控制） | 单个实体可保留的传闻来源数量上限——防止 UI 来源标注溢出 |
| `confidence_initial_default` | 20–60 | 由来源定义（伙伴/intel），无全局默认值 | 新来源首次提供传闻时的初始置信度数值。例如：老港务长初始 55，港口流言初始 25 |
| `confidence_gain_on_verification_match` | 15–35 | 25 | 玩家亲身验证结果与某来源的传闻一致时，该来源置信度增加值 |
| `confidence_loss_on_verification_contradiction` | 20–40 | 30 | 玩家亲身验证结果与某来源的传闻矛盾时，该来源置信度减少值 |
| `confidence_authority_threshold` | 60–80 | 67 | 置信度达到此值的来源被视为"权威"——其 `reveal_rumor()` 效果等同于可靠情报 |

### 不可调的设计钢印

以下行为不可调——它们定义了系统的核心玩家承诺：

- 规律知识一旦达到 `confirmed` 不可退回 `partially_observed` 或 `undiscovered`
- 地点知识一旦达到 `verified` 不可降级到任何其他状态
- 能力一旦 `unlocked` 不可退回 `locked`
- 观测事件一旦触发，其贡献永久有效（不因时间流逝或玩家死亡而丢失）

## Visual/Audio Requirements

### 规律观测的视觉提示

- 首次触发 `narrative_hint` 事件时：镜头缓慢推近目标实体（鸟群/灯塔/雾层），1.5–2 秒特写后恢复。不打断玩家操作——纯视觉叠加。
- `passive_observation` 事件触发时：屏幕边缘短暂（0.5 秒）闪烁对应规律的图标提示色（鸟类=暖黄、灯塔=冷蓝、雾气=灰白），不弹文字。
- `active_investigation` 事件触发时：屏幕中央出现半透明图标 + 短音效确认（0.3 秒），随后渐隐。
- `pattern_usage_success` 触发时：图鉴日志图标闪烁，提示新记录已写入。

### 航图视觉状态

- `unknown` 实体：不渲染。
- `rumored` 实体：虚线轮廓（2px 虚线，透明度 60%），隐藏风险标签显示为 `?` 闪烁动画。
- `identified` 实体：实线轮廓，完整标签，来源标注以小字附在实体名称下方。
- `verified` 实体：实线 + 微光边缘（暖金色），个人标注以手写体风格显示。

### 能力解锁时刻

- 能力解锁时：全屏短暂（1 秒）暗角 + 中央显示能力名称 + 图标放大动画（参考 Zelda BOTW 获得关键物品的 UI 风格但更低调）。
- 配套音效：上升的、完成的音色（非战斗/警报风格——更接近"领悟"的感觉）。

### 音频提示

- 触发任何观测事件时：轻柔的"注意"音（类似笔记本翻页的声响）——表示"有什么值得记住的"。
- 规律达到 `confirmed` 时：完整的乐句片段（2–3 秒），每条规律有独特的旋律片段（鸟类=木管、灯塔=钟铃、雾气=弦乐泛音）。

## UI Requirements

### 图鉴日志（Pattern Log）

- 入口：航图界面或暂停菜单中的"图鉴"标签。
- 列表视图：每条规律一行——名称 + 状态图标（? = undiscovered/hidden、🔍 = partially_observed、✓ = confirmed）。
- 详情视图：完整描述文本 + 已触发的观测事件清单（含事件类型图标）+ 当前 observation_score / 满分。
- `undiscovered` 规律不在列表中显示——保持神秘感。

### 能力列表（Ability List）

- 入口：与图鉴日志同级的"能力"标签。
- 列表视图：每条能力一行——图标 + 名称。
- `locked` 状态：置灰图标 + 置灰名称 + 解锁路径提示文本（如"据说老港务长有一本信号手册……"）。
- `unlocked` 状态：彩色图标 + 名称 + 简短效果描述。

### 航图知识叠加

- 航线：颜色编码——绿色（安全/已验证）、黄色（部分已知）、红色（高风险且信息不全）。
- 地点：图标编码——实心圆（verified）、空心圆（identified）、虚线圆（rumored）。
- 风险标签：小图标行显示在实体名称下方。
- 来源标注：悬浮 tooltip 或点击实体后在详情面板中显示。

### 情报物品消耗 UI

- 物品栏中选择情报物品 → "阅读/研究"按钮（非"使用"——叙事包装）。
- 消耗确认弹窗："确定要研读 [intel 名称] 吗？研读后物品将被消耗。"
- 消耗后：IntelConsumeResult 的三个数组依次在 UI 中以动画展示（新地点揭示 → 新观测记录 → 新能力解锁）。

## Acceptance Criteria

### AC-1: 规律知识——观测事件触发与分数累积

**AC-1.1** 当玩家首次进入有鸟实体的开放空域时，镜头对鸟群做跟随特写，且 `pattern.bird-flight-direction` 的 `observation_score` 增加 1（`narrative_hint` 权重）。

**AC-1.2** 同一观测事件触发第二次时，`observation_score` 不再增加——日志可验证 `triggered_events` 集合不包含重复事件 ID。

**AC-1.3** 消耗 `intel.bird-migration-notes` 后，`pattern.bird-flight-direction` 的 `observation_score` 增加 2（`log_fragment` 权重），事件 `bird-log-migration` 被记录。

**AC-1.4** 当 `observation_score >= 5`（partial_threshold）时，规律名称和模糊提示文本出现在图鉴日志中，状态变为 `partially_observed`。无机械性收益（如罗盘无鸟群方向叠加层）。

**AC-1.5** 当 `observation_score >= 10`（confirmation_threshold）时，无论 `pattern_usage_success` 是否为 true，状态变为 `confirmed`，完整描述记录在日志中。基础机械收益激活（如罗盘鸟群大致方向模糊扇形——±30°）。可验证：状态值 = `confirmed`，`query_pattern_state()` 返回 `confirmed`。

**AC-1.6** 当 `observation_score >= 10` 且 `pattern_usage_success == true` 时，状态为 `confirmed` 且 `is_confirmed_plus == true`。增强机械收益激活（如罗盘鸟群精确方向 + 距离预估 + 目的地类型图标）。可验证：`query_pattern_state()` 返回 `confirmed` 且 `is_confirmed_plus == true`。

**AC-1.7** 当 `observation_score >= 10` 且 `pattern_usage_success == false` 时，`is_confirmed_plus == false`。基础收益激活，增强收益未激活。随后若 `pattern_usage_success` 变为 true（如玩家追踪鸟群找到新地点），`is_confirmed_plus` 立即变为 true 且增强收益激活。

### AC-2: 规律知识——不可退化

**AC-2.1** 已 `confirmed` 的规律在玩家死亡、重新加载存档、或离开重进区域后仍保持 `confirmed`。

**AC-2.2** 已 `partially_observed` 的规律不会退回 `undiscovered`——即使长时间未触发该规律的新观测事件。

**AC-2.3** 当某规律的 `pattern_usage_success == true` 但 `observation_score < confirmation_threshold`（如分数 7，状态 `partially_observed`）时，`is_confirmed_plus == false`。随后当 `observation_score` 通过新观测事件累积至 `>= confirmation_threshold` 时，状态变为 `confirmed` 且 `is_confirmed_plus`**自动**变为 true——不需要再次触发 usage_success。

### AC-3: 地点知识——状态推进

**AC-3.1** 新游戏开始时，安全航线 `route.sky-reef-arc-01` 在航图上完全可见，所有风险标签显示，来源标注为"空港基础航图"——状态为 `identified`。

**AC-3.2** 高风险航线在航图上以虚线轮廓显示，风险标签仅部分显示（隐藏标签显示 `?`），来源标注为"港口传闻"——状态为 `rumored`。

**AC-3.3** 调用 `reveal_rumor(location_id, source_tag, ...)` 后，`unknown` 的目标地点变为 `rumored`，出现虚线轮廓和来源标注。

**AC-3.4** 消耗与某 `unknown` 或 `rumored` 地点关联的 intel 后，该地点的知识状态变为 `identified`，所有静态风险标签显示。

**AC-3.5** 当 `player_arrived_at(location_id)` 被调用且目标地点的知识状态为 `unknown`、`rumored` 或 `identified` 时，该地点的知识状态变为 `verified`，来源标注为"亲身探索"。

**AC-3.5b** 当 `player_arrived_at(location_id)` 被调用且目标地点已经是 `verified` 时：a) 状态保持 `verified`；b) 不重复触发状态变更事件/日志；c) 个人标注和来源保持不变。

**AC-3.6** 玩家无任何先验信息直接抵达 `unknown` 地点时，状态从 `unknown` 直接跳转 `verified`（跳过 `rumored` 和 `identified`）。

**AC-3.7** 当 `reveal_rumor(location_id, source_tag, hazard_tags, confidence >= 67)` 被调用且目标地点当前为 `unknown` 时，该地点知识状态直接变为 `identified`（跳过 `rumored`），实体在航图上完全可见，所有静态风险标签显示。

**AC-3.8** 当 `reveal_rumor(location_id, source_tag, hazard_tags, confidence >= 67)` 被调用且目标地点当前为 `rumored` 时，该地点知识状态变为 `identified`，传闻来源标注被保留，风险标签被可靠情报替换。

### AC-4: 地点知识——不可降级

**AC-4.1** `verified` 地点收到新传闻（`reveal_rumor()`）后，状态保持 `verified`，风险标签和来源标注不变。

**AC-4.2** `identified` 地点收到低置信度传闻后，状态不退回 `rumored`。

### AC-5: 传闻冲突

**AC-5.1** 当两个不同来源对同一实体的风险给出不同标签时，航图同时显示两个来源标注，每个来源的风险标签独立显示且标注来源名称和置信度（如「老水手 (可靠): 礁石区」「港口传闻 (不确定): 礁石区 + 风暴」）。两份风险标签并排显示，玩家自行判断信哪个来源。

**AC-5.2** 同一来源（相同 `source_tag`）对同一实体重复写入传闻时，不追加重复来源标注。

### AC-6: 能力——多路径解锁

**AC-6.1** 鸟类飞行方向理解——Path A：`pattern.bird-flight-direction` 变为 `confirmed` 后，能力自动解锁。

**AC-6.2** 鸟类飞行方向理解——Path B：消耗 `intel.bird-migration-notes` 且此前已触发至少 1 个鸟类相关观测事件（如 `bird-narrative-hint`）后，能力解锁。若 intel 已消耗但从未触发过任何鸟类观测事件——能力保持 `locked`。

**AC-6.3** 鸟类飞行方向理解——Path C：老水手在队 + 触发至少 1 次鸟类被动观测事件后，能力解锁（即使规律仍为 `undiscovered`）。

**AC-6.4** 灯塔信号解读——Path C：修复 `repair_node.starlight_dock` 后，能力解锁（即使从未观察过灯塔规律）。

**AC-6.5** 灯塔信号解读——Path D：灯塔看守后裔在队 + 访问至少 1 个灯塔地点（verified）后，能力解锁。

**AC-6.6** 雾气穿行——Path C：累计 3 次成功穿越雾区（从边界到边界完成穿越即计——不要求 `hazard_hits == 0`）后，能力解锁。单次失败穿越（未到达对侧边界即退出）不计入。

**AC-6.7** 雾气穿行——Path D：制图师在队 + 触发 >= 2 个雾气观测事件后，能力解锁。

### AC-7: 能力——不可退化

**AC-7.1** 已解锁的能力在玩家死亡、重新加载存档、或伙伴离队后保持 `unlocked`。

**AC-7.2** 能力已解锁后，后续事件（如消耗关联 intel、再次满足解锁条件）不会导致：a) `ability_state` 发生变更（保持 `unlocked`）；b) 重复触发解锁动画/UI 通知；c) 系统日志中记录 error 或 warning 级别日志。

### AC-8: 情报消耗

**AC-8.1** `consume_intel("intel.bird-migration-notes")` 返回 `success = true`，且 `location_advancements` 包含所有从 `unknown`/`rumored` 推进到 `identified` 的地点。

**AC-8.2** 重复调用 `consume_intel("intel.bird-migration-notes")` 返回 `success = false`，`error_code = "ERR_INTEL_ALREADY_CONSUMED"`，所有数组为空。

**AC-8.3** 消耗已消耗 intel 时，不发生任何状态变更——`consumed_intel_ids`、`knowledge_state`、`observation_score` 均不变。

**AC-8.4** 消耗不存在的 intel ID 时，返回 `error_code = "ERR_INTEL_NOT_FOUND"`，不崩溃。

**AC-8.5** 消耗任意 intel 物品时，`IntelConsumeResult.pattern_observations` 数组包含该 intel 定义中 `linked_patterns` 对应的所有规律观测记录。每条记录包含正确的 `event_id`、`event_type`（为 `"log_fragment"`）、`added_score`（为 2）、以及更新后的 `new_observation_score` 和 `new_pattern_state`。

### AC-9: 存档/读档完整性

**AC-9.1** 存档后立即读档：所有 `observation_score`、`triggered_events`、`pattern_usage_success`、`knowledge_state`、`ability_state`、`consumed_intel_ids` 与存档前完全一致。

**AC-9.2** 读档后，`confirmed` 规律的机械性收益（如罗盘叠加层）正常激活。

**AC-9.3** 读档后，`verified` 地点的个人标注保留。

### AC-10: MVP 起始状态

**AC-10.1** 新游戏：所有规律类知识为 `undiscovered`，图鉴日志为空。

**AC-10.2** 新游戏：所有能力条目为 `locked`，能力列表显示置灰名称和提示文本。

**AC-10.3** 新游戏：除初始航线（`identified`）和高风险航线（`rumored`）、起始空港（`identified`）外，所有其他实体为 `unknown`。

## Intel Supply Model (MVP)

> 此节是对 economy-designer 审查中"情报供给未定义"问题的回应。完整的情报获取规则由探索系统和伙伴系统 GDD 拥有，本节仅定义 MVP 规模和约束。

| 参数 | 值 | 说明 |
|------|-----|------|
| MVP 情报物品总数 | 5–7 个 | 3 个能力相关 intel（必得，放在关键探索点或叙事节拍中）+ 2–4 个可选地点揭示 intel（可由伙伴侦察或随机探索点产出） |
| 能力相关 intel 的获取保证 | 必得（非随机） | `intel.bird-migration-notes`、`intel.signal-codex`、`intel.fog-compass-manual` 放置在玩家必经或大概率访问的内容节点——不允许因 RNG 导致玩家完全错过能力解锁 |
| 可选地点揭示 intel 的获取 | 随机或条件触发 | 不影响核心能力解锁，仅加速地点知识推进。允许玩家因选择不同而看到不同 intel |
| 单个 intel 的预期效果密度 | 中等 | 大部分 intel 只推进地点知识（`linked_content_ids`）。少数"核心叙事情报"可同时推进地点 + 添加规律观测事件——但不解锁能力（能力解锁需要额外满足观测条件——见能力解锁条件 Path B） |
| intel 物品的容量成本 | light (50 容积, 1 重量) | 参照 resources-goods-capacity.md。10 个 light 物品占据 500 容积——这是有意的设计压力："带补给还是带情报"是一个真实选择 |

## Open Questions

1. **完整游戏的规律总数**：MVP 为 3 条规律。完整游戏预计多少条？10–15 条可覆盖主要探索/航行领域吗？
2. **能力总数**：MVP 为 3 条能力。完整游戏中能力数量上限？是否会有"能力树"或"能力进阶"（如雾气穿行 Lv.1 → Lv.2 可穿越更浓的雾）？
3. **图鉴日志的美术风格**：是纯文本/图标风格，还是每条规律配有手绘插图？（取决于美术资源预算）
4. **个人标注系统**：`verified` 地点的"个人标注"是自由文本输入还是从预设标签中选择？自由文本带来更多个性化但增加本地化/审核/存档体积成本。
5. **规律知识与其他 progression 系统的关系**：规律确认是否影响主线推进（如某主线任务需要确认某规律后才能继续）？如果影响，哪些规律是"主线门控"？
6. **观测事件的冷却时间**：同一规律的不同观测事件之间是否需要最小时间间隔？防止玩家在短时间内刷满所有事件。
7. **死后知识保留**：当前设计为知识永久不退化。如果引入"死亡惩罚"机制，知识系统是否参与？（当前设计：不参与——知识是永久积累。）
8. **多存档槽的知识共享**：不同存档的知识进度完全独立，还是有一个"账号级"的规律知识集合？（建议：完全独立——每个存档是独立的旅程。）
