# Review Log: 资源、货物与容量

## Review — 2026-04-29 — Verdict: APPROVED (Lean re-review — 0 blockers)

Scope signal: L
Specialists: None (lean mode — per creative director recommendation after 4 full adversarial rounds)
Blocking items: 0 | Recommended: 3 | Prior blockers resolved (all rounds): 26

Summary: Fifth and final review pass. All 26 blockers from Rounds 1-4 + economy-designer focused review are resolved. The GDD's core architecture — dual capacity model (slot + volume), six-pool state machine, mass_class/weight system, atomic operations, supply_class differentiation, and 7-signal contract — is coherent, complete, and implementation-ready. Three minor recommendations: (R1) volume_medium tuning knob description incorrectly claims optimal volume efficiency when heavy is actually optimal — cosmetic fix; (R2) discard confirmation flow could use a dedicated EC for consistency with EC-13; (R3) economy-designer's base_value anchor concern remains deferred to registry system per creative director's classification. None are blocking. Creative director's Round 4 assessment confirmed: GDD is APPROVED.

Prior verdict resolved: Yes (26 prior blockers from 5 review rounds — all resolved or downgraded to recommended)

---

## Review — 2026-04-29 — Verdict: NEEDS REVISION (6 blockers resolved inline)

Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, ux-designer, godot-gdscript-specialist, creative-director (7 agents)
Blocking items: 6 (all resolved) | Recommended: 6 | Total issues found: ~50 across all specialists

Summary: The GDD has strong bones — slot/volume dual-capacity model, six-pool state architecture, mass_class/weight system, and atomic operation primitives form a coherent resource contract. Creative Director identified 5 blocking issues plus 1 cross-document contradiction (drag-and-drop vs approved movement GDD). All 6 blockers were resolved in the same session: EC-05 softened to partial loss + recoverable crates, starting state defined with pre-installed cargo module and minimum supplies, cargo creation delegated to market system, four formula/schema bugs fixed, Pool 1 on_person state added to state machine, and all drag-and-drop references removed. Additional recommended revisions (mass_class efficiency tuning, AC coverage gaps, colorblind accessibility, basic route consumption deferral) remain for future consideration.

Prior verdict resolved: First review

## Review — 2026-04-29 — Verdict: NEEDS REVISION (6 new blockers resolved inline)

Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, ux-designer, godot-gdscript-specialist, creative-director (7 agents)
Blocking items: 6 (all resolved) | Recommended: 13 | Total issues found: ~35 across all specialists

Summary: Re-review after prior 6 blockers resolved. Six new blocking issues identified: B1 mass_class efficiency math (heavy had identical vol/wt ratio to light with worse granularity — heavy volume adjusted 300→200 to give each class distinct advantage), B2 transfer_validation false positive for multi-stack overflow (formula only checked 1 slot/volume instead of ceil(overflow/max_stack) stacks), B3 EC-05 single-stack cargo 100% loss (refined from stack-level to per-stack Q-proportional loss with minimum retention guarantee), B4 cargo resource quantity Q undefined (added as immutable cargo item attribute set by market system), B5 supply_class color-only accessibility violation (added shape coding: circle/square/diamond/triangle/star), B6 transfer confirmation UI inconsistency across 3 specs (unified to select→hover preview→click target to confirm). All 6 resolved in same session. 13 recommended items (signal architecture, AC coverage gaps, UX polish, loadout presets, unique item volume, etc.) deferred.

Prior verdict resolved: Yes (6 prior blockers resolved; 6 new blockers from deeper specialist analysis)

## Review — 2026-04-29 — Verdict: NEEDS REVISION (Light — 5 blockers, all resolved inline)

Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, ux-designer, godot-gdscript-specialist, creative-director (7 agents)
Blocking items: 5 (all resolved) | Recommended: 14

Summary: Round 3 caught implementation-correctness issues that survived the first two rounds. B1: heavy volume 300→200 fix from round 2 didn't propagate to formula tables (lines 200, 306 — stale values). B2: EC-05 rewrite introduced Q=1 formula contradiction (loss+retention > Q) plus floor/ceil inconsistency between lines 383-384 — fixed with `loss = min(Q-1, max(1, ceil(Q×0.4)))` and unified retention = Q-loss. B3: resource system had zero state-change signals despite being a bottleneck for 8 downstream systems — added 7-signal contract + EC-23 emission order/re-entrancy rules. B8: EC-02 add_loot targeted on_person (Pool 1) instead of carried (Pool 5), bypassing exploration extraction loss and violating Pillar 4 — corrected pool target. B9: Pool 5 (carried) capacity never defined — added 5-slot default + tuning knob. All 5 blockers resolved in this session. 14 recommended items (signal re-entrancy edge case, discard mechanic, merge-stacks operation, formula variable table completeness, UX polish items, AC coverage gaps) deferred for implementation or downstream GDDs.

Prior verdict resolved: Yes (6+6+5=17 prior blockers resolved across rounds 1-3; 7 new blockers from round 4 specialist analysis)

## Review — 2026-04-29 — Verdict: NEEDS REVISION (Moderate — 7 blockers, all resolved inline)

Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, ux-designer, ui-programmer, godot-gdscript-specialist, creative-director (8 agents)
Blocking items: 7 (all resolved) | Recommended: 14 | Total issues found: ~45 across all specialists

Summary: Round 4 caught implementation-correctness issues that survived the first three rounds because they cluster at cross-system boundaries. Key patterns: (1) Interface Gap — most blockers are handoff points between this system and downstream systems (route consumption pool, exploration loss formula, discard operation); (2) Formula Propagation Decay — the add_loot multi-stack overflow bug is the third instance of a formula fix not propagating to all locations (same bug class as Round 2 B2 in transfer_validation); (3) UI vs Model Blur — several "blocking" items were UI implementation concerns downgraded to recommended by creative-director. The core design — dual capacity model, six-pool state architecture, mass_class/weight system, atomic operations, supply_class differentiation — is coherent and well-specified. The creative-director assessed that the GDD is one targeted revision away from APPROVED; no architectural redesign is needed.

7 blockers resolved: (1) add_loot formula changed from slot_availability(carry, 1) to slot_capacity_check(carry, Q, max_stack); (2) discard operation added to operations table with confirmation requirement; (3) Pillar 4 non-negotiable constraint added: unique items (max_stack=1) must have 0 loss on exploration failure; (4) stack_merge merge priority defined: fullest-stack-first with lowest-index tiebreaker; (5) route consumption source pool specified as in_storage (Pool 2); (6) 航行与路线风险 added to Interactions table with consume_for_route() interface; (7) AC-RES-012 added with 12 signal contract acceptance criteria covering emission order, parameter correctness, re-entrancy, and emit-after-mutation.

14 recommended items deferred: base_value→registry, UX polish (capacity bar, transfer flow, WCAG colors), carry/carried asymmetry, state table polish, signal storm debouncing, keyboard accessibility, ERR_BUSY guard scope, etc.

Prior verdict resolved: Yes (6+6+5=17 prior blockers resolved across rounds 1-3; 7 new blockers from round 4 creative-director synthesis)

## Review (Round 4) — 2026-04-29 — `[economy-designer]` 经济设计专项审查

Scope signal: L (focused on economic layer only)
Specialist: economy-designer (1 agent)
Blocking items: 2 | Recommended: 5 | Observations: 8

Summary: Round 4 is the first dedicated economy-designer review. The GDD's resource architecture is solid — dual-capacity model, six-pool state machine, and atomic primitives form a coherent economic substrate. However, the GDD defines the resource *plumbing* without fully specifying resource *flows*. The faucet/sink balance has a structural gap: there is no defined recurring sink to prevent long-term resource accumulation. The supply class hierarchy creates real economic differentiation but the value of navigation and local-specialty items depends entirely on undesigned downstream systems. The cargo model creates a meaningful trade layer but the market system's pricing authority plus the absence of base resource values creates a value vacuum. The capacity numbers are well-calibrated for MVP scope. Starting economy is placeholder-correct but unvalidatable. Loss mechanics are elegantly tuned for "gentle pressure." Two blocking issues identified: (B1) absence of any base value anchor for resources, and (B2) undefined route consumption rate preventing faucet/sink balance validation.

---

### 1. [economy-designer] 资源 Faucet/Sink 分析

#### Faucet 清单（资源进入玩家经济的入口）

| Faucet | 类型 | 频率 | 量级（已知/未知） | 拥有者 |
|--------|------|------|------------------|--------|
| 探索战利品（`add_loot`） | 变量收益（探索生成） | 每次探索 1-N 次拾取 | 未知——由探索系统定义 | 探索 / 搜撤场景 |
| 集市购买 | 确定性收益（购买） | 每次访问集市 | 未知——由集市系统定义 | 空港 / 村镇状态与集市交易 |
| 起始补给（`starting_basic_supply_qty` + `starting_repair_kit_qty`） | 一次性注入 | 新游戏 1 次 | 10 basic + 2 repair（已知） | 本系统 |
| 货物拆包（`unpack_cargo`） | 确定性收益（拆包） | 取决于购买行为 | Q 由集市系统设定 | 本系统（执行）+ 集市系统（创建） |

#### Sink 清单（资源离开玩家经济的出口）

| Sink | 类型 | 频率 | 量级（已知/未知） | 拥有者 |
|------|------|------|------------------|--------|
| 修复节点提交（`commit_deposit`） | 一次性不可逆消耗 | 每个修复节点 1 次（MVP: 1 节点） | 未知——由世界修复系统定义 | 世界修复与解锁 |
| 航线消耗（`consume` for basic） | 重复性消耗 | 每次出航 | **未定义**——仅有 "航线消耗" 提及，无数量/公式 | 航行与路线风险（未设计） |
| 战斗消耗（`consume_in_combat`） | 条件性消耗 | 战斗发生时 | **未定义**——仅有接口，无消耗量 | 战斗与威胁处理（未设计） |
| 探索失败损失（`extraction_loss_ratio`） | 概率性损失 | 探索失败时 | 未知——由探索系统定义 | 探索 / 搜撤场景（未设计） |
| 模块摧毁货物损失（EC-05） | 灾难性损失 | 战斗摧毁模块时 | 40% Q 损失（已知公式） | 本系统 |
| 集市出售 | 玩家主动变卖 | 取决于玩家 | 未知——由集市系统定义 | 空港 / 村镇状态与集市交易 |
| 情报消耗（`consume_intel`） | 一次性消耗 | 每次解锁知识 | 1 个 intel 物品 | 玩家知识与情报 |
| 版本迁移截断（EC-08/EC-09） | 罕见一次性损失 | 版本更新时 | 极小 | 本系统 |

#### 结构性分析

**核心问题：本 GDD 在 MVP 范围内缺乏结构性循环 Sink。** 详细分析：

1. **修复节点是 MVP 中唯一确定的一次性 Sink**，但 MVP 只有 1 个修复节点（系统索引限定）。修复完成后，这个 Sink 永久关闭。之后资源只有入口没有结构性出口。

2. **航线消耗是设计意图中最自然的循环 Sink**——每次出航消耗 basic 补给——但本 GDD 和所有已设计系统中均未定义消耗量和消耗公式。`supply_class` 表格将 basic 的用途列为 "航线消耗、轻量维修"，但 GDD 中没有 `consume_for_route()` 接口，Interactions 表格中也没有与 `航行与路线风险` 系统的航线消耗接口。

3. **战斗消耗同样未定义**。有 `consume_in_combat` 接口但无消耗量。MVP 只有 1 种威胁类型和 "damaged / knocked back / retreat" 三种结果——战斗消耗可能非常轻微或不存在。

4. **探索失败损失是概率性的、可避免的**——它不是稳定 Sink，而是风险缓冲。技术娴熟的玩家可以基本避免此损失。

5. **集市出售**在 MVP 中是 "fixed or repair-flag-driven stock changes"——没有价格模拟或供需模型。玩家出售资源的能力和收益完全未定义。

**结论**：如果航线消耗未定义或过于轻微，MVP 经济模型将演变为纯积累模型——玩家持续从探索中获得资源，唯一的结构性出口是 1 个一次性修复节点。在 MVP 的有限内容范围内（可能 3-5 个 session），这不会造成严重问题，但它是经济设计中的结构性空白。本 GDD 至少应：
- 在 Interactions 表格中增加与 `航行与路线风险` 系统的航线消耗接口
- 声明航线消耗的基本约束（如 "每次出航固定消耗 basic × N，N 由航线风险等级决定"）
- 将此列为 Open Question 或明确标注为下游系统责任

**判定**：🟡 推荐项 R1——航线消耗接口缺失。在 `航行与路线风险` GDD 设计前，本 GDD 无法验证 Faucet/Sink 平衡。建议在 Interactions 表格和 Open Questions 中明确标注此依赖。

---

### 2. [economy-designer] Supply Class 经济平衡

#### 5 类供给的经济权重分析

| supply_class | max_stack | 槽位效率（相对） | 经济角色 | 差异化风险 |
|-------------|-----------|-----------------|---------|-----------|
| `basic` | 99 | 1.00× 基准 | 航线消耗品、轻量维修——**必需消耗品** | 低风险——消耗性确保持续需求 |
| `repair` | 99 | 1.00× 基准 | 世界修复、模块制作——**必需推进材料** | 低风险——修复消耗确保持续需求 |
| `navigation` | 20 | 0.20× basic（5 倍槽位成本） | 降低航线风险、辅助情报揭示——**优化品** | 🟡 中风险——若风险降低效果不够显著，玩家忽略 |
| `local-specialty` | 10 | 0.10× basic（10 倍槽位成本） | 高价值交易——**经济品** | 🟡 中风险——若交易收益不显著优于直接拾取 basic，玩家忽略 |
| `intel` | 1 | 0.01× basic（个体占 1 槽） | 解锁永久知识——**独特品** | 低风险——永久知识解锁动机强，且不可堆叠意味着"带就带了" |

#### 经济行为预测

**basic 和 repair 会自然形成经济基础**：因为它们同时是消耗品和推进材料，有内在需求驱动。max_stack=99 使它们成为"大宗商品"——玩家可以大量携带、大量消耗。这是正确的设计。

**navigation 的经济可行性取决于一个关键参数**：航线风险降低的边际收益是否值得 5 倍槽位成本。如果 navigation 只能减少 10% 的航线风险，而玩家可以通过更好的航线选择规避风险，navigation 就是背包里的死重。本 GDD 无法回答这个问题——答案在 `航行与路线风险` GDD 中。

**local-specialty 的经济可行性取决于两个未设计的系统**：（1）集市系统的定价逻辑——特产必须确实"高价值"；（2）村镇需求系统——特产必须与特定地点的需求匹配。如果特产只是在任意集市以略高价格出售，那就是 basic 的变体而非独特经济层。

**intel 的经济设计最完善**：unique、不可堆叠、一次性消耗解锁永久知识——这是教科书式的经济学设计。唯一小瑕疵：EC-17 指出 10 件 light intel 物品消耗 500 容积（基础仓库的一半）。这个容积代价是否合理？intel 是"情报信匣"（light），不是大型货物。但 50 容积的情报纸在物理上感觉偏大。建议考虑：intel 物品是否应该是特殊的"无容积"档案物品，或者至少是独立的 `volume` 值（如 10 而非 50）？不过 OQ-02 提到了 "物品是否有耐久度或品质等级？" —— intel 的容积可以在此讨论。

#### 差异化判断

当前设计**创造了有意义的经济差异化**——五类供给在经济角色、空间成本和边际收益上有明确梯度。这不是"玩家只会囤 basic/repair"的问题——恰恰相反，basic/repair 是经济基础层，navigation/specialty/intel 是经济优化层。但优化层的可行性完全取决于下游系统的数值设计。

**判定**：🟢 无阻塞问题。差异化设计正确。但 navigation 和 local-specialty 的经济可行性需要在对应的下游 GDD 中验证。建议在 Open Questions 中增加一项：navigation 的风险降低效果和 local-specialty 的价格倍率应达到什么水平才能确保经济可行性。

---

### 3. [economy-designer] Cargo Economy 深度分析

#### 货物模型的价值链

```
集市创建货物（设定 Q + mass_class） → 玩家购买 → 装载到货舱 → 运输到目的地 → 拆包 → 资源进入仓库
```

这个模型创造了一个有意义的经济层——货物是"打包的价值"，运输是增值过程。但当前设计有几个未解决的张力：

#### mass_class 的"效率"悖论

当前容积效率（volume/weight）：
- light: 50.0 — 每单位重量占用最多容积（"低效"）
- medium: 40.0 — 中等
- heavy: ≈33.3 — 每单位重量占用最少容积（"高效"）

Tuning Knobs 说这是有意的差异化——"不同货物类型在不同约束维度下各有优势，避免单一最优解"。但让我们测试这个说法：

**场景 1：重量是主导约束**（飞船载重上限紧张）
- heavy 优势：2 heavy = 400 容积 / 12 重量，如果价值与 Q 成正比，每重量单位运输最多货物
- light 劣势：10 light = 500 容积 / 10 重量，重量相近但容积占用更多

**场景 2：容积是主导约束**（货舱小，重量宽松）
- heavy 劣势：2 heavy = 400 容积 / 12 重量——但还剩 100 容积只能装 2 light 或 0 medium
- light 优势：10 light = 500 容积 / 10 重量——最大化利用容积

**场景 3：Q（货物内资源数量）与 mass_class 的关系未定义**
- 如果 heavy 货物总是包含更多 Q（例如 heavy=100, medium=60, light=30），那么 heavy 在所有维度上都占优
- 如果 Q 与 mass_class 无关（集市随机设定），那么玩家选择基于容积/重量约束
- **本 GDD 明确说 "本系统不设定也不验证 Q 值"**——这是关键集成风险

#### 关键发现：Q 值的经济意义

货物模型的核心经济变量是 Q（货物包含的资源数量），但它完全由集市系统控制。这意味着：
1. 货物的"价值密度"（每容积/重量的资源数量）不由本系统决定
2. mass_class 的效率优势只在 Q 值相同时成立——如果 heavy 货物的 Q 是 light 的 4 倍，heavy 在所有维度上都是最优选择
3. 集市系统可能（也可能不）为不同 mass_class 设置合理的 Q 值梯度

**判定**：🟡 推荐项 R2——货物 Q 值与 mass_class 的经济关系未定义。建议在本 GDD 的 Tuning Knobs 或 Open Questions 中声明 Q 值的推荐约束（如 "heavy 货物的 Q 建议为 light 的 N 倍，以平衡容积效率差异"），作为集市系统的设计输入。否则集市系统可能无意中使某个 mass_class 成为单一最优解。

#### 货物堆叠问题

OQ-04 问到 "货物是否可以堆叠？同 ID 货物在货舱中是否合并为一个堆？" 这是一个重要经济问题：
- 如果货物不堆叠：每个购买的货物独立占容积——每次买 5 个 light 货物 = 5×50 = 250 容积
- 如果货物可堆叠：同 ID 货物合并——10 个同 ID light 货物理论上可堆叠为 1 堆

当前设计倾向于"不堆叠"（每件货物独立），这与"货物是物理包装"的叙事一致。但这也意味着货舱容积是"货物件数"的硬约束，而非"货物内容"的约束——你买的包装方式比买的内容更重要。这在经济上是奇怪的：两个相同的 light 货物占 100 容积，但拆包后两个 light 资源可能合并为 1 堆占 50 容积。

**判定**：🟢 无阻塞问题。OQ-04 已标记此问题。建议在回答 OQ-04 时考虑：货物的不可堆叠性是否会创造"购买策略博弈"（买大包装 vs 买多个小包装）？

---

### 4. [economy-designer] 容量作为经济约束

#### 三个容量池的压力测试

**5 槽随身物品栏（Pool 1 — on_person）**：

假设玩家在探索前将物品从随身选入局内池（Pool 5 — carried）：
- 典型探索负载：1 navigation 物品 + 1 repair 材料 = 2/5 槽
- 剩余 3 槽用于战利品——可拾取 3 种不同资源（或同种资源的不同堆）

在探索中：
- Pool 5（carried）另有 5 槽——用于局内战利品
- 理论最大战利品种类：5 种（如果全部不同）或 5×99=495 个同种 basic

对 30-90 分钟的 session，5 槽随身 + 5 槽局内是合理的探索约束。从 Pool 5 回到飞艇后全部归入仓库，玩家在下次探索前重新整备——这创造了自然的"返航整理"循环。

**潜在挫败点**：EC-22 描述的场景——随身已满进入探索，拾取失败。但 OQ-01（丢弃功能）和 OQ-03（一键整理）都未解决。如果玩家不能方便地丢弃低价值物品来腾出空间，5 槽约束会从"有意义的取舍"变成"烦人的库存管理"。

**1000 容积飞艇仓库（Pool 2 — in_storage）**：

对 MVP 来说非常充裕。MVP 只有 1 个探索点、2 条航线、1 个修复节点。以每次探索带回 100-200 容积的战利品计，仓库可以容纳 5-10 次探索的收获。对 3-5 个 session 的 MVP 体量，1000 容积不太可能成为瓶颈。

但调参范围（500-2000）表明设计者也意识到这可能太宽松——如果仓库永远不会满，"整理"就失去了意义。500 的下限会创造更频繁的容量决策。

**500 容积货舱（Pool 3 — loaded）**：

这是设计最精确的池。500 容积创造明确的货物组合决策：
- 2 heavy + 1 light (2×200 + 50 = 450) — 最大化重量效率
- 4 medium + 1 light (4×120 + 50 = 530 > 500 — 不行，4 medium = 480 + 1 light = 530) → 3 medium + 1 light (360 + 50 = 410) 或 4 medium (480)
- 10 light (10×50 = 500) — 最大化灵活性

对于跑商玩法，500 容积意味着每次航线可以携带有限种类的货物。这是好的——它创造了"带什么去"和"在哪买/卖"的策略空间。

**判定**：🟢 无阻塞问题。三个容量池的数值对 MVP 体量合理。建议：(1) 在 OQ-01 中优先解决丢弃功能——没有丢弃，5 槽随身约束可能产生挫败感；(2) 考虑在仓库首次达到 70%/90% 容量时触发提示或引导，强化"整理"的意义。

---

### 5. [economy-designer] 损失机制作为经济排水

#### EC-05 公式深度分析

`loss = min(Q-1, max(1, ceil(Q × 0.4)))`

| Q | loss | retention | 实际损失率 | 心理影响 |
|---|------|-----------|-----------|---------|
| 1 | 0 | 1 | 0% | 安全感——"单件不会全毁" |
| 2 | 1 | 1 | 50% | 痛感——但还剩 1 件 |
| 3 | 1 | 2 | 33% | 温和 |
| 5 | 2 | 3 | 40% | 接近标称值 |
| 10 | 4 | 6 | 40% | 标称值 |
| 20 | 8 | 12 | 40% | 标称值 |
| 50 | 20 | 30 | 40% | 标称值 |
| 100 | 40 | 60 | 40% | 标称值 |

这个公式是优秀的损失经济学设计：
- **小堆保护**：Q=1 零损失——单件 cargo 不会完全蒸发。这避免了"运气不好全没了"的挫败感。
- **首件保护**：`min(Q-1, ...)` 确保 Q≥2 时至少保留 1 件——即使在小堆上损失也不超过 Q-1。
- **大堆按比例**：Q≥5 时收敛到约 40%——大堆上的损失是可预测的。
- **可回收货箱**：保留部分 + 可回收货箱将"灾难"转化为"后续任务"——损失是有方法补救的。

#### extraction_loss_ratio（探索失败）

本 GDD 不拥有此参数——它由探索系统定义。但本 GDD 消费它来执行 `carried → destroyed` 转移。设计约束：
- 游戏概念明确说 "失败应以教育性损失为主，不做恶劣惩罚"
- Pillar 4 要求 "未知带来温和压力"
- 建议范围：0.2-0.4（20%-40% 损失），与 EC-05 保持一致

**判定**：🟢 无阻塞问题。EC-05 公式是教科书级的"温和压力"损失设计。建议在 Interactions 表格中增加与 `探索 / 搜撤场景` 系统的契约项：`extraction_loss_ratio` 的建议范围和 Pillar 4 一致性约束。

---

### 6. [economy-designer] 起始经济分析

`basic_supply × 10 + repair_kit × 2`

#### 能否支撑首轮闭环？

首轮闭环（Hub → Route → Explore → Return → Repair）需要的资源消费：

| 步骤 | 需要的资源 | 当前 GDD 是否定义量 | 未知项 |
|------|-----------|-------------------|--------|
| Route（出航） | basic 航线消耗 | ❌ 未定义——本 GDD 无航线消耗接口 | 航线消耗量 N（每次出航 basic × ?） |
| Explore（探索） | 无强制消耗（探索中消耗可选） | N/A | — |
| Return（返航） | basic 航线消耗 | ❌ 同上 | 返航是否也需要消耗？ |
| Repair（修复） | repair 材料 | ❌ 未定义——世界修复系统未设计 | 首个修复节点需要多少 repair？ |

**关键发现**：起始经济的数值（10 basic / 2 repair）在本 GDD 中**无法被验证**——验证所需的两个参数（航线消耗率、修复材料需求量）都不在本系统的控制范围内。

#### 最坏情况分析

- 如果来回航线共消耗 6 basic，剩余 4 basic 用于下次出航——有压力但可行
- 如果来回航线共消耗 10 basic，首次返航后 basic=0——必须立即通过探索或购买获取更多 basic
- 如果首个修复节点需要 3 repair_kit，起始的 2 个不够——必须先在探索中找到 repair

建议的起始值处于"最低存活基准"是正确的设计方向——刚好够但不充裕。但具体的"够"是多少需要在航线消耗和修复需求定义后才能确定。

**判定**：🟡 推荐项 R3——起始经济数值无法独立验证。建议在本 GDD 的 Starting State 部分或 Open Questions 中声明验证起始经济所需的下游参数（航线消耗量、首修需求量），并注明这些参数确定后起始值需要重新校准。

---

### 7. [economy-designer] 市场集成缺口 —— 资源价值锚点

#### 核心问题：资源没有"价值"

本 GDD 定义资源的物理属性（volume、weight、stack_rule、supply_class）和经济行为（可交易、可消耗），但**没有定义资源的经济价值**。集市系统"拥有价格"，但价格必须有一个锚点——即使是固定价格也需要一个基准。

当前设计中，以下信息缺失：
1. **资源的基础价值**：一个 basic_supply 值多少钱？一个 repair_kit 值多少钱？
2. **supply_class 的价值层级**：local-specialty 比 basic 贵多少？
3. **mass_class 与价值的关系**：heavy 货物是否因为"重"而包含更多或更贵的资源？
4. **价值锚点的归属**：是注册表定义 `base_value`，还是本系统定义 `base_value`，还是集市系统完全自主定价？

#### 为什么这是阻塞问题

**这是 Round 4 的 B1 阻塞项**——原因是：
- 集市系统（`空港 / 村镇状态与集市交易`）尚未设计，但它是本 GDD 的直接下游
- 集市系统需要从本系统或注册表获取定价锚点，否则价格是空中楼阁
- 本 GDD 的 Interactions 表格定义了 `get_available_goods`、`validate_purchase`、`execute_purchase` 但没有任何价值相关接口
- 玩家需要价值信息来做经济决策——"这个特产值不值得占用 120 容积？"——而 GDD 没有提供这个信息的基础

**本 GDD 不需要拥有定价逻辑**——定价是集市系统的职责。但本 GDD（或注册表）需要提供价值锚点（如 `base_value`），为集市系统的定价提供参考基准。没有锚点，集市系统无法一致地定价。

#### 推荐的解决方案

**选项 A**：在注册表的资源 Schema 中增加 `base_value` 字段（单位为某种货币）。本 GDD 的 Interactions 表格增加 `get_resource_base_value(resource_id)` 查询。集市系统读取 `base_value` 并应用其价格规则（固定价格 = base_value × 倍率，或根据供需调整）。

**选项 B**：在本 GDD 的 supply_class 表格中增加隐式价值层级（如 "local-specialty 的建议价值约为 basic 的 5-10 倍"）。集市系统据此自主定价。

**选项 C**：完全交由集市系统——集市系统自由定义所有价格，本 GDD 不提供任何价值锚点。风险：集市系统可能无意中创造经济异常（如 basic 比 specialty 贵）。

**推荐选项 A**——注册表是内容数据的权威源，`base_value` 应该在注册表中定义。这保持了"注册表定义静态数据，本系统消费注册表数据"的架构一致性。

**判定**：🔴 阻塞项 B1——资源缺乏价值锚点。集市系统无法在无锚点的情况下一致地定价。需要在注册表或本 GDD 中定义 `base_value` 或等价的资源价值层级。

---

### 8. [economy-designer] 附加经济发现

#### 8.1 Pool 1（on_person）vs Pool 5（carried）的可用性歧义

State 描述说 on_person 是 "玩家身上持久携带"，carried 是 "探索中随身携带，处于局内（临时）"。从状态机看，on_person 的物品 "探索期间保留在玩家身上不受损失"，但：

- **on_person 中的物品在探索中是否可用？** 玩家能从 Pool 1 直接使用物品（如消耗 navigation 降低风险）还是必须先转移到 Pool 5？
- 若 Pool 1 在探索中不可用（物品在飞艇上而非身上），那命名 `on_person` 是误导
- 若 Pool 1 在探索中可用但不承受风险，那存在不对称——为什么身上的物品有些有风险有些没有？

OQ-06 问 "探索中能否从随身物品栏直接使用物品？" 但没有回答。这是经济设计中的信息不对称——玩家在探索中的资源可用性直接影响风险决策。

**判定**：🟡 推荐项 R4——Pool 1 在探索中的可用性需要澄清。建议在状态机约束中增加说明：探索期间 Pool 1 物品是否可被消耗/使用。

#### 8.2 容积制的"通货膨胀"

仓库从 0 到 1000 容积的填充过程会经历阶段性经济心理：
- **0-30%**：充裕感——"空间很够，什么都可以带回来"
- **30-70%**：正常管理——"需要留意但不用太担心"
- **70-90%**：压力出现——"该考虑用掉一些库存了"
- **90-100%**：硬约束——"必须清理或消耗才能继续收集"

这是好的容量心理学设计。但本 GDD 的容量条设计（Visual/Audio Requirements）已经很好地捕捉了这些阶段。不需要额外的经济设计干预。

#### 8.3 资源"通胀"的结构性分析

对 MVP（3-5 个 session，1 个修复节点，1 个探索点）：
- Session 1-2：玩家积累资源，修复第一个节点——Sink 活跃
- Session 3-5：修复节点已消耗——Sink 枯竭。如果航线消耗未定义，资源只进不出

这不会在 MVP 范围内造成严重问题（内容太少，session 太少），但它意味着 MVP 经济模型对于"长于 MVP 的 session 序列"是不自洽的。这不是 MVP 的阻塞问题——但需要明确记录为"后 MVP 设计债"。

#### 8.4 伙伴容量加成的时间点

Tuning Knobs 中 `carry_slot_bonus` 和 `carry_volume_bonus` 标记为"预留（MVP 为 0）"。如果在 MVP 后期或 Vertical Slice 引入伙伴加成，会出现经济突变：玩家的容量突然增加，整备策略改变。建议：如果伙伴系统在 MVP 后期引入容量加成，需要重新校准起始经济和探索战利品数量。

---

### 综合判定

| 类别 | 数量 | 项目 |
|------|------|------|
| 🔴 阻塞项 | 2 | B1: 资源缺乏价值锚点（base_value）；B2: 航线消耗接口未定义（Faucet/Sink 无法平衡验证） |
| 🟡 推荐项 | 5 | R1: 航线消耗接口缺失；R2: 货物 Q 值与 mass_class 经济关系未定义；R3: 起始经济数值无法独立验证；R4: Pool 1 在探索中的可用性歧义；R5: 后 MVP 循环 Sink 枯竭风险需记录 |
| 🟢 观察项 | 3 | 货物不可堆叠性的经济影响（OQ-04）、伙伴容量加成的经济突变风险、容积制通胀的阶段性心理（已在 Visual 中处理） |

#### 阻塞项详细说明

**B1 — 资源价值锚点缺失**：本 GDD 定义资源的所有物理和行为属性，但没有定义经济价值（`base_value`）。集市系统将需要定价锚点。建议在注册表 Schema 中增加 `base_value` 字段，或在本 GDD 中定义 supply_class 的价值层级。集市系统设计无法在没有价值锚点的情况下开始。

**B2 — 航线消耗接口缺失**：`supply_class` 表格将 basic 的用途列为 "航线消耗"，但本 GDD 的 Interactions 表格和操作集中没有航线消耗接口。`航行与路线风险` 系统（状态：未设计）需要从本系统获得消耗 basic 补给的能力。没有这个接口，Faucet/Sink 分析是不完整的——最自然的循环 Sink 是缺失的。

---

### 对 GDD 的建议修订（如用户批准）

1. **Interactions 表格**：增加与 `航行与路线风险` 系统的行，暴露 `consume_for_route(resource_costs)` 接口
2. **Open Questions**：增加航线消耗的基本约束声明（"航线消耗量由航行系统定义，建议每次出航 basic × N，N 基于航线长度/风险"）
3. **注册表 Schema 影响**：建议在注册表的资源条目中增加 `base_value` 字段——本 GDD 的 Interactions 表格增加 `get_resource_base_value(resource_id)` 查询
4. **Tuning Knobs**：增加航线消耗相关调参（即使值未定，参数名和范围应先存在）
5. **状态机**：在 carried 状态的约束中说明 Pool 1 物品在探索期间的可用性
