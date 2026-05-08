# Active Design Session

<!-- STATUS -->
Epic: Pre-Production
Feature: P3 原型 + Epic/Story 框架
Task: UI/HUD #16 Epic + 6 stories 已完成 → Presentation Layer 1/3 unblocked, 下一目标: ADR-0016/0017 (Feedback/Onboarding, 无 GDD, Vertical Slice)
<!-- /STATUS -->

## Current: Pre-Production — 2026-05-08

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
| 5 | Dual-focus + Web 生命周期未测试 | 🟡 MEDIUM | Sprint 1 Spike 任务 |
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
