# Platform Pivot Review — 平台转向复审

> **Date**: 2026-05-09
> **Review Type**: Full (all 16 GDDs + architecture docs + production artifacts)
> **Authority**: ADR-0019 Desktop C# Platform Pivot
> **Verdict**: CONCERNS (0 blockers, 6 warnings, 4 nice-to-have)

---

## Executive Summary

对全部 16 份 GDD、架构文档、TR Registry、Control Manifest、production epics 和 13 个目标文件进行了平台转向复审。结论：**平台转向整体完成，没有阻塞级残留**。发现 6 个 WARNING 级问题和 4 个 NICE-TO-HAVE 建议。

核心判断：
- ADR-0019 是毋庸置疑的权威平台决策，Control Manifest 明确禁止 Web-only 需求
- 大部分 GDD 已添加 "Platform Pivot Note" 头部，将旧 Web 内容标记为历史参考
- `local-save-world-state-persistence.md` 的 "Historical Web Contract" section 模式是正确做法
- 仍存在少量具体语句使用了浏览器特定术语（pagehide、visibilitychange、WebGL 2、localStorage），但逻辑等价物在桌面上下文中仍然可读

---

## Review Scope

### Files Reviewed (13 target files)

| # | File | Web/GDScript Residuals | Severity |
|---|------|----------------------|----------|
| 1 | `design/gdd/platform-session-shell.md` | AC line 407: `beforeunload` / `unload` 浏览器事件 | WARNING |
| 2 | `design/gdd/local-save-world-state-persistence.md` | line 712: "iframe/cookie policy change" 浏览器 probe 触发器 | WARNING |
| 3 | `design/gdd/player-movement-interaction.md` | line 165: `gdscript` 代码块; line 472: 浏览器 API 引用 | WARNING |
| 4 | `design/gdd/airship-hub.md` | line 248: `pagehide`; line 350/364: WebGL 2; line 360: "Web 约束"; line 366: "Godot Web 引擎" | WARNING |
| 5 | `design/gdd/navigation-route-risk.md` | AC-18 line 674: "browser tab is hidden" | WARNING |
| 6 | `design/gdd/exploration-scavenge-scenario.md` | EC-11-20: `visibilitychange`; EC-11-21: `localStorage` QuotaExceededError | WARNING |
| 7 | `design/gdd/game-concept.md` | line 255: "首发必须服从 Web 性能预算" | OK (historical context) |
| 8 | `design/gdd/ui-hud-chart-interface.md` | Clean — Platform Pivot Note present | OK |
| 9 | `design/accessibility-requirements.md` | Clean — Platform Pivot Note present | OK |
| 10 | `design/art/art-bible.md` | Clean — only in Platform Pivot Note | OK |
| 11 | `docs/architecture/architecture.md` | lines 385+: GDScript 风格 `func` API 签名（已标注为历史 IDL） | OK |
| 12 | `docs/architecture/control-manifest.md` | Clean — explicitly forbids browser requirements | OK |
| 13 | `docs/architecture/tr-registry.yaml` | TR-ui-004: "browser tab freeze recovery" | OK (minor wording) |

### Additional Files Reviewed

| File | Status |
|------|--------|
| `design/gdd/systems-index.md` | Clean — Platform Pivot Note + Director Review Notes reflect desktop |
| `design/registry/entities.yaml` | Clean — no platform assumptions |
| `production/sprints/sprint-001-desktop-csharp-pivot.md` | Clean |
| `production/epics/` (all 16 epics) | Mixed — many story files have gdscript code blocks and browser references (see §Production Epic Notes) |
| `docs/architecture/adr-0002, 0005, 0010, 0015` | gdscript code examples present but marked as implementation illustrations |

---

## Detailed Findings

### WARNING Level (should fix before C# implementation begins)

**W-R01 — `platform-session-shell.md` AC line 407: `beforeunload` / `unload`**

当前文本：
```
GIVEN beforeunload 或 unload 触发，WHEN 壳层处理关闭前信号，THEN 不得启动新序列化...
```

问题：`beforeunload` 和 `unload` 是浏览器特有事件，桌面 Godot 应用使用 `NOTIFICATION_WM_CLOSE_REQUEST` 或引擎 quit 信号。AC 逻辑仍然正确（关闭时不启动新保存），但术语会误导 C# 实现者去搜索不存在的 Web API。

建议修复：替换为 `GIVEN 桌面退出请求或进程终止信号触发`

**W-R02 — `player-movement-interaction.md` line 165: `gdscript` 代码块**

Interactable Contract 部分使用 `gdscript` 代码块定义 `class_name Interactable extends Node2D`。虽然注释说明使用 Godot 4.5+ `@abstract`，但 C# 实现者需要的是 C# 等价物（`partial class` + `[Export]` 属性）。

建议修复：将代码块标注为 `<!-- 历史 IDL 草案; C# 实现使用 abstract partial class + [Export] 属性 -->`

**W-R03 — `airship-hub.md`: 多处 Web 特定引用**

- line 248: "If 浏览器 `pagehide`" → 应改为 "If 桌面窗口失焦/最小化"
- line 350: "服从 WebGL 2 / Compatibility renderer 性能预算" → 应改为 "服从 Compatibility renderer 桌面性能预算"
- line 360: "MVP 不做空间音频（Web 约束）" → 删除 "(Web 约束)"
- line 364: "WebGL 2 渲染管线" → 应改为 "Compatibility 渲染管线"
- line 366: "Godot Web 引擎（~15-20MB 压缩）" → 应改为 "Godot 桌面运行时"

**W-R04 — `navigation-route-risk.md` AC-18 line 674**

当前文本：
```
when the browser tab is hidden, elapsed_time stops accumulating, and when the tab is restored...
```

问题：直接引用浏览器标签页行为。桌面等价物是窗口焦点丢失时进程暂停。

建议修复：改为 "when the game window loses focus or is minimized, `elapsed_time` stops accumulating; when the window is restored, encounters queue and settle without being missed"

**W-R05 — `exploration-scavenge-scenario.md` EC-11-20 / EC-11-21**

- EC-11-20 line 712: "标签页 visibilitychange → hidden" → 应改为 "桌面窗口失焦/最小化或 >30 分钟无交互"
- EC-11-21 line 717-720: "localStorage.setItem() 抛出 QuotaExceededError" → 应改为 "桌面持久化写入失败（磁盘满/权限不足）"

**W-R06 — `local-save-world-state-persistence.md` AC line 712**

"iframe/cookie policy change" → 这是浏览器特有概念，桌面不存在 iframe 或 cookie policy。应改为 "平台存储策略变更（磁盘配额/权限/文件系统变更）"

### NICE-TO-HAVE (would improve clarity, not misleading)

**N-01 — `game-concept.md` line 255**: "首发必须服从 Web 性能预算" — 在桌面上下文中这句话技术上不正确，但整段在 "Art Pipeline Complexity" 中，读者从前后文可知桌面是目标。建议改为 "首发必须服从 Compatibility renderer 桌面性能预算"。

**N-02 — `tr-registry.yaml` TR-ui-004**: "browser tab freeze recovery" 建议改为 "desktop window focus recovery" 以匹配当前平台。

**N-03 — `docs/architecture/architecture.md` API Boundaries section**: 已标注 `func` 签名为 "旧 GDScript 风格签名仅作为历史 IDL 草案"，但视觉上占比很大。建议在每个 API block 上方添加一行 C# 类型提示（如 `// C#: string QueryEntity(string entityId)`）。

**N-04 — Production epic story files**: 大量 story 文件（约 115 个）包含 gdscript 代码块和浏览器 API 引用。这些是规划级文档，实现时会被翻译为 C#。建议在 `production/epics/index.md` 添加提示：所有 story 中的代码块是规划伪代码，实现在对应 ADR 和 C# coding standards 下执行。

### ALREADY CLEAN (verified)

- `design/gdd/systems-index.md` — Platform Pivot Note + 所有系统描述已更新
- `design/gdd/ui-hud-chart-interface.md` — 清晰桌面键鼠规格
- `design/accessibility-requirements.md` — "桌面键鼠体验为准"
- `design/art/art-bible.md` — 仅在 Platform Pivot Note 有 Web 字眼
- `design/ux/hub.md` — 清晰桌面 UX spec
- `docs/architecture/control-manifest.md` — 明确禁止 browser-only requirements
- `production/sprints/sprint-001-desktop-csharp-pivot.md` — 正确定义 pivot sprint
- `design/gdd/local-save-world-state-persistence.md` §Historical Web Contract — 正确标注为 superseded

---

## Historical Contract vs Active Implementation Contract

复审的关键区分：

| 类别 | 示例 | 处理方式 |
|------|------|---------|
| **历史记录（可保留）** | `persistence.md` "Historical Web Contract" section, `systems-index.md` "remaining Web references... are historical" | 保留，已有清晰标注 |
| **活跃实现契约（必须修复）** | AC 中的 `beforeunload` 事件、`browser tab` 术语、`WebGL 2` 性能预算 | W-R01~W-R06 修复 |
| **过渡性文档（可逐步迁移）** | `architecture.md` GDScript IDL, production epic gdscript 伪代码, ADR gdscript 示例 | 当前标注充足，实现时翻译为 C# |

---

## Cross-Reference: /review-all-gdds 2026-05-08 Status

上一轮 `/review-all-gdds` 结论: **PASS** (0 blockers, 15 warnings, 17/25 resolved).

平台转向复审与 GDD 交叉复审互补但不重叠：
- `/review-all-gdds` 关注 GDD 之间的规则一致性、公式兼容性、依赖双向性
- 本复审关注 Web/GDScript 残余是否误导 C# 桌面实现者

两个复审在以下方面达成一致：
- `systems-index.md` 的 Platform Pivot Note 是权威的分界标记
- `persistence.md` 的 Historical Web Contract section 是处理历史约束的正确模式
- 16/16 MVP GDD Approved，无阻塞级设计问题

---

## Verdict: CONCERNS

**0 blockers, 6 warnings, 4 nice-to-have**

平台转向的核心目标已达成：
- ADR-0019 是权威平台决策
- Control Manifest 明确禁止 browser-only requirements for MVP
- 16/16 GDD 已标注平台转向状态
- 所有系统描述指向 Godot 4.6.2 .NET/C# 桌面优先

剩余 6 个 WARNING 是术语级别的浏览器残余——逻辑等价物在桌面上下文中可读，但会让 C# 实现者短暂困惑。建议在 Foundation spike 开始前修复。

---

## Recommended Action

1. **立即修复 W-R01~W-R06**（约 10 处文本替换，预计 15 分钟）
2. **可选修复 N-01~N-04**（低优先级，可在后续工作中顺手修正）
3. **修复后重新验证**：`grep -rn "pagehide\|beforeunload\|visibilitychange\|WebGL 2\|localStorage\|browser tab" design/gdd/` 应返回 0 结果（Historical Contract sections 除外）
