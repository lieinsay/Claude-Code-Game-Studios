# 飞艇模块与船体状态

> **Status**: CONDITIONAL APPROVAL（Round 3 Revision 完成 2026-05-01 — 5 硬阻断项修复：destroyed 波段 η=0 定义、swap_module 货舱占用门控、swap 两阶段语义、swap ACs、全部 AC 类型标签。5 建议项：floor 舍入损失文档化、波段重入 AC、AC-22 拆分、边界 AC、D.4 值集补全。见 review log 完整裁决）
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-01
> **Implements Pillar**: 飞艇是家，不只是载具; 规划先于冒险

## Overview

飞艇模块与船体状态是《云海织航》中飞艇的机械定制层与适航状态层。它定义 MVP 中的两类核心模块——侦察模块与货仓/维修模块——以及它们的安装/卸下/损伤三态、效果计算、载重适航判定和船体完整性模型。在架构上，这个系统是 Hub（拥有模块槽物理位置和交互锚点）与资源系统（拥有货物重量和容量数据）之间的中间件：它从 Hub 接收槽位状态查询和安装/卸下请求，从资源系统接收当前货舱装载质量和容量值，向 Hub 返回模块效果（容积加成、损伤引起的效果打折），向航行系统提供载重适航判定，向存档系统导出模块安装与船体损伤快照。玩家不直接操作"模块系统"——他们通过 Hub 的模块接口交互点安装或检查模块，通过货舱装载货物感受容积变化，通过出航确认看到模块完好度摘要，通过船体维修点和返航后的伤痕看到损伤与修复的痕迹。这个系统的存在意义是让飞艇不是一块整体数值，而是一个可拼装、会受伤、需要维护的机械体——它让 Pillar 3（飞艇是家，不只是载具）拥有机械牙齿，也让 Pillar 1（规划先于冒险）在模块选择和载重取舍中获得具体权重。

## Player Fantasy

飞艇模块与船体状态服务的是一种安静的所有权与风霜的亲密感——它不喧哗，但玩家每次扫过模块接口的状态灯、每次注意到船体上多了一道补丁时，都在确认同一件事：**这是我的船，它和我一起经历过什么，我是这样的人**。

**模块选择即身份表达：** 模块不是装备栏位，是飞艇身上长出来的器官。两个槽位都是开放的——玩家可以选择装两个货仓模块最大化运力、装两个侦察模块获得冗余视野、或一侦察一货仓走平衡路线。双货仓的飞艇侧面多出两块亲手"拼上去"的舱室，每次走进那个加倍的货舱空间，都是在确认：我选择了做运输船长。双侦察的飞艇工程舱里两盏绿灯常亮，航图上比别人多看一段风险标注还不够——就算一个侦察模块在风暴中受损，另一个仍在工作。这不是数值优化，这是玩家在这个世界里选择成为什么样的船长。模块选择不是一次性的出航前填写——它是持续生效的规划：选了侦察，每次航线都多看到风险；选了货仓，每次返航都多带回东西；选了双份，就在那条路上走得更远。选择在沉默中持续回响。

**伤痕是航志，不是失败：** 一次差点没回来的高风险撤离后，船体上多了一道新的裂痕。它不是 HUD 上的红色数字——它是你家墙上的一道伤口。玩家记得那是哪次航行、哪个区域、哪次狼狈的回避。船体不是血条，是写在船身上的航海日志。修理之后，补丁盖住了裂缝但留下了痕迹——船看起来不像新的，像活过的。每一道补丁都是幸存证明：你去过别人不敢去的地方，你回来了，你的船扛住了。Hub 的"船是你的航志"记录你为世界做了什么（修灯塔→海图多一条白线）；模块/船体记录船本身承受了什么（高风险撤离→船体多一道补丁）。两者并行不矛盾——船既记录你修好的世界，也记录你自己受的伤。

**锚定时刻：** 出航前站在工程舱，目光扫过模块接口——绿灯、绿灯。你记得侦察模块是三趟航程前装的，它每次都让你提前看到风暴。重量表指针停在安全区与黄区之间——你选了带更多空间而不是更轻负重。那个指针的位置就是你作为船长的签名。返航后推门进舱——船体上多了一道伤痕。之后去维修点修好它，补丁出现了。船不新了，但更好了。

参考游戏的情感基准：《方舟：生存进化》中第一次驯服恐龙、拥有自己的基地和伙伴的归属感——不是通过宏大规模，而是通过"我亲手选择和建造的东西还在那里"的确认。这里的区别是：方舟的家是固定的，云海织航的家会移动、会受伤、会被修补——每一次归来都是一次"你还在，我也还在"的双向确认。

## Detailed Design

### Core Rules

**动力炉（Power Furnace）：**

1. 动力炉是模块的内置属性——不是独立的可安装物，而是模块自带的推进/浮空装置。有动力炉的模块在安装后为飞艇提供浮空推进力和载重容量；没有动力炉的模块仅提供其功能效果，不贡献载重。
2. MVP 中两个模块均内置动力炉：
   - **侦察模块动力炉**：小型炉，载重贡献 8 单位
   - **货仓模块动力炉**：大型炉，载重贡献 12 单位
3. 动力炉需要能量供应才能运作。MVP 阶段假定动力炉始终有燃料（能量系统将在后续 GDD 中设计，届时将引入燃料消耗和能量管理作为出航约束）。本 GDD 预留接口：`get_furnace_energy_status(furnace_id) → float (0.0–1.0)`，当前始终返回 1.0。
4. 动力炉状态与模块状态绑定——模块 `damaged` 时动力炉出力打折，模块 `empty` 时动力炉不提供载重。
5. 飞艇最大载重 = Σ(各已安装模块的动力炉载重贡献 × 模块效率系数)。
   - 双货仓完好：12 + 12 = **24** 载重单位
   - 双侦察完好：8 + 8 = **16** 载重单位
   - 侦察 + 货仓完好：8 + 12 = **20** 载重单位
   - 侦察 damaged + 货仓完好：8×0.6 + 12 = 4.8 + 12 = **16.8**（向下取整 16）
   - 侦察完好 + 货仓 damaged：8 + 12×0.5 = 8 + 6 = **14**
   - 双模块 damaged：8×0.6 + 12×0.5 = 4.8 + 6 = **10.8**（向下取整 10）
6. 飞行能力判定：至少一个动力炉的载重贡献 > 0 时，飞艇可以出航。所有动力炉载重贡献 = 0（全部 empty 或全部 energy=0）时，飞艇无法起飞——这替代了"船体 integrity=0 则坠毁"的单一判定，改为"动力炉全灭 = 失去飞行能力"。

**模块系统基础规则：**

7. 飞艇拥有两个模块槽位：**槽 A** 和 **槽 B**，均为开放槽位——每个槽位可安装任意类型的模块。MVP 提供两种模块：侦察模块和货仓/维修模块。玩家可自由选择每个槽位安装哪种模块，支持同型双装（两个货仓模块或两个侦察模块）或异型搭配。槽位的物理位置和交互锚点由 Hub 系统（#7）拥有和管理——本系统拥有槽位的效果逻辑、动力炉出力和状态机。
8. 每个槽位有三种状态：`empty`、`installed`、`damaged`。状态由 Hub 在玩家执行安装/卸下操作时通过接口同步给本系统；`damaged` 状态由航行系统（#10）在返航时根据航行事件写入。
9. 模块效果通过 **效率系数（efficiency）** 计算：
   - `empty` → 0（无效果，无载重贡献）
   - `installed` → 1.0（完整效果，完整载重贡献）
   - `damaged`（侦察模块）→ 0.6（效果和载重贡献均打折）
   - `damaged`（货仓模块）→ 0.5（效果和载重贡献均打折）
   - `unchecked`（出航后尚未检查）→ 0.95（效果和载重贡献微降）
10. 模块安装消耗 Hub 资源池中的材料。卸下模块退还 75% 材料（向上取整），damaged 模块卸下不退还材料。
10a. **模块交换（swap_module）**：提供 `swap_module(slot_id, new_module_type)` 操作——一步完成卸下旧模块和安装新模块。操作分两阶段执行：（1）**验证阶段**——检查所有前提条件（槽位非空、材料充足、若旧模块为货仓模块且 `new_module_type` 非货仓模块则 `get_cargo_bay_usage().used_volume` 必须为 0、`consume_for_module()` 对净消耗的预演校验通过）。任何前提条件失败均返回错误且无状态变更。（2）**执行阶段**——按序执行卸下旧模块、发放退款、安装新模块、扣除材料。两阶段保证不会出现"卸下了但装不上"的中间状态。材料净消耗按资源类型逐项计算：各资源的净消耗 = `max(0, 新模块该资源的 install_cost − 旧模块该资源的 refund)`，退还不能跨资源类型抵扣。refund_for_old = 旧模块安装成本的 75%（向上取整，同卸下规则）。若旧模块为 damaged，refund_for_old = 0。若 `new_module_type == current_module_type`（同类型交换），操作被拒绝——提示"模块类型相同，无需更换"，不消耗材料。交换操作消除了配置实验的来回惩罚——玩家可在一次操作中将货仓配置切换为侦察配置，仅支付净差额。
11. 货仓模块提供基础容积加成 +500（每个）。两个货仓模块的容积加成叠加：V_effective = V_base + 500×η_A + 500×η_B。damaged 时该模块的容积加成降为 250（50% 效率）。双货仓完好时 V_effective = 1000。
12. 侦察模块提供航线风险可见度加成——在航行系统的航线图中额外显示一段风险标注。damaged 时可见范围缩减为 60%。同型双装时：任一侦察模块完好即提供完整侦察效果（冗余保护——两个都 damaged 时可见范围缩减为 60%）。MVP 中第二个侦察模块不叠加额外风险可见范围。

13. 模块槽位状态模型包含两层数据：`actual_state`（模块真实物理状态——是否真的受损）和 `visible_state`（玩家看到的状态——返航后为 unchecked 直到检查）。详细规则见"模块槽位状态机"的 actual/visible 字段说明。

**船体完整性规则：**

14. 船体完整性（Hull Integrity）为 0–100 的整数值，初始值为 100。每次出航返航后，根据航行中遭遇的风险事件累计损伤。船体完整性代表船体结构本身的完好程度——与动力炉（飞行能力）是两个独立维度：动力炉决定"能不能飞"，船体完整性决定"飞起来有多危险/多慢"。
15. 船体伤痕（Hull Scars）为 ≥0 的整数值，初始值为 0。每次航行损伤事件（返航结算时 integrity 减少的任意事件）使 `hull_scars += 1`。跨波段伤害链中，每个新进入的波段额外 +1（见规则 17）。伤痕在 MVP 中为纯叙事计数器——不影响机械，但驱动 Hub 船体外观的补丁痕迹显示和 NPC 对话变化（如"你的船经历过不少啊"）。伤痕持久化在存档快照中，无上限。
16. 船体完整性分为四个波段：
    - **intact（76–100）**：结构完好，无负面效果。视觉显示洁净船体。
    - **damaged（26–75）**：可见伤痕/补丁。航速 -10%，燃料消耗 +15%。惩罚在下次出航时生效。
    - **critical（1–25）**：严重结构损伤。航速 -25%，燃料消耗 +30%，模块效率额外 × 0.8（与模块自身效率叠加）。无法执行高风险航线。
    - **destroyed（0）**：结构崩溃，无法出航——即使动力炉正常也无法起飞。必须紧急修复恢复到至少 1 点。
17. 跨波段伤害链：若一次伤害事件跨越多个波段，每个**新进入**的波段使 `hull_scars += 1`（本次伤害事件本身已贡献 +1，见规则 15）。"新进入"指该波段在本次伤害事件前未被占据——若起始 integrity 已处于某波段内，该波段不计入转换。例如：integrity 从 80（intact）一次受到 80 点伤害降至 0，依次进入 damaged、critical、destroyed 三个波段，hull_scars 增加：基础事件 +1 + 进入 damaged +1 + 进入 critical +1 + 进入 destroyed +1 = +4。但 integrity 从 30（已处于 damaged 波段）一次受 35 点伤害降至 0，仅新进入 critical 和 destroyed 两个波段，hull_scars 增加：基础事件 +1 + 进入 critical +1 + 进入 destroyed +1 = +3。最终波段的惩罚在下次出航时生效。中间波段的惩罚不实际生效（伤害是一次性结算事件，无中途出航窗口），但伤痕计数保留全部波段转换记录——船体上的一道深痕可能跨越多个波段，伤痕计数反映了这个"深度"。
18. 船体修复在 Hub 的 Station 10（船体维修点）执行，消耗资源材料。修复量 = 消耗资源的修复值总和。修复后补丁覆盖裂痕但留下视觉痕迹。若 `integrity >= 100`，修复操作被拒绝并提示"船体结构完好"——防止玩家浪费修复材料。
19. 每次修复操作最小恢复 1 点 integrity，最大恢复至 100。单次修复量无上限。

**中探索船体与模块损伤：**

20. `apply_hull_damage(amount: int) → void`：由战斗与威胁处理（#12）在威胁结算期间（硬扛后）调用，以应用中探索船体损伤。扣除 `integrity -= amount`，若跨越波段边界则触发波段转换，并根据规则 15 和规则 17 增加 `hull_scars`。若 `amount` 将 integrity 推至 0 以下，则钳制为 0（destroyed 波段）。发出 `hull_integrity_changed` 信号，若波段变化则随后发出 `hull_band_changed`。前提条件：`amount > 0`。若 `amount <= 0`，则静默无操作返回。本操作由 #12 拥有并调用。
21. `apply_module_damage(slot_id: StringName, damage_type: StringName) → void`：由战斗与威胁处理（#12）在威胁结算期间（硬扛后）调用，以将指定槽位的 `actual_state` 标记为 `damaged`。效率系数相应地设为 0.6（侦察兵）或 0.5（货物）。发出 `actual_state_changed`、`slot_state_changed`、`module_efficiency_changed` 和 `departure_readiness_changed` 信号（遵循规则 21 中定义的顺序）。`damage_type` 是一个传递字符串，用于识别损坏来源（MVP 中为 `"guard_impact"`）；#8 仅存储此值，其含义由 #17（反馈/特效/音频语义）消费。前提条件：`slot_id` 对应于一个已安装的、非空的槽位，且其 `actual_state` 当前未被标记为 `damaged`。若 `slot_id` 无效、为空或其 `actual_state` 已为 `damaged`，则此调用视为无操作（在已受损模块上返回无错误——不造成二次损坏）。当 #12 在调用 `apply_module_damage` 之前无法正确过滤出已受损槽位时，此调用作为防御性安全网。本操作由 #12 拥有并调用。

**出航适航判定：**

20. 出航条件 = `飞艇最大载重 > 0`（至少一个动力炉有出力）且 `船体完整性 > 0` 且 `当前总载重 ≤ 最大载重`。`can_depart()` 返回 `{can: bool, reasons: [StringName]}`——每个不满足的条件对应一条原因文本。Hub 的出航确认界面展示不满足的条件作为阻断原因。

**Hub 同步信号契约：**

21. 模块系统通过以下 Godot 信号向 Hub 和 UI 系统广播状态变更（emit-after-mutation）：
    - `slot_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName)` — 模块槽位 visible_state 变更后触发
    - `actual_state_changed(slot_id: StringName, old_state: StringName, new_state: StringName)` — 模块槽位 actual_state 变更后触发（由航行系统写入时触发）
    - `hull_integrity_changed(old_value: int, new_value: int)` — 船体完整性值变更后触发
    - `hull_band_changed(old_band: StringName, new_band: StringName)` — 船体波段变更后触发
    - `module_efficiency_changed(slot_id: StringName, old_eff: float, new_eff: float)` — 模块效率系数（η_final）变更后触发
    - `departure_readiness_changed(can_depart: bool, reasons: Array[StringName])` — 适航状态变更后触发
    信号在状态变更完全完成后触发，回调中可安全调用查询方法（`get_*`），但不得调用变更方法（重入防护返回 `ERR_BUSY`）。
    
    信号发射顺序约定：`actual_state_changed` → `slot_state_changed` → `module_efficiency_changed` → `departure_readiness_changed`。船体相关信号（`hull_integrity_changed` 先于 `hull_band_changed`）独立于模块信号链。`departure_readiness_changed` 仅在 `can` 或 `reasons` 与上次缓存值实际不同时才触发（避免无变化信号噪音）。

**模块获取：**

22. 货仓模块：游戏开始时预装在槽 B（玩家后续可将其卸下并重新安装至任意槽位）。
23. 侦察模块：通过首次探索任务完成后作为奖励获得——NPC 在玩家完成第一次成功返航后交付。获得后玩家可在 Hub 工程舱的任意空槽位安装。

### States and Transitions

**模块状态双字段模型：**

每个已安装模块槽位内部维护两个字段：
- `actual_state`：模块的真实物理状态（`installed` 或 `damaged`）——由航行系统在返航时写入。`empty` 槽位的 `actual_state` 为 `empty`。
- `visible_state`：玩家在 Hub 中看到的状态（`installed`、`damaged`、`unchecked`）——返航后自动置为 `unchecked`，玩家检查后同步为 `actual_state`，维修后置为 `installed`。

效率系数和动力炉载重贡献基于 `visible_state` 计算（玩家只能基于可见信息做决策）。

**模块槽位状态机（visible_state，含动力炉）：**

| 当前 visible_state | 触发事件 | 目标 visible_state | 模块效果 | 动力炉载重贡献 | actual_state 影响 |
|---------|---------|---------|---------|-------------|-------------------|
| `empty` | 玩家安装模块 | `installed` | 0→1.0 | 0→满额 | 置为 `installed` |
| `installed` | 航行中模块受损 | `unchecked` | 1.0→0.95 | 满额→满额×0.95 | 置为 `damaged`（航行系统写入） |
| `installed` | 航行中模块未受损 | `unchecked` | 1.0→0.95 | 满额→满额×0.95 | 保持 `installed` |
| `installed` | 玩家卸下模块 | `empty` | 1.0→0 | 满额→0 | 置为 `empty` |
| `unchecked` | 玩家检查模块（actual=installed） | `installed` | 0.95→1.0 | 恢复满额 | 不变（已为 `installed`） |
| `unchecked` | 玩家检查模块（actual=damaged） | `damaged` | 0.95→对应值 | 满额×0.95→对应值×efficiency | 不变（已为 `damaged`） |
| `unchecked` | 玩家直接维修（不检查） | `installed` | 0.95→1.0 | 恢复满额 | 置为 `installed`（维修修复了 actual 状态）。消耗全额维修材料（`repair_kit × 2`），无论 actual_state 如何——不检查即付全价 |
| `unchecked` | 玩家卸下模块 | `empty` | 0.95→0 | 满额×0.95→0 | 置为 `empty`——卸下 unchecked 模块不退还材料（同 damaged 卸下规则），因为玩家未确认模块完好 |
| `damaged` | 玩家维修模块 | `installed` | 对应值→1.0 | 恢复满额 | 置为 `installed` |
| `damaged` | 玩家卸下模块 | `empty` | 对应值→0 | 剩余→0 | 置为 `empty` |
| `damaged` | 航行中再次受损 | `damaged` | 维持对应值（不提升至 0.95） | 维持对应值×efficiency | 保持 `damaged`（航行系统可追加受损标记，伤痕计数增加，但效率不恢复） |

> **返航后流程修正**：航行系统在返航时写入 `actual_state`（installed 或 damaged）。然后本系统处理：
> - 出航前 `actual_state = installed` 的模块：`visible_state` 统一置为 `unchecked`（效率 0.95）。
> - 出航前 `actual_state = damaged` 的模块：`visible_state` 维持 `damaged`（效率不恢复）。受损模块不会因出航而"变好"——玩家已知道它坏了，不需要 unchecked 来制造不确定性。
> 
> 出航前检查流程见下文。

**船体完整性状态机：**

| 当前波段 | 触发事件 | 目标波段 | 附加效果 |
|---------|---------|---------|---------|
| `intact` | 航行损伤使 integrity ≤ 75 | `damaged` | 施加 damaged 航行惩罚；视觉伤痕 |
| `damaged` | 航行损伤使 integrity ≤ 25 | `critical` | 施加 critical 惩罚；高风险航线封锁 |
| `damaged` | 修复使 integrity ≥ 76 | `intact` | 移除惩罚；保留补丁痕迹 |
| `critical` | 航行损伤使 integrity = 0 | `destroyed` | 无法出航（结构崩溃） |
| `critical` | 修复使 integrity ≥ 26 | `damaged` | 降级至 damaged 惩罚 |
| `destroyed` | 紧急修复使 integrity ≥ 1 | `critical` | 恢复出航能力（结构勉强支撑） |

**飞行能力状态机（动力炉维度）：**

| 条件 | 飞行能力 | 效果 |
|------|---------|------|
| 至少 1 个动力炉载重贡献 > 0 | `flyable` | 可出航，载重 = Σ 各动力炉贡献 |
| 全部动力炉载重贡献 = 0 | `grounded` | 无法出航——无浮空力 |

> 飞行能力与船体完整性是 AND 关系——两者同时满足才能出航。动力炉全灭或 integrity=0 各自独立阻断出航。

**出航前后检查流程：**

```
出航前: 
  1. Hub 出航确认 → 本系统被查询
  2. 返回: 飞行能力(动力炉状态+总载重) + 船体完整性波段+惩罚 + 各模块状态与效率
  3. Hub 在确认界面展示摘要

返航后:
  1. 航行系统写入航行事件（损伤量、模块受损标记）
  2. 本系统处理损伤: integrity -= 损伤量; 模块受损标记 → 对应槽位 actual_state → damaged
  3. 模块 visible_state 更新：出航前 actual=installed 的模块 → `unchecked`（η=0.95）；出航前 actual=damaged 的模块 → 维持 `damaged`（η 保持对应值，不提升）
  4. 船体新伤痕在 Hub 船体上视觉显示
```

### Interactions with Other Systems

**上游依赖（本系统消费的数据）：**

| 系统 | 提供的数据 | 接口方向 |
|------|----------|---------|
| 飞艇家园 Hub (#7) | 槽位物理位置、完整槽位状态、交互锚点触发 | Hub → 模块系统 |
| 资源货物与容量 (#5) | `get_total_loaded_mass()`、`cargo_module_volume_bonus`、`consume_for_module(resource_costs)`、`mass_class` 映射表 | 资源系统 → 模块系统 |
| 本地存档 (#3) | 存档/读档触发、`progress.airship` 快照读写 | 存档系统 ↔ 模块系统 |
| *能量系统（待设计）* | `get_furnace_energy_status()` — 当前 stub 返回 1.0 | *provisional* → 模块系统 |

**下游消费者（消费本系统数据）：**

| 系统 | 消费的数据 | 接口方向 |
|------|----------|---------|
| 航行与路线风险 (#10) | 侦察模块效率、船体完整性波段+惩罚、适航判定、动力炉载重上限 | 模块系统 → 航行系统 |
| 探索搜撤 (#11) | 高风险撤离事件可能导致的额外船体损伤量 | 模块系统 → 探索系统 |
| 战斗威胁 (#12) | `apply_hull_damage(amount)`、`apply_module_damage(slot_id, damage_type)`、`get_installed_slots()` — 中探索威胁结算期间由 #12 调用 | 模块系统 → 战斗系统 |
| UI HUD (#16) | 模块状态摘要、船体完整性显示、动力炉状态、载重/适航指示 | 模块系统 → UI |

**数据流契约：**

- **槽位状态所有权**：Hub 拥有物理槽位存在性和交互触发；模块系统拥有槽位效果计算、动力炉出力和状态机逻辑。
- **载重适航判定所有权**：资源系统提供数据，模块系统综合动力炉状态 + 模块状态 + 船体完整性 + 载重数据做出航判定（`can_depart() → {can: bool, reasons: [StringName]}`）。
- **动力炉能量接口（provisional）**：`get_furnace_energy_status(furnace_id: StringName) → float`。当前始终返回 1.0。未来能量系统实现后，返回值将根据实际燃料/能量水平变化。
- **存档快照**：导出纯 Dict 的 `progress.airship`：`{modules: {slot_a: {visible_state, actual_state, efficiency, module_type}, slot_b: {visible_state, actual_state, efficiency, module_type}}, hull_integrity: int, hull_scars: int}`

## Formulas

### D.1 最大载重

**公式：** `M_max = ⌊Σ (R_furnace(i) × η_final(i)) for all installed modules⌋`

| 变量 | 定义 | 值域 |
|------|------|------|
| `M_max` | 飞艇最大载重（整数） | 0–24 |
| `R_furnace(i)` | 模块 i 的动力炉载重额定值 | scout=8, cargo=12 |
| `η_final(i)` | 模块 i 的波段修正后有效效率系数（D.2b） | 0.0–1.0 |

> **注意**：`η_final` 已包含船体波段修正（D.2b）。在 critical 波段下，M_max 会额外降低——例如双货仓 installed 在 critical 波段下：⌊12×0.8 + 12×0.8⌋ = 19（而非 intact 波段下的 24）。

**示例计算：**

| 场景 | 计算 | M_max |
|------|------|-------|
| 双货仓完好 | ⌊12×1.0 + 12×1.0⌋ | **24** |
| 侦察+货仓完好 | ⌊8×1.0 + 12×1.0⌋ | **20** |
| 双侦察完好 | ⌊8×1.0 + 8×1.0⌋ | **16** |
| 货仓 damaged, 侦察完好 | ⌊8×1.0 + 12×0.5⌋ = ⌊8 + 6⌋ | **14** |
| 侦察 damaged, 货仓完好 | ⌊8×0.6 + 12×1.0⌋ = ⌊4.8 + 12⌋ | **16** |
| 双模块 damaged | ⌊8×0.6 + 12×0.5⌋ = ⌊4.8 + 6⌋ | **10** |
| 仅单侦察安装 | ⌊8×1.0⌋ | **8** |
| 仅单货仓安装 | ⌊12×1.0⌋ | **12** |

### D.2 模块效率系数

**公式：** `η = efficiency_table[module_type][state]`

| module_type\state | `empty` | `unchecked` | `installed` | `damaged` |
|-------------------|---------|-------------|-------------|-----------|
| `scout` | 0 | 0.95 | 1.0 | 0.6 |
| `cargo` | 0 | 0.95 | 1.0 | 0.5 |

`unchecked` 状态在返航后自动施加，玩家在工程舱检查模块后转为 `installed` 或 `damaged`（取决于航行中是否受损）。`unchecked` 的 0.95 值确保玩家有动机检查模块——不检查也能飞，但略打折扣。

### D.2b 有效效率（波段修正后）

**公式：** `η_final = η_visible × η_hull_band`

| 变量 | 定义 | 值域 |
|------|------|------|
| `η_visible` | 模块 visible_state 对应的效率系数（D.2） | 0, 0.5, 0.6, 0.95, 1.0 |
| `η_hull_band` | 当前船体波段的模块效率修正（D.3） | intact/damaged=1.0, critical=0.8, destroyed=0 |

D.1（最大载重）和 D.4（有效货舱容积）使用 `η_final` 而非原始 `η_visible`。这意味着在 critical 波段下，所有模块的效率和载重贡献额外打八折。

**示例：**
- 侦察模块 installed（η_visible=1.0） + critical 波段（η_hull_band=0.8）→ η_final = 1.0 × 0.8 = **0.8**
- 货仓模块 damaged（η_visible=0.5） + critical 波段（η_hull_band=0.8）→ η_final = 0.5 × 0.8 = **0.4**
- 侦察模块 damaged（η_visible=0.6） + critical 波段（η_hull_band=0.8）→ η_final = 0.6 × 0.8 = **0.48**

极端调参边界（scout damaged 0.3 × critical band 0.6 = 0.18）：若调参至安全范围下限，有效效率可能低至 0.18——校准时应确保此极端值仍产生正数载重贡献。

### D.3 船体完整性波段判定

**公式：** `Band = band_table(integrity)`

| integrity 范围 | 波段 | 航速修正 | 燃料消耗修正 | 模块效率额外修正 | 额外限制 |
|---------------|------|---------|------------|----------------|---------|
| 76–100 | `intact` | 1.0 | 1.0 | 1.0 | 无 |
| 26–75 | `damaged` | 0.9 | 1.15 | 1.0 | 无 |
| 1–25 | `critical` | 0.75 | 1.3 | 0.8 | 高风险航线封锁 |
| 0 | `destroyed` | — | — | 0 | 无法出航 |

**示例：**

- integrity=50 → damaged 波段：航速 ×0.9，燃料消耗 ×1.15
- integrity=15 → critical 波段：航速 ×0.75，燃料消耗 ×1.3，模块效率额外 ×0.8。若此时侦察模块 installed，实际侦察效率 = 1.0 × 0.8 = 0.8（先算模块自身效率，再乘以波段修正）

### D.4 有效货舱容积

**公式：** `V_effective = V_base + V_bonus × η_final_A + V_bonus × η_final_B`

| 变量 | 定义 | 值 |
|------|------|-----|
| `V_effective` | 有效货舱容积 | 0–1000（双货仓完好 + intact 波段时） |
| `V_base` | 货舱基础容积（来自资源系统 #5） | 0 |
| `V_bonus` | 单个货仓模块容积加成 | 500 |
| `η_final_A` | 槽 A 货仓模块的波段修正后有效效率（D.2b，若槽 A 非货仓模块则 = 0） | 0/0.4/0.48/0.5/0.6/0.76/0.8/0.95/1.0 |
| `η_final_B` | 槽 B 货仓模块的波段修正后有效效率（D.2b，若槽 B 非货仓模块则 = 0） | 0/0.4/0.48/0.5/0.6/0.76/0.8/0.95/1.0 |

> **注意**：`η_final` 值域扩展了以下 critical 波段组合值：0.4（cargo damaged + critical: 0.5×0.8）、0.48（scout damaged + critical: 0.6×0.8）、0.76（unchecked + critical: 0.95×0.8）、0.8（installed + critical: 1.0×0.8）。在 critical 波段下，双货仓 installed 的 V_effective = 0 + 500×0.8 + 500×0.8 = 800（而非 intact 波段下的 1000）。

**示例：**

- 双货仓 installed：V_effective = 0 + 500×1.0 + 500×1.0 = **1000**
- 侦察+货仓 installed：V_effective = 0 + 0 + 500×1.0 = **500**
- 双侦察：V_effective = 0 + 0 + 0 = **0**
- 双货仓，一个 damaged：V_effective = 0 + 500×1.0 + 500×0.5 = **750**
- 双货仓 empty：V_effective = 0 + 0 + 0 = **0**

### D.5 适航判定

**公式：** `can_depart() = (M_max > 0) AND (integrity > 0) AND (M_loaded ≤ M_max)`

| 变量 | 定义 | 来源 |
|------|------|------|
| `M_max` | 飞艇最大载重（公式 D.1） | 本系统 |
| `integrity` | 船体完整性值 | 本系统 |
| `M_loaded` | 当前总载重质量 | 资源系统 #5 `get_total_loaded_mass()` |

返回值：`{can: bool, reasons: [StringName]}` ——每个不满足的条件对应一条原因标识，如 `"overloaded"`、`"no_furnace"`、`"hull_destroyed"`。

### D.6 维修量计算

**公式：** `R_total = Σ repair_value(m) for m in consumed_materials`

**公式：** `integrity_new = min(100, integrity_old + R_total)`

| 变量 | 定义 | 值域 |
|------|------|------|
| `R_total` | 总修复量 | ≥ 0 |
| `repair_value(m)` | 材料 m 的修复值 | 由资源系统 #5 定义 |
| `integrity_new` | 修复后完整性 | 1–100 |

约束：若 `R_total < 1`，修复操作拒绝执行——保证每次修复至少恢复 1 点 integrity（符合规则 19 的"最小恢复 1 点"约束）。此约束在所有 integrity 值下生效，防止玩家在任何 integrity 值下消耗零价值材料。

## Edge Cases

**EC-01 — 货仓模块效率下降导致容积不足：** 当货仓模块从 installed→damaged（V_effective 从 500→250），或卸下（→0），导致当前已装载货物的体积超过新的有效容积时，超出部分货物变为 `trapped` 状态——货物仍属于玩家、在 UI 中可见但灰显，无法取出、使用或出售。直到 V_effective 恢复到 ≥ 货物总体积（模块修复或重新安装），trapped 货物自动恢复可访问。货物不会因模块 damage 而丢失——只有模块 destroyed 时才触发丢失（EC-05 in resources-goods-capacity.md）。

**EC-02 — 出航前超载阻断：** 若 `M_loaded > M_max`（例如货仓模块在本次航行中从 installed→damaged，导致 M_max 从 20→14，但之前装载的货物已达 18 载重），Hub 出航确认界面显示超载原因（"当前载重 18 / 最大载重 14 —— 货仓模块受损，载重上限降低"），不允许出航。玩家必须卸货或先修复货仓模块。

**EC-03 — 双动力炉全灭：** 若玩家卸下两个模块（M_max=0），或两个模块均 energy=0（未来能量系统），飞艇无法出航。UI 显示"无可用动力炉——请至少安装一个模块"。注意：即使 M_max=0，船体 integrity 可能仍然是 100——动力炉和船体是独立维度。

**EC-04 — unchecked 状态下出航：** 玩家在模块处于 `unchecked` 状态（效率 0.95）时出航，模块在整个航程中维持 0.95 效率。航程中不会"突然发现模块是坏的"——unchecked 是已知的风险折价，已体现在 0.95 的打折中。返航后模块保持 unchecked 直到玩家检查。此设计避免中航程状态突变带来的复杂性和玩家挫败感。

**EC-05 — unchecked + 直接维修 vs 先检查后维修：** 玩家不先检查模块就直接维修。维修操作消耗正常维修材料（`slot_repair_cost_base = repair_kit × 2`），无论 actual_state 是 installed 还是 damaged——直接维修是"付钱买确定"，不获取 actual_state 信息但跳过效率骤降风险。维修后 `visible_state` 和 `actual_state` 均置为 `installed`（η=1.0）。先检查（免费，0 材料）则揭示 actual_state——若 actual=installed 则 η 恢复 1.0 且无需维修；若 actual=damaged 则 η 降至对应值（0.5/0.6），然后玩家可选择花费维修材料修复。两条路径各有取舍：检查路径可能省钱（如果模块完好），但若模块受损则需经历效率骤降；直接维修路径成本确定，但可能为完好模块白花钱。

**EC-06 — 船体 integrity 超过 0 的过度损伤：** 航行损伤量可能使 integrity 计算值 < 0（例如 integrity=5，损伤量=15）。实际计算为 `integrity = max(0, 5 - 15) = 0`，不会出现负值。额外的 10 点损伤不累积——最小值为 0。

**EC-07 — 维修溢出：** 修复材料提供的修复量可能使 integrity 超过 100。公式 `min(100, integrity_old + R_total)` 处理溢出——多余修复值不保留、不退款。UI 应在修复确认前显示"将恢复至 100/100"以提示溢出。

**EC-08 — 卸下最后一个动力炉：** 玩家可以卸下飞艇上唯一的已安装模块（例如只剩下货仓模块时将其卸下）。这会导致 M_max=0 且无法出航，但操作本身不被阻止——系统只阻止出航，不阻止卸下。若将来有"在港口长期停泊、不需要飞行能力"的场景，此状态是合法的。

**EC-09 — 侦察模块 damaged 时已显示的航线风险：** 玩家在侦察模块完好时查看了航线图，看到了完整的风险标注。然后模块在航程中变为 damaged——已显示的标注不会消失（玩家已经知道了），但新的风险不再被提前标注。这由航行系统 #10 处理具体行为，本系统仅提供当前侦察效率值。

**EC-10 — 模块槽位空置时 Hub 的交互表现：** 槽位 empty 时，Hub 的模块接口交互点应显示"空槽位"状态（视觉上可能是一个空的安装位/支架），而非完全消失。玩家需要能看到"这里可以装东西"才能触发安装操作。具体的交互 UI 由 Hub 系统 #7 和 UI 系统 #16 定义。

**EC-11 — 起始状态一致性：** 新游戏开始时，货仓模块预装在槽 B（installed, η=1.0），槽 A 为 empty。船体 integrity=100（intact 波段），hull_scars=0。最大载重 M_max=12（仅货仓动力炉）。玩家在获得侦察模块之前，飞艇以单炉状态运行——这创造了通过首次探索获得侦察模块的动机弧线。玩家可在获得侦察模块后选择将其装入槽 A（形成侦察+货仓平衡配置）、或卸下货仓装入槽 A、将侦察装入槽 B（任意组合均有效）。

**EC-12 — 跨波段伤害结算：** 若一次航行损伤使 integrity 从 30（已处于 damaged 波段 26-75）降至 0（damage=35, clamp 至 0），波段依次经过 damaged(30)→critical(进入 1-25)→destroyed(进入 0)。hull_scars 增加：基础伤害事件 +1，进入 critical +1，进入 destroyed +1（共计 +3）。注意：integrity=30 已在 damaged 波段中，"进入 damaged"不计数（非新进入）。最终波段为 destroyed——无法出航。中间波段 critical 的惩罚（航速/燃料）在本次结算中不生效（伤害是一次性结算事件），但伤痕计数保留了完整的波段转换记录。对比：若 integrity 从 80（intact）一次降至 0，则 hull_scars = +4（基础 +1 + 进入 damaged +1 + 进入 critical +1 + 进入 destroyed +1）。

**EC-13 — 双侦察模块同时受损：** 若两个槽位均安装侦察模块，其中一个在航行中受损（actual_state→damaged），另一个保持 installed。由于任一侦察完好即提供完整侦察效果（规则 12），航线风险可见度不受影响。两个侦察均 damaged 时，可见范围缩减为 60%（取最差状态）。

**EC-14 — trapped 货物通知：** 当货仓模块效率下降（η_final 降低）导致 V_effective 减小时，本系统主动调用资源系统 #5 的 `update_cargo_bay_effective_volume(new_volume: int)` 接口。资源系统内部检测 total_loaded_volume > new_volume 时触发 trapped 货物标记（见 EC-01）。同时本系统发射 `module_efficiency_changed` 信号供 UI 系统检测并显示通知横幅。通知路径：模块效率变更 → `module_efficiency_changed` 信号 → UI 系统查询 V_effective → 发现 trapped 条件 → 显示通知。具体通知格式由 UI 系统 #16 定义。

> **注意**：原设计依赖 `mass_changed` 信号间接通知 trapped 条件，但模块效率变更不会改变货舱质量——`mass_changed` 不会触发。现改为模块系统主动调用资源系统的 `update_cargo_bay_effective_volume()` 接口。资源系统 #5 需在 Interactions 表中新增此接口。

## Dependencies

### 上游依赖（本系统依赖的系统）

| # | 系统 | 依赖内容 | 状态 |
|---|------|---------|------|
| 7 | 飞艇家园 Hub | 模块槽位物理位置、槽位状态同步、交互锚点触发、Station 10 船体维修点 | GDD 已批准 |
| 5 | 资源货物与容量 | `get_total_loaded_mass()`、`cargo_module_volume_bonus`(500)、`consume_for_module(resource_costs)`、`mass_class` 映射表、`repair_value(m)` 材料修复值 | GDD 已批准 |
| 3 | 本地存档与世界状态持久化 | `progress.airship` 快照的写入触发和恢复接口 | GDD 已批准 |
| — | 能量系统（名称待定） | `get_furnace_energy_status(furnace_id) → float` — 当前 stub 返回 1.0 | **尚未设计** |

### 下游消费者（依赖本系统的系统）

| # | 系统 | 消费内容 | 状态 |
|---|------|---------|------|
| 10 | 航行与路线风险 | 侦察模块效率（航线风险可见范围）、船体完整性波段 + 惩罚系数、`can_depart()` 适航判定、M_max 载重上限 | **尚未设计** |
| 11 | 探索搜撤 | 高风险撤离事件可能触发的额外船体损伤量、模块受损标记 | **尚未设计** |
| 12 | 战斗威胁 | 战斗命中可能触发的模块受损标记和船体损伤量 | **尚未设计** |
| 16 | UI HUD | 模块状态摘要（出航确认界面）、船体完整性显示（常驻 HUD）、动力炉状态、载重/适航指示 | **尚未设计** |

### 双向依赖说明

- **Hub (#7) ↔ 模块系统**：Hub 拥有槽位物理存在性，模块系统拥有槽位的效果逻辑。双方通过信号同步状态变更。
- **资源系统 (#5) → 模块系统 → 航行系统 (#10)**：资源提供数据，模块系统执行适航判定，航行系统消费判定结果。适航判定是模块系统的核心权限——只有它能综合动力炉 + 模块状态 + 船体 + 载重四个维度。
- **能量系统（provisional）**：动力炉设计隐含了对能量系统的依赖。当前 stub 保证 MVP 可运行。能量系统设计时必须读取本 GDD 中的 `get_furnace_energy_status()` 接口契约。

## Tuning Knobs

| 参数 | 当前值 | 安全范围 | 影响方面 |
|------|--------|---------|---------|
| `furnace_rating_scout` | 8 | 5–15 | 侦察模块动力炉载重贡献。双侦察时 M_max=16；提高 = 侦察配置的载重能力接近货仓配置 |
| `furnace_rating_cargo` | 12 | 8–20 | 货仓模块动力炉载重贡献。双货仓时 M_max=24（MVP 载重上限）。调整时注意与货物重量（mass_class: light=1, medium=3, heavy=6）的比例关系 |
| `efficiency_scout_damaged` | 0.6 | 0.3–0.8 | 侦察模块 damaged 时的效果比率。提高 = 战损惩罚更宽容；降低 = 更强调维修紧迫性 |
| `efficiency_cargo_damaged` | 0.5 | 0.3–0.75 | 货仓模块 damaged 时的效果比率。直接影响 damaged 时 V_effective 和 M_max 的下降幅度 |
| `efficiency_unchecked` | 0.95 | 0.85–1.0 | 未检查模块的效率。提高 = 检查动力减弱；设为 1.0 = 移除检查机制的意义 |
| `hull_band_intact_min` | 76 | 70–85 | intact→damaged 的阈值。提高 = 更早出现伤痕和惩罚；降低 = 更宽容的损伤容限 |
| `hull_band_damaged_min` | 26 | 15–35 | damaged→critical 的阈值。提高 = 更早进入严重惩罚；降低 = 更长的 damaged 缓冲区间 |
| `hull_speed_damaged` | 0.9 | 0.8–0.95 | damaged 波段航速倍率。更接近 1.0 = 轻伤几乎不影响飞行节奏 |
| `hull_fuel_damaged` | 1.15 | 1.05–1.3 | damaged 波段燃料消耗倍率。更高 = 受伤飞行更贵，增强修理动机 |
| `hull_speed_critical` | 0.75 | 0.5–0.85 | critical 波段航速倍率。显著惩罚以制造紧张感 |
| `hull_fuel_critical` | 1.3 | 1.2–1.5 | critical 波段燃料消耗倍率 |
| `hull_efficiency_critical` | 0.8 | 0.6–0.9 | critical 波段额外模块效率倍率（与模块自身效率叠加）。更低 = critical 状态严重影响所有模块功能 |
| `uninstall_refund_ratio` | 0.75 | 0.5–1.0 | 卸下完好模块时的材料退还比例（向上取整）。0.75 = 安装是有成本的决策但实验可行；1.0 = 无成本随意更换；0.5 = 强锁定 |
| `hull_integrity_initial` | 100 | 80–100 | 新游戏起始船体完整性。影响玩家在首次出航前的船体状态 |
| `hull_integrity_max` | 100 | 100 | 船体完整性上限。固定 100——修改会连锁影响所有波段阈值 |
| `hull_scars_initial` | 0 | 0 | 新游戏起始伤痕计数。固定为 0 |
| `floor_rounding_loss_max` | 21.9% | —（只读指标） | D.1 中 `floor()` 取整导致的最坏舍入损失（单 damaged 货仓 + critical 波段：⌊12×0.4⌋=4，精确值为 4.8，损失 0.8/4.8=16.67%；最坏为 21.88% ≈ 21.9%）。`floor()` 选择是保守设计——飞艇略微低估载重而非高估。调参时若损失超 25%，考虑将 R_furnace 值扩大 10 倍或改用 `round()` |

### 安装与修复经济参数

以下参数由本 GDD 定义推荐值，资源系统 #5 在实现时确认或覆盖。所有数值为 MVP 占位基准——首次 playtest 后校准。

| 参数 | 推荐值 | 安全范围 | 影响方面 |
|------|--------|---------|---------|
| `scout_install_cost` | `basic_supply` × 5 + `repair_kit` × 2 | basic: 3–10, repair: 1–4 | 安装侦察模块的材料消耗。过低→安装无沉没成本；过高→首探后 NPC 赠送模块但玩家装不起 |
| `cargo_install_cost` | `basic_supply` × 3 + `repair_kit` × 3 | basic: 2–8, repair: 2–5 | 安装货仓模块的材料消耗。开局预装的货仓模块免除此成本 |
| `slot_repair_cost_base` | `repair_kit` × 2 | repair: 1–4 | 维修受损模块的基础材料消耗。unchecked+直接维修消耗此全额（无论 actual_state）。先检查则免费——若 actual=installed 则恢复 η 无需维修；若 actual=damaged 则花费此值修复 |
| `hull_repair_value_per_repair_kit` | 5 integrity | 3–10 | 单个 `repair_kit` 的船体修复值。当前值意味从 0→100 需 20 个 repair_kit——支持细粒度维修（如 95→100 仅需 1 个 kit），服务 Pillar 3"每次归来都修补"的幻想 |

> **注意**：`cargo_module_volume_bonus`（500）的 tuning 归属资源系统 #5，不在本系统中重复定义。本系统通过效率系数 η_cargo 间接影响有效容积。

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

**模块安装与卸下：**

- [ ] **AC-01**（Logic）: 在任意空槽位上执行安装操作，消耗指定材料后，模块状态变为 `installed`（visible_state 和 actual_state 均为 installed）。若安装货仓模块，M_max 增加 12，V_effective 增加 500（该槽位贡献）；若安装侦察模块，M_max 增加 8。
- [ ] **AC-02a**（Logic）: 在 installed 状态的货仓模块上执行卸下：模块状态变为 `empty`，M_max 减少 12，该槽位的 V_effective 贡献归零，玩家获得安装材料的 75% 退还（向上取整）。例如安装成本 basic×3 + repair_kit×3 时，退还 basic×3 + repair_kit×3（ceil(3×0.75)=3, ceil(3×0.75)=3）。
- [ ] **AC-02b**（Logic）: 在 installed 状态的侦察模块上执行卸下：模块状态变为 `empty`，M_max 减少 8，无容积变化，玩家获得安装材料的 75% 退还（向上取整）。例如安装成本 basic×5 + repair_kit×2 时，退还 basic×4 + repair_kit×2（ceil(5×0.75)=4, ceil(2×0.75)=2）。
- [ ] **AC-03a**（Logic）: 在 damaged 状态的模块上执行卸下操作，模块状态变为 `empty`，玩家不获得任何材料退还。
- [ ] **AC-03b**（Logic）: 在 unchecked 状态的模块上执行卸下操作（不先检查），模块状态变为 `empty`，玩家不获得任何材料退还。卸下提示应区分"模块已受损——卸下不退还材料"和"模块尚未检查——卸下不退还材料，建议先检查"。
- [ ] **AC-04a**（Logic）: Empty 槽位的 `is_interactable` 属性为 `true`——可以被 Use 交互聚焦。
- [ ] **AC-04b**（UI）: Empty 槽位在 Hub 中显示为可见的"空安装位"交互点，而非完全隐形。[Visual/Feel — 截图+签字验证]
- [ ] **AC-04c**（Logic）: 对已安装模块的已占用槽位执行安装操作：操作被拒绝并返回 `ERR_SLOT_OCCUPIED`，不消耗材料，不改变模块状态。
- [ ] **AC-04d**（Logic）: 对空槽位执行卸载操作：操作被拒绝并返回 `ERR_SLOT_EMPTY`，不授予材料，槽位保持 empty。

**模块交换（swap_module）：**

- [ ] **AC-37**（Logic）: 在槽 A 安装侦察模块（installed）时，调用 `swap_module("slot_a", "cargo")` 将侦察模块替换为货仓模块：侦察模块被卸下（退还 basic×4 + repair_kit×2），货仓模块被安装（消耗 basic×3 + repair_kit×3），净消耗按资源类型逐项计算（basic: 0, repair: 1），槽 A 最终 visible_state=installed、module_type=cargo、M_max 增加 4（12−8），V_effective 增加 500（该槽位贡献）。操作不经过中间 empty 状态。
- [ ] **AC-38**（Logic）: 调用 `swap_module("slot_a", "cargo")` 但仓库中 repair_kit 不足（净消耗 repair×1 而仅有 0）：操作在验证阶段失败并返回 `ERR_INSUFFICIENT_RESOURCES`，槽 A 保留原侦察模块状态不变，不消耗任何材料，M_max 和 V_effective 不变。
- [ ] **AC-39**（Logic）: 在槽 B 安装货仓模块（installed）且货舱中有货物（used_volume > 0）时，调用 `swap_module("slot_b", "scout")` 将货仓模块替换为侦察模块：操作在验证阶段被拒绝，返回 `ERR_CARGO_BAY_NOT_EMPTY`，槽 B 保留货仓模块，货物不受影响。
- [ ] **AC-40**（Logic）: 在槽 A 安装 damaged 货仓模块时，调用 `swap_module("slot_a", "scout")`：refund_for_old = 0（damaged 无退款），净消耗为侦察模块完整安装成本（basic×5 + repair_kit×2），操作成功后槽 A visible_state=installed、module_type=scout，M_max 减少 4（12→8），V_effective 减少 500（该槽位货仓贡献归零）。
- [ ] **AC-41**（Logic）: 调用 `swap_module("slot_a", "scout")` 但槽 A 当前已安装侦察模块（installed —— 同类型交换）：操作被拒绝并提示"模块类型相同，无需更换"，不消耗材料，槽 A 状态不变。
- [ ] **AC-42**（Logic）: 调用 `swap_module("slot_a", "cargo")` 但槽 A 当前为 empty：操作被拒绝并返回错误（无可卸载模块），不消耗材料，槽 A 仍为 empty。

**模块状态与效率：**

- [ ] **AC-05**（Logic）: 侦察模块 installed 时效率 = 1.0；damaged 时效率 = 0.6。货仓模块 installed 时效率 = 1.0；damaged 时效率 = 0.5。
- [ ] **AC-06**（Logic）: 货仓模块 damaged 时，该槽位的 V_effective 贡献从 500 降为 250，M_max 中的货仓贡献从 12 降为 6。若双货仓配置中仅一个 damaged，V_effective = 500×1.0 + 500×0.5 = 750（而非 1000）。
- [ ] **AC-07a**（Logic）: 返航后，出航前 `actual_state = installed` 的模块：`actual_state` 由航行系统写入（installed 或 damaged），`visible_state` 自动变为 `unchecked`，效率 = 0.95。
- [ ] **AC-07b**（Logic）: 返航后，出航前 `actual_state = damaged` 的模块：`visible_state` 维持 `damaged`，效率维持对应值（不提升至 0.95）。航行系统可追加写入受损标记（actual_state 保持 damaged），伤痕计数增加，但效率不恢复。
- [ ] **AC-08**（Logic）: 玩家在工程舱检查 unchecked 模块后，`visible_state` 同步为 `actual_state`——若 actual=installed 则变为 `installed`（η=1.0），若 actual=damaged 则变为 `damaged`（η=对应值）。
- [ ] **AC-09a**（Logic）: 对 unchecked 模块先执行检查（免费，0 材料消耗）：`visible_state` 同步为 `actual_state`——若 actual=installed 则变为 `installed`（η=1.0），若 actual=damaged 则变为 `damaged`（η=0.5 或 0.6）。检查操作不改变 actual_state。
- [ ] **AC-09b**（Logic）: 对 unchecked 模块直接执行维修（不先检查）：消耗 `repair_kit × 2`（slot_repair_cost_base），`visible_state` 和 `actual_state` 均置为 `installed`（η=1.0）。材料消耗为固定全额，与 actual_state 无关。

**货舱容积与货物：**

- [ ] **AC-10a**（Logic）: 货仓模块从 installed→damaged 导致 V_effective 从 500→250。若当前装载货物 400 体积，则 150 体积的货物变为 `trapped` 状态（`is_accessible: false`）。修复模块后 trapped 货物自动恢复（`is_accessible: true`）。
- [ ] **AC-10b**（UI）: trapped 货物在 UI 中灰显且不可交互（点击无效、tooltip 显示"货物困锁——修复货仓模块以取回"）。[Visual/Feel — 截图验证]
- [ ] **AC-11**（Logic）: 所有货仓模块卸下导致 V_effective=0，所有货物 trapped。重新安装至少一个货仓模块后恢复。

**船体完整性与波段：**

- [ ] **AC-12**（Logic）: 新游戏起始 integrity=100，波段为 `intact`，hull_scars=0，无航行惩罚。
- [ ] **AC-13**（Logic）: integrity 从 100 降至 75 时，波段从 `intact`→`damaged`，航速 ×0.9，燃料消耗 ×1.15，hull_scars += 2（基础损伤事件 +1，进入 damaged 波段 +1）。
- [ ] **AC-14**（Logic）: integrity 从 26 降至 25 时，波段从 `damaged`→`critical`，航速 ×0.75，燃料消耗 ×1.3，模块效率额外 ×0.8，`high_risk_blocked` 标志 = `true`。注意：integrity=25 时已处于 critical 波段；转换发生在跨过 26→25 边界时。
- [ ] **AC-15**（Logic）: integrity 降至 0 时，波段为 `destroyed`，无法出航——即使动力炉正常、载重未超。
- [ ] **AC-16**（Logic）: integrity 无法低于 0（15 点损伤打到 integrity=5 → integrity=0，不出现负值）。
- [ ] **AC-17**（Logic）: 修复材料使用时，integrity 恢复到 min(100, old+R_total)。若 R_total 会使 integrity 超过 100，多余修复值被丢弃。若 integrity 已为 100，修复操作被拒绝并提示"船体结构完好"。

**船体伤痕：**

- [ ] **AC-28**（Logic）: 每次航行损伤事件（integrity 减少的任意事件）使 `hull_scars += 1`。
- [ ] **AC-29**（Logic）: 跨波段伤害链——integrity 从 30（已处于 damaged 波段）一次受到 35 点伤害降至 0 时，hull_scars 累计：基础事件 +1，进入 critical +1，进入 destroyed +1（共计 +3）。integrity=30 已在 damaged 波段中，"进入 damaged"不计数。对比验证——integrity 从 80（intact）一次受到 80 点伤害降至 0：基础事件 +1，进入 damaged +1，进入 critical +1，进入 destroyed +1（共计 +4）。
- [ ] **AC-30**（Integration）: hull_scars 持久化在存档快照中，从存档恢复后与存档时一致。hull_scars 无机械效果（MVP 中仅驱动视觉和叙事），无上限。
- [ ] **AC-43**（Logic）: 船体波段重新进入——integrity 从 20（critical）修复至 30（damaged），然后受到 10 点伤害降至 20（critical 波段）。第二次伤害事件：基础事件 +1，进入 critical +1（critical 在本次事件前未被占据——玩家修复后处于 damaged 波段）。总 hull_scars 增量 = +2。验证波段重新进入被正确计数。

**适航判定：**

- [ ] **AC-18**（Logic）: 当 M_loaded > M_max 时，`can_depart()` 返回 `{false, ["overloaded"]}`，出航被阻断。
- [ ] **AC-19**（Logic）: 当 M_max = 0（两个模块均 empty 或 energy=0）时，`can_depart()` 返回 `{false, ["no_furnace"]}`。
- [ ] **AC-20**（Logic）: 当 integrity = 0 时，`can_depart()` 返回 `{false, ["hull_destroyed"]}`。
- [ ] **AC-21**（Logic）: 同时存在多个阻断条件时，`can_depart()` 返回所有原因标识。至少验证：M_loaded > M_max 且 integrity = 0 → `{false, ["overloaded", "hull_destroyed"]}`；M_max = 0 且 integrity = 0 → `{false, ["no_furnace", "hull_destroyed"]}`。

**动力炉与载重：**

- [ ] **AC-22a**（Logic）: 双货仓完好 → M_max = 24
- [ ] **AC-22b**（Logic）: 侦察+货仓完好 → M_max = 20
- [ ] **AC-22c**（Logic）: 双侦察完好 → M_max = 16
- [ ] **AC-22d**（Logic）: 货仓 damaged + 侦察完好 → M_max = 14
- [ ] **AC-22e**（Logic）: 侦察 damaged + 货仓完好 → M_max = 16
- [ ] **AC-22f**（Logic）: 双模块 damaged → M_max = 10
- [ ] **AC-22g**（Logic）: 仅单侦察 → M_max = 8
- [ ] **AC-22h**（Logic）: 仅单货仓 → M_max = 12
- [ ] **AC-23**（Logic）: 仅单货仓模块安装时（另一槽 empty），M_max = 12，飞艇可以出航。仅单侦察模块安装时，M_max = 8，飞艇可以出航。

**侦察模块获取：**

- [ ] **AC-24**（Logic）: 新游戏中侦察模块不可用（玩家尚未获得——槽 A 和槽 B 均无侦察模块）。
- [ ] **AC-25a**（Logic — 本系统可独立验证）: 槽 A 和槽 B 初始均为开放槽位——可接收任意类型模块的安装操作。槽 A 初始 empty，槽 B 预装货仓模块。
- [ ] **AC-25b**（Integration — 需探索系统 #11 + Hub #7）: 完成首次成功探索返航后，NPC 交付侦察模块物品，玩家可在 Hub 工程舱将其安装至任意空槽位。

**模块组合（开放槽位）：**

- [ ] **AC-31**（Logic）: 玩家可卸下槽 B 的货仓模块并安装至槽 A——两个槽位对任意模块类型均可安装。槽 A 安装货仓后 visible_state=installed、module_type=cargo、η_final_A=1.0（intact 波段）、槽 B 为 empty，M_max = 12（仅货仓动力炉），V_effective = 500（槽 A 货仓贡献）。
- [ ] **AC-32**（Logic）: 双货仓配置（槽 A 货仓 + 槽 B 货仓）均完好时：M_max = 24，V_effective = 1000。
- [ ] **AC-33**（Logic）: 双侦察配置（槽 A 侦察 + 槽 B 侦察）均完好时：M_max = 16，任一侦察完好即提供完整风险可见度（冗余保护）。两个侦察均 damaged 时可见范围缩减为 60%。

**Hub 信号契约：**

- [ ] **AC-34a**（Logic）: 安装模块至空槽 A 时，`slot_state_changed` 信号触发，参数为 `("slot_a", "empty", "installed")`。
- [ ] **AC-34b**（Logic）: 返航后模块进入 unchecked 状态时，`slot_state_changed` 信号触发，参数为 `("slot_a", "installed", "unchecked")`。
- [ ] **AC-35**（Logic）: 船体完整性变更时，`hull_integrity_changed(old_value, new_value)` 信号触发。波段变更时，`hull_band_changed(old_band, new_band)` 信号额外触发。
- [ ] **AC-36**（Logic）: 适航状态变更时，`departure_readiness_changed(can_depart, reasons)` 信号触发——例如最后一个动力炉被卸下时从 `(true, [])` 变为 `(false, ["no_furnace"])`。

**存档：**

- [ ] **AC-26**（Logic）: 存档时 `progress.airship` 导出为纯 Dict，不包含 Node/Resource/信号引用。格式：`{modules: {slot_a: {visible_state, actual_state, efficiency, module_type}, slot_b: {visible_state, actual_state, efficiency, module_type}}, hull_integrity, hull_scars}`。
- [ ] **AC-27**（Integration）: 从存档恢复后，模块的 visible_state、actual_state、效率、module_type、船体完整性、伤痕计数与存档时一致。

## Open Questions

[To be designed]
