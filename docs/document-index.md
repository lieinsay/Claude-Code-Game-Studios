# 云海织航 — 文档索引

> **最后更新**: 2026-05-05
> **项目阶段**: Technical Setup — Foundation + Core ADRs 完成 (12/12)
> **引擎**: Godot 4.6.2 + GDScript (Web-first)
> **文档总数**: ~330 个 .md 文件 + 9 个配置文件

---

## 一、文档全景图

```mermaid
graph TB
    subgraph 入口["🚀 入口文档"]
        README["README.md<br/>项目总览"]
        CLAUDE["CLAUDE.md<br/>主配置"]
        UPGRADING["UPGRADING.md<br/>升级指南"]
        WORKFLOW["docs/WORKFLOW-GUIDE.md<br/>完整工作流指南"]
    end

    subgraph 设计["🎮 设计层 design/"]
        CONCEPT["game-concept.md<br/>游戏概念"]
        SYSIDX["systems-index.md<br/>系统索引"]
        GDD["18 个 GDD 文档<br/>游戏设计文档"]
        REVIEWS["13 个 Review Logs<br/>设计审查记录"]
        ART["art-bible.md<br/>艺术圣经"]
        REGISTRY["entities.yaml<br/>实体注册表"]
    end

    subgraph 架构["🏗️ 架构层 docs/"]
        ARCH["architecture.md<br/>主架构文档"]
        TR["tr-registry.yaml<br/>技术需求注册表"]
        COLLAB["COLLABORATIVE-DESIGN-PRINCIPLE.md<br/>协作设计原则"]
        ENGINE["engine-reference/<br/>引擎参考文档"]
    end

    subgraph 生产["📋 生产层 production/"]
        ACTIVE["session-state/active.md<br/>当前会话状态"]
        LOGS["session-logs/<br/>会话日志"]
        PHASES["Phase 2-5 审查报告"]
    end

    subgraph 基础设施["⚙️ 基础设施 .claude/"]
        AGENTS["agents/ (49)<br/>AI Agent 定义"]
        SKILLS["skills/ (70)<br/>技能定义"]
        RULES["rules/ (11)<br/>代码规范"]
        DOCS[".claude/docs/<br/>配置文档"]
        TEMPLATES["templates/ (39)<br/>文档模板"]
    end

    入口 --> 设计
    入口 --> 架构
    设计 --> 架构
    架构 --> 生产
    基础设施 -.-> 设计
    基础设施 -.-> 架构
    基础设施 -.-> 生产
```

### 入口文档

| 文件 | 说明 |
|------|------|
| [README.md](../README.md) | 项目总览、Studio 层级、Slash 命令概览 |
| [CLAUDE.md](../CLAUDE.md) | 主配置 — 技术栈、目录结构、编码标准 |
| [UPGRADING.md](../UPGRADING.md) | 升级指南 |
| [docs/WORKFLOW-GUIDE.md](WORKFLOW-GUIDE.md) | 完整工作流指南 (1684 行) |
| [docs/COLLABORATIVE-DESIGN-PRINCIPLE.md](COLLABORATIVE-DESIGN-PRINCIPLE.md) | 协作设计原则 |

### 设计层快速入口

| 文件 | 说明 |
|------|------|
| [design/gdd/game-concept.md](../design/gdd/game-concept.md) | 游戏概念 — 核心幻想、MDA 分析 |
| [design/gdd/systems-index.md](../design/gdd/systems-index.md) | 系统索引 — 18 个系统的边界与约束 |
| [design/art/art-bible.md](../design/art/art-bible.md) | 艺术圣经 (2111 行) |
| [design/registry/entities.yaml](../design/registry/entities.yaml) | 实体注册表 |

### 架构层快速入口

| 文件 | 说明 |
|------|------|
| [docs/architecture/architecture.md](architecture/architecture.md) | 主架构 — 52 TR / 5 层 / 18 系统 (TD+LP 双签收) |
| [docs/architecture/tr-registry.yaml](architecture/tr-registry.yaml) | 技术需求注册表 |
| [docs/engine-reference/godot/VERSION.md](engine-reference/godot/VERSION.md) | Godot 4.6.2 版本锁定 |

### 生产层快速入口

| 文件 | 说明 |
|------|------|
| [production/session-state/active.md](../production/session-state/active.md) | 当前会话状态 |
| [production/session-logs/session-log.md](../production/session-logs/session-log.md) | 会话日志 |

---

## 二、游戏设计文档 (GDD) — 依赖关系图

> 18 个系统，5 层架构。实线箭头 = 运行时依赖，虚线 = 信号/事件订阅。

```mermaid
graph TB
    subgraph Presentation["🖥️ Presentation 层"]
        UI["#16 UI/HUD/航图界面"]
        VFX["#17 反馈/特效/音频 (VS)"]
    end

    subgraph Feature["⚔️ Feature 层"]
        NAV["#10 航行与路线风险"]
        EXPLORE["#11 探索/搜撤场景"]
        COMBAT["#12 战斗与威胁处理"]
        REPAIR["#13 世界修复与解锁"]
        SETTLE["#14 空港/村镇/集市"]
        PARTNER["#15 伙伴功能与关系"]
        ONBOARD["#18 新手引导 (VS)"]
    end

    subgraph Core["🔧 Core 层"]
        RES["#5 资源/货物/容量"]
        INTEL["#6 玩家知识与情报"]
        HUB["#7 飞艇家园 Hub"]
        MODULE["#8 飞艇模块/船体"]
        CHART["#9 航图与航线规划"]
    end

    subgraph Foundation["📦 Foundation 层"]
        REG["#1 内容数据/状态注册表"]
        PLATFORM["#2 平台与会话壳"]
        SAVE["#3 本地存档/持久化"]
        MOVE["#4 玩家移动与交互"]
    end

    subgraph Platform["🖧 Platform 层"]
        ENGINE["Godot 4.6.2 + GDScript + Web Export"]
    end

    %% Foundation 依赖
    MOVE --> PLATFORM
    SAVE --> REG
    RES --> REG
    INTEL --> REG
    HUB --> MOVE
    HUB --> SAVE

    %% Core 依赖
    CHART --> INTEL
    CHART --> HUB
    CHART --> REG
    MODULE --> HUB
    MODULE --> RES

    %% Core→Core 依赖
    CHART -.->|route_committed| NAV
    CHART -.->|route_enhanced| REPAIR
    INTEL -.->|knowledge_advanced| CHART
    INTEL -.->|ability_unlocked| CHART
    RES -.->|deposit_committed| REPAIR
    MODULE -.->|departure_readiness_changed| NAV

    %% Feature 依赖
    NAV --> CHART
    NAV --> MODULE
    NAV -.->|voyage_completed(EncounterContext)| EXPLORE
    EXPLORE --> NAV
    COMBAT --> EXPLORE
    COMBAT --> MODULE
    REPAIR --> RES
    REPAIR -.->|repair_completed| INTEL
    REPAIR -.->|repair_completed| CHART
    REPAIR -.->|repair_completed| SETTLE
    SETTLE --> REPAIR
    PARTNER --> HUB
    PARTNER --> INTEL
    ONBOARD --> HUB
    ONBOARD --> CHART
    ONBOARD --> EXPLORE
    ONBOARD --> REPAIR

    %% Presentation 依赖
    UI --> CHART
    UI --> HUB
    UI --> EXPLORE
    UI -.->|10 semantic events| VFX
    VFX --> REPAIR
    VFX --> COMBAT
    SAVE -.->|progress.routes snapshot| CHART
    SAVE -.->|progress.intel snapshot| INTEL
    SAVE -.->|progress.world-repair snapshot| REPAIR

    Platform --> Foundation
    Foundation --> Core
    Core --> Feature
    Feature --> Presentation
```

### GDD 文件清单

| # | 系统名 | 文件 | 层级 | 状态 |
|---|--------|------|------|------|
| 1 | 内容数据与状态注册表 | [content-data-state-registry.md](../design/gdd/content-data-state-registry.md) | Foundation | ✅ 已审查 |
| 2 | 平台与会话壳 | [platform-session-shell.md](../design/gdd/platform-session-shell.md) | Foundation | ✅ 已审查 |
| 3 | 本地存档与世界状态持久化 | [local-save-world-state-persistence.md](../design/gdd/local-save-world-state-persistence.md) | Foundation | ✅ 已审查 |
| 4 | 玩家移动与交互 | [player-movement-interaction.md](../design/gdd/player-movement-interaction.md) | Foundation | ✅ 已审查 |
| 5 | 资源、货物与容量 | [resources-goods-capacity.md](../design/gdd/resources-goods-capacity.md) | Core | ✅ 已审查 |
| 6 | 玩家知识与情报 | [player-knowledge-intel.md](../design/gdd/player-knowledge-intel.md) | Core | ✅ 已审查 |
| 7 | 飞艇家园 Hub | [airship-hub.md](../design/gdd/airship-hub.md) | Core | ✅ 已审查 |
| 8 | 飞艇模块与船体状态 | [airship-modules-hull-state.md](../design/gdd/airship-modules-hull-state.md) | Core | ✅ 已审查 |
| 9 | 航图与航线规划 | [chart-route-planning.md](../design/gdd/chart-route-planning.md) | Core | ✅ 已审查 |
| 10 | 航行与路线风险 | [navigation-route-risk.md](../design/gdd/navigation-route-risk.md) | Feature | ✅ 已审查 |
| 11 | 探索 / 搜撤场景 | [exploration-scavenge-scenario.md](../design/gdd/exploration-scavenge-scenario.md) | Feature | ✅ 已审查 |
| 12 | 战斗与威胁处理 | [combat-threat-handling.md](../design/gdd/combat-threat-handling.md) | Feature | ✅ 已审查 |
| 13 | 世界修复与解锁 | [world-repair-unlock.md](../design/gdd/world-repair-unlock.md) | Feature | ✅ 已审查 |
| 14 | 空港/村镇状态与集市交易 | [port-village-market.md](../design/gdd/port-village-market.md) | Feature | ✅ 已审查 |
| 15 | 伙伴功能与关系 | [partner-relationships.md](../design/gdd/partner-relationships.md) | Feature | ✅ 已审查 |
| 16 | UI / HUD / 航图界面 | [ui-hud-chart-interface.md](../design/gdd/ui-hud-chart-interface.md) | Presentation | ✅ 已审查 |
| 17 | 反馈、特效与音频语义 | *(Vertical Slice)* | Presentation | ⏳ VS |
| 18 | 新手引导与首轮闭环 | *(Vertical Slice)* | Feature | ⏳ VS |

### GDD 审查记录 (Review Logs)

| 系统 | Review Log |
|------|------------|
| #1 内容数据 | [review-log](../design/gdd/reviews/content-data-state-registry-review-log.md) |
| #2 平台会话壳 | [review-log](../design/gdd/reviews/platform-session-shell-review-log.md) |
| #3 本地存档 | [review-log](../design/gdd/reviews/local-save-world-state-persistence-review-log.md) |
| #4 移动交互 | [review-log](../design/gdd/reviews/player-movement-interaction-review-log.md) |
| #5 资源货物 | [review-log](../design/gdd/reviews/resources-goods-capacity-review-log.md) |
| #7 飞艇家园 | [review-log](../design/gdd/reviews/airship-hub-review-log.md) |
| #8 模块船体 | [review-log](../design/gdd/reviews/airship-modules-hull-state-review-log.md) |
| #9 航图规划 | [review-log](../design/gdd/reviews/chart-route-planning-review-log.md) |
| #11 探索搜撤 | [review-log](../design/gdd/reviews/exploration-scavenge-scenario-review-log.md) |
| #12 战斗威胁 | [review-log](../design/gdd/reviews/combat-threat-handling-review-log.md) |
| #13 世界修复 | [review-log](../design/gdd/reviews/world-repair-unlock-review-log.md) |
| #14 空港集市 | [review-log](../design/gdd/reviews/port-village-market-review-log.md) |
| 跨 GDD 审查 | [cross-review-2026-05-03](../design/gdd/gdd-cross-review-2026-05-03.md) |

> **VS** = Vertical Slice 阶段实现，MVP 不要求完整版本。

---

## 三、架构决策记录 (ADR) 全景

### ADR 信号流与状态机架构

```mermaid
graph TB
    subgraph Foundation["📦 Foundation ADRs"]
        ADR1["ADR-0001<br/>Autoload/Scene 架构<br/>9 Autoload + 启动顺序"]
        ADR2["ADR-0002<br/>Signal 通信协议<br/>{noun}_{verb_past} + typed params"]
        ADR3["ADR-0003<br/>存档系统<br/>SnapshotPackage + JSON"]
        ADR4["ADR-0004<br/>交互系统<br/>@abstract Interactable"]
        ADR5["ADR-0005<br/>资源池架构<br/>6 Pools + 13 Result 枚举"]
        ADR6["ADR-0006<br/>Web 平台约束<br/>GDScript + WebGL 2 + 单线程"]
    end

    subgraph Core["🔧 Core ADRs"]
        ADR7["ADR-0007<br/>IntelManager<br/>知识/能力 Dictionary 状态"]
        ADR8["ADR-0008<br/>Chart 状态机<br/>5-state + route_committed"]
        ADR9["ADR-0009<br/>Module/Hull System<br/>双字段 + 出航就绪"]
        ADR10["ADR-0010<br/>EncounterContext<br/>Navigation→Exploration 数据桥"]
    end

    subgraph Feature_Presentation["⚔️ Feature + 🖥️ Presentation ADRs"]
        ADR11["ADR-0011<br/>WorldRepair<br/>3-state 不可逆状态机"]
        ADR12["ADR-0012<br/>UIManager<br/>屏幕状态机 + 4层输入路由"]
    end

    %% Foundation→Core
    ADR1 -->|"Autoload #6"| ADR7
    ADR1 -->|"Autoload #9 Phase 3b"| ADR8
    ADR1 -->|"Autoload #7 Phase 4"| ADR9
    ADR2 -->|"signal 协议"| ADR7
    ADR2 -->|"signal 协议"| ADR8
    ADR2 -->|"signal 协议"| ADR9
    ADR2 -->|"signal 协议"| ADR10
    ADR3 -->|"progress.intel snapshot"| ADR7
    ADR3 -->|"progress.routes snapshot"| ADR8
    ADR3 -->|"progress.modules snapshot"| ADR9
    ADR5 -.->|"consume_intel 入口"| ADR7
    ADR6 -.->|"Web 约束"| ADR7
    ADR6 -.->|"Web 约束"| ADR8

    %% Core→Core
    ADR7 -->|"query_route_knowledge"| ADR8
    ADR7 -->|"query_ability_state"| ADR9
    ADR8 -->|"route_committed"| ADR10

    %% Core→Feature
    ADR7 -.->|"knowledge 查询"| ADR11
    ADR5 -.->|"commit_deposit"| ADR11
    ADR8 -.->|"route_enhanced"| ADR11
    ADR8 -.->|"UI 数据接口"| ADR12
    ADR9 -.->|"can_depart"| ADR12

    %% Feature internal
    ADR11 -.->|"repair_completed fan-out"| ADR8
    ADR10 -.->|"EncounterContext"| ADR11
```

### ADR 状态机一览

```mermaid
stateDiagram-v2
    direction LR

    state "Chart #9" as CH
    state "WorldRepair #13" as WR
    state "IntelManager #6" as INTEL
    state "UIManager #16" as UI

    state CH {
        LOADING --> BROWSING: COMPLETE
        LOADING --> ERROR: FAIL
        BROWSING --> ROUTE_SELECTED: SELECT
        ROUTE_SELECTED --> BROWSING: DESELECT
        ROUTE_SELECTED --> DEPARTURE_CONFIRMED: CONFIRM ▶
        ERROR --> LOADING: RETRY
        note right of DEPARTURE_CONFIRMED: 终端 (irreversible)
    }

    state WR {
        UNREVEALED --> KNOWN: player_arrived
        KNOWN --> REPAIRED: submit_deposit ▶
        note right of REPAIRED: 终端 (irreversible)
    }

    state INTEL {
        [*] --> knowledge_UNKNOWN
        knowledge_UNKNOWN --> knowledge_RUMORED: consume_intel
        knowledge_RUMORED --> knowledge_IDENTIFIED: observation
        knowledge_IDENTIFIED --> knowledge_VERIFIED: player_arrived
    }

    state UI {
        HUB --> CHART: open_chart
        CHART --> HUB: close_chart
        HUB --> EXPLORATION: voyage_start
        EXPLORATION --> SETTLEMENT: arrive
        SETTLEMENT --> HUB: return
    }
```

### 架构文档结构

```mermaid
graph LR
    subgraph 核心架构["核心架构文档"]
        MA["architecture.md<br/>主架构 v1<br/>52 TR / 5 层 / 18 系统"]
        TR2["tr-registry.yaml<br/>技术需求注册表"]
        ADR["ADR-0001~0012<br/>架构决策记录<br/>(12/17 complete)"]
    end

    subgraph 引擎参考["引擎参考 (3 引擎并行)"]
        GODOT["godot/<br/>Godot 4.6.2<br/>12 个参考文件"]
        UNITY["unity/<br/>Unity 6.3 LTS<br/>17 个参考文件"]
        UNREAL["unreal/<br/>UE 5.7<br/>17 个参考文件"]
    end

    subgraph 协作规范["协作与流程"]
        CP["COLLABORATIVE-DESIGN-PRINCIPLE.md<br/>协作设计原则"]
        WG["WORKFLOW-GUIDE.md<br/>工作流指南"]
        EX["examples/<br/>11 个会话示例"]
    end

    MA --> TR2
    MA -.-> GODOT
    CP --> WG
    WG --> EX
```

### 架构文档清单

| 文件 | 说明 |
|------|------|
| [architecture.md](architecture/architecture.md) | 主架构 — v1 签收 (TD+LP 双签收) |
| [tr-registry.yaml](architecture/tr-registry.yaml) | 52 条技术需求注册表 |
| [registry/architecture.yaml](registry/architecture.yaml) | 架构注册表 — 状态所有权、接口契约、禁止模式 (12 ADR 注册) |
| | **Foundation ADRs (6)** |
| [architecture/adr-0001-autoload-scene-boot-order.md](architecture/adr-0001-autoload-scene-boot-order.md) | ADR-0001: Autoload/Scene 架构 — 9 Autoload + 9-Phase 启动链 |
| [architecture/adr-0002-signal-communication-protocol.md](architecture/adr-0002-signal-communication-protocol.md) | ADR-0002: Signal 通信协议 — typed params + sync emit + max depth 2 |
| [architecture/adr-0003-save-system-snapshot-json.md](architecture/adr-0003-save-system-snapshot-json.md) | ADR-0003: 存档系统 — SnapshotPackage + Canonical JSON |
| [architecture/adr-0004-interaction-handler-abstract.md](architecture/adr-0004-interaction-handler-abstract.md) | ADR-0004: 交互系统 — @abstract Interactable + Registry |
| [architecture/adr-0005-resource-pool-system.md](architecture/adr-0005-resource-pool-system.md) | ADR-0005: 资源池 — 6 Pools + 13 ResourceResult 枚举 |
| [architecture/adr-0006-web-platform-constraints.md](architecture/adr-0006-web-platform-constraints.md) | ADR-0006: Web 平台约束 — WebGL 2 + 单线程 + 无 C# |
| | **Core ADRs (6)** |
| [architecture/adr-0007-intel-knowledge-ability-system.md](architecture/adr-0007-intel-knowledge-ability-system.md) | ADR-0007: IntelManager — 知识/能力 Dictionary 状态 + 多路径解锁 |
| [architecture/adr-0008-chart-route-state-machine.md](architecture/adr-0008-chart-route-state-machine.md) | ADR-0008: Chart 状态机 — 5-state + route_committed 不可逆承诺 |
| [architecture/adr-0009-airship-module-hull-system.md](architecture/adr-0009-airship-module-hull-system.md) | ADR-0009: Module/Hull System — 双字段 + 出航就绪三维检查 |
| [architecture/adr-0010-encounter-context-type.md](architecture/adr-0010-encounter-context-type.md) | ADR-0010: EncounterContext — Navigation→Exploration 数据桥 |
| [architecture/adr-0011-world-repair-state-machine.md](architecture/adr-0011-world-repair-state-machine.md) | ADR-0011: WorldRepair — 3-state 不可逆 + 批量提交 + 6 下游 fan-out |
| [architecture/adr-0012-ui-input-routing-dual-focus.md](architecture/adr-0012-ui-input-routing-dual-focus.md) | ADR-0012: UIManager — 屏幕状态机 + 模态栈 + 4 层输入路由 |
| [COLLABORATIVE-DESIGN-PRINCIPLE.md](COLLABORATIVE-DESIGN-PRINCIPLE.md) | 协作设计原则 |
| [WORKFLOW-GUIDE.md](WORKFLOW-GUIDE.md) | 完整工作流指南 (1684 行) |
| [examples/](examples/) | 11 个会话流程示例 |

### 引擎参考文档 (当前使用: Godot)

| 文件 | 说明 |
|------|------|
| [VERSION.md](engine-reference/godot/VERSION.md) | 版本锁定: Godot 4.6.2 |
| [breaking-changes.md](engine-reference/godot/breaking-changes.md) | Godot 4.4→4.6 破坏性变更 |
| [current-best-practices.md](engine-reference/godot/current-best-practices.md) | 当前最佳实践 |
| [deprecated-apis.md](engine-reference/godot/deprecated-apis.md) | 废弃 API |
| [modules/animation.md](engine-reference/godot/modules/animation.md) | 动画参考 |
| [modules/audio.md](engine-reference/godot/modules/audio.md) | 音频参考 |
| [modules/input.md](engine-reference/godot/modules/input.md) | 输入参考 |
| [modules/navigation.md](engine-reference/godot/modules/navigation.md) | 导航参考 (⚠️ NavigationServer2D 4.5+) |
| [modules/networking.md](engine-reference/godot/modules/networking.md) | 网络参考 |
| [modules/physics.md](engine-reference/godot/modules/physics.md) | 物理参考 |
| [modules/rendering.md](engine-reference/godot/modules/rendering.md) | 渲染参考 (WebGL 2 约束) |
| [modules/ui.md](engine-reference/godot/modules/ui.md) | UI 参考 (🔴 Dual-focus 4.6) |

---

## 四、审查与质量门禁流程

```mermaid
graph LR
    subgraph 设计审查["设计审查"]
        DR["/design-review<br/>单 GDD 审查"]
        CR["/consistency-check<br/>跨 GDD 一致性"]
        HR["/review-all-gdds<br/>全 GDD 综合审查"]
    end

    subgraph 架构审查["架构审查"]
        AR["/architecture-review<br/>架构完整性"]
        ADR["/architecture-decision<br/>ADR 创建"]
    end

    subgraph 代码审查["代码审查"]
        CODE["/code-review<br/>代码质量"]
        SEC["/security-audit<br/>安全检查"]
    end

    subgraph 阶段门禁["阶段门禁"]
        GC["/gate-check<br/>阶段转换验证"]
        SC["/scope-check<br/>范围蔓延检查"]
    end

    GDD["18 个 GDD"] --> DR
    DR --> CR
    CR --> HR
    HR --> AR
    AR --> ADR
    ADR --> CODE
    CODE --> GC

    DR -.-> SC
    CR -.-> SC
```

### 已完成审查记录

| 日期 | 审查 | 结果 | 报告 |
|------|------|------|------|
| 2026-04-30 | `/consistency-check` 跨 GDD 一致性 | 通过 | [cross-review-2026-04-30](../design/gdd/gdd-cross-review-2026-04-30-consistency.md) |
| 2026-05-03 | `/review-all-gdds` Phase 2 — 跨 GDD 一致性 | 5 BLOCKING/CRITICAL 已修复 | [phase2-report](../production/session-state/phase2-cross-gdd-consistency-report.md) |
| 2026-05-03 | `/review-all-gdds` Phase 3 — 游戏设计整体审查 | 通过 | [phase3-report](../production/session-state/phase3-game-design-holism-review.md) |
| 2026-05-04 | `/review-all-gdds` Phase 4 — 跨系统场景走查 | 通过 | [phase4-report](../production/session-state/phase4-cross-system-scenario-walkthrough.md) |
| 2026-05-04 | `/review-all-gdds` Phase 5 — 最终裁决 | PASS WITH NOTES | [phase5-report](../production/session-state/phase5-final-verdict.md) |
| 2026-05-04 | `/create-architecture` — 主架构 | TD+LP 双签收 | [architecture.md](architecture/architecture.md) |

---

## 五、Studio 基础设施

### Agent 体系 (49 个)

```mermaid
graph TB
    subgraph Tier1["Tier 1 — 总监 (Opus)"]
        CD["creative-director<br/>创意总监"]
        TD["technical-director<br/>技术总监"]
        PR["producer<br/>制作人"]
    end

    subgraph Tier2["Tier 2 — 部门主管 (Sonnet)"]
        GD["game-designer<br/>游戏设计师"]
        LP["lead-programmer<br/>首席程序员"]
        AD["art-director<br/>艺术总监"]
        AUD["audio-director<br/>音频总监"]
        ND["narrative-director<br/>叙事总监"]
        QL["qa-lead<br/>QA 主管"]
        RM["release-manager<br/>发布经理"]
        LL["localization-lead<br/>本地化主管"]
    end

    subgraph Tier3["Tier 3 — 专家 (Sonnet/Haiku)"]
        PROG["程序员 (5)<br/>gameplay/engine/ai/network/tools/ui"]
        DESIGN["设计师 (5)<br/>systems/level/economy/ux/live-ops"]
        ART["美术/音频 (4)<br/>technical-artist/sound-designer/writer/world-builder"]
        QA["质量 (4)<br/>qa-tester/security/performance/accessibility"]
        OPS["运维 (4)<br/>devops/analytics/community/release"]
        ENGINE["引擎专家 (15)<br/>godot(5)/unity(5)/unreal(5)"]
    end

    Tier1 --> Tier2
    Tier2 --> PROG
    Tier2 --> DESIGN
    Tier2 --> ART
    Tier2 --> QA
    Tier2 --> OPS
    Tier2 --> ENGINE
```

### Skill 体系 (70 个) — 按工作流阶段

```mermaid
graph LR
    subgraph 构思["💡 构思"]
        START["/start"]
        BRAINSTORM["/brainstorm"]
        MAP["/map-systems"]
    end

    subgraph 设计["📝 设计"]
        DSGN["/design-system"]
        QD["/quick-design"]
        UX["/ux-design"]
        ARTB["/art-bible"]
    end

    subgraph 审查["🔍 审查"]
        DR2["/design-review"]
        UX2["/ux-review"]
        CR2["/consistency-check"]
        AR2["/architecture-review"]
        RAG["/review-all-gdds"]
    end

    subgraph 架构["🏗️ 架构"]
        CA["/create-architecture"]
        AD["/architecture-decision"]
        CCM["/create-control-manifest"]
    end

    subgraph 规划["📋 规划"]
        CE["/create-epics"]
        CS["/create-stories"]
        SP["/sprint-plan"]
    end

    subgraph 实现["⚡ 实现"]
        DEV["/dev-story"]
        PROTOTYPE["/prototype"]
    end

    subgraph 质量["✅ 质量"]
        CR3["/code-review"]
        SA["/security-audit"]
        BC["/balance-check"]
        PP["/perf-profile"]
        TD2["/tech-debt"]
    end

    subgraph 发布["🚀 发布"]
        GC2["/gate-check"]
        SC2["/scope-check"]
        SMOKE["/smoke-check"]
        LC["/launch-checklist"]

    end

    构思 --> 设计
    设计 --> 审查
    审查 --> 架构
    架构 --> 规划
    规划 --> 实现
    实现 --> 质量
    质量 --> 发布
```

### 完整 Skill 分类

| 类别 | 技能 | 数量 |
|------|------|------|
| **入门引导** | `/start` `/help` `/project-stage-detect` `/setup-engine` `/adopt` `/onboard` | 6 |
| **创意构思** | `/brainstorm` `/map-systems` | 2 |
| **设计创作** | `/design-system` `/quick-design` `/ux-design` `/art-bible` `/reverse-document` | 5 |
| **设计审查** | `/design-review` `/ux-review` `/consistency-check` `/review-all-gdds` | 4 |
| **架构** | `/create-architecture` `/architecture-decision` `/architecture-review` `/create-control-manifest` | 4 |
| **规划** | `/create-epics` `/create-stories` `/sprint-plan` `/sprint-status` `/estimate` | 5 |
| **实现** | `/dev-story` `/prototype` `/story-readiness` `/story-done` | 4 |
| **代码质量** | `/code-review` `/security-audit` `/scope-check` `/tech-debt` `/simplify` | 5 |
| **测试** | `/qa-plan` `/smoke-check` `/soak-test` `/regression-suite` `/test-flakiness` `/test-evidence-review` `/playtest-report` `/test-setup` `/test-helpers` | 9 |
| **性能** | `/perf-profile` `/asset-audit` `/content-audit` | 3 |
| **平衡** | `/balance-check` `/propagate-design-change` | 2 |
| **发布** | `/gate-check` `/launch-checklist` `/release-checklist` `/changelog` `/patch-notes` `/day-one-patch` `/hotfix` | 7 |
| **回顾** | `/milestone-review` `/retrospective` | 2 |
| **本地化** | `/localize` | 1 |
| **Bug** | `/bug-report` `/bug-triage` | 2 |
| **团队** | `/team-audio` `/team-combat` `/team-level` `/team-live-ops` `/team-narrative` `/team-polish` `/team-qa` `/team-release` `/team-ui` | 9 |

---

## 六、规范与模板

### 路径规则 (11 个)

[`.claude/rules/`](../.claude/rules/) 目录下的代码规范，按文件类型自动激活：

| 规则文件 | 适用范围 |
|----------|----------|
| [ai-code.md](../.claude/rules/ai-code.md) | AI 行为树/状态机代码 |
| [data-files.md](../.claude/rules/data-files.md) | 数据文件 |
| [design-docs.md](../.claude/rules/design-docs.md) | 设计文档 (8 个必需节) |
| [engine-code.md](../.claude/rules/engine-code.md) | 引擎代码 |
| [gameplay-code.md](../.claude/rules/gameplay-code.md) | 游戏玩法代码 |
| [narrative.md](../.claude/rules/narrative.md) | 叙事文本 |
| [network-code.md](../.claude/rules/network-code.md) | 网络代码 |
| [prototype-code.md](../.claude/rules/prototype-code.md) | 原型代码 (宽松标准) |
| [shader-code.md](../.claude/rules/shader-code.md) | Shader 代码 |
| [test-standards.md](../.claude/rules/test-standards.md) | 测试标准 |
| [ui-code.md](../.claude/rules/ui-code.md) | UI 代码 |

### 核心配置文档

| 文件 | 说明 |
|------|------|
| [.claude/settings.json](../.claude/settings.json) | 项目设置 (权限/Hooks) |
| [.claude/docs/agent-roster.md](../.claude/docs/agent-roster.md) | Agent 完整名册 |
| [.claude/docs/agent-coordination-map.md](../.claude/docs/agent-coordination-map.md) | Agent 协调委托关系图 |
| [.claude/docs/coordination-rules.md](../.claude/docs/coordination-rules.md) | Agent 协调规则 |
| [.claude/docs/directory-structure.md](../.claude/docs/directory-structure.md) | 目录结构 |
| [.claude/docs/technical-preferences.md](../.claude/docs/technical-preferences.md) | 技术偏好 (待配置) |
| [.claude/docs/coding-standards.md](../.claude/docs/coding-standards.md) | 编码标准 |
| [.claude/docs/context-management.md](../.claude/docs/context-management.md) | 上下文管理策略 |
| [.claude/docs/review-workflow.md](../.claude/docs/review-workflow.md) | 审查工作流 |
| [.claude/docs/skills-reference.md](../.claude/docs/skills-reference.md) | Skill 参考 |
| [.claude/docs/director-gates.md](../.claude/docs/director-gates.md) | 总监门禁模式 |
| [.claude/docs/quick-start.md](../.claude/docs/quick-start.md) | 快速入门 |
| [.claude/docs/hooks-reference.md](../.claude/docs/hooks-reference.md) | Hooks 参考 |
| [.claude/docs/rules-reference.md](../.claude/docs/rules-reference.md) | 规则参考 |
| [.claude/docs/setup-requirements.md](../.claude/docs/setup-requirements.md) | 安装要求 |
| [.claude/agent-memory/lead-programmer/MEMORY.md](../.claude/agent-memory/lead-programmer/MEMORY.md) | Lead Programmer Agent 记忆 |

---

## 七、文档阅读路线图

### 新成员入门路径

```mermaid
graph TB
    A["1. README.md<br/>了解项目是什么"] --> B["2. docs/WORKFLOW-GUIDE.md<br/>理解完整工作流"]
    B --> C["3. design/gdd/game-concept.md<br/>理解游戏核心概念"]
    C --> D["4. design/gdd/systems-index.md<br/>了解所有系统"]
    D --> E["5. docs/architecture/architecture.md<br/>理解技术架构"]
    E --> F["6. .claude/docs/quick-start.md<br/>开始工作"]
```

### 按角色推荐阅读

| 角色 | 必读文档 | 选读 |
|------|----------|------|
| **新成员** | [README](../README.md) → [WORKFLOW-GUIDE](WORKFLOW-GUIDE.md) → [game-concept](../design/gdd/game-concept.md) | [examples/](examples/) |
| **游戏设计师** | [game-concept](../design/gdd/game-concept.md) → [systems-index](../design/gdd/systems-index.md) → [全部 GDD](../design/gdd/) | [reviews/](../design/gdd/reviews/) |
| **程序员** | [architecture](architecture/architecture.md) → [coding-standards](../.claude/docs/coding-standards.md) → [engine-reference/godot/](engine-reference/godot/) | [rules/](../.claude/rules/) |
| **美术** | [art-bible](../design/art/art-bible.md) → [game-concept](../design/gdd/game-concept.md) | [rendering](engine-reference/godot/modules/rendering.md) |
| **音频** | [game-concept](../design/gdd/game-concept.md) → GDD #17 (VFX/Audio) | [audio](engine-reference/godot/modules/audio.md) |
| **QA** | [全部 GDD](../design/gdd/) → [test-standards](../.claude/rules/test-standards.md) → [coding-standards](../.claude/docs/coding-standards.md) | [reviews/](../design/gdd/reviews/) |
| **制作人** | [WORKFLOW-GUIDE](WORKFLOW-GUIDE.md) → [architecture](architecture/architecture.md) → sprint 相关 | [session-logs/](../production/session-logs/) |

---

## 八、统计概览

```
文档分布 (按目录)

  design/          ████████████████░░░░  35 文件  (GDD + 审查 + 艺术)
  docs/            ████████████████████████████████  63 文件  (架构 + 引擎参考 + 示例)
  production/      ████░░░░░░░░░░░░░░░   7 文件  (会话状态 + 日志)
  .claude/         ██████████████████████████████████████████████████  123 文件  (Agent + Skill + 规则 + 模板)
  CCGS Framework/  ██████████████████████████████████████  85 文件  (测试框架)
  .github/         █░░░░░░░░░░░░░░░░░░░   3 文件  (Issue/PR 模板)
  src/             █░░░░░░░░░░░░░░░░░░░   1 文件  (占位)

  📊 总计: ~330 个 Markdown 文档 + 9 个配置/数据文件 (12 ADR 已写入)
```

---

## 九、待创建文档

根据 `/create-architecture` Phase 8 结论，以下文档待创建：

- [x] **17 个 ADR** (Architecture Decision Records) — `docs/architecture/` (12/17 complete)
  - [x] ADR-0001: Autoload/Scene 架构与启动顺序 ✅
  - [x] ADR-0002: 基于 Signal 的跨系统通信协议 ✅
  - [x] ADR-0003: 存档系统 — 快照包与 JSON 序列化 ✅
  - [x] ADR-0004: 交互系统 — @abstract Handler + Registry ✅
  - [x] ADR-0005: 资源池架构 — 6 Pools / Capacity Types / Terminal Deposit ✅
  - [x] ADR-0006: Web 平台约束与引擎兼容性 ✅
  - [x] ADR-0007: IntelManager 知识状态与能力解锁架构 ✅
  - [x] ADR-0008: Chart 航图路线状态机与出航承诺 ✅
  - [x] ADR-0009: AirshipModuleSystem 飞艇模块与船体伤害模型 ✅
  - [x] ADR-0010: EncounterContext 跨系统类型契约 ✅
  - [x] ADR-0011: WorldRepair 修复状态机与分批提交 ✅
  - [x] ADR-0012: UIManager 屏幕状态机/模态栈/输入路由 ✅
  - [ ] ADR-0013~0017: AirshipHub, Exploration, Combat, Settlement, Partner (Feature 层)
- [ ] **Control Manifest** — `docs/architecture/control-manifest.md`
- [ ] **Epics** — 按系统分组的 Epic 文件
- [ ] **技术偏好完整配置** — `.claude/docs/technical-preferences.md`
- [ ] **Sprint Plan** — 首个开发 Sprint 计划

---

> **提示**: 本文档使用 Mermaid 图表。在 VS Code 中安装 "Markdown Preview Mermaid Support" 插件，
> 或在 GitHub 上直接查看以渲染图表。也可使用 `npx mermaid-cli` 生成静态图片。
