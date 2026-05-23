# 探索 / 搜撤场景

> **Status**: In Review (Revision 2 applied 2026-05-03 — 5 blockers resolved: B1 F-11-04 batch extract_carried_to_storage atomicity, B2 build_threat_context environmental guard, B3 F-11-01 empty pool guard, B4 registry compute_loss max(1→0) sync, B5 4 GM commands added; R1 knowledge-gated descriptions added — dual-variant description_enhanced gated on #6 has_relevant_intel())
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-09
> **Implements Pillar**: 规划先于冒险; 未知带来温和压力
> **Platform Pivot Note**: ADR-0019 supersedes browser storage/lifecycle assumptions. Active exploration implementation targets desktop Godot .NET/C#; browser-tab edge cases should be read as desktop pause/quit/restart recovery requirements until individually refreshed.
> **Scene Physics Note**: Exploration scenes must include a Scene Physics Contract from `design/gdd/scene-physics-unit-system.md`. Physical world exploration is a bottom-layer play contract, not presentation-only polish.

## Overview

探索 / 搜撤场景是《云海织航》第一循环的第三步——玩家离开飞艇后的"短途出行"。在数据层，它消费航行系统（#10）在抵达目的地时发出的 `EncounterContext`，将其中的航线 ID、目的地 ID、航程结果和遭遇列表转化为一个可进入、可搜索、可拾取、可撤离的 2D 俯视探索点场景；在体验层，它是玩家在完成航线规划后真正"踏上未知地面"的时刻——玩家在一个手工设计且有物理规则的探索点中移动，观察环境、判断阻挡和可推动单位、理解特殊表面和危险区域、搜索可拾取资源、在容量限制下做出"带走什么"的取舍，并在风险升级或容量满载时决定撤离。

探索点是一个可复用的模板——MVP 中使用一个核心探索点布局，通过状态变体（未搜刮 / 已搜刮 / 危险变化）来产生不同的探索体验，而非制作多个独立地牢。搜索点、资源分布、风险标记和撤离锚点均由模板定义且可配置；`EncounterContext` 中的航程结果（安全抵达 vs 迫降）和遭遇列表影响探索点的初始状态——安全抵达意味着正常的探索起点，迫降意味着从坠机点开始、船体已受损、可能面临更紧迫的撤离压力。侦察模块的效率（η_scout）影响玩家在探索点内能提前看到多少风险信息——效率越高，越多风险点在进入前就被标注。

探索的产出是三类：资源 / 货物（通过 `carried` 池进入容量系统，撤离成功后归入飞艇仓库）、情报（隐藏标签被揭示，航线知识永久推进）、船体后果（探索中的威胁接触可能造成额外船体损伤或模块受损标记，在返航后结算）。探索系统不自行结算战斗——当玩家在探索点触发威胁（如遇到守卫、陷阱或环境危险），它将威胁上下文传递给战斗与威胁处理系统（#12）解决，并接收战斗结果来更新探索状态。

没有这个系统，航图上的目的地就只是一个名字——永远不会变成一个玩家可以用脚丈量、用手搜索、用物理空间判断撤离的真实地方。"规划先于冒险"（Pillar 1）会在抵达后断裂——玩家做了航线规划，但没有一个场景让规划的结果被体验。"未知带来温和压力"（Pillar 4）会失去它最重要的载体——探索点是未知变成已知的地方，是传闻变成经历的空间。

**明确不在 MVP 范围内**：战斗规则和结算（属于 #12 战斗与威胁处理）；第二个地牢节奏或多探索点类型；对航行结果的逆向修改；复杂的环境叙事或大规模程序生成；连续开放世界探索（MVP 是离散探索点）。

## Player Fantasy

探索 / 搜撤场景服务的核心幻想是：**你是一个能读懂废墟的修补者——你站在一个破碎的地方，能看出它缺少什么、哪里还有东西可以带走、以及下次应该带什么来。你不是来征服的，你是来回收、记录和判断的。**

### 识荒人：看见修复的路径（主基调）

探索点不是一个"刷 loot 的房间"——它是一个曾经有过功能的地方：一个废弃的气象站、一座被风暴掀掉半边屋顶的灯塔附属仓库、一处搁浅在浮岛边缘的旧货船残骸。玩家走进这里时，感受到的不是"里面有什么好东西"，而是"这个地方曾经是做什么的，它缺了什么才变成现在这样"。

锚定时刻不是在捡到稀有物品时——而是在走近一堆散落的零件、观察周围环境后，你意识到"这些零件正好是灯塔继电器需要的那些"。这个时刻，你不再是在"打怪掉装备"，你是在从遗忘中回收有用的东西。你知道你带回去的每一件东西都有一个具体的用途——修灯塔、补船体、换模块——不是卖金币，是让世界恢复运转。

这种感受是克制的——不是"发财了！"的亢奋，而是"找到了，这就够了"的平静满足。像老航海日志里的一行："本日探索：回收继电器零件 × 4，发现一块旧海图残片。灯塔修复仍需铜线。"

你在废墟中识别出灯塔继电器的零件——这是"识荒"。你把它带回飞艇，装进灯塔，看见航线重新亮起——那是"修补"。探索是修补的第一步：没有识别就没有修复。每一个搜索点旁边的简短描述——"一堆散落的电器零件，看起来像是通讯设备的残骸"——都在帮助你建立这种识别：这个地方曾经是做什么的，这些碎片曾经属于什么。

### 探图人：未知变成已知（第二层）

探索也是情报的源头。航图上的 `?` ——那个在航线规划时你只能猜测的东西——在探索中揭晓。你可能在废墟的墙壁上发现旧航海日志的一页，上面标着"此航线以西有稳定尾流"——于是你知道了 `route.storm-cut-01` 的 `low-visibility` 标签实际上是可以绕过的，只要你走西侧。也可能是你在搜索点发现了一块破损的标牌，上面写着"危险：不稳定浮石区"——你知道了某个风险标签的具体含义。

这个层面的满足不是"我变强了"，而是"我现在知道了"。探索结束后，你回到飞艇，打开航图——那个 `?` 变成了一个具体的标签。你不用再猜测，你有了答案。这就是 Pillar 4（未知带来温和压力）的正面兑现：未知的压力是温和的——它是谜题，不是威胁。每一次探索都让世界变得更可读。

### 撤退的判断——与搜索同样重要

Dark and Darker 的搜撤满足感来自"带回来了"，但《云海织航》的节奏更温和。探索中没有倒计时，没有缩圈——撤离是玩家的判断，不是被迫的逃命。当随身物品栏从 2/5 变成 5/5，当你看到一个风险标记但你不想冒这个险，当你知道再深入可能触发威胁但你今天的船体已经够破了——你做出撤离的决定。这个决定不是失败，而是船长在航海日志里写的"判断情况，决定返航"。

撤离的锚定时刻是：你站在撤离锚点上，回头看了一眼探索点——你带走了一些东西，你也标记了下次要来的地方。你知道自己还会回来，带着更多的准备、更好的模块、或只是更多的时间。

### 参考感受

参考老航海日志中的打捞记录、旧废墟调查报告的冷静笔调——不追求"史诗冒险"的高亢叙事，而是"我去了、我看了、我带回了这些、我知道了下一次该带什么"。探索结束后的感受不是"我征服了这个地方"，而是"这个地方我了解了，我带回了有用的东西，下一次我会更有方向。"

## Detailed Design

### Core Rules

**C1. 探索点模板**

MVP 使用一个探索点模板「云观站废墟」（Ruined Cloud-Watching Station），通过状态变体产生不同的探索体验。

- 布局：4 区域辐条式拓扑，约 50×35 单位（2D 俯视）
- 内容配置：6 个搜索点、2 个情报点、2+ 环境威胁点、1 个撤离锚点
- 风险模型：同心圆——越靠近中心，搜索质量越高但威胁密度越大；无全局计时器
- 状态变体：未搜刮（unlooted）、已搜刮（looted）、危险变化（danger-changed）
- 入场位置：安全抵达 → 入口区正常入场；迫降 → 坠机点入场

**C2. 探索会话流程（4 阶段）**

| 阶段 | 名称 | 玩家可操作 | 说明 |
|------|------|-----------|------|
| 1 | ARRIVING | 仅跳过 | 展示抵达描述（安全抵达或迫降），按任意键跳过 |
| 2 | EXPLORING | 自由移动、搜索、交互 | 核心探索阶段，无时间限制 |
| 3 | EXTRACTING | 不可移动（读条中） | 在撤离锚点触发，2-3 秒读条，可被打断 |
| 4 | DEPARTED | 无 | 结算阶段，产出写入各系统 |

**C3. 进入探索点**

探索系统消费 System #10（航行与路线风险）在航程抵达时发出的 `EncounterContext`：

- `voyage_result = "arrived"`（安全抵达）→ 玩家从入口区正常进入，船体无额外损伤
- `voyage_result = "forced_landing"`（迫降）→ 玩家从坠机点进入，船体已有损伤标记，部分区域可能因坠机影响而改变（如入口区被残骸封锁，需绕行）
- `resolved_encounters[]` 中与探索点相关的遭遇（如航线中触发的侦察情报）在进入时预载——已揭示的风险标签在进入前就标注在探索点地图上
- **Pool 5 初始状态**：进入探索点时，Pool 5（随身物品栏）保留玩家在飞艇准备阶段已装入的内容。玩家可携带 repair_kit、备品等进入探索，但这也意味着容量取舍会更早触发——准备越充分，越需要判断"带走什么"。进入探索后，Pool 5 的增减操作通过 System #5 接口进行。

**C4. 探索中的移动与交互**

- 玩家使用 System #4（玩家移动与交互）的 2D 俯视移动能力在探索点内移动
- 交互焦点系统用于靠近搜索点、情报点、威胁点、撤离锚点时触发交互
- 移动到相邻区域无加载——整个探索点（50×35 单位）为一个连续场景
- 每个探索点必须提供 System #20（场景单位物理设计）的 Scene Physics Contract。MVP 默认探索点为 `水平场景`：玩家在地面平面上下左右移动；阻挡残骸、可推动货箱、特殊表面、危险边缘、飞行/跳跃高度提示和动态物理行为必须在场景规格中明确。
- 物理空间是探索玩法的一部分。搜索点、撤离锚点、威胁点和情报点不应只靠 HUD 按钮推进；玩家应通过路径、遮挡、碰撞、可推动物、特殊表面或安全/危险边界来理解“能否到达、是否值得继续、如何撤离”。

**C5. 搜索机制**

- 每个搜索点可被搜索一次（每个会话中）。搜索触发一个检索动画（短暂停留），然后返回结果。
- **搜索点描述**：每个搜索点在模板配置中携带 `description` 和 `description_enhanced` 两个字段（每行文字 ≤40 字）。默认显示 `description`（如"一堆散落的电器零件，看起来像是通讯设备的残骸"）。当 System #6 `has_relevant_intel(sp_id)` 返回 true 时——即玩家已获取与该搜索点主题相关的情报（如研读过灯塔继电器图纸）——改为显示 `description_enhanced`（如"一堆电器零件——其中一些铜线接头和你见过的灯塔继电器图纸吻合"）。描述随状态变体变化（unlooted / looted / danger-changed 各有不同文字对）。这是"识荒人"幻想的机械支撑——准备改变感知，情报让废墟变得可读。
- **自由搜索**：搜索结果为空的搜索点不消耗搜索次数。玩家可以搜索探索点内的每一个搜索点，不会因为"翻到空的"而受到惩罚。这确保探索的温和压力——风险来自威胁，而非资源焦虑。
- **容量约束**：搜索获得的资源/货物进入随身物品栏（Pool 5，5 格，由 System #5 管理）。当 Pool 5 已满（5/5），玩家必须做出取舍——丢弃现有物品腾出空间，或放弃新发现的物品。
- 搜索点的内容由其配置和当前状态变体决定：
  - 未搜刮状态：完整的搜索点内容池（资源 + 可能的空结果）
  - 已搜刮状态：所有搜索点已枯竭（返回"这里已经被搜过了"）
  - 危险变化状态：部分搜索点被封锁（需绕路或使用工具），剩余搜索点的内容池可能变化（如部分资源被破坏）

**C6. 侦察与威胁预览**

侦察模块效率 η_scout（来自 System #8）决定玩家在探索点内能提前看到多少威胁信息：

| η_scout | 效果 |
|---------|------|
| 0 | 无预览——所有威胁点不可见，靠近时才触发 |
| (0, 1.0) | 存在预览——威胁点显示"此处有威胁"标记，但不显示具体类型。注：任何 η_scout > 0 均进入此档，包括 0.48（Scout 受损+critical band） |
| 1.0 | 完整预览——威胁点显示类型+位置（如"守卫哨兵，东北角"） |

预览标记在进入探索点时一次性加载，探索过程中不会新增预览（η_scout 在进入时快照）。

**C7. 威胁触发与处理**

- 当玩家靠近威胁点或在威胁点执行搜索/交互时，威胁可能触发
- 威胁分为两类：
  - **环境威胁**（如塌方、不稳定地板）：由探索系统自行处理——施加船体损伤或封锁路径
  - **守卫威胁**（如哨兵、自动防御装置）：将威胁上下文传递给 System #12（战斗与威胁处理），由战斗系统解决后返回结果
- 战斗结果回传后更新探索状态：守卫被清除 → 威胁点变为安全；玩家撤退 → 威胁点保持活跃
- 威胁接触可能造成船体损伤或模块受损标记，在撤离后结算

**C8. 撤离机制**

- 撤离锚点位于探索点的固定位置（入口区附近）
- 触发撤离：玩家在撤离锚点上执行"撤离"操作 → 开始 2-3 秒读条
- 读条可被打断：如果在读条期间受到威胁攻击或被环境伤害击中，读条中断，玩家可以选择应对威胁或再次尝试撤离
- 撤离成功 → 进入 DEPARTED 阶段，开始结算
- 玩家可在任何时刻选择撤离——背包满了、看到了不想冒的风险、船体状态太差——撤离是策略判断，不是失败

**C9. 探索结算（DEPARTED 阶段）**

撤离成功后自动结算：

1. **资源/货物**：Pool 5（carried）中的物品通过 `extract_carried_to_storage()` 转入飞艇仓库（System #5）。按 F-11-04 撤离损耗结算——λ_success（成功撤离）或 λ_forced（被迫撤退）决定损耗率。Unique 物品（Q=1，max_stack=1）不可被损耗（Pillar 4 约束）。
2. **情报**：探索中揭示的隐藏标签写入 System #6（玩家知识与情报）——航线知识永久推进。
3. **船体后果**：守卫威胁接触造成的船体损伤/模块受损标记已在威胁处理时由 #12 即时应用到 #8；环境威胁损伤由 #11 在触发时即时应用到 #8。DEPARTED 阶段汇总展示本次探索的全部损伤（不做二次写入），确保玩家在探索中始终看到实时船体状态。
4. **探索点状态更新**：当前探索点根据本次探索结果更新状态变体——如果所有搜索点被搜刮 → looted；如果触发了环境威胁且未清除 → danger-changed。

**C10. 探索点状态变体生命周期**

| 状态 | 触发条件 | 效果 | 持续 |
|------|---------|------|------|
| unlooted | 默认（首次访问） | 全搜索点可用，威胁正常分布 | 直到被搜刮或触发危险 |
| looted | 所有搜索点被搜刮完毕 | 搜索点标记为空，环境描述改变 | 直到被重置（玩家带工具/模块回来修复） |
| danger-changed | 触发环境威胁且未清除 | 部分路径被封锁，新增环境危险 | 永久直到玩家清除威胁（D5） |

### States and Transitions

**探索会话状态机**

```
                 ┌─────────┐
                 │ARRIVING │
                 └────┬────┘
                      │ 按任意键 / 自动跳过
                      ▼
                 ┌─────────┐
        ┌───────│EXPLORING│◄──────────────┐
        │       └────┬────┘               │
        │            │                    │
        │   触发撤离锚点                   │ 威胁处理完成 / 返回探索
        │            ▼                    │
        │       ┌──────────┐   被打断     │
        │       │EXTRACTING│──────────────┘
        │       └────┬─────┘
        │            │ 读条完成
        │            ▼
        │       ┌─────────┐
        │       │DEPARTED │
        │       └─────────┘
        │
        └── 玩家可在 EXPLORING 阶段自由循环：移动 → 搜索 → 遭遇威胁 → 返回移动
```

**玩家在探索中的子状态**

| 子状态 | 触发 | 可操作 |
|--------|------|--------|
| idle | 默认 | 移动、交互 |
| moving | 玩家输入移动 | — |
| searching | 触发搜索点 | 等待动画完成 |
| threatened | 触发威胁 | 取决于威胁类型：逃跑/战斗（→ System #12） |
| extracting | 触发撤离锚点 | 等待读条，可被打断 |

### Interactions with Other Systems

**上游（本系统依赖）**

| 系统 | 数据流入 | 数据流出 |
|------|---------|---------|
| #10 航行与路线风险 | `EncounterContext` {route_id, destination_id, voyage_result, resolved_encounters[]} | — |
| #20 场景单位物理设计 | Scene Physics Contract: horizontal scene type, collision, occlusion, unit scale, special surfaces, physical behaviors, recovery rules | — |
| #8 飞艇模块与船体状态 | η_scout（侦察效率）, `can_depart()`（检查是否可以出发） | 船体损伤、模块受损标记（结算时写入） |
| #5 资源/货物与容量 | Pool 5（carried，5 格）读写 | `extract_carried_to_storage()` 结算时调用 |
| #4 玩家移动与交互 | 2D 俯视移动、交互焦点系统、Use 入口 | — |
| #6 玩家知识与情报 | `has_relevant_intel(sp_id)` — 搜索点描述增强门控 | 情报揭示（隐藏标签 → 已知标签） |

**下游（依赖本系统的系统）**

| 系统 | 数据流入 | 数据流出 |
|------|---------|---------|
| #12 战斗与威胁处理 | 威胁上下文 {threat_type, threat_id, position, encounter_params} | 战斗结果 {outcome, hull_damage, module_damage, resources_consumed, knockback, retreat_flagged} |
| #5 资源/货物与容量 | 探索拾取的资源/货物（含 `currency.cloud-coins` 云海币，通过 `add_loot()` 进入玩家货币余额） | — |
| #8 飞艇模块与船体状态 | 探索造成的船体损伤、模块受损标记 | — |
| #6 玩家知识与情报 | 探索揭示的情报 | — |
| #14 空港/村镇状态与集市交易 | 探索产出的货币（`currency.cloud-coins`）为集市购买提供资金来源——MVP 中货币的唯一获取途径 | — |

## Formulas

### F-11-01 搜索产出投骰 `search_yield`

玩家搜索一个搜索点时，判定是否为空、产出品质档位、具体物品和数量。

```
search_yield(sp_id, zone, state):
    if state == "looted":
        return {items: [], is_empty: true, search_consumed: false,
                message: "这里已经被搜过了"}

    P_empty = empty_chance[state][zone]
    if random() < P_empty:
        return {items: [], is_empty: true, search_consumed: false}

    tier = weighted_random(quality_weights[state][zone])
    pool = loot_pool[sp_id][tier]

    // 空池守卫：配置错误时回退为空结果，保护自由搜索保证（C5）
    if len(pool) == 0:
        return {items: [], is_empty: true, search_consumed: false,
                message: "这里似乎还能找到些什么，但已经什么都没有了——或许下次再来？"}

    n = random_int(draw_count[tier].min, draw_count[tier].max)
    selected = sample_without_replacement(pool, min(n, len(pool)))

    return {items: selected, is_empty: false, search_consumed: true}
```

**变量表**：

| 变量 | 类型 | 值域 | 说明 |
|------|------|------|------|
| `sp_id` | string | 模板定义的搜索点 ID | 搜索点唯一标识 |
| `zone` | enum | {A_core, B_inner, C_mid, D_outer} | 搜索点所在区域（A=核心, B=内圈, C=中圈, D=外圈） |
| `state` | enum | {unlooted, looted, danger-changed} | 探索点当前状态变体 |
| `empty_chance[state][zone]` | float | [0.0, 1.0] | 空结果概率，按状态×区域索引 |
| `quality_weights[state][zone]` | dict | 权重和=1.0 | {Poor: w1, Common: w2, Uncommon: w3} |
| `tier` | enum | {Poor, Common, Uncommon} | 品质档位，MVP 三档 |
| `loot_pool[sp_id][tier]` | list | 每档 1-5 条目 | 该搜索点在该档位的可掉落资源列表 |
| `draw_count[tier]` | {min, max} | min≥1, max≤5 | 该档位产出的物品种类数范围 |

**推荐默认值（unlooted 状态）**：

| 区域 | empty_chance | Poor 权重 | Common 权重 | Uncommon 权重 |
|------|-------------|-----------|-------------|---------------|
| A_core | 0.00 | 0.20 | 0.45 | 0.35 |
| B_inner | 0.05 | 0.25 | 0.45 | 0.30 |
| C_mid | 0.20 | 0.35 | 0.40 | 0.25 |
| D_outer | 0.35 | 0.50 | 0.30 | 0.20 |

| tier | draw_count {min, max} |
|------|----------------------|
| Poor | {1, 2} |
| Common | {1, 2} |
| Uncommon | {1, 2} |

**danger-changed 修正**：所有区域 empty_chance +0.15，所有品质权重向 Poor 偏移（Uncommon 权重 ×0.5，差额加给 Poor）。

**搜索点区域分布**（云观站废墟，共 6 个）：

| 区域 | 搜索点数 | 情报点数 |
|------|---------|---------|
| A_core | 1 | 1 |
| B_inner | 1 | 1 |
| C_mid | 2 | 0 |
| D_outer | 2 | 0 |

**演算示例**（unlooted，搜索 A_core 的 search_point.cloudwatch.core-01）：

- `empty_chance[unlooted][A_core] = 0.00` → 非空
- `weighted_random({Poor:0.20, Common:0.45, Uncommon:0.35})` → 抽中 Uncommon
- `loot_pool["search_point.cloudwatch.core-01"][Uncommon] = [("cloud_crystal", [2,4]), ("ancient_relay_part", [1,2])]`
- `draw_count[Uncommon] = {1,2}`, `random_int(1,2)` → 2
- 不放回抽取 2 个条目，数量随机：cloud_crystal ×3, ancient_relay_part ×1
- **结果**: `{items: [("cloud_crystal", 3), ("ancient_relay_part", 1)], is_empty: false, search_consumed: true}`
- **repair_kit 额外判定**：主搜索产出结算后，独立判定 `random() < repair_kit_drop_rate` (MVP 值 0.25)。若通过，追加 `repair_kit ×1` 到产出列表。该判定与品质投骰无关——即使是 Poor 品质档位或空结果，仍可能产出 repair_kit。预期单次探索 1-2 个（6 搜索点 × 0.25 = 1.5 期望值）。

---

### F-11-02 威胁触发判定 `threat_trigger`

判定玩家靠近或在威胁点上交互时，威胁是否触发。

```
threat_trigger(threat_point, trigger_type, player_pos):
    if not threat_point.is_active:
        return {triggered: false}

    if trigger_type == "interaction":
        return {triggered: true,
                context: build_threat_context(threat_point, "interaction")}

    if trigger_type == "proximity":
        dist = distance(player_pos, threat_point.position)
        if dist > threat_point.trigger_radius:
            return {triggered: false}
        P = trigger_prob[threat_point.threat_category]
        if random() < P:
            return {triggered: true,
                    context: build_threat_context(threat_point, "proximity")}

    return {triggered: false}
```

**变量表**：

| 变量 | 类型 | 值域 | 说明 |
|------|------|------|------|
| `threat_point.is_active` | bool | {false, true} | 威胁是否仍活跃（清除后为 false）。持续存在于本探索会话中，并跨 save/load 持久化（写入探索点快照）。 |
| `threat_point.threat_category` | enum | {environmental, guard} | 环境威胁 vs 守卫威胁 |
| `threat_point.trigger_radius` | float | [1.0, 8.0] | 触发半径（单位），环境 2-3，守卫 4-6 |
| `threat_point.position` | Vector2 | — | 探索点内威胁的世界空间位置 |
| `threat_point.facing` | Vector2 | 单位向量 | 威胁的朝向方向。用于击退方向回退（EC-12-06）。由探索点模板数据定义。 |
| `trigger_type` | enum | {proximity, interaction} | 触发来源 |
| `trigger_prob` | dict | [0.0, 1.0] | 靠近触发概率，按类别 |

| threat_category | trigger_prob (proximity) | 处理方式 |
|-----------------|------------------------|---------|
| environmental | 1.0（必触发） | 探索系统自行处理：施加 hull_damage 或封锁路径 |
| guard | 0.70 | 传递 threat_context 至 System #12 |

环境威胁踩到即触发——塌方不会"概率塌方"。守卫以概率抽象巡逻/视野概念。

**输出范围**：`triggered` ∈ {false, true}

**演算示例**：

- 守卫威胁，坐标 (18,22)，trigger_radius=5.0，is_active=true
- 玩家靠近到 (16,19)，`distance = 3.61 ≤ 5.0`
- `trigger_prob["guard"] = 0.70`, `random() = 0.52 < 0.70`
- **结果**: triggered=true，传递 context 至 #12

**`build_threat_context` 工厂函数：**

`build_threat_context(threat_point, trigger_type)` 构建传递给 #12 的 `threat_context` 结构体。在 #11 的 F-11-02 判定触发后调用。

```
build_threat_context(threat_point, trigger_type):
    // 环境威胁由 #11 自行处理（施加 hull_damage 或封锁路径，见 C7）——不构建战斗上下文
    if threat_point.threat_category == "environmental":
        return {
            threat_type:      "environmental",
            threat_id:        threat_point.id,
            position:         threat_point.position,
            encounter_params: null  // 无战斗参数——#11 直接 apply_hull_damage 或 block_path
        }

    // 守卫威胁：从 #1 Registry 的静态威胁配置查询战斗参数（#12 C8 威胁配置表）
    config = THREAT_CONFIG_TABLE[threat_point.threat_category]

    encounter_params = {
        threat_category:      threat_point.threat_category,
        full_damage_min:      config.full_damage_range[0],
        full_damage_max:      config.full_damage_range[1],
        module_damage_chance: config.module_damage_chance,
        emergency_cost:       config.emergency_cost,
        knockback_distance_tanked:   config.knockback_distance_tanked,
        knockback_distance_retreat:  config.knockback_distance_retreat,
        can_be_suppressed:    config.can_be_suppressed
    }

    return {
        threat_type:      threat_point.threat_category,
        threat_id:        threat_point.id,
        position:         threat_point.position,
        encounter_params: encounter_params
    }
```

`THREAT_CONFIG_TABLE` 由 #1（内容数据与状态注册表）提供。查询键为 `threat_category`。`trigger_radius` 不包含在 `encounter_params` 中——它保留在 #11 的 `threat_point` 中，用于计算重触发距离。

---

### F-11-03 侦察预览映射 `scout_preview_level`

将侦察效率 η_scout 映射为探索点内的威胁预览等级。

```
scout_preview_level(η_scout):
    if η_scout <= 0:
        return PREVIEW_NONE
    elif η_scout >= 1.0:
        return PREVIEW_FULL
    else:
        return PREVIEW_PRESENCE
```

**变量表**：

| 变量 | 类型 | 值域 | 说明 |
|------|------|------|------|
| `η_scout` | float | {0, 0.48, 0.6, 0.76, 0.8, 0.95, 1.0} | 侦察模块有效效率，来自 System #8 |
| `PREVIEW_NONE` | — | — | 无预览：威胁点不可见，靠近时才触发 |
| `PREVIEW_PRESENCE` | — | — | 存在预览：威胁点显示红色感叹号，不显示类型 |
| `PREVIEW_FULL` | — | — | 完整预览：显示类型+名称（"守卫哨兵·东北角"） |

**η_scout 来源速查**（from System #8）：

| 模块状态 + 船体波段 | η_scout | 预览等级 |
|---------------------|---------|---------|
| 无侦察模块 | 0 | PREVIEW_NONE |
| Scout 受损 + critical band | 0.48 | PREVIEW_PRESENCE |
| Scout 受损 + intact/damaged band | 0.6 | PREVIEW_PRESENCE |
| Scout 正常 + critical band | 0.76-0.8 | PREVIEW_PRESENCE |
| Scout 正常/安装 + intact/damaged band | 0.95-1.0 | PREVIEW_PRESENCE / PREVIEW_FULL |

**输出范围**：{PREVIEW_NONE, PREVIEW_PRESENCE, PREVIEW_FULL}

**演算示例**：Scout 已安装，船体 intact → η_scout=1.0 → `PREVIEW_FULL`

---

### F-11-04 撤离损耗结算 `extraction_loss_settlement`

DEPARTED 阶段，对 Pool 5 中每堆物品独立判定撤离损耗。本函数接收来自 #12 的 `retreat_flagged` 和探索会话期间累积的任何战斗结果。

```
extraction_loss_settlement(carried_stacks, retreat_flagged):
    result = {transferred: [], lost: [], total_lost_qty: 0}
    transfer_batch = []  // 组装批量转移清单，一次性提交至 #5

    for each stack in carried_stacks:
        if stack.is_unique and stack.max_stack == 1:
            transfer_batch.append({resource_id: stack.resource_id, quantity: stack.quantity})
            result.transferred.append({id: stack.resource_id, qty: stack.quantity, lost: 0})
            continue

        λ = retreat_flagged ? λ_forced : λ_success

        loss_qty = compute_loss(stack.quantity, λ)
        retained_qty = stack.quantity - loss_qty
        transfer_batch.append({resource_id: stack.resource_id, quantity: retained_qty})

        if loss_qty > 0:
            destroy(stack.resource_id, loss_qty)
            result.lost.append({id: stack.resource_id, qty: loss_qty})
            result.total_lost_qty += loss_qty

    // 批量原子转移：全成功或全失败（#5 规则 14）
    extract_carried_to_storage(transfer_batch)

    return result


compute_loss(Q, λ):
    if Q <= 1:
        return 0
    if λ <= 0:
        return 0
    return min(Q - 1, max(0, ceil(Q × λ)))
```

**变量表**：

| 变量 | 类型 | 值域 | 说明 |
|------|------|------|------|
| `carried_stacks` | list | 0-5 堆 | Pool 5 撤离时快照 |
| `retreat_flagged` | bool | {false, true} | 若在当前探索会话期间，任何战斗结果设置了 `retreat_flagged = true`，则为 true。由探索系统在收到 #12 的 `combat_result` 时存储为会话级布尔值。当 `retreat_flagged = true` 时使 λ_forced 生效；当为 false 时使用 λ_success。 |
| `λ_success` | float | [0.0, 0.10] | 成功撤离损耗率，MVP 默认 0.08 |
| `λ_forced` | float | [0.10, 0.50] | 被迫撤退损耗率，MVP 默认 0.25 |
| `compute_loss(Q, λ)` | int | [0, Q-1] | 单堆损耗量。λ≤0 时返回 0（无损）；λ>0 时最小可能为 0，保证每堆至少保留 1 |
| `stack.is_unique` | bool | {false, true} | Q=1 且 max_stack=1 |

**Pillar 4 硬约束**：Unique 物品（Q=1, max_stack=1）永不损耗，直接全量转移。

**输出范围**：`total_lost_qty` ∈ [0, total_carried_qty)。λ_success ≤ 0 时 total_lost_qty = 0。每堆至少保留 1（Q ≤ 1 时无损，λ ≤ 0 时无损）。

**演算示例**（SUCCESSFUL_EXTRACTION，λ_success=0.08）：

| 物品 | 数量 | Unique? | compute_loss | 保留 | 损耗 |
|------|------|---------|-------------|------|------|
| basic_supply | 20 | no | min(19, max(0, ceil(20×0.08))) = 2 | 18 | 2 |
| cloud_crystal | 1 | no | Q≤1 → 0 | 1 | 0 |
| cloud_crystal ×3 | 3 | no | min(2, max(0, ceil(3×0.08))) = max(0, 1) = 1 | 2 | 1 |
| intel.ancient-log | 1 | **yes** | 跳过 | 1 | 0 |
| repair_kit | 12 | no | min(11, max(0, ceil(12×0.08))) = 1 | 11 | 1 |

- **结果**: transferred 18+1+2+1+11=33, lost 4, total_lost_qty=4
- 注：λ_success=0.0 时，compute_loss 对所有 Q 返回 0（无损）

---

### F-11-05 状态变体转换 `exploration_state_variant_transition`

探索结束时，根据本次探索结果判定探索点的持久状态变化。

```
state_variant_transition(current_state, all_searched, env_threat_active):

    if current_state == "unlooted":
        if env_threat_active:
            return "danger-changed"
        elif all_searched:
            return "looted"
        else:
            return "unlooted"

    if current_state == "looted":
        return "danger-changed" if env_threat_active else "looted"

    if current_state == "danger-changed":
        if not env_threat_active and all_searched:
            return "looted"
        elif not env_threat_active and not all_searched:
            return "unlooted"
        else:
            return "danger-changed"
```

**变量表**：

| 变量 | 类型 | 值域 | 说明 |
|------|------|------|------|
| `current_state` | enum | {unlooted, looted, danger-changed} | 探索前状态 |
| `all_searched` | bool | {false, true} | 模板内全部 6 个搜索点均已被搜索 |
| `env_threat_active` | bool | {false, true} | 本次有环境威胁被触发且未清除 |

**状态转换表**：

| current | all_searched | env_threat | → new |
|---------|-------------|-----------|-------|
| unlooted | false | false | unlooted |
| unlooted | true | false | **looted** |
| unlooted | * | true | **danger-changed** |
| looted | * | true | **danger-changed** |
| looted | * | false | looted |
| danger-changed | false | false | **unlooted** |
| danger-changed | true | false | **looted** |
| danger-changed | * | true | danger-changed |

**优先规则**：`env_threat_active=true` 优先进入 danger-changed——环境威胁改变探索点结构。守卫威胁不影响持久状态（由 #12 局内处理）。

**输出范围**：{unlooted, looted, danger-changed}

**演算示例**：

- 首次探索，全搜 6 点，无威胁：unlooted → **looted**
- 触发塌方未清除，仅搜 3 点：unlooted → **danger-changed**
- 重返 danger-changed，清除威胁，搜完余点：danger-changed → **looted**

---

### F-11-06 情报点产出 `intel_yield`

情报点不参与搜索投骰——固定产出 1 个 intel 物品。

```
intel_yield(intel_point_id):
    return {items: [(intel_point_config[intel_point_id].intel_id, 1)],
            is_empty: false}
```

| 变量 | 类型 | 值域 | 说明 |
|------|------|------|------|
| `intel_point_id` | string | 模板定义的情报点 ID | 情报点唯一标识 |
| `intel_point_config[id].intel_id` | string | intel.* | 该情报点产出的情报物品 ID（Unique, Q=1） |

每个情报点在每个会话中只能被交互一次。

---

### 公式汇总

| # | 公式名 | Registry Key | 类型 | 关键依赖 |
|---|--------|-------------|------|---------|
| F-11-01 | 搜索产出投骰 | `search_yield` | 概率抽取 | 模板配置（loot_pool） |
| F-11-02 | 威胁触发判定 | `threat_trigger` | 距离+概率 | System #12 |
| F-11-03 | 侦察预览映射 | `scout_preview_level` | 分段阈值 | System #8 (η_scout) |
| F-11-04 | 撤离损耗结算 | `extraction_loss_settlement` | 比例损耗 | System #5 (Pool 5, EC-05) |
| F-11-05 | 状态变体转换 | `exploration_state_variant_transition` | 分段状态机 | 模板配置 |
| F-11-06 | 情报点产出 | `intel_yield` | 固定产出 | System #6 |

## Edge Cases

### E1. 会话中断与恢复

**EC-11-01: 浏览器标签页在 EXPLORING 阶段被关闭**
- 触发：玩家处于 EXPLORING 阶段，标签页关闭/崩溃/导航离开
- 处理：持久化快照在每次搜索后、威胁结算后、进入 EXTRACTING 时写入。恢复时加载最新快照，玩家从当前区域入口重新开始。已搜索的搜索点保持已搜索，已触发的威胁保持已触发/清除。Pool 5 恢复至快照状态。最近一次快照后的进度丢失。
- 玩家感知：是。重新进入显示"你在探索中中断了。部分最近的进度可能丢失。"
- 依赖：#3 本地存档与世界状态持久化

**EC-11-02: 浏览器标签页在 EXTRACTING 阶段被关闭**
- 触发：撤离读条中（2-3秒），标签页被关闭
- 处理：读条是原子操作——未完成视为未发生。恢复时阶段为 EXPLORING，玩家位置在撤离锚点旁，需重新触发撤离。
- 玩家感知：是。玩家发现自己站在撤离锚点旁，需重新撤离。
- 依赖：#3

**EC-11-03: DEPARTED 结算期间 localStorage 写入失败**
- 触发：结算写入时浏览器存储配额满或其他写入错误
- 处理：结算采用事务模式——内存中组装完整结算包，一次性写入。失败则自动重试（1s/2s/4s/8s，最多 4 次）。全部失败后显示"保存失败。你的探索收获暂时保留。请检查浏览器存储空间后点击重试。"按钮。结算包保留在内存中直到页面关闭。
- 玩家感知：是。错误提示 + 手动重试按钮。
- 依赖：#3, #5, #8, #6

### E2. 容量边界

**EC-11-04: Pool 5 已满（5/5），搜索结果包含物品**
- 触发：Pool 5 全部占用，搜索返回非空结果
- 处理：弹出交互界面显示搜索结果，提示"随身物品栏已满（5/5）。请选择：丢弃现有物品腾出空间，或放弃这些物品。"选项：(a) 丢弃现有物品（永久丢失），(b) 放弃新物品（永久丢失），(c) 如果新物品与某格同 resource_id 且可堆叠 → 合并选项。关闭弹窗不做选择 = 保留现有物品，放弃新物品（保守默认行为，避免误触丢弃）。
- 注：取舍界面打开时探索暂停，避免玩家在决策时被威胁触发打断。交互界面应展示选择前后的背包状态对比（丢弃 X → 空出 1 格，可装入 Y）。
- 玩家感知：是。核心取舍体验。
- 依赖：#5

**EC-11-05: Pool 5 已满，情报点产出 Unique 物品**
- 触发：Pool 5 满，intel_yield() 返回 Q=1 Unique 物品
- 处理：同 EC-11-04 但附加提示"此情报为唯一物品，放弃后将无法再次获取。"放弃的情报永久丢失，情报点标记为已交互。
- 玩家感知：是。高压力取舍。
- 依赖：#5, #9

**EC-11-06: Pool 5 有可堆叠物品，搜索产出同一资源**
- 触发：搜索产出 resource_id 匹配现有格位，且 stackable=true
- 处理：自动合并至 max_stack。溢出的数量需要新格位；若无空闲格位则按 EC-11-04 处理。合并是静默的，不弹窗。
- 玩家感知：部分感知。仅溢出时弹窗。
- 依赖：#5

### E3. 依赖系统异常

**EC-11-07: EncounterContext 缺失或格式错误**
- 触发：enter_exploration() 收到 null、缺失 route_id/destination_id/voyage_result
- 处理：进入前校验。失败则构建 fallback context：`{route_id: "unknown", destination_id: "cloudwatch-ruins-fallback", voyage_result: "arrived", resolved_encounters: []}`。正常进入探索，同时记录内部错误日志。
- 玩家感知：否（正常进入，fallback 不会导致不合理的迫降体验）。
- 依赖：#10

**EC-11-08: 探索中船体 hull 变为 0**
- 触发：环境威胁触发后损伤导致 hull=0
- 处理：探索系统不自行终止探索。HUD 显示"船体严重损毁"警告。撤离锚点仍然可用——玩家可带着 loot 撤离。hull==0 的全局后果（是否不可继续航行等）由 #8 负责。
- 玩家感知：是。HUD 严重警告。
- 依赖：#8

**EC-11-09: Pool 5 状态不一致**
- 触发：occupied_slots 计数与实际格位占用量不匹配（因持久化损坏等）
- 处理：在进入探索点、每次搜索后、触发撤离锚点时执行一致性扫描。以实际格位状态为准修正 occupied_slots。静默修复，不通知玩家。
- 玩家感知：否。
- 依赖：#5

### E4. 威胁交互边缘

**EC-11-10: 多个威胁同时触发**
- 触发：玩家踏入两个以上威胁点触发半径重叠区域
- 处理：依次处理，不并行。优先级：(1) 环境威胁 > 守卫威胁，(2) 同类型中距离近者优先，(3) 同距离按 threat_id 字典序。每个结算完毕后处理下一个。环境威胁损伤可累积。
- 玩家感知：是。依次经历每个威胁的触发序列。
- 依赖：#12, #8

**EC-11-11: 威胁在撤离读条期间触发**
- 触发：EXTRACTING 阶段中，守卫或环境威胁满足触发条件
- 处理：读条被打断，进度重置为 0，阶段回到 EXPLORING → threatened 子状态。威胁按 F-11-02 处理。威胁处理完毕后玩家仍在撤离锚点旁，可再次尝试撤离。重复打断无额外惩罚——由玩家判断决定先处理威胁还是绕行。
- 玩家感知：是。读条打断动画+音效。锚点非绝对安全区。
- 依赖：#12

**EC-11-12: 守卫威胁传递给 #12，但 #12 尚未实现或不可用**
- 触发：F-11-02 判定守卫触发，但 #12 接口返回 unavailable
- 处理：守卫威胁不触发、不造成伤害。威胁点保持 is_active=true，记录日志。当 #12 实现后，守卫威胁获得完整的战斗处理流程。MVP 早期可在探索点模板中减少守卫威胁数量，优先使用环境威胁。
- 玩家感知：否。守卫威胁在 #12 就绪前处于 inert 状态。
- 依赖：#12

**EC-11-13: 威胁清除后玩家重新进入威胁区域**
- 触发：之前已清除的威胁（is_active=false），玩家再次走过其原始触发半径
- 处理：F-11-02 第一步检查 is_active=false → 直接返回 {triggered: false}。该区域在本会话内永久安全。η_scout 预览标记在进入时快照，不会因威胁清除而动态更新（已知轻度 UI 不一致，MVP 接受）。
- 玩家感知：否。
- 依赖：#12

### E5. 探索点状态边缘

**EC-11-14: 重复访问 looted 探索点**
- 触发：抵达状态为 looted 的探索点
- 处理：正常进入（ARRIVING → EXPLORING）。所有搜索点返回"这里已经被搜过了"。情报点保持枯竭。威胁点正常活跃。撤离锚点可用。ARRIVING 描述变化。
- 玩家感知：是。搜索交互返回已搜刮消息。
- 依赖：无

**EC-11-15: 首次进入 danger-changed 探索点**
- 触发：上次探索触发了环境威胁且未清除，本次进入 danger-changed
- 处理：加载 danger-changed 变体布局——封锁路径、新增 1-2 环境威胁点、搜索点 empty_chance +0.15、品质权重向 Poor 偏移。ARRIVING 描述变为"废墟看起来比上次更糟——某处发生了二次塌方，空气中仍有粉尘。"
- 玩家感知：是。布局视觉变化、新威胁标记、搜索品质降低。
- 依赖：模板配置

**EC-11-16: 所有搜索点+情报点全枯竭后玩家仍在探索中**
- 触发：6/6 搜索点已搜，2/2 情报点已交互
- 处理：不强制退出。玩家仍可挑战未清除威胁、查看环境、随时撤离。无"你已完成探索"的 UI 提示——由玩家自己判断撤离时机。
- 玩家感知：部分感知。HUD 可显示搜索点计数，但无自动事件。
- 依赖：无

**EC-11-17: 世界修复与探索点状态独立**
- 触发：玩家在 #13 中完成灯塔修复，但探索点状态为 looted 或 danger-changed
- 处理：灯塔修复只影响航线解锁和世界反馈——不影响探索点的搜索点状态或威胁状态。探索点的状态变体仅由本系统的 F-11-05 根据探索行为判定。两个系统独立运作。
- 玩家感知：否。修复后重新进入探索点，状态与修复前一致。
- 依赖：#13（仅位置关联，无接口依赖）

### E6. 撤离边缘

**EC-11-18: 在威胁触发半径附近但未触发时撤离**
- 触发：玩家在已知威胁点附近但未触发，成功撤离
- 处理：正常撤离。"在危险边缘溜走"是有效策略。MVP 威胁无追踪/激怒 AI。
- 玩家感知：否。正常撤离流程。
- 依赖：无

**EC-11-19: Pool 5 为空时撤离**
- 触发：撤离时随身物品栏完全为空
- 处理：正常撤离流程。extraction_loss_settlement() 输入为空，无物品处理。结算摘要显示"本次探索未带回任何物品。"不施加惩罚。
- 玩家感知：是。结算摘要明确显示 0 物品。
- 依赖：#5

### E7. 浏览器特定

**EC-11-20: 窗口失焦 / 长时间闲置**
- 触发：桌面窗口失焦、最小化，或 >30 分钟无交互
- 处理：探索无全局计时器，无惩罚。(a) 搜索动画中 → 跳过动画直接显示结果（F-11-01 在动画前已计算），(b) 撤离读条中 → 读条中断并重置（计时器在暂停期间不可靠），需重新撤离，(c) ARRIVING 中 → 跳过描述文本直接进入 EXPLORING，(d) EXPLORING → 恢复为 idle。
- 玩家感知：部分感知。撤离读条被打断（有反馈），搜索动画被跳过（无反馈）。
- 依赖：#2

**EC-11-21: 磁盘空间不足——持久化失败**
- 触发：持久化写入失败（磁盘满或权限不足）
- 处理：HUD 显示非阻塞警告"⚠ 存储空间不足，探索进度可能无法保存。"30 秒内不重复显示。DEPARTED 结算写入失败时按 EC-11-03 重试逻辑处理。探索仍可继续但不保证恢复。
- 玩家感知：是。HUD 持久警告图标。
- 依赖：#3

## Dependencies

### 上游依赖（本系统依赖）

| 系统 | 依赖内容 | 关键接口 | 状态 |
|------|---------|---------|------|
| #4 玩家移动与交互 | 2D 俯视移动、交互焦点系统、Use 入口 | 移动能力、焦点检测、交互触发 | Required |
| #5 资源/货物与容量 | Pool 5（carried，5 格）读写、`extract_carried_to_storage()`、`extraction_loss_ratio`、max_stack 判定 | `add_loot()`, `extract_carried_to_storage()`, `discard()` | Required |
| #8 飞艇模块与船体状态 | η_scout 侦察效率值、`can_depart()`、船体损伤写入 | η_scout, `can_depart()`, `apply_hull_damage()` | Required |
| #10 航行与路线风险 | `EncounterContext` {route_id, destination_id, voyage_result, resolved_encounters[]} | `EncounterContext` 消费 | Required |

### 下游依赖（依赖本系统）

| 系统 | 依赖内容 | 关键接口 | 状态 |
|------|---------|---------|------|
| #5 资源/货物与容量 | 探索拾取的资源/货物通过 Pool 5 进入容量系统 | `add_loot()` 调用 | Required |
| #8 飞艇模块与船体状态 | 探索中的威胁接触造成船体损伤或模块受损标记（结算时写入） | `apply_hull_damage()`, `apply_module_damage()` | Required |
| #6 玩家知识与情报 | 探索揭示的隐藏标签 → 写入情报系统，航线知识永久推进 | `reveal_tag()`, `intel_writes` | Required |
| #12 战斗与威胁处理 | 守卫威胁触发时传递威胁上下文，接收战斗结果。`combat_result.retreat_flagged`（当为 true 时）被存储为探索会话级布尔值 `session.retreat_flagged`，并传递给 F-11-04。多次 true 结果不叠加（布尔值）。 | `threat_context → #12`, `combat_result ← #12` | Required |

### 双向依赖校验

- **#5 ↔ #11**：#5 定义 Pool 5 和 `extract_carried_to_storage()`，#11 定义探索拾取和撤离结算消费这些接口。
- **#8 ↔ #11**：#8 定义 η_scout 和船体损伤接口，#11 定义侦察预览消费 η_scout、探索威胁写入船体损伤。
- **#10 → #11**：#10 定义 `EncounterContext` 在航程抵达时发出，#11 在 ARRIVING 阶段消费。#10 的 GDD 已列 #11 为下游。
- **#11 → #12**：#12 定义威胁上下文和战斗结果接口（`combat_result` 包含 `outcome, hull_damage, module_damage, resources_consumed, knockback, retreat_flagged`），#11 定义守卫威胁触发时传递上下文、接收组合结果、存储 `retreat_flagged` 为会话状态，并将击退应用于玩家位置。#12 的 GDD 已列 #11 为上游。✅ 已对齐。
- **#11 → #6**：#6 定义情报揭示接口，#11 定义探索中的情报产出。#6 的 GDD 需列 #11 为上游。

### 间接依赖

| 系统 | 关系 | 说明 |
|------|------|------|
| #1 内容数据与状态注册表 | 间接（通过 #10） | 探索点模板配置、路线注册表在 #1 中定义，#10 消费后传递给 #11 |
| #3 本地存档与世界状态持久化 | 间接 | 探索的持久化快照通过 #3 写入，见 EC-11-01/02/03/21 |
| #13 世界修复与解锁 | 位置关联，无接口依赖 | 修复节点与探索点在同一位置，但状态独立（EC-11-17） |

## Tuning Knobs

| # | 参数名 | 类型 | 安全范围 | MVP 值 | 影响 |
|---|--------|------|---------|--------|------|
| 1 | `empty_chance[zone]` | dict | 0.0–0.50 | {A:0.00, B:0.05, C:0.20, D:0.35} | 各区搜索点空结果概率。提高 → 更多空手，降低 → 搜索更"有料"。过高（>0.5）打击探索动力 |
| 2 | `quality_weights[zone]` | dict | 权重和=1.0 | 见 F-11-01 默认值表 | 各区 Poor/Common/Uncommon 的产出品质分布。内圈 Uncommon 权重不宜低于 0.30 |
| 3 | `draw_count[tier]` | {min, max} | {1,2}–{1,5} | Poor:{1,2}, Common:{1,2}, Uncommon:{1,2} | 单次搜索产出的物品种类数。上限受 Pool 5（5 格）约束，不宜超过 5 |
| 4 | `trigger_prob["guard"]` | float | 0.30–0.90 | 0.70 | 守卫威胁靠近触发概率。低于 0.3 太容易绕过，高于 0.9 几乎必触发 |
| 5 | `trigger_radius[threat_category]` | float | 1.0–8.0 | 环境:2-3, 守卫:4-6 | 威胁触发半径（单位）。环境威胁范围小但必触发，守卫范围大但概率触发 |
| 6 | `λ_success` | float | 0.0–0.15 | 0.08 | 成功撤离损耗率。0 = 无损，>0.1 开始与"温和压力"冲突 |
| 7 | `λ_forced` | float | 0.10–0.50 | 0.25 | 被迫撤退损耗率。>0.4 逼近"恶劣惩罚"红线 |
| 8 | `extraction_channel_duration` | float | 1.5–5.0 秒 | 2.5 秒 | 撤离读条时长。太短（<1.5s）失去紧张感，太长（>5s）变为拖沓 |
| 9 | `danger_empty_chance_mod` | float | 0.05–0.25 | 0.15 | danger-changed 状态下所有区域 empty_chance 增量 |
| 10 | `danger_quality_shift` | float | 0.3–0.7 | 0.5 | danger-changed 状态下 Uncommon 权重保留比例（0.5 = 减半） |
| 11 | `search_points_per_zone` | dict | 总和 4–10 | {A:1, B:1, C:2, D:2} 共 6 | 各区域搜索点分布。数量越多单次探索可搜刮越久 |
| 12 | `intel_points_per_template` | int | 1–4 | 2 | 情报点数量。情报稀缺且固定产出，过多会稀释价值感 |
| 13 | `threat_points_per_template` | int | 1–6 | 2+ | 威胁点基础数量。danger-changed 变体可在此基础上新增 1-2 |
| 14 | `repair_kit_drop_rate` | float | 0.15–0.35 | 0.25 | repair_kit 在搜索点中的掉落概率（独立于品质投骰，每搜索点额外判定一次）。预期单次探索 1-2 个（6 搜索点 × 0.25 = 1.5 期望）。低于 0.15 可能导致灯塔修复不可达；高于 0.35 牺牲 repair_kit 稀缺性的取舍张力 |

## Visual/Audio Requirements

### 视觉规格

| 元素 | 说明 | MVP 规格 |
|------|------|---------|
| 探索点场景 | 2D 俯视视图，50×35 单位，4 区域辐条式 | 单张静态背景 + 可配置覆盖层（封锁路径、瓦砾堆等） |
| 玩家角色 | 2D 俯视角色精灵，8 方向或 4 方向移动 | 复用 #3 的 CharacterBody2D 精灵 |
| 搜索点 | 可交互的视觉标记——未搜索时微弱闪烁，已搜索后变为灰暗 | 2 帧切换（活跃/枯竭）+ 微弱粒子提示（可选） |
| 情报点 | 区别于搜索点的视觉标记（如发光的文档/笔记图标） | 静态精灵，交互后变灰 |
| 威胁标记（η>0） | PREVIEW_PRESENCE: 红色感叹号 `!` 覆盖在威胁位置；PREVIEW_FULL: 类型图标 + 文字标签 | 2 个精灵变体 + 动态文本标签 |
| 威胁触发 | 环境威胁：屏幕震动 + 瓦砾掉落粒子；守卫威胁：红色闪烁 + 守卫精灵出现 | 2-3 帧动画 + 简单粒子 |
| 撤离锚点 | 固定位置的光柱/信标标记，始终可见 | 静态发光精灵，呼吸式透明度变化 |
| 撤离读条 | 屏幕底部或角色头顶的进度条 | 简单矩形填充条，被打断时红色闪烁 |
| 状态变体视觉 | unlooted: 场景完整；looted: 搜索点灰色、环境色调微降；danger-changed: 新增瓦砾封锁路径、裂缝/粉尘覆盖层 | 覆盖层切换 + 搜索点状态精灵切换 |
| ARRIVING 文本 | 全屏半透明黑底 + 中央白色描述文本，2-3 行 | 淡入 0.5s → 停留 → 按任意键淡出 0.3s |

### 音频规格

| 事件 | MVP 规格 |
|------|---------|
| 进入探索点（ARRIVING） | 环境音淡入（风声/废墟氛围，循环），3-5 秒过渡 |
| 搜索交互 | 短促翻找声（布料/金属碰撞，<1s） |
| 搜索空结果 | 同样翻找声但结尾轻微下沉（表示"空的"） |
| 情报获取 | 清脆"叮"声 + 纸张翻动（<1s） |
| 威胁触发（环境） | 低频轰隆 + 碎石声（1-2s） |
| 威胁触发（守卫） | 尖锐警报音 + 守卫激活声（1-2s） |
| 撤离读条 | 持续充能嗡鸣（2.5s，音调逐渐升高） |
| 撤离成功 | 嗡鸣收束 + 确认音（<1s） |
| 撤离打断 | 嗡鸣骤停 + 低频断音（<0.5s） |
| DEPARTED 结算 | 温和的收获提示音（轻柔上升音阶，<2s） |
| 探索点背景氛围 | 低频风声 + 偶尔的结构微响（循环，>30s 周期） |

## UI Requirements

| 界面 | 内容 | 触发条件 |
|------|------|---------|
| HUD — 随身物品栏 | Pool 5 的 5 格实时显示：物品图标 + 数量。满格（5/5）时边框高亮为橙色 | EXPLORING 阶段始终显示 |
| HUD — 搜索点计数 | 可选显示"搜索点：3/6"，帮助玩家判断探索进度 | EXPLORING 阶段，可配置显示/隐藏 |
| HUD — 船体状态 | 当前船体 HP 简条（来自 #8），颜色按波段变化（绿/黄/红） | 始终显示 |
| HUD — 威胁预览标记 | 根据 η_scout 在地图上覆盖标记（无/感叹号/完整标签） | EXPLORING 阶段，η_scout 在进入时快照 |
| 搜索结果弹出 | 物品名称 + 图标 + 数量，从搜索点位置弹出，1-2 秒后自动消失或点击关闭 | 搜索产出非空时 |
| 搜索空结果提示 | 淡色文字"空的……"浮出，1 秒后消失 | 搜索产出为空时 |
| 容量取舍界面 | 全屏半透明遮罩 + 中央面板：新物品列表（左）、现有背包（右）、操作按钮（丢弃/放弃/合并/取消）。Unique 物品标注"唯一"警告 | Pool 5 满格且有新物品时 |
| 撤离读条 | 屏幕底部居中进度条 + "撤离中……"文字。被打断时进度条红色闪烁 + "撤离中断！" | EXTRACTING 阶段 |
| 结算摘要 | 全屏面板：本次探索带回物品清单（名称+数量）、情报获取摘要、船体损伤摘要。"确认"按钮关闭 | DEPARTED 阶段 |
| ARRIVING 文本覆盖 | 半透明黑底 + 白色描述文本，底部"按任意键继续"闪烁提示 | ARRIVING 阶段 |
| 存储空间警告 | HUD 角落黄色警告图标 + "⚠ 存储空间不足"，30s 防抖 | localStorage 写入失败时（EC-11-21） |
| 保存失败重试 | 全屏弹窗："保存失败。你的探索收获暂时保留。请检查浏览器存储空间后点击重试。" + 重试按钮 | DEPARTED 写入全部重试失败时（EC-11-03） |

## Acceptance Criteria

### 核心流程

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-11-01 | 进入探索点时，ARRIVING 阶段展示抵达描述文本（安全抵达 vs 迫降），按任意键后进入 EXPLORING 阶段，玩家角色出现在入口区或坠机点。 | 准备 `EncounterContext`（voyage_result=arrived 和 forced_landing 各测一次）。进入探索点，观察 ARRIVING 文本与入场位置是否匹配 C3 规则。按任意键，确认阶段切换为 EXPLORING。引用 C2、C3。 |
| AC-11-02 | 在 EXPLORING 阶段靠近搜索点并触发交互，播放搜索动画后返回结果。若结果为"空"，search_consumed=false；若结果为非空，search_consumed=true，物品进入 Pool 5。 | 准备 unlooted 探索点。(a) 使用 `gm_set_search_config <sp_id> empty_chance=1.0` 将任意搜索点设为必空。搜索该点 → 确认返回空结果且 search_consumed=false，该搜索点可再次交互。(b) 将该点 empty_chance 恢复为 0.0 → 确认返回非空、search_consumed=true、物品进入 Pool 5。(c) 在 A_core 区域用默认配置搜索 → 确认非空产出（empty_chance=0.00）。引用 C5、F-11-01。 |
| AC-11-03 | 与情报点交互，每次会话仅可交互一次，固定产出 1 个 Q=1 Unique 情报物品（intel.*），不参与 F-11-01 投骰。 | 探索点内有 2 个情报点。依次交互，确认各产出 1 个 Unique 情报物品（检查物品 Q=1, max_stack=1）。再次尝试交互同一情报点 → 应提示"此处已调查过"。引用 C5、F-11-06。 |
| AC-11-04 | 玩家移动到撤离锚点，执行"撤离"操作 → 进入 EXTRACTING 阶段，开始 2.5 秒读条。读条完成 → 进入 DEPARTED 阶段。 | 玩家携带任意物品到达撤离锚点，触发撤离，计时 2.5 秒。读条期间不可移动，读条完成后确认阶段切换为 DEPARTED。引用 C8。 |
| AC-11-05 | DEPARTED 结算：Pool 5 中物品按 F-11-04 撤离损耗结算。Unique 物品（Q=1, max_stack=1）永不损耗、全量转入飞艇仓库。非 Unique 物品按 λ_success=0.08（成功撤离）或 λ_forced=0.25（被迫撤退）计算损耗，每堆至少保留 1。 | 准备 Pool 5 含 Unique 物品 ×1、非 Unique 物品 ×2（如 basic_supply ×20、repair_kit ×12）。正常撤离后检查仓库：Unique 全量保留；basic_supply 保留 16-19；repair_kit 保留 10-12。对比 F-11-04 演算示例。引用 C9、F-11-04。 |
| AC-11-06 | DEPARTED 结算：探索中获取的情报物品写入 System #6，对应隐藏标签变为已知标签，航线知识永久推进。 | 在探索中获取情报物品后正常撤离，打开航图（System #6 管理），确认相关标签从 unknown/rumor 变为 revealed/verified。引用 C9。 |
| AC-11-07 | DEPARTED 结算：探索中的威胁接触造成的船体损伤和模块受损标记结算至 System #8。 | 在探索中触发环境威胁（产生损伤），正常撤离后检查飞艇船体 HP → 应扣除对应损伤值。引用 C9。 |

### 状态机

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-11-08 | ARRIVING → EXPLORING 转换：按任意键触发转换。ARRIVING 中不可移动或交互。 | 进入探索点，按住方向键 → 无响应。按任意键 → 进入 EXPLORING。引用 C2。 |
| AC-11-09 | EXPLORING → EXTRACTING → DEPARTED 主流程：在 EXPLORING 中触发撤离锚点 → EXTRACTING，读条完成 → DEPARTED。中间无其他阶段跳转。 | 完整走一遍 ARRIVING→EXPLORING（搜索 1-2 点）→ 撤离锚点 → EXTRACTING（读条完成）→ DEPARTED。确认阶段转换顺序严格匹配 C2 状态图。引用 C2、C8。 |
| AC-11-10 | EXTRACTING 被打断 → 回落 EXPLORING：读条期间若被威胁攻击或环境伤害击中，读条进度重置为 0，阶段回到 EXPLORING，进入 threatened 子状态。威胁处理完毕后玩家仍在撤离锚点旁，可再次尝试撤离。 | 在 EXTRACTING 读条期间，使用 `gm_threat_trigger <threat_point_id>` 强制触发附近一个活跃威胁。观察读条中断、进度归零、阶段显示 EXPLORING → threatened。威胁结算后检查玩家位置仍在撤离锚点旁。引用 C2、C8、EC-11-11。 |
| AC-11-11 | EXPLORING 内子状态正确切换：idle（默认）↔ moving（移动中）→ searching（搜索动画中）→ idle；threatened（威胁触发中）→ idle（处理完毕）。 | 执行移动 → 观察子状态为 moving。触发搜索 → 子状态为 searching，动画完成后恢复 idle。触发环境威胁 → 子状态为 threatened，损伤施加后恢复 idle。引用 C2。 |

### 搜索与容量交互

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-11-12 | 自由搜索：搜索结果为空的搜索点，search_consumed=false。玩家可在本会话中继续搜索其他搜索点，不会因"翻到空"受到惩罚。 | 在 C_mid 区域（empty_chance=0.20）反复测试 10 次搜索点，统计空结果次数。每次空结果均不消耗搜索次数，该搜索点可再次交互。引用 C5、F-11-01。 |
| AC-11-13 | Pool 5 已满（5/5）且搜索结果包含非空物品时，弹出取舍界面：选项 (a) 丢弃现有物品（永久丢失），(b) 放弃新物品（永久丢失），(c) 如同 ID 可堆叠 → 自动合并选项。关闭弹窗不做选择 = 放弃新物品。 | 先将 Pool 5 填满 5 格不同物品，然后搜索一个新搜索点（确保产出非空）。确认弹窗出现，列出 3 个选项。分别测试各选项和关闭弹窗行为。引用 C5、EC-11-04。 |
| AC-11-14 | 堆叠合并：搜索产出 resource_id 与 Pool 5 中某格相同且 stackable=true → 自动合并至 max_stack，溢出部分若无空闲格位则按 EC-11-04 取舍处理。合并本身静默不弹窗。 | Pool 5 中有一格 basic_supply ×18（max_stack=20），搜索产出 basic_supply ×5 → 合并至 20，溢出 3 触发取舍弹窗。引用 C5、EC-11-06。 |
| AC-11-15 | Pool 5 已满 + 情报点产出 Unique 物品：取舍界面附加提示"此情报为唯一物品，放弃后将无法再次获取。"放弃的情报永久丢失，情报点标记为已交互。 | Pool 5 填满 5/5，与情报点交互。确认取舍界面出现，且包含 Unique 物品警告。选择"放弃" → 情报点标记为已交互，下次无法再次获取。引用 EC-11-05。 |

### 威胁与侦察

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-11-16 | η_scout = 0（无侦察模块）→ PREVIEW_NONE：探索点内所有威胁点不可见，仅当玩家靠近并触发时才显示。 | 配置 η_scout=0 进入探索点。在包含已知威胁点坐标的区域移动，观察地图/HUD 无任何威胁标记。靠近威胁点触发半径后才出现威胁表现。引用 C6、F-11-03。 |
| AC-11-17 | 0 < η_scout < 1.0 → PREVIEW_PRESENCE：威胁点显示红色感叹号标记，但不显示具体类型和名称。 | 配置 η_scout=0.6（Scout 受损+intact band）进入探索点。观察所有已知威胁点位置显示红色感叹号，但不显示"守卫""塌方"等具体文字。引用 C6、F-11-03。 |
| AC-11-18 | η_scout = 1.0 → PREVIEW_FULL：威胁点显示类型和名称（如"守卫哨兵·东北角"），预览标记在进入时一次性快照加载。 | 配置 η_scout=1.0（Scout 正常+intact band）进入探索点。观察所有已知威胁点显示完整标签，包含类型文本和方位描述。探索过程中清除一个守卫威胁后，预览标记不动态消失（快照不变）。引用 C6、F-11-03。 |
| AC-11-19 | 环境威胁靠近必触发：玩家进入环境威胁的 trigger_radius（2-3 单位）→ trigger_prob=1.0，必定触发。系统自行施加船体损伤或封锁路径。 | 配置 η_scout 为任意值，在已知环境威胁点处逐步靠近。测量触发距离 = 2-3 单位时必定触发，检查船体 HP 扣减或路径封锁。重复 10 次确认触发率 100%。引用 C7、F-11-02。 |
| AC-11-20 | 守卫威胁靠近 70% 概率触发 + 不可用时 inert：进入守卫威胁 trigger_radius（4-6 单位）→ 70% 概率触发，触发后传递 threat_context 至 #12。若 #12 不可用则守卫 inert（不触发、不造成伤害），threat_point.is_active 保持 true。 | (a) 使用 `gm_set_trigger_prob <threat_id> 1.0` 将守卫威胁触发概率设为 1.0。靠近 → 确认每次必定触发，传递 threat_context 至 #12。(b) 设为 0.0 → 靠近确认从不触发（proximity 方式不触发，但 interaction 方式仍触发）。(c) 使用 `gm_disable_system 12` 断开 #12 接口，设 trigger_prob=1.0 → 靠近确认守卫变为 inert（不触发、不造成伤害），threat_point.is_active 保持 true。(d) 恢复默认 prob=0.70 后，50 次靠近统计为调优验证（非 CI 门禁，不阻塞合并）。引用 C7、F-11-02、EC-11-12。 |

### 状态变体

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-11-21 | unlooted → looted：全部 6 个搜索点被搜刮完毕且无环境威胁活跃 → 探索点状态变为 looted。再次进入该探索点，所有搜索点返回"这里已经被搜过了"，情报点保持枯竭，威胁点正常活跃。 | 首次进入 unlooted 探索点，搜索全部 6 个搜索点但不触发任何环境威胁。撤离结算后检查持久化状态 = looted。重新进入 → 交互任意搜索点返回"已搜刮"消息。引用 C10、F-11-05、EC-11-14。 |
| AC-11-22 | unlooted → danger-changed：触发环境威胁且未清除（env_threat_active=true），无论搜索了多少点，探索点状态变为 danger-changed。再次进入：路径布局变化、新增 1-2 环境威胁点、所有区域 empty_chance +0.15、Uncommon 权重 ×0.5 向 Poor 偏移。 | 首次进入探索点，触发一处塌方环境威胁但不处理/不清除。撤离结算后检查持久化状态 = danger-changed。重新进入 → 确认 ARRIVING 描述变为警告文本，观察地图上新增封锁路径和威胁点，在 A_core 搜索 → empty_chance 从 0.00 变为 0.15。引用 C10、F-11-05、EC-11-15。 |
| AC-11-23 | danger-changed 不可自动恢复：danger-changed 状态永久保持，直到玩家清除所有环境威胁。清除全威胁 + 搜完剩余点 → looted；清除全威胁 + 未搜完 → unlooted。 | 创建 danger-changed 探索点（含 1 活跃环境威胁），进入后清除威胁，搜索剩余 3 个未搜点。撤离后检查状态变为 looted。再创 danger-changed 点，仅清除威胁不搜索 → 状态变为 unlooted。引用 C10、F-11-05。 |

### 持久化与恢复

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-11-24 | EXPLORING 中标签页关闭后恢复：持久化快照在每次搜索/威胁结算后写入。关闭浏览器标签页，重新打开并恢复会话 → 玩家处于 EXPLORING 阶段，已搜索的搜索点保持已搜索，Pool 5 恢复至快照状态。显示"你在探索中中断了"提示。 | 在 EXPLORING 阶段搜索 3 个搜索点、获取 1 个情报、Pool 5 有 2 格物品。关闭标签页，重新打开游戏 → 确认恢复后：(a) 已搜索的 3 点不可再次搜索，(b) Pool 5 恢复 2 格物品，(c) 提示信息出现。最近一次快照后的进度丢失。引用 EC-11-01。 |
| AC-11-25 | EXTRACTING 中标签页关闭：撤离读条是原子操作。在读条期间关闭标签页，重新打开 → 玩家处于 EXPLORING 阶段，位于撤离锚点旁，需重新触发撤离。 | 在撤离读条第 1.5 秒时关闭标签页。重新打开 → 确认阶段为 EXPLORING（不是 DEPARTED），位置在撤离锚点旁，Pool 5 保持撤离前状态。引用 EC-11-02。 |
| AC-11-26 | DEPARTED 结算写入事务性：结算在内存中组装完整结算包，一次性写入。若写入失败，自动重试 1s/2s/4s/8s（最多 4 次）。全部失败后显示"保存失败"提示和手动重试按钮，结算包保留在内存中。 | 模拟 localStorage 写入失败（填满配额），触发 DEPARTED 结算。观察自动重试日志（4 次间隔递增）。全部失败后确认 UI 显示错误提示 + "重试"按钮。清理配额后重试成功 → 结算包正确写入。引用 EC-11-03。 |

## Test Tools Requirements

以下 GM 命令由本 GDD 的验收条件引用，需在实现时提供。命令由 #3（本地存档与世界状态持久化）或专用调试模块注册。

| GM 命令 | 参数 | 用途 | 被 AC 使用 |
|---------|------|------|-----------|
| `gm_set_search_config` | `<sp_id> empty_chance=<v>` | 覆盖搜索点的 empty_chance（确定性测试） | AC-11-02, AC-11-12 |
| `gm_threat_trigger` | `<threat_point_id>` | 强制触发指定威胁点（如同玩家已进入其触发半径）。可在 EXTRACTING 阶段调用 | AC-11-10 |
| `gm_set_trigger_prob` | `<threat_id> <value>` | 覆盖威胁触发概率（0.0–1.0） | AC-11-20 |
| `gm_disable_system` | `<system_id>` | 断开指定系统的接口，模拟系统不可用 | AC-11-20 |
| `gm_show_threat_hitboxes` | — | 在调试模式下可视化所有威胁的触发半径、ID、类型 | AC-11-16–19 |
| `gm_show_player_position` | — | HUD 显示玩家当前坐标（用于距离验证） | AC-11-19 |
| `gm_show_substate` | — | HUD 显示当前探索状态机子状态 | AC-11-11 |
| `gm_dump_search_config` | — | 打印当前探索点所有搜索点的配置快照（含修饰符） | AC-11-22 |
| `gm_fill_localstorage` | — | 将浏览器 localStorage 填充至配额限制（模拟写入失败） | AC-11-26 |
| `gm_dump_exploration_state` | — | 打印持久化的探索点状态（当前变体、各搜索点枯竭状态、威胁活跃状态） | AC-11-21–23 |
| `gm_set_voyage_result` | `<arrived\|forced_landing>` | 覆盖 EncounterContext 的 voyage_result，用于测试入场条件 | AC-11-01 |
| `gm_set_carried` | `<resource_id> <quantity> [<resource_id2> <qty2> ...]` | 直接设置 Pool 5 内容（清空后填入），用于容量与结算 AC 的精确配置 | AC-11-05, AC-11-13, AC-11-14, AC-11-15 |
| `gm_set_eta_scout` | `<value>` | 覆盖 η_scout 侦察效率值（0.0–1.0），独立于模块/船体状态 | AC-11-16, AC-11-17, AC-11-18 |
| `gm_set_exploration_state_variant` | `<unlooted\|looted\|danger-changed>` | 直接设置探索点的持久化状态变体，用于状态转换 AC 的快速场景搭建 | AC-11-21, AC-11-22, AC-11-23 |

## Open Questions

1. **多探索点模板扩展**：MVP 只有「云观站废墟」一个模板。第二个探索点模板应在何时引入？是否需要等 #12 战斗系统完成后才能设计有守卫密度更高的探索点？

2. **工具与 danger-changed 交互**：当前设计中，danger-changed 通过"清除威胁"恢复——清除威胁的具体方式是什么？（通过 #12 战斗清除守卫？携带特定工具清除塌方？）这需要与 #12 和 #5 协调。MVP 中暂时以"清除所有环境威胁"为条件。

3. **η_scout 预览标记的动态更新**：当前设计中 η_scout 在进入时快照，探索过程中即使威胁被清除，预览标记也不消失（EC-11-13 已知的轻度 UI 不一致）。是否需要在后续版本中改为动态更新？

4. **撤离锚点的安全性**：当前撤离锚点非绝对安全区——读条可被打断。是否需要在探索点设计中考虑"撤离锚点附近的威胁密度应较低"的布局约束？还是由关卡设计师自行平衡？

5. **空手而归的叙事反馈**：Pool 5 为空时撤离（EC-11-19），结算摘要显示"本次探索未带回任何物品。"是否应该有更丰富的叙事反馈（如日志条目）？MVP 中保持简洁。

6. **情报点产出与 #9 的接口细节**：情报物品（intel.*）的具体字段结构（标签类型、关联航线 ID、揭示效果）需要在 #6 的 GDD 中明确定义。当前 #11 只定义了"固定产出 1 个 Q=1 Unique intel 物品"的行为。
