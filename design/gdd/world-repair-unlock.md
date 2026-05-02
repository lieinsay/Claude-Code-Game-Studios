# 世界修复与解锁

> **Status**: Re-Revised (re-review feedback applied — 4 BLOCKING + 2 RECOMMENDED fixed)
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-02
> **Implements Pillar**: 世界会回应照料; 规划先于冒险

## Overview

世界修复与解锁是 Progression 层的核心推进系统。玩家将探索收集的材料与情报
（#5 资源系统、#6 情报系统）通过不可逆的提交操作转化为永久的世界修复成果——
灯塔重亮、航标复位、航线稳定、设施恢复运作。系统在数据层维护每个修复节点的
状态机（未发现 → 可修复 → 已修复），并在玩家层驱动可见的世界状态变化：
熄灭的灯塔重新发光、断航的路线变为可通行、NPC 恢复活动。

MVP 规模为 1 个永久修复结果：修复一座灯塔/航标，解锁或稳定一条与之关联的航线，
并触发世界视觉反馈（光照、粒子、NPC 状态变化）。修复后的视觉呈现由
#17 反馈系统（Vertical Slice）扩展，但最小可用版本（灯塔重亮 + 航线解锁）
由本系统直接定义以确保 MVP 自我闭环。

本系统拥有修复条件判定与解锁状态；修复结果在村镇/市集中的交易与展示属于
#14 空港/村镇状态与集市交易。

## Player Fantasy

玩家扮演的不是征服者，而是世界的照料者。在云海荒芜之地，熄灭的灯塔是"被遗忘"的象征，
而修复它代表着"有人还在乎"。

核心幻想：**亲手点燃一盏灯，看见它照亮的航线重新出现在航图上。**

修复时刻的情感曲线：
1. **投入**——倾注辛苦收集的材料，每一份资源都是探索的果实
2. **仪式**——不可逆提交（`commit_deposit` 终态），决心与承诺的瞬间
3. **见证**——灯塔重亮，光晕扩散到航线，世界回应了照料（美学瞬间）
4. **确认**——航线变为可通行，航图更新，探索边界扩展（功能回报）

与 #8 飞艇探索系统形成"收束—展开"的情感节拍：探索收集材料与动机（发散），
修复赋予探索以意义与方向（收敛）。两者循环驱动核心循环：**探索 → 收集 → 修复 → 更多探索**。

美学先行，功能为基：玩家最先感受到的是视觉变化——灰暗的灯台重新发光——
这是"世界会回应照料"支柱最直接的证明。航线解锁是理性回报的延续。

## Detailed Design

### Core Rules

**1. 修复节点定义**

修复节点是注册表（#1）中 `kind=repair_node` 的静态实体，运行时状态由本系统持有。

MVP 修复节点 `repair_node.starlight_dock`（与注册表 #1 `repair_node.starlight_dock` 对齐）：

| 属性 | 值 | 说明 |
|------|-----|------|
| `node_id` | `repair_node.starlight_dock` | 唯一标识，与注册表 #1 对齐 |
| `name` | 天礁灯塔 | 显示名称 |
| `linked_location_id` | `location.glass-harbor-outskirts` | 郊外探索点，需从琉璃港短途飞行到达 |
| `required_resources` | `[{resource.repair_kit, 5}, {resource.basic_supply, 4}]` | 修复所需材料（repair_kit 缺口 1，降低首轮闭环阻塞风险） |
| `unlocked_routes` | `[route.sky-reef-arc-01]` | 修复后天礁弧航线从不可通行变为可通行 |
| `route_enhancement` | `hazard_reduction: 0.3` (比例) | 修复后航线 hazard 额外降低 30%（相对值） |
| `pre_repair_route_state` | `traversable: false` | 修复前该航线不可通行 |
| `visual_state_anchor` | `anchor.starlight_dock_beacon` | 供 #17 消费的视觉锚点 |

**2. 修复条件（三条件 AND）**

发起修复必须同时满足：

- **(a) 位置前提**：玩家当前位置 = 节点 `linked_location_id`（郊外探索点）。物理到达自动将 `unrevealed → known`，始终允许查看修复需求和提交材料。
- **(b) 材料前提**：#5 `can_deposit(node_id, required_resources)` 返回 `true`——源池中全部需求材料总量足够
- **(c) 状态前提**：节点 `repair_state == known`，已 `repaired` 节点不可再次修复

知识门控（#6）不再阻止物理交互。情报系统的知识状态仅影响：
- 航图上是否标记节点位置（#9 消费）
- 修复 UI 中材料清单的提示信息（已知材料 ≥ 情报揭示时显示数量建议，否则显示"？"）
- 解锁预览（能力解锁信息在情报不足时显示为"未知效果"）

**3. 修复流程（分批提交）**

同一节点的不同材料可跨多次访问分批提交。节点维护 `deposited` 计数器，最后一次提交触发修复完成。

1. **交互触发**：玩家在灯塔所在地与修复锚点交互 → UI 展示材料清单（已满足/未满足）、解锁预览、"提交后材料不可取回"警告
2. **确认提交**：玩家选择提交材料 → 调用 #5 `commit_deposit(node_id, offered_resources)`
3. **部分扣除**：提交的材料从源池进入 Pool 6（终态），节点 `deposited` 计数器更新
4. **进度检查**：调用 `repair_completion(node_id)` → 全部需求满足时触发 `known → repaired`
5. **下游通知**：发出 `repair_completed(node_id)` → 通知 #6 `on_repair_completed`（能力解锁）→ 通知 #9 航线增强 → 触发 #3 存档检查点
6. **视觉反馈**：`visual_state_anchor = repaired`，灯塔点亮动画，光晕扩散到航线

分批规则：
- 每次提交至少包含一种需求材料的部分或全部数量（玩家通过数量选择器指定提交量，默认填充缺口 `min(carried, required - deposited)`，防止误提交多余材料）
- 同种材料已满足需求数量后不可再提交（垃圾邮件守卫）
- 每次提交后触发微仪式：灯塔开始微弱闪烁，光晕随 progress 逐渐变亮
- 修复完成的完整视觉仪式在最后一种材料集齐时触发

**4. 解锁结果**

修复 `starlight_dock` 后：

| 解锁内容 | 具体效果 | 消费系统 |
|---------|---------|---------|
| 航线解锁 | `route.sky-reef-arc-01` 从不可通行变为可通行，hazard 降低 30% | #9 航图系统 |
| 能力解锁 | 满足 `ability.lighthouse-signal-interpretation` Path C 条件 | #6 情报系统 |
| 世界反馈 | 灯塔重亮、光晕粒子、关联 NPC 恢复活动 | #17 反馈系统 |
| 村镇状态 | `repair_completed` 信号驱动 NPC 活跃度和对话变化 | #14 村镇系统 |

**5. 不可逆性**

- `commit_deposit` 是 #5 终态操作，材料进入 Pool 6 后永久锁定
- `known → repaired` 为单向转换，所有反向转换被状态机拒绝
- 支撑 Pillar 2（世界会回应照料）：玩家的照料留下永久痕迹

---

### States and Transitions

修复节点三态状态机：

```
unrevealed ──[物理到达 OR knowledge_state >= identified]──→ known ──[commit_deposit 集齐全部材料]──→ repaired (终态)
```

| 状态 | 含义 | 进入条件 | 有效转出 |
|------|------|---------|---------|
| `unrevealed` | 节点存在但玩家未发现。航图上不可见标记，但物理到达后可交互。 | 初始状态 | → `known` |
| `known` | 玩家已发现节点（物理到达 OR 情报获知）。世界/航图中显示为损坏/休眠状态。可查看需求，可分批提交材料。 | 玩家到达节点位置 OR #6 将节点 `knowledge_state` 推进至 `>= identified` | → `repaired` |
| `repaired` | 终态。节点已永久修复，功能已解锁，视觉已切换。 | `commit_deposit` 使全部 `required_resources` 满足 | 无 |

**无效转换（拒绝）**：

| 尝试 | 拒绝方式 | 理由 |
|------|---------|------|
| `known → unrevealed` | 状态机拒绝 | 知识不可退化（#6 契约） |
| `repaired → known` | 状态机拒绝 | 修复不可撤销（Pool 6 终态） |
| `repaired → unrevealed` | 状态机拒绝 | 已修复不可遗忘 |
| 对 `repaired` 节点重复提交 | 返回 `ERR_ALREADY_REPAIRED` | 幂等守卫 |

---

### Interactions with Other Systems

| 系统 | 方向 | 接口 | 说明 |
|------|------|------|------|
| #1 Registry | 读取 | `query_entity(node_id)` | 读取修复节点静态定义（材料清单、关联航线、视觉锚点） |
| #3 Persistence | 写入 | `write_snapshot("progress.world-repair", data)` | `repair_completed` 信号触发存档检查点 |
| #5 Resources | 调用 | `can_deposit(node_id, resources) → bool` | 验证材料是否足量（源池总量检查） |
| #5 Resources | 调用 | `commit_deposit(node_id, resources) → Result` | 原子扣除材料至 Pool 6（终态），不可逆 |
| #5 Resources | 接收 | `deposit_committed(node_id)` 信号 | 确认提交成功，更新 deposited 计数器 |
| #6 Intel | 调用 | `query_knowledge_state(node_id) → state` | 读取节点知识状态（影响材料提示和解锁预览，不阻止交互） |
| #6 Intel | 通知 | `on_repair_completed(node_id)` | 触发能力解锁重评估（Path C: lighthouse-signal-interpretation） |
| #9 Chart | 通知 | `on_route_enhanced(route_id, enhancement)` | 修复完成后航线 hazard 降低/增强生效 |
| #14 Settlement | 通知 | `repair_completed(node_id)` 信号 | 驱动 NPC 活跃度/对话变化（具体展示由 #14 负责） |
| #17 Feedback | 写入 | `visual_state_anchor = repaired` | 灯塔视觉锚点切换，驱动重亮动画、光晕粒子 |

## Formulas

| 公式名 | 输入 | 输出 | 说明 |
|--------|------|------|------|
| `repair_node_state` | `node_id`, `trigger_event` | `{new_state, allowed}` | 状态机转换：校验 trigger_event 在当前状态下是否合法，返回转换后状态 |
| `deposit_validation` | `node_id`, `offer: {resource_id: quantity}` | `{valid: bool, violations: [str]}` | 校验提交材料是否匹配节点需求（类型无误、数量不超过缺口） |
| `repair_progress` | `node_id` | `progress ∈ [0.0, 1.0]` | 已提交材料占全部需求的比例，按需求条目计数取平均 |
| `repair_completion` | `node_id` | `bool` | 全部 `required_resources` 是否已满足——`known → repaired` 的前置条件 |
| `route_enhancement` | `node_id` | `[{route_id, effect_type, magnitude}]` | 修复完成后输出关联航线的增强效果列表 |

### 公式详述

**`deposit_validation(node_id, offer)`**：
```
若 node_id 不在注册表中 → violation: "invalid_node"
若 offer 为空或所有 quantity <= 0 → violation: "empty_offer"
对 offer 中每种 resource_id：
  若 resource_id 不在 required_resources 中 → violation: "invalid_material"
  若 offer[rid] > (required[rid] - deposited[rid]) → violation: "excess_quantity"
全部通过 → valid: true
```

**`deposit_committed` 信号负载**：
```
deposit_committed(node_id)
// #5 在 commit_deposit 成功后发出单参数信号（#5 契约 line 129）。
// #13 在接收信号时从自身调用记录中更新 deposited 计数器——
// #13 发起了 commit_deposit 调用，已知本次提交的 {rid: qty} 映射。
```
> **跨系统接口备注**：若 #5 在后续修订中扩展信号负载以携带 `{rid: qty}`，
> #13 可直接消费该负载替代内部记录。当前 #5 仅承诺 `deposit_committed(repair_node_id)`。

**`repair_progress(node_id)`**：
```
若 |required_resources| == 0 → 返回 0.0
对任一 rid，若 required[rid] == 0 → 该项视为已满足（progress 贡献 = 1.0）
progress = Σ(min(deposited[rid] / required[rid], 1.0)) / max(|required_resources|, 1)
返回 clamp(progress, 0.0, 1.0)
```

**`repair_completion(node_id)`**：
```
若 |required_resources| == 0 → 返回 false
对所有 rid ∈ required_resources：deposited[rid] >= required[rid] → true
任一不足 → false
```

## Edge Cases

| # | 情况 | 处理方式 | 理由 |
|---|------|---------|------|
| 1 | 玩家提交材料数量超过需求缺口 | `deposit_validation` 返回 `excess_quantity`，UI 层阻止提交，多余材料保留在源池 | 防止浪费，保护玩家资产 |
| 2 | 玩家对已 `repaired` 节点发起修复交互 | UI 不显示修复入口，若直接调用 API 返回 `ERR_ALREADY_REPAIRED` | 状态机幂等守卫 |
| 3 | 玩家位置不等于 `linked_location_id` 时尝试提交 | UI 不可交互（修复入口仅在到达探索点时激活），API 层拒绝请求 | 位置是修复条件之一 |
| 4 | 玩家物理到达但情报系统无知识（`knowledge_state < identified`） | 节点从 `unrevealed → known`（物理到达触发），可查看需求、提交材料，但材料清单中未通过情报确认的资源显示为"？"，解锁预览显示"未知效果" | 物理可见性优先于情报门控，Pillar 2 要求所见即可照料 |
| 5 | 玩家知识状态退化的极端情况（如存档回滚） | 加载存档后重新评估：若 `knowledge_state >= identified` 但节点 `repair_state == repaired`，以 `repaired` 为准（终态优先） | 修复不可逆，知识状态服从修复状态 |
| 6 | `commit_deposit` 原子操作失败（如 #5 内部错误） | 状态机不变，材料全部保留在源池，向玩家显示"提交失败，材料未消耗" | #5 保证原子性，失败则全部回滚 |
| 7 | 中途离开探索点（在分批提交间隙飞走） | 已提交材料保留在 Pool 6（终态），`deposited` 计数器不重置，下次返回可继续提交 | 分批提交的设计意图，进度持久化 |
| 8 | 分批提交中途存档/读档 | 加载存档后 `deposited` 计数器与保存时一致，`repair_progress` 保持不变，可继续提交 | 进度持久化是分批提交的基础契约 |
| 9 | 航线增强后 hazard 降至 0 以下 | `route_enhancement` 输出 hazard 做 `max(0, hazard - reduction)` 底限保护 | hazard 最低为 0（完全安全航线），不可为负 |
| 10 | 新游戏 / 存档重置 | 所有修复节点 `repair_state = unrevealed`，`deposited` 计数器清零 | 初始状态保证 |
| 11 | 最后一个材料提交时玩家背包正好空出一个特殊槽位 | 不触发任何额外事件——修复完成仪式独立于背包状态 | 修复关注节点状态，不关注背包空间变化 |
| 12 | 浏览器标签页在仪式期间被暂停后恢复 | 仪式计时器基于 `delta` 继续运行（不跳过不卡死），动画从当前进度恢复 | Web 平台设计约束（VERSION.md） |

## Dependencies

### 上游依赖（本系统依赖）

| 系统 | 依赖内容 | 关键接口 | 状态 |
|------|---------|---------|------|
| #1 Entity Registry | 修复节点静态定义（材料清单、关联航线、位置、视觉锚点） | `query_entity(node_id)` | Required |
| #3 Local Save / World State Persistence | 修复状态持久化 | `write_snapshot("progress.world-repair", data)` | Required |
| #5 Resources, Goods & Capacity | 材料消耗、容量约束、Pool 6 终态 | `can_deposit()`, `commit_deposit()`, `deposit_committed` 信号 | Required |
| #6 Player Knowledge & Intel | 修复目标情报解锁、修复后能力解锁触发 | `query_knowledge_state()`, `on_repair_completed()` | Required |
| #9 Chart & Route Planning | 航线可通行性变更、航线增强效果 | `on_route_enhanced(route_id, enhancement)` | Required |

### 下游依赖（依赖本系统）

| 系统 | 依赖内容 | 关键接口 | 状态 |
|------|---------|---------|------|
| #14 Airship Hub / Settlement State | 修复完成信号 → 村镇 NPC 活跃度/对话/交易变化 | `repair_completed(node_id)` 信号 | Required |
| #17 Feedback System | 视觉锚点状态 → 灯塔重亮动画、光晕粒子 | `visual_state_anchor` | Required (Vertical Slice) |

### 双向依赖校验

- **#5 ↔ #13**：#5 定义 `commit_deposit` 和 Pool 6，#13 定义修复节点需求和 `deposit_validation`。两方文档均已引用对方。
- **#6 ↔ #13**：#6 定义 `on_repair_completed` 和 `knowledge_state`，#13 定义 `repair_completion` 触发条件。Path C 能力解锁条件已在 #6 中引用本系统。
- **#3 ↔ #13**：#3 定义 `progress.world-repair` 快照包，#13 定义 `repair_completed` 信号触发存档检查点。

## Tuning Knobs

| # | 参数名 | 类型 | 安全范围 | MVP 值 | 说明 |
|---|--------|------|---------|--------|------|
| 1 | `repair_lighthouse_material_costs` | `Dict[id, qty]` | repair_kit: 2–8, basic_supply: 3–15 | `{repair_kit: 5, basic_supply: 4}` | 灯塔修复材料清单。repair_kit 起始 4 需 5（缺口 1），降低首轮闭环阻塞风险 |
| 2 | `route_hazard_reduction` | `float`（比例） | 0.1–0.5 | `0.3` | 修复后航线 hazard 降低比例（相对值），配合航线从不可通行变为可通行 |
| 3 | `repair_ceremony_duration_sec` | `float` | 3.0–8.0 | `5.0` | 最后一批材料提交后灯塔点亮仪式时长 |
| 4 | `repair_cost_to_loot_ratio` | `float` | 1.5–3.0 | `2.0` | 修复总成本相对单次探索预期产出的比例，驱动探索→修复循环节奏 |
| 5 | `max_repair_nodes_mvp` | `int` | 1 | `1` | MVP 阶段修复节点总数 |

## Visual/Audio Requirements

Creative Director 约束：MVP 必须承担"可见恢复"反馈的归属权（#17 为 Vertical Slice，
因此修复本身的视觉/世界反馈需在本系统内定义最低版本）。

### MVP 最低视觉规格

| 元素 | 状态 `known` | 状态 `repaired` | 说明 |
|------|-------------|----------------|------|
| 灯塔本体 | 灰暗/破损 sprite，静止 | 发光 sprite，暖黄色光晕，缓慢呼吸式明暗变化（±10% opacity, 周期 3s） | 2 帧切换 + modulate 动画 |
| 光束 | 无 | 从灯塔顶部向关联航线方向射出半透明光束（`Color(1.0, 0.9, 0.6, 0.3)`），持续循环 | 单条射线 sprite，alpha 脉动 |
| 周围粒子 | 无 | 暖色光点粒子（6-8 个），从灯塔向上浮升，2-4 秒生命周期，在灯塔周围 48px 半径内生成 | 极简粒子，不依赖 #17 粒子系统 |
| 航线光照 | 不显示 | 航线上覆盖半透明暖色高亮带，持续至首次航行后渐隐 | 由 #9 消费 visual_state_anchor |

### 音频

| 事件 | MVP 规格 |
|------|---------|
| 提交材料 | 短促确认音（金属/石料碰撞，<0.5s） |
| 最后提交/点亮 | 持续嗡鸣渐强 → 清脆"叮"声（2-3s），暗示装置启动 |
| 修复后环境 | 灯塔附近循环播放低频嗡鸣 + 间歇性钟鸣（超出 MVP 则省略） |

### 扩展路径

以上 MVP 规格直接写入本系统。#17 在 Vertical Slice 阶段可替换为更丰富的
粒子系统、动态光照着色器、以及环境音频空间化。

## UI Requirements

| 界面 | 内容 | 触发条件 |
|------|------|---------|
| 修复交互面板 | 节点名称、当前状态、材料清单（名称 + 图标 + 已提交/需求量 + 满足状态颜色——#FF3333 不足 / #33FF33 满足，禁用纯颜色区分需加图标辅助）、每种材料旁数量选择器（默认全部携带量）、解锁预览（情报不足时显示"未知效果"）、"确认提交"按钮（材料不足时灰态，`interactable = false`）、"取消"按钮 | 玩家在修复节点位置与锚点交互 |
| 提交确认弹窗 | "确认提交以下材料？提交后材料不可取回。" + 提交材料明细 + "确认"/"取消" | 玩家点击"确认提交" |
| 进度提示 | 顶部 toast："已提交 repair_kit ×3（还需 repair_kit ×2, basic_supply ×4）"，2-3 秒后消失 | 提交成功后 |
| 航图进度标记 | 航图上灯塔节点旁显示修复进度（如"3/5 repair_kit"）作为持久化参考 | 首次提交后持续显示，修复完成后切换为"已修复" |
| 未揭示到访提示 | 破损灯塔显示微弱交互光标，状态描述："一座损坏的灯塔——你不知道它的来历，但看起来可以修复。" | 玩家首次到达 `unrevealed` 节点 |
| 修复完成提示 | 全屏中央：灯塔名称 + "已修复" + 解锁内容摘要，3 秒后自动消失或点击关闭；解锁摘要事后可在航图/日志中回顾 | 最后一批材料提交后 |

## Acceptance Criteria

### 核心流程

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-1 | 玩家物理到达灯塔时始终可交互（查看需求、提交材料），即使情报系统无知识 | 新游戏 → 前往探索点 → 确认交互入口可用 |
| AC-2 | 情报获知灯塔后在航图上标记节点位置，材料清单显示具体数量建议（非"？"） | 执行情报揭露 → 检查航图标记 + 修复面板材料清单 |
| AC-3a | 材料不足时 `can_deposit` 返回 false，材料清单中不足行数量文字渲染为 #FF3333，已满足行渲染为 #33FF33，提交按钮 `interactable = false` | 携带不足材料前往 → 确认颜色状态和按钮灰态 |
| AC-3b | 玩家到达修复节点但背包中无任何匹配 `required_resources` 的材料：修复面板仍可打开（可查看需求清单），所有材料行渲染为不足状态，提交按钮灰态 | 清空背包 → 前往灯塔 → 交互 → 确认面板可打开 + 全部行红色 + 按钮灰态 |
| AC-3c | 数量选择器不允许输入 0 或负数 | 单元测试：尝试 offer `{repair_kit: 0}` → 确认 `empty_offer` violation |
| AC-4a | 分批提交：提交 repair_kit×3 → `deposited` 计数器更新为 3，`repair_progress < 1.0`，灯塔保持 known 视觉但开始微弱闪烁；再次提交 repair_kit×2 + basic_supply×4 → 全部满足 → 灯塔切入 repaired 视觉 | 分两次提交 → 验证中间状态渐进反馈和最终状态 |
| AC-4b | 单次提交（全部材料一次提交）：玩家携带 ≥5 repair_kit 且 ≥4 basic_supply → 打开面板 → 确认提交全部所需数量 → `deposited` 计数器直接满足需求，`repair_completion` 返回 true，仪式触发 | 携带足量材料一次提交 → 确认修复完成 + 视觉/下游通知全部触发 |

### 状态机

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-5 | 修复完成后再次前往灯塔，无修复交互入口，视觉保持 repaired 状态 | 修复后重访 → 确认不可再次修复 |
| AC-6 | 对已修复节点直接调用 API 返回 `ERR_ALREADY_REPAIRED` | 单元测试覆盖 |
| AC-7 | 提交超出需求数量的材料被 `deposit_validation` 拒绝 | 单元测试：携带 repair_kit×10（只需 5）→ 确认 excess_quantity violation |
| AC-8 | 提交无效材料类型被 `deposit_validation` 拒绝 | 单元测试：提交 `{invalid_material: 1}` → 确认 invalid_material violation |

### 下游通知

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-9 | 修复完成后航线 `route.sky-reef-arc-01` 从不可通行变为可通行，hazard 降低 30% | 对比修复前后 #9 的 `traversable` 和 `route_hazard` 值 |
| AC-10 | 修复完成后 `ability.lighthouse-signal-interpretation` 解锁条件满足 | 查询 #6 `query_ability_state("lighthouse-signal-interpretation")` → unlocked |
| AC-11 | 修复完成后 `progress.world-repair` 快照写入 | 触发修复 → 检查存档数据或单元测试断言 |

### 视觉反馈（Creative Director 约束）

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-12a | 修复完成后灯塔 sprite 从灰暗/破损（known）切换为发光（repaired） | 修复前后截图对比，确认 sprite 已切换 |
| AC-12b | 修复完成后暖黄色光晕出现，±10% opacity 呼吸动画运行（周期约 3s） | 目视确认光晕可见 + 计时验证呼吸周期 |
| AC-12c | 修复完成后半透明光束从灯塔顶部向关联航线方向射出，持续循环 | 目视确认光束方向指向 `route.sky-reef-arc-01` |
| AC-12d | 修复完成后暖色光点粒子从灯塔向上浮升，6-8 个，生命周期 2-4s，48px 半径内生成 | 目视确认粒子数量、方向、生命周期 |
| AC-12e | 提交材料时播放短促确认音（<0.5s 金属/石料碰撞） | 提交材料 → 确认音频播放且时长 <0.5s |
| AC-12f | 最后提交/点亮时播放持续嗡鸣渐强 → 清脆"叮"声（2-3s） | 提交最后一批材料 → 确认完整音频序列播放 |
| AC-12g | 修复后航线覆盖半透明暖色高亮带 | 修复后打开航图 → 确认 `route.sky-reef-arc-01` 高亮渲染 |
| AC-13 | 修复仪式持续 `repair_ceremony_duration_sec`（默认 5.0s ± 0.5s），期间 UI 关闭按钮可交互——点击关闭 UI 后仪式动画继续播放至完成 | 计时验证 + 确认 UI 关闭后动画继续 |
| AC-14 | 浏览器标签页在仪式期间被暂停后恢复，仪式计时器基于 `delta` 继续（不跳过不卡死），动画从当前进度恢复 | 标签页切出 → 等待 3s → 切回 → 确认仪式继续而非直接结束 |

### 持久化

| # | 验收条件 | 验证方法 |
|---|---------|---------|
| AC-15 | 分批提交中途存档 → 读档 → `deposited` 计数器与保存时一致，`repair_progress` 不变，可继续提交 | 提交部分材料 → 存档 → 读档 → 验证进度一致性 |

## Open Questions

1. 未来修复节点的层级扩展——T2/T3 节点材料类型何时引入？需与 #1 Registry 和 #8 Exploration 协调新区域开放节奏。
2. `route.sky-reef-arc-01` 增强后的"首次航行后渐隐"航线高亮时间窗口——是否需要可配置？当前 MVP 建议硬编码。
3. 灯塔修复仪式的音频是否在 MVP 中实现完整 2-3s 版本，还是先放占位音效？取决于音频资源的可用性。
