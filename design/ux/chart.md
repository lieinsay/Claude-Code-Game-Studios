# UX Spec: 航图与航线规划

> **Status**: In Design
> **Author**: lieinsay + ux-designer
> **Last Updated**: 2026-05-05
> **Journey Phase(s)**: 出航决策 — Hub 之后、航行之前
> **Template**: UX Spec
> **GDD Reference**: `design/gdd/chart-route-planning.md`, `design/gdd/ui-hud-chart-interface.md`

---

## Purpose & Player Need

航图屏幕（S4 `screen_chart`）是《云海织航》的核心决策界面 —— 它是"准备"与"行动"之间的那道门。在游戏循环中，玩家在 Hub 整备、收集情报、安装模块、修复船体，所有准备行为的最终意义都汇聚到航图上的一个动作：选择一条航线，然后承诺出航。

航图服务一个双重玩家幻想，直接来自 GDD #9 Player Fantasy：

**亲手画图的掌控感。** 航图上每一条实线、每一个风险标记、每一段来源注记，都不是系统自动解锁的，而是玩家一步一步积累出来的 —— 来自消耗的情报物品、来自伙伴的侦察报告、来自旧航海日志的坐标碎片、来自亲身验证的目击。玩家从 Hub 进入航图时，看到的是"我亲手让这张图从空白变成可读"的结果。锚点时刻：玩家用手指沿一条从 `rumored`（2px 黄色虚线，60% 透明度）变成 `verified`（暖金色发光实线）的航线划过，意识到这条路不是系统给的 —— 是你把它从"港口传闻"变成"航线"的。

**世界变得可读的安心感。** 航图上曾经全是柔雾边缘的未知空间（GDD #9 规则 17），现在逐渐被安全线、风险带、来源标注、个人注记填满。玩家感受到的不是"内容解锁进度"，而是 Pillar 4「未知带来温和压力」的正面兑现：压力不来自惩罚，来自情报不足；安宁不来自系统放水，来自你比别人更了解这片天。

航图也是 Pillar 1「规划先于冒险」的执行点：两步出航确认（规则 15-16）让"承诺出航"成为一个有重量的决策。玩家必须在航图上阅读每条可选航线的风险标签、来源置信度、阻塞原因，然后基于自己的准备和判断做出不可逆的选择（`DEPARTURE_CONFIRMED` 是终端状态）。

航图不拥有数据 —— 它从 IntelManager（#6）读取知识状态、从 Registry（#1）读取静态航线定义、从 AirshipHub（#7）读取当前停靠地点。它向 Navigation（#10）发出 `route_committed` 承诺，向 UIManager（#16）提供只读查询接口。航图是连接知识系统、航行系统、UI 系统的枢纽。

---

## Player Context on Arrival

玩家进入航图屏幕（S4）有两种不同的上下文，决定了他们到达时的心理状态和可用操作：

**A. 从出航确认进入（承诺模式）**
路径：Hub（S1）→ 与舱门/舵轮交互 → 出航确认对话框（S3 `modal_departure`）→ 确认出航 → `departure_locked` 2.0s → 航图 S4 打开（GDD #16 C.2 屏幕流）。

此时玩家已经做出了"我要出航"的决定。`departure_locked = true` 期间所有面板强制关闭、移动冻结（GDD #16 C.2 强制过渡保护）。2.0s 后航图以 0.8s 羊皮纸纹理渐变从横版飞艇视图过渡到俯视航图视图 —— 这是一个心理切换信号："你现在正在规划，不再是在行走。"（GDD #9 规则 4）。

到达航图时，航图状态为 `LOADING`。系统检查四大内容域（`routes`、`world`、`intel`、`threats`）是否均为 `COMPLETE`（GDD #9 规则 2）。通过后进入 `BROWSING` 状态，航线渲染。

**B. 从 Hub M 键快捷打开（浏览模式）**
路径：Hub 任意位置 → 按 M 键 → 航图 S4 直接打开（GDD #16 C.4 全局按键表）。

此时玩家尚未承诺出航。他们打开航图是为了浏览、规划、查看航线状态 —— "我有哪些可选航线？那条传闻航线现在有更多情报了吗？上次修复灯塔后有没有新航线出现？"

到达航图时，航图同样从 `LOADING` 开始，通过域检查后进入 `BROWSING`。区别在于：按 Esc 或 M 键可以直接返回 Hub（`CHART → HUB` 屏幕状态转换，GDD #16 C.10），玩家没有承诺任何事。

**上下文差异总结**：
| 维度 | 出航确认进入 | M 键进入 |
|------|------------|---------|
| 玩家意图 | 已决定出航，来找航线 | 浏览规划，可能不出航 |
| 退出行为 | 选择航线并确认，或 Esc 返回 Hub | Esc/M 返回 Hub |
| 心理状态 | "从这些航线中选一条" | "让我看看现在有什么选择" |
| 前置动画 | departure_locked 2.0s + 羊皮纸渐变 0.8s | 仅羊皮纸渐变 0.8s |

---

## Navigation Position

航图屏幕（S4）位于游戏核心循环的"决策"节点。它的导航位置可以从三个维度理解：

**在屏幕流中的位置（GDD #16 C.2 屏幕流）**

```
HUB (S1 常驻 HUD)
  │
  ├──[Use 舱门/舵轮]──→ S3 出航确认（模态）
  │                        │
  │                        └──[确认出航]──→ departure_locked 2.0s
  │                                           │
  │                                           ▼
  │                                    S4 航图屏幕（全屏）
  │                                      │
  │                             ┌────────┴────────┐
  │                       [选中路线→确认]    [M键/Esc 返回Hub]
  │                             │                  │
  │                             ▼                  ▼
  │                       墨水扩散 0.6s        Hub 恢复 (S1)
  │                       锁定 1.2s
  │                             │
  │                             ▼
  │                   航行过渡（黑屏/加载）
  │                             │
  │                             ▼
  │                   EXPLORATION (S5 常驻 HUD)
```

航图是整个游戏循环中 Hub → Exploration 之间的唯一通路。没有航图，就没有出航。

**在系统依赖图中的位置（ADR-0008）**

航图（Chart Autoload #9）是连接三个系统的枢纽：
- **上游读取**：Registry（#1）的静态航线定义、IntelManager（#6）的知识状态和可通行性、AirshipHub（#7）的当前停靠地点
- **下游写入**：向 Navigation（#10）发出 `route_committed` 信号（出航承诺）、向 Persistence（#3）写入 `progress.routes` 快照包
- **侧向提供**：向 UIManager（#16）提供 4 个只读查询接口（`get_chart_state`、`get_visible_routes`、`get_selected_route`、`get_filter_state`）

**在玩家心智模型中的位置**

航图是"规划阶段"的物理化身。在 Hub 中，玩家处于"整备模式"（修船、买卖、整理货舱）；进入航图后，玩家切换到"决策模式"（阅读风险、比较航线、做出承诺）。这个模式切换由 0.8s 羊皮纸纹理渐变支撑 —— 视角从横版飞艇内部变为俯视航图，UI 语言从"航海日志内页"变为"被反复折叠标注的手绘海图"。

航图是 Route Planning 系统的家屏幕（home screen）。所有航线选择、风险评估、出航承诺的行为都发生在这个屏幕上。它的前序屏幕是 Hub，后序屏幕是探索场景。

---

## Entry & Exit Points

| # | 入口 | 来源屏幕 | 触发条件 | 到达时航图状态 | 玩家上下文 |
|---|------|---------|---------|--------------|-----------|
| E1 | 出航确认后 | S3 `modal_departure` | 玩家在出航确认对话框中点击"出航"（第二步），`departure_confirmed` 事件触发 | `LOADING`（经 2.0s departure_locked 后进入） | 已承诺出航，必须选择航线 |
| E2 | M 键快捷打开 | Hub（S1 常驻 HUD） | 玩家在 Hub 任意位置按 M 键。条件：`departure_locked == false`，无模态面板打开（GDD #16 C.4 全局按键表，仅 Layer 4 可用） | `LOADING` | 浏览规划，可自由返回 Hub |
| E3 | 存档恢复（出航中） | 启动/读档 | 快照 `progress.routes` 中 `departure_state = DEPARTURE_CONFIRMED`（ADR-0008 恢复边界） | `DEPARTURE_CONFIRMED`（跳过渲染，直接加载出航锁定序列） | 恢复出航过程，不重新看到航图 |

| # | 出口 | 目标屏幕 | 触发条件 | 航图最终状态 | 视觉效果 |
|---|------|---------|---------|-------------|---------|
| X1 | 确认出航 | VOYAGE（航行过渡） | 玩家在 `ROUTE_SELECTED` 状态下完成两步出航确认（GDD #9 规则 15-16），`route_committed` 信号发射 | `DEPARTURE_CONFIRMED`（终端，不可逆） | 墨迹扩散 0.6s → 锁定 1.2s → 黑屏过渡至航行 |
| X2 | 返回 Hub | Hub（S1 恢复） | 玩家按 Esc 或 M 键（仅在 `BROWSING` 或 `ROUTE_SELECTED` 状态有效）。`CHART → HUB` 屏幕状态转换（GDD #16 C.10） | `BROWSING` 或 `ROUTE_SELECTED` → 航图关闭 | 羊皮纸渐变返回 Hub 视图 |
| X3 | 错误重试 | LOADING（重试） | `ERROR` 状态下玩家点击重试按钮，且 `retry_cooldown_remaining <= 0`（GDD #9 EC-1） | `ERROR → LOADING`（RETRY 触发，GDD #9 Formula 3） | 安全错误提示 → 重新加载 |

**被阻塞的出口**：
- 从 `DEPARTURE_CONFIRMED` 返回任何浏览状态：**永久禁止**。`chart_state_transition` 的终端状态守卫（GDD #9 Formula 3 不可逆约束）拒绝所有触发。
- 从 `ERROR` 直接进入 `BROWSING`：禁止。必须通过 `LOADING` 重试验证内容域（GDD #9 无效转换）。

---

## Layout Specification

### Information Hierarchy
1. **已选航线详情**（最高优先级）—— 仅在 `ROUTE_SELECTED` 时可见。航线名称、全部风险标签、全部来源标注、已知风险 vs 未知风险计数、确认出航按钮
2. **悬停航线摘要** —— 鼠标悬停/Tab 聚焦时在侧边面板显示：航线名称、风险标签摘要、来源标注、一句话概述（GDD #9 规则 12）
3. **航线线条与节点** —— 最核心的视觉元素。按知识状态编码（实线/虚线/发光、实心圆/空心圆/虚线圆）和颜色编码（绿/黄/红）
4. **风险标签图标行** —— 航线中点下方。`rumored` 航线的隐藏风险标签显示为闪烁 `?`
5. **来源标注文字** —— 小号手写体，风险标签下方。悬停弹出 tooltip（来源名称 + 置信度层级）
6. **背景纹理** —— 羊皮纸底图 + 边缘柔雾渐变。非航线覆盖区域为"等待被绘制的空白羊皮纸"

### Layout Zones
- **航图区域（70% 屏幕，左侧）**：羊皮纸底图 + 航线渲染 + 地点节点 + 风险标签 + 来源标注。MVP 固定视图，2 条航线从同一出发港辐射，适配单屏，无平移/缩放（GDD #9 规则 8）
- **侧边详情面板（30% 屏幕，右侧）**：垂直面板，半透明羊皮纸底色。内容随航图状态变化（浏览=摘要，选中=完整详情）。面板底部条件性出现"确认出航"按钮

### Component Inventory
| 组件 | 位置 | 可见条件 | 交互 |
|------|------|---------|------|
| 羊皮纸底图 | 全屏 | 始终 | 无（仅视觉） |
| 航线线条（`route.sky-reef-arc-01`） | 航图区域 | `route_visibility = true` | 悬停高亮、点击选择 |
| 航线线条（`route.storm-cut-01`） | 航图区域 | `route_visibility = true` | 同上 |
| 起点节点（`location.glass-harbor`） | 航图区域 | identified，空心圆 | 无（起点固定） |
| 终点节点 | 航线末端 | 按知识状态编码 | 无 |
| 风险标签图标行 | 航线中点下方 | 按知识状态部分/全部显示 | 悬停 tooltip |
| 来源标注文字 | 风险标签下方 | 始终（有来源时） | 悬停 tooltip |
| 闪烁 `?` 标记 | 风险标签行中 | `rumored` 航线 + 隐藏风险标签 | 视觉闪烁（规则 5） |
| 谣言筛选开关 | 面板顶部或航图角落 | 始终 | 点击切换 |
| "确认出航"按钮 | 侧边面板底部 | `ROUTE_SELECTED` | 点击 → 第一步确认浮层 |
| 返回按钮 / Esc 提示 | 航图角落 | `BROWSING` / `ROUTE_SELECTED` | 点击/Esc → 返回 Hub |
| 上下文消息 | 航图居中 | 空状态/全部 UNAVAILABLE | 无（只读文本） |
| 重试按钮 | ERROR 界面 | `ERROR` 状态 | 点击 → RETRY |

### ASCII Wireframe
```
+-----------------------------------------------------------------------------+
|  [筛选: 显示/隐藏传闻 ✓]                              [返回 Hub] [Esc]      |
|                                                                             |
|  +---- 70% 航图区域 ----------------------------------+  +-- 30% 详情面板 --+ |
|  |                                                      |  |                  | |
|  |    边缘柔雾渐变                                       |  |  航线名称         | |
|  |      ┌──────────────────────┐                        |  |  sky-reef-arc-01 | |
|  |      │                      │                        |  |                  | |
|  |      │   羊皮纸底图          │                        |  |  风险标签         | |
|  |      │                      │                        |  |  [safe]           | |
|  |      │  (glass-harbor)      │                        |  |                  | |
|  |      │      ○               │                        |  |  来源             | |
|  |      │     / \              │                        |  |  空港基础航图      | |
|  |      │    /   \             │                        |  |                  | |
|  |      │   /     \  绿色实线   │                        |  |  距离带: short    | |
|  |      │  /  [safe] \         │                        |  |                  | |
|  |      │ / "空港基础航图" \   │                        |  |  概述             | |
|  |      │/                 ○   │                        |  |  安全的短途航线... | |
|  |      │    starlight-dock   │                        |  |                  | |
|  |      │                      │                        |  |                  | |
|  |      │    - - - - - - -    │ 黄色虚线 60%            |  |  [确认出航]       | |
|  |      │  [storm] [?] 闪烁    │                        |  |  (仅在已选时)     | |
|  |      │  "港口传闻"           │                        |  |                  | |
|  |      │                      │                        |  |                  | |
|  |      └──────────────────────┘                        |  |                  | |
|  |    边缘柔雾渐变                                       |  |                  | |
|  +--------------------------------------------------------+  +------------------+ |
|                                                                             |
|  "航图上尚无已知航线。在世界中收集情报以揭示航线。" (空状态时显示)            |
+-----------------------------------------------------------------------------+
```

---

## States & Variants

航图屏幕有 5 个核心显示状态（对齐 GDD #9 状态机 + GDD #16 C.10 屏幕状态），每个状态对应不同的 UI 呈现：

| # | 状态 | Chart 状态机 | 屏幕状态（#16） | UI 呈现 |
|---|------|-------------|----------------|---------|
| S-A | **加载中** | `LOADING` | `CHART`（过渡中） | 羊皮纸底图 + 居中加载指示（非 spinner —— 建议用航图风格的墨迹渲染中动画）。四大内容域校验进行中。 |
| S-B | **浏览**（默认） | `BROWSING` | `CHART` | 航线全量渲染。已渲染航线按 `route_display_order` 排序（GDD #9 Formula 5）。侧边面板空或显示悬停航线摘要。无选中航线。筛选开关可用。Esc/M 可返回 Hub。 |
| S-C | **航线已选** | `ROUTE_SELECTED` | `CHART_ROUTE_SELECTED` | 已选航线：暖金色脉冲 0.3s + 实线高亮。其余航线：40% 透明度。侧边面板：展开完整详情 + "确认出航"按钮（默认焦点）。取消：Esc 或点击空白区域。 |
| S-D | **出航已锁定** | `DEPARTURE_CONFIRMED` | `CHART_DEPARTURE_CONFIRMED` | 所有航线 `LOCKED`。所有交互禁用。墨迹扩散动画 0.6s（ShaderMaterial，沿选中航线从起点描摹至终点）→ 锁定 1.2s → 黑屏过渡。不可逆。 |
| S-E | **错误** | `ERROR` | `CHART`（错误覆盖） | 安全错误提示：失败域名称 + 域状态（如 "threats 域状态：FAILED"，GDD #9 EC-1）。重试按钮可用（`retry_cooldown` 2.0s 冷却）。不渲染任何航线。 |

### 状态变体

| 变体 | 触发条件 | UI 区别 |
|------|---------|--------|
| **空航图（首次打开）** | 所有航线 `knowledge_state = unknown`（GDD #9 EC-12） | `BROWSING` 状态但零航线渲染。羊皮纸背景 + 边缘雾渐变 + 居中消息："航图上尚无已知航线。在世界中收集情报以揭示航线。" 非 ERROR。 |
| **空航图（全部 UNAVAILABLE）** | 所有已知航线起点不等于当前停靠港口（GDD #9 EC-13） | 所有航线置灰 + tooltip 显示原因。居中消息："当前港口 [名称] 无可用出发航线。前往其他港口以选择航线。" |
| **空航图（全部 rumored + 筛选关闭）** | `hide_rumored = true` 且所有已知航线为 `rumored`（GDD #9 EC-14） | 消息区别于首次打开："所有航线均为传闻级别 —— 关闭'隐藏传闻航线'以查看。" |
| **修复后变化** | WorldRepair `repair_completed` 信号触发（ADR-0008 §8） | 受影响航线/节点获得暖金色发光标记 1.0s。新解锁航线从不可见变为可见。`UNAVAILABLE → BROWSABLE` 航线有颜色过渡动画 0.3s（GDD #9 EC-7）。 |
| **外部状态变化** | 航图打开期间 IntelManager 更新知识状态/可通行性 | 实时重新评估的可选择性（ADR-0008 §8）：航线可能出现/消失、UNAVAILABLE/BROWSABLE 切换。有过渡动画的产品变更；无过渡的破坏性变更（如强制取消选择，GDD #9 EC-5）。 |

---

## Interaction Map

所有交互遵循 Godot 4.6 dual-focus 规则（ADR-0012 §5）：鼠标点击时显式 `grab_focus()`，focus/hover 两套 Theme 样式不可混淆。

### 鼠标交互

| 操作 | 目标 | 前置条件 | 系统响应 | 视觉反馈 |
|------|------|---------|---------|---------|
| **悬停航线** | 航线线条区域 | `BROWSING` 状态，航线 `browsable` | 查询航线详情 → 填充侧边面板摘要（名称 + 风险摘要 + 来源 + 概述，GDD #9 规则 12） | 线条微亮；鼠标指针变为 pointer |
| **悬停风险标签** | 风险标签图标 | 始终 | 显示 tooltip：标签名称 + 风险描述 | Tooltip 浮层 |
| **悬停来源文字** | 来源标注文字 | 始终 | 显示 tooltip：来源名称 + 置信度层级（`不确定`/`可靠`/`权威`，GDD #9 规则 7） | Tooltip 浮层，手写体风格 |
| **悬停阻塞航线** | 置灰航线 | 航线 `UNAVAILABLE` | 显示 tooltip：阻塞原因（如"需要灯塔信号解读能力" 或 "不在当前港口"，GDD #9 规则 9-10） | Tooltip 浮层 |
| **点击可选航线** | 航线线条 | `BROWSING` 状态，航线 `browsable` | `BROWSING → ROUTE_SELECTED`。`chart_state_transition(BROWSING, SELECT)`（GDD #9 Formula 3） | 航线暖金色脉冲 0.3s；其余航线 40% 透明度；侧边面板展开；"确认出航"按钮出现并 grab_focus |
| **点击空白区域** | 航图区域（非航线） | `ROUTE_SELECTED` 状态 | `ROUTE_SELECTED → BROWSING`（DESELECT 触发） | 航线恢复正常透明度；面板清空；按钮消失 |
| **点击"确认出航"** | 侧边面板按钮 | `ROUTE_SELECTED` 状态 | 弹出确认浮层（第一步）（GDD #9 规则 15） | 浮层弹出：航线名称 + 风险摘要（刷新后）+ 预估距离带 + "出航"/"取消"按钮 |
| **点击浮层"出航"** | 确认浮层内 | 第一步已完成 | `ROUTE_SELECTED → DEPARTURE_CONFIRMED`（CONFIRM 触发）。`_commit_departure()` 执行（ADR-0008 §5） | 墨迹扩散 0.6s |
| **点击浮层"取消"** | 确认浮层内 | 第一步已完成 | 浮层关闭，保持 `ROUTE_SELECTED` | 浮层消失 |
| **点击谣言筛选** | 筛选开关 | `BROWSING` / `ROUTE_SELECTED` | `hide_rumored` 切换。即时响应，无动画阻塞（GDD #9 规则 11） | 开关状态改变；`rumored` 航线即时出现/消失 |
| **悬停置灰按钮** | "确认出航"按钮灰色态 | 材料不足/条件不满足 | 不响应点击 | Tooltip 解释为何不可用（GDD #16 C.5） |

### 键盘交互

| 按键 | 上下文 | 行为 |
|------|------|------|
| **Tab** | `BROWSING` 状态 | 焦点按 `route_display_order` 在可见航线间循环移动（GDD #9 Formula 5 排序）。焦点顺序：`路线1 → 路线2 → 谣言切换 → 返回按钮`（GDD #16 C.5） |
| **Shift+Tab** | 同上 | 反向循环 |
| **Enter** | 焦点在 `browsable` 航线上 | 同鼠标点击选中该航线 |
| **Enter** | 焦点在"确认出航"按钮上 | 同鼠标点击 → 弹出确认浮层 |
| **Enter** | 焦点在重试按钮上（ERROR 状态） | 触发 RETRY |
| **Esc** | `ROUTE_SELECTED` 状态 | 取消选择 → `BROWSING` |
| **Esc** | `BROWSING` 状态 | 关闭航图 → 返回 Hub（`CHART → HUB` 屏幕转换） |
| **Esc** | 确认浮层打开 | 关闭浮层，保持 `ROUTE_SELECTED` |
| **M** | `BROWSING` / `ROUTE_SELECTED` | 同 Esc —— 关闭航图返回 Hub（GDD #16 C.4 全局按键） |
| **方向键** | 列表/网格内（如未来多航线） | 列表内导航。当前 MVP 2 条航线用 Tab 足够 |

### 锁定期间

`DEPARTURE_CONFIRMED` / `departure_locked = true` 期间：**所有输入事件被拦截**。`chart_state_transition` 终端守卫返回 `allowed: false`（GDD #9 Formula 3）。点击、Tab、Enter、Esc、M 键全部无效。事件不排队 —— 锁定是不可逆的状态约束（GDD #9 EC-3/EC-4）。

---

## Events Fired

航图屏幕在用户交互过程中发出以下语义事件。事件信号遵循 ADR-0002 协议：typed params, sync emit, `{noun}_{verb_past}` 命名。

### 航图系统事件（Chart Autoload #9 发出）

| 事件 | 信号签名 | 触发时机 | 携带数据 | 消费者 |
|------|---------|---------|---------|--------|
| `route_selected` | `route_selected(route_id: StringName)` | 玩家点击/Enter 选中一条 `browsable` 航线，`BROWSING → ROUTE_SELECTED` 状态转换完成时 | 被选中航线的稳定 ID | UIManager #16（驱动侧边面板展开 + 动画） |
| `route_deselected` | `route_deselected(route_id: StringName)` | 玩家 Esc/点击空白区域取消选择，或系统强制取消选择（知识撤销/注册表删除） | 被取消选中的航线 ID | UIManager #16（驱动面板清空 + 航线恢复透明度） |
| `route_committed` | `route_committed(route_id: StringName, destination_id: StringName, hazard_tags: Array[StringName])` | 两步出航确认完成，`ROUTE_SELECTED → DEPARTURE_CONFIRMED`。单次发射保证 —— 终端状态守卫阻止重复 emit（ADR-0008 §5） | 航线 ID + 目的地 ID + 刷新后的最新风险标签数组 | Navigation #10（触发航行阶段、遭遇生成）；Persistence #3（触发 `progress.routes` 快照写入） |
| `route_selection_failed` | `route_selection_failed(route_id: StringName, reason: StringName)` | 出航确认过程中风险检查失败（`traversable → false`）或快照校验失败（`snapshot_package_validity → false`）（ADR-0008 §5） | 航线 ID + 失败原因枚举（`route_not_traversable` / `snapshot_invalid`） | UIManager #16（驱动错误通知/强制取消选择） |
| `route_enhanced` | `route_enhanced(route_id: StringName, enhancement_id: StringName)` | WorldRepair 修复完成后，评估出因修复而受益的航线（ADR-0008 §8） | 受益航线 ID + 触发修复的节点 ID | UIManager #16（驱动航线增强视觉反馈 —— 暖金色发光标记） |
| `chart_opened` | `chart_opened()` | 航图加载完成，`LOADING → BROWSING` 转换后 | 无 | Feedback #17（音频：羊皮纸展开音效）；UIManager #16（屏幕状态同步） |
| `chart_closed` | `chart_closed()` | 航图关闭（Esc/M 返回 Hub，或出航锁定完成移交控制权） | 无 | Feedback #17（音频）；UIManager #16（屏幕状态同步） |
| `rumor_toggle_changed` | `rumor_toggle_changed(hide_rumored: bool)` | 玩家点击筛选开关 | 新的筛选器状态 | 无直接消费者（`hide_rumored` 通过 `get_filter_state()` 查询）；供未来 Feedback 系统可选音效 |

### UI 语义事件（UIManager #16 发出，供 #17 Feedback 消费）

| 事件 | 触发时机 |
|------|---------|
| `ui_route_selected` | 航图中选中航线（GDD #16 C.13） |
| `ui_departure_confirmed` | 出航确认第二步（GDD #16 C.13） |
| `ui_panel_opened` | 确认浮层/侧边面板打开 |
| `ui_panel_closed` | 确认浮层/侧边面板关闭 |

### 事件时序保障

- `route_committed` 在快照校验通过后、状态转换为 `DEPARTURE_CONFIRMED` 后同步 emit（emit-after-mutation，ADR-0002 要求）。fan-out 在 emit 调用栈内完成。
- 同一帧内两次 CONFIRM 触发：第一个成功执行 → 状态变为 `DEPARTURE_CONFIRMED` → 第二个命中终端守卫返回 `allowed: false`。`route_committed` 恰好一次（ADR-0008 Validation Criteria）。
- `route_selected` + `route_committed` 的最大信号级联深度 = 2（符合 ADR-0002 `signal_cascade_depth_3plus` 禁止规则）。

---

## Transitions & Animations

所有动画使用 `create_tween()`（SceneTreeTween），禁止手动 `_process()` 插值（GDD #16 C.8 性能约束）。墨水扩散使用 ShaderMaterial + uniform `progress`（GPU 侧完成）。

### 入场过渡

| 动画 | 时长 | 缓动 | 触发条件 | 视觉描述 |
|------|------|------|---------|---------|
| **羊皮纸纹理渐变** | 0.8s | ease-out | 从 Hub 横版视图过渡到航图俯视视图（GDD #9 规则 4） | 屏幕从 Hub 场景淡出，航图场景以羊皮纸纹理从中心向外展开覆盖全屏。`NinePatchRect` 9-slice 展开（GDD #16 C.8），不使用整张 2048 纹理。心理切换信号："你现在正在规划，不再是在行走。" |

### 交互动画

| 动画 | 时长 | 缓动 | 触发 | 视觉描述 |
|------|------|------|------|---------|
| **航线悬停高亮** | 即时（0.05s 过渡） | linear | 鼠标悬停/Tab 聚焦航线 | 线条亮度提升约 20%，无缩放。侧边面板同步填充摘要内容 |
| **航线选中脉冲** | 0.3s | ease-in-out | 点击/Enter 选中可选航线 | 暖金色描边宽度从 1px → 3px → 1px（来回脉冲一次）。其余航线同步降至 40% 透明度（GDD #9 规则 13/EC-7） |
| **取消选择恢复** | 0.3s | ease-out | Esc/点击空白区域 | 选中航线脉冲消退，其余航线 40% → 100% 透明度。面板清空 |
| **确认浮层弹出** | 0.25s | ease-out | 点击"确认出航"按钮（第一步） | 浮层从面板区域向外展开 + 透明度 0→1。背景 60% 暗化遮罩（`modal_backdrop_alpha`） |
| **确认浮层关闭** | 0.15s | ease-in | 点击"取消" / Esc | 浮层向中心收拢 + 透明度 1→0，遮罩同时消失 |

### 出航承诺动画（不可逆序列）

| 阶段 | 动画 | 时长 | 缓动 | 视觉描述 |
|------|------|------|------|---------|
| **Phase 1: 墨迹扩散** | ShaderMaterial progress 0→1 | 0.6s | ease-out | 墨迹沿选中航线从起点（`glass-harbor`）向外描摹至终点。墨水颜色：深褐/墨黑，半透明叠加在航线线条上。GPU 侧距离场判断渲染（GDD #16 C.8） |
| **Phase 2: 出发口封闭** | 线条凝固 + 锁定标记 | 1.2s | linear | 墨迹扩散完成后，起点节点出现锚链/锁标记，航线不可再交互。所有航线进入 `LOCKED`。锁定提示文字："出航中……"（可选） |
| **Phase 3: 航行过渡** | 黑屏淡出 | 0.3s | ease-in | 航图整体淡出至全黑 → 场景控制权移交 Navigation #10 |

### 状态变化动画

| 动画 | 时长 | 缓动 | 触发 | 视觉描述 |
|------|------|------|------|---------|
| **新航线揭示** | 1.0s | ease-out | WorldRepair 修复完成 → `route_enhanced` 信号（ADR-0008 §8） | 新出现/受益航线以暖金色发光从 0→100% 透明度渐显。`rumored → identified` 航线：虚线→实线过渡 |
| **航线恢复可选择性** | 0.3s | ease-out | 能力解锁或到达对应港口后，`UNAVAILABLE → BROWSABLE`（GDD #9 EC-7） | 置灰恢复为正常颜色。tooltip 消失。颜色过渡动画足够明显 —— 正向变化通过视觉变化被玩家发现 |
| **航线消失（知识撤销）** | 0.3s | ease-in | `route_visibility → false`（知识状态变为 `unknown`，GDD #9 EC-5） | 航线快速淡出 + 强制取消选择 + 通知文字弹出 |
| **筛选切换** | 即时 | — | 谣言筛选开关切换（GDD #9 规则 11） | `rumored` 航线即时出现/消失。无动画 —— 筛选是显式切换，不需要过渡暗示 |

### 退出过渡

| 出口 | 动画 | 时长 | 缓动 |
|------|------|------|------|
| 返回 Hub（Esc/M） | 航图淡出 → Hub 场景淡入（羊皮纸翻页反向） | 0.5s | ease-in-out |
| 出航（voyage） | 黑屏淡出（见 Phase 3） | 0.3s | ease-in |

---

## Data Requirements

航图屏幕的所有显示元素的数据来源、读取方式和写入行为。遵循 Chart Autoload #9 的数据/UI 分离合约：Chart 拥有数据逻辑，UIManager #16 拥有视觉渲染。

### 显示元素 → 数据源映射

| 显示元素 | 数据来源 | 读取接口 | 刷新时机 | 写回 |
|---------|---------|---------|---------|------|
| **航线可见性** | IntelManager #6 | `query_route_knowledge(route_id).state` | 航图加载、知识状态变化信号（`knowledge_advanced`） | 不写回 |
| **航线线条样式**（实线/虚线/发光） | IntelManager #6 + GDD #9 规则 5 视觉编码表 | `query_route_knowledge(route_id).state` → 映射为 visual encoding enum | 同上 | 不写回 |
| **航线颜色**（绿/黄/红） | IntelManager #6 + GDD #9 规则 6 | `query_route_knowledge(route_id).hazard_tags` + 风险等级判定 | 同上 | 不写回 |
| **地点节点样式**（空心圆/虚线圆/实心圆） | IntelManager #6 | `query_location_discovery(location_id).state` | 同上 | 不写回 |
| **风险标签（可见/隐藏/闪烁?）** | IntelManager #6 | `query_route_knowledge(route_id).visible_hazard_tags` + `hidden_hazard_tags` | 同上 | 不写回 |
| **来源标注文字 + tooltip** | IntelManager #6 | `query_route_knowledge(route_id).sources[]` —— 每个来源的 `source_name` + `confidence` 数值 | 同上 | 不写回 |
| **航线可选择性** | Chart #9（Formula 2）+ IntelManager #6 + AirshipHub #7 | `route_selectability(route_id)` 短路求值：visibility → traversable → origin match → selected → browsable | 航图加载、能力解锁信号（`ability_unlocked`）、停靠地变更 | 不写回（Chart 内部公式计算） |
| **航线排序** | Chart #9 Formula 5 | `route_display_order(route_id)` = `knowledge_rank × 100 + distance_rank` | 知识状态变化 | 不写回 |
| **筛选器状态** | Chart #9（内部状态 `_hide_rumored`） | `get_filter_state()` → `{hide_rumored: bool}` | 玩家切换筛选开关 | Chart 内部写入；持久化至 `progress.routes.hide_rumored` |
| **已选航线 ID** | Chart #9（内部状态 `_selected_route_id`） | `get_selected_route()` | 玩家选择/取消选择航线 | Chart 内部写入；不持久化（UI 会话状态） |
| **航图状态** | Chart #9（内部状态 `_chart_state`） | `get_chart_state()` | 状态机转换 | Chart 内部写入；持久化至 `progress.routes.departure_state` |
| **航图可见航线列表** | Chart #9 | `get_visible_routes()` —— 已按 `display_order` 排序的 `Array[StringName]` | 航图加载、筛选切换、知识状态变化 | 不写回 |
| **阻塞原因 tooltip** | IntelManager #6 + Chart #9 Formula 2 | `query_route_accessibility(route_id).block_reason`（能力不足） / Formula 2 分支 4（origin 不匹配） | 同上 | 不写回 |
| **航线静态数据**（起终点、距离带） | Registry #1 | `list_by_kind("route")` → `origin_location_id`, `destination_location_id`, `distance_band` | 航图 LOADING 阶段批量查询 | 不写回（只读） |
| **地点静态数据**（名称、类型） | Registry #1 | `get_display_name(location_id)`, `get_description(location_id)` | 侧边面板/地点节点渲染时 | 不写回（只读） |
| **当前停靠地点** | AirshipHub #7 | `get_current_docked_location()` | `route_selectability` 计算时 | 不写回 |
| **四大内容域状态** | Registry #1 | `domain_state` 查询（`routes`, `world`, `intel`, `threats`） | LOADING 阶段门控检查 | 不写回 |
| **出航锁定时长** | Registry #1 | `get_constant("base_lock_duration")` → 2.0s | 出航确认时 | 不写回 |
| **快照包** | Persistence #3 | `progress.routes` domain serializer（ADR-0008 §6） | 出航确认时写入；存档恢复时读取 | Chart 在 `_commit_departure()` 中构建并请求 Persistence 写入 |

### 数据合约边界（重申）

- Chart **只读** Registry #1 和 IntelManager #6 —— 永远不写入知识状态或修改静态定义（GDD #9 合约边界）。
- Chart **只发出事件** 给 Navigation #10 —— 事件发出后航图关闭，不再参与航行过程。
- Chart **只提供数据** 给 UIManager #16 —— 状态枚举、航线 ID 列表、筛选器状态。不返回颜色值、位置坐标、透明度、动画关键帧（GDD #9 数据合约）。

---

## Accessibility

以下无障碍要求来自 GDD #16 C.9（硬性要求）+ GDD #9 AC-19/AC-20。

### 键盘导航

- **Tab 键**：在可见航线间按 `route_display_order` 顺序循环移动焦点（GDD #9 AC-19）。`BROWSING` 状态焦点顺序：`航线列表项1 → 航线列表项2 → 谣言切换开关 → 返回按钮`（GDD #16 C.5 航图焦点顺序）。
- **Shift+Tab**：反向循环。
- **Enter 键**：焦点在可选航线上 = 选中航线（同鼠标点击）；焦点在确认出航按钮 = 弹出确认浮层；焦点在浮层"出航"按钮 = 执行出航承诺。
- **Esc 键**：取消选择 / 关闭浮层 / 返回 Hub。在 `ROUTE_SELECTED` 状态按 Esc 的行为与点击空白区域一致。
- **完整键盘出航流程**（不碰鼠标，GDD #9 AC-19）：Tab 选航线 → Enter 选中 → Tab 到确认按钮 → Enter 打开浮层 → Tab 到出航 → Enter 确认。所有视觉反馈（脉冲、面板展开、墨迹动画）均正常播放。
- Godot 4.6 dual-focus 规则：鼠标点击后显式 `grab_focus()` 同步键盘焦点（ADR-0012 §5a）。focus/hover 两套 Theme 样式不可混淆（focus = 航标青 `#4FB7B2` 1.5px 实色边框；hover = 半透明底色叠加 10% 亮度，无边框）。

### 颜色无障碍

- **颜色不是唯一通道**（WCAG AA 要求）。所有 24px 以下的颜色编码必须同时使用形状/图标/文本三重区分（GDD #16 C.9 硬性要求）：
  - 航线知识状态：颜色（绿/黄/红）+ 线条样式（实线/虚线/发光）+ 端点形状（实心圆/空心圆/虚线圆）——三重编码（GDD #9 规则 5-6）
  - 风险等级：颜色 + 图标形状 + 文字标签
  - 可选择性：颜色（正常/置灰）+ 交互响应（可点击/无响应）+ tooltip 文字说明
- **对比度**：所有文本在羊皮纸底色（`#E4D2B3` 帆布米）上的对比度 ≥ 4.5:1（WCAG AA）。危险红 `#D4644B` 在帆布米上 4.52:1（GDD #16 C.9 色板偏差说明）。航线颜色（绿/黄/红）的选择同样需要满足在羊皮纸底色上的对比度要求。
- **风险标签字体最小 14px**（用户任务要求）。来源标注可为 12px（次要信息）。
- 闪烁 `?` 标记（隐藏风险标签）：闪烁频率不超过 3 次/秒，避免光敏性触发。提供静止替代 —— tooltip 中显示完整"未知风险"文字。

### 其他无障碍考量

- **只读元素跳过 Tab 链**：标签、纯文本、状态条设置 `focus_mode = Control.FOCUS_NONE`（GDD #16 C.5）。
- **灰显按钮仍可聚焦**：不可用按钮可 Tab 聚焦，Enter 无响应 + tooltip 显示原因（如"材料不足，无法出航"）（GDD #16 C.5）。
- **ERROR 状态重试按钮**：始终可键盘聚焦和激活。
- **屏幕阅读器**：Godot 4.5+ AccessKit 支持（GDD #16 引擎参考）。航图状态变化时更新可访问性标签。航线选中/取消选中时需通知屏幕阅读器。

---

## Localization Considerations

[To be designed]

---

## Acceptance Criteria

以下 6 条验收标准可由 QA 测试人员独立验证为通过/失败。测试前置条件：四大内容域（`routes`, `world`, `intel`, `threats`）均为 `COMPLETE`；玩家停靠于 `location.glass-harbor`；MVP 两条航线配置有效（`route.sky-reef-arc-01` = `identified` + `safe`, `route.storm-cut-01` = `rumored` + `storm` + `low-visibility` 隐藏）。

**AC-UX-01 — 航图打开与初始状态**

GIVEN 玩家从 Hub 按 M 键或完成出航确认流程，WHEN 航图加载完成（四大域 COMPLETE 检查通过），THEN 航图以 0.8s 羊皮纸纹理渐变进入 `BROWSING` 状态。航图区域（约 70% 屏幕）渲染两条航线：`route.sky-reef-arc-01` 以绿色实线 + 空心圆端点显示；`route.storm-cut-01` 以黄色 2px 虚线 + 60% 透明度 + 虚线圆端点显示。侧边面板（约 30% 屏幕）可见但为空。谣言筛选开关默认"显示"（`hide_rumored = false`）。Esc 键可按，按下后返回 Hub。

**AC-UX-02 — 航线悬停与选中交互**

GIVEN 航图处于 `BROWSING` 状态，WHEN 鼠标悬停在 `route.sky-reef-arc-01` 线条上，THEN 线条微亮，侧边面板显示航线名称"sky-reef-arc-01"、风险标签摘要（`safe`）、来源标注"空港基础航图"、一句话概述。WHEN 点击该航线，THEN 航线发出暖金色脉冲 0.3s，其余航线降至 40% 透明度，侧边面板展开完整详情，"确认出航"按钮出现在面板底部并自动获得焦点。WHEN 按 Esc，THEN 航线恢复正常，面板清空，按钮消失。Tab 键可在两条航线间按 `route_display_order` 顺序切换焦点，Enter 键选中行为与鼠标点击一致。

**AC-UX-03 — 两步出航确认与不可逆锁定**

GIVEN 航图处于 `ROUTE_SELECTED` 状态，已选中 `route.sky-reef-arc-01`，WHEN 点击"确认出航"按钮（第一步），THEN 确认浮层弹出，显示航线名称、风险摘要（刷新后的最新数据）、预估距离带、"出航"和"取消"按钮。WHEN 点击"出航"（第二步），THEN 墨迹扩散动画 0.6s 沿航线从起点描摹至终点，随后锁定 1.2s，所有交互禁用。锁定期间点击任意区域、按 Tab/Enter/Esc 键均无响应。锁定结束后航图淡出至黑屏。`route_committed` 事件恰好发射一次。

**AC-UX-04 — 谣言筛选开关**

GIVEN 航图处于 `BROWSING` 状态，两条航线可见（含 `rumored` 的 `route.storm-cut-01`），WHEN 切换谣言筛选为"隐藏"（`hide_rumored = true`），THEN `route.storm-cut-01` 即时从航图消失（无动画延迟），`route.sky-reef-arc-01` 保持可见。WHEN 切换回"显示"（`hide_rumored = false`），THEN `route.storm-cut-01` 即时恢复，恢复传闻样式（黄色虚线 60%）。GIVEN 两条航线均为 `rumored` 且筛选器为"隐藏"，THEN 航图显示空状态消息："所有航线均为传闻级别 —— 关闭'隐藏传闻航线'以查看。"（区别于"航图上尚无已知航线"消息）。

**AC-UX-05 — 空状态与 UNAVAILABLE 状态**

GIVEN 所有航线 `knowledge_state = unknown`（首次打开航图，无任何情报），WHEN 航图加载完成，THEN 航图进入 `BROWSING` 状态但零航线渲染。显示羊皮纸背景 + 边缘雾渐变 + 居中消息"航图上尚无已知航线。在世界中收集情报以揭示航线。"不进入 ERROR。GIVEN 所有已知航线起点不等于当前停靠港口，THEN 所有航线置灰（`UNAVAILABLE`），tooltip 显示"不在当前港口"，居中消息显示"当前港口 [名称] 无可用出发航线。"GIVEN 任意内容域非 COMPLETE，THEN 航图进入 ERROR 状态，显示失败域名称和重试按钮。

**AC-UX-06 — 键盘完整流程 + 颜色无障碍三重编码**

GIVEN 仅使用键盘（Tab/Enter/Esc），WHEN 执行完整的"打开航图 → Tab 选航线 → Enter 选中 → Tab 到确认按钮 → Enter 打开浮层 → Tab 到出航 → Enter 确认"流程，THEN 所有步骤的视觉反馈与鼠标操作一致（脉冲、面板展开、墨迹动画）。GIVEN 航图渲染两条航线，WHEN 仅通过灰度/形状观察（模拟色觉障碍），THEN 可区分 `identified` 航线（实线 + 空心圆）和 `rumored` 航线（虚线 + 虚线圆），不依赖颜色。所有风险标签文字 ≥ 14px，在羊皮纸底色上满足 WCAG AA 对比度（≥ 4.5:1）。

---

## Open Questions

[To be designed]
