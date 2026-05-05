# UX Spec: 探索 / 搜撤场景

> **Status**: In Design
> **Author**: lieinsay + ux-designer
> **Last Updated**: 2026-05-05
> **Journey Phase(s)**: 核心循环第三步 — 航程抵达后、返航前
> **Template**: UX Spec
> **GDD Reference**: `design/gdd/exploration-scavenge-scenario.md`, `design/gdd/ui-hud-chart-interface.md`

---

## Purpose & Player Need

探索场景是《云海织航》核心循环的第三步 —— 航程抵达后、返航前的"搜撤"阶段。玩家离开飞艇，进入一个由遭遇上下文（EncounterContext）定义的未知空间，在有限时间内搜索物品、收集情报、应对威胁，然后提取回船。

玩家在此场景中的核心需求是：**在可控的紧张中行使判断力**。探索不是实时动作 —— 每个威胁提供三种响应选项，玩家有时间阅读、权衡、选择。紧张感来自空间限制（背包容量、Pool 5 余量）、信息不对称（威胁预览由侦察效率决定）、以及"再多搜一个点还是现在提取"的推拉张力。

探索场景也是 Pillar 1「规划先于冒险」的兑现点：玩家在航图上选择航线时看到的风险标签（如 `storm`、`low-visibility`），在探索场景中转化为具体的威胁点、地形限制和情报机会。规划的质量直接影响探索的难度。

---

## Player Context on Arrival

玩家到达探索场景有两种模式，由航行结果决定：

**模式一：安全抵达（航行顺利）**

玩家从飞艇舱门走出，场景从黑屏淡入。第一帧画面：飞艇停靠在提取锚点旁，玩家角色站在舱门口。镜头缓慢拉远（0.5s）展示周围环境 —— 被云海环绕的浮岛/废墟/遗迹。HUD S5 覆盖层渲染：5 格 Pool 全满、侦察效率指示器显示当前值、区域名称标签淡入。

玩家情绪：期待、好奇。"这个地方有什么？"

**模式二：强制着陆（航行遭遇威胁，船体受损）**

玩家从飞艇舱门走出，但场景开头附加 0.5s 的"船体损伤脉冲"动画（HUD 船体栏红色闪烁），镜头略微不稳定。HUD S5 覆盖层渲染时 Pool 5 可能有 1-2 格已消耗（因航行中消耗了资源）。

玩家情绪：压力略高。"船受伤了，这次探索需要更小心。"

---

## Navigation Position

探索场景在导航层级中是 Hub 的"子场景" —— 从 Hub 出发，经过航图（S4）和航行过渡后到达，完成探索后返回 Hub。

```
S4 航图屏幕
  │
  └──[确认出航]──→ 航行过渡（黑屏/加载）
        │
        └──→ 探索场景 (S5 常驻 HUD)
              │
              ├── [E 交互] ──→ 搜点 / 情报点 / 威胁点
              │
              ├── [Tab] ──→ S6a 背包覆盖层
              │
              ├── [I] ──→ S6b 物品详情
              │
              ├── [接近提取锚点 + E] ──→ 提取确认
              │     └── [确认] ──→ extraction_locked 1.5s ──→ 返航过渡
              │
              └── [战斗触发] ──→ S7 威胁响应界面
                    └── [选择响应] ──→ 3 种结果之一 ──→ 返回探索
```

---

## Entry & Exit Points

| # | 入口 | 触发条件 | 到达时场景状态 |
|---|------|---------|--------------|
| E1 | 航行完成（安全抵达） | 航行系统完成过渡，EncounterContext 传递 | `ARRIVING` → 0.8s 场景淡入 → `EXPLORING` |
| E2 | 航行完成（强制着陆） | 航行中遭遇威胁导致船体受损 | `ARRIVING` + 船体损伤脉冲动画 → `EXPLORING` |

| # | 出口 | 触发条件 | 目标 |
|---|------|---------|------|
| X1 | 主动提取 | 玩家在提取锚点按 E 并确认 | `EXTRACTING` → extraction_locked 1.5s → 返航过渡 → Hub |
| X2 | 强制提取 | Pool 5 耗尽（5/5 格全空）或所有搜点已搜索 | 自动触发提取 → `EXTRACTING` → 返航过渡 → Hub |
| X3 | 撤退 | 威胁响应中选择"撤退"选项 | 立即返航，可能损失部分物品 |

---

## Layout Specification

### Information Hierarchy

1. **Pool 5 余量**（最高优先级）—— 屏幕顶部 HUD，5 格指示器。每消耗一格触发视觉警告（最后 2 格红色闪烁）
2. **当前区域名称** —— HUD 顶部居中，进入新区时淡入
3. **侦察效率指示器** —— HUD 顶部小图标 + 数值，影响威胁预览精度
4. **提取锚点方向指示** —— HUD 边缘指南针/箭头，始终指向提取锚点
5. **交互提示 `[E]`** —— 玩家接近搜点/情报点/威胁点时出现
6. **容量警告** —— 背包接近满时 HUD 闪烁提示

### Layout Zones

- **游戏视图（85% 屏幕）**：2D 俯视/侧视探索区域。玩家角色居中，场景包含搜点、情报点、威胁点、提取锚点。风险模型为同心圆：提取锚点为中心，越远威胁越密集
- **HUD 覆盖层（15% 屏幕，顶部 + 边缘）**：Pool 5 条、侦察指示器、区域名称、提取方向指南针、容量警告

### ASCII Wireframe
```
+============================================================================+
| S5 HUD (顶部)  [⚡⚡⚡⚡⚡] Pool 5  [👁 78%] 侦察   区域: 云海废墟           |
+============================================================================+
|                                                                             |
|                        ☁️ ☁️ 云海背景 ☁️ ☁️                                 |
|                                                                             |
|              [?] 搜点C                    [⚠️] 威胁点A                       |
|                    (距离: 中)                  (距离: 远)                    |
|                                                                             |
|     [📦] 搜点A                                          [⚠️] 威胁点B         |
|       (距离: 近)                                        (距离: 中)           |
|                                                                             |
|                      [@] 玩家                                              |
|                                                                             |
|            [!] 情报点A                                                      |
|              (距离: 近)                                                     |
|                                                                             |
|                              [⚓] 提取锚点                                   |
|                                                                             |
|              [📦] 搜点B                                                     |
|                (距离: 近)         ← 指南针指向提取锚点                       |
|                                                                             |
+============================================================================+
| 操作提示: WASD 移动  E 交互  Tab 背包  I 物品详情  Esc 暂停                 |
+============================================================================+
```

---

## States & Variants

| # | 状态 | 触发条件 | UI 表现 |
|---|------|---------|--------|
| S1 | **ARRIVING** | 航行完成，场景开始加载 | 黑屏淡入（0.8s），飞艇舱门出现，玩家角色从舱门走出。HUD 逐渐渲染 |
| S2 | **EXPLORING** | 到达动画完成 | 完整 HUD 显示，所有搜点/情报点/威胁点按距离渲染。玩家自由移动和交互 |
| S3 | **EXTRACTING** | 玩家在提取锚点确认提取，或强制提取触发 | 提取动画 1.5s：玩家角色向锚点移动，屏幕渐亮（暖金色），物品清点快速滚动 |
| S4 | **DEPARTED** | 提取完成 | 黑屏过渡 → 返航 → Hub |

### 状态变体

| 变体 | 触发 | 区别 |
|------|------|------|
| **威胁触发** | 玩家接近威胁点或威胁主动触发 | S7 威胁响应界面覆盖（模态），三种响应选项显示 |
| **容量满** | 背包达到上限 | HUD 背包图标闪烁红色，新物品无法拾取，需丢弃或消耗物品 |
| **Pool 5 低** | Pool 5 剩余 2 格或以下 | Pool 5 条最后格红色闪烁，HUD 边缘微红脉冲 |

### 场景变体（由 EncounterContext 决定）

| 变体 | 条件 | 视觉区别 |
|------|------|---------|
| **未搜刮** | 首次到达此场景 | 搜点显示 `[?]` 未搜索标记，物品未知 |
| **已搜刮** | 之前已搜索过此场景 | 搜点显示 `[✓]` 已搜索标记，不可再交互 |
| **威胁变化** | 世界修复后威胁等级改变 | 威胁点标记颜色变化（红→黄或黄→绿），新威胁可能出现 |

---

## Interaction Map

### 移动与通用交互

| 按键 | 动作 | 行为 |
|------|------|------|
| `W`/`A`/`S`/`D` | 八向移动 | 玩家在探索区域移动。受地形边界限制 |
| `E` | 交互 | 对最近的可交互目标执行操作（搜点/情报点/提取锚点） |
| `Tab` | 背包覆盖层 | 打开 S6a 背包覆盖层，显示当前携带物品列表 |
| `I` | 物品详情 | 打开 S6b 物品详情界面 |
| `Esc` | 暂停 | 打开暂停菜单 |
| 鼠标点击 | 点击移动 | 点击地面移动玩家，点击可交互目标触发交互 |

### 搜点交互
- 接近搜点 → 显示 `[E] 搜索` → 按 E → 搜索动画 1.0s → 物品获得弹窗 → 物品进入背包（若背包满则提示丢弃）
- 搜索后搜点标记变为 `[✓]`

### 情报点交互
- 接近情报点 → 显示 `[E] 调查` → 按 E → 阅读动画 1.5s → 情报文本显示 → 情报写入 IntelManager
- 情报收集后标记变为 `[✓]`

### 威胁点交互
- 接近威胁点或威胁主动触发 → S7 威胁响应界面弹出（模态）→ 显示威胁描述 + 3 种响应选项
- 选择选项 → 结果动画 → 3 种结果之一：damaged（船体受损）/ knocked back（弹回锚点方向）/ retreat（撤退返航）

---

## Events Fired

| 事件 | 信号签名 | 触发时机 | 消费者 |
|------|---------|---------|--------|
| `search_performed` | `search_performed(point_id: StringName, items_found: Array[StringName])` | 玩家完成搜点 | Resources #5, Persistence #3 |
| `item_picked_up` | `item_picked_up(item_id: StringName, quantity: int)` | 物品进入背包 | Resources #5, Feedback #17 |
| `intel_discovered` | `intel_discovered(intel_id: StringName, knowledge_state: String)` | 情报收集完成 | Intel #6, Feedback #17 |
| `threat_triggered` | `threat_triggered(threat_id: StringName, threat_type: String)` | 威胁点激活 | Combat #12, Feedback #17 |
| `extraction_started` | `extraction_started(reason: String)` | 提取流程开始 | Hub #7, Persistence #3 |
| `extraction_completed` | `extraction_completed(items_count: int, intel_count: int)` | 提取完成 | Hub #7, Persistence #3 |
| `extraction_interrupted` | `extraction_interrupted(reason: String)` | 提取被中断 | Hub #7, Feedback #17 |
| `phase_changed` | `phase_changed(old_phase: String, new_phase: String)` | 探索阶段变化 | UIManager #16 |

---

## Transitions & Animations

| 动画 | 时长 | 缓动 | 描述 |
|------|------|------|------|
| 场景进入（安全抵达） | 0.8s | ease-out | 黑屏淡入，飞艇舱门先亮起，然后场景全亮 |
| 场景进入（强制着陆） | 1.0s | ease-out | 黑屏淡入 + 0.5s 船体损伤红色脉冲 |
| 搜索动画 | 1.0s | ease-in-out | 玩家角色播放搜索动画，搜点标记从 `[?]` 变为 `[✓]` |
| 物品获得弹窗 | 0.3s | ease-out | 物品图标从搜点弹出，放大，飞入 HUD 背包图标 |
| 情报阅读 | 1.5s | ease-in-out | 半透明羊皮纸面板从中心展开，文字逐行淡入 |
| 威胁触发 | 0.3s | ease-out | 屏幕边缘红色脉冲 + S7 界面从底部滑入 |
| 威胁响应结果 | 0.5s | ease-in-out | 根据结果播放对应动画（损伤=红色闪烁，弹回=角色位移，撤退=快速淡出） |
| 提取确认 | 1.5s | ease-out | 玩家向锚点移动 + 屏幕渐亮暖金色 + 物品清点滚动 |
| 容量警告 | 持续 | 脉冲 | HUD 背包图标红色闪烁（1s 周期）直到容量释放 |
| Pool 5 消耗 | 0.2s | ease-out | 单格从亮变暗，最后 2 格附加红色闪烁 |
| 区域切换 | 0.3s | ease-out | 区域名称标签淡入，旧标签淡出 |

---

## Data Requirements

| 显示元素 | 数据来源 | 读取接口 |
|---------|---------|---------|
| Pool 5 余量 | Resources #5 | `get_pool_state("pool_5")` |
| 侦察效率 | Intel #6 | `get_scout_efficiency()` |
| 区域名称 | EncounterContext #10 | `get_current_zone_name()` |
| 搜点位置/状态 | EncounterContext #10 | `get_search_points()` |
| 情报点位置/状态 | EncounterContext #10 + Intel #6 | `get_intel_points()` |
| 威胁点位置/类型/预览 | EncounterContext #10 + Combat #12 | `get_threat_points()` |
| 提取锚点位置 | EncounterContext #10 | `get_extraction_anchor()` |
| 背包内容 | Resources #5 | `get_inventory()` |
| 船体完整性 | AirshipHub #7 | `get_hull_integrity()` |
| 物品定义 | Registry #1 | `list_by_kind("item")` |
| 探索会话状态 | Persistence #3 | `progress.exploration` 快照包 |

---

## Accessibility

- 所有交互支持键盘完整操作（WASD + E + Tab + Esc）
- 威胁指示不依赖颜色 —— 同时使用图标形状（⚠️ 三角形）和文字标签
- Pool 5 状态使用"满/空"视觉 + 数字文字双重编码
- 所有文本满足 WCAG AA 对比度（≥ 4.5:1）
- 交互目标有清晰的焦点高亮（航标青 `#4FB7B2` 轮廓）

---

## Localization Considerations

[To be designed]

---

## Acceptance Criteria

- **AC-UX-01**: 玩家从航行过渡到探索场景时，ARRIVING 动画 0.8s 正常播放，HUD 正确渲染 Pool 5 和侦察效率
- **AC-UX-02**: 玩家可靠近搜点并按 E 执行搜索，搜索动画播放，物品正确加入背包
- **AC-UX-03**: 接近威胁点时 S7 威胁响应界面弹出，三种选项均可点击并产生对应结果
- **AC-UX-04**: 在提取锚点按 E 可触发提取流程，extraction_locked 1.5s 后正确返航
- **AC-UX-05**: Pool 5 耗尽时自动触发强制提取
- **AC-UX-06**: 仅使用键盘（不碰鼠标）可完成完整的探索→搜索→提取流程

---

## Open Questions

[To be designed]
