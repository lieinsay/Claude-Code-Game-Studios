# Active Design Session

<!-- STATUS -->
Epic: Pre-Production
Feature: Content Registry
Task: 🚧 Story-007 Diagnostic UI — Dev Tools In Progress
<!-- /STATUS -->

## Session Marker — Story Start 2026-05-11

- Story: `production/epics/content-registry/story-007-diagnostic-ui.md` — Story 007: Diagnostic UI — Dev Tools
- Status: In Progress
- Pre-development marker only; implementation starts after this marker is committed and pushed.

## Session Extract — /story-done 2026-05-10

- Verdict: COMPLETE
- Story: `production/epics/content-registry/story-003-content-lifecycle.md` — Story 003: Content Lifecycle
- Tech debt logged: None
- Next recommended: `production/epics/content-registry/story-004-reference-integrity.md` — Story 004: Reference Integrity

## Session Extract — /dev-story 2026-05-10

- Story: `production/epics/content-registry/story-003-content-lifecycle.md` — Story 003: Content Lifecycle
- Files changed: `src/core/content/Registry.cs`, `tests/unit/registry/ContentLifecycleTest.csproj`, `tests/unit/registry/ContentLifecycleProgram.cs`, `tests/unit/registry/IdRegistryCoreTest.csproj`, `CloudWeaverVoyage.sln`, `production/epics/content-registry/story-003-content-lifecycle.md`, `production/session-state/active.md`
- Test written: `tests/unit/registry/ContentLifecycleTest.csproj` — 6/6 Story-003 AC checks passing
- Blockers: None
- Next: `/code-review src/core/content/Registry.cs tests/unit/registry/ContentLifecycleProgram.cs` then `/story-done production/epics/content-registry/story-003-content-lifecycle.md`

### 全量 Story Readiness 元数据收口 — 2026-05-10

- [x] `production/epics/**/*.md` — 115 个 Story 统一 `Manifest Version: 2026-05-09`
- [x] 115 个 Story 补齐 ADR-0019 Desktop Godot .NET/C# implementation contract
- [x] Story test evidence 从 legacy `.gd` 路径翻译为 C# `.csproj` evidence path
- [x] 补齐缺失 `Estimate` 的 UI story，并保留 Type/Test Evidence gate
- [x] `production/epics/content-registry/story-003-content-lifecycle.md` — 范围校正: Lifecycle 提供 status/migration hint；Deprecated/Retired 引用错误由 Story-004 Reference Integrity 判定
- [x] `docs/document-index.md`, `docs/reference/production-flowchart.md`, `docs/reference/multiplayer-collaboration-plan.md` — reference 状态同步
- [x] `git diff --check` — PASS
- [x] `dotnet run --project tests/unit/registry/IdRegistryCoreTest.csproj` — 11/11 PASS
- [x] `dotnet build CloudWeaverVoyage.sln --no-restore` — PASS (0 errors; NU1900 SSL advisory warnings only)

### Foundation Layer: content-registry Story-002 Done — 2026-05-10

- [x] `src/core/content/Registry.cs` — Schema Validation: definition_validity U/K/R/S flags, required fields, controlled vocabularies, runtime field denylist, read-only write rejection, deep-copy query boundary
- [x] `tests/unit/registry/Program.cs` — Story-002 acceptance checks expanded to 11/11 PASS, including nested typed-dictionary runtime-field regression coverage
- [x] `production/epics/content-registry/story-002-schema-validation.md` — 状态更新: Ready → Done
- [x] `production/epics/content-registry/EPIC.md` — Story-001/002 状态同步为 Done
- [x] `dotnet run --project tests/unit/registry/IdRegistryCoreTest.csproj` — 11/11 PASS
- [x] `dotnet build CloudWeaverVoyage.sln --no-restore` — PASS (0 errors, warnings only)

### Foundation Layer: content-registry Story-001 Done — 2026-05-10

- [x] `src/core/content/Registry.cs` — 升级: `MaxQueryResultCount` 分页 + `domainKindMap` 域隔离 + `ApplyResultLimit`
- [x] `tests/unit/registry/IdRegistryCoreTest.csproj` — 新增测试项目: 10/10 AC 全部 PASS
- [x] `tests/unit/registry/Program.cs` — Story-001 10 个 AC 测试
- [x] `production/epics/content-registry/story-001-id-registry-core-query.md` — 状态更新: Ready → Done
- [x] `CloudWeaverVoyage.sln` — 新增 IdRegistryCoreTest 项目引用
- [x] `dotnet run --project tests/unit/registry` — 10/10 PASS

### C# Autoload 全量迁移 — 2026-05-10 完成

- [x] `src/core/InteractionRegistry.cs` — 5 状态焦点机 + 注册/取消注册 + FocusChanged 事件
- [x] `src/core/ResourcesManager.cs` — 6 资源池 + "fill fullest first" 合并 + SupplyClassCapacity
- [x] `src/core/IntelManager.cs` — KnowledgeState 机 + RevealRumor (confidence clamp) + PatternObserved
- [x] `src/core/ChartManager.cs` — ChartState 机 (Idle→DepartureLocked) + RouteSelectability + RouteCommitted
- [x] `src/feature/WorldRepair.cs` — RepairState 机 + CommitDeposit + RepairCompleted 扇出信号
- [x] `src/presentation/UIManager.cs` — 12 屏 FSM + ModalStack + CombatOverride + InputLayer 路由
- [x] `src/presentation/FeedbackManager.cs` — SemanticEventHub (ADR-0016 桩) + Subscribe/EmitFeedback
- [x] `src/core/SessionBootChain.cs` — Phase 0→7 引导链 + ShellState 转换 + InputGate
- [x] `tests/csharp/FoundationParity/Program.cs` — 70/70 checks PASS (从 20 扩展到 70)
- [x] `dotnet build CloudWeaverVoyage.sln --no-restore` — PASS (0 errors, 3 warnings — 未使用 API 事件)
- [x] `dotnet run --project tests/csharp/FoundationParity` — PASS (70/70)

**C# Autoload 迁移对照:**

| # | Autoload | GDScript | C# | Namespace |
|---|----------|----------|----|-----------|
| 1 | Registry | registry.gd | Registry.cs | Core |
| 2 | Persistence | persistence.gd | Persistence.cs | Core |
| — | SnapshotPackage | snapshot_package.gd | SnapshotPackage.cs | Core |
| 3 | InteractionRegistry | interaction_registry.gd | InteractionRegistry.cs | Core |
| 4 | Resources | resources_manager.gd | ResourcesManager.cs | Core |
| 5 | Intel | intel_manager.gd | IntelManager.cs | Core |
| 6 | Chart | chart_manager.gd | ChartManager.cs | Core |
| 7 | WorldRepair | world_repair.gd | WorldRepair.cs | Feature |
| 8 | UIManager | ui_manager.gd | UIManager.cs | Presentation |
| 9 | FeedbackManager | feedback_manager.gd | FeedbackManager.cs | Presentation |
| — | SessionShell | session_shell.gd | SessionBootChain.cs | Core |

### 平台转向复审 — 2026-05-09 完成

- [x] `/review-all-gdds` — PASS (前次: 0 blockers, 16/16 Approved)
- [x] 平台转向复审报告: `design/gdd/platform-pivot-review-2026-05-09.md`
- [x] 修复 6 个 WARNING 级 Web/GDScript 残余 (10 个文件):
  - `platform-session-shell.md`: `beforeunload` → 桌面退出请求
  - `player-movement-interaction.md`: gdscript IDL 标注 + AC-WEB → AC-FOCUS + 浏览器 API → 窗口事件
  - `airship-hub.md`: pagehide/WebGL 2/Web 引擎/Web 约束 → 桌面等价物
  - `navigation-route-risk.md`: AC-18 browser tab → game window focus
  - `exploration-scavenge-scenario.md`: EC-11-20/21 visibilitychange/localStorage → 桌面等价物
  - `local-save-world-state-persistence.md`: iframe/cookie → 桌面存储策略
- [x] 修复 4 个 NICE-TO-HAVE:
  - `game-concept.md`: Web 性能预算 → Compatibility 桌面性能预算
  - `tr-registry.yaml`: browser tab freeze → desktop window focus
  - `document-index.md`: GDScript Web-first → C# Desktop-first
  - `document-index.md`: 平台转向 Mermaid 架构图 + 修正文件总览
- [x] `docs/document-index.md` 更新 — 平台转向架构图 + 审查记录

**Verdict: CONCERNS (0 blockers, 6 warnings fixed, 4 nice-to-have fixed)**

### 平台转向 Foundation Spike — 2026-05-09 完成

- [x] C# Foundation spike: migrated `SnapshotPackage`, `Registry`, and `Persistence`.
- [x] C# parity runner expanded to SnapshotPackage + Registry + Persistence.
- [x] Production epic index now states ADR-0019 as the active implementation contract and treats older GDScript/Web snippets as historical pseudocode.
- [x] Renderer default recorded: Compatibility renderer remains the desktop MVP default; Forward+ is deferred until evidence justifies it.
- [x] Legacy GDScript prototype files labeled in `src/LEGACY_GDSCRIPT.md`.
- [x] Completion trace written: `production/session-state/platform-pivot-foundation-completion-2026-05-09.md`.


## Current: Pre-Production — Desktop C# Foundation Ready — 2026-05-09

### Platform / Language Pivot 进行中

- [x] Decision accepted: abandon Web MVP target and move to Godot 4.6.2 .NET + C# desktop-first.
- [x] ADR-0019 created: `docs/architecture/adr-0019-desktop-csharp-platform-pivot.md`.
- [x] Technical preferences updated: C# primary, Windows desktop primary, Web lifecycle constraints superseded.
- [x] Temporary Sprint 001 pivot task created and completed; durable completion trace now lives in `production/session-state/platform-pivot-foundation-completion-2026-05-09.md`.
- [x] Interim .NET 8 solution created: `CloudWeaverVoyage.sln`, `CloudWeaverVoyage.csproj`.
- [x] Godot command registered: `D:\Godot\godot.cmd` and user PATH shim `~\.local\bin\godot.cmd`.
- [x] Verification: `godot --version` PASS (`4.6.2.stable.mono.official.71f334935`).
- [x] Verification: `godot --headless --path . --quit` PASS when allowed to write normal user cache/log files.
- [x] Verification: `godot --headless --path . --build-solutions --quit` PASS when allowed to write normal user cache/log files.
- [x] C# Foundation spike completed: `SnapshotPackage`, `Registry`, and `Persistence` migrated with parity runner coverage.
- [x] C# build/validation docs updated in `tests/README.md`.
- [x] Active design contracts refreshed for desktop C# pivot: `systems-index`, `game-concept`, platform shell, local save/persistence, UI/HUD, accessibility, art bible, Hub UX, and affected GDDs with Web lifecycle/storage/performance assumptions.
- [x] Verification: `dotnet build CloudWeaverVoyage.sln --no-restore` PASS (0 warnings, 0 errors).
- [x] Verification: `dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj` PASS (20/20 checks).
- [x] Generate/verify C# project files with the Godot .NET CLI.
- [x] Continue C# Foundation spike: migrate `Registry` and `Persistence`.
- [x] Refresh remaining production epic/story notes at the index level: ADR-0019 governs active implementation; older story snippets are historical pseudocode until individually refreshed.

### P2 已完成

- [x] `/architecture-review` — CONCERNS (65.4% TR coverage, Combat #12 gap)
- [x] `/ux-design` — Hub, Chart, Exploration 三份完整 UX Spec
- [x] `/gate-check technical-setup` — CONCERNS (11/13 artifacts, 9/9 quality, 4/4 directors CONCERNS)
- [x] `git push` fa0a21b — P2 交付物推送完成
- [x] `docs/document-index.md` 更新 — 图表 + 就绪矩阵 + 待创建列表 + Concerns 跟踪表

### P3 进行中

- [x] ADR-0018 — Combat/Threat Resolution System (Concern #1 清除)
- [x] ADR-0013 — Exploration/Scavenge System (2026-05-08, Concern #4 部分解决)
- [x] `design/ux/interaction-patterns.md` — 10 个交互模式 (Concern #2 清除)
- [x] `docs/architecture/architecture-traceability.md` — 54 TR 全覆盖矩阵 (Concern #3 清除)
- [x] `production/epics/` — 10 个 Epic 文件 (5 Foundation + 5 Core) + index.md + 完整 Coverage 表 (18 系统)
- [x] `production/epics/content-registry/` — 8 个 Story (001-008): 6 Logic + 1 Integration + 1 UI
- [x] `production/epics/platform-session-shell/` — 7 个 Story (001-007): 2 Logic + 3 Integration + 1 UI
- [x] `production/epics/local-save-persistence/` — 8 个 Story (001-008): 5 Logic + 3 Integration
- [x] `production/epics/player-movement-interaction/` — 7 个 Story (001-007): 3 Logic + 4 Integration
- [x] `production/epics/resources-goods-capacity/` — 9 个 Story (001-009): 6 Logic + 3 Integration
- [x] `production/epics/intel-knowledge/` — 8 个 Story (001-008): 6 Logic + 2 Integration
- [x] `production/epics/airship-hub/` — 8 个 Story (001-008): 6 Logic + 2 Integration
- [x] `production/epics/modules-hull-state/` — 8 个 Story (001-008): 4 Logic + 4 Integration
- [x] `production/epics/chart-route-planning/` — 8 个 Story (001-008): 4 Logic + 4 Integration
- [x] `production/epics/navigation-route-risk/` — 8 个 Story (001-008): 5 Logic + 3 Integration
- [x] `production/epics/combat-threat/` — 6 个 Story (001-006): 3 Logic + 3 Integration
- [x] `production/epics/world-repair/` — 6 个 Story (001-006): 3 Logic + 3 Integration
- [x] `production/epics/exploration-scavenge/` — 6 个 Story (001-006): 3 Logic + 3 Integration
- [x] ADR-0014 — Settlement/Market System (2026-05-08, Concern #4 部分解决)
- [x] `production/epics/settlement-market/` — 6 个 Story (001-006): 3 Logic + 3 Integration
- [x] ADR-0015 — Partner/Relationships System (2026-05-08, Concern #4 清除 — 仅剩 0016/0017)
- [x] `production/epics/partner-relationships/` — 6 个 Story (001-006): 3 Logic + 3 Integration
- [x] `production/epics/ui-hud-interface/` — 6 个 Story (001-006): 3 Logic + 3 Integration

### Foundation Layer 完成

**5/5 Foundation Epics — 全部 Story 分解完成 (30+9=39 stories total)**

| Epic | # | Stories | Types |
|------|---|---------|-------|
| content-registry | #1 | 8 | 6 Logic + 1 Integration + 1 UI |
| platform-session-shell | #2 | 7 | 2 Logic + 3 Integration + 1 UI |
| local-save-persistence | #3 | 8 | 5 Logic + 3 Integration |
| player-movement-interaction | #4 | 7 | 3 Logic + 4 Integration |
| resources-goods-capacity | #5 | 9 | 6 Logic + 3 Integration |
| **Total** | | **39** | **22 Logic + 14 Integration + 2 UI** |

### Core Layer 完成

**5/5 Core Epics — 全部 Story 分解完成 (40 stories)**

| Epic | # | Stories | Types |
|------|---|---------|-------|
| intel-knowledge | #6 | 8 | 6 Logic + 2 Integration |
| airship-hub | #7 | 8 | 6 Logic + 2 Integration |
| modules-hull-state | #8 | 8 | 4 Logic + 4 Integration |
| chart-route-planning | #9 | 8 | 4 Logic + 4 Integration |
| navigation-route-risk | #10 | 8 | 5 Logic + 3 Integration |
| **Total** | | **40** | **25 Logic + 15 Integration** |

### Feature Layer 完成

**5/5 Feature Epics — 全部 Story 分解完成 (30 stories)**

| Epic | # | Stories | Types |
|------|---|---------|-------|
| combat-threat | #12 | 6 | 3 Logic + 3 Integration |
| world-repair | #13 | 6 | 3 Logic + 3 Integration |
| exploration-scavenge | #11 | 6 | 3 Logic + 3 Integration |
| settlement-market | #14 | 6 | 3 Logic + 3 Integration |
| partner-relationships | #15 | 6 | 3 Logic + 3 Integration |
| **Total** | | **30** | **15 Logic + 15 Integration** |

### Presentation Layer 进行中

**1/3 Presentation Epics — UI/HUD 完成 (6 stories, 2 Blocked)**

| Epic | # | Stories | Types |
|------|---|---------|-------|
| ui-hud-interface | #16 | 6 | 3 Logic + 3 Integration |
| feedback-fx-audio | #17 | — | Blocked — ADR-0016 deferred, 无 GDD |
| onboarding-first-loop | #18 | — | Blocked — ADR-0017 deferred, 无 GDD |

### Pre-Production 入口状态

- **Stage**: Pre-Production (已写入 `production/stage.txt`)
- **ADRs**: 16 Accepted (0001-0015 + 0018), 2 deferred (0016-0017)
- **Stories**: 115 total (Foundation 39 + Core 40 + Feature 30 + Presentation 6)
- **UX Specs**: 3 complete (Hub, Chart, Exploration)
- **Tests**: 1 example (GdUnit4 framework ready, CI wired)
- **Art Bible**: 9 chapters complete

### 门禁留下的 Concerns（按优先级）

| # | Concern | 严重度 | 建议动作 |
|---|---------|--------|---------|
| 1 | Combat #12 零 ADR 覆盖 | 🔴 HIGH | ✅ ADR-0018 已创建 (2026-05-05) |
| 2 | 缺少 interaction-patterns.md | 🟡 MEDIUM | ✅ 已创建 — 10 个交互模式提取完成 |
| 3 | 缺少 architecture-traceability.md | 🟡 MEDIUM | ✅ 已创建 — 54 TR 全覆盖矩阵 |
| 4 | 4 个延期 ADR 无时间表 | 🟡 MEDIUM | ADR-0013, ADR-0014, ADR-0015 已清除; 剩余 0016-0017 Production 前完成 |
| 5 | Desktop focus / pause lifecycle needs C# validation | 🟡 MEDIUM | Sprint 1 Spike 任务 |
| 6 | 仅 1 个示例测试 | 🟡 LOW | 随 Foundation/Core 实现同步编写 |
| 7 | 无视觉参考/Mood Board | 🟡 LOW | 概念美术开始前整理 |
| 8 | UX Specs 未交叉引用 Art Bible | 🟡 LOW | 早期 Pre-Production 轻量对齐 |

### 关键文件

- `docs/architecture/architecture-review-2026-05-05.md`
- `docs/architecture/control-manifest.md`
- `design/ux/hub.md`
- `design/ux/chart.md`
- `design/ux/exploration.md`
- `production/epics/index.md` — 18-system complete coverage table (Foundation 5 + Core 5 + Feature 5 Ready + Presentation 1 Ready + 2 Blocked)
- `docs/architecture/adr-0013-exploration-scavenge-system.md` — ADR-0013 Accepted (2026-05-08)
- `docs/architecture/adr-0015-partner-relationships-system.md` — ADR-0015 Accepted (2026-05-08)
- `production/epics/partner-relationships/EPIC.md`
- `production/epics/partner-relationships/story-001-cat-state-machine-presence.md` through `story-006-edge-cases-r15-defensive.md`
- `docs/architecture/adr-0012-ui-input-routing-dual-focus.md` — ADR-0012 Accepted (2026-05-05)
- `production/epics/ui-hud-interface/EPIC.md`
- `production/epics/ui-hud-interface/story-001-screen-state-machine-flow.md` through `story-006-edge-cases-web-recovery-a11y.md`
- `production/epics/exploration-scavenge/EPIC.md`
- `production/epics/exploration-scavenge/story-001-state-machine-phase-transitions.md` through `story-006-persistence-session-recovery-edge-cases.md`
- `docs/architecture/adr-0014-settlement-market-system.md` — ADR-0014 Accepted (2026-05-08)
- `production/epics/settlement-market/EPIC.md`
- `production/epics/settlement-market/story-001-state-machine-stall-lifecycle.md` through `story-006-edge-cases-ui-defensive.md`
- `production/epics/content-registry/EPIC.md`
- `production/epics/platform-session-shell/EPIC.md`
- `production/epics/local-save-persistence/EPIC.md`
- `production/epics/player-movement-interaction/EPIC.md`
- `production/epics/resources-goods-capacity/EPIC.md`
- `production/epics/intel-knowledge/EPIC.md`
- `production/epics/intel-knowledge/story-001-pattern-knowledge-state-machine.md` through `story-008-persistence-mvp-bootstrap.md`
- `production/epics/airship-hub/story-001-hub-scene-state-machine.md` through `story-008-scene-persistence-transition.md`
- `production/epics/modules-hull-state/story-001-slot-state-machine-dual-field.md` through `story-008-scout-acquisition-combat-damage.md`
- `production/epics/airship-hub/EPIC.md`
- `production/epics/modules-hull-state/EPIC.md`
- `production/epics/chart-route-planning/EPIC.md`
- `production/epics/chart-route-planning/story-001-chart-state-machine-content-gate.md` through `story-008-edge-cases-error-recovery-keyboard.md`
- `production/epics/navigation-route-risk/EPIC.md`
- `production/epics/navigation-route-risk/story-001-voyage-state-machine-preflight.md` through `story-008-edge-cases-defensive-error-handling.md`
- `production/epics/combat-threat/EPIC.md`
- `production/epics/combat-threat/story-001-combat-state-machine-threat-queue.md` through `story-006-edge-cases-defensive-handling.md`
- `production/epics/world-repair/EPIC.md`
- `production/epics/world-repair/story-001-repair-state-machine-node-lifecycle.md` through `story-006-edge-cases-visual-audio-defensive.md`
- `production/epics/exploration-scavenge/EPIC.md`
- `production/epics/exploration-scavenge/story-001-state-machine-phase-transitions.md` through `story-006-persistence-session-recovery-edge-cases.md`

### /review-all-gdds 2026-05-08 — Result: FAIL
- 16 GDDs reviewed, 14 blockers, 25 warnings
- 10 GDDs marked Needs Revision in systems-index
- Report: `design/gdd/gdd-cross-review-2026-05-08.md`

---

### /review-all-gdds 2026-05-08 — Blocker Resolution Complete

All 14 blockers across 10 GDDs resolved in 3 rounds:

**Round 1 — Quick Fixes (C1 stale values + entity registry sync):**
- `entities.yaml`: calc_hull_damage sync (8-12 range, 0.30 module chance), added `location.glass-harbor-outskirts`
- `combat-threat-handling.md`: hull threshold 18→12 (×2 locations)

**Round 2 — Simple Fixes (status markers, ID renames, field consistency):**
- `player-knowledge-intel.md`: 7 fixes (dep status markers, repair_node ID, stale references)
- `resources-goods-capacity.md`: dependency table updated, risk assessment corrected
- `content-data-state-registry.md`: starlight-dock→glass-harbor/sky-reef-outpost, repair-node IDs, destination_id field
- `partner-relationships.md`: Cross-GDD Impact header cleared

**Round 3 — Design-Heavy Fixes (user-approved decisions):**
- `exploration-scavenge-scenario.md`: repair_kit_drop_rate = 0.25 (range 0.15-0.35)
- `navigation-route-risk.md`: supply consumption (Short=2, Medium=4, Long=8), Mode B signal interface (helm_activated), #7/#5 added as upstream deps
- `entities.yaml`: location.glass-harbor-outskirts created (separate from glass-harbor)

**Result:** 16/16 GDDs Approved. 10 Needs Revision → 0. Systems-index progress tracker synced.

**Modified files (10 GDDs + registry + systems-index + session state):**
- `design/gdd/combat-threat-handling.md`
- `design/gdd/content-data-state-registry.md`
- `design/gdd/exploration-scavenge-scenario.md`
- `design/gdd/navigation-route-risk.md`
- `design/gdd/partner-relationships.md`
- `design/gdd/player-knowledge-intel.md`
- `design/gdd/resources-goods-capacity.md`
- `design/gdd/systems-index.md`
- `design/registry/entities.yaml`
- `production/session-state/active.md`

**Recommended next step:** Re-run `/review-all-gdds` to verify all blockers cleared → PASS.

### Session Extract — /review-all-gdds Rerun 2026-05-08
- Verdict: PASS
- GDDs reviewed: 16
- Flagged for revision: None
- Blocking issues: None (all 14 previous blockers verified resolved)
- New warnings: 7 (W-RERUN-01~04 + W-SCEN-R01~R03), all LOW severity
- Previous warnings resolved: 17/25
- Persistent design-choice warnings: 8
- Report: design/gdd/gdd-cross-review-2026-05-08-rerun.md

### P3 架构验证原型 — 2026-05-09

- [x] `project.godot` — Godot 4.6.2 项目初始化（Compatibility 渲染器、WebGL 2、9 Autoload、输入映射）
- [x] `src/core/registry.gd` — Registry Autoload (#1) 静态内容目录 + 查询引擎（5 种查询结果区分）
- [x] `src/core/registry_bootstrap.gd` — 引导数据：4 地点、2 航线、6 资源、1 修复节点、1 威胁、1 伙伴
- [x] `src/core/persistence.gd` — Persistence Autoload (#2) 完整 staging→verify→promotion 管道 + Canonical JSON 编码器 + SHA-256
- [x] `src/core/snapshot_package.gd` — SnapshotPackage RefCounted 数据类（to_dict/from_dict 往返）
- [x] `src/core/interaction_registry.gd` — InteractionRegistry Autoload (#3) 可交互对象注册中心 + 5 状态焦点机
- [x] `src/core/interactable.gd` — Interactable @abstract 基类（所有可交互对象的父类）
- [x] `src/core/resources_manager.gd` — Resources Autoload (#4) 6 资源池 + "fill fullest first" stack merge
- [x] `src/core/intel_manager.gd` — Intel Autoload (#5) 知识状态机 + reveal_rumor/report_observation_event
- [x] `src/core/chart_manager.gd` — Chart Autoload (#6) 航线状态机 + route_selectability + departure 确认
- [x] `src/feature/world_repair.gd` — WorldRepair Autoload (#7) 修复状态机 + commit_deposit + repair_completed 信号
- [x] `src/presentation/ui_manager.gd` — UIManager Autoload (#8) 12 屏幕 FSM + 单槽模态栈 + 4 层输入路由
- [x] `src/presentation/feedback_manager.gd` — FeedbackManager Autoload (#9) 语义事件中心 (VS stub)
- [x] `src/session_shell.gd` — SessionShell 主场景（Phase 0→7 引导链 + Web 生命周期钩子）
- [x] `src/session_shell.tscn` — 主场景文件
- [x] `tests/unit/test_registry_query.gd` — Registry 查询引擎测试（6 例）
- [x] `tests/unit/test_resources_merge.gd` — Resources stack merge 算法测试（7 例）
- [x] `tests/unit/test_persistence_roundtrip.gd` — Persistence 往返 + Canonical JSON 测试（8 例）
- [x] `tests/integration/test_boot_chain.gd` — 引导链 + 信号协议测试（5 例）
- [x] `prototypes/p3-architecture/README.md` — 原型说明文件

**已验证 ✅** (2026-05-09 — Godot 4.6.2, RTX 4080, Compatibility Renderer):
- [x] **场景 A**：启动后终端输出 "Architecture boot: PASS" — **122.35 ms** (预算 <2s ✅)
  - Phase 0→7 全部完成，9 个 Autoload 按序加载
  - 修复: 移除 6 个 Autoload 脚本的 `class_name` (与 Autoload 名称冲突)

**待验证**:
- [x] 场景 B：存档往返 — ✅ PASS (16/16 checks) — 完整 save→load 往返 + 多域 + 空加载
- [x] 场景 C：信号协议 — ✅ PASS (33/33 checks) — repair_completed 4 消费者扇出 + 状态机 + 类型检查
- [x] Bug Fix: persistence.gd checksum_mismatch — `_checksum` 字段在验证前加入 manifest 导致重编码校验和不一致
- [x] Bug Fix: Godot 4.6 --script 模式下 lambda 不能作为 signal callback（必须用命名方法）
- [x] 验证脚本: `tests/p3_verification.gd` — 自包含 SceneTree runner，49 项检查

**P3 全场景验证完成** ✅ — 场景 A (启动引导 122ms) + 场景 B (存档往返) + 场景 C (信号扇出)
- 源代码修改: `src/core/persistence.gd` (checksum fix)
- 测试文件: `tests/p3_verification.gd` (新增), `tests/integration/test_save_roundtrip.gd` (新增), `tests/integration/test_signal_fanout.gd` (新增)
- 修复编译错误: 6 个测试文件的 class_name/类型引用问题 (Autoload 脚本无 class_name)

### Document Index 更新 — 2026-05-09

- [x] `docs/document-index.md` — 新增 §五 P3 架构原型章节
  - 9 Autoload 依赖层次图 (Mermaid graph TB)
  - SessionShell Phase 0→7 引导链 (Mermaid sequenceDiagram)
  - Persistence staging→verify→promotion 管道 (Mermaid graph LR)
  - 测试架构图 (Mermaid graph TB)
  - 源代码文件清单 (15 .gd + 1 .tscn + 4 tests + project.godot)
  - 架构关键决策表
- [x] 文档全景图新增 `src/` 源代码层
- [x] 统计数据更新 (src/ 16 文件, tests/ 4 文件, prototypes/ 1 文件)
- [x] §十一待完成列表 — P3 原型标记 ✅
- [x] 全部 7 个节号重新编排 (原 §五→§六, §六→§七, ... §十→§十一)

## Session Extract — /dev-story 2026-05-10
- Story: `production/epics/content-registry/story-002-schema-validation.md` — Story 002: Schema Validation
- Files changed: `src/core/content/Registry.cs`, `tests/unit/registry/Program.cs`
- Test written: `tests/unit/registry/Program.cs` — 11 Story 002 acceptance/regression checks
- Blockers: None
- Next: /code-review `src/core/content/Registry.cs` `tests/unit/registry/Program.cs` then /story-done `production/epics/content-registry/story-002-schema-validation.md`

## Session Extract — /story-done 2026-05-10
- Verdict: COMPLETE
- Story: `production/epics/content-registry/story-002-schema-validation.md` — Story 002: Schema Validation
- Code review: Complete — local review; nested typed-dictionary runtime-field scan fixed before closure
- Tech debt logged: None
- Next recommended: `production/epics/content-registry/story-003-content-lifecycle.md` — Story 003: Content Lifecycle

## Session Extract — /dev-story 2026-05-11
- Story: `production/epics/content-registry/story-004-reference-integrity.md` — Story 004: Reference Integrity
- Files changed: `src/core/content/Registry.cs`, `tests/unit/registry/ReferenceIntegrityProgram.cs`, `tests/unit/registry/ReferenceIntegrityTest.csproj`, `production/epics/content-registry/story-004-reference-integrity.md`
- Test written: `tests/unit/registry/ReferenceIntegrityTest.csproj` — 7 Story 004 acceptance checks
- Blockers: None
- Next: /code-review `src/core/content/Registry.cs` `tests/unit/registry/ReferenceIntegrityProgram.cs` then /story-done `production/epics/content-registry/story-004-reference-integrity.md`

## Session Extract — /story-done 2026-05-11
- Verdict: COMPLETE
- Story: `production/epics/content-registry/story-004-reference-integrity.md` — Story 004: Reference Integrity
- Code review: Complete — reference-chain status annotation gap fixed before closure
- Tech debt logged: None
- Next recommended: `production/epics/content-registry/story-005-domain-loading-decision-gating.md` — Story 005: Domain Loading & Decision UI Gating

## Session Extract — /dev-story 2026-05-11
- Story: `production/epics/content-registry/story-005-domain-loading-decision-gating.md` — Story 005: Domain Loading & Decision UI Gating
- Files changed: `src/core/content/Registry.cs`, `tests/integration/registry/DomainLoadingTest.csproj`, `tests/integration/registry/Program.cs`, `CloudWeaverVoyage.sln`, `.github/workflows/tests.yml`, `production/epics/content-registry/story-005-domain-loading-decision-gating.md`
- Test written: `tests/integration/registry/DomainLoadingTest.csproj` — 7 Story 005 acceptance/signal checks
- Blockers: None
- Next: /code-review `src/core/content/Registry.cs` `tests/integration/registry/Program.cs` then /story-done `production/epics/content-registry/story-005-domain-loading-decision-gating.md`

## Session Extract — /story-done 2026-05-11
- Verdict: COMPLETE
- Story: `production/epics/content-registry/story-005-domain-loading-decision-gating.md` — Story 005: Domain Loading & Decision UI Gating
- Code review: Complete — stale loaded-domain reload failure gap fixed before closure
- Tech debt logged: None
- Next recommended: `production/epics/content-registry/story-006-diagnostic-system.md` — Story 006: Diagnostic System

## Session Extract — /dev-story 2026-05-11
- Story: `production/epics/content-registry/story-006-diagnostic-system.md` — Story 006: Diagnostic System
- Files changed: `src/core/content/Registry.cs`, `tests/unit/registry/DiagnosticSystemTest.csproj`, `tests/unit/registry/DiagnosticSystemProgram.cs`, `CloudWeaverVoyage.sln`, `.github/workflows/tests.yml`, `production/epics/content-registry/story-006-diagnostic-system.md`
- Test written: `tests/unit/registry/DiagnosticSystemTest.csproj` — 7 Story 006 acceptance/regression checks
- Blockers: None
- Next: /code-review `src/core/content/Registry.cs` `tests/unit/registry/DiagnosticSystemProgram.cs` then /story-done `production/epics/content-registry/story-006-diagnostic-system.md`

## Session Extract — /story-done 2026-05-11
- Verdict: COMPLETE
- Story: `production/epics/content-registry/story-006-diagnostic-system.md` — Story 006: Diagnostic System
- Code review: Complete — deprecated-reference severity context tightened before closure
- Tech debt logged: None
- Next recommended: `production/epics/content-registry/story-007-diagnostic-ui.md` — Story 007: Diagnostic UI — Dev Tools

## Session Extract — parallel #2 startup 2026-05-11
- Target: `production/epics/platform-session-shell/EPIC.md` — Epic #2 Platform Session Shell
- Plan: Story 001 first as shared state-machine kernel; then Story 002 Entry/Audio and Story 003 Suspend/Resume in parallel; Story 004 failure severity can join after kernel review; Story 006 waits for 003; Story 007 waits for 002/004/005.
- Story started: `production/epics/platform-session-shell/story-001-state-machine-core.md` — Story 001: Platform State Machine Core
- Files changed: `src/core/boot/SessionBootChain.cs`, `tests/unit/session/StateMachineTest.csproj`, `tests/unit/session/StateMachineProgram.cs`, `CloudWeaverVoyage.sln`, `.github/workflows/tests.yml`, `production/epics/platform-session-shell/story-001-state-machine-core.md`, `production/epics/platform-session-shell/EPIC.md`
- Test written: `tests/unit/session/StateMachineTest.csproj` — 8/8 Story 001 acceptance checks passing
- Verification: `dotnet run --project tests/unit/session/StateMachineTest.csproj` PASS; `dotnet run --project tests/csharp/FoundationParity/FoundationParity.csproj --no-restore` PASS; `dotnet build CloudWeaverVoyage.sln --no-restore` PASS
- Parallel agents started: Story 002 Entry/Audio token flow; Story 003 Desktop suspend/resume flow
- Blockers: None
- Next: /code-review `src/core/boot/SessionBootChain.cs` `tests/unit/session/StateMachineProgram.cs` then /story-done `production/epics/platform-session-shell/story-001-state-machine-core.md`

## Session Extract — parallel #2 progress 2026-05-11
- Stories implemented in parallel: `production/epics/platform-session-shell/story-002-start-continue-audio.md`, `production/epics/platform-session-shell/story-003-background-suspend-resume.md`
- Files changed: `src/core/session/EntryAudioFlow.cs`, `tests/integration/session/EntryAudioTest.csproj`, `tests/integration/session/EntryAudioProgram.cs`, `src/core/session/DesktopSessionLifecycle.cs`, `tests/integration/session/SuspendResumeTest.csproj`, `tests/integration/session/SuspendResumeProgram.cs`, `CloudWeaverVoyage.sln`, `.github/workflows/tests.yml`, `production/epics/platform-session-shell/story-002-start-continue-audio.md`, `production/epics/platform-session-shell/story-003-background-suspend-resume.md`, `production/epics/platform-session-shell/EPIC.md`, `docs/reference/production-flowchart.md`
- Tests written: `tests/integration/session/EntryAudioTest.csproj` — 10/10 Story 002 checks passing; `tests/integration/session/SuspendResumeTest.csproj` — 7/7 Story 003 checks passing
- Blockers: None
- Next: /code-review `src/core/session/EntryAudioFlow.cs` `tests/integration/session/EntryAudioProgram.cs` `src/core/session/DesktopSessionLifecycle.cs` `tests/integration/session/SuspendResumeProgram.cs`; then /story-done Story 002 and Story 003

## Session Extract — #2 platform-session-shell complete 2026-05-11
- Verdict: COMPLETE
- Epic: `production/epics/platform-session-shell/EPIC.md` — Platform Session Shell (#2)
- Stories completed: Story 001 State Machine Core, Story 002 Start/Continue Entry + Audio Activation, Story 003 Background Suspend/Resume, Story 004 Failure Severity & Recovery Paths, Story 005 Storage Capability & Ephemeral Sessions, Story 006 Input Gate & Shell Overlay Control, Story 007 Shell UI — Entry, Loading & Error Screens
- Files changed: `src/core/boot/SessionBootChain.cs`, `src/core/session/EntryAudioFlow.cs`, `src/core/session/DesktopSessionLifecycle.cs`, `src/core/session/FailureRecoveryPolicy.cs`, `src/core/session/StorageCapabilityCoordinator.cs`, `src/core/session/ShellInputGate.cs`, `src/presentation/ShellUiPresenter.cs`, `tests/unit/session/*`, `tests/integration/session/*`, `production/qa/evidence/shell-ui-evidence.md`, `CloudWeaverVoyage.sln`, `.github/workflows/tests.yml`, `production/epics/platform-session-shell/*.md`, `production/epics/index.md`, `docs/reference/production-flowchart.md`
- Test evidence: Story 001 8/8; Story 002 10/10; Story 003 7/7; Story 004 9/9; Story 005 7/7; Story 006 6/6; Story 007 8/8 model + scene checks; FoundationParity 70/70
- Build evidence: `dotnet build CloudWeaverVoyage.sln --no-restore` PASS (0 warnings, 0 errors)
- Residual risk reduced: Story 007 now has a concrete Godot `CanvasLayer`/`Control` scene mounted by `SessionShell`, Godot 4.6.2 scene-load checks, and generated loading screenshot evidence. Remaining risk is dynamic runtime data binding/polish.
- Unlocked: #4 player-movement-interaction can start now; #3 local-save-persistence starts after #1 content-registry completes.
