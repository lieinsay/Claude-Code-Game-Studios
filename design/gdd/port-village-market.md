# 空港/村镇状态与集市交易

> **Status**: Approved (2026-05-03)
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-03
> **Review Verdict**: NEEDS REVISION (R1) → NEEDS REVISION (R2) → APPROVED (R3 — 1 blocker resolved: pool destination 3→2 + 4 recommendations applied)
> **Implements Pillar**: 世界会回应照料; 规划先于冒险

## Overview

空港/村镇状态与集市交易是 World/Economy 层的村镇侧世界反馈系统。它在数据层
维护每个定居点的 NPC 活跃度、摊位开放状态和库存内容；在玩家层呈现为可步行到达、
可聚焦交互、可购买商品的集市摊位——每个摊位和每件货物都绑定于该村镇的身份与需求，
而非通用商店货架。

玩家通过 #4 移动系统步行靠近摊位、确认焦点、按下 Use，本系统接收 `use_requested`
后调用 #5 资源系统的购买接口（`validate_purchase` → `execute_purchase`）完成交易。
可购入商品分为五类：补给（航线消耗）、材料/零件（维修与修复）、本地特产（高价值、
地方身份）、情报（消耗后解锁知识条目），以及修复后才开放的特殊货物。

本系统的核心推动力来自 #13 世界修复与解锁。当玩家修复一座灯塔或设施后，
`repair_completed` 信号触发村镇侧的连锁变化：休眠摊位重新开放、已有摊位补货上新、
NPC 从稀疏恢复活跃、对话文本更新——让玩家看见"修复一个地方"如何转化为"这个地方又
活过来了"。这不是更好的购物清单，而是世界对玩家照料的回应，承担 Pillar 4
（世界恢复可见性）在村镇侧的 MVP 归属。

MVP 规模锁定为固定摊位购买 + 修复节点驱动的库存变化。不做价格模拟、供需模拟、
库存刷新经济或贸易路线算法。集市购买写入 #5 的资源/货物/容量，本系统不拥有货物
所有权。

## Player Fantasy

集市不是菜单里的商店列表，而是村落的生命体征。玩家每次走进琉璃港的集市
区域，看到的不是"更好的购物清单"——而是摊位重新开张、货架有了新货、NPC
从寥寥无几变得有来有往。摊位关门时村镇病着；摊位重开时脉搏恢复。这份
"见证"是本系统最核心的情感引擎。

两个支撑情感：

**识得一方水土。** 每个摊位和货物回答一个问题：这个地方靠什么活着？
琉璃港卖透镜维护套件，因为旧灯塔用过菲涅尔透镜。天礁弧卖抗风暴帆布，
因为那里的风需要特殊织法。玩家不是在逛通用商店——是在阅读一个地方的自传。
每次购买都是对本地身份的确认。

**留有照料痕迹。** 玩家的修复不是一次性的数值提交——当灯塔点亮后回到集市，
织帆大娘摊上多了一种"只有灯塔亮了才能编织的发光帆线"，老测量技师重新校准了
尘封半年的仪器。这些东西不是因为玩家升了级才出现，而是因为世界被照料了。
买走它们时，玩家感到的不是"我更强了"——而是"他们又能靠这个活了"。

这两个情感共同锚定 Pillar 2（世界会回应照料）的村镇侧表达：修复不只是解锁
更高级的商店，而是让一个具体地方、具体的人、具体的行当重新运转。

## Detailed Design

### Core Rules

**规则 1 — 摊位定义。** 每个定居点拥有一组固定的集市摊位。MVP 阶段，琉璃港设有
**4 个摊位**，每个摊位由一位具名 NPC 经营。MVP 中所有摊位均为杂货铺——商品在
各摊位间重叠，以营造"集市有人在交易"的生活感，而不需要专门的分类库存逻辑。
摊位的主题特色（透镜工坊、帆具铺、星图斋）通过 NPC 身份与对话体现，而非
机械性的分类锁定。

4 个摊位及其 NPC 定义于叙事内容文件 `design/narrative/glass-harbor.md`。
本 GDD 通过 `stall_id` 和 `npc_id` 引用它们。MVP 摊位结构：

| stall_id | npc_id | 基础商品 | 独占风味商品 | 解锁等级 |
|---|---|---|---|---|---|
| `stall.gh-lens-workshop` | `npc.wei` | 基础物资包, 修补帆布, 航线手记 | 透镜维护套件 | `open_basic` |
| `stall.gh-sail-shop` | `npc.yun` | 基础物资包, 修补帆布, 航线手记 | 抗风暴涂层 | `open_basic` |
| `stall.gh-chart-studio` | `npc.cen` | 基础物资包, 修补帆布, 航线手记 | 简易六分仪 | `open_basic` |
| `stall.gh-general` | `npc.atu` | 基础物资包, 修补帆布 | — | `open_basic`（默认开启） |

**规则 2 — 商品定义。** 每件可购买商品具有以下属性：
- `good_id`：唯一标识符，如 `good.basic-supply-bundle`
- `display_name`：玩家可见名称
- `category`：`supply`（补给）| `material_part`（材料/零件）| `local_speciality`（本地特产）| `intelligence`（情报）
- `price`：固定整数价格，具体数值在 Section G（Tuning Knobs）中统一调校
- `consumes`：购买后商品占用的资源池/物品槽（由 #5 系统定义）
- `local_identity_tag`：字符串引用键，指向 `design/narrative/` 中解释该商品与
  定居点关联的叙事文本

**规则 3 — 购买流程。**
1. 玩家步行进入摊位交互范围（#4 移动系统）。
2. 焦点高亮出现在摊位上；显示 NPC 名字和摊位标签。
3. 玩家按下 Use → #4 分发 `use_requested(stall_id)`。
4. 本系统接收信号，打开摊位购买界面。
5. 界面列出当前可购买商品（按摊位当前解锁等级过滤），含价格。
6. 玩家选择商品并确认购买。
7. 本系统调用 #5 的 `validate_purchase(good_id, quantity)`。
8. 成功：调用 #5 的 `execute_purchase(good_id, quantity)`，播放购买确认音效。
9. 失败（货币不足/容量不足）：#5 返回失败原因，界面显示错误提示。
10. 玩家可继续购物，或按取消关闭界面。

**规则 4 — 修复驱动的摊位变化。** 当 #13 系统发出 `repair_completed(node_id)` 信号时：
1. 本系统通过 #13 的注册表查询 `node_id`，获取 `linked_location_id` 和节点
   修复类型。
2. 将 `linked_location_id` 匹配到对应定居点：定居点实体定义中包含
	  `linked_location_ids` 列表（如琉璃港 `linked_location_ids = [location.glass-harbor-outskirts, ...]`），
	  系统据此反向查找 `linked_location_id` 所属的定居点。
3. 对该定居点的每个 `closed` 摊位，检查 `node_id` 是否属于该摊位的
   `required_node_ids` 集合。
4. 若匹配且摊位状态为 `closed` → 转换为 `open_basic`。NPC 从 `absent` 转换为
   `idle`。
5. `open_basic → open_expanded` 的转换在 MVP 中定义但不可达。第二个匹配修复
   触发 expanded 解锁为 post-MVP 行为。
6. 更新 `progress.settlement-market` 快照（#3 持久化系统）。

MVP 中，杂货摊（阿图）默认以 `open_basic` 状态开启（无需任何修复）——确保玩家
在任何修复完成之前至少有一个购买点。

**规则 5 — NPC 活跃状态。** 每个摊位 NPC 具有以下状态之一：
- `absent`：NPC 不在场，摊位呈现关闭外观。修复前状态。
- `idle`：NPC 在摊位，播放待机动画（擦柜台、伸懒腰）。有问候对话。
- `active`：NPC 播放制作/整理动画。有问候 + 复兴主题对话（如"灯塔修好以后，
  来往的人多了不少"）。

NPC 名字、对话文本、性格备注存放于 `design/narrative/glass-harbor.md`。
本 GDD 通过 `npc_id` 引用。本系统从该文件读取对话键以在摊位界面中显示。

**规则 6 — 商品可见性。** 摊位当前解锁等级下所有可用商品均可见且可购买。
MVP **不**模拟库存消耗——商品解锁后始终可购买。摊位在柜台/货架上展示物品模型，
未解锁的特色商品位置留空（视觉暗示"这里还能有更多"）。

**规则 7 — 商品分类。**

| 分类 | 用途 | MVP 商品 |
|---|---|---|
| 补给（Supply）| 航线行进中消耗（#10）；进入 #5 资源池 2（`in_storage`，飞艇仓库）| 基础物资包, 修补帆布, 透镜维护套件, 抗风暴涂层, 简易六分仪 |
| 材料/零件（Material/Part）| 维修动作消耗（#13）或合成 |（post-MVP）|
| 情报（Intelligence）| 消耗品，解锁日志中的知识条目 | 航线手记 |
| 本地特产（Local Speciality）| 高价值交易品，轻重量 |（post-MVP，`open_expanded` 解锁）|

MVP 交付 6 种商品：2 种通用补给（基础物资包、修补帆布）+ 3 种独占风味补给
（透镜维护套件、抗风暴涂层、简易六分仪）+ 1 种情报（航线手记）。
独占风味商品在机械上与基础物资包等价（同价、同消耗、同容量占用），
通过 `display_name` 和 `local_identity_tag` 赋予每个摊位独特的在地身份。
更多商品在 post-MVP 添加。

### States and Transitions

**定居点状态机：**

| 状态 | 条件 | 摊位状态 | NPC 行为 |
|---|---|---|---|
| `dormant` | 无修复完成 | 1 个摊位开启（杂货摊），3 个关闭 | 1 个 NPC idle，3 个 NPC absent |
| `recovering` | 1–2 个修复完成 | 2–3 个摊位开启 | 已开摊位 NPC idle；其余 absent |
| `active` | 全部关联修复完成 | 4 个摊位全部开启（均为 open_basic）| 全部 NPC 在场（均为 idle）|

**摊位状态机：**

| 状态 | 解锁条件 | 可用商品 | NPC 状态 |
|---|---|---|---|
| `closed` | 默认（杂货摊除外）| 无；摊位关闭 | NPC absent |
| `open_basic` | 首个匹配 `node_id` 的修复完成（杂货摊为默认）| 基础商品 + 独占风味商品 | NPC idle |
| `open_expanded` | （post-MVP）第二个匹配修复 | 基础 + 特色商品 | NPC active |

**状态转换表：**

| 从 | 到 | 触发器 |
|---|---|---|
| `dormant` | `recovering` | 首个匹配当前定居点的 `repair_completed(node_id)` |
| `recovering` | `active` | 全部关联修复节点完成 |
| `closed` | `open_basic` | 匹配的 `node_id` 的修复完成 |
| `open_basic` | `open_expanded` | （post-MVP）第二个匹配的修复节点完成 |
| `absent` | `idle` | 所属摊位转换为 `open_basic` |
| `idle` | `active` | （post-MVP）所属摊位转换为 `open_expanded` |

MVP 中不存在状态退化——修复是永久的。状态只向前推进。`open_expanded` 及其关联
的 `idle → active` 转换在 MVP 中定义但不可达。

### Interactions with Other Systems

| 系统 | 方向 | 数据流 | 接口归属 |
|---|---|---|---|
| **#5 资源/货物/容量** | ↓ 上游（依赖）| #14 发送：`validate_purchase(good_id, quantity)` → #5 返回 bool + failure_reason。`execute_purchase(good_id, quantity)` → #5 转移货物至 Pool 2（`in_storage`）、扣除货币。#5 提供：`get_storage_summary()` → `{total_volume, used_volume, stacks}`（#14 从中推导 `remaining_capacity = total_volume - used_volume` 和玩家货币持有量）| #5 拥有购买执行；#14 拥有商品定义与价格。#5 从内容注册表（#1）读取 `price` 后内部计算 `total_cost = price × quantity`，验证货币充足性与容量 |
| **#13 世界修复与解锁** | ↓ 上游（依赖）| #13 发出：`repair_completed(node_id)`。#14 通过 #13 注册表查询 `linked_location_id` 和节点修复类型，匹配摊位 `required_node_ids` → 摊位解锁、NPC 状态变化 | #13 拥有信号和注册表；#14 拥有响应逻辑 |
| **#4 移动与交互** | ↓ 上游（依赖）| #14 通过 #4 的焦点系统注册每个已开启摊位：`register_focus_target(stall_id, world_pos, label)`。#4 分发 `use_requested(target_id)` → #14 打开摊位界面 | #4 拥有焦点/交互分发；#14 注册目标 |
| **#3 持久化** | ↓ 上游（依赖）| #14 定义快照 schema：`{settlement_id, completed_node_ids: [node_id], stall_states: {stall_id: state}, npc_states: {npc_id: state}}`。#3 在存档/读档时保存/加载 | #3 拥有 I/O；#14 拥有 schema。`completed_node_ids` 为 F.2 公式在跨会话间追踪已完成的修复节点 |
| **#10 航行与路线风险** | ↑ 下游（被依赖）| 摊位购买的补给品作为航线消耗（#10 通过 #5 的 `consume_for_route()` 从 Pool 2 `in_storage` 消耗补给品）。情报商品解锁航线知识条目 | #10 通过 #5 引用商品；#14 定义哪些商品存在 |
| **叙事内容** | → 引用 | #14 读取 `npc_id` → 从 `design/narrative/glass-harbor.md` 获取名字、对话。`local_identity_tag` → 从同一文件获取风味文本 | 叙事文件拥有内容；#14 读取并显示 |

## Formulas

MVP 不涉及浮动价格、供需模拟或经济刷新算法。公式仅覆盖购买结算与解锁判定。

### F.1 购买总价

```
total_cost = price × quantity
```

- `price`：商品的固定单价（整数，> 0），在 Section G（Tuning Knobs）中为每个 `good_id` 指定
- `quantity`：玩家选择购买的整数数量，UI 层面强制 range ∈ [1, max_affordable]
  - `max_affordable = min(floor(player_currency / price), remaining_capacity)`
  - 下限 1：quantity ≤ 0 时购买按钮不可用
  - 若 `player_currency < price`：商品灰显，不可选
- 验证由 #5 执行：`validate_purchase(good_id, quantity)`。#5 从内容注册表（#1）
  读取 `price` 后内部计算 `total_cost = price × quantity`，验证
  `total_cost ≤ player_currency` 且购买后货物可装入对应资源池

**示例**：基础物资包 `price = 50`，玩家购买 3 个 → `total_cost = 150`。若玩家持有
200 货币且资源池有空位，购买成功。

### F.2 摊位解锁判定

```
unlock_triggered = |stall.required_node_ids ∩ completed_node_ids| ≥ unlock_threshold
```

- `stall.required_node_ids`：解锁该摊位所需的修复节点 ID 集合（例如 `{repair_node.starlight_dock}`）
- `completed_node_ids`：该定居点已完成的修复节点 ID 集合，由 #13 的
  `repair_completed(node_id)` 信号累积。集合天然去重——重复信号不产生重复条目
- 对于 MVP：`unlock_threshold_basic = 1`，首次匹配 → `closed → open_basic`
- Post-MVP：第二个匹配 → `open_basic → open_expanded`（状态机定义保留，`unlock_threshold_expanded = 2`）

**示例**：琉璃港帆具铺 `required_node_ids = {repair_node.starlight_dock}`。
玩家修复码头后 #13 发出 `repair_completed(repair_node.starlight_dock)`，
`completed_node_ids` 变为 `{repair_node.starlight_dock}` →
`|{starlight_dock} ∩ {starlight_dock}| = 1 ≥ 1`，帆具铺从 `closed` 变为 `open_basic`。

### F.3 定居点活跃度（聚合）

```
active_stall_count = COUNT({ stall_id | stall.state = open_basic })
```

- `active_stall_count = 1` → `dormant`（仅默认杂货摊）
- `1 < active_stall_count < total_stall_count` → `recovering`
- `active_stall_count = total_stall_count` → `active`

`active_stall_count = 0`：若定居点无默认开启摊位（见 E.6），属于 `dormant`
的特殊情况——所有摊位关闭，无 NPC 在场，集市区域无可交互目标。MVP 中琉璃港
不出现此状态（杂货摊始终开启）。

Post-MVP：当 `open_expanded` 状态在游戏中可达时，`active` 条件可扩展为
`active_stall_count = total_stall_count AND expanded_count ≥ 1`。

**示例**：琉璃港 `total_stall_count = 4`。初始仅杂货摊开启 → `active = 1` →
`dormant`。修复码头后帆具铺开启 → `active = 2` → `recovering`。全部关联节点
修复后 4 个摊位全开 → `active = 4` → `active`。

## Edge Cases

### E.1 交互已关闭摊位

玩家走到一个 `closed` 状态的摊位前并按下 Use。**处理**：#4 的焦点系统不会将
`closed` 摊位注册为可交互目标，因此 `use_requested` 不会被分发。摊位外观
呈现关闭状态（木板封门、无 NPC），玩家无法对其按下 Use。

### E.2 容量已满时购买

玩家背包/资源池已满，试图购买补给品。**处理**：#5 的 `validate_purchase` 检测到
目标资源池无剩余容量，返回 `false` + `failure_reason = "capacity_full"`。
本系统在摊位界面显示提示："携带空间不足，无法购买。"购买被阻止，不扣货币。

### E.3 货币不足

玩家持有货币不足以支付 `total_cost`。**处理**：#5 的 `validate_purchase` 返回
`false` + `failure_reason = "insufficient_funds"`。界面显示："货币不足。"
商品价格以灰色显示（不可选），购买按钮不触发确认。

### E.4 重复修复信号

同一个修复节点因 Bug 或边界条件发出两次 `repair_completed`。
**处理**：本系统在内部维护 `completed_node_ids` 集合——集合天然去重。
重复信号不会导致摊位重复解锁或状态倒退。

### E.5 存档期间摊位状态

玩家在摊位界面打开时触发存档（自动存档或手动存档）。
**处理**：摊位界面是只读展示 + 购买确认的模态——购买为原子操作（#5 的
`execute_purchase` 在一次调用中完成），不存在"购物车半满"的中间状态。
存档时，本系统的快照 schema 只记录摊位/NPC 状态，不记录界面状态。
读档后，摊位界面关闭，玩家回到摊位前。

### E.6 全摊位关闭（无默认杂货摊的定居点）

若未来定居点不设默认开启摊位，玩家到达时所有摊位均为 `closed`。
**处理**：此时该定居点的集市区域仍可步行进入，但无可交互摊位。NPC 全部 absent，
环境呈现冷清状态。这不阻塞游戏——玩家可通过修复（#13）解锁首个摊位。
琉璃港 MVP 不会出现此情况（杂货摊始终开启）。

### E.7 叙事文件缺失或 npc_id 无匹配

`design/narrative/glass-harbor.md` 不存在，或某个 `npc_id` 在该文件中未定义。
**处理**：NPC 名字回退显示为人类可读的默认名称"摊主"（而非 `npc_id` 原始字符串）。
对话文本和 `local_identity_tag` 回退为空字符串。摊位功能不受影响——购买流程不依赖
叙事内容。日志输出 warning 提示缺失内容。

> **实现前置条件**：在 #14 系统进入实现阶段前，`design/narrative/glass-harbor.md`
> 必须存在并通过 narrative-director 审核。E.7 的回退逻辑是安全网，不是交付目标。

### E.8 全部摊位已达 MVP 最大解锁

定居点所有摊位均已 `open_basic`（MVP 终态），新的 `repair_completed` 到达。
**处理**：状态机无有效转换——所有摊位已在终态。信号被安全忽略，不产生错误。

### E.9 购买界面打开期间收到修复信号

玩家在摊位购买界面打开时，该摊位因修复信号发生解锁等级变化。
**处理**：当前购买会话不受影响——界面继续显示打开时的商品列表。界面关闭并重新
打开后，反映新解锁等级的商品列表。

### E.10 修复信号同时匹配多个摊位

单个 `repair_completed(node_id)` 匹配两个不同摊位的 `required_node_ids`。
**处理**：两个摊位各自独立从 `closed` 转换为 `open_basic`，互不干扰。
NPC 各自从 `absent` 转换为 `idle`。

### E.11 摊位 required_node_ids 为空

某摊位（非默认杂货摊）配置了空的 `required_node_ids`。
**处理**：该摊位永远无法从 `closed` 解锁——交集基数为 0。配置验证工具应对此
发出 warning（post-MVP 中可能故意为"永久关闭"摊位保留空集）。

### E.12 修复节点 ID 不属于任何摊位的 required_node_ids

#13 发出 `repair_completed(node_id)`，但该 `node_id` 不出现在任何摊位的
`required_node_ids` 中。
**处理**：信号被安全忽略。这不产生错误——某些修复可能只影响其他系统（如航线
稳定性），不影响集市摊位。

### E.13 购买数量异常输入

玩家通过 UI 控件输入 quantity = 0、负值或非整数。
**处理**：UI 输入端强制 range ∈ [1, max_affordable]。减号按钮在 quantity = 1 时
灰显。文本输入框中任何 < 1 的值在失焦时自动 clamp 为 1。非整数值向下取整。

### E.14 玩家离开交互范围时界面保持打开

玩家在摊位界面打开时步行离开摊位交互范围。
**处理**：界面保持打开直到玩家手动关闭或再次按 Use。界面关闭后，若玩家仍在
范围外，按 Use 无反应——需重新进入范围才能交互。

### E.15 商品价格为 0

某商品因配置错误 price = 0。
**处理**：`total_cost = 0 × quantity = 0`。购买不扣除货币，货物正确进入资源池。
此为配置错误——调校验证应在数据导入时拒绝 price = 0 的商品（价格安全范围下限
为 20）。运行时允许零价购买是因为防御性逻辑不应崩溃，但日志应输出 error。

### E.16 玩家货币恰好等于 total_cost

边界值：玩家货币恰好等于本次购买的总价。
**处理**：购买成功（`total_cost ≤ player_currency` 通过），货币归零。购买后界面
更新货币显示为 0，所有商品因 `player_currency < price` 而灰显。

## Dependencies

### 上游依赖（本系统依赖）

| 系统 | 依赖内容 | 若不可用 |
|---|---|---|
| **#5 资源/货物/容量** | `validate_purchase(good_id, quantity)` → bool + failure_reason；`execute_purchase(good_id, quantity)` → void；`get_storage_summary()` → `{total_volume, used_volume, stacks}`（从中推导剩余容量）；货币作为 #5 跟踪的资源，通过 `get_storage_summary()` 查询持有量 | 购买流程完全无法执行；集市无功能 |
| **#13 世界修复与解锁** | `repair_completed(node_id)` 信号；#13 注册表中修复节点的 `linked_location_id` 和类型分类查询 | 摊位永远不升级；集市停留在初始状态 |
| **#4 移动与交互** | `register_focus_target(target_id, world_pos, label)`；`use_requested(target_id)` 分发 | 玩家无法与摊位交互；集市不可达 |
| **#3 持久化** | `progress.settlement-market` 快照的保存与加载 | 摊位状态在会话间丢失；每次进入游戏重置 |
| **货币获取系统（待分配）** | 玩家获取货币的操作（探索奖励、物品出售或其他来源）| 无货币来源则购买功能无法验证；价格无锚点 |

### 下游依赖（依赖本系统）

| 系统 | 依赖内容 | 本系统提供 |
|---|---|---|
| **#10 航行与路线风险** | 补给品作为航线消耗品；情报商品解锁航线知识 | 商品定义（`good_id`、类别、效果类型）经由 #5 传达 |
| **#7 飞艇家园 Hub** | 集市作为飞艇停靠后的可访问目的地 | 摊位焦点注册使集市区域成为有交互内容的空间 |

### 引用依赖（非运行时）

| 资源 | 内容 | 本系统如何使用 |
|---|---|---|
| `design/narrative/glass-harbor.md` | NPC 名字、对话、`local_identity_tag` 风味文本 | 运行时读取以填充摊位界面文本 |

## Tuning Knobs

每个调校参数包含：变量名、安全范围、影响的游戏体验维度。

### G.1 商品定价

| good_id | 建议 MVP 价格 | 安全范围 | 影响 |
|---|---|---|---|
| `good.basic-supply-bundle` | 50 | 20–100 | 基础补给获取频率；过低使探索无成本，过高使初期无法购买 |
| `good.repair-canvas` | 80 | 40–150 | 航线中消耗品补充节奏；与航线消耗速率联动 |
| `good.route-notes` | 120 | 60–200 | 情报获取门槛；过高使玩家跳过知识解锁 |
| `good.lens-maintenance-kit` | 50 | 20–100 | 与基础物资包同价，风味区分 |
| `good.storm-resistant-coating` | 50 | 20–100 | 与基础物资包同价，风味区分 |
| `good.simple-sextant` | 50 | 20–100 | 与基础物资包同价，风味区分 |

**联动约束**：航线单次消耗的补给品价值应小于单次航线收益的 30%，确保补充是
"有意义的成本"而非"禁止性门槛"。具体数值需与 #10 系统的航线收益公式协同调校。
风味商品与基础物资包功能等价——仅在不同摊位间创造身份差异，不创造价格差异。

### G.2 摊位解锁阈值

| 参数 | MVP 值 | 安全范围 | 影响 |
|---|---|---|---|
| `unlock_threshold_basic` | 1 个匹配 node_id | 1 | 首次修复即解锁——不可低于 1（否则修复无反馈），不可高于 1（否则首个修复无可见变化） |
| `unlock_threshold_expanded` | （post-MVP）2 个匹配 node_id | 1–3 | 控制 open_basic → open_expanded 转换节奏；MVP 中不可达 |

### G.3 定居点活跃度阈值

| 参数 | MVP 值 | 安全范围 | 影响 |
|---|---|---|---|
| `dormant_max_stalls` | 1 | 1 | 超过此值定居点进入 recovering；与默认开启摊位数量联动 |
| `active_min_stalls` | `total_stall_count` | 1–total_stall_count | MVP：全部摊位 open_basic → active。Post-MVP：可扩展为要求 expanded_count ≥ N |

### G.4 叙事内容

| 参数 | 说明 | 安全范围 | 影响 |
|---|---|---|---|
| `npc_greeting_text` | 每个 NPC 的问候对话 | 任意非空字符串 | 玩家对 NPC 的第一印象；空字符串会使 NPC 沉默 |
| `npc_recovery_text` | NPC active 状态的复兴对话 | 任意非空字符串 | 传达"世界在恢复"的情感反馈 |
| `local_identity_tag` | 每个商品的本地身份描述 | 任意非空字符串 | 玩家理解"这个地方为什么卖这个" |

这些参数的值存放于 `design/narrative/`，本 GDD 定义结构和安全范围。

## Visual/Audio Requirements

**视觉：**
- 摊位在 `closed` 状态呈现关闭外观（木板封挡、无货物展示、无 NPC）
- 摊位在 `open_basic` 状态呈现开张外观（柜台可见、基础商品模型陈列、NPC 在摊后 idle 动画）
- 摊位在 `open_expanded` 状态呈现更丰富的视觉效果（更多商品陈列、NPC active 制作动画、摊位装饰增多）
- 未解锁的特色商品位置留空（空位暗示"还有东西可以解锁"），而非完全不显示
- 定居点处于 `dormant` 状态时，集市区域整体偏冷色调；`recovering` 时暖色调逐渐恢复；`active` 时全暖色调 + 粒子效果（如灯笼光晕）

**音频：**
- 打开摊位界面：纸卷展开或布袋声
- 购买成功：铜钱/金属清脆声
- 购买失败（货币不足/容量满）：低沉的否定提示音
- 集市区域环境音：`dormant` 时安静（仅风声）；`recovering` 时有人声低语；`active` 时有热闹的集市背景音（交谈、货物搬运）

## UI Requirements

**摊位界面布局（2D）：**
- 顶部：NPC 名字 + 摊位标签
- 中部：商品列表（图标 + display_name + price + local_identity_tag 一行简述）
- 底部：玩家持有货币显示 + 关闭按钮
- 购买确认：选中商品后弹出确认浮层（商品名、数量、总价、确认/取消）
- 容量满/货币不足时：商品价格灰显 + 红色提示文字

**界面行为：**
- 界面为模态——打开时暂停玩家移动
- 按取消或再次按 Use 关闭界面
- 焦点在商品列表间上下移动（键盘/手柄）

## Acceptance Criteria

每条标准必须是 QA 测试人员可独立验证的 pass/fail。各 AC 仅测试 #14 系统范围内
的行为——跨系统行为在相关系统的 AC 或集成测试中覆盖。

### H.1 摊位交互

- **AC-H.1.1**：玩家步行靠近一个已开启摊位（在 #4 定义的交互范围内），焦点高亮出现在
  摊位上，高亮标签显示 NPC 名字和摊位标签。
- **AC-H.1.2**：玩家按下 Use 键，摊位购买界面打开，显示该摊位当前解锁等级下所有
  可用商品（基础商品 + 独占风味商品）及其价格。
- **AC-H.1.3a**：玩家选择一个商品并确认购买——若货币充足且容量足够，货物进入 #5
  对应资源池，货币按 `total_cost` 扣除。
- **AC-H.1.3b**：玩家选择一个商品并确认购买——若货币不足，显示"货币不足"提示，
  购买被阻止，不扣货币。
- **AC-H.1.3c**：玩家选择一个商品并确认购买——若容量已满，显示"携带空间不足"提示，
  购买被阻止，不扣货币。
- **AC-H.1.4**：玩家走向一个 `closed` 状态的摊位——摊位外观呈现关闭状态（木板封挡），
  无可交互焦点，按下 Use 无反应。

### H.2 修复驱动变化

- **AC-H.2.1**：玩家完成一个修复节点（#13 发出 `repair_completed(node_id)`），
  `node_id` 匹配该定居点某摊位的 `required_node_ids` 时——摊位状态从 `closed` 变为
  `open_basic`，NPC 状态从 `absent` 变为 `idle`，NPC 模型可见并播放 idle 动画，
  商品列表更新为基础商品 + 独占风味商品。
- **AC-H.2.2**：`open_basic` 是 MVP 中摊位可达的最高状态。`open_expanded` 状态在
  状态机中定义但 MVP 中不可达——无第二个修复触发 expanded 转换。快照 schema 和
  状态机支持 future-proof 扩展。
- **AC-H.2.3**：`repair_completed(node_id)` 的 `node_id` 不匹配某摊位的
  `required_node_ids` 时，该摊位状态和 NPC 状态不发生变化。
- **AC-H.2.4**：新游戏开始时，杂货摊（`stall.gh-general`）处于 `open_basic` 状态，
  无任何修复完成——玩家可直接与阿图交互并购买其 2 种基础商品。

### H.3 持久化

- **AC-H.3.1**：存档后读档，所有摊位状态（`closed` / `open_basic`）和 NPC 状态
  （`absent` / `idle`）与存档前一致。`completed_node_ids` 集合在重新加载后完整
  恢复——后续修复信号的 F.2 判定不受影响。
- **AC-H.3.2**：新游戏开始时，琉璃港杂货摊（`stall.gh-general`）为 `open_basic`，
  NPC 阿图为 `idle`；其余 3 个摊位为 `closed`，对应 NPC 为 `absent`。
  `completed_node_ids` 为空集。

### H.4 商品与分类

- **AC-H.4.1**：补给品（基础物资包、修补帆布、透镜维护套件、抗风暴涂层、简易六分仪）
  购买成功后，对应 `good_id` 在 #5 的对应资源池中数量增加 `quantity`，货币按
  `total_cost` 扣除。
- **AC-H.4.2**：情报商品（航线手记）购买成功后，商品进入玩家背包（#5）。
  知识条目的实际解锁由消费该情报物品时触发（消费接口所属系统待 OQ-3 决议后确定）。
- **AC-H.4.3**：所有已解锁的商品在购买后仍保持可购买状态——重复购买同一商品不
  减少其可用性或改变其价格（MVP 无库存耗尽机制）。

### H.5 边界条件

- **AC-H.5.1**：玩家货币恰好等于 `total_cost` 时购买成功，货币归零。购买后界面刷新，
  所有商品因货币不足而灰显。
- **AC-H.5.2**：对同一修复节点重复发出 `repair_completed(node_id)` 信号后——已处于
  `open_basic` 的摊位不重复触发 `closed → open_basic` 转换（`completed_node_ids` 集合
  天然去重），摊位状态和 NPC 状态在重复信号前后保持一致。
- **AC-H.5.3**：所有摊位均已 `open_basic`（MVP 终态）后，新的 `repair_completed`
  信号被安全忽略——不产生错误、不触发额外状态的转换。

### H.6 输入边界与鲁棒性

- **AC-H.6.1**：购买界面中 quantity 输入控件下限为 1——减号按钮在 quantity = 1 时
  灰显。无法输入 0、负值或非整数。
- **AC-H.6.2**：单个 `repair_completed(node_id)` 匹配两个不同摊位的
  `required_node_ids` 时，两个摊位各自独立从 `closed` 转换为 `open_basic`，互不干扰。
- **AC-H.6.3**：玩家在摊位购买界面打开期间走出摊位交互范围——界面保持打开直到手动
  关闭或再次按 Use。界面关闭后，若仍在范围外，按 Use 无反应。
- **AC-H.6.4**：玩家在购买界面打开时收到修复信号——当前购买会话不受影响。界面关闭
  并重新打开后，反映新的解锁等级商品列表。
- **AC-H.6.5**：`repair_completed(node_id)` 的 `node_id` 不属于该定居点任何摊位的
  `required_node_ids`——信号被安全忽略，不产生错误日志。
- **AC-H.6.6**：某商品 price = 0（配置错误）——购买不扣除货币，货物进入资源池。
  日志输出 error 级别警告（价格验证应在数据导入阶段拒绝 price = 0）。
- **AC-H.6.7**：玩家货币不足以支付某商品 `price × 1` 时，该商品在界面中灰显
  （不可选）。
- **AC-H.6.8**：补给品类商品在对应资源池无剩余容量时灰显（不可选）。

## Open Questions

- **OQ-1**：杂货摊（阿图）是否应始终保持 `open_basic` 而非升级到 `open_expanded`？
  若升级，其特色商品应是什么？
- **OQ-2**：未来定居点（天礁弧、雾隐港）是否需要不同的默认开启摊位策略，还是统一
  采用"1 个默认杂货摊"模式？
- **OQ-3**：情报商品（航线手记）解锁的知识条目——由本系统直接写入日志，还是通过 #6
  系统间接解锁？此决定影响 AC-H.4.2 的消费流验证。
- **OQ-4**：Post-MVP 的本地特产（Local Speciality）——是否需要"带到其他定居点出售
  获得更高价格"的跨区域交易机制？此决定影响价格数据模型是否从 `good_id` 级别扩展
  为 `good_id × settlement_id` 二维结构。
