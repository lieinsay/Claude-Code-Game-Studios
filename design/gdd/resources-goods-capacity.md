# 资源、货物与容量

> **Status**: In Review (Round 4 revision applied 2026-04-29 — 7 blockers resolved: add_loot formula, discard op, carried loss Pillar 4 constraint, stack_merge priority, route consumption pool+interface, signal ACs)
> **Author**: User + Claude Code
> **Last Updated**: 2026-05-09
> **Implements Pillar**: 规划先于冒险; 世界会回应照料; 飞艇是家，不只是载具
> **Platform Pivot Note**: ADR-0019 supersedes browser storage assumptions. Active resource/cargo implementation targets desktop Godot .NET/C# and desktop persistence boundaries.

## Overview

`资源、货物与容量` 是《云海织航》的资源与物流契约层。它定义材料、补给、货物、可购买商品和携带战利品的稳定身份、堆叠规则、货物分类、容量上限以及"带什么去 vs 带回什么"的取舍模型。在数据层面，它基于 `内容数据与状态注册表` 的稳定 ID 和 Schema，把所有资源实例化为可查询、可转移、可消耗、可存储的标准化条目；在玩家层面，它让每次出航前的整备（带多少补给、留多少空间给战利品、装哪种货物）、探索中的负重判断（继续深入还是撤离保全）和返航后的分配（修灯塔还是补货架还是存仓库）都基于同一套清楚、没有歧义的规则。没有这个系统，资源会在不同场景中各自定义，容量矛盾会让整备失去意义，修复和交易的因果关系也会变得不可追踪。

## Player Fantasy

玩家不会直接感受"资源系统"，但会通过它感到自己的准备不是盲目的、带回来的东西不是无名的、分配去向不是随意的。这个系统不要求玩家记住 ID、单位或堆叠上限，它要求的是一种克制的秩序感：出航前站在货舱前想清楚"这次带什么、留多少空间"，探索中面对有限容量判断"继续搜还是撤"，返航后把带回的材料放进对的位置——或送到对的人手里，或用在对的设施上。

它支撑三种间接情绪：（1）意向性——每件携带物都对应一个计划，打包不是在填表格，是在预演航线；（2）因果信心——灯塔亮了、航线稳了，玩家能回溯到是哪次探索、哪批材料、哪个决定让这件事发生；（3）归位安稳——飞艇不乱、货架不空不溢、家的秩序被每次整理和每次归来维持。这三种情绪共同锚定一个信念：照料会有痕迹，世界记得你带回了什么。

## Detailed Design

### Core Rules

**1. 资源身份与堆叠**

1. 资源实例由注册表稳定 ID（`resource.*` 格式）唯一标识，运行时以 `(id, quantity)` 元组存储。不得使用显示名、文件路径或节点引用作为资源身份。
2. `stack_rule` 决定堆叠行为：
   - `stackable`：同 ID 资源合并到一个槽位，上限 `max_stack`（全局默认 99）。
   - `unique`：每件单独占一槽，`max_stack = 1`。用于关键物品、情报物件等不可堆叠条目。
3. `unit` 字段（如 `"chunk"`、`"keg"`）仅用于 UI 显示，不参与机械运算。
4. `material_tags` 为下游系统提供过滤依据（如 `metal` 用于修复需求匹配、`repair-material` 用于维修消耗过滤）。

**2. 货物模型**

5. 货物（`kind: cargo`）是对资源的封装包装。货物的 `linked_resource_id` 指向其包含的资源稳定 ID。
6. 货物与裸资源的区别：
   - 货物只能存在于货舱中，不能进入随身物品栏或飞艇仓库。
   - 货物按 `mass_class` 占用货舱槽位（见规则 11），裸资源在物品栏/仓库中每堆占 1 槽。
   - 货物内部的资源必须在飞艇上执行显式拆包（免费、即时操作），拆包后货物物品销毁，资源进入飞艇仓库。不拆包不可直接用于修复、消耗或交易。
7. `mass_class` 定义每堆货物的槽位成本和重量值（见规则 12），`handling_class`（`crate` / `barrel` / `bundle` / `sack`）在 MVP 中仅用于交互和视觉区分。

> **货物来源（MVP）**：货物物品仅由 `空港 / 村镇状态与集市交易` 系统创建——玩家在集市购买商品时获得货物物品，不可自行将裸资源打包为货物（MVP 不提供 `pack_cargo` 操作）。货物创建是集市系统的独占权。未来版本可评估开放玩家打包。`unpack_cargo`（拆包）已由本系统定义；其反向操作 `pack_cargo` 不在 MVP 范围。

> **货物资源数量（Q）**：货物的 `linked_resource_id` 指向资源类型，货物物品自身的 `resource_quantity`（Q）属性声明了该货物拆包后获得的资源数量。Q 由创建该货物的集市系统在购买发生时设定并存储为货物物品实例的不可变属性。本系统不设定也不验证 Q 值——仅消费 Q 值执行拆包和容量计算。Q 必须是正整数。在注册表 Schema 中，货物条目应定义默认 Q 值供集市系统参考。

**3. 容量系统**

8. 容量基于槽位，每槽容纳一堆物品。`stackable` 资源在同一槽内可堆叠至 `max_stack`。
9. 三个玩家面池：

| 池 | 默认容量 | 容量类型 | 来源 | 用途 |
|---|---------|---------|------|------|
| 随身物品栏 | 5 槽 | 槽位制 | 固定（可受背包/伙伴加成） | 探索中即时携带、拾取 |
| 飞艇仓库 | 1000 容积 | 容积制 | 固定（MVP） | 长期存放、整备中转、拆包目标 |
| 货舱 | 0 容积（基础）+ 500（模块） | 容积制 | 货物/维修模块提供基础 500 | 航线批量货物运输 |

10. 未安装货物/维修模块时，货舱基础容积为 0——不能装载或运输任何货物。模块容积加成由 `飞艇模块与船体状态` 提供，本系统只消费最终容积值。
11. 物品的 `mass_class` 决定其在容积制池（仓库、货舱）中的容积占用和在货舱中的重量贡献：

| mass_class | volume（容积占用） | weight（重量值） |
|------------|-------------------|------------------|
| `light` | 50 | 1 |
| `medium` | 120 | 3 |
| `heavy` | 200 | 6 |

裸资源在随身物品栏中不计容积（槽位制），每堆固定占 1 槽。货物在货舱中同时受容积和重量约束。

**4. 重量与适航**

12. 本系统为所有已装载物品维护重量总值（`mass_class → weight_value` 映射：light=1, medium=3, heavy=6），并暴露 `get_total_loaded_mass() -> float` 查询。
13. 超重判定、载重上限和适航放行由 `飞艇模块与船体状态` 拥有。本系统只提供重量数据，不做航线/出航判定，也不自行阻止装载。

**5. 资源操作**

14. 所有操作原子执行：全成功或全失败，不产生中间状态或部分变更。
15. 操作集：

| 操作 | 语义 | 失败条件 |
|------|------|---------|
| `add(pool, resource_id, quantity)` | 添加至目标池。`stackable` 时优先与已有同 ID 堆合并。 | 池满且无同 ID 可合并堆 |
| `remove(pool, resource_id, quantity)` | 从目标池移除指定数量。 | 该 ID 总持有量不足 |
| `transfer(from_pool, to_pool, resource_id, quantity)` | 跨池转移。支持拆分：源堆保留剩余。 | 源不足或目标满 |
| `consume(pool, resource_id, quantity)` | 领域系统驱动的消耗（修复、制造、航线消耗）。 | 同 remove |
| `discard(pool, resource_id, quantity)` | 从指定池移除指定数量并永久销毁。需二次确认（类似 `commit_deposit`）。有效目标池：Pool 1（`on_person`）、Pool 2（`in_storage`）、Pool 3（`loaded`）、Pool 5（`carried`）。触发入口由探索 UI 或物品栏 UI 提供。 | 同 remove |
| `unpack_cargo(cargo_slot)` | 销毁指定货物物品，将其 `linked_resource_id` 的资源以货物声明数量加入飞艇仓库。 | 仓库剩余槽位不足以接收全部拆包资源 |
| `consume_in_combat(resource_id, quantity)` | 战斗中消耗的专用入口。一个围绕 `consume(Pool 5, resource_id, quantity)` 的薄封装，从随身物品栏（Pool 5）消耗。此封装不添加额外的原子性保证——继承 `consume` 的完全成功/完全失败语义。发出 `resource_removed(Pool5, resource_id, quantity)` 信号。由战斗与威胁处理（#12）在威胁结算期间调用。 | 同 `consume(Pool5, ...)`：Pool 5 中该 ID 总持有量不足 |
| `get_carried_contents_by_tag(material_tag)` | 查询 Pool 5 中所有 `material_tags` 与给定标签匹配的资源。返回 `Dictionary[resource_id → quantity]`。用于检查在出航前是否已准备好特定的消耗品（例如，#12 查询 `"repair-material"` 以确定 repair_kit 的可用性）。不进行修改。若没有匹配的资源，则返回一个空字典。由战斗与威胁处理（#12）在决策面板设置期间调用。 | 无（仅查询） |

16. 集市交易由 `空港 / 村镇状态与集市交易` 通过组合 `remove` / `add` 原语实现。本系统不拥有交易规则、价格或库存刷新逻辑。

**6. 供给类别**

17. `supply_class` 决定物品的默认堆叠上限、寻找来源和使用场景：

| 类别 | 默认 max_stack | 所在处 | 主要用途 |
|------|---------------|--------|---------|
| `basic` | 99 | 摊位（常备） | 航线消耗、轻量维修 |
| `repair` | 99 | 探索点 | 世界修复节点、模块制作 |
| `navigation` | 20 | 摊位、探索点 | 降低航线风险、辅助情报揭示 |
| `local-specialty` | 10 | 特定地点 | 高价值交易、满足村镇需求 |
| `intel` | 1（unique） | 探索点、伙伴侦察 | 消耗后解锁永久知识条目 |

> **航线消耗的源池**：`basic` 补给的航线消耗从 `in_storage`（Pool 2 — 飞艇仓库）扣除。这与起始状态一致（basic 补给初始位于仓库），也支持"出航前从仓库装载补给"的整备 fantasy。航线系统通过 `consume_for_route(resource_costs)` 接口发起消耗（见 Interactions）。玩家可在出航前将补给从仓库转移到随身，但航线消耗不强制此操作——航线系统可以从 `in_storage` 直接消耗。

18. `navigation` 和 `intel` 物品占用与其他物品相同的容量池。玩家在容量满时必须决定带补给还是带情报——不设单独的知识物品栏。

**7. 存储位置**

19. 六个规范池。池 1-3 由本系统直接管理，池 4-6 由对应领域系统通过本系统原语间接操作：

| # | 池 | 拥有者 | 玩家可见 | 持久化 |
|---|-----|--------|---------|--------|
| 1 | 随身物品栏 | 本系统 | 是（HUD + 探索 UI） | 是（`progress.resources` 快照） |
| 2 | 飞艇仓库 | 本系统 | 是（仓库 UI） | 是（`progress.resources` 快照） |
| 3 | 货舱 | 本系统 | 是（货舱 UI） | 是（`progress.resources` 快照） |
| 4 | 集市摊位库存 | 村镇系统 | 仅交易 UI | 是（`progress.settlement-market` 快照） |
| 5 | 探索点局内池 | 探索系统 | 仅搜撤 UI | 否（单局生成，结算后清空） |
| 6 | 修复节点提交 | 世界修复系统 | 仅修复 UI | 否（即时消耗，不可逆） |

**8. 信号契约**

20. 本系统是所有资源状态的唯一权威源。下游系统通过本系统暴露的查询方法消费状态，通过信号接收变更通知。调用方不得缓存资源数量数据（见 Interactions 数据流方向）。
21. 所有信号在状态变更完成后触发（emit-after-mutation），不在操作中途触发；信号处理器不得在回调中重新进入本系统的变更操作（禁止重入 mutation），但可安全调用查询方法。
22. 信号集：

| 信号 | 参数 | 触发时机 |
|------|------|---------|
| `pool_changed(pool_id: StringName)` | 池标识 | 任意池内容变更后（聚合通知；UI 应按需重查） |
| `resource_added(pool_id, resource_id, quantity)` | 池、资源ID、数量 | `add()` 成功后 |
| `resource_removed(pool_id, resource_id, quantity)` | 池、资源ID、数量 | `remove()` / `consume()` 成功后 |
| `transfer_completed(from_pool, to_pool, resource_id, quantity)` | 源池、目标池、资源ID、数量 | `transfer()` 成功后 |
| `cargo_unpacked(cargo_id, resource_id, quantity)` | 货物ID、资源ID、数量 | `unpack_cargo()` 成功后 |
| `deposit_committed(repair_node_id)` | 修复节点ID | `commit_deposit()` 成功后 |
| `mass_changed(new_mass: int)` | 新总装载质量 | 货舱内容变更后 |

23. Godot 4.6 的信号默认同步派发：`emit()` 调用会立即执行所有已连接回调，回调返回后才继续执行 emit 之后的代码。因此信号必须在所有状态变更完成后才触发，避免处理器读到中间态。实现要求见 EC-23。

### States and Transitions

资源堆/槽位在六个池之间移动，状态即"当前所在池"：

| 状态 | 对应池 | 含义 | 有效转入 | 有效转出 |
|------|--------|------|---------|---------|
| `on_person` | Pool 1 — 随身物品栏 | 玩家身上持久携带，不随探索结算丢失 | 初始状态、`in_storage`（从仓库取出） | `in_storage`（存入仓库）、`carried`（进入探索时选带）、`deposited`（直接提交修复）、`destroyed`（消耗） |
| `in_storage` | Pool 2 — 飞艇仓库 | 在飞艇仓库中，未装载 | `on_person`（存入）、`loaded`（卸货）、`carried`（探索成功撤离）、`unpack_cargo`（拆包结果） | `on_person`（取出）、`loaded`（装载到货舱）、`listed`（上架集市）、`deposited`（提交修复） |
| `loaded` | Pool 3 — 货舱 | 已装载到货舱，等待出航或已在航线中 | `in_storage`（装载动作） | `carried`（进入探索点）、`in_storage`（卸货撤回）、`deposited`（直接提交修复）、`listed`（上架） |
| `carried` | Pool 5 — 探索点局内池 | 探索中随身携带，处于局内（临时） | `on_person`（从随身选带进入探索）、`loaded`（从货舱物资进入探索）、`in_storage`（从仓库直接携带） | `in_storage`（探索成功撤离，全部归仓）、`destroyed`（探索失败按损失比部分损失） |
| `deposited` | Pool 6 — 修复节点提交 | 已提交到修复节点，退出玩家池 | `on_person`、`in_storage`、`loaded`、`carried`（经修复确认） | **终态**——不可逆，不可取回 |
| `listed` | Pool 4 — 集市摊位库存 | 在集市上架，等待交易 | `in_storage`、`loaded` | `in_storage`（下架取回）、`destroyed`（被买走/成交） |
| `destroyed` | —（终态） | 已消耗/已损失，退出所有池 | `carried`（探索失败损失）、`on_person`/`in_storage`/`loaded`/`listed`（领域系统驱动消耗） | **终态** |

**关键约束：**
- `deposited` 和 `destroyed` 为终态，不可撤销。`deposited` 在 UI 中必须经过确认才能提交。
- `carried` 状态（Pool 5，局内临时）的资源在探索失败时，按 `extraction_loss_ratio` 比例进入 `destroyed`，剩余自动回到 `in_storage`。撤离成功时全部进入 `in_storage`。**仅 `carried`（Pool 5）受探索失败影响**——`on_person`（Pool 1）中的物品不参与探索风险，探索期间保留在玩家身上不受损失。
- **探索失败损失 Pillar 4 约束**：探索系统拥有 `extraction_loss_ratio` 参数和损失公式，但必须在设计时遵守以下不可协商约束：(a) Q=1 的 `unique` 物品（max_stack=1，如 intel）在探索失败时损失量为 0——unique 物品不可被探索失败完全摧毁，这违反 Pillar 4（温和压力）；(b) 损失公式应参照 EC-05 的"至少保留 1"保护模式，确保单堆不全毁。此约束已记录在 Tuning Knobs 的 `extraction_loss_ratio` 参数说明中。
- 探索结束后 `carried` 物品归入 `in_storage`（非 `on_person`）。玩家需手动从仓库取回物品到随身物品栏——这强化了"返航整理"的归位仪式感。
- 任何状态变更必须通过本系统的原子操作——领域系统不得直接修改池内容或绕过状态机写入。
- 资源不能同时在两个池中。跨池转移必须先 `remove` 再 `add`，由原子 `transfer` 原语包裹。
- `loaded` 状态下飞艇被摧毁或模块被移除时，货物按 EC-05 规则处理（部分损失 + 可回收货箱）。

### Starting State (MVP Bootstrapping)

新游戏开始时，资源系统的初始状态必须支持玩家从飞艇出发完成首轮探索——创意总监要求 MVP GDD 证明 `Hub → Route → Explore → Return → Repair` 闭环：

| 池 | 初始内容 | 说明 |
|----|---------|------|
| `on_person`（Pool 1 — 随身物品栏） | 空（0/5 槽） | 玩家在飞艇中开始，物品存放在仓库 |
| `in_storage`（Pool 2 — 飞艇仓库） | `basic_supply` × 10（1 堆，basic，light），`repair_kit` × 4（1 堆，repair，light） | 启动补给：足够一次安全航线消耗 + 一次完整模块维修或多次船体小修的材料参考量 |
| `loaded`（Pool 3 — 货舱） | 空（0/500 容积） | 货舱模块已在飞艇上预装，提供 500 容积。玩家开局时无待运输货物 |
| `carried`（Pool 5 — 探索局内） | 空 | 从进入探索点开始生成 |
| `deposited`（Pool 6） | 空 | 随修复提交累积 |
| `listed`（Pool 4） | 空 | 由集市系统初始化 |

**关键设计决策**：
- 货舱模块在 MVP 开局时预装——这避开了"需要资源安装模块才能运输货物赚取资源"的鸡与蛋问题。预装模块是新手引导的一部分（`新手引导与首轮闭环` GDD 负责展示货舱功能）。
- 起始资源的具体数量和类型是可调参数（见 Tuning Knobs）；上述数值是 MVP 最低存活基准——足够触发首轮闭环但不足以跳过探索需求。
- 起始状态由 `本地存档与世界状态持久化` 系统在 `new_game()` 时通过快照包注入。本系统暴露 `reset_for_new_game(starting_snapshot)` 接口。

### Interactions with Other Systems

| 系统 | 方向 | 本系统提供 | 本系统接收 | 边界 |
|------|------|-----------|-----------|------|
| `内容数据与状态注册表` | 上游 | — | 资源/货物稳定 ID、`stack_rule`、`unit`、`material_tags`、`mass_class`、`handling_class`、`supply_class` | 注册表只提供静态定义；本系统不写回注册表 |
| `本地存档与世界状态持久化` | 上游 | `progress.resources` 快照包（池 1-3 完整状态） | 保存调度、恢复结果、迁移提示 | 存档系统不解释资源语义；快照 payload 只含稳定 ID + 数量 + 池归属 |
| `玩家移动与交互` | 上游 | — | `use_requested` 对存储点/拾取点/货舱的交互入口 | 本系统处理 `use_requested` 触发的存储/拆包/装载后果；移动系统不拥有资源规则 |
| `飞艇模块与船体状态` | 下游 | `get_total_loaded_mass()`、`get_cargo_bay_capacity()`、`get_storage_capacity()`、`can_afford_cost(resource_costs)`、`consume_for_module(resource_costs)` | 货舱槽位加成、载重上限 | 模块系统拥有载重适航判定；本系统只提供重量和容量数据 |
| `探索 / 搜撤场景` | 下游 | `get_carried_contents()`、`get_carry_capacity()`、`add_loot(resource_id, quantity)`、`extract_carried_to_storage()` | 探索开始（将 `loaded` → `carried`）、探索结算（撤离或失败处理） | 探索系统拥有生成规则和撤离判定；本系统只提供容量检查和转移执行 |
| `航行与路线风险` | 下游 | `consume_for_route(resource_costs)`（封装 `consume(in_storage, ...)` 调用）、`get_storage_summary()` | 航线消耗请求（消耗 basic/navigation 补给） | 航线系统拥有消耗速率和消耗时机；本系统只执行从 `in_storage` 的原子消耗 |
| `战斗与威胁处理` | 下游 | `get_carried_contents_by_tag(material_tag)`、`consume_in_combat(resource_id, quantity)` | 威胁消耗请求 | 战斗系统拥有威胁后果；本系统只提供物资查询和消耗 |
| `世界修复与解锁` | 下游 | `can_deposit(repair_node_id, resource_costs)`、`commit_deposit(repair_node_id, resource_costs)` | 修复节点的材料需求列表 | 修复系统拥有解锁条件和修复语义；`commit_deposit` 为不可逆终态操作 |
| `空港 / 村镇状态与集市交易` | 下游 | `get_available_goods(stall_id)`、`validate_purchase(good_id, quantity)`、`execute_purchase(good_id, quantity)`、`list_for_sale(resource_id, quantity, price)` | 购买/出售/上架/下架请求 | 集市系统拥有价格、库存和摊位规则；本系统只执行原子转移 |
| `UI / HUD / 航图界面` | 下游 | `get_storage_summary()` → `{total_volume, used_volume, stacks: [{id, qty, mass_class}]}`; `get_cargo_bay_summary()` → `{total_volume, used_volume, total_mass, stacks: [{id, qty, mass_class}]}`; `get_carry_summary()` → `{total_slots, used_slots, stacks: [{id, qty, mass_class}]}` | — | UI 只读不写；不直接修改任何池内容 |
| `玩家知识与情报` | 下游 | `get_carried_intel()`（过滤 `supply_class=intel`）、`consume_intel(intel_id)` | 情报消耗请求（解锁永久知识） | 情报系统拥有已知/未知状态；本系统只负责情报物品的持有和消耗 |
| `伙伴功能与关系` | 下游 | `get_carry_capacity()` | 伙伴携带容量加成（如有） | 伙伴系统拥有关系状态；容量加成由伙伴系统通过本系统接口注入 |

**数据流方向**：上游系统向本系统注入静态定义和基础设施能力；下游系统通过本系统提供的查询和操作接口消费资源状态（主动拉取），通过信号接收变更通知（被动推送）。本系统是所有资源状态的唯一权威源——下游系统不得缓存或复制资源数量数据。

## Formulas

The `slot_availability` formula is defined as:

`slot_available = (used_slots + slot_cost <= total_slots)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `used_slots` | U | int | [0, total_slots] | 池中当前已占用槽位数 |
| `slot_cost` | C | int | {1} | 新堆所需槽位。裸资源在随身物品栏中固定为 1 |
| `total_slots` | T | int | {5 + bonuses} | 池总槽位：随身基础 5，可受背包/伙伴加成 |
| `slot_available` | S | bool | {false, true} | 是否有空槽放置新堆 |

**Output Range:** false / true.
**Example:** 随身 4/5 槽已用，新资源 slot_cost=1，4+1=5 ≤ 5 → true。

---

The `volume_availability` formula is defined as:

`volume_available = (used_volume + item_volume <= total_volume)`

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `used_volume` | U | int | [0, total_volume] | 池当前已用容积 |
| `item_volume` | V | int | {50, 120, 200} | 待放入物品的 volume 值（由 mass_class 决定） |
| `total_volume` | T | int | {1000, 0-500+bonuses} | 池总容积：仓库=1000，货舱基础=0 + 模块提供 |
| `volume_available` | A | bool | {false, true} | 池是否有足够容积 |

**Output Range:** false / true.
**Example:** 仓库已用 920，尝试存入 medium 物品（120），920+120=1040 > 1000 → false。
**Example:** 货舱已用 380，装载 medium 货物（120），380+120=500 ≤ 500 → true。

---

The `stack_merge` formula is defined as:

```
if has_match:
    merge_qty = min(Q, max_stack - E)
else:
    merge_qty = 0
overflow_qty = Q - merge_qty
```

When no matching stack exists in the target pool (`has_match = false`, E irrelevant), nothing can merge: `merge_qty = 0`, `overflow_qty = Q`. A new slot/volume is always required.

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `has_match` | H | bool | {false, true} | 目标池中是否已有该 resource_id 的堆 |
| `Q` | Q | int | [0, ∞) | 待添加数量（0 = no-op per EC-14） |
| `E` | E | int | [0, max_stack] | 已有同 ID 堆的数量（has_match=false 时忽略此值） |
| `max_stack` | M | int | {1, 10, 20, 99} | 该资源的堆叠上限（由 supply_class / stack_rule 决定） |
| `merge_qty` | MQ | int | [0, Q] | 合并到已有堆的数量 |
| `overflow_qty` | OQ | int | [0, Q] | 需要新槽位的剩余数量。overflow_qty > 0 时需检查 slot_availability 或 volume_availability |

**Output Range:** merge_qty [0, min(Q, max_stack)].
**Example:** Q=30 basic（max_stack=99），has_match=true, E=80。merge_qty=min(30, 19)=19，overflow_qty=11。若为槽位制池，overflow_qty=11 需一个空槽。
**Example:** Q=30 basic（max_stack=99），has_match=false。merge_qty=0，overflow_qty=30。需新槽/新容积。

**合并优先级**：若目标池中存在多个同 ID 匹配堆（如 basic E1=80, E2=60），优先合并到已有数量最大的堆（fill fullest first），以最小化 overflow_qty。若多个堆数量相同，合并到最低槽位索引的堆。此规则保证 `stack_merge` 的确定性行为。

---

The `transfer_validation` formula is defined as:

`transfer_valid = (source_count >= Q) AND target_valid_for_kind AND target_can_take`

Where `target_valid_for_kind`:
- If item `kind = cargo`: target must be cargo_bay（Rule 6: 货物只能存在于货舱中，不可进入随身物品栏或飞艇仓库）
- If item `kind = resource`: target can be carry or storage（裸资源不可进入货舱——货舱仅接受货物物品）

Where `target_can_take`:
- For slot-based pool (carry inventory): `(has_match AND overflow_qty = 0)` OR `slot_capacity_check(target, overflow_qty, max_stack) = true`
- For volume-based pool (storage, cargo bay): `(has_match AND overflow_qty = 0)` OR `volume_capacity_check(target, overflow_qty, max_stack, item_volume) = true`

Where `slot_capacity_check` = `used_slots + ceil(overflow_qty / max_stack) <= total_slots`（溢出所需的槽位数，而非仅 1 槽）

Where `volume_capacity_check` = `used_volume + ceil(overflow_qty / max_stack) × item_volume <= total_volume`（溢出所需的容积，而非仅 1 堆容积。与 `unpack_validation` 中的多堆容积计算方式一致）

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `source_count` | S | int | [0, ∞) | 源池中该 resource_id 的总持有量 |
| `Q` | Q | int | [1, source_count] | 请求转移的数量 |
| `target_can_take` | T | bool | {false, true} | 目标池可容纳 Q（含合并或新堆/新容积） |
| `transfer_valid` | V | bool | {false, true} | 转移是否可行 |

**Output Range:** false / true.
**Example:** 从仓库转移 Q=5 repair（max_stack=99）到随身（4/5 槽，无 repair 堆）。source: ✓。target: overflow_qty=5, slot_available: 4+1≤5 ✓。→ true。

---

The `total_loaded_mass` formula is defined as:

`total_loaded_mass = sum(weight_value(item.mass_class) for each item in cargo_bay)`

Only cargo bay contents contribute to ship flight mass. Raw resources in carry inventory and airship storage do not contribute.

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `w_i` | W | int | {1, 3, 6} | 每个货物的重量值：light=1, medium=3, heavy=6 |
| `N` | N | int | [0, ∞) | 货舱中的货物数量 |
| `total_loaded_mass` | M | int | [0, ∞) | 货舱总装载质量 |

**Output Range:** [0, ∞). MVP 货舱 500 容积，满载时质量范围约 5-14（取决于货物组合，如 2 heavy=400 容积/12 重量，10 light=500 容积/10 重量）。
**Example:** 货舱中 2 light（2×1）+ 1 medium（1×3）+ 1 heavy（1×6）= 11。

---

The `unpack_validation` formula is defined as:

`unpack_valid = (has_match AND overflow_qty = 0) OR volume_availability(storage, overflow_volume)`

Where `overflow_qty` and `overflow_volume` are derived from `stack_merge` applied to the unpacked resource:
- `merge_qty` = quantity absorbed by existing matching stack (see stack_merge formula)
- `overflow_qty` = Q - merge_qty (quantity requiring new stack(s))
- `overflow_volume` = ceil(overflow_qty / max_stack) × resource_volume (number of new stacks needed × volume per stack)

If `overflow_qty = 0`, all items merge into existing stacks — no additional volume needed, unpack always valid.
If `overflow_qty > 0`: new stacks are created, each occupying the resource's mass_class volume. `volume_availability` checks if storage has room.

Unpacking destroys the cargo item and adds its linked resource to airship storage. Since the resource inside the cargo enters as a raw resource (non-cargo), it occupies storage volume based on its own mass_class mapping:

| Resource mass_class (from linked resource) | volume in storage |
|--------------------------------------------|-------------------|
| light | 50 |
| medium | 120 |
| heavy | 200 |

If a matching stack already exists in storage and can absorb the full quantity (see `stack_merge`), no additional volume is needed.

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `has_match` | H | bool | {false, true} | 仓库中已有该 linked_resource_id 的堆 |
| `E` | E | int | [0, max_stack] | 已有匹配堆的数量 |
| `Q` | Q | int | [1, ∞) | 货物包含的资源数量 |
| `max_stack` | M | int | {1, 10, 20, 99} | 该资源的堆叠上限 |
| `unpack_valid` | V | bool | {false, true} | 拆包是否可行 |

**Output Range:** false / true.
**Example:** 拆包 Q=5 basic（max_stack=99）的 light 货物。仓库已用 920/1000，已有 basic 堆 E=90。has_match=true，90+5=95≤99 → 全部合并，不占新容积 → true。

---

The `add_loot` formula is defined as:

`add_loot_valid = slot_capacity_check(carry, Q, max_stack) OR (has_match AND E + Q <= max_stack)`

Where `slot_capacity_check` = `used_slots + ceil(overflow_qty / max_stack) <= total_slots`（与 `transfer_validation` 中同名检查一致；`overflow_qty` = Q when no match, or `Q - (max_stack - E)` when match exists but is insufficient）。此修复与 Round 2 B2 的 `transfer_validation` 修正同一模式——确保多堆溢出时检查足够槽位数而非仅 1 槽。

**Variables:**

| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| `has_match` | H | bool | {false, true} | 随身中已有该 resource_id 的堆 |
| `E` | E | int | [0, max_stack] | 已有匹配堆的数量（has_match=false 时忽略） |
| `Q` | Q | int | [1, ∞) | 拾取数量 |
| `max_stack` | M | int | {1, 10, 20, 99} | 该资源的堆叠上限 |
| `used_slots` | U | int | [0, total_slots] | 当前已占用槽位数 |
| `total_slots` | T | int | {5 + bonuses} | 池总槽位 |
| `add_loot_valid` | V | bool | {false, true} | 拾取是否可行 |

**Output Range:** false / true.
**Example 1（合并场景）:** 拾取 Q=3 repair（max_stack=99），随身 5/5 槽已满，但有 repair 堆 E=90。has_match=true，90+3=93≤99 → true（合并不需要新槽位）。
**Example 2（多堆溢出）:** 拾取 Q=200 basic（max_stack=99），随身 2/5 槽已用，无 basic 堆。has_match=false，overflow_qty=200，slot_capacity_check: 2 + ceil(200/99)=2+3=5 ≤ 5 → true（创建 3 个新堆：99+99+2）。
**Example 3（多堆溢出被拒）:** 拾取 Q=200 basic（max_stack=99），随身 4/5 槽已用，无 basic 堆。slot_capacity_check: 4 + ceil(200/99)=4+3=7 > 5 → false。

---

### mass_class Reference Table

| mass_class | volume（容积占用） | weight（重量值） | 典型轮廓 |
|------------|-------------------|------------------|---------|
| `light` | 50 | 1 | 小包裹、卷轴筒、情报信匣 |
| `medium` | 120 | 3 | 标准货箱、木桶 |
| `heavy` | 200 | 6 | 大型板条箱、机械部件、成捆金属 |

容积效率（volume / weight）：light=50.0, medium=40.0, heavy≈33.3。heavy 容积效率最优（每单位重量占最少容积），但粒度最粗（200 容积/堆）——适合高重量密度运输；medium 在重量受限场景下每堆重量适中（3 wt/堆）；light 粒度最细（50 容积/堆）——适合灵活拼装、精确匹配剩余容积。不同货物类型在不同约束维度下各有优势，避免单一最优解。

### Capacity Pool Summary

| 池 | 容量类型 | 默认值 | 加成来源 |
|---|---------|--------|---------|
| 随身物品栏（Pool 1 — `on_person`） | 槽位制 | 5 槽 | 背包物品（预留）、伙伴携带加成（预留） |
| 飞艇仓库（Pool 2 — `in_storage`） | 容积制 | 1000 | 飞艇模块扩展（后续） |
| 货舱（Pool 3 — `loaded`） | 容积制 | 0 + 500 | 货物/维修模块提供基础 500 |
| 探索局内池（Pool 5 — `carried`） | 槽位制 | 5 槽 | 与随身物品栏共享加成来源（背包、伙伴） |

## Edge Cases

### 容量边界

**EC-01: 货舱零容量（未安装模块）**
未安装货物/维修模块时，货舱基础容积为 0。任何 `add(cargo_bay, ...)` 或 `load` 操作返回 `ERR_CAPACITY_ZERO`。货舱 UI 显示"未安装货舱模块"，容量条置灰。在模块提供容积之前，无法从仓库装载货物到货舱。

**EC-02: 拾取时局内临时池（`carried`）已满**
`add_loot()` 被调用，但 `carried`（Pool 5 — 探索点局内池）无空槽且无匹配的可堆叠资源（或有匹配但已达 max_stack），拾取失败。交互 UI 显示"随身物品已满"，物品保留为世界中的可拾取实体。玩家必须丢弃携带物品或放弃拾取。注意：探索中拾取的战利品进入 `carried`（Pool 5）而非 `on_person`（Pool 1）——仅 `carried` 中的物品受探索失败提取损失影响（见状态机关键约束）。

**EC-03: 转移到已满目标池（原子失败）**
`transfer(from, to, id, qty)` 发现目标池在尝试合并后仍无足够容积/槽位，则整个操作原子失败——不发生部分转移。源堆保持原样。UI 收到 `ERR_TARGET_FULL` 并显示具体命中的约束（容积或槽位）。

**EC-04: 拆包到已满仓库**
`unpack_cargo(cargo_slot)` 产生的资源超出仓库容积，且该资源在仓库中无已有堆可合并吸收全部数量，拆包失败返回 `ERR_STORAGE_FULL`。货物保留在货舱槽位。无部分拆包——全成功或全失败。

**EC-05: 有货物装载时禁止移除模块；战斗摧毁则货物损失**
当货舱中有货物（`loaded` 状态非空，即 `used_volume > 0`）时，玩家不可移除或替换提供货舱容积的模块。模块管理 UI 对此类模块的移除/替换按钮置灰，提示"请先卸下货舱中的货物"。模块系统在移除前调用 `get_cargo_bay_usage()` 检查——若 `used_volume > 0`，移除被拒绝。

若模块在战斗中被摧毁（非玩家主动操作），货舱内容按以下规则处理，不与游戏概念的失败设计理念（"失败应以教育性损失为主；不做恶劣惩罚"）和支柱 4（"未知带来温和压力"）冲突：

1. **部分损失**：货舱中每个货物堆的损失量按公式 `loss = min(Q - 1, max(1, ceil(Q × 0.4)))` 计算，保留量 `retention = Q - loss`。Q=1 时 loss=0（保留 1 单位，保证单堆不 100% 全毁）；Q≥2 时 loss 至少为 1。货物物品本体进入 `destroyed` 终态，但其内部资源按 `retention` 保留在可回收货箱中。
2. **可回收货箱**：每个货物堆中保留的资源（retention = Q − loss）以"受损货箱"形式弹出到飞艇附近世界空间（`loaded` → `recoverable_crate` 临时状态）。玩家可在飞艇修复后靠近拾取，货箱内容自动回到仓库。
3. **通知**：玩家收到通知："货舱模块被摧毁！部分货物已损失，剩余货物散落在飞艇附近——修复模块后可回收。"受损货箱在世界中以闪烁标记显示。
4. **货舱归零**：货舱容积回到基础值 0，直到模块被修复或更换。在重新安装模块之前，不可装载新货物。
5. **设计意图**：保留战斗损失的风险感（支柱 4 的温和压力），但不以完全摧毁惩罚玩家（支柱 2 的世界照料痕迹被保留）。可回收货箱创造后续回收行动——损失是暂时的，补救是主动的。

### 状态与持久化边界

**EC-06: carried 状态下崩溃/退出**
游戏在资源处于 `carried` 状态（探索中）时终止（崩溃、浏览器标签页关闭、强制退出），存档系统在最后稳定边界捕获快照。按持久化 GDD 的存档边界规则，探索中不是稳定边界——存档仅在停靠/航线提交/探索结算/修复提交/交易结算时产生。重新加载时：
- 若最后存档在探索开始前：资源处于 `loaded` 状态（仍在货舱中），探索损失被撤销（在恢复的存档中探索从未"发生"）
- 若最后存档在结算后：资源处于 `in_storage`（成功撤离已提交）
- 系统不尝试"恢复"探索中途状态——这违反快照契约

**EC-07: 原子操作中途存档（快照时序）**
若存档在 `transfer()` 执行期间被触发（由稳定边界事件），快照捕获的是完全转移前或完全转移后的状态——绝不会是转移中途状态。引擎的存档系统在游戏循环内同步序列化，原子操作在单帧/tick 内完成，因此存档始终看到一致状态。实现要求：原子操作必须在让出到下一帧之前完成；transfer 逻辑中不得有 `await` 或延迟执行。

**EC-08: 版本间 max_stack 变更**
若未来版本更改了资源的 `max_stack`（如 basic 从 99→50），加载时：
- 等于或低于新 `max_stack` 的已有堆不受影响
- 超出新 `max_stack` 的堆被拆分：一个堆为新 `max_stack`，余量在槽位/容积允许时生成一个新堆
- 若无空间容纳拆分余量：余量进入 `destroyed` 终态，并显示玩家通知："货物堆叠规则已更新，部分物品因空间不足被丢弃"

**EC-09: 版本间 mass_class 变更**
若未来版本更改了资源的 `mass_class`（如 medium→heavy），加载时：
- 已有堆的容积和重量使用新 `mass_class` 值重新计算
- 若新容积超出当前池容量：超出部分进入 `destroyed` 终态，玩家收到通知
- 若新重量超出飞船飞行阈值（由飞船系统拥有）：飞船系统在下一次查询时收到更新后的 `get_total_loaded_mass()`，自行应用门控逻辑
- 写入存档迁移日志条目：`mass_class of [resource_id] changed from [old] to [new]`

### 数据完整性边界

**EC-10: 资源 ID 弃用或退役**
若注册表将资源 ID 标记为 `deprecated`，玩家库存中的已有实例保持可用但不可再获取。以弃用 ID 调用 `add()` 返回 `ERR_DEPRECATED_ID`。弃用资源仍可正常移除、转移、消耗或交易——只是不可补充。若 ID 为 `retired`（完全移除），加载时该 ID 的任何实例被转换为通用"遗留物品"占位符，原始显示名保留在提示中："此物品已从游戏中移除"。

**EC-11: 货物 linked_resource_id 重复**
注册表 Schema 保证每个货物 ID 恰好链接到一个 `linked_resource_id`。若数据错误导致重复（同一资源被两个货物 ID 链接），系统将它们视为独立货物物品——它们只是拆包出相同的资源。不产生运行时错误。重复在内容验证时被标记（注册表系统的职责），而非运行时。

**EC-12: 资源 ID 无注册表条目（ERR_MISSING_REFERENCE）**
若 `add()` 或任何操作收到不在注册表中的资源 ID，返回 `ERR_MISSING_REFERENCE`。操作被拒绝。这是数据完整性守卫——正常游戏中不应发生，但可防止损坏的存档数据或 mod 错误。

### 用户交互边界

**EC-13: 提交不可逆确认**
玩家发起向修复节点的 `deposit` 时，UI 显示确认对话框，列出所有待提交资源及数量，并警告："提交后材料不可取回，确定提交？"`commit_deposit()` 仅在用户明确确认后才被调用。确认后无"撤销"。

**EC-14: 零数量操作（无操作）**
`add()`、`remove()`、`transfer()` 或 `consume()` 以 `quantity = 0` 调用时，立即返回成功且无状态变更。这不是错误——允许调用方传入计算结果而不必先检查零值。

**EC-15: 负数量（拒绝）**
所有操作以 `quantity < 0` 调用时，返回 `ERR_INVALID_QUANTITY`。负值在资源模型中没有有效语义。

### 容量碎片化边界

**EC-16: 容积碎片化（有意不整理）**
系统不自动碎片整理或重排堆来优化容积使用。容积以单个整数计数器跟踪，不存在传统磁盘意义上的碎片——但系统不会跨槽位重新合并部分堆来腾出空间。每个堆占据其逻辑槽位的完整容积。这是有意设计：保持心智模型简单，防止玩家未发起的意外堆合并。

**EC-17: 大量 max_stack=1 物品耗尽容积**
每件 `unique` 物品（max_stack=1，如情报物品）单独占据其完整 `mass_class` 容积——它们不堆叠，因此 10 件 light 情报物品消耗 10 × 50 = 500 容积（基础仓库的一半）。这是有意设计：情报物品物理上比其信息价值更占空间，强化了规则 18 中"带补给还是带情报"的取舍。

**EC-18: 随身槽位耗尽 vs. 容积耗尽——不同提示**
随身物品栏满时，系统根据约束返回不同的失败信息：
- 无空槽且无匹配堆：`ERR_CARRY_SLOTS_FULL` → UI："随身物品栏已满"
- 有匹配堆但已达 max_stack：`ERR_CARRY_STACK_FULL` → UI："该物品已达堆叠上限"
- 若后续系统添加随身容积加成：错误区分槽位 vs. 容积耗尽
确保玩家知道为什么不能拾取，而不仅仅是不能拾取。

### 并发与性能边界

**EC-19: 同时访问（序列化保证）**
所有资源操作是同步的，在单个游戏循环 tick 内执行。Godot 的单线程模型（信号除非延迟否则同步处理）保证无并发修改。由资源操作触发、回调到资源系统的信号处理器在同一调用栈上执行——第一个操作在第二个开始前已完成。这不是需要特殊处理的情况；它是引擎的属性，GDD 明确记录以避免程序员尝试添加不必要的锁。

### 货物特定边界

**EC-20: mass_class 变更后货舱 loaded_mass 不一致**
若资源的 `mass_class` 在版本间变更（EC-09），加载时 `get_total_loaded_mass()` 值从当前货舱内容使用当前 `mass_class` 映射重新计算。存档中持久化的质量值不被直接使用——它从内容派生。这防止了存储质量与实际货物组成之间的漂移。若新质量超出飞船容量，飞船系统处理门控（规则 13）。

**EC-21: 部分转移（拆分支持）**
`transfer(from, to, id, qty)` 被调用且 `qty < source_stack.count` 时，源堆被拆分：`qty` 移动到目标，`source_stack.count - qty` 留在源池。这不是部分失败——这是预期行为。整个操作原子完成（移除和添加都完成，或都不完成）。拆分仅对 `stackable` 资源支持；对 `unique` 物品，`qty` 必须等于完整堆数量（1）。

**EC-22: 探索开始时随身物品栏已满**
玩家从飞艇进入探索点时，从 `on_person`（Pool 1，随身物品栏）中选择物品带入 `carried`（Pool 5，局内临时）。若玩家 `on_person` 物品栏已满（全部 5 槽位达 max_stack），而探索点生成了战利品，玩家立即面临丢弃决策。进入探索时，若随身已满，探索 UI 明确警告："随身物品栏已满，探索中无法拾取新物品。是否继续？"这是玩家的选择——系统不阻止满物品栏进入探索。

### 信号契约边界

**EC-23: 信号触发时序与重入防护**
本系统的所有信号必须在状态变更完全完成后触发（emit-after-mutation），不得在操作中途触发。具体规则：
1. 任一变更操作（`add`、`remove`、`transfer`、`consume`、`unpack_cargo`、`commit_deposit`）必须在对所有受影响池完成修改后才 emit 信号。
2. 信号处理器（回调）不得在回调执行期间调用本系统的变更方法——违反此规则会导致重入错误（`ERR_BUSY`）。处理器可安全调用查询方法（`get_*`、`can_*`、`validate_*`）。
3. 若下游系统在信号处理器中需要触发新的变更操作，应使用 `call_deferred()` 将操作推迟到下一帧执行。
4. Godot 4.6 的信号默认同步派发，因此规则 1-2 利用同步派发特性保证一致性：emit 时状态已完整，处理器读到的是完整状态，处理器返回后 emit 之后的代码才继续执行。
5. 关于并发性：参见 EC-19——Godot 在单个游戏循环 tick 内串行执行，无真正并发。EC-23 补充的是"单帧内因信号回调导致的重入"，而非多线程问题。

## Dependencies

### 上游依赖（本系统依赖的外部系统）

| 系统 | 状态 | 本系统需要什么 | 关键契约 |
|------|------|---------------|---------|
| **内容数据与状态注册表** | ✅ Approved | 资源/货物稳定 ID（`resource.*`、`cargo.*`）；`stack_rule`、`unit`、`material_tags`、`mass_class`、`handling_class`、`supply_class` 字段 | 注册表提供静态定义，本系统不写回。资源域必须在资源 UI 可显示前完成 |
| **本地存档与世界状态持久化** | ✅ Approved | 保存调度、恢复结果、迁移提示；快照 payload 格式约束（bool/int/finite float/string/enum/array/dict） | 本系统以 `progress.resources` 快照包提供池 1-3 完整状态。存档系统不解释资源语义。保存仅在稳定边界触发（停靠/航线提交/探索结算/修复提交/交易结算） |
| **玩家移动与交互** | ✅ Approved | `use_requested` 分发——存储点/拾取点/货舱的交互入口 | 本系统处理 `use_requested` 触发的存储/拆包/装载后果。移动系统不拥有资源规则 |

### 下游依赖（依赖本系统的外部系统）

| 系统 | 状态 | 从本系统获取什么 | 本系统暴露的接口 | 契约约束 |
|------|------|-----------------|-----------------|---------|
| **飞艇模块与船体状态** | ❌ 未设计 | 货舱容积加成、载重上限判断依据、模块消耗材料 | `get_total_loaded_mass()`、`get_cargo_bay_capacity()`、`get_storage_capacity()`、`can_afford_cost(resource_costs)`、`consume_for_module(resource_costs)` | 模块系统拥有载重适航判定和容积加成；本系统只提供重量和容量数据，不自行阻止装载 |
| **探索 / 搜撤场景** | ❌ 未设计 | 随身容量检查、战利品拾取、探索结算转移 | `get_carried_contents()`、`get_carry_capacity()`、`add_loot(resource_id, quantity)`、`extract_carried_to_storage()` | 探索系统拥有生成规则和撤离判定；本系统只提供容量检查和转移执行 |
| **战斗与威胁处理** | ❌ 未设计 | 威胁消耗、战损扣减 | `get_carried_contents_by_tag(material_tag)`、`consume_in_combat(resource_id, quantity)` | 战斗系统拥有威胁后果；本系统只提供物资查询和消耗 |
| **世界修复与解锁** | ❌ 未设计 | 修复节点材料提交（不可逆） | `can_deposit(repair_node_id, resource_costs)`、`commit_deposit(repair_node_id, resource_costs)` | 修复系统拥有解锁条件和修复语义；`commit_deposit` 为不可逆终态操作 |
| **空港 / 村镇状态与集市交易** | ❌ 未设计 | 购买/出售/上架/下架的原子资源转移 | `get_available_goods(stall_id)`、`validate_purchase(good_id, quantity)`、`execute_purchase(good_id, quantity)`、`list_for_sale(resource_id, quantity, price)` | 集市系统拥有价格、库存和摊位规则；本系统只执行原子转移 |
| **UI / HUD / 航图界面** | ❌ 未设计 | 仓库/货舱/随身摘要数据 | `get_storage_summary()`、`get_cargo_bay_summary()`、`get_carry_summary()`——各返回 `{total_capacity, used_capacity, stacks: [{id, qty, mass_class}]}` | UI 只读不写；不直接修改任何池内容 |
| **玩家知识与情报** | ❌ 未设计 | 情报物品过滤、消耗 | `get_carried_intel()`（过滤 `supply_class=intel`）、`consume_intel(intel_id)` | 情报系统拥有已知/未知状态；本系统只负责情报物品的持有和消耗 |
| **伙伴功能与关系** | ❌ 未设计 | 携带容量查询（用于注入加成） | `get_carry_capacity()` | 伙伴系统拥有关系状态；容量加成由伙伴系统通过本系统接口注入，而非本系统主动查询伙伴 |

### 依赖风险

| 风险 | 等级 | 说明 | 缓解 |
|------|------|------|------|
| 下游系统接口已在各系统 GDD 中完成双向对齐 | 🟢 低 | 所有 7 个下游系统 (#8, #11-#16) 均已设计并完成双向依赖标注 | 本 GDD 的 Interactions 表格和各操作接口已明确定义契约；下游系统设计时必须遵循这些接口签名 |
| 注册表资源域完整性 | 🟡 中 | 注册表的资源域条目（material_tags、supply_class 等）必须在本系统实现前完成填充，否则运行时查询无数据 | 注册表 GDD 要求在资源系统实现前完成资源域 Schema |
| 模块系统接口未锁定 | 🟡 中 | `飞艇模块与船体状态` 是重量适航的判定方，但其设计尚未开始。本系统假设模块系统接收 `get_total_loaded_mass()` 并自行决定超重后果 | 已在规则 12-13 中明确边界；模块系统设计时必须以本 GDD 的边界为输入 |

### 依赖图

```
内容数据与状态注册表 ──→ 资源、货物与容量 ──→ 飞艇模块与船体状态
本地存档与持久化    ──→                    ──→ 探索 / 搜撤场景
玩家移动与交互      ──→                    ──→ 战斗与威胁处理
                                          ──→ 世界修复与解锁
                                          ──→ 空港 / 村镇状态与集市交易
                                          ──→ UI / HUD / 航图界面
                                          ──→ 玩家知识与情报
                                          ──→ 伙伴功能与关系
```

### 双向依赖检查

| 关联系统 | 本 GDD 是否提及对方 | 对方 GDD 是否提及本系统 | 状态 |
|---------|-------------------|----------------------|------|
| 内容数据与状态注册表 | ✅ 规则 1-3、Interactions | ✅ 下游依赖中列出本系统 | 已对齐 |
| 本地存档与世界状态持久化 | ✅ 规则 19、Interactions、EC-06/07 | ✅ 快照包定义中引用 `progress.resources` | 已对齐 |
| 玩家移动与交互 | ✅ 规则 19、Interactions | ⚠️ 隐式对齐——移动系统广播 `use_requested` 给领域系统，本系统消费存储/拾取/货舱交互；移动 GDD 不显式列出本系统（它不提供空间锚点） | 已对齐（隐式） |
| 飞艇模块与船体状态 | ✅ 规则 12-13、Interactions | ✅ #8 Dependencies 已双向标注本系统 | 已对齐 |
| 探索 / 搜撤场景 | ✅ 规则 18、Interactions | ✅ #11 Dependencies 已双向标注本系统 | 已对齐 |
| 战斗与威胁处理 | ✅ Interactions (consume_in_combat) | ✅ #12 Dependencies 已双向标注本系统 | 已对齐 |
| 世界修复与解锁 | ✅ Interactions (commit_deposit) | ✅ #13 Dependencies 已双向标注本系统 | 已对齐 |
| 空港/村镇状态与集市交易 | ✅ Interactions (purchase) | ✅ #14 Dependencies 已双向标注本系统 | 已对齐 |
| 伙伴功能与关系 | ✅ Interactions (capacity query) | ✅ #15 Dependencies 已双向标注本系统 | 已对齐 |
| UI/HUD/航图界面 | ✅ Interactions | ✅ #16 Dependencies 已双向标注本系统 | 已对齐 |

## Tuning Knobs

### 容量调参

| 参数 | 默认值 | 安全范围 | 单位 | 影响的玩法感受 |
|------|--------|---------|------|---------------|
| `carry_base_slots` | 5 | 3–8 | 槽位 | 随身物品栏基础槽位数。太低→探索频繁被迫丢弃；太高→整备取舍失去意义 |
| `carried_base_slots` | 5 | 3–8 | 槽位 | 探索局内池（Pool 5）基础槽位数——探索中可携带的战利品上限。默认与随身物品栏一致，可单独调整以改变探索风险压力 |
| `storage_base_volume` | 1000 | 500–2000 | 容积 | 飞艇仓库基础容积。太低→返航后频繁溢出；太高→仓库永远不会满，整理无意义 |
| `cargo_bay_base_volume` | 0 | 0（固定） | 容积 | 货舱基础容积——必须为 0，由模块提供容量。改为正数会破坏模块系统的存在意义 |
| `cargo_module_volume_bonus` | 500 | 300–800 | 容积 | 货物/维修模块提供的货舱容积加成。太低→航线货物运输不划算；太高→单次航线收益过于丰厚 |

### 堆叠调参

| 参数 | 默认值 | 安全范围 | 单位 | 影响的玩法感受 |
|------|--------|---------|------|---------------|
| `max_stack_basic` | 99 | 50–999 | 个/堆 | 基础补给的堆叠上限。太高→补给管理失去粒度；太低→UI 被大量堆刷屏 |
| `max_stack_repair` | 99 | 50–999 | 个/堆 | 维修材料的堆叠上限。与 basic 同等对待，因为它们是探索中大量收集的主要材料 |
| `max_stack_navigation` | 20 | 10–50 | 个/堆 | 导航物品的堆叠上限。低于 basic/repair 以体现其特殊性和空间成本 |
| `max_stack_local_specialty` | 10 | 5–30 | 个/堆 | 地方特产的堆叠上限。低堆叠鼓励尽快使用/交易而非囤积 |
| `max_stack_intel` | 1 | 1（固定） | 个/堆 | 情报物品的堆叠上限——必须为 1（unique）。改为可堆叠会破坏"每件情报都是独特发现"的感觉 |

### mass_class 调参

| 参数 | 默认值 | 安全范围 | 单位 | 影响的玩法感受 |
|------|--------|---------|------|---------------|
| `volume_light` | 50 | 30–80 | 容积/堆 | 轻型物品的容积占用。影响"带大量轻型还是少量重型"的取舍 |
| `volume_medium` | 120 | 80–180 | 容积/堆 | 中型物品的容积占用。当前为容积效率最优（40.0），若需削弱中型优势可上调 |
| `volume_heavy` | 200 | 180–350 | 容积/堆 | 重型物品的容积占用。调低使 heavy 获得最优容积效率（≈33.3 vol/wt），以粗粒度换取高重量密度 |
| `weight_light` | 1 | 1–2 | 重量值 | 轻型物品的重量贡献。最小单位，作为重量基准 |
| `weight_medium` | 3 | 2–4 | 重量值 | 中型物品的重量贡献。约 3 倍于轻型，保持重量与容积的合理比例 |
| `weight_heavy` | 6 | 4–8 | 重量值 | 重型物品的重量贡献。约 6 倍于轻型，使超重成为重型货物航线中的真实约束 |

### 容积效率约束

| 约束 | 说明 |
|------|------|
| `volume_light / weight_light ≠ volume_medium / weight_medium ≠ volume_heavy / weight_heavy` | 三种 mass_class 的效率不应相同——必须有差异化 |
| `volume_light < volume_medium < volume_heavy` | 容积排序不可打破 |
| `weight_light < weight_medium < weight_heavy` | 重量排序不可打破 |
| 不同货物类型在不同约束维度下各有优势 | 避免某个 mass_class 在所有维度上都是最优或最劣 |

### 操作与状态调参

| 参数 | 默认值 | 安全范围 | 单位 | 影响的玩法感受 |
|------|--------|---------|------|---------------|
| `extraction_loss_ratio` | (由探索系统定义) | 0.0–1.0 | 比例 | 探索失败时 carried 资源的损失比例。0=无损失（探索无风险），1=全损失（过于惩罚）。本系统不拥有此参数，但消费其值来执行 `destroyed` 转移。**Pillar 4 约束（不可协商）**：探索系统设计损失公式时必须保证 (a) Q=1 的 unique 物品（max_stack=1）损失量为 0——unique 物品不可被探索失败完全摧毁；(b) 建议参照 EC-05 的 `loss = min(Q-1, max(1, ceil(Q×0.4)))` 模式，确保至少保留 1 单位。推荐范围 0.2–0.4：低于 0.2 时探索失败无足轻重；高于 0.4 时与游戏概念的"不做恶劣惩罚"冲突 |
| `unpack_cost_time` | 0（即时） | 0–3 | 秒 | 拆包操作的耗时。MVP 为即时。改为非零值增加拆包的操作成本——在飞艇上拆包变为有时限活动，影响探索后整理节奏 |
| `cargo_unpack_free` | true | true/false | 布尔 | 拆包是否免费。若改为 false，拆包需消耗资源或货币，增加货物贸易的策略深度 |

### 接口暴露调参（供下游系统使用）

| 参数 | 默认值 | 安全范围 | 单位 | 影响的玩法感受 |
|------|--------|---------|------|---------------|
| `carry_slot_bonus` | 0（预留） | 0–5 | 槽位 | 由背包物品或伙伴系统注入的随身槽位加成。本系统只暴露 `get_carry_capacity()` 包含此加成 |
| `carry_volume_bonus` | 0（预留） | 0–500 | 容积 | 由伙伴系统注入的随身容积加成（如宠物驮兽）。MVP 为 0 |
| `storage_volume_bonus` | 0（预留） | 0–2000 | 容积 | 由飞艇模块系统注入的仓库扩展加成。本系统只消费最终值 |

### 起始状态调参（Starting State）

| 参数 | 默认值 | 安全范围 | 单位 | 影响的玩法感受 |
|------|--------|---------|------|---------------|
| `starting_basic_supply_qty` | 10 | 5–20 | 个 | 新游戏仓库中 basic 补给数量。太低→首轮航线消耗后玩家被迫立即寻找补给；太高→初期无资源压力 |
| `starting_repair_kit_qty` | 4 | 2–8 | 个 | 新游戏仓库中 repair 材料数量。太低→首次修复机会被推迟；太高→修复决策失去取舍 |
| `cargo_module_preinstalled` | true | true / false | 布尔 | 货舱模块是否在 MVP 开局预装。false 仅在玩家选择"硬核"或"无模块开局"变体时使用 |

核心原则：起始状态提供最低存活基准——足够触发首轮闭环（Hub → Route → Explore → Return → Repair）但不跳过探索需求。具体数值在首次平衡性游戏测试后校准。

## Visual/Audio Requirements

### 容量条设计

容量条不是一个单调的纯色进度条，而是一个**堆叠构成可视化**——玩家一眼能看出池里放了什么、占比多少、还剩多少空间。

#### 分段堆叠条（Stack Composition Bar）

每段代表一个资源堆，段宽 = 该堆容积 / 总容积。颜色按 `mass_class` 区分：

| mass_class | 填充色 | 边框色 | 视觉特征 |
|------------|--------|--------|---------|
| `light` | 浅灰蓝 `#8AB4D6` | `#6A94B6` | 细密斜纹——暗示轻薄的包裹 |
| `medium` | 琥珀 `#C89850` | `#A87830` | 水平木纹——暗示标准货箱 |
| `heavy` | 深铁灰 `#5C6B73` | `#3C4B53` | 铆钉点纹——暗示重型机械部件 |

`unique` 物品额外叠加金色对角线闪光（缓慢动画，周期 3s），区别于可堆叠物品。

#### 悬停交互

鼠标悬停在某段上时：该段亮度提升 15%；弹出浮动 tooltip（物品名称、数量、mass_class、容积占用、堆叠情况 E/M）；其余段轻微变暗（透明度降到 60%），形成聚焦效果。

#### 悬停预览（运输预览）

玩家在仓库/货舱中点击选中一个物品堆，然后将鼠标悬停在目标池的"转移到此处"按钮（或目标池面板标题栏）上时，目标池的容量条尾部出现半透明幽灵段——预览转移完成后该物品将占据的位置。若容积足够：幽灵段以该物品 mass_class 的颜色 + 50% 透明度 + 虚线边框显示。若容积不足：整个容量条边框变红 + 抖动动画，幽灵段不显示。鼠标移开后幽灵段消失。确认转移通过点击目标池标签（随身/仓库/货舱面板标题栏）执行——与随身物品栏和仓库 UI 的转移确认方式一致。

> **注意**：MVP 不使用拖拽操作（与 `玩家移动与交互` GDD 规则 11 一致）。所有跨池转移通过点击选择 + 确认目标执行。悬停预览提供与拖拽预览相同的空间规划信息，但不依赖拖拽输入。

#### 阈值标记与警示

| 填充率 | 视觉效果 |
|--------|---------|
| 0–70% | 正常显示，无特殊效果 |
| 70–90% | 容量条右端出现细竖线标记，颜色 #888 |
| 90–95% | 标记变橙色，容量条整体有微弱呼吸式脉动（周期 2s，亮度 ±5%） |
| 95–100% | 标记变红，脉动加快（周期 1s），剩余空间段以红色虚线填充 |

#### 动画过渡

| 事件 | 动画 |
|------|------|
| 添加物品 | 新段从右端滑入并弹性缩放到最终宽度（ease-out-back，300ms） |
| 移除物品 | 对应段缩小消失，相邻段平滑填补空隙（ease-in-out，250ms） |
| 数量变更（合并/拆分） | 段宽平滑过渡到新宽度（ease-out，200ms） |
| 跨 session 加载 | 无动画——直接显示最终状态 |

#### supply_class 分解指示器（仓库/货舱容量条下方）

一排 5 个小标记（颜色 + 形状双重编码，符合 WCAG 2.1 SC 1.4.1），表示各 `supply_class` 在池中的容积占比。basic=灰 ● 圆，repair=蓝 ■ 方，navigation=青 ◆ 菱，local-specialty=紫 ▲ 三角，intel=金 ★ 星。悬停小标记显示该类别的总容积占用和堆数。

#### 货舱双条联动

货舱同时有容积条和重量条，上下排列。悬停预览时两个条同时显示幽灵段。当重量超过飞船载重上限时，重量条超限部分以红色 + 警告图标显示——但本系统不阻止装载（规则 13），只在条上标记超重警告线。

### 资源与货物图标

- 每个资源/货物需唯一图标（由注册表 `icon_path` 字段引用），在物品栏、仓库、货舱 UI 中以 64×64 像素显示
- 图标右下角叠加数量角标（stackable 资源 >1 时显示数字，unique 不显示）
- `handling_class` 在货物图标上以小型轮廓标记区分：木箱（crate）、圆桶（barrel）、捆扎（bundle）、布袋（sack）

### supply_class 边框色

| supply_class | 边框色 | 含义 |
|-------------|--------|------|
| `basic` | 灰 ● | 常见补给 |
| `repair` | 蓝 ■ | 功能材料 |
| `navigation` | 青 ◆ | 导航物品 |
| `local-specialty` | 紫 ▲ | 地方特产 |
| `intel` | 金 ★ | 情报物品 |

每种 supply_class 的边框色与形状组合提供冗余编码：色盲玩家可通过形状区分，低视力玩家可通过颜色区分。

`unique` 物品附加菱形背景区别于方形。

### 状态视觉反馈

- 货物未拆包时图标上叠加小锁图标
- `deposited` 确认对话框中，待提交物品以红色高亮闪烁一次再稳定显示
- 模块被摧毁时，货舱 UI 闪红并显示"货舱模块被摧毁！部分货物已损失，剩余货物散落在飞艇附近——修复模块后可回收。"通知横幅，持续至玩家关闭。世界中的可回收货箱以闪烁标记显示

### 音效需求

| 事件 | 音效方向 | 优先级 |
|------|---------|--------|
| 物品拾取（add_loot） | 短促清脆的收集音，按 mass_class 变化音高（light 高、heavy 低） | 高 |
| 物品转移（transfer） | 柔和的移动/放置音，区分 source→target 方向 | 中 |
| 货物装载（load to cargo bay） | 沉重的放置音，带轻微回响（飞艇货舱氛围） | 中 |
| 拆包（unpack_cargo） | 开箱/拆封音——木板碎裂或绳索解开，持续 0.5–1s | 高 |
| 容量满拒绝 | 低沉的"拒绝"提示音 | 中 |
| 提交修复（commit_deposit） | 确认音——钟声或金属共鸣，强调不可逆 | 高 |
| 模块摧毁 / 货物损失 + 货箱弹出 | 破碎音 + 下沉的低频嗡鸣（2s），后接货箱弹出的短促弹射音 | 高 |
| 货舱模块安装 | 机械锁定/卡扣音——短促有力 | 低 |
| 物品丢弃/销毁 | 轻微消散音 | 低 |

## UI Requirements

### 随身物品栏 UI

| 要素 | 规格 |
|------|------|
| 布局 | 水平槽位条，5 槽 + 加成槽（预留），每槽 72×72 像素 |
| 交互 | 点击选中槽位 → 弹出操作菜单：使用/转移/丢弃/取消。选择"转移"后，目标池标签高亮；悬停在目标池标签上时，目标池容量条显示悬停预览幽灵段（见悬停预览规格）；点击目标池标签确认并执行转移。点击仓库/货舱标签页可切换目标池 |
| 空槽 | 虚线边框，半透明背景 |
| 满槽 | 物品图标 + 数量角标（>1 时），悬停显示 tooltip：名称、supply_class、mass_class、堆叠数/max_stack |
| 容量条 | 槽位条下方：`[████░░] 4/5 槽` |
| 快捷键 | 数字键 1-5 对应槽位快捷使用 |

### 飞艇仓库 UI

| 要素 | 规格 |
|------|------|
| 布局 | 可滚动网格，每行一个资源堆（图标 + 名称 + 数量 + mass_class 标签 + 容积占用） |
| 排序 | 标题行可点击排序：按名称/数量/supply_class/mass_class |
| 筛选 | 顶栏标签页：全部 / basic / repair / navigation / local-specialty / intel |
| 交互 | 点击堆 → 操作菜单：转移/丢弃/拆分堆。选择"转移"后，目标池标签高亮；悬停在目标池标签上时，目标池容量条显示悬停预览幽灵段（见悬停预览规格）；点击目标池标签确认并执行转移 |
| 容量条 | 底栏：`[████████░░] 920/1000 容积` |
| 空状态 | 仓库为空时显示"仓库为空——从探索中带回材料或拆包货物来填充" |

### 货舱 UI

| 要素 | 规格 |
|------|------|
| 布局 | 类似仓库，但仅显示货物物品（非裸资源），每行显示货物名称 + mass_class + 容积占用 + 重量贡献 |
| 容量条 | 双重容量条：`容积 [████░░] 380/500` + `重量 [██░░░░] 11/25`（重量上限由飞船系统提供） |
| 无模块 | 货舱 UI 整体置灰，中央显示"未安装货舱模块——在飞艇模块界面安装货物模块以启用货舱" |
| 装载操作 | 在仓库中点击货物 → "装载到货舱"（或悬停在货物行上按快捷键）。悬停"装载到货舱"按钮时，货舱双条显示幽灵预览段 |
| 卸货操作 | 点击货舱中货物 → "卸回仓库" |
| 拆包操作 | 点击货舱中货物 → "拆包"（仅当仓库有足够容积时可用，否则置灰 + tooltip 显示所需容积） |
| 模块被摧毁通知 | 货舱 UI 顶部显示红色横幅"货舱模块被摧毁！部分货物已损失，剩余货物散落在飞艇附近——修复模块后可回收。"持续至玩家关闭。同时显示损失货物清单（名称 + 数量） |

### 拆包确认弹窗

| 要素 | 规格 |
|------|------|
| 触发 | 点击货舱中货物 → "拆包" |
| 内容 | "拆包 [货物名称] 将获得 [资源名称] ×[数量]，货物物品将被销毁。确定拆包？" |
| 空间提示 | 若仓库容积不足以接收全部拆包资源，显示"仓库空间不足——需要 [所需容积]，当前可用 [可用容积]"并禁用确认按钮 |

### 提交修复确认弹窗

| 要素 | 规格 |
|------|------|
| 触发 | 在修复节点点击"提交材料" |
| 内容 | 列出所有待提交资源（图标 + 名称 + 数量），每行显示红色警告标记。底部警告文字："提交后材料不可取回。确定提交？" |
| 确认 | 双按钮——"取消"（默认焦点）和"确认提交"（红色）。确认后不可撤销 |

### 容量满提示

| 场景 | UI 反馈 |
|------|---------|
| 拾取时随身满 | 物品上方弹出红色"随身物品已满"浮动文字（2s），物品保持可拾取状态 |
| 装载时货舱满 | 红色"货舱容积不足"浮动文字，高亮货舱容量条闪烁一次 |
| 存入时仓库满 | 红色"仓库容积不足"浮动文字，高亮仓库容量条闪烁一次 |
| 模块移除被拒 | 红色"请先卸下货舱中的货物"浮动文字 |

## Acceptance Criteria

### AC-RES-001: 资源身份与堆叠

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-001.1 | `stackable` 资源同 ID 合并到一个槽位，达 `max_stack` 后溢出到新槽 | 对容量尚有空槽的池执行 `add()` 多次同 ID stackable 资源，数量超过 max_stack | 溢出部分正确生成新堆；已有堆不超 max_stack |
| AC-RES-001.2 | `unique` 资源每件单独占一槽，`max_stack=1` | 对池执行 `add()` 多次同 ID unique 资源 | 每次添加占一个新槽；同 ID 不合并 |
| AC-RES-001.3 | 资源身份仅由稳定 ID 确定，不依赖显示名或文件路径 | 在测试中修改资源显示名后执行 `transfer()` | 转移成功，匹配基于 ID 而非名称 |

### AC-RES-002: 容量系统

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-002.1 | 随身物品栏基础 5 槽，每堆裸资源占 1 槽 | 向随身添加 5 种不同资源各 1 个，再尝试添加第 6 种 | 前 5 次成功，第 6 次返回 `ERR_CARRY_SLOTS_FULL` |
| AC-RES-002.2 | 飞艇仓库容积 1000，按 `mass_class` 的 volume 值占用 | 向仓库添加 light 物品直到 1000/1000，再尝试添加 medium 物品 | 最后一次添加返回 `ERR_TARGET_FULL`；已用容积计算准确 |
| AC-RES-002.3 | 货舱基础容积为 0（无模块），不可装载任何货物 | 不安装货物模块时尝试 `add(cargo_bay, ...)` | 返回 `ERR_CAPACITY_ZERO` |
| AC-RES-002.4 | 货舱安装模块后容积为 500，容积计算正确 | 安装模块后装载 2×heavy（200×2=400）+ light（50）= 450，再尝试添加 medium（120） | 450+120=570 > 500，返回 `ERR_TARGET_FULL` |

### AC-RES-003: 货物模型

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-003.1 | 货物只能存在于货舱，不能放入随身或仓库 | 尝试 `transfer(cargo_bay, carry, cargo_id, 1)` 和 `transfer(cargo_bay, storage, cargo_id, 1)` | 两次操作均失败；货物保留在货舱 |
| AC-RES-003.2 | `unpack_cargo()` 销毁货物物品，将其 `linked_resource_id` 的资源加入仓库 | 执行拆包操作后检查货舱和仓库状态 | 货舱中货物物品消失；仓库中出现对应资源且数量等于货物声明数量 |
| AC-RES-003.3 | 未拆包的货物不可用于修复、消耗或交易 | 在货物仍在货舱中时尝试 `consume()` 和 `commit_deposit()` 引用货物内部资源 | 操作失败，提示需先拆包 |

### AC-RES-004: 重量与载重

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-004.1 | `get_total_loaded_mass()` 仅计算货舱中货物的重量值 | 在货舱放入 2 light（2×1）+ 1 medium（3）+ 1 heavy（6），同时在仓库和随身也放入物品 | 返回 11；仓库和随身中的物品不计入 |
| AC-RES-004.2 | 本系统不阻止超重装载 | 装入超过飞船载重上限的货物 | 装载成功完成；`get_total_loaded_mass()` 正确返回超重值；无门控错误 |
| AC-RES-004.3 | 物品卸出货舱后重量值减少 | 装载 heavy 货物后记录质量，再卸载它 | 卸载后 `get_total_loaded_mass()` 减少 6 |

### AC-RES-005: 原子操作

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-005.1 | `add()` — stackable 时优先合并已有同 ID 堆 | 仓库中有 basic E=90（max_stack=99），`add(storage, basic_id, 30)` | merge_qty=9 合并→已有堆达 99；overflow_qty=21 需新容积；总数量正确 |
| AC-RES-005.2 | `add()` — 目标池满且无可合并堆时全操作失败 | 随身已满 5/5 槽，无同 ID 堆，`add(carry, new_resource_id, 1)` | 返回 `ERR_CARRY_SLOTS_FULL`；源和目标状态均未变更 |
| AC-RES-005.3 | `remove()` — 数量不足时全操作失败 | 仓库有 basic × 5，`remove(storage, basic_id, 10)` | 返回错误；basic 仍为 5；无部分移除 |
| AC-RES-005.4 | `transfer()` — 源不足时全操作失败 | 仓库 basic × 3，`transfer(storage, carry, basic_id, 5)` | 返回错误；仓库 basic 仍为 3；随身不变 |
| AC-RES-005.5 | `transfer()` — 支持拆分（qty < 源堆数量） | 仓库 basic × 50（一个堆），`transfer(storage, carry, basic_id, 20)` | 仓库保留 30；随身获得 20；操作成功 |
| AC-RES-005.6 | `consume()` — 同 `remove()` 语义 | `consume(storage, basic_id, 5)` 在仓库 basic × 10 时 | 仓库 basic 变为 5；消耗数量正确 |
| AC-RES-005.7 | 零数量操作返回成功无变更 | `add(storage, id, 0)` / `remove(storage, id, 0)` / `transfer(storage, carry, id, 0)` | 所有操作返回成功；各池内容不变 |
| AC-RES-005.8 | 负数量操作被拒绝 | `add(storage, id, -5)` / `remove(storage, id, -3)` | 返回 `ERR_INVALID_QUANTITY` |

### AC-RES-006: 状态机

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-006.1 | 资源不能同时在两个池中 | 执行 transfer 后检查源和目标池 | 源池中资源数量减少，目标池增加——总量守恒（非终态非销毁操作） |
| AC-RES-006.2 | `deposited` 为终态，不可取回 | 执行 `commit_deposit()` 后尝试 `transfer(repair_node, storage, id, qty)` | 操作失败；资源不可从修复节点移出 |
| AC-RES-006.3 | `deposited` 操作需 UI 确认 | 触发 deposit 流程 | UI 显示确认对话框，包含资源列表和不可逆警告；取消不执行提交 |
| AC-RES-006.4 | `destroyed` 为终态 | 资源被消耗后尝试查询 | 资源不再出现在任何池中 |
| AC-RES-006.5 | 跨池转移必须通过原子 `transfer` 原语 | 领域系统代码审查 | 所有跨池移动使用 `transfer()`；无直接修改池内容的旁路 |

### AC-RES-007: 拆包验证

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-007.1 | 仓库有匹配堆且可合并时，拆包不消耗新容积 | 仓库 basic E=90（max_stack=99），拆包含 Q=5 basic 的货物 | 拆包成功；仓库容积使用不变；basic 堆变为 95 |
| AC-RES-007.2 | 仓库满且无可合并堆时，拆包失败 | 仓库已用 1000/1000，拆包含新资源的货物 | 返回 `ERR_STORAGE_FULL`；货物保留在货舱；仓库不变 |

### AC-RES-008: 持久化

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-008.1 | 资源状态正确保存和恢复 | 在仓库/货舱/随身中放入不同资源，保存后加载 | 所有池中资源 ID、数量、堆结构完全一致 |
| AC-RES-008.2 | 快照仅含稳定 ID + 数量 + 池归属 | 检查保存文件中的 `progress.resources` 字段 | 不包含显示名、模型引用、运行时指针；仅有 ID/数量/池/槽位索引 |
| AC-RES-008.3 | 探索中途不产生存档 | 进入探索点后直接退出游戏（不撤离），重新加载 | 资源状态回到进入探索前（loaded 或 in_storage） |

### AC-RES-009: 供给类别

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-009.1 | `intel` 类物品 max_stack=1（unique） | 尝试 `add(carry, intel_id, 5)` | 每个 intel 占独立槽位；不堆叠；同 ID 不合并 |
| AC-RES-009.2 | `navigation` 类物品 max_stack=20 | 添加 25 个 navigation 资源到有空槽的池 | 一个堆 20 + 一个堆 5；不会出现单堆 25 |
| AC-RES-009.3 | `get_carried_intel()` 仅返回 `supply_class=intel` 的物品 | 随身中放入 basic、repair、intel 各一个 | 查询只返回 intel 物品 |

### AC-RES-010: 模块与货舱互动

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-010.1 | 货舱有货物时不可移除模块 | 货舱装载任意货物后，尝试通过模块 UI 移除货物模块 | 操作被拒绝；提示"请先卸下货舱中的货物" |
| AC-RES-010.2 | 货舱清空后可正常移除模块 | 卸下全部货物后，移除货物模块 | 移除成功；货舱容积回到 0 |
| AC-RES-010.3 | 模块被战斗摧毁时货物部分损失 + 可回收货箱 | 模拟模块摧毁事件（货舱中有 5 堆不同货物），检查资源状态 | 约 40% 货物（至少 1 堆）进入 `destroyed`；剩余变为 `recoverable_crate` 临时状态；货舱容积归零；玩家收到损失通知 + 回收提示 |

### AC-RES-011: 接口契约

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-011.1 | `get_storage_summary()` 等 UI 查询只读不写 | 多次调用查询接口后检查池内容 | 池内容不变 |
| AC-RES-011.2 | `can_deposit()` 不产生副作用 | 多次调用 `can_deposit()` 后检查资源状态 | 资源数量不变；未发生任何转移 |
| AC-RES-011.3 | 领域系统只能通过本系统原语操作资源 | 代码审查 + 接口测试 | 无直接访问资源池内部数据结构的代码路径 |

### AC-RES-012: 信号契约

| ID | 验收条件 | 验证方法 | 通过标准 |
|----|---------|---------|---------|
| AC-RES-012.1 | 信号在操作成功后触发——`add()` | 连接 `resource_added` 和 `pool_changed` 信号监听器；`add(storage, basic_id, 5)` | `resource_added` 触发 1 次，参数为 `(storage, basic_id, 5)`；`pool_changed` 触发 1 次，参数为 `(storage)` |
| AC-RES-012.2 | 信号在操作成功后触发——`remove()` | 连接 `resource_removed` 信号监听器；仓库有 basic×10，`remove(storage, basic_id, 5)` | `resource_removed` 触发 1 次，参数为 `(storage, basic_id, 5)` |
| AC-RES-012.3 | 信号在操作成功后触发——`transfer()` | 连接 `transfer_completed` 和 `pool_changed` 信号；`transfer(storage, carry, basic_id, 5)` | `transfer_completed` 触发 1 次，参数为 `(storage, carry, basic_id, 5)`；`pool_changed` 触发 2 次（源池 + 目标池各一次） |
| AC-RES-012.4 | `cargo_unpacked` 携带正确参数 | 连接 `cargo_unpacked` 信号；`unpack_cargo(cargo_slot)` 其中货物 Q=30, linked_resource_id="resource.basic_supply" | `cargo_unpacked` 触发 1 次，参数为 `(cargo_id, "resource.basic_supply", 30)` |
| AC-RES-012.5 | `deposit_committed` 携带正确参数 | 连接 `deposit_committed` 信号；`commit_deposit(repair_node_id, costs)` | `deposit_committed` 触发 1 次，参数含 `repair_node_id` |
| AC-RES-012.6 | `mass_changed` 在货舱变更后触发 | 连接 `mass_changed` 信号；添加 1 个 heavy 货物到货舱（weight=6） | `mass_changed` 触发 1 次；`new_mass` 等于添加货物后 `get_total_loaded_mass()` 的返回值 |
| AC-RES-012.7 | 信号在操作失败后不触发 | 连接所有 7 个信号监听器；`add(carry, unknown_id, 5)` 到随身 5/5 满且无匹配堆 | 操作返回错误；所有 7 个信号触发 0 次 |
| AC-RES-012.8 | 信号触发顺序——操作信号先于 `pool_changed` | 在 `transfer()` 操作时记录信号触发顺序列表 | `transfer_completed` 在 `pool_changed`（源）和 `pool_changed`（目标）之前触发 |
| AC-RES-012.9 | 重入防护——信号回调中调用变更方法返回 `ERR_BUSY` | 在 `resource_added` 信号回调中调用 `add(storage, basic_id, 5)` | `add()` 返回 `ERR_BUSY`；原操作不受影响；`resource_added` 信号回调中不会触发新的 `resource_added` |
| AC-RES-012.10 | 信号回调中可安全调用查询方法 | 在 `resource_added` 信号回调中调用 `get_storage_summary()` | 查询成功返回数据；不触发 `ERR_BUSY` |
| AC-RES-012.11 | emit-after-mutation——信号触发时状态已完整 | 在 `pool_changed` 回调中调用 `get_storage_summary()` | 返回的状态反映已完成的变更——新资源已出现在查询结果中（非操作前的旧状态） |
| AC-RES-012.12 | `discard()` 触发 `resource_removed` 信号 | 连接 `resource_removed` 信号；`discard(carry, basic_id, 3)` 从随身移除 3 个 basic | `resource_removed` 触发 1 次，参数为 `(carry, basic_id, 3)`；`pool_changed` 触发 1 次 |

## Open Questions

| # | 问题 | 影响范围 | 建议在哪个下游系统设计时决定 |
|---|------|---------|--------------------------|
| OQ-01 | 玩家如何丢弃物品？是否需要专门的"丢弃区"或直接在物品栏中操作？ | 随身/仓库 UI 交互 | 玩家移动与交互 |
| OQ-02 | 物品是否有耐久度或品质等级？`unique` 物品是否有变体？ | 资源 Schema 扩展 | 内容数据与状态注册表 |
| OQ-03 | 仓库是否需要"快速堆叠到已有堆"的一键整理功能？ | 仓库 UI | UI / HUD / 航图界面 |
| OQ-04 | 货物是否可以堆叠？同 ID 货物在货舱中是否合并为一个堆？ | 货舱容量计算 | 本系统——当前设计为每件货物独立占容积，若需堆叠需修订规则 5-7 |
| OQ-05 | 多个货物模块是否叠加容积？上限是多少？ | 模块系统设计 | 飞艇模块与船体状态 |
| OQ-06 | 探索中能否从随身物品栏直接使用物品？（如消耗 navigation 物品降低航线风险） | 探索互动设计 | 探索 / 搜撤场景 |
| OQ-07 | 集市交易是否支持以物易物，还是统一使用某种货币？ | 交易系统设计 | 空港 / 村镇状态与集市交易 |
| OQ-08 | `local-specialty` 物品的价格是否随距离产地远近而变化？ | 经济系统深度 | 空港 / 村镇状态与集市交易 |
