# 战斗与威胁处理

> **Status**: In Review (Revision 2 applied 2026-05-03; 2026-05-04 C1 fix: Tank rebalanced — damage 12-18→8-12, module chance 50%→30%, cross-band threshold recalculated 37→33, hull_warning_threshold 18→12)
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-04
> **Implements Pillar**: 未知带来温和压力

## Overview

战斗与威胁处理是《云海织航》探索循环中的薄层威胁解决系统。在数据层，它消费探索/搜撤场景（#11）在守卫威胁触发时发出的 `threat_context {threat_type, threat_id, position, encounter_params}`，将威胁解析为一个简单的决策点——玩家从 1-2 种响应中选择——并产出 `combat_result {outcome, hull_damage, module_damage, resources_consumed}`，结果分别写入飞艇模块与船体状态（#8，船体损伤/模块受损标记）和资源系统（#5，应急消耗品扣除）。在体验层，它是探索中"遇到危险"的时刻——不是一场完整的战斗，而是一个有代价的判断：你可以硬扛（受伤）、规避（击退/绕路）、或放弃深入（撤退并承受货物损失）。MVP 中只定义一种威胁类型（守卫哨兵）、三种玩家响应（应急处理 / 硬扛 / 撤退）、三种结果（船体受伤 / 被击退 / 被迫撤退）。

没有这个系统，探索点的威胁标记就是空的——玩家可以无代价地搜刮每一个搜索点，Pillar 4（未知带来温和压力）失去它最直接的机械载体：不是所有风险都能被侦察模块提前看到并绕过，有些风险需要你在现场做出判断并付出代价。

## Player Fantasy

战斗与威胁处理服务的核心幻想是：**你是一个在危险面前不慌张的船长——你在警报响起的那一拍呼吸里，根据船体状态、携带物资和此次出航的目标，做出一个清晰的判断：扛过去、绕过去、还是撤回去。**

### 有备而来：准备被验证的时刻（主基调）

威胁遭遇不是随机的厄运——它们是出航前准备决策的回响。你在航图上读过风险标签。你在工程舱检查过模块状态。你带了足够的 repair kit 还是多带了货舱空间。当守卫哨兵激活的那一刻，你不是在"刷怪"——你是在面对一个你早有预料的可能性。

锚定时刻不是"我打败了它"，而是"我知道这一下我能扛，因为来之前我修好了船体"。或者反过来："我判断错了——船体只剩 15%，我不该冒这个险"。两种结果都是满足的：正确的判断被验证，错误的判断变成下一趟的经验。这不是战斗的爽快，这是规划者看到自己的准备（或疏忽）在现实中落地的因果满足。

这种满足是克制的、私人的。不是胜利的呐喊，而是航海日志里的一行："B 区遭遇哨兵。船体承受一次撞击。判断可继续深入。"像船长在航海日志里记录一次风暴——不带戏剧化，只记录事实和判断。

### 沉着判断：警报响起后的那一拍（情感语调）

守卫哨兵激活时，画面短暂闪烁红色，警报音响起——然后是一个停顿。在这个停顿里，玩家看到：船体 62%，随身物品栏有 repair kit ×2，已经搜了 4/6 个搜索点，还有 2 个没看。这一拍是系统给玩家的判断空间——不是反应速度测试，不是 QTE，是你作为船长的决策时刻。

"未知带来温和压力"（Pillar 4）在这里兑现：压力来自情报不足和取舍——你知道这个哨兵会打掉你多少船体，但你不知道后面还有没有第二个。你知道你可以用 repair kit 应急处理，但那是留给灯塔修复的。你不知道撤退会不会损失你已找到的那块云晶。判断的压力是真实的，但不是来自倒计时——来自你必须在不完全信息下做出选择。

### 每一次接触都是一堂课（隐含层）

威胁接触后，无论你选择了什么，你知道了更多。你知道 B 区有这个类型的哨兵。你知道它的触发半径。你知道一次撞击大概打掉 8-12 点船体。下一次来，你的侦察模块可能已经升级了，你的船体可能已经修好了，你可能选择从西侧绕行——或者你这次就是来清掉它的。

这不是"刷怪升级"——这是"我上次来过，我这次更懂了"。Pillar 4 的"失败应以教育性损失为主"在这里是最直接的兑现：即使你被迫撤退、损失了部分货物，你带回了一个关键情报——这个地方有什么、怎么应对、下次带什么。

### 参考感受

参考航海日志中遭遇风暴的记录——"本日遭遇哨兵自动防御装置，船体承受轻度损伤。判断可继续任务。"冷静、克制、基于判断。情绪不是战斗的亢奋，而是决策后的平静——无论选择了什么，你知道你为什么选。

## Detailed Design

### Core Rules

**C1. 威胁上下文接收**

探索系统（#11）在守卫威胁触发后（F-11-02），调用 `resolve_threat(threat_context)` 并将探索阶段切换到 `threatened` 子状态。本系统接收 `threat_context {threat_type: "guard", threat_id, position, encounter_params}`，暂停探索移动（无计时器），进入决策呼吸阶段。

**C2. 决策呼吸（Decision Breath）**

威胁触发后，探索画面短暂闪烁红色边框（0.3s），警报音响起，随后进入暂停状态。画面中央或侧边显示决策面板，包含：

- **威胁描述**：守卫哨兵——自动防御装置。一行文字说明威胁性质。
- **当前船体状态**：integrity 值 + 波段标签（intact/damaged/critical）+ 颜色指示（绿/黄/红）。
- **可用响应选项**：2-3 个按钮，不可用的选项灰显 + tooltip 说明缺失条件。
- **随身物资摘要**：Pool 5 中的 repair_kit 和 basic_supply 数量。

玩家在此暂停中可以自由查看船体状态和随身物品，不限时。玩家可按 Esc 或点击面板外遮罩关闭面板，以便查看小地图、模块详情和完整货物清单——探索保持暂停，但 UI 审查不受限。关闭后屏幕顶部显示"威胁活跃"指示器（琥珀色标记），点击指示器或再次按威胁快捷键可重新打开面板。三个响应选项绑定快捷键（不论面板是否打开均有效）。这是 Pillar 4 的"温和压力"载体——压力来自信息不完全和后果未知，不来自倒计时。

**C3. 响应选项**

| # | 选项 | 可用条件 | 资源消耗 | 船体伤害 | 模块风险 | 威胁结果 | 附加效果 |
|---|------|---------|---------|---------|---------|---------|---------|
| A | **应急处理** | Pool 5 中 ≥1 repair_kit | 1 repair_kit | **0** | 无 | 清除（is_active=false，本会话永久安全） | — |
| B | **硬扛** | 始终 | 无 | **8-12**（uniform 随机） | 30%: 随机 1 个已安装模块 → damaged | 活跃（保持 is_active=true） | 击退 8 单位（推出 trigger_radius） |
| C | **撤退** | 始终 | 无 | **0** | 无 | 活跃（保持 is_active=true） | 击退 10 单位 + retreat_flagged=true |

- **应急处理**不可用时灰显，tooltip："需要 repair_kit ×1（随身物品栏中无可用）"
- 如果 Pool 5 中有 repair_kit 但不在 carried 池（在仓库），同样不可用——准备必须在出航前完成
- **硬扛**在 hull ≤ 12 时，选项标签附加警告标记"⚠ 船体严重受损"但不阻止选择
- **撤退**始终可用——这是安全阀

**C4. 结算序列（严格顺序）**

```
1. 验证所选选项的可用条件。不满足 → 返回 ERR_UNAVAILABLE，不消耗、不改变状态。
2. 执行资源消耗（仅应急处理）：
   → #5.consume_in_combat("repair_kit", 1) from Pool 5
3. 计算船体伤害（按选项）：
   → 应急处理: 0
   → 硬扛: random_int(8, 12)
   → 撤退: 0
4. 判定模块损伤（仅硬扛）：
   → if random() < 0.30 AND 存在已安装模块：
       target_slot = random_choice(已安装且非empty的槽位)
       module_damage = {slot_id: target_slot, damage_type: "guard_impact"}
5. 应用船体伤害（如有）：
   → #8.apply_hull_damage(hull_damage)
6. 应用模块损伤（如有）：
   → #8.apply_module_damage(slot_id, "guard_impact")
7. 更新威胁状态（仅应急处理）：
   → threat_point.is_active = false（本会话永久安全，EC-11-13 保证不再触发）
8. 执行击退（硬扛 / 撤退）：
   → 玩家位置被推出，方向 = 从威胁位置指向玩家位置
   → 硬扛: 8 单位；撤退: 10 单位
9. 返回 combat_result 至 #11（见 C5）
10. #11 将探索阶段从 threatened 恢复到 exploring
```

**C5. 战斗结果契约（返回 #11）**

```
{
    outcome: "suppressed" | "tanked" | "retreated",
    hull_damage: int,
    module_damage: {slot_id: StringName, damage_type: StringName} | null,
    resources_consumed: [{resource_id: StringName, quantity: int}] | null,
    knockback: {direction: Vector2, distance: float} | null,
    retreat_flagged: bool
}
```

**C6. 结果级联**

- `hull_damage > 0` → #8 `apply_hull_damage(amount)`：integrity 扣减，波段可能转换，scars 递增
- `module_damage != null` → #8 `apply_module_damage(slot_id, "guard_impact")`：模块 actual_state → damaged，效率降至 0.6（scout）或 0.5（cargo）
- `resources_consumed != null` → #5 `consume_in_combat(resource_id, quantity)`：从 Pool 5 永久移除
- `retreat_flagged = true` → #11 记录撤退标记。若玩家后续从探索点撤离，撤离损耗使用 λ_forced = 0.25（替代正常 λ_success = 0.08）。见 #11 F-11-04 `extraction_loss_settlement` 的 voyage_result 判定
- `knockback` → #11 将玩家位置移动指定方向和距离。击退不穿越碰撞体——若目标位置被阻挡，停在阻挡物前

**C7. 重触发防护**

硬扛和撤退后威胁保持 `is_active=true`，但击退将玩家推出 trigger_radius（守卫 trigger_radius = 4-6 单位；硬扛击退 8 单位，撤退击退 10 单位）。玩家主动走回触发半径时，F-11-02 可再次触发同一威胁。这是有意设计——硬扛让你暂时脱身，但没有消除威胁；你可以选择绕路、再次硬扛、或用应急处理彻底清除。

**C8. 威胁类型扩展预留**

MVP 仅定义一种威胁类型（`guard`）。威胁配置表预留字段以支持后续类型的添加：

| 字段 | MVP 值（guard） | 说明 |
|------|----------------|------|
| `threat_category` | `guard` | 威胁类别标识 |
| `full_damage_range` | [8, 12] | 硬扛时的伤害范围 |
| `module_damage_chance` | 0.30 | 模块受损概率 |
| `trigger_radius` | 4-6 | 触发半径（单位），由 #11 管理 |
| `emergency_cost` | repair_kit ×1 | 应急处理消耗 |
| `knockback_distance_tanked` | 8.0 | 硬扛击退距离 |
| `knockback_distance_retreat` | 10.0 | 撤退击退距离 |
| `can_be_suppressed` | true | 是否可被应急处理清除 |

### States and Transitions

**威胁结算微观状态机**

```
┌──────┐  threat_context 到达   ┌──────────────────┐
│ IDLE │───────────────────────→│ AWAITING_RESPONSE │
└──────┘                        └───────┬──────────┘
                                        │ 玩家选择响应
                                        ▼
                               ┌──────────────┐
                               │  PROCESSING   │
                               └───────┬──────┘
                                       │ 结算完成
                                       ▼
                               ┌──────────────┐
                               │   RESOLVED    │────→ IDLE（返回 #11 控制）
                               └──────────────┘
```

| 状态 | 说明 | 玩家可操作 |
|------|------|-----------|
| `IDLE` | 无活跃威胁结算。探索正常进行。 | 移动、搜索、交互 |
| `AWAITING_RESPONSE` | 决策呼吸阶段。探索暂停，决策面板显示。 | 查看状态、选择响应（不可移动/搜索） |
| `PROCESSING` | 执行结算序列（C4 步骤 1-9）。短暂过渡态。 | 无（自动进行） |
| `RESOLVED` | 结算完成。combat_result 已返回 #11。 | 无（#11 恢复探索控制） |

**重入防护**：当状态 ≠ IDLE 时，任何到达的 threat_context 均被加入队列（FIFO），在当前威胁结算完成且状态返回 IDLE 后出队处理。同一时间只有一个威胁处于活跃结算状态。若队列长度达到上限（4），最早进入队列的威胁被丢弃并记录警告日志——防止极端情况下的队列堆积。

**威胁持久状态**

| 状态 | 含义 | 持续 |
|------|------|------|
| `active` | 威胁未被清除，可再次触发 | 本会话内永久（直到被应急处理清除） |
| `suppressed` | 威胁已被应急处理清除 | 本探索会话内永久，跨 save/load 持久（重入触发半径不触发，EC-11-13）。`is_active = false` 写入 #11 的探索点快照（EC-11-01：每次威胁结算后写入 snapshot）。重新加载后 suppressed 威胁保持 suppressed。若探索会话结束（DEPARTED）后重新进入同一探索点，所有威胁重置为默认状态（active），因为这是一个新的探索会话。 |
| `active` | 威胁未被清除，可再次触发 | 本探索会话内持久（直到被应急处理清除），跨 save/load 持久。`is_active = true` 写入 #11 的探索点快照。 |

### Interactions with Other Systems

**上游（本系统消费）**

| 系统 | 数据流入 | 接口 |
|------|---------|------|
| #11 探索/搜撤场景 | `threat_context {threat_type, threat_id, position, encounter_params}` | `resolve_threat(threat_context) → combat_result` |
| #5 资源/货物与容量 | Pool 5 中 repair_kit 数量查询；消耗执行 | `get_carried_contents_by_tag("repair-material")`, `consume_in_combat(resource_id, quantity)` |

**下游（本系统产出）**

| 系统 | 数据流出 | 接口 |
|------|---------|------|
| #11 探索/搜撤场景 | `combat_result {outcome, hull_damage, module_damage, resources_consumed, knockback, retreat_flagged}` | 返回值 |
| #8 飞艇模块与船体状态 | 船体伤害量、模块受损标记 | `apply_hull_damage(amount)`, `apply_module_damage(slot_id, damage_type)` |
| #5 资源/货物与容量 | 应急消耗 | `consume_in_combat(resource_id, quantity)` |
| #17 反馈/特效/音频语义 | 威胁结算事件（suppressed/tanked/retreated） | 信号/事件（具体接口由 #17 定义） |

**间接依赖**

| 系统 | 关系 | 说明 |
|------|------|------|
| #1 内容数据与状态注册表 | 间接（通过 #11） | 威胁配置表（threat_type 定义、伤害范围、消耗成本）存储在注册表中 |
| #16 UI/HUD/航图界面 | 下游 | 决策面板 UI、船体状态显示、威胁警告标记 |

## Formulas

### F-12-01 威胁结算主公式 `resolve_threat`

`resolve_threat(threat_context)` 由 #11 调用（单参数入口点）。#12 在 AWAITING_RESPONSE 状态内部查询 Pool 5（通过 `get_carried_contents_by_tag`）以获取 `carried_inventory`，并通过决策面板 UI 收集玩家的 `response_choice`。结算使用以下分段逻辑：

```
resolve_threat(threat_context) =
  Internal: carried_inventory = #5.get_carried_contents_by_tag("repair-material")
  Internal: response_choice = UI.get_player_response()
  Piecewise:
    response_choice = "emergency_handling" AND check_emergency_available(carried_inventory) = true →
      {outcome: "suppressed", hull_damage: 0, module_damage: null,
       resources_consumed: [{resource_id: "repair_kit", quantity: 1}],
       knockback: null, retreat_flagged: false}
    response_choice = "tank" →
      {outcome: "tanked", hull_damage: calc_hull_damage(response_choice, encounter_params),
       module_damage: calc_module_damage(response_choice, encounter_params, module_state),
       resources_consumed: null, knockback: {direction: threat→player, distance: 8.0},
       retreat_flagged: false}
    response_choice = "retreat" →
      {outcome: "retreated", hull_damage: 0, module_damage: null,
       resources_consumed: null, knockback: {direction: threat→player, distance: 10.0},
       retreat_flagged: true}
```

**变量表**：

| 变量 | 符号 | 类型 | 值域 | 说明 |
|------|------|------|------|------|
| `threat_context` | — | struct | — | #11 传入的威胁触发载荷（threat_type, threat_id, position, encounter_params）。encounter_params 在 #11 的 F-11-02 `build_threat_context()` 中根据威胁配置表（C8）填充。`response_choice` 和 `carried_inventory` 由 #12 内部获取（分别来自决策面板 UI 输入和 #5 `get_carried_contents_by_tag("repair-material")` 查询）——它们不是 #11 传入 #12 的参数。 |
| `carried_inventory` | — | map | — | #12 内部从 Pool 5 查询得到：resource_id → quantity |
| `response_choice` | — | enum | {emergency_handling, tank, retreat} | #12 内部从决策面板 UI 收集 |
| `combat_result` | — | struct | — | 输出：outcome, hull_damage, module_damage, resources_consumed, knockback, retreat_flagged |

**输出范围**：三种离散 `combat_result` 变体之一。

**演算示例**：B 区守卫触发。玩家 Pool 5 含 repair_kit ×2，hull=62%。选择"应急处理"。`check_emergency_available` → true。结果：`{outcome: "suppressed", hull_damage: 0, module_damage: null, resources_consumed: [{resource_id: "repair_kit", quantity: 1}], knockback: null, retreat_flagged: false}`。威胁点 `is_active = false`。

---

### F-12-02 船体伤害计算 `calc_hull_damage`

```
calc_hull_damage(response_choice, encounter_params) =
  0                                                     if response_choice ≠ "tank"
  uniform_int(encounter_params.full_damage_min,
              encounter_params.full_damage_max)          if response_choice = "tank"
```

**变量表**：

| 变量 | 符号 | 类型 | 值域 | 说明 |
|------|------|------|------|------|
| `response_choice` | — | enum | {emergency_handling, tank, retreat} | 必须为 "tank" 才产生伤害 |
| `encounter_params.full_damage_min` | — | int | 8（guard） | 均匀随机伤害下界 |
| `encounter_params.full_damage_max` | — | int | 12（guard） | 均匀随机伤害上界 |
| `result` | — | int | 0 或 [8, 12] | 通过 #8.apply_hull_damage() 应用的船体伤害 |

**输出范围**：0（非硬扛）或 8-12 闭区间均匀随机整数（硬扛）。每个整数的概率均为 1/5。

**演算示例**：玩家硬扛守卫。`uniform_int(8, 12)` 掷出 10。结果：10 船体伤害。施加到 integrity=62 → 新值 52（仍在 intact 波段）。5 次硬扛期望总伤害约 50（5×10），刚好从 intact 推到 damaged 边界但不进入 critical。

---

### F-12-03 模块损伤判定 `calc_module_damage`

```
calc_module_damage(response_choice, encounter_params, module_state) =
  if response_choice = "tank"
    AND random_float(0, 1) < encounter_params.module_damage_chance
    AND count(eligible_modules) > 0:
      target = random_choice(eligible_modules)
      → {module_damaged: true, target_slot_id: target}
  else:
      → {module_damaged: false, target_slot_id: null}
```

**变量表**：

| 变量 | 符号 | 类型 | 值域 | 说明 |
|------|------|------|------|------|
| `response_choice` | — | enum | {emergency_handling, tank, retreat} | 必须为 "tank" 才产生模块风险 |
| `encounter_params.module_damage_chance` | — | float | 0.30（guard） | 概率阈值，来自威胁配置表 C8 |
| `random_float(0, 1)` | — | float | [0.0, 1.0) | 均匀随机数 |
| `eligible_modules` | — | list | 0-2 条目 | #8 槽位中已安装且 `actual_state ≠ damaged` 的槽位 ID 列表（EC-12-04 过滤：排除已受损槽位） |
| `result.module_damaged` | — | bool | {false, true} | 是否有模块受损 |
| `result.target_slot_id` | — | string 或 null | slot_a / slot_b / null | 受损的目标槽位 |

**输出范围**：{module_damaged: false, target: null} 或 {module_damaged: true, target: slot_a 或 slot_b}。

**演算示例**：玩家硬扛守卫。两模块已安装（scout 在 A，cargo 在 B）。`random()` = 0.22（< 0.30，判定通过）。`random_choice(["slot_a", "slot_b"])` 选中 "slot_a"。结果：`{module_damaged: true, target_slot_id: "slot_a"}`。#8 将侦察模块效率设为 0.6。

---

### F-12-04 应急可用性检查 `check_emergency_available`

```
check_emergency_available(carried_inventory, repair_kit_id) =
  carried_inventory.get(repair_kit_id, 0) >= 1
```

**变量表**：

| 变量 | 符号 | 类型 | 值域 | 说明 |
|------|------|------|------|------|
| `carried_inventory` | — | map | — | Pool 5 内容：resource_id → quantity |
| `repair_kit_id` | — | string | — | repair_kit 的资源标识符（由 #5 定义） |
| `result` | — | bool | {false, true} | 应急处理选项是否可选 |

**输出范围**：二元。false → 应急处理按钮灰显 + tooltip "需要 repair_kit ×1（随身物品栏中无可用）"。

**演算示例**：Pool 5 = {repair_kit: 2, basic_supply: 5}。`carried_inventory.get("repair_kit", 0)` = 2 ≥ 1 → true。应急处理选项可用。

---

### F-12-05 击退距离计算 `calc_knockback`

```
calc_knockback(response_choice, encounter_params) =
  0                                                if response_choice = "emergency_handling"
  encounter_params.knockback_distance_tanked        if response_choice = "tank"
  encounter_params.knockback_distance_retreat       if response_choice = "retreat"
```

**变量表**：

| 变量 | 符号 | 类型 | 值域 | 说明 |
|------|------|------|------|------|
| `response_choice` | — | enum | {emergency_handling, tank, retreat} | 应急处理无击退 |
| `encounter_params.knockback_distance_tanked` | — | float | 8.0（guard） | 硬抗击退距离，来自威胁配置表 C8 |
| `encounter_params.knockback_distance_retreat` | — | float | 10.0（guard） | 撤退击退距离，来自威胁配置表 C8 |
| `result` | — | float | {0, 8.0, 10.0} | 击退距离（单位） |

**输出范围**：{0, 8.0, 10.0}。击退方向 = threat.position → player.position。

---

### 公式汇总

| # | 公式名 | Registry Key | 类型 | 关键依赖 |
|---|--------|-------------|------|---------|
| F-12-01 | 威胁结算主公式 | `resolve_threat` | 分段调度 | #5 (repair_kit), #8 (hull/module) |
| F-12-02 | 船体伤害计算 | `calc_hull_damage` | 均匀随机 | 威胁配置表 C8 |
| F-12-03 | 模块损伤判定 | `calc_module_damage` | 概率投骰 | #8 (module_state), 威胁配置表 C8 |
| F-12-04 | 应急可用性检查 | `check_emergency_available` | 布尔查询 | #5 (Pool 5) |
| F-12-05 | 击退距离计算 | `calc_knockback` | 查表 | 威胁配置表 C8 |

## Edge Cases

**EC-12-01: 低船体 + 无 repair_kit + 硬扛 → hull=0**
- **条件**：hull ≤ 12，玩家选择硬扛，damage roll ≥ 当前 integrity
- **处理**：integrity = max(0, integrity - damage) = 0（destroyed 波段）。船体不可再出航（#8 `can_depart()` 返回 `{false, ["hull_destroyed"]}`）。探索本身不终止——玩家仍可撤离、携带已搜刮物品离开探索点。hull=0 的后果由 #8 在返航后执行。#8 EC-11-08 已规定探索系统不自行终止探索。
- **玩家感知**：是。HUD 显示"船体严重损毁"警告（#8 EC-11-08），但撤离锚点仍可用。
- **设计意图**：不阻止玩家带着已有收获撤离——你可以把自己逼到极限，但不会因一次错误判断而丢失已搜刮的一切。

**EC-12-02: 硬扛伤害跨越波段边界**
- **条件**：一次硬扛伤害使 integrity 跨越波段边界（如 33→25 跨越 damaged→critical，33-8=25）
- **处理**：决策面板在显示船体状态时加入预估——若当前 hull ≤ 33（damaged 波段内，最小伤害 8 将 integrity 推入 ≤25 即 critical 波段）且玩家将光标悬停在"硬扛"上，面板显示警告文字"硬扛可能造成船体结构性恶化"。实际伤害结算由 #8 按跨波段规则执行（#8 EC-12 / AC-29），scars 增量按规定计算。
- **玩家感知**：是。决策面板的预测性警告。

**EC-12-03: 全部模块槽位为空**
- **条件**：`count(eligible_modules) = 0`（所有槽位为空），但玩家选择了硬扛
- **处理**：F-12-03 中 `count(eligible_modules) > 0` 判定失败 → `{module_damaged: false, target: null}`。模块损伤投骰被跳过——没有模块可以受伤。不产生错误。
- **玩家感知**：否。无模块时硬扛不显示模块风险提示。

**EC-12-04: 模块损伤命中已受损槽位**
- **条件**：硬扛触发模块损伤（P=0.30），但 `random_choice` 选中的槽位已处于 `damaged` 状态
- **处理**：F-12-03 的 `eligible_modules` 列表必须按 `actual_state`（非 `visible_state`）过滤——仅包含 `actual_state = installed` 的槽位（排除 `actual_state = damaged` 的槽位，无论其 `visible_state` 为何）。这保证了 `unchecked` 可见状态（对应 `actual_state = damaged`）的模块永远不会被选为目标——在 `unchecked` 状态下对 `apply_module_damage` 的调用会被 #8 拒绝或视为无操作（但正确的过滤可完全防止该调用发生）。若所有已安装模块的 `actual_state` 均为 `damaged`，则 `count(eligible_modules) = 0` → `module_damaged: false`。
- **玩家感知**：否。已受损模块不会二次受损。
- **设计意图**：防止模块损伤投骰变成"浪费的投骰"——已受损的模块不应再次被选中。过滤语义必须使用 `actual_state`（物理真实状态），而非 `visible_state`（玩家感知状态）。

**EC-12-05: 撤退后返回 + 应急处理同一威胁**
- **条件**：玩家先选择"撤退"（retreat_flagged=true），走开后获得 repair_kit（如从探索点其他位置拾取），返回同一威胁点，选择"应急处理"清除威胁
- **处理**：威胁被清除（is_active=false），但 `retreat_flagged` 保持 true——它不会被应急处理清除。若玩家后续撤离，λ_forced=0.25 仍生效。
- **玩家感知**：部分感知。威胁标记消失（已清除），但撤离结算时损耗率仍为 0.25。玩家可能困惑为何损耗未降低——撤离结算摘要应提示"本次探索中曾选择撤退"。
- **设计意图**：撤退是一次战略判断，即使后来清除了威胁，曾选择撤退的事实仍影响撤离损耗。

**EC-12-06: 击退方向退化**
- **条件**：玩家位置与威胁位置完全重叠（理论上不应发生，但防御性处理）
- **处理**：方向向量退化为零向量时，fallback 方向 = 威胁的 facing 方向（由 threat_point 配置定义）。若威胁无 facing 方向，使用随机单位向量。
- **设计意图**：防御性编程——不应因边缘坐标情况导致击退失败或崩溃。

**EC-12-07: 硬扛→hull=0 + 同时模块损伤**
- **条件**：一次硬扛同时触发 hull=0 和模块损伤（P=0.30 命中）
- **处理**：C4 结算序列保证先应用 hull_damage（步骤 5），后应用 module_damage（步骤 6）。此时模块在 destroyed 波段下被标记为 damaged——η_final = η_visible × 0 = 0（#8 D.2b）。模块状态正确转为 damaged，但有效效率为 0（因船体已崩溃）。修复船体后模块效率恢复至其 damaged 状态对应的效率值。
- **设计意图**：结算顺序有意如此——船体损伤优先处理，模块损伤正确记录。

**EC-12-08: 多次撤退标记**
- **条件**：玩家在同一探索会话中对多个不同威胁选择了"撤退"
- **处理**：`retreat_flagged` 是布尔值——多次撤退不叠加。第二次及之后的撤退只产生击退效果，不再改变 retreat_flagged（已为 true）。撤离损耗使用 λ_forced=0.25，不由撤退次数决定。
- **设计意图**：撤退代价固定——不给玩家叠加惩罚的压力。Pillar 4 的温和约束。

**EC-12-09: repair_kit 在 Pool 5 中零数量堆**
- **条件**：Pool 5 含 repair_kit ×0（空堆，因持久化损坏等异常情况）
- **处理**：`check_emergency_available` 要求 `quantity >= 1`。0 < 1 → false。应急处理按钮灰显。零数量堆不应正常存在（#5 的 `remove` 在 quantity 归零时应清理槽位），但本系统安全处理此异常。
- **玩家感知**：否。应急处理按钮灰显与正常"无 repair_kit"表现一致。

**EC-12-10: #12 未实现或不可用**
- **条件**：#11 调用 `resolve_threat()` 但 #12 接口返回 unavailable
- **处理**：与 #11 EC-11-12 一致——守卫威胁不触发、不造成伤害。threat_point.is_active 保持 true。当 #12 实现后，完整威胁结算流程生效。
- **玩家感知**：否。守卫威胁在 #12 就绪前处于 inert 状态。
- **设计意图**：允许 MVP 早期在 #12 就绪前测试探索流程。

## Dependencies

### 上游依赖（本系统依赖）

| 系统 | 依赖内容 | 关键接口 | 状态 |
|------|---------|---------|------|
| #11 探索/搜撤场景 | 守卫威胁触发时调用 `resolve_threat(threat_context)`；接收 `combat_result` 更新探索状态；执行击退位移；记录 retreat_flagged | `resolve_threat(threat_context) → combat_result` | Required（#11 In Review） |
| #5 资源/货物与容量 | Pool 5 中 repair_kit 数量查询；应急消耗执行 | `get_carried_contents_by_tag("repair-material")`, `consume_in_combat(resource_id, quantity)` | Required（#5 Approved） |
| #8 飞艇模块与船体状态 | 船体伤害应用、模块损伤应用、模块状态查询（判定 eligible 槽位） | `apply_hull_damage(amount)`, `apply_module_damage(slot_id, damage_type)`, `get_installed_slots()` | Required（#8 In Review, Round 2） |

### 下游依赖（依赖本系统）

| 系统 | 依赖内容 | 关键接口 | 状态 |
|------|---------|---------|------|
| #11 探索/搜撤场景 | 威胁结算结果（outcome, hull_damage, module_damage, knockback, retreat_flagged）决定探索子状态恢复和威胁持久状态 | `combat_result` 返回值 | Required |
| #8 飞艇模块与船体状态 | 船体伤害量写入 → integrity 扣减、波段转换、scars 递增；模块受损标记写入 → 效率下降 | `apply_hull_damage()`, `apply_module_damage()` | Required |
| #5 资源/货物与容量 | 应急消耗从 Pool 5 永久移除 | `consume_in_combat()` | Required |
| #17 反馈/特效/音频语义 | 威胁结算事件（suppressed/tanked/retreated）→ 音效和视觉反馈 | 信号/事件（具体由 #17 定义） | Soft（MVP 可用基础反馈，完整由 #17 覆盖） |

### 双向依赖校验

- **#11 ↔ #12**：#11 定义威胁触发和 `threat_context` 接口，#12 消费并返回 `combat_result`。#11 GDD 已列 #12 为下游（Interactions 表）。#12 本 GDD 列 #11 为上游。✅ 已对齐。
- **#12 ↔ #8**：#8 定义 `apply_hull_damage()` 和 `apply_module_damage()` 接口，#12 消费这些接口。#8 GDD 已列 #12 为下游消费者。✅ 已对齐。
- **#12 ↔ #5**：#5 定义 `consume_in_combat()` 和 `get_carried_contents_by_tag()` 接口，#12 消费这些接口。#5 GDD 已列 #12 为下游。✅ 已对齐。

### 间接依赖

| 系统 | 关系 | 说明 |
|------|------|------|
| #1 内容数据与状态注册表 | 间接（通过 #11） | 威胁配置表（threat_type、伤害范围、消耗成本）存储在注册表中 |
| #10 航行与路线风险 | 间接（通过 #11） | EncounterContext 中的航程结果影响探索初始状态，间接影响威胁遭遇的上下文 |
| #16 UI/HUD/航图界面 | 下游 | 决策面板 UI、船体状态显示、威胁警告标记 |

## Tuning Knobs

| # | 参数名 | 类型 | 安全范围 | MVP 值 | 影响 |
|---|--------|------|---------|--------|------|
| 1 | `guard_full_damage_min` | int | 5–15 | **8** | 硬扛最小伤害。低于 5 → 硬扛代价太低，应急处理价值被稀释；高于 15 → 单次命中可能跨越两个波段 |
| 2 | `guard_full_damage_max` | int | 10–20 | **12** | 硬扛最大伤害。与 min 的间距控制伤害波动：当前间距 4（约 1/3 均值），保留足够不确定性 |
| 3 | `guard_module_damage_chance` | float | 0.15–0.45 | **0.30** | 硬扛时模块受损概率。高于 0.45 → 硬扛几乎必然损失模块，威慑过强；低于 0.15 → 模块风险形同虚设 |
| 4 | `emergency_cost_repair_kit` | int | 1–2 | **1** | 应急处理消耗 repair_kit 数量。2 → 应急处理成本翻倍，可能使玩家宁愿硬扛 |
| 5 | `knockback_distance_tanked` | float | 5.0–12.0 | **8.0** | 硬抗击退距离（单位）。必须 > guard trigger_radius 最大值（6.0），否则击退后仍在触发半径内。低于 6.0 → 重触发循环 |
| 6 | `knockback_distance_retreat` | float | 8.0–15.0 | **10.0** | 撤退击退距离（单位）。大于硬抗击退以体现"撤退比硬扛走得更远"的差异化 |
| 7 | `hull_warning_threshold` | int | 8–20 | **12** | 硬扛选项显示"⚠ 船体严重受损"警告的 hull 阈值。设为 full_damage_max（12）——低于此值时一次硬扛可能将船体推至 0。太高 → 警告频繁出现；太低 → 警告失去预警价值 |

### 间接调参（由其他系统拥有，影响本系统行为）

| 参数 | 来源 | 对本系统的影响 |
|------|------|--------------|
| `trigger_prob["guard"]` = 0.70 | #11 Tuning Knobs #4 | 守卫靠近触发概率。降低 → 玩家更容易绕过威胁而不触发结算 |
| `trigger_radius[guard]` = 4-6 | #11 Tuning Knobs #5 | 守卫触发半径。必须小于 knockback_distance_tanked（8.0），否则击退后仍在触发半径内 |
| `λ_forced` = 0.25 | #11 Tuning Knobs #7 | 撤退后撤离损耗率。影响"撤退"选项的机会成本 |
| `extraction_loss_success_ratio` = 0.08 | #11 Tuning Knobs #6 | 正常撤离损耗率。与 λ_forced 的差值（0.17）定义"撤退的额外代价" |
| `hull_band_*` 系列 | #8 Tuning Knobs #4-7 | 船体波段阈值和惩罚系数。影响硬扛伤害的实际游戏感受（damaged 波段航速 -10%，critical 波段 -25% + 模块效率 ×0.8）——Tank 期望伤害 10，约 2 个 Tank 遭遇才会从 intact 进入 damaged |

## Visual/Audio Requirements

> **创意基调**：视听语言必须服务于克制的、航海日志式的船长幻想——"不是胜利的呐喊，而是决策后的平静"。Sensation 是《云海织航》审美优先级中最低的（排名 7/7）。威胁遭遇是信息事件，而非动作事件。设计检验标准：新玩家在第一次威胁遭遇后将其描述为"船提醒我那里有状况"，而非"我被攻击了"。

### Visual

| # | Trigger | Visual | Duration/Spec |
|---|---------|--------|---------------|
| V-01 | Threat triggers (enters trigger_radius) | Subtle border color shift to warm amber/copper at screen edges, like a navigation instrument status light illuminating. No red. | 0.6s ease-in-out, single play |
| V-02 | Threat triggered, decision panel appears | Dark semi-transparent overlay (60% opacity, deep navy #1a1a2e rather than pure black), centered panel slides in | Slide-in 0.25s, ease-out |
| V-03 | Persistent: hull status display | Segmented bar (0-100 width), color-coded by band: Green #4CAF50 (intact, 100-76) / Yellow #FFC107 (damaged, 75-26) / Orange #FF9800 (critical, 25-1) / Red #F44336 (destroyed, 0), with band label text + current integrity number. Band segments separated by subtle hatch pattern for colorblind distinction. | Bar width 65% of panel width, height 22px |
| V-04 | Persistent: response buttons | Three distinct buttons with clear visual hierarchy: Emergency Handling = Blue/Teal (safe, resource cost), Tank = Orange/Red (dangerous, free), Retreat = Gray/Neutral (safe, retreat cost). Shared cool-tone undertone on both safe options (Emergency + Retreat) to visually group "no hull damage" options. | Full width, min height 44px, keyboard shortcuts 1/2/3 |
| V-05 | Button hover | Highlight border (2px, brighter than idle), Tank: always show damage range "8–12 船体伤害" as subtitle. When hull ≤ 33 (minimum 8 damage crosses into critical ≤25): additionally show cross-band preview "硬扛可能造成船体结构性恶化". | 150ms delay on hover |
| V-06 | Button disabled | Desaturated, 50% opacity, lock icon | Emergency Handling disabled when repair_kit = 0 |
| V-07 | Tank warning marker | Yellow warning triangle icon + text "⚠ 船体严重受损" beside Tank button label | Visible when hull ≤ 12 |
| V-08 | Resolution feedback — suppressed | Threat marker smoothly fades out on minimap (opacity 1.0 → 0 over 0.5s). No screen flash. The satisfying moment is the marker disappearing. | Fade 0.5s, ease-out |
| V-09 | Resolution feedback — tanked | Hull bar smoothly decrements to new value (animation, 0.3s). New hull number briefly pulses (scale 1.0 → 1.15 → 1.0, 0.4s) to register the change, then settles. No camera shake, no floating damage number, no screen flash. Player pushed back per V-11. | Bar animation 0.3s ease-out; number pulse 0.4s |
| V-10 | Resolution feedback — retreated | Muted amber hue shift at screen edges (matching V-01 tone), player pushed back per V-11. A small text label fades in beside inventory summary: "撤离损耗增至 25%" (2s, then fades out). | Label fade-in 0.3s, display 2s, fade-out 0.5s |
| V-11 | Knockback movement | Player sprite translates along knockback direction, stops at obstacle surface (`move_and_collide`) or travels full distance | Interpolation 0.3-0.5s (distance-based), ease-out |
| V-12 | Minimap threat indicator | Active threat = pulsing amber dot (pulse period 1.5s, opacity alternates 0.6 ↔ 1.0). Suppressed/cleared threat = dot removed (fades out per V-08). | Pulse via `_process` alpha oscillation or `TIME` shader uniform |
| V-13 | Hull critical HUD pulse | When hull ≤ 20, hull bar on persistent HUD pulses subtly (opacity 1.0 ↔ 0.7, 3s period). Visual only — no audio component. Replaces the horror-genre heartbeat trope. | 3s period, continuous while hull ≤ 20 |

### Audio

| # | Trigger | Audio | Spec |
|---|---------|-------|------|
| A-01 | Threat triggers | Low ship's bell or soft chime — a single clear, resonant tone, as if the ship's navigation system has detected something. Followed by a quiet ambient shift: existing exploration ambient subtly changes (distant wind picks up, or a low mechanical hum fades in at -22dB). No alarm, no sting. | Bell: 0.8s one-shot, -14dB; ambient shift: fades in 0.5s |
| A-02 | Emergency Handling selected | Soft mechanical sound — metal clicks, a toolkit closing. Reassuring, not urgent. | 0.8s, -16dB |
| A-03 | Tank selected | Low, resonant impact — like a ship hull absorbing a strike. Deep, muffled, absorbed. Not a crash — a thud. | 0.8s, -14dB (felt more than heard) |
| A-04 | Retreat selected | Whoosh/escape sound — fast air movement, receding | 0.6s, -14dB |
| A-05 | Resolution complete | Ambient shift (from A-01) fades out, exploration atmosphere restored | Fade-out 0.5s |
| A-06 | — | **已移除。** 心跳声（原 A-06）属于恐怖类型手法，与克制的船长幻想相矛盾。替换为独立的视觉 HUD 脉冲（V-13）。 | — |

## UI Requirements

### Decision Panel Layout

```
┌─────────────────────────────────┐
│  ⚠ Guard Sentinel — Auto-Defense │  ← Threat name + short description
│                                 │
│  Hull Status                     │
│  ┌─────────────────────────┐    │
│  │██████████████░░░░░░░░░░░│    │  ← Hull bar (Green/Yellow/Orange/Red), 65% panel width
│  └─────────────────────────┘    │
│  Damaged — 47 / 100             │  ← Band label + value
│                                 │
│  ┌─────────────────────────┐    │
│  │ [E] 🔧 Emergency     [1x]  │    │  ← Blue/Teal (available)
│  └─────────────────────────┘    │
│  ┌─────────────────────────┐    │
│  │ [T] ⚠ Tank       ⚠ Danger │    │  ← Orange/Red (always available, warning when hull ≤ 33 or hull ≤ 12 severe)
│  └─────────────────────────┘    │
│  ┌─────────────────────────┐    │
│  │ [R] ← Retreat    Loss 25% │    │  ← Gray (always available)
│  └─────────────────────────┘    │
│                                 │
│  Carried: 🔧×2  📦×5           │  ← Inventory summary row
└─────────────────────────────────┘
```

### Specs

| # | Element | Spec |
|---|---------|------|
| UI-01 | Panel size | Width 380px, height auto (min 320px), responsive scaling on smaller screens |
| UI-02 | Panel position | Screen center (horizontal and vertical), z-index above all exploration UI |
| UI-03 | Hull bar | Width 260px, height 22px, four segments (band boundaries: 100/76/26/0), 1px divider at each boundary |
| UI-04 | Hull value | Right of bar, centered: "[Band Label] — [Current] / 100", font 14px |
| UI-05 | Buttons | Full width 340px, min height 44px (accessibility tap target), 8px gap, 6px border radius |
| UI-06 | Button labels | Left-aligned, font 16px, bold. Subtitle on right (cost/consequence preview), font 13px |
| UI-07 | Keyboard shortcuts | Semantic key bindings: `[E]` Emergency Handling, `[T]` Tank, `[R]` Retreat. Keycap hint overlay on each button (e.g., `[E]`), positioned at button left edge, font 12px. Keys work regardless of panel open/closed state when threat is active |
| UI-08 | Disabled button | Background desaturated to grayscale, text 60% opacity, 🔒 lock icon overlay on right |
| UI-09 | Disabled button tooltip | Appears on 300ms hover, max width 250px, shows requirement text. Arrow pointing at button. z-index above panel |
| UI-10 | Tank hover preview | Appears on 150ms hover. If hull ≤ 33: shows "硬扛可能造成船体结构性恶化". If hull ≤ 12: shows "⚠ 船体严重受损 — 硬扛可能导致船体崩溃" |
| UI-11 | Inventory summary row | Panel bottom, 8px separator line above buttons. Shows icon + count per carried item type, min 60px per item. repair_kit count ≥ 1 = green, 0 = gray |
| UI-12 | Panel animation | Slide-in: from y+40px + opacity 0 to y=0 + opacity 1, 250ms, ease-out. Dismiss: reverse, 200ms |
| UI-13 | Background overlay | Fullscreen, 60% black opacity, blocks click-through to exploration UI. Clicking overlay or pressing Esc dismisses panel (exploration stays paused). Panel can be reopened via persistent "Threat Active" indicator at screen top |
| UI-14 | Hull critical pulse | When hull ≤ 20 and panel visible, hull bar pulses at 2s period between full opacity and 70% |
| UI-15 | Threat Active indicator | When threat active and panel is dismissed: amber dot icon appears at screen top (position: center-x of screen top, y=12px). Pulsing opacity 0.6↔1.0, 1.5s period (matching minimap indicator V-12). On click: reopens decision panel. Tooltip: "威胁活跃 — 点击以打开决策面板". Keyboard shortcut: same as panel-open key (default: Space) |

| Hull State | Hull Bar Color | Tank Preview Text | Button Warning |
|------------|---------------|-------------------|----------------|
| intact (100-76) | Green #4CAF50 | None | None |
| damaged (75-26) | Yellow #FFC107 | "硬扛可能造成船体结构性恶化" (when hull ≤ 33 — minimum 8 damage crosses into critical ≤25) | ⚠ when hull ≤ 12 |
| critical (25-1) | Orange #FF9800 | "硬扛可能造成船体结构性恶化" | ⚠ when hull ≤ 12 |
| destroyed (0) | Red #F44336 | N/A (cannot depart, but panel may still display during exploration) | ⚠ always |

## Open Questions

All ACs follow Given-When-Then format. ACs marked [DETERMINISTIC] produce identical results every run. ACs marked [RANGE] verify output within defined boundaries. ACs marked [SEEDED] use a fixed RNG seed for reproducibility.

### Core Rules (C1–C8)

**AC-12-01 — Threat context receipt triggers AWAITING_RESPONSE** [DETERMINISTIC]
- **Given** System in IDLE state, #11 calls `resolve_threat(threat_context)` with `threat_context = {threat_type: "guard", threat_id: "g-b1", position: (120, 45), encounter_params: {...}}`
- **When** Call arrives
- **Then** System transitions to AWAITING_RESPONSE, exploration movement pauses, decision panel displays threat description, hull status, and response options

**AC-12-02a — Emergency Handling available when condition met** [DETERMINISTIC]
- **Given** Threat active, Pool 5 contains repair_kit ×2
- **When** Decision panel renders
- **Then** Emergency Handling button is enabled with no tooltip; Tank and Retreat buttons also enabled

**AC-12-02b — Emergency Handling unavailable when no repair_kit** [DETERMINISTIC]
- **Given** Threat active, Pool 5 contains repair_kit ×0
- **When** Decision panel renders
- **Then** Emergency Handling button grayed out, tooltip: "需要 repair_kit ×1（随身物品栏中无可用）"

**AC-12-02c — Tank warning threshold at hull ≤ 12** [DETERMINISTIC]
- **Given** Threat active, hull integrity = 11
- **When** Decision panel renders
- **Then** Tank button label shows "⚠ 船体严重受损" warning but remains clickable (not blocked)

**AC-12-03 — Resolution sequence: validation failure changes no state** [DETERMINISTIC]
- **Given** Threat active, Pool 5 contains repair_kit ×0 (Emergency Handling unavailable)
- **When** Player somehow submits `response_choice = "emergency_handling"` (corrupted client or race condition)
- **Then** System returns ERR_UNAVAILABLE; no resources consumed, no damage applied, threat state unchanged, no knockback, exploration stays paused

**AC-12-04a — Emergency Handling resolution produces suppressed outcome** [DETERMINISTIC]
- **Given** Threat active, Pool 5 contains repair_kit ×3, hull = 62
- **When** Player selects Emergency Handling, system executes resolution sequence
- **Then** 1 repair_kit consumed from Pool 5, hull stays 62 (0 damage), no module damage applied, threat_point.is_active = false, combat_result = {outcome: "suppressed", hull_damage: 0, module_damage: null, resources_consumed: [{repair_kit, 1}], knockback: null, retreat_flagged: false}

**AC-12-04b — Tank resolution produces tanked outcome** [RANGE]
- **Given** Threat active, hull = 62, both modules installed (scout in slot_a, cargo in slot_b)
- **When** Player selects Tank and system executes resolution sequence (tested across 1,000 independent calls with fresh RNG each time)
- **Then** hull_damage ∈ [8, 12] for all calls (never outside range), minimum observed = 8, maximum observed = 12, mean ≈ 10.0 ± 0.13 (SE = 1.41/√1000 = 0.045, 99% CI ± 0.115); module_damage proportion with `module_damaged = true` ∈ [0.263, 0.338] (99% CI for 30% binomial at n=1,000; SE = √(0.3×0.7/1000) = 0.0145, z=2.576); resources_consumed = null; knockback = {direction: threat→player, distance: 8.0}; retreat_flagged = false; threat_point.is_active = true

**AC-12-04c — Retreat resolution produces retreated outcome** [DETERMINISTIC]
- **Given** Threat active, hull = 62, both modules installed
- **When** Player selects Retreat, system executes resolution sequence
- **Then** hull_damage = 0, module_damage = null, resources_consumed = null, knockback = {direction: threat→player, distance: 10.0}, retreat_flagged = true, threat_point.is_active = true

**AC-12-05 — combat_result contract matches defined schema** [DETERMINISTIC]
- **Given** Any valid threat resolution completed
- **When** combat_result returned to #11
- **Then** Struct contains all required fields: outcome ∈ {"suppressed", "tanked", "retreated"}, hull_damage ∈ int ≥ 0, module_damage is null or {slot_id: StringName, damage_type: StringName}, resources_consumed is null or [{resource_id: StringName, quantity: int}], knockback is null or {direction: Vector2, distance: float}, retreat_flagged ∈ bool

**AC-12-06a — Outcome cascade: hull damage → #8** [DETERMINISTIC]
- **Given** Tank resolution produces hull_damage = 15
- **When** Resolution sequence executes step 5
- **Then** `#8.apply_hull_damage(15)` called; integrity reduced by 15; band may transition

**AC-12-06b — Outcome cascade: module damage → #8** [DETERMINISTIC]
- **Given** Tank resolution produces module_damage = {slot_id: "slot_a", damage_type: "guard_impact"}
- **When** Resolution sequence executes step 6
- **Then** `#8.apply_module_damage("slot_a", "guard_impact")` called; actual_state → damaged; efficiency set to 0.6 (scout) or 0.5 (cargo)

**AC-12-06c — Outcome cascade: resource consumption → #5** [DETERMINISTIC]
- **Given** Emergency Handling resolution consumes 1 repair_kit
- **When** Resolution sequence executes step 2
- **Then** `#5.consume_in_combat("repair_kit", 1)` called; resource permanently removed from Pool 5

**AC-12-06d — Outcome cascade: retreat flag → #11** [DETERMINISTIC]
- **Given** retreat_flagged = true
- **When** #11 invokes `extraction_loss_settlement`
- **Then** Extraction loss uses λ_forced = 0.25 instead of λ_success = 0.08

**AC-12-07 — Re-trigger protection: knockback distance > trigger radius** [DETERMINISTIC]
- **Given** Guard threat trigger_radius = 6, player within 4 units of threat position, player selects Tank (knockback 8 units)
- **When** Knockback applied
- **Then** Player position ≥ 8 units from threat (barring collision blockage in knockback direction), exceeding 6-unit trigger_radius, no immediate re-trigger

**AC-12-08 — Threat type configuration read from data** [DETERMINISTIC]
- **Given** Threat config table defines a non-guard type with full_damage_range = [20, 30], module_damage_chance = 0.70, different emergency_cost
- **When** `resolve_threat` called with this threat type, Tank selected
- **Then** hull_damage ∈ [20, 30], module_damage_chance = 0.70 — behavior is config-driven, not hardcoded

### Formulas (F-12-01 through F-12-05)

**AC-12-09 — F-12-02: Hull damage range is [8, 12]** [RANGE]
- **Given** `response_choice = "tank"`
- **When** `calc_hull_damage("tank", guard_encounter_params)` called 1,000 times
- **Then** Every result ∈ [8, 12] inclusive, minimum = 8, maximum = 12, mean ≈ 10.0 ± 0.5

**AC-12-10 — F-12-03: Module damage excludes already-damaged slots** [DETERMINISTIC]
- **Given** slot_a = damaged, slot_b = installed, `eligible_modules = ["slot_b"]`, module_damage_chance = 1.0 (force hit for test), RNG for slot selection seeded
- **When** `calc_module_damage("tank", encounter_params, module_state)` called with slot_a already in damaged state (excluded from eligible_modules)
- **Then** target_slot_id selects slot_b (slot_a excluded from eligible pool), result = {module_damaged: true, target_slot_id: "slot_b"}

**AC-12-11 — F-12-04: Emergency availability check boundary** [DETERMINISTIC]
- **Given** carried_inventory = {repair_kit: 1}
- **When** `check_emergency_available(carried_inventory, "repair_kit")` called
- **Then** result = true (boundary: exactly 1, ≥ 1)
- **And** when carried_inventory = {repair_kit: 0}, result = false

**AC-12-12 — F-12-05: Knockback distance matches response choice** [DETERMINISTIC]
- **Given** guard encounter_params
- **When** `calc_knockback` called with emergency_handling, tank, retreat respectively
- **Then** Results are 0, 8.0, 10.0 respectively

### Edge Cases (EC-12-01 through EC-12-10)

**AC-12-13 — EC-12-01: Low hull tank → hull = 0, exploration continues** [SEEDED]
- **Given** hull = 7, Tank selected (min damage 8 > 7 hull → guaranteed destroy), retreat_flagged = false
- **When** Resolution completes
- **Then** integrity = 0 (destroyed band), combat_result returns outcome = "tanked", #11 does not terminate exploration, extraction anchor still available, #8 `can_depart()` returns {false, ["hull_destroyed"]}

**AC-12-14 — EC-12-02: Cross-band damage warning display** [DETERMINISTIC]
- **Given** hull = 33 (damaged band, edge: 8 damage = 25 which crosses into critical ≤25), threat active
- **When** Player hovers cursor over Tank button
- **Then** Panel displays warning: "硬扛可能造成船体结构性恶化"
- **And** When hull = 34 (8 damage = 26, stays in damaged band), no warning displayed

**AC-12-15 — EC-12-03: Tank with all slots empty** [DETERMINISTIC]
- **Given** slot_a = empty, slot_b = empty, Tank selected, module_damage_chance = 1.0 (forced)
- **When** `calc_module_damage` computed
- **Then** result = {module_damaged: false, target_slot_id: null} — no error thrown, no slot selected

**AC-12-16 — EC-12-05: Retreat then Emergency Handling on same threat retains retreat_flagged** [DETERMINISTIC]
- **Given** Player previously selected Retreat on threat g-b1 (retreat_flagged = true), later acquires repair_kit, returns to same threat, selects Emergency Handling
- **When** Emergency Handling resolution completes (threat is_active = false), player leaves exploration point and triggers extraction settlement
- **Then** retreat_flagged remains true, λ_forced = 0.25 applied to extraction loss despite threat being cleared

**AC-12-17 — EC-12-06: Knockback direction degeneracy fallback** [DETERMINISTIC]
- **Given** Player position = threat position = (100, 100) (overlap, zero vector), threat has configured facing direction (1, 0)
- **When** Knockback direction computed
- **Then** Direction falls back to threat facing (1, 0); knockback applied successfully (no crash, no null)

**AC-12-18 — EC-12-07: Tank → hull = 0 with simultaneous module damage** [SEEDED]
- **Given** hull = 10, both modules installed, Tank selected, damage roll seeded to 10 (yields hull = 0), module_damage_chance = 1.0 (force hit for test)
- **When** Resolution sequence executes
- **Then** Step 5 executes before Step 6: hull_damage applied first (integrity → 0), then module_damage applied (target module actual_state → damaged, η_effective = 0 due to η_final = η_visible × 0 under destroyed band)

**AC-12-19 — EC-12-08: Multiple retreats do not stack** [DETERMINISTIC]
- **Given** Player retreats from threat g-b1 (retreat_flagged = true), later retreats from threat g-b2
- **When** Second retreat resolves
- **Then** retreat_flagged stays true (already true, unchanged); extraction loss = λ_forced = 0.25 (not determined by retreat count)

**AC-12-20 — EC-12-09: Zero-quantity stack handled safely** [DETERMINISTIC]
- **Given** Pool 5 contains repair_kit stack with quantity = 0 (anomalous persistence state)
- **When** `check_emergency_available(carried_inventory, "repair_kit")` called
- **Then** result = false (0 < 1), Emergency Handling button grayed out, identical behavior to normal missing repair_kit

**AC-12-21 — EC-12-10: #12 unimplemented — no damage** [DETERMINISTIC]
- **Given** #12 not implemented or returns unavailable, #11 calls `resolve_threat()` on guard trigger
- **When** Interface returns unavailable
- **Then** No damage applied, no resources consumed, threat_point.is_active stays true, exploration continues normally

**AC-12-22 — EC-12-04: Tank with all installed modules already damaged** [DETERMINISTIC]
- **Given** slot_a = damaged, slot_b = damaged, module_damage_chance = 1.0 (force hit), Tank selected
- **When** `calc_module_damage` computed
- **Then** eligible slot count = 0 (damaged status excluded), result = {module_damaged: false, target_slot_id: null}

**AC-12-23 — Knockback collision: blocked knockback stops at obstacle** [DETERMINISTIC]
- **Given** Player at (100, 100), threat at (100, 94) (straight above direction), retreat knockback 10 units direction (0, -1), collision body at y = 95
- **When** Knockback applied
- **Then** Player position clamped at y = 95 (obstacle surface), does not pass through collision body

### Coverage Summary

| Category | AC Count | AC IDs |
|----------|----------|--------|
| Core Rules (C1–C8) | 12 | AC-12-01 through AC-12-08 (including sub-ACs) |
| Formulas (F-12-01 through F-12-05) | 4 | AC-12-09 through AC-12-12 |
| Edge Cases (EC-12-01 through EC-12-10) | 10 | AC-12-13 through AC-12-22 |
| Additional (collision) | 1 | AC-12-23 |
| **Total** | **27** | |

**Test coverage:** 8 core rules + 5 formulas + 10 edge cases = full coverage. Probabilistic ACs (AC-12-04b, AC-12-10, AC-12-13, AC-12-18, AC-12-22) use seeded RNG for deterministic reproducibility. AC-12-09 uses range assertion to verify under non-deterministic conditions.

## Open Questions

MVP 中无未解决问题。以下问题推迟至后续阶段：

- 额外的威胁类型（环境、陷阱、巡逻——推迟至垂直切片阶段）
- 多威胁同时触发的行为（推迟至 ≥2 种威胁类型时）
- 威胁难度缩放（推迟至游戏平衡阶段）
- 伙伴协助解决威胁（推迟至 #15 伙伴功能与关系实施时）
