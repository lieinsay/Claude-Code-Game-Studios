# ADR-0012: UI 屏幕状态机、模态栈与输入路由 — UIManager Autoload #16

## Status
Accepted

## Date
2026-05-05

## Summary
UIManager 作为 Autoload #16（Presentation 层），是 MVP 中所有 UI 屏幕、模态面板、非模态面板、HUD 覆盖层、输入路由和焦点管理的唯一权威。它维护屏幕状态机（HUB→CHART→EXPLORATION→SETTLEMENT→HUB 闭环）、单槽模态栈（S7 战斗覆盖例外）、4 层输入路由优先级（模态→半模态→非模态→HUD→世界交互）、以及 Godot 4.6 dual-focus 显式同步策略（鼠标点击时显式 grab_focus()、键盘/鼠标焦点分离的 Theme 样式）。UIManager 不拥有任何领域数据——所有显示数据通过直接方法调用从领域系统查询。HUD 采用信号驱动 + 脏标记批量更新（_process 零开销空闲帧）。所有 UI 状态以 Dictionary[StringName, Variant] 存储在 UIManager 内部，不通过 ADR-0003 持久化——UI 状态在场景切换和浏览器恢复时从内存重建。

## Decision Makers
User + Claude Code (technical-director pending)

## Last Verified
2026-05-05

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.2 |
| **Domain** | UI / Control — Presentation |
| **Knowledge Risk** | 🔴 HIGH — Godot 4.6 dual-focus 系统将鼠标/触控焦点与键盘/手柄焦点分离；`Control.focus_mode` 行为变更；Theme focus/hover 样式必须各自独立 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/modules/ui.md`, `docs/engine-reference/godot/modules/input.md`, `docs/engine-reference/godot/breaking-changes.md`, `design/gdd/ui-hud-chart-interface.md`, `docs/architecture/architecture.md` |
| **Post-Cutoff APIs Used** | Dual-focus system (4.6): `Control.focus_mode`, 独立的 focus/hover Theme StyleBox 注册；FoldableContainer (4.5)；Control.mouse_filter 递归禁用 (4.5)；AccessKit 屏幕阅读器支持 (4.5) |
| **Verification Required** | Godot 4.6 dual-focus: `grab_focus()` 不影响鼠标焦点的运行时验证；mouse click → `grab_focus()` 显式同步的正确性；Tab 键焦点循环在模态面板内不穿透到下层 UI；Combat Override (S7) CanvasLayer=100 渲染高于一切 UI；Web tab freeze 恢复后 full_ui_refresh 一致性；墨水扩散 ShaderMaterial 在 WebGL 2 Compatibility 渲染器上的精度 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Autoload #16 Phase 8 ui_ready)；ADR-0002 (Signal 通信协议 — typed params, sync emit, max cascade depth 2)；ADR-0006 (Web 平台约束 — 单线程、键鼠唯一输入、tab freeze delta 恢复、AudioContext 用户手势激活) |
| **Enables** | ADR-0017 (Feedback 系统 — 消费 #16 发出的 ui_* 语义事件驱动音频/VFX)；ADR-0018 (Onboarding 系统 — 消费面板打开/关闭事件和焦点状态定位引导高亮) |
| **Blocks** | 所有需要 UI 面板的系统的故事 (#5 仓库/货舱面板、#7 站点面板/出航确认、#8 模块状态灯、#9 航图屏幕、#11 探索 HUD/容量取舍/撤离读条/结算摘要、#12 战斗威胁面板、#13 修复面板、#14 摊位界面、#15 命名/嗅辨面板) |
| **Ordering Note** | 应在 ADR-0001 + ADR-0002 + ADR-0006 全部 Accepted 后 Author。Phase 8 Autoload #16 在所有 Foundation + Core + Feature Autoload 之后初始化 — 消费所有下层系统数据，被 #17/#18 消费 |

## Context

### Problem Statement

《云海织航》的 UI 层包含 12 个屏幕/面板（S1–S12），跨越 Hub、航图、探索、战斗、修复、集市、伙伴 7 个游戏上下文。GDD #16 定义了统一的交互规则——同时最多一个模态、4 层输入路由优先级、战斗威胁覆盖（唯一例外）、信号驱动的 HUD 脏标记批量更新。但这些规则分散在 GDD 的多个章节中（C.1–C.13），未被形式化为架构契约。没有 ADR-0012，每个面板的实现者可能独立解释输入路由优先级（导致 Esc 键行为不一致、焦点穿透到世界层）、模态栈规则（导致多个模态同时打开破坏状态一致性）、和 dual-focus 同步策略（导致键盘导航与鼠标点击的焦点状态分离）。同时，Godot 4.6 的 dual-focus 系统（C1 HIGH 风险）将鼠标/触控焦点与键盘/手柄焦点分离为两个独立状态——如果不在 ADR 中定义显式同步契约，UI 将在键盘导航和鼠标点击之间出现"两个焦点指示器同时可见"或"键盘焦点滞后于鼠标点击"的视觉混乱。

### Constraints

- **Godot 4.6.2 + GDScript**: 🔴 dual-focus 系统——`grab_focus()` 仅影响键盘/手柄焦点，鼠标点击不会自动同步键盘焦点
- **ADR-0001 启动顺序**: UIManager 在 Phase 8 (ui_ready) 初始化——在所有下层系统之后，在 SessionShell 的 `session_ready` 之前
- **ADR-0002 信号协议**: typed params, sync emit, max cascade depth 2, emit-after-mutation
- **ADR-0006 Web 平台**: 键鼠唯一输入设备（无手柄、无触控）；单线程执行；浏览器 tab freeze 恢复需 delta > 1.0s 检测 + full_ui_refresh
- **GDD #16 硬性约束**: 同时最多一个模态面板（S7 覆盖例外）；所有 24px 以下颜色编码必须形状+颜色+文字三重编码（WCAG AA）；全局按键（Esc/M/Tab/Enter/WASD/E）由顶层消费不向下穿透
- **WebGL 2 Compatibility 渲染器**: 墨水扩散 ShaderMaterial 的 `texture()` 采样精度需验证

### Requirements

- 12 屏清单管理: S1–S12 的打开/关闭/预加载/缓存池
- 屏幕状态机: HUB → CHART → EXPLORATION → SETTLEMENT → HUB 闭环 + departure_locked 过渡锁
- 单槽模态栈: `_modal_panel` 单槽 + `combat_override_stack` 战斗覆盖保存 + 排队策略（S7 覆盖 / S10 排队 / 其余丢弃）
- 4 层输入路由: Layer 0 (模态) → Layer 1 (半模态) → Layer 2 (非模态) → Layer 3 (HUD) → Layer 4 (世界交互)
- Godot 4.6 dual-focus 显式同步: 鼠标点击时 `grab_focus()` → 键盘焦点跟随鼠标；focus/hover 两套 Theme 样式视觉不可混淆
- HUD 更新: 信号驱动脏标记 + `_process` 批量更新（空闲帧零开销）
- 面板生命周期: 非模态面板距离驱动（预加载/打开/自动关闭）、模态面板事件驱动
- 动画时序: `create_tween()` 统一——墨水扩散 ShaderMaterial、羊皮纸翻开 0.25s、departure_locked 2.0s 等

## Decision

### 1. UIManager 作为 Autoload #16

UIManager 在 Phase 8 (ui_ready) 中初始化。`_ready()` 仅执行常量定义和信号声明；实际初始化在收到 `ui_ready` 信号后执行——此时所有下层 Autoload (#1–#15) 已完成初始化，UIManager 可以安全调用 `get_chart_state()`、`get_hull_integrity()` 等查询方法。

```
Autoload 顺序 (Phase 8):
  #16 UIManager           ← ui_ready (倒数第二，消费所有下层数据)
  随后: SessionShell 发出 session_ready → 进入 Playing
```

**UIManager 内部状态结构:**

```gdscript
# UIManager 运行时状态 — Dictionary[StringName, Variant]
# 不在 ADR-0003 持久化范围内 — UI 状态在场景切换和浏览器恢复时从内存重建

var _state: Dictionary = {
    "_active_screen": &"HUB",           # StringName — 当前全屏状态
    "_modal_panel": null,               # Control|null — 当前模态面板实例（单槽）
    "_modal_id": &"",                   # StringName — 当前模态面板 ID
    "_combat_override_stack": null,     # Dictionary|null — 被战斗覆盖的面板保存状态
    "_departure_locked": false,         # bool
    "_non_modal_panels": [],            # Array[Control] — z-index 排序
    "_dirty_flags": {},                 # Dict[StringName, bool]
    "_pending_payloads": {},            # Dict[StringName, Variant]
    "_panel_cache": {},                 # Dict[StringName, Control] — LRU，最大 2
}
```

### 2. 屏幕状态机

```
HUB ──[use_gangway/use_helm]──→ HUB + S3 模态（出航确认）
 │
 ├──[press_m_key]──→ CHART (S4 全屏航图)
 │
HUB + S3 ──[departure_confirmed]──→ DEPARTURE_LOCKED (2.0s, 所有面板关闭)
DEPARTURE_LOCKED ──[lock_timer_complete]──→ CHART (S4)
 │
CHART ──[route_selected]──→ CHART_ROUTE_SELECTED（侧边面板展开）
CHART_ROUTE_SELECTED ──[departure_confirmed]──→ CHART_DEPARTURE_CONFIRMED
CHART_DEPARTURE_CONFIRMED ──[lock_complete]──→ VOYAGE (黑屏过渡)
CHART / CHART_ROUTE_SELECTED ──[esc_pressed]──→ HUB
 │
VOYAGE ──[encounter_context_ready]──→ EXPLORATION (S5 HUD)
 │
EXPLORATION ──[threat_triggered]──→ EXPLORATION + S7 覆盖
EXPLORATION + S7 ──[threat_resolved]──→ EXPLORATION（恢复被覆盖面板）
 │
EXPLORATION ──[extraction_started]──→ EXTRACTING + S6b
EXTRACTING ──[extraction_interrupted]──→ EXPLORATION
EXTRACTING ──[extraction_complete]──→ SETTLEMENT + S6c
 │
SETTLEMENT + S6c ──[settlement_confirmed]──→ HUB_ARRIVING
HUB_ARRIVING ──[arrival_complete + naming_eligible]──→ HUB + S10
HUB_ARRIVING ──[arrival_complete + !naming_eligible]──→ HUB
```

**状态枚举:**

```gdscript
const SCREEN_HUB: StringName = &"HUB"
const SCREEN_CHART: StringName = &"CHART"
const SCREEN_CHART_ROUTE_SELECTED: StringName = &"CHART_ROUTE_SELECTED"
const SCREEN_CHART_DEPARTURE_CONFIRMED: StringName = &"CHART_DEPARTURE_CONFIRMED"
const SCREEN_DEPARTURE_LOCKED: StringName = &"DEPARTURE_LOCKED"
const SCREEN_VOYAGE: StringName = &"VOYAGE"
const SCREEN_EXPLORATION: StringName = &"EXPLORATION"
const SCREEN_EXTRACTING: StringName = &"EXTRACTING"
const SCREEN_SETTLEMENT: StringName = &"SETTLEMENT"
const SCREEN_HUB_ARRIVING: StringName = &"HUB_ARRIVING"
```

**强制过渡保护:**
- `departure_locked = true` 期间: `open_screen()` / `open_modal()` 静默拒绝，`force_close_all_panels()` 已执行
- CHART_DEPARTURE_CONFIRMED 不可逆——确认出航后无法返回 BROWSING
- EXTRACTING 阶段除 S7 威胁触发外不可取消

### 3. 模态栈

**规则 1 — 单模态**: `_modal_panel` 单槽，同时最多一个模态面板可见。

**规则 2 — 战斗覆盖 (CombatOverride)**: S7 是唯一可覆盖当前模态的面板。
```
覆盖流程:
1. 保存被覆盖面板状态: _combat_override_stack = {
     panel_id: _modal_id,
     data_context: <当前数据上下文>,
     scroll_offset: <滚动位置>,
     selected_index: <选中索引>,
   }
2. 被覆盖面板 process_mode = DISABLED, modulate.a = 0.2
3. S7 渲染在独立 CanvasLayer (layer = 100)
4. 恢复规则:
   - 应急处理/硬扛后: 从 stack 完整恢复被覆盖面板
   - 撤退后: 丢弃被覆盖面板 (撤退 = 放弃当前探索动作)
```

**规则 3 — 排队策略:**
- S7 (威胁): 覆盖——无视当前模态
- S10 (命名): 排队——当前模态关闭后自动打开。命名模态在到达序列中触发，此时通常无其他模态
- 其余模态: 丢弃——转为 Toast "当前无法操作"

**规则 4 — 非模态面板共存**: 多个非模态面板 (S2 非模态实例/S11/S12) 可同时打开，z-index 递增。

### 4. 4 层输入路由

事件传播链（优先级从高到低，通过 `_unhandled_input` 传播 + `mouse_filter` 阻断）:

```
Layer 0 (最高)  模态面板 (S3/S6a/S6c/S7/S8/S9/S10)
                Control.mouse_filter = STOP
                若未消费: _unhandled_input 继续 ↓
Layer 1        半模态覆盖 (S6b 撤离读条)
                Control.mouse_filter = STOP
                若未消费: ↓
Layer 2        非模态面板 (S2 非模态实例/S11/S12)
                Control.mouse_filter = PASS (允许下方 HUD tooltip)
                若未消费: ↓
Layer 3        常驻 HUD 覆盖层 (S1/S5)
                Control.mouse_filter = IGNORE (点击穿透到世界层)
                键盘事件不穿透 ↓
Layer 4        世界交互层 (WASD 移动 / E Use / 锚点检测)
```

**Layer 判定逻辑:**

```gdscript
func _get_active_input_layer() -> int:
    if _state["_modal_panel"] != null:
        return 0  # 模态面板活跃
    if _is_semi_modal_open():  # S6b
        return 1
    if _state["_non_modal_panels"].size() > 0:
        return 2
    if _is_hud_overlay_active():  # S1 or S5
        return 3
    return 4  # 世界交互
```

**全局按键（由 UIManager._unhandled_input 顶层消费）:**

| 按键 | Layer 0-3 行为 | Layer 4 行为 |
|------|---------------|-------------|
| `Esc` | 关闭当前模态→关闭非模态面板→航图取消选择→无效 | 无效（世界层不消费） |
| `M` | 被阻断 | Hub 中打开航图 S4 |
| `Tab` / `Shift+Tab` | 模态内焦点循环；Layer 3 HUD 元素间循环 | 不可用 |
| `Enter` | 确认当前焦点元素动作 | — |
| `WASD` | Layer 0-2 被阻断；Layer 3 HUD 上不触发移动 | 玩家移动 |
| `E` | Layer 0-2: 模态确认/Use；S7 中=应急处理 | 世界交互 Use |

**例外:**
- S7 (战斗面板) 中 Esc 无效——必须选择响应 (E/T/R)。视觉提示: "选择一个响应以继续"
- S10 (命名模态) 中 Esc = 跳过命名

### 5. Godot 4.6 Dual-Focus 同步策略

Godot 4.6 将键盘焦点 (`grab_focus()`) 与鼠标悬浮 (`hover` 样式) 分离为两个独立状态。UIManager 必须显式同步：

**5a. 鼠标点击时显式同步键盘焦点:**

```gdscript
# 所有可交互 Control 的 gui_input 或 _on_pressed 中:
func _on_button_pressed() -> void:
    grab_focus()  # 键盘焦点跟随鼠标点击
    # ... 按钮逻辑 ...

# 全局规则: 任何 Control 在 MOUSE_BUTTON_PRESSED (button=LEFT) 时
# 必须调 grab_focus() — 包括 Button、ItemList 项、TextEdit、SpinBox
```

**5b. Theme focus/hover 样式分离（视觉不可混淆）:**

```gdscript
# focus 样式（键盘焦点）: 航标青 #4FB7B2 1.5px 实色边框
theme.set_stylebox(&"focus", &"Button", focus_stylebox)
# → StyleBoxFlat: border_color = Color("#4FB7B2"), border_width = 1.5

# hover 样式（鼠标悬浮）: 半透明底色叠加 10% 亮度，无边框
theme.set_stylebox(&"hover", &"Button", hover_stylebox)
# → StyleBoxFlat: bg_color 叠加 10% 亮度，border_width = 0
```

**5c. 模态打开时自动焦点:**

```gdscript
func _open_modal(panel_id: StringName, data: Dictionary) -> void:
    # ... 实例化面板、绑定数据 ...
    _state["_modal_panel"] = panel_instance
    _state["_modal_id"] = panel_id
    # 自动将键盘焦点移到面板内第一个可交互元素
    await get_tree().process_frame
    _focus_first_interactable(panel_instance)
```

**5d. 模态关闭时焦点恢复:**

```gdscript
func _close_modal() -> void:
    var previous_focus := _state["_modal_panel"].get_viewport().gui_get_focus_owner()
    # 关闭面板...
    if is_instance_valid(previous_focus) and previous_focus is Control:
        previous_focus.grab_focus()
    else:
        _focus_first_interactable_in_active_screen()
```

**5e. 只读元素自动跳过 Tab 链:**

```gdscript
# 标签、纯文本、状态条 — focus_mode = FOCUS_NONE
readonly_label.focus_mode = Control.FOCUS_NONE
# 灰显按钮 — 仍可 Tab 聚焦，但 Enter 无响应 + tooltip 显示原因
disabled_button.disabled = true
disabled_button.tooltip_text = tr("UI_MATERIALS_INSUFFICIENT")
```

### 6. HUD 更新策略

**核心原则**: 信号驱动 + 脏标记批量更新。HUD 永不做 `_process()` 轮询。

```gdscript
# 信号 → 脏标记
func _on_signal(element_id: StringName, payload: Variant) -> void:
    _state["_dirty_flags"][element_id] = true
    _state["_pending_payloads"][element_id] = payload

# 批量更新 — process_priority = -10 (渲染前执行)
func _process(_delta: float) -> void:
    if _state["_dirty_flags"].is_empty():
        return  # 空闲帧零开销
    for element_id in _state["_dirty_flags"]:
        _update_hud_element(element_id, _state["_pending_payloads"][element_id])
    _state["_dirty_flags"].clear()

# Web tab freeze 恢复
func _notification(what: int) -> void:
    if what == NOTIFICATION_APPLICATION_RESUMED:
        if Engine.get_process_frames() > 0 and _last_delta > 1.0:
            _request_full_ui_refresh()
```

**信号 → HUD 元素映射（关键订阅）:**

| 信号 (来源) | 目标元素 | 更新内容 |
|------------|---------|---------|
| `hull_integrity_changed` (#8) | S1/S5 船体条 | 分段亮起数 + 波段色 |
| `hull_band_changed` (#8) | S1/S5 船体条颜色 | 绿/黄/红切换 |
| `storage_changed` (#5) | S1 仓库余量 | 文本 + 容量条宽度 |
| `cargo_changed` (#5) | S1 货舱装载 | 数值 或 "无货舱" |
| `carried_changed` (#5) | S5 随身物品栏 | 指定格图标+数量；满格时橙色边框 |
| `search_progress_changed` (#11) | S5 搜索计数 | "3/6" 文本 |
| `scout_preview_changed` (#11) | S5 威胁预览 | 三态切换 |
| `module_state_changed` (#8) | S1 模块状态灯 | ✓/⚡/○ 形状+颜色双编码 |
| `currency_changed` (#5) | S9/S1 货币 | 更新余额显示 |

### 7. 面板生命周期

**非模态面板 (S2非模态/S11/S12):**

```
PROXIMITY_ENTER (1.5× anchor_radius) → PRELOAD → READY → ACTIVE → PROXIMITY_EXIT (2× anchor_radius) → CLOSE
```
- 进入 1.5× 锚点半径: `_preload_panel_data()` 异步加载
- 玩家按 Use: 面板打开 (0.25s 羊皮纸翻开的 `create_tween()`)
- 离开 2× 锚点半径: 面板自动关闭 (0.15s 合上动画)
- 玩家手动 Esc: 立即关闭

**模态面板 (S3/S6a/S6c/S7/S8/S9/S10):**
- 不依赖距离——由领域事件触发
- 仅手动关闭 (Esc / 按钮 / 战斗结算等系统事件)
- S6b 撤离读条: 半模态——事件驱动，不可手动关闭 (除非 S7 打断)

**全屏面板 (S4 航图):**
- 场景级别 visible 切换，不经过 `change_scene`
- 进出由屏幕状态机控制

**懒加载策略:**
- S4: 首次进入时 `load()` → 缓存 PackedScene (5-20ms)
- S2: 单个通用 `StationDetailPanel` 模板——数据从 Registry 绑定，非 10 个独立场景
- S7: HUD 初始化时 `preload()` (探索中点触发不可等待)
- 缓存池: 最大 2 面板实例 (LRU 淘汰)。场景切换时清空

### 8. 动画与过渡时序

所有 UI 动画使用 `create_tween()` (SceneTreeTween)——禁止手动 `_process()` 插值。

| 动画 | 时长 | 缓动 | 触发 |
|------|------|------|------|
| 面板翻开（羊皮纸） | 0.25s | ease-out | 非模态面板打开 |
| 面板合上 | 0.15s | ease-in | 非模态面板关闭 |
| departure_locked | 2.0s | — | 出航确认后 |
| 航线选中脉冲 | 0.3s | ease-in-out | 航图选中航线 |
| 墨水扩散 | 0.6s | ease-out | 确认出航第二步 (ShaderMaterial) |
| 出发口封闭+锁定 | 1.2s | linear | 墨水扩散完成后 |
| 撤离读条 | 2.5s (可配置) | linear | 确认撤离 |
| 进度 Toast | 3.0s (入场 0.2s) | ease-out→ease-in | 修复提交 / 物品转移 |
| 结算摘要入场 | 0.5s | ease-out | 撤离完成 |
| 命名模态弹出 | 0.3s | ease-out | 到达序列中 naming_eligible |

**动画性能约束:**
- 墨水扩散: ShaderMaterial + uniform `progress: 0→1` (GPU 侧)，禁止 Canvas `draw_*()` 逐帧绘制
- 全屏羊皮纸: `NinePatchRect` 9-slice 展开，不使用整张 2048×2048 纹理
- 所有 Tween 使用 `Tween.EASE_OUT` / `Tween.EASE_IN` 预设，不自定义缓动曲线

### 9. 上游数据接口 (Read-Only Direct Calls)

UIManager 不拥有任何领域数据——所有显示数据通过直接方法调用从领域系统获取:

```gdscript
# 按 ADR-0002 read-vs-signal 边界: 读取查询 = 直接方法调用
func _query_screen_data(screen_id: StringName) -> Dictionary:
    match screen_id:
        &"S1":  # Hub HUD
            return {
                "hull": AirshipModuleSystem.get_hull_state(),
                "modules": AirshipModuleSystem.get_module_states(),
                "storage": ResourcesManager.get_storage_summary(),
                "cargo": ResourcesManager.get_cargo_summary(),
                "currency": ResourcesManager.get_currency(),
            }
        &"S4":  # Chart Screen
            return Chart.get_chart_state()
        &"S7":  # Combat Threat Panel
            return Combat.build_threat_context()
        &"S8":  # Repair Panel
            return WorldRepair.get_repair_state(active_node_id)
        # ... 其余面板同理
```

下游系统禁止缓存 UIManager 查询结果——每次打开面板时重新调用 `_query_screen_data()`。

### 10. 下游语义事件 (Signals for #17 Feedback)

UIManager 在用户交互时刻发射语义事件——供 #17 消费触发音频/VFX:

```gdscript
# 10 个语义事件信号 — 遵循 ADR-0002 typed params, sync emit
signal ui_route_selected(route_id: StringName, route_name: String)
signal ui_departure_confirmed(route_id: StringName, departure_mode: StringName)
signal ui_threat_response_chosen(threat_id: StringName, response: StringName)
signal ui_repair_submitted(node_id: StringName, materials: Dictionary)
signal ui_purchase_confirmed(stall_id: StringName, good_id: StringName, quantity: int, total_cost: int)
signal ui_item_transferred(item_id: StringName, from_pool: StringName, to_pool: StringName, qty: int)
signal ui_naming_confirmed(partner_id: StringName, chosen_name: String)
signal ui_settlement_closed(voyage_id: StringName, items_brought: Array, intel_gained: Array)
signal ui_panel_opened(panel_id: StringName)
signal ui_panel_closed(panel_id: StringName)
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      UIManager (Autoload #16)                              │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────┐       │
│  │                    SCREEN STATE MACHINE                        │       │
│  │                                                                │       │
│  │  HUB ──→ CHART ──→ EXPLORATION ──→ SETTLEMENT ──→ HUB        │       │
│  │    ↑         ↑            ↑              ↑            ↑        │       │
│  │  S1+S2     S4+M     S5+S6a/b+S7    S6c+S10       S1+S8/S9   │       │
│  └──────────────────────────────────────────────────────────────┘       │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │              MODAL STACK (single slot)                            │   │
│  │                                                                    │   │
│  │  _modal_panel: Control|null    ← 单槽                              │   │
│  │  _combat_override_stack: Dict|null  ← S7 覆盖保存                   │   │
│  │                                                                    │   │
│  │  Queue Strategy:                                                   │   │
│  │    S7 (threat)     → OVERRIDE (save → disable → CanvasLayer 100)  │   │
│  │    S10 (naming)    → QUEUE (open after current modal closes)      │   │
│  │    Others          → DISCARD → Toast "当前无法操作"                 │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │              4-LAYER INPUT ROUTING                                 │   │
│  │                                                                    │   │
│  │  Layer 0 (Modal)          → mouse_filter=STOP, 消费所有事件       │   │
│  │  Layer 1 (Semi-Modal)     → mouse_filter=STOP, 仅 S6b             │   │
│  │  Layer 2 (Non-Modal)      → mouse_filter=PASS, WASD可通过         │   │
│  │  Layer 3 (HUD Overlay)    → mouse_filter=IGNORE, 点击穿透         │   │
│  │  Layer 4 (World)          → WASD+E, 锚点检测                      │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │              GODOT 4.6 DUAL-FOCUS SYNC                             │   │
│  │                                                                    │   │
│  │  Mouse Press → grab_focus()  (explicit sync)                      │   │
│  │  Theme focus StyleBox:  #4FB7B2 1.5px border                      │   │
│  │  Theme hover StyleBox:  bg +10% brightness, no border              │   │
│  │  Tab chain: FOCUS_NONE for readonly, FOCUS_ALL for interactable   │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │              HUD UPDATE (dirty-flag batch)                         │   │
│  │                                                                    │   │
│  │  Domain signals → _dirty_flags[elem] = true                       │   │
│  │  _process (prio=-10) → batch update → _dirty_flags.clear()        │   │
│  │  Idle frames: _process zero-cost when _dirty_flags empty          │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          UPSTREAM (consumes via direct calls)                      │   │
│  │  #1  Registry    ← get_display_name / get_description            │   │
│  │  #5  Resources   ← get_carried/storage/cargo/currency            │   │
│  │  #8  Modules     ← get_hull_state / get_module_states            │   │
│  │  #9  Chart       ← get_chart_state / get_visible_routes          │   │
│  │  #11 Exploration ← get_search_progress / get_extraction_state    │   │
│  │  #12 Combat      ← build_threat_context                          │   │
│  │  #13 WorldRepair ← get_repair_state                              │   │
│  │  #14 Settlement  ← get_stall_data                                │   │
│  │  #15 Partner     ← query_partner_name / naming_prompt_eligibility│   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                          │                                                │
│  ┌───────────────────────┼──────────────────────────────────────────┐   │
│  │          DOWNSTREAM (emits semantic events)                        │   │
│  │  #17 Feedback ← ui_route_selected / ui_departure_confirmed /     │   │
│  │                  ui_threat_response_chosen / ui_repair_submitted /│   │
│  │                  ui_purchase_confirmed / ui_item_transferred /    │   │
│  │                  ui_naming_confirmed / ui_settlement_closed /     │   │
│  │                  ui_panel_opened / ui_panel_closed                │   │
│  │  #18 Onboarding ← ui_panel_opened / ui_panel_closed               │   │
│  └────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

#### UIManager 公共 API

```gdscript
# 屏幕管理
func open_screen(screen_id: StringName, data: Dictionary = {}) -> int  # returns ScreenResult
func close_screen(screen_id: StringName) -> void

# 模态面板管理
func open_modal(panel_id: StringName, data: Dictionary = {}) -> int  # returns ModalResult
func close_modal() -> void
func is_modal_open() -> bool

# 非模态面板管理
func open_non_modal(panel_id: StringName, data: Dictionary = {}) -> void
func close_non_modal(panel_id: StringName) -> void

# HUD 更新
func update_hud(element_id: StringName, payload: Variant) -> void
func request_full_ui_refresh() -> void

# 输入查询
func get_active_input_layer() -> int
func is_input_blocked_for_movement() -> bool  # Layer 0-2 活跃时返回 true
```

#### ScreenResult / ModalResult 枚举

```gdscript
enum ScreenResult {
    SUCCESS = 0,
    ERR_DEPARTURE_LOCKED = 1,
    ERR_MODAL_OPEN = 2,
    ERR_INVALID_SCREEN = 3,
}

enum ModalResult {
    SUCCESS = 0,
    ERR_ANOTHER_MODAL_OPEN = 1,
    ERR_DEPARTURE_LOCKED = 2,
    ERR_INVALID_PANEL = 3,
    ERR_QUEUED = 4,  # S10 排队成功
}
```

#### fetch_focus_owner 恢复协议

```gdscript
# 面板关闭时焦点恢复
func _close_modal() -> void:
    var focus_owner := get_viewport().gui_get_focus_owner()
    _state["_modal_panel"].queue_free()
    _state["_modal_panel"] = null
    _state["_modal_id"] = &""
    emit_signal("ui_panel_closed", closed_panel_id)
    # 恢复焦点
    if is_instance_valid(focus_owner):
        focus_owner.grab_focus()
    else:
        _focus_first_interactable_in_active_screen()
```

## Alternatives Considered

### Alternative A: 每个面板独立场景自管输入（无统一 UIManager）

- **Description**: 每个面板（S2-S12）作为独立场景实例，各自的 `_unhandled_input` 独立处理输入，不使用集中式输入路由
- **Pros**: 面板自包含，减少集中式 UIManager 的复杂度
- **Cons**: 无法执行全局规则（同时最多一个模态、Esc 统一返回、S7 战斗覆盖）；输入优先级冲突——两个面板同时 claim 输入时无仲裁机制；模态栈规则（S7 覆盖/S10 排队/其余丢弃）无法在面板级实现
- **Rejection Reason**: GDD #16 明确定义了单槽模态栈和 4 层输入路由优先级——这些规则本质上是跨面板协调的，必须由一个集中式 UIManager 执行。面板自管输入无法实现"战斗覆盖当前模态"的跨面板行为

### Alternative B: 使用 Godot 内置 Focus 系统替代自定义输入路由

- **Description**: 完全依赖 Godot 的 `Control.focus_mode` 和内置 Tab 导航，不实现 4 层输入路由。模态面板使用 `process_mode = DISABLED` 阻断下层——其余规则由引擎处理
- **Pros**: 更少的自定义代码；利用引擎内置行为；减少维护负担
- **Cons**: Godot 内置 focus 没有"模态栈"概念——不能区分 Layer 0 模态和 Layer 2 非模态面板的输入阻断差异；无法实现 S7 战斗覆盖的 CanvasLayer=100 特殊层级；全局按键（Esc 统一返回、M 快捷航图、WASD 阻断）的行为无法在纯 focus 系统中实现；`process_mode = DISABLED` 阻断整个子树——包括动画和信号连接
- **Rejection Reason**: GDD 的 4 层输入路由有明确的差异化阻断语义——模态阻断移动、非模态不阻断、HUD 点击穿透——Godot 内置 focus 系统不具备这种层级化输入路由能力。S7 战斗覆盖需求（保存被覆盖面板状态、独立 CanvasLayer 渲染、根据战斗结果选择性恢复）超出了引擎 focus 系统的设计范围

### Alternative C: UIManager 持久化 UI 状态到 ADR-0003

- **Description**: UIManager 将当前活跃面板、焦点位置、滚动偏移等 UI 状态纳入 `progress.ui` snapshot package
- **Pros**: 浏览器崩溃后可以恢复到确切的 UI 状态
- **Cons**: UI 状态高度依赖场景结构——读档时场景可能不同（如读档时不在探索中）；序列化和恢复 Control 节点的引用链复杂且脆弱；Web tab freeze 恢复已经通过内存保留 + full_ui_refresh 处理
- **Rejection Reason**: UI 状态的最佳恢复策略是从内存重建（Web tab freeze 保留内存）或从领域系统数据重新渲染（新场景/读档时）。持久化 UI 状态增加了序列化复杂度而收益有限——修复进度、物品状态等在领域系统中已持久化，UI 从这些数据重建即可

## Consequences

### Positive

- **单槽模态栈 + 战斗覆盖**: 同时最多一个模态面板（S7 例外），消除多个模态同时打开的状态混乱。S7 覆盖保存完整上下文——战斗结束后玩家不丢失决策上下文
- **4 层输入路由**: 层级化阻断语义明确——模态阻断移动、非模态不阻断、HUD 点击穿透——玩家在打开嗅辨面板时仍可移动
- **Dual-focus 显式同步**: 鼠标点击时 grab_focus() 确保键盘导航与鼠标点击焦点一致；focus/hover Theme 样式视觉不可混淆
- **信号驱动 HUD**: 脏标记批量更新——`_process` 空闲帧零开销；Web tab freeze 恢复通过 delta > 1.0s 检测触发全量刷新
- **懒加载 + 缓存池**: S7 预加载确保战斗中无等待；通用 StationDetailPanel 模板避免 10 个独立面板场景

### Negative

- **Autoload #16 复杂度**: UIManager 是 MVP 中最大的 Autoload——集中管理 12 屏、模态栈、输入路由、HUD 更新、焦点管理、动画时序
- **Godot 4.6 dual-focus 的持续验证负担**: 每个需要键盘导航的 Control 必须显式处理 MOUSE_BUTTON_PRESSED → grab_focus()——漏掉一处就导致"鼠标点击后 Tab 键焦点不在点击处"的 bug
- **WebGL 2 墨水扩散 Shader 风险**: ShaderMaterial + uniform progress 在 Compatibility 渲染器上的 `texture()` 采样精度未经验证——可能需要回退方案

### Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Godot 4.6 dual-focus 导致"两个焦点指示器同时可见" | Medium — 需每个交互控件显式处理 | Medium — 视觉混乱，玩家困惑 | 全局基类 `FocusSyncButton` / `FocusSyncItemList` 封装 mouse press→grab_focus() 逻辑，所有可交互控件继承基类 |
| 墨水扩散 Shader 在 WebGL 2 上精度不足 | Low — 4.6 Compatibility 渲染器已成熟 | Medium — 出航确认动画降级 | 回退: 使用 `create_tween()` 控制多个 `ColorRect` 片段沿航线路径依次显示——效果略逊但功能等价 |
| `combat_override_stack` 保存的引用在恢复时已失效 | Very Low — S7 生命周期内被覆盖面板不会被销毁（仅 DISABLED） | Low — 恢复失败，面板数据丢失 | 被覆盖面板不销毁只禁用；恢复时检查 `is_instance_valid()`；若已失效则重新从数据源 bind |
| 浏览器 Tab freeze 恢复后 HUD 脏标记与领域状态不一致 | Low — freeze 期间不收信号 | Medium — HUD 显示过期数据 | `NOTIFICATION_APPLICATION_RESUMED` 中 delta > 1.0s 强制 `_request_full_ui_refresh()` |
| 12 屏的 Skin/Tuning 值跨面板不一致 | Medium — 多个面板独立使用 Theme 资源 | Low — 颜色/字体视觉碎片化 | 单一 `ui_theme.tres` 定义所有颜色常量、字体规格、focus/hover/disabled StyleBox——所有面板共享此 Theme |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| ui-hud-chart-interface.md | C.1 12 屏清单 (S1–S12) | UIManager 的 open_screen/open_modal/open_non_modal API + 面板缓存池 |
| ui-hud-chart-interface.md | C.2 屏幕流状态机 (HUB→CHART→EXPLORATION→SETTLEMENT→HUB) | 屏幕状态机 §2 + SCREEN_* 枚举 + 强制过渡保护 |
| ui-hud-chart-interface.md | C.3 模态栈规则 (单模态 + 战斗覆盖 + 排队策略) | 模态栈 §3 + _modal_panel 单槽 + _combat_override_stack + Queue Strategy |
| ui-hud-chart-interface.md | C.4 4 层输入路由 (Modal→Semi-modal→Non-modal→HUD→World) | 4 层输入路由 §4 + _get_active_input_layer() + 全局按键表 |
| ui-hud-chart-interface.md | C.5 焦点管理 + Godot 4.6 dual-focus | Dual-Focus 同步策略 §5 — mouse press→grab_focus() + focus/hover Theme 分离 + Tab 链管理 |
| ui-hud-chart-interface.md | C.6 HUD 更新策略 (信号驱动 + 脏标记批量) | HUD 更新 §6 — _dirty_flags + _pending_payloads + _process(prio=-10) + Web freeze 恢复 |
| ui-hud-chart-interface.md | C.7 面板生命周期 (非模态距离驱动/模态事件驱动/懒加载) | 面板生命周期 §7 — preload/auto-close/cache pool |
| ui-hud-chart-interface.md | C.8 动画与过渡时序 (0.15s–3.0s, create_tween 统一) | 动画时序表 §8 — 所有动画使用 create_tween() + GPU ShaderMaterial 墨水扩散 |
| ui-hud-chart-interface.md | C.9 UI 语义权威 (统一色板 + 术语统一 + WCAG AA) | Theme focus/hover 样式 §5b — 航标青 focus + 10%亮度 hover + 颜色+形状+文字三重编码 |
| ui-hud-chart-interface.md | C.10 屏幕状态机 (11 个状态 + 16 个转换) | SCREEN_* 枚举 + 状态转换图 §2 |
| ui-hud-chart-interface.md | C.11 模态栈运行时状态 | _state Dictionary 定义 §1 — _active_screen, _modal_panel, _modal_id, _combat_override_stack, _departure_locked, _non_modal_panels |
| ui-hud-chart-interface.md | C.12 上游数据接口 (19 个查询方法) | 上游数据接口 §9 — 每个面板按 screen_id 调用领域系统直接方法 |
| ui-hud-chart-interface.md | C.13 下游语义事件 (10 个事件) | 10 个 typed signals §10 — 供 #17 Feedback 消费 |
| ui-hud-chart-interface.md | AC-20 浏览器切页恢复 | Web tab freeze 恢复 §6 — delta > 1.0s → full_ui_refresh |

## Performance Implications

- **CPU**: 空闲帧零开销 — `_process` 在 `_dirty_flags` 为空时立即返回。HUD 批量更新: O(N) N=脏元素数 (典型帧 N=0–3, < 0.1ms)。面板打开: StationDetailPanel 从 Registry `query_entity` O(1) 绑定数据 < 0.5ms。墨水扩散 Shader: GPU 侧完成 — CPU 开销仅限于 `set_shader_parameter("progress", t)` (每帧 1 次 uniform 写入)
- **Memory**: UIManager 内部 _state Dictionary ~500 bytes。像素面板缓存池（最大 2 实例）~50-100KB。HUD 常驻纹理（羊皮纸 256×256 + 航图 9-slice）~200KB。总计 < 500KB
- **Load Time**: S7 战斗面板 preload (HUD 初始化时) < 5ms。S4 航图首次 load 5-20ms。其余面板首次打开时加载 < 10ms 各
- **Network**: N/A — 单机游戏

## Migration Plan

无需迁移 — 项目尚无代码。

实现检查清单:
1. 在 project.godot 中注册 UIManager 为 Autoload #16
2. 创建单一 `ui_theme.tres`: 颜色常量、字体规格、focus StyleBox (航标青 1.5px)、hover StyleBox (10%亮度叠加)、disabled StyleBox
3. 实现 `FocusSyncButton` / `FocusSyncItemList` 基类 — 封装 mouse press→grab_focus()
4. 实现屏幕状态机 (SCREEN_* enum + 16 个状态转换)
5. 实现单槽模态栈 + `_combat_override_stack` + 排队策略
6. 实现 `_get_active_input_layer()` + `_unhandled_input` 全局按键分发
7. 实现 HUD 脏标记: `_on_signal → dirty_flags → _process batch → clear`
8. 实现面板生命周期: preload/open/close/cache pool (LRU 2)
9. 实现 `NOTIFICATION_APPLICATION_RESUMED` + delta > 1.0s full_ui_refresh
10. 创建通用 `StationDetailPanel` 模板 (替代 10 个独立站点面板)
11. 单元测试: 4 层 input routing 的 WASD/Esc 阻断矩阵、dualfocus mouse→grab_focus 同步、模态栈 S7 覆盖→恢复/S10 排队/其余丢弃、dirty-flag 批量更新、departure_locked 拒绝所有面板、Web freeze delta > 1.0s full_ui_refresh

## Validation Criteria

- 4 层输入路由: Layer 0-2 WASD 被阻断、Layer 3 WASD 被阻断但 mouse 穿透到世界、Layer 4 全部可用
- 模态栈: 同时打开两个模态时第二个被丢弃 (非 S7/S10)；S7 覆盖当前模态→CanvasLayer=100→恢复完整上下文；S10 排队在当前模态关闭后自动打开
- Dual-focus: 鼠标点击按钮后 Tab 焦点在该按钮上；键盘 focus 样式 (航标青边框) 与鼠标 hover 样式 (10%亮度) 不混淆；只读元素 Tab 链跳过
- HUD: 信号发射后下一帧 HUD 元素更新；空闲帧 `_process` 无开销
- Web 恢复: tab 切出→切回→delta > 1.0s → 全量 UI 刷新→显示与领域状态一致
- departure_locked: 2.0s 期间 `open_screen()` / `open_modal()` 静默拒绝、WASD 冻结
- 墨水扩散: ShaderMaterial progress 0→1 GPU 侧完成、60fps 不掉帧
- Esc 统一返回: 模态→非模态→航图取消选择→无效 (S7/S10 例外)

## Related Decisions

- **ADR-0001**: Autoload/Scene 架构 — UIManager 为 Autoload #16，Phase 8 初始化
- **ADR-0002**: Signal 通信协议 — 10 个 typed signals, sync emit, fan-out 模式
- **ADR-0006**: Web 平台约束 — 键鼠唯一输入、单线程、tab freeze delta 恢复、AudioContext 用户手势
- **GDD #16**: ui-hud-chart-interface.md — 完整 UI 规格、12 屏、模态栈、输入路由、双焦点
- **GDD #2**: platform-session-shell.md — SessionShell 覆盖层 (FatalError/Pause/Loading) 渲染在 UIManager 上层
- **GDD #4**: player-movement-interaction.md — InteractionRegistry 注册/焦点管理
- **GDD #17**: feedback-fx-audio.md — 消费 ui_* 语义事件
