# ADR-0018: 威胁结算系统 — CombatManager Autoload #12

## Status

Accepted

## Date

2026-05-05

## Summary

CombatManager 作为 Autoload #12，是探索循环中的薄层威胁结算引擎。它消费 #11 (Exploration) 传入的 `threat_context`，驱动一个 4 态微观状态机（IDLE → AWAITING_RESPONSE → PROCESSING → RESOLVED），以数据驱动方式解析威胁配置（Registry #1），产出 `combat_result` 结构体并级联写入 #8 (Module/Hull) 和 #5 (Resources)。威胁状态通过 #11 探索点快照持久化（ADR-0003 `progress.exploration`），CombatManager 本身无独立持久化层。MVP 仅定义一种威胁类型（guard），但威胁配置表和结算公式均为类型参数化设计，预留多类型扩展能力。

## Decision Makers

User + Claude Code

## Last Verified

2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | Core — Game Logic |
| **Knowledge Risk** | LOW — 纯 GDScript 数据结构、状态机、信号，无引擎特定 API 依赖 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `design/gdd/combat-threat-handling.md`, `docs/architecture/architecture.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | 4 态状态机转换完整性；威胁队列 FIFO 顺序；combat_result contract 所有字段非 null 验证；retreat_flagged 跨威胁会话持久化；module_damage eligible_modules 过滤逻辑（仅 actual_state=installed 的槽位） |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #12 启动顺序, Phase 5 feature_ready)；ADR-0002 (Signal 通信协议 — typed params, sync emit, max depth 2)；ADR-0003 (威胁持久化通过 #11 探索点快照 — `progress.exploration`)；ADR-0005 (ResourcesManager — `consume_in_combat`, `get_carried_contents_by_tag`)；ADR-0009 (Module/Hull — `apply_hull_damage`, `apply_module_damage`, `get_installed_slots`)；ADR-0010 (EncounterContext — 威胁点配置传递到 #11 再传入本系统)；ADR-0012 (UIManager — 决策面板 UI, 输入路由, 威胁活跃指示器) |
| **Enables** | ADR-0013 (Exploration — 威胁触发与结算结果消费)；ADR-0016 (Feedback/VFX — 威胁结算事件的视听反馈)；Future threat type expansions |
| **Blocks** | Exploration (#11) 威胁点结算逻辑 — 依赖 `resolve_threat()` 入口和 `combat_result` 契约；Feedback (#17) 威胁结算事件 — 依赖威胁信号定义 |
| **Ordering Note** | 应在 ADR-0005 (ResourcesManager) 和 ADR-0009 (Module/Hull) 之后 Accepted — 核心结算依赖 `consume_in_combat` 和 `apply_hull_damage` |

## Context

### Problem Statement

《云海织航》探索循环的核心张力来自"搜刮深度 vs 安全提取"的推拉。威胁点是张力的机械载体——玩家在探索区域的威胁触发半径内面临一个判断点：用 repair_kit 安全清除（应急处理）、硬扛伤害（Tank）、或放弃深入（Retreat 并承受更高的物资损失率 λ_forced=0.25）。GDD #12 定义了完整的威胁结算流程、4 态状态机、5 个核心公式、10 个边界案例和威胁配置扩展表。但 CombatManager 的 Autoload 定位、威胁队列机制、combat_result 契约的形式化、与 #11 的状态所有权边界（谁管理 is_active？谁持久化？）、以及 data-driven 威胁配置的传递路径未在 ADR 中形式化。没有这个 ADR，#11 和 #12 之间的威胁状态管理会分散到两个系统中，失去单一权威边界。

### Current State

- 所有威胁相关逻辑仅存在于 GDD #12 的设计层——无代码、无 ADR
- `TR-combat-001`, `TR-combat-002`, `TR-combat-003` 在 `tr-registry.yaml` 中均标记 `adr: ""`（零覆盖）
- 架构审计报告 (`architecture-review-2026-05-05.md`) 将此标记为 Pre-Production 最高优先级 Concern
- #12 与其他系统的所有依赖（#5, #8, #11, #16, #17）接口已由 GDD 定义，但无 ADR 级别的契约固化

### Constraints

- **Godot 4.6.2 + GDScript**: 纯游戏逻辑，无引擎 API 风险
- **ADR-0002 信号协议**: typed params, sync emit, max depth 2, emit-after-mutation
- **ADR-0003 持久化**: 威胁 `is_active`/`suppressed` 状态通过 #11 探索点快照（`progress.exploration`）持久化——CombatManager 不拥有独立持久化层
- **ADR-0005 ResourcesManager**: `consume_in_combat` 从 Pool 5 永久移除资源
- **ADR-0009 Module/Hull**: `apply_hull_damage` 和 `apply_module_damage` 遵循 #8 的波段转换和模块效率规则
- **ADR-0010 EncounterContext**: 威胁配置通过 EncounterContext → #11 → threat_context.encounter_params 路径传递
- **ADR-0012 UIManager**: 决策面板为模态覆盖层，威胁活跃期间的输入路由由 #12 与 #16 协调
- **MVP 边界**: 1 种威胁类型 (guard), 3 种响应选项, 3 种结算结果

### Requirements

- 4 态微观状态机: IDLE → AWAITING_RESPONSE → PROCESSING → RESOLVED
- 单一入口点 `resolve_threat(threat_context) → combat_result`
- 3 种响应选项: 应急处理（消耗 repair_kit，清除威胁）、硬扛（承受 8-12 伤害 + 30% 模块风险 + 击退 8 单位）、撤退（无伤害 + retreat_flagged + 击退 10 单位）
- 威胁队列: FIFO，最大深度 4，溢出丢弃 + 警告日志
- Data-driven 威胁配置: 所有数值来自 Registry，通过 encounter_params 传入
- 重触发防护: 击退距离 > 触发半径，防止立即重触发
- 重入防护: 同一时间仅一个威胁处于活跃结算状态
- 应急可用性校验: check_emergency_available 在结算步骤 1 执行，硬失败返回 ERR_UNAVAILABLE
- 模块损伤过滤: eligible_modules 仅包含 actual_state=installed 的槽位

## Decision

### 1. CombatManager 作为 Autoload #12

CombatManager 在 Phase 5 (feature_ready) 中初始化。`_ready()` 仅执行信号声明和内部常量定义；实际威胁队列和状态在收到 `feature_ready` 信号后初始化。

```
Autoload 顺序 (Phase 5):
  #10 Navigation          ──┐
  #11 Exploration         ──┤
  #12 Combat              ──┤ 并行接收 feature_ready
  #13 WorldRepair         ──┤
  #14 Settlement          ──┤
  #15 Partner             ──┘
```

- 单例命名: `Combat`
- `_ready()` 预算: ≤5ms（仅信号声明）
- 无 `_process()` / `_physics_process()` — 纯事件驱动

### 2. 4-State Micro State Machine

```
┌──────┐  threat_context 到达   ┌──────────────────┐
│ IDLE │───────────────────────→│ AWAITING_RESPONSE │
└──────┘                        └───────┬──────────┘
      ↑                                 │ 玩家选择响应
      │                                 ▼
      │                        ┌──────────────┐
      │                        │  PROCESSING   │
      │                        └───────┬──────┘
      │                                │ 结算完成
      │                                ▼
      │                        ┌──────────────┐
      └────────────────────────│   RESOLVED    │
                               └──────────────┘
```

| 状态 | 说明 | 玩家可操作 | 持续时间 |
|------|------|-----------|---------|
| `IDLE` | 无活跃威胁结算 | 移动、搜索、交互 | 直到威胁触发 |
| `AWAITING_RESPONSE` | 决策呼吸阶段，探索暂停，决策面板显示 | 查看状态、选择响应 | 不限时（无计时器） |
| `PROCESSING` | 执行结算序列（C4 步骤 1-9） | 无 | ≤1 帧（同步） |
| `RESOLVED` | 结算完成，combat_result 返回 #11 | 无 | 1 帧（#11 恢复探索控制后转 IDLE） |

**重入防护**: 状态 ≠ IDLE 时，任何到达的 threat_context 被加入 FIFO 队列（最大深度 4）。当前威胁结算完成后自动出队处理。队列满时，最早进入的威胁被丢弃并记录警告日志。

### 3. Single Entry Point: resolve_threat()

```gdscript
## 由 #11 Exploration 调用。单参数入口。
## 在 AWAITING_RESPONSE 状态内部，通过 #5 查询 carried_inventory，
## 通过 #16 决策面板 UI 收集 response_choice。
func resolve_threat(threat_context: Dictionary) -> Dictionary:
    # threat_context:
    #   threat_type: StringName       # "guard" (MVP)
    #   threat_id: StringName         # e.g. "g-b1"
    #   position: Vector2             # 威胁世界位置（用于击退方向计算）
    #   encounter_params: Dictionary  # 来自 Registry 的威胁配置（见 §8）
```

**返回值 `combat_result`**:

| 字段 | 类型 | 可空 | 说明 |
|------|------|------|------|
| `outcome` | String | 否 | `"suppressed"` / `"tanked"` / `"retreated"` |
| `hull_damage` | int | 否 | 0 或 8-12（硬扛） |
| `module_damage` | Dictionary 或 null | 是 | `{slot_id: StringName, damage_type: StringName}` |
| `resources_consumed` | Array 或 null | 是 | `[{resource_id: StringName, quantity: int}]` |
| `knockback` | Dictionary 或 null | 是 | `{direction: Vector2, distance: float}` |
| `retreat_flagged` | bool | 否 | 撤退标记（持久化至探索会话结束） |

### 4. Three Response Options

| # | 选项 | 可用条件 | 资源消耗 | 船体伤害 | 模块风险 | 威胁结果 | 击退 |
|---|------|---------|---------|---------|---------|---------|------|
| A | **应急处理** | Pool 5 中 ≥1 repair_kit | 1 repair_kit | 0 | 无 | suppressed (is_active=false) | 0 |
| B | **硬扛** | 始终 | 无 | uniform_int(8, 12) | 30%: installed 槽位 → damaged | active (保持) | 8.0 |
| C | **撤退** | 始终 | 无 | 0 | 无 | active (保持) + retreat_flagged | 10.0 |

**可用性校验**:
- 应急处理不可用 → 按钮灰显，tooltip: "需要 repair_kit ×1（随身物品栏中无可用）"
- 硬扛在 hull ≤ 12 时 → 按钮显示 "⚠ 船体严重受损" 但不阻止选择
- 撤退始终可用 — 安全阀

### 5. Settlement Sequence (C4 — Strict Order)

```
1. 验证所选选项的可用条件。不满足 → 返回 ERR_UNAVAILABLE
2. 执行资源消耗（仅应急处理）→ #5.consume_in_combat("repair_kit", 1)
3. 计算船体伤害 → calc_hull_damage(response_choice, encounter_params)
4. 判定模块损伤（仅硬扛）→ calc_module_damage(response_choice, encounter_params, module_state)
5. 应用船体伤害 → #8.apply_hull_damage(hull_damage)
6. 应用模块损伤 → #8.apply_module_damage(slot_id, damage_type)
7. 更新威胁状态（仅应急处理）→ threat_point.is_active = false
8. 执行击退 → 方向从威胁指向玩家
9. 返回 combat_result 至 #11
10. #11 恢复探索状态: threatened → exploring
```

步骤 1-9 在单帧内同步完成。步骤 10 由 #11 执行。

### 6. Downstream Cascades

| 结果条件 | 目标系统 | 接口调用 |
|---------|---------|---------|
| hull_damage > 0 | #8 Module/Hull | `apply_hull_damage(amount)` — integrity 扣减，波段可能转换 |
| module_damage != null | #8 Module/Hull | `apply_module_damage(slot_id, "guard_impact")` — actual_state → damaged，效率降至 0.6(scout)/0.5(cargo) |
| resources_consumed != null | #5 Resources | `consume_in_combat(resource_id, quantity)` — 从 Pool 5 永久移除 |
| retreat_flagged = true | #11 Exploration | `extraction_loss_settlement` 使用 λ_forced = 0.25 |
| knockback | #11 Exploration | 玩家位置按方向+距离移动（`move_and_collide`） |

### 7. Persistence Strategy — State Ownership Boundary

**CombatManager 不拥有独立持久化层。** 威胁持久状态的权威来源是 Exploration (#11)。

| 状态 | 所有者 | 持久化路径 | 生命周期 |
|------|--------|----------|---------|
| `is_active` | #11 探索点快照 | `progress.exploration.threats[threat_id].is_active` | 探索会话内永久 |
| `retreat_flagged` | #11 探索会话状态 | `progress.exploration.retreat_flagged` | 探索会话内永久 |
| `response_choice` | #12 内部 | 不持久化 | 仅结算期间 |
| 威胁队列 | #12 内部 | 不持久化 | 仅结算期间 |

**跨 save/load 行为**:
- 保存 → 加载在同一探索会话内: suppressed 威胁保持 suppressed（`is_active=false` 在快照中）
- 探索会话结束（DEPARTED）→ 重新进入同一探索点: 所有威胁重置为默认 active（新会话，新快照）

### 8. Data-Driven Threat Configuration

所有威胁数值来自 Registry (#1)，通过 EncounterContext (#10) → Exploration (#11) → `threat_context.encounter_params` 路径传入。CombatManager 内部不硬编码任何数值。

| 配置字段 | MVP 值 (guard) | 公式引用 |
|---------|---------------|---------|
| `full_damage_min` | 8 | F-12-02 |
| `full_damage_max` | 12 | F-12-02 |
| `module_damage_chance` | 0.30 | F-12-03 |
| `emergency_cost_repair_kit` | 1 | F-12-04 |
| `knockback_distance_tanked` | 8.0 | F-12-05 |
| `knockback_distance_retreat` | 10.0 | F-12-05 |
| `can_be_suppressed` | true | C4 步骤 7 |
| `trigger_radius` | 4-6 | #11 管理，用于重触发防护判定 |

**约束**: `knockback_distance_tanked` (8.0) > `trigger_radius` 最大值 (6.0) — 违反此约束将导致击退后立即重触发。此约束由 #11 在加载威胁配置时校验。

### 9. Signal Events

Combat 声明的信号（遵循 ADR-0002 typed params + sync emit 协议）:

| 信号 | 签名 | 触发时机 | 消费者 |
|------|------|---------|--------|
| `threat_resolved` | `threat_resolved(outcome: String, threat_id: StringName)` | 结算序列完成（C4 步骤 9 之后） | #17 (Feedback), #16 (UI) |
| `threat_suppressed` | `threat_suppressed(threat_id: StringName)` | 应急处理成功，threat.is_active → false | #17 (Feedback — 威胁标记从 minimap 淡出) |
| `threat_tanked` | `threat_tanked(threat_id: StringName, hull_damage: int)` | 硬扛结算完成 | #17 (Feedback — 船体伤害动画), #16 (UI) |
| `threat_retreated` | `threat_retreated(threat_id: StringName)` | 撤退结算完成 | #17 (Feedback — 撤退效果), #16 (UI) |

**Max cascade depth**: threat_resolved → #17 消费 → 终结（depth=1）。不超过 depth=2。

**Emit rule**: Emit-after-mutation — 信号在状态变更完成后 emit，不在变更过程中 emit。

## Alternatives Considered

### Alternative 1: 威胁结算内嵌于 Exploration (#11)

- **Description**: 不创建独立的 CombatManager。威胁结算作为 Exploration 的私有方法实现。
- **Pros**: 减少 Autoload 数量；威胁状态直接访问探索点数据，无跨系统调用开销
- **Cons**: Exploration 职责膨胀——它已经是探索移动、搜索点、情报点、威胁点、提取锚点的协调者，加入结算逻辑后违反单一职责原则；威胁类型扩展时 Exploration 需要修改而非 Combat 独立扩展；威胁结算无法独立测试（必须加载完整 Exploration 场景）；威胁队列逻辑与探索状态机耦合，增加 Exploration 状态机复杂度
- **Estimated Effort**: 低（短期），高（长期维护）
- **Rejection Reason**: 违反 SRP——Exploration 是探索流程的协调者，不应同时是威胁结算引擎。ADR-0001 已将 #11 和 #12 定义为独立 Autoload，当前设计遵循已建立的架构分层

### Alternative 2: 实时战斗（Active-Time Battle）

- **Description**: 威胁触发后进入实时动作战斗——玩家操控角色躲避攻击、瞄准弱点、使用技能。
- **Pros**: 更高的动作参与感；战斗深度更大
- **Cons**: 违反 Pillar 1「规划先于冒险」——实时战斗奖励反应速度而非判断力；违反 Pillar 4「温和压力」——倒计时/实时压力取代了决策呼吸的设计意图；Web 平台 input latency 和 Compatibility 渲染器性能约束不适合实时战斗；增加 #12 的 scope 远超 MVP 边界
- **Estimated Effort**: 极高（3-5× 当前设计）
- **Rejection Reason**: 完全不符合游戏的核心设计支柱。这是策略/航海主题的游戏，不是动作游戏。「决策呼吸」是设计的核心差异化点

## Consequences

### Positive

- **单一职责**: CombatManager 仅负责威胁结算——接收 threat_context，产出 combat_result。不管理探索移动、不管理物品生成、不管理 UI 渲染
- **独立可测试**: 所有 5 个核心公式可独立单元测试（无场景依赖）；状态机可在 mock #5/#8/#11 注入下集成测试
- **威胁类型扩展**: C8 配置表 + encounter_params 传递路径使新威胁类型的添加不需要修改 CombatManager 代码——仅需在 Registry 中注册新配置
- **状态所有权清晰**: 威胁持久状态由 #11 管理；Combat 不持久化任何状态——这是有意设计，使 Combat 成为 stateless 结算引擎（除当前结算事务外）

### Negative

- **跨系统耦合**: 一次威胁结算涉及 #5 + #8 + #11 + #12 四个 Autoload 的调用——增加了调用链深度和调试复杂度。这是薄层设计的固有代价
- **击退不穿越碰撞体**: `move_and_collide` 在碰撞体前停止——某些边缘地形配置下击退距离可能不足以保证脱离触发半径（需关卡设计配合，非代码问题）
- **retreat_flagged 不可清除**: 玩家即使后续用应急处理清除威胁，撤退标记仍保留（λ_forced=0.25）。这可能导致玩家困惑——UI 需要在撤离结算摘要中明确提示

### Neutral

- 威胁队列机制增加了 CombatManager 的内部复杂度，但避免了 Exploration 的状态管理负担
- 所有 Combat 信号为异步发出——消费者不阻塞结算

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| 威胁队列满导致威胁丢失 | Low | Medium | 队列深度 4 是保守上限——通常探索区域不会有 4 个威胁同时触发。警告日志记录丢弃事件用于调试 |
| `move_and_collide` 击退被阻挡导致重触发 | Low | Low | 击退距离 (8.0) > 触发半径 (6.0) 提供了 2 单位的余量。仅当玩家被推入墙角且威胁紧邻墙时可能发生——由关卡设计避免 |
| 跨系统调用链性能瓶颈 | Low | Low | 结算在单帧内完成，所有调用为同步方法调用。GDScript 方法调用开销 <0.01ms/调用，最坏情况 6 次调用 <0.1ms |
| 威胁清除动画未完成时 save/load | Low | Medium | 威胁状态持久化由 #11 在结算完成后触发——不会捕获中间态。save/load 发生在 AWAITING_RESPONSE 期间时，combat_result 未产生，状态一致 |

## Performance Implications

| Metric | Estimated | Budget | Note |
|--------|-----------|--------|------|
| `resolve_threat()` 单次调用 | <0.5ms | ≤1ms | 纯 GDScript 同步调用链（6 次方法调用），无引擎 API 开销 |
| Memory (CombatManager persistent) | <0.1MB | ≤1MB | 仅存储内部状态枚举和威胁队列（最大 4 条目） |
| Signal emit cascade | <0.01ms | ≤0.1ms | 单 emit，depth=1，消费者 ≤2 |

## Migration Plan

N/A — greenfield 系统。CombatManager 在新文件中创建。以下为首次实现步骤：

1. 创建 `src/core/combat_manager.gd` — Autoload #12
2. 在 `project.godot` 注册 Autoload
3. 实现 4 态状态机 + `resolve_threat()` 入口
4. 实现 3 个公式函数: `calc_hull_damage`, `calc_module_damage`, `calc_knockback`
5. 连接到 #5, #8, #11 接口（由各自 ADR 定义的契约）
6. #16 实现决策面板 UI 后，连接响应选择回调

## Validation Criteria

- [ ] 所有 4 个状态转换：(IDLE→AWAITING_RESPONSE), (AWAITING_RESPONSE→PROCESSING), (PROCESSING→RESOLVED), (RESOLVED→IDLE) 均正确
- [ ] 重入防护: AWAITING_RESPONSE 期间到达的 threat_context 加入队列而非直接处理
- [ ] 队列溢出: 第 5 个威胁到达时最早条目被丢弃 + 警告日志记录
- [ ] `resolve_threat()` 在状态 ≠ IDLE 时返回 ERR_BUSY
- [ ] `check_emergency_available()` 在 repair_kit=0 时返回 false
- [ ] `calc_hull_damage("tank", guard_params)` 1,000 次调用均在 [8, 12] 区间
- [ ] `calc_module_damage` 的 eligible_modules 过滤排除 actual_state=damaged 的槽位
- [ ] retreat_flagged=true 跨威胁会话持久化（第二次撤退不改变状态）
- [ ] 所有 4 个信号在正确时机 emit（emit-after-mutation）
- [ ] CombatManager 在 #5/#8/#11 mock 注入下可独立测试

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/combat-threat-handling.md` | Combat/Threat #12 | TR-combat-001: 1 threat type with 3 response options | §4: 3 response options table, data-driven from Registry |
| `design/gdd/combat-threat-handling.md` | Combat/Threat #12 | TR-combat-002: Decision breath — player chooses without real-time pressure | §2: AWAITING_RESPONSE state — exploration paused, no timer |
| `design/gdd/combat-threat-handling.md` | Combat/Threat #12 | TR-combat-003: 3 outcomes — damaged, knocked back, retreat | §6: Downstream cascades — hull_damage→#8, knockback→#11, retreat_flagged→#11 |
| `design/gdd/combat-threat-handling.md` | Combat/Threat #12 | AC-12-01 through AC-12-23 (27 total) | §5: Settlement sequence implements C4; §3: combat_result contract implements C5 |
| `design/gdd/exploration-scavenge-scenario.md` | Exploration #11 | Threat point interaction triggers threat resolution | §3: resolve_threat(threat_context) single entry point |

## Related

- [ADR-0001 — Autoload/Scene Boot Order](adr-0001-autoload-scene-boot-order.md) — Combat Autoload #12, Phase 5
- [ADR-0002 — Signal Communication Protocol](adr-0002-signal-communication-protocol.md) — typed params, sync emit
- [ADR-0003 — Save System / JSON Serialization](adr-0003-save-system-snapshot-json.md) — threat persistence via Exploration snapshot
- [ADR-0005 — Resource Pool System](adr-0005-resource-pool-system.md) — consume_in_combat, Pool 5
- [ADR-0009 — Module/Hull System](adr-0009-airship-module-hull-system.md) — apply_hull_damage, apply_module_damage
- [ADR-0010 — EncounterContext Type](adr-0010-encounter-context-type.md) — threat config data path
- [ADR-0012 — UI/Input Routing](adr-0012-ui-input-routing-dual-focus.md) — decision panel overlay, input routing during threat
- [architecture-review-2026-05-05.md](architecture-review-2026-05-05.md) — Concern #1: Combat #12 zero ADR coverage
