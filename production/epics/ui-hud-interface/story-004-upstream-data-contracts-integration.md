# Story 004: Upstream Data Contracts & Domain Integration

> **Epic**: UI / HUD / 航图界面
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: Not yet created — run `/create-control-manifest`

## Context

**GDD**: `design/gdd/ui-hud-chart-interface.md`
**Requirement**: `TR-ui-004`

**ADR Governing Implementation**: ADR-0012 (§9 上游数据接口, C.12 上游数据接口)
**ADR Decision Summary**: UIManager 不拥有任何领域数据——所有显示数据通过直接方法调用从 9 个上游领域系统获取。19 个查询接口覆盖全部 12 屏的数据需求：#5 Resources（get_carried_inventory/get_storage_state/get_cargo_state/get_currency）→ S1/S5/S9/S12；#8 Modules（get_hull_integrity/get_module_states）→ S1/S5；#9 Chart（get_chart_state/get_visible_routes/get_selected_route/get_filter_state）→ S4；#11 Exploration（get_search_progress/get_scout_preview_level/get_extraction_state）→ S5/S6b/S6c；#12 Combat（build_threat_context）→ S7；#13 WorldRepair（get_repair_state）→ S8；#14 Settlement（get_stall_data）→ S9；#15 Partner（query_partner_name/get_sniff_items/naming_prompt_eligibility）→ S10/S11；#1 Registry（get_display_name/get_description）→ 所有面板。面板在每次打开时调用 bind_data() 获取领域系统最新数据——面板打开期间不自动刷新（打开时快照语义）。面板关闭时不依赖面板内数据做写回——提交动作通过领域系统 API 完成。下游系统禁止缓存 UIManager 查询结果。

#16 初始化顺序：_ready() 中注册 12 屏清单 + 声明信号 → 收到 ui_ready → 连接 11 个 HUD 信号 → 预加载 S7 面板 → 设置 S1 可见。初始化期间不调用其他 Autoload 方法——数据绑定仅在面板打开时触发。

**Engine**: Godot 4.6.2 | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: 面板每次打开时重新 bind_data()——不使用缓存数据；面板打开期间不自动刷新——保持打开时快照；提交动作通过领域 API 写回——不从面板数据反向同步
- Forbidden: 在 _ready() 中调用其他 Autoload 方法；下游系统缓存 UIManager.set_data() 结果
- Guardrail: 上游系统不可用时 panel 显示空状态视图——不崩溃；get_display_name() 失败时回退到 entity_id 原始字符串

---

## Acceptance Criteria

### Initialization Sequence

- [ ] **AC-1**: GIVEN UIManager._ready() 调用，WHEN 检查，THEN 仅执行常量定义 + 12 屏注册表填充 + 信号声明。不调用任何其他 Autoload 方法
- [ ] **AC-2**: GIVEN ui_ready 信号收到，WHEN _on_ui_ready()，THEN 依次: (1) 连接 11 个 HUD 信号 (2) preload S7 战斗面板 PackedScene (3) 设置 S1 visible=true (4) S5 visible=false

### Per-Panel Data Binding

- [ ] **AC-3**: GIVEN S4 航图首次打开（CHART），WHEN open_screen("CHART")，THEN UIManager 调用 Chart.get_chart_state() + Chart.get_visible_routes()。数据绑定到航图控件。后续面板打开期间的路线数据变更不刷新航图——使用打开时快照
- [ ] **AC-4**: GIVEN S7 战斗面板打开，WHEN open_modal("S7")，THEN UIManager 调用 Combat.build_threat_context()。返回的 threat_name/description/hull_state/options[] 渲染到面板
- [ ] **AC-5**: GIVEN S8 修复面板打开，WHEN open_modal("S8", {node_id: "beacon_02"})，THEN UIManager 调用 WorldRepair.get_repair_state("beacon_02")。node_name/materials[]/unlock_preview 渲染
- [ ] **AC-6**: GIVEN S9 摊位界面打开，WHEN open_modal("S9", {stall_id: "stall_weaver"})，THEN UIManager 调用 SettlementManager.get_stall_data("stall_weaver")。npc_name/goods[] 渲染
- [ ] **AC-7**: GIVEN S10 命名模态打开，WHEN open_modal("S10")，THEN UIManager 调用 PartnerManager.query_partner_name() + PartnerManager.naming_prompt_eligibility()
- [ ] **AC-8**: GIVEN S11 嗅辨面板打开，WHEN open_non_modal("S11")，THEN UIManager 调用 PartnerManager.get_sniff_items()。仅 cat_sniff_signature 非空的物品出现在列表中

### Empty State Views

- [ ] **AC-9**: GIVEN 背包中无可嗅辨物品（get_sniff_items() 返回 []），WHEN S11 打开，THEN 显示空状态："猫没有闻到任何值得注意的气味——试试从探索中带回更多材料"
- [ ] **AC-10**: GIVEN 仓库为空（get_storage_state() 返回 current=0），WHEN S12 打开，THEN 显示空状态："从探索中带回材料或拆包货物来填充"
- [ ] **AC-11**: GIVEN Chart.get_visible_routes() 返回 []（无可见路线），WHEN S4 打开，THEN 显示空状态："没有可读取的航线——去情报台了解更多信息"

### Graceful Degradation

- [ ] **AC-12**: GIVEN ResourcesManager 不可用（null），WHEN S1 HUD 尝试 bind_data()，THEN 仓库显示 "—" + 货舱显示 "—"。不崩溃
- [ ] **AC-13**: GIVEN Registry.get_display_name(item_id) 抛出异常或返回空，WHEN 任何面板渲染物品名，THEN 回退显示 item_id 原始字符串。不崩溃
- [ ] **AC-14**: GIVEN PartnerManager 不可用（null），WHEN S11 打开，THEN 显示空状态——"伙伴系统暂不可用"

### Data Write-Back (Not via UI)

- [ ] **AC-15**: GIVEN S6a 容量取舍面板 + 玩家选择丢弃 item_A，WHEN 确认，THEN UIManager 调用 ResourcesManager.transfer_item(item_A, CARRIED, STORAGE, 1) 或 ResourcesManager.discard_item(item_A)。不通过面板内数据反向写回
- [ ] **AC-16**: GIVEN S8 修复面板 + 玩家提交材料，WHEN 确认提交，THEN UIManager 调用 WorldRepair.submit_repair(node_id, materials)。不通过 _state 反向写回

---

## Implementation Notes

### Data Binding on Panel Open

```gdscript
func _bind_panel_data(panel_id: StringName, panel: Control, context: Dictionary) -> void:
    match panel_id:
        &"S4":
            var chart_state := Chart.get_chart_state()
            var routes := Chart.get_visible_routes()
            panel.bind_data({&"chart_state": chart_state, &"routes": routes})
        &"S7":
            var threat_ctx := Combat.build_threat_context()
            panel.bind_data(threat_ctx)
        &"S8":
            var node_id: StringName = context.get("node_id", &"")
            var repair_state := WorldRepair.get_repair_state(node_id)
            panel.bind_data(repair_state)
        &"S9":
            var stall_id: StringName = context.get("stall_id", &"")
            var stall_data := SettlementManager.get_stall_data(stall_id)
            panel.bind_data(stall_data)
        &"S11":
            var items := _safe_call(PartnerManager, &"get_sniff_items", [])
            panel.bind_data({&"items": items})

func _safe_call(target: Object, method: StringName, default: Variant) -> Variant:
    if target == null or not target.has_method(method):
        return default
    return target.call(method)
```

### Upstream Query Interface Registry

```gdscript
const UPSTREAM_QUERIES: Dictionary = {
    &"S1": [&"get_hull_integrity", &"get_module_states", &"get_storage_state", &"get_cargo_state", &"get_currency"],
    &"S4": [&"get_chart_state", &"get_visible_routes", &"get_filter_state"],
    &"S5": [&"get_carried_inventory", &"get_search_progress", &"get_scout_preview_level", &"get_hull_integrity"],
    &"S7": [&"build_threat_context"],
    &"S8": [&"get_repair_state"],
    &"S9": [&"get_stall_data"],
    &"S10": [&"query_partner_name", &"naming_prompt_eligibility"],
    &"S11": [&"get_sniff_items", &"query_partner_name"],
}
```

---

## Out of Scope

- 各领域系统查询方法的具体实现——属于各自系统的 Epic
- StationDetailPanel 模板的 UI 布局——属于 #16 场景实现
- HUD 脏标记信号订阅（connect）——属于 Story 003
- 下游语义事件的发射——属于 Story 005

---

## QA Test Cases

- **AC-1/2**: Init sequence — no cross-Autoload calls in _ready
- **AC-3-8**: Per-panel data binding on open (S4/S7/S8/S9/S10/S11)
- **AC-9-11**: Empty state views for all panels
- **AC-12-14**: Graceful degradation when upstream unavailable
- **AC-15/16**: Write-back via domain API, not UI state

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/ui-hud-interface/upstream_data_contracts_test.gd` — must exist and pass, OR documented playtest
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (panel open flow), Story 002 (modal management), Story 003 (panel lifecycle triggers bind_data)
- External: #5 Resources, #8 Modules, #9 Chart, #11 Exploration, #12 Combat, #13 WorldRepair, #14 Settlement, #15 Partner, #1 Registry (query interfaces)
- Unlocks: Story 005 (semantic events after data binding), Story 006 (edge cases with missing data)
