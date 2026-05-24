# 航行与路线风险

> **Status**: Approved (CD-GDD-ALIGN: APPROVE WITH NOTES — 1 blocker resolved)
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-09
> **Implements Pillar**: 规划先于冒险; 未知带来温和压力
> **Platform Pivot Note**: ADR-0019 supersedes browser timer/lifecycle assumptions. Active voyage implementation targets desktop Godot .NET/C#; tab-throttle edge cases should be read as desktop pause/resume timing requirements.

## Overview

航行与路线风险是《云海织航》的航行阶段核心系统——它接收航图系统（#9）在出航确认后发出的 `route_committed` 事件（玩家直接出航），并在未来扩展中接收来自空港/村镇系统（#14）的委托货运请求（NPC 航线与玩家委托路线），读取航线的静态风险标签与对应的飞艇/船队状态，在航行过程中将风险标签逐步解析为具体的遭遇上下文（EncounterContext），并将航行结果输出至下游系统。

在数据层，它是一个风险解析引擎：消费静态的 authored 风险标签（`safe` / `storm` / `low-visibility` / `pirate_activity` 等）和遭遇表，结合航行主体的当前状态（玩家出航时：侦察模块效率影响风险预见范围、船体完整性波段决定航行惩罚系数、动力炉能量/燃料决定可航行最远距离；NPC/委托航线时：航线稳定度、世界修复状态等），产出结构化的 EncounterContext 供下游消费。在体验层，它是玩家在航图上做出选择后真正"飞出去"的旅程——航行不是一段过场动画，而是一个风险逐步展开的过程：玩家在航图上看到一条航线的距离带和风险标签，基于飞艇当前的燃料储备判断"我能飞多远"、基于船体状态预估"我能承受多少风险"、基于侦察模块判断"我能提前看到多少"，然后带着这些判断出发。航程中风暴来临、视野收缩、侦察模块提前标注的礁石区浮现——每一个风险标签变成真实的遭遇，最终要么安全抵达目的地，要么在半途因燃料不足或船体严重受损而被迫折返。

没有这个系统，航图上的风险标签就只是标签——永远不会变成航行中的紧张、判断和后果；燃料容量和航线距离就只是两个无关的数字——永远不会产生"这条航线太远，我必须减重或先修船"的规划取舍。"规划先于冒险"（Pillar 1）会在出航确认后断裂——玩家做了准备，但没有一个系统让准备被考验。"未知带来温和压力"（Pillar 4）会因缺少航行中的压力兑现而失效——传闻航线的 `?` 标记永远不会揭晓它代表的到底是什么。

**架构预留**：本系统的风险解析核心设计为与航行主体类型无关（玩家飞艇 / NPC 船队 / 玩家委托货船），消费统一的 `VoyageContext`（航线 ID + 目的地 ID + 风险标签 + 航行主体状态快照），产出统一的 `EncounterContext`。MVP 仅实现玩家直接出航路径；NPC 航线与玩家委托货运路线属于 Phase 3+ 扩展。

**当前 demo 航行场景要求**：`route.sky-reef-arc-01` 与 `route.storm-cut-01` 不应被实现成两个彼此割裂的按钮流程或独立小过场。它们共享一个“航行大场景”：玩家从初始岛屿离港后进入同一片可读的大世界航行空间，再根据航线状态抵达雾灯残骸或旧集市边缘。该场景不是全自由开放世界，也不是物理飞行模拟；它是以固定视角和可控朝向为核心的伪 3D 航行场景。

**明确不在 MVP 范围内**：探索点的具体内容生成（由探索/搜撤场景 #11 消费 EncounterContext 后实现）；战斗的具体规则和结算（由战斗与威胁处理 #12 实现）；NPC 船队的具体贸易逻辑和路线生成（由 #14 定义，本系统仅提供风险解析服务）；真实物理飞行模拟。航行表现层不再视为纯 UI/HUD 附属物：当前 demo 需要一个独立的航行大场景来承载固定航道的玩家体验，具体场景完整性由 #19 / #20 门禁验收。

## Player Fantasy

航行与路线风险服务的核心幻想是：**你是一个在不确定天空里做出冷静判断的船长——你的准备在航行中被持续考验，你的每一个判断都在书写航志。**

### 航海家的判断（主基调）

航行中的紧张不来自"能不能活下来"，而来自"我有没有准备好"。风暴来临时玩家感受的不是被动恐惧，而是"我带的额外燃料够不够绕过这片风暴区"的判断时刻。视野收缩时压力不在"什么都看不见"，而在"侦察模块标注的三格之外全是未知——我该继续深入，还是折返？"

这个幻想的锚定时刻有两个面：

**成功的判断**：航行中风暴云墙迫近，侦察模块提前标注出雷暴核心区。你调整航向绕行，船体擦过乱流边缘但未进入风暴中心。燃料消耗比预期多，但仍在安全范围内。你穿过风暴带边缘后回头看了一眼——那片云墙在航图上曾经只是一个深灰色的 `?` 标记，现在你知道里面是什么了。返航后，那条航线会在你的海图上从虚线变成实线。

**撤退的判断——与成功同样重要**：船体完整性从绿色波段跌入黄色，仪表发出低频警报。前方侦察模块显示还有未知空域，燃料指针已在倒数位置。你看着航线终点在地图边缘发光，然后你做出了最重要的决定：折返。你没有到达目的地，但你的船还活着，你知道了一个新的撤退基准点。下一次你会带双倍的燃料，或者先修好船体的那道裂痕。

每次航行结束后的感受不是"我运气好"，而是"我准备得好"或"我这次准备不足，但我知道了问题在哪里"——这是 Pillar 1（规划先于冒险）在航行中的战斗层面兑现。

### 未知的温柔闭合（第二层）

航行是传闻变成经历的过程。航图上的每一个 `?` 在航行中揭晓它的真实身份——可能是风暴、可能是平静空域、可能是一片从未有人踏足的岛屿群。压力不来自危险本身，而来自"不知道"——航行结束后的奖赏是"现在我知道了"。

这是 Pillar 4（未知带来温和压力）的正面兑现：未知的压力是温和的——它是谜题，不是威胁。每一次航行都让世界变得更可读、更可预测、更可信任。一条曾经只有传闻的航线，在亲身飞过后变成了航图上的实线——你把它从"有人说是这样"变成了"我知道是这样"。

### 船长的孤航（暗线）

航行中只有你和你的飞艇。船体在风暴中发出的每一声响动你都熟悉，仪表板上的每一格燃料衰减你都感受得到。航行不是外部事件的发生，而是飞艇与你之间的私密对话。每一次新的伤痕都是共享记忆——你记得那道裂痕是哪个航段留下的，就像水手记得每一道伤疤的来历。

这条暗线不喧哗——它渗透在船体伤痕、燃料消耗和模块效率变化的感官反馈中。不需要语言表达，每一次仪表变化都在提醒"这是你的船，它正在和你一起经历这段航程"。

### 参考感受

参考老航海日志的书写传统——平实、有重量、不煽情。不是"史诗冒险"的高亢叙事，而是船长在航海日志里写下的冷静记录："本日航程：穿越风暴带边缘，船体轻微受损，燃料消耗超预期两格，侦察模块标注新礁石区一处。航线部分确认。"

航行结束后，玩家感受的不是"我征服了这片天空"，而是"我对这片天空的了解又多了一点，我的船陪我扛过了这一程，下一次我会准备得更好。"

### 航行大场景的视角幻想

航行时，玩家不是看一个平面地图上的图标滑动，而是站在飞船前进方向所对准的视角里。镜头始终和飞船当前前进方向保持一致：飞船向前，世界从远处压近并向两侧掠过；飞船拐弯，云层、航标、远岛轮廓和风险物围绕视线方向重新排列；飞船后退，世界运动方向反转，但玩家仍理解自己是在操控同一艘船的朝向。

表现效果的重点是“世界在变化”。飞船本体可以作为驾驶舱边缘、船首轮廓、仪表或前景结构被看见，但运动感主要由世界层级提供：近景云雾快速掠过，中景航标和漂浮残骸按透视缩放，远景岛屿和风暴墙缓慢移动。航行大场景应让玩家感到自己在穿过空海，而不是在操作菜单。

## Detailed Design

### Core Rules

**航行大场景表现**

0. 当前 demo 的固定航道共享一个航行大场景。航线数据仍由本系统、航图和注册表拥有，但玩家可见的航行体验必须被 #19 作为场景验收，而不是只由 UI 进度条证明。
1. 视角永远与飞船前进方向保持一致。转向时，镜头/世界层级重新对齐到新航向；不得使用与飞船朝向脱节的上帝视角来替代核心航行体验。
2. 飞船可以前进、拐弯和后退。前进/后退的读法通过世界层级运动方向、速度、透视缩放和前景船体/仪表反馈表达。
3. 伪 3D 表现不等于真实 3D 物理。实现可以使用 2D 层级、缩放、视差、sprite 变形、路径采样和镜头约束，但必须让玩家读出航道、风险物、转向、目的地接近和撤退方向。
4. 两条 demo 航道应在同一大场景内共享基础天空、云海、航标、远景和风险物语言；差异来自航线风险、目的地方向、环境密度和抵达岛屿轮廓。
5. 航行大场景必须记录自己的 Scene Physics Contract 或等价 #20 设计：移动平面、碰撞/风险边界、可见风险物、遮挡层级、尺度、特殊表面（云雾/风暴/漂浮残骸）和恢复规则。
6. 航行大场景的第一版核心乐趣是导航判断，其次是驾驶手感和氛围旅程。玩家应通过观察前方问题、判断风险类型、选择规避或解决方式、承担代价或获得知识来推进航程。
7. 航行遭遇战第一版不包含主动开火。当前 demo 的遭遇压力以飞行大鸟生态避险为主，必须通过规避、借遮蔽、减速、硬抗或撤退处理；主动武器、瞄准、射击、击毁敌人属于后续开发范围。

**航行问题与非射击遭遇**

16. 航行问题分为环境问题、生态压迫问题和后续对抗问题。环境问题考验航路判断；生态压迫问题考验移动中的应变；后续对抗问题也必须服务航行，不应把场景切换成独立战斗房间。

| 问题类型 | 玩家可读信号 | 第一版解决方式 | 失败或代价 |
| --- | --- | --- | --- |
| 浓雾带 | 远景消失、航标弱化、云层压近 | 减速穿越、绕行、消耗侦察扫描 | 迷航、错过目标、航程变长 |
| 漂浮残骸群 | 近景残骸快速接近，存在可穿缝隙 | 转向穿缝、后退重找角度、低速通过 | 船体损伤、航向偏移 |
| 飞行大鸟临时避险 | 大鸟剪影靠近、翼振增强、航标短暂被遮住 | 借云雾/残骸遮蔽、改航向、降速等待、撤退 | 船体晃动、航向偏移、被迫改道 |

17. 第一版生态压迫问题的默认解法不应奖励“击毁目标”，而应奖励读航路和控船：利用云层/浓雾避险、利用残骸遮蔽、牺牲速度换安全，或在风险过高时撤退。空盗拦截和主动开火均为后续内容。

18. 航行大场景使用阶段编排而非纯随机刷题。安全短线（雾灯残骸）应是 60-75 秒的教学航行，包含 1 个主问题和 1 个轻背景问题；风险中线（旧集市边缘）应是 110-140 秒的完整航行，包含 2 个主问题和 1 个轻背景问题。具体阶段、处理窗口和撤退阈值以 `production/scene-specs/voyage-open-world-scene.md` 为当前 #19 场景规格来源。
19. 第一版核心问题为浓雾带、漂浮残骸群、飞行大鸟临时避险。风暴边缘、失准航标、空盗拦截和失控无人浮标保留为后续问题池；#10 的数据表不得把第一版问题降级为不可预读的随机扣血。

**航行启动**

1. 航图系统（#9）发出 `route_committed(route_id, destination_id, hazard_tags)` 事件后，本系统接管控制权。出航锁定（`base_lock_duration` 2.0s）结束后，航行阶段开始。
2. 航行启动时，本系统构建 `VoyageContext`：
   - 从航图事件读取：`route_id`、`destination_id`、`hazard_tags`
   - 从模块系统（#8）查询：侦察模块效率 `η_scout`、船体完整性波段 `hull_band`、最大载重 `M_max`、当前载重 `M_loaded`
   - 从情报系统（#6）查询：`query_route_knowledge(route_id)` 获取知识状态和隐藏/可见风险标签
   - 从注册表（#1）读取：航线静态数据（距离带、所有静态风险标签）
3. 航行总时长由距离带和船体波段共同决定（见 Formula 1）。

**时间推进与遭遇检查**

4. 航行以时间推进——不是分段制，而是连续计时。玩家看到航行进度条从 0% 推进到 100%。
5. 每隔 `encounter_check_interval`（默认 12 秒），系统执行一次遭遇检查：
   - 系统从当前可见的风险标签集合中抽取（若标签被情报系统标记为隐藏，则使用 rumored 的方差规则——见规则 9）
   - 系统基于风险标签查询遭遇表，产出本次遭遇的 `EncounterEntry`（遭遇类型、基础伤害量、特殊效果标记）
   - 遭遇在进度条上以标记点显示——已发生的显示为已解析图标，即将发生的（在侦察窗口内）显示为预警图标
6. 船体完整性波段影响遭遇检查的频率偏移：`damaged` 波段遭遇间隔缩短 10%，`critical` 波段缩短 20%——船越破，飞得越慢，遭遇越密集。

**侦察模块信息作用**

7. 侦察模块提供**纯信息预览**——不提供绕行或减免能力。侦察效率 `η_scout` 决定玩家能提前多少秒看到即将发生的遭遇：
   - `η_scout = 1.0`（完好）：`scout_preview_window = 24s`（提前约 2 个遭遇检查周期看到预警）
   - `η_scout = 0.6`（受损）：`scout_preview_window = 12s`（提前约 1 个周期）
   - `η_scout = 0.95`（未检查）：`scout_preview_window = 12s`（`N_preview = ⌊0.95 × 2⌋ = 1`，见 Formula 3）
   - 无侦察模块：`scout_preview_window = 0s`——遭遇在发生时才知道
8. 侦察预览在航行 UI 中以进度条前方的半透明图标显示——让玩家在遭遇发生前就知道"前方有什么"，可以提前做心理准备和撤退判断。但如果风险标签本身对玩家是隐藏的（rumored 且未揭示），侦察预览仅显示 `?` 标记——你知道前方有东西，但不知道是什么。

**风险标签解析**

9. 每条航线的遭遇表是一个 `hazard_tag → EncounterEntry[]` 的映射。当遭遇检查触发时：
   - 系统收集该航线所有 `visible_hazard_tags`（情报系统中非隐藏的标签）和 `hidden_hazard_tags`（情报系统中隐藏的标签）
   - 对每个可见标签：从其遭遇表中等概率抽取一个 `EncounterEntry`
   - 对每个隐藏标签：先判定是否出现在本次检查中（基于 `rumor_reveal_chance`，默认 30%），若出现则从其遭遇表中抽取
   - 多个标签同时命中时，取伤害最高的遭遇（而非叠加）——一次遭遇检查只产出一次遭遇
10. 隐藏标签在首次被揭露后（在航行中实际遇到），航行结束后该标签在情报系统中从隐藏变为可见——航图下次显示时将显示该标签。

**撤退**

11. 玩家在航行中**任何时刻**可以触发撤退（按 Esc 或点击撤退按钮）。撤退不是失败——它是船长的判断。
12. 撤退结算：
    - 保留已获得的资源/货物（来自已发生的遭遇中搜刮到的内容——但 MVP 中遭遇不直接产出货物，此条为架构预留）
    - 保留已揭示的风险标签信息（隐藏标签在遭遇中被揭露的，保持可见）
    - 船体承受已发生遭遇的全部伤害（不减免）
    - 航程状态标记为 `retreated`，航线知识状态不推进到 `verified`——只有成功抵达才能验证航线
13. 撤退后，航行日志记录撤退点："航程中断于 [进度百分比]——[撤退原因]。航线部分确认。下次建议：[基于当前船体/侦察状态的建议]。"

**航行结束**

14. 航行结束有三种路径：
    - **抵达**：进度条到达 100% → `VoyageState.ARRIVED`。航线知识状态推进至 `verified`。向情报系统发出 `route_travel_completed` 事件。
    - **撤退**：玩家主动触发 → `VoyageState.RETREATED`。航线知识状态保持当前级别。向情报系统发出 `route_travel_completed(status=retreated)` 事件。
    - **迫降**：船体完整性降至 0 或以下 → `VoyageState.FORCED_LANDING`。航线知识状态不推进。船体伤痕 +1。向情报系统和模块系统发出损伤事件。

**MVP 两条航线配置**

15. MVP 遭遇表定义。当前 demo 中，两条航线都进入同一个 `voyage_open_world_scene`，区别在目的地方向、风险表现和抵达岛屿：安全短线抵达雾灯残骸，风险中线抵达旧集市边缘。既有 route id 可保留为数据 ID，但 player-facing destination 必须按当前 demo 场景名呈现。

| 航线 ID | 当前 demo 目的地 | 距离带 | 航行时长 | 遭遇检查次数 | 可见风险标签 | 隐藏风险标签 |
|--------|--------|--------|---------|------------|------------|------------|
| `route.sky-reef-arc-01` | 雾灯残骸 | short | 60s | 5 次 | `safe` | 无 |
| `route.storm-cut-01` | 旧集市边缘 | medium | 120s | 10 次 | `storm` | `low-visibility` |

**安全线遭遇表（`safe`）：**
| EncounterEntry | 概率 | 伤害量 | 特殊效果 |
|---------------|------|--------|---------|
| `calm_passage` | 40% | 0 | 无 |
| `gentle_crosswind` | 35% | 0 | 轻微减速（+5s 总时长） |
| `minor_debris` | 20% | 1-2 | 无 |
| `scenic_discovery` | 5% | 0 | 揭示一个航段中的小地标（叙事内容） |

**风险线遭遇表（`storm`）：**
| EncounterEntry | 概率 | 伤害量 | 特殊效果 |
|---------------|------|--------|---------|
| `storm_cell_edge` | 30% | 1-3 | 轻微减速 |
| `turbulence_zone` | 25% | 2-4 | 速度降低 15% 持续至下一检查 |
| `lightning_proximity` | 20% | 3-6 | 概率击中侦察模块（20% → module damaged） |
| `wind_shear` | 15% | 1-2 | 下一次遭遇检查提前 5s |
| `storm_eye_passage` | 10% | 0 | 揭示此航段所有隐藏标签 |

**风险线隐藏标签遭遇表（`low-visibility`）：**
| EncounterEntry | 概率 | 伤害量 | 特殊效果 |
|---------------|------|--------|---------|
| `dense_fog_bank` | 40% | 0 | 下一次遭遇检查的侦察预览窗口减半 |
| `hidden_reef_proximity` | 35% | 2-4 | 无预警（绕过侦察） |
| `false_horizon` | 25% | 0 | 航行剩余时间估算偏差（显示比实际多/少 15%） |

### States and Transitions

**航行状态机**

| 状态 | 含义 | 进入条件 | 有效转出 |
|------|------|---------|---------|
| `VOYAGE_PREPARING` | 构建 VoyageContext，查询上游系统，验证适航条件 | `route_committed` 事件接收 | `IN_PROGRESS`、`ABORTED_PREFLIGHT` |
| `IN_PROGRESS` | 航行进行中——计时器运行，遭遇检查周期触发，玩家可撤退 | VoyageContext 构建完成，所有查询通过 | `ARRIVED`、`RETREATED`、`FORCED_LANDING` |
| `ARRIVED` | 安全抵达目的地 | 航行计时器到达 `total_duration` | 无（终态——控制权移交至探索系统或空港系统） |
| `RETREATED` | 玩家主动撤退 | 玩家在 `IN_PROGRESS` 期间触发撤退 | 无（终态——控制权移交回飞艇 Hub） |
| `FORCED_LANDING` | 船体完整性降至 0，被迫降落 | `hull_integrity <= 0` 在 `IN_PROGRESS` 期间触发 | 无（终态——控制权移交至紧急修复流程） |
| `ABORTED_PREFLIGHT` | 航行前检查失败 | `can_depart()` 返回 `{false, reasons}` 或上游查询失败 | 无（终态——返回航图 ERROR 状态） |

**无效转换**：
- `ARRIVED → *`、`RETREATED → *`、`FORCED_LANDING → *`、`ABORTED_PREFLIGHT → *`：所有终态不可逆转
- `IN_PROGRESS → IN_PROGRESS`：无操作转换，不产生效果

**航程内部子状态**：
在 `IN_PROGRESS` 期间，系统内部追踪：
- `elapsed_time`：已过航行秒数
- `last_check_time`：上一次遭遇检查的时间戳
- `pending_encounters`：侦察模块已预览但尚未触发的遭遇列表
- `resolved_encounters`：已完成结算的遭遇列表
- `accumulated_damage`：累计未结算的船体伤害（在航行结束后统一写入 #8）

### Interactions with Other Systems

**上游（本系统从中读取）**

| 系统 | 读取内容 | 接口 | 使用时机 |
|------|---------|------|---------|
| #9 航图与航线规划 | `route_committed` 事件 | 信号 | 航行启动 |
| #1 内容数据与状态注册表 | 航线静态数据（距离带、所有风险标签） | `list_by_kind("route")` | 航行启动 |
| #8 飞艇模块与船体状态 | 侦察模块效率 `η_scout`、船体完整性波段及当前值、`can_depart()` | 查询接口 | 航行启动 + 每次遭遇检查前 |
| #6 玩家知识与情报 | 航线知识状态、可见/隐藏风险标签、来源标注 | `query_route_knowledge()` | 航行启动 + 每次遭遇检查 |

**下游（本系统向其写入/发出信号）**

| 系统 | 写入/发出内容 | 接口/时机 | 消费目的 |
|------|-------------|---------|---------|
| #8 飞艇模块与船体状态 | 累计船体伤害量、模块受损标记（`lightning_proximity` 等） | 航行结束时写入 | 更新船体完整性值、模块 actual_state |
| #6 玩家知识与情报 | `route_travel_completed`、`player_entered_zone`、`player_hit_obstacle` 事件 | 航行中实时 + 航行结束时 | 规律观测事件触发、航线知识状态推进（抵达时 → verified） |
| #11 探索/搜撤场景 | `EncounterContext`（航线 ID、目的地 ID、航程结果、遭遇列表） | 抵达时 | 如目的地有探索点内容，生成探索场景 |
| #17 反馈/特效/音频语义 | 遭遇事件（`encounter_triggered`）、状态变更（`voyage_state_changed`）、波段变更 | 航行中实时 | 播放遭遇视觉效果、音频提示、UI 更新 |
| #3 本地存档与世界状态持久化 | `progress.voyage` 快照包 | 航行结束时（抵达/撤退/迫降） | 持久化航线完成状态、新揭示的风险标签 |

**合约边界**：
- 本系统**只读**注册表、模块系统和情报系统——不写入知识状态（除航行事件通知外）、不修改模块状态（除结算伤害外）
- 本系统**只发出事件**给探索系统——事件发出后本系统关闭，不再参与探索过程
- 航行中的具体视觉/音频表现由 #16 和 #17 消费事件后实现——本系统只产出结构化的事件数据
- MVP 中遭遇不直接产出资源/货物——资源获取由探索系统（#11）在探索点中实现。航行遭遇只产出伤害、模块损毁标记和情报揭示

## Formulas

### Formula 1: Voyage Total Duration

```
T_voyage = T_distance / s_hull + ΣT_flat + ΣT_temp
```

| Variable | Definition | Source | Range |
|----------|-----------|--------|-------|
| `T_distance` | 距离带基础时长 | 航线静态数据（#1） | `short` = 60s, `medium` = 120s, `long` = 180s |
| `s_hull` | 船体速度系数 | 船体波段（#8） | `intact` = 1.0, `damaged` = 0.9, `critical` = 0.75 |
| `ΣT_flat` | 遭遇效果累计固定时长增加 | 遭遇表特殊效果（本系统） | 0–30s（`gentle_crosswind` 每次 +5s） |
| `ΣT_temp` | 遭遇效果临时速度惩罚折算时长 | 遭遇表特殊效果（本系统） | 0–15s（`turbulence_zone` 在下一检查前减速 15%） |
| `T_voyage` | 航行实际总时长 | 本系统计算 | `short` 约 60–90s, `medium` 约 120–180s |

**关键约束**：`T_voyage` 在航行启动时基于 `T_distance / s_hull` 固定基准，后续遭遇效果（`ΣT_flat`、`ΣT_temp`）叠加到已计算的基准上。遭遇检查次数 `N_checks` 以基准时长计算（见 Formula 2），不受遭遇效果影响——防止遭遇→延长时间→更多遭遇的正反馈循环。

**MVP 示例**：
- `route.sky-reef-arc-01`（short, intact hull）：`T_voyage = 60 / 1.0 + ΣT_flat + ΣT_temp`。若无减速遭遇：60s。若命中 2 次 `gentle_crosswind`：60 + 10 = 70s。
- `route.storm-cut-01`（medium, damaged hull）：`T_voyage = 120 / 0.9 + ΣT_flat + ΣT_temp ≈ 133.3s`。若额外命中 1 次 `turbulence_zone`（下一周期减速 15%，等效约 +3s）：≈ 136.3s。

### Formula 2: Encounter Check Timing & Count

```
T_check = T_base × (1 + Δ_hull)
N_checks = ⌊T_voyage_base / T_check⌋
```

| Variable | Definition | Source | Range |
|----------|-----------|--------|-------|
| `T_base` | 基础遭遇检查间隔 | 本系统配置 | 12s（默认值） |
| `Δ_hull` | 船体波段间隔偏移 | #8 波段定义 | `intact` = 0, `damaged` = -0.10, `critical` = -0.20 |
| `T_check` | 实际遭遇检查间隔 | 本系统计算 | 9.6–12s |
| `T_voyage_base` | 基准航行时长（不含遭遇效果） | Formula 1 | `T_distance / s_hull` |
| `N_checks` | 航行期间遭遇检查总次数 | 本系统计算 | 5–20 次 |

**偏移方向说明**：`Δ_hull` 为负值时 `T_check` 缩短，即船越破，遭遇检查越密集——体现"船越破飞得越慢、越容易遭遇麻烦"。

**MVP 示例**：
- `route.sky-reef-arc-01`（intact）：`T_check = 12 × 1.0 = 12s`，`N_checks = ⌊60 / 12⌋ = 5` 次。
- `route.storm-cut-01`（damaged）：`T_check = 12 × 0.9 = 10.8s`，`N_checks = ⌊120 / 10.8⌋ = 11` 次（比 intact 的 10 次多 1 次）。

### Formula 3: Scout Preview Window

```
T_preview = N_preview(η_scout) × T_check
```

| Variable | Definition | Source | Range |
|----------|-----------|--------|-------|
| `η_scout` | 侦察模块效率 | #8 模块状态查询 | 0, 0.6, 0.95, 1.0 |
| `N_preview(η_scout)` | 预览遭遇检查周期数 | 本系统映射表 | 见下表 |
| `T_check` | 实际遭遇检查间隔 | Formula 2 | 9.6–12s |
| `T_preview` | 侦察预览窗口时长 | 本系统计算 | 0–24s |

**`N_preview` 映射表**：

| `η_scout` | `N_preview` | `T_preview`（T_base=12s, intact） | 说明 |
|-----------|-------------|----------------------------------|------|
| 1.0 | 2 | 24s | 完好——提前约 2 个检查周期预览 |
| 0.95 | 1.5 → ⌊取整⌋ = 1 | 12s | 未检查但基本正常——提前约 1 个周期 |
| 0.6 | 1 | 12s | 受损——提前约 1 个周期 |
| 0（无模块） | 0 | 0s | 遭遇在发生时才知道 |

**取整规则**：`N_preview = ⌊η_scout × 2⌋`，即效率值 × 2 后向下取整。这决定了 `η_scout = 0.95` 时取整为 1（而非 2），产生有意义的阶梯差异。

**MVP 示例**：
- 完好侦察（`η_scout = 1.0`）在 `storm-cut-01` 上：可提前 2 个检查周期（约 24s）看到即将发生的遭遇图标。
- 无侦察模块：进度条上不显示任何预警图标，遭遇在触发时直接弹出结算。

### Formula 4: Damage Accumulation

```
d_check = max(d_entry_1, d_entry_2, ..., d_entry_k)   // k = 本次检查命中的遭遇条目数
D_accumulated += d_check
hull_integrity_effective = max(0, hull_integrity_departure - D_accumulated)
```

| Variable | Definition | Source | Range |
|----------|-----------|--------|-------|
| `d_entry_i` | 单个命中遭遇条目的伤害量 | 遭遇表（本系统 MVP 配置） | 0–6（见 Core Rules 遭遇表） |
| `d_check` | 单次遭遇检查结算伤害 | 本系统计算 | 0–6（取最大值，不叠加） |
| `D_accumulated` | 航行中累计未结算伤害 | 本系统追踪 | 0–`hull_integrity_departure` |
| `hull_integrity_departure` | 出航时船体完整性值 | #8 查询 | 1–100 |
| `hull_integrity_effective` | 航行中有效船体值 | 本系统计算 | 0–100 |

**叠加规则**：单次遭遇检查可能命中多个风险标签（如 `storm` + `low-visibility` 同时有效），但取所有命中条目伤害的最大值而非求和——一次遭遇检查只产出一次伤害结算。这防止了多标签航线出现不合理的伤害叠加。

**伤害写入时机**：`D_accumulated` 在航行中实时累积于内存。航行结束时（抵达/撤退/迫降）一次性写入 #8 系统更新 `hull_integrity` 值。模块损毁标记（如 `lightning_proximity` 的 20% 概率击中侦察模块）在遭遇结算时实时发出事件给 #8。

**MVP 示例**：
- `storm-cut-01` 第 3 次检查：同时命中 `storm` → `turbulence_zone`（d=2-4→抽到 3）和 `low-visibility` → `hidden_reef_proximity`（d=2-4→抽到 4）。`d_check = max(3, 4) = 4`，非 3+4=7。
- 若出航时 `hull_integrity = 85`，累计 5 次遭遇后 `D_accumulated = 18`：`hull_integrity_effective = max(0, 85 - 18) = 67`（仍在 `damaged` 波段）。

### Formula 5: Hidden Tag Reveal Probability

```
P_reveal = r_base   // 默认 0.30，storm_eye_passage 覆盖为 1.0
```

| Variable | Definition | Source | Range |
|----------|-----------|--------|-------|
| `r_base` | 基础揭露概率 | 本系统配置 | 0.30（默认，每遭遇检查） |
| `storm_eye_passage` | 强制揭露覆盖 | 遭遇条目特殊效果 | `P_reveal = 1.0`（本次检查揭露所有隐藏标签） |

**揭露机制**：
1. 每次遭遇检查触发时，对每个隐藏标签独立判定 `P_reveal`。
2. 若某隐藏标签在本次航行中已被揭露（此前任意检查中判定成功），后续检查不再重复判定——该标签转为可见，使用其遭遇表正常抽取。
3. 隐藏标签揭露后，航行结束时向 #6 情报系统发出更新事件——下次航图刷新时该标签从隐藏变为可见。
4. 若整个航行过程中隐藏标签未被揭露（所有判定均失败），标签保持隐藏状态——航线知识不推进。

**MVP 示例**：
- `storm-cut-01` 的 `low-visibility` 为隐藏标签。5 次遭遇检查中每次独立 30% 概率揭露。前 3 次失败（70%³ ≈ 34.3% 概率），第 4 次成功。前 3 次检查只消费 `storm` 遭遇表；第 4 次起同时消费 `storm` + `low-visibility` 遭遇表。航行结束后 `low-visibility` 标签在情报系统中更新为可见。
- 若命中 `storm_eye_passage`（10% 概率），该次检查立即揭露所有隐藏标签——无论此前判定结果如何。

## Edge Cases

### 1. 状态转换边界

**EC-01 — FORCED_LANDING 与 ARRIVED 同时触发**
船体在最后一次检查中降至 0，同时进度到达 100%。FORCED_LANDING 优先——船体归零发生在抵达之前，航程以迫降结束，不是抵达。

**EC-02 — 接近 100% 时撤退**
玩家在 99.9% 进度时触发撤退。接受撤退 → RETREATED。玩家的显式决定优先于逼近的抵达。

**EC-03 — VOYAGE_PREPARING 收到第二个 route_committed**
拒绝第二个事件，记录警告，保持当前 PREPARING 状态。航程系统不是可重入的。

**EC-04 — 上游查询部分成功后出航前检查失败**
VOYAGE_PREPARING → ABORTED_PREFLIGHT。不保留部分 VoyageContext，不出航。MVP 中 PREPARING 阶段不消耗资源。

**EC-05 — 终态下尝试直接启动新航程**
所有终态（ARRIVED/RETREATED/FORCED_LANDING/ABORTED_PREFLIGHT）不可逆转。唯一入口是 #9 的 `route_committed`，在清理完成后从 IDLE 转入。

### 2. 数值边界

**EC-06 — D_accumulated 超过 hull_integrity_departure**
如 hull=3，一次命中 6 点伤害 → D_accumulated = 9。`hull_integrity_effective = max(0, 3 - 9) = 0`。超出部分丢弃，不产生负值。

**EC-07 — N_checks = 0**
航线极短或 T_check 极长导致 ⌊T_voyage_base / T_check⌋ = 0。航程零遭遇，正常抵达。这是合法行为，不是错误。

**EC-08 — 空遭遇条目集的 max()**
一次检查中所有标签均为隐藏且未揭示，或可见标签的遭遇表为空 → 命中条目为空。`d_check = max(空集) = 0`，需显式定义。

**EC-09 — 所有遭遇条目的 d_entry 均为 0**
如 `calm_passage`(0) + `storm_eye_passage`(0) → d_check = 0。遭遇仍算"已结算"，触发非伤害效果（如揭示标签）。

**EC-10 — 航线零风险标签**
`hazard_tags: []`。每次检查零命中，d_check = 0。航程无遭遇但正常完成。

**EC-11 — 波段边界值**
hull = 76 属于 intact（≥76），hull = 75 属于 damaged（≤75）。所有边界用 ≥/≤ 判断，无 off-by-one。

### 3. 航行中波段动态变化（Option B）

**EC-12 — hull_integrity_effective 跨越波段阈值**
当累计伤害使船体跨波段（如 76→75，intact→damaged）：
- `s_hull` 和 `Δ_hull` 立即更新为新波段值
- `T_voyage` 重算：`T_distance / s_hull_new + ΣT_flat + ΣT_temp`
- `T_check` 重算：`T_base × (1 + Δ_hull_new)`
- 已调度但未触发的检查时间不回溯调整——仅下次新检查使用新 T_check
- 进度条不跳回——进度保持当前百分比前进，到达 100% 的时间变长。UI 用"预计剩余时间"数字变化传递"变慢了"
- 波段变更事件实时发送至 #17 播放反馈

**EC-13 — 同一检查中波段跨越多次**
单次检查伤害上限 6 点，不可能一次跨越两个波段（intact→damaged 需 -25）。若未来出现超高伤害遭遇，逐波段触发——每个阈值跨越时发出独立事件。

### 4. 隐藏标签与揭示

**EC-14 — 所有隐藏标签全程未被揭示**
概率 = 0.7^N_checks。N=5 时约 16.8%，N=10 时约 2.8%。航程零遭遇——隐藏标签未揭示则不参与遭遇抽取。这是 Pillar 4 的正向设计，不是 bug。

**EC-15 — storm_eye_passage 揭示已揭示的标签**
仅对当前仍隐藏的标签生效。已揭示的不重复更新。

**EC-16 — 注册表有标签但情报系统未知**
#1 的标签列表比 #6 的查询结果多（如新增了 `pirate_activity`）。#6 无该标签的条目时，默认 `hidden=true`（悲观策略）。记录警告。

### 5. 模块状态变化

**EC-17 — 侦察模块航行中被 lightning_proximity 击中**
η_scout 从 1.0→0.6（或 0.95→0.6）。N_preview 立即重算——预览窗口缩小。已在队列中的预览遭遇保留；超出新预览范围的图标移除。若侦察槽为空（无模块安装），20% 命中检定跳过。

**EC-18 — 双侦察模块冗余**
Slot A 完好(1.0)、Slot B 受损(0.6) → η_effective = max(1.0, 0.6) = 1.0。预览窗口保持 24s。两个都受损后才降到 0.6。

**EC-19 — unchecked 模块在航行中保持 unchecked**
出发时 η_scout=0.95（unchecked），即使实际状态为 damaged，航行中不"意外发现"真实状态。按 #8 EC-04——unchecked 是已知风险折扣，不是航行中的突袭。

### 6. 遭遇效果叠加与冲突

**EC-20 — wind_shear 连续命中导致检查间隔过短**
每次 wind_shear 下一检查 -5s。多次叠加时设硬下限 `T_check_min = 4s`，防止遭遇触发过快玩家无法处理。

**EC-21 — turbulence_zone 惩罚与检查边界重叠**
惩罚在两次检查之间生效。新检查在惩罚计时触发。检查结算后惩罚过期。不会出现"惩罚在结算中途过期"的模糊窗口。

**EC-22 — 多标签同时命中时的伤害取最大值**
一次检查中 `storm→turbulence_zone(d=3)` + `low-visibility→hidden_reef(d=4)` → d_check = max(3,4) = 4，非 3+4=7。防止多标签航线伤害不合理叠加。

### 7. 玩家行为

**EC-23 — 出航锁结束后立即撤退**
进度 0%，D_accumulated=0，无遭遇结算。合法操作——玩家可能点错了或改变主意。不惩罚。

**EC-24 — 完全被动完成航程**
玩家不操作，不撤退。航程正常推进，遭遇自动结算，终态取决于累计伤害是否触发 FORCED_LANDING。航程是"观察并决策"的体验。

**EC-25 — 快速切换撤退确认/取消**
撤退确认是布尔切换。一旦确认进入 RETREATED（终态），后续 UI 输入被终态守卫忽略。

### 8. 存档与恢复

**EC-26 — 航行中存档/读档**
存档时导出完整快照：`{route_id, D_accumulated, elapsed_time, N_checks_total, resolved_encounters, pending_encounters, revealed_hidden_tags, hull_integrity_departure, scout_efficiency_snapshot, hull_band_snapshot, voyage_state}`。读档时若 `voyage_state == IN_PROGRESS`，从 `elapsed_time` 恢复。

**EC-27 — 跨版本存档的遭遇表变更**
已结算遭遇保留历史记录。未触发的检查使用当前版本的遭遇表。如需废弃进行中的航程，由 #3 存档迁移系统决定。

**EC-28 — 航行结束多系统写入中途崩溃**
写入顺序：(1) #8 船体伤害 (2) #6 路线知识更新 (3) #11 EncounterContext (4) #17 状态变更事件 (5) 存档。若在 (1)(2) 之间崩溃：读档时检测 `voyage_state==ARRIVED` 但 #6 未确认事件，重新发送。

### 9. 上游数据一致性

**EC-29 — route_id 在注册表中不存在**
#9 发出的事件包含无效 route_id。ABORTED_PREFLIGHT，原因："route_id [id] not found in content registry"。

**EC-30 — #9 的 hazard_tags 与注册表不一致**
以注册表为准。#9 漏掉的标签补入并警告；#9 多出的标签排除并警告。

**EC-31 — #6 query_route_knowledge 超时/失败**
ABORTED_PREFLIGHT。没有知识状态就无法确定可见/隐藏标签——航程无法安全进行。不缓存过期知识。

**EC-32 — #9 允许出航但 #10 查询时 can_depart 返回 false**
VOYAGE_PREPARING 结尾重新查询 #8——不信任 #9 的预检结果。若此刻 can_depart 为 false，ABORTED_PREFLIGHT。防御 TOCTOU。

### 10. 平台与计时

**EC-33 — 浏览器标签页切换导致计时器节流**
航程使用引擎 delta 而非挂钟时间。标签页挂起时航程暂停；恢复后 delta 变大但 elapsed_time 累积正确。遭遇按 elapsed_time 触发，不会"错过"——恢复后排队结算。Web 平台显式设计约束。

**EC-34 — 浮点累积误差**
elapsed_time 用 float，每帧累加 delta。抵达判定的 epsilon = 0.01s，避免浮点比较跳过抵达触发。进度条 clamp 到 100%。

### 11. 配置错误防御

**EC-35 — Δ_hull 配置错误导致 T_check ≤ 0**
Δ_hull 限制范围 (-0.5, 0]。T_check 硬下限 = max(4s, T_base × 0.5)。启动时验证，超出范围的值 clamp 并告警。验证失败时回退到 T_base=12s, Δ_hull=0。

**EC-36 — T_voyage_base < T_check**
航线极短或配置错误。N_checks = 0，零遭遇。内容加载时发出警告。

## Dependencies

### 上游依赖（本系统从以下系统读取）

| 系统 | 依赖内容 | 接口 | 临界性 |
|------|---------|------|--------|
| #9 航图与航线规划 | `route_committed(route_id, destination_id, hazard_tags)` 事件 —— 航程启动的唯一入口 | 信号 | 阻断——无此事件系统不运行 |
| #7 飞艇家园 Hub | `helm_activated(hub_state_pack)` 事件 —— Mode B 自主飞行入口 | 信号 | 非阻断——仅 Mode B 航程需要；Mode A 由 #9 `route_committed` 触发 |
| #1 内容数据与状态注册表 | 航线静态数据：`distance_band`、所有 `hazard_tags`、`origin_id`、`destination_id` | `list_by_kind("route")` 查询 | 阻断——无静态数据无法构建 VoyageContext |
| #8 飞艇模块与船体状态 | `η_scout`、`hull_band`、`hull_integrity`、`can_depart()`、`M_max`、`M_loaded` | 查询接口（出航时 + 每次遭遇检查前） | 阻断——can_depart 返回 false 则出航中止 |
| #5 资源、货物与容量 | `get_carried_supply()` —— 查询随身补给品数量，用于消耗结算 | 查询接口（出航时 + 抵达时） | 阻断——补给品不足则出航中止 |
| #6 玩家知识与情报 | `query_route_knowledge(route_id)` → 知识状态 + 可见/隐藏标签映射 + 来源标注 | 查询接口（出航时 + 每次遭遇检查） | 阻断——查询失败则出航中止 |

### 下游依赖（以下系统消费本系统的输出）

| 系统 | 消费内容 | 接口/时机 | 消费目的 |
|------|---------|---------|---------|
| #8 飞艇模块与船体状态 | 累计船体伤害 `D_accumulated`、模块损毁标记（`lightning_proximity` 等） | 航程结束时写入 + 遭遇结算时实时事件 | 更新 `hull_integrity` 值、模块 `actual_state` |
| #6 玩家知识与情报 | `route_travel_completed`（含 status=arrived/retreated）、`player_entered_zone`、`player_hit_obstacle`、隐藏标签揭示更新 | 航行中实时 + 航行结束时 | 规律观测事件触发、航线知识状态推进（抵达时 → verified） |
| #11 探索/搜撤场景 | `EncounterContext`（航线 ID、目的地 ID、航程结果、遭遇列表、迫降位置） | 抵达或迫降时 | 如目的地有探索点内容则生成探索场景；迫降时生成坠机场景 |
| #17 反馈/特效/音频语义 | `encounter_triggered`、`voyage_state_changed`、`hull_band_changed`、`scout_preview_updated` | 航行中实时 | 播放遭遇视觉效果、音频提示、UI 进度条更新 |
| #3 本地存档与世界状态持久化 | `progress.voyage` 快照包（含完整航行状态用于断点续传） | 航行结束时 + 自动存档点 | 持久化航线完成状态、新揭示的风险标签、航行中断恢复点 |
| #14 空港/村镇/船队（Phase 3+） | 统一的 `EncounterContext`（与玩家航程相同接口） | 委托航程结算时 | NPC 航线风险解析、玩家委托货运路线结算 |

### 双向交叉引用验证

| 本系统声明的依赖 | 对应系统的 GDD 是否记录了反向依赖 |
|----------------|------------------------------|
| 依赖 #9 的 `route_committed` | ✅ #9 GDD 记录了下游 `route_committed → #10` |
| 依赖 #7 的 `helm_activated` | ✅ #7 GDD Interactions 表委托 Mode B 至 #10；本系统已添加 #7 为上游依赖（2026-05-08） |
| 依赖 #8 的模块/船体查询 | ✅ #8 GDD 记录了 `can_depart()` 的消费方为 #9 和 #10 |
| 依赖 #6 的知识查询 | ✅ #6 GDD 记录了 `query_route_knowledge` 被 #9、#10、#11 消费 |
| 写入 #8 船体伤害 | ✅ #8 GDD 在受损来源中记录了 #10（航行伤害） |
| 写入 #6 路线知识事件 | ✅ #6 GDD 记录了 #10 是规律观测事件的来源之一 |
| 写入 #11 EncounterContext | ✅ #11 GDD 已编写并批准（2026-05-03）——EncounterContext 合约双向确认：#10 定义输出 schema，#11 在 Interactions 表中确认消费。字段名、类型、到达时序已对齐。 |
| 写入 #17 反馈事件 | ⚠️ #17 GDD 尚未编写——事件 schema 已在本系统定义，待 #17 设计时确认 |

### 未满足的依赖

- **#7 飞艇家园 Hub**：双向依赖已确认（2026-05-08）。#7 Interactions 表委托 Mode B 至 #10，本系统已添加 `helm_activated(hub_state_pack)` 作为上游信号。Mode B 风险倍率（×2.0）将在 Vertical Slice 中细化。
- **#5 资源、货物与容量**：双向依赖已确认（2026-05-08）。本系统出航时查询 `get_carried_supply()` 并消耗补给品；补给品消耗量见 Tuning Knobs > 补给品消耗。
- **#11 探索/搜撤场景**：GDD 已编写并批准（2026-05-03）。#11 的 Interactions 表已确认消费 `EncounterContext`。#11 AC-11-01 要求有效的 EncounterContext 含 voyage_result 和 destination_id。双向合约已确认。
- **#17 反馈/特效/音频语义**：GDD 尚未编写。本系统已定义事件 schema，但 #17 如何消费待其设计时确认。
- **#14 空港/村镇/船队**：Phase 3+ 系统。本系统已预留统一的 `VoyageContext` 接口（与航行主体类型无关），但 #14 的具体委托航程逻辑待 Phase 3 设计。

## Tuning Knobs

### 节奏调节

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `T_base` — 基础遭遇检查间隔 | 12s | 8–20s | 遭遇密度。过低 → 遭遇拥挤、玩家来不及处理；过高 → 航行感觉空洞 |
| `base_lock_duration` — 出航锁定时间 | 2.0s | 1.0–4.0s | 出航确认后的过渡窗口，防止玩家在 UI 未就绪时操作 |
| `T_check_min` — 遭遇间隔硬下限 | 4s | 3–6s | 防止 wind_shear 叠加导致遭遇连发。低于 3s 玩家无法反应 |

### 船体波段

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `s_hull[ intact ]` | 1.0 | —（基准，不应调整） | 基准速度系数。其他波段相对此值定义 |
| `s_hull[ damaged ]` | 0.9 | 0.80–0.95 | 受损后的速度惩罚。过低 → 撤退变得太有吸引力；过高 → 波段跨越无感知 |
| `s_hull[ critical ]` | 0.75 | 0.60–0.85 | 危急时的速度惩罚。与 damaged 需保持 ≥0.10 差距以产生阶梯感 |
| `Δ_hull[ damaged ]` | -0.10 | -0.05 ~ -0.20 | 受损后遭遇检查间隔偏移。绝对值越大 → 越密集 |
| `Δ_hull[ critical ]` | -0.20 | -0.10 ~ -0.30 | 危急时遭遇检查间隔偏移。与 damaged 需保持阶梯差 |
| `hull_band_thresholds` | intact ≥ 76, damaged ≥ 26, critical ≥ 1 | 不可大幅偏离 #8 的波段定义 | 必须与 #8 的波段阈值一致。此处仅作为快照引用，权威值在 #8 |

### 侦察预览

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `scout_preview_multiplier` | 2 | 1.5–3.0 | `N_preview = ⌊η_scout × multiplier⌋`。越大 → 侦察信息优势越明显。当前 multiplier=2 时预览 0–2 个周期 |
| `N_preview 映射表` | η=1.0→2, η=0.95→1, η=0.6→1, η=0→0 | — | 离散阶梯。若 multiplier 调整，此表重新计算。安全约束：最高不应超过 4 个周期（48s） |

### 风险揭示

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `r_base` — 隐藏标签基础揭露概率 | 0.30 | 0.15–0.50 | 每次遭遇检查的揭露概率。过低 → 隐藏标签几乎永远不揭示；过高 → 失去"未知"的张力。0.30 使 5 次检查后仍有 16.8% 概率未揭示 |

### 距离带基准时长

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `T_distance[ short ]` | 60s | 40–90s | 短途航行的基准时长。MVP 中用于 `sky-reef-arc-01` |
| `T_distance[ medium ]` | 120s | 90–180s | 中途航行的基准时长。MVP 中用于 `storm-cut-01` |
| `T_distance[ long ]` | 180s | 150–300s | 长途航行的基准时长。MVP 中不使用，Phase 2+ 预留 |

### 补给品消耗

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `supply_consumption[short]` | 2 | 1–3 | 短途航行消耗 basic_supply 数量（等价于 100 云海币）。repair-canvas 可作为 basic_supply 替代品按 1:1 换算。MVP 中用于 `sky-reef-arc-01` |
| `supply_consumption[medium]` | 4 | 3–6 | 中途航行消耗量（等价于 200 云海币）。MVP 中用于 `storm-cut-01` |
| `supply_consumption[long]` | 8 | 6–12 | 长途航行消耗量。MVP 中不使用，Phase 2+ 预留 |
| `supply_cost_ratio_max` | 0.30 | 0.20–0.40 | 补给品成本占航线预期收益的上限比例。短途示例：2 basic_supply (100¢) ÷ 探索预期收益 (150-240¢) ≈ 0.42-0.67 — 超过上限，需通过探索收益平衡或降低消耗。见 #14 联动约束 |

### 遭遇表调优参数

| 参数 | 当前值 | 安全范围 | 影响 |
|------|--------|---------|------|
| 单次遭遇伤害上限 | 6 | 4–10 | `d_entry` 的最大值。与 hull=100 的比例决定"几次重大遭遇让船进入危急"。当前 6 点 = 约 4 次最大伤害从 intact 到 damaged |
| `encounter_probability` 分布 | 见 Core Rules 遭遇表 | 总和必须 = 100% | 单表内概率必须归一化。不同遭遇条目的概率比决定该标签的"性格"——安全线的 `calm_passage` 40% 保证大部分检查无事发生 |
| `storm_eye_passage` 概率 | 10% | 5–20% | 风暴眼中的幸运时刻。过高 → 隐藏标签揭示太容易；过低 → 几乎遇不到 |

### 平台与精度

| 参数 | 默认值 | 安全范围 | 影响 |
|------|--------|---------|------|
| `arrival_epsilon` | 0.01s | 0.005–0.05s | 浮点比较容差。低于 0.005s 可能因浮点误差跳过抵达；高于 0.05s 玩家可能感知到"提前到达" |

> **Phase 2+ 注意**：以上安全范围基于 MVP 的 60–120s 航程设计。若引入更长航程（180s+），节奏相关参数（`T_base`、`T_distance`）需按比例重调。

## Visual/Audio Requirements

航行系统是后台风险解析引擎，不直接负责视觉/音频实现。以下为向下游 #16（UI/HUD）和 #17（反馈/特效/音频）发出的**语义事件需求**——具体视觉/音频表现由消费系统定义。

### 事件驱动的视觉需求

| 事件 | 语义 | 建议视觉方向 | 消费方 |
|------|------|-------------|--------|
| `voyage_state_changed(PREPARING→IN_PROGRESS)` | 出航锁结束，航行开始 | 航图缩小/过渡至航行视图；飞艇剪影出现在云层中 | #16 #17 |
| `encounter_triggered(type, damage, effects)` | 遭遇发生 | 遭遇卡片弹出（类型图标 + 伤害值 + 效果文字）；飞艇剪影产生相应反应（晃动/火花） | #16 #17 |
| `scout_preview_updated(preview_encounters)` | 侦察预览变化 | 进度条前方的半透明预警图标更新；`?` 标记用于隐藏标签 | #16 |
| `hull_band_changed(old, new)` | 船体波段跨越 | 仪表板颜色切换（绿→黄→红）；飞艇剪影产生结构性变化（冒烟/碎片） | #17 |
| `voyage_state_changed(→ARRIVED)` | 安全抵达 | 目的地浮现动画；飞艇平稳降落 | #16 #17 |
| `voyage_state_changed(→RETREATED)` | 主动撤退 | 飞艇调头动画；航程日志弹出 | #16 |
| `voyage_state_changed(→FORCED_LANDING)` | 迫降 | 飞艇下坠动画；撞击效果；黑屏过渡 | #16 #17 |

### 事件驱动的音频需求

| 事件 | 语义 | 建议音频方向 | 消费方 |
|------|------|-------------|--------|
| `encounter_triggered(type=storm_*)` | 风暴类遭遇 | 风声音量增强；雷声（如 lightning_proximity） | #17 |
| `encounter_triggered(type=calm_passage)` | 平静通过 | 安静的风声底噪；偶有飞艇引擎平稳运转声 | #17 |
| `encounter_triggered(damage > 0)` | 受到伤害 | 船体受击音效（木质/金属碰撞）；船员反应声 | #17 |
| `hull_band_changed(→damaged)` | 进入受损波段 | 低频警报音；船体结构呻吟 | #17 |
| `hull_band_changed(→critical)` | 进入危急波段 | 警报音量增大；引擎不规律运转声 | #17 |
| `voyage_state_changed(→ARRIVED)` | 抵达 | 引擎减速音；目的地环境音渐入 | #17 |

### 氛围设计备注

- 航行中保持**持续的环境音层**：风声（随风力变化）、引擎声（随波段变化）、船体偶尔的呻吟
- 进度条推进时保持**节拍感**：每 12 秒（或变体）的遭遇检查周期形成航行的心跳节奏
- 接近目的地时（最后 20%）环境音可以逐渐引入**目的地特征音**（如港口钟声、岛屿海浪）

## UI Requirements

### 航行主界面

| 元素 | 位置 | 行为 |
|------|------|------|
| 航行进度条 | 屏幕底部居中 | 从 0% 平滑推进至 100%；波段转换时进度不回退，剩余时间数字更新 |
| 遭遇标记点 | 进度条上方 | 已结算遭遇显示为小图标（类型对应）；侦察预览的未触发遭遇显示为半透明图标在前方；隐藏标签预览显示 `?` |
| 撤退按钮 | 屏幕右下角 | 航行中始终可用；点击弹出确认对话框；确认后立即进入 RETREATED |
| 当前波段指示器 | 屏幕左上角 | 颜色编码：绿（intact）/ 黄（damaged）/ 红（critical）；波段转换时闪烁过渡 |
| 船体完整性条 | 进度条上方或侧边 | 实时反映 `hull_integrity_effective`；伤害结算时红色闪烁减少 |
| 剩余时间估算 | 进度条右侧 | 格式 "约 X:XX"；`false_horizon` 效果期间偏离实际值（±15%） |
| 侦察效率指示器 | 波段指示器旁 | 小型侦察图标 + 效率值；模块受损时图标变化 |

### 遭遇弹窗

| 元素 | 行为 |
|------|------|
| 遭遇卡片 | 遭遇触发时从右侧滑入，停留 3-4s 后滑出；显示类型名称、图标、伤害值（如有）、特殊效果文字 |
| 非伤害遭遇 | 仅显示效果文字（如"视野收缩——侦察预览减半"），无伤害数字 |
| 多重遭遇 | 同时触发多个遭遇时，卡片堆叠显示，取伤害最大者在最前 |

### 撤退确认

| 元素 | 行为 |
|------|------|
| 确认对话框 | 暗色半透明遮罩 + 居中卡片；显示"确定撤退？" + 当前进度 + 已承受伤害 + 已揭示信息 |
| 确认按钮 | 标签"撤退"；点击后不可撤销（终态） |
| 取消按钮 | 标签"继续航行"；关闭对话框返回航行 |

## Acceptance Criteria

> Each AC must be verifiable as **pass/fail** by a QA tester without ambiguity.
> Test data uses the two MVP routes: `route.sky-reef-arc-01` (short, `safe`, `identified`) and `route.storm-cut-01` (medium, `storm` + `low-visibility` hidden, `rumored`), departing from `location.glass-harbor` with hull integrity = 85 (intact band).
>
> **QA Debug Commands Required**: `set_hull_integrity(value)`, `set_module_state(slot, state)`, `set_route_knowledge(route_id, state)`, `force_encounter(encounter_id)`, `advance_voyage_time(seconds)`, `set_hidden_tag_revealed(route_id, tag, bool)`, `trigger_save()`, `trigger_load()`.

### Voyage Initiation and State Transitions

**AC-01**: Receiving a valid `route_committed(route_id, destination_id, hazard_tags)` event while the system is IDLE transitions to `VOYAGE_PREPARING`, constructs a VoyageContext with all queried data, and after all upstream checks pass transitions to `IN_PROGRESS` with the voyage timer starting.

**AC-02**: When `base_lock_duration` (2.0s) elapses after `IN_PROGRESS` is entered, the player sees the voyage progress bar at 0% and the first encounter check is scheduled at `elapsed_time + T_check`.

**AC-03**: Receiving a second `route_committed` event while already in `VOYAGE_PREPARING` or `IN_PROGRESS` rejects the duplicate event, logs a warning, and preserves the current state unchanged.

**AC-04**: Any trigger applied to a terminal state (`ARRIVED`, `RETREATED`, `FORCED_LANDING`, `ABORTED_PREFLIGHT`) returns `{allowed: false}` with no state change — all terminal states are irreversible.

**AC-05**: The `IN_PROGRESS → IN_PROGRESS` transition is a no-op — it produces no effect when triggered and does not reset or double-schedule any timers.

### Aborted Preflight

**AC-06**: When the `route_id` in the `route_committed` event does not exist in the content registry (`list_by_kind("route")`), the system transitions to `ABORTED_PREFLIGHT` with the reason "route_id [id] not found in content registry".

**AC-07**: When `#6 query_route_knowledge(route_id)` times out or returns a failure during `VOYAGE_PREPARING`, the system transitions to `ABORTED_PREFLIGHT` — no cached or stale knowledge is used.

**AC-08**: When `can_depart()` returns `{false, reasons}` on the final preflight re-check (TOCTOU defense), the system transitions to `ABORTED_PREFLIGHT` with the specific `reasons` listed, even if #9's pre-check previously passed.

**AC-09**: When #9's `hazard_tags` in the event disagrees with the registry's static tags, the registry wins — tags missing from the event are added with a warning, and tags in the event but absent from the registry are excluded with a warning.

**AC-10**: When a partial upstream query succeeds (e.g., #8 returns data but #6 fails), the system transitions to `ABORTED_PREFLIGHT` — no partial VoyageContext is retained and no departure occurs.

**AC-11**: When the registry has a tag not present in #6's knowledge result, the tag is treated as `hidden=true` by default with a warning logged — the pessimistic strategy prevents unknown risks from appearing visible.

### Time-Based Voyage and Encounter Checking

**AC-12**: `T_check` is computed as `T_base × (1 + Δ_hull)` at voyage start: 12s for intact (Δ=0), 10.8s for damaged (Δ=-0.10), and 9.6s for critical (Δ=-0.20).

**AC-13**: `N_checks = ⌊T_voyage_base / T_check⌋` is computed once at departure and never changes during the voyage — encounter effects (`ΣT_flat`, `ΣT_temp`) do not trigger additional checks, preventing a positive feedback loop.

**AC-14**: For `route.sky-reef-arc-01` (short, intact hull): `T_voyage_base = 60 / 1.0 = 60s`, `T_check = 12s`, `N_checks = ⌊60/12⌋ = 5`.

**AC-15**: For `route.storm-cut-01` (medium, intact hull): `T_voyage_base = 120 / 1.0 = 120s`, `T_check = 12s`, `N_checks = ⌊120/12⌋ = 10`.

**AC-16**: For `route.storm-cut-01` (medium, damaged hull): `T_voyage_base = 120 / 0.9 ≈ 133.3s`, `T_check = 12 × 0.9 = 10.8s`, `N_checks = ⌊133.3/10.8⌋ = 11` — one more check than intact.

**AC-17**: For `route.storm-cut-01` (medium, critical hull): `T_voyage_base = 120 / 0.75 = 160s`, `T_check = 12 × 0.8 = 9.6s`, `N_checks = ⌊160/9.6⌋ = 16`.

**AC-18**: The encounter timer uses engine delta, not wall-clock time — when the game window loses focus or is minimized, `elapsed_time` stops accumulating, and when the window is restored, encounters queue and settle without being missed.

**AC-19**: The voyage progress bar displays `min(100%, elapsed_time / T_voyage × 100)` and is clamped to 100% — arrival is detected when `elapsed_time >= T_voyage - epsilon` where `epsilon = 0.01s`.

**AC-20**: When `T_voyage_base < T_check` (e.g., extremely short route or misconfiguration), `N_checks = 0` — the voyage proceeds with zero encounter checks and arrives normally.

### Scout Preview

**AC-21**: `N_preview = ⌊η_scout × 2⌋` — with `η_scout = 1.0` (installed), `N_preview = 2` and `T_preview = 2 × T_check = 24s` at intact hull.

**AC-22**: With `η_scout = 0.6` (damaged), `N_preview = ⌊0.6 × 2⌋ = 1` and `T_preview = 1 × T_check = 12s` at intact hull.

**AC-23**: With `η_scout = 0.95` (unchecked), `N_preview = ⌊0.95 × 2⌋ = 1` — the floor rounding produces a meaningful step difference from `η_scout = 1.0`.

**AC-24**: With no scout module installed (`η_scout = 0`), `N_preview = 0` and `T_preview = 0s` — encounters appear only at the moment they trigger, with no advance warning icons.

**AC-25**: With dual scout modules where Slot A = installed (1.0) and Slot B = damaged (0.6), `η_effective = max(1.0, 0.6) = 1.0` and preview window remains 24s.

**AC-26**: Scout preview icons appear on the progress bar ahead of the current position — visible hazard tags display the encounter type icon, while hidden hazard tags display only a `?` marker.

**AC-27**: When `lightning_proximity` hits the scout module mid-voyage (20% chance), `η_scout` drops from 1.0 to 0.6, `N_preview` recalculates from 2 to 1, and any preview icons beyond the new preview range are removed from the UI.

**AC-28**: When a scout module is `unchecked` (η=0.95) at departure, it remains `unchecked` throughout the entire voyage — no mid-voyage discovery of its actual state occurs.

### Damage Accumulation

**AC-29**: When a single encounter check triggers one visible hazard tag, one `EncounterEntry` is drawn from that tag's encounter table and `d_check = d_entry` from that single entry.

**AC-30**: When a single encounter check triggers multiple visible tags (e.g., `storm` + `low-visibility` both active), an entry is drawn from each tag's table and `d_check = max(d_entry_1, d_entry_2, ...)` — damage is NOT summed across tags.

**AC-31**: When all tags for a check are hidden and none are revealed, the hit set is empty and `d_check = max(empty set) = 0` — the check still counts as "resolved" but produces zero damage.

**AC-32**: When all drawn encounter entries have `d_entry = 0` (e.g., `calm_passage` + `storm_eye_passage`), `d_check = 0` but non-damage effects (tag reveals, speed changes) are still applied.

**AC-33**: `D_accumulated` increments by `d_check` after each encounter and is held in memory during `IN_PROGRESS` — the accumulated value is written to #8 as a single hull damage update only when the voyage ends.

**AC-34**: `hull_integrity_effective = max(0, hull_integrity_departure - D_accumulated)` — damage that would push the effective value below zero is discarded, and the value is never negative.

**AC-35**: No single `d_entry` in the MVP encounter tables exceeds 6 — the encounter configuration enforces a per-entry damage cap of 6.

**AC-36**: Module damage from `lightning_proximity` (20% chance to hit scout module) is emitted as a real-time event to #8 when the check resolves — it does not wait for voyage end.

**AC-37**: Damage from a route with zero hazard tags (`hazard_tags: []`) produces `d_check = 0` for every check — the voyage completes normally with no damage.

### Hidden Tag Reveals

**AC-38**: At each encounter check, for every hidden tag the system independently rolls `P_reveal = 0.30` — if the roll succeeds, that tag is revealed and from the next check onward it is treated as a visible tag using its normal encounter table.

**AC-39**: A hidden tag that successfully reveals on check N contributes to the encounter draw from check N+1 onward — the reveal check itself happens before the encounter draw on check N, so the newly revealed tag's table is available for check N.

**AC-40**: When `storm_eye_passage` (10% probability in the `storm` table) is drawn, all currently-hidden tags are immediately revealed — `P_reveal` is overridden to 1.0 for this check only.

**AC-41**: When `storm_eye_passage` triggers but all hidden tags are already revealed, only still-hidden tags are affected — no duplicate or redundant updates occur.

**AC-42**: When a hidden tag remains unrevealed across all N_checks, the tag stays hidden — no knowledge update is emitted to #6, and the route knowledge state does not advance.

**AC-43**: On voyage end (any terminal state), all tags revealed during the voyage emit an update event to #6 — the intelligence system marks those tags as visible for future chart rendering.

**AC-44**: For `route.storm-cut-01`: the `storm` tag is visible from the start (drawn from the `storm` encounter table), while the `low-visibility` tag is hidden and only contributes its encounter table after a successful reveal roll or a `storm_eye_passage` trigger.

### Retreat

**AC-45**: The player may trigger retreat at any point during `IN_PROGRESS` — the system transitions immediately to `RETREATED` (terminal state).

**AC-46**: Retreat at 0% progress (immediately after the departure lock ends, before any encounter) is legal — `D_accumulated = 0`, no encounters are resolved, and no penalty is applied.

**AC-47**: Retreat at 99.9% progress (just before arrival) is accepted — the player's explicit retreat decision overrides the imminent arrival, and the state is `RETREATED`, not `ARRIVED`.

**AC-48**: On retreat, all accumulated damage (`D_accumulated`) is preserved and written to #8 — there is no damage forgiveness for retreating.

**AC-49**: On retreat, all hidden tags revealed during the voyage remain revealed — the knowledge gained is retained for future voyages.

**AC-50**: On retreat, the route knowledge state does NOT advance to `verified` — only a successful `ARRIVED` triggers the `verified` state transition.

**AC-51**: Once the retreat confirmation is triggered, it is a boolean toggle to the terminal `RETREATED` state — subsequent UI input after confirmation is ignored by the terminal state guard.

### End States

**AC-52**: When `elapsed_time >= T_voyage` (progress reaches 100%), the system transitions to `ARRIVED` (terminal) — `route_travel_completed(status=arrived)` is emitted to #6, route knowledge advances to `verified`, and `EncounterContext` is emitted to #11.

**AC-53**: When `hull_integrity_effective <= 0` at any point during `IN_PROGRESS`, the system transitions to `FORCED_LANDING` (terminal) — hull scars increment by 1, damage events are emitted to #8, and forced landing events are emitted to #11 and #17.

**AC-54**: When `FORCED_LANDING` and `ARRIVED` conditions trigger on the same check (hull reaches 0 exactly as progress hits 100%), `FORCED_LANDING` takes priority.

**AC-55**: When the player takes no action for the entire voyage, the voyage completes normally — the end state is `ARRIVED` if hull stays above 0, or `FORCED_LANDING` if accumulated damage reduces hull to 0.

**AC-56**: Any attempt to start a new voyage from a terminal state is rejected — the only valid entry point is a new `route_committed` event from #9 after the system has returned to IDLE.

### Hull Band Transitions Mid-Voyage (Dynamic)

**AC-57**: When `hull_integrity_effective` crosses from intact (>=76) to damaged (<=75), `s_hull` immediately updates to 0.9 and `Δ_hull` immediately updates to -0.10.

**AC-58**: When `hull_integrity_effective` crosses from damaged (>=26) to critical (<=25), `s_hull` immediately updates to 0.75 and `Δ_hull` immediately updates to -0.20.

**AC-59**: On band transition, `T_voyage` is recalculated using the new `s_hull` (`T_distance / s_hull_new + ΣT_flat + ΣT_temp`), and `T_check` is recalculated using the new `Δ_hull` (`T_base × (1 + Δ_hull_new)`).

**AC-60**: On band transition, the progress bar does NOT jump backward — the current percentage is preserved; instead, the remaining time estimate in the UI updates to reflect the new, longer `T_voyage`.

**AC-61**: On band transition, already-scheduled but not-yet-triggered encounter checks are NOT retroactively rescheduled — only the next new check after the transition uses the new `T_check`.

**AC-62**: On band transition, a `hull_band_changed(old_band, new_band)` event is emitted to #17 in real-time for audio/visual feedback.

**AC-63**: The band thresholds use correct boundary logic: `hull = 76` is intact, `hull = 75` is damaged, `hull = 26` is damaged, `hull = 25` is critical — no off-by-one error at any boundary.

**AC-64**: With the current per-check damage cap of 6, a single encounter check cannot cause two band transitions; if a future high-damage entry is added, each threshold crossing emits an independent `hull_band_changed` event in sequence.

### Encounter Effects

**AC-65**: Each `gentle_crosswind` encounter adds 5s to `ΣT_flat`, increasing `T_voyage` by exactly 5s per occurrence — `route.sky-reef-arc-01` with 2 `gentle_crosswind` hits has `T_voyage = 60 + 10 = 70s`.

**AC-66**: A `turbulence_zone` encounter reduces speed by 15% for the interval between the current check and the next check — the effect expires cleanly after the next check resolves, with no ambiguous overlap window.

**AC-67**: A `wind_shear` encounter reduces the interval until the next encounter check by 5s — if multiple `wind_shear` effects stack, the effective interval is clamped at the hard floor `T_check_min = 4s`.

**AC-68**: `ΣT_flat` and `ΣT_temp` affect `T_voyage` but do NOT affect `N_checks` — `N_checks` is computed from `T_voyage_base` only at departure, preventing a positive feedback loop.

**AC-69**: `dense_fog_bank` (from the `low-visibility` hidden table) halves the scout preview window for the next encounter check only — the halving applies to `T_preview` at that specific check, then resets.

**AC-70**: `false_horizon` (from the `low-visibility` hidden table) causes the remaining time estimate displayed to the player to deviate by +/-15% from the actual value for the remainder of the voyage — the actual `T_voyage` and encounter timing are unaffected, only the UI estimate is distorted.

### Save/Load

**AC-71**: Saving during `IN_PROGRESS` exports a complete snapshot containing: `route_id`, `D_accumulated`, `elapsed_time`, `N_checks_total`, `resolved_encounters[]`, `pending_encounters[]`, `revealed_hidden_tags[]`, `hull_integrity_departure`, `scout_efficiency_snapshot`, `hull_band_snapshot`, and `voyage_state`.

**AC-72**: Loading a save with `voyage_state == IN_PROGRESS` restores the voyage from `elapsed_time` — the timer continues from the saved point, all internal state is restored, and the progress bar reflects the saved percentage.

**AC-73**: Loading a save with `voyage_state == ARRIVED` but where #6's knowledge state was not updated (crash between write steps) detects the inconsistency and re-sends the `route_travel_completed` event to #6.

**AC-74**: The write order on voyage end follows: (1) #8 hull damage, (2) #6 route knowledge update, (3) #11 EncounterContext, (4) #17 state change events, (5) save — a crash at any step is detectable on load and incomplete writes are retried.

**AC-75**: Loading a cross-version save where encounter tables have changed preserves resolved encounters as immutable history, while pending checks use the current version's encounter tables.

### Formula Boundary Tests

**AC-76**: Formula 1 boundary — `T_voyage` with zero encounter effects (`ΣT_flat = 0, ΣT_temp = 0`) equals exactly `T_distance / s_hull`: 60s for short+intact, ~133.3s for medium+damaged, 160s for medium+critical.

**AC-77**: Formula 1 boundary — `T_voyage` with maximum plausible encounter effects: safe route with 5 `gentle_crosswind` hits = 60 + 25 = 85s; risk route with mixed effects stays within the safety range (ΣT_flat <= 30s, ΣT_temp <= 15s).

**AC-78**: Formula 2 boundary — `Δ_hull` is validated on startup: values outside (-0.5, 0] are clamped, and if validation fails the system falls back to `T_base = 12s, Δ_hull = 0` with an alert.

**AC-79**: Formula 2 boundary — `T_check` has a hard minimum of `max(4s, T_base × 0.5)`, so even with worst-case config `T_check >= 4s`.

**AC-80**: Formula 3 boundary — `η_scout = 0.99` produces `N_preview = ⌊0.99 × 2⌋ = 1`, confirming the floor-rounding step difference: the discontinuity is at exactly `η_scout = 1.0`.

**AC-81**: Formula 4 boundary — `hull_integrity_departure = 3`, a single check hits `d_check = 6`: `hull_integrity_effective = max(0, 3 - 6) = 0`, the excess 3 damage is discarded.

**AC-82**: Formula 4 boundary — `hull_integrity_departure = 100`, 17 consecutive checks at max damage (d=6 each, total 102): `hull_integrity_effective = 0` after check 17, `FORCED_LANDING` triggers, and no negative hull value is ever produced.

**AC-83**: Formula 5 boundary — with `r_base = 0.30` and 10 checks, the probability all hidden tags remain unrevealed is `0.7^10 ≈ 2.8%`; in 1000 simulated voyages, at least some voyages should end with tags still hidden.

**AC-84**: Formula 5 boundary — `storm_eye_passage` at 10% probability appears roughly once per 10 checks on average; in 1000 simulated voyages of 10 checks each, the count of voyages with at least one `storm_eye_passage` is approximately 651 (binomial: 1 - 0.9^10 ≈ 65.1%).

### MVP Route Configuration

**AC-85**: `route.sky-reef-arc-01` (short, intact hull, η_scout=1.0): `T_voyage_base = 60s`, `N_checks = 5`, `T_check = 12s`, `N_preview = 2` (24s preview), `safe` tag visible from start, zero hidden tags.

**AC-86**: `route.storm-cut-01` (medium, intact hull, η_scout=1.0): `T_voyage_base = 120s`, `N_checks = 10`, `T_check = 12s`, `N_preview = 2` (24s preview), `storm` visible, `low-visibility` hidden with `?` marker.

**AC-87**: On `route.sky-reef-arc-01`, over 5 checks with the `safe` encounter table, the expected damage accumulated is low (0-2 from `minor_debris` at 20% per check).

**AC-88**: On `route.storm-cut-01`, over 10 checks with the `storm` encounter table, the expected damage accumulated is moderate — the hidden `low-visibility` tag creates meaningful uncertainty.

### Integration and Contract

**AC-89**: When `route_committed` arrives, the VoyageContext is constructed with all required fields: `route_id` and `destination_id` from the event, `hazard_tags` (reconciled against registry), hull band and integrity from #8, `η_scout` from #8, knowledge state and visible/hidden tag mapping from #6, and route static data from #1.

**AC-90**: On `ARRIVED`, the `EncounterContext` emitted to #11 contains: `route_id`, `destination_id`, voyage result (`arrived`), and the full list of resolved encounters with their types, damage values, and special effects.

**AC-91**: During `IN_PROGRESS`, every encounter check emits an `encounter_triggered` event to #17 containing the encounter type, damage value, and any special effects — the event fires at the moment the check resolves, not batched.

**AC-92**: Every state transition (PREPARING→IN_PROGRESS, IN_PROGRESS→ARRIVED/RETREATED/FORCED_LANDING) emits a `voyage_state_changed(old_state, new_state)` event to #17.

**AC-93**: When `lightning_proximity` successfully hits the scout module (20% roll passes), a real-time module damage event is emitted to #8 identifying the affected slot and the new `actual_state = damaged`.

**AC-94**: The system never directly modifies #8's module states, #6's knowledge states, or #1's registry data — all writes go through defined events and interfaces.

**AC-95**: When `can_depart()` is re-queried at the end of `VOYAGE_PREPARING` (TOCTOU defense) and returns `false`, the system strictly respects the current `can_depart()` answer and aborts preflight.

## Open Questions

| ID | 问题 | 影响范围 | 优先级 |
|----|------|---------|--------|
| OQ-01 | ~~目的地具体是什么？MVP 两条航线的 `destination_id` 注册表中为 TBD——在哪个系统确定？~~ **已修订 (2026-05-24)**：当前 demo player-facing 目的地为 `route.sky-reef-arc-01` → 雾灯残骸、`route.storm-cut-01` → 旧集市边缘；数据层 route/location ID 可保留历史值，但场景规格和 release handoff 必须使用当前 demo 场景名。 | 注册表 #1、探索 #11、场景构成 #19 | ✅ CLOSED WITH REVISION |
| OQ-02 | 燃料/能量系统在哪个 GDD 中定义？当前航行距离可行性仅由 hull 波段限制，缺少能量维度的规划取舍 | 航程可行性判断、航图 UI #9 | MEDIUM——Phase 2 需解决 |
| OQ-03 | NPC 航线与玩家委托货运的航程参数（船队规模、货物价值 vs 风险承受阈值）由谁定义？ | #14 空港/村镇、本系统的 VoyageContext 扩展 | LOW——Phase 3+ |
| OQ-04 | `gentle_crosswind` 的 +5s 和 `turbulence_zone` 的 -15% 速度惩罚在 60s 短途航程中的感知强度是否合适？需要实际 playtest 验证 | Tuning Knobs 节奏参数 | MEDIUM——需 playtest |
| OQ-05 | 侦察预览窗口最大 24s（2 个检查周期）是否足够？更长的预览（如 multiplier=3 → 36s）会让信息优势过于明显吗？ | Tuning Knobs 侦察参数 | LOW——Phase 2 调优 |
| OQ-06 | 航程中是否需要"遭遇日志"实时可查看（而非仅在航程结束后展示航行日志）？ | UI Requirements | LOW——取决于 UI/UX 设计 |
| OQ-07 | Web 平台标签页挂起期间的航程状态——是否需要在恢复时给玩家一个"你离开了 X 秒"的提示？ | 平台适配、UI | LOW——UX 细节
