# GDD Cross-Review Report — 2026-05-08

**审查日期**: 2026-05-08
**GDD 审查数**: 16
**系统覆盖**: #1-#16 (全部 MVP 系统)
**审查模式**: Full (一致性 + 设计理论 + 场景走查)
**先前审查**: 2026-04-30 (consistency only), 2026-05-03 (full)

---

## 综合汇总

| 类别 | BLOCKER | WARNING | 合计 |
|------|---------|---------|------|
| Phase 2: 一致性 | 12 | 14 | 26 |
| Phase 3: 设计理论 | 1 | 7 | 8 |
| Phase 4: 场景走查 | 1 | 4 | 5 |
| **总计** | **14** | **25** | **39** |

---

## Phase 2: 跨 GDD 一致性

### 2a — 依赖双向性

#### 🔴 B-2a-1: 航行 (#10) 缺少 Hub (#7) 上游依赖

Hub (#7) Interactions 表将 Mode B (自主飞行/舵轮) 委派给 Navigation (#10)，但 Navigation (#10) Dependencies 节仅列出 #9, #1, #8, #6 作为上游来源，Airship Hub (#7) 缺失。

**修复**: 将 #7 添加到 Navigation (#10) 上游依赖，并定义 Mode B 激活接口。

#### 🔴 B-2a-2: 资源 (#5) 过时的双向检查表

资源 (#5) GDD "双向依赖检查" 表将 7 个下游系统中的 8 个标记为 "❌ 未设计" 或 "⚠️ 尚未设计"。实际上所有系统均已 Approved。依赖风险表显示 "所有下游系统未设计 — 🔴 高"，这与事实不符。

**修复**: 使用当前状态更新双向检查表，并验证每个下游 GDD 的 Dependencies 节是否引用 #5。

#### 🔴 B-2a-3: 情报 (#6) 过时的下游状态标记

情报 (#6) GDD Dependencies 节将 #9, #10, #11, #13 标记为 "尚未设计"，并注明 "对端 GDD 中的反向引用：待该 GDD 创建时，需在 Dependencies 中双向标注"。以上四个系统均已 Approved 且在其 Dependencies 节中正确引用了 #6。

**修复**: 更新 #6 的下游依赖条目以反映当前 Approved 状态，并确认反向引用存在。

#### ⚠️ W-2a-1: 伙伴 (#15) 跨 GDD 修订标记可能已解决但未清除

伙伴 (#15) GDD 头注 "Cross-GDD Impact: 修订 #6 Part 8" — 但 #6 Part 8 已有明确的 Scope note 说明 R15.5 已应用。标记未清除。

#### ⚠️ W-2a-2: Hub (#7) 引用 #10 Mode B 接口但名称未定义

Hub 将 Mode B 委派给 Navigation，但具体的信号/函数接口名称未在 #10 侧定义。

---

### 2b — 规则矛盾

#### 🔴 B-2b-1: 航线 `route.sky-reef-arc-01` 有两个不同目的地

- `entities.yaml` (line 116): `destination_id: location.sky-reef-outpost`
- `content-data-state-registry.md` (line 302, schema example): `destination_location_id: location.starlight-dock`

两个权威来源给出同一 route ID 的不同目的地。

**修复**: 统一为一个目的地。按 systems-index.md，`location.sky-reef-outpost` 是已记录的目的地。更新 content-data-state-registry.md 中的示例。确定 `location.starlight-dock` 是有效地点还是旧名称。

#### 🔴 B-2b-2: 实体注册表中缺失 Location 实体

两个 location ID 在 GDD 中被引用但不存在于 `entities.yaml`:
- `location.glass-harbor-outskirts` — 被 world-repair-unlock.md (line 57) 和 port-village-market.md (line 98) 引用
- `location.starlight-dock` — 被 content-data-state-registry.md 示例引用

**修复**: 将这些 location 添加到 entities.yaml 并赋予完整定义，或更新 GDD 引用已有 location ID。

#### 🔴 B-2b-3: entities.yaml 中战斗伤害值过时

- `entities.yaml` `calc_hull_damage` (line 555-558): 注释称 "Guard: uniform_int(12, 18)"
- `entities.yaml` constants (lines 779-797): `guard_full_damage_min: 8, guard_full_damage_max: 12` (revised 2026-05-04, C1 fix)
- `combat-threat-handling.md` (line 66): "8-12 (uniform 随机)"

公式注释与同一文件中自身的常量矛盾。代码将使用 8-12，但注释声称 12-18。

**修复**: 更新 `calc_hull_damage` 注释为 "Guard: uniform_int(8, 12). Each integer equally likely (1/5)." 同时更新 `calc_module_damage` 注释从 "0.50" 改为 "0.30"。

#### 🔴 B-2b-4: 战斗 GDD 内部不一致 — `hull_warning_threshold`

- combat-threat-handling.md 头注 (line 3): "hull_warning_threshold 18→12" (C1 fix 已应用)
- combat-threat-handling.md 正文 (line 71): "硬扛在 hull ≤ 18 时"
- combat-threat-handling.md 正文 (line 354): "条件：hull ≤ 18"
- combat-threat-handling.md tuning knobs (line 450): 正确值 "12"

Detailed Rules 和 Edge Cases 节仍引用旧值 18。

**修复**: 更新第 71 行和 354 行为 `hull ≤ 12`，或引用 `hull_warning_threshold` 常量名而非硬编码。

#### ⚠️ W-2b-1: 情报 (#6) 使用不同的 Repair Node ID

- 情报 (#6) Path C condition (line 517): `world_repair.repair_lighthouse_01 completed`
- 世界修复 (#13) (line 55): `node_id: repair_node.starlight_dock`

如果 #6 查找 `repair_lighthouse_01` 但 #13 发出 `repair_node.starlight_dock`，能力将永远不会通过 Path C 解锁。

**修复**: 将 #6 对齐至 `repair_node.starlight_dock`。

#### ⚠️ W-2b-2: 起始 repair_kit 数量 vs. 修复节点需求

- 资源 (#5) Starting State: `repair_kit × 4`
- 世界修复 (#13): `required_resources: [{resource.repair_kit, 4}]`

起始数量恰好等于修复需求，零容错空间。

---

### 2c — 过期引用

#### 🔴 B-2c-1: entities.yaml `calc_hull_damage` output_range 使用旧值

`output_range: "[0, 12, 18]"` — 应为 `[0, 8, 12]`（C1 fix 后）。

#### 🔴 B-2c-2: 资源 (#5) 7 个下游系统标记为 "未设计"

双向检查表 (lines 556-563) 将 #8, #11, #12, #13, #14, #15, #16 标记为 "❌ 未设计" / "⚠️ 尚未设计"。所有系统均已 Approved/Designed。

#### 🔴 B-2c-3: 情报 (#6) Approved 系统标记为 "尚未设计"

下游依赖条目 (lines 806-841) 将 #9, #10, #11, #13, #16 标记为 "尚未设计" 并附有 "对端 GDD 中的反向引用：待该 GDD 创建时"。

#### 🔴 B-2c-4: entities.yaml `calc_hull_damage` output_range 列出旧值

`output_range: "[0, 12, 18]"` — 在 C1 fix（伤害从 12-18 重新平衡至 8-12）之后，应为 `[0, 8, 12]`。

#### ⚠️ W-2c-1: entities.yaml `calc_module_damage` 注释值过时

注释称 "Guard: module_damage_chance = 0.50"，但常量 `guard_module_damage_chance: 0.30`（revised 2026-05-04）。

#### ⚠️ W-2c-2: `location.glass-harbor-outskirts` 引用无注册表条目

world-repair-unlock.md (line 57) 和 port-village-market.md (line 98) 引用的 location ID 在 entities.yaml 中不存在。

#### ⚠️ W-2c-3: 注册表示例使用不存在的 Location

content-data-state-registry.md 示例引用 `location.starlight-dock`，该 ID 在 entities.yaml 中不存在。

#### ⚠️ W-2c-4: Route 字段名在注册表中不一致

- content-data-state-registry.md (line 302): `destination_location_id` (schema example)
- entities.yaml (line 116): `destination_id` (actual entry)

同一概念的不同字段名。

---

### 2d — Tuning Knob 所有权冲突

#### ⚠️ W-2d-1: `base_lock_duration` 所有权

#7 和 #9 均消费 `base_lock_duration`。注册表正确地将 #7 标识为来源。无冲突，但 #9 应注明 #7 拥有该常量。

#### ⚠️ W-2d-2: `extraction_loss_ratio` 跨所有权

#5 Tuning Knobs 包含 `extraction_loss_ratio` 并注明 "由探索系统定义"。#11 拥有提取丢失公式（λ_success=0.08, λ_forced=0.25）。无冲突，但 #5 的安全范围（0.2-0.4）与 #11 的实际值（0.08, 0.25）不一致。

---

### 2e — 公式兼容性

#### ⚠️ W-2e-1: 战斗伤害范围 vs. 船体完整性波段

C1 fix 后：单次最大伤害 (12) 可使 integrity 从 76 降至 64，跨越一个波段（intact→damaged）。在 C1 fix 之前的旧公式注释声称 12-18 伤害，可能单次跨越 2 个波段。系统在数学上是一致的，但过时的公式注释有风险。

#### ⚠️ W-2e-2: 侦察预览窗口公式不连续性

`N_preview = ⌊η_scout × 2⌋` — η=0.95（未校准）和 η=0.6（损坏）均给出 N_preview=1。floor 函数使两个状态在预览行为上无法区分。

#### ⚠️ W-2e-3: 船体警告阈值 vs. 完整性波段

hull_warning_threshold = 12（已更正值）。critical 波段为 1-25。在 integrity ≤ 12 时，单次最大 tank 伤害 (12) 可能将 integrity 推至 0。警告在数学上合理的阈值触发。

---

### 2f — AC 交叉检查

#### 🔴 B-2f-1: 情报 AC-6.4 引用不存在的 Repair Node ID

情报 (#6) AC-6.4 测试能力在 `repair_lighthouse_01` 完成时解锁。世界修复 (#13) 在 MVP 中的唯一修复节点是 `repair_node.starlight_dock`。没有系统会发出 `repair_lighthouse_01` 的完成事件。

**修复**: 在 #6 line 517 和 1046 中将 `repair_lighthouse_01` 改为 `repair_node.starlight_dock`。

#### ⚠️ W-2f-1: 修复后航线解锁的跨 GDD AC

- 世界修复 (#13) AC-9: "修复完成后航线 `route.sky-reef-arc-01` 从不可通行变为可通行"
- 航图 (#9) 核心规则: `route.sky-reef-arc-01` initial `traversable: true`

如果航图显示航线初始为可通行，则修复的主要游戏效果（使其可通行）无意义。需要澄清：#9 的静态 `traversable: true` 可能是 "潜在" 状态，而动态检查添加了修复闸门。

---

## Phase 3: 游戏设计整体论

### 3a: 核心循环与竞争循环

核心循环结构健康：

```
Hub整备 → 航图规划 → 航行风险 → 探索搜撤 → 返航 → 世界修复 → (循环)
```

所有 16 个系统均服务于该循环，不存在独立运行的竞争性循环。云海币来源唯一（探索搜刮点）、用途唯一（市场购买），清晰的单用途循环。

#### ⚠️ W-HOL-01: repair_kit 用途竞争

repair_kit 同时被三个独立系统消耗（战斗应急 #12、模块/船体维修 #8、灯塔修复 #13），且探索中的补充率未作规定。

---

### 3b: 玩家注意力预算

| 核心循环环节 | 同时活跃系统数 | 评估 |
|------|------|------|
| Hub (着陆) | 10 (6 active + 4 passive) | ⚠️ 负荷最高 |
| 航图规划 | 4 (2 active + 2 passive) | ✅ |
| 航行 | 3 (2 active + 1 passive) | ✅ 刻意低压 |
| 探索 (搜刮) | 6 (4 active + 2 passive) | ✅ |
| 探索 (战斗) | 5 (4 active + 1 passive) | ✅ |
| 撤离/结算 | 4 (3 active + 1 passive) | ✅ |
| 世界修复 | 8 (3 active + 5 passive) | ✅ |

#### ⚠️ W-HOL-02: Hub 注意力负荷超过 10 个同时系统

着陆状态下 10 个同时活跃系统可能让新玩家不知所措。#16 C.2 节通过将交互限制在站点 Use 事件内来缓解，但 Hub 在视觉上同时呈现所有站点。

---

### 3c: 优势策略检测

**无阻断性优势策略发现。**

战斗响应矩阵（应急处理/硬扛/撤退）不存在明显的最优选项——最优选择严格取决于当前 repair_kit 库存和修复目标。云海币来源垄断是刻意的闭环设计（探索即核心玩法）。

#### ⚠️ W-HOL-03: 风味商品缺乏机械差异性

六种市场商品中，五种在机械上完全相同。风味商品提供了叙事身份，但缺乏玩法差异化。

---

### 3d: 经济循环分析

| 资源 | 来源 | 用途 | 平衡评估 |
|------|------|------|---------|
| 云海币 | 探索搜刮点 (25-40/点) | 市场采购 | ✅ 预期 150-240/探索，覆盖 1-2 个基础物资包 |
| repair_kit | 起始 4 + 探索掉落 | 战斗/维修/修复 | 🔴 补充率未规定 |
| 基础物资 (basic_supply) | 起始 10 + 市场 (50 云海币) | 航行消耗 + 灯塔修复 (4) | ⚠️ 每条航线消耗量未规定 |
| 修补帆布 (repair-canvas) | 市场 (80 云海币) | 航行消耗 | ✅ |
| 航线手记 (route-notes) | 市场 (120 云海币) | 情报解锁 | ✅ |

#### 🔴 B-HOL-01: 探索中 repair_kit 补充率未作规定

repair_kit 在三个独立系统中被消耗，但探索搜刮中的唯一补充来源未在 GDD #11 的掉落池或公式中规定。如果掉落率为 0，使用 repair_kit 进行战斗的玩家将被永久锁在进度之外；如果掉落率过高，灯塔修复成本 (4) 不再构成有意义的取舍。

**修复**: GDD #11 必须明确规定 repair_kit 的掉落率和预期单次探索可得数量。

#### ⚠️ W-HOL-04: 航行补给品消耗率未作规定

市场给出补给品定价，并建议航线消耗应 < 收益的 30%，但 GDD #10 中实际的每条航线补给品消耗量未规定。

---

### 3e: 难度曲线一致性

MVP 中没有传统 "难度曲线"——这是刻意的设计选择，与反支柱 "无纯属性成长" 一致。成长完全通过世界状态变化体现。

#### ⚠️ W-HOL-05: MVP 中不存在难度曲线

在 10 小时以上的游戏过程中可能导致乏味感。压力完全来自未知因素（新内容），而非更高的挑战。

---

### 3f: 支柱对齐

全部 5 项支柱和 5 项反支柱在所有 16 个 GDD 中得到验证。**无违规。**

| 支柱 | 主要系统 | 覆盖情况 |
|------|---------|---------|
| P1 规划 | #6, #9 | 充分 — 7 个系统贡献 |
| P2 照料 | #13, #14 | 充分 — 少数系统中集中深度体现 |
| P3 家园 | #7 | 充分 — 4 个系统贡献 |
| P4 未知 | #10, #11 | 充分 — 7 个系统贡献 |
| P5 深关系 | #15 | 狭窄但深度足够 — 2 个系统 |

#### ⚠️ W-HOL-06: P2 (世界回应照料) 严重依赖于 repair_kit 闸门

如果玩家无法获得足够的 repair_kit（参见 B-HOL-01），整个 P2 支柱在 MVP 中将没有可验证的证据。

---

### 3g: 玩家幻想一致性

**高度一致。** "世界照料者 + 谨慎探索者 + 猫的伙伴" 这一综合体在每个系统中都得到强化。

#### ⚠️ W-HOL-07: 经济循环缺乏 "照料" 情感机制

修复循环提供 "照料" 机制（不可逆提交、世界变化），而经济循环是标准商店界面。不存在 "投资摊位" 或 "帮助 NPC 重建" 机制。

---

## Phase 4: 跨系统场景走查

走查了 4 个关键多系统场景。

### 场景 A: 完整核心循环

**系统链**: #7 → #9 → #10 → #11 → #5 → #8 → #3 → #13 → #6 → #14 → #16

- ✅ 数据流完整，状态转换一致
- ⚠️ **W-SCEN-01**: 修复完成信号级联深度达到 3（#13 → #9, #14, #6 → #16），接近编码规范上限

### 场景 B: 探索中触发战斗应急处理

**系统链**: #11 → #12 → #5 → #8

- ✅ 原子结算（C4 严格顺序），repair_kit 可用性双重验证
- ⚠️ **W-SCEN-02**: 撤退后 knockback 距离大于 trigger_radius 但需验证实现中是否有冷却期

### 场景 C: 修复解锁级联

**系统链**: #13 → #9 + #14 + #6 → #16

- 🔴 **B-SCEN-01**: 情报能力 Path C 因修复节点 ID 不匹配而永久阻塞 (`repair_lighthouse_01` ≠ `repair_node.starlight_dock`)
- ⚠️ **W-SCEN-03**: 修复信号的三个消费者之间可能存在隐式排序依赖

### 场景 D: Save/Load 中会话恢复

**系统链**: #3 ← #5, #6, #8, #9, #11, #13, #14, #15

- ✅ Domain 隔离，SnapshotPackage 校验
- ⚠️ **W-SCEN-04**: 域重置恢复可能静默丢失世界修复进度

---

## 需修订的文件

| 文件 | 阻断项 | 警告项 | 主要问题 |
|------|--------|--------|---------|
| `design/registry/entities.yaml` | 3 | 2 | 公式注释过时、output_range 枚举错误、旧伤害值 |
| `design/gdd/combat-threat-handling.md` | 1 | 0 | hull_warning_threshold 正文值过时 (18→12) |
| `design/gdd/player-knowledge-intel.md` | 3 | 1 | 下游状态标记过时 + 修复节点 ID 不匹配 |
| `design/gdd/resources-goods-capacity.md` | 2 | 1 | 依赖检查表标记过时 |
| `design/gdd/content-data-state-registry.md` | 2 | 2 | 示例引用不存在的 location + 字段名不一致 |
| `design/gdd/navigation-route-risk.md` | 1 | 1 | 缺少 Hub (#7) 上游依赖 |
| `design/gdd/exploration-scavenge-scenario.md` | 1 | 0 | repair_kit 补充率未规定 |
| `design/gdd/world-repair-unlock.md` | 0 | 1 | 引用未注册的 location ID |
| `design/gdd/port-village-market.md` | 0 | 1 | 引用未注册的 location ID |
| `design/gdd/partner-relationships.md` | 0 | 1 | 跨 GDD 修订标记未清除 |
| `design/gdd/chart-route-planning.md` | 0 | 1 | 航线修复前/后可通行性需澄清 |

---

## 修复优先级

### 第 1 层 — 立即修复（阻断 implementation 启动）

1. **entities.yaml 数据完整性** — 更新所有 C1 fix 相关数值: `calc_hull_damage` 注释、output_range、`calc_module_damage` 注释
2. **combat-threat-handling.md** — 修复正文中过时的 `hull_warning_threshold` 值 (line 71, 354)
3. **entities.yaml** — 添加缺失的 location 实体: `glass-harbor-outskirts` 和 `starlight-dock`（或统一为一个）
4. **content-data-state-registry.md** — 解决 `route.sky-reef-arc-01` 目的地矛盾 + 统一 `destination_id`/`destination_location_id` 字段名
5. **player-knowledge-intel.md** — 修复 `repair_lighthouse_01` → `repair_node.starlight_dock` (line 517, 1046)

### 第 2 层 — 架构移交前修复

6. **resources-goods-capacity.md** — 更新依赖状态标记以反映当前 Approved 状态
7. **player-knowledge-intel.md** — 更新下游依赖条目以反映当前 Approved 状态
8. **navigation-route-risk.md** — 添加 Hub (#7) 作为上游依赖 + 定义 Mode B 接口 + 规定每条航线补给品消耗量
9. **exploration-scavenge-scenario.md** — 在掉落表/公式中规定 repair_kit 补充率

### 第 3 层 — 清理

10. **partner-relationships.md** — 清除已解决的跨 GDD 修订标记
11. **chart-route-planning.md + world-repair-unlock.md** — 澄清航线修复前/后可通行性
12. **port-village-market.md** — 将 `location.glass-harbor-outskirts` 对齐至 entities.yaml

---

## 裁定: FAIL 🔴

14 个阻断项必须在架构设计开始前解决。

**若裁定为 FAIL — 重新运行前需执行的操作:**
1. 在 entities.yaml 中完成所有 C1 修复值同步
2. 修复 combat-threat-handling.md 正文中的 hull_warning_threshold
3. 统一所有 GDD 间的 repair node ID、route destination 和 location ID
4. 更新 #5 和 #6 中过时的依赖状态标记
5. 在 #11 探索 GDD 中规定 repair_kit 补充率
6. 在 #10 航行 GDD 中规定补给品消耗率
7. 为 #7↔#10 Mode B 定义接口合约
