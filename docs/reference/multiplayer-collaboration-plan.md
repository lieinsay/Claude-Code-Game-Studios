# 多人协作生产计划 — 云海织航 MVP

> 生成: 2026-05-10 | 最后更新: 2026-05-13
> 基于: 16 Epic × 115 Story 完整分解
> 用途: 分派给 3-5 名开发者并行推进的协作路线图
> 前提: ADR-0019 (Desktop C# Pivot) 已生效，所有新代码用 C# 写
> 当前状态: **Phase 0 完成 | Phase 1 Foundation 完成 | Content Registry #1 完成 | Platform Session Shell #2 完成 | Local Save Persistence #3 完成 | Player Movement & Interaction #4 完成 | Resources #5 完成 | Intel #6 完成 | Airship Hub #7 完成 | Modules/Hull #8 完成 | #9 已解锁 | BUG-005 已修复**

> **Readiness 基线**: 所有 115 个生产 Story 已补齐 Manifest 2026-05-09、ADR-0019 Desktop C# implementation contract、Type、Estimate、Test Evidence 与 C# test evidence 路径。多人并行实现时不得恢复旧 Web/GDScript 路径。
> **文档索引**: `docs/document-index.md` 已于 2026-05-13 同步到 #5/#6/#7/#8 Complete、#9 解锁、63 个 C# runner PASS 的当前基线。

---

## 一、假设团队规模

本文档按 **3 名开发者** 排期。5 人团队可以进一步拆分（标注了可再拆的点）。

| 角色 | 代号 | 职责 |
|------|------|------|
| Developer A | **D-A** | Foundation 系统 (注册表、存档、会话壳) |
| Developer B | **D-B** | Core 系统前半 (资源、情报、Hub、模块) |
| Developer C | **D-C** | Core 系统后半 + Feature 系统 (航图、航行、探索、战斗) |

---

## 二、总览：16 Epic 按人头分配

```
D-A (Foundation + 世界修复/集市/UI 后半)
  Epic #1  内容注册表        8 stories
  Epic #2  会话壳            7 stories
  Epic #3  存档持久化        8 stories
  Epic #13 世界修复         6 stories
  Epic #14 空港集市         6 stories
  Epic #16 UI/HUD (后3)     3 stories
  ─────────────────────────
  合计: 38 stories

D-B (Core 前半 + 伙伴/UI前半)
  Epic #4  玩家移动          7 stories
  Epic #5  资源货物          9 stories
  Epic #6  玩家情报          8 stories
  Epic #7  飞艇 Hub          8 stories
  Epic #15 伙伴关系          6 stories
  Epic #16 UI/HUD (前3)      3 stories
  ─────────────────────────
  合计: 41 stories

D-C (Core 后半 + Feature 层)
  Epic #8  飞艇模块          8 stories
  Epic #9  航图航线          8 stories
  Epic #10 航行风险          8 stories
  Epic #11 探索搜撤          6 stories
  Epic #12 战斗威胁          6 stories
  ─────────────────────────
  合计: 36 stories
```

---

## 三、Phase 0 — 环境准备 (✅ 已完成 — 2026-05-10)

**所有人同步完成，不依赖任何 Epic。**

```
Week 0 (2-3 天) — ✅ 全部完成
┌─────────────────────────────────────────────────────────┐
│ D-A  ☑ Godot 4.6.2 .NET 项目搭建                        │
│      ☑ project.godot C# feature 确认                     │
│      ☑ .csproj + .sln 模板确认                           │
│      ☑ 目录结构 scaffold (src/core, tests/unit, etc.)    │
│                                                         │
│ D-B  ☑ C# 编码规范落地                                   │
│      ☑ .editorconfig 配置                                │
│      ☑ dotnet build 验证通过                             │
│      ☑ 测试框架选型 + CI 配置                            │
│                                                         │
│ D-C  ☑ C# Foundation Parity 验证                         │
│      ☑ 9/9 Autoload + SessionBootChain 迁移到 C#         │
│      ☑ 70/70 parity checks 通过 (从 20 扩展到 70)         │
│      ☑ LEGACY_GDSCRIPT.md 审查                           │
│      ☑ Story-001 10/10 AC PASS                           │
│      ☑ Story-002 Schema Validation 11/11 AC PASS          │
│      ☑ Story-003 Content Lifecycle 6/6 AC PASS            │
│      ☑ Story-004 Reference Integrity 7/7 AC PASS          │
└─────────────────────────────────────────────────────────┘
```

**门禁**: `dotnet build CloudWeaverVoyage.sln` PASS ✅ + 55 个 C# test runner PASS ✅ + Foundation Parity 70/70 PASS ✅ + Registry #1 全部 Story evidence PASS ✅ + Persistence #3 全部 8 个 Story evidence PASS ✅ + Resources #5 全部 9 个 Story evidence PASS ✅ + Intel #6 全部 8 个 Story evidence PASS ✅

---

## 四、Phase 1 — Foundation Sprint (Week 1-3)

### 第 1 周: 并行启动

```
D-A ───────────────────────────────────────────────────────
  Epic #1 Content Registry
  ┌──────────────────────────────────────────────────────┐
  │ Story 001 ☑ ID Registry Core + Query Engine   (L, 2d)│  ← ✅ 完成
  │ Story 002 ☑ Schema Validation                 (L, 2d)│  ← ✅ 完成
  │   ↓ (001+002 完成后)                                  │
  │ Story 003 ☑ Content Lifecycle                 (L, 1d)│  ← ✅ 完成
  │ Story 004 ☑ Reference Integrity               (L, 1d)│  ← ✅ 完成
  │ Story 005 ☑ Domain Loading & Decision Gating  (I, 1d)│  ← ✅ 完成
  │ Story 006 ☑ Diagnostic System                 (L, 1d)│  ← ✅ 完成
  │ Story 007 ☑ Diagnostic UI (Dev Tools)        (U, 2d)│  ← ✅ 完成
  │ Story 008 ☑ Player-Facing Boundary            (I, 1d)│  ← ✅ 完成
  └──────────────────────────────────────────────────────┘

D-B ───────────────────────────────────────────────────────
  Epic #2 Session Shell
  ┌──────────────────────────────────────────────────────┐
  │ Story 001-007 ☑ Platform Session Shell        (Done)│  ← ✅ 完成
  │ 结果: #3 Persistence 与 #4 Movement 均已完成       │
  └──────────────────────────────────────────────────────┘

D-C ───────────────────────────────────────────────────────
  等待 #3 完成（D-C 的 Core 系统依赖 Persistence）
  ┌──────────────────────────────────────────────────────┐
  │ □ 阅读 GDD + ADR 全集                                │
  │ □ 搭建本地开发/测试环境                                │
  │ □ 准备 EncounterContext 类型桩                        │
  └──────────────────────────────────────────────────────┘
```

### 第 2-3 周: Registry 解锁下游

```
#1 已完成 (D-A 已交出 Registry API) 之后:

D-A ───────────────────────────────────────────────────────
  Epic #3 Persistence
  ┌──────────────────────────────────────────────────────┐
  │ Story 001 ☑ Staging→Verify→Promotion 管道    (L, 2d)│  ← ✅ 完成
  │ Story 002 ☑ Snapshot Package Contract         (L, 1d)│  ← ✅ 完成
  │ Story 003 ☑ Storage Capability Detection      (L, 1d)│  ← ✅ 完成
  │ Story 004 ☑ Continue Availability             (I, 1d)│  ← ✅ 完成
  │ Story 005 ☑ Version Migration                 (L, 1d)│  ← ✅ 完成
  │ Story 006 ☑ Backup Failover                   (L, 1d)│  ← ✅ 完成
  │ Story 007 ☑ Artifact Isolation                (I, 1d)│  ← ✅ 完成
  │ Story 008 ☑ Desktop Lifecycle Integration     (I, 1d)│  ← ✅ 完成
  └──────────────────────────────────────────────────────┘

D-B ───────────────────────────────────────────────────────
  Epic #2 已完成 + Epic #4 Movement 已完成
  ┌──────────────────────────────────────────────────────┐
  │ Epic #2:                                             │
  │ Story 001-007 ☑ Platform Session Shell       (Done) │  ← ✅ 完成
  │                                                       │
  │ Epic #4:                                             │
  │ Story 001-007 ☑ Player Movement & Interaction(Done) │  ← ✅ 完成
  └──────────────────────────────────────────────────────┘

D-C ───────────────────────────────────────────────────────
  #1 已完成 → 可以开始阅读 Registry API 文档
  #3 已完成 → 可以开始写 #5 和 #6 的桩代码
  #2 已完成 → #4 Movement 已完成，#7 Hub 已接入移动/交互契约
```

**Phase 1 门禁**:
- [x] `dotnet build` PASS
- [x] Registry 查询/API + Schema Validation 测试 PASS (`tests/unit/registry`: Story-002 11/11)
- [x] Registry Content Lifecycle 测试 PASS (`tests/unit/registry/ContentLifecycleTest.csproj`: Story-003 6/6)
- [x] Persistence Story-001~008 自动化测试 PASS (`SavePipeline`, `SnapshotPackage`, `StorageCapability`, `ContinueAvailability`, `Migration`, `BackupFailover`, `ArtifactIsolation`, `DesktopLifecycle`)
- [x] Platform Session Shell 测试 PASS (`tests/unit/session` + `tests/integration/session`: 55/55)
- [x] Movement C# 合同验证 PASS (`tests/unit/movement` + `tests/integration/movement`: 58/58)
- [x] Airship Hub #7 C# 合同复审 PASS (`tests/integration/hub`: 38/38)
- [x] Modules/Hull #8 C# 合同复审 PASS (`tests/unit/modules` + `tests/integration/modules`: 36/36)
- [ ] Movement + Hub Godot 灰盒场景手动验证 PASS（C# 合同已完成，实机场景证据随 UI/场景接入执行）

---

## 五、Phase 2 — Core Layer (Week 4-7)

### 关键解锁事件

```
Phase 1 完成时刻，解锁关系:

  #1 Registry ✅
  #2 Shell  ✅
  #3 Persistence ✅
  #4 Movement ✅

  此时以下 Epic 全部解锁，可大规模并行:

  ┌──────────────┬──────────────┬──────────────┐
  │     D-A       │     D-B       │     D-C       │
  ├──────────────┼──────────────┼──────────────┤
  │ #5 资源货物   │ #6 玩家情报   │ #8 飞艇模块   │
  │   (等 #1+#3)  │   (等 #1+#3)  │   (等 #3+#5+#7)│
  │              │              │              │
  │ #13 世界修复  │ #7 飞艇 Hub ✅ │ #9 航图航线   │
  │   (等 #3+#5)  │   (已完成)     │   (等 #1+#3+#6)│
  └──────────────┴──────────────┴──────────────┘
```

### 第 4 周: 三方并行 — 第一批

```
D-A: Epic #5 Resources (前 4 个 Story)
────────────────────────────────────────
  Story 001 □ Resource Identity + Stack Merge     (L, 2d)
  Story 002 □ Dual Capacity System                (L, 2d)  ← 与 001 并行
  Story 003 □ Cargo Model + Unpack                (L, 1d)  ← 等 001
  Story 004 □ Weight + Mass Tracking              (L, 1d)  ← 等 002

  产出: ResourcePool 6-pool 模型 + 堆叠合并 + 双容量系统


D-B: Epic #6 Intel + Epic #7 Hub (各前 2 个)
────────────────────────────────────────
  Epic #6:
  Story 001 □ Pattern Knowledge State Machine     (L, 2d)
  Story 002 □ Location Knowledge + Rumor System   (L, 2d)  ← 与 001 并行

  Epic #7:
  Story 001 ✅ Hub Scene Foundation + State Machine (L)
  Story 002 ✅ Station Registration + Routing       (L)


D-C: Epic #8 Modules + Epic #9 Chart (各前 2 个)
────────────────────────────────────────
  等 D-A #5 Story 001-002 完成 →
  Epic #8:
  Story 001 □ Slot State Machine + Dual-Field     (L, 2d)
  Story 002 □ Module Swap Two-Phase               (L, 1d)  ← 等 001

  等 D-B #6 Story 001-002 完成 →
  Epic #9:
  Story 001 □ Chart State Machine + Content Gate  (L, 2d)
  Story 002 □ Route Visibility + Selectability    (L, 2d)  ← 等 001
```

### 第 5 周: 三方并行 — 第二批

```
D-A: Epic #5 剩余 + 开始 Epic #13
────────────────────────────────────────
  Epic #5:
  Story 005 □ Core Atomic Operations              (L, 2d)  ← 等 001-004
  Story 006 □ State Machine + Pool Transitions    (L, 1d)  ← 等 005

  Epic #13 World Repair:
  Story 001 □ Repair State Machine + Node Lifecycle (L, 2d)  ← 等 #5 001-005
  Story 002 □ Deposit Validation + Batch Commit     (L, 2d)  ← 等 001


D-B: Epic #6 剩余 + Epic #7 剩余
────────────────────────────────────────
  Epic #6:
  Story 003 ☑ Ability Multi-Path Unlock           (L, 2d)  ← ✅ 完成
  Story 004 ☑ IntelConsumeResult Algorithm        (L, 2d)  ← ✅ 完成

  Epic #7:
  Story 003 ✅ Room Gating + Module Slot Display   (L)
  Story 004 ✅ Departure Modes + Confirmation Gate (L)


D-C: Epic #8 剩余 + Epic #9 剩余
────────────────────────────────────────
  Epic #8:
  Story 003 □ Hull Integrity + Bands + Scars      (L, 2d)  ← 等 001
  Story 004 □ Furnace Capacity + Departure Readiness(L, 1d) ← 等 001+003

  Epic #9:
  Story 003 □ Two-Step Departure Confirmation     (L, 2d)  ← 等 001-002
  Story 004 □ Route Display Ordering + Filtering  (L, 1d)  ← 等 002
```

### 第 6 周: 三方并行 — 第三批

```
D-A: Epic #5+#13 收尾
────────────────────────────────────────
  Epic #5:
  Story 007 □ Specialized Operations             (I, 2d)  ← 等 001-006
  Story 008 □ Signal Contract + Reentry Guard    (I, 1d)  ← 等 007
  Story 009 □ Persistence + External Integration (I, 2d)  ← 等 008

  Epic #13:
  Story 003 □ Formulas: Progress/Completion/Enhance(L, 2d) ← 等 001-002
  Story 004 □ Signal Events + Downstream Chain   (I, 2d)  ← 等 003


D-B: Epic #6+#7 收尾
────────────────────────────────────────
  Epic #6:
  Story 005 ☑ Upstream Event Receivers           (L, 1d)  ← ✅ 完成
  Story 006 ☑ Downstream Query Interface         (L, 1d)  ← ✅ 完成
  Story 007 ☑ Signal Contract + Non-Degradation  (I, 2d)  ← ✅ 完成
  Story 008 ☑ Persistence + MVP Bootstrap        (I, 2d)  ← ✅ 完成

  Epic #7:
  Story 005 ✅ Arrival Flow + State Continuity    (L)
  Story 006 ✅ Life Trace Anchors                 (L)
  Story 007 ✅ Signal Contract + HUD Integration  (I)
  Story 008 ✅ Scene Persistence + Transition     (I)


D-C: Epic #8+#9 收尾 → 启动 Epic #10
────────────────────────────────────────
  Epic #8:
  Story 005 □ Cargo Bay Effective Volume        (I, 2d)  ← 等 001-004
  Story 006 □ Module Signal Contract            (I, 1d)  ← 与 005 并行
  Story 007 □ Module Snapshot Persistence       (I, 2d)  ← 等 006
  Story 008 □ Scout Acquisition + Combat Damage (I, 2d)  ← 等 007

  Epic #9:
  Story 005 □ Snapshot Validation + Persistence (I, 2d)  ← 等 001-004
  Story 006 □ UIManager Query + Signal Contract (I, 2d)  ← 与 005 并行
  Story 007 □ External State Change Response    (I, 2d)  ← 等 005-006
  Story 008 □ Edge Cases + Error Recovery       (I, 1d)  ← 等 007

  Epic #10 Navigation (等 #5+#6+#7+#8+#9 全部完成):
  Story 001 □ Voyage State Machine + Preflight   (L, 2d)
  Story 002 □ Voyage Duration + Check Timing     (L, 2d)  ← 与 001 并行
```

### 第 7 周: Core 收尾 + Feature 启动

```
D-A: Epic #13 收尾 + 启动 Epic #14
────────────────────────────────────────
  Epic #13:
  Story 005 □ Persistence + State Recovery       (I, 1d)  ← 等 001-004
  Story 006 □ Edge Cases + Visual/Audio          (I, 2d)  ← 等 004-005

  Epic #14 Settlement (等 #13 Story 003):
  Story 001 □ Settlement State Machine + Stall   (L, 2d)
  Story 002 □ Purchase Flow + Price Formula      (L, 2d)  ← 与 001 并行


D-B: Epic #15 Partner + 启动 UI
────────────────────────────────────────
  Epic #15 Partner (等 #1+#3+#5+#6+#7+#9):
  Story 001 □ Cat State Machine + Presence       (L, 2d)
  Story 002 □ Scout Sniff Algorithm + Clamp      (L, 2d)  ← 与 001 并行
  Story 003 □ Naming System + Nest Accumulation  (L, 2d)  ← 等 001

  Epic #16 UI 前半 (等 #5+#8+#9):
  Story 001 □ Screen State Machine + Flow        (L, 3d)


D-C: Epic #10 继续
────────────────────────────────────────
  Epic #10:
  Story 003 □ Scout Preview + Hidden Tag Reveal  (L, 2d)  ← 等 001-002
  Story 004 □ Damage Accumulation + Hull Band    (L, 2d)  ← 等 002
  Story 005 □ Encounter Resolution + Dispatch    (L, 2d)  ← 等 003-004
```

**Phase 2 门禁**:
- [ ] 5 Core Epic 全部 Story 单元测试 PASS
- [ ] Hub → Chart → Navigation 集成流程手动走通
- [ ] Resources 6-pool 运算全部 PASS
- [ ] Intel 4 态知识状态机 PASS

---

## 六、Phase 3 — Feature Layer (Week 8-10)

### 解锁条件

```
Phase 2 完成时刻:

  #5 Resources    ✅
  #6 Intel        ✅
  #7 Hub          ✅
  #8 Modules      ✅
  #9 Chart        ✅
  #10 Navigation  ✅ ← D-C 刚完成

  此时 Feature 层 5 个 Epic 全部可启动:

  #11 Exploration (等 #4+#5+#6+#8+#10 → 全部满足)
  #12 Combat      (等 #5+#8+#11 → 等 #11 完成)
  #13 Repair      (等 #3+#5+#6+#9 → 全部满足，D-A 已在做)
  #14 Settlement  (等 #3+#4+#5+#13 → 等 #13 完成)
  #15 Partner     (等 #1+#3+#5+#6+#7+#9 → 全部满足，D-B 已在做)
```

### 第 8 周: Feature 大规模并行

```
D-A: Epic #14 继续 + Epic #13 收尾
────────────────────────────────────────
  Epic #13:
  Story 005 □ Persistence + State Recovery       (I, 1d)  ← 如果 Week 7 未完成
  Story 006 □ Edge Cases + Visual/Audio          (I, 2d)

  Epic #14:
  Story 003 □ Repair-Driven Unlock + NPC State   (L, 2d)  ← 等 #13 Story 004
  Story 004 □ Signal + Resources Integration     (I, 2d)  ← 等 003


D-B: Epic #15 继续 + Epic #16 继续
────────────────────────────────────────
  Epic #15:
  Story 004 □ Hub Event + Intel API Integration  (I, 2d)  ← 等 001-003
  Story 005 □ Persistence + State Recovery       (I, 1d)  ← 等 003
  Story 006 □ Edge Cases + R15 Guards            (I, 2d)  ← 等 004-005

  Epic #16:
  Story 002 □ Modal Stack + Input Routing        (L, 2d)  ← 等 001
  Story 003 □ HUD Update + Panel Lifecycle       (L, 2d)  ← 与 002 并行


D-C: Epic #11 Exploration (全部 6 个 Story)
────────────────────────────────────────
  Story 001 □ State Machine + Phase Transitions  (L, 2d)
  Story 002 □ Search/Scavenge + Intel Formulas   (L, 2d)  ← 与 001 并行
  Story 003 □ Threat Trigger + Scout Preview     (L, 2d)  ← 等 001
  Story 004 □ EncounterContext + ARRIVING Entry  (I, 2d)  ← 等 001+#10
  Story 005 □ Extraction + Settlement Loss       (I, 2d)  ← 等 002
  Story 006 □ Persistence + Session Recovery     (I, 2d)  ← 等 004-005
```

### 第 9 周: Feature 后半

```
D-A: Epic #14 收尾
────────────────────────────────────────
  Epic #14:
  Story 005 □ Persistence + State Recovery       (I, 1d)  ← 等 001-004
  Story 006 □ Edge Cases + UI + Defensive        (I, 2d)  ← 等 004-005


D-B: Epic #16 UI 收尾
────────────────────────────────────────
  Epic #16:
  Story 004 □ Upstream Data Contracts            (I, 3d)  ← 等 #11+#13+#14 基础就绪
  Story 005 □ Animation Timing + Events          (I, 2d)  ← 等 004
  Story 006 □ Edge Cases + A11y                  (I, 2d)  ← 等 004-005


D-C: Epic #12 Combat (等 #11 完成)
────────────────────────────────────────
  Story 001 □ Combat State Machine + Threat Queue(L, 2d)
  Story 002 □ Response Resolution + Settlement   (L, 2d)  ← 等 001
  Story 003 □ Damage/Module/Knockback Formulas   (L, 2d)  ← 等 001
  Story 004 □ combat_result Contract + Signals   (I, 2d)  ← 等 002-003
  Story 005 □ Data-Driven Threat Config          (I, 1d)  ← 等 001
  Story 006 □ Edge Cases + Defensive             (I, 1d)  ← 等 004-005
```

### 第 10 周: 收尾 + 集成

```
全员: 集成测试 + 横向修复
────────────────────────────────────────
  D-A □ Registry + Persistence + 世界修复 + 集市 集成测试
  D-B □ Hub + 伙伴 + UI 集成测试
  D-C □ 航图 → 航行 → 探索 → 战斗 → 返回 全链路测试

  □ 核心循环: Hub → Chart → Navigation → Exploration → Combat → Extraction → Repair → Settlement
  □ 存档恢复: 全状态快照 roundtrip
  □ CI 全部 PASS
```

**Phase 3 门禁**:
- [ ] 核心循环可完整走通（手动）
- [ ] 存档往返测试 PASS（全部 8 个快照包）
- [ ] 信号协议全线 PASS（信号扇出 + cascade depth ≤2）
- [ ] 12 个屏幕状态机测试 PASS

---

## 七、Phase 4 — Presentation + Polish (Week 11-12)

```
D-A + D-B: Epic #16 UI/HUD 完成
────────────────────────────────────────
  □ 12 屏 UI 全部可用
  □ 4 层输入路由验证
  □ HUD 脏标记批量更新实测 <1ms
  □ 无障碍（WCAG AA 对比度）验证

D-C: 性能分析 + 优化
────────────────────────────────────────
  □ 帧预算验证 (16ms)
  □ 内存预算验证 (512MB)
  □ 场景切换 <500ms
  □ 存档 p95 <50ms

后续 (Vertical Slice，需先创建 ADR-0016/0017):
  □ Epic #17 Feedback/Audio
  □ Epic #18 Onboarding
```

---

## 八、Story 内并行度标注

每个 Story 文件内部也标注了可以进一步并行的子任务。以下是关键 Story 的拆解示例：

### 示例: Epic #1 Story 001 (ID Registry Core)

```
可并行的子任务 (2 人):
  人 A □ ID schema 定义 (12 种内容类型)
  人 B □ Query engine 实现 (5 种查询结果区分)

  串行:
  人 A+B 完成后 → □ Bootstrap 数据写入
                → □ 单元测试
```

### 示例: Epic #3 Story 001 (Save Pipeline)

```
可并行的子任务 (2 人):
  人 A □ Staging buffer 实现
  人 B □ SHA-256 校验逻辑

  串行:
  人 A+B 完成后 → □ 原子 promote 逻辑
                → □ 测试
```

### 示例: Epic #11 Story 002 (Search Formulas)

```
可并行的子任务 (2 人):
  人 A □ search_yield 公式 (加权骰子)
  人 B □ intel_yield 公式 (情报产出)

  串行:
  人 A 完成后 → □ 自由搜索保证 (空结果不消耗次数)
  人 B 完成后 → □ scout_preview_level 映射 (η → 3 档)
```

---

## 九、关键集成点 (Integration Gates)

```
 Gate 1 — Week 3 末
 ┌─────────────────────────────────────────────┐
 │ Registry API 可用 + Persistence Pipeline 可用 │
 │ 验证: dotnet build + 单元测试 PASS            │
 │ 参与: D-A (交付) + D-B/D-C (验收)             │
 └─────────────────────────────────────────────┘

 Gate 2 — Week 6 末
 ┌─────────────────────────────────────────────┐
 │ Core 5 系统 API 全部可用                      │
 │ 验证: Hub→Chart→Navigation 流程走通           │
 │ 参与: D-B (Hub/Intel) + D-C (Chart/Nav)       │
 │        + D-A (Resources 消费验证)              │
 └─────────────────────────────────────────────┘

 Gate 3 — Week 9 末
 ┌─────────────────────────────────────────────┐
 │ Feature 5 系统 API 全部可用                    │
 │ 验证: 探索→战斗→世界修复 全链路走通            │
 │ 参与: D-C (Exploration/Combat)                │
 │        + D-A (WorldRepair/Settlement)          │
 │        + D-B (Partner 集成验证)                │
 └─────────────────────────────────────────────┘

 Gate 4 — Week 11 末
 ┌─────────────────────────────────────────────┐
 │ 完整核心循环可玩 + 存档可恢复                  │
 │ 验证: 手动 playtest 1 小时                    │
 │ 参与: 全员                                    │
 └─────────────────────────────────────────────┘
```

---

## 十、Story 开工条件速查表

下表列出**每个 Epic 的第一个 Story 需要等什么**。内部 Story 依赖见各 Epic 文件。

| Epic | 第一个 Story | 阻塞条件 | 满足于 |
|------|-------------|----------|--------|
| #1 Registry | 001 | 无 | Phase 0 完成即刻 |
| #2 Shell | 001 | 无 | ✅ Complete — 2026-05-11 |
| #3 Persistence | 001-008 | #1 + #2 | ✅ Complete — 2026-05-12 |
| #4 Movement | 001-007 | #2 输入路由 | ✅ Complete — 2026-05-12 |
| #5 Resources | 001-009 | #1 稳定 ID + #3 快照契约 | ✅ Complete — 2026-05-13 |
| #6 Intel | 001-008 | #1 稳定 ID + #3 快照契约 | ✅ Complete — 2026-05-13 |
| #7 Hub | 001-008 | #1 ID + #3 快照 + #4 移动 | ✅ Complete + reviewed — 2026-05-12 |
| #8 Modules | 001-008 | #3 快照 + #5 池定义 + #7 槽位注册 | ✅ Complete + reviewed — 2026-05-13 |
| #9 Chart | 001 | #1 ID + #3 快照 + #6 知识状态 | 当前可开工 |
| #10 Navigation | 001 | #5+#6+#7+#8+#9 全部 | Week 6-7 |
| #11 Exploration | 001 | #4+#5+#6+#8+#10 全部 | Week 8 |
| #12 Combat | 001 | #5+#8+#11 全部 | Week 9 |
| #13 WorldRepair | 001 | #3+#5+#6+#9 全部 | Week 5 |
| #14 Settlement | 001 | #3+#4+#5+#13 Story 003 | Week 7-8 |
| #15 Partner | 001 | #1+#3+#5+#6+#7+#9 全部 | Week 7 |
| #16 UI | 001 | #5+#8+#9 基础 API | Week 7 |

---

## 十一、风险缓冲

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| Registry schema 后期变更 | 中 | 高: 14 系统受影响 | Phase 1 完成后 schema freeze |
| Persistence 序列化格式变更 | 低 | 高: 8 系统受影响 | Phase 1 内确定 Canonical JSON 格式 |
| 全链路集成时信号协议不匹配 | 中 | 中: 返工 2-3 天 | Gate 1-3 各做一次信号 contract 验证 |
| 人员不足 (实际 <3 人) | 中 | 高: 工期 ×1.5 | 砍 Feature 层到最小 (仅 #11+#13)，推迟 #14+#15 |
| Godot .NET 构建问题 | 低 | 中: 阻塞全员 | Phase 0 彻底验证 + CI 每日构建 |

---

## 十二、如果只有 1-2 人

**单人模式**: 严格按关键路径串行

```
#1 → #2 → #3 → #4 → #5 → #6 → #7 → #8 → #9 → #10 → #11 → #12 → #13 → #14 → #15 → #16
预计: 22-26 周

可在任一 Phase 内并行 2 个 Epic (如果你能同时维护两个上下文)
```

**双人模式**:

```
人 A: #1 → #2 → #3 → #5 → #8 → #10 → #13 → #14 → #16
人 B: #1 → #2 → #4 → #6 → #7 → #9 → #11 → #12 → #15
预计: 14-16 周
```
