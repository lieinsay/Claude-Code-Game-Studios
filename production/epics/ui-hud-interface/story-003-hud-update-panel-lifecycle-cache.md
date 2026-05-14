# Story 003: HUD Update, Panel Lifecycle & Cache

> **Epic**: UI / HUD / 航图界面
> **Status**: Complete
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-05-09
> **Implementation Contract**: ADR-0019 (Desktop Godot .NET/C#) governs active implementation; translate any pre-pivot wording, API names, and test paths to C# desktop equivalents before implementation.

## Context

**GDD**: `design/gdd/ui-hud-chart-interface.md`
**Requirement**: `TR-ui-003`

**ADR Governing Implementation**: ADR-0012 (§6 HUD 更新策略, §7 面板生命周期, C.6/C.7)
**ADR Decision Summary**: HUD 更新核心原则：信号驱动 + 脏标记批量更新——永不做 _process() 轮询。领域信号到达→_dirty_flags[element_id]=true + _pending_payloads[element_id]=payload 保存。_process(process_priority=-10) 中若 dirty_flags 非空→遍历脏元素更新→clear。空闲帧 dirty_flags 为空时 _process 零开销（立即返回）。desktop window freeze 恢复：NOTIFICATION_APPLICATION_RESUMED + delta > 1.0s → _request_full_ui_refresh() 绕过脏标记全量强制刷新。HUD 可见性门控：S1 仅在 Hub 场景激活时可见，S5 仅在 EXPLORING/EXTRACTING 阶段可见（ARRIVING/DEPARTED 隐藏）。11 个信号→HUD 元素映射（hull_integrity_changed→船体条、storage_changed→仓库余量、carried_changed→随身物品栏格等）。

面板生命周期：非模态面板（S2 非模态/S11/S12）距离驱动——进入 1.5× anchor_radius 预加载面板数据（异步），按 Use 打开（0.25s 羊皮纸翻开动画），离开 2× anchor_radius 自动关闭（0.15s 合上动画），手动 Esc 立即关闭。模态面板（S3/S6a/S6c/S7/S8/S9/S10）事件驱动——不依赖距离，仅手动关闭（Esc/按钮/系统事件）。全屏面板（S4）场景级 visible 切换。懒加载策略：S4 首次进入 load() 缓存 PackedScene（5-20ms）；S2 使用单个通用 StationDetailPanel 模板（数据从 Registry 绑定）；S7 HUD 初始化时 preload()；缓存池最大 2 面板实例（LRU 淘汰），场景切换时清空。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: HUD 永不 _process() 轮询——所有更新通过信号→脏标记→批量更新路径；空闲帧 _process 零开销——dirty_flags 为空时立即返回；通用 StationDetailPanel 模板替代 10 个独立站点面板场景
- Forbidden: 在 _process 中逐帧查询领域系统状态；面板打开期间自动刷新显示数据（应保持打开时快照——除非收到对应变更信号）
- Guardrail: process_priority=-10 确保批量更新在渲染前完成；delta > 1.0s 强制 full_ui_refresh 绕过脏标记

---

## Acceptance Criteria

### HUD Dirty Flag System

- [x] **AC-1**: GIVEN hull_integrity_changed(30, 25) 信号发射，WHEN _on_signal("hull_bar", payload) 调用，THEN _dirty_flags["hull_bar"]=true + _pending_payloads["hull_bar"]={old:30, new:25}
- [x] **AC-2**: GIVEN _dirty_flags 包含 3 个脏元素，WHEN _process 执行（process_priority=-10），THEN 3 个元素全部更新。_dirty_flags 清空。更新在下一帧渲染前完成
- [x] **AC-3**: GIVEN _dirty_flags 为空，WHEN _process 执行，THEN 立即返回——零开销。不遍历任何集合
- [x] **AC-4**: GIVEN 同一帧内 storage_changed 信号发射 3 次（快速连续变更），WHEN _process 执行，THEN 仅最后 1 次的 payload 被应用。中间值被覆盖——最终一致

### Signal → HUD Element Mapping

- [x] **AC-5**: GIVEN hull_band_changed(GREEN, YELLOW) 发射，WHEN 批量更新，THEN S1/S5 船体条颜色从 #5FAF5F 切换为 #E8A840 + 形状指示从 ✓ 变为 ⚡
- [x] **AC-6**: GIVEN storage_changed(920, 1000) 发射，WHEN 批量更新，THEN S1 HUD 文本更新为 "920/1000" + 容量条宽度更新为 184px（max_bar_width=200）
- [x] **AC-7**: GIVEN carried_changed(slot=4, item="lens_kit", qty=1) 发射且随身物品栏已满 5/5，WHEN 批量更新，THEN S5 指定格图标+数量更新 + 所有格橙色边框指示满载
- [x] **AC-8**: GIVEN search_progress_changed(3, 6) 发射，WHEN 批量更新，THEN S5 搜索计数文本更新为 "3/6"
- [x] **AC-9**: GIVEN scout_preview_changed(PREVIEW_FULL) 发射，WHEN 批量更新，THEN S5 威胁预览标记切换为完整标签
- [x] **AC-10**: GIVEN module_state_changed(slot=0, INSTALLED) 发射，WHEN 批量更新，THEN S1 模块状态灯显示 ✓ 形状 + 绿色
- [x] **AC-11**: GIVEN currency_changed(150) 发射，WHEN 批量更新，THEN S9 摊位界面 + S1 Hub HUD 货币余额同步更新

### HUD Visibility Gating

- [x] **AC-12**: GIVEN _active_screen=HUB，WHEN 检查 S1/S5 可见性，THEN S1 visible=true + S5 visible=false
- [x] **AC-13**: GIVEN _active_screen=EXPLORATION 或 EXTRACTING，WHEN 检查 S1/S5 可见性，THEN S1 visible=false + S5 visible=true
- [x] **AC-14**: GIVEN _active_screen=VOYAGE（航行过渡），WHEN 检查，THEN S1 visible=false + S5 visible=false。两个 HUD 均隐藏

### Panel Lifecycle — Non-Modal

- [x] **AC-15**: GIVEN 玩家距离 S11 嗅辨锚点 1.5× anchor_radius，WHEN PROXIMITY_ENTER 触发，THEN _preload_panel_data("S11") 异步调用。面板数据在后台加载，不阻塞
- [x] **AC-16**: GIVEN 非模态面板 S11 数据预加载完成 + 玩家按 Use，WHEN open_non_modal("S11")，THEN S11 打开——0.25s 羊皮纸翻开 tween。面板 ACTIVE
- [x] **AC-17**: GIVEN S11 已打开 + 玩家离开 2× anchor_radius，WHEN PROXIMITY_EXIT 触发，THEN S11 自动关闭——0.15s 合上 tween。面板从 _non_modal_panels 移除
- [x] **AC-18**: GIVEN S11 已打开 + 玩家在锚点范围内按 Esc，WHEN esc_pressed，THEN S11 立即关闭。不等待 PROXIMITY_EXIT

### Panel Lifecycle — Modal

- [x] **AC-19**: GIVEN 玩家对修复节点按 Use，WHEN open_modal("S8", {node_id: "lighthouse_01"})，THEN S8 打开。不依赖距离——由领域事件（Use on repair node）触发
- [x] **AC-20**: GIVEN S8 已打开 + 玩家在修复面板中按"取消"，WHEN close_modal()，THEN S8 关闭。WASD 移动恢复

### Lazy Loading & Cache Pool

- [x] **AC-21**: GIVEN S7 战斗面板在 HUD 初始化时 preload()，WHEN 探索中威胁触发→S7 打开，THEN S7 立即实例化——无加载延迟（< 1ms）
- [x] **AC-22**: GIVEN 缓存池有 2 个面板实例（LRU），WHEN 第 3 个面板请求打开，THEN 最久未使用的面板实例被淘汰（queue_free()）。新面板实例加入缓存池
- [x] **AC-23**: GIVEN 场景从 HUB 切换到 VOYAGE，WHEN 场景切换，THEN 缓存池清空。所有缓存的面板实例 queue_free()

### StationDetailPanel Template

- [x] **AC-24**: GIVEN 玩家 Use 情报台锚点 + Use 仓库锚点，WHEN 两个 S2 面板先后打开，THEN 使用同一个 StationDetailPanel 模板。数据从 Registry 绑定——不同站点显示不同内容。非 2 个独立场景

---

## Implementation Notes

### Dirty Flag Core

```text
func _on_signal(element_id: StringName, payload: Variant) -> void:
    _state["_dirty_flags"][element_id] = true
    _state["_pending_payloads"][element_id] = payload

func _process(_delta: float) -> void:
    if _state["_dirty_flags"].is_empty():
        return  # 空闲帧零开销
    for element_id in _state["_dirty_flags"]:
        _update_hud_element(element_id, _state["_pending_payloads"][element_id])
    _state["_dirty_flags"].clear()
```

### Signal Subscription (in _on_feature_ready)

```text
func _connect_hud_signals() -> void:
    AirshipModuleSystem.hull_integrity_changed.connect(_on_signal.bind(&"hull_bar"))
    AirshipModuleSystem.hull_band_changed.connect(_on_signal.bind(&"hull_band"))
    AirshipModuleSystem.module_state_changed.connect(_on_signal.bind(&"module_lights"))
    ResourcesManager.storage_changed.connect(_on_signal.bind(&"storage_bar"))
    ResourcesManager.cargo_changed.connect(_on_signal.bind(&"cargo_bar"))
    ResourcesManager.carried_changed.connect(_on_signal.bind(&"carried_grid"))
    ResourcesManager.currency_changed.connect(_on_signal.bind(&"currency_display"))
    ExplorationManager.search_progress_changed.connect(_on_signal.bind(&"search_count"))
    ExplorationManager.scout_preview_changed.connect(_on_signal.bind(&"threat_preview"))
```

### Panel Lifecycle — Non-Modal

```text
func _on_proximity_enter(anchor_id: StringName) -> void:
    var panel_id := _anchor_to_panel_id(anchor_id)
    if panel_id == &"":
        return
    _preload_panel_data_async(panel_id)

func _on_proximity_exit(anchor_id: StringName) -> void:
    var panel_id := _anchor_to_panel_id(anchor_id)
    if panel_id in _state["_non_modal_panels_map"]:
        close_non_modal(panel_id)
```

### Cache Pool (LRU)

```text
const PANEL_CACHE_MAX := 2

func _cache_panel(panel_id: StringName, instance: Control) -> void:
    if _state["_panel_cache"].size() >= PANEL_CACHE_MAX:
        var lru_key := _state["_panel_cache"].keys()[0]
        _state["_panel_cache"][lru_key].queue_free()
        _state["_panel_cache"].erase(lru_key)
    _state["_panel_cache"][panel_id] = instance
```

---

## Out of Scope

- 屏幕状态机的 HUD 可见性门控触发——属于 Story 001（_apply_screen_visibility）
- 4 层输入路由的鼠标穿透规则（mouse_filter=IGNORE）——属于 Story 002
- 羊皮纸翻开/合上动画实现——属于 Story 005
- desktop window freeze 的 full_ui_refresh 实现——属于 Story 006
- 各领域系统的信号发射——属于各自系统的 Epic
- StationDetailPanel 模板的 UI 布局——属于 #16 UIManager 场景实现

---

## QA Test Cases

- **AC-1-4**: Dirty flag set→batch update→clear→idle zero-cost
- **AC-5-11**: All 11 signal→HUD element mappings verified
- **AC-12-14**: HUD visibility gating by screen state
- **AC-15-18**: Non-modal panel distance-driven lifecycle
- **AC-19-20**: Modal panel event-driven lifecycle
- **AC-21-23**: Lazy loading + LRU cache pool
- **AC-24**: StationDetailPanel template reuse

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/ui-hud-interface/HudUpdatePanelLifecycleTest.csproj` — must exist and pass
**Status**: [x] Created and passing — `dotnet run --project tests/unit/ui-hud-interface/HudUpdatePanelLifecycleTest.csproj -p:UseSharedCompilation=false` (25/25 PASS, 2026-05-14)

---

## Implementation Evidence

**Implementation**: `src/presentation/UIManager.cs` extends the existing headless C# UIManager logic contract with HUD dirty flags, process-priority batch updates, HUD element snapshots, non-modal/modal lifecycle state, S7 preload state, two-entry LRU panel cache, and StationDetailPanel template binding.
**Test Evidence**: `tests/unit/ui-hud-interface/HudUpdatePanelLifecycleTest.csproj` — 25/25 PASS.
**Regression Evidence**: `tests/unit/ui-hud-interface/ScreenStateMachineTest.csproj` — 20/20 PASS; `tests/unit/ui-hud-interface/ModalStackInputRoutingTest.csproj` — 25/25 PASS.
**Residual Risk**: This verifies the UIManager logic model. Real Godot Control node rendering, actual `create_tween()` animation playback, scene PackedScene preload timing, and visual/manual HUD behavior remain downstream #16 integration and story-done verification scope.

---

## Dependencies

- Depends on: Story 001 (screen states for visibility gating), ADR-0002 (signal connection pattern)
- Unlocks: Story 004 (panel data binding on lifecycle events), Story 005 (animation integration with lifecycle)

## Completion Notes

**Completed**: 2026-05-14
**Criteria**: 24/24 acceptance criteria passing; runner reports 25/25 checks including the added scout preview three-state regression.
**Deviations**: No blocking GDD/ADR implementation deviations. Advisory documentation issue: `docs/architecture/tr-registry.yaml` maps `TR-ui-003` to an older screen-state-machine text, while Epic/Story/GDD C.6/C.7 define HUD update + lifecycle/cache scope.
**Test Evidence**: Logic story evidence at `tests/unit/ui-hud-interface/HudUpdatePanelLifecycleTest.csproj` — 25/25 PASS. Regression: Story 001 20/20 PASS; Story 002 25/25 PASS; `dotnet build CloudWeaverVoyage.sln --no-restore -p:UseSharedCompilation=false` PASS with 0 warnings, 0 errors.
**Code Review**: Complete — `/code-review src/presentation/UIManager.cs tests/unit/ui-hud-interface/HudUpdatePanelLifecycleProgram.cs` APPROVED after fixing the carried signal contract and scout preview three-state mapping.
